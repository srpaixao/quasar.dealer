/*
    Corrige notas fiscais GM importadas pelo arquivo de trânsito.

    Regra:
    - o código de origem do registro DNC (por exemplo, F089) pertence a Emissor;
    - Observacoes só é limpa quando contém exclusivamente esse mesmo código;
    - somente códigos cadastrados em OrigemNotaFiscal são migrados.

    O script é idempotente e mantém uma cópia dos valores anteriores.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID('dbo.BackupNotaFiscalEmissor_20260729', 'U') IS NULL
    BEGIN
        SELECT TOP (0)
            CONVERT(int, NF.Id) AS Id,
            NF.Numero,
            NF.FilialId,
            NF.Emissor AS EmissorAnterior,
            NF.Observacoes AS ObservacoesAnterior,
            CAST(SYSDATETIME() AS datetime2(7)) AS BackupEm
        INTO dbo.BackupNotaFiscalEmissor_20260729
        FROM dbo.NotaFiscal AS NF;

        CREATE UNIQUE INDEX UX_BackupNotaFiscalEmissor_20260729_Id
            ON dbo.BackupNotaFiscalEmissor_20260729 (Id);
    END;

    CREATE TABLE #NotasCorrigir
    (
        Id int NOT NULL PRIMARY KEY,
        Emissor varchar(255) NOT NULL
    );

    INSERT INTO #NotasCorrigir (Id, Emissor)
    SELECT
        NF.Id,
        LTRIM(RTRIM(NF.Observacoes))
    FROM dbo.NotaFiscal AS NF
    WHERE NF.TipoId = 4
      AND NULLIF(LTRIM(RTRIM(NF.Emissor)), '') IS NULL
      AND NULLIF(LTRIM(RTRIM(NF.Observacoes)), '') IS NOT NULL
      AND EXISTS
      (
          SELECT 1
          FROM dbo.OrigemNotaFiscal AS Origem
          WHERE LTRIM(RTRIM(Origem.Codigo)) = LTRIM(RTRIM(NF.Observacoes))
            AND (Origem.FilialId = NF.FilialId OR Origem.FilialId IS NULL)
      );

    INSERT INTO dbo.BackupNotaFiscalEmissor_20260729
    (
        Id,
        Numero,
        FilialId,
        EmissorAnterior,
        ObservacoesAnterior,
        BackupEm
    )
    SELECT
        NF.Id,
        NF.Numero,
        NF.FilialId,
        NF.Emissor,
        NF.Observacoes,
        SYSDATETIME()
    FROM dbo.NotaFiscal AS NF
    INNER JOIN #NotasCorrigir AS Corrigir
        ON Corrigir.Id = NF.Id
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.BackupNotaFiscalEmissor_20260729 AS Copia
        WHERE Copia.Id = NF.Id
    );

    UPDATE NF
    SET
        NF.Emissor = Corrigir.Emissor,
        NF.Observacoes = CASE
            WHEN LTRIM(RTRIM(ISNULL(NF.Observacoes, ''))) = Corrigir.Emissor
                THEN NULL
            ELSE NF.Observacoes
        END
    FROM dbo.NotaFiscal AS NF
    INNER JOIN #NotasCorrigir AS Corrigir
        ON Corrigir.Id = NF.Id;

    SELECT
        NF.Id,
        NF.Numero,
        NF.FilialId,
        NF.Emissor,
        NF.Observacoes
    FROM dbo.NotaFiscal AS NF
    INNER JOIN #NotasCorrigir AS Corrigir
        ON Corrigir.Id = NF.Id
    ORDER BY NF.Id;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;
