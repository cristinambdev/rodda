using System.ComponentModel.DataAnnotations;
using Domain.Enums;
using Domain.Models;

namespace Presentation.Models;

public class EditItemViewModel
{
    public string ItemId { get; set; } = null!;

    public string EventId { get; set; } = null!;

    [Required]
    [StringLength(300)]
    public string Title { get; set; } = null!;

    [StringLength(200)]
    public string? Amount { get; set; }

    [Range(1, 99)]
    public int PeopleNeeded { get; set; } = 1;

    public SignupMode SignupMode { get; set; } = SignupMode.Available;

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

}
