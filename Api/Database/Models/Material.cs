using System.ComponentModel.DataAnnotations;

namespace QuasarApi.Database.Models
{
    public class Material
    {
        [Key]
        public string Codigo { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public string UN {  get; set; } = string.Empty;
        public int? EmbalagemMin { get; set; }
        public decimal? MediaVendas { get;set; }
        public decimal? CustoUnitario { get; set; }
        public string Curva { get; set; } = string.Empty;
        public bool ItemCritico { get; set; }
        public string ObsItemCritico { get; set; } = string.Empty;
        public string CriadoPor { get; set; } = string.Empty;
        public DateTime CriadoEm { get; set; }
        public string ModificadoPor { get; set; } = string.Empty;
        public DateTime? ModificadoEm { get; set; }
        public int FilialId { get; set; }
    }
}
