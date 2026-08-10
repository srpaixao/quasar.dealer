using System.Web.Mvc;

namespace Simplify.Quasar.Areas.GarantiaApp
{
    public class GarantiaAppAreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "GarantiaApp";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            context.MapRoute(
                "GarantiaApp_default",
                "GarantiaApp/{controller}/{action}/{id}",
                new { controller="Home", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}