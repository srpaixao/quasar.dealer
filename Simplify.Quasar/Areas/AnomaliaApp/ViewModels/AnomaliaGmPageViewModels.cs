using System.Collections.Generic;
using Simplify.Quasar.Areas.AnomaliaApp.Services;

namespace Simplify.Quasar.Areas.AnomaliaApp.ViewModels
{
    public class AnomaliaConsultaPageViewModel
    {
        public AnomaliaConsultaPageViewModel()
        {
            Processos = new List<AnomaliaProcessoResumo>();
        }

        public string NumeroControle { get; set; }
        public string Tipo { get; set; }
        public int? StatusId { get; set; }
        public IList<AnomaliaProcessoResumo> Processos { get; set; }
    }

    public class AnomaliaDetalhePageViewModel
    {
        public AnomaliaProcessoResumo Processo { get; set; }
        public IList<AnomaliaItemDetalhe> Itens { get; set; }
    }
}
