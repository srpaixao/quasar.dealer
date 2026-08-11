using System;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Web.Mvc;
using Simplify.Quasar.Models;
using Simplify.Quasar.Custom;
using Simplify.Quasar.Areas.ControleAcessoApp.ViewModels;

namespace Simplify.Quasar.Areas.ControleAcessoApp.Controllers
{
    [ValidateSession]
    [AuthorizeFunction]
    public class FuncaoController : Controller
    {
        Quasar_Entities db = new Quasar_Entities();
        int filialId = Util.GetCurrentFilial();

        // GET: Funcao
        public ActionResult Index()
        {
            var vm = (from f in db.AppFuncao
                      select new FuncaoViewModel
                      {
                          Id = f.Id,
                          Codigo = f.Codigo,
                          DescPTBR = f.DescPTBR,
                          DescES = f.DescES,
                          CodComponente = f.CodComponente,
                          Controller = f.Controller,
                          Action = f.Action,
                          Status = f.Status ?? false,
                          FilialId = f.FilialId,
                          NomeFilial = (from e in db.Empresa where e.Id == f.FilialId select e.Nome).FirstOrDefault(),
                          IdMenu = f.IdMenu,
                          TituloMenu = (from m in db.AppMenu where m.Id == f.IdMenu select m.Titulo).FirstOrDefault()
                      }).ToList();

            return View(vm);
        }

        public ActionResult Create()
        {
            FuncaoViewModel vm = new FuncaoViewModel();
            vm.MenuDDL = Util.GetAppMenuDDL(null);
            vm.FilialDDL = Util.GetEmpresas(null);
            vm.Status = true;
            return PartialView("_Create", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(FuncaoViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                vm.MenuDDL = Util.GetAppMenuDDL(vm.IdMenu);
                vm.FilialDDL = Util.GetEmpresas(vm.FilialId);
                return PartialView("_Create", vm);
            }

            if (db.AppFuncao.Any(f => f.Codigo.ToLower() == vm.Codigo.ToLower()))
            {
                ModelState.AddModelError("Codigo", "Já existe uma função com este código");
                vm.MenuDDL = Util.GetAppMenuDDL(vm.IdMenu);
                vm.FilialDDL = Util.GetEmpresas(vm.FilialId);
                return PartialView("_Create", vm);
            }

            AppFuncao funcao = new AppFuncao();
            funcao.Codigo = vm.Codigo;
            funcao.DescPTBR = vm.DescPTBR;
            funcao.DescES = vm.DescES;
            funcao.CodComponente = vm.CodComponente;
            funcao.Controller = vm.Controller;
            funcao.Action = vm.Action;
            funcao.IdMenu = vm.IdMenu;
            funcao.Status = vm.Status;
            funcao.FilialId = vm.FilialId;

            using (DbContextTransaction tr = db.Database.BeginTransaction())
            {
                try
                {
                    db.AppFuncao.Add(funcao);
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

        public ActionResult Edit(int id)
        {
            AppFuncao funcao = db.AppFuncao.Find(id);
            if (funcao == null)
            {
                return HttpNotFound();
            }

            FuncaoViewModel vm = new FuncaoViewModel();
            vm.Id = funcao.Id;
            vm.Codigo = funcao.Codigo;
            vm.DescPTBR = funcao.DescPTBR;
            vm.DescES = funcao.DescES;
            vm.CodComponente = funcao.CodComponente;
            vm.Controller = funcao.Controller;
            vm.Action = funcao.Action;
            vm.Status = funcao.Status ?? false;
            vm.FilialId = funcao.FilialId;
            vm.IdMenu = funcao.IdMenu;
            vm.MenuDDL = Util.GetAppMenuDDL(funcao.IdMenu);
            vm.FilialDDL = Util.GetEmpresas(funcao.FilialId);

            return PartialView("_Edit", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(FuncaoViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                vm.MenuDDL = Util.GetAppMenuDDL(vm.IdMenu);
                vm.FilialDDL = Util.GetEmpresas(vm.FilialId);
                return PartialView("_Edit", vm);
            }

            AppFuncao funcao = db.AppFuncao.Find(vm.Id);
            if (funcao == null)
            {
                return HttpNotFound();
            }

            if (db.AppFuncao.Any(f => f.Codigo.ToLower() == vm.Codigo.ToLower() && f.Id != vm.Id))
            {
                ModelState.AddModelError("Codigo", "Já existe uma função com este código");
                vm.MenuDDL = Util.GetAppMenuDDL(vm.IdMenu);
                vm.FilialDDL = Util.GetEmpresas(vm.FilialId);
                return PartialView("_Edit", vm);
            }

            funcao.Codigo = vm.Codigo;
            funcao.DescPTBR = vm.DescPTBR;
            funcao.DescES = vm.DescES;
            funcao.CodComponente = vm.CodComponente;
            funcao.Controller = vm.Controller;
            funcao.Action = vm.Action;
            funcao.IdMenu = vm.IdMenu;
            funcao.Status = vm.Status;
            funcao.FilialId = vm.FilialId;

            db.Entry(funcao).State = EntityState.Modified;

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

        [HttpPost]
        public ActionResult Delete(int id)
        {
            AppFuncao funcao = db.AppFuncao.Find(id);
            if (funcao == null)
            {
                return Json(new { success = false, msg = "Função não encontrada!" });
            }

            using (DbContextTransaction tr = db.Database.BeginTransaction())
            {
                try
                {
                    var perfisFuncoes = db.PerfilFuncao.Where(pf => pf.IdFuncao == id).ToList();
                    foreach (var pf in perfisFuncoes)
                    {
                        db.PerfilFuncao.Remove(pf);
                    }
                    db.AppFuncao.Remove(funcao);
                    db.SaveChanges();
                    tr.Commit();
                }
                catch (DbEntityValidationException ex)
                {
                    string msgErro = "";
                    foreach (DbEntityValidationResult item in ex.EntityValidationErrors)
                    {
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

        public ActionResult Detail(int id)
        {
            var vm = (from f in db.AppFuncao
                      where f.Id == id
                      select new FuncaoViewModel
                      {
                          Id = f.Id,
                          Codigo = f.Codigo,
                          DescPTBR = f.DescPTBR,
                          DescES = f.DescES,
                          CodComponente = f.CodComponente,
                          Controller = f.Controller,
                          Action = f.Action,
                          Status = f.Status ?? false,
                          FilialId = f.FilialId,
                          NomeFilial = (from e in db.Empresa where e.Id == f.FilialId select e.Nome).FirstOrDefault(),
                          IdMenu = f.IdMenu,
                          TituloMenu = (from m in db.AppMenu where m.Id == f.IdMenu select m.Titulo).FirstOrDefault()
                      }).FirstOrDefault();

            if (vm == null)
            {
                return HttpNotFound();
            }

            return PartialView("_Detail", vm);
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
