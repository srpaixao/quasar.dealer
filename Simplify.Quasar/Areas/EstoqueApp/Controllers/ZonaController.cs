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
        private const int TipoAreaPermitidoId = 1;
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

            if (!AreaValida(vm.AreaId))
            {
                ModelState.AddModelError("AreaId", "Selecione a área válida.");
            }

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
                        @"INSERT INTO Zona (Codigo, AreaId, Nome, Descricao, QtdeLinha, ProntoDespacho, ValorPedido, QtdeCliente, Ativo, CriadoPor, CriadoEm, ModificadoPor, ModificadoEm, FilialId)
                          VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9, @p10, @p11, @p12, @p13)",
                        vm.Nome?.Trim(),
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
                    return Json(new { success = false, message = ex.GetBaseException().Message });
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

            if (!AreaValida(vm.AreaId))
            {
                ModelState.AddModelError("AreaId", "Selecione a área válida.");
            }

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
                             SET Codigo = @p0,
                                 AreaId = @p1,
                                 Nome = @p2,
                                 Descricao = @p3,
                                 QtdeLinha = @p4,
                                 ProntoDespacho = @p5,
                                 ValorPedido = @p6,
                                 QtdeCliente = @p7,
                                 Ativo = @p8,
                                 ModificadoPor = @p9,
                                 ModificadoEm = @p10
                           WHERE Id = @p11
                             AND FilialId = @p12",
                        vm.Nome?.Trim(),
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
                        vm.Id,
                        filialId);

                    tr.Commit();
                    return Json(new { success = true });
                }
                catch (Exception ex)
                {
                    tr.Rollback();
                    return Json(new { success = false, message = ex.GetBaseException().Message });
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
                    db.Database.ExecuteSqlCommand(
                        "DELETE FROM Zona WHERE Id = @p0 AND FilialId = @p1",
                        id,
                        filialId);
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
                         COALESCE(NULLIF(LTRIM(RTRIM(z.Nome)), ''), z.Codigo) AS Nome,
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
                   WHERE z.FilialId = @p0
                   ORDER BY z.Nome",
                filialId).ToList();
        }

        private IEnumerable<SelectListItem> BuildAreaDDL()
        {
            var areas = db.Area
                .Where(x =>
                    x.TipoAreaId == TipoAreaPermitidoId &&
                    x.FilialId == filialId)
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

        private bool AreaValida(int? areaId)
        {
            return !areaId.HasValue || db.Area.Any(x =>
                x.Id == areaId.Value &&
                x.TipoAreaId == TipoAreaPermitidoId &&
                x.FilialId == filialId);
        }

        private bool ZonaExists(string nome, int? id)
        {
            nome = (nome ?? string.Empty).Trim();
            if (id.HasValue)
            {
                return db.Database.SqlQuery<int>(
                    @"SELECT COUNT(1)
                        FROM Zona
                       WHERE LTRIM(RTRIM(Nome)) = @p0
                         AND FilialId = @p1
                         AND Id <> @p2",
                    nome,
                    filialId,
                    id.Value).FirstOrDefault() > 0;
            }

            return db.Database.SqlQuery<int>(
                @"SELECT COUNT(1)
                    FROM Zona
                   WHERE LTRIM(RTRIM(Nome)) = @p0
                     AND FilialId = @p1",
                nome,
                filialId).FirstOrDefault() > 0;
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
