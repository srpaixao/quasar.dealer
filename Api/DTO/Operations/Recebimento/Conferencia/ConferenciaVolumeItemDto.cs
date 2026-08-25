namespace QuasarApi.DTO.Operations.Recebimento.Conferencia;

public sealed class ConferenciaVolumeItemDto
{
    public int Id { get; set; }
    public int NotaFiscalId { get; set; }
    public string NotaFiscal { get; set; } = string.Empty;
    public string Item { get; set; } = string.Empty;
    public bool ItemCritico { get; set; }
    public string? ObservacaoItemCritico { get; set; }
    public string Volume { get; set; } = string.Empty;
    public string Pedido { get; set; } = string.Empty;
    public decimal Quantidade { get; set; }
    public decimal? QtdConferida { get; set; }
    public decimal? QtdArmazenada { get; set; }
    public decimal? Diferenca { get; set; }
    public bool Conferido { get; set; }
    public string Situacao { get; set; } = string.Empty;
    public string? UsuarioConferencia { get; set; }
    public DateTime? DtHrConferencia { get; set; }
    public string? UsuarioArmazenagem { get; set; }
    public DateTime? DtHrArmazenagem { get; set; }
    public DateTime? ModificadoEm { get; set; }
}
