namespace Presentation.Extensions;

public static class StringExtensions
{
    public static string? TrimOrNull(this string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
