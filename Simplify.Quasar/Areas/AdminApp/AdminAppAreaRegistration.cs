using System.Web.Mvc;

namespace Simplify.Quasar.Areas.AdminApp
{
    public class AdminAppAreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "AdminApp";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            context.MapRoute(
                "AdminApp_default",
                "AdminApp/{controller}/{action}/{id}",
                 new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}