SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    IF NOT EXISTS (SELECT 1 FROM dbo.AppSQL WHERE Nome = 'INSERT_MERGE_DocExpedicao')
        THROW 50001, 'Comando INSERT_MERGE_DocExpedicao nao encontrado na AppSQL.', 1;

    IF NOT EXISTS (SELECT 1 FROM dbo.AppSQL WHERE Nome = 'INSERT_Historico_DocExpedicao')
        THROW 50002, 'Comando INSERT_Historico_DocExpedicao nao encontrado na AppSQL.', 1;

    /* A filial do documento pai e a referencia segura para o historico. */
    UPDATE Historico
    SET Historico.FilialId = Documento.FilialId
    FROM dbo.HistoricoDocExpedicao Historico
    INNER JOIN dbo.DocExpedicao Documento
        ON Documento.Id = Historico.DocExpedicaoId
    WHERE Documento.FilialId IS NOT NULL
      AND
      (
          Historico.FilialId IS NULL
          OR Historico.FilialId <> Documento.FilialId
      );

    UPDATE dbo.AppSQL
    SET Comando = N'MERGE [dbo].[DocExpedicao] AS Destino
USING
(
    SELECT
        LTRIM(RTRIM(NUMERO_NOTA_FISCAL)) AS NUMERO_NOTA_FISCAL,
        CAST(SUBSTRING(DTA_ENTRADA_SAIDA, 7, 4) + ''-'' + SUBSTRING(DTA_ENTRADA_SAIDA, 4, 2) + ''-'' + SUBSTRING(DTA_ENTRADA_SAIDA, 1, 2) AS DATE) AS [Data],
        NOME_DEPARTAMENTO,
        CONTATO,
        NOME_VENDEDOR,
        CLIENTE,
        NOME_CLIENTE,
        CIDADE,
        ESTADO,
        1 AS StatusId,
        0 AS RoteiroImpresso,
        ''_usuario_'' AS CriadoPor,
        CONVERT(datetime, ''@data_sistema'', 120) AS CriadoEm,
        @filial AS FilialId
    FROM dbo.DocExpedicaoUpload_APOLLO
) AS Origem
ON
(
    RIGHT(CONCAT(''000000000'', Origem.NUMERO_NOTA_FISCAL), 9) = RIGHT(CONCAT(''000000000'', Destino.Numero), 9)
    AND Origem.[Data] = Destino.DataEmissao
    AND (Destino.FilialId = Origem.FilialId OR Destino.FilialId IS NULL)
)
WHEN MATCHED THEN
    UPDATE SET
        Destino.Numero = RIGHT(CONCAT(''000000000'', Origem.NUMERO_NOTA_FISCAL), 9),
        Destino.Classificacao = Origem.NOME_DEPARTAMENTO,
        Destino.Controle = Origem.CONTATO,
        Destino.Vendedor = Origem.NOME_VENDEDOR,
        Destino.CodigoCliente = Origem.CLIENTE,
        Destino.NomeCliente = Origem.NOME_CLIENTE,
        Destino.Cidade = Origem.CIDADE,
        Destino.Estado = Origem.ESTADO,
        Destino.FilialId = Origem.FilialId,
        Destino.ModificadoPor = ''_usuario_'',
        Destino.ModificadoEm = CONVERT(datetime, ''@data_sistema'', 120)
WHEN NOT MATCHED THEN
    INSERT
    (
        Numero, DataEmissao, Classificacao, Controle, Vendedor,
        CodigoCliente, NomeCliente, Cidade, Estado, StatusId,
        RoteiroImpresso, CriadoPor, CriadoEm, FilialId
    )
    VALUES
    (
        RIGHT(CONCAT(''000000000'', Origem.NUMERO_NOTA_FISCAL), 9),
        Origem.[Data], Origem.NOME_DEPARTAMENTO, Origem.CONTATO,
        Origem.NOME_VENDEDOR, Origem.CLIENTE, Origem.NOME_CLIENTE,
        Origem.CIDADE, Origem.ESTADO, Origem.StatusId,
        Origem.RoteiroImpresso, Origem.CriadoPor, Origem.CriadoEm,
        Origem.FilialId
    );

DELETE FROM dbo.DocExpedicao
WHERE FilialId = @filial
  AND Classificacao LIKE ''%MERCADO LIVRE%'';

DELETE FROM dbo.DocExpedicao
WHERE FilialId = @filial
  AND StatusId = 1
  AND CriadoEm < DATEADD(DAY, -1, CONVERT(datetime, ''@data_sistema'', 120));'
    WHERE Nome = 'INSERT_MERGE_DocExpedicao';

    UPDATE dbo.AppSQL
    SET Comando = N'INSERT INTO dbo.HistoricoDocExpedicao
(
    DocExpedicaoId,
    HistoricoId,
    Observacoes,
    DataHora,
    Usuario,
    FilialId
)
SELECT
    Documento.Id,
    1,
    NULL,
    CONVERT(datetime, ''@data_sistema'', 120),
    ''_usuario_'',
    @filial
FROM dbo.DocExpedicao Documento
WHERE Documento.FilialId = @filial
  AND NOT EXISTS
  (
      SELECT 1
      FROM dbo.HistoricoDocExpedicao Historico
      WHERE Historico.DocExpedicaoId = Documento.Id
        AND Historico.HistoricoId = 1
        AND Historico.FilialId = @filial
  );'
    WHERE Nome = 'INSERT_Historico_DocExpedicao';

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;
GO

/*
   Auditoria: registros legados sem filial nao sao corrigidos automaticamente,
   porque a unidade correta nao pode ser inferida com seguranca pelo script.
*/
SELECT
    Id,
    Numero,
    DataEmissao,
    Controle,
    TransportadoraId,
    QtdVolumes,
    CriadoEm,
    CriadoPor
FROM dbo.DocExpedicao
WHERE FilialId IS NULL
ORDER BY CriadoEm DESC, Id DESC;
GO
