using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace Simplify.Quasar.Areas.ConfiguracaoApp.ViewModels
{
    public class EquipamentoCadastroViewModel : IValidatableObject
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Informe o nome.")]
        [StringLength(100)]
        public string Nome { get; set; }

        [StringLength(100)]
        public string Tipo { get; set; }

        [StringLength(100)]
        [Display(Name = "Descrição")]
        public string Descricao { get; set; }

        [Display(Name = "Bloqueado")]
        public bool Bloqueado { get; set; }

        [StringLength(500)]
        [Display(Name = "Observações")]
        public string Observacoes { get; set; }

        [Display(Name = "Comprimento")]
        public decimal? Comp { get; set; }

        [Display(Name = "Largura")]
        public decimal? Larg { get; set; }

        [Display(Name = "Altura")]
        public decimal? Altu { get; set; }

        [Display(Name = "Quantidade")]
        [Range(0, int.MaxValue, ErrorMessage = "Informe uma quantidade válida.")]
        public int? Qtde { get; set; }

        [Required(ErrorMessage = "Selecione ao menos uma zona.")]
        [Display(Name = "Zonas")]
        public string[] ZonasSelecionadas { get; set; }

        public string Zonas { get; set; }
        public IEnumerable<SelectListItem> ZonasDDL { get; set; }
        public string CriadoPor { get; set; }
        public DateTime? CriadoEm { get; set; }
        public string ModificadoPor { get; set; }
        public DateTime? ModificadoEm { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            const decimal maximumDimension = 9999999999999999.99m;

            foreach (var validation in ValidateDimension(Comp, "Comp", "comprimento", maximumDimension))
            {
                yield return validation;
            }

            foreach (var validation in ValidateDimension(Larg, "Larg", "largura", maximumDimension))
            {
                yield return validation;
            }

            foreach (var validation in ValidateDimension(Altu, "Altu", "altura", maximumDimension))
            {
                yield return validation;
            }
        }

        private static IEnumerable<ValidationResult> ValidateDimension(
            decimal? value,
            string member,
            string label,
            decimal maximum)
        {
            if (value.HasValue && (value.Value < 0 || value.Value > maximum))
            {
                yield return new ValidationResult(
                    "Informe um valor válido para " + label + ".",
                    new[] { member });
            }
        }
    }
}
