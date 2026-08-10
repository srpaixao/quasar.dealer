using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Validation;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
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
        Quasar_Entities db = new Quasar_Entities();
        string current_user = Util.GetCurrentUser();

        int filialId = Util.GetCurrentFilial();

        int periodo;
        DateTime inicio;
        public NotaFiscalController()
        {
            periodo = Util.GetPeriodoExpedicao();
            inicio = DateTime.Now.AddDays(-periodo);
        }

        // GET: ExpedicaoApp/NotaFiscal/Print
        public ActionResult Print()
        {
            NotaFiscalViewModel vm = new NotaFiscalViewModel();
            vm.PrinterServerIP = (from a in db.AppConfig where a.Nome == "PrinterServerIP" select a.Valor).FirstOrDefault();
            if (vm.PrinterServerIP == string.Empty || vm.PrinterServerIP == null)
            {
                vm.PrinterServerIP = "localhost";
            }

            vm.PrinterServerPort = (from a in db.AppConfig where a.Nome == "PrinterServerPort" select a.Valor).FirstOrDefault();
            if (vm.PrinterServerPort == string.Empty || vm.PrinterServerPort == null)
            {
                vm.PrinterServerPort = "8080";
            }

            ViewBag.ImpressoraDDL = (from i in db.Impressora where i.FilialId == filialId
                                     select new SelectListItem
                                     {
                                         Value = i.Id.ToString(),
                                         Text = i.Nome,
                                         Selected = i.Localizacao == "EXP"
                                     }).ToList();

            return View(vm);
        }

        // GET: ExpedicaoApp/NotaFiscal
        public ActionResult Index()
        {
            NotaFiscalViewModel vm = new NotaFiscalViewModel();

            //DateTime inicio = Util.GetCurrentDateTime().AddDays(-periodo);

            vm.TotalLancamento = db.DocExpedicao.Where(x => x.StatusId == 1 && x.CriadoEm >= inicio).Count();
            vm.TotalAguardandoLancamento = db.DocExpedicao.Where(x => x.FilialId == filialId && x.StatusId == 1 && x.TipoMovimentoId == null && x.CriadoEm >= inicio).Count();
            vm.TotalEntrega = db.DocExpedicao.Where(x => x.FilialId == filialId && x.StatusId == 2 && x.TipoMovimentoId == 1 && x.CriadoEm >= inicio).Count();
            vm.TotalRetirada = db.DocExpedicao.Where(x => x.FilialId == filialId && x.StatusId == 2 && x.TipoMovimentoId == 2 && x.CriadoEm >= inicio).Count();
            vm.TotalGarantia = db.DocExpedicao.Where(x => x.FilialId == filialId && x.StatusId == 2 && x.TipoMovimentoId == 3 && x.CriadoEm >= inicio).Count(); ;
            vm.TotalTroca = db.DocExpedicao.Where(x => x.FilialId == filialId && x.StatusId == 2 && x.TipoMovimentoId == 4 && x.CriadoEm >= inicio).Count();
            vm.TotalRoteiro = db.DocExpedicao.Where(x => x.FilialId == filialId && x.StatusId == 3 && x.RoteiroImpresso == false && x.CriadoEm >= inicio).Count();
            vm.TotalFinalizado = db.DocExpedicao.Where(x => x.FilialId == filialId && x.StatusId == 4 && x.CriadoEm >= inicio).Count();
            vm.TotalEmTransito = db.DocExpedicao.Where(x => x.FilialId == filialId && x.StatusId == 2 && x.CriadoEm >= inicio).Count();
            vm.TotalEmEspera = db.DocExpedicao.Where(x => x.FilialId == filialId && x.StatusId == 1002 && x.CriadoEm >= inicio).Count();

            vm.ZPL_Etiqueta = (from e in db.Etiqueta where e.Nome == "Expedicao" select e.ZPL).FirstOrDefault();

            //if (vm.ZPL_Etiqueta == null)
            //{
            //    return HttpNotFound();
            //}

            //string local = (from a in db.AppConfig where a.Nome == "local" select a.Valor).FirstOrDefault();
            vm.PrinterServerIP = (from a in db.AppConfig where a.Nome == "PrinterServerIP" select a.Valor).FirstOrDefault();
            if (vm.PrinterServerIP == string.Empty || vm.PrinterServerIP == null)
            {
                vm.PrinterServerIP = "localhost";
            }

            vm.PrinterServerPort = (from a in db.AppConfig where a.Nome == "PrinterServerPort" select a.Valor).FirstOrDefault();
            if (vm.PrinterServerPort == string.Empty || vm.PrinterServerPort == null)
            {
                vm.PrinterServerPort = "8080";
            }

            ViewBag.ImpressoraDDL = (from i in db.Impressora where i.FilialId == filialId
                                     select new SelectListItem
                                     {
                                         Value = i.Id.ToString(),
                                         Text = i.Nome,
                                         Selected = i.Localizacao == "EXP"
                                     }).ToList();

            return View(vm);
        }

        [HttpGet]
        // GET: GetData
        public ActionResult GetData(int? movimento)
        {
            List<NotaFiscalViewModel> notas = new List<NotaFiscalViewModel>();

            movimento = movimento ?? 0;
            

            var count = (from nf in db.DocExpedicao 
                         where nf.CriadoEm >= inicio && nf.FilialId == filialId
                         select nf).Count();

            if (count > 0)
            {
                notas = (from nf in db.DocExpedicao.Where(x => x.CriadoEm >= inicio && x.FilialId == filialId).DefaultIfEmpty()
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
                             NomeCliente = cli.Nome,
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
                             NomeTransportadora = (from t in db.Transportadora where t.Id == nf.TransportadoraId select t.Nome_Fantasia).FirstOrDefault(), //Alteração 03.03.2024 para pegar o nome fantasia
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
                         }).ToList();

                // Aguardando lançamento
                if (movimento == 0)
                {
                    notas = notas.Where(x => x.TipoMovimentoId == null && x.CriadoEm >= inicio).ToList();
                }

                // Finalizado
                if (movimento == 5)
                {
                    notas = notas.Where(x => x.StatusId == 4 && x.CriadoEm >= inicio).ToList();
                }

                //Em Espera
                if (movimento == 6)
                {
                    notas = notas.Where(x => x.StatusId == 1002 && x.CriadoEm >= inicio).ToList();
                }

                if (movimento == 7)
                {
                    //Em Transito
                    notas = notas.Where(x => x.StatusId == 2 && x.CriadoEm >= inicio).ToList();
                }

                //Aguardando Roteiro ???
                if (movimento == 99)
                {
                    notas = notas.Where(x => x.StatusId == 3 && x.RoteiroImpresso == false && x.CriadoEm >= inicio).ToList();
                }
            }

            JsonResult result = Json(new { data = notas }, JsonRequestBehavior.AllowGet);
            result.MaxJsonLength = int.MaxValue;

            return result;
        }

        public ActionResult Edit(int id)
        {
            DocExpedicao documento = db.DocExpedicao.Find(id);
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
            vm.TransportadoraDDL = Util.GetTransportadoraDDL(documento.TransportadoraId);

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
                        sql = Util.FormatSQL(sql);

                        try
                        {
                            db.Database.ExecuteSqlCommand(sql);
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

                    // Gerar histórico
                    sql = (from s in db.AppSQL where s.Nome == "INSERT_Historico_DocExpedicao" select s.Comando).FirstOrDefault();
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
                            msg = "[Historico_DocExpedicao] INSERT failed<br>" + ex.Message;
                            return Json(new { erro = true, mensagem = msg }, JsonRequestBehavior.AllowGet);
                        }
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
            vm.TransportadoraDDL = Util.GetTransportadoraDDL(null);
            vm.TipoMovimentoDDL = Util.GetTipoMovimentoExpedicaoDDL(null);
            return View(vm);
        }

        [HttpPost]
        public ActionResult CancelarLancamento(int id)
        {
            var nota = db.DocExpedicao.Find(id);

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
                            var nota = db.DocExpedicao.Find(item.Id);
                            if (nota != null)
                            {
                                var transp = db.Transportadora.Find(item.TransportadoraId);
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
                                nota.Numero = item.Numero;
                                nota.DataEmissao = Util.GetCurrentDateTime();
                                nota.Classificacao = string.Empty;
                                nota.Controle = string.Empty;
                                nota.Vendedor = string.Empty;
                                nota.CodigoCliente = cliente.CodigoDMS;
                                nota.NomeCliente = cliente.Nome;
                                nota.Cidade = cliente.Endereco_Cidade;
                                nota.Estado = cliente.Endereco_UF;

                                var transp = db.Transportadora.Find(item.TransportadoraId);
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
            DocExpedicao notafiscal = db.DocExpedicao.Find(id);
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
                notafiscal = db.DocExpedicao.Where(x => x.Numero == numeroNF).FirstOrDefault();
                //if (notafiscal == null && key.Length == 44)
                if (notafiscal == null)
                {
                    numeroNF = numeroNF.TrimStart('0');
                    notafiscal = db.DocExpedicao.Where(x => x.Numero == numeroNF).FirstOrDefault();
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
                        JsonResult result = Json(new { data = notafiscal, success = true, msg = string.Empty }, JsonRequestBehavior.AllowGet);
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
                         where ids.Contains(nf.Id)
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
                        DocExpedicao doc = db.DocExpedicao.Find(notafiscal.Id);
                        if (doc != null)
                        {
                            doc.StatusId = 2; // Em trânsito

                            // Transportadora
                            var transp = (from t in db.Transportadora
                                          where t.Id == doc.TransportadoraId
                                          select t).FirstOrDefault();

                            if (transp != null)
                            {
                                if (transp.EmitirRoteiro)
                                {
                                    doc.StatusId = 3;  // Aguardando Roteiro 
                                }

                                if (transp.Finalizar)
                                {
                                    doc.StatusId = 4;  // Finalizado 
                                }
                            }

                            doc.ModificadoEm = Util.GetCurrentDateTime();
                            doc.ModificadoPor = Util.GetCurrentUser();
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

            var roteiro = db.DocExpedicao.Where(x => x.StatusId == 3).ToList(); //// -----> Escrever query

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

            DocExpedicao notafiscal = db.DocExpedicao.Where(x => x.Numero == numeroNF).FirstOrDefault();
            if (notafiscal == null && key.Length == 44)
            {
                numeroNF = numeroNF.TrimStart('0');
                notafiscal = db.DocExpedicao.Where(x => x.Numero == numeroNF).FirstOrDefault();
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
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
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
                         where ids == nf.Id
                         select nf).ToList();

            if (notas.Count == 0)
            {
                return Json(new { etiquetas = listaEtiquetas, success = true, msg = "Nenhuma NF localizada!" }, JsonRequestBehavior.AllowGet);
            }

            try
            {
                foreach (var notafiscal in notas)
                {
                    DocExpedicao doc = db.DocExpedicao.Find(notafiscal.Id);
                    if (doc != null)
                    {

                        // Transportadora
                        var transp = (from t in db.Transportadora
                                      where t.Id == doc.TransportadoraId
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

        }


        //método para imprimir etiquetas que deram problema, especificando os volumes
        [HttpGet]
        public ActionResult GetDataToPrintVolume(string key, int minVolume, int maxVolume)
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
                         where numeroNF == nf.Numero
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
                    DocExpedicao doc = db.DocExpedicao.Find(notafiscal.Id);
                    if (doc != null)
                    {

                        // Transportadora
                        var transp = (from t in db.Transportadora
                                      where t.Id == doc.TransportadoraId
                                      select t).FirstOrDefault();


                        doc.ModificadoEm = Util.GetCurrentDateTime();
                        doc.ModificadoPor = Util.GetCurrentUser();

                        // Gerar etiqueta (ZPL) para cada volume
                        if (!transp.EmitirEtiqueta)
                        {
                            return Json(new { etiquetas = listaEtiquetas, success = false, msg = "A transportadora cadastrada na nota fiscal não emite etiqueta!" }, JsonRequestBehavior.AllowGet);
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
                         where ids.Contains(nf.Id)
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
                    DocExpedicao doc = db.DocExpedicao.Find(notaFiscal.Id);
                    if (doc != null)
                    {
                        // Transportadora
                        var transp = (from t in db.Transportadora
                                      where t.Id == doc.TransportadoraId
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
                             where numeroNF == nf.Numero
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
                              where t.Id == notas.TransportadoraId
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
            DocExpedicao notaFiscal = db.DocExpedicao.Find(id);

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
        }

    }
}