using System.Text;

namespace Showroom.Web.Security;

public static class InputSanitizer
{
    public static string? SanitizeQuery(string? input, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        var trimmed = input.Trim();
        if (trimmed.Length > maxLength)
        {
            trimmed = trimmed[..maxLength];
        }

        var sb = new StringBuilder(trimmed.Length);
        foreach (var c in trimmed)
        {
            if (char.IsControl(c) && c != '\t' && c != '\n' && c != '\r')
            {
                continue;
            }

            sb.Append(c);
        }

        return sb
            .ToString()
            .Replace('\r', ' ')
            .Replace('\n', ' ')
            .Replace('\t', ' ')
            .Trim();
    }

    public static string SanitizePreview(string? input, int maxLength)
        => SanitizeQuery(input, maxLength) ?? string.Empty;

    public static int? ClampYear(int? year)
    {
        if (year is null)
        {
            return null;
        }

        return year is >= 1900 and <= 2100 ? year : null;
    }
}

