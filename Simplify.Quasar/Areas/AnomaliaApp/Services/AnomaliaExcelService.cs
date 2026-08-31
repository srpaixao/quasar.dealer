using System;
using System.Collections.Generic;
using System.Linq;

namespace Simplify.Quasar.Areas.AnomaliaApp.Services
{
    public interface IAnomaliaExcelService
    {
        IList<AnomaliaArquivoLote> PrepararLotes(IEnumerable<AnomaliaArquivoItemEntrada> itens, bool reenvio);
    }

    public class AnomaliaArquivoItemEntrada
    {
        public int AnomaliaItemId { get; set; }
        public string TipoCodigo { get; set; }
    }

    public class AnomaliaExcelService : IAnomaliaExcelService
    {
        public IList<AnomaliaArquivoLote> PrepararLotes(IEnumerable<AnomaliaArquivoItemEntrada> itens, bool reenvio)
        {
            var entradas = (itens ?? Enumerable.Empty<AnomaliaArquivoItemEntrada>())
                .Where(x => x != null)
                .ToList();
            var lotes = new List<AnomaliaArquivoLote>();

            foreach (var grupo in entradas.GroupBy(x => (x.TipoCodigo ?? string.Empty).Trim().ToUpperInvariant()))
            {
                int limite = ObterLimite(grupo.Key);
                var ids = grupo.Select(x => x.AnomaliaItemId).Distinct().OrderBy(x => x).ToList();
                int sequencia = 1;

                for (int inicio = 0; inicio < ids.Count; inicio += limite)
                {
                    lotes.Add(new AnomaliaArquivoLote
                    {
                        TipoCodigo = grupo.Key,
                        Sequencia = sequencia++,
                        Reenvio = reenvio,
                        ItemIds = ids.Skip(inicio).Take(limite).ToList()
                    });
                }
            }

            return lotes;
        }

        private static int ObterLimite(string tipoCodigo)
        {
            if (tipoCodigo == "G") return 10;
            if (tipoCodigo == "A" || tipoCodigo == "B" || tipoCodigo == "C") return 5;
            throw new InvalidOperationException("Tipo não suportado para geração de arquivo nesta fase.");
        }
    }
}
