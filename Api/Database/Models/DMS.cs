namespace QuasarApi.Database.Models;

using System.ComponentModel.DataAnnotations;

public partial class DMS
{
    [Key]
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? BaseApi { get; set; }
    public string? UserApi { get; set; }
    public string CriadoPor { get; set; } = string.Empty;
    public DateTime? CriadoEm { get; set; }
    public string? ModificadoPor { get; set; }
    public DateTime? ModificadoEm { get; set; }
}
