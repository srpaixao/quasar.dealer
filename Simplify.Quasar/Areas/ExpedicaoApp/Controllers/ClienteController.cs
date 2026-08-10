using System;
using System.Linq;
using System.Web.Mvc;

using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Data.Entity.Infrastructure;

using Simplify.Quasar.Models;
using Simplify.Quasar.Custom;
using Simplify.Quasar.Areas.ExpedicaoApp.ViewModels;
using Microsoft.Reporting.WebForms;
using System.IO;

namespace Simplify.Quasar.Areas.ExpedicaoApp.Controllers
{
    [ValidateSession]
    public class ClienteController : Controller
    {
        Quasar_Entities db = new Quasar_Entities();
        //public int? TransportadoraId { get; private set; }

        // GET: Cliente/Index
        public ActionResult Index()
        {
            var vm = (from u in db.Cliente
                      select new ClienteViewModel
                      {
                          Id = u.Id,
                          Nome = u.Nome,
                          CNPJ = u.CNPJ,
                          Endereco_Cidade = u.Endereco_Cidade,
                          Endereco_UF = u.Endereco_UF,
                          IdVendedor = u.VendedorId,
                          //TransportadoraId = TransportadoraId, //(from t in db.Transportadora where t.Id == u.TransportadoraId select t.Nome).FirstOrDefault(),
                          NomeRota = (from r in db.Rota where r.Id == u.RotaId select r.Nome).FirstOrDefault(),
                          NomeParada = (from p in db.Parada where p.Id == u.ParadaId select p.Nome).FirstOrDefault()
                      }).ToList();

            foreach (var item in vm.Where(x => x.CNPJ != null && x.CNPJ != string.Empty))
            {
                var cnpjNoFormat = Util.SemFormatacao(item.CNPJ);
                item.CNPJ = Util.FormatCNPJ(cnpjNoFormat);
                
            }

            // Obtem lista de permissões mostra botão para criar/alterar/excluir ou não
            ViewBag.Permissoes = Util.GetPermissoes(ControllerContext.RouteData.Values["controller"].ToString());

            return View(vm);
        }

        // GET: Cliente/Create
        public ActionResult Create()
        {
            ClienteViewModel vm = new ClienteViewModel();

            vm.EstadoDDL = Util.GetEstadoDDL(null);

            return PartialView("_Create", vm);
        }

        // POST: Cliente/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(ClienteViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                vm.EstadoDDL = Util.GetEstadoDDL(vm.Endereco_UF);
                return PartialView("_Create", vm);
            }

            string cnpj = Util.SemFormatacao(vm.CNPJ);

            // Verifica se o CNPJ informado é valido
            if (!Util.IsValid(cnpj) || cnpj == "00000000000000")
            {
                vm.EstadoDDL = Util.GetEstadoDDL(vm.Endereco_UF);
                ModelState.AddModelError("CNPJ", "O CNPJ informado não é válido");
                return PartialView("_Create", vm);
            }

            // Verifica se a Cliente já existe 
            if (db.Cliente.Any(p => p.CNPJ == cnpj))
            {
                vm.EstadoDDL = Util.GetEstadoDDL(vm.Endereco_UF);
                ModelState.AddModelError("CNPJ", "Já existe um cliente cadastrada com este CNPJ");
                return PartialView("_Create", vm);
            }

            //Verificar se o Cliente já existe em casos que o CNPJ é salvo com os . ou /

            if (db.Cliente.Any(p => p.CNPJ == vm.CNPJ))
            {
                vm.EstadoDDL = Util.GetEstadoDDL(vm.Endereco_UF);
                ModelState.AddModelError("CNPJ", "Já existe um cliente cadastrada com este CNPJ");
                return PartialView("_Create", vm);
            }

            Cliente cliente = new Cliente();
            cliente.Nome = vm.Nome;
            cliente.CNPJ = cnpj;
            cliente.Endereco_Logradouro = vm.Endereco_Logradouro;
            cliente.Endereco_Numero = vm.Endereco_Numero;
            cliente.Endereco_Complemento = vm.Endereco_Complemento;
            cliente.Endereco_Bairro = vm.Endereco_Bairro;
            cliente.Endereco_Cidade = vm.Endereco_Cidade;
            cliente.Endereco_UF = vm.Endereco_UF;
            cliente.Endereco_CEP = vm.Endereco_CEP;
            cliente.Telefone1 = vm.Telefone1;
            //cliente.TransportadoraId = vm.TransportadoraId;
            cliente.RotaId = vm.RotaId;
            cliente.ParadaId = vm.ParadaId;

            cliente.CriadoPor = Util.GetCurrentUser();
            cliente.CriadoEm = Util.GetCurrentDateTime();

            ViewBag.ControllerName = ControllerContext.RouteData.Values["controller"].ToString();
            ViewBag.ActionName = ControllerContext.RouteData.Values["action"].ToString();

            using (DbContextTransaction tr = db.Database.BeginTransaction())
            {
                try
                {
                    db.Cliente.Add(cliente);
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
                    return PartialView("_Create", vm);
                }
                catch (Exception ex)
                {
                    ViewBag.Exception = ex.Message;
                    ViewBag.InnerException = ex.InnerException;
                    ViewBag.Source = ex.Source;
                    tr.Rollback();
                    return PartialView("_Create", vm);
                }
            }

            return Json(new { success = true });
        }

        // GET: Cliente/Edit
        public ActionResult Edit(int id)
        {
            Cliente cliente = db.Cliente.Find(id);
            if (cliente == null)
            {
                return HttpNotFound();
            }

            ClienteViewModel vm = new ClienteViewModel();
            if(cliente.CNPJ == null)
            {
                cliente.CNPJ = "00000000000000";
            }

            var cnpjNoFormat = Util.SemFormatacao(cliente.CNPJ); //tira formatação dos CNPJs salvos com . e /

            vm.Id = cliente.Id;
            vm.Nome = cliente.Nome;
            vm.CNPJ = Util.FormatCNPJ(cnpjNoFormat);
            vm.Endereco_Logradouro = cliente.Endereco_Logradouro;
            vm.Endereco_Numero = cliente.Endereco_Numero;
            vm.Endereco_Complemento = cliente.Endereco_Complemento;
            vm.Endereco_Bairro = cliente.Endereco_Bairro;
            vm.Endereco_Cidade = cliente.Endereco_Cidade;
            vm.Endereco_UF = cliente.Endereco_UF;
            vm.EstadoDDL = Util.GetEstadoDDL(cliente.Endereco_UF);
            vm.Endereco_CEP = cliente.Endereco_CEP;
            vm.Telefone1 = cliente.Telefone1;
            vm.RotaId = cliente.RotaId;
            vm.ParadaId = cliente.ParadaId;
            //vm.TransportadoraId = cliente.TransportadoraId;
            vm.CriadoEm = cliente.CriadoEm;
            vm.CriadoPor = cliente.CriadoPor;
            vm.CriadoPorNome = (from u in db.Usuario where u.Login == cliente.CriadoPor select u.Nome).FirstOrDefault();
            vm.ModificadoEm = cliente.ModificadoEm;
            vm.ModificadoPor = cliente.ModificadoPor;
            vm.ModificadoPorNome = (from u in db.Usuario where u.Login == cliente.ModificadoPor select u.Nome).FirstOrDefault();

            return PartialView("_Edit", vm);
        }

        // POST: Cliente/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(ClienteViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                vm.EstadoDDL = Util.GetEstadoDDL(vm.Endereco_UF);
                return PartialView("_Edit", vm);
            }

            Cliente cliente = db.Cliente.Find(vm.Id);
            if (cliente == null)
            {
                return HttpNotFound();
            }

            string cnpj = Util.SemFormatacao(vm.CNPJ);

            // Verifica se o CNPJ informado é valido
            if (!Util.IsValid(cnpj) || cnpj == "00000000000000")
            {
                vm.EstadoDDL = Util.GetEstadoDDL(vm.Endereco_UF);
                ModelState.AddModelError("CNPJ", "O CNPJ informado não é válido");
                return PartialView("_Edit", vm);
            }


            // Verifica se a Cliente já existe
            if (cliente.CNPJ != cnpj)
            {
                if(cliente.CNPJ != vm.CNPJ) //verifica se o cnpj com . ou / está salvo no db
                {
                    if (db.Cliente.Any(p => p.CNPJ == cnpj))
                    {
                        vm.EstadoDDL = Util.GetEstadoDDL(vm.Endereco_UF);
                        ModelState.AddModelError("CNPJ", "Já existe um cliente cadastrado com este CNPJ");
                        return PartialView("_Edit", vm);
                    }

                    //Verificar se o Cliente já existe em casos que o CNPJ é salvo com os . ou /

                    if (db.Cliente.Any(p => p.CNPJ == vm.CNPJ))
                    {
                        vm.EstadoDDL = Util.GetEstadoDDL(vm.Endereco_UF);
                        ModelState.AddModelError("CNPJ", "Já existe um cliente cadastrada com este CNPJ");
                        return PartialView("_Create", vm);
                    }
                }
                

            }

            cliente.Nome = vm.Nome;
            cliente.CNPJ = cnpj;
            cliente.Endereco_Logradouro = vm.Endereco_Logradouro;
            cliente.Endereco_Numero = vm.Endereco_Numero;
            cliente.Endereco_Complemento = vm.Endereco_Complemento;
            cliente.Endereco_Bairro = vm.Endereco_Bairro;
            cliente.Endereco_Cidade = vm.Endereco_Cidade;
            cliente.Endereco_UF = vm.Endereco_UF;
            cliente.Endereco_CEP = vm.Endereco_CEP;
            cliente.Telefone1 = vm.Telefone1;
            //cliente.TransportadoraId = vm.TransportadoraId;
            cliente.RotaId = vm.RotaId;
            cliente.ParadaId = vm.ParadaId;

            cliente.ModificadoPor = Util.GetCurrentUser();
            cliente.ModificadoEm = Util.GetCurrentDateTime();

            db.Entry(cliente).State = EntityState.Modified;

            ViewBag.ControllerName = ControllerContext.RouteData.Values["controller"].ToString();
            ViewBag.ActionName = ControllerContext.RouteData.Values["action"].ToString();

            using (DbContextTransaction tr = db.Database.BeginTransaction())
            {
                try
                {
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
                    return PartialView("_Edit", vm);
                }
                catch (Exception ex)
                {
                    ViewBag.Exception = ex.Message;
                    ViewBag.InnerException = ex.InnerException;
                    ViewBag.Source = ex.Source;
                    tr.Rollback();
                    return PartialView("_Edit", vm);
                }
            }

            return Json(new { success = true });
        }


        // POST: Cliente/Delete (chamada AJAX)
        [HttpPost]
        public ActionResult Delete(int id)
        {
            Cliente cliente = db.Cliente.Find(id);
            if (cliente == null)
            {
                return Json(new { success = false, msg = "Cliente não encontrado!" });
            }

            using (DbContextTransaction tr = db.Database.BeginTransaction())
            {
                try
                {
                    db.Cliente.Remove(cliente);
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

            return Json(new { success = true, msg = "Operação realizada com sucesso" });
        }

        public JsonResult GetClientes(string search)
        {
            var result = (from f in db.Cliente
                          where f.Nome.ToLower().Contains(search.ToLower())
                          orderby f.Nome
                          select new
                          {
                              id = f.Id,
                              text = f.Nome
                          }).ToList();

            return Json(new { items = result }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult ImprimirLista()
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

            var dadostreinamento = db.Cliente.ToList();
            //var dadostreinamento = db.Treinamento.Where(x => x.Id == id).ToList(); //// -----> Escrever query
            //ReportParameter[] parameters = new ReportParameter[];
            //parameters[0] = new ReportParameter("Posto", posto);
            //parameters[1] = new ReportParameter("DataInicio", datainicio);
            //parameters[2] = new ReportParameter("DataTermino", datatermino);
            //parameters[3] = new ReportParameter("Item", item);
            //parameters[4] = new ReportParameter("Cadencia", Math.Round(cadencia).ToString("N0"));
            //parameters[] = new ReportParameter("Dias", dias.ToString());
            //lr.SetParameters(new ReportParameter[] { param });

            ReportDataSource rd = new ReportDataSource("DataSet1", dadostreinamento);
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


        public JsonResult ClienteLookup(string query)
        {
            var clientes = (from t in db.Cliente
                         where t.Nome.ToUpper().Contains(query.ToUpper()) ||
                               t.CodigoDMS.ToUpper().Contains(query.ToUpper()) ||
                               t.CNPJ.ToUpper().Contains(query.ToUpper()) ||
                               t.Endereco_Cidade.ToUpper().Contains(query.ToUpper()) || 
                               t.Endereco_UF.ToUpper().Contains(query.ToUpper()) 
                            select new 
                         {
                             Id = t.Id,
                             Nome = t.Nome + " - " + t.Endereco_Cidade + "/" + t.Endereco_UF
                         }).ToList();

            return Json(new { total_results = clientes.Count, results = clientes }, JsonRequestBehavior.AllowGet);

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