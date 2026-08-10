
using System;
using System.Linq;
using System.Web.Mvc;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Validation;
using System.Data.Entity;

using Simplify.Quasar.Areas.RecebimentoApp.ViewModels;
using Simplify.Quasar.Custom;
using Simplify.Quasar.Models;
using WebGrease.Css.ImageAssemblyAnalysis.LogModel;
using System.Xml.Linq;
using Newtonsoft.Json;
using Simplify.Quasar.Areas.EstoqueApp.ViewModels;
using System.IO;
using System.Web;

namespace Simplify.Quasar.Areas.RecebimentoApp.Controllers
{
    [ValidateSession]
    public class RetornoInternoController : Controller
    {
        private Quasar_Entities db = new Quasar_Entities();
        int filialId = Util.GetCurrentFilial();

        // GET: RecebimentoApp/RetornoInterno
        public ActionResult Index()
        {
            var vm = (from r in db.RetornoInterno where r.FilialId == filialId
                      orderby r.Id descending
                      select new RetornoInternoViewModel()
                      {
                          Id = r.Id,
                          NrDocumento = r.NrDocumento,
                          TipoDocumentoRetornoNome = (from t in db.TipoDocumentoRetorno
                                                      where t.Id == r.TipoDocumentoRetornoId
                                                      select t.Descricao).FirstOrDefault(),
                          LocalOrigemNome = (from o in db.LocalOrigem
                                             where o.Id == r.LocalOrigemId
                                             select o.Nome).FirstOrDefault(),
                          LocalDestinoNome = (from o in db.LocalDestino
                                              where o.Id == r.LocalDestinoId
                                              select o.Nome).FirstOrDefault(),
                          Responsavel = r.Responsavel,
                          QtdItens = (from i in db.RetornoInternoItem
                                      where i.RetornoInternoId == r.Id
                                      select i).Count(),
                          FinalizadoEm = r.FinalizadoEm
                      }).ToList();

            foreach (var item in vm)
            {
                item.AllowDelete = (from i in db.RetornoInternoItem
                                    where i.RetornoInternoId == item.Id &&
                                          i.QtdArmazenada != null && i.QtdArmazenada > 0
                                    select i).Count() == 0;

                item.StatusDocumentoRetornoNome = item.FinalizadoEm != null ? "Finalizado" :
                                                  item.AllowDelete ? "Lançado" : "Em processamento";
            }

            return View(vm);
        }

        // Cadastrar documento
        public ActionResult Create()
        {
            ViewBag.TipoDocumentoDDL = Util.GetTipoDocumentoRetornoDDL(filialId, null);
            ViewBag.OrigemDDL = Util.GetLocalOrigemDDL(filialId, null);
            ViewBag.DestinoDDL = Util.GetLocalDestinoDDL(filialId, null);

            return View();
        }

        [HttpPost]
        public ActionResult Create(RetornoInternoViewModel documento)
        {
            using (DbContextTransaction tr = db.Database.BeginTransaction())
            {
                try
                {
                    RetornoInterno retorno = new RetornoInterno();
                    retorno.NrDocumento = documento.NrDocumento;
                    retorno.TipoDocumentoRetornoId = documento.TipoDocumentoRetornoId;
                    retorno.LocalOrigemId = documento.LocalOrigemId;
                    retorno.LocalDestinoId = documento.LocalDestinoId;
                    retorno.Responsavel = documento.Responsavel;
                    retorno.Observacoes = documento.Observacoes;
                    retorno.CriadoPor = Util.GetCurrentUser();
                    retorno.CriadoEm = Util.GetCurrentDateTime();
                    retorno.FilialId = Util.GetCurrentFilial();

                    db.RetornoInterno.Add(retorno);
                    db.SaveChanges();

                    foreach (var item in documento._itens)
                    {
                        RetornoInternoItem itemRetorno = new RetornoInternoItem();
                        itemRetorno.RetornoInternoId = retorno.Id;
                        itemRetorno.ItemNr = item.ItemNr;
                        itemRetorno.Quantidade = item.Quantidade;
                        itemRetorno.StatusRetornoId = 3; // Recebido
                        itemRetorno.QtdArmazenada = null;
                        itemRetorno.CriadoPor = Util.GetCurrentUser();
                        itemRetorno.CriadoEm = Util.GetCurrentDateTime();
                        itemRetorno.FilialId = Util.GetCurrentFilial();
                        db.RetornoInternoItem.Add(itemRetorno);
                        db.SaveChanges();
                    }

                    tr.Commit();

                    return Json(new { success = true, message = "Volumes coletados com sucesso!" });
                }
                catch (Exception ex)
                {
                    tr.Rollback();
                    return Json(new { success = false, message = ex.Message });
                }
            }
        }

        // Modificar documento
        public ActionResult Edit(int id)
        {
            RetornoInterno retorno = db.RetornoInterno.Find(id);
            if (retorno == null)
            {
                return HttpNotFound();
            }

            RetornoInternoViewModel vm = new RetornoInternoViewModel();
            vm.Id = retorno.Id;
            vm.NrDocumento = retorno.NrDocumento;

            vm.TipoDocumentoRetornoId = retorno.TipoDocumentoRetornoId;
            vm.TipoDocumentoRetornoDDL = Util.GetTipoDocumentoRetornoDDL((int)retorno.FilialId, retorno.TipoDocumentoRetornoId);

            vm.LocalOrigemId = retorno.LocalOrigemId;
            vm.LocalOrigemDDL = Util.GetLocalOrigemDDL((int)retorno.FilialId, retorno.LocalOrigemId);

            vm.LocalDestinoId = retorno.LocalDestinoId;
            vm.LocalDestinoDDL = Util.GetLocalDestinoDDL((int)retorno.FilialId, retorno.LocalDestinoId);

            vm.Responsavel = retorno.Responsavel;
            vm.Observacoes = retorno.Observacoes;
            vm.CriadoPor = retorno.CriadoPor;
            vm.CriadoEm = retorno.CriadoEm;
            vm.ModificadoPor = retorno.ModificadoPor;
            vm.ModificadoEm = retorno.ModificadoEm;
            vm.FinalizadoEm = retorno.FinalizadoEm;
            vm.FilialId = Util.GetCurrentFilial();

            vm._itens = (from i in db.RetornoInternoItem
                         where i.RetornoInternoId == id
                         select new RetornoInternoItemViewModel()
                         {
                             Id = i.Id,
                             ItemNr = i.ItemNr,
                             Quantidade = i.Quantidade,
                             StatusRetornoId = i.StatusRetornoId,
                             StatusRetornoNome = (from s in db.StatusRetorno
                                                  where s.Id == i.StatusRetornoId
                                                  select s.Nome).FirstOrDefault(),
                             QtdArmazenada = i.QtdArmazenada ?? 0,
                             CriadoPor = i.CriadoPor,
                             CriadoEm = i.CriadoEm,
                             ModificadoPor = i.ModificadoPor,
                             ModificadoEm = i.ModificadoEm
                         }).ToList();

            foreach (var item in vm._itens)
            {
                item.ItemNrDescricao = Util.GetMaterial(item.ItemNr).Descricao;
            }

            return View(vm);
        }

        // POST: RetornoInterno/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(RetornoInternoViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                vm.TipoDocumentoRetornoDDL = Util.GetTipoDocumentoRetornoDDL((int)vm.FilialId, vm.TipoDocumentoRetornoId);
                vm.LocalOrigemDDL = Util.GetLocalOrigemDDL((int)vm.FilialId, vm.LocalOrigemId);
                vm.LocalDestinoDDL = Util.GetLocalDestinoDDL((int)vm.FilialId, vm.LocalDestinoId);
                return View(vm);
            }

            RetornoInterno retorno = db.RetornoInterno.Find(vm.Id);
            if (retorno == null)
            {
                return HttpNotFound();
            }

            using (DbContextTransaction tr = db.Database.BeginTransaction())
            {
                try
                {
                    retorno.TipoDocumentoRetornoId = vm.TipoDocumentoRetornoId;
                    retorno.LocalOrigemId = vm.LocalOrigemId;
                    retorno.LocalDestinoId = vm.LocalDestinoId;
                    retorno.Responsavel = vm.Responsavel;
                    retorno.Observacoes = vm.Observacoes;
                    retorno.ModificadoPor = Util.GetCurrentUser();
                    retorno.ModificadoEm = Util.GetCurrentDateTime();
                    retorno.FilialId = Util.GetCurrentFilial();
                    db.Entry(retorno).State = EntityState.Modified;
                    db.SaveChanges();

                    if (!string.IsNullOrEmpty(vm.JsonItens))
                    {
                        // Obter a lista de itens existentes no banco de dados
                        var itensRetorno = db.RetornoInternoItem
                                             .Where(x => x.RetornoInternoId == retorno.Id)
                                             .ToList();

                        var JsonItens = JsonConvert.DeserializeObject<List<RetornoInternoItemViewModel>>(vm.JsonItens);

                        // Atualizar ou remover itens
                        foreach (var item in itensRetorno)
                        {
                            var itemAtualizado = JsonItens.FirstOrDefault(i => i.Id == item.Id);
                            if (itemAtualizado != null)
                            {
                                item.Quantidade = itemAtualizado.Quantidade;
                                item.ModificadoPor = Util.GetCurrentUser();
                                item.ModificadoEm = Util.GetCurrentDateTime();
                                item.FilialId = Util.GetCurrentFilial();
                                db.Entry(item).State = EntityState.Modified;
                                db.SaveChanges();
                            }
                            else
                            {
                                db.RetornoInternoItem.Remove(item);
                                db.SaveChanges();
                            }
                        }
                    }

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
                    TempData["ErrorDetail"] = msgErro;
                    return View("Error", new HandleErrorInfo(ex, "RetornoInterno", "Edit"));
                }
                catch (Exception ex)
                {
                    tr.Rollback();
                    TempData["ErrorDetail"] = ex.Message;
                    return View("Error", new HandleErrorInfo(ex, "RetornoInterno", "Edit"));
                }
            }

            // Exclui o documento se todos os itens forem excluídos
            var itens = db.RetornoInternoItem.Count(x => x.RetornoInternoId == retorno.Id);

            if (itens == 0)
            {
                var doc = db.RetornoInterno.Find(retorno.Id);
                if (doc != null)
                {
                    db.RetornoInterno.Remove(doc);
                    db.SaveChanges();
                }
            }


            return RedirectToAction("Index");

        }

        // POST: Acao/Delete
        [HttpPost]
        public ActionResult Delete(int id)
        {
            RetornoInterno retorno = db.RetornoInterno.Find(id);
            if (retorno == null)
            {
                return HttpNotFound();
            }

            ViewBag.ControllerName = "RetornoInterno";
            ViewBag.ActionName = "Delete";

            db.Configuration.AutoDetectChangesEnabled = false;
            using (DbContextTransaction tr = db.Database.BeginTransaction())
            {
                try
                {
                    var itens = (from i in db.RetornoInternoItem
                                 where i.RetornoInternoId == retorno.Id
                                 select i).ToList();
                    db.RetornoInternoItem.RemoveRange(itens);
                    db.SaveChanges();

                    db.RetornoInterno.Remove(retorno);
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
                    TempData["ErrorDetail"] = msgErro;
                    return View("Error", new HandleErrorInfo(ex, "RetornoInterno", "Delete"));
                }
                catch (Exception ex)
                {
                    tr.Rollback();
                    TempData["ErrorDetail"] = ex.Message;
                    return View("Error", new HandleErrorInfo(ex, "RetornoInterno", "Delete"));
                }
            }

            return RedirectToAction("Index");

        }

        // Validar documento
        public ActionResult ValidarDocumento(string nrdocumento)
        {
            RetornoInterno documento = new RetornoInterno();

            try
            {
                documento = db.RetornoInterno.Where(x => x.NrDocumento == nrdocumento).FirstOrDefault();
                JsonResult result = Json(new { data = documento, success = true, msg = string.Empty }, JsonRequestBehavior.AllowGet);
                return result;
            }
            catch (Exception ex)
            {
                JsonResult result = Json(new { data = documento, success = false, msg = ex.Message }, JsonRequestBehavior.AllowGet);
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