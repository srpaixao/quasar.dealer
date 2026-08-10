using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Simplify.Quasar.Areas.SeparacaoApp.ViewModels
{
    public class RomaneioViewModel
    {
        public int Id { get; set; }
        public string RomaneioNr { get; set; }
        public DateTime? DataEmissao { get; set; }
        public string OS { get; set; }
        public int? SeparadorId { get; set; }
        public string Separador { get; set; }
        public DateTime? DataSeparador { get; set; }
        public int? ConferenteId { get; set; }
        public string Conferente { get; set; }
        public DateTime? DataConferente { get; set; }
        public string ContatoNr { get; set; }
        public int? VendedorId { get; set; }
        public int? StatusId { get; set; }
        public int? FuncaoId { get; set; }
        public string Localizacao { get; set; }
        public IEnumerable<SelectListItem> SeparadorDDL { get; set; }
        public int TotalRomaneioPendente { get; set; }
        public int TotalRomaneioLancar { get; set; }
        public int TotalRomaneioSeparar { get; set; }
        public int TotalRemaneioOcorrencia { get; set; }
        public int TotalRomaneioFinalizado { get; set; }

        public DateTime? CriadoEm { get; set; }
        public string CriadoPor { get; set; }
        public DateTime? ModificadoEm { get; set; }
        public string ModificadoPor { get; set; }
    }

    public class UploadArquivo
    {
        public HttpPostedFileBase Arquivo { set; get; }
    }

}