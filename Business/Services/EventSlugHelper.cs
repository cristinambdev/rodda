using Data.Entities;
using Data.Repositories;
using Domain.Extensions;

namespace Business.Services;

internal static class EventSlugHelper
{
    private const int MaxSlugLength = 160;

    // ************************************************************************************************
    // Assigns a unique slug to an event.
    public static async Task<string> AssignUniqueSlugAsync( IEventRepository repository, EventEntity entity, string? requestedSlug)
    {
        var baseSlug = requestedSlug.ToSlug()
            ?? entity.Title.ToSlug()
            ?? entity.Id.ToSlug(maxLength: 36)
            ?? entity.Id;

        return await EnsureUniqueAsync(repository, baseSlug, entity.Id);
    }

    // ************************************************************************************************
    // Ensures a slug is unique.
    private static async Task<string> EnsureUniqueAsync( IEventRepository repository, string baseSlug, string eventId)
    {
        var candidate = baseSlug;
        var suffix = 2;

        while (await SlugTakenAsync(repository, candidate, eventId))
        {
            var suffixText = $"-{suffix}";
            var maxBaseLength = Math.Max(1, MaxSlugLength - suffixText.Length);
            var trimmedBase = baseSlug.Length > maxBaseLength
                ? baseSlug[..maxBaseLength].TrimEnd('-')
                : baseSlug;

            if (string.IsNullOrEmpty(trimmedBase))
                trimmedBase = eventId[..Math.Min(eventId.Length, maxBaseLength)];

            candidate = $"{trimmedBase}{suffixText}";
            suffix++;
        }

        return candidate;
    }

    // ************************************************************************************************
    // Checks if a slug is taken.
    private static async Task<bool> SlugTakenAsync( IEventRepository repository, string slug, string excludeEventId)
    {
        var response = await repository.GetEntityAsync(e => e.Slug == slug && e.Id != excludeEventId);
        return response.Succeeded && response.Result != null;
    }
}
