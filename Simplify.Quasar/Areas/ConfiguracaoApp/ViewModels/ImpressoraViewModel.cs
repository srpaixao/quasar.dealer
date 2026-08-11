using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Simplify.Quasar.Areas.ConfiguracaoApp.ViewModels
{
    public class ImpressoraViewModel
    {
        public int Id { get; set; }
        public string Nome { get; set; }
        public string IP { get; set; }
        public int Porta { get; set; }

        public int? FilialId { get; set; }
        public string FilialNome { get; set; }
        public string Localizacao { get; set; }
        public string Fabricante { get; set; }
        public string Modelo { get; set; }
        public string CriadoPor { get; set; }
        public DateTime? CriadoEm { get; set; }
        public string ModificadoPor { get; set; }
        public DateTime? ModificadoEm { get; set; }
    }
}
