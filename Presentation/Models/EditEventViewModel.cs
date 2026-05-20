using System.ComponentModel.DataAnnotations;
using Domain.Enums;
using Domain.Models;

namespace Presentation.Models;

public class EditEventViewModel
{
    public string Id { get; set; } = null!;

    public string EventId { get; set; } = null!;

    [Required]
    [StringLength(500)]
    public string Title { get; set; } = null!;

    [StringLength(160)]
    public string? Slug { get; set; }

    [StringLength(2048)]
    [Url]
    public string? CoverImageUrl { get; set; }

    [StringLength(8000)]
    public string? Description { get; set; }

    [Required]
    public DateTimeOffset StartAt { get; set; }

    public DateTimeOffset? EndAt { get; set; }

    [Required]
    [StringLength(100)]
    public string Timezone { get; set; } = null!;

    [StringLength(300)]
    public string? LocationName { get; set; }

    [StringLength(500)]
    public string? LocationStreet { get; set; }

    [StringLength(32)]
    public string? LocationPostcode { get; set; }

    [StringLength(120)]
    public string? LocationCity { get; set; }

    [StringLength(100)]
    public string? LocationCountry { get; set; }

    public JoinMode JoinMode { get; set; }

    public EventStatus Status { get; set; }

    public bool ChatEnabled { get; set; }

    public bool? ItemsTasksEnabled { get; set; }

    public bool AllowGuestBringItems { get; set; }

    public bool AllowGuestTasks { get; set; }

    [StringLength(120)]
    public string? PaymentMethod { get; set; }

    [StringLength(120)]
    public string? PaymentNumber { get; set; }

    [StringLength(200)]
    public string? PaymentName { get; set; }

    public decimal? PaymentAmount { get; set; }

    [StringLength(200)]
    public string? PaymentComment { get; set; }

    // **************************************************************************************************************************

    public UpdateEventFormData ToFormData() => new()
    {
        Title = Title,
        Slug = Slug,
        CoverImageUrl = CoverImageUrl,
        Description = Description,
        StartAt = StartAt,
        EndAt = EndAt,
        Timezone = Timezone,
        LocationName = LocationName,
        LocationStreet = LocationStreet,
        LocationPostcode = LocationPostcode,
        LocationCity = LocationCity,
        LocationCountry = LocationCountry,
        JoinMode = JoinMode,
        Status = Status,
        ChatEnabled = ChatEnabled,
        ItemsTasksEnabled = ItemsTasksEnabled,
        AllowGuestBringItems = AllowGuestBringItems,
        AllowGuestTasks = AllowGuestTasks,
        PaymentMethod = PaymentMethod,
        PaymentNumber = PaymentNumber,
        PaymentName = PaymentName,
        PaymentAmount = PaymentAmount,
        PaymentComment = PaymentComment
    };
}
