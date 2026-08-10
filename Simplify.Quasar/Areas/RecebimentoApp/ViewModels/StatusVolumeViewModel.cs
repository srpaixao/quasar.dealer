using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Simplify.Quasar.Areas.RecebimentoApp.ViewModels
{
    public class StatusVolumeViewModel
    {
        public string Volume { get; set; }
        public string Status { get; set; }
        public string Usuario { get; set; }
        public DateTime? CriadoEm { get; set; }
        public string NFiscal { get; set; }
    }
}