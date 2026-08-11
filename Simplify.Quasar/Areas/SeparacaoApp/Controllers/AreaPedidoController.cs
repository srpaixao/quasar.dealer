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
    public class AreaPedidoController : Controller
    {
        private const int ProtectedAreaPedidoId = 1;
        private readonly Quasar_Entities db = new Quasar_Entities();

        public ActionResult Index()
        {
            EnsureAreaRomaneioSchema();

            var areaRomaneios = LoadAreaRomaneios()
                .GroupBy(a => a.Id)
                .ToDictionary(g => g.Key, g => g.First());

            var vm = LoadAreaPedidos()
                .Select(a =>
                {
                    AreaRomaneio areaRomaneio;
                    areaRomaneios.TryGetValue(a.AreaId ?? 0, out areaRomaneio);

                    return new AreaPedidoViewModel
                    {
                        Id = a.Id,
                        UsuarioApollo = a.UsuarioApollo,
                        AreaId = a.AreaId,
                        Area = areaRomaneio != null ? areaRomaneio.Area : a.Area
                    };
                })
                .OrderBy(a => a.UsuarioApollo)
                .ToList();

            return View(vm);
        }

        public ActionResult Create()
        {
            EnsureAreaRomaneioSchema();
            return PartialView("_Create", BuildAreaPedidoViewModel(new AreaPedidoViewModel()));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(AreaPedidoViewModel vm)
        {
            EnsureAreaRomaneioSchema();

            if (AreaPedidoExists(vm.UsuarioApollo, null))
            {
                ModelState.AddModelError("UsuarioApollo", "Ja existe vendedor cadastrado com este nome.");
            }

            if (!ModelState.IsValid)
            {
                return PartialView("_Create", BuildAreaPedidoViewModel(vm));
            }

            var areaRomaneio = GetAreaRomaneioById(vm.AreaId);
            string area = areaRomaneio != null ? areaRomaneio.Area : null;

            using (var tr = db.Database.BeginTransaction())
            {
                try
                {
                    db.Database.ExecuteSqlCommand(
                        @"INSERT INTO AreaPedido (UsuarioApollo, AreaId, Area)
                          VALUES (@p0, @p1, @p2)",
                        (vm.UsuarioApollo ?? string.Empty).Trim(),
                        vm.AreaId,
                        area);
                    tr.Commit();
                    return Json(new { success = true });
                }
                catch (Exception ex)
                {
                    tr.Rollback();
                    ModelState.AddModelError(string.Empty, ex.Message);
                    return PartialView("_Create", BuildAreaPedidoViewModel(vm));
                }
            }
        }

        public ActionResult Edit(int id)
        {
            EnsureAreaRomaneioSchema();

            if (id == ProtectedAreaPedidoId)
            {
                return new HttpStatusCodeResult(403, "O registro padrao 'Nao Identificado' nao pode ser alterado.");
            }

            var entity = GetAreaPedidoById(id);
            if (entity == null)
            {
                return HttpNotFound();
            }

            var vm = new AreaPedidoViewModel
            {
                Id = entity.Id,
                UsuarioApollo = entity.UsuarioApollo,
                AreaId = entity.AreaId,
                Area = entity.Area
            };

            return PartialView("_Edit", BuildAreaPedidoViewModel(vm));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(AreaPedidoViewModel vm)
        {
            EnsureAreaRomaneioSchema();

            if (vm.Id == ProtectedAreaPedidoId)
            {
                return new HttpStatusCodeResult(403, "O registro padrao 'Nao Identificado' nao pode ser alterado.");
            }

            var entity = GetAreaPedidoById(vm.Id);
            if (entity == null)
            {
                return HttpNotFound();
            }

            if (AreaPedidoExists(vm.UsuarioApollo, vm.Id))
            {
                ModelState.AddModelError("UsuarioApollo", "Ja existe vendedor cadastrado com este nome.");
            }

            if (!ModelState.IsValid)
            {
                return PartialView("_Edit", BuildAreaPedidoViewModel(vm));
            }

            var areaRomaneio = GetAreaRomaneioById(vm.AreaId);
            string area = areaRomaneio != null ? areaRomaneio.Area : null;

            using (var tr = db.Database.BeginTransaction())
            {
                try
                {
                    db.Database.ExecuteSqlCommand(
                        @"UPDATE AreaPedido
                          SET UsuarioApollo = @p0,
                              AreaId = @p1,
                              Area = @p2
                          WHERE Id = @p3",
                        (vm.UsuarioApollo ?? string.Empty).Trim(),
                        vm.AreaId,
                        area,
                        vm.Id);
                    tr.Commit();
                    return Json(new { success = true });
                }
                catch (Exception ex)
                {
                    tr.Rollback();
                    ModelState.AddModelError(string.Empty, ex.Message);
                    return PartialView("_Edit", BuildAreaPedidoViewModel(vm));
                }
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            if (id == ProtectedAreaPedidoId)
            {
                return Json(new { success = false, msg = "O registro padrao 'Nao Identificado' nao pode ser excluido." });
            }

            var entity = GetAreaPedidoById(id);
            if (entity == null)
            {
                return Json(new { success = false, msg = "Cadastro nao encontrado." });
            }

            using (var tr = db.Database.BeginTransaction())
            {
                try
                {
                    db.Database.ExecuteSqlCommand("DELETE FROM AreaPedido WHERE Id = @p0", id);
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

        private AreaPedidoViewModel BuildAreaPedidoViewModel(AreaPedidoViewModel vm)
        {
            if (!vm.AreaId.HasValue && !string.IsNullOrWhiteSpace(vm.Area))
            {
                var areaRomaneioByName = LoadAreaRomaneios()
                    .FirstOrDefault(a => string.Equals(
                        (a.Area ?? string.Empty).Trim(),
                        vm.Area.Trim(),
                        StringComparison.OrdinalIgnoreCase));

                if (areaRomaneioByName != null)
                {
                    vm.AreaId = areaRomaneioByName.Id;
                }
            }

            vm.AreaRomaneioDDL = BuildAreaRomaneioDDL(vm.AreaId);

            if (vm.AreaId.HasValue)
            {
                var areaRomaneio = GetAreaRomaneioById(vm.AreaId.Value);
                vm.Area = areaRomaneio != null ? areaRomaneio.Area : vm.Area;
            }
            else
            {
                vm.Area = null;
            }

            return vm;
        }

        private IEnumerable<SelectListItem> BuildAreaRomaneioDDL(int? areaId)
        {
            return LoadAreaRomaneios()
                .OrderBy(a => a.Prioridade ?? int.MaxValue)
                .ThenBy(a => a.Area)
                .Select(a => new SelectListItem
                {
                    Value = a.Id.ToString(),
                    Text = string.Format("{0}{1}", a.Area, a.Prioridade.HasValue ? " - " + a.Prioridade.Value : string.Empty),
                    Selected = areaId.HasValue && areaId.Value == a.Id
                })
                .ToList();
        }

        private bool AreaPedidoExists(string usuarioApollo, int? id)
        {
            string usuarioNormalizado = Normalize(usuarioApollo);
            return LoadAreaPedidos().Any(a =>
                a.Id != (id ?? 0) &&
                Normalize(a.UsuarioApollo) == usuarioNormalizado);
        }

        private List<AreaPedido> LoadAreaPedidos()
        {
            return db.Database.SqlQuery<AreaPedido>(
                @"SELECT Id, UsuarioApollo, AreaId, Area
                  FROM AreaPedido")
                .ToList();
        }

        private AreaPedido GetAreaPedidoById(int id)
        {
            return db.Database.SqlQuery<AreaPedido>(
                @"SELECT Id, UsuarioApollo, AreaId, Area
                  FROM AreaPedido
                  WHERE Id = @p0", id)
                .FirstOrDefault();
        }

        private List<AreaRomaneio> LoadAreaRomaneios()
        {
            return db.Database.SqlQuery<AreaRomaneio>(
                @"SELECT Id, Area, Prioridade, Separar, Conferir, Alocar, Mapa
                  FROM AreaRomaneio")
                .ToList();
        }

        private AreaRomaneio GetAreaRomaneioById(int? id)
        {
            if (!id.HasValue)
            {
                return null;
            }

            return db.Database.SqlQuery<AreaRomaneio>(
                @"SELECT Id, Area, Prioridade, Separar, Conferir, Alocar, Mapa
                  FROM AreaRomaneio
                  WHERE Id = @p0", id.Value)
                .FirstOrDefault();
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
