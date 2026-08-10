namespace QuasarApi.Database.Models;

public partial class Volume
{
    public string NotaFiscalNr { get; set; } = null!;
    public string VolumeNr { get; set; } = null!;
    public int StatusId { get; set; }
    public int AreaId { get; set; }
    public int QtdItens { get; set; }
    public bool Imprimir { get; set; }
    public string? Danfe { get; set; }
    public string? CriadoPor { get; set; }
    public DateTime? CriadoEm { get; set; }
    public string? ModificadoPor { get; set; }
    public DateTime? ModificadoEm { get; set; }
    public int? FilialId { get; set; }
}
