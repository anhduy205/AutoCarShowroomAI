# Thiet ke database quy trinh showroom o to

File SQL Server di kem: [database/showroom_process_schema.sql](../database/showroom_process_schema.sql)

Thiet ke nay tach rieng thanh database `AutoCarShowroomProcessDb` de khong anh huong schema hien tai cua ung dung. Cac bang duoc chia theo schema nghiep vu:

- `core`: chi nhanh, khach hang, nhan vien, vai tro.
- `inventory`: hang xe, dong xe, xe moi, VIN, vi tri kho/showroom, dat hang nha may, phu tung, ton kho.
- `sales`: lead, opportunity, bao gia, lai thu, hop dong, phu kien, dang ky, ban giao xe.
- `service`: xe cua khach, lich hen, tiep nhan, lenh sua chua, cong viec, phu tung xuat cho xuat xuong, QC, thong bao.
- `finance`: phe duyet, hoa don, thanh toan, doi soat, dieu chinh, hoa hong, KPI.

## Mapping quy trinh

### Sales process

- Tiep nhan va phan loai: `sales.Leads`, `sales.SalesOpportunities`.
- Tu van va lai thu: `sales.SalesQuotes`, `sales.TestDrives`.
- Dam phan va ky hop dong: `sales.SalesContracts`, `sales.ContractAccessories`.
- Thu tuc hanh chinh va dang ky: `sales.RegistrationTasks`.
- Ban giao xe: `sales.VehicleDeliveries`, `sales.DeliveryChecklistItems`.

### Service process

- Dat lich hen: `service.ServiceBookings`.
- Tiep nhan xe: `service.Intakes`, `service.IntakeMedia`.
- Lenh sua chua va xuong: `service.WorkOrders`, `service.WorkOrderJobs`.
- Phu tung: `service.WorkOrderParts`, `service.PartIssues`, `inventory.PartInventory`.
- QC va ban giao: `service.QualityChecks`, `service.ServiceNotifications`.

### Inventory process

- Dat hang nha may: `inventory.FactoryOrders`, `inventory.FactoryOrderItems`.
- Tiep nhan xe: `inventory.NewVehicles`.
- Quan ly vi tri: `inventory.Locations`, `inventory.VehicleLocationMovements`.
- Xuat/khoa xe theo hop dong: `sales.SalesContracts` lien ket `inventory.NewVehicles`.

### Financial process

- Thu tien: `finance.Payments`.
- Xuat hoa don VAT: `finance.SalesInvoices`, `finance.ServiceInvoices`.
- Doi soat: `finance.Reconciliations`.
- Hoa hong va KPI: `finance.StaffCommissionLedger`, `finance.TechnicianKpiBonuses`.

## Rang buoc chinh

- VIN duy nhat va dung chuan 17 ky tu: `UQ_NewVehicles_Vin`, `CK_NewVehicles_Vin_Format`.
- Moi VIN chi co mot hop dong chua thanh ly: filtered unique index `UX_SalesContracts_OneOpenContractPerVehicle`.
- Xe da khoa cho hop dong thi khong xuat hoa don cho hop dong/khach khac: trigger `finance.TR_SalesInvoices_ValidateVehicleAndCustomer`.
- Hop dong khong du coc toi thieu 10% thi khong duoc qua trang thai `Draft`: `CK_SalesContracts_MinDeposit`.
- Gia ban thap hon gia von can phe duyet `BelowCostSale` boi `SalesManager`: trigger `sales.TR_SalesContracts_ValidatePriceAndLock`.
- Chi `Accountant` duoc sua `FinalSalePrice` sau khi tao hop dong: trigger `sales.TR_SalesContracts_AccountingOnlyPriceChange`.
- Moi xe khach chi co mot work order dang mo: filtered unique index `UX_WorkOrders_OneOpenWorkOrderPerVehicle`.
- Work order khong duoc hoan tat/dong neu phu tung chua xuat du: trigger `service.TR_WorkOrders_RequirePartsBeforeCompletion`.
- Gio cong thuc te khong vuot dinh muc neu chua phe duyet: trigger `service.TR_WorkOrderJobs_ValidateLaborLimit`.
- Xuat phu tung tu dong tru kho va chan xuat neu duoi min stock ma khong co PO da duyet: trigger `service.TR_PartIssues_DecrementStock`.
- Xe chi duoc chuyen tu kho luu tru ra showroom khi status `Ready`: trigger `inventory.TR_VehicleLocationMovements_ValidateTransfer`.
- Khong tat toan hop dong/dong phieu dich vu neu thanh toan thuc te nho hon hoa don, tru khi co dieu chinh duoc duyet: triggers `sales.TR_SalesContracts_BlockUnderpaidSettlement`, `service.TR_WorkOrders_BlockUnderpaidClose`.
- Thoi gian giao dich tai chinh khong duoc sua sau khi ghi nhan/xuat hoa don: triggers `finance.TR_Payments_PreventPaidAtUpdate`, `finance.TR_SalesInvoices_PreventIssuedTimeChange`, `finance.TR_ServiceInvoices_PreventIssuedTimeChange`.

## Ghi chu trien khai

Ung dung can set nhan vien dang thao tac vao SQL Server session context truoc khi update gia hop dong:

```sql
EXEC sp_set_session_context @key = N'actor_staff_id', @value = @CurrentStaffId;
```

Dieu nay cho phep trigger database kiem tra RBAC truc tiep tren bang `core.StaffUserRoles`.

