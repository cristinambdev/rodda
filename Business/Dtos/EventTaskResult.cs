using Domain.Models;

namespace Business.Dtos;

public class EventTaskResult : ServiceResult
{
    public IEnumerable<EventTask>? Result { get; set; }
}

public class EventTaskResult<T> : ServiceResult
{
    public T? Result { get; set; }
}
