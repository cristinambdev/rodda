using Data.Contexts;
using Data.Entities;

namespace Data.Repositories;

public interface IEventRepository : IBaseRepository<EventEntity, EventEntity>
{
}

public class EventRepository : BaseRepository<EventEntity, EventEntity>, IEventRepository
{
    public EventRepository(AppDbContext context) : base(context)
    {
    }
}
