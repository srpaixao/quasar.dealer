namespace QuasarApi.Database.Models;

using System.ComponentModel.DataAnnotations;

public partial class AppConfig
{
    [Key]
    public int Id { get; set; }
    public string Nome { get; set; } = null!;
    public string? Descricao { get; set; }
    public bool? Valor { get; set; }    
    public DateTime? CriadoEm { get; set; }
    public string? ModificadoPor { get; set; }
    public DateTime? ModificadoEm { get; set; }
    public int? FilialId { get; set; }
}
