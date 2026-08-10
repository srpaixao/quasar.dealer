using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Simplify.Quasar.Areas.ExpedicaoApp.ViewModels
{
    public class LogImpressaoExpedicaoViewlModel
    {
        public int Id { get; set; }
        public string Zpl { get; set; }
        public DateTime ImpressoEm { get; set; }
        public string Usuario { get; set; }
    }
}
