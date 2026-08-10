using System.Web.Mvc;

namespace Simplify.Quasar.Areas.ExpedicaoApp
{
    public class ExpedicaoAppAreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "ExpedicaoApp";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            context.MapRoute(
                "ExpedicaoApp_default",
                "ExpedicaoApp/{controller}/{action}/{id}",
                new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}