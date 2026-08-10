namespace QuasarApi.Database.Models
{
    public class HistoricoDespacho
    {
        public int Id { get; set; }
        public string NotaFiscalNr { get; set; } = string.Empty;
        public string VolumeNr { get; set; } = string.Empty;
        public int TransportadoraId { get; set; }
        public string? TransportadoraNome { get; set; }
        public string? Veiculo { get; set; }
        public string? Responsavel { get; set; }
        public string? NrMapa { get; set; }
        public string? CriadoPor { get; set; }
        public DateTime CriadoEm { get; set; }

    }
}

