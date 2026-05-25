namespace Domain.Models;

public class UpdateUserFormData
{
    public string UserName { get; set; } = null!;
    public string? DisplayName { get; set; }
    public string? Email { get; set; }
    public string? ProfileImageUrl { get; set; }
    public string? NewPassword { get; set; }
}
