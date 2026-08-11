using System;
using System.Web;
using System.Web.Mvc;

namespace Simplify.Quasar.Custom
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class AuthorizeFunction : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var session = HttpContext.Current.Session;
            if (session == null || session["useraccount"] == null)
            {
                filterContext.Result = new RedirectResult("~/Account/Login");
                return;
            }

            string controller = filterContext.ActionDescriptor.ControllerDescriptor.ControllerName;
            string action = filterContext.ActionDescriptor.ActionName;
            string area = filterContext.RouteData.DataTokens["area"] as string;

            if (!Util.HasFunctionAccess(area, controller, action))
            {
                filterContext.Result = new HttpStatusCodeResult(403, "Acesso não autorizado");
            }
        }
    }
}
