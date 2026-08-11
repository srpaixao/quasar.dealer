using System.Linq;
using System.Web.Mvc;
using Simplify.Quasar.Models;
using Simplify.Quasar.ViewModels;
using Simplify.Quasar.Custom;

namespace Simplify.Quasar.Controllers
{
    [ValidateSession]
    public class MenuController : Controller
    {
        Quasar_Entities db = new Quasar_Entities();
        int filialId = Util.GetCurrentFilial();

        public ActionResult Index()
        {
            int perfilId = Util.GetPerfilId();
            var itens_menu = Util.GetMenusByPerfil(perfilId, db);

            ViewBag.TotalVolumes = 1;

            return PartialView("_ItensMenu", itens_menu);
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
