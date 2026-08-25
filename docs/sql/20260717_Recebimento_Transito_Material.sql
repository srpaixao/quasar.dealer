SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @ComandoTransito VARCHAR(MAX) = '
DELETE dbo.TransitoUploadColumns
WHERE FilialId = @filial;

INSERT INTO dbo.TransitoUploadColumns
(
    RecordType,
    NotaFiscal,
    Origem,
    Emissao,
    Volume,
    Item,
    Pedido,
    Quantidade,
    Dtatual,
    FilialId
)
SELECT
    SUBSTRING(Linha, 1, 3),
    CASE SUBSTRING(Linha, 1, 3) WHEN ''DNC'' THEN LTRIM(RTRIM(SUBSTRING(Linha, 24, 9))) ELSE '''' END,
    CASE SUBSTRING(Linha, 1, 3) WHEN ''DNC'' THEN LTRIM(RTRIM(SUBSTRING(Linha, 115, 4))) ELSE '''' END,
    CASE SUBSTRING(Linha, 1, 3) WHEN ''DNC'' THEN LTRIM(RTRIM(SUBSTRING(Linha, 37, 8))) ELSE '''' END,
    CASE SUBSTRING(Linha, 1, 3) WHEN ''DNI'' THEN LTRIM(RTRIM(SUBSTRING(Linha, 62, 10))) ELSE '''' END,
    CASE SUBSTRING(Linha, 1, 3) WHEN ''DNI'' THEN LTRIM(RTRIM(SUBSTRING(Linha, 4, 8))) ELSE '''' END,
    CASE SUBSTRING(Linha, 1, 3) WHEN ''DNI'' THEN LTRIM(RTRIM(SUBSTRING(Linha, 15, 6))) ELSE '''' END,
    CASE SUBSTRING(Linha, 1, 3) WHEN ''DNI'' THEN LTRIM(RTRIM(SUBSTRING(Linha, 22, 5))) ELSE '''' END,
    ''@data_sistema'',
    @filial
FROM dbo.TransitoUpload
WHERE SUBSTRING(Linha, 1, 3) IN (''DNC'', ''DNI'')
  AND FilialId = @filial;';

    IF (SELECT COUNT(*) FROM dbo.AppSQL WHERE Nome = 'INSERT_TransitoUploadColumns') > 1
    BEGIN
        THROW 50002, 'Existe mais de um comando INSERT_TransitoUploadColumns na AppSQL.', 1;
    END;

    IF EXISTS (SELECT 1 FROM dbo.AppSQL WHERE Nome = 'INSERT_TransitoUploadColumns')
    BEGIN
        UPDATE dbo.AppSQL
        SET Comando = @ComandoTransito
        WHERE Nome = 'INSERT_TransitoUploadColumns';
    END
    ELSE
    BEGIN
        INSERT INTO dbo.AppSQL (Nome, Comando)
        VALUES ('INSERT_TransitoUploadColumns', @ComandoTransito);
    END;

    DECLARE @ComandoAtualizaTransito VARCHAR(MAX) = '
UPDATE Item
SET
    Item.NotaFiscal = Cabecalho.NotaFiscal,
    Item.Origem = Cabecalho.Origem,
    Item.Emissao = Cabecalho.Emissao
FROM dbo.TransitoUploadColumns Item
CROSS APPLY
(
    SELECT TOP (1)
        Header.NotaFiscal,
        Header.Origem,
        Header.Emissao
    FROM dbo.TransitoUploadColumns Header
    WHERE Header.RecordType = ''DNC''
      AND Header.FilialId = Item.FilialId
      AND Header.Id < Item.Id
    ORDER BY Header.Id DESC
) Cabecalho
WHERE Item.RecordType = ''DNI''
  AND Item.FilialId = @filial;';

    IF (SELECT COUNT(*) FROM dbo.AppSQL WHERE Nome = 'UPDATE_TransitoUploadColumns') > 1
    BEGIN
        THROW 50005, 'Existe mais de um comando UPDATE_TransitoUploadColumns na AppSQL.', 1;
    END;

    IF EXISTS (SELECT 1 FROM dbo.AppSQL WHERE Nome = 'UPDATE_TransitoUploadColumns')
    BEGIN
        UPDATE dbo.AppSQL
        SET Comando = @ComandoAtualizaTransito
        WHERE Nome = 'UPDATE_TransitoUploadColumns';
    END
    ELSE
    BEGIN
        INSERT INTO dbo.AppSQL (Nome, Comando)
        VALUES ('UPDATE_TransitoUploadColumns', @ComandoAtualizaTransito);
    END;

    DECLARE @Comando VARCHAR(MAX) = '
INSERT INTO dbo.Material
(
    Codigo,
    Descricao,
    UN,
    EmbalagemMin,
    MediaVendas,
    CustoUnitario,
    Curva,
    ItemCritico,
    ObsItemCritico,
    CriadoPor,
    CriadoEm,
    FilialId,
    CategoriaProduto
)
SELECT DISTINCT
    LTRIM(RTRIM(T.Item)),
    '''',
    '''',
    NULL,
    NULL,
    NULL,
    ''N'',
    0,
    NULL,
    ''@usuario_sistema'',
    ''@data_sistema'',
    @filial,
    ''Diretos''
FROM dbo.TransitoUploadColumns T
WHERE NULLIF(LTRIM(RTRIM(T.Item)), '''') IS NOT NULL
  AND T.FilialId = @filial
  AND NOT EXISTS
  (
      SELECT 1
      FROM dbo.Material M
      WHERE M.Codigo = LTRIM(RTRIM(T.Item))
  );';

    IF (SELECT COUNT(*) FROM dbo.AppSQL WHERE Nome = 'INSERT_Material_From_Transito') > 1
    BEGIN
        THROW 50001, 'Existe mais de um comando INSERT_Material_From_Transito na AppSQL.', 1;
    END;

    IF EXISTS (SELECT 1 FROM dbo.AppSQL WHERE Nome = 'INSERT_Material_From_Transito')
    BEGIN
        UPDATE dbo.AppSQL
        SET Comando = @Comando
        WHERE Nome = 'INSERT_Material_From_Transito';
    END
    ELSE
    BEGIN
        INSERT INTO dbo.AppSQL (Nome, Comando)
        VALUES ('INSERT_Material_From_Transito', @Comando);
    END;

    DECLARE @ComandoNotaFiscal VARCHAR(MAX) = '
;WITH Cabecalhos AS
(
    SELECT
        T.NotaFiscal,
        T.Origem,
        T.Emissao,
        T.FilialId,
        ROW_NUMBER() OVER
        (
            PARTITION BY T.FilialId, LTRIM(RTRIM(T.NotaFiscal))
            ORDER BY T.Id DESC
        ) AS NumeroLinha
    FROM dbo.TransitoUploadColumns T
    WHERE T.RecordType = ''DNC''
      AND T.FilialId = @filial
      AND NULLIF(LTRIM(RTRIM(T.NotaFiscal)), '''') IS NOT NULL
),
OrigemUnica AS
(
    SELECT
        LTRIM(RTRIM(NotaFiscal)) AS NotaFiscal,
        Origem,
        Emissao,
        FilialId
    FROM Cabecalhos
    WHERE NumeroLinha = 1
)
MERGE dbo.NotaFiscal WITH (HOLDLOCK) AS Destino
USING OrigemUnica AS Origem
ON Destino.Numero = Origem.NotaFiscal
AND Destino.FilialId = Origem.FilialId
AND Destino.Movimento = ''E''
WHEN MATCHED THEN
    UPDATE SET
        Destino.DataEmissao = TRY_CAST(Origem.Emissao AS DATE),
        Destino.ModificadoEm = ''@data_sistema'',
        Destino.ModificadoPor = ''@usuario_sistema''
WHEN NOT MATCHED BY TARGET THEN
    INSERT
    (
        Movimento,
        TipoId,
        StatusId,
        Numero,
        Serie,
        Emissor,
        DataEmissao,
        Valor,
        Descricao,
        Observacoes,
        Danfe,
        RecebidoAdmEm,
        RecebidoAdmPor,
        CriadoPor,
        CriadoEm,
        ModificadoPor,
        ModificadoEm,
        FilialId
    )
    VALUES
    (
        ''E'',
        4,
        1,
        Origem.NotaFiscal,
        NULL,
        NULL,
        TRY_CAST(Origem.Emissao AS DATE),
        0,
        NULL,
        Origem.Origem,
        NULL,
        NULL,
        NULL,
        ''@usuario_sistema'',
        ''@data_sistema'',
        NULL,
        NULL,
        Origem.FilialId
    );';

    IF (SELECT COUNT(*) FROM dbo.AppSQL WHERE Nome = 'INSERT_MERGE_NotaFiscal') > 1
    BEGIN
        THROW 50003, 'Existe mais de um comando INSERT_MERGE_NotaFiscal na AppSQL.', 1;
    END;

    IF EXISTS (SELECT 1 FROM dbo.AppSQL WHERE Nome = 'INSERT_MERGE_NotaFiscal')
    BEGIN
        UPDATE dbo.AppSQL
        SET Comando = @ComandoNotaFiscal
        WHERE Nome = 'INSERT_MERGE_NotaFiscal';
    END
    ELSE
    BEGIN
        INSERT INTO dbo.AppSQL (Nome, Comando)
        VALUES ('INSERT_MERGE_NotaFiscal', @ComandoNotaFiscal);
    END;

    DECLARE @ComandoNotaFiscalItem VARCHAR(MAX) = '
;WITH OrigemAgrupada AS
(
    SELECT
        NF.Id AS NotaFiscalId,
        LTRIM(RTRIM(T.Item)) AS Item,
        SUM(T.Quantidade) AS Quantidade,
        T.Volume,
        T.Pedido,
        T.FilialId
    FROM dbo.NotaFiscal NF
    INNER JOIN dbo.TransitoUploadColumns T
        ON NF.Numero = T.NotaFiscal
       AND NF.FilialId = T.FilialId
    WHERE T.RecordType = ''DNI''
      AND T.FilialId = @filial
      AND NULLIF(LTRIM(RTRIM(T.Item)), '''') IS NOT NULL
    GROUP BY
        NF.Id,
        LTRIM(RTRIM(T.Item)),
        T.Volume,
        T.Pedido,
        T.FilialId
)
MERGE dbo.NotaFiscalItem WITH (HOLDLOCK) AS Destino
USING OrigemAgrupada AS Origem
ON Destino.NotaFiscalId = Origem.NotaFiscalId
AND Destino.Item = Origem.Item
AND ISNULL(Destino.Volume, '''') = ISNULL(Origem.Volume, '''')
AND ISNULL(Destino.Pedido, '''') = ISNULL(Origem.Pedido, '''')
AND Destino.FilialId = Origem.FilialId
WHEN MATCHED THEN
    UPDATE SET
        Destino.Quantidade = Origem.Quantidade,
        Destino.Volume = Origem.Volume,
        Destino.ModificadoEm = ''@data_sistema'',
        Destino.ModificadoPor = ''@usuario_sistema''
WHEN NOT MATCHED BY TARGET THEN
    INSERT
    (
        NotaFiscalId,
        Item,
        Quantidade,
        QtdArmazenada,
        Volume,
        Pedido,
        StatusId,
        Observacao,
        CriadoPor,
        CriadoEm,
        ModificadoPor,
        ModificadoEm,
        FilialId
    )
    VALUES
    (
        Origem.NotaFiscalId,
        Origem.Item,
        Origem.Quantidade,
        NULL,
        Origem.Volume,
        Origem.Pedido,
        1,
        NULL,
        ''@usuario_sistema'',
        ''@data_sistema'',
        NULL,
        NULL,
        Origem.FilialId
    );';

    IF (SELECT COUNT(*) FROM dbo.AppSQL WHERE Nome = 'INSERT_MERGE_NotaFiscalItem') > 1
    BEGIN
        THROW 50004, 'Existe mais de um comando INSERT_MERGE_NotaFiscalItem na AppSQL.', 1;
    END;

    IF EXISTS (SELECT 1 FROM dbo.AppSQL WHERE Nome = 'INSERT_MERGE_NotaFiscalItem')
    BEGIN
        UPDATE dbo.AppSQL
        SET Comando = @ComandoNotaFiscalItem
        WHERE Nome = 'INSERT_MERGE_NotaFiscalItem';
    END
    ELSE
    BEGIN
        INSERT INTO dbo.AppSQL (Nome, Comando)
        VALUES ('INSERT_MERGE_NotaFiscalItem', @ComandoNotaFiscalItem);
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;
GO

SELECT Id, Nome, Comando
FROM dbo.AppSQL
WHERE Nome IN
(
    'INSERT_TransitoUploadColumns',
    'UPDATE_TransitoUploadColumns',
    'INSERT_Material_From_Transito',
    'INSERT_MERGE_NotaFiscal',
    'INSERT_MERGE_NotaFiscalItem'
)
ORDER BY Nome;
GO
