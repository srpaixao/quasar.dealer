namespace QuasarApi.DTO.Operations.Separacao;

public class ConfirmarLinhaResponseDto
{
    public bool Finalizada { get; set; }
    public string Mensagem { get; set; } = string.Empty;
    public TarefaLinhaDto? ProximaLinha { get; set; }
}
