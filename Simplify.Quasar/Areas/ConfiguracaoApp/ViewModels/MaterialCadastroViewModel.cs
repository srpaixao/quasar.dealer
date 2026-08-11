using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace Simplify.Quasar.Areas.ConfiguracaoApp.ViewModels
{
    public class MaterialCadastroViewModel : IValidatableObject
    {
        [Required(ErrorMessage = "Informe o código do item.")]
        [StringLength(100)]
        [Display(Name = "Código do item")]
        public string Codigo { get; set; }

        [Required(ErrorMessage = "Informe a descrição.")]
        [StringLength(500)]
        [Display(Name = "Descrição")]
        public string Descricao { get; set; }

        [StringLength(100)]
        [Display(Name = "Unidade")]
        public string UN { get; set; }

        [Display(Name = "Embalagem mínima")]
        [Range(0, int.MaxValue, ErrorMessage = "Informe uma embalagem mínima válida.")]
        public int? EmbalagemMin { get; set; }

        [Display(Name = "Média de vendas")]
        public decimal? MediaVendas { get; set; }

        [Display(Name = "Custo unitário")]
        public decimal? CustoUnitario { get; set; }

        [Display(Name = "Comprimento")]
        public decimal? Comp { get; set; }

        [Display(Name = "Largura")]
        public decimal? Larg { get; set; }

        [Display(Name = "Altura")]
        public decimal? Altu { get; set; }

        [StringLength(100)]
        public string Curva { get; set; }

        [Display(Name = "Item crítico")]
        public bool ItemCritico { get; set; }

        [StringLength(100)]
        [Display(Name = "Observação do item crítico")]
        public string ObsItemCritico { get; set; }

        [Required(ErrorMessage = "Selecione a categoria do produto.")]
        [StringLength(20)]
        [Display(Name = "Categoria do produto")]
        public string CategoriaProduto { get; set; }

        [StringLength(100)]
        [Display(Name = "Item Apollo")]
        public string ItemApollo { get; set; }

        [Required(ErrorMessage = "Selecione a Zona da Unidade.")]
        [Display(Name = "Zona da Unidade")]
        public int? Zona1Id { get; set; }

        [Required(ErrorMessage = "Selecione o Equipamento da Unidade.")]
        [Display(Name = "Equipamento da Unidade")]
        public int? Eqpto1Id { get; set; }

        [Required(ErrorMessage = "Informe a Quantidade por Unidade.")]
        [Display(Name = "Quantidade por Unidade")]
        [Range(1, int.MaxValue, ErrorMessage = "A Quantidade por Unidade deve ser maior que zero.")]
        public int? QtdePadrao1 { get; set; }

        [Display(Name = "Zona da Caixa")]
        public int? Zona2Id { get; set; }

        [Display(Name = "Equipamento da Caixa")]
        public int? Eqpto2Id { get; set; }

        [Display(Name = "Quantidade por Caixa")]
        [Range(1, int.MaxValue, ErrorMessage = "A Quantidade por Caixa deve ser maior que zero.")]
        public int? QtdePadrao2 { get; set; }

        [Display(Name = "Zona do Palete")]
        public int? Zona3Id { get; set; }

        [Display(Name = "Equipamento do Palete")]
        public int? Eqpto3Id { get; set; }

        [Display(Name = "Quantidade por Palete")]
        [Range(1, int.MaxValue, ErrorMessage = "A Quantidade por Palete deve ser maior que zero.")]
        public int? QtdePadrao3 { get; set; }

        public IEnumerable<SelectListItem> Zonas1 { get; set; }
        public IEnumerable<SelectListItem> Zonas2 { get; set; }
        public IEnumerable<SelectListItem> Zonas3 { get; set; }
        public IEnumerable<SelectListItem> Equipamentos1 { get; set; }
        public IEnumerable<SelectListItem> Equipamentos2 { get; set; }
        public IEnumerable<SelectListItem> Equipamentos3 { get; set; }
        public string Zona1Nome { get; set; }
        public string Eqpto1Nome { get; set; }
        public string Zona2Nome { get; set; }
        public string Eqpto2Nome { get; set; }
        public string Zona3Nome { get; set; }
        public string Eqpto3Nome { get; set; }

        public string CriadoPor { get; set; }
        public DateTime? CriadoEm { get; set; }
        public string ModificadoPor { get; set; }
        public DateTime? ModificadoEm { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            const decimal maximumValue = 9999999999999.99m;

            if (ItemCritico && string.IsNullOrWhiteSpace(ObsItemCritico))
            {
                yield return new ValidationResult(
                    "Informe a observação do item crítico.",
                    new[] { "ObsItemCritico" });
            }

            if (MediaVendas.HasValue &&
                (MediaVendas.Value < 0 || MediaVendas.Value > maximumValue))
            {
                yield return new ValidationResult(
                    "Informe uma média de vendas entre 0 e 9.999.999.999.999,99.",
                    new[] { "MediaVendas" });
            }

            if (CustoUnitario.HasValue &&
                (CustoUnitario.Value < 0 || CustoUnitario.Value > maximumValue))
            {
                yield return new ValidationResult(
                    "Informe um custo unitário entre 0 e 9.999.999.999.999,99.",
                    new[] { "CustoUnitario" });
            }

            foreach (var validation in ValidateDimension(Comp, "Comp", "comprimento", maximumValue))
            {
                yield return validation;
            }

            foreach (var validation in ValidateDimension(Larg, "Larg", "largura", maximumValue))
            {
                yield return validation;
            }

            foreach (var validation in ValidateDimension(Altu, "Altu", "altura", maximumValue))
            {
                yield return validation;
            }

            if (QtdePadrao1.HasValue &&
                QtdePadrao2.HasValue &&
                QtdePadrao2.Value <= QtdePadrao1.Value)
            {
                yield return new ValidationResult(
                    "A Quantidade por Caixa deve ser maior que a Quantidade por Unidade.",
                    new[] { "QtdePadrao2" });
            }

            if (QtdePadrao3.HasValue && !QtdePadrao2.HasValue)
            {
                yield return new ValidationResult(
                    "Configure a Quantidade por Caixa antes da Quantidade por Palete.",
                    new[] { "QtdePadrao3" });
            }
            else if (QtdePadrao2.HasValue &&
                     QtdePadrao3.HasValue &&
                     QtdePadrao3.Value <= QtdePadrao2.Value)
            {
                yield return new ValidationResult(
                    "A Quantidade por Palete deve ser maior que a Quantidade por Caixa.",
                    new[] { "QtdePadrao3" });
            }
        }

        private static IEnumerable<ValidationResult> ValidateDimension(
            decimal? value,
            string field,
            string description,
            decimal maximumValue)
        {
            if (value.HasValue && (value.Value < 0 || value.Value > maximumValue))
            {
                yield return new ValidationResult(
                    "Informe um valor válido para " + description + ".",
                    new[] { field });
            }
        }
    }

    public class MaterialConsultaViewModel
    {
        public string Codigo { get; set; }
        public string Descricao { get; set; }
        public string UN { get; set; }
        public string CategoriaProduto { get; set; }
        public string ItemApollo { get; set; }
    }
}
