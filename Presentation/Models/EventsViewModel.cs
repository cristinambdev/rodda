using Business.Dtos;
using Domain.Models;

namespace Presentation.Models;

public class EventsViewModel
{
    public EventResult<IEnumerable<Event>>? Events { get; set; }
}
