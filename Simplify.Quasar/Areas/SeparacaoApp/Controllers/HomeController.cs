using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;
using Simplify.Quasar.Areas.SeparacaoApp.ViewModels;
using Simplify.Quasar.Custom;
using Simplify.Quasar.Models;

namespace Simplify.Quasar.Areas.SeparacaoApp.Controllers
{
    [ValidateSession]
    public class HomeController : Controller
    {
        private const int StatusNaoGerado = 1;
        private const int StatusAguardandoSeparacao = 2;
        private const int StatusEmSeparacao = 3;
        private const int StatusFinalizado = 4;
        private const int StatusNaoSeparar = 5;
        private const int StatusNaoGeradoAnalise = 7;
        private const int MaximoDiasDashboard = 15;

        private readonly Quasar_Entities db = new Quasar_Entities();

        private int filialId = Util.GetCurrentFilial();

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Dashboard(DateTime? dataInicial, DateTime? dataFinal)
        {
            return View(BuildDashboardViewModel(dataInicial, dataFinal));
        }

        private SeparacaoDashboardViewModel BuildDashboardViewModel(DateTime? dataInicial, DateTime? dataFinal)
        {
            DateTime hoje = Util.GetCurrentDateTime().Date;
            DateTime inicioPeriodo = (dataInicial ?? hoje).Date;
            DateTime fimPeriodo = (dataFinal ?? hoje).Date;

            if (inicioPeriodo > fimPeriodo)
            {
                return BuildDashboardPeriodoInvalido(
                    inicioPeriodo,
                    fimPeriodo,
                    "A Data Inicial não pode ser maior que a Data Final.");
            }

            int totalDias = (fimPeriodo - inicioPeriodo).Days + 1;
            if (totalDias > MaximoDiasDashboard)
            {
                return BuildDashboardPeriodoInvalido(
                    inicioPeriodo,
                    fimPeriodo,
                    "O período selecionado não pode ser superior a 15 dias.");
            }

            var romaneios = db.Romaneio
                .Where(r => r.FilialId == filialId)
                .Select(r => new
                {
                    r.Id,
                    r.StatusId,
                    r.SeparadorId,
                    r.ConferenteId,
                    r.Pecas,
                    r.CriadoEm,
                    r.ModificadoEm,
                    r.DataSeparador,
                    r.DataConferente
                })
                .ToList();

            Func<dynamic, DateTime?> dataReferenciaRomaneio = r => r.DataSeparador ?? r.DataConferente ?? r.ModificadoEm ?? r.CriadoEm;
            Func<dynamic, DateTime?> dataFinalizacaoRomaneio = r => r.ModificadoEm ?? r.DataConferente ?? r.DataSeparador ?? r.CriadoEm;
            Func<dynamic, DateTime?> dataProdutividadeSeparacao = r => r.DataSeparador ?? r.ModificadoEm ?? r.CriadoEm;
            Func<dynamic, DateTime?> dataProdutividadeConferencia = r => r.DataConferente ?? r.ModificadoEm ?? r.CriadoEm;

            DateTime inicioConsulta = inicioPeriodo;
            DateTime fimConsultaExclusivo = fimPeriodo.AddDays(1);

            var romaneiosDia = romaneios
                .Where(r =>
                {
                    DateTime? dataRef = dataReferenciaRomaneio(r);
                    return dataRef.HasValue && dataRef.Value >= inicioConsulta && dataRef.Value < fimConsultaExclusivo;
                })
                .ToList();

            int totalRomaneiosDia = romaneiosDia.Count;

            var statusLookup = db.StatusRomaneio
                .ToList()
                .GroupBy(x => x.Id)
                .Select(g => g.First())
                .ToDictionary(x => x.Id, x => x.Descricao);

            var cores = new Dictionary<int, string>
            {
                { StatusNaoGerado, "#75d2fa" },
                { StatusAguardandoSeparacao, "#b7f0d6" },
                { StatusEmSeparacao, "#ffd97b" },
                { StatusFinalizado, "#9fc0ff" },
                { StatusNaoSeparar, "#f8b4b4" },
                { StatusNaoGeradoAnalise, "#c7b8ff" }
            };

            var statusDia = romaneiosDia
                .GroupBy(r => r.StatusId ?? 0)
                .Select(g => new SeparacaoDashboardStatusItemViewModel
                {
                    StatusId = g.Key,
                    Status = statusLookup.ContainsKey(g.Key) ? statusLookup[g.Key] : "Sem status",
                    Quantidade = g.Count(),
                    Cor = cores.ContainsKey(g.Key) ? cores[g.Key] : "#d7dee8",
                    Percentual = totalRomaneiosDia == 0
                        ? 0m
                        : Math.Round((g.Count() * 100m) / totalRomaneiosDia, 1, MidpointRounding.AwayFromZero)
                })
                .OrderBy(x => x.StatusId)
                .ToList();

            var movimentoLookup = romaneios
                .Where(r => r.StatusId == StatusFinalizado)
                .Select(r => new
                {
                    DataReferencia = dataFinalizacaoRomaneio(r)
                })
                .Where(r => r.DataReferencia.HasValue && r.DataReferencia.Value.Date >= inicioPeriodo && r.DataReferencia.Value.Date <= fimPeriodo)
                .GroupBy(r => r.DataReferencia.Value.Date)
                .ToDictionary(g => g.Key, g => g.Count());

            var movimentoSemanal = Enumerable.Range(0, totalDias)
                .Select(indice =>
                {
                    DateTime data = inicioPeriodo.AddDays(indice);
                    return new SeparacaoDashboardSemanaItemViewModel
                    {
                        Data = data,
                        DiaSemana = data.ToString("ddd", new CultureInfo("pt-BR")),
                        Quantidade = movimentoLookup.ContainsKey(data) ? movimentoLookup[data] : 0
                    };
                })
                .ToList();

            int separadoresTrabalhando = romaneios
                .Where(r => r.StatusId == StatusEmSeparacao && r.SeparadorId.HasValue)
                .Select(r => r.SeparadorId.Value)
                .Distinct()
                .Count();

            int tarefasPendentesDia = db.RomaneioItem
                .Where(ri => ri.FilialId == filialId
                    && ri.CriadoEm.HasValue
                    && ri.CriadoEm.Value >= inicioConsulta
                    && ri.CriadoEm.Value < fimConsultaExclusivo
                    && ri.TarefaNr != null
                    && ri.TarefaNr.Trim() != "")
                .Join(
                    db.Romaneio.Where(r => r.FilialId == filialId && r.StatusId < StatusFinalizado),
                    ri => ri.RomaneioId,
                    r => r.Id,
                    (ri, r) => ri.TarefaNr)
                .Distinct()
                .Count();

            string produtividadeConfigMensagem;
            var produtividadeConfig = TryResolveProdutividadeConfig(out produtividadeConfigMensagem);

            var romaneiosProdutividade = romaneios
                .Where(r => r.SeparadorId.HasValue)
                .Where(r =>
                {
                    DateTime? dataRef = dataProdutividadeSeparacao(r);
                    return dataRef.HasValue && dataRef.Value >= inicioConsulta && dataRef.Value < fimConsultaExclusivo;
                })
                .ToList();

            var romaneiosProdutividadeConferencia = romaneios
                .Where(r => r.ConferenteId.HasValue)
                .Where(r =>
                {
                    DateTime? dataRef = dataProdutividadeConferencia(r);
                    return dataRef.HasValue && dataRef.Value >= inicioConsulta && dataRef.Value < fimConsultaExclusivo;
                })
                .ToList();

            var romaneioIdsProdutividade = romaneiosProdutividade
                .Select(r => r.Id)
                .Concat(romaneiosProdutividadeConferencia.Select(r => r.Id))
                .Distinct()
                .ToList();

            var quantidadeItensPorRomaneio = romaneioIdsProdutividade.Any()
                ? db.RomaneioItem
                    .Where(x => x.FilialId == filialId && romaneioIdsProdutividade.Contains(x.RomaneioId))
                    .GroupBy(x => x.RomaneioId)
                    .ToDictionary(g => g.Key, g => g.Count())
                : new Dictionary<int, int>();

            var userIds = romaneiosProdutividade
                .Where(r => r.SeparadorId.HasValue)
                .Select(r => r.SeparadorId.Value)
                .Concat(
                    romaneiosProdutividadeConferencia
                        .Where(r => r.ConferenteId.HasValue)
                        .Select(r => r.ConferenteId.Value))
                .Distinct()
                .ToList();

            var usuarios = userIds.Any()
                ? db.Usuario
                    .Where(u => userIds.Contains(u.Id))
                    .ToDictionary(u => u.Id, u => u.Nome)
                : new Dictionary<int, string>();

            var produtividade = produtividadeConfig != null
                ? BuildProdutividadeDashboard(
                    romaneiosProdutividade,
                    quantidadeItensPorRomaneio,
                    produtividadeConfig,
                    r => ResolveUsuarioNome(usuarios, r.SeparadorId),
                    r => r.SeparadorId ?? 0,
                    r => r.Pecas ?? 0)
                : new List<RomaneioDashboardItemViewModel>();

            var produtividadeConferencia = produtividadeConfig != null
                ? BuildProdutividadeDashboard(
                    romaneiosProdutividadeConferencia,
                    quantidadeItensPorRomaneio,
                    produtividadeConfig,
                    r => ResolveUsuarioNome(usuarios, r.ConferenteId),
                    r => r.ConferenteId ?? 0,
                    r => r.Pecas ?? 0)
                : new List<RomaneioDashboardItemViewModel>();

            return new SeparacaoDashboardViewModel
            {
                DataInicial = inicioPeriodo,
                DataFinal = fimPeriodo,
                PeriodoValido = true,
                SeparadoresTrabalhando = separadoresTrabalhando,
                TarefasPendentesDia = tarefasPendentesDia,
                RomaneiosDia = totalRomaneiosDia,
                ProdutividadeConfigValida = produtividadeConfig != null,
                ProdutividadeConfigMensagem = produtividadeConfigMensagem,
                StatusDia = statusDia,
                MovimentoSemanal = movimentoSemanal,
                Produtividade = produtividade,
                ProdutividadeConferencia = produtividadeConferencia
            };
        }

        private static SeparacaoDashboardViewModel BuildDashboardPeriodoInvalido(
            DateTime dataInicial,
            DateTime dataFinal,
            string mensagem)
        {
            return new SeparacaoDashboardViewModel
            {
                DataInicial = dataInicial,
                DataFinal = dataFinal,
                PeriodoValido = false,
                PeriodoMensagem = mensagem,
                ProdutividadeConfigValida = true,
                StatusDia = Enumerable.Empty<SeparacaoDashboardStatusItemViewModel>(),
                MovimentoSemanal = Enumerable.Empty<SeparacaoDashboardSemanaItemViewModel>(),
                Produtividade = Enumerable.Empty<RomaneioDashboardItemViewModel>(),
                ProdutividadeConferencia = Enumerable.Empty<RomaneioDashboardItemViewModel>()
            };
        }

        private List<RomaneioDashboardItemViewModel> BuildProdutividadeDashboard<T>(
            IEnumerable<T> itens,
            IDictionary<int, int> quantidadeItensPorRomaneio,
            ProdutividadeConfig config,
            Func<T, string> nomeSelector,
            Func<T, int> idSelector,
            Func<T, int> pecasSelector) where T : class
        {
            var ranking = itens
                .GroupBy(i => new
                {
                    Nome = nomeSelector(i),
                    Id = idSelector(i)
                })
                .Select(g =>
                {
                    int quantidadeRomaneios = g.Count();
                    int quantidadeLinhas = g.Sum(x =>
                    {
                        dynamic item = x;
                        int romaneioId = item.Id;
                        return quantidadeItensPorRomaneio.ContainsKey(romaneioId) ? quantidadeItensPorRomaneio[romaneioId] : 0;
                    });
                    int quantidadePecas = g.Sum(pecasSelector);

                    return new RomaneioDashboardItemViewModel
                    {
                        PickerId = g.Key.Id,
                        PickerNome = g.Key.Nome,
                        QuantidadeRomaneios = quantidadeRomaneios,
                        QuantidadeLinhas = quantidadeLinhas,
                        QuantidadePecas = quantidadePecas,
                        ProdutividadeCalculada =
                            (quantidadeRomaneios * config.PercentualRomaneios / 100m) +
                            (quantidadeLinhas * config.PercentualLinhas / 100m) +
                            (quantidadePecas * config.PercentualPecas / 100m)
                    };
                })
                .OrderByDescending(g => g.ProdutividadeCalculada)
                .ThenBy(g => g.PickerNome)
                .ToList();

            if (!ranking.Any())
            {
                return ranking;
            }

            decimal totalGeral = ranking.Sum(x => x.ProdutividadeCalculada);
            if (totalGeral <= 0m)
            {
                ranking.ForEach(x => x.ProdutividadeCalculada = 0m);
                return ranking;
            }

            ranking.ForEach(x =>
            {
                x.ProdutividadeCalculada = Math.Round((x.ProdutividadeCalculada / totalGeral) * 100m, 1, MidpointRounding.AwayFromZero);
            });

            decimal diferenca = 100m - ranking.Sum(x => x.ProdutividadeCalculada);
            if (diferenca != 0m)
            {
                ranking[0].ProdutividadeCalculada += diferenca;
            }

            return ranking;
        }

        private ProdutividadeConfig TryResolveProdutividadeConfig(out string mensagem)
        {
            mensagem = null;
            var configs = db.AppConfig
                .Where(x => x.FilialId == filialId &&
                    (x.Nome == "Romaneios" || x.Nome == "Linhas" || x.Nome == "Peças" || x.Nome == "Pecas"))
                .ToList();

            decimal percentualRomaneios;
            decimal percentualLinhas;
            decimal percentualPecas;

            if (!TryGetProdutividadePercentual(configs, "Romaneios", out percentualRomaneios))
            {
                mensagem = "Configuração inválida de produtividade: percentual de Romaneios não localizado.";
                return null;
            }

            if (!TryGetProdutividadePercentual(configs, "Linhas", out percentualLinhas))
            {
                mensagem = "Configuração inválida de produtividade: percentual de Linhas não localizado.";
                return null;
            }

            if (!TryGetProdutividadePercentual(configs, "Peças", out percentualPecas))
            {
                mensagem = "Configuração inválida de produtividade: percentual de Peças não localizado.";
                return null;
            }

            decimal total = percentualRomaneios + percentualLinhas + percentualPecas;
            if (Math.Abs(total - 100m) > 0.01m)
            {
                mensagem = "Configuração inválida de produtividade: Romaneios + Linhas + Peças deve totalizar 100%.";
                return null;
            }

            return new ProdutividadeConfig
            {
                PercentualRomaneios = percentualRomaneios,
                PercentualLinhas = percentualLinhas,
                PercentualPecas = percentualPecas
            };
        }

        private bool TryGetProdutividadePercentual(IEnumerable<AppConfig> configs, string indicador, out decimal percentual)
        {
            percentual = 0m;
            string indicadorNormalizado = NormalizeIndicadorNome(indicador);
            string valor = configs
                .Where(x => NormalizeIndicadorNome(x.Nome) == indicadorNormalizado)
                .Select(x => x.Valor)
                .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(valor))
            {
                return false;
            }

            string valorNormalizado = valor.Replace("%", string.Empty).Trim();
            return decimal.TryParse(valorNormalizado, NumberStyles.Number, new CultureInfo("pt-BR"), out percentual)
                || decimal.TryParse(valorNormalizado, NumberStyles.Number, CultureInfo.InvariantCulture, out percentual);
        }

        private static string NormalizeIndicadorNome(string nome)
        {
            return Util.RemoverAcentuacao(nome ?? string.Empty)
                .Trim()
                .ToUpperInvariant();
        }

        private static string ResolveUsuarioNome(IDictionary<int, string> usuarios, int? usuarioId)
        {
            if (!usuarioId.HasValue)
            {
                return string.Empty;
            }

            return usuarios.ContainsKey(usuarioId.Value)
                ? usuarios[usuarioId.Value]
                : "Usuário " + usuarioId.Value.ToString(CultureInfo.InvariantCulture);
        }

        private sealed class ProdutividadeConfig
        {
            public decimal PercentualRomaneios { get; set; }
            public decimal PercentualLinhas { get; set; }
            public decimal PercentualPecas { get; set; }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
