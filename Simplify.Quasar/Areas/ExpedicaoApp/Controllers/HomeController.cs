using System.Web.Mvc;
using Simplify.Quasar.Custom;
using Simplify.Quasar.Models;

namespace Simplify.Quasar.Areas.ExpedicaoApp.Controllers
{
    [ValidateSession]
    public class HomeController : Controller
    {
        Quasar_Entities db = new Quasar_Entities();

        // GET: Admin/Home
        public ActionResult Index()
        {
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