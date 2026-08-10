using System;
using System.Linq;
using System.Web.Mvc;
using System.Data;

using Simplify.Quasar.Models;
using Simplify.Quasar.Areas.RecebimentoApp.ViewModels;
using Simplify.Quasar.Custom;
using System.Data.Entity;

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
            periodo = Util.GetPeriodoExpedicao();
            inicio = DateTime.Now.AddDays(-periodo);
        }

        public ActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public ActionResult GetItens(int? areaId)
        {
            var itens = (from nf in db.NotaFiscalItem
                         where nf.StatusId < 7 && nf.FilialId == filialId && nf.CriadoEm >= inicio
                         select new PendenciasViewModel
                         {
                             NFId = nf.Id,
                             ItemNr = nf.Item,
                             Quantidade = nf.Quantidade,
                             VolumeNr = nf.Volume,
                             Usuario = nf.CriadoPor,
                             DtHr = (DateTime)nf.CriadoEm,
                             Status = (from sv in db.StatusNotaFiscal where sv.Id == nf.StatusId select sv.Nome).FirstOrDefault(),
                             Descricao = (from s in db.Material where s.Codigo == nf.Item select s.Descricao).FirstOrDefault(),
                             Locacao = (from i in db.Estoque where i.FilialId == filialId && i.ItemNr == nf.Item select i.Locacao).FirstOrDefault(),
                         }).Distinct().ToList();

            JsonResult result = Json(new { data = itens }, JsonRequestBehavior.AllowGet);
            result.MaxJsonLength = int.MaxValue;
            return result;
        }

        [HttpGet]
        public ActionResult GetNotasFiscais(int? areaId)
        {
            var notas = (from h in db.NotaFiscal
                         where h.StatusId < 7 && h.FilialId == filialId && h.CriadoEm >= inicio
                         select new PendenciasViewModel
                         {
                             NFiscal = h.Numero,
                             Status = (from sv in db.StatusNotaFiscal where sv.Id == h.StatusId select sv.Nome).FirstOrDefault(),
                             CriadoEm = h.CriadoEm,
                             Origem = (from sv in db.OrigemNotaFiscal where sv.Codigo == h.Observacoes select sv.Descricao).FirstOrDefault(),
                             Usuario = h.CriadoPor
                         }).ToList();

            JsonResult result = Json(new { data = notas }, JsonRequestBehavior.AllowGet);
            result.MaxJsonLength = int.MaxValue;
            return result;
        }

        [HttpGet]
        public ActionResult GetVolumes(int? areaId)
        {
            var data = (from nfi in db.NotaFiscalItem
                        join nf in db.NotaFiscal on nfi.NotaFiscalId equals nf.Id
                        where nfi.StatusId < 7 && nfi.Volume != null && nfi.Volume != string.Empty && nf.FilialId == filialId && nf.CriadoEm >= inicio
                        select new
                        {
                            nfi.Volume,
                            nfi.CriadoPor,
                            nf.Numero,
                            nfi.Item,
                            nfi.Quantidade
                        }).ToList();

            var volumes = data
                .GroupBy(x => new { x.Volume, x.CriadoPor })
                .Select(g => new PendenciasViewModel
                {
                    Volume = g.Key.Volume,
                    Usuario = g.Key.CriadoPor,
                    NFiscal = string.Join(", ", g.Select(x => x.Numero).Distinct()),
                    NFiscalCount = g.Select(x => x.Item).Distinct().Count(),
                    ItemNr = string.Join(", ", g.Select(x => x.Item).Distinct()),
                    ItemNrCount = g.Select(x => x.Item).Distinct().Count(),
                    Quantidade = g.Sum(x => x.Quantidade)
                }).ToList();

            foreach (var item in volumes)
            {
                item.CriadoEm = (from nfi in db.NotaFiscalItem
                                 join nf in db.NotaFiscal on nfi.NotaFiscalId equals nf.Id
                                 where nfi.Volume == item.Volume && nfi.FilialId == filialId
                                 select nfi.CriadoEm).FirstOrDefault(); 
            }

            JsonResult result = Json(new { data = volumes }, JsonRequestBehavior.AllowGet);
            result.MaxJsonLength = int.MaxValue;
            return result;
        }

        [HttpGet]
        public ActionResult GetItensByVolume(string volume)
        {
            ViewBag.Volume = volume;

            var vm = (from nfi in db.NotaFiscalItem
                      join nf in db.NotaFiscal on nfi.NotaFiscalId equals nf.Id
                      join m in db.Material on nfi.Item equals m.Codigo
                      join s in db.StatusNotaFiscal on nfi.StatusId equals s.Id
                      where nfi.Volume == volume && nf.FilialId == filialId && nf.CriadoEm >= inicio
                      select new ItensByVolumeViewModel
                      {
                          NfItemId = nfi.Id,
                          ItemNr = nfi.Item,
                          ItemDescricao = m.Descricao,
                          Quantidade = (int)nfi.Quantidade,
                          NumeroNF = nf.Numero,
                          StatusId = (int)nfi.StatusId,
                          StatusNome = s.Nome
                      }).OrderBy(x => x.ItemNr).ToList();

            foreach (var item in vm)
            {
                item.HabilitarCheckbox = item.StatusId == 3 || item.StatusId == 4;
            }

            return PartialView("_ItensByVolume", vm);
        }

        [HttpPost]
        public ActionResult AtualizarStatus(int id, bool conferido)
        {
            NotaFiscalItem itemNF = db.NotaFiscalItem.Find(id);
            if (itemNF == null)
            {
                return Json(new { success = false, message = "Item não encontrado em NotaFiscalItem" });
            }

            try
            {
                itemNF.StatusId = conferido ? 4 : 3;
                itemNF.ModificadoEm = Util.GetCurrentDateTime();
                itemNF.ModificadoPor = Util.GetCurrentUser();
                db.Entry(itemNF).State = EntityState.Modified;
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }

            return Json(new { success = true, message = "Status atualizado com sucesso" });
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
                          Origem = (from sv in db.OrigemNotaFiscal where sv.Codigo == h.Observacoes select sv.Descricao).FirstOrDefault(),
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