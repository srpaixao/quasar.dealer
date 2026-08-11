using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace Simplify.Quasar.Areas.ControleAcessoApp.ViewModels
{
    public class FuncaoViewModel
    {
        public int Id { get; set; }

        [DisplayName("Código")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "{0} deve ter no mínimo {2} e no máximo {1} caracteres")]
        [Required(ErrorMessage = "Campo obrigatório")]
        public string Codigo { get; set; }

        [DisplayName("Descrição (PT-BR)")]
        [StringLength(200, ErrorMessage = "{0} deve ter no máximo {1} caracteres")]
        [Required(ErrorMessage = "Campo obrigatório")]
        public string DescPTBR { get; set; }

        [DisplayName("Descrição (ES)")]
        [StringLength(200, ErrorMessage = "{0} deve ter no máximo {1} caracteres")]
        public string DescES { get; set; }

        [DisplayName("Componente")]
        [StringLength(50, ErrorMessage = "{0} deve ter no máximo {1} caracteres")]
        public string CodComponente { get; set; }

        [DisplayName("Menu")]
        public int? IdMenu { get; set; }
        public string TituloMenu { get; set; }
        public IEnumerable<SelectListItem> MenuDDL { get; set; }

        [DisplayName("Controller")]
        [StringLength(100, ErrorMessage = "{0} deve ter no máximo {1} caracteres")]
        public string Controller { get; set; }

        [DisplayName("Action")]
        [StringLength(100, ErrorMessage = "{0} deve ter no máximo {1} caracteres")]
        public string Action { get; set; }

        [DisplayName("Ativo")]
        public bool Status { get; set; }

        [DisplayName("Filial")]
        public int? FilialId { get; set; }
        public string NomeFilial { get; set; }
        public IEnumerable<SelectListItem> FilialDDL { get; set; }

        public DateTime? CriadoEm { get; set; }
        public string CriadoPor { get; set; }
        public string ModificadoEm { get; set; }
        public string ModificadoPor { get; set; }
    }
}
