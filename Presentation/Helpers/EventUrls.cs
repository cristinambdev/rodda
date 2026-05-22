using Domain.Models;

namespace Presentation.Helpers;

// ************************************************************************************************
// Canonical public paths for event pages (slug when available, else id).
public static class EventUrls
{
    public static string DetailsSegment(string? slug, string eventId) =>
        string.IsNullOrWhiteSpace(slug) ? eventId : slug.Trim();

    public static string DetailsPath(Event ev) => $"/events/{DetailsSegment(ev.Slug, ev.Id)}";

    public static string DetailsPath(string? slug, string eventId) =>
        $"/events/{DetailsSegment(slug, eventId)}";
}
