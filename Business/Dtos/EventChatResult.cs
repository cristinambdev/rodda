using Domain.Models;

namespace Business.Dtos;

public class EventChatResult : ServiceResult
{
    public IEnumerable<EventChat>? Result { get; set; }
}
