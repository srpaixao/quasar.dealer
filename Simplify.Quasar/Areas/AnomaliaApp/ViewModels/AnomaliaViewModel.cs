using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Simplify.Quasar.Areas.AnomaliaApp.ViewModels
{
    public class AnomaliaViewModel
    {
        public int Id { get; set; }
        public int FornecedorId { get; set; }
        public string FornecedorNome { get; set; }

        public int StatusId { get; set; }
        public string StatusDescricao { get; set; }
        
        public string Controle { get; set; }
        public string Observacoes { get; set; }
        public string CriadoPor { get; set; }
        public DateTime? CriadoEm { get; set; }
        public string ModificadoPor { get; set; }
        public DateTime? ModificadoEm { get; set; }

        public List<AnomaliaItemViewModel> _itensAnomalia { get; set; }
        public List<AnomaliaItemViewModel> _itensDanificado { get; set; }
    }

    public class AnomaliaItemViewModel
    {
        public int Sequencial { get; set; }

        public int Id { get; set; }
        public int AnomaliaId { get; set; }

        public int TipoId { get; set; }
        public string TipoDescricao { get; set; }

        public int StatusId { get; set; }
        public string StatusDescricao { get; set; }

        public int NotaFiscalItemId { get; set; }
        public decimal QtdRecebida { get; set; }
        public decimal QtdReclamada { get; set; }
        public bool Instalado { get; set; }

        public int StatusEmbalagemId { get; set; }
        public string StatusEmbalagemDescricao { get; set; }

        public string Observacoes { get; set; }
        public string CriadoPor { get; set; }
        public DateTime? CriadoEm { get; set; }
        public string ModificadoPor { get; set; }
        public DateTime? ModificadoEm { get; set; }

    }
}