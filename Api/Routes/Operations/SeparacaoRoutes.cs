using System.Collections.Concurrent;
using System.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using QuasarApi.DataBase;
using QuasarApi.Database.Models;
using QuasarApi.DTO.Operations.Separacao;
using QuasarApi.Helpers;

namespace QuasarApi.Routes.Operations
{
    public static class SeparacaoRoutes
    {
        private static readonly ConcurrentDictionary<string, HashSet<string>> SkippedLinesByTask = new();
        private const int StatusAguardandoSeparacao = 2;
        private const int StatusEmSeparacao = 3;
        private const int StatusSeparado = 8;

        public static WebApplication MapSeparacaoRoutes(this WebApplication app, WebApplicationBuilder builder)
        {
            const string groupPrefix = "/separacao";
            var group = app.MapGroup(groupPrefix);

            group.MapGet("/zonas", async (HttpContext httpContext, AppDbContext db) =>
            {
                var usuario = await ResolveCurrentUserAsync(httpContext, db);
                if (usuario == null)
                {
                    return Results.Unauthorized();
                }

                int? filialId = usuario.FilialId;

                var zonas = await db.Zona
                    .Where(z => z.Ativo && (!filialId.HasValue || z.FilialId == filialId || z.FilialId == null))
                    .Select(z => new ZonaDisponivelDto
                    {
                        Id = z.Id,
                        Nome = (z.Nome ?? string.Empty).Trim(),
                        Descricao = (z.Descricao ?? string.Empty).Trim(),
                        TarefasPendentes = db.RomaneioItem
                            .Where(ri =>
                                ri.ZonaId == z.Id &&
                                (!filialId.HasValue || ri.FilialId == filialId || ri.FilialId == null) &&
                                ri.StatusId == StatusAguardandoSeparacao &&
                                ((ri.SeparadorId ?? 0) == 0) &&
                                ri.TarefaNr != null &&
                                ri.TarefaNr != string.Empty)
                            .Count()
                    })
                    .Where(z => z.TarefasPendentes > 0)
                    .OrderBy(z => z.Nome)
                    .ToListAsync();

                return Results.Ok(zonas);
            }).RequireAuthorization();

            group.MapPost("/assumir-tarefa", async (HttpContext httpContext, [FromBody] AssumirTarefaRequestDto request, AppDbContext db) =>
            {
                if (request == null || request.ZonaId <= 0)
                {
                    return Results.BadRequest(new { mensagem = "Zona inválida." });
                }

                var usuario = await ResolveCurrentUserAsync(httpContext, db);
                if (usuario == null)
                {
                    return Results.Unauthorized();
                }

                int? filialId = usuario.FilialId;
                var zona = await db.Zona
                    .Where(z => z.Id == request.ZonaId && (!filialId.HasValue || z.FilialId == filialId || z.FilialId == null))
                    .Select(z => new { z.Id, z.Nome, z.Ativo })
                    .FirstOrDefaultAsync();

                if (zona == null || !zona.Ativo)
                {
                    return Results.NotFound(new { mensagem = "Zona não encontrada." });
                }

                string? tarefaEmAndamento = await GetTaskByOwnerAsync(db, filialId, request.ZonaId, usuario.Id);
                if (!string.IsNullOrWhiteSpace(tarefaEmAndamento))
                {
                    return Results.Conflict(new
                    {
                        mensagem = "Usuário já possui uma tarefa em andamento nesta zona. Finalize a tarefa atual antes de abrir outra instância."
                    });
                }

                string? tarefaNr = await TryAssumeNextTaskAsync(db, filialId, request.ZonaId, usuario.Id);
                if (string.IsNullOrWhiteSpace(tarefaNr))
                {
                    return Results.NotFound(new { mensagem = "Nenhuma tarefa disponível para a zona selecionada." });
                }

                return Results.Ok(new AssumirTarefaResponseDto
                {
                    TarefaNr = tarefaNr,
                    ZonaId = zona.Id,
                    ZonaNome = (zona.Nome ?? string.Empty).Trim(),
                    Reentrada = false
                });
            }).RequireAuthorization();

            group.MapGet("/tarefas/{tarefaNr}/linha-atual", async (HttpContext httpContext, string tarefaNr, AppDbContext db) =>
            {
                var usuario = await ResolveCurrentUserAsync(httpContext, db);
                if (usuario == null)
                {
                    return Results.Unauthorized();
                }

                var snapshot = await BuildTaskSnapshotAsync(db, usuario, tarefaNr);
                if (!snapshot.Exists)
                {
                    return Results.NotFound(new { mensagem = "Tarefa não encontrada para o usuário logado." });
                }

                if (snapshot.CurrentLine == null)
                {
                    return Results.Ok(new
                    {
                        finalizada = true,
                        mensagem = "Separação finalizada com sucesso.",
                        linha = (TarefaLinhaDto?)null
                    });
                }

                await EnsureLineStartDateAsync(db, snapshot.CurrentLine);

                return Results.Ok(new
                {
                    finalizada = false,
                    linha = snapshot.CurrentLine.ToDto()
                });
            }).RequireAuthorization();

            group.MapPost("/tarefas/{tarefaNr}/abandonar", async (HttpContext httpContext, string tarefaNr, AppDbContext db) =>
            {
                var usuario = await ResolveCurrentUserAsync(httpContext, db);
                if (usuario == null)
                {
                    return Results.Unauthorized();
                }

                string tarefa = (tarefaNr ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(tarefa))
                {
                    return Results.BadRequest(new { mensagem = "Tarefa inválida." });
                }

                int? filialId = usuario.FilialId;
                var itensEmSeparacao = await db.RomaneioItem
                    .Where(ri =>
                        ri.TarefaNr == tarefa &&
                        ri.SeparadorId == usuario.Id &&
                        ri.StatusId == StatusEmSeparacao &&
                        (!filialId.HasValue || ri.FilialId == filialId || ri.FilialId == null))
                    .ToListAsync();

                if (itensEmSeparacao.Count == 0)
                {
                    ClearSkippedLines(usuario.Id, tarefa);
                    return Results.Ok(new { liberada = false });
                }

                foreach (var item in itensEmSeparacao)
                {
                    item.StatusId = StatusAguardandoSeparacao;
                    item.SeparadorId = null;
                    item.DataSeparador = null;
                }

                await db.SaveChangesAsync();
                ClearSkippedLines(usuario.Id, tarefa);

                return Results.Ok(new { liberada = true });
            }).RequireAuthorization();

            group.MapPost("/tarefas/{tarefaNr}/passby-linha", async (HttpContext httpContext, string tarefaNr, AppDbContext db) =>
            {
                var usuario = await ResolveCurrentUserAsync(httpContext, db);
                if (usuario == null)
                {
                    return Results.Unauthorized();
                }

                var snapshot = await BuildTaskSnapshotAsync(db, usuario, tarefaNr);
                if (!snapshot.Exists)
                {
                    return Results.NotFound(new { mensagem = "Tarefa não encontrada para o usuário logado." });
                }

                var linhaAtual = snapshot.CurrentLine;
                if (linhaAtual == null)
                {
                    ClearSkippedLines(usuario.Id, tarefaNr);

                    return Results.Ok(new ConfirmarLinhaResponseDto
                    {
                        Finalizada = true,
                        Mensagem = "Separação finalizada com sucesso."
                    });
                }

                AddSkippedLine(usuario.Id, tarefaNr, linhaAtual.LineKey);

                var updatedSnapshot = await BuildTaskSnapshotAsync(db, usuario, tarefaNr);
                if (updatedSnapshot.CurrentLine == null)
                {
                    ClearSkippedLines(usuario.Id, tarefaNr);

                    return Results.Ok(new ConfirmarLinhaResponseDto
                    {
                        Finalizada = true,
                        Mensagem = "Separação finalizada com sucesso."
                    });
                }

                await EnsureLineStartDateAsync(db, updatedSnapshot.CurrentLine);

                return Results.Ok(new ConfirmarLinhaResponseDto
                {
                    Finalizada = false,
                    Mensagem = "Linha movida para o final da tarefa.",
                    ProximaLinha = updatedSnapshot.CurrentLine.ToDto()
                });
            }).RequireAuthorization();

            group.MapPost("/tarefas/{tarefaNr}/confirmar-linha", async (HttpContext httpContext, string tarefaNr, [FromBody] ConfirmarLinhaRequestDto request, AppDbContext db) =>
            {
                if (request == null)
                {
                    return Results.BadRequest(new { mensagem = "Dados de confirmação inválidos." });
                }

                var usuario = await ResolveCurrentUserAsync(httpContext, db);
                if (usuario == null)
                {
                    return Results.Unauthorized();
                }

                var snapshot = await BuildTaskSnapshotAsync(db, usuario, tarefaNr);
                if (!snapshot.Exists)
                {
                    return Results.NotFound(new { mensagem = "Tarefa não encontrada para o usuário logado." });
                }

                var linhaAtual = snapshot.CurrentLine;
                if (linhaAtual == null)
                {
                    return Results.Ok(new ConfirmarLinhaResponseDto
                    {
                        Finalizada = true,
                        Mensagem = "Separação finalizada com sucesso."
                    });
                }

                string locacaoInformada = NormalizeCode(request.LocacaoInformada);
                string locacaoEsperada = NormalizeCode(linhaAtual.Locacao);

                if (!string.Equals(locacaoInformada, locacaoEsperada, StringComparison.OrdinalIgnoreCase))
                {
                    return Results.BadRequest(new { mensagem = "Locação informada diferente da locação da tarefa." });
                }

                if (request.QuantidadeInformada <= 0)
                {
                    return Results.BadRequest(new { mensagem = "Informe uma quantidade separada válida." });
                }

                if (request.QuantidadeInformada > linhaAtual.QuantidadePendente)
                {
                    return Results.BadRequest(new
                    {
                        mensagem = $"Quantidade informada maior que o saldo pendente da linha ({linhaAtual.QuantidadePendente})."
                    });
                }

                var itemIds = linhaAtual.ItemIds.Distinct().ToList();
                var entities = await db.RomaneioItem
                    .Where(x => itemIds.Contains(x.Id))
                    .OrderBy(x => x.Id)
                    .ToListAsync();

                var agora = CurrentDateTime.GetCurrentDateTime();
                int quantidadeRestante = request.QuantidadeInformada;

                foreach (var entity in entities)
                {
                    if (quantidadeRestante <= 0)
                    {
                        break;
                    }

                    int quantidadeSolicitada = Math.Max(entity.Qtde ?? 0, 0);
                    int quantidadeJaSeparada = Math.Max(entity.QtdeSeparada ?? 0, 0);
                    int saldoItem = Math.Max(quantidadeSolicitada - quantidadeJaSeparada, 0);

                    if (saldoItem == 0)
                    {
                        entity.StatusId = StatusSeparado;
                        continue;
                    }

                    int quantidadeAplicada = Math.Min(quantidadeRestante, saldoItem);
                    entity.QtdeSeparada = quantidadeJaSeparada + quantidadeAplicada;
                    entity.StatusId = entity.QtdeSeparada >= quantidadeSolicitada
                        ? StatusSeparado
                        : StatusEmSeparacao;
                    entity.DataSeparador ??= agora;

                    quantidadeRestante -= quantidadeAplicada;
                }

                if (quantidadeRestante > 0)
                {
                    return Results.BadRequest(new { mensagem = "Não foi possível aplicar a quantidade informada na linha selecionada." });
                }

                await db.SaveChangesAsync();

                if (linhaAtual.QuantidadePendente - request.QuantidadeInformada <= 0)
                {
                    RemoveSkippedLine(usuario.Id, tarefaNr, linhaAtual.LineKey);
                }

                var updatedSnapshot = await BuildTaskSnapshotAsync(db, usuario, tarefaNr);
                if (updatedSnapshot.CurrentLine == null)
                {
                    ClearSkippedLines(usuario.Id, tarefaNr);

                    return Results.Ok(new ConfirmarLinhaResponseDto
                    {
                        Finalizada = true,
                        Mensagem = "Separação finalizada com sucesso."
                    });
                }

                await EnsureLineStartDateAsync(db, updatedSnapshot.CurrentLine);

                return Results.Ok(new ConfirmarLinhaResponseDto
                {
                    Finalizada = false,
                    Mensagem = "Quantidade registrada com sucesso.",
                    ProximaLinha = updatedSnapshot.CurrentLine.ToDto()
                });
            }).RequireAuthorization();

            group.MapGet("/tarefas/{tarefaNr}/status", async (HttpContext httpContext, string tarefaNr, AppDbContext db) =>
            {
                var usuario = await ResolveCurrentUserAsync(httpContext, db);
                if (usuario == null)
                {
                    return Results.Unauthorized();
                }

                var snapshot = await BuildTaskSnapshotAsync(db, usuario, tarefaNr);
                if (!snapshot.Exists)
                {
                    return Results.NotFound(new { mensagem = "Tarefa não encontrada para o usuário logado." });
                }

                return Results.Ok(new StatusTarefaDto
                {
                    TarefaNr = tarefaNr,
                    Finalizada = snapshot.CurrentLine == null,
                    TotalLinhas = snapshot.TotalLines,
                    LinhasSeparadas = snapshot.CompletedLines,
                    LinhasPendentes = Math.Max(snapshot.TotalLines - snapshot.CompletedLines, 0)
                });
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

        private static async Task<string?> GetTaskByOwnerAsync(AppDbContext db, int? filialId, int zonaId, int separadorId)
        {
            return await QuerySingleTaskNumberAsync(
                db,
                @"
SELECT TOP 1 ri.TarefaNr
  FROM RomaneioItem ri
 WHERE (@filialId IS NULL OR ri.FilialId = @filialId OR ri.FilialId IS NULL)
   AND ri.ZonaId = @zonaId
   AND ri.StatusId = @statusId
   AND ri.SeparadorId = @separadorId
   AND ISNULL(LTRIM(RTRIM(ri.TarefaNr)), '') <> ''
 GROUP BY ri.TarefaNr
 ORDER BY MIN(ISNULL(ri.CriadoEm, '19000101')),
          ri.TarefaNr",
                new[]
                {
                    new SqlParameter("@filialId", (object?)filialId ?? DBNull.Value),
                    new SqlParameter("@zonaId", zonaId),
                    new SqlParameter("@statusId", StatusEmSeparacao),
                    new SqlParameter("@separadorId", separadorId)
                });
        }

        private static async Task<string?> TryAssumeNextTaskAsync(AppDbContext db, int? filialId, int zonaId, int separadorId)
        {
            for (int tentativa = 0; tentativa < 3; tentativa++)
            {
                await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);

                string? tarefaNr = await QuerySingleTaskNumberAsync(
                    db,
                    @"
DECLARE @Task TABLE (TarefaNr VARCHAR(50));

;WITH tarefa AS
(
    SELECT TOP 1 ri.TarefaNr
      FROM RomaneioItem ri WITH (UPDLOCK, READPAST, ROWLOCK)
     WHERE (@filialId IS NULL OR ri.FilialId = @filialId OR ri.FilialId IS NULL)
       AND ri.ZonaId = @zonaId
       AND ri.StatusId = @statusAguardando
       AND ISNULL(ri.SeparadorId, 0) = 0
       AND ISNULL(LTRIM(RTRIM(ri.TarefaNr)), '') <> ''
     GROUP BY ri.TarefaNr
     ORDER BY MIN(ISNULL(ri.CriadoEm, '19000101')),
              ri.TarefaNr
)
UPDATE ri
   SET SeparadorId = @separadorId,
       StatusId = @statusEmSeparacao
OUTPUT inserted.TarefaNr INTO @Task (TarefaNr)
  FROM RomaneioItem ri
  INNER JOIN tarefa t
          ON t.TarefaNr = ri.TarefaNr
 WHERE (@filialId IS NULL OR ri.FilialId = @filialId OR ri.FilialId IS NULL)
   AND ri.ZonaId = @zonaId
   AND ri.StatusId = @statusAguardando
   AND ISNULL(ri.SeparadorId, 0) = 0;

SELECT TOP 1 TarefaNr
  FROM @Task;",
                    new[]
                    {
                        new SqlParameter("@filialId", (object?)filialId ?? DBNull.Value),
                        new SqlParameter("@zonaId", zonaId),
                        new SqlParameter("@statusAguardando", StatusAguardandoSeparacao),
                        new SqlParameter("@statusEmSeparacao", StatusEmSeparacao),
                        new SqlParameter("@separadorId", separadorId)
                    },
                    transaction);

                if (!string.IsNullOrWhiteSpace(tarefaNr))
                {
                    await transaction.CommitAsync();
                    return tarefaNr;
                }

                await transaction.RollbackAsync();
            }

            return null;
        }

        private static async Task<string?> QuerySingleTaskNumberAsync(
            AppDbContext db,
            string sql,
            IEnumerable<SqlParameter> parameters,
            IDbContextTransaction? transaction = null)
        {
            var connection = db.Database.GetDbConnection();
            bool shouldClose = false;

            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync();
                shouldClose = true;
            }

            try
            {
                await using var command = connection.CreateCommand();
                command.CommandText = sql;
                command.Transaction = transaction?.GetDbTransaction();

                foreach (var parameter in parameters)
                {
                    command.Parameters.Add(parameter);
                }

                object? result = await command.ExecuteScalarAsync();
                return result == null || result == DBNull.Value ? null : Convert.ToString(result)?.Trim();
            }
            finally
            {
                if (shouldClose)
                {
                    await connection.CloseAsync();
                }
            }
        }

        private static async Task<TaskSnapshot> BuildTaskSnapshotAsync(AppDbContext db, Usuario usuario, string tarefaNr)
        {
            string tarefa = (tarefaNr ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(tarefa))
            {
                return TaskSnapshot.Empty;
            }

            int? filialId = usuario.FilialId;
            var skippedLineKeys = GetSkippedLines(usuario.Id, tarefa);

            var rows = await (
                from ri in db.RomaneioItem
                join m in db.Material on ri.ItemNr equals m.Codigo into materialJoin
                from material in materialJoin.DefaultIfEmpty()
                join loc in db.Locacao on ri.LocacaoId equals loc.Id into locacaoJoin
                from locacao in locacaoJoin.DefaultIfEmpty()
                where (!filialId.HasValue || ri.FilialId == filialId || ri.FilialId == null)
                   && ri.TarefaNr == tarefa
                   && ri.SeparadorId == usuario.Id
                   && (ri.StatusId == StatusEmSeparacao || ri.StatusId == StatusSeparado)
                select new TaskRowRaw
                {
                    Id = ri.Id,
                    ItemNr = ri.ItemNr,
                    Descricao = !string.IsNullOrEmpty(ri.Descricao)
                        ? (ri.Descricao ?? string.Empty)
                        : (material != null ? (material.Descricao ?? string.Empty) : string.Empty),
                    Qtde = ri.Qtde ?? 0,
                    QtdeSeparada = ri.QtdeSeparada ?? 0,
                    StatusId = ri.StatusId ?? 0,
                    DataSeparador = ri.DataSeparador,
                    Locacao = locacao != null && !string.IsNullOrEmpty(locacao.Codigo)
                        ? (locacao.Codigo ?? string.Empty)
                        : (db.Estoque
                            .Where(e =>
                                e.ItemNr == ri.ItemNr &&
                                (ri.FilialId == null || e.FilialId == ri.FilialId || e.FilialId == null) &&
                                e.Locacao != null &&
                                e.Locacao != string.Empty)
                            .OrderBy(e => (e.Saldo ?? 0) > 0 ? 0 : 1)
                            .ThenByDescending(e => e.Saldo ?? 0)
                            .ThenBy(e => e.Locacao)
                            .Select(e => e.Locacao ?? string.Empty)
                            .FirstOrDefault() ?? string.Empty)
                }).ToListAsync();

            if (!rows.Any())
            {
                return TaskSnapshot.Empty;
            }

            var grouped = rows
                .GroupBy(x => new
                {
                    ItemNr = (x.ItemNr ?? string.Empty).Trim(),
                    Descricao = (x.Descricao ?? string.Empty).Trim(),
                    Locacao = (x.Locacao ?? string.Empty).Trim()
                })
                .Select(g => new TaskLineGroup
                {
                    TarefaNr = tarefa,
                    ItemNr = g.Key.ItemNr,
                    Descricao = g.Key.Descricao,
                    Locacao = g.Key.Locacao,
                    LineKey = BuildLineKey(g.Key.ItemNr, g.Key.Descricao, g.Key.Locacao),
                    QuantidadeSolicitada = g.Sum(x => x.Qtde),
                    QuantidadeSeparada = g.Sum(x => Math.Min(x.QtdeSeparada, x.Qtde)),
                    ItemIds = g.Select(x => x.Id).Distinct().ToList(),
                    MissingStartDateItemIds = g.Where(x => !x.DataSeparador.HasValue).Select(x => x.Id).Distinct().ToList(),
                    Separated = g.Sum(x => Math.Max(x.Qtde - Math.Min(x.QtdeSeparada, x.Qtde), 0)) == 0
                })
                .Select(x =>
                {
                    x.QuantidadePendente = Math.Max(x.QuantidadeSolicitada - x.QuantidadeSeparada, 0);
                    return x;
                })
                .OrderBy(x => x.Locacao)
                .ThenBy(x => x.ItemNr)
                .ThenBy(x => x.Descricao)
                .ToList();

            int completed = grouped.Count(x => x.Separated);
            var pendingLines = grouped.Where(x => !x.Separated).ToList();
            var current = pendingLines.FirstOrDefault(x => !skippedLineKeys.Contains(x.LineKey));
            if (current == null && pendingLines.Count > 0)
            {
                ClearSkippedLines(usuario.Id, tarefa);
                current = pendingLines.FirstOrDefault();
            }

            if (current != null)
            {
                current.LinhaAtual = grouped.FindIndex(x => ReferenceEquals(x, current)) + 1;
                current.TotalLinhas = grouped.Count;
                current.LinhasSeparadas = completed;
            }

            return new TaskSnapshot
            {
                Exists = true,
                TotalLines = grouped.Count,
                CompletedLines = completed,
                CurrentLine = current
            };
        }

        private static async Task EnsureLineStartDateAsync(AppDbContext db, TaskLineGroup line)
        {
            if (line.MissingStartDateItemIds == null || line.MissingStartDateItemIds.Count == 0)
            {
                return;
            }

            var agora = CurrentDateTime.GetCurrentDateTime();
            foreach (int itemId in line.MissingStartDateItemIds)
            {
                var entity = await db.RomaneioItem.FindAsync(itemId);
                if (entity != null && !entity.DataSeparador.HasValue)
                {
                    entity.DataSeparador = agora;
                }
            }

            await db.SaveChangesAsync();
            line.MissingStartDateItemIds.Clear();
        }

        private static string NormalizeCode(string? value)
        {
            return (value ?? string.Empty)
                .Replace(".", string.Empty)
                .Replace(" ", string.Empty)
                .Trim()
                .ToUpperInvariant();
        }

        private static string BuildTaskUserKey(int usuarioId, string tarefaNr)
        {
            return usuarioId.ToString() + "|" + (tarefaNr ?? string.Empty).Trim().ToUpperInvariant();
        }

        private static string BuildLineKey(string? itemNr, string? descricao, string? locacao)
        {
            return string.Join("|",
                (itemNr ?? string.Empty).Trim().ToUpperInvariant(),
                (descricao ?? string.Empty).Trim().ToUpperInvariant(),
                NormalizeCode(locacao));
        }

        private static HashSet<string> GetSkippedLines(int usuarioId, string tarefaNr)
        {
            var key = BuildTaskUserKey(usuarioId, tarefaNr);
            if (!SkippedLinesByTask.TryGetValue(key, out var skipped))
            {
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }

            lock (skipped)
            {
                return new HashSet<string>(skipped, StringComparer.OrdinalIgnoreCase);
            }
        }

        private static void AddSkippedLine(int usuarioId, string tarefaNr, string lineKey)
        {
            var key = BuildTaskUserKey(usuarioId, tarefaNr);
            var skipped = SkippedLinesByTask.GetOrAdd(key, _ => new HashSet<string>(StringComparer.OrdinalIgnoreCase));

            lock (skipped)
            {
                skipped.Add(lineKey);
            }
        }

        private static void RemoveSkippedLine(int usuarioId, string tarefaNr, string lineKey)
        {
            var key = BuildTaskUserKey(usuarioId, tarefaNr);
            if (!SkippedLinesByTask.TryGetValue(key, out var skipped))
            {
                return;
            }

            lock (skipped)
            {
                skipped.Remove(lineKey);
                if (skipped.Count == 0)
                {
                    SkippedLinesByTask.TryRemove(key, out _);
                }
            }
        }

        private static void ClearSkippedLines(int usuarioId, string tarefaNr)
        {
            var key = BuildTaskUserKey(usuarioId, tarefaNr);
            SkippedLinesByTask.TryRemove(key, out _);
        }

        private sealed class TaskRowRaw
        {
            public int Id { get; set; }
            public string ItemNr { get; set; } = string.Empty;
            public string Descricao { get; set; } = string.Empty;
            public string Locacao { get; set; } = string.Empty;
            public int Qtde { get; set; }
            public int QtdeSeparada { get; set; }
            public int StatusId { get; set; }
            public DateTime? DataSeparador { get; set; }
        }

        private sealed class TaskLineGroup
        {
            public string TarefaNr { get; set; } = string.Empty;
            public string ItemNr { get; set; } = string.Empty;
            public string Descricao { get; set; } = string.Empty;
            public string Locacao { get; set; } = string.Empty;
            public string LineKey { get; set; } = string.Empty;
            public int QuantidadeSolicitada { get; set; }
            public int QuantidadeSeparada { get; set; }
            public int QuantidadePendente { get; set; }
            public List<int> ItemIds { get; set; } = new();
            public List<int> MissingStartDateItemIds { get; set; } = new();
            public bool Separated { get; set; }
            public int LinhaAtual { get; set; }
            public int TotalLinhas { get; set; }
            public int LinhasSeparadas { get; set; }

            public TarefaLinhaDto ToDto()
            {
                return new TarefaLinhaDto
                {
                    TarefaNr = TarefaNr,
                    ItemNr = ItemNr,
                    Descricao = Descricao,
                    Locacao = Locacao,
                    QuantidadeSolicitada = QuantidadeSolicitada,
                    QuantidadeSeparada = QuantidadeSeparada,
                    QuantidadePendente = QuantidadePendente,
                    LinhaAtual = LinhaAtual,
                    TotalLinhas = TotalLinhas,
                    LinhasSeparadas = LinhasSeparadas
                };
            }
        }

        private sealed class TaskSnapshot
        {
            public static TaskSnapshot Empty => new TaskSnapshot();

            public bool Exists { get; set; }
            public int TotalLines { get; set; }
            public int CompletedLines { get; set; }
            public TaskLineGroup? CurrentLine { get; set; }
        }
    }
}
