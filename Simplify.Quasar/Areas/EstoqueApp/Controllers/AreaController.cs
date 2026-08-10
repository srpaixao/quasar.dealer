using System;
using System.Linq;
using System.Web.Mvc;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Data.Entity.Infrastructure;

using Simplify.Quasar.Models;
using Simplify.Quasar.Custom;
using Simplify.Quasar.Areas.EstoqueApp.ViewModels;

namespace Simplify.Quasar.Areas.EstoqueApp.Controllers
{
    [ValidateSession]
    public class AreaController : Controller
    {
        Quasar_Entities db = new Quasar_Entities();

        int filialId = Util.GetCurrentFilial();

        // GET: Area/Index
        public ActionResult Index()
        {
            var vm = (from u in db.Area
                      where u.FilialId == filialId
                      select new AreaViewModel
                      {
                          Id = u.Id,
                          Descricao = u.Descricao,
                          Nome = u.Nome,                       
                      }).ToList();

            // Obtem lista de permissões mostra botão para criar/alterar/excluir ou não
            ViewBag.Permissoes = Util.GetPermissoes(ControllerContext.RouteData.Values["controller"].ToString());

            return View(vm);
        }

        // GET: Area/Create
        public ActionResult Create()
        {
            AreaViewModel vm = new AreaViewModel();
            return PartialView("_Create", vm);
        }

        // POST: Area/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(AreaViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                return PartialView("_Create", vm);
            }

            // Verifica se a Area já existe 
            if (db.Area.Any(p => p.Nome.ToLower() == vm.Nome.ToLower()))
            {
                ModelState.AddModelError("Nome", "Já existe área cadastrada com este nome");
                return PartialView("_Create", vm);
            }

            Area area = new Area();
            area.Nome = vm.Nome;
            area.Descricao = vm.Descricao;
            area.CriadoPor = Util.GetCurrentUser();
            area.CriadoEm = Util.GetCurrentDateTime();
            area.FilialId = filialId;

            ViewBag.ControllerName = ControllerContext.RouteData.Values["controller"].ToString();
            ViewBag.ActionName = ControllerContext.RouteData.Values["action"].ToString();

            using (DbContextTransaction tr = db.Database.BeginTransaction())
            {
                try
                {
                    db.Area.Add(area);
                    db.SaveChanges();
                    tr.Commit();
                }
                catch (DbEntityValidationException ex)
                {
                    ViewBag.Exception = ex.Message;
                    ViewBag.InnerException = ex.InnerException;
                    ViewBag.Source = ex.Source;

                    string msgErro = "";
                    foreach (DbEntityValidationResult item in ex.EntityValidationErrors)
                    {
                        DbEntityEntry entry = item.Entry;
                        string entityTypeName = entry.Entity.GetType().Name;
                        foreach (DbValidationError subItem in item.ValidationErrors)
                        {
                            msgErro += string.Format("{0}", subItem.ErrorMessage + Environment.NewLine);
                        }
                    }
                    TempData["ErrorDetail"] = msgErro;

                    tr.Rollback();
                    return PartialView("_Create", vm);
                }
                catch (Exception ex)
                {
                    ViewBag.Exception = ex.Message;
                    ViewBag.InnerException = ex.InnerException;
                    ViewBag.Source = ex.Source;
                    tr.Rollback();
                    return PartialView("_Create", vm);
                }
            }

            return Json(new { success = true });
        }

        // GET: Area/Edit
        public ActionResult Edit(int id)
        {
            Area area = db.Area.Find(id);
            if (area == null)
            {
                return HttpNotFound();
            }

            AreaViewModel vm = new AreaViewModel();
            vm.Id = area.Id;
            vm.Nome = area.Nome;
            vm.Descricao = area.Descricao;
            vm.CriadoEm = area.CriadoEm;
            vm.CriadoPor = area.CriadoPor;
            vm.CriadoPorNome = (from u in db.Usuario where u.Login == area.CriadoPor select u.Nome).FirstOrDefault();
            vm.ModificadoEm = area.ModificadoEm;
            vm.ModificadoPor = area.ModificadoPor;
            vm.FilialId = filialId;
            vm.ModificadoPorNome = (from u in db.Usuario where u.Login == area.ModificadoPor select u.Nome).FirstOrDefault();
            return PartialView("_Edit", vm);
        }

        // POST: Area/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(AreaViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                return PartialView("_Edit", vm);
            }

            Area area = db.Area.Find(vm.Id);
            if (area == null)
            {
                return HttpNotFound();
            }

            // Verifica se a Area informada já existe
            if (area.Nome.ToLower() != vm.Nome.ToLower())
            {
                if (db.Area.Any(p => p.FilialId == filialId && p.Nome.ToLower() == vm.Nome.ToLower()))
                {
                    ModelState.AddModelError("Nome", "Já existe área cadastrada com este nome");
                    return PartialView("_Edit", vm);
                }
            }

            area.Nome = vm.Nome;
            area.Descricao = vm.Descricao;
            area.ModificadoPor = Util.GetCurrentUser();
            area.ModificadoEm = Util.GetCurrentDateTime();
            area.FilialId = filialId;
            db.Entry(area).State = EntityState.Modified;

            ViewBag.ControllerName = ControllerContext.RouteData.Values["controller"].ToString();
            ViewBag.ActionName = ControllerContext.RouteData.Values["action"].ToString();

            using (DbContextTransaction tr = db.Database.BeginTransaction())
            {
                try
                {
                    db.SaveChanges();
                    tr.Commit();
                }
                catch (DbEntityValidationException ex)
                {
                    ViewBag.Exception = ex.Message;
                    ViewBag.InnerException = ex.InnerException;
                    ViewBag.Source = ex.Source;

                    string msgErro = "";
                    foreach (DbEntityValidationResult item in ex.EntityValidationErrors)
                    {
                        DbEntityEntry entry = item.Entry;
                        string entityTypeName = entry.Entity.GetType().Name;
                        foreach (DbValidationError subItem in item.ValidationErrors)
                        {
                            msgErro += string.Format("{0}", subItem.ErrorMessage + Environment.NewLine);
                        }
                    }
                    TempData["ErrorDetail"] = msgErro;

                    tr.Rollback();
                    return PartialView("_Edit", vm);
                }
                catch (Exception ex)
                {
                    ViewBag.Exception = ex.Message;
                    ViewBag.InnerException = ex.InnerException;
                    ViewBag.Source = ex.Source;
                    tr.Rollback();
                    return PartialView("_Edit", vm);
                }
            }

            return Json(new { success = true });
        }

        // POST: Area/Delete (chamada AJAX)
        [HttpPost]
        public ActionResult Delete(int id)
        {
            Area area = db.Area.Find(id);
            if (area == null)
            {
                return Json(new { success = false, msg = "Área não encontrada!" });
            }

            using (DbContextTransaction tr = db.Database.BeginTransaction())
            {
                try
                {
                    db.Area.Remove(area);
                    db.SaveChanges();
                    tr.Commit();
                }
                catch (DbEntityValidationException ex)
                {
                    string msgErro = "";
                    foreach (DbEntityValidationResult item in ex.EntityValidationErrors)
                    {
                        DbEntityEntry entry = item.Entry;
                        string entityTypeName = entry.Entity.GetType().Name;
                        foreach (DbValidationError subItem in item.ValidationErrors)
                        {
                            msgErro += string.Format("{0}", subItem.ErrorMessage + Environment.NewLine);
                        }
                    }
                    tr.Rollback();
                    return Json(new { success = false, msg = msgErro });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, msg = ex.Message });
                }
            }

            return Json(new { success = true, msg = "Operação realizada com sucesso" });
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