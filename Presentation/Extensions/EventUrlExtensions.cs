using Domain.Models;

namespace Presentation.Extensions;

// Canonical URL paths for event detail pages (slug preferred, id fallback). Route template:
// `/events/{id}` on `EventDetails`. Used by event cards, `HomeTodosHelper`, and `EventsController` redirects.
public static class EventUrlExtensions
{
    // Path segment for one event: trimmed slug when set, otherwise guid/id.
    public static string DetailsSegment(this string eventId, string? slug) =>
        string.IsNullOrWhiteSpace(slug) ? eventId : slug.Trim();

    public static string DetailsSegment(this Event ev) =>
        ev.Id.DetailsSegment(ev.Slug);

    // `/events/{segment}` when slug and id are separate (view models, share links).
    public static string DetailsPath(this string eventId, string? slug) =>
        $"/events/{eventId.DetailsSegment(slug)}";

    public static string DetailsPath(this Event ev) =>
        ev.Id.DetailsPath(ev.Slug);
}
