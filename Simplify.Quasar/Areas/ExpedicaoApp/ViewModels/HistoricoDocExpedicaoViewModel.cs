using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Simplify.Quasar.Areas.ExpedicaoApp.ViewModels
{
    public class HistoricoDocExpedicaoViewModel
    {
        public long Id { get; set; }
        public int DocExpedicaoId { get; set; }
        public int HistoricoId { get; set; }
        public string DescricaoHistorico { get; set; }
        public string Observacoes { get; set; }
        public DateTime DataHora { get; set; }
        public string Usuario { get; set; }
    }
}