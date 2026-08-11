using System.Collections.Generic;

namespace Simplify.Quasar.ViewModels
{
    public class ManualViewModel
    {
        public string PaginaAtual { get; set; }
        public string Titulo { get; set; }
        public string ConteudoHtml { get; set; }
        public string Pesquisa { get; set; }
        public IList<ManualNavigationItemViewModel> Paginas { get; set; }
        public IList<ManualSearchResultViewModel> Resultados { get; set; }
    }

    public class ManualNavigationItemViewModel
    {
        public string Slug { get; set; }
        public string Titulo { get; set; }
        public string Icone { get; set; }
    }

    public class ManualSearchResultViewModel
    {
        public string Slug { get; set; }
        public string Titulo { get; set; }
        public string Trecho { get; set; }
    }
}
