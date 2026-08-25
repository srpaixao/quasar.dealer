namespace QuasarApi.DTO.Operations.Separacao;

public class AssumirTarefaResponseDto
{
    public string TarefaNr { get; set; } = string.Empty;
    public int ZonaId { get; set; }
    public string ZonaNome { get; set; } = string.Empty;
    public bool Reentrada { get; set; }
}
