using System.Web.Mvc;
using Simplify.Quasar.Custom;
using Simplify.Quasar.Models;

namespace Simplify.Quasar.Areas.EstoqueApp.Controllers
{
    [ValidateSession]
    public class ArmazenagemController : Controller
    {
        Quasar_Entities db = new Quasar_Entities();

        // GET: Estoque/Armazenagem
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