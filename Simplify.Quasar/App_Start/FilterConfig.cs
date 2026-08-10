using System.Web.Mvc;
using Simplify.Quasar.Custom;

namespace Simplify.Quasar.App_Start
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }
    }
}

