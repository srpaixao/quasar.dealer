SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

/*
   Suporta o filtro obrigatorio por filial e PeriodoRecebimento da tela
   Recebimento Historico. As colunas exibidas ficam cobertas pelo indice,
   reduzindo leituras da tabela durante contagem, ordenacao e paginacao.
*/

IF OBJECT_ID(N'dbo.HistoricoRecebimento', N'U') IS NULL
BEGIN
    THROW 50030, 'A tabela dbo.HistoricoRecebimento nao foi encontrada.', 1;
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.HistoricoRecebimento')
      AND name = N'IX_HistoricoRecebimento_FilialId_DataHora'
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_HistoricoRecebimento_FilialId_DataHora
        ON dbo.HistoricoRecebimento (FilialId, DataHora DESC)
        INCLUDE
        (
            CodMaterial,
            DescMaterial,
            Curva,
            CodLocacao,
            NroVolume,
            Quantidade,
            Usuario
        );
END;
GO

SELECT
    Indice.name AS Indice,
    Indice.type_desc AS Tipo,
    Indice.is_disabled AS Desabilitado
FROM sys.indexes Indice
WHERE Indice.object_id = OBJECT_ID(N'dbo.HistoricoRecebimento')
  AND Indice.name = N'IX_HistoricoRecebimento_FilialId_DataHora';
GO
