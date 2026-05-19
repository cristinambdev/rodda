using System.ComponentModel.DataAnnotations;
using Domain.Models;

namespace Presentation.Models;

public class EditChatMessageViewModel
{
    public string MessageId { get; set; } = null!;

    public string EventId { get; set; } = null!;

    [Required]
    [StringLength(8000)]
    public string Body { get; set; } = null!;
}
