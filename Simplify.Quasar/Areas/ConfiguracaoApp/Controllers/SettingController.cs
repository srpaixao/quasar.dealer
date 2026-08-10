using System.Web.Mvc;

using Simplify.Quasar.Models;
using System;
using Simplify.Quasar.Custom;

namespace Simplify.Quasar.Areas.ConfiguracaoApp.Controllers
{
    [ValidateSession]
    public class SettingController : Controller
    {
        Quasar_Entities db = new Quasar_Entities();

        // GET: Estoque/Contagem
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public ActionResult ImprimirZPL(string ipImpressora, string portaImpressora, string comandosZPL)
        {
            try
            {
                return Json(new { success = true, msg = "OK" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, msg = ex.Message }, JsonRequestBehavior.AllowGet);
            }
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