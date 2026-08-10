using System.Web.Mvc;

namespace Simplify.Quasar.Areas.SeparacaoApp
{
    public class SeparacaoAppAreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "SeparacaoApp";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            context.MapRoute(
                "SeparacaoApp_default",
                "SeparacaoApp/{controller}/{action}/{id}",
                new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );
        }

    }
}