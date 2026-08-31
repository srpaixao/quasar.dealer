using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using Simplify.Quasar.Models;

namespace Simplify.Quasar.Areas.AnomaliaApp.Services
{
    public class AnomaliaConsultaService
    {
        private readonly Quasar_Entities db;
        private readonly int filialId;
        private readonly DateTime agora;

        public AnomaliaConsultaService(Quasar_Entities db, int filialId, DateTime agora)
        {
            this.db = db ?? throw new ArgumentNullException("db");
            this.filialId = filialId;
            this.agora = agora;
        }

        public IList<AnomaliaPesquisaItemResult> PesquisarItens(string tipoCodigo, string pesquisarPor, string termo)
        {
            string tipo = (tipoCodigo ?? string.Empty).Trim().ToUpperInvariant();
            string modo = (pesquisarPor ?? string.Empty).Trim().ToUpperInvariant();
            string valor = (termo ?? string.Empty).Trim();
            if (tipo != "A" && tipo != "B" && tipo != "C" && tipo != "G")
                throw new InvalidOperationException("Selecione um tipo operacional válido.");
            if (modo != "NF" && modo != "VOLUME" && modo != "ITEM")
                throw new InvalidOperationException("Selecione uma forma de pesquisa válida.");
            if (string.IsNullOrWhiteSpace(valor))
                throw new InvalidOperationException("Informe um valor para pesquisa.");

            string filtro = modo == "NF"
                ? "nf.Numero = @termo"
                : modo == "VOLUME"
                    ? "nfi.Volume = @termo"
                    : "nfi.Item = @termo";

            string sql = @"
SELECT TOP 200
       nf.Id AS NotaFiscalId,
       nfi.Id AS NotaFiscalItemId,
       nf.Numero AS NotaFiscalNr,
       CAST(nf.DataEmissao AS datetime) AS DataEmissao,
       nfi.Volume AS VolumeNr,
       nfi.Item AS ItemNr,
       ISNULL(material.Descricao, '') AS Descricao,
       nfi.Quantidade AS QuantidadeNF,
       nfi.QtdConferida AS QuantidadeRecebida,
       saldo.QuantidadeJaReclamada,
       CASE
           WHEN tipo.Codigo = 'B' THEN
               CASE WHEN ISNULL(nfi.QtdConferida, 0) - nfi.Quantidade - saldo.QuantidadeJaReclamada > 0
                    THEN ISNULL(nfi.QtdConferida, 0) - nfi.Quantidade - saldo.QuantidadeJaReclamada ELSE 0 END
           ELSE
               CASE WHEN nfi.Quantidade - saldo.QuantidadeJaReclamada > 0
                    THEN nfi.Quantidade - saldo.QuantidadeJaReclamada ELSE 0 END
       END AS SaldoDisponivel,
       tipo.PrazoDias,
       DATEDIFF(DAY, CAST(nf.DataEmissao AS date), @agoraData) AS DiasDecorridos,
       CAST(CASE WHEN DATEDIFF(DAY, CAST(nf.DataEmissao AS date), @agoraData) <= tipo.PrazoDias THEN 1 ELSE 0 END AS bit) AS DentroDoPrazo,
       DATEADD(DAY, tipo.PrazoDias, CAST(nf.DataEmissao AS date)) AS DataLimite
FROM NotaFiscalItem nfi
INNER JOIN NotaFiscal nf ON nf.Id = nfi.NotaFiscalId
INNER JOIN AnomaliaGmTipo tipo ON tipo.Codigo = @tipo AND tipo.Ativo = 1 AND tipo.Operacional = 1
LEFT JOIN Material material ON material.Codigo = nfi.Item AND (material.FilialId = @filialId OR material.FilialId IS NULL)
OUTER APPLY
(
    SELECT ISNULL(SUM(ai.QuantidadeReclamada), 0) AS QuantidadeJaReclamada
    FROM AnomaliaGmItem ai
    INNER JOIN AnomaliaGmTipo tipoConsumido ON tipoConsumido.Id = ai.AnomaliaTipoId
    WHERE ai.FilialId = @filialId
      AND ai.NotaFiscalItemId = nfi.Id
      AND ai.SaldoConsumido = 1
      AND ai.Cancelado = 0
      AND ((tipo.Codigo = 'B' AND tipoConsumido.Codigo = 'B') OR
           (tipo.Codigo <> 'B' AND tipoConsumido.Codigo <> 'B'))
) saldo
WHERE nfi.FilialId = @filialId
  AND nf.FilialId = @filialId
  AND nf.DataEmissao IS NOT NULL
  AND DATEDIFF(DAY, CAST(nf.DataEmissao AS date), @agoraData) <= tipo.PrazoDias
  AND " + filtro + @"
  AND
  (
      (tipo.Codigo = 'B' AND ISNULL(nfi.QtdConferida, 0) - nfi.Quantidade - saldo.QuantidadeJaReclamada > 0)
      OR
      (tipo.Codigo <> 'B' AND nfi.Quantidade - saldo.QuantidadeJaReclamada > 0)
  )
ORDER BY nf.DataEmissao, nf.Numero, nfi.Volume, nfi.Item";

            return db.Database.SqlQuery<AnomaliaPesquisaItemResult>(
                sql,
                new SqlParameter("@tipo", tipo),
                new SqlParameter("@filialId", filialId),
                new SqlParameter("@agoraData", agora.Date),
                new SqlParameter("@termo", valor)).ToList();
        }

        public IList<AnomaliaItemOcorrenciaResult> PesquisarOcorrenciasItem(string termo)
        {
            string valor = (termo ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(valor))
                throw new InvalidOperationException("Informe o Item Nr para pesquisa.");

            const string sql = @"
SELECT TOP 200
       nf.Id AS NotaFiscalId,
       nfi.Id AS NotaFiscalItemId,
       nf.Numero AS NotaFiscalNr,
       CAST(nf.DataEmissao AS datetime) AS DataEmissao,
       nfi.Volume AS VolumeNr,
       nfi.Item AS ItemNr,
       ISNULL(material.Descricao, '') AS Descricao,
       nfi.Quantidade AS QuantidadeNF,
       nfi.QtdConferida AS QuantidadeRecebida,
       CASE WHEN nfi.Quantidade - saldo.ConsumidoPadrao > 0
            THEN nfi.Quantidade - saldo.ConsumidoPadrao ELSE 0 END AS SaldoPadrao,
       CASE WHEN ISNULL(nfi.QtdConferida, 0) - nfi.Quantidade - saldo.ConsumidoExcesso > 0
            THEN ISNULL(nfi.QtdConferida, 0) - nfi.Quantidade - saldo.ConsumidoExcesso ELSE 0 END AS SaldoExcesso,
       DATEDIFF(DAY, CAST(nf.DataEmissao AS date), @agoraData) AS DiasDecorridos,
       prazos.PrazoMinimoDias,
       prazos.PrazoMaximoDias,
       DATEADD(DAY, prazos.PrazoMinimoDias, CAST(nf.DataEmissao AS date)) AS DataLimiteMinima,
       DATEADD(DAY, prazos.PrazoMaximoDias, CAST(nf.DataEmissao AS date)) AS DataLimiteMaxima,
       CAST(CASE WHEN DATEDIFF(DAY, CAST(nf.DataEmissao AS date), @agoraData) > prazos.PrazoMaximoDias THEN 1 ELSE 0 END AS bit) AS TodosTiposForaDoPrazo,
       CAST(CASE WHEN nfi.Quantidade - saldo.ConsumidoPadrao <= 0
                      AND ISNULL(nfi.QtdConferida, 0) - nfi.Quantidade - saldo.ConsumidoExcesso <= 0
                 THEN 1 ELSE 0 END AS bit) AS SemSaldo
FROM NotaFiscalItem nfi
INNER JOIN NotaFiscal nf ON nf.Id = nfi.NotaFiscalId
LEFT JOIN Material material
       ON material.Codigo = nfi.Item
      AND (material.FilialId = @filialId OR material.FilialId IS NULL)
CROSS APPLY
(
    SELECT MIN(PrazoDias) AS PrazoMinimoDias,
           MAX(PrazoDias) AS PrazoMaximoDias
    FROM AnomaliaGmTipo
    WHERE Ativo = 1 AND Operacional = 1
) prazos
OUTER APPLY
(
    SELECT ISNULL(SUM(CASE WHEN tipo.Codigo <> 'B' THEN ai.QuantidadeReclamada ELSE 0 END), 0) AS ConsumidoPadrao,
           ISNULL(SUM(CASE WHEN tipo.Codigo = 'B' THEN ai.QuantidadeReclamada ELSE 0 END), 0) AS ConsumidoExcesso
    FROM AnomaliaGmItem ai
    INNER JOIN AnomaliaGmTipo tipo ON tipo.Id = ai.AnomaliaTipoId
    WHERE ai.FilialId = @filialId
      AND ai.NotaFiscalItemId = nfi.Id
      AND ai.SaldoConsumido = 1
      AND ai.Cancelado = 0
) saldo
WHERE nfi.FilialId = @filialId
  AND nf.FilialId = @filialId
  AND nf.DataEmissao IS NOT NULL
  AND nfi.Item = @termo
ORDER BY DATEADD(DAY, prazos.PrazoMinimoDias, CAST(nf.DataEmissao AS date)),
         nf.Numero, nfi.Volume";

            return db.Database.SqlQuery<AnomaliaItemOcorrenciaResult>(
                sql,
                new SqlParameter("@filialId", filialId),
                new SqlParameter("@agoraData", agora.Date),
                new SqlParameter("@termo", valor)).ToList();
        }

        public AnomaliaPesquisaItemResult ObterContextoItem(int notaFiscalItemId, string tipoCodigo)
        {
            string tipo = (tipoCodigo ?? string.Empty).Trim().ToUpperInvariant();
            if (tipo != "A" && tipo != "B" && tipo != "C" && tipo != "G")
                throw new InvalidOperationException("Selecione um tipo operacional válido.");

            const string sql = @"
SELECT nf.Id AS NotaFiscalId,
       nfi.Id AS NotaFiscalItemId,
       nf.Numero AS NotaFiscalNr,
       CAST(nf.DataEmissao AS datetime) AS DataEmissao,
       nfi.Volume AS VolumeNr,
       nfi.Item AS ItemNr,
       ISNULL(material.Descricao, '') AS Descricao,
       nfi.Quantidade AS QuantidadeNF,
       nfi.QtdConferida AS QuantidadeRecebida,
       saldo.QuantidadeJaReclamada,
       CASE WHEN tipo.Codigo = 'B'
            THEN CASE WHEN ISNULL(nfi.QtdConferida, 0) - nfi.Quantidade - saldo.QuantidadeJaReclamada > 0
                      THEN ISNULL(nfi.QtdConferida, 0) - nfi.Quantidade - saldo.QuantidadeJaReclamada ELSE 0 END
            ELSE CASE WHEN nfi.Quantidade - saldo.QuantidadeJaReclamada > 0
                      THEN nfi.Quantidade - saldo.QuantidadeJaReclamada ELSE 0 END
       END AS SaldoDisponivel,
       tipo.PrazoDias,
       DATEDIFF(DAY, CAST(nf.DataEmissao AS date), @agoraData) AS DiasDecorridos,
       CAST(CASE WHEN DATEDIFF(DAY, CAST(nf.DataEmissao AS date), @agoraData) <= tipo.PrazoDias THEN 1 ELSE 0 END AS bit) AS DentroDoPrazo,
       DATEADD(DAY, tipo.PrazoDias, CAST(nf.DataEmissao AS date)) AS DataLimite
FROM NotaFiscalItem nfi
INNER JOIN NotaFiscal nf ON nf.Id = nfi.NotaFiscalId
INNER JOIN AnomaliaGmTipo tipo
        ON tipo.Codigo = @tipo AND tipo.Ativo = 1 AND tipo.Operacional = 1
LEFT JOIN Material material
       ON material.Codigo = nfi.Item
      AND (material.FilialId = @filialId OR material.FilialId IS NULL)
OUTER APPLY
(
    SELECT ISNULL(SUM(ai.QuantidadeReclamada), 0) AS QuantidadeJaReclamada
    FROM AnomaliaGmItem ai
    INNER JOIN AnomaliaGmTipo tipoConsumido ON tipoConsumido.Id = ai.AnomaliaTipoId
    WHERE ai.FilialId = @filialId
      AND ai.NotaFiscalItemId = nfi.Id
      AND ai.SaldoConsumido = 1
      AND ai.Cancelado = 0
      AND ((tipo.Codigo = 'B' AND tipoConsumido.Codigo = 'B') OR
           (tipo.Codigo <> 'B' AND tipoConsumido.Codigo <> 'B'))
) saldo
WHERE nfi.Id = @notaFiscalItemId
  AND nfi.FilialId = @filialId
  AND nf.FilialId = @filialId";

            var result = db.Database.SqlQuery<AnomaliaPesquisaItemResult>(
                sql,
                new SqlParameter("@notaFiscalItemId", notaFiscalItemId),
                new SqlParameter("@filialId", filialId),
                new SqlParameter("@agoraData", agora.Date),
                new SqlParameter("@tipo", tipo)).FirstOrDefault();
            if (result == null) throw new InvalidOperationException("Item não localizado para a filial atual.");
            return result;
        }

        public IList<AnomaliaProcessoResumo> ConsultarProcessos(string numeroControle, string tipo, int? statusId)
        {
            const string sql = @"
SELECT p.Id,
       p.NumeroControle,
       p.DataAbertura,
       STUFF((SELECT DISTINCT ', ' + t2.Codigo
                FROM AnomaliaGmItem i2
                INNER JOIN AnomaliaGmTipo t2 ON t2.Id = i2.AnomaliaTipoId
               WHERE i2.AnomaliaId = p.Id AND i2.Cancelado = 0
               FOR XML PATH('')), 1, 2, '') AS Tipos,
       COUNT(i.Id) AS QuantidadeItens,
       SUM(CASE WHEN i.StatusId = 1 THEN 1 ELSE 0 END) AS EmProcesso,
       SUM(CASE WHEN i.StatusId = 2 THEN 1 ELSE 0 END) AS Aceitos,
       SUM(CASE WHEN i.StatusId = 3 THEN 1 ELSE 0 END) AS Rejeitados,
       s.Descricao AS StatusDescricao,
       p.CriadoPor
FROM AnomaliaGmProcesso p
INNER JOIN AnomaliaGmStatus s ON s.Id = p.StatusId
INNER JOIN AnomaliaGmItem i ON i.AnomaliaId = p.Id AND i.Cancelado = 0
INNER JOIN AnomaliaGmTipo t ON t.Id = i.AnomaliaTipoId
WHERE p.FilialId = @filialId
  AND p.Ativo = 1
  AND p.Cancelado = 0
  AND (@controle = '' OR p.NumeroControle LIKE '%' + @controle + '%')
  AND (@tipo = '' OR t.Codigo = @tipo)
  AND (@statusId IS NULL OR p.StatusId = @statusId)
GROUP BY p.Id, p.NumeroControle, p.DataAbertura, s.Descricao, p.CriadoPor
ORDER BY p.DataAbertura DESC, p.Id DESC";

            return db.Database.SqlQuery<AnomaliaProcessoResumo>(
                sql,
                new SqlParameter("@filialId", filialId),
                new SqlParameter("@controle", (numeroControle ?? string.Empty).Trim()),
                new SqlParameter("@tipo", (tipo ?? string.Empty).Trim().ToUpperInvariant()),
                AnomaliaService.SqlNullable("@statusId", statusId, System.Data.SqlDbType.Int)).ToList();
        }

        public IList<AnomaliaItemDetalhe> ObterItens(int anomaliaId)
        {
            const string sql = @"
SELECT i.Id,
       tipo.Codigo AS TipoCodigo,
       nf.Numero AS NotaFiscalNr,
       CAST(nf.DataEmissao AS datetime) AS DataEmissao,
       i.VolumeNr,
       i.ItemNr,
       ISNULL(material.Descricao, '') AS Descricao,
       i.QuantidadeNF,
       i.QuantidadeReclamada,
       i.StatusId,
       status.Descricao AS StatusDescricao,
       i.DataLimiteReclamacao AS DataLimite,
       i.Observacao,
       i.InstaladoVeiculo,
       i.CondicaoEmbalagem
FROM AnomaliaGmItem i
INNER JOIN AnomaliaGmProcesso p ON p.Id = i.AnomaliaId
INNER JOIN AnomaliaGmTipo tipo ON tipo.Id = i.AnomaliaTipoId
INNER JOIN AnomaliaGmStatus status ON status.Id = i.StatusId
INNER JOIN NotaFiscal nf ON nf.Id = i.NotaFiscalId
LEFT JOIN Material material ON material.Codigo = i.ItemNr AND (material.FilialId = @filialId OR material.FilialId IS NULL)
WHERE i.AnomaliaId = @anomaliaId
  AND i.FilialId = @filialId
  AND p.FilialId = @filialId
  AND i.Cancelado = 0
ORDER BY i.Id";
            return db.Database.SqlQuery<AnomaliaItemDetalhe>(
                sql,
                new SqlParameter("@anomaliaId", anomaliaId),
                new SqlParameter("@filialId", filialId)).ToList();
        }
    }
}
