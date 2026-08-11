using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Mvc;
using Simplify.Quasar.Custom;

namespace Simplify.Quasar.Areas.EstoqueApp.ViewModels
{
    public class LocacaoLoteViewModel
    {
        public LocacaoLoteViewModel()
        {
            Linhas = new List<LocacaoLoteLinhaViewModel>();
        }

        public HttpPostedFileBase Arquivo { get; set; }
        public string Token { get; set; }
        public string NomeArquivo { get; set; }
        public List<LocacaoLoteLinhaViewModel> Linhas { get; set; }
        public int LinhasImportadas { get; set; }
        public int LinhasValidas { get; set; }
        public int LinhasComErro { get; set; }
        public int LocacoesPrevistas { get; set; }
        public int LocacoesJaExistentes { get; set; }
        public int LocacoesDuplicadasArquivo { get; set; }
        public int NovasLocacoes { get; set; }
        public string ErroGeral { get; set; }
        public LocacaoLoteResultadoViewModel Resultado { get; set; }

        public bool PossuiPreview
        {
            get { return LinhasImportadas > 0 || !string.IsNullOrWhiteSpace(ErroGeral); }
        }

        public bool PodeConfirmar
        {
            get
            {
                return LinhasImportadas > 0 &&
                    LinhasComErro == 0 &&
                    string.IsNullOrWhiteSpace(ErroGeral) &&
                    NovasLocacoes > 0;
            }
        }
    }

    public class LocacaoLoteLinhaViewModel
    {
        public LocacaoLoteLinhaViewModel()
        {
            Erros = new List<string>();
        }

        public int Linha { get; set; }
        public string Descricao { get; set; }
        public string Area { get; set; }
        public string Zona { get; set; }
        public string Corredor { get; set; }
        public string EstanteInicio { get; set; }
        public string EstanteFinal { get; set; }
        public string NivelInicio { get; set; }
        public string NivelFinal { get; set; }
        public string CompartimentoInicio { get; set; }
        public string CompartimentoFinal { get; set; }
        public string Lado { get; set; }
        public string Demanda { get; set; }
        public string Equipamento { get; set; }
        public int QuantidadePrevista { get; set; }
        public int QuantidadeExistente { get; set; }
        public int QuantidadeDuplicadaArquivo { get; set; }
        public int QuantidadeNova { get; set; }
        public List<string> Erros { get; set; }

        public bool Valida
        {
            get { return Erros.Count == 0; }
        }

        public string Status
        {
            get
            {
                if (!Valida)
                {
                    return string.Join("; ", Erros);
                }

                if (QuantidadeDuplicadaArquivo > 0)
                {
                    return string.Format("OK — {0:N0} duplicada(s) no arquivo", QuantidadeDuplicadaArquivo);
                }

                return "OK";
            }
        }
    }

    public class LocacaoLoteResultadoViewModel
    {
        public int Processadas { get; set; }
        public int Criadas { get; set; }
        public int JaExistentes { get; set; }
        public int DuplicadasArquivo { get; set; }
        public int Erros { get; set; }
        public string TokenEtiquetas { get; set; }
    }

    public class LocacaoEtiquetaLoteViewModel
    {
        public IEnumerable<SelectListItem> Areas { get; set; }
        public IEnumerable<SelectListItem> Zonas { get; set; }
        public IEnumerable<SelectListItem> Equipamentos { get; set; }
        public IEnumerable<SelectListItem> Demandas { get; set; }
        public string Erro { get; set; }
    }

    public class LocacaoEtiquetaConsultaRequest : DataTableAjaxPostModel
    {
        public string codigo { get; set; }
        public string descricao { get; set; }
        public int? equipamentoId { get; set; }
        public string demanda { get; set; }
    }

    public class LocacaoEtiquetaFiltroViewModel
    {
        public string Codigo { get; set; }
        public string Descricao { get; set; }
        public int? AreaId { get; set; }
        public int? ZonaId { get; set; }
        public int? EquipamentoId { get; set; }
        public string Demanda { get; set; }
        public string Pesquisa { get; set; }
    }

    public class LocacaoEtiquetaGridItemViewModel
    {
        public int Id { get; set; }
        public string Codigo { get; set; }
        public string Descricao { get; set; }
        public string Area { get; set; }
        public string Zona { get; set; }
        public string Equipamento { get; set; }
        public string Demanda { get; set; }
    }

    public class LocacaoEtiquetaImpressaoViewModel
    {
        public LocacaoEtiquetaImpressaoViewModel()
        {
            Etiquetas = new List<LocacaoEtiquetaItemViewModel>();
        }

        public List<LocacaoEtiquetaItemViewModel> Etiquetas { get; set; }
    }

    public class LocacaoEtiquetaItemViewModel
    {
        public string Codigo { get; set; }
        public string CodigoSemEspacos { get; set; }
        public string CodigoFormatado { get; set; }
        public string Descricao { get; set; }
        public string Area { get; set; }
        public string Zona { get; set; }
        public string Equipamento { get; set; }
        public string Demanda { get; set; }
    }

    [Serializable]
    public class LocacaoLoteItem
    {
        public int Linha { get; set; }
        public string Codigo { get; set; }
        public string Descricao { get; set; }
        public int AreaId { get; set; }
        public int ZonaId { get; set; }
        public int? EquipamentoId { get; set; }
        public string Demanda { get; set; }
        public bool JaExiste { get; set; }
    }

    [Serializable]
    public class LocacaoLoteSessao
    {
        public LocacaoLoteSessao()
        {
            Itens = new List<LocacaoLoteItem>();
        }

        public string Token { get; set; }
        public string NomeArquivo { get; set; }
        public DateTime CriadoEm { get; set; }
        public int FilialId { get; set; }
        public LocacaoLoteViewModel Preview { get; set; }
        public List<LocacaoLoteItem> Itens { get; set; }
    }

    [Serializable]
    public class LocacaoEtiquetaLoteSessao
    {
        public LocacaoEtiquetaLoteSessao()
        {
            Ids = new List<int>();
        }

        public string Token { get; set; }
        public int FilialId { get; set; }
        public DateTime CriadoEm { get; set; }
        public List<int> Ids { get; set; }
    }
}
