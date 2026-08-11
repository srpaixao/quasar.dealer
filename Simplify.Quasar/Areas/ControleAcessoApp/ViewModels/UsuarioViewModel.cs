using Simplify.Quasar.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web.Mvc;

namespace Simplify.Quasar.Areas.ControleAcessoApp.ViewModels
{
    public class UsuarioViewModel
    {

        private Quasar_Entities db = new Quasar_Entities();

        public int Id { get; set; }

        [DisplayName("Login")]
        [StringLength(100, MinimumLength = 4, ErrorMessage = "{0} deve ter no mínimo {2} e no máximo {1} caracteres")]
        [Required(ErrorMessage = "Campo obrigatório")]
        public string Login { get; set; }

        [DisplayName("Senha")]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "{0} deve ter no mínimo {2} e no máximo {1} caracteres")]
        public string Senha { get; set; }

        [DisplayName("Confirme a senha")]
        [DataType(DataType.Password)]
        [StringLength(010, MinimumLength = 6, ErrorMessage = "{0} deve ter no mínimo {2} e no máximo {1} caracteres")]
        [System.ComponentModel.DataAnnotations.Compare("Senha", ErrorMessage = "As senhas informadas devem ser iguais")]
        public string ConfirmaSenha { get; set; }

        [DisplayName("Nome")]
        [StringLength(100, ErrorMessage = "{0} deve ter no máximo {1} caracteres")]
        [Required(ErrorMessage = "Campo obrigatório")]
        public string Nome { get; set; }

        [DisplayName("Email")]
        [StringLength(100, ErrorMessage = "{0} deve ter no máximo {1} caracteres")]
        public string Email { get; set; }

        [DisplayName("Telefone")]
        [StringLength(100, ErrorMessage = "{0} deve ter no máximo {1} caracteres")]
        public string Telefone { get; set; }

        [DisplayName("Empresa")]
        public int? EmpresaId { get; set; }

        public int? FuncaoId { get; set; }
        public string NomeFuncao { get; set; }
        //public IEnumerable<SelectListItem> FuncaoDDL { get; set; }

        //public IEnumerable<SelectListItem> FuncaoDDL
        //{
        //    get
        //    {
        //        var ddl = db.Funcao.Select(t => new SelectListItem
        //        {
        //            Value = t.Id.ToString(),
        //            Text = t.Nome
        //        }).OrderBy(t => t.Text);
        //        return ddl;
        //    }
        //}


        public int? FilialId { get; set; }
        public string NomeEmpresa { get; set; }
        public IEnumerable<SelectListItem> EmpresaDDL { get; set; }
        public string StatusEmpresaCss { get; set; }
        
        [DisplayName("Área")]
        public int? AreaId { get; set; }
        public string NomeArea { get; set; }
        public IEnumerable<SelectListItem> AreaDDL { get; set; }

        [DisplayName("Perfil de acesso")]
        [Required(ErrorMessage = "Campo obrigatório")]
        public int PerfilId { get; set; }
        public string NomePerfil { get; set; }
        public IEnumerable<SelectListItem> PerfilDDL { get; set; }
        public bool PerfilSomenteLeitura { get; set; }

        [DisplayName("Senha expirada")]
        public bool SenhaExpirada { get; set; }

        public string SenhaGerada { get; set; }

        [DisplayName("Acesso bloqueado")]
        public bool AcessoBloqueado { get; set; }

        [DisplayName("Último acesso")]
        public DateTime? UltimoAcesso { get; set; }

        public DateTime? CriadoEm { get; set; }
        public string CriadoPor { get; set; }
        public string CriadoPorNome { get; set; }
        public DateTime? ModificadoEm { get; set; }
        public string ModificadoPor { get; set; }
        public string ModificadoPorNome { get; set; }
        public bool UsuarioLogado { get; set; }
        public string FuncionalidadeAtual { get; set; }
        public string RotaAtual { get; set; }
        public DateTime? UltimaAtividade { get; set; }
    }
}
