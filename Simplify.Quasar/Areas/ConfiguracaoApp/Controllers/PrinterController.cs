using System;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Data.Entity.Infrastructure;

using System.Linq;
using System.Web.Mvc;

using Simplify.Quasar.Models;
using Simplify.Quasar.Custom;
using Simplify.Quasar.Areas.ConfiguracaoApp.ViewModels;

namespace Simplify.Quasar.Areas.ConfiguracaoApp.Controllers
{
    [ValidateSession]
    public class PrinterController : Controller
    {
        Quasar_Entities db = new Quasar_Entities();

        int filialId = Util.GetCurrentFilial();

        // GET: Printer/Index
        public ActionResult Index()
        {
            var vm = (from i in db.Impressora.AsNoTracking() where i.FilialId == filialId
                      select new ImpressoraViewModel
                      {
                          Id = i.Id,
                          Nome = i.Nome,
                          IP = i.IP,
                          Porta = i.Porta,
                          FilialId = i.FilialId,
                          FilialNome = db.Empresa
                              .Where(e => e.Id == i.FilialId)
                              .Select(e => e.Nome)
                              .FirstOrDefault(),
                          Localizacao = i.Localizacao,
                          Fabricante = i.Fabricante,
                          Modelo =  i.Modelo,
                          CriadoEm = i.CriadoEm,
                          CriadoPor = i.CriadoPor,
                          ModificadoEm = i.ModificadoEm,
                          ModificadoPor = i.ModificadoPor
                      }).ToList();

            // Obtem lista de permissões
            //   ViewBag.Permissoes = Util.GetPermissoes(ControllerContext.RouteData.Values["controller"].ToString(), ControllerContext.RouteData.DataTokens["area"] as string);
            return View(vm);
        }

        // GET: Printer/Create
        public ActionResult Create()
        {
            ImpressoraViewModel vm = new ImpressoraViewModel();
            return PartialView("_Create", vm);
        }

        // POST: Printer/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(ImpressoraViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                return PartialView("_Create", vm);
            }

            // Verifica se a impressora já existe
            if (db.Impressora.Any(p => p.IP == vm.IP && p.FilialId == filialId))
            {
                ModelState.AddModelError("Login", "Já existe uma impressora cadastrada com este IP");
                return PartialView("_Create", vm);
            }

            Impressora impressora = new Impressora();
            impressora.Nome = vm.Nome;
            impressora.IP = vm.IP;
            impressora.Porta = vm.Porta;
            impressora.Localizacao = vm.Localizacao;
            impressora.Fabricante = vm.Fabricante;
            impressora.Modelo = vm.Modelo;
            impressora.FilialId = filialId;
            impressora.CriadoPor = Util.GetCurrentUser();
            impressora.CriadoEm = Util.GetCurrentDateTime();

            ViewBag.ControllerName = ControllerContext.RouteData.Values["controller"].ToString();
            ViewBag.ActionName = ControllerContext.RouteData.Values["action"].ToString();

            using (DbContextTransaction tr = db.Database.BeginTransaction())
            {
                try
                {
                    db.Impressora.Add(impressora);
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

        // GET: Printer/Edit
        public ActionResult Edit(int id)
        {
            Impressora impressora = GetPrinterByFilial(id);
            if (impressora == null)
            {
                return HttpNotFound();
            }

            ImpressoraViewModel vm = new ImpressoraViewModel();
            vm.Id = impressora.Id;
            vm.Nome = impressora.Nome;
            vm.IP = impressora.IP;
            vm.Porta = impressora.Porta;
            vm.Localizacao = impressora.Localizacao;
            vm.Fabricante = impressora.Fabricante;
            vm.Modelo = impressora.Modelo;
            vm.FilialId = filialId;
            vm.CriadoPor = impressora.CriadoPor;
            vm.CriadoEm = impressora.CriadoEm;
            vm.ModificadoPor = impressora.ModificadoPor;
            vm.ModificadoEm = impressora.ModificadoEm;

            return PartialView("_Edit", vm);
        }

        // POST: Printer/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(ImpressoraViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                return PartialView("_Edit", vm);
            }

            Impressora impressora = GetPrinterByFilial(vm.Id);
            if (impressora == null)
            {
                return HttpNotFound();
            }

            impressora.Nome = vm.Nome;
            impressora.IP = vm.IP;
            impressora.Porta = vm.Porta;
            impressora.Localizacao = vm.Localizacao;
            impressora.Fabricante = vm.Fabricante;
            impressora.Modelo = vm.Modelo;
            impressora.FilialId = filialId;
            impressora.ModificadoPor = Util.GetCurrentUser();
            impressora.ModificadoEm = Util.GetCurrentDateTime();
            db.Entry(impressora).State = EntityState.Modified;

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

        // POST: Printer/Delete (chamada AJAX)
        [HttpPost]
        public ActionResult Delete(int id)
        {
            Impressora impressora = GetPrinterByFilial(id);
            if (impressora == null)
            {
                return Json(new { success = false, msg = "Impressora não encontrada!" });
            }

            using (DbContextTransaction tr = db.Database.BeginTransaction())
            {
                try
                {
                    db.Impressora.Remove(impressora);
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

        public ActionResult GetPrinterData(int id)
        {
            try
            {
                var result = db.Impressora
                    .AsNoTracking()
                    .FirstOrDefault(x => x.Id == id && x.FilialId == filialId);

                if (result == null)
                {
                    return Json(new { success = false, msg = "Impressora não encontrada!" }, JsonRequestBehavior.AllowGet);
                }

                return Json(new { data = result, success = true, }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, msg = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        private Impressora GetPrinterByFilial(int id)
        {
            return db.Impressora.FirstOrDefault(x => x.Id == id && x.FilialId == filialId);
        }

        public ActionResult GetLabelData(int id)
        {
            try
            {
                var result = db.Etiqueta.Find(id);
                return Json(new { data = result, success = true, msg = string.Empty }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, msg = ex.Message }, JsonRequestBehavior.AllowGet);
            }
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
