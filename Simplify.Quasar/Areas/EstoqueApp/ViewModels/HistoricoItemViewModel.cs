using System;
using System.Collections.Generic;

namespace Simplify.Quasar.Areas.EstoqueApp.ViewModels
{
    public class HistoricoItemViewModel
    {
        public HistoricoItemViewModel()
        {
            Historicos = new List<HistoricoItemLinhaViewModel>();
        }

        public string ItemNr { get; set; }
        public bool ConsultaRealizada { get; set; }
        public string ConsultaMensagem { get; set; }
        public HistoricoItemCabecalhoViewModel Cabecalho { get; set; }
        public List<HistoricoItemLinhaViewModel> Historicos { get; set; }
    }

    public class HistoricoItemCabecalhoViewModel
    {
        public string ItemNr { get; set; }
        public string Descricao { get; set; }
        public string LocacaoCodigo { get; set; }
        public string LocacaoDescricao { get; set; }
        public int? Saldo { get; set; }
        public int? Indisponivel { get; set; }
    }

    public class HistoricoItemLinhaViewModel
    {
        public DateTime? Data { get; set; }
        public string Processo { get; set; }
        public string DocumentoNr { get; set; }
        public string DocumentoUrl { get; set; }
        public string TipoMovimento { get; set; }
        public decimal Quantidade { get; set; }
        public string Status { get; set; }
        public string Usuario { get; set; }
        public string Observacao { get; set; }
    }
}
