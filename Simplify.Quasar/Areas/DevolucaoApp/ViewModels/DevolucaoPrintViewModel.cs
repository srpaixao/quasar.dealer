using System;
using System.Collections.Generic;
using System.Web.Mvc;

namespace Simplify.Quasar.Areas.DevolucaoApp.ViewModels
{
    public class DevolucaoPrintViewModel
    {
        public int Id { get; set; }
        public string ControleNr { get; set; }
        public int? StatusId { get; set; }
        public int? OriginalStatusId { get; set; }
        public string Movimento { get; set; }
        public string Retirar { get; set; }
        public int? MotivoId { get; set; }
        public string Motivo { get; set; }
        public string NFVenda { get; set; }
        public string Cliente { get; set; }
        public DateTime? DataVenda { get; set; }
        public string Vendedor { get; set; }
        public int? TransportadoraId { get; set; }
        public string Transportadora { get; set; }
        public string NFDevolucao { get; set; }
        public string Sinistro { get; set; }
        public string PlacaVeiculo { get; set; }
        public string Observacao { get; set; }
        public string StatusNome { get; set; }
        public string FilialNome { get; set; }
        public string UsuarioCadastro { get; set; }
        public DateTime? DataCadastro { get; set; }
        public IEnumerable<SelectListItem> MovimentoDDL { get; set; }
        public IEnumerable<SelectListItem> RetirarDDL { get; set; }
        public IEnumerable<SelectListItem> MotivoDDL { get; set; }
        public IEnumerable<SelectListItem> StatusDDL { get; set; }
        public IEnumerable<SelectListItem> TransportadoraDDL { get; set; }
        public List<DevolucaoPrintItemViewModel> Itens { get; set; }
    }

    public class DevolucaoPrintItemViewModel
    {
        public string ItemNr { get; set; }
        public string Descricao { get; set; }
        public int Quantidade { get; set; }
        public string StatusNome { get; set; }
        public int QtdeOcorrencia { get; set; }
        public string OcorrenciaNome { get; set; }
        public decimal ValorUnitario { get; set; }
        public string Observacao { get; set; }
    }
}
