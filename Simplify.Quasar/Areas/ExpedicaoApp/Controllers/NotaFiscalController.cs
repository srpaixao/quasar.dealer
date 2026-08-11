using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Validation;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using Microsoft.Reporting.WebForms;

using Simplify.Quasar.Areas.ExpedicaoApp.ViewModels;
using Simplify.Quasar.Custom;
using Simplify.Quasar.Models;

namespace Simplify.Quasar.Areas.ExpedicaoApp.Controllers
{
    [ValidateSession]
    public class NotaFiscalController : Controller
    {
        private const int TransportadoraImportTimeoutSeconds = 300;
        private const int TransportadoraContatoBatchSize = 500;
        private static readonly string[] Code128Patterns =
        {
            "212222", "222122", "222221", "121223", "121322", "131222", "122213", "122312", "132212", "221213",
            "221312", "231212", "112232", "122132", "122231", "113222", "123122", "123221", "223211", "221132",
            "221231", "213212", "223112", "312131", "311222", "321122", "321221", "312212", "322112", "322211",
            "212123", "212321", "232121", "111323", "131123", "131321", "112313", "132113", "132311", "211313",
            "231113", "231311", "112133", "112331", "132131", "113123", "113321", "133121", "313121", "211331",
            "231131", "213113", "213311", "213131", "311123", "311321", "331121", "312113", "312311", "332111",
            "314111", "221411", "431111", "111224", "111422", "121124", "121421", "141122", "141221", "112214",
            "112412", "122114", "122411", "142112", "142211", "241211", "221114", "413111", "241112", "134111",
            "111242", "121142", "121241", "114212", "124112", "124211", "411212", "421112", "421211", "212141",
            "214121", "412121", "111143", "111341", "131141", "114113", "114311", "411113", "411311", "113141",
            "114131", "311141", "411131", "211412", "211214", "211232", "2331112"
        };

        Quasar_Entities db = new Quasar_Entities();

        string current_user = Util.GetCurrentUser();
        int filialId = Util.GetCurrentFilial();

        int periodo;
        DateTime inicio;

        public NotaFiscalController()
        {
            periodo = Util.GetPeriodoExpedicao();
            inicio = Util.GetCurrentDateTime().AddDays(-periodo);
        }

        // GET: ExpedicaoApp/NotaFiscal/Print
        public ActionResult Print()
        {
            NotaFiscalViewModel vm = new NotaFiscalViewModel
            {
                ImprimirDireto = IsTransportadoraDirectPrintEnabled(),
                PrinterServerIP = GetAppConfigValueByName("PrinterServerIP"),
                PrinterServerPort = GetAppConfigValueByName("PrinterServerPort")
            };

            if (vm.ImprimirDireto)
            {
                TransportadoraDirectPrinterSettings impressoraPadrao = ResolveTransportadoraDefaultPrinter();
                vm.ImpressoraPadraoNome = impressoraPadrao != null ? impressoraPadrao.PrinterName : string.Empty;
                vm.ImpressoraPadraoIP = impressoraPadrao != null ? impressoraPadrao.PrinterTarget : string.Empty;
                vm.ImpressoraPadraoPorta = impressoraPadrao != null ? impressoraPadrao.PrinterPort : string.Empty;
            }

            return View(vm);
        }

        // GET: ExpedicaoApp/NotaFiscal
        public ActionResult Index()
        {
            NotaFiscalViewModel vm = new NotaFiscalViewModel();
            IQueryable<DocExpedicao> documentosPeriodo = BuildDocExpedicaoPeriodoQuery();

            vm.TotalLancamento = documentosPeriodo.Count(x => x.StatusId == 1);
            vm.TotalAguardandoLancamento = documentosPeriodo.Count(x => x.StatusId == 1 && x.TipoMovimentoId == null);
            vm.TotalEntrega = documentosPeriodo.Count(x => x.StatusId == 2 && x.TipoMovimentoId == 1);
            vm.TotalRetirada = documentosPeriodo.Count(x => x.StatusId == 2 && x.TipoMovimentoId == 2);
            vm.TotalGarantia = documentosPeriodo.Count(x => x.StatusId == 2 && x.TipoMovimentoId == 3);
            vm.TotalTroca = documentosPeriodo.Count(x => x.StatusId == 2 && x.TipoMovimentoId == 4);
            vm.TotalRoteiro = documentosPeriodo.Count(x => x.StatusId == 3 && x.RoteiroImpresso == false);
            vm.TotalFinalizado = documentosPeriodo.Count(x => x.StatusId == 4);
            vm.TotalEmTransito = documentosPeriodo.Count(x => x.StatusId == 2);
            vm.TotalEmEspera = documentosPeriodo.Count(x => x.StatusId == 1002);

            vm.ZPL_Etiqueta = (from e in db.Etiqueta where e.Nome == "Expedicao" select e.ZPL).FirstOrDefault();

            //if (vm.ZPL_Etiqueta == null)
            //{
            //    return HttpNotFound();
            //}

            //string local = (from a in db.AppConfig where a.Nome == "local" select a.Valor).FirstOrDefault();
            vm.PrinterServerIP = GetAppConfigValueByName("PrinterServerIP");
            vm.PrinterServerPort = GetAppConfigValueByName("PrinterServerPort");

            ViewBag.ImpressoraDDL = BuildExpedicaoImpressoraDDL();

            return View(vm);
        }

        [HttpPost]
        // GET: GetData
        public ActionResult GetData(int? movimento, DataTableAjaxPostModel model)
        {
            movimento = movimento ?? 0;
            IQueryable<DocExpedicao> documentosPeriodo = ApplyMovimentoFilter(BuildDocExpedicaoPeriodoQuery(), movimento.Value);

            if (model == null)
            {
                return Json(new { draw = 0, recordsFiltered = 0, recordsTotal = 0, data = new object[0] });
            }

            var query = from nf in documentosPeriodo.AsNoTracking()
                        from cli in db.Cliente.Where(x => x.CodigoDMS == nf.CodigoCliente).DefaultIfEmpty()
                        select new NotaFiscalViewModel
                        {
                            Id = nf.Id,
                            Numero = nf.Numero,
                            DataEmissao = nf.DataEmissao,
                            Classificacao = nf.Classificacao,
                            Controle = nf.Controle,
                            Vendedor = nf.Vendedor,
                            CodigoCliente = nf.CodigoCliente,
                            NomeCliente = cli.Nome ?? nf.NomeCliente,
                            CNPJ = cli.CNPJ,
                            Cidade = nf.Cidade,
                            Estado = nf.Estado,
                            StatusId = nf.StatusId,
                            StatusNF = (from s in db.StatusDocExpedicao where s.Id == nf.StatusId select s.Nome).FirstOrDefault(),
                            EmpresaId = nf.EmpresaId,
                            NomeEmpresa = (from e in db.Empresa where e.Id == nf.EmpresaId select e.Nome).FirstOrDefault(),
                            RoteiroImpresso = nf.RoteiroImpresso,
                            RoteiroId = nf.RoteiroId,
                            NumeroRoteiro = (from r in db.Roteiro where r.Id == nf.RoteiroId select r.Codigo).FirstOrDefault(),
                            TransportadoraId = nf.TransportadoraId,
                            NomeTransportadora = (from t in db.Transportadora where t.Id == nf.TransportadoraId select t.Nome_Fantasia).FirstOrDefault(),
                            Finalizar = (from t in db.Transportadora where t.Id == nf.TransportadoraId select t.Finalizar).FirstOrDefault(),
                            QtdVolumes = nf.QtdVolumes ?? 1,
                            RotaId = nf.RotaId,
                            NomeRota = (from r in db.Rota where r.Id == nf.RotaId select r.Nome).FirstOrDefault(),
                            ParadaId = nf.ParadaId,
                            NomeParada = (from p in db.Parada where p.Id == nf.ParadaId select p.Nome).FirstOrDefault(),
                            Movimento = nf.Movimento,
                            TipoMovimentoId = nf.TipoMovimentoId,
                            NomeTipoMovimento = (from t in db.TipoMovimentoExpedicao where t.Id == nf.TipoMovimentoId select t.Descricao).FirstOrDefault(),
                            Danfe = nf.Danfe,
                            Valor = nf.Valor,
                            Observacoes = nf.Observacoes,
                            CriadoEm = nf.CriadoEm,
                            CriadoPor = nf.CriadoPor,
                            ModificadoEm = nf.ModificadoEm == null ? nf.CriadoEm : nf.ModificadoEm,
                            ModificadoPor = nf.ModificadoPor == null ? nf.CriadoPor : nf.ModificadoPor,
                            CriadoPorNome = (from u in db.Usuario where u.Login == nf.CriadoPor select u.Nome).FirstOrDefault(),
                            ModificadoPorNome = (from u in db.Usuario where u.Login == nf.ModificadoPor select u.Nome).FirstOrDefault()
                        };

            int recordsTotal = query.Count();
            string termo = model.search == null ? string.Empty : (model.search.value ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(termo))
            {
                query = query.Where(x =>
                    (x.Numero ?? string.Empty).Contains(termo) ||
                    (x.Classificacao ?? string.Empty).Contains(termo) ||
                    (x.Controle ?? string.Empty).Contains(termo) ||
                    (x.Vendedor ?? string.Empty).Contains(termo) ||
                    (x.CodigoCliente ?? string.Empty).Contains(termo) ||
                    (x.NomeCliente ?? string.Empty).Contains(termo) ||
                    (x.NomeTransportadora ?? string.Empty).Contains(termo) ||
                    (x.NomeTipoMovimento ?? string.Empty).Contains(termo) ||
                    (x.StatusNF ?? string.Empty).Contains(termo) ||
                    (x.ModificadoPor ?? string.Empty).Contains(termo));
            }

            int recordsFiltered = query.Count();
            int sortIndex = model.order != null && model.order.Length > 0 ? model.order[0].column : -1;
            string sortField = sortIndex >= 0 && model.columns != null && sortIndex < model.columns.Length
                ? model.columns[sortIndex].data
                : string.Empty;
            bool desc = model.order != null && model.order.Length > 0 && model.order[0].dir == "desc";

            switch (sortField)
            {
                case "Numero": query = desc ? query.OrderByDescending(x => x.Numero) : query.OrderBy(x => x.Numero); break;
                case "DataEmissaoTexto": query = desc ? query.OrderByDescending(x => x.DataEmissao) : query.OrderBy(x => x.DataEmissao); break;
                case "Classificacao": query = desc ? query.OrderByDescending(x => x.Classificacao) : query.OrderBy(x => x.Classificacao); break;
                case "Controle": query = desc ? query.OrderByDescending(x => x.Controle) : query.OrderBy(x => x.Controle); break;
                case "Vendedor": query = desc ? query.OrderByDescending(x => x.Vendedor) : query.OrderBy(x => x.Vendedor); break;
                case "NomeCliente": query = desc ? query.OrderByDescending(x => x.NomeCliente) : query.OrderBy(x => x.NomeCliente); break;
                case "NomeTransportadora": query = desc ? query.OrderByDescending(x => x.NomeTransportadora) : query.OrderBy(x => x.NomeTransportadora); break;
                case "QtdVolumes": query = desc ? query.OrderByDescending(x => x.QtdVolumes) : query.OrderBy(x => x.QtdVolumes); break;
                case "NomeTipoMovimento": query = desc ? query.OrderByDescending(x => x.NomeTipoMovimento) : query.OrderBy(x => x.NomeTipoMovimento); break;
                case "StatusNF": query = desc ? query.OrderByDescending(x => x.StatusNF) : query.OrderBy(x => x.StatusNF); break;
                case "ModificadoPor": query = desc ? query.OrderByDescending(x => x.ModificadoPor) : query.OrderBy(x => x.ModificadoPor); break;
                default: query = query.OrderByDescending(x => x.ModificadoEm).ThenByDescending(x => x.Id); break;
            }

            int length = model.length > 0 ? model.length : 25;
            List<NotaFiscalViewModel> notas = query.Skip(model.start).Take(length).ToList();
            foreach (var nota in notas)
            {
                nota.DataEmissaoTexto = nota.DataEmissao.HasValue
                    ? nota.DataEmissao.Value.ToString("dd/MM/yyyy")
                    : string.Empty;
            }

            JsonResult result = Json(new { draw = model.draw, recordsFiltered, recordsTotal, data = notas });
            result.MaxJsonLength = int.MaxValue;

            return result;
        }

        public ActionResult Edit(int id)
        {
            DocExpedicao documento = db.DocExpedicao
                .FirstOrDefault(x => x.Id == id && x.FilialId == filialId);
            if (documento == null)
            {
                return HttpNotFound();
            }

            NotaFiscalViewModel vm = new NotaFiscalViewModel();

            vm.Id = documento.Id;
            vm.Numero = documento.Numero;
            vm.DataEmissao = documento.DataEmissao;
            vm.Classificacao = documento.Classificacao;
            vm.Controle = documento.Controle;
            vm.Vendedor = documento.Vendedor;
            vm.CodigoCliente = documento.CodigoCliente;
            vm.NomeCliente = documento.NomeCliente;
            vm.Cidade = documento.Cidade;
            vm.Estado = documento.Estado;

            vm.StatusId = documento.StatusId;
            vm.StatusNF = (from s in db.StatusDocExpedicao where s.Id == documento.StatusId select s.Nome).FirstOrDefault();

            vm.EmpresaId = documento.EmpresaId;
            vm.NomeEmpresa = (from e in db.Empresa where e.Id == documento.EmpresaId select e.Nome).FirstOrDefault();

            vm.RoteiroImpresso = documento.RoteiroImpresso;
            vm.QtdVolumes = documento.QtdVolumes;

            vm.TransportadoraId = documento.TransportadoraId;
            vm.TransportadoraDDL = Util.GetTransportadoraDDL(documento.FilialId, documento.TransportadoraId);

            vm.RotaId = documento.RotaId;
            vm.RotaDDL = Util.GetRotaDDL(documento.FilialId, documento.RotaId);

            vm.ParadaId = documento.ParadaId;
            vm.ParadaDDL = Util.GetParadaDDL(documento.FilialId,documento.ParadaId);

            vm.Movimento = documento.Movimento;
            vm.TipoMovimentoId = documento.TipoMovimentoId;

            vm.Danfe = documento.Danfe;
            vm.Valor = documento.Valor;
            vm.Observacoes = documento.Observacoes;

            return View(vm);
        }

        public ActionResult Import()
        {
            return View();
        }

        public ActionResult ImportTransportadora()
        {
            return View(BuildTransportadoraImportViewModel());
        }

        private IQueryable<DocExpedicao> BuildDocExpedicaoPeriodoQuery()
        {
            return db.DocExpedicao.Where(x =>
                x.FilialId == filialId &&
                x.CriadoEm >= inicio);
        }

        private static IQueryable<DocExpedicao> ApplyMovimentoFilter(IQueryable<DocExpedicao> query, int movimento)
        {
            switch (movimento)
            {
                case 0:
                    return query.Where(x => x.StatusId == 1 && x.TipoMovimentoId == null);
                case 5:
                    return query.Where(x => x.StatusId == 4);
                case 6:
                    return query.Where(x => x.StatusId == 1002);
                case 7:
                    return query.Where(x => x.StatusId == 2);
                case 99:
                    return query.Where(x => x.StatusId == 3 && x.RoteiroImpresso == false);
                default:
                    return query;
            }
        }

        [HttpGet]
        public ActionResult GetPrinterData(int id)
        {
            try
            {
                var result = db.Impressora
                    .AsNoTracking()
                    .FirstOrDefault(x => x.Id == id && x.FilialId == filialId);

                if (result == null)
                {
                    return Json(new { success = false, msg = "Impressora não encontrada!" }, JsonRequestBehavior.AllowGet);
                }

                return Json(new { data = result, success = true }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, msg = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public ActionResult GetTransportadoraData()
        {
            const string sql = @"
SELECT
    nft.Id,
    nft.NotaFiscal AS NotaFiscalNr,
    nf.TransportadoraId,
    COALESCE(tDoc.Nome_Fantasia, tDoc.Nome, nft.Transportadora) AS NomeTransportadora,
    nft.Volumes,
    cliDoc.Id AS ClienteId,
    COALESCE(cliDoc.Nome, nft.Cliente) AS NomeCliente,
    nft.Contato AS ContatoNr,
    nft.VolumeNr,
    CAST(NULL AS int) AS SeqNr,
    nf.RotaId,
    r.Nome AS NomeRota,
    nf.ParadaId,
    p.Nome AS NomeParada,
    nft.FilialId,
    nf.Id AS NotaFiscalId,
    nf.StatusId,
    s.Nome AS StatusNF,
    nf.TipoMovimentoId,
    tm.Descricao AS NomeTipoMovimento,
    nf.QtdVolumes AS QtdVolumesDoc
FROM
(
    SELECT *,
        ROW_NUMBER() OVER
        (
            PARTITION BY FilialId, NotaFiscal, Contato, VolumeNr
            ORDER BY Id
        ) AS RegistroNr
    FROM NotaFiscalTransportadora
) nft
OUTER APPLY (
    SELECT TOP 1 doc.*
    FROM DocExpedicao doc
    WHERE doc.FilialId = nft.FilialId
      AND doc.Controle = nft.Contato
      AND (nft.NotaFiscal IS NULL OR doc.Numero = nft.NotaFiscal)
    ORDER BY
        CASE WHEN doc.Numero = nft.NotaFiscal THEN 0 ELSE 1 END,
        doc.Id DESC
) nf
LEFT JOIN StatusDocExpedicao s
    ON s.Id = nf.StatusId
LEFT JOIN TipoMovimentoExpedicao tm
    ON tm.Id = nf.TipoMovimentoId
LEFT JOIN Transportadora tDoc
    ON tDoc.Id = nf.TransportadoraId
LEFT JOIN Cliente cliDoc
    ON cliDoc.CodigoDMS = nf.CodigoCliente
LEFT JOIN Rota r
    ON r.Id = nf.RotaId
LEFT JOIN Parada p
    ON p.Id = nf.ParadaId
WHERE nft.FilialId = @p0
  AND nft.RegistroNr = 1
ORDER BY nft.NotaFiscal, nft.Contato, nft.Id";

            var data = db.Database
                .SqlQuery<NotaFiscalTransportadoraGridItemViewModel>(sql, filialId)
                .ToList();

            JsonResult result = Json(new { data }, JsonRequestBehavior.AllowGet);
            result.MaxJsonLength = int.MaxValue;
            return result;
        }

        [HttpPost]
        public ActionResult UploadTransportadoraFile(UploadArquivoTransportadora vm)
        {
            if (vm == null || vm.Arquivo == null)
            {
                return Json(new { erro = true, mensagem = "Arquivo não informado" }, JsonRequestBehavior.AllowGet);
            }

            if (!vm.TransportadoraId.HasValue)
            {
                return Json(new { erro = true, mensagem = "Selecione a transportadora para continuar" }, JsonRequestBehavior.AllowGet);
            }

            HttpPostedFileBase arquivo = vm.Arquivo;
            if (arquivo == null)
            {
                return Json(new { erro = true, mensagem = "[HttpPostedFileBase] Não foi possível importar o arquivo informado" }, JsonRequestBehavior.AllowGet);
            }

            if (!string.Equals(Path.GetExtension(arquivo.FileName ?? string.Empty), ".pdf", StringComparison.OrdinalIgnoreCase))
            {
                return Json(new { erro = true, mensagem = "O arquivo da transportadora deve ser enviado em PDF" }, JsonRequestBehavior.AllowGet);
            }

            try
            {
                db.Database.CommandTimeout = TransportadoraImportTimeoutSeconds;
                bool imprimirDireto = IsTransportadoraDirectPrintEnabled();
                TransportadoraDirectPrinterSettings impressoraPadrao = null;
                string printerServerIp = null;
                string printerServerPort = null;
                if (imprimirDireto)
                {
                    printerServerIp = GetAppConfigValueByName("PrinterServerIP");
                    printerServerPort = GetAppConfigValueByName("PrinterServerPort");
                    int parsedPrinterServerPort;
                    if (string.IsNullOrWhiteSpace(printerServerIp) ||
                        !int.TryParse(printerServerPort, out parsedPrinterServerPort) ||
                        parsedPrinterServerPort <= 0 ||
                        parsedPrinterServerPort > 65535)
                    {
                        LogTransportadoraPrintIssue(
                            "UploadTransportadoraFile",
                            "Resolver servidor de impressão",
                            "Os parâmetros PrinterServerIP e PrinterServerPort são obrigatórios para impressão automática.");
                        return Json(new { erro = true, mensagem = "Servidor de impressão não configurado na AppConfig" }, JsonRequestBehavior.AllowGet);
                    }

                    printerServerPort = parsedPrinterServerPort.ToString(CultureInfo.InvariantCulture);

                    impressoraPadrao = ResolveTransportadoraDefaultPrinter();
                    if (impressoraPadrao == null)
                    {
                        LogTransportadoraPrintIssue(
                            "UploadTransportadoraFile",
                            "Resolver impressora padrão",
                            "A configuração da impressora padrão está ausente ou incompleta para impressão automática.");
                        return Json(new { erro = true, mensagem = "Impressora padrão não localizada para impressão automática" }, JsonRequestBehavior.AllowGet);
                    }
                }

                Transportadora transportadora = db.Transportadora.FirstOrDefault(x =>
                    x.Id == vm.TransportadoraId.Value &&
                    x.FilialId == filialId &&
                    x.EmitirEtiqueta);
                if (transportadora == null)
                {
                    return Json(new { erro = true, mensagem = "A transportadora selecionada não foi localizada" }, JsonRequestBehavior.AllowGet);
                }

                string nomeTransportadora = transportadora.Nome_Fantasia ?? transportadora.Nome;
                TransportadoraImportParseResult parseResult = ParseTransportadoraFile(arquivo, nomeTransportadora);
                List<NotaFiscalTransportadoraImportRow> items = parseResult.Items;
                string diagnostico = BuildTransportadoraImportDiagnosticMessage(parseResult.Diagnostics);

                if (items.Count == 0)
                {
                    ClearNotaFiscalTransportadoraDaFilial();
                    return Json(new
                    {
                        erro = false,
                        mensagem = "Nenhuma NF nova foi localizada para impressão de etiquetas.",
                        diagnostico,
                        qtd_linhas = 0,
                        imprimirDireto = false
                    }, JsonRequestBehavior.AllowGet);
                }

                HashSet<string> notasCriadas = EnsureDocExpedicaoCriadoPorImportacaoTransportadora(
                    items,
                    vm.TipoMovimentoId,
                    transportadora);
                items = items
                    .Where(x => notasCriadas.Contains(NormalizeNotaFiscalNr(x.NotaFiscalNr)))
                    .ToList();

                if (items.Count == 0)
                {
                    ClearNotaFiscalTransportadoraDaFilial();
                    return Json(new
                    {
                        erro = false,
                        mensagem = "As notas fiscais do arquivo já existem na DocExpedicao. Nenhuma etiqueta foi gerada.",
                        diagnostico,
                        qtd_linhas = 0,
                        imprimirDireto = false
                    }, JsonRequestBehavior.AllowGet);
                }

                DataTable dataTable = BuildNotaFiscalTransportadoraDataTable(items);
                int idAnteriorImportacao = db.NotaFiscalTransportadora
                    .Select(x => (int?)x.Id)
                    .Max() ?? 0;

                using (SqlConnection dbConn = new SqlConnection(db.Database.Connection.ConnectionString))
                {
                    dbConn.Open();

                    using (SqlTransaction sqlTransaction = dbConn.BeginTransaction())
                    {
                        using (SqlCommand clearCommand = new SqlCommand(
                            "DELETE FROM [NotaFiscalTransportadora] WHERE FilialId = @FilialId",
                            dbConn,
                            sqlTransaction))
                        {
                            clearCommand.Parameters.Add("@FilialId", SqlDbType.Int).Value = filialId;
                            clearCommand.CommandTimeout = TransportadoraImportTimeoutSeconds;
                            clearCommand.ExecuteNonQuery();
                        }

                        using (SqlBulkCopy bulkCopy = new SqlBulkCopy(dbConn, SqlBulkCopyOptions.TableLock, sqlTransaction))
                        {
                            bulkCopy.DestinationTableName = "NotaFiscalTransportadora";
                            bulkCopy.BatchSize = Math.Max(1, Math.Min(1000, dataTable.Rows.Count));
                            bulkCopy.BulkCopyTimeout = TransportadoraImportTimeoutSeconds;

                            bulkCopy.ColumnMappings.Add("Transportadora", "Transportadora");
                            bulkCopy.ColumnMappings.Add("NotaFiscal", "NotaFiscal");
                            bulkCopy.ColumnMappings.Add("Volumes", "Volumes");
                            bulkCopy.ColumnMappings.Add("Cliente", "Cliente");
                            bulkCopy.ColumnMappings.Add("Contato", "Contato");
                            bulkCopy.ColumnMappings.Add("VolumeNr", "VolumeNr");
                            bulkCopy.ColumnMappings.Add("FilialId", "FilialId");
                            bulkCopy.ColumnMappings.Add("Sequencia", "Sequencia");
                            bulkCopy.ColumnMappings.Add("ZPL", "ZPL");

                            bulkCopy.WriteToServer(dataTable);
                        }

                        sqlTransaction.Commit();
                    }
                }

                int idFinalImportacao = db.NotaFiscalTransportadora
                    .Where(x => x.FilialId == filialId && x.Id > idAnteriorImportacao)
                    .Select(x => (int?)x.Id)
                    .Max() ?? idAnteriorImportacao;

                if (imprimirDireto)
                {
                    TransportadoraPrintPayload payload = BuildTransportadoraPrintPayload(
                        false,
                        idAnteriorImportacao + 1,
                        idFinalImportacao);
                    if (payload == null)
                    {
                        LogTransportadoraPrintIssue(
                            "UploadTransportadoraFile",
                            "Montar payload ZPL",
                            "Nenhuma etiqueta com ZPL foi localizada após a importação do PDF da transportadora.");
                        return Json(new { erro = true, mensagem = "Nenhuma etiqueta foi localizada para impressão automática" }, JsonRequestBehavior.AllowGet);
                    }

                    JsonResult directResult = Json(new
                    {
                        erro = false,
                        mensagem = "Arquivo importado com sucesso",
                        diagnostico,
                        qtd_linhas = items.Count,
                        imprimirDireto = true,
                        zpl = payload.Zpl,
                        countImpressao = payload.CountImpressao,
                        defaultPrinterIp = impressoraPadrao.PrinterTarget,
                        defaultPrinterPort = impressoraPadrao.PrinterPort,
                        defaultPrinterName = impressoraPadrao.PrinterName,
                        printerServerIp = printerServerIp.Trim(),
                        printerServerPort = printerServerPort.Trim(),
                        transportadora = nomeTransportadora,
                        inicioId = idAnteriorImportacao + 1,
                        fimId = idFinalImportacao
                    }, JsonRequestBehavior.AllowGet);
                    directResult.MaxJsonLength = int.MaxValue;
                    return directResult;
                }

                TransportadoraPrintPayload manualPayload = BuildTransportadoraPrintPayload(
                    false,
                    idAnteriorImportacao + 1,
                    idFinalImportacao);

                if (manualPayload == null)
                {
                    return Json(new
                    {
                        erro = false,
                        mensagem = "Arquivo processado, mas nenhuma etiqueta foi gerada.",
                        diagnostico,
                        qtd_linhas = 0,
                        imprimirDireto = false
                    }, JsonRequestBehavior.AllowGet);
                }

                JsonResult manualResult = Json(new
                {
                    erro = false,
                    mensagem = "Arquivo importado com sucesso",
                    diagnostico,
                    qtd_linhas = manualPayload.CountImpressao,
                    imprimirDireto = false,
                    countImpressao = manualPayload.CountImpressao,
                    transportadora = nomeTransportadora,
                    inicioId = idAnteriorImportacao + 1,
                    fimId = idFinalImportacao
                }, JsonRequestBehavior.AllowGet);
                manualResult.MaxJsonLength = int.MaxValue;
                return manualResult;
            }
            catch (DbEntityValidationException ex)
            {
                string mensagem = BuildEntityValidationErrorMessage(ex);
                LogTransportadoraPrintIssue("UploadTransportadoraFile", "Validação da importação", mensagem);
                return Json(new { erro = true, mensagem }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                string mensagem = BuildDetailedExceptionMessage(ex);
                LogTransportadoraPrintIssue("UploadTransportadoraFile", "Processar importação", mensagem);
                return Json(new { erro = true, mensagem }, JsonRequestBehavior.AllowGet);
            }
        }

        private void ClearNotaFiscalTransportadoraDaFilial()
        {
            db.Database.ExecuteSqlCommand(
                "DELETE FROM [NotaFiscalTransportadora] WHERE FilialId = @p0",
                filialId);
        }

        [HttpPost]
        public ActionResult GetTransportadoraPrintCount()
        {
            try
            {
                int countImpressao = db.NotaFiscalTransportadora
                    .AsNoTracking()
                    .Count(x => x.FilialId == filialId && x.ZPL != null && x.ZPL != string.Empty);

                if (countImpressao == 0)
                {
                    return Json(new { success = false, msg = "Nenhuma NF em espera foi localizada nos registros importados!" }, JsonRequestBehavior.AllowGet);
                }

                return Json(new
                {
                    countImpressao,
                    success = countImpressao > 0,
                    msg = countImpressao > 0 ? "Operação realizada com sucesso" : "As NFs importadas não possuem etiquetas pendentes para impressão!"
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, msg = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult PrintTransportadora(List<int> ids)
        {
            try
            {
                List<int> selectedIds = NormalizeTransportadoraSelectedIds(ids);
                if (selectedIds.Count == 0)
                {
                    return Json(new { zpl = string.Empty, success = false, msg = "Selecione ao menos uma etiqueta para impressão." }, JsonRequestBehavior.AllowGet);
                }

                TransportadoraPrintPayload payload = BuildTransportadoraPrintPayload(false, null, null, selectedIds);
                if (payload == null)
                {
                    LogTransportadoraPrintIssue(
                        "PrintTransportadora",
                        "Montar payload ZPL",
                        "Nenhuma NF selecionada foi localizada nos registros importados para impressão.");
                    return Json(new { zpl = string.Empty, success = false, msg = "Nenhuma etiqueta selecionada foi localizada para impressão!" }, JsonRequestBehavior.AllowGet);
                }

                JsonResult result = Json(new
                {
                    zpl = payload.Zpl,
                    countImpressao = payload.CountImpressao,
                    success = true,
                    msg = "Operação realizada com sucesso"
                }, JsonRequestBehavior.AllowGet);
                result.MaxJsonLength = int.MaxValue;
                return result;
            }
            catch (Exception ex)
            {
                string mensagem = BuildDetailedExceptionMessage(ex);
                LogTransportadoraPrintIssue("PrintTransportadora", "Gerar ZPL para impressão manual", mensagem);
                JsonResult result = Json(new { zpl = string.Empty, success = false, msg = mensagem }, JsonRequestBehavior.AllowGet);
                result.MaxJsonLength = int.MaxValue;
                return result;
            }
        }

        [HttpGet]
        public ActionResult PrintTransportadoraManual(List<int> ids, int? inicioId, int? fimId)
        {
            List<int> selectedIds = NormalizeTransportadoraSelectedIds(ids);
            if (selectedIds.Count == 0)
            {
                return HttpNotFound("Selecione ao menos uma etiqueta para impressão.");
            }

            List<EtiquetaExpedicaoViewModel> etiquetas = BuildTransportadoraManualLabels(inicioId, fimId, selectedIds);
            if (etiquetas.Count == 0)
            {
                return HttpNotFound("Nenhuma etiqueta foi localizada para impressão manual.");
            }

            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();
            return View(etiquetas);
        }

        [HttpGet]
        public ActionResult PrintExpedicaoManual(string key, int minVolume, int maxVolume)
        {
            if (string.IsNullOrWhiteSpace(key) || minVolume <= 0 || maxVolume < minVolume)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Informe a nota fiscal e um intervalo de volumes válido.");
            }

            string numeroNotaFiscal = key.Trim();
            if (numeroNotaFiscal.Length == 44)
            {
                numeroNotaFiscal = numeroNotaFiscal.Substring(25, 9);
            }
            else
            {
                numeroNotaFiscal = numeroNotaFiscal.PadLeft(9, '0');
            }

            DocExpedicao documento = db.DocExpedicao
                .AsNoTracking()
                .FirstOrDefault(x => x.FilialId == filialId && x.Numero == numeroNotaFiscal);
            if (documento == null)
            {
                return HttpNotFound("Nenhuma NF foi localizada.");
            }

            if (documento.StatusId == 1 || documento.StatusId == 1002)
            {
                return new HttpStatusCodeResult(
                    HttpStatusCode.BadRequest,
                    "A nota fiscal precisa estar Em trânsito, Finalizada ou Aguardando Roteiro para ser impressa.");
            }

            int quantidadeVolumes = documento.QtdVolumes ?? 0;
            if (quantidadeVolumes <= 0 || maxVolume > quantidadeVolumes)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "O intervalo informado excede a quantidade de volumes da nota fiscal.");
            }

            Cliente cliente = ResolveClienteEtiqueta(documento);
            if (!ClientePermiteGerarEtiqueta(cliente))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "O cliente desta nota fiscal está configurado para não gerar etiqueta.");
            }

            Transportadora transportadora = db.Transportadora
                .AsNoTracking()
                .FirstOrDefault(x =>
                    x.Id == documento.TransportadoraId &&
                    x.FilialId == filialId &&
                    x.EmitirEtiqueta);
            if (transportadora == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "A transportadora cadastrada na nota fiscal não emite etiqueta.");
            }

            string rota = documento.RotaId.HasValue
                ? db.Rota.Where(x => x.Id == documento.RotaId.Value).Select(x => x.Nome).FirstOrDefault()
                : string.Empty;
            string parada = documento.ParadaId.HasValue
                ? db.Parada.Where(x => x.Id == documento.ParadaId.Value).Select(x => x.Nome).FirstOrDefault()
                : string.Empty;
            string contato = documento.Controle ?? string.Empty;
            string nomeCliente = FirstNotEmpty(
                documento.NomeCliente,
                cliente != null ? cliente.Nome : null);
            string origem = ResolveLocalOrigem();
            DateTime agora = Util.GetCurrentDateTime();
            List<EtiquetaExpedicaoViewModel> etiquetas = new List<EtiquetaExpedicaoViewModel>();

            for (int volume = minVolume; volume <= maxVolume; volume++)
            {
                string volumeNr = string.Concat(documento.Numero, volume.ToString().PadLeft(3, '0'));
                etiquetas.Add(new EtiquetaExpedicaoViewModel
                {
                    NotaFiscal = documento.Numero ?? string.Empty,
                    QtdVolumes = quantidadeVolumes,
                    Contato = contato,
                    Cliente = nomeCliente,
                    Endereco = cliente != null ? cliente.Endereco_Logradouro ?? string.Empty : string.Empty,
                    Numero = cliente != null ? cliente.Endereco_Numero ?? string.Empty : string.Empty,
                    Complemento = cliente != null ? cliente.Endereco_Complemento ?? string.Empty : string.Empty,
                    Bairro = cliente != null ? cliente.Endereco_Bairro ?? string.Empty : string.Empty,
                    Cidade = FirstNotEmpty(documento.Cidade, cliente != null ? cliente.Endereco_Cidade : null),
                    Estado = FirstNotEmpty(documento.Estado, cliente != null ? cliente.Endereco_UF : null),
                    Origem = origem,
                    Parada = parada ?? string.Empty,
                    Rota = rota ?? string.Empty,
                    Transportadora = transportadora.Nome_Fantasia ?? transportadora.Nome ?? string.Empty,
                    Data = agora.ToString("dd/MM/yyyy"),
                    Hora = agora.ToString("HH:mm:ss"),
                    VolumeNr = volumeNr,
                    Sequencia = string.Concat(volume.ToString(), "/", quantidadeVolumes.ToString()),
                    CodigoBarrasVolumeSvg = BuildCode128Svg(volumeNr),
                    CodigoBarrasContatoSvg = BuildCode128Svg(contato)
                });
            }

            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();
            return View("PrintTransportadoraManual", etiquetas);
        }

        [HttpPost]
        public ActionResult FinalizeTransportadoraPrint(
            List<int> ids,
            int? inicioId,
            int? fimId,
            bool reimpressao = false)
        {
            try
            {
                List<int> selectedIds = ids == null
                    ? null
                    : NormalizeTransportadoraSelectedIds(ids);

                IQueryable<NotaFiscalTransportadora> query = db.NotaFiscalTransportadora
                    .Where(x =>
                        x.FilialId == filialId &&
                        x.ZPL != null &&
                        x.ZPL != string.Empty);

                if (selectedIds != null)
                {
                    if (selectedIds.Count == 0)
                    {
                        return Json(new { success = false, msg = "Selecione ao menos uma etiqueta para finalizar a impressão." }, JsonRequestBehavior.AllowGet);
                    }

                    query = query.Where(x => selectedIds.Contains(x.Id));
                }

                if (inicioId.HasValue)
                {
                    query = query.Where(x => x.Id >= inicioId.Value);
                }

                if (fimId.HasValue)
                {
                    query = query.Where(x => x.Id <= fimId.Value);
                }

                List<NotaFiscalTransportadora> registros = query
                    .OrderBy(x => x.NotaFiscal)
                    .ThenBy(x => x.VolumeNr)
                    .ToList();

                if (registros.Count == 0)
                {
                    return Json(new { success = false, msg = "Nenhuma NF em espera foi localizada nos registros importados!" }, JsonRequestBehavior.AllowGet);
                }

                if (!reimpressao)
                {
                    PersistTransportadoraPrintResult(registros);
                }

                return Json(new { success = true, msg = "Operação realizada com sucesso", countImpressao = registros.Count }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                string mensagem = BuildDetailedExceptionMessage(ex);
                LogTransportadoraPrintIssue("FinalizeTransportadoraPrint", "Persistir resultado da impressão", mensagem);
                return Json(new { success = false, msg = mensagem }, JsonRequestBehavior.AllowGet);
            }
        }

        private TransportadoraPrintPayload BuildTransportadoraPrintPayload(
            bool persistResult,
            int? inicioId = null,
            int? fimId = null,
            IEnumerable<int> ids = null)
        {
            IQueryable<NotaFiscalTransportadora> query = db.NotaFiscalTransportadora
                .Where(x => x.FilialId == filialId && x.ZPL != null && x.ZPL != string.Empty);

            List<int> selectedIds = ids == null ? null : NormalizeTransportadoraSelectedIds(ids);
            if (selectedIds != null)
            {
                query = query.Where(x => selectedIds.Contains(x.Id));
            }

            if (inicioId.HasValue)
            {
                query = query.Where(x => x.Id >= inicioId.Value);
            }

            if (fimId.HasValue)
            {
                query = query.Where(x => x.Id <= fimId.Value);
            }

            List<NotaFiscalTransportadora> registros = query
                .OrderBy(x => x.NotaFiscal)
                .ThenBy(x => x.VolumeNr)
                .ToList()
                .GroupBy(x => new { x.FilialId, x.NotaFiscal, x.Contato, x.VolumeNr })
                .Select(x => x.First())
                .ToList();

            if (registros.Count == 0)
            {
                return null;
            }

            string zplAll = string.Concat(registros.Select(x => x.ZPL));
            if (persistResult)
            {
                PersistTransportadoraPrintResult(registros);
            }

            return new TransportadoraPrintPayload
            {
                Zpl = zplAll,
                CountImpressao = registros.Count
            };
        }

        private List<EtiquetaExpedicaoViewModel> BuildTransportadoraManualLabels(
            int? inicioId,
            int? fimId,
            IEnumerable<int> ids = null)
        {
            IQueryable<NotaFiscalTransportadora> query = db.NotaFiscalTransportadora
                .AsNoTracking()
                .Where(x => x.FilialId == filialId && x.ZPL != null && x.ZPL != string.Empty);

            List<int> selectedIds = ids == null ? null : NormalizeTransportadoraSelectedIds(ids);
            if (selectedIds != null)
            {
                query = query.Where(x => selectedIds.Contains(x.Id));
            }

            if (inicioId.HasValue)
            {
                query = query.Where(x => x.Id >= inicioId.Value);
            }

            if (fimId.HasValue)
            {
                query = query.Where(x => x.Id <= fimId.Value);
            }

            List<NotaFiscalTransportadora> registros = query
                .OrderBy(x => x.NotaFiscal)
                .ThenBy(x => x.VolumeNr)
                .ToList()
                .GroupBy(x => new { x.FilialId, x.NotaFiscal, x.Contato, x.VolumeNr })
                .Select(x => x.First())
                .ToList();

            if (registros.Count == 0)
            {
                return new List<EtiquetaExpedicaoViewModel>();
            }

            List<string> numeros = registros
                .Select(x => x.NotaFiscal)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();
            List<string> contatos = registros
                .Select(x => x.Contato)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();

            List<DocExpedicao> documentos = db.DocExpedicao
                .AsNoTracking()
                .Where(x => x.FilialId == filialId && numeros.Contains(x.Numero) && contatos.Contains(x.Controle))
                .ToList();

            List<string> codigosCliente = documentos
                .Select(x => x.CodigoCliente)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();
            List<string> nomesCliente = documentos
                .Select(x => x.NomeCliente)
                .Concat(registros.Select(x => x.Cliente))
                .Select(NormalizeTransportadoraWhitespace)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            List<Cliente> clientes = db.Cliente
                .AsNoTracking()
                .Where(x =>
                    (x.FilialId == filialId || !x.FilialId.HasValue) &&
                    (codigosCliente.Contains(x.CodigoDMS) || nomesCliente.Contains(x.Nome)))
                .ToList();

            List<int> rotasIds = documentos.Where(x => x.RotaId.HasValue).Select(x => x.RotaId.Value).Distinct().ToList();
            List<int> paradasIds = documentos.Where(x => x.ParadaId.HasValue).Select(x => x.ParadaId.Value).Distinct().ToList();
            Dictionary<int, string> rotas = db.Rota
                .AsNoTracking()
                .Where(x => x.FilialId == filialId && rotasIds.Contains(x.Id))
                .ToDictionary(x => x.Id, x => x.Nome);
            Dictionary<int, string> paradas = db.Parada
                .AsNoTracking()
                .Where(x => x.FilialId == filialId && paradasIds.Contains(x.Id))
                .ToDictionary(x => x.Id, x => x.Nome);

            string origem = ResolveLocalOrigem();
            DateTime agora = Util.GetCurrentDateTime();
            List<EtiquetaExpedicaoViewModel> etiquetas = new List<EtiquetaExpedicaoViewModel>();

            foreach (NotaFiscalTransportadora registro in registros)
            {
                DocExpedicao documento = documentos.FirstOrDefault(x =>
                    x.Numero == registro.NotaFiscal &&
                    x.Controle == registro.Contato);
                if (documento == null)
                {
                    continue;
                }

                string nomeCliente = NormalizeTransportadoraWhitespace(
                    !string.IsNullOrWhiteSpace(documento.NomeCliente)
                        ? documento.NomeCliente
                        : registro.Cliente);
                Cliente cliente = ResolveClienteEtiqueta(
                    clientes,
                    documento.CodigoCliente,
                    nomeCliente);
                if (!ClientePermiteGerarEtiqueta(cliente))
                {
                    continue;
                }

                string rota = documento.RotaId.HasValue && rotas.ContainsKey(documento.RotaId.Value)
                    ? rotas[documento.RotaId.Value]
                    : string.Empty;
                string parada = documento.ParadaId.HasValue && paradas.ContainsKey(documento.ParadaId.Value)
                    ? paradas[documento.ParadaId.Value]
                    : string.Empty;
                string volumeNr = registro.VolumeNr ?? string.Empty;
                string contato = registro.Contato ?? string.Empty;

                etiquetas.Add(new EtiquetaExpedicaoViewModel
                {
                    NotaFiscal = registro.NotaFiscal ?? documento.Numero ?? string.Empty,
                    QtdVolumes = registro.Volumes ?? documento.QtdVolumes ?? 1,
                    Contato = contato,
                    Cliente = FirstNotEmpty(documento.NomeCliente, registro.Cliente),
                    Endereco = cliente != null ? cliente.Endereco_Logradouro ?? string.Empty : string.Empty,
                    Numero = cliente != null ? cliente.Endereco_Numero ?? string.Empty : string.Empty,
                    Complemento = cliente != null ? cliente.Endereco_Complemento ?? string.Empty : string.Empty,
                    Bairro = cliente != null ? cliente.Endereco_Bairro ?? string.Empty : string.Empty,
                    Cidade = FirstNotEmpty(documento.Cidade, cliente != null ? cliente.Endereco_Cidade : null),
                    Estado = FirstNotEmpty(documento.Estado, cliente != null ? cliente.Endereco_UF : null),
                    Origem = origem,
                    Parada = parada,
                    Rota = rota,
                    Transportadora = registro.Transportadora ?? string.Empty,
                    Data = agora.ToString("dd/MM/yyyy"),
                    Hora = agora.ToString("HH:mm:ss"),
                    VolumeNr = volumeNr,
                    Sequencia = registro.Sequencia ?? string.Empty,
                    CodigoBarrasVolumeSvg = BuildCode128Svg(volumeNr),
                    CodigoBarrasContatoSvg = BuildCode128Svg(contato)
                });
            }

            return etiquetas;
        }

        private static List<int> NormalizeTransportadoraSelectedIds(IEnumerable<int> ids)
        {
            return ids == null
                ? new List<int>()
                : ids.Where(x => x > 0).Distinct().ToList();
        }

        private Cliente ResolveClienteEtiqueta(
            IEnumerable<Cliente> clientes,
            string codigoCliente,
            string nomeCliente)
        {
            List<Cliente> lista = clientes != null
                ? clientes.ToList()
                : new List<Cliente>();

            if (!string.IsNullOrWhiteSpace(codigoCliente))
            {
                return lista
                    .Where(x => string.Equals(
                        (x.CodigoDMS ?? string.Empty).Trim(),
                        codigoCliente.Trim(),
                        StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(x => x.FilialId == filialId)
                    .ThenByDescending(x => x.Etiqueta == true)
                    .FirstOrDefault();
            }

            if (string.IsNullOrWhiteSpace(nomeCliente))
            {
                return null;
            }

            List<Cliente> candidatos = lista
                .Where(x => string.Equals(
                    NormalizeTransportadoraWhitespace(x.Nome),
                    nomeCliente,
                    StringComparison.OrdinalIgnoreCase))
                .ToList();

            List<Cliente> candidatosFilial = candidatos
                .Where(x => x.FilialId == filialId)
                .ToList();
            if (candidatosFilial.Count > 0)
            {
                candidatos = candidatosFilial;
            }
            else
            {
                candidatos = candidatos
                    .Where(x => !x.FilialId.HasValue)
                    .ToList();
            }

            // Em caso de homonimos, somente bloqueia quando todos os cadastros
            // encontrados estiverem com Etiqueta = 0 ou NULL. Na duvida,
            // prevalece o cadastro que permite gerar a etiqueta.
            return candidatos
                .OrderByDescending(x => x.Etiqueta == true)
                .FirstOrDefault();
        }

        private Cliente ResolveClienteEtiqueta(DocExpedicao documento)
        {
            if (documento == null)
            {
                return null;
            }

            IQueryable<Cliente> query = db.Cliente
                .AsNoTracking()
                .Where(x => x.FilialId == filialId || !x.FilialId.HasValue || x.FilialId == 0);

            if (!string.IsNullOrWhiteSpace(documento.CodigoCliente))
            {
                string codigoCliente = documento.CodigoCliente.Trim();
                Cliente clientePorCodigo = query
                    .Where(x => x.CodigoDMS == codigoCliente)
                    .OrderByDescending(x => x.FilialId == filialId)
                    .ThenByDescending(x => x.Etiqueta == true)
                    .FirstOrDefault();
                if (clientePorCodigo != null)
                {
                    return clientePorCodigo;
                }
            }

            string nomeCliente = NormalizeTransportadoraWhitespace(documento.NomeCliente);
            if (string.IsNullOrWhiteSpace(nomeCliente))
            {
                return null;
            }

            List<Cliente> clientesPorNome = query
                .Where(x => x.Nome != null && x.Nome.Trim() == nomeCliente)
                .ToList();

            return ResolveClienteEtiqueta(clientesPorNome, null, nomeCliente);
        }

        private static bool ClientePermiteGerarEtiqueta(Cliente cliente)
        {
            return cliente == null || cliente.Etiqueta == true;
        }

        private static string FirstNotEmpty(params string[] values)
        {
            return values == null
                ? string.Empty
                : values.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? string.Empty;
        }

        private void PersistTransportadoraPrintResult(List<NotaFiscalTransportadora> registros)
        {
            if (registros == null || registros.Count == 0)
            {
                return;
            }

            DateTime agora = Util.GetCurrentDateTime();
            List<string> transportadoras = registros
                .Select(x => x.Transportadora)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            List<Transportadora> transportadorasDb = db.Transportadora
                .Where(x => x.FilialId == filialId &&
                    (transportadoras.Contains(x.Nome_Fantasia) ||
                     transportadoras.Contains(x.Nome)))
                .ToList();

            List<DocExpedicaoTransportadoraUpdate> updates = registros
                .Select(x => new DocExpedicaoTransportadoraUpdate
                {
                    NotaFiscalNr = x.NotaFiscal,
                    ContatoNr = x.Contato,
                    TransportadoraNome = x.Transportadora,
                    QtdVolumes = x.Volumes
                })
                .GroupBy(x => new { x.NotaFiscalNr, x.ContatoNr, x.TransportadoraNome })
                .Select(x => x.First())
                .ToList();

            List<string> numeros = updates
                .Select(x => x.NotaFiscalNr)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();

            List<DocExpedicao> documentos = db.DocExpedicao
                .Where(x => x.FilialId == filialId && numeros.Contains(x.Numero))
                .ToList();

            using (DbContextTransaction tr = db.Database.BeginTransaction())
            {
                foreach (DocExpedicaoTransportadoraUpdate update in updates)
                {
                    List<DocExpedicao> documentosDaNota = documentos
                        .Where(x =>
                            x.FilialId == filialId &&
                            x.Numero == update.NotaFiscalNr)
                        .ToList();

                    DocExpedicao doc = !string.IsNullOrWhiteSpace(update.ContatoNr)
                        ? documentosDaNota.FirstOrDefault(x =>
                            string.Equals(
                                NormalizeTransportadoraWhitespace(x.Controle),
                                NormalizeTransportadoraWhitespace(update.ContatoNr),
                                StringComparison.OrdinalIgnoreCase))
                        : null;

                    if (doc == null && documentosDaNota.Count == 1)
                    {
                        doc = documentosDaNota[0];
                    }

                    if (doc == null)
                    {
                        continue;
                    }

                    Transportadora transportadora = transportadorasDb.FirstOrDefault(x =>
                        string.Equals(x.Nome_Fantasia, update.TransportadoraNome, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(x.Nome, update.TransportadoraNome, StringComparison.OrdinalIgnoreCase));

                    if (transportadora == null)
                    {
                        continue;
                    }

                    doc.TransportadoraId = transportadora.Id;
                    if (update.QtdVolumes.HasValue && update.QtdVolumes.Value > 0)
                    {
                        doc.QtdVolumes = update.QtdVolumes.Value;
                    }

                    doc.FilialId = filialId;
                    doc.StatusId = ResolveDocExpedicaoStatusByTransportadora(transportadora);
                    doc.ModificadoPor = current_user;
                    doc.ModificadoEm = agora;
                }

                db.SaveChanges();

                tr.Commit();
            }
        }

        // Upload arquivo de notas fiscais 
        [HttpPost]
        public ActionResult UploadFile(UploadArquivo vm)
        {
            string sql = string.Empty;
            string msg = string.Empty;

            string dms = (from a in db.AppConfig where a.Nome == "DMS" select a.Valor).FirstOrDefault();
            if (dms == null || dms == string.Empty)
            {
                msg = "DMS não está configurado";
                return Json(new { erro = true, mensagem = msg }, JsonRequestBehavior.AllowGet);
            }

            if (vm.Arquivo == null)
            {
                msg = "Arquivo não informado";
                return Json(new { erro = true, mensagem = msg }, JsonRequestBehavior.AllowGet);
            }

            HttpPostedFileBase arquivo = vm.Arquivo;
            if (arquivo == null)
            {
                msg = "[HttpPostedFileBase] Não foi possível importar o arquivo informado";
                return Json(new { erro = true, mensagem = msg }, JsonRequestBehavior.AllowGet);
            }

            // Limpar tabela temporária
            try
            {
                db.Database.ExecuteSqlCommand("TRUNCATE TABLE [DocExpedicaoUpload]");
                db.SaveChanges();

            }
            catch (Exception ex)
            {
                msg = "[DocExpedicaoUpload] TRUNCATE TABLE failed<br>" + ex.Message;
                return Json(new { erro = true, mensagem = msg }, JsonRequestBehavior.AllowGet);
            }

            // Importar arquivo para tabela temporária
            int rows = 0;
            try
            {
                StreamReader reader = new StreamReader(arquivo.InputStream, Encoding.Default);
                string line;

                var dt = new DataTable();
                dt.Columns.Add(new DataColumn());
                var dbConn = new SqlConnection(db.Database.Connection.ConnectionString);

                while ((line = reader.ReadLine()) != null)
                {
                    dt.Rows.Add(line);
                }

                var bullCopy = new SqlBulkCopy(dbConn, SqlBulkCopyOptions.TableLock, null)
                {
                    DestinationTableName = "DocExpedicaoUpload",
                    BatchSize = dt.Rows.Count
                };

                dbConn.Open();
                bullCopy.WriteToServer(dt);
                dbConn.Close();
                bullCopy.Close();

                rows = dt.Rows.Count;

            }
            catch (Exception ex)
            {
                msg = "[DocExpedicaoUpload] SqlBulkCopy failed<br>" + ex.Message;
                return Json(new { erro = true, mensagem = msg }, JsonRequestBehavior.AllowGet);
            }

            if (dms == "SERCON")
            {
                // Gerar tabela temporária de notas fiscais

            }
            else
            {
                if (dms == "APOLLO")
                {
                    // Gerar tabela temporária de notas fiscais
                    try
                    {
                        db.Database.ExecuteSqlCommand("TRUNCATE TABLE [DocExpedicaoUpload_APOLLO]");
                        db.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        msg = "[DocExpedicaoUpload_APOLLO] TRUNCATE TABLE failed<br>" + ex.Message;
                        return Json(new { erro = true, mensagem = msg }, JsonRequestBehavior.AllowGet);
                    }

                    sql = (from s in db.AppSQL where s.Nome == "INSERT_NotaFiscal_APOLLO" select s.Comando).FirstOrDefault();
                    if (!string.IsNullOrEmpty(sql))
                    {
                        sql = Util.FormatSQL(sql);

                        try
                        {
                            db.Database.ExecuteSqlCommand(sql);
                            db.SaveChanges();
                        }
                        catch (Exception ex)
                        {
                            msg = "[DocExpedicaoUpload_APOLLO] INSERT failed<br>" + ex.Message;
                            return Json(new { erro = true, mensagem = msg }, JsonRequestBehavior.AllowGet);
                        }
                    }

                    sql = (from s in db.AppSQL where s.Nome == "INSERT_MERGE_DocExpedicao" select s.Comando).FirstOrDefault();
                    if (!string.IsNullOrEmpty(sql))
                    {
                        sql = dms == "APOLLO"
                            ? BuildApolloDocExpedicaoMergeSql()
                            : Util.FormatSQL(sql);

                        try
                        {
                            db.Database.ExecuteSqlCommand(
                                sql,
                                new SqlParameter("@Agora", SqlDbType.DateTime)
                                {
                                    Value = Util.GetCurrentDateTime()
                                });
                            db.SaveChanges();
                        }
                        catch (Exception ex)
                        {
                            msg = "[DocExpedicao] MERGE failed<br>" + ex.Message;
                            return Json(new { erro = true, mensagem = msg }, JsonRequestBehavior.AllowGet);
                        }
                    }

                    sql = (from s in db.AppSQL where s.Nome == "INSERT_Cliente_From_DocExpedicao" select s.Comando).FirstOrDefault();
                    if (!string.IsNullOrEmpty(sql))
                    {
                        sql = Util.FormatSQL(sql);

                        try
                        {
                            db.Database.ExecuteSqlCommand(sql);
                            db.SaveChanges();
                        }
                        catch (Exception ex)
                        {
                            msg = "[Cliente] INSERT failed<br>" + ex.Message;
                            return Json(new { erro = true, mensagem = msg }, JsonRequestBehavior.AllowGet);
                        }
                    }

                    // Gerar histórico por documento e por filial.
                    sql = BuildDocExpedicaoImportHistorySql();
                    try
                    {
                        db.Database.ExecuteSqlCommand(
                            sql,
                            new SqlParameter("@Agora", SqlDbType.DateTime)
                            {
                                Value = Util.GetCurrentDateTime()
                            });
                        db.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        msg = "[Historico_DocExpedicao] INSERT failed<br>" + ex.Message;
                        return Json(new { erro = true, mensagem = msg }, JsonRequestBehavior.AllowGet);
                    }
                }
                else
                {
                    msg = "DMS incorreto";
                    return Json(new { erro = true, mensagem = msg }, JsonRequestBehavior.AllowGet);
                }
            }

            msg = "Arquivo importado com sucesso";
            return Json(new { erro = false, mensagem = msg, qtd_linhas = rows }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Lancamento()
        {
            NotaFiscalViewModel vm = new NotaFiscalViewModel();
            vm.TransportadoraDDL = Util.GetTransportadoraDDL(filialId, null);
            vm.TipoMovimentoDDL = Util.GetTipoMovimentoExpedicaoDDL(null);
            return View(vm);
        }

        [HttpPost]
        public ActionResult CancelarLancamento(int id)
        {
            var nota = db.DocExpedicao
                .FirstOrDefault(x => x.Id == id && x.FilialId == filialId);

            if (nota == null)
            {
                return Json(new { success = false, mensagem = "Nota Fiscal não encontrada!" }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                using (DbContextTransaction tr = db.Database.BeginTransaction())
                {
                    try
                    {
                        if (nota.Classificacao != string.Empty)
                        {
                            nota.StatusId = 1;
                            nota.TipoMovimentoId = null;
                            nota.QtdVolumes = null;
                            nota.TransportadoraId = null;
                            nota.ModificadoPor = current_user;
                            nota.ModificadoEm = Util.GetCurrentDateTime();
                            nota.FilialId = filialId;
                            db.Entry(nota).State = EntityState.Modified;
                            db.SaveChanges();

                            HistoricoDocExpedicao historico = new HistoricoDocExpedicao();
                            historico.DocExpedicaoId = nota.Id;
                            historico.HistoricoId = 3;
                            historico.Observacoes = null;
                            historico.DataHora = Util.GetCurrentDateTime();
                            historico.Usuario = current_user;
                            historico.FilialId = filialId;
                            db.HistoricoDocExpedicao.Add(historico);
                            db.SaveChanges();
                        }
                        else
                        {
                            var historico = (from h in db.HistoricoDocExpedicao where h.DocExpedicaoId == nota.Id select h).ToList();
                            db.HistoricoDocExpedicao.RemoveRange(historico);
                            db.SaveChanges();

                            db.DocExpedicao.Remove(nota);
                            db.SaveChanges();
                        }

                        tr.Commit();
                        return Json(new { success = true, mensagem = "Lançamento cancelado com sucesso!" }, JsonRequestBehavior.AllowGet);
                    }
                    catch (Exception ex)
                    {
                        tr.Rollback();
                        return Json(new { success = false, mensagem = ex.Message }, JsonRequestBehavior.AllowGet);
                    }
                }
            }
        }

        [HttpPost]
        public ActionResult LancarNotas(List<NotaFiscalViewModel> notafiscal)
        {
            using (DbContextTransaction tr = db.Database.BeginTransaction())
            {
                try
                {
                    foreach (var item in notafiscal)
                    {
                        if (item.Id != 0)
                        {
                            var nota = db.DocExpedicao
                                .FirstOrDefault(x => x.Id == item.Id && x.FilialId == filialId);
                            if (nota != null)
                            {
                                var transp = db.Transportadora
                                    .FirstOrDefault(x => x.Id == item.TransportadoraId && x.FilialId == filialId);
                                if (transp == null)
                                {
                                    tr.Rollback();
                                    return Json(new { success = false, message = "Transportadora não cadastrada!" });
                                }

                                nota.TransportadoraId = item.TransportadoraId;
                                if (transp.EmitirRoteiro)
                                {
                                    if (transp.EmitirEtiqueta)
                                    {
                                        nota.StatusId = 1002; // Aguardando roteiro
                                        nota.RoteiroImpresso = false;
                                    }
                                    else
                                    {
                                        nota.StatusId = 3; // Aguardando roteiro
                                        nota.RoteiroImpresso = false;
                                    }

                                }
                                else
                                {
                                    //nota.StatusId = 2; // Em transporte
                                    nota.StatusId = 1002; //Em Espera
                                    nota.RoteiroImpresso = null;
                                }

                                nota.TipoMovimentoId = item.TipoMovimentoId;
                                nota.QtdVolumes = item.QtdVolumes;
                                nota.ModificadoPor = current_user;
                                nota.ModificadoEm = Util.GetCurrentDateTime();
                                nota.FilialId = filialId;
                                db.Entry(nota).State = EntityState.Modified;
                                db.SaveChanges();

                                HistoricoDocExpedicao historico = new HistoricoDocExpedicao();
                                historico.DocExpedicaoId = nota.Id;
                                historico.HistoricoId = 2;
                                historico.Observacoes = (from t in db.TipoMovimentoExpedicao
                                                         where t.Id == item.TipoMovimentoId
                                                         select t.Descricao).FirstOrDefault();
                                historico.DataHora = Util.GetCurrentDateTime();
                                historico.Usuario = current_user;
                                historico.FilialId = filialId;
                                db.HistoricoDocExpedicao.Add(historico);
                                db.SaveChanges();
                            }
                        }
                        else //lançamento de um nota fiscal que não é da GM
                        {
                            var cliente = db.Cliente.Find(item.ClienteId);
                            if (cliente != null)
                            {
                                DocExpedicao nota = new DocExpedicao();
                                nota.Numero = NormalizeNotaFiscalNr(item.Numero);
                                nota.DataEmissao = Util.GetCurrentDateTime();
                                nota.Classificacao = string.Empty;
                                nota.Controle = string.Empty;
                                nota.Vendedor = string.Empty;
                                nota.CodigoCliente = cliente.CodigoDMS;
                                nota.NomeCliente = cliente.Nome;
                                nota.Cidade = cliente.Endereco_Cidade;
                                nota.Estado = cliente.Endereco_UF;

                                var transp = db.Transportadora
                                    .FirstOrDefault(x => x.Id == item.TransportadoraId && x.FilialId == filialId);
                                if (transp == null)
                                {
                                    tr.Rollback();
                                    return Json(new { success = false, message = "Transportadora não cadastrada!" });
                                }

                                nota.TransportadoraId = item.TransportadoraId;
                                if (transp.EmitirRoteiro)
                                {
                                    nota.StatusId = 3; // Aguardando roteiro
                                    nota.RoteiroImpresso = false;
                                }
                                else
                                {
                                    nota.StatusId = 1002; //Em Espera
                                    nota.RoteiroImpresso = null;
                                }

                                nota.QtdVolumes = item.QtdVolumes;
                                nota.TipoMovimentoId = item.TipoMovimentoId;
                                nota.Observacoes = item.Observacoes;
                                nota.CriadoPor = current_user;
                                nota.CriadoEm = Util.GetCurrentDateTime();
                                nota.FilialId = filialId;

                                db.DocExpedicao.Add(nota);
                                db.SaveChanges();

                                HistoricoDocExpedicao historico = new HistoricoDocExpedicao();
                                historico.DocExpedicaoId = nota.Id;
                                historico.HistoricoId = 2;
                                historico.Observacoes = (from t in db.TipoMovimentoExpedicao
                                                         where t.Id == item.TipoMovimentoId
                                                         select t.Descricao).FirstOrDefault();
                                historico.DataHora = Util.GetCurrentDateTime();
                                historico.Usuario = current_user;
                                historico.FilialId = filialId;
                                db.HistoricoDocExpedicao.Add(historico);
                                db.SaveChanges();
                            }

                        }

                    }
                    tr.Commit();

                    return Json(new { success = true, message = "Notas Fiscais lançadas com sucesso!" });
                }
                catch (Exception ex)
                {
                    tr.Rollback();
                    return Json(new { success = false, message = ex.Message });
                }
            }
        }

        public ActionResult Historico(int id)
        {
            DocExpedicao notafiscal = db.DocExpedicao
                .FirstOrDefault(x => x.Id == id && x.FilialId == filialId);
            if (notafiscal == null)
            {
                return HttpNotFound();
            }

            var vm = (from h in db.HistoricoDocExpedicao
                      join t in db.TipoHistoricoExpedicao on h.HistoricoId equals t.Id
                      where h.DocExpedicaoId == notafiscal.Id && h.FilialId == filialId
                      select new HistoricoDocExpedicaoViewModel
                      {
                          Id = h.Id,
                          DocExpedicaoId = h.DocExpedicaoId,
                          HistoricoId = h.HistoricoId,
                          Observacoes = h.Observacoes,
                          DescricaoHistorico = t.Descricao,
                          DataHora = h.DataHora,
                          Usuario = h.Usuario
                      }).ToList();

            ViewBag.NumeroNF = notafiscal.Numero;
            ViewBag.Cliente = notafiscal.CodigoCliente + " - " + notafiscal.NomeCliente;
            ViewBag.Cidade = notafiscal.Cidade;
            ViewBag.Estado = notafiscal.Estado;

            return PartialView("_Historico", vm);
        }

        public ActionResult GetNotaFiscal(string key)
        {
            string numeroNF = key.Trim();
            if (key.Length == 44)
            {
                numeroNF = key.Trim().Substring(25, 9);
            }
            else
            {
                numeroNF = numeroNF.PadLeft(9, '0');
            }

            DocExpedicao notafiscal = new DocExpedicao();

            try
            {
                notafiscal = db.DocExpedicao
                    .FirstOrDefault(x => x.FilialId == filialId && x.Numero == numeroNF);
                //if (notafiscal == null && key.Length == 44)
                if (notafiscal == null)
                {
                    numeroNF = numeroNF.TrimStart('0');
                    notafiscal = db.DocExpedicao
                        .FirstOrDefault(x => x.FilialId == filialId && x.Numero == numeroNF);
                }

                if (notafiscal == null)
                {
                    JsonResult result = Json(new { data = notafiscal, success = false, msg = "Nota Fiscal não encontrada!" }, JsonRequestBehavior.AllowGet);
                    return result;
                }
                else
                {
                    if (notafiscal.StatusId != 1)
                    {
                        JsonResult result = Json(new { data = notafiscal, success = false, msg = "Nota Fiscal já está lançada!" }, JsonRequestBehavior.AllowGet);
                        return result;
                    }
                    else
                    {
                        JsonResult result = Json(new
                        {
                            data = new
                            {
                                notafiscal.Id,
                                notafiscal.Numero,
                                DataEmissaoTexto = notafiscal.DataEmissao.HasValue
                                    ? notafiscal.DataEmissao.Value.ToString("dd/MM/yyyy")
                                    : string.Empty,
                                notafiscal.CodigoCliente,
                                notafiscal.NomeCliente,
                                notafiscal.Cidade,
                                notafiscal.Estado
                            },
                            success = true,
                            msg = string.Empty
                        }, JsonRequestBehavior.AllowGet);
                        return result;
                    }

                }

            }
            catch (Exception ex)
            {
                JsonResult result = Json(new { data = notafiscal, success = false, msg = ex.Message }, JsonRequestBehavior.AllowGet);
                return result;
            }

        }

        // ---------------------------------------------------------------------------------------------
        // Processar Notas Fiscais
        //
        // 1. Altera o status da nota
        //    - quando Transportadora.Finalizar for 'true'      => alterar para 4 (Finalizado)
        //    - quando Transportadora.EmitirRoteiro for 'true'  => alterar para 3 (Aguardando roteiro)
        //    - se nenhuma das condições acima for 'true'       => alterar para 2 (Em trânsito)
        //
        // 2. Gera e retorna array de etiquetas para impressão (string zpl)
        // 
        // ---------------------------------------------------------------------------------------------
        [HttpPost]
        public ActionResult Processar(int[] ids)
        {
            List<string> listaEtiquetas = new List<string>();
            string template_zpl = (from e in db.Etiqueta
                                   where e.Nome == "Expedicao"
                                   select e.ZPL).FirstOrDefault();
            int qtd_volumes = 1;
            string zpl, zpl2, zpl3;

            var notas = (from nf in db.DocExpedicao
                         where nf.FilialId == filialId && ids.Contains(nf.Id)
                         select nf).ToList();

            if (notas.Count == 0)
            {
                return Json(new { etiquetas = listaEtiquetas, success = true, msg = "Nenhuma NF localizada!" }, JsonRequestBehavior.AllowGet);
            }

            using (DbContextTransaction tr = db.Database.BeginTransaction())
            {
                try
                {
                    foreach (var notafiscal in notas)
                    {
                        DocExpedicao doc = db.DocExpedicao
                            .FirstOrDefault(x => x.Id == notafiscal.Id && x.FilialId == filialId);
                        if (doc != null)
                        {
                            // Transportadora
                            var transp = (from t in db.Transportadora
                                          where t.Id == doc.TransportadoraId && t.FilialId == filialId
                                          select t).FirstOrDefault();

                            doc.StatusId = ResolveDocExpedicaoStatusByTransportadora(transp);

                            doc.ModificadoEm = Util.GetCurrentDateTime();
                            doc.ModificadoPor = Util.GetCurrentUser();
                            doc.FilialId = filialId;
                            db.Entry(doc).State = EntityState.Modified;
                            db.SaveChanges();

                            // Gerar etiqueta (ZPL) para cada volume
                            if (transp != null && transp.EmitirEtiqueta)
                            {
                                DateTime dt = Util.GetCurrentDateTime();

                                zpl = template_zpl;
                                zpl = zpl.Replace("local-origem", "Sorocaba");
                                zpl = zpl.Replace("data-impressao", dt.ToString("dd/MM/yyyy"));
                                zpl = zpl.Replace("hora-impressao", dt.ToString("HH:mm:ss"));
                                zpl = zpl.Replace("nome-transportadora", transp.Nome_Fantasia ?? string.Empty);

                                // Remover zeros à esquerda do número da NF
                                char[] zero = { '0' };
                                string nf_aux = doc.Numero ?? string.Empty;
                                zpl = zpl.Replace("nfiscal-nr", nf_aux.TrimStart(zero));

                                zpl = zpl.Replace("contato-nr", doc.Controle ?? string.Empty);

                                // Dados do cliente
                                var cliente = (from c in db.Cliente
                                               where c.CodigoDMS == doc.CodigoCliente
                                               select c).FirstOrDefault();

                                if (cliente != null)
                                {
                                    string cidadeEstado = cliente.Endereco_Cidade + "/" + cliente.Endereco_UF;
                                    zpl = zpl.Replace("ruanr-cliente", cliente.Endereco_Logradouro ?? string.Empty);
                                    zpl = zpl.Replace("bairro-cliente", cliente.Endereco_Bairro ?? string.Empty);
                                    //zpl = zpl.Replace("cidadeestado-cliente", cliente.Endereco_Cidade ?? string.Empty);
                                    zpl = zpl.Replace("cidadeestado-cliente", cidadeEstado ?? string.Empty);

                                    string aux_cliente = cliente.Nome ?? string.Empty;
                                    if (aux_cliente.Length > 14)
                                    {
                                        aux_cliente = aux_cliente.Substring(0, 14);
                                    }
                                    zpl = zpl.Replace("nome-cliente", aux_cliente);
                                }

                                // Nome da rota
                                string rota = (from r in db.Rota
                                               where r.Id == doc.RotaId
                                               select r.Nome).FirstOrDefault() ?? string.Empty;
                                zpl = zpl.Replace("rota-cliente", rota ?? string.Empty);


                                // Nome da Parada
                                string parada = (from p in db.Parada
                                                 where p.Id == doc.ParadaId
                                                 select p.Nome).FirstOrDefault() ?? string.Empty;
                                zpl = zpl.Replace("parada-cliente", parada ?? string.Empty);

                                qtd_volumes = doc.QtdVolumes ?? 1;
                                for (int i = 1; i <= qtd_volumes; i++)
                                {
                                    zpl2 = zpl.Replace("controlenr-quasar", string.Concat(doc.Numero, i.ToString().PadLeft(3, '0')));
                                    zpl3 = zpl2.Replace("sequencia-volume", string.Concat(i.ToString(), "/", qtd_volumes.ToString()));
                                    listaEtiquetas.Add(Util.RemoverAcentuacao(zpl3));
                                }
                            }
                        }
                    }
                    tr.Commit();

                    JsonResult result = Json(new { etiquetas = listaEtiquetas, success = true, msg = "Operação realizada com sucesso" }, JsonRequestBehavior.AllowGet);
                    result.MaxJsonLength = int.MaxValue;
                    return result;
                }
                catch (Exception ex)
                {
                    tr.Rollback();
                    JsonResult result = Json(new { etiquetas = listaEtiquetas, success = false, msg = ex.Message }, JsonRequestBehavior.AllowGet);
                    result.MaxJsonLength = int.MaxValue;
                    return result;
                }
            }

        }

        public ActionResult ImprimirRoteiro(string notas)
        {
            string formato = "PDF";

            LocalReport lr = new LocalReport();
            string path = Path.Combine(Server.MapPath("~/Areas/ExpedicaoApp/Reports"), "Report1.rdlc");
            if (System.IO.File.Exists(path))
            {
                lr.ReportPath = path;
            }
            else
            {
                return HttpNotFound();
            }

            var roteiro = db.DocExpedicao
                .Where(x => x.FilialId == filialId && x.StatusId == 3)
                .ToList();

            //ReportParameter[] parameters = new ReportParameter[];
            //parameters[0] = new ReportParameter("Posto", posto);
            //parameters[1] = new ReportParameter("DataInicio", datainicio);
            //parameters[2] = new ReportParameter("DataTermino", datatermino);
            //parameters[3] = new ReportParameter("Item", item);
            //parameters[4] = new ReportParameter("Cadencia", Math.Round(cadencia).ToString("N0"));
            //parameters[] = new ReportParameter("Dias", dias.ToString());

            //lr.SetParameters(new ReportParameter[] { param });

            ReportDataSource rd = new ReportDataSource("DataSet1", roteiro);
            lr.DataSources.Add(rd);
            //lr.SetParameters(parameters);

            string reportType = formato;
            string mimeType;
            string encoding;
            string fileNameExtension;

            //  Retrato
            //  <PageWidth>8.27in</PageWidth>
            //  <PageHeight>11.69in</PageHeight>

            //  Paisagem
            //  <PageWidth>11.69in</PageWidth>
            //  <PageHeight>8.27in</PageHeight>

            string deviceInfo =

            "<DeviceInfo>" +
            "  <OutputFormat>" + formato + "</OutputFormat>" +
            "  <PageWidth>11.69in</PageWidth>" +
            "  <PageHeight>8.27in</PageHeight>" +
            "  <MarginTop>0.2in</MarginTop>" +
            "  <MarginLeft>0.2in</MarginLeft>" +
            "  <MarginRight>0.2in</MarginRight>" +
            "  <MarginBottom>0.2in</MarginBottom>" +
            "</DeviceInfo>";

            Warning[] warnings;
            string[] streams;
            byte[] renderedBytes;

            renderedBytes = lr.Render(
                reportType,
                deviceInfo,
                out mimeType,
                out encoding,
                out fileNameExtension,
                out streams,
                out warnings);
            return File(renderedBytes, mimeType);
        }

        [HttpPost]
        public ActionResult Finalizar(string key)
        {
            string numeroNF = key.Trim();
            if (key.Length == 44)
            {
                numeroNF = key.Trim().Substring(25, 9);
            }
            else
            {
                numeroNF = numeroNF.PadLeft(9, '0');
            }

            DocExpedicao notafiscal = db.DocExpedicao
                .FirstOrDefault(x => x.FilialId == filialId && x.Numero == numeroNF);
            if (notafiscal == null && key.Length == 44)
            {
                numeroNF = numeroNF.TrimStart('0');
                notafiscal = db.DocExpedicao
                    .FirstOrDefault(x => x.FilialId == filialId && x.Numero == numeroNF);
            }

            if (notafiscal == null)
            {
                JsonResult result = Json(new { data = notafiscal, success = false, msg = "Nota Fiscal não encontrada!" }, JsonRequestBehavior.AllowGet);
                return result;
            }

            if (notafiscal.TipoMovimentoId == null) //verificar se a nota foi lançada
            {
                JsonResult result = Json(new { data = notafiscal, success = false, msg = "A Nota Fiscal não foi lançada!" }, JsonRequestBehavior.AllowGet);
                return result;
            }

            if (notafiscal.StatusId == 4)
            {
                JsonResult result = Json(new { data = notafiscal, success = false, msg = "Nota Fiscal já foi processada!" }, JsonRequestBehavior.AllowGet);
                return result;
            }

            if (notafiscal.StatusId != 2)
            {
                JsonResult result = Json(new { data = notafiscal, success = false, msg = "A Nota Fiscal precisa estar 'Em trânsito' para ser finalizada" }, JsonRequestBehavior.AllowGet);
                return result;
            }

            else
            {
                using (DbContextTransaction tr = db.Database.BeginTransaction())
                {
                    try
                    {
                        notafiscal.StatusId = 4;
                        notafiscal.ModificadoPor = current_user;
                        notafiscal.ModificadoEm = Util.GetCurrentDateTime();
                        notafiscal.FilialId = filialId;
                        db.Entry(notafiscal).State = EntityState.Modified;
                        db.SaveChanges();

                        HistoricoDocExpedicao historico = new HistoricoDocExpedicao();
                        historico.DocExpedicaoId = notafiscal.Id;
                        historico.HistoricoId = 4;
                        historico.Observacoes = null;
                        historico.DataHora = Util.GetCurrentDateTime();
                        historico.Usuario = current_user;
                        historico.FilialId = filialId;
                        db.HistoricoDocExpedicao.Add(historico);
                        db.SaveChanges();
                        tr.Commit();
                    }
                    catch (Exception ex)
                    {
                        tr.Rollback();
                        JsonResult result2 = Json(new { data = notafiscal, success = false, msg = ex.Message }, JsonRequestBehavior.AllowGet);
                        return result2;
                    }
                }

                JsonResult result = Json(new { data = notafiscal, success = true, msg = string.Empty }, JsonRequestBehavior.AllowGet);
                return result;
            }

        }
        private NotaFiscalTransportadoraImportViewModel BuildTransportadoraImportViewModel(int? tipoMovimentoId = null, int? transportadoraId = null)
        {
            int? defaultTipoMovimentoId = tipoMovimentoId
                ?? (from t in db.TipoMovimentoExpedicao
                    where t.Descricao == "Entrega"
                    select (int?)t.Id).FirstOrDefault();

            NotaFiscalTransportadoraImportViewModel vm = new NotaFiscalTransportadoraImportViewModel();
            List<SelectListItem> transportadoraDDL = (from t in db.Transportadora
                                                      where t.FilialId == filialId && t.EmitirEtiqueta
                                                      orderby t.Nome_Fantasia, t.Nome
                                                      select new SelectListItem
                                                      {
                                                          Value = t.Id.ToString(),
                                                          Text = t.Nome_Fantasia ?? t.Nome,
                                                          Selected = t.Id == transportadoraId
                                                      }).ToList();
            transportadoraDDL.Insert(0, new SelectListItem
            {
                Value = string.Empty,
                Text = "Selecione",
                Selected = !transportadoraId.HasValue
            });

            vm.TransportadoraId = transportadoraId;
            vm.TransportadoraDDL = transportadoraDDL;
            vm.TipoMovimentoId = defaultTipoMovimentoId;
            vm.TipoMovimentoDDL = Util.GetTipoMovimentoExpedicaoDDL(defaultTipoMovimentoId);
            vm.ZPL_Etiqueta = (from e in db.Etiqueta where e.Nome == "Expedicao" select e.ZPL).FirstOrDefault();
            vm.PrinterServerIP = GetAppConfigValueByName("PrinterServerIP");
            vm.PrinterServerPort = GetAppConfigValueByName("PrinterServerPort");
            vm.ImprimirDireto = IsTransportadoraDirectPrintEnabled();

            TransportadoraDirectPrinterSettings impressoraPadrao = vm.ImprimirDireto ? ResolveTransportadoraDefaultPrinter() : null;
            vm.ImpressoraPadraoNome = impressoraPadrao != null ? impressoraPadrao.PrinterName : string.Empty;
            vm.ImpressoraPadraoIP = impressoraPadrao != null ? impressoraPadrao.PrinterTarget : string.Empty;
            vm.ImpressoraPadraoPorta = impressoraPadrao != null ? impressoraPadrao.PrinterPort : string.Empty;

            ViewBag.ImpressoraDDL = BuildExpedicaoImpressoraDDL();

            return vm;
        }

        private bool IsTransportadoraDirectPrintEnabled()
        {
            string value = GetAppConfigValueByName("ImpressaoDireto");
            if (string.IsNullOrWhiteSpace(value))
            {
                value = GetAppConfigValueByName("ImprimirDireto");
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            bool parsedBool;
            if (bool.TryParse(value.Trim(), out parsedBool))
            {
                return parsedBool;
            }

            string normalized = value.Trim();
            return normalized == "1" ||
                   normalized.Equals("sim", StringComparison.OrdinalIgnoreCase) ||
                   normalized.Equals("s", StringComparison.OrdinalIgnoreCase);
        }

        private TransportadoraDirectPrinterSettings ResolveTransportadoraDefaultPrinter()
        {
            Impressora impressora = ResolveExpedicaoDefaultPrinter();
            if (impressora == null ||
                string.IsNullOrWhiteSpace(impressora.Nome) ||
                string.IsNullOrWhiteSpace(impressora.IP) ||
                impressora.Porta <= 0)
            {
                return null;
            }

            return new TransportadoraDirectPrinterSettings
            {
                PrinterName = impressora.Nome.Trim(),
                PrinterTarget = impressora.IP.Trim(),
                PrinterPort = impressora.Porta.ToString()
            };
        }

        private List<SelectListItem> BuildExpedicaoImpressoraDDL()
        {
            Impressora impressoraPadrao = ResolveExpedicaoDefaultPrinter();
            int? impressoraPadraoId = impressoraPadrao != null ? (int?)impressoraPadrao.Id : null;

            return db.Impressora
                .Where(i => i.FilialId == filialId)
                .OrderBy(i => i.Nome)
                .ToList()
                .Select(i => new SelectListItem
                {
                    Value = i.Id.ToString(),
                    Text = i.Nome,
                    Selected = impressoraPadraoId.HasValue && i.Id == impressoraPadraoId.Value
                })
                .ToList();
        }

        private Impressora ResolveExpedicaoDefaultPrinter()
        {
            string printerName = GetAppConfigValueByName("ImpressoraPadrao");
            if (string.IsNullOrWhiteSpace(printerName))
            {
                return null;
            }

            string normalizedName = printerName.Trim();
            List<Impressora> impressoras = db.Impressora
                .Where(x => x.FilialId == filialId)
                .ToList();

            Impressora impressora = impressoras.FirstOrDefault(x =>
                string.Equals((x.Nome ?? string.Empty).Trim(), normalizedName, StringComparison.OrdinalIgnoreCase));

            return impressora;
        }

        private string GetAppConfigValueByName(string nome)
        {
            string valor = db.AppConfig
                .Where(x => x.Nome == nome && x.FilialId == filialId)
                .OrderBy(x => x.Id)
                .Select(x => x.Valor)
                .FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(valor))
            {
                return valor;
            }

            return db.AppConfig
                .Where(x => x.Nome == nome && (!x.FilialId.HasValue || x.FilialId == 0))
                .OrderBy(x => x.Id)
                .Select(x => x.Valor)
                .FirstOrDefault();
        }

        [HttpPost]
        public ActionResult LogTransportadoraPrintIssue(string etapa, string detalhe)
        {
            try
            {
                LogTransportadoraPrintIssue(
                    "ImportTransportadora",
                    string.IsNullOrWhiteSpace(etapa) ? "LogTransportadoraPrintIssue" : etapa,
                    detalhe);

                return Json(new { success = true, msg = "Log criado!" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, msg = BuildDetailedExceptionMessage(ex) }, JsonRequestBehavior.AllowGet);
            }
        }

        private List<DocExpedicao> GetImportedNotasFiscaisEmEspera()
        {
            List<string> numeros = db.Database
                .SqlQuery<string>("SELECT DISTINCT NotaFiscal FROM NotaFiscalTransportadora WHERE FilialId = @p0 AND NotaFiscal IS NOT NULL", filialId)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();

            if (numeros.Count == 0)
            {
                return new List<DocExpedicao>();
            }

            return db.DocExpedicao
                .Where(x => x.FilialId == filialId && x.StatusId == 1002 && numeros.Contains(x.Numero))
                .OrderBy(x => x.Numero)
                .ToList();
        }

        private List<string> BuildExpedicaoEtiquetas(List<DocExpedicao> notas)
        {
            List<string> listaEtiquetas = new List<string>();
            string templateZpl = (from e in db.Etiqueta
                                  where e.Nome == "Expedicao"
                                  select e.ZPL).FirstOrDefault();

            if (string.IsNullOrWhiteSpace(templateZpl))
            {
                return listaEtiquetas;
            }

            foreach (DocExpedicao doc in notas)
            {
                Transportadora transp = (from t in db.Transportadora
                                         where t.Id == doc.TransportadoraId
                                         select t).FirstOrDefault();

                if (transp == null || !transp.EmitirEtiqueta)
                {
                    continue;
                }

                DateTime dt = Util.GetCurrentDateTime();
                string zpl = templateZpl;
                zpl = zpl.Replace("local-origem", "Sorocaba");
                zpl = zpl.Replace("data-impressao", dt.ToString("dd/MM/yyyy"));
                zpl = zpl.Replace("hora-impressao", dt.ToString("HH:mm:ss"));
                zpl = zpl.Replace("nome-transportadora", transp.Nome_Fantasia ?? transp.Nome ?? string.Empty);

                char[] zero = { '0' };
                string nfAux = doc.Numero ?? string.Empty;
                zpl = zpl.Replace("nfiscal-nr", nfAux.TrimStart(zero));
                zpl = zpl.Replace("contato-nr", doc.Controle ?? string.Empty);

                Cliente cliente = (from c in db.Cliente
                                   where c.CodigoDMS == doc.CodigoCliente
                                   select c).FirstOrDefault();

                if (cliente != null)
                {
                    string cidadeEstado = string.Concat(cliente.Endereco_Cidade ?? string.Empty, "/", cliente.Endereco_UF ?? string.Empty).Trim('/');
                    zpl = zpl.Replace("ruanr-cliente", cliente.Endereco_Logradouro ?? string.Empty);
                    zpl = zpl.Replace("bairro-cliente", cliente.Endereco_Bairro ?? string.Empty);
                    zpl = zpl.Replace("cidadeestado-cliente", cidadeEstado);

                    string nomeCliente = cliente.Nome ?? string.Empty;
                    if (nomeCliente.Length > 14)
                    {
                        nomeCliente = nomeCliente.Substring(0, 14);
                    }

                    zpl = zpl.Replace("nome-cliente", nomeCliente);
                }

                string rota = (from r in db.Rota where r.Id == doc.RotaId select r.Nome).FirstOrDefault() ?? string.Empty;
                string parada = (from p in db.Parada where p.Id == doc.ParadaId select p.Nome).FirstOrDefault() ?? string.Empty;

                zpl = zpl.Replace("rota-cliente", rota);
                zpl = zpl.Replace("parada-cliente", parada);

                int qtdVolumes = doc.QtdVolumes ?? 1;
                for (int i = 1; i <= qtdVolumes; i++)
                {
                    string zpl2 = zpl.Replace("controlenr-quasar", string.Concat(doc.Numero, i.ToString().PadLeft(3, '0')));
                    string zpl3 = zpl2.Replace("sequencia-volume", string.Concat(i.ToString(), "/", qtdVolumes.ToString()));
                    listaEtiquetas.Add(Util.RemoverAcentuacao(zpl3));
                }
            }

            return listaEtiquetas;
        }

        private DataTable BuildNotaFiscalTransportadoraDataTable(List<NotaFiscalTransportadoraImportRow> items)
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("Transportadora", typeof(string));
            dt.Columns.Add("NotaFiscal", typeof(string));
            dt.Columns.Add("Volumes", typeof(int));
            dt.Columns.Add("Cliente", typeof(string));
            dt.Columns.Add("Contato", typeof(string));
            dt.Columns.Add("VolumeNr", typeof(string));
            dt.Columns.Add("FilialId", typeof(int));
            dt.Columns.Add("Sequencia", typeof(string));
            dt.Columns.Add("ZPL", typeof(string));

            foreach (NotaFiscalTransportadoraImportRow item in items)
            {
                DataRow row = dt.NewRow();
                row["Transportadora"] = item.Transportadora != null ? (object)item.Transportadora : DBNull.Value;
                row["NotaFiscal"] = item.NotaFiscalNr != null ? (object)item.NotaFiscalNr : DBNull.Value;
                row["Volumes"] = item.Volumes.HasValue ? (object)item.Volumes.Value : DBNull.Value;
                row["Cliente"] = item.Cliente != null ? (object)item.Cliente : DBNull.Value;
                row["Contato"] = item.ContatoNr != null ? (object)item.ContatoNr : DBNull.Value;
                row["VolumeNr"] = item.VolumeNr != null ? (object)item.VolumeNr : DBNull.Value;
                row["FilialId"] = filialId;
                row["Sequencia"] = item.Sequencia != null ? (object)item.Sequencia : DBNull.Value;
                row["ZPL"] = item.ZPL != null ? (object)item.ZPL : DBNull.Value;
                dt.Rows.Add(row);
            }

            return dt;
        }

        private TransportadoraImportParseResult ParseTransportadoraFile(
            HttpPostedFileBase arquivo,
            string transportadora)
        {
            List<List<string>> pages = ExtractTransportadoraPdfTokens(arquivo);
            if (pages.Count == 0)
            {
                throw new Exception("Nenhum texto legível foi localizado no PDF informado");
            }

            List<TransportadoraPdfRow> pdfRows = new List<TransportadoraPdfRow>();
            foreach (List<string> pageTokens in pages)
            {
                pdfRows.AddRange(ParseTransportadoraPdfRows(pageTokens));
            }

            if (pdfRows.Count == 0)
            {
                throw new Exception("Nenhuma linha válida foi localizada no PDF da transportadora");
            }

            TransportadoraDocExpedicaoResolutionResult resolution = ResolveDocExpedicaoParaImportacaoTransportadora(pdfRows);
            List<DocExpedicaoTransportadoraLink> documentosNovos = resolution.DocumentosNovos;
            TransportadoraImportDiagnostics diagnostics = new TransportadoraImportDiagnostics
            {
                PaginasLidas = pages.Count,
                LinhasPdf = pdfRows.Count,
                NotasDistintasPdf = resolution.NotasDistintasPdf,
                NotasJaExistentesDocExpedicao = resolution.NotasJaExistentesDocExpedicao,
                NotasNovasDocExpedicao = resolution.DocumentosNovos.Count
            };
            if (documentosNovos.Count == 0)
            {
                return new TransportadoraImportParseResult
                {
                    Items = new List<NotaFiscalTransportadoraImportRow>(),
                    Diagnostics = diagnostics
                };
            }

            string templateZpl = ResolveTransportadoraTemplateZpl();
            string localOrigem = ResolveLocalOrigem();
            DateTime dataImpressao = Util.GetCurrentDateTime();
            Dictionary<string, string> clienteCidadeEstadoByNome = GetClienteCidadeEstadoByNome(
                documentosNovos.Select(x => x.Cliente)
                    .Concat(pdfRows.Select(x => x.Destinatario)));
            Dictionary<string, bool> clienteEtiquetaByNome = GetClienteEtiquetaByNome(
                documentosNovos.Select(x => x.Cliente)
                    .Concat(pdfRows.Select(x => x.Destinatario)));
            List<NotaFiscalTransportadoraImportRow> items = new List<NotaFiscalTransportadoraImportRow>();
            int linhasSemDocumento = 0;
            int linhasSemVolume = 0;
            int linhasClienteSemEtiqueta = 0;
            foreach (TransportadoraPdfRow row in pdfRows)
            {
                DocExpedicaoTransportadoraLink documento = MatchDocExpedicaoByNotaFiscal(documentosNovos, row);
                if (documento == null)
                {
                    linhasSemDocumento++;
                    continue;
                }

                int totalVolumes = row.Volumes ?? 0;
                if (totalVolumes <= 0)
                {
                    linhasSemVolume++;
                    continue;
                }

                string notaFiscalNr = documento.NotaFiscalNr;
                string cliente = !string.IsNullOrWhiteSpace(row.Destinatario)
                    ? row.Destinatario
                    : documento.Cliente;
                if (!ClientePermiteGerarEtiqueta(clienteEtiquetaByNome, cliente))
                {
                    linhasClienteSemEtiqueta++;
                    continue;
                }

                string cidadeEstadoCliente = ResolveClienteCidadeEstado(clienteCidadeEstadoByNome, cliente);
                for (int volume = 1; volume <= totalVolumes; volume++)
                {
                    string volumeNr = BuildTransportadoraVolumeNr(notaFiscalNr, volume);
                    string sequencia = string.Concat(volume.ToString(), "/", totalVolumes.ToString());
                    items.Add(new NotaFiscalTransportadoraImportRow
                    {
                        Transportadora = transportadora,
                        NotaFiscalNr = notaFiscalNr,
                        Volumes = totalVolumes,
                        Cliente = cliente,
                        ContatoNr = documento.ContatoNr,
                        VolumeNr = volumeNr,
                        DocExpedicaoId = documento.Id,
                        Sequencia = sequencia,
                        ZPL = BuildNotaFiscalTransportadoraZpl(
                            templateZpl,
                            localOrigem,
                            transportadora,
                            notaFiscalNr,
                            cliente,
                            documento.ContatoNr,
                            sequencia,
                            volumeNr,
                            cidadeEstadoCliente,
                            dataImpressao)
                    });
                }
            }

            int registrosAntesDeduplicacao = items.Count;
            items = items
                .GroupBy(x => new
                {
                    NotaFiscal = NormalizeNotaFiscalNr(x.NotaFiscalNr),
                    Contato = NormalizeTransportadoraWhitespace(x.ContatoNr),
                    Volume = NormalizeTransportadoraWhitespace(x.VolumeNr)
                })
                .Select(x => x.First())
                .ToList();

            diagnostics.LinhasIgnoradasSemDocumentoNovo = linhasSemDocumento;
            diagnostics.LinhasIgnoradasSemVolume = linhasSemVolume;
            diagnostics.LinhasIgnoradasClienteSemEtiqueta = linhasClienteSemEtiqueta;
            diagnostics.RegistrosDuplicadosIgnorados = registrosAntesDeduplicacao - items.Count;
            diagnostics.RegistrosEtiquetaGerados = items.Count;
            diagnostics.VolumesGerados = items.Count;

            return new TransportadoraImportParseResult
            {
                Items = items,
                Diagnostics = diagnostics
            };
        }

        private TransportadoraDocExpedicaoResolutionResult ResolveDocExpedicaoParaImportacaoTransportadora(
            IEnumerable<TransportadoraPdfRow> pdfRows)
        {
            List<TransportadoraPdfRow> linhas = pdfRows?
                .Where(x => !string.IsNullOrWhiteSpace(NormalizeNotaFiscalNr(x.NotaFiscalNr)))
                .ToList() ?? new List<TransportadoraPdfRow>();

            if (linhas.Count == 0)
            {
                return new TransportadoraDocExpedicaoResolutionResult();
            }

            List<string> numerosNormalizados = linhas
                .Select(x => NormalizeNotaFiscalNr(x.NotaFiscalNr))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            HashSet<string> numerosExistentes = GetNumerosNotaFiscalExistentesDocExpedicao();

            List<DocExpedicaoTransportadoraLink> documentosNovos = new List<DocExpedicaoTransportadoraLink>();
            foreach (TransportadoraPdfRow row in linhas
                .GroupBy(x => NormalizeNotaFiscalNr(x.NotaFiscalNr), StringComparer.OrdinalIgnoreCase)
                .Select(x => x.First()))
            {
                string numero = NormalizeNotaFiscalNr(row.NotaFiscalNr);
                if (string.IsNullOrWhiteSpace(numero) || numerosExistentes.Contains(numero))
                {
                    continue;
                }

                documentosNovos.Add(new DocExpedicaoTransportadoraLink
                {
                    Id = 0,
                    NotaFiscalNr = numero,
                    Cliente = NormalizeTransportadoraWhitespace(row.Destinatario) ?? string.Empty,
                    ContatoNr = NormalizeTransportadoraWhitespace(row.ContatoNr) ?? string.Empty
                });
                numerosExistentes.Add(numero);
            }

            return new TransportadoraDocExpedicaoResolutionResult
            {
                DocumentosNovos = documentosNovos,
                NotasDistintasPdf = numerosNormalizados.Count,
                NotasJaExistentesDocExpedicao = numerosNormalizados.Count - documentosNovos.Count
            };
        }

        private HashSet<string> EnsureDocExpedicaoCriadoPorImportacaoTransportadora(
            List<NotaFiscalTransportadoraImportRow> items,
            int? tipoMovimentoId,
            Transportadora transportadora)
        {
            HashSet<string> numerosCriados = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            List<NotaFiscalTransportadoraImportRow> registros = items?
                .Where(x => !string.IsNullOrWhiteSpace(NormalizeNotaFiscalNr(x.NotaFiscalNr)))
                .ToList();

            if (registros == null || registros.Count == 0)
            {
                return numerosCriados;
            }

            using (DbContextTransaction tr = db.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                HashSet<string> numerosExistentes = GetNumerosNotaFiscalExistentesDocExpedicao();
                DateTime agora = Util.GetCurrentDateTime();
                List<DocExpedicao> novosDocumentos = new List<DocExpedicao>();
                foreach (NotaFiscalTransportadoraImportRow item in registros
                    .GroupBy(x => NormalizeNotaFiscalNr(x.NotaFiscalNr), StringComparer.OrdinalIgnoreCase)
                    .Select(x => x.First()))
                {
                    string numero = NormalizeNotaFiscalNr(item.NotaFiscalNr);
                    if (string.IsNullOrWhiteSpace(numero) || numerosExistentes.Contains(numero))
                    {
                        continue;
                    }

                    novosDocumentos.Add(new DocExpedicao
                    {
                        Numero = numero,
                        DataEmissao = agora,
                        Classificacao = string.Empty,
                        Controle = NormalizeTransportadoraWhitespace(item.ContatoNr) ?? string.Empty,
                        Vendedor = string.Empty,
                        CodigoCliente = string.Empty,
                        NomeCliente = NormalizeTransportadoraWhitespace(item.Cliente) ?? string.Empty,
                        TransportadoraId = transportadora.Id,
                        StatusId = ResolveDocExpedicaoStatusByTransportadora(transportadora),
                        RoteiroImpresso = null,
                        QtdVolumes = item.Volumes,
                        TipoMovimentoId = tipoMovimentoId,
                        CriadoPor = current_user,
                        CriadoEm = agora,
                        ModificadoPor = current_user,
                        ModificadoEm = agora,
                        FilialId = filialId
                    });

                    numerosExistentes.Add(numero);
                    numerosCriados.Add(numero);
                }

                if (novosDocumentos.Count > 0)
                {
                    db.DocExpedicao.AddRange(novosDocumentos);
                    db.SaveChanges();
                }

                tr.Commit();
            }

            return numerosCriados;
        }

        private HashSet<string> GetNumerosNotaFiscalExistentesDocExpedicao()
        {
            List<string> numeros = db.DocExpedicao
                .AsNoTracking()
                .Where(x => x.FilialId == filialId && x.Numero != null && x.Numero != string.Empty)
                .Select(x => x.Numero)
                .ToList();

            return new HashSet<string>(
                numeros
                    .Select(NormalizeNotaFiscalNr)
                    .Where(x => !string.IsNullOrWhiteSpace(x)),
                StringComparer.OrdinalIgnoreCase);
        }

        private static int ResolveDocExpedicaoStatusByTransportadora(Transportadora transportadora)
        {
            if (transportadora == null)
            {
                return 2;
            }

            if (transportadora.EmitirRoteiro)
            {
                return 3;
            }

            return transportadora.Finalizar ? 4 : 2;
        }

        private static string BuildEntityValidationErrorMessage(DbEntityValidationException ex)
        {
            StringBuilder msgErro = new StringBuilder();
            foreach (DbEntityValidationResult item in ex.EntityValidationErrors)
            {
                foreach (DbValidationError subItem in item.ValidationErrors)
                {
                    if (msgErro.Length > 0)
                    {
                        msgErro.Append(" | ");
                    }

                    msgErro.Append(subItem.PropertyName);
                    msgErro.Append(": ");
                    msgErro.Append(subItem.ErrorMessage);
                }
            }

            return msgErro.Length > 0 ? msgErro.ToString() : ex.Message;
        }

        private static string BuildTransportadoraImportDiagnosticMessage(TransportadoraImportDiagnostics diagnostics)
        {
            if (diagnostics == null)
            {
                return string.Empty;
            }

            return string.Format(
                CultureInfo.InvariantCulture,
                "Páginas lidas: {0} | Linhas do PDF: {1} | NFs distintas no PDF: {2} | NFs já existentes na DocExpedicao: {3} | NFs novas: {4} | Linhas ignoradas sem documento novo: {5} | Linhas ignoradas sem volume: {6} | Linhas ignoradas por Cliente.Etiqueta diferente de 1: {7} | Registros duplicados ignorados: {8} | Registros gerados na NotaFiscalTransportadora: {9}",
                diagnostics.PaginasLidas,
                diagnostics.LinhasPdf,
                diagnostics.NotasDistintasPdf,
                diagnostics.NotasJaExistentesDocExpedicao,
                diagnostics.NotasNovasDocExpedicao,
                diagnostics.LinhasIgnoradasSemDocumentoNovo,
                diagnostics.LinhasIgnoradasSemVolume,
                diagnostics.LinhasIgnoradasClienteSemEtiqueta,
                diagnostics.RegistrosDuplicadosIgnorados,
                diagnostics.RegistrosEtiquetaGerados);
        }

        private Dictionary<string, bool> GetClienteEtiquetaByNome(IEnumerable<string> nomes)
        {
            List<string> nomesClientes = nomes
                .Select(NormalizeTransportadoraWhitespace)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            Dictionary<string, bool> resultado = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            if (nomesClientes.Count == 0)
            {
                return resultado;
            }

            List<Cliente> clientes = db.Cliente
                .AsNoTracking()
                .Where(x =>
                    x.Nome != null &&
                    nomesClientes.Contains(x.Nome.Trim()) &&
                    (x.FilialId == filialId || !x.FilialId.HasValue || x.FilialId == 0))
                .ToList();

            foreach (IGrouping<string, Cliente> grupo in clientes
                .Where(x => !string.IsNullOrWhiteSpace(x.Nome))
                .GroupBy(x => NormalizeTransportadoraWhitespace(x.Nome), StringComparer.OrdinalIgnoreCase))
            {
                List<Cliente> candidatosFilial = grupo
                    .Where(x => x.FilialId == filialId)
                    .ToList();
                List<Cliente> candidatos = candidatosFilial.Count > 0
                    ? candidatosFilial
                    : grupo.Where(x => !x.FilialId.HasValue || x.FilialId == 0).ToList();

                if (candidatos.Count > 0)
                {
                    resultado[grupo.Key] = candidatos.Any(x => x.Etiqueta == true);
                }
            }

            return resultado;
        }

        private static bool ClientePermiteGerarEtiqueta(
            IDictionary<string, bool> clienteEtiquetaByNome,
            string nomeCliente)
        {
            if (clienteEtiquetaByNome == null || string.IsNullOrWhiteSpace(nomeCliente))
            {
                return true;
            }

            bool gerarEtiqueta;
            string nomeNormalizado = NormalizeTransportadoraWhitespace(nomeCliente);
            return string.IsNullOrWhiteSpace(nomeNormalizado) ||
                   !clienteEtiquetaByNome.TryGetValue(nomeNormalizado, out gerarEtiqueta) ||
                   gerarEtiqueta;
        }

        private Dictionary<string, string> GetClienteCidadeEstadoByNome(IEnumerable<string> nomes)
        {
            List<string> nomesClientes = nomes
                .Select(NormalizeTransportadoraWhitespace)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            Dictionary<string, string> resultado = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (nomesClientes.Count == 0)
            {
                return resultado;
            }

            List<Cliente> clientes = db.Cliente
                .AsNoTracking()
                .Where(x => nomesClientes.Contains(x.Nome))
                .OrderByDescending(x => x.FilialId == filialId)
                .ToList();

            foreach (IGrouping<string, Cliente> grupo in clientes
                .Where(x => !string.IsNullOrWhiteSpace(x.Nome))
                .GroupBy(x => NormalizeTransportadoraWhitespace(x.Nome), StringComparer.OrdinalIgnoreCase))
            {
                string nome = grupo.Key;
                if (string.IsNullOrWhiteSpace(nome))
                {
                    continue;
                }

                if (grupo.Count() != 1)
                {
                    resultado[nome] = string.Empty;
                    continue;
                }

                Cliente cliente = grupo.First();
                string cidadeEstado = string.Concat(cliente.Endereco_Cidade ?? string.Empty, "/", cliente.Endereco_UF ?? string.Empty).Trim('/');
                resultado[nome] = cidadeEstado;
            }

            return resultado;
        }

        private static string ResolveClienteCidadeEstado(Dictionary<string, string> clienteCidadeEstadoByNome, string cliente)
        {
            if (clienteCidadeEstadoByNome == null || string.IsNullOrWhiteSpace(cliente))
            {
                return string.Empty;
            }

            string nome = NormalizeTransportadoraWhitespace(cliente);
            string cidadeEstado;
            return !string.IsNullOrWhiteSpace(nome) && clienteCidadeEstadoByNome.TryGetValue(nome, out cidadeEstado)
                ? cidadeEstado ?? string.Empty
                : string.Empty;
        }

        private string ResolveTransportadoraTemplateZpl()
        {
            string templateZpl = db.Etiqueta
                .Where(e => e.Nome == "Expedicao" && (e.FilialId == filialId || e.FilialId == null))
                .OrderByDescending(e => e.FilialId)
                .Select(e => e.ZPL)
                .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(templateZpl))
            {
                templateZpl = db.Etiqueta
                    .Where(e => e.Nome == "Expedicao")
                    .Select(e => e.ZPL)
                    .FirstOrDefault();
            }

            return templateZpl ?? string.Empty;
        }

        private string ResolveLocalOrigem()
        {
            string nomeFilial = db.Empresa
                .Where(x => x.Id == filialId)
                .Select(x => x.Nome)
                .FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(nomeFilial))
            {
                return nomeFilial;
            }

            string localOrigem = db.LocalOrigem
                .Where(x => x.FilialId == filialId)
                .OrderBy(x => x.Id)
                .Select(x => x.Nome)
                .FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(localOrigem))
            {
                return localOrigem;
            }

            return filialId.ToString();
        }

        private static string BuildNotaFiscalTransportadoraZpl(
            string templateZpl,
            string localOrigem,
            string transportadora,
            string notaFiscalNr,
            string cliente,
            string contatoNr,
            string sequencia,
            string controlenr,
            string cidadeEstadoCliente,
            DateTime dataImpressao)
        {
            if (string.IsNullOrWhiteSpace(templateZpl))
            {
                return null;
            }

            string zpl = templateZpl;
            zpl = zpl.Replace("controlenr-quasar", controlenr ?? string.Empty);
            zpl = zpl.Replace("nome-transportadora", transportadora ?? string.Empty);
            zpl = zpl.Replace("nfiscal-nr", FormatNotaFiscalForEtiqueta(notaFiscalNr));
            zpl = zpl.Replace("nome-cliente", cliente ?? string.Empty);
            zpl = zpl.Replace("contato-nr", contatoNr ?? string.Empty);
            zpl = zpl.Replace("sequencia-volume", sequencia ?? string.Empty);
            zpl = zpl.Replace("cidadeestado-cliente", cidadeEstadoCliente ?? string.Empty);
            zpl = zpl.Replace("local-origem", localOrigem ?? string.Empty);
            zpl = zpl.Replace("data-impressao", dataImpressao.ToString("dd/MM/yyyy"));
            zpl = zpl.Replace("hora-impressao", dataImpressao.ToString("HH:mm:ss"));

            return Util.RemoverAcentuacao(zpl);
        }

        private static string FormatNotaFiscalForEtiqueta(string notaFiscalNr)
        {
            if (string.IsNullOrWhiteSpace(notaFiscalNr))
            {
                return string.Empty;
            }

            string numero = notaFiscalNr.TrimStart('0');
            return string.IsNullOrWhiteSpace(numero) ? "0" : numero;
        }

        private static DocExpedicaoTransportadoraLink MatchDocExpedicaoByNotaFiscal(
            List<DocExpedicaoTransportadoraLink> documentos,
            TransportadoraPdfRow row)
        {
            string notaFiscalPdf = NormalizeNotaFiscalNr(row.NotaFiscalNr);
            if (string.IsNullOrWhiteSpace(notaFiscalPdf))
            {
                return null;
            }

            List<DocExpedicaoTransportadoraLink> candidatos = documentos
                .Where(x => string.Equals(NormalizeNotaFiscalNr(x.NotaFiscalNr), notaFiscalPdf, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(x => x.Id)
                .ToList();

            if (candidatos.Count == 0)
            {
                return null;
            }

            if (candidatos.Count == 1)
            {
                return candidatos[0];
            }

            string contato = NormalizeTransportadoraWhitespace(row.ContatoNr);
            if (string.IsNullOrWhiteSpace(contato))
            {
                return candidatos[0];
            }

            List<DocExpedicaoTransportadoraLink> candidatosPorContato = candidatos
                .Where(x => string.Equals(NormalizeTransportadoraWhitespace(x.ContatoNr), contato, StringComparison.OrdinalIgnoreCase))
                .ToList();

            return candidatosPorContato.Count > 0 ? candidatosPorContato[0] : candidatos[0];
        }

        private List<List<string>> ExtractTransportadoraPdfTokens(HttpPostedFileBase arquivo)
        {
            return ExecuteWithPdfSharpAssemblyResolver(() =>
            {
                MemoryStream stream = new MemoryStream();
                arquivo.InputStream.Position = 0;
                arquivo.InputStream.CopyTo(stream);
                stream.Position = 0;

                List<List<string>> pages = new List<List<string>>();
                Assembly pdfSharpAssembly = ResolvePdfSharpAssembly();
                Type pdfReaderType = pdfSharpAssembly.GetType("PdfSharp.Pdf.IO.PdfReader", true);
                Type documentOpenModeType = pdfSharpAssembly.GetType("PdfSharp.Pdf.IO.PdfDocumentOpenMode", true);
                Type contentReaderType = pdfSharpAssembly.GetType("PdfSharp.Pdf.Content.ContentReader", true);
                object document = OpenPdfDocument(pdfReaderType, documentOpenModeType, stream);
                try
                {
                    object pagesCollection = document.GetType().GetProperty("Pages").GetValue(document);
                    int pageCount = (int)pagesCollection.GetType().GetProperty("Count").GetValue(pagesCollection);
                    MethodInfo readContentMethod = contentReaderType
                        .GetMethods(BindingFlags.Public | BindingFlags.Static)
                        .FirstOrDefault(m =>
                        {
                            if (m.Name != "ReadContent")
                            {
                                return false;
                            }

                            ParameterInfo[] parameters = m.GetParameters();
                            return parameters.Length == 1 &&
                                   string.Equals(parameters[0].ParameterType.FullName, "PdfSharp.Pdf.PdfPage", StringComparison.Ordinal);
                        });

                    if (readContentMethod == null)
                    {
                        throw new InvalidOperationException("Não foi possível localizar o método ReadContent(PdfPage) do PdfSharp.");
                    }

                    for (int i = 0; i < pageCount; i++)
                    {
                        object page = pagesCollection.GetType().GetProperty("Item").GetValue(pagesCollection, new object[] { i });
                        object content = InvokeReflectionMethod(
                            readContentMethod,
                            null,
                            new[] { page },
                            "Não foi possível ler o conteúdo de uma página do PDF");

                        List<string> tokens = new List<string>();
                        CollectTransportadoraPdfTokens(content, tokens);

                        List<string> normalizedTokens = tokens
                            .Select(NormalizeTransportadoraWhitespace)
                            .Where(x => !string.IsNullOrWhiteSpace(x))
                            .ToList();

                        if (normalizedTokens.Count > 0)
                        {
                            pages.Add(normalizedTokens);
                        }
                    }
                }
                finally
                {
                    IDisposable disposable = document as IDisposable;
                    if (disposable != null)
                    {
                        disposable.Dispose();
                    }
                }

                return pages;
            });
        }

        private static T ExecuteWithPdfSharpAssemblyResolver<T>(Func<T> action)
        {
            ResolveEventHandler handler = (sender, args) => TryResolveAssemblyFromBin(args.Name);
            AppDomain.CurrentDomain.AssemblyResolve += handler;
            try
            {
                EnsurePdfSharpRuntimeDependenciesLoaded();
                return action();
            }
            finally
            {
                AppDomain.CurrentDomain.AssemblyResolve -= handler;
            }
        }

        private static Assembly TryResolveAssemblyFromBin(string assemblyDisplayName)
        {
            if (string.IsNullOrWhiteSpace(assemblyDisplayName))
            {
                return null;
            }

            try
            {
                AssemblyName requestedName = new AssemblyName(assemblyDisplayName);
                Assembly loadedAssembly = AppDomain.CurrentDomain
                    .GetAssemblies()
                    .FirstOrDefault(a => string.Equals(a.GetName().Name, requestedName.Name, StringComparison.OrdinalIgnoreCase));
                if (loadedAssembly != null)
                {
                    return loadedAssembly;
                }

                string assemblyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin", requestedName.Name + ".dll");
                if (!System.IO.File.Exists(assemblyPath))
                {
                    return null;
                }

                return Assembly.LoadFrom(assemblyPath);
            }
            catch
            {
                return null;
            }
        }

        private static void EnsurePdfSharpRuntimeDependenciesLoaded()
        {
            string[] assemblyNames =
            {
                "Microsoft.Extensions.Logging.Abstractions",
                "Microsoft.Extensions.DependencyInjection.Abstractions",
                "Microsoft.Bcl.AsyncInterfaces",
                "System.Security.Cryptography.Pkcs",
                "System.Memory",
                "System.Buffers",
                "System.Runtime.CompilerServices.Unsafe",
                "System.Threading.Tasks.Extensions",
                "PdfSharp.Shared",
                "PdfSharp.System"
            };

            foreach (string assemblyName in assemblyNames)
            {
                TryResolveAssemblyFromBin(assemblyName);
            }
        }

        private static object OpenPdfDocument(Type pdfReaderType, Type documentOpenModeType, Stream stream)
        {
            MethodInfo[] openMethods = pdfReaderType
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.Name == "Open")
                .ToArray();

            MethodInfo streamOnlyMethod = openMethods.FirstOrDefault(m =>
            {
                ParameterInfo[] parameters = m.GetParameters();
                return parameters.Length == 1 &&
                       typeof(Stream).IsAssignableFrom(parameters[0].ParameterType);
            });

            if (streamOnlyMethod != null)
            {
                return InvokeReflectionMethod(
                    streamOnlyMethod,
                    null,
                    new object[] { stream },
                    "Não foi possível abrir o PDF informado");
            }

            Type readerOptionsType = pdfReaderType.Assembly.GetType("PdfSharp.Pdf.IO.PdfReaderOptions", true);
            object readOnlyMode = Enum.Parse(documentOpenModeType, "ReadOnly");

            MethodInfo streamModeOptionsMethod = openMethods.FirstOrDefault(m =>
            {
                ParameterInfo[] parameters = m.GetParameters();
                return parameters.Length == 3 &&
                       typeof(Stream).IsAssignableFrom(parameters[0].ParameterType) &&
                       parameters[1].ParameterType == documentOpenModeType &&
                       parameters[2].ParameterType == readerOptionsType;
            });

            if (streamModeOptionsMethod != null)
            {
                return InvokeReflectionMethod(
                    streamModeOptionsMethod,
                    null,
                    new[] { stream, readOnlyMode, null },
                    "Não foi possível abrir o PDF informado");
            }

            throw new InvalidOperationException("Não foi possível localizar uma sobrecarga compatível de PdfReader.Open para leitura do PDF.");
        }

        private static object InvokeReflectionMethod(MethodInfo method, object instance, object[] parameters, string operationDescription)
        {
            try
            {
                return method.Invoke(instance, parameters);
            }
            catch (TargetInvocationException ex)
            {
                Exception baseException = ex.InnerException ?? ex;
                string baseMessage = BuildDetailedExceptionMessage(baseException);
                throw new InvalidOperationException(operationDescription + ": " + baseMessage, baseException);
            }
        }

        private static string BuildDetailedExceptionMessage(Exception ex)
        {
            if (ex == null)
            {
                return "Erro não identificado.";
            }

            Exception current = ex;
            while (current is TargetInvocationException && current.InnerException != null)
            {
                current = current.InnerException;
            }

            while (current.InnerException != null &&
                   !(current is DbEntityValidationException))
            {
                current = current.InnerException;
            }

            return string.IsNullOrWhiteSpace(current.Message)
                ? ex.Message
                : current.Message;
        }

        private void LogTransportadoraPrintIssue(string action, string instrucao, string errorMessage)
        {
            try
            {
                AppLogErro erro = new AppLogErro
                {
                    Area = "Expedicao",
                    Controller = "NotaFiscal",
                    Action = action,
                    Instrucao = instrucao,
                    ErrorCode = string.Empty,
                    ErrorMessage = errorMessage,
                    Usuario = current_user,
                    FilialId = filialId,
                    DataHora = Util.GetCurrentDateTime()
                };

                db.AppLogErro.Add(erro);
                db.SaveChanges();
            }
            catch
            {
            }
        }

        private static void CollectTransportadoraPdfTokens(object content, List<string> tokens)
        {
            if (content == null)
            {
                return;
            }

            if (IsPdfSharpObjectType(content, "PdfSharp.Pdf.Content.Objects.CSequence"))
            {
                for (int i = 0; i < GetPdfSharpCollectionCount(content); i++)
                {
                    CollectTransportadoraPdfTokens(GetPdfSharpCollectionItem(content, i), tokens);
                }

                return;
            }

            if (IsPdfSharpObjectType(content, "PdfSharp.Pdf.Content.Objects.COperator"))
            {
                string operatorName = content.GetType().GetProperty("Name").GetValue(content) as string;
                if (IsTransportadoraTextOperator(operatorName))
                {
                    object operands = content.GetType().GetProperty("Operands").GetValue(content);
                    AppendTransportadoraTextOperands(operands, tokens);
                }

                return;
            }
        }

        private static bool IsTransportadoraTextOperator(string operatorName)
        {
            return operatorName == "Tj" ||
                   operatorName == "TJ" ||
                   operatorName == "'" ||
                   operatorName == "\"";
        }

        private static void AppendTransportadoraTextOperands(object operands, List<string> tokens)
        {
            if (operands == null)
            {
                return;
            }

            for (int i = 0; i < GetPdfSharpCollectionCount(operands); i++)
            {
                AppendTransportadoraTextObject(GetPdfSharpCollectionItem(operands, i), tokens);
            }
        }

        private static void AppendTransportadoraTextObject(object value, List<string> tokens)
        {
            if (IsPdfSharpObjectType(value, "PdfSharp.Pdf.Content.Objects.CString"))
            {
                string rawValue = value.GetType().GetProperty("Value").GetValue(value) as string;
                string normalized = NormalizeTransportadoraWhitespace(rawValue);
                if (!string.IsNullOrWhiteSpace(normalized))
                {
                    tokens.Add(normalized);
                }

                return;
            }

            if (IsPdfSharpObjectType(value, "PdfSharp.Pdf.Content.Objects.CArray"))
            {
                for (int i = 0; i < GetPdfSharpCollectionCount(value); i++)
                {
                    AppendTransportadoraTextObject(GetPdfSharpCollectionItem(value, i), tokens);
                }
            }
        }

        private static Assembly ResolvePdfSharpAssembly()
        {
            string[] preferredAssemblies = { "PdfSharp-gdi", "PdfSharp-wpf" };
            List<string> loadErrors = new List<string>();

            foreach (string assemblyName in preferredAssemblies)
            {
                Assembly loadedAssembly = AppDomain.CurrentDomain
                    .GetAssemblies()
                    .FirstOrDefault(a => string.Equals(a.GetName().Name, assemblyName, StringComparison.OrdinalIgnoreCase));

                if (loadedAssembly != null)
                {
                    return loadedAssembly;
                }

                try
                {
                    return Assembly.Load(assemblyName);
                }
                catch (Exception ex)
                {
                    Assembly fileAssembly = TryResolveAssemblyFromBin(assemblyName);
                    if (fileAssembly != null)
                    {
                        return fileAssembly;
                    }

                    loadErrors.Add(assemblyName + ": " + BuildDetailedExceptionMessage(ex));
                }
            }

            throw new InvalidOperationException("Não foi possível localizar uma assembly compatível do PdfSharp para leitura do PDF. " + string.Join(" | ", loadErrors));
        }

        private static bool IsPdfSharpObjectType(object value, string fullTypeName)
        {
            return value != null && string.Equals(value.GetType().FullName, fullTypeName, StringComparison.Ordinal);
        }

        private static int GetPdfSharpCollectionCount(object collection)
        {
            return (int)collection.GetType().GetProperty("Count").GetValue(collection);
        }

        private static object GetPdfSharpCollectionItem(object collection, int index)
        {
            return collection.GetType().GetProperty("Item").GetValue(collection, new object[] { index });
        }

        private List<TransportadoraPdfRow> ParseTransportadoraPdfRows(IReadOnlyList<string> tokens)
        {
            int start = FindTransportadoraHeaderStartIndex(tokens);
            if (start < 0)
            {
                List<TransportadoraPdfRow> fallbackRows = ParseTransportadoraPdfRowsByLine(tokens);
                return fallbackRows.Count > 0 ? fallbackRows : ParseTransportadoraPdfRowsFromBlock(tokens);
            }

            List<TransportadoraPdfRow> lineRows = ParseTransportadoraPdfRowsByLine(tokens.Skip(start));
            if (lineRows.Count > 0)
            {
                return lineRows;
            }

            List<TransportadoraPdfRow> rows = new List<TransportadoraPdfRow>();
            for (int i = start; i + 4 < tokens.Count;)
            {
                TransportadoraPdfRow row;
                int nextIndex;

                if (TryParseTransportadoraPdfRow(tokens, i, out row, out nextIndex))
                {
                    rows.Add(row);
                    i = nextIndex;
                    continue;
                }

                i++;
            }

            return rows.Count > 0 ? rows : ParseTransportadoraPdfRowsFromBlock(tokens.Skip(start));
        }

        private static List<TransportadoraPdfRow> ParseTransportadoraPdfRowsByLine(IEnumerable<string> lines)
        {
            Regex lineRegex = new Regex(
                @"^(?<contato>\d{4,})\s+(?<nota>\d+(?:\s*-\s*\d+)?)\s+(?<dest>.+?)\s+(?<data>\d{1,2}[/-]\d{1,2}[/-]\d{4})(?:\s+(?<valor>R?\$?\s*[\d\.\,]+))?\s+(?<vol>\d+)\s*$",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);

            List<string> entries = lines
                .Select(NormalizeTransportadoraWhitespace)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();

            List<TransportadoraPdfRow> rows = new List<TransportadoraPdfRow>();
            for (int i = 0; i < entries.Count; i++)
            {
                string normalizedLine = entries[i];
                if (string.IsNullOrWhiteSpace(normalizedLine) || IsTransportadoraHeaderLine(normalizedLine))
                {
                    continue;
                }

                TransportadoraPdfRow parsedRow;
                int consumed;
                if (!TryParseTransportadoraPdfRowFromText(entries, i, lineRegex, out parsedRow, out consumed))
                {
                    continue;
                }

                rows.Add(parsedRow);
                i += consumed - 1;
            }

            return rows;
        }

        private static bool TryParseTransportadoraPdfRowFromText(
            IReadOnlyList<string> entries,
            int startIndex,
            Regex lineRegex,
            out TransportadoraPdfRow row,
            out int consumed)
        {
            row = null;
            consumed = 0;

            int maxWindow = Math.Min(8, entries.Count - startIndex);
            for (int window = 1; window <= maxWindow; window++)
            {
                string candidate = string.Join(" ", entries.Skip(startIndex).Take(window));
                if (IsTransportadoraHeaderLine(candidate))
                {
                    continue;
                }

                Match match = lineRegex.Match(candidate);
                if (!match.Success)
                {
                    continue;
                }

                int parsedVolume;
                if (!int.TryParse(match.Groups["vol"].Value, out parsedVolume))
                {
                    continue;
                }

                string contatoNr = NormalizeTransportadoraContato(match.Groups["contato"].Value);
                string notaFiscalNr = NormalizeTransportadoraNotaFiscal(match.Groups["nota"].Value);
                string destinatario = NormalizeTransportadoraDestinatario(match.Groups["dest"].Value);
                if (!IsValidTransportadoraPdfFields(contatoNr, notaFiscalNr, destinatario))
                {
                    continue;
                }

                row = new TransportadoraPdfRow
                {
                    ContatoNr = contatoNr,
                    NotaFiscalNr = notaFiscalNr,
                    Destinatario = destinatario,
                    Volumes = parsedVolume
                };

                consumed = window;
                return true;
            }

            return false;
        }

        private static List<TransportadoraPdfRow> ParseTransportadoraPdfRowsFromBlock(IEnumerable<string> entries)
        {
            string block = string.Join(" ", entries
                .Select(NormalizeTransportadoraWhitespace)
                .Where(x => !string.IsNullOrWhiteSpace(x) && !IsTransportadoraHeaderLine(x)));

            if (string.IsNullOrWhiteSpace(block))
            {
                return new List<TransportadoraPdfRow>();
            }

            Regex blockRegex = new Regex(
                @"(?<contato>\d{4,})\s+(?<nota>\d+(?:\s*-\s*\d+)?)\s+(?<dest>.+?)\s+(?<data>\d{1,2}[/-]\d{1,2}[/-]\d{4})(?:\s+(?<valor>R?\$?\s*[\d\.\,]+))?\s+(?<vol>\d+)(?=\s+\d{4,}\s+\d+(?:\s*-\s*\d+)?\s+.+?\s+\d{1,2}[/-]\d{1,2}[/-]\d{4}|\s*$)",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);

            List<TransportadoraPdfRow> rows = new List<TransportadoraPdfRow>();
            MatchCollection matches = blockRegex.Matches(block);
            foreach (Match match in matches)
            {
                int parsedVolume;
                if (!match.Success || !int.TryParse(match.Groups["vol"].Value, out parsedVolume))
                {
                    continue;
                }

                string contatoNr = NormalizeTransportadoraContato(match.Groups["contato"].Value);
                string notaFiscalNr = NormalizeTransportadoraNotaFiscal(match.Groups["nota"].Value);
                string destinatario = NormalizeTransportadoraDestinatario(match.Groups["dest"].Value);
                if (!IsValidTransportadoraPdfFields(contatoNr, notaFiscalNr, destinatario))
                {
                    continue;
                }

                rows.Add(new TransportadoraPdfRow
                {
                    ContatoNr = contatoNr,
                    NotaFiscalNr = notaFiscalNr,
                    Destinatario = destinatario,
                    Volumes = parsedVolume
                });
            }

            return rows;
        }

        private static bool TryParseTransportadoraPdfRow(IReadOnlyList<string> tokens, int startIndex, out TransportadoraPdfRow row, out int nextIndex)
        {
            row = null;
            nextIndex = startIndex + 1;

            if (startIndex + 4 >= tokens.Count)
            {
                return false;
            }

            string contato = tokens[startIndex];
            string notaFiscal;
            int destinatarioStartIndex;
            int dateIndex = FindNextTransportadoraDateIndex(tokens, startIndex + 2);
            if (dateIndex < 0)
            {
                return false;
            }

            if (!TryBuildTransportadoraNotaFiscal(tokens, startIndex + 1, dateIndex, out notaFiscal, out destinatarioStartIndex))
            {
                return false;
            }

            string destinatario = string.Join(" ", tokens.Skip(destinatarioStartIndex).Take(dateIndex - destinatarioStartIndex));
            string contatoNr = NormalizeTransportadoraContato(contato);
            string notaFiscalNr = NormalizeTransportadoraNotaFiscal(notaFiscal);
            string destinatarioNr = NormalizeTransportadoraDestinatario(destinatario);
            if (!IsValidTransportadoraPdfFields(contatoNr, notaFiscalNr, destinatarioNr))
            {
                return false;
            }

            int volumeIndex;
            string valor;
            int? volumes;
            if (!TryParseTransportadoraValueAndVolume(tokens, dateIndex + 1, out valor, out volumes, out volumeIndex))
            {
                return false;
            }

            if (!IsTransportadoraPdfRow(contato, destinatario, tokens[dateIndex], valor, volumes))
            {
                return false;
            }

            row = new TransportadoraPdfRow
            {
                ContatoNr = contatoNr,
                NotaFiscalNr = notaFiscalNr,
                Destinatario = destinatarioNr,
                Volumes = volumes
            };

            nextIndex = volumeIndex + 1;
            return true;
        }

        private static bool TryParseTransportadoraValueAndVolume(IReadOnlyList<string> tokens, int startIndex, out string valor, out int? volumes, out int volumeIndex)
        {
            valor = null;
            volumes = null;
            volumeIndex = -1;

            if (startIndex >= tokens.Count)
            {
                return false;
            }

            for (int i = startIndex; i < tokens.Count && i < startIndex + 4; i++)
            {
                int? currentVolume = ParseTransportadoraVolume(tokens[i]);
                if (!currentVolume.HasValue)
                {
                    continue;
                }

                string currentValor = string.Join(" ", tokens.Skip(startIndex).Take(i - startIndex));
                if (LooksLikeTransportadoraCurrency(currentValor))
                {
                    valor = currentValor;
                    volumes = currentVolume;
                    volumeIndex = i;
                    return true;
                }
            }

            for (int i = startIndex; i < tokens.Count && i < startIndex + 4; i++)
            {
                int? currentVolume = ParseTransportadoraVolume(tokens[i]);
                if (!currentVolume.HasValue)
                {
                    continue;
                }

                valor = null;
                volumes = currentVolume;
                volumeIndex = i;
                return true;
            }

            return false;
        }

        private static bool TryBuildTransportadoraNotaFiscal(
            IReadOnlyList<string> tokens,
            int notaStartIndex,
            int dateIndex,
            out string notaFiscal,
            out int destinatarioStartIndex)
        {
            notaFiscal = null;
            destinatarioStartIndex = notaStartIndex + 1;

            if (notaStartIndex >= tokens.Count || notaStartIndex >= dateIndex)
            {
                return false;
            }

            notaFiscal = NormalizeTransportadoraWhitespace(tokens[notaStartIndex]);
            if (string.IsNullOrWhiteSpace(notaFiscal))
            {
                return false;
            }

            if (destinatarioStartIndex + 1 < dateIndex &&
                string.Equals(NormalizeTransportadoraWhitespace(tokens[destinatarioStartIndex]), "-", StringComparison.Ordinal))
            {
                string serie = NormalizeTransportadoraWhitespace(tokens[destinatarioStartIndex + 1]);
                if (!string.IsNullOrWhiteSpace(serie) && serie.All(char.IsDigit))
                {
                    notaFiscal = string.Concat(notaFiscal, "-", serie);
                    destinatarioStartIndex += 2;
                }
            }

            return destinatarioStartIndex < dateIndex;
        }

        private static bool IsTransportadoraPdfRow(string contato, string destinatario, string dataEmissao, string valor, int? volumes)
        {
            if (string.IsNullOrWhiteSpace(contato) || string.IsNullOrWhiteSpace(destinatario) || !volumes.HasValue)
            {
                return false;
            }

            if (!IsTransportadoraDate(dataEmissao))
            {
                return false;
            }

            return string.IsNullOrWhiteSpace(valor) || LooksLikeTransportadoraCurrency(valor);
        }

        private static bool IsValidTransportadoraPdfFields(string contatoNr, string notaFiscalNr, string destinatario)
        {
            return !string.IsNullOrWhiteSpace(contatoNr) &&
                   !string.IsNullOrWhiteSpace(notaFiscalNr) &&
                   !string.IsNullOrWhiteSpace(destinatario);
        }

        private static int FindNextTransportadoraDateIndex(IReadOnlyList<string> tokens, int startIndex)
        {
            for (int i = startIndex; i < tokens.Count; i++)
            {
                if (IsTransportadoraDate(tokens[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        private static bool IsTransportadoraDate(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            DateTime parsedDate;
            string[] formats = { "dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy", "d-M-yyyy" };
            return DateTime.TryParseExact(value.Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDate) ||
                   DateTime.TryParse(value.Trim(), new CultureInfo("pt-BR"), DateTimeStyles.None, out parsedDate);
        }

        private static bool LooksLikeTransportadoraCurrency(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            string normalized = value
                .Replace("R$", string.Empty)
                .Replace("r$", string.Empty)
                .Replace("$", string.Empty)
                .Trim();

            decimal parsed;
            return decimal.TryParse(normalized, out parsed) ||
                   decimal.TryParse(normalized, NumberStyles.Number, new CultureInfo("pt-BR"), out parsed);
        }

        private static int FindTransportadoraHeaderStartIndex(IReadOnlyList<string> tokens)
        {
            for (int i = 0; i < tokens.Count; i++)
            {
                if (IsTransportadoraHeaderLine(tokens[i]))
                {
                    return i + 1;
                }

                int index = i;
                if (!ConsumeTransportadoraHeader(tokens, ref index, "contato"))
                {
                    continue;
                }

                if (!ConsumeTransportadoraHeader(tokens, ref index, "notafiscal"))
                {
                    continue;
                }

                if (!ConsumeTransportadoraHeader(tokens, ref index, "destinatario"))
                {
                    continue;
                }

                if (!ConsumeTransportadoraHeader(tokens, ref index, "dataemissao"))
                {
                    continue;
                }

                if (!ConsumeTransportadoraHeader(tokens, ref index, "valor"))
                {
                    continue;
                }

                if (ConsumeTransportadoraHeader(tokens, ref index, "qtvol"))
                {
                    return index;
                }
            }

            return -1;
        }

        private static bool IsTransportadoraHeaderLine(string value)
        {
            string normalized = NormalizeTransportadoraToken(value);
            return normalized.Contains("CONTATO") &&
                   normalized.Contains("NOTAFISCAL") &&
                   normalized.Contains("DESTINATARIO") &&
                   normalized.Contains("DATAEMISSAO") &&
                   normalized.Contains("VALOR") &&
                   normalized.Contains("QTVOL");
        }

        private static bool ConsumeTransportadoraHeader(IReadOnlyList<string> tokens, ref int index, string expected)
        {
            if (index >= tokens.Count)
            {
                return false;
            }

            string current = NormalizeTransportadoraToken(tokens[index]);
            if (current == expected)
            {
                index++;
                return true;
            }

            if (index + 1 < tokens.Count)
            {
                string combined = NormalizeTransportadoraToken(tokens[index] + tokens[index + 1]);
                if (combined == expected)
                {
                    index += 2;
                    return true;
                }
            }

            return false;
        }

        private static int? ParseTransportadoraVolume(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            string digits = new string(value.Where(char.IsDigit).ToArray());
            if (string.IsNullOrWhiteSpace(digits))
            {
                return null;
            }

            int parsed;
            return int.TryParse(digits, out parsed) ? (int?)parsed : null;
        }

        private static string NormalizeTransportadoraContato(string value)
        {
            string contato = NormalizeTransportadoraWhitespace(value);
            if (string.IsNullOrWhiteSpace(contato))
            {
                return null;
            }

            string digits = new string(contato.Where(char.IsDigit).ToArray());
            return digits.Length >= 4 ? digits : null;
        }

        private static string NormalizeTransportadoraNotaFiscal(string value)
        {
            string notaFiscalNr = NormalizeNotaFiscalNr(value);
            return string.IsNullOrWhiteSpace(notaFiscalNr) ? null : notaFiscalNr;
        }

        private static string NormalizeTransportadoraDestinatario(string value)
        {
            string destinatario = NormalizeTransportadoraWhitespace(value);
            if (string.IsNullOrWhiteSpace(destinatario))
            {
                return null;
            }

            destinatario = Regex.Replace(destinatario, @"^\-\s*\d+\s*", string.Empty);
            destinatario = Regex.Replace(destinatario, @"^[\-\.:;/\\\s]+", string.Empty);
            destinatario = NormalizeTransportadoraWhitespace(destinatario);

            return string.IsNullOrWhiteSpace(destinatario) ? null : destinatario;
        }

        private static string NormalizeNotaFiscalNr(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            string numero = value.Trim();
            if (numero.Length == 44 && numero.All(char.IsDigit))
            {
                numero = numero.Substring(25, 9);
            }
            else
            {
                int hyphenIndex = numero.IndexOf('-');
                if (hyphenIndex >= 0)
                {
                    numero = numero.Substring(0, hyphenIndex);
                }

                string digits = new string(numero.Where(char.IsDigit).ToArray());
                if (!string.IsNullOrWhiteSpace(digits))
                {
                    numero = digits;
                }
            }

            if (numero.All(char.IsDigit) && numero.Length > 9)
            {
                numero = numero.Substring(numero.Length - 9, 9);
            }

            long numericValue;
            if (long.TryParse(numero, out numericValue) && numero.Length <= 9)
            {
                numero = numero.PadLeft(9, '0');
            }

            return numero;
        }

        private static string BuildTransportadoraVolumeNr(string notaFiscalNr, int volume)
        {
            string numeroNormalizado = NormalizeNotaFiscalVolumeBase(notaFiscalNr);
            if (string.IsNullOrWhiteSpace(numeroNormalizado))
            {
                return null;
            }

            return string.Concat(numeroNormalizado, volume.ToString("000"));
        }

        private static string BuildApolloDocExpedicaoMergeSql()
        {
            const string sql = @"
MERGE [dbo].[DocExpedicao] AS Destino
USING (
    SELECT
        LTRIM(RTRIM(NUMERO_NOTA_FISCAL)) AS NUMERO_NOTA_FISCAL,
        CAST(SUBSTRING(DTA_ENTRADA_SAIDA, 7, 4) + '-' + SUBSTRING(DTA_ENTRADA_SAIDA, 4, 2) + '-' + SUBSTRING(DTA_ENTRADA_SAIDA, 1, 2) AS DATE) AS [Data],
        NOME_DEPARTAMENTO,
        CONTATO,
        NOME_VENDEDOR,
        CLIENTE,
        NOME_CLIENTE,
        CIDADE,
        ESTADO,
        1 AS StatusId,
        0 AS RoteiroImpresso,
        '_usuario_' AS CriadoPor,
        @Agora AS CriadoEm,
        @filial AS FilialId
    FROM DocExpedicaoUpload_APOLLO
) AS Origem
ON (
    RIGHT(CONCAT('000000000', Origem.NUMERO_NOTA_FISCAL), 9) = RIGHT(CONCAT('000000000', Destino.Numero), 9)
    AND Origem.[Data] = Destino.[DataEmissao]
    AND (Destino.FilialId = Origem.FilialId OR Destino.FilialId IS NULL)
)
WHEN MATCHED THEN
    UPDATE SET
        Numero = RIGHT(CONCAT('000000000', Origem.NUMERO_NOTA_FISCAL), 9),
        Classificacao = Origem.NOME_DEPARTAMENTO,
        Controle = Origem.CONTATO,
        Vendedor = Origem.NOME_VENDEDOR,
        CodigoCliente = Origem.CLIENTE,
        NomeCliente = Origem.NOME_CLIENTE,
        Cidade = Origem.CIDADE,
        Estado = Origem.ESTADO,
        FilialId = Origem.FilialId,
        ModificadoPor = '_usuario_',
        ModificadoEm = @Agora
WHEN NOT MATCHED THEN
    INSERT (
        [Numero], [DataEmissao], [Classificacao], [Controle], [Vendedor],
        [CodigoCliente], [NomeCliente], [Cidade], [Estado], [StatusId],
        [RoteiroImpresso], [CriadoPor], [CriadoEm], [FilialId]
    )
    VALUES (
        RIGHT(CONCAT('000000000', Origem.NUMERO_NOTA_FISCAL), 9), Origem.[Data], Origem.NOME_DEPARTAMENTO, Origem.CONTATO, Origem.NOME_VENDEDOR,
        Origem.CLIENTE, Origem.NOME_CLIENTE, Origem.CIDADE, Origem.ESTADO, 1,
        0, '_usuario_', @Agora, @filial
    );

DELETE DocExpedicao
FROM DocExpedicao
WHERE FilialId = @filial
  AND Classificacao LIKE '%MERCADO LIVRE%';";

            return Util.FormatSQL(sql);
        }

        private static string BuildDocExpedicaoImportHistorySql()
        {
            const string sql = @"
INSERT INTO dbo.HistoricoDocExpedicao
(
    DocExpedicaoId,
    HistoricoId,
    Observacoes,
    DataHora,
    Usuario,
    FilialId
)
SELECT
    Documento.Id,
    1,
    NULL,
    @Agora,
    '_usuario_',
    @filial
FROM dbo.DocExpedicao Documento
WHERE Documento.FilialId = @filial
  AND NOT EXISTS
  (
      SELECT 1
      FROM dbo.HistoricoDocExpedicao Historico
      WHERE Historico.DocExpedicaoId = Documento.Id
        AND Historico.HistoricoId = 1
        AND Historico.FilialId = @filial
  );";

            return Util.FormatSQL(sql);
        }

        private static string NormalizeNotaFiscalVolumeBase(string notaFiscalNr)
        {
            if (string.IsNullOrWhiteSpace(notaFiscalNr))
            {
                return null;
            }

            string numero = notaFiscalNr.Trim();
            int hyphenIndex = numero.IndexOf('-');
            if (hyphenIndex >= 0)
            {
                numero = numero.Substring(0, hyphenIndex);
            }

            string digits = new string(numero.Where(char.IsDigit).ToArray());
            if (string.IsNullOrWhiteSpace(digits))
            {
                return null;
            }

            if (digits.Length > 9)
            {
                digits = digits.Substring(digits.Length - 9, 9);
            }

            return digits.PadLeft(9, '0');
        }

        private static string BuildCode128Svg(string value)
        {
            string texto = value ?? string.Empty;
            List<int> codigos = new List<int> { 104 }; // Code 128 conjunto B
            foreach (char caractere in texto)
            {
                char imprimivel = caractere >= 32 && caractere <= 126 ? caractere : '?';
                codigos.Add(imprimivel - 32);
            }

            int checksum = 104;
            for (int indice = 1; indice < codigos.Count; indice++)
            {
                checksum += codigos[indice] * indice;
            }

            codigos.Add(checksum % 103);
            codigos.Add(106);

            int totalModulos = 20;
            foreach (int codigo in codigos)
            {
                totalModulos += Code128Patterns[codigo].Sum(x => x - '0');
            }

            StringBuilder svg = new StringBuilder();
            svg.AppendFormat(
                CultureInfo.InvariantCulture,
                "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 {0} 60' preserveAspectRatio='none' role='img' aria-label='Código de barras'>",
                totalModulos);
            svg.Append("<rect width='100%' height='60' fill='white'/>");

            int posicaoX = 10;
            foreach (int codigo in codigos)
            {
                string padrao = Code128Patterns[codigo];
                for (int indice = 0; indice < padrao.Length; indice++)
                {
                    int largura = padrao[indice] - '0';
                    if (indice % 2 == 0)
                    {
                        svg.AppendFormat(
                            CultureInfo.InvariantCulture,
                            "<rect x='{0}' y='0' width='{1}' height='60' fill='black'/>",
                            posicaoX,
                            largura);
                    }

                    posicaoX += largura;
                }
            }

            svg.Append("</svg>");
            return svg.ToString();
        }

        private static string NormalizeTransportadoraWhitespace(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return string.Join(" ", value
                .Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                .Trim();
        }

        private static string NormalizeTransportadoraToken(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            StringBuilder normalized = new StringBuilder();
            foreach (char c in Util.RemoverAcentuacao(value).ToUpperInvariant())
            {
                if (char.IsLetterOrDigit(c))
                {
                    normalized.Append(c);
                }
            }

            return normalized.ToString();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private sealed class NotaFiscalTransportadoraImportRow
        {
            public string Transportadora { get; set; }
            public string NotaFiscalNr { get; set; }
            public int? Volumes { get; set; }
            public string Cliente { get; set; }
            public string ContatoNr { get; set; }
            public string VolumeNr { get; set; }
            public int? DocExpedicaoId { get; set; }
            public string Sequencia { get; set; }
            public string ZPL { get; set; }
        }

        private sealed class DocExpedicaoTransportadoraLink
        {
            public int Id { get; set; }
            public string NotaFiscalNr { get; set; }
            public string Cliente { get; set; }
            public string ContatoNr { get; set; }
        }

        private sealed class DocExpedicaoTransportadoraUpdate
        {
            public string NotaFiscalNr { get; set; }
            public string ContatoNr { get; set; }
            public string TransportadoraNome { get; set; }
            public int? QtdVolumes { get; set; }
        }

        private sealed class TransportadoraPrintPayload
        {
            public string Zpl { get; set; }
            public int CountImpressao { get; set; }
        }

        private sealed class TransportadoraDirectPrinterSettings
        {
            public string PrinterName { get; set; }
            public string PrinterTarget { get; set; }
            public string PrinterPort { get; set; }
        }

        private sealed class TransportadoraImportParseResult
        {
            public List<NotaFiscalTransportadoraImportRow> Items { get; set; }
            public TransportadoraImportDiagnostics Diagnostics { get; set; }
        }

        private sealed class TransportadoraDocExpedicaoResolutionResult
        {
            public List<DocExpedicaoTransportadoraLink> DocumentosNovos { get; set; }
            public int NotasDistintasPdf { get; set; }
            public int NotasJaExistentesDocExpedicao { get; set; }

            public TransportadoraDocExpedicaoResolutionResult()
            {
                DocumentosNovos = new List<DocExpedicaoTransportadoraLink>();
            }
        }

        private sealed class TransportadoraImportDiagnostics
        {
            public int PaginasLidas { get; set; }
            public int LinhasPdf { get; set; }
            public int NotasDistintasPdf { get; set; }
            public int NotasJaExistentesDocExpedicao { get; set; }
            public int NotasNovasDocExpedicao { get; set; }
            public int LinhasIgnoradasSemDocumentoNovo { get; set; }
            public int LinhasIgnoradasSemVolume { get; set; }
            public int LinhasIgnoradasClienteSemEtiqueta { get; set; }
            public int RegistrosDuplicadosIgnorados { get; set; }
            public int RegistrosEtiquetaGerados { get; set; }
            public int VolumesGerados { get; set; }
        }

        private sealed class TransportadoraPdfRow
        {
            public string ContatoNr { get; set; }
            public string NotaFiscalNr { get; set; }
            public string Destinatario { get; set; }
            public int? Volumes { get; set; }
            public string VolumeNr { get; set; }
        }

        //////////////////////////////////////////////////////////////////////////////
        /// MÉTODO TEMPORÁRIO PARA IMPRIMIR OS QUE NÃO FOREM IMPRESSOS CORRETAMENTE //
        //////////////////////////////////////////////////////////////////////////////
        [HttpGet]
        public ActionResult GetDataToPrintTemporary(int ids)
        {
            List<string> listaEtiquetas = new List<string>();
            string template_zpl = (from e in db.Etiqueta
                                   where e.Nome == "Expedicao" && e.FilialId == filialId
                                   select e.ZPL).FirstOrDefault();
            int qtd_volumes = 1;
            string zpl, zpl2, zpl3;

            var notas = (from nf in db.DocExpedicao
                         where nf.FilialId == filialId && ids == nf.Id
                         select nf).ToList();

            if (notas.Count == 0)
            {
                return Json(new { etiquetas = listaEtiquetas, success = true, msg = "Nenhuma NF localizada!" }, JsonRequestBehavior.AllowGet);
            }

            try
            {
                foreach (var notafiscal in notas)
                {
                    DocExpedicao doc = db.DocExpedicao
                        .FirstOrDefault(x => x.Id == notafiscal.Id && x.FilialId == filialId);
                    if (doc != null)
                    {

                        // Transportadora
                        var transp = (from t in db.Transportadora
                                      where t.Id == doc.TransportadoraId && t.FilialId == filialId
                                      select t).FirstOrDefault();


                        doc.ModificadoEm = Util.GetCurrentDateTime();
                        doc.ModificadoPor = Util.GetCurrentUser();

                        // Gerar etiqueta (ZPL) para cada volume
                        if (transp != null && transp.EmitirEtiqueta)
                        {
                            DateTime dt = Util.GetCurrentDateTime();

                            zpl = template_zpl;
                            zpl = zpl.Replace("local-origem", "Sorocaba");
                            zpl = zpl.Replace("data-impressao", dt.ToString("dd/MM/yyyy"));
                            zpl = zpl.Replace("hora-impressao", dt.ToString("HH:mm:ss"));
                            zpl = zpl.Replace("nome-transportadora", transp.Nome_Fantasia ?? string.Empty);

                            // Remover zeros à esquerda do número da NF
                            char[] zero = { '0' };
                            string nf_aux = doc.Numero ?? string.Empty;
                            zpl = zpl.Replace("nfiscal-nr", nf_aux.TrimStart(zero));

                            zpl = zpl.Replace("contato-nr", doc.Controle ?? string.Empty);

                            // Dados do cliente
                            var cliente = (from c in db.Cliente
                                           where c.CodigoDMS == doc.CodigoCliente
                                           select c).FirstOrDefault();

                            if (cliente != null)
                            {
                                string cidadeEstado = cliente.Endereco_Cidade + "/" + cliente.Endereco_UF;
                                zpl = zpl.Replace("ruanr-cliente", cliente.Endereco_Logradouro ?? string.Empty);
                                zpl = zpl.Replace("bairro-cliente", cliente.Endereco_Bairro ?? string.Empty);
                                //zpl = zpl.Replace("cidadeestado-cliente", cliente.Endereco_Cidade ?? string.Empty);
                                zpl = zpl.Replace("cidadeestado-cliente", cidadeEstado ?? string.Empty);

                                string aux_cliente = cliente.Nome ?? string.Empty;
                                if (aux_cliente.Length > 14)
                                {
                                    aux_cliente = aux_cliente.Substring(0, 14);
                                }
                                zpl = zpl.Replace("nome-cliente", aux_cliente);
                            }

                            // Nome da rota
                            string rota = (from r in db.Rota
                                           where r.Id == doc.RotaId
                                           select r.Nome).FirstOrDefault() ?? string.Empty;
                            zpl = zpl.Replace("rota-cliente", rota ?? string.Empty);


                            // Nome da Parada
                            string parada = (from p in db.Parada
                                             where p.Id == doc.ParadaId
                                             select p.Nome).FirstOrDefault() ?? string.Empty;
                            zpl = zpl.Replace("parada-cliente", parada ?? string.Empty);

                            qtd_volumes = doc.QtdVolumes ?? 1;
                            for (int i = 1; i <= qtd_volumes; i++)
                            {
                                zpl2 = zpl.Replace("controlenr-quasar", string.Concat(doc.Numero, i.ToString().PadLeft(3, '0')));
                                zpl3 = zpl2.Replace("sequencia-volume", string.Concat(i.ToString(), "/", qtd_volumes.ToString()));
                                listaEtiquetas.Add(Util.RemoverAcentuacao(zpl3));
                            }
                        }
                    }
                }

                JsonResult result = Json(new { etiquetas = listaEtiquetas, success = true, msg = "Operação realizada com sucesso" }, JsonRequestBehavior.AllowGet);
                result.MaxJsonLength = int.MaxValue;
                return result;
            }
            catch (Exception ex)
            {

                JsonResult result = Json(new { etiquetas = listaEtiquetas, success = false, msg = ex.Message }, JsonRequestBehavior.AllowGet);
                result.MaxJsonLength = int.MaxValue;
                return result;
            }
        }

        [HttpPost]
        public ActionResult LogPrintZpl(string zpl)
        {
            /* Histórico de impressão temporariamente desativado.
            if (zpl.Length > 0)
            {
                using (DbContextTransaction tr = db.Database.BeginTransaction())
                {
                    try
                    {
                        LogImpressaoExpedicao logImpressao = new LogImpressaoExpedicao();
                        logImpressao.Zpl = zpl;
                        logImpressao.ImpressoEm = Util.GetCurrentDateTime();
                        logImpressao.Usuario = current_user;
                        logImpressao.FilialId = filialId;
                        db.LogImpressaoExpedicao.Add(logImpressao);
                        db.SaveChanges();
                        tr.Commit();

                    }
                    catch (Exception ex)
                    {
                        tr.Rollback();
                        JsonResult resultError = Json(new { success = false, msg = ex.Message });
                        return resultError;
                    }

                    JsonResult result = Json(new { success = true, msg = "Log criado!" });
                    return result;
                }
            }
            else
            {
                JsonResult resultNoZpl = Json(new { success = false, msg = "Zpl nula!" });
                return resultNoZpl;
            }
            */

            return Json(new { success = true, msg = "Histórico de impressão desativado." });
        }


        //método para imprimir etiquetas que deram problema, especificando os volumes
        [HttpGet]
        public ActionResult GetDataToPrintVolume(string key, int minVolume, int maxVolume, bool somenteValidar = false)
        {
            if (minVolume <= 0)
            {
                return Json(new { success = false, msg = "Volume mínimo precisa ser maior que 0!" }, JsonRequestBehavior.AllowGet);
            }
            if (maxVolume <= 0)
            {
                return Json(new { success = false, msg = "Volume máximo precisa ser maior que 0!" }, JsonRequestBehavior.AllowGet);
            }
            if (maxVolume < minVolume)
            {
                return Json(new { success = false, msg = "Volume máximo precisa ser maior ou igual ao volume mínimo!" }, JsonRequestBehavior.AllowGet);
            }
            string numeroNF = key.Trim();
            if (key.Length == 44)
            {
                numeroNF = key.Trim().Substring(25, 9);
            }
            else
            {
                numeroNF = numeroNF.PadLeft(9, '0');
            }
            List<string> listaEtiquetas = new List<string>();
            string template_zpl = (from e in db.Etiqueta
                                   where e.Nome == "Expedicao"
                                   select e.ZPL).FirstOrDefault();
            int qtd_volumes = 1;
            string zpl, zpl2, zpl3;

            var notas = (from nf in db.DocExpedicao
                         where nf.FilialId == filialId && numeroNF == nf.Numero
                         select nf).ToList();

            if (notas.Count == 0)
            {
                return Json(new { etiquetas = listaEtiquetas, success = false, msg = "Nenhuma NF localizada!" }, JsonRequestBehavior.AllowGet);
            }

            try
            {
                foreach (var notafiscal in notas)
                {

                    if (maxVolume > notafiscal.QtdVolumes)
                    {
                        return Json(new { etiquetas = listaEtiquetas, success = false, msg = "A quantidade de volumes máxima informada é maior que a quantidade cadastrada no sistema!" }, JsonRequestBehavior.AllowGet);
                    }
                    if (notafiscal.StatusId == 1 || notafiscal.StatusId == 1002)
                    {
                        return Json(new { etiquetas = listaEtiquetas, success = false, msg = "A nota fiscal precisa estar 'Em trânsito', 'Finalizada' ou 'Aguardando Roteiro' para ser impressa!" }, JsonRequestBehavior.AllowGet);
                    }
                    DocExpedicao doc = db.DocExpedicao
                        .FirstOrDefault(x => x.Id == notafiscal.Id && x.FilialId == filialId);
                    if (doc != null)
                    {

                        // Transportadora
                        var transp = (from t in db.Transportadora
                                      where t.Id == doc.TransportadoraId && t.FilialId == filialId
                                      select t).FirstOrDefault();

                        Cliente cliente = ResolveClienteEtiqueta(doc);
                        if (!ClientePermiteGerarEtiqueta(cliente))
                        {
                            return Json(new
                            {
                                etiquetas = listaEtiquetas,
                                success = false,
                                msg = "O cliente desta nota fiscal está configurado para não gerar etiqueta."
                            }, JsonRequestBehavior.AllowGet);
                        }

                        doc.ModificadoEm = Util.GetCurrentDateTime();
                        doc.ModificadoPor = Util.GetCurrentUser();

                        // Gerar etiqueta (ZPL) para cada volume
                        if (transp == null || !transp.EmitirEtiqueta)
                        {
                            return Json(new { etiquetas = listaEtiquetas, success = false, msg = "A transportadora cadastrada na nota fiscal não emite etiqueta!" }, JsonRequestBehavior.AllowGet);
                        }
                        if (somenteValidar)
                        {
                            continue;
                        }

                        if (transp != null && transp.EmitirEtiqueta)
                        {
                            DateTime dt = Util.GetCurrentDateTime();

                            zpl = template_zpl;
                            zpl = zpl.Replace("local-origem", "Sorocaba");
                            zpl = zpl.Replace("data-impressao", dt.ToString("dd/MM/yyyy"));
                            zpl = zpl.Replace("hora-impressao", dt.ToString("HH:mm:ss"));
                            zpl = zpl.Replace("nome-transportadora", transp.Nome_Fantasia ?? string.Empty);

                            // Remover zeros à esquerda do número da NF
                            char[] zero = { '0' };
                            string nf_aux = doc.Numero ?? string.Empty;
                            zpl = zpl.Replace("nfiscal-nr", nf_aux.TrimStart(zero));

                            zpl = zpl.Replace("contato-nr", doc.Controle ?? string.Empty);

                            // Dados do cliente
                            if (cliente != null)
                            {
                                string cidadeEstado = cliente.Endereco_Cidade + "/" + cliente.Endereco_UF;
                                zpl = zpl.Replace("ruanr-cliente", cliente.Endereco_Logradouro ?? string.Empty);
                                zpl = zpl.Replace("bairro-cliente", cliente.Endereco_Bairro ?? string.Empty);
                                //zpl = zpl.Replace("cidadeestado-cliente", cliente.Endereco_Cidade ?? string.Empty);
                                zpl = zpl.Replace("cidadeestado-cliente", cidadeEstado ?? string.Empty);

                                string aux_cliente = cliente.Nome ?? string.Empty;
                                if (aux_cliente.Length > 14)
                                {
                                    aux_cliente = aux_cliente.Substring(0, 14);
                                }
                                zpl = zpl.Replace("nome-cliente", aux_cliente);
                            }

                            // Nome da rota
                            string rota = (from r in db.Rota
                                           where r.Id == doc.RotaId
                                           select r.Nome).FirstOrDefault() ?? string.Empty;
                            zpl = zpl.Replace("rota-cliente", rota ?? string.Empty);


                            // Nome da Parada
                            string parada = (from p in db.Parada
                                             where p.Id == doc.ParadaId
                                             select p.Nome).FirstOrDefault() ?? string.Empty;
                            zpl = zpl.Replace("parada-cliente", parada ?? string.Empty);

                            qtd_volumes = doc.QtdVolumes ?? 1;
                            for (int i = minVolume; i <= maxVolume; i++)
                            {
                                zpl2 = zpl.Replace("controlenr-quasar", string.Concat(doc.Numero, i.ToString().PadLeft(3, '0')));
                                zpl3 = zpl2.Replace("sequencia-volume", string.Concat(i.ToString(), "/", qtd_volumes.ToString()));
                                listaEtiquetas.Add(Util.RemoverAcentuacao(zpl3));
                            }
                        }
                    }
                }

                JsonResult result = Json(new { etiquetas = listaEtiquetas, success = true, msg = "Operação realizada com sucesso" }, JsonRequestBehavior.AllowGet);
                result.MaxJsonLength = int.MaxValue;
                return result;
            }
            catch (Exception ex)
            {

                JsonResult result = Json(new { etiquetas = listaEtiquetas, success = false, msg = ex.Message }, JsonRequestBehavior.AllowGet);
                result.MaxJsonLength = int.MaxValue;
                return result;
            }

        }

        //método para pegar a contagem de volumes a serem impressos
        [HttpPost]
        public ActionResult GetPrintCount(int[] ids)
        {
            if (ids == null)
            {
                return Json(new { success = false, msg = "Nenhuma NF foi selecionada!" }, JsonRequestBehavior.AllowGet);
            }

            var notas = (from nf in db.DocExpedicao
                         where nf.FilialId == filialId && ids.Contains(nf.Id)
                         select nf).ToList();


            int countImpressao = 0;

            if (notas.Count == 0)
            {
                return Json(new { success = false, msg = "Nenhuma NF localizada!" }, JsonRequestBehavior.AllowGet);
            }

            try
            {
                foreach (var notaFiscal in notas)
                {
                    DocExpedicao doc = db.DocExpedicao
                        .FirstOrDefault(x => x.Id == notaFiscal.Id && x.FilialId == filialId);
                    if (doc != null)
                    {
                        // Transportadora
                        var transp = (from t in db.Transportadora
                                      where t.Id == doc.TransportadoraId && t.FilialId == filialId
                                      select t).FirstOrDefault();

                        if (transp != null && transp.EmitirEtiqueta)
                        {
                            int volume = doc.QtdVolumes ?? 0;
                            countImpressao += volume;
                        }
                    }
                }
                JsonResult result = Json(new { countImpressao = countImpressao, success = true, msg = "Operação realizada com sucesso" }, JsonRequestBehavior.AllowGet);
                return result;

            }
            catch (Exception ex)
            {
                JsonResult result = Json(new { success = false, msg = ex.Message }, JsonRequestBehavior.AllowGet);
                return result;
            }
        }

        //método para pegar informações da nota fiscal para ser exibida na página "Print"
        [HttpGet]
        public ActionResult GetDanfeInfo(string key)
        {
            if (key == null)
            {
                JsonResult result = Json(new { success = false, msg = "Nota fiscal não encontrada!" }, JsonRequestBehavior.AllowGet);
                return result;
            }
            string numeroNF = key.Trim();
            if (key.Length == 44)
            {
                numeroNF = key.Trim().Substring(25, 9);
            }
            else
            {
                numeroNF = numeroNF.PadLeft(9, '0');
            }

            try
            {
                var notas = (from nf in db.DocExpedicao
                             where nf.FilialId == filialId && numeroNF == nf.Numero
                             select nf).FirstOrDefault();

                if (notas == null)
                {
                    JsonResult resultError = Json(new { success = false, msg = "Nota fiscal não encontrada!" }, JsonRequestBehavior.AllowGet);
                    return resultError;
                }
                if (notas.StatusId == 1 || notas.StatusId == 1002)
                {
                    JsonResult resultError = Json(new { success = false, msg = "A nota fiscal precisa estar 'Em trânsito', 'Finalizada' ou 'Aguardando Roteiro' para ser impressa!" }, JsonRequestBehavior.AllowGet);
                    return resultError;
                }

                // Transportadora
                var transp = (from t in db.Transportadora
                              where t.Id == notas.TransportadoraId && t.FilialId == filialId
                              select t).FirstOrDefault();


                JsonResult result = Json(new { notaFiscal = notas, transportadora = transp, success = true, msg = "Sucesso!" }, JsonRequestBehavior.AllowGet);
                return result;
            }
            catch (Exception ex)
            {
                JsonResult result = Json(new { success = false, msg = ex.Message }, JsonRequestBehavior.AllowGet);
                return result;
            }

        }

        //método para deletar notas fiscais que foram importadas erradas do arquivo

        [HttpPost]
        public ActionResult DeleteDanfe(int id)
        {
            DocExpedicao notaFiscal = db.DocExpedicao
                .FirstOrDefault(x => x.Id == id && x.FilialId == filialId);

            if (notaFiscal == null)
            {
                return Json(new { success = false, msg = "NotaFiscal não encontrada!" });
            }

            using (DbContextTransaction tr = db.Database.BeginTransaction())
            {
                try
                {
                    db.DocExpedicao.Remove(notaFiscal);
                    db.SaveChanges();
                    tr.Commit();
                }

                catch (DbEntityValidationException ex)
                {
                    string msgErro = "";
                    foreach (DbEntityValidationResult item in ex.EntityValidationErrors)
                    {
                        DbEntityEntry entry = item.Entry;
                        string entityTypeName = entry.Entity.GetType().Name;
                        foreach (DbValidationError subItem in item.ValidationErrors)
                        {
                            msgErro += string.Format("{0}", subItem.ErrorMessage + Environment.NewLine);
                        }
                    }
                    tr.Rollback();
                    return Json(new { success = false, msg = msgErro });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, msg = ex.Message });
                }
            }

            return Json(new { success = true, msg = "Nota Fiscal Deletada com sucesso!" });
        }

        [HttpGet]
        public ActionResult GetUser()
        {
            string currentUser;
            try
            {
                currentUser = Util.GetCurrentUser();
            }
            catch (Exception ex)
            {

                return Json(new { success = false, msg = ex.Message });
            }

            JsonResult result = Json(new { user = currentUser, success = true, msg = "Requisição completa com sucesso!" }, JsonRequestBehavior.AllowGet);
            return result;

        }

        public ActionResult LogZplGenerated(string zpl, string tipo)
        {
            /* Histórico de geração de ZPL temporariamente desativado.
            if (zpl.Length > 0)
            {
                using (DbContextTransaction tr = db.Database.BeginTransaction())
                {
                    try
                    {
                        LogZplGeneratedExpedicao logZplGenerated = new LogZplGeneratedExpedicao();
                        logZplGenerated.ZPL = zpl;
                        logZplGenerated.GeradoEm = Util.GetCurrentDateTime();
                        logZplGenerated.Usuario = current_user;
                        logZplGenerated.Tipo = tipo;
                        logZplGenerated.FilialId = filialId;
                        db.LogZplGeneratedExpedicao.Add(logZplGenerated);
                        db.SaveChanges();
                        tr.Commit();

                    }
                    catch (Exception ex)
                    {
                        tr.Rollback();
                        JsonResult resultError = Json(new { success = false, msg = ex.Message });
                        return resultError;
                    }

                    JsonResult result = Json(new { success = true, msg = "Log criado!" });
                    return result;
                }
            }
            else
            {
                JsonResult resultNoZpl = Json(new { success = false, msg = "Zpl nula!" });
                return resultNoZpl;
            }
            */

            return Json(new { success = true, msg = "Histórico de geração de ZPL desativado." });
        }

    }
}
