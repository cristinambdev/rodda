using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Presentation.Models;

public class AddEventViewModel
{
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
    public string Timezone { get; set; } = "Europe/Stockholm";

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

    public JoinMode JoinMode { get; set; } = JoinMode.Open;

    public EventStatus Status { get; set; } = EventStatus.Active;

    public bool ChatEnabled { get; set; }

    public bool? ItemsTasksEnabled { get; set; }

    public bool? AllowGuestBringItems { get; set; }

    public bool? AllowGuestTasks { get; set; }

    [StringLength(120)]
    public string? PaymentMethod { get; set; }

    [StringLength(120)]
    public string? PaymentNumber { get; set; }

    [StringLength(200)]
    public string? PaymentName { get; set; }

    public decimal? PaymentAmount { get; set; }

    [StringLength(200)]
    public string? PaymentComment { get; set; }

}
