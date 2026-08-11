using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace Simplify.Quasar.Areas.ControleAcessoApp.ViewModels
{
    public class PerfilViewModel
    {
        public int Id { get; set; }

        [DisplayName("Nome")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "{0} deve ter no mínimo {2} e no máximo {1} caracteres")]
        [Required(ErrorMessage = "Campo obrigatório")]
        public string Nome { get; set; }

        [DisplayName("Descrição")]
        [StringLength(500, ErrorMessage = "{0} deve ter no máximo {1} caracteres")]
        public string Descricao { get; set; }

        [DisplayName("Filial")]
        public int? FilialId { get; set; }

        public string NomeFilial { get; set; }
        public IEnumerable<SelectListItem> FilialDDL { get; set; }

        public string CriadoPor { get; set; }
        public string CriadoPorNome { get; set; }
        public DateTime? CriadoEm { get; set; }
        public string ModificadoPor { get; set; }
        public string ModificadoPorNome { get; set; }
        public DateTime? ModificadoEm { get; set; }
    }
}
