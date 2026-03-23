namespace Showroom.Web.Configuration;

public class AdminCredentialsOptions
{
    public const string SectionName = "AdminCredentials";

    public string Username { get; set; } = "admin";

    public string Password { get; set; } = "Admin@123";

    public string DisplayName { get; set; } = "Quản trị viên hệ thống";
}
