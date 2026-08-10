using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Simplify.Quasar.Areas.ExpedicaoApp.ViewModels
{
    public class PrintViewModel
    {
        public string PrinterServerIP { get; set; }
        public string PrinterServerPort { get; set; }
        public string ZPL_Volume { get; set; }
        public string ZPL_Material { get; set; }
    }
}