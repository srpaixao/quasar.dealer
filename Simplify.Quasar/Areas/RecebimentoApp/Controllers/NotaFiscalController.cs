using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Data.Entity;
using System.Web;
using System.IO;
using System.Data;
using System.Data.SqlClient;
using Newtonsoft.Json;

using Simplify.Quasar.Areas.RecebimentoApp.ViewModels;
using Simplify.Quasar.Models;
using Simplify.Quasar.Custom;

namespace Simplify.Quasar.Areas.RecebimentoApp.Controllers
{
    [ValidateSession]
    public class NotaFiscalController : Controller
    {
        Quasar_Entities db = new Quasar_Entities();

        int filialId = Util.GetCurrentFilial();
        public string IdNF { get; private set; }

        int periodo;
        DateTime inicio;
        public NotaFiscalController()
        {
            periodo = Util.GetPeriodoRecebimento();
            inicio = DateTime.Now.AddDays(-periodo);

        }

        // GET: Recebimento/NotaFiscal/Index
        public ActionResult Index()
        {
            //DateTime inicio = Util.GetCurrentDateTime().AddDays(-30);

            var vm = (from nf in db.NotaFiscal
                      where nf.CriadoEm >= inicio && nf.FilialId == filialId
                      select new NotaFiscalViewModel
                      {
                          Id = nf.Id,
                          Numero = nf.Numero,
                          TipoId = nf.TipoId,
                          StatusId = nf.StatusId,
                          StatusNF = (from s in db.StatusNotaFiscal where s.Id == nf.StatusId select s.Nome).FirstOrDefault(),
                          Emissor = nf.Emissor,
                          ModificadoEm = nf.ModificadoEm == null ? nf.CriadoEm : nf.ModificadoEm
                      }).ToList();

            foreach (var item in vm)
            {
                if (item.TipoId == 1)
                {
                    // Rede
                    item.NomeEmissor = (from f in db.Fornecedor where f.CNPJ == item.Emissor select f.Nome).FirstOrDefault();
                }

                if (item.TipoId == 2)
                {
                    // Devolução
                    item.NomeEmissor = string.Empty;
                }

                if (item.TipoId == 3)
                {
                    // Transferência
                    item.NomeEmissor = (from e in db.Empresa where e.CNPJ == item.Emissor select e.Nome).FirstOrDefault();
                }

                if (item.TipoId == 4)
                {
                    // GM
                    item.NomeEmissor = "GM";
                }
            }

            ViewBag.StatusNF = new SelectList(db.StatusNotaFiscal, "Id", "Nome");

            return View(vm);
        }

        // GET: Recebimento/NotaFiscal/Rede
        public ActionResult Rede()
        {
            //var locacoes = (from l in db.Locacao
            //                where !db.Estoque.Any(x => x.Locacao == l.Codigo && x.Saldo == 0)
            //                select l.Codigo).ToList();

            //ViewBag.Locacoes = string.Join(",", locacoes);
            return View();
        }

        [HttpPost]
        public ActionResult Rede(List<NotaFiscalRedeViewModel> notafiscal)
        {
            using (DbContextTransaction tr = db.Database.BeginTransaction())
            {
                try
                {
                    NotaFiscal nf = new NotaFiscal();
                    nf.Movimento = "E";
                    nf.TipoId = 1; // Rede
                    nf.StatusId = 3; // Conferência
                    nf.Numero = notafiscal.First().NumeroNF;
                    nf.Emissor = notafiscal.First().Fornecedor;
                    nf.Danfe = notafiscal.First().Danfe;
                    nf.CriadoEm = Util.GetCurrentDateTime();
                    nf.CriadoPor = Util.GetCurrentUser();
                    nf.FilialId = filialId;
                    db.NotaFiscal.Add(nf);
                    db.SaveChanges();

                    if (notafiscal.First().AddFornecedor)
                    {
                        var fornecedor = db.Fornecedor.Where(x => x.CNPJ == nf.Emissor).FirstOrDefault();
                        if (fornecedor == null)
                        {
                            Fornecedor novo_fornecedor = new Fornecedor();
                            novo_fornecedor.Nome = notafiscal.First().NomeFornecedor;
                            novo_fornecedor.CNPJ = notafiscal.First().Fornecedor;
                            novo_fornecedor.StatusId = 1;
                            novo_fornecedor.CriadoPor = Util.GetCurrentUser();
                            novo_fornecedor.CriadoEm = Util.GetCurrentDateTime();
                            db.Fornecedor.Add(novo_fornecedor);
                            db.SaveChanges();
                        }
                    }

                    foreach (var item in notafiscal)
                    {
                        NotaFiscalItem itemNF = new NotaFiscalItem();
                        itemNF.NotaFiscalId = nf.Id;
                        itemNF.Item = item.ItemNr;
                        itemNF.Quantidade = item.Quantidade;
                        itemNF.Volume = "Rede/Fornecedores";
                        itemNF.StatusId = 3;
                        itemNF.CriadoEm = Util.GetCurrentDateTime();
                        itemNF.CriadoPor = Util.GetCurrentUser();
                        itemNF.FilialId = filialId;
                        db.NotaFiscalItem.Add(itemNF);
                        db.SaveChanges();

                        var material = db.Material.Where(x => x.Codigo == item.ItemNr).FirstOrDefault();
                        if (material == null)
                        {
                            Material novo_material = new Material();
                            novo_material.Codigo = item.ItemNr;
                            novo_material.Descricao = item.Descricao == null ? string.Empty : item.Descricao;
                            novo_material.UN = "PC";
                            novo_material.EmbalagemMin = null;
                            novo_material.MediaVendas = null;
                            novo_material.CustoUnitario = null;
                            novo_material.Curva = "N";
                            novo_material.CriadoPor = Util.GetCurrentUser();
                            novo_material.CriadoEm = Util.GetCurrentDateTime();
                            db.Material.Add(novo_material);
                            db.SaveChanges();
                        }

                        var estoque = db.Estoque.Where(x => x.ItemNr == item.ItemNr).ToList();
                        if (estoque.Count() == 0)
                        {
                            Estoque novo_estoque = new Estoque();
                            novo_estoque.Locacao = string.Empty;
                            novo_estoque.ItemNr = item.ItemNr;
                            novo_estoque.Saldo = item.Quantidade;
                            novo_estoque.Indisponivel = null;
                            novo_estoque.PedidoPendente = null;
                            novo_estoque.ValorEstoque = null;
                            novo_estoque.Range = null;
                            novo_estoque.CriadoPor = Util.GetCurrentUser();
                            novo_estoque.CriadoEm = Util.GetCurrentDateTime();
                            novo_estoque.FilialId = filialId;
                            db.Estoque.Add(novo_estoque);
                            db.SaveChanges();
                        }
                    }

                    tr.Commit();

                    return Json(new { success = true, message = "Volumes coletados com sucesso!" });
                }
                catch (Exception ex)
                {
                    tr.Rollback();
                    return Json(new { success = false, message = ex.Message });
                }
            }
        }

        // GET: Recebimento/NotaFiscal/Transferencia
        public ActionResult Transferencia()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Transferencia(List<NotaFiscalTransfViewModel> notafiscal)
        {
            using (DbContextTransaction tr = db.Database.BeginTransaction())
            {
                try
                {
                    NotaFiscal nf = new NotaFiscal();
                    nf.Movimento = "E";
                    nf.TipoId = 3;
                    nf.StatusId = 3;
                    nf.Numero = notafiscal.First().NumeroNF;
                    nf.Emissor = notafiscal.First().Filial;
                    nf.Danfe = notafiscal.First().Danfe;
                    nf.CriadoEm = Util.GetCurrentDateTime();
                    nf.CriadoPor = Util.GetCurrentUser();
                    nf.FilialId = filialId;
                    db.NotaFiscal.Add(nf);
                    db.SaveChanges();

                    foreach (var item in notafiscal)
                    {
                        NotaFiscalItem itemNF = new NotaFiscalItem();
                        itemNF.NotaFiscalId = nf.Id;
                        itemNF.Item = item.ItemNr;
                        itemNF.Quantidade = item.Quantidade;
                        itemNF.Volume = "Transferência";
                        itemNF.StatusId = 3;
                        itemNF.CriadoEm = Util.GetCurrentDateTime();
                        itemNF.CriadoPor = Util.GetCurrentUser();
                        itemNF.FilialId = filialId;
                        db.NotaFiscalItem.Add(itemNF);
                        db.SaveChanges();

                        var material = db.Material.Where(x => x.Codigo == item.ItemNr).FirstOrDefault();
                        if (material == null)
                        {
                            Material novo_material = new Material();
                            novo_material.Codigo = item.ItemNr;
                            novo_material.Descricao = item.Descricao == null ? string.Empty : item.Descricao;
                            novo_material.UN = "PC";
                            novo_material.EmbalagemMin = null;
                            novo_material.MediaVendas = null;
                            novo_material.CustoUnitario = null;
                            novo_material.Curva = "N";
                            novo_material.CriadoPor = Util.GetCurrentUser();
                            novo_material.CriadoEm = Util.GetCurrentDateTime();
                            db.Material.Add(novo_material);
                            db.SaveChanges();
                        }

                        var estoque = db.Estoque.Where(x => x.ItemNr == item.ItemNr).ToList();
                        if (estoque.Count() == 0)
                        {
                            Estoque novo_estoque = new Estoque();
                            novo_estoque.Locacao = string.Empty;
                            novo_estoque.ItemNr = item.ItemNr;
                            novo_estoque.Saldo = item.Quantidade;
                            novo_estoque.Indisponivel = null;
                            novo_estoque.PedidoPendente = null;
                            novo_estoque.ValorEstoque = null;
                            novo_estoque.Range = null;
                            novo_estoque.CriadoPor = Util.GetCurrentUser();
                            novo_estoque.CriadoEm = Util.GetCurrentDateTime();
                            novo_estoque.FilialId = filialId;
                            db.Estoque.Add(novo_estoque);
                            db.SaveChanges();
                        }
                    }

                    tr.Commit();

                    return Json(new { success = true, message = "Volumes coletados com sucesso!" });
                }
                catch (Exception ex)
                {
                    tr.Rollback();
                    return Json(new { success = false, message = ex.Message });
                }
            }
        }

        // GET: Recebimento/NotaFiscal/Devolucao
        public ActionResult Devolucao()
        {
            return View();
        }

        public ActionResult RecebimentoADM()
        {
            return View();
        }

        [HttpGet]
        public ActionResult GetDataADM()
        {
            var notas = (from nf in db.NotaFiscal
                         where nf.RecebidoAdmEm == null && nf.FilialId == filialId
                         select new NotaFiscalViewModel
                         {
                             Id = nf.Id,
                             Numero = nf.Numero,
                             TipoId = nf.TipoId,
                             TipoNF = (from t in db.TipoNotaFiscal where t.Id == nf.TipoId select t.Descricao).FirstOrDefault(),
                             StatusId = nf.StatusId,
                             StatusNF = (from s in db.StatusNotaFiscal where s.Id == nf.StatusId select s.Nome).FirstOrDefault(),
                             Emissor = nf.Emissor,
                             QtdItensNF = (from i in db.NotaFiscalItem where i.NotaFiscalId == nf.Id select i).Count(),
                             QtdItens = (from i in db.NotaFiscalItem where i.NotaFiscalId == nf.Id select i.Item).Distinct().Count(),
                             QtdVolumes = (from i in db.NotaFiscalItem where i.NotaFiscalId == nf.Id select i.Volume).Distinct().Count(),
                             QtdTotal = (from i in db.NotaFiscalItem where i.NotaFiscalId == nf.Id select i.Quantidade).Sum(),
                             ModificadoEm = nf.ModificadoEm == null ? nf.CriadoEm : nf.ModificadoEm
                         }).ToList();

            foreach (var nota in notas)
            {
                nota.Emissor = nota.Emissor == null ? string.Empty : Util.FormatCNPJ(nota.Emissor);

                // nome do emissor depende do tipo da NF => 
                // 1 rede (tabela Fornecedor)
                // 2 devolução (vazio) 
                // 3 transferência (tabela Empresa)
                // 4 trânsito GM (fixo "GM")

                nota.NomeEmissor = nota.TipoId == 1 ? (from f in db.Fornecedor where f.CNPJ == nota.Emissor select f.Nome).FirstOrDefault() :
                                 nota.TipoId == 3 ? (from f in db.Empresa where f.CNPJ == nota.Emissor select f.Nome).FirstOrDefault() :
                                 nota.TipoId == 4 ? "GM" :
                                 string.Empty;

                nota.OrigemNF = string.Empty;
                if (nota.TipoId == 4)
                {
                    var origemNF = (from o in db.OrigemNotaFiscal where o.Codigo == nota.Observacoes select o).FirstOrDefault();
                    if (origemNF != null)
                    {
                        nota.OrigemNF = string.Concat(origemNF.Codigo, " - ", origemNF.Descricao);
                    }
                }

                nota._itens = (from nfi in db.NotaFiscalItem
                               where nfi.NotaFiscalId == nota.Id && nfi.FilialId == filialId
                               select new ItemNotaFiscalViewModel
                               {
                                   ItemNr = nfi.Item,
                                   ItemDesc = (from m in db.Material where m.Codigo == nfi.Item select m.Descricao).FirstOrDefault(),
                                   Quantidade = nfi.Quantidade,
                                   Volume = nfi.Volume,
                                   Pedido = nfi.Pedido
                               }).ToList();

                nota.CriadoEm = nota.CriadoEm;
                nota.CriadoPor = nota.CriadoPor;
                nota.CriadoPorNome = (from u in db.Usuario where u.Login == nota.CriadoPor select u.Nome).FirstOrDefault();

                nota.ModificadoEm = nota.ModificadoEm;
                nota.ModificadoPor = nota.ModificadoPor;
                nota.ModificadoPorNome = (from u in db.Usuario where u.Login == nota.ModificadoPor select u.Nome).FirstOrDefault();

                nota.RecebidoAdmEm = nota.RecebidoAdmEm;
                nota.RecebidoAdmPor = nota.RecebidoAdmPor;
                nota.RecebidoAdmPorNome = (from u in db.Usuario where u.Login == nota.RecebidoAdmPor select u.Nome).FirstOrDefault();
            }

            JsonResult result = Json(new { data = notas }, JsonRequestBehavior.AllowGet);
            result.MaxJsonLength = int.MaxValue;
            return result;
        }


        [HttpPost]
        public ActionResult ConfirmarADM(string danfe)
        {
            danfe = danfe.Trim();

            if (danfe.Length != 44)
            {
                return Json(new { success = false, msg = "Chave NFe inválida!" }, JsonRequestBehavior.AllowGet);
            }

            string numeroNF = danfe.Substring(25, 9);
            var notafiscal = (from nf in db.NotaFiscal where nf.Numero == numeroNF select nf).FirstOrDefault();
            if (notafiscal == null)
            {
                return Json(new { success = false, msg = "Nota Fiscal não encontrada!" }, JsonRequestBehavior.AllowGet);
            }

            // Atualizar a nota fiscal
            try
            {
                notafiscal.RecebidoAdmPor = Util.GetCurrentUser();
                notafiscal.RecebidoAdmEm = Util.GetCurrentDateTime();
                notafiscal.ModificadoPor = Util.GetCurrentUser();
                notafiscal.ModificadoEm = Util.GetCurrentDateTime();
                notafiscal.FilialId = filialId;
                db.Entry(notafiscal).State = EntityState.Modified;
                db.SaveChanges();

                return Json(new { success = true, msg = string.Empty }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, msg = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // GET: GetDataTransito
        [HttpGet]
        public ActionResult GetDataTransito(string material)
        {
            if (material == string.Empty)
            {
                var transito = (from nf in db.NotaFiscal
                                where nf.TipoId == 4 && nf.FilialId == filialId
                                select new TransitoViewModel
                                {
                                    NotaFiscalId = nf.Id,
                                    NotaFiscalNr = nf.Numero,
                                    Fornecedor = "GM",
                                    Origem = (from o in db.OrigemNotaFiscal where o.Codigo == nf.Observacoes select o.Codigo + "-" + o.Descricao).FirstOrDefault(),
                                    Status = (from s in db.StatusNotaFiscal where s.Id == nf.StatusId select s.Nome).FirstOrDefault(),
                                    QtdItensNF = (from i in db.NotaFiscalItem where i.NotaFiscalId == nf.Id select i).Count(),
                                    QtdItens = (from i in db.NotaFiscalItem where i.NotaFiscalId == nf.Id select i.Item).Distinct().Count(),
                                    QtdVolumes = (from i in db.NotaFiscalItem where i.NotaFiscalId == nf.Id select i.Volume).Distinct().Count(),
                                    QtdTotal = (from i in db.NotaFiscalItem where i.NotaFiscalId == nf.Id select i.Quantidade).Sum(),
                                    ModificadoEm = nf.ModificadoEm == null ? nf.CriadoEm : nf.ModificadoEm
                                }).ToList();

                JsonResult result = Json(new { data = transito }, JsonRequestBehavior.AllowGet);
                result.MaxJsonLength = int.MaxValue;
                return result;
            }
            else
            {
                var transito = (from nf in db.NotaFiscal
                                join nfi in db.NotaFiscalItem on nf.Id equals nfi.NotaFiscalId
                                where nf.TipoId == 4 && nfi.Item == material && nf.FilialId == filialId
                                select new TransitoViewModel
                                {
                                    NotaFiscalId = nf.Id,
                                    NotaFiscalNr = nf.Numero,
                                    Fornecedor = "GM",
                                    Origem = (from o in db.OrigemNotaFiscal where o.Codigo == nf.Observacoes select o.Codigo + "-" + o.Descricao).FirstOrDefault(),
                                    Status = (from s in db.StatusNotaFiscal where s.Id == nf.StatusId select s.Nome).FirstOrDefault(),
                                    QtdItensNF = (from i in db.NotaFiscalItem where i.NotaFiscalId == nf.Id select i).Count(),
                                    QtdItens = (from i in db.NotaFiscalItem where i.NotaFiscalId == nf.Id select i.Item).Distinct().Count(),
                                    QtdVolumes = (from i in db.NotaFiscalItem where i.NotaFiscalId == nf.Id select i.Volume).Distinct().Count(),
                                    QtdTotal = (from i in db.NotaFiscalItem where i.NotaFiscalId == nf.Id select i.Quantidade).Sum(),
                                    ModificadoEm = nf.ModificadoEm == null ? nf.CriadoEm : nf.ModificadoEm
                                }).Distinct().ToList();

                JsonResult result = Json(new { data = transito }, JsonRequestBehavior.AllowGet);
                result.MaxJsonLength = int.MaxValue;
                return result;
            }

        }

        // GET: NotaFiscal/GetItens
        public ActionResult GetItens(int notafiscal)
        {
            NotaFiscal nf = db.NotaFiscal.Find(notafiscal);
            if (nf == null)
            {
                return HttpNotFound();
            }

            NotaFiscalViewModel vm = new NotaFiscalViewModel();
            vm.Numero = nf.Numero;
            vm.Emissor = nf.Emissor == null ? string.Empty : Util.FormatCNPJ(nf.Emissor);

            // nome do emissor depende do tipo da NF => 
            // 1 rede (tabela Fornecedor)
            // 2 devolução (vazio) 
            // 3 transferência (tabela Empresa)
            // 4 trânsito GM (fixo "GM")

            vm.NomeEmissor = nf.TipoId == 1 ? (from f in db.Fornecedor where f.CNPJ == nf.Emissor select f.Nome).FirstOrDefault() :
                             nf.TipoId == 3 ? (from f in db.Empresa where f.CNPJ == nf.Emissor select f.Nome).FirstOrDefault() :
                             nf.TipoId == 4 ? "GM" :
                             string.Empty;

            vm.TipoNF = (from t in db.TipoNotaFiscal where t.Id == nf.TipoId select t.Descricao).FirstOrDefault();
            vm.StatusNF = (from s in db.StatusNotaFiscal where s.Id == nf.StatusId select s.Nome).FirstOrDefault();

            vm.OrigemNF = string.Empty;
            if (nf.TipoId == 4)
            {
                var origemNF = (from o in db.OrigemNotaFiscal where o.Codigo == nf.Observacoes select o).FirstOrDefault();
                if (origemNF != null)
                {
                    vm.OrigemNF = string.Concat(origemNF.Codigo, " - ", origemNF.Descricao);
                }
            }

            vm._itens = (from nfi in db.NotaFiscalItem
                         where nfi.NotaFiscalId == notafiscal && nfi.FilialId == filialId
                         select new ItemNotaFiscalViewModel
                         {
                             ItemNr = nfi.Item,
                             ItemDesc = (from m in db.Material where m.Codigo == nfi.Item select m.Descricao).FirstOrDefault(),
                             Quantidade = nfi.Quantidade,
                             Volume = nfi.Volume,
                             Pedido = nfi.Pedido,
                             Status = (from m in db.StatusNotaFiscal where m.Id == nfi.StatusId select m.Nome).FirstOrDefault()
                         }).ToList();

            vm.CriadoEm = nf.CriadoEm;
            vm.CriadoPor = nf.CriadoPor;
            vm.CriadoPorNome = (from u in db.Usuario where u.Login == nf.CriadoPor select u.Nome).FirstOrDefault();

            vm.ModificadoEm = nf.ModificadoEm;
            vm.ModificadoPor = nf.ModificadoPor;
            vm.ModificadoPorNome = (from u in db.Usuario where u.Login == nf.ModificadoPor select u.Nome).FirstOrDefault();

            vm.RecebidoAdmEm = nf.RecebidoAdmEm;
            vm.RecebidoAdmPor = nf.RecebidoAdmPor;
            vm.RecebidoAdmPorNome = (from u in db.Usuario where u.Login == nf.RecebidoAdmPor select u.Nome).FirstOrDefault();

            return PartialView("_ItensNF", vm);
        }

        // Upload arquivo de trânsito 
        [HttpPost]
        public ActionResult UploadFileTransito(UploadArquivo vm)
        {
            string msg = string.Empty;

            if (vm.Arquivo == null)
            {
                msg = "Arquivo não informado";
                return Json(new { erro = true, mensagem = msg }, JsonRequestBehavior.AllowGet);
            }

            HttpPostedFileBase arquivo = vm.Arquivo;
            if (arquivo == null)
            {
                msg = "[HttpPostedFileBase] Não foi possível immportar o arquivo informado";
                return Json(new { erro = true, mensagem = msg }, JsonRequestBehavior.AllowGet);
            }

            // DELETE tabela TransitoUpload
            try
            {
                db.Database.ExecuteSqlCommand("DELETE [TransitoUpload] FROM [TransitoUpload] where FilialId = " + filialId + ";");
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                msg = "[TransitoUpload] DELETE TABLE failed<br>" + ex.Message;
                return Json(new { erro = true, mensagem = msg }, JsonRequestBehavior.AllowGet);
            }

            // DELETE tabela TransitoUploadColumns
            try
            {
                db.Database.ExecuteSqlCommand("DELETE [TransitoUploadColumns] FROM [TransitoUploadColumns] where FilialId = " + filialId + ";");
                db.SaveChanges();

            }
            catch (Exception ex)
            {
                msg = "[TransitoUploadColumns] DELETE TABLE failed<br>" + ex.Message;
                return Json(new { erro = true, mensagem = msg }, JsonRequestBehavior.AllowGet);
            }

            // Importar arquivo para tabela temporária
            int rows = 0;
            //int filialId = Util.GetCurrentFilial();
            try
            {
                StreamReader reader = new StreamReader(arquivo.InputStream);
                string line;

                var dt = new DataTable();
                dt.Columns.Add(new DataColumn());
                var dbConn = new SqlConnection(db.Database.Connection.ConnectionString);

                dt.Columns.Add(new DataColumn("FilialId"));

                while ((line = reader.ReadLine()) != null)
                {
                    dt.Rows.Add(line, Util.GetCurrentFilial());                    
                }

                var bullCopy = new SqlBulkCopy(dbConn, SqlBulkCopyOptions.TableLock, null)
                {
                    DestinationTableName = "TransitoUpload",
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
                msg = "[TransitoUpload] SqlBulkCopy failed<br>" + ex.Message;
                return Json(new { erro = true, mensagem = msg }, JsonRequestBehavior.AllowGet);
            }

            // INSERT tabela TransitoUploadColumns
            string sql = (from s in db.AppSQL where s.Nome == "INSERT_TransitoUploadColumns" select s.Comando).FirstOrDefault();
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
                    msg = "[TransitoUploadColumns] INSERT failed<br>" + ex.Message;
                    return Json(new { erro = true, mensagem = msg }, JsonRequestBehavior.AllowGet);
                }
            }

            // UPDATE tabela TransitoUploadColumns
            sql = (from s in db.AppSQL where s.Nome == "UPDATE_TransitoUploadColumns" select s.Comando).FirstOrDefault();
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
                    msg = "[TransitoUploadColumns] UPDATE failed<br>" + ex.Message;
                    return Json(new { erro = true, mensagem = msg }, JsonRequestBehavior.AllowGet);
                }
            }

            // UPDATE tabela Materiais
            sql = (from s in db.AppSQL where s.Nome == "INSERT_Material_From_Transito" select s.Comando).FirstOrDefault();
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
                    msg = "[TransitoUploadColumns] UPDATE failed<br>" + ex.Message;
                    return Json(new { erro = true, mensagem = msg }, JsonRequestBehavior.AllowGet);
                }
            }

            // INSERT tabela NotaFiscal (com MERGE)
            sql = (from s in db.AppSQL where s.Nome == "INSERT_MERGE_NotaFiscal" select s.Comando).FirstOrDefault();
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
                    msg = "[NotaFiscal] INSERT (MERGE) failed<br>" + ex.Message;
                    return Json(new { erro = true, mensagem = msg }, JsonRequestBehavior.AllowGet);
                }
            }


            // INSERT tabela NotaFiscalItem (com MERGE)
            sql = (from s in db.AppSQL where s.Nome == "INSERT_MERGE_NotaFiscalItem" select s.Comando).FirstOrDefault();
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
                    msg = "[NotaFiscalItem] INSERT (MERGE) failed<br>" + ex.Message;
                    return Json(new { erro = true, mensagem = msg }, JsonRequestBehavior.AllowGet);
                }
            }


            msg = "Arquivo importado com sucesso";
            return Json(new { erro = false, mensagem = msg, qtd_linhas = rows }, JsonRequestBehavior.AllowGet);
        }

        // GET: ConferenciaVolume
        public ActionResult ConferenciaVolume()
        {
            ViewBag.Pendente = db.Volume.Where(x => x.StatusId == 1 && x.FilialId == filialId).Select(x => x.VolumeNr).Distinct().Count();
            ViewBag.Conferido = db.Volume.Where(x => x.StatusId == 2 && x.FilialId == filialId).Select(x => x.VolumeNr).Distinct().Count();
            ViewBag.Incorreto = db.Volume.Where(x => x.StatusId == 3 && x.FilialId == filialId).Select(x => x.VolumeNr).Distinct().Count();
            ViewBag.Total = db.Volume.Where(x => x.StatusId != 3 && x.FilialId == filialId).Select(x => x.VolumeNr).Distinct().Count();

            ViewBag.AreaDDL = (from b in db.Area
                               where b.Tipo == "R" && b.FilialId == filialId
                               orderby b.Nome, b.Descricao
                               select new SelectListItem
                               {
                                   Value = b.Id.ToString(),
                                   Text = b.Nome + " - " + b.Descricao,
                               }
                            ).ToList();


            return View(new List<VolumeViewModel>());
        }

        // GET: GetDataVolume
        [HttpGet]

        public ActionResult GetDataVolume(int statusId, int areaId, int filialId)
        {
            var volumes = (from v in db.Volume
                           join a in db.Area on v.AreaId equals a.Id
                           join sv in db.StatusVolume on v.StatusId equals sv.Id
                           where a.Id == areaId && v.FilialId == filialId
                           select new VolumeViewModel
                           {
                               Area = a.Nome,
                               VolumeNr = v.VolumeNr,
                               NotaFiscalNr = v.NotaFiscalNr,
                               QtdeItens = v.QtdItens,
                               StatusId = v.StatusId,
                               StatusNome = sv.Nome,
                               CriadoEm = v.CriadoEm
                           }).ToList();

            if (statusId != 0)
            {
                volumes = volumes.Where(x => x.FilialId == filialId && x.StatusId == statusId && x.AreaId == areaId ).ToList();
            }

            // Agrupar volumes
            var group = (from v in volumes.AsEnumerable()
                         where v.FilialId == filialId
                         group v by v.VolumeNr into grp
                         select new VolumeViewModel
                         {
                             VolumeNr = grp.Key,
                             NotaFiscalNr = string.Join(" / ", grp.Select(x => x.NotaFiscalNr)),
                             QtdeItens = grp.Select(x => x.QtdeItens).Sum(),
                             StatusId = grp.Select(x => x.StatusId).First(),
                             StatusNome = grp.Select(x => x.StatusNome).First(),
                             CriadoEm = grp.Select(x => x.CriadoEm).Max()
                         }).ToList();

            JsonResult result = Json(new { data = group }, JsonRequestBehavior.AllowGet);
            result.MaxJsonLength = int.MaxValue;
            return result;
        }

        // GET: ConferenciaVolumeDANFE       
        public ActionResult ConferenciaVolumeDANFE()
        {
 
            ViewBag.AreaDDL = (from b in db.Area
                               where b.Id > 13 && b.FilialId == filialId
                               orderby b.Nome, b.Descricao
                               select new SelectListItem
                               {
                                   Value = b.Id.ToString(),
                                   Text = b.Nome + " - " + b.Descricao,

                               }
                ).ToList();


            return View(new List<VolumeViewModel>());
        }

        // GET: GetDataVolumeDANFE
        [HttpGet]
        public ActionResult GetDataVolumeDANFE(int statusId, int areaId)
        {
            var volumes = (from v in db.Volume
                           join a in db.Area on v.AreaId equals a.Id
                           join sv in db.StatusVolume on v.StatusId equals sv.Id
                           where a.Id == areaId && v.FilialId == filialId
                           select new VolumeViewModel
                           {
                               Area = a.Nome,
                               AreaId = a.Id,
                               VolumeNr = v.VolumeNr,
                               NotaFiscalNr = v.NotaFiscalNr,
                               QtdeItens = v.QtdItens,
                               StatusId = v.StatusId,
                               StatusNome = sv.Nome,
                               CriadoEm = v.CriadoEm
                           }).ToList();

            if (statusId != 0)
            {
                volumes = volumes.Where(x => x.StatusId == statusId && x.AreaId == areaId && x.FilialId == filialId).ToList();
            }

            // Agrupar volumes
            var group = (from v in volumes.AsEnumerable()                         
                         group v by v.VolumeNr into grp
                         select new VolumeViewModel
                         {
                             VolumeNr = grp.Key,
                             NotaFiscalNr = string.Join(" / ", grp.Select(x => x.NotaFiscalNr)),
                             QtdeItens = grp.Select(x => x.QtdeItens).Sum(),
                             StatusId = grp.Select(x => x.StatusId).First(),
                             StatusNome = grp.Select(x => x.StatusNome).First(),
                             CriadoEm = grp.Select(x => x.CriadoEm).Max()
                         }).ToList();

            JsonResult result = Json(new { data = group }, JsonRequestBehavior.AllowGet);
            result.MaxJsonLength = int.MaxValue;
            return result;
        }

        // POST: AddVolume
        [HttpPost]
        public ActionResult AddVolume(string danfe, int areaid)
        {

            string nf = danfe.Substring(25, 9);

            int IdNF = (from x in db.NotaFiscal
                        where x.Numero == nf
                        select x.Id).Distinct().FirstOrDefault();

                if (IdNF == 0)
            {
                return Json(new { msg = "A Nota Fiscal não cadastrada!", erro = true });
            }

                if (db.Volume.Any(x => x.Danfe == danfe))
            {
                return Json(new { msg = "A Nota Fiscal já processada!", erro = true });
            }

            using (DbContextTransaction tr = db.Database.BeginTransaction())
            {

                try
                {

                    var transito = (from item in db.NotaFiscalItem
                                    group item by item.Volume into vol
                                    join nota in db.NotaFiscal on vol.FirstOrDefault().NotaFiscalId equals nota.Id
                                    where nota.Numero == nf && nota.FilialId == filialId
                                    select new
                                    {
                                        VolumeNr = vol.Key,
                                        IdNF = nota.Id,
                                        QtdeItens = vol.Count()
                                    }).OrderBy(x => x.VolumeNr).ToList();                 

                    foreach (var item in transito)
                    {
                        Volume volume = new Volume();
                        volume.AreaId = areaid;
                        volume.NotaFiscalNr = nf;
                        volume.VolumeNr = item.VolumeNr;
                        volume.StatusId = 1;
                        volume.QtdItens = item.QtdeItens;
                        volume.Imprimir = false;
                        volume.Danfe = danfe;
                        volume.FilialId = filialId;
                        volume.CriadoEm = Util.GetCurrentDateTime();
                        volume.CriadoPor = Util.GetCurrentUser();

                        db.Volume.Add(volume);
                        db.SaveChanges();

                        var notafiscal = db.NotaFiscal.Find(item.IdNF);
                        if (notafiscal != null)
                        {
                            if (notafiscal.StatusId == 1)
                            {
                                notafiscal.StatusId = 2;
                                db.Entry(notafiscal).State = EntityState.Modified;

                                var itens_nf = db.NotaFiscalItem
                                               .Where(x => x.NotaFiscalId == notafiscal.Id && x.StatusId == 1)
                                               .ToList();

                                foreach (var item_nf in itens_nf)
                                {
                                    item_nf.StatusId = 2;
                                    db.Entry(item_nf).State = EntityState.Modified;
                                }

                                db.SaveChanges();
                            }
                        }
                    }

                    if (!transito.Any())
                    {

                        Volume volume = new Volume();
                        volume.AreaId = areaid;
                        volume.NotaFiscalNr = nf;
                        volume.VolumeNr = "None";
                        volume.StatusId = 4;
                        volume.QtdItens = 0;
                        volume.Imprimir = false;
                        volume.Danfe = danfe;
                        volume.FilialId = filialId;
                        volume.CriadoEm = Util.GetCurrentDateTime();
                        volume.CriadoPor = Util.GetCurrentUser();

                        db.Volume.Add(volume);
                        db.SaveChanges();

                        db.Database.ExecuteSqlCommand("UPDATE NotaFiscalItem set StatusId = 2 WHERE FilialId = " + filialId + " AND StatusId = 1 AND NotaFiscalId = " + IdNF);
                        db.Database.ExecuteSqlCommand("UPDATE NotaFiscal set StatusId = 2 WHERE FilialId = " + filialId + " AND StatusId = 1 AND Id = " + IdNF);
                        db.SaveChanges();
                    }
                      
                    tr.Commit();
                }
                catch (Exception ex)
                {
                    tr.Rollback();

                    return Json(new { msg = ex.Message, erro = true });
                }
            }

            int rows_total = db.Volume.Where(x => x.AreaId == areaid && x.FilialId == filialId).Select(x => x.VolumeNr).Distinct().Count();
            int rows_pendente = db.Volume.Where(x => x.StatusId == 1 && x.AreaId == areaid && x.FilialId == filialId).Select(x => x.VolumeNr).Distinct().Count();

            return Json(new { msg = "Operação executada com sucesso", erro = false, total = rows_total, pendentes = rows_pendente });
        }

        // POST: Update Status do Volume na tabela Volume
        [HttpPost]
        public ActionResult UpdateVolume(string volume, int area)
        {
            int rows;
            int rows_pendente;
            int rows_conferido;
            int rows_incorreto;

            // Grava volume incorreto (??)
            int qtdevolume = db.Volume.Where(x => x.VolumeNr == volume && x.AreaId == area).Count();
            if (qtdevolume == 0)
            {
                Volume vol = new Volume();
                vol.NotaFiscalNr = string.Empty;
                vol.VolumeNr = volume;
                vol.StatusId = 3;
                vol.QtdItens = 0;
                vol.AreaId = area;
                vol.Imprimir = false;
                vol.Danfe = string.Empty;
                vol.FilialId = filialId;
                db.Volume.Add(vol);
                db.SaveChanges();

                rows = db.Volume.Where(x => x.StatusId != 3 && x.AreaId == area && x.FilialId == filialId).Select(x => x.VolumeNr).Distinct().Count();
                rows_pendente = db.Volume.Where(x => x.StatusId == 1 && x.AreaId == area && x.FilialId == filialId).Select(x => x.VolumeNr).Distinct().Count();
                rows_conferido = db.Volume.Where(x => x.StatusId == 2 && x.AreaId == area && x.FilialId == filialId).Select(x => x.VolumeNr).Distinct().Count();
                rows_incorreto = db.Volume.Where(x => x.StatusId == 3 && x.AreaId == area && x.FilialId == filialId).Select(x => x.VolumeNr).Distinct().Count();

                return Json(new { msg = "Volume Incorreto!", erro = true, notfound = true, total = rows, pendentes = rows_pendente, conferidos = rows_conferido, incorretos = rows_incorreto });

            }

            using (DbContextTransaction tr = db.Database.BeginTransaction())
            {
                try
                {
                    var volumes = db.Volume.Where(x => x.VolumeNr == volume && x.StatusId == 1 && x.AreaId == area).ToList();
                    if (volumes.Count() > 0)
                    {
                        foreach (var item in volumes)
                        {
                            item.StatusId = 2;
                            db.SaveChanges();
                        }
                    }

                    if (volume != "")
                    {
                        int StatusArea = 7;

                        //A informação Etiqueta abaixo deve vir da tabela Area
                        //Conforme Área escolhida na tela de conferência de volumes (Mobile)
                        //Será True ou False

                        bool imprimiretiqueta = false;

                        var _area = db.Area.Find(area);
                        if (_area != null)
                        {
                            imprimiretiqueta = _area.Etiqueta ?? false;
                            if (imprimiretiqueta)
                            {
                                StatusArea = 3;
                            }
                        }

                        try
                        {
                            db.Database.ExecuteSqlCommand("UPDATE NotaFiscalItem set StatusId = " + StatusArea + " WHERE Volume = '" + volume + "' && FilialId == " + filialId);
                            db.SaveChanges();
                        }
                        catch (Exception ex)
                        {
                            string erro = ex.Message;
                        }

                        int IdNF = (from i in db.Volume
                                    join x in db.NotaFiscal on i.NotaFiscalNr equals x.Numero
                                    where i.VolumeNr == volume && i.FilialId == filialId
                                    select x.Id).Distinct().FirstOrDefault();

                        //Se não houve mais volumes com status < 4 (Em Conferência)
                        //O status da NF será alterado para 7 (Finalizado)
                        //Posteriomente terá outros status, precisaremos avaliar como fazer

                        if (IdNF > 0)
                        {
                            int Volumes_pendente = db.NotaFiscalItem.Where(x => x.StatusId < 4 && x.NotaFiscalId == IdNF && x.FilialId == filialId).Select(x => x.Volume).Distinct().Count();
                            if (Volumes_pendente == 0)
                            {
                                db.Database.ExecuteSqlCommand("UPDATE NotaFiscal set StatusId = 7 WHERE id = " + IdNF + " && FilialId = " + filialId);
                                db.SaveChanges();
                            }

                            //Colocar mensagem de conferência de volumes finalizado
                            //Quando qtde pendentes = 0
                            //Não deve bloquear a leitura de mais volumes
                            if (db.Volume.Where(x => x.StatusId == 1 && x.AreaId == area).Select(x => x.VolumeNr).Distinct().Count() == 0)
                            {
                                db.Database.ExecuteSqlCommand("UPDATE NotaFiscal set StatusId = 7 WHERE id = " + IdNF + " && FilialId = " + filialId);
                                db.SaveChanges();
                            }
                        }
                    }

                    tr.Commit();

                }
                catch (Exception ex)
                {
                    tr.Rollback();

                    rows = db.Volume.Where(x => x.StatusId != 3 && x.AreaId == area && x.FilialId == filialId).Select(x => x.VolumeNr).Distinct().Count();
                    rows_pendente = db.Volume.Where(x => x.StatusId == 1 && x.AreaId == area && x.FilialId == filialId).Select(x => x.VolumeNr).Distinct().Count();
                    rows_conferido = db.Volume.Where(x => x.StatusId == 2 && x.AreaId == area && x.FilialId == filialId).Select(x => x.VolumeNr).Distinct().Count();
                    rows_incorreto = db.Volume.Where(x => x.StatusId == 3 && x.AreaId == area && x.FilialId == filialId).Select(x => x.VolumeNr).Distinct().Count();

                    return Json(new { msg = ex.Message, erro = true, notfound = false, total = rows, pendentes = rows_pendente, conferidos = rows_conferido, incorretos = rows_incorreto });
                }
            }

            rows = db.Volume.Where(x => x.StatusId != 3 && x.AreaId == area).Select(x => x.VolumeNr).Distinct().Count();
            rows_pendente = db.Volume.Where(x => x.StatusId == 1 && x.AreaId == area && x.FilialId == filialId).Select(x => x.VolumeNr).Distinct().Count();
            rows_conferido = db.Volume.Where(x => x.StatusId == 2 && x.AreaId == area && x.FilialId == filialId).Select(x => x.VolumeNr).Distinct().Count();
            rows_incorreto = db.Volume.Where(x => x.StatusId == 3 && x.AreaId == area && x.FilialId == filialId).Select(x => x.VolumeNr).Distinct().Count();

            if (rows_pendente == 0)
            {
                return Json(new { msg = "Conferência Finalizada!", finalizado = true, erro = false, total = rows, pendentes = rows_pendente, conferidos = rows_conferido, incorretos = rows_incorreto });
            }
            else
            {

                return Json(new { msg = "Operação executada com sucesso", finalizado = false, erro = false, total = rows, pendentes = rows_pendente, conferidos = rows_conferido, incorretos = rows_incorreto });

            }
        }


        // POST: ReiniciarVolume
        [HttpPost]
        public ActionResult ReiniciarVolume(int areaId)
        {
            using (DbContextTransaction tr = db.Database.BeginTransaction())
            {
                try
                {
                    var volumes = (from v in db.Volume
                                   where v.AreaId == areaId && v.FilialId == filialId
                                   select v).ToList();

                    db.Volume.RemoveRange(volumes);
                    db.SaveChanges();

                    tr.Commit();
                }
                catch (Exception ex)
                {
                    tr.Rollback();
                    return Json(new { msg = ex.Message, erro = true });
                }
            }

            return Json(new { msg = "Operação executada com sucesso", erro = false });
        }

        public ActionResult GetNotaFiscalByDanfe(string danfe)
        {
            NotaFiscal notafiscal = new NotaFiscal();

            try
            {
                notafiscal = db.NotaFiscal.Where(x => x.Danfe == danfe).FirstOrDefault();
                JsonResult result = Json(new { data = notafiscal, success = true, msg = string.Empty }, JsonRequestBehavior.AllowGet);
                return result;
            }
            catch (Exception ex)
            {
                JsonResult result = Json(new { data = notafiscal, success = false, msg = ex.Message }, JsonRequestBehavior.AllowGet);
                return result;
            }
        }

        public ActionResult Print()
        {
            PrintViewModel vm = new PrintViewModel();

            vm.ZPL_Volume = (from e in db.Etiqueta where e.Nome == "Volume" select e.ZPL).FirstOrDefault();
            vm.ZPL_Material = (from e in db.Etiqueta where e.Nome == "Material" select e.ZPL).FirstOrDefault();

            if (vm.ZPL_Volume == null || vm.ZPL_Material == null)
            {
                return HttpNotFound();
            }

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
                                     orderby (i.Nome == "RECEBIMENTO" ? 1 : 2)
                                     select new SelectListItem
                                     {
                                         Value = i.Id.ToString(),
                                         Text = i.Nome
                                     }).ToList();

            return View(vm);
        }

        // Retorna dados para impressão da etiqueta de volume
        public ActionResult GetMateriaisVolumeToPrint(string volume)
        {
            List<string> listaEtiquetas = new List<string>();
            string template_zpl = (from e in db.Etiqueta
                                   where e.Nome == "Volume" 
                                   select e.ZPL).FirstOrDefault();
            string zpl;
            try
            {
                var itens = (from nfi in db.NotaFiscalItem
                             join m in db.Material on nfi.Item equals m.Codigo
                             where nfi.Volume == volume && nfi.FilialId == filialId
                             select new EtiquetaRecebimentoViewModel
                             {
                                 Material = m.Codigo,
                                 Descricao = m.Descricao,
                                 Curva = m.Curva,
                                 Volume = nfi.Volume,
                                 Quantidade = nfi.Quantidade
                             }).ToList();

                var group_itens = (from m in itens
                                   group m by m.Material into g
                                   select new EtiquetaRecebimentoViewModel
                                   {
                                       Material = g.First().Material,
                                       Descricao = g.First().Descricao,
                                       Curva = g.First().Curva,
                                       Volume = g.First().Volume,
                                       Quantidade = g.Sum(x => x.Quantidade)
                                   }).ToList();

                foreach (var item in group_itens)
                {
                    string d1, d2, d3;
                    d1 = null;
                    d2 = null;
                    d3 = null;
                    var estoque = (from e in db.Estoque
                                   //join l in db.Locacao on e.Locacao equals l.Codigo
                                   where e.ItemNr == item.Material && e.FilialId == filialId //&& l.Tipo == "P"
                                   select e).FirstOrDefault();

                    if (estoque == null)
                    {
                        estoque = (from e in db.Estoque
                                   //join l in db.Locacao on e.Locacao equals l.Codigo
                                   where e.ItemNr == item.Material && e.FilialId == filialId //&& l.Tipo == "S" 
                                   select e).FirstOrDefault();

                        if (estoque == null)
                        {
                            item.Locacao = string.Empty;
                        }
                        else
                        {
                            item.Locacao = estoque.Locacao;
                        }
                    }
                    else
                    {
                        item.Locacao = estoque.Locacao;
                    }

                    DateTime dt = Util.GetCurrentDateTime();
                    item.Data = dt.ToString("dd/MM/yyyy");
                    item.Hora = dt.ToString("HH:mm:ss");

                    zpl = template_zpl;
                    zpl = zpl.Replace("codigo-item", item.Material);
                    zpl = zpl.Replace("descricao-item", item.Descricao);
                    zpl = zpl.Replace("codigo-curva", item.Curva);
                    zpl = zpl.Replace("numero-volume", item.Volume);
                    zpl = zpl.Replace("qtd-item", item.Quantidade.ToString("N0"));
                    zpl = zpl.Replace("data-impressao", item.Data);
                    zpl = zpl.Replace("hora-impressao", item.Hora);

                    string saldo_estoque = string.Empty;
                    try
                    {
                        saldo_estoque = estoque.Saldo.ToString();
                    }
                    catch (Exception)
                    {
                        saldo_estoque = "0";
                    }

                    zpl = zpl.Replace("saldo-estoque", saldo_estoque);

                    //Arrumar como deve ser o layout a locação
                    if (item.Locacao.Length < 9)
                    {
                        d1 = item.Locacao;
                    }

                    if (item.Locacao.Length == 9)
                    {
                        d1 = item.Locacao.Substring(0, 8);
                        d2 = item.Locacao.Substring(8, 1);
                    }

                    if (item.Locacao.Length == 10)
                    {
                        string espaco = item.Locacao.Substring(6, 1);

                        if (espaco != " ")
                        {
                            d1 = item.Locacao.Substring(0, 5);
                            d2 = item.Locacao.Substring(6, 2);
                            d3 = item.Locacao.Substring(8, 2);
                        }
                        else
                        {
                            d1 = item.Locacao.Substring(0, 6);
                            d2 = item.Locacao.Substring(7, 2);
                            d3 = item.Locacao.Substring(9, 1);
                        }

                    }

                    if (item.Locacao.Length == 11)
                    {
                        d1 = item.Locacao.Substring(0, 9);
                        d2 = item.Locacao.Substring(9, 2);
                    }

                    if (d3 != null)
                    {
                        d3 = " " + d3;
                    }

                    string locAcertada = d1 + " " + d2 + d3;

                    zpl = zpl.Replace("codigo-locacao", locAcertada);

                    listaEtiquetas.Add(Util.RemoverAcentuacao(zpl));

                    // Gravar histórico
                    try
                    {
                        HistoricoRecebimento historico = new HistoricoRecebimento();
                        historico.CodMaterial = item.Material;
                        historico.DescMaterial = item.Descricao;
                        historico.Curva = item.Curva;
                        historico.CodLocacao = item.Locacao;
                        historico.NroVolume = item.Volume;
                        historico.Quantidade = item.Quantidade;
                        historico.DataHora = dt;
                        historico.Usuario = Util.GetCurrentUser();
                        historico.FilialId = filialId;
                        db.HistoricoRecebimento.Add(historico);
                        db.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        try
                        {
                            AppLogErro erro = new AppLogErro();
                            erro.Area = "Recebimento";
                            erro.Controller = "NotaFiscal";
                            erro.Action = "GetMateriaisVolumeToPrint";
                            erro.Instrucao = "Gravar log de impressão (HistoricoRecebimento)";
                            erro.ErrorCode = string.Empty;
                            erro.ErrorMessage = ex.Message;
                            erro.Usuario = Util.GetCurrentUser();
                            erro.FilialId = filialId;
                            erro.DataHora = dt;
                            db.AppLogErro.Add(erro);
                            db.SaveChanges();
                        }
                        catch (Exception)
                        {
                        }
                    }

                    db.Database.ExecuteSqlCommand("UPDATE NotaFiscalItem set StatusId = 4 WHERE FilialId = " + filialId + " && StatusId = 3 AND Volume = '" + item.Volume + "'");
                    db.SaveChanges();

                }

                JsonResult result = Json(new { data = listaEtiquetas, success = true, msg = string.Empty }, JsonRequestBehavior.AllowGet);
                result.MaxJsonLength = int.MaxValue;
                return result;

            }
            catch (Exception ex)
            {
                return Json(new { success = false, msg = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // Retorna dados para impressão da etiqueta de material
        public ActionResult GetMaterialToPrint(string material, int quantidadeImpressao)
        {
            EtiquetaMaterialViewModel result = new EtiquetaMaterialViewModel();
            List<string> listaEtiquetas = new List<string>();
            string template_zpl = (from e in db.Etiqueta
                                   where e.Nome == "Material"
                                   select e.ZPL).FirstOrDefault();

            string zpl;

            try
            {
                if (quantidadeImpressao < 0)
                {
                    return Json(new { data = result, success = false, msg = "A quantidade de impressão precisa ser maior que 0!" }, JsonRequestBehavior.AllowGet);
                }

                string d1, d2, d3;
                d1 = null;
                d2 = null;
                d3 = null;

                var item = (from m in db.Material
                            where m.Codigo == material
                            select m).FirstOrDefault();

                if (item != null)
                {
                    result.Material = item.Codigo;
                    result.Descricao = item.Descricao;
                    result.Curva = item.Curva;
                    result.Locacao = string.Empty;
                }
                else if (item == null)
                {

                    return Json(new { data = result, success = false, msg = "Material não localizado" }, JsonRequestBehavior.AllowGet);
                }

                var estoque = (from e in db.Estoque                               
                               where e.ItemNr == item.Codigo && e.FilialId == filialId
                               select e).Distinct().FirstOrDefault();

                DateTime dt = Util.GetCurrentDateTime();
                result.Data = dt.ToString("dd/MM/yyyy");
                result.Hora = dt.ToString("HH:mm:ss");

                zpl = template_zpl;
                zpl = zpl.Replace("codigo-item", result.Material);
                zpl = zpl.Replace("descricao-item", result.Descricao);
                zpl = zpl.Replace("codigo-curva", result.Curva);
                zpl = zpl.Replace("data-impressao", result.Data);
                zpl = zpl.Replace("hora-impressao", result.Hora);

                string saldo_estoque = string.Empty;
                try
                {
                    saldo_estoque = estoque.Saldo.ToString();
                }
                catch (Exception)
                {
                    saldo_estoque = "0";
                }

                zpl = zpl.Replace("saldo-estoque", saldo_estoque);

                string locAcertada = "";

                if (saldo_estoque != null)
                {

                    if (estoque.Locacao.Length < 9)
                    {
                        d1 = estoque.Locacao;
                    }

                    if (estoque.Locacao.Length == 9)
                    {
                        d1 = estoque.Locacao.Substring(0, 8);
                        d2 = estoque.Locacao.Substring(8, 1);
                    }

                    if (estoque.Locacao.Length == 10)
                    {
                        string espaco = estoque.Locacao.Substring(6, 1);

                        if (espaco != " ")
                        {
                            d1 = estoque.Locacao.Substring(0, 5);
                            d2 = estoque.Locacao.Substring(6, 2);
                            d3 = estoque.Locacao.Substring(8, 2);
                        }
                        else
                        {
                            d1 = estoque.Locacao.Substring(0, 6);
                            d2 = estoque.Locacao.Substring(7, 2);
                            d3 = estoque.Locacao.Substring(9, 1);
                        }

                    }

                    if (estoque.Locacao.Length == 11)
                    {
                        d1 = estoque.Locacao.Substring(0, 9);
                        d2 = estoque.Locacao.Substring(9, 2);
                    }

                    if (d3 != null)
                    {
                        d3 = " " + d3;
                    }

                    locAcertada = d1 + " " + d2 + d3;
                }
                else
                {
                    return Json(new { data = result, success = false, msg = "Item Nr não cadastrado!" }, JsonRequestBehavior.AllowGet);
                }

                result.Locacao = locAcertada;
                zpl = zpl.Replace("codigo-locacao", locAcertada);

                for (int i = 0; i < quantidadeImpressao; i++)
                {
                    listaEtiquetas.Add(Util.RemoverAcentuacao(zpl));
                }

                // Gravar histórico
                try
                {
                    HistoricoRecebimento historico = new HistoricoRecebimento();
                    historico.CodMaterial = result.Material;
                    historico.DescMaterial = result.Descricao;
                    historico.Curva = result.Curva;
                    historico.CodLocacao = result.Locacao;
                    historico.NroVolume = "Por Item Nr";
                    historico.Quantidade = quantidadeImpressao;
                    historico.DataHora = dt;
                    historico.Usuario = Util.GetCurrentUser();
                    historico.FilialId = filialId;
                    db.HistoricoRecebimento.Add(historico);
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    try
                    {
                        AppLogErro erro = new AppLogErro();
                        erro.Area = "Recebimento";
                        erro.Controller = "NotaFiscal";
                        erro.Action = "GetMaterialToPrint";
                        erro.Instrucao = "Gravar log de impressão (HistoricoRecebimento)";
                        erro.ErrorCode = string.Empty;
                        erro.ErrorMessage = ex.Message;
                        erro.Usuario = Util.GetCurrentUser();
                        erro.FilialId = filialId;
                        erro.DataHora = dt;
                        db.AppLogErro.Add(erro);
                        db.SaveChanges();
                    }
                    catch (Exception)
                    {
                    }
                }

                JsonResult jsonResult = Json(new { data = listaEtiquetas, success = true, msg = string.Empty }, JsonRequestBehavior.AllowGet);
                jsonResult.MaxJsonLength = int.MaxValue;
                return jsonResult;
            }
            catch (Exception ex)
            {
                return Json(new { success = false, msg = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // GET: Recebimento/NotaFiscal/Historico
        public ActionResult Historico()
        {
            return View();
        }

        [HttpPost]
        public ActionResult GetDataHistorico()
        {
            using (var reader = new StreamReader(Request.InputStream))

            {
                var json = reader.ReadToEnd();
                var model = JsonConvert.DeserializeObject<DataTableAjaxPostModel>(json);

                var draw = model.draw;
                var start = model.start;
                var length = model.length;
                var searchValue = model.search.value;
                var sortColumn = model.order[0].column;
                var sortColumnDir = model.order[0].dir;

                var historico = (from h in db.HistoricoRecebimento
                                 where h.FilialId == filialId && h.DataHora >= inicio
                                 select new HistoricoViewModel
                                 {
                                     Id = h.Id,
                                     CodMaterial = h.CodMaterial,
                                     DescMaterial = h.DescMaterial,
                                     Curva = h.Curva,
                                     CodLocacao = h.CodLocacao,
                                     NroVolume = h.NroVolume,
                                     Quantidade = h.Quantidade,
                                     DataHora = h.DataHora,
                                     Usuario = h.Usuario
                                 }).ToList();

                var recordsTotal = historico.Count();

                // Filtragem
                if (!string.IsNullOrEmpty(searchValue))
                {
                    historico = historico.Where(m => (m.CodMaterial ?? string.Empty).ToLower().Contains(searchValue.ToLower()) ||
                                                     (m.DescMaterial ?? string.Empty).ToLower().Contains(searchValue.ToLower()) ||
                                                     (m.Curva ?? string.Empty).ToLower().Contains(searchValue.ToLower()) ||
                                                     (m.CodLocacao ?? string.Empty).ToLower().Contains(searchValue.ToLower()) ||
                                                     (m.NroVolume ?? string.Empty).ToLower().Contains(searchValue.ToLower()) ||
                                                     (m.DataHora.ToString().Contains(searchValue.ToLower())) ||
                                                     (m.Usuario ?? string.Empty).ToLower().Contains(searchValue.ToLower())).ToList();
                }

                // Ordenação
                switch (sortColumn)
                {
                    case 0:
                        historico = sortColumnDir == "desc" ? historico.OrderByDescending(c => c.CodMaterial).ToList() : historico.OrderBy(c => c.CodMaterial).ToList();
                        break;
                    case 1:
                        historico = sortColumnDir == "desc" ? historico.OrderByDescending(c => c.DescMaterial).ToList() : historico.OrderBy(c => c.DescMaterial).ToList();
                        break;
                    case 2:
                        historico = sortColumnDir == "desc" ? historico.OrderByDescending(c => c.Curva).ToList() : historico.OrderBy(c => c.Curva).ToList();
                        break;
                    case 3:
                        historico = sortColumnDir == "desc" ? historico.OrderByDescending(c => c.CodLocacao).ToList() : historico.OrderBy(c => c.CodLocacao).ToList();
                        break;
                    case 4:
                        historico = sortColumnDir == "desc" ? historico.OrderByDescending(c => c.NroVolume).ToList() : historico.OrderBy(c => c.NroVolume).ToList();
                        break;
                    case 5:
                        historico = sortColumnDir == "desc" ? historico.OrderByDescending(c => c.Quantidade).ToList() : historico.OrderBy(c => c.Quantidade).ToList();
                        break;
                    case 6:
                        historico = sortColumnDir == "desc" ? historico.OrderByDescending(c => c.DataHora).ToList() : historico.OrderBy(c => c.DataHora).ToList();
                        break;
                    case 7:
                        historico = sortColumnDir == "desc" ? historico.OrderByDescending(c => c.Usuario).ToList() : historico.OrderBy(c => c.Usuario).ToList();
                        break;
                }

                var filteredData = historico.Skip(start).Take(length).ToList();
                var result = new { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = filteredData };

                return Json(result, JsonRequestBehavior.AllowGet);
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
    }
}