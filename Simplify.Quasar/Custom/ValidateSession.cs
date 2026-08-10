using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Simplify.Quasar.Custom
{
    public class ValidateSession : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (HttpContext.Current.Session["useraccount"] == null)
            {
                context.Result = new RedirectResult("~/Account/Login");
            }
            base.OnActionExecuting(context);
        }
    }
}