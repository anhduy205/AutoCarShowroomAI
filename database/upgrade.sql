IF DB_ID(N'AutoCarShowroomDb') IS NULL
BEGIN
    CREATE DATABASE AutoCarShowroomDb;
END;
GO

USE AutoCarShowroomDb;
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET ARITHABORT ON;
SET NUMERIC_ROUNDABORT OFF;
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

IF OBJECT_ID(N'dbo.Branches', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Branches
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        BranchCode NVARCHAR(30) NOT NULL,
        Name NVARCHAR(150) NOT NULL,
        Status NVARCHAR(20) NOT NULL DEFAULT N'Active',
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT CK_Branches_Code_NotBlank CHECK (LEN(LTRIM(RTRIM(BranchCode))) > 0),
        CONSTRAINT CK_Branches_Name_NotBlank CHECK (LEN(LTRIM(RTRIM(Name))) > 0),
        CONSTRAINT CK_Branches_Status_Valid CHECK (Status IN (N'Active', N'Inactive'))
    );
END;
GO

IF OBJECT_ID(N'dbo.StaffUsers', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.StaffUsers', N'BranchId') IS NULL
BEGIN
    ALTER TABLE dbo.StaffUsers ADD BranchId INT NULL;
END;
GO

IF OBJECT_ID(N'dbo.StaffUsers', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.StaffUsers', N'StaffCode') IS NULL
BEGIN
    ALTER TABLE dbo.StaffUsers ADD StaffCode NVARCHAR(30) NULL;
END;
GO

IF OBJECT_ID(N'dbo.StaffUsers', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.StaffUsers', N'Email') IS NULL
BEGIN
    ALTER TABLE dbo.StaffUsers ADD Email NVARCHAR(254) NULL;
END;
GO

IF OBJECT_ID(N'dbo.StaffUsers', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.StaffUsers', N'Phone') IS NULL
BEGIN
    ALTER TABLE dbo.StaffUsers ADD Phone NVARCHAR(30) NULL;
END;
GO

IF OBJECT_ID(N'dbo.StaffUsers', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.StaffUsers', N'IsActive') IS NULL
BEGIN
    ALTER TABLE dbo.StaffUsers ADD IsActive BIT NOT NULL CONSTRAINT DF_StaffUsers_IsActive DEFAULT 1;
END;
GO

IF OBJECT_ID(N'dbo.StaffUsers', N'U') IS NOT NULL
   AND NOT EXISTS
   (
       SELECT 1
       FROM sys.foreign_keys
       WHERE parent_object_id = OBJECT_ID(N'dbo.StaffUsers')
         AND name = N'FK_StaffUsers_Branches'
   )
BEGIN
    ALTER TABLE dbo.StaffUsers
    ADD CONSTRAINT FK_StaffUsers_Branches FOREIGN KEY (BranchId) REFERENCES dbo.Branches(Id);
END;
GO

IF OBJECT_ID(N'dbo.StaffUsers', N'U') IS NOT NULL
   AND NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'UX_StaffUsers_StaffCode_NotNull'
      AND object_id = OBJECT_ID(N'dbo.StaffUsers')
)
BEGIN
    CREATE UNIQUE INDEX UX_StaffUsers_StaffCode_NotNull ON dbo.StaffUsers (StaffCode) WHERE StaffCode IS NOT NULL;
END;
GO

IF OBJECT_ID(N'dbo.StaffUsers', N'U') IS NOT NULL
   AND NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'UX_StaffUsers_Email_NotNull'
      AND object_id = OBJECT_ID(N'dbo.StaffUsers')
)
BEGIN
    CREATE UNIQUE INDEX UX_StaffUsers_Email_NotNull ON dbo.StaffUsers (Email) WHERE Email IS NOT NULL;
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'UX_Branches_BranchCode'
      AND object_id = OBJECT_ID(N'dbo.Branches')
)
BEGIN
    CREATE UNIQUE INDEX UX_Branches_BranchCode ON dbo.Branches (BranchCode);
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

IF OBJECT_ID(N'dbo.Customers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Customers
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        CustomerCode NVARCHAR(30) NOT NULL,
        FullName NVARCHAR(150) NOT NULL,
        Phone NVARCHAR(30) NULL,
        Email NVARCHAR(254) NULL,
        TaxCode NVARCHAR(50) NULL,
        Address NVARCHAR(300) NULL,
        CustomerType NVARCHAR(20) NOT NULL DEFAULT N'Individual',
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT CK_Customers_Code_NotBlank CHECK (LEN(LTRIM(RTRIM(CustomerCode))) > 0),
        CONSTRAINT CK_Customers_FullName_NotBlank CHECK (LEN(LTRIM(RTRIM(FullName))) > 0),
        CONSTRAINT CK_Customers_Type_Valid CHECK (CustomerType IN (N'Individual', N'Company'))
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'UX_Customers_CustomerCode'
      AND object_id = OBJECT_ID(N'dbo.Customers')
)
BEGIN
    CREATE UNIQUE INDEX UX_Customers_CustomerCode ON dbo.Customers (CustomerCode);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'UX_Customers_Email_NotNull'
      AND object_id = OBJECT_ID(N'dbo.Customers')
)
BEGIN
    CREATE UNIQUE INDEX UX_Customers_Email_NotNull ON dbo.Customers (Email) WHERE Email IS NOT NULL;
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_Customers_Phone'
      AND object_id = OBJECT_ID(N'dbo.Customers')
)
BEGIN
    CREATE INDEX IX_Customers_Phone ON dbo.Customers (Phone) WHERE Phone IS NOT NULL;
END;
GO

IF OBJECT_ID(N'dbo.CustomerRequests', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CustomerRequests
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        RequestType NVARCHAR(30) NOT NULL,
        CarId INT NULL,
        CustomerName NVARCHAR(150) NOT NULL,
        CustomerPhone NVARCHAR(30) NULL,
        CustomerEmail NVARCHAR(254) NULL,
        PreferredTime DATETIME2 NULL,
        DepositAmount DECIMAL(18,2) NULL,
        Note NVARCHAR(500) NULL,
        Status NVARCHAR(30) NOT NULL DEFAULT N'Pending',
        NotificationChannel NVARCHAR(30) NULL,
        NotificationSentAt DATETIME2 NULL,
        NotificationMessage NVARCHAR(500) NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        ConfirmedAt DATETIME2 NULL,
        CONSTRAINT CK_CustomerRequests_Type CHECK (RequestType IN (N'ViewCar', N'Consultation', N'Deposit', N'TestDrive')),
        CONSTRAINT CK_CustomerRequests_Status CHECK (Status IN (N'Pending', N'Confirmed', N'Cancelled')),
        CONSTRAINT CK_CustomerRequests_CustomerName_NotBlank CHECK (LEN(LTRIM(RTRIM(CustomerName))) > 0),
        CONSTRAINT CK_CustomerRequests_Contact CHECK (CustomerPhone IS NOT NULL OR CustomerEmail IS NOT NULL),
        CONSTRAINT CK_CustomerRequests_DepositAmount CHECK (DepositAmount IS NULL OR DepositAmount >= 0)
    );
END;
GO

IF OBJECT_ID(N'dbo.CustomerRequests', N'U') IS NOT NULL
   AND NOT EXISTS
   (
       SELECT 1
       FROM sys.foreign_keys
       WHERE parent_object_id = OBJECT_ID(N'dbo.CustomerRequests')
         AND referenced_object_id = OBJECT_ID(N'dbo.Cars')
   )
BEGIN
    ALTER TABLE dbo.CustomerRequests
    ADD CONSTRAINT FK_CustomerRequests_Cars FOREIGN KEY (CarId) REFERENCES dbo.Cars(Id);
END;
GO

IF OBJECT_ID(N'dbo.Roles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Roles
    (
        RoleCode NVARCHAR(50) NOT NULL PRIMARY KEY,
        RoleName NVARCHAR(150) NOT NULL,
        CONSTRAINT CK_Roles_Code_NotBlank CHECK (LEN(LTRIM(RTRIM(RoleCode))) > 0),
        CONSTRAINT CK_Roles_Name_NotBlank CHECK (LEN(LTRIM(RTRIM(RoleName))) > 0)
    );
END;
GO

IF OBJECT_ID(N'dbo.StaffUsers', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.StaffUsers', N'CreatedAt') IS NULL
BEGIN
    ALTER TABLE dbo.StaffUsers
    ADD CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_StaffUsers_CreatedAt DEFAULT SYSUTCDATETIME();
END;
GO

IF OBJECT_ID(N'dbo.StaffUsers', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.StaffUsers', N'CreatedAt') IS NOT NULL
BEGIN
    UPDATE dbo.StaffUsers
    SET CreatedAt = SYSUTCDATETIME()
    WHERE CreatedAt IS NULL;

    IF EXISTS
    (
        SELECT 1
        FROM sys.columns
        WHERE object_id = OBJECT_ID(N'dbo.StaffUsers')
          AND name = N'CreatedAt'
          AND is_nullable = 1
    )
    BEGIN
        ALTER TABLE dbo.StaffUsers ALTER COLUMN CreatedAt DATETIME2 NOT NULL;
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.default_constraints dc
        INNER JOIN sys.columns c
            ON c.object_id = dc.parent_object_id
            AND c.column_id = dc.parent_column_id
        WHERE dc.parent_object_id = OBJECT_ID(N'dbo.StaffUsers')
          AND c.name = N'CreatedAt'
    )
    BEGIN
        ALTER TABLE dbo.StaffUsers
        ADD CONSTRAINT DF_StaffUsers_CreatedAt DEFAULT SYSUTCDATETIME() FOR CreatedAt;
    END;
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_CustomerRequests_Status_CreatedAt'
      AND object_id = OBJECT_ID(N'dbo.CustomerRequests')
)
BEGIN
    CREATE INDEX IX_CustomerRequests_Status_CreatedAt ON dbo.CustomerRequests (Status, CreatedAt DESC);
END;
GO

IF OBJECT_ID(N'dbo.StaffUsers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.StaffUsers
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        BranchId INT NULL,
        StaffCode NVARCHAR(30) NULL,
        Username NVARCHAR(100) NOT NULL,
        PasswordHash NVARCHAR(500) NOT NULL,
        DisplayName NVARCHAR(150) NOT NULL,
        Role NVARCHAR(50) NOT NULL DEFAULT N'Staff',
        Email NVARCHAR(254) NULL,
        Phone NVARCHAR(30) NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_StaffUsers_Branches FOREIGN KEY (BranchId) REFERENCES dbo.Branches(Id),
        CONSTRAINT CK_StaffUsers_StaffCode_NotBlank CHECK (StaffCode IS NULL OR LEN(LTRIM(RTRIM(StaffCode))) > 0),
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

IF OBJECT_ID(N'dbo.StaffUsers', N'U') IS NOT NULL
   AND NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'UX_StaffUsers_StaffCode_NotNull'
      AND object_id = OBJECT_ID(N'dbo.StaffUsers')
)
BEGIN
    CREATE UNIQUE INDEX UX_StaffUsers_StaffCode_NotNull ON dbo.StaffUsers (StaffCode) WHERE StaffCode IS NOT NULL;
END;
GO

IF OBJECT_ID(N'dbo.StaffUsers', N'U') IS NOT NULL
   AND NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'UX_StaffUsers_Email_NotNull'
      AND object_id = OBJECT_ID(N'dbo.StaffUsers')
)
BEGIN
    CREATE UNIQUE INDEX UX_StaffUsers_Email_NotNull ON dbo.StaffUsers (Email) WHERE Email IS NOT NULL;
END;
GO

IF OBJECT_ID(N'dbo.StaffUserRoles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.StaffUserRoles
    (
        StaffUserId INT NOT NULL,
        RoleCode NVARCHAR(50) NOT NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_StaffUserRoles PRIMARY KEY (StaffUserId, RoleCode),
        CONSTRAINT FK_StaffUserRoles_StaffUsers FOREIGN KEY (StaffUserId) REFERENCES dbo.StaffUsers(Id) ON DELETE CASCADE,
        CONSTRAINT FK_StaffUserRoles_Roles FOREIGN KEY (RoleCode) REFERENCES dbo.Roles(RoleCode)
    );
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

MERGE dbo.Branches AS target
USING (VALUES
    (N'HCM-01', N'Showroom Quận 1', N'Active'),
    (N'HN-01', N'Showroom Hà Nội', N'Active'),
    (N'DN-01', N'Showroom Đà Nẵng', N'Inactive')
) AS source (BranchCode, Name, Status)
ON target.BranchCode = source.BranchCode
WHEN MATCHED THEN
    UPDATE SET Name = source.Name, Status = source.Status
WHEN NOT MATCHED THEN
    INSERT (BranchCode, Name, Status)
    VALUES (source.BranchCode, source.Name, source.Status);
GO

MERGE dbo.Roles AS target
USING (VALUES
    (N'Administrator', N'Quản trị viên'),
    (N'Staff', N'Nhân viên'),
    (N'Sales', N'Tư vấn bán hàng'),
    (N'SalesManager', N'Quản lý bán hàng'),
    (N'ServiceAdvisor', N'Cố vấn dịch vụ'),
    (N'Technician', N'Kỹ thuật viên'),
    (N'PartsWarehouse', N'Kho phụ tùng'),
    (N'Accountant', N'Kế toán'),
    (N'InventoryManager', N'Quản lý tồn kho')
) AS source (RoleCode, RoleName)
ON target.RoleCode = source.RoleCode
WHEN MATCHED THEN
    UPDATE SET RoleName = source.RoleName
WHEN NOT MATCHED THEN
    INSERT (RoleCode, RoleName)
    VALUES (source.RoleCode, source.RoleName);
GO

MERGE dbo.Customers AS target
USING (VALUES
    (N'CUST0001', N'Phạm Thị Mai', N'0901000001', N'mai.pham@example.com', NULL, N'Quận 1, TP HCM', N'Individual'),
    (N'CUST0002', N'Đặng Minh Đức', N'0901000002', N'duc.dang@example.com', NULL, N'Quận Hai Bà Trưng, Hà Nội', N'Individual'),
    (N'CUST0003', N'Công ty Vận tải An Phát', N'0901000003', N'contact@anphat.example.com', N'0312345678', N'Thủ Đức, TP HCM', N'Company'),
    (N'CUST0004', N'Bùi Ngọc Anh', N'0901000004', N'anh.bui@example.com', NULL, N'Quận Thanh Khê, Đà Nẵng', N'Individual'),
    (N'CUST0005', N'Hồ Thị Lan', N'0901000005', N'lan.ho@example.com', NULL, N'Nha Trang, Khánh Hòa', N'Individual')
) AS source (CustomerCode, FullName, Phone, Email, TaxCode, Address, CustomerType)
ON target.CustomerCode = source.CustomerCode
WHEN MATCHED THEN
    UPDATE SET FullName = source.FullName,
               Phone = source.Phone,
               Email = source.Email,
               TaxCode = source.TaxCode,
               Address = source.Address,
               CustomerType = source.CustomerType
WHEN NOT MATCHED THEN
    INSERT (CustomerCode, FullName, Phone, Email, TaxCode, Address, CustomerType)
    VALUES (source.CustomerCode, source.FullName, source.Phone, source.Email, source.TaxCode, source.Address, source.CustomerType);
GO

MERGE dbo.Brands AS target
USING (VALUES
    (N'Toyota'),
    (N'Hyundai'),
    (N'Ford'),
    (N'Mazda')
) AS source (Name)
ON target.Name = source.Name
WHEN NOT MATCHED THEN
    INSERT (Name) VALUES (source.Name);
GO

MERGE dbo.Cars AS target
USING (VALUES
    (N'Toyota', N'Toyota Vios', 2024, N'Sedan', N'Bạc', N'Sedan hạng B gọn gàng, tiết kiệm nhiên liệu, phù hợp đi phố.', N'Động cơ: 1.5L\nHộp số: CVT\nSố chỗ: 5\nNhiên liệu: Xăng', N'InStock', 489000000, 6),
    (N'Toyota', N'Toyota Fortuner', 2024, N'SUV', N'Đen', N'SUV 7 chỗ khung gầm chắc chắn, phù hợp gia đình và du lịch.', N'Động cơ: 2.4L\nHộp số: AT\nSố chỗ: 7\nNhiên liệu: Dầu', N'InStock', 1185000000, 2),
    (N'Toyota', N'Toyota Innova Cross', 2024, N'MPV', N'Trắng', N'MPV lai crossover, khoang nội thất rộng và linh hoạt.', N'Động cơ: 2.0L\nHộp số: CVT\nSố chỗ: 7\nNhiên liệu: Xăng', N'Promotion', 810000000, 3),
    (N'Toyota', N'Toyota Raize', 2023, N'SUV', N'Đỏ', N'SUV đô thị nhỏ gọn, dễ lái trong đường đông.', N'Động cơ: 1.0L Turbo\nHộp số: CVT\nSố chỗ: 5\nNhiên liệu: Xăng', N'InStock', 552000000, 5),
    (N'Hyundai', N'Hyundai Creta', 2024, N'SUV', N'Trắng', N'Crossover 5 chỗ cân bằng giữa tiện nghi và chi phí vận hành.', N'Động cơ: 1.5L\nHộp số: IVT\nSố chỗ: 5\nNhiên liệu: Xăng', N'InStock', 640000000, 4),
    (N'Hyundai', N'Hyundai Santa Fe', 2024, N'SUV', N'Xám', N'SUV 7 chỗ nhiều công nghệ an toàn, nội thất rộng.', N'Động cơ: 2.5L\nHộp số: AT\nSố chỗ: 7\nNhiên liệu: Xăng', N'InStock', 1069000000, 2),
    (N'Hyundai', N'Hyundai Grand i10', 2023, N'Hatchback', N'Vàng', N'Xe đô thị cỡ nhỏ, linh hoạt và dễ sử dụng hằng ngày.', N'Động cơ: 1.2L\nHộp số: AT\nSố chỗ: 5\nNhiên liệu: Xăng', N'InStock', 380000000, 6),
    (N'Hyundai', N'Hyundai Stargazer', 2024, N'MPV', N'Bạc', N'MPV 7 chỗ thiết kế thực dụng, phù hợp dịch vụ và gia đình.', N'Động cơ: 1.5L\nHộp số: IVT\nSố chỗ: 7\nNhiên liệu: Xăng', N'Promotion', 575000000, 3),
    (N'Ford', N'Ford Ranger', 2024, N'Pickup', N'Cam', N'Bán tải mạnh mẽ, sức kéo tốt, phù hợp công việc và đi xa.', N'Động cơ: 2.0L\nHộp số: AT\nSố chỗ: 5\nNhiên liệu: Dầu', N'InStock', 665000000, 4),
    (N'Ford', N'Ford Territory', 2024, N'SUV', N'Xanh', N'SUV 5 chỗ rộng rãi, trang bị tiện nghi cho gia đình trẻ.', N'Động cơ: 1.5L Turbo\nHộp số: AT\nSố chỗ: 5\nNhiên liệu: Xăng', N'InStock', 799000000, 3),
    (N'Ford', N'Ford Explorer', 2023, N'SUV', N'Đen', N'SUV cỡ lớn nhập khẩu, khoang cabin cao cấp và động cơ khỏe.', N'Động cơ: 2.3L Turbo\nHộp số: AT\nSố chỗ: 7\nNhiên liệu: Xăng', N'InStock', 2099000000, 1),
    (N'Ford', N'Ford Transit', 2024, N'Van', N'Trắng', N'Xe 16 chỗ phù hợp vận tải hành khách và doanh nghiệp.', N'Động cơ: 2.2L\nHộp số: MT\nSố chỗ: 16\nNhiên liệu: Dầu', N'InStock', 905000000, 2),
    (N'Mazda', N'Mazda 2', 2024, N'Sedan', N'Đỏ', N'Sedan nhỏ gọn, thiết kế trẻ trung, lái nhẹ trong đô thị.', N'Động cơ: 1.5L\nHộp số: AT\nSố chỗ: 5\nNhiên liệu: Xăng', N'InStock', 420000000, 5),
    (N'Mazda', N'Mazda 3', 2024, N'Sedan', N'Xám', N'Sedan hạng C thiết kế đẹp, cảm giác lái tốt.', N'Động cơ: 1.5L\nHộp số: AT\nSố chỗ: 5\nNhiên liệu: Xăng', N'Promotion', 579000000, 4),
    (N'Mazda', N'Mazda CX-30', 2024, N'SUV', N'Trắng', N'Crossover nhỏ gọn, nội thất cao cấp trong tầm giá.', N'Động cơ: 2.0L\nHộp số: AT\nSố chỗ: 5\nNhiên liệu: Xăng', N'InStock', 699000000, 3),
    (N'Mazda', N'Mazda CX-8', 2023, N'SUV', N'Đen', N'SUV 7 chỗ thanh lịch, phù hợp gia đình cần khoang rộng.', N'Động cơ: 2.5L\nHộp số: AT\nSố chỗ: 7\nNhiên liệu: Xăng', N'InStock', 949000000, 2),
    (N'Toyota', N'Toyota Yaris Cross', 2024, N'SUV', N'Xanh', N'Crossover đô thị tiết kiệm, nhiều tính năng an toàn.', N'Động cơ: 1.5L\nHộp số: CVT\nSố chỗ: 5\nNhiên liệu: Xăng', N'InStock', 730000000, 4),
    (N'Hyundai', N'Hyundai Venue', 2024, N'SUV', N'Xanh rêu', N'SUV cỡ nhỏ thực dụng, phù hợp khách hàng mua xe lần đầu.', N'Động cơ: 1.0L Turbo\nHộp số: DCT\nSố chỗ: 5\nNhiên liệu: Xăng', N'InStock', 539000000, 5),
    (N'Ford', N'Ford EcoSport', 2022, N'SUV', N'Bạc', N'Crossover đô thị đã qua sử dụng ít, giá tốt để demo xe cũ.', N'Động cơ: 1.5L\nHộp số: AT\nSố chỗ: 5\nNhiên liệu: Xăng', N'InStock', 465000000, 2),
    (N'Mazda', N'Mazda BT-50', 2023, N'Pickup', N'Xám', N'Bán tải thiết kế thực dụng, phù hợp công việc hằng ngày.', N'Động cơ: 1.9L\nHộp số: AT\nSố chỗ: 5\nNhiên liệu: Dầu', N'InStock', 659000000, 3)
) AS source (BrandName, Name, [Year], [Type], Color, [Description], Specifications, Status, Price, StockQuantity)
ON target.Name = source.Name
WHEN NOT MATCHED THEN
    INSERT (BrandId, Name, [Year], [Type], Color, [Description], Specifications, ImageUrls, Status, Price, StockQuantity)
    VALUES ((SELECT Id FROM dbo.Brands WHERE Name = source.BrandName), source.Name, source.[Year], source.[Type], source.Color, source.[Description], source.Specifications, NULL, source.Status, source.Price, source.StockQuantity);
GO

MERGE dbo.StaffUsers AS target
USING (VALUES
    (N'staff01', N'pbkdf2-sha256$210000$+VTZfrCSvIR/1F5AqQHfkg==$XcsUn4gL9GDo4a/aHwG2G9akbdtpvbHvPRfTnt5q1Lg=', N'Phạm Minh Quân', N'Administrator'),
    (N'staff02', N'pbkdf2-sha256$210000$+VTZfrCSvIR/1F5AqQHfkg==$XcsUn4gL9GDo4a/aHwG2G9akbdtpvbHvPRfTnt5q1Lg=', N'Hoàng Thu Hà', N'Staff'),
    (N'staff03', N'pbkdf2-sha256$210000$+VTZfrCSvIR/1F5AqQHfkg==$XcsUn4gL9GDo4a/aHwG2G9akbdtpvbHvPRfTnt5q1Lg=', N'Do Anh Khoa', N'Staff'),
    (N'staff04', N'pbkdf2-sha256$210000$+VTZfrCSvIR/1F5AqQHfkg==$XcsUn4gL9GDo4a/aHwG2G9akbdtpvbHvPRfTnt5q1Lg=', N'Nguyễn Bảo Ngọc', N'Staff'),
    (N'staff05', N'pbkdf2-sha256$210000$+VTZfrCSvIR/1F5AqQHfkg==$XcsUn4gL9GDo4a/aHwG2G9akbdtpvbHvPRfTnt5q1Lg=', N'Trần Gia Huy', N'Staff')
) AS source (Username, PasswordHash, DisplayName, Role)
ON target.Username = source.Username
WHEN NOT MATCHED THEN
    INSERT (Username, PasswordHash, DisplayName, Role)
    VALUES (source.Username, source.PasswordHash, source.DisplayName, source.Role);
GO

UPDATE staff
SET
    BranchId = branch.Id,
    StaffCode = source.StaffCode,
    Email = source.Email,
    Phone = source.Phone,
    IsActive = 1
FROM dbo.StaffUsers staff
INNER JOIN
(
    VALUES
        (N'staff01', N'HCM-01', N'STAFF001', N'quan.pham@example.com', N'0902000001'),
        (N'staff02', N'HCM-01', N'STAFF002', N'ha.hoang@example.com', N'0902000002'),
        (N'staff03', N'HN-01', N'STAFF003', N'khoa.do@example.com', N'0902000003'),
        (N'staff04', N'HN-01', N'STAFF004', N'ngoc.nguyen@example.com', N'0902000004'),
        (N'staff05', N'HCM-01', N'STAFF005', N'huy.tran@example.com', N'0902000005')
) AS source (Username, BranchCode, StaffCode, Email, Phone)
    ON source.Username = staff.Username
INNER JOIN dbo.Branches branch
    ON branch.BranchCode = source.BranchCode;
GO

MERGE dbo.StaffUserRoles AS target
USING
(
    SELECT staff.Id AS StaffUserId, source.RoleCode
    FROM dbo.StaffUsers staff
    INNER JOIN
    (
        VALUES
            (N'staff01', N'Administrator'),
            (N'staff01', N'SalesManager'),
            (N'staff02', N'Sales'),
            (N'staff03', N'InventoryManager'),
            (N'staff04', N'ServiceAdvisor'),
            (N'staff05', N'Accountant')
    ) AS source (Username, RoleCode)
        ON source.Username = staff.Username
) AS source
ON target.StaffUserId = source.StaffUserId
   AND target.RoleCode = source.RoleCode
WHEN NOT MATCHED THEN
    INSERT (StaffUserId, RoleCode)
    VALUES (source.StaffUserId, source.RoleCode);
GO

MERGE dbo.Orders AS target
USING (VALUES
    (N'Phạm Thị Mai', N'0901000001', N'mai.pham@example.com', N'Quận 1, TP HCM', N'Quan tâm xe tiết kiệm nhiên liệu.', N'Pending'),
    (N'Đặng Minh Đức', N'0901000002', N'duc.dang@example.com', N'Quận Hai Bà Trưng, Hà Nội', N'Đã cọc giữ xe.', N'Paid'),
    (N'Võ Thanh Long', N'0901000003', N'long.vo@example.com', N'Thủ Đức, TP HCM', N'Giao xe cuối tuần.', N'Completed'),
    (N'Bùi Ngọc Anh', N'0901000004', N'anh.bui@example.com', N'Quận Thanh Khê, Đà Nẵng', N'Cần phụ kiện gia đình.', N'Delivered'),
    (N'Hồ Thị Lan', N'0901000005', N'lan.ho@example.com', N'Nha Trang, Khánh Hòa', N'Thanh toán chuyển khoản.', N'Paid'),
    (N'Ngô Quang Hiếu', N'0901000006', N'hieu.ngo@example.com', N'Biên Hòa, Đồng Nai', N'Khách hủy do đổi màu xe.', N'Cancelled'),
    (N'Lý Bảo Châu', N'0901000007', N'chau.ly@example.com', N'Cần Thơ', N'Đang chờ duyệt hồ sơ trả góp.', N'Pending'),
    (N'Trương Gia Bảo', N'0901000008', N'bao.truong@example.com', N'Huế', N'Khách mua thêm gói bảo dưỡng.', N'Completed'),
    (N'Nguyễn Minh Tâm', N'0901000009', N'tam.nguyen@example.com', N'Vũng Tàu', N'Lấy xe trong giờ hành chính.', N'Paid'),
    (N'Lê Phương Linh', N'0901000010', N'linh.le@example.com', N'Long Biên, Hà Nội', N'Giao xe tại showroom.', N'Delivered')
) AS source (CustomerName, CustomerPhone, CustomerEmail, CustomerAddress, Note, Status)
ON target.CustomerPhone = source.CustomerPhone
WHEN NOT MATCHED THEN
    INSERT (CustomerName, CustomerPhone, CustomerEmail, CustomerAddress, Note, Status)
    VALUES (source.CustomerName, source.CustomerPhone, source.CustomerEmail, source.CustomerAddress, source.Note, source.Status);
GO

INSERT INTO dbo.OrderItems (OrderId, CarId, Quantity, UnitPrice)
SELECT o.Id, c.Id, source.Quantity, source.UnitPrice
FROM (VALUES
    (N'0901000001', N'Toyota Vios', 1, 489000000),
    (N'0901000002', N'Toyota Raize', 1, 552000000),
    (N'0901000003', N'Hyundai Santa Fe', 1, 1069000000),
    (N'0901000004', N'Ford Ranger', 2, 665000000),
    (N'0901000005', N'Mazda 3', 1, 579000000),
    (N'0901000006', N'Ford Explorer', 1, 2099000000),
    (N'0901000007', N'Toyota Yaris Cross', 1, 730000000),
    (N'0901000008', N'Mazda 2', 2, 420000000),
    (N'0901000009', N'Hyundai Venue', 1, 539000000),
    (N'0901000010', N'Mazda BT-50', 1, 659000000)
) AS source (CustomerPhone, CarName, Quantity, UnitPrice)
INNER JOIN dbo.Orders o
    ON o.CustomerPhone = source.CustomerPhone
INNER JOIN dbo.Cars c
    ON c.Name = source.CarName
WHERE NOT EXISTS
(
    SELECT 1
    FROM dbo.OrderItems oi
    WHERE oi.OrderId = o.Id
      AND oi.CarId = c.Id
);
GO

-- 1) Kiểm tra DB đang dùng
IF OBJECT_ID(N'dbo.VehicleModels', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.VehicleModels
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        BrandId INT NOT NULL,
        ModelCode NVARCHAR(50) NOT NULL,
        ModelName NVARCHAR(150) NOT NULL,
        ModelYear SMALLINT NOT NULL,
        BasePrice DECIMAL(18,2) NOT NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_VehicleModels_Brands FOREIGN KEY (BrandId) REFERENCES dbo.Brands(Id),
        CONSTRAINT CK_VehicleModels_Code_NotBlank CHECK (LEN(LTRIM(RTRIM(ModelCode))) > 0),
        CONSTRAINT CK_VehicleModels_Name_NotBlank CHECK (LEN(LTRIM(RTRIM(ModelName))) > 0),
        CONSTRAINT CK_VehicleModels_ModelYear CHECK (ModelYear BETWEEN 1990 AND 2100),
        CONSTRAINT CK_VehicleModels_BasePrice CHECK (BasePrice >= 0)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_VehicleModels_ModelCode' AND object_id = OBJECT_ID(N'dbo.VehicleModels'))
BEGIN
    CREATE UNIQUE INDEX UX_VehicleModels_ModelCode ON dbo.VehicleModels (ModelCode);
END;
GO

IF OBJECT_ID(N'dbo.Locations', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Locations
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        BranchId INT NOT NULL,
        LocationCode NVARCHAR(50) NOT NULL,
        LocationName NVARCHAR(150) NOT NULL,
        LocationType NVARCHAR(30) NOT NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_Locations_Branches FOREIGN KEY (BranchId) REFERENCES dbo.Branches(Id),
        CONSTRAINT CK_Locations_Code_NotBlank CHECK (LEN(LTRIM(RTRIM(LocationCode))) > 0),
        CONSTRAINT CK_Locations_Name_NotBlank CHECK (LEN(LTRIM(RTRIM(LocationName))) > 0),
        CONSTRAINT CK_Locations_Type CHECK (LocationType IN (N'Storage', N'Showroom', N'Workshop', N'Transit', N'Delivery'))
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Locations_LocationCode' AND object_id = OBJECT_ID(N'dbo.Locations'))
BEGIN
    CREATE UNIQUE INDEX UX_Locations_LocationCode ON dbo.Locations (LocationCode);
END;
GO

IF OBJECT_ID(N'dbo.FactoryOrders', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FactoryOrders
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        FactoryOrderNo NVARCHAR(50) NOT NULL,
        BranchId INT NOT NULL,
        OrderedByStaffUserId INT NULL,
        OrderDate DATE NOT NULL,
        ExpectedArrivalDate DATE NULL,
        Status NVARCHAR(30) NOT NULL DEFAULT N'Planned',
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_FactoryOrders_Branches FOREIGN KEY (BranchId) REFERENCES dbo.Branches(Id),
        CONSTRAINT FK_FactoryOrders_StaffUsers FOREIGN KEY (OrderedByStaffUserId) REFERENCES dbo.StaffUsers(Id),
        CONSTRAINT CK_FactoryOrders_No_NotBlank CHECK (LEN(LTRIM(RTRIM(FactoryOrderNo))) > 0),
        CONSTRAINT CK_FactoryOrders_Status CHECK (Status IN (N'Planned', N'Ordered', N'InTransit', N'Received', N'Cancelled'))
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_FactoryOrders_No' AND object_id = OBJECT_ID(N'dbo.FactoryOrders'))
BEGIN
    CREATE UNIQUE INDEX UX_FactoryOrders_No ON dbo.FactoryOrders (FactoryOrderNo);
END;
GO

IF OBJECT_ID(N'dbo.FactoryOrderItems', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FactoryOrderItems
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        FactoryOrderId INT NOT NULL,
        VehicleModelId INT NOT NULL,
        Quantity INT NOT NULL,
        PlannedUnitCost DECIMAL(18,2) NOT NULL,
        CONSTRAINT FK_FactoryOrderItems_FactoryOrders FOREIGN KEY (FactoryOrderId) REFERENCES dbo.FactoryOrders(Id) ON DELETE CASCADE,
        CONSTRAINT FK_FactoryOrderItems_VehicleModels FOREIGN KEY (VehicleModelId) REFERENCES dbo.VehicleModels(Id),
        CONSTRAINT CK_FactoryOrderItems_Quantity CHECK (Quantity > 0),
        CONSTRAINT CK_FactoryOrderItems_PlannedUnitCost CHECK (PlannedUnitCost >= 0)
    );
END;
GO

IF OBJECT_ID(N'dbo.NewVehicles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.NewVehicles
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        VehicleModelId INT NOT NULL,
        FactoryOrderItemId INT NULL,
        CurrentLocationId INT NULL,
        Vin CHAR(17) NOT NULL,
        EngineNumber NVARCHAR(50) NOT NULL,
        ExteriorColor NVARCHAR(80) NULL,
        InteriorColor NVARCHAR(80) NULL,
        CostPrice DECIMAL(18,2) NOT NULL,
        Msrp DECIMAL(18,2) NOT NULL,
        CurrentOdometer INT NOT NULL DEFAULT 0,
        Status NVARCHAR(30) NOT NULL DEFAULT N'Ready',
        InboundCheckedAt DATETIME2 NULL,
        InboundDamageNote NVARCHAR(1000) NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_NewVehicles_VehicleModels FOREIGN KEY (VehicleModelId) REFERENCES dbo.VehicleModels(Id),
        CONSTRAINT FK_NewVehicles_FactoryOrderItems FOREIGN KEY (FactoryOrderItemId) REFERENCES dbo.FactoryOrderItems(Id),
        CONSTRAINT FK_NewVehicles_Locations FOREIGN KEY (CurrentLocationId) REFERENCES dbo.Locations(Id),
        CONSTRAINT CK_NewVehicles_CostPrice CHECK (CostPrice >= 0),
        CONSTRAINT CK_NewVehicles_Msrp CHECK (Msrp >= 0),
        CONSTRAINT CK_NewVehicles_Odometer CHECK (CurrentOdometer >= 0),
        CONSTRAINT CK_NewVehicles_Status CHECK (Status IN (N'Ordered', N'Inbound', N'Ready', N'OnDisplay', N'Allocated_Locked', N'PendingDelivery', N'Delivered', N'ServiceHold', N'DamagedHold', N'ReturnedToFactory'))
    );
END;
GO

IF OBJECT_ID(N'dbo.NewVehicles', N'U') IS NOT NULL
    AND COL_LENGTH(N'dbo.NewVehicles', N'FactoryOrderItemId') IS NULL
BEGIN
    ALTER TABLE dbo.NewVehicles ADD FactoryOrderItemId INT NULL;
END;
GO

IF OBJECT_ID(N'dbo.FK_NewVehicles_FactoryOrderItems', N'F') IS NULL
    AND OBJECT_ID(N'dbo.NewVehicles', N'U') IS NOT NULL
    AND OBJECT_ID(N'dbo.FactoryOrderItems', N'U') IS NOT NULL
    AND COL_LENGTH(N'dbo.NewVehicles', N'FactoryOrderItemId') IS NOT NULL
BEGIN
    ALTER TABLE dbo.NewVehicles
    ADD CONSTRAINT FK_NewVehicles_FactoryOrderItems FOREIGN KEY (FactoryOrderItemId) REFERENCES dbo.FactoryOrderItems(Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_NewVehicles_Vin' AND object_id = OBJECT_ID(N'dbo.NewVehicles'))
BEGIN
    CREATE UNIQUE INDEX UX_NewVehicles_Vin ON dbo.NewVehicles (Vin);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_NewVehicles_EngineNumber' AND object_id = OBJECT_ID(N'dbo.NewVehicles'))
BEGIN
    CREATE UNIQUE INDEX UX_NewVehicles_EngineNumber ON dbo.NewVehicles (EngineNumber);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_NewVehicles_Model_Status' AND object_id = OBJECT_ID(N'dbo.NewVehicles'))
BEGIN
    CREATE INDEX IX_NewVehicles_Model_Status ON dbo.NewVehicles (VehicleModelId, Status);
END;
GO

IF OBJECT_ID(N'dbo.VehicleLocationMovements', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.VehicleLocationMovements
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        NewVehicleId INT NOT NULL,
        FromLocationId INT NULL,
        ToLocationId INT NOT NULL,
        MovedByStaffUserId INT NULL,
        MovedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        Reason NVARCHAR(300) NULL,
        CONSTRAINT FK_VehicleLocationMovements_NewVehicles FOREIGN KEY (NewVehicleId) REFERENCES dbo.NewVehicles(Id),
        CONSTRAINT FK_VehicleLocationMovements_FromLocations FOREIGN KEY (FromLocationId) REFERENCES dbo.Locations(Id),
        CONSTRAINT FK_VehicleLocationMovements_ToLocations FOREIGN KEY (ToLocationId) REFERENCES dbo.Locations(Id),
        CONSTRAINT FK_VehicleLocationMovements_StaffUsers FOREIGN KEY (MovedByStaffUserId) REFERENCES dbo.StaffUsers(Id)
    );
END;
GO

IF OBJECT_ID(N'dbo.Parts', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Parts
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        PartCode NVARCHAR(50) NOT NULL,
        PartName NVARCHAR(150) NOT NULL,
        Unit NVARCHAR(20) NOT NULL DEFAULT N'pcs',
        ListPrice DECIMAL(18,2) NOT NULL DEFAULT 0,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT CK_Parts_Code_NotBlank CHECK (LEN(LTRIM(RTRIM(PartCode))) > 0),
        CONSTRAINT CK_Parts_Name_NotBlank CHECK (LEN(LTRIM(RTRIM(PartName))) > 0),
        CONSTRAINT CK_Parts_ListPrice CHECK (ListPrice >= 0)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Parts_PartCode' AND object_id = OBJECT_ID(N'dbo.Parts'))
BEGIN
    CREATE UNIQUE INDEX UX_Parts_PartCode ON dbo.Parts (PartCode);
END;
GO

IF OBJECT_ID(N'dbo.PartInventory', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PartInventory
    (
        PartId INT NOT NULL,
        LocationId INT NOT NULL,
        QuantityOnHand DECIMAL(18,2) NOT NULL DEFAULT 0,
        MinStockLevel DECIMAL(18,2) NOT NULL DEFAULT 0,
        UpdatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_PartInventory PRIMARY KEY (PartId, LocationId),
        CONSTRAINT FK_PartInventory_Parts FOREIGN KEY (PartId) REFERENCES dbo.Parts(Id),
        CONSTRAINT FK_PartInventory_Locations FOREIGN KEY (LocationId) REFERENCES dbo.Locations(Id),
        CONSTRAINT CK_PartInventory_Quantity CHECK (QuantityOnHand >= 0),
        CONSTRAINT CK_PartInventory_MinStock CHECK (MinStockLevel >= 0)
    );
END;
GO

IF OBJECT_ID(N'dbo.PartPurchaseOrders', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PartPurchaseOrders
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        PurchaseOrderNo NVARCHAR(50) NOT NULL,
        BranchId INT NOT NULL,
        SupplierName NVARCHAR(150) NOT NULL,
        OrderedByStaffUserId INT NULL,
        OrderDate DATE NOT NULL,
        ExpectedArrivalDate DATE NULL,
        Status NVARCHAR(30) NOT NULL DEFAULT N'Draft',
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_PartPurchaseOrders_Branches FOREIGN KEY (BranchId) REFERENCES dbo.Branches(Id),
        CONSTRAINT FK_PartPurchaseOrders_StaffUsers FOREIGN KEY (OrderedByStaffUserId) REFERENCES dbo.StaffUsers(Id),
        CONSTRAINT CK_PartPurchaseOrders_No_NotBlank CHECK (LEN(LTRIM(RTRIM(PurchaseOrderNo))) > 0),
        CONSTRAINT CK_PartPurchaseOrders_Supplier_NotBlank CHECK (LEN(LTRIM(RTRIM(SupplierName))) > 0),
        CONSTRAINT CK_PartPurchaseOrders_Status CHECK (Status IN (N'Draft', N'Approved', N'Ordered', N'PartiallyReceived', N'Received', N'Cancelled'))
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_PartPurchaseOrders_No' AND object_id = OBJECT_ID(N'dbo.PartPurchaseOrders'))
BEGIN
    CREATE UNIQUE INDEX UX_PartPurchaseOrders_No ON dbo.PartPurchaseOrders (PurchaseOrderNo);
END;
GO

IF OBJECT_ID(N'dbo.PartPurchaseOrderItems', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PartPurchaseOrderItems
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        PartPurchaseOrderId INT NOT NULL,
        PartId INT NOT NULL,
        OrderedQuantity DECIMAL(18,2) NOT NULL,
        ReceivedQuantity DECIMAL(18,2) NOT NULL DEFAULT 0,
        UnitCost DECIMAL(18,2) NOT NULL,
        CONSTRAINT FK_PartPurchaseOrderItems_Orders FOREIGN KEY (PartPurchaseOrderId) REFERENCES dbo.PartPurchaseOrders(Id) ON DELETE CASCADE,
        CONSTRAINT FK_PartPurchaseOrderItems_Parts FOREIGN KEY (PartId) REFERENCES dbo.Parts(Id),
        CONSTRAINT CK_PartPurchaseOrderItems_OrderedQuantity CHECK (OrderedQuantity > 0),
        CONSTRAINT CK_PartPurchaseOrderItems_ReceivedQuantity CHECK (ReceivedQuantity >= 0),
        CONSTRAINT CK_PartPurchaseOrderItems_ReceivedLimit CHECK (ReceivedQuantity <= OrderedQuantity),
        CONSTRAINT CK_PartPurchaseOrderItems_UnitCost CHECK (UnitCost >= 0)
    );
END;
GO

IF OBJECT_ID(N'dbo.PartPurchaseOrderItems', N'U') IS NOT NULL
    AND COL_LENGTH(N'dbo.PartPurchaseOrderItems', N'ReceivedQuantity') IS NULL
BEGIN
    ALTER TABLE dbo.PartPurchaseOrderItems
    ADD ReceivedQuantity DECIMAL(18,2) NOT NULL
        CONSTRAINT DF_PartPurchaseOrderItems_ReceivedQuantity DEFAULT 0 WITH VALUES;
END;
GO

IF OBJECT_ID(N'dbo.CK_PartPurchaseOrderItems_ReceivedQuantity', N'C') IS NULL
    AND OBJECT_ID(N'dbo.PartPurchaseOrderItems', N'U') IS NOT NULL
    AND COL_LENGTH(N'dbo.PartPurchaseOrderItems', N'ReceivedQuantity') IS NOT NULL
BEGIN
    ALTER TABLE dbo.PartPurchaseOrderItems
    ADD CONSTRAINT CK_PartPurchaseOrderItems_ReceivedQuantity CHECK (ReceivedQuantity >= 0);
END;
GO

IF OBJECT_ID(N'dbo.CK_PartPurchaseOrderItems_ReceivedLimit', N'C') IS NULL
    AND OBJECT_ID(N'dbo.PartPurchaseOrderItems', N'U') IS NOT NULL
    AND COL_LENGTH(N'dbo.PartPurchaseOrderItems', N'ReceivedQuantity') IS NOT NULL
BEGIN
    ALTER TABLE dbo.PartPurchaseOrderItems
    ADD CONSTRAINT CK_PartPurchaseOrderItems_ReceivedLimit CHECK (ReceivedQuantity <= OrderedQuantity);
END;
GO

IF OBJECT_ID(N'dbo.PartStockMovements', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PartStockMovements
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        PartId INT NOT NULL,
        LocationId INT NOT NULL,
        MovementType NVARCHAR(30) NOT NULL,
        QuantityDelta DECIMAL(18,2) NOT NULL,
        PartPurchaseOrderItemId INT NULL,
        CreatedByStaffUserId INT NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        Note NVARCHAR(300) NULL,
        CONSTRAINT FK_PartStockMovements_Parts FOREIGN KEY (PartId) REFERENCES dbo.Parts(Id),
        CONSTRAINT FK_PartStockMovements_Locations FOREIGN KEY (LocationId) REFERENCES dbo.Locations(Id),
        CONSTRAINT FK_PartStockMovements_PurchaseOrderItems FOREIGN KEY (PartPurchaseOrderItemId) REFERENCES dbo.PartPurchaseOrderItems(Id),
        CONSTRAINT FK_PartStockMovements_StaffUsers FOREIGN KEY (CreatedByStaffUserId) REFERENCES dbo.StaffUsers(Id),
        CONSTRAINT CK_PartStockMovements_Type CHECK (MovementType IN (N'Receive', N'Issue', N'Adjust', N'TransferIn', N'TransferOut')),
        CONSTRAINT CK_PartStockMovements_QuantityDelta CHECK (QuantityDelta <> 0)
    );
END;
GO

IF OBJECT_ID(N'dbo.PartStockMovements', N'U') IS NOT NULL
    AND COL_LENGTH(N'dbo.PartStockMovements', N'PartPurchaseOrderItemId') IS NULL
BEGIN
    ALTER TABLE dbo.PartStockMovements ADD PartPurchaseOrderItemId INT NULL;
END;
GO

IF OBJECT_ID(N'dbo.FK_PartStockMovements_PurchaseOrderItems', N'F') IS NULL
    AND OBJECT_ID(N'dbo.PartStockMovements', N'U') IS NOT NULL
    AND OBJECT_ID(N'dbo.PartPurchaseOrderItems', N'U') IS NOT NULL
    AND COL_LENGTH(N'dbo.PartStockMovements', N'PartPurchaseOrderItemId') IS NOT NULL
BEGIN
    ALTER TABLE dbo.PartStockMovements
    ADD CONSTRAINT FK_PartStockMovements_PurchaseOrderItems FOREIGN KEY (PartPurchaseOrderItemId) REFERENCES dbo.PartPurchaseOrderItems(Id);
END;
GO

IF OBJECT_ID(N'dbo.Leads', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Leads
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        CustomerId INT NULL,
        CustomerRequestId INT NULL,
        AssignedSalesStaffUserId INT NULL,
        DesiredModelId INT NULL,
        Source NVARCHAR(30) NOT NULL DEFAULT N'Website',
        ContactName NVARCHAR(150) NOT NULL,
        ContactPhone NVARCHAR(30) NULL,
        ContactEmail NVARCHAR(254) NULL,
        LeadScore TINYINT NOT NULL DEFAULT 0,
        Status NVARCHAR(30) NOT NULL DEFAULT N'New',
        Note NVARCHAR(500) NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_Leads_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Customers(Id),
        CONSTRAINT FK_Leads_CustomerRequests FOREIGN KEY (CustomerRequestId) REFERENCES dbo.CustomerRequests(Id),
        CONSTRAINT FK_Leads_AssignedSales FOREIGN KEY (AssignedSalesStaffUserId) REFERENCES dbo.StaffUsers(Id),
        CONSTRAINT FK_Leads_DesiredModels FOREIGN KEY (DesiredModelId) REFERENCES dbo.VehicleModels(Id),
        CONSTRAINT CK_Leads_Source CHECK (Source IN (N'Website', N'Showroom', N'App', N'Phone', N'Social', N'Referral', N'Other')),
        CONSTRAINT CK_Leads_Status CHECK (Status IN (N'New', N'Qualified', N'Unqualified', N'Converted', N'Lost')),
        CONSTRAINT CK_Leads_Score CHECK (LeadScore BETWEEN 0 AND 100),
        CONSTRAINT CK_Leads_ContactName_NotBlank CHECK (LEN(LTRIM(RTRIM(ContactName))) > 0),
        CONSTRAINT CK_Leads_Contact CHECK (ContactPhone IS NOT NULL OR ContactEmail IS NOT NULL)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Leads_Status_AssignedSales' AND object_id = OBJECT_ID(N'dbo.Leads'))
BEGIN
    CREATE INDEX IX_Leads_Status_AssignedSales ON dbo.Leads (Status, AssignedSalesStaffUserId);
END;
GO

IF OBJECT_ID(N'dbo.SalesOpportunities', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SalesOpportunities
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        LeadId INT NOT NULL,
        CustomerId INT NULL,
        DesiredModelId INT NULL,
        SalesStaffUserId INT NULL,
        OpportunityName NVARCHAR(150) NOT NULL,
        Stage NVARCHAR(40) NOT NULL DEFAULT N'LeadManagement',
        ProbabilityPercent TINYINT NOT NULL DEFAULT 10,
        ExpectedCloseDate DATE NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_SalesOpportunities_Leads FOREIGN KEY (LeadId) REFERENCES dbo.Leads(Id),
        CONSTRAINT FK_SalesOpportunities_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Customers(Id),
        CONSTRAINT FK_SalesOpportunities_DesiredModels FOREIGN KEY (DesiredModelId) REFERENCES dbo.VehicleModels(Id),
        CONSTRAINT FK_SalesOpportunities_SalesStaff FOREIGN KEY (SalesStaffUserId) REFERENCES dbo.StaffUsers(Id),
        CONSTRAINT CK_SalesOpportunities_Name_NotBlank CHECK (LEN(LTRIM(RTRIM(OpportunityName))) > 0),
        CONSTRAINT CK_SalesOpportunities_Stage CHECK (Stage IN (N'LeadManagement', N'NeedsAnalysis', N'TestDrive', N'Quote', N'Negotiation', N'Contract', N'Won', N'Lost')),
        CONSTRAINT CK_SalesOpportunities_Probability CHECK (ProbabilityPercent BETWEEN 0 AND 100)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SalesOpportunities_Stage' AND object_id = OBJECT_ID(N'dbo.SalesOpportunities'))
BEGIN
    CREATE INDEX IX_SalesOpportunities_Stage ON dbo.SalesOpportunities (Stage, CreatedAt DESC);
END;
GO

IF OBJECT_ID(N'dbo.TestDrives', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TestDrives
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        OpportunityId INT NOT NULL,
        NewVehicleId INT NOT NULL,
        ScheduledStartAt DATETIME2 NOT NULL,
        ActualStartAt DATETIME2 NULL,
        ActualEndAt DATETIME2 NULL,
        Status NVARCHAR(30) NOT NULL DEFAULT N'Scheduled',
        FeedbackNote NVARCHAR(500) NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_TestDrives_Opportunities FOREIGN KEY (OpportunityId) REFERENCES dbo.SalesOpportunities(Id),
        CONSTRAINT FK_TestDrives_NewVehicles FOREIGN KEY (NewVehicleId) REFERENCES dbo.NewVehicles(Id),
        CONSTRAINT CK_TestDrives_Status CHECK (Status IN (N'Scheduled', N'Completed', N'Cancelled', N'NoShow')),
        CONSTRAINT CK_TestDrives_Time CHECK (ActualEndAt IS NULL OR ActualStartAt IS NULL OR ActualEndAt >= ActualStartAt)
    );
END;
GO

IF OBJECT_ID(N'dbo.SalesQuotes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SalesQuotes
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        QuoteNo NVARCHAR(50) NOT NULL,
        OpportunityId INT NOT NULL,
        ModelId INT NOT NULL,
        NewVehicleId INT NULL,
        VehiclePrice DECIMAL(18,2) NOT NULL,
        DiscountAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
        RegistrationFeeEstimate DECIMAL(18,2) NOT NULL DEFAULT 0,
        InsuranceEstimate DECIMAL(18,2) NOT NULL DEFAULT 0,
        AccessoryPackageValue DECIMAL(18,2) NOT NULL DEFAULT 0,
        ValidUntil DATE NULL,
        Status NVARCHAR(30) NOT NULL DEFAULT N'Draft',
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_SalesQuotes_Opportunities FOREIGN KEY (OpportunityId) REFERENCES dbo.SalesOpportunities(Id),
        CONSTRAINT FK_SalesQuotes_Models FOREIGN KEY (ModelId) REFERENCES dbo.VehicleModels(Id),
        CONSTRAINT FK_SalesQuotes_NewVehicles FOREIGN KEY (NewVehicleId) REFERENCES dbo.NewVehicles(Id),
        CONSTRAINT CK_SalesQuotes_No_NotBlank CHECK (LEN(LTRIM(RTRIM(QuoteNo))) > 0),
        CONSTRAINT CK_SalesQuotes_Amounts CHECK (VehiclePrice >= 0 AND DiscountAmount >= 0 AND RegistrationFeeEstimate >= 0 AND InsuranceEstimate >= 0 AND AccessoryPackageValue >= 0),
        CONSTRAINT CK_SalesQuotes_Status CHECK (Status IN (N'Draft', N'Sent', N'Accepted', N'Expired', N'Cancelled'))
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_SalesQuotes_QuoteNo' AND object_id = OBJECT_ID(N'dbo.SalesQuotes'))
BEGIN
    CREATE UNIQUE INDEX UX_SalesQuotes_QuoteNo ON dbo.SalesQuotes (QuoteNo);
END;
GO

IF OBJECT_ID(N'dbo.SalesContracts', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SalesContracts
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        ContractNo NVARCHAR(50) NOT NULL,
        OpportunityId INT NOT NULL,
        CustomerId INT NULL,
        NewVehicleId INT NOT NULL,
        SalesStaffUserId INT NULL,
        CreatedByStaffUserId INT NULL,
        FinalSalePrice DECIMAL(18,2) NOT NULL,
        PaymentMethod NVARCHAR(30) NOT NULL DEFAULT N'Cash',
        Status NVARCHAR(30) NOT NULL DEFAULT N'Draft',
        DepositAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
        DepositDueAt DATETIME2 NULL,
        SignedAt DATETIME2 NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_SalesContracts_Opportunities FOREIGN KEY (OpportunityId) REFERENCES dbo.SalesOpportunities(Id),
        CONSTRAINT FK_SalesContracts_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Customers(Id),
        CONSTRAINT FK_SalesContracts_NewVehicles FOREIGN KEY (NewVehicleId) REFERENCES dbo.NewVehicles(Id),
        CONSTRAINT FK_SalesContracts_SalesStaff FOREIGN KEY (SalesStaffUserId) REFERENCES dbo.StaffUsers(Id),
        CONSTRAINT FK_SalesContracts_CreatedBy FOREIGN KEY (CreatedByStaffUserId) REFERENCES dbo.StaffUsers(Id),
        CONSTRAINT CK_SalesContracts_No_NotBlank CHECK (LEN(LTRIM(RTRIM(ContractNo))) > 0),
        CONSTRAINT CK_SalesContracts_PaymentMethod CHECK (PaymentMethod IN (N'Cash', N'Installment')),
        CONSTRAINT CK_SalesContracts_Status CHECK (Status IN (N'Draft', N'DepositPaid', N'Signed', N'PendingRegistration', N'PendingDelivery', N'Delivered', N'Cancelled', N'Voided')),
        CONSTRAINT CK_SalesContracts_Amounts CHECK (FinalSalePrice >= 0 AND DepositAmount >= 0)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_SalesContracts_ContractNo' AND object_id = OBJECT_ID(N'dbo.SalesContracts'))
BEGIN
    CREATE UNIQUE INDEX UX_SalesContracts_ContractNo ON dbo.SalesContracts (ContractNo);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_SalesContracts_OneOpenContractPerVehicle' AND object_id = OBJECT_ID(N'dbo.SalesContracts'))
BEGIN
    CREATE UNIQUE INDEX UX_SalesContracts_OneOpenContractPerVehicle
    ON dbo.SalesContracts (NewVehicleId)
    WHERE Status <> N'Cancelled' AND Status <> N'Voided' AND Status <> N'Delivered';
END;
GO

IF OBJECT_ID(N'dbo.ContractAccessories', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ContractAccessories
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        SalesContractId INT NOT NULL,
        AccessoryName NVARCHAR(150) NOT NULL,
        Quantity INT NOT NULL DEFAULT 1,
        UnitPrice DECIMAL(18,2) NOT NULL DEFAULT 0,
        UnitCost DECIMAL(18,2) NOT NULL DEFAULT 0,
        IsGift BIT NOT NULL DEFAULT 0,
        CONSTRAINT FK_ContractAccessories_SalesContracts FOREIGN KEY (SalesContractId) REFERENCES dbo.SalesContracts(Id),
        CONSTRAINT CK_ContractAccessories_Name_NotBlank CHECK (LEN(LTRIM(RTRIM(AccessoryName))) > 0),
        CONSTRAINT CK_ContractAccessories_Quantity CHECK (Quantity > 0),
        CONSTRAINT CK_ContractAccessories_Amounts CHECK (UnitPrice >= 0 AND UnitCost >= 0)
    );
END;
GO

IF OBJECT_ID(N'dbo.RegistrationTasks', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RegistrationTasks
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        SalesContractId INT NOT NULL,
        TaskType NVARCHAR(40) NOT NULL,
        Status NVARCHAR(30) NOT NULL DEFAULT N'Pending',
        DueDate DATE NULL,
        Amount DECIMAL(18,2) NOT NULL DEFAULT 0,
        HandledByStaffUserId INT NULL,
        Note NVARCHAR(300) NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_RegistrationTasks_SalesContracts FOREIGN KEY (SalesContractId) REFERENCES dbo.SalesContracts(Id),
        CONSTRAINT FK_RegistrationTasks_HandledBy FOREIGN KEY (HandledByStaffUserId) REFERENCES dbo.StaffUsers(Id),
        CONSTRAINT CK_RegistrationTasks_Type CHECK (TaskType IN (N'RegistrationTax', N'PlateNumber', N'Inspection', N'Insurance', N'Other')),
        CONSTRAINT CK_RegistrationTasks_Status CHECK (Status IN (N'Pending', N'Submitted', N'Completed', N'Cancelled')),
        CONSTRAINT CK_RegistrationTasks_Amount CHECK (Amount >= 0)
    );
END;
GO

IF OBJECT_ID(N'dbo.VehicleDeliveries', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.VehicleDeliveries
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        SalesContractId INT NOT NULL,
        DeliveryStaffUserId INT NULL,
        DeliveredAt DATETIME2 NULL,
        OdometerAtDelivery INT NOT NULL DEFAULT 0,
        CustomerAccepted BIT NOT NULL DEFAULT 0,
        Note NVARCHAR(500) NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_VehicleDeliveries_SalesContracts FOREIGN KEY (SalesContractId) REFERENCES dbo.SalesContracts(Id),
        CONSTRAINT FK_VehicleDeliveries_StaffUsers FOREIGN KEY (DeliveryStaffUserId) REFERENCES dbo.StaffUsers(Id),
        CONSTRAINT CK_VehicleDeliveries_Odometer CHECK (OdometerAtDelivery >= 0),
        CONSTRAINT CK_VehicleDeliveries_AcceptanceTime CHECK (CustomerAccepted = 0 OR DeliveredAt IS NOT NULL)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_VehicleDeliveries_SalesContractId' AND object_id = OBJECT_ID(N'dbo.VehicleDeliveries'))
BEGIN
    CREATE UNIQUE INDEX UX_VehicleDeliveries_SalesContractId ON dbo.VehicleDeliveries (SalesContractId);
END;
GO

IF OBJECT_ID(N'dbo.DeliveryChecklistItems', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DeliveryChecklistItems
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        VehicleDeliveryId INT NOT NULL,
        ChecklistName NVARCHAR(150) NOT NULL,
        IsCompleted BIT NOT NULL DEFAULT 0,
        CompletedAt DATETIME2 NULL,
        Note NVARCHAR(300) NULL,
        CONSTRAINT FK_DeliveryChecklistItems_VehicleDeliveries FOREIGN KEY (VehicleDeliveryId) REFERENCES dbo.VehicleDeliveries(Id),
        CONSTRAINT CK_DeliveryChecklistItems_Name_NotBlank CHECK (LEN(LTRIM(RTRIM(ChecklistName))) > 0),
        CONSTRAINT CK_DeliveryChecklistItems_CompletedAt CHECK (IsCompleted = 0 OR CompletedAt IS NOT NULL)
    );
END;
GO

MERGE dbo.VehicleModels AS target
USING
(
    SELECT b.Id AS BrandId, source.ModelCode, source.ModelName, source.ModelYear, source.BasePrice
    FROM
    (
        VALUES
            (N'Toyota', N'TOY-CAMRY-2024', N'Camry', 2024, 1200000000),
            (N'Toyota', N'TOY-FORTUNER-2024', N'Fortuner', 2024, 1185000000),
            (N'Hyundai', N'HYU-TUCSON-2024', N'Tucson', 2024, 845000000),
            (N'Ford', N'FOR-RANGER-2024', N'Ranger', 2024, 665000000),
            (N'Mazda', N'MAZ-CX5-2024', N'CX-5', 2024, 799000000)
    ) source(BrandName, ModelCode, ModelName, ModelYear, BasePrice)
    INNER JOIN dbo.Brands b ON b.Name = source.BrandName
) AS source
ON target.ModelCode = source.ModelCode
WHEN MATCHED THEN
    UPDATE SET BrandId = source.BrandId, ModelName = source.ModelName, ModelYear = source.ModelYear, BasePrice = source.BasePrice
WHEN NOT MATCHED THEN
    INSERT (BrandId, ModelCode, ModelName, ModelYear, BasePrice)
    VALUES (source.BrandId, source.ModelCode, source.ModelName, source.ModelYear, source.BasePrice);
GO

MERGE dbo.Locations AS target
USING
(
    SELECT b.Id AS BranchId, source.LocationCode, source.LocationName, source.LocationType, source.IsActive
    FROM
    (
        VALUES
            (N'HCM-01', N'HCM-STORAGE', N'Kho xe HCM', N'Storage', 1),
            (N'HCM-01', N'HCM-SHOWROOM', N'Sàn trưng bày HCM', N'Showroom', 1),
            (N'HCM-01', N'HCM-WORKSHOP', N'Xưởng PDI HCM', N'Workshop', 1),
            (N'HN-01', N'HN-STORAGE', N'Kho xe Hà Nội', N'Storage', 1),
            (N'HN-01', N'HN-SHOWROOM', N'Sàn trưng bày Hà Nội', N'Showroom', 1)
    ) source(BranchCode, LocationCode, LocationName, LocationType, IsActive)
    INNER JOIN dbo.Branches b ON b.BranchCode = source.BranchCode
) AS source
ON target.LocationCode = source.LocationCode
WHEN MATCHED THEN
    UPDATE SET BranchId = source.BranchId, LocationName = source.LocationName, LocationType = source.LocationType, IsActive = source.IsActive
WHEN NOT MATCHED THEN
    INSERT (BranchId, LocationCode, LocationName, LocationType, IsActive)
    VALUES (source.BranchId, source.LocationCode, source.LocationName, source.LocationType, source.IsActive);
GO

MERGE dbo.Parts AS target
USING (VALUES
    (N'FILTER-OIL-001', N'Lọc dầu động cơ', N'pcs', 180000, 1),
    (N'BRAKE-PAD-001', N'Bố thắng trước', N'set', 1250000, 1),
    (N'BATTERY-12V-001', N'Ắc quy 12V', N'pcs', 2400000, 1),
    (N'WIPER-STD-001', N'Cần gạt mưa tiêu chuẩn', N'pair', 450000, 1)
) AS source (PartCode, PartName, Unit, ListPrice, IsActive)
ON target.PartCode = source.PartCode
WHEN MATCHED THEN
    UPDATE SET PartName = source.PartName, Unit = source.Unit, ListPrice = source.ListPrice, IsActive = source.IsActive
WHEN NOT MATCHED THEN
    INSERT (PartCode, PartName, Unit, ListPrice, IsActive)
    VALUES (source.PartCode, source.PartName, source.Unit, source.ListPrice, source.IsActive);
GO

MERGE dbo.PartInventory AS target
USING
(
    SELECT p.Id AS PartId, loc.Id AS LocationId, source.QuantityOnHand, source.MinStockLevel
    FROM
    (
        VALUES
            (N'FILTER-OIL-001', N'HCM-WORKSHOP', 24, 8),
            (N'BRAKE-PAD-001', N'HCM-WORKSHOP', 12, 4),
            (N'BATTERY-12V-001', N'HCM-WORKSHOP', 6, 2),
            (N'WIPER-STD-001', N'HCM-WORKSHOP', 18, 6)
    ) source(PartCode, LocationCode, QuantityOnHand, MinStockLevel)
    INNER JOIN dbo.Parts p ON p.PartCode = source.PartCode
    INNER JOIN dbo.Locations loc ON loc.LocationCode = source.LocationCode
) AS source
ON target.PartId = source.PartId AND target.LocationId = source.LocationId
WHEN MATCHED THEN
    UPDATE SET MinStockLevel = source.MinStockLevel
WHEN NOT MATCHED THEN
    INSERT (PartId, LocationId, QuantityOnHand, MinStockLevel)
    VALUES (source.PartId, source.LocationId, source.QuantityOnHand, source.MinStockLevel);
GO

SELECT DB_NAME() AS CurrentDb;
GO

-- 2) Kiểm tra bảng StaffUsers đã tồn tại chưa
SELECT OBJECT_ID(N'dbo.StaffUsers', N'U') AS StaffUsersObjectId;
GO

-- 3) Kiểm tra cột + kiểu dữ liệu của StaffUsers
SELECT
  c.name AS ColumnName,
  t.name AS TypeName,
  CASE WHEN c.max_length = -1 THEN -1 ELSE c.max_length END AS MaxLengthBytes,
  c.is_nullable AS IsNullable
FROM sys.columns c
JOIN sys.types t
  ON t.user_type_id = c.user_type_id AND t.system_type_id = c.system_type_id
WHERE c.object_id = OBJECT_ID(N'dbo.StaffUsers')
ORDER BY c.column_id;
GO

-- 4) Kiểm tra unique index cho Username (phải có 1 index unique)
SELECT
  i.name,
  i.is_unique,
  i.type_desc
FROM sys.indexes i
WHERE i.object_id = OBJECT_ID(N'dbo.StaffUsers')
  AND i.name = N'UX_StaffUsers_Username';
GO

-- 5) Kiểm tra dữ liệu mẫu (nếu có)
SELECT TOP (20) Id, Username, DisplayName, Role, CreatedAt
FROM dbo.StaffUsers
ORDER BY CreatedAt DESC, Id DESC;
GO

go
