using System.ComponentModel.DataAnnotations;
using Domain.Enums;
using Domain.Models;

namespace Presentation.Models;

public class AddTaskViewModel
{
    public string EventId { get; set; } = null!;

    [Required]
    [StringLength(300)]
    public string Title { get; set; } = null!;

    public DateTimeOffset? ScheduledAt { get; set; }

    [StringLength(32)]
    public string? TaskTime { get; set; }

    [StringLength(500)]
    public string? TaskLocation { get; set; }

    public SignupMode SignupMode { get; set; } = SignupMode.Available;

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;


}
