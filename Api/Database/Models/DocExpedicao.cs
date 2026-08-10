namespace QuasarApi.Database.Models
{
    public class DocExpedicao
    {
        public int Id { get; set; }
        public string Numero { get; set; } = string.Empty; // 9 dígitos NF
        public string Controle { get; set; } = string.Empty;
        public string NomeCliente { get; set; } = string.Empty;
        public string Cidade { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;  
        public int TransportadoraId { get; set; }
        public int QtdVolumes { get; set; }
        public int StatusId { get; set; }
        public int? QtdVolConf { get; set; } 

        // Campos adicionais opcionais
        public int? FilialId { get; set; }
        public string? CriadoPor { get; set; }
        public DateTime? CriadoEm { get; set; }
    }
}

