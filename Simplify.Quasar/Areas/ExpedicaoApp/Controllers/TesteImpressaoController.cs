using Simplify.Quasar.Custom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Simplify.Quasar.Areas.ExpedicaoApp.Controllers
{
    [ValidateSession]
    public class TesteImpressaoController : Controller
    {
        // GET: ExpedicaoApp/TesteImpressao
        public ActionResult Index()
        {
            return View();
        }
    }
}