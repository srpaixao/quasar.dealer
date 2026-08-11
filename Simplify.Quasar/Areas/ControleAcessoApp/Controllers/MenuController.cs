using System.Linq;
using System.Web;
using System.Web.Mvc;
using Simplify.Quasar.Custom;
using Simplify.Quasar.Models;
using Simplify.Quasar.ViewModels;

namespace Simplify.Quasar.Areas.ControleAcessoApp.Controllers
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

            if (!Util.IsAdminProfile())
            {
                itens_menu = itens_menu
                    .Select(m =>
                    {
                        m._menu = m._menu
                            .Where(sub => !string.Equals(sub.Controller, "Perfil", System.StringComparison.OrdinalIgnoreCase))
                            .ToList();

                        return m;
                    })
                    .Where(m =>
                        !string.Equals(m.Controller, "Perfil", System.StringComparison.OrdinalIgnoreCase)
                        && (
                            m._menu.Count > 0
                            || (!string.IsNullOrWhiteSpace(m.Controller) && !string.IsNullOrWhiteSpace(m.Action))
                        ))
                    .ToList();
            }

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
