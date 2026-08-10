namespace QuasarApi.DTO.Operations.Recebimento.Armazenagem
{
    public class ValidarMaterial
    {
        public string CodigoMaterial { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public string Locacao { get; set; } = string.Empty;
        public string LocacaoFormatada { get; set; } = string.Empty;
    }
}
