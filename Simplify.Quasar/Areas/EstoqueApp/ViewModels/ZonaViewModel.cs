using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace Simplify.Quasar.Areas.EstoqueApp.ViewModels
{
    public class ZonaViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Area")]
        public int? AreaId { get; set; }

        public string AreaNome { get; set; }

        [Required(ErrorMessage = "Informe o nome da zona.")]
        [StringLength(100)]
        public string Nome { get; set; }

        [StringLength(500)]
        public string Descricao { get; set; }

        [Display(Name = "Qtde. Linhas")]
        public int? QtdeLinha { get; set; }

        [Display(Name = "Pronto para despacho")]
        public bool ProntoDespacho { get; set; }

        [Display(Name = "Valor Pedido")]
        public decimal? ValorPedido { get; set; }

        [Display(Name = "Qtde. Clientes")]
        public int? QtdeCliente { get; set; }

        public bool Ativo { get; set; }
        public string CriadoPor { get; set; }
        public string CriadoPorNome { get; set; }
        public DateTime? CriadoEm { get; set; }
        public string ModificadoPor { get; set; }
        public string ModificadoPorNome { get; set; }
        public DateTime? ModificadoEm { get; set; }
        public int? FilialId { get; set; }
        public IEnumerable<SelectListItem> AreaDDL { get; set; }
    }
}
