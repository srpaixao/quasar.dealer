using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuasarApi.DataBase;

namespace QuasarApi.Routes.Operations
{
    public static class TransportadoraRoutes
    {
        public static WebApplication MapTransportadoraRoutes(this WebApplication app, WebApplicationBuilder builder)
        {
            const string groupPrefix = "/transportadoras";
            var group = app.MapGroup(groupPrefix);

            // Lista básica de transportadoras para dropdown
            group.MapGet("/", async ([FromQuery] int? filialId, AppDbContext db) =>
            {
                try
                {
                    var query = db.Transportadora
                        .Where(t => t.FilialId == filialId);

                    var list = await query
                        .OrderBy(t => t.Nome)
                        .Select(t => new { id = t.Id, nome = t.Nome })
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

            return app;
        }
    }
}
