using System.Linq;
using System.Web.Mvc;

using Simplify.Quasar.Models;
using Simplify.Quasar.Areas.EstoqueApp.ViewModels;
using Simplify.Quasar.Custom;
using System;

namespace Simplify.Quasar.Areas.EstoqueApp.Controllers
{
    [ValidateSession]
    public class MaterialController : Controller
    {
        Quasar_Entities db = new Quasar_Entities();

        int filialId = Util.GetCurrentFilial();

        // GET: EstoqueApp/Material
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult GetMaterial(string codigo)
        {
            MaterialViewModel material = new MaterialViewModel();

            try
            {
                material = (from m in db.Material
                            where m.Codigo == codigo
                            select new MaterialViewModel
                            {
                                Codigo = m.Codigo,
                                Descricao = m.Descricao,
                                UN = m.UN,
                                EmbalagemMin = m.EmbalagemMin,
                                MediaVendas = m.MediaVendas,
                                CustoUnitario = m.CustoUnitario,
                                Curva = m.Curva,
                                CriadoEm = m.CriadoEm,
                                CriadoPor = m.CriadoPor,
                                ModificadoEm = m.ModificadoEm,
                                ModificadoPor = m.ModificadoPor
                            }).FirstOrDefault();

                JsonResult result = Json(new { data = material, success = true, msg = string.Empty }, JsonRequestBehavior.AllowGet);
                return result;
            }
            catch (Exception ex)
            {
                JsonResult result = Json(new { data = material, success = false, msg = ex.Message }, JsonRequestBehavior.AllowGet);
                return result;
            }
        }

        public JsonResult MaterialLookup(string query)
        {
            var materiais = (from m in db.Material
                             where m.Codigo.ToUpper().Contains(query.ToUpper()) || m.Descricao.ToUpper().Contains(query.ToUpper())
                             select new MaterialViewModel
                             {
                                 Codigo = m.Codigo,
                                 Descricao = m.Codigo + " - " + m.Descricao
                             }).ToList();

            return Json(new { total_results = materiais.Count, results = materiais }, JsonRequestBehavior.AllowGet);
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