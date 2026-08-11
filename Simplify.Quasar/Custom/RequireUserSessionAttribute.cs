using System;
using System.Web;
using System.Web.Mvc;

namespace Simplify.Quasar.Custom
{
    /// <summary>
    /// Exige uma sessao autenticada sem depender de uma funcao cadastrada no
    /// controle de acesso. Usado por recursos globais disponiveis a todos os
    /// usuarios autenticados, como o manual operacional.
    /// </summary>
    public class RequireUserSessionAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            HttpSessionStateBase session = context.HttpContext.Session;
            if (session == null || session["useraccount"] == null)
            {
                context.Result = new RedirectResult("~/Account/Login");
                return;
            }

            int userId;
            int filialId;
            if (string.Equals(context.ActionDescriptor.ActionName, "Index", StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(Convert.ToString(session["userid"]), out userId) &&
                int.TryParse(Convert.ToString(session["filialid"]), out filialId))
            {
                OnlineUserTracker.Track(
                    session.SessionID,
                    userId,
                    filialId,
                    session.Timeout,
                    null,
                    "Manual",
                    context.ActionDescriptor.ActionName,
                    "Manual do Sistema");
            }

            base.OnActionExecuting(context);
        }
    }
}
