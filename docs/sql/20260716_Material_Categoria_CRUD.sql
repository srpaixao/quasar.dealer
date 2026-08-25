SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    IF COL_LENGTH(N'dbo.Material', N'CategoriaProduto') IS NULL
    BEGIN
        ALTER TABLE dbo.Material
            ADD CategoriaProduto VARCHAR(20) NULL;
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.default_constraints
        WHERE parent_object_id = OBJECT_ID(N'dbo.Material')
          AND name = N'DF_Material_CategoriaProduto'
    )
    BEGIN
        ALTER TABLE dbo.Material
            ADD CONSTRAINT DF_Material_CategoriaProduto
            DEFAULT ('Diretos') FOR CategoriaProduto;
    END;

    IF COL_LENGTH(N'dbo.Material', N'Zona1Id') IS NULL
        ALTER TABLE dbo.Material ADD Zona1Id INT NULL;

    IF COL_LENGTH(N'dbo.Material', N'Eqpto1Id') IS NULL
        ALTER TABLE dbo.Material ADD Eqpto1Id INT NULL;

    IF COL_LENGTH(N'dbo.Material', N'QtdePadrao1') IS NULL
        ALTER TABLE dbo.Material ADD QtdePadrao1 INT NULL;

    IF COL_LENGTH(N'dbo.Material', N'Zona2Id') IS NULL
        ALTER TABLE dbo.Material ADD Zona2Id INT NULL;

    IF COL_LENGTH(N'dbo.Material', N'Eqpto2Id') IS NULL
        ALTER TABLE dbo.Material ADD Eqpto2Id INT NULL;

    IF COL_LENGTH(N'dbo.Material', N'QtdePadrao2') IS NULL
        ALTER TABLE dbo.Material ADD QtdePadrao2 INT NULL;

    IF COL_LENGTH(N'dbo.Equipamento', N'Comp') IS NULL
        ALTER TABLE dbo.Equipamento ADD Comp NUMERIC(18,2) NULL;

    IF COL_LENGTH(N'dbo.Equipamento', N'Larg') IS NULL
        ALTER TABLE dbo.Equipamento ADD Larg NUMERIC(18,2) NULL;

    IF COL_LENGTH(N'dbo.Equipamento', N'Altu') IS NULL
        ALTER TABLE dbo.Equipamento ADD Altu NUMERIC(18,2) NULL;

    IF COL_LENGTH(N'dbo.Equipamento', N'Zonas') IS NULL
        ALTER TABLE dbo.Equipamento ADD Zonas VARCHAR(200) NULL;

    IF COL_LENGTH(N'dbo.Equipamento', N'Qtde') IS NULL
        ALTER TABLE dbo.Equipamento ADD Qtde INT NULL;

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.check_constraints
        WHERE parent_object_id = OBJECT_ID(N'dbo.Material')
          AND name = N'CK_Material_CategoriaProduto'
    )
    BEGIN
        ALTER TABLE dbo.Material WITH CHECK
            ADD CONSTRAINT CK_Material_CategoriaProduto
            CHECK
            (
                CategoriaProduto IS NULL
                OR CategoriaProduto IN ('Diretos', 'Indiretos')
            );
    END;

    IF NOT EXISTS
    (
        SELECT 1 FROM sys.check_constraints
        WHERE parent_object_id = OBJECT_ID(N'dbo.Material')
          AND name = N'CK_Material_QtdePadrao1'
    )
    BEGIN
        EXEC(N'
            ALTER TABLE dbo.Material WITH CHECK
                ADD CONSTRAINT CK_Material_QtdePadrao1
                CHECK (QtdePadrao1 IS NULL OR QtdePadrao1 > 0);');
    END;

    IF NOT EXISTS
    (
        SELECT 1 FROM sys.foreign_keys
        WHERE parent_object_id = OBJECT_ID(N'dbo.Material')
          AND name = N'FK_Material_Zona1'
    )
    BEGIN
        EXEC(N'
            ALTER TABLE dbo.Material WITH CHECK
                ADD CONSTRAINT FK_Material_Zona1
                FOREIGN KEY (Zona1Id) REFERENCES dbo.Zona(Id);');
    END;

    IF NOT EXISTS
    (
        SELECT 1 FROM sys.foreign_keys
        WHERE parent_object_id = OBJECT_ID(N'dbo.Material')
          AND name = N'FK_Material_Equipamento1'
    )
    BEGIN
        EXEC(N'
            ALTER TABLE dbo.Material WITH CHECK
                ADD CONSTRAINT FK_Material_Equipamento1
                FOREIGN KEY (Eqpto1Id) REFERENCES dbo.Equipamento(Id);');
    END;

    IF NOT EXISTS
    (
        SELECT 1 FROM sys.foreign_keys
        WHERE parent_object_id = OBJECT_ID(N'dbo.Material')
          AND name = N'FK_Material_Zona2'
    )
    BEGIN
        EXEC(N'
            ALTER TABLE dbo.Material WITH CHECK
                ADD CONSTRAINT FK_Material_Zona2
                FOREIGN KEY (Zona2Id) REFERENCES dbo.Zona(Id);');
    END;

    IF NOT EXISTS
    (
        SELECT 1 FROM sys.foreign_keys
        WHERE parent_object_id = OBJECT_ID(N'dbo.Material')
          AND name = N'FK_Material_Equipamento2'
    )
    BEGIN
        EXEC(N'
            ALTER TABLE dbo.Material WITH CHECK
                ADD CONSTRAINT FK_Material_Equipamento2
                FOREIGN KEY (Eqpto2Id) REFERENCES dbo.Equipamento(Id);');
    END;

    IF NOT EXISTS
    (
        SELECT 1 FROM sys.check_constraints
        WHERE parent_object_id = OBJECT_ID(N'dbo.Material')
          AND name = N'CK_Material_QtdePadrao2'
    )
    BEGIN
        EXEC(N'
            ALTER TABLE dbo.Material WITH CHECK
                ADD CONSTRAINT CK_Material_QtdePadrao2
                CHECK (QtdePadrao2 IS NULL OR QtdePadrao2 > 0);');
    END;

    DECLARE @MenuPaiId INT = 29;
    DECLARE @MenuId INT;

    SELECT @MenuId = Id
    FROM dbo.AppMenu
    WHERE IdNivelSup = @MenuPaiId
      AND Area = 'ConfiguracaoApp'
      AND Controller = 'Material'
      AND Action = 'Index';

    IF @MenuId IS NULL
    BEGIN
        INSERT INTO dbo.AppMenu
        (
            Titulo, Area, Controller, Action, Css, Status,
            Sequencia, Nivel, IdNivelSup, HasChild, DatUltAtlz
        )
        VALUES
        (
            'Materiais',
            'ConfiguracaoApp',
            'Material',
            'Index',
            'fa-solid fa-boxes-stacked',
            1,
            250,
            2,
            @MenuPaiId,
            0,
            GETDATE()
        );

        SET @MenuId = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE dbo.AppMenu
        SET Titulo = 'Materiais',
            Css = 'fa-solid fa-boxes-stacked',
            Status = 1,
            Sequencia = 250,
            Nivel = 2,
            IdNivelSup = @MenuPaiId,
            HasChild = 0,
            DatUltAtlz = GETDATE()
        WHERE Id = @MenuId;
    END;

    DECLARE @Funcoes TABLE
    (
        Codigo VARCHAR(20),
        Descricao VARCHAR(100),
        Action VARCHAR(30)
    );

    INSERT INTO @Funcoes (Codigo, Descricao, Action)
    VALUES
        ('add', 'Incluir material', 'Create'),
        ('update', 'Editar material', 'Edit'),
        ('view', 'Visualizar material', 'Details'),
        ('delete', 'Excluir material', 'Delete');

    MERGE dbo.AppFuncao AS Destino
    USING @Funcoes AS Origem
       ON Destino.IdMenu = @MenuId
      AND Destino.Controller = 'Material'
      AND Destino.Action = Origem.Action
    WHEN MATCHED THEN
        UPDATE SET
            Destino.Codigo = Origem.Codigo,
            Destino.DescPTBR = Origem.Descricao,
            Destino.CodComponente = 'Material',
            Destino.Status = 1
    WHEN NOT MATCHED THEN
        INSERT
        (
            Codigo, DescPTBR, CodComponente, IdMenu,
            Status, Controller, Action
        )
        VALUES
        (
            Origem.Codigo, Origem.Descricao, 'Material', @MenuId,
            1, 'Material', Origem.Action
        );

    DECLARE @EquipamentoMenuId INT;

    SELECT @EquipamentoMenuId = Id
    FROM dbo.AppMenu
    WHERE IdNivelSup = @MenuPaiId
      AND Area = 'ConfiguracaoApp'
      AND Controller = 'Equipamento'
      AND Action = 'Index';

    IF @EquipamentoMenuId IS NULL
    BEGIN
        INSERT INTO dbo.AppMenu
        (
            Titulo, Area, Controller, Action, Css, Status,
            Sequencia, Nivel, IdNivelSup, HasChild, DatUltAtlz
        )
        VALUES
        (
            'Equipamentos',
            'ConfiguracaoApp',
            'Equipamento',
            'Index',
            'fa-solid fa-cubes',
            1,
            260,
            2,
            @MenuPaiId,
            0,
            GETDATE()
        );

        SET @EquipamentoMenuId = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE dbo.AppMenu
        SET Titulo = 'Equipamentos',
            Css = 'fa-solid fa-cubes',
            Status = 1,
            Sequencia = 260,
            Nivel = 2,
            IdNivelSup = @MenuPaiId,
            HasChild = 0,
            DatUltAtlz = GETDATE()
        WHERE Id = @EquipamentoMenuId;
    END;

    MERGE dbo.AppFuncao AS Destino
    USING @Funcoes AS Origem
       ON Destino.IdMenu = @EquipamentoMenuId
      AND Destino.Controller = 'Equipamento'
      AND Destino.Action = Origem.Action
    WHEN MATCHED THEN
        UPDATE SET
            Destino.Codigo = Origem.Codigo,
            Destino.DescPTBR = REPLACE(Origem.Descricao, 'material', 'equipamento'),
            Destino.CodComponente = 'Equipamento',
            Destino.Status = 1
    WHEN NOT MATCHED THEN
        INSERT
        (
            Codigo, DescPTBR, CodComponente, IdMenu,
            Status, Controller, Action
        )
        VALUES
        (
            Origem.Codigo,
            REPLACE(Origem.Descricao, 'material', 'equipamento'),
            'Equipamento',
            @EquipamentoMenuId,
            1,
            'Equipamento',
            Origem.Action
        );

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;
GO

SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = 'dbo'
  AND TABLE_NAME = 'Material'
  AND COLUMN_NAME IN
  (
      'CategoriaProduto',
      'Zona1Id',
      'Eqpto1Id',
      'QtdePadrao1',
      'Zona2Id',
      'Eqpto2Id',
      'QtdePadrao2'
  )
ORDER BY ORDINAL_POSITION;

SELECT Id, Titulo, Area, Controller, Action, Sequencia, IdNivelSup, Status
FROM dbo.AppMenu
WHERE Area = 'ConfiguracaoApp'
  AND Controller IN ('Material', 'Equipamento')
  AND Action = 'Index';
GO
