using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Simplify.Quasar.Areas.EstoqueApp.ViewModels
{
    public class MaterialViewModel
    {
        public string Codigo { get; set; }
        public string Descricao { get; set; }
        public string UN { get; set; }
        public int? EmbalagemMin { get; set; }
        public decimal? MediaVendas { get; set; }
        public decimal? CustoUnitario { get; set; }
        public string Curva { get; set; }
        public DateTime? CriadoEm { get; set; }
        public string CriadoPor { get; set; }
        public DateTime? ModificadoEm { get; set; }
        public string ModificadoPor { get; set; }
    }
}