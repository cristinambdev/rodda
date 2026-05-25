namespace Domain.Models;

public class UserProfile
{
    public string DisplayName { get; set; } = "";
    public string Email { get; set; } = "";
    public string? ProfileImageUrl { get; set; }
}
