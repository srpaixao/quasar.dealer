using System.Web.Mvc;

namespace Simplify.Quasar.Controllers
{
    public class ErrorController : Controller
    {
        public ActionResult GenericError()
        {
            return View();
        }

        public ActionResult InternalServerError()
        {
            return View();
        }

        public ActionResult PageNotFoundError()
        {
            return View();
        }

        public ActionResult UnauthorizedError()
        {
            return View();
        }

        public ActionResult AjaxError()
        {
            return View();
        }
    }
}
