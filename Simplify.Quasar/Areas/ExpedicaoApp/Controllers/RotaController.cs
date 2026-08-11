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
    public class RotaController : Controller
    {
        Quasar_Entities db = new Quasar_Entities();

        int filialId = Util.GetCurrentFilial();

        // GET: Fornecedor/Index
        public ActionResult Index()
        {
            var vm = (from u in db.Rota where u.FilialId == filialId
                      select new RotaViewModel
                      {
                          Id = u.Id,
                          Descricao = u.Descricao,
                          Nome = u.Nome,
                          //Transferencia = u.Transferencia,                        
                      }).ToList();

            // Obtem lista de permissões mostra botão para criar/alterar/excluir ou não
            ViewBag.Permissoes = Util.GetPermissoes(ControllerContext.RouteData.Values["controller"].ToString(), ControllerContext.RouteData.DataTokens["area"] as string);

            return View(vm);
        }

        // GET: Rota/Create
        public ActionResult Create()
        {
            RotaViewModel vm = new RotaViewModel();
            return PartialView("_Create", vm);
        }

        // POST: Rota/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(RotaViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                return PartialView("_Create", vm);
            }

            string nome = Util.SemFormatacao(vm.Nome);

            // Verifica se o Rota já existe 
            if (db.Rota.Any(p => p.Nome.ToLower() == nome.ToLower()))
            {
                ModelState.AddModelError("Nome", "Já existe uma rota cadastrada com este nome");
                return PartialView("_Create", vm);
            }

            Rota rota = new Rota();
            rota.Nome = vm.Nome;
            rota.Descricao = vm.Descricao;
            rota.Observacoes = vm.Observacoes;
            rota.CriadoPor = Util.GetCurrentUser();
            rota.CriadoEm = Util.GetCurrentDateTime();
            rota.FilialId = filialId;

            ViewBag.ControllerName = ControllerContext.RouteData.Values["controller"].ToString();
            ViewBag.ActionName = ControllerContext.RouteData.Values["action"].ToString();

            using (DbContextTransaction tr = db.Database.BeginTransaction())
            {
                try
                {
                    db.Rota.Add(rota);
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

        // GET: Rota/Edit
        public ActionResult Edit(int id)
        {
            Rota rota = db.Rota.Find(id);
            if (rota == null)
            {
                return HttpNotFound();
            }

            RotaViewModel vm = new RotaViewModel();
            vm.Id = rota.Id;
            vm.Nome = rota.Nome;
            vm.Descricao  = rota.Descricao ;
            vm.Observacoes = rota.Observacoes;
            vm.FilialId = filialId;
            vm.CriadoEm = rota.CriadoEm;
            vm.CriadoPor = rota.CriadoPor;
            vm.CriadoPorNome = (from u in db.Usuario where u.Login == rota.CriadoPor select u.Nome).FirstOrDefault();
            vm.ModificadoEm = rota.ModificadoEm;
            vm.ModificadoPor = rota.ModificadoPor;
            vm.ModificadoPorNome = (from u in db.Usuario where u.Login == rota.ModificadoPor select u.Nome).FirstOrDefault();

            return PartialView("_Edit", vm);
        }

        // POST: Rota/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(RotaViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                return PartialView("_Edit", vm);
            }

            Rota rota = db.Rota.Find(vm.Id);
            if (rota == null)
            {
                return HttpNotFound();
            }       

            // Verifica se a rota informada já existe
            if (rota.Nome.ToLower() != vm.Nome.ToLower())
            {
                if (db.Rota.Any(p => p.Nome.ToLower() == vm.Nome.ToLower()))
                {
                    ModelState.AddModelError("Nome", "Já existe uma rota cadastrada com este nome");
                    return PartialView("_Edit", vm);
                }
            }

            rota.Nome = vm.Nome;
            rota.Descricao  = vm.Descricao;
            rota.Observacoes = vm.Observacoes;
            rota.ModificadoPor = Util.GetCurrentUser();
            rota.ModificadoEm = Util.GetCurrentDateTime();
            rota.FilialId = filialId;
            db.Entry(rota).State = EntityState.Modified;

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

        // POST: Rota/Delete (chamada AJAX)
        [HttpPost]
        public ActionResult Delete(int id)
        {
            Rota rota = db.Rota.Find(id);
            if (rota == null)
            {
                return Json(new { success = false, msg = "Rota não encontrada!" });
            }

            using (DbContextTransaction tr = db.Database.BeginTransaction())
            {
                try
                {
                    db.Rota.Remove(rota);
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