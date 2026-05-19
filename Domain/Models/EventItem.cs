using Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Models;

public class EventItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public string EventId { get; set; } = null!;
    public string? EventTitle { get; set; }

    // CURRENT DISPLAY DATA (What the UI shows)
    // If an assignee edits the item, it changes THESE fields.
    public string Title { get; set; } = null!;
    public string? Amount { get; set; }
    public SignupMode SignupMode { get; set; } = SignupMode.Available;

    // THE ORIGINAL BACKUP (Event creator data)
    // Used ONLY to revert the item if the assignee cancels.
    public string OriginalTitle { get; set; } = null!;
    public string? OriginalAmount { get; set; }
    public SignupMode OriginalSignupMode { get; set; } = SignupMode.Available;

    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public string? CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }

    public ICollection<EventItemAssignment> Assignments { get; set; } = new List<EventItemAssignment>();
}
