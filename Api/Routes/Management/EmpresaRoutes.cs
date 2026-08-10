using Microsoft.EntityFrameworkCore;
using QuasarApi.Database.Models;
using QuasarApi.DataBase;

namespace QuasarApi.Routes.Management
{
    public static class EmpresaRoutes
    {
        public static WebApplication MapEmpresaRoutes(this WebApplication app, WebApplicationBuilder builder)
        {
            // Obter lista de empresas (filiais ativas)
            app.MapGet("/empresas", async (AppDbContext db) =>
            {
                return await db.Empresa
                    .Where(e => e.StatusId == 1) // Assumindo StatusId 1 = Ativo
                    .Select(e => new { e.Id, e.Nome })
                    .ToListAsync();
            });

            return app;
        }
    }
}