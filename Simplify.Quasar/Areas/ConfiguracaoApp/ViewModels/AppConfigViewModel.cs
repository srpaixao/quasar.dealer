using System.Collections.Generic;
using System.Web.Mvc;

namespace Simplify.Quasar.Areas.ConfiguracaoApp.ViewModels
{
    public class AppConfigViewModel
    {
        public int Id { get; set; }
        public string Nome { get; set; }
        public string Descricao { get; set; }
        public string Valor { get; set; }
        public string ValorOriginal { get; set; }
        public int? FilialId { get; set; }
        public string FilialNome { get; set; }
        public bool UsaDDLValor { get; set; }
        public IEnumerable<SelectListItem> ValorDDL { get; set; }
    }
}
