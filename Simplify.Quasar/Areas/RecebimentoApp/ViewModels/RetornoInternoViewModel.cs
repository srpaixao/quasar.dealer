using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Simplify.Quasar.Areas.RecebimentoApp.ViewModels
{
    public class RetornoInternoViewModel
    {
        public int Id { get; set; }
        public string NrDocumento { get; set; }

        public int TipoDocumentoRetornoId { get; set; }
        public string TipoDocumentoRetornoNome { get; set; }
        public IEnumerable<SelectListItem> TipoDocumentoRetornoDDL { get; set; }

        public int? LocalOrigemId { get; set; }
        public string LocalOrigemNome { get; set; }
        public IEnumerable<SelectListItem> LocalOrigemDDL { get; set; }

        public int? LocalDestinoId { get; set; }
        public string LocalDestinoNome { get; set; }
        public IEnumerable<SelectListItem> LocalDestinoDDL { get; set; }

        public string Responsavel { get; set; }
        public string Observacoes { get; set; }

        public int StatusDocumentoRetornoId { get; set; }
        public string StatusDocumentoRetornoNome { get; set; }

        public string CriadoPor { get; set; }
        public string CriadoPorNome { get; set; }
        public DateTime? CriadoEm { get; set; }

        public string ModificadoPor { get; set; }
        public string ModificadoPorNome { get; set; }
        public DateTime? ModificadoEm { get; set; }

        public int? FilialId { get; set; }

        public int QtdItens { get; set; }
        public List<RetornoInternoItemViewModel> _itens { get; set; }

        public string JsonItens { get; set; }
        public DateTime? FinalizadoEm { get; set; }

        public bool AllowDelete { get; set; }
    }

    public class RetornoInternoItemViewModel
    {
        public int Id { get; set; }
        public int RetornoInternoId { get; set; }
        public string ItemNr { get; set; }
        public string ItemNrDescricao { get; set; }
        public decimal? Quantidade { get; set; }
        public int StatusRetornoId { get; set; }
        public string StatusRetornoNome { get; set; }
        public decimal? QtdArmazenada { get; set; }
        public string CriadoPor { get; set; }
        public string CriadoPorNome { get; set; }
        public DateTime? CriadoEm { get; set; }
        public string ModificadoPor { get; set; }
        public string ModificadoPorNome { get; set; }
        public DateTime? ModificadoEm { get; set; }
        public bool AllowEdit { get; set; }
    }
}