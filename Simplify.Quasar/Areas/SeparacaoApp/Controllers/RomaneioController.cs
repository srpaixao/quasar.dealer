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

using Simplify.Quasar.Areas.SeparacaoApp.ViewModels;
using Simplify.Quasar.Custom;
using Simplify.Quasar.Models;

namespace Simplify.Quasar.Areas.SeparacaoApp.Controllers
{
    [ValidateSession]
    public class RomaneioController : Controller
    {
        Quasar_Entities db = new Quasar_Entities();
        string current_user = Util.GetCurrentUser();

        // GET: SeparacaoApp/Romaneios
        //public ActionResult Index()
        //{
        //    RomaneioViewModel vm = new RomaneioViewModel();

        //    DateTime inicio = Util.GetCurrentDateTime().AddDays(-7);

        //    vm.TotalRomaneioPendente = db.Romaneio.Where(x => x.StatusId == 1 && x.CriadoEm >= inicio).Count();
        //    vm.TotalRomaneioSeparar = db.Romaneio.Where(x => x.StatusId == 2 && x.CriadoEm >= inicio).Count();
        //    vm.TotalRemaneioOcorrencia = db.Romaneio.Where(x => x.StatusId == 3 && x.CriadoEm >= inicio).Count();
        //    vm.TotalRomaneioFinalizado = db.Romaneio.Where(x => x.StatusId == 4 && x.CriadoEm >= inicio).Count(); ;

        //    ViewBag.SeparadorDDL = (from i in db.Usuario
        //                            where i.FuncaoId == 3
        //                            select new SelectListItem
        //                            {
        //                                Value = i.Id.ToString(),
        //                                Text = i.Nome,
        //                                Selected = i.Id == Id
        //                            }).ToList();

        //    return View(vm);
        //}




        [HttpGet]
        // GET: GetData
        //public ActionResult RomaneioGetData()
        //{
        //    DateTime inicio = Util.GetCurrentDateTime().AddDays(-15);

        //    var notas = (from ron in db.Romaneio.Where(x => x.CriadoEm >= inicio).DefaultIfEmpty()
        //                 select new RomaneioViewModel
        //                 {
        //                     Id = ron.Id,
        //                     RomaneioNr = ron.RomaneioNr,
        //                     DataEmissao = ron.DataEmissao,
        //                     ContatoNr = ron.ContatoNr,
        //                     VendedorId = ron.VendedorId,
        //                     SeparadorId = ron.VendedorId,
        //                     Separador = (from u in db.Usuario where u.Id == ron.SeparadorId select u.Nome).FirstOrDefault(),
        //                     DataSeparador = ron.DataSeparador,
        //                     ConferenteId = ron.ConferenteId,
        //                     Conferente = (from u in db.Usuario where u.Id == ron.ConferenteId select u.Nome).FirstOrDefault(),
        //                     DataConferente = ron.DataConferente,
        //                     //CriadoEm = ron.CriadoEm,
        //                     //CriadoPor = (from u in db.Usuario where u.Id == ron.Id select u.Nome).FirstOrDefault(),
        //                     //ModificadoEm = ron.ModificadoEm,
        //                     //ModificadoPor = (from u in db.Usuario where u.Id == ron.Id select u.Nome).FirstOrDefault()
        //                 }).ToList();

        //    JsonResult result = Json(new { data = notas }, JsonRequestBehavior.AllowGet);
        //    result.MaxJsonLength = int.MaxValue;

        //    return result;
        //}

        //public ActionResult Edit(int id)
        //{
        //    Romaneio romaneio = db.Romaneio.Find(id);
        //    if (romaneio == null)
        //    {
        //        return HttpNotFound();
        //    }

        //    RomaneioViewModel vm = new RomaneioViewModel();

        //    vm.Id = romaneio.Id;
        //    vm.RomaneioNr = romaneio.RomaneioNr;
        //    vm.DataEmissao = romaneio.DataEmissao;
        //    vm.ContatoNr = romaneio.ContatoNr;
        //    vm.VendedorId = romaneio.VendedorId;
        //    vm.SeparadorId = romaneio.SeparadorId;
        //    vm.DataSeparador = romaneio.DataSeparador;
        //    vm.ConferenteId = romaneio.ConferenteId;
        //    vm.StatusId = romaneio.StatusId;
        //    vm.Localizacao = romaneio.Localizacao;

        //    return View(vm);
        //}

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
                db.Database.ExecuteSqlCommand("TRUNCATE TABLE [RomaneioUpload]");
                db.SaveChanges();

            }
            catch (Exception ex)
            {
                msg = "[RomaneioUpload] TRUNCATE TABLE failed<br>" + ex.Message;
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
                    DestinationTableName = "RomaneioUpload",
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
                msg = "[RomaneioUpload] SqlBulkCopy failed<br>" + ex.Message;
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
                        db.Database.ExecuteSqlCommand("TRUNCATE TABLE [RomaneioUpload_APOLLO]");
                        db.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        msg = "[RomaneioUpload_APOLLO] TRUNCATE TABLE failed<br>" + ex.Message;
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
                            msg = "[RomaneioUpload_APOLLO] INSERT failed<br>" + ex.Message;
                            return Json(new { erro = true, mensagem = msg }, JsonRequestBehavior.AllowGet);
                        }
                    }

                    sql = (from s in db.AppSQL where s.Nome == "INSERT_MERGE_Romaneio" select s.Comando).FirstOrDefault();
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
                            msg = "[Romaneio] MERGE failed<br>" + ex.Message;
                            return Json(new { erro = true, mensagem = msg }, JsonRequestBehavior.AllowGet);
                        }
                    }

                    // Gerar histórico
                    sql = (from s in db.AppSQL where s.Nome == "INSERT_Historico_Romaneio" select s.Comando).FirstOrDefault();
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
                            msg = "[Historico_Romaneio] INSERT failed<br>" + ex.Message;
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

        //[HttpPost]
        //public ActionResult LancarRomaneio(List<RomaneioViewModel> romaneio)
        //{
        //    using (DbContextTransaction tr = db.Database.BeginTransaction())
        //    {
        //        try
        //        {
        //            foreach (var item in romaneio)
        //            {
        //                if (item.Id != 0)
        //                {
        //                    var nota = db.Romaneio.Find(item.Id);
        //                    if (nota != null)
        //                    {

        //                        nota.DataSeparador = item.DataSeparador;
        //                        nota.SeparadorId = item.SeparadorId;
        //                        nota.ModificadoPor = current_user;
        //                        nota.ModificadoEm = Util.GetCurrentDateTime();
        //                        db.Entry(nota).State = EntityState.Modified;
        //                        db.SaveChanges();
 
        //                    }
        //                }

        //            }
        //            tr.Commit();

        //            return Json(new { success = true, message = "Romaneio lançado com sucesso!" });
        //        }
        //        catch (Exception ex)
        //        {
        //            tr.Rollback();
        //            return Json(new { success = false, message = ex.Message });
        //        }
        //    }
        //}


        //public ActionResult GetRomaneio(string key)
        //{
        //    string romaneionr = key.Trim();
          
        //    Romaneio romaneio = new Romaneio();

        //    try
        //    {
        //        romaneio = db.Romaneio.Where(x => x.RomaneioNr == romaneionr).FirstOrDefault();
        //        //if (romaneio == null)
        //        //{
        //        //    numeroNF = romaneio.TrimStart('0');
        //        //    romaneio = db.DocExpedicao.Where(x => x.Numero == numeroNF).FirstOrDefault();
        //        //}

        //        if (romaneio == null)
        //        {
        //            JsonResult result = Json(new { data = romaneio, success = false, msg = "Romaneio Nr não encontrada!" }, JsonRequestBehavior.AllowGet);
        //            return result;
        //        }
        //        else
        //        {
        //            if (romaneio.StatusId != 1)
        //            {
        //                JsonResult result = Json(new { data = romaneio, success = false, msg = "Romaneio já está lançado!" }, JsonRequestBehavior.AllowGet);
        //                return result;
        //            }
        //            else
        //            {
        //                JsonResult result = Json(new { data = romaneio, success = true, msg = string.Empty }, JsonRequestBehavior.AllowGet);
        //                return result;
        //            }

        //        }

        //    }
        //    catch (Exception ex)
        //    {
        //        JsonResult result = Json(new { data = romaneio, success = false, msg = ex.Message }, JsonRequestBehavior.AllowGet);
        //        return result;
        //    }

        //}

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
                                char[] zero = { '0'};
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

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }      

        [HttpGet]
        public ActionResult GetUser()
        {
            string currentUser;
            try
            {
                currentUser = Util.GetCurrentUser();
            }
            catch (Exception ex) {

                return Json(new { success = false, msg = ex.Message });
            }

            JsonResult result = Json(new { user = currentUser, success = true, msg = "Requisição completa com sucesso!" }, JsonRequestBehavior.AllowGet);
            return result;

        }
        
    }
}