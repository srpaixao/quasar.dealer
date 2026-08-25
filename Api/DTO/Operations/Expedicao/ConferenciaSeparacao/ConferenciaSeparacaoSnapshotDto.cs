namespace QuasarApi.DTO.Operations.Expedicao.ConferenciaSeparacao;

public sealed class ConferenciaSeparacaoSnapshotDto
{
    public bool Finalizado { get; set; }
    public string Mensagem { get; set; } = string.Empty;
    public ConferenciaSeparacaoItemDto? ItemAtual { get; set; }
    public List<ConferenciaSeparacaoItemDto> Itens { get; set; } = new();
}
