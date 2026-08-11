using ExcelDataReader;
using Simplify.Quasar.Areas.EstoqueApp.ViewModels;
using Simplify.Quasar.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;

namespace Simplify.Quasar.Custom
{
    public class LocacaoLoteService
    {
        private const int MaximoLinhasPlanilha = 1000;
        private const int MaximoLocacoesPorLinha = 50000;
        private const int MaximoLocacoesArquivo = 100000;

        private static readonly string[] CabecalhosEsperados =
        {
            "descricao", "area", "zona", "corredor", "estanteinicio", "estantefinal",
            "nivelinicio", "nivelfinal", "compartimentoinicio", "compartimentofinal",
            "lado", "demanda", "equipamento"
        };

        private readonly Quasar_Entities db;
        private readonly int filialId;

        public LocacaoLoteService(Quasar_Entities db, int filialId)
        {
            this.db = db ?? throw new ArgumentNullException("db");
            this.filialId = filialId;
        }

        public LocacaoLoteSessao Simular(HttpPostedFileBase arquivo)
        {
            ValidarArquivo(arquivo);

            List<PlanilhaLinha> planilha = LerPlanilha(arquivo.InputStream);
            var preview = new LocacaoLoteViewModel
            {
                NomeArquivo = Path.GetFileName(arquivo.FileName),
                LinhasImportadas = planilha.Count
            };

            if (planilha.Count == 0)
            {
                preview.ErroGeral = "A planilha não possui linhas de dados.";
                return CriarSessao(preview);
            }

            var areas = db.Area.AsNoTracking()
                .Where(x => x.FilialId == filialId || x.FilialId == null)
                .ToList();
            var zonas = db.Zona.AsNoTracking()
                .Where(x => x.Ativo && (x.FilialId == filialId || x.FilialId == null))
                .ToList();
            var equipamentos = db.Equipamento.AsNoTracking()
                .Where(x => x.FilialId == filialId || x.FilialId == null)
                .ToList();
            var codigosExistentes = new HashSet<string>(
                db.Locacao.AsNoTracking()
                    .Where(x => x.FilialId == filialId)
                    .Select(x => x.Codigo)
                    .ToList()
                    .Select(LocacaoService.NormalizarCodigo),
                StringComparer.OrdinalIgnoreCase);

            var codigosArquivo = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var itens = new List<LocacaoLoteItem>();

            foreach (PlanilhaLinha origem in planilha)
            {
                LocacaoLoteLinhaViewModel linha = CriarPreviewLinha(origem);
                preview.Linhas.Add(linha);

                Area area = ResolverArea(areas, origem.Area);
                if (area == null)
                {
                    linha.Erros.Add("Área não encontrada para a filial.");
                }

                Zona zona = ResolverZona(zonas, origem.Zona);
                if (zona == null)
                {
                    linha.Erros.Add("Zona não encontrada ou inativa.");
                }

                Equipamento equipamento = ResolverEquipamento(equipamentos, origem.Equipamento);
                if (!string.IsNullOrWhiteSpace(origem.Equipamento) && equipamento == null)
                {
                    linha.Erros.Add("Equipamento não encontrado.");
                }

                ValidarObrigatorios(origem, linha);

                int corredor = 0;
                int estanteInicio = 0;
                int estanteFinal = 0;
                if (!TryNumero(origem.Corredor, 1, 99, out corredor))
                {
                    linha.Erros.Add("Corredor deve ser numérico, entre 1 e 99.");
                }

                if (!TryNumero(origem.EstanteInicio, 1, 99, out estanteInicio) ||
                    !TryNumero(origem.EstanteFinal, 1, 99, out estanteFinal))
                {
                    linha.Erros.Add("Estantes inicial e final devem estar entre 1 e 99.");
                }
                else if (estanteInicio > estanteFinal)
                {
                    linha.Erros.Add("Estante inicial deve ser menor ou igual à final.");
                }

                string lado = NormalizarLado(origem.Lado);
                if (lado == null)
                {
                    linha.Erros.Add("Lado deve ser Ambos, Par ou Ímpar.");
                }

                string demanda = NormalizarDominio(origem.Demanda);
                if (demanda != "A" && demanda != "B" && demanda != "C" && demanda != "D" && demanda != "N")
                {
                    linha.Erros.Add("Demanda deve ser A, B, C, D ou N.");
                }

                List<string> niveis;
                string erroNivel;
                if (!TryExpandirFaixa(origem.NivelInicio, origem.NivelFinal, out niveis, out erroNivel))
                {
                    linha.Erros.Add("Nível: " + erroNivel);
                }

                List<string> compartimentos;
                string erroCompartimento;
                if (!TryExpandirFaixa(origem.CompartimentoInicio, origem.CompartimentoFinal, out compartimentos, out erroCompartimento))
                {
                    linha.Erros.Add("Compartimento: " + erroCompartimento);
                }

                if (!linha.Valida)
                {
                    continue;
                }

                List<int> estantes = Enumerable.Range(estanteInicio, estanteFinal - estanteInicio + 1)
                    .Where(x => lado == "A" || (lado == "P" && x % 2 == 0) || (lado == "I" && x % 2 != 0))
                    .ToList();

                long quantidade = (long)estantes.Count * niveis.Count * compartimentos.Count;
                if (quantidade == 0)
                {
                    linha.Erros.Add("O lado selecionado não gera nenhuma estante no intervalo.");
                    continue;
                }

                if (quantidade > MaximoLocacoesPorLinha)
                {
                    linha.Erros.Add(string.Format("A linha geraria {0:N0} locações; o limite por linha é {1:N0}.", quantidade, MaximoLocacoesPorLinha));
                    continue;
                }

                linha.QuantidadePrevista = (int)quantidade;
                foreach (int estante in estantes)
                {
                    foreach (string nivel in niveis)
                    {
                        foreach (string compartimento in compartimentos)
                        {
                            string codigo = LocacaoService.FormarCodigo(zona.Codigo, corredor, estante, nivel, compartimento);
                            if (!codigosArquivo.Add(codigo))
                            {
                                linha.QuantidadeDuplicadaArquivo++;
                                continue;
                            }

                            bool jaExiste = codigosExistentes.Contains(codigo);
                            if (jaExiste)
                            {
                                linha.QuantidadeExistente++;
                            }
                            else
                            {
                                linha.QuantidadeNova++;
                            }

                            itens.Add(new LocacaoLoteItem
                            {
                                Linha = origem.Numero,
                                Codigo = codigo,
                                Descricao = origem.Descricao.Trim(),
                                AreaId = area.Id,
                                ZonaId = zona.Id,
                                EquipamentoId = equipamento == null ? (int?)null : equipamento.Id,
                                Demanda = demanda,
                                JaExiste = jaExiste
                            });
                        }
                    }
                }

                if (itens.Count > MaximoLocacoesArquivo)
                {
                    preview.ErroGeral = string.Format("O arquivo ultrapassa o limite de {0:N0} locações por importação.", MaximoLocacoesArquivo);
                    break;
                }
            }

            preview.LinhasValidas = preview.Linhas.Count(x => x.Valida);
            preview.LinhasComErro = preview.Linhas.Count(x => !x.Valida);
            preview.LocacoesPrevistas = preview.Linhas.Where(x => x.Valida).Sum(x => x.QuantidadePrevista - x.QuantidadeDuplicadaArquivo);
            preview.LocacoesJaExistentes = preview.Linhas.Sum(x => x.QuantidadeExistente);
            preview.LocacoesDuplicadasArquivo = preview.Linhas.Sum(x => x.QuantidadeDuplicadaArquivo);
            preview.NovasLocacoes = preview.Linhas.Sum(x => x.QuantidadeNova);

            return CriarSessao(preview, itens);
        }

        private LocacaoLoteSessao CriarSessao(LocacaoLoteViewModel preview, List<LocacaoLoteItem> itens = null)
        {
            var token = Guid.NewGuid().ToString("N");
            preview.Token = token;
            return new LocacaoLoteSessao
            {
                Token = token,
                NomeArquivo = preview.NomeArquivo,
                CriadoEm = Util.GetCurrentDateTime(),
                FilialId = filialId,
                Preview = preview,
                Itens = itens ?? new List<LocacaoLoteItem>()
            };
        }

        private static void ValidarArquivo(HttpPostedFileBase arquivo)
        {
            if (arquivo == null || arquivo.ContentLength <= 0)
            {
                throw new InvalidOperationException("Selecione um arquivo Excel.");
            }

            if (arquivo.ContentLength > 10 * 1024 * 1024)
            {
                throw new InvalidOperationException("O arquivo não pode exceder 10 MB.");
            }

            string extensao = Path.GetExtension(arquivo.FileName ?? string.Empty).ToLowerInvariant();
            if (extensao != ".xlsx" && extensao != ".xls")
            {
                throw new InvalidOperationException("Formato inválido. Utilize um arquivo .xlsx ou .xls.");
            }
        }

        private static List<PlanilhaLinha> LerPlanilha(Stream stream)
        {
            var linhas = new List<PlanilhaLinha>();
            using (IExcelDataReader reader = ExcelReaderFactory.CreateReader(stream))
            {
                if (!reader.Read())
                {
                    return linhas;
                }

                var cabecalhos = new List<string>();
                for (int coluna = 0; coluna < reader.FieldCount; coluna++)
                {
                    cabecalhos.Add(NormalizarCabecalho(ConverterCelula(reader.GetValue(coluna))));
                }

                if (cabecalhos.Count < CabecalhosEsperados.Length || !CabecalhosValidos(cabecalhos))
                {
                    throw new InvalidOperationException("O cabeçalho da planilha é inválido. Utilize o modelo disponibilizado pelo sistema.");
                }

                int numeroLinha = 1;
                while (reader.Read())
                {
                    numeroLinha++;
                    var valores = new string[CabecalhosEsperados.Length];
                    for (int coluna = 0; coluna < valores.Length; coluna++)
                    {
                        valores[coluna] = coluna < reader.FieldCount
                            ? ConverterCelula(reader.GetValue(coluna))
                            : string.Empty;
                    }

                    if (valores.All(string.IsNullOrWhiteSpace))
                    {
                        continue;
                    }

                    linhas.Add(new PlanilhaLinha(numeroLinha, valores));
                    if (linhas.Count > MaximoLinhasPlanilha)
                    {
                        throw new InvalidOperationException(string.Format("A planilha excede o limite de {0:N0} linhas.", MaximoLinhasPlanilha));
                    }
                }
            }

            return linhas;
        }

        private static LocacaoLoteLinhaViewModel CriarPreviewLinha(PlanilhaLinha origem)
        {
            return new LocacaoLoteLinhaViewModel
            {
                Linha = origem.Numero,
                Descricao = origem.Descricao,
                Area = origem.Area,
                Zona = origem.Zona,
                Corredor = origem.Corredor,
                EstanteInicio = origem.EstanteInicio,
                EstanteFinal = origem.EstanteFinal,
                NivelInicio = origem.NivelInicio,
                NivelFinal = origem.NivelFinal,
                CompartimentoInicio = origem.CompartimentoInicio,
                CompartimentoFinal = origem.CompartimentoFinal,
                Lado = origem.Lado,
                Demanda = origem.Demanda,
                Equipamento = origem.Equipamento
            };
        }

        private static void ValidarObrigatorios(PlanilhaLinha origem, LocacaoLoteLinhaViewModel linha)
        {
            var obrigatorios = new Dictionary<string, string>
            {
                { "Descrição", origem.Descricao }, { "Área", origem.Area }, { "Zona", origem.Zona },
                { "Corredor", origem.Corredor }, { "Estante inicial", origem.EstanteInicio },
                { "Estante final", origem.EstanteFinal }, { "Nível inicial", origem.NivelInicio },
                { "Nível final", origem.NivelFinal }, { "Compartimento inicial", origem.CompartimentoInicio },
                { "Compartimento final", origem.CompartimentoFinal }, { "Lado", origem.Lado },
                { "Demanda", origem.Demanda }
            };

            foreach (KeyValuePair<string, string> campo in obrigatorios.Where(x => string.IsNullOrWhiteSpace(x.Value)))
            {
                linha.Erros.Add(campo.Key + " é obrigatório(a).");
            }

            if ((origem.Descricao ?? string.Empty).Trim().Length > 100)
                linha.Erros.Add("Descrição não pode exceder 100 caracteres.");
            if ((origem.Area ?? string.Empty).Trim().Length > 2)
                linha.Erros.Add("Área não pode exceder 2 caracteres.");
            if ((origem.Zona ?? string.Empty).Trim().Length > 2)
                linha.Erros.Add("Zona não pode exceder 2 caracteres.");
        }

        private Area ResolverArea(IEnumerable<Area> areas, string valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
            {
                return null;
            }

            string chave = NormalizarPesquisa(valor);
            return areas.FirstOrDefault(x => NormalizarPesquisa(x.Nome) == chave || x.Id.ToString(CultureInfo.InvariantCulture) == chave);
        }

        private Zona ResolverZona(IEnumerable<Zona> zonas, string valor)
        {
            string chave = NormalizarPesquisa(valor);
            return zonas.FirstOrDefault(x => NormalizarPesquisa(x.Codigo) == chave || NormalizarPesquisa(x.Nome) == chave);
        }

        private Equipamento ResolverEquipamento(IEnumerable<Equipamento> equipamentos, string valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
            {
                return null;
            }

            string chave = NormalizarPesquisa(valor);
            return equipamentos.FirstOrDefault(x =>
                NormalizarPesquisa(x.Nome) == chave ||
                NormalizarPesquisa(x.Tipo) == chave ||
                x.Id.ToString(CultureInfo.InvariantCulture) == chave);
        }

        private static bool TryNumero(string valor, int minimo, int maximo, out int numero)
        {
            string texto = (valor ?? string.Empty).Trim();
            return int.TryParse(texto, NumberStyles.Integer, CultureInfo.InvariantCulture, out numero) && numero >= minimo && numero <= maximo;
        }

        private static bool TryExpandirFaixa(string inicio, string final, out List<string> valores, out string erro)
        {
            valores = new List<string>();
            erro = null;
            string primeiro = NormalizarDominio(inicio);
            string ultimo = NormalizarDominio(final);

            if (string.IsNullOrWhiteSpace(primeiro) || string.IsNullOrWhiteSpace(ultimo))
            {
                erro = "início e final são obrigatórios.";
                return false;
            }

            if (primeiro.Length > 2 || ultimo.Length > 2)
            {
                erro = "os códigos devem possuir no máximo 2 caracteres.";
                return false;
            }

            int numeroInicio;
            int numeroFinal;
            if (int.TryParse(primeiro, NumberStyles.Integer, CultureInfo.InvariantCulture, out numeroInicio) &&
                int.TryParse(ultimo, NumberStyles.Integer, CultureInfo.InvariantCulture, out numeroFinal))
            {
                if (numeroInicio < 0 || numeroFinal > 99 || numeroInicio > numeroFinal)
                {
                    erro = "o intervalo numérico deve ser crescente e estar entre 0 e 99.";
                    return false;
                }

                for (int numero = numeroInicio; numero <= numeroFinal; numero++)
                {
                    valores.Add(numero.ToString(CultureInfo.InvariantCulture));
                }
                return true;
            }

            if (!primeiro.All(char.IsLetter) || !ultimo.All(char.IsLetter))
            {
                erro = "use uma faixa totalmente numérica ou alfabética.";
                return false;
            }

            int alphaInicio = CodigoAlfaParaNumero(primeiro);
            int alphaFinal = CodigoAlfaParaNumero(ultimo);
            if (alphaInicio > alphaFinal)
            {
                erro = "o código inicial deve ser menor ou igual ao final.";
                return false;
            }

            for (int numero = alphaInicio; numero <= alphaFinal; numero++)
            {
                valores.Add(NumeroParaCodigoAlfa(numero));
            }
            return true;
        }

        private static int CodigoAlfaParaNumero(string valor)
        {
            int resultado = 0;
            foreach (char caractere in valor)
            {
                resultado = checked(resultado * 26 + (caractere - 'A' + 1));
            }
            return resultado;
        }

        private static string NumeroParaCodigoAlfa(int valor)
        {
            var resultado = new StringBuilder();
            while (valor > 0)
            {
                valor--;
                resultado.Insert(0, (char)('A' + (valor % 26)));
                valor /= 26;
            }
            return resultado.ToString();
        }

        private static string ConverterCelula(object valor)
        {
            if (valor == null || valor == DBNull.Value)
            {
                return string.Empty;
            }

            if (valor is double)
            {
                double numero = (double)valor;
                return Math.Abs(numero % 1) < 0.0000001
                    ? numero.ToString("0", CultureInfo.InvariantCulture)
                    : numero.ToString(CultureInfo.InvariantCulture);
            }

            return Convert.ToString(valor, CultureInfo.InvariantCulture).Trim();
        }

        private static string NormalizarCabecalho(string valor)
        {
            return new string(Util.RemoverAcentuacao(valor ?? string.Empty)
                .ToLowerInvariant()
                .Where(char.IsLetterOrDigit)
                .ToArray());
        }

        private static string NormalizarPesquisa(string valor)
        {
            return Util.RemoverAcentuacao((valor ?? string.Empty).Trim()).ToUpperInvariant();
        }

        private static string NormalizarDominio(string valor)
        {
            return (valor ?? string.Empty).Trim().ToUpperInvariant();
        }

        private static string NormalizarLado(string valor)
        {
            string lado = NormalizarPesquisa(valor);
            if (lado == "A" || lado == "AMBOS") return "A";
            if (lado == "P" || lado == "PAR" || lado == "PARES") return "P";
            if (lado == "I" || lado == "IMPAR" || lado == "IMPARES") return "I";
            return null;
        }

        private static bool CabecalhosValidos(IList<string> cabecalhos)
        {
            string[][] aceitos =
            {
                new[] { "descricao" },
                new[] { "area" },
                new[] { "zona" },
                new[] { "corredor" },
                new[] { "estante", "estanteinicio", "estanteinicial" },
                new[] { "estante", "estantefinal" },
                new[] { "nivel", "nivelinicio", "nivelinicial" },
                new[] { "nivel", "nivelfinal" },
                new[] { "compartimento", "compartimentoinicio", "compartimentoinicial" },
                new[] { "compartimento", "compartimentofinal" },
                new[] { "lado", "ladoparimparambos" },
                new[] { "demanda" },
                new[] { "equipamento" }
            };

            return aceitos.Select((opcoes, indice) => opcoes.Contains(cabecalhos[indice])).All(x => x);
        }

        private sealed class PlanilhaLinha
        {
            public PlanilhaLinha(int numero, string[] valores)
            {
                Numero = numero;
                Descricao = valores[0]; Area = valores[1]; Zona = valores[2]; Corredor = valores[3];
                EstanteInicio = valores[4]; EstanteFinal = valores[5]; NivelInicio = valores[6]; NivelFinal = valores[7];
                CompartimentoInicio = valores[8]; CompartimentoFinal = valores[9]; Lado = valores[10];
                Demanda = valores[11]; Equipamento = valores[12];
            }

            public int Numero { get; private set; }
            public string Descricao { get; private set; }
            public string Area { get; private set; }
            public string Zona { get; private set; }
            public string Corredor { get; private set; }
            public string EstanteInicio { get; private set; }
            public string EstanteFinal { get; private set; }
            public string NivelInicio { get; private set; }
            public string NivelFinal { get; private set; }
            public string CompartimentoInicio { get; private set; }
            public string CompartimentoFinal { get; private set; }
            public string Lado { get; private set; }
            public string Demanda { get; private set; }
            public string Equipamento { get; private set; }
        }
    }
}
