namespace QuasarApi.DTO.Operations.Estoque
{
    public class ConsultarLocacao
    {
        public string Codigo { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public bool Bloqueado { get; set; }
        public int? AreaId { get; set; }
        public string Area { get; set; } = string.Empty;
        public int? EquipamentoId { get; set; }
        public string Equipamento { get; set; } = string.Empty;
        public string Curva { get; set; } = string.Empty;
        public string Estrategia { get; set; } = string.Empty;
        public string Observacoes { get; set; } = string.Empty;
        public int? FilialId { get; set; } = 0;
        public List<ConsultarItem>? Itens { get; set; }
    }
}
