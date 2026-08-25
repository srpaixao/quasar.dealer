SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF COL_LENGTH('dbo.Movimentacao', 'LocacaoEspera') IS NULL
    BEGIN
        ALTER TABLE dbo.Movimentacao
            ADD LocacaoEspera NVARCHAR(100) NULL;
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID('dbo.Movimentacao')
          AND name = 'IX_Movimentacao_LocacaoEspera_Pendente'
    )
    BEGIN
        CREATE NONCLUSTERED INDEX IX_Movimentacao_LocacaoEspera_Pendente
            ON dbo.Movimentacao (LocacaoEspera, FinalizadoEm)
            INCLUDE (ItemNr, QtdOrigem, LocacaoOrigem, CriadoEm);
    END;

    /* Estoque permanece consolidado em uma linha por filial e item. */
    IF EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID('dbo.Estoque')
          AND name = 'UX_Estoque_FilialId_ItemNr_Locacao'
    )
    BEGIN
        DROP INDEX UX_Estoque_FilialId_ItemNr_Locacao ON dbo.Estoque;
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID('dbo.Estoque')
          AND name = 'UX_Estoque_FilialId_ItemNr'
    )
    BEGIN
        CREATE UNIQUE NONCLUSTERED INDEX UX_Estoque_FilialId_ItemNr
            ON dbo.Estoque (FilialId, ItemNr);
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;

SELECT
    c.name AS Coluna,
    TYPE_NAME(c.user_type_id) AS Tipo,
    c.max_length AS TamanhoBytes,
    c.is_nullable AS PermiteNulo
FROM sys.columns c
WHERE c.object_id = OBJECT_ID('dbo.Movimentacao')
  AND c.name = 'LocacaoEspera';

SELECT
    i.name AS Indice,
    i.type_desc AS Tipo
FROM sys.indexes i
WHERE (i.object_id = OBJECT_ID('dbo.Movimentacao')
       AND i.name = 'IX_Movimentacao_LocacaoEspera_Pendente')
   OR (i.object_id = OBJECT_ID('dbo.Estoque')
       AND i.name = 'UX_Estoque_FilialId_ItemNr');
