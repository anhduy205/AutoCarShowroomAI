using Microsoft.AspNetCore.Mvc.Rendering;
using Showroom.Web.Models;

namespace Showroom.Web.Services;

public interface ICoreManagementService
{
    Task<IReadOnlyList<BranchListItemViewModel>> GetBranchesAsync(CancellationToken cancellationToken = default);

    Task<BranchFormViewModel?> GetBranchAsync(int id, CancellationToken cancellationToken = default);

    Task CreateBranchAsync(BranchFormViewModel model, CancellationToken cancellationToken = default);

    Task<bool> UpdateBranchAsync(BranchFormViewModel model, CancellationToken cancellationToken = default);

    Task<bool> DeleteBranchAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SelectListItem>> GetBranchOptionsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CustomerListItemViewModel>> GetCustomersAsync(CancellationToken cancellationToken = default);

    Task<CustomerFormViewModel?> GetCustomerAsync(int id, CancellationToken cancellationToken = default);

    Task CreateCustomerAsync(CustomerFormViewModel model, CancellationToken cancellationToken = default);

    Task<bool> UpdateCustomerAsync(CustomerFormViewModel model, CancellationToken cancellationToken = default);

    Task<bool> DeleteCustomerAsync(int id, CancellationToken cancellationToken = default);
}
