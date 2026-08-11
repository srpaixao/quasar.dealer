using System;

namespace Simplify.Quasar.Custom
{
    public class ProcessDashboardViewModel
    {
        public string Processo { get; set; }
        public string AreaName { get; set; }
        public DateTime DataInicial { get; set; }
        public DateTime DataFinal { get; set; }
        public bool PeriodoValido { get; set; }
        public string PeriodoMensagem { get; set; }

        public static ProcessDashboardViewModel Create(
            string processo,
            string areaName,
            DateTime? dataInicial,
            DateTime? dataFinal)
        {
            DateTime hoje = Util.GetCurrentDateTime().Date;
            DateTime inicio = (dataInicial ?? hoje).Date;
            DateTime fim = (dataFinal ?? hoje).Date;
            int totalDias = (fim - inicio).Days + 1;

            string mensagem = null;
            if (totalDias <= 0)
            {
                mensagem = "A Data Inicial n\u00E3o pode ser maior que a Data Final.";
            }
            else if (totalDias > 15)
            {
                mensagem = "O per\u00EDodo selecionado n\u00E3o pode ser superior a 15 dias.";
            }

            return new ProcessDashboardViewModel
            {
                Processo = processo,
                AreaName = areaName,
                DataInicial = inicio,
                DataFinal = fim,
                PeriodoValido = mensagem == null,
                PeriodoMensagem = mensagem
            };
        }
    }
}
