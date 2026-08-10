using System.Web.Mvc;

namespace Simplify.Quasar.Areas.RecebimentoApp
{
    public class RecebimentoAppAreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "RecebimentoApp";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            context.MapRoute(
             "RecebimentoApp_default",
             "RecebimentoApp/{controller}/{action}/{id}",
             new { controller = "Home", action = "Index", id = UrlParameter.Optional }
         );
        }
    }
}