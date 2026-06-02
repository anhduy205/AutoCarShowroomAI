using Showroom.Web.Models;

namespace Showroom.Web.Services;

public interface IStaffUserManagementService
{
    Task<IReadOnlyList<StaffUser>> GetStaffUsersAsync(CancellationToken cancellationToken = default);

    Task<StaffUser?> GetStaffUserAsync(int id, CancellationToken cancellationToken = default);

    Task<StaffUser?> FindByUsernameAsync(string username, CancellationToken cancellationToken = default);

    Task<int> CreateStaffUserAsync(StaffUserCreateRequest request, CancellationToken cancellationToken = default);

    Task<bool> UpdateStaffUserAsync(StaffUserUpdateRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteStaffUserAsync(int id, CancellationToken cancellationToken = default);
}

public sealed class StaffUserCreateRequest
{
    public string Username { get; init; } = string.Empty;

    public string PasswordHash { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public string Role { get; init; } = "Staff";

    public int? BranchId { get; init; }

    public string? StaffCode { get; init; }

    public string? Email { get; init; }

    public string? Phone { get; init; }

    public bool IsActive { get; init; } = true;
}

public sealed class StaffUserUpdateRequest
{
    public int Id { get; init; }

    public string Username { get; init; } = string.Empty;

    public string? PasswordHash { get; init; }

    public string DisplayName { get; init; } = string.Empty;

    public string Role { get; init; } = "Staff";

    public int? BranchId { get; init; }

    public string? StaffCode { get; init; }

    public string? Email { get; init; }

    public string? Phone { get; init; }

    public bool IsActive { get; init; } = true;
}
