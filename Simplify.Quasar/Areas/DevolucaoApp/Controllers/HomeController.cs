using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Shapes;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using Simplify.Quasar.Areas.DevolucaoApp.ViewModels;
using Simplify.Quasar.Custom;
using Simplify.Quasar.Models;

namespace Simplify.Quasar.Areas.DevolucaoApp.Controllers
{
    [ValidateSession]
    public class HomeController : Controller
    {
        private const string ControleNrConfigName = "ControleNr";
        private const string DevolucaoComplementoTableName = "dbo.DevolucaoComplemento";
        private static readonly string ValorUnitarioColumnName = "ValorUnit\u00E1rio";
        private static readonly CultureInfo PtBrCulture = CultureInfo.GetCultureInfo("pt-BR");

        private readonly Quasar_Entities db = new Quasar_Entities();
        private readonly int filialId = Util.GetCurrentFilial();
        private readonly JavaScriptSerializer serializer = new JavaScriptSerializer();

        public ActionResult Dashboard(DateTime? dataInicial, DateTime? dataFinal)
        {
            return View(
                "~/Views/Shared/ProcessDashboard.cshtml",
                ProcessDashboardViewModel.Create("Devolu\u00E7\u00E3o", "DevolucaoApp", dataInicial, dataFinal));
        }

        public ActionResult Index(int? savedId = null)
        {
            DevolucaoCadastroViewModel vm = PrepareViewModel(new DevolucaoCadastroViewModel
            {
                Movimento = "Devolu\u00E7\u00E3o",
                Retirar = "Sim",
                StatusId = ResolveStatusDevolucaoInicial(),
                Itens = new List<DevolucaoCadastroItemViewModel>()
            });

            if (savedId.HasValue)
            {
                PopulateLastSavedDevolucao(vm, savedId.Value);
            }

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Index(DevolucaoCadastroViewModel vm)
        {
            vm = PrepareViewModel(vm);
            string nfVendaNormalizada = NormalizeNotaFiscalNr(vm.NFVenda);
            if (!string.IsNullOrWhiteSpace(nfVendaNormalizada))
            {
                vm.NFVenda = nfVendaNormalizada;
            }

            List<DevolucaoCadastroItemViewModel> itens = DeserializeItens(vm.ItensJson);
            vm.Itens = itens;
            vm.ItensJson = SerializeItens(itens);

            DocExpedicao documentoVenda = FindDocExpedicao(vm.NFVenda);
            PopulateDocExpedicaoFields(vm, documentoVenda);
            ValidateViewModel(vm, itens, documentoVenda);

            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            string vendedorFinal = ResolveVendedor(vm, documentoVenda);
            DateTime agora = Util.GetCurrentDateTime();
            string usuarioAtual = Util.GetCurrentUser();

            try
            {
                using (DbContextTransaction tr = db.Database.BeginTransaction(IsolationLevel.Serializable))
                {
                    EnsureDevolucaoComplementoTable();

                    string controleNr = GetAndIncrementControleNr(usuarioAtual, agora);
                    int statusDevolucaoId = vm.StatusId ?? ResolveStatusDevolucaoInicial();

                    int devolucaoId = InsertDevolucao(vm, controleNr, vendedorFinal, statusDevolucaoId, usuarioAtual, agora);

                    NotaFiscal notaFiscal = new NotaFiscal
                    {
                        Movimento = "D",
                        TipoId = 2,
                        StatusId = 1,
                        Numero = string.IsNullOrWhiteSpace(vm.NFDevolucao) ? controleNr : vm.NFDevolucao.Trim(),
                        Emissor = vm.Cliente ?? string.Empty,
                        DataEmissao = agora,
                        Descricao = string.Concat("Devolucao ", controleNr),
                        Observacoes = vm.Observacao,
                        CriadoPor = usuarioAtual,
                        CriadoEm = agora,
                        FilialId = filialId
                    };

                    db.NotaFiscal.Add(notaFiscal);
                    db.SaveChanges();
                    EnsureNotaFiscalTipoDevolucao(notaFiscal.Id, filialId);

                    foreach (DevolucaoCadastroItemViewModel item in itens)
                    {
                        InsertDevolucaoItem(devolucaoId, item, statusDevolucaoId, usuarioAtual, agora);

                        NotaFiscalItem notaFiscalItem = new NotaFiscalItem
                        {
                            NotaFiscalId = notaFiscal.Id,
                            Item = NormalizeCodigoMaterial(item.ItemNr),
                            Quantidade = item.Quantidade,
                            Volume = controleNr,
                            StatusId = 1,
                            Observacao = item.Observacao,
                            CriadoPor = usuarioAtual,
                            CriadoEm = agora,
                            FilialId = filialId
                        };

                        db.NotaFiscalItem.Add(notaFiscalItem);
                    }

                    db.SaveChanges();

                    InsertDevolucaoComplemento(devolucaoId, documentoVenda, notaFiscal.Id, usuarioAtual, agora);

                    tr.Commit();
                    TempData["SuccessMessage"] = string.Concat("Cadastro gravado com sucesso. Controle: ", controleNr);
                    return RedirectToAction("Index", new { savedId = devolucaoId });
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(vm);
            }
        }

        [HttpGet]
        public ActionResult Print(int id)
        {
            EnsureDevolucaoComplementoTable();

            DevolucaoPrintViewModel vm = BuildPrintViewModel(id);
            if (vm == null)
            {
                return HttpNotFound("Devolução não localizada.");
            }

            byte[] pdf = BuildAutorizacaoDevolucaoPdf(vm);
            return File(pdf, "application/pdf");
        }

        [HttpGet]
        public ActionResult Consulta()
        {
            Dictionary<int, string> statusLookup = db.StatusDevolucao
                .AsNoTracking()
                .OrderBy(x => x.Id)
                .ToList()
                .GroupBy(x => x.Id)
                .ToDictionary(x => x.Key, x => x.Select(y => y.Nome ?? string.Empty).FirstOrDefault() ?? string.Empty);

            Dictionary<int, string> transportadoraLookup = db.Transportadora
                .AsNoTracking()
                .Where(x => x.FilialId == filialId || x.FilialId == null)
                .ToList()
                .GroupBy(x => x.Id)
                .ToDictionary(
                    x => x.Key,
                    x => x.OrderByDescending(y => y.FilialId == filialId)
                        .Select(y => y.Nome ?? string.Empty)
                        .FirstOrDefault() ?? string.Empty);

            List<DevolucaoConsultaViewModel> model = db.Devolucao
                .AsNoTracking()
                .Where(x => x.FilialId == filialId)
                .OrderByDescending(x => x.CriadoEm)
                .ThenByDescending(x => x.Id)
                .Select(x => new
                {
                    x.Id,
                    x.DevolucaoNr,
                    x.Cliente,
                    x.TransportadoraId,
                    x.StatusId,
                    x.CriadoEm
                })
                .ToList()
                .Select(x => new DevolucaoConsultaViewModel
                {
                    Id = x.Id,
                    ControleNr = x.DevolucaoNr,
                    Cliente = x.Cliente,
                    Transportadora = x.TransportadoraId.HasValue && transportadoraLookup.ContainsKey(x.TransportadoraId.Value)
                        ? transportadoraLookup[x.TransportadoraId.Value]
                        : string.Empty,
                    StatusNome = x.StatusId.HasValue && statusLookup.ContainsKey(x.StatusId.Value)
                        ? statusLookup[x.StatusId.Value]
                        : string.Empty,
                    DataCadastro = x.CriadoEm
                })
                .ToList();

            return View(model);
        }

        [HttpGet]
        public ActionResult Ocorrencias()
        {
            EnsureDevolucaoComplementoTable();

            int statusOcorrenciaId = ResolveStatusDevolucaoIdByName("Ocorrencia") ?? 3;
            List<DevolucaoOcorrenciaConsultaViewModel> model = BuildDevolucaoOcorrenciaConsulta(statusOcorrenciaId);
            return View(model);
        }

        [HttpGet]
        public ActionResult OcorrenciaDetalhe(int id)
        {
            EnsureDevolucaoComplementoTable();

            DevolucaoOcorrenciaViewModel vm = BuildDevolucaoOcorrenciaViewModel(id);
            if (vm == null)
            {
                return HttpNotFound("Devolução não localizada.");
            }

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult OcorrenciaDetalhe(DevolucaoOcorrenciaViewModel vm)
        {
            EnsureDevolucaoComplementoTable();

            DevolucaoOcorrenciaViewModel atual = BuildDevolucaoOcorrenciaViewModel(vm.Id);
            if (atual == null)
            {
                return HttpNotFound("Devolução não localizada.");
            }

            List<DevolucaoOcorrenciaItemViewModel> itensTratamento = (vm.Itens ?? new List<DevolucaoOcorrenciaItemViewModel>())
                .Where(x => x.NovoStatusId.HasValue || x.QuantidadeTratada.HasValue || !string.IsNullOrWhiteSpace(x.ObservacaoTratamento))
                .ToList();

            if (itensTratamento.Count == 0)
            {
                ModelState.AddModelError(string.Empty, "Selecione pelo menos um item para tratamento.");
            }

            HashSet<int> statusPermitidos = new HashSet<int>(new[] { atual.StatusCorrigidaId, atual.StatusFinalizadoId });
            Dictionary<int, DevolucaoOcorrenciaItemViewModel> itensAtuais = (atual.Itens ?? new List<DevolucaoOcorrenciaItemViewModel>())
                .ToDictionary(x => x.DevolucaoItemId, x => x);

            foreach (DevolucaoOcorrenciaItemViewModel item in itensTratamento)
            {
                DevolucaoOcorrenciaItemViewModel itemAtual;
                if (!itensAtuais.TryGetValue(item.DevolucaoItemId, out itemAtual))
                {
                    ModelState.AddModelError(string.Empty, "Item de devolução não localizado.");
                    continue;
                }

                if (!itemAtual.PermiteTratamento)
                {
                    ModelState.AddModelError(string.Empty, string.Concat("Item ", itemAtual.ItemNr, ": este item não está mais pendente de ocorrência."));
                    continue;
                }

                if (!item.NovoStatusId.HasValue || !statusPermitidos.Contains(item.NovoStatusId.Value))
                {
                    ModelState.AddModelError(string.Empty, string.Concat("Item ", itemAtual.ItemNr, ": selecione um status válido para tratamento."));
                }

                if (!item.QuantidadeTratada.HasValue || item.QuantidadeTratada.Value <= 0)
                {
                    ModelState.AddModelError(string.Empty, string.Concat("Item ", itemAtual.ItemNr, ": informe uma quantidade de peças maior que zero."));
                }
                else if (item.QuantidadeTratada.Value > itemAtual.QuantidadeOcorrencia)
                {
                    ModelState.AddModelError(string.Empty, string.Concat("Item ", itemAtual.ItemNr, ": a quantidade tratada não pode ser maior que a quantidade com ocorrência."));
                }
            }

            if (!ModelState.IsValid)
            {
                MergeDevolucaoOcorrenciaInput(atual, vm);
                return View(atual);
            }

            string usuarioAtual = Util.GetCurrentUser();
            DateTime agora = Util.GetCurrentDateTime();

            try
            {
                using (DbContextTransaction tr = db.Database.BeginTransaction(IsolationLevel.Serializable))
                {
                    bool devolucaoExiste = db.Devolucao
                        .AsNoTracking()
                        .Any(x => x.Id == vm.Id && x.FilialId == filialId);

                    if (!devolucaoExiste)
                    {
                        tr.Rollback();
                        return HttpNotFound("Devolução não localizada.");
                    }

                    DevolucaoComplemento complemento = db.DevolucaoComplemento
                        .AsNoTracking()
                        .FirstOrDefault(x => x.DevolucaoId == vm.Id);

                    List<DevolucaoItem> devolucaoItens = db.DevolucaoItem
                        .AsNoTracking()
                        .Where(x => x.DevolucaoId == vm.Id)
                        .OrderBy(x => x.Id)
                        .ToList();

                    Dictionary<int, DevolucaoItem> devolucaoItemLookup = devolucaoItens.ToDictionary(x => x.Id, x => x);

                    List<NotaFiscalItem> notaFiscalItens = new List<NotaFiscalItem>();
                    Dictionary<int, int> notaFiscalItemMap = new Dictionary<int, int>();
                    if (complemento != null && complemento.NotaFiscalId.HasValue)
                    {
                        notaFiscalItens = db.NotaFiscalItem
                            .AsNoTracking()
                            .Where(x => x.NotaFiscalId == complemento.NotaFiscalId.Value && x.FilialId == filialId)
                            .OrderBy(x => x.Id)
                            .ToList();

                        notaFiscalItemMap = BuildDevolucaoNotaFiscalItemMap(devolucaoItens, notaFiscalItens);
                    }

                    Dictionary<int, NotaFiscalItem> notaFiscalItemLookup = notaFiscalItens.ToDictionary(x => x.Id, x => x);

                    foreach (DevolucaoOcorrenciaItemViewModel itemTratamento in itensTratamento)
                    {
                        DevolucaoItem devolucaoItem;
                        if (!devolucaoItemLookup.TryGetValue(itemTratamento.DevolucaoItemId, out devolucaoItem))
                        {
                            throw new InvalidOperationException("Item de devolução não localizado para atualização.");
                        }

                        int quantidadeAnterior = devolucaoItem.QtdeOcorrencia ?? 0;
                        int quantidadeTratada = itemTratamento.QuantidadeTratada ?? 0;
                        if (quantidadeAnterior <= 0)
                        {
                            throw new InvalidOperationException(string.Concat("Item ", devolucaoItem.ItemNr, ": não possui quantidade de ocorrência válida para tratamento."));
                        }

                        NotaFiscalItem notaFiscalItem = null;
                        int notaFiscalItemId;
                        if (notaFiscalItemMap.TryGetValue(devolucaoItem.Id, out notaFiscalItemId))
                        {
                            notaFiscalItemLookup.TryGetValue(notaFiscalItemId, out notaFiscalItem);
                        }

                        devolucaoItem.QtdeOcorrencia = quantidadeTratada;
                        devolucaoItem.Observacao = (itemTratamento.ObservacaoTratamento ?? string.Empty).Trim();
                        devolucaoItem.ModificadoPor = usuarioAtual;
                        devolucaoItem.ModificadoEm = agora;

                        if (itemTratamento.NovoStatusId.Value == atual.StatusCorrigidaId)
                        {
                            if (notaFiscalItem == null)
                            {
                                throw new InvalidOperationException(string.Concat("Item ", devolucaoItem.ItemNr, ": item da Nota Fiscal não localizado para correção."));
                            }

                            decimal novaQuantidadeNotaFiscal = notaFiscalItem.Quantidade + quantidadeTratada;
                            int rowsNotaFiscalCorrigida = db.Database.ExecuteSqlCommand(
                                @"
UPDATE dbo.NotaFiscalItem
SET Quantidade = @quantidade,
    StatusId = @statusId,
    ModificadoPor = @modificadoPor,
    ModificadoEm = @modificadoEm
WHERE Id = @id
  AND FilialId = @filialId",
                                new SqlParameter("@quantidade", novaQuantidadeNotaFiscal),
                                new SqlParameter("@statusId", 4),
                                new SqlParameter("@modificadoPor", usuarioAtual),
                                new SqlParameter("@modificadoEm", agora),
                                new SqlParameter("@id", notaFiscalItem.Id),
                                new SqlParameter("@filialId", filialId));

                            if (rowsNotaFiscalCorrigida == 0)
                            {
                                throw new InvalidOperationException(string.Concat("Item ", devolucaoItem.ItemNr, ": item da Nota Fiscal não localizado para correção."));
                            }

                            notaFiscalItem.Quantidade = novaQuantidadeNotaFiscal;
                            notaFiscalItem.StatusId = 4;
                            notaFiscalItem.ModificadoPor = usuarioAtual;
                            notaFiscalItem.ModificadoEm = agora;
                            devolucaoItem.StatusId = atual.StatusCorrigidaId;
                        }
                        else
                        {
                            if (quantidadeAnterior != quantidadeTratada)
                            {
                                if (notaFiscalItem == null)
                                {
                                    throw new InvalidOperationException(string.Concat("Item ", devolucaoItem.ItemNr, ": item da Nota Fiscal não localizado para ajuste de quantidade."));
                                }

                                decimal novaQuantidadeNotaFiscal = notaFiscalItem.Quantidade + (quantidadeAnterior - quantidadeTratada);
                                int rowsNotaFiscalFinalizada = db.Database.ExecuteSqlCommand(
                                    @"
UPDATE dbo.NotaFiscalItem
SET Quantidade = @quantidade,
    ModificadoPor = @modificadoPor,
    ModificadoEm = @modificadoEm
WHERE Id = @id
  AND FilialId = @filialId",
                                    new SqlParameter("@quantidade", novaQuantidadeNotaFiscal),
                                    new SqlParameter("@modificadoPor", usuarioAtual),
                                    new SqlParameter("@modificadoEm", agora),
                                    new SqlParameter("@id", notaFiscalItem.Id),
                                    new SqlParameter("@filialId", filialId));

                                if (rowsNotaFiscalFinalizada == 0)
                                {
                                    throw new InvalidOperationException(string.Concat("Item ", devolucaoItem.ItemNr, ": item da Nota Fiscal não localizado para ajuste de quantidade."));
                                }

                                notaFiscalItem.Quantidade = novaQuantidadeNotaFiscal;
                                notaFiscalItem.ModificadoPor = usuarioAtual;
                                notaFiscalItem.ModificadoEm = agora;
                            }

                            devolucaoItem.StatusId = atual.StatusFinalizadoId;
                        }

                        int rowsDevolucaoItem = db.Database.ExecuteSqlCommand(
                            @"
UPDATE dbo.DevolucaoItem
SET StatusId = @statusId,
    QtdeOcorrencia = @qtdeOcorrencia,
    Observacao = @observacao,
    ModificadoPor = @modificadoPor,
    ModificadoEm = @modificadoEm
WHERE Id = @id
  AND DevolucaoId = @devolucaoId",
                            new SqlParameter("@statusId", (object)devolucaoItem.StatusId ?? DBNull.Value),
                            new SqlParameter("@qtdeOcorrencia", (object)devolucaoItem.QtdeOcorrencia ?? DBNull.Value),
                            new SqlParameter("@observacao", (object)(devolucaoItem.Observacao ?? string.Empty)),
                            new SqlParameter("@modificadoPor", usuarioAtual),
                            new SqlParameter("@modificadoEm", agora),
                            new SqlParameter("@id", devolucaoItem.Id),
                            new SqlParameter("@devolucaoId", vm.Id));

                        if (rowsDevolucaoItem == 0)
                        {
                            throw new InvalidOperationException(string.Concat("Item ", devolucaoItem.ItemNr, ": não foi possível atualizar a devolução."));
                        }
                    }

                    bool possuiItensComOcorrencia = devolucaoItens.Any(x => (x.StatusId ?? 0) == atual.StatusOcorrenciaId);
                    int rowsDevolucao = db.Database.ExecuteSqlCommand(
                        @"
UPDATE dbo.Devolucao
SET StatusId = @statusId,
    ModificadoPor = @modificadoPor,
    ModificadoEm = @modificadoEm
WHERE Id = @id
  AND FilialId = @filialId",
                        new SqlParameter("@statusId", possuiItensComOcorrencia ? atual.StatusOcorrenciaId : atual.StatusFinalizadoId),
                        new SqlParameter("@modificadoPor", usuarioAtual),
                        new SqlParameter("@modificadoEm", agora),
                        new SqlParameter("@id", vm.Id),
                        new SqlParameter("@filialId", filialId));

                    if (rowsDevolucao == 0)
                    {
                        tr.Rollback();
                        return HttpNotFound("Devoluçaõ não localizada.");
                    }

                    tr.Commit();
                }

                TempData["SuccessMessage"] = "Tratamento da ocorrência gravado com sucesso.";

                DevolucaoOcorrenciaViewModel atualizado = BuildDevolucaoOcorrenciaViewModel(vm.Id);
                bool aindaPossuiOcorrencia = atualizado != null && (atualizado.Itens ?? new List<DevolucaoOcorrenciaItemViewModel>()).Any(x => x.PermiteTratamento);
                if (aindaPossuiOcorrencia)
                {
                    return RedirectToAction("OcorrenciaDetalhe", new { id = vm.Id });
                }

                return RedirectToAction("Ocorrencias");
            }
            catch (Exception ex)
            {
                MergeDevolucaoOcorrenciaInput(atual, vm);
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(atual);
            }
        }

        [HttpGet]
        public ActionResult Detalhe(int id)
        {
            EnsureDevolucaoComplementoTable();

            DevolucaoPrintViewModel vm = BuildPrintViewModel(id);
            if (vm == null)
            {
                return HttpNotFound("Devolução não localizada.");
            }

            PrepareDetailViewModel(vm);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Detalhe(DevolucaoPrintViewModel vm)
        {
            EnsureDevolucaoComplementoTable();

            DevolucaoPrintViewModel atual = BuildPrintViewModel(vm.Id);
            if (atual == null)
            {
                return HttpNotFound("Devolução não localizada.");
            }

            if (!vm.StatusId.HasValue)
            {
                ModelState.AddModelError("StatusId", "Informe o status.");
            }

            HashSet<int> allowedStatusIds = GetAllowedDetailStatusIds();
            if (!atual.StatusId.HasValue || !allowedStatusIds.Contains(atual.StatusId.Value))
            {
                ModelState.AddModelError("StatusId", "Neste formulário a devolução pode ser alterada para Cancelado.");
            }

            if (vm.StatusId.HasValue && !allowedStatusIds.Contains(vm.StatusId.Value))
            {
                ModelState.AddModelError("StatusId", "Neste formulário o status pode ser alterado para Cancelado.");
            }

            if (string.IsNullOrWhiteSpace(vm.Movimento) || !IsMovimentoDetalheValido(vm.Movimento))
            {
                ModelState.AddModelError("Movimento", "Informe um tipo de movimento válido.");
            }

            if (string.IsNullOrWhiteSpace(vm.Retirar) || !IsRetirarDetalheValido(vm.Retirar))
            {
                ModelState.AddModelError("Retirar", "Informe se deve efetuar retirada.");
            }

            if (!vm.MotivoId.HasValue)
            {
                ModelState.AddModelError("MotivoId", "Informe o motivo.");
            }
            else
            {
                bool motivoExiste = db.MotivoDevolucao
                    .AsNoTracking()
                    .Any(x => x.Id == vm.MotivoId.Value);

                if (!motivoExiste)
                {
                    ModelState.AddModelError("MotivoId", "Motivo informado não localizado.");
                }
            }

            if (!vm.TransportadoraId.HasValue)
            {
                ModelState.AddModelError("TransportadoraId", "Informe a transportadora.");
            }
            else
            {
                bool transportadoraExiste = db.Transportadora
                    .AsNoTracking()
                    .Any(x => x.Id == vm.TransportadoraId.Value && (!x.FilialId.HasValue || x.FilialId.Value == filialId));

                if (!transportadoraExiste)
                {
                    ModelState.AddModelError("TransportadoraId", "Transportadora informada não localizada.");
                }
            }

            if (string.IsNullOrWhiteSpace(vm.NFDevolucao))
            {
                ModelState.AddModelError("NFDevolucao", "Informe a NFiscal de devolução.");
            }

            if (!ModelState.IsValid)
            {
                ApplyDetailEditableFields(atual, vm);
                PrepareDetailViewModel(atual);
                return View(atual);
            }

            return SaveDetailChanges(vm, atual);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Excluir(int id)
        {
            EnsureDevolucaoComplementoTable();

            try
            {
                using (DbContextTransaction tr = db.Database.BeginTransaction(IsolationLevel.Serializable))
                {
                    bool devolucaoExiste = db.Devolucao
                        .AsNoTracking()
                        .Any(x => x.Id == id && x.FilialId == filialId);

                    if (!devolucaoExiste)
                    {
                        tr.Rollback();
                        return HttpNotFound("Devolu\u00E7\u00E3o n\u00E3o localizada.");
                    }

                    db.Database.ExecuteSqlCommand(
                        "DELETE FROM dbo.DevolucaoComplemento WHERE DevolucaoId = @p0",
                        id);

                    db.Database.ExecuteSqlCommand(
                        "DELETE FROM dbo.DevolucaoItem WHERE DevolucaoId = @p0",
                        id);

                    int devolucoesExcluidas = db.Database.ExecuteSqlCommand(
                        "DELETE FROM dbo.Devolucao WHERE Id = @p0 AND FilialId = @p1",
                        id,
                        filialId);

                    if (devolucoesExcluidas != 1)
                    {
                        tr.Rollback();
                        return HttpNotFound("Devolu\u00E7\u00E3o n\u00E3o localizada.");
                    }

                    tr.Commit();
                }

                TempData["SuccessMessage"] = "Processo de devolu\u00E7\u00E3o exclu\u00EDdo com sucesso.";
                return RedirectToAction("Consulta");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = string.Concat("N\u00E3o foi poss\u00EDvel excluir o processo: ", ex.Message);
                return RedirectToAction("Detalhe", new { id = id });
            }
        }

/*
            int rows = db.Database.ExecuteSqlCommand(
                @"
UPDATE dbo.Devolucao
SET StatusId = @statusId,
    Movimento = @movimento,
    Retirar = @retirar,
    MotivoId = @motivoId,
    TransportadoraId = @transportadoraId,
    NFDevolucao = @nfDevolucao,
    Sinistro = @sinistro,
    PlacaVeiculo = @placaVeiculo,
    Observacao = @observacao,
    ModificadoPor = @modificadoPor,
    ModificadoEm = @modificadoEm
WHERE Id = @id
  AND FilialId = @filialId",
                new SqlParameter("@statusId", vm.StatusId.Value),
                new SqlParameter("@movimento", vm.Movimento ?? string.Empty),
                new SqlParameter("@retirar", vm.Retirar ?? string.Empty),
                new SqlParameter("@motivoId", vm.MotivoId.Value),
                new SqlParameter("@nfDevolucao", (object)(vm.NFDevolucao ?? string.Empty)),
                new SqlParameter("@sinistro", (object)(vm.Sinistro ?? string.Empty)),
                new SqlParameter("@placaVeiculo", (object)(vm.PlacaVeiculo ?? string.Empty)),
                new SqlParameter("@observacao", (object)(vm.Observacao ?? string.Empty)),
                new SqlParameter("@modificadoPor", Util.GetCurrentUser()),
                new SqlParameter("@modificadoEm", Util.GetCurrentDateTime()),
                new SqlParameter("@id", vm.Id),
                new SqlParameter("@filialId", filialId));

            if (rows == 0)
            {
                return HttpNotFound("Devolução não localizada.");
            }

            TempData["SuccessMessage"] = "Alterações gravadas com sucesso.";
            return RedirectToAction("Detalhe", new { id = vm.Id });
        }

*/
        private ActionResult SaveDetailChanges(DevolucaoPrintViewModel vm, DevolucaoPrintViewModel atual)
        {
            string usuarioAtual = Util.GetCurrentUser();
            DateTime agora = Util.GetCurrentDateTime();

            try
            {
                using (DbContextTransaction tr = db.Database.BeginTransaction())
                {
                    int rows = db.Database.ExecuteSqlCommand(
                        @"
UPDATE dbo.Devolucao
SET StatusId = @statusId,
    Movimento = @movimento,
    Retirar = @retirar,
    MotivoId = @motivoId,
    NFDevolucao = @nfDevolucao,
    Sinistro = @sinistro,
    PlacaVeiculo = @placaVeiculo,
    Observacao = @observacao,
    ModificadoPor = @modificadoPor,
    ModificadoEm = @modificadoEm
WHERE Id = @id
  AND FilialId = @filialId",
                        new SqlParameter("@statusId", vm.StatusId.Value),
                        new SqlParameter("@movimento", vm.Movimento ?? string.Empty),
                        new SqlParameter("@retirar", vm.Retirar ?? string.Empty),
                        new SqlParameter("@motivoId", vm.MotivoId.Value),
                        new SqlParameter("@transportadoraId", vm.TransportadoraId.Value),
                        new SqlParameter("@nfDevolucao", (object)(vm.NFDevolucao ?? string.Empty)),
                        new SqlParameter("@sinistro", (object)(vm.Sinistro ?? string.Empty)),
                        new SqlParameter("@placaVeiculo", (object)(vm.PlacaVeiculo ?? string.Empty)),
                        new SqlParameter("@observacao", (object)(vm.Observacao ?? string.Empty)),
                        new SqlParameter("@modificadoPor", usuarioAtual),
                        new SqlParameter("@modificadoEm", agora),
                        new SqlParameter("@id", vm.Id),
                        new SqlParameter("@filialId", filialId));

                    if (rows == 0)
                    {
                        tr.Rollback();
                        return HttpNotFound("Devolução não localizada.");
                    }

                    DevolucaoComplemento complemento = db.DevolucaoComplemento
                        .AsNoTracking()
                        .FirstOrDefault(x => x.DevolucaoId == vm.Id);

                    if (complemento != null && complemento.NotaFiscalId.HasValue)
                    {
                        int notaFiscalStatusId;
                        int notaFiscalItemStatusId;
                        if (TryResolveLinkedNotaFiscalStatusByDevolucaoStatus(vm.StatusId.Value, out notaFiscalStatusId, out notaFiscalItemStatusId))
                        {
                            db.Database.ExecuteSqlCommand(
                                @"
UPDATE dbo.NotaFiscal
SET StatusId = @statusId,
    ModificadoPor = @modificadoPor,
    ModificadoEm = @modificadoEm
WHERE Id = @id
  AND FilialId = @filialId",
                                new SqlParameter("@statusId", notaFiscalStatusId),
                                new SqlParameter("@modificadoPor", usuarioAtual),
                                new SqlParameter("@modificadoEm", agora),
                                new SqlParameter("@id", complemento.NotaFiscalId.Value),
                                new SqlParameter("@filialId", filialId));

                            db.Database.ExecuteSqlCommand(
                                @"
UPDATE dbo.NotaFiscalItem
SET StatusId = @statusId,
    ModificadoPor = @modificadoPor,
    ModificadoEm = @modificadoEm
WHERE NotaFiscalId = @notaFiscalId
  AND FilialId = @filialId",
                                new SqlParameter("@statusId", notaFiscalItemStatusId),
                                new SqlParameter("@modificadoPor", usuarioAtual),
                                new SqlParameter("@modificadoEm", agora),
                                new SqlParameter("@notaFiscalId", complemento.NotaFiscalId.Value),
                                new SqlParameter("@filialId", filialId));
                        }
                    }

                    tr.Commit();
                }

            TempData["SuccessMessage"] = "Alterações gravadas com sucesso.";
            return RedirectToAction("Detalhe", new { id = vm.Id });
            }
            catch (Exception ex)
            {
                ApplyDetailEditableFields(atual, vm);
                PrepareDetailViewModel(atual);
                ModelState.AddModelError(string.Empty, ex.Message);
                return View("Detalhe", atual);
            }
        }

        private bool TryResolveLinkedNotaFiscalStatusByDevolucaoStatus(int devolucaoStatusId, out int notaFiscalStatusId, out int notaFiscalItemStatusId)
        {
            notaFiscalStatusId = 0;
            notaFiscalItemStatusId = 0;

            string statusNome = db.StatusDevolucao
                .AsNoTracking()
                .Where(x => x.Id == devolucaoStatusId)
                .Select(x => x.Nome)
                .FirstOrDefault();

            string statusNormalizado = Util.RemoverAcentuacao(SafeText(statusNome)).ToUpperInvariant();
            if (statusNormalizado == "CANCELADO")
            {
                notaFiscalStatusId = 1003;
                notaFiscalItemStatusId = 4;
                return true;
            }

            if (statusNormalizado == "PENDENTE")
            {
                notaFiscalStatusId = 2;
                notaFiscalItemStatusId = 1;
                return true;
            }

            return false;
        }

        private void PrepareDetailViewModel(DevolucaoPrintViewModel vm)
        {
            if (vm == null)
            {
                return;
            }

            vm.OriginalStatusId = vm.OriginalStatusId ?? vm.StatusId;
            vm.MovimentoDDL = BuildMovimentoDDL(vm.Movimento);
            vm.RetirarDDL = BuildRetirarDDL(vm.Retirar);
            vm.MotivoDDL = BuildMotivoDDL(vm.MotivoId);
            vm.StatusDDL = BuildDetailStatusDDL(vm.StatusId);
            vm.TransportadoraDDL = BuildTransportadoraDDL(vm.TransportadoraId);
        }

        private IEnumerable<SelectListItem> BuildDetailStatusDDL(int? selectedStatusId)
        {
            Dictionary<int, string> allowedStatuses = GetAllowedDetailStatuses();
            bool selectedAllowed = selectedStatusId.HasValue && allowedStatuses.ContainsKey(selectedStatusId.Value);

            List<SelectListItem> status = allowedStatuses
                .OrderBy(x => x.Key)
                .Select(x => new SelectListItem
                {
                    Value = x.Key.ToString(),
                    Text = x.Value,
                    Selected = selectedAllowed && x.Key == selectedStatusId.Value
                })
                .ToList();

            status.Insert(0, new SelectListItem
            {
                Value = string.Empty,
                Text = "-- Selecione --",
                Selected = !selectedAllowed
            });

            return status;
        }

        private Dictionary<int, string> GetAllowedDetailStatuses()
        {
            return db.StatusDevolucao
                .AsNoTracking()
                .OrderBy(x => x.Id)
                .ToList()
                .GroupBy(x => x.Id)
                .Select(x => x.First())
                .Where(x =>
                    string.Equals((x.Nome ?? string.Empty).Trim(), "Pendente", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals((x.Nome ?? string.Empty).Trim(), "Cancelado", StringComparison.OrdinalIgnoreCase))
                .ToDictionary(x => x.Id, x => x.Nome ?? string.Empty);
        }

        private HashSet<int> GetAllowedDetailStatusIds()
        {
            return new HashSet<int>(GetAllowedDetailStatuses().Keys);
        }

        private bool IsMovimentoDetalheValido(string movimento)
        {
            string valor = SafeText(movimento);
            return
                string.Equals(valor, "Devolucao", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(valor, "Devolu\u00E7\u00E3o", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(valor, "Garantia", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(valor, "Troca", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsRetirarDetalheValido(string retirar)
        {
            string valor = SafeText(retirar);
            return
                string.Equals(valor, "Sim", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(valor, "Nao", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(valor, "N\u00E3o", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsMovimentoValido(string movimento)
        {
            string valor = SafeText(movimento);
            return
                string.Equals(valor, "Devolucao", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(valor, "Devolução", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(valor, "Garantia", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(valor, "Troca", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsRetirarValido(string retirar)
        {
            string valor = SafeText(retirar);
            return
                string.Equals(valor, "Sim", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(valor, "Nao", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(valor, "Não", StringComparison.OrdinalIgnoreCase);
        }

        private void ApplyDetailEditableFields(DevolucaoPrintViewModel target, DevolucaoPrintViewModel source)
        {
            if (target == null || source == null)
            {
                return;
            }

            target.StatusId = source.StatusId;
            target.Movimento = source.Movimento;
            target.Retirar = source.Retirar;
            target.MotivoId = source.MotivoId;
            target.Motivo = ResolveMotivoDescricao(source.MotivoId);
            target.TransportadoraId = source.TransportadoraId;
            target.Transportadora = ResolveTransportadoraNome(source.TransportadoraId);
            target.Sinistro = source.Sinistro;
            target.PlacaVeiculo = source.PlacaVeiculo;
            target.NFDevolucao = source.NFDevolucao;
            target.Observacao = source.Observacao;
        }

        private string ResolveMotivoDescricao(int? motivoId)
        {
            if (!motivoId.HasValue)
            {
                return string.Empty;
            }

            return db.MotivoDevolucao
                .AsNoTracking()
                .Where(x => x.Id == motivoId.Value)
                .Select(x => x.Motivo)
                .FirstOrDefault() ?? string.Empty;
        }

        private string ResolveTransportadoraNome(int? transportadoraId)
        {
            if (!transportadoraId.HasValue)
            {
                return string.Empty;
            }

            return db.Transportadora
                .AsNoTracking()
                .Where(x => x.Id == transportadoraId.Value && (!x.FilialId.HasValue || x.FilialId.Value == filialId))
                .OrderByDescending(x => x.FilialId == filialId)
                .Select(x => x.Nome)
                .FirstOrDefault() ?? string.Empty;
        }

        [HttpGet]
        public ActionResult GetNotaFiscalVenda(string numero)
        {
            try
            {
                string numeroNormalizado = NormalizeNotaFiscalNr(numero);
                DocExpedicao documento = FindDocExpedicao(!string.IsNullOrWhiteSpace(numeroNormalizado) ? numeroNormalizado : numero);
                if (documento == null)
                {
                    return Json(new { success = false, msg = "Nota Fiscal de venda não localizada." }, JsonRequestBehavior.AllowGet);
                }

                string vendedor = documento.Vendedor ?? string.Empty;

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        DocExpedicaoId = documento.Id,
                        Numero = !string.IsNullOrWhiteSpace(numeroNormalizado) ? numeroNormalizado : documento.Numero,
                        Cliente = documento.NomeCliente ?? string.Empty,
                        DataVenda = documento.DataEmissao.HasValue ? documento.DataEmissao.Value.ToString("dd/MM/yyyy") : string.Empty,
                        Vendedor = vendedor,
                        RequerVendedorManual = string.IsNullOrWhiteSpace(vendedor)
                    }
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, msg = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public ActionResult GetMaterial(string codigo)
        {
            try
            {
                string codigoNormalizado = NormalizeCodigoMaterial(codigo);
                Material material = db.Material
                    .AsNoTracking()
                    .FirstOrDefault(x => x.Codigo == codigoNormalizado && x.FilialId == filialId);

                if (material == null)
                {
                    material = db.Material
                        .AsNoTracking()
                        .FirstOrDefault(x => x.Codigo == codigoNormalizado && x.FilialId == null);
                }

                if (material == null)
                {
                    return Json(new { success = false, msg = "Material não localizado." }, JsonRequestBehavior.AllowGet);
                }

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        Codigo = material.Codigo,
                        Descricao = material.Descricao ?? string.Empty
                    }
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, msg = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        private DevolucaoCadastroViewModel CreateDefaultViewModel()
        {
            DevolucaoCadastroViewModel vm = new DevolucaoCadastroViewModel
            {
                Movimento = "Devolu\u00E7\u00E3o",
                Retirar = "Sim",
                StatusId = ResolveStatusDevolucaoInicial(),
                Itens = new List<DevolucaoCadastroItemViewModel>()
            };

            return PrepareViewModel(vm);
        }

        private DevolucaoCadastroViewModel PrepareViewModel(DevolucaoCadastroViewModel vm)
        {
            vm = vm ?? new DevolucaoCadastroViewModel();
            vm.Itens = vm.Itens ?? new List<DevolucaoCadastroItemViewModel>();
            vm.StatusId = vm.StatusId ?? ResolveStatusDevolucaoInicial();
            vm.MovimentoDDL = BuildMovimentoDDL(vm.Movimento);
            vm.RetirarDDL = BuildRetirarDDL(vm.Retirar);
            vm.StatusDDL = BuildStatusDDL(vm.StatusId);
            vm.MotivoDDL = BuildMotivoDDL(vm.MotivoId);
            vm.TransportadoraDDL = BuildTransportadoraDDL(vm.TransportadoraId);
            vm.ItensJson = string.IsNullOrWhiteSpace(vm.ItensJson) ? SerializeItens(vm.Itens) : vm.ItensJson;
            return vm;
        }

        private IEnumerable<SelectListItem> BuildMovimentoDDL(string selectedValue)
        {
            return new[]
            {
                new SelectListItem
                {
                    Value = "Devolu\u00E7\u00E3o",
                    Text = "Devolu\u00E7\u00E3o",
                    Selected = string.Equals(selectedValue, "Devolu\u00E7\u00E3o", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(selectedValue, "Devolucao", StringComparison.OrdinalIgnoreCase)
                },
                new SelectListItem { Value = "Garantia", Text = "Garantia", Selected = string.Equals(selectedValue, "Garantia", StringComparison.OrdinalIgnoreCase) },
                new SelectListItem { Value = "Troca", Text = "Troca", Selected = string.Equals(selectedValue, "Troca", StringComparison.OrdinalIgnoreCase) }
            };
        }

        private IEnumerable<SelectListItem> BuildRetirarDDL(string selectedValue)
        {
            return new[]
            {
                new SelectListItem
                {
                    Value = "Sim",
                    Text = "Sim",
                    Selected = string.Equals(selectedValue, "Sim", StringComparison.OrdinalIgnoreCase)
                        || string.IsNullOrWhiteSpace(selectedValue)
                },
                new SelectListItem
                {
                    Value = "N\u00E3o",
                    Text = "N\u00E3o",
                    Selected = string.Equals(selectedValue, "N\u00E3o", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(selectedValue, "Nao", StringComparison.OrdinalIgnoreCase)
                },
            };
        }

        private IEnumerable<SelectListItem> BuildStatusDDL(int? statusId)
        {
            List<SelectListItem> status = db.StatusDevolucao
                .AsNoTracking()
                .OrderBy(x => x.Id)
                .ToList()
                .GroupBy(x => x.Id)
                .Select(x => x.First())
                .Select(x => new SelectListItem
                {
                    Value = x.Id.ToString(),
                    Text = x.Nome,
                    Selected = statusId.HasValue && x.Id == statusId.Value
                })
                .ToList();

            status.Insert(0, new SelectListItem
            {
                Value = string.Empty,
                Text = "-- Selecione --",
                Selected = !statusId.HasValue
            });

            return status;
        }

        private IEnumerable<SelectListItem> BuildMotivoDDL(int? motivoId)
        {
            List<SelectListItem> motivos = db.MotivoDevolucao
                .AsNoTracking()
                .OrderBy(x => x.Motivo)
                .ToList()
                .Select(x => new SelectListItem
                {
                    Value = x.Id.ToString(),
                    Text = x.Motivo,
                    Selected = motivoId.HasValue && x.Id == motivoId.Value
                })
                .ToList();

            motivos.Insert(0, new SelectListItem { Value = string.Empty, Text = "-- Selecione --", Selected = !motivoId.HasValue });
            return motivos;
        }

        private IEnumerable<SelectListItem> BuildTransportadoraDDL(int? transportadoraId)
        {
            List<SelectListItem> transportadoras = db.Transportadora
                .AsNoTracking()
                .Where(x => x.FilialId == filialId || x.FilialId == null)
                .ToList()
                .GroupBy(x => x.Id)
                .Select(x => x.OrderByDescending(y => y.FilialId == filialId).First())
                .OrderBy(x => x.Nome)
                .Select(x => new SelectListItem
                {
                    Value = x.Id.ToString(),
                    Text = x.Nome,
                    Selected = transportadoraId.HasValue && x.Id == transportadoraId.Value
                })
                .ToList();

            transportadoras.Insert(0, new SelectListItem
            {
                Value = string.Empty,
                Text = "-- Selecione --",
                Selected = !transportadoraId.HasValue
            });

            return transportadoras;
        }

        private void ValidateViewModel(
            DevolucaoCadastroViewModel vm,
            List<DevolucaoCadastroItemViewModel> itens,
            DocExpedicao documentoVenda)
        {
            if (string.IsNullOrWhiteSpace(vm.Movimento))
            {
                ModelState.AddModelError("Movimento", "Informe o tipo de movimento.");
            }

            if (string.IsNullOrWhiteSpace(vm.Retirar))
            {
                ModelState.AddModelError("Retirar", "Informe se deve efetuar retirada.");
            }

            if (!vm.MotivoId.HasValue)
            {
                ModelState.AddModelError("MotivoId", "Informe o motivo.");
            }
            else
            {
                bool motivoExiste = db.MotivoDevolucao
                    .AsNoTracking()
                    .Any(x => x.Id == vm.MotivoId.Value);

                if (!motivoExiste)
                {
                    ModelState.AddModelError("MotivoId", "Motivo informado não localizado.");
                }
            }

            if (!vm.StatusId.HasValue)
            {
                ModelState.AddModelError("StatusId", "Informe o status.");
            }

            if (string.IsNullOrWhiteSpace(vm.NFVenda))
            {
                ModelState.AddModelError("NFVenda", "Informe a Nota Fiscal de venda.");
            }
            if (string.IsNullOrWhiteSpace(vm.Cliente))
            {
                ModelState.AddModelError("Cliente", "Informe o cliente.");
            }

            if (string.IsNullOrWhiteSpace(vm.DataVenda))
            {
                ModelState.AddModelError("DataVenda", "Informe a data da venda.");
            }

            if (string.IsNullOrWhiteSpace(vm.NFDevolucao))
            {
                ModelState.AddModelError("NFDevolucao", "Informe a NFiscal de devolução.");
            }

            if (!vm.TransportadoraId.HasValue)
            {
                ModelState.AddModelError("TransportadoraId", "Informe a transportadora.");
            }
            else
            {
                bool transportadoraExiste = db.Transportadora
                    .AsNoTracking()
                    .Any(x => x.Id == vm.TransportadoraId.Value && (!x.FilialId.HasValue || x.FilialId.Value == filialId));

                if (!transportadoraExiste)
                {
                    ModelState.AddModelError("TransportadoraId", "Transportadora informada não localizada.");
                }
            }

            if (itens == null || itens.Count == 0)
            {
                ModelState.AddModelError(string.Empty, "Adicione pelo menos um item.");
            }

            if (itens != null)
            {
                for (int i = 0; i < itens.Count; i++)
                {
                    DevolucaoCadastroItemViewModel item = itens[i];
                    if (string.IsNullOrWhiteSpace(item.ItemNr))
                    {
                        ModelState.AddModelError(string.Empty, string.Concat("Item ", i + 1, ": código não informado."));
                    }

                    if (item.Quantidade <= 0)
                    {
                        ModelState.AddModelError(string.Empty, string.Concat("Item ", i + 1, ": quantidade deve ser maior que zero."));
                    }

                    if (item.ValorUnitario <= 0)
                    {
                        ModelState.AddModelError(string.Empty, string.Concat("Item ", i + 1, ": valor unitário deve ser maior que zero."));
                    }
                }
            }

            string vendedorFinal = ResolveVendedor(vm, documentoVenda);
            if (string.IsNullOrWhiteSpace(vendedorFinal))
            {
                ModelState.AddModelError("Vendedor", "Informe o vendedor.");
            }
        }

        private string ResolveVendedor(DevolucaoCadastroViewModel vm, DocExpedicao documentoVenda)
        {
            if (documentoVenda != null && !string.IsNullOrWhiteSpace(documentoVenda.Vendedor))
            {
                return documentoVenda.Vendedor.Trim();
            }

            return (vm.Vendedor ?? string.Empty).Trim();
        }

        private void PopulateDocExpedicaoFields(DevolucaoCadastroViewModel vm, DocExpedicao documentoVenda)
        {
            if (vm == null)
            {
                return;
            }

            if (documentoVenda == null)
            {
                vm.VendedorBloqueado = false;
                return;
            }

            vm.Cliente = documentoVenda.NomeCliente ?? string.Empty;
            vm.DataVenda = documentoVenda.DataEmissao.HasValue
                ? documentoVenda.DataEmissao.Value.ToString("dd/MM/yyyy")
                : string.Empty;

            if (!string.IsNullOrWhiteSpace(documentoVenda.Vendedor))
            {
                vm.Vendedor = documentoVenda.Vendedor.Trim();
                vm.VendedorBloqueado = true;
            }
            else
            {
                vm.VendedorBloqueado = false;
            }
        }

        private DocExpedicao FindDocExpedicao(string numero)
        {
            string numeroInformado = (numero ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(numeroInformado))
            {
                return null;
            }

            string numeroNormalizado = NormalizeNotaFiscalNr(numeroInformado);

            return db.DocExpedicao
                .AsNoTracking()
                .Where(x => x.FilialId == filialId && (x.Numero == numeroInformado || x.Numero == numeroNormalizado))
                .OrderByDescending(x => x.DataEmissao)
                .FirstOrDefault();
        }

        private string NormalizeNotaFiscalNr(string numero)
        {
            string digits = new string((numero ?? string.Empty)
                .TakeWhile(ch => ch != '-')
                .Where(char.IsDigit)
                .ToArray());

            if (string.IsNullOrWhiteSpace(digits))
            {
                return string.Empty;
            }

            if (digits.Length > 9)
            {
                digits = digits.Substring(digits.Length - 9, 9);
            }

            return digits.PadLeft(9, '0');
        }

        private string NormalizeCodigoMaterial(string codigo)
        {
            return (codigo ?? string.Empty).Trim().ToUpperInvariant();
        }

        private List<DevolucaoCadastroItemViewModel> DeserializeItens(string itensJson)
        {
            if (string.IsNullOrWhiteSpace(itensJson))
            {
                return new List<DevolucaoCadastroItemViewModel>();
            }

            try
            {
                List<DevolucaoCadastroItemViewModel> itens = serializer.Deserialize<List<DevolucaoCadastroItemViewModel>>(itensJson);
                return itens ?? new List<DevolucaoCadastroItemViewModel>();
            }
            catch
            {
                return new List<DevolucaoCadastroItemViewModel>();
            }
        }

        private string SerializeItens(List<DevolucaoCadastroItemViewModel> itens)
        {
            return serializer.Serialize(itens ?? new List<DevolucaoCadastroItemViewModel>());
        }

        private void PopulateLastSavedDevolucao(DevolucaoCadastroViewModel vm, int devolucaoId)
        {
            if (vm == null)
            {
                return;
            }

            var devolucao = db.Devolucao
                .AsNoTracking()
                .Where(x => x.Id == devolucaoId && x.FilialId == filialId)
                .Select(x => new
                {
                    x.Id,
                    x.DevolucaoNr
                })
                .FirstOrDefault();

            if (devolucao == null)
            {
                return;
            }

            vm.UltimaDevolucaoId = devolucao.Id;
            vm.UltimoControleNr = devolucao.DevolucaoNr;
        }

        private DevolucaoPrintViewModel BuildPrintViewModel(int devolucaoId)
        {
            const string headerSql = @"
SELECT TOP 1
    d.Id,
    d.DevolucaoNr AS ControleNr,
    d.StatusId,
    d.Movimento,
    d.Retirar,
    d.MotivoId,
    ISNULL(m.Motivo, '') AS Motivo,
    d.NFVenda,
    d.Cliente,
    dc.DataVenda,
    d.Vendedor,
    d.TransportadoraId,
    ISNULL(trp.Nome, '') AS Transportadora,
    d.NFDevolucao,
    d.Sinistro,
    d.PlacaVeiculo,
    d.Observacao,
    ISNULL(s.Nome, '') AS StatusNome,
    ISNULL(e.Nome, '') AS FilialNome,
    d.CriadoPor AS UsuarioCadastro,
    d.CriadoEm AS DataCadastro
FROM dbo.Devolucao d
LEFT JOIN dbo.MotivoDevolucao m
    ON m.Id = d.MotivoId
LEFT JOIN dbo.StatusDevolucao s
    ON s.Id = d.StatusId
LEFT JOIN dbo.DevolucaoComplemento dc
    ON dc.DevolucaoId = d.Id
LEFT JOIN dbo.Empresa e
    ON e.Id = d.FilialId
OUTER APPLY
(
    SELECT TOP 1 t.Nome
    FROM dbo.Transportadora t
    WHERE t.Id = d.TransportadoraId
      AND (t.FilialId = @filialId OR t.FilialId IS NULL)
    ORDER BY CASE WHEN t.FilialId = @filialId THEN 0 ELSE 1 END
) trp
WHERE d.Id = @id
  AND d.FilialId = @filialId";

            DevolucaoPrintViewModel vm = db.Database.SqlQuery<DevolucaoPrintViewModel>(
                headerSql,
                new SqlParameter("@id", devolucaoId),
                new SqlParameter("@filialId", filialId))
                .FirstOrDefault();

            if (vm == null)
            {
                return null;
            }

            vm.OriginalStatusId = vm.StatusId;

            string itensSql = string.Format(
                CultureInfo.InvariantCulture,
                @"
SELECT
    di.ItemNr,
    ISNULL(mat.Descricao, '') AS Descricao,
    ISNULL(di.Quantidade, 0) AS Quantidade,
    ISNULL(sd.Nome, '') AS StatusNome,
    ISNULL(di.QtdeOcorrencia, 0) AS QtdeOcorrencia,
    ISNULL(oc.Nome, '') AS OcorrenciaNome,
    CAST(ISNULL(di.[{0}], 0) AS DECIMAL(18, 2)) AS ValorUnitario,
    di.Observacao
FROM dbo.DevolucaoItem di
LEFT JOIN dbo.StatusDevolucao sd
    ON sd.Id = di.StatusId
LEFT JOIN dbo.Ocorrencia oc
    ON oc.Id = di.OcorrenciaId
OUTER APPLY
(
    SELECT TOP 1 m.Descricao
    FROM dbo.Material m
    WHERE m.Codigo = di.ItemNr
      AND (m.FilialId = @filialId OR m.FilialId IS NULL)
    ORDER BY CASE WHEN m.FilialId = @filialId THEN 0 ELSE 1 END
) mat
WHERE di.DevolucaoId = @id
ORDER BY di.Id;",
                ValorUnitarioColumnName);

            vm.Itens = db.Database.SqlQuery<DevolucaoPrintItemViewModel>(
                itensSql,
                new SqlParameter("@id", devolucaoId),
                new SqlParameter("@filialId", filialId))
                .ToList();

            return vm;
        }

        private List<DevolucaoOcorrenciaConsultaViewModel> BuildDevolucaoOcorrenciaConsulta(int statusOcorrenciaId)
        {
            List<Devolucao> devolucoes = db.Devolucao
                .AsNoTracking()
                .Where(x => x.FilialId == filialId && x.StatusId == statusOcorrenciaId)
                .OrderByDescending(x => x.ModificadoEm ?? x.CriadoEm)
                .ThenByDescending(x => x.Id)
                .ToList();

            List<int> devolucaoIds = devolucoes.Select(x => x.Id).ToList();
            if (devolucaoIds.Count == 0)
            {
                return new List<DevolucaoOcorrenciaConsultaViewModel>();
            }

            List<DevolucaoComplemento> complementos = db.DevolucaoComplemento
                .AsNoTracking()
                .Where(x => devolucaoIds.Contains(x.DevolucaoId))
                .ToList();

            Dictionary<int, DevolucaoComplemento> complementoLookup = complementos
                .GroupBy(x => x.DevolucaoId)
                .ToDictionary(x => x.Key, x => x.OrderByDescending(y => y.NotaFiscalId ?? 0).First());

            List<int> notaFiscalIds = complementos
                .Where(x => x.NotaFiscalId.HasValue)
                .Select(x => x.NotaFiscalId.Value)
                .Distinct()
                .ToList();

            Dictionary<int, NotaFiscal> notaFiscalLookup = db.NotaFiscal
                .AsNoTracking()
                .Where(x => notaFiscalIds.Contains(x.Id) && x.FilialId == filialId)
                .ToList()
                .ToDictionary(x => x.Id, x => x);

            Dictionary<int, Tuple<int, int>> itensLookup = db.DevolucaoItem
                .AsNoTracking()
                .Where(x => devolucaoIds.Contains(x.DevolucaoId))
                .ToList()
                .GroupBy(x => x.DevolucaoId)
                .ToDictionary(
                    x => x.Key,
                    x => Tuple.Create(
                        x.Count(),
                        x.Sum(y => y.Quantidade ?? 0)));

            return devolucoes.Select(x =>
            {
                DevolucaoComplemento complemento;
                NotaFiscal notaFiscal = null;
                if (complementoLookup.TryGetValue(x.Id, out complemento) && complemento.NotaFiscalId.HasValue)
                {
                    notaFiscalLookup.TryGetValue(complemento.NotaFiscalId.Value, out notaFiscal);
                }

                Tuple<int, int> resumoItens;
                bool possuiResumo = itensLookup.TryGetValue(x.Id, out resumoItens);

                return new DevolucaoOcorrenciaConsultaViewModel
                {
                    Id = x.Id,
                    ControleNr = x.DevolucaoNr ?? string.Empty,
                    NotaFiscalNr = notaFiscal == null ? (x.NFDevolucao ?? string.Empty) : (notaFiscal.Numero ?? string.Empty),
                    Emissor = notaFiscal == null ? (x.Cliente ?? string.Empty) : (notaFiscal.Emissor ?? string.Empty),
                    QuantidadeLinhas = possuiResumo ? resumoItens.Item1 : 0,
                    QuantidadePecas = possuiResumo ? resumoItens.Item2 : 0,
                    UltimaAtualizacao = x.ModificadoEm ?? x.CriadoEm
                };
            }).ToList();
        }

        private DevolucaoOcorrenciaViewModel BuildDevolucaoOcorrenciaViewModel(int devolucaoId)
        {
            Devolucao devolucao = db.Devolucao
                .AsNoTracking()
                .FirstOrDefault(x => x.Id == devolucaoId && x.FilialId == filialId);

            if (devolucao == null)
            {
                return null;
            }

            DevolucaoComplemento complemento = db.DevolucaoComplemento
                .AsNoTracking()
                .FirstOrDefault(x => x.DevolucaoId == devolucaoId);

            NotaFiscal notaFiscal = null;
            if (complemento != null && complemento.NotaFiscalId.HasValue)
            {
                notaFiscal = db.NotaFiscal
                    .AsNoTracking()
                    .FirstOrDefault(x => x.Id == complemento.NotaFiscalId.Value && x.FilialId == filialId);
            }

            int statusOcorrenciaId = ResolveStatusDevolucaoIdByName("Ocorrencia") ?? 3;
            int statusCorrigidaId = ResolveStatusDevolucaoIdByName("Corrigida") ?? 5;
            int statusFinalizadoId = ResolveStatusDevolucaoIdByName("Finalizado") ?? 2;

            Dictionary<int, string> statusLookup = BuildStatusDevolucaoLookup();

            List<DevolucaoItem> devolucaoItens = db.DevolucaoItem
                .AsNoTracking()
                .Where(x => x.DevolucaoId == devolucaoId)
                .OrderBy(x => x.Id)
                .ToList();

            List<string> itemCodes = devolucaoItens
                .Select(x => NormalizeCodigoMaterial(x.ItemNr))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();

            Dictionary<string, string> descricaoLookup = db.Material
                .AsNoTracking()
                .Where(x => itemCodes.Contains(x.Codigo) && (x.FilialId == filialId || x.FilialId == null))
                .ToList()
                .GroupBy(x => NormalizeCodigoMaterial(x.Codigo))
                .ToDictionary(
                    x => x.Key,
                    x => x.OrderByDescending(y => y.FilialId == filialId ? 1 : 0)
                        .Select(y => y.Descricao ?? string.Empty)
                        .FirstOrDefault() ?? string.Empty);

            List<NotaFiscalItem> notaFiscalItens = new List<NotaFiscalItem>();
            if (notaFiscal != null)
            {
                notaFiscalItens = db.NotaFiscalItem
                    .AsNoTracking()
                    .Where(x => x.NotaFiscalId == notaFiscal.Id && x.FilialId == filialId)
                    .OrderBy(x => x.Id)
                    .ToList();
            }

            Dictionary<int, int> notaFiscalItemMap = BuildDevolucaoNotaFiscalItemMap(devolucaoItens, notaFiscalItens);

            return new DevolucaoOcorrenciaViewModel
            {
                Id = devolucao.Id,
                NotaFiscalId = complemento == null ? (int?)null : complemento.NotaFiscalId,
                StatusOcorrenciaId = statusOcorrenciaId,
                StatusCorrigidaId = statusCorrigidaId,
                StatusFinalizadoId = statusFinalizadoId,
                ControleNr = devolucao.DevolucaoNr ?? string.Empty,
                NotaFiscalNr = notaFiscal == null ? (devolucao.NFDevolucao ?? string.Empty) : (notaFiscal.Numero ?? string.Empty),
                Emissor = notaFiscal == null ? (devolucao.Cliente ?? string.Empty) : (notaFiscal.Emissor ?? string.Empty),
                Cliente = devolucao.Cliente ?? string.Empty,
                Vendedor = devolucao.Vendedor ?? string.Empty,
                Motivo = ResolveMotivoDescricao(devolucao.MotivoId),
                NFDevolucao = devolucao.NFDevolucao ?? string.Empty,
                Sinistro = devolucao.Sinistro ?? string.Empty,
                PlacaVeiculo = devolucao.PlacaVeiculo ?? string.Empty,
                Observacao = devolucao.Observacao ?? string.Empty,
                StatusNome = devolucao.StatusId.HasValue && statusLookup.ContainsKey(devolucao.StatusId.Value)
                    ? statusLookup[devolucao.StatusId.Value]
                    : string.Empty,
                FilialNome = db.Empresa
                    .AsNoTracking()
                    .Where(x => x.Id == filialId)
                    .Select(x => x.Nome)
                    .FirstOrDefault() ?? string.Empty,
                DataVenda = complemento == null ? (DateTime?)null : complemento.DataVenda,
                DataCadastro = devolucao.CriadoEm,
                UltimaAtualizacao = devolucao.ModificadoEm ?? devolucao.CriadoEm,
                StatusTratamentoDDL = BuildOcorrenciaTratamentoDDL(statusCorrigidaId, statusFinalizadoId),
                Itens = devolucaoItens.Select(x =>
                {
                    string codigo = NormalizeCodigoMaterial(x.ItemNr);
                    int notaFiscalItemId;
                    bool permiteTratamento = (x.StatusId ?? 0) == statusOcorrenciaId && (x.QtdeOcorrencia ?? 0) > 0;

                    return new DevolucaoOcorrenciaItemViewModel
                    {
                        DevolucaoItemId = x.Id,
                        NotaFiscalItemId = notaFiscalItemMap.TryGetValue(x.Id, out notaFiscalItemId) ? (int?)notaFiscalItemId : null,
                        ItemNr = x.ItemNr,
                        Descricao = descricaoLookup.ContainsKey(codigo) ? descricaoLookup[codigo] : string.Empty,
                        QuantidadeOriginal = x.Quantidade ?? 0,
                        QuantidadeOcorrencia = x.QtdeOcorrencia ?? 0,
                        StatusId = x.StatusId,
                        StatusNome = x.StatusId.HasValue && statusLookup.ContainsKey(x.StatusId.Value)
                            ? statusLookup[x.StatusId.Value]
                            : string.Empty,
                        Observacao = x.Observacao ?? string.Empty,
                        PermiteTratamento = permiteTratamento,
                        QuantidadeTratada = permiteTratamento ? (int?)(x.QtdeOcorrencia ?? 0) : null,
                        ObservacaoTratamento = x.Observacao ?? string.Empty
                    };
                }).ToList()
            };
        }

        private IEnumerable<SelectListItem> BuildOcorrenciaTratamentoDDL(int statusCorrigidaId, int statusFinalizadoId)
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Value = string.Empty, Text = "-- Selecione --" },
                new SelectListItem { Value = statusCorrigidaId.ToString(), Text = "Corrigida" },
                new SelectListItem { Value = statusFinalizadoId.ToString(), Text = "Finalizado" }
            };
        }

        private void MergeDevolucaoOcorrenciaInput(DevolucaoOcorrenciaViewModel target, DevolucaoOcorrenciaViewModel source)
        {
            if (target == null || source == null)
            {
                return;
            }

            Dictionary<int, DevolucaoOcorrenciaItemViewModel> sourceLookup = (source.Itens ?? new List<DevolucaoOcorrenciaItemViewModel>())
                .ToDictionary(x => x.DevolucaoItemId, x => x);

            foreach (DevolucaoOcorrenciaItemViewModel item in target.Itens ?? new List<DevolucaoOcorrenciaItemViewModel>())
            {
                DevolucaoOcorrenciaItemViewModel sourceItem;
                if (!sourceLookup.TryGetValue(item.DevolucaoItemId, out sourceItem))
                {
                    continue;
                }

                item.NovoStatusId = sourceItem.NovoStatusId;
                item.QuantidadeTratada = sourceItem.QuantidadeTratada;
                item.ObservacaoTratamento = sourceItem.ObservacaoTratamento;
            }
        }

        private Dictionary<int, int> BuildDevolucaoNotaFiscalItemMap(IEnumerable<DevolucaoItem> devolucaoItens, IEnumerable<NotaFiscalItem> notaFiscalItens)
        {
            Dictionary<int, int> map = new Dictionary<int, int>();

            Dictionary<string, List<DevolucaoItem>> devolucaoByCode = (devolucaoItens ?? Enumerable.Empty<DevolucaoItem>())
                .GroupBy(x => NormalizeCodigoMaterial(x.ItemNr))
                .ToDictionary(x => x.Key, x => x.OrderBy(y => y.Id).ToList());

            Dictionary<string, List<NotaFiscalItem>> notaFiscalByCode = (notaFiscalItens ?? Enumerable.Empty<NotaFiscalItem>())
                .GroupBy(x => NormalizeCodigoMaterial(x.Item))
                .ToDictionary(x => x.Key, x => x.OrderBy(y => y.Id).ToList());

            foreach (KeyValuePair<string, List<DevolucaoItem>> group in devolucaoByCode)
            {
                List<NotaFiscalItem> notaFiscalGrupo;
                if (!notaFiscalByCode.TryGetValue(group.Key, out notaFiscalGrupo))
                {
                    continue;
                }

                List<DevolucaoItem> devolucaoGrupo = group.Value;
                int limite = Math.Min(devolucaoGrupo.Count, notaFiscalGrupo.Count);
                for (int i = 0; i < limite; i++)
                {
                    map[devolucaoGrupo[i].Id] = notaFiscalGrupo[i].Id;
                }
            }

            return map;
        }

        private Dictionary<int, string> BuildStatusDevolucaoLookup()
        {
            return db.StatusDevolucao
                .AsNoTracking()
                .OrderBy(x => x.Id)
                .ToList()
                .GroupBy(x => x.Id)
                .ToDictionary(x => x.Key, x => x.Select(y => y.Nome ?? string.Empty).FirstOrDefault() ?? string.Empty);
        }

        private int? ResolveStatusDevolucaoIdByName(string statusName)
        {
            string statusNormalizado = NormalizeStatusDevolucaoName(statusName);
            return db.StatusDevolucao
                .AsNoTracking()
                .OrderBy(x => x.Id)
                .ToList()
                .Where(x => NormalizeStatusDevolucaoName(x.Nome) == statusNormalizado)
                .Select(x => (int?)x.Id)
                .FirstOrDefault();
        }

        private string NormalizeStatusDevolucaoName(string value)
        {
            return Util.RemoverAcentuacao((value ?? string.Empty).Trim()).ToUpperInvariant();
        }

        private byte[] BuildAutorizacaoDevolucaoPdf(DevolucaoPrintViewModel vm)
        {
            Document doc = new Document();
            doc.Info.Title = ResolveTituloAutorizacao(vm.Movimento);

            Style normalStyle = doc.Styles["Normal"];
            normalStyle.Font.Name = "Arial";
            normalStyle.Font.Size = 10;

            Section section = doc.AddSection();
            section.PageSetup.PageFormat = PageFormat.A4;
            section.PageSetup.Orientation = Orientation.Portrait;
            section.PageSetup.TopMargin = Unit.FromCentimeter(0.8);
            section.PageSetup.BottomMargin = Unit.FromCentimeter(0.8);
            section.PageSetup.LeftMargin = Unit.FromCentimeter(0.8);
            section.PageSetup.RightMargin = Unit.FromCentimeter(0.8);

            DateTime geradoEm = Util.GetCurrentDateTime();
            int totalItens = vm.Itens == null ? 0 : vm.Itens.Count;
            int totalQuantidade = vm.Itens == null ? 0 : vm.Itens.Sum(x => x.Quantidade);
            decimal valorTotal = vm.Itens == null ? 0M : vm.Itens.Sum(x => x.Quantidade * x.ValorUnitario);

            AddHeader(section, vm, geradoEm);
            AddResumo(section, vm);
            AddItens(section, vm.Itens ?? new List<DevolucaoPrintItemViewModel>());

            Table totalTable = section.AddTable();
            totalTable.Borders.Visible = false;
            totalTable.Rows.LeftIndent = 0;
            totalTable.AddColumn(Unit.FromCentimeter(2.8));
            totalTable.AddColumn(Unit.FromCentimeter(6.8));
            totalTable.AddColumn(Unit.FromCentimeter(1.8));
            totalTable.AddColumn(Unit.FromCentimeter(2.5));
            totalTable.AddColumn(Unit.FromCentimeter(1.8));
            totalTable.AddColumn(Unit.FromCentimeter(3.7));

            Row totalRow = totalTable.AddRow();
            totalRow.TopPadding = Unit.FromPoint(8);
            totalRow.BottomPadding = Unit.FromPoint(2);
            totalRow.Cells[0].MergeRight = 1;

            Paragraph totalItensParagraph = totalRow.Cells[0].AddParagraph(string.Concat(totalItens.ToString("N0", PtBrCulture), " Item(s)"));
            totalItensParagraph.Format.Font.Bold = true;

            Paragraph totalQuantidadeParagraph = totalRow.Cells[2].AddParagraph(totalQuantidade.ToString("N0", PtBrCulture));
            totalQuantidadeParagraph.Format.Alignment = ParagraphAlignment.Center;
            totalQuantidadeParagraph.Format.Font.Bold = true;

            Paragraph totalValorParagraph = totalRow.Cells[3].AddParagraph(string.Concat("R$", valorTotal.ToString("N2", PtBrCulture)));
            totalValorParagraph.Format.Alignment = ParagraphAlignment.Right;
            totalValorParagraph.Format.Font.Bold = true;

            if (!string.IsNullOrWhiteSpace(vm.Observacao))
            {
                Paragraph observacaoParagraph = section.AddParagraph();
                observacaoParagraph.Format.SpaceBefore = Unit.FromCentimeter(0.4);
                observacaoParagraph.AddFormattedText("Observa\u00E7\u00E3o ", TextFormat.Bold);
                observacaoParagraph.AddText(SafeText(vm.Observacao));
            }

            PdfDocumentRenderer renderer = new PdfDocumentRenderer
            {
                Document = doc
            };

            renderer.RenderDocument();
            TryDrawHeaderAssets(renderer.PdfDocument, vm.ControleNr);

            using (MemoryStream stream = new MemoryStream())
            {
                renderer.PdfDocument.Save(stream, false);
                return stream.ToArray();
            }
        }

        private void AddHeader(Section section, DevolucaoPrintViewModel vm, DateTime geradoEm)
        {
            Table headerTable = section.AddTable();
            headerTable.Borders.Visible = false;
            headerTable.AddColumn(Unit.FromCentimeter(3.8));
            headerTable.AddColumn(Unit.FromCentimeter(10.8));
            headerTable.AddColumn(Unit.FromCentimeter(4.8));

            Row row = headerTable.AddRow();
            row.HeightRule = RowHeightRule.Exactly;
            row.Height = Unit.FromCentimeter(3.6);

            Paragraph title = row.Cells[1].AddParagraph(ResolveTituloAutorizacao(vm.Movimento));
            title.Format.Alignment = ParagraphAlignment.Center;
            title.Format.Font.Size = 22;
            title.Format.Font.Bold = true;
            title.Format.SpaceAfter = Unit.FromPoint(1);

            Paragraph date = row.Cells[1].AddParagraph(geradoEm.ToString("dd/MM/yyyy HH:mm:ss", PtBrCulture));
            date.Format.Alignment = ParagraphAlignment.Center;
            date.Format.Font.Size = 11;
            date.Format.Font.Bold = true;
            date.Format.SpaceAfter = Unit.FromPoint(24);

            Paragraph transportadora = row.Cells[1].AddParagraph(SafeText(vm.Transportadora));
            transportadora.Format.Alignment = ParagraphAlignment.Center;
            transportadora.Format.Font.Size = 15;
            transportadora.Format.Font.Bold = true;
            transportadora.Format.SpaceAfter = 0;
        }

        private void AddResumo(Section section, DevolucaoPrintViewModel vm)
        {
            section.AddParagraph().Format.SpaceAfter = Unit.FromPoint(2);

            Table firstRow = section.AddTable();
            firstRow.Borders.Visible = false;
            firstRow.AddColumn(Unit.FromCentimeter(10.0));
            firstRow.AddColumn(Unit.FromCentimeter(9.4));

            Row row1 = firstRow.AddRow();
            row1.TopPadding = Unit.FromPoint(2);
            row1.BottomPadding = Unit.FromPoint(2);
            AddLabelValueParagraph(row1.Cells[0], "Cliente", vm.Cliente);
            AddLabelValueParagraph(row1.Cells[1], "Vendedor", vm.Vendedor);

            Table secondRow = section.AddTable();
            secondRow.Borders.Visible = false;
            secondRow.AddColumn(Unit.FromCentimeter(7.0));
            secondRow.AddColumn(Unit.FromCentimeter(6.2));
            secondRow.AddColumn(Unit.FromCentimeter(0.5));
            secondRow.AddColumn(Unit.FromCentimeter(5.7));

            Row row2 = secondRow.AddRow();
            row2.TopPadding = Unit.FromPoint(2);
            row2.BottomPadding = Unit.FromPoint(2);
            AddLabelValueParagraph(row2.Cells[0], "Motivo", vm.Motivo);
            AddLabelValueParagraph(row2.Cells[1], "Sinistro", vm.Sinistro);
            row2.Cells[2].AddParagraph();
            AddLabelValueParagraph(row2.Cells[3], "Placa", vm.PlacaVeiculo);

            Table thirdRow = section.AddTable();
            thirdRow.Borders.Visible = false;
            thirdRow.AddColumn(Unit.FromCentimeter(7.0));
            thirdRow.AddColumn(Unit.FromCentimeter(6.2));
            thirdRow.AddColumn(Unit.FromCentimeter(0.5));
            thirdRow.AddColumn(Unit.FromCentimeter(5.7));

            Row row3 = thirdRow.AddRow();
            row3.TopPadding = Unit.FromPoint(2);
            row3.BottomPadding = Unit.FromPoint(2);
            AddLabelValueParagraph(row3.Cells[0], "NFiscal Devolu\u00E7\u00E3o", vm.NFDevolucao);
            AddLabelValueParagraph(row3.Cells[1], "Dt NFiscal", vm.DataVenda.HasValue ? vm.DataVenda.Value.ToString("dd/MM/yyyy", PtBrCulture) : string.Empty);
            row3.Cells[2].AddParagraph();
            AddLabelValueParagraph(row3.Cells[3], "NFiscal Venda", vm.NFVenda);

            section.AddParagraph().Format.SpaceAfter = Unit.FromPoint(4);
        }

        private void AddItens(Section section, List<DevolucaoPrintItemViewModel> itens)
        {
            Table table = section.AddTable();
            table.Borders.Visible = false;
            table.Rows.LeftIndent = 0;
            table.AddColumn(Unit.FromCentimeter(2.8));
            table.AddColumn(Unit.FromCentimeter(6.8));
            table.AddColumn(Unit.FromCentimeter(1.8));
            table.AddColumn(Unit.FromCentimeter(2.5));
            table.AddColumn(Unit.FromCentimeter(1.8));
            table.AddColumn(Unit.FromCentimeter(3.7));

            Row header = table.AddRow();
            header.TopPadding = Unit.FromPoint(3);
            header.BottomPadding = Unit.FromPoint(5);
            header.Format.Font.Bold = true;

            AddHeaderCell(header.Cells[0], "Item Nr");
            AddHeaderCell(header.Cells[1], "Descri\u00E7\u00E3o");
            AddHeaderCell(header.Cells[2], "Qtde", ParagraphAlignment.Center);
            AddHeaderCell(header.Cells[3], "R$ Total", ParagraphAlignment.Right);
            AddHeaderCell(header.Cells[4], "Localizada", ParagraphAlignment.Center);
            AddHeaderCell(header.Cells[5], "Observa\u00E7\u00E3o");

            if (itens.Count == 0)
            {
                Row emptyRow = table.AddRow();
                emptyRow.TopPadding = Unit.FromPoint(6);
                emptyRow.BottomPadding = Unit.FromPoint(6);
                emptyRow.Cells[0].MergeRight = 5;
                emptyRow.Cells[0].AddParagraph("Nenhum item localizado.");
                emptyRow.Cells[0].Borders.Bottom.Width = 0.5;
                return;
            }

            foreach (DevolucaoPrintItemViewModel item in itens)
            {
                Row row = table.AddRow();
                row.TopPadding = Unit.FromPoint(4);
                row.BottomPadding = Unit.FromPoint(5);

                AddItemCell(row.Cells[0], SafeText(item.ItemNr), ParagraphAlignment.Left, true);
                AddItemCell(row.Cells[1], SafeText(item.Descricao));
                AddItemCell(row.Cells[2], item.Quantidade.ToString("N0", PtBrCulture), ParagraphAlignment.Center);
                AddItemCell(row.Cells[3], (item.Quantidade * item.ValorUnitario).ToString("N2", PtBrCulture), ParagraphAlignment.Right);
                AddItemCell(row.Cells[4], "(     )", ParagraphAlignment.Center);
                AddItemCell(row.Cells[5], GetItemObservacao(item.Observacao));
            }
        }

        private void AddHeaderCell(Cell cell, string text, ParagraphAlignment alignment = ParagraphAlignment.Left)
        {
            Paragraph paragraph = cell.AddParagraph(text);
            paragraph.Format.Alignment = alignment;
            paragraph.Format.Font.Bold = true;
            cell.Borders.Bottom.Width = 0.75;
        }

        private void AddItemCell(Cell cell, string text, ParagraphAlignment alignment = ParagraphAlignment.Left, bool bold = false)
        {
            Paragraph paragraph = cell.AddParagraph(text);
            paragraph.Format.Alignment = alignment;
            paragraph.Format.Font.Bold = bold;
            cell.Borders.Bottom.Width = 0.5;
        }

        private void AddLabelValueParagraph(Cell cell, string label, string value)
        {
            Paragraph paragraph = cell.AddParagraph();
            paragraph.Format.SpaceAfter = Unit.FromPoint(2);
            paragraph.AddFormattedText(string.Concat(label, " "), TextFormat.Bold);
            paragraph.AddText(SafeText(value));
        }

        private static string ResolveTituloAutorizacao(string movimento)
        {
            string movimentoNormalizado = Util.RemoverAcentuacao((movimento ?? string.Empty).Trim()).ToUpperInvariant();

            if (movimentoNormalizado == "TROCA")
            {
                return "Autoriza\u00E7\u00E3o de Troca";
            }

            if (movimentoNormalizado == "GARANTIA")
            {
                return "Autoriza\u00E7\u00E3o de Garantia";
            }

            return "Autoriza\u00E7\u00E3o de Devolu\u00E7\u00E3o";
        }

        private static string GetItemObservacao(string observacao)
        {
            return string.IsNullOrWhiteSpace(observacao) ? string.Empty : observacao.Trim();
        }

        private static string SafeText(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private void TryDrawHeaderAssets(object pdfDocument, string controleNr)
        {
            if (pdfDocument == null)
            {
                return;
            }

            try
            {
                Assembly pdfSharpAssembly = pdfDocument.GetType().Assembly;
                Assembly barCodesAssembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(x => string.Equals(x.GetName().Name, "PdfSharp.BarCodes-gdi", StringComparison.OrdinalIgnoreCase))
                    ?? Assembly.Load("PdfSharp.BarCodes-gdi");

                object pages = pdfDocument.GetType().GetProperty("Pages").GetValue(pdfDocument, null);
                object firstPage = pages.GetType().GetMethod("get_Item").Invoke(pages, new object[] { 0 });

                Type xGraphicsType = pdfSharpAssembly.GetType("PdfSharp.Drawing.XGraphics", true);
                MethodInfo fromPdfPageMethod = xGraphicsType.GetMethod("FromPdfPage", new[] { firstPage.GetType() });
                object graphics = fromPdfPageMethod.Invoke(null, new[] { firstPage });

                try
                {
                    TryDrawHeaderLogo(pdfSharpAssembly, graphics);
                    if (!string.IsNullOrWhiteSpace(controleNr))
                    {
                        TryDrawHeaderBarcodeAndControle(pdfSharpAssembly, barCodesAssembly, firstPage, graphics, controleNr.Trim().ToUpperInvariant());
                    }
                }
                finally
                {
                    (graphics as IDisposable)?.Dispose();
                }
            }
            catch
            {
            }
        }

        private void TryDrawHeaderLogo(Assembly pdfSharpAssembly, object graphics)
        {
            string imagePath = Server.MapPath("~/Content/img/Logo.png");
            if (!System.IO.File.Exists(imagePath))
            {
                return;
            }

            Type xImageType = pdfSharpAssembly.GetType("PdfSharp.Drawing.XImage", true);
            Type xGraphicsType = pdfSharpAssembly.GetType("PdfSharp.Drawing.XGraphics", true);

            MethodInfo fromFileMethod = xImageType.GetMethod("FromFile", new[] { typeof(string) });
            object image = fromFileMethod.Invoke(null, new object[] { imagePath });

            try
            {
                double pointWidth = Convert.ToDouble(xImageType.GetProperty("PointWidth").GetValue(image, null), CultureInfo.InvariantCulture);
                double pointHeight = Convert.ToDouble(xImageType.GetProperty("PointHeight").GetValue(image, null), CultureInfo.InvariantCulture);
                double drawWidth = 108d;
                double drawHeight = pointWidth <= 0 ? 40d : drawWidth * (pointHeight / pointWidth);
                double drawY = 18d - Unit.FromCentimeter(1).Point;

                MethodInfo drawImageMethod = xGraphicsType.GetMethod("DrawImage", new[] { xImageType, typeof(double), typeof(double), typeof(double), typeof(double) });
                drawImageMethod.Invoke(graphics, new object[] { image, 12d, drawY, drawWidth, drawHeight });
            }
            finally
            {
                (image as IDisposable)?.Dispose();
            }
        }

        private void TryDrawHeaderBarcodeAndControle(Assembly pdfSharpAssembly, Assembly barCodesAssembly, object firstPage, object graphics, string controleNr)
        {
            try
            {
                Type xGraphicsType = pdfSharpAssembly.GetType("PdfSharp.Drawing.XGraphics", true);
                Type xSizeType = pdfSharpAssembly.GetType("PdfSharp.Drawing.XSize", true);
                Type xPointType = pdfSharpAssembly.GetType("PdfSharp.Drawing.XPoint", true);
                Type xUnitType = pdfSharpAssembly.GetType("PdfSharp.Drawing.XUnit", true);
                Type xFontType = pdfSharpAssembly.GetType("PdfSharp.Drawing.XFont", true);
                Type xFontStyleExType = pdfSharpAssembly.GetType("PdfSharp.Drawing.XFontStyleEx", true);
                Type xBrushType = pdfSharpAssembly.GetType("PdfSharp.Drawing.XBrush", true);
                Type xBrushesType = pdfSharpAssembly.GetType("PdfSharp.Drawing.XBrushes", true);
                Type xRectType = pdfSharpAssembly.GetType("PdfSharp.Drawing.XRect", true);
                Type xStringFormatType = pdfSharpAssembly.GetType("PdfSharp.Drawing.XStringFormat", true);
                Type xStringFormatsType = pdfSharpAssembly.GetType("PdfSharp.Drawing.XStringFormats", true);

                Type codeType = barCodesAssembly.GetType("PdfSharp.Drawing.BarCodes.CodeType", true);
                Type barCodeType = barCodesAssembly.GetType("PdfSharp.Drawing.BarCodes.BarCode", true);
                Type textLocationType = barCodesAssembly.GetType("PdfSharp.Drawing.BarCodes.TextLocation", true);
                Type extensionsType = barCodesAssembly.GetType("PdfSharp.Extensions", true);

                object size = Activator.CreateInstance(xSizeType, 138d, 42d);
                object code39 = Enum.Parse(codeType, "Code3of9Standard");
                MethodInfo fromTypeMethod = barCodeType.GetMethod("FromType", new[] { codeType, typeof(string), xSizeType });
                object barcode = fromTypeMethod.Invoke(null, new object[] { code39, controleNr, size });

                PropertyInfo textLocationProperty = barCodeType.GetProperty("TextLocation");
                if (textLocationProperty != null && textLocationProperty.CanWrite)
                {
                    object noneTextLocation = Enum.Parse(textLocationType, "None");
                    textLocationProperty.SetValue(barcode, noneTextLocation, null);
                }

                object widthValue = firstPage.GetType().GetProperty("Width").GetValue(firstPage, null);
                double pageWidth = Convert.ToDouble(xUnitType.GetProperty("Point").GetValue(widthValue, null), CultureInfo.InvariantCulture);

                double barcodeX = pageWidth - 158d;
                double barcodeY = 18d;
                object point = Activator.CreateInstance(xPointType, barcodeX, barcodeY);

                MethodInfo drawMethod = extensionsType.GetMethod("DrawBarCode", new[] { xGraphicsType, barCodeType, xPointType });
                drawMethod.Invoke(null, new[] { graphics, barcode, point });

                object fontStyleBold = Enum.Parse(xFontStyleExType, "Bold");
                object font = Activator.CreateInstance(xFontType, "Arial", 18d, fontStyleBold);
                object brush = xBrushesType.GetProperty("Black").GetValue(null, null);
                double controleOffsetLeft = Unit.FromCentimeter(1.5).Point;
                object rect = Activator.CreateInstance(xRectType, (barcodeX + 56.70d) - controleOffsetLeft, 62d, 120d, 24d);
                object format = xStringFormatsType.GetProperty("TopCenter").GetValue(null, null);

                MethodInfo drawStringMethod = xGraphicsType.GetMethod("DrawString", new[] { typeof(string), xFontType, xBrushType, xRectType, xStringFormatType });
                drawStringMethod.Invoke(graphics, new object[] { controleNr, font, brush, rect, format });
            }
            catch
            {
            }
        }

        private void TryDrawControleBarCode(object pdfDocument, string controleNr)
        {
            if (pdfDocument == null || string.IsNullOrWhiteSpace(controleNr))
            {
                return;
            }

            try
            {
                Assembly pdfSharpAssembly = pdfDocument.GetType().Assembly;
                Assembly barCodesAssembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(x => string.Equals(x.GetName().Name, "PdfSharp.BarCodes-gdi", StringComparison.OrdinalIgnoreCase))
                    ?? Assembly.Load("PdfSharp.BarCodes-gdi");

                object pages = pdfDocument.GetType().GetProperty("Pages").GetValue(pdfDocument, null);
                object firstPage = pages.GetType().GetMethod("get_Item").Invoke(pages, new object[] { 0 });

                Type xGraphicsType = pdfSharpAssembly.GetType("PdfSharp.Drawing.XGraphics", true);
                MethodInfo fromPdfPageMethod = xGraphicsType.GetMethod("FromPdfPage", new[] { firstPage.GetType() });
                object graphics = fromPdfPageMethod.Invoke(null, new[] { firstPage });

                try
                {
                    Type xSizeType = pdfSharpAssembly.GetType("PdfSharp.Drawing.XSize", true);
                    Type xPointType = pdfSharpAssembly.GetType("PdfSharp.Drawing.XPoint", true);
                    Type xUnitType = pdfSharpAssembly.GetType("PdfSharp.Drawing.XUnit", true);
                    Type codeType = barCodesAssembly.GetType("PdfSharp.Drawing.BarCodes.CodeType", true);
                    Type barCodeType = barCodesAssembly.GetType("PdfSharp.Drawing.BarCodes.BarCode", true);
                    Type textLocationType = barCodesAssembly.GetType("PdfSharp.Drawing.BarCodes.TextLocation", true);
                    Type extensionsType = barCodesAssembly.GetType("PdfSharp.Extensions", true);

                    object size = Activator.CreateInstance(xSizeType, 150d, 42d);
                    object code39 = Enum.Parse(codeType, "Code3of9Standard");
                    MethodInfo fromTypeMethod = barCodeType.GetMethod("FromType", new[] { codeType, typeof(string), xSizeType });
                    object barcode = fromTypeMethod.Invoke(null, new object[] { code39, controleNr.Trim().ToUpperInvariant(), size });

                    PropertyInfo textLocationProperty = barCodeType.GetProperty("TextLocation");
                    if (textLocationProperty != null && textLocationProperty.CanWrite)
                    {
                        object noneTextLocation = Enum.Parse(textLocationType, "None");
                        textLocationProperty.SetValue(barcode, noneTextLocation, null);
                    }

                    object widthValue = firstPage.GetType().GetProperty("Width").GetValue(firstPage, null);
                    double pageWidth = Convert.ToDouble(xUnitType.GetProperty("Point").GetValue(widthValue, null), CultureInfo.InvariantCulture);

                    double x = pageWidth - 170d;
                    double y = 18d;
                    object point = Activator.CreateInstance(xPointType, x, y);

                    MethodInfo drawMethod = extensionsType.GetMethod("DrawBarCode", new[] { xGraphicsType, barCodeType, xPointType });
                    drawMethod.Invoke(null, new[] { graphics, barcode, point });
                }
                finally
                {
                    (graphics as IDisposable)?.Dispose();
                }
            }
            catch
            {
            }
        }

        private void EnsureDevolucaoComplementoTable()
        {
            string sql = @"
IF OBJECT_ID('dbo.DevolucaoComplemento', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.DevolucaoComplemento
    (
        DevolucaoId INT NOT NULL PRIMARY KEY,
        DocExpedicaoId INT NULL,
        NotaFiscalId INT NULL,
        DataVenda DATETIME NULL,
        CriadoPor VARCHAR(100) NULL,
        CriadoEm DATETIME NULL,
        ModificadoPor VARCHAR(100) NULL,
        ModificadoEm DATETIME NULL
    );
END";

            db.Database.ExecuteSqlCommand(sql);
        }

        private string GetAndIncrementControleNr(string usuarioAtual, DateTime agora)
        {
            const string selectSql = @"
SELECT TOP 1 Id, Valor
FROM AppConfig WITH (UPDLOCK, HOLDLOCK, ROWLOCK)
WHERE Nome = @nome
  AND (FilialId = @filialId OR FilialId IS NULL)
ORDER BY CASE WHEN FilialId = @filialId THEN 0 ELSE 1 END, Id";

            ControleNrConfigRow config = db.Database.SqlQuery<ControleNrConfigRow>(
                selectSql,
                new SqlParameter("@nome", ControleNrConfigName),
                new SqlParameter("@filialId", filialId)).FirstOrDefault();

            if (config == null || string.IsNullOrWhiteSpace(config.Valor))
            {
                throw new InvalidOperationException("Parametro ControleNr nao localizado na AppConfig.");
            }

            if (!int.TryParse(config.Valor, out int controleAtual))
            {
                throw new InvalidOperationException("Valor invalido para ControleNr na AppConfig.");
            }

            const string updateSql = @"
UPDATE AppConfig
SET Valor = @novoValor,
    ModificadoPor = @modificadoPor,
    ModificadoEm = @modificadoEm
WHERE Id = @id";

            int registrosAfetados = db.Database.ExecuteSqlCommand(
                updateSql,
                new SqlParameter("@novoValor", (controleAtual + 1).ToString(CultureInfo.InvariantCulture)),
                new SqlParameter("@modificadoPor", usuarioAtual),
                new SqlParameter("@modificadoEm", agora),
                new SqlParameter("@id", config.Id));

            if (registrosAfetados == 0)
            {
                throw new InvalidOperationException("Nao foi possivel atualizar o ControleNr.");
            }

            return controleAtual.ToString(CultureInfo.InvariantCulture);
        }

        private int ResolveStatusDevolucaoInicial()
        {
            int? statusId = db.StatusDevolucao
                .AsNoTracking()
                .OrderBy(x => x.Id)
                .Select(x => (int?)x.Id)
                .FirstOrDefault();

            return statusId ?? 1;
        }

        private int InsertDevolucao(
            DevolucaoCadastroViewModel vm,
            string controleNr,
            string vendedorFinal,
            int statusId,
            string usuarioAtual,
            DateTime agora)
        {
            const string sql = @"
INSERT INTO dbo.Devolucao
(
    DevolucaoNr,
    Movimento,
    Retirar,
    MotivoId,
    NFVenda,
    Cliente,
    Vendedor,
    TransportadoraId,
    NFDevolucao,
    Sinistro,
    PlacaVeiculo,
    Observacao,
    StatusId,
    FilialId,
    CriadoPor,
    CriadoEm,
    ModificadoPor,
    ModificadoEm
)
VALUES
(
    @DevolucaoNr,
    @Movimento,
    @Retirar,
    @MotivoId,
    @NFVenda,
    @Cliente,
    @Vendedor,
    @TransportadoraId,
    @NFDevolucao,
    @Sinistro,
    @PlacaVeiculo,
    @Observacao,
    @StatusId,
    @FilialId,
    @CriadoPor,
    @CriadoEm,
    @ModificadoPor,
    @ModificadoEm
);
SELECT CAST(SCOPE_IDENTITY() AS INT);";

            return db.Database.SqlQuery<int>(
                sql,
                new SqlParameter("@DevolucaoNr", controleNr),
                new SqlParameter("@Movimento", vm.Movimento ?? string.Empty),
                new SqlParameter("@Retirar", vm.Retirar ?? string.Empty),
                new SqlParameter("@MotivoId", (object)vm.MotivoId ?? DBNull.Value),
                new SqlParameter("@NFVenda", vm.NFVenda ?? string.Empty),
                new SqlParameter("@Cliente", vm.Cliente ?? string.Empty),
                new SqlParameter("@Vendedor", vendedorFinal ?? string.Empty),
                new SqlParameter("@TransportadoraId", (object)vm.TransportadoraId ?? DBNull.Value),
                new SqlParameter("@NFDevolucao", (object)(vm.NFDevolucao ?? string.Empty)),
                new SqlParameter("@Sinistro", (object)(vm.Sinistro ?? string.Empty)),
                new SqlParameter("@PlacaVeiculo", (object)(vm.PlacaVeiculo ?? string.Empty)),
                new SqlParameter("@Observacao", (object)(vm.Observacao ?? string.Empty)),
                new SqlParameter("@StatusId", statusId),
                new SqlParameter("@FilialId", filialId),
                new SqlParameter("@CriadoPor", usuarioAtual),
                new SqlParameter("@CriadoEm", agora),
                new SqlParameter("@ModificadoPor", usuarioAtual),
                new SqlParameter("@ModificadoEm", agora))
                .Single();
        }

        private void InsertDevolucaoItem(
            int devolucaoId,
            DevolucaoCadastroItemViewModel item,
            int statusId,
            string usuarioAtual,
            DateTime agora)
        {
            string sql = string.Format(
                CultureInfo.InvariantCulture,
                @"
INSERT INTO dbo.DevolucaoItem
(
    DevolucaoId,
    ItemNr,
    Quantidade,
    [{0}],
    StatusId,
    OcorrenciaId,
    Observacao,
    CriadoPor,
    CriadoEm,
    ModificadoPor,
    ModificadoEm
)
VALUES
(
    @DevolucaoId,
    @ItemNr,
    @Quantidade,
    @ValorUnitario,
    @StatusId,
    @OcorrenciaId,
    @Observacao,
    @CriadoPor,
    @CriadoEm,
    @ModificadoPor,
    @ModificadoEm
);",
                ValorUnitarioColumnName);

            db.Database.ExecuteSqlCommand(
                sql,
                new SqlParameter("@DevolucaoId", devolucaoId),
                new SqlParameter("@ItemNr", NormalizeCodigoMaterial(item.ItemNr)),
                new SqlParameter("@Quantidade", item.Quantidade),
                new SqlParameter("@ValorUnitario", item.ValorUnitario),
                new SqlParameter("@StatusId", statusId),
                new SqlParameter("@OcorrenciaId", DBNull.Value),
                new SqlParameter("@Observacao", (object)(item.Observacao ?? string.Empty)),
                new SqlParameter("@CriadoPor", usuarioAtual),
                new SqlParameter("@CriadoEm", agora),
                new SqlParameter("@ModificadoPor", usuarioAtual),
                new SqlParameter("@ModificadoEm", agora));
        }

        private void InsertDevolucaoComplemento(
            int devolucaoId,
            DocExpedicao documentoVenda,
            int notaFiscalId,
            string usuarioAtual,
            DateTime agora)
        {
            const string sql = @"
INSERT INTO dbo.DevolucaoComplemento
(
    DevolucaoId,
    DocExpedicaoId,
    NotaFiscalId,
    DataVenda,
    CriadoPor,
    CriadoEm,
    ModificadoPor,
    ModificadoEm
)
VALUES
(
    @DevolucaoId,
    @DocExpedicaoId,
    @NotaFiscalId,
    @DataVenda,
    @CriadoPor,
    @CriadoEm,
    @ModificadoPor,
    @ModificadoEm
);";

            db.Database.ExecuteSqlCommand(
                sql,
                new SqlParameter("@DevolucaoId", devolucaoId),
                new SqlParameter("@DocExpedicaoId", (object)(documentoVenda == null ? (int?)null : documentoVenda.Id) ?? DBNull.Value),
                new SqlParameter("@NotaFiscalId", notaFiscalId),
                new SqlParameter("@DataVenda", (object)(documentoVenda == null ? (DateTime?)null : documentoVenda.DataEmissao) ?? DBNull.Value),
                new SqlParameter("@CriadoPor", usuarioAtual),
                new SqlParameter("@CriadoEm", agora),
                new SqlParameter("@ModificadoPor", usuarioAtual),
                new SqlParameter("@ModificadoEm", agora));
        }

        private void EnsureNotaFiscalTipoDevolucao(int notaFiscalId, int filialId)
        {
            const string sql = @"
UPDATE dbo.NotaFiscal
SET TipoId = 2
WHERE Id = @Id
  AND FilialId = @FilialId;

SELECT TOP 1 TipoId
FROM dbo.NotaFiscal
WHERE Id = @Id
  AND FilialId = @FilialId;";

            object result = db.Database.SqlQuery<int?>(
                sql,
                new SqlParameter("@Id", notaFiscalId),
                new SqlParameter("@FilialId", filialId)).FirstOrDefault();

            int? tipoId = result as int?;
            if (tipoId != 2)
            {
                throw new InvalidOperationException("N\u00E3o foi poss\u00EDvel gravar a Nota Fiscal da devolu\u00E7\u00E3o com TipoId = 2.");
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

        private sealed class ControleNrConfigRow
        {
            public int Id { get; set; }
            public string Valor { get; set; }
        }
    }
}
