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

        // GET: Menu
        public ActionResult Index()
        {
            var itens_menu = (from m in db.AppMenu
                              where m.Nivel == 1 && m.Status == true
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
                                           where m2.IdNivelSup == m.Id && m2.Status == true
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

            string user = string.Empty;
            try
            {
                user = Session["useraccount"] as string ?? string.Empty;
            }
            catch (System.Exception)
            {
                user = string.Empty;
            }

            if (user.ToLower() != "admin")
            {
                itens_menu = itens_menu.Where(m => m.Id != 10 && m.IdNivelSup != 10).ToList();
            }

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