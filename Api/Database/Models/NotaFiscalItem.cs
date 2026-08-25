using System.ComponentModel.DataAnnotations;

namespace QuasarApi.Database.Models;

public partial class NotaFiscalItem
{
    [Key]
    public int Id { get; set; }

    public int NotaFiscalId { get; set; }

    public string Item { get; set; } = null!;

    public decimal Quantidade { get; set; }

    public decimal? QtdConferida { get; set; }

    public decimal? QtdArmazenada { get; set; }

    public bool Conferido { get; set; }

    public string? UsuarioConferencia { get; set; }

    public DateTime? DtHrConferencia { get; set; }

    public string? UsuarioArmazenagem { get; set; }

    public DateTime? DtHrArmazenagem { get; set; }

    public string? Volume { get; set; }

    public string? Pedido { get; set; }

    public int? StatusId { get; set; }

    public string? Observacao { get; set; }

    public string? CriadoPor { get; set; }

    public DateTime? CriadoEm { get; set; }

    public string? ModificadoPor { get; set; }

    public DateTime? ModificadoEm { get; set; }

    public int? FilialId { get; set; }
}
