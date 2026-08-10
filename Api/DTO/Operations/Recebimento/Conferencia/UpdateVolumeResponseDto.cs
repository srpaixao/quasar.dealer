namespace QuasarApi.DTO.Operations.Recebimento.Conferencia
{
    public class UpdateVolumeResponseDto
    {
        public string Msg { get; set; } = string.Empty;
        public bool Erro { get; set; }
        public bool NotFound { get; set; }
        public bool Finalizado { get; set; }
        public int Total { get; set; }
        public int Pendentes { get; set; }
        public int Conferidos { get; set; }
        public int Incorretos { get; set; }
    }
}
