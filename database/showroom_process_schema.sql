/*
    AutoCarShowroom process database design
    Target: SQL Server

    This script is intentionally standalone. It creates a separate database
    so the current application schema can remain untouched while the fuller
    sales, service, inventory, and finance process design is reviewed.
*/

IF DB_ID(N'AutoCarShowroomProcessDb') IS NULL
BEGIN
    CREATE DATABASE AutoCarShowroomProcessDb;
END;
GO

USE AutoCarShowroomProcessDb;
GO

SET NOCOUNT ON;
GO

IF OBJECT_ID(N'sales.TR_SalesContracts_ValidatePriceAndLock', N'TR') IS NOT NULL DROP TRIGGER sales.TR_SalesContracts_ValidatePriceAndLock;
IF OBJECT_ID(N'sales.TR_SalesContracts_AccountingOnlyPriceChange', N'TR') IS NOT NULL DROP TRIGGER sales.TR_SalesContracts_AccountingOnlyPriceChange;
IF OBJECT_ID(N'sales.TR_VehicleDeliveries_SetDelivered', N'TR') IS NOT NULL DROP TRIGGER sales.TR_VehicleDeliveries_SetDelivered;
IF OBJECT_ID(N'inventory.TR_VehicleLocationMovements_ValidateTransfer', N'TR') IS NOT NULL DROP TRIGGER inventory.TR_VehicleLocationMovements_ValidateTransfer;
IF OBJECT_ID(N'finance.TR_SalesInvoices_ValidateVehicleAndCustomer', N'TR') IS NOT NULL DROP TRIGGER finance.TR_SalesInvoices_ValidateVehicleAndCustomer;
IF OBJECT_ID(N'finance.TR_SalesInvoices_PreventIssuedTimeChange', N'TR') IS NOT NULL DROP TRIGGER finance.TR_SalesInvoices_PreventIssuedTimeChange;
IF OBJECT_ID(N'finance.TR_ServiceInvoices_PreventIssuedTimeChange', N'TR') IS NOT NULL DROP TRIGGER finance.TR_ServiceInvoices_PreventIssuedTimeChange;
IF OBJECT_ID(N'finance.TR_Payments_PreventPaidAtUpdate', N'TR') IS NOT NULL DROP TRIGGER finance.TR_Payments_PreventPaidAtUpdate;
IF OBJECT_ID(N'sales.TR_SalesContracts_BlockUnderpaidSettlement', N'TR') IS NOT NULL DROP TRIGGER sales.TR_SalesContracts_BlockUnderpaidSettlement;
IF OBJECT_ID(N'service.TR_WorkOrders_BlockUnderpaidClose', N'TR') IS NOT NULL DROP TRIGGER service.TR_WorkOrders_BlockUnderpaidClose;
IF OBJECT_ID(N'service.TR_WorkOrders_RequirePartsBeforeCompletion', N'TR') IS NOT NULL DROP TRIGGER service.TR_WorkOrders_RequirePartsBeforeCompletion;
IF OBJECT_ID(N'service.TR_WorkOrderJobs_ValidateLaborLimit', N'TR') IS NOT NULL DROP TRIGGER service.TR_WorkOrderJobs_ValidateLaborLimit;
IF OBJECT_ID(N'service.TR_PartIssues_DecrementStock', N'TR') IS NOT NULL DROP TRIGGER service.TR_PartIssues_DecrementStock;
GO

IF OBJECT_ID(N'inventory.NewVehicles', N'U') IS NOT NULL
   AND EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_NewVehicles_AllocatedContracts')
BEGIN
    ALTER TABLE inventory.NewVehicles DROP CONSTRAINT FK_NewVehicles_AllocatedContracts;
END;
GO

IF OBJECT_ID(N'finance.TechnicianKpiBonuses', N'U') IS NOT NULL DROP TABLE finance.TechnicianKpiBonuses;
IF OBJECT_ID(N'finance.StaffCommissionLedger', N'U') IS NOT NULL DROP TABLE finance.StaffCommissionLedger;
IF OBJECT_ID(N'finance.CommissionRules', N'U') IS NOT NULL DROP TABLE finance.CommissionRules;
IF OBJECT_ID(N'finance.Reconciliations', N'U') IS NOT NULL DROP TABLE finance.Reconciliations;
IF OBJECT_ID(N'finance.FinancialAdjustments', N'U') IS NOT NULL DROP TABLE finance.FinancialAdjustments;
IF OBJECT_ID(N'finance.Payments', N'U') IS NOT NULL DROP TABLE finance.Payments;
IF OBJECT_ID(N'finance.ServiceInvoices', N'U') IS NOT NULL DROP TABLE finance.ServiceInvoices;
IF OBJECT_ID(N'finance.SalesInvoices', N'U') IS NOT NULL DROP TABLE finance.SalesInvoices;
IF OBJECT_ID(N'service.ServiceNotifications', N'U') IS NOT NULL DROP TABLE service.ServiceNotifications;
IF OBJECT_ID(N'service.QualityChecks', N'U') IS NOT NULL DROP TABLE service.QualityChecks;
IF OBJECT_ID(N'service.PartIssues', N'U') IS NOT NULL DROP TABLE service.PartIssues;
IF OBJECT_ID(N'service.WorkOrderParts', N'U') IS NOT NULL DROP TABLE service.WorkOrderParts;
IF OBJECT_ID(N'service.WorkOrderJobs', N'U') IS NOT NULL DROP TABLE service.WorkOrderJobs;
IF OBJECT_ID(N'service.StandardLaborOperations', N'U') IS NOT NULL DROP TABLE service.StandardLaborOperations;
IF OBJECT_ID(N'service.WorkOrders', N'U') IS NOT NULL DROP TABLE service.WorkOrders;
IF OBJECT_ID(N'service.IntakeMedia', N'U') IS NOT NULL DROP TABLE service.IntakeMedia;
IF OBJECT_ID(N'service.Intakes', N'U') IS NOT NULL DROP TABLE service.Intakes;
IF OBJECT_ID(N'service.ServiceBookings', N'U') IS NOT NULL DROP TABLE service.ServiceBookings;
IF OBJECT_ID(N'service.CustomerVehicles', N'U') IS NOT NULL DROP TABLE service.CustomerVehicles;
IF OBJECT_ID(N'sales.DeliveryChecklistItems', N'U') IS NOT NULL DROP TABLE sales.DeliveryChecklistItems;
IF OBJECT_ID(N'sales.VehicleDeliveries', N'U') IS NOT NULL DROP TABLE sales.VehicleDeliveries;
IF OBJECT_ID(N'sales.RegistrationTasks', N'U') IS NOT NULL DROP TABLE sales.RegistrationTasks;
IF OBJECT_ID(N'sales.ContractAccessories', N'U') IS NOT NULL DROP TABLE sales.ContractAccessories;
IF OBJECT_ID(N'sales.SalesContracts', N'U') IS NOT NULL DROP TABLE sales.SalesContracts;
IF OBJECT_ID(N'sales.SalesQuotes', N'U') IS NOT NULL DROP TABLE sales.SalesQuotes;
IF OBJECT_ID(N'sales.TestDrives', N'U') IS NOT NULL DROP TABLE sales.TestDrives;
IF OBJECT_ID(N'sales.SalesOpportunities', N'U') IS NOT NULL DROP TABLE sales.SalesOpportunities;
IF OBJECT_ID(N'sales.Leads', N'U') IS NOT NULL DROP TABLE sales.Leads;
IF OBJECT_ID(N'inventory.PartStockMovements', N'U') IS NOT NULL DROP TABLE inventory.PartStockMovements;
IF OBJECT_ID(N'inventory.PartPurchaseOrderItems', N'U') IS NOT NULL DROP TABLE inventory.PartPurchaseOrderItems;
IF OBJECT_ID(N'inventory.PartPurchaseOrders', N'U') IS NOT NULL DROP TABLE inventory.PartPurchaseOrders;
IF OBJECT_ID(N'inventory.PartInventory', N'U') IS NOT NULL DROP TABLE inventory.PartInventory;
IF OBJECT_ID(N'inventory.Parts', N'U') IS NOT NULL DROP TABLE inventory.Parts;
IF OBJECT_ID(N'inventory.VehicleLocationMovements', N'U') IS NOT NULL DROP TABLE inventory.VehicleLocationMovements;
IF OBJECT_ID(N'inventory.NewVehicles', N'U') IS NOT NULL DROP TABLE inventory.NewVehicles;
IF OBJECT_ID(N'inventory.FactoryOrderItems', N'U') IS NOT NULL DROP TABLE inventory.FactoryOrderItems;
IF OBJECT_ID(N'inventory.FactoryOrders', N'U') IS NOT NULL DROP TABLE inventory.FactoryOrders;
IF OBJECT_ID(N'inventory.Locations', N'U') IS NOT NULL DROP TABLE inventory.Locations;
IF OBJECT_ID(N'inventory.VehicleModels', N'U') IS NOT NULL DROP TABLE inventory.VehicleModels;
IF OBJECT_ID(N'inventory.Brands', N'U') IS NOT NULL DROP TABLE inventory.Brands;
IF OBJECT_ID(N'finance.Approvals', N'U') IS NOT NULL DROP TABLE finance.Approvals;
IF OBJECT_ID(N'core.StaffUserRoles', N'U') IS NOT NULL DROP TABLE core.StaffUserRoles;
IF OBJECT_ID(N'core.Roles', N'U') IS NOT NULL DROP TABLE core.Roles;
IF OBJECT_ID(N'core.StaffUsers', N'U') IS NOT NULL DROP TABLE core.StaffUsers;
IF OBJECT_ID(N'core.Customers', N'U') IS NOT NULL DROP TABLE core.Customers;
IF OBJECT_ID(N'core.Branches', N'U') IS NOT NULL DROP TABLE core.Branches;
GO

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'core') EXEC(N'CREATE SCHEMA core');
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'inventory') EXEC(N'CREATE SCHEMA inventory');
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'sales') EXEC(N'CREATE SCHEMA sales');
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'service') EXEC(N'CREATE SCHEMA service');
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'finance') EXEC(N'CREATE SCHEMA finance');
GO

CREATE TABLE core.Branches
(
    BranchId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Branches PRIMARY KEY,
    BranchCode NVARCHAR(20) NOT NULL CONSTRAINT UQ_Branches_BranchCode UNIQUE,
    Name NVARCHAR(150) NOT NULL,
    Status NVARCHAR(20) NOT NULL CONSTRAINT DF_Branches_Status DEFAULT N'Active',
    CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Branches_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT CK_Branches_Name_NotBlank CHECK (LEN(LTRIM(RTRIM(Name))) > 0),
    CONSTRAINT CK_Branches_Status CHECK (Status IN (N'Active', N'Inactive'))
);
GO

CREATE TABLE core.Customers
(
    CustomerId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Customers PRIMARY KEY,
    CustomerCode NVARCHAR(30) NOT NULL CONSTRAINT UQ_Customers_CustomerCode UNIQUE,
    FullName NVARCHAR(150) NOT NULL,
    Phone NVARCHAR(30) NOT NULL,
    Email NVARCHAR(255) NULL,
    TaxCode NVARCHAR(50) NULL,
    Address NVARCHAR(500) NULL,
    CustomerType NVARCHAR(20) NOT NULL CONSTRAINT DF_Customers_CustomerType DEFAULT N'Individual',
    CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Customers_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT CK_Customers_FullName_NotBlank CHECK (LEN(LTRIM(RTRIM(FullName))) > 0),
    CONSTRAINT CK_Customers_Phone_NotBlank CHECK (LEN(LTRIM(RTRIM(Phone))) > 0),
    CONSTRAINT CK_Customers_CustomerType CHECK (CustomerType IN (N'Individual', N'Company'))
);
GO

CREATE TABLE core.StaffUsers
(
    StaffId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_StaffUsers PRIMARY KEY,
    BranchId INT NOT NULL,
    StaffCode NVARCHAR(30) NOT NULL CONSTRAINT UQ_StaffUsers_StaffCode UNIQUE,
    FullName NVARCHAR(150) NOT NULL,
    Email NVARCHAR(255) NOT NULL CONSTRAINT UQ_StaffUsers_Email UNIQUE,
    Phone NVARCHAR(30) NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_StaffUsers_IsActive DEFAULT 1,
    CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_StaffUsers_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_StaffUsers_Branches FOREIGN KEY (BranchId) REFERENCES core.Branches(BranchId),
    CONSTRAINT CK_StaffUsers_FullName_NotBlank CHECK (LEN(LTRIM(RTRIM(FullName))) > 0)
);
GO

CREATE TABLE core.Roles
(
    RoleCode NVARCHAR(50) NOT NULL CONSTRAINT PK_Roles PRIMARY KEY,
    RoleName NVARCHAR(100) NOT NULL,
    CONSTRAINT CK_Roles_RoleCode CHECK (RoleCode IN
    (
        N'Admin',
        N'Sales',
        N'SalesManager',
        N'ServiceAdvisor',
        N'Technician',
        N'PartsWarehouse',
        N'Accountant',
        N'InventoryManager'
    ))
);
GO

INSERT INTO core.Roles (RoleCode, RoleName)
VALUES
    (N'Admin', N'Administrator'),
    (N'Sales', N'Sales consultant'),
    (N'SalesManager', N'Sales manager'),
    (N'ServiceAdvisor', N'Service advisor'),
    (N'Technician', N'Technician'),
    (N'PartsWarehouse', N'Parts warehouse'),
    (N'Accountant', N'Accountant'),
    (N'InventoryManager', N'Inventory manager');
GO

CREATE TABLE core.StaffUserRoles
(
    StaffId INT NOT NULL,
    RoleCode NVARCHAR(50) NOT NULL,
    AssignedAt DATETIME2(0) NOT NULL CONSTRAINT DF_StaffUserRoles_AssignedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_StaffUserRoles PRIMARY KEY (StaffId, RoleCode),
    CONSTRAINT FK_StaffUserRoles_StaffUsers FOREIGN KEY (StaffId) REFERENCES core.StaffUsers(StaffId),
    CONSTRAINT FK_StaffUserRoles_Roles FOREIGN KEY (RoleCode) REFERENCES core.Roles(RoleCode)
);
GO

CREATE TABLE finance.Approvals
(
    ApprovalId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Approvals PRIMARY KEY,
    ApprovalType NVARCHAR(40) NOT NULL,
    TargetEntity NVARCHAR(60) NOT NULL,
    TargetId INT NULL,
    RequestedByStaffId INT NULL,
    ApprovedByStaffId INT NULL,
    Status NVARCHAR(20) NOT NULL CONSTRAINT DF_Approvals_Status DEFAULT N'Pending',
    RequestedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Approvals_RequestedAt DEFAULT SYSUTCDATETIME(),
    ApprovedAt DATETIME2(0) NULL,
    Notes NVARCHAR(1000) NULL,
    CONSTRAINT FK_Approvals_RequestedBy FOREIGN KEY (RequestedByStaffId) REFERENCES core.StaffUsers(StaffId),
    CONSTRAINT FK_Approvals_ApprovedBy FOREIGN KEY (ApprovedByStaffId) REFERENCES core.StaffUsers(StaffId),
    CONSTRAINT CK_Approvals_Type CHECK (ApprovalType IN
    (
        N'BelowCostSale',
        N'MinStockOverride',
        N'PaymentShortfall',
        N'LaborOverrun',
        N'PriceChange'
    )),
    CONSTRAINT CK_Approvals_Status CHECK (Status IN (N'Pending', N'Approved', N'Rejected', N'Cancelled')),
    CONSTRAINT CK_Approvals_ApprovedFields CHECK
    (
        (Status = N'Approved' AND ApprovedByStaffId IS NOT NULL AND ApprovedAt IS NOT NULL)
        OR
        (Status <> N'Approved')
    )
);
GO

CREATE TABLE inventory.Brands
(
    BrandId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_InventoryBrands PRIMARY KEY,
    BrandName NVARCHAR(100) NOT NULL CONSTRAINT UQ_InventoryBrands_BrandName UNIQUE,
    CONSTRAINT CK_InventoryBrands_Name_NotBlank CHECK (LEN(LTRIM(RTRIM(BrandName))) > 0)
);
GO

CREATE TABLE inventory.VehicleModels
(
    ModelId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_VehicleModels PRIMARY KEY,
    BrandId INT NOT NULL,
    ModelCode NVARCHAR(50) NOT NULL CONSTRAINT UQ_VehicleModels_ModelCode UNIQUE,
    ModelName NVARCHAR(150) NOT NULL,
    ModelYear SMALLINT NOT NULL,
    BasePrice DECIMAL(18,2) NOT NULL,
    CONSTRAINT FK_VehicleModels_Brands FOREIGN KEY (BrandId) REFERENCES inventory.Brands(BrandId),
    CONSTRAINT CK_VehicleModels_ModelYear CHECK (ModelYear BETWEEN 1990 AND 2100),
    CONSTRAINT CK_VehicleModels_BasePrice CHECK (BasePrice >= 0),
    CONSTRAINT CK_VehicleModels_Name_NotBlank CHECK (LEN(LTRIM(RTRIM(ModelName))) > 0)
);
GO

CREATE TABLE inventory.Locations
(
    LocationId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Locations PRIMARY KEY,
    BranchId INT NOT NULL,
    LocationCode NVARCHAR(50) NOT NULL CONSTRAINT UQ_Locations_LocationCode UNIQUE,
    LocationName NVARCHAR(150) NOT NULL,
    LocationType NVARCHAR(30) NOT NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_Locations_IsActive DEFAULT 1,
    CONSTRAINT FK_Locations_Branches FOREIGN KEY (BranchId) REFERENCES core.Branches(BranchId),
    CONSTRAINT CK_Locations_Type CHECK (LocationType IN (N'Storage', N'Showroom', N'Workshop', N'Transit', N'Delivery')),
    CONSTRAINT CK_Locations_Name_NotBlank CHECK (LEN(LTRIM(RTRIM(LocationName))) > 0)
);
GO

CREATE TABLE inventory.FactoryOrders
(
    FactoryOrderId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_FactoryOrders PRIMARY KEY,
    FactoryOrderNo NVARCHAR(50) NOT NULL CONSTRAINT UQ_FactoryOrders_No UNIQUE,
    BranchId INT NOT NULL,
    OrderedByStaffId INT NOT NULL,
    OrderDate DATE NOT NULL,
    ExpectedArrivalDate DATE NULL,
    Status NVARCHAR(30) NOT NULL CONSTRAINT DF_FactoryOrders_Status DEFAULT N'Planned',
    CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_FactoryOrders_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_FactoryOrders_Branches FOREIGN KEY (BranchId) REFERENCES core.Branches(BranchId),
    CONSTRAINT FK_FactoryOrders_OrderedBy FOREIGN KEY (OrderedByStaffId) REFERENCES core.StaffUsers(StaffId),
    CONSTRAINT CK_FactoryOrders_Status CHECK (Status IN (N'Planned', N'Ordered', N'InTransit', N'Received', N'Cancelled'))
);
GO

CREATE TABLE inventory.FactoryOrderItems
(
    FactoryOrderItemId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_FactoryOrderItems PRIMARY KEY,
    FactoryOrderId INT NOT NULL,
    ModelId INT NOT NULL,
    Quantity INT NOT NULL,
    PlannedUnitCost DECIMAL(18,2) NOT NULL,
    CONSTRAINT FK_FactoryOrderItems_FactoryOrders FOREIGN KEY (FactoryOrderId) REFERENCES inventory.FactoryOrders(FactoryOrderId),
    CONSTRAINT FK_FactoryOrderItems_Models FOREIGN KEY (ModelId) REFERENCES inventory.VehicleModels(ModelId),
    CONSTRAINT CK_FactoryOrderItems_Quantity CHECK (Quantity > 0),
    CONSTRAINT CK_FactoryOrderItems_PlannedUnitCost CHECK (PlannedUnitCost >= 0)
);
GO

CREATE TABLE inventory.NewVehicles
(
    NewVehicleId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_NewVehicles PRIMARY KEY,
    ModelId INT NOT NULL,
    FactoryOrderItemId INT NULL,
    CurrentLocationId INT NULL,
    Vin CHAR(17) NOT NULL CONSTRAINT UQ_NewVehicles_Vin UNIQUE,
    EngineNumber NVARCHAR(50) NOT NULL CONSTRAINT UQ_NewVehicles_EngineNumber UNIQUE,
    ExteriorColor NVARCHAR(80) NULL,
    InteriorColor NVARCHAR(80) NULL,
    CostPrice DECIMAL(18,2) NOT NULL,
    Msrp DECIMAL(18,2) NOT NULL,
    CurrentOdometer INT NOT NULL CONSTRAINT DF_NewVehicles_CurrentOdometer DEFAULT 0,
    Status NVARCHAR(30) NOT NULL CONSTRAINT DF_NewVehicles_Status DEFAULT N'Ready',
    AllocatedContractId INT NULL,
    InboundCheckedAt DATETIME2(0) NULL,
    InboundDamageNote NVARCHAR(1000) NULL,
    CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_NewVehicles_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_NewVehicles_Models FOREIGN KEY (ModelId) REFERENCES inventory.VehicleModels(ModelId),
    CONSTRAINT FK_NewVehicles_FactoryOrderItems FOREIGN KEY (FactoryOrderItemId) REFERENCES inventory.FactoryOrderItems(FactoryOrderItemId),
    CONSTRAINT FK_NewVehicles_Locations FOREIGN KEY (CurrentLocationId) REFERENCES inventory.Locations(LocationId),
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
    CONSTRAINT CK_NewVehicles_Status CHECK (Status IN
    (
        N'Ordered',
        N'Inbound',
        N'Ready',
        N'OnDisplay',
        N'Allocated_Locked',
        N'PendingDelivery',
        N'Delivered',
        N'MaintenanceHold',
        N'Cancelled'
    ))
);
GO

CREATE INDEX IX_NewVehicles_ModelId_Status ON inventory.NewVehicles(ModelId, Status);
GO

CREATE TABLE inventory.VehicleLocationMovements
(
    MovementId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_VehicleLocationMovements PRIMARY KEY,
    NewVehicleId INT NOT NULL,
    FromLocationId INT NULL,
    ToLocationId INT NOT NULL,
    MovedByStaffId INT NOT NULL,
    MovedAt DATETIME2(0) NOT NULL CONSTRAINT DF_VehicleLocationMovements_MovedAt DEFAULT SYSUTCDATETIME(),
    Reason NVARCHAR(300) NULL,
    CONSTRAINT FK_VehicleLocationMovements_NewVehicles FOREIGN KEY (NewVehicleId) REFERENCES inventory.NewVehicles(NewVehicleId),
    CONSTRAINT FK_VehicleLocationMovements_FromLocations FOREIGN KEY (FromLocationId) REFERENCES inventory.Locations(LocationId),
    CONSTRAINT FK_VehicleLocationMovements_ToLocations FOREIGN KEY (ToLocationId) REFERENCES inventory.Locations(LocationId),
    CONSTRAINT FK_VehicleLocationMovements_StaffUsers FOREIGN KEY (MovedByStaffId) REFERENCES core.StaffUsers(StaffId)
);
GO

CREATE TABLE inventory.Parts
(
    PartId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Parts PRIMARY KEY,
    PartNo NVARCHAR(50) NOT NULL CONSTRAINT UQ_Parts_PartNo UNIQUE,
    PartName NVARCHAR(150) NOT NULL,
    Unit NVARCHAR(20) NOT NULL CONSTRAINT DF_Parts_Unit DEFAULT N'Piece',
    IsActive BIT NOT NULL CONSTRAINT DF_Parts_IsActive DEFAULT 1,
    CONSTRAINT CK_Parts_Name_NotBlank CHECK (LEN(LTRIM(RTRIM(PartName))) > 0)
);
GO

CREATE TABLE inventory.PartInventory
(
    LocationId INT NOT NULL,
    PartId INT NOT NULL,
    QuantityOnHand DECIMAL(18,2) NOT NULL CONSTRAINT DF_PartInventory_Quantity DEFAULT 0,
    MinStockLevel DECIMAL(18,2) NOT NULL CONSTRAINT DF_PartInventory_MinStock DEFAULT 0,
    ReorderQuantity DECIMAL(18,2) NOT NULL CONSTRAINT DF_PartInventory_ReorderQuantity DEFAULT 0,
    LastUpdatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_PartInventory_LastUpdatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_PartInventory PRIMARY KEY (LocationId, PartId),
    CONSTRAINT FK_PartInventory_Locations FOREIGN KEY (LocationId) REFERENCES inventory.Locations(LocationId),
    CONSTRAINT FK_PartInventory_Parts FOREIGN KEY (PartId) REFERENCES inventory.Parts(PartId),
    CONSTRAINT CK_PartInventory_Quantity CHECK (QuantityOnHand >= 0),
    CONSTRAINT CK_PartInventory_MinStock CHECK (MinStockLevel >= 0),
    CONSTRAINT CK_PartInventory_ReorderQuantity CHECK (ReorderQuantity >= 0)
);
GO

CREATE TABLE inventory.PartPurchaseOrders
(
    PartPurchaseOrderId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PartPurchaseOrders PRIMARY KEY,
    PurchaseOrderNo NVARCHAR(50) NOT NULL CONSTRAINT UQ_PartPurchaseOrders_No UNIQUE,
    BranchId INT NOT NULL,
    SupplierName NVARCHAR(150) NOT NULL,
    Status NVARCHAR(30) NOT NULL CONSTRAINT DF_PartPurchaseOrders_Status DEFAULT N'Draft',
    CreatedByStaffId INT NOT NULL,
    ApprovedByStaffId INT NULL,
    OrderedAt DATETIME2(0) NULL,
    ApprovedAt DATETIME2(0) NULL,
    ExpectedReceiptDate DATE NULL,
    CONSTRAINT FK_PartPurchaseOrders_Branches FOREIGN KEY (BranchId) REFERENCES core.Branches(BranchId),
    CONSTRAINT FK_PartPurchaseOrders_CreatedBy FOREIGN KEY (CreatedByStaffId) REFERENCES core.StaffUsers(StaffId),
    CONSTRAINT FK_PartPurchaseOrders_ApprovedBy FOREIGN KEY (ApprovedByStaffId) REFERENCES core.StaffUsers(StaffId),
    CONSTRAINT CK_PartPurchaseOrders_Status CHECK (Status IN (N'Draft', N'Approved', N'Ordered', N'PartiallyReceived', N'Received', N'Cancelled')),
    CONSTRAINT CK_PartPurchaseOrders_Approval CHECK
    (
        (Status IN (N'Approved', N'Ordered', N'PartiallyReceived', N'Received') AND ApprovedByStaffId IS NOT NULL AND ApprovedAt IS NOT NULL)
        OR
        (Status IN (N'Draft', N'Cancelled'))
    )
);
GO

CREATE TABLE inventory.PartPurchaseOrderItems
(
    PartPurchaseOrderItemId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PartPurchaseOrderItems PRIMARY KEY,
    PartPurchaseOrderId INT NOT NULL,
    PartId INT NOT NULL,
    OrderedQuantity DECIMAL(18,2) NOT NULL,
    ReceivedQuantity DECIMAL(18,2) NOT NULL CONSTRAINT DF_PartPurchaseOrderItems_ReceivedQuantity DEFAULT 0,
    UnitCost DECIMAL(18,2) NOT NULL,
    CONSTRAINT FK_PartPurchaseOrderItems_PurchaseOrders FOREIGN KEY (PartPurchaseOrderId) REFERENCES inventory.PartPurchaseOrders(PartPurchaseOrderId),
    CONSTRAINT FK_PartPurchaseOrderItems_Parts FOREIGN KEY (PartId) REFERENCES inventory.Parts(PartId),
    CONSTRAINT CK_PartPurchaseOrderItems_OrderedQuantity CHECK (OrderedQuantity > 0),
    CONSTRAINT CK_PartPurchaseOrderItems_ReceivedQuantity CHECK (ReceivedQuantity >= 0),
    CONSTRAINT CK_PartPurchaseOrderItems_UnitCost CHECK (UnitCost >= 0),
    CONSTRAINT CK_PartPurchaseOrderItems_ReceivedNotOverOrdered CHECK (ReceivedQuantity <= OrderedQuantity)
);
GO

CREATE TABLE inventory.PartStockMovements
(
    PartStockMovementId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PartStockMovements PRIMARY KEY,
    PartId INT NOT NULL,
    LocationId INT NOT NULL,
    MovementType NVARCHAR(30) NOT NULL,
    QuantityDelta DECIMAL(18,2) NOT NULL,
    WorkOrderId INT NULL,
    PartPurchaseOrderId INT NULL,
    CreatedByStaffId INT NULL,
    CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_PartStockMovements_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_PartStockMovements_Parts FOREIGN KEY (PartId) REFERENCES inventory.Parts(PartId),
    CONSTRAINT FK_PartStockMovements_Locations FOREIGN KEY (LocationId) REFERENCES inventory.Locations(LocationId),
    CONSTRAINT FK_PartStockMovements_PurchaseOrders FOREIGN KEY (PartPurchaseOrderId) REFERENCES inventory.PartPurchaseOrders(PartPurchaseOrderId),
    CONSTRAINT FK_PartStockMovements_StaffUsers FOREIGN KEY (CreatedByStaffId) REFERENCES core.StaffUsers(StaffId),
    CONSTRAINT CK_PartStockMovements_Type CHECK (MovementType IN (N'Issue', N'Receipt', N'Adjustment')),
    CONSTRAINT CK_PartStockMovements_QuantityDelta CHECK (QuantityDelta <> 0)
);
GO

CREATE TABLE sales.Leads
(
    LeadId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Leads PRIMARY KEY,
    CustomerId INT NULL,
    Source NVARCHAR(30) NOT NULL,
    ContactName NVARCHAR(150) NOT NULL,
    Phone NVARCHAR(30) NOT NULL,
    Email NVARCHAR(255) NULL,
    AssignedSalesStaffId INT NULL,
    LeadScore TINYINT NOT NULL CONSTRAINT DF_Leads_LeadScore DEFAULT 0,
    Status NVARCHAR(30) NOT NULL CONSTRAINT DF_Leads_Status DEFAULT N'New',
    CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Leads_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Leads_Customers FOREIGN KEY (CustomerId) REFERENCES core.Customers(CustomerId),
    CONSTRAINT FK_Leads_AssignedSales FOREIGN KEY (AssignedSalesStaffId) REFERENCES core.StaffUsers(StaffId),
    CONSTRAINT CK_Leads_Source CHECK (Source IN (N'Website', N'Showroom', N'App', N'Phone', N'Social', N'Referral', N'Other')),
    CONSTRAINT CK_Leads_Status CHECK (Status IN (N'New', N'Qualified', N'Unqualified', N'Converted', N'Lost')),
    CONSTRAINT CK_Leads_LeadScore CHECK (LeadScore BETWEEN 0 AND 100),
    CONSTRAINT CK_Leads_ContactName_NotBlank CHECK (LEN(LTRIM(RTRIM(ContactName))) > 0)
);
GO

CREATE TABLE sales.SalesOpportunities
(
    OpportunityId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SalesOpportunities PRIMARY KEY,
    LeadId INT NULL,
    CustomerId INT NOT NULL,
    DesiredModelId INT NULL,
    SalesStaffId INT NOT NULL,
    Stage NVARCHAR(40) NOT NULL CONSTRAINT DF_SalesOpportunities_Stage DEFAULT N'LeadManagement',
    ProbabilityPercent TINYINT NOT NULL CONSTRAINT DF_SalesOpportunities_Probability DEFAULT 10,
    ExpectedCloseDate DATE NULL,
    CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_SalesOpportunities_CreatedAt DEFAULT SYSUTCDATETIME(),
    ClosedAt DATETIME2(0) NULL,
    CONSTRAINT FK_SalesOpportunities_Leads FOREIGN KEY (LeadId) REFERENCES sales.Leads(LeadId),
    CONSTRAINT FK_SalesOpportunities_Customers FOREIGN KEY (CustomerId) REFERENCES core.Customers(CustomerId),
    CONSTRAINT FK_SalesOpportunities_Models FOREIGN KEY (DesiredModelId) REFERENCES inventory.VehicleModels(ModelId),
    CONSTRAINT FK_SalesOpportunities_SalesStaff FOREIGN KEY (SalesStaffId) REFERENCES core.StaffUsers(StaffId),
    CONSTRAINT CK_SalesOpportunities_Stage CHECK (Stage IN
    (
        N'LeadManagement',
        N'Consultation',
        N'TestDrive',
        N'Negotiation',
        N'Contract',
        N'Administration',
        N'Delivery',
        N'ClosedWon',
        N'ClosedLost'
    )),
    CONSTRAINT CK_SalesOpportunities_Probability CHECK (ProbabilityPercent BETWEEN 0 AND 100)
);
GO

CREATE TABLE sales.TestDrives
(
    TestDriveId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TestDrives PRIMARY KEY,
    OpportunityId INT NOT NULL,
    NewVehicleId INT NULL,
    ScheduledAt DATETIME2(0) NOT NULL,
    ActualStartAt DATETIME2(0) NULL,
    ActualEndAt DATETIME2(0) NULL,
    Status NVARCHAR(30) NOT NULL CONSTRAINT DF_TestDrives_Status DEFAULT N'Scheduled',
    CustomerFeedback NVARCHAR(1000) NULL,
    CONSTRAINT FK_TestDrives_Opportunities FOREIGN KEY (OpportunityId) REFERENCES sales.SalesOpportunities(OpportunityId),
    CONSTRAINT FK_TestDrives_NewVehicles FOREIGN KEY (NewVehicleId) REFERENCES inventory.NewVehicles(NewVehicleId),
    CONSTRAINT CK_TestDrives_Status CHECK (Status IN (N'Scheduled', N'Completed', N'Cancelled', N'NoShow')),
    CONSTRAINT CK_TestDrives_Time CHECK (ActualEndAt IS NULL OR ActualStartAt IS NULL OR ActualEndAt >= ActualStartAt)
);
GO

CREATE TABLE sales.SalesQuotes
(
    SalesQuoteId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SalesQuotes PRIMARY KEY,
    QuoteNo NVARCHAR(50) NOT NULL CONSTRAINT UQ_SalesQuotes_QuoteNo UNIQUE,
    OpportunityId INT NOT NULL,
    ModelId INT NOT NULL,
    NewVehicleId INT NULL,
    OnRoadPrice DECIMAL(18,2) NOT NULL,
    RegistrationFeeEstimate DECIMAL(18,2) NOT NULL CONSTRAINT DF_SalesQuotes_RegistrationFee DEFAULT 0,
    InsuranceEstimate DECIMAL(18,2) NOT NULL CONSTRAINT DF_SalesQuotes_Insurance DEFAULT 0,
    AccessoryPackageValue DECIMAL(18,2) NOT NULL CONSTRAINT DF_SalesQuotes_AccessoryValue DEFAULT 0,
    Status NVARCHAR(30) NOT NULL CONSTRAINT DF_SalesQuotes_Status DEFAULT N'Draft',
    ValidUntil DATE NOT NULL,
    CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_SalesQuotes_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_SalesQuotes_Opportunities FOREIGN KEY (OpportunityId) REFERENCES sales.SalesOpportunities(OpportunityId),
    CONSTRAINT FK_SalesQuotes_Models FOREIGN KEY (ModelId) REFERENCES inventory.VehicleModels(ModelId),
    CONSTRAINT FK_SalesQuotes_NewVehicles FOREIGN KEY (NewVehicleId) REFERENCES inventory.NewVehicles(NewVehicleId),
    CONSTRAINT CK_SalesQuotes_Amounts CHECK
    (
        OnRoadPrice >= 0
        AND RegistrationFeeEstimate >= 0
        AND InsuranceEstimate >= 0
        AND AccessoryPackageValue >= 0
    ),
    CONSTRAINT CK_SalesQuotes_Status CHECK (Status IN (N'Draft', N'Sent', N'Accepted', N'Expired', N'Cancelled'))
);
GO

CREATE TABLE sales.SalesContracts
(
    SalesContractId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SalesContracts PRIMARY KEY,
    ContractNo NVARCHAR(50) NOT NULL CONSTRAINT UQ_SalesContracts_ContractNo UNIQUE,
    OpportunityId INT NULL,
    CustomerId INT NOT NULL,
    NewVehicleId INT NOT NULL,
    SalesStaffId INT NOT NULL,
    CreatedByStaffId INT NOT NULL,
    BelowCostApprovalId INT NULL,
    PaymentMethod NVARCHAR(20) NOT NULL,
    Status NVARCHAR(30) NOT NULL CONSTRAINT DF_SalesContracts_Status DEFAULT N'Draft',
    FinalSalePrice DECIMAL(18,2) NOT NULL,
    DepositAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_SalesContracts_DepositAmount DEFAULT 0,
    DepositDueAt DATETIME2(0) NULL,
    SignedAt DATETIME2(0) NULL,
    CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_SalesContracts_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_SalesContracts_UpdatedAt DEFAULT SYSUTCDATETIME(),
    IsOpenForVinLock AS
    (
        CONVERT(bit, CASE
            WHEN Status IN (N'Draft', N'Signed', N'PendingPayment', N'Registration', N'ReadyForDelivery', N'Delivered') THEN 1
            ELSE 0
        END)
    ) PERSISTED,
    CONSTRAINT FK_SalesContracts_Opportunities FOREIGN KEY (OpportunityId) REFERENCES sales.SalesOpportunities(OpportunityId),
    CONSTRAINT FK_SalesContracts_Customers FOREIGN KEY (CustomerId) REFERENCES core.Customers(CustomerId),
    CONSTRAINT FK_SalesContracts_NewVehicles FOREIGN KEY (NewVehicleId) REFERENCES inventory.NewVehicles(NewVehicleId),
    CONSTRAINT FK_SalesContracts_SalesStaff FOREIGN KEY (SalesStaffId) REFERENCES core.StaffUsers(StaffId),
    CONSTRAINT FK_SalesContracts_CreatedBy FOREIGN KEY (CreatedByStaffId) REFERENCES core.StaffUsers(StaffId),
    CONSTRAINT FK_SalesContracts_BelowCostApprovals FOREIGN KEY (BelowCostApprovalId) REFERENCES finance.Approvals(ApprovalId),
    CONSTRAINT CK_SalesContracts_PaymentMethod CHECK (PaymentMethod IN (N'Cash', N'Installment')),
    CONSTRAINT CK_SalesContracts_Status CHECK (Status IN
    (
        N'Draft',
        N'Signed',
        N'PendingPayment',
        N'Registration',
        N'ReadyForDelivery',
        N'Delivered',
        N'Liquidated',
        N'Cancelled'
    )),
    CONSTRAINT CK_SalesContracts_Amounts CHECK (FinalSalePrice >= 0 AND DepositAmount >= 0),
    CONSTRAINT CK_SalesContracts_MinDeposit CHECK
    (
        Status = N'Draft'
        OR DepositAmount >= FinalSalePrice * 0.10
    )
);
GO

CREATE UNIQUE INDEX UX_SalesContracts_OneOpenContractPerVehicle
ON sales.SalesContracts(NewVehicleId)
WHERE Status <> N'Liquidated' AND Status <> N'Cancelled';
GO

CREATE TABLE sales.ContractAccessories
(
    ContractAccessoryId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ContractAccessories PRIMARY KEY,
    SalesContractId INT NOT NULL,
    AccessoryName NVARCHAR(150) NOT NULL,
    Quantity INT NOT NULL CONSTRAINT DF_ContractAccessories_Quantity DEFAULT 1,
    UnitPrice DECIMAL(18,2) NOT NULL CONSTRAINT DF_ContractAccessories_UnitPrice DEFAULT 0,
    UnitCost DECIMAL(18,2) NOT NULL CONSTRAINT DF_ContractAccessories_UnitCost DEFAULT 0,
    IsGift BIT NOT NULL CONSTRAINT DF_ContractAccessories_IsGift DEFAULT 0,
    CONSTRAINT FK_ContractAccessories_SalesContracts FOREIGN KEY (SalesContractId) REFERENCES sales.SalesContracts(SalesContractId),
    CONSTRAINT CK_ContractAccessories_Quantity CHECK (Quantity > 0),
    CONSTRAINT CK_ContractAccessories_Amounts CHECK (UnitPrice >= 0 AND UnitCost >= 0)
);
GO

CREATE TABLE sales.RegistrationTasks
(
    RegistrationTaskId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_RegistrationTasks PRIMARY KEY,
    SalesContractId INT NOT NULL,
    TaskType NVARCHAR(30) NOT NULL,
    Status NVARCHAR(30) NOT NULL CONSTRAINT DF_RegistrationTasks_Status DEFAULT N'Pending',
    GovernmentReceiptNo NVARCHAR(80) NULL,
    Amount DECIMAL(18,2) NOT NULL CONSTRAINT DF_RegistrationTasks_Amount DEFAULT 0,
    SubmittedAt DATETIME2(0) NULL,
    CompletedAt DATETIME2(0) NULL,
    HandledByStaffId INT NULL,
    CONSTRAINT FK_RegistrationTasks_SalesContracts FOREIGN KEY (SalesContractId) REFERENCES sales.SalesContracts(SalesContractId),
    CONSTRAINT FK_RegistrationTasks_HandledBy FOREIGN KEY (HandledByStaffId) REFERENCES core.StaffUsers(StaffId),
    CONSTRAINT CK_RegistrationTasks_Type CHECK (TaskType IN (N'RegistrationTax', N'PlateNumber', N'Inspection', N'Insurance', N'Other')),
    CONSTRAINT CK_RegistrationTasks_Status CHECK (Status IN (N'Pending', N'Submitted', N'Completed', N'Cancelled')),
    CONSTRAINT CK_RegistrationTasks_Amount CHECK (Amount >= 0)
);
GO

CREATE TABLE sales.VehicleDeliveries
(
    VehicleDeliveryId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_VehicleDeliveries PRIMARY KEY,
    SalesContractId INT NOT NULL CONSTRAINT UQ_VehicleDeliveries_SalesContractId UNIQUE,
    DeliveryStaffId INT NOT NULL,
    DeliveredAt DATETIME2(0) NULL,
    OdometerAtDelivery INT NOT NULL CONSTRAINT DF_VehicleDeliveries_Odometer DEFAULT 0,
    CustomerAccepted BIT NOT NULL CONSTRAINT DF_VehicleDeliveries_CustomerAccepted DEFAULT 0,
    DeliveryNotes NVARCHAR(1000) NULL,
    CONSTRAINT FK_VehicleDeliveries_SalesContracts FOREIGN KEY (SalesContractId) REFERENCES sales.SalesContracts(SalesContractId),
    CONSTRAINT FK_VehicleDeliveries_StaffUsers FOREIGN KEY (DeliveryStaffId) REFERENCES core.StaffUsers(StaffId),
    CONSTRAINT CK_VehicleDeliveries_Odometer CHECK (OdometerAtDelivery >= 0),
    CONSTRAINT CK_VehicleDeliveries_AcceptanceTime CHECK (CustomerAccepted = 0 OR DeliveredAt IS NOT NULL)
);
GO

CREATE TABLE sales.DeliveryChecklistItems
(
    DeliveryChecklistItemId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DeliveryChecklistItems PRIMARY KEY,
    VehicleDeliveryId INT NOT NULL,
    ChecklistName NVARCHAR(150) NOT NULL,
    IsCompleted BIT NOT NULL CONSTRAINT DF_DeliveryChecklistItems_IsCompleted DEFAULT 0,
    CompletedAt DATETIME2(0) NULL,
    CONSTRAINT FK_DeliveryChecklistItems_VehicleDeliveries FOREIGN KEY (VehicleDeliveryId) REFERENCES sales.VehicleDeliveries(VehicleDeliveryId),
    CONSTRAINT CK_DeliveryChecklistItems_Name_NotBlank CHECK (LEN(LTRIM(RTRIM(ChecklistName))) > 0),
    CONSTRAINT CK_DeliveryChecklistItems_CompletedAt CHECK (IsCompleted = 0 OR CompletedAt IS NOT NULL)
);
GO

ALTER TABLE inventory.NewVehicles
ADD CONSTRAINT FK_NewVehicles_AllocatedContracts
FOREIGN KEY (AllocatedContractId) REFERENCES sales.SalesContracts(SalesContractId);
GO

CREATE TABLE service.CustomerVehicles
(
    CustomerVehicleId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CustomerVehicles PRIMARY KEY,
    CustomerId INT NOT NULL,
    NewVehicleId INT NULL,
    Vin CHAR(17) NOT NULL CONSTRAINT UQ_CustomerVehicles_Vin UNIQUE,
    PlateNumber NVARCHAR(30) NULL,
    BrandName NVARCHAR(100) NOT NULL,
    ModelName NVARCHAR(150) NOT NULL,
    WarrantyStartDate DATE NULL,
    WarrantyEndDate DATE NULL,
    CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_CustomerVehicles_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_CustomerVehicles_Customers FOREIGN KEY (CustomerId) REFERENCES core.Customers(CustomerId),
    CONSTRAINT FK_CustomerVehicles_NewVehicles FOREIGN KEY (NewVehicleId) REFERENCES inventory.NewVehicles(NewVehicleId),
    CONSTRAINT CK_CustomerVehicles_Vin_Format CHECK
    (
        LEN(Vin) = 17
        AND Vin = UPPER(Vin)
        AND Vin NOT LIKE '%[IOQ]%'
        AND Vin NOT LIKE '%[^A-HJ-NPR-Z0-9]%'
    ),
    CONSTRAINT CK_CustomerVehicles_WarrantyDates CHECK (WarrantyEndDate IS NULL OR WarrantyStartDate IS NULL OR WarrantyEndDate >= WarrantyStartDate)
);
GO

CREATE UNIQUE INDEX UX_CustomerVehicles_NewVehicleId
ON service.CustomerVehicles(NewVehicleId)
WHERE NewVehicleId IS NOT NULL;

CREATE UNIQUE INDEX UX_CustomerVehicles_PlateNumber
ON service.CustomerVehicles(PlateNumber)
WHERE PlateNumber IS NOT NULL;
GO

CREATE TABLE service.ServiceBookings
(
    ServiceBookingId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ServiceBookings PRIMARY KEY,
    BookingNo NVARCHAR(50) NOT NULL CONSTRAINT UQ_ServiceBookings_BookingNo UNIQUE,
    CustomerId INT NOT NULL,
    CustomerVehicleId INT NOT NULL,
    RequestedAt DATETIME2(0) NOT NULL CONSTRAINT DF_ServiceBookings_RequestedAt DEFAULT SYSUTCDATETIME(),
    ScheduledAt DATETIME2(0) NOT NULL,
    ServiceType NVARCHAR(50) NOT NULL,
    Status NVARCHAR(30) NOT NULL CONSTRAINT DF_ServiceBookings_Status DEFAULT N'Booked',
    Notes NVARCHAR(1000) NULL,
    CONSTRAINT FK_ServiceBookings_Customers FOREIGN KEY (CustomerId) REFERENCES core.Customers(CustomerId),
    CONSTRAINT FK_ServiceBookings_CustomerVehicles FOREIGN KEY (CustomerVehicleId) REFERENCES service.CustomerVehicles(CustomerVehicleId),
    CONSTRAINT CK_ServiceBookings_ServiceType CHECK (ServiceType IN (N'Maintenance', N'Repair', N'Warranty', N'Inspection', N'Other')),
    CONSTRAINT CK_ServiceBookings_Status CHECK (Status IN (N'Booked', N'Confirmed', N'CheckedIn', N'Cancelled', N'NoShow', N'Completed'))
);
GO

CREATE TABLE service.Intakes
(
    IntakeId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Intakes PRIMARY KEY,
    ServiceBookingId INT NULL,
    CustomerVehicleId INT NOT NULL,
    ServiceAdvisorStaffId INT NOT NULL,
    IntakeAt DATETIME2(0) NOT NULL CONSTRAINT DF_Intakes_IntakeAt DEFAULT SYSUTCDATETIME(),
    Odometer INT NOT NULL,
    FuelLevelPercent TINYINT NULL,
    ExteriorConditionNote NVARCHAR(1000) NULL,
    CustomerComplaint NVARCHAR(1000) NULL,
    CustomerConfirmed BIT NOT NULL CONSTRAINT DF_Intakes_CustomerConfirmed DEFAULT 0,
    CONSTRAINT FK_Intakes_ServiceBookings FOREIGN KEY (ServiceBookingId) REFERENCES service.ServiceBookings(ServiceBookingId),
    CONSTRAINT FK_Intakes_CustomerVehicles FOREIGN KEY (CustomerVehicleId) REFERENCES service.CustomerVehicles(CustomerVehicleId),
    CONSTRAINT FK_Intakes_ServiceAdvisor FOREIGN KEY (ServiceAdvisorStaffId) REFERENCES core.StaffUsers(StaffId),
    CONSTRAINT CK_Intakes_Odometer CHECK (Odometer >= 0),
    CONSTRAINT CK_Intakes_Fuel CHECK (FuelLevelPercent IS NULL OR FuelLevelPercent BETWEEN 0 AND 100)
);
GO

CREATE TABLE service.IntakeMedia
(
    IntakeMediaId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_IntakeMedia PRIMARY KEY,
    IntakeId INT NOT NULL,
    MediaType NVARCHAR(20) NOT NULL,
    FileUrl NVARCHAR(1000) NOT NULL,
    CapturedAt DATETIME2(0) NOT NULL CONSTRAINT DF_IntakeMedia_CapturedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_IntakeMedia_Intakes FOREIGN KEY (IntakeId) REFERENCES service.Intakes(IntakeId),
    CONSTRAINT CK_IntakeMedia_Type CHECK (MediaType IN (N'Photo', N'Video')),
    CONSTRAINT CK_IntakeMedia_FileUrl_NotBlank CHECK (LEN(LTRIM(RTRIM(FileUrl))) > 0)
);
GO

CREATE TABLE service.WorkOrders
(
    WorkOrderId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_WorkOrders PRIMARY KEY,
    WorkOrderNo NVARCHAR(50) NOT NULL CONSTRAINT UQ_WorkOrders_WorkOrderNo UNIQUE,
    IntakeId INT NOT NULL,
    CustomerVehicleId INT NOT NULL,
    ServiceAdvisorStaffId INT NOT NULL,
    LeadTechnicianStaffId INT NULL,
    Status NVARCHAR(30) NOT NULL CONSTRAINT DF_WorkOrders_Status DEFAULT N'Open',
    TotalEstimate DECIMAL(18,2) NOT NULL CONSTRAINT DF_WorkOrders_TotalEstimate DEFAULT 0,
    CustomerApprovedAt DATETIME2(0) NULL,
    OpenedAt DATETIME2(0) NOT NULL CONSTRAINT DF_WorkOrders_OpenedAt DEFAULT SYSUTCDATETIME(),
    ClosedAt DATETIME2(0) NULL,
    IsOpenWorkOrder AS
    (
        CONVERT(bit, CASE
            WHEN Status IN (N'Open', N'InProgress', N'WaitingParts', N'QC') THEN 1
            ELSE 0
        END)
    ) PERSISTED,
    CONSTRAINT FK_WorkOrders_Intakes FOREIGN KEY (IntakeId) REFERENCES service.Intakes(IntakeId),
    CONSTRAINT FK_WorkOrders_CustomerVehicles FOREIGN KEY (CustomerVehicleId) REFERENCES service.CustomerVehicles(CustomerVehicleId),
    CONSTRAINT FK_WorkOrders_ServiceAdvisor FOREIGN KEY (ServiceAdvisorStaffId) REFERENCES core.StaffUsers(StaffId),
    CONSTRAINT FK_WorkOrders_LeadTechnician FOREIGN KEY (LeadTechnicianStaffId) REFERENCES core.StaffUsers(StaffId),
    CONSTRAINT CK_WorkOrders_Status CHECK (Status IN (N'Open', N'InProgress', N'WaitingParts', N'QC', N'Completed', N'Closed', N'Cancelled')),
    CONSTRAINT CK_WorkOrders_TotalEstimate CHECK (TotalEstimate >= 0),
    CONSTRAINT CK_WorkOrders_CloseTime CHECK (Status <> N'Closed' OR ClosedAt IS NOT NULL)
);
GO

CREATE UNIQUE INDEX UX_WorkOrders_OneOpenWorkOrderPerVehicle
ON service.WorkOrders(CustomerVehicleId)
WHERE Status <> N'Completed' AND Status <> N'Closed' AND Status <> N'Cancelled';
GO

CREATE TABLE service.StandardLaborOperations
(
    StandardLaborOperationId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_StandardLaborOperations PRIMARY KEY,
    OperationCode NVARCHAR(50) NOT NULL CONSTRAINT UQ_StandardLaborOperations_Code UNIQUE,
    OperationName NVARCHAR(150) NOT NULL,
    StandardHours DECIMAL(8,2) NOT NULL,
    MaxVariancePercent DECIMAL(8,2) NOT NULL CONSTRAINT DF_StandardLaborOperations_MaxVariance DEFAULT 20,
    IsActive BIT NOT NULL CONSTRAINT DF_StandardLaborOperations_IsActive DEFAULT 1,
    CONSTRAINT CK_StandardLaborOperations_StandardHours CHECK (StandardHours > 0),
    CONSTRAINT CK_StandardLaborOperations_MaxVariance CHECK (MaxVariancePercent >= 0)
);
GO

CREATE TABLE service.WorkOrderJobs
(
    WorkOrderJobId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_WorkOrderJobs PRIMARY KEY,
    WorkOrderId INT NOT NULL,
    StandardLaborOperationId INT NOT NULL,
    AssignedTechnicianStaffId INT NOT NULL,
    LaborOverrunApprovalId INT NULL,
    ActualLaborHours DECIMAL(8,2) NOT NULL CONSTRAINT DF_WorkOrderJobs_ActualLaborHours DEFAULT 0,
    Status NVARCHAR(30) NOT NULL CONSTRAINT DF_WorkOrderJobs_Status DEFAULT N'Assigned',
    StartedAt DATETIME2(0) NULL,
    CompletedAt DATETIME2(0) NULL,
    CONSTRAINT FK_WorkOrderJobs_WorkOrders FOREIGN KEY (WorkOrderId) REFERENCES service.WorkOrders(WorkOrderId),
    CONSTRAINT FK_WorkOrderJobs_StandardLabor FOREIGN KEY (StandardLaborOperationId) REFERENCES service.StandardLaborOperations(StandardLaborOperationId),
    CONSTRAINT FK_WorkOrderJobs_Technicians FOREIGN KEY (AssignedTechnicianStaffId) REFERENCES core.StaffUsers(StaffId),
    CONSTRAINT FK_WorkOrderJobs_LaborApprovals FOREIGN KEY (LaborOverrunApprovalId) REFERENCES finance.Approvals(ApprovalId),
    CONSTRAINT CK_WorkOrderJobs_ActualHours CHECK (ActualLaborHours >= 0),
    CONSTRAINT CK_WorkOrderJobs_Status CHECK (Status IN (N'Assigned', N'InProgress', N'Completed', N'Cancelled')),
    CONSTRAINT CK_WorkOrderJobs_CompleteTime CHECK (Status <> N'Completed' OR CompletedAt IS NOT NULL)
);
GO

CREATE TABLE service.WorkOrderParts
(
    WorkOrderPartId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_WorkOrderParts PRIMARY KEY,
    WorkOrderId INT NOT NULL,
    PartId INT NOT NULL,
    RequestedQuantity DECIMAL(18,2) NOT NULL,
    IssuedQuantity DECIMAL(18,2) NOT NULL CONSTRAINT DF_WorkOrderParts_IssuedQuantity DEFAULT 0,
    UnitPrice DECIMAL(18,2) NOT NULL,
    Status NVARCHAR(30) NOT NULL CONSTRAINT DF_WorkOrderParts_Status DEFAULT N'Requested',
    CONSTRAINT FK_WorkOrderParts_WorkOrders FOREIGN KEY (WorkOrderId) REFERENCES service.WorkOrders(WorkOrderId),
    CONSTRAINT FK_WorkOrderParts_Parts FOREIGN KEY (PartId) REFERENCES inventory.Parts(PartId),
    CONSTRAINT CK_WorkOrderParts_RequestedQuantity CHECK (RequestedQuantity > 0),
    CONSTRAINT CK_WorkOrderParts_IssuedQuantity CHECK (IssuedQuantity >= 0),
    CONSTRAINT CK_WorkOrderParts_QuantityLimit CHECK (IssuedQuantity <= RequestedQuantity),
    CONSTRAINT CK_WorkOrderParts_UnitPrice CHECK (UnitPrice >= 0),
    CONSTRAINT CK_WorkOrderParts_Status CHECK (Status IN (N'Requested', N'PartiallyIssued', N'Issued', N'Cancelled'))
);
GO

CREATE TABLE service.PartIssues
(
    PartIssueId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PartIssues PRIMARY KEY,
    WorkOrderPartId INT NOT NULL,
    LocationId INT NOT NULL,
    Quantity DECIMAL(18,2) NOT NULL,
    IssuedByStaffId INT NOT NULL,
    IssuedAt DATETIME2(0) NOT NULL CONSTRAINT DF_PartIssues_IssuedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_PartIssues_WorkOrderParts FOREIGN KEY (WorkOrderPartId) REFERENCES service.WorkOrderParts(WorkOrderPartId),
    CONSTRAINT FK_PartIssues_Locations FOREIGN KEY (LocationId) REFERENCES inventory.Locations(LocationId),
    CONSTRAINT FK_PartIssues_StaffUsers FOREIGN KEY (IssuedByStaffId) REFERENCES core.StaffUsers(StaffId),
    CONSTRAINT CK_PartIssues_Quantity CHECK (Quantity > 0)
);
GO

CREATE TABLE service.QualityChecks
(
    QualityCheckId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_QualityChecks PRIMARY KEY,
    WorkOrderId INT NOT NULL CONSTRAINT UQ_QualityChecks_WorkOrderId UNIQUE,
    CheckedByStaffId INT NOT NULL,
    CheckedAt DATETIME2(0) NOT NULL CONSTRAINT DF_QualityChecks_CheckedAt DEFAULT SYSUTCDATETIME(),
    FinalCheckPassed BIT NOT NULL,
    WashCompleted BIT NOT NULL CONSTRAINT DF_QualityChecks_WashCompleted DEFAULT 0,
    Notes NVARCHAR(1000) NULL,
    CONSTRAINT FK_QualityChecks_WorkOrders FOREIGN KEY (WorkOrderId) REFERENCES service.WorkOrders(WorkOrderId),
    CONSTRAINT FK_QualityChecks_CheckedBy FOREIGN KEY (CheckedByStaffId) REFERENCES core.StaffUsers(StaffId)
);
GO

CREATE TABLE service.ServiceNotifications
(
    ServiceNotificationId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ServiceNotifications PRIMARY KEY,
    WorkOrderId INT NOT NULL,
    Channel NVARCHAR(20) NOT NULL,
    Recipient NVARCHAR(255) NOT NULL,
    MessageText NVARCHAR(1000) NOT NULL,
    SentAt DATETIME2(0) NOT NULL CONSTRAINT DF_ServiceNotifications_SentAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_ServiceNotifications_WorkOrders FOREIGN KEY (WorkOrderId) REFERENCES service.WorkOrders(WorkOrderId),
    CONSTRAINT CK_ServiceNotifications_Channel CHECK (Channel IN (N'Zalo', N'SMS', N'Email', N'Phone')),
    CONSTRAINT CK_ServiceNotifications_Recipient_NotBlank CHECK (LEN(LTRIM(RTRIM(Recipient))) > 0)
);
GO

CREATE TABLE finance.SalesInvoices
(
    SalesInvoiceId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SalesInvoices PRIMARY KEY,
    InvoiceNo NVARCHAR(50) NOT NULL CONSTRAINT UQ_SalesInvoices_InvoiceNo UNIQUE,
    ElectronicInvoiceNo NVARCHAR(80) NULL,
    SalesContractId INT NOT NULL CONSTRAINT UQ_SalesInvoices_SalesContractId UNIQUE,
    CustomerId INT NOT NULL,
    SubtotalAmount DECIMAL(18,2) NOT NULL,
    VatAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_SalesInvoices_VatAmount DEFAULT 0,
    TotalAmount AS (SubtotalAmount + VatAmount) PERSISTED,
    Status NVARCHAR(30) NOT NULL CONSTRAINT DF_SalesInvoices_Status DEFAULT N'Draft',
    IssuedAt DATETIME2(0) NULL,
    CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_SalesInvoices_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_SalesInvoices_SalesContracts FOREIGN KEY (SalesContractId) REFERENCES sales.SalesContracts(SalesContractId),
    CONSTRAINT FK_SalesInvoices_Customers FOREIGN KEY (CustomerId) REFERENCES core.Customers(CustomerId),
    CONSTRAINT CK_SalesInvoices_Amounts CHECK (SubtotalAmount >= 0 AND VatAmount >= 0),
    CONSTRAINT CK_SalesInvoices_Status CHECK (Status IN (N'Draft', N'Issued', N'Paid', N'Cancelled')),
    CONSTRAINT CK_SalesInvoices_IssuedFields CHECK
    (
        (Status IN (N'Issued', N'Paid') AND IssuedAt IS NOT NULL AND ElectronicInvoiceNo IS NOT NULL)
        OR
        (Status IN (N'Draft', N'Cancelled'))
    )
);
GO

CREATE UNIQUE INDEX UX_SalesInvoices_ElectronicInvoiceNo
ON finance.SalesInvoices(ElectronicInvoiceNo)
WHERE ElectronicInvoiceNo IS NOT NULL;
GO

CREATE TABLE finance.ServiceInvoices
(
    ServiceInvoiceId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ServiceInvoices PRIMARY KEY,
    InvoiceNo NVARCHAR(50) NOT NULL CONSTRAINT UQ_ServiceInvoices_InvoiceNo UNIQUE,
    ElectronicInvoiceNo NVARCHAR(80) NULL,
    WorkOrderId INT NOT NULL CONSTRAINT UQ_ServiceInvoices_WorkOrderId UNIQUE,
    CustomerId INT NOT NULL,
    SubtotalAmount DECIMAL(18,2) NOT NULL,
    VatAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_ServiceInvoices_VatAmount DEFAULT 0,
    TotalAmount AS (SubtotalAmount + VatAmount) PERSISTED,
    Status NVARCHAR(30) NOT NULL CONSTRAINT DF_ServiceInvoices_Status DEFAULT N'Draft',
    IssuedAt DATETIME2(0) NULL,
    CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_ServiceInvoices_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_ServiceInvoices_WorkOrders FOREIGN KEY (WorkOrderId) REFERENCES service.WorkOrders(WorkOrderId),
    CONSTRAINT FK_ServiceInvoices_Customers FOREIGN KEY (CustomerId) REFERENCES core.Customers(CustomerId),
    CONSTRAINT CK_ServiceInvoices_Amounts CHECK (SubtotalAmount >= 0 AND VatAmount >= 0),
    CONSTRAINT CK_ServiceInvoices_Status CHECK (Status IN (N'Draft', N'Issued', N'Paid', N'Cancelled')),
    CONSTRAINT CK_ServiceInvoices_IssuedFields CHECK
    (
        (Status IN (N'Issued', N'Paid') AND IssuedAt IS NOT NULL AND ElectronicInvoiceNo IS NOT NULL)
        OR
        (Status IN (N'Draft', N'Cancelled'))
    )
);
GO

CREATE UNIQUE INDEX UX_ServiceInvoices_ElectronicInvoiceNo
ON finance.ServiceInvoices(ElectronicInvoiceNo)
WHERE ElectronicInvoiceNo IS NOT NULL;
GO

CREATE TABLE finance.Payments
(
    PaymentId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Payments PRIMARY KEY,
    PaymentNo NVARCHAR(50) NOT NULL CONSTRAINT UQ_Payments_PaymentNo UNIQUE,
    CustomerId INT NOT NULL,
    SalesContractId INT NULL,
    WorkOrderId INT NULL,
    PaymentPurpose NVARCHAR(30) NOT NULL,
    PaymentSource NVARCHAR(30) NOT NULL,
    Amount DECIMAL(18,2) NOT NULL,
    PaidAt DATETIME2(0) NOT NULL CONSTRAINT DF_Payments_PaidAt DEFAULT SYSUTCDATETIME(),
    RecordedByStaffId INT NOT NULL,
    ReconciledAt DATETIME2(0) NULL,
    CONSTRAINT FK_Payments_Customers FOREIGN KEY (CustomerId) REFERENCES core.Customers(CustomerId),
    CONSTRAINT FK_Payments_SalesContracts FOREIGN KEY (SalesContractId) REFERENCES sales.SalesContracts(SalesContractId),
    CONSTRAINT FK_Payments_WorkOrders FOREIGN KEY (WorkOrderId) REFERENCES service.WorkOrders(WorkOrderId),
    CONSTRAINT FK_Payments_RecordedBy FOREIGN KEY (RecordedByStaffId) REFERENCES core.StaffUsers(StaffId),
    CONSTRAINT CK_Payments_Target CHECK
    (
        (SalesContractId IS NOT NULL AND WorkOrderId IS NULL)
        OR
        (SalesContractId IS NULL AND WorkOrderId IS NOT NULL)
    ),
    CONSTRAINT CK_Payments_Purpose CHECK (PaymentPurpose IN (N'Deposit', N'FinalPayment', N'BankDisbursement', N'ServicePayment', N'Refund')),
    CONSTRAINT CK_Payments_Source CHECK (PaymentSource IN (N'Cash', N'BankTransfer', N'Card', N'BankDisbursement')),
    CONSTRAINT CK_Payments_Amount CHECK (Amount > 0)
);
GO

CREATE TABLE finance.FinancialAdjustments
(
    FinancialAdjustmentId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_FinancialAdjustments PRIMARY KEY,
    SalesContractId INT NULL,
    WorkOrderId INT NULL,
    ApprovalId INT NOT NULL,
    Amount DECIMAL(18,2) NOT NULL,
    Reason NVARCHAR(500) NOT NULL,
    CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_FinancialAdjustments_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_FinancialAdjustments_SalesContracts FOREIGN KEY (SalesContractId) REFERENCES sales.SalesContracts(SalesContractId),
    CONSTRAINT FK_FinancialAdjustments_WorkOrders FOREIGN KEY (WorkOrderId) REFERENCES service.WorkOrders(WorkOrderId),
    CONSTRAINT FK_FinancialAdjustments_Approvals FOREIGN KEY (ApprovalId) REFERENCES finance.Approvals(ApprovalId),
    CONSTRAINT CK_FinancialAdjustments_Target CHECK
    (
        (SalesContractId IS NOT NULL AND WorkOrderId IS NULL)
        OR
        (SalesContractId IS NULL AND WorkOrderId IS NOT NULL)
    ),
    CONSTRAINT CK_FinancialAdjustments_Amount CHECK (Amount >= 0)
);
GO

CREATE TABLE finance.Reconciliations
(
    ReconciliationId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Reconciliations PRIMARY KEY,
    PaymentId INT NOT NULL CONSTRAINT UQ_Reconciliations_PaymentId UNIQUE,
    BankReferenceNo NVARCHAR(100) NULL,
    SystemRevenueAmount DECIMAL(18,2) NOT NULL,
    ActualReceivedAmount DECIMAL(18,2) NOT NULL,
    DifferenceAmount AS (ActualReceivedAmount - SystemRevenueAmount) PERSISTED,
    ReconciledByStaffId INT NOT NULL,
    ReconciledAt DATETIME2(0) NOT NULL CONSTRAINT DF_Reconciliations_ReconciledAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Reconciliations_Payments FOREIGN KEY (PaymentId) REFERENCES finance.Payments(PaymentId),
    CONSTRAINT FK_Reconciliations_StaffUsers FOREIGN KEY (ReconciledByStaffId) REFERENCES core.StaffUsers(StaffId),
    CONSTRAINT CK_Reconciliations_Amounts CHECK (SystemRevenueAmount >= 0 AND ActualReceivedAmount >= 0)
);
GO

CREATE TABLE finance.CommissionRules
(
    CommissionRuleId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CommissionRules PRIMARY KEY,
    RoleCode NVARCHAR(50) NOT NULL,
    ProcessType NVARCHAR(30) NOT NULL,
    RatePercent DECIMAL(8,4) NOT NULL CONSTRAINT DF_CommissionRules_RatePercent DEFAULT 0,
    FixedAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_CommissionRules_FixedAmount DEFAULT 0,
    EffectiveFrom DATE NOT NULL,
    EffectiveTo DATE NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_CommissionRules_IsActive DEFAULT 1,
    CONSTRAINT FK_CommissionRules_Roles FOREIGN KEY (RoleCode) REFERENCES core.Roles(RoleCode),
    CONSTRAINT CK_CommissionRules_ProcessType CHECK (ProcessType IN (N'Sales', N'Service')),
    CONSTRAINT CK_CommissionRules_Amounts CHECK (RatePercent >= 0 AND FixedAmount >= 0),
    CONSTRAINT CK_CommissionRules_Dates CHECK (EffectiveTo IS NULL OR EffectiveTo >= EffectiveFrom)
);
GO

CREATE TABLE finance.StaffCommissionLedger
(
    StaffCommissionLedgerId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_StaffCommissionLedger PRIMARY KEY,
    StaffId INT NOT NULL,
    SalesContractId INT NULL,
    WorkOrderId INT NULL,
    CommissionAmount DECIMAL(18,2) NOT NULL,
    Status NVARCHAR(30) NOT NULL CONSTRAINT DF_StaffCommissionLedger_Status DEFAULT N'Calculated',
    CalculatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_StaffCommissionLedger_CalculatedAt DEFAULT SYSUTCDATETIME(),
    PaidAt DATETIME2(0) NULL,
    CONSTRAINT FK_StaffCommissionLedger_StaffUsers FOREIGN KEY (StaffId) REFERENCES core.StaffUsers(StaffId),
    CONSTRAINT FK_StaffCommissionLedger_SalesContracts FOREIGN KEY (SalesContractId) REFERENCES sales.SalesContracts(SalesContractId),
    CONSTRAINT FK_StaffCommissionLedger_WorkOrders FOREIGN KEY (WorkOrderId) REFERENCES service.WorkOrders(WorkOrderId),
    CONSTRAINT CK_StaffCommissionLedger_Target CHECK
    (
        (SalesContractId IS NOT NULL AND WorkOrderId IS NULL)
        OR
        (SalesContractId IS NULL AND WorkOrderId IS NOT NULL)
    ),
    CONSTRAINT CK_StaffCommissionLedger_Amount CHECK (CommissionAmount >= 0),
    CONSTRAINT CK_StaffCommissionLedger_Status CHECK (Status IN (N'Calculated', N'Approved', N'Paid', N'Cancelled'))
);
GO

CREATE TABLE finance.TechnicianKpiBonuses
(
    TechnicianKpiBonusId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TechnicianKpiBonuses PRIMARY KEY,
    WorkOrderId INT NOT NULL,
    TechnicianStaffId INT NOT NULL,
    KpiScore DECIMAL(8,2) NOT NULL,
    BonusAmount DECIMAL(18,2) NOT NULL,
    CalculatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_TechnicianKpiBonuses_CalculatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_TechnicianKpiBonuses_WorkOrders FOREIGN KEY (WorkOrderId) REFERENCES service.WorkOrders(WorkOrderId),
    CONSTRAINT FK_TechnicianKpiBonuses_StaffUsers FOREIGN KEY (TechnicianStaffId) REFERENCES core.StaffUsers(StaffId),
    CONSTRAINT CK_TechnicianKpiBonuses_KpiScore CHECK (KpiScore BETWEEN 0 AND 100),
    CONSTRAINT CK_TechnicianKpiBonuses_BonusAmount CHECK (BonusAmount >= 0)
);
GO

CREATE TRIGGER sales.TR_SalesContracts_AccountingOnlyPriceChange
ON sales.SalesContracts
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF UPDATE(FinalSalePrice)
       AND EXISTS
       (
            SELECT 1
            FROM inserted i
            INNER JOIN deleted d ON d.SalesContractId = i.SalesContractId
            WHERE i.FinalSalePrice <> d.FinalSalePrice
       )
    BEGIN
        DECLARE @ActorStaffId INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'actor_staff_id'));

        IF @ActorStaffId IS NULL
           OR NOT EXISTS
           (
                SELECT 1
                FROM core.StaffUserRoles sur
                WHERE sur.StaffId = @ActorStaffId
                  AND sur.RoleCode = N'Accountant'
           )
        BEGIN
            THROW 51001, 'Only staff with Accountant role can adjust FinalSalePrice after contract draft creation.', 1;
        END;
    END;
END;
GO

CREATE TRIGGER sales.TR_SalesContracts_ValidatePriceAndLock
ON sales.SalesContracts
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS
    (
        SELECT 1
        FROM inserted i
        INNER JOIN inventory.NewVehicles nv ON nv.NewVehicleId = i.NewVehicleId
        LEFT JOIN finance.Approvals a ON a.ApprovalId = i.BelowCostApprovalId
        LEFT JOIN core.StaffUserRoles approverRole
            ON approverRole.StaffId = a.ApprovedByStaffId
           AND approverRole.RoleCode = N'SalesManager'
        WHERE i.FinalSalePrice < nv.CostPrice
          AND
          (
                a.ApprovalId IS NULL
                OR a.ApprovalType <> N'BelowCostSale'
                OR a.Status <> N'Approved'
                OR approverRole.StaffId IS NULL
          )
    )
    BEGIN
        THROW 51002, 'FinalSalePrice cannot be below vehicle cost without an approved BelowCostSale approval from SalesManager.', 1;
    END;

    UPDATE nv
       SET nv.Status =
            CASE
                WHEN i.Status = N'ReadyForDelivery' THEN N'PendingDelivery'
                ELSE N'Allocated_Locked'
            END,
           nv.AllocatedContractId = i.SalesContractId
    FROM inventory.NewVehicles nv
    INNER JOIN inserted i ON i.NewVehicleId = nv.NewVehicleId
    WHERE i.IsOpenForVinLock = 1
      AND (i.DepositAmount > 0 OR i.Status <> N'Draft');

    UPDATE nv
       SET nv.Status = N'Ready',
           nv.AllocatedContractId = NULL
    FROM inventory.NewVehicles nv
    INNER JOIN inserted i ON i.SalesContractId = nv.AllocatedContractId
    WHERE i.IsOpenForVinLock = 0
      AND nv.Status <> N'Delivered';
END;
GO

CREATE TRIGGER inventory.TR_VehicleLocationMovements_ValidateTransfer
ON inventory.VehicleLocationMovements
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS
    (
        SELECT 1
        FROM inserted i
        INNER JOIN inventory.Locations fromLoc ON fromLoc.LocationId = i.FromLocationId
        INNER JOIN inventory.Locations toLoc ON toLoc.LocationId = i.ToLocationId
        INNER JOIN inventory.NewVehicles nv ON nv.NewVehicleId = i.NewVehicleId
        WHERE fromLoc.LocationType = N'Storage'
          AND toLoc.LocationType = N'Showroom'
          AND nv.Status <> N'Ready'
    )
    BEGIN
        THROW 51003, 'Vehicle can move from storage to showroom only when vehicle status is Ready.', 1;
    END;

    UPDATE nv
       SET nv.CurrentLocationId = i.ToLocationId,
           nv.Status =
                CASE
                    WHEN toLoc.LocationType = N'Showroom' AND nv.Status = N'Ready' THEN N'OnDisplay'
                    ELSE nv.Status
                END
    FROM inventory.NewVehicles nv
    INNER JOIN inserted i ON i.NewVehicleId = nv.NewVehicleId
    INNER JOIN inventory.Locations toLoc ON toLoc.LocationId = i.ToLocationId;
END;
GO

CREATE TRIGGER finance.TR_SalesInvoices_ValidateVehicleAndCustomer
ON finance.SalesInvoices
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS
    (
        SELECT 1
        FROM inserted i
        INNER JOIN sales.SalesContracts c ON c.SalesContractId = i.SalesContractId
        WHERE i.CustomerId <> c.CustomerId
    )
    BEGIN
        THROW 51004, 'Sales invoice customer must match the sales contract customer.', 1;
    END;

    IF EXISTS
    (
        SELECT 1
        FROM inserted i
        INNER JOIN sales.SalesContracts c ON c.SalesContractId = i.SalesContractId
        INNER JOIN inventory.NewVehicles nv ON nv.NewVehicleId = c.NewVehicleId
        WHERE nv.AllocatedContractId IS NOT NULL
          AND nv.AllocatedContractId <> c.SalesContractId
    )
    BEGIN
        THROW 51005, 'Vehicle is locked for another contract and cannot be invoiced here.', 1;
    END;
END;
GO

CREATE TRIGGER finance.TR_SalesInvoices_PreventIssuedTimeChange
ON finance.SalesInvoices
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS
    (
        SELECT 1
        FROM inserted i
        INNER JOIN deleted d ON d.SalesInvoiceId = i.SalesInvoiceId
        WHERE d.Status IN (N'Issued', N'Paid')
          AND
          (
                ISNULL(CONVERT(NVARCHAR(30), i.IssuedAt, 126), N'') <> ISNULL(CONVERT(NVARCHAR(30), d.IssuedAt, 126), N'')
                OR ISNULL(i.ElectronicInvoiceNo, N'') <> ISNULL(d.ElectronicInvoiceNo, N'')
          )
    )
    BEGIN
        THROW 51006, 'Issued sales invoice timestamp and electronic invoice number cannot be changed.', 1;
    END;
END;
GO

CREATE TRIGGER finance.TR_ServiceInvoices_PreventIssuedTimeChange
ON finance.ServiceInvoices
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS
    (
        SELECT 1
        FROM inserted i
        INNER JOIN deleted d ON d.ServiceInvoiceId = i.ServiceInvoiceId
        WHERE d.Status IN (N'Issued', N'Paid')
          AND
          (
                ISNULL(CONVERT(NVARCHAR(30), i.IssuedAt, 126), N'') <> ISNULL(CONVERT(NVARCHAR(30), d.IssuedAt, 126), N'')
                OR ISNULL(i.ElectronicInvoiceNo, N'') <> ISNULL(d.ElectronicInvoiceNo, N'')
          )
    )
    BEGIN
        THROW 51007, 'Issued service invoice timestamp and electronic invoice number cannot be changed.', 1;
    END;
END;
GO

CREATE TRIGGER finance.TR_Payments_PreventPaidAtUpdate
ON finance.Payments
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF UPDATE(PaidAt)
       AND EXISTS
       (
            SELECT 1
            FROM inserted i
            INNER JOIN deleted d ON d.PaymentId = i.PaymentId
            WHERE i.PaidAt <> d.PaidAt
       )
    BEGIN
        THROW 51008, 'Payment time cannot be modified after the transaction is recorded.', 1;
    END;
END;
GO

CREATE TRIGGER sales.TR_SalesContracts_BlockUnderpaidSettlement
ON sales.SalesContracts
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF UPDATE(Status)
       AND EXISTS
       (
            SELECT 1
            FROM inserted i
            OUTER APPLY
            (
                SELECT SUM(si.TotalAmount) AS InvoiceTotal
                FROM finance.SalesInvoices si
                WHERE si.SalesContractId = i.SalesContractId
                  AND si.Status IN (N'Issued', N'Paid')
            ) invoices
            OUTER APPLY
            (
                SELECT SUM(p.Amount) AS PaymentTotal
                FROM finance.Payments p
                WHERE p.SalesContractId = i.SalesContractId
            ) payments
            OUTER APPLY
            (
                SELECT SUM(fa.Amount) AS AdjustmentTotal
                FROM finance.FinancialAdjustments fa
                INNER JOIN finance.Approvals a ON a.ApprovalId = fa.ApprovalId
                WHERE fa.SalesContractId = i.SalesContractId
                  AND a.ApprovalType = N'PaymentShortfall'
                  AND a.Status = N'Approved'
            ) adjustments
            WHERE i.Status = N'Liquidated'
              AND ISNULL(invoices.InvoiceTotal, 0) > ISNULL(payments.PaymentTotal, 0) + ISNULL(adjustments.AdjustmentTotal, 0)
       )
    BEGIN
        THROW 51009, 'Cannot liquidate sales contract because actual payments are lower than issued invoices without an approved adjustment.', 1;
    END;
END;
GO

CREATE TRIGGER sales.TR_VehicleDeliveries_SetDelivered
ON sales.VehicleDeliveries
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE nv
       SET nv.Status = N'Delivered'
    FROM inventory.NewVehicles nv
    INNER JOIN sales.SalesContracts c ON c.NewVehicleId = nv.NewVehicleId
    INNER JOIN inserted i ON i.SalesContractId = c.SalesContractId
    WHERE i.CustomerAccepted = 1;

    UPDATE c
       SET c.Status = N'Delivered',
           c.UpdatedAt = SYSUTCDATETIME()
    FROM sales.SalesContracts c
    INNER JOIN inserted i ON i.SalesContractId = c.SalesContractId
    WHERE i.CustomerAccepted = 1
      AND c.Status <> N'Liquidated';
END;
GO

CREATE TRIGGER service.TR_WorkOrders_RequirePartsBeforeCompletion
ON service.WorkOrders
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF UPDATE(Status)
       AND EXISTS
       (
            SELECT 1
            FROM inserted i
            INNER JOIN service.WorkOrderParts wop ON wop.WorkOrderId = i.WorkOrderId
            WHERE i.Status IN (N'QC', N'Completed', N'Closed')
              AND wop.Status <> N'Cancelled'
              AND wop.IssuedQuantity < wop.RequestedQuantity
       )
    BEGIN
        THROW 51010, 'Cannot complete or close work order before all requested parts have been issued.', 1;
    END;
END;
GO

CREATE TRIGGER service.TR_WorkOrders_BlockUnderpaidClose
ON service.WorkOrders
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF UPDATE(Status)
       AND EXISTS
       (
            SELECT 1
            FROM inserted i
            OUTER APPLY
            (
                SELECT SUM(si.TotalAmount) AS InvoiceTotal
                FROM finance.ServiceInvoices si
                WHERE si.WorkOrderId = i.WorkOrderId
                  AND si.Status IN (N'Issued', N'Paid')
            ) invoices
            OUTER APPLY
            (
                SELECT SUM(p.Amount) AS PaymentTotal
                FROM finance.Payments p
                WHERE p.WorkOrderId = i.WorkOrderId
            ) payments
            OUTER APPLY
            (
                SELECT SUM(fa.Amount) AS AdjustmentTotal
                FROM finance.FinancialAdjustments fa
                INNER JOIN finance.Approvals a ON a.ApprovalId = fa.ApprovalId
                WHERE fa.WorkOrderId = i.WorkOrderId
                  AND a.ApprovalType = N'PaymentShortfall'
                  AND a.Status = N'Approved'
            ) adjustments
            WHERE i.Status = N'Closed'
              AND ISNULL(invoices.InvoiceTotal, 0) > ISNULL(payments.PaymentTotal, 0) + ISNULL(adjustments.AdjustmentTotal, 0)
       )
    BEGIN
        THROW 51011, 'Cannot close work order because actual payments are lower than issued invoices without an approved adjustment.', 1;
    END;
END;
GO

CREATE TRIGGER service.TR_WorkOrderJobs_ValidateLaborLimit
ON service.WorkOrderJobs
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS
    (
        SELECT 1
        FROM inserted i
        INNER JOIN service.StandardLaborOperations slo
            ON slo.StandardLaborOperationId = i.StandardLaborOperationId
        LEFT JOIN finance.Approvals a
            ON a.ApprovalId = i.LaborOverrunApprovalId
        WHERE i.ActualLaborHours > slo.StandardHours * (1 + slo.MaxVariancePercent / 100.0)
          AND
          (
                a.ApprovalId IS NULL
                OR a.ApprovalType <> N'LaborOverrun'
                OR a.Status <> N'Approved'
          )
    )
    BEGIN
        THROW 51012, 'Actual labor hours exceed allowed standard labor variance without approved LaborOverrun approval.', 1;
    END;
END;
GO

CREATE TRIGGER service.TR_PartIssues_DecrementStock
ON service.PartIssues
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS
    (
        SELECT 1
        FROM
        (
            SELECT
                i.LocationId,
                wop.PartId,
                wop.WorkOrderPartId,
                wop.WorkOrderId,
                SUM(i.Quantity) AS IssueQuantity,
                MAX(i.IssuedByStaffId) AS IssuedByStaffId
            FROM inserted i
            INNER JOIN service.WorkOrderParts wop ON wop.WorkOrderPartId = i.WorkOrderPartId
            GROUP BY i.LocationId, wop.PartId, wop.WorkOrderPartId, wop.WorkOrderId
        ) a
        INNER JOIN service.WorkOrderParts wop ON wop.WorkOrderPartId = a.WorkOrderPartId
        WHERE wop.IssuedQuantity + a.IssueQuantity > wop.RequestedQuantity
    )
    BEGIN
        THROW 51013, 'Issued part quantity cannot exceed the requested quantity on the work order.', 1;
    END;

    IF EXISTS
    (
        SELECT 1
        FROM
        (
            SELECT
                i.LocationId,
                wop.PartId,
                SUM(i.Quantity) AS IssueQuantity
            FROM inserted i
            INNER JOIN service.WorkOrderParts wop ON wop.WorkOrderPartId = i.WorkOrderPartId
            GROUP BY i.LocationId, wop.PartId
        ) a
        LEFT JOIN inventory.PartInventory pi
            ON pi.LocationId = a.LocationId
           AND pi.PartId = a.PartId
        WHERE pi.PartId IS NULL
           OR pi.QuantityOnHand - a.IssueQuantity < 0
    )
    BEGIN
        THROW 51014, 'Cannot issue parts because warehouse stock is not sufficient.', 1;
    END;

    IF EXISTS
    (
        SELECT 1
        FROM
        (
            SELECT
                i.LocationId,
                wop.PartId,
                SUM(i.Quantity) AS IssueQuantity
            FROM inserted i
            INNER JOIN service.WorkOrderParts wop ON wop.WorkOrderPartId = i.WorkOrderPartId
            GROUP BY i.LocationId, wop.PartId
        ) a
        INNER JOIN inventory.PartInventory pi
            ON pi.LocationId = a.LocationId
           AND pi.PartId = a.PartId
        WHERE pi.QuantityOnHand - a.IssueQuantity < pi.MinStockLevel
          AND NOT EXISTS
          (
                SELECT 1
                FROM inventory.PartPurchaseOrderItems poi
                INNER JOIN inventory.PartPurchaseOrders po
                    ON po.PartPurchaseOrderId = poi.PartPurchaseOrderId
                WHERE poi.PartId = a.PartId
                  AND poi.ReceivedQuantity < poi.OrderedQuantity
                  AND po.Status IN (N'Approved', N'Ordered', N'PartiallyReceived')
          )
    )
    BEGIN
        THROW 51015, 'Cannot issue parts below min stock level without an approved replacement purchase order.', 1;
    END;

    ;WITH issueAgg AS
    (
        SELECT
            i.LocationId,
            wop.PartId,
            SUM(i.Quantity) AS IssueQuantity
        FROM inserted i
        INNER JOIN service.WorkOrderParts wop ON wop.WorkOrderPartId = i.WorkOrderPartId
        GROUP BY i.LocationId, wop.PartId
    )
    UPDATE pi
       SET pi.QuantityOnHand = pi.QuantityOnHand - a.IssueQuantity,
           pi.LastUpdatedAt = SYSUTCDATETIME()
    FROM inventory.PartInventory pi
    INNER JOIN issueAgg a
        ON a.LocationId = pi.LocationId
       AND a.PartId = pi.PartId;

    ;WITH issueAgg AS
    (
        SELECT
            wop.WorkOrderPartId,
            SUM(i.Quantity) AS IssueQuantity
        FROM inserted i
        INNER JOIN service.WorkOrderParts wop ON wop.WorkOrderPartId = i.WorkOrderPartId
        GROUP BY wop.WorkOrderPartId
    )
    UPDATE wop
       SET wop.IssuedQuantity = wop.IssuedQuantity + a.IssueQuantity,
           wop.Status =
                CASE
                    WHEN wop.IssuedQuantity + a.IssueQuantity = wop.RequestedQuantity THEN N'Issued'
                    ELSE N'PartiallyIssued'
                END
    FROM service.WorkOrderParts wop
    INNER JOIN issueAgg a ON a.WorkOrderPartId = wop.WorkOrderPartId;

    INSERT INTO inventory.PartStockMovements
    (
        PartId,
        LocationId,
        MovementType,
        QuantityDelta,
        WorkOrderId,
        CreatedByStaffId
    )
    SELECT
        wop.PartId,
        i.LocationId,
        N'Issue',
        -SUM(i.Quantity),
        wop.WorkOrderId,
        MAX(i.IssuedByStaffId)
    FROM inserted i
    INNER JOIN service.WorkOrderParts wop ON wop.WorkOrderPartId = i.WorkOrderPartId
    GROUP BY wop.PartId, i.LocationId, wop.WorkOrderId;
END;
GO

CREATE INDEX IX_Leads_Status_AssignedSales ON sales.Leads(Status, AssignedSalesStaffId);
CREATE INDEX IX_SalesContracts_Customer_Status ON sales.SalesContracts(CustomerId, Status);
CREATE INDEX IX_ServiceBookings_ScheduledAt_Status ON service.ServiceBookings(ScheduledAt, Status);
CREATE INDEX IX_WorkOrders_Status ON service.WorkOrders(Status);
CREATE INDEX IX_Payments_SalesContractId ON finance.Payments(SalesContractId) WHERE SalesContractId IS NOT NULL;
CREATE INDEX IX_Payments_WorkOrderId ON finance.Payments(WorkOrderId) WHERE WorkOrderId IS NOT NULL;
GO
