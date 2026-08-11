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
    [ValidateSession]
    public class VeiculoController : Controller
    {
        Quasar_Entities db = new Quasar_Entities();

        int filialId = Util.GetCurrentFilial();

        // GET: Veiculo/Index
        public ActionResult Index()
        {
            var vm = (from u in db.Veiculo
                      select new VeiculoViewModel
                      {
                          Id = u.Id,
                          Nome = u.Nome,
                          Descricao = u.Descricao                 
                      }).ToList();

            // Obtem lista de permissões mostra botão para criar/alterar/excluir ou não
            ViewBag.Permissoes = Util.GetPermissoes(ControllerContext.RouteData.Values["controller"].ToString(), ControllerContext.RouteData.DataTokens["area"] as string);

            return View(vm);
        }

        // GET: Veiculo/Create
        public ActionResult Create()
        {
            VeiculoViewModel vm = new VeiculoViewModel();
            return PartialView("_Create", vm);
        }

        // POST: Veiculo/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(VeiculoViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                return PartialView("_Create", vm);
            }

            // Verifica se o veículo já existe 
            if (db.Veiculo.Any(p => p.Nome.ToLower() == vm.Nome.ToLower()))
            {
                ModelState.AddModelError("Nome", "Já existe um veículo cadastrado com este nome");
                return PartialView("_Create", vm);
            }

            Veiculo veiculo = new Veiculo();
            veiculo.Nome = vm.Nome;
            veiculo.Descricao = vm.Descricao;
            veiculo.CriadoPor = Util.GetCurrentUser();
            veiculo.CriadoEm = Util.GetCurrentDateTime();

            ViewBag.ControllerName = ControllerContext.RouteData.Values["controller"].ToString();
            ViewBag.ActionName = ControllerContext.RouteData.Values["action"].ToString();

            using (DbContextTransaction tr = db.Database.BeginTransaction())
            {
                try
                {
                    db.Veiculo.Add(veiculo);
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

        // GET: Veiculo/Edit
        public ActionResult Edit(int id)
        {
            Veiculo veiculo = db.Veiculo.Find(id);
            if (veiculo == null)
            {
                return HttpNotFound();
            }

            VeiculoViewModel vm = new VeiculoViewModel();
            vm.Id = veiculo.Id;
            vm.Nome = veiculo.Nome;
            vm.Descricao = veiculo.Descricao;
            vm.CriadoEm = veiculo.CriadoEm;
            vm.CriadoPor = veiculo.CriadoPor;
            vm.CriadoPorNome = (from u in db.Usuario where u.Login == veiculo.CriadoPor select u.Nome).FirstOrDefault();
            vm.ModificadoEm = veiculo.ModificadoEm;
            vm.ModificadoPor = veiculo.ModificadoPor;
            vm.ModificadoPorNome = (from u in db.Usuario where u.Login == veiculo.ModificadoPor select u.Nome).FirstOrDefault();

            return PartialView("_Edit", vm);
        }

        // POST: Veiculo/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(VeiculoViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                return PartialView("_Edit", vm);
            }

            Veiculo veiculo = db.Veiculo.Find(vm.Id);
            if (veiculo == null)
            {
                return HttpNotFound();
            }

            // Verifica se o veículo informado já existe
            if (veiculo.Nome.ToLower() != vm.Nome.ToLower())
            {
                if (db.Veiculo.Any(p => p.Nome.ToLower() == vm.Nome.ToLower()))
                {
                    ModelState.AddModelError("Nome", "Já existe um veículo cadastrado com este nome");
                    return PartialView("_Edit", vm);
                }
            }

            veiculo.Nome = vm.Nome;
            veiculo.Descricao = vm.Descricao;
            veiculo.ModificadoPor = Util.GetCurrentUser();
            veiculo.ModificadoEm = Util.GetCurrentDateTime();

            db.Entry(veiculo).State = EntityState.Modified;

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

        // POST: Veiculo/Delete (chamada AJAX)
        [HttpPost]
        public ActionResult Delete(int id)
        {
            Veiculo veiculo = db.Veiculo.Find(id);
            if (veiculo == null)
            {
                return Json(new { success = false, msg = "Veículo não encontrado!" });
            }

            using (DbContextTransaction tr = db.Database.BeginTransaction())
            {
                try
                {
                    db.Veiculo.Remove(veiculo);
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