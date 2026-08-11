using System.Linq;
using System.Web;
using System.Web.Mvc;

using Simplify.Quasar.Models;
using Simplify.Quasar.Custom;
using Simplify.Quasar.ViewModels;

namespace Simplify.Quasar.Areas.AdminApp.Controllers
{
    [ValidateSession]
    public class MenuController : Controller
    {
        Quasar_Entities db = new Quasar_Entities();

        int filialId = Util.GetCurrentFilial();

        public ActionResult Index()
        {
            string area = ControllerContext.RouteData.DataTokens["area"].ToString();
            if (area == "")
            {
                throw new HttpException(500, "Area not found");
            }

            int perfilId = Util.GetPerfilId();
            var itens_menu = Util.GetMenusByPerfil(perfilId, db, area);

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
