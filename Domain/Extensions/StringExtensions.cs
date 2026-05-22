using System.Text.RegularExpressions;

namespace Domain.Extensions;

public static class StringExtensions
{
    private const int DefaultMaxSlugLength = 160;

    // ************************************************************************************************
    // URL-safe slug from a title or phrase (e.g. "Midsommarfest 2026" → "midsommarfest-2026").
    public static string? ToSlug(this string? phrase, int maxLength = DefaultMaxSlugLength)
    {
        if (string.IsNullOrWhiteSpace(phrase))
            return null;

        var str = phrase.Trim().ToLowerInvariant();
        str = str
            .Replace("å", "a")
            .Replace("ä", "a")
            .Replace("ö", "o")
            .Replace("æ", "ae")
            .Replace("ø", "o");

        str = Regex.Replace(str, @"[^a-z0-9\s-]", "");
        str = Regex.Replace(str, @"\s+", " ").Trim();
        str = Regex.Replace(str, @"\s", "-");
        str = Regex.Replace(str, @"-+", "-").Trim('-');

        if (string.IsNullOrEmpty(str))
            return null;

        if (str.Length <= maxLength)
            return str;

        var truncated = str[..maxLength].TrimEnd('-');
        return string.IsNullOrEmpty(truncated) ? null : truncated;
    }
}
