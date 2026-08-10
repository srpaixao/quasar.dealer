using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Simplify.Quasar.Areas.ExpedicaoApp.ViewModels
{
    public class TransportadoraViewModel
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "O tamanho máximo permitido para o campo {0} é de {1} caracteres")]
        public string Nome { get; set; }


        [Required]
        public string CNPJ { get; set; }

        [StringLength(100, ErrorMessage = "O tamanho máximo permitido para o campo {0} é de {1} caracteres")]
        [DisplayName("Endereço")]
        public string Endereco_Logradouro { get; set; }

        [StringLength(100, ErrorMessage = "O tamanho máximo permitido para o campo {0} é de {1} caracteres")]
        [DisplayName("Número")]
        public string Endereco_Numero { get; set; }

        [StringLength(100, ErrorMessage = "O tamanho máximo permitido para o campo {0} é de {1} caracteres")]
        public string Endereco_Complemento { get; set; }

        [StringLength(100, ErrorMessage = "O tamanho máximo permitido para o campo {0} é de {1} caracteres")]
        public string Endereco_Bairro { get; set; }

        [StringLength(100, ErrorMessage = "O tamanho máximo permitido para o campo {0} é de {1} caracteres")]
        public string Endereco_Cidade { get; set; }

        [DisplayName("Estado")]
        public string Endereco_UF { get; set; }
        public string NomeEstado { get; set; }
        public IEnumerable<SelectListItem> EstadoDDL { get; set; }

        [StringLength(8, ErrorMessage = "O tamanho máximo permitido para o campo {0} é de {1} caracteres")]
        public string Endereco_CEP { get; set; }

        [StringLength(100, ErrorMessage = "O tamanho máximo permitido para o campo {0} é de {1} caracteres")]
        public string Telefone1 { get; set; }

        [StringLength(100, ErrorMessage = "O tamanho máximo permitido para o campo {0} é de {1} caracteres")]
        public string Email { get; set; }

        public bool EmitirEtiqueta { get; set; }
        public bool EmitirRoteiro { get; set; }

        public int? StatusNotaFiscal { get; set; }
        public string NomeStatus { get; set; }
        public IEnumerable<SelectListItem>StatusDDL { get; set; }

        [StringLength(500, ErrorMessage = "O tamanho máximo permitido para o campo {0} é de {1} caracteres")]
        [DisplayName("Observações")]
        public string Observacoes { get; set; }

        public string CriadoPor { get; set; }
        public string CriadoPorNome { get; set; }
        public DateTime? CriadoEm { get; set; }
        public string ModificadoPor { get; set; }
        public string ModificadoPorNome { get; set; }
        public DateTime? ModificadoEm { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "O tamanho máximo permitido para o campo {0} é de {1} caracteres")]
        [DisplayName("Nome Fantasia")]
        public string Nome_Fantasia { get; set; }

        public bool Finalizar { get; set; }
    }
}