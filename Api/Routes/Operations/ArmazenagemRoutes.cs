using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using QuasarApi.Database.Models;
using QuasarApi.DataBase;
using QuasarApi.DTO.Operations.Recebimento.Armazenagem;
using QuasarApi.Helpers;

namespace QuasarApi.Routes.Operations
{
    using System.Security.Claims;

    public static class ArmazenagemRoutes
    {
        public static WebApplication MapArmazenagemRoutes(this WebApplication app, WebApplicationBuilder builder)
        {
            const string groupPrefix = "/armazenagem";
            var group = app.MapGroup(groupPrefix);

            // Obter material
            group.MapGet("/validarmaterial/{codigo}", async (string codigo, AppDbContext db) =>
            {
                try
                {
                    var material = await (from m in db.Material
                                          join e in db.Estoque on m.Codigo equals e.ItemNr
                                          into resultado
                                          from e in resultado.DefaultIfEmpty()
                                          where m.Codigo == codigo
                                          select new ValidarMaterial
                                          {
                                              CodigoMaterial = m.Codigo,
                                              Descricao = m.Descricao,
                                              Locacao = e.Locacao ?? string.Empty
                                          }).FirstOrDefaultAsync();


                    if (material == null)
                    {
                        return Results.NotFound(new { mensagem = "Item não cadastrado" });
                    }

                    //if (!string.IsNullOrEmpty(material.Locacao) && (material.Locacao.StartsWith("P") || material.Locacao.StartsWith("S")))
                    //if (!string.IsNullOrEmpty(material.Locacao))
                    //{
                    //    material.LocacaoFormatada = StringUtils.FormatarLocacao(material.Locacao);
                    //}
                    //else
                    //{
                    //    material.LocacaoFormatada = material.Locacao;
                    //}
                    material.LocacaoFormatada = material.Locacao.Replace(" ", "").Replace(".", "");


                    return Results.Ok(material);
                }
                catch (Exception ex)
                {
                    var problemDetails = new ProblemDetails
                    {
                        Title = "Ocorreu um erro ao processar a solicitação",
                        Status = 500,
                        Detail = ex.Message,
                        Instance = $"{groupPrefix}/validarmaterial/{{codigo}}"
                    };
                    return Results.Problem(problemDetails);
                }

            }).RequireAuthorization();

            // Obter quantidade de peças para armazenar
            group.MapGet("/validarquantidade/{codigo}", async (string codigo, AppDbContext db) =>
            {
                decimal qtdeDisponivelArmazenar = 0;

                // Obter quantidade dos itens 
                try
                {
                    decimal qtde_notafiscal = await (from nf in db.NotaFiscal
                                                     join nfi in db.NotaFiscalItem on nf.Id equals nfi.NotaFiscalId
                                                     where nfi.StatusId == 4 && nfi.Item.ToLower() == codigo.ToLower()
                                                     select nfi.Quantidade - (nfi.QtdArmazenada ?? 0.0M)).SumAsync();

                    qtdeDisponivelArmazenar = +qtde_notafiscal;
                }
                catch (Exception ex)
                {
                    var problemDetails = new ProblemDetails
                    {
                        Title = "Ocorreu um erro ao processar a solicitação",
                        Status = 500,
                        Detail = ex.Message,
                        Instance = $"{groupPrefix}/validarquantidade/{{codigo}} => qtdNF"
                    };
                    return Results.Problem(problemDetails);
                }

                // Obter quantidade de retorno
                try
                {
                    decimal retorno = await (from r in db.RetornoInternoItem
                                             where r.ItemNr != null && r.ItemNr.ToLower() == codigo.ToLower()
                                             select (r.Quantidade ?? 0.0M) - (r.QtdArmazenada ?? 0.0M)).SumAsync();

                    qtdeDisponivelArmazenar += retorno;
                }
                catch (Exception ex)
                {
                    var problemDetails = new ProblemDetails
                    {
                        Title = "Ocorreu um erro ao processar a solicitação",
                        Status = 500,
                        Detail = ex.Message,
                        Instance = $"{groupPrefix}/validarquantidade/{{codigo}} => qtdRetorno"
                    };
                    return Results.Problem(problemDetails);
                }

                ValidarQuantidade result = new ValidarQuantidade
                {
                    CodigoMaterial = codigo,
                    Quantidade = qtdeDisponivelArmazenar
                };

                return Results.Ok(result);

            }).RequireAuthorization();

            // Atualizar quantidades armazenadas
            group.MapPost("/atualizarItemNotaFiscal", async (HttpContext httpContext, AppDbContext db, ArmazenarItem postedItem) =>
            {
                var usuario = await ResolveCurrentUserAsync(httpContext, db);
                if (usuario == null)
                    return Results.Unauthorized();

                if (!usuario.FilialId.HasValue)
                    return Results.BadRequest(new { mensagem = "O usuario nao possui filial configurada." });

                if (postedItem.Quantidade <= 0)
                    return Results.BadRequest(new { mensagem = "A quantidade armazenada deve ser maior que zero." });

                using (var transaction = await db.Database.BeginTransactionAsync())
                {
                    try
                    {
                        var itens_nf = await (from nf in db.NotaFiscal
                                              join nfi in db.NotaFiscalItem on nf.Id equals nfi.NotaFiscalId
                                              where nfi.Item.ToLower() == postedItem.ItemNr.ToLower() &&
                                                    nfi.FilialId == usuario.FilialId.Value &&
                                                    nfi.StatusId == 4 &&
                                                    nfi.Quantidade > 0.0M
                                              select nfi).ToListAsync();

                        foreach (var itemnf in itens_nf)
                        {
                            if (postedItem.Quantidade <= 0)
                            {
                                break;
                            }

                            if (itemnf.Quantidade > postedItem.Quantidade)
                            {
                                itemnf.QtdArmazenada = itemnf.QtdArmazenada ?? 0.0M;
                                itemnf.QtdArmazenada = itemnf.QtdArmazenada + postedItem.Quantidade;
                                if (itemnf.QtdArmazenada > itemnf.Quantidade)
                                {
                                    postedItem.Quantidade = (decimal)itemnf.QtdArmazenada - itemnf.Quantidade;
                                    itemnf.QtdArmazenada = itemnf.Quantidade;
                                    itemnf.StatusId = 7;
                                }
                                else
                                {
                                    postedItem.Quantidade = postedItem.Quantidade - itemnf.Quantidade;
                                }
                            }
                            else
                            {
                                itemnf.QtdArmazenada = itemnf.Quantidade;
                                itemnf.StatusId = 7;
                                postedItem.Quantidade = postedItem.Quantidade - itemnf.Quantidade;
                            }

                            DateTime agora = CurrentDateTime.GetCurrentDateTime();
                            itemnf.UsuarioArmazenagem = usuario.Login;
                            itemnf.DtHrArmazenagem = agora;
                            itemnf.ModificadoPor = usuario.Login;
                            itemnf.ModificadoEm = agora;
                            await db.SaveChangesAsync();


                            bool itensFinalizados = db.NotaFiscalItem
                                                      .Where(item => item.NotaFiscalId == itemnf.NotaFiscalId)
                                                      .All(item => item.StatusId == 7);

                            if (itensFinalizados)
                            {
                                NotaFiscal? notafiscal = db.NotaFiscal.Find(itemnf.NotaFiscalId);
                                if (notafiscal != null)
                                {
                                    notafiscal.StatusId = 7;
                                    db.Entry(notafiscal).State = EntityState.Modified;
                                    db.SaveChanges();
                                }

                            }
                        }

                        var itens_retorno = await (from r in db.RetornoInternoItem
                                                   orderby r.Id
                                                   where r.ItemNr != null &&
                                                         r.ItemNr.ToLower() == postedItem.ItemNr.ToLower() &&
                                                         r.StatusRetornoId == 4 &&
                                                         r.Quantidade > 0.0M
                                                   select r).ToListAsync();

                        foreach (var itemret in itens_retorno)
                        {
                            if (postedItem.Quantidade <= 0)
                            {
                                break;
                            }

                            if (itemret.Quantidade > postedItem.Quantidade)
                            {
                                itemret.Quantidade = itemret.Quantidade ?? 0.0M;
                                itemret.QtdArmazenada = itemret.QtdArmazenada ?? 0.0M;
                                itemret.QtdArmazenada = itemret.QtdArmazenada + postedItem.Quantidade;
                                if (itemret.QtdArmazenada > itemret.Quantidade)
                                {
                                    postedItem.Quantidade = (decimal)itemret.QtdArmazenada - (decimal)itemret.Quantidade;
                                    itemret.QtdArmazenada = itemret.Quantidade;
                                    itemret.StatusRetornoId = 7;
                                }
                                else
                                {
                                    postedItem.Quantidade = postedItem.Quantidade - (decimal)itemret.Quantidade;
                                }
                            }
                            else
                            {
                                itemret.QtdArmazenada = itemret.Quantidade;
                                itemret.StatusRetornoId = 7;
                                postedItem.Quantidade = postedItem.Quantidade - (itemret.Quantidade ?? 0);
                            }

                            itemret.ModificadoPor = postedItem.Usuario;
                            itemret.ModificadoEm = CurrentDateTime.GetCurrentDateTime();
                            await db.SaveChangesAsync();


                            bool itensFinalizados = db.RetornoInternoItem
                                                      .Where(item => item.RetornoInternoId == itemret.RetornoInternoId)
                                                      .All(item => item.StatusRetornoId == 7);

                            if (itensFinalizados)
                            {
                                RetornoInterno? retorno = db.RetornoInterno.Find(itemret.RetornoInternoId);
                                if (retorno != null)
                                {
                                    retorno.FinalizadoEm = CurrentDateTime.GetCurrentDateTime(); 
                                    db.Entry(retorno).State = EntityState.Modified;
                                    db.SaveChanges();
                                }

                            }
                        }

                        await transaction.CommitAsync();
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        var problemDetails = new ProblemDetails
                        {
                            Title = "Ocorreu um erro ao processar a solicitação",
                            Status = 500,
                            Detail = ex.Message,
                            Instance = $"{groupPrefix}/atualizarItemNotaFiscal/{postedItem.ItemNr}"
                        };
                        return Results.Problem(problemDetails);
                    }
                }

                return Results.Ok(new { Message = "Item armazenado com sucesso" });

            }).RequireAuthorization();

            // Gravar Histórico de Armazenagem
            group.MapPost("/gravarHistorico", async (HttpContext httpContext, AppDbContext db, RegistrarHistorico postedHistorico) =>
            {
                using (var transaction = await db.Database.BeginTransactionAsync())
                {
                    try
                    {
                        HistoricoArmazenagem historico = new HistoricoArmazenagem();
                        historico.ItemNr = postedHistorico.ItemNr;
                        historico.Descricao = postedHistorico.Descricao;
                        historico.Locacao = postedHistorico.Locacao;
                        historico.LocacaoConfirmada = postedHistorico.LocacaoConfirmada;
                        historico.Quantidade = postedHistorico.Quantidade;
                        historico.DataHora = CurrentDateTime.GetCurrentDateTime();
                        historico.Erro = postedHistorico.Erro;
                        historico.Mensagem = postedHistorico.Mensagem;
                        historico.Usuario = postedHistorico.Usuario;
                        historico.FilialId = postedHistorico.FilialId;
                        db.HistoricoArmazenagem.Add(historico);
                        db.SaveChanges();
                        await transaction.CommitAsync();
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        var problemDetails = new ProblemDetails
                        {
                            Title = "Ocorreu um erro ao processar a solicitação",
                            Status = 500,
                            Detail = ex.Message,
                            Instance = $"{groupPrefix}/gravarHistorico/{postedHistorico.ItemNr}"
                        };
                        return Results.Problem(problemDetails);
                    }
                }

                return Results.Ok(new { Message = "Histórico registrado com sucesso" });

            }).RequireAuthorization();

            return app;
        }

        private static async Task<Usuario?> ResolveCurrentUserAsync(HttpContext httpContext, AppDbContext db)
        {
            string? userIdValue = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdValue, out int userId))
                return await db.Usuario.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId);

            string? login = httpContext.User.Identity?.Name;
            return string.IsNullOrWhiteSpace(login)
                ? null
                : await db.Usuario.AsNoTracking().FirstOrDefaultAsync(x => x.Login == login);
        }
    }
}
