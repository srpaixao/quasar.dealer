using System.Linq;
using System.Web;
using System.Web.Mvc;
using Simplify.Quasar.Custom;
using Simplify.Quasar.Models;
using Simplify.Quasar.ViewModels;

namespace Simplify.Quasar.Areas.EstoqueApp.Controllers
{
    [ValidateSession]
    public class MenuController : Controller
    {
        Quasar_Entities db = new Quasar_Entities();

        int filialId = Util.GetCurrentFilial();

        public ActionResult Index(string menuContext)
        {
            string area = ControllerContext.RouteData.DataTokens["area"].ToString();
            if (area == "")
            {
                throw new HttpException(500, "Area not found");
            }

            int perfilId = Util.GetPerfilId();
            var itens_menu = Util.GetMenusByPerfil(perfilId, db, area);

            if (string.Equals(menuContext, "Locacao", System.StringComparison.OrdinalIgnoreCase))
            {
                var menuGlobal = Util.GetMenusByPerfil(perfilId, db);
                var raizLocacao = menuGlobal.FirstOrDefault(item =>
                    item._menu.Any(sub => string.Equals(sub.Controller, "Locacao", System.StringComparison.OrdinalIgnoreCase)));

                if (raizLocacao != null)
                {
                    raizLocacao.Titulo = "Cadastros";
                    raizLocacao.Css = "fa fa-folder-open";
                    raizLocacao._menu = raizLocacao._menu
                        .Where(sub => string.Equals(sub.Controller, "Locacao", System.StringComparison.OrdinalIgnoreCase))
                        .ToList();
                    itens_menu = new System.Collections.Generic.List<MenuViewModel> { raizLocacao };
                }
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
