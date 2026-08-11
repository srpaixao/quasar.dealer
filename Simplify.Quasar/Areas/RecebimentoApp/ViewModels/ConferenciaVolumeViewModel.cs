using System.Collections.Generic;

namespace Simplify.Quasar.Areas.RecebimentoApp.ViewModels
{
    public class ConferenciaVolumeViewModel
    {
        public string VolumeNr { get; set; }
        public bool ConsultaRealizada { get; set; }
        public string Mensagem { get; set; }
        public List<ItensByVolumeViewModel> Itens { get; set; }
    }
}
