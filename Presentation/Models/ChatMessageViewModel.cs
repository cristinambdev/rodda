namespace Presentation.Models;

public class ChatMessageViewModel
{
    public string Id { get; set; } = null!;
    public string EventId { get; set; } = null!;
    public string? AuthorUserId { get; set; }
    public string? AuthorDisplay { get; set; }
    public string? AuthorProfileImageUrl { get; set; }
    public string Body { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public bool IsFromCurrentUser { get; set; }
    public bool CanDelete { get; set; }
}
