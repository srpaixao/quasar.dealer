using System.Web.Mvc;

namespace Simplify.Quasar.Areas.DevolucaoApp
{
    public class DevolucaoAppAreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "DevolucaoApp";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            context.MapRoute(
                "DevolucaoApp_default",
                "DevolucaoApp/{controller}/{action}/{id}",
                new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}