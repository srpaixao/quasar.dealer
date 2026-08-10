using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Simplify.Quasar.Areas.ExpedicaoApp.ViewModels
{
    public class RotaViewModel
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "O tamanho máximo permitido para o campo {0} é de {1} caracteres")]
        public string Nome { get; set; }

        [StringLength(500, ErrorMessage = "O tamanho máximo permitido para o campo {0} é de {1} caracteres")]
        [DisplayName("Descrição")]
        public string Descricao { get; set; }

        public string Observacoes { get; set; }

        public int FilialId { get; set; }

        public string CriadoPor { get; set; }
        public string CriadoPorNome { get; set; }
        public DateTime? CriadoEm { get; set; }
        public string ModificadoPor { get; set; }
        public string ModificadoPorNome { get; set; }
        public DateTime? ModificadoEm { get; set; }
    }

}