using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using Simplify.Quasar.Areas.ConfiguracaoApp.ViewModels;
using Simplify.Quasar.Custom;
using Simplify.Quasar.Models;

namespace Simplify.Quasar.Areas.ConfiguracaoApp.Controllers
{
    [ValidateSession]
    public class EquipamentoController : Controller
    {
        private readonly Quasar_Entities db = new Quasar_Entities();
        private readonly int filialId = Util.GetCurrentFilial();

        public ActionResult Index()
        {
            var equipamentos = db.Equipamento
                .AsNoTracking()
                .Where(x => x.FilialId == filialId)
                .OrderBy(x => x.Nome)
                .Select(x => new EquipamentoCadastroViewModel
                {
                    Id = x.Id,
                    Nome = x.Nome,
                    Tipo = x.Tipo,
                    Descricao = x.Descricao,
                    Zonas = x.Zonas,
                    Qtde = x.Qtde,
                    Bloqueado = x.Bloqueado
                })
                .ToList();

            return View(equipamentos);
        }

        [HttpGet]
        public ActionResult Create()
        {
            if (!HasPermission("add"))
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            }

            var model = new EquipamentoCadastroViewModel();
            LoadZones(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(EquipamentoCadastroViewModel model)
        {
            if (!HasPermission("add"))
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            }

            Normalize(model);
            ValidateZones(model);
            if (db.Equipamento.Any(x => x.FilialId == filialId && x.Nome == model.Nome))
            {
                ModelState.AddModelError("Nome", "Já existe um equipamento cadastrado com este nome.");
            }

            if (!ModelState.IsValid)
            {
                LoadZones(model);
                return View(model);
            }

            try
            {
                var equipamento = new Equipamento
                {
                    Nome = model.Nome,
                    Tipo = model.Tipo,
                    Descricao = model.Descricao,
                    Bloqueado = model.Bloqueado,
                    Observacoes = model.Observacoes,
                    Comp = model.Comp,
                    Larg = model.Larg,
                    Altu = model.Altu,
                    Zonas = JoinZones(model.ZonasSelecionadas),
                    Qtde = model.Qtde,
                    FilialId = filialId,
                    CriadoPor = Util.GetCurrentUser(),
                    CriadoEm = Util.GetCurrentDateTime()
                };

                db.Equipamento.Add(equipamento);
                db.SaveChanges();
                SetFlash("success", "Equipamento cadastrado com sucesso.");
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                LoadZones(model);
                ModelState.AddModelError(string.Empty, "Não foi possível cadastrar o equipamento. " + ex.Message);
                return View(model);
            }
        }

        [HttpGet]
        public ActionResult Edit(int id)
        {
            if (!HasPermission("update"))
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            }

            Equipamento equipamento = FindEquipment(id);
            if (equipamento == null)
            {
                return HttpNotFound();
            }

            EquipamentoCadastroViewModel model = ToViewModel(equipamento);
            LoadZones(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(EquipamentoCadastroViewModel model)
        {
            if (!HasPermission("update"))
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            }

            Normalize(model);
            ValidateZones(model);
            Equipamento equipamento = FindEquipment(model.Id);
            if (equipamento == null)
            {
                return HttpNotFound();
            }

            if (db.Equipamento.Any(x =>
                x.FilialId == filialId &&
                x.Id != model.Id &&
                x.Nome == model.Nome))
            {
                ModelState.AddModelError("Nome", "Já existe um equipamento cadastrado com este nome.");
            }

            if (!ModelState.IsValid)
            {
                CopyAudit(model, equipamento);
                LoadZones(model);
                return View(model);
            }

            try
            {
                equipamento.Nome = model.Nome;
                equipamento.Tipo = model.Tipo;
                equipamento.Descricao = model.Descricao;
                equipamento.Bloqueado = model.Bloqueado;
                equipamento.Observacoes = model.Observacoes;
                equipamento.Comp = model.Comp;
                equipamento.Larg = model.Larg;
                equipamento.Altu = model.Altu;
                equipamento.Zonas = JoinZones(model.ZonasSelecionadas);
                equipamento.Qtde = model.Qtde;
                equipamento.ModificadoPor = Util.GetCurrentUser();
                equipamento.ModificadoEm = Util.GetCurrentDateTime();

                db.Entry(equipamento).State = EntityState.Modified;
                db.SaveChanges();
                SetFlash("success", "Equipamento atualizado com sucesso.");
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                CopyAudit(model, equipamento);
                LoadZones(model);
                ModelState.AddModelError(string.Empty, "Não foi possível atualizar o equipamento. " + ex.Message);
                return View(model);
            }
        }

        [HttpGet]
        public ActionResult Details(int id)
        {
            if (!HasPermission("view"))
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            }

            Equipamento equipamento = FindEquipment(id);
            return equipamento == null ? (ActionResult)HttpNotFound() : View(ToViewModel(equipamento));
        }

        [HttpGet]
        public ActionResult Delete(int id)
        {
            if (!HasPermission("delete"))
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            }

            Equipamento equipamento = FindEquipment(id);
            if (equipamento == null)
            {
                return HttpNotFound();
            }

            ViewBag.PossuiVinculos = HasRelatedRecords(id);
            return View(ToViewModel(equipamento));
        }

        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            if (!HasPermission("delete"))
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            }

            Equipamento equipamento = FindEquipment(id);
            if (equipamento == null)
            {
                return DeleteResult(false, "Equipamento não encontrado.", "warning");
            }

            if (HasRelatedRecords(id))
            {
                return DeleteResult(
                    false,
                    "O equipamento não pode ser excluído porque possui registros relacionados.",
                    "warning");
            }

            try
            {
                db.Equipamento.Remove(equipamento);
                db.SaveChanges();
                return DeleteResult(true, "Equipamento excluído com sucesso.", "success");
            }
            catch (Exception ex)
            {
                return DeleteResult(
                    false,
                    "Não foi possível excluir o equipamento. " + ex.Message,
                    "danger");
            }
        }

        private ActionResult DeleteResult(bool success, string message, string type)
        {
            if (Request.IsAjaxRequest())
            {
                return Json(new { success, msg = message, type });
            }

            SetFlash(type, message);
            return RedirectToAction("Index");
        }

        private Equipamento FindEquipment(int id)
        {
            return db.Equipamento.FirstOrDefault(x => x.Id == id && x.FilialId == filialId);
        }

        private bool HasRelatedRecords(int id)
        {
            const string sql = @"
SELECT CASE WHEN
       EXISTS (SELECT 1 FROM dbo.Material WHERE Eqpto1Id = @p0 OR Eqpto2Id = @p0)
    OR EXISTS (SELECT 1 FROM dbo.Locacao WHERE EquipamentoId = @p0)
THEN 1 ELSE 0 END";
            return db.Database.SqlQuery<int>(sql, id).First() == 1;
        }

        private void ValidateZones(EquipamentoCadastroViewModel model)
        {
            if (model.ZonasSelecionadas == null || model.ZonasSelecionadas.Length == 0)
            {
                return;
            }

            string[] zones = model.ZonasSelecionadas
                .Select(x => (x ?? string.Empty).Trim())
                .Where(x => x.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            int validZones = db.Zona.AsNoTracking().Count(x =>
                x.FilialId == filialId &&
                x.Ativo &&
                zones.Contains(x.Codigo));

            if (validZones != zones.Length)
            {
                ModelState.AddModelError("ZonasSelecionadas", "Uma ou mais zonas são inválidas para a filial.");
            }
        }

        private void LoadZones(EquipamentoCadastroViewModel model)
        {
            var selected = (model.ZonasSelecionadas ?? SplitZones(model.Zonas))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            model.ZonasDDL = db.Zona
                .AsNoTracking()
                .Where(x => x.FilialId == filialId && x.Ativo)
                .OrderBy(x => x.Codigo)
                .Select(x => new { x.Codigo, x.Descricao })
                .ToList()
                .Select(x => new SelectListItem
                {
                    Value = x.Codigo,
                    Text = string.IsNullOrWhiteSpace(x.Descricao)
                        ? x.Codigo
                        : x.Codigo + " - " + x.Descricao,
                    Selected = selected.Contains(x.Codigo)
                })
                .ToList();
        }

        private static EquipamentoCadastroViewModel ToViewModel(Equipamento equipamento)
        {
            return new EquipamentoCadastroViewModel
            {
                Id = equipamento.Id,
                Nome = equipamento.Nome,
                Tipo = equipamento.Tipo,
                Descricao = equipamento.Descricao,
                Bloqueado = equipamento.Bloqueado,
                Observacoes = equipamento.Observacoes,
                Comp = equipamento.Comp,
                Larg = equipamento.Larg,
                Altu = equipamento.Altu,
                Zonas = equipamento.Zonas,
                ZonasSelecionadas = SplitZones(equipamento.Zonas),
                Qtde = equipamento.Qtde,
                CriadoPor = equipamento.CriadoPor,
                CriadoEm = equipamento.CriadoEm,
                ModificadoPor = equipamento.ModificadoPor,
                ModificadoEm = equipamento.ModificadoEm
            };
        }

        private static void CopyAudit(EquipamentoCadastroViewModel model, Equipamento equipamento)
        {
            model.CriadoPor = equipamento.CriadoPor;
            model.CriadoEm = equipamento.CriadoEm;
            model.ModificadoPor = equipamento.ModificadoPor;
            model.ModificadoEm = equipamento.ModificadoEm;
        }

        private static void Normalize(EquipamentoCadastroViewModel model)
        {
            model.Nome = (model.Nome ?? string.Empty).Trim();
            model.Tipo = NullIfEmpty(model.Tipo);
            model.Descricao = NullIfEmpty(model.Descricao);
            model.Observacoes = NullIfEmpty(model.Observacoes);
            model.ZonasSelecionadas = (model.ZonasSelecionadas ?? new string[0])
                .Select(x => (x ?? string.Empty).Trim())
                .Where(x => x.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x)
                .ToArray();
        }

        private static string JoinZones(string[] zones)
        {
            return string.Join("; ", zones ?? new string[0]);
        }

        private static string[] SplitZones(string zones)
        {
            return (zones ?? string.Empty)
                .Replace(",", ";")
                .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => x.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private static string NullIfEmpty(string value)
        {
            string normalized = (value ?? string.Empty).Trim();
            return normalized.Length == 0 ? null : normalized;
        }

        private bool HasPermission(string permission)
        {
            return Util.GetPermissoes("Equipamento", "ConfiguracaoApp")
                .Contains("[" + permission + "]");
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
