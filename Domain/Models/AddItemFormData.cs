using Domain.Enums;

namespace Domain.Models;

public class AddItemFormData
{
    public string Title { get; set; } = null!;
    public string? Amount { get; set; }
    public SignupMode SignupMode { get; set; } = SignupMode.Available;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
