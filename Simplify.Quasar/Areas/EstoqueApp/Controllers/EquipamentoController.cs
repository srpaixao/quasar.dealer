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
    public class EquipamentoController : Controller
    {
        Quasar_Entities db = new Quasar_Entities();

        int filialId = Util.GetCurrentFilial();

        // GET: Equipamento/Index
        public ActionResult Index()
        {
            var vm = (from u in db.Equipamento
                      where u.FilialId == filialId
                      select new EquipamentoViewModel
                      {
                          Id = u.Id,
                          Nome = u.Nome,
                          Tipo = u.Tipo,
                          Descricao = u.Descricao,
                          Bloqueado = u.Bloqueado,
                          Observacoes = u.Observacoes                    
                      }).ToList();

            // Obtem lista de permissões mostra botão para criar/alterar/excluir ou não
            ViewBag.Permissoes = Util.GetPermissoes(ControllerContext.RouteData.Values["controller"].ToString(), ControllerContext.RouteData.DataTokens["area"] as string);

            return View(vm);
        }

        // GET: Equipamento/Create
        public ActionResult Create()
        {
            EquipamentoViewModel vm = new EquipamentoViewModel();
            return PartialView("_Create", vm);
        }

        // POST: Equipamento/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(EquipamentoViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                return PartialView("_Create", vm);
            }

            // Verifica se o equipamento já existe 
            if (db.Equipamento.Any(p => p.FilialId == filialId && p.Nome.ToLower() == vm.Nome.ToLower()))
            {
                ModelState.AddModelError("Nome", "Já existe um equipamento cadastrado com este nome");
                return PartialView("_Create", vm);
            }

            Equipamento equipamento = new Equipamento();
            equipamento.Nome = vm.Nome;
            equipamento.Tipo = vm.Tipo;
            equipamento.Descricao = vm.Descricao;
            equipamento.Bloqueado = vm.Bloqueado;
            equipamento.Observacoes = vm.Observacoes;
            equipamento.CriadoPor = Util.GetCurrentUser();
            equipamento.CriadoEm = Util.GetCurrentDateTime();
            equipamento.FilialId = filialId;

            ViewBag.ControllerName = ControllerContext.RouteData.Values["controller"].ToString();
            ViewBag.ActionName = ControllerContext.RouteData.Values["action"].ToString();

            using (DbContextTransaction tr = db.Database.BeginTransaction())
            {
                try
                {
                    db.Equipamento.Add(equipamento);
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

        // GET: Equipamento/Edit
        public ActionResult Edit(int id)
        {
            Equipamento equipamento = db.Equipamento.Find(id);
            if (equipamento == null)
            {
                return HttpNotFound();
            }

            EquipamentoViewModel vm = new EquipamentoViewModel();
            vm.Id = equipamento.Id;
            vm.Nome = equipamento.Nome;
            vm.Tipo = equipamento.Tipo;
            vm.Descricao = equipamento.Descricao;
            vm.Bloqueado = equipamento.Bloqueado;
            vm.Observacoes = equipamento.Observacoes;
            vm.CriadoEm = equipamento.CriadoEm;
            vm.CriadoPor = equipamento.CriadoPor;
            vm.CriadoPorNome = (from u in db.Usuario where u.FilialId == filialId && u.Login == equipamento.CriadoPor select u.Nome).FirstOrDefault();
            vm.ModificadoEm = equipamento.ModificadoEm;
            vm.ModificadoPor = equipamento.ModificadoPor;
            vm.ModificadoPorNome = (from u in db.Usuario where u.FilialId == filialId && u.Login == equipamento.ModificadoPor select u.Nome).FirstOrDefault();

            return PartialView("_Edit", vm);
        }

        // POST: Equipamento/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(EquipamentoViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                return PartialView("_Edit", vm);
            }

            Equipamento equipamento = db.Equipamento.Find(vm.Id);
            if (equipamento == null)
            {
                return HttpNotFound();
            }

            // Verifica se o equipamento informado já existe
            if (equipamento.Nome.ToLower() != vm.Nome.ToLower())
            {
                if (db.Equipamento.Any(p => p.Nome.ToLower() == vm.Nome.ToLower() && p.FilialId == filialId))
                {
                    ModelState.AddModelError("Nome", "Já existe um equipamento cadastrado com este nome");
                    return PartialView("_Edit", vm);
                }
            }

            equipamento.Nome = vm.Nome;
            equipamento.Tipo = vm.Tipo;
            equipamento.Descricao = vm.Descricao;
            equipamento.Bloqueado = vm.Bloqueado;
            equipamento.Observacoes = vm.Observacoes;
            equipamento.ModificadoPor = Util.GetCurrentUser();
            equipamento.ModificadoEm = Util.GetCurrentDateTime();
            equipamento.FilialId = filialId;

            db.Entry(equipamento).State = EntityState.Modified;

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

        // POST: Equipamento/Delete (chamada AJAX)
        [HttpPost]
        public ActionResult Delete(int id)
        {
            Equipamento equipamento = db.Equipamento.Find(id);
            if (equipamento == null)
            {
                return Json(new { success = false, msg = "Equipamento não encontrado!" });
            }

            using (DbContextTransaction tr = db.Database.BeginTransaction())
            {
                try
                {
                    db.Equipamento.Remove(equipamento);
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