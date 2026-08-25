using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using QuasarApi.DataBase;
using QuasarApi.Database.Models;
using QuasarApi.DTO.Operations.Expedicao.ConferenciaSeparacao;
using QuasarApi.Helpers;
using System.Data;
using System.Security.Claims;

namespace QuasarApi.Routes.Operations
{
    public static class ExpedicaoRoutes
    {
        private const int StatusFinalizado = 4;
        private const int StatusEmBusca = 6;
        private const int StatusSeparado = 8;
        private const int StatusEmConferencia = 9;

        public static WebApplication MapExpedicaoRoutes(this WebApplication app, WebApplicationBuilder builder)
        {
            const string groupPrefix = "/expedicao";
            var group = app.MapGroup(groupPrefix);

            group.MapGet("/conferencia-separacao/romaneios", async (HttpContext httpContext, AppDbContext db) =>
            {
                var usuario = await ResolveCurrentUserAsync(httpContext, db);
                if (usuario == null)
                {
                    return Results.Unauthorized();
                }

                int? filialId = usuario.FilialId;
                int userId = usuario.Id;

                var romaneios = await db.Romaneio
                    .Where(r =>
                        (!filialId.HasValue || r.FilialId == filialId || r.FilialId == null) &&
                        (r.StatusId ?? 0) != StatusFinalizado &&
                        ((r.ConferenteId ?? 0) == 0 || r.ConferenteId == userId) &&
                        db.RomaneioItem.Any(ri =>
                            ri.RomaneioId == r.Id &&
                            (!filialId.HasValue || ri.FilialId == filialId || ri.FilialId == null) &&
                            ((ri.StatusId ?? 0) == StatusSeparado || (ri.StatusId ?? 0) == StatusEmConferencia)) &&
                        !db.RomaneioItem.Any(ri =>
                            ri.RomaneioId == r.Id &&
                            (!filialId.HasValue || ri.FilialId == filialId || ri.FilialId == null) &&
                            ((ri.StatusId ?? 0) != StatusFinalizado &&
                             (ri.StatusId ?? 0) != StatusEmBusca &&
                             (ri.StatusId ?? 0) != StatusSeparado &&
                             (ri.StatusId ?? 0) != StatusEmConferencia)))
                    .OrderBy(r => r.RomaneioNr)
                    .Select(r => new RomaneioConferenciaDisponivelDto
                    {
                        Id = r.Id,
                        RomaneioNr = (r.RomaneioNr ?? string.Empty).Trim()
                    })
                    .ToListAsync();

                return Results.Ok(romaneios);
            }).RequireAuthorization();

            group.MapPost("/conferencia-separacao/iniciar", async (HttpContext httpContext, [FromBody] IniciarConferenciaSeparacaoRequestDto request, AppDbContext db) =>
            {
                if (request == null || request.RomaneioId <= 0)
                {
                    return Results.BadRequest(new { mensagem = "Romaneio inválido." });
                }

                var usuario = await ResolveCurrentUserAsync(httpContext, db);
                if (usuario == null)
                {
                    return Results.Unauthorized();
                }

                int? filialId = usuario.FilialId;
                var romaneio = await db.Romaneio.FirstOrDefaultAsync(r =>
                    r.Id == request.RomaneioId &&
                    (!filialId.HasValue || r.FilialId == filialId || r.FilialId == null));

                if (romaneio == null)
                {
                    return Results.NotFound(new { mensagem = "Romaneio não encontrado." });
                }

                bool reentrada = (romaneio.ConferenteId ?? 0) == usuario.Id && (romaneio.StatusId ?? 0) == StatusEmConferencia;

                await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
                try
                {
                    if (!reentrada)
                    {
                        if ((romaneio.ConferenteId ?? 0) != 0 && romaneio.ConferenteId != usuario.Id)
                        {
                            return Results.Conflict(new { mensagem = "Romaneio já está em conferência por outro usuário." });
                        }

                        bool possuiItensPendentesSeparacao = await db.RomaneioItem.AnyAsync(ri =>
                            ri.RomaneioId == romaneio.Id &&
                            (!filialId.HasValue || ri.FilialId == filialId || ri.FilialId == null) &&
                            ((ri.StatusId ?? 0) != StatusFinalizado &&
                             (ri.StatusId ?? 0) != StatusEmBusca &&
                             (ri.StatusId ?? 0) != StatusSeparado &&
                             (ri.StatusId ?? 0) != StatusEmConferencia));

                        if (possuiItensPendentesSeparacao)
                        {
                            await transaction.RollbackAsync();
                            return Results.BadRequest(new { mensagem = "O romaneio ainda possui itens pendentes de separação." });
                        }

                        bool possuiItensParaConferencia = await db.RomaneioItem.AnyAsync(ri =>
                            ri.RomaneioId == romaneio.Id &&
                            (!filialId.HasValue || ri.FilialId == filialId || ri.FilialId == null) &&
                            ((ri.StatusId ?? 0) == StatusSeparado || (ri.StatusId ?? 0) == StatusEmConferencia));

                        if (!possuiItensParaConferencia)
                        {
                            await transaction.RollbackAsync();
                            return Results.BadRequest(new { mensagem = "Nenhum item disponível para conferência neste romaneio." });
                        }
                    }

                    var agora = CurrentDateTime.GetCurrentDateTime();
                    int lockRows = await db.Database.ExecuteSqlRawAsync(
                        @"
UPDATE Romaneio
   SET StatusId = @statusEmConferencia,
       ConferenteId = @conferenteId,
       DataConferente = @dataConferente
 WHERE Id = @romaneioId
   AND (@filialId IS NULL OR FilialId = @filialId OR FilialId IS NULL)
   AND ISNULL(StatusId, 0) <> @statusFinalizado
   AND (ISNULL(ConferenteId, 0) = 0 OR ConferenteId = @conferenteId);",
                        new SqlParameter("@statusEmConferencia", StatusEmConferencia),
                        new SqlParameter("@statusFinalizado", StatusFinalizado),
                        new SqlParameter("@conferenteId", usuario.Id),
                        new SqlParameter("@dataConferente", agora),
                        new SqlParameter("@romaneioId", romaneio.Id),
                        new SqlParameter("@filialId", (object?)filialId ?? DBNull.Value));

                    if (lockRows == 0)
                    {
                        await transaction.RollbackAsync();
                        return Results.Conflict(new { mensagem = "Romaneio já está em conferência por outro usuário." });
                    }

                    await db.Database.ExecuteSqlRawAsync(
                        @"
UPDATE RomaneioItem
   SET StatusId = @statusEmConferencia,
       ConferenteId = @conferenteId,
       DataConferente = @dataConferente
 WHERE RomaneioId = @romaneioId
   AND (@filialId IS NULL OR FilialId = @filialId OR FilialId IS NULL)
   AND ISNULL(StatusId, 0) NOT IN (@statusFinalizado, @statusEmBusca);",
                        new SqlParameter("@statusEmConferencia", StatusEmConferencia),
                        new SqlParameter("@statusFinalizado", StatusFinalizado),
                        new SqlParameter("@statusEmBusca", StatusEmBusca),
                        new SqlParameter("@conferenteId", usuario.Id),
                        new SqlParameter("@dataConferente", agora),
                        new SqlParameter("@romaneioId", romaneio.Id),
                        new SqlParameter("@filialId", (object?)filialId ?? DBNull.Value));

                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }

                return Results.Ok(new IniciarConferenciaSeparacaoResponseDto
                {
                    RomaneioId = romaneio.Id,
                    RomaneioNr = (romaneio.RomaneioNr ?? string.Empty).Trim(),
                    Reentrada = reentrada
                });
            }).RequireAuthorization();

            group.MapGet("/conferencia-separacao/romaneios/{romaneioId:int}/itens", async (HttpContext httpContext, int romaneioId, AppDbContext db) =>
            {
                var usuario = await ResolveCurrentUserAsync(httpContext, db);
                if (usuario == null)
                {
                    return Results.Unauthorized();
                }

                var accessBlock = await ValidateConferenciaSeparacaoAccessAsync(db, usuario, romaneioId);
                if (accessBlock != null)
                {
                    return accessBlock;
                }

                var snapshot = await BuildConferenciaSeparacaoSnapshotAsync(db, usuario, romaneioId);
                if (!snapshot.Exists)
                {
                    return Results.NotFound(new { mensagem = "Romaneio não encontrado para o usuário logado." });
                }

                return Results.Ok(snapshot.ToDto());
            }).RequireAuthorization();

            group.MapPost("/conferencia-separacao/romaneios/{romaneioId:int}/confirmar", async (HttpContext httpContext, int romaneioId, [FromBody] ConfirmarConferenciaSeparacaoRequestDto request, AppDbContext db) =>
            {
                if (request == null || request.QuantidadeInformada < 0)
                {
                    return Results.BadRequest(new { mensagem = "Quantidade informada inválida." });
                }

                var usuario = await ResolveCurrentUserAsync(httpContext, db);
                if (usuario == null)
                {
                    return Results.Unauthorized();
                }

                var accessBlock = await ValidateConferenciaSeparacaoAccessAsync(db, usuario, romaneioId);
                if (accessBlock != null)
                {
                    return accessBlock;
                }

                var snapshot = await BuildConferenciaSeparacaoSnapshotAsync(db, usuario, romaneioId);
                if (!snapshot.Exists)
                {
                    return Results.NotFound(new { mensagem = "Romaneio não encontrado para o usuário logado." });
                }

                var itemAtual = snapshot.CurrentItem;
                if (itemAtual == null)
                {
                    return Results.Ok(snapshot.ToDto());
                }

                if (request.QuantidadeInformada > itemAtual.QuantidadeFaltante)
                {
                    return Results.BadRequest(new
                    {
                        mensagem = $"Quantidade informada maior que a quantidade faltante do item ({itemAtual.QuantidadeFaltante})."
                    });
                }

                var entities = await db.RomaneioItem
                    .Where(ri => itemAtual.ItemIds.Contains(ri.Id))
                    .OrderBy(ri => ri.Id)
                    .ToListAsync();

                var agora = CurrentDateTime.GetCurrentDateTime();

                if (request.QuantidadeInformada == 0)
                {
                    foreach (var entity in entities)
                    {
                        if ((entity.StatusId ?? 0) == StatusFinalizado)
                        {
                            continue;
                        }

                        entity.StatusId = StatusEmBusca;
                        entity.ConferenteId = usuario.Id;
                        entity.DataConferente = agora;
                    }
                }
                else
                {
                    int quantidadeRestante = request.QuantidadeInformada;

                    foreach (var entity in entities)
                    {
                        if (quantidadeRestante <= 0)
                        {
                            break;
                        }

                        int quantidadePedido = Math.Max(entity.Qtde ?? 0, 0);
                        int quantidadeConferida = Math.Max(entity.QtdeConferida ?? 0, 0);
                        int saldoItem = Math.Max(quantidadePedido - quantidadeConferida, 0);

                        if (saldoItem == 0)
                        {
                            entity.StatusId = StatusFinalizado;
                            continue;
                        }

                        int quantidadeAplicada = Math.Min(quantidadeRestante, saldoItem);
                        entity.QtdeConferida = quantidadeConferida + quantidadeAplicada;
                        entity.StatusId = entity.QtdeConferida >= quantidadePedido
                            ? StatusFinalizado
                            : StatusEmConferencia;
                        entity.ConferenteId = usuario.Id;
                        entity.DataConferente = agora;

                        quantidadeRestante -= quantidadeAplicada;
                    }

                    if (quantidadeRestante > 0)
                    {
                        return Results.BadRequest(new { mensagem = "Não foi possível aplicar a quantidade informada no item atual." });
                    }
                }

                await db.SaveChangesAsync();
                await FinalizeRomaneioConferenciaIfNeededAsync(db, usuario, romaneioId);

                var updatedSnapshot = await BuildConferenciaSeparacaoSnapshotAsync(db, usuario, romaneioId);
                if (!updatedSnapshot.Exists)
                {
                    return Results.NotFound(new { mensagem = "Romaneio não encontrado para o usuário logado." });
                }

                return Results.Ok(updatedSnapshot.ToDto());
            }).RequireAuthorization();

            group.MapPost("/conferencia-separacao/romaneios/{romaneioId:int}/abandonar", async (HttpContext httpContext, int romaneioId, AppDbContext db) =>
            {
                var usuario = await ResolveCurrentUserAsync(httpContext, db);
                if (usuario == null)
                {
                    return Results.Unauthorized();
                }

                int? filialId = usuario.FilialId;
                var romaneio = await db.Romaneio.FirstOrDefaultAsync(r =>
                    r.Id == romaneioId &&
                    (!filialId.HasValue || r.FilialId == filialId || r.FilialId == null));

                if (romaneio == null)
                {
                    return Results.NotFound(new { mensagem = "Romaneio não encontrado." });
                }

                if ((romaneio.ConferenteId ?? 0) != usuario.Id || (romaneio.StatusId ?? 0) != StatusEmConferencia)
                {
                    return Results.Ok(new { liberado = false });
                }

                await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
                try
                {
                    var itensNaoConfirmados = await db.RomaneioItem
                        .Where(ri =>
                            ri.RomaneioId == romaneioId &&
                            ri.ConferenteId == usuario.Id &&
                            (!filialId.HasValue || ri.FilialId == filialId || ri.FilialId == null) &&
                            (ri.StatusId ?? 0) == StatusEmConferencia &&
                            (ri.QtdeConferida ?? 0) <= 0)
                        .ToListAsync();

                    foreach (var item in itensNaoConfirmados)
                    {
                        item.StatusId = StatusSeparado;
                        item.ConferenteId = null;
                        item.DataConferente = null;
                    }

                    romaneio.StatusId = StatusSeparado;
                    romaneio.ConferenteId = null;
                    romaneio.DataConferente = null;
                    await db.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }

                return Results.Ok(new { liberado = true });
            }).RequireAuthorization();

            group.MapGet("/transportadoras", async (AppDbContext db) =>
            {
                try
                {
                    var query = db.Transportadora.AsQueryable();

                    var list = await query
                        .Where(t => t.EmitirEtiqueta == true)
                        .Where(t => db.DocExpedicao
                                .Where(d => d.TransportadoraId == t.Id)
                                .Any(d => d.QtdVolumes > db.HistoricoDespacho
                                .Count(v => v.NotaFiscalNr == d.Numero && v.TransportadoraId == t.Id)))
                        .OrderBy(t => t.Nome)
                        .Select(t => new { id = t.Id, nome = t.Nome, t.EmitirEtiqueta })
                        .ToListAsync();

                    return Results.Ok(list);
                }
                catch (Exception ex)
                {
                    var problemDetails = new ProblemDetails
                    {
                        Title = "Erro ao obter transportadoras",
                        Status = 500,
                        Detail = ex.Message,
                        Instance = groupPrefix
                    };
                    return Results.Problem(problemDetails);
                }
            }).RequireAuthorization();

            group.MapGet("/volumes/resumo/{transportadoraId}", async (int transportadoraId, AppDbContext db) =>
            {
                if (transportadoraId <= 0)
                {
                    return Results.BadRequest(new { mensagem = "Id da transportadora não informado" });
                }

                try
                {
                    var pendentes = (from d in db.DocExpedicao
                                     where d.TransportadoraId == transportadoraId
                                     select new
                                     {
                                         d.QtdVolumes,
                                         QtdVolConf = d.QtdVolConf ?? 0
                                     }).Sum(x => (int?)x.QtdVolumes) - (from d in db.DocExpedicao
                                                                        where d.TransportadoraId == transportadoraId
                                                                        select d.QtdVolConf).Sum() ?? 0;
                    var total = db.DocExpedicao
                        .Where(d => d.TransportadoraId == transportadoraId)
                        .Sum(d => (int?)d.QtdVolumes) ?? 0;

                    var lidos = db.DocExpedicao
                        .Where(d => d.TransportadoraId == transportadoraId)
                        .Sum(d => (int?)(d.QtdVolConf ?? 0)) ?? 0;

                    return Results.Ok(new { total, lidos, pendentes });
                }
                catch (Exception ex)
                {
                    var problemDetails = new ProblemDetails
                    {
                        Title = "Erro ao obter resumo de volumes por transportadora",
                        Status = 500,
                        Detail = ex.Message,
                        Instance = $"{groupPrefix}/volumes/resumo"
                    };

                    return Results.Problem(problemDetails);
                }
            }).RequireAuthorization();

            group.MapGet("/volumes/pendentes/{transportadoraId}", async (int transportadoraId, AppDbContext db) =>
            {
                if (transportadoraId <= 0)
                {
                    return Results.BadRequest(new { mensagem = "Id da transportadora não informado" });
                }

                try
                {
                    var lista = await db.DocExpedicao
                        .Where(d => d.TransportadoraId == transportadoraId)
                        .Where(d => (d.QtdVolConf ?? 0) < d.QtdVolumes)
                        .OrderBy(d => d.Numero)
                        .Select(d => new
                        {
                            d.Numero,
                            d.Controle,
                            d.QtdVolumes,
                            d.NomeCliente,
                            d.Cidade,
                            d.Estado
                        }).ToListAsync();

                    return Results.Ok(lista);
                }
                catch (Exception ex)
                {
                    var problemDetails = new ProblemDetails
                    {
                        Title = "Erro ao obter volumes pendentes",
                        Status = 500,
                        Detail = ex.Message,
                        Instance = $"{groupPrefix}/volumes/pendentes"
                    };
                    return Results.Problem(problemDetails);
                }
            }).RequireAuthorization();

            group.MapGet("/volumes/lidos/{transportadoraId}", async (int transportadoraId, AppDbContext db) =>
            {
                if (transportadoraId <= 0)
                {
                    return Results.BadRequest(new { mensagem = "Id da transportadora não informado" });
                }

                try
                {
                    var lista = await (
                        from h in db.HistoricoDespacho
                        join d in db.DocExpedicao
                            on new { Numero = h.NotaFiscalNr, h.TransportadoraId }
                            equals new { d.Numero, d.TransportadoraId }
                        where h.TransportadoraId == transportadoraId
                        orderby h.CriadoEm descending
                        select new
                        {
                            h.NotaFiscalNr,
                            h.VolumeNr,
                            h.Veiculo,
                            h.Responsavel,
                            d.NomeCliente,
                            d.Cidade,
                            d.Estado,
                            h.CriadoEm,
                            h.CriadoPor
                        }
                    ).ToListAsync();

                    return Results.Ok(lista);
                }
                catch (Exception ex)
                {
                    var problemDetails = new ProblemDetails
                    {
                        Title = "Erro ao obter volumes conferidos",
                        Status = 500,
                        Detail = ex.Message,
                        Instance = $"{groupPrefix}/volumes/lidos"
                    };
                    return Results.Problem(problemDetails);
                }
            }).RequireAuthorization();

            group.MapGet("/doc", async ([FromQuery] string numero, [FromQuery] int transportadoraId, AppDbContext db) =>
            {
                if (string.IsNullOrWhiteSpace(numero))
                {
                    return Results.BadRequest(new { mensagem = "Informe o número da nota fiscal." });
                }

                try
                {
                    IQueryable<DocExpedicao> query = db.DocExpedicao.Where(d => d.TransportadoraId == transportadoraId);
                    query = query.Where(d => d.Numero == numero);

                    var doc = await query.Select(d => new
                    {
                        numero = d.Numero,
                        transportadoraId = d.TransportadoraId,
                        qtdVolumes = d.QtdVolumes
                    }).FirstOrDefaultAsync();

                    if (doc == null)
                    {
                        return Results.NotFound(new { mensagem = "Documento não encontrado." });
                    }

                    return Results.Ok(doc);
                }
                catch (Exception ex)
                {
                    var problemDetails = new ProblemDetails
                    {
                        Title = "Erro ao obter documento de expedição",
                        Status = 500,
                        Detail = ex.Message,
                        Instance = $"{groupPrefix}/doc"
                    };
                    return Results.Problem(problemDetails);
                }
            }).RequireAuthorization();

            group.MapGet("/historico/volumes", async ([FromQuery] string notaFiscalNr, AppDbContext db) =>
            {
                if (string.IsNullOrWhiteSpace(notaFiscalNr))
                {
                    return Results.BadRequest(new { mensagem = "Informe a notaFiscalNr." });
                }

                try
                {
                    var volumes = await db.HistoricoDespacho
                        .Where(h => h.NotaFiscalNr == notaFiscalNr)
                        .OrderBy(h => h.VolumeNr)
                        .Select(h => h.VolumeNr)
                        .ToListAsync();

                    return Results.Ok(new { volumes });
                }
                catch (Exception ex)
                {
                    var problemDetails = new ProblemDetails
                    {
                        Title = "Erro ao listar volumes",
                        Status = 500,
                        Detail = ex.Message,
                        Instance = $"{groupPrefix}/historico/volumes"
                    };
                    return Results.Problem(problemDetails);
                }
            }).RequireAuthorization();

            group.MapPost("/historico", async ([FromBody] HistoricoDespacho request, AppDbContext db) =>
            {
                if (string.IsNullOrWhiteSpace(request.NotaFiscalNr) || string.IsNullOrWhiteSpace(request.VolumeNr))
                {
                    return Results.BadRequest(new { mensagem = "Dados inválidos." });
                }

                await using var transaction = await db.Database.BeginTransactionAsync();
                try
                {
                    if (string.IsNullOrWhiteSpace(request.CriadoPor))
                    {
                        request.CriadoPor = "Quasar";
                    }

                    request.CriadoEm = DateTime.UtcNow;

                    db.HistoricoDespacho.Add(request);
                    await db.SaveChangesAsync();

                    var docExpedicao = await db.DocExpedicao.FirstOrDefaultAsync(d => d.Numero == request.NotaFiscalNr);
                    if (docExpedicao != null)
                    {
                        docExpedicao.QtdVolConf = (docExpedicao.QtdVolConf ?? 0) + 1;
                        await db.SaveChangesAsync();
                    }

                    await transaction.CommitAsync();

                    return Results.Created($"{groupPrefix}/historico/{request.Id}", new { id = request.Id });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    var problemDetails = new ProblemDetails
                    {
                        Title = "Erro ao gravar histórico",
                        Status = 500,
                        Detail = ex.Message,
                        Instance = $"{groupPrefix}/historico"
                    };
                    return Results.Problem(problemDetails);
                }
            }).RequireAuthorization();

            return app;
        }

        private static async Task<Usuario?> ResolveCurrentUserAsync(HttpContext httpContext, AppDbContext db)
        {
            string? userIdValue = httpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdValue, out int userId))
            {
                return await db.Usuario.FirstOrDefaultAsync(x => x.Id == userId);
            }

            string? login = httpContext.User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(login))
            {
                return null;
            }

            return await db.Usuario.FirstOrDefaultAsync(x => x.Login == login);
        }

        private static async Task<IResult?> ValidateConferenciaSeparacaoAccessAsync(AppDbContext db, Usuario usuario, int romaneioId)
        {
            int? filialId = usuario.FilialId;
            var romaneio = await db.Romaneio.FirstOrDefaultAsync(r =>
                r.Id == romaneioId &&
                (!filialId.HasValue || r.FilialId == filialId || r.FilialId == null));

            if (romaneio == null)
            {
                return Results.NotFound(new { mensagem = "Romaneio não encontrado." });
            }

            if ((romaneio.ConferenteId ?? 0) != 0 &&
                romaneio.ConferenteId != usuario.Id &&
                (romaneio.StatusId ?? 0) == StatusEmConferencia)
            {
                return Results.Conflict(new { mensagem = "Romaneio já está em conferência por outro usuário." });
            }

            return null;
        }

        private static async Task FinalizeRomaneioConferenciaIfNeededAsync(AppDbContext db, Usuario usuario, int romaneioId)
        {
            int? filialId = usuario.FilialId;
            bool possuiPendencia = await db.RomaneioItem.AnyAsync(ri =>
                ri.RomaneioId == romaneioId &&
                (!filialId.HasValue || ri.FilialId == filialId || ri.FilialId == null) &&
                (ri.StatusId ?? 0) != StatusFinalizado &&
                (ri.StatusId ?? 0) != StatusEmBusca);

            var romaneio = await db.Romaneio.FirstOrDefaultAsync(r =>
                r.Id == romaneioId &&
                (!filialId.HasValue || r.FilialId == filialId || r.FilialId == null));

            if (romaneio == null)
            {
                return;
            }

            if (!possuiPendencia)
            {
                romaneio.StatusId = StatusFinalizado;
                romaneio.ConferenteId = usuario.Id;
            }
            else
            {
                romaneio.StatusId = StatusEmConferencia;
                romaneio.ConferenteId = usuario.Id;
            }

            romaneio.DataConferente = await db.RomaneioItem
                .Where(ri =>
                    ri.RomaneioId == romaneioId &&
                    (!filialId.HasValue || ri.FilialId == filialId || ri.FilialId == null))
                .MaxAsync(ri => ri.DataConferente);

            await db.SaveChangesAsync();
        }

        private static async Task<ConferenciaSeparacaoSnapshot> BuildConferenciaSeparacaoSnapshotAsync(AppDbContext db, Usuario usuario, int romaneioId)
        {
            int? filialId = usuario.FilialId;
            var romaneio = await db.Romaneio.FirstOrDefaultAsync(r =>
                r.Id == romaneioId &&
                (!filialId.HasValue || r.FilialId == filialId || r.FilialId == null) &&
                ((r.ConferenteId ?? 0) == usuario.Id || (r.StatusId ?? 0) == StatusFinalizado));

            if (romaneio == null)
            {
                return ConferenciaSeparacaoSnapshot.Empty;
            }

            var rows = await (
                from ri in db.RomaneioItem
                join z in db.Zona on ri.ZonaId equals z.Id into zonaJoin
                from zona in zonaJoin.DefaultIfEmpty()
                where ri.RomaneioId == romaneioId
                   && (!filialId.HasValue || ri.FilialId == filialId || ri.FilialId == null)
                select new ConferenciaSeparacaoRowRaw
                {
                    Id = ri.Id,
                    ZonaId = ri.ZonaId ?? 0,
                    Zona = zona != null ? (zona.Nome ?? string.Empty) : string.Empty,
                    ItemNr = ri.ItemNr ?? string.Empty,
                    Descricao = ri.Descricao ?? string.Empty,
                    Qtde = ri.Qtde ?? 0,
                    QtdeConferida = ri.QtdeConferida ?? 0,
                    StatusId = ri.StatusId ?? 0
                }).ToListAsync();

            if (rows.Count == 0)
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
                await FinalizeRomaneioConferenciaIfNeededAsync(db, usuario, romaneioId);
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

        private sealed class ConferenciaSeparacaoRowRaw
        {
            public int Id { get; set; }
            public int ZonaId { get; set; }
            public string Zona { get; set; } = string.Empty;
            public string ItemNr { get; set; } = string.Empty;
            public string Descricao { get; set; } = string.Empty;
            public int Qtde { get; set; }
            public int QtdeConferida { get; set; }
            public int StatusId { get; set; }
        }

        private sealed class ConferenciaSeparacaoItemGroup
        {
            public int ZonaId { get; set; }
            public string Zona { get; set; } = string.Empty;
            public string ItemNr { get; set; } = string.Empty;
            public string Descricao { get; set; } = string.Empty;
            public int QuantidadePedido { get; set; }
            public int QuantidadeConferida { get; set; }
            public int QuantidadeFaltante { get; set; }
            public bool Finalizado { get; set; }
            public bool EmBusca { get; set; }
            public List<int> ItemIds { get; set; } = new();

            public ConferenciaSeparacaoItemDto ToDto(bool atual)
            {
                return new ConferenciaSeparacaoItemDto
                {
                    ZonaId = ZonaId,
                    Zona = Zona,
                    ItemNr = ItemNr,
                    Descricao = Descricao,
                    QuantidadePedido = QuantidadePedido,
                    QuantidadeConferida = QuantidadeConferida,
                    QuantidadeFaltante = QuantidadeFaltante,
                    Atual = atual,
                    Finalizado = Finalizado,
                    EmBusca = EmBusca
                };
            }
        }

        private sealed class ConferenciaSeparacaoSnapshot
        {
            public static ConferenciaSeparacaoSnapshot Empty => new ConferenciaSeparacaoSnapshot();

            public bool Exists { get; set; }
            public int RomaneioId { get; set; }
            public string RomaneioNr { get; set; } = string.Empty;
            public ConferenciaSeparacaoItemGroup? CurrentItem { get; set; }
            public List<ConferenciaSeparacaoItemGroup> Items { get; set; } = new();

            public ConferenciaSeparacaoSnapshotDto ToDto()
            {
                return new ConferenciaSeparacaoSnapshotDto
                {
                    Finalizado = CurrentItem == null,
                    Mensagem = CurrentItem == null ? "Conferência finalizada com sucesso." : string.Empty,
                    ItemAtual = CurrentItem?.ToDto(true),
                    Itens = Items.Select(item => item.ToDto(CurrentItem != null && ReferenceEquals(item, CurrentItem))).ToList()
                };
            }
        }
    }
}
