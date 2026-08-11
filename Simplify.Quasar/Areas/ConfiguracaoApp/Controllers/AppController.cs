using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using Simplify.Quasar.Areas.ConfiguracaoApp.ViewModels;
using Simplify.Quasar.Custom;
using Simplify.Quasar.Models;

namespace Simplify.Quasar.Areas.ConfiguracaoApp.Controllers
{
    [ValidateSession]
    public class AppController : Controller
    {
        private static readonly string[] SystemOnlyParameterNames =
        {
            "RecebimentoUltimoArquivoTransito",
            "MigracaoFusoHorarioExpedicao_20260722"
        };

        private readonly Quasar_Entities db = new Quasar_Entities();
        private readonly int filialId = Util.GetCurrentFilial();

        // GET: Configuracao/App
        public ActionResult Index()
        {
            if (!HasAccess())
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden, "Acesso n\u00E3o autorizado");
            }

            EnsureEstoqueUploadParameters();
            Util.EnsureOnlineUserTimeoutParameters();
            var vm = LoadAppConfigs();
            PrepareViewModels(vm);

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Index(List<AppConfigViewModel> model)
        {
            if (!HasAccess())
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden, "Acesso n\u00E3o autorizado");
            }

            var vm = model ?? new List<AppConfigViewModel>();

            string mensagemValidacao;
            if (!TryValidateProdutividadeConfig(vm, out mensagemValidacao))
            {
                PrepareViewModels(vm);
                TempData["Flash.Type"] = "warning";
                TempData["Flash.Message"] = mensagemValidacao;
                return View(vm.OrderBy(x => x.Id).ToList());
            }

            var ids = vm.Select(x => x.Id).Distinct().ToList();
            var registros = db.AppConfig
                .Where(x =>
                    x.Id != 3 &&
                    x.Id != 8 &&
                    x.FilialId == filialId &&
                    ids.Contains(x.Id) &&
                    !SystemOnlyParameterNames.Contains(x.Nome))
                .ToList();

            if (!TryValidateControleNr(vm, registros, out mensagemValidacao))
            {
                PrepareViewModels(vm);
                TempData["Flash.Type"] = "warning";
                TempData["Flash.Message"] = mensagemValidacao;
                return View(vm.OrderBy(x => x.Id).ToList());
            }

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    foreach (var registro in registros)
                    {
                        var item = vm.FirstOrDefault(x => x.Id == registro.Id);
                        if (item == null)
                        {
                            continue;
                        }

                        registro.Valor = item.Valor;
                    }

                    db.SaveChanges();
                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    PrepareViewModels(vm);
                    TempData["Flash.Type"] = "danger";
                    TempData["Flash.Message"] = "N\u00E3o foi poss\u00EDvel salvar os par\u00E2metros informados.";
                    return View(vm.OrderBy(x => x.Id).ToList());
                }
            }

            TempData["Flash.Type"] = "success";
            TempData["Flash.Message"] = "Par\u00E2metros atualizados com sucesso.";
            return RedirectToAction("Index");
        }

        private List<AppConfigViewModel> LoadAppConfigs()
        {
            return db.AppConfig
                .AsNoTracking()
                .Where(x => x.Id != 3 &&
                            x.Id != 8 &&
                            !SystemOnlyParameterNames.Contains(x.Nome) &&
                            x.FilialId == filialId)
                .Select(x => new AppConfigViewModel
                {
                    Id = x.Id,
                    Nome = x.Nome,
                    Descricao = x.Descricao,
                    Valor = x.Valor,
                    ValorOriginal = x.Valor,
                    FilialId = x.FilialId,
                    FilialNome = db.Empresa
                        .Where(e => e.Id == x.FilialId)
                        .Select(e => e.Nome)
                        .FirstOrDefault()
                })
                .OrderBy(x => x.Id)
                .ToList();
        }

        private void PrepareViewModels(IList<AppConfigViewModel> model)
        {
            var impressoras = db.Impressora
                .AsNoTracking()
                .Where(x => x.FilialId == filialId)
                .OrderBy(x => x.Nome)
                .Select(x => x.Nome)
                .ToList();

            foreach (var item in model)
            {
                item.UsaDDLValor = ShouldUseDropdown(item.Nome);
                item.ValorDDL = item.UsaDDLValor
                    ? BuildValueOptions(item, impressoras)
                    : Enumerable.Empty<SelectListItem>();
            }
        }

        private bool TryValidateProdutividadeConfig(IEnumerable<AppConfigViewModel> model, out string mensagem)
        {
            mensagem = null;

            decimal percentualRomaneios;
            decimal percentualLinhas;
            decimal percentualPecas;

            if (!TryGetParametroValor(model, "Romaneios", out percentualRomaneios) ||
                !TryGetParametroValor(model, "Linhas", out percentualLinhas) ||
                !TryGetParametroValor(model, "Pecas", out percentualPecas))
            {
                mensagem = "A soma dos par\u00E2metros Romaneios, Linhas e Pe\u00E7as deve ser igual a 100%.";
                return false;
            }

            if (Math.Abs((percentualRomaneios + percentualLinhas + percentualPecas) - 100m) > 0.01m)
            {
                mensagem = "A soma dos par\u00E2metros Romaneios, Linhas e Pe\u00E7as deve ser igual a 100%.";
                return false;
            }

            return true;
        }

        private bool TryGetParametroValor(IEnumerable<AppConfigViewModel> model, string nome, out decimal valor)
        {
            valor = 0m;
            string nomeNormalizado = NormalizeParametroNome(nome);
            string valorTexto = model
                .Where(x => NormalizeParametroNome(x.Nome) == nomeNormalizado)
                .Select(x => x.Valor)
                .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(valorTexto))
            {
                return false;
            }

            string valorNormalizado = valorTexto.Replace("%", string.Empty).Trim();
            return decimal.TryParse(valorNormalizado, NumberStyles.Number, new CultureInfo("pt-BR"), out valor)
                || decimal.TryParse(valorNormalizado, NumberStyles.Number, CultureInfo.InvariantCulture, out valor);
        }

        private static string NormalizeParametroNome(string nome)
        {
            return Util.RemoverAcentuacao(nome ?? string.Empty)
                .Trim()
                .ToUpperInvariant();
        }

        private bool TryValidateControleNr(IEnumerable<AppConfigViewModel> model, IEnumerable<AppConfig> registros, out string mensagem)
        {
            mensagem = null;

            var itemControleNr = model.FirstOrDefault(x => NormalizeParametroNome(x.Nome) == "CONTROLENR");
            var registroControleNr = registros.FirstOrDefault(x => NormalizeParametroNome(x.Nome) == "CONTROLENR");

            if (itemControleNr == null || registroControleNr == null)
            {
                return true;
            }

            long valorAtual;
            long novoValor;
            if (!long.TryParse((registroControleNr.Valor ?? string.Empty).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out valorAtual) ||
                !long.TryParse((itemControleNr.Valor ?? string.Empty).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out novoValor))
            {
                mensagem = "O par\u00E2metro \"ControleNr\" n\u00E3o pode ser reduzido. Informe um valor maior ou igual ao valor atual.";
                return false;
            }

            if (novoValor < valorAtual)
            {
                mensagem = "O par\u00E2metro \"ControleNr\" n\u00E3o pode ser reduzido. Informe um valor maior ou igual ao valor atual.";
                return false;
            }

            return true;
        }

        private bool ShouldUseDropdown(string nome)
        {
            string nomeNormalizado = NormalizeParametroNome(nome);
            return nomeNormalizado == "IMPRESSORAPADRAO"
                || nomeNormalizado == "IMPRIMIRDIRETO"
                || nomeNormalizado == "MOVIMENTACAOCORRETA"
                || nomeNormalizado == "LIMPARLOCACAOSALDOZERO";
        }

        private IEnumerable<SelectListItem> BuildValueOptions(AppConfigViewModel item, IList<string> impressoras)
        {
            string nomeNormalizado = NormalizeParametroNome(item.Nome);
            if (nomeNormalizado == "IMPRESSORAPADRAO")
            {
                var options = impressoras
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Select(x => new SelectListItem
                    {
                        Value = x,
                        Text = x,
                        Selected = string.Equals(x, item.Valor, StringComparison.OrdinalIgnoreCase)
                    })
                    .ToList();

                if (!string.IsNullOrWhiteSpace(item.Valor) &&
                    !options.Any(x => string.Equals(x.Value, item.Valor, StringComparison.OrdinalIgnoreCase)))
                {
                    options.Insert(0, new SelectListItem
                    {
                        Value = item.Valor,
                        Text = item.Valor,
                        Selected = true
                    });
                }

                return options;
            }

            if (nomeNormalizado == "IMPRIMIRDIRETO" ||
                nomeNormalizado == "MOVIMENTACAOCORRETA" ||
                nomeNormalizado == "LIMPARLOCACAOSALDOZERO")
            {
                return new[]
                {
                    new SelectListItem
                    {
                        Value = "true",
                        Text = "Sim",
                        Selected = string.Equals(item.Valor, "true", StringComparison.OrdinalIgnoreCase)
                    },
                    new SelectListItem
                    {
                        Value = "false",
                        Text = "Não",
                        Selected = string.Equals(item.Valor, "false", StringComparison.OrdinalIgnoreCase)
                    }
                };
            }

            return Enumerable.Empty<SelectListItem>();
        }

        private void EnsureEstoqueUploadParameters()
        {
            const string nome = "LimparLocacaoSaldoZero";
            bool existe = db.AppConfig.Any(x => x.Nome == nome && x.FilialId == filialId);
            if (existe)
            {
                return;
            }

            db.AppConfig.Add(new AppConfig
            {
                Nome = nome,
                Descricao = "No upload de estoque, limpar a locação do item quando o saldo for zero.",
                Valor = "false",
                CriadoPor = Util.GetCurrentUser(),
                CriadoEm = Util.GetCurrentDateTime(),
                FilialId = filialId
            });
            db.SaveChanges();
        }

        private bool HasAccess()
        {
            int perfilId = Util.GetPerfilId();
            string area = ControllerContext.RouteData.DataTokens["area"] as string;
            return Util.HasMenuAreaAccess(perfilId, area);
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
