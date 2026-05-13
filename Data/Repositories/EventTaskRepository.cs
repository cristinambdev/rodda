using Data.Contexts;
using Data.Entities;

namespace Data.Repositories;

public interface IEventTaskRepository : IBaseRepository<EventTaskEntity, EventTaskEntity>
{
}

public class EventTaskRepository : BaseRepository<EventTaskEntity, EventTaskEntity>, IEventTaskRepository
{
    public EventTaskRepository(AppDbContext context) : base(context)
    {
    }
}
