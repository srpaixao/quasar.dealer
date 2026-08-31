using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using Simplify.Quasar.Models;

namespace Simplify.Quasar.Areas.AnomaliaApp.Services
{
    public class AnomaliaReenvioService
    {
        private readonly Quasar_Entities db;
        private readonly int filialId;
        private readonly string usuario;
        private readonly DateTime agora;
        private readonly IAnomaliaExcelService excelService;

        public AnomaliaReenvioService(
            Quasar_Entities db,
            int filialId,
            string usuario,
            DateTime agora,
            IAnomaliaExcelService excelService)
        {
            this.db = db ?? throw new ArgumentNullException("db");
            this.filialId = filialId;
            this.usuario = string.IsNullOrWhiteSpace(usuario) ? "SISTEMA" : usuario.Trim();
            this.agora = agora;
            this.excelService = excelService ?? throw new ArgumentNullException("excelService");
        }

        public IList<int> Gerar(AnomaliaReenvioRequest request)
        {
            if (request == null) throw new ArgumentNullException("request");
            var ids = (request.AnomaliaItemIds ?? new List<int>()).Distinct().OrderBy(x => x).ToList();
            if (ids.Count == 0) throw new InvalidOperationException("Selecione pelo menos um item rejeitado.");

            using (var transaction = db.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                try
                {
                    ProcessoRow processo = BloquearProcesso(request.AnomaliaId);
                    var itens = new List<ItemReenvioRow>();
                    foreach (int id in ids)
                    {
                        ItemReenvioRow item = BloquearItemRejeitado(request.AnomaliaId, id);
                        itens.Add(item);
                    }

                    // O lote referencia os mesmos AnomaliaGmItem originais. Nenhuma linha consumidora
                    // de saldo é criada durante o reenvio.
                    var lotes = excelService.PrepararLotes(
                        itens.Select(x => new AnomaliaArquivoItemEntrada
                        {
                            AnomaliaItemId = x.Id,
                            TipoCodigo = x.TipoCodigo
                        }),
                        true);
                    var arquivos = new List<int>();

                    foreach (var grupo in lotes.GroupBy(x => x.TipoCodigo))
                    {
                        int proximaSequencia = ObterProximaSequencia(request.AnomaliaId, grupo.Key, true);
                        foreach (AnomaliaArquivoLote lote in grupo.OrderBy(x => x.Sequencia))
                        {
                            int sequencia = proximaSequencia++;
                            string nomeArquivo = string.Format(
                                CultureInfo.InvariantCulture,
                                "{0}-{1}-R{2:D2}",
                                processo.NumeroControle,
                                grupo.Key,
                                sequencia);
                            int arquivoOrigemId = ObterArquivoOrigemComum(lote.ItemIds);
                            int arquivoId = InserirArquivo(
                                request.AnomaliaId,
                                grupo.Key,
                                sequencia,
                                nomeArquivo,
                                lote.ItemIds.Count,
                                arquivoOrigemId > 0 ? (int?)arquivoOrigemId : null);

                            foreach (int itemId in lote.ItemIds)
                            {
                                InserirArquivoItem(arquivoId, itemId);
                                InserirHistorico(
                                    request.AnomaliaId,
                                    itemId,
                                    "Reenvio gerado no arquivo " + nomeArquivo + ". O saldo original foi preservado.");
                            }

                            arquivos.Add(arquivoId);
                        }
                    }

                    transaction.Commit();
                    return arquivos;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        private ProcessoRow BloquearProcesso(int anomaliaId)
        {
            const string sql = @"
SELECT Id, NumeroControle
FROM AnomaliaGmProcesso WITH (UPDLOCK, HOLDLOCK, ROWLOCK)
WHERE Id = @anomaliaId AND FilialId = @filialId AND Ativo = 1 AND Cancelado = 0";
            var processo = db.Database.SqlQuery<ProcessoRow>(
                sql,
                new SqlParameter("@anomaliaId", anomaliaId),
                new SqlParameter("@filialId", filialId)).FirstOrDefault();
            if (processo == null) throw new InvalidOperationException("Processo não localizado para a filial atual.");
            return processo;
        }

        private ItemReenvioRow BloquearItemRejeitado(int anomaliaId, int itemId)
        {
            const string sql = @"
SELECT ai.Id, tipo.Codigo AS TipoCodigo
FROM AnomaliaGmItem ai WITH (UPDLOCK, HOLDLOCK, ROWLOCK)
INNER JOIN AnomaliaGmTipo tipo ON tipo.Id = ai.AnomaliaTipoId
WHERE ai.Id = @itemId
  AND ai.AnomaliaId = @anomaliaId
  AND ai.FilialId = @filialId
  AND ai.StatusId = @rejeitado
  AND ai.Cancelado = 0";
            var item = db.Database.SqlQuery<ItemReenvioRow>(
                sql,
                new SqlParameter("@itemId", itemId),
                new SqlParameter("@anomaliaId", anomaliaId),
                new SqlParameter("@filialId", filialId),
                new SqlParameter("@rejeitado", AnomaliaGmStatusIds.Rejeitado)).FirstOrDefault();
            if (item == null)
                throw new InvalidOperationException("O reenvio só pode conter itens rejeitados do processo e da filial atuais.");
            return item;
        }

        private int ObterProximaSequencia(int anomaliaId, string tipo, bool reenvio)
        {
            return db.Database.SqlQuery<int>(
                @"SELECT ISNULL(MAX(NumeroSequencia), 0) + 1
                    FROM AnomaliaGmArquivo WITH (UPDLOCK, HOLDLOCK)
                   WHERE AnomaliaId = @anomaliaId
                     AND TipoAnomalia = @tipo
                     AND Reenvio = @reenvio",
                new SqlParameter("@anomaliaId", anomaliaId),
                new SqlParameter("@tipo", tipo),
                new SqlParameter("@reenvio", reenvio)).First();
        }

        private int ObterArquivoOrigemComum(IEnumerable<int> itemIds)
        {
            var origens = new HashSet<int>();
            foreach (int itemId in itemIds)
            {
                int origem = db.Database.SqlQuery<int>(
                    @"SELECT TOP 1 arq.Id
                        FROM AnomaliaGmArquivoItem rel
                        INNER JOIN AnomaliaGmArquivo arq ON arq.Id = rel.AnomaliaArquivoId
                       WHERE rel.AnomaliaItemId = @itemId AND arq.Reenvio = 0
                       ORDER BY arq.DataGeracao, arq.Id",
                    new SqlParameter("@itemId", itemId)).FirstOrDefault();
                if (origem > 0) origens.Add(origem);
            }
            return origens.Count == 1 ? origens.First() : 0;
        }

        private int InserirArquivo(
            int anomaliaId,
            string tipo,
            int sequencia,
            string nomeArquivo,
            int quantidadeItens,
            int? arquivoOrigemId)
        {
            return db.Database.SqlQuery<int>(
                @"INSERT INTO AnomaliaGmArquivo
                    (AnomaliaId, TipoAnomalia, NumeroSequencia, NomeArquivo, QuantidadeItens,
                     DataGeracao, UsuarioGeracaoLogin, Reenvio, ArquivoOrigemId, FilialId, CriadoEm)
                  VALUES
                    (@anomaliaId, @tipo, @sequencia, @nomeArquivo, @quantidadeItens,
                     @agora, @usuario, 1, @origemId, @filialId, @agora);
                  SELECT CAST(SCOPE_IDENTITY() AS int);",
                new SqlParameter("@anomaliaId", anomaliaId),
                new SqlParameter("@tipo", tipo),
                new SqlParameter("@sequencia", sequencia),
                new SqlParameter("@nomeArquivo", nomeArquivo),
                new SqlParameter("@quantidadeItens", quantidadeItens),
                new SqlParameter("@agora", agora),
                new SqlParameter("@usuario", usuario),
                AnomaliaService.SqlNullable("@origemId", arquivoOrigemId, SqlDbType.Int),
                new SqlParameter("@filialId", filialId)).First();
        }

        private void InserirArquivoItem(int arquivoId, int itemId)
        {
            db.Database.ExecuteSqlCommand(
                @"INSERT INTO AnomaliaGmArquivoItem (AnomaliaArquivoId, AnomaliaItemId)
                  VALUES (@arquivoId, @itemId)",
                new SqlParameter("@arquivoId", arquivoId),
                new SqlParameter("@itemId", itemId));
        }

        private void InserirHistorico(int anomaliaId, int itemId, string observacao)
        {
            db.Database.ExecuteSqlCommand(
                @"INSERT INTO AnomaliaGmHistorico
                    (AnomaliaId, AnomaliaItemId, Evento, StatusAnteriorId, StatusNovoId,
                     UsuarioLogin, DataHora, Observacao, FilialId)
                  VALUES
                    (@anomaliaId, @itemId, @evento, @rejeitado, @rejeitado,
                     @usuario, @agora, @observacao, @filialId)",
                new SqlParameter("@anomaliaId", anomaliaId),
                new SqlParameter("@itemId", itemId),
                new SqlParameter("@evento", AnomaliaGmEventos.ReenvioGerado),
                new SqlParameter("@rejeitado", AnomaliaGmStatusIds.Rejeitado),
                new SqlParameter("@usuario", usuario),
                new SqlParameter("@agora", agora),
                new SqlParameter("@observacao", observacao),
                new SqlParameter("@filialId", filialId));
        }

        private class ProcessoRow
        {
            public int Id { get; set; }
            public string NumeroControle { get; set; }
        }

        private class ItemReenvioRow
        {
            public int Id { get; set; }
            public string TipoCodigo { get; set; }
        }
    }
}
