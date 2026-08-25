SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

/* Permite executar novamente o script na mesma janela/sessao do SQL Server. */
IF OBJECT_ID(N'tempdb..#NotaFiscalItemDuplicado') IS NOT NULL
    DROP TABLE #NotaFiscalItemDuplicado;

IF OBJECT_ID(N'tempdb..#NotaFiscalDuplicada') IS NOT NULL
    DROP TABLE #NotaFiscalDuplicada;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    /*
       Para processos que nao sao devolucao, uma NF somente pode repetir o numero
       quando a filial ou o movimento forem diferentes. Devolucoes (TipoId = 2)
       podem repetir NF e item.
    */
    SELECT
        Duplicada.Id AS IdDuplicado,
        Principal.Id AS IdPrincipal
    INTO #NotaFiscalDuplicada
    FROM dbo.NotaFiscal Duplicada
    CROSS APPLY
    (
        SELECT MIN(Candidata.Id) AS Id
        FROM dbo.NotaFiscal Candidata
        WHERE ISNULL(Candidata.FilialId, -2147483648) = ISNULL(Duplicada.FilialId, -2147483648)
          AND Candidata.Movimento = Duplicada.Movimento
          AND Candidata.Numero = Duplicada.Numero
          AND Candidata.TipoId <> 2
    ) Principal
    WHERE Duplicada.TipoId <> 2
      AND Duplicada.Id <> Principal.Id;

    IF OBJECT_ID(N'dbo.DevolucaoComplemento', N'U') IS NOT NULL
    BEGIN
        UPDATE Complemento
        SET Complemento.NotaFiscalId = Mapa.IdPrincipal
        FROM dbo.DevolucaoComplemento Complemento
        INNER JOIN #NotaFiscalDuplicada Mapa
            ON Mapa.IdDuplicado = Complemento.NotaFiscalId;
    END;

    UPDATE Item
    SET Item.NotaFiscalId = Mapa.IdPrincipal
    FROM dbo.NotaFiscalItem Item
    INNER JOIN #NotaFiscalDuplicada Mapa
        ON Mapa.IdDuplicado = Item.NotaFiscalId;

    DELETE Nota
    FROM dbo.NotaFiscal Nota
    INNER JOIN #NotaFiscalDuplicada Mapa
        ON Mapa.IdDuplicado = Nota.Id;

    /*
       A chave do item e a mesma usada pelo upload: NF, item, volume, pedido e
       filial. NULL e texto vazio representam a mesma ausencia no arquivo.
    */
    SELECT
        Duplicado.Id AS IdDuplicado,
        Principal.Id AS IdPrincipal
    INTO #NotaFiscalItemDuplicado
    FROM dbo.NotaFiscalItem Duplicado
    INNER JOIN dbo.NotaFiscal Nota
        ON Nota.Id = Duplicado.NotaFiscalId
       AND Nota.TipoId <> 2
    CROSS APPLY
    (
        SELECT MIN(Candidato.Id) AS Id
        FROM dbo.NotaFiscalItem Candidato
        WHERE Candidato.NotaFiscalId = Duplicado.NotaFiscalId
          AND Candidato.Item = Duplicado.Item
          AND ISNULL(Candidato.Volume, '') = ISNULL(Duplicado.Volume, '')
          AND ISNULL(Candidato.Pedido, '') = ISNULL(Duplicado.Pedido, '')
          AND ISNULL(Candidato.FilialId, -2147483648) = ISNULL(Duplicado.FilialId, -2147483648)
    ) Principal
    WHERE Duplicado.Id <> Principal.Id;

    IF OBJECT_ID(N'dbo.AnomaliaItem', N'U') IS NOT NULL
    BEGIN
        UPDATE Anomalia
        SET Anomalia.NotaFiscalItemId = Mapa.IdPrincipal
        FROM dbo.AnomaliaItem Anomalia
        INNER JOIN #NotaFiscalItemDuplicado Mapa
            ON Mapa.IdDuplicado = Anomalia.NotaFiscalItemId;
    END;

    DELETE Item
    FROM dbo.NotaFiscalItem Item
    INNER JOIN #NotaFiscalItemDuplicado Mapa
        ON Mapa.IdDuplicado = Item.Id;

    IF EXISTS
    (
        SELECT 1
        FROM dbo.NotaFiscal
        WHERE TipoId <> 2
        GROUP BY FilialId, Movimento, Numero
        HAVING COUNT(*) > 1
    )
    BEGIN
        THROW 50001, 'Ainda existem NFs repetidas para a mesma filial, movimento e numero.', 1;
    END;

    IF EXISTS
    (
        SELECT 1
        FROM dbo.NotaFiscalItem Item
        INNER JOIN dbo.NotaFiscal Nota
            ON Nota.Id = Item.NotaFiscalId
           AND Nota.TipoId <> 2
        GROUP BY Item.NotaFiscalId, Item.Item, ISNULL(Item.Volume, ''), ISNULL(Item.Pedido, ''), Item.FilialId
        HAVING COUNT(*) > 1
    )
    BEGIN
        THROW 50002, 'Ainda existem itens repetidos para a mesma NF, item, volume, pedido e filial.', 1;
    END;

    IF EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.NotaFiscal')
          AND name = N'UX_NotaFiscal_FilialId_Movimento_Numero'
    )
    BEGIN
        DROP INDEX UX_NotaFiscal_FilialId_Movimento_Numero
            ON dbo.NotaFiscal;
    END;

    CREATE UNIQUE INDEX UX_NotaFiscal_FilialId_Movimento_Numero
        ON dbo.NotaFiscal (FilialId, Movimento, Numero)
        WHERE TipoId <> 2;

    /*
       NotaFiscalItem nao recebe indice unico porque o tipo esta na tabela pai e
       itens repetidos sao validos em devolucoes. O MERGE e a transacao serializada
       do upload continuam garantindo a idempotencia dos itens de transito.
    */
    IF EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.NotaFiscalItem')
          AND name = N'UX_NotaFiscalItem_Nota_Item_Volume_Pedido_Filial'
    )
    BEGIN
        DROP INDEX UX_NotaFiscalItem_Nota_Item_Volume_Pedido_Filial
            ON dbo.NotaFiscalItem;
    END;

    /* Corrige a chave do MERGE do upload para respeitar tambem o movimento. */
    DECLARE @ComandoNotaFiscal VARCHAR(MAX) =
    (
        SELECT Comando
        FROM dbo.AppSQL
        WHERE Nome = 'INSERT_MERGE_NotaFiscal'
    );

    IF @ComandoNotaFiscal IS NULL
    BEGIN
        THROW 50003, 'Comando INSERT_MERGE_NotaFiscal nao encontrado na AppSQL.', 1;
    END;

    IF @ComandoNotaFiscal NOT LIKE '%Destino.Movimento%'
    BEGIN
        SET @ComandoNotaFiscal = REPLACE
        (
            @ComandoNotaFiscal,
            'AND Destino.FilialId = Origem.FilialId',
            'AND Destino.FilialId = Origem.FilialId' + CHAR(13) + CHAR(10) + 'AND Destino.Movimento = ''E'''
        );

        IF @ComandoNotaFiscal NOT LIKE '%Destino.Movimento%'
        BEGIN
            THROW 50004, 'Nao foi possivel incluir Movimento na chave do MERGE de NotaFiscal.', 1;
        END;

        UPDATE dbo.AppSQL
        SET Comando = @ComandoNotaFiscal
        WHERE Nome = 'INSERT_MERGE_NotaFiscal';
    END;

    DROP TABLE #NotaFiscalItemDuplicado;
    DROP TABLE #NotaFiscalDuplicada;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;

    IF OBJECT_ID(N'tempdb..#NotaFiscalItemDuplicado') IS NOT NULL
        DROP TABLE #NotaFiscalItemDuplicado;

    IF OBJECT_ID(N'tempdb..#NotaFiscalDuplicada') IS NOT NULL
        DROP TABLE #NotaFiscalDuplicada;

    THROW;
END CATCH;
GO

SELECT Movimento, Numero, FilialId, COUNT(*) AS Qtde
FROM dbo.NotaFiscal
WHERE TipoId <> 2
GROUP BY Movimento, Numero, FilialId
HAVING COUNT(*) > 1;

SELECT Item.NotaFiscalId, Item.Item, ISNULL(Item.Volume, '') AS Volume, ISNULL(Item.Pedido, '') AS Pedido, Item.FilialId, COUNT(*) AS Qtde
FROM dbo.NotaFiscalItem Item
INNER JOIN dbo.NotaFiscal Nota
    ON Nota.Id = Item.NotaFiscalId
   AND Nota.TipoId <> 2
GROUP BY Item.NotaFiscalId, Item.Item, ISNULL(Item.Volume, ''), ISNULL(Item.Pedido, ''), Item.FilialId
HAVING COUNT(*) > 1;
GO
