using System;

namespace Simplify.Quasar.Areas.AnomaliaApp.Services
{
    public class AnomaliaSaldoService
    {
        public AnomaliaSaldoSnapshot Calcular(
            string tipoCodigo,
            decimal quantidadeNF,
            decimal? quantidadeRecebida,
            decimal quantidadeConsumida)
        {
            string tipo = (tipoCodigo ?? string.Empty).Trim().ToUpperInvariant();
            decimal quantidadeBase;

            if (tipo == "B")
            {
                if (!quantidadeRecebida.HasValue)
                {
                    throw new InvalidOperationException("Informe a quantidade efetivamente recebida para a anomalia tipo B.");
                }

                quantidadeBase = Math.Max(0, quantidadeRecebida.Value - quantidadeNF);
                if (quantidadeBase <= 0)
                {
                    throw new InvalidOperationException("A quantidade recebida não possui excesso em relação à quantidade faturada.");
                }
            }
            else if (tipo == "A" || tipo == "C" || tipo == "G")
            {
                quantidadeBase = quantidadeNF;
            }
            else
            {
                throw new InvalidOperationException("Tipo de anomalia não operacional nesta fase.");
            }

            return new AnomaliaSaldoSnapshot
            {
                QuantidadeBase = quantidadeBase,
                QuantidadeConsumida = Math.Max(0, quantidadeConsumida)
            };
        }

        public void ValidarQuantidade(decimal quantidadeReclamada, AnomaliaSaldoSnapshot saldo)
        {
            if (quantidadeReclamada <= 0)
            {
                throw new InvalidOperationException("A quantidade reclamada deve ser maior que zero.");
            }

            if (decimal.Truncate(quantidadeReclamada) != quantidadeReclamada)
            {
                throw new InvalidOperationException("A quantidade reclamada deve ser informada em número inteiro.");
            }

            if (quantidadeReclamada > saldo.SaldoDisponivel)
            {
                throw new InvalidOperationException(
                    string.Format(
                        "Saldo reclamável insuficiente. Disponível: {0:N4}; solicitado: {1:N0}.",
                        saldo.SaldoDisponivel,
                        quantidadeReclamada));
            }
        }
    }
}
