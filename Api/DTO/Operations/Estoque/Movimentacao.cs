namespace QuasarApi.DTO.Operations.Estoque
{
    public class CriarMovimentacao
    {
        public string ItemNr { get; set; } = string.Empty;
        public string? LocacaoOrigem { get; set; }
        public int? QtdOrigem { get; set; }
        public string? LocacaoEspera { get; set; }
        public int? FilialId { get; set; }
        public string? CriadoPor { get; set; }
    }

    public class FinalizarMovimentacao
    {
        public int Id { get; set; }
        public string? LocacaoDestino { get; set; }
        public int? QtdDestino { get; set; }
        public string? FinalizadoPor { get; set; }
        public string? UrlDMS { get; set; }
        public string? Payload { get; set; }
        public string? Response { get; set; }
    }

    public class ConsultarMovimentacao
    {
        public int Id { get; set; }
        public string ItemNr { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public string UN { get; set; } = string.Empty;
        public string? LocacaoOrigem { get; set; }
        public int? QtdOrigem { get; set; }
        public string? LocacaoEspera { get; set; }
        public string? LocacaoDestino { get; set; }
        public int? QtdDestino { get; set; }
        public int? FilialId { get; set; }
        public string? CriadoPor { get; set; }
        public DateTime? CriadoEm { get; set; }
        public string? FinalizadoPor { get; set; }
        public DateTime? FinalizadoEm { get; set; }
    }

    public class MovimentacaoLocacaoEspera
    {
        public int Id { get; set; }
        public string ItemNr { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public string UN { get; set; } = string.Empty;
        public string LocacaoOrigem { get; set; } = string.Empty;
        public string LocacaoEspera { get; set; } = string.Empty;
        public string LocacaoDestino { get; set; } = string.Empty;
        public int? FilialId { get; set; }
        public int Quantidade { get; set; }
        public DateTime? CriadoEm { get; set; }
    }

    public class LocacaoEsperaResumo
    {
        public string LocacaoEspera { get; set; } = string.Empty;
        public bool MovimentacaoCorreta { get; set; }
        public List<MovimentacaoLocacaoEspera> Itens { get; set; } = new();
    }
}

