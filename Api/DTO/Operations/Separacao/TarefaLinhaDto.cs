namespace QuasarApi.DTO.Operations.Separacao;

public class TarefaLinhaDto
{
    public string TarefaNr { get; set; } = string.Empty;
    public string ItemNr { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string Locacao { get; set; } = string.Empty;
    public int QuantidadeSolicitada { get; set; }
    public int QuantidadeSeparada { get; set; }
    public int QuantidadePendente { get; set; }
    public int LinhaAtual { get; set; }
    public int TotalLinhas { get; set; }
    public int LinhasSeparadas { get; set; }
}
