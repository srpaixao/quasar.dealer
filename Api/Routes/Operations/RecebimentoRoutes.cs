using Microsoft.AspNetCore.Mvc;
using QuasarApi.DTO.Operations.Recebimento.Conferencia;
using QuasarApi.Services.Interfaces;

namespace QuasarApi.Routes.Operations
{
    public static class RecebimentoRoutes
    {
        public static WebApplication MapRecebimentoRoutes(this WebApplication app, WebApplicationBuilder builder)
        {
            const string groupPrefix = "/recebimento";
            var group = app.MapGroup(groupPrefix);

            group.MapGet("/volumeresumo/{statusId}/{areaId}", async (int statusId, int areaId, [FromQuery] int? filialId, [FromServices] IVolumeService service) =>
            {
                var volumes = await service.ResumoVolumesAsync(statusId, areaId, filialId);
                return Results.Ok(volumes);
            });

            group.MapPost("/volumeupdate", async (UpdateVolumeRequestDto request, [FromServices] IConferenciaVolumeService service) =>
            {
                var result = await service.UpdateVolumeAsync(request);
                return Results.Json(result);
            });

            return app;
        }

    }
}
