namespace Presentation.Models;

/// <summary>JSON shape posted from <c>newevent.js</c> draft bring-items on event create.</summary>
public class DraftBringItemPayload
{
    public string Title { get; set; } = "";
    public string? Amount { get; set; }
    public int People { get; set; } = 1;
}

/// <summary>JSON shape posted from <c>newevent.js</c> draft guest tasks on event create.</summary>
public class DraftGuestTaskPayload
{
    public string Title { get; set; } = "";
    public string? Location { get; set; }
    public string? Time { get; set; }
    public int People { get; set; } = 1;
}
