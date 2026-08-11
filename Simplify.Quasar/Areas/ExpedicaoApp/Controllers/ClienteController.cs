using System;
using System.Linq;
using System.Collections.Generic;
using System.Web.Mvc;

using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Data.Entity.Infrastructure;

using Simplify.Quasar.Models;
using Simplify.Quasar.Custom;
using Simplify.Quasar.Areas.ExpedicaoApp.ViewModels;
using Microsoft.Reporting.WebForms;
using System.IO;
using Newtonsoft.Json;

namespace Simplify.Quasar.Areas.ExpedicaoApp.Controllers
{
    [ValidateSession]
    public class ClienteController : Controller
    {
        Quasar_Entities db = new Quasar_Entities();

        int filialId = Util.GetCurrentFilial();

        // GET: Cliente/Index
        public ActionResult Index()
        {
            // Obtem lista de permissões mostra botão para criar/alterar/excluir ou não
            ViewBag.Permissoes = Util.GetPermissoes(ControllerContext.RouteData.Values["controller"].ToString(), ControllerContext.RouteData.DataTokens["area"] as string);

            return View();
        }

        [HttpPost]
        public ActionResult GetData()
        {
            DataTableAjaxPostModel model;
            using (var reader = new StreamReader(Request.InputStream))
            {
                model = JsonConvert.DeserializeObject<DataTableAjaxPostModel>(reader.ReadToEnd());
            }

            if (model == null)
            {
                return Json(new { draw = 0, recordsFiltered = 0, recordsTotal = 0, data = new object[0] });
            }

            IQueryable<ClienteViewModel> query =
                from cliente in db.Cliente.AsNoTracking()
                join transportadora in db.Transportadora.AsNoTracking()
                    on cliente.TransportadoraId equals (int?)transportadora.Id into transportadoras
                from transportadora in transportadoras.DefaultIfEmpty()
                join rota in db.Rota.AsNoTracking()
                    on cliente.RotaId equals (int?)rota.Id into rotas
                from rota in rotas.DefaultIfEmpty()
                join parada in db.Parada.AsNoTracking()
                    on cliente.ParadaId equals (int?)parada.Id into paradas
                from parada in paradas.DefaultIfEmpty()
                select new ClienteViewModel
                {
                    Id = cliente.Id,
                    Nome = cliente.Nome,
                    CNPJ = cliente.CNPJ,
                    Endereco_Cidade = cliente.Endereco_Cidade,
                    Endereco_UF = cliente.Endereco_UF,
                    Etiqueta = cliente.Etiqueta == true,
                    NomeTransportadora = transportadora.Nome,
                    NomeRota = rota.Nome,
                    NomeParada = parada.Nome
                };

            int recordsTotal = db.Cliente.AsNoTracking().Count();
            string termo = model.search == null
                ? string.Empty
                : (model.search.value ?? string.Empty).Trim();

            if (!string.IsNullOrWhiteSpace(termo))
            {
                string cnpj = new string(termo.Where(char.IsDigit).ToArray());
                string termoEtiqueta = termo.ToLower();
                bool pesquisarEtiquetaSim = termoEtiqueta == "sim";
                bool pesquisarEtiquetaNao = termoEtiqueta == "nao" || termoEtiqueta == "não";

                query = query.Where(x =>
                    (x.Nome ?? string.Empty).Contains(termo) ||
                    (x.Endereco_Cidade ?? string.Empty).Contains(termo) ||
                    (x.Endereco_UF ?? string.Empty).Contains(termo) ||
                    (x.NomeTransportadora ?? string.Empty).Contains(termo) ||
                    (x.NomeRota ?? string.Empty).Contains(termo) ||
                    (x.NomeParada ?? string.Empty).Contains(termo) ||
                    (x.CNPJ ?? string.Empty).Contains(termo) ||
                    (!string.IsNullOrEmpty(cnpj) && (x.CNPJ ?? string.Empty).Contains(cnpj)) ||
                    (pesquisarEtiquetaSim && x.Etiqueta) ||
                    (pesquisarEtiquetaNao && !x.Etiqueta));
            }

            int recordsFiltered = string.IsNullOrWhiteSpace(termo)
                ? recordsTotal
                : query.Count();
            int sortColumn = model.order != null && model.order.Length > 0
                ? model.order[0].column
                : 0;
            bool desc = model.order != null &&
                        model.order.Length > 0 &&
                        model.order[0].dir == "desc";

            IOrderedQueryable<ClienteViewModel> orderedQuery;
            switch (sortColumn)
            {
                case 1:
                    orderedQuery = desc ? query.OrderByDescending(x => x.Endereco_Cidade) : query.OrderBy(x => x.Endereco_Cidade);
                    break;
                case 2:
                    orderedQuery = desc ? query.OrderByDescending(x => x.Endereco_UF) : query.OrderBy(x => x.Endereco_UF);
                    break;
                case 3:
                    orderedQuery = desc ? query.OrderByDescending(x => x.NomeTransportadora) : query.OrderBy(x => x.NomeTransportadora);
                    break;
                case 4:
                    orderedQuery = desc ? query.OrderByDescending(x => x.NomeRota) : query.OrderBy(x => x.NomeRota);
                    break;
                case 5:
                    orderedQuery = desc ? query.OrderByDescending(x => x.NomeParada) : query.OrderBy(x => x.NomeParada);
                    break;
                case 6:
                    orderedQuery = desc ? query.OrderByDescending(x => x.Etiqueta) : query.OrderBy(x => x.Etiqueta);
                    break;
                case 7:
                    orderedQuery = desc ? query.OrderByDescending(x => x.CNPJ) : query.OrderBy(x => x.CNPJ);
                    break;
                default:
                    orderedQuery = desc ? query.OrderByDescending(x => x.Nome) : query.OrderBy(x => x.Nome);
                    break;
            }

            int length = model.length > 0 ? model.length : 25;
            int start = model.start > 0 ? model.start : 0;
            var clientes = orderedQuery
                .ThenBy(x => x.Id)
                .Skip(start)
                .Take(length)
                .ToList();

            foreach (ClienteViewModel cliente in clientes.Where(x => !string.IsNullOrWhiteSpace(x.CNPJ)))
            {
                cliente.CNPJ = FormatDocumento(cliente.CNPJ);
            }

            JsonResult result = Json(new
            {
                draw = model.draw,
                recordsFiltered,
                recordsTotal,
                data = clientes
            });
            result.MaxJsonLength = int.MaxValue;
            return result;
        }

        private IEnumerable<SelectListItem> GetTransportadoraClienteDDL(int? selectedId)
        {
            var transportadoras = db.Transportadora
                .AsNoTracking()
                .Where(x => x.FilialId == filialId)
                .Select(x => new
                {
                    x.Id,
                    x.Nome,
                    x.Nome_Fantasia,
                    Cidade = x.Endereco_Cidade,
                    x.CNPJ
                })
                .ToList();

            return transportadoras
                .Select(x =>
                {
                    string nome = !string.IsNullOrWhiteSpace(x.Nome_Fantasia)
                        ? x.Nome_Fantasia
                        : x.Nome;
                    string documento = FormatDocumento(x.CNPJ);
                    string texto = string.Join(" - ", new[] { nome, x.Cidade, documento }
                        .Where(item => !string.IsNullOrWhiteSpace(item)));

                    return new SelectListItem
                    {
                        Value = x.Id.ToString(),
                        Text = texto,
                        Selected = x.Id == selectedId
                    };
                })
                .OrderBy(x => x.Text)
                .ToList();
        }

        private static string NormalizarDocumento(string documento)
        {
            return string.IsNullOrWhiteSpace(documento)
                ? null
                : documento.Trim()
                    .Replace(".", string.Empty)
                    .Replace("-", string.Empty)
                    .Replace("/", string.Empty)
                    .Replace(" ", string.Empty);
        }

        private static bool DocumentoValido(string documento)
        {
            return !string.IsNullOrWhiteSpace(documento) &&
                   (documento.Length == 11 || documento.Length == 14) &&
                   documento.All(char.IsDigit) &&
                   Util.IsValid(documento);
        }

        private static string FormatDocumento(string documento)
        {
            string normalizado = NormalizarDocumento(documento);
            if (string.IsNullOrWhiteSpace(normalizado) || !normalizado.All(char.IsDigit))
            {
                return documento ?? string.Empty;
            }

            if (normalizado.Length == 11)
            {
                return Util.FormatCPF(normalizado);
            }

            return normalizado.Length == 14
                ? Util.FormatCNPJ(normalizado)
                : documento;
        }

        // GET: Cliente/Create
        public ActionResult Create()
        {
            ClienteViewModel vm = new ClienteViewModel();
            vm.Etiqueta = true;

            vm.EstadoDDL = Util.GetEstadoDDL(null);
            vm.TransportadoraDDL = GetTransportadoraClienteDDL(null);
            vm.RotaDDL = Util.GetRotaDDL(filialId, null);
            vm.ParadaDDL = Util.GetParadaDDL(filialId, null);

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
                vm.TransportadoraDDL = GetTransportadoraClienteDDL(vm.TransportadoraId);
                vm.RotaDDL = Util.GetRotaDDL(filialId, vm.RotaId);
                vm.ParadaDDL = Util.GetParadaDDL(filialId, vm.ParadaId);
                return PartialView("_Create", vm);
            }

            string documento = NormalizarDocumento(vm.CNPJ);

            // CPF/CNPJ e opcional, mas deve ser valido quando informado.
            if (!string.IsNullOrWhiteSpace(documento) && !DocumentoValido(documento))
            {
                vm.EstadoDDL = Util.GetEstadoDDL(vm.Endereco_UF);
                vm.TransportadoraDDL = GetTransportadoraClienteDDL(vm.TransportadoraId);
                vm.RotaDDL = Util.GetRotaDDL(filialId, vm.RotaId);
                vm.ParadaDDL = Util.GetParadaDDL(filialId, vm.ParadaId);
                ModelState.AddModelError("CNPJ", "O CPF/CNPJ informado não é válido");
                return PartialView("_Create", vm);
            }

            if (!string.IsNullOrWhiteSpace(documento) &&
                db.Cliente.Any(p => p.CNPJ == documento || p.CNPJ == vm.CNPJ))
            {
                vm.EstadoDDL = Util.GetEstadoDDL(vm.Endereco_UF);
                vm.TransportadoraDDL = GetTransportadoraClienteDDL(vm.TransportadoraId);
                vm.RotaDDL = Util.GetRotaDDL(filialId, vm.RotaId);
                vm.ParadaDDL = Util.GetParadaDDL(filialId, vm.ParadaId);
                ModelState.AddModelError("CNPJ", "Já existe um cliente cadastrado com este CPF/CNPJ");
                return PartialView("_Create", vm);
            }

            Cliente cliente = new Cliente();
            cliente.Nome = vm.Nome;
            cliente.CNPJ = documento;
            cliente.Endereco_Logradouro = vm.Endereco_Logradouro;
            cliente.Endereco_Numero = vm.Endereco_Numero;
            cliente.Endereco_Complemento = vm.Endereco_Complemento;
            cliente.Endereco_Bairro = vm.Endereco_Bairro;
            cliente.Endereco_Cidade = vm.Endereco_Cidade;
            cliente.Endereco_UF = vm.Endereco_UF;
            cliente.Endereco_CEP = vm.Endereco_CEP;
            cliente.Telefone1 = vm.Telefone1;
            cliente.Telefone2 = vm.Telefone2;
            cliente.Telefone3 = vm.Telefone3;
            cliente.Observacoes = vm.Observacoes;
            cliente.TransportadoraId = vm.TransportadoraId;
            cliente.RotaId = vm.RotaId;
            cliente.ParadaId = vm.ParadaId;
            cliente.Etiqueta = vm.Etiqueta;
            cliente.FilialId = filialId;

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
            vm.Id = cliente.Id;
            vm.Nome = cliente.Nome;
            vm.CNPJ = FormatDocumento(cliente.CNPJ);
            vm.Endereco_Logradouro = cliente.Endereco_Logradouro;
            vm.Endereco_Numero = cliente.Endereco_Numero;
            vm.Endereco_Complemento = cliente.Endereco_Complemento;
            vm.Endereco_Bairro = cliente.Endereco_Bairro;
            vm.Endereco_Cidade = cliente.Endereco_Cidade;
            vm.Endereco_CEP = cliente.Endereco_CEP;
            vm.Telefone1 = cliente.Telefone1;
            vm.Telefone2 = cliente.Telefone2;
            vm.Telefone3 = cliente.Telefone3;
            vm.Observacoes = cliente.Observacoes;

            vm.Endereco_UF = cliente.Endereco_UF;
            vm.EstadoDDL = Util.GetEstadoDDL(cliente.Endereco_UF);

            vm.TransportadoraId = cliente.TransportadoraId;
            vm.TransportadoraDDL = GetTransportadoraClienteDDL(cliente.TransportadoraId);

            vm.RotaId = cliente.RotaId;
            vm.RotaDDL = Util.GetRotaDDL(filialId, cliente.RotaId);

            vm.ParadaId = cliente.ParadaId;
            vm.ParadaDDL = Util.GetParadaDDL(filialId, cliente.ParadaId);
            vm.Etiqueta = cliente.Etiqueta == true;

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
                vm.TransportadoraDDL = GetTransportadoraClienteDDL(vm.TransportadoraId);
                vm.RotaDDL = Util.GetRotaDDL(filialId, vm.RotaId);
                vm.ParadaDDL = Util.GetParadaDDL(filialId, vm.ParadaId);
                return PartialView("_Edit", vm);
            }

            Cliente cliente = db.Cliente.Find(vm.Id);
            if (cliente == null)
            {
                return HttpNotFound();
            }

            string documento = NormalizarDocumento(vm.CNPJ);

            // CPF/CNPJ e opcional, mas deve ser valido quando informado.
            if (!string.IsNullOrWhiteSpace(documento) && !DocumentoValido(documento))
            {
                vm.EstadoDDL = Util.GetEstadoDDL(vm.Endereco_UF);
                vm.TransportadoraDDL = GetTransportadoraClienteDDL(vm.TransportadoraId);
                vm.RotaDDL = Util.GetRotaDDL(filialId, vm.RotaId);
                vm.ParadaDDL = Util.GetParadaDDL(filialId, vm.ParadaId);
                ModelState.AddModelError("CNPJ", "O CPF/CNPJ informado não é válido");
                return PartialView("_Edit", vm);
            }

            if (!string.IsNullOrWhiteSpace(documento) &&
                db.Cliente.Any(p =>
                    p.Id != vm.Id &&
                    (p.CNPJ == documento || p.CNPJ == vm.CNPJ)))
            {
                vm.EstadoDDL = Util.GetEstadoDDL(vm.Endereco_UF);
                vm.TransportadoraDDL = GetTransportadoraClienteDDL(vm.TransportadoraId);
                vm.RotaDDL = Util.GetRotaDDL(filialId, vm.RotaId);
                vm.ParadaDDL = Util.GetParadaDDL(filialId, vm.ParadaId);
                ModelState.AddModelError("CNPJ", "Já existe um cliente cadastrado com este CPF/CNPJ");
                return PartialView("_Edit", vm);
            }

            cliente.Nome = vm.Nome;
            cliente.CNPJ = documento;
            cliente.Endereco_Logradouro = vm.Endereco_Logradouro;
            cliente.Endereco_Numero = vm.Endereco_Numero;
            cliente.Endereco_Complemento = vm.Endereco_Complemento;
            cliente.Endereco_Bairro = vm.Endereco_Bairro;
            cliente.Endereco_Cidade = vm.Endereco_Cidade;
            cliente.Endereco_UF = vm.Endereco_UF;
            cliente.Endereco_CEP = vm.Endereco_CEP;
            cliente.Telefone1 = vm.Telefone1;
            cliente.Telefone2 = vm.Telefone2;
            cliente.Telefone3 = vm.Telefone3;
            cliente.Observacoes = vm.Observacoes;
            cliente.TransportadoraId = vm.TransportadoraId;
            cliente.RotaId = vm.RotaId;
            cliente.ParadaId = vm.ParadaId;
            cliente.Etiqueta = vm.Etiqueta;

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
