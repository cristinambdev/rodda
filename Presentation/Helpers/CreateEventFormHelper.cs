using System.Globalization;
using Domain.Enums;
using Domain.Models;
using Microsoft.AspNetCore.Http;
using Presentation.Extensions;
using Presentation.Models;

namespace Presentation.Helpers;

// ************************************************************************************************
// Maps domain events to the shared create/edit event form model.
public static class CreateEventFormHelper
{
    // **************************************************************************************************************************
    // Maps a domain event to the create/edit event form model.
    public static AddEventViewModel FromEvent(Event eventData)
    {
        var localStart = eventData.StartAt.ToLocalTime();
        var location = eventData.Location;
        var payment = eventData.Payment;

        var viewModel = new AddEventViewModel
        {
            EventId = eventData.Id,
            Title = eventData.Title,
            Slug = eventData.Slug,
            CoverImageUrl = eventData.CoverImageUrl,
            Description = eventData.Description,
            Date = localStart.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            Time = localStart.ToString("HH:mm", CultureInfo.InvariantCulture),
            StartAt = eventData.StartAt,
            EndAt = eventData.EndAt,
            Timezone = string.IsNullOrWhiteSpace(eventData.Timezone) ? "Europe/Stockholm" : eventData.Timezone,
            Location = ResolveLocationDisplay(location),
            LocationName = location?.Name,
            LocationStreet = location?.Street,
            LocationPostcode = location?.Postcode,
            LocationCity = location?.City,
            LocationCountry = location?.Country,
            JoinButton = eventData.JoinMode != Domain.Enums.JoinMode.Disabled,
            JoinMode = eventData.JoinMode,
            Status = eventData.Status,
            ChatEnabled = eventData.ChatEnabled,
            ItemsTasksEnabled = eventData.ItemsTasksEnabled,
            AllowGuestBringItems = eventData.AllowGuestBringItems,
            AllowGuestTasks = eventData.AllowGuestTasks,
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

    // ************************************************************************************************
    // Normalizes create/edit form fields from posted values (dates, location, join toggles, payment).
    public static void ApplyFormFields(AddEventViewModel model, IFormCollection form)
    {
        if (!string.IsNullOrWhiteSpace(model.Date))
        {
            var timePart = string.IsNullOrWhiteSpace(model.Time) ? "00:00" : model.Time.Trim();
            if (DateTime.TryParse(
                    $"{model.Date.Trim()} {timePart}",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeLocal,
                    out var localStart))
            {
                model.StartAt = new DateTimeOffset(localStart);
            }
        }

        if (!string.IsNullOrWhiteSpace(model.Location))
            model.LocationName = model.Location.Trim();

        if (string.IsNullOrWhiteSpace(model.Timezone))
            model.Timezone = "Europe/Stockholm";

        model.JoinButton = IsFormCheckboxChecked(form, nameof(model.JoinButton), "joinButton");
        model.ChatEnabled = IsFormCheckboxChecked(form, nameof(model.ChatEnabled), "chatEnabled");
        model.JoinMode = model.JoinButton ? JoinMode.Open : JoinMode.Disabled;

        NormalizePaymentFields(model);
    }

    // ************************************************************************************************
    // Normalizes the payment fields in the view model.
    private static void NormalizePaymentFields(AddEventViewModel model)
    {
        model.PaymentMethod = model.PaymentMethod.TrimOrNull();
        model.PaymentNumber = model.PaymentNumber.TrimOrNull();
        model.PaymentName = model.PaymentName.TrimOrNull();
        model.PaymentComment = model.PaymentComment.TrimOrNull();
        model.PaymentAmount = model.PaymentAmount.TrimOrNull();
    }

    // ************************************************************************************************
    // Checks if a checkbox is checked in the form.
    private static bool IsFormCheckboxChecked(IFormCollection form, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (!form.TryGetValue(key, out var values))
                continue;

            foreach (var value in values)
            {
                if (string.IsNullOrEmpty(value))
                    continue;

                if (value == "1"
                    || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(value, "on", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }

        return false;
    }
}
