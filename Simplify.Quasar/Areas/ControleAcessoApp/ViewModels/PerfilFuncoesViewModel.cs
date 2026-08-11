using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Web.Mvc;

namespace Simplify.Quasar.Areas.ControleAcessoApp.ViewModels
{
    public class PerfilFuncoesViewModel
    {
        public int PerfilId { get; set; }
        public string PerfilNome { get; set; }
        public List<FuncaoPerfilItem> Funcoes { get; set; }

        public PerfilFuncoesViewModel()
        {
            Funcoes = new List<FuncaoPerfilItem>();
        }
    }

    public class FuncaoPerfilItem
    {
        public int FuncaoId { get; set; }
        public string Codigo { get; set; }
        public string DescPTBR { get; set; }
        public string CodComponente { get; set; }
        public string Controller { get; set; }
        public string Action { get; set; }
        public string TituloMenu { get; set; }
        public bool Selecionada { get; set; }
    }
}
