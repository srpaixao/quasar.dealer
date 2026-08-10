using System.ComponentModel.DataAnnotations;

namespace QuasarApi.Database.Models;

public partial class RetornoInternoItem
{
    [Key]
    public int Id { get; set; }

    public int? RetornoInternoId { get; set; }

    public string? ItemNr { get; set; }

    public decimal? Quantidade { get; set; }

    public int? StatusRetornoId { get; set; }

    public decimal? QtdArmazenada { get; set; }

    public string? CriadoPor { get; set; }

    public DateTime? CriadoEm { get; set; }

    public string? ModificadoPor { get; set; }

    public DateTime? ModificadoEm { get; set; }

     public int? FilialId { get; set; }

}
