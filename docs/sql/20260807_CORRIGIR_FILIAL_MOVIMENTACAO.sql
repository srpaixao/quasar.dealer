SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

UPDATE movimento
SET movimento.FilialId = COALESCE(locacao.FilialId, estoque.FilialId)
FROM dbo.Movimentacao AS movimento
OUTER APPLY
(
    SELECT TOP (1) l.FilialId
    FROM dbo.Locacao AS l
    WHERE REPLACE(REPLACE(UPPER(LTRIM(RTRIM(l.Codigo))), '.', ''), ' ', '')
        = REPLACE(REPLACE(UPPER(LTRIM(RTRIM(movimento.LocacaoEspera))), '.', ''), ' ', '')
      AND l.FilialId IS NOT NULL
    ORDER BY l.Id
) AS locacao
OUTER APPLY
(
    SELECT TOP (1) e.FilialId
    FROM dbo.Estoque AS e
    WHERE e.ItemNr = movimento.ItemNr
      AND e.FilialId IS NOT NULL
    ORDER BY CASE WHEN ISNULL(e.Saldo, 0) > 0 THEN 0 ELSE 1 END, e.Id
) AS estoque
WHERE movimento.FilialId IS NULL
  AND COALESCE(locacao.FilialId, estoque.FilialId) IS NOT NULL;

SELECT
    Id,
    ItemNr,
    LocacaoEspera,
    FilialId,
    CriadoEm,
    FinalizadoEm
FROM dbo.Movimentacao
WHERE FilialId IS NULL
ORDER BY Id DESC;

COMMIT TRANSACTION;
