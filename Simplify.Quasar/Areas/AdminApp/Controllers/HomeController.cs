using System.Web.Mvc;

using Simplify.Quasar.Models;
using Simplify.Quasar.Custom;

namespace Simplify.Quasar.Areas.AdminApp.Controllers
{
    [ValidateSession]
    public class HomeController : Controller
    {
        Quasar_Entities db = new Quasar_Entities();

        // GET: AdminApp/Home
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