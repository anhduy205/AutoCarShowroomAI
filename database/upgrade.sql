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
