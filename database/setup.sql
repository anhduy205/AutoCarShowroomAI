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

IF OBJECT_ID(N'dbo.AuditLogs', N'U') IS NOT NULL DROP TABLE dbo.AuditLogs;
IF OBJECT_ID(N'dbo.DeliveryChecklistItems', N'U') IS NOT NULL DROP TABLE dbo.DeliveryChecklistItems;
IF OBJECT_ID(N'dbo.VehicleDeliveries', N'U') IS NOT NULL DROP TABLE dbo.VehicleDeliveries;
IF OBJECT_ID(N'dbo.RegistrationTasks', N'U') IS NOT NULL DROP TABLE dbo.RegistrationTasks;
IF OBJECT_ID(N'dbo.ContractAccessories', N'U') IS NOT NULL DROP TABLE dbo.ContractAccessories;
IF OBJECT_ID(N'dbo.SalesContracts', N'U') IS NOT NULL DROP TABLE dbo.SalesContracts;
IF OBJECT_ID(N'dbo.SalesQuotes', N'U') IS NOT NULL DROP TABLE dbo.SalesQuotes;
IF OBJECT_ID(N'dbo.TestDrives', N'U') IS NOT NULL DROP TABLE dbo.TestDrives;
IF OBJECT_ID(N'dbo.SalesOpportunities', N'U') IS NOT NULL DROP TABLE dbo.SalesOpportunities;
IF OBJECT_ID(N'dbo.Leads', N'U') IS NOT NULL DROP TABLE dbo.Leads;
IF OBJECT_ID(N'dbo.CustomerRequests', N'U') IS NOT NULL DROP TABLE dbo.CustomerRequests;
IF OBJECT_ID(N'dbo.OrderItems', N'U') IS NOT NULL DROP TABLE dbo.OrderItems;
IF OBJECT_ID(N'dbo.Orders', N'U') IS NOT NULL DROP TABLE dbo.Orders;
IF OBJECT_ID(N'dbo.PartStockMovements', N'U') IS NOT NULL DROP TABLE dbo.PartStockMovements;
IF OBJECT_ID(N'dbo.PartPurchaseOrderItems', N'U') IS NOT NULL DROP TABLE dbo.PartPurchaseOrderItems;
IF OBJECT_ID(N'dbo.PartPurchaseOrders', N'U') IS NOT NULL DROP TABLE dbo.PartPurchaseOrders;
IF OBJECT_ID(N'dbo.PartInventory', N'U') IS NOT NULL DROP TABLE dbo.PartInventory;
IF OBJECT_ID(N'dbo.Parts', N'U') IS NOT NULL DROP TABLE dbo.Parts;
IF OBJECT_ID(N'dbo.VehicleLocationMovements', N'U') IS NOT NULL DROP TABLE dbo.VehicleLocationMovements;
IF OBJECT_ID(N'dbo.NewVehicles', N'U') IS NOT NULL DROP TABLE dbo.NewVehicles;
IF OBJECT_ID(N'dbo.FactoryOrderItems', N'U') IS NOT NULL DROP TABLE dbo.FactoryOrderItems;
IF OBJECT_ID(N'dbo.FactoryOrders', N'U') IS NOT NULL DROP TABLE dbo.FactoryOrders;
IF OBJECT_ID(N'dbo.Locations', N'U') IS NOT NULL DROP TABLE dbo.Locations;
IF OBJECT_ID(N'dbo.VehicleModels', N'U') IS NOT NULL DROP TABLE dbo.VehicleModels;
IF OBJECT_ID(N'dbo.StaffUserRoles', N'U') IS NOT NULL DROP TABLE dbo.StaffUserRoles;
IF OBJECT_ID(N'dbo.StaffUsers', N'U') IS NOT NULL DROP TABLE dbo.StaffUsers;
IF OBJECT_ID(N'dbo.Roles', N'U') IS NOT NULL DROP TABLE dbo.Roles;
IF OBJECT_ID(N'dbo.Customers', N'U') IS NOT NULL DROP TABLE dbo.Customers;
IF OBJECT_ID(N'dbo.Cars', N'U') IS NOT NULL DROP TABLE dbo.Cars;
IF OBJECT_ID(N'dbo.Brands', N'U') IS NOT NULL DROP TABLE dbo.Brands;
IF OBJECT_ID(N'dbo.Branches', N'U') IS NOT NULL DROP TABLE dbo.Branches;
GO

CREATE TABLE dbo.Brands
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL UNIQUE,
    CONSTRAINT CK_Brands_Name_NotBlank CHECK (LEN(LTRIM(RTRIM(Name))) > 0)
);
GO

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
GO

CREATE UNIQUE INDEX UX_Branches_BranchCode ON dbo.Branches (BranchCode);
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
GO

CREATE UNIQUE INDEX UX_Customers_CustomerCode ON dbo.Customers (CustomerCode);
CREATE UNIQUE INDEX UX_Customers_Email_NotNull ON dbo.Customers (Email) WHERE Email IS NOT NULL;
CREATE INDEX IX_Customers_Phone ON dbo.Customers (Phone) WHERE Phone IS NOT NULL;
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
GO

CREATE UNIQUE INDEX UX_VehicleModels_ModelCode ON dbo.VehicleModels (ModelCode);
GO

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
GO

CREATE UNIQUE INDEX UX_Locations_LocationCode ON dbo.Locations (LocationCode);
GO

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
    CONSTRAINT CK_FactoryOrders_No_NotBlank CHECK (LEN(LTRIM(RTRIM(FactoryOrderNo))) > 0),
    CONSTRAINT CK_FactoryOrders_Status CHECK (Status IN (N'Planned', N'Ordered', N'InTransit', N'Received', N'Cancelled'))
);
GO

CREATE UNIQUE INDEX UX_FactoryOrders_No ON dbo.FactoryOrders (FactoryOrderNo);
GO

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
GO

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
    CONSTRAINT CK_NewVehicles_Vin_Format CHECK
    (
        LEN(Vin) = 17
        AND Vin = UPPER(Vin)
        AND Vin NOT LIKE '%[IOQ]%'
        AND Vin NOT LIKE '%[^A-HJ-NPR-Z0-9]%'
    ),
    CONSTRAINT CK_NewVehicles_CostPrice CHECK (CostPrice >= 0),
    CONSTRAINT CK_NewVehicles_Msrp CHECK (Msrp >= 0),
    CONSTRAINT CK_NewVehicles_Odometer CHECK (CurrentOdometer >= 0),
    CONSTRAINT CK_NewVehicles_Status CHECK (Status IN (N'Ordered', N'Inbound', N'Ready', N'OnDisplay', N'Allocated_Locked', N'PendingDelivery', N'Delivered', N'ServiceHold', N'DamagedHold', N'ReturnedToFactory'))
);
GO

CREATE UNIQUE INDEX UX_NewVehicles_Vin ON dbo.NewVehicles (Vin);
CREATE UNIQUE INDEX UX_NewVehicles_EngineNumber ON dbo.NewVehicles (EngineNumber);
CREATE INDEX IX_NewVehicles_Model_Status ON dbo.NewVehicles (VehicleModelId, Status);
GO

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
    CONSTRAINT FK_VehicleLocationMovements_ToLocations FOREIGN KEY (ToLocationId) REFERENCES dbo.Locations(Id)
);
GO

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
    CONSTRAINT FK_Leads_DesiredModels FOREIGN KEY (DesiredModelId) REFERENCES dbo.VehicleModels(Id),
    CONSTRAINT CK_Leads_Source CHECK (Source IN (N'Website', N'Showroom', N'App', N'Phone', N'Social', N'Referral', N'Other')),
    CONSTRAINT CK_Leads_Status CHECK (Status IN (N'New', N'Qualified', N'Unqualified', N'Converted', N'Lost')),
    CONSTRAINT CK_Leads_Score CHECK (LeadScore BETWEEN 0 AND 100),
    CONSTRAINT CK_Leads_ContactName_NotBlank CHECK (LEN(LTRIM(RTRIM(ContactName))) > 0),
    CONSTRAINT CK_Leads_Contact CHECK (ContactPhone IS NOT NULL OR ContactEmail IS NOT NULL)
);
GO

CREATE INDEX IX_Leads_Status_AssignedSales ON dbo.Leads (Status, AssignedSalesStaffUserId);
GO

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
    CONSTRAINT CK_SalesOpportunities_Name_NotBlank CHECK (LEN(LTRIM(RTRIM(OpportunityName))) > 0),
    CONSTRAINT CK_SalesOpportunities_Stage CHECK (Stage IN (N'LeadManagement', N'NeedsAnalysis', N'TestDrive', N'Quote', N'Negotiation', N'Contract', N'Won', N'Lost')),
    CONSTRAINT CK_SalesOpportunities_Probability CHECK (ProbabilityPercent BETWEEN 0 AND 100)
);
GO

CREATE INDEX IX_SalesOpportunities_Stage ON dbo.SalesOpportunities (Stage, CreatedAt DESC);
GO

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
GO

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
GO

CREATE UNIQUE INDEX UX_SalesQuotes_QuoteNo ON dbo.SalesQuotes (QuoteNo);
GO

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
    CONSTRAINT CK_SalesContracts_No_NotBlank CHECK (LEN(LTRIM(RTRIM(ContractNo))) > 0),
    CONSTRAINT CK_SalesContracts_PaymentMethod CHECK (PaymentMethod IN (N'Cash', N'Installment')),
    CONSTRAINT CK_SalesContracts_Status CHECK (Status IN (N'Draft', N'DepositPaid', N'Signed', N'PendingRegistration', N'PendingDelivery', N'Delivered', N'Cancelled', N'Voided')),
    CONSTRAINT CK_SalesContracts_Amounts CHECK (FinalSalePrice >= 0 AND DepositAmount >= 0)
);
GO

CREATE UNIQUE INDEX UX_SalesContracts_ContractNo ON dbo.SalesContracts (ContractNo);
CREATE UNIQUE INDEX UX_SalesContracts_OneOpenContractPerVehicle
ON dbo.SalesContracts (NewVehicleId)
WHERE Status <> N'Cancelled' AND Status <> N'Voided' AND Status <> N'Delivered';
GO

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
GO

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
    CONSTRAINT CK_RegistrationTasks_Type CHECK (TaskType IN (N'RegistrationTax', N'PlateNumber', N'Inspection', N'Insurance', N'Other')),
    CONSTRAINT CK_RegistrationTasks_Status CHECK (Status IN (N'Pending', N'Submitted', N'Completed', N'Cancelled')),
    CONSTRAINT CK_RegistrationTasks_Amount CHECK (Amount >= 0)
);
GO

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
    CONSTRAINT CK_VehicleDeliveries_Odometer CHECK (OdometerAtDelivery >= 0),
    CONSTRAINT CK_VehicleDeliveries_AcceptanceTime CHECK (CustomerAccepted = 0 OR DeliveredAt IS NOT NULL)
);
GO

CREATE UNIQUE INDEX UX_VehicleDeliveries_SalesContractId ON dbo.VehicleDeliveries (SalesContractId);
GO

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
GO

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
GO

CREATE UNIQUE INDEX UX_Parts_PartCode ON dbo.Parts (PartCode);
GO

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
GO

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
    CONSTRAINT CK_PartPurchaseOrders_No_NotBlank CHECK (LEN(LTRIM(RTRIM(PurchaseOrderNo))) > 0),
    CONSTRAINT CK_PartPurchaseOrders_Supplier_NotBlank CHECK (LEN(LTRIM(RTRIM(SupplierName))) > 0),
    CONSTRAINT CK_PartPurchaseOrders_Status CHECK (Status IN (N'Draft', N'Approved', N'Ordered', N'PartiallyReceived', N'Received', N'Cancelled'))
);
GO

CREATE UNIQUE INDEX UX_PartPurchaseOrders_No ON dbo.PartPurchaseOrders (PurchaseOrderNo);
GO

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
GO

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
    CONSTRAINT CK_PartStockMovements_Type CHECK (MovementType IN (N'Receive', N'Issue', N'Adjust', N'TransferIn', N'TransferOut')),
    CONSTRAINT CK_PartStockMovements_QuantityDelta CHECK (QuantityDelta <> 0)
);
GO

CREATE TABLE dbo.Roles
(
    RoleCode NVARCHAR(50) NOT NULL PRIMARY KEY,
    RoleName NVARCHAR(150) NOT NULL,
    CONSTRAINT CK_Roles_Code_NotBlank CHECK (LEN(LTRIM(RTRIM(RoleCode))) > 0),
    CONSTRAINT CK_Roles_Name_NotBlank CHECK (LEN(LTRIM(RTRIM(RoleName))) > 0)
);
GO

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
GO

CREATE UNIQUE INDEX UX_StaffUsers_Username ON dbo.StaffUsers (Username);
CREATE UNIQUE INDEX UX_StaffUsers_StaffCode_NotNull ON dbo.StaffUsers (StaffCode) WHERE StaffCode IS NOT NULL;
CREATE UNIQUE INDEX UX_StaffUsers_Email_NotNull ON dbo.StaffUsers (Email) WHERE Email IS NOT NULL;
GO

ALTER TABLE dbo.FactoryOrders
ADD CONSTRAINT FK_FactoryOrders_StaffUsers FOREIGN KEY (OrderedByStaffUserId) REFERENCES dbo.StaffUsers(Id);

ALTER TABLE dbo.VehicleLocationMovements
ADD CONSTRAINT FK_VehicleLocationMovements_StaffUsers FOREIGN KEY (MovedByStaffUserId) REFERENCES dbo.StaffUsers(Id);

ALTER TABLE dbo.PartPurchaseOrders
ADD CONSTRAINT FK_PartPurchaseOrders_StaffUsers FOREIGN KEY (OrderedByStaffUserId) REFERENCES dbo.StaffUsers(Id);

ALTER TABLE dbo.PartStockMovements
ADD CONSTRAINT FK_PartStockMovements_StaffUsers FOREIGN KEY (CreatedByStaffUserId) REFERENCES dbo.StaffUsers(Id);

ALTER TABLE dbo.Leads
ADD CONSTRAINT FK_Leads_AssignedSales FOREIGN KEY (AssignedSalesStaffUserId) REFERENCES dbo.StaffUsers(Id);

ALTER TABLE dbo.SalesOpportunities
ADD CONSTRAINT FK_SalesOpportunities_SalesStaff FOREIGN KEY (SalesStaffUserId) REFERENCES dbo.StaffUsers(Id);

ALTER TABLE dbo.SalesContracts
ADD CONSTRAINT FK_SalesContracts_SalesStaff FOREIGN KEY (SalesStaffUserId) REFERENCES dbo.StaffUsers(Id);

ALTER TABLE dbo.SalesContracts
ADD CONSTRAINT FK_SalesContracts_CreatedBy FOREIGN KEY (CreatedByStaffUserId) REFERENCES dbo.StaffUsers(Id);

ALTER TABLE dbo.RegistrationTasks
ADD CONSTRAINT FK_RegistrationTasks_HandledBy FOREIGN KEY (HandledByStaffUserId) REFERENCES dbo.StaffUsers(Id);

ALTER TABLE dbo.VehicleDeliveries
ADD CONSTRAINT FK_VehicleDeliveries_StaffUsers FOREIGN KEY (DeliveryStaffUserId) REFERENCES dbo.StaffUsers(Id);
GO

CREATE TABLE dbo.StaffUserRoles
(
    StaffUserId INT NOT NULL,
    RoleCode NVARCHAR(50) NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_StaffUserRoles PRIMARY KEY (StaffUserId, RoleCode),
    CONSTRAINT FK_StaffUserRoles_StaffUsers FOREIGN KEY (StaffUserId) REFERENCES dbo.StaffUsers(Id) ON DELETE CASCADE,
    CONSTRAINT FK_StaffUserRoles_Roles FOREIGN KEY (RoleCode) REFERENCES dbo.Roles(RoleCode)
);
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

INSERT INTO dbo.Branches (BranchCode, Name, Status)
VALUES
    (N'HCM-01', N'Showroom Quận 1', N'Active'),
    (N'HN-01', N'Showroom Hà Nội', N'Active'),
    (N'DN-01', N'Showroom Đà Nẵng', N'Inactive');
GO

INSERT INTO dbo.Roles (RoleCode, RoleName)
VALUES
    (N'Administrator', N'Quản trị viên'),
    (N'Staff', N'Nhân viên'),
    (N'Sales', N'Tư vấn bán hàng'),
    (N'SalesManager', N'Quản lý bán hàng'),
    (N'ServiceAdvisor', N'Cố vấn dịch vụ'),
    (N'Technician', N'Kỹ thuật viên'),
    (N'PartsWarehouse', N'Kho phụ tùng'),
    (N'Accountant', N'Kế toán'),
    (N'InventoryManager', N'Quản lý tồn kho');
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

INSERT INTO dbo.Customers (CustomerCode, FullName, Phone, Email, TaxCode, Address, CustomerType)
VALUES
    (N'CUST0001', N'Phạm Thị Mai', N'0901000001', N'mai.pham@example.com', NULL, N'Quận 1, TP HCM', N'Individual'),
    (N'CUST0002', N'Đặng Minh Đức', N'0901000002', N'duc.dang@example.com', NULL, N'Quận Hai Bà Trưng, Hà Nội', N'Individual'),
    (N'CUST0003', N'Công ty Vận tải An Phát', N'0901000003', N'contact@anphat.example.com', N'0312345678', N'Thủ Đức, TP HCM', N'Company'),
    (N'CUST0004', N'Bùi Ngọc Anh', N'0901000004', N'anh.bui@example.com', NULL, N'Quận Thanh Khê, Đà Nẵng', N'Individual'),
    (N'CUST0005', N'Hồ Thị Lan', N'0901000005', N'lan.ho@example.com', NULL, N'Nha Trang, Khánh Hòa', N'Individual');
GO

INSERT INTO dbo.StaffUsers (Username, PasswordHash, DisplayName, Role)
VALUES
    (N'staff01', N'pbkdf2-sha256$210000$+VTZfrCSvIR/1F5AqQHfkg==$XcsUn4gL9GDo4a/aHwG2G9akbdtpvbHvPRfTnt5q1Lg=', N'Phạm Minh Quân', N'Administrator'),
    (N'staff02', N'pbkdf2-sha256$210000$+VTZfrCSvIR/1F5AqQHfkg==$XcsUn4gL9GDo4a/aHwG2G9akbdtpvbHvPRfTnt5q1Lg=', N'Hoàng Thu Hà', N'Staff'),
    (N'staff03', N'pbkdf2-sha256$210000$+VTZfrCSvIR/1F5AqQHfkg==$XcsUn4gL9GDo4a/aHwG2G9akbdtpvbHvPRfTnt5q1Lg=', N'Do Anh Khoa', N'Staff'),
    (N'staff04', N'pbkdf2-sha256$210000$+VTZfrCSvIR/1F5AqQHfkg==$XcsUn4gL9GDo4a/aHwG2G9akbdtpvbHvPRfTnt5q1Lg=', N'Nguyễn Bảo Ngọc', N'Staff'),
    (N'staff05', N'pbkdf2-sha256$210000$+VTZfrCSvIR/1F5AqQHfkg==$XcsUn4gL9GDo4a/aHwG2G9akbdtpvbHvPRfTnt5q1Lg=', N'Trần Gia Huy', N'Staff');
GO

UPDATE dbo.StaffUsers
SET
    BranchId = (SELECT Id FROM dbo.Branches WHERE BranchCode = N'HCM-01'),
    StaffCode = N'STAFF001',
    Email = N'quan.pham@example.com',
    Phone = N'0902000001',
    IsActive = 1
WHERE Username = N'staff01';

UPDATE dbo.StaffUsers
SET
    BranchId = (SELECT Id FROM dbo.Branches WHERE BranchCode = N'HCM-01'),
    StaffCode = N'STAFF002',
    Email = N'ha.hoang@example.com',
    Phone = N'0902000002',
    IsActive = 1
WHERE Username = N'staff02';

UPDATE dbo.StaffUsers
SET
    BranchId = (SELECT Id FROM dbo.Branches WHERE BranchCode = N'HN-01'),
    StaffCode = N'STAFF003',
    Email = N'khoa.do@example.com',
    Phone = N'0902000003',
    IsActive = 1
WHERE Username = N'staff03';

UPDATE dbo.StaffUsers
SET
    BranchId = (SELECT Id FROM dbo.Branches WHERE BranchCode = N'HN-01'),
    StaffCode = N'STAFF004',
    Email = N'ngoc.nguyen@example.com',
    Phone = N'0902000004',
    IsActive = 1
WHERE Username = N'staff04';

UPDATE dbo.StaffUsers
SET
    BranchId = (SELECT Id FROM dbo.Branches WHERE BranchCode = N'HCM-01'),
    StaffCode = N'STAFF005',
    Email = N'huy.tran@example.com',
    Phone = N'0902000005',
    IsActive = 1
WHERE Username = N'staff05';
GO

INSERT INTO dbo.StaffUserRoles (StaffUserId, RoleCode)
SELECT staff.Id, roles.RoleCode
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
) roles(Username, RoleCode)
    ON roles.Username = staff.Username;
GO

INSERT INTO dbo.VehicleModels (BrandId, ModelCode, ModelName, ModelYear, BasePrice)
SELECT b.Id, source.ModelCode, source.ModelName, source.ModelYear, source.BasePrice
FROM
(
    VALUES
        (N'Toyota', N'TOY-CAMRY-2024', N'Camry', 2024, 1200000000),
        (N'Toyota', N'TOY-FORTUNER-2024', N'Fortuner', 2024, 1185000000),
        (N'Hyundai', N'HYU-TUCSON-2024', N'Tucson', 2024, 845000000),
        (N'Ford', N'FOR-RANGER-2024', N'Ranger', 2024, 665000000),
        (N'Mazda', N'MAZ-CX5-2024', N'CX-5', 2024, 799000000)
) source(BrandName, ModelCode, ModelName, ModelYear, BasePrice)
INNER JOIN dbo.Brands b ON b.Name = source.BrandName;
GO

INSERT INTO dbo.Locations (BranchId, LocationCode, LocationName, LocationType, IsActive)
SELECT b.Id, source.LocationCode, source.LocationName, source.LocationType, source.IsActive
FROM
(
    VALUES
        (N'HCM-01', N'HCM-STORAGE', N'Kho xe HCM', N'Storage', 1),
        (N'HCM-01', N'HCM-SHOWROOM', N'Sàn trưng bày HCM', N'Showroom', 1),
        (N'HCM-01', N'HCM-WORKSHOP', N'Xưởng PDI HCM', N'Workshop', 1),
        (N'HN-01', N'HN-STORAGE', N'Kho xe Hà Nội', N'Storage', 1),
        (N'HN-01', N'HN-SHOWROOM', N'Sàn trưng bày Hà Nội', N'Showroom', 1)
) source(BranchCode, LocationCode, LocationName, LocationType, IsActive)
INNER JOIN dbo.Branches b ON b.BranchCode = source.BranchCode;
GO

INSERT INTO dbo.FactoryOrders (FactoryOrderNo, BranchId, OrderedByStaffUserId, OrderDate, ExpectedArrivalDate, Status)
SELECT N'FO-2026-0001', b.Id, s.Id, CAST('2026-05-15' AS date), CAST('2026-07-15' AS date), N'Ordered'
FROM dbo.Branches b
LEFT JOIN dbo.StaffUsers s ON s.Username = N'staff03'
WHERE b.BranchCode = N'HCM-01';
GO

INSERT INTO dbo.FactoryOrderItems (FactoryOrderId, VehicleModelId, Quantity, PlannedUnitCost)
SELECT fo.Id, vm.Id, 3, 610000000
FROM dbo.FactoryOrders fo
INNER JOIN dbo.VehicleModels vm ON vm.ModelCode = N'FOR-RANGER-2024'
WHERE fo.FactoryOrderNo = N'FO-2026-0001';
GO

INSERT INTO dbo.NewVehicles (VehicleModelId, FactoryOrderItemId, CurrentLocationId, Vin, EngineNumber, ExteriorColor, InteriorColor, CostPrice, Msrp, CurrentOdometer, Status, InboundCheckedAt)
SELECT vm.Id, foi.Id, loc.Id, source.Vin, source.EngineNumber, source.ExteriorColor, source.InteriorColor, source.CostPrice, source.Msrp, source.CurrentOdometer, source.Status, SYSUTCDATETIME()
FROM
(
    VALUES
        (N'FOR-RANGER-2024', N'1FTER4FH0RLA00001', N'ENG-RAN-0001', N'Cam', N'Đen', 610000000, 665000000, 8, N'Ready'),
        (N'FOR-RANGER-2024', N'1FTER4FH0RLA00002', N'ENG-RAN-0002', N'Trắng', N'Đen', 610000000, 665000000, 5, N'Ready'),
        (N'HYU-TUCSON-2024', N'KM8JB3AE0RU000001', N'ENG-TUC-0001', N'Xám', N'Nâu', 780000000, 845000000, 12, N'OnDisplay')
) source(ModelCode, Vin, EngineNumber, ExteriorColor, InteriorColor, CostPrice, Msrp, CurrentOdometer, Status)
INNER JOIN dbo.VehicleModels vm ON vm.ModelCode = source.ModelCode
LEFT JOIN dbo.FactoryOrderItems foi ON foi.VehicleModelId = vm.Id
INNER JOIN dbo.Locations loc ON loc.LocationCode = CASE WHEN source.Status = N'OnDisplay' THEN N'HCM-SHOWROOM' ELSE N'HCM-STORAGE' END;
GO

INSERT INTO dbo.VehicleLocationMovements (NewVehicleId, FromLocationId, ToLocationId, MovedByStaffUserId, Reason)
SELECT nv.Id, NULL, nv.CurrentLocationId, s.Id, N'Nhập kho ban đầu'
FROM dbo.NewVehicles nv
LEFT JOIN dbo.StaffUsers s ON s.Username = N'staff03';
GO

INSERT INTO dbo.Parts (PartCode, PartName, Unit, ListPrice, IsActive)
VALUES
    (N'FILTER-OIL-001', N'Lọc dầu động cơ', N'pcs', 180000, 1),
    (N'BRAKE-PAD-001', N'Bố thắng trước', N'set', 1250000, 1),
    (N'BATTERY-12V-001', N'Ắc quy 12V', N'pcs', 2400000, 1),
    (N'WIPER-STD-001', N'Cần gạt mưa tiêu chuẩn', N'pair', 450000, 1);
GO

INSERT INTO dbo.PartInventory (PartId, LocationId, QuantityOnHand, MinStockLevel)
SELECT p.Id, loc.Id, source.QuantityOnHand, source.MinStockLevel
FROM
(
    VALUES
        (N'FILTER-OIL-001', N'HCM-WORKSHOP', 24, 8),
        (N'BRAKE-PAD-001', N'HCM-WORKSHOP', 12, 4),
        (N'BATTERY-12V-001', N'HCM-WORKSHOP', 6, 2),
        (N'WIPER-STD-001', N'HCM-WORKSHOP', 18, 6)
) source(PartCode, LocationCode, QuantityOnHand, MinStockLevel)
INNER JOIN dbo.Parts p ON p.PartCode = source.PartCode
INNER JOIN dbo.Locations loc ON loc.LocationCode = source.LocationCode;
GO

INSERT INTO dbo.PartPurchaseOrders (PurchaseOrderNo, BranchId, SupplierName, OrderedByStaffUserId, OrderDate, ExpectedArrivalDate, Status)
SELECT N'PPO-2026-0001', b.Id, N'Nhà cung cấp phụ tùng HCM', s.Id, CAST('2026-05-20' AS date), CAST('2026-06-10' AS date), N'Ordered'
FROM dbo.Branches b
LEFT JOIN dbo.StaffUsers s ON s.Username = N'staff03'
WHERE b.BranchCode = N'HCM-01';
GO

INSERT INTO dbo.PartPurchaseOrderItems (PartPurchaseOrderId, PartId, OrderedQuantity, ReceivedQuantity, UnitCost)
SELECT po.Id, p.Id, 20, 0, 150000
FROM dbo.PartPurchaseOrders po
INNER JOIN dbo.Parts p ON p.PartCode = N'FILTER-OIL-001'
WHERE po.PurchaseOrderNo = N'PPO-2026-0001';
GO

INSERT INTO dbo.PartStockMovements (PartId, LocationId, MovementType, QuantityDelta, CreatedByStaffUserId, Note)
SELECT pi.PartId, pi.LocationId, N'Receive', pi.QuantityOnHand, s.Id, N'Tồn đầu kỳ'
FROM dbo.PartInventory pi
LEFT JOIN dbo.StaffUsers s ON s.Username = N'staff03';
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
