using System.Web.Mvc;
using System.Web.Routing;

namespace Simplify.Quasar
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                name: "ManualAsset",
                url: "Manual/Asset",
                defaults: new { controller = "Manual", action = "Asset" },
                namespaces: new[] { "Simplify.Quasar.Controllers" }
            );

            routes.MapRoute(
                name: "Manual",
                url: "Manual/{pagina}",
                defaults: new { controller = "Manual", action = "Index", pagina = UrlParameter.Optional },
                namespaces: new[] { "Simplify.Quasar.Controllers" }
            );

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Account", action = "Login", id = UrlParameter.Optional }
                , namespaces: new[] { "Simplify.Quasar.Controllers" }
            );
        }
    }
}
