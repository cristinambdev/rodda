using System.ComponentModel.DataAnnotations;
using Domain.Models;

namespace Presentation.Models;

public class SignUpViewModel
{
    [Required(ErrorMessage = "Email is required.")]
    [RegularExpression(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", ErrorMessage = "Enter a valid email address.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [Display(Name = "Email", Prompt = "you@example.com")]
    [DataType(DataType.EmailAddress)]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "Display name is required.")]
    [StringLength(256)]
    [Display(Name = "Display name", Prompt = "Your name")]
    [DataType(DataType.Text)]
    public string DisplayName { get; set; } = null!;

    [Required(ErrorMessage = "Password is required.")]
    [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d)[A-Za-z\d]{8,}$", ErrorMessage = "Password must be at least 8 characters long and contain at least one letter and one number.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters.")]
    [DataType(DataType.Password)]
    [Display(Name = "Password", Prompt = "At least 8 characters")]
    public string Password { get; set; } = null!;

    [Required(ErrorMessage = "Please confirm your password.")]
    [Compare(nameof(Password), ErrorMessage = "Password and confirmation do not match.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm password", Prompt = "Repeat password")]
    public string ConfirmPassword { get; set; } = null!;
}
