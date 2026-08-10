using System.Web.Mvc;

namespace Simplify.Quasar.Areas.ConfiguracaoApp
{
    public class ConfiguracaoAppAreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "ConfiguracaoApp";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            context.MapRoute(
                "ConfiguracaoApp_default",
                "ConfiguracaoApp/{controller}/{action}/{id}",
                new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}