/*
    QUASAR.DEALER - Anomalias GM - Fase 1

    AMBIENTE DE TESTES EXCLUSIVAMENTE.
    Para executar conscientemente no banco de testes, altere a variável abaixo para 1.
    O script não deve ser executado em produção.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @ConfirmarAmbienteDeTestes bit = 0;
IF @ConfirmarAmbienteDeTestes <> 1
    THROW 51000, 'Execucao bloqueada: confirme explicitamente o ambiente de TESTES no script.', 1;

BEGIN TRANSACTION;

IF OBJECT_ID('dbo.AnomaliaGmTipo', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AnomaliaGmTipo
    (
        Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_AnomaliaGmTipo PRIMARY KEY,
        Codigo varchar(2) NOT NULL,
        Descricao varchar(250) NOT NULL,
        PrazoDias int NOT NULL,
        Operacional bit NOT NULL CONSTRAINT DF_AnomaliaGmTipo_Operacional DEFAULT (0),
        Ativo bit NOT NULL CONSTRAINT DF_AnomaliaGmTipo_Ativo DEFAULT (1),
        CriadoEm datetime NOT NULL,
        CriadoPor varchar(100) NULL,
        ModificadoEm datetime NULL,
        ModificadoPor varchar(100) NULL,
        CONSTRAINT UQ_AnomaliaGmTipo_Codigo UNIQUE (Codigo),
        CONSTRAINT CK_AnomaliaGmTipo_Prazo CHECK (PrazoDias > 0)
    );
END;

IF OBJECT_ID('dbo.AnomaliaGmStatus', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AnomaliaGmStatus
    (
        Id int NOT NULL CONSTRAINT PK_AnomaliaGmStatus PRIMARY KEY,
        Codigo varchar(30) NOT NULL,
        Descricao varchar(100) NOT NULL,
        Ativo bit NOT NULL CONSTRAINT DF_AnomaliaGmStatus_Ativo DEFAULT (1),
        CONSTRAINT UQ_AnomaliaGmStatus_Codigo UNIQUE (Codigo)
    );
END;

IF OBJECT_ID('dbo.AnomaliaGmProcesso', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AnomaliaGmProcesso
    (
        Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_AnomaliaGmProcesso PRIMARY KEY,
        NumeroControle varchar(20) NOT NULL,
        StatusId int NOT NULL,
        DataAbertura datetime NOT NULL,
        UsuarioLogin varchar(100) NOT NULL,
        Observacao varchar(1000) NULL,
        EmpresaId int NULL,
        FilialId int NOT NULL,
        Ativo bit NOT NULL CONSTRAINT DF_AnomaliaGmProcesso_Ativo DEFAULT (1),
        Cancelado bit NOT NULL CONSTRAINT DF_AnomaliaGmProcesso_Cancelado DEFAULT (0),
        CanceladoEm datetime NULL,
        CanceladoPor varchar(100) NULL,
        MotivoCancelamento varchar(500) NULL,
        CriadoEm datetime NOT NULL,
        CriadoPor varchar(100) NOT NULL,
        ModificadoEm datetime NULL,
        ModificadoPor varchar(100) NULL,
        Versao rowversion NOT NULL,
        CONSTRAINT FK_AnomaliaGmProcesso_Status FOREIGN KEY (StatusId) REFERENCES dbo.AnomaliaGmStatus(Id),
        CONSTRAINT UQ_AnomaliaGmProcesso_ControleFilial UNIQUE (FilialId, NumeroControle)
    );
END;

IF OBJECT_ID('dbo.AnomaliaGmItem', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AnomaliaGmItem
    (
        Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_AnomaliaGmItem PRIMARY KEY,
        AnomaliaId int NOT NULL,
        AnomaliaTipoId int NOT NULL,
        NotaFiscalId int NOT NULL,
        NotaFiscalItemId int NOT NULL,
        VolumeNr varchar(100) NULL,
        ItemNr varchar(100) NOT NULL,
        QuantidadeNF decimal(18,4) NOT NULL,
        QuantidadeReclamada decimal(18,4) NOT NULL,
        QuantidadeRecebida decimal(18,4) NULL,
        ItemRecebidoNr varchar(100) NULL,
        StatusId int NOT NULL,
        Observacao varchar(1000) NULL,
        InstaladoVeiculo bit NULL,
        CondicaoEmbalagem varchar(500) NULL,
        DataReclamacao datetime NOT NULL,
        DataLimiteReclamacao datetime NOT NULL,
        SaldoConsumido bit NOT NULL CONSTRAINT DF_AnomaliaGmItem_SaldoConsumido DEFAULT (1),
        Cancelado bit NOT NULL CONSTRAINT DF_AnomaliaGmItem_Cancelado DEFAULT (0),
        FilialId int NOT NULL,
        CriadoEm datetime NOT NULL,
        CriadoPor varchar(100) NOT NULL,
        ModificadoEm datetime NULL,
        ModificadoPor varchar(100) NULL,
        Versao rowversion NOT NULL,
        CONSTRAINT FK_AnomaliaGmItem_Processo FOREIGN KEY (AnomaliaId) REFERENCES dbo.AnomaliaGmProcesso(Id),
        CONSTRAINT FK_AnomaliaGmItem_Tipo FOREIGN KEY (AnomaliaTipoId) REFERENCES dbo.AnomaliaGmTipo(Id),
        CONSTRAINT FK_AnomaliaGmItem_Status FOREIGN KEY (StatusId) REFERENCES dbo.AnomaliaGmStatus(Id),
        CONSTRAINT FK_AnomaliaGmItem_NotaFiscal FOREIGN KEY (NotaFiscalId) REFERENCES dbo.NotaFiscal(Id),
        CONSTRAINT FK_AnomaliaGmItem_NotaFiscalItem FOREIGN KEY (NotaFiscalItemId) REFERENCES dbo.NotaFiscalItem(Id),
        CONSTRAINT CK_AnomaliaGmItem_Quantidade CHECK (QuantidadeReclamada > 0),
        CONSTRAINT CK_AnomaliaGmItem_QuantidadeNF CHECK (QuantidadeNF >= 0)
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.AnomaliaGmItem') AND name = 'IX_AnomaliaGmItem_Saldo')
BEGIN
    CREATE INDEX IX_AnomaliaGmItem_Saldo
        ON dbo.AnomaliaGmItem (FilialId, NotaFiscalItemId, SaldoConsumido, Cancelado)
        INCLUDE (QuantidadeReclamada, AnomaliaTipoId);
END;

IF NOT EXISTS
(
    SELECT 1 FROM sys.check_constraints
    WHERE parent_object_id = OBJECT_ID('dbo.AnomaliaGmItem')
      AND name = 'CK_AnomaliaGmItem_QuantidadeInteira'
)
BEGIN
    ALTER TABLE dbo.AnomaliaGmItem WITH CHECK
        ADD CONSTRAINT CK_AnomaliaGmItem_QuantidadeInteira
        CHECK (QuantidadeReclamada = FLOOR(QuantidadeReclamada));
END;

IF OBJECT_ID('dbo.AnomaliaGmHistorico', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AnomaliaGmHistorico
    (
        Id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_AnomaliaGmHistorico PRIMARY KEY,
        AnomaliaId int NOT NULL,
        AnomaliaItemId int NULL,
        Evento varchar(50) NOT NULL,
        StatusAnteriorId int NULL,
        StatusNovoId int NULL,
        UsuarioLogin varchar(100) NOT NULL,
        DataHora datetime NOT NULL,
        Observacao varchar(1000) NULL,
        FilialId int NOT NULL,
        CONSTRAINT FK_AnomaliaGmHistorico_Processo FOREIGN KEY (AnomaliaId) REFERENCES dbo.AnomaliaGmProcesso(Id),
        CONSTRAINT FK_AnomaliaGmHistorico_Item FOREIGN KEY (AnomaliaItemId) REFERENCES dbo.AnomaliaGmItem(Id)
    );
END;

IF OBJECT_ID('dbo.AnomaliaGmArquivo', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AnomaliaGmArquivo
    (
        Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_AnomaliaGmArquivo PRIMARY KEY,
        AnomaliaId int NOT NULL,
        TipoAnomalia varchar(10) NOT NULL,
        NumeroSequencia int NOT NULL,
        NomeArquivo varchar(260) NOT NULL,
        QuantidadeItens int NOT NULL,
        DataGeracao datetime NOT NULL,
        UsuarioGeracaoLogin varchar(100) NOT NULL,
        Reenvio bit NOT NULL CONSTRAINT DF_AnomaliaGmArquivo_Reenvio DEFAULT (0),
        ArquivoOrigemId int NULL,
        FilialId int NOT NULL,
        CriadoEm datetime NOT NULL,
        CONSTRAINT FK_AnomaliaGmArquivo_Processo FOREIGN KEY (AnomaliaId) REFERENCES dbo.AnomaliaGmProcesso(Id),
        CONSTRAINT FK_AnomaliaGmArquivo_Origem FOREIGN KEY (ArquivoOrigemId) REFERENCES dbo.AnomaliaGmArquivo(Id),
        CONSTRAINT UQ_AnomaliaGmArquivo_Sequencia UNIQUE (AnomaliaId, TipoAnomalia, Reenvio, NumeroSequencia),
        CONSTRAINT CK_AnomaliaGmArquivo_Quantidade CHECK (QuantidadeItens > 0)
    );
END;

IF OBJECT_ID('dbo.AnomaliaGmArquivoItem', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AnomaliaGmArquivoItem
    (
        Id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_AnomaliaGmArquivoItem PRIMARY KEY,
        AnomaliaArquivoId int NOT NULL,
        AnomaliaItemId int NOT NULL,
        CONSTRAINT FK_AnomaliaGmArquivoItem_Arquivo FOREIGN KEY (AnomaliaArquivoId) REFERENCES dbo.AnomaliaGmArquivo(Id),
        CONSTRAINT FK_AnomaliaGmArquivoItem_Item FOREIGN KEY (AnomaliaItemId) REFERENCES dbo.AnomaliaGmItem(Id),
        CONSTRAINT UQ_AnomaliaGmArquivoItem UNIQUE (AnomaliaArquivoId, AnomaliaItemId)
    );
END;

DECLARE @Agora datetime = GETDATE();
MERGE dbo.AnomaliaGmStatus AS destino
USING (VALUES
    (1, 'EM_PROCESSO', 'Em processo'),
    (2, 'ACEITO', 'Aceito'),
    (3, 'REJEITADO', 'Rejeitado'),
    (4, 'FINALIZADO', 'Finalizado')
) AS origem (Id, Codigo, Descricao)
ON destino.Id = origem.Id
WHEN MATCHED THEN UPDATE SET Codigo = origem.Codigo, Descricao = origem.Descricao, Ativo = 1
WHEN NOT MATCHED THEN INSERT (Id, Codigo, Descricao, Ativo) VALUES (origem.Id, origem.Codigo, origem.Descricao, 1);

MERGE dbo.AnomaliaGmTipo AS destino
USING (VALUES
    ('A', 'ITEM ENVIADO A MENOR OU NÃO ENVIADO', 7, 1),
    ('B', 'ITEM ENVIADO A MAIOR, AGUARDANDO NF PARA REGULARIZAÇÃO', 7, 1),
    ('C', 'PEÇA NÃO CONFERE COM O FATURADO, AGUARDANDO NF PARA REGULARIZAÇÃO', 7, 1),
    ('D', 'PEÇA TROCADA NA EMBALAGEM, AGUARDANDO NF PARA REGULARIZAÇÃO', 120, 0),
    ('F', 'PAINEIS METÁLICOS E VIDROS COM DEFEITO DE FABRICAÇÃO', 120, 0),
    ('G', 'PEÇAS/CONJUNTOS DANIFICADOS DENTRO DA EMBALAGEM', 30, 1),
    ('H', 'PEÇAS COM MAU FUNCIONAMENTO (PERMITEM INSTALAÇÃO)', 120, 0),
    ('I', 'KITs/CONJUNTOS INCOMPLETOS', 120, 0),
    ('J', 'OUTROS', 120, 0)
) AS origem (Codigo, Descricao, PrazoDias, Operacional)
ON destino.Codigo = origem.Codigo
WHEN MATCHED THEN UPDATE SET
    Descricao = origem.Descricao,
    PrazoDias = origem.PrazoDias,
    Operacional = origem.Operacional,
    Ativo = 1,
    ModificadoEm = @Agora,
    ModificadoPor = 'SCRIPT-TESTES'
WHEN NOT MATCHED THEN INSERT
    (Codigo, Descricao, PrazoDias, Operacional, Ativo, CriadoEm, CriadoPor)
    VALUES
    (origem.Codigo, origem.Descricao, origem.PrazoDias, origem.Operacional, 1, @Agora, 'SCRIPT-TESTES');

IF NOT EXISTS (SELECT 1 FROM dbo.AppConfig WHERE Nome = 'ControleNr' AND FilialId IS NOT NULL)
BEGIN
    THROW 51001, 'Configure ControleNr por filial na AppConfig antes de utilizar Anomalias GM.', 1;
END;

DECLARE @MenuAnomaliasId int;
SELECT TOP 1 @MenuAnomaliasId = Id
FROM dbo.AppMenu
WHERE Area = 'AnomaliaApp' AND Nivel = 1
ORDER BY Id;

IF @MenuAnomaliasId IS NULL
BEGIN
    INSERT INTO dbo.AppMenu
        (Titulo, Area, Controller, Action, Css, Status, Sequencia, Nivel,
         IdNivelSup, HasChild, DatUltAtlz, FilialId)
    VALUES
        ('Anomalias', 'AnomaliaApp', '', '', 'fa-solid fa-triangle-exclamation fa-fw',
         1, 500, 1, NULL, 1, @Agora, 1);
    SET @MenuAnomaliasId = SCOPE_IDENTITY();
END
ELSE
BEGIN
    UPDATE dbo.AppMenu
       SET Titulo = 'Anomalias',
           Controller = '',
           Action = '',
           Css = 'fa-solid fa-triangle-exclamation fa-fw',
           Status = 1,
           Sequencia = 500,
           Nivel = 1,
           IdNivelSup = NULL,
           HasChild = 1,
           DatUltAtlz = @Agora
     WHERE Id = @MenuAnomaliasId;
END;

IF NOT EXISTS
(
    SELECT 1 FROM dbo.AppMenu
    WHERE Area = 'AnomaliaApp' AND Controller = 'Processo' AND Action = 'Create'
)
BEGIN
    INSERT INTO dbo.AppMenu
        (Titulo, Area, Controller, Action, Css, Status, Sequencia, Nivel,
         IdNivelSup, HasChild, DatUltAtlz, FilialId)
    VALUES
        ('Cadastrar Anomalia', 'AnomaliaApp', 'Processo', 'Create', 'fa-solid fa-plus',
         1, 501, 2, @MenuAnomaliasId, 0, @Agora, 1);
END;

IF NOT EXISTS
(
    SELECT 1 FROM dbo.AppMenu
    WHERE Area = 'AnomaliaApp' AND Controller = 'Processo' AND Action = 'Index'
)
BEGIN
    INSERT INTO dbo.AppMenu
        (Titulo, Area, Controller, Action, Css, Status, Sequencia, Nivel,
         IdNivelSup, HasChild, DatUltAtlz, FilialId)
    VALUES
        ('Consultar Anomalias', 'AnomaliaApp', 'Processo', 'Index', 'fa-solid fa-magnifying-glass',
         1, 502, 2, @MenuAnomaliasId, 0, @Agora, 1);
END;

COMMIT TRANSACTION;
