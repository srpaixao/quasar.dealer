SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    IF COL_LENGTH('dbo.Material', 'QtdePadrao3') IS NULL
    BEGIN
        ALTER TABLE dbo.Material ADD QtdePadrao3 INT NULL;
    END;

    IF COL_LENGTH('dbo.Material', 'Zona3Id') IS NULL
    BEGIN
        ALTER TABLE dbo.Material ADD Zona3Id INT NULL;
    END;

    IF COL_LENGTH('dbo.Material', 'Eqpto3Id') IS NULL
    BEGIN
        ALTER TABLE dbo.Material ADD Eqpto3Id INT NULL;
    END;

    IF COL_LENGTH('dbo.Material', 'Comp') IS NULL
    BEGIN
        ALTER TABLE dbo.Material ADD Comp DECIMAL(15,2) NULL;
    END;

    IF COL_LENGTH('dbo.Material', 'Larg') IS NULL
    BEGIN
        ALTER TABLE dbo.Material ADD Larg DECIMAL(15,2) NULL;
    END;

    IF COL_LENGTH('dbo.Material', 'Altu') IS NULL
    BEGIN
        ALTER TABLE dbo.Material ADD Altu DECIMAL(15,2) NULL;
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
    BEGIN
        ROLLBACK TRANSACTION;
    END;

    THROW;
END CATCH;
GO

SELECT
    c.name AS Coluna,
    t.name AS Tipo,
    c.is_nullable AS PermiteNulo
FROM sys.columns c
INNER JOIN sys.types t
    ON t.user_type_id = c.user_type_id
WHERE c.object_id = OBJECT_ID(N'dbo.Material')
  AND c.name IN
  (
      'QtdePadrao3',
      'Zona3Id',
      'Eqpto3Id',
      'Comp',
      'Larg',
      'Altu'
  )
ORDER BY c.column_id;
GO
