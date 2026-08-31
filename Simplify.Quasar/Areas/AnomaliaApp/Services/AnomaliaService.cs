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
    public class AnomaliaService
    {
        private const string ControleNrConfigName = "ControleNr";
        private readonly Quasar_Entities db;
        private readonly int filialId;
        private readonly string usuario;
        private readonly DateTime agora;
        private readonly AnomaliaPrazoService prazoService;
        private readonly AnomaliaSaldoService saldoService;

        public AnomaliaService(Quasar_Entities db, int filialId, string usuario, DateTime agora)
        {
            this.db = db ?? throw new ArgumentNullException("db");
            this.filialId = filialId;
            this.usuario = string.IsNullOrWhiteSpace(usuario) ? "SISTEMA" : usuario.Trim();
            this.agora = agora;
            prazoService = new AnomaliaPrazoService();
            saldoService = new AnomaliaSaldoService();
        }

        public AnomaliaProcessoCadastroResult Criar(AnomaliaProcessoCadastroRequest request)
        {
            if (request == null) throw new ArgumentNullException("request");
            if (filialId <= 0) throw new InvalidOperationException("Filial não identificada.");

            var itens = (request.Itens ?? new List<AnomaliaItemCadastroRequest>())
                .Where(x => x != null)
                .ToList();
            if (itens.Count == 0) throw new InvalidOperationException("Inclua pelo menos um item na anomalia.");

            using (var transaction = db.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                try
                {
                    // Ordem estável evita deadlocks quando dois processos contêm os mesmos itens em ordens diferentes.
                    var itensFiscais = new Dictionary<int, NotaFiscalItemBloqueado>();
                    foreach (int notaFiscalItemId in itens.Select(x => x.NotaFiscalItemId).Distinct().OrderBy(x => x))
                    {
                        itensFiscais[notaFiscalItemId] = BloquearNotaFiscalItem(notaFiscalItemId);
                    }

                    string numeroControle = ObterEIncrementarNumeroControle();
                    int anomaliaId = InserirProcesso(numeroControle, request);

                    foreach (var item in itens)
                    {
                        NotaFiscalItemBloqueado itemFiscal;
                        if (!itensFiscais.TryGetValue(item.NotaFiscalItemId, out itemFiscal))
                            throw new InvalidOperationException("Item da nota fiscal não localizado.");

                        ValidarVinculos(item, itemFiscal);
                        TipoAnomaliaRow tipo = ObterTipoOperacional(item.TipoCodigo);
                        DateTime dataLimite = prazoService.CalcularDataLimite(itemFiscal.DataEmissao.Value, tipo.PrazoDias);
                        prazoService.Validar(itemFiscal.DataEmissao.Value, tipo.PrazoDias, agora);

                        decimal quantidadeConsumida = ObterQuantidadeConsumida(
                            item.NotaFiscalItemId,
                            tipo.Codigo == "B");
                        AnomaliaSaldoSnapshot saldo = saldoService.Calcular(
                            tipo.Codigo,
                            itemFiscal.QuantidadeNF,
                            item.QuantidadeRecebida,
                            quantidadeConsumida);
                        saldoService.ValidarQuantidade(item.QuantidadeReclamada, saldo);
                        ValidarCamposPorTipo(item, tipo.Codigo);

                        int anomaliaItemId = InserirItem(
                            anomaliaId,
                            tipo,
                            item,
                            itemFiscal,
                            dataLimite);
                        InserirHistorico(
                            anomaliaId,
                            anomaliaItemId,
                            AnomaliaGmEventos.ItemIncluido,
                            null,
                            AnomaliaGmStatusIds.EmProcesso,
                            "Item incluído com consumo de saldo reclamável.");
                    }

                    InserirHistorico(
                        anomaliaId,
                        null,
                        AnomaliaGmEventos.ProcessoCriado,
                        null,
                        AnomaliaGmStatusIds.EmProcesso,
                        "Processo criado com validação transacional de saldo.");

                    transaction.Commit();
                    return new AnomaliaProcessoCadastroResult
                    {
                        AnomaliaId = anomaliaId,
                        NumeroControle = numeroControle,
                        QuantidadeItens = itens.Count
                    };
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        private NotaFiscalItemBloqueado BloquearNotaFiscalItem(int notaFiscalItemId)
        {
            const string sql = @"
SELECT nfi.Id AS NotaFiscalItemId,
       nfi.NotaFiscalId,
       nf.Numero AS NotaFiscalNr,
       nf.DataEmissao,
       nfi.Item AS ItemNr,
       nfi.Volume AS VolumeNr,
       nfi.Quantidade AS QuantidadeNF
FROM NotaFiscalItem nfi WITH (UPDLOCK, HOLDLOCK, ROWLOCK)
INNER JOIN NotaFiscal nf WITH (UPDLOCK, HOLDLOCK, ROWLOCK) ON nf.Id = nfi.NotaFiscalId
WHERE nfi.Id = @notaFiscalItemId
  AND nfi.FilialId = @filialId
  AND nf.FilialId = @filialId";

            var row = db.Database.SqlQuery<NotaFiscalItemBloqueado>(
                sql,
                new SqlParameter("@notaFiscalItemId", notaFiscalItemId),
                new SqlParameter("@filialId", filialId)).FirstOrDefault();

            if (row == null) throw new InvalidOperationException("Nota fiscal/item não localizado para a filial atual.");
            if (!row.DataEmissao.HasValue) throw new InvalidOperationException("A nota fiscal não possui data de emissão.");
            return row;
        }

        private void ValidarVinculos(AnomaliaItemCadastroRequest item, NotaFiscalItemBloqueado itemFiscal)
        {
            if (item.NotaFiscalId != itemFiscal.NotaFiscalId)
                throw new InvalidOperationException("O item informado não pertence à nota fiscal selecionada.");

            string volumeInformado = (item.VolumeNr ?? string.Empty).Trim();
            string volumeFiscal = (itemFiscal.VolumeNr ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(volumeInformado) &&
                !string.Equals(volumeInformado, volumeFiscal, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("O volume informado não pertence ao item da nota fiscal.");
        }

        private static void ValidarCamposPorTipo(AnomaliaItemCadastroRequest item, string tipoCodigo)
        {
            if (tipoCodigo == "C" && string.IsNullOrWhiteSpace(item.ItemRecebidoNr))
                throw new InvalidOperationException("Informe o item efetivamente recebido para a anomalia tipo C.");
            if (tipoCodigo == "G" && string.IsNullOrWhiteSpace(item.Observacao))
                throw new InvalidOperationException("Informe o detalhe do defeito ou dano para a anomalia tipo G.");
            if (tipoCodigo == "G" && !item.InstaladoVeiculo.HasValue)
                throw new InvalidOperationException("Informe se a peça foi instalada no veículo para a anomalia tipo G.");
            if (tipoCodigo == "G" && string.IsNullOrWhiteSpace(item.CondicaoEmbalagem))
                throw new InvalidOperationException("Informe as condições da embalagem para a anomalia tipo G.");
        }

        private TipoAnomaliaRow ObterTipoOperacional(string codigo)
        {
            const string sql = @"
SELECT TOP 1 Id, Codigo, Descricao, PrazoDias
FROM AnomaliaGmTipo
WHERE Codigo = @codigo AND Ativo = 1 AND Operacional = 1";
            string normalizado = (codigo ?? string.Empty).Trim().ToUpperInvariant();
            var tipo = db.Database.SqlQuery<TipoAnomaliaRow>(
                sql,
                new SqlParameter("@codigo", normalizado)).FirstOrDefault();
            if (tipo == null) throw new InvalidOperationException("Tipo de anomalia inexistente, inativo ou não operacional.");
            return tipo;
        }

        private decimal ObterQuantidadeConsumida(int notaFiscalItemId, bool excesso)
        {
            const string sql = @"
SELECT ISNULL(SUM(ai.QuantidadeReclamada), 0)
FROM AnomaliaGmItem ai WITH (UPDLOCK, HOLDLOCK)
INNER JOIN AnomaliaGmTipo tipo ON tipo.Id = ai.AnomaliaTipoId
WHERE ai.FilialId = @filialId
  AND ai.NotaFiscalItemId = @notaFiscalItemId
  AND ai.SaldoConsumido = 1
  AND ai.Cancelado = 0
  AND ((@excesso = 1 AND tipo.Codigo = 'B') OR (@excesso = 0 AND tipo.Codigo <> 'B'))";

            return db.Database.SqlQuery<decimal>(
                sql,
                new SqlParameter("@filialId", filialId),
                new SqlParameter("@notaFiscalItemId", notaFiscalItemId),
                new SqlParameter("@excesso", excesso)).FirstOrDefault();
        }

        private string ObterEIncrementarNumeroControle()
        {
            const string selectSql = @"
SELECT TOP 1 Id, Valor
FROM AppConfig WITH (UPDLOCK, HOLDLOCK, ROWLOCK)
WHERE Nome = @nome
  AND (FilialId = @filialId OR FilialId IS NULL)
ORDER BY CASE WHEN FilialId = @filialId THEN 0 ELSE 1 END, Id";
            var config = db.Database.SqlQuery<ControleNrRow>(
                selectSql,
                new SqlParameter("@nome", ControleNrConfigName),
                new SqlParameter("@filialId", filialId)).FirstOrDefault();
            int atual;
            if (config == null || !int.TryParse(config.Valor, out atual))
                throw new InvalidOperationException("Parâmetro ControleNr não localizado ou inválido.");

            db.Database.ExecuteSqlCommand(
                @"UPDATE AppConfig
                     SET Valor = @valor, ModificadoPor = @usuario, ModificadoEm = @agora
                   WHERE Id = @id",
                new SqlParameter("@valor", (atual + 1).ToString(CultureInfo.InvariantCulture)),
                new SqlParameter("@usuario", usuario),
                new SqlParameter("@agora", agora),
                new SqlParameter("@id", config.Id));
            return atual.ToString("D8", CultureInfo.InvariantCulture);
        }

        private int InserirProcesso(string numeroControle, AnomaliaProcessoCadastroRequest request)
        {
            const string sql = @"
INSERT INTO AnomaliaGmProcesso
    (NumeroControle, StatusId, DataAbertura, UsuarioLogin, Observacao, EmpresaId,
     FilialId, Ativo, Cancelado, CriadoEm, CriadoPor)
VALUES
    (@numeroControle, @statusId, @agora, @usuario, @observacao, @empresaId,
     @filialId, 1, 0, @agora, @usuario);
SELECT CAST(SCOPE_IDENTITY() AS int);";
            return db.Database.SqlQuery<int>(
                sql,
                new SqlParameter("@numeroControle", numeroControle),
                new SqlParameter("@statusId", AnomaliaGmStatusIds.EmProcesso),
                new SqlParameter("@agora", agora),
                new SqlParameter("@usuario", usuario),
                SqlNullable("@observacao", request.Observacao, SqlDbType.VarChar, 1000),
                SqlNullable("@empresaId", request.EmpresaId, SqlDbType.Int),
                new SqlParameter("@filialId", filialId)).First();
        }

        private int InserirItem(
            int anomaliaId,
            TipoAnomaliaRow tipo,
            AnomaliaItemCadastroRequest item,
            NotaFiscalItemBloqueado itemFiscal,
            DateTime dataLimite)
        {
            const string sql = @"
INSERT INTO AnomaliaGmItem
    (AnomaliaId, AnomaliaTipoId, NotaFiscalId, NotaFiscalItemId, VolumeNr, ItemNr,
     QuantidadeNF, QuantidadeReclamada, QuantidadeRecebida, ItemRecebidoNr, StatusId,
     Observacao, InstaladoVeiculo, CondicaoEmbalagem,
     DataReclamacao, DataLimiteReclamacao, SaldoConsumido, Cancelado,
     FilialId, CriadoEm, CriadoPor)
VALUES
    (@anomaliaId, @tipoId, @notaFiscalId, @notaFiscalItemId, @volumeNr, @itemNr,
     @quantidadeNF, @quantidadeReclamada, @quantidadeRecebida, @itemRecebidoNr, @statusId,
     @observacao, @instaladoVeiculo, @condicaoEmbalagem,
     @agora, @dataLimite, 1, 0, @filialId, @agora, @usuario);
SELECT CAST(SCOPE_IDENTITY() AS int);";
            return db.Database.SqlQuery<int>(
                sql,
                new SqlParameter("@anomaliaId", anomaliaId),
                new SqlParameter("@tipoId", tipo.Id),
                new SqlParameter("@notaFiscalId", itemFiscal.NotaFiscalId),
                new SqlParameter("@notaFiscalItemId", itemFiscal.NotaFiscalItemId),
                SqlNullable("@volumeNr", itemFiscal.VolumeNr, SqlDbType.VarChar, 100),
                new SqlParameter("@itemNr", itemFiscal.ItemNr ?? string.Empty),
                new SqlParameter("@quantidadeNF", itemFiscal.QuantidadeNF),
                new SqlParameter("@quantidadeReclamada", item.QuantidadeReclamada),
                SqlNullable("@quantidadeRecebida", item.QuantidadeRecebida, SqlDbType.Decimal),
                SqlNullable("@itemRecebidoNr", item.ItemRecebidoNr, SqlDbType.VarChar, 100),
                new SqlParameter("@statusId", AnomaliaGmStatusIds.EmProcesso),
                SqlNullable("@observacao", item.Observacao, SqlDbType.VarChar, 1000),
                SqlNullable("@instaladoVeiculo", item.InstaladoVeiculo, SqlDbType.Bit),
                SqlNullable("@condicaoEmbalagem", item.CondicaoEmbalagem, SqlDbType.VarChar, 500),
                new SqlParameter("@agora", agora),
                new SqlParameter("@dataLimite", dataLimite),
                new SqlParameter("@filialId", filialId),
                new SqlParameter("@usuario", usuario)).First();
        }

        private void InserirHistorico(
            int anomaliaId,
            int? anomaliaItemId,
            string evento,
            int? statusAnterior,
            int? statusNovo,
            string observacao)
        {
            db.Database.ExecuteSqlCommand(
                @"INSERT INTO AnomaliaGmHistorico
                    (AnomaliaId, AnomaliaItemId, Evento, StatusAnteriorId, StatusNovoId,
                     UsuarioLogin, DataHora, Observacao, FilialId)
                  VALUES
                    (@anomaliaId, @itemId, @evento, @anterior, @novo,
                     @usuario, @agora, @observacao, @filialId)",
                new SqlParameter("@anomaliaId", anomaliaId),
                SqlNullable("@itemId", anomaliaItemId, SqlDbType.Int),
                new SqlParameter("@evento", evento),
                SqlNullable("@anterior", statusAnterior, SqlDbType.Int),
                SqlNullable("@novo", statusNovo, SqlDbType.Int),
                new SqlParameter("@usuario", usuario),
                new SqlParameter("@agora", agora),
                SqlNullable("@observacao", observacao, SqlDbType.VarChar, 1000),
                new SqlParameter("@filialId", filialId));
        }

        internal static SqlParameter SqlNullable(string nome, object valor, SqlDbType tipo, int? tamanho = null)
        {
            var parameter = new SqlParameter(nome, tipo) { Value = valor ?? DBNull.Value };
            if (tamanho.HasValue) parameter.Size = tamanho.Value;
            if (tipo == SqlDbType.Decimal)
            {
                parameter.Precision = 18;
                parameter.Scale = 4;
            }
            return parameter;
        }

        private class NotaFiscalItemBloqueado
        {
            public int NotaFiscalItemId { get; set; }
            public int NotaFiscalId { get; set; }
            public string NotaFiscalNr { get; set; }
            public DateTime? DataEmissao { get; set; }
            public string ItemNr { get; set; }
            public string VolumeNr { get; set; }
            public decimal QuantidadeNF { get; set; }
        }

        private class TipoAnomaliaRow
        {
            public int Id { get; set; }
            public string Codigo { get; set; }
            public string Descricao { get; set; }
            public int PrazoDias { get; set; }
        }

        private class ControleNrRow
        {
            public int Id { get; set; }
            public string Valor { get; set; }
        }
    }
}
