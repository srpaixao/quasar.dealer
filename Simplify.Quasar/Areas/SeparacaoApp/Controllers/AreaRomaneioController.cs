using Simplify.Quasar.Areas.SeparacaoApp.ViewModels;
using Simplify.Quasar.Custom;
using Simplify.Quasar.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace Simplify.Quasar.Areas.SeparacaoApp.Controllers
{
    [ValidateSession]
    public class AreaRomaneioController : Controller
    {
        private const int ProtectedAreaPedidoId = 1;
        private readonly Quasar_Entities db = new Quasar_Entities();

        public ActionResult Index()
        {
            EnsureAreaRomaneioSchema();

            var vm = LoadAreaRomaneios()
                .Select(a => new AreaRomaneioViewModel
                {
                    Id = a.Id,
                    Area = a.Area,
                    Prioridade = a.Prioridade,
                    Separar = a.Separar ?? false,
                    Conferir = a.Conferir ?? false,
                    Alocar = a.Alocar ?? false,
                    Mapa = a.Mapa ?? false
                })
                .OrderBy(a => a.Prioridade ?? int.MaxValue)
                .ThenBy(a => a.Area)
                .ToList();

            return View(vm);
        }

        public ActionResult Create()
        {
            EnsureAreaRomaneioSchema();
            return PartialView("_Create", new AreaRomaneioViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(AreaRomaneioViewModel vm)
        {
            EnsureAreaRomaneioSchema();

            if (AreaRomaneioExists(vm.Area, null))
            {
                ModelState.AddModelError("Area", "Ja existe area de romaneio cadastrada com este nome.");
            }

            vm.Conferir = vm.Separar && vm.Conferir;

            if (!ModelState.IsValid)
            {
                return PartialView("_Create", vm);
            }

            using (var tr = db.Database.BeginTransaction())
            {
                try
                {
                    db.Database.ExecuteSqlCommand(
                        @"INSERT INTO AreaRomaneio (Area, Prioridade, Separar, Conferir, Alocar, Mapa)
                          VALUES (@p0, @p1, @p2, @p3, @p4, @p5)",
                        (vm.Area ?? string.Empty).Trim(),
                        vm.Prioridade,
                        vm.Separar,
                        vm.Conferir,
                        vm.Alocar,
                        vm.Mapa);
                    tr.Commit();
                    return Json(new { success = true });
                }
                catch (Exception ex)
                {
                    tr.Rollback();
                    ModelState.AddModelError(string.Empty, ex.Message);
                    return PartialView("_Create", vm);
                }
            }
        }

        public ActionResult Edit(int id)
        {
            EnsureAreaRomaneioSchema();

            var entity = GetAreaRomaneioById(id);
            if (entity == null)
            {
                return HttpNotFound();
            }

            var vm = new AreaRomaneioViewModel
            {
                Id = entity.Id,
                Area = entity.Area,
                Prioridade = entity.Prioridade,
                Separar = entity.Separar ?? false,
                Conferir = entity.Conferir ?? false,
                Alocar = entity.Alocar ?? false,
                Mapa = entity.Mapa ?? false
            };

            return PartialView("_Edit", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(AreaRomaneioViewModel vm)
        {
            EnsureAreaRomaneioSchema();

            var entity = GetAreaRomaneioById(vm.Id);
            if (entity == null)
            {
                return HttpNotFound();
            }

            if (AreaRomaneioExists(vm.Area, vm.Id))
            {
                ModelState.AddModelError("Area", "Ja existe area de romaneio cadastrada com este nome.");
            }

            vm.Conferir = vm.Separar && vm.Conferir;

            if (!ModelState.IsValid)
            {
                return PartialView("_Edit", vm);
            }

            string areaAnterior = entity.Area;
            string areaAtual = (vm.Area ?? string.Empty).Trim();

            using (var tr = db.Database.BeginTransaction())
            {
                try
                {
                    db.Database.ExecuteSqlCommand(
                        @"UPDATE AreaRomaneio
                          SET Area = @p0,
                              Prioridade = @p1,
                              Separar = @p2,
                              Conferir = @p3,
                              Alocar = @p4,
                              Mapa = @p5
                          WHERE Id = @p6",
                        areaAtual,
                        vm.Prioridade,
                        vm.Separar,
                        vm.Conferir,
                        vm.Alocar,
                        vm.Mapa,
                        vm.Id);

                    if (!string.Equals(areaAnterior, areaAtual, StringComparison.OrdinalIgnoreCase))
                    {
                        db.Database.ExecuteSqlCommand(
                            @"UPDATE AreaPedido
                              SET Area = @p0
                              WHERE AreaId = @p1
                                AND Id <> @p2",
                            areaAtual,
                            vm.Id,
                            ProtectedAreaPedidoId);
                    }

                    tr.Commit();
                    return Json(new { success = true });
                }
                catch (Exception ex)
                {
                    tr.Rollback();
                    ModelState.AddModelError(string.Empty, ex.Message);
                    return PartialView("_Edit", vm);
                }
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            EnsureAreaRomaneioSchema();

            var entity = GetAreaRomaneioById(id);
            if (entity == null)
            {
                return Json(new { success = false, msg = "Cadastro nao encontrado." });
            }

            if (LoadAreaPedidos().Any(a => a.AreaId == id))
            {
                return Json(new { success = false, msg = "Existe vinculo com area Pedido. Remova os vinculos antes de excluir." });
            }

            using (var tr = db.Database.BeginTransaction())
            {
                try
                {
                    db.Database.ExecuteSqlCommand("DELETE FROM AreaRomaneio WHERE Id = @p0", id);
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ToggleSeparar(int id)
        {
            EnsureAreaRomaneioSchema();

            var entity = GetAreaRomaneioById(id);
            if (entity == null)
            {
                return Json(new { success = false, msg = "Cadastro nao encontrado." });
            }

            bool novoValor = !(entity.Separar ?? false);

            using (var tr = db.Database.BeginTransaction())
            {
                try
                {
                    db.Database.ExecuteSqlCommand(
                        @"UPDATE AreaRomaneio
                          SET Separar = @p0,
                              Conferir = CASE WHEN @p0 = 1 THEN Conferir ELSE 0 END
                          WHERE Id = @p1",
                        novoValor,
                        id);
                    tr.Commit();
                    return Json(new { success = true, value = novoValor });
                }
                catch (Exception ex)
                {
                    tr.Rollback();
                    return Json(new { success = false, msg = ex.Message });
                }
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ToggleConferir(int id)
        {
            EnsureAreaRomaneioSchema();

            var entity = GetAreaRomaneioById(id);
            if (entity == null)
            {
                return Json(new { success = false, msg = "Cadastro nao encontrado." });
            }

            bool novoValor = (entity.Separar ?? false) && !(entity.Conferir ?? false);

            using (var tr = db.Database.BeginTransaction())
            {
                try
                {
                    db.Database.ExecuteSqlCommand(
                        @"UPDATE AreaRomaneio
                          SET Conferir = @p0
                          WHERE Id = @p1",
                        novoValor,
                        id);
                    tr.Commit();
                    return Json(new { success = true, value = novoValor });
                }
                catch (Exception ex)
                {
                    tr.Rollback();
                    return Json(new { success = false, msg = ex.Message });
                }
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ToggleAlocar(int id)
        {
            EnsureAreaRomaneioSchema();

            var entity = GetAreaRomaneioById(id);
            if (entity == null)
            {
                return Json(new { success = false, msg = "Cadastro nao encontrado." });
            }

            bool novoValor = !(entity.Alocar ?? false);

            using (var tr = db.Database.BeginTransaction())
            {
                try
                {
                    db.Database.ExecuteSqlCommand(
                        @"UPDATE AreaRomaneio
                          SET Alocar = @p0
                          WHERE Id = @p1",
                        novoValor,
                        id);
                    tr.Commit();
                    return Json(new { success = true, value = novoValor });
                }
                catch (Exception ex)
                {
                    tr.Rollback();
                    return Json(new { success = false, msg = ex.Message });
                }
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ToggleMapa(int id)
        {
            EnsureAreaRomaneioSchema();

            var entity = GetAreaRomaneioById(id);
            if (entity == null)
            {
                return Json(new { success = false, msg = "Cadastro nao encontrado." });
            }

            bool novoValor = !(entity.Mapa ?? false);

            using (var tr = db.Database.BeginTransaction())
            {
                try
                {
                    db.Database.ExecuteSqlCommand(
                        @"UPDATE AreaRomaneio
                          SET Mapa = @p0
                          WHERE Id = @p1",
                        novoValor,
                        id);
                    tr.Commit();
                    return Json(new { success = true, value = novoValor });
                }
                catch (Exception ex)
                {
                    tr.Rollback();
                    return Json(new { success = false, msg = ex.Message });
                }
            }
        }

        private bool AreaRomaneioExists(string area, int? id)
        {
            string areaNormalizada = Normalize(area);
            return LoadAreaRomaneios().Any(a =>
                a.Id != (id ?? 0) &&
                Normalize(a.Area) == areaNormalizada);
        }

        private List<AreaRomaneio> LoadAreaRomaneios()
        {
            return db.Database.SqlQuery<AreaRomaneio>(
                @"SELECT Id, Area, Prioridade, Separar, Conferir, Alocar, Mapa
                  FROM AreaRomaneio")
                .ToList();
        }

        private AreaRomaneio GetAreaRomaneioById(int id)
        {
            return db.Database.SqlQuery<AreaRomaneio>(
                @"SELECT Id, Area, Prioridade, Separar, Conferir, Alocar, Mapa
                  FROM AreaRomaneio
                  WHERE Id = @p0", id)
                .FirstOrDefault();
        }

        private List<AreaPedido> LoadAreaPedidos()
        {
            return db.Database.SqlQuery<AreaPedido>(
                @"SELECT Id, UsuarioApollo, AreaId, Area
                  FROM AreaPedido")
                .ToList();
        }

        private static string Normalize(string value)
        {
            return (value ?? string.Empty).Trim().ToLowerInvariant();
        }

        private void EnsureAreaRomaneioSchema()
        {
            bool alocarExiste = db.Database.SqlQuery<int>(
                @"SELECT COUNT(1)
                    FROM INFORMATION_SCHEMA.COLUMNS
                   WHERE TABLE_NAME = 'AreaRomaneio'
                     AND COLUMN_NAME = 'Alocar'")
                .FirstOrDefault() > 0;

            if (!alocarExiste)
            {
                db.Database.ExecuteSqlCommand(
                    @"ALTER TABLE AreaRomaneio
                        ADD Alocar BIT NOT NULL
                            CONSTRAINT DF_AreaRomaneio_Alocar_Runtime DEFAULT (0)");
            }

            bool mapaExiste = db.Database.SqlQuery<int>(
                @"SELECT COUNT(1)
                    FROM INFORMATION_SCHEMA.COLUMNS
                   WHERE TABLE_NAME = 'AreaRomaneio'
                     AND COLUMN_NAME = 'Mapa'")
                .FirstOrDefault() > 0;

            if (!mapaExiste)
            {
                db.Database.ExecuteSqlCommand(
                    @"ALTER TABLE AreaRomaneio
                        ADD Mapa BIT NOT NULL
                            CONSTRAINT DF_AreaRomaneio_Mapa_Runtime DEFAULT (0)");

                bool areaPedidoMapaExiste = db.Database.SqlQuery<int>(
                    @"SELECT COUNT(1)
                        FROM INFORMATION_SCHEMA.COLUMNS
                       WHERE TABLE_NAME = 'AreaPedido'
                         AND COLUMN_NAME = 'Mapa'")
                    .FirstOrDefault() > 0;

                if (areaPedidoMapaExiste)
                {
                    db.Database.ExecuteSqlCommand(
                        @"UPDATE ar
                             SET Mapa = 1
                            FROM AreaRomaneio ar
                           WHERE EXISTS
                           (
                               SELECT 1
                                 FROM AreaPedido ap
                                WHERE ap.AreaId = ar.Id
                                  AND ISNULL(ap.Mapa, 0) = 1
                           )");
                }
            }

            db.Database.ExecuteSqlCommand(
                @"UPDATE AreaRomaneio
                     SET Alocar = ISNULL(Alocar, ISNULL(Separar, 0))
                   WHERE Alocar IS NULL");

            db.Database.ExecuteSqlCommand(
                @"UPDATE AreaRomaneio
                     SET Mapa = ISNULL(Mapa, 0)
                   WHERE Mapa IS NULL");
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
