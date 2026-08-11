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
            var session = HttpContext.Current != null ? HttpContext.Current.Session : null;
            if (session == null || session["useraccount"] == null)
            {
                context.Result = new RedirectResult("~/Account/Login");
                return;
            }

            string controller = context.ActionDescriptor.ControllerDescriptor.ControllerName;
            string action = context.ActionDescriptor.ActionName;
            string area = context.RouteData.DataTokens["area"] as string;

            if (!Util.HasFunctionAccess(area, controller, action))
            {
                context.Result = new HttpStatusCodeResult(403, "Acesso não autorizado");
                return;
            }

            int userId;
            int filialId;
            if (int.TryParse(Convert.ToString(session["userid"]), out userId)
                && int.TryParse(Convert.ToString(session["filialid"]), out filialId))
            {
                bool isAjaxRequest = context.HttpContext.Request.IsAjaxRequest();

                OnlineUserTracker.Track(
                    session.SessionID,
                    userId,
                    filialId,
                    session.Timeout,
                    isAjaxRequest ? null : area,
                    isAjaxRequest ? null : controller,
                    isAjaxRequest ? null : action);
            }

            var mvcController = context.Controller as Controller;
            if (mvcController != null)
            {
                mvcController.ViewBag.Permissoes = Util.GetPermissoes(controller, area);
            }

            base.OnActionExecuting(context);
        }
    }
}
