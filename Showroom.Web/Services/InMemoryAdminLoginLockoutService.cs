using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Showroom.Web.Configuration;

namespace Showroom.Web.Services;

public class InMemoryAdminLoginLockoutService : IAdminLoginLockoutService
{
    private readonly IMemoryCache _memoryCache;
    private readonly AdminLoginProtectionOptions _options;

    public InMemoryAdminLoginLockoutService(
        IMemoryCache memoryCache,
        IOptions<AdminLoginProtectionOptions> options)
    {
        _memoryCache = memoryCache;
        _options = options.Value;
    }

    public AdminLoginAttemptStatus GetStatus(string username, string ipAddress)
    {
        var now = DateTimeOffset.UtcNow;
        var key = BuildCacheKey(username, ipAddress);
        if (!_memoryCache.TryGetValue<LoginAttemptState>(key, out var state) || state is null)
        {
            return CreateStatus(0, isLockedOut: false, retryAfter: null);
        }

        if (TryResetExpiredWindow(key, state, now))
        {
            return CreateStatus(0, isLockedOut: false, retryAfter: null);
        }

        if (state.LockedUntilUtc is not null && state.LockedUntilUtc > now)
        {
            return CreateStatus(state.FailedAttempts, isLockedOut: true, state.LockedUntilUtc - now);
        }

        return CreateStatus(state.FailedAttempts, isLockedOut: false, retryAfter: null);
    }

    public AdminLoginAttemptStatus RegisterFailure(string username, string ipAddress)
    {
        var now = DateTimeOffset.UtcNow;
        var key = BuildCacheKey(username, ipAddress);

        if (!_memoryCache.TryGetValue<LoginAttemptState>(key, out var state) || state is null || TryResetExpiredWindow(key, state, now))
        {
            state = new LoginAttemptState
            {
                FailedAttempts = 0,
                WindowStartedUtc = now
            };
        }

        state.FailedAttempts++;

        if (state.FailedAttempts >= _options.MaxFailedAttempts)
        {
            state.LockedUntilUtc = now.AddMinutes(Math.Max(1, _options.LockoutMinutes));
        }

        _memoryCache.Set(key, state, BuildCacheEntryOptions(now, state.LockedUntilUtc));

        var retryAfter = state.LockedUntilUtc is not null && state.LockedUntilUtc > now
            ? state.LockedUntilUtc - now
            : null;

        return CreateStatus(
            state.FailedAttempts,
            isLockedOut: retryAfter is not null,
            retryAfter: retryAfter);
    }

    public void Reset(string username, string ipAddress)
        => _memoryCache.Remove(BuildCacheKey(username, ipAddress));

    private bool TryResetExpiredWindow(string key, LoginAttemptState state, DateTimeOffset now)
    {
        if (state.LockedUntilUtc is not null && state.LockedUntilUtc > now)
        {
            return false;
        }

        var window = TimeSpan.FromMinutes(Math.Max(1, _options.FailedAttemptWindowMinutes));
        if (now - state.WindowStartedUtc < window)
        {
            return false;
        }

        _memoryCache.Remove(key);
        return true;
    }

    private MemoryCacheEntryOptions BuildCacheEntryOptions(DateTimeOffset now, DateTimeOffset? lockedUntilUtc)
    {
        var absoluteExpiration = now.AddMinutes(Math.Max(1, _options.FailedAttemptWindowMinutes));
        if (lockedUntilUtc is not null && lockedUntilUtc > absoluteExpiration)
        {
            absoluteExpiration = lockedUntilUtc.Value;
        }

        return new MemoryCacheEntryOptions
        {
            AbsoluteExpiration = absoluteExpiration.AddMinutes(1)
        };
    }

    private AdminLoginAttemptStatus CreateStatus(int failedAttempts, bool isLockedOut, TimeSpan? retryAfter)
        => new()
        {
            FailedAttempts = failedAttempts,
            MaxFailedAttempts = Math.Max(1, _options.MaxFailedAttempts),
            IsLockedOut = isLockedOut,
            RetryAfter = retryAfter
        };

    private static string BuildCacheKey(string username, string ipAddress)
    {
        var normalizedUsername = string.IsNullOrWhiteSpace(username)
            ? "anonymous"
            : username.Trim().ToUpperInvariant();
        var normalizedIpAddress = string.IsNullOrWhiteSpace(ipAddress)
            ? "unknown"
            : ipAddress.Trim();

        return $"admin-login:{normalizedUsername}:{normalizedIpAddress}";
    }

    private sealed class LoginAttemptState
    {
        public int FailedAttempts { get; set; }

        public DateTimeOffset WindowStartedUtc { get; set; }

        public DateTimeOffset? LockedUntilUtc { get; set; }
    }
}
