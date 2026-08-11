using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Simplify.Quasar.Areas.RecebimentoApp.ViewModels
{
    public class VolumeViewModel
    {
        public int AreaId { get; set; }
        public string Area { get; set; }
        public string NotaFiscalNr { get; set; }
        public string VolumeNr { get; set; }
        public int StatusId { get; set; }
        public string StatusNome { get; set; }
        public int QtdeItens { get; set; }
        public bool Imprimir { get; set; }
        public string Danfe { get; set; }
        public int? FilialId { get; set; }
        public DateTime? CriadoEm { get; set; }
        public string CriadoEmTexto { get; set; }

            public string ItemNr { get; set; }
            public string Descricao { get; set; }
            public string Locacao { get; set; }
            public string LocInformada { get; set; }
            public decimal Quantidade { get; set; }
            public decimal QtdeInformada { get; set; }
            public int NFId { get; set; }

    }
}
