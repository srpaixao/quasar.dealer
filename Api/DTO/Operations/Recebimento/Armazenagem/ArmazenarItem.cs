namespace QuasarApi.DTO.Operations.Recebimento.Armazenagem
{
    public class ArmazenarItem
    {
        public string ItemNr { get; set; } = string.Empty;
        public decimal Quantidade { get; set; }
        public string Usuario { get; set; } = string.Empty;
        public int? FilialId { get; set; }
    }
}
