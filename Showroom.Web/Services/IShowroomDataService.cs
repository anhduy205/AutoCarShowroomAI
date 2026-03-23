using Showroom.Web.Models;

namespace Showroom.Web.Services;

public interface IShowroomDataService
{
    Task<AdminDashboardViewModel> GetDashboardAsync(CancellationToken cancellationToken = default);
}
