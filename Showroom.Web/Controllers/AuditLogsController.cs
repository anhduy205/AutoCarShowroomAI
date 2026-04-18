using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Showroom.Web.Models;
using Showroom.Web.Security;
using Showroom.Web.Services;

namespace Showroom.Web.Controllers;

[Authorize(Policy = ShowroomPolicies.AuditViewer)]
public class AuditLogsController : Controller
{
    private readonly IAuditLogService _auditLogService;

    public AuditLogsController(IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        try
        {
            var logs = await _auditLogService.GetRecentLogsAsync(cancellationToken);
            return View(logs);
        }
        catch (InvalidOperationException ex)
        {
            ViewData["StatusMessage"] = ex.Message;
            ViewData["StatusType"] = "warning";
            return View(Array.Empty<AuditLogListItemViewModel>());
        }
    }
}
