using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Simplify.Quasar.Areas.ExpedicaoApp.ViewModels
{
    public class NotaFiscalViewModel
    {
        public int Id { get; set; }

        public string Numero { get; set; }
        public DateTime? DataEmissao { get; set; }
        public string Classificacao { get; set; }
        public string Controle { get; set; }
        public string Vendedor { get; set; }

        public int ClienteId { get; set; }
        public string CodigoCliente { get; set; }
        public string NomeCliente { get; set; }

        public string CNPJ { get; set; }
        public string Cidade { get; set; }
        public string Estado { get; set; }

        public int StatusId { get; set; }
        public string StatusNF { get; set; }

        public int? EmpresaId { get; set; }
        public string NomeEmpresa { get; set; }

        public bool? RoteiroImpresso { get; set; }
        public int? RoteiroId { get; set; }
        public string NumeroRoteiro { get; set; }

        public int? TransportadoraId { get; set; }
        public string NomeTransportadora { get; set; }
        public IEnumerable<SelectListItem> TransportadoraDDL { get; set; }
        public bool ImprimirRoteiro { get; set; }

        public int? QtdVolumes { get; set; }

        public int? RotaId { get; set; }
        public string NomeRota { get; set; }
        public IEnumerable<SelectListItem> RotaDDL { get; set; }

        public int? ParadaId { get; set; }
        public string NomeParada { get; set; }
        public IEnumerable<SelectListItem> ParadaDDL { get; set; }

        public string Movimento { get; set; }

        public int? TipoMovimentoId { get; set; }
        public string NomeTipoMovimento { get; set; }
        public IEnumerable<SelectListItem> TipoMovimentoDDL { get; set; }

        public string Danfe { get; set; }
        public decimal? Valor { get; set; }
        public string Observacoes { get; set; }

        public string LocalEntrega { get; set; }

        public DateTime? CriadoEm { get; set; }
        public string CriadoPor { get; set; }
        public string CriadoPorNome { get; set; }

        public DateTime? ModificadoEm { get; set; }
        public string ModificadoPor { get; set; }
        public string ModificadoPorNome { get; set; }

        public int TotalLancamento { get; set; }
        public int TotalAguardandoLancamento { get; set; }
        public int TotalEntrega { get; set; }
        public int TotalRetirada { get; set; }
        public int TotalGarantia { get; set; }
        public int TotalTroca { get; set; }
        public int TotalRoteiro { get; set; }
        public int TotalFinalizado { get; set; }

        public int TotalEmTransito { get; set; }

        public int TotalEmEspera { get; set; }

        public string ZPL_Etiqueta { get; set; }
        public string PrinterServerIP { get; set; }
        public string PrinterServerPort { get; set; }
        public bool Finalizar { get; set; }

    }

    public class UploadArquivo
    {
        public bool ApagarHistorico { get; set; }
        public HttpPostedFileBase Arquivo { set; get; }
    }

}