using System.Security.Cryptography;

namespace Showroom.Web.Security;

public static class PasswordHashing
{
    private const string Sha256Prefix = "pbkdf2-sha256";
    private const string LegacySha1Prefix = "pbkdf2-sha1";
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int DefaultIterations = 210_000;

    public static string HashPassword(string password, int iterations = DefaultIterations)
        => HashPassword(password, iterations, Sha256Prefix, HashAlgorithmName.SHA256);

    public static bool VerifyPassword(string password, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            return false;
        }

        var parts = passwordHash.Split('$', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 4)
        {
            return false;
        }

        var prefix = parts[0];
        if (!TryResolveAlgorithm(prefix, out var algorithm))
        {
            return false;
        }

        if (!int.TryParse(parts[1], out var iterations) || iterations <= 0)
        {
            return false;
        }

        try
        {
            var salt = Convert.FromBase64String(parts[2]);
            var expectedHash = Convert.FromBase64String(parts[3]);

            using var deriveBytes = new Rfc2898DeriveBytes(password, salt, iterations, algorithm);
            var actualHash = deriveBytes.GetBytes(expectedHash.Length);
            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static string HashPassword(string password, int iterations, string prefix, HashAlgorithmName algorithm)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        using var deriveBytes = new Rfc2898DeriveBytes(password, salt, iterations, algorithm);
        var hash = deriveBytes.GetBytes(KeySize);

        return string.Create(
            System.Globalization.CultureInfo.InvariantCulture,
            $"{prefix}${iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}");
    }

    private static bool TryResolveAlgorithm(string prefix, out HashAlgorithmName algorithm)
    {
        if (string.Equals(prefix, Sha256Prefix, StringComparison.OrdinalIgnoreCase))
        {
            algorithm = HashAlgorithmName.SHA256;
            return true;
        }

        if (string.Equals(prefix, LegacySha1Prefix, StringComparison.OrdinalIgnoreCase))
        {
            algorithm = HashAlgorithmName.SHA1;
            return true;
        }

        algorithm = default;
        return false;
    }
}
