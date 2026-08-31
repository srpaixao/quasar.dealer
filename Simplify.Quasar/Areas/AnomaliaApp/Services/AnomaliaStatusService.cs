using System;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using Simplify.Quasar.Models;

namespace Simplify.Quasar.Areas.AnomaliaApp.Services
{
    public class AnomaliaStatusService
    {
        private readonly Quasar_Entities db;
        private readonly int filialId;
        private readonly string usuario;
        private readonly DateTime agora;

        public AnomaliaStatusService(Quasar_Entities db, int filialId, string usuario, DateTime agora)
        {
            this.db = db ?? throw new ArgumentNullException("db");
            this.filialId = filialId;
            this.usuario = usuario;
            this.agora = agora;
        }

        public void AlterarStatusItem(int anomaliaId, int itemId, int novoStatusId, string observacao)
        {
            if (novoStatusId != AnomaliaGmStatusIds.Aceito && novoStatusId != AnomaliaGmStatusIds.Rejeitado)
                throw new InvalidOperationException("Status de destino inválido.");

            using (var transaction = db.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                try
                {
                    const string selectSql = @"
SELECT i.Id, i.StatusId
FROM AnomaliaGmItem i WITH (UPDLOCK, HOLDLOCK, ROWLOCK)
INNER JOIN AnomaliaGmProcesso p WITH (UPDLOCK, HOLDLOCK, ROWLOCK) ON p.Id = i.AnomaliaId
WHERE i.Id = @itemId AND i.AnomaliaId = @anomaliaId
  AND i.FilialId = @filialId AND p.FilialId = @filialId
  AND i.Cancelado = 0 AND p.Cancelado = 0";
                    var item = db.Database.SqlQuery<StatusItemRow>(
                        selectSql,
                        new SqlParameter("@itemId", itemId),
                        new SqlParameter("@anomaliaId", anomaliaId),
                        new SqlParameter("@filialId", filialId)).FirstOrDefault();
                    if (item == null) throw new InvalidOperationException("Item não localizado para a filial atual.");
                    if (item.StatusId != AnomaliaGmStatusIds.EmProcesso)
                        throw new InvalidOperationException("Somente itens em processo podem ser aceitos ou rejeitados.");

                    db.Database.ExecuteSqlCommand(
                        @"UPDATE AnomaliaGmItem
                             SET StatusId = @novo, ModificadoEm = @agora, ModificadoPor = @usuario
                           WHERE Id = @itemId AND AnomaliaId = @anomaliaId AND FilialId = @filialId",
                        new SqlParameter("@novo", novoStatusId),
                        new SqlParameter("@agora", agora),
                        new SqlParameter("@usuario", usuario),
                        new SqlParameter("@itemId", itemId),
                        new SqlParameter("@anomaliaId", anomaliaId),
                        new SqlParameter("@filialId", filialId));

                    db.Database.ExecuteSqlCommand(
                        @"INSERT INTO AnomaliaGmHistorico
                            (AnomaliaId, AnomaliaItemId, Evento, StatusAnteriorId, StatusNovoId,
                             UsuarioLogin, DataHora, Observacao, FilialId)
                          VALUES (@anomaliaId, @itemId, @evento, @anterior, @novo,
                                  @usuario, @agora, @observacao, @filialId)",
                        new SqlParameter("@anomaliaId", anomaliaId),
                        new SqlParameter("@itemId", itemId),
                        new SqlParameter("@evento", AnomaliaGmEventos.StatusAlterado),
                        new SqlParameter("@anterior", item.StatusId),
                        new SqlParameter("@novo", novoStatusId),
                        new SqlParameter("@usuario", usuario),
                        new SqlParameter("@agora", agora),
                        AnomaliaService.SqlNullable("@observacao", observacao, SqlDbType.VarChar, 1000),
                        new SqlParameter("@filialId", filialId));

                    int pendentes = db.Database.SqlQuery<int>(
                        @"SELECT COUNT(1) FROM AnomaliaGmItem WITH (HOLDLOCK)
                           WHERE AnomaliaId = @anomaliaId AND FilialId = @filialId
                             AND Cancelado = 0 AND StatusId = @emProcesso",
                        new SqlParameter("@anomaliaId", anomaliaId),
                        new SqlParameter("@filialId", filialId),
                        new SqlParameter("@emProcesso", AnomaliaGmStatusIds.EmProcesso)).First();
                    int statusProcesso = pendentes > 0
                        ? AnomaliaGmStatusIds.EmProcesso
                        : AnomaliaGmStatusIds.Finalizado;
                    db.Database.ExecuteSqlCommand(
                        @"UPDATE AnomaliaGmProcesso
                             SET StatusId = @status, ModificadoEm = @agora, ModificadoPor = @usuario
                           WHERE Id = @anomaliaId AND FilialId = @filialId",
                        new SqlParameter("@status", statusProcesso),
                        new SqlParameter("@agora", agora),
                        new SqlParameter("@usuario", usuario),
                        new SqlParameter("@anomaliaId", anomaliaId),
                        new SqlParameter("@filialId", filialId));

                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        private class StatusItemRow
        {
            public int Id { get; set; }
            public int StatusId { get; set; }
        }
    }
}
