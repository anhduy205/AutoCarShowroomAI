IF DB_ID(N'AutoCarShowroomDb') IS NULL
BEGIN
    CREATE DATABASE AutoCarShowroomDb;
END;
GO

USE AutoCarShowroomDb;
GO

IF OBJECT_ID(N'dbo.AuditLogs', N'U') IS NOT NULL DROP TABLE dbo.AuditLogs;
IF OBJECT_ID(N'dbo.CustomerRequests', N'U') IS NOT NULL DROP TABLE dbo.CustomerRequests;
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
    CONSTRAINT FK_CustomerRequests_Cars FOREIGN KEY (CarId) REFERENCES dbo.Cars(Id),
    CONSTRAINT CK_CustomerRequests_Type CHECK (RequestType IN (N'ViewCar', N'Consultation', N'Deposit', N'TestDrive')),
    CONSTRAINT CK_CustomerRequests_Status CHECK (Status IN (N'Pending', N'Confirmed', N'Cancelled')),
    CONSTRAINT CK_CustomerRequests_CustomerName_NotBlank CHECK (LEN(LTRIM(RTRIM(CustomerName))) > 0),
    CONSTRAINT CK_CustomerRequests_Contact CHECK (CustomerPhone IS NOT NULL OR CustomerEmail IS NOT NULL),
    CONSTRAINT CK_CustomerRequests_DepositAmount CHECK (DepositAmount IS NULL OR DepositAmount >= 0)
);
GO

CREATE INDEX IX_CustomerRequests_Status_CreatedAt ON dbo.CustomerRequests (Status, CreatedAt DESC);
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
    (1, N'Toyota Camry', 2023, N'Sedan', N'Đen', N'Sedan hạng D, vận hành êm ái, nội thất rộng rãi.', N'Động cơ: 2.0L\nHộp số: AT\nSố chỗ: 5\nNhiên liệu: Xăng', NULL, N'InStock', 1200000000, 2),
    (1, N'Toyota Corolla Cross', 2024, N'SUV', N'Trắng', N'Crossover 5 chỗ, tiết kiệm nhiên liệu.', N'Động cơ: 1.8L\nHộp số: CVT\nSố chỗ: 5\nNhiên liệu: Xăng', NULL, N'Promotion', 890000000, 4),
    (2, N'Hyundai Accent', 2023, N'Sedan', N'Đỏ', N'Sedan hạng B phổ thông, dễ bảo dưỡng.', N'Động cơ: 1.4L\nHộp số: AT\nSố chỗ: 5\nNhiên liệu: Xăng', NULL, N'InStock', 520000000, 4),
    (2, N'Hyundai Tucson', 2024, N'SUV', N'Xám', N'SUV 5 chỗ, phù hợp gia đình.', N'Động cơ: 2.0L\nHộp số: AT\nSố chỗ: 5\nNhiên liệu: Xăng', NULL, N'InStock', 845000000, 5),
    (3, N'Ford Everest', 2023, N'SUV', N'Trắng', N'SUV 7 chỗ khung gầm cao, đi du lịch.', N'Động cơ: 2.0L\nHộp số: AT\nSố chỗ: 7\nNhiên liệu: Dầu', NULL, N'InStock', 1399000000, 1),
    (4, N'Mazda CX-5', 2024, N'SUV', N'Xanh', N'SUV 5 chỗ thiết kế trẻ trung, option tốt.', N'Động cơ: 2.0L\nHộp số: AT\nSố chỗ: 5\nNhiên liệu: Xăng', NULL, N'InStock', 799000000, 5);
GO

INSERT INTO dbo.Cars (BrandId, Name, [Year], [Type], Color, [Description], Specifications, ImageUrls, Status, Price, StockQuantity)
VALUES
    (1, N'Toyota Vios', 2024, N'Sedan', N'Bạc', N'Sedan hạng B gọn gàng, tiết kiệm nhiên liệu, phù hợp đi phố.', N'Động cơ: 1.5L\nHộp số: CVT\nSố chỗ: 5\nNhiên liệu: Xăng', NULL, N'InStock', 489000000, 6),
    (1, N'Toyota Fortuner', 2024, N'SUV', N'Đen', N'SUV 7 chỗ khung gầm chắc chắn, phù hợp gia đình và du lịch.', N'Động cơ: 2.4L\nHộp số: AT\nSố chỗ: 7\nNhiên liệu: Dầu', NULL, N'InStock', 1185000000, 2),
    (1, N'Toyota Innova Cross', 2024, N'MPV', N'Trắng', N'MPV lai crossover, khoang nội thất rộng và linh hoạt.', N'Động cơ: 2.0L\nHộp số: CVT\nSố chỗ: 7\nNhiên liệu: Xăng', NULL, N'Promotion', 810000000, 3),
    (1, N'Toyota Raize', 2023, N'SUV', N'Đỏ', N'SUV đô thị nhỏ gọn, dễ lái trong đường đông.', N'Động cơ: 1.0L Turbo\nHộp số: CVT\nSố chỗ: 5\nNhiên liệu: Xăng', NULL, N'InStock', 552000000, 5),
    (2, N'Hyundai Creta', 2024, N'SUV', N'Trắng', N'Crossover 5 chỗ cân bằng giữa tiện nghi và chi phí vận hành.', N'Động cơ: 1.5L\nHộp số: IVT\nSố chỗ: 5\nNhiên liệu: Xăng', NULL, N'InStock', 640000000, 4),
    (2, N'Hyundai Santa Fe', 2024, N'SUV', N'Xám', N'SUV 7 chỗ nhiều công nghệ an toàn, nội thất rộng.', N'Động cơ: 2.5L\nHộp số: AT\nSố chỗ: 7\nNhiên liệu: Xăng', NULL, N'InStock', 1069000000, 2),
    (2, N'Hyundai Grand i10', 2023, N'Hatchback', N'Vàng', N'Xe đô thị cỡ nhỏ, linh hoạt và dễ sử dụng hằng ngày.', N'Động cơ: 1.2L\nHộp số: AT\nSố chỗ: 5\nNhiên liệu: Xăng', NULL, N'InStock', 380000000, 6),
    (2, N'Hyundai Stargazer', 2024, N'MPV', N'Bạc', N'MPV 7 chỗ thiết kế thực dụng, phù hợp dịch vụ và gia đình.', N'Động cơ: 1.5L\nHộp số: IVT\nSố chỗ: 7\nNhiên liệu: Xăng', NULL, N'Promotion', 575000000, 3),
    (3, N'Ford Ranger', 2024, N'Pickup', N'Cam', N'Bán tải mạnh mẽ, sức kéo tốt, phù hợp công việc và đi xa.', N'Động cơ: 2.0L\nHộp số: AT\nSố chỗ: 5\nNhiên liệu: Dầu', NULL, N'InStock', 665000000, 4),
    (3, N'Ford Territory', 2024, N'SUV', N'Xanh', N'SUV 5 chỗ rộng rãi, trang bị tiện nghi cho gia đình trẻ.', N'Động cơ: 1.5L Turbo\nHộp số: AT\nSố chỗ: 5\nNhiên liệu: Xăng', NULL, N'InStock', 799000000, 3),
    (3, N'Ford Explorer', 2023, N'SUV', N'Đen', N'SUV cỡ lớn nhập khẩu, khoang cabin cao cấp và động cơ khỏe.', N'Động cơ: 2.3L Turbo\nHộp số: AT\nSố chỗ: 7\nNhiên liệu: Xăng', NULL, N'InStock', 2099000000, 1),
    (3, N'Ford Transit', 2024, N'Van', N'Trắng', N'Xe 16 chỗ phù hợp vận tải hành khách và doanh nghiệp.', N'Động cơ: 2.2L\nHộp số: MT\nSố chỗ: 16\nNhiên liệu: Dầu', NULL, N'InStock', 905000000, 2),
    (4, N'Mazda 2', 2024, N'Sedan', N'Đỏ', N'Sedan nhỏ gọn, thiết kế trẻ trung, lái nhẹ trong đô thị.', N'Động cơ: 1.5L\nHộp số: AT\nSố chỗ: 5\nNhiên liệu: Xăng', NULL, N'InStock', 420000000, 5),
    (4, N'Mazda 3', 2024, N'Sedan', N'Xám', N'Sedan hạng C thiết kế đẹp, cảm giác lái tốt.', N'Động cơ: 1.5L\nHộp số: AT\nSố chỗ: 5\nNhiên liệu: Xăng', NULL, N'Promotion', 579000000, 4),
    (4, N'Mazda CX-30', 2024, N'SUV', N'Trắng', N'Crossover nhỏ gọn, nội thất cao cấp trong tầm giá.', N'Động cơ: 2.0L\nHộp số: AT\nSố chỗ: 5\nNhiên liệu: Xăng', NULL, N'InStock', 699000000, 3),
    (4, N'Mazda CX-8', 2023, N'SUV', N'Đen', N'SUV 7 chỗ thanh lịch, phù hợp gia đình cần khoang rộng.', N'Động cơ: 2.5L\nHộp số: AT\nSố chỗ: 7\nNhiên liệu: Xăng', NULL, N'InStock', 949000000, 2),
    (1, N'Toyota Yaris Cross', 2024, N'SUV', N'Xanh', N'Crossover đô thị tiết kiệm, nhiều tính năng an toàn.', N'Động cơ: 1.5L\nHộp số: CVT\nSố chỗ: 5\nNhiên liệu: Xăng', NULL, N'InStock', 730000000, 4),
    (2, N'Hyundai Venue', 2024, N'SUV', N'Xanh rêu', N'SUV cỡ nhỏ thực dụng, phù hợp khách hàng mua xe lần đầu.', N'Động cơ: 1.0L Turbo\nHộp số: DCT\nSố chỗ: 5\nNhiên liệu: Xăng', NULL, N'InStock', 539000000, 5),
    (3, N'Ford EcoSport', 2022, N'SUV', N'Bạc', N'Crossover đô thị đã qua sử dụng ít, giá tốt để demo xe cũ.', N'Động cơ: 1.5L\nHộp số: AT\nSố chỗ: 5\nNhiên liệu: Xăng', NULL, N'InStock', 465000000, 2),
    (4, N'Mazda BT-50', 2023, N'Pickup', N'Xám', N'Bán tải thiết kế thực dụng, phù hợp công việc hằng ngày.', N'Động cơ: 1.9L\nHộp số: AT\nSố chỗ: 5\nNhiên liệu: Dầu', NULL, N'InStock', 659000000, 3);
GO

INSERT INTO dbo.StaffUsers (Username, PasswordHash, DisplayName, Role)
VALUES
    (N'staff01', N'pbkdf2-sha256$210000$+VTZfrCSvIR/1F5AqQHfkg==$XcsUn4gL9GDo4a/aHwG2G9akbdtpvbHvPRfTnt5q1Lg=', N'Phạm Minh Quân', N'Administrator'),
    (N'staff02', N'pbkdf2-sha256$210000$+VTZfrCSvIR/1F5AqQHfkg==$XcsUn4gL9GDo4a/aHwG2G9akbdtpvbHvPRfTnt5q1Lg=', N'Hoàng Thu Hà', N'Staff'),
    (N'staff03', N'pbkdf2-sha256$210000$+VTZfrCSvIR/1F5AqQHfkg==$XcsUn4gL9GDo4a/aHwG2G9akbdtpvbHvPRfTnt5q1Lg=', N'Do Anh Khoa', N'Staff'),
    (N'staff04', N'pbkdf2-sha256$210000$+VTZfrCSvIR/1F5AqQHfkg==$XcsUn4gL9GDo4a/aHwG2G9akbdtpvbHvPRfTnt5q1Lg=', N'Nguyễn Bảo Ngọc', N'Staff'),
    (N'staff05', N'pbkdf2-sha256$210000$+VTZfrCSvIR/1F5AqQHfkg==$XcsUn4gL9GDo4a/aHwG2G9akbdtpvbHvPRfTnt5q1Lg=', N'Trần Gia Huy', N'Staff');
GO

INSERT INTO dbo.Orders (CustomerName, Status)
VALUES
    (N'Nguyen Van A', N'Completed'),
    (N'Tran Thi B', N'Completed'),
    (N'Le Van C', N'Paid');
GO

INSERT INTO dbo.Orders (CustomerName, CustomerPhone, CustomerEmail, CustomerAddress, Note, Status)
VALUES
    (N'Phạm Thị Mai', N'0901000001', N'mai.pham@example.com', N'Quận 1, TP HCM', N'Quan tâm xe tiết kiệm nhiên liệu.', N'Pending'),
    (N'Đặng Minh Đức', N'0901000002', N'duc.dang@example.com', N'Quận Hai Bà Trưng, Hà Nội', N'Đã cọc giữ xe.', N'Paid'),
    (N'Võ Thanh Long', N'0901000003', N'long.vo@example.com', N'Thủ Đức, TP HCM', N'Giao xe cuối tuần.', N'Completed'),
    (N'Bùi Ngọc Anh', N'0901000004', N'anh.bui@example.com', N'Quận Thanh Khê, Đà Nẵng', N'Cần phụ kiện gia đình.', N'Delivered'),
    (N'Hồ Thị Lan', N'0901000005', N'lan.ho@example.com', N'Nha Trang, Khánh Hòa', N'Thanh toán chuyển khoản.', N'Paid'),
    (N'Ngô Quang Hiếu', N'0901000006', N'hieu.ngo@example.com', N'Biên Hòa, Đồng Nai', N'Khách hủy do đổi màu xe.', N'Cancelled'),
    (N'Lý Bảo Châu', N'0901000007', N'chau.ly@example.com', N'Cần Thơ', N'Đang chờ duyệt hồ sơ trả góp.', N'Pending'),
    (N'Trương Gia Bảo', N'0901000008', N'bao.truong@example.com', N'Huế', N'Khách mua thêm gói bảo dưỡng.', N'Completed'),
    (N'Nguyễn Minh Tâm', N'0901000009', N'tam.nguyen@example.com', N'Vũng Tàu', N'Lấy xe trong giờ hành chính.', N'Paid'),
    (N'Lê Phương Linh', N'0901000010', N'linh.le@example.com', N'Long Biên, Hà Nội', N'Giao xe tại showroom.', N'Delivered');
GO

INSERT INTO dbo.OrderItems (OrderId, CarId, Quantity, UnitPrice)
VALUES
    (1, 1, 1, 1200000000),
    (1, 6, 1, 799000000),
    (2, 3, 2, 520000000),
    (3, 2, 1, 890000000),
    (3, 5, 1, 1399000000);
GO

INSERT INTO dbo.OrderItems (OrderId, CarId, Quantity, UnitPrice)
VALUES
    (4, 7, 1, 489000000),
    (5, 10, 1, 552000000),
    (6, 12, 1, 1069000000),
    (7, 15, 2, 665000000),
    (8, 20, 1, 579000000),
    (9, 17, 1, 2099000000),
    (10, 23, 1, 730000000),
    (11, 19, 2, 420000000),
    (12, 24, 1, 539000000),
    (13, 26, 1, 659000000);
GO
