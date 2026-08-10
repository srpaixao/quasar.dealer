using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Simplify.Quasar.Areas.EstoqueApp.ViewModels
{
    public class EstoqueViewModel
    {
        public int Id { get; set; }
        public string Locacao { get; set; }
        public string ItemNr { get; set; }
        public string Descricao { get; set; }
        public int? Saldo { get; set; }
        public int? Indisponivel { get; set; }
        public int? PedidoPendente { get; set; }
        public decimal? ValorEstoque { get; set; }
        public string Range { get; set; }
        public DateTime? CriadoEm { get; set; }
        public string CriadoPor { get; set; }
        public DateTime? ModificadoEm { get; set; }
        public string ModificadoPor { get; set; }
        public int? FilialId { get; set; }
    }

    public class UploadArquivo
    {
        public HttpPostedFileBase Arquivo { set; get; }
    }
}