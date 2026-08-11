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

        public ActionResult UnauthorizedError(string message)
        {
            ViewBag.Message = string.IsNullOrWhiteSpace(message)
                ? "Acesso não autorizado. Seu perfil não possui permissão para acessar esta funcionalidade."
                : message;
            return View();
        }

        public ActionResult AjaxError()
        {
            return View();
        }
    }
}
