namespace Simplify.Quasar.Areas.EstoqueApp.ViewModels
{
    public class AssociacaoLocacaoViewModel
    {
        public string Locacao { get; set; }
        public string Descricao { get; set; }
        public string Filial { get; set; }
        public string Situacao { get; set; }
        public int QuantidadeItens { get; set; }
    }

    public class AssociacaoLocacaoItemViewModel
    {
        public int EstoqueId { get; set; }
        public string ItemNr { get; set; }
        public string Descricao { get; set; }
        public string Texto { get; set; }
    }
}
