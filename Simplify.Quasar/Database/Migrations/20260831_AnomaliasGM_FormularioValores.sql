/*
    QUASAR.DEALER - Anomalias GM - valores do Trânsito GM e formulário oficial.

    AMBIENTE DE TESTES EXCLUSIVAMENTE.
    Para executar conscientemente no banco de testes, altere a variável abaixo para 1.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @ConfirmarAmbienteDeTestes bit = 0;
IF @ConfirmarAmbienteDeTestes <> 1
    THROW 51000, 'Execucao bloqueada: confirme explicitamente o ambiente de TESTES no script.', 1;

BEGIN TRANSACTION;

IF COL_LENGTH('dbo.Empresa', 'CodigoGM') IS NULL
BEGIN
    ALTER TABLE dbo.Empresa ADD CodigoGM varchar(50) NULL;
END;

IF COL_LENGTH('dbo.TransitoUploadColumns', 'PrecoUnitario') IS NULL
BEGIN
    ALTER TABLE dbo.TransitoUploadColumns ADD PrecoUnitario decimal(18,2) NULL;
END;

IF COL_LENGTH('dbo.TransitoUploadColumns', 'Imposto') IS NULL
BEGIN
    ALTER TABLE dbo.TransitoUploadColumns ADD Imposto decimal(18,2) NULL;
END;

IF COL_LENGTH('dbo.NotaFiscalItem', 'PrecoUnitario') IS NULL
BEGIN
    ALTER TABLE dbo.NotaFiscalItem ADD PrecoUnitario decimal(18,2) NULL;
END;

IF COL_LENGTH('dbo.NotaFiscalItem', 'Imposto') IS NULL
BEGIN
    ALTER TABLE dbo.NotaFiscalItem ADD Imposto decimal(18,2) NULL;
END;

IF EXISTS
(
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.AnomaliaGmArquivo')
      AND name = 'TipoAnomalia'
      AND max_length < 10
)
BEGIN
    ALTER TABLE dbo.AnomaliaGmArquivo ALTER COLUMN TipoAnomalia varchar(10) NOT NULL;
END;

COMMIT TRANSACTION;
