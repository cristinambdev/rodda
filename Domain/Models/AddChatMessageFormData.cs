namespace Domain.Models;

public class AddChatMessageFormData
{
    public string Body { get; set; } = null!;
    public string? AuthorDisplay { get; set; }
}
