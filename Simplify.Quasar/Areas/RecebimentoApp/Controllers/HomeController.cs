using System;
using System.Web.Mvc;
using Simplify.Quasar.Custom;

namespace Simplify.Quasar.Areas.RecebimentoApp.Controllers
{
    [ValidateSession]
    public class HomeController : Controller
    {
        public ActionResult Dashboard(DateTime? dataInicial, DateTime? dataFinal)
        {
            return View(
                "~/Views/Shared/ProcessDashboard.cshtml",
                ProcessDashboardViewModel.Create("Recebimento", "RecebimentoApp", dataInicial, dataFinal));
        }
    }
}
