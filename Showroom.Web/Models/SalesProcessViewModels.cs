using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Showroom.Web.Models;

public sealed class SalesProcessDashboardViewModel
{
    public IReadOnlyList<LeadListItemViewModel> Leads { get; init; } = Array.Empty<LeadListItemViewModel>();

    public IReadOnlyList<SalesOpportunityListItemViewModel> Opportunities { get; init; } = Array.Empty<SalesOpportunityListItemViewModel>();

    public IReadOnlyList<TestDriveListItemViewModel> TestDrives { get; init; } = Array.Empty<TestDriveListItemViewModel>();

    public IReadOnlyList<SalesQuoteListItemViewModel> Quotes { get; init; } = Array.Empty<SalesQuoteListItemViewModel>();

    public IReadOnlyList<SalesContractListItemViewModel> Contracts { get; init; } = Array.Empty<SalesContractListItemViewModel>();

    public IReadOnlyList<ContractAccessoryListItemViewModel> Accessories { get; init; } = Array.Empty<ContractAccessoryListItemViewModel>();

    public IReadOnlyList<RegistrationTaskListItemViewModel> RegistrationTasks { get; init; } = Array.Empty<RegistrationTaskListItemViewModel>();

    public IReadOnlyList<VehicleDeliveryListItemViewModel> Deliveries { get; init; } = Array.Empty<VehicleDeliveryListItemViewModel>();

    public IReadOnlyList<DeliveryChecklistItemListItemViewModel> DeliveryChecklistItems { get; init; } = Array.Empty<DeliveryChecklistItemListItemViewModel>();

    public LeadFormViewModel LeadForm { get; init; } = new();

    public SalesOpportunityFormViewModel OpportunityForm { get; init; } = new();

    public TestDriveFormViewModel TestDriveForm { get; init; } = new();

    public SalesQuoteFormViewModel QuoteForm { get; init; } = new();

    public SalesContractFormViewModel ContractForm { get; init; } = new();

    public ContractAccessoryFormViewModel AccessoryForm { get; init; } = new();

    public RegistrationTaskFormViewModel RegistrationTaskForm { get; init; } = new();

    public VehicleDeliveryFormViewModel DeliveryForm { get; init; } = new();

    public DeliveryChecklistItemFormViewModel ChecklistItemForm { get; init; } = new();
}

public sealed class LeadListItemViewModel
{
    public int Id { get; init; }

    public string ContactName { get; init; } = string.Empty;

    public string? ContactPhone { get; init; }

    public string? ContactEmail { get; init; }

    public string Source { get; init; } = LeadSourceCatalog.Website;

    public string SourceLabel => LeadSourceCatalog.GetDisplayName(Source);

    public string Status { get; init; } = LeadStatusCatalog.New;

    public string StatusLabel => LeadStatusCatalog.GetDisplayName(Status);

    public byte LeadScore { get; init; }

    public string? DesiredModelName { get; init; }

    public string? AssignedSalesName { get; init; }

    public DateTime CreatedAt { get; init; }
}

public sealed class SalesOpportunityListItemViewModel
{
    public int Id { get; init; }

    public string OpportunityName { get; init; } = string.Empty;

    public string LeadName { get; init; } = string.Empty;

    public string? CustomerName { get; init; }

    public string? DesiredModelName { get; init; }

    public string Stage { get; init; } = SalesStageCatalog.LeadManagement;

    public string StageLabel => SalesStageCatalog.GetDisplayName(Stage);

    public byte ProbabilityPercent { get; init; }

    public DateTime? ExpectedCloseDate { get; init; }

    public DateTime CreatedAt { get; init; }
}

public sealed class TestDriveListItemViewModel
{
    public int Id { get; init; }

    public string OpportunityName { get; init; } = string.Empty;

    public string Vin { get; init; } = string.Empty;

    public string ModelName { get; init; } = string.Empty;

    public DateTime ScheduledStartAt { get; init; }

    public string Status { get; init; } = TestDriveStatusCatalog.Scheduled;

    public string StatusLabel => TestDriveStatusCatalog.GetDisplayName(Status);
}

public sealed class SalesQuoteListItemViewModel
{
    public int Id { get; init; }

    public string QuoteNo { get; init; } = string.Empty;

    public string OpportunityName { get; init; } = string.Empty;

    public string ModelName { get; init; } = string.Empty;

    public string? Vin { get; init; }

    public decimal VehiclePrice { get; init; }

    public decimal DiscountAmount { get; init; }

    public decimal RegistrationFeeEstimate { get; init; }

    public decimal InsuranceEstimate { get; init; }

    public decimal AccessoryPackageValue { get; init; }

    public decimal TotalEstimate => VehiclePrice - DiscountAmount + RegistrationFeeEstimate + InsuranceEstimate + AccessoryPackageValue;

    public DateTime? ValidUntil { get; init; }

    public string Status { get; init; } = SalesQuoteStatusCatalog.Draft;

    public string StatusLabel => SalesQuoteStatusCatalog.GetDisplayName(Status);
}

public sealed class SalesContractListItemViewModel
{
    public int Id { get; init; }

    public string ContractNo { get; init; } = string.Empty;

    public string OpportunityName { get; init; } = string.Empty;

    public string? CustomerName { get; init; }

    public string Vin { get; init; } = string.Empty;

    public string ModelName { get; init; } = string.Empty;

    public decimal FinalSalePrice { get; init; }

    public decimal DepositAmount { get; init; }

    public string PaymentMethod { get; init; } = SalesPaymentMethodCatalog.Cash;

    public string PaymentMethodLabel => SalesPaymentMethodCatalog.GetDisplayName(PaymentMethod);

    public string Status { get; init; } = SalesContractStatusCatalog.Draft;

    public string StatusLabel => SalesContractStatusCatalog.GetDisplayName(Status);

    public DateTime CreatedAt { get; init; }
}

public sealed class ContractAccessoryListItemViewModel
{
    public string ContractNo { get; init; } = string.Empty;

    public string AccessoryName { get; init; } = string.Empty;

    public int Quantity { get; init; }

    public decimal UnitPrice { get; init; }

    public bool IsGift { get; init; }
}

public sealed class RegistrationTaskListItemViewModel
{
    public string ContractNo { get; init; } = string.Empty;

    public string TaskType { get; init; } = RegistrationTaskTypeCatalog.RegistrationTax;

    public string TaskTypeLabel => RegistrationTaskTypeCatalog.GetDisplayName(TaskType);

    public string Status { get; init; } = RegistrationTaskStatusCatalog.Pending;

    public string StatusLabel => RegistrationTaskStatusCatalog.GetDisplayName(Status);

    public DateTime? DueDate { get; init; }

    public decimal Amount { get; init; }

    public string? HandledByName { get; init; }
}

public sealed class VehicleDeliveryListItemViewModel
{
    public int Id { get; init; }

    public string ContractNo { get; init; } = string.Empty;

    public string Vin { get; init; } = string.Empty;

    public string ModelName { get; init; } = string.Empty;

    public string? DeliveryStaffName { get; init; }

    public DateTime? DeliveredAt { get; init; }

    public int OdometerAtDelivery { get; init; }

    public bool CustomerAccepted { get; init; }
}

public sealed class DeliveryChecklistItemListItemViewModel
{
    public string ContractNo { get; init; } = string.Empty;

    public string ChecklistName { get; init; } = string.Empty;

    public bool IsCompleted { get; init; }

    public DateTime? CompletedAt { get; init; }
}

public sealed class LeadFormViewModel
{
    [Display(Name = "Khách hàng")]
    public int? CustomerId { get; set; }

    [Display(Name = "Yêu cầu khách")]
    public int? CustomerRequestId { get; set; }

    [Display(Name = "Nhân viên sales")]
    public int? AssignedSalesStaffUserId { get; set; }

    [Display(Name = "Mẫu quan tâm")]
    public int? DesiredModelId { get; set; }

    [Display(Name = "Nguồn")]
    public string Source { get; set; } = LeadSourceCatalog.Website;

    [Required(ErrorMessage = "Vui lòng nhập tên liên hệ.")]
    [StringLength(150)]
    [Display(Name = "Tên liên hệ")]
    public string ContactName { get; set; } = string.Empty;

    [StringLength(30)]
    [Display(Name = "Số điện thoại")]
    public string? ContactPhone { get; set; }

    [EmailAddress]
    [StringLength(254)]
    [Display(Name = "Email")]
    public string? ContactEmail { get; set; }

    [Range(0, 100)]
    [Display(Name = "Điểm lead")]
    public byte LeadScore { get; set; } = 10;

    [Display(Name = "Trạng thái")]
    public string Status { get; set; } = LeadStatusCatalog.New;

    [StringLength(500)]
    [Display(Name = "Ghi chú")]
    public string? Note { get; set; }

    public IReadOnlyList<SelectListItem> CustomerOptions { get; set; } = Array.Empty<SelectListItem>();

    public IReadOnlyList<SelectListItem> CustomerRequestOptions { get; set; } = Array.Empty<SelectListItem>();

    public IReadOnlyList<SelectListItem> StaffOptions { get; set; } = Array.Empty<SelectListItem>();

    public IReadOnlyList<SelectListItem> ModelOptions { get; set; } = Array.Empty<SelectListItem>();

    public IReadOnlyList<SelectListItem> SourceOptions { get; set; } = LeadSourceCatalog.GetSelectList();

    public IReadOnlyList<SelectListItem> StatusOptions { get; set; } = LeadStatusCatalog.GetSelectList();
}

public sealed class SalesOpportunityFormViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn lead.")]
    [Display(Name = "Lead")]
    public int LeadId { get; set; }

    [Display(Name = "Khách hàng")]
    public int? CustomerId { get; set; }

    [Display(Name = "Mẫu mong muốn")]
    public int? DesiredModelId { get; set; }

    [Display(Name = "Nhân viên sales")]
    public int? SalesStaffUserId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên cơ hội.")]
    [StringLength(150)]
    [Display(Name = "Tên cơ hội")]
    public string OpportunityName { get; set; } = string.Empty;

    [Display(Name = "Giai đoạn")]
    public string Stage { get; set; } = SalesStageCatalog.LeadManagement;

    [Range(0, 100)]
    [Display(Name = "Xác suất")]
    public byte ProbabilityPercent { get; set; } = 20;

    [Display(Name = "Dự kiến chốt")]
    public DateTime? ExpectedCloseDate { get; set; }

    public IReadOnlyList<SelectListItem> LeadOptions { get; set; } = Array.Empty<SelectListItem>();

    public IReadOnlyList<SelectListItem> CustomerOptions { get; set; } = Array.Empty<SelectListItem>();

    public IReadOnlyList<SelectListItem> ModelOptions { get; set; } = Array.Empty<SelectListItem>();

    public IReadOnlyList<SelectListItem> StaffOptions { get; set; } = Array.Empty<SelectListItem>();

    public IReadOnlyList<SelectListItem> StageOptions { get; set; } = SalesStageCatalog.GetSelectList();
}

public sealed class TestDriveFormViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn cơ hội.")]
    [Display(Name = "Cơ hội")]
    public int OpportunityId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn VIN.")]
    [Display(Name = "Xe lái thử")]
    public int NewVehicleId { get; set; }

    [Required]
    [Display(Name = "Lịch hẹn")]
    public DateTime ScheduledStartAt { get; set; } = DateTime.Now.AddDays(1);

    [Display(Name = "Trạng thái")]
    public string Status { get; set; } = TestDriveStatusCatalog.Scheduled;

    [StringLength(500)]
    [Display(Name = "Ghi chú")]
    public string? FeedbackNote { get; set; }

    public IReadOnlyList<SelectListItem> OpportunityOptions { get; set; } = Array.Empty<SelectListItem>();

    public IReadOnlyList<SelectListItem> VehicleOptions { get; set; } = Array.Empty<SelectListItem>();

    public IReadOnlyList<SelectListItem> StatusOptions { get; set; } = TestDriveStatusCatalog.GetSelectList();
}

public sealed class SalesQuoteFormViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập số báo giá.")]
    [StringLength(50)]
    [Display(Name = "Số báo giá")]
    public string QuoteNo { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn cơ hội.")]
    [Display(Name = "Cơ hội")]
    public int OpportunityId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn mẫu xe.")]
    [Display(Name = "Mẫu xe")]
    public int ModelId { get; set; }

    [Display(Name = "VIN cụ thể")]
    public int? NewVehicleId { get; set; }

    [Range(0, double.MaxValue)]
    [Display(Name = "Giá xe")]
    public decimal VehiclePrice { get; set; }

    [Range(0, double.MaxValue)]
    [Display(Name = "Giảm giá")]
    public decimal DiscountAmount { get; set; }

    [Range(0, double.MaxValue)]
    [Display(Name = "Phí đăng ký")]
    public decimal RegistrationFeeEstimate { get; set; }

    [Range(0, double.MaxValue)]
    [Display(Name = "Bảo hiểm")]
    public decimal InsuranceEstimate { get; set; }

    [Range(0, double.MaxValue)]
    [Display(Name = "Phụ kiện")]
    public decimal AccessoryPackageValue { get; set; }

    [Display(Name = "Hiệu lực đến")]
    public DateTime? ValidUntil { get; set; }

    [Display(Name = "Trạng thái")]
    public string Status { get; set; } = SalesQuoteStatusCatalog.Draft;

    public IReadOnlyList<SelectListItem> OpportunityOptions { get; set; } = Array.Empty<SelectListItem>();

    public IReadOnlyList<SelectListItem> ModelOptions { get; set; } = Array.Empty<SelectListItem>();

    public IReadOnlyList<SelectListItem> VehicleOptions { get; set; } = Array.Empty<SelectListItem>();

    public IReadOnlyList<SelectListItem> StatusOptions { get; set; } = SalesQuoteStatusCatalog.GetSelectList();
}

public sealed class SalesContractFormViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập số hợp đồng.")]
    [StringLength(50)]
    [Display(Name = "Số hợp đồng")]
    public string ContractNo { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn cơ hội.")]
    [Display(Name = "Cơ hội")]
    public int OpportunityId { get; set; }

    [Display(Name = "Khách hàng")]
    public int? CustomerId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn VIN.")]
    [Display(Name = "VIN")]
    public int NewVehicleId { get; set; }

    [Display(Name = "Nhân viên sales")]
    public int? SalesStaffUserId { get; set; }

    [Range(0, double.MaxValue)]
    [Display(Name = "Giá chốt")]
    public decimal FinalSalePrice { get; set; }

    [Display(Name = "Thanh toán")]
    public string PaymentMethod { get; set; } = SalesPaymentMethodCatalog.Cash;

    [Display(Name = "Trạng thái")]
    public string Status { get; set; } = SalesContractStatusCatalog.Draft;

    [Range(0, double.MaxValue)]
    [Display(Name = "Tiền cọc")]
    public decimal DepositAmount { get; set; }

    [Display(Name = "Hạn cọc")]
    public DateTime? DepositDueAt { get; set; }

    [Display(Name = "Ngày ký")]
    public DateTime? SignedAt { get; set; }

    public IReadOnlyList<SelectListItem> OpportunityOptions { get; set; } = Array.Empty<SelectListItem>();

    public IReadOnlyList<SelectListItem> CustomerOptions { get; set; } = Array.Empty<SelectListItem>();

    public IReadOnlyList<SelectListItem> VehicleOptions { get; set; } = Array.Empty<SelectListItem>();

    public IReadOnlyList<SelectListItem> StaffOptions { get; set; } = Array.Empty<SelectListItem>();

    public IReadOnlyList<SelectListItem> PaymentMethodOptions { get; set; } = SalesPaymentMethodCatalog.GetSelectList();

    public IReadOnlyList<SelectListItem> StatusOptions { get; set; } = SalesContractStatusCatalog.GetSelectList();
}

public sealed class ContractAccessoryFormViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn hợp đồng.")]
    [Display(Name = "Hợp đồng")]
    public int SalesContractId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập phụ kiện.")]
    [StringLength(150)]
    [Display(Name = "Phụ kiện")]
    public string AccessoryName { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    [Display(Name = "SL")]
    public int Quantity { get; set; } = 1;

    [Range(0, double.MaxValue)]
    [Display(Name = "Giá bán")]
    public decimal UnitPrice { get; set; }

    [Range(0, double.MaxValue)]
    [Display(Name = "Giá vốn")]
    public decimal UnitCost { get; set; }

    [Display(Name = "Quà tặng")]
    public bool IsGift { get; set; }

    public IReadOnlyList<SelectListItem> ContractOptions { get; set; } = Array.Empty<SelectListItem>();
}

public sealed class RegistrationTaskFormViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn hợp đồng.")]
    [Display(Name = "Hợp đồng")]
    public int SalesContractId { get; set; }

    [Display(Name = "Loại thủ tục")]
    public string TaskType { get; set; } = RegistrationTaskTypeCatalog.RegistrationTax;

    [Display(Name = "Trạng thái")]
    public string Status { get; set; } = RegistrationTaskStatusCatalog.Pending;

    [Display(Name = "Hạn xử lý")]
    public DateTime? DueDate { get; set; }

    [Range(0, double.MaxValue)]
    [Display(Name = "Chi phí")]
    public decimal Amount { get; set; }

    [Display(Name = "Người xử lý")]
    public int? HandledByStaffUserId { get; set; }

    [StringLength(300)]
    [Display(Name = "Ghi chú")]
    public string? Note { get; set; }

    public IReadOnlyList<SelectListItem> ContractOptions { get; set; } = Array.Empty<SelectListItem>();

    public IReadOnlyList<SelectListItem> StaffOptions { get; set; } = Array.Empty<SelectListItem>();

    public IReadOnlyList<SelectListItem> TaskTypeOptions { get; set; } = RegistrationTaskTypeCatalog.GetSelectList();

    public IReadOnlyList<SelectListItem> StatusOptions { get; set; } = RegistrationTaskStatusCatalog.GetSelectList();
}

public sealed class VehicleDeliveryFormViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn hợp đồng.")]
    [Display(Name = "Hợp đồng")]
    public int SalesContractId { get; set; }

    [Display(Name = "Nhân viên bàn giao")]
    public int? DeliveryStaffUserId { get; set; }

    [Display(Name = "Thời điểm giao")]
    public DateTime? DeliveredAt { get; set; }

    [Range(0, int.MaxValue)]
    [Display(Name = "Odo khi giao")]
    public int OdometerAtDelivery { get; set; }

    [Display(Name = "Khách đã nhận")]
    public bool CustomerAccepted { get; set; }

    [StringLength(500)]
    [Display(Name = "Ghi chú")]
    public string? Note { get; set; }

    public IReadOnlyList<SelectListItem> ContractOptions { get; set; } = Array.Empty<SelectListItem>();

    public IReadOnlyList<SelectListItem> StaffOptions { get; set; } = Array.Empty<SelectListItem>();
}

public sealed class DeliveryChecklistItemFormViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn phiếu bàn giao.")]
    [Display(Name = "Phiếu bàn giao")]
    public int VehicleDeliveryId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập checklist.")]
    [StringLength(150)]
    [Display(Name = "Checklist")]
    public string ChecklistName { get; set; } = string.Empty;

    [Display(Name = "Hoàn tất")]
    public bool IsCompleted { get; set; }

    [StringLength(300)]
    [Display(Name = "Ghi chú")]
    public string? Note { get; set; }

    public IReadOnlyList<SelectListItem> DeliveryOptions { get; set; } = Array.Empty<SelectListItem>();
}

public static class LeadSourceCatalog
{
    public const string Website = "Website";
    public const string Showroom = "Showroom";
    public const string App = "App";
    public const string Phone = "Phone";
    public const string Social = "Social";
    public const string Referral = "Referral";
    public const string Other = "Other";

    public static IReadOnlyList<SelectListItem> GetSelectList(string? selected = null) => SalesCatalogHelpers.Build(GetItems(), selected);

    public static string GetDisplayName(string? value) => SalesCatalogHelpers.Display(GetItems(), value, "Website");

    public static string Normalize(string? value) => SalesCatalogHelpers.Normalize(GetItems(), value, Website);

    private static IReadOnlyList<SelectListItem> GetItems() => new[]
    {
        new SelectListItem { Value = Website, Text = "Website" },
        new SelectListItem { Value = Showroom, Text = "Showroom" },
        new SelectListItem { Value = App, Text = "Ứng dụng" },
        new SelectListItem { Value = Phone, Text = "Điện thoại" },
        new SelectListItem { Value = Social, Text = "Mạng xã hội" },
        new SelectListItem { Value = Referral, Text = "Giới thiệu" },
        new SelectListItem { Value = Other, Text = "Khác" }
    };
}

public static class LeadStatusCatalog
{
    public const string New = "New";
    public const string Qualified = "Qualified";
    public const string Unqualified = "Unqualified";
    public const string Converted = "Converted";
    public const string Lost = "Lost";

    public static IReadOnlyList<SelectListItem> GetSelectList(string? selected = null) => SalesCatalogHelpers.Build(GetItems(), selected);

    public static string GetDisplayName(string? value) => SalesCatalogHelpers.Display(GetItems(), value, "Mới");

    public static string Normalize(string? value) => SalesCatalogHelpers.Normalize(GetItems(), value, New);

    private static IReadOnlyList<SelectListItem> GetItems() => new[]
    {
        new SelectListItem { Value = New, Text = "Mới" },
        new SelectListItem { Value = Qualified, Text = "Đủ tiềm năng" },
        new SelectListItem { Value = Unqualified, Text = "Chưa phù hợp" },
        new SelectListItem { Value = Converted, Text = "Đã chuyển cơ hội" },
        new SelectListItem { Value = Lost, Text = "Mất lead" }
    };
}

public static class SalesStageCatalog
{
    public const string LeadManagement = "LeadManagement";
    public const string NeedsAnalysis = "NeedsAnalysis";
    public const string TestDrive = "TestDrive";
    public const string Quote = "Quote";
    public const string Negotiation = "Negotiation";
    public const string Contract = "Contract";
    public const string Won = "Won";
    public const string Lost = "Lost";

    public static IReadOnlyList<SelectListItem> GetSelectList(string? selected = null) => SalesCatalogHelpers.Build(GetItems(), selected);

    public static string GetDisplayName(string? value) => SalesCatalogHelpers.Display(GetItems(), value, "Quản lý lead");

    public static string Normalize(string? value) => SalesCatalogHelpers.Normalize(GetItems(), value, LeadManagement);

    private static IReadOnlyList<SelectListItem> GetItems() => new[]
    {
        new SelectListItem { Value = LeadManagement, Text = "Quản lý lead" },
        new SelectListItem { Value = NeedsAnalysis, Text = "Phân tích nhu cầu" },
        new SelectListItem { Value = TestDrive, Text = "Lái thử" },
        new SelectListItem { Value = Quote, Text = "Báo giá" },
        new SelectListItem { Value = Negotiation, Text = "Đàm phán" },
        new SelectListItem { Value = Contract, Text = "Hợp đồng" },
        new SelectListItem { Value = Won, Text = "Thắng" },
        new SelectListItem { Value = Lost, Text = "Thua" }
    };
}

public static class TestDriveStatusCatalog
{
    public const string Scheduled = "Scheduled";
    public const string Completed = "Completed";
    public const string Cancelled = "Cancelled";
    public const string NoShow = "NoShow";

    public static IReadOnlyList<SelectListItem> GetSelectList(string? selected = null) => SalesCatalogHelpers.Build(GetItems(), selected);

    public static string GetDisplayName(string? value) => SalesCatalogHelpers.Display(GetItems(), value, "Đã đặt lịch");

    public static string Normalize(string? value) => SalesCatalogHelpers.Normalize(GetItems(), value, Scheduled);

    private static IReadOnlyList<SelectListItem> GetItems() => new[]
    {
        new SelectListItem { Value = Scheduled, Text = "Đã đặt lịch" },
        new SelectListItem { Value = Completed, Text = "Hoàn tất" },
        new SelectListItem { Value = Cancelled, Text = "Đã hủy" },
        new SelectListItem { Value = NoShow, Text = "Khách không đến" }
    };
}

public static class SalesQuoteStatusCatalog
{
    public const string Draft = "Draft";
    public const string Sent = "Sent";
    public const string Accepted = "Accepted";
    public const string Expired = "Expired";
    public const string Cancelled = "Cancelled";

    public static IReadOnlyList<SelectListItem> GetSelectList(string? selected = null) => SalesCatalogHelpers.Build(GetItems(), selected);

    public static string GetDisplayName(string? value) => SalesCatalogHelpers.Display(GetItems(), value, "Nháp");

    public static string Normalize(string? value) => SalesCatalogHelpers.Normalize(GetItems(), value, Draft);

    private static IReadOnlyList<SelectListItem> GetItems() => new[]
    {
        new SelectListItem { Value = Draft, Text = "Nháp" },
        new SelectListItem { Value = Sent, Text = "Đã gửi" },
        new SelectListItem { Value = Accepted, Text = "Khách đồng ý" },
        new SelectListItem { Value = Expired, Text = "Hết hạn" },
        new SelectListItem { Value = Cancelled, Text = "Đã hủy" }
    };
}

public static class SalesContractStatusCatalog
{
    public const string Draft = "Draft";
    public const string DepositPaid = "DepositPaid";
    public const string Signed = "Signed";
    public const string PendingRegistration = "PendingRegistration";
    public const string PendingDelivery = "PendingDelivery";
    public const string Delivered = "Delivered";
    public const string Cancelled = "Cancelled";
    public const string Voided = "Voided";

    public static IReadOnlyList<SelectListItem> GetSelectList(string? selected = null) => SalesCatalogHelpers.Build(GetItems(), selected);

    public static string GetDisplayName(string? value) => SalesCatalogHelpers.Display(GetItems(), value, "Nháp");

    public static string Normalize(string? value) => SalesCatalogHelpers.Normalize(GetItems(), value, Draft);

    public static bool LocksVehicle(string status) => status is not (Draft or Cancelled or Voided);

    private static IReadOnlyList<SelectListItem> GetItems() => new[]
    {
        new SelectListItem { Value = Draft, Text = "Nháp" },
        new SelectListItem { Value = DepositPaid, Text = "Đã đặt cọc" },
        new SelectListItem { Value = Signed, Text = "Đã ký" },
        new SelectListItem { Value = PendingRegistration, Text = "Chờ đăng ký" },
        new SelectListItem { Value = PendingDelivery, Text = "Chờ bàn giao" },
        new SelectListItem { Value = Delivered, Text = "Đã bàn giao" },
        new SelectListItem { Value = Cancelled, Text = "Đã hủy" },
        new SelectListItem { Value = Voided, Text = "Vô hiệu" }
    };
}

public static class SalesPaymentMethodCatalog
{
    public const string Cash = "Cash";
    public const string Installment = "Installment";

    public static IReadOnlyList<SelectListItem> GetSelectList(string? selected = null) => SalesCatalogHelpers.Build(GetItems(), selected);

    public static string GetDisplayName(string? value) => SalesCatalogHelpers.Display(GetItems(), value, "Tiền mặt");

    public static string Normalize(string? value) => SalesCatalogHelpers.Normalize(GetItems(), value, Cash);

    private static IReadOnlyList<SelectListItem> GetItems() => new[]
    {
        new SelectListItem { Value = Cash, Text = "Tiền mặt" },
        new SelectListItem { Value = Installment, Text = "Trả góp" }
    };
}

public static class RegistrationTaskTypeCatalog
{
    public const string RegistrationTax = "RegistrationTax";
    public const string PlateNumber = "PlateNumber";
    public const string Inspection = "Inspection";
    public const string Insurance = "Insurance";
    public const string Other = "Other";

    public static IReadOnlyList<SelectListItem> GetSelectList(string? selected = null) => SalesCatalogHelpers.Build(GetItems(), selected);

    public static string GetDisplayName(string? value) => SalesCatalogHelpers.Display(GetItems(), value, "Thuế trước bạ");

    public static string Normalize(string? value) => SalesCatalogHelpers.Normalize(GetItems(), value, RegistrationTax);

    private static IReadOnlyList<SelectListItem> GetItems() => new[]
    {
        new SelectListItem { Value = RegistrationTax, Text = "Thuế trước bạ" },
        new SelectListItem { Value = PlateNumber, Text = "Biển số" },
        new SelectListItem { Value = Inspection, Text = "Đăng kiểm" },
        new SelectListItem { Value = Insurance, Text = "Bảo hiểm" },
        new SelectListItem { Value = Other, Text = "Khác" }
    };
}

public static class RegistrationTaskStatusCatalog
{
    public const string Pending = "Pending";
    public const string Submitted = "Submitted";
    public const string Completed = "Completed";
    public const string Cancelled = "Cancelled";

    public static IReadOnlyList<SelectListItem> GetSelectList(string? selected = null) => SalesCatalogHelpers.Build(GetItems(), selected);

    public static string GetDisplayName(string? value) => SalesCatalogHelpers.Display(GetItems(), value, "Chờ xử lý");

    public static string Normalize(string? value) => SalesCatalogHelpers.Normalize(GetItems(), value, Pending);

    private static IReadOnlyList<SelectListItem> GetItems() => new[]
    {
        new SelectListItem { Value = Pending, Text = "Chờ xử lý" },
        new SelectListItem { Value = Submitted, Text = "Đã nộp" },
        new SelectListItem { Value = Completed, Text = "Hoàn tất" },
        new SelectListItem { Value = Cancelled, Text = "Đã hủy" }
    };
}

internal static class SalesCatalogHelpers
{
    public static IReadOnlyList<SelectListItem> Build(IReadOnlyList<SelectListItem> items, string? selected)
        => items.Select(item => new SelectListItem
        {
            Value = item.Value,
            Text = item.Text,
            Selected = string.Equals(item.Value, selected, StringComparison.OrdinalIgnoreCase)
        }).ToArray();

    public static string Display(IReadOnlyList<SelectListItem> items, string? value, string fallback)
        => items.FirstOrDefault(item => string.Equals(item.Value, value, StringComparison.OrdinalIgnoreCase))?.Text ?? fallback;

    public static string Normalize(IReadOnlyList<SelectListItem> items, string? value, string fallback)
        => items.Any(item => string.Equals(item.Value, value, StringComparison.OrdinalIgnoreCase)) ? value!.Trim() : fallback;
}
