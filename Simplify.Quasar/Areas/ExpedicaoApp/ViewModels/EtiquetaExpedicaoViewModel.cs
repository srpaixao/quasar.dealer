using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Simplify.Quasar.Areas.ExpedicaoApp.ViewModels
{
    public class EtiquetaExpedicaoViewModel
    {
        public string NotaFiscal { get; set; }
        public int QtdVolumes { get; set; }
        public string Contato { get; set; }
        public string Cliente { get; set; }
        public string Endereco { get; set; }
        public string Numero { get; set; }
        public string Complemento { get; set; }
        public string Bairro { get; set; }
        public string Cidade { get; set; }
        public string Estado { get; set; }
        public string Origem { get; set; }
        public string Parada { get; set; }
        public string Rota { get; set; }
        public string Transportadora { get; set; }
        public string Data { get; set; }
        public string Hora { get; set; }
        public string VolumeNr { get; set; }
        public string Sequencia { get; set; }
        public string CodigoBarrasVolumeSvg { get; set; }
        public string CodigoBarrasContatoSvg { get; set; }
        public int TranportadoraNotaFiscalStatusId { get; set; }
    }
}
