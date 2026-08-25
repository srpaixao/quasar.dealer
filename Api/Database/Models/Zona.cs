using System.ComponentModel.DataAnnotations;

namespace QuasarApi.Database.Models;

public partial class Zona
{
    [Key]
    public int Id { get; set; }
    public int? AreaId { get; set; }
    public string? Nome { get; set; }
    public string? Descricao { get; set; }
    public int? QtdeLinha { get; set; }
    public bool? ProntoDespacho { get; set; }
    public decimal? ValorPedido { get; set; }
    public int? QtdeCliente { get; set; }
    public bool Ativo { get; set; }
    public string? CriadoPor { get; set; }
    public DateTime? CriadoEm { get; set; }
    public string? ModificadoPor { get; set; }
    public DateTime? ModificadoEm { get; set; }
    public int? FilialId { get; set; }
}
