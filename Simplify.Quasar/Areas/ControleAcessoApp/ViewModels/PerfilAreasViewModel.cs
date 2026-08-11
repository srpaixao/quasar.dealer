using System.Collections.Generic;

namespace Simplify.Quasar.Areas.ControleAcessoApp.ViewModels
{
    public class PerfilAreasViewModel
    {
        public int PerfilId { get; set; }
        public string PerfilNome { get; set; }
        public List<AreaPerfilItem> Areas { get; set; }

        public PerfilAreasViewModel()
        {
            Areas = new List<AreaPerfilItem>();
        }
    }

    public class AreaPerfilItem
    {
        public string Area { get; set; }
        public string Titulo { get; set; }
        public int QuantidadeMenus { get; set; }
        public bool Selecionada { get; set; }
    }
}
