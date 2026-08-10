using System;
using System.Linq;
using System.Web.Mvc;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Data.Entity.Infrastructure;

using Simplify.Quasar.Models;
using Simplify.Quasar.Areas.RecebimentoApp.ViewModels;
using Simplify.Quasar.Custom;

namespace Simplify.Quasar.Areas.RecebimentoApp.Controllers
{
    //[VerifySessionState]
    [ValidateSession]
    public class FornecedorController : Controller
    {
        Quasar_Entities db = new Quasar_Entities();

        // GET: Fornecedor/Index
        public ActionResult Index()
        {
            var vm = (from f in db.Fornecedor
                      select new FornecedorViewModel
                      {
                          Id = f.Id,
                          CNPJ = f.CNPJ,
                          Nome = f.Nome,
                          Endereco_Cidade = f.Endereco_Cidade,
                          Endereco_UF = f.Endereco_UF,
                          Telefone1 = f.Telefone1,
                          Telefone2 = f.Telefone2,
                          Telefone3 = f.Telefone3,
                          Observacoes = f.Observacoes,
                      }).ToList();

            foreach (var item in vm.Where(x => x.CNPJ != null && x.CNPJ != string.Empty))
            {
                item.CNPJ = Util.FormatCNPJ(item.CNPJ);
            }

            // Obtem lista de permissões mostra botão para criar/alterar/excluir ou não
            ViewBag.Permissoes = Util.GetPermissoes(ControllerContext.RouteData.Values["controller"].ToString());

            return View(vm);
        }

        // GET: Fornecedor/Create
        public ActionResult Create()
        {
            FornecedorViewModel vm = new FornecedorViewModel();
            vm.EstadoDDL = Util.GetEstados(string.Empty);
            return PartialView("_Create", vm);
        }

        // POST: Fornecedor/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(FornecedorViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                vm.EstadoDDL = Util.GetEstados(vm.EstadoUF);
                return PartialView("_Create", vm);
            }

            string cnpj = Util.SemFormatacao(vm.CNPJ);

            // Verifica se o CNPJ informado é valido
            if (!Util.IsValid(cnpj) || cnpj == "00000000000000")
            {
                vm.EstadoDDL = Util.GetEstados(vm.EstadoUF);
                ModelState.AddModelError("CNPJ", "O CNPJ informado não é válido");
                return PartialView("_Create", vm);
            }

            // Verifica se o fornecedor já existe 
            if (db.Fornecedor.Any(p => p.CNPJ == cnpj))
            {
                vm.EstadoDDL = Util.GetEstados(vm.EstadoUF);
                ModelState.AddModelError("CNPJ", "Já existe um Fornecedor cadastrado com este CNPJ");
                return PartialView("_Create", vm);
            }

            Fornecedor fornecedor = new Fornecedor();
            fornecedor.Nome = vm.Nome;
            fornecedor.CNPJ = cnpj;
            fornecedor.StatusId = vm.StatusId ?? 0;
            fornecedor.TipoId = vm.TipoId ?? 0;
            fornecedor.Endereco_Logradouro = vm.Endereco_Logradouro;
            fornecedor.Endereco_Numero = vm.Endereco_Numero;
            fornecedor.Endereco_Complemento = vm.Endereco_Complemento;
            fornecedor.Endereco_Bairro = vm.Endereco_Bairro;
            fornecedor.Endereco_Cidade = vm.Endereco_Cidade;
            fornecedor.Endereco_UF = vm.EstadoUF;
            fornecedor.Endereco_CEP = vm.Endereco_CEP;
            fornecedor.Telefone1 = vm.Telefone1;
            fornecedor.Telefone2 = vm.Telefone2;
            fornecedor.Telefone3 = vm.Telefone3;
            fornecedor.Observacoes = vm.Observacoes;

            ViewBag.ControllerName = ControllerContext.RouteData.Values["controller"].ToString();
            ViewBag.ActionName = ControllerContext.RouteData.Values["action"].ToString();

            using (DbContextTransaction tr = db.Database.BeginTransaction())
            {
                try
                {
                    db.Fornecedor.Add(fornecedor);
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

        // GET: Fornecedor/Edit
        public ActionResult Edit(int id)
        {
            Fornecedor fornecedor = db.Fornecedor.Find(id);
            if (fornecedor == null)
            {
                return HttpNotFound();
            }

            FornecedorViewModel vm = new FornecedorViewModel();
            vm.Id = fornecedor.Id;
            vm.Nome = fornecedor.Nome;
            vm.CNPJ = Util.FormatCNPJ(fornecedor.CNPJ);
            vm.StatusId = fornecedor.StatusId;
            vm.TipoId = fornecedor.TipoId;
            vm.Endereco_Logradouro = fornecedor.Endereco_Logradouro;
            vm.Endereco_Numero = fornecedor.Endereco_Numero;
            vm.Endereco_Complemento = fornecedor.Endereco_Complemento;
            vm.Endereco_Bairro = fornecedor.Endereco_Bairro;
            vm.Endereco_Cidade = fornecedor.Endereco_Cidade;
            vm.Endereco_UF = fornecedor.Endereco_UF;
            vm.EstadoDDL = Util.GetEstados(fornecedor.Endereco_UF);
            vm.Endereco_CEP = fornecedor.Endereco_CEP;
            vm.Telefone1 = fornecedor.Telefone1;
            vm.Telefone2 = fornecedor.Telefone2;
            vm.Telefone3 = fornecedor.Telefone3;
            vm.Observacoes = fornecedor.Observacoes;

            return PartialView("_Edit", vm);
        }

        // POST: Fornecedor/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(FornecedorViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                return PartialView("_Edit", vm);
            }

            Fornecedor fornecedor = db.Fornecedor.Find(vm.Id);
            if (fornecedor == null)
            {
                return HttpNotFound();
            }

            string cnpj = Util.SemFormatacao(vm.CNPJ);

            // Verifica se o CNPJ informado é valido
            if (!Util.IsValid(cnpj) || cnpj == "00000000000000")
            {
                ModelState.AddModelError("CNPJ", "O CNPJ informado não é válido");
                return PartialView("_Edit", vm);
            }

            // Verifica se o Fornecedor informado já existe
            if (fornecedor.CNPJ != cnpj)
            {
                if (db.Fornecedor.Any(p => p.CNPJ == cnpj))
                {
                    ModelState.AddModelError("CNPJ", "Já existe um Fornecedor cadastrado com este CNPJ");
                    return PartialView("_Edit", vm);
                }
            }


            fornecedor.Nome = vm.Nome;
            fornecedor.CNPJ = cnpj;
            fornecedor.StatusId = vm.StatusId ?? 0;
            fornecedor.TipoId = vm.TipoId ?? 0;
            fornecedor.Endereco_Logradouro = vm.Endereco_Logradouro;
            fornecedor.Endereco_Numero = vm.Endereco_Numero;
            fornecedor.Endereco_Complemento = vm.Endereco_Complemento;
            fornecedor.Endereco_Bairro = vm.Endereco_Bairro;
            fornecedor.Endereco_Cidade = vm.Endereco_Cidade;
            fornecedor.Endereco_UF = vm.EstadoUF;
            fornecedor.Endereco_CEP = vm.Endereco_CEP;
            fornecedor.Telefone1 = vm.Telefone1;
            fornecedor.Telefone2 = vm.Telefone2;
            fornecedor.Telefone3 = vm.Telefone3;
            fornecedor.Observacoes = vm.Observacoes;

            db.Entry(fornecedor).State = EntityState.Modified;

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


        // POST: Fornecedor/Delete (chamada AJAX)
        [HttpPost]
        public ActionResult Delete(int id)
        {
            Fornecedor fornecedor = db.Fornecedor.Find(id);
            if (fornecedor == null)
            {
                return Json(new { success = false, msg = "Fornecedor não encontrado!" });
            }

            using (DbContextTransaction tr = db.Database.BeginTransaction())
            {
                try
                {
                    db.Fornecedor.Remove(fornecedor);
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

        public ActionResult GetInfo(string cnpj)
        {
            FornecedorViewModel fornecedor = new FornecedorViewModel();

            try
            {
                fornecedor = (from f in db.Fornecedor
                              where f.CNPJ == cnpj
                              select new FornecedorViewModel
                              {
                                  Id = f.Id,
                                  CNPJ = f.CNPJ,
                                  Nome = f.Nome,
                                  Endereco_Logradouro = f.Endereco_Logradouro,
                                  Endereco_Cidade = f.Endereco_Cidade,
                                  EstadoUF = f.Endereco_UF,
                                  Observacoes = f.Observacoes
                              }).FirstOrDefault();

                JsonResult result = Json(new { data = fornecedor, success = true, msg = string.Empty }, JsonRequestBehavior.AllowGet);
                return result;
            }
            catch (Exception ex)
            {
                JsonResult result = Json(new { data = fornecedor, success = false, msg = ex.Message }, JsonRequestBehavior.AllowGet);
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