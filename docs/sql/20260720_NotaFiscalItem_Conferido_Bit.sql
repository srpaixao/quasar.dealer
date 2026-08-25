SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

/*
    Corrige o tipo da coluna dbo.NotaFiscalItem.Conferido.

    Regra de conversao:
      0    = nao conferido
      <> 0 = conferido
      NULL = nao conferido

    O script pode ser executado novamente. Se a coluna ja estiver como
    BIT NOT NULL, apenas garante a existencia do DEFAULT (0).
*/

IF OBJECT_ID(N'dbo.NotaFiscalItem', N'U') IS NULL
BEGIN
    THROW 50020, 'A tabela dbo.NotaFiscalItem nao foi encontrada.', 1;
END;
GO

IF COL_LENGTH(N'dbo.NotaFiscalItem', N'Conferido') IS NULL
BEGIN
    THROW 50021, 'A coluna dbo.NotaFiscalItem.Conferido nao foi encontrada.', 1;
END;
GO

DECLARE
    @TipoAtual SYSNAME,
    @Precisao TINYINT,
    @Escala TINYINT,
    @PermiteNulo BIT;

SELECT
    @TipoAtual = Tipo.name,
    @Precisao = Coluna.precision,
    @Escala = Coluna.scale,
    @PermiteNulo = Coluna.is_nullable
FROM sys.columns Coluna
INNER JOIN sys.types Tipo
    ON Tipo.user_type_id = Coluna.user_type_id
WHERE Coluna.object_id = OBJECT_ID(N'dbo.NotaFiscalItem')
  AND Coluna.name = N'Conferido';

IF @TipoAtual NOT IN (N'decimal', N'numeric', N'bit')
BEGIN
    THROW 50022, 'O tipo atual de NotaFiscalItem.Conferido nao permite esta migracao automatica.', 1;
END;

IF @TipoAtual IN (N'decimal', N'numeric')
   AND (@Precisao <> 15 OR @Escala <> 3)
BEGIN
    THROW 50023, 'A coluna Conferido nao esta definida como decimal(15,3). A migracao foi cancelada.', 1;
END;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE
        @DefaultNome SYSNAME,
        @Comando NVARCHAR(MAX);

    SELECT @DefaultNome = Padrao.name
    FROM sys.default_constraints Padrao
    INNER JOIN sys.columns Coluna
        ON Coluna.object_id = Padrao.parent_object_id
       AND Coluna.column_id = Padrao.parent_column_id
    WHERE Padrao.parent_object_id = OBJECT_ID(N'dbo.NotaFiscalItem')
      AND Coluna.name = N'Conferido';

    IF @TipoAtual IN (N'decimal', N'numeric')
    BEGIN
        IF COL_LENGTH(N'dbo.NotaFiscalItem', N'Conferido_MigracaoBit') IS NOT NULL
        BEGIN
            THROW 50025, 'A coluna temporaria Conferido_MigracaoBit ja existe. A migracao foi cancelada.', 1;
        END;

        /*
           Os comandos sao executados separadamente para que o SQL Server
           recompile o lote depois da criacao da coluna temporaria.
        */
        SET @Comando = N'ALTER TABLE dbo.NotaFiscalItem '
            + N'ADD Conferido_MigracaoBit BIT NULL;';
        EXEC sys.sp_executesql @Comando;

        SET @Comando = N'UPDATE dbo.NotaFiscalItem '
            + N'SET Conferido_MigracaoBit = CASE '
            + N'WHEN ISNULL(Conferido, 0) = 0 THEN 0 ELSE 1 END;';
        EXEC sys.sp_executesql @Comando;

        SET @Comando = N'ALTER TABLE dbo.NotaFiscalItem '
            + N'ALTER COLUMN Conferido_MigracaoBit BIT NOT NULL;';
        EXEC sys.sp_executesql @Comando;

        IF @DefaultNome IS NOT NULL
        BEGIN
            SET @Comando = N'ALTER TABLE dbo.NotaFiscalItem DROP CONSTRAINT '
                + QUOTENAME(@DefaultNome) + N';';
            EXEC sys.sp_executesql @Comando;
        END;

        SET @Comando = N'ALTER TABLE dbo.NotaFiscalItem '
            + N'DROP COLUMN Conferido;';
        EXEC sys.sp_executesql @Comando;

        EXEC sys.sp_rename
            N'dbo.NotaFiscalItem.Conferido_MigracaoBit',
            N'Conferido',
            N'COLUMN';
    END
    ELSE IF @PermiteNulo = 1
    BEGIN
        UPDATE dbo.NotaFiscalItem
        SET Conferido = 0
        WHERE Conferido IS NULL;

        ALTER TABLE dbo.NotaFiscalItem
            ALTER COLUMN Conferido BIT NOT NULL;
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.default_constraints Padrao
        INNER JOIN sys.columns Coluna
            ON Coluna.object_id = Padrao.parent_object_id
           AND Coluna.column_id = Padrao.parent_column_id
        WHERE Padrao.parent_object_id = OBJECT_ID(N'dbo.NotaFiscalItem')
          AND Coluna.name = N'Conferido'
    )
    BEGIN
        SET @Comando = N'ALTER TABLE dbo.NotaFiscalItem '
            + N'ADD CONSTRAINT DF_NotaFiscalItem_Conferido '
            + N'DEFAULT (0) FOR Conferido;';
        EXEC sys.sp_executesql @Comando;
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.columns Coluna
        INNER JOIN sys.types Tipo
            ON Tipo.user_type_id = Coluna.user_type_id
        WHERE Coluna.object_id = OBJECT_ID(N'dbo.NotaFiscalItem')
          AND Coluna.name = N'Conferido'
          AND Tipo.name = N'bit'
          AND Coluna.is_nullable = 0
    )
    BEGIN
        THROW 50024, 'Nao foi possivel alterar NotaFiscalItem.Conferido para BIT NOT NULL.', 1;
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
    Tipo.name AS Tipo,
    Coluna.max_length AS Tamanho,
    Coluna.is_nullable AS PermiteNulo,
    Padrao.name AS DefaultConstraint,
    Padrao.definition AS DefaultValor
FROM sys.columns Coluna
INNER JOIN sys.types Tipo
    ON Tipo.user_type_id = Coluna.user_type_id
LEFT JOIN sys.default_constraints Padrao
    ON Padrao.parent_object_id = Coluna.object_id
   AND Padrao.parent_column_id = Coluna.column_id
WHERE Coluna.object_id = OBJECT_ID(N'dbo.NotaFiscalItem')
  AND Coluna.name = N'Conferido';
GO
