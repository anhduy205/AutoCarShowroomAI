namespace Showroom.Web.Models;

public sealed class CustomerListItemViewModel
{
    public int Id { get; init; }

    public string CustomerCode { get; init; } = string.Empty;

    public string FullName { get; init; } = string.Empty;

    public string? Phone { get; init; }

    public string? Email { get; init; }

    public string? TaxCode { get; init; }

    public string? Address { get; init; }

    public string CustomerType { get; init; } = CustomerTypeCatalog.Individual;

    public DateTime CreatedAt { get; init; }

    public string CustomerTypeLabel => CustomerTypeCatalog.GetDisplayName(CustomerType);
}
