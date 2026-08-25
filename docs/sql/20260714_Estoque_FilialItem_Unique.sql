SET XACT_ABORT ON;
GO

IF EXISTS
(
    SELECT 1
    FROM dbo.Estoque
    GROUP BY FilialId, ItemNr
    HAVING COUNT(*) > 1
)
BEGIN
    THROW 50001, 'Existem registros duplicados em Estoque para FilialId e ItemNr.', 1;
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Estoque')
      AND name = N'UX_Estoque_FilialId_ItemNr'
)
BEGIN
    CREATE UNIQUE INDEX UX_Estoque_FilialId_ItemNr
        ON dbo.Estoque (FilialId, ItemNr);
END;
GO
