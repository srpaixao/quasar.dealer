/*
    Cadastra os volumes das NFs informadas para nova conferencia.

    Regra:
      - uma linha por FilialId + VolumeNr;
      - FilialId = 1;
      - AreaId = 21;
      - StatusId = 1 (Pendente);
      - as relacoes com todas as NFs permanecem em NotaFiscalItem;
      - QtdItens e calculada considerando todos os itens do volume na filial.

    IMPORTANTE: gerar backup antes de executar em producao.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @FilialId int = 1;
DECLARE @AreaId int = 21;
DECLARE @Usuario varchar(100) = 'SCRIPT_20260803';
DECLARE @Agora datetime = GETDATE();

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.Area
    WHERE Id = @AreaId
      AND FilialId = @FilialId
)
    THROW 50001, 'A area informada nao pertence a filial.', 1;

CREATE TABLE #NotasSolicitadas
(
    Numero varchar(20) COLLATE DATABASE_DEFAULT NOT NULL PRIMARY KEY
);

INSERT INTO #NotasSolicitadas (Numero)
VALUES
    ('014532439'),
    ('014532355'),
    ('014532521'),
    ('014532522'),
    ('014532525'),
    ('014532527'),
    ('014532528'),
    ('014532529'),
    ('014532115'),
    ('014532125'),
    ('014532341'),
    ('014532349'),
    ('014532440'),
    ('014532561'),
    ('014533813'),
    ('014532548'),
    ('014532420');

IF EXISTS
(
    SELECT 1
    FROM #NotasSolicitadas AS Solicitada
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.NotaFiscal AS Nota
        WHERE Nota.Numero = Solicitada.Numero
          AND Nota.FilialId = @FilialId
    )
)
BEGIN
    SELECT Solicitada.Numero AS NotaFiscalNaoLocalizada
    FROM #NotasSolicitadas AS Solicitada
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.NotaFiscal AS Nota
        WHERE Nota.Numero = Solicitada.Numero
          AND Nota.FilialId = @FilialId
    );

    THROW 50002, 'Existem notas fiscais nao localizadas para a filial.', 1;
END;

CREATE TABLE #VolumesSelecionados
(
    VolumeNr varchar(100) COLLATE DATABASE_DEFAULT NOT NULL PRIMARY KEY
);

INSERT INTO #VolumesSelecionados (VolumeNr)
SELECT DISTINCT LTRIM(RTRIM(Item.Volume))
FROM dbo.NotaFiscalItem AS Item
INNER JOIN dbo.NotaFiscal AS Nota ON Nota.Id = Item.NotaFiscalId
INNER JOIN #NotasSolicitadas AS Solicitada ON Solicitada.Numero = Nota.Numero
WHERE Nota.FilialId = @FilialId
  AND Item.FilialId = @FilialId
  AND NULLIF(LTRIM(RTRIM(Item.Volume)), '') IS NOT NULL
  AND LTRIM(RTRIM(Item.Volume)) <> 'None';

IF NOT EXISTS (SELECT 1 FROM #VolumesSelecionados)
    THROW 50003, 'Nenhum volume valido foi localizado nas notas fiscais.', 1;

CREATE TABLE #FonteVolume
(
    VolumeNr varchar(100) COLLATE DATABASE_DEFAULT NOT NULL PRIMARY KEY,
    NotaFiscalNr varchar(100) COLLATE DATABASE_DEFAULT NOT NULL,
    QtdItens int NOT NULL
);

INSERT INTO #FonteVolume (VolumeNr, NotaFiscalNr, QtdItens)
SELECT
    Selecionado.VolumeNr,
    MIN(Nota.Numero) AS NotaFiscalNrRepresentativa,
    COUNT(Item.Id) AS QtdItens
FROM #VolumesSelecionados AS Selecionado
INNER JOIN dbo.NotaFiscalItem AS Item
    ON LTRIM(RTRIM(Item.Volume)) = Selecionado.VolumeNr
   AND Item.FilialId = @FilialId
INNER JOIN dbo.NotaFiscal AS Nota
    ON Nota.Id = Item.NotaFiscalId
   AND Nota.FilialId = @FilialId
GROUP BY Selecionado.VolumeNr;

-- Previa do que sera processado.
SELECT
    Fonte.VolumeNr,
    Fonte.NotaFiscalNr,
    Fonte.QtdItens,
    CASE WHEN Volume.VolumeNr IS NULL THEN 'INSERIR' ELSE 'ATUALIZAR' END AS Operacao
FROM #FonteVolume AS Fonte
OUTER APPLY
(
    SELECT TOP (1) Existente.VolumeNr
    FROM dbo.Volume AS Existente
    WHERE Existente.FilialId = @FilialId
      AND LTRIM(RTRIM(Existente.VolumeNr)) = Fonte.VolumeNr
) AS Volume
ORDER BY Fonte.VolumeNr;

BEGIN TRANSACTION;

-- Recupera a filial de registros antigos desses volumes pela area.
UPDATE Volume
SET FilialId = @FilialId
FROM dbo.Volume AS Volume
INNER JOIN #FonteVolume AS Fonte
    ON LTRIM(RTRIM(Volume.VolumeNr)) = Fonte.VolumeNr
WHERE Volume.FilialId IS NULL
  AND Volume.AreaId = @AreaId;

-- Mantem apenas uma ocorrencia caso ainda existam registros antigos repetidos.
;WITH VolumeOrdenado AS
(
    SELECT
        Volume.NotaFiscalNr,
        Volume.VolumeNr,
        ROW_NUMBER() OVER
        (
            PARTITION BY Volume.FilialId, LTRIM(RTRIM(Volume.VolumeNr))
            ORDER BY
                CASE WHEN NULLIF(LTRIM(RTRIM(Volume.NotaFiscalNr)), '') IS NULL THEN 1 ELSE 0 END,
                ISNULL(Volume.CriadoEm, '99991231'),
                Volume.NotaFiscalNr
        ) AS Ordem
    FROM dbo.Volume AS Volume
    INNER JOIN #FonteVolume AS Fonte
        ON LTRIM(RTRIM(Volume.VolumeNr)) = Fonte.VolumeNr
    WHERE Volume.FilialId = @FilialId
)
DELETE Volume
FROM dbo.Volume AS Volume
INNER JOIN VolumeOrdenado AS Ordenado
    ON Ordenado.NotaFiscalNr = Volume.NotaFiscalNr
   AND Ordenado.VolumeNr = Volume.VolumeNr
WHERE Ordenado.Ordem > 1;

DECLARE @Removidos int = @@ROWCOUNT;

UPDATE Volume
SET
    AreaId = @AreaId,
    StatusId = 1,
    QtdItens = Fonte.QtdItens,
    FilialId = @FilialId,
    ModificadoPor = @Usuario,
    ModificadoEm = @Agora
FROM dbo.Volume AS Volume
INNER JOIN #FonteVolume AS Fonte
    ON LTRIM(RTRIM(Volume.VolumeNr)) = Fonte.VolumeNr
WHERE Volume.FilialId = @FilialId;

DECLARE @Atualizados int = @@ROWCOUNT;

INSERT INTO dbo.Volume
(
    NotaFiscalNr,
    VolumeNr,
    StatusId,
    AreaId,
    QtdItens,
    Imprimir,
    Danfe,
    CriadoPor,
    CriadoEm,
    FilialId
)
SELECT
    Fonte.NotaFiscalNr,
    Fonte.VolumeNr,
    1,
    @AreaId,
    Fonte.QtdItens,
    0,
    '',
    @Usuario,
    @Agora,
    @FilialId
FROM #FonteVolume AS Fonte
WHERE NOT EXISTS
(
    SELECT 1
    FROM dbo.Volume AS Volume WITH (UPDLOCK, HOLDLOCK)
    WHERE Volume.FilialId = @FilialId
      AND LTRIM(RTRIM(Volume.VolumeNr)) = Fonte.VolumeNr
);

DECLARE @Inseridos int = @@ROWCOUNT;

COMMIT TRANSACTION;

SELECT
    (SELECT COUNT(*) FROM #NotasSolicitadas) AS NotasProcessadas,
    (SELECT COUNT(*) FROM #FonteVolume) AS VolumesProcessados,
    @Inseridos AS VolumesInseridos,
    @Atualizados AS VolumesAtualizados,
    @Removidos AS RepeticoesRemovidas;

SELECT
    Volume.VolumeNr,
    Volume.NotaFiscalNr,
    Volume.QtdItens,
    Volume.StatusId,
    Volume.AreaId,
    Volume.FilialId
FROM dbo.Volume AS Volume
INNER JOIN #FonteVolume AS Fonte
    ON LTRIM(RTRIM(Volume.VolumeNr)) = Fonte.VolumeNr
WHERE Volume.FilialId = @FilialId
ORDER BY Volume.VolumeNr;
