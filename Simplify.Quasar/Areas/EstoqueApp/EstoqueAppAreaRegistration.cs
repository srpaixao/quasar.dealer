using System.Web.Mvc;

namespace Simplify.Quasar.Areas.EstoqueApp
{
    public class SeparacaoAppAreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "EstoqueApp";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            context.MapRoute(
                "EstoqueApp_default",
                "EstoqueApp/{controller}/{action}/{id}",
                new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}