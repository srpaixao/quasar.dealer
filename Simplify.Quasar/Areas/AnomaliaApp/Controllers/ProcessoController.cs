using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Web.Mvc;
using Newtonsoft.Json;
using Simplify.Quasar.Areas.AnomaliaApp.Services;
using Simplify.Quasar.Areas.AnomaliaApp.ViewModels;
using Simplify.Quasar.Custom;
using Simplify.Quasar.Models;

namespace Simplify.Quasar.Areas.AnomaliaApp.Controllers
{
    [ValidateSession]
    public class ProcessoController : Controller
    {
        private readonly Quasar_Entities db = new Quasar_Entities();

        public ActionResult Index(string numeroControle = "", string tipo = "", int? statusId = null)
        {
            var vm = new AnomaliaConsultaPageViewModel
            {
                NumeroControle = numeroControle,
                Tipo = tipo,
                StatusId = statusId,
                Processos = CriarConsultaService().ConsultarProcessos(numeroControle, tipo, statusId)
            };
            ViewBag.Permissoes = Util.GetPermissoes("Processo", "AnomaliaApp");
            return View(vm);
        }

        public ActionResult Create()
        {
            ViewBag.Permissoes = Util.GetPermissoes("Processo", "AnomaliaApp");
            return View();
        }

        [HttpGet]
        public ActionResult PesquisarOcorrenciasItem(string termo)
        {
            try
            {
                var itens = CriarConsultaService().PesquisarOcorrenciasItem(termo);
                return Json(new { success = true, data = itens }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Response.StatusCode = 400;
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public ActionResult ObterContextoItem(int notaFiscalItemId, string tipoCodigo)
        {
            try
            {
                var item = CriarConsultaService().ObterContextoItem(notaFiscalItemId, tipoCodigo);
                return Json(new { success = true, data = item }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Response.StatusCode = 400;
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public ActionResult PesquisarItens(string tipoCodigo, string pesquisarPor, string termo)
        {
            try
            {
                var itens = CriarConsultaService().PesquisarItens(tipoCodigo, pesquisarPor, termo);
                return Json(new { success = true, data = itens }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Response.StatusCode = 400;
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Finalizar(string payload)
        {
            try
            {
                var request = JsonConvert.DeserializeObject<AnomaliaProcessoCadastroRequest>(payload ?? string.Empty);
                var result = new AnomaliaService(
                    db, Util.GetCurrentFilial(), Util.GetCurrentUser(), Util.GetCurrentDateTime())
                    .Criar(request);
                return Json(new
                {
                    success = true,
                    message = "Anomalia " + result.NumeroControle + " cadastrada com sucesso.",
                    id = result.AnomaliaId,
                    controle = result.NumeroControle
                });
            }
            catch (JsonException)
            {
                Response.StatusCode = 400;
                return Json(new { success = false, message = "Os dados enviados são inválidos." });
            }
            catch (Exception ex)
            {
                Response.StatusCode = 400;
                return Json(new { success = false, message = ex.Message });
            }
        }

        public ActionResult Detalhe(int id)
        {
            var consulta = CriarConsultaService();
            var processo = consulta.ConsultarProcessos(string.Empty, string.Empty, null)
                .FirstOrDefault(x => x.Id == id);
            if (processo == null) return HttpNotFound();

            return View(new AnomaliaDetalhePageViewModel
            {
                Processo = processo,
                Itens = consulta.ObterItens(id)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ExportarFormulario(int id)
        {
            try
            {
                var arquivos = new AnomaliaFormularioGmService(
                    db,
                    Util.GetCurrentFilial(),
                    Util.GetCurrentUser(),
                    Util.GetCurrentDateTime(),
                    Server.MapPath("~/App_Data/Templates/Formulario Anomalias GM.xls"))
                    .Gerar(id);

                return CriarDownload(arquivos);
            }
            catch (Exception ex)
            {
                TempData["AnomaliaExportacaoErro"] = ex.Message;
                return RedirectToAction("Detalhe", new { id });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ExportarDanificados(int id)
        {
            try
            {
                var arquivos = new AnomaliaFormularioGmService(
                    db,
                    Util.GetCurrentFilial(),
                    Util.GetCurrentUser(),
                    Util.GetCurrentDateTime(),
                    Server.MapPath("~/App_Data/Templates/Formulario Danificados GM.xls"))
                    .GerarDanificados(id);

                return CriarDownload(arquivos);
            }
            catch (Exception ex)
            {
                TempData["AnomaliaExportacaoErro"] = ex.Message;
                return RedirectToAction("Detalhe", new { id });
            }
        }

        private ActionResult CriarDownload(System.Collections.Generic.IList<AnomaliaFormularioArquivo> arquivos)
        {
            if (arquivos.Count == 1)
                return File(arquivos[0].Conteudo, "application/vnd.ms-excel", arquivos[0].NomeArquivo);

            using (var memoria = new MemoryStream())
            {
                using (var zip = new ZipArchive(memoria, ZipArchiveMode.Create, true))
                {
                    foreach (AnomaliaFormularioArquivo arquivo in arquivos)
                    {
                        ZipArchiveEntry entrada = zip.CreateEntry(arquivo.NomeArquivo, CompressionLevel.Optimal);
                        using (Stream destino = entrada.Open())
                            destino.Write(arquivo.Conteudo, 0, arquivo.Conteudo.Length);
                    }
                }

                string primeiroNome = Path.GetFileNameWithoutExtension(arquivos[0].NomeArquivo);
                int separadorLote = primeiroNome.LastIndexOf('-');
                string nomeZip = (separadorLote > 0 ? primeiroNome.Substring(0, separadorLote) : primeiroNome) + ".zip";
                return File(memoria.ToArray(), "application/zip", nomeZip);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AlterarStatusItem(int anomaliaId, int itemId, int novoStatusId, string observacao)
        {
            try
            {
                new AnomaliaStatusService(
                    db, Util.GetCurrentFilial(), Util.GetCurrentUser(), Util.GetCurrentDateTime())
                    .AlterarStatusItem(anomaliaId, itemId, novoStatusId, observacao);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                Response.StatusCode = 400;
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult GerarReenvio(AnomaliaReenvioRequest request)
        {
            try
            {
                var arquivos = new AnomaliaReenvioService(
                    db,
                    Util.GetCurrentFilial(),
                    Util.GetCurrentUser(),
                    Util.GetCurrentDateTime(),
                    new AnomaliaExcelService())
                    .Gerar(request);
                return Json(new { success = true, quantidadeArquivos = arquivos.Count });
            }
            catch (Exception ex)
            {
                Response.StatusCode = 400;
                return Json(new { success = false, message = ex.Message });
            }
        }

        private AnomaliaConsultaService CriarConsultaService()
        {
            return new AnomaliaConsultaService(db, Util.GetCurrentFilial(), Util.GetCurrentDateTime());
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
