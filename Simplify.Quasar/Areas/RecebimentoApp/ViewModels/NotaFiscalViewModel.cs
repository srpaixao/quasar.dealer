using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Simplify.Quasar.Areas.RecebimentoApp.ViewModels
{
    public class NotaFiscalViewModel
    {
        public int Id { get; set; }
        public string Movimento { get; set; }

        public string Numero { get; set; }
        public string Serie { get; set; }

        public string Emissor { get; set; }
        public string NomeEmissor { get; set; }

        public int TipoId { get; set; }
        public string TipoNF { get; set; }

        public int StatusId { get; set; }
        public string StatusNF { get; set; }

        public string OrigemNF { get; set; }

        public DateTime? DataEmissao { get; set; }
        public decimal? Valor { get; set; }
        public string Descricao { get; set; }
        public string Danfe { get; set; }

        public string Observacoes { get; set; }

        public DateTime? RecebidoAdmEm { get; set; }
        public string RecebidoAdmPor { get; set; }
        public string RecebidoAdmPorNome { get; set; }

        public DateTime? CriadoEm { get; set; }
        public string CriadoPor { get; set; }
        public string CriadoPorNome { get; set; }
        public DateTime? ModificadoEm { get; set; }
        public string ModificadoPor { get; set; }
        public string ModificadoPorNome { get; set; }

        public int QtdItensNF { get; set; }
        public int QtdVolumes { get; set; }
        public int QtdItens { get; set; }
        public decimal? QtdTotal { get; set; }

        public List<ItemNotaFiscalViewModel> _itens { get; set; }

        public int TotalItens { get; set; }
        public int TotalVolumes { get; set; }
        public int TotalNFiscais { get; set; }

        public int? FilialId { get; set; }



    }

    public class ItemNotaFiscalViewModel
    {
        public int Id { get; set; }
        public int NotaFiscalId { get; set; }
        public string ItemNr { get; set; }
        public string ItemDesc { get; set; }
        public decimal Quantidade { get; set; }
        public string Volume { get; set; }
        public string Pedido { get; set; }

        public string Status { get; set; }
        public string Observacoes { get; set; }
        public DateTime? CriadoEm { get; set; }
        public string CriadoPor { get; set; }
        public string CriadoPorNome { get; set; }
        public DateTime? ModificadoEm { get; set; }
        public string ModificadoPor { get; set; }
        public string ModificadoPorNome { get; set; }
    }

    public class NotaFiscalRedeViewModel
    {
        public string Fornecedor { get; set; }
        public string NomeFornecedor { get; set; }
        public string NumeroNF { get; set; }
        public string Danfe { get; set; }
        public string ItemNr { get; set; }
        public string Descricao { get; set; }
        //public string Locacao { get; set; }
        public int Quantidade { get; set; }

        public bool AddFornecedor { get; set; }
    }

    public class NotaFiscalTransfViewModel
    {
        public string Filial { get; set; }
        public string NomeFilial { get; set; }
        public string NumeroNF { get; set; }
        public string Danfe { get; set; }
        public string ItemNr { get; set; }
        public string Descricao { get; set; }
        public int Quantidade { get; set; }
    }
}