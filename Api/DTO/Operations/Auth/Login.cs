namespace QuasarApi.DTO.Operations.Auth
{
    public class Login
    {
        public string Usuario { get; set; } = string.Empty;
        public string Senha { get; set; } = string.Empty;
        public int EmpresaId { get; set; }
        public int FilialId { get; set; }
    }
}
