using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Data.Entity;
using System.Web;
using System.IO;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using Newtonsoft.Json;

using Simplify.Quasar.Areas.RecebimentoApp.ViewModels;
using Simplify.Quasar.Models;
using Simplify.Quasar.Custom;

namespace Simplify.Quasar.Areas.RecebimentoApp.Controllers
{
    [ValidateSession]
    public class NotaFiscalController : Controller
    {
        private const string UltimoArquivoTransitoConfigName = "RecebimentoUltimoArquivoTransito";

        Quasar_Entities db = new Quasar_Entities();

        int filialId = Util.GetCurrentFilial();
        public string IdNF { get; private set; }

        int periodo;
        DateTime inicio;

        public NotaFiscalController()
        {
            periodo = Util.GetPeriodoRecebimento();
            inicio = Util.GetCurrentDateTime().Date.AddDays(-periodo);

        }

        // GET: Recebimento/NotaFiscal/Index
        public ActionResult Index()
        {
            //DateTime inicio = Util.GetCurrentDateTime().AddDays(-30);

            ViewBag.UltimaAtualizacaoTransito = db.Database.SqlQuery<DateTime?>(
                "SELECT MAX(Dtatual) FROM dbo.TransitoUploadColumns WHERE FilialId = @p0",
                filialId).FirstOrDefault();

            ViewBag.UltimoArquivoTransito = db.AppConfig
                .AsNoTracking()
                .Where(x => x.Nome == UltimoArquivoTransitoConfigName && x.FilialId == filialId)
                .OrderByDescending(x => x.Id)
                .Select(x => x.Valor)
                .FirstOrDefault();

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
                    item.NomeEmissor = (from origem in db.OrigemNotaFiscal
                                        where origem.Codigo == item.Emissor
                                        select origem.Descricao).FirstOrDefault() ?? "GM";
                }
            }

            PopulateDevolucaoIndexData(vm);

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

        [HttpGet]
        public ActionResult DevolucaoDetalhe(int id)
        {
            DevolucaoRecebimentoViewModel vm = BuildDevolucaoRecebimentoViewModel(id);
            if (vm == null)
            {
                return HttpNotFound();
            }

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DevolucaoDetalhe(DevolucaoRecebimentoViewModel vm)
        {
            DevolucaoRecebimentoViewModel atual = BuildDevolucaoRecebimentoViewModel(vm.Id);
            if (atual == null)
            {
                return HttpNotFound();
            }

            int statusFinalizadoId = ResolveStatusDevolucaoRecebimentoFinalizadoId();
            List<DevolucaoRecebimentoItemViewModel> itensInformados = (vm.Itens ?? new List<DevolucaoRecebimentoItemViewModel>());
            bool possuiQuantidadeComProblema = itensInformados.Any(x => x.OcorrenciaInformada.HasValue && x.OcorrenciaInformada.Value > 0);
            bool possuiOcorrenciaSelecionada = itensInformados.Any(x => x.OcorrenciaId.HasValue && x.OcorrenciaId.Value > 0);
            bool possuiDadosOcorrencia = possuiQuantidadeComProblema || possuiOcorrenciaSelecionada;
            vm.StatusId = possuiQuantidadeComProblema ? atual.StatusOcorrenciaId : statusFinalizadoId;
            bool isOcorrencia = vm.StatusId.Value == atual.StatusOcorrenciaId;

            List<DevolucaoRecebimentoItemViewModel> itensOcorrencia = new List<DevolucaoRecebimentoItemViewModel>();
            if (possuiDadosOcorrencia)
            {
                itensOcorrencia = itensInformados
                    .Where(x => (x.OcorrenciaInformada.HasValue && x.OcorrenciaInformada.Value > 0) || (x.OcorrenciaId.HasValue && x.OcorrenciaId.Value > 0))
                    .ToList();

                Dictionary<int, DevolucaoRecebimentoItemViewModel> itensAtuais = (atual.Itens ?? new List<DevolucaoRecebimentoItemViewModel>())
                    .ToDictionary(x => x.DevolucaoItemId, x => x);

                foreach (DevolucaoRecebimentoItemViewModel item in itensOcorrencia)
                {
                    DevolucaoRecebimentoItemViewModel itemAtual;
                    if (!itensAtuais.TryGetValue(item.DevolucaoItemId, out itemAtual))
                    {
                        ModelState.AddModelError(string.Empty, "Existem itens inválidos na devolução.");
                        continue;
                    }

                    if (!item.OcorrenciaInformada.HasValue || item.OcorrenciaInformada.Value <= 0)
                    {
                        ModelState.AddModelError(string.Empty, "Preencha a ocorrência dos itens com problema.");
                    }
                    else if (item.OcorrenciaInformada.Value > itemAtual.Quantidade)
                    {
                        ModelState.AddModelError(string.Empty, "A Qtde com Problema não pode ser maior que a quantidade do item.");
                    }

                    if (!item.OcorrenciaId.HasValue || item.OcorrenciaId.Value <= 0)
                    {
                        ModelState.AddModelError(string.Empty, "Selecione a ocorrência para os itens com problema.");
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                MergeDevolucaoRecebimentoInput(atual, vm);
                return View(atual);
            }

            string usuarioAtual = Util.GetCurrentUser();
            DateTime agora = Util.GetCurrentDateTime();

            try
            {
                using (DbContextTransaction tr = db.Database.BeginTransaction())
                {
                    Devolucao devolucao = db.Devolucao
                        .AsNoTracking()
                        .FirstOrDefault(x => x.Id == vm.Id && x.FilialId == filialId);
                    if (devolucao == null)
                    {
                        tr.Rollback();
                        return HttpNotFound();
                    }

                    db.Database.ExecuteSqlCommand(
                        @"
UPDATE dbo.Devolucao
SET StatusId = @statusId,
    ModificadoPor = @modificadoPor,
    ModificadoEm = @modificadoEm
WHERE Id = @id
  AND FilialId = @filialId",
                        new SqlParameter("@statusId", vm.StatusId.Value),
                        new SqlParameter("@modificadoPor", usuarioAtual),
                        new SqlParameter("@modificadoEm", agora),
                        new SqlParameter("@id", vm.Id),
                        new SqlParameter("@filialId", filialId));

                    DevolucaoComplemento complemento = db.DevolucaoComplemento
                        .AsNoTracking()
                        .FirstOrDefault(x => x.DevolucaoId == vm.Id);

                    if (complemento != null && complemento.NotaFiscalId.HasValue)
                    {
                        int notaFiscalStatusId;
                        int? notaFiscalItemStatusId;
                        if (TryResolveNotaFiscalStatusByDevolucaoStatus(vm.StatusId.Value, out notaFiscalStatusId, out notaFiscalItemStatusId))
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

                            if (notaFiscalItemStatusId.HasValue)
                            {
                                db.Database.ExecuteSqlCommand(
                                    @"
UPDATE dbo.NotaFiscalItem
SET StatusId = @statusId,
    ModificadoPor = @modificadoPor,
    ModificadoEm = @modificadoEm
WHERE NotaFiscalId = @notaFiscalId
  AND FilialId = @filialId",
                                    new SqlParameter("@statusId", notaFiscalItemStatusId.Value),
                                    new SqlParameter("@modificadoPor", usuarioAtual),
                                    new SqlParameter("@modificadoEm", agora),
                                    new SqlParameter("@notaFiscalId", complemento.NotaFiscalId.Value),
                                    new SqlParameter("@filialId", filialId));
                            }
                        }
                    }

                    if (isOcorrencia)
                    {
                        if (complemento == null || !complemento.NotaFiscalId.HasValue)
                        {
                            throw new InvalidOperationException("Nota Fiscal vinculada à devolução não localizada.");
                        }

                        List<int> devolucaoItemIds = itensOcorrencia.Select(x => x.DevolucaoItemId).Distinct().ToList();
                        List<DevolucaoItem> devolucaoItens = db.DevolucaoItem
                            .AsNoTracking()
                            .Where(x => x.DevolucaoId == vm.Id && devolucaoItemIds.Contains(x.Id))
                            .OrderBy(x => x.Id)
                            .ToList();

                        List<NotaFiscalItem> notaFiscalItens = db.NotaFiscalItem
                            .AsNoTracking()
                            .Where(x => x.NotaFiscalId == complemento.NotaFiscalId.Value && x.FilialId == filialId)
                            .OrderBy(x => x.Id)
                            .ToList();

                        HashSet<int> notaFiscalItensConsumidos = new HashSet<int>();
                        foreach (DevolucaoRecebimentoItemViewModel itemOcorrencia in itensOcorrencia)
                        {
                            DevolucaoItem devolucaoItem = devolucaoItens.FirstOrDefault(x => x.Id == itemOcorrencia.DevolucaoItemId);
                            if (devolucaoItem == null)
                            {
                                throw new InvalidOperationException("Item de devolução não localizado para gravação da ocorrência.");
                            }

                            int quantidadeOcorrencia = itemOcorrencia.OcorrenciaInformada ?? 0;
                            if (quantidadeOcorrencia <= 0)
                            {
                                throw new InvalidOperationException(string.Concat("Item ", devolucaoItem.ItemNr, ": a quantidade de ocorrência deve ser maior que zero."));
                            }

                            int quantidadeAtual = devolucaoItem.Quantidade ?? 0;
                            if (quantidadeOcorrencia > quantidadeAtual)
                            {
                                throw new InvalidOperationException(string.Concat("Item ", devolucaoItem.ItemNr, ": a ocorrência não pode ser maior que a quantidade atual."));
                            }

                            NotaFiscalItem notaFiscalItem = notaFiscalItens
                                .Where(x =>
                                    !notaFiscalItensConsumidos.Contains(x.Id) &&
                                    string.Equals((x.Item ?? string.Empty).Trim(), (devolucaoItem.ItemNr ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase) &&
                                    x.Quantidade >= quantidadeOcorrencia)
                                .OrderBy(x => x.Id)
                                .FirstOrDefault();

                            if (notaFiscalItem == null)
                            {
                                throw new InvalidOperationException(string.Concat("Não foi possível localizar o item da Nota Fiscal para registrar a ocorrência do item ", devolucaoItem.ItemNr, "."));
                            }

                            db.Database.ExecuteSqlCommand(
                                @"
UPDATE dbo.DevolucaoItem
SET StatusId = @statusId,
    OcorrenciaId = @ocorrenciaId,
    QtdeOcorrencia = @qtdeOcorrencia,
    ModificadoPor = @modificadoPor,
    ModificadoEm = @modificadoEm
WHERE Id = @id
  AND DevolucaoId = @devolucaoId",
                                new SqlParameter("@statusId", atual.StatusOcorrenciaId),
                                new SqlParameter("@ocorrenciaId", itemOcorrencia.OcorrenciaId.Value),
                                new SqlParameter("@qtdeOcorrencia", quantidadeOcorrencia),
                                new SqlParameter("@modificadoPor", usuarioAtual),
                                new SqlParameter("@modificadoEm", agora),
                                new SqlParameter("@id", devolucaoItem.Id),
                                new SqlParameter("@devolucaoId", vm.Id));

                            decimal novaQuantidadeNotaFiscal = notaFiscalItem.Quantidade - quantidadeOcorrencia;
                            db.Database.ExecuteSqlCommand(
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

                            notaFiscalItensConsumidos.Add(notaFiscalItem.Id);
                        }
                    }
                    else
                    {
                        db.Database.ExecuteSqlCommand(
                            @"
UPDATE dbo.DevolucaoItem
SET StatusId = @statusId,
    OcorrenciaId = NULL,
    QtdeOcorrencia = NULL,
    ModificadoPor = @modificadoPor,
    ModificadoEm = @modificadoEm
WHERE DevolucaoId = @devolucaoId",
                            new SqlParameter("@statusId", vm.StatusId.Value),
                            new SqlParameter("@modificadoPor", usuarioAtual),
                            new SqlParameter("@modificadoEm", agora),
                            new SqlParameter("@devolucaoId", vm.Id));
                    }

                    tr.Commit();
                }

                TempData["SuccessMessage"] = "Processo de devolução atualizado com sucesso.";
                return RedirectToAction("DevolucaoDetalhe", new { id = vm.Id });
            }
            catch (Exception ex)
            {
                MergeDevolucaoRecebimentoInput(atual, vm);
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(atual);
            }
        }

        public ActionResult RecebimentoADM()
        {
            return View();
        }

        [HttpPost]
        public ActionResult GetDataADM()
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

            var query = (from nf in db.NotaFiscal.AsNoTracking()
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
                             NomeEmissor = nf.TipoId == 1
                                ? (from f in db.Fornecedor where f.CNPJ == nf.Emissor select f.Nome).FirstOrDefault()
                                : nf.TipoId == 3
                                    ? (from f in db.Empresa where f.CNPJ == nf.Emissor select f.Nome).FirstOrDefault()
                                    : nf.TipoId == 4
                                        ? (from origem in db.OrigemNotaFiscal
                                           where origem.Codigo == nf.Emissor
                                           select origem.Descricao).FirstOrDefault()
                                        : string.Empty,
                             QtdItensNF = (from i in db.NotaFiscalItem where i.NotaFiscalId == nf.Id select i).Count(),
                             QtdItens = (from i in db.NotaFiscalItem where i.NotaFiscalId == nf.Id select i.Item).Distinct().Count(),
                             QtdVolumes = (from i in db.NotaFiscalItem where i.NotaFiscalId == nf.Id select i.Volume).Distinct().Count(),
                             QtdTotal = (from i in db.NotaFiscalItem where i.NotaFiscalId == nf.Id select i.Quantidade).Sum(),
                             CriadoEm = nf.CriadoEm,
                             ModificadoEm = nf.ModificadoEm == null ? nf.CriadoEm : nf.ModificadoEm
                         });

            int recordsTotal = query.Count();
            string termo = model.search == null ? string.Empty : (model.search.value ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(termo))
            {
                query = query.Where(x =>
                    (x.Numero ?? string.Empty).Contains(termo) ||
                    (x.NomeEmissor ?? string.Empty).Contains(termo) ||
                    (x.Emissor ?? string.Empty).Contains(termo) ||
                    (x.StatusNF ?? string.Empty).Contains(termo));
            }

            int recordsFiltered = query.Count();
            int sortIndex = model.order != null && model.order.Length > 0 ? model.order[0].column : -1;
            string sortField = sortIndex >= 0 && model.columns != null && sortIndex < model.columns.Length ? model.columns[sortIndex].data : string.Empty;
            bool desc = model.order != null && model.order.Length > 0 && model.order[0].dir == "desc";
            switch (sortField)
            {
                case "NomeEmissor": query = desc ? query.OrderByDescending(x => x.NomeEmissor) : query.OrderBy(x => x.NomeEmissor); break;
                case "Emissor": query = desc ? query.OrderByDescending(x => x.Emissor) : query.OrderBy(x => x.Emissor); break;
                case "StatusNF": query = desc ? query.OrderByDescending(x => x.StatusNF) : query.OrderBy(x => x.StatusNF); break;
                case "QtdItensNF": query = desc ? query.OrderByDescending(x => x.QtdItensNF) : query.OrderBy(x => x.QtdItensNF); break;
                case "QtdItens": query = desc ? query.OrderByDescending(x => x.QtdItens) : query.OrderBy(x => x.QtdItens); break;
                case "QtdVolumes": query = desc ? query.OrderByDescending(x => x.QtdVolumes) : query.OrderBy(x => x.QtdVolumes); break;
                case "CriadoEmTexto": query = desc ? query.OrderByDescending(x => x.CriadoEm) : query.OrderBy(x => x.CriadoEm); break;
                default: query = desc ? query.OrderByDescending(x => x.Numero) : query.OrderBy(x => x.Numero); break;
            }

            int length = model.length > 0 ? model.length : 25;
            var notas = query.Skip(model.start).Take(length).ToList();

            foreach (var nota in notas)
            {
                if ((nota.TipoId == 1 || nota.TipoId == 3) && !string.IsNullOrWhiteSpace(nota.Emissor))
                {
                    nota.Emissor = Util.FormatCNPJ(nota.Emissor);
                }
                else
                {
                    nota.Emissor = nota.Emissor ?? string.Empty;
                }
                nota.CriadoEmTexto = nota.CriadoEm.HasValue ? nota.CriadoEm.Value.ToString("dd/MM/yyyy HH:mm:ss") : string.Empty;
                nota.ModificadoEmTexto = nota.ModificadoEm.HasValue ? nota.ModificadoEm.Value.ToString("dd/MM/yyyy HH:mm:ss") : string.Empty;
            }

            JsonResult result = Json(new { draw = model.draw, recordsFiltered, recordsTotal, data = notas });
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
        [HttpPost]
        public ActionResult GetDataTransito(string material, DataTableAjaxPostModel model)
        {
            if (model == null)
            {
                model = new DataTableAjaxPostModel
                {
                    draw = 0,
                    start = 0,
                    length = 25
                };
            }

            string materialFiltro = (material ?? string.Empty).Trim();
            if (string.Equals(materialFiltro, "_material", StringComparison.OrdinalIgnoreCase))
            {
                materialFiltro = string.Empty;
            }

            var notas = db.NotaFiscal.AsNoTracking().Where(nf =>
                nf.FilialId == filialId &&
                nf.TipoId == 4 &&
                nf.StatusId < 7 &&
                (nf.CriadoEm >= inicio || nf.ModificadoEm >= inicio));

            if (!string.IsNullOrWhiteSpace(materialFiltro))
            {
                notas = notas.Where(nf => db.NotaFiscalItem.Any(i =>
                    i.NotaFiscalId == nf.Id && i.FilialId == filialId && i.Item == materialFiltro));
            }

            var query = from nf in notas
                        select new TransitoViewModel
                        {
                             NotaFiscalId = nf.Id,
                             NotaFiscalNr = nf.Numero,
                             Origem = (from o in db.OrigemNotaFiscal where o.Codigo == nf.Emissor select o.Codigo + "-" + o.Descricao).FirstOrDefault(),
                            Status = (from s in db.StatusNotaFiscal where s.Id == nf.StatusId select s.Nome).FirstOrDefault(),
                            QtdItensNF = db.NotaFiscalItem.Count(i => i.NotaFiscalId == nf.Id && i.FilialId == filialId),
                             QtdItens = db.NotaFiscalItem.Where(i => i.NotaFiscalId == nf.Id && i.FilialId == filialId).Select(i => i.Item).Distinct().Count(),
                             QtdVolumes = db.NotaFiscalItem.Where(i => i.NotaFiscalId == nf.Id && i.FilialId == filialId).Select(i => i.Volume).Distinct().Count(),
                             QtdTotal = db.NotaFiscalItem.Where(i => i.NotaFiscalId == nf.Id && i.FilialId == filialId).Sum(i => (decimal?)i.Quantidade),
                             CriadoEm = nf.CriadoEm,
                             ModificadoEm = nf.ModificadoEm == null ? nf.CriadoEm : nf.ModificadoEm
                         };

            int recordsTotal = query.Count();
            string termo = model.search == null ? string.Empty : (model.search.value ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(termo))
            {
                 query = query.Where(x =>
                     (x.NotaFiscalNr ?? string.Empty).Contains(termo) ||
                     (x.Origem ?? string.Empty).Contains(termo) ||
                     (x.Status ?? string.Empty).Contains(termo));
            }

            int recordsFiltered = query.Count();
            int sortIndex = model.order != null && model.order.Length > 0 ? model.order[0].column : -1;
            string sortField = sortIndex >= 0 && model.columns != null && sortIndex < model.columns.Length ? model.columns[sortIndex].data : string.Empty;
            bool desc = model.order != null && model.order.Length > 0 && model.order[0].dir == "desc";
            switch (sortField)
            {
                 case "NotaFiscalNr": query = desc ? query.OrderByDescending(x => x.NotaFiscalNr) : query.OrderBy(x => x.NotaFiscalNr); break;
                 case "Origem": query = desc ? query.OrderByDescending(x => x.Origem) : query.OrderBy(x => x.Origem); break;
                case "Status": query = desc ? query.OrderByDescending(x => x.Status) : query.OrderBy(x => x.Status); break;
                 case "QtdItensNF": query = desc ? query.OrderByDescending(x => x.QtdItensNF) : query.OrderBy(x => x.QtdItensNF); break;
                 case "QtdItens": query = desc ? query.OrderByDescending(x => x.QtdItens) : query.OrderBy(x => x.QtdItens); break;
                 case "QtdVolumes": query = desc ? query.OrderByDescending(x => x.QtdVolumes) : query.OrderBy(x => x.QtdVolumes); break;
                 case "CriadoEmTexto": query = desc ? query.OrderByDescending(x => x.CriadoEm) : query.OrderBy(x => x.CriadoEm); break;
                 case "ModificadoEmTexto": query = desc ? query.OrderByDescending(x => x.ModificadoEm) : query.OrderBy(x => x.ModificadoEm); break;
                default: query = query.OrderByDescending(x => x.ModificadoEm).ThenByDescending(x => x.NotaFiscalId); break;
            }

            int length = model.length > 0 ? model.length : 25;
            List<TransitoViewModel> transito = query.Skip(model.start).Take(length).ToList();
             foreach (var item in transito)
             {
                 item.CriadoEmTexto = item.CriadoEm.HasValue
                     ? item.CriadoEm.Value.ToString("dd/MM/yyyy HH:mm:ss")
                     : string.Empty;
                 item.ModificadoEmTexto = item.ModificadoEm.HasValue
                     ? item.ModificadoEm.Value.ToString("dd/MM/yyyy HH:mm:ss")
                    : string.Empty;
            }

            JsonResult result = Json(new { draw = model.draw, recordsFiltered, recordsTotal, data = transito });
            result.MaxJsonLength = int.MaxValue;
            return result;
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
            vm.Emissor = nf.Emissor ?? string.Empty;
            if ((nf.TipoId == 1 || nf.TipoId == 3) && !string.IsNullOrWhiteSpace(nf.Emissor))
            {
                vm.Emissor = Util.FormatCNPJ(nf.Emissor);
            }

            // nome do emissor depende do tipo da NF => 
            // 1 rede (tabela Fornecedor)
            // 2 devolução (vazio) 
            // 3 transferência (tabela Empresa)
            // 4 trânsito GM (fixo "GM")

            vm.NomeEmissor = nf.TipoId == 1 ? (from f in db.Fornecedor where f.CNPJ == nf.Emissor select f.Nome).FirstOrDefault() :
                             nf.TipoId == 3 ? (from f in db.Empresa where f.CNPJ == nf.Emissor select f.Nome).FirstOrDefault() :
                             nf.TipoId == 4 ? (from o in db.OrigemNotaFiscal where o.Codigo == nf.Emissor select o.Descricao).FirstOrDefault() ?? "GM" :
                             string.Empty;

            vm.TipoNF = (from t in db.TipoNotaFiscal where t.Id == nf.TipoId select t.Descricao).FirstOrDefault();
            vm.StatusNF = (from s in db.StatusNotaFiscal where s.Id == nf.StatusId select s.Nome).FirstOrDefault();

            vm.OrigemNF = string.Empty;
            if (nf.TipoId == 4)
            {
                var origemNF = (from o in db.OrigemNotaFiscal where o.Codigo == nf.Emissor select o).FirstOrDefault();
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

        private void PopulateDevolucaoIndexData(List<NotaFiscalViewModel> notas)
        {
            if (notas == null)
            {
                return;
            }

            List<Devolucao> devolucoes = db.Devolucao
                .AsNoTracking()
                .Where(x => x.FilialId == filialId
                    && ((x.ModificadoEm ?? x.CriadoEm) >= inicio))
                .OrderByDescending(x => x.ModificadoEm ?? x.CriadoEm)
                .ToList();

            notas.RemoveAll(x => x.TipoId == 2);

            if (devolucoes.Count == 0)
            {
                return;
            }

            List<int> devolucaoIds = devolucoes.Select(x => x.Id).ToList();
            Dictionary<int, DevolucaoComplemento> complementoLookup = db.DevolucaoComplemento
                .AsNoTracking()
                .Where(x => devolucaoIds.Contains(x.DevolucaoId))
                .ToList()
                .GroupBy(x => x.DevolucaoId)
                .ToDictionary(x => x.Key, x => x.OrderByDescending(y => y.ModificadoEm ?? y.CriadoEm).First());

            List<int> notaFiscalIds = complementoLookup.Values
                .Where(x => x.NotaFiscalId.HasValue)
                .Select(x => x.NotaFiscalId.Value)
                .Distinct()
                .ToList();

            Dictionary<int, NotaFiscal> notaFiscalLookup = db.NotaFiscal
                .AsNoTracking()
                .Where(x => notaFiscalIds.Contains(x.Id) && x.FilialId == filialId)
                .ToList()
                .ToDictionary(x => x.Id, x => x);

            Dictionary<int, Tuple<int, decimal>> itensLookup = db.DevolucaoItem
                .AsNoTracking()
                .Where(x => devolucaoIds.Contains(x.DevolucaoId))
                .ToList()
                .GroupBy(x => x.DevolucaoId)
                .ToDictionary(
                    x => x.Key,
                    x => Tuple.Create(
                        x.Count(),
                        x.Sum(y => Convert.ToDecimal(y.Quantidade ?? 0))));

            foreach (Devolucao devolucao in devolucoes)
            {
                DevolucaoComplemento complemento;
                complementoLookup.TryGetValue(devolucao.Id, out complemento);

                NotaFiscal notaFiscal = null;
                if (complemento != null && complemento.NotaFiscalId.HasValue)
                {
                    notaFiscalLookup.TryGetValue(complemento.NotaFiscalId.Value, out notaFiscal);
                }

                var nota = new NotaFiscalViewModel
                {
                    Id = notaFiscal == null ? 0 : notaFiscal.Id,
                    DevolucaoId = devolucao.Id,
                    ControleNr = devolucao.DevolucaoNr,
                    Numero = notaFiscal == null
                        ? (string.IsNullOrWhiteSpace(devolucao.NFDevolucao) ? devolucao.NFVenda : devolucao.NFDevolucao)
                        : notaFiscal.Numero,
                    Emissor = notaFiscal == null ? devolucao.Cliente : notaFiscal.Emissor,
                    NomeEmissor = notaFiscal == null || string.IsNullOrWhiteSpace(notaFiscal.Emissor)
                        ? (devolucao.Cliente ?? string.Empty)
                        : notaFiscal.Emissor,
                    TipoId = 2,
                    StatusId = notaFiscal == null ? 0 : notaFiscal.StatusId,
                    CriadoEm = devolucao.CriadoEm,
                    ModificadoEm = devolucao.ModificadoEm ?? devolucao.CriadoEm,
                    FilialId = devolucao.FilialId
                };

                Tuple<int, decimal> resumoItens;
                if (itensLookup.TryGetValue(devolucao.Id, out resumoItens))
                {
                    nota.QtdItensNF = resumoItens.Item1;
                    nota.QtdTotal = resumoItens.Item2;
                }
                else
                {
                    nota.QtdItensNF = 0;
                    nota.QtdTotal = 0;
                }

                notas.Add(nota);
            }
        }

        private DevolucaoRecebimentoViewModel BuildDevolucaoRecebimentoViewModel(int devolucaoId)
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

            Dictionary<int, string> statusLookup = db.StatusDevolucao
                .AsNoTracking()
                .OrderBy(x => x.Id)
                .ToList()
                .GroupBy(x => x.Id)
                .ToDictionary(x => x.Key, x => x.Select(y => y.Nome ?? string.Empty).FirstOrDefault() ?? string.Empty);

            IEnumerable<SelectListItem> ocorrenciaDDL = BuildOcorrenciaDDL();

            List<DevolucaoItem> devolucaoItens = db.DevolucaoItem
                .AsNoTracking()
                .Where(x => x.DevolucaoId == devolucaoId)
                .OrderBy(x => x.Id)
                .ToList();

            List<string> itemCodes = devolucaoItens
                .Select(x => (x.ItemNr ?? string.Empty).Trim())
                .Where(x => x != string.Empty)
                .Distinct()
                .ToList();

            Dictionary<string, string> descricaoLookup = db.Material
                .AsNoTracking()
                .Where(x => itemCodes.Contains(x.Codigo) && (x.FilialId == filialId || x.FilialId == null))
                .ToList()
                .GroupBy(x => x.Codigo)
                .ToDictionary(
                    x => x.Key,
                    x => x.OrderByDescending(y => y.FilialId == filialId ? 1 : 0).Select(y => y.Descricao ?? string.Empty).FirstOrDefault() ?? string.Empty);

            int statusOcorrenciaId = 3;
            int statusFinalizadoId = ResolveStatusDevolucaoRecebimentoFinalizadoId();
            DevolucaoRecebimentoViewModel vm = new DevolucaoRecebimentoViewModel
            {
                Id = devolucao.Id,
                NotaFiscalId = complemento == null ? (int?)null : complemento.NotaFiscalId,
                ControleNr = devolucao.DevolucaoNr,
                NotaFiscalNr = notaFiscal == null ? (devolucao.NFDevolucao ?? string.Empty) : (notaFiscal.Numero ?? string.Empty),
                Emissor = notaFiscal == null
                    ? (devolucao.Cliente ?? string.Empty)
                    : (string.IsNullOrWhiteSpace(notaFiscal.Emissor) ? (devolucao.Cliente ?? string.Empty) : (notaFiscal.Emissor ?? string.Empty)),
                StatusId = devolucao.StatusId,
                StatusNome = devolucao.StatusId.HasValue && statusLookup.ContainsKey(devolucao.StatusId.Value) ? statusLookup[devolucao.StatusId.Value] : string.Empty,
                UltimaAtualizacao = devolucao.ModificadoEm ?? devolucao.CriadoEm,
                StatusOcorrenciaId = statusOcorrenciaId,
                StatusFinalizadoId = statusFinalizadoId,
                StatusDDL = BuildStatusDevolucaoRecebimentoDDL(devolucao.StatusId),
                OcorrenciaDDL = ocorrenciaDDL,
                Itens = devolucaoItens.Select(x => new DevolucaoRecebimentoItemViewModel
                {
                    DevolucaoItemId = x.Id,
                    ItemNr = x.ItemNr,
                    Descricao = descricaoLookup.ContainsKey(x.ItemNr ?? string.Empty) ? descricaoLookup[x.ItemNr ?? string.Empty] : string.Empty,
                    Quantidade = x.Quantidade ?? 0,
                    StatusId = x.StatusId,
                    StatusNome = x.StatusId.HasValue && statusLookup.ContainsKey(x.StatusId.Value) ? statusLookup[x.StatusId.Value] : string.Empty,
                    OcorrenciaId = x.OcorrenciaId,
                    Observacao = x.Observacao,
                    OcorrenciaInformada = x.QtdeOcorrencia,
                    ObservacaoOcorrencia = x.Observacao
                }).ToList()
            };

            return vm;
        }

        private IEnumerable<SelectListItem> BuildStatusDevolucaoRecebimentoDDL(int? selectedStatusId)
        {
            Dictionary<int, string> allowedStatuses = GetStatusDevolucaoRecebimentoLookup();
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

        private IEnumerable<SelectListItem> BuildOcorrenciaDDL()
        {
            List<SelectListItem> ocorrencias = db.Ocorrencia
                .AsNoTracking()
                .OrderBy(x => x.Nome)
                .Select(x => new SelectListItem
                {
                    Value = x.Id.ToString(),
                    Text = x.Nome
                })
                .ToList();

            ocorrencias.Insert(0, new SelectListItem
            {
                Value = string.Empty,
                Text = "-- Selecione --"
            });

            return ocorrencias;
        }

        private Dictionary<int, string> GetStatusDevolucaoRecebimentoLookup()
        {
            return db.StatusDevolucao
                .AsNoTracking()
                .OrderBy(x => x.Id)
                .ToList()
                .Where(x => x.Id == 2 || x.Id == 3)
                .GroupBy(x => x.Id)
                .ToDictionary(x => x.Key, x => x.Select(y => y.Nome ?? string.Empty).FirstOrDefault() ?? string.Empty);
        }

        private int ResolveStatusDevolucaoRecebimentoFinalizadoId()
        {
            Dictionary<int, string> allowedStatuses = GetStatusDevolucaoRecebimentoLookup();
            int? finalizadoId = ResolveStatusDevolucaoIdByName("Finalizado");
            if (finalizadoId.HasValue && allowedStatuses.ContainsKey(finalizadoId.Value))
            {
                return finalizadoId.Value;
            }

            return allowedStatuses.Keys.FirstOrDefault(x => x != 3);
        }

        private int? ResolveStatusDevolucaoIdByName(string statusName)
        {
            string statusNormalizado = NormalizeStatusName(statusName);
            return db.StatusDevolucao
                .AsNoTracking()
                .OrderBy(x => x.Id)
                .ToList()
                .Where(x => NormalizeStatusName(x.Nome) == statusNormalizado)
                .Select(x => (int?)x.Id)
                .FirstOrDefault();
        }

        private bool TryResolveNotaFiscalStatusByDevolucaoStatus(int devolucaoStatusId, out int notaFiscalStatusId, out int? notaFiscalItemStatusId)
        {
            notaFiscalStatusId = 0;
            notaFiscalItemStatusId = null;

            if (devolucaoStatusId == 2)
            {
                notaFiscalStatusId = 4;
                return true;
            }

            return false;
        }

        private void MergeDevolucaoRecebimentoInput(DevolucaoRecebimentoViewModel target, DevolucaoRecebimentoViewModel source)
        {
            if (target == null || source == null)
            {
                return;
            }

            target.StatusId = source.StatusId;
            target.StatusDDL = BuildStatusDevolucaoRecebimentoDDL(source.StatusId);
            target.OcorrenciaDDL = BuildOcorrenciaDDL();

            Dictionary<int, DevolucaoRecebimentoItemViewModel> sourceLookup = (source.Itens ?? new List<DevolucaoRecebimentoItemViewModel>())
                .ToDictionary(x => x.DevolucaoItemId, x => x);

            foreach (DevolucaoRecebimentoItemViewModel item in target.Itens ?? new List<DevolucaoRecebimentoItemViewModel>())
            {
                DevolucaoRecebimentoItemViewModel sourceItem;
                if (!sourceLookup.TryGetValue(item.DevolucaoItemId, out sourceItem))
                {
                    continue;
                }

                item.OcorrenciaInformada = sourceItem.OcorrenciaInformada;
                item.OcorrenciaId = sourceItem.OcorrenciaId;
                item.ObservacaoOcorrencia = sourceItem.ObservacaoOcorrencia;
            }
        }

        private string NormalizeStatusName(string value)
        {
            return Util.RemoverAcentuacao((value ?? string.Empty).Trim()).ToUpperInvariant();
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

            string nomeArquivoProcessado = Path.GetFileName(arquivo.FileName) ?? string.Empty;
            if (nomeArquivoProcessado.Length > 100)
            {
                nomeArquivoProcessado = nomeArquivoProcessado.Substring(0, 100);
            }

            // A tabela de estagio e compartilhada por filial. A trava no banco impede que
            // dois uploads simultaneos apaguem ou misturem os dados um do outro.
            using (DbContextTransaction uploadTransaction = db.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                int lockResult = db.Database.SqlQuery<int>(@"
DECLARE @Result int;
EXEC @Result = sys.sp_getapplock
    @Resource = @p0,
    @LockMode = 'Exclusive',
    @LockOwner = 'Transaction',
    @LockTimeout = 60000;
SELECT @Result;", "Recebimento.UploadFileTransito.Filial." + filialId).Single();

                if (lockResult < 0)
                {
                    return Json(new
                    {
                        erro = true,
                        mensagem = "Outro upload de recebimento esta em andamento para esta filial. Tente novamente."
                    }, JsonRequestBehavior.AllowGet);
                }

            // DELETE tabela TransitoUpload
            try
            {
                db.Database.ExecuteSqlCommand("DELETE FROM dbo.TransitoUpload WHERE FilialId = @p0", filialId);
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
                db.Database.ExecuteSqlCommand("DELETE FROM dbo.TransitoUploadColumns WHERE FilialId = @p0", filialId);
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
                var dbConn = (SqlConnection)db.Database.Connection;

                dt.Columns.Add(new DataColumn("FilialId"));

                while ((line = reader.ReadLine()) != null)
                {
                    dt.Rows.Add(line, Util.GetCurrentFilial());                    
                }

                var bullCopy = new SqlBulkCopy(
                    dbConn,
                    SqlBulkCopyOptions.TableLock,
                    (SqlTransaction)uploadTransaction.UnderlyingTransaction)
                {
                    DestinationTableName = "TransitoUpload",
                    BatchSize = dt.Rows.Count
                };

                bullCopy.WriteToServer(dt);
                bullCopy.Close();

                rows = dt.Rows.Count;

            }
            catch (Exception ex)
            {
                msg = "[TransitoUpload] SqlBulkCopy failed<br>" + ex.Message;
                return Json(new { erro = true, mensagem = msg }, JsonRequestBehavior.AllowGet);
            }

            // INSERT tabela TransitoUploadColumns. Os campos de preço unitário e
            // imposto são posições fixas do registro DNI do arquivo Trânsito GM.
            // O valor vem sem separador decimal e possui duas casas implícitas.
            string sql;
            try
            {
                db.Database.ExecuteSqlCommand(@"
INSERT INTO dbo.TransitoUploadColumns
(
    RecordType, NotaFiscal, Origem, Emissao, Volume, Item, Pedido,
    Quantidade, PrecoUnitario, Imposto, Dtatual, FilialId
)
SELECT
    SUBSTRING(Linha, 1, 3),
    CASE SUBSTRING(Linha, 1, 3) WHEN 'DNC' THEN LTRIM(RTRIM(SUBSTRING(Linha, 24, 9))) ELSE '' END,
    CASE SUBSTRING(Linha, 1, 3) WHEN 'DNC' THEN LTRIM(RTRIM(SUBSTRING(Linha, 115, 4))) ELSE '' END,
    CASE SUBSTRING(Linha, 1, 3) WHEN 'DNC' THEN LTRIM(RTRIM(SUBSTRING(Linha, 37, 8))) ELSE '' END,
    CASE SUBSTRING(Linha, 1, 3) WHEN 'DNI' THEN LTRIM(RTRIM(SUBSTRING(Linha, 62, 10))) ELSE '' END,
    CASE SUBSTRING(Linha, 1, 3) WHEN 'DNI' THEN LTRIM(RTRIM(SUBSTRING(Linha, 4, 8))) ELSE '' END,
    CASE SUBSTRING(Linha, 1, 3) WHEN 'DNI' THEN LTRIM(RTRIM(SUBSTRING(Linha, 15, 6))) ELSE '' END,
    CASE SUBSTRING(Linha, 1, 3) WHEN 'DNI' THEN TRY_CONVERT(int, NULLIF(LTRIM(RTRIM(SUBSTRING(Linha, 22, 5))), '')) ELSE NULL END,
    CASE SUBSTRING(Linha, 1, 3) WHEN 'DNI' THEN TRY_CONVERT(decimal(18,2), NULLIF(SUBSTRING(Linha, 27, 13), '')) / 100 ELSE NULL END,
    CASE SUBSTRING(Linha, 1, 3) WHEN 'DNI' THEN TRY_CONVERT(decimal(18,2), NULLIF(SUBSTRING(Linha, 40, 11), '')) / 100 ELSE NULL END,
    @p1,
    @p0
FROM dbo.TransitoUpload
WHERE SUBSTRING(Linha, 1, 3) IN ('DNC', 'DNI')
  AND FilialId = @p0;",
                    filialId,
                    Util.GetCurrentDateTime());
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                msg = "[TransitoUploadColumns] INSERT failed<br>" + ex.Message;
                return Json(new { erro = true, mensagem = msg }, JsonRequestBehavior.AllowGet);
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
                    msg = "[Material] INSERT failed<br>" + ex.Message;
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

                    // Uma NF do arquivo de trânsito é sempre GM. O MERGE legado pode
                    // localizar uma NF já existente sem corrigir o TipoId, fazendo a
                    // consulta da aba GM ocultá-la depois da importação.
                    db.Database.ExecuteSqlCommand(@"
UPDATE NF
SET
    TipoId = 4,
    Emissor = NULLIF(LTRIM(RTRIM(Transito.Origem)), ''),
    Observacoes = CASE
        WHEN LTRIM(RTRIM(ISNULL(NF.Observacoes, ''))) =
             LTRIM(RTRIM(ISNULL(Transito.Origem, '')))
        THEN NULL
        ELSE NF.Observacoes
    END,
    ModificadoPor = @p1,
    ModificadoEm = @p2
FROM NotaFiscal AS NF
INNER JOIN
(
    SELECT
        LTRIM(RTRIM(NotaFiscal)) AS Numero,
        MAX(NULLIF(LTRIM(RTRIM(Origem)), '')) AS Origem
    FROM TransitoUploadColumns
    WHERE FilialId = @p0
      AND RecordType = 'DNC'
      AND NULLIF(LTRIM(RTRIM(NotaFiscal)), '') IS NOT NULL
    GROUP BY LTRIM(RTRIM(NotaFiscal))
) Transito
    ON Transito.Numero = NF.Numero
WHERE NF.FilialId = @p0
  AND
  (
      ISNULL(NF.TipoId, 0) <> 4
      OR ISNULL(LTRIM(RTRIM(NF.Emissor)), '') <> ISNULL(Transito.Origem, '')
      OR LTRIM(RTRIM(ISNULL(NF.Observacoes, ''))) = ISNULL(Transito.Origem, '')
  );",
                        filialId,
                        Util.GetCurrentUser(),
                        Util.GetCurrentDateTime());
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

                    db.Database.ExecuteSqlCommand(@"
;WITH ValoresAgrupados AS
(
    SELECT
        NF.Id AS NotaFiscalId,
        LTRIM(RTRIM(T.Item)) AS Item,
        T.Volume,
        T.Pedido,
        T.FilialId,
        MAX(T.PrecoUnitario) AS PrecoUnitario,
        MAX(T.Imposto) AS Imposto
    FROM dbo.NotaFiscal NF
    INNER JOIN dbo.TransitoUploadColumns T
        ON NF.Numero = T.NotaFiscal
       AND NF.FilialId = T.FilialId
    WHERE T.RecordType = 'DNI'
      AND T.FilialId = @p0
      AND NULLIF(LTRIM(RTRIM(T.Item)), '') IS NOT NULL
    GROUP BY NF.Id, LTRIM(RTRIM(T.Item)), T.Volume, T.Pedido, T.FilialId
)
UPDATE NFI
   SET NFI.PrecoUnitario = Origem.PrecoUnitario,
       NFI.Imposto = Origem.Imposto,
       NFI.ModificadoEm = @p1,
       NFI.ModificadoPor = @p2
FROM dbo.NotaFiscalItem NFI
INNER JOIN ValoresAgrupados Origem
        ON NFI.NotaFiscalId = Origem.NotaFiscalId
       AND NFI.Item = Origem.Item
       AND ISNULL(NFI.Volume, '') = ISNULL(Origem.Volume, '')
       AND ISNULL(NFI.Pedido, '') = ISNULL(Origem.Pedido, '')
       AND NFI.FilialId = Origem.FilialId;",
                        filialId,
                        Util.GetCurrentDateTime(),
                        Util.GetCurrentUser());
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    msg = "[NotaFiscalItem] INSERT (MERGE) failed<br>" + ex.Message;
                    return Json(new { erro = true, mensagem = msg }, JsonRequestBehavior.AllowGet);
                }
            }


            msg = "Arquivo importado com sucesso";

            AppConfig ultimoArquivoConfig = db.AppConfig
                .Where(x => x.Nome == UltimoArquivoTransitoConfigName && x.FilialId == filialId)
                .OrderByDescending(x => x.Id)
                .FirstOrDefault();

            if (ultimoArquivoConfig == null)
            {
                db.AppConfig.Add(new AppConfig
                {
                    Nome = UltimoArquivoTransitoConfigName,
                    Descricao = "Nome do ultimo arquivo de transito processado no recebimento.",
                    Valor = nomeArquivoProcessado,
                    CriadoPor = Util.GetCurrentUser(),
                    CriadoEm = Util.GetCurrentDateTime(),
                    FilialId = filialId
                });
            }
            else
            {
                ultimoArquivoConfig.Valor = nomeArquivoProcessado;
                ultimoArquivoConfig.ModificadoPor = Util.GetCurrentUser();
                ultimoArquivoConfig.ModificadoEm = Util.GetCurrentDateTime();
            }

            db.SaveChanges();
            uploadTransaction.Commit();
            return Json(new
            {
                erro = false,
                mensagem = msg,
                qtd_linhas = rows,
                atualizado_em = Util.GetCurrentDateTime().ToString("dd/MM/yyyy HH:mm:ss"),
                arquivo_processado = nomeArquivoProcessado
            }, JsonRequestBehavior.AllowGet);
            }
        }

        // GET: ConferenciaVolume
        public ActionResult ConferenciaVolume()
        {
            ViewBag.Pendente = db.Volume.Where(x => x.StatusId == 1 && x.FilialId == filialId).Select(x => x.VolumeNr).Distinct().Count();
            ViewBag.Conferido = db.Volume.Where(x => x.StatusId == 2 && x.FilialId == filialId).Select(x => x.VolumeNr).Distinct().Count();
            ViewBag.Incorreto = db.Volume.Where(x => x.StatusId == 3 && x.FilialId == filialId).Select(x => x.VolumeNr).Distinct().Count();
            ViewBag.Total = db.Volume.Where(x => x.StatusId != 3 && x.FilialId == filialId).Select(x => x.VolumeNr).Distinct().Count();

            ViewBag.AreaDDL = (from b in db.Area
                               where b.TipoAreaId == 4 // Recebimento
                               && b.FilialId == filialId
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
        [HttpPost]
        public ActionResult GetDataVolume()
        {
            return GetDataVolumeServerSide(false);
        }

        // GET: ConferenciaVolumeDANFE       
        public ActionResult ConferenciaVolumeDANFE()
        {
 
            ViewBag.AreaDDL = (from b in db.Area
                               where b.TipoAreaId == 4 // Recebimento
                               && b.FilialId == filialId
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
        [HttpPost]
        public ActionResult GetDataVolumeDANFE()
        {
            return GetDataVolumeServerSide(true);
        }

        private ActionResult GetDataVolumeServerSide(bool ocultarIncorretosSemFiltro)
        {
            DataTableAjaxPostModel model;
            using (var reader = new StreamReader(Request.InputStream))
            {
                model = JsonConvert.DeserializeObject<DataTableAjaxPostModel>(reader.ReadToEnd());
            }

            if (model == null || !model.areaId.HasValue)
            {
                return Json(new { draw = model == null ? 0 : model.draw, recordsFiltered = 0, recordsTotal = 0, data = new object[0], totalCount = 0, pendenteCount = 0, conferidoCount = 0, incorretoCount = 0 });
            }

            int areaId = model.areaId.Value;
            int statusId = model.statusId ?? 0;
            var linhas = db.Volume.AsNoTracking().Where(v => v.AreaId == areaId && v.FilialId == filialId);
            var agrupadosBase = linhas.GroupBy(v => v.VolumeNr).Select(g => new VolumeViewModel
            {
                AreaId = areaId,
                FilialId = filialId,
                VolumeNr = g.Key,
                QtdeItens = g.Sum(x => x.QtdItens),
                StatusId = g.Any(x => x.StatusId == 3) ? 3
                    : g.Any(x => x.StatusId == 1) ? 1
                    : g.Any(x => x.StatusId == 2) ? 2
                    : g.Min(x => x.StatusId),
                CriadoEm = g.Max(x => x.CriadoEm)
            });

            int totalCount = agrupadosBase.Count(x => x.StatusId != 3);
            int pendenteCount = agrupadosBase.Count(x => x.StatusId == 1);
            int conferidoCount = agrupadosBase.Count(x => x.StatusId == 2);
            int incorretoCount = agrupadosBase.Count(x => x.StatusId == 3);

            var query = statusId != 0
                ? agrupadosBase.Where(x => x.StatusId == statusId)
                : ocultarIncorretosSemFiltro
                    ? agrupadosBase.Where(x => x.StatusId != 3)
                    : agrupadosBase;

            int recordsTotal = query.Count();
            string termo = model.search == null ? string.Empty : (model.search.value ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(termo))
            {
                query = query.Where(x =>
                    (x.VolumeNr ?? string.Empty).Contains(termo) ||
                    linhas.Any(v => v.VolumeNr == x.VolumeNr && (v.NotaFiscalNr ?? string.Empty).Contains(termo)));
            }

            int recordsFiltered = query.Count();
            int sortIndex = model.order != null && model.order.Length > 0 ? model.order[0].column : -1;
            string sortField = sortIndex >= 0 && model.columns != null && sortIndex < model.columns.Length ? model.columns[sortIndex].data : string.Empty;
            bool desc = model.order != null && model.order.Length > 0 && model.order[0].dir == "desc";
            switch (sortField)
            {
                case "VolumeNr": query = desc ? query.OrderByDescending(x => x.VolumeNr) : query.OrderBy(x => x.VolumeNr); break;
                case "QtdeItens": query = desc ? query.OrderByDescending(x => x.QtdeItens) : query.OrderBy(x => x.QtdeItens); break;
                case "StatusNome": query = desc ? query.OrderByDescending(x => x.StatusId) : query.OrderBy(x => x.StatusId); break;
                case "CriadoEmTexto": query = desc ? query.OrderByDescending(x => x.CriadoEm) : query.OrderBy(x => x.CriadoEm); break;
                default: query = query.OrderByDescending(x => x.CriadoEm).ThenBy(x => x.VolumeNr); break;
            }

            int length = model.length > 0 ? model.length : 25;
            List<VolumeViewModel> volumes = query.Skip(model.start).Take(length).ToList();
            List<string> volumesPagina = volumes.Select(x => x.VolumeNr).ToList();
            var notasPorVolume = (from item in db.NotaFiscalItem.AsNoTracking()
                                  join nota in db.NotaFiscal.AsNoTracking() on item.NotaFiscalId equals nota.Id
                                  where item.FilialId == filialId
                                     && nota.FilialId == filialId
                                     && item.Volume != null
                                     && volumesPagina.Contains(item.Volume.Trim())
                                  select new { VolumeNr = item.Volume.Trim(), NotaFiscalNr = nota.Numero })
                .Distinct()
                .ToList()
                .GroupBy(x => x.VolumeNr)
                .ToDictionary(x => x.Key, x => string.Join(" / ", x.Select(n => n.NotaFiscalNr)));
            var statusNomes = db.StatusVolume.AsNoTracking().ToDictionary(x => x.Id, x => x.Nome);

            foreach (var volume in volumes)
            {
                volume.NotaFiscalNr = notasPorVolume.ContainsKey(volume.VolumeNr) ? notasPorVolume[volume.VolumeNr] : string.Empty;
                volume.StatusNome = statusNomes.ContainsKey(volume.StatusId) ? statusNomes[volume.StatusId] : string.Empty;
                volume.CriadoEmTexto = volume.CriadoEm.HasValue ? volume.CriadoEm.Value.ToString("dd/MM/yyyy HH:mm:ss") : string.Empty;
            }

            JsonResult result = Json(new { draw = model.draw, recordsFiltered, recordsTotal, data = volumes, totalCount, pendenteCount, conferidoCount, incorretoCount });
            result.MaxJsonLength = int.MaxValue;
            return result;
        }

        private int ResolveGroupedVolumeStatusId(IEnumerable<VolumeViewModel> volumes)
        {
            List<int> statuses = volumes
                .Select(x => x.StatusId)
                .Distinct()
                .ToList();

            if (statuses.Contains(3))
            {
                return 3;
            }

            if (statuses.Contains(1))
            {
                return 1;
            }

            if (statuses.Contains(2))
            {
                return 2;
            }

            return statuses.FirstOrDefault();
        }

        private string ResolveStatusVolumeNome(int statusId)
        {
            return db.StatusVolume
                .Where(x => x.Id == statusId)
                .Select(x => x.Nome)
                .FirstOrDefault() ?? string.Empty;
        }

        // POST: AddVolume
        [HttpPost]
        public ActionResult AddVolume(string danfe, int areaid)
        {

            string nf = danfe.Substring(25, 9);

            int IdNF = (from x in db.NotaFiscal
                        where x.Numero == nf && x.FilialId == filialId
                        select x.Id).FirstOrDefault();

                if (IdNF == 0)
            {
                return Json(new { msg = "A Nota Fiscal não cadastrada!", erro = true });
            }

                if (db.Volume.Any(x => x.Danfe == danfe && x.FilialId == filialId)
                    || db.NotaFiscal.Any(x => x.Id == IdNF && x.FilialId == filialId && x.StatusId != 1))
            {
                return Json(new { msg = "A Nota Fiscal já processada!", erro = true });
            }

            using (DbContextTransaction tr = db.Database.BeginTransaction(IsolationLevel.Serializable))
            {

                try
                {

                    var transito = (from item in db.NotaFiscalItem
                                    where item.NotaFiscalId == IdNF && item.FilialId == filialId
                                    group item by item.Volume into vol
                                    select new
                                    {
                                        VolumeNr = vol.Key,
                                        IdNF = IdNF,
                                        QtdeItens = vol.Count()
                    }).OrderBy(x => x.VolumeNr).ToList();                 

                    foreach (var item in transito)
                    {
                        string volumeNr = (item.VolumeNr ?? string.Empty).Trim();
                        int quantidadeItensVolume = db.NotaFiscalItem.Count(x =>
                            x.FilialId == filialId
                            && (x.Volume ?? string.Empty).Trim() == volumeNr);

                        Volume volume = db.Volume.FirstOrDefault(x =>
                            x.FilialId == filialId
                            && (x.VolumeNr ?? string.Empty).Trim() == volumeNr);

                        if (volume == null)
                        {
                            volume = new Volume
                            {
                                AreaId = areaid,
                                NotaFiscalNr = nf,
                                VolumeNr = volumeNr,
                                StatusId = 1,
                                QtdItens = quantidadeItensVolume,
                                Imprimir = false,
                                Danfe = danfe,
                                FilialId = filialId,
                                CriadoEm = Util.GetCurrentDateTime(),
                                CriadoPor = Util.GetCurrentUser()
                            };

                            db.Volume.Add(volume);
                        }
                        else
                        {
                            volume.AreaId = areaid;
                            volume.StatusId = 1;
                            volume.QtdItens = quantidadeItensVolume;
                            volume.ModificadoEm = Util.GetCurrentDateTime();
                            volume.ModificadoPor = Util.GetCurrentUser();
                        }

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
        [ValidateAntiForgeryToken]
        public ActionResult GetConferenciaVolumeItens(string volume, int area)
        {
            string volumeNormalizado = (volume ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(volumeNormalizado))
            {
                return Json(new { erro = true, msg = "Volume não informado." });
            }

            if (filialId <= 0)
            {
                return Json(new { erro = true, msg = "Filial não identificada na sessão atual." });
            }

            bool areaValida = db.Area.Any(x =>
                x.Id == area &&
                x.TipoAreaId == 4 &&
                x.FilialId == filialId);
            if (!areaValida)
            {
                return Json(new { erro = true, msg = "Área de recebimento inválida para a filial atual." });
            }

            bool volumeValido = db.Volume.Any(x =>
                (x.VolumeNr ?? string.Empty).Trim() == volumeNormalizado
                && x.AreaId == area
                && x.FilialId == filialId
                && x.StatusId != 3);

            if (!volumeValido)
            {
                if (!db.Volume.Any(x => x.VolumeNr == volumeNormalizado
                    && x.AreaId == area
                    && x.FilialId == filialId
                    && x.StatusId == 3))
                {
                    db.Volume.Add(BuildIncorrectVolume(volumeNormalizado, area));
                    db.SaveChanges();
                }

                return Json(new
                {
                    erro = true,
                    notfound = true,
                    msg = "Volume incorreto!",
                    contadores = GetVolumeCounters(area)
                });
            }

            string usuarioAtual = Util.GetCurrentUser();
            bool administrador = Util.IsAdminProfile() || Util.IsAdminUser();

            var itens = (from item in db.NotaFiscalItem.AsNoTracking()
                         join nota in db.NotaFiscal.AsNoTracking() on item.NotaFiscalId equals nota.Id
                         join materialBase in db.Material.AsNoTracking() on item.Item equals materialBase.Codigo into materiais
                         from material in materiais.DefaultIfEmpty()
                         where item.FilialId == filialId
                            && nota.FilialId == filialId
                            && (item.Volume ?? string.Empty).Trim() == volumeNormalizado
                         orderby nota.Numero, item.Item, item.Id
                         select new
                         {
                             id = item.Id,
                             notaFiscal = nota.Numero,
                             item = item.Item,
                             itemCritico = material != null && material.ItemCritico,
                             observacaoItemCritico = material != null && material.ItemCritico
                                ? material.ObsItemCritico
                                : null,
                             pedido = item.Pedido,
                             quantidade = item.Quantidade,
                             qtdConferida = item.QtdConferida,
                             qtdArmazenada = item.QtdArmazenada,
                             diferenca = item.QtdConferida.HasValue
                                ? item.QtdConferida.Value - item.Quantidade
                                : (decimal?)null,
                             conferido = item.Conferido,
                             situacao = !item.Conferido
                                ? "Pendente"
                                : !item.QtdConferida.HasValue || item.QtdConferida.Value == item.Quantidade
                                    ? "Conferido"
                                    : item.QtdConferida.Value < item.Quantidade
                                        ? "Conferido a menor"
                                        : "Conferido a maior",
                             usuarioConferencia = item.UsuarioConferencia,
                             dtHrConferencia = item.DtHrConferencia.HasValue ? item.DtHrConferencia.Value.ToString("dd/MM/yyyy HH:mm:ss") : string.Empty,
                             usuarioArmazenagem = item.UsuarioArmazenagem,
                             dtHrArmazenagem = item.DtHrArmazenagem.HasValue ? item.DtHrArmazenagem.Value.ToString("dd/MM/yyyy HH:mm:ss") : string.Empty,
                             modificadoEm = item.ModificadoEm.HasValue ? item.ModificadoEm.Value.ToString("o") : string.Empty,
                             podeEditar = administrador
                                || !item.Conferido
                                || item.UsuarioConferencia == usuarioAtual
                         }).ToList();

            if (itens.Count == 0)
            {
                return Json(new { erro = true, msg = "Nenhum item foi localizado para o volume e a filial informados." });
            }

            return Json(new
            {
                erro = false,
                volume = volumeNormalizado,
                itens,
                contadores = GetVolumeCounters(area)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ConfirmarConferenciaVolumeItem(
            int id,
            string volume,
            int area,
            decimal? qtdConferida,
            bool conferido,
            bool confirmarDivergencia,
            DateTime? modificadoEmEsperado)
        {
            if (!conferido)
            {
                return Json(new { erro = true, msg = "Marque o campo Conferido antes de finalizar." });
            }

            if (!qtdConferida.HasValue)
            {
                return Json(new { erro = true, msg = "Informe a quantidade conferida." });
            }

            if (qtdConferida.Value < 0)
            {
                return Json(new { erro = true, msg = "A quantidade conferida não pode ser negativa." });
            }

            string volumeNormalizado = (volume ?? string.Empty).Trim();
            string usuarioAtual = Util.GetCurrentUser();
            bool administrador = Util.IsAdminProfile() || Util.IsAdminUser();

            using (DbContextTransaction tr = db.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                try
                {
                    NotaFiscalItem item = (from candidato in db.NotaFiscalItem
                                           join nota in db.NotaFiscal on candidato.NotaFiscalId equals nota.Id
                                           where candidato.Id == id
                                              && candidato.FilialId == filialId
                                              && nota.FilialId == filialId
                                              && (candidato.Volume ?? string.Empty).Trim() == volumeNormalizado
                                              && db.Volume.Any(v => (v.VolumeNr ?? string.Empty).Trim() == volumeNormalizado
                                                  && v.AreaId == area
                                                  && v.FilialId == filialId
                                                  && v.StatusId != 3)
                                           select candidato).SingleOrDefault();

                    if (item == null)
                    {
                        return Json(new { erro = true, msg = "Item ou volume não localizado para a filial atual." });
                    }

                    if (item.ModificadoEm != modificadoEmEsperado)
                    {
                        return Json(new
                        {
                            erro = true,
                            concorrencia = true,
                            msg = "O item foi alterado durante a operação. Recarregue o volume antes de continuar."
                        });
                    }

                    if (item.Conferido
                        && !administrador
                        && !string.Equals(item.UsuarioConferencia, usuarioAtual, StringComparison.OrdinalIgnoreCase))
                    {
                        return Json(new
                        {
                            erro = true,
                            concorrencia = true,
                            msg = "O item já foi conferido por " + (item.UsuarioConferencia ?? "outro usuário") + "."
                        });
                    }

                    decimal diferenca = qtdConferida.Value - item.Quantidade;
                    if (diferenca != 0 && !confirmarDivergencia)
                    {
                        return Json(new
                        {
                            erro = true,
                            msg = "A divergência deve ser confirmada explicitamente antes da finalização."
                        });
                    }

                    DateTime agora = Util.GetCurrentDateTime();
                    item.QtdConferida = qtdConferida.Value;
                    item.Conferido = true;
                    item.UsuarioConferencia = usuarioAtual;
                    item.DtHrConferencia = agora;
                    item.ModificadoPor = usuarioAtual;
                    item.ModificadoEm = agora;
                    db.SaveChanges();

                    List<NotaFiscalItem> itensVolume = (from candidato in db.NotaFiscalItem
                                                        join nota in db.NotaFiscal on candidato.NotaFiscalId equals nota.Id
                                                        where candidato.FilialId == filialId
                                                           && nota.FilialId == filialId
                                                           && (candidato.Volume ?? string.Empty).Trim() == volumeNormalizado
                                                        select candidato).ToList();

                    bool volumeFinalizado = itensVolume.Count > 0 && itensVolume.All(x => x.Conferido);
                    if (volumeFinalizado)
                    {
                        List<Volume> volumes = db.Volume
                            .Where(x => x.VolumeNr == volumeNormalizado
                                && x.AreaId == area
                                && x.FilialId == filialId
                                && x.StatusId != 3)
                            .ToList();

                        foreach (Volume registroVolume in volumes)
                        {
                            registroVolume.StatusId = 2;
                            registroVolume.ModificadoPor = usuarioAtual;
                            registroVolume.ModificadoEm = agora;
                        }

                        Area areaRecebimento = db.Area.First(x => x.Id == area && x.FilialId == filialId);
                        int statusArea = areaRecebimento.Etiqueta == true ? 3 : 7;
                        foreach (NotaFiscalItem itemVolume in itensVolume)
                        {
                            itemVolume.StatusId = statusArea;
                        }

                        List<int> notasIds = itensVolume.Select(x => x.NotaFiscalId).Distinct().ToList();
                        foreach (int notaId in notasIds)
                        {
                            bool todosItensConferidos = db.NotaFiscalItem
                                .Where(x => x.NotaFiscalId == notaId && x.FilialId == filialId)
                                .All(x => x.Conferido);

                            if (todosItensConferidos)
                            {
                                NotaFiscal nota = db.NotaFiscal.FirstOrDefault(x => x.Id == notaId && x.FilialId == filialId);
                                if (nota != null)
                                {
                                    nota.StatusId = 7;
                                    nota.ModificadoPor = usuarioAtual;
                                    nota.ModificadoEm = agora;
                                }
                            }
                        }

                        db.SaveChanges();
                    }

                    tr.Commit();

                    return Json(new
                    {
                        erro = false,
                        item = new
                        {
                            id = item.Id,
                            qtdConferida = item.QtdConferida,
                            qtdArmazenada = item.QtdArmazenada,
                            diferenca,
                            conferido = item.Conferido,
                            situacao = diferenca == 0 ? "Conferido" : diferenca < 0 ? "Conferido a menor" : "Conferido a maior",
                            usuarioConferencia = item.UsuarioConferencia,
                            dtHrConferencia = item.DtHrConferencia.HasValue ? item.DtHrConferencia.Value.ToString("dd/MM/yyyy HH:mm:ss") : string.Empty,
                            usuarioArmazenagem = item.UsuarioArmazenagem,
                            dtHrArmazenagem = item.DtHrArmazenagem.HasValue ? item.DtHrArmazenagem.Value.ToString("dd/MM/yyyy HH:mm:ss") : string.Empty,
                            modificadoEm = item.ModificadoEm.HasValue ? item.ModificadoEm.Value.ToString("o") : string.Empty,
                            podeEditar = true
                        },
                        volumeFinalizado,
                        contadores = GetVolumeCounters(area)
                    });
                }
                catch (Exception ex)
                {
                    tr.Rollback();
                    return Json(new { erro = true, msg = ex.Message });
                }
            }
        }

        private object GetVolumeCounters(int area)
        {
            var volumes = db.Volume
                .Where(x => x.AreaId == area && x.FilialId == filialId)
                .Select(x => new { x.VolumeNr, x.StatusId })
                .ToList()
                .GroupBy(x => x.VolumeNr)
                .Select(grupo => new
                {
                    StatusId = grupo.Any(x => x.StatusId == 3) ? 3
                        : grupo.Any(x => x.StatusId == 1) ? 1
                        : grupo.Any(x => x.StatusId == 2) ? 2
                        : grupo.Select(x => x.StatusId).FirstOrDefault()
                });

            return new
            {
                total = volumes.Count(x => x.StatusId != 3),
                pendentes = volumes.Count(x => x.StatusId == 1),
                conferidos = volumes.Count(x => x.StatusId == 2),
                incorretos = volumes.Count(x => x.StatusId == 3)
            };
        }

        [HttpPost]
        public ActionResult UpdateVolume(string volume, int area)
        {
            int rows;
            int rows_pendente;
            int rows_conferido;
            int rows_incorreto;

            if (filialId <= 0)
            {
                return Json(new { msg = "Filial não identificada na sessão atual.", erro = true, notfound = false });
            }

            if (!db.Area.Any(x =>
                x.Id == area &&
                x.TipoAreaId == 4 &&
                x.FilialId == filialId))
            {
                return Json(new { msg = "Área de recebimento inválida para a filial atual.", erro = true, notfound = false });
            }

            // Grava volume incorreto (??)
            int qtdevolume = db.Volume.Count(x => x.VolumeNr == volume && x.AreaId == area && x.FilialId == filialId);
            if (qtdevolume == 0)
            {
                db.Volume.Add(BuildIncorrectVolume((volume ?? string.Empty).Trim(), area));
                db.SaveChanges();

                rows = db.Volume.Where(x => x.StatusId != 3 && x.AreaId == area && x.FilialId == filialId).Select(x => x.VolumeNr).Distinct().Count();
                rows_pendente = db.Volume.Where(x => x.StatusId == 1 && x.AreaId == area && x.FilialId == filialId).Select(x => x.VolumeNr).Distinct().Count();
                rows_conferido = db.Volume.Where(x => x.StatusId == 2 && x.AreaId == area && x.FilialId == filialId).Select(x => x.VolumeNr).Distinct().Count();
                rows_incorreto = db.Volume.Where(x => x.StatusId == 3 && x.AreaId == area && x.FilialId == filialId).Select(x => x.VolumeNr).Distinct().Count();

                return Json(new { msg = "Volume Incorreto!", erro = true, notfound = true, total = rows, pendentes = rows_pendente, conferidos = rows_conferido, incorretos = rows_incorreto });

            }

            List<NotaFiscalItem> itensConferencia = (from item in db.NotaFiscalItem
                                                     join nota in db.NotaFiscal on item.NotaFiscalId equals nota.Id
                                                     where item.FilialId == filialId
                                                        && nota.FilialId == filialId
                                                        && (item.Volume ?? string.Empty).Trim() == volume
                                                     select item).ToList();

            bool conferenciaIncompleta = itensConferencia.Count == 0
                || itensConferencia.Any(x => !x.Conferido || !x.QtdConferida.HasValue);

            if (conferenciaIncompleta)
            {
                return Json(new
                {
                    msg = "Existem itens sem quantidade conferida. Conclua a conferência dos itens antes de finalizar o volume.",
                    erro = true,
                    notfound = false,
                    contadores = GetVolumeCounters(area)
                });
            }

            using (DbContextTransaction tr = db.Database.BeginTransaction())
            {
                try
                {
                    var volumes = db.Volume.Where(x => x.VolumeNr == volume && x.StatusId == 1 && x.AreaId == area && x.FilialId == filialId).ToList();
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

                        List<NotaFiscalItem> itensDoVolume = (from item in db.NotaFiscalItem
                                                              join nota in db.NotaFiscal on item.NotaFiscalId equals nota.Id
                                                              where item.FilialId == filialId
                                                                 && nota.FilialId == filialId
                                                                 && (item.Volume ?? string.Empty).Trim() == volume
                                                              select item).ToList();

                        foreach (NotaFiscalItem itemDoVolume in itensDoVolume)
                        {
                            itemDoVolume.StatusId = StatusArea;
                        }

                        db.SaveChanges();

                        List<int> notasIds = itensDoVolume.Select(x => x.NotaFiscalId).Distinct().ToList();

                        //Se não houve mais volumes com status < 4 (Em Conferência)
                        //O status da NF será alterado para 7 (Finalizado)
                        //Posteriomente terá outros status, precisaremos avaliar como fazer

                        foreach (int notaId in notasIds)
                        {
                            int Volumes_pendente = db.NotaFiscalItem.Where(x => x.StatusId < 4 && x.NotaFiscalId == notaId && x.FilialId == filialId).Select(x => x.Volume).Distinct().Count();
                            if (Volumes_pendente == 0)
                            {
                                NotaFiscal nota = db.NotaFiscal.FirstOrDefault(x => x.Id == notaId && x.FilialId == filialId);
                                if (nota != null)
                                {
                                    nota.StatusId = 7;
                                }
                            }

                            if (db.Volume.Where(x => x.StatusId == 1 && x.AreaId == area && x.FilialId == filialId).Select(x => x.VolumeNr).Distinct().Count() == 0)
                            {
                                NotaFiscal nota = db.NotaFiscal.FirstOrDefault(x => x.Id == notaId && x.FilialId == filialId);
                                if (nota != null)
                                {
                                    nota.StatusId = 7;
                                }
                            }
                        }

                        db.SaveChanges();
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

            rows = db.Volume.Where(x => x.StatusId != 3 && x.AreaId == area && x.FilialId == filialId).Select(x => x.VolumeNr).Distinct().Count();
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
            vm.PrinterServerPort = (from a in db.AppConfig where a.Nome == "PrinterServerPort" select a.Valor).FirstOrDefault();

            ViewBag.ImpressoraDDL = (from i in db.Impressora where i.FilialId == filialId
                                     orderby (i.Nome == "RECEBIMENTO" ? 1 : 2)
                                     select new SelectListItem
                                     {
                                         Value = i.Id.ToString(),
                                         Text = i.Nome
                                     }).ToList();

            return View(vm);
        }

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

        private Volume BuildIncorrectVolume(string volume, int area)
        {
            return new Volume
            {
                NotaFiscalNr = string.Empty,
                VolumeNr = volume,
                StatusId = 3,
                QtdItens = 0,
                AreaId = area,
                Imprimir = false,
                Danfe = string.Empty,
                FilialId = filialId,
                CriadoPor = Util.GetCurrentUser(),
                CriadoEm = Util.GetCurrentDateTime()
            };
        }

        // Retorna dados para impressão da etiqueta de volume
        public ActionResult GetMateriaisVolumeToPrint(string volume)
        {
            List<string> listaEtiquetas = new List<string>();
            string volumeNormalizado = (volume ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(volumeNormalizado))
            {
                return Json(new { data = listaEtiquetas, success = false, msg = "Informe o número do volume." }, JsonRequestBehavior.AllowGet);
            }

            string template_zpl = (from e in db.Etiqueta
                                   where e.Nome == "Volume" 
                                   select e.ZPL).FirstOrDefault();

            if (string.IsNullOrWhiteSpace(template_zpl))
            {
                return Json(new { data = listaEtiquetas, success = false, msg = "Modelo da etiqueta de volume não encontrado." }, JsonRequestBehavior.AllowGet);
            }

            string zpl;
            try
            {
                var itens = (from nfi in db.NotaFiscalItem
                             join material in db.Material.Where(x => x.FilialId == filialId)
                                 on nfi.Item equals material.Codigo into materiais
                             from m in materiais.DefaultIfEmpty()
                             where nfi.FilialId == filialId
                                && nfi.Volume != null
                                && nfi.Volume.Trim() == volumeNormalizado
                             select new EtiquetaRecebimentoViewModel
                             {
                                 Material = nfi.Item,
                                 Descricao = m != null ? (m.Descricao ?? string.Empty) : string.Empty,
                                 Curva = m != null ? (m.Curva ?? string.Empty) : string.Empty,
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
                    item.Locacao = estoque != null ? (estoque.Locacao ?? string.Empty) : string.Empty;

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

                    string saldo_estoque = estoque != null && estoque.Saldo.HasValue
                        ? estoque.Saldo.Value.ToString()
                        : "0";

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

                }

                JsonResult result = Json(new { data = listaEtiquetas, success = true, msg = string.Empty }, JsonRequestBehavior.AllowGet);
                result.MaxJsonLength = int.MaxValue;
                return result;

            }
            catch (Exception ex)
            {
                return Json(new { data = listaEtiquetas, success = false, msg = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // Retorna dados para impressão da etiqueta de material
        public ActionResult GetMaterialToPrint(string material, int quantidadeImpressao)
        {
            EtiquetaMaterialViewModel result = new EtiquetaMaterialViewModel();
            List<string> listaEtiquetas = new List<string>();
            string materialNormalizado = (material ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(materialNormalizado))
            {
                return Json(new { data = listaEtiquetas, success = false, msg = "Informe o Item Nr." }, JsonRequestBehavior.AllowGet);
            }

            string template_zpl = (from e in db.Etiqueta
                                   where e.Nome == "Material"
                                   select e.ZPL).FirstOrDefault();

            if (string.IsNullOrWhiteSpace(template_zpl))
            {
                return Json(new { data = listaEtiquetas, success = false, msg = "Modelo da etiqueta de material não encontrado." }, JsonRequestBehavior.AllowGet);
            }

            string zpl;

            try
            {
                if (quantidadeImpressao <= 0)
                {
                    return Json(new { data = result, success = false, msg = "A quantidade de impressão precisa ser maior que 0!" }, JsonRequestBehavior.AllowGet);
                }

                string d1, d2, d3;
                d1 = null;
                d2 = null;
                d3 = null;

                var item = (from m in db.Material
                            where m.Codigo == materialNormalizado
                            select m).FirstOrDefault();

                if (item != null)
                {
                    result.Material = item.Codigo;
                    result.Descricao = item.Descricao ?? string.Empty;
                    result.Curva = item.Curva ?? string.Empty;
                    result.Locacao = string.Empty;
                }
                else
                {
                    return Json(new { data = listaEtiquetas, success = false, msg = "Material não localizado." }, JsonRequestBehavior.AllowGet);
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

                string saldo_estoque = estoque != null && estoque.Saldo.HasValue
                    ? estoque.Saldo.Value.ToString()
                    : "0";

                zpl = zpl.Replace("saldo-estoque", saldo_estoque);

                string locAcertada = "";
                string locacao = estoque != null
                    ? (estoque.Locacao ?? string.Empty)
                    : string.Empty;

                if (locacao.Length < 9)
                {
                    d1 = locacao;
                }

                if (locacao.Length == 9)
                {
                    d1 = locacao.Substring(0, 8);
                    d2 = locacao.Substring(8, 1);
                }

                if (locacao.Length == 10)
                {
                    string espaco = locacao.Substring(6, 1);

                    if (espaco != " ")
                    {
                        d1 = locacao.Substring(0, 5);
                        d2 = locacao.Substring(6, 2);
                        d3 = locacao.Substring(8, 2);
                    }
                    else
                    {
                        d1 = locacao.Substring(0, 6);
                        d2 = locacao.Substring(7, 2);
                        d3 = locacao.Substring(9, 1);
                    }
                }

                if (locacao.Length == 11)
                {
                    d1 = locacao.Substring(0, 9);
                    d2 = locacao.Substring(9, 2);
                }

                if (locacao.Length > 11)
                {
                    d1 = locacao;
                }

                if (d3 != null)
                {
                    d3 = " " + d3;
                }

                locAcertada = string.Concat(d1, " ", d2, d3).Trim();

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

                var historico = db.HistoricoRecebimento
                    .AsNoTracking()
                    .Where(h => h.FilialId == filialId && h.DataHora >= inicio);

                var recordsTotal = historico.Count();

                // Filtragem
                if (!string.IsNullOrWhiteSpace(searchValue))
                {
                    string termo = searchValue.Trim();
                    DateTime dataPesquisa;
                    bool pesquisaData = DateTime.TryParseExact(
                        termo,
                        new[] { "dd/MM/yyyy", "dd/MM/yyyy HH:mm", "dd/MM/yyyy HH:mm:ss" },
                        CultureInfo.GetCultureInfo("pt-BR"),
                        DateTimeStyles.None,
                        out dataPesquisa);

                    if (pesquisaData)
                    {
                        bool informouHora = termo.Contains(":");
                        DateTime dataFinal = !informouHora
                            ? dataPesquisa.Date.AddDays(1)
                            : termo.Length == 16
                                ? dataPesquisa.AddMinutes(1)
                                : dataPesquisa.AddSeconds(1);
                        DateTime dataInicial = informouHora ? dataPesquisa : dataPesquisa.Date;

                        historico = historico.Where(h =>
                            (h.CodMaterial ?? string.Empty).Contains(termo) ||
                            (h.DescMaterial ?? string.Empty).Contains(termo) ||
                            (h.Curva ?? string.Empty).Contains(termo) ||
                            (h.CodLocacao ?? string.Empty).Contains(termo) ||
                            (h.NroVolume ?? string.Empty).Contains(termo) ||
                            (h.Usuario ?? string.Empty).Contains(termo) ||
                            (h.DataHora >= dataInicial && h.DataHora < dataFinal));
                    }
                    else
                    {
                        historico = historico.Where(h =>
                            (h.CodMaterial ?? string.Empty).Contains(termo) ||
                            (h.DescMaterial ?? string.Empty).Contains(termo) ||
                            (h.Curva ?? string.Empty).Contains(termo) ||
                            (h.CodLocacao ?? string.Empty).Contains(termo) ||
                            (h.NroVolume ?? string.Empty).Contains(termo) ||
                            (h.Usuario ?? string.Empty).Contains(termo));
                    }
                }

                // O DataTables usa recordsFiltered para calcular a quantidade
                // de paginas e o total exibido depois da pesquisa.
                var recordsFiltered = historico.Count();

                // Ordenação
                switch (sortColumn)
                {
                    case 0:
                        historico = sortColumnDir == "desc" ? historico.OrderByDescending(c => c.CodMaterial) : historico.OrderBy(c => c.CodMaterial);
                        break;
                    case 1:
                        historico = sortColumnDir == "desc" ? historico.OrderByDescending(c => c.DescMaterial) : historico.OrderBy(c => c.DescMaterial);
                        break;
                    case 2:
                        historico = sortColumnDir == "desc" ? historico.OrderByDescending(c => c.Curva) : historico.OrderBy(c => c.Curva);
                        break;
                    case 3:
                        historico = sortColumnDir == "desc" ? historico.OrderByDescending(c => c.CodLocacao) : historico.OrderBy(c => c.CodLocacao);
                        break;
                    case 4:
                        historico = sortColumnDir == "desc" ? historico.OrderByDescending(c => c.NroVolume) : historico.OrderBy(c => c.NroVolume);
                        break;
                    case 5:
                        historico = sortColumnDir == "desc" ? historico.OrderByDescending(c => c.Quantidade) : historico.OrderBy(c => c.Quantidade);
                        break;
                    case 6:
                        historico = sortColumnDir == "desc" ? historico.OrderByDescending(c => c.DataHora) : historico.OrderBy(c => c.DataHora);
                        break;
                    case 7:
                        historico = sortColumnDir == "desc" ? historico.OrderByDescending(c => c.Usuario) : historico.OrderBy(c => c.Usuario);
                        break;
                    default:
                        historico = historico.OrderByDescending(c => c.DataHora);
                        break;
                }

                var filteredData = historico
                    .Skip(start)
                    .Take(length)
                    .Select(h => new HistoricoViewModel
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
                    })
                    .ToList()
                    .Select(h => new
                    {
                        h.Id,
                        h.CodMaterial,
                        h.DescMaterial,
                        h.Curva,
                        h.CodLocacao,
                        h.NroVolume,
                        h.Quantidade,
                        DataHora = h.DataHora.ToString("dd/MM/yyyy HH:mm:ss"),
                        h.Usuario
                    })
                    .ToList();
                var result = new { draw = draw, recordsFiltered, recordsTotal, data = filteredData };

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
