namespace Domain.Models;

public class User
{
    public string Id { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? ProfileImageUrl { get; set; }
}
