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
    public class TransportadoraController : Controller
    {
        Quasar_Entities db = new Quasar_Entities();

        int filialId = Util.GetCurrentFilial();

        // GET: transportadora/Index
        public ActionResult Index()
        {
            var vm = (from u in db.Transportadora
                      select new TransportadoraViewModel
                      {
                          Id = u.Id,
                          Nome = u.Nome,
                          CNPJ = u.CNPJ,
                          Endereco_Cidade = u.Endereco_Cidade,
                          Endereco_UF = u.Endereco_UF,
                          Nome_Fantasia = u.Nome_Fantasia
                      }).ToList();

            foreach (var item in vm.Where(x => x.CNPJ != null && x.CNPJ != string.Empty))
            {
                item.CNPJ = Util.FormatCNPJ(item.CNPJ);
            }

            // Obtem lista de permissões mostra botão para criar/alterar/excluir ou não
            ViewBag.Permissoes = Util.GetPermissoes(ControllerContext.RouteData.Values["controller"].ToString(), ControllerContext.RouteData.DataTokens["area"] as string);

            return View(vm);
        }

        // GET: Transportadora/Create
        public ActionResult Create()
        {
            TransportadoraViewModel vm = new TransportadoraViewModel();

            vm.EstadoDDL = Util.GetEstadoDDL(null);
            //vm.StatusDDL = Util.GetStatusNotaFiscal(null);
            vm.StatusDDL = Util.GetStatusDocExpedicao(null);
            return PartialView("_Create", vm);
        }

        // POST: Transportadora/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(TransportadoraViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                vm.EstadoDDL = Util.GetEstadoDDL(vm.Endereco_UF);
                vm.StatusDDL = Util.GetStatusNotaFiscal(vm.StatusNotaFiscal);
                return PartialView("_Create", vm);
            }

            string cnpj = Util.SemFormatacao(vm.CNPJ);

            // Verifica se o CNPJ informado é valido
            if (!Util.IsValid(cnpj) || cnpj == "00000000000000")
            {
                vm.EstadoDDL = Util.GetEstadoDDL(vm.Endereco_UF);
                vm.StatusDDL = Util.GetStatusNotaFiscal(vm.StatusNotaFiscal);
                ModelState.AddModelError("CNPJ", "O CNPJ informado não é válido");
                return PartialView("_Create", vm);
            }

            // Verifica se a Transportadora já existe 
            if (db.Transportadora.Any(p => p.CNPJ == cnpj))
            {
                vm.EstadoDDL = Util.GetEstadoDDL(vm.Endereco_UF);
                vm.StatusDDL = Util.GetStatusNotaFiscal(vm.StatusNotaFiscal);
                ModelState.AddModelError("CNPJ", "Já existe uma Transportadora cadastrada com este CNPJ");
                return PartialView("_Create", vm);
            }

            Transportadora transportadora = new Transportadora();
            transportadora.Nome = vm.Nome;
            transportadora.CNPJ = cnpj;
            transportadora.Endereco_Logradouro = vm.Endereco_Logradouro;
            transportadora.Endereco_Numero = vm.Endereco_Numero;
            transportadora.Endereco_Complemento = vm.Endereco_Complemento;
            transportadora.Endereco_Bairro = vm.Endereco_Bairro;
            transportadora.Endereco_Cidade = vm.Endereco_Cidade;
            transportadora.Endereco_UF = vm.Endereco_UF;
            transportadora.Endereco_CEP = vm.Endereco_CEP;
            transportadora.Telefone1 = vm.Telefone1;
            transportadora.Observacoes = vm.Observacoes;
            transportadora.EmitirEtiqueta = vm.EmitirEtiqueta;
            transportadora.EmitirRoteiro = vm.EmitirRoteiro;
            transportadora.StatusNotaFiscal = vm.StatusNotaFiscal;
            transportadora.Nome_Fantasia = vm.Nome_Fantasia;
            transportadora.CriadoPor = Util.GetCurrentUser();
            transportadora.CriadoEm = Util.GetCurrentDateTime();
            transportadora.Finalizar = vm.Finalizar;

            ViewBag.ControllerName = ControllerContext.RouteData.Values["controller"].ToString();
            ViewBag.ActionName = ControllerContext.RouteData.Values["action"].ToString();

            using (DbContextTransaction tr = db.Database.BeginTransaction())
            {
                try
                {
                    db.Transportadora.Add(transportadora);
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

        // GET: Transportadora/Edit
        public ActionResult Edit(int id)
        {
            Transportadora transportadora = db.Transportadora.Find(id);
            if (transportadora == null)
            {
                return HttpNotFound();
            }

            TransportadoraViewModel vm = new TransportadoraViewModel();
            vm.Id = transportadora.Id;
            vm.Nome = transportadora.Nome;
            vm.CNPJ = Util.FormatCNPJ(transportadora.CNPJ);
            vm.Endereco_Logradouro = transportadora.Endereco_Logradouro;
            vm.Endereco_Numero = transportadora.Endereco_Numero;
            vm.Endereco_Complemento = transportadora.Endereco_Complemento;
            vm.Endereco_Bairro = transportadora.Endereco_Bairro;
            vm.Endereco_Cidade = transportadora.Endereco_Cidade;
            vm.Endereco_UF = transportadora.Endereco_UF;
            vm.EstadoDDL = Util.GetEstadoDDL(transportadora.Endereco_UF);
            vm.Endereco_CEP = transportadora.Endereco_CEP;
            vm.Telefone1 = transportadora.Telefone1;
            vm.Observacoes = transportadora.Observacoes;
            vm.EmitirEtiqueta = transportadora.EmitirEtiqueta;
            vm.EmitirRoteiro = transportadora.EmitirRoteiro;
            vm.StatusNotaFiscal = transportadora.StatusNotaFiscal;
            vm.StatusDDL = Util.GetStatusNotaFiscal(transportadora.StatusNotaFiscal);

            vm.CriadoEm = transportadora.CriadoEm;
            vm.CriadoPor = transportadora.CriadoPor;
            vm.CriadoPorNome = (from u in db.Usuario where u.Login == transportadora.CriadoPor select u.Nome).FirstOrDefault();
            vm.ModificadoEm = transportadora.ModificadoEm;
            vm.ModificadoPor = transportadora.ModificadoPor;
            vm.ModificadoPorNome = (from u in db.Usuario where u.Login == transportadora.ModificadoPor select u.Nome).FirstOrDefault();
            vm.Nome_Fantasia = transportadora.Nome_Fantasia;
            vm.Finalizar = transportadora.Finalizar;

            return PartialView("_Edit", vm);
        }

        // POST: Transportadora/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(TransportadoraViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                vm.EstadoDDL = Util.GetEstadoDDL(vm.Endereco_UF);
                vm.StatusDDL = Util.GetStatusNotaFiscal(vm.StatusNotaFiscal);
                return PartialView("_Edit", vm);
            }

            Transportadora transportadora = db.Transportadora.Find(vm.Id);
            if (transportadora == null)
            {
                return HttpNotFound();
            }

            string cnpj = Util.SemFormatacao(vm.CNPJ);

            // Verifica se o CNPJ informado é valido
            if (!Util.IsValid(cnpj) || cnpj == "00000000000000")
            {
                vm.EstadoDDL = Util.GetEstadoDDL(vm.Endereco_UF);
                vm.StatusDDL = Util.GetStatusNotaFiscal(vm.StatusNotaFiscal);
                ModelState.AddModelError("CNPJ", "O CNPJ informado não é válido");
                return PartialView("_Edit", vm);
            }


            // Verifica se a Transportadora já existe
            if (transportadora.CNPJ != cnpj)
            {
                if (db.Transportadora.Any(p => p.CNPJ == cnpj))
                {
                    vm.EstadoDDL = Util.GetEstadoDDL(vm.Endereco_UF);
                    vm.StatusDDL = Util.GetStatusNotaFiscal(vm.StatusNotaFiscal);
                    ModelState.AddModelError("CNPJ", "Já existe uma Transportadora cadastrada com este CNPJ");
                    return PartialView("_Edit", vm);
                }
            }

            transportadora.Nome = vm.Nome;
            transportadora.CNPJ = cnpj;
            transportadora.Endereco_Logradouro = vm.Endereco_Logradouro;
            transportadora.Endereco_Numero = vm.Endereco_Numero;
            transportadora.Endereco_Complemento = vm.Endereco_Complemento;
            transportadora.Endereco_Bairro = vm.Endereco_Bairro;
            transportadora.Endereco_Cidade = vm.Endereco_Cidade;
            transportadora.Endereco_UF = vm.Endereco_UF;
            transportadora.Endereco_CEP = vm.Endereco_CEP;
            transportadora.Telefone1 = vm.Telefone1;
            transportadora.Observacoes = vm.Observacoes;
            transportadora.EmitirEtiqueta = vm.EmitirEtiqueta;
            transportadora.EmitirRoteiro = vm.EmitirRoteiro;
            transportadora.StatusNotaFiscal = vm.StatusNotaFiscal;
            transportadora.ModificadoPor = Util.GetCurrentUser();
            transportadora.ModificadoEm = Util.GetCurrentDateTime();
            transportadora.Nome_Fantasia = vm.Nome_Fantasia;
            transportadora.Finalizar = vm.Finalizar;

            db.Entry(transportadora).State = EntityState.Modified;

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


        // POST: Transportadora/Delete (chamada AJAX)
        [HttpPost]
        public ActionResult Delete(int id)
        {
            Transportadora transp = db.Transportadora.Find(id);
            if (transp == null)
            {
                return Json(new { success = false, msg = "Transportadora não encontrada!" });
            }

            using (DbContextTransaction tr = db.Database.BeginTransaction())
            {
                try
                {
                    db.Transportadora.Remove(transp);
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

        public JsonResult GetTransportadora(string search)
        {
            var result = (from f in db.Transportadora
                          where f.Nome.ToLower().Contains(search.ToLower())
                          orderby f.Nome
                          select new
                          {
                              id = f.Id,
                              text = f.Nome
                          }).ToList();

            return Json(new { items = result }, JsonRequestBehavior.AllowGet);
        }

        //Verificar se transportadora rquer impressão de etiquetas
        public ActionResult CheckImpressao(int id)
        {
            try
            {
                bool imprimir = (from t in db.Transportadora where t.Id == id select t.EmitirEtiqueta).FirstOrDefault();
                return Json(new { success = true, result = imprimir });
            }
            catch (Exception)
            {
                bool imprimir = false;
                return Json(new { success = false, result = imprimir });
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