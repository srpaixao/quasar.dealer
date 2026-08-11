using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace Simplify.Quasar.Areas.SeparacaoApp.ViewModels
{
    public class AreaPedidoViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Informe o vendedor.")]
        [StringLength(100, ErrorMessage = "O vendedor deve ter no máximo 100 caracteres.")]
        public string UsuarioApollo { get; set; }

        public int? AreaId { get; set; }

        public string Area { get; set; }

        public bool Mapa { get; set; }

        public IEnumerable<SelectListItem> AreaRomaneioDDL { get; set; }
    }

    public class AreaRomaneioViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Informe a área.")]
        [StringLength(100, ErrorMessage = "A área deve ter no máximo 100 caracteres.")]
        public string Area { get; set; }

        [Range(0, 9999, ErrorMessage = "Informe uma prioridade válida.")]
        public int? Prioridade { get; set; }

        public bool Separar { get; set; }

        public bool Conferir { get; set; }

        public bool Alocar { get; set; }

        public bool Mapa { get; set; }
    }
}
