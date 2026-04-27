IF DB_ID(N'AutoCarShowroomDb') IS NULL
BEGIN
    CREATE DATABASE AutoCarShowroomDb;
END;
GO

USE AutoCarShowroomDb;
GO

IF OBJECT_ID(N'dbo.Brands', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Brands
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(100) NOT NULL,
        CONSTRAINT CK_Brands_Name_NotBlank CHECK (LEN(LTRIM(RTRIM(Name))) > 0)
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes i
    INNER JOIN sys.index_columns ic
        ON ic.object_id = i.object_id
        AND ic.index_id = i.index_id
    INNER JOIN sys.columns c
        ON c.object_id = ic.object_id
        AND c.column_id = ic.column_id
    WHERE i.object_id = OBJECT_ID(N'dbo.Brands')
      AND i.is_unique = 1
      AND ic.key_ordinal = 1
      AND c.name = N'Name'
      AND NOT EXISTS
      (
          SELECT 1
          FROM sys.index_columns ic2
          WHERE ic2.object_id = i.object_id
            AND ic2.index_id = i.index_id
            AND ic2.key_ordinal > 1
      )
)
AND NOT EXISTS
(
    SELECT 1
    FROM dbo.Brands
    GROUP BY Name
    HAVING COUNT(*) > 1
)
BEGIN
    CREATE UNIQUE INDEX UX_Brands_Name ON dbo.Brands (Name);
END;
GO

IF OBJECT_ID(N'dbo.Cars', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Cars
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        BrandId INT NOT NULL,
        Name NVARCHAR(150) NOT NULL,
        [Year] INT NULL,
        [Type] NVARCHAR(50) NULL,
        Color NVARCHAR(50) NULL,
        [Description] NVARCHAR(1000) NULL,
        Specifications NVARCHAR(MAX) NULL,
        ImageUrls NVARCHAR(MAX) NULL,
        Status NVARCHAR(20) NOT NULL DEFAULT N'InStock',
        Price DECIMAL(18,2) NOT NULL,
        StockQuantity INT NOT NULL DEFAULT 0,
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT CK_Cars_Name_NotBlank CHECK (LEN(LTRIM(RTRIM(Name))) > 0),
        CONSTRAINT CK_Cars_Year CHECK ([Year] IS NULL OR ([Year] BETWEEN 1900 AND 2100)),
        CONSTRAINT CK_Cars_Price CHECK (Price >= 0),
        CONSTRAINT CK_Cars_StockQuantity CHECK (StockQuantity >= 0)
    );
END;
GO

IF OBJECT_ID(N'dbo.Cars', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.Cars', N'Year') IS NULL
BEGIN
    ALTER TABLE dbo.Cars ADD [Year] INT NULL;
END;
GO

IF OBJECT_ID(N'dbo.Cars', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.Cars', N'Type') IS NULL
BEGIN
    ALTER TABLE dbo.Cars ADD [Type] NVARCHAR(50) NULL;
END;
GO

IF OBJECT_ID(N'dbo.Cars', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.Cars', N'Color') IS NULL
BEGIN
    ALTER TABLE dbo.Cars ADD Color NVARCHAR(50) NULL;
END;
GO

IF OBJECT_ID(N'dbo.Cars', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.Cars', N'Description') IS NULL
BEGIN
    ALTER TABLE dbo.Cars ADD [Description] NVARCHAR(1000) NULL;
END;
GO

IF OBJECT_ID(N'dbo.Cars', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.Cars', N'ImageUrls') IS NULL
BEGIN
    ALTER TABLE dbo.Cars ADD ImageUrls NVARCHAR(MAX) NULL;
END;
GO

IF OBJECT_ID(N'dbo.Cars', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.Cars', N'Specifications') IS NULL
BEGIN
    ALTER TABLE dbo.Cars ADD Specifications NVARCHAR(MAX) NULL;
END;
GO

IF OBJECT_ID(N'dbo.Cars', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.Cars', N'Status') IS NULL
BEGIN
    ALTER TABLE dbo.Cars ADD Status NVARCHAR(20) NOT NULL CONSTRAINT DF_Cars_Status DEFAULT N'InStock';
END;
GO

IF OBJECT_ID(N'dbo.Orders', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Orders
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        CustomerName NVARCHAR(150) NOT NULL,
        CustomerPhone NVARCHAR(30) NULL,
        CustomerEmail NVARCHAR(254) NULL,
        CustomerAddress NVARCHAR(300) NULL,
        Note NVARCHAR(500) NULL,
        Status NVARCHAR(50) NOT NULL DEFAULT N'Pending',
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT CK_Orders_CustomerName_NotBlank CHECK (LEN(LTRIM(RTRIM(CustomerName))) > 0),
        CONSTRAINT CK_Orders_Status_Valid CHECK (Status IN (N'Pending', N'Paid', N'Completed', N'Delivered', N'Cancelled'))
    );
END;
GO

IF OBJECT_ID(N'dbo.Orders', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.Orders', N'CustomerPhone') IS NULL
BEGIN
    ALTER TABLE dbo.Orders ADD CustomerPhone NVARCHAR(30) NULL;
END;
GO

IF OBJECT_ID(N'dbo.Orders', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.Orders', N'CustomerEmail') IS NULL
BEGIN
    ALTER TABLE dbo.Orders ADD CustomerEmail NVARCHAR(254) NULL;
END;
GO

IF OBJECT_ID(N'dbo.Orders', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.Orders', N'CustomerAddress') IS NULL
BEGIN
    ALTER TABLE dbo.Orders ADD CustomerAddress NVARCHAR(300) NULL;
END;
GO

IF OBJECT_ID(N'dbo.Orders', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.Orders', N'Note') IS NULL
BEGIN
    ALTER TABLE dbo.Orders ADD Note NVARCHAR(500) NULL;
END;
GO

IF OBJECT_ID(N'dbo.OrderItems', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.OrderItems
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        OrderId INT NOT NULL,
        CarId INT NOT NULL,
        Quantity INT NOT NULL,
        UnitPrice DECIMAL(18,2) NOT NULL,
        CONSTRAINT CK_OrderItems_Quantity CHECK (Quantity > 0),
        CONSTRAINT CK_OrderItems_UnitPrice CHECK (UnitPrice >= 0)
    );
END;
GO

IF OBJECT_ID(N'dbo.StaffUsers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.StaffUsers
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Username NVARCHAR(100) NOT NULL,
        PasswordHash NVARCHAR(500) NOT NULL,
        DisplayName NVARCHAR(150) NOT NULL,
        Role NVARCHAR(50) NOT NULL DEFAULT N'Staff',
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT CK_StaffUsers_Username_NotBlank CHECK (LEN(LTRIM(RTRIM(Username))) > 0),
        CONSTRAINT CK_StaffUsers_DisplayName_NotBlank CHECK (LEN(LTRIM(RTRIM(DisplayName))) > 0),
        CONSTRAINT CK_StaffUsers_Role_Valid CHECK (Role IN (N'Administrator', N'Staff'))
    );
END;
GO

IF OBJECT_ID(N'dbo.StaffUsers', N'U') IS NOT NULL
BEGIN
    DECLARE @usernameType SYSNAME =
        (
            SELECT TOP (1) t.name
            FROM sys.columns c
            INNER JOIN sys.types t
                ON t.user_type_id = c.user_type_id
                AND t.system_type_id = c.system_type_id
            WHERE c.object_id = OBJECT_ID(N'dbo.StaffUsers')
              AND c.name = N'Username'
        );

    DECLARE @usernameMaxLen SMALLINT =
        (
            SELECT TOP (1) c.max_length
            FROM sys.columns c
            WHERE c.object_id = OBJECT_ID(N'dbo.StaffUsers')
              AND c.name = N'Username'
        );

    IF (@usernameType IN (N'nvarchar', N'varchar') AND @usernameMaxLen = -1)
    BEGIN
        IF EXISTS (SELECT 1 FROM dbo.StaffUsers WHERE LEN(Username) > 100)
        BEGIN
            PRINT N'Warning: StaffUsers.Username is (n)varchar(max) and has values > 100 chars. Cannot alter to NVARCHAR(100); unique index will be skipped.';
        END
        ELSE
        BEGIN
            ALTER TABLE dbo.StaffUsers ALTER COLUMN Username NVARCHAR(100) NOT NULL;
        END
    END

    IF COL_LENGTH(N'dbo.StaffUsers', N'PasswordHash') IS NOT NULL
       AND EXISTS
       (
           SELECT 1
           FROM sys.columns c
           WHERE c.object_id = OBJECT_ID(N'dbo.StaffUsers')
             AND c.name = N'PasswordHash'
             AND c.max_length = -1
       )
    BEGIN
        ALTER TABLE dbo.StaffUsers ALTER COLUMN PasswordHash NVARCHAR(500) NOT NULL;
    END

    IF COL_LENGTH(N'dbo.StaffUsers', N'DisplayName') IS NOT NULL
       AND EXISTS
       (
           SELECT 1
           FROM sys.columns c
           WHERE c.object_id = OBJECT_ID(N'dbo.StaffUsers')
             AND c.name = N'DisplayName'
             AND c.max_length = -1
       )
    BEGIN
        ALTER TABLE dbo.StaffUsers ALTER COLUMN DisplayName NVARCHAR(150) NOT NULL;
    END
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes i
    INNER JOIN sys.index_columns ic
        ON ic.object_id = i.object_id
        AND ic.index_id = i.index_id
    INNER JOIN sys.columns c
        ON c.object_id = ic.object_id
        AND c.column_id = ic.column_id
    WHERE i.object_id = OBJECT_ID(N'dbo.StaffUsers')
      AND i.is_unique = 1
      AND ic.key_ordinal = 1
      AND c.name = N'Username'
      AND NOT EXISTS
      (
          SELECT 1
          FROM sys.index_columns ic2
          WHERE ic2.object_id = i.object_id
            AND ic2.index_id = i.index_id
            AND ic2.key_ordinal > 1
      )
)
AND NOT EXISTS
(
    SELECT 1
    FROM dbo.StaffUsers
    GROUP BY Username
    HAVING COUNT(*) > 1
)
AND EXISTS
(
    SELECT 1
    FROM sys.columns c
    WHERE c.object_id = OBJECT_ID(N'dbo.StaffUsers')
      AND c.name = N'Username'
      AND c.max_length <> -1
      AND c.system_type_id IN (231, 167)
)
BEGIN
    CREATE UNIQUE INDEX UX_StaffUsers_Username ON dbo.StaffUsers (Username);
END;
GO

IF OBJECT_ID(N'dbo.AuditLogs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AuditLogs
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Username NVARCHAR(100) NOT NULL,
        DisplayName NVARCHAR(150) NOT NULL,
        Role NVARCHAR(50) NOT NULL,
        Action NVARCHAR(100) NOT NULL,
        EntityType NVARCHAR(100) NOT NULL,
        EntityId INT NULL,
        Description NVARCHAR(500) NOT NULL,
        IpAddress NVARCHAR(64) NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_AuditLogs_CreatedAt'
      AND object_id = OBJECT_ID(N'dbo.AuditLogs')
)
BEGIN
    CREATE INDEX IX_AuditLogs_CreatedAt ON dbo.AuditLogs (CreatedAt DESC);
END;
GO

IF OBJECT_ID(N'dbo.Cars', N'U') IS NOT NULL
   AND NOT EXISTS
   (
       SELECT 1
       FROM sys.foreign_keys
       WHERE parent_object_id = OBJECT_ID(N'dbo.Cars')
         AND referenced_object_id = OBJECT_ID(N'dbo.Brands')
   )
BEGIN
    ALTER TABLE dbo.Cars
    ADD CONSTRAINT FK_Cars_Brands FOREIGN KEY (BrandId) REFERENCES dbo.Brands(Id);
END;
GO

IF OBJECT_ID(N'dbo.OrderItems', N'U') IS NOT NULL
   AND NOT EXISTS
   (
       SELECT 1
       FROM sys.foreign_keys
       WHERE parent_object_id = OBJECT_ID(N'dbo.OrderItems')
         AND referenced_object_id = OBJECT_ID(N'dbo.Orders')
   )
BEGIN
    ALTER TABLE dbo.OrderItems
    ADD CONSTRAINT FK_OrderItems_Orders FOREIGN KEY (OrderId) REFERENCES dbo.Orders(Id);
END;
GO

IF OBJECT_ID(N'dbo.OrderItems', N'U') IS NOT NULL
   AND NOT EXISTS
   (
       SELECT 1
       FROM sys.foreign_keys
       WHERE parent_object_id = OBJECT_ID(N'dbo.OrderItems')
         AND referenced_object_id = OBJECT_ID(N'dbo.Cars')
   )
BEGIN
    ALTER TABLE dbo.OrderItems
    ADD CONSTRAINT FK_OrderItems_Cars FOREIGN KEY (CarId) REFERENCES dbo.Cars(Id);
END;
GO

IF OBJECT_ID(N'dbo.Brands', N'U') IS NOT NULL
   AND NOT EXISTS
   (
       SELECT 1
       FROM sys.check_constraints
       WHERE name = N'CK_Brands_Name_NotBlank'
         AND parent_object_id = OBJECT_ID(N'dbo.Brands')
   )
BEGIN
    ALTER TABLE dbo.Brands
    ADD CONSTRAINT CK_Brands_Name_NotBlank CHECK (LEN(LTRIM(RTRIM(Name))) > 0);
END;
GO

IF OBJECT_ID(N'dbo.Cars', N'U') IS NOT NULL
   AND NOT EXISTS
   (
       SELECT 1
       FROM sys.check_constraints
       WHERE name = N'CK_Cars_Name_NotBlank'
         AND parent_object_id = OBJECT_ID(N'dbo.Cars')
   )
BEGIN
    ALTER TABLE dbo.Cars
    ADD CONSTRAINT CK_Cars_Name_NotBlank CHECK (LEN(LTRIM(RTRIM(Name))) > 0);
END;
GO

IF OBJECT_ID(N'dbo.Cars', N'U') IS NOT NULL
   AND NOT EXISTS
   (
       SELECT 1
       FROM sys.check_constraints
       WHERE name = N'CK_Cars_Year'
         AND parent_object_id = OBJECT_ID(N'dbo.Cars')
   )
BEGIN
    ALTER TABLE dbo.Cars
    ADD CONSTRAINT CK_Cars_Year CHECK ([Year] IS NULL OR ([Year] BETWEEN 1900 AND 2100));
END;
GO

IF OBJECT_ID(N'dbo.Cars', N'U') IS NOT NULL
   AND NOT EXISTS
   (
       SELECT 1
       FROM sys.check_constraints
       WHERE name = N'CK_Cars_Status_Valid'
         AND parent_object_id = OBJECT_ID(N'dbo.Cars')
   )
BEGIN
    ALTER TABLE dbo.Cars
    ADD CONSTRAINT CK_Cars_Status_Valid CHECK (Status IN (N'InStock', N'Sold', N'Promotion'));
END;
GO

IF OBJECT_ID(N'dbo.Cars', N'U') IS NOT NULL
   AND NOT EXISTS
   (
       SELECT 1
       FROM sys.check_constraints
       WHERE name = N'CK_Cars_Price'
         AND parent_object_id = OBJECT_ID(N'dbo.Cars')
   )
BEGIN
    ALTER TABLE dbo.Cars
    ADD CONSTRAINT CK_Cars_Price CHECK (Price >= 0);
END;
GO

IF OBJECT_ID(N'dbo.Cars', N'U') IS NOT NULL
   AND NOT EXISTS
   (
       SELECT 1
       FROM sys.check_constraints
       WHERE name = N'CK_Cars_StockQuantity'
         AND parent_object_id = OBJECT_ID(N'dbo.Cars')
   )
BEGIN
    ALTER TABLE dbo.Cars
    ADD CONSTRAINT CK_Cars_StockQuantity CHECK (StockQuantity >= 0);
END;
GO

IF OBJECT_ID(N'dbo.Orders', N'U') IS NOT NULL
   AND NOT EXISTS
   (
       SELECT 1
       FROM sys.check_constraints
       WHERE name = N'CK_Orders_CustomerName_NotBlank'
         AND parent_object_id = OBJECT_ID(N'dbo.Orders')
   )
BEGIN
    ALTER TABLE dbo.Orders
    ADD CONSTRAINT CK_Orders_CustomerName_NotBlank CHECK (LEN(LTRIM(RTRIM(CustomerName))) > 0);
END;
GO

IF OBJECT_ID(N'dbo.Orders', N'U') IS NOT NULL
   AND NOT EXISTS
   (
       SELECT 1
       FROM sys.check_constraints
       WHERE name = N'CK_Orders_Status_Valid'
         AND parent_object_id = OBJECT_ID(N'dbo.Orders')
   )
BEGIN
    ALTER TABLE dbo.Orders
    ADD CONSTRAINT CK_Orders_Status_Valid CHECK (Status IN (N'Pending', N'Paid', N'Completed', N'Delivered', N'Cancelled'));
END;
GO

IF OBJECT_ID(N'dbo.OrderItems', N'U') IS NOT NULL
   AND NOT EXISTS
   (
       SELECT 1
       FROM sys.check_constraints
       WHERE name = N'CK_OrderItems_Quantity'
         AND parent_object_id = OBJECT_ID(N'dbo.OrderItems')
   )
BEGIN
    ALTER TABLE dbo.OrderItems
    ADD CONSTRAINT CK_OrderItems_Quantity CHECK (Quantity > 0);
END;
GO

IF OBJECT_ID(N'dbo.OrderItems', N'U') IS NOT NULL
   AND NOT EXISTS
   (
       SELECT 1
       FROM sys.check_constraints
       WHERE name = N'CK_OrderItems_UnitPrice'
         AND parent_object_id = OBJECT_ID(N'dbo.OrderItems')
   )
BEGIN
    ALTER TABLE dbo.OrderItems
    ADD CONSTRAINT CK_OrderItems_UnitPrice CHECK (UnitPrice >= 0);
END;
GO
