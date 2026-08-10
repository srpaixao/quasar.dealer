using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuasarApi.DataBase;

namespace QuasarApi.Routes.Management
{
    public static class MaterialRoutes
    {
        public static WebApplication MapMaterialRoutes(this WebApplication app, WebApplicationBuilder builder)
        {
            // Obter material
            app.MapGet("/materiais/{codigo}", async (string codigo, [FromQuery] int? filialId, AppDbContext db) =>
            {
                var material = await db.Material
                    .Where(u => u.Codigo == codigo)
                    //.Where(u => u.Codigo == codigo && u.FilialId == filialId)
                    .FirstOrDefaultAsync();

                if (material == null)
                {
                    return Results.NotFound(new { mensagem = "Item não cadastrado" });
                }

                return Results.Ok(material);
            }).RequireAuthorization();

            return app;
        }
    }
}
