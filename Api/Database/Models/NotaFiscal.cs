using System.ComponentModel.DataAnnotations;

namespace QuasarApi.Database.Models;

public partial class NotaFiscal
{
    [Key]
    public int Id { get; set; }

    public string Movimento { get; set; } = null!;

    public int TipoId { get; set; }

    public int StatusId { get; set; }

    public string Numero { get; set; } = null!;

    public string? Serie { get; set; }

    public string? Emissor { get; set; }

    public DateOnly? DataEmissao { get; set; }

    public decimal? Valor { get; set; }

    public string? Descricao { get; set; }

    public string? Observacoes { get; set; }

    public string? Danfe { get; set; }

    public DateTime? RecebidoAdmEm { get; set; }

    public string? RecebidoAdmPor { get; set; }

    public string? CriadoPor { get; set; }

    public DateTime? CriadoEm { get; set; }

    public string? ModificadoPor { get; set; }

    public DateTime? ModificadoEm { get; set; }

    public int? FilialId { get; set; }
}
