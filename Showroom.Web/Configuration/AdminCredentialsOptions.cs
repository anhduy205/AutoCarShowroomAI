using Showroom.Web.Security;

namespace Showroom.Web.Configuration;

public class AdminCredentialsOptions
{
    public const string SectionName = "AdminCredentials";

    // Legacy single-account fields are kept for backward compatibility.
    public string Username { get; set; } = "admin";

    public string Password { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string DisplayName { get; set; } = "Quan tri vien he thong";

    public string Role { get; set; } = ShowroomRoles.Administrator;

    public List<ConfiguredAccountOptions> Accounts { get; set; } = new();

    public IReadOnlyList<ConfiguredAccountOptions> GetAccounts()
    {
        if (Accounts.Count > 0)
        {
            return Accounts
                .Where(account => !string.IsNullOrWhiteSpace(account.Username))
                .Where(account => account.HasPasswordConfigured)
                .ToList();
        }

        if (string.IsNullOrWhiteSpace(Username))
        {
            return Array.Empty<ConfiguredAccountOptions>();
        }

        var legacyAccount = new ConfiguredAccountOptions
        {
            Username = Username,
            Password = Password,
            PasswordHash = PasswordHash,
            DisplayName = DisplayName,
            Role = Role
        };

        return legacyAccount.HasPasswordConfigured
            ? new[] { legacyAccount }
            : Array.Empty<ConfiguredAccountOptions>();
    }
}

public class ConfiguredAccountOptions
{
    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string Role { get; set; } = ShowroomRoles.Staff;

    public bool HasPasswordConfigured =>
        !string.IsNullOrWhiteSpace(PasswordHash) || !string.IsNullOrWhiteSpace(Password);

    public string NormalizedRole =>
        string.Equals(Role, ShowroomRoles.Administrator, StringComparison.OrdinalIgnoreCase)
            ? ShowroomRoles.Administrator
            : ShowroomRoles.Staff;
}
