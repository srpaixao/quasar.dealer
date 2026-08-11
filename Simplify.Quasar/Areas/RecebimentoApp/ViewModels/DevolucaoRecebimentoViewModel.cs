using System;
using System.Collections.Generic;
using System.Web.Mvc;

namespace Simplify.Quasar.Areas.RecebimentoApp.ViewModels
{
    public class DevolucaoRecebimentoViewModel
    {
        public int Id { get; set; }
        public int? NotaFiscalId { get; set; }
        public int StatusOcorrenciaId { get; set; }
        public int StatusFinalizadoId { get; set; }
        public string ControleNr { get; set; }
        public string NotaFiscalNr { get; set; }
        public string Emissor { get; set; }
        public int? StatusId { get; set; }
        public string StatusNome { get; set; }
        public DateTime? UltimaAtualizacao { get; set; }
        public IEnumerable<SelectListItem> StatusDDL { get; set; }
        public IEnumerable<SelectListItem> OcorrenciaDDL { get; set; }
        public List<DevolucaoRecebimentoItemViewModel> Itens { get; set; }
    }

    public class DevolucaoRecebimentoItemViewModel
    {
        public int DevolucaoItemId { get; set; }
        public string ItemNr { get; set; }
        public string Descricao { get; set; }
        public int Quantidade { get; set; }
        public int? StatusId { get; set; }
        public string StatusNome { get; set; }
        public int? Ocorrencia { get; set; }
        public int? OcorrenciaId { get; set; }
        public string Observacao { get; set; }
        public bool Selecionado { get; set; }
        public int? OcorrenciaInformada { get; set; }
        public string ObservacaoOcorrencia { get; set; }
    }
}
