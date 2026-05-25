namespace Presentation.Models;

public class SettingsViewModel
{
    public string DisplayName { get; set; } = "";
    public string Email { get; set; } = "";
    public string? ProfileImageUrl { get; set; }

    public bool NotifyNewEvents { get; set; }
    public bool NotifyEventChanges { get; set; } = true;
    public bool NotifyEventComments { get; set; } = true;
    public bool NotifyCoOwnership { get; set; }

    public string Language { get; set; } = "en";
    public string LanguageLabel { get; set; } = "English";

    public bool PrivacyShowInCommunity { get; set; } = true;
    public bool PrivacyShowAttendance { get; set; } = true;
}
