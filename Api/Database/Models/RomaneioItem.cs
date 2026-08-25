using System.ComponentModel.DataAnnotations;

namespace QuasarApi.Database.Models;

public partial class RomaneioItem
{
    [Key]
    public int Id { get; set; }
    public int RomaneioId { get; set; }
    public string ItemNr { get; set; } = string.Empty;
    public int? Qtde { get; set; }
    public string? CriadoPor { get; set; }
    public DateTime? CriadoEm { get; set; }
    public int? FilialId { get; set; }
    public string? TarefaNr { get; set; }
    public string? Descricao { get; set; }
    public decimal? ValorUnitario { get; set; }
    public decimal? ValorTotal { get; set; }
    public int? StatusId { get; set; }
    public int? SeparadorId { get; set; }
    public DateTime? DataSeparador { get; set; }
    public int? QtdeSeparada { get; set; }
    public int? QtdeConferida { get; set; }
    public int? ConferenteId { get; set; }
    public DateTime? DataConferente { get; set; }
    public int? LocacaoId { get; set; }
    public int? ZonaId { get; set; }
}
