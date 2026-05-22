using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Data.Entities;


[Table("EventAttendances")]
public class EventAttendanceEntity
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    public string EventId { get; set; } = null!;

    [Required]
    public string UserId { get; set; } = null!;

    public AttendanceStatus Status { get; set; }

    [Range(0, 1000)]
    public int GuestCount { get; set; } = 1;

    public DateTime RespondedAt { get; set; } = DateTime.UtcNow;

    /// <summary>When true, the event is hidden from this user's event lists until they join again.</summary>
    // Suggested by LLM: "When true, the event is hidden from this user's event lists until they join again."
    public bool HiddenFromList { get; set; }

    [ForeignKey(nameof(EventId))]
    public virtual EventEntity Event { get; set; } = null!;

    [ForeignKey(nameof(UserId))]
    public virtual UserEntity User { get; set; } = null!;
}
