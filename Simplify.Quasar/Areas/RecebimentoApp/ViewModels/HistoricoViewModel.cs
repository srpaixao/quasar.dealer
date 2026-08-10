using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Simplify.Quasar.Areas.RecebimentoApp.ViewModels
{

    public class HistoricoViewModel
    {
        public long Id { get; set; }
        public string CodMaterial { get; set; }
        public string DescMaterial { get; set; }
        public string Curva { get; set; }
        public string CodLocacao { get; set; }
        public string NroVolume { get; set; }
        public Decimal? Quantidade { get; set; }
        public DateTime DataHora { get; set; }
        public string Usuario { get; set; }
        public string UsuarioNome { get; set; }
        public int? FilialId { get; set; }

    }


}