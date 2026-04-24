using Showroom.Web.Models;

namespace Showroom.Web.Services;

public interface IShowroomDataService
{
    Task<AdminDashboardViewModel> GetDashboardAsync(
        DateOnly? salesFrom = null,
        DateOnly? salesTo = null,
        CancellationToken cancellationToken = default);
}
