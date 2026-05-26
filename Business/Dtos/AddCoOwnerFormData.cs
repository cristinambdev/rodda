namespace Business.Dtos;

// Target user to promote to co-owner on an event.
public class AddCoOwnerFormData
{
    public string TargetUserId { get; set; } = null!;
}
