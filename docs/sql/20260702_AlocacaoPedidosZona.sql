IF COL_LENGTH('dbo.Zona', 'AreaId') IS NULL
BEGIN
    ALTER TABLE dbo.Zona ADD AreaId INT NULL;
END;
GO

IF COL_LENGTH('dbo.Zona', 'Nome') IS NULL
BEGIN
    ALTER TABLE dbo.Zona ADD Nome VARCHAR(100) NULL;
END;
GO

IF COL_LENGTH('dbo.Zona', 'Ativo') IS NULL
BEGIN
    ALTER TABLE dbo.Zona ADD Ativo BIT NOT NULL CONSTRAINT DF_Zona_Ativo DEFAULT (1);
END;
GO

IF COL_LENGTH('dbo.AreaRomaneio', 'Alocar') IS NULL
BEGIN
    ALTER TABLE dbo.AreaRomaneio ADD Alocar BIT NOT NULL CONSTRAINT DF_AreaRomaneio_Alocar DEFAULT (0);

    UPDATE dbo.AreaRomaneio
       SET Alocar = ISNULL(Separar, 0);
END;
GO

IF COL_LENGTH('dbo.AreaRomaneio', 'Mapa') IS NULL
BEGIN
    ALTER TABLE dbo.AreaRomaneio ADD Mapa BIT NOT NULL CONSTRAINT DF_AreaRomaneio_Mapa DEFAULT (0);

    IF EXISTS
    (
        SELECT 1
          FROM sys.columns
         WHERE object_id = OBJECT_ID('dbo.AreaPedido')
           AND name = 'Mapa'
    )
    BEGIN
        EXEC sp_executesql N'
            UPDATE ar
               SET Mapa = 1
              FROM dbo.AreaRomaneio ar
             WHERE EXISTS
             (
                 SELECT 1
                   FROM dbo.AreaPedido ap
                  WHERE ap.AreaId = ar.Id
                    AND ISNULL(ap.Mapa, 0) = 1
             );';
    END
END;
GO

IF COL_LENGTH('dbo.Zona', 'Nome') IS NOT NULL AND COL_LENGTH('dbo.Zona', 'Codigo') IS NOT NULL
BEGIN
    UPDATE dbo.Zona
       SET Nome = COALESCE(NULLIF(LTRIM(RTRIM(Nome)), ''), NULLIF(LTRIM(RTRIM(Codigo)), ''))
     WHERE Nome IS NULL OR LTRIM(RTRIM(Nome)) = '';
END;
GO

IF COL_LENGTH('dbo.Locacao', 'AreaId') IS NULL
BEGIN
    ALTER TABLE dbo.Locacao ADD AreaId INT NULL;
END;
GO

IF COL_LENGTH('dbo.Locacao', 'Id') IS NULL
BEGIN
    ALTER TABLE dbo.Locacao ADD Id INT NULL;
END;
GO

IF EXISTS
(
    SELECT 1
      FROM sys.columns
     WHERE object_id = OBJECT_ID('dbo.Locacao')
       AND name = 'Id'
)
AND EXISTS
(
    SELECT 1
      FROM dbo.Locacao
     WHERE Id IS NULL
)
BEGIN
    ;WITH locacaoBase AS
    (
        SELECT Codigo,
               FilialId,
               ROW_NUMBER() OVER (ORDER BY ISNULL(FilialId, 0), Codigo) AS NovoId
          FROM dbo.Locacao
         WHERE Id IS NULL
    )
    UPDATE l
       SET Id = b.NovoId
      FROM dbo.Locacao l
      INNER JOIN locacaoBase b
              ON b.Codigo = l.Codigo
             AND ISNULL(b.FilialId, -1) = ISNULL(l.FilialId, -1);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.sequences WHERE name = 'SEQ_Locacao_Id' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    DECLARE @NextLocacaoId INT;
    DECLARE @SqlCreateLocacaoSequence NVARCHAR(4000);

    SELECT @NextLocacaoId = ISNULL(MAX(Id), 0) + 1
      FROM dbo.Locacao;

    SET @SqlCreateLocacaoSequence =
        N'CREATE SEQUENCE dbo.SEQ_Locacao_Id AS INT START WITH '
        + CONVERT(NVARCHAR(20), @NextLocacaoId)
        + N' INCREMENT BY 1;';

    EXEC sp_executesql @SqlCreateLocacaoSequence;
END;
GO

IF EXISTS
(
    SELECT 1
      FROM sys.columns
     WHERE object_id = OBJECT_ID('dbo.Locacao')
       AND name = 'Id'
       AND is_nullable = 1
)
AND NOT EXISTS
(
    SELECT 1
      FROM dbo.Locacao
     WHERE Id IS NULL
)
BEGIN
    ALTER TABLE dbo.Locacao ALTER COLUMN Id INT NOT NULL;
END;
GO

IF NOT EXISTS
(
    SELECT 1
      FROM sys.default_constraints dc
      INNER JOIN sys.columns c
              ON c.default_object_id = dc.object_id
     WHERE dc.parent_object_id = OBJECT_ID('dbo.Locacao')
       AND c.name = 'Id'
)
AND EXISTS
(
    SELECT 1
      FROM sys.sequences
     WHERE name = 'SEQ_Locacao_Id'
       AND schema_id = SCHEMA_ID('dbo')
)
BEGIN
    ALTER TABLE dbo.Locacao
        ADD CONSTRAINT DF_Locacao_Id DEFAULT (NEXT VALUE FOR dbo.SEQ_Locacao_Id) FOR Id;
END;
GO

IF COL_LENGTH('dbo.Locacao', 'ZonaId') IS NULL
BEGIN
    ALTER TABLE dbo.Locacao ADD ZonaId INT NULL;
END;
GO

UPDATE dbo.Locacao
   SET ZonaId = NULL
 WHERE ZonaId IS NOT NULL
   AND NOT EXISTS
   (
       SELECT 1
         FROM dbo.Zona z
        WHERE z.Id = dbo.Locacao.ZonaId
   );
GO

IF COL_LENGTH('dbo.Romaneio', 'OS') IS NULL
BEGIN
    ALTER TABLE dbo.Romaneio ADD OS VARCHAR(100) NULL;
END;
GO

IF COL_LENGTH('dbo.RomaneioItem', 'Descricao') IS NULL
BEGIN
    ALTER TABLE dbo.RomaneioItem ADD Descricao VARCHAR(250) NULL;
END;
GO

IF COL_LENGTH('dbo.RomaneioItem', 'ValorUnitario') IS NULL
BEGIN
    ALTER TABLE dbo.RomaneioItem ADD ValorUnitario NUMERIC(18, 2) NULL;
END;
GO

IF COL_LENGTH('dbo.RomaneioItem', 'ValorTotal') IS NULL
BEGIN
    ALTER TABLE dbo.RomaneioItem ADD ValorTotal NUMERIC(18, 2) NULL;
END;
GO

IF COL_LENGTH('dbo.RomaneioItem', 'QtdeSeparada') IS NULL
BEGIN
    ALTER TABLE dbo.RomaneioItem ADD QtdeSeparada INT NULL;
END;
GO

IF COL_LENGTH('dbo.RomaneioItem', 'QtdeConferida') IS NULL
BEGIN
    ALTER TABLE dbo.RomaneioItem ADD QtdeConferida INT NULL;
END;
GO

IF COL_LENGTH('dbo.Zona', 'Prioridade') IS NOT NULL
BEGIN
    DECLARE @DfZonaPrioridade NVARCHAR(128);
    DECLARE @SqlDropPrioridade NVARCHAR(4000);

    SELECT @DfZonaPrioridade = dc.name
      FROM sys.default_constraints dc
      INNER JOIN sys.columns c
              ON c.default_object_id = dc.object_id
     WHERE dc.parent_object_id = OBJECT_ID('dbo.Zona')
       AND c.name = 'Prioridade';

    IF @DfZonaPrioridade IS NOT NULL
    BEGIN
        SET @SqlDropPrioridade = N'ALTER TABLE dbo.Zona DROP CONSTRAINT [' + REPLACE(@DfZonaPrioridade, ']', ']]') + N'];';
        EXEC sp_executesql @SqlDropPrioridade;
    END;

    ALTER TABLE dbo.Zona DROP COLUMN Prioridade;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Zona_Area')
AND COL_LENGTH('dbo.Zona', 'AreaId') IS NOT NULL
BEGIN
    ALTER TABLE dbo.Zona
        ADD CONSTRAINT FK_Zona_Area FOREIGN KEY (AreaId) REFERENCES dbo.Area (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Locacao_Zona')
AND COL_LENGTH('dbo.Locacao', 'ZonaId') IS NOT NULL
BEGIN
    ALTER TABLE dbo.Locacao
        ADD CONSTRAINT FK_Locacao_Zona FOREIGN KEY (ZonaId) REFERENCES dbo.Zona (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Locacao_ZonaId' AND object_id = OBJECT_ID('dbo.Locacao'))
BEGIN
    CREATE INDEX IX_Locacao_ZonaId ON dbo.Locacao (ZonaId);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Locacao_Id' AND object_id = OBJECT_ID('dbo.Locacao'))
BEGIN
    CREATE UNIQUE INDEX UX_Locacao_Id ON dbo.Locacao (Id);
END;
GO

IF COL_LENGTH('dbo.RomaneioItem', 'LocacaoId') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_RomaneioItem_Locacao')
AND EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Locacao_Id' AND object_id = OBJECT_ID('dbo.Locacao'))
BEGIN
    ALTER TABLE dbo.RomaneioItem
        ADD CONSTRAINT FK_RomaneioItem_Locacao FOREIGN KEY (LocacaoId) REFERENCES dbo.Locacao (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_RomaneioItem_TarefaNr' AND object_id = OBJECT_ID('dbo.RomaneioItem'))
BEGIN
    CREATE INDEX IX_RomaneioItem_TarefaNr ON dbo.RomaneioItem (TarefaNr);
END;
GO

IF NOT EXISTS
(
    SELECT 1
      FROM sys.indexes
     WHERE name = 'IX_RomaneioItem_Filial_Status_Pendencia'
       AND object_id = OBJECT_ID('dbo.RomaneioItem')
)
BEGIN
    IF EXISTS
    (
        SELECT 1
          FROM sys.stats
         WHERE name = 'IX_RomaneioItem_Filial_Status_Pendencia'
           AND object_id = OBJECT_ID('dbo.RomaneioItem')
    )
    BEGIN
        DROP STATISTICS dbo.RomaneioItem.IX_RomaneioItem_Filial_Status_Pendencia;
    END;

    CREATE INDEX IX_RomaneioItem_Filial_Status_Pendencia
        ON dbo.RomaneioItem (FilialId, StatusId)
        INCLUDE (RomaneioId, ZonaId, LocacaoId, ItemNr, Descricao, Qtde);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Romaneio_OS' AND object_id = OBJECT_ID('dbo.Romaneio'))
BEGIN
    CREATE INDEX IX_Romaneio_OS ON dbo.Romaneio (OS);
END;
GO

IF NOT EXISTS
(
    SELECT 1
      FROM sys.indexes
     WHERE name = 'UX_Usuario_Login'
       AND object_id = OBJECT_ID('dbo.Usuario')
)
AND NOT EXISTS
(
    SELECT 1
      FROM dbo.Usuario
     GROUP BY UPPER(LTRIM(RTRIM(Login)))
    HAVING COUNT(*) > 1
)
BEGIN
    CREATE UNIQUE INDEX UX_Usuario_Login ON dbo.Usuario (Login);
END;
GO

DECLARE @MenuEstoqueId INT;
DECLARE @MenuSeparacaoId INT;

SELECT @MenuEstoqueId = Id
  FROM dbo.AppMenu
 WHERE Area = 'EstoqueApp'
   AND Titulo = 'Estoque'
   AND Nivel = 1
   AND IdNivelSup IS NULL;

SELECT @MenuSeparacaoId = Id
  FROM dbo.AppMenu
 WHERE Area = 'SeparacaoApp'
   AND Titulo = 'Romaneios'
   AND Nivel = 1
   AND IdNivelSup IS NULL;

IF @MenuEstoqueId IS NOT NULL
AND NOT EXISTS
(
    SELECT 1
      FROM dbo.AppMenu
     WHERE Area = 'EstoqueApp'
       AND Controller = 'Zona'
       AND Action = 'Index'
)
BEGIN
    INSERT INTO dbo.AppMenu
    (
        Titulo,
        Area,
        Controller,
        Action,
        Css,
        Status,
        Sequencia,
        Nivel,
        IdNivelSup,
        HasChild,
        DatUltAtlz,
        FilialId
    )
    VALUES
    (
        'Zonas',
        'EstoqueApp',
        'Zona',
        'Index',
        'fa-solid fa-layer-group fa-fw',
        1,
        270,
        2,
        @MenuEstoqueId,
        0,
        GETDATE(),
        NULL
    );
END;
GO

DECLARE @MenuSeparacaoId INT;
SELECT @MenuSeparacaoId = Id
  FROM dbo.AppMenu
 WHERE Area = 'SeparacaoApp'
   AND Titulo = 'Romaneios'
   AND Nivel = 1
   AND IdNivelSup IS NULL;

IF @MenuSeparacaoId IS NOT NULL
AND NOT EXISTS
(
    SELECT 1
      FROM dbo.AppMenu
     WHERE Area = 'SeparacaoApp'
       AND Controller = 'Romaneio'
       AND Action = 'AlocacaoZona'
)
BEGIN
    INSERT INTO dbo.AppMenu
    (
        Titulo,
        Area,
        Controller,
        Action,
        Css,
        Status,
        Sequencia,
        Nivel,
        IdNivelSup,
        HasChild,
        DatUltAtlz,
        FilialId
    )
    VALUES
    (
        'Alocacao por Zona',
        'SeparacaoApp',
        'Romaneio',
        'AlocacaoZona',
        'fa-solid fa-diagram-project fa-fw',
        1,
        340,
        2,
        @MenuSeparacaoId,
        0,
        GETDATE(),
        NULL
    );
END;
GO

UPDATE dbo.AppMenu
   SET Titulo = N'Alocação de Pedidos'
 WHERE Area = 'SeparacaoApp'
   AND Controller = 'Romaneio'
   AND Action = 'AlocacaoZona'
   AND Titulo <> N'Alocação de Pedidos';
GO

UPDATE dbo.AppMenu
   SET Titulo = N'Pendências'
 WHERE Area = 'SeparacaoApp'
   AND Controller = 'Romaneio'
   AND Action = 'Pendencias'
   AND Titulo <> N'Pendências';
GO

UPDATE dbo.AppMenu
   SET Titulo = N'Não Encontrados'
 WHERE Area = 'SeparacaoApp'
   AND Controller = 'Romaneio'
   AND Action = 'NaoEncontrados'
   AND Titulo <> N'Não Encontrados';
GO

DECLARE @MenuSeparacaoId INT;
SELECT @MenuSeparacaoId = Id
  FROM dbo.AppMenu
 WHERE Area = 'SeparacaoApp'
   AND Titulo = 'Romaneios'
   AND Nivel = 1
   AND IdNivelSup IS NULL;

IF @MenuSeparacaoId IS NOT NULL
AND NOT EXISTS
(
    SELECT 1
      FROM dbo.AppMenu
     WHERE Area = 'SeparacaoApp'
       AND Controller = 'Romaneio'
       AND Action = 'NaoEncontrados'
)
BEGIN
    INSERT INTO dbo.AppMenu
    (
        Titulo,
        Area,
        Controller,
        Action,
        Css,
        Status,
        Sequencia,
        Nivel,
        IdNivelSup,
        HasChild,
        DatUltAtlz,
        FilialId
    )
    VALUES
    (
        N'Não Encontrados',
        'SeparacaoApp',
        'Romaneio',
        'NaoEncontrados',
        'fa-solid fa-ban fa-fw',
        1,
        370,
        2,
        @MenuSeparacaoId,
        0,
        GETDATE(),
        NULL
    );
END;
GO

DECLARE @MenuSeparacaoId INT;
SELECT @MenuSeparacaoId = Id
  FROM dbo.AppMenu
 WHERE Area = 'SeparacaoApp'
   AND Titulo = 'Romaneios'
   AND Nivel = 1
   AND IdNivelSup IS NULL;

IF @MenuSeparacaoId IS NOT NULL
AND NOT EXISTS
(
    SELECT 1
      FROM dbo.AppMenu
     WHERE Area = 'SeparacaoApp'
       AND Controller = 'Romaneio'
       AND Action = 'Pendencias'
)
BEGIN
    INSERT INTO dbo.AppMenu
    (
        Titulo,
        Area,
        Controller,
        Action,
        Css,
        Status,
        Sequencia,
        Nivel,
        IdNivelSup,
        HasChild,
        DatUltAtlz,
        FilialId
    )
    VALUES
    (
        N'Pendências',
        'SeparacaoApp',
        'Romaneio',
        'Pendencias',
        'fa-solid fa-triangle-exclamation fa-fw',
        1,
        360,
        2,
        @MenuSeparacaoId,
        0,
        GETDATE(),
        NULL
    );
END;
GO

DECLARE @MenuSeparacaoId INT;
SELECT @MenuSeparacaoId = Id
  FROM dbo.AppMenu
 WHERE Area = 'SeparacaoApp'
   AND Titulo = 'Romaneios'
   AND Nivel = 1
   AND IdNivelSup IS NULL;

IF @MenuSeparacaoId IS NOT NULL
AND NOT EXISTS
(
    SELECT 1
      FROM dbo.AppMenu
     WHERE Area = 'SeparacaoApp'
       AND Controller = 'Romaneio'
       AND Action = 'Tarefas'
)
BEGIN
    INSERT INTO dbo.AppMenu
    (
        Titulo,
        Area,
        Controller,
        Action,
        Css,
        Status,
        Sequencia,
        Nivel,
        IdNivelSup,
        HasChild,
        DatUltAtlz,
        FilialId
    )
    VALUES
    (
        'Consulta de Tarefas',
        'SeparacaoApp',
        'Romaneio',
        'Tarefas',
        'fa-solid fa-list-check fa-fw',
        1,
        350,
        2,
        @MenuSeparacaoId,
        0,
        GETDATE(),
        NULL
    );
END;
GO
