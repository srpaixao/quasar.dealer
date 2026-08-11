using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Mvc;
using Simplify.Quasar.Models;

namespace Simplify.Quasar.Areas.SeparacaoApp.ViewModels
{
    public class RomaneioViewModel
    {
        public int? RomaneioId { get; set; }
        public string RomaneioNr { get; set; }
        public int? PickerId { get; set; }
        public int? StatusId { get; set; }
        public int RomaneioInicio { get; set; }
        public int RomaneioFinal { get; set; }
        public DateTime DataInicial { get; set; }
        public DateTime DataFinal { get; set; }
        public string DiretorioRomaneios { get; set; }
        public bool CanViewFullMenu { get; set; }
        public bool CanConferir { get; set; }
        public bool CanFinalizarConferencia { get; set; }
        public bool CanAdministrar { get; set; }
        public bool CanFinalizarNaoGeradosExportados { get; set; }
        public bool CanDownloadNaoGeradosExportados { get; set; }
        public bool TriggerDownloadNaoGerados { get; set; }
        public bool PromptGerarMapa { get; set; }
        public bool ProdutividadeConfigValida { get; set; }
        public string ProdutividadeConfigMensagem { get; set; }
        public int TotalNaoGerado { get; set; }
        public int TotalAguardandoSeparacao { get; set; }
        public int TotalEmSeparacao { get; set; }
        public int TotalFinalizado { get; set; }
        public int TotalNaoSeparar { get; set; }
        public IEnumerable<SelectListItem> RomaneioDDL { get; set; }
        public IEnumerable<SelectListItem> PickerDDL { get; set; }
        public IEnumerable<SelectListItem> StatusDDL { get; set; }
        public IEnumerable<RomaneioGridItemViewModel> GridItems { get; set; }
        public IEnumerable<RomaneioGridItemViewModel> AnaliseItems { get; set; }
        public IEnumerable<RomaneioDashboardItemViewModel> Produtividade { get; set; }
        public IEnumerable<RomaneioDashboardItemViewModel> ProdutividadeConferencia { get; set; }
        public RomaneioImportSummaryViewModel ImportSummary { get; set; }
        public bool ConsultaRealizada { get; set; }
        public string ConsultaMensagem { get; set; }
        public Romaneio ConsultaHeader { get; set; }
        public IEnumerable<RomaneioItem> ConsultaItens { get; set; }
        public RomaneioConsultaHeaderViewModel ConsultaDetalhe { get; set; }
        public IEnumerable<RomaneioConsultaItemViewModel> ConsultaItensDetalhe { get; set; }
        public TarefaConsultaFiltroViewModel TarefaFiltro { get; set; }
        public IEnumerable<TarefaConsultaItemViewModel> TarefaItens { get; set; }
        public IEnumerable<RomaneioPendenciaItemViewModel> PendenciaItens { get; set; }
        public AlocacaoZonaResumoViewModel AlocacaoResumo { get; set; }
    }

    public class RomaneioGridItemViewModel
    {
        public int Id { get; set; }
        public string RomaneioNr { get; set; }
        public bool PossuiVendedor { get; set; }
        public string Area { get; set; }
        public int? Prioridade { get; set; }
        public string UsuarioApollo { get; set; }
        public int? ContatoNr { get; set; }
        public int? Itens { get; set; }
        public int? Pecas { get; set; }
        public int? PickerId { get; set; }
        public string PickerNome { get; set; }
        public DateTime? DataPicker { get; set; }
        public int? ConferenteId { get; set; }
        public string ConferenteNome { get; set; }
        public DateTime? DataConferencia { get; set; }
        public string Status { get; set; }
        public int? StatusId { get; set; }
        public DateTime? DataRomaneio { get; set; }
        public string CriadoPor { get; set; }
        public DateTime? CriadoEm { get; set; }
        public string ModificadoPor { get; set; }
        public DateTime? ModificadoEm { get; set; }
    }

    public class RomaneioDashboardItemViewModel
    {
        public int PickerId { get; set; }
        public string PickerNome { get; set; }
        public int QuantidadeRomaneios { get; set; }
        public int QuantidadeLinhas { get; set; }
        public int QuantidadePecas { get; set; }
        public decimal ProdutividadeCalculada { get; set; }
    }

    public class RomaneioImportSummaryViewModel
    {
        public int Processados { get; set; }
        public int Atualizados { get; set; }
        public int Criados { get; set; }
        public int StatusAlterados { get; set; }
        public int NaoSeparar { get; set; }
        public int Erros { get; set; }
        public IList<string> Mensagens { get; set; }

        public RomaneioImportSummaryViewModel()
        {
            Mensagens = new List<string>();
        }
    }

    public class RomaneioConsultaHeaderViewModel
    {
        public string RomaneioNr { get; set; }
        public string ContatoNr { get; set; }
        public string Status { get; set; }
        public string Vendedor { get; set; }
        public DateTime? CriadoEm { get; set; }
        public string CriadoPor { get; set; }
        public string Separador { get; set; }
        public DateTime? DataSeparacao { get; set; }
        public string Conferente { get; set; }
        public DateTime? DataConferencia { get; set; }
        public DateTime? ModificadoEm { get; set; }
        public string ModificadoPor { get; set; }
    }

    public class RomaneioConsultaItemViewModel
    {
        public string ItemNr { get; set; }
        public string Descricao { get; set; }
        public int? Qtde { get; set; }
    }

    public class UploadArquivo
    {
        public HttpPostedFileBase Arquivo { get; set; }
    }

    public class TarefaConsultaFiltroViewModel
    {
        public string TarefaNr { get; set; }
        public string RomaneioNr { get; set; }
        public string Contato { get; set; }
        public string OS { get; set; }
        public string ItemNr { get; set; }
        public string Zona { get; set; }
        public DateTime? Data { get; set; }
    }

    public class TarefaConsultaItemViewModel
    {
        public string TarefaNr { get; set; }
        public int? StatusId { get; set; }
        public string Status { get; set; }
        public string Zona { get; set; }
        public string RomaneioNr { get; set; }
        public string Contato { get; set; }
        public string OS { get; set; }
        public string ItemNr { get; set; }
        public string Locacao { get; set; }
        public string Descricao { get; set; }
        public int? Qtde { get; set; }
        public decimal? ValorTotal { get; set; }
        public int? Prioridade { get; set; }
        public int LinhasSumarizadas { get; set; }
        public DateTime? CriadoEm { get; set; }
    }

    public class AlocacaoZonaResumoViewModel
    {
        public int RomaneiosAtualizados { get; set; }
        public int ItensImportados { get; set; }
        public int TarefasGeradas { get; set; }
        public int ItensAlocados { get; set; }
        public int ItensSemZona { get; set; }
        public int ItensSemLocacao { get; set; }
        public IList<string> Mensagens { get; set; }

        public AlocacaoZonaResumoViewModel()
        {
            Mensagens = new List<string>();
        }
    }

    public class RomaneioPendenciaItemViewModel
    {
        public int Id { get; set; }
        public string RomaneioNr { get; set; }
        public string ItemNr { get; set; }
        public string Descricao { get; set; }
        public string Zona { get; set; }
        public string Locacao { get; set; }
        public int? Quantidade { get; set; }
        public int? StatusId { get; set; }
        public string Status { get; set; }
    }

    public class SeparacaoDashboardViewModel
    {
        public DateTime DataInicial { get; set; }
        public DateTime DataFinal { get; set; }
        public bool PeriodoValido { get; set; }
        public string PeriodoMensagem { get; set; }
        public int SeparadoresTrabalhando { get; set; }
        public int TarefasPendentesDia { get; set; }
        public int RomaneiosDia { get; set; }
        public bool ProdutividadeConfigValida { get; set; }
        public string ProdutividadeConfigMensagem { get; set; }
        public IEnumerable<SeparacaoDashboardStatusItemViewModel> StatusDia { get; set; }
        public IEnumerable<SeparacaoDashboardSemanaItemViewModel> MovimentoSemanal { get; set; }
        public IEnumerable<RomaneioDashboardItemViewModel> Produtividade { get; set; }
        public IEnumerable<RomaneioDashboardItemViewModel> ProdutividadeConferencia { get; set; }
    }

    public class SeparacaoDashboardStatusItemViewModel
    {
        public int StatusId { get; set; }
        public string Status { get; set; }
        public int Quantidade { get; set; }
        public string Cor { get; set; }
        public decimal Percentual { get; set; }
    }

    public class SeparacaoDashboardSemanaItemViewModel
    {
        public DateTime Data { get; set; }
        public string DiaSemana { get; set; }
        public int Quantidade { get; set; }
    }
}
