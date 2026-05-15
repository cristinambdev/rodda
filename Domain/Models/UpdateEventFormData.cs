using Domain.Enums;

namespace Domain.Models;

public class UpdateEventFormData
{
    public string Title { get; set; } = null!;
    public string? Slug { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset StartAt { get; set; }
    public DateTimeOffset? EndAt { get; set; }
    public string Timezone { get; set; } = null!;
    public string? LocationName { get; set; }
    public string? LocationStreet { get; set; }
    public string? LocationPostcode { get; set; }
    public string? LocationCity { get; set; }
    public string? LocationCountry { get; set; }
    public JoinMode JoinMode { get; set; }
    public EventStatus Status { get; set; }
    public bool ChatEnabled { get; set; }
    public bool? ItemsTasksEnabled { get; set; }
    public bool? AllowGuestBringItems { get; set; }
    public bool? AllowGuestTasks { get; set; }
    public string? PaymentMethod { get; set; }
    public string? PaymentNumber { get; set; }
    public string? PaymentName { get; set; }
    public decimal? PaymentAmount { get; set; }
    public string? PaymentComment { get; set; }
}
