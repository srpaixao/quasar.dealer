using System;
using System.Linq;
using System.Web.Mvc;

using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Data.Entity.Infrastructure;

using Simplify.Quasar.Models;
using Simplify.Quasar.Areas.EstoqueApp.ViewModels;
using Simplify.Quasar.Custom;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using System.Web;

namespace Simplify.Quasar.Areas.EstoqueApp.Controllers
{
    [ValidateSession]
    public class LocacaoController : Controller
    {
        private const string SessaoLotePrefixo = "locacao-lote:";
        private const string SessaoEtiquetaPrefixo = "locacao-etiqueta:";
        private static readonly TimeSpan ValidadeLote = TimeSpan.FromHours(2);

        Quasar_Entities db = new Quasar_Entities();

        int filialId = Util.GetCurrentFilial();

        // GET: Locacao/Index
        public ActionResult Index()
        {
            ViewBag.Permissoes = Util.GetPermissoes(
                ControllerContext.RouteData.Values["controller"].ToString(),
                ControllerContext.RouteData.DataTokens["area"] as string);
            return View();
        }

        // GET: Locacao/Create
        public ActionResult Create()
        {
            LocacaoViewModel vm = new LocacaoViewModel
            {
                TipoDDL = BuildTipoDDL(string.Empty),
                ZonaDDL = BuildZonaDDL()
            };

            return PartialView("_Create", vm);
        }

        // POST: Locacao/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(LocacaoViewModel vm)
        {
            vm.Codigo = (vm.Codigo ?? string.Empty).Trim().ToUpperInvariant();
            vm.Tipo = (vm.Tipo ?? string.Empty).Trim().ToUpperInvariant();
            vm.TipoDDL = BuildTipoDDL(vm.Tipo);
            vm.ZonaDDL = BuildZonaDDL();

            if (string.IsNullOrWhiteSpace(vm.Codigo))
                ModelState.AddModelError("Codigo", "Informe o código da locação.");

            if (vm.Tipo != "P" && vm.Tipo != "R" && vm.Tipo != "E")
                ModelState.AddModelError("Tipo", "Selecione um tipo de locação válido.");

            if (db.Locacao.Any(locacao => locacao.Codigo == vm.Codigo && locacao.FilialId == filialId))
                ModelState.AddModelError("Codigo", "Já existe uma locação cadastrada com este código.");

            if (!ModelState.IsValid)
                return PartialView("_Create", vm);

            using (DbContextTransaction tr = db.Database.BeginTransaction())
            {
                try
                {
                    var service = new LocacaoService(db, filialId, Util.GetCurrentUser(), Util.GetCurrentDateTime());
                    service.Adicionar(new LocacaoCreateRequest
                    {
                        Codigo = vm.Codigo,
                        Tipo = vm.Tipo,
                        Descricao = vm.Descricao,
                        Bloqueado = vm.Bloqueado,
                        AreaId = vm.AreaId,
                        ZonaId = vm.ZonaId,
                        EquipamentoId = vm.EquipamentoId,
                        Curva = vm.Curva,
                        Estrategia = vm.Estrategia,
                        Observacoes = vm.Observacoes
                    });
                    db.SaveChanges();
                    tr.Commit();
                }
                catch (Exception ex)
                {
                    tr.Rollback();
                    ViewBag.Exception = ex.Message;
                    ViewBag.InnerException = ex.InnerException;
                    ViewBag.Source = ex.Source;
                    return PartialView("_Create", vm);
                }
            }

            return Json(new { success = true });
        }

        // GET: EstoqueApp/Locacao/Lote
        public ActionResult Lote()
        {
            if (!PodeAdicionar())
            {
                return new HttpStatusCodeResult(403);
            }

            return View(new LocacaoLoteViewModel());
        }

        // POST: EstoqueApp/Locacao/Lote
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Lote(HttpPostedFileBase arquivo)
        {
            if (!PodeAdicionar())
            {
                return new HttpStatusCodeResult(403);
            }

            try
            {
                var service = new LocacaoLoteService(db, filialId);
                LocacaoLoteSessao sessao = service.Simular(arquivo);
                Session[SessaoLotePrefixo + sessao.Token] = sessao;
                return View(sessao.Preview);
            }
            catch (Exception ex)
            {
                return View(new LocacaoLoteViewModel
                {
                    NomeArquivo = arquivo == null ? null : Path.GetFileName(arquivo.FileName),
                    ErroGeral = ex.GetBaseException().Message
                });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ConfirmarLote(string token)
        {
            if (!PodeAdicionar())
            {
                return new HttpStatusCodeResult(403);
            }

            LocacaoLoteSessao sessao = ObterSessaoLote(token);
            if (sessao == null)
            {
                return View("Lote", new LocacaoLoteViewModel
                {
                    ErroGeral = "A simulação expirou ou não pertence à filial atual. Importe o arquivo novamente."
                });
            }

            if (sessao.Preview.LinhasComErro > 0 || !string.IsNullOrWhiteSpace(sessao.Preview.ErroGeral))
            {
                sessao.Preview.ErroGeral = "Corrija os erros do arquivo antes de confirmar a criação.";
                return View("Lote", sessao.Preview);
            }

            var codigosCriados = new List<string>();
            int jaExistentes = 0;
            DateTime dataHora = Util.GetCurrentDateTime();
            string usuario = Util.GetCurrentUser();

            using (DbContextTransaction tr = db.Database.BeginTransaction())
            {
                try
                {
                    var existentesNoMomento = new HashSet<string>(
                        db.Locacao.AsNoTracking()
                            .Where(x => x.FilialId == filialId)
                            .Select(x => x.Codigo)
                            .ToList()
                            .Select(LocacaoService.NormalizarCodigo),
                        StringComparer.OrdinalIgnoreCase);
                    var service = new LocacaoService(db, filialId, usuario, dataHora);
                    var pendentes = new List<Locacao>();

                    foreach (LocacaoLoteItem item in sessao.Itens)
                    {
                        if (existentesNoMomento.Contains(item.Codigo))
                        {
                            jaExistentes++;
                            continue;
                        }

                        Locacao criada = service.Adicionar(new LocacaoCreateRequest
                        {
                            Codigo = item.Codigo,
                            Tipo = "P",
                            Descricao = item.Descricao,
                            Bloqueado = false,
                            AreaId = item.AreaId,
                            ZonaId = item.ZonaId,
                            EquipamentoId = item.EquipamentoId,
                            Curva = item.Demanda,
                            Estrategia = null,
                            Observacoes = "Criada por importação em lote."
                        });
                        pendentes.Add(criada);
                        existentesNoMomento.Add(item.Codigo);
                        codigosCriados.Add(item.Codigo);

                        if (pendentes.Count >= 1000)
                        {
                            db.SaveChanges();
                            foreach (Locacao persistida in pendentes)
                            {
                                db.Entry(persistida).State = EntityState.Detached;
                            }
                            pendentes.Clear();
                        }
                    }

                    if (pendentes.Count > 0)
                    {
                        db.SaveChanges();
                    }
                    RegistrarHistoricoLote(sessao, usuario, dataHora, codigosCriados.Count, jaExistentes);
                    tr.Commit();
                }
                catch (Exception ex)
                {
                    tr.Rollback();
                    sessao.Preview.ErroGeral = "Nenhuma locação foi criada. " + ex.GetBaseException().Message;
                    return View("Lote", sessao.Preview);
                }
            }

            Session.Remove(SessaoLotePrefixo + sessao.Token);
            sessao.Preview.Resultado = new LocacaoLoteResultadoViewModel
            {
                Processadas = sessao.Preview.LocacoesPrevistas,
                Criadas = codigosCriados.Count,
                JaExistentes = jaExistentes,
                DuplicadasArquivo = sessao.Preview.LocacoesDuplicadasArquivo,
                Erros = 0
            };
            sessao.Preview.Token = null;
            return View("Lote", sessao.Preview);
        }

        public ActionResult DownloadModelo()
        {
            if (!PodeAdicionar())
            {
                return new HttpStatusCodeResult(403);
            }

            string caminho = Server.MapPath("~/Content/Modelos/Modelo-Locacoes-Lote.xlsx");
            if (!System.IO.File.Exists(caminho))
            {
                return HttpNotFound("Modelo de importação não encontrado.");
            }

            return File(caminho, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Modelo-Locacoes-Lote.xlsx");
        }

        public ActionResult Etiquetas()
        {
            return View(BuildEtiquetaConsultaViewModel());
        }

        [HttpPost]
        public ActionResult GetEtiquetasData()
        {
            LocacaoEtiquetaConsultaRequest model;
            using (var reader = new StreamReader(Request.InputStream))
            {
                model = JsonConvert.DeserializeObject<LocacaoEtiquetaConsultaRequest>(reader.ReadToEnd());
            }

            if (model == null)
            {
                return Json(new { draw = 0, recordsFiltered = 0, recordsTotal = 0, data = new object[0] });
            }

            IQueryable<Locacao> todasDaFilial = db.Locacao.AsNoTracking().Where(x => x.FilialId == filialId);
            int recordsTotal = todasDaFilial.Count();
            string pesquisa = model.search == null ? null : model.search.value;
            IQueryable<Locacao> query = AplicarFiltrosEtiquetas(
                todasDaFilial, model.codigo, model.descricao, model.areaId, model.zonaId,
                model.equipamentoId, model.demanda, pesquisa);
            int recordsFiltered = query.Count();

            var areasDaFilial = db.Area.AsNoTracking()
                .Where(a => a.FilialId == filialId || a.FilialId == null);
            var zonasDaFilial = db.Zona.AsNoTracking()
                .Where(z => z.FilialId == filialId || z.FilialId == null);
            var equipamentosDaFilial = db.Equipamento.AsNoTracking()
                .Where(e => e.FilialId == filialId || e.FilialId == null);

            int sortColumn = model.order != null && model.order.Length > 0 ? model.order[0].column : 1;
            bool desc = model.order != null && model.order.Length > 0 && model.order[0].dir == "desc";
            IQueryable<Locacao> ordenada;
            switch (sortColumn)
            {
                case 2:
                    ordenada = desc ? query.OrderByDescending(x => x.Descricao) : query.OrderBy(x => x.Descricao);
                    break;
                case 3:
                    ordenada = desc
                        ? from l in query
                          join a in areasDaFilial on l.AreaId equals (int?)a.Id into areaJoin
                          from a in areaJoin.DefaultIfEmpty()
                          orderby a.Nome descending, l.Codigo
                          select l
                        : from l in query
                          join a in areasDaFilial on l.AreaId equals (int?)a.Id into areaJoin
                          from a in areaJoin.DefaultIfEmpty()
                          orderby a.Nome, l.Codigo
                          select l;
                    break;
                case 4:
                    ordenada = desc
                        ? from l in query
                          join z in zonasDaFilial on l.ZonaId equals (int?)z.Id into zonaJoin
                          from z in zonaJoin.DefaultIfEmpty()
                          orderby (z.Codigo ?? z.Nome) descending, l.Codigo
                          select l
                        : from l in query
                          join z in zonasDaFilial on l.ZonaId equals (int?)z.Id into zonaJoin
                          from z in zonaJoin.DefaultIfEmpty()
                          orderby (z.Codigo ?? z.Nome), l.Codigo
                          select l;
                    break;
                case 5:
                    ordenada = desc
                        ? from l in query
                          join e in equipamentosDaFilial on l.EquipamentoId equals (int?)e.Id into equipamentoJoin
                          from e in equipamentoJoin.DefaultIfEmpty()
                          orderby e.Nome descending, l.Codigo
                          select l
                        : from l in query
                          join e in equipamentosDaFilial on l.EquipamentoId equals (int?)e.Id into equipamentoJoin
                          from e in equipamentoJoin.DefaultIfEmpty()
                          orderby e.Nome, l.Codigo
                          select l;
                    break;
                case 6:
                    ordenada = desc ? query.OrderByDescending(x => x.Curva) : query.OrderBy(x => x.Curva);
                    break;
                default:
                    ordenada = desc ? query.OrderByDescending(x => x.Codigo) : query.OrderBy(x => x.Codigo);
                    break;
            }

            int length = model.length > 0 ? Math.Min(model.length, 100) : 25;
            // Primeiro pagina Locacao usando o índice; os nomes auxiliares são
            // resolvidos somente para os poucos registros exibidos na página.
            var locacoesPagina = ordenada.Skip(model.start).Take(length).ToList();
            var areaIds = locacoesPagina.Where(x => x.AreaId.HasValue).Select(x => x.AreaId.Value).Distinct().ToList();
            var zonaIds = locacoesPagina.Where(x => x.ZonaId.HasValue).Select(x => x.ZonaId.Value).Distinct().ToList();
            var equipamentoIds = locacoesPagina.Where(x => x.EquipamentoId.HasValue).Select(x => x.EquipamentoId.Value).Distinct().ToList();
            var areas = areasDaFilial.Where(x => areaIds.Contains(x.Id)).ToDictionary(x => x.Id, x => x.Nome);
            var zonas = zonasDaFilial.Where(x => zonaIds.Contains(x.Id)).ToDictionary(x => x.Id, x => x.Codigo ?? x.Nome);
            var equipamentos = equipamentosDaFilial.Where(x => equipamentoIds.Contains(x.Id)).ToDictionary(x => x.Id, x => x.Nome);
            var pagina = locacoesPagina.Select(l => new LocacaoEtiquetaGridItemViewModel
            {
                Id = l.Id,
                Codigo = l.Codigo,
                Descricao = l.Descricao,
                Area = l.AreaId.HasValue && areas.ContainsKey(l.AreaId.Value) ? areas[l.AreaId.Value] : string.Empty,
                Zona = l.ZonaId.HasValue && zonas.ContainsKey(l.ZonaId.Value) ? zonas[l.ZonaId.Value] : string.Empty,
                Equipamento = l.EquipamentoId.HasValue && equipamentos.ContainsKey(l.EquipamentoId.Value) ? equipamentos[l.EquipamentoId.Value] : string.Empty,
                Demanda = l.Curva
            }).ToList();
            JsonResult result = Json(new { draw = model.draw, recordsFiltered, recordsTotal, data = pagina });
            result.MaxJsonLength = int.MaxValue;
            return result;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ObterIdsEtiquetas(LocacaoEtiquetaFiltroViewModel filtro)
        {
            filtro = filtro ?? new LocacaoEtiquetaFiltroViewModel();
            var ids = AplicarFiltrosEtiquetas(
                    db.Locacao.AsNoTracking().Where(x => x.FilialId == filialId),
                    filtro.Codigo, filtro.Descricao, filtro.AreaId, filtro.ZonaId,
                    filtro.EquipamentoId, filtro.Demanda, filtro.Pesquisa)
                .OrderBy(x => x.Codigo)
                .Select(x => x.Id)
                .ToList();

            return Json(new { success = true, ids });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult PrepararEtiquetas(string ids, string modelo)
        {
            var selecionados = (ids ?? string.Empty)
                .Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => { int id; return int.TryParse(x, out id) ? id : 0; })
                .Where(x => x > 0)
                .Distinct()
                .ToList();

            if (selecionados.Count == 0)
            {
                return Json(new { success = false, message = "Selecione pelo menos uma locação para imprimir." });
            }

            var locacoes = CarregarLocacoesPorIds(selecionados);
            var idsValidos = new HashSet<int>(locacoes.Select(x => x.Id));
            var ordenados = selecionados.Where(idsValidos.Contains).ToList();
            if (ordenados.Count == 0)
            {
                return Json(new { success = false, message = "Selecione pelo menos uma locação para imprimir." });
            }

            string token = Guid.NewGuid().ToString("N");
            Session[SessaoEtiquetaPrefixo + token] = new LocacaoEtiquetaLoteSessao
            {
                Token = token,
                FilialId = filialId,
                CriadoEm = Util.GetCurrentDateTime(),
                Ids = ordenados
            };

            return Json(new
            {
                success = true,
                url = string.Equals(modelo, "alternativo", StringComparison.OrdinalIgnoreCase)
                    ? Url.Action("LayoutEtiquetasAlternativo", "Locacao", new { area = "EstoqueApp", token })
                    : Url.Action("LayoutEtiquetas", "Locacao", new { area = "EstoqueApp", token })
            });
        }

        public ActionResult LayoutEtiquetas(string token)
        {
            return MontarLayoutEtiquetas(token, "LayoutEtiquetas");
        }

        public ActionResult LayoutEtiquetasAlternativo(string token)
        {
            return MontarLayoutEtiquetas(token, "LayoutEtiquetasAlternativo");
        }

        private ActionResult MontarLayoutEtiquetas(string token, string nomeView)
        {
            LocacaoEtiquetaLoteSessao sessao = ObterSessaoEtiqueta(token);
            if (sessao == null)
            {
                return new HttpStatusCodeResult(410, "A seleção de etiquetas expirou ou não pertence à filial atual.");
            }

            var locacoes = CarregarLocacoesPorIds(sessao.Ids);
            var areas = db.Area.AsNoTracking().Where(x => x.FilialId == filialId || x.FilialId == null).ToDictionary(x => x.Id, x => x.Nome);
            var zonas = db.Zona.AsNoTracking().Where(x => x.FilialId == filialId || x.FilialId == null).ToDictionary(x => x.Id, x => x.Nome ?? x.Codigo);
            var equipamentos = db.Equipamento.AsNoTracking().Where(x => x.FilialId == filialId || x.FilialId == null).ToDictionary(x => x.Id, x => x.Nome);
            var porId = locacoes.ToDictionary(x => x.Id);
            var vm = new LocacaoEtiquetaImpressaoViewModel();

            foreach (int id in sessao.Ids)
            {
                Locacao locacao;
                if (!porId.TryGetValue(id, out locacao))
                {
                    continue;
                }

                string area = locacao.AreaId.HasValue && areas.ContainsKey(locacao.AreaId.Value) ? areas[locacao.AreaId.Value] : string.Empty;
                string zona = locacao.ZonaId.HasValue && zonas.ContainsKey(locacao.ZonaId.Value) ? zonas[locacao.ZonaId.Value] : string.Empty;
                string equipamento = locacao.EquipamentoId.HasValue && equipamentos.ContainsKey(locacao.EquipamentoId.Value) ? equipamentos[locacao.EquipamentoId.Value] : string.Empty;
                vm.Etiquetas.Add(new LocacaoEtiquetaItemViewModel
                {
                    Codigo = locacao.Codigo,
                    CodigoSemEspacos = ChaveComparacaoCodigo(locacao.Codigo),
                    CodigoFormatado = FormatarCodigoEtiqueta(locacao.Codigo),
                    Descricao = locacao.Descricao,
                    Area = area,
                    Zona = zona,
                    Equipamento = equipamento,
                    Demanda = locacao.Curva
                });
            }

            return View(nomeView, vm);
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

            var query = from l in db.Locacao.AsNoTracking()
                        where l.FilialId == filialId
                        select new LocacaoViewModel
                        {
                            Codigo = l.Codigo,
                            Tipo = l.Tipo,
                            Descricao = l.Descricao,
                            Bloqueado = l.Bloqueado,
                            AreaNome = (from a in db.Area where a.Id == l.AreaId && a.FilialId == filialId select a.Nome).FirstOrDefault(),
                            Curva = l.Curva,
                            Observacoes = l.Observacoes
                        };

            int recordsTotal = query.Count();
            string termo = model.search == null ? string.Empty : (model.search.value ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(termo))
            {
                query = query.Where(x =>
                    (x.Codigo ?? string.Empty).Contains(termo) ||
                    (x.Tipo ?? string.Empty).Contains(termo) ||
                    (x.Descricao ?? string.Empty).Contains(termo) ||
                    (x.AreaNome ?? string.Empty).Contains(termo) ||
                    (x.Curva ?? string.Empty).Contains(termo) ||
                    (x.Observacoes ?? string.Empty).Contains(termo));
            }

            int recordsFiltered = query.Count();
            int sortColumn = model.order != null && model.order.Length > 0 ? model.order[0].column : 0;
            bool desc = model.order != null && model.order.Length > 0 && model.order[0].dir == "desc";

            switch (sortColumn)
            {
                case 1: query = desc ? query.OrderByDescending(x => x.Tipo) : query.OrderBy(x => x.Tipo); break;
                case 2: query = desc ? query.OrderByDescending(x => x.Descricao) : query.OrderBy(x => x.Descricao); break;
                case 3: query = desc ? query.OrderByDescending(x => x.Bloqueado) : query.OrderBy(x => x.Bloqueado); break;
                case 4: query = desc ? query.OrderByDescending(x => x.AreaNome) : query.OrderBy(x => x.AreaNome); break;
                case 5: query = desc ? query.OrderByDescending(x => x.Curva) : query.OrderBy(x => x.Curva); break;
                case 6: query = desc ? query.OrderByDescending(x => x.Observacoes) : query.OrderBy(x => x.Observacoes); break;
                default: query = desc ? query.OrderByDescending(x => x.Codigo) : query.OrderBy(x => x.Codigo); break;
            }

            int length = model.length > 0 ? model.length : 25;
            var locacoes = query.Skip(model.start).Take(length).ToList();
            foreach (var locacao in locacoes)
            {
                locacao.Status = locacao.Bloqueado ? "<span class='text-red'>*** Bloqueado ***</span>" : string.Empty;
            }

            JsonResult result = Json(new { draw = model.draw, recordsFiltered, recordsTotal, data = locacoes });
            result.MaxJsonLength = int.MaxValue;
            return result;
        }

        // GET: Locacao/Edit
        public ActionResult Edit(string codigo)
        {
            Locacao locacao = db.Locacao.Where(x => x.Codigo == codigo && x.FilialId == filialId).FirstOrDefault();
            if (locacao == null)
            {
                return HttpNotFound();
            }

            LocacaoViewModel vm = new LocacaoViewModel();
            vm.Codigo = locacao.Codigo;
            vm.Tipo = locacao.Tipo;
            vm.Descricao = locacao.Descricao;
            vm.Bloqueado = locacao.Bloqueado;
            vm.AreaId = locacao.AreaId;
            vm.ZonaId = GetZonaId(locacao.Codigo);
            vm.Curva = locacao.Curva;
            vm.Observacoes = locacao.Observacoes;
            vm.CriadoEm = locacao.CriadoEm;
            vm.CriadoPor = locacao.CriadoPor;
            vm.CriadoPorNome = (from u in db.Usuario where u.FilialId == filialId && u.Login == locacao.CriadoPor select u.Nome).FirstOrDefault();
            vm.ModificadoEm = locacao.ModificadoEm;
            vm.ModificadoPor = locacao.ModificadoPor;
            vm.ModificadoPorNome = (from u in db.Usuario where u.FilialId == filialId && u.Login == locacao.ModificadoPor select u.Nome).FirstOrDefault();
            vm.TipoDDL = BuildTipoDDL(vm.Tipo);
            vm.ZonaDDL = BuildZonaDDL();

            return PartialView("_Edit", vm);
        }

        // POST: Locacao/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(LocacaoViewModel vm)
        {
            vm.Tipo = (vm.Tipo ?? string.Empty).Trim().ToUpperInvariant();
            vm.TipoDDL = BuildTipoDDL(vm.Tipo);
            vm.ZonaDDL = BuildZonaDDL();

            if (vm.Tipo != "P" && vm.Tipo != "R" && vm.Tipo != "E")
            {
                ModelState.AddModelError("Tipo", "Selecione um tipo de locação válido.");
            }

            if (!ModelState.IsValid)
            {
                return PartialView("_Edit", vm);
            }

            Locacao locacao = db.Locacao.Where(x => x.Codigo == vm.Codigo && x.FilialId == filialId).FirstOrDefault();
            if (locacao == null)
            {
                return HttpNotFound();
            }

            locacao.Tipo = vm.Tipo;
            locacao.Descricao = vm.Descricao;
            locacao.Bloqueado = vm.Bloqueado;
            locacao.AreaId = vm.AreaId;
            locacao.Curva = vm.Curva;
            locacao.Observacoes = vm.Observacoes;
            locacao.ModificadoEm = Util.GetCurrentDateTime();
            locacao.ModificadoPor = Util.GetCurrentUser();
            locacao.FilialId = filialId;
            db.Entry(locacao).State = EntityState.Modified;

            ViewBag.ControllerName = ControllerContext.RouteData.Values["controller"].ToString();
            ViewBag.ActionName = ControllerContext.RouteData.Values["action"].ToString();

            using (DbContextTransaction tr = db.Database.BeginTransaction())
            {
                try
                {
                    db.SaveChanges();
                    db.Database.ExecuteSqlCommand(
                        "UPDATE Locacao SET ZonaId = @p0 WHERE Codigo = @p1 AND FilialId = @p2",
                        (object)vm.ZonaId ?? DBNull.Value,
                        vm.Codigo,
                        filialId);
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

        // POST: Locacao/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(string codigo)
        {
            codigo = (codigo ?? string.Empty).Trim();
            Locacao locacao = db.Locacao
                .FirstOrDefault(item => item.Codigo == codigo && item.FilialId == filialId);

            if (locacao == null)
                return Json(new { success = false, msg = "Locação não encontrada." });

            int emUso = db.Database.SqlQuery<int>(
                @"SELECT CASE WHEN
                       EXISTS
                       (
                           SELECT 1
                           FROM Estoque
                           WHERE FilialId = @p0
                             AND REPLACE(REPLACE(UPPER(LTRIM(RTRIM(Locacao))), '.', ''), ' ', '')
                               = REPLACE(REPLACE(UPPER(LTRIM(RTRIM(@p1))), '.', ''), ' ', '')
                       )
                       OR EXISTS
                       (
                           SELECT 1
                           FROM MovimentacaoDestino
                           WHERE FilialId = @p0
                             AND REPLACE(REPLACE(UPPER(LTRIM(RTRIM(Locacao))), '.', ''), ' ', '')
                               = REPLACE(REPLACE(UPPER(LTRIM(RTRIM(@p1))), '.', ''), ' ', '')
                       )
                       OR EXISTS
                       (
                           SELECT 1
                           FROM Movimentacao
                           WHERE FilialId = @p0
                             AND FinalizadoEm IS NULL
                             AND REPLACE(REPLACE(UPPER(LTRIM(RTRIM(LocacaoEspera))), '.', ''), ' ', '')
                               = REPLACE(REPLACE(UPPER(LTRIM(RTRIM(@p1))), '.', ''), ' ', '')
                       )
                       THEN 1 ELSE 0 END",
                filialId,
                codigo).FirstOrDefault();

            if (emUso == 1)
            {
                return Json(new
                {
                    success = false,
                    msg = "A locação está em uso e não pode ser excluída."
                });
            }

            using (DbContextTransaction tr = db.Database.BeginTransaction())
            {
                try
                {
                    db.Locacao.Remove(locacao);
                    db.SaveChanges();
                    tr.Commit();
                }
                catch (Exception ex)
                {
                    tr.Rollback();
                    return Json(new { success = false, msg = ex.GetBaseException().Message });
                }
            }

            return Json(new { success = true, msg = "Locação excluída com sucesso." });
        }

        public JsonResult GetLocacoesDisponiveis()
        {
            var codigos = db.Database.SqlQuery<string>(
                @"SELECT LTRIM(RTRIM(Locacao))
                    FROM dbo.Estoque
                   WHERE FilialId = @p0
                     AND NULLIF(LTRIM(RTRIM(Locacao)), '') IS NOT NULL
                   GROUP BY LTRIM(RTRIM(Locacao))
                  HAVING SUM(CASE WHEN ISNULL(Saldo, 0) > 0 THEN 1 ELSE 0 END) = 0
                   ORDER BY LTRIM(RTRIM(Locacao))",
                filialId).ToList();

            var locacoes = codigos.Select(codigo => new
            {
                codigo = codigo,
                tipo = string.Empty,
                area = string.Empty,
                equipamento = string.Empty
            }).ToList();

            return Json(new { total_results = locacoes.Count, results = locacoes }, JsonRequestBehavior.AllowGet);
        }

        private int? GetZonaId(string codigo)
        {
            return db.Database.SqlQuery<int?>(
                "SELECT TOP 1 ZonaId FROM Locacao WHERE Codigo = @p0 AND FilialId = @p1",
                codigo,
                filialId).FirstOrDefault();
        }

        private IEnumerable<SelectListItem> BuildZonaDDL()
        {
            var zonas = db.Database.SqlQuery<ZonaLookupItem>(
                @"SELECT Id, Nome
                    FROM Zona
                   WHERE Ativo = 1
                     AND (FilialId = @p0 OR FilialId IS NULL)
                   ORDER BY Nome",
                filialId).ToList();

            var ddl = zonas.Select(x => new SelectListItem
            {
                Value = x.Id.ToString(),
                Text = x.Nome
            }).ToList();

            ddl.Insert(0, new SelectListItem { Value = string.Empty, Text = string.Empty });
            return ddl;
        }

        private static IEnumerable<SelectListItem> BuildTipoDDL(string tipoSelecionado)
        {
            string selecionado = (tipoSelecionado ?? string.Empty).Trim().ToUpperInvariant();
            return new[]
            {
                new SelectListItem { Value = "P", Text = "Principal", Selected = selecionado == "P" },
                new SelectListItem { Value = "R", Text = "Reserva", Selected = selecionado == "R" },
                new SelectListItem { Value = "E", Text = "Espera", Selected = selecionado == "E" }
            };
        }

        private bool PodeAdicionar()
        {
            string permissoes = Util.GetPermissoes("Locacao", "EstoqueApp") ?? string.Empty;
            return permissoes.Contains("[add]");
        }

        private LocacaoLoteSessao ObterSessaoLote(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return null;
            }

            var sessao = Session[SessaoLotePrefixo + token] as LocacaoLoteSessao;
            if (sessao == null || sessao.FilialId != filialId || Util.GetCurrentDateTime() - sessao.CriadoEm > ValidadeLote)
            {
                Session.Remove(SessaoLotePrefixo + token);
                return null;
            }

            return sessao;
        }

        private LocacaoEtiquetaLoteSessao ObterSessaoEtiqueta(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return null;
            }

            var sessao = Session[SessaoEtiquetaPrefixo + token] as LocacaoEtiquetaLoteSessao;
            if (sessao == null || sessao.FilialId != filialId || Util.GetCurrentDateTime() - sessao.CriadoEm > ValidadeLote)
            {
                Session.Remove(SessaoEtiquetaPrefixo + token);
                return null;
            }

            return sessao;
        }

        private LocacaoEtiquetaLoteViewModel BuildEtiquetaConsultaViewModel()
        {
            var areas = db.Area.AsNoTracking()
                .Where(x => x.FilialId == filialId || x.FilialId == null)
                .OrderBy(x => x.Nome)
                .Select(x => new { x.Id, x.Nome })
                .ToList()
                .Select(x => new SelectListItem { Value = x.Id.ToString(), Text = x.Nome })
                .ToList();
            areas.Insert(0, new SelectListItem { Value = string.Empty, Text = "Todas" });

            var zonasDados = db.Zona.AsNoTracking()
                .Where(x => x.FilialId == filialId || x.FilialId == null)
                .OrderBy(x => x.Codigo)
                .ThenBy(x => x.Nome)
                .Select(x => new { x.Id, x.Codigo, x.Nome })
                .ToList();
            var zonas = zonasDados.Select(x => new SelectListItem
            {
                Value = x.Id.ToString(),
                Text = string.IsNullOrWhiteSpace(x.Nome) || x.Nome == x.Codigo
                    ? x.Codigo
                    : x.Codigo + " - " + x.Nome
            }).ToList();
            zonas.Insert(0, new SelectListItem { Value = string.Empty, Text = "Todas" });

            var equipamentos = db.Equipamento.AsNoTracking()
                .Where(x => x.FilialId == filialId || x.FilialId == null)
                .OrderBy(x => x.Nome)
                .Select(x => new { x.Id, x.Nome })
                .ToList()
                .Select(x => new SelectListItem { Value = x.Id.ToString(), Text = x.Nome })
                .ToList();
            equipamentos.Insert(0, new SelectListItem { Value = string.Empty, Text = "Todos" });

            return new LocacaoEtiquetaLoteViewModel
            {
                Areas = areas,
                Zonas = zonas,
                Equipamentos = equipamentos,
                Demandas = new[]
                {
                    new SelectListItem { Value = string.Empty, Text = "Todas" },
                    new SelectListItem { Value = "A", Text = "A" },
                    new SelectListItem { Value = "B", Text = "B" },
                    new SelectListItem { Value = "C", Text = "C" },
                    new SelectListItem { Value = "D", Text = "D" },
                    new SelectListItem { Value = "N", Text = "N" }
                }
            };
        }

        private IQueryable<Locacao> AplicarFiltrosEtiquetas(
            IQueryable<Locacao> query,
            string codigo,
            string descricao,
            int? areaId,
            int? zonaId,
            int? equipamentoId,
            string demanda,
            string pesquisa)
        {
            codigo = (codigo ?? string.Empty).Trim();
            descricao = (descricao ?? string.Empty).Trim();
            demanda = (demanda ?? string.Empty).Trim();
            pesquisa = (pesquisa ?? string.Empty).Trim();

            if (!string.IsNullOrWhiteSpace(codigo)) query = query.Where(x => x.Codigo.Contains(codigo));
            if (!string.IsNullOrWhiteSpace(descricao)) query = query.Where(x => x.Descricao.Contains(descricao));
            if (areaId.HasValue) query = query.Where(x => x.AreaId == areaId.Value);
            if (zonaId.HasValue) query = query.Where(x => x.ZonaId == zonaId.Value);
            if (equipamentoId.HasValue) query = query.Where(x => x.EquipamentoId == equipamentoId.Value);
            if (!string.IsNullOrWhiteSpace(demanda)) query = query.Where(x => x.Curva == demanda);

            if (!string.IsNullOrWhiteSpace(pesquisa))
            {
                query = query.Where(x =>
                    (x.Codigo ?? string.Empty).Contains(pesquisa) ||
                    (x.Descricao ?? string.Empty).Contains(pesquisa) ||
                    (x.Curva ?? string.Empty).Contains(pesquisa) ||
                    db.Area.Any(a => a.Id == x.AreaId && (a.FilialId == filialId || a.FilialId == null) && a.Nome.Contains(pesquisa)) ||
                    db.Zona.Any(z => z.Id == x.ZonaId && (z.FilialId == filialId || z.FilialId == null) &&
                        ((z.Codigo ?? string.Empty).Contains(pesquisa) || (z.Nome ?? string.Empty).Contains(pesquisa))) ||
                    db.Equipamento.Any(e => e.Id == x.EquipamentoId && (e.FilialId == filialId || e.FilialId == null) && e.Nome.Contains(pesquisa)));
            }

            return query;
        }

        private List<Locacao> CarregarLocacoesPorIds(IEnumerable<int> ids)
        {
            var distintos = (ids ?? Enumerable.Empty<int>()).Where(x => x > 0).Distinct().ToList();
            var resultado = new List<Locacao>();
            for (int inicio = 0; inicio < distintos.Count; inicio += 1000)
            {
                var lote = distintos.Skip(inicio).Take(1000).ToList();
                resultado.AddRange(db.Locacao.AsNoTracking()
                    .Where(x => x.FilialId == filialId && lote.Contains(x.Id))
                    .ToList());
            }
            return resultado;
        }

        private static string ChaveComparacaoCodigo(string codigo)
        {
            return new string((codigo ?? string.Empty)
                .Trim()
                .ToUpperInvariant()
                .Where(x => !char.IsWhiteSpace(x) && x != '.')
                .ToArray());
        }

        private static string FormatarCodigoEtiqueta(string codigo)
        {
            return string.Join(".", (codigo ?? string.Empty)
                .Trim()
                .ToUpperInvariant()
                .Split(new[] { ' ', '.', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));
        }

        private void RegistrarHistoricoLote(LocacaoLoteSessao sessao, string usuario, DateTime dataHora, int criadas, int existentes)
        {
            db.Database.ExecuteSqlCommand(
                @"IF OBJECT_ID('dbo.LocacaoImportacao', 'U') IS NOT NULL
                  INSERT INTO dbo.LocacaoImportacao
                    (Arquivo, Usuario, CriadoEm, FilialId, QtdeLinhas, QtdePrevistas, QtdeCriadas, QtdeExistentes, QtdeErros)
                  VALUES
                    (@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8)",
                sessao.NomeArquivo ?? string.Empty,
                usuario ?? string.Empty,
                dataHora,
                filialId,
                sessao.Preview.LinhasImportadas,
                sessao.Preview.LocacoesPrevistas,
                criadas,
                existentes,
                sessao.Preview.LinhasComErro);
        }

        private sealed class ZonaLookupItem
        {
            public int Id { get; set; }
            public string Nome { get; set; }
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
