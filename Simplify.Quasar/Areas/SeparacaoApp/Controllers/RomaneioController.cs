using ExcelDataReader;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using Simplify.Quasar.Areas.SeparacaoApp.ViewModels;
using Simplify.Quasar.Custom;
using Simplify.Quasar.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Xml.Linq;

namespace Simplify.Quasar.Areas.SeparacaoApp.Controllers
{
    [ValidateSession]
    public class RomaneioController : Controller
    {
        private const int AdminProfileId = 1;
        private const int PickerProfileId = 2;
        private const int ConferProfileId = 3;
        private const int SupervisorProfileId = 8;
        private const int UnknownAreaPedidoId = 1;

        private const int StatusNaoGerado = 1;
        private const int StatusAguardandoSeparacao = 2;
        private const int StatusEmSeparacao = 3;
        private const int StatusFinalizado = 4;
        private const int StatusNaoSeparar = 5;
        private const int StatusEmBusca = 6;
        private const int StatusNaoGeradoAnalise = 7;
        private const int StatusNaoEncontrado = 10;
        private static readonly int[] AllowedPickerProfileIds = { AdminProfileId, PickerProfileId };
        private const string ExportacaoNaoGeradosSessionKey = "SeparacaoApp.Romaneio.NaoGerados.ExportacaoAtual";
        private const string ExportacaoNaoGeradosArquivoSessionKey = "SeparacaoApp.Romaneio.NaoGerados.Arquivo";
        private const string ExportacaoNaoGeradosArquivoNomeSessionKey = "SeparacaoApp.Romaneio.NaoGerados.ArquivoNome";
        private const string TriggerDownloadNaoGeradosSessionKey = "SeparacaoApp.Romaneio.NaoGerados.TriggerDownload";
        private const string UltimoIntervaloNaoGeradosSessionKey = "SeparacaoApp.Romaneio.NaoGerados.UltimoIntervalo";

        private readonly Quasar_Entities db = new Quasar_Entities();
        private int filialId
        {
            get { return GetEffectiveFilialId(); }
        }

        public ActionResult Index()
        {
            return View(BuildSeparacaoViewModel());
        }

        public ActionResult Lancamento()
        {
            return RedirectToAction("Index");
        }

        public ActionResult Import()
        {
            return RedirectToAction("Atualizacao");
        }

        public ActionResult Edit(int id)
        {
            return RedirectToAction("Administracao");
        }

        public ActionResult Conferencia()
        {
            if (!CanViewFullMenu())
            {
                return new HttpStatusCodeResult(403, "Perfil sem permissão para conferência.");
            }

            return View(BuildConferenciaViewModel());
        }

        public ActionResult Administracao()
        {
            if (!CanViewFullMenu())
            {
                return new HttpStatusCodeResult(403, "Perfil sem permissão para administração.");
            }

            return View(BuildAdministracaoViewModel());
        }

        public ActionResult Consulta(string romaneioNr)
        {
            return View(BuildConsultaViewModel(romaneioNr));
        }

        public ActionResult AnaliseNaoGerados(int? romaneioInicio, int? romaneioFinal, bool? triggerDownload, string exportNonce)
        {
            if (!CanViewFullMenu())
            {
                return new HttpStatusCodeResult(403, "Perfil sem permissão para análise.");
            }

            var intervalo = ResolveAnaliseNaoGeradosInterval(romaneioInicio, romaneioFinal);
            return View(BuildAnaliseNaoGeradosViewModel(
                intervalo.Item1,
                intervalo.Item2,
                triggerDownload.GetValueOrDefault()));
        }

        public ActionResult Atualizacao(DateTime? dataInicial, DateTime? dataFinal, int? romaneioInicio, int? romaneioFinal)
        {
            if (!CanViewFullMenu())
            {
                return new HttpStatusCodeResult(403, "Perfil sem permissão para atualização.");
            }

            if (!dataInicial.HasValue)
            {
                dataInicial = Util.GetCurrentDateTime().Date;
            }

            if (!dataFinal.HasValue)
            {
                dataFinal = Util.GetCurrentDateTime().Date;
            }

            var periodo = ResolvePeriodo(dataInicial, dataFinal);
            var intervalo = ResolveRomaneioInterval(romaneioInicio, romaneioFinal);
            return View("Import", BuildAtualizacaoViewModel(periodo.Item1, periodo.Item2, intervalo.Item1, intervalo.Item2));
        }

        public ActionResult AlocacaoZona(DateTime? dataInicial, DateTime? dataFinal)
        {
            if (!CanViewFullMenu())
            {
                return new HttpStatusCodeResult(403, "Perfil sem permissao para alocacao.");
            }

            if (!dataInicial.HasValue)
            {
                dataInicial = Util.GetCurrentDateTime().Date;
            }

            if (!dataFinal.HasValue)
            {
                dataFinal = Util.GetCurrentDateTime().Date;
            }

            var periodo = ResolvePeriodo(dataInicial, dataFinal);
            return View(BuildAlocacaoZonaViewModel(periodo.Item1, periodo.Item2));
        }

        public ActionResult Tarefas(string tarefaNr, string romaneio, string contato, string os, string itemEstoque, string zona, DateTime? data)
        {
            if (!CanViewFullMenu())
            {
                return new HttpStatusCodeResult(403, "Perfil sem permissao para consulta de tarefas.");
            }

            return View(BuildTarefasViewModel(tarefaNr, romaneio, contato, os, itemEstoque, zona, data));
        }

        public ActionResult Pendencias()
        {
            if (!CanViewFullMenu())
            {
                return new HttpStatusCodeResult(403, "Perfil sem permiss\u00E3o para pend\u00EAncias.");
            }

            return View(BuildPendenciasViewModel());
        }

        public ActionResult NaoEncontrados()
        {
            if (!CanViewFullMenu())
            {
                return new HttpStatusCodeResult(403, "Perfil sem permiss\u00E3o para itens n\u00E3o encontrados.");
            }

            return View(BuildNaoEncontradosViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AtualizarPendenciaStatus(int id, int statusId)
        {
            if (!CanViewFullMenu())
            {
                return new HttpStatusCodeResult(403, "Perfil sem permiss\u00E3o para pend\u00EAncias.");
            }

            if (statusId != StatusAguardandoSeparacao && statusId != StatusNaoEncontrado)
            {
                SetFlash("danger", "Status inv\u00E1lido para tratamento da pend\u00EAncia.");
                return RedirectToAction("Pendencias");
            }

            var item = db.RomaneioItem.FirstOrDefault(x => x.Id == id && x.FilialId == filialId);
            if (item == null)
            {
                SetFlash("danger", "Item de pend\u00EAncia n\u00E3o localizado.");
                return RedirectToAction("Pendencias");
            }

            if (item.StatusId != StatusEmBusca)
            {
                SetFlash("warning", "O item n\u00E3o est\u00E1 mais com status Em Busca.");
                return RedirectToAction("Pendencias");
            }

            if (!db.StatusRomaneio.Any(s => s.Id == statusId))
            {
                SetFlash("danger", "Status informado n\u00E3o existe.");
                return RedirectToAction("Pendencias");
            }

            var romaneio = db.Romaneio.FirstOrDefault(x => x.Id == item.RomaneioId && x.FilialId == filialId);
            var agora = Util.GetCurrentDateTime();
            var usuarioAtual = Util.GetCurrentUser();

            item.StatusId = statusId;

            if (statusId == StatusAguardandoSeparacao)
            {
                item.ConferenteId = null;
                item.DataConferente = null;
                item.SeparadorId = null;
                item.DataSeparador = null;
            }
            else
            {
                var usuario = db.Usuario.FirstOrDefault(u => u.Login == usuarioAtual);
                item.ConferenteId = usuario != null ? (int?)usuario.Id : item.ConferenteId;
                item.DataConferente = agora;
            }

            if (romaneio != null)
            {
                romaneio.ModificadoPor = usuarioAtual;
                romaneio.ModificadoEm = agora;
            }

            db.SaveChanges();

            if (statusId == StatusAguardandoSeparacao)
            {
                db.Database.ExecuteSqlCommand(
                    "UPDATE RomaneioItem SET QtdeConferida = NULL WHERE Id = @p0",
                    id);
            }

            SetFlash("success", statusId == StatusAguardandoSeparacao
                ? "Item retornado para a fila de separa\u00E7\u00E3o com sucesso."
                : "Item alterado para N\u00E3o Encontrado com sucesso.");

            return RedirectToAction("Pendencias");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult IniciarSeparacao(string romaneioNr, int? pickerId)
        {
            if (string.IsNullOrWhiteSpace(romaneioNr) || !pickerId.HasValue)
            {
                SetFlash("danger", "Selecione o romaneio e o picker.");
                return RedirectToAction("Index");
            }

            var picker = BuildPickerQuery()
                .FirstOrDefault(u => u.Id == pickerId.Value);
            if (picker == null)
            {
                SetFlash("danger", "Picker inválido para a filial atual.");
                return RedirectToAction("Index");
            }

            string romaneioNrNormalizado = romaneioNr.Trim();
            var romaneio = db.Romaneio
                .Where(r => r.FilialId == filialId)
                .AsEnumerable()
                .FirstOrDefault(r => string.Equals((r.RomaneioNr ?? string.Empty).Trim(), romaneioNrNormalizado, StringComparison.OrdinalIgnoreCase));
            if (romaneio == null)
            {
                SetFlash("danger", "Romaneio Nr não Encontrado");
                return RedirectToAction("Index");
            }

            if (!HasOperationalArea(romaneio.VendedorId))
            {
                SetFlash("warning", "O romaneio ainda não possui área operacional. Atualize a planilha antes de iniciar a separação.");
                return RedirectToAction("Index");
            }

            var agora = Util.GetCurrentDateTime();
            var usuarioAtual = Util.GetCurrentUser();
            bool requerConferencia = RequiresConferencia(romaneio.VendedorId);
            int statusLancamento = requerConferencia ? StatusEmSeparacao : StatusFinalizado;
            object dataConferente = requerConferencia ? (object)DBNull.Value : agora;

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    int rows = db.Database.ExecuteSqlCommand(
                        @"UPDATE Romaneio
                          SET SeparadorId = @p0,
                              DataSeparador = @p1,
                              StatusId = @p2,
                              DataConferente = @p3,
                              ModificadoPor = @p4,
                              ModificadoEm = @p5
                          WHERE Id = @p6
                            AND FilialId = @p7
                            AND StatusId IN (@p8, @p9)",
                        picker.Id,
                        agora,
                        statusLancamento,
                        dataConferente,
                        usuarioAtual,
                        agora,
                        romaneio.Id,
                        filialId,
                        StatusNaoGerado,
                        StatusAguardandoSeparacao);

                    if (rows == 0)
                    {
                        transaction.Rollback();
                        SetFlash("warning", "O romaneio já foi assumido por outro usuário ou não está mais disponível.");
                        return RedirectToAction("Index");
                    }

                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    SetFlash("danger", ex.Message);
                }
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult GerarRomaneios()
        {
            if (!CanAdministrar())
            {
                return new HttpStatusCodeResult(403, "Perfil sem permissão para gerar romaneios.");
            }

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    int maxRomaneio = db.Romaneio
                        .Where(r => r.FilialId == filialId)
                        .AsEnumerable()
                        .Select(r => ParseRomaneioNr(r.RomaneioNr))
                        .DefaultIfEmpty(0)
                        .Max();

                    string usuarioAtual = Util.GetCurrentUser();
                    DateTime agora = Util.GetCurrentDateTime();

                    var novos = new List<Romaneio>();
                    for (int i = 1; i <= 1000; i++)
                    {
                        novos.Add(new Romaneio
                        {
                            RomaneioNr = (maxRomaneio + i).ToString(CultureInfo.InvariantCulture),
                            VendedorId = UnknownAreaPedidoId,
                            StatusId = StatusNaoGerado,
                            FilialId = filialId,
                            CriadoPor = usuarioAtual,
                            CriadoEm = agora,
                            ModificadoPor = usuarioAtual,
                            ModificadoEm = agora
                        });
                    }

                    db.Romaneio.AddRange(novos);
                    db.SaveChanges();
                    transaction.Commit();
                    SetFlash("success", "1000 romaneios gerados com sucesso.");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    SetFlash("danger", ex.Message);
                }
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult FinalizarConferencia(int id)
        {
            if (!CanFinalizarConferencia())
            {
                return new HttpStatusCodeResult(403, "Perfil sem permissão para conferência.");
            }

            var usuarioAtual = GetUsuarioAtual();
            if (usuarioAtual == null)
            {
                SetFlash("danger", "Usuário atual não encontrado.");
                return RedirectToAction("Conferencia");
            }

            var agora = Util.GetCurrentDateTime();

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    db.Database.ExecuteSqlCommand(
                        @"UPDATE RomaneioItem
                          SET ConferenteId = @p0,
                              DataConferente = @p1
                          WHERE RomaneioId = @p2
                            AND FilialId = @p3",
                        usuarioAtual.Id,
                        agora,
                        id,
                        filialId);

                    int rows = db.Database.ExecuteSqlCommand(
                        @"UPDATE Romaneio
                          SET ConferenteId = @p0,
                              DataConferente = @p1,
                              StatusId = @p2,
                              ModificadoPor = @p3,
                              ModificadoEm = @p4
                          WHERE Id = @p5
                            AND FilialId = @p6
                            AND StatusId = @p7",
                        usuarioAtual.Id,
                        agora,
                        StatusFinalizado,
                        usuarioAtual.Login,
                        agora,
                        id,
                        filialId,
                        StatusEmSeparacao);

                    if (rows == 0)
                    {
                        transaction.Rollback();
                        SetFlash("warning", "O romaneio não est\u00E1 mais em separação.");
                        return RedirectToAction("Conferencia");
                    }

                    transaction.Commit();
                    SetFlash("success", "Conferência finalizada com sucesso.");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    SetFlash("danger", ex.Message);
                }
            }

            return RedirectToAction("Conferencia");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AlterarStatus(int id, int statusId)
        {
            if (!CanAdministrar())
            {
                return new HttpStatusCodeResult(403, "Perfil sem permissão para administração.");
            }

            var romaneio = db.Romaneio.FirstOrDefault(r => r.Id == id && r.FilialId == filialId);
            if (romaneio == null)
            {
                SetFlash("danger", "Romaneio não localizado.");
                return RedirectToAction("Administracao");
            }

            bool statusExiste = db.StatusRomaneio.Any(s => s.Id == statusId);
            if (!statusExiste)
            {
                SetFlash("danger", "Status informado não existe.");
                return RedirectToAction("Administracao");
            }

            ApplyStatusRegression(romaneio, statusId);
            romaneio.StatusId = statusId;
            romaneio.ModificadoPor = Util.GetCurrentUser();
            romaneio.ModificadoEm = Util.GetCurrentDateTime();

            db.Entry(romaneio).State = EntityState.Modified;
            db.SaveChanges();

            SetFlash("success", "Status atualizado com sucesso.");
            return RedirectToAction("Administracao");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UploadPlanilha(HttpPostedFileBase arquivo, DateTime? dataInicial, DateTime? dataFinal)
        {
            if (!CanAdministrar())
            {
                return new HttpStatusCodeResult(403, "Perfil sem permissão para atualização.");
            }

            var periodo = ResolvePeriodo(dataInicial, dataFinal);

            if (arquivo == null || arquivo.ContentLength == 0)
            {
                SetFlash("danger", "Selecione uma planilha antes de importar.");
                return RedirectToAction("Atualizacao", new { dataInicial = periodo.Item1.ToString("yyyy-MM-dd"), dataFinal = periodo.Item2.ToString("yyyy-MM-dd") });
            }

            var summary = new RomaneioImportSummaryViewModel();
            bool importacaoConcluida = false;
            int finalizadosAutomaticamente = 0;

            try
            {
                var linhasLidas = ReadImportRows(arquivo);
                foreach (var linhaInvalida in linhasLidas.Where(l => l == null || string.IsNullOrWhiteSpace(l.RomaneioNr)))
                {
                    summary.Erros++;
                    summary.Mensagens.Add("Linha ignorada: Romaneio é obrigatório.");
                }

                foreach (var linhaInvalida in linhasLidas.Where(l => l != null && !string.IsNullOrWhiteSpace(l.RomaneioNr) && string.IsNullOrWhiteSpace(l.ItemNr)))
                {
                    summary.Erros++;
                    summary.Mensagens.Add("Linha ignorada para RomaneioItem: Item Estoque obrigatorio.");
                }

                var linhas = AggregateImportRows(linhasLidas);
                var linhasItens = AggregateImportItemRows(linhasLidas);
                var usuarioAtual = Util.GetCurrentUser();
                var agora = Util.GetCurrentDateTime();

                var areaPedidos = LoadAreaPedidosConfiguracao();
                var areaPedidosById = areaPedidos
                    .GroupBy(a => a.Id)
                    .ToDictionary(g => g.Key, g => g.First());
                var areaPedidosByUsuario = areaPedidos
                    .Where(a => !string.IsNullOrWhiteSpace(a.UsuarioApollo))
                    .GroupBy(a => a.UsuarioApollo.Trim(), StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
                AreaPedido fallbackAreaPedido;
                if (!areaPedidosById.TryGetValue(UnknownAreaPedidoId, out fallbackAreaPedido))
                {
                    throw new InvalidOperationException("AreaPedido 1 (Nao Identificado) nao encontrado.");
                }

                var areaRomaneios = LoadAreaRomaneiosConfiguracao();
                var areaRomaneiosById = areaRomaneios
                    .GroupBy(a => a.Id)
                    .ToDictionary(g => g.Key, g => g.First());
                var areaRomaneiosByName = areaRomaneios
                    .Where(a => !string.IsNullOrWhiteSpace(a.Area))
                    .GroupBy(a => a.Area.Trim(), StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
                var romaneios = db.Romaneio.Where(r => r.FilialId == filialId).ToList();
                var romaneiosElegiveisParaImportacao = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                using (var transaction = db.Database.BeginTransaction())
                {
                    foreach (var linha in linhas)
                    {
                        summary.Processados++;

                        if (string.IsNullOrWhiteSpace(linha.RomaneioNr))
                        {
                            summary.Erros++;
                            summary.Mensagens.Add("Linha ignorada: Romaneio é obrigatório.");
                            continue;
                        }

                        var romaneio = romaneios.FirstOrDefault(r =>
                            string.Equals((r.RomaneioNr ?? string.Empty).Trim(), linha.RomaneioNr.Trim(), StringComparison.OrdinalIgnoreCase));
                        bool statusAlteradoParaAguardandoSeparacao = false;

                        if (romaneio == null)
                        {
                            romaneio = new Romaneio
                            {
                                RomaneioNr = linha.RomaneioNr.Trim(),
                                FilialId = filialId,
                                StatusId = StatusNaoGerado,
                                CriadoPor = usuarioAtual,
                                CriadoEm = agora
                            };

                            db.Romaneio.Add(romaneio);
                            romaneios.Add(romaneio);
                            summary.Criados++;
                        }
                        else
                        {
                            summary.Atualizados++;
                            romaneio.DataEmissao = agora;
                        }

                        romaneiosElegiveisParaImportacao.Add((romaneio.RomaneioNr ?? string.Empty).Trim());

                        romaneio.Contato = linha.ContatoNr ?? romaneio.Contato;

                        AreaPedido areaPedido = null;
                        if (!string.IsNullOrWhiteSpace(linha.Vendedor))
                        {
                            areaPedidosByUsuario.TryGetValue(linha.Vendedor.Trim(), out areaPedido);
                            if (areaPedido == null)
                            {
                                areaPedido = fallbackAreaPedido;
                                summary.Mensagens.Add("Romaneio " + linha.RomaneioNr + ": Vendedor '" + linha.Vendedor + "' não encontrado em AreaPedido. Usando 'Nao Identificado'.");
                            }
                        }
                        else if (romaneio.VendedorId.HasValue)
                        {
                            areaPedidosById.TryGetValue(romaneio.VendedorId.Value, out areaPedido);
                        }

                        if (areaPedido == null)
                        {
                            areaPedido = fallbackAreaPedido;
                        }

                        romaneio.VendedorId = areaPedido.Id;

                        var areaRomaneio = ResolveAreaRomaneio(areaPedido, areaRomaneiosById, areaRomaneiosByName);
                        if (areaRomaneio != null && romaneio.StatusId == StatusNaoGerado)
                        {
                            romaneio.StatusId = StatusAguardandoSeparacao;
                            romaneio.SeparadorId = null;
                            romaneio.DataSeparador = null;
                            romaneio.ConferenteId = null;
                            romaneio.DataConferente = null;
                            statusAlteradoParaAguardandoSeparacao = true;
                        }

                        if (statusAlteradoParaAguardandoSeparacao)
                        {
                            summary.StatusAlterados++;
                        }

                        romaneio.Itens = linha.Itens ?? romaneio.Itens;
                        romaneio.Pecas = linha.Pecas ?? romaneio.Pecas;
                        romaneio.ModificadoPor = usuarioAtual;
                        romaneio.ModificadoEm = agora;
                    }

                    db.SaveChanges();

                    var romaneiosByNumero = romaneios
                        .Where(r => !string.IsNullOrWhiteSpace(r.RomaneioNr))
                        .GroupBy(r => r.RomaneioNr.Trim(), StringComparer.OrdinalIgnoreCase)
                        .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
                    var romaneioIdsAtualizados = linhasItens
                        .Where(l =>
                            !string.IsNullOrWhiteSpace(l.RomaneioNr) &&
                            romaneiosElegiveisParaImportacao.Contains(l.RomaneioNr.Trim()) &&
                            romaneiosByNumero.ContainsKey(l.RomaneioNr.Trim()))
                        .Select(l => romaneiosByNumero[l.RomaneioNr.Trim()].Id)
                        .Distinct()
                        .ToList();

                    if (romaneioIdsAtualizados.Any())
                    {
                        var itensExistentes = db.RomaneioItem
                            .Where(i => romaneioIdsAtualizados.Contains(i.RomaneioId))
                            .ToList();

                        if (itensExistentes.Any())
                        {
                            db.RomaneioItem.RemoveRange(itensExistentes);
                        }

                        var itensImportados = linhasItens
                            .Where(l =>
                                !string.IsNullOrWhiteSpace(l.RomaneioNr) &&
                                romaneiosElegiveisParaImportacao.Contains(l.RomaneioNr.Trim()) &&
                                romaneiosByNumero.ContainsKey(l.RomaneioNr.Trim()))
                            .Select(l => new RomaneioItem
                            {
                                RomaneioId = romaneiosByNumero[l.RomaneioNr.Trim()].Id,
                                ItemNr = (l.ItemNr ?? string.Empty).Trim(),
                                Qtde = l.Pecas,
                                FilialId = filialId,
                                CriadoPor = usuarioAtual,
                                CriadoEm = agora
                            })
                            .ToList();

                        if (itensImportados.Any())
                        {
                            db.RomaneioItem.AddRange(itensImportados);
                        }
                    }

                    foreach (var romaneioImportado in romaneios
                        .Where(r =>
                            r.StatusId == StatusEmSeparacao &&
                            !string.IsNullOrWhiteSpace(r.RomaneioNr) &&
                            romaneiosElegiveisParaImportacao.Contains(r.RomaneioNr.Trim()))
                        .ToList())
                    {
                        if (!romaneioImportado.VendedorId.HasValue)
                        {
                            continue;
                        }

                        AreaPedido areaPedidoAtual;
                        if (!areaPedidosById.TryGetValue(romaneioImportado.VendedorId.Value, out areaPedidoAtual))
                        {
                            continue;
                        }

                        var areaRomaneioAtual = ResolveAreaRomaneio(areaPedidoAtual, areaRomaneiosById, areaRomaneiosByName);
                        if (areaRomaneioAtual == null || (areaRomaneioAtual.Conferir ?? true))
                        {
                            continue;
                        }

                        romaneioImportado.StatusId = StatusFinalizado;
                        if (!romaneioImportado.DataConferente.HasValue)
                        {
                            romaneioImportado.DataConferente = agora;
                        }

                        romaneioImportado.ModificadoPor = usuarioAtual;
                        romaneioImportado.ModificadoEm = agora;
                        finalizadosAutomaticamente++;
                    }

                    db.SaveChanges();
                    transaction.Commit();
                    importacaoConcluida = true;

                    summary.Mensagens.Add(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "Resumo do processamento: {0} registro(s) atualizado(s) e {1} status alterado(s) de 1 para 2.",
                            summary.Atualizados,
                            summary.StatusAlterados));

                    if (finalizadosAutomaticamente > 0)
                    {
                        summary.Mensagens.Add(
                            string.Format(
                                CultureInfo.InvariantCulture,
                                "Finalização automática: {0} romaneio(s) alterado(s) de 3 para 4 por área sem conferência.",
                                finalizadosAutomaticamente));
                    }

                    Trace.TraceInformation(
                        "Romaneios Atualizacoes - FilialId={0}; Usuario={1}; Processados={2}; Atualizados={3}; Criados={4}; StatusAlterados1Para2={5}; FinalizadosAutomaticamente3Para4={6}; Erros={7}",
                        filialId,
                        usuarioAtual,
                        summary.Processados,
                        summary.Atualizados,
                        summary.Criados,
                        summary.StatusAlterados,
                        finalizadosAutomaticamente,
                        summary.Erros);
                }
            }
            catch (Exception ex)
            {
                summary.Erros++;
                summary.Mensagens.Add(ex.Message);
            }

            TempData["RomaneioImportSummary"] = summary;
            TempData["PromptGerarMapa"] = importacaoConcluida && (summary.Atualizados + summary.Criados) > 0;
            return RedirectToAction("Atualizacao", new { dataInicial = periodo.Item1.ToString("yyyy-MM-dd"), dataFinal = periodo.Item2.ToString("yyyy-MM-dd") });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UploadPlanilhaAlocacao(HttpPostedFileBase arquivo, DateTime? dataInicial, DateTime? dataFinal)
        {
            if (!CanAdministrar())
            {
                return new HttpStatusCodeResult(403, "Perfil sem permissao para alocacao.");
            }

            var periodo = ResolvePeriodo(dataInicial, dataFinal);

            if (arquivo == null || arquivo.ContentLength == 0)
            {
                SetFlash("danger", "Selecione uma planilha antes de importar.");
                return RedirectToAction("AlocacaoZona", new { dataInicial = periodo.Item1.ToString("yyyy-MM-dd"), dataFinal = periodo.Item2.ToString("yyyy-MM-dd") });
            }

            var resumo = new AlocacaoZonaResumoViewModel();

            try
            {
                EnsureAlocacaoSchema();

                var linhasLidas = ReadImportRows(arquivo, "ArquivoExportacao");
                var linhas = AggregateImportRows(linhasLidas);
                var linhasItens = AggregateAllocationItemRows(linhasLidas);
                var usuarioAtual = Util.GetCurrentUser();
                var agora = Util.GetCurrentDateTime();
                var areaPedidos = LoadAreaPedidosConfiguracao();
                var areaPedidosById = areaPedidos.GroupBy(a => a.Id).ToDictionary(g => g.Key, g => g.First());
                var areaPedidosByUsuario = areaPedidos
                    .Where(a => !string.IsNullOrWhiteSpace(a.UsuarioApollo))
                    .GroupBy(a => a.UsuarioApollo.Trim(), StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
                var fallbackAreaPedido = areaPedidosById.ContainsKey(UnknownAreaPedidoId) ? areaPedidosById[UnknownAreaPedidoId] : null;
                var areaRomaneios = LoadAreaRomaneiosConfiguracao();
                var areaRomaneiosById = areaRomaneios.GroupBy(a => a.Id).ToDictionary(g => g.Key, g => g.First());
                var areaRomaneiosByName = areaRomaneios
                    .Where(a => !string.IsNullOrWhiteSpace(a.Area))
                    .GroupBy(a => a.Area.Trim(), StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
                var possuiAreaComAlocar = areaRomaneios.Any(a => a.Alocar ?? false);
                var romaneios = db.Romaneio.Where(r => r.FilialId == filialId).ToList();
                using (var transaction = db.Database.BeginTransaction())
                {
                    foreach (var linha in linhas)
                    {
                        if (string.IsNullOrWhiteSpace(linha.RomaneioNr))
                        {
                            continue;
                        }

                        var romaneio = romaneios.FirstOrDefault(r =>
                            string.Equals((r.RomaneioNr ?? string.Empty).Trim(), linha.RomaneioNr.Trim(), StringComparison.OrdinalIgnoreCase));

                        if (romaneio == null)
                        {
                            romaneio = new Romaneio
                            {
                                RomaneioNr = linha.RomaneioNr.Trim(),
                                FilialId = filialId,
                                StatusId = StatusNaoGerado,
                                CriadoPor = usuarioAtual,
                                CriadoEm = agora
                            };

                            db.Romaneio.Add(romaneio);
                            romaneios.Add(romaneio);
                        }

                        romaneio.Contato = linha.ContatoNr ?? romaneio.Contato;
                        romaneio.DataEmissao = linha.DataFaturamento ?? agora;
                        romaneio.Itens = linha.Itens ?? romaneio.Itens;
                        romaneio.Pecas = linha.Pecas ?? romaneio.Pecas;
                        romaneio.StatusId = StatusNaoGerado;
                        romaneio.ModificadoPor = usuarioAtual;
                        romaneio.ModificadoEm = agora;

                        AreaPedido areaPedido = null;
                        if (!string.IsNullOrWhiteSpace(linha.Vendedor))
                        {
                            areaPedidosByUsuario.TryGetValue(linha.Vendedor.Trim(), out areaPedido);
                        }

                        if (areaPedido == null && romaneio.VendedorId.HasValue)
                        {
                            areaPedidosById.TryGetValue(romaneio.VendedorId.Value, out areaPedido);
                        }

                        if (areaPedido == null)
                        {
                            areaPedido = fallbackAreaPedido;
                        }

                        if (areaPedido != null)
                        {
                            romaneio.VendedorId = areaPedido.Id;
                        }
                    }

                    db.SaveChanges();

                    foreach (var linha in linhas.Where(x => !string.IsNullOrWhiteSpace(x.RomaneioNr) && !string.IsNullOrWhiteSpace(x.OS)))
                    {
                        var romaneio = romaneios.FirstOrDefault(r =>
                            string.Equals((r.RomaneioNr ?? string.Empty).Trim(), linha.RomaneioNr.Trim(), StringComparison.OrdinalIgnoreCase));
                        if (romaneio == null)
                        {
                            continue;
                        }

                        db.Database.ExecuteSqlCommand(
                            "UPDATE Romaneio SET OS = @p0 WHERE Id = @p1",
                            linha.OS.Trim(),
                            romaneio.Id);
                    }

                    var romaneiosByNumero = romaneios
                        .Where(r => !string.IsNullOrWhiteSpace(r.RomaneioNr))
                        .GroupBy(r => r.RomaneioNr.Trim(), StringComparer.OrdinalIgnoreCase)
                        .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

                    var romaneioIdsAtualizados = linhasItens
                        .Where(l => !string.IsNullOrWhiteSpace(l.RomaneioNr) && romaneiosByNumero.ContainsKey(l.RomaneioNr.Trim()))
                        .Select(l => romaneiosByNumero[l.RomaneioNr.Trim()].Id)
                        .Distinct()
                        .ToList();

                    var romaneioIdsElegiveisAlocacao = romaneiosByNumero.Values
                        .Where(r =>
                        {
                            if (!romaneioIdsAtualizados.Contains(r.Id) || r.StatusId != StatusNaoGerado)
                            {
                                return false;
                            }

                            AreaPedido areaPedido = null;
                            if (r.VendedorId.HasValue)
                            {
                                areaPedidosById.TryGetValue(r.VendedorId.Value, out areaPedido);
                            }

                            var areaInfo = ResolveAreaRomaneio(areaPedido, areaRomaneiosById, areaRomaneiosByName);
                            return IsAreaHabilitadaParaAlocacao(areaInfo, possuiAreaComAlocar);
                        })
                        .Select(r => r.Id)
                        .Distinct()
                        .ToList();

                    if (romaneioIdsAtualizados.Any())
                    {
                        var itensExistentes = db.RomaneioItem.Where(i => romaneioIdsAtualizados.Contains(i.RomaneioId)).ToList();
                        if (itensExistentes.Any())
                        {
                            db.RomaneioItem.RemoveRange(itensExistentes);
                            db.SaveChanges();
                        }

                        foreach (var item in linhasItens.Where(l => !string.IsNullOrWhiteSpace(l.RomaneioNr) && romaneiosByNumero.ContainsKey(l.RomaneioNr.Trim())))
                        {
                            db.Database.ExecuteSqlCommand(
                                @"INSERT INTO RomaneioItem (RomaneioId, ItemNr, Qtde, Descricao, ValorUnitario, ValorTotal, TarefaNr, FilialId, CriadoPor, CriadoEm)
                                  VALUES (@p0, @p1, @p2, @p3, @p4, @p5, NULL, @p6, @p7, @p8)",
                                romaneiosByNumero[item.RomaneioNr.Trim()].Id,
                                (item.ItemNr ?? string.Empty).Trim(),
                                (object)item.Pecas ?? DBNull.Value,
                                (object)item.Descricao ?? DBNull.Value,
                                (object)item.ValorUnitario ?? DBNull.Value,
                                (object)item.ValorTotal ?? DBNull.Value,
                                filialId,
                                usuarioAtual,
                                agora);
                        }

                        AtualizarBaseAlocacaoRomaneios(romaneioIdsAtualizados);
                    }

                    var resultadoAlocacao = GerarTarefasPorZona(romaneioIdsElegiveisAlocacao, usuarioAtual);
                    resumo.RomaneiosAtualizados = romaneioIdsAtualizados.Count;
                    resumo.ItensImportados = linhasItens.Count;
                    resumo.TarefasGeradas = resultadoAlocacao.TarefasGeradas;
                    resumo.ItensAlocados = resultadoAlocacao.ItensAlocados;
                    resumo.ItensSemLocacao = resultadoAlocacao.ItensSemLocacao;
                    resumo.ItensSemZona = resultadoAlocacao.ItensSemZona;

                    var romaneiosNaoElegiveis = romaneioIdsAtualizados
                        .Except(romaneioIdsElegiveisAlocacao)
                        .ToList();

                    if (romaneiosNaoElegiveis.Any())
                    {
                        var numerosNaoElegiveis = romaneiosByNumero.Values
                            .Where(r => romaneiosNaoElegiveis.Contains(r.Id))
                            .Select(r => r.RomaneioNr)
                            .Where(r => !string.IsNullOrWhiteSpace(r))
                            .Distinct()
                            .OrderBy(r => r)
                            .ToList();

                        foreach (var romaneioNr in numerosNaoElegiveis)
                        {
                            resumo.Mensagens.Add("Romaneio " + romaneioNr + " nao alocado porque a area nao esta habilitada para alocacao.");
                        }
                    }

                    foreach (var mensagem in resultadoAlocacao.Mensagens)
                    {
                        resumo.Mensagens.Add(mensagem);
                    }

                    transaction.Commit();
                }
            }
            catch (Exception ex)
            {
                LogImportError("UploadPlanilhaAlocacao", "Processar alocacao por zona", ex.Message);
                resumo.Mensagens.Add(ex.Message);
            }

            TempData["AlocacaoZonaResumo"] = resumo;
            return RedirectToAction("AlocacaoZona", new { dataInicial = periodo.Item1.ToString("yyyy-MM-dd"), dataFinal = periodo.Item2.ToString("yyyy-MM-dd") });
        }

        public ActionResult GerarMapa()
        {
            if (!CanViewFullMenu())
            {
                return new HttpStatusCodeResult(403, "Perfil sem permissão para gerar mapa.");
            }

            var areaPedidos = LoadAreaPedidosConfiguracao()
                .GroupBy(a => a.Id)
                .ToDictionary(g => g.Key, g => g.First());

            var areaRomaneios = LoadAreaRomaneiosConfiguracao();
            var areaRomaneiosById = areaRomaneios
                .GroupBy(a => a.Id)
                .ToDictionary(g => g.Key, g => g.First());
            var areaRomaneiosByName = areaRomaneios
                .Where(a => !string.IsNullOrWhiteSpace(a.Area))
                .GroupBy(a => a.Area.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var romaneios = db.Romaneio
                .Where(r => r.FilialId == filialId && r.StatusId == StatusAguardandoSeparacao)
                .Select(r => new { r.Id, r.RomaneioNr, r.VendedorId })
                .ToList();

            romaneios = romaneios
                .Where(r =>
                {
                    AreaPedido areaPedido = null;
                    if (r.VendedorId.HasValue)
                    {
                        areaPedidos.TryGetValue(r.VendedorId.Value, out areaPedido);
                    }

                    var areaRomaneio = ResolveAreaRomaneio(areaPedido, areaRomaneiosById, areaRomaneiosByName);
                    return CanIncludeInMapa(areaRomaneio);
                })
                .ToList();

            if (!romaneios.Any())
            {
                SetFlash("warning", "Nenhum romaneio com status 2 foi localizado para gerar o MAPA.");
                return RedirectToAction("Atualizacao");
            }

            var romaneioById = romaneios.ToDictionary(r => r.Id, r => r.RomaneioNr ?? string.Empty);
            var romaneiosRelacionados = romaneios
                .Select(r => (r.RomaneioNr ?? string.Empty).Trim())
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(ParseRomaneioNr)
                .ThenBy(r => r)
                .ToList();
            var romaneioIds = romaneioById.Keys.ToList();
            var itensRomaneio = db.RomaneioItem
                .Where(i => romaneioIds.Contains(i.RomaneioId))
                .ToList();

            if (!itensRomaneio.Any())
            {
                SetFlash("warning", "Nenhum item foi localizado para os romaneios com status 2.");
                return RedirectToAction("Atualizacao");
            }

            var estoquePorItem = Util.GetItensEstoque(filialId, db)
                .Where(i => !string.IsNullOrWhiteSpace(i.ItemNr))
                .GroupBy(i => i.ItemNr.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderBy(i => i.Locacao ?? string.Empty).First(),
                    StringComparer.OrdinalIgnoreCase);

            var linhas = itensRomaneio
                .Where(i => !string.IsNullOrWhiteSpace(i.ItemNr) && romaneioById.ContainsKey(i.RomaneioId))
                .Select(i =>
                {
                    SP_GetItensEstoque_Result estoqueInfo;
                    estoquePorItem.TryGetValue(i.ItemNr.Trim(), out estoqueInfo);

                    return new MapaLinhaViewModel
                    {
                        Locacao = estoqueInfo != null ? estoqueInfo.Locacao : string.Empty,
                        ItemNr = i.ItemNr.Trim(),
                        Descricao = estoqueInfo != null ? estoqueInfo.Descricao : string.Empty,
                        Qtde = i.Qtde ?? 0,
                        RomaneioNr = romaneioById[i.RomaneioId]
                    };
                })
                .GroupBy(l => new
                {
                    l.Locacao,
                    l.ItemNr,
                    l.Descricao
                })
                .Select(g => new MapaLinhaViewModel
                {
                    Locacao = g.Key.Locacao,
                    ItemNr = g.Key.ItemNr,
                    Descricao = g.Key.Descricao,
                    Qtde = g.Sum(x => x.Qtde)
                })
                .OrderBy(l => l.Locacao ?? string.Empty)
                .ThenBy(l => l.ItemNr ?? string.Empty)
                .ToList();

            if (!linhas.Any())
            {
                SetFlash("warning", "Nenhum item valido foi localizado para gerar o MAPA.");
                return RedirectToAction("Atualizacao");
            }

            int mapaBreakLength = GetMapaBreakLength();
            var bytes = BuildMapaPdf(linhas, romaneiosRelacionados, mapaBreakLength);
            string fileName = "mapa-separacao-" + Util.GetCurrentDateTime().ToString("yyyyMMddHHmmss") + ".pdf";
            return File(bytes, "application/pdf", fileName);
        }

        public ActionResult ExportarPendentes(DateTime? dataInicial, DateTime? dataFinal)
        {
            if (!CanAdministrar())
            {
                return new HttpStatusCodeResult(403, "Perfil sem permissão para exportação.");
            }

            var periodo = ResolvePeriodo(dataInicial, dataFinal);
            var itens = LoadRomaneioGridItems()
                .Where(i => (i.StatusId == StatusNaoGerado || (i.StatusId == StatusAguardandoSeparacao && !i.PossuiVendedor)) && IsWithinPeriodo(i.DataRomaneio, periodo.Item1, periodo.Item2))
                .OrderBy(i => i.Prioridade ?? int.MaxValue)
                .ThenBy(i => ParseRomaneioNr(i.RomaneioNr))
                .ToList();

            var csv = new StringBuilder();
            csv.AppendLine("RomaneioNr;Area;Prioridade;UsuarioApollo;DataRomaneio;Status");
            foreach (var item in itens)
            {
                csv.AppendLine(string.Join(";",
                    CsvValue(item.RomaneioNr),
                    CsvValue(item.Area),
                    CsvValue(item.Prioridade.HasValue ? item.Prioridade.Value.ToString(CultureInfo.InvariantCulture) : string.Empty),
                    CsvValue(item.UsuarioApollo),
                    CsvValue(item.DataRomaneio.HasValue ? item.DataRomaneio.Value.ToString("dd/MM/yyyy HH:mm:ss") : string.Empty),
                    CsvValue(item.Status)));
            }

            byte[] bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
            string fileName = "romaneios-pendentes-" + Util.GetCurrentDateTime().ToString("yyyyMMddHHmmss") + ".csv";
            return File(bytes, "text/csv", fileName);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult FinalizarNaoGerados(int? romaneioInicio, int? romaneioFinal)
        {
            if (!CanFinalizarNaoGeradosExportados())
            {
                return new HttpStatusCodeResult(403, "Perfil sem permissão para atualização.");
            }

            var intervalo = ResolveRomaneioInterval(romaneioInicio, romaneioFinal);
            var exportacaoIds = GetExportacaoNaoGeradosIds();
            int filialAtualId = filialId;
            if (!exportacaoIds.Any())
            {
                SetFlash("warning", "Nenhum romaneio exportado foi localizado na sessão atual.");
                return RedirectToAction("AnaliseNaoGerados", new
                {
                    romaneioInicio = intervalo.Item1,
                    romaneioFinal = intervalo.Item2
                });
            }

            string usuarioAtual = Util.GetCurrentUser();
            DateTime agora = Util.GetCurrentDateTime();

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    var romaneios = db.Romaneio
                        .Where(r => r.FilialId == filialAtualId && exportacaoIds.Contains(r.Id))
                        .ToList();

                    if (romaneios.Count != exportacaoIds.Count)
                    {
                        transaction.Rollback();
                        SetFlash("danger", "A lista exportada não está mais disponível integralmente para finalização.");
                        return RedirectToAction("AnaliseNaoGerados", new
                        {
                            romaneioInicio = intervalo.Item1,
                            romaneioFinal = intervalo.Item2
                        });
                    }

                    if (romaneios.Any(r => r.StatusId != StatusNaoGerado))
                    {
                        transaction.Rollback();
                        SetFlash("danger", "Nem todos os romaneios exportados continuam com status pendente. A finalização foi cancelada.");
                        return RedirectToAction("AnaliseNaoGerados", new
                        {
                            romaneioInicio = intervalo.Item1,
                            romaneioFinal = intervalo.Item2
                        });
                    }

                    foreach (var romaneio in romaneios)
                    {
                        romaneio.StatusId = StatusNaoGeradoAnalise;
                        romaneio.ModificadoPor = usuarioAtual;
                        romaneio.ModificadoEm = agora;
                    }

                    db.SaveChanges();
                    transaction.Commit();
                    ClearExportacaoNaoGeradosIds();
                    ClearExportacaoNaoGeradosArquivo();
                    ClearExportacaoNaoGeradosFlowFlags();
                    SetFlash("success", romaneios.Count.ToString(CultureInfo.InvariantCulture) + " romaneio(s) finalizado(s) com sucesso.");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    SetFlash("danger", "Não foi possível finalizar os romaneios exportados. " + ex.Message);
                }
            }

            return RedirectToAction("AnaliseNaoGerados", new
            {
                romaneioInicio = intervalo.Item1,
                romaneioFinal = intervalo.Item2
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ExportarNaoGeradosAnalise(int? romaneioInicio, int? romaneioFinal)
        {
            if (!CanExportarNaoGerados())
            {
                return new HttpStatusCodeResult(403, "Perfil sem permissão para exportação.");
            }

            var intervalo = ResolveRomaneioInterval(romaneioInicio, romaneioFinal);
            SetUltimoIntervaloNaoGerados(intervalo.Item1, intervalo.Item2);
            int filialAtualId = filialId;
            if (intervalo.Item1 <= 0 && intervalo.Item2 <= 0)
            {
                ClearExportacaoNaoGeradosIds();
                ClearExportacaoNaoGeradosArquivo();
                ClearExportacaoNaoGeradosFlowFlags();
                SetFlash("warning", "Informe o intervalo de romaneios para exportar.");
                return RedirectToAction("AnaliseNaoGerados", new
                {
                    romaneioInicio = intervalo.Item1,
                    romaneioFinal = intervalo.Item2
                });
            }

            if (filialAtualId <= 0)
            {
                ClearExportacaoNaoGeradosIds();
                ClearExportacaoNaoGeradosArquivo();
                ClearExportacaoNaoGeradosFlowFlags();
                SetFlash("danger", "Não foi possível identificar a filial do usuário para a exportação.");
                return RedirectToAction("AnaliseNaoGerados", new
                {
                    romaneioInicio = intervalo.Item1,
                    romaneioFinal = intervalo.Item2
                });
            }

            var itens = LoadRomaneiosNaoGeradosParaExportacao(filialAtualId, intervalo.Item1, intervalo.Item2)
                .Select(r => new
                {
                    r.Id,
                    r.RomaneioNr
                })
                .ToList();

            if (!itens.Any())
            {
                ClearExportacaoNaoGeradosIds();
                ClearExportacaoNaoGeradosArquivo();
                ClearExportacaoNaoGeradosFlowFlags();
                SetFlash("warning", "Nenhum romaneio não gerado foi localizado para exportação.");
                return RedirectToAction("AnaliseNaoGerados", new
                {
                    romaneioInicio = intervalo.Item1,
                    romaneioFinal = intervalo.Item2
                });
            }

            SetExportacaoNaoGeradosIds(itens.Select(i => i.Id));
            string fileName = "romaneios-nao-gerados-" + Util.GetCurrentDateTime().ToString("yyyyMMddHHmmss") + ".xls";
            SetExportacaoNaoGeradosArquivo(BuildNaoGeradosExportBytes(itens.Select(i => i.RomaneioNr)), fileName);
            SetExportacaoNaoGeradosFlowFlags(true);
            SetFlash("success", itens.Count.ToString(CultureInfo.InvariantCulture) + " romaneio(s) localizado(s) para exportação.");
            return RedirectToAction("AnaliseNaoGerados", new
            {
                romaneioInicio = intervalo.Item1,
                romaneioFinal = intervalo.Item2,
                triggerDownload = true,
                exportNonce = Guid.NewGuid().ToString("N")
            });
        }

        public ActionResult DownloadNaoGeradosAnalise()
        {
            if (!CanExportarNaoGerados())
            {
                return new HttpStatusCodeResult(403, "Perfil sem permissão para exportação.");
            }

            var arquivoEmMemoria = GetExportacaoNaoGeradosArquivo();
            var nomeArquivoEmMemoria = GetExportacaoNaoGeradosArquivoNome();
            if (arquivoEmMemoria != null && arquivoEmMemoria.Length > 0)
            {
                ClearExportacaoNaoGeradosArquivo();
                return File(
                    arquivoEmMemoria,
                    "application/vnd.ms-excel",
                    string.IsNullOrWhiteSpace(nomeArquivoEmMemoria)
                        ? "romaneios-nao-gerados-" + Util.GetCurrentDateTime().ToString("yyyyMMddHHmmss") + ".xls"
                        : nomeArquivoEmMemoria);
            }

            var ids = GetExportacaoNaoGeradosIds();
            int filialAtualId = filialId;
            if (!ids.Any())
            {
                return new HttpStatusCodeResult(404, "Nenhum romaneio exportado foi localizado na sessão atual.");
            }

            var itens = db.Romaneio
                .Where(r => r.FilialId == filialAtualId && ids.Contains(r.Id))
                .ToList()
                .OrderBy(r => ParseRomaneioNr(r.RomaneioNr))
                .ThenBy(r => (r.RomaneioNr ?? string.Empty).Trim())
                .ToList();

            if (!itens.Any())
            {
                return new HttpStatusCodeResult(404, "Nenhum romaneio exportado foi localizado na sessão atual.");
            }

            byte[] bytes = BuildNaoGeradosExportBytes(itens.Select(item => item.RomaneioNr));
            string fileName = "romaneios-nao-gerados-" + Util.GetCurrentDateTime().ToString("yyyyMMddHHmmss") + ".xls";
            return File(bytes, "application/vnd.ms-excel", fileName);
        }

        private RomaneioViewModel BuildSeparacaoViewModel()
        {
            var itens = LoadRomaneioGridItems();
            return CreateBaseViewModel(itens, Util.GetCurrentDateTime().Date.AddDays(-30), Util.GetCurrentDateTime().Date)
                .With(vm =>
                {
                    vm.RomaneioDDL = itens
                        .Where(i => i.StatusId == StatusNaoGerado || i.StatusId == StatusAguardandoSeparacao)
                        .OrderBy(i => i.Prioridade ?? int.MaxValue)
                        .ThenBy(i => ParseRomaneioNr(i.RomaneioNr))
                        .Select(i => new SelectListItem
                        {
                            Value = i.Id.ToString(CultureInfo.InvariantCulture),
                            Text = string.Format("{0} - {1} - {2}", i.RomaneioNr, string.IsNullOrWhiteSpace(i.Area) ? "Sem área" : i.Area, i.Status)
                        })
                        .ToList();

                    vm.PickerDDL = BuildPickerQuery()
                        .OrderBy(u => u.Nome)
                        .ToList()
                        .Select(u => new SelectListItem
                        {
                            Value = u.Id.ToString(CultureInfo.InvariantCulture),
                            Text = u.Nome
                        })
                        .ToList();

                    vm.GridItems = itens
                        .Where(i => i.StatusId == StatusNaoGerado || i.StatusId == StatusAguardandoSeparacao)
                        .OrderBy(i => i.Prioridade ?? int.MaxValue)
                        .ThenBy(i => ParseRomaneioNr(i.RomaneioNr))
                        .ToList();
                });
        }

        private RomaneioViewModel BuildConferenciaViewModel()
        {
            var itens = LoadRomaneioGridItems();
            return CreateBaseViewModel(itens, Util.GetCurrentDateTime().Date.AddDays(-30), Util.GetCurrentDateTime().Date)
                .With(vm =>
                {
                    vm.GridItems = itens
                        .Where(i => i.StatusId == StatusEmSeparacao)
                        .OrderBy(i => i.Prioridade ?? int.MaxValue)
                        .ThenBy(i => i.DataPicker ?? DateTime.MaxValue)
                        .ThenBy(i => ParseRomaneioNr(i.RomaneioNr))
                        .ToList();
                });
        }

        private IQueryable<Usuario> BuildPickerQuery()
        {
            return db.Usuario.Where(u =>
                u.FilialId == filialId &&
                AllowedPickerProfileIds.Contains(u.PerfilId));
        }

        private RomaneioViewModel BuildAdministracaoViewModel()
        {
            var itens = LoadRomaneioGridItems();
            DateTime hoje = Util.GetCurrentDateTime().Date;
            return CreateBaseViewModel(itens, Util.GetCurrentDateTime().Date.AddDays(-30), Util.GetCurrentDateTime().Date)
                .With(vm =>
                {
                    vm.StatusDDL = BuildStatusDDL();
                    vm.GridItems = itens
                        .Where(i => i.StatusId == StatusFinalizado)
                        .Where(i => i.DataConferencia.HasValue && i.DataConferencia.Value.Date == hoje)
                        .OrderByDescending(i => i.DataConferencia ?? DateTime.MinValue)
                        .ThenBy(i => ParseRomaneioNr(i.RomaneioNr))
                        .ToList();
                });
        }

        private RomaneioViewModel BuildAnaliseNaoGeradosViewModel(int romaneioInicio, int romaneioFinal, bool triggerDownload)
        {
            DateTime limiteAlteracao = Util.GetCurrentDateTime().AddDays(-30);
            var itens = LoadRomaneioGridItems(StatusNaoGeradoAnalise, limiteAlteracao);
            var exportacaoAtualIds = GetExportacaoNaoGeradosIds();
            bool triggerDownloadFlag = ConsumeTriggerDownloadNaoGerados();
            return CreateBaseViewModel(itens, Util.GetCurrentDateTime().Date.AddDays(-30), Util.GetCurrentDateTime().Date)
                .With(vm =>
                {
                    vm.RomaneioInicio = romaneioInicio;
                    vm.RomaneioFinal = romaneioFinal;
                    vm.CanDownloadNaoGeradosExportados = exportacaoAtualIds.Any();
                    vm.CanFinalizarNaoGeradosExportados = exportacaoAtualIds.Any() && CanFinalizarNaoGeradosExportados();
                    vm.TriggerDownloadNaoGerados = exportacaoAtualIds.Any() && (triggerDownloadFlag || triggerDownload);
                    vm.AnaliseItems = itens
                        .Where(i => exportacaoAtualIds.Contains(i.Id))
                        .OrderBy(i => ParseRomaneioNr(i.RomaneioNr))
                        .ToList();
                    vm.GridItems = itens
                        .Where(i => i.StatusId == StatusNaoGeradoAnalise)
                        .Where(i => IsWithinRomaneioInterval(i.RomaneioNr, romaneioInicio, romaneioFinal))
                        .OrderByDescending(i => i.ModificadoEm ?? DateTime.MinValue)
                        .ThenByDescending(i => ParseRomaneioNr(i.RomaneioNr))
                        .ToList();
                });
        }

        private RomaneioViewModel BuildConsultaViewModel(string romaneioNr)
        {
            var vm = new RomaneioViewModel
            {
                RomaneioNr = (romaneioNr ?? string.Empty).Trim(),
                CanViewFullMenu = CanViewFullMenu(),
                CanConferir = CanConferir(),
                CanFinalizarConferencia = CanFinalizarConferencia(),
                CanAdministrar = CanAdministrar(),
                ConsultaItens = new List<RomaneioItem>(),
                ConsultaItensDetalhe = new List<RomaneioConsultaItemViewModel>()
            };

            if (string.IsNullOrWhiteSpace(vm.RomaneioNr))
            {
                return vm;
            }

            vm.ConsultaRealizada = true;

            Romaneio romaneio = db.Romaneio
                .AsNoTracking()
                .Where(x => x.FilialId == filialId)
                .AsEnumerable()
                .FirstOrDefault(x => string.Equals((x.RomaneioNr ?? string.Empty).Trim(), vm.RomaneioNr, StringComparison.OrdinalIgnoreCase));

            if (romaneio == null)
            {
                vm.ConsultaMensagem = "Romaneio não localizado para o número informado.";
                return vm;
            }

            vm.ConsultaHeader = romaneio;
            var itens = db.RomaneioItem
                .AsNoTracking()
                .Where(x => x.FilialId == filialId && x.RomaneioId == romaneio.Id)
                .OrderBy(x => x.Id)
                .ToList();
            vm.ConsultaItens = itens;

            var statusMap = db.StatusRomaneio
                .AsNoTracking()
                .ToList()
                .GroupBy(x => x.Id)
                .ToDictionary(g => g.Key, g => g.First().Descricao);

            var ultimaConferenciaItem = itens
                .Where(x => x.DataConferente.HasValue)
                .OrderByDescending(x => x.DataConferente.Value)
                .ThenByDescending(x => x.Id)
                .FirstOrDefault();

            var usuariosIds = new[] { romaneio.SeparadorId, ultimaConferenciaItem != null ? ultimaConferenciaItem.ConferenteId : romaneio.ConferenteId }
                .Where(x => x.HasValue)
                .Select(x => x.Value)
                .Distinct()
                .ToList();

            var usuariosMap = usuariosIds.Any()
                ? db.Usuario
                    .AsNoTracking()
                    .Where(x => usuariosIds.Contains(x.Id))
                    .ToList()
                    .GroupBy(x => x.Id)
                    .ToDictionary(g => g.Key, g => g.First().Nome)
                : new Dictionary<int, string>();

            AreaPedido areaPedido = null;
            if (romaneio.VendedorId.HasValue)
            {
                areaPedido = LoadAreaPedidoById(romaneio.VendedorId.Value);
            }

            var codigosMaterial = itens
                .Where(x => !string.IsNullOrWhiteSpace(x.ItemNr))
                .Select(x => x.ItemNr.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var materiais = codigosMaterial.Any()
                ? db.Material
                    .AsNoTracking()
                    .Where(x => codigosMaterial.Contains(x.Codigo))
                    .ToList()
                : new List<Material>();

            var materialMap = materiais
                .GroupBy(x => x.Codigo ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => g
                        .OrderByDescending(x => x.FilialId == filialId)
                        .ThenByDescending(x => x.ModificadoEm ?? x.CriadoEm ?? DateTime.MinValue)
                        .Select(x => x.Descricao)
                        .FirstOrDefault(),
                    StringComparer.OrdinalIgnoreCase);

            vm.ConsultaDetalhe = new RomaneioConsultaHeaderViewModel
            {
                RomaneioNr = romaneio.RomaneioNr,
                ContatoNr = romaneio.Contato.HasValue ? romaneio.Contato.Value.ToString(CultureInfo.InvariantCulture) : string.Empty,
                Status = statusMap.ContainsKey(romaneio.StatusId ?? 0) ? statusMap[romaneio.StatusId ?? 0] : string.Empty,
                Vendedor = ResolveConsultaVendedor(areaPedido, romaneio.VendedorId),
                CriadoEm = romaneio.CriadoEm,
                CriadoPor = romaneio.CriadoPor,
                Separador = ResolveConsultaUsuarioNome(usuariosMap, romaneio.SeparadorId, "Separador"),
                DataSeparacao = romaneio.DataSeparador,
                Conferente = ResolveConsultaUsuarioNome(usuariosMap, ultimaConferenciaItem != null ? ultimaConferenciaItem.ConferenteId : romaneio.ConferenteId, "Conferente"),
                DataConferencia = ultimaConferenciaItem != null ? ultimaConferenciaItem.DataConferente : romaneio.DataConferente,
                ModificadoEm = romaneio.ModificadoEm,
                ModificadoPor = romaneio.ModificadoPor
            };

            vm.ConsultaItensDetalhe = itens
                .Select(x => new RomaneioConsultaItemViewModel
                {
                    ItemNr = x.ItemNr,
                    Descricao = ResolveConsultaMaterialDescricao(materialMap, x.ItemNr),
                    Qtde = x.Qtde
                })
                .ToList();

            return vm;
        }

        private static string ResolveConsultaUsuarioNome(IDictionary<int, string> usuariosMap, int? usuarioId, string prefixoFallback)
        {
            if (!usuarioId.HasValue)
            {
                return string.Empty;
            }

            string nome;
            if (usuariosMap.TryGetValue(usuarioId.Value, out nome))
            {
                return nome ?? string.Empty;
            }

            return prefixoFallback + " " + usuarioId.Value.ToString(CultureInfo.InvariantCulture);
        }

        private static string ResolveConsultaVendedor(AreaPedido areaPedido, int? vendedorId)
        {
            if (areaPedido != null)
            {
                if (!string.IsNullOrWhiteSpace(areaPedido.UsuarioApollo))
                {
                    return areaPedido.UsuarioApollo;
                }

                if (!string.IsNullOrWhiteSpace(areaPedido.Area))
                {
                    return areaPedido.Area;
                }
            }

            return vendedorId.HasValue ? vendedorId.Value.ToString(CultureInfo.InvariantCulture) : string.Empty;
        }

        private static string ResolveConsultaMaterialDescricao(IDictionary<string, string> materialMap, string itemNr)
        {
            if (string.IsNullOrWhiteSpace(itemNr))
            {
                return string.Empty;
            }

            string descricao;
            return materialMap.TryGetValue(itemNr.Trim(), out descricao) ? (descricao ?? string.Empty) : string.Empty;
        }

        private RomaneioViewModel BuildAtualizacaoViewModel(DateTime dataInicial, DateTime dataFinal, int romaneioInicio, int romaneioFinal)
        {
            var itens = LoadRomaneioGridItems();
            var importSummary = TempData["RomaneioImportSummary"] as RomaneioImportSummaryViewModel;
            var promptGerarMapaValue = TempData["PromptGerarMapa"];
            string produtividadeConfigMensagem;
            var produtividadeConfig = TryResolveProdutividadeConfig(out produtividadeConfigMensagem);
            var itensProdutividade = itens
                .Where(i => i.PickerId.HasValue && i.PickerId.Value > 0)
                .Where(i => IsWithinPeriodo(i.DataPicker ?? i.ModificadoEm ?? i.CriadoEm ?? i.DataRomaneio, dataInicial, dataFinal))
                .ToList();
            var itensProdutividadeConferencia = itens
                .Where(i => i.ConferenteId.HasValue && i.ConferenteId.Value > 0)
                .Where(i => IsWithinPeriodo(i.DataConferencia ?? i.ModificadoEm ?? i.CriadoEm ?? i.DataRomaneio, dataInicial, dataFinal))
                .ToList();
            var romaneioIdsProdutividade = itensProdutividade
                .Select(i => i.Id)
                .Concat(itensProdutividadeConferencia.Select(i => i.Id))
                .Distinct()
                .ToList();
            var quantidadeItensPorRomaneio = romaneioIdsProdutividade.Any()
                ? db.RomaneioItem
                    .Where(x => x.FilialId == filialId && romaneioIdsProdutividade.Contains(x.RomaneioId))
                    .GroupBy(x => x.RomaneioId)
                    .ToDictionary(g => g.Key, g => g.Count())
                : new Dictionary<int, int>();
            return CreateBaseViewModel(itens, dataInicial, dataFinal)
                .With(vm =>
                {
                    vm.RomaneioInicio = romaneioInicio;
                    vm.RomaneioFinal = romaneioFinal;
                    vm.GridItems = itens
                        .Where(i => (i.StatusId == StatusNaoGerado || (i.StatusId == StatusAguardandoSeparacao && !i.PossuiVendedor)) && IsWithinPeriodo(i.DataRomaneio, dataInicial, dataFinal))
                        .OrderBy(i => i.Prioridade ?? int.MaxValue)
                        .ThenBy(i => ParseRomaneioNr(i.RomaneioNr))
                        .ToList();

                    vm.AnaliseItems = itens
                        .Where(i => i.StatusId == StatusNaoGerado)
                        .Where(i => IsWithinRomaneioInterval(i.RomaneioNr, romaneioInicio, romaneioFinal))
                        .OrderBy(i => ParseRomaneioNr(i.RomaneioNr))
                        .ToList();

                    vm.Produtividade = itensProdutividade
                        .GroupBy(i => new { i.PickerNome, PickerId = i.PickerId ?? 0 })
                        .Select(g => new RomaneioDashboardItemViewModel
                        {
                            PickerId = g.Key.PickerId,
                            PickerNome = g.Key.PickerNome,
                            QuantidadeRomaneios = g.Count(),
                            // O schema atual não possui tabela de linhas do romaneio.
                            QuantidadeLinhas = g.Sum(x => quantidadeItensPorRomaneio.ContainsKey(x.Id) ? quantidadeItensPorRomaneio[x.Id] : 0),
                            QuantidadePecas = g.Sum(x => x.Pecas ?? 0)
                        })
                        .OrderByDescending(g => g.QuantidadeRomaneios)
                        .ThenBy(g => g.PickerNome)
                        .ToList();

                    vm.ProdutividadeConferencia = itensProdutividadeConferencia
                        .GroupBy(i => new { i.ConferenteNome, ConferenteId = i.ConferenteId ?? 0 })
                        .Select(g => new RomaneioDashboardItemViewModel
                        {
                            PickerId = g.Key.ConferenteId,
                            PickerNome = g.Key.ConferenteNome,
                            QuantidadeRomaneios = g.Count(),
                            QuantidadeLinhas = g.Sum(x => quantidadeItensPorRomaneio.ContainsKey(x.Id) ? quantidadeItensPorRomaneio[x.Id] : 0),
                            QuantidadePecas = g.Sum(x => x.Pecas ?? 0)
                        })
                        .OrderByDescending(g => g.QuantidadeRomaneios)
                        .ThenBy(g => g.PickerNome)
                        .ToList();

                    vm.ProdutividadeConfigValida = produtividadeConfig != null;
                    vm.ProdutividadeConfigMensagem = produtividadeConfigMensagem;
                    if (produtividadeConfig != null)
                    {
                        vm.Produtividade = BuildProdutividadeDashboard(
                            itensProdutividade,
                            quantidadeItensPorRomaneio,
                            produtividadeConfig,
                            x => x.PickerNome,
                            x => x.PickerId ?? 0);

                        vm.ProdutividadeConferencia = BuildProdutividadeDashboard(
                            itensProdutividadeConferencia,
                            quantidadeItensPorRomaneio,
                            produtividadeConfig,
                            x => x.ConferenteNome,
                            x => x.ConferenteId ?? 0);
                    }
                    else
                    {
                        vm.Produtividade = new List<RomaneioDashboardItemViewModel>();
                        vm.ProdutividadeConferencia = new List<RomaneioDashboardItemViewModel>();
                    }

                    vm.ImportSummary = importSummary;
                    vm.PromptGerarMapa = promptGerarMapaValue is bool && (bool)promptGerarMapaValue;
                });
        }

        private RomaneioViewModel BuildAlocacaoZonaViewModel(DateTime dataInicial, DateTime dataFinal)
        {
            var itens = LoadRomaneioGridItems();
            var resumo = TempData["AlocacaoZonaResumo"] as AlocacaoZonaResumoViewModel;

            return CreateBaseViewModel(itens, dataInicial, dataFinal)
                .With(vm =>
                {
                    vm.AlocacaoResumo = resumo;
                });
        }

        private RomaneioViewModel BuildTarefasViewModel(string tarefaNr, string romaneio, string contato, string os, string itemEstoque, string zona, DateTime? data)
        {
            var itens = LoadRomaneioGridItems();
            var filtro = new TarefaConsultaFiltroViewModel
            {
                TarefaNr = tarefaNr,
                RomaneioNr = romaneio,
                Contato = contato,
                OS = os,
                ItemNr = itemEstoque,
                Zona = zona,
                Data = data
            };

            return CreateBaseViewModel(itens, Util.GetCurrentDateTime().Date, Util.GetCurrentDateTime().Date)
                .With(vm =>
                {
                    vm.TarefaFiltro = filtro;
                    vm.TarefaItens = LoadTarefaConsultaItens(filtro);
                });
        }

        private RomaneioViewModel BuildPendenciasViewModel()
        {
            var itens = LoadRomaneioGridItems();

            return CreateBaseViewModel(itens, Util.GetCurrentDateTime().Date, Util.GetCurrentDateTime().Date)
                .With(vm =>
                {
                    vm.PendenciaItens = LoadPendenciaItens(StatusEmBusca, "Em Busca");
                });
        }

        private RomaneioViewModel BuildNaoEncontradosViewModel()
        {
            var itens = LoadRomaneioGridItems();

            return CreateBaseViewModel(itens, Util.GetCurrentDateTime().Date, Util.GetCurrentDateTime().Date)
                .With(vm =>
                {
                    vm.PendenciaItens = LoadPendenciaItens(StatusNaoEncontrado, "N\u00E3o Encontrado");
                });
        }

        private List<RomaneioDashboardItemViewModel> BuildProdutividadeDashboard(
            IEnumerable<RomaneioGridItemViewModel> itens,
            IDictionary<int, int> quantidadeItensPorRomaneio,
            ProdutividadeConfig config,
            Func<RomaneioGridItemViewModel, string> nomeSelector,
            Func<RomaneioGridItemViewModel, int> idSelector)
        {
            var ranking = itens
                .GroupBy(i => new
                {
                    Nome = nomeSelector(i),
                    Id = idSelector(i)
                })
                .Select(g =>
                {
                    int quantidadeRomaneios = g.Count();
                    int quantidadeLinhas = g.Sum(x => quantidadeItensPorRomaneio.ContainsKey(x.Id) ? quantidadeItensPorRomaneio[x.Id] : 0);
                    int quantidadePecas = g.Sum(x => x.Pecas ?? 0);

                    return new RomaneioDashboardItemViewModel
                    {
                        PickerId = g.Key.Id,
                        PickerNome = g.Key.Nome,
                        QuantidadeRomaneios = quantidadeRomaneios,
                        QuantidadeLinhas = quantidadeLinhas,
                        QuantidadePecas = quantidadePecas,
                        ProdutividadeCalculada =
                            (quantidadeRomaneios * config.PercentualRomaneios / 100m) +
                            (quantidadeLinhas * config.PercentualLinhas / 100m) +
                            (quantidadePecas * config.PercentualPecas / 100m)
                    };
                })
                .OrderByDescending(g => g.ProdutividadeCalculada)
                .ThenBy(g => g.PickerNome)
                .ToList();

            if (!ranking.Any())
            {
                return ranking;
            }

            decimal totalGeral = ranking.Sum(x => x.ProdutividadeCalculada);
            if (totalGeral <= 0m)
            {
                ranking.ForEach(x => x.ProdutividadeCalculada = 0m);
                return ranking;
            }

            ranking.ForEach(x =>
            {
                x.ProdutividadeCalculada = Math.Round((x.ProdutividadeCalculada / totalGeral) * 100m, 1, MidpointRounding.AwayFromZero);
            });

            decimal diferenca = 100m - ranking.Sum(x => x.ProdutividadeCalculada);
            if (diferenca != 0m)
            {
                ranking[0].ProdutividadeCalculada += diferenca;
            }

            return ranking;
        }

        private ProdutividadeConfig TryResolveProdutividadeConfig(out string mensagem)
        {
            mensagem = null;
            var configs = db.AppConfig
                .Where(x => x.FilialId == filialId &&
                    (x.Nome == "Romaneios" || x.Nome == "Linhas" || x.Nome == "Peças" || x.Nome == "Pecas"))
                .ToList();

            decimal percentualRomaneios;
            decimal percentualLinhas;
            decimal percentualPecas;

            if (!TryGetProdutividadePercentual(configs, "Romaneios", out percentualRomaneios))
            {
                mensagem = "Configuração inválida de produtividade: percentual de Romaneios não localizado.";
                return null;
            }

            if (!TryGetProdutividadePercentual(configs, "Linhas", out percentualLinhas))
            {
                mensagem = "Configuração inválida de produtividade: percentual de Linhas não localizado.";
                return null;
            }

            if (!TryGetProdutividadePercentual(configs, "Peças", out percentualPecas))
            {
                mensagem = "Configuração inválida de produtividade: percentual de Peças não localizado.";
                return null;
            }

            decimal total = percentualRomaneios + percentualLinhas + percentualPecas;
            if (Math.Abs(total - 100m) > 0.01m)
            {
                mensagem = "Configuração inválida de produtividade: Romaneios + Linhas + Peças deve totalizar 100%.";
                return null;
            }

            return new ProdutividadeConfig
            {
                PercentualRomaneios = percentualRomaneios,
                PercentualLinhas = percentualLinhas,
                PercentualPecas = percentualPecas
            };
        }

        private bool TryGetProdutividadePercentual(IEnumerable<AppConfig> configs, string indicador, out decimal percentual)
        {
            percentual = 0m;
            string indicadorNormalizado = NormalizeIndicadorNome(indicador);
            string valor = configs
                .Where(x => NormalizeIndicadorNome(x.Nome) == indicadorNormalizado)
                .Select(x => x.Valor)
                .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(valor))
            {
                return false;
            }

            string valorNormalizado = valor.Replace("%", string.Empty).Trim();
            return decimal.TryParse(valorNormalizado, NumberStyles.Number, new CultureInfo("pt-BR"), out percentual)
                || decimal.TryParse(valorNormalizado, NumberStyles.Number, CultureInfo.InvariantCulture, out percentual);
        }

        private static string NormalizeIndicadorNome(string nome)
        {
            return Util.RemoverAcentuacao(nome ?? string.Empty)
                .Trim()
                .ToUpperInvariant();
        }

        private RomaneioViewModel CreateBaseViewModel(List<RomaneioGridItemViewModel> itens, DateTime dataInicial, DateTime dataFinal)
        {
            var itensPeriodo = itens
                .Where(i => IsWithinPeriodo(i.DataRomaneio, dataInicial, dataFinal))
                .ToList();
            var hoje = Util.GetCurrentDateTime().Date;

            return new RomaneioViewModel
            {
                DataInicial = dataInicial,
                DataFinal = dataFinal,
                CanViewFullMenu = CanViewFullMenu(),
                CanConferir = CanConferir(),
                CanFinalizarConferencia = CanFinalizarConferencia(),
                CanAdministrar = CanAdministrar(),
                TotalNaoGerado = itensPeriodo.Count(i => i.StatusId == StatusNaoGerado || (i.StatusId == StatusAguardandoSeparacao && !i.PossuiVendedor)),
                TotalAguardandoSeparacao = itensPeriodo.Count(i => i.StatusId == StatusAguardandoSeparacao && i.PossuiVendedor),
                TotalEmSeparacao = itensPeriodo.Count(i => i.StatusId == StatusEmSeparacao),
                TotalFinalizado = itens.Count(i => i.StatusId == StatusFinalizado && i.DataConferencia.HasValue && i.DataConferencia.Value.Date == hoje),
                TotalNaoSeparar = itensPeriodo.Count(i => i.StatusId == StatusNaoSeparar)
            };
        }

        private List<SelectListItem> BuildStatusDDL()
        {
            return db.StatusRomaneio
                .ToList()
                .GroupBy(s => s.Id)
                .Select(g => g.First())
                .OrderBy(s => s.Id)
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(CultureInfo.InvariantCulture),
                    Text = s.Descricao
                })
                .ToList();
        }

        private List<RomaneioGridItemViewModel> LoadRomaneioGridItems(int? statusId = null, DateTime? modificadoApos = null)
        {
            IQueryable<Romaneio> query = db.Romaneio
                .Where(r => r.FilialId == filialId);

            if (statusId.HasValue)
            {
                int filtroStatus = statusId.Value;
                query = query.Where(r => r.StatusId == filtroStatus);
            }

            if (modificadoApos.HasValue)
            {
                DateTime filtroData = modificadoApos.Value;
                query = query.Where(r => r.ModificadoEm.HasValue && r.ModificadoEm.Value >= filtroData);
            }

            var romaneios = query.ToList();

            var romaneioIds = romaneios
                .Select(r => r.Id)
                .Distinct()
                .ToList();

            var conferenciaMap = romaneioIds.Any()
                ? db.RomaneioItem
                    .Where(ri => ri.FilialId == filialId && romaneioIds.Contains(ri.RomaneioId) && ri.DataConferente.HasValue)
                    .ToList()
                    .GroupBy(ri => ri.RomaneioId)
                    .ToDictionary(
                        g => g.Key,
                        g => g
                            .OrderByDescending(ri => ri.DataConferente.Value)
                            .ThenByDescending(ri => ri.Id)
                            .Select(ri => new ConferenciaResumo
                            {
                                ConferenteId = ri.ConferenteId,
                                DataConferencia = ri.DataConferente
                            })
                            .FirstOrDefault())
                : new Dictionary<int, ConferenciaResumo>();

            var userIds = romaneios
                .SelectMany(r =>
                {
                    ConferenciaResumo conferencia;
                    conferenciaMap.TryGetValue(r.Id, out conferencia);
                    return new[] { r.SeparadorId, conferencia != null ? conferencia.ConferenteId : r.ConferenteId };
                })
                .Where(id => id.HasValue)
                .Select(id => id.Value)
                .Distinct()
                .ToList();

            var usuarios = db.Usuario
                .Where(u => userIds.Contains(u.Id))
                .ToDictionary(u => u.Id, u => u.Nome);

            var statusMap = db.StatusRomaneio
                .ToList()
                .GroupBy(s => s.Id)
                .Select(g => g.First())
                .ToDictionary(s => s.Id, s => s.Descricao);

            var areaRomaneios = LoadAreaRomaneiosConfiguracao()
                .GroupBy(a => a.Id)
                .ToDictionary(g => g.Key, g => g.First());

            var areaRomaneiosByName = areaRomaneios.Values
                .Where(a => !string.IsNullOrWhiteSpace(a.Area))
                .GroupBy(a => a.Area.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var areaPedidoIds = romaneios
                .Where(r => r.VendedorId.HasValue)
                .Select(r => r.VendedorId.Value)
                .Distinct()
                .ToList();

            var areaPedidos = LoadAreaPedidosConfiguracao()
                .Where(a => areaPedidoIds.Contains(a.Id))
                .ToList()
                .GroupBy(a => a.Id)
                .ToDictionary(g => g.Key, g => g.First());

            return romaneios.Select(r =>
            {
                AreaPedido areaPedido;
                areaPedidos.TryGetValue(r.VendedorId ?? 0, out areaPedido);

                var areaInfo = ResolveAreaRomaneio(areaPedido, areaRomaneios, areaRomaneiosByName);
                ConferenciaResumo conferencia;
                conferenciaMap.TryGetValue(r.Id, out conferencia);
                int? conferenteId = conferencia != null ? conferencia.ConferenteId : r.ConferenteId;
                DateTime? dataConferencia = conferencia != null ? conferencia.DataConferencia : r.DataConferente;

                return new RomaneioGridItemViewModel
                {
                    Id = r.Id,
                    RomaneioNr = r.RomaneioNr,
                    PossuiVendedor = areaInfo != null || r.VendedorId == UnknownAreaPedidoId,
                    Area = areaInfo != null ? areaInfo.Area : (areaPedido != null ? areaPedido.Area : r.Localizacao),
                    Prioridade = areaInfo != null ? areaInfo.Prioridade : null,
                    UsuarioApollo = areaPedido != null ? areaPedido.UsuarioApollo : string.Empty,
                    ContatoNr = r.Contato,
                    Itens = r.Itens,
                    Pecas = r.Pecas,
                    PickerId = r.SeparadorId,
                    PickerNome = r.SeparadorId.HasValue
                        ? (usuarios.ContainsKey(r.SeparadorId.Value)
                            ? usuarios[r.SeparadorId.Value]
                            : "Picker " + r.SeparadorId.Value.ToString(CultureInfo.InvariantCulture))
                        : string.Empty,
                    DataPicker = r.DataSeparador,
                    ConferenteId = conferenteId,
                    ConferenteNome = conferenteId.HasValue
                        ? (usuarios.ContainsKey(conferenteId.Value)
                            ? usuarios[conferenteId.Value]
                            : "Conferente " + conferenteId.Value.ToString(CultureInfo.InvariantCulture))
                        : string.Empty,
                    DataConferencia = dataConferencia,
                    StatusId = r.StatusId,
                    Status = statusMap.ContainsKey(r.StatusId ?? 0) ? statusMap[r.StatusId ?? 0] : "Sem status",
                    DataRomaneio = r.DataEmissao ?? r.CriadoEm,
                    CriadoPor = r.CriadoPor,
                    CriadoEm = r.CriadoEm,
                    ModificadoPor = r.ModificadoPor,
                    ModificadoEm = r.ModificadoEm
                };
            }).ToList();
        }

        private List<RomaneioExportacaoQueryItem> LoadRomaneiosNaoGeradosParaExportacao(int filialAtualId, int inicio, int fim)
        {
            long inicioFiltro = inicio > 0 ? inicio : long.MinValue;
            long fimFiltro = fim > 0 ? fim : long.MaxValue;

            return db.Database.SqlQuery<RomaneioExportacaoQueryItem>(
                @"SELECT
                      Id,
                      LTRIM(RTRIM(RomaneioNr)) AS RomaneioNr
                  FROM Romaneio
                  WHERE FilialId = @p0
                    AND StatusId = @p1
                    AND TRY_CAST(LTRIM(RTRIM(RomaneioNr)) AS BIGINT) IS NOT NULL
                    AND TRY_CAST(LTRIM(RTRIM(RomaneioNr)) AS BIGINT) BETWEEN @p2 AND @p3
                  ORDER BY TRY_CAST(LTRIM(RTRIM(RomaneioNr)) AS BIGINT), LTRIM(RTRIM(RomaneioNr))",
                filialAtualId,
                StatusNaoGerado,
                inicioFiltro,
                fimFiltro)
                .ToList();
        }

        private Tuple<int, int> ResolveAnaliseNaoGeradosInterval(int? romaneioInicio, int? romaneioFinal)
        {
            if (!romaneioInicio.HasValue && !romaneioFinal.HasValue)
            {
                var ultimoIntervalo = GetUltimoIntervaloNaoGerados();
                if (ultimoIntervalo != null)
                {
                    return ultimoIntervalo;
                }

                return Tuple.Create(0, 0);
            }

            var intervalo = ResolveRomaneioInterval(romaneioInicio, romaneioFinal);
            SetUltimoIntervaloNaoGerados(intervalo.Item1, intervalo.Item2);
            return intervalo;
        }

        private Tuple<DateTime, DateTime> ResolvePeriodo(DateTime? dataInicial, DateTime? dataFinal)
        {
            DateTime inicio = dataInicial.HasValue ? dataInicial.Value.Date : Util.GetCurrentDateTime().Date.AddDays(-30);
            DateTime fim = dataFinal.HasValue ? dataFinal.Value.Date : Util.GetCurrentDateTime().Date;

            if (fim < inicio)
            {
                var temp = inicio;
                inicio = fim;
                fim = temp;
            }

            return Tuple.Create(inicio, fim);
        }

        private static Tuple<int, int> ResolveRomaneioInterval(int? romaneioInicio, int? romaneioFinal)
        {
            int inicio = romaneioInicio.GetValueOrDefault();
            int fim = romaneioFinal.GetValueOrDefault();

            if (inicio < 0)
            {
                inicio = 0;
            }

            if (fim < 0)
            {
                fim = 0;
            }

            if (inicio > 0 && fim > 0 && fim < inicio)
            {
                var temp = inicio;
                inicio = fim;
                fim = temp;
            }

            return Tuple.Create(inicio, fim);
        }

        private List<RomaneioPendenciaItemViewModel> LoadPendenciaItens(int statusId, string statusDescricao)
        {
            const string sql = @"
SELECT
    ri.Id,
    r.RomaneioNr,
    ri.ItemNr,
    COALESCE(NULLIF(ri.Descricao, ''), m.Descricao, '') AS Descricao,
    ISNULL(z.Nome, '') AS Zona,
    ISNULL(COALESCE(lri.Codigo, le.Codigo, estoque.Locacao), '') AS Locacao,
    ri.Qtde AS Quantidade,
    ri.StatusId,
    @p2 AS Status
FROM dbo.RomaneioItem ri
INNER JOIN dbo.Romaneio r
        ON r.Id = ri.RomaneioId
LEFT JOIN dbo.Material m
       ON m.Codigo = ri.ItemNr
OUTER APPLY (
    SELECT TOP 1 e.Locacao
      FROM dbo.Estoque e
     WHERE e.ItemNr = ri.ItemNr
       AND (e.FilialId = ri.FilialId OR ri.FilialId IS NULL)
       AND ISNULL(LTRIM(RTRIM(e.Locacao)), '') <> ''
     ORDER BY CASE WHEN ISNULL(e.Saldo, 0) > 0 THEN 0 ELSE 1 END,
              ISNULL(e.Saldo, 0) DESC,
              e.Locacao
) estoque
LEFT JOIN dbo.Locacao lri
       ON lri.Id = ri.LocacaoId
LEFT JOIN dbo.Locacao le
       ON le.Codigo = estoque.Locacao
      AND (le.FilialId = ri.FilialId OR le.FilialId IS NULL)
LEFT JOIN dbo.Zona z
       ON z.Id = COALESCE(ri.ZonaId, lri.ZonaId, le.ZonaId)
WHERE ri.FilialId = @p0
  AND ri.StatusId = @p1
ORDER BY r.RomaneioNr, z.Nome, ri.ItemNr";

            return db.Database
                .SqlQuery<RomaneioPendenciaItemViewModel>(sql, filialId, statusId, statusDescricao)
                .ToList();
        }

        private static bool IsWithinPeriodo(DateTime? valor, DateTime inicio, DateTime fim)
        {
            if (!valor.HasValue)
            {
                return false;
            }

            return valor.Value.Date >= inicio.Date && valor.Value.Date <= fim.Date;
        }

        private static bool IsWithinRomaneioInterval(string romaneioNr, int inicio, int fim)
        {
            if (inicio <= 0 && fim <= 0)
            {
                return true;
            }

            int numero = ParseRomaneioNr(romaneioNr);
            if (numero == int.MaxValue)
            {
                return false;
            }

            if (inicio > 0 && numero < inicio)
            {
                return false;
            }

            if (fim > 0 && numero > fim)
            {
                return false;
            }

            return true;
        }

        private int GetEffectiveFilialId()
        {
            int filialAtual = Util.GetCurrentFilial();
            if (filialAtual > 0)
            {
                return filialAtual;
            }

            string usuarioAtual = Util.GetCurrentUser();
            if (string.IsNullOrWhiteSpace(usuarioAtual))
            {
                return 0;
            }

            return db.Usuario
                .Where(u => u.Login == usuarioAtual && u.FilialId.HasValue)
                .Select(u => u.FilialId.Value)
                .FirstOrDefault();
        }

        private Tuple<int, int> GetUltimoIntervaloNaoGerados()
        {
            var intervalo = Session[UltimoIntervaloNaoGeradosSessionKey] as int[];
            if (intervalo == null || intervalo.Length < 2)
            {
                return null;
            }

            return Tuple.Create(intervalo[0], intervalo[1]);
        }

        private void SetUltimoIntervaloNaoGerados(int inicio, int fim)
        {
            Session[UltimoIntervaloNaoGeradosSessionKey] = new[] { inicio, fim };
        }

        private Usuario GetUsuarioAtual()
        {
            string login = Util.GetCurrentUser();
            return db.Usuario.FirstOrDefault(u => u.Login == login && u.FilialId == filialId);
        }

        private bool CanConferir()
        {
            int perfilId = Util.GetPerfilId();
            return perfilId == ConferProfileId || perfilId == AdminProfileId;
        }

        private bool HasOperationalArea(int? vendedorId)
        {
            if (!vendedorId.HasValue)
            {
                return false;
            }

            if (vendedorId.Value == UnknownAreaPedidoId)
            {
                return true;
            }

            var areaPedido = LoadAreaPedidoById(vendedorId.Value);
            if (areaPedido == null)
            {
                return false;
            }

            var areaRomaneios = LoadAreaRomaneiosConfiguracao();
            var areaRomaneiosById = areaRomaneios
                .GroupBy(a => a.Id)
                .ToDictionary(g => g.Key, g => g.First());
            var areaRomaneiosByName = areaRomaneios
                .Where(a => !string.IsNullOrWhiteSpace(a.Area))
                .GroupBy(a => a.Area.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            return ResolveAreaRomaneio(areaPedido, areaRomaneiosById, areaRomaneiosByName) != null;
        }

        private bool RequiresConferencia(int? vendedorId)
        {
            if (!vendedorId.HasValue)
            {
                return true;
            }

            var areaPedido = LoadAreaPedidoById(vendedorId.Value);
            if (areaPedido == null)
            {
                return true;
            }

            var areaRomaneios = LoadAreaRomaneiosConfiguracao();
            var areaRomaneiosById = areaRomaneios
                .GroupBy(a => a.Id)
                .ToDictionary(g => g.Key, g => g.First());
            var areaRomaneiosByName = areaRomaneios
                .Where(a => !string.IsNullOrWhiteSpace(a.Area))
                .GroupBy(a => a.Area.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var areaInfo = ResolveAreaRomaneio(areaPedido, areaRomaneiosById, areaRomaneiosByName);
            return areaInfo == null || (areaInfo.Conferir ?? true);
        }

        private static AreaRomaneio ResolveAreaRomaneio(
            AreaPedido areaPedido,
            IDictionary<int, AreaRomaneio> areaRomaneiosById,
            IDictionary<string, AreaRomaneio> areaRomaneiosByName)
        {
            if (areaPedido == null)
            {
                return null;
            }

            AreaRomaneio areaInfo = null;
            if (areaPedido.AreaId.HasValue)
            {
                areaRomaneiosById.TryGetValue(areaPedido.AreaId.Value, out areaInfo);
            }

            if (areaInfo == null && !string.IsNullOrWhiteSpace(areaPedido.Area))
            {
                areaRomaneiosByName.TryGetValue(areaPedido.Area.Trim(), out areaInfo);
            }

            return areaInfo;
        }

        private static bool IsAreaHabilitadaParaAlocacao(AreaRomaneio areaInfo, bool possuiAreaComAlocar)
        {
            if (areaInfo == null)
            {
                return false;
            }

            if (possuiAreaComAlocar)
            {
                return areaInfo.Alocar ?? false;
            }

            return areaInfo.Separar ?? false;
        }

        private List<AreaRomaneio> LoadAreaRomaneiosConfiguracao()
        {
            EnsureAreaRomaneioSchema();

            return db.Database.SqlQuery<AreaRomaneio>(
                @"SELECT Id, Area, Prioridade, Separar, Conferir, Alocar, Mapa
                    FROM AreaRomaneio")
                .ToList();
        }

        private List<AreaPedido> LoadAreaPedidosConfiguracao()
        {
            return db.Database.SqlQuery<AreaPedido>(
                @"SELECT Id, UsuarioApollo, AreaId, Area
                    FROM AreaPedido")
                .ToList();
        }

        private AreaPedido LoadAreaPedidoById(int id)
        {
            return db.Database.SqlQuery<AreaPedido>(
                @"SELECT Id, UsuarioApollo, AreaId, Area
                    FROM AreaPedido
                   WHERE Id = @p0", id)
                .FirstOrDefault();
        }

        private void EnsureAreaRomaneioSchema()
        {
            bool alocarExiste = db.Database.SqlQuery<int>(
                @"SELECT COUNT(1)
                    FROM INFORMATION_SCHEMA.COLUMNS
                   WHERE TABLE_NAME = 'AreaRomaneio'
                     AND COLUMN_NAME = 'Alocar'")
                .FirstOrDefault() > 0;

            if (!alocarExiste)
            {
                db.Database.ExecuteSqlCommand(
                    @"ALTER TABLE AreaRomaneio
                        ADD Alocar BIT NOT NULL
                            CONSTRAINT DF_AreaRomaneio_Alocar_Runtime DEFAULT (0)");
            }

            bool mapaExiste = db.Database.SqlQuery<int>(
                @"SELECT COUNT(1)
                    FROM INFORMATION_SCHEMA.COLUMNS
                   WHERE TABLE_NAME = 'AreaRomaneio'
                     AND COLUMN_NAME = 'Mapa'")
                .FirstOrDefault() > 0;

            if (!mapaExiste)
            {
                db.Database.ExecuteSqlCommand(
                    @"ALTER TABLE AreaRomaneio
                        ADD Mapa BIT NOT NULL
                            CONSTRAINT DF_AreaRomaneio_Mapa_Runtime DEFAULT (0)");

                bool areaPedidoMapaExiste = db.Database.SqlQuery<int>(
                    @"SELECT COUNT(1)
                        FROM INFORMATION_SCHEMA.COLUMNS
                       WHERE TABLE_NAME = 'AreaPedido'
                         AND COLUMN_NAME = 'Mapa'")
                    .FirstOrDefault() > 0;

                if (areaPedidoMapaExiste)
                {
                    db.Database.ExecuteSqlCommand(
                        @"UPDATE ar
                             SET Mapa = 1
                            FROM AreaRomaneio ar
                           WHERE EXISTS
                           (
                               SELECT 1
                                 FROM AreaPedido ap
                                WHERE ap.AreaId = ar.Id
                                  AND ISNULL(ap.Mapa, 0) = 1
                           )");
                }
            }

            db.Database.ExecuteSqlCommand(
                @"UPDATE AreaRomaneio
                     SET Alocar = ISNULL(Alocar, ISNULL(Separar, 0))
                   WHERE Alocar IS NULL");

            db.Database.ExecuteSqlCommand(
                @"UPDATE AreaRomaneio
                     SET Mapa = ISNULL(Mapa, 0)
                   WHERE Mapa IS NULL");
        }

        private bool CanViewFullMenu()
        {
            int perfilId = Util.GetPerfilId();
            return perfilId == AdminProfileId
                || perfilId == PickerProfileId
                || perfilId == ConferProfileId
                || perfilId == SupervisorProfileId;
        }

        private bool CanFinalizarConferencia()
        {
            int perfilId = Util.GetPerfilId();
            return perfilId == AdminProfileId || perfilId == ConferProfileId;
        }

        private bool CanAdministrar()
        {
            return Util.GetPerfilId() == AdminProfileId;
        }

        private bool CanExportarNaoGerados()
        {
            return CanViewFullMenu();
        }

        private bool CanFinalizarNaoGeradosExportados()
        {
            int perfilId = Util.GetPerfilId();
            return perfilId == AdminProfileId || perfilId == SupervisorProfileId;
        }

        private void ApplyStatusRegression(Romaneio romaneio, int novoStatus)
        {
            if (novoStatus <= StatusAguardandoSeparacao || novoStatus == StatusNaoSeparar)
            {
                romaneio.SeparadorId = null;
                romaneio.DataSeparador = null;
                romaneio.ConferenteId = null;
                romaneio.DataConferente = null;
                return;
            }

            if (novoStatus == StatusEmSeparacao)
            {
                romaneio.ConferenteId = null;
                romaneio.DataConferente = null;
            }
        }

        private void SetFlash(string type, string message)
        {
            TempData["Flash.Type"] = type;
            TempData["Flash.Message"] = message;
        }

        private List<int> GetExportacaoNaoGeradosIds()
        {
            return Session[ExportacaoNaoGeradosSessionKey] as List<int> ?? new List<int>();
        }

        private void SetExportacaoNaoGeradosIds(IEnumerable<int> ids)
        {
            Session[ExportacaoNaoGeradosSessionKey] = ids == null
                ? new List<int>()
                : ids.Distinct().ToList();
        }

        private void ClearExportacaoNaoGeradosIds()
        {
            Session.Remove(ExportacaoNaoGeradosSessionKey);
        }

        private byte[] GetExportacaoNaoGeradosArquivo()
        {
            return Session[ExportacaoNaoGeradosArquivoSessionKey] as byte[];
        }

        private string GetExportacaoNaoGeradosArquivoNome()
        {
            return Session[ExportacaoNaoGeradosArquivoNomeSessionKey] as string;
        }

        private void SetExportacaoNaoGeradosArquivo(byte[] arquivo, string nomeArquivo)
        {
            Session[ExportacaoNaoGeradosArquivoSessionKey] = arquivo;
            Session[ExportacaoNaoGeradosArquivoNomeSessionKey] = nomeArquivo;
        }

        private void ClearExportacaoNaoGeradosArquivo()
        {
            Session.Remove(ExportacaoNaoGeradosArquivoSessionKey);
            Session.Remove(ExportacaoNaoGeradosArquivoNomeSessionKey);
        }

        private void SetExportacaoNaoGeradosFlowFlags(bool triggerDownload)
        {
            Session[TriggerDownloadNaoGeradosSessionKey] = triggerDownload;
        }

        private void ClearExportacaoNaoGeradosFlowFlags()
        {
            Session.Remove(TriggerDownloadNaoGeradosSessionKey);
        }

        private bool ConsumeTriggerDownloadNaoGerados()
        {
            bool value = Session[TriggerDownloadNaoGeradosSessionKey] is bool
                && (bool)Session[TriggerDownloadNaoGeradosSessionKey];
            Session.Remove(TriggerDownloadNaoGeradosSessionKey);
            return value;
        }

        private static byte[] BuildNaoGeradosExportBytes(IEnumerable<string> romaneios)
        {
            var html = new StringBuilder();
            html.AppendLine("<table border='1'>");
            html.AppendLine("<thead><tr><th>Romaneio Nr</th></tr></thead>");
            html.AppendLine("<tbody>");

            foreach (var romaneioNr in romaneios ?? Enumerable.Empty<string>())
            {
                html.AppendLine("<tr>");
                html.AppendLine("<td>" + HttpUtility.HtmlEncode(romaneioNr ?? string.Empty) + "</td>");
                html.AppendLine("</tr>");
            }

            html.AppendLine("</tbody></table>");
            return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(html.ToString())).ToArray();
        }

        private static string CsvValue(string value)
        {
            value = value ?? string.Empty;
            if (value.Contains(";") || value.Contains("\"") || value.Contains("\n"))
            {
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            }

            return value;
        }

        private static int ParseRomaneioNr(string romaneioNr)
        {
            int numero;
            string valor = (romaneioNr ?? string.Empty).Trim();
            if (int.TryParse(valor, NumberStyles.Integer, CultureInfo.InvariantCulture, out numero))
            {
                return numero;
            }

            string somenteDigitos = new string(valor.Where(char.IsDigit).ToArray());
            return int.TryParse(somenteDigitos, NumberStyles.Integer, CultureInfo.InvariantCulture, out numero)
                ? numero
                : int.MaxValue;
        }

        private List<RomaneioImportRow> ReadImportRows(HttpPostedFileBase arquivo, string worksheetName = null)
        {
            string extensao = Path.GetExtension(arquivo.FileName ?? string.Empty);
            if (string.IsNullOrWhiteSpace(extensao))
            {
                throw new InvalidOperationException("Arquivo sem extensão.");
            }

            extensao = extensao.ToLowerInvariant();
            if (extensao == ".csv" || extensao == ".txt")
            {
                return ReadDelimitedRows(arquivo.InputStream);
            }

            if (extensao == ".xls" || extensao == ".xlsx")
            {
                return ReadExcelRows(arquivo.InputStream, worksheetName);
            }

            throw new InvalidOperationException("Formato não suportado. Utilize .xls, .xlsx, .csv ou .txt.");
        }

        private List<RomaneioImportRow> ReadDelimitedRows(Stream stream)
        {
            var rows = new List<RomaneioImportRow>();
            using (var reader = new StreamReader(stream, Encoding.UTF8, true, 1024, true))
            {
                string headerLine = reader.ReadLine();
                if (string.IsNullOrWhiteSpace(headerLine))
                {
                    return rows;
                }

                char separator = DetectSeparator(headerLine);
                var headers = ParseDelimitedLine(headerLine, separator).Select(NormalizeColumnName).ToList();

                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    var values = ParseDelimitedLine(line, separator);
                    rows.Add(MapImportRow(headers, values));
                }
            }

            return rows;
        }

        private List<RomaneioImportRow> ReadExcelRows(Stream stream, string worksheetName = null)
        {
            var rows = new List<RomaneioImportRow>();
            string normalizedWorksheetName = NormalizeColumnName(worksheetName);

            using (var reader = ExcelReaderFactory.CreateReader(stream))
            {
                do
                {
                    bool processCurrentSheet = string.IsNullOrWhiteSpace(normalizedWorksheetName)
                        || string.Equals(NormalizeColumnName(reader.Name), normalizedWorksheetName, StringComparison.OrdinalIgnoreCase);
                    List<string> headers = null;

                    while (reader.Read())
                    {
                        if (!processCurrentSheet)
                        {
                            continue;
                        }

                        var values = new List<string>();
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            values.Add(ConvertCellValue(reader.GetValue(i)));
                        }

                        if (headers == null)
                        {
                            headers = values.Select(NormalizeColumnName).ToList();
                            if (headers.All(string.IsNullOrWhiteSpace))
                            {
                                headers = null;
                            }

                            continue;
                        }

                        if (values.All(string.IsNullOrWhiteSpace))
                        {
                            continue;
                        }

                        rows.Add(MapImportRow(headers, values));
                    }

                    if (processCurrentSheet && rows.Any())
                    {
                        break;
                    }
                } while (reader.NextResult());
            }

            return rows;
        }

        private static string ConvertCellValue(object value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            if (value is DateTime)
            {
                return ((DateTime)value).ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
            }

            return Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
        }

        private static Dictionary<int, string> ReadXlsxHeader(XElement row, XNamespace ns, IList<string> sharedStrings)
        {
            var headerMap = new Dictionary<int, string>();
            foreach (var cell in row.Elements(ns + "c"))
            {
                int index = GetColumnIndex((string)cell.Attribute("r"));
                string value = NormalizeColumnName(GetCellValue(cell, ns, sharedStrings));
                headerMap[index] = value;
            }

            return headerMap;
        }

        private static string GetCellValue(XElement cell, XNamespace ns, IList<string> sharedStrings)
        {
            string type = (string)cell.Attribute("t");
            if (type == "inlineStr")
            {
                return string.Concat(cell.Descendants(ns + "t").Select(t => t.Value));
            }

            string value = cell.Element(ns + "v") != null ? cell.Element(ns + "v").Value : string.Empty;
            if (type == "s")
            {
                int index;
                if (int.TryParse(value, out index) && index >= 0 && index < sharedStrings.Count)
                {
                    return sharedStrings[index];
                }
            }

            return value;
        }

        private static int GetColumnIndex(string cellReference)
        {
            if (string.IsNullOrWhiteSpace(cellReference))
            {
                return 0;
            }

            int index = 0;
            foreach (char ch in cellReference)
            {
                if (!char.IsLetter(ch))
                {
                    break;
                }

                index = (index * 26) + (char.ToUpperInvariant(ch) - 'A' + 1);
            }

            return Math.Max(index - 1, 0);
        }

        private static char DetectSeparator(string headerLine)
        {
            if (headerLine.Contains(";"))
            {
                return ';';
            }

            if (headerLine.Contains("\t"))
            {
                return '\t';
            }

            return ',';
        }

        private static List<string> ParseDelimitedLine(string line, char separator)
        {
            var result = new List<string>();
            var current = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char ch = line[i];
                if (ch == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (ch == separator && !inQuotes)
                {
                    result.Add(current.ToString().Trim());
                    current.Clear();
                }
                else
                {
                    current.Append(ch);
                }
            }

            result.Add(current.ToString().Trim());
            return result;
        }

        private RomaneioImportRow MapImportRow(IList<string> headers, IList<string> values)
        {
            var data = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < headers.Count; i++)
            {
                if (!data.ContainsKey(headers[i]))
                {
                    data[headers[i]] = new List<string>();
                }

                data[headers[i]].Add(i < values.Count ? values[i] : string.Empty);
            }

            return new RomaneioImportRow
            {
                RomaneioNr = GetMappedValue(data, "romaneionr", "romaneio", "romaneio_nr"),
                ItemNr = GetMappedValue(data, "itemestoque", "item_estoque", "itemestoquenr", "itemestoque_nr", "item", "itemnr", "item_nr"),
                Vendedor = GetMappedValue(data, "usuariosolicitante", "usuario_solicitante", "vendedor", "usuarioapollo", "usuario_apollo", "usuario"),
                ContatoNr = ParseNullableInt(GetMappedValue(data, "contatonr", "contato", "contato_nr")),
                OS = GetMappedValue(data, "os"),
                DataFaturamento = ParseNullableDate(GetMappedValue(data, "datafaturamento", "data_faturamento", "data")),
                Descricao = GetMappedValue(data, "descricao"),
                ValorUnitario = ParseNullableDecimal(GetMappedValue(data, 1, "valunitario", "valorunitario")),
                ValorTotal = ParseNullableDecimal(GetMappedValue(data, 1, "valtotal", "valortotal")),
                Itens = null,
                Pecas = ParseNullableInt(GetMappedValue(data, "qtde", "pecas", "quantidade"))
            };
        }

        private static List<RomaneioImportRow> AggregateImportRows(IEnumerable<RomaneioImportRow> rows)
        {
            return rows
                .Where(r => r != null && !string.IsNullOrWhiteSpace(r.RomaneioNr))
                .GroupBy(r => r.RomaneioNr.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(g => new RomaneioImportRow
                {
                    RomaneioNr = g.First().RomaneioNr.Trim(),
                    Vendedor = g.Select(r => r.Vendedor).FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)),
                    ContatoNr = g.Select(r => r.ContatoNr).FirstOrDefault(v => v.HasValue),
                    OS = g.Select(r => r.OS).FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)),
                    DataFaturamento = g.Select(r => r.DataFaturamento).FirstOrDefault(v => v.HasValue),
                    Itens = g.Count(),
                    Pecas = g.Sum(r => r.Pecas ?? 0)
                })
                .ToList();
        }

        private static List<RomaneioImportRow> AggregateImportItemRows(IEnumerable<RomaneioImportRow> rows)
        {
            return rows
                .Where(r => r != null && !string.IsNullOrWhiteSpace(r.RomaneioNr) && !string.IsNullOrWhiteSpace(r.ItemNr))
                .GroupBy(r => (r.RomaneioNr.Trim() + "|" + r.ItemNr.Trim()), StringComparer.OrdinalIgnoreCase)
                .Select(g => new RomaneioImportRow
                {
                    RomaneioNr = g.First().RomaneioNr.Trim(),
                    ItemNr = g.First().ItemNr.Trim(),
                    Pecas = g.Sum(r => r.Pecas ?? 0)
                })
                .ToList();
        }

        private static List<RomaneioImportRow> AggregateAllocationItemRows(IEnumerable<RomaneioImportRow> rows)
        {
            return rows
                .Where(r => r != null && !string.IsNullOrWhiteSpace(r.RomaneioNr) && !string.IsNullOrWhiteSpace(r.ItemNr))
                .GroupBy(r => (r.RomaneioNr.Trim() + "|" + r.ItemNr.Trim()), StringComparer.OrdinalIgnoreCase)
                .Select(g => new RomaneioImportRow
                {
                    RomaneioNr = g.First().RomaneioNr.Trim(),
                    ItemNr = g.First().ItemNr.Trim(),
                    Descricao = g.Select(r => r.Descricao).FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)),
                    Pecas = g.Sum(r => r.Pecas ?? 0),
                    ValorUnitario = g.Select(r => r.ValorUnitario).FirstOrDefault(v => v.HasValue),
                    ValorTotal = g.Sum(r => r.ValorTotal ?? 0m)
                })
                .ToList();
        }

        private static string GetMappedValue(IDictionary<string, List<string>> data, params string[] keys)
        {
            return GetMappedValue(data, 0, keys);
        }

        private static string GetMappedValue(IDictionary<string, List<string>> data, int occurrence, params string[] keys)
        {
            foreach (string key in keys)
            {
                List<string> values;
                if (data.TryGetValue(key, out values) && values != null && values.Count > occurrence)
                {
                    return values[occurrence];
                }
            }

            return string.Empty;
        }

        private static int? ParseNullableInt(string value)
        {
            int number;
            return int.TryParse((value ?? string.Empty).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out number)
                ? number
                : (int?)null;
        }

        private static decimal? ParseNullableDecimal(string value)
        {
            value = (value ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            decimal number;
            if (decimal.TryParse(value, NumberStyles.Any, new CultureInfo("pt-BR"), out number))
            {
                return number;
            }

            if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out number))
            {
                return number;
            }

            return null;
        }

        private static DateTime? ParseNullableDate(string value)
        {
            value = (value ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            DateTime date;
            double oaDate;

            string[] formats =
            {
                "dd/MM/yyyy",
                "dd/MM/yyyy HH:mm:ss",
                "yyyy-MM-dd",
                "yyyy-MM-dd HH:mm:ss",
                "MM/dd/yyyy",
                "MM/dd/yyyy HH:mm:ss"
            };

            if (DateTime.TryParseExact(value, formats, new CultureInfo("pt-BR"), DateTimeStyles.None, out date))
            {
                return date;
            }

            if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            {
                return date;
            }

            if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out oaDate))
            {
                try
                {
                    return DateTime.FromOADate(oaDate);
                }
                catch (ArgumentException)
                {
                    return null;
                }
            }

            return null;
        }

        private IEnumerable<TarefaConsultaItemViewModel> LoadTarefaConsultaItens(TarefaConsultaFiltroViewModel filtro)
        {
            EnsureAlocacaoSchema();

            var itens = db.Database.SqlQuery<TarefaConsultaItemViewModel>(
                @";WITH itensBase AS (
                    SELECT ri.TarefaNr,
                           r.StatusId,
                           sr.Descricao AS Status,
                           z.Nome AS Zona,
                           r.RomaneioNr,
                           CAST(r.Contato AS VARCHAR(50)) AS Contato,
                           r.OS,
                           ri.ItemNr,
                           estoque.Locacao,
                           COALESCE(NULLIF(ri.Descricao, ''), m.Descricao) AS Descricao,
                           ri.Qtde,
                           ri.ValorTotal,
                           COALESCE(arId.Prioridade, arNome.Prioridade) AS Prioridade,
                           ri.CriadoEm
                      FROM RomaneioItem ri
                      INNER JOIN Romaneio r ON r.Id = ri.RomaneioId
                      LEFT JOIN StatusRomaneio sr ON sr.Id = r.StatusId
                      LEFT JOIN AreaPedido ap ON ap.Id = r.VendedorId
                      LEFT JOIN AreaRomaneio arId ON arId.Id = ap.AreaId
                      LEFT JOIN AreaRomaneio arNome ON arNome.Area = ap.Area AND arId.Id IS NULL
                      LEFT JOIN Material m ON m.Codigo = ri.ItemNr
                      OUTER APPLY (
                          SELECT TOP 1 e.Locacao
                            FROM Estoque e
                           WHERE e.ItemNr = ri.ItemNr
                             AND (e.FilialId = ri.FilialId OR ri.FilialId IS NULL)
                             AND ISNULL(LTRIM(RTRIM(e.Locacao)), '') <> ''
                           ORDER BY CASE WHEN ISNULL(e.Saldo, 0) > 0 THEN 0 ELSE 1 END,
                                    ISNULL(e.Saldo, 0) DESC,
                                    e.Locacao
                      ) estoque
                      LEFT JOIN Locacao l ON l.Codigo = estoque.Locacao AND (l.FilialId = ri.FilialId OR l.FilialId IS NULL)
                      LEFT JOIN Zona z ON z.Id = l.ZonaId AND (z.FilialId = ri.FilialId OR z.FilialId IS NULL)
                     WHERE ri.FilialId = @p0
                       AND ISNULL(LTRIM(RTRIM(ri.TarefaNr)), '') <> ''
                       AND ISNULL(r.StatusId, 0) < 4
                       AND (@p1 IS NULL OR ri.TarefaNr = @p1)
                       AND (@p2 IS NULL OR r.RomaneioNr = @p2)
                       AND (@p3 IS NULL OR CAST(r.Contato AS VARCHAR(50)) = @p3)
                       AND (@p4 IS NULL OR r.OS = @p4)
                       AND (@p5 IS NULL OR ri.ItemNr = @p5)
                       AND (@p6 IS NULL OR z.Nome = @p6)
                       AND (@p7 IS NULL OR CONVERT(date, ri.CriadoEm) = @p7)
                )
                SELECT ib.TarefaNr,
                       ib.StatusId,
                       ib.Status,
                       ib.Zona,
                       ib.RomaneioNr,
                       ib.Contato,
                       ib.OS,
                       ib.ItemNr,
                       ib.Locacao,
                       ib.Descricao,
                       ib.Qtde,
                       ib.ValorTotal,
                       ib.Prioridade,
                       1 AS LinhasSumarizadas,
                       ib.CriadoEm
                  FROM itensBase ib
                 ORDER BY ib.TarefaNr,
                          ISNULL(ib.Locacao, ''),
                          ib.RomaneioNr,
                          ib.ItemNr",
                filialId,
                (object)NullIfWhiteSpace(filtro.TarefaNr) ?? DBNull.Value,
                (object)NullIfWhiteSpace(filtro.RomaneioNr) ?? DBNull.Value,
                (object)NullIfWhiteSpace(filtro.Contato) ?? DBNull.Value,
                (object)NullIfWhiteSpace(filtro.OS) ?? DBNull.Value,
                (object)NullIfWhiteSpace(filtro.ItemNr) ?? DBNull.Value,
                (object)NullIfWhiteSpace(filtro.Zona) ?? DBNull.Value,
                (object)filtro.Data ?? DBNull.Value).ToList();

            return itens;
        }

        private AlocacaoProcessamentoResultado GerarTarefasPorZona(IList<int> romaneioIdsAtualizados, string usuarioAtual)
        {
            var resultado = new AlocacaoProcessamentoResultado();
            if (romaneioIdsAtualizados == null || !romaneioIdsAtualizados.Any())
            {
                return resultado;
            }

            db.Database.ExecuteSqlCommand(
                "UPDATE RomaneioItem SET TarefaNr = NULL, ZonaId = NULL, LocacaoId = NULL, SeparadorId = NULL, DataSeparador = NULL, QtdeSeparada = NULL, StatusId = @p1 WHERE FilialId = @p0 AND RomaneioId IN (" + string.Join(",", romaneioIdsAtualizados) + ")",
                filialId,
                StatusAguardandoSeparacao);

            var itens = LoadItensParaAlocacao(romaneioIdsAtualizados);
            PersistirDadosBaseAlocacao(itens);
            int sequencia = GetNextTarefaSequence();

            foreach (var item in itens.Where(x => string.IsNullOrWhiteSpace(x.Locacao)))
            {
                resultado.ItensSemLocacao++;
                resultado.Mensagens.Add("Item " + item.ItemNr + " sem locacao. Tarefa nao gerada.");
                LogImportError("UploadPlanilhaAlocacao", "Item sem locacao", "Romaneio " + item.RomaneioNr + " / Item " + item.ItemNr);
            }

            foreach (var item in itens.Where(x => !string.IsNullOrWhiteSpace(x.Locacao) && !x.ZonaId.HasValue))
            {
                resultado.ItensSemZona++;
                resultado.Mensagens.Add("Item " + item.ItemNr + " sem zona vinculada. Tarefa nao gerada.");
                LogImportError("UploadPlanilhaAlocacao", "Item sem zona", "Romaneio " + item.RomaneioNr + " / Item " + item.ItemNr);
            }

            var itensValidos = itens
                .Where(x => !string.IsNullOrWhiteSpace(x.Locacao) && x.ZonaId.HasValue)
                .ToList();

            var romaneiosComOs = itensValidos
                .Where(x => !string.IsNullOrWhiteSpace(x.OS))
                .Select(x => x.RomaneioNr)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x)
                .ToList();

            foreach (var romaneioNr in romaneiosComOs)
            {
                resultado.Mensagens.Add("Romaneio " + romaneioNr + " nao alocado por possuir OS.");
            }

            foreach (var grupoZona in itensValidos
                .Where(x => string.IsNullOrWhiteSpace(x.OS))
                .GroupBy(x => x.ZonaId.Value)
                .OrderBy(g => g.Min(x => x.ZonaNome)))
            {
                int limiteLinhas = grupoZona.Select(x => x.QtdeLinha).FirstOrDefault() ?? 0;
                if (limiteLinhas <= 0)
                {
                    string nomeZona = grupoZona.Select(x => x.ZonaNome).FirstOrDefault() ?? grupoZona.Key.ToString(CultureInfo.InvariantCulture);
                    resultado.Mensagens.Add("Zona " + nomeZona + " sem QtdeLinha configurada. Tarefa nao gerada.");

                    foreach (var item in grupoZona)
                    {
                        LogImportError("UploadPlanilhaAlocacao", "Zona sem QtdeLinha", "Romaneio " + item.RomaneioNr + " / Item " + item.ItemNr + " / Zona " + nomeZona);
                    }

                    continue;
                }

                var linhasZona = SumarizarLinhasAlocacao(grupoZona)
                    .OrderBy(x => x.Locacao ?? string.Empty)
                    .ThenBy(x => x.ItemNr ?? string.Empty)
                    .ThenBy(x => x.Descricao ?? string.Empty)
                    .ToList();

                for (int i = 0; i < linhasZona.Count; i += limiteLinhas)
                {
                    var itensTarefa = linhasZona.Skip(i).Take(limiteLinhas).ToList();
                    AtualizarTarefaNr(itensTarefa, BuildTarefaNr(sequencia++));
                    resultado.TarefasGeradas++;
                    resultado.ItensAlocados += itensTarefa.Count;
                }
            }

            return resultado;
        }

        private void AtualizarBaseAlocacaoRomaneios(IList<int> romaneioIdsAtualizados)
        {
            if (romaneioIdsAtualizados == null || !romaneioIdsAtualizados.Any())
            {
                return;
            }

            var itens = LoadItensParaAlocacao(romaneioIdsAtualizados);
            PersistirDadosBaseAlocacao(itens);
        }

        private List<AlocacaoItemLinha> LoadItensParaAlocacao(IList<int> romaneioIdsAtualizados)
        {
            string locacaoIdSelect = ColumnExists("Locacao", "Id")
                ? "l.Id AS LocacaoId,"
                : "CAST(NULL AS INT) AS LocacaoId,";

            return db.Database.SqlQuery<AlocacaoItemLinha>(
                @"SELECT ri.Id,
                         r.RomaneioNr,
                         CAST(r.Contato AS VARCHAR(50)) AS Contato,
                         r.OS,
                         ri.ItemNr,
                         COALESCE(NULLIF(ri.Descricao, ''), m.Descricao) AS Descricao,
                         ri.Qtde,
                         ri.ValorTotal,
                         estoque.Locacao,
                         " + locacaoIdSelect + @"
                         z.Id AS ZonaId,
                         z.Nome AS ZonaNome,
                         COALESCE(arId.Prioridade, arNome.Prioridade) AS Prioridade,
                         z.QtdeLinha,
                         z.ValorPedido,
                         z.QtdeCliente,
                         ISNULL(z.ProntoDespacho, 0) AS ProntoDespacho
                    FROM RomaneioItem ri
                    INNER JOIN Romaneio r ON r.Id = ri.RomaneioId
                    LEFT JOIN AreaPedido ap ON ap.Id = r.VendedorId
                    LEFT JOIN AreaRomaneio arId ON arId.Id = ap.AreaId
                    LEFT JOIN AreaRomaneio arNome ON arNome.Area = ap.Area AND arId.Id IS NULL
                    LEFT JOIN Material m ON m.Codigo = ri.ItemNr
                    OUTER APPLY (
                        SELECT TOP 1 e.Locacao
                          FROM Estoque e
                         WHERE e.ItemNr = ri.ItemNr
                           AND (e.FilialId = ri.FilialId OR ri.FilialId IS NULL)
                           AND ISNULL(LTRIM(RTRIM(e.Locacao)), '') <> ''
                         ORDER BY CASE WHEN ISNULL(e.Saldo, 0) > 0 THEN 0 ELSE 1 END,
                                  ISNULL(e.Saldo, 0) DESC,
                                  e.Locacao
                    ) estoque
                    LEFT JOIN Locacao l ON l.Codigo = estoque.Locacao AND (l.FilialId = ri.FilialId OR l.FilialId IS NULL)
                    LEFT JOIN Zona z ON z.Id = l.ZonaId AND (z.FilialId = ri.FilialId OR z.FilialId IS NULL) AND z.Ativo = 1
                   WHERE ri.FilialId = @p0
                     AND ri.RomaneioId IN (" + string.Join(",", romaneioIdsAtualizados) + @")",
                filialId).ToList();
        }

        private List<AlocacaoLinhaSumarizada> SumarizarLinhasAlocacao(IEnumerable<AlocacaoItemLinha> itens)
        {
            return itens
                .GroupBy(x => new
                {
                    x.ZonaId,
                    Locacao = (x.Locacao ?? string.Empty).Trim(),
                    ItemNr = (x.ItemNr ?? string.Empty).Trim(),
                    Descricao = (x.Descricao ?? string.Empty).Trim()
                })
                .Select(g => new AlocacaoLinhaSumarizada
                {
                    RomaneioNrs = g.Select(x => x.RomaneioNr)
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList(),
                    ItemIds = g.Select(x => x.Id).Distinct().ToList(),
                    ItemNr = g.First().ItemNr,
                    Descricao = g.First().Descricao,
                    Locacao = g.First().Locacao,
                    ZonaId = g.First().ZonaId,
                    ZonaNome = g.First().ZonaNome,
                    Prioridade = g.First().Prioridade,
                    QtdeLinha = g.First().QtdeLinha
                })
                .ToList();
        }

        private void PersistirDadosBaseAlocacao(IEnumerable<AlocacaoItemLinha> itens)
        {
            var itensLista = itens == null ? new List<AlocacaoItemLinha>() : itens.ToList();
            if (!itensLista.Any())
            {
                return;
            }

            bool locacaoTableHasId = ColumnExists("Locacao", "Id");

            foreach (var item in itensLista)
            {
                if (!item.ZonaId.HasValue && !item.LocacaoId.HasValue)
                {
                    continue;
                }

                if (locacaoTableHasId)
                {
                    db.Database.ExecuteSqlCommand(
                        "UPDATE RomaneioItem SET ZonaId = @p0, LocacaoId = @p1 WHERE Id = @p2",
                        (object)item.ZonaId ?? DBNull.Value,
                        (object)item.LocacaoId ?? DBNull.Value,
                        item.Id);
                }
                else
                {
                    db.Database.ExecuteSqlCommand(
                        "UPDATE RomaneioItem SET ZonaId = @p0 WHERE Id = @p1",
                        (object)item.ZonaId ?? DBNull.Value,
                        item.Id);
                }
            }
        }

        private void AtualizarTarefaNr(IEnumerable<AlocacaoLinhaSumarizada> itens, string tarefaNr)
        {
            var itensLista = itens == null ? new List<AlocacaoLinhaSumarizada>() : itens.ToList();
            if (!itensLista.Any())
            {
                return;
            }

            var itemIds = itensLista.SelectMany(x => x.ItemIds).Distinct().ToList();
            foreach (var item in itemIds)
            {
                db.Database.ExecuteSqlCommand(
                    "UPDATE RomaneioItem SET TarefaNr = @p0, SeparadorId = NULL, DataSeparador = NULL, QtdeSeparada = NULL, StatusId = @p2 WHERE Id = @p1",
                    tarefaNr,
                    item,
                    StatusAguardandoSeparacao);
            }

            int? zonaIdTarefa = itensLista.Select(x => x.ZonaId).FirstOrDefault();
            if (zonaIdTarefa.HasValue && itemIds.Any())
            {
                db.Database.ExecuteSqlCommand(
                    "UPDATE RomaneioItem SET ZonaId = @p0 WHERE Id IN (" + string.Join(",", itemIds) + ")",
                    zonaIdTarefa.Value);
            }

            var romaneios = itensLista
                .SelectMany(x => x.RomaneioNrs)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (romaneios.Any())
            {
                foreach (var romaneioNr in romaneios)
                {
                    db.Database.ExecuteSqlCommand(
                        @"UPDATE Romaneio
                             SET StatusId = @p0
                               , SeparadorId = NULL
                               , DataSeparador = NULL
                            WHERE FilialId = @p1
                              AND LTRIM(RTRIM(RomaneioNr)) = @p2",
                        StatusAguardandoSeparacao,
                        filialId,
                        romaneioNr.Trim());
                }
            }
        }

        private int GetNextTarefaSequence()
        {
            string prefixo = "TZ" + Util.GetCurrentDateTime().ToString("yyyyMMdd", CultureInfo.InvariantCulture) + "-";
            string ultimo = db.Database.SqlQuery<string>(
                @"SELECT TOP 1 TarefaNr
                    FROM RomaneioItem
                   WHERE TarefaNr LIKE @p0
                   ORDER BY TarefaNr DESC",
                prefixo + "%").FirstOrDefault();

            if (string.IsNullOrWhiteSpace(ultimo))
            {
                return 1;
            }

            int numero;
            return int.TryParse(ultimo.Split('-').LastOrDefault(), out numero) ? numero + 1 : 1;
        }

        private static string BuildTarefaNr(int sequencia)
        {
            return "TZ" + Util.GetCurrentDateTime().ToString("yyyyMMdd", CultureInfo.InvariantCulture) + "-" + sequencia.ToString("0000", CultureInfo.InvariantCulture);
        }

        private void EnsureAlocacaoSchema()
        {
            var requiredColumns = new[]
            {
                new { Table = "Zona", Column = "AreaId" },
                new { Table = "Zona", Column = "Nome" },
                new { Table = "Zona", Column = "Ativo" },
                new { Table = "Locacao", Column = "Id" },
                new { Table = "Locacao", Column = "ZonaId" },
                new { Table = "Romaneio", Column = "OS" },
                new { Table = "RomaneioItem", Column = "Descricao" },
                new { Table = "RomaneioItem", Column = "LocacaoId" },
                new { Table = "RomaneioItem", Column = "ZonaId" },
                new { Table = "RomaneioItem", Column = "ValorUnitario" },
                new { Table = "RomaneioItem", Column = "ValorTotal" },
                new { Table = "RomaneioItem", Column = "TarefaNr" }
            };

            foreach (var item in requiredColumns)
            {
                bool exists = db.Database.SqlQuery<int>(
                    @"SELECT COUNT(1)
                        FROM INFORMATION_SCHEMA.COLUMNS
                       WHERE TABLE_NAME = @p0
                         AND COLUMN_NAME = @p1",
                    item.Table,
                    item.Column).FirstOrDefault() > 0;

                if (!exists)
                {
                    throw new InvalidOperationException("Schema desatualizado. Execute o script docs/sql/20260702_AlocacaoPedidosZona.sql.");
                }
            }
        }

        private bool ColumnExists(string tableName, string columnName)
        {
            return db.Database.SqlQuery<int>(
                @"SELECT COUNT(1)
                    FROM INFORMATION_SCHEMA.COLUMNS
                   WHERE TABLE_NAME = @p0
                     AND COLUMN_NAME = @p1",
                tableName,
                columnName).FirstOrDefault() > 0;
        }

        private void LogImportError(string action, string instrucao, string mensagem)
        {
            try
            {
                db.AppLogErro.Add(new AppLogErro
                {
                    Area = "SeparacaoApp",
                    Controller = "Romaneio",
                    Action = action,
                    Instrucao = instrucao,
                    ErrorCode = "ALOCACAO_ZONA",
                    ErrorMessage = mensagem,
                    Usuario = Util.GetCurrentUser(),
                    DataHora = Util.GetCurrentDateTime(),
                    FilialId = filialId
                });
                db.SaveChanges();
            }
            catch
            {
                Trace.TraceError("Falha ao gravar AppLogErro: {0}", mensagem);
            }
        }

        private static string NullIfWhiteSpace(string value)
        {
            value = (value ?? string.Empty).Trim();
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        private static string NormalizeColumnName(string value)
        {
            value = Util.RemoverAcentuacao((value ?? string.Empty).Trim().ToLowerInvariant());
            var normalized = new StringBuilder();
            foreach (char ch in value)
            {
                if (char.IsLetterOrDigit(ch))
                {
                    normalized.Append(ch);
                }
            }

            return normalized.ToString();
        }

        private static bool CanIncludeInMapa(AreaRomaneio areaRomaneio)
        {
            return areaRomaneio != null && (areaRomaneio.Mapa ?? false);
        }

        private static string GetMapaLocacaoPrefix(string locacao, int breakLength)
        {
            locacao = (locacao ?? string.Empty).Trim().ToUpperInvariant();
            breakLength = Math.Max(breakLength, 1);
            return locacao.Length <= breakLength ? locacao : locacao.Substring(0, breakLength);
        }

        private int GetMapaBreakLength()
        {
            string valor = db.AppConfig
                .Where(c => c.Nome == "QuebraMapa" && (c.FilialId == filialId || !c.FilialId.HasValue))
                .OrderByDescending(c => c.FilialId.HasValue)
                .Select(c => c.Valor)
                .FirstOrDefault();

            int breakLength;
            return int.TryParse((valor ?? string.Empty).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out breakLength) && breakLength > 0
                ? breakLength
                : 2;
        }

        private byte[] BuildMapaPdf(IList<MapaLinhaViewModel> linhas, IList<string> romaneiosRelacionados, int breakLength)
        {
            var doc = new Document();
            doc.Info.Title = "MAPA de Separação";

            var section = doc.AddSection();
            section.PageSetup.PageFormat = PageFormat.A4;
            section.PageSetup.Orientation = Orientation.Portrait;
            section.PageSetup.TopMargin = "2.2cm";
            section.PageSetup.BottomMargin = "1.2cm";
            section.PageSetup.LeftMargin = "0.8cm";
            section.PageSetup.RightMargin = "0.8cm";
            section.PageSetup.HeaderDistance = "0.35cm";

            var header = section.Headers.Primary;
            var titleTable = header.AddTable();
            titleTable.Borders.Width = 0;
            titleTable.AddColumn("3.5cm");
            titleTable.AddColumn("12.4cm");
            titleTable.AddColumn("3.5cm");

            var titleRow = titleTable.AddRow();
            titleRow.Cells[1].VerticalAlignment = MigraDoc.DocumentObjectModel.Tables.VerticalAlignment.Center;
            titleRow.Cells[2].VerticalAlignment = MigraDoc.DocumentObjectModel.Tables.VerticalAlignment.Center;

            var titulo = titleRow.Cells[1].AddParagraph("MAPA DE SEPARA\u00C7\u00C3O");
            titulo.Format.Alignment = ParagraphAlignment.Center;
            titulo.Format.Font.Size = 19;
            titulo.Format.Font.Bold = true;

            var paginacao = titleRow.Cells[2].AddParagraph();
            paginacao.Format.Alignment = ParagraphAlignment.Right;
            paginacao.Format.Font.Size = 8;
            paginacao.AddText("P\u00E1gina ");
            paginacao.AddPageField();
            paginacao.AddText(" de ");
            paginacao.AddNumPagesField();

            var subtitulo = header.AddParagraph(Util.GetCurrentDateTime().ToString("dd/MM/yyyy HH:mm"));
            subtitulo.Format.Alignment = ParagraphAlignment.Center;
            subtitulo.Format.Font.Size = 9;
            subtitulo.Format.SpaceAfter = "0.45cm";

            bool primeiraPaginaPrefixo = true;
            foreach (var grupoPrefixo in linhas
                .GroupBy(l => GetMapaLocacaoPrefix(l.Locacao, breakLength))
                .OrderBy(g => g.Key))
            {
                if (!primeiraPaginaPrefixo)
                {
                    section.AddPageBreak();
                }

                var table = CreateMapaTable(section);
                bool primeiraLocacao = true;

                foreach (var grupoLocacao in grupoPrefixo
                    .GroupBy(l => l.Locacao ?? string.Empty)
                    .OrderBy(g => g.Key))
                {
                    if (!primeiraLocacao)
                    {
                        var separator = table.AddRow();
                        separator.Height = "0.22cm";
                        for (int i = 0; i < 4; i++)
                        {
                            separator.Cells[i].Borders.Top.Width = 0.7;
                        }
                    }

                    bool primeiraLinhaLocacao = true;
                    foreach (var linha in grupoLocacao.OrderBy(g => g.ItemNr ?? string.Empty))
                    {
                        var row = table.AddRow();
                        row.Cells[0].AddParagraph(primeiraLinhaLocacao ? (grupoLocacao.Key ?? string.Empty) : string.Empty);
                        row.Cells[1].AddParagraph(linha.ItemNr ?? string.Empty);
                        row.Cells[2].AddParagraph(linha.Descricao ?? string.Empty);
                        row.Cells[3].AddParagraph(linha.Qtde.ToString(CultureInfo.InvariantCulture));
                        row.Cells[3].Format.Alignment = ParagraphAlignment.Right;

                        if (primeiraLinhaLocacao)
                        {
                            row.Cells[0].Format.Font.Bold = true;
                        }

                        row.Cells[1].Format.Font.Bold = true;
                        primeiraLinhaLocacao = false;
                    }

                    primeiraLocacao = false;
                }

                primeiraPaginaPrefixo = false;
            }

            if (romaneiosRelacionados != null && romaneiosRelacionados.Any())
            {
                section.AddPageBreak();

                var tituloRomaneios = section.AddParagraph("Romaneios:");
                tituloRomaneios.Format.Font.Bold = true;
                tituloRomaneios.Format.Font.Size = 10;
                tituloRomaneios.Format.SpaceAfter = "0.20cm";

                var listaRomaneios = section.AddParagraph(string.Join(" | ", romaneiosRelacionados.Where(r => !string.IsNullOrWhiteSpace(r))));
                listaRomaneios.Format.Font.Size = 9;
                listaRomaneios.Format.SpaceAfter = "0.25cm";

                var totalRomaneios = section.AddParagraph("Quantidade de Romaneios: " + romaneiosRelacionados.Count.ToString(CultureInfo.InvariantCulture));
                totalRomaneios.Format.Font.Bold = true;
                totalRomaneios.Format.Font.Size = 9.5;
            }

            var renderer = new PdfDocumentRenderer { Document = doc };
            renderer.RenderDocument();

            using (var stream = new MemoryStream())
            {
                renderer.PdfDocument.Save(stream, false);
                return stream.ToArray();
            }
        }

        private static Table CreateMapaTable(Section section)
        {
            var table = section.AddTable();
            table.Borders.Width = 0;
            table.Rows.LeftIndent = 0;
            table.Format.Font.Size = 8.5;
            table.TopPadding = "0.03cm";
            table.BottomPadding = "0.03cm";

            table.AddColumn("4cm");
            table.AddColumn("3.3cm");
            table.AddColumn("9.1cm");
            table.AddColumn("2cm");

            var headerRow = table.AddRow();
            headerRow.HeadingFormat = true;
            headerRow.Format.Font.Bold = true;
            headerRow.Format.SpaceAfter = "0.12cm";
            headerRow.Cells[0].AddParagraph("LOCA\u00C7\u00C3O");
            headerRow.Cells[1].AddParagraph("ITEM NR");
            headerRow.Cells[2].AddParagraph("DESCRI\u00C7\u00C3O");
            headerRow.Cells[3].AddParagraph("QTDE");

            return table;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }

            base.Dispose(disposing);
        }

        private sealed class RomaneioImportRow
        {
            public string RomaneioNr { get; set; }
            public string ItemNr { get; set; }
            public string Vendedor { get; set; }
            public int? ContatoNr { get; set; }
            public string OS { get; set; }
            public DateTime? DataFaturamento { get; set; }
            public string Descricao { get; set; }
            public decimal? ValorUnitario { get; set; }
            public decimal? ValorTotal { get; set; }
            public int? Itens { get; set; }
            public int? Pecas { get; set; }
        }

        private sealed class MapaLinhaViewModel
        {
            public string Locacao { get; set; }
            public string ItemNr { get; set; }
            public string Descricao { get; set; }
            public int Qtde { get; set; }
            public string RomaneioNr { get; set; }
        }

        private sealed class RomaneioExportacaoQueryItem
        {
            public int Id { get; set; }
            public string RomaneioNr { get; set; }
        }

        private sealed class ProdutividadeConfig
        {
            public decimal PercentualRomaneios { get; set; }
            public decimal PercentualLinhas { get; set; }
            public decimal PercentualPecas { get; set; }
        }

        private sealed class AlocacaoItemLinha
        {
            public int Id { get; set; }
            public string RomaneioNr { get; set; }
            public string Contato { get; set; }
            public string OS { get; set; }
            public string ItemNr { get; set; }
            public string Descricao { get; set; }
            public int? Qtde { get; set; }
            public decimal? ValorTotal { get; set; }
            public string Locacao { get; set; }
            public int? LocacaoId { get; set; }
            public int? ZonaId { get; set; }
            public string ZonaNome { get; set; }
            public int? Prioridade { get; set; }
            public int? QtdeLinha { get; set; }
            public decimal? ValorPedido { get; set; }
            public int? QtdeCliente { get; set; }
            public bool ProntoDespacho { get; set; }
        }

        private sealed class AlocacaoLinhaSumarizada
        {
            public IList<int> ItemIds { get; set; }
            public IList<string> RomaneioNrs { get; set; }
            public string ItemNr { get; set; }
            public string Descricao { get; set; }
            public string Locacao { get; set; }
            public int? ZonaId { get; set; }
            public string ZonaNome { get; set; }
            public int? Prioridade { get; set; }
            public int? QtdeLinha { get; set; }

            public AlocacaoLinhaSumarizada()
            {
                ItemIds = new List<int>();
                RomaneioNrs = new List<string>();
            }
        }

        private sealed class AlocacaoProcessamentoResultado
        {
            public int TarefasGeradas { get; set; }
            public int ItensAlocados { get; set; }
            public int ItensSemLocacao { get; set; }
            public int ItensSemZona { get; set; }
            public IList<string> Mensagens { get; set; }

            public AlocacaoProcessamentoResultado()
            {
                Mensagens = new List<string>();
            }
        }

        private sealed class ConferenciaResumo
        {
            public int? ConferenteId { get; set; }
            public DateTime? DataConferencia { get; set; }
        }
    }

    internal static class RomaneioViewModelExtensions
    {
        public static T With<T>(this T model, Action<T> configure)
        {
            configure(model);
            return model;
        }
    }
}
