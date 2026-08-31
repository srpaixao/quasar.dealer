using System;
using System.Collections.Generic;

namespace Simplify.Quasar.Areas.AnomaliaApp.Services
{
    public static class AnomaliaGmStatusIds
    {
        public const int EmProcesso = 1;
        public const int Aceito = 2;
        public const int Rejeitado = 3;
        public const int Finalizado = 4;
    }

    public static class AnomaliaGmEventos
    {
        public const string ProcessoCriado = "PROCESSO_CRIADO";
        public const string ItemIncluido = "ITEM_INCLUIDO";
        public const string StatusAlterado = "STATUS_ALTERADO";
        public const string ArquivoGerado = "ARQUIVO_GERADO";
        public const string ReenvioGerado = "REENVIO_GERADO";
    }

    public class AnomaliaProcessoCadastroRequest
    {
        public AnomaliaProcessoCadastroRequest()
        {
            Itens = new List<AnomaliaItemCadastroRequest>();
        }

        public string Observacao { get; set; }
        public int? EmpresaId { get; set; }
        public IList<AnomaliaItemCadastroRequest> Itens { get; set; }
    }

    public class AnomaliaItemCadastroRequest
    {
        public string TipoCodigo { get; set; }
        public int NotaFiscalId { get; set; }
        public int NotaFiscalItemId { get; set; }
        public string VolumeNr { get; set; }
        public decimal QuantidadeReclamada { get; set; }
        public decimal? QuantidadeRecebida { get; set; }
        public string ItemRecebidoNr { get; set; }
        public string Observacao { get; set; }
        public bool? InstaladoVeiculo { get; set; }
        public string CondicaoEmbalagem { get; set; }
    }

    public class AnomaliaProcessoCadastroResult
    {
        public int AnomaliaId { get; set; }
        public string NumeroControle { get; set; }
        public int QuantidadeItens { get; set; }
    }

    public class AnomaliaSaldoSnapshot
    {
        public decimal QuantidadeBase { get; set; }
        public decimal QuantidadeConsumida { get; set; }
        public decimal SaldoDisponivel
        {
            get { return Math.Max(0, QuantidadeBase - QuantidadeConsumida); }
        }
    }

    public class AnomaliaReenvioRequest
    {
        public AnomaliaReenvioRequest()
        {
            AnomaliaItemIds = new List<int>();
        }

        public int AnomaliaId { get; set; }
        public IList<int> AnomaliaItemIds { get; set; }
    }

    public class AnomaliaArquivoLote
    {
        public AnomaliaArquivoLote()
        {
            ItemIds = new List<int>();
        }

        public string TipoCodigo { get; set; }
        public int Sequencia { get; set; }
        public bool Reenvio { get; set; }
        public IList<int> ItemIds { get; set; }
    }

    public class AnomaliaPesquisaItemResult
    {
        public int NotaFiscalId { get; set; }
        public int NotaFiscalItemId { get; set; }
        public string NotaFiscalNr { get; set; }
        public DateTime DataEmissao { get; set; }
        public string VolumeNr { get; set; }
        public string ItemNr { get; set; }
        public string Descricao { get; set; }
        public decimal QuantidadeNF { get; set; }
        public decimal? QuantidadeRecebida { get; set; }
        public decimal QuantidadeJaReclamada { get; set; }
        public decimal SaldoDisponivel { get; set; }
        public int PrazoDias { get; set; }
        public int DiasDecorridos { get; set; }
        public bool DentroDoPrazo { get; set; }
        public DateTime DataLimite { get; set; }
    }

    public class AnomaliaItemOcorrenciaResult
    {
        public int NotaFiscalId { get; set; }
        public int NotaFiscalItemId { get; set; }
        public string NotaFiscalNr { get; set; }
        public DateTime DataEmissao { get; set; }
        public string VolumeNr { get; set; }
        public string ItemNr { get; set; }
        public string Descricao { get; set; }
        public decimal QuantidadeNF { get; set; }
        public decimal? QuantidadeRecebida { get; set; }
        public decimal SaldoPadrao { get; set; }
        public decimal SaldoExcesso { get; set; }
        public int DiasDecorridos { get; set; }
        public int PrazoMinimoDias { get; set; }
        public int PrazoMaximoDias { get; set; }
        public DateTime DataLimiteMinima { get; set; }
        public DateTime DataLimiteMaxima { get; set; }
        public bool TodosTiposForaDoPrazo { get; set; }
        public bool SemSaldo { get; set; }
    }

    public class AnomaliaProcessoResumo
    {
        public int Id { get; set; }
        public string NumeroControle { get; set; }
        public DateTime DataAbertura { get; set; }
        public string Tipos { get; set; }
        public int QuantidadeItens { get; set; }
        public int EmProcesso { get; set; }
        public int Aceitos { get; set; }
        public int Rejeitados { get; set; }
        public string StatusDescricao { get; set; }
        public string CriadoPor { get; set; }
    }

    public class AnomaliaItemDetalhe
    {
        public int Id { get; set; }
        public string TipoCodigo { get; set; }
        public string NotaFiscalNr { get; set; }
        public DateTime DataEmissao { get; set; }
        public string VolumeNr { get; set; }
        public string ItemNr { get; set; }
        public string Descricao { get; set; }
        public decimal QuantidadeNF { get; set; }
        public decimal QuantidadeReclamada { get; set; }
        public int StatusId { get; set; }
        public string StatusDescricao { get; set; }
        public DateTime DataLimite { get; set; }
        public string Observacao { get; set; }
        public bool? InstaladoVeiculo { get; set; }
        public string CondicaoEmbalagem { get; set; }
    }
}
