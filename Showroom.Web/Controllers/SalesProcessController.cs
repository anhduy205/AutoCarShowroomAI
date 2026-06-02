using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Showroom.Web.Extensions;
using Showroom.Web.Models;
using Showroom.Web.Security;
using Showroom.Web.Services;

namespace Showroom.Web.Controllers;

[Authorize(Policy = ShowroomPolicies.SalesManager)]
[Route("admin/sales-process")]
public sealed class SalesProcessController : Controller
{
    private readonly IAuditLogService _auditLogService;
    private readonly ISalesProcessService _salesProcessService;

    public SalesProcessController(ISalesProcessService salesProcessService, IAuditLogService auditLogService)
    {
        _salesProcessService = salesProcessService;
        _auditLogService = auditLogService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        try
        {
            var model = await _salesProcessService.GetDashboardAsync(cancellationToken);
            return View(model);
        }
        catch (InvalidOperationException ex)
        {
            SetStatus(ex.Message, "warning");
            return View(new SalesProcessDashboardViewModel());
        }
    }

    [HttpPost("leads")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> CreateLead([Bind(Prefix = "LeadForm")] LeadFormViewModel model, CancellationToken cancellationToken)
        => RunActionAsync(
            ModelState.IsValid,
            () => _salesProcessService.CreateLeadAsync(model, cancellationToken),
            "LEAD_CREATED",
            "Lead",
            $"Đã tạo lead '{model.ContactName}'.",
            "Đã tạo lead.",
            "Thông tin lead chưa hợp lệ.",
            cancellationToken);

    [HttpPost("opportunities")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> CreateOpportunity([Bind(Prefix = "OpportunityForm")] SalesOpportunityFormViewModel model, CancellationToken cancellationToken)
        => RunActionAsync(
            ModelState.IsValid,
            () => _salesProcessService.CreateOpportunityAsync(model, cancellationToken),
            "SALES_OPPORTUNITY_CREATED",
            "SalesOpportunity",
            $"Đã tạo cơ hội '{model.OpportunityName}'.",
            "Đã tạo cơ hội bán hàng.",
            "Thông tin cơ hội chưa hợp lệ.",
            cancellationToken);

    [HttpPost("test-drives")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> ScheduleTestDrive([Bind(Prefix = "TestDriveForm")] TestDriveFormViewModel model, CancellationToken cancellationToken)
        => RunActionAsync(
            ModelState.IsValid,
            () => _salesProcessService.ScheduleTestDriveAsync(model, cancellationToken),
            "TEST_DRIVE_SCHEDULED",
            "TestDrive",
            $"Đã đặt lịch lái thử cho cơ hội {model.OpportunityId}.",
            "Đã đặt lịch lái thử.",
            "Thông tin lịch lái thử chưa hợp lệ.",
            cancellationToken);

    [HttpPost("quotes")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> CreateQuote([Bind(Prefix = "QuoteForm")] SalesQuoteFormViewModel model, CancellationToken cancellationToken)
        => RunActionAsync(
            ModelState.IsValid,
            () => _salesProcessService.CreateQuoteAsync(model, cancellationToken),
            "SALES_QUOTE_CREATED",
            "SalesQuote",
            $"Đã tạo báo giá '{model.QuoteNo}'.",
            "Đã tạo báo giá.",
            "Thông tin báo giá chưa hợp lệ.",
            cancellationToken);

    [HttpPost("contracts")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> CreateContract([Bind(Prefix = "ContractForm")] SalesContractFormViewModel model, CancellationToken cancellationToken)
        => RunActionAsync(
            ModelState.IsValid,
            () => _salesProcessService.CreateContractAsync(model, cancellationToken),
            "SALES_CONTRACT_CREATED",
            "SalesContract",
            $"Đã tạo hợp đồng '{model.ContractNo}'.",
            "Đã tạo hợp đồng và xử lý khóa VIN nếu cần.",
            "Thông tin hợp đồng chưa hợp lệ.",
            cancellationToken);

    [HttpPost("accessories")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> AddAccessory([Bind(Prefix = "AccessoryForm")] ContractAccessoryFormViewModel model, CancellationToken cancellationToken)
        => RunActionAsync(
            ModelState.IsValid,
            () => _salesProcessService.AddAccessoryAsync(model, cancellationToken),
            "CONTRACT_ACCESSORY_ADDED",
            "ContractAccessory",
            $"Đã thêm phụ kiện '{model.AccessoryName}'.",
            "Đã thêm phụ kiện hợp đồng.",
            "Thông tin phụ kiện chưa hợp lệ.",
            cancellationToken);

    [HttpPost("registration-tasks")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> CreateRegistrationTask([Bind(Prefix = "RegistrationTaskForm")] RegistrationTaskFormViewModel model, CancellationToken cancellationToken)
        => RunActionAsync(
            ModelState.IsValid,
            () => _salesProcessService.CreateRegistrationTaskAsync(model, cancellationToken),
            "REGISTRATION_TASK_CREATED",
            "RegistrationTask",
            $"Đã tạo thủ tục đăng ký cho hợp đồng {model.SalesContractId}.",
            "Đã tạo thủ tục đăng ký.",
            "Thông tin thủ tục chưa hợp lệ.",
            cancellationToken);

    [HttpPost("deliveries")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> CreateDelivery([Bind(Prefix = "DeliveryForm")] VehicleDeliveryFormViewModel model, CancellationToken cancellationToken)
        => RunActionAsync(
            ModelState.IsValid,
            () => _salesProcessService.CreateDeliveryAsync(model, cancellationToken),
            "VEHICLE_DELIVERY_CREATED",
            "VehicleDelivery",
            $"Đã tạo phiếu bàn giao cho hợp đồng {model.SalesContractId}.",
            "Đã tạo phiếu bàn giao.",
            "Thông tin bàn giao chưa hợp lệ.",
            cancellationToken);

    [HttpPost("delivery-checklist")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> AddChecklistItem([Bind(Prefix = "ChecklistItemForm")] DeliveryChecklistItemFormViewModel model, CancellationToken cancellationToken)
        => RunActionAsync(
            ModelState.IsValid,
            () => _salesProcessService.AddChecklistItemAsync(model, cancellationToken),
            "DELIVERY_CHECKLIST_ADDED",
            "DeliveryChecklistItem",
            $"Đã thêm checklist '{model.ChecklistName}'.",
            "Đã thêm checklist bàn giao.",
            "Thông tin checklist chưa hợp lệ.",
            cancellationToken);

    private async Task<IActionResult> RunActionAsync(
        bool isValid,
        Func<Task> action,
        string auditAction,
        string entityType,
        string auditDescription,
        string successMessage,
        string invalidMessage,
        CancellationToken cancellationToken)
    {
        if (!isValid)
        {
            SetStatus(invalidMessage, "warning");
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await action();
            await WriteAuditAsync(auditAction, entityType, auditDescription, cancellationToken);
            SetStatus(successMessage, "success");
        }
        catch (InvalidOperationException ex)
        {
            SetStatus(ex.Message, "warning");
        }

        return RedirectToAction(nameof(Index));
    }

    private void SetStatus(string message, string type)
    {
        TempData["StatusMessage"] = message;
        TempData["StatusType"] = type;
    }

    private Task WriteAuditAsync(string action, string entityType, string description, CancellationToken cancellationToken)
        => _auditLogService.WriteAsync(
            new AuditLogEntry
            {
                Username = User.GetUsername(),
                DisplayName = User.GetDisplayName(),
                Role = User.GetPrimaryRole(),
                Action = action,
                EntityType = entityType,
                Description = description,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty
            },
            cancellationToken);
}
