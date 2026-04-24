USE AutoCarShowRoomDb;
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
