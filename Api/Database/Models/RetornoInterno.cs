using System.ComponentModel.DataAnnotations;

namespace QuasarApi.Database.Models;

public partial class RetornoInterno
{
    [Key]
    public int Id { get; set; }

    public string? NrDocumento { get; set; }

    public int? TipoDocumentoRetornoId { get; set; }

    public int? LocalOrigemId { get; set; }

    public int? LocalDestinoId { get; set; }

    public string? Responsavel { get; set; }

    public string? Observacoes { get; set; }

    public DateTime? FinalizadoEm { get; set; }

    public string? CriadoPor { get; set; }

    public DateTime? CriadoEm { get; set; }

    public string? ModificadoPor { get; set; }

    public DateTime? ModificadoEm { get; set; }

    public int? FilialId { get; set; }
}
