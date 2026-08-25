using Microsoft.EntityFrameworkCore;
using QuasarApi.DataBase;

namespace QuasarApi.Routes.Management
{
    public static class MaterialRoutes
    {
        public static WebApplication MapMaterialRoutes(this WebApplication app, WebApplicationBuilder builder)
        {
            // Obter material
            app.MapGet("/materiais/{codigo}", async (string codigo, AppDbContext db) =>
            {
                var material = await db.Material
                                        .Where(u => u.Codigo == codigo)
                                        //.Select(u => new { u.Codigo, u.Descricao, u.FilialId })
                                        .FirstOrDefaultAsync();

                if (material == null)
                {
                    return Results.NotFound(new { mensagem = "Item não cadastrado" });
                }
                // Retorna o material encontrado
                return Results.Ok(material);

            }).RequireAuthorization();

            return app;
        }
    }
}
