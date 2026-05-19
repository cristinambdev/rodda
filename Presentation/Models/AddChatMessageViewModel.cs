using System.ComponentModel.DataAnnotations;
using Domain.Models;

namespace Presentation.Models;

public class AddChatMessageViewModel
{
    public string EventId { get; set; } = null!;

    [Required]
    [StringLength(8000)]
    public string Body { get; set; } = null!;

    [StringLength(200)]
    public string? AuthorDisplay { get; set; }

}
