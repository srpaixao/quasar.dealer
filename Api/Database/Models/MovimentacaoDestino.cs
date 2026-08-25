namespace QuasarApi.Database.Models
{
    public class MovimentacaoDestino
    {
        public int Id { get; set; }
        public string ItemNr { get; set; } = string.Empty;
        public string? Locacao { get; set; }
        public int? FilialId { get; set; }
    }
}
