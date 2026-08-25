using QuasarApi.Services.Interfaces;
using static QuasarApi.DTO.Management.AreaDTO;

namespace QuasarApi.Routes.Management
{
    public static class AreaRoutes
    {
        public static WebApplication MapAreaRoutes(this WebApplication app, WebApplicationBuilder builder)
        {
            const string groupPrefix = "/areas";
            var group = app.MapGroup(groupPrefix);

            group.MapGet("/", async (IAreaService service) =>
            {
                var areas = await service.ObterTodosAsync();
                return Results.Ok(areas);
            });

            group.MapGet("/{id:int}", async (int id, IAreaService service) =>
            {
                var usuario = await service.ObterPorIdAsync(id);
                return usuario is not null ? Results.Ok(usuario) : Results.NotFound();
            });

            group.MapPost("/", async (AreaCreateDto dto, IAreaService service) =>
            {
                var novo = await service.CriarAsync(dto);
                return Results.Created($"/areas/{novo.Id}", novo);
            });

            group.MapPut("/", async (AreaUpdateDto dto, IAreaService service) =>
            {
                await service.AtualizarAsync(dto);
                return Results.NoContent();
            });

            group.MapDelete("/{id:int}", async (int id, IAreaService service) =>
            {
                await service.ExcluirAsync(id);
                return Results.NoContent();
            });

            return app;
        }
    }
}
