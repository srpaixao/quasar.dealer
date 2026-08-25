using Microsoft.AspNetCore.Mvc;
using QuasarApi.DTO.Operations.Recebimento.Conferencia;
using QuasarApi.Services.Interfaces;

namespace QuasarApi.Routes.Operations
{
    using System.Data;
    using System.Security.Claims;
    using Microsoft.EntityFrameworkCore;
    using QuasarApi.DataBase;
    using QuasarApi.Database.Models;
    using QuasarApi.Helpers;

    public static class RecebimentoRoutes
    {
        public static WebApplication MapRecebimentoRoutes(this WebApplication app, WebApplicationBuilder builder)
        {
            const string groupPrefix = "/recebimento";
            var group = app.MapGroup(groupPrefix);

            group.MapGet("/volumeresumo/{statusId}/{areaId}", async (int statusId, int areaId, HttpContext httpContext, AppDbContext db, [FromServices] IVolumeService service) =>
            {
                var usuario = await ResolveCurrentUserAsync(httpContext, db);
                if (usuario == null)
                    return Results.Unauthorized();

                if (!usuario.FilialId.HasValue)
                    return Results.BadRequest(new { mensagem = "O usuario nao possui filial configurada." });

                var volumes = await service.ResumoVolumesAsync(statusId, areaId, usuario.FilialId.Value);
                return Results.Ok(volumes);
            }).RequireAuthorization();

            group.MapPost("/volumeupdate", async (UpdateVolumeRequestDto request, HttpContext httpContext, AppDbContext db, [FromServices] IConferenciaVolumeService service) =>
            {
                var usuario = await ResolveCurrentUserAsync(httpContext, db);
                if (usuario == null)
                    return Results.Unauthorized();

                if (!usuario.FilialId.HasValue)
                    return Results.BadRequest(new { mensagem = "O usuario nao possui filial configurada." });

                var result = await service.UpdateVolumeAsync(request, usuario.FilialId.Value, usuario.Login);
                return Results.Json(result);
            }).RequireAuthorization();

            group.MapGet("/conferencia-volume/{volume}", async (string volume, HttpContext httpContext, AppDbContext db) =>
            {
                var usuario = await ResolveCurrentUserAsync(httpContext, db);
                if (usuario == null)
                    return Results.Unauthorized();

                if (!usuario.FilialId.HasValue)
                    return Results.BadRequest(new { mensagem = "O usuario nao possui filial configurada." });

                string volumeNormalizado = (volume ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(volumeNormalizado))
                    return Results.BadRequest(new { mensagem = "Volume nao informado." });

                int filialId = usuario.FilialId.Value;
                var itens = await (from item in db.NotaFiscalItem.AsNoTracking()
                                   join nota in db.NotaFiscal.AsNoTracking() on item.NotaFiscalId equals nota.Id
                                   join materialBase in db.Material.AsNoTracking() on item.Item equals materialBase.Codigo into materiais
                                   from material in materiais.DefaultIfEmpty()
                                   where item.FilialId == filialId
                                      && nota.FilialId == filialId
                                      && item.Volume != null
                                      && item.Volume.Trim() == volumeNormalizado
                                   orderby nota.Numero, item.Item, item.Id
                                   select new ConferenciaVolumeItemDto
                                   {
                                       Id = item.Id,
                                       NotaFiscalId = nota.Id,
                                       NotaFiscal = nota.Numero,
                                       Item = item.Item,
                                       ItemCritico = material != null && material.ItemCritico,
                                       ObservacaoItemCritico = material != null && material.ItemCritico
                                           ? material.ObsItemCritico
                                           : null,
                                       Volume = item.Volume ?? string.Empty,
                                       Pedido = item.Pedido ?? string.Empty,
                                       Quantidade = item.Quantidade,
                                       QtdConferida = item.QtdConferida,
                                       QtdArmazenada = item.QtdArmazenada,
                                       Diferenca = item.QtdConferida.HasValue ? item.QtdConferida.Value - item.Quantidade : null,
                                       Conferido = item.Conferido,
                                       UsuarioConferencia = item.UsuarioConferencia,
                                       DtHrConferencia = item.DtHrConferencia,
                                       UsuarioArmazenagem = item.UsuarioArmazenagem,
                                       DtHrArmazenagem = item.DtHrArmazenagem,
                                       ModificadoEm = item.ModificadoEm
                                   }).ToListAsync();

                if (itens.Count == 0)
                    return Results.NotFound(new { mensagem = "Volume nao localizado para a filial do usuario." });

                foreach (var item in itens)
                    item.Situacao = ObterSituacao(item.Conferido, item.QtdConferida, item.Quantidade);

                return Results.Ok(new ConferenciaVolumeDetalheDto { Volume = volumeNormalizado, Itens = itens });
            }).RequireAuthorization();

            group.MapPost("/conferencia-volume/{volume}/itens/{itemId:int}/confirmar", async (
                string volume,
                int itemId,
                ConfirmarConferenciaItemRequestDto request,
                HttpContext httpContext,
                AppDbContext db) =>
            {
                var usuario = await ResolveCurrentUserAsync(httpContext, db);
                if (usuario == null)
                    return Results.Unauthorized();

                if (!usuario.FilialId.HasValue)
                    return Results.BadRequest(new { mensagem = "O usuario nao possui filial configurada." });
                if (!request.Conferido)
                    return Results.BadRequest(new { mensagem = "O flag Conferido deve ser confirmado para finalizar." });
                if (!request.QtdConferida.HasValue)
                    return Results.BadRequest(new { mensagem = "Informe a quantidade conferida." });
                if (request.QtdConferida.Value < 0)
                    return Results.BadRequest(new { mensagem = "A quantidade conferida nao pode ser negativa." });

                string volumeNormalizado = (volume ?? string.Empty).Trim();
                int filialId = usuario.FilialId.Value;
                await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

                var itemComMaterial = await (from candidato in db.NotaFiscalItem
                                             join nota in db.NotaFiscal on candidato.NotaFiscalId equals nota.Id
                                             join materialBase in db.Material.AsNoTracking() on candidato.Item equals materialBase.Codigo into materiais
                                             from material in materiais.DefaultIfEmpty()
                                             where candidato.Id == itemId
                                                && candidato.FilialId == filialId
                                                && nota.FilialId == filialId
                                                && candidato.Volume != null
                                                && candidato.Volume.Trim() == volumeNormalizado
                                             select new
                                             {
                                                 Item = candidato,
                                                 ItemCritico = material != null && material.ItemCritico,
                                                 ObservacaoItemCritico = material != null && material.ItemCritico
                                                     ? material.ObsItemCritico
                                                     : null
                                             }).SingleOrDefaultAsync();

                var item = itemComMaterial?.Item;

                if (item == null)
                    return Results.NotFound(new { mensagem = "Item ou volume nao localizado para a filial do usuario." });

                if (item.ModificadoEm != request.ModificadoEmEsperado)
                {
                    return Results.Json(
                        new { mensagem = "O item foi alterado durante a operacao. Recarregue o volume antes de continuar." },
                        statusCode: StatusCodes.Status409Conflict);
                }

                if (item.Conferido && !string.Equals(item.UsuarioConferencia, usuario.Login, StringComparison.OrdinalIgnoreCase))
                {
                    return Results.Json(
                        new { mensagem = $"O item ja foi conferido por {item.UsuarioConferencia ?? "outro usuario"}." },
                        statusCode: StatusCodes.Status409Conflict);
                }

                decimal diferenca = request.QtdConferida.Value - item.Quantidade;
                if (diferenca != 0 && !request.ConfirmarDivergencia)
                {
                    return Results.Json(
                        new { mensagem = "A divergencia deve ser confirmada explicitamente antes da finalizacao." },
                        statusCode: StatusCodes.Status409Conflict);
                }

                DateTime agora = CurrentDateTime.GetCurrentDateTime();
                item.QtdConferida = request.QtdConferida.Value;
                item.Conferido = true;
                item.UsuarioConferencia = usuario.Login;
                item.DtHrConferencia = agora;
                item.ModificadoPor = usuario.Login;
                item.ModificadoEm = agora;

                await db.SaveChangesAsync();
                await transaction.CommitAsync();

                return Results.Ok(new ConferenciaVolumeItemDto
                {
                    Id = item.Id,
                    NotaFiscalId = item.NotaFiscalId,
                    Item = item.Item,
                    ItemCritico = itemComMaterial!.ItemCritico,
                    ObservacaoItemCritico = itemComMaterial.ObservacaoItemCritico,
                    Volume = item.Volume ?? string.Empty,
                    Pedido = item.Pedido ?? string.Empty,
                    Quantidade = item.Quantidade,
                    QtdConferida = item.QtdConferida,
                    QtdArmazenada = item.QtdArmazenada,
                    Diferenca = diferenca,
                    Conferido = item.Conferido,
                    Situacao = ObterSituacao(item.Conferido, item.QtdConferida, item.Quantidade),
                    UsuarioConferencia = item.UsuarioConferencia,
                    DtHrConferencia = item.DtHrConferencia,
                    UsuarioArmazenagem = item.UsuarioArmazenagem,
                    DtHrArmazenagem = item.DtHrArmazenagem,
                    ModificadoEm = item.ModificadoEm
                });
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

        private static string ObterSituacao(bool conferido, decimal? qtdConferida, decimal quantidade)
        {
            if (!conferido)
                return "Pendente";
            if (!qtdConferida.HasValue || qtdConferida.Value == quantidade)
                return "Conferido";
            return qtdConferida.Value < quantidade ? "Conferido a menor" : "Conferido a maior";
        }

    }
}
