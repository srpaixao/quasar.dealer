SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    IF EXISTS (SELECT 1 FROM dbo.Estoque WHERE FilialId IS NULL)
    BEGIN
        THROW 50001, 'Existem registros em Estoque sem FilialId. Regularize-os antes de criar o indice.', 1;
    END;

    IF EXISTS
    (
        SELECT 1
        FROM dbo.Estoque
        GROUP BY FilialId, ItemNr
        HAVING COUNT(*) > 1
    )
    BEGIN
        SELECT FilialId, ItemNr, COUNT(*) AS Quantidade
        FROM dbo.Estoque
        GROUP BY FilialId, ItemNr
        HAVING COUNT(*) > 1
        ORDER BY FilialId, ItemNr;

        THROW 50002, 'Existem registros duplicados em Estoque para FilialId e ItemNr.', 1;
    END;

    IF EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.Estoque')
          AND name = N'UX_Estoque_FilialId_ItemNr'
          AND is_unique = 0
    )
    BEGIN
        THROW 50003, 'O indice UX_Estoque_FilialId_ItemNr existe, mas nao e unico.', 1;
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.Estoque')
          AND name = N'UX_Estoque_FilialId_ItemNr'
    )
    BEGIN
        CREATE UNIQUE INDEX UX_Estoque_FilialId_ItemNr
            ON dbo.Estoque (FilialId, ItemNr);
    END;

    DECLARE @InsertEstoqueUpload VARCHAR(MAX) = '
DELETE FROM dbo.EstoqueUpload_APOLLO
WHERE FilialId = @filial;

INSERT INTO dbo.EstoqueUpload_APOLLO
(
    Item,
    Descricao,
    QtdContabil,
    QtdPedida,
    QtdOrcada,
    QtdTransito,
    QtdDisponivel,
    Local,
    CustoMedio,
    DemandaMedia,
    Dtatual,
    FilialId
)
SELECT
    LTRIM(RTRIM(Item)),
    LTRIM(RTRIM(Descricao)),
    ISNULL(TRY_CONVERT(INT, REPLACE(LTRIM(RTRIM(QtdContabil)), ''.'', '''')), 0),
    ISNULL(TRY_CONVERT(INT, REPLACE(LTRIM(RTRIM(QtdPedida)), ''.'', '''')), 0),
    ISNULL(TRY_CONVERT(INT, REPLACE(LTRIM(RTRIM(QtdOrcada)), ''.'', '''')), 0),
    ISNULL(TRY_CONVERT(INT, REPLACE(LTRIM(RTRIM(QtdTransito)), ''.'', '''')), 0),
    ISNULL(TRY_CONVERT(INT, REPLACE(LTRIM(RTRIM(QtdDisponivel)), ''.'', '''')), 0),
    REPLACE(REPLACE(LTRIM(RTRIM(Local)), ''  '', '' ''), ''  '', '' ''),
    ISNULL(TRY_CONVERT(DECIMAL(15,2), REPLACE(REPLACE(LTRIM(RTRIM(CustoMedio)), ''.'', ''''), '','', ''.'')), 0),
    ISNULL(TRY_CONVERT(DECIMAL(15,2), REPLACE(REPLACE(LTRIM(RTRIM(DemandaMedia)), ''.'', ''''), '','', ''.'')), 0),
    GETDATE(),
    @filial
FROM
(
    SELECT
        Item = JSON_VALUE(S, ''$[0]''),
        Descricao = JSON_VALUE(S, ''$[1]''),
        QtdContabil = JSON_VALUE(S, ''$[2]''),
        QtdPedida = JSON_VALUE(S, ''$[3]''),
        QtdOrcada = JSON_VALUE(S, ''$[4]''),
        QtdTransito = JSON_VALUE(S, ''$[5]''),
        QtdDisponivel = JSON_VALUE(S, ''$[6]''),
        Local = JSON_VALUE(S, ''$[7]''),
        CustoMedio = JSON_VALUE(S, ''$[8]''),
        DemandaMedia = JSON_VALUE(S, ''$[9]'')
    FROM dbo.EstoqueUpload A
    CROSS APPLY
    (
        VALUES (''[""'' + REPLACE(STRING_ESCAPE(A.Linha, ''json''), '';'', ''"",""'') + ''""]'')
    ) B(S)
    WHERE A.FilialId = @filial
) Dados
WHERE NULLIF(LTRIM(RTRIM(Item)), '''') IS NOT NULL
  AND Item <> ''ITEM_ESTOQUE_PUB'';

;WITH Duplicados AS
(
    SELECT
        Id,
        ROW_NUMBER() OVER
        (
            PARTITION BY FilialId, Item
            ORDER BY Id
        ) AS NumeroLinha
    FROM dbo.EstoqueUpload_APOLLO
    WHERE FilialId = @filial
)
DELETE FROM Duplicados WHERE NumeroLinha > 1;';

    DECLARE @UpdateEstoque VARCHAR(MAX) = '
MERGE dbo.Estoque WITH (HOLDLOCK) AS Destino
USING
(
    SELECT
        Item,
        Local,
        QtdDisponivel,
        Dtatual,
        FilialId
    FROM dbo.EstoqueUpload_APOLLO
    WHERE FilialId = @filial
) AS Origem
ON Destino.ItemNr = Origem.Item
AND Destino.FilialId = Origem.FilialId
WHEN MATCHED THEN
    UPDATE SET
        Destino.Locacao = CASE
            WHEN EXISTS
            (
                SELECT 1
                FROM dbo.AppConfig
                WHERE Nome = ''LimparLocacaoSaldoZero''
                  AND FilialId = @filial
                  AND UPPER(LTRIM(RTRIM(ISNULL(Valor, '''')))) IN (''TRUE'', ''1'', ''SIM'', ''S'')
            )
            AND Origem.QtdDisponivel = 0 THEN NULL
            ELSE Origem.Local
        END,
        Destino.Saldo = Origem.QtdDisponivel,
        Destino.ModificadoPor = ''@usuario_sistema'',
        Destino.ModificadoEm = Origem.Dtatual
WHEN NOT MATCHED BY TARGET THEN
    INSERT
    (
        Locacao,
        ItemNr,
        Saldo,
        Indisponivel,
        PedidoPendente,
        ValorEstoque,
        Range,
        CriadoPor,
        CriadoEm,
        FilialId
    )
    VALUES
    (
        CASE
            WHEN EXISTS
            (
                SELECT 1
                FROM dbo.AppConfig
                WHERE Nome = ''LimparLocacaoSaldoZero''
                  AND FilialId = @filial
                  AND UPPER(LTRIM(RTRIM(ISNULL(Valor, '''')))) IN (''TRUE'', ''1'', ''SIM'', ''S'')
            )
            AND Origem.QtdDisponivel = 0 THEN NULL
            ELSE Origem.Local
        END,
        Origem.Item,
        Origem.QtdDisponivel,
        NULL,
        NULL,
        NULL,
        NULL,
        ''@usuario_sistema'',
        Origem.Dtatual,
        Origem.FilialId
    );';

    DECLARE @UpdateMaterial VARCHAR(MAX) = '
MERGE dbo.Material WITH (HOLDLOCK) AS Destino
USING
(
    SELECT Item, Descricao
    FROM dbo.EstoqueUpload_APOLLO
    WHERE FilialId = @filial
) AS Origem
ON Destino.Codigo = Origem.Item
WHEN NOT MATCHED BY TARGET THEN
    INSERT
    (
        Codigo, Descricao, UN, EmbalagemMin, MediaVendas, CustoUnitario,
        Curva, ItemCritico, ObsItemCritico, CriadoPor, CriadoEm, FilialId
    )
    VALUES
    (
        Origem.Item, Origem.Descricao, '''', NULL, NULL,
        NULL, ''N'', 0, NULL, ''@usuario_sistema'', GETDATE(), @filial
    );';

    DECLARE @UpdateLocacao VARCHAR(MAX) = '
IF EXISTS
(
    SELECT 1
    FROM dbo.EstoqueUpload_APOLLO Origem
    INNER JOIN dbo.Locacao Destino ON Destino.Codigo = Origem.Local
    WHERE Origem.FilialId = @filial
      AND NULLIF(Origem.Local, '''') IS NOT NULL
      AND ISNULL(Destino.FilialId, -1) <> @filial
)
    THROW 50013, ''Existe locacao do arquivo cadastrada em outra filial.'', 1;

MERGE dbo.Locacao WITH (HOLDLOCK) AS Destino
USING
(
    SELECT DISTINCT Local AS Codigo
    FROM dbo.EstoqueUpload_APOLLO
    WHERE FilialId = @filial
      AND NULLIF(Local, '''') IS NOT NULL
) AS Origem
ON Destino.Codigo = Origem.Codigo
AND Destino.FilialId = @filial
WHEN NOT MATCHED BY TARGET THEN
    INSERT
    (
        Codigo, Tipo, Descricao, Bloqueado, AreaId, EquipamentoId,
        Curva, Estrategia, Observacoes, CriadoPor, CriadoEm, FilialId, ZonaId
    )
    VALUES
    (
        Origem.Codigo, ''P'', '''', 0, NULL, NULL,
        NULL, NULL, NULL, ''@usuario_sistema'', GETDATE(), @filial, NULL
    );';

    DECLARE @InsertId INT;

    IF (SELECT COUNT(*) FROM dbo.AppSQL WHERE Nome IN ('INSERT_EstoqueUpload', 'INSERT_EstoqueUpload_APOLLO')) > 1
    BEGIN
        THROW 50004, 'Existe mais de um comando AppSQL para inserir o estoque APOLLO.', 1;
    END;

    SELECT TOP (1) @InsertId = Id
    FROM dbo.AppSQL
    WHERE Nome IN ('INSERT_EstoqueUpload', 'INSERT_EstoqueUpload_APOLLO')
    ORDER BY CASE WHEN Nome = 'INSERT_EstoqueUpload' THEN 0 ELSE 1 END, Id;

    IF @InsertId IS NULL
    BEGIN
        INSERT INTO dbo.AppSQL (Nome, Comando)
        VALUES ('INSERT_EstoqueUpload', @InsertEstoqueUpload);
    END
    ELSE
    BEGIN
        UPDATE dbo.AppSQL
        SET Nome = 'INSERT_EstoqueUpload',
            Comando = @InsertEstoqueUpload
        WHERE Id = @InsertId;
    END;

    DECLARE @UpdateId INT;

    IF (SELECT COUNT(*) FROM dbo.AppSQL WHERE Nome IN ('UPDATE_Estoque', 'UPDATE_Estoque_From_APOLLO')) > 1
    BEGIN
        THROW 50005, 'Existe mais de um comando AppSQL para atualizar o estoque APOLLO.', 1;
    END;

    SELECT TOP (1) @UpdateId = Id
    FROM dbo.AppSQL
    WHERE Nome IN ('UPDATE_Estoque', 'UPDATE_Estoque_From_APOLLO')
    ORDER BY CASE WHEN Nome = 'UPDATE_Estoque' THEN 0 ELSE 1 END, Id;

    IF @UpdateId IS NULL
    BEGIN
        INSERT INTO dbo.AppSQL (Nome, Comando)
        VALUES ('UPDATE_Estoque', @UpdateEstoque);
    END
    ELSE
    BEGIN
        UPDATE dbo.AppSQL
        SET Nome = 'UPDATE_Estoque',
            Comando = @UpdateEstoque
        WHERE Id = @UpdateId;
    END;

    IF (SELECT COUNT(*) FROM dbo.AppSQL WHERE Nome = 'UPDATE_Material_From_APOLLO') > 1
    BEGIN
        THROW 50007, 'Existe mais de um comando AppSQL para atualizar Material APOLLO.', 1;
    END;

    IF EXISTS (SELECT 1 FROM dbo.AppSQL WHERE Nome = 'UPDATE_Material_From_APOLLO')
    BEGIN
        UPDATE dbo.AppSQL
        SET Comando = @UpdateMaterial
        WHERE Nome = 'UPDATE_Material_From_APOLLO';
    END
    ELSE
    BEGIN
        INSERT INTO dbo.AppSQL (Nome, Comando)
        VALUES ('UPDATE_Material_From_APOLLO', @UpdateMaterial);
    END;

    IF (SELECT COUNT(*) FROM dbo.AppSQL WHERE Nome = 'UPDATE_Locacao_From_APOLLO') > 1
    BEGIN
        THROW 50008, 'Existe mais de um comando AppSQL para atualizar Locacao APOLLO.', 1;
    END;

    IF EXISTS (SELECT 1 FROM dbo.AppSQL WHERE Nome = 'UPDATE_Locacao_From_APOLLO')
    BEGIN
        UPDATE dbo.AppSQL
        SET Comando = @UpdateLocacao
        WHERE Nome = 'UPDATE_Locacao_From_APOLLO';
    END
    ELSE
    BEGIN
        INSERT INTO dbo.AppSQL (Nome, Comando)
        VALUES ('UPDATE_Locacao_From_APOLLO', @UpdateLocacao);
    END;

    IF EXISTS
    (
        SELECT Nome
        FROM dbo.AppSQL
        WHERE Nome IN
        (
            'INSERT_EstoqueUpload',
            'INSERT_EstoqueUpload_APOLLO',
            'UPDATE_Estoque',
            'UPDATE_Estoque_From_APOLLO',
            'UPDATE_Material_From_APOLLO',
            'UPDATE_Locacao_From_APOLLO'
        )
        GROUP BY Nome
        HAVING COUNT(*) > 1
    )
    BEGIN
        THROW 50006, 'Existem comandos AppSQL duplicados para o fluxo de estoque APOLLO.', 1;
    END;

    UPDATE dbo.AppSQL
    SET Comando = '/* Desativado: o upload APOLLO nao pode excluir registros de Estoque. */ SELECT 0;'
    WHERE Nome = 'UPDATE_ItemSemEstoque_From_APOLLO';

    INSERT INTO dbo.AppConfig
    (
        Nome, Descricao, Valor, CriadoPor, CriadoEm, FilialId
    )
    SELECT
        'LimparLocacaoSaldoZero',
        'No upload de estoque, limpar a locacao do item quando o saldo for zero.',
        'false',
        'SYSTEM',
        GETDATE(),
        Filiais.FilialId
    FROM
    (
        SELECT DISTINCT FilialId
        FROM dbo.AppConfig
        WHERE Nome = 'DMS'
          AND FilialId IS NOT NULL
    ) Filiais
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.AppConfig Existente
        WHERE Existente.Nome = 'LimparLocacaoSaldoZero'
          AND Existente.FilialId = Filiais.FilialId
    );

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;
GO

SELECT name, is_unique
FROM sys.indexes
WHERE object_id = OBJECT_ID(N'dbo.Estoque')
  AND name = N'UX_Estoque_FilialId_ItemNr';

SELECT Id, Nome
FROM dbo.AppSQL
WHERE Nome IN
(
    'INSERT_EstoqueUpload',
    'UPDATE_Estoque',
    'UPDATE_Material_From_APOLLO',
    'UPDATE_Locacao_From_APOLLO'
)
ORDER BY Nome;
GO
