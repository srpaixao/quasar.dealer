SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

/*
   O sistema grava horarios operacionais no fuso de Brasilia/Bahia (UTC-03:00).
   Este script:
   1. elimina GETDATE() dos comandos dinamicos da AppSQL;
   2. corrige somente datas historicas cuja origem pode ser identificada com
      seguranca como o antigo importador SQL de expedicao;
   3. lista defaults de banco que ainda dependem do relogio local do SQL Server.

   A diferenca e calculada no momento da execucao. Assim, nenhum valor fixo de
   quatro horas e aplicado quando o servidor SQL ja estiver no horario correto.
*/
DECLARE @AgoraServidor datetime = GETDATE();
DECLARE @AgoraUtc datetime = GETUTCDATE();
DECLARE @AgoraOperacional datetime = DATEADD(HOUR, -3, GETUTCDATE());
DECLARE @AjusteMinutos int = CONVERT
(
    int,
    ROUND(CONVERT(decimal(10, 4), DATEDIFF(MINUTE, @AgoraServidor, @AgoraOperacional)) / 60, 0)
) * 60;

IF ABS(@AjusteMinutos) > 840
    THROW 50001, 'Diferenca de horario superior a 14 horas. Revise o fuso do servidor antes de continuar.', 1;

SELECT
    @AgoraServidor AS HorarioServidorSQL,
    @AgoraUtc AS HorarioUTC,
    @AgoraOperacional AS HorarioOperacional,
    @AjusteMinutos AS AjusteCalculadoMinutos;

BEGIN TRY
    BEGIN TRANSACTION;

    /*
       Util.FormatSQL substitui @data_sistema pelo horario calculado pela
       aplicacao. A atualizacao protege tambem consumidores dos comandos AppSQL
       que ainda utilizem as definicoes armazenadas no banco.
    */
    UPDATE dbo.AppSQL
    SET Comando = REPLACE
    (
        Comando,
        'GETDATE()',
        'CONVERT(datetime, ''@data_sistema'', 120)'
    )
    WHERE Comando LIKE '%GETDATE()%';

    DECLARE @Marcador varchar(100) = 'MigracaoFusoHorarioExpedicao_20260722';
    DECLARE @DocumentosCorrigidos int = 0;
    DECLARE @HistoricosCorrigidos int = 0;

    IF @AjusteMinutos <> 0
       AND NOT EXISTS
       (
           SELECT 1
           FROM dbo.AppConfig
           WHERE Nome = @Marcador
             AND FilialId IS NULL
       )
    BEGIN
        SELECT
            Historico.Id,
            Historico.DocExpedicaoId,
            Historico.DataHora
        INTO #HistoricoInicialImportado
        FROM dbo.HistoricoDocExpedicao Historico
        WHERE Historico.HistoricoId = 1
          AND Historico.DataHora IS NOT NULL
          AND Historico.DataHora <= @AgoraServidor;

        /*
           CriadoEm somente e alterado quando coincide com o historico inicial.
           Essa coincidencia identifica o par gravado pelo importador antigo e
           evita alterar documentos cadastrados manualmente pela aplicacao Web.
        */
        UPDATE Documento
        SET Documento.CriadoEm = DATEADD(MINUTE, @AjusteMinutos, Documento.CriadoEm)
        FROM dbo.DocExpedicao Documento
        INNER JOIN #HistoricoInicialImportado Historico
            ON Historico.DocExpedicaoId = Documento.Id
        WHERE Documento.CriadoEm IS NOT NULL
          AND ABS(DATEDIFF(SECOND, Documento.CriadoEm, Historico.DataHora)) <= 60;

        SET @DocumentosCorrigidos = @@ROWCOUNT;

        UPDATE Historico
        SET Historico.DataHora = DATEADD(MINUTE, @AjusteMinutos, Historico.DataHora)
        FROM dbo.HistoricoDocExpedicao Historico
        INNER JOIN #HistoricoInicialImportado Importado
            ON Importado.Id = Historico.Id;

        SET @HistoricosCorrigidos = @@ROWCOUNT;

        INSERT INTO dbo.AppConfig
        (
            Nome,
            Descricao,
            Valor,
            CriadoPor,
            CriadoEm,
            FilialId
        )
        VALUES
        (
            @Marcador,
            'Controle da correcao de datas do importador de expedicao.',
            CONVERT(varchar(12), @AjusteMinutos),
            'SYSTEM',
            @AgoraOperacional,
            NULL
        );
    END;

    COMMIT TRANSACTION;

    SELECT
        @DocumentosCorrigidos AS DocExpedicaoCorrigidos,
        @HistoricosCorrigidos AS HistoricoDocExpedicaoCorrigidos;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;
GO

/*
   Auditoria: defaults abaixo ainda usam o relogio do SQL Server. Eles nao sao
   alterados automaticamente porque podem pertencer a integracoes que trabalham
   deliberadamente em UTC.
*/
SELECT
    OBJECT_SCHEMA_NAME(Coluna.object_id) AS Esquema,
    OBJECT_NAME(Coluna.object_id) AS Tabela,
    Coluna.name AS Coluna,
    Padrao.definition AS DefinicaoDefault
FROM sys.columns Coluna
INNER JOIN sys.default_constraints Padrao
    ON Padrao.parent_object_id = Coluna.object_id
   AND Padrao.parent_column_id = Coluna.column_id
WHERE Padrao.definition LIKE '%GETDATE%'
   OR Padrao.definition LIKE '%CURRENT_TIMESTAMP%'
   OR Padrao.definition LIKE '%SYSDATETIME%'
ORDER BY Esquema, Tabela, Coluna;
GO
