using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Simplify.Quasar.Areas.RecebimentoApp.ViewModels
{
    public class EtiquetaMaterialViewModel
    {
        public string Material { get; set; }
        public string Descricao { get; set; }
        public string Locacao { get; set; }
        public int Quantidade { get; set; }
        public string Curva { get; set; }
        public string Data { get; set; }
        public string Hora { get; set; }
    }
}