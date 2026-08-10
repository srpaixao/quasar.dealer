using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Simplify.Quasar.Areas.RecebimentoApp.ViewModels
{
    public class FornecedorViewModel
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

        public string EstadoUF { get; set; }
        public IEnumerable<SelectListItem> EstadoDDL { get; set; }
        public string EstadoNome { get; set; }

        [StringLength(8, ErrorMessage = "O tamanho máximo permitido para o campo {0} é de {1} caracteres")]
        public string Endereco_CEP { get; set; }

        [StringLength(100, ErrorMessage = "O tamanho máximo permitido para o campo {0} é de {1} caracteres")]
        public string Telefone1 { get; set; }

        [StringLength(100, ErrorMessage = "O tamanho máximo permitido para o campo {0} é de {1} caracteres")]
        public string Telefone2 { get; set; }

        [StringLength(100, ErrorMessage = "O tamanho máximo permitido para o campo {0} é de {1} caracteres")]
        public string Telefone3 { get; set; }

        [StringLength(500, ErrorMessage = "O tamanho máximo permitido para o campo {0} é de {1} caracteres")]
        [DisplayName("Observações")]
        public string Observacoes { get; set; }

        public int? StatusId { get; set; }
        public int? TipoId { get; set; }
    }

}