namespace QuasarApi.DTO.Operations.Recebimento.Conferencia
{
    public class VolumeResumoDto
    {
        public string? VolumeNr { get; set; }
        public string? NotaFiscalNr { get; set; }
        public int QtdeItens { get; set; }
        public int StatusId { get; set; }
        public string? StatusNome { get; set; }
        public DateTime? CriadoEm { get; set; }
    }
}
