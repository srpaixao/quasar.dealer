using System;
using System.Linq;
using System.Web.Mvc;

using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Data.Entity.Infrastructure;

using Simplify.Quasar.Models;
using Simplify.Quasar.Areas.EstoqueApp.ViewModels;
using Simplify.Quasar.Custom;

namespace Simplify.Quasar.Areas.EstoqueApp.Controllers
{
    [ValidateSession]
    public class LocacaoController : Controller
    {
        Quasar_Entities db = new Quasar_Entities();

        int filialId = Util.GetCurrentFilial();

        // GET: Locacao/Index
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult GetData()
        {
            var locacoes = (from l in db.Locacao where l.FilialId == filialId
                            select new LocacaoViewModel
                            {
                                Codigo = l.Codigo,
                                Tipo = l.Tipo,
                                Descricao = l.Descricao,
                                Bloqueado = l.Bloqueado,
                                AreaNome = (from a in db.Area where a.Id == l.AreaId && l.FilialId == filialId select a.Nome).FirstOrDefault(),
                                Curva = l.Curva,
                                Observacoes = l.Observacoes,
                                Status = l.Bloqueado ? "<span class='text-red'>*** Bloqueado ***</span>" : string.Empty
                            }).ToList();

            JsonResult result = Json(new { data = locacoes }, JsonRequestBehavior.AllowGet);
            result.MaxJsonLength = int.MaxValue;
            return result;
        }

        // GET: Locacao/Edit
        public ActionResult Edit(string codigo)
        {
            Locacao locacao = db.Locacao.Where(x => x.Codigo == codigo && x.FilialId == filialId).FirstOrDefault();
            if (locacao == null)
            {
                return HttpNotFound();
            }

            LocacaoViewModel vm = new LocacaoViewModel();
            vm.Codigo = locacao.Codigo;
            vm.Tipo = locacao.Tipo;
            vm.Descricao = locacao.Descricao;
            vm.Bloqueado = locacao.Bloqueado;
            vm.AreaId = locacao.AreaId;
            vm.Curva = locacao.Curva;
            vm.Observacoes = locacao.Observacoes;
            vm.CriadoEm = locacao.CriadoEm;
            vm.CriadoPor = locacao.CriadoPor;
            vm.CriadoPorNome = (from u in db.Usuario where u.FilialId == filialId && u.Login == locacao.CriadoPor select u.Nome).FirstOrDefault();
            vm.ModificadoEm = locacao.ModificadoEm;
            vm.ModificadoPor = locacao.ModificadoPor;
            vm.ModificadoPorNome = (from u in db.Usuario where u.FilialId == filialId && u.Login == locacao.ModificadoPor select u.Nome).FirstOrDefault();

            return PartialView("_Edit", vm);
        }

        // POST: Locacao/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(LocacaoViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                return PartialView("_Edit", vm);
            }

            Locacao locacao = db.Locacao.Where(x => x.Codigo == vm.Codigo && x.FilialId == filialId).FirstOrDefault();
            if (locacao == null)
            {
                return HttpNotFound();
            }

            locacao.Tipo = vm.Tipo;
            locacao.Descricao = vm.Descricao;
            locacao.Bloqueado = vm.Bloqueado;
            locacao.AreaId = vm.AreaId;
            locacao.Curva = vm.Curva;
            locacao.Observacoes = vm.Observacoes;
            locacao.ModificadoEm = Util.GetCurrentDateTime();
            locacao.ModificadoPor = Util.GetCurrentUser();
            locacao.FilialId = filialId;

            db.Entry(locacao).State = EntityState.Modified;

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

        public JsonResult GetLocacoesDisponiveis(string query, int filialId)
        {
            var locacoes = (from l in db.SP_GetLocacoesDisponiveis(query, filialId)
                            select new
                            {
                                codigo = l.Codigo,
                                tipo = l.Tipo,
                                area = l.Area,
                                equipamento =l.Equipamento
                            }).ToList();

            return Json(new { total_results = locacoes.Count, results = locacoes }, JsonRequestBehavior.AllowGet);
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