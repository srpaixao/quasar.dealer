using System.ComponentModel.DataAnnotations;

namespace QuasarApi.Database.Models;

public partial class Romaneio
{
    [Key]
    public int Id { get; set; }
    public string RomaneioNr { get; set; } = string.Empty;
    public DateTime? DataEmissao { get; set; }
    public int? VendedorId { get; set; }
    public int? Contato { get; set; }
    public int? SeparadorId { get; set; }
    public DateTime? DataSeparador { get; set; }
    public int? ConferenteId { get; set; }
    public DateTime? DataConferente { get; set; }
    public int? StatusId { get; set; }
    public string? Localizacao { get; set; }
    public string? CriadoPor { get; set; }
    public DateTime? CriadoEm { get; set; }
    public string? ModificadoPor { get; set; }
    public DateTime? ModificadoEm { get; set; }
    public int? FilialId { get; set; }
    public int? Itens { get; set; }
    public int? Pecas { get; set; }
    public string? OS { get; set; }
}
