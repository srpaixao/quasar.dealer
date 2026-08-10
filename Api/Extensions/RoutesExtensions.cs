using QuasarApi.Routes.Management;
using QuasarApi.Routes.Operations;

namespace QuasarApi.Extensions
{
    public static class RouterExtensions
    {
        public static void MapRoutes(this WebApplication app, WebApplicationBuilder builder)
        {
            app.MapAuthRoutes(builder);
            app.MapUsuarioRoutes(builder);
            app.MapEmpresaRoutes(builder);
            app.MapMaterialRoutes(builder);
            app.MapNotaFiscalRoutes(builder);
            app.MapArmazenagemRoutes(builder);
            app.MapEstoqueRoutes(builder);
            app.MapRecebimentoRoutes(builder);
            app.MapAreaRoutes(builder);
            app.MapTransportadoraRoutes(builder);
            app.MapExpedicaoRoutes(builder);
            app.MapConfigRoutes(builder);
        }
    }
}

