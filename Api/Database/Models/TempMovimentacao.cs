using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuasarApi.Database.Models
{
    [Table("Movimentacao")]
    public class Movimentacao
    {
        [Key]
        public int Id { get; set; } // Chave primária e identidade

        [Required]
        [MaxLength(100)]
        public string ItemNr { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? LocacaoOrigem { get; set; }

        public int? QtdOrigem { get; set; }

        [MaxLength(100)]
        public string? LocacaoDestino { get; set; }

        public int? QtdDestino { get; set; }

        [MaxLength(100)]
        public string? CriadoPor { get; set; }

        public DateTime? CriadoEm { get; set; }

        [MaxLength(100)]
        public string? FinalizadoPor { get; set; }

        public DateTime? FinalizadoEm { get; set; }

        public string? UrlDMS { get; set; }
        public string? Payload { get; set; }
        public string? Response { get; set; }
        public int? FilialId { get; set; }
    }
}

