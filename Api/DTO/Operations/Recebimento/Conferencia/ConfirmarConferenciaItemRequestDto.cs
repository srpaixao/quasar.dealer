namespace QuasarApi.DTO.Operations.Recebimento.Conferencia;

public sealed class ConfirmarConferenciaItemRequestDto
{
    public decimal? QtdConferida { get; set; }
    public bool Conferido { get; set; }
    public bool ConfirmarDivergencia { get; set; }
    public DateTime? ModificadoEmEsperado { get; set; }
}
