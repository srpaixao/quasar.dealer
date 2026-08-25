IF OBJECT_ID('dbo.DevolucaoComplemento', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.DevolucaoComplemento
    (
        DevolucaoId INT NOT NULL PRIMARY KEY,
        DocExpedicaoId INT NULL,
        NotaFiscalId INT NULL,
        DataVenda DATETIME NULL,
        CriadoPor VARCHAR(100) NULL,
        CriadoEm DATETIME NULL,
        ModificadoPor VARCHAR(100) NULL,
        ModificadoEm DATETIME NULL
    );
END
