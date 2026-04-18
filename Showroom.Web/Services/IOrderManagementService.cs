using Showroom.Web.Models;

namespace Showroom.Web.Services;

public interface IOrderManagementService
{
    Task<IReadOnlyList<OrderListItemViewModel>> GetOrdersAsync(CancellationToken cancellationToken = default);

    Task<OrderFormViewModel> GetNewOrderAsync(CancellationToken cancellationToken = default);

    Task<OrderFormViewModel?> GetOrderAsync(int id, CancellationToken cancellationToken = default);

    Task PopulateCarOptionsAsync(OrderFormViewModel model, CancellationToken cancellationToken = default);

    Task<int> CreateOrderAsync(OrderFormViewModel model, CancellationToken cancellationToken = default);

    Task<bool> UpdateOrderAsync(OrderFormViewModel model, CancellationToken cancellationToken = default);

    Task<bool> DeleteOrderAsync(int id, CancellationToken cancellationToken = default);
}
