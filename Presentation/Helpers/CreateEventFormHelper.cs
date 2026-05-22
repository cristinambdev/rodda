using System.Globalization;
using Domain.Models;
using Presentation.Models;

namespace Presentation.Helpers;

// ************************************************************************************************
// Maps domain events to the shared create/edit event form model.
public static class CreateEventFormHelper
{
    // **************************************************************************************************************************
    // Maps a domain event to the create/edit event form model.
    public static AddEventViewModel FromEvent(Event ev)
    {
        var localStart = ev.StartAt.ToLocalTime();
        var location = ev.Location;
        var payment = ev.Payment;

        var viewModel = new AddEventViewModel
        {
            EventId = ev.Id,
            Title = ev.Title,
            Slug = ev.Slug,
            CoverImageUrl = ev.CoverImageUrl,
            Description = ev.Description,
            Date = localStart.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            Time = localStart.ToString("HH:mm", CultureInfo.InvariantCulture),
            StartAt = ev.StartAt,
            EndAt = ev.EndAt,
            Timezone = string.IsNullOrWhiteSpace(ev.Timezone) ? "Europe/Stockholm" : ev.Timezone,
            Location = ResolveLocationDisplay(location),
            LocationName = location?.Name,
            LocationStreet = location?.Street,
            LocationPostcode = location?.Postcode,
            LocationCity = location?.City,
            LocationCountry = location?.Country,
            JoinButton = ev.JoinMode != Domain.Enums.JoinMode.Disabled,
            JoinMode = ev.JoinMode,
            Status = ev.Status,
            ChatEnabled = ev.ChatEnabled,
            ItemsTasksEnabled = ev.ItemsTasksEnabled,
            AllowGuestBringItems = ev.AllowGuestBringItems,
            AllowGuestTasks = ev.AllowGuestTasks,
            PaymentMethod = payment?.Method,
            PaymentNumber = payment?.Number,
            PaymentName = payment?.Name,
            PaymentAmount = FormatPaymentAmount(payment?.Amount),
            PaymentComment = payment?.Comment,
        };

        return viewModel;
    }

    // ************************************************************************************************
    // Resolves a location display string from an address object (e.g. "Stockholm", "123 Main St, 12345, New York, USA").
    private static string? ResolveLocationDisplay(Address? location)
    {
        if (location == null)
            return null;

        if (!string.IsNullOrWhiteSpace(location.Name))
            return location.Name.Trim();

        var parts = new[] { location.Street, location.Postcode, location.City, location.Country }
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p!.Trim())
            .ToArray();

        return parts.Length == 0 ? null : string.Join(", ", parts);
    }

    // ************************************************************************************************
    // Parses free-text payment amounts from the create/edit form (e.g. "50kr", "12,50").
    public static decimal? ParsePaymentAmount(string? amountRaw)
    {
        if (string.IsNullOrWhiteSpace(amountRaw))
            return null;

        var normalized = amountRaw.Trim();
        foreach (var token in new[] { "kr", "sek", ":-" })
            normalized = normalized.Replace(token, "", StringComparison.OrdinalIgnoreCase);

        normalized = normalized.Replace(" ", "", StringComparison.Ordinal)
            .Replace("\u00a0", "", StringComparison.Ordinal)
            .Trim();

        if (decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
            return amount;

        var swedish = CultureInfo.GetCultureInfo("sv-SE");
        if (decimal.TryParse(normalized, NumberStyles.Number, swedish, out amount))
            return amount;

        if (decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.CurrentCulture, out amount))
            return amount;

        return null;
    }

    // ************************************************************************************************
    // Formats payment amounts for display (e.g. "50kr", "12,50").
    private static string? FormatPaymentAmount(decimal? amount)
    {
        if (!amount.HasValue)
            return null;

        var value = amount.Value;
        if (value == decimal.Truncate(value))
            return $"{value:0}kr";

        return value.ToString(CultureInfo.InvariantCulture);
    }
}
