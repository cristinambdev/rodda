using System.ComponentModel.DataAnnotations;
using Domain.Models;

namespace Presentation.Models;

public class SignUpViewModel
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "Display name is required.")]
    [StringLength(256)]
    public string DisplayName { get; set; } = null!;

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = null!;

    [Required(ErrorMessage = "Please confirm your password.")]
    [Compare(nameof(Password), ErrorMessage = "Password and confirmation do not match.")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = null!;

    public SignUpFormData ToFormData() => new()
    {
        Email = Email,
        DisplayName = DisplayName,
        Password = Password,
        ConfirmPassword = ConfirmPassword
    };
}
