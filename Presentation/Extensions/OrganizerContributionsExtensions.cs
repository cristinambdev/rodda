using Domain.Enums;
using Presentation.Models;

namespace Presentation.Extensions;

// String and assignment helpers for `EventOrganizerContributionsHelper` (guests & contributions table).
public static class OrganizerContributionsExtensions
{
    // Uppercase display title for the contributions table (not home todos title case).
    public static string FormatContributionTitle(this string? title) =>
        (title ?? "").Trim().ToUpperInvariant();

    public static string UserKey(this string userId) => $"user:{userId}";

    public static string SlotKey(this string? placeholder) =>
        $"slot:{(placeholder ?? "").Trim().ToUpperInvariant()}";

    // Strips “(host)” before deduplicating person names in the organizer table.
    public static string NormalizePersonName(this string name) =>
        name.Replace("(host)", "", StringComparison.OrdinalIgnoreCase).Trim();

    public static bool TitlesMatch(this string a, string b) =>
        string.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase);

    // Stable dictionary key for grouping assignments onto one table row.
    public static string OrganizerBucketKey(this EventAssignmentSlotViewModel assignment) =>
        assignment.AssigneeType switch
        {
            AssigneeType.OpenSlot => assignment.PlaceholderLabel.SlotKey(),
            _ => (assignment.UserId ?? assignment.Id).UserKey(),
        };
}
