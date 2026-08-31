using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using Simplify.Quasar.Models;

namespace Simplify.Quasar.Areas.AnomaliaApp.Services
{
    public class AnomaliaFormularioArquivo
    {
        public string NomeArquivo { get; set; }
        public byte[] Conteudo { get; set; }
    }

    public class AnomaliaFormularioGmService
    {
        private const int ItensPorFormularioAbc = 5;
        private const int ItensPorFormularioDanificados = 10;
        private const string TipoFormularioAbc = "ABC";
        private const string TipoFormularioDanificados = "G";

        private readonly Quasar_Entities db;
        private readonly int filialId;
        private readonly string usuario;
        private readonly DateTime agora;
        private readonly string caminhoModelo;

        public AnomaliaFormularioGmService(
            Quasar_Entities db,
            int filialId,
            string usuario,
            DateTime agora,
            string caminhoModelo)
        {
            this.db = db;
            this.filialId = filialId;
            this.usuario = usuario;
            this.agora = agora;
            this.caminhoModelo = caminhoModelo;
        }

        public IList<AnomaliaFormularioArquivo> Gerar(int anomaliaId)
        {
            if (!File.Exists(caminhoModelo))
                throw new InvalidOperationException("O modelo oficial do formulário GM não foi localizado.");

            ProcessoFormulario processo = ObterProcesso(anomaliaId);
            EmpresaFormulario empresa = ObterEmpresa();
            IList<ItemFormulario> itens = ObterItens(anomaliaId);

            if (itens.Count == 0)
                throw new InvalidOperationException("Este processo não possui itens dos tipos A, B ou C para exportação.");
            if (string.IsNullOrWhiteSpace(empresa.CodigoGM))
                throw new InvalidOperationException("Informe o Código GM da empresa antes de exportar o formulário.");

            ItemFormulario semValores = itens.FirstOrDefault(x => !x.PrecoUnitario.HasValue || !x.Imposto.HasValue);
            if (semValores != null)
                throw new InvalidOperationException(
                    "O item " + semValores.ItemNr + " da NF " + semValores.NotaFiscalNr +
                    " não possui preço unitário ou imposto do Trânsito GM. Reimporte o arquivo de recebimento antes de exportar.");

            int quantidadeArquivos = (int)Math.Ceiling(itens.Count / (decimal)ItensPorFormularioAbc);
            var arquivos = new List<AnomaliaFormularioArquivo>();

            for (int indice = 0; indice < quantidadeArquivos; indice++)
            {
                List<ItemFormulario> lote = itens.Skip(indice * ItensPorFormularioAbc)
                    .Take(ItensPorFormularioAbc)
                    .ToList();
                string nome = quantidadeArquivos == 1
                    ? processo.NumeroControle + ".xls"
                    : processo.NumeroControle + "-" + (indice + 1).ToString("00", CultureInfo.InvariantCulture) + ".xls";

                arquivos.Add(new AnomaliaFormularioArquivo
                {
                    NomeArquivo = nome,
                    Conteudo = PreencherModelo(processo, empresa, lote)
                });
            }

            RegistrarGeracao(
                processo.Id,
                arquivos,
                itens.Select(x => x.Id).ToList(),
                ItensPorFormularioAbc,
                TipoFormularioAbc,
                "formulário oficial GM A/B/C");
            return arquivos;
        }

        public IList<AnomaliaFormularioArquivo> GerarDanificados(int anomaliaId)
        {
            if (!File.Exists(caminhoModelo))
                throw new InvalidOperationException("O modelo oficial do formulário Danificados não foi localizado.");

            ProcessoFormulario processo = ObterProcesso(anomaliaId);
            EmpresaFormulario empresa = ObterEmpresa();
            IList<ItemDanificadoFormulario> itens = ObterItensDanificados(anomaliaId);

            if (itens.Count == 0)
                throw new InvalidOperationException("Este processo não possui itens do tipo G para exportação.");
            if (string.IsNullOrWhiteSpace(empresa.CodigoGM))
                throw new InvalidOperationException("Informe o Código GM da empresa antes de exportar o formulário.");

            ItemDanificadoFormulario incompleto = itens.FirstOrDefault(x =>
                !x.PrecoUnitario.HasValue ||
                string.IsNullOrWhiteSpace(x.DetalheDano) ||
                !x.InstaladoVeiculo.HasValue ||
                string.IsNullOrWhiteSpace(x.CondicaoEmbalagem));
            if (incompleto != null)
            {
                if (!incompleto.PrecoUnitario.HasValue)
                    throw new InvalidOperationException(
                        "O item " + incompleto.ItemNr + " da NF " + incompleto.NotaFiscalNr +
                        " não possui preço unitário do Trânsito GM. Reimporte o arquivo de recebimento antes de exportar.");
                throw new InvalidOperationException(
                    "O item " + incompleto.ItemNr + " da NF " + incompleto.NotaFiscalNr +
                    " não possui todos os dados de Danificados.");
            }

            int quantidadeArquivos = (int)Math.Ceiling(itens.Count / (decimal)ItensPorFormularioDanificados);
            var arquivos = new List<AnomaliaFormularioArquivo>();
            for (int indice = 0; indice < quantidadeArquivos; indice++)
            {
                List<ItemDanificadoFormulario> lote = itens
                    .Skip(indice * ItensPorFormularioDanificados)
                    .Take(ItensPorFormularioDanificados)
                    .ToList();
                string nome = quantidadeArquivos == 1
                    ? processo.NumeroControle + ".xls"
                    : processo.NumeroControle + "-" + (indice + 1).ToString("00", CultureInfo.InvariantCulture) + ".xls";

                arquivos.Add(new AnomaliaFormularioArquivo
                {
                    NomeArquivo = nome,
                    Conteudo = PreencherModeloDanificados(processo, empresa, lote)
                });
            }

            RegistrarGeracao(
                processo.Id,
                arquivos,
                itens.Select(x => x.Id).ToList(),
                ItensPorFormularioDanificados,
                TipoFormularioDanificados,
                "formulário oficial GM Danificados");
            return arquivos;
        }

        private ProcessoFormulario ObterProcesso(int anomaliaId)
        {
            ProcessoFormulario processo = db.Database.SqlQuery<ProcessoFormulario>(@"
SELECT Id, NumeroControle, DataAbertura
FROM dbo.AnomaliaGmProcesso
WHERE Id = @id AND FilialId = @filialId AND Ativo = 1 AND Cancelado = 0",
                new SqlParameter("@id", anomaliaId),
                new SqlParameter("@filialId", filialId)).SingleOrDefault();

            if (processo == null)
                throw new InvalidOperationException("Processo de anomalia não localizado para a filial atual.");
            return processo;
        }

        private EmpresaFormulario ObterEmpresa()
        {
            EmpresaFormulario empresa = db.Database.SqlQuery<EmpresaFormulario>(@"
SELECT TOP 1
       Id, CodigoGM, Nome, CNPJ,
       Endereco_Logradouro, Endereco_Numero, Endereco_Complemento, Endereco_Bairro,
       Endereco_Cidade, Endereco_UF, Endereco_CEP,
       COALESCE(NULLIF(Telefone1, ''), NULLIF(Telefone2, ''), Telefone3) AS Telefone
FROM dbo.Empresa
WHERE Id = @filialId",
                new SqlParameter("@filialId", filialId)).SingleOrDefault();

            if (empresa == null)
                throw new InvalidOperationException("Dados da empresa não localizados para a filial atual.");
            return empresa;
        }

        private IList<ItemFormulario> ObterItens(int anomaliaId)
        {
            return db.Database.SqlQuery<ItemFormulario>(@"
SELECT ai.Id,
       tipo.Codigo AS TipoCodigo,
       tipo.Descricao AS TipoDescricao,
       nf.Numero AS NotaFiscalNr,
       CAST(nf.DataEmissao AS datetime) AS DataEmissao,
       nf.RecebidoAdmEm AS DataRecebimento,
       ai.VolumeNr,
       ai.ItemNr,
       ISNULL(materialSolicitado.Descricao, '') AS DescricaoItem,
       ai.QuantidadeNF,
       ai.QuantidadeReclamada,
       ai.QuantidadeRecebida,
       ai.ItemRecebidoNr,
       ISNULL(materialRecebido.Descricao, '') AS DescricaoItemRecebido,
       nfi.PrecoUnitario,
       nfi.Imposto
FROM dbo.AnomaliaGmItem ai
INNER JOIN dbo.AnomaliaGmProcesso p ON p.Id = ai.AnomaliaId
INNER JOIN dbo.AnomaliaGmTipo tipo ON tipo.Id = ai.AnomaliaTipoId
INNER JOIN dbo.NotaFiscal nf ON nf.Id = ai.NotaFiscalId
INNER JOIN dbo.NotaFiscalItem nfi ON nfi.Id = ai.NotaFiscalItemId
OUTER APPLY
(
    SELECT TOP 1 m.Descricao
    FROM dbo.Material m
    WHERE m.Codigo = ai.ItemNr AND (m.FilialId = @filialId OR m.FilialId IS NULL)
    ORDER BY CASE WHEN m.FilialId = @filialId THEN 0 ELSE 1 END, m.Descricao
) materialSolicitado
OUTER APPLY
(
    SELECT TOP 1 m.Descricao
    FROM dbo.Material m
    WHERE m.Codigo = ai.ItemRecebidoNr AND (m.FilialId = @filialId OR m.FilialId IS NULL)
    ORDER BY CASE WHEN m.FilialId = @filialId THEN 0 ELSE 1 END, m.Descricao
) materialRecebido
WHERE ai.AnomaliaId = @anomaliaId
  AND ai.FilialId = @filialId
  AND p.FilialId = @filialId
  AND ai.Cancelado = 0
  AND tipo.Codigo IN ('A', 'B', 'C')
ORDER BY ai.Id",
                new SqlParameter("@anomaliaId", anomaliaId),
                new SqlParameter("@filialId", filialId)).ToList();
        }

        private IList<ItemDanificadoFormulario> ObterItensDanificados(int anomaliaId)
        {
            return db.Database.SqlQuery<ItemDanificadoFormulario>(@"
SELECT ai.Id,
       nf.Numero AS NotaFiscalNr,
       CAST(nf.DataEmissao AS datetime) AS DataEmissao,
       nf.RecebidoAdmEm AS DataRecebimento,
       ai.VolumeNr,
       ai.ItemNr,
       ISNULL(material.Descricao, '') AS DescricaoItem,
       ai.QuantidadeReclamada,
       ai.Observacao AS DetalheDano,
       ai.InstaladoVeiculo,
       ai.CondicaoEmbalagem,
       nfi.PrecoUnitario
FROM dbo.AnomaliaGmItem ai
INNER JOIN dbo.AnomaliaGmProcesso p ON p.Id = ai.AnomaliaId
INNER JOIN dbo.AnomaliaGmTipo tipo ON tipo.Id = ai.AnomaliaTipoId
INNER JOIN dbo.NotaFiscal nf ON nf.Id = ai.NotaFiscalId
INNER JOIN dbo.NotaFiscalItem nfi ON nfi.Id = ai.NotaFiscalItemId
OUTER APPLY
(
    SELECT TOP 1 m.Descricao
    FROM dbo.Material m
    WHERE m.Codigo = ai.ItemNr AND (m.FilialId = @filialId OR m.FilialId IS NULL)
    ORDER BY CASE WHEN m.FilialId = @filialId THEN 0 ELSE 1 END, m.Descricao
) material
WHERE ai.AnomaliaId = @anomaliaId
  AND ai.FilialId = @filialId
  AND p.FilialId = @filialId
  AND ai.Cancelado = 0
  AND tipo.Codigo = 'G'
ORDER BY ai.Id",
                new SqlParameter("@anomaliaId", anomaliaId),
                new SqlParameter("@filialId", filialId)).ToList();
        }

        private byte[] PreencherModelo(
            ProcessoFormulario processo,
            EmpresaFormulario empresa,
            IList<ItemFormulario> itens)
        {
            using (var entrada = new FileStream(caminhoModelo, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var workbook = new HSSFWorkbook(entrada))
            {
                ISheet origem = workbook.GetSheet("Processo1");
                ISheet formulario = workbook.GetSheet("anomalia de A a D");
                if (origem == null || formulario == null)
                    throw new InvalidOperationException("O modelo oficial GM está sem as planilhas esperadas.");

                LimparOrigem(origem, ItensPorFormularioAbc);
                SetData(origem, 1, 0, processo.DataAbertura);
                SetTexto(origem, 1, 1, processo.NumeroControle);
                SetTexto(origem, 1, 38, empresa.CodigoGM);
                SetTexto(origem, 1, 39, empresa.Nome);
                SetTexto(origem, 1, 40, empresa.EnderecoCompleto);
                SetTexto(origem, 1, 41, empresa.Endereco_CEP);
                SetTexto(origem, 1, 42, empresa.Endereco_Cidade);
                SetTexto(origem, 1, 43, empresa.Endereco_UF);
                SetTexto(origem, 1, 44, empresa.CNPJ);
                SetTexto(origem, 1, 46, empresa.Telefone);

                for (int indice = 0; indice < itens.Count; indice++)
                {
                    ItemFormulario item = itens[indice];
                    int linha = indice + 1;
                    SetTexto(origem, linha, 4, item.NotaFiscalNr);
                    SetData(origem, linha, 5, item.DataEmissao);
                    SetData(origem, linha, 6, item.DataRecebimento ?? processo.DataAbertura);
                    SetTexto(origem, linha, 8, item.ItemNr);
                    SetTexto(origem, linha, 9, item.DescricaoItem);
                    SetNumero(origem, linha, 10, item.QuantidadeNF);
                    SetTexto(origem, linha, 11, item.ItemRecebido);
                    SetTexto(origem, linha, 12, item.DescricaoRecebida);
                    SetNumero(origem, linha, 13, item.QuantidadeEfetivamenteRecebida);
                    SetNumero(origem, linha, 14, item.QuantidadeReclamada);
                    SetTexto(origem, linha, 15, item.VolumeNr);
                    SetTexto(origem, linha, 16, item.TipoCodigo + " - " + item.TipoDescricao);
                    SetNumero(origem, linha, 17, item.PrecoUnitario.Value);

                    // A seção financeira do formulário oficial não referencia a
                    // planilha de origem para o imposto; por isso ele é preenchido
                    // diretamente no campo vermelho correspondente ao item.
                    SetNumero(formulario, 26 + indice, 11, item.Imposto.Value * item.QuantidadeReclamada);
                }

                LimparLinhasNaoUtilizadas(formulario, itens.Count);

                // O arquivo oficial recebido possui uma referência inválida nesta
                // célula vazia. Removê-la evita que o formulário exportado exiba #REF!.
                SetVazio(formulario, 21, 10);
                workbook.ForceFormulaRecalculation = true;
                formulario.ForceFormulaRecalculation = true;

                using (var saida = new MemoryStream())
                {
                    workbook.Write(saida, true);
                    return saida.ToArray();
                }
            }
        }

        private byte[] PreencherModeloDanificados(
            ProcessoFormulario processo,
            EmpresaFormulario empresa,
            IList<ItemDanificadoFormulario> itens)
        {
            using (var entrada = new FileStream(caminhoModelo, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var workbook = new HSSFWorkbook(entrada))
            {
                ISheet origem = workbook.GetSheet("Processo1");
                ISheet formulario = workbook.GetSheet("anomalia de G a J");
                if (origem == null || formulario == null)
                    throw new InvalidOperationException("O modelo oficial de Danificados está sem as planilhas esperadas.");

                LimparOrigem(origem, ItensPorFormularioDanificados);
                SetData(origem, 1, 0, processo.DataAbertura);
                SetTexto(origem, 1, 1, processo.NumeroControle);
                SetTexto(origem, 1, 38, empresa.CodigoGM);
                SetTexto(origem, 1, 39, empresa.Nome);
                SetTexto(origem, 1, 40, empresa.EnderecoCompleto);
                SetTexto(origem, 1, 41, empresa.Endereco_CEP);
                SetTexto(origem, 1, 42, empresa.Endereco_Cidade);
                SetTexto(origem, 1, 43, empresa.Endereco_UF);
                SetTexto(origem, 1, 44, empresa.CNPJ);
                SetTexto(origem, 1, 46, empresa.Telefone);

                for (int indice = 0; indice < itens.Count; indice++)
                {
                    ItemDanificadoFormulario item = itens[indice];
                    int linha = indice + 1;
                    SetTexto(origem, linha, 4, item.NotaFiscalNr);
                    SetData(origem, linha, 5, item.DataEmissao);
                    SetData(origem, linha, 6, item.DataRecebimento ?? processo.DataAbertura);
                    SetTexto(origem, linha, 8, item.ItemNr);
                    SetTexto(origem, linha, 9, item.DescricaoItem);
                    SetNumero(origem, linha, 14, item.QuantidadeReclamada);
                    SetTexto(origem, linha, 15, item.VolumeNr);
                    SetTexto(origem, linha, 16, "G");
                    SetNumero(origem, linha, 17, item.PrecoUnitario.Value);
                    SetTexto(origem, linha, 21, item.DetalheDano);
                    SetTexto(origem, linha, 22, item.InstaladoVeiculo.Value ? "SIM" : "NÃO");
                    SetTexto(origem, linha, 23, item.CondicaoEmbalagem);
                }

                LimparLinhasDanificadosNaoUtilizadas(formulario, itens.Count);
                workbook.ForceFormulaRecalculation = true;
                formulario.ForceFormulaRecalculation = true;

                using (var saida = new MemoryStream())
                {
                    workbook.Write(saida, true);
                    return saida.ToArray();
                }
            }
        }

        private void RegistrarGeracao(
            int anomaliaId,
            IList<AnomaliaFormularioArquivo> arquivos,
            IList<int> itemIds,
            int itensPorFormulario,
            string tipoFormulario,
            string descricaoFormulario)
        {
            using (DbContextTransaction transacao = db.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                try
                {
                    int sequencia = db.Database.SqlQuery<int>(@"
SELECT ISNULL(MAX(NumeroSequencia), 0)
FROM dbo.AnomaliaGmArquivo WITH (UPDLOCK, HOLDLOCK)
WHERE AnomaliaId = @anomaliaId AND TipoAnomalia = @tipo AND Reenvio = 0",
                        new SqlParameter("@anomaliaId", anomaliaId),
                        new SqlParameter("@tipo", tipoFormulario)).Single();

                    for (int indice = 0; indice < arquivos.Count; indice++)
                    {
                        List<int> lote = itemIds.Skip(indice * itensPorFormulario)
                            .Take(itensPorFormulario)
                            .ToList();
                        int arquivoId = db.Database.SqlQuery<int>(@"
INSERT INTO dbo.AnomaliaGmArquivo
    (AnomaliaId, TipoAnomalia, NumeroSequencia, NomeArquivo, QuantidadeItens,
     DataGeracao, UsuarioGeracaoLogin, Reenvio, ArquivoOrigemId, FilialId, CriadoEm)
VALUES
    (@anomaliaId, @tipo, @sequencia, @nome, @quantidade,
     @agora, @usuario, 0, NULL, @filialId, @agora);
SELECT CAST(SCOPE_IDENTITY() AS int);",
                            new SqlParameter("@anomaliaId", anomaliaId),
                            new SqlParameter("@tipo", tipoFormulario),
                            new SqlParameter("@sequencia", ++sequencia),
                            new SqlParameter("@nome", arquivos[indice].NomeArquivo),
                            new SqlParameter("@quantidade", lote.Count),
                            new SqlParameter("@agora", agora),
                            new SqlParameter("@usuario", usuario),
                            new SqlParameter("@filialId", filialId)).Single();

                        foreach (int itemId in lote)
                        {
                            db.Database.ExecuteSqlCommand(@"
INSERT INTO dbo.AnomaliaGmArquivoItem (AnomaliaArquivoId, AnomaliaItemId)
VALUES (@arquivoId, @itemId)",
                                new SqlParameter("@arquivoId", arquivoId),
                                new SqlParameter("@itemId", itemId));
                        }
                    }

                    db.Database.ExecuteSqlCommand(@"
INSERT INTO dbo.AnomaliaGmHistorico
    (AnomaliaId, AnomaliaItemId, Evento, StatusAnteriorId, StatusNovoId,
     UsuarioLogin, DataHora, Observacao, FilialId)
VALUES
    (@anomaliaId, NULL, 'FORMULARIO_GERADO', NULL, NULL,
     @usuario, @agora, @observacao, @filialId)",
                        new SqlParameter("@anomaliaId", anomaliaId),
                        new SqlParameter("@usuario", usuario),
                        new SqlParameter("@agora", agora),
                        new SqlParameter("@observacao", arquivos.Count + " arquivo(s) do " + descricaoFormulario + " gerado(s)."),
                        new SqlParameter("@filialId", filialId));

                    transacao.Commit();
                }
                catch
                {
                    transacao.Rollback();
                    throw;
                }
            }
        }

        private static void LimparOrigem(ISheet sheet, int quantidadeLinhas)
        {
            for (int linha = 1; linha <= quantidadeLinhas; linha++)
            {
                for (int coluna = 0; coluna <= 46; coluna++)
                    SetVazio(sheet, linha, coluna);
            }
        }

        private static void LimparLinhasNaoUtilizadas(ISheet formulario, int quantidadeItens)
        {
            int[] colunasItem = { 6, 8, 10, 12, 15, 18, 20, 23, 26, 28, 30, 32, 34 };
            int[] colunasFinanceiras = { 3, 8, 11, 14 };

            for (int indice = quantidadeItens; indice < ItensPorFormularioAbc; indice++)
            {
                foreach (int coluna in colunasItem)
                    SetVazio(formulario, 16 + indice, coluna);
                foreach (int coluna in colunasFinanceiras)
                    SetVazio(formulario, 26 + indice, coluna);
            }
        }

        private static void LimparLinhasDanificadosNaoUtilizadas(ISheet formulario, int quantidadeItens)
        {
            int[] colunas = { 6, 8, 9, 10, 12, 15, 17, 20, 22, 25, 29, 32, 34 };
            for (int indice = quantidadeItens; indice < ItensPorFormularioDanificados; indice++)
            {
                foreach (int coluna in colunas)
                    SetVazio(formulario, 15 + indice, coluna);
            }
        }

        private static ICell ObterCelula(ISheet sheet, int linha, int coluna)
        {
            IRow row = sheet.GetRow(linha) ?? sheet.CreateRow(linha);
            return row.GetCell(coluna) ?? row.CreateCell(coluna);
        }

        private static void SetTexto(ISheet sheet, int linha, int coluna, string valor)
        {
            ICell cell = ObterCelula(sheet, linha, coluna);
            cell.SetCellType(CellType.String);
            cell.SetCellValue(valor ?? string.Empty);
        }

        private static void SetVazio(ISheet sheet, int linha, int coluna)
        {
            ObterCelula(sheet, linha, coluna).SetCellType(CellType.Blank);
        }

        private static void SetNumero(ISheet sheet, int linha, int coluna, decimal valor)
        {
            ICell cell = ObterCelula(sheet, linha, coluna);
            cell.SetCellType(CellType.Numeric);
            cell.SetCellValue(Convert.ToDouble(valor));
        }

        private static void SetData(ISheet sheet, int linha, int coluna, DateTime valor)
        {
            ICell cell = ObterCelula(sheet, linha, coluna);
            cell.SetCellType(CellType.Numeric);
            cell.SetCellValue(valor);
        }

        public class ProcessoFormulario
        {
            public int Id { get; set; }
            public string NumeroControle { get; set; }
            public DateTime DataAbertura { get; set; }
        }

        public class EmpresaFormulario
        {
            public int Id { get; set; }
            public string CodigoGM { get; set; }
            public string Nome { get; set; }
            public string CNPJ { get; set; }
            public string Endereco_Logradouro { get; set; }
            public string Endereco_Numero { get; set; }
            public string Endereco_Complemento { get; set; }
            public string Endereco_Bairro { get; set; }
            public string Endereco_Cidade { get; set; }
            public string Endereco_UF { get; set; }
            public string Endereco_CEP { get; set; }
            public string Telefone { get; set; }

            public string EnderecoCompleto
            {
                get
                {
                    return string.Join(", ", new[]
                    {
                        Endereco_Logradouro,
                        Endereco_Numero,
                        Endereco_Complemento,
                        Endereco_Bairro
                    }.Where(x => !string.IsNullOrWhiteSpace(x)));
                }
            }
        }

        public class ItemFormulario
        {
            public int Id { get; set; }
            public string TipoCodigo { get; set; }
            public string TipoDescricao { get; set; }
            public string NotaFiscalNr { get; set; }
            public DateTime DataEmissao { get; set; }
            public DateTime? DataRecebimento { get; set; }
            public string VolumeNr { get; set; }
            public string ItemNr { get; set; }
            public string DescricaoItem { get; set; }
            public decimal QuantidadeNF { get; set; }
            public decimal QuantidadeReclamada { get; set; }
            public decimal? QuantidadeRecebida { get; set; }
            public string ItemRecebidoNr { get; set; }
            public string DescricaoItemRecebido { get; set; }
            public decimal? PrecoUnitario { get; set; }
            public decimal? Imposto { get; set; }

            public string ItemRecebido
            {
                get { return TipoCodigo == "C" ? ItemRecebidoNr : ItemNr; }
            }

            public string DescricaoRecebida
            {
                get { return TipoCodigo == "C" ? DescricaoItemRecebido : DescricaoItem; }
            }

            public decimal QuantidadeEfetivamenteRecebida
            {
                get
                {
                    if (TipoCodigo == "A") return Math.Max(0, QuantidadeNF - QuantidadeReclamada);
                    if (TipoCodigo == "B") return QuantidadeRecebida ?? QuantidadeNF;
                    return QuantidadeReclamada;
                }
            }
        }

        public class ItemDanificadoFormulario
        {
            public int Id { get; set; }
            public string NotaFiscalNr { get; set; }
            public DateTime DataEmissao { get; set; }
            public DateTime? DataRecebimento { get; set; }
            public string VolumeNr { get; set; }
            public string ItemNr { get; set; }
            public string DescricaoItem { get; set; }
            public decimal QuantidadeReclamada { get; set; }
            public string DetalheDano { get; set; }
            public bool? InstaladoVeiculo { get; set; }
            public string CondicaoEmbalagem { get; set; }
            public decimal? PrecoUnitario { get; set; }
        }
    }
}
