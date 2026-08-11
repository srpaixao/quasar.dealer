using System.Web.Optimization;
using WebHelpers.Mvc5;

namespace Simplify.Quasar.App_Start
{
    public class BundleConfig
    {
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(new StyleBundle("~/Bundles/css")
                .Include("~/Content/bootstrap/bootstrap.min.css", new CssRewriteUrlTransformAbsolute())
                .Include("~/Content/bootstrap/select/bootstrap-select.css")
                .Include("~/Content/select2/select2.min.css")
                .Include("~/Content/bootstrap/datepicker3/bootstrap-datepicker3.min.css")
                .Include("~/Content/sweetalert.js.org/custom.css")
                .Include("~/Content/icheck/flat/blue.css", new CssRewriteUrlTransformAbsolute())
                .Include("~/Content/icheck/flat/red.css", new CssRewriteUrlTransformAbsolute())
                .Include("~/Content/icheck/flat/green.css", new CssRewriteUrlTransformAbsolute())
                .Include("~/Content/AdminLTE/AdminLTE.css", new CssRewriteUrlTransformAbsolute())
                .Include("~/Content/skins/_all-skins.min.css")
                .Include("~/Content/Site.css")
                .Include("~/Content/Quasar.Modern.css"));

            bundles.Add(new ScriptBundle("~/Bundles/jquery")
                .Include("~/Scripts/jquery/jquery-3.6.0.js"));

            bundles.Add(new ScriptBundle("~/Bundles/bootstrap")
                .Include("~/Scripts/bootstrap/bootstrap.js")
                .Include("~/Scripts/bootstrap-select/bootstrap-select.js")
                .Include("~/Scripts/select2/select2.min.js")
                .Include("~/Scripts/datepicker/bootstrap-datepicker.js"));

            bundles.Add(new ScriptBundle("~/Bundles/js")
                .Include("~/Scripts/fastclick/fastclick.js")
                .Include("~/Scripts/slimscroll/jquery.slimscroll.js")
                .Include("~/Scripts/modernizr/modernizr-2.6.2.js")
                .Include("~/Scripts/moment/moment.js")
                .Include("~/Scripts/icheck/icheck.min.js")
                .Include("~/Scripts/validator/validator.js")
                .Include("~/Scripts/inputmask/jquery.inputmask.bundle.js")
                .Include("~/Scripts/sweetalert.js.org/sweetalert.min.js"));

            bundles.Add(new ScriptBundle("~/Bundles/adminlte")
                .Include("~/Scripts/AdminLTE/adminlte.js")
                .Include("~/Scripts/AdminLTE/init.js"));

#if DEBUG
            BundleTable.EnableOptimizations = true;
#else
            BundleTable.EnableOptimizations = true;
#endif
        }
    }
}
