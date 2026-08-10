using Newtonsoft.Json;
using Simplify.Quasar.Areas.EstoqueApp.ViewModels;
using Simplify.Quasar.Custom;
using Simplify.Quasar.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Validation;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Data;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Simplify.Quasar.Areas.EstoqueApp.Controllers
{
    public class UploadController : Controller
    {
        Quasar_Entities db = new Quasar_Entities();

        int filialId = Util.GetCurrentFilial();

        // GET: EstoqueApp/Upload
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public ActionResult UploadFileEstoque(UploadArquivo vm)
        {
            string sql = string.Empty;
            string msg = string.Empty;

            string dms = (from a in db.AppConfig where a.Nome == "DMS" select a.Valor).FirstOrDefault();
            if (string.IsNullOrEmpty(dms))
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
                db.Database.ExecuteSqlCommand("DELETE [EstoqueUpload] FROm [EstoqueUpload] WHERE FilialId = " + filialId);
                db.SaveChanges();

            }
            catch (Exception ex)
            {
                msg = "[EstoqueUploadColumns] DELETE TABLE failed<br>" + ex.Message;
                return Json(new { erro = true, mensagem = msg }, JsonRequestBehavior.AllowGet);
            }

            // Importar arquivo para tabela temporária
            int rows = 0;
            try
            {
                StreamReader reader = new StreamReader(arquivo.InputStream);
                string line;

                var dt = new DataTable();
                dt.Columns.Add(new DataColumn());
                dt.Columns.Add(new DataColumn("FilialId"));

                var dbConn = new SqlConnection(db.Database.Connection.ConnectionString);

                while ((line = reader.ReadLine()) != null)
                {
                    dt.Rows.Add(line, filialId);
                }

                var bullCopy = new SqlBulkCopy(dbConn, SqlBulkCopyOptions.TableLock, null)
                {
                    DestinationTableName = "EstoqueUpload",
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
                msg = "[EstoqueUpload] SqlBulkCopy failed<br>" + ex.Message;
                return Json(new { erro = true, mensagem = msg }, JsonRequestBehavior.AllowGet);
            }

            if (dms == "SERCON")
            {
                // Gerar tabela temporária de estoque
                try
                {
                    db.Database.ExecuteSqlCommand("DELETE [EstoqueUpload_SERCON] FROM [EstoqueUpload_SERCON] WHERE FilialId = " + filialId);
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    msg = "[EstoqueUpload_SERCON] TRUNCATE TABLE failed<br>" + ex.Message;
                    return Json(new { erro = true, mensagem = msg }, JsonRequestBehavior.AllowGet);
                }

                

                sql = (from s in db.AppSQL where s.Nome == "INSERT_EstoqueUpload_SERCON" select s.Comando).FirstOrDefault();
                if (!string.IsNullOrEmpty(sql))
                {
                    sql = Util.FormatSQL(sql);
                    //sql = sql.Replace("@FilialId", filialId.ToString());

                    try
                    {
                        db.Database.ExecuteSqlCommand(sql);
                        db.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        msg = "[EstoqueUpload] INSERT failed<br>" + ex.Message;
                        return Json(new { erro = true, mensagem = msg }, JsonRequestBehavior.AllowGet);
                    }
                }

                // Aplicar filtro de locações
                try
                {
                    List<string> exclude_locacoes = new List<string>();
                    string config = (from c in db.AppConfig where c.Nome == "ExcludeLoc" select c.Valor).FirstOrDefault();
                    List<string> filtro = config.Split(';').ToList();
                    foreach (string item in filtro)
                    {
                        List<string> locacoes = (from l in db.EstoqueUpload_SERCON where l.Local.StartsWith(item) select l.Local).ToList();
                        if (locacoes != null)
                        {
                            exclude_locacoes.AddRange(locacoes);
                        }
                    }
                    db.EstoqueUpload_SERCON.RemoveRange(db.EstoqueUpload_SERCON.Where(x => exclude_locacoes.Contains(x.Local)));
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    msg = "[EstoqueUpload] DELETE failed<br>" + ex.Message;
                    return Json(new { erro = true, mensagem = msg }, JsonRequestBehavior.AllowGet);
                }

                // Atualizar tabela de estoque
                sql = (from s in db.AppSQL where s.Nome == "UPDATE_Estoque_From_SERCON" select s.Comando).FirstOrDefault();
                if (!string.IsNullOrEmpty(sql))
                {
                    sql = Util.FormatSQL(sql);
                    //sql = sql.Replace("@FilialId", filialId.ToString());

                    try
                    {
                        db.Database.ExecuteSqlCommand(sql);
                        db.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        msg = "[Estoque] MERGE failed<br>" + ex.Message;
                        return Json(new { erro = true, mensagem = msg }, JsonRequestBehavior.AllowGet);
                    }
                }

                // Atualizar tabela de materiais
                sql = (from s in db.AppSQL where s.Nome == "UPDATE_Material_From_SERCON" select s.Comando).FirstOrDefault();
                if (!string.IsNullOrEmpty(sql))
                {
                    sql = Util.FormatSQL(sql);
                    //sql = sql.Replace("@FilialId", filialId.ToString());

                    try
                    {
                        db.Database.ExecuteSqlCommand(sql);
                        db.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        msg = "[Material] MERGE failed<br>" + ex.Message;
                        return Json(new { erro = true, mensagem = msg }, JsonRequestBehavior.AllowGet);
                    }
                }

                // Atualizar tabela de locações
                sql = (from s in db.AppSQL where s.Nome == "UPDATE_Locacao_From_SERCON" select s.Comando).FirstOrDefault();
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
                        msg = "[Locacao] MERGE failed<br>" + ex.Message;
                        return Json(new { erro = true, mensagem = msg }, JsonRequestBehavior.AllowGet);
                    }
                }
            }
            else
            {
                if (dms == "APOLLO")
                {
                    // Gerar tabela temporária de estoque
                    //try
                    //{
                    //    db.Database.ExecuteSqlCommand("DELETE [EstoqueUpload] FROM [EstoqueUpload] WHERE FilialId = " + filialId);
                    //    db.SaveChanges();
                    //}
                    //catch (Exception ex)
                    //{
                    //    msg = "[EstoqueUpload] DELETE TABLE failed<br>" + ex.Message;
                    //    return Json(new { erro = true, mensagem = msg }, JsonRequestBehavior.AllowGet);
                    //}

                    sql = (from s in db.AppSQL where s.Nome == "INSERT_EstoqueUpload" select s.Comando).FirstOrDefault();
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
                            msg = "[EstoqueUpload] INSERT failed<br>" + ex.Message;
                            return Json(new { erro = true, mensagem = msg }, JsonRequestBehavior.AllowGet);
                        }
                    }

                    // Aplicar filtro de locações
                    //try
                    //{
                    //    List<string> exclude_locacoes = new List<string>();
                    //    string config = (from c in db.AppConfig where c.Nome == "ExcludeLoc" select c.Valor).FirstOrDefault();
                    //    List<string> filtro = config.Split(';').ToList();
                    //    foreach (string item in filtro)
                    //    {
                    //        List<string> locacoes = (from l in db.EstoqueUpload_APOLLO where l.Local.StartsWith(item) select l.Local).ToList();
                    //        if (locacoes != null)
                    //        {
                    //            exclude_locacoes.AddRange(locacoes);
                    //        }
                    //    }
                    //    db.EstoqueUpload_APOLLO.RemoveRange(db.EstoqueUpload_APOLLO.Where(x => exclude_locacoes.Contains(x.Local)));
                    //    db.SaveChanges();
                    //}
                    //catch (Exception ex)
                    //{
                    //    msg = "[EstoqueUpload] DELETE failed<br>" + ex.Message;
                    //    return Json(new { erro = true, mensagem = msg }, JsonRequestBehavior.AllowGet);
                    //}

                    // Atualizar tabela de estoque
                    //sql = (from s in db.AppSQL where s.Nome == "UPDATE_Estoque_From_APOLLO" select s.Comando).FirstOrDefault();
                    //if (!string.IsNullOrEmpty(sql))
                    //{
                    //    sql = Util.FormatSQL(sql);
                        
                    //    try
                    //    {
                    //        db.Database.ExecuteSqlCommand(sql);
                    //        db.SaveChanges();
                    //    }
                    //    catch (Exception ex)
                    //    {
                    //        msg = "[Estoque] MERGE failed<br>" + ex.Message;
                    //        return Json(new { erro = true, mensagem = msg }, JsonRequestBehavior.AllowGet);
                    //    }
                    //}

                    // Atualizar LOCAÇÕES na tabela de estoque onde qtde Estoque = ZERO
                    sql = (from s in db.AppSQL where s.Nome == "UPDATE_ItemSemEstoque_From_APOLLO" select s.Comando).FirstOrDefault();
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
                            msg = "[Estoque] UPDATE failed<br>" + ex.Message;
                            return Json(new { erro = true, mensagem = msg }, JsonRequestBehavior.AllowGet);
                        }
                    }

                    // Atualizar tabela de materiais
                    sql = (from s in db.AppSQL where s.Nome == "UPDATE_Material_From_APOLLO" select s.Comando).FirstOrDefault();
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
                            msg = "[Material] MERGE failed<br>" + ex.Message;
                            return Json(new { erro = true, mensagem = msg }, JsonRequestBehavior.AllowGet);
                        }
                    }

                    // Atualizar tabela de locações
                    sql = (from s in db.AppSQL where s.Nome == "UPDATE_Locacao_From_APOLLO" select s.Comando).FirstOrDefault();
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
                            msg = "[Locacao] MERGE failed<br>" + ex.Message;
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

        public ActionResult ConfigUpload()
        {
            ConfigUploadViewModel vm = new ConfigUploadViewModel();
            AppConfig config = db.AppConfig.Where(x => x.Nome == "ExcludeLoc").FirstOrDefault();
            if (config != null)
            {
                vm.Id = config.Id;
                vm.Nome = config.Nome;
                vm.Descricao = config.Descricao;
                vm.Valor = config.Valor;

                vm.ModificadoPor = config.ModificadoPor;
                vm.ModificadoPorNome = (from u in db.Usuario where u.FilialId == filialId && u.Login == config.ModificadoPor select u.Nome).FirstOrDefault();
                vm.ModificadoEm = config.ModificadoEm;

                if (config.ModificadoPor == null)
                {
                    vm.ModificadoPor = config.CriadoPor;
                    vm.ModificadoPorNome = (from u in db.Usuario where u.FilialId == filialId && u.Login == config.CriadoPor select u.Nome).FirstOrDefault();
                    vm.ModificadoEm = config.CriadoEm;
                }
            }

            return PartialView("_ConfigUpload", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ConfigUpload(ConfigUploadViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                return PartialView("_ConfigUpload", vm);
            }

            ViewBag.ControllerName = ControllerContext.RouteData.Values["controller"].ToString();
            ViewBag.ActionName = ControllerContext.RouteData.Values["action"].ToString();

            using (DbContextTransaction tr = db.Database.BeginTransaction())
            {
                try
                {
                    if (vm.Valor == null || vm.Valor == string.Empty)
                    {
                        AppConfig config = db.AppConfig.Find(vm.Id);
                        if (config != null)
                        {
                            db.AppConfig.Remove(config);
                        }
                    }
                    else
                    {
                        AppConfig config = db.AppConfig.Find(vm.Id);
                        if (config == null)
                        {
                            AppConfig new_cfg = new AppConfig();
                            new_cfg.Nome = "ExcludeLoc";
                            new_cfg.Descricao = "Locações a serem desconsideradas na atualização do arquivo de inventário";
                            new_cfg.Valor = vm.Valor;
                            new_cfg.CriadoPor = Util.GetCurrentUser();
                            new_cfg.CriadoEm = Util.GetCurrentDateTime();
                            db.AppConfig.Add(new_cfg);
                        }
                        else
                        {
                            config.Valor = vm.Valor;
                            config.ModificadoPor = Util.GetCurrentUser();
                            config.ModificadoEm = Util.GetCurrentDateTime();
                            db.Entry(config).State = EntityState.Modified;
                        }
                    }

                    db.SaveChanges();
                    tr.Commit();
                }
                catch (DbEntityValidationException ex)
                {
                    ViewBag.Exception = ex.Message;
                    ViewBag.InnerException = ex.InnerException;
                    ViewBag.Source = ex.Source;

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
                    TempData["ErrorDetail"] = msgErro;

                    tr.Rollback();
                    return PartialView("_ConfigUpload", vm);
                }
                catch (Exception ex)
                {
                    ViewBag.Exception = ex.Message;
                    ViewBag.InnerException = ex.InnerException;
                    ViewBag.Source = ex.Source;
                    tr.Rollback();
                    return PartialView("_ConfigUpload", vm);
                }
            }

            return Json(new { success = true });
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