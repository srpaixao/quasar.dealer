using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Simplify.Quasar.Areas.RecebimentoApp.ViewModels
{
    public class ItensByVolumeViewModel
    {
        public int NfItemId { get; set; }
        public string NumeroNF { get; set; }
        public string ItemNr { get; set; }
        public string ItemDescricao { get; set; }
        public bool ItemCritico { get; set; }
        public string ObservacaoItemCritico { get; set; }
        public string Locacao { get; set; }
        public decimal Quantidade { get; set; }
        public decimal? QtdConferida { get; set; }
        public decimal? QtdArmazenada { get; set; }
        public decimal? Diferenca { get; set; }
        public bool Conferido { get; set; }
        public string SituacaoConferencia { get; set; }
        public string UsuarioConferencia { get; set; }
        public DateTime? DtHrConferencia { get; set; }
        public string UsuarioArmazenagem { get; set; }
        public DateTime? DtHrArmazenagem { get; set; }
        public DateTime? ModificadoEm { get; set; }
        public int StatusId { get; set; }
        public string StatusNome { get; set; }
        public bool HabilitarCheckbox { get; set; }
    }
}
