using Simplify.Quasar.Areas.EstoqueApp.ViewModels;
using Simplify.Quasar.Custom;
using Simplify.Quasar.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace Simplify.Quasar.Areas.EstoqueApp.Controllers
{
    [ValidateSession]
    public class HistoricoController : Controller
    {
        private readonly Quasar_Entities db = new Quasar_Entities();

        private int filialId
        {
            get { return Util.GetCurrentFilial(); }
        }

        public ActionResult Index(string itemNr)
        {
            return View(BuildViewModel(itemNr));
        }

        private HistoricoItemViewModel BuildViewModel(string itemNr)
        {
            string itemNrNormalizado = (itemNr ?? string.Empty).Trim().ToUpperInvariant();
            var vm = new HistoricoItemViewModel
            {
                ItemNr = itemNrNormalizado
            };

            if (string.IsNullOrWhiteSpace(itemNrNormalizado))
            {
                return vm;
            }

            vm.ConsultaRealizada = true;

            Material material = db.Material
                .Where(x => (x.FilialId == filialId || x.FilialId == null) && x.Codigo == itemNrNormalizado)
                .OrderByDescending(x => x.FilialId == filialId)
                .FirstOrDefault();

            Estoque estoque = db.Estoque
                .Where(x => x.FilialId == filialId && x.ItemNr == itemNrNormalizado)
                .OrderByDescending(x => x.ModificadoEm ?? x.CriadoEm)
                .ThenByDescending(x => x.Id)
                .FirstOrDefault();

            if (material == null && estoque == null)
            {
                vm.ConsultaMensagem = "Item n\u00E3o localizado.";
                return vm;
            }

            Locacao locacao = null;
            if (estoque != null && !string.IsNullOrWhiteSpace(estoque.Locacao))
            {
                string locacaoCodigo = estoque.Locacao.Trim();
                locacao = db.Locacao
                    .Where(x => (x.FilialId == filialId || x.FilialId == null) && x.Codigo == locacaoCodigo)
                    .OrderByDescending(x => x.FilialId == filialId)
                    .FirstOrDefault();
            }

            vm.Cabecalho = new HistoricoItemCabecalhoViewModel
            {
                ItemNr = itemNrNormalizado,
                Descricao = material == null ? string.Empty : (material.Descricao ?? string.Empty),
                LocacaoCodigo = estoque == null ? string.Empty : (estoque.Locacao ?? string.Empty),
                LocacaoDescricao = locacao == null ? string.Empty : (locacao.Descricao ?? string.Empty),
                Saldo = estoque == null ? (int?)null : estoque.Saldo,
                Indisponivel = estoque == null ? (int?)null : estoque.Indisponivel
            };

            var historicos = new List<HistoricoItemLinhaViewModel>();

            db.Configuration.AutoDetectChangesEnabled = false;
            db.Configuration.LazyLoadingEnabled = false;
            db.Configuration.ProxyCreationEnabled = false;

            var tipoNotaFiscalMap = db.TipoNotaFiscal
                .AsNoTracking()
                .Where(x => x.FilialId == filialId || x.FilialId == null)
                .ToList()
                .GroupBy(x => x.Id)
                .ToDictionary(
                    x => x.Key,
                    x => x.OrderByDescending(y => y.FilialId == filialId).Select(y => y.Descricao).FirstOrDefault() ?? string.Empty);

            var statusNotaFiscalMap = BuildDescricaoMap(
                db.StatusNotaFiscal
                    .AsNoTracking()
                    .ToList()
                    .Select(x => new KeyValuePair<int, string>(x.Id, !string.IsNullOrWhiteSpace(x.Descricao) ? x.Descricao : x.Nome)));

            var statusDevolucaoMap = BuildDescricaoMap(
                db.StatusDevolucao
                    .AsNoTracking()
                    .ToList()
                    .Select(x => new KeyValuePair<int, string>(x.Id, x.Nome ?? string.Empty)));

            var statusRetornoMap = BuildDescricaoMap(
                db.StatusRetorno
                    .AsNoTracking()
                    .ToList()
                    .Select(x => new KeyValuePair<int, string>(x.Id, !string.IsNullOrWhiteSpace(x.Descricao) ? x.Descricao : x.Nome)));

            var statusRomaneioMap = BuildDescricaoMap(
                db.StatusRomaneio
                    .AsNoTracking()
                    .ToList()
                    .Select(x => new KeyValuePair<int, string>(x.Id, x.Descricao ?? string.Empty)));

            var notaFiscalItens = (from item in db.NotaFiscalItem
                                   join nota in db.NotaFiscal on item.NotaFiscalId equals nota.Id
                                   where item.FilialId == filialId
                                      && nota.FilialId == filialId
                                      && item.Item == itemNrNormalizado
                                   select new
                                   {
                                       Item = item,
                                       Nota = nota
                                   }).ToList();

            foreach (var entry in notaFiscalItens.Where(x => x.Nota.TipoId != 2))
            {
                historicos.Add(new HistoricoItemLinhaViewModel
                {
                    Data = entry.Item.ModificadoEm ?? entry.Item.CriadoEm ?? entry.Nota.ModificadoEm ?? entry.Nota.CriadoEm,
                    Processo = ResolveTipoNotaFiscal(tipoNotaFiscalMap, entry.Nota.TipoId),
                    DocumentoNr = entry.Nota.Numero ?? string.Empty,
                    DocumentoUrl = Url.Action("NotaFiscal", "Volumes", new { area = "RecebimentoApp", notaFiscalNr = entry.Nota.Numero }),
                    TipoMovimento = ResolveTipoMovimento(entry.Nota.TipoId, entry.Nota.Movimento),
                    Quantidade = entry.Item.Quantidade,
                    Status = ResolveDescricao(statusNotaFiscalMap, entry.Item.StatusId),
                    Usuario = !string.IsNullOrWhiteSpace(entry.Item.ModificadoPor) ? entry.Item.ModificadoPor : (entry.Item.CriadoPor ?? string.Empty),
                    Observacao = entry.Item.Observacao ?? string.Empty
                });
            }

            var devolucaoItens = (from item in db.DevolucaoItem
                                  join devolucao in db.Devolucao on item.DevolucaoId equals devolucao.Id
                                  where item.ItemNr == itemNrNormalizado
                                     && devolucao.FilialId == filialId
                                  select new
                                  {
                                      Item = item,
                                      Devolucao = devolucao
                                  }).ToList();

            foreach (var entry in devolucaoItens)
            {
                historicos.Add(new HistoricoItemLinhaViewModel
                {
                    Data = entry.Item.ModificadoEm ?? entry.Item.CriadoEm ?? entry.Devolucao.ModificadoEm ?? entry.Devolucao.CriadoEm,
                    Processo = "Devolu\u00E7\u00E3o",
                    DocumentoNr = entry.Devolucao.DevolucaoNr ?? string.Empty,
                    DocumentoUrl = Url.Action("Detalhe", "Home", new { area = "DevolucaoApp", id = entry.Devolucao.Id }),
                    TipoMovimento = string.IsNullOrWhiteSpace(entry.Devolucao.Movimento) ? "Devolu\u00E7\u00E3o" : entry.Devolucao.Movimento,
                    Quantidade = Convert.ToDecimal(entry.Item.Quantidade ?? 0),
                    Status = ResolveDescricao(statusDevolucaoMap, entry.Item.StatusId),
                    Usuario = !string.IsNullOrWhiteSpace(entry.Item.ModificadoPor) ? entry.Item.ModificadoPor : (entry.Item.CriadoPor ?? string.Empty),
                    Observacao = entry.Item.Observacao ?? string.Empty
                });
            }

            var retornoItens = (from item in db.RetornoInternoItem
                                join retorno in db.RetornoInterno on item.RetornoInternoId equals retorno.Id
                                where retorno.FilialId == filialId
                                   && item.ItemNr == itemNrNormalizado
                                select new
                                {
                                    Item = item,
                                    Retorno = retorno
                                }).ToList();

            foreach (var entry in retornoItens)
            {
                historicos.Add(new HistoricoItemLinhaViewModel
                {
                    Data = entry.Item.ModificadoEm ?? entry.Item.CriadoEm ?? entry.Retorno.ModificadoEm ?? entry.Retorno.CriadoEm,
                    Processo = "Retorno Interno",
                    DocumentoNr = entry.Retorno.NrDocumento ?? string.Empty,
                    DocumentoUrl = Url.Action("Edit", "RetornoInterno", new { area = "RecebimentoApp", id = entry.Retorno.Id }),
                    TipoMovimento = "Retorno",
                    Quantidade = entry.Item.Quantidade ?? 0,
                    Status = ResolveDescricao(statusRetornoMap, entry.Item.StatusRetornoId),
                    Usuario = !string.IsNullOrWhiteSpace(entry.Item.ModificadoPor) ? entry.Item.ModificadoPor : (entry.Item.CriadoPor ?? string.Empty),
                    Observacao = entry.Retorno.Observacoes ?? string.Empty
                });
            }

            var romaneioItens = (from item in db.RomaneioItem
                                 join romaneio in db.Romaneio on item.RomaneioId equals romaneio.Id
                                 where item.FilialId == filialId
                                    && romaneio.FilialId == filialId
                                    && item.ItemNr == itemNrNormalizado
                                 select new
                                 {
                                     Item = item,
                                     Romaneio = romaneio
                                 }).ToList();

            foreach (var entry in romaneioItens)
            {
                historicos.Add(new HistoricoItemLinhaViewModel
                {
                    Data = entry.Item.CriadoEm ?? entry.Romaneio.ModificadoEm ?? entry.Romaneio.CriadoEm,
                    Processo = "Romaneio",
                    DocumentoNr = entry.Romaneio.RomaneioNr ?? string.Empty,
                    DocumentoUrl = Url.Action("Consulta", "Romaneio", new { area = "SeparacaoApp", romaneioNr = entry.Romaneio.RomaneioNr }),
                    TipoMovimento = "Sa\u00EDda",
                    Quantidade = Convert.ToDecimal(entry.Item.Qtde ?? 0),
                    Status = ResolveDescricao(statusRomaneioMap, entry.Romaneio.StatusId),
                    Usuario = entry.Item.CriadoPor ?? entry.Romaneio.ModificadoPor ?? entry.Romaneio.CriadoPor ?? string.Empty,
                    Observacao = string.Empty
                });
            }

            var historicoRecebimento = db.HistoricoRecebimento
                .AsNoTracking()
                .Where(x => x.FilialId == filialId && x.CodMaterial == itemNrNormalizado)
                .OrderByDescending(x => x.DataHora)
                .Take(500)
                .ToList();

            foreach (var entry in historicoRecebimento)
            {
                historicos.Add(new HistoricoItemLinhaViewModel
                {
                    Data = entry.DataHora,
                    Processo = "Hist\u00F3rico Recebimento",
                    DocumentoNr = entry.NroVolume ?? string.Empty,
                    DocumentoUrl = string.IsNullOrWhiteSpace(entry.NroVolume)
                        ? string.Empty
                        : Url.Action("Index", "Volumes", new { area = "RecebimentoApp", volumeNr = entry.NroVolume }),
                    TipoMovimento = "Entrada",
                    Quantidade = entry.Quantidade ?? 0,
                    Status = "Conclu\u00EDdo",
                    Usuario = entry.Usuario ?? string.Empty,
                    Observacao = entry.CodLocacao ?? string.Empty
                });
            }

            var movimentacoes = db.Movimentacao
                .AsNoTracking()
                .Where(x => x.FilialId == filialId && x.ItemNr == itemNrNormalizado)
                .OrderByDescending(x => x.FinalizadoEm ?? x.CriadoEm)
                .Take(500)
                .ToList();

            foreach (var entry in movimentacoes)
            {
                historicos.Add(new HistoricoItemLinhaViewModel
                {
                    Data = entry.FinalizadoEm ?? entry.CriadoEm,
                    Processo = "Movimenta\u00E7\u00E3o de Estoque",
                    DocumentoNr = entry.Id.ToString(),
                    DocumentoUrl = string.Empty,
                    TipoMovimento = string.Concat(
                        "Origem: ", entry.LocacaoOrigem ?? "-",
                        " / Destino: ", entry.LocacaoDestino ?? "-"),
                    Quantidade = Convert.ToDecimal(entry.QtdDestino ?? entry.QtdOrigem ?? 0),
                    Status = entry.FinalizadoEm.HasValue ? "Finalizada" : "Pendente",
                    Usuario = entry.FinalizadoPor ?? entry.CriadoPor ?? string.Empty,
                    Observacao = entry.Payload ?? string.Empty
                });
            }

            vm.Historicos = historicos
                .OrderByDescending(x => x.Data ?? DateTime.MinValue)
                .ThenBy(x => x.Processo ?? string.Empty)
                .ThenBy(x => x.DocumentoNr ?? string.Empty)
                .ToList();

            return vm;
        }

        private static Dictionary<int, string> BuildDescricaoMap(IEnumerable<KeyValuePair<int, string>> source)
        {
            return source
                .GroupBy(x => x.Key)
                .ToDictionary(x => x.Key, x => x.Select(y => y.Value).FirstOrDefault() ?? string.Empty);
        }

        private static string ResolveDescricao(IDictionary<int, string> source, int? id)
        {
            if (!id.HasValue)
            {
                return string.Empty;
            }

            string value;
            return source.TryGetValue(id.Value, out value) ? value : string.Empty;
        }

        private static string ResolveTipoNotaFiscal(IDictionary<int, string> source, int id)
        {
            string value;
            return source.TryGetValue(id, out value) && !string.IsNullOrWhiteSpace(value)
                ? value
                : "Nota Fiscal";
        }

        private static string ResolveTipoMovimento(int tipoId, string movimento)
        {
            if (tipoId == 3)
            {
                return "Transfer\u00EAncia";
            }

            if (tipoId == 4)
            {
                return "Entrada";
            }

            if (!string.IsNullOrWhiteSpace(movimento))
            {
                if (string.Equals(movimento, "E", StringComparison.OrdinalIgnoreCase))
                {
                    return "Entrada";
                }

                if (string.Equals(movimento, "S", StringComparison.OrdinalIgnoreCase))
                {
                    return "Sa\u00EDda";
                }
            }

            return "Recebimento";
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
