using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Simplify.Quasar.Areas.RecebimentoApp.ViewModels
{
    public class PendenciasViewModel
    {
        public int NFId { get; set; }
        public string ItemNr { get; set; }
        public int ItemNrCount { get; set; }
        public string Descricao { get; set; }
        public string Locacao { get; set; }
        public decimal Quantidade { get; set; }
        public DateTime DtHr { get; set; }
        public string DtHrTexto { get; set; }
        public string VolumeNr { get; set; }
        public int StatusId { get; set; }
        public string Origem { get; set; }
        public string Status { get; set; }
        public string Usuario { get; set; }
        public DateTime? CriadoEm { get; set; }
        public string CriadoEmTexto { get; set; }

        public DateTime? ModificadoEm { get; set; }
        
        public string NFiscal { get; set; }
        public int NFiscalCount { get; set; }
        public string Volume { get; set; }

        public int periodo { get; set; }

    }

}
