namespace Presentation.Models;

// ************************************************************************************************
// Home dashboard (`Home/Index`) — My To-Dos section.
public class HomeIndexViewModel
{
    public string ActiveRange { get; set; } = "month";
    public IReadOnlyList<HomeTodoEventCardViewModel> TodoEvents { get; set; } = [];
    public string EmptyMessage { get; set; } = "Nothing on your plate this month.";
}

// ************************************************************************************************
public class HomeTodoEventCardViewModel
{
    public DateTimeOffset StartAt { get; set; }
    public string EventId { get; set; } = "";
    public string Title { get; set; } = "";
    public string DetailUrl { get; set; } = "";
    public string ListDateTime { get; set; } = "";
    public string DatePart { get; set; } = "";
    public string TimePart { get; set; } = "";
    public string ListLocation { get; set; } = "";
    public string CardTheme { get; set; } = "teal";
    public int DoneCount { get; set; }
    public int LineCount { get; set; }
    public IReadOnlyList<HomeTodoBringLineViewModel> BringItems { get; set; } = [];
    public IReadOnlyList<HomeTodoTaskLineViewModel> GuestTasks { get; set; } = [];
}

// ************************************************************************************************
public class HomeTodoBringLineViewModel
{
    public string ItemId { get; set; } = "";
    public string Title { get; set; } = "";
    public string Amount { get; set; } = "";
    public bool Done { get; set; }
}

// ************************************************************************************************
public class HomeTodoTaskLineViewModel
{
    public string TaskId { get; set; } = "";
    public string Title { get; set; } = "";
    public string TaskTime { get; set; } = "";
    public string TaskLocation { get; set; } = "";
    public bool Done { get; set; }
}
