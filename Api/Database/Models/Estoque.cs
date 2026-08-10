namespace QuasarApi.Database.Models;

using System.ComponentModel.DataAnnotations;

public partial class Estoque
{
    [Key]
    public int Id { get; set; }
    public string? Locacao { get; set; }
    public string ItemNr { get; set; } = null!;
    public int? Saldo { get; set; }
    public int? Indisponivel { get; set; }
    public int? PedidoPendente { get; set; }
    public decimal? ValorEstoque { get; set; }
    public string? Range { get; set; }
    public string? CriadoPor { get; set; }
    public DateTime? CriadoEm { get; set; }
    public string? ModificadoPor { get; set; }
    public DateTime? ModificadoEm { get; set; }
    public int? FilialId { get; set; }
}
