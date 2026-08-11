using System.Collections.Generic;
using System.Web.Mvc;

namespace Simplify.Quasar.Areas.DevolucaoApp.ViewModels
{
    public class DevolucaoCadastroViewModel
    {
        public string Movimento { get; set; }
        public string Retirar { get; set; }
        public int? StatusId { get; set; }
        public int? MotivoId { get; set; }
        public string NFVenda { get; set; }
        public string Cliente { get; set; }
        public string DataVenda { get; set; }
        public string Vendedor { get; set; }
        public bool VendedorBloqueado { get; set; }
        public int? TransportadoraId { get; set; }
        public string NFDevolucao { get; set; }
        public string Sinistro { get; set; }
        public string PlacaVeiculo { get; set; }
        public string Observacao { get; set; }
        public string ItensJson { get; set; }
        public int? UltimaDevolucaoId { get; set; }
        public string UltimoControleNr { get; set; }
        public List<DevolucaoCadastroItemViewModel> Itens { get; set; }
        public IEnumerable<SelectListItem> MovimentoDDL { get; set; }
        public IEnumerable<SelectListItem> RetirarDDL { get; set; }
        public IEnumerable<SelectListItem> StatusDDL { get; set; }
        public IEnumerable<SelectListItem> MotivoDDL { get; set; }
        public IEnumerable<SelectListItem> TransportadoraDDL { get; set; }
    }

    public class DevolucaoCadastroItemViewModel
    {
        public string ItemNr { get; set; }
        public string ItemDescricao { get; set; }
        public int Quantidade { get; set; }
        public decimal ValorUnitario { get; set; }
        public string Observacao { get; set; }
    }
}
