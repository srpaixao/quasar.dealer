namespace QuasarApi.DTO.Operations.Estoque
{
    public class ConsultarItem
    {
        public string ItemNr { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public string UN { get; set; } = string.Empty;
        public string Locacao { get; set; } = string.Empty;
        public int? Saldo { get; set; } = 0;
        public int? Indisponivel { get; set; } = 0;
        public int? PedidoPendente { get; set; } = 0;
        public string Curva { get; set; } = string.Empty;
        public bool ItemCritico { get; set; } = false;
        public bool EstoqueCadastrado { get; set; } = false;
        public bool MovimentacaoCorreta { get; set; } = false;
        public int? FilialId { get; set; } = 0;
    }
}
