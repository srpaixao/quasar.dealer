using System;
using System.Linq;
using System.Web.Mvc;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Data.Entity.Infrastructure;

using Simplify.Quasar.Models;
using Simplify.Quasar.Custom;
using Simplify.Quasar.Areas.ExpedicaoApp.ViewModels;

namespace Simplify.Quasar.Areas.ExpedicaoApp.Controllers
{
    //[VerifySessionState]
    [ValidateSession]
    public class ParadaController : Controller
    {
        Quasar_Entities db = new Quasar_Entities();

        // GET: Parada/Index
        public ActionResult Index()
        {
            var vm = (from u in db.Parada
                      select new ParadaViewModel
                      {
                          Id = u.Id,
                          Descricao = u.Descricao,
                          Nome = u.Nome                    
                      }).ToList();

            // Obtem lista de permissões mostra botão para criar/alterar/excluir ou não
            ViewBag.Permissoes = Util.GetPermissoes(ControllerContext.RouteData.Values["controller"].ToString());

            return View(vm);
        }

        // GET: Parada/Create
        public ActionResult Create()
        {
            ParadaViewModel vm = new ParadaViewModel();
            return PartialView("_Create", vm);
        }

        // POST: Parada/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(ParadaViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                return PartialView("_Create", vm);
            }

            // Verifica se a parada já existe 
            if (db.Parada.Any(p => p.Nome.ToLower() == vm.Nome.ToLower()))
            {
                ModelState.AddModelError("Nome", "Já existe uma parada cadastrada com este nome");
                return PartialView("_Create", vm);
            }

            Parada parada = new Parada();
            parada.Nome = vm.Nome;
            parada.Descricao = vm.Descricao;
            parada.Observacoes = vm.Observacoes;
            parada.CriadoPor = Util.GetCurrentUser();
            parada.CriadoEm = Util.GetCurrentDateTime();

            ViewBag.ControllerName = ControllerContext.RouteData.Values["controller"].ToString();
            ViewBag.ActionName = ControllerContext.RouteData.Values["action"].ToString();

            using (DbContextTransaction tr = db.Database.BeginTransaction())
            {
                try
                {
                    db.Parada.Add(parada);
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

        // GET: Parada/Edit
        public ActionResult Edit(int id)
        {
            Parada parada = db.Parada.Find(id);
            if (parada == null)
            {
                return HttpNotFound();
            }

            ParadaViewModel vm = new ParadaViewModel();
            vm.Id = parada.Id;
            vm.Nome = parada.Nome;
            vm.Descricao = parada.Descricao;
            vm.Observacoes = parada.Observacoes;
            vm.CriadoEm = parada.CriadoEm;
            vm.CriadoPor = parada.CriadoPor;
            vm.CriadoPorNome = (from u in db.Usuario where u.Login == parada.CriadoPor select u.Nome).FirstOrDefault();
            vm.ModificadoEm = parada.ModificadoEm;
            vm.ModificadoPor = parada.ModificadoPor;
            vm.ModificadoPorNome = (from u in db.Usuario where u.Login == parada.ModificadoPor select u.Nome).FirstOrDefault();

            return PartialView("_Edit", vm);
        }

        // POST: Parada/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(ParadaViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                return PartialView("_Edit", vm);
            }

            Parada parada = db.Parada.Find(vm.Id);
            if (parada == null)
            {
                return HttpNotFound();
            }

            // Verifica se a Parada informada já existe
            if (parada.Nome.ToLower() != vm.Nome.ToLower())
            {
                if (db.Parada.Any(p => p.Nome.ToLower() == vm.Nome.ToLower()))
                {
                    ModelState.AddModelError("Nome", "Já existe uma parada cadastrada com este nome");
                    return PartialView("_Edit", vm);
                }
            }

            parada.Nome = vm.Nome;
            parada.Descricao = vm.Descricao;
            parada.Observacoes = vm.Observacoes;
            parada.ModificadoPor = Util.GetCurrentUser();
            parada.ModificadoEm = Util.GetCurrentDateTime();

            db.Entry(parada).State = EntityState.Modified;

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

        // POST: Parada/Delete (chamada AJAX)
        [HttpPost]
        public ActionResult Delete(int id)
        {
            Parada parada = db.Parada.Find(id);
            if (parada == null)
            {
                return Json(new { success = false, msg = "Parada não encontrada!" });
            }

            using (DbContextTransaction tr = db.Database.BeginTransaction())
            {
                try
                {
                    db.Parada.Remove(parada);
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