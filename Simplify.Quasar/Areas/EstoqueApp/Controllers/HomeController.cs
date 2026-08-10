using System;
using System.Linq;
using System.Web.Mvc;

using Simplify.Quasar.Models;
using Simplify.Quasar.Areas.EstoqueApp.ViewModels;
using System.Web;
using System.IO;
using System.Data.Entity;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using Simplify.Quasar.Custom;
using System.Data.Entity.Validation;
using System.Data.Entity.Infrastructure;

using Newtonsoft.Json;

namespace Simplify.Quasar.Areas.EstoqueApp.Controllers
{
    [ValidateSession]
    public class HomeController : Controller
    {
        Quasar_Entities db = new Quasar_Entities();

        int filialId = Util.GetCurrentFilial();

        // GET: Estoque/Index
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public ActionResult GetDataEstoque()
        {
            using (var reader = new StreamReader(Request.InputStream))
            {
                var json = reader.ReadToEnd();
                var model = JsonConvert.DeserializeObject<DataTableAjaxPostModel>(json);

                var draw = model.draw;
                var start = model.start;
                var length = model.length;
                var searchValue = model.search.value;
                var sortColumn = model.order[0].column;
                var sortColumnDir = model.order[0].dir;

                var estoqueData = db.SP_GetItensEstoque(filialId).ToList();
                var recordsTotal = estoqueData.Count();

                // Filtragem
                if (!string.IsNullOrEmpty(searchValue))
                {
                    estoqueData = estoqueData.Where(m => m.ItemNr.ToLower().Contains(searchValue.ToLower()) ||
                                                         m.Descricao.ToLower().Contains(searchValue.ToLower()) ||
                                                         m.Locacao.ToLower().Contains(searchValue.ToLower())).ToList();
                }

                // Ordenação
                switch (sortColumn)
                {
                    case 0:
                        estoqueData = sortColumnDir == "desc" ? estoqueData.OrderByDescending(c => c.ItemNr).ToList() : estoqueData.OrderBy(c => c.ItemNr).ToList();
                        break;
                    case 1:
                        estoqueData = sortColumnDir == "desc" ? estoqueData.OrderByDescending(c => c.Descricao).ToList() : estoqueData.OrderBy(c => c.Descricao).ToList();
                        break;
                    case 2:
                        estoqueData = sortColumnDir == "desc" ? estoqueData.OrderByDescending(c => c.Locacao).ToList() : estoqueData.OrderBy(c => c.Locacao).ToList();
                        break;
                    case 3:
                        estoqueData = sortColumnDir == "desc" ? estoqueData.OrderByDescending(c => c.Saldo).ToList() : estoqueData.OrderBy(c => c.Saldo).ToList();
                        break;
                    case 4:
                        estoqueData = sortColumnDir == "desc" ? estoqueData.OrderByDescending(c => c.Indisponivel).ToList() : estoqueData.OrderBy(c => c.Indisponivel).ToList();
                        break;
                    case 5:
                        estoqueData = sortColumnDir == "desc" ? estoqueData.OrderByDescending(c => c.PedidoPendente).ToList() : estoqueData.OrderBy(c => c.PedidoPendente).ToList();
                        break;
                }

                var filteredData = estoqueData.Skip(start).Take(length).ToList();
                var result = new { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = filteredData };

                return Json(result, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult GetInfoByItem(string item)
        {
            EstoqueViewModel estoque = new EstoqueViewModel();

            try
            {
                estoque = (from e in db.Estoque
                           from m in db.Material.Where(x => x.Codigo == e.ItemNr).DefaultIfEmpty()
                           where e.ItemNr == item && e.FilialId == filialId && e.ItemNr != null
                           select new EstoqueViewModel
                           {
                               Id = e.Id,
                               Locacao = e.Locacao,
                               ItemNr = e.ItemNr,
                               Descricao = m.Descricao,
                               Saldo = e.Saldo,
                               Indisponivel = e.Indisponivel,
                               PedidoPendente = e.PedidoPendente,
                               ValorEstoque = e.ValorEstoque,
                               Range = e.Range,
                               ModificadoEm = e.ModificadoEm
                           }).FirstOrDefault();

                JsonResult result = Json(new { data = estoque, success = true, msg = string.Empty }, JsonRequestBehavior.AllowGet);
                return result;
            }
            catch (Exception ex)
            {
                JsonResult result = Json(new { data = estoque, success = false, msg = ex.Message }, JsonRequestBehavior.AllowGet);
                return result;
            }
        }

        [HttpPost]
        public ActionResult DefinirLocacaoItem(int id, string locacao)
        {
            Estoque estoque = db.Estoque.Find(id);
            if (estoque == null)
            {
                return Json(new { success = false, msg = "Item não encontrado" }, JsonRequestBehavior.AllowGet);
            }

            try
            {
                estoque.Locacao = locacao;
                estoque.ModificadoPor = Util.GetCurrentUser();
                estoque.ModificadoEm = Util.GetCurrentDateTime();
                estoque.FilialId = filialId;

                db.Entry(estoque).State = EntityState.Modified;
                db.SaveChanges();
                return Json(new { success = true, msg = string.Empty }, JsonRequestBehavior.AllowGet);
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