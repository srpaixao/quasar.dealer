using System;
using System.Collections.Generic;
using System.Web.Mvc;

namespace Simplify.Quasar.Areas.DevolucaoApp.ViewModels
{
    public class DevolucaoOcorrenciaConsultaViewModel
    {
        public int Id { get; set; }
        public string ControleNr { get; set; }
        public string NotaFiscalNr { get; set; }
        public string Emissor { get; set; }
        public int QuantidadeLinhas { get; set; }
        public int QuantidadePecas { get; set; }
        public DateTime? UltimaAtualizacao { get; set; }
    }

    public class DevolucaoOcorrenciaViewModel
    {
        public int Id { get; set; }
        public int? NotaFiscalId { get; set; }
        public int StatusOcorrenciaId { get; set; }
        public int StatusCorrigidaId { get; set; }
        public int StatusFinalizadoId { get; set; }
        public string ControleNr { get; set; }
        public string NotaFiscalNr { get; set; }
        public string Emissor { get; set; }
        public string Cliente { get; set; }
        public string Vendedor { get; set; }
        public string Motivo { get; set; }
        public string NFDevolucao { get; set; }
        public string Sinistro { get; set; }
        public string PlacaVeiculo { get; set; }
        public string Observacao { get; set; }
        public string StatusNome { get; set; }
        public string FilialNome { get; set; }
        public DateTime? DataVenda { get; set; }
        public DateTime? DataCadastro { get; set; }
        public DateTime? UltimaAtualizacao { get; set; }
        public IEnumerable<SelectListItem> StatusTratamentoDDL { get; set; }
        public List<DevolucaoOcorrenciaItemViewModel> Itens { get; set; }
    }

    public class DevolucaoOcorrenciaItemViewModel
    {
        public int DevolucaoItemId { get; set; }
        public int? NotaFiscalItemId { get; set; }
        public string ItemNr { get; set; }
        public string Descricao { get; set; }
        public int QuantidadeOriginal { get; set; }
        public int QuantidadeOcorrencia { get; set; }
        public int? StatusId { get; set; }
        public string StatusNome { get; set; }
        public string Observacao { get; set; }
        public bool PermiteTratamento { get; set; }
        public int? NovoStatusId { get; set; }
        public int? QuantidadeTratada { get; set; }
        public string ObservacaoTratamento { get; set; }
    }
}
