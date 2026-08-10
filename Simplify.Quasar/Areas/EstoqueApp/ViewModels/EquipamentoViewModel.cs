using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Simplify.Quasar.Areas.EstoqueApp.ViewModels
{
    public class EquipamentoViewModel
    {
        public int Id { get; set; }
        public string Nome { get; set; }
        public string Tipo { get; set; }
        public string Descricao { get; set; }
        public bool Bloqueado { get; set; }
        public string Observacoes { get; set; }
        public string CriadoPor { get; set; }
        public string CriadoPorNome { get; set; }
        public DateTime? CriadoEm { get; set; }
        public string ModificadoPor { get; set; }
        public string ModificadoPorNome { get; set; }
        public DateTime? ModificadoEm { get; set; }
        public int? FilialId { get; set; }
    }
}