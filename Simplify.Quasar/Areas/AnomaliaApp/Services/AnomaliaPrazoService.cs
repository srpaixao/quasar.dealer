using System;

namespace Simplify.Quasar.Areas.AnomaliaApp.Services
{
    public class AnomaliaPrazoService
    {
        public DateTime CalcularDataLimite(DateTime dataEmissao, int prazoDias)
        {
            if (prazoDias <= 0)
            {
                throw new ArgumentOutOfRangeException("prazoDias", "O prazo deve ser maior que zero.");
            }

            return dataEmissao.Date.AddDays(prazoDias);
        }

        public bool EstaDentroDoPrazo(DateTime dataEmissao, int prazoDias, DateTime dataReferencia)
        {
            if (prazoDias <= 0)
            {
                throw new ArgumentOutOfRangeException("prazoDias", "O prazo deve ser maior que zero.");
            }

            return CalcularDiasDecorridos(dataEmissao, dataReferencia) <= prazoDias;
        }

        public int CalcularDiasDecorridos(DateTime dataEmissao, DateTime dataReferencia)
        {
            return (dataReferencia.Date - dataEmissao.Date).Days;
        }

        public void Validar(DateTime dataEmissao, int prazoDias, DateTime dataReferencia)
        {
            if (!EstaDentroDoPrazo(dataEmissao, prazoDias, dataReferencia))
            {
                int diasDecorridos = CalcularDiasDecorridos(dataEmissao, dataReferencia);
                throw new InvalidOperationException(
                    string.Format(
                        "O prazo para abertura da reclamação foi encerrado. Dias decorridos: {0}; prazo permitido: {1}.",
                        diasDecorridos,
                        prazoDias));
            }
        }
    }
}
