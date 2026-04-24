namespace Showroom.Web.Configuration;

public class AdminLoginProtectionOptions
{
    public const string SectionName = "AdminLoginProtection";
    public const string RateLimitPolicyName = "AdminLogin";

    public int MaxFailedAttempts { get; set; } = 5;

    public int FailedAttemptWindowMinutes { get; set; } = 15;

    public int LockoutMinutes { get; set; } = 10;

    public int LoginRequestsPerMinute { get; set; } = 15;
}
