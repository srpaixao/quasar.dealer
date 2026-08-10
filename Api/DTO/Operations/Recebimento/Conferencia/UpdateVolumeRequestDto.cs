namespace QuasarApi.DTO.Operations.Recebimento.Conferencia
{
    public class UpdateVolumeRequestDto
    {
        public string Volume { get; set; } = string.Empty;
        public int Area { get; set; }
        public int? FilialId { get; set; }
        public string? Usuario { get; set; }
        public string Tipo { get; set; }
    }
}
