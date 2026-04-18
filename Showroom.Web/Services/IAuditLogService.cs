using Showroom.Web.Models;

namespace Showroom.Web.Services;

public interface IAuditLogService
{
    Task WriteAsync(AuditLogEntry entry, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AuditLogListItemViewModel>> GetRecentLogsAsync(CancellationToken cancellationToken = default);
}
