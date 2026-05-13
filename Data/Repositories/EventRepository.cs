using Data.Contexts;
using Data.Entities;
using Domain.Models;

namespace Data.Repositories;

public interface IEventRepository : IBaseRepository<EventEntity, Event>
{
}

public class EventRepository : BaseRepository<EventEntity, Event>, IEventRepository
{
    public EventRepository(AppDbContext context) : base(context)
    {
    }
}
