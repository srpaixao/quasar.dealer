namespace QuasarApi.DTO.Operations.Expedicao.ConferenciaSeparacao;

public sealed class ConferenciaSeparacaoItemDto
{
    public int ZonaId { get; set; }
    public string Zona { get; set; } = string.Empty;
    public string ItemNr { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public int QuantidadePedido { get; set; }
    public int QuantidadeConferida { get; set; }
    public int QuantidadeFaltante { get; set; }
    public bool Atual { get; set; }
    public bool Finalizado { get; set; }
    public bool EmBusca { get; set; }
}
