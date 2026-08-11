SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

/* Histórico operacional da importação. As locações continuam sendo gravadas
   exclusivamente na tabela dbo.Locacao. */
IF OBJECT_ID('dbo.LocacaoImportacao', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.LocacaoImportacao
    (
        Id               INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_LocacaoImportacao PRIMARY KEY,
        Arquivo          VARCHAR(260) NOT NULL,
        Usuario          VARCHAR(100) NOT NULL,
        CriadoEm         DATETIME NOT NULL,
        FilialId         INT NOT NULL,
        QtdeLinhas       INT NOT NULL,
        QtdePrevistas    INT NOT NULL,
        QtdeCriadas      INT NOT NULL,
        QtdeExistentes   INT NOT NULL,
        QtdeErros        INT NOT NULL
    );

    CREATE INDEX IX_LocacaoImportacao_Filial_CriadoEm
        ON dbo.LocacaoImportacao (FilialId, CriadoEm DESC);
END;

/* Cadastro -> Locações. O item existente é reaproveitado quando possível. */
DECLARE @CadastroId INT;
SELECT TOP (1) @CadastroId = Id
FROM dbo.AppMenu
WHERE Area = 'EstoqueApp'
  AND Nivel = 1
  AND UPPER(LTRIM(RTRIM(Titulo))) IN ('CADASTRO', 'CADASTROS')
ORDER BY Id;

IF @CadastroId IS NULL
BEGIN
    INSERT INTO dbo.AppMenu
        (Titulo, Area, Controller, Action, Css, Status, Sequencia, Nivel, IdNivelSup, HasChild, DatUltAtlz, FilialId)
    VALUES
        ('Cadastro', 'EstoqueApp', 'Home', 'Index', 'fa fa-folder-open', 1, 10, 1, NULL, 1, GETDATE(), NULL);

    SET @CadastroId = SCOPE_IDENTITY();
END
ELSE
BEGIN
    UPDATE dbo.AppMenu
       SET HasChild = 1,
           Status = 1,
           DatUltAtlz = GETDATE()
     WHERE Id = @CadastroId;
END;

DECLARE @LocacaoMenuId INT;
SELECT TOP (1) @LocacaoMenuId = Id
FROM dbo.AppMenu
WHERE Area = 'EstoqueApp'
  AND Controller = 'Locacao'
  AND Action = 'Index'
ORDER BY Id;

IF @LocacaoMenuId IS NULL
BEGIN
    INSERT INTO dbo.AppMenu
        (Titulo, Area, Controller, Action, Css, Status, Sequencia, Nivel, IdNivelSup, HasChild, DatUltAtlz, FilialId)
    VALUES
        ('Locações', 'EstoqueApp', 'Locacao', 'Index', 'fa fa-map-marker', 1, 1, 2, @CadastroId, 0, GETDATE(), NULL);
END
ELSE
BEGIN
    UPDATE dbo.AppMenu
       SET Titulo = 'Locações',
           IdNivelSup = @CadastroId,
           Nivel = 2,
           Status = 1,
           HasChild = 0,
           DatUltAtlz = GETDATE()
     WHERE Id = @LocacaoMenuId;
END;

COMMIT TRANSACTION;
