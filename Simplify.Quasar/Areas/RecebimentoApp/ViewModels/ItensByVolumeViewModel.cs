using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Simplify.Quasar.Areas.RecebimentoApp.ViewModels
{
    public class ItensByVolumeViewModel
    {
        public int NfItemId { get; set; }
        public string NumeroNF { get; set; }
        public string ItemNr { get; set; }
        public string ItemDescricao { get; set; }
        public int Quantidade { get; set; }
        public int StatusId { get; set; }
        public string StatusNome { get; set; }
        public bool HabilitarCheckbox { get; set; }
    }
}