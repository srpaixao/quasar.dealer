using System;
using System.Linq;
using System.Web.Mvc;

using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Data.Entity.Infrastructure;

using Simplify.Quasar.Models;
using Simplify.Quasar.Areas.ComprasApp.ViewModels;
using Simplify.Quasar.Custom;

namespace Simplify.Quasar.Areas.ComprasApp.Controllers
{
    [ValidateSession]
    public class EmpresaController : Controller
    {
        Quasar_Entities db = new Quasar_Entities();

        // GET: Empresa/Index
        public ActionResult Index()
        {
            var vm = (from e in db.Empresa
                      select new EmpresaViewModel
                      {
                          Id = e.Id,
                          Nome = e.Nome,
                          CNPJ = e.CNPJ,
                          Endereco_Cidade = e.Endereco_Cidade,
                          Endereco_UF = e.Endereco_UF,
                      }).ToList();

            foreach (var item in vm.Where(x => x.CNPJ != null && x.CNPJ != string.Empty))
            {
                item.CNPJ = Util.FormatCNPJ(item.CNPJ);
            }

            // Obtem lista de permissões mostra botão para criar/alterar/excluir ou não
            ViewBag.Permissoes = Util.GetPermissoes(ControllerContext.RouteData.Values["controller"].ToString());

            return View(vm);
        }

        // GET: Empresa/Create
        public ActionResult Create()
        {
            EmpresaViewModel vm = new EmpresaViewModel();

            vm.EstadoDDL = Util.GetEstadoDDL(null);

            return PartialView("_Create", vm);
        }

        // POST: Empresa/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(EmpresaViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                vm.EstadoDDL = Util.GetEstadoDDL(vm.Endereco_UF);
                return PartialView("_Create", vm);
            }

            string cnpj = Util.SemFormatacao(vm.CNPJ);

            // Verifica se o CNPJ informado é valido
            if (!Util.IsValid(cnpj) || cnpj == "00000000000000")
            {
                vm.EstadoDDL = Util.GetEstadoDDL(vm.Endereco_UF);
                ModelState.AddModelError("CNPJ", "O CNPJ informado não é válido");
                return PartialView("_Create", vm);
            }

            // Verifica se a empresa já existe 
            if (db.Empresa.Any(p => p.CNPJ == cnpj))
            {
                vm.EstadoDDL = Util.GetEstadoDDL(vm.Endereco_UF);
                ModelState.AddModelError("CNPJ", "Já existe uma Empresa cadastrada com este CNPJ");
                return PartialView("_Create", vm);
            }

            Empresa empresa = new Empresa();
            empresa.Nome = vm.Nome;
            empresa.CNPJ = cnpj;
            empresa.Endereco_Logradouro = vm.Endereco_Logradouro;
            empresa.Endereco_Numero = vm.Endereco_Numero;
            empresa.Endereco_Complemento = vm.Endereco_Complemento;
            empresa.Endereco_Bairro = vm.Endereco_Bairro;
            empresa.Endereco_Cidade = vm.Endereco_Cidade;
            empresa.Endereco_UF = vm.Endereco_UF;
            empresa.Endereco_CEP = vm.Endereco_CEP;
            empresa.Telefone1 = vm.Telefone1;

            ViewBag.ControllerName = ControllerContext.RouteData.Values["controller"].ToString();
            ViewBag.ActionName = ControllerContext.RouteData.Values["action"].ToString();

            using (DbContextTransaction tr = db.Database.BeginTransaction())
            {
                try
                {
                    db.Empresa.Add(empresa);
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

        // GET: Empresa/Edit
        public ActionResult Edit(int id)
        {
            Empresa empresa = db.Empresa.Find(id);
            if (empresa == null)
            {
                return HttpNotFound();
            }

            EmpresaViewModel vm = new EmpresaViewModel();
            vm.Id = empresa.Id;
            vm.Nome = empresa.Nome;
            vm.CNPJ = Util.FormatCNPJ(empresa.CNPJ);
            vm.Endereco_Logradouro = empresa.Endereco_Logradouro;
            vm.Endereco_Numero = empresa.Endereco_Numero;
            vm.Endereco_Complemento = empresa.Endereco_Complemento;
            vm.Endereco_Bairro = empresa.Endereco_Bairro;
            vm.Endereco_Cidade = empresa.Endereco_Cidade;
            vm.Endereco_UF = empresa.Endereco_UF;
            vm.EstadoDDL = Util.GetEstadoDDL(empresa.Endereco_UF);
            vm.Endereco_CEP = empresa.Endereco_CEP;
            vm.Telefone1 = empresa.Telefone1;

            return PartialView("_Edit", vm);
        }

        // POST: Empresa/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(EmpresaViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                vm.EstadoDDL = Util.GetEstadoDDL(vm.Endereco_UF);
                return PartialView("_Edit", vm);
            }

            Empresa empresa = db.Empresa.Find(vm.Id);
            if (empresa == null)
            {
                return HttpNotFound();
            }

            string cnpj = Util.SemFormatacao(vm.CNPJ);

            // Verifica se o CNPJ informado é valido
            if (!Util.IsValid(cnpj) || cnpj == "00000000000000")
            {
                vm.EstadoDDL = Util.GetEstadoDDL(vm.Endereco_UF);
                ModelState.AddModelError("CNPJ", "O CNPJ informado não é válido");
                return PartialView("_Edit", vm);
            }


            // Verifica se a empresa já existe
            if (empresa.CNPJ != cnpj)
            {
                if (db.Empresa.Any(p => p.CNPJ == cnpj))
                {
                    vm.EstadoDDL = Util.GetEstadoDDL(vm.Endereco_UF);
                    ModelState.AddModelError("CNPJ", "Já existe uma Empresa cadastrada com este CNPJ");
                    return PartialView("_Edit", vm);
                }
            }

            empresa.Nome = vm.Nome;
            empresa.CNPJ = cnpj;
            empresa.Endereco_Logradouro = vm.Endereco_Logradouro;
            empresa.Endereco_Numero = vm.Endereco_Numero;
            empresa.Endereco_Complemento = vm.Endereco_Complemento;
            empresa.Endereco_Bairro = vm.Endereco_Bairro;
            empresa.Endereco_Cidade = vm.Endereco_Cidade;
            empresa.Endereco_UF = vm.Endereco_UF;
            empresa.Endereco_CEP = vm.Endereco_CEP;
            empresa.Telefone1 = vm.Telefone1;

            db.Entry(empresa).State = EntityState.Modified;

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

        // POST: Empresa/Delete (chamada AJAX)
        [HttpPost]
        public ActionResult Delete(int id)
        {
            Empresa empresa = db.Empresa.Find(id);
            if (empresa == null)
            {
                return Json(new { success = false, msg = "Empresa não encontrada!" });
            }

            using (DbContextTransaction tr = db.Database.BeginTransaction())
            {
                try
                {
                    db.Empresa.Remove(empresa);
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

        public JsonResult GetEmpresas(string search)
        {
            var result = (from f in db.Empresa
                          where f.Nome.ToLower().Contains(search.ToLower())
                          orderby f.Nome
                          select new
                          {
                              id = f.Id,
                              text = f.Nome
                          }).ToList();

            return Json(new { items = result }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetInfo(string cnpj)
        {
            EmpresaViewModel empresa = new EmpresaViewModel();

            try
            {
                empresa = (from e in db.Empresa
                              where e.CNPJ == cnpj
                              select new EmpresaViewModel
                              {
                                  Id = e.Id,
                                  CNPJ = e.CNPJ,
                                  Nome = e.Nome,
                                  Endereco_Logradouro = e.Endereco_Logradouro,
                                  Observacoes = e.Observacoes
                              }).FirstOrDefault();

                JsonResult result = Json(new { data = empresa, success = true, msg = string.Empty }, JsonRequestBehavior.AllowGet);
                return result;
            }
            catch (Exception ex)
            {
                JsonResult result = Json(new { data = empresa, success = false, msg = ex.Message }, JsonRequestBehavior.AllowGet);
                return result;
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