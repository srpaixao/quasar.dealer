using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuasarApi.Database.Models;
using QuasarApi.DataBase;
using QuasarApi.Services.Interfaces;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace QuasarApi.Routes.Operations
{
    public static class ExpedicaoRoutes
    {
        public static WebApplication MapExpedicaoRoutes(this WebApplication app, WebApplicationBuilder builder)
        {
            const string groupPrefix = "/expedicao";
            var group = app.MapGroup(groupPrefix);

            // GET /transportadoras?filialId={int?}
            // Retorna lista de transportadoras habilitadas para expedição (emitiretiqueta = true) 
            group.MapGet("/transportadoras", async ([FromQuery] int? filialId, AppDbContext db) =>
            {
                try
                {
                    var query = db.Transportadora.AsQueryable();

                    var list = await query
                        .Where(t => t.EmitirEtiqueta == true && t.FilialId == filialId)
                        .Where(t => db.DocExpedicao
                                .Where(d => d.TransportadoraId == t.Id &&
                                            d.FilialId == filialId)
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

            // GET /expedicao/volumes/resumo?transportadoraId=1
            // Retorna resumo de volumes por transportadora
            group.MapGet("/volumes/resumo/{transportadoraId}", async (int transportadoraId, [FromQuery] int? filialId, AppDbContext db) =>
            {
                if (transportadoraId <= 0)
                    return Results.BadRequest(new { mensagem = "Id da transportadora não informado" });

                try
                {
                    var pendentes = (from d in db.DocExpedicao
                                     where d.TransportadoraId == transportadoraId &&
                                           d.FilialId == filialId
                                     select new
                                     {
                                         d.QtdVolumes,
                                         QtdVolConf = d.QtdVolConf ?? 0
                                     }).Sum(x => (int?)x.QtdVolumes) - (from d in db.DocExpedicao
                                                                        where d.TransportadoraId == transportadoraId &&
                                                                              d.FilialId == filialId
                                                                        select d.QtdVolConf).Sum() ?? 0;
                    var total = db.DocExpedicao
                        .Where(d => d.TransportadoraId == transportadoraId &&
                                    d.FilialId == filialId)
                        .Sum(d => (int?)d.QtdVolumes) ?? 0;

                    var lidos = db.DocExpedicao
                        .Where(d => d.TransportadoraId == transportadoraId &&
                                    d.FilialId == filialId)
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

            // GET /expedicao/volumes/pendentes/{transportadoraId}
            group.MapGet("/volumes/pendentes/{transportadoraId}", async (int transportadoraId, [FromQuery] int? filialId, AppDbContext db) =>
            {
                if (transportadoraId <= 0)
                    return Results.BadRequest(new { mensagem = "Id da transportadora não informado" });

                try
                {
                    var lista = await db.DocExpedicao
                        .Where(d => d.TransportadoraId == transportadoraId &&
                                    d.FilialId == filialId)
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

            // GET /expedicao/volumes/lidos/{transportadoraId}
            group.MapGet("/volumes/lidos/{transportadoraId}", async (int transportadoraId, [FromQuery] int? filialId, AppDbContext db) =>
            {
                if (transportadoraId <= 0)
                    return Results.BadRequest(new { mensagem = "Id da transportadora não informado" });

                try
                {
                    var lista = await (
                        from h in db.HistoricoDespacho
                        join d in db.DocExpedicao
                            on new { Numero = h.NotaFiscalNr, h.TransportadoraId }
                            equals new { d.Numero, d.TransportadoraId }
                        where h.TransportadoraId == transportadoraId &&
                              d.FilialId == filialId
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

            // GET /expedicao/doc?numero={string}&transportadoraId={int}
            // Retorna dados do documento de expedição
            group.MapGet("/doc", async ([FromQuery] string numero, [FromQuery] int transportadoraId, [FromQuery] int? filialId, AppDbContext db) =>
            {
                if (string.IsNullOrWhiteSpace(numero)) return Results.BadRequest(new { mensagem = "Informe o número da nota fiscal." });

                try
                {
                    //var transportadora = await db.Transportadora.FirstOrDefaultAsync(t => t.Id == transportadoraId);
                    //if (transportadora == null)
                    //{
                    //    return Results.NotFound(new { mensagem = "Transportadora não encontrada." });
                    //}

                    IQueryable<DocExpedicao> query = db.DocExpedicao
                        .Where(d => d.TransportadoraId == transportadoraId && d.FilialId == filialId);
                    //if (transportadora.EmitirEtiqueta)
                    //{
                    query = query.Where(d => d.Numero == numero);
                    //}
                    //else
                    //{
                    //    query = query.Where(d => d.Controle == numero);
                    //}

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
                    var problemDetails = new ProblemDetails { Title = "Erro ao obter documento de expedição", Status = 500, Detail = ex.Message, Instance = $"{groupPrefix}/doc" };
                    return Results.Problem(problemDetails);
                }
            }).RequireAuthorization();

            // GET /expedicao/historico/volumes?notaFiscalNr={string}
            // Retorna a lista de volumes despachados para uma nota fiscal
            group.MapGet("/historico/volumes", async ([FromQuery] string notaFiscalNr, [FromQuery] int? filialId, AppDbContext db) =>
            {
                if (string.IsNullOrWhiteSpace(notaFiscalNr)) return Results.BadRequest(new { mensagem = "Informe a notaFiscalNr." });
                try
                {
                    var volumes = await (from h in db.HistoricoDespacho
                                         join d in db.DocExpedicao
                                             on new { Numero = h.NotaFiscalNr, h.TransportadoraId }
                                             equals new { d.Numero, d.TransportadoraId }
                                         where h.NotaFiscalNr == notaFiscalNr &&
                                               d.FilialId == filialId
                                         orderby h.VolumeNr
                                         select h.VolumeNr)
                                         .ToListAsync();

                    return Results.Ok(new { volumes });
                }
                catch (Exception ex)
                {
                    var problemDetails = new ProblemDetails { Title = "Erro ao listar volumes", Status = 500, Detail = ex.Message, Instance = $"{groupPrefix}/historico/volumes" };
                    return Results.Problem(problemDetails);
                }
            }).RequireAuthorization();

            // POST /expedicao/historico
            // Grava volume despachado
            group.MapPost("/historico", async ([FromQuery] int? filialId, [FromBody] HistoricoDespacho request, AppDbContext db) =>
            {
                if (string.IsNullOrWhiteSpace(request.NotaFiscalNr) || string.IsNullOrWhiteSpace(request.VolumeNr))
                {
                    return Results.BadRequest(new { mensagem = "Dados inválidos." });
                }

                await using var transaction = await db.Database.BeginTransactionAsync(); try
                {
                    // Normalizar usuário
                    if (string.IsNullOrWhiteSpace(request.CriadoPor))
                    {
                        request.CriadoPor = "Quasar";
                    }

                    request.CriadoEm = DateTime.UtcNow;

                    // Inserir histórico
                    db.HistoricoDespacho.Add(request);
                    await db.SaveChangesAsync();

                    // Atualizar QtdVolConf na tabela DocExpedicao
                    var docExpedicao = await db.DocExpedicao.FirstOrDefaultAsync(d =>
                        d.Numero == request.NotaFiscalNr &&
                        d.TransportadoraId == request.TransportadoraId &&
                        d.FilialId == filialId);
                    if (docExpedicao != null)
                    {
                        docExpedicao.QtdVolConf = (docExpedicao.QtdVolConf ?? 0) + 1;
                        await db.SaveChangesAsync();
                    }

                    // Confirmar transação
                    await transaction.CommitAsync();

                    return Results.Created($"{groupPrefix}/historico/{request.Id}", new { id = request.Id });
                }
                catch (Exception ex)
                {
                    // Reverter transação
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
    }
}
