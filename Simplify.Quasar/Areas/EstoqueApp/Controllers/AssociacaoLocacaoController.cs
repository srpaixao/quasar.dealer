using System;
using System.Data;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using Newtonsoft.Json;
using Simplify.Quasar.Areas.EstoqueApp.ViewModels;
using Simplify.Quasar.Custom;
using Simplify.Quasar.Models;

namespace Simplify.Quasar.Areas.EstoqueApp.Controllers
{
    [ValidateSession]
    public class AssociacaoLocacaoController : Controller
    {
        private readonly Quasar_Entities db = new Quasar_Entities();
        private readonly int filialId = Util.GetCurrentFilial();

        public ActionResult Index()
        {
            var zonas = db.Zona
                .AsNoTracking()
                .Where(x => x.FilialId == filialId && x.Ativo)
                .OrderBy(x => x.Nome)
                .ThenBy(x => x.Codigo)
                .Select(x => new
                {
                    x.Id,
                    x.Codigo,
                    x.Nome,
                    x.Descricao
                })
                .ToList()
                .Select(x => new SelectListItem
                {
                    Value = x.Id.ToString(),
                    Text = string.IsNullOrWhiteSpace(x.Descricao)
                        ? (string.IsNullOrWhiteSpace(x.Nome) || x.Nome == x.Codigo
                            ? x.Codigo
                            : x.Codigo + " - " + x.Nome)
                        : x.Codigo + " - " + x.Descricao
                })
                .ToList();

            ViewBag.Zonas = zonas;
            return View();
        }

        [HttpPost]
        public ActionResult GetData()
        {
            DataTableAjaxPostModel model;
            using (var reader = new StreamReader(Request.InputStream))
            {
                model = JsonConvert.DeserializeObject<DataTableAjaxPostModel>(reader.ReadToEnd());
            }

            if (model == null)
            {
                return Json(new { draw = 0, recordsFiltered = 0, recordsTotal = 0, data = new object[0] });
            }

            if (!model.zonaId.HasValue ||
                !db.Zona.AsNoTracking().Any(x =>
                    x.Id == model.zonaId.Value &&
                    x.FilialId == filialId &&
                    x.Ativo))
            {
                return Json(new
                {
                    draw = model.draw,
                    recordsFiltered = 0,
                    recordsTotal = 0,
                    data = new object[0]
                });
            }

            string filialNome = db.Empresa
                .Where(x => x.Id == filialId)
                .Select(x => x.Nome)
                .FirstOrDefault() ?? filialId.ToString();

            var query = db.Locacao
                .AsNoTracking()
                .Where(x =>
                    x.FilialId == filialId &&
                    x.ZonaId == model.zonaId.Value)
                .Select(x => new
                {
                    x.Codigo,
                    x.Descricao,
                    TotalItens = db.Estoque.Count(e =>
                        e.FilialId == filialId &&
                        e.Locacao != null &&
                        e.Locacao == x.Codigo)
                })
                .Where(x => x.TotalItens == 0);

            int recordsTotal = query.Count();
            string searchValue = model.search != null
                ? (model.search.value ?? string.Empty).Trim()
                : string.Empty;

            if (!string.IsNullOrWhiteSpace(searchValue))
            {
                query = query.Where(x => x.Codigo.Contains(searchValue));
            }

            int recordsFiltered = query.Count();
            int sortColumn = model.order != null && model.order.Length > 0
                ? model.order[0].column
                : 0;
            bool descending = model.order != null &&
                              model.order.Length > 0 &&
                              string.Equals(model.order[0].dir, "desc", StringComparison.OrdinalIgnoreCase);

            if (sortColumn == 2)
            {
                query = descending
                    ? query.OrderByDescending(x => x.TotalItens).ThenByDescending(x => x.Codigo)
                    : query.OrderBy(x => x.TotalItens).ThenBy(x => x.Codigo);
            }
            else
            {
                query = descending
                    ? query.OrderByDescending(x => x.Codigo)
                    : query.OrderBy(x => x.Codigo);
            }

            int pageLength = model.length > 0 ? model.length : 25;
            var locacoes = query
                .Skip(model.start)
                .Take(pageLength)
                .ToList()
                .Select(x => new AssociacaoLocacaoViewModel
                {
                    Locacao = (x.Codigo ?? string.Empty).Trim(),
                    Descricao = (x.Descricao ?? string.Empty).Trim(),
                    Filial = filialNome,
                    Situacao = "Vazia",
                    QuantidadeItens = x.TotalItens
                })
                .ToList();

            return Json(new
            {
                draw = model.draw,
                recordsFiltered,
                recordsTotal,
                data = locacoes
            });
        }

        [HttpGet]
        public ActionResult GetItensDisponiveis(string term, int? page)
        {
            const int pageSize = 30;
            int currentPage = page.GetValueOrDefault(1);
            if (currentPage < 1)
            {
                currentPage = 1;
            }

            string searchValue = (term ?? string.Empty).Trim();
            var query = from estoque in db.Estoque.AsNoTracking()
                        join material in db.Material.AsNoTracking()
                            on estoque.ItemNr equals material.Codigo into materiais
                        from material in materiais.DefaultIfEmpty()
                        where estoque.FilialId == filialId
                           && (estoque.Locacao == null || estoque.Locacao.Trim() == string.Empty)
                        select new
                        {
                            EstoqueId = estoque.Id,
                            estoque.ItemNr,
                            Descricao = material != null ? material.Descricao : string.Empty
                        };

            if (!string.IsNullOrWhiteSpace(searchValue))
            {
                query = query.Where(x =>
                    x.ItemNr.Contains(searchValue) ||
                    x.Descricao.Contains(searchValue));
            }

            var itens = query
                .OrderBy(x => x.ItemNr)
                .Skip((currentPage - 1) * pageSize)
                .Take(pageSize + 1)
                .ToList();

            bool hasMore = itens.Count > pageSize;
            var results = itens
                .Take(pageSize)
                .Select(x => new
                {
                    id = x.EstoqueId,
                    text = string.IsNullOrWhiteSpace(x.Descricao)
                        ? x.ItemNr
                        : x.ItemNr + " - " + x.Descricao
                })
                .ToList();

            return Json(new
            {
                results,
                pagination = new { more = hasMore }
            }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Salvar(
            string locacao,
            int? estoqueId,
            int? quantidadeItensInicial,
            int? zonaId)
        {
            string locacaoNormalizada = (locacao ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(locacaoNormalizada))
            {
                return Json(new { success = false, msg = "Selecione uma locação." });
            }

            if (!estoqueId.HasValue || estoqueId.Value <= 0)
            {
                return Json(new { success = false, msg = "Selecione um item." });
            }

            if (!quantidadeItensInicial.HasValue || quantidadeItensInicial.Value < 0)
            {
                return Json(new { success = false, msg = "Atualize a consulta e selecione novamente a locação." });
            }

            if (!zonaId.HasValue || zonaId.Value <= 0)
            {
                return Json(new { success = false, msg = "Selecione uma zona." });
            }

            using (DbContextTransaction transaction = db.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                try
                {
                    AcquireLocationLock(locacaoNormalizada);

                    Locacao locacaoDb = db.Locacao.FirstOrDefault(x =>
                        x.FilialId == filialId &&
                        x.ZonaId == zonaId.Value &&
                        x.Codigo == locacaoNormalizada);
                    if (locacaoDb == null)
                    {
                        transaction.Rollback();
                        return Json(new { success = false, msg = "Locação não encontrada para a filial." });
                    }

                    int quantidadeItensAtual = db.Estoque.Count(x =>
                        x.FilialId == filialId &&
                        x.Locacao != null &&
                        x.Locacao == locacaoNormalizada);
                    if (quantidadeItensAtual != quantidadeItensInicial.Value)
                    {
                        transaction.Rollback();
                        return Json(new
                        {
                            success = false,
                            msg = "A locação não está mais disponível."
                        });
                    }

                    Estoque estoque = db.Estoque.FirstOrDefault(x =>
                        x.Id == estoqueId.Value &&
                        x.FilialId == filialId);
                    if (estoque == null)
                    {
                        transaction.Rollback();
                        return Json(new { success = false, msg = "Item não encontrado para a filial." });
                    }

                    if (!string.IsNullOrWhiteSpace(estoque.Locacao))
                    {
                        transaction.Rollback();
                        return Json(new
                        {
                            success = false,
                            msg = "O item selecionado não está mais disponível."
                        });
                    }

                    estoque.Locacao = locacaoDb.Codigo;
                    estoque.ModificadoPor = Util.GetCurrentUser();
                    estoque.ModificadoEm = Util.GetCurrentDateTime();

                    db.Entry(estoque).State = EntityState.Modified;
                    db.SaveChanges();
                    transaction.Commit();

                    return Json(new
                    {
                        success = true,
                        msg = "Item associado à locação com sucesso."
                    });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Json(new { success = false, msg = ex.Message });
                }
            }
        }

        private void AcquireLocationLock(string locacao)
        {
            const string sql = @"
DECLARE @Result INT;
EXEC @Result = sys.sp_getapplock
    @Resource = @p0,
    @LockMode = 'Exclusive',
    @LockOwner = 'Transaction',
    @LockTimeout = 15000;
IF @Result < 0
    THROW 50001, 'Não foi possível bloquear a locação para associação.', 1;";

            db.Database.ExecuteSqlCommand(
                sql,
                "Quasar.AssociacaoLocacao." + filialId + "." + locacao);
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
