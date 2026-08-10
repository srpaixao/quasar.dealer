using System.ComponentModel.DataAnnotations;

namespace QuasarApi.Database.Models;

public partial class Usuario
{
    [Key]
    public int Id { get; set; }
    public string Login { get; set; } = null!;
    public string Senha { get; set; } = null!;
    public int PerfilId { get; set; }
    public string? Nome { get; set; }
    public string? Email { get; set; }
    public string? Telefone { get; set; }
    public int? EmpresaId { get; set; }
    public int? AreaId { get; set; }
    public bool SenhaExpirada { get; set; }
    public bool AcessoBloqueado { get; set; }
    public DateTime? UltimoAcesso { get; set; }
    public string? CriadoPor { get; set; }
    public DateTime? CriadoEm { get; set; }
    public string? ModificadoPor { get; set; }
    public DateTime? ModificadoEm { get; set; }
    public int? FilialId { get; set; }
}
