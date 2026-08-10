using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Simplify.Quasar.Areas.EstoqueApp.ViewModels
{
    public class ConfigUploadViewModel
    {
        public int Id { get; set; }
        public string Nome { get; set; }
        public string Descricao { get; set; }
        public string Valor { get; set; }
        public DateTime? CriadoEm { get; set; }
        public string CriadoPor { get; set; }
        public string CriadoPorNome { get; set; }
        public DateTime? ModificadoEm { get; set; }
        public string ModificadoPor { get; set; }
        public string ModificadoPorNome { get; set; }
    }
}