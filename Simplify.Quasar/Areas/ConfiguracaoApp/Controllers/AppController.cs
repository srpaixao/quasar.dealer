using System.Web.Mvc;

using Simplify.Quasar.Custom;
using Simplify.Quasar.Models;

namespace Simplify.Quasar.Areas.ConfiguracaoApp.Controllers
{
    [ValidateSession]
    public class AppController : Controller
    {
        Quasar_Entities db = new Quasar_Entities();

        // GET: Estoque/Contagem
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