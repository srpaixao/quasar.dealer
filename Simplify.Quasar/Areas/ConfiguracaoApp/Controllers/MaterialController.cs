using System;
using System.Data.Entity;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using Simplify.Quasar.Areas.ConfiguracaoApp.ViewModels;
using Simplify.Quasar.Custom;
using Simplify.Quasar.Models;

namespace Simplify.Quasar.Areas.ConfiguracaoApp.Controllers
{
    [ValidateSession]
    public class MaterialController : Controller
    {
        private readonly Quasar_Entities db = new Quasar_Entities();
        private readonly int filialId = Util.GetCurrentFilial();

        public ActionResult Index()
        {
            ViewBag.Permissoes = Util.GetPermissoes("Material", "ConfiguracaoApp");
            return View();
        }

        [HttpGet]
        public ActionResult PesquisarMateriais(string term, int? page)
        {
            const int pageSize = 30;
            int currentPage = page.GetValueOrDefault(1);
            if (currentPage < 1)
            {
                currentPage = 1;
            }

            string searchValue = (term ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(searchValue))
            {
                return Json(new
                {
                    results = new object[0],
                    pagination = new { more = false }
                }, JsonRequestBehavior.AllowGet);
            }

            string escapedSearch = searchValue
                .Replace("~", "~~")
                .Replace("%", "~%")
                .Replace("_", "~_")
                .Replace("[", "~[");

            const string sql = @"
SELECT Codigo, Descricao
FROM dbo.Material WITH (READPAST)
WHERE Codigo LIKE @search ESCAPE '~'
ORDER BY Codigo
OFFSET @offset ROWS FETCH NEXT @take ROWS ONLY;";

            var materials = db.Database.SqlQuery<MaterialSearchResult>(
                    sql,
                    new SqlParameter("@search", System.Data.SqlDbType.VarChar, 101)
                    {
                        Value = escapedSearch + "%"
                    },
                    new SqlParameter("@offset", (currentPage - 1) * pageSize),
                    new SqlParameter("@take", pageSize + 1))
                .ToList();

            bool hasMore = materials.Count > pageSize;
            var results = materials
                .Take(pageSize)
                .Select(x => new
                {
                    id = x.Codigo,
                    text = x.Codigo + " - " + x.Descricao
                })
                .ToList();

            return Json(new
            {
                results,
                pagination = new { more = hasMore }
            }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult ObterResumo(string codigo)
        {
            Material material = FindMaterial(codigo);
            if (material == null)
            {
                return Json(new { success = false, msg = "Material não encontrado." }, JsonRequestBehavior.AllowGet);
            }

            MaterialCadastroViewModel model = ToViewModel(material);
            LoadReferenceNames(model);

            return Json(new
            {
                success = true,
                data = new
                {
                    model.Codigo,
                    model.Descricao,
                    model.UN,
                    model.CategoriaProduto,
                    model.ItemApollo,
                    model.ItemCritico,
                    model.Comp,
                    model.Larg,
                    model.Altu,
                    model.Zona1Nome,
                    model.Eqpto1Nome,
                    model.QtdePadrao1,
                    model.Zona2Nome,
                    model.Eqpto2Nome,
                    model.QtdePadrao2,
                    model.Zona3Nome,
                    model.Eqpto3Nome,
                    model.QtdePadrao3
                }
            }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult Create()
        {
            if (!HasPermission("add"))
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            }

            var model = new MaterialCadastroViewModel
            {
                CategoriaProduto = "Diretos",
                QtdePadrao1 = 1
            };
            LoadLookups(model);
            return View(model);
        }

        [HttpGet]
        public ActionResult GetEquipamentosPorZona(int? zonaId)
        {
            if (!zonaId.HasValue)
            {
                return Json(new { results = new object[0] }, JsonRequestBehavior.AllowGet);
            }

            string zonaCodigo = db.Zona
                .AsNoTracking()
                .Where(x => x.Id == zonaId.Value && x.FilialId == filialId && x.Ativo)
                .Select(x => x.Codigo)
                .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(zonaCodigo))
            {
                return Json(new { results = new object[0] }, JsonRequestBehavior.AllowGet);
            }

            var equipamentos = GetEquipamentosByZone(zonaCodigo)
                .Select(x => new
                {
                    id = x.Id,
                    text = FormatEquipment(x.Nome, x.Descricao)
                })
                .ToList();

            return Json(new { results = equipamentos }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(MaterialCadastroViewModel model)
        {
            if (!HasPermission("add"))
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            }

            Normalize(model);
            ValidateDefaultEquipment(model);
            if (db.Material.Any(x => x.Codigo == model.Codigo))
            {
                ModelState.AddModelError("Codigo", "Já existe um material cadastrado com este código.");
            }

            if (!ModelState.IsValid)
            {
                LoadLookups(model);
                return View(model);
            }

            try
            {
                var material = new Material
                {
                    Codigo = model.Codigo,
                    Descricao = model.Descricao,
                    UN = model.UN ?? string.Empty,
                    EmbalagemMin = model.EmbalagemMin,
                    MediaVendas = model.MediaVendas,
                    CustoUnitario = model.CustoUnitario,
                    Comp = model.Comp,
                    Larg = model.Larg,
                    Altu = model.Altu,
                    Curva = model.Curva,
                    ItemCritico = model.ItemCritico,
                    ObsItemCritico = model.ObsItemCritico,
                    CategoriaProduto = model.CategoriaProduto,
                    ItemApollo = model.ItemApollo,
                    Zona1Id = model.Zona1Id,
                    Eqpto1Id = model.Eqpto1Id,
                    QtdePadrao1 = model.QtdePadrao1,
                    Zona2Id = model.Zona2Id,
                    Eqpto2Id = model.Eqpto2Id,
                    QtdePadrao2 = model.QtdePadrao2,
                    Zona3Id = model.Zona3Id,
                    Eqpto3Id = model.Eqpto3Id,
                    QtdePadrao3 = model.QtdePadrao3,
                    CriadoPor = Util.GetCurrentUser(),
                    CriadoEm = Util.GetCurrentDateTime(),
                    FilialId = filialId
                };

                db.Material.Add(material);
                db.SaveChanges();
                SetFlash("success", "Material cadastrado com sucesso.");
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                LoadLookups(model);
                ModelState.AddModelError(string.Empty, "Não foi possível cadastrar o material. " + ex.Message);
                return View(model);
            }
        }

        [HttpGet]
        public ActionResult Edit(string codigo)
        {
            if (!HasPermission("update"))
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            }

            Material material = FindMaterial(codigo);
            if (material == null)
            {
                return HttpNotFound();
            }

            MaterialCadastroViewModel model = ToViewModel(material);
            LoadLookups(model);
            LoadReferenceNames(model);
            ViewBag.PodeExcluir = HasPermission("delete");
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(MaterialCadastroViewModel model)
        {
            if (!HasPermission("update"))
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            }

            Normalize(model);
            ValidateDefaultEquipment(model);
            Material material = FindMaterial(model.Codigo);
            if (material == null)
            {
                return HttpNotFound();
            }

            if (!ModelState.IsValid)
            {
                CopyAudit(model, material);
                LoadLookups(model);
                LoadReferenceNames(model);
                ViewBag.PodeExcluir = HasPermission("delete");
                return View(model);
            }

            try
            {
                material.Descricao = model.Descricao;
                material.UN = model.UN ?? string.Empty;
                material.EmbalagemMin = model.EmbalagemMin;
                material.MediaVendas = model.MediaVendas;
                material.CustoUnitario = model.CustoUnitario;
                material.Comp = model.Comp;
                material.Larg = model.Larg;
                material.Altu = model.Altu;
                material.Curva = model.Curva;
                material.ItemCritico = model.ItemCritico;
                material.ObsItemCritico = model.ObsItemCritico;
                material.CategoriaProduto = model.CategoriaProduto;
                material.ItemApollo = model.ItemApollo;
                material.Zona1Id = model.Zona1Id;
                material.Eqpto1Id = model.Eqpto1Id;
                material.QtdePadrao1 = model.QtdePadrao1;
                material.Zona2Id = model.Zona2Id;
                material.Eqpto2Id = model.Eqpto2Id;
                material.QtdePadrao2 = model.QtdePadrao2;
                material.Zona3Id = model.Zona3Id;
                material.Eqpto3Id = model.Eqpto3Id;
                material.QtdePadrao3 = model.QtdePadrao3;
                material.ModificadoPor = Util.GetCurrentUser();
                material.ModificadoEm = Util.GetCurrentDateTime();

                db.Entry(material).State = EntityState.Modified;
                db.SaveChanges();
                SetFlash("success", "Material atualizado com sucesso.");
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                CopyAudit(model, material);
                LoadLookups(model);
                LoadReferenceNames(model);
                ViewBag.PodeExcluir = HasPermission("delete");
                ModelState.AddModelError(string.Empty, "Não foi possível atualizar o material. " + ex.Message);
                return View(model);
            }
        }

        [HttpGet]
        public ActionResult Details(string codigo)
        {
            if (!HasPermission("view"))
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            }

            Material material = FindMaterial(codigo);
            if (material == null)
            {
                return HttpNotFound();
            }

            MaterialCadastroViewModel model = ToViewModel(material);
            LoadReferenceNames(model);
            return View(model);
        }

        [HttpGet]
        public ActionResult Delete(string codigo)
        {
            if (!HasPermission("delete"))
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            }

            Material material = FindMaterial(codigo);
            if (material == null)
            {
                return HttpNotFound();
            }

            return RedirectToAction("Edit", new { codigo = material.Codigo });
        }

        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(string codigo)
        {
            if (!HasPermission("delete"))
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            }

            Material material = FindMaterial(codigo);
            if (material == null)
            {
                SetFlash("warning", "Material não encontrado.");
                return RedirectToAction("Index");
            }

            if (HasRelatedRecords(material.Codigo))
            {
                SetFlash(
                    "warning",
                    "O material não pode ser excluído porque possui movimentações ou registros relacionados.");
                return RedirectToAction("Index");
            }

            try
            {
                db.Material.Remove(material);
                db.SaveChanges();
                SetFlash("success", "Material excluído com sucesso.");
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                SetFlash("danger", "Não foi possível excluir o material. " + ex.Message);
                return RedirectToAction("Index");
            }
        }

        private Material FindMaterial(string codigo)
        {
            string normalizedCode = (codigo ?? string.Empty).Trim();
            return db.Material.FirstOrDefault(x => x.Codigo == normalizedCode);
        }

        private bool HasRelatedRecords(string codigo)
        {
            const string sql = @"
SELECT CASE WHEN
       EXISTS (SELECT 1 FROM dbo.Estoque WHERE ItemNr = @p0)
    OR EXISTS (SELECT 1 FROM dbo.NotaFiscalItem WHERE Item = @p0)
    OR EXISTS (SELECT 1 FROM dbo.RomaneioItem WHERE ItemNr = @p0)
    OR EXISTS (SELECT 1 FROM dbo.DevolucaoItem WHERE ItemNr = @p0)
    OR EXISTS (SELECT 1 FROM dbo.HistoricoArmazenagem WHERE ItemNr = @p0)
    OR EXISTS (SELECT 1 FROM dbo.HistoricoRecebimento WHERE CodMaterial = @p0)
    OR EXISTS (SELECT 1 FROM dbo.Movimentacao WHERE ItemNr = @p0)
    OR EXISTS (SELECT 1 FROM dbo.MovimentacaoDestino WHERE ItemNr = @p0)
    OR EXISTS (SELECT 1 FROM dbo.RetornoInternoItem WHERE ItemNr = @p0)
THEN 1 ELSE 0 END";

            return db.Database.SqlQuery<int>(sql, codigo).First() == 1;
        }

        private bool HasPermission(string permission)
        {
            string permissions = Util.GetPermissoes("Material", "ConfiguracaoApp");
            return permissions.Contains("[" + permission + "]");
        }

        private static MaterialCadastroViewModel ToViewModel(Material material)
        {
            return new MaterialCadastroViewModel
            {
                Codigo = material.Codigo,
                Descricao = material.Descricao,
                UN = material.UN,
                EmbalagemMin = material.EmbalagemMin,
                MediaVendas = material.MediaVendas,
                CustoUnitario = material.CustoUnitario,
                Comp = material.Comp,
                Larg = material.Larg,
                Altu = material.Altu,
                Curva = material.Curva,
                ItemCritico = material.ItemCritico,
                ObsItemCritico = material.ObsItemCritico,
                CategoriaProduto = material.CategoriaProduto,
                ItemApollo = material.ItemApollo,
                Zona1Id = material.Zona1Id,
                Eqpto1Id = material.Eqpto1Id,
                QtdePadrao1 = material.QtdePadrao1,
                Zona2Id = material.Zona2Id,
                Eqpto2Id = material.Eqpto2Id,
                QtdePadrao2 = material.QtdePadrao2,
                Zona3Id = material.Zona3Id,
                Eqpto3Id = material.Eqpto3Id,
                QtdePadrao3 = material.QtdePadrao3,
                CriadoPor = material.CriadoPor,
                CriadoEm = material.CriadoEm,
                ModificadoPor = material.ModificadoPor,
                ModificadoEm = material.ModificadoEm
            };
        }

        private static void CopyAudit(MaterialCadastroViewModel model, Material material)
        {
            model.CriadoPor = material.CriadoPor;
            model.CriadoEm = material.CriadoEm;
            model.ModificadoPor = material.ModificadoPor;
            model.ModificadoEm = material.ModificadoEm;
        }

        private static void Normalize(MaterialCadastroViewModel model)
        {
            model.Codigo = (model.Codigo ?? string.Empty).Trim();
            model.Descricao = (model.Descricao ?? string.Empty).Trim();
            model.UN = (model.UN ?? string.Empty).Trim();
            model.Curva = NullIfEmpty(model.Curva);
            model.ObsItemCritico = NullIfEmpty(model.ObsItemCritico);
            if (!model.ItemCritico)
            {
                model.ObsItemCritico = null;
            }
            model.CategoriaProduto = NullIfEmpty(model.CategoriaProduto);
            model.ItemApollo = NullIfEmpty(model.ItemApollo);
        }

        private void ValidateDefaultEquipment(MaterialCadastroViewModel model)
        {
            ValidateDefaultEquipmentGroup(
                model.Zona1Id,
                model.Eqpto1Id,
                model.QtdePadrao1,
                "Zona1Id",
                "Eqpto1Id",
                "QtdePadrao1",
                "Unidade",
                true);

            ValidateDefaultEquipmentGroup(
                model.Zona2Id,
                model.Eqpto2Id,
                model.QtdePadrao2,
                "Zona2Id",
                "Eqpto2Id",
                "QtdePadrao2",
                "Caixa",
                false);

            ValidateDefaultEquipmentGroup(
                model.Zona3Id,
                model.Eqpto3Id,
                model.QtdePadrao3,
                "Zona3Id",
                "Eqpto3Id",
                "QtdePadrao3",
                "Palete",
                false);
        }

        private void ValidateDefaultEquipmentGroup(
            int? zonaId,
            int? equipamentoId,
            int? quantidade,
            string zonaField,
            string equipamentoField,
            string quantidadeField,
            string groupName,
            bool required)
        {
            bool hasAnyValue = zonaId.HasValue || equipamentoId.HasValue || quantidade.HasValue;
            if (!required && !hasAnyValue)
            {
                return;
            }

            string levelReference = groupName == "Palete" ? "do Palete" : "da " + groupName;

            if (!zonaId.HasValue)
            {
                ModelState.AddModelError(zonaField, "Selecione a Zona " + levelReference + ".");
            }
            else if (!db.Zona.AsNoTracking().Any(x =>
                x.Id == zonaId.Value &&
                x.FilialId == filialId &&
                x.Ativo))
            {
                ModelState.AddModelError(zonaField, "A zona selecionada não pertence à filial ou está inativa.");
            }

            if (!equipamentoId.HasValue)
            {
                ModelState.AddModelError(equipamentoField, "Selecione o Equipamento " + levelReference + ".");
            }
            else if (!db.Equipamento.AsNoTracking().Any(x =>
                x.Id == equipamentoId.Value &&
                x.FilialId == filialId))
            {
                ModelState.AddModelError(equipamentoField, "O equipamento selecionado não pertence à filial.");
            }
            else if (zonaId.HasValue)
            {
                string zonaCodigo = db.Zona
                    .AsNoTracking()
                    .Where(x =>
                        x.Id == zonaId.Value &&
                        x.FilialId == filialId &&
                        x.Ativo)
                    .Select(x => x.Codigo)
                    .FirstOrDefault();

                if (string.IsNullOrWhiteSpace(zonaCodigo) ||
                    !GetEquipamentosByZone(zonaCodigo).Any(x => x.Id == equipamentoId.Value))
                {
                    ModelState.AddModelError(
                        equipamentoField,
                        "O equipamento selecionado não está associado à zona informada.");
                }
            }

            if (!quantidade.HasValue)
            {
                ModelState.AddModelError(quantidadeField, "Informe a Quantidade por " + groupName + ".");
            }
        }

        private void LoadLookups(MaterialCadastroViewModel model)
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
                    x.Descricao
                })
                .ToList();

            model.Zonas1 = zonas.Select(x => new SelectListItem
                {
                    Value = x.Id.ToString(),
                    Text = FormatZone(x.Codigo, x.Descricao),
                    Selected = model.Zona1Id.HasValue && model.Zona1Id.Value == x.Id
                })
                .ToList();

            model.Zonas2 = zonas.Select(x => new SelectListItem
                {
                    Value = x.Id.ToString(),
                    Text = FormatZone(x.Codigo, x.Descricao),
                    Selected = model.Zona2Id.HasValue && model.Zona2Id.Value == x.Id
                })
                .ToList();

            model.Zonas3 = zonas.Select(x => new SelectListItem
                {
                    Value = x.Id.ToString(),
                    Text = FormatZone(x.Codigo, x.Descricao),
                    Selected = model.Zona3Id.HasValue && model.Zona3Id.Value == x.Id
                })
                .ToList();

            model.Equipamentos1 = BuildEquipmentList(model.Zona1Id, model.Eqpto1Id);
            model.Equipamentos2 = BuildEquipmentList(model.Zona2Id, model.Eqpto2Id);
            model.Equipamentos3 = BuildEquipmentList(model.Zona3Id, model.Eqpto3Id);
        }

        private void LoadReferenceNames(MaterialCadastroViewModel model)
        {
            var zonas = db.Zona
                .AsNoTracking()
                .Where(x => x.FilialId == filialId)
                .Select(x => new { x.Id, x.Codigo, x.Descricao })
                .ToList();
            var equipamentos = db.Equipamento
                .AsNoTracking()
                .Where(x => x.FilialId == filialId)
                .Select(x => new { x.Id, x.Nome, x.Descricao })
                .ToList();

            var zona1 = zonas.FirstOrDefault(x => model.Zona1Id.HasValue && x.Id == model.Zona1Id.Value);
            var zona2 = zonas.FirstOrDefault(x => model.Zona2Id.HasValue && x.Id == model.Zona2Id.Value);
            var zona3 = zonas.FirstOrDefault(x => model.Zona3Id.HasValue && x.Id == model.Zona3Id.Value);
            var equipamento1 = equipamentos.FirstOrDefault(x => model.Eqpto1Id.HasValue && x.Id == model.Eqpto1Id.Value);
            var equipamento2 = equipamentos.FirstOrDefault(x => model.Eqpto2Id.HasValue && x.Id == model.Eqpto2Id.Value);
            var equipamento3 = equipamentos.FirstOrDefault(x => model.Eqpto3Id.HasValue && x.Id == model.Eqpto3Id.Value);

            model.Zona1Nome = FormatZone(zona1 == null ? null : zona1.Codigo, zona1 == null ? null : zona1.Descricao);
            model.Zona2Nome = FormatZone(zona2 == null ? null : zona2.Codigo, zona2 == null ? null : zona2.Descricao);
            model.Zona3Nome = FormatZone(zona3 == null ? null : zona3.Codigo, zona3 == null ? null : zona3.Descricao);
            model.Eqpto1Nome = FormatEquipment(
                equipamento1 == null ? null : equipamento1.Nome,
                equipamento1 == null ? null : equipamento1.Descricao);
            model.Eqpto2Nome = FormatEquipment(
                equipamento2 == null ? null : equipamento2.Nome,
                equipamento2 == null ? null : equipamento2.Descricao);
            model.Eqpto3Nome = FormatEquipment(
                equipamento3 == null ? null : equipamento3.Nome,
                equipamento3 == null ? null : equipamento3.Descricao);
        }

        private System.Collections.Generic.IEnumerable<SelectListItem> BuildEquipmentList(
            int? zoneId,
            int? selectedEquipmentId)
        {
            if (!zoneId.HasValue)
            {
                return new SelectListItem[0];
            }

            string zoneCode = db.Zona
                .AsNoTracking()
                .Where(x => x.Id == zoneId.Value && x.FilialId == filialId && x.Ativo)
                .Select(x => x.Codigo)
                .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(zoneCode))
            {
                return new SelectListItem[0];
            }

            return GetEquipamentosByZone(zoneCode)
                .Select(x => new SelectListItem
                {
                    Value = x.Id.ToString(),
                    Text = FormatEquipment(x.Nome, x.Descricao),
                    Selected = selectedEquipmentId.HasValue && x.Id == selectedEquipmentId.Value
                })
                .ToList();
        }

        private System.Collections.Generic.List<EquipmentLookup> GetEquipamentosByZone(string zoneCode)
        {
            string normalizedZone = (zoneCode ?? string.Empty).Trim();

            return db.Equipamento
                .AsNoTracking()
                .Where(x =>
                    x.FilialId == filialId &&
                    !x.Bloqueado)
                .Select(x => new
                {
                    x.Id,
                    x.Nome,
                    x.Descricao,
                    x.Zonas
                })
                .ToList()
                .Where(x => ContainsZone(x.Zonas, normalizedZone))
                .OrderBy(x => x.Nome)
                .Select(x => new EquipmentLookup
                {
                    Id = x.Id,
                    Nome = x.Nome,
                    Descricao = x.Descricao
                })
                .ToList();
        }

        private static bool ContainsZone(string zones, string zoneCode)
        {
            if (string.IsNullOrWhiteSpace(zoneCode))
            {
                return false;
            }

            return (zones ?? string.Empty)
                .Replace(",", ";")
                .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Any(x => string.Equals(x, zoneCode, StringComparison.OrdinalIgnoreCase));
        }

        private static string FormatZone(string code, string description)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return string.Empty;
            }

            return string.IsNullOrWhiteSpace(description) ? code : code + " - " + description;
        }

        private static string FormatEquipment(string name, string description)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return string.Empty;
            }

            return string.IsNullOrWhiteSpace(description) ? name : name + " - " + description;
        }

        private class EquipmentLookup
        {
            public int Id { get; set; }
            public string Nome { get; set; }
            public string Descricao { get; set; }
        }

        private class MaterialSearchResult
        {
            public string Codigo { get; set; }
            public string Descricao { get; set; }
        }

        private static string NullIfEmpty(string value)
        {
            string normalized = (value ?? string.Empty).Trim();
            return normalized.Length == 0 ? null : normalized;
        }

        private void SetFlash(string type, string message)
        {
            TempData["Flash.Type"] = type;
            TempData["Flash.Message"] = message;
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
