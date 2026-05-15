using System.ComponentModel.DataAnnotations;
using Domain.Models;

namespace Presentation.Models;

public class SignInViewModel
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "Password is required.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = null!;

    [Display(Name = "Remember me")]
    public bool IsPersistent { get; set; }

    public SignInFormData ToFormData() => new()
    {
        Email = Email,
        Password = Password,
        IsPersistent = IsPersistent
    };
}
