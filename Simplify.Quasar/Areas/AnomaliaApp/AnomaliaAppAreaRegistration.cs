using System.Web.Mvc;

namespace Simplify.Quasar.Areas.AnomaliaApp
{
    public class AnomaliaAppAreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "AnomaliaApp";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            context.MapRoute(
                "AnomaliaApp_default",
                "AnomaliaApp/{controller}/{action}/{id}",
                new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}