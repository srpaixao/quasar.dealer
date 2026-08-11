using Simplify.Quasar.Areas.RecebimentoApp.ViewModels;
using Simplify.Quasar.Custom;
using Simplify.Quasar.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace Simplify.Quasar.Areas.RecebimentoApp.Controllers
{
    [ValidateSession]
    public class VolumesController : Controller
    {
        private readonly Quasar_Entities db = new Quasar_Entities();
        private int filialId
        {
            get { return Util.GetCurrentFilial(); }
        }

        public ActionResult Index(string volumeNr)
        {
            return View(BuildConsultaViewModel(volumeNr));
        }

        public ActionResult Consulta(string volumeNr)
        {
            return View("Index", BuildConsultaViewModel(volumeNr));
        }

        public ActionResult NotaFiscal(string notaFiscalNr)
        {
            return View(BuildNotaFiscalConsultaViewModel(notaFiscalNr));
        }

        public ActionResult Item(string itemNr)
        {
            return View(BuildItemConsultaViewModel(itemNr));
        }

        private VolumeConsultaViewModel BuildConsultaViewModel(string volumeNr)
        {
            var vm = new VolumeConsultaViewModel
            {
                VolumeNr = (volumeNr ?? string.Empty).Trim(),
                Header = null,
                Itens = new List<VolumeConsultaItemViewModel>()
            };

            if (string.IsNullOrWhiteSpace(vm.VolumeNr))
            {
                return vm;
            }

            vm.ConsultaRealizada = true;

            var query = (from item in db.NotaFiscalItem
                         join nota in db.NotaFiscal on item.NotaFiscalId equals nota.Id
                         where item.FilialId == filialId
                            && nota.FilialId == filialId
                            && (item.Volume ?? string.Empty).Trim() == vm.VolumeNr
                         select new
                         {
                             Item = item,
                             Nota = nota
                         })
                        .ToList();

            if (!query.Any())
            {
                vm.ConsultaMensagem = "Volume não localizado.";
                return vm;
            }

            var materialMap = db.Material
                .Where(x => x.FilialId == filialId || x.FilialId == null)
                .ToList()
                .GroupBy(x => (x.Codigo ?? string.Empty).Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    x => x.Key,
                    x => x.OrderByDescending(y => y.FilialId == filialId).Select(y => y.Descricao).FirstOrDefault() ?? string.Empty,
                    StringComparer.OrdinalIgnoreCase);

            var statusMap = db.StatusNotaFiscal
                .AsNoTracking()
                .ToList()
                .GroupBy(x => x.Id)
                .ToDictionary(
                    x => x.Key,
                    x => x.Select(y => !string.IsNullOrWhiteSpace(y.Descricao) ? y.Descricao : y.Nome).FirstOrDefault() ?? string.Empty);

            var origemMap = BuildOrigemNotaFiscalMap();

            var headerSource = query
                .OrderByDescending(x => x.Nota.ModificadoEm ?? x.Nota.CriadoEm)
                .ThenByDescending(x => x.Nota.Id)
                .First();

            vm.Header = new VolumeConsultaHeaderViewModel
            {
                VolumeNr = vm.VolumeNr,
                NotaFiscal = headerSource.Nota.Numero ?? string.Empty,
                Serie = headerSource.Nota.Serie ?? string.Empty,
                Emissor = ResolveOrigemDescricao(origemMap, headerSource.Nota.Emissor, headerSource.Nota.Emissor),
                StatusNotaFiscal = ResolveStatusDescricao(statusMap, headerSource.Nota.StatusId),
                DataEmissao = headerSource.Nota.DataEmissao,
                ValorNotaFiscal = headerSource.Nota.Valor,
                Movimento = headerSource.Nota.Movimento ?? string.Empty,
                QuantidadeItens = query.Count,
                QuantidadePecas = query.Sum(x => x.Item.Quantidade),
                CriadoEm = headerSource.Nota.CriadoEm,
                CriadoPor = headerSource.Nota.CriadoPor ?? string.Empty,
                ModificadoEm = headerSource.Nota.ModificadoEm,
                ModificadoPor = headerSource.Nota.ModificadoPor ?? string.Empty
            };

            vm.Itens = query
                .OrderBy(x => x.Nota.Numero)
                .ThenBy(x => x.Item.Item)
                .ThenBy(x => x.Item.Id)
                .Select(x => new VolumeConsultaItemViewModel
                {
                    NotaFiscal = x.Nota.Numero ?? string.Empty,
                    ItemNr = x.Item.Item ?? string.Empty,
                    Descricao = ResolveMaterialDescricao(materialMap, x.Item.Item),
                    Quantidade = x.Item.Quantidade,
                    QtdConferida = x.Item.QtdConferida,
                    QtdArmazenada = x.Item.QtdArmazenada,
                    Diferenca = x.Item.QtdConferida.HasValue ? x.Item.QtdConferida.Value - x.Item.Quantidade : (decimal?)null,
                    SituacaoConferencia = ResolveSituacaoConferencia(x.Item.Conferido, x.Item.QtdConferida, x.Item.Quantidade),
                    UsuarioConferencia = x.Item.UsuarioConferencia ?? string.Empty,
                    DtHrConferencia = x.Item.DtHrConferencia,
                    UsuarioArmazenagem = x.Item.UsuarioArmazenagem ?? string.Empty,
                    DtHrArmazenagem = x.Item.DtHrArmazenagem,
                    Pedido = x.Item.Pedido ?? string.Empty,
                    StatusItem = ResolveStatusDescricao(statusMap, x.Item.StatusId),
                    Observacao = x.Item.Observacao ?? string.Empty,
                    ModificadoEm = x.Item.ModificadoEm,
                    ModificadoPor = x.Item.ModificadoPor ?? string.Empty
                })
                .ToList();

            return vm;
        }

        private NotaFiscalConsultaViewModel BuildNotaFiscalConsultaViewModel(string notaFiscalNr)
        {
            var vm = new NotaFiscalConsultaViewModel
            {
                NotaFiscalNr = (notaFiscalNr ?? string.Empty).Trim(),
                Header = null,
                Itens = new List<NotaFiscalConsultaItemViewModel>()
            };

            if (string.IsNullOrWhiteSpace(vm.NotaFiscalNr))
            {
                return vm;
            }

            vm.ConsultaRealizada = true;

            var nota = db.NotaFiscal
                .FirstOrDefault(x => x.FilialId == filialId && (x.Numero ?? string.Empty).Trim() == vm.NotaFiscalNr);

            if (nota == null)
            {
                vm.ConsultaMensagem = "Nota Fiscal não localizada.";
                return vm;
            }

            var itens = db.NotaFiscalItem
                .Where(x => x.FilialId == filialId && x.NotaFiscalId == nota.Id)
                .OrderBy(x => x.Item)
                .ThenBy(x => x.Id)
                .ToList();

            var materialMap = db.Material
                .Where(x => x.FilialId == filialId || x.FilialId == null)
                .ToList()
                .GroupBy(x => (x.Codigo ?? string.Empty).Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    x => x.Key,
                    x => x.OrderByDescending(y => y.FilialId == filialId).Select(y => y.Descricao).FirstOrDefault() ?? string.Empty,
                    StringComparer.OrdinalIgnoreCase);

            var statusMap = db.StatusNotaFiscal
                .AsNoTracking()
                .ToList()
                .GroupBy(x => x.Id)
                .ToDictionary(
                    x => x.Key,
                    x => x.Select(y => !string.IsNullOrWhiteSpace(y.Descricao) ? y.Descricao : y.Nome).FirstOrDefault() ?? string.Empty);

            var origemMap = BuildOrigemNotaFiscalMap();

            vm.Header = new NotaFiscalConsultaHeaderViewModel
            {
                NotaFiscal = nota.Numero ?? string.Empty,
                Emissor = ResolveOrigemDescricao(origemMap, nota.Emissor, nota.Emissor),
                StatusNotaFiscal = ResolveStatusDescricao(statusMap, nota.StatusId),
                DataEmissao = nota.DataEmissao,
                ValorNotaFiscal = nota.Valor,
                Movimento = nota.Movimento ?? string.Empty,
                QuantidadeItens = itens.Count,
                QuantidadePecas = itens.Sum(x => x.Quantidade),
                CriadoEm = nota.CriadoEm,
                CriadoPor = nota.CriadoPor ?? string.Empty,
                ModificadoEm = nota.ModificadoEm,
                ModificadoPor = nota.ModificadoPor ?? string.Empty
            };

            vm.Itens = itens.Select(x => new NotaFiscalConsultaItemViewModel
            {
                VolumeNr = x.Volume ?? string.Empty,
                ItemNr = x.Item ?? string.Empty,
                Descricao = ResolveMaterialDescricao(materialMap, x.Item),
                Quantidade = x.Quantidade,
                QtdConferida = x.QtdConferida,
                QtdArmazenada = x.QtdArmazenada,
                Diferenca = x.QtdConferida.HasValue ? x.QtdConferida.Value - x.Quantidade : (decimal?)null,
                SituacaoConferencia = ResolveSituacaoConferencia(x.Conferido, x.QtdConferida, x.Quantidade),
                UsuarioConferencia = x.UsuarioConferencia ?? string.Empty,
                DtHrConferencia = x.DtHrConferencia,
                UsuarioArmazenagem = x.UsuarioArmazenagem ?? string.Empty,
                DtHrArmazenagem = x.DtHrArmazenagem,
                Pedido = x.Pedido ?? string.Empty,
                StatusItem = ResolveStatusDescricao(statusMap, x.StatusId),
                Observacao = x.Observacao ?? string.Empty,
                ModificadoEm = x.ModificadoEm,
                ModificadoPor = x.ModificadoPor ?? string.Empty
            }).ToList();

            return vm;
        }

        private ItemConsultaViewModel BuildItemConsultaViewModel(string itemNr)
        {
            var vm = new ItemConsultaViewModel
            {
                ItemNr = (itemNr ?? string.Empty).Trim(),
                Header = null,
                Itens = new List<ItemConsultaItemViewModel>()
            };

            if (string.IsNullOrWhiteSpace(vm.ItemNr))
            {
                return vm;
            }

            vm.ConsultaRealizada = true;

            var material = db.Material
                .Where(x => (x.FilialId == filialId || x.FilialId == null) && (x.Codigo ?? string.Empty).Trim() == vm.ItemNr)
                .OrderByDescending(x => x.FilialId == filialId)
                .FirstOrDefault();

            var query = (from item in db.NotaFiscalItem
                         join nota in db.NotaFiscal on item.NotaFiscalId equals nota.Id
                         where item.FilialId == filialId
                            && nota.FilialId == filialId
                            && (item.Item ?? string.Empty).Trim() == vm.ItemNr
                         select new
                         {
                             Item = item,
                             Nota = nota
                         })
                        .ToList();

            if (material == null && !query.Any())
            {
                vm.ConsultaMensagem = "Item n&atilde;o localizado.";
                return vm;
            }

            var statusMap = db.StatusNotaFiscal
                .AsNoTracking()
                .ToList()
                .GroupBy(x => x.Id)
                .ToDictionary(
                    x => x.Key,
                    x => x.Select(y => !string.IsNullOrWhiteSpace(y.Descricao) ? y.Descricao : y.Nome).FirstOrDefault() ?? string.Empty);

            var origemMap = BuildOrigemNotaFiscalMap();

            vm.Header = new ItemConsultaHeaderViewModel
            {
                ItemNr = vm.ItemNr,
                Descricao = material == null ? string.Empty : (material.Descricao ?? string.Empty),
                QuantidadeNotasFiscais = query.Select(x => x.Nota.Id).Distinct().Count(),
                QuantidadeVolumes = query.Select(x => (x.Item.Volume ?? string.Empty).Trim()).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().Count(),
                QuantidadePecas = query.Sum(x => x.Item.Quantidade),
                UltimaMovimentacao = query
                    .Select(x => x.Item.ModificadoEm ?? x.Item.CriadoEm ?? x.Nota.ModificadoEm ?? x.Nota.CriadoEm)
                    .OrderByDescending(x => x)
                    .FirstOrDefault()
            };

            vm.Itens = query
                .OrderByDescending(x => x.Item.ModificadoEm ?? x.Item.CriadoEm ?? x.Nota.ModificadoEm ?? x.Nota.CriadoEm)
                .ThenByDescending(x => x.Nota.Id)
                .ThenBy(x => x.Item.Id)
                .Select(x => new ItemConsultaItemViewModel
                {
                    NotaFiscal = x.Nota.Numero ?? string.Empty,
                    VolumeNr = x.Item.Volume ?? string.Empty,
                    Quantidade = x.Item.Quantidade,
                    QtdConferida = x.Item.QtdConferida,
                    QtdArmazenada = x.Item.QtdArmazenada,
                    Diferenca = x.Item.QtdConferida.HasValue ? x.Item.QtdConferida.Value - x.Item.Quantidade : (decimal?)null,
                    SituacaoConferencia = ResolveSituacaoConferencia(x.Item.Conferido, x.Item.QtdConferida, x.Item.Quantidade),
                    UsuarioConferencia = x.Item.UsuarioConferencia ?? string.Empty,
                    DtHrConferencia = x.Item.DtHrConferencia,
                    UsuarioArmazenagem = x.Item.UsuarioArmazenagem ?? string.Empty,
                    DtHrArmazenagem = x.Item.DtHrArmazenagem,
                    Pedido = x.Item.Pedido ?? string.Empty,
                    Emissor = ResolveOrigemDescricao(origemMap, x.Nota.Emissor, x.Nota.Emissor),
                    StatusItem = ResolveStatusDescricao(statusMap, x.Item.StatusId),
                    Observacao = x.Item.Observacao ?? string.Empty,
                    DataEmissao = x.Nota.DataEmissao,
                    ModificadoEm = x.Item.ModificadoEm,
                    ModificadoPor = x.Item.ModificadoPor ?? string.Empty
                })
                .ToList();

            return vm;
        }

        private Dictionary<string, string> BuildOrigemNotaFiscalMap()
        {
            return db.OrigemNotaFiscal
                .AsNoTracking()
                .Where(x => x.FilialId == filialId || x.FilialId == null)
                .ToList()
                .GroupBy(x => (x.Codigo ?? string.Empty).Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    x => x.Key,
                    x => x.OrderByDescending(y => y.FilialId == filialId).Select(y => y.Descricao).FirstOrDefault() ?? string.Empty,
                    StringComparer.OrdinalIgnoreCase);
        }

        private static string ResolveMaterialDescricao(IDictionary<string, string> materialMap, string codigo)
        {
            string chave = (codigo ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(chave))
            {
                return string.Empty;
            }

            string descricao;
            return materialMap.TryGetValue(chave, out descricao) ? descricao : string.Empty;
        }

        private static string ResolveOrigemDescricao(IDictionary<string, string> origemMap, string codigoOrigem, string fallback)
        {
            string chave = (codigoOrigem ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(chave))
            {
                string descricao;
                if (origemMap.TryGetValue(chave, out descricao) && !string.IsNullOrWhiteSpace(descricao))
                {
                    return descricao;
                }
            }

            return fallback ?? string.Empty;
        }

        private static string ResolveStatusDescricao(IDictionary<int, string> statusMap, int? statusId)
        {
            if (!statusId.HasValue)
            {
                return string.Empty;
            }

            string descricao;
            return statusMap.TryGetValue(statusId.Value, out descricao) ? descricao : statusId.Value.ToString();
        }

        private static string ResolveSituacaoConferencia(bool conferido, decimal? qtdConferida, decimal quantidade)
        {
            if (!conferido)
            {
                return "Pendente";
            }

            if (!qtdConferida.HasValue || qtdConferida.Value == quantidade)
            {
                return "Conferido";
            }

            return qtdConferida.Value < quantidade ? "Conferido a menor" : "Conferido a maior";
        }
    }
}
