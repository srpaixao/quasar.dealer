using System.ComponentModel.DataAnnotations;

namespace QuasarApi.Database.Models;

public partial class StatusVolume
{
    [Key]
    public int Id { get; set; }
    public string Nome { get; set; } = null!;
    public string Descricao { get; set; } = null!;
    public int? FilialId { get; set; }
}
