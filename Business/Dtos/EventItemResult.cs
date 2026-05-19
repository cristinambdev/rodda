using Domain.Models;

namespace Business.Dtos;

public class EventItemResult : ServiceResult
{
    public IEnumerable<EventItem>? Result { get; set; }
}

public class EventItemResult<T> : ServiceResult
{
    public T? Result { get; set; }
}
