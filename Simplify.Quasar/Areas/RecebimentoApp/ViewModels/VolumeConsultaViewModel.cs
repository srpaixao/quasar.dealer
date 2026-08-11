using System;
using System.Collections.Generic;

namespace Simplify.Quasar.Areas.RecebimentoApp.ViewModels
{
    public class VolumeConsultaViewModel
    {
        public string VolumeNr { get; set; }
        public bool ConsultaRealizada { get; set; }
        public string ConsultaMensagem { get; set; }
        public VolumeConsultaHeaderViewModel Header { get; set; }
        public IEnumerable<VolumeConsultaItemViewModel> Itens { get; set; }
    }

    public class VolumeConsultaHeaderViewModel
    {
        public string VolumeNr { get; set; }
        public string NotaFiscal { get; set; }
        public string Serie { get; set; }
        public string Emissor { get; set; }
        public string StatusNotaFiscal { get; set; }
        public DateTime? DataEmissao { get; set; }
        public decimal? ValorNotaFiscal { get; set; }
        public string Danfe { get; set; }
        public string Movimento { get; set; }
        public int QuantidadeItens { get; set; }
        public decimal QuantidadePecas { get; set; }
        public DateTime? CriadoEm { get; set; }
        public string CriadoPor { get; set; }
        public DateTime? ModificadoEm { get; set; }
        public string ModificadoPor { get; set; }
    }

    public class VolumeConsultaItemViewModel
    {
        public string NotaFiscal { get; set; }
        public string ItemNr { get; set; }
        public string Descricao { get; set; }
        public decimal Quantidade { get; set; }
        public decimal? QtdConferida { get; set; }
        public decimal? QtdArmazenada { get; set; }
        public decimal? Diferenca { get; set; }
        public string SituacaoConferencia { get; set; }
        public string UsuarioConferencia { get; set; }
        public DateTime? DtHrConferencia { get; set; }
        public string UsuarioArmazenagem { get; set; }
        public DateTime? DtHrArmazenagem { get; set; }
        public string Pedido { get; set; }
        public string StatusItem { get; set; }
        public string Observacao { get; set; }
        public DateTime? ModificadoEm { get; set; }
        public string ModificadoPor { get; set; }
    }

    public class NotaFiscalConsultaViewModel
    {
        public string NotaFiscalNr { get; set; }
        public bool ConsultaRealizada { get; set; }
        public string ConsultaMensagem { get; set; }
        public NotaFiscalConsultaHeaderViewModel Header { get; set; }
        public IEnumerable<NotaFiscalConsultaItemViewModel> Itens { get; set; }
    }

    public class NotaFiscalConsultaHeaderViewModel
    {
        public string NotaFiscal { get; set; }
        public string Emissor { get; set; }
        public string StatusNotaFiscal { get; set; }
        public DateTime? DataEmissao { get; set; }
        public decimal? ValorNotaFiscal { get; set; }
        public string Movimento { get; set; }
        public int QuantidadeItens { get; set; }
        public decimal QuantidadePecas { get; set; }
        public DateTime? CriadoEm { get; set; }
        public string CriadoPor { get; set; }
        public DateTime? ModificadoEm { get; set; }
        public string ModificadoPor { get; set; }
    }

    public class NotaFiscalConsultaItemViewModel
    {
        public string VolumeNr { get; set; }
        public string ItemNr { get; set; }
        public string Descricao { get; set; }
        public decimal Quantidade { get; set; }
        public decimal? QtdConferida { get; set; }
        public decimal? QtdArmazenada { get; set; }
        public decimal? Diferenca { get; set; }
        public string SituacaoConferencia { get; set; }
        public string UsuarioConferencia { get; set; }
        public DateTime? DtHrConferencia { get; set; }
        public string UsuarioArmazenagem { get; set; }
        public DateTime? DtHrArmazenagem { get; set; }
        public string Pedido { get; set; }
        public string StatusItem { get; set; }
        public string Observacao { get; set; }
        public DateTime? ModificadoEm { get; set; }
        public string ModificadoPor { get; set; }
    }

    public class ItemConsultaViewModel
    {
        public string ItemNr { get; set; }
        public bool ConsultaRealizada { get; set; }
        public string ConsultaMensagem { get; set; }
        public ItemConsultaHeaderViewModel Header { get; set; }
        public IEnumerable<ItemConsultaItemViewModel> Itens { get; set; }
    }

    public class ItemConsultaHeaderViewModel
    {
        public string ItemNr { get; set; }
        public string Descricao { get; set; }
        public int QuantidadeNotasFiscais { get; set; }
        public int QuantidadeVolumes { get; set; }
        public decimal QuantidadePecas { get; set; }
        public DateTime? UltimaMovimentacao { get; set; }
    }

    public class ItemConsultaItemViewModel
    {
        public string NotaFiscal { get; set; }
        public string VolumeNr { get; set; }
        public decimal Quantidade { get; set; }
        public decimal? QtdConferida { get; set; }
        public decimal? QtdArmazenada { get; set; }
        public decimal? Diferenca { get; set; }
        public string SituacaoConferencia { get; set; }
        public string UsuarioConferencia { get; set; }
        public DateTime? DtHrConferencia { get; set; }
        public string UsuarioArmazenagem { get; set; }
        public DateTime? DtHrArmazenagem { get; set; }
        public string Pedido { get; set; }
        public string Emissor { get; set; }
        public string StatusItem { get; set; }
        public string Observacao { get; set; }
        public DateTime? DataEmissao { get; set; }
        public DateTime? ModificadoEm { get; set; }
        public string ModificadoPor { get; set; }
    }
}
