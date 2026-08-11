using Simplify.Quasar.Custom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Simplify.Quasar.Models;

namespace Simplify.Quasar.Areas.ExpedicaoApp.Controllers
{
    [ValidateSession]
    public class TesteImpressaoController : Controller
    {
        private readonly Quasar_Entities db = new Quasar_Entities();
        int filialId = Util.GetCurrentFilial();

        // GET: ExpedicaoApp/TesteImpressao
        public ActionResult Index()
        {
            string impressoraPadrao = GetAppConfigValue("ImpressoraPadrao");
            Impressora impressora = db.Impressora
                .FirstOrDefault(x => x.FilialId == filialId && x.Nome == impressoraPadrao);

            ViewBag.PrinterServerIP = GetAppConfigValue("PrinterServerIP");
            ViewBag.PrinterServerPort = GetAppConfigValue("PrinterServerPort");
            ViewBag.PrinterName = impressora != null ? impressora.Nome : string.Empty;
            ViewBag.PrinterIP = impressora != null ? impressora.IP : string.Empty;
            ViewBag.PrinterPort = impressora != null && impressora.Porta > 0 ? impressora.Porta.ToString() : string.Empty;
            return View();
        }

        private string GetAppConfigValue(string nome)
        {
            string valor = db.AppConfig
                .Where(x => x.Nome == nome && x.FilialId == filialId)
                .OrderBy(x => x.Id)
                .Select(x => x.Valor)
                .FirstOrDefault();

            return !string.IsNullOrWhiteSpace(valor)
                ? valor
                : db.AppConfig
                    .Where(x => x.Nome == nome && (!x.FilialId.HasValue || x.FilialId == 0))
                    .OrderBy(x => x.Id)
                    .Select(x => x.Valor)
                    .FirstOrDefault();
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
