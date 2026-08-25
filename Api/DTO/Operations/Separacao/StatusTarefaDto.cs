namespace QuasarApi.DTO.Operations.Separacao;

public class StatusTarefaDto
{
    public string TarefaNr { get; set; } = string.Empty;
    public bool Finalizada { get; set; }
    public int TotalLinhas { get; set; }
    public int LinhasSeparadas { get; set; }
    public int LinhasPendentes { get; set; }
}
