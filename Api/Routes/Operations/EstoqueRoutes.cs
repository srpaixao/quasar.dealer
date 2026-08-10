using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using QuasarApi.DataBase;
using QuasarApi.Database.Models;
using QuasarApi.DTO.Operations.Estoque;
using QuasarApi.Helpers;

namespace QuasarApi.Routes.Operations
{
    public static class EstoqueRoutes
    {
        public static WebApplication MapEstoqueRoutes(this WebApplication app, WebApplicationBuilder builder)
        {
            const string groupPrefix = "/estoque";
            var group = app.MapGroup(groupPrefix);

            // Consultar dados do material (itemnr)
            group.MapGet("/consultaritem/{itemnr}", async (string itemnr, [FromQuery] int? filialId, AppDbContext db) =>
            {
                try
                {
                    var item = await (from m in db.Material
                                      where m.Codigo == itemnr //&& m.FilialId == filialId
                                      select new ConsultarItem
                                      {
                                          ItemNr = m.Codigo,
                                          Descricao = m.Descricao ?? string.Empty,
                                          UN = m.UN ?? string.Empty,
                                          Curva = m.Curva ?? string.Empty,
                                          ItemCritico = m.ItemCritico,
                                      }).FirstOrDefaultAsync();

                    if (item == null)
                    {
                        return Results.NotFound(new { mensagem = "Item não cadastrado" });
                    }

                    // Obter dados do estoque
                    var estoque = await (from e in db.Estoque
                                         where e.ItemNr == item.ItemNr && e.FilialId == filialId
                                         select e).FirstOrDefaultAsync();

                    if (estoque != null)
                    {
                        item.Locacao = estoque.Locacao ?? string.Empty;
                        item.Saldo = estoque.Saldo ?? 0;
                        item.Indisponivel = estoque.Indisponivel ?? 0;
                        item.PedidoPendente = estoque.PedidoPendente ?? 0;
                        item.FilialId = estoque.FilialId ?? 0;
                    }

                    return Results.Ok(item);
                }
                catch (Exception ex)
                {
                    var problemDetails = new ProblemDetails
                    {
                        Title = "Ocorreu um erro ao processar a solicitação",
                        Status = 500,
                        Detail = ex.Message,
                        Instance = $"{groupPrefix}/consultaritem/{{itemnr}}"
                    };
                    return Results.Problem(problemDetails);
                }

            }).RequireAuthorization();

            // Consultar dados da locacao
            group.MapGet("/consultarlocacao/{codigo}", async (string codigo, [FromQuery] int? filialId, AppDbContext db) =>
            {
                string codigolocacao = codigo.Replace(".", "").Replace(" ", "");

                try
                {
                    var locacao = await (from l in db.Locacao
                                         where l.Codigo.Replace(".", "").Replace(" ", "") == codigolocacao
                                               && l.FilialId == filialId
                                         select new ConsultarLocacao
                                         {
                                             Codigo = l.Codigo,
                                             Tipo = l.Tipo ?? string.Empty,
                                             Descricao = l.Descricao ?? string.Empty,
                                             Bloqueado = l.Bloqueado,
                                             AreaId = l.AreaId,
                                             EquipamentoId = l.EquipamentoId,
                                             Curva = l.Curva ?? string.Empty,
                                             Estrategia = l.Estrategia ?? string.Empty,
                                             Observacoes = l.Observacoes ?? string.Empty,
                                             FilialId = l.FilialId
                                         }).FirstOrDefaultAsync();

                    if (locacao == null)
                    {
                        return Results.NotFound(new { mensagem = "Locação não cadastrada" });
                    }

                    // Obter nome da Área
                    if (locacao.AreaId.HasValue)
                    {
                        locacao.Area = await db.Area
                            .Where(a => a.Id == locacao.AreaId.Value && a.FilialId == filialId)
                            .Select(a => a.Descricao)
                            .FirstOrDefaultAsync() ?? string.Empty;
                    }

                    // Obter nome do Equipamento
                    if (locacao.EquipamentoId.HasValue)
                    {
                        locacao.Equipamento = await db.Equipamento
                            .Where(e => e.Id == locacao.EquipamentoId.Value && e.FilialId == filialId)
                            .Select(e => e.Descricao)
                            .FirstOrDefaultAsync() ?? string.Empty;
                    }

                    // Itens
                    locacao.Itens = await (from m in db.Material
                                           join e in db.Estoque on m.Codigo equals e.ItemNr into resultado
                                           from e in resultado.DefaultIfEmpty()
                                           where e != null &&
                                                 //m.FilialId == filialId &&
                                                 e.Locacao != null &&
                                                 e.Locacao.Replace(".", "").Replace(" ", "") == codigolocacao
                                           select new ConsultarItem
                                           {
                                               ItemNr = m.Codigo,
                                               Descricao = m.Descricao,
                                               UN = m.UN,
                                               Locacao = e.Locacao ?? string.Empty,
                                               Saldo = e.Saldo,
                                               Indisponivel = e.Indisponivel,
                                               PedidoPendente = e.PedidoPendente,
                                               Curva = m.Curva,
                                               ItemCritico = m.ItemCritico,
                                               FilialId = e.FilialId
                                           }).ToListAsync();


                    return Results.Ok(locacao);
                }
                catch (Exception ex)
                {
                    var problemDetails = new ProblemDetails
                    {
                        Title = "Ocorreu um erro ao processar a solicitação",
                        Status = 500,
                        Detail = ex.Message,
                        Instance = $"{groupPrefix}/consultarlocacao/{{codigo}}"
                    };
                    return Results.Problem(problemDetails);
                }

            }).RequireAuthorization();

            // Consultar Movimentacao (itemnr)
            group.MapGet("/consultarmovimentacao/{itemnr}", async (string itemnr, [FromQuery] int? filialId, AppDbContext db) =>
            {
                try
                {
                    var item = await (from m in db.Movimentacao
                                      join mat in db.Material on m.ItemNr equals mat.Codigo
                                      where m.ItemNr == itemnr &&
                                            m.FinalizadoEm == null &&
                                            m.FilialId == filialId
                                      select new ConsultarMovimentacao
                                      {
                                          Id = m.Id,
                                          ItemNr = m.ItemNr,
                                          Descricao = mat.Descricao ?? string.Empty,
                                          UN = mat.UN ?? string.Empty,
                                          LocacaoOrigem = m.LocacaoOrigem,
                                          QtdOrigem = m.QtdOrigem,
                                          LocacaoDestino = m.LocacaoDestino,
                                          QtdDestino = m.QtdDestino,
                                          CriadoPor = m.CriadoPor,
                                          CriadoEm = m.CriadoEm,
                                          FinalizadoPor = m.FinalizadoPor,
                                          FinalizadoEm = m.FinalizadoEm
                                      }).FirstOrDefaultAsync();

                    if (item == null)
                    {
                        return Results.NotFound(new { mensagem = "Item não disponível para transferência" });
                    }

                    var destino = await db.MovimentacaoDestino
                        .Where(md => md.ItemNr == itemnr)
                        .Select(md => md.Locacao)
                        .FirstOrDefaultAsync();

                    if (string.IsNullOrWhiteSpace(destino))
                    {
                        return Results.NotFound(new { mensagem = "Loca��o Destino n�o definida para este item" });
                    }

                    item.LocacaoDestino = destino?.Trim();

                    return Results.Ok(item);
                }
                catch (Exception ex)
                {
                    var problemDetails = new ProblemDetails
                    {
                        Title = "Ocorreu um erro ao processar a solicitação",
                        Status = 500,
                        Detail = ex.Message,
                        Instance = $"{groupPrefix}/consultarmovimentacao/{{itemnr}}"
                    };
                    return Results.Problem(problemDetails);
                }

            }).RequireAuthorization();

            // Registrar Movimentacao (coleta)
            group.MapPost("movimentacao", async ([FromBody] CriarMovimentacao request, AppDbContext db) =>
            {
                if (string.IsNullOrWhiteSpace(request.ItemNr))
                    return Results.BadRequest(new { mensagem = "ItemNr é obrigatório." });

                if (string.IsNullOrWhiteSpace(request.LocacaoOrigem))
                    return Results.BadRequest(new { mensagem = "Locação é obrigatória." });

                if (request.QtdOrigem == null || request.QtdOrigem <= 0)
                    return Results.BadRequest(new { mensagem = "Quantidade deve ser maior que zero." });

                try
                {
                    var itemNr = request.ItemNr.Trim();
                    var locacao = request.LocacaoOrigem?.Trim();
                    var qtde = request.QtdOrigem ?? 0;
                    var criadoPor = string.IsNullOrWhiteSpace(request.CriadoPor) ? "Quasar" : request.CriadoPor.Trim();

                    // Verifica se já existe movimentação não finalizada para o mesmo ItemNr
                    var existente = await db.Movimentacao
                        .Where(m => m.ItemNr == itemNr && m.FinalizadoEm == null && m.FilialId == request.FilialId)
                        .FirstOrDefaultAsync();

                    if (existente != null)
                    {
                        return Results.BadRequest(new { mensagem = "Já existe movimentação não finalizada para este item." });
                    }

                    var entity = new Movimentacao
                    {
                        ItemNr = itemNr,
                        LocacaoOrigem = locacao,
                        QtdOrigem = qtde,
                        CriadoPor = criadoPor,
                        CriadoEm = CurrentDateTime.GetCurrentDateTime(),
                        FilialId = request.FilialId
                    };

                    db.Movimentacao.Add(entity);
                    await db.SaveChangesAsync();

                    return Results.Created($"{groupPrefix}/movimentacao/{entity.Id}", new
                    {
                        entity.Id,
                        entity.ItemNr,
                        entity.LocacaoOrigem,
                        entity.QtdOrigem
                    });
                }
                catch (Exception ex)
                {
                    var problemDetails = new ProblemDetails
                    {
                        Title = "Erro ao registrar movimentação",
                        Status = 500,
                        Detail = ex.Message,
                        Instance = $"{groupPrefix}/movimentacao"
                    };
                    return Results.Problem(problemDetails);
                }
            }).RequireAuthorization();

            // Finalizar Movimentacao (transferência)
            group.MapPut("/movimentacao/{id:int}", async ([FromRoute] int id, [FromBody] FinalizarMovimentacao request, AppDbContext db) =>
            {
                if (request == null)
                    return Results.BadRequest(new { mensagem = "Corpo da requisicao invalido." });

                if (request.Id != 0 && request.Id != id)
                    return Results.BadRequest(new { mensagem = "Identificador da movimentacao divergente." });

                request.Id = id;

                if (request.QtdDestino == null || request.QtdDestino <= 0)
                    return Results.BadRequest(new { mensagem = "Quantidade de destino deve ser maior que zero." });

                try
                {
                    var entity = await db.Movimentacao
                        .FirstOrDefaultAsync(m => m.Id == id && m.FilialId == request.FilialId);
                    if (entity == null)
                        return Results.NotFound(new { mensagem = "Movimentação não encontrada." });

                    if (entity.FinalizadoEm != null)
                        return Results.BadRequest(new { mensagem = "Movimentação já finalizada." });

                    entity.LocacaoDestino = request.LocacaoDestino?.Trim();

                     // Permite movimentação somente de quantidades de Coleta = Transferência
                     // Através do flag na tabela APPConfig
                    bool movimentacaoCorreta = await db.AppConfig
                        .Where(m => m.Nome == "MovimentacaoCorreta")
                        .Select(m => m.Valor ?? false)
                        .FirstOrDefaultAsync();

                    if (entity.QtdOrigem != request.QtdDestino && movimentacaoCorreta)
                         return Results.BadRequest(new { mensagem = "Quantidade diferente da Coletada!" });
                    else
                        entity.QtdDestino = request.QtdDestino;

                    if (request.FilialId.HasValue)
                        entity.FilialId = request.FilialId;

                    entity.FinalizadoPor = string.IsNullOrWhiteSpace(request.FinalizadoPor) ? "Quasar" : request.FinalizadoPor.Trim();
                    entity.FinalizadoEm = CurrentDateTime.GetCurrentDateTime();

                    entity.UrlDMS = request.UrlDMS?.Trim();
                    entity.Payload = request.Payload?.Trim();
                    entity.Response = request.Response?.Trim();

                    await db.SaveChangesAsync();

                    return Results.Ok(new
                    {
                        entity.Id,
                        entity.ItemNr,
                        entity.LocacaoDestino,
                        entity.QtdDestino,
                        entity.FinalizadoPor,
                        entity.FinalizadoEm,
                        entity.UrlDMS,
                        entity.Payload,
                        entity.Response
                    });
                }
                catch (Exception ex)
                {
                    var problemDetails = new ProblemDetails
                    {
                        Title = "Erro ao finalizar movimentação",
                        Status = 500,
                        Detail = ex.Message,
                        Instance = $"{groupPrefix}/movimentacao/{id}"
                    };
                    return Results.Problem(problemDetails);
                }
            }).RequireAuthorization();

            return app;
        }
    }
}
