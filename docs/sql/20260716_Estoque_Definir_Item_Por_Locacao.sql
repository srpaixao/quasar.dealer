SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Estoque')
      AND name = N'IX_Estoque_FilialId_Locacao'
)
BEGIN
    CREATE INDEX IX_Estoque_FilialId_Locacao
        ON dbo.Estoque (FilialId, Locacao);
END;

DECLARE @MenuPaiId INT = 23;
DECLARE @MenuId INT;
DECLARE @Titulo NVARCHAR(100) = N'Definir Item por Loca' + NCHAR(231) + NCHAR(227) + N'o';

SELECT @MenuId = Id
FROM dbo.AppMenu
WHERE IdNivelSup = @MenuPaiId
  AND Area = 'EstoqueApp'
  AND Controller = 'AssociacaoLocacao'
  AND Action = 'Index';

IF @MenuId IS NULL
BEGIN
    INSERT INTO dbo.AppMenu
    (
        Titulo, Area, Controller, Action, Css, Status,
        Sequencia, Nivel, IdNivelSup, HasChild, DatUltAtlz
    )
    VALUES
    (
        @Titulo,
        'EstoqueApp',
        'AssociacaoLocacao',
        'Index',
        'fa-solid fa-map-location-dot',
        1,
        270,
        2,
        @MenuPaiId,
        0,
        GETDATE()
    );
END
ELSE
BEGIN
    UPDATE dbo.AppMenu
    SET Titulo = @Titulo,
        Css = 'fa-solid fa-map-location-dot',
        Status = 1,
        Sequencia = 270,
        Nivel = 2,
        IdNivelSup = @MenuPaiId,
        HasChild = 0,
        DatUltAtlz = GETDATE()
    WHERE Id = @MenuId;
END;

COMMIT TRANSACTION;
GO

SELECT Id, Titulo, Area, Controller, Action, Sequencia, IdNivelSup, Status
FROM dbo.AppMenu
WHERE Area = 'EstoqueApp'
  AND Controller = 'AssociacaoLocacao'
  AND Action = 'Index';
GO
