using System;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Simplify.Quasar.App_Start;
using Simplify.Quasar.Controllers;
using NLog;

namespace Simplify.Quasar
{
    public class MvcApplication : HttpApplication
    {
        //private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);

            // Ajustes de estrutura e dados devem ser aplicados no processo de
            // atualização, não durante a primeira requisição do usuário. Executar
            // consultas e gravações aqui fazia a tela de login aguardar o SQL Server
            // sempre que o IIS/Visual Studio reiniciava a aplicação.
        }

        protected void Session_Start(object sender, EventArgs e)
        {
            /* Sets the session duration to 120 minutes. */
            Session.Timeout = 120;
        }

        protected void Session_End(object sender, EventArgs e)
        {
            if (Session != null)
            {
                Simplify.Quasar.Custom.OnlineUserTracker.Unregister(Session.SessionID);
            }
        }

        public override string GetVaryByCustomString(HttpContext context, string custom)
        {
            if (custom == "MenuProfile")
            {
                var session = context != null ? context.Session : null;
                string perfil = session != null && session["perfilid"] != null ? session["perfilid"].ToString() : "0";
                string filial = session != null && session["filialid"] != null ? session["filialid"].ToString() : "0";

                int perfilId;
                int.TryParse(perfil, out perfilId);

                int version = Simplify.Quasar.Custom.Util.GetMenuCacheVersion(perfilId);
                return perfil + ":" + filial + ":" + version;
            }

            return base.GetVaryByCustomString(context, custom);
        }

        //protected void Application_Error(object sender, EventArgs e)
        //{
        //Exception exception = Server.GetLastError();
        //// Log the exception.

        ////ILogger logger = Container.Resolve<ILogger>();
        ////logger.Error(exception);

        //Response.Clear();

        //HttpException httpException = exception as HttpException;

        //RouteData routeData = new RouteData();
        //routeData.Values.Add("controller", "Error");

        //if (httpException == null)
        //{
        //    routeData.Values.Add("action", "Exception");
        //}
        //else // Http Exception
        //{
        //    switch (httpException.GetHttpCode())
        //    {
        //        case 404:
        //            routeData.Values.Add("action", "NotFound");
        //            break;
        //        case 500:
        //            routeData.Values.Add("action", "InternalServerError");
        //            break;
        //        default:
        //            routeData.Values.Add("action", "HttpError");
        //            break;
        //    }
        //}

        //// Pass exception details to the target error View.
        //routeData.Values.Add("exception", exception);
        //routeData.Values["area"] = string.Empty;

        //// Clear the error on server.
        //Server.ClearError();

        //// Avoid IIS7 getting in the middle
        //Response.TrySkipIisCustomErrors = true;

        //// Call target Controller and pass the routeData.
        //IController errorController = new ErrorController();
        //errorController.Execute(new RequestContext(new HttpContextWrapper(Context), routeData));
        //}

        //protected void Application_Error(object sender, EventArgs e)
        //{
        //    var exception = Server.GetLastError();
        //    //Log.Error(exception, "Unhandled application exception");

        //    var httpContext = ((HttpApplication)sender).Context;
        //    httpContext.Response.Clear();
        //    httpContext.ClearError();

        //    if (new HttpRequestWrapper(httpContext.Request).IsAjaxRequest())
        //    {
        //        return;
        //    }

        //    ExecuteErrorController(httpContext, exception as HttpException);
        //}

        //private void ExecuteErrorController(HttpContext httpContext, HttpException exception)
        //{
        //    var routeData = new RouteData();
        //    routeData.Values["controller"] = "Error";

        //    if (exception != null && exception.GetHttpCode() == (int)HttpStatusCode.NotFound)
        //    {
        //        routeData.Values["action"] = "NotFound";
        //    }
        //    else
        //    {
        //        routeData.Values["action"] = "InternalServerError";
        //    }

        //    routeData.Values["area"] = string.Empty;
        //    routeData.Values["exception"] = exception;

        //    using (Controller controller = new ErrorController())
        //    {
        //        ((IController)controller).Execute(new RequestContext(new HttpContextWrapper(httpContext), routeData));
        //    }
        //}
    }
}
