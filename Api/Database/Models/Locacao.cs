namespace QuasarApi.Database.Models;

using System.ComponentModel.DataAnnotations;

public partial class Locacao
{
    [Key]
    public string Codigo { get; set; } = null!;
    public int? FilialId { get; set; }
    public string? Tipo { get; set; } = null!;
    public string? Descricao { get; set; } = null!;
    public bool Bloqueado { get; set; }
    public int? AreaId { get; set; } = null!;
    public int? EquipamentoId { get; set; } = null!;
    public string? Curva { get; set; } = null!;
    public string? Estrategia { get; set; } = null!;
    public string? Observacoes { get; set; } = null!;
    public string? CriadoPor { get; set; } = null!;
    public DateTime? CriadoEm { get; set; }
    public string? ModificadoPor { get; set; }
    public DateTime? ModificadoEm { get; set; }
}
