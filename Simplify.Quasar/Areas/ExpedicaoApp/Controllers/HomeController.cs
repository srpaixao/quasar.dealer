using Simplify.Quasar.Custom;
using Simplify.Quasar.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace Simplify.Quasar.Areas.ExpedicaoApp.Controllers
{
    [ValidateSession]
    public class HomeController : Controller
    {
        private const int StatusFinalizado = 4;
        private const int StatusEmBusca = 6;
        private const int StatusSeparado = 8;
        private const int StatusEmConferencia = 9;

        private readonly Quasar_Entities db = new Quasar_Entities();

        private int filialId
        {
            get { return Util.GetCurrentFilial(); }
        }

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Dashboard(DateTime? dataInicial, DateTime? dataFinal)
        {
            return View(
                "~/Views/Shared/ProcessDashboard.cshtml",
                ProcessDashboardViewModel.Create("Expedi\u00E7\u00E3o", "ExpedicaoApp", dataInicial, dataFinal));
        }

        public ActionResult ConferenciaRomaneios()
        {
            ViewBag.Title = "Conferir Romaneios";
            ViewBag.Description = "Expedicao";
            return View();
        }

        [HttpGet]
        public JsonResult ObterRomaneiosConferenciaSeparacao()
        {
            var usuario = GetUsuarioAtual();
            if (usuario == null)
            {
                Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                return Json(new { mensagem = "Usuario nao localizado." }, JsonRequestBehavior.AllowGet);
            }

            int? filialUsuario = usuario.FilialId;
            int userId = usuario.Id;

            var romaneios = db.Romaneio
                .Where(r =>
                    (!filialUsuario.HasValue || r.FilialId == filialUsuario || r.FilialId == null) &&
                    (r.StatusId ?? 0) != StatusFinalizado &&
                    ((r.ConferenteId ?? 0) == 0 || r.ConferenteId == userId) &&
                    db.RomaneioItem.Any(ri =>
                        ri.RomaneioId == r.Id &&
                        (!filialUsuario.HasValue || ri.FilialId == filialUsuario || ri.FilialId == null) &&
                        ((ri.StatusId ?? 0) == StatusSeparado || (ri.StatusId ?? 0) == StatusEmConferencia)) &&
                    !db.RomaneioItem.Any(ri =>
                        ri.RomaneioId == r.Id &&
                        (!filialUsuario.HasValue || ri.FilialId == filialUsuario || ri.FilialId == null) &&
                        ((ri.StatusId ?? 0) != StatusFinalizado &&
                         (ri.StatusId ?? 0) != StatusEmBusca &&
                         (ri.StatusId ?? 0) != StatusSeparado &&
                         (ri.StatusId ?? 0) != StatusEmConferencia)))
                .OrderBy(r => r.RomaneioNr)
                .Select(r => new
                {
                    id = r.Id,
                    romaneioNr = (r.RomaneioNr ?? string.Empty).Trim()
                })
                .ToList();

            return Json(romaneios, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult IniciarConferenciaSeparacao(int romaneioId)
        {
            if (romaneioId <= 0)
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new { mensagem = "Romaneio invalido." });
            }

            var usuario = GetUsuarioAtual();
            if (usuario == null)
            {
                Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                return Json(new { mensagem = "Usuario nao localizado." });
            }

            int? filialUsuario = usuario.FilialId;
            var romaneio = db.Romaneio.FirstOrDefault(r =>
                r.Id == romaneioId &&
                (!filialUsuario.HasValue || r.FilialId == filialUsuario || r.FilialId == null));

            if (romaneio == null)
            {
                Response.StatusCode = (int)HttpStatusCode.NotFound;
                return Json(new { mensagem = "Romaneio nao encontrado." });
            }

            bool reentrada = (romaneio.ConferenteId ?? 0) == usuario.Id && (romaneio.StatusId ?? 0) == StatusEmConferencia;

            using (var transaction = db.Database.BeginTransaction(IsolationLevel.ReadCommitted))
            {
                try
                {
                    if (!reentrada)
                    {
                        if ((romaneio.ConferenteId ?? 0) != 0 && romaneio.ConferenteId != usuario.Id)
                        {
                            transaction.Rollback();
                            Response.StatusCode = (int)HttpStatusCode.Conflict;
                            return Json(new { mensagem = "Romaneio ja esta em conferencia por outro usuario." });
                        }

                        bool possuiItensPendentesSeparacao = db.RomaneioItem.Any(ri =>
                            ri.RomaneioId == romaneio.Id &&
                            (!filialUsuario.HasValue || ri.FilialId == filialUsuario || ri.FilialId == null) &&
                            ((ri.StatusId ?? 0) != StatusFinalizado &&
                             (ri.StatusId ?? 0) != StatusEmBusca &&
                             (ri.StatusId ?? 0) != StatusSeparado &&
                             (ri.StatusId ?? 0) != StatusEmConferencia));

                        if (possuiItensPendentesSeparacao)
                        {
                            transaction.Rollback();
                            Response.StatusCode = (int)HttpStatusCode.BadRequest;
                            return Json(new { mensagem = "O romaneio ainda possui itens pendentes de separacao." });
                        }

                        bool possuiItensParaConferencia = db.RomaneioItem.Any(ri =>
                            ri.RomaneioId == romaneio.Id &&
                            (!filialUsuario.HasValue || ri.FilialId == filialUsuario || ri.FilialId == null) &&
                            ((ri.StatusId ?? 0) == StatusSeparado || (ri.StatusId ?? 0) == StatusEmConferencia));

                        if (!possuiItensParaConferencia)
                        {
                            transaction.Rollback();
                            Response.StatusCode = (int)HttpStatusCode.BadRequest;
                            return Json(new { mensagem = "Nenhum item disponivel para conferencia neste romaneio." });
                        }
                    }

                    var agora = Util.GetCurrentDateTime();
                    int lockRows = db.Database.ExecuteSqlCommand(
                        @"UPDATE Romaneio
                             SET StatusId = @p0,
                                 ConferenteId = @p1,
                                 DataConferente = @p2
                           WHERE Id = @p3
                             AND (@p4 IS NULL OR FilialId = @p4 OR FilialId IS NULL)
                             AND ISNULL(StatusId, 0) <> @p5
                             AND (ISNULL(ConferenteId, 0) = 0 OR ConferenteId = @p1)",
                        StatusEmConferencia,
                        usuario.Id,
                        agora,
                        romaneio.Id,
                        (object)filialUsuario ?? DBNull.Value,
                        StatusFinalizado);

                    if (lockRows == 0)
                    {
                        transaction.Rollback();
                        Response.StatusCode = (int)HttpStatusCode.Conflict;
                        return Json(new { mensagem = "Romaneio ja esta em conferencia por outro usuario." });
                    }

                    db.Database.ExecuteSqlCommand(
                        @"UPDATE RomaneioItem
                             SET StatusId = @p0,
                                 ConferenteId = @p1,
                                 DataConferente = @p2
                           WHERE RomaneioId = @p3
                             AND (@p4 IS NULL OR FilialId = @p4 OR FilialId IS NULL)
                             AND ISNULL(StatusId, 0) NOT IN (@p5, @p6)",
                        StatusEmConferencia,
                        usuario.Id,
                        agora,
                        romaneio.Id,
                        (object)filialUsuario ?? DBNull.Value,
                        StatusFinalizado,
                        StatusEmBusca);

                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    return Json(new { mensagem = ex.Message });
                }
            }

            return Json(new
            {
                romaneioId = romaneio.Id,
                romaneioNr = (romaneio.RomaneioNr ?? string.Empty).Trim(),
                reentrada = reentrada
            });
        }

        [HttpGet]
        public JsonResult ObterItensConferenciaSeparacao(int romaneioId)
        {
            var usuario = GetUsuarioAtual();
            if (usuario == null)
            {
                Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                return Json(new { mensagem = "Usuario nao localizado." }, JsonRequestBehavior.AllowGet);
            }

            var accessError = ValidateConferenciaSeparacaoAccess(usuario, romaneioId);
            if (accessError != null)
            {
                return accessError;
            }

            var snapshot = BuildConferenciaSeparacaoSnapshot(usuario, romaneioId);
            if (!snapshot.Exists)
            {
                Response.StatusCode = (int)HttpStatusCode.NotFound;
                return Json(new { mensagem = "Romaneio nao encontrado para o usuario logado." }, JsonRequestBehavior.AllowGet);
            }

            return Json(snapshot.ToJson(), JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult RegistrarInteracaoConferenciaSeparacao(int romaneioId, string key)
        {
            var usuario = GetUsuarioAtual();
            if (usuario == null)
            {
                Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                return Json(new { mensagem = "Usuario nao localizado." });
            }

            var accessError = ValidateConferenciaSeparacaoAccess(usuario, romaneioId);
            if (accessError != null)
            {
                return accessError;
            }

            var snapshot = BuildConferenciaSeparacaoSnapshot(usuario, romaneioId);
            if (!snapshot.Exists)
            {
                Response.StatusCode = (int)HttpStatusCode.NotFound;
                return Json(new { mensagem = "Romaneio nao encontrado para o usuario logado." });
            }

            var itemAtual = snapshot.Items.FirstOrDefault(x => string.Equals(x.Key, key, StringComparison.Ordinal));
            if (itemAtual == null)
            {
                Response.StatusCode = (int)HttpStatusCode.NotFound;
                return Json(new { mensagem = "Item nao encontrado para conferencia." });
            }

            var agora = Util.GetCurrentDateTime();
            foreach (var itemId in itemAtual.ItemIds)
            {
                db.Database.ExecuteSqlCommand(
                    @"UPDATE RomaneioItem
                         SET ConferenteId = @p0,
                             DataConferente = @p1
                       WHERE Id = @p2",
                    usuario.Id,
                    agora,
                    itemId);
            }

            FinalizeRomaneioConferenciaIfNeeded(usuario, romaneioId);
            return Json(new { success = true });
        }

        [HttpPost]
        public JsonResult ConfirmarItensConferenciaSeparacao(int romaneioId, IList<ConferenciaQuantidadeInput> itens)
        {
            var usuario = GetUsuarioAtual();
            if (usuario == null)
            {
                Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                return Json(new { mensagem = "Usuario nao localizado." });
            }

            var accessError = ValidateConferenciaSeparacaoAccess(usuario, romaneioId);
            if (accessError != null)
            {
                return accessError;
            }

            var snapshot = BuildConferenciaSeparacaoSnapshot(usuario, romaneioId);
            if (!snapshot.Exists)
            {
                Response.StatusCode = (int)HttpStatusCode.NotFound;
                return Json(new { mensagem = "Romaneio nao encontrado para o usuario logado." });
            }

            var entradas = (itens ?? new List<ConferenciaQuantidadeInput>())
                .Where(x => x != null && !string.IsNullOrWhiteSpace(x.Key) && x.QuantidadeInformada.HasValue)
                .ToList();

            if (!entradas.Any())
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new { mensagem = "Informe ao menos uma quantidade para confirmar." });
            }

            foreach (var entrada in entradas)
            {
                if (entrada.QuantidadeInformada.Value < 0)
                {
                    Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return Json(new { mensagem = "Quantidade informada invalida." });
                }
            }

            using (var transaction = db.Database.BeginTransaction(IsolationLevel.ReadCommitted))
            {
                try
                {
                    var agora = Util.GetCurrentDateTime();

                    foreach (var entrada in entradas)
                    {
                        var itemAtual = snapshot.Items.FirstOrDefault(x => string.Equals(x.Key, entrada.Key, StringComparison.Ordinal));
                        if (itemAtual == null || itemAtual.Finalizado || itemAtual.EmBusca)
                        {
                            continue;
                        }

                        int quantidadeInformada = entrada.QuantidadeInformada.Value;
                        if (quantidadeInformada > itemAtual.QuantidadeFaltante)
                        {
                            transaction.Rollback();
                            Response.StatusCode = (int)HttpStatusCode.BadRequest;
                            return Json(new { mensagem = "Quantidade informada maior que a quantidade faltante do item (" + itemAtual.QuantidadeFaltante + ")." });
                        }

                        var entityIds = db.RomaneioItem
                            .Where(ri => itemAtual.ItemIds.Contains(ri.Id))
                            .OrderBy(ri => ri.Id)
                            .Select(ri => ri.Id)
                            .ToList();

                        var entityRows = LoadConferenciaEntityRows(entityIds);

                        if (quantidadeInformada == 0)
                        {
                            foreach (var entity in entityRows)
                            {
                                if (entity.StatusId == StatusFinalizado)
                                {
                                    continue;
                                }

                                db.Database.ExecuteSqlCommand(
                                    @"UPDATE RomaneioItem
                                         SET StatusId = @p0,
                                             ConferenteId = @p1,
                                             DataConferente = @p2
                                       WHERE Id = @p3",
                                    StatusEmBusca,
                                    usuario.Id,
                                    agora,
                                    entity.Id);
                            }

                            continue;
                        }

                        int quantidadeRestante = quantidadeInformada;
                        foreach (var entity in entityRows)
                        {
                            if (quantidadeRestante <= 0)
                            {
                                break;
                            }

                            int quantidadePedido = Math.Max(entity.Qtde, 0);
                            int quantidadeConferida = Math.Max(entity.QtdeConferida, 0);
                            int saldoItem = Math.Max(quantidadePedido - quantidadeConferida, 0);

                            if (saldoItem == 0)
                            {
                                db.Database.ExecuteSqlCommand(
                                    @"UPDATE RomaneioItem
                                         SET StatusId = @p0
                                       WHERE Id = @p1",
                                    StatusFinalizado,
                                    entity.Id);
                                continue;
                            }

                            int quantidadeAplicada = Math.Min(quantidadeRestante, saldoItem);
                            int novaQtdeConferida = quantidadeConferida + quantidadeAplicada;
                            int novoStatus = novaQtdeConferida >= quantidadePedido
                                ? StatusFinalizado
                                : StatusEmConferencia;

                            db.Database.ExecuteSqlCommand(
                                @"UPDATE RomaneioItem
                                     SET QtdeConferida = @p0,
                                         StatusId = @p1,
                                         ConferenteId = @p2,
                                         DataConferente = @p3
                                   WHERE Id = @p4",
                                novaQtdeConferida,
                                novoStatus,
                                usuario.Id,
                                agora,
                                entity.Id);

                            quantidadeRestante -= quantidadeAplicada;
                        }

                        if (quantidadeRestante > 0)
                        {
                            transaction.Rollback();
                            Response.StatusCode = (int)HttpStatusCode.BadRequest;
                            return Json(new { mensagem = "Nao foi possivel aplicar a quantidade informada no item atual." });
                        }
                    }

                    db.SaveChanges();
                    FinalizeRomaneioConferenciaIfNeeded(usuario, romaneioId);
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    return Json(new { mensagem = ex.Message });
                }
            }

            var updatedSnapshot = BuildConferenciaSeparacaoSnapshot(usuario, romaneioId);
            if (!updatedSnapshot.Exists)
            {
                Response.StatusCode = (int)HttpStatusCode.NotFound;
                return Json(new { mensagem = "Romaneio nao encontrado para o usuario logado." });
            }

            return Json(updatedSnapshot.ToJson());
        }

        [HttpPost]
        public JsonResult LiberarConferenciaSeparacao(int romaneioId)
        {
            var usuario = GetUsuarioAtual();
            if (usuario == null)
            {
                Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                return Json(new { mensagem = "Usuario nao localizado." });
            }

            int? filialUsuario = usuario.FilialId;
            var romaneio = db.Romaneio.FirstOrDefault(r =>
                r.Id == romaneioId &&
                (!filialUsuario.HasValue || r.FilialId == filialUsuario || r.FilialId == null));

            if (romaneio == null)
            {
                Response.StatusCode = (int)HttpStatusCode.NotFound;
                return Json(new { mensagem = "Romaneio nao encontrado." });
            }

            if ((romaneio.ConferenteId ?? 0) != usuario.Id || (romaneio.StatusId ?? 0) != StatusEmConferencia)
            {
                return Json(new { liberado = false });
            }

            using (var transaction = db.Database.BeginTransaction(IsolationLevel.ReadCommitted))
            {
                try
                {
                    var itensEmConferencia = db.RomaneioItem
                        .Where(ri =>
                            ri.RomaneioId == romaneioId &&
                            ri.ConferenteId == usuario.Id &&
                            (!filialUsuario.HasValue || ri.FilialId == filialUsuario || ri.FilialId == null) &&
                            (ri.StatusId ?? 0) == StatusEmConferencia)
                        .Select(ri => ri.Id)
                        .ToList();

                    foreach (var item in LoadConferenciaEntityRows(itensEmConferencia).Where(x => x.QtdeConferida <= 0))
                    {
                        db.Database.ExecuteSqlCommand(
                            @"UPDATE RomaneioItem
                                 SET StatusId = @p0,
                                     ConferenteId = NULL,
                                     DataConferente = NULL
                               WHERE Id = @p1",
                            StatusSeparado,
                            item.Id);
                    }

                    romaneio.StatusId = StatusSeparado;
                    romaneio.ConferenteId = null;
                    romaneio.DataConferente = null;
                    db.SaveChanges();
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    return Json(new { mensagem = ex.Message });
                }
            }

            return Json(new { liberado = true });
        }

        private Usuario GetUsuarioAtual()
        {
            string login = Util.GetCurrentUser();
            return db.Usuario.FirstOrDefault(u => u.Login == login && u.FilialId == filialId);
        }

        private JsonResult ValidateConferenciaSeparacaoAccess(Usuario usuario, int romaneioId)
        {
            int? filialUsuario = usuario.FilialId;
            var romaneio = db.Romaneio.FirstOrDefault(r =>
                r.Id == romaneioId &&
                (!filialUsuario.HasValue || r.FilialId == filialUsuario || r.FilialId == null));

            if (romaneio == null)
            {
                Response.StatusCode = (int)HttpStatusCode.NotFound;
                return Json(new { mensagem = "Romaneio nao encontrado." }, JsonRequestBehavior.AllowGet);
            }

            if ((romaneio.ConferenteId ?? 0) != 0 &&
                romaneio.ConferenteId != usuario.Id &&
                (romaneio.StatusId ?? 0) == StatusEmConferencia)
            {
                Response.StatusCode = (int)HttpStatusCode.Conflict;
                return Json(new { mensagem = "Romaneio ja esta em conferencia por outro usuario." }, JsonRequestBehavior.AllowGet);
            }

            return null;
        }

        private void FinalizeRomaneioConferenciaIfNeeded(Usuario usuario, int romaneioId)
        {
            int? filialUsuario = usuario.FilialId;
            bool possuiPendencia = db.RomaneioItem.Any(ri =>
                ri.RomaneioId == romaneioId &&
                (!filialUsuario.HasValue || ri.FilialId == filialUsuario || ri.FilialId == null) &&
                (ri.StatusId ?? 0) != StatusFinalizado &&
                (ri.StatusId ?? 0) != StatusEmBusca);

            var romaneio = db.Romaneio.FirstOrDefault(r =>
                r.Id == romaneioId &&
                (!filialUsuario.HasValue || r.FilialId == filialUsuario || r.FilialId == null));

            if (romaneio == null)
            {
                return;
            }

            romaneio.StatusId = possuiPendencia ? StatusEmConferencia : StatusFinalizado;
            romaneio.ConferenteId = usuario.Id;
            romaneio.DataConferente = db.RomaneioItem
                .Where(ri =>
                    ri.RomaneioId == romaneioId &&
                    (!filialUsuario.HasValue || ri.FilialId == filialUsuario || ri.FilialId == null))
                .Select(ri => ri.DataConferente)
                .ToList()
                .Where(data => data.HasValue)
                .OrderByDescending(data => data.Value)
                .FirstOrDefault();

            db.SaveChanges();
        }

        private ConferenciaSeparacaoSnapshot BuildConferenciaSeparacaoSnapshot(Usuario usuario, int romaneioId)
        {
            int? filialUsuario = usuario.FilialId;
            var romaneio = db.Romaneio.FirstOrDefault(r =>
                r.Id == romaneioId &&
                (!filialUsuario.HasValue || r.FilialId == filialUsuario || r.FilialId == null) &&
                ((r.ConferenteId ?? 0) == usuario.Id || (r.StatusId ?? 0) == StatusFinalizado));

            if (romaneio == null)
            {
                return ConferenciaSeparacaoSnapshot.Empty;
            }

            var rows = db.Database.SqlQuery<ConferenciaSeparacaoRowRaw>(
                @"SELECT
                      ri.Id,
                      ISNULL(ri.ZonaId, 0) AS ZonaId,
                      ISNULL(z.Nome, '') AS Zona,
                      ISNULL(ri.ItemNr, '') AS ItemNr,
                      ISNULL(ri.Descricao, '') AS Descricao,
                      ISNULL(ri.Qtde, 0) AS Qtde,
                      ISNULL(ri.QtdeConferida, 0) AS QtdeConferida,
                      ISNULL(ri.StatusId, 0) AS StatusId
                  FROM RomaneioItem ri
             LEFT JOIN Zona z ON ri.ZonaId = z.Id
                 WHERE ri.RomaneioId = @p0
                   AND (@p1 IS NULL OR ri.FilialId = @p1 OR ri.FilialId IS NULL)",
                romaneioId,
                (object)filialUsuario ?? DBNull.Value)
                .ToList();

            if (!rows.Any())
            {
                return ConferenciaSeparacaoSnapshot.Empty;
            }

            var grouped = rows
                .GroupBy(x => new
                {
                    x.ZonaId,
                    Zona = (x.Zona ?? string.Empty).Trim(),
                    ItemNr = (x.ItemNr ?? string.Empty).Trim(),
                    Descricao = (x.Descricao ?? string.Empty).Trim()
                })
                .Select(g =>
                {
                    int quantidadePedido = g.Sum(x => Math.Max(x.Qtde, 0));
                    int quantidadeConferida = g.Sum(x => Math.Min(Math.Max(x.QtdeConferida, 0), Math.Max(x.Qtde, 0)));
                    bool emBusca = g.All(x => x.StatusId == StatusEmBusca);
                    bool finalizado = emBusca || Math.Max(quantidadePedido - quantidadeConferida, 0) == 0;

                    return new ConferenciaSeparacaoItemGroup
                    {
                        ZonaId = g.Key.ZonaId,
                        Key = string.Join("-", g.Select(x => x.Id).Distinct().OrderBy(x => x)),
                        Zona = g.Key.Zona,
                        ItemNr = g.Key.ItemNr,
                        Descricao = g.Key.Descricao,
                        QuantidadePedido = quantidadePedido,
                        QuantidadeConferida = quantidadeConferida,
                        QuantidadeFaltante = Math.Max(quantidadePedido - quantidadeConferida, 0),
                        EmBusca = emBusca,
                        Finalizado = finalizado,
                        ItemIds = g.Select(x => x.Id).Distinct().ToList()
                    };
                })
                .OrderBy(x => x.Zona)
                .ThenBy(x => x.ItemNr)
                .ThenBy(x => x.Descricao)
                .ToList();

            var current = grouped.FirstOrDefault(x => !x.Finalizado && !x.EmBusca);
            if (current == null)
            {
                FinalizeRomaneioConferenciaIfNeeded(usuario, romaneioId);
            }

            return new ConferenciaSeparacaoSnapshot
            {
                Exists = true,
                RomaneioId = romaneio.Id,
                RomaneioNr = (romaneio.RomaneioNr ?? string.Empty).Trim(),
                CurrentItem = current,
                Items = grouped
            };
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }

            base.Dispose(disposing);
        }

        private List<ConferenciaSeparacaoEntityRow> LoadConferenciaEntityRows(IEnumerable<int> itemIds)
        {
            var ids = (itemIds ?? Enumerable.Empty<int>()).Distinct().ToList();
            if (!ids.Any())
            {
                return new List<ConferenciaSeparacaoEntityRow>();
            }

            return db.Database.SqlQuery<ConferenciaSeparacaoEntityRow>(
                @"SELECT
                      Id,
                      ISNULL(Qtde, 0) AS Qtde,
                      ISNULL(QtdeConferida, 0) AS QtdeConferida,
                      ISNULL(StatusId, 0) AS StatusId
                  FROM RomaneioItem
                  WHERE Id IN (" + string.Join(",", ids) + @")
                  ORDER BY Id")
                .ToList();
        }

        private sealed class ConferenciaSeparacaoRowRaw
        {
            public int Id { get; set; }
            public int ZonaId { get; set; }
            public string Zona { get; set; }
            public string ItemNr { get; set; }
            public string Descricao { get; set; }
            public int Qtde { get; set; }
            public int QtdeConferida { get; set; }
            public int StatusId { get; set; }
        }

        private sealed class ConferenciaSeparacaoEntityRow
        {
            public int Id { get; set; }
            public int Qtde { get; set; }
            public int QtdeConferida { get; set; }
            public int StatusId { get; set; }
        }

        private sealed class ConferenciaSeparacaoItemGroup
        {
            public int ZonaId { get; set; }
            public string Key { get; set; }
            public string Zona { get; set; }
            public string ItemNr { get; set; }
            public string Descricao { get; set; }
            public int QuantidadePedido { get; set; }
            public int QuantidadeConferida { get; set; }
            public int QuantidadeFaltante { get; set; }
            public bool Finalizado { get; set; }
            public bool EmBusca { get; set; }
            public List<int> ItemIds { get; set; }

            public object ToJson(bool atual)
            {
                return new
                {
                    zonaId = ZonaId,
                    key = Key,
                    zona = Zona,
                    itemNr = ItemNr,
                    descricao = Descricao,
                    quantidadePedido = QuantidadePedido,
                    quantidadeConferida = QuantidadeConferida,
                    quantidadeFaltante = QuantidadeFaltante,
                    atual = atual,
                    finalizado = Finalizado,
                    emBusca = EmBusca
                };
            }
        }

        private sealed class ConferenciaSeparacaoSnapshot
        {
            public static ConferenciaSeparacaoSnapshot Empty
            {
                get { return new ConferenciaSeparacaoSnapshot(); }
            }

            public bool Exists { get; set; }
            public int RomaneioId { get; set; }
            public string RomaneioNr { get; set; }
            public ConferenciaSeparacaoItemGroup CurrentItem { get; set; }
            public List<ConferenciaSeparacaoItemGroup> Items { get; set; }

            public object ToJson()
            {
                return new
                {
                    finalizado = CurrentItem == null,
                    mensagem = CurrentItem == null ? "Conferencia finalizada com sucesso." : string.Empty,
                    itemAtual = CurrentItem != null ? CurrentItem.ToJson(true) : null,
                    itens = (Items ?? new List<ConferenciaSeparacaoItemGroup>())
                        .Select(item => item.ToJson(CurrentItem != null && object.ReferenceEquals(item, CurrentItem)))
                        .ToList()
                };
            }
        }

        public sealed class ConferenciaQuantidadeInput
        {
            public string Key { get; set; }
            public int? QuantidadeInformada { get; set; }
        }
    }
}
