using Data.Contexts;
using Data.Entities;

namespace Data.Repositories;

public interface IEventItemRepository : IBaseRepository<EventItemEntity, EventItemEntity>
{
}

public class EventItemRepository : BaseRepository<EventItemEntity, EventItemEntity>, IEventItemRepository
{
    public EventItemRepository(AppDbContext context) : base(context)
    {
    }
}
