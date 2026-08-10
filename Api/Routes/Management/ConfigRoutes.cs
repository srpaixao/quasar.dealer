using System.Dynamic;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using QuasarApi.Database.Models;
using QuasarApi.DataBase;

namespace QuasarApi.Routes.Management
{
    public static class ConfigRoutes
    {
        public static WebApplication MapConfigRoutes(this WebApplication app, WebApplicationBuilder builder)
        {
            const string groupPrefix = "/config";
            var group = app.MapGroup(groupPrefix);

            // Obter API Cliente
            group.MapGet("/cliente-api", async (AppDbContext db) =>
            {
                var api = await db.DMS
                                   .Select(x => new { x.Id, x.Nome, x.BaseApi, x.UserApi })
                                   .FirstOrDefaultAsync();

                if (api == null)
                {
                    return Results.Ok(new { Id = 0, Nome = string.Empty, BaseApi = string.Empty, UserApi = string.Empty });
                }

                return Results.Ok(api);
            }).RequireAuthorization();

            return app;
        }
    }
}
