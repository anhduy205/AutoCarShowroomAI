namespace Showroom.Web.Models;

public sealed class BranchListItemViewModel
{
    public int Id { get; init; }

    public string BranchCode { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string Status { get; init; } = BranchStatusCatalog.Active;

    public DateTime CreatedAt { get; init; }

    public int StaffCount { get; init; }

    public string StatusLabel => BranchStatusCatalog.GetDisplayName(Status);
}
