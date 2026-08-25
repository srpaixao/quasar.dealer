/*
    Recebimento - consolidacao da tabela Volume
    Data: 03/08/2026

    Regra:
      - somente uma linha por FilialId + VolumeNr;
      - as NFs relacionadas ao volume permanecem em NotaFiscalItem;
      - QtdItens recebe a soma das linhas consolidadas;
      - se alguma linha estiver pendente, o volume consolidado fica pendente;
      - FilialId ausente e recuperado pela Area.

    IMPORTANTE: gerar backup antes de executar em producao.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;

BEGIN TRANSACTION;

UPDATE Volume
SET FilialId = Area.FilialId
FROM dbo.Volume AS Volume
INNER JOIN dbo.Area AS Area ON Area.Id = Volume.AreaId
WHERE Volume.FilialId IS NULL
  AND Area.FilialId IS NOT NULL;

DECLARE @FiliaisCorrigidas int = @@ROWCOUNT;

IF OBJECT_ID('tempdb..#VolumeConsolidacao') IS NOT NULL
    DROP TABLE #VolumeConsolidacao;

SELECT
    Volume.FilialId,
    Volume.NotaFiscalNr,
    Volume.VolumeNr,
    ROW_NUMBER() OVER
    (
        PARTITION BY Volume.FilialId, LTRIM(RTRIM(Volume.VolumeNr))
        ORDER BY
            CASE Volume.StatusId
                WHEN 1 THEN 0
                WHEN 2 THEN 1
                WHEN 3 THEN 3
                ELSE 2
            END,
            ISNULL(Volume.CriadoEm, '99991231'),
            Volume.NotaFiscalNr
    ) AS Ordem,
    SUM(Volume.QtdItens) OVER
    (
        PARTITION BY Volume.FilialId, LTRIM(RTRIM(Volume.VolumeNr))
    ) AS QuantidadeItens,
    MAX(CASE WHEN Volume.StatusId = 1 THEN 1 ELSE 0 END) OVER
    (
        PARTITION BY Volume.FilialId, LTRIM(RTRIM(Volume.VolumeNr))
    ) AS TemPendente,
    MAX(CASE WHEN Volume.StatusId = 2 THEN 1 ELSE 0 END) OVER
    (
        PARTITION BY Volume.FilialId, LTRIM(RTRIM(Volume.VolumeNr))
    ) AS TemConfirmado,
    MAX(CASE WHEN Volume.StatusId = 3 THEN 1 ELSE 0 END) OVER
    (
        PARTITION BY Volume.FilialId, LTRIM(RTRIM(Volume.VolumeNr))
    ) AS TemIncorreto
INTO #VolumeConsolidacao
FROM dbo.Volume AS Volume
WHERE Volume.FilialId IS NOT NULL
  AND NULLIF(LTRIM(RTRIM(Volume.VolumeNr)), '') IS NOT NULL
  AND LTRIM(RTRIM(Volume.VolumeNr)) <> 'None';

UPDATE Volume
SET
    QtdItens = Consolidacao.QuantidadeItens,
    StatusId = CASE
        WHEN Consolidacao.TemPendente = 1 THEN 1
        WHEN Consolidacao.TemConfirmado = 1 THEN 2
        WHEN Consolidacao.TemIncorreto = 1 THEN 3
        ELSE Volume.StatusId
    END
FROM dbo.Volume AS Volume
INNER JOIN #VolumeConsolidacao AS Consolidacao
    ON Consolidacao.FilialId = Volume.FilialId
   AND Consolidacao.NotaFiscalNr = Volume.NotaFiscalNr
   AND Consolidacao.VolumeNr = Volume.VolumeNr
WHERE Consolidacao.Ordem = 1;

DELETE Volume
FROM dbo.Volume AS Volume
INNER JOIN #VolumeConsolidacao AS Consolidacao
    ON Consolidacao.FilialId = Volume.FilialId
   AND Consolidacao.NotaFiscalNr = Volume.NotaFiscalNr
   AND Consolidacao.VolumeNr = Volume.VolumeNr
WHERE Consolidacao.Ordem > 1;

DECLARE @RegistrosConsolidados int = @@ROWCOUNT;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Volume')
      AND name = N'UX_Volume_Filial_VolumeNr'
)
BEGIN
    CREATE UNIQUE INDEX UX_Volume_Filial_VolumeNr
        ON dbo.Volume (FilialId, VolumeNr)
        WHERE FilialId IS NOT NULL
          AND VolumeNr <> ''
          AND VolumeNr <> 'None';
END;

COMMIT TRANSACTION;

SELECT
    @FiliaisCorrigidas AS RegistrosComFilialCorrigida,
    @RegistrosConsolidados AS RegistrosDuplicadosRemovidos;

SELECT
    FilialId,
    LTRIM(RTRIM(VolumeNr)) AS VolumeNr,
    COUNT(*) AS QuantidadeRegistros
FROM dbo.Volume
WHERE FilialId IS NOT NULL
  AND NULLIF(LTRIM(RTRIM(VolumeNr)), '') IS NOT NULL
  AND LTRIM(RTRIM(VolumeNr)) <> 'None'
GROUP BY FilialId, LTRIM(RTRIM(VolumeNr))
HAVING COUNT(*) > 1;
