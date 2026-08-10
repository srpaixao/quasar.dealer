using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Simplify.Quasar.Areas.EstoqueApp.ViewModels
{
    public class LocacaoViewModel
    {
        public string Codigo { get; set; }
        public string Tipo { get; set; }
        public string Descricao { get; set; }
        public bool Bloqueado { get; set; }
        public string Status { get; set; }
        public int? AreaId { get; set; }
        public string AreaNome { get; set; }
        public int? EquipamentoId { get; set; }
        public string EquipamentoNome { get; set; }
        public string Curva { get; set; }
        public string Estrategia { get; set; }
        public string Observacoes { get; set; }
        public DateTime? CriadoEm { get; set; }
        public string CriadoPor { get; set; }
        public string CriadoPorNome { get; set; }
        public DateTime? ModificadoEm { get; set; }
        public string ModificadoPor { get; set; }
        public string ModificadoPorNome { get; set; }
        public int? FilialId { get; set; }
    }
}