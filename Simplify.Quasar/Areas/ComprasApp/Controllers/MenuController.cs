using System.Linq;
using System.Web;
using System.Web.Mvc;

using Simplify.Quasar.Custom;
using Simplify.Quasar.Models;
using Simplify.Quasar.ViewModels;

namespace Simplify.Quasar.Areas.ComprasApp.Controllers
{
    [ValidateSession]
    public class MenuController : Controller
    {
        Quasar_Entities db = new Quasar_Entities();

        // GET: Menu
        public ActionResult Index()
        {
            string area = ControllerContext.RouteData.DataTokens["area"].ToString();
            if (area == "")
            {
                throw new HttpException(500, "Area not found");
            }

            var itens_menu = (from m in db.AppMenu
                              where m.Nivel == 1 && m.Status == true && m.Area == area
                              orderby m.Sequencia
                              select new MenuViewModel
                              {
                                  Id = m.Id,
                                  Titulo = m.Titulo,
                                  Area = m.Area,
                                  Controller = m.Controller,
                                  Action = m.Action,
                                  Css = m.Css,
                                  Status = m.Status,
                                  Nivel = m.Nivel,
                                  IdNivelSup = m.IdNivelSup,
                                  Sequencia = m.Sequencia,
                                  _menu = (from m2 in db.AppMenu
                                           where m2.IdNivelSup == m.Id && m2.Status == true && m.Area == area
                                           orderby m2.Sequencia
                                           select new SubMenu
                                           {
                                               Id = m2.Id,
                                               Titulo = m2.Titulo,
                                               Area = m.Area,
                                               Controller = m2.Controller,
                                               Action = m2.Action,
                                               Css = m2.Css,
                                               Status = m2.Status,
                                               IdNivelSup = m2.IdNivelSup,
                                               Sequencia = m2.Sequencia
                                           }).ToList()
                              }).ToList();

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