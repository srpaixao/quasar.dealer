using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Security.Claims;

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
            group.MapGet("/consultaritem/{itemnr}", async (string itemnr, HttpContext httpContext, AppDbContext db) =>
            {
                try
                {
                    var usuario = await ResolveCurrentUserAsync(httpContext, db);
                    if (usuario == null)
                        return Results.Unauthorized();
                    if (!usuario.FilialId.HasValue)
                        return Results.BadRequest(new { mensagem = "Usuario sem filial configurada." });

                    var filialId = usuario.FilialId.Value;
                    var item = await (from m in db.Material
                                      where m.Codigo == itemnr
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
                                         orderby (e.Saldo ?? 0) > 0 descending, e.Saldo descending
                                         select e).FirstOrDefaultAsync();

                    if (estoque != null)
                    {
                        item.EstoqueCadastrado = true;
                        item.Locacao = estoque.Locacao ?? string.Empty;
                        item.Saldo = estoque.Saldo ?? 0;
                        item.Indisponivel = estoque.Indisponivel ?? 0;
                        item.PedidoPendente = estoque.PedidoPendente ?? 0;
                        item.FilialId = estoque.FilialId ?? 0;
                    }

                    item.MovimentacaoCorreta = await ExigeMovimentacaoCorretaAsync(
                        db,
                        estoque?.FilialId);

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
            group.MapGet("/consultarlocacao/{codigo}", async (string codigo, HttpContext httpContext, AppDbContext db) =>
            {
                string codigolocacao = codigo.Replace(".", "").Replace(" ", "");

                try
                {
                    var usuario = await ResolveCurrentUserAsync(httpContext, db);
                    if (usuario == null)
                        return Results.Unauthorized();
                    if (!usuario.FilialId.HasValue)
                        return Results.BadRequest(new { mensagem = "Usuario sem filial configurada." });

                    var filialId = usuario.FilialId.Value;
                    var locacao = await (from l in db.Locacao
                                         where l.FilialId == filialId
                                               && l.Codigo.Replace(".", "").Replace(" ", "") == codigolocacao
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
                                             Observacoes = l.Observacoes ?? string.Empty
                                         }).FirstOrDefaultAsync();

                    if (locacao == null)
                    {
                        return Results.NotFound(new { mensagem = "Locação não cadastrada" });
                    }

                    // Obter nome da Área
                    if (locacao.AreaId.HasValue)
                    {
                        locacao.Area = await db.Area
                            .Where(a => a.Id == locacao.AreaId.Value)
                            .Select(a => a.Descricao)
                            .FirstOrDefaultAsync() ?? string.Empty;
                    }

                    // Obter nome do Equipamento
                    if (locacao.EquipamentoId.HasValue)
                    {
                        locacao.Equipamento = await db.Equipamento
                            .Where(e => e.Id == locacao.EquipamentoId.Value)
                            .Select(e => e.Descricao)
                            .FirstOrDefaultAsync() ?? string.Empty;
                    }

                    // Itens
                    locacao.Itens = await (from m in db.Material
                                           join e in db.Estoque on m.Codigo equals e.ItemNr into resultado
                                           from e in resultado.DefaultIfEmpty()
                                           where e != null &&
                                                 e.FilialId == filialId &&
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

            // Validar uma locacao sem carregar os itens de estoque associados.
            group.MapGet("/validarlocacao/{codigo}", async (string codigo, HttpContext httpContext, AppDbContext db) =>
            {
                var usuario = await ResolveCurrentUserAsync(httpContext, db);
                if (usuario == null)
                    return Results.Unauthorized();
                if (!usuario.FilialId.HasValue)
                    return Results.BadRequest(new { mensagem = "Usuario sem filial configurada." });

                var filialId = usuario.FilialId.Value;
                var codigoNormalizado = NormalizeCode(codigo);
                if (string.IsNullOrWhiteSpace(codigoNormalizado))
                    return Results.BadRequest(new { mensagem = "Locacao e obrigatoria." });

                var locacao = await db.Locacao
                    .AsNoTracking()
                    .Where(l => l.FilialId == filialId
                                && l.Codigo.Replace(".", "").Replace(" ", "").ToUpper() == codigoNormalizado)
                    .Select(l => new
                    {
                        l.Codigo,
                        l.Descricao,
                        l.Tipo,
                        l.Bloqueado,
                        l.FilialId
                    })
                    .FirstOrDefaultAsync();

                if (locacao == null)
                    return Results.NotFound(new { mensagem = "Locação não cadastrada." });

                if (locacao.Bloqueado)
                    return Results.BadRequest(new { mensagem = "Locacao bloqueada." });

                if (!string.Equals(locacao.Tipo?.Trim(), "E", StringComparison.OrdinalIgnoreCase))
                    return Results.BadRequest(new { mensagem = "A locação informada não é do tipo Espera." });

                return Results.Ok(locacao);
            }).RequireAuthorization();

            // Listar todos os itens pendentes vinculados a uma Locacao de Espera.
            group.MapGet("/locacao-espera/{codigo}/movimentacoes", async (string codigo, HttpContext httpContext, AppDbContext db) =>
            {
                var usuario = await ResolveCurrentUserAsync(httpContext, db);
                if (usuario == null)
                    return Results.Unauthorized();
                if (!usuario.FilialId.HasValue)
                    return Results.BadRequest(new { mensagem = "Usuario sem filial configurada." });

                var filialId = usuario.FilialId.Value;
                var codigoNormalizado = NormalizeCode(codigo);
                if (string.IsNullOrWhiteSpace(codigoNormalizado))
                    return Results.BadRequest(new { mensagem = "Locacao de Espera e obrigatoria." });

                var locacao = await db.Locacao
                    .AsNoTracking()
                    .Where(l => l.FilialId == filialId
                                && l.Codigo.Replace(".", "").Replace(" ", "").ToUpper() == codigoNormalizado)
                    .Select(l => new { l.Codigo, l.Tipo, l.Bloqueado, l.FilialId })
                    .FirstOrDefaultAsync();

                if (locacao == null)
                    return Results.NotFound(new { mensagem = "Locação de Espera não cadastrada." });

                if (locacao.Bloqueado)
                    return Results.BadRequest(new { mensagem = "Locacao de Espera bloqueada." });

                if (!string.Equals(locacao.Tipo?.Trim(), "E", StringComparison.OrdinalIgnoreCase))
                    return Results.BadRequest(new { mensagem = "A locação informada não é do tipo Espera." });

                var itensBase = await (from movimento in db.Movimentacao.AsNoTracking()
                                       join material in db.Material.AsNoTracking()
                                           on movimento.ItemNr equals material.Codigo
                                       where movimento.FinalizadoEm == null
                                             && movimento.FilialId == filialId
                                             && movimento.LocacaoEspera != null
                                             && movimento.LocacaoEspera.Replace(".", "").Replace(" ", "").ToUpper() == codigoNormalizado
                                       orderby movimento.CriadoEm, movimento.ItemNr
                                       select new MovimentacaoLocacaoEspera
                                       {
                                           Id = movimento.Id,
                                           ItemNr = movimento.ItemNr,
                                           Descricao = material.Descricao ?? string.Empty,
                                           UN = material.UN ?? string.Empty,
                                           LocacaoOrigem = movimento.LocacaoOrigem ?? string.Empty,
                                           LocacaoEspera = movimento.LocacaoEspera ?? string.Empty,
                                           FilialId = movimento.FilialId,
                                           Quantidade = movimento.QtdOrigem ?? 0,
                                           CriadoEm = movimento.CriadoEm
                                       }).ToListAsync();

                var itemNrs = itensBase.Select(i => i.ItemNr).Distinct().ToList();
                var destinosBase = await db.MovimentacaoDestino
                    .AsNoTracking()
                    .Where(d => d.FilialId == filialId && itemNrs.Contains(d.ItemNr))
                    .Select(d => new { d.ItemNr, d.Locacao })
                    .ToListAsync();

                var destinos = destinosBase
                    .GroupBy(d => d.ItemNr)
                    .ToDictionary(g => g.Key, g => g.Select(d => d.Locacao).FirstOrDefault() ?? string.Empty);

                foreach (var item in itensBase)
                {
                    item.LocacaoDestino = destinos.TryGetValue(item.ItemNr, out var destino)
                        ? destino.Trim()
                        : string.Empty;
                }

                itensBase = itensBase
                    .OrderBy(item => string.IsNullOrWhiteSpace(item.LocacaoDestino) ? 1 : 0)
                    .ThenBy(item => item.LocacaoDestino)
                    .ThenBy(item => item.ItemNr)
                    .ToList();

                return Results.Ok(new LocacaoEsperaResumo
                {
                    LocacaoEspera = locacao.Codigo.Trim(),
                    MovimentacaoCorreta = await ExigeMovimentacaoCorretaAsync(db, locacao.FilialId),
                    Itens = itensBase
                });
            }).RequireAuthorization();

            // Consultar Movimentacao (itemnr)
            group.MapGet("/consultarmovimentacao/{itemnr}", async (string itemnr, HttpContext httpContext, AppDbContext db) =>
            {
                try
                {
                    var usuario = await ResolveCurrentUserAsync(httpContext, db);
                    if (usuario == null)
                        return Results.Unauthorized();
                    if (!usuario.FilialId.HasValue)
                        return Results.BadRequest(new { mensagem = "Usuario sem filial configurada." });

                    var filialId = usuario.FilialId.Value;
                    var item = await (from m in db.Movimentacao
                                     join mat in db.Material on m.ItemNr equals mat.Codigo
                                      where m.ItemNr == itemnr
                                            && m.FilialId == filialId
                                            && m.FinalizadoEm == null
                                      select new ConsultarMovimentacao
                                      {
                                          Id = m.Id,
                                          ItemNr = m.ItemNr,
                                          Descricao = mat.Descricao ?? string.Empty,
                                          UN = mat.UN ?? string.Empty,
                                           LocacaoOrigem = m.LocacaoOrigem,
                                           QtdOrigem = m.QtdOrigem,
                                           LocacaoEspera = m.LocacaoEspera,
                                           LocacaoDestino = m.LocacaoDestino,
                                           QtdDestino = m.QtdDestino,
                                          FilialId = m.FilialId,
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
                        .Where(md => md.ItemNr == itemnr && md.FilialId == filialId)
                        .Select(md => md.Locacao)
                        .FirstOrDefaultAsync();

                    if (string.IsNullOrWhiteSpace(destino))
                    {
                        return Results.NotFound(new { mensagem = "Locação Destino não definida para este item" });
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
            group.MapPost("movimentacao", async ([FromBody] CriarMovimentacao request, HttpContext httpContext, AppDbContext db) =>
            {
                if (string.IsNullOrWhiteSpace(request.ItemNr))
                    return Results.BadRequest(new { mensagem = "ItemNr é obrigatório." });

                if (string.IsNullOrWhiteSpace(request.LocacaoEspera))
                    return Results.BadRequest(new { mensagem = "Locação de Espera é obrigatória." });

                if (request.QtdOrigem == null || request.QtdOrigem <= 0)
                    return Results.BadRequest(new { mensagem = "Quantidade deve ser maior que zero." });

                try
                {
                    var usuario = await ResolveCurrentUserAsync(httpContext, db);
                    if (usuario == null)
                        return Results.Unauthorized();
                    if (!usuario.FilialId.HasValue)
                        return Results.BadRequest(new { mensagem = "Usuario sem filial configurada." });

                    var filialId = usuario.FilialId.Value;
                    var itemNr = request.ItemNr.Trim();
                    var locacao = request.LocacaoOrigem?.Trim();
                    var locacaoOrigemNormalizada = NormalizeCode(locacao);
                    var locacaoEsperaNormalizada = NormalizeCode(request.LocacaoEspera);
                    var qtde = request.QtdOrigem ?? 0;
                    var criadoPor = string.IsNullOrWhiteSpace(request.CriadoPor) ? "Quasar" : request.CriadoPor.Trim();

                    await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

                    var estoqueOrigem = await db.Estoque
                        .Where(e => e.ItemNr == itemNr
                                    && e.FilialId == filialId
                                    && e.Locacao != null
                                    && e.Locacao.Replace(".", "").Replace(" ", "").ToUpper() == locacaoOrigemNormalizada)
                        .FirstOrDefaultAsync();

                    // O estoque é consolidado por filial/item. Se a localização recebida
                    // estiver desatualizada, reutiliza a linha existente em vez de duplicá-la.
                    estoqueOrigem ??= await db.Estoque
                        .Where(e => e.ItemNr == itemNr && e.FilialId == filialId)
                        .OrderByDescending(e => e.Saldo)
                        .FirstOrDefaultAsync();

                    var itemNovoNoEstoque = estoqueOrigem == null;

                    var locacaoEspera = await db.Locacao
                        .Where(l => l.FilialId == filialId
                                    && l.Codigo.Replace(".", "").Replace(" ", "").ToUpper() == locacaoEsperaNormalizada)
                        .FirstOrDefaultAsync();

                    if (locacaoEspera == null)
                        return Results.BadRequest(new { mensagem = "Locação de Espera não cadastrada." });

                    if (locacaoEspera.Bloqueado)
                        return Results.BadRequest(new { mensagem = "Locação de Espera bloqueada." });

                    if (!string.Equals(locacaoEspera.Tipo?.Trim(), "E", StringComparison.OrdinalIgnoreCase))
                        return Results.BadRequest(new { mensagem = "A locação informada não é do tipo Espera." });

                    var exigeMovimentacaoCorreta = await ExigeMovimentacaoCorretaAsync(
                        db,
                        filialId);

                    if (!itemNovoNoEstoque)
                    {
                        if (exigeMovimentacaoCorreta && (estoqueOrigem!.Saldo ?? 0) != qtde)
                            return Results.BadRequest(new { mensagem = "A quantidade coletada deve ser igual ao saldo." });

                        if (estoqueOrigem!.FilialId.HasValue && locacaoEspera.FilialId.HasValue
                            && estoqueOrigem.FilialId != locacaoEspera.FilialId)
                            return Results.BadRequest(new { mensagem = "Locação de Espera pertence a outra filial." });
                    }

                    // Verifica se já existe movimentação não finalizada para o mesmo ItemNr
                    var existente = await db.Movimentacao
                        .Where(m => m.ItemNr == itemNr
                                    && m.FilialId == filialId
                                    && m.FinalizadoEm == null)
                        .FirstOrDefaultAsync();

                    if (existente != null)
                    {
                        return Results.BadRequest(new { mensagem = "Já existe movimentação não finalizada para este item." });
                    }

                    var coletadoEm = CurrentDateTime.GetCurrentDateTime();
                    if (itemNovoNoEstoque)
                    {
                        estoqueOrigem = new QuasarApi.Database.Models.Estoque
                        {
                            ItemNr = itemNr,
                            Locacao = locacaoEspera.Codigo.Trim(),
                            Saldo = 0,
                            Indisponivel = 0,
                            PedidoPendente = 0,
                            CriadoPor = criadoPor,
                            CriadoEm = coletadoEm,
                            FilialId = locacaoEspera.FilialId
                        };
                        db.Estoque.Add(estoqueOrigem);
                    }
                    else
                    {
                        estoqueOrigem!.Locacao = locacaoEspera.Codigo.Trim();
                        estoqueOrigem.ModificadoPor = criadoPor;
                        estoqueOrigem.ModificadoEm = coletadoEm;
                    }

                    var entity = new Movimentacao
                    {
                        ItemNr = itemNr,
                        LocacaoOrigem = locacao,
                        QtdOrigem = qtde,
                        LocacaoEspera = locacaoEspera.Codigo.Trim(),
                        FilialId = filialId,
                        CriadoPor = criadoPor,
                        CriadoEm = coletadoEm
                    };

                    db.Movimentacao.Add(entity);
                    await db.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return Results.Created($"{groupPrefix}/movimentacao/{entity.Id}", new
                    {
                        entity.Id,
                        entity.ItemNr,
                        entity.LocacaoOrigem,
                        entity.LocacaoEspera,
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
            group.MapPut("/movimentacao/{id:int}", async ([FromRoute] int id, [FromBody] FinalizarMovimentacao request, HttpContext httpContext, AppDbContext db) =>
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
                    var usuario = await ResolveCurrentUserAsync(httpContext, db);
                    if (usuario == null)
                        return Results.Unauthorized();
                    if (!usuario.FilialId.HasValue)
                        return Results.BadRequest(new { mensagem = "Usuario sem filial configurada." });

                    var filialId = usuario.FilialId.Value;
                    if (string.IsNullOrWhiteSpace(request.LocacaoDestino))
                        return Results.BadRequest(new { mensagem = "Locação de destino é obrigatória." });

                    await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

                    var entity = await db.Movimentacao
                        .Where(m => m.Id == id && m.FilialId == filialId)
                        .FirstOrDefaultAsync();
                    if (entity == null)
                        return Results.NotFound(new { mensagem = "Movimentação não encontrada." });

                    if (entity.FinalizadoEm != null)
                        return Results.BadRequest(new { mensagem = "Movimentação já finalizada." });

                    var quantidadeDisponivel = entity.QtdOrigem ?? 0;
                    var quantidadeTransferida = request.QtdDestino ?? 0;
                    if (quantidadeTransferida > quantidadeDisponivel)
                        return Results.BadRequest(new { mensagem = "Quantidade transferida maior que o saldo da Locação de Espera." });

                    var tipoLocacaoEspera = await db.Locacao
                        .AsNoTracking()
                        .Where(l => l.FilialId == filialId
                                    && entity.LocacaoEspera != null
                                    && l.Codigo.Replace(".", "").Replace(" ", "").ToUpper()
                                       == entity.LocacaoEspera.Replace(".", "").Replace(" ", "").ToUpper())
                        .Select(l => l.Tipo)
                        .FirstOrDefaultAsync();

                    if (tipoLocacaoEspera == null)
                        return Results.BadRequest(new { mensagem = "Locação de Espera não cadastrada para a filial do usuário." });

                    if (!string.Equals(tipoLocacaoEspera.Trim(), "E", StringComparison.OrdinalIgnoreCase))
                        return Results.BadRequest(new { mensagem = "A locação informada não é do tipo Espera." });

                    var exigeMovimentacaoCorreta = await ExigeMovimentacaoCorretaAsync(db, filialId);
                    if (exigeMovimentacaoCorreta && quantidadeTransferida != quantidadeDisponivel)
                        return Results.BadRequest(new { mensagem = "A quantidade transferida deve ser igual a quantidade coletada." });

                    entity.FilialId ??= filialId;

                    var destinoNormalizado = NormalizeCode(request.LocacaoDestino);
                    var destinoConfigurado = await db.MovimentacaoDestino
                        .AsNoTracking()
                        .Where(md => md.ItemNr == entity.ItemNr && md.FilialId == filialId)
                        .Select(md => md.Locacao)
                        .FirstOrDefaultAsync();

                    if (string.IsNullOrWhiteSpace(destinoConfigurado))
                        return Results.BadRequest(new { mensagem = "Locação final não definida para este item." });

                    if (NormalizeCode(destinoConfigurado) != destinoNormalizado)
                        return Results.BadRequest(new { mensagem = "Locação final divergente da configuração do item." });

                    var finalizadoPor = string.IsNullOrWhiteSpace(request.FinalizadoPor) ? "Quasar" : request.FinalizadoPor.Trim();
                    var finalizadoEm = CurrentDateTime.GetCurrentDateTime();

                    var quantidadeRestante = quantidadeDisponivel - quantidadeTransferida;
                    var estoqueItem = await db.Estoque
                        .Where(e => e.ItemNr == entity.ItemNr && e.FilialId == filialId)
                        .OrderByDescending(e => e.Saldo)
                        .FirstOrDefaultAsync();

                    if (estoqueItem == null)
                    {
                        estoqueItem = new QuasarApi.Database.Models.Estoque
                        {
                            ItemNr = entity.ItemNr,
                            Locacao = quantidadeRestante > 0
                                ? entity.LocacaoEspera?.Trim()
                                : destinoConfigurado.Trim(),
                            Saldo = 0,
                            Indisponivel = 0,
                            PedidoPendente = 0,
                            FilialId = filialId,
                            CriadoPor = finalizadoPor,
                            CriadoEm = finalizadoEm
                        };
                        db.Estoque.Add(estoqueItem);
                    }
                    else
                    {
                        // Com saldo pendente, o item continua fisicamente na espera.
                        if (quantidadeRestante == 0)
                            estoqueItem.Locacao = destinoConfigurado.Trim();

                        estoqueItem.ModificadoPor = finalizadoPor;
                        estoqueItem.ModificadoEm = finalizadoEm;
                    }

                    entity.LocacaoDestino = destinoConfigurado.Trim();
                    entity.QtdDestino = quantidadeTransferida;

                    entity.FinalizadoPor = finalizadoPor;
                    entity.FinalizadoEm = finalizadoEm;

                    entity.UrlDMS = request.UrlDMS?.Trim();
                    entity.Payload = request.Payload?.Trim();
                    entity.Response = request.Response?.Trim();

                    Movimentacao? movimentacaoPendente = null;
                    if (quantidadeRestante > 0)
                    {
                        movimentacaoPendente = new Movimentacao
                        {
                            ItemNr = entity.ItemNr,
                            LocacaoOrigem = entity.LocacaoOrigem,
                            QtdOrigem = quantidadeRestante,
                            LocacaoEspera = entity.LocacaoEspera,
                            FilialId = entity.FilialId ?? filialId,
                            CriadoPor = entity.CriadoPor,
                            CriadoEm = entity.CriadoEm ?? finalizadoEm
                        };
                        db.Movimentacao.Add(movimentacaoPendente);
                    }

                    await db.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return Results.Ok(new
                    {
                        entity.Id,
                        entity.ItemNr,
                        entity.LocacaoEspera,
                        entity.LocacaoDestino,
                        entity.QtdDestino,
                        QuantidadeRestante = quantidadeRestante,
                        MovimentacaoPendenteId = movimentacaoPendente?.Id,
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

        private static string NormalizeCode(string? value)
        {
            return (value ?? string.Empty)
                .Replace(".", string.Empty)
                .Replace(" ", string.Empty)
                .Trim()
                .ToUpperInvariant();
        }

        private static async Task<bool> ExigeMovimentacaoCorretaAsync(AppDbContext db, int? filialId)
        {
            var configuracoes = await db.AppConfig
                .AsNoTracking()
                .Where(config => config.Nome == "MovimentacaoCorreta"
                                 && (config.FilialId == filialId || config.FilialId == null))
                .Select(config => new { config.FilialId, config.Valor })
                .ToListAsync();

            var valor = configuracoes
                .OrderByDescending(config => filialId.HasValue && config.FilialId == filialId)
                .Select(config => config.Valor)
                .FirstOrDefault();

            valor ??= await db.AppConfig
                .AsNoTracking()
                .Where(config => config.Nome == "MovimentacaoCorreta")
                .Select(config => config.Valor)
                .FirstOrDefaultAsync();

            return valor != null &&
                   (valor.Trim().Equals("true", StringComparison.OrdinalIgnoreCase)
                    || valor.Trim() == "1"
                    || valor.Trim().Equals("sim", StringComparison.OrdinalIgnoreCase)
                   || valor.Trim().Equals("s", StringComparison.OrdinalIgnoreCase));
        }

        private static async Task<Usuario?> ResolveCurrentUserAsync(HttpContext httpContext, AppDbContext db)
        {
            string? userIdValue = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdValue, out int userId))
                return await db.Usuario.AsNoTracking().FirstOrDefaultAsync(usuario => usuario.Id == userId);

            string? login = httpContext.User.Identity?.Name;
            return string.IsNullOrWhiteSpace(login)
                ? null
                : await db.Usuario.AsNoTracking().FirstOrDefaultAsync(usuario => usuario.Login == login);
        }
    }
}
