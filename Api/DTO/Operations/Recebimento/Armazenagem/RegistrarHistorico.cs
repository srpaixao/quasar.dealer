namespace QuasarApi.DTO.Operations.Recebimento.Armazenagem
{
    public class RegistrarHistorico
    {
        public string ItemNr { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public string Locacao { get; set; } = string.Empty;
        public string LocacaoConfirmada { get; set; } = string.Empty;
        public decimal Quantidade { get; set; }
        public bool Erro { get; set; } = false;
        public string Mensagem { get; set; } = string.Empty;
        public string Usuario { get; set; } = string.Empty;
        public int FilialId { get; set; }
    }
}
