using System;

namespace Simplify.Quasar.Areas.DevolucaoApp.ViewModels
{
    public class DevolucaoConsultaViewModel
    {
        public int Id { get; set; }
        public string ControleNr { get; set; }
        public string Cliente { get; set; }
        public string Transportadora { get; set; }
        public string StatusNome { get; set; }
        public DateTime? DataCadastro { get; set; }
    }
}
