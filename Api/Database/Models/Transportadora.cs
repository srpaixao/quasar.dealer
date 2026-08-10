namespace QuasarApi.Database.Models
{
    public class Transportadora
    {
        public int Id { get; set; }
        public string? Nome { get; set; }
        public string? CNPJ { get; set; }
        public int StatusId { get; set; }
        public int? TipoId { get; set; }
        public string? Endereco_Logradouro { get; set; }
        public string? Endereco_Numero { get; set; }
        public string? Endereco_Complemento { get; set; }
        public string? Endereco_Bairro { get; set; }
        public string? Endereco_Cidade { get; set; }
        public string? Endereco_UF { get; set; }
        public string? Endereco_CEP { get; set; }
        public string? Telefone1 { get; set; }
        public string? Telefone2 { get; set; }
        public string? Telefone3 { get; set; }
        public string? Observacoes { get; set; }
        public bool EmitirEtiqueta { get; set; }
        public bool EmitirRoteiro { get; set; }
        public int? StatusNotaFiscal { get; set; }
        public string? CriadoPor { get; set; }
        public DateTime? CriadoEm { get; set; }
        public string? ModificadoPor { get; set; }
        public DateTime? ModificadoEm { get; set; }
        public string? Nome_Fantasia { get; set; }
        public bool Finalizar { get; set; }
        public int? FilialId { get; set; }
    }
}

