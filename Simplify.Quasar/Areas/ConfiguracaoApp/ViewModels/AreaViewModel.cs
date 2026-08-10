using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Simplify.Quasar.Areas.ConfiguracaoApp.ViewModels
{
    public class AreaViewModel
    {
        public int Id { get; set; }
        public string Nome { get; set; }
        public string Descricao { get; set; }
        public Boolean Etiqueta { get; set; }
        public string CriadoPor { get; set; }
        public string CriadoPorNome { get; set; }

        public string Tipo { get; set; }

        public int FilialId { get; set; }
        public DateTime? CriadoEm { get; set; }
        public string ModificadoPor { get; set; }
        public string ModificadoPorNome { get; set; }
        public DateTime? ModificadoEm { get; set; }
        public int QtdeArmazenagem { get; set; }
        public int QtdeSeparacao { get; set; }
    }
}