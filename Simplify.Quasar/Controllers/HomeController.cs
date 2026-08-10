using System.Web.Mvc;
using System.Linq;
using Simplify.Quasar.Models;
using Simplify.Quasar.Custom;

namespace Simplify.Quasar.Controllers
{
    [ValidateSession]
    public class HomeController : Controller
    {
        Quasar_Entities db = new Quasar_Entities();

        [HttpGet]
        public ActionResult Index()
        {
            // Buscar filial informada no login
            int filialid = Util.GetCurrentFilial();
            if (filialid == 0) 
            {
                return RedirectToAction("Logout", "Account");
            }              

            ViewBag.filialid = filialid;
            ViewBag.VolumesParaConferencia = db.Volume.Where(x => x.StatusId == 1 && x.FilialId == filialid).Count();
            return View(); 
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
