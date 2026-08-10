using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Simplify.Quasar.Areas.RecebimentoApp.ViewModels
{
    public class TransitoGMViewModel
    {
        public List<TransitoViewModel> transito { get; set; }
    }

    public class TransitoViewModel
    {
        public int NotaFiscalId { get; set; }
        public string NotaFiscalNr { get; set; }
        public string ItemNr { get; set; }
        public string ItemDesc { get; set; }
        public string VolumeNr { get; set; }
        public string PedidoNr { get; set; }
        public int Quantidade { get; set; }

        public string Fornecedor { get; set; }
        public string Origem { get; set; }
        public string Status { get; set; }

        public string Locacao { get; set; }
        public string StatusItem { get; set; }

        public int QtdItensNF { get; set; }
        public int QtdVolumes { get; set; }
        public int QtdItens { get; set; }
        public decimal? QtdTotal { get; set; }

        public DateTime? CriadoEm { get; set; }
        public string CriadoPor { get; set; }
        public DateTime? ModificadoEm { get; set; }
        public string ModificadoPor { get; set; }
    }

    public class UploadArquivo
    {
        public bool ApagarHistorico { get; set; }
        public HttpPostedFileBase Arquivo { set; get; }
    }

}