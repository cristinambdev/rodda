using Domain.Models;

namespace Business.Dtos;
public class EventResult <T> : ServiceResult
{
    public T? Result { get; set; }
}

public class EventResult : ServiceResult
{
   public IEnumerable<EventResult>? Result { get; set; }
}
