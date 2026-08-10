using System.Web.Mvc;

namespace Simplify.Quasar.Areas.ControleAcessoApp
{
    public class ControleAcessoAppAreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "ControleAcessoApp";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            context.MapRoute(
                "ControleAcessoApp_default",
                "ControleAcessoApp/{controller}/{action}/{id}",
                new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );
        }

    }
}