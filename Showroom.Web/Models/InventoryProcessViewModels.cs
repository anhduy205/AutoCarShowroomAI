using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Showroom.Web.Validation;

namespace Showroom.Web.Models;

public sealed class InventoryProcessDashboardViewModel
{
    public IReadOnlyList<VehicleModelListItemViewModel> VehicleModels { get; init; } = Array.Empty<VehicleModelListItemViewModel>();

    public IReadOnlyList<NewVehicleListItemViewModel> NewVehicles { get; init; } = Array.Empty<NewVehicleListItemViewModel>();

    public IReadOnlyList<LocationListItemViewModel> Locations { get; init; } = Array.Empty<LocationListItemViewModel>();

    public IReadOnlyList<FactoryOrderListItemViewModel> FactoryOrders { get; init; } = Array.Empty<FactoryOrderListItemViewModel>();

    public IReadOnlyList<PartListItemViewModel> Parts { get; init; } = Array.Empty<PartListItemViewModel>();

    public IReadOnlyList<PartPurchaseOrderListItemViewModel> PartPurchaseOrders { get; init; } = Array.Empty<PartPurchaseOrderListItemViewModel>();

    public IReadOnlyList<VehicleMovementListItemViewModel> VehicleMovements { get; init; } = Array.Empty<VehicleMovementListItemViewModel>();

    public IReadOnlyList<PartStockMovementListItemViewModel> PartStockMovements { get; init; } = Array.Empty<PartStockMovementListItemViewModel>();

    public VehicleModelFormViewModel VehicleModelForm { get; init; } = new();

    public LocationFormViewModel LocationForm { get; init; } = new();

    public NewVehicleFormViewModel NewVehicleForm { get; init; } = new();

    public VehicleMovementFormViewModel VehicleMovementForm { get; init; } = new();

    public FactoryOrderFormViewModel FactoryOrderForm { get; init; } = new();

    public PartFormViewModel PartForm { get; init; } = new();

    public PartInventoryAdjustmentFormViewModel PartInventoryAdjustmentForm { get; init; } = new();

    public PartPurchaseOrderFormViewModel PartPurchaseOrderForm { get; init; } = new();

    public PartPurchaseReceiptFormViewModel PartPurchaseReceiptForm { get; init; } = new();
}

public sealed class VehicleModelListItemViewModel
{
    public int Id { get; init; }

    public string BrandName { get; init; } = string.Empty;

    public string ModelCode { get; init; } = string.Empty;

    public string ModelName { get; init; } = string.Empty;

    public short ModelYear { get; init; }

    public decimal BasePrice { get; init; }

    public int VehicleCount { get; init; }
}

public sealed class VehicleModelFormViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn hãng xe.")]
    [Display(Name = "Hãng xe")]
    public int BrandId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập mã mẫu xe.")]
    [NotWhiteSpace(ErrorMessage = "Mã mẫu xe không được để trống.")]
    [StringLength(50)]
    [Display(Name = "Mã mẫu xe")]
    public string ModelCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập tên mẫu xe.")]
    [NotWhiteSpace(ErrorMessage = "Tên mẫu xe không được để trống.")]
    [StringLength(150)]
    [Display(Name = "Tên mẫu xe")]
    public string ModelName { get; set; } = string.Empty;

    [Range(1990, 2100, ErrorMessage = "Năm mẫu xe không hợp lệ.")]
    [Display(Name = "Năm")]
    public int ModelYear { get; set; } = DateTime.UtcNow.Year;

    [Range(0, double.MaxValue, ErrorMessage = "Giá niêm yết không hợp lệ.")]
    [Display(Name = "Giá nền")]
    public decimal BasePrice { get; set; }

    public IReadOnlyList<SelectListItem> BrandOptions { get; set; } = Array.Empty<SelectListItem>();
}

public sealed class LocationListItemViewModel
{
    public int Id { get; init; }

    public string BranchName { get; init; } = string.Empty;

    public string LocationCode { get; init; } = string.Empty;

    public string LocationName { get; init; } = string.Empty;

    public string LocationType { get; init; } = LocationTypeCatalog.Storage;

    public bool IsActive { get; init; }

    public string LocationTypeLabel => LocationTypeCatalog.GetDisplayName(LocationType);
}

public sealed class LocationFormViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn chi nhánh.")]
    [Display(Name = "Chi nhánh")]
    public int BranchId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập mã vị trí.")]
    [NotWhiteSpace(ErrorMessage = "Mã vị trí không được để trống.")]
    [StringLength(50)]
    [Display(Name = "Mã vị trí")]
    public string LocationCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập tên vị trí.")]
    [NotWhiteSpace(ErrorMessage = "Tên vị trí không được để trống.")]
    [StringLength(150)]
    [Display(Name = "Tên vị trí")]
    public string LocationName { get; set; } = string.Empty;

    [Display(Name = "Loại vị trí")]
    public string LocationType { get; set; } = LocationTypeCatalog.Storage;

    [Display(Name = "Đang hoạt động")]
    public bool IsActive { get; set; } = true;

    public IReadOnlyList<SelectListItem> BranchOptions { get; set; } = Array.Empty<SelectListItem>();

    public IReadOnlyList<SelectListItem> LocationTypeOptions { get; set; } = LocationTypeCatalog.GetSelectList();
}

public sealed class NewVehicleListItemViewModel
{
    public int Id { get; init; }

    public string ModelName { get; init; } = string.Empty;

    public string Vin { get; init; } = string.Empty;

    public string EngineNumber { get; init; } = string.Empty;

    public string? CurrentLocationName { get; init; }

    public string? ExteriorColor { get; init; }

    public decimal Msrp { get; init; }

    public int CurrentOdometer { get; init; }

    public string Status { get; init; } = NewVehicleStatusCatalog.Ready;

    public string StatusLabel => NewVehicleStatusCatalog.GetDisplayName(Status);
}

public sealed class NewVehicleFormViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn mẫu xe.")]
    [Display(Name = "Mẫu xe")]
    public int VehicleModelId { get; set; }

    [Display(Name = "Dòng đơn nhà máy")]
    public int? FactoryOrderItemId { get; set; }

    [Display(Name = "Vị trí hiện tại")]
    public int? CurrentLocationId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập VIN.")]
    [StringLength(17, MinimumLength = 17, ErrorMessage = "VIN phải đủ 17 ký tự.")]
    public string Vin { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số máy.")]
    [NotWhiteSpace(ErrorMessage = "Số máy không được để trống.")]
    [StringLength(50)]
    [Display(Name = "Số máy")]
    public string EngineNumber { get; set; } = string.Empty;

    [StringLength(80)]
    [Display(Name = "Màu ngoại thất")]
    public string? ExteriorColor { get; set; }

    [StringLength(80)]
    [Display(Name = "Màu nội thất")]
    public string? InteriorColor { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Giá vốn không hợp lệ.")]
    [Display(Name = "Giá vốn")]
    public decimal CostPrice { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Giá bán đề xuất không hợp lệ.")]
    [Display(Name = "MSRP")]
    public decimal Msrp { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Số km không hợp lệ.")]
    [Display(Name = "Odometer")]
    public int CurrentOdometer { get; set; }

    [Display(Name = "Trạng thái")]
    public string Status { get; set; } = NewVehicleStatusCatalog.Ready;

    [StringLength(1000)]
    [Display(Name = "Ghi chú PDI")]
    public string? InboundDamageNote { get; set; }

    public IReadOnlyList<SelectListItem> VehicleModelOptions { get; set; } = Array.Empty<SelectListItem>();

    public IReadOnlyList<SelectListItem> FactoryOrderItemOptions { get; set; } = Array.Empty<SelectListItem>();

    public IReadOnlyList<SelectListItem> LocationOptions { get; set; } = Array.Empty<SelectListItem>();

    public IReadOnlyList<SelectListItem> StatusOptions { get; set; } = NewVehicleStatusCatalog.GetSelectList();
}

public sealed class VehicleMovementFormViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn xe.")]
    [Display(Name = "Xe theo VIN")]
    public int NewVehicleId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn vị trí đến.")]
    [Display(Name = "Vị trí đến")]
    public int ToLocationId { get; set; }

    [StringLength(300)]
    [Display(Name = "Lý do")]
    public string? Reason { get; set; }

    public IReadOnlyList<SelectListItem> NewVehicleOptions { get; set; } = Array.Empty<SelectListItem>();

    public IReadOnlyList<SelectListItem> LocationOptions { get; set; } = Array.Empty<SelectListItem>();
}

public sealed class VehicleMovementListItemViewModel
{
    public string Vin { get; init; } = string.Empty;

    public string ModelName { get; init; } = string.Empty;

    public string? FromLocationName { get; init; }

    public string ToLocationName { get; init; } = string.Empty;

    public DateTime MovedAt { get; init; }

    public string? Reason { get; init; }
}

public sealed class FactoryOrderListItemViewModel
{
    public int Id { get; init; }

    public string FactoryOrderNo { get; init; } = string.Empty;

    public string BranchName { get; init; } = string.Empty;

    public DateTime OrderDate { get; init; }

    public DateTime? ExpectedArrivalDate { get; init; }

    public string Status { get; init; } = FactoryOrderStatusCatalog.Planned;

    public int ItemCount { get; init; }

    public int TotalQuantity { get; init; }
}

public sealed class FactoryOrderFormViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập số đơn nhà máy.")]
    [NotWhiteSpace(ErrorMessage = "Số đơn không được để trống.")]
    [StringLength(50)]
    [Display(Name = "Số đơn nhà máy")]
    public string FactoryOrderNo { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn chi nhánh.")]
    [Display(Name = "Chi nhánh")]
    public int BranchId { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Ngày đặt")]
    public DateTime OrderDate { get; set; } = DateTime.Today;

    [DataType(DataType.Date)]
    [Display(Name = "Ngày dự kiến về")]
    public DateTime? ExpectedArrivalDate { get; set; }

    [Display(Name = "Trạng thái")]
    public string Status { get; set; } = FactoryOrderStatusCatalog.Planned;

    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn mẫu xe.")]
    [Display(Name = "Mẫu xe")]
    public int VehicleModelId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0.")]
    [Display(Name = "Số lượng")]
    public int Quantity { get; set; } = 1;

    [Range(0, double.MaxValue, ErrorMessage = "Giá vốn dự kiến không hợp lệ.")]
    [Display(Name = "Giá vốn dự kiến")]
    public decimal PlannedUnitCost { get; set; }

    public IReadOnlyList<SelectListItem> BranchOptions { get; set; } = Array.Empty<SelectListItem>();

    public IReadOnlyList<SelectListItem> VehicleModelOptions { get; set; } = Array.Empty<SelectListItem>();

    public IReadOnlyList<SelectListItem> StatusOptions { get; set; } = FactoryOrderStatusCatalog.GetSelectList();
}

public sealed class PartListItemViewModel
{
    public int Id { get; init; }

    public string PartCode { get; init; } = string.Empty;

    public string PartName { get; init; } = string.Empty;

    public string Unit { get; init; } = string.Empty;

    public decimal ListPrice { get; init; }

    public bool IsActive { get; init; }

    public decimal QuantityOnHand { get; init; }

    public decimal MinStockLevel { get; init; }
}

public sealed class PartFormViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập mã phụ tùng.")]
    [NotWhiteSpace(ErrorMessage = "Mã phụ tùng không được để trống.")]
    [StringLength(50)]
    [Display(Name = "Mã phụ tùng")]
    public string PartCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập tên phụ tùng.")]
    [NotWhiteSpace(ErrorMessage = "Tên phụ tùng không được để trống.")]
    [StringLength(150)]
    [Display(Name = "Tên phụ tùng")]
    public string PartName { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    [Display(Name = "Đơn vị")]
    public string Unit { get; set; } = "pcs";

    [Range(0, double.MaxValue, ErrorMessage = "Giá bán không hợp lệ.")]
    [Display(Name = "Giá bán")]
    public decimal ListPrice { get; set; }

    [Display(Name = "Đang hoạt động")]
    public bool IsActive { get; set; } = true;
}

public sealed class PartInventoryAdjustmentFormViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn phụ tùng.")]
    [Display(Name = "Phụ tùng")]
    public int PartId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn vị trí.")]
    [Display(Name = "Vị trí")]
    public int LocationId { get; set; }

    [Display(Name = "Tăng/giảm tồn")]
    public decimal QuantityDelta { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Tồn tối thiểu không hợp lệ.")]
    [Display(Name = "Tồn tối thiểu")]
    public decimal MinStockLevel { get; set; }

    [StringLength(300)]
    [Display(Name = "Ghi chú")]
    public string? Note { get; set; }

    public IReadOnlyList<SelectListItem> PartOptions { get; set; } = Array.Empty<SelectListItem>();

    public IReadOnlyList<SelectListItem> LocationOptions { get; set; } = Array.Empty<SelectListItem>();
}

public sealed class PartPurchaseOrderListItemViewModel
{
    public int Id { get; init; }

    public string PurchaseOrderNo { get; init; } = string.Empty;

    public string BranchName { get; init; } = string.Empty;

    public string SupplierName { get; init; } = string.Empty;

    public DateTime OrderDate { get; init; }

    public DateTime? ExpectedArrivalDate { get; init; }

    public string Status { get; init; } = PartPurchaseOrderStatusCatalog.Draft;

    public int ItemCount { get; init; }

    public decimal TotalQuantity { get; init; }
}

public sealed class PartPurchaseOrderFormViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập số đơn phụ tùng.")]
    [NotWhiteSpace(ErrorMessage = "Số đơn không được để trống.")]
    [StringLength(50)]
    [Display(Name = "Số đơn phụ tùng")]
    public string PurchaseOrderNo { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn chi nhánh.")]
    [Display(Name = "Chi nhánh")]
    public int BranchId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập nhà cung cấp.")]
    [NotWhiteSpace(ErrorMessage = "Nhà cung cấp không được để trống.")]
    [StringLength(150)]
    [Display(Name = "Nhà cung cấp")]
    public string SupplierName { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    [Display(Name = "Ngày đặt")]
    public DateTime OrderDate { get; set; } = DateTime.Today;

    [DataType(DataType.Date)]
    [Display(Name = "Ngày dự kiến về")]
    public DateTime? ExpectedArrivalDate { get; set; }

    [Display(Name = "Trạng thái")]
    public string Status { get; set; } = PartPurchaseOrderStatusCatalog.Draft;

    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn phụ tùng.")]
    [Display(Name = "Phụ tùng")]
    public int PartId { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0.")]
    [Display(Name = "Số lượng đặt")]
    public decimal OrderedQuantity { get; set; } = 1;

    [Range(0, double.MaxValue, ErrorMessage = "Đơn giá không hợp lệ.")]
    [Display(Name = "Đơn giá")]
    public decimal UnitCost { get; set; }

    public IReadOnlyList<SelectListItem> BranchOptions { get; set; } = Array.Empty<SelectListItem>();

    public IReadOnlyList<SelectListItem> PartOptions { get; set; } = Array.Empty<SelectListItem>();

    public IReadOnlyList<SelectListItem> StatusOptions { get; set; } = PartPurchaseOrderStatusCatalog.GetSelectList();
}

public sealed class PartPurchaseReceiptFormViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn dòng đơn phụ tùng.")]
    [Display(Name = "Dòng đơn phụ tùng")]
    public int PartPurchaseOrderItemId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn vị trí nhận.")]
    [Display(Name = "Vị trí nhận")]
    public int LocationId { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Số lượng nhận phải lớn hơn 0.")]
    [Display(Name = "Số lượng nhận")]
    public decimal ReceivedQuantity { get; set; } = 1;

    [StringLength(300)]
    [Display(Name = "Ghi chú")]
    public string? Note { get; set; }

    public IReadOnlyList<SelectListItem> PartPurchaseOrderItemOptions { get; set; } = Array.Empty<SelectListItem>();

    public IReadOnlyList<SelectListItem> LocationOptions { get; set; } = Array.Empty<SelectListItem>();
}

public sealed class PartStockMovementListItemViewModel
{
    public string PartCode { get; init; } = string.Empty;

    public string PartName { get; init; } = string.Empty;

    public string LocationName { get; init; } = string.Empty;

    public string MovementType { get; init; } = string.Empty;

    public decimal QuantityDelta { get; init; }

    public DateTime CreatedAt { get; init; }

    public string? Note { get; init; }
}

public static class LocationTypeCatalog
{
    public const string Storage = "Storage";
    public const string Showroom = "Showroom";
    public const string Workshop = "Workshop";
    public const string Transit = "Transit";
    public const string Delivery = "Delivery";

    public static IReadOnlyList<SelectListItem> GetSelectList(string? selected = null)
        => BuildSelectList(GetItems(), selected);

    public static string Normalize(string? value)
        => GetItems().Any(item => string.Equals(item.Value, value, StringComparison.OrdinalIgnoreCase))
            ? value!.Trim()
            : Storage;

    public static string GetDisplayName(string? value)
        => GetItems().FirstOrDefault(item => string.Equals(item.Value, value, StringComparison.OrdinalIgnoreCase))?.Text ?? "Kho xe";

    private static IReadOnlyList<SelectListItem> GetItems()
        => new[]
        {
            new SelectListItem { Value = Storage, Text = "Kho xe" },
            new SelectListItem { Value = Showroom, Text = "Trưng bày" },
            new SelectListItem { Value = Workshop, Text = "Xưởng" },
            new SelectListItem { Value = Transit, Text = "Đang vận chuyển" },
            new SelectListItem { Value = Delivery, Text = "Khu giao xe" }
        };

    private static IReadOnlyList<SelectListItem> BuildSelectList(IReadOnlyList<SelectListItem> items, string? selected)
        => items.Select(item => new SelectListItem { Value = item.Value, Text = item.Text, Selected = item.Value == selected }).ToArray();
}

public static class NewVehicleStatusCatalog
{
    public const string Ready = "Ready";

    public static IReadOnlyList<SelectListItem> GetSelectList(string? selected = null)
        => BuildSelectList(GetItems(), selected ?? Ready);

    public static string Normalize(string? value)
        => GetItems().Any(item => string.Equals(item.Value, value, StringComparison.OrdinalIgnoreCase))
            ? value!.Trim()
            : Ready;

    public static string GetDisplayName(string? value)
        => GetItems().FirstOrDefault(item => string.Equals(item.Value, value, StringComparison.OrdinalIgnoreCase))?.Text ?? "Sẵn sàng";

    private static IReadOnlyList<SelectListItem> GetItems()
        => new[]
        {
            new SelectListItem { Value = "Ordered", Text = "Đã đặt" },
            new SelectListItem { Value = "Inbound", Text = "Đang về kho" },
            new SelectListItem { Value = Ready, Text = "Sẵn sàng" },
            new SelectListItem { Value = "OnDisplay", Text = "Trưng bày" },
            new SelectListItem { Value = "Allocated_Locked", Text = "Đã khoá VIN" },
            new SelectListItem { Value = "PendingDelivery", Text = "Chờ giao" },
            new SelectListItem { Value = "Delivered", Text = "Đã giao" },
            new SelectListItem { Value = "ServiceHold", Text = "Giữ dịch vụ" },
            new SelectListItem { Value = "DamagedHold", Text = "Giữ do hư hỏng" },
            new SelectListItem { Value = "ReturnedToFactory", Text = "Trả nhà máy" }
        };

    private static IReadOnlyList<SelectListItem> BuildSelectList(IReadOnlyList<SelectListItem> items, string? selected)
        => items.Select(item => new SelectListItem { Value = item.Value, Text = item.Text, Selected = item.Value == selected }).ToArray();
}

public static class FactoryOrderStatusCatalog
{
    public const string Planned = "Planned";

    public static IReadOnlyList<SelectListItem> GetSelectList(string? selected = null)
        => BuildSelectList(GetItems(), selected ?? Planned);

    public static string Normalize(string? value)
        => GetItems().Any(item => string.Equals(item.Value, value, StringComparison.OrdinalIgnoreCase))
            ? value!.Trim()
            : Planned;

    private static IReadOnlyList<SelectListItem> GetItems()
        => new[]
        {
            new SelectListItem { Value = Planned, Text = "Dự kiến" },
            new SelectListItem { Value = "Ordered", Text = "Đã đặt" },
            new SelectListItem { Value = "InTransit", Text = "Đang vận chuyển" },
            new SelectListItem { Value = "Received", Text = "Đã nhận" },
            new SelectListItem { Value = "Cancelled", Text = "Đã huỷ" }
        };

    private static IReadOnlyList<SelectListItem> BuildSelectList(IReadOnlyList<SelectListItem> items, string? selected)
        => items.Select(item => new SelectListItem { Value = item.Value, Text = item.Text, Selected = item.Value == selected }).ToArray();
}

public static class PartPurchaseOrderStatusCatalog
{
    public const string Draft = "Draft";

    public static IReadOnlyList<SelectListItem> GetSelectList(string? selected = null)
        => BuildSelectList(GetItems(), selected ?? Draft);

    public static string Normalize(string? value)
        => GetItems().Any(item => string.Equals(item.Value, value, StringComparison.OrdinalIgnoreCase))
            ? value!.Trim()
            : Draft;

    private static IReadOnlyList<SelectListItem> GetItems()
        => new[]
        {
            new SelectListItem { Value = Draft, Text = "Nháp" },
            new SelectListItem { Value = "Approved", Text = "Đã duyệt" },
            new SelectListItem { Value = "Ordered", Text = "Đã đặt" },
            new SelectListItem { Value = "PartiallyReceived", Text = "Nhận một phần" },
            new SelectListItem { Value = "Received", Text = "Đã nhận" },
            new SelectListItem { Value = "Cancelled", Text = "Đã huỷ" }
        };

    private static IReadOnlyList<SelectListItem> BuildSelectList(IReadOnlyList<SelectListItem> items, string? selected)
        => items.Select(item => new SelectListItem { Value = item.Value, Text = item.Text, Selected = item.Value == selected }).ToArray();
}
