namespace Domain.Models;

public class SignUpFormData
{
    public string Email { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string ConfirmPassword { get; set; } = null!;
    public bool TermsAccepted { get; set; }
}
