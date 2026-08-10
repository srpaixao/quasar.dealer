using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Simplify.Quasar.ViewModels
{
    public class MenuViewModel
    {
        public int Id { get; set; }
        public string Titulo { get; set; }
        public string Area { get; set; }
        public string Controller { get; set; }
        public string Action { get; set; }
        public string Css { get; set; }
        public bool Status { get; set; }
        public int? Sequencia { get; set; }
        public int? Nivel { get; set; }
        public int? IdNivelSup { get; set; }
        public List<SubMenu> _menu;
    }

    public class SubMenu
    {
        public int Id { get; set; }
        public string Titulo { get; set; }
        public string Area { get; set; }
        public string Controller { get; set; }
        public string Action { get; set; }
        public string Css { get; set; }
        public bool Status { get; set; }
        public int? Sequencia { get; set; }
        public int? Nivel { get; set; }
        public int? IdNivelSup { get; set; }
        public DateTime? DatUltAtlz { get; set; }
    }
}