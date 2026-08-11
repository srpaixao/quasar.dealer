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

        public ActionResult Dashboard(DateTime? dataInicial, DateTime? dataFinal)
        {
            return View(
                "~/Views/Shared/ProcessDashboard.cshtml",
                ProcessDashboardViewModel.Create("Estoque", "EstoqueApp", dataInicial, dataFinal));
        }

        [HttpPost]
        public ActionResult GetDataEstoque()
        {
            using (var reader = new StreamReader(Request.InputStream))
            {
                var json = reader.ReadToEnd();
                var model = JsonConvert.DeserializeObject<DataTableAjaxPostModel>(json);
                if (model == null)
                {
                    return Json(new
                    {
                        draw = 0,
                        recordsFiltered = 0,
                        recordsTotal = 0,
                        data = new object[0]
                    });
                }

                var draw = model.draw;
                var start = Math.Max(model.start, 0);
                var length = model.length > 0 ? Math.Min(model.length, 250) : 25;
                var searchValue = model.search == null
                    ? string.Empty
                    : model.search.value ?? string.Empty;
                var sortColumn = model.order != null && model.order.Length > 0
                    ? model.order[0].column
                    : 0;
                var sortColumnDir = model.order != null && model.order.Length > 0
                    ? model.order[0].dir
                    : "asc";

                var estoqueData = from estoque in db.Estoque.AsNoTracking()
                                  from material in db.Material.Where(x => x.Codigo == estoque.ItemNr).DefaultIfEmpty()
                                  where estoque.FilialId == filialId
                                  select new EstoqueViewModel
                                  {
                                      Id = estoque.Id,
                                      Locacao = estoque.Locacao ?? string.Empty,
                                      ItemNr = estoque.ItemNr,
                                      Descricao = material.Descricao ?? string.Empty,
                                      Saldo = estoque.Saldo ?? 0,
                                      Indisponivel = estoque.Indisponivel ?? 0,
                                      PedidoPendente = estoque.PedidoPendente ?? 0,
                                      ValorEstoque = estoque.ValorEstoque ?? 0,
                                      Range = estoque.Range ?? string.Empty,
                                      ModificadoEm = estoque.ModificadoEm
                                  };
                var recordsTotal = estoqueData.Count();

                // Filtragem
                if (!string.IsNullOrEmpty(searchValue))
                {
                    searchValue = searchValue.Trim();
                    estoqueData = estoqueData.Where(m =>
                        (m.ItemNr ?? string.Empty).Contains(searchValue) ||
                        (m.Descricao ?? string.Empty).Contains(searchValue) ||
                        (m.Locacao ?? string.Empty).Contains(searchValue));
                }

                // Quantidade depois da pesquisa, usada pelo DataTables para
                // recalcular a paginacao e o texto de total filtrado.
                var recordsFiltered = estoqueData.Count();

                // Ordenação
                switch (sortColumn)
                {
                    case 0:
                        estoqueData = sortColumnDir == "desc" ? estoqueData.OrderByDescending(c => c.ItemNr) : estoqueData.OrderBy(c => c.ItemNr);
                        break;
                    case 1:
                        estoqueData = sortColumnDir == "desc" ? estoqueData.OrderByDescending(c => c.Descricao) : estoqueData.OrderBy(c => c.Descricao);
                        break;
                    case 2:
                        estoqueData = sortColumnDir == "desc" ? estoqueData.OrderByDescending(c => c.Locacao) : estoqueData.OrderBy(c => c.Locacao);
                        break;
                    case 3:
                        estoqueData = sortColumnDir == "desc" ? estoqueData.OrderByDescending(c => c.Saldo) : estoqueData.OrderBy(c => c.Saldo);
                        break;
                    case 4:
                        estoqueData = sortColumnDir == "desc" ? estoqueData.OrderByDescending(c => c.Indisponivel) : estoqueData.OrderBy(c => c.Indisponivel);
                        break;
                    case 5:
                        estoqueData = sortColumnDir == "desc" ? estoqueData.OrderByDescending(c => c.PedidoPendente) : estoqueData.OrderBy(c => c.PedidoPendente);
                        break;
                    default:
                        estoqueData = estoqueData.OrderBy(c => c.ItemNr);
                        break;
                }

                var filteredData = estoqueData.Skip(start).Take(length).ToList();
                var result = new { draw = draw, recordsFiltered, recordsTotal, data = filteredData };

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
            string locacaoNormalizada = (locacao ?? string.Empty).Trim();
            Estoque estoque = db.Estoque.FirstOrDefault(x => x.Id == id && x.FilialId == filialId);
            if (estoque == null)
            {
                return Json(new { success = false, msg = "Item não encontrado" }, JsonRequestBehavior.AllowGet);
            }

            if (string.IsNullOrWhiteSpace(locacaoNormalizada))
            {
                return Json(new { success = false, msg = "Selecione uma locação." }, JsonRequestBehavior.AllowGet);
            }

            bool locacaoDisponivel = db.Estoque.Any(x =>
                    x.FilialId == filialId &&
                    x.Locacao != null &&
                    x.Locacao.Trim() == locacaoNormalizada) &&
                !db.Estoque.Any(x =>
                    x.FilialId == filialId &&
                    x.Locacao != null &&
                    x.Locacao.Trim() == locacaoNormalizada &&
                    (x.Saldo ?? 0) > 0);

            if (!locacaoDisponivel)
            {
                return Json(new
                {
                    success = false,
                    msg = "A locação selecionada não está disponível ou possui item com saldo maior que zero."
                }, JsonRequestBehavior.AllowGet);
            }

            try
            {
                estoque.Locacao = locacaoNormalizada;
                estoque.ModificadoPor = Util.GetCurrentUser();
                estoque.ModificadoEm = Util.GetCurrentDateTime();

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
