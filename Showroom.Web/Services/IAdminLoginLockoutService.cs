namespace Showroom.Web.Services;

public interface IAdminLoginLockoutService
{
    AdminLoginAttemptStatus GetStatus(string username, string ipAddress);

    AdminLoginAttemptStatus RegisterFailure(string username, string ipAddress);

    void Reset(string username, string ipAddress);
}

public sealed class AdminLoginAttemptStatus
{
    public int FailedAttempts { get; init; }

    public int MaxFailedAttempts { get; init; }

    public bool IsLockedOut { get; init; }

    public TimeSpan? RetryAfter { get; init; }
}
