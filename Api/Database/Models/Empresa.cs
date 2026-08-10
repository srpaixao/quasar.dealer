namespace QuasarApi.Database.Models;

using System.ComponentModel.DataAnnotations;

public partial class Empresa
{
    [Key]
    public int Id { get; set; }
    public string Nome { get; set; } = null!;
    public string Cnpj { get; set; } = null!;
    public int StatusId { get; set; }
    public int? TipoId { get; set; }
    public string? EnderecoLogradouro { get; set; }
    public string? EnderecoNumero { get; set; }
    public string? EnderecoComplemento { get; set; }
    public string? EnderecoBairro { get; set; }
    public string? EnderecoCidade { get; set; }
    public string? EnderecoUf { get; set; }
    public string? EnderecoCep { get; set; }
    public string? Telefone1 { get; set; }
    public string? Telefone2 { get; set; }
    public string? Telefone3 { get; set; }
    public string? Observacoes { get; set; }
    public string? CriadoPor { get; set; }
    public DateTime? CriadoEm { get; set; }
    public string? ModificadoPor { get; set; }
    public DateTime? ModificadoEm { get; set; }
}
