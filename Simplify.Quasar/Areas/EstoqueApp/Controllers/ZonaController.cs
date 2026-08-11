using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;
using Simplify.Quasar.Areas.EstoqueApp.ViewModels;
using Simplify.Quasar.Custom;
using Simplify.Quasar.Models;

namespace Simplify.Quasar.Areas.EstoqueApp.Controllers
{
    [ValidateSession]
    public class ZonaController : Controller
    {
        private readonly Quasar_Entities db = new Quasar_Entities();
        private readonly int filialId = Util.GetCurrentFilial();

        public ActionResult Index()
        {
            EnsureZonaSchema();

            ViewBag.Permissoes = Util.GetPermissoes(
                ControllerContext.RouteData.Values["controller"].ToString(),
                ControllerContext.RouteData.DataTokens["area"] as string);

            var zonas = LoadZonas();
            return View(zonas);
        }

        public ActionResult Create()
        {
            EnsureZonaSchema();

            var vm = new ZonaViewModel
            {
                Ativo = true,
                AreaDDL = BuildAreaDDL()
            };

            return PartialView("_Create", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(ZonaViewModel vm)
        {
            EnsureZonaSchema();
            vm.AreaDDL = BuildAreaDDL();

            if (!ModelState.IsValid)
            {
                return PartialView("_Create", vm);
            }

            if (ZonaExists(vm.Nome, null))
            {
                ModelState.AddModelError("Nome", "Ja existe zona cadastrada com este nome.");
                return PartialView("_Create", vm);
            }

            var agora = Util.GetCurrentDateTime();
            var usuario = Util.GetCurrentUser();

            using (var tr = db.Database.BeginTransaction())
            {
                try
                {
                    db.Database.ExecuteSqlCommand(
                        @"INSERT INTO Zona (AreaId, Nome, Descricao, QtdeLinha, ProntoDespacho, ValorPedido, QtdeCliente, Ativo, CriadoPor, CriadoEm, ModificadoPor, ModificadoEm, FilialId)
                          VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9, @p10, @p11, @p12)",
                        (object)vm.AreaId ?? DBNull.Value,
                        vm.Nome?.Trim(),
                        (object)vm.Descricao ?? DBNull.Value,
                        (object)vm.QtdeLinha ?? DBNull.Value,
                        vm.ProntoDespacho,
                        (object)vm.ValorPedido ?? DBNull.Value,
                        (object)vm.QtdeCliente ?? DBNull.Value,
                        vm.Ativo,
                        usuario,
                        agora,
                        usuario,
                        agora,
                        filialId);

                    tr.Commit();
                    return Json(new { success = true });
                }
                catch (Exception ex)
                {
                    tr.Rollback();
                    ViewBag.Exception = ex.Message;
                    return PartialView("_Create", vm);
                }
            }
        }

        public ActionResult Edit(int id)
        {
            EnsureZonaSchema();

            var vm = LoadZonas().FirstOrDefault(x => x.Id == id);
            if (vm == null)
            {
                return HttpNotFound();
            }

            vm.AreaDDL = BuildAreaDDL();
            return PartialView("_Edit", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(ZonaViewModel vm)
        {
            EnsureZonaSchema();
            vm.AreaDDL = BuildAreaDDL();

            if (!ModelState.IsValid)
            {
                return PartialView("_Edit", vm);
            }

            if (ZonaExists(vm.Nome, vm.Id))
            {
                ModelState.AddModelError("Nome", "Ja existe zona cadastrada com este nome.");
                return PartialView("_Edit", vm);
            }

            var agora = Util.GetCurrentDateTime();
            var usuario = Util.GetCurrentUser();

            using (var tr = db.Database.BeginTransaction())
            {
                try
                {
                    db.Database.ExecuteSqlCommand(
                        @"UPDATE Zona
                             SET AreaId = @p0,
                                 Nome = @p1,
                                 Descricao = @p2,
                                 QtdeLinha = @p3,
                                 ProntoDespacho = @p4,
                                 ValorPedido = @p5,
                                 QtdeCliente = @p6,
                                 Ativo = @p7,
                                 ModificadoPor = @p8,
                                 ModificadoEm = @p9
                           WHERE Id = @p10",
                        (object)vm.AreaId ?? DBNull.Value,
                        vm.Nome?.Trim(),
                        (object)vm.Descricao ?? DBNull.Value,
                        (object)vm.QtdeLinha ?? DBNull.Value,
                        vm.ProntoDespacho,
                        (object)vm.ValorPedido ?? DBNull.Value,
                        (object)vm.QtdeCliente ?? DBNull.Value,
                        vm.Ativo,
                        usuario,
                        agora,
                        vm.Id);

                    tr.Commit();
                    return Json(new { success = true });
                }
                catch (Exception ex)
                {
                    tr.Rollback();
                    ViewBag.Exception = ex.Message;
                    return PartialView("_Edit", vm);
                }
            }
        }

        [HttpPost]
        public ActionResult Delete(int id)
        {
            EnsureZonaSchema();

            using (var tr = db.Database.BeginTransaction())
            {
                try
                {
                    db.Database.ExecuteSqlCommand("DELETE FROM Zona WHERE Id = @p0", id);
                    tr.Commit();
                    return Json(new { success = true, msg = "Operacao realizada com sucesso." });
                }
                catch (Exception ex)
                {
                    tr.Rollback();
                    return Json(new { success = false, msg = ex.Message });
                }
            }
        }

        private List<ZonaViewModel> LoadZonas()
        {
            return db.Database.SqlQuery<ZonaViewModel>(
                @"SELECT z.Id,
                         z.AreaId,
                         a.Nome AS AreaNome,
                         z.Nome,
                         z.Descricao,
                         z.QtdeLinha,
                         z.ProntoDespacho,
                         z.ValorPedido,
                         z.QtdeCliente,
                         z.Ativo,
                         z.CriadoPor,
                         uc.Nome AS CriadoPorNome,
                         z.CriadoEm,
                         z.ModificadoPor,
                         um.Nome AS ModificadoPorNome,
                         z.ModificadoEm,
                         z.FilialId
                    FROM Zona z
                    LEFT JOIN Area a ON a.Id = z.AreaId
                    LEFT JOIN Usuario uc ON uc.Login = z.CriadoPor AND (uc.FilialId = z.FilialId OR z.FilialId IS NULL)
                    LEFT JOIN Usuario um ON um.Login = z.ModificadoPor AND (um.FilialId = z.FilialId OR z.FilialId IS NULL)
                   WHERE z.FilialId = @p0 OR z.FilialId IS NULL
                   ORDER BY z.Nome",
                filialId).ToList();
        }

        private IEnumerable<SelectListItem> BuildAreaDDL()
        {
            var areas = db.Area
                .Where(x => x.FilialId == filialId)
                .OrderBy(x => x.Nome)
                .ToList()
                .Select(x => new SelectListItem
                {
                    Value = x.Id.ToString(CultureInfo.InvariantCulture),
                    Text = x.Nome
                })
                .ToList();

            areas.Insert(0, new SelectListItem { Value = string.Empty, Text = string.Empty });
            return areas;
        }

        private bool ZonaExists(string nome, int? id)
        {
            nome = (nome ?? string.Empty).Trim();
            return db.Database.SqlQuery<int>(
                @"SELECT COUNT(1)
                    FROM Zona
                   WHERE LTRIM(RTRIM(Nome)) = @p0
                     AND (FilialId = @p1 OR FilialId IS NULL)
                     AND (@p2 IS NULL OR Id <> @p2)",
                nome,
                filialId,
                (object)id ?? DBNull.Value).FirstOrDefault() > 0;
        }

        private void EnsureZonaSchema()
        {
            var requiredColumns = new[]
            {
                "AreaId",
                "Nome",
                "Ativo"
            };

            foreach (var column in requiredColumns)
            {
                var exists = db.Database.SqlQuery<int>(
                    @"SELECT COUNT(1)
                        FROM INFORMATION_SCHEMA.COLUMNS
                       WHERE TABLE_NAME = 'Zona'
                         AND COLUMN_NAME = @p0",
                    column).FirstOrDefault() > 0;

                if (!exists)
                {
                    throw new InvalidOperationException(
                        "Schema da tabela Zona desatualizado. Execute o script docs/sql/20260702_AlocacaoPedidosZona.sql.");
                }
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
