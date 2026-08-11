using System;

namespace Simplify.Quasar.Areas.ControleAcessoApp.ViewModels
{
    public class AtividadeViewModel
    {
        public string SessionId { get; set; }
        public int UsuarioId { get; set; }
        public string Login { get; set; }
        public string Nome { get; set; }
        public string Filial { get; set; }
        public string Funcionalidade { get; set; }
        public string Rota { get; set; }
        public DateTime LoginEm { get; set; }
        public DateTime UltimaAtividade { get; set; }
    }
}
