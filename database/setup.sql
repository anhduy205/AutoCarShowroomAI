IF DB_ID(N'AutoCarShowroomDb') IS NULL
BEGIN
    CREATE DATABASE AutoCarShowroomDb;
END;
GO

USE AutoCarShowroomDb;
GO

IF OBJECT_ID(N'dbo.AuditLogs', N'U') IS NOT NULL DROP TABLE dbo.AuditLogs;
IF OBJECT_ID(N'dbo.OrderItems', N'U') IS NOT NULL DROP TABLE dbo.OrderItems;
IF OBJECT_ID(N'dbo.Orders', N'U') IS NOT NULL DROP TABLE dbo.Orders;
IF OBJECT_ID(N'dbo.StaffUsers', N'U') IS NOT NULL DROP TABLE dbo.StaffUsers;
IF OBJECT_ID(N'dbo.Cars', N'U') IS NOT NULL DROP TABLE dbo.Cars;
IF OBJECT_ID(N'dbo.Brands', N'U') IS NOT NULL DROP TABLE dbo.Brands;
GO

CREATE TABLE dbo.Brands
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL UNIQUE,
    CONSTRAINT CK_Brands_Name_NotBlank CHECK (LEN(LTRIM(RTRIM(Name))) > 0)
);
GO

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
    CONSTRAINT FK_Cars_Brands FOREIGN KEY (BrandId) REFERENCES dbo.Brands(Id),
    CONSTRAINT CK_Cars_Name_NotBlank CHECK (LEN(LTRIM(RTRIM(Name))) > 0),
    CONSTRAINT CK_Cars_Year CHECK ([Year] IS NULL OR ([Year] BETWEEN 1900 AND 2100)),
    CONSTRAINT CK_Cars_Status_Valid CHECK (Status IN (N'InStock', N'Sold', N'Promotion')),
    CONSTRAINT CK_Cars_Price CHECK (Price >= 0),
    CONSTRAINT CK_Cars_StockQuantity CHECK (StockQuantity >= 0)
);
GO

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
GO

CREATE TABLE dbo.OrderItems
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    OrderId INT NOT NULL,
    CarId INT NOT NULL,
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(18,2) NOT NULL,
    CONSTRAINT FK_OrderItems_Orders FOREIGN KEY (OrderId) REFERENCES dbo.Orders(Id),
    CONSTRAINT FK_OrderItems_Cars FOREIGN KEY (CarId) REFERENCES dbo.Cars(Id),
    CONSTRAINT CK_OrderItems_Quantity CHECK (Quantity > 0),
    CONSTRAINT CK_OrderItems_UnitPrice CHECK (UnitPrice >= 0)
);
GO

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
GO

CREATE UNIQUE INDEX UX_StaffUsers_Username ON dbo.StaffUsers (Username);
GO

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
GO

CREATE INDEX IX_AuditLogs_CreatedAt ON dbo.AuditLogs (CreatedAt DESC);
GO

INSERT INTO dbo.Brands (Name)
VALUES
    (N'Toyota'),
    (N'Hyundai'),
    (N'Ford'),
    (N'Mazda');
GO

INSERT INTO dbo.Cars (BrandId, Name, [Year], [Type], Color, [Description], Specifications, ImageUrls, Status, Price, StockQuantity)
VALUES
    (1, N'Toyota Camry', 2023, N'Sedan', N'Den', N'Sedan hang D, van hanh em ai, noi that rong rai.', N'Dong co: 2.0L\nHop so: AT\nSo cho: 5\nNhien lieu: Xang', NULL, N'InStock', 1200000000, 2),
    (1, N'Toyota Corolla Cross', 2024, N'SUV', N'Trang', N'Crossover 5 cho, tiet kiem nhien lieu.', N'Dong co: 1.8L\nHop so: CVT\nSo cho: 5\nNhien lieu: Xang', NULL, N'Promotion', 890000000, 4),
    (2, N'Hyundai Accent', 2023, N'Sedan', N'Do', N'Sedan hang B pho thong, de bao duong.', N'Dong co: 1.4L\nHop so: AT\nSo cho: 5\nNhien lieu: Xang', NULL, N'InStock', 520000000, 4),
    (2, N'Hyundai Tucson', 2024, N'SUV', N'Xam', N'SUV 5 cho, phu hop gia dinh.', N'Dong co: 2.0L\nHop so: AT\nSo cho: 5\nNhien lieu: Xang', NULL, N'InStock', 845000000, 5),
    (3, N'Ford Everest', 2023, N'SUV', N'Trang', N'SUV 7 cho khung gam cao, di du lich.', N'Dong co: 2.0L\nHop so: AT\nSo cho: 7\nNhien lieu: Dau', NULL, N'InStock', 1399000000, 1),
    (4, N'Mazda CX-5', 2024, N'SUV', N'Xanh', N'SUV 5 cho thiet ke tre trung, option tot.', N'Dong co: 2.0L\nHop so: AT\nSo cho: 5\nNhien lieu: Xang', NULL, N'InStock', 799000000, 5);
GO

INSERT INTO dbo.Orders (CustomerName, Status)
VALUES
    (N'Nguyen Van A', N'Completed'),
    (N'Tran Thi B', N'Completed'),
    (N'Le Van C', N'Paid');
GO

INSERT INTO dbo.OrderItems (OrderId, CarId, Quantity, UnitPrice)
VALUES
    (1, 1, 1, 1200000000),
    (1, 6, 1, 799000000),
    (2, 3, 2, 520000000),
    (3, 2, 1, 890000000),
    (3, 5, 1, 1399000000);
GO
