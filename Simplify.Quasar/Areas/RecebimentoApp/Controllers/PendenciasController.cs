using System;
using System.Linq;
using System.Web.Mvc;
using System.Data;
using System.Collections.Generic;

using Simplify.Quasar.Models;
using Simplify.Quasar.Areas.RecebimentoApp.ViewModels;
using Simplify.Quasar.Custom;
using System.Data.Entity;
using System.IO;
using Newtonsoft.Json;

namespace Simplify.Quasar.Areas.RecebimentoApp.Controllers
{
    [ValidateSession]
    public class PendenciasController : Controller
    {
        Quasar_Entities db = new Quasar_Entities();

        int filialId = Util.GetCurrentFilial();

        int periodo;
        DateTime inicio;
        public PendenciasController()
        {
            periodo = Util.GetPeriodoRecebimento();
            inicio = Util.GetCurrentDateTime().AddDays(-periodo);
        }

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult ConferenciaVolume(string volumeNr)
        {
            return View(BuildConferenciaVolumeViewModel(volumeNr));
        }

        [HttpPost]
        public ActionResult GetItens(int? areaId)
        {
            DataTableAjaxPostModel model = ReadDataTableModel();
            if (model == null) return EmptyDataTableResult();

            var query = from nf in db.NotaFiscalItem.AsNoTracking()
                        where nf.StatusId < 7 && nf.FilialId == filialId && nf.CriadoEm >= inicio
                        select new PendenciasViewModel
                        {
                            NFId = nf.Id,
                            ItemNr = nf.Item,
                            Quantidade = nf.Quantidade,
                            VolumeNr = nf.Volume,
                            Usuario = nf.CriadoPor,
                            DtHr = nf.CriadoEm.Value,
                            Status = (from sv in db.StatusNotaFiscal where sv.Id == nf.StatusId select sv.Nome).FirstOrDefault(),
                            Descricao = (from s in db.Material where s.Codigo == nf.Item select s.Descricao).FirstOrDefault(),
                            Locacao = (from i in db.Estoque where i.FilialId == filialId && i.ItemNr == nf.Item select i.Locacao).FirstOrDefault()
                        };

            int recordsTotal = query.Count();
            string termo = GetSearchValue(model);
            if (!string.IsNullOrWhiteSpace(termo))
            {
                query = query.Where(x =>
                    (x.ItemNr ?? string.Empty).Contains(termo) ||
                    (x.Descricao ?? string.Empty).Contains(termo) ||
                    (x.Locacao ?? string.Empty).Contains(termo) ||
                    (x.Status ?? string.Empty).Contains(termo) ||
                    (x.VolumeNr ?? string.Empty).Contains(termo) ||
                    (x.Usuario ?? string.Empty).Contains(termo));
            }

            int recordsFiltered = query.Count();
            string sortField = GetSortField(model);
            bool desc = IsDescending(model);
            switch (sortField)
            {
                case "Descricao": query = desc ? query.OrderByDescending(x => x.Descricao) : query.OrderBy(x => x.Descricao); break;
                case "Locacao": query = desc ? query.OrderByDescending(x => x.Locacao) : query.OrderBy(x => x.Locacao); break;
                case "Status": query = desc ? query.OrderByDescending(x => x.Status) : query.OrderBy(x => x.Status); break;
                case "VolumeNr": query = desc ? query.OrderByDescending(x => x.VolumeNr) : query.OrderBy(x => x.VolumeNr); break;
                case "Quantidade": query = desc ? query.OrderByDescending(x => x.Quantidade) : query.OrderBy(x => x.Quantidade); break;
                case "DtHrTexto": query = desc ? query.OrderByDescending(x => x.DtHr) : query.OrderBy(x => x.DtHr); break;
                case "Usuario": query = desc ? query.OrderByDescending(x => x.Usuario) : query.OrderBy(x => x.Usuario); break;
                default: query = desc ? query.OrderByDescending(x => x.ItemNr) : query.OrderBy(x => x.ItemNr); break;
            }

            var itens = query.Skip(model.start).Take(GetPageLength(model)).ToList();
            foreach (var item in itens) item.DtHrTexto = item.DtHr.ToString("dd/MM/yyyy HH:mm:ss");

            JsonResult result = Json(new { draw = model.draw, recordsFiltered, recordsTotal, data = itens });
            result.MaxJsonLength = int.MaxValue;
            return result;
        }

        [HttpPost]
        public ActionResult GetNotasFiscais(int? areaId)
        {
            DataTableAjaxPostModel model = ReadDataTableModel();
            if (model == null) return EmptyDataTableResult();

            var query = (from h in db.NotaFiscal.AsNoTracking()
                         where h.StatusId < 7 && h.FilialId == filialId && h.CriadoEm >= inicio
                         select new PendenciasViewModel
                         {
                             NFiscal = h.Numero,
                             Status = (from sv in db.StatusNotaFiscal where sv.Id == h.StatusId select sv.Nome).FirstOrDefault(),
                             CriadoEm = h.CriadoEm,
                             Origem = (from sv in db.OrigemNotaFiscal where sv.Codigo == h.Emissor select sv.Descricao).FirstOrDefault(),
                             Usuario = h.CriadoPor
                         });

            int recordsTotal = query.Count();
            string termo = GetSearchValue(model);
            if (!string.IsNullOrWhiteSpace(termo))
            {
                query = query.Where(x =>
                    (x.NFiscal ?? string.Empty).Contains(termo) ||
                    (x.Status ?? string.Empty).Contains(termo) ||
                    (x.Origem ?? string.Empty).Contains(termo) ||
                    (x.Usuario ?? string.Empty).Contains(termo));
            }

            int recordsFiltered = query.Count();
            string sortField = GetSortField(model);
            bool desc = IsDescending(model);
            switch (sortField)
            {
                case "Status": query = desc ? query.OrderByDescending(x => x.Status) : query.OrderBy(x => x.Status); break;
                case "Origem": query = desc ? query.OrderByDescending(x => x.Origem) : query.OrderBy(x => x.Origem); break;
                case "DtHrTexto": query = desc ? query.OrderByDescending(x => x.CriadoEm) : query.OrderBy(x => x.CriadoEm); break;
                case "Usuario": query = desc ? query.OrderByDescending(x => x.Usuario) : query.OrderBy(x => x.Usuario); break;
                default: query = desc ? query.OrderByDescending(x => x.NFiscal) : query.OrderBy(x => x.NFiscal); break;
            }

            var notas = query.Skip(model.start).Take(GetPageLength(model)).ToList();

            foreach (var nota in notas)
            {
                nota.CriadoEmTexto = nota.CriadoEm.HasValue
                    ? nota.CriadoEm.Value.ToString("dd/MM/yyyy HH:mm:ss")
                    : string.Empty;
            }

            JsonResult result = Json(new { draw = model.draw, recordsFiltered, recordsTotal, data = notas });
            result.MaxJsonLength = int.MaxValue;
            return result;
        }

        [HttpPost]
        public ActionResult GetVolumes(int? areaId)
        {
            DataTableAjaxPostModel model = ReadDataTableModel();
            if (model == null) return EmptyDataTableResult();

            var query = from nfi in db.NotaFiscalItem.AsNoTracking()
                        join nf in db.NotaFiscal.AsNoTracking() on nfi.NotaFiscalId equals nf.Id
                        where nfi.StatusId < 7
                           && nfi.Volume != null
                           && nfi.Volume != string.Empty
                           && nf.FilialId == filialId
                           && nf.CriadoEm >= inicio
                        select new
                        {
                            nfi.Volume,
                            nfi.CriadoPor,
                            nf.Numero,
                            nfi.Item,
                            nfi.Quantidade,
                            nfi.CriadoEm
                        };

            string termo = GetSearchValue(model);
            if (!string.IsNullOrWhiteSpace(termo))
            {
                query = query.Where(x =>
                    (x.Volume ?? string.Empty).Contains(termo) ||
                    (x.CriadoPor ?? string.Empty).Contains(termo) ||
                    (x.Numero ?? string.Empty).Contains(termo) ||
                    (x.Item ?? string.Empty).Contains(termo));
            }

            var volumesQuery = query
                .GroupBy(x => new { x.Volume, x.CriadoPor })
                .Select(g => new PendenciasViewModel
                {
                    Volume = g.Key.Volume,
                    Usuario = g.Key.CriadoPor,
                    ItemNrCount = g.Select(x => x.Item).Distinct().Count(),
                    Quantidade = g.Sum(x => x.Quantidade),
                    CriadoEm = g.Min(x => x.CriadoEm)
                });

            int recordsFiltered = volumesQuery.Count();
            int recordsTotal = recordsFiltered;
            if (!string.IsNullOrWhiteSpace(termo))
            {
                recordsTotal = (from nfi in db.NotaFiscalItem.AsNoTracking()
                                join nf in db.NotaFiscal.AsNoTracking() on nfi.NotaFiscalId equals nf.Id
                                where nfi.StatusId < 7 && nfi.Volume != null && nfi.Volume != string.Empty
                                   && nf.FilialId == filialId && nf.CriadoEm >= inicio
                                group nfi by new { nfi.Volume, nfi.CriadoPor } into g
                                select g.Key).Count();
            }

            string sortField = GetSortField(model);
            bool desc = IsDescending(model);
            switch (sortField)
            {
                case "ItemNrCount": volumesQuery = desc ? volumesQuery.OrderByDescending(x => x.ItemNrCount) : volumesQuery.OrderBy(x => x.ItemNrCount); break;
                case "Quantidade": volumesQuery = desc ? volumesQuery.OrderByDescending(x => x.Quantidade) : volumesQuery.OrderBy(x => x.Quantidade); break;
                case "CriadoEmTexto": volumesQuery = desc ? volumesQuery.OrderByDescending(x => x.CriadoEm) : volumesQuery.OrderBy(x => x.CriadoEm); break;
                case "Usuario": volumesQuery = desc ? volumesQuery.OrderByDescending(x => x.Usuario) : volumesQuery.OrderBy(x => x.Usuario); break;
                default: volumesQuery = desc ? volumesQuery.OrderByDescending(x => x.Volume) : volumesQuery.OrderBy(x => x.Volume); break;
            }

            var volumes = volumesQuery.Skip(model.start).Take(GetPageLength(model)).ToList();
            var chavesVolume = volumes.Select(x => x.Volume).Distinct().ToList();

            var notasPorVolume = query
                .Where(x => chavesVolume.Contains(x.Volume))
                .Select(x => new
                {
                    x.Volume,
                    x.CriadoPor,
                    x.Numero
                })
                .Distinct()
                .ToList()
                .GroupBy(x => new { x.Volume, x.CriadoPor })
                .ToDictionary(
                    g => string.Concat(g.Key.Volume, "|", g.Key.CriadoPor ?? string.Empty),
                    g => string.Join(", ", g.Select(x => x.Numero))
                );

            foreach (var volume in volumes)
            {
                var chave = string.Concat(volume.Volume, "|", volume.Usuario ?? string.Empty);
                volume.NFiscal = notasPorVolume.ContainsKey(chave) ? notasPorVolume[chave] : string.Empty;
                volume.NFiscalCount = string.IsNullOrWhiteSpace(volume.NFiscal)
                    ? 0
                    : volume.NFiscal.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries).Length;
                volume.CriadoEmTexto = volume.CriadoEm.HasValue
                    ? volume.CriadoEm.Value.ToString("dd/MM/yyyy HH:mm:ss")
                    : string.Empty;
            }

            JsonResult result = Json(new { draw = model.draw, recordsFiltered, recordsTotal, data = volumes });
            result.MaxJsonLength = int.MaxValue;
            return result;
        }

        [HttpGet]
        public ActionResult GetItensByVolume(string volume)
        {
            ViewBag.Volume = (volume ?? string.Empty).Trim();
            return PartialView("_ItensByVolume", BuildItensByVolumeViewModel(volume, true));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AtualizarStatus(
            int id,
            decimal? qtdConferida,
            bool conferido,
            bool confirmarDivergencia,
            DateTime? modificadoEmEsperado)
        {
            using (DbContextTransaction transaction = db.Database.BeginTransaction(IsolationLevel.Serializable))
            {
            NotaFiscalItem itemNF = db.NotaFiscalItem
                .FirstOrDefault(x => x.Id == id && x.FilialId == filialId);
            if (itemNF == null)
            {
                return Json(new { success = false, message = "Item não encontrado em NotaFiscalItem" });
            }

            if (!conferido)
            {
                return Json(new { success = false, message = "Marque o campo Conferido antes de finalizar." });
            }

            if (!qtdConferida.HasValue)
            {
                return Json(new { success = false, message = "Informe a quantidade conferida." });
            }

            if (qtdConferida.Value < 0)
            {
                return Json(new { success = false, message = "A quantidade conferida não pode ser negativa." });
            }

            try
            {
                if (itemNF.ModificadoEm != modificadoEmEsperado)
                {
                    return Json(new
                    {
                        success = false,
                        concorrencia = true,
                        message = "O item foi alterado durante a operação. Recarregue o volume antes de continuar."
                    });
                }

                string usuarioAtual = Util.GetCurrentUser();
                bool administrador = Util.IsAdminProfile() || Util.IsAdminUser();
                if (itemNF.Conferido
                    && !administrador
                    && !string.Equals(itemNF.UsuarioConferencia, usuarioAtual, StringComparison.OrdinalIgnoreCase))
                {
                    return Json(new
                    {
                        success = false,
                        concorrencia = true,
                        message = "O item já foi conferido por " + (itemNF.UsuarioConferencia ?? "outro usuário") + "."
                    });
                }

                decimal diferenca = qtdConferida.Value - itemNF.Quantidade;
                if (diferenca != 0 && !confirmarDivergencia)
                {
                    return Json(new
                    {
                        success = false,
                        message = "A divergência deve ser confirmada explicitamente antes da finalização."
                    });
                }

                int statusConferidoId = GetStatusNotaFiscalId("Conferido");
                if (statusConferidoId <= 0)
                {
                    return Json(new { success = false, message = "Status 'Conferido' não encontrado na tabela StatusNotaFiscal" });
                }

                DateTime agora = Util.GetCurrentDateTime();
                itemNF.QtdConferida = qtdConferida.Value;
                itemNF.Conferido = true;
                itemNF.UsuarioConferencia = usuarioAtual;
                itemNF.DtHrConferencia = agora;
                itemNF.StatusId = statusConferidoId;
                itemNF.ModificadoEm = agora;
                itemNF.ModificadoPor = usuarioAtual;
                db.Entry(itemNF).State = EntityState.Modified;
                db.SaveChanges();

                AtualizarStatusNotaFiscalSeTodosItensConferidos(itemNF.NotaFiscalId, statusConferidoId);
                string volumeAtual = (itemNF.Volume ?? string.Empty).Trim();
                bool volumeFinalizado = !string.IsNullOrWhiteSpace(volumeAtual)
                    && db.NotaFiscalItem
                        .Where(x => x.FilialId == filialId && (x.Volume ?? string.Empty).Trim() == volumeAtual)
                        .All(x => x.Conferido && x.QtdConferida.HasValue);
                transaction.Commit();

                return Json(new
                {
                    success = true,
                    message = "Conferência registrada com sucesso",
                    volumeFinalizado,
                    item = new
                    {
                        qtdConferida = itemNF.QtdConferida,
                        qtdArmazenada = itemNF.QtdArmazenada,
                        diferenca,
                        conferido = itemNF.Conferido,
                        situacao = diferenca == 0 ? "Conferido" : diferenca < 0 ? "Conferido a menor" : "Conferido a maior",
                        usuarioConferencia = itemNF.UsuarioConferencia,
                        dtHrConferencia = itemNF.DtHrConferencia.HasValue ? itemNF.DtHrConferencia.Value.ToString("dd/MM/yyyy HH:mm:ss") : string.Empty,
                        usuarioArmazenagem = itemNF.UsuarioArmazenagem,
                        dtHrArmazenagem = itemNF.DtHrArmazenagem.HasValue ? itemNF.DtHrArmazenagem.Value.ToString("dd/MM/yyyy HH:mm:ss") : string.Empty,
                        modificadoEm = itemNF.ModificadoEm.HasValue ? itemNF.ModificadoEm.Value.ToString("o") : string.Empty
                    }
                });
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return Json(new { success = false, message = ex.Message });
            }
            }
        }

        private ConferenciaVolumeViewModel BuildConferenciaVolumeViewModel(string volumeNr)
        {
            string volumeNormalizado = (volumeNr ?? string.Empty).Trim();
            List<ItensByVolumeViewModel> itens = string.IsNullOrWhiteSpace(volumeNormalizado)
                ? new List<ItensByVolumeViewModel>()
                : BuildItensByVolumeViewModel(volumeNormalizado, false);

            return new ConferenciaVolumeViewModel
            {
                VolumeNr = volumeNormalizado,
                ConsultaRealizada = !string.IsNullOrWhiteSpace(volumeNormalizado),
                Mensagem = !string.IsNullOrWhiteSpace(volumeNormalizado) && itens.Count == 0
                    ? "Volume n\u00E3o localizado."
                    : string.Empty,
                Itens = itens
            };
        }

        private List<ItensByVolumeViewModel> BuildItensByVolumeViewModel(string volume, bool aplicarFiltroPeriodo)
        {
            string volumeNormalizado = (volume ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(volumeNormalizado))
            {
                return new List<ItensByVolumeViewModel>();
            }

            var query = from nfi in db.NotaFiscalItem
                        join nf in db.NotaFiscal on nfi.NotaFiscalId equals nf.Id
                        join s in db.StatusNotaFiscal on nfi.StatusId equals s.Id
                        join materialBase in db.Material on nfi.Item equals materialBase.Codigo into materiais
                        from material in materiais.DefaultIfEmpty()
                        where (nfi.Volume ?? string.Empty).Trim() == volumeNormalizado
                           && nf.FilialId == filialId
                        select new
                         {
                             nfi.Id,
                             nfi.Item,
                             nfi.Quantidade,
                             nfi.QtdConferida,
                             nfi.QtdArmazenada,
                             nfi.Conferido,
                             nfi.UsuarioConferencia,
                             nfi.DtHrConferencia,
                             nfi.UsuarioArmazenagem,
                             nfi.DtHrArmazenagem,
                             nfi.ModificadoEm,
                             nf.Numero,
                             nfi.StatusId,
                             nf.CriadoEm,
                             StatusNome = s.Nome,
                             ItemDescricao = material != null ? material.Descricao : null,
                             ItemCritico = material != null && material.ItemCritico,
                             ObservacaoItemCritico = material != null && material.ItemCritico
                                ? material.ObsItemCritico
                                : null,
                             Locacao = (from e in db.Estoque
                                        where e.FilialId == filialId && e.ItemNr == nfi.Item
                                        select e.Locacao).FirstOrDefault()
                         };

            if (aplicarFiltroPeriodo)
            {
                query = query.Where(x => x.CriadoEm >= inicio);
            }

            return query.ToList()
                    .Select(x => new ItensByVolumeViewModel
                     {
                         NfItemId = x.Id,
                         ItemNr = x.Item,
                         ItemDescricao = x.ItemDescricao,
                         ItemCritico = x.ItemCritico,
                         ObservacaoItemCritico = x.ObservacaoItemCritico,
                         Locacao = x.Locacao,
                         Quantidade = x.Quantidade,
                         QtdConferida = x.QtdConferida,
                         QtdArmazenada = x.QtdArmazenada,
                         Diferenca = x.QtdConferida.HasValue ? x.QtdConferida.Value - x.Quantidade : (decimal?)null,
                         Conferido = x.Conferido,
                         SituacaoConferencia = !x.Conferido
                            ? "Pendente"
                            : !x.QtdConferida.HasValue || x.QtdConferida.Value == x.Quantidade
                                ? "Conferido"
                                : x.QtdConferida.Value < x.Quantidade
                                    ? "Conferido a menor"
                                    : "Conferido a maior",
                         UsuarioConferencia = x.UsuarioConferencia,
                         DtHrConferencia = x.DtHrConferencia,
                         UsuarioArmazenagem = x.UsuarioArmazenagem,
                         DtHrArmazenagem = x.DtHrArmazenagem,
                         ModificadoEm = x.ModificadoEm,
                         NumeroNF = x.Numero,
                         StatusId = (int)(x.StatusId ?? 0),
                        StatusNome = x.StatusNome,
                        HabilitarCheckbox = Util.IsAdminProfile()
                            || Util.IsAdminUser()
                            || !x.Conferido
                            || x.UsuarioConferencia == Util.GetCurrentUser()
                    })
                    .OrderBy(x => x.ItemNr)
                    .ToList();
        }

        private int GetStatusNotaFiscalId(string nomeStatus)
        {
            string nomeNormalizado = NormalizeStatusName(nomeStatus);

            return db.StatusNotaFiscal
                .AsNoTracking()
                .ToList()
                .Where(x => NormalizeStatusName(x.Nome) == nomeNormalizado
                    || NormalizeStatusName(x.Descricao) == nomeNormalizado)
                .Select(x => x.Id)
                .FirstOrDefault();
        }

        private void AtualizarStatusNotaFiscalSeTodosItensConferidos(int? notaFiscalId, int statusConferidoId)
        {
            if (!notaFiscalId.HasValue || statusConferidoId <= 0)
            {
                return;
            }

            List<bool> statusItens = db.NotaFiscalItem
                .Where(x => x.NotaFiscalId == notaFiscalId.Value && x.FilialId == filialId)
                .Select(x => x.Conferido)
                .ToList();

            if (statusItens.Count == 0 || statusItens.Any(x => !x))
            {
                return;
            }

            NotaFiscal notaFiscal = db.NotaFiscal
                .FirstOrDefault(x => x.Id == notaFiscalId.Value && x.FilialId == filialId);

            if (notaFiscal == null || notaFiscal.StatusId == statusConferidoId)
            {
                return;
            }

            notaFiscal.StatusId = statusConferidoId;
            notaFiscal.ModificadoEm = Util.GetCurrentDateTime();
            notaFiscal.ModificadoPor = Util.GetCurrentUser();
            db.Entry(notaFiscal).State = EntityState.Modified;
            db.SaveChanges();
        }

        private static string NormalizeStatusName(string value)
        {
            return Util.RemoverAcentuacao((value ?? string.Empty).Trim()).ToUpperInvariant();
        }

        private DataTableAjaxPostModel ReadDataTableModel()
        {
            using (var reader = new StreamReader(Request.InputStream))
            {
                return JsonConvert.DeserializeObject<DataTableAjaxPostModel>(reader.ReadToEnd());
            }
        }

        private JsonResult EmptyDataTableResult()
        {
            return Json(new { draw = 0, recordsFiltered = 0, recordsTotal = 0, data = new object[0] });
        }

        private static string GetSearchValue(DataTableAjaxPostModel model)
        {
            return model.search == null ? string.Empty : (model.search.value ?? string.Empty).Trim();
        }

        private static string GetSortField(DataTableAjaxPostModel model)
        {
            int index = model.order != null && model.order.Length > 0 ? model.order[0].column : -1;
            return index >= 0 && model.columns != null && index < model.columns.Length
                ? model.columns[index].data
                : string.Empty;
        }

        private static bool IsDescending(DataTableAjaxPostModel model)
        {
            return model.order != null && model.order.Length > 0 && model.order[0].dir == "desc";
        }

        private static int GetPageLength(DataTableAjaxPostModel model)
        {
            return model.length > 0 ? model.length : 25;
        }

        public ActionResult NFiscaisFinalizadas()
        {
            //int periodo = Util.GetPeriodoRecebimento();

            //DateTime inicio = Util.GetCurrentDateTime().AddDays(-periodo);

            var vm = (from h in db.NotaFiscal
                      where h.StatusId == 7 && h.CriadoEm >= inicio && h.FilialId == filialId
                      select new PendenciasViewModel
                      {
                          NFiscal = h.Numero,
                          Status = (from sv in db.StatusNotaFiscal where sv.Id == h.StatusId select sv.Nome).FirstOrDefault(),
                          ModificadoEm = h.ModificadoEm,
                          Origem = (from sv in db.OrigemNotaFiscal where sv.Codigo == h.Emissor select sv.Descricao).FirstOrDefault(),
                          Usuario = h.ModificadoPor
                      }).ToList();

            return View(vm);
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

    internal class Datetime
    {
    }
}
