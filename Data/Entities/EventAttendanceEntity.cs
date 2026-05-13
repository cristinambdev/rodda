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

    [ForeignKey(nameof(EventId))]
    public virtual EventEntity Event { get; set; } = null!;

    [ForeignKey(nameof(UserId))]
    public virtual UserEntity User { get; set; } = null!;
}
