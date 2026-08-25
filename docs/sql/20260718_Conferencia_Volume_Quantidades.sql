SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    IF COL_LENGTH(N'dbo.NotaFiscalItem', N'QtdConferida') IS NULL
        ALTER TABLE dbo.NotaFiscalItem ADD QtdConferida DECIMAL(15,3) NULL;

    /* QtdArmazenada ja existe nas bases atuais; a verificacao torna o script seguro em bases antigas. */
    IF COL_LENGTH(N'dbo.NotaFiscalItem', N'QtdArmazenada') IS NULL
        ALTER TABLE dbo.NotaFiscalItem ADD QtdArmazenada DECIMAL(15,3) NULL;

    IF COL_LENGTH(N'dbo.NotaFiscalItem', N'Conferido') IS NULL
    BEGIN
        ALTER TABLE dbo.NotaFiscalItem
            ADD Conferido BIT NOT NULL
                CONSTRAINT DF_NotaFiscalItem_Conferido DEFAULT (0) WITH VALUES;
    END;

    IF COL_LENGTH(N'dbo.NotaFiscalItem', N'UsuarioConferencia') IS NULL
        ALTER TABLE dbo.NotaFiscalItem ADD UsuarioConferencia VARCHAR(100) NULL;

    IF COL_LENGTH(N'dbo.NotaFiscalItem', N'DtHrConferencia') IS NULL
        ALTER TABLE dbo.NotaFiscalItem ADD DtHrConferencia DATETIME NULL;

    IF COL_LENGTH(N'dbo.NotaFiscalItem', N'UsuarioArmazenagem') IS NULL
        ALTER TABLE dbo.NotaFiscalItem ADD UsuarioArmazenagem VARCHAR(100) NULL;

    IF COL_LENGTH(N'dbo.NotaFiscalItem', N'DtHrArmazenagem') IS NULL
        ALTER TABLE dbo.NotaFiscalItem ADD DtHrArmazenagem DATETIME NULL;

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.check_constraints
        WHERE parent_object_id = OBJECT_ID(N'dbo.NotaFiscalItem')
          AND name = N'CK_NotaFiscalItem_QtdConferida_NaoNegativa'
    )
    BEGIN
        /* SQL dinamico evita a compilacao antecipada quando a coluna acabou de ser criada neste lote. */
        EXEC
        (
            N'ALTER TABLE dbo.NotaFiscalItem WITH CHECK
              ADD CONSTRAINT CK_NotaFiscalItem_QtdConferida_NaoNegativa
                  CHECK (QtdConferida IS NULL OR QtdConferida >= 0);'
        );
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;
GO

SELECT
    COL_LENGTH(N'dbo.NotaFiscalItem', N'QtdConferida') AS QtdConferida,
    COL_LENGTH(N'dbo.NotaFiscalItem', N'QtdArmazenada') AS QtdArmazenada,
    COL_LENGTH(N'dbo.NotaFiscalItem', N'Conferido') AS Conferido,
    COL_LENGTH(N'dbo.NotaFiscalItem', N'UsuarioConferencia') AS UsuarioConferencia,
    COL_LENGTH(N'dbo.NotaFiscalItem', N'DtHrConferencia') AS DtHrConferencia,
    COL_LENGTH(N'dbo.NotaFiscalItem', N'UsuarioArmazenagem') AS UsuarioArmazenagem,
    COL_LENGTH(N'dbo.NotaFiscalItem', N'DtHrArmazenagem') AS DtHrArmazenagem;
GO
