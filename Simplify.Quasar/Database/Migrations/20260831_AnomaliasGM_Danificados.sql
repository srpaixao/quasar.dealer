/*
    QUASAR.DEALER - Anomalias GM - dados específicos do formulário Danificados.

    AMBIENTE DE TESTES EXCLUSIVAMENTE.
    Para executar conscientemente no banco de testes, altere a variável abaixo para 1.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @ConfirmarAmbienteDeTestes bit = 0;
IF @ConfirmarAmbienteDeTestes <> 1
    THROW 51000, 'Execucao bloqueada: confirme explicitamente o ambiente de TESTES no script.', 1;

BEGIN TRANSACTION;

IF COL_LENGTH('dbo.AnomaliaGmItem', 'InstaladoVeiculo') IS NULL
BEGIN
    ALTER TABLE dbo.AnomaliaGmItem ADD InstaladoVeiculo bit NULL;
END;

IF COL_LENGTH('dbo.AnomaliaGmItem', 'CondicaoEmbalagem') IS NULL
BEGIN
    ALTER TABLE dbo.AnomaliaGmItem ADD CondicaoEmbalagem varchar(500) NULL;
END;

COMMIT TRANSACTION;
