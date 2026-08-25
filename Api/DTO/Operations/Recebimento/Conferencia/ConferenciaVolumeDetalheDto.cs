namespace QuasarApi.DTO.Operations.Recebimento.Conferencia;

public sealed class ConferenciaVolumeDetalheDto
{
    public string Volume { get; set; } = string.Empty;
    public List<ConferenciaVolumeItemDto> Itens { get; set; } = [];
}
