SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

/* Indices das grades operacionais com paginacao executada no SQL Server. */

IF OBJECT_ID(N'dbo.NotaFiscal', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.NotaFiscal') AND name = N'IX_NotaFiscal_Filial_Tipo_CriadoEm')
BEGIN
    CREATE NONCLUSTERED INDEX IX_NotaFiscal_Filial_Tipo_CriadoEm
        ON dbo.NotaFiscal (FilialId, TipoId, CriadoEm DESC)
        INCLUDE (Id, Numero, StatusId, Emissor, Movimento, ModificadoEm, ModificadoPor, RecebidoAdmEm);
END;
GO

IF OBJECT_ID(N'dbo.NotaFiscal', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.NotaFiscal') AND name = N'IX_NotaFiscal_ADM_Pendente')
BEGIN
    CREATE NONCLUSTERED INDEX IX_NotaFiscal_ADM_Pendente
        ON dbo.NotaFiscal (FilialId, Numero)
        INCLUDE (Id, TipoId, StatusId, Emissor, CriadoEm, ModificadoEm)
        WHERE RecebidoAdmEm IS NULL;
END;
GO

IF OBJECT_ID(N'dbo.NotaFiscalItem', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.NotaFiscalItem') AND name = N'IX_NotaFiscalItem_Filial_Status_CriadoEm')
BEGIN
    CREATE NONCLUSTERED INDEX IX_NotaFiscalItem_Filial_Status_CriadoEm
        ON dbo.NotaFiscalItem (FilialId, StatusId, CriadoEm DESC)
        INCLUDE (Id, NotaFiscalId, Item, Volume, Quantidade, CriadoPor, Conferido, QtdConferida, ModificadoEm);
END;
GO

IF OBJECT_ID(N'dbo.NotaFiscalItem', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.NotaFiscalItem') AND name = N'IX_NotaFiscalItem_NotaFiscalId_Filial')
BEGIN
    CREATE NONCLUSTERED INDEX IX_NotaFiscalItem_NotaFiscalId_Filial
        ON dbo.NotaFiscalItem (NotaFiscalId, FilialId)
        INCLUDE (Item, Volume, Quantidade, StatusId, CriadoEm);
END;
GO

IF OBJECT_ID(N'dbo.Volume', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Volume') AND name = N'IX_Volume_Filial_Area_VolumeNr')
BEGIN
    CREATE NONCLUSTERED INDEX IX_Volume_Filial_Area_VolumeNr
        ON dbo.Volume (FilialId, AreaId, VolumeNr)
        INCLUDE (NotaFiscalNr, StatusId, QtdItens, CriadoEm);
END;
GO

IF OBJECT_ID(N'dbo.DocExpedicao', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.DocExpedicao') AND name = N'IX_DocExpedicao_Filial_CriadoEm')
BEGIN
    CREATE NONCLUSTERED INDEX IX_DocExpedicao_Filial_CriadoEm
        ON dbo.DocExpedicao (FilialId, CriadoEm DESC)
        INCLUDE (Id, Numero, DataEmissao, StatusId, Movimento, TipoMovimentoId, TransportadoraId, QtdVolumes, ModificadoEm, ModificadoPor);
END;
GO

IF OBJECT_ID(N'dbo.Estoque', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Estoque') AND name = N'IX_Estoque_Filial_ItemNr')
BEGIN
    CREATE NONCLUSTERED INDEX IX_Estoque_Filial_ItemNr
        ON dbo.Estoque (FilialId, ItemNr)
        INCLUDE (Id, Locacao, Saldo, Indisponivel, PedidoPendente, ValorEstoque, [Range], ModificadoEm);
END;
GO

IF OBJECT_ID(N'dbo.Locacao', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Locacao') AND name = N'IX_Locacao_Filial_Codigo')
BEGIN
    CREATE NONCLUSTERED INDEX IX_Locacao_Filial_Codigo
        ON dbo.Locacao (FilialId, Codigo)
        INCLUDE (Tipo, Descricao, Bloqueado, AreaId, Curva, Observacoes);
END;
GO

IF OBJECT_ID(N'dbo.RetornoInterno', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.RetornoInterno') AND name = N'IX_RetornoInterno_Filial_Id')
BEGIN
    CREATE NONCLUSTERED INDEX IX_RetornoInterno_Filial_Id
        ON dbo.RetornoInterno (FilialId, Id DESC)
        INCLUDE (NrDocumento, TipoDocumentoRetornoId, LocalOrigemId, LocalDestinoId, Responsavel, FinalizadoEm);
END;
GO

IF OBJECT_ID(N'dbo.RetornoInternoItem', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.RetornoInternoItem') AND name = N'IX_RetornoInternoItem_RetornoInternoId')
BEGIN
    CREATE NONCLUSTERED INDEX IX_RetornoInternoItem_RetornoInternoId
        ON dbo.RetornoInternoItem (RetornoInternoId)
        INCLUDE (QtdArmazenada);
END;
GO

SELECT
    OBJECT_NAME(Indice.object_id) AS Tabela,
    Indice.name AS Indice,
    Indice.is_disabled AS Desabilitado
FROM sys.indexes Indice
WHERE Indice.name IN
(
    N'IX_NotaFiscal_Filial_Tipo_CriadoEm',
    N'IX_NotaFiscal_ADM_Pendente',
    N'IX_NotaFiscalItem_Filial_Status_CriadoEm',
    N'IX_NotaFiscalItem_NotaFiscalId_Filial',
    N'IX_Volume_Filial_Area_VolumeNr',
    N'IX_DocExpedicao_Filial_CriadoEm',
    N'IX_Estoque_Filial_ItemNr',
    N'IX_Locacao_Filial_Codigo',
    N'IX_RetornoInterno_Filial_Id',
    N'IX_RetornoInternoItem_RetornoInternoId'
)
ORDER BY Tabela, Indice;
GO
