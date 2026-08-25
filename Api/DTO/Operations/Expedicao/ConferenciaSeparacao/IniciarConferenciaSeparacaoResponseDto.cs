namespace QuasarApi.DTO.Operations.Expedicao.ConferenciaSeparacao;

public sealed class IniciarConferenciaSeparacaoResponseDto
{
    public int RomaneioId { get; set; }
    public string RomaneioNr { get; set; } = string.Empty;
    public bool Reentrada { get; set; }
}
