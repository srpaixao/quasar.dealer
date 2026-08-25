namespace QuasarApi.DTO.Operations.Separacao;

public class ZonaDisponivelDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public int TarefasPendentes { get; set; }
}
