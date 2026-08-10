
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using Simplify.Quasar.Models;
using System.Linq;

namespace Simplify.Quasar.ViewModels
{
    public class LoginViewModel
    {
        private Quasar_Entities db = new Quasar_Entities();

        public int? Id { get; set; }

        public int IdFilial { get; set; }

        public int? PerfilId { get; set; }
        public string Filial { get; set; }

        public IEnumerable<SelectListItem> FilialDDL
        {
            get
            {
                var ddl = db.Empresa.Select(t => new SelectListItem
                {
                    Value = t.Id.ToString(),
                    Text = t.Nome
                }).OrderBy(t => t.Value);
                return ddl;
            }
        }

        [DisplayName("Usuário")]
        public string Usuario { get; set; }
        public string Senha { get; set; }
        public bool SenhaExpirada { get; set; }
    }

    public class NewPasswordViewModel
    {
        public int UsuarioId { get; set; }

        [DisplayName("Senha")]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "{0} deve ter no mínimo {2} caracteres")]
        public string NovaSenha { get; set; }

        [DisplayName("Confirme a senha")]
        [DataType(DataType.Password)]
        [StringLength(10, MinimumLength = 6, ErrorMessage = "{0} deve ter no mínimo {2} caracteres")]
        [System.ComponentModel.DataAnnotations.Compare("NovaSenha", ErrorMessage = "As senhas informadas devem ser iguais")]
        public string ConfirmaNovaSenha { get; set; }
    }
}