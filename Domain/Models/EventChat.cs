namespace Domain.Models;

public class EventChat
{
    public string Id { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string EventId { get; set; } = null!;
    public string? AuthorUserId { get; set; }
    public User? Author { get; set; }
    public string? AuthorDisplay { get; set; }
    public string Body { get; set; } = null!;
}
