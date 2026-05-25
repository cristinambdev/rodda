using System.Globalization;
using Domain.Enums;
using Domain.Models;
using Microsoft.AspNetCore.Http;
using Presentation.Extensions;
using Presentation.Models;

namespace Presentation.Helpers;

// ************************************************************************************************
// CreateEventFormHelper — maps domain events to `AddEventViewModel` and normalizes create/edit form posts.
// ************************************************************************************************
// Consumers: `EventsController` create/update/edit actions, `AddEventViewModel` payment parsing.
// Data in: `Event` entity or `IFormCollection` on POST; writes into `AddEventViewModel` / redirect targets.
//  Wrong date parsing shifts `StartAt` stored by `EventService`; payment parse affects `Payment` entity.
// ************************************************************************************************
public static class CreateEventFormHelper
{
    // ************************************************************************************************
    // FromEvent — hydrates the shared create/edit form from an existing event (edit flow).
    // Maps schedule, location fields, join/chat/items flags, payment, cover URL to `AddEventViewModel` because one form (`CreateNewEvent.cshtml`) serves both create and edit; needs a single mapping from domain.
    // Uses `EventsController` edit GET returns `View(FromEvent(ev))`; domain from `EventService`.
    // Drives pre-filled inputs and modals on edit; does not load items/tasks (separate modal helper).
    // ************************************************************************************************
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
    // ResolveLocationDisplay — single-line location summary for the form’s location field.
    // Prefers venue name; otherwise joins street/postcode/city/country because edit form shows one `Location` input while DB stores structured `Address`.
    // Uses `ApplyFormFields` may copy trimmed `Location` into `LocationName` on POST. Display string on form only unless POST overwrites structured fields.
    // ************************************************************************************************
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
    // ParsePaymentAmount — parses free-text payment amount from the form (also used by view model).
    // Strips kr/sek/:- , tries invariant, sv-SE, and current culture decimal parse because users type “50kr” or “12,50”; DB stores `decimal?`.
    // Uses `AddEventViewModel` setters; `EventService` persists `Payment.Amount`. Parse failure yields null amount on event; shown on `EventDetails` payment row when set.
    // ************************************************************************************************
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
    // FormatPaymentAmount — formats stored decimal for the edit form input.
    // Whole numbers as `{n}kr`; decimals as invariant string because matches how users expect to see Swedish amounts in the form.
    // Uses `FromEvent`; inverse of `ParsePaymentAmount`. Form display only.
    // ************************************************************************************************
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
    // ApplyFormFields — normalizes POSTed create/edit fields before validation and service calls.
    // Builds `StartAt` from date+time, default timezone, join/chat checkboxes, trims payment fields because HTML checkboxes and split date/time inputs are not bound cleanly without explicit normalization.
    // Uses `IsFormCheckboxChecked`. `JoinMode` open/disabled drives join UI on `EventDetails`; wrong checkbox read disables join.
    // ************************************************************************************************
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
    // NormalizePaymentFields — trims payment strings on the view model.
    // Applies `TrimOrNull` extension to payment properties because avoids persisting whitespace-only payment metadata.
    // Uses `ApplyFormFields`; `AddEventViewModel` mapping to domain. Empty strings become null in downstream save.
    // ************************************************************************************************
    private static void NormalizePaymentFields(AddEventViewModel model)
    {
        model.PaymentMethod = model.PaymentMethod.TrimOrNull();
        model.PaymentNumber = model.PaymentNumber.TrimOrNull();
        model.PaymentName = model.PaymentName.TrimOrNull();
        model.PaymentComment = model.PaymentComment.TrimOrNull();
        model.PaymentAmount = model.PaymentAmount.TrimOrNull();
    }

    // ************************************************************************************************
    // IsFormCheckboxChecked — reads checkbox values from raw form (on/true/1).
    // Tries multiple field names used by the create/edit form markup because unchecked checkboxes are absent from model binding; POST must read form collection explicitly.
    // Uses `ApplyFormFields`. Controls `JoinEnabled` and chat section visibility on event details after save.
    // ************************************************************************************************
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
