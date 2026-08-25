using System.ComponentModel.DataAnnotations;

namespace QuasarApi.Database.Models
{
    public class AppConfig
    {
        [Key]
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public string Valor { get; set; } = string.Empty;
        public string? CriadoPor { get; set; }
        public DateTime? CriadoEm { get; set; }
        public string? ModificadoPor { get; set; }
        public DateTime? ModificadoEm { get; set; }
        public int? FilialId { get; set; }
    }
}
