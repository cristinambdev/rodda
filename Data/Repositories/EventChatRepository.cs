using Data.Contexts;
using Data.Entities;

namespace Data.Repositories;

public interface IEventChatRepository : IBaseRepository<EventChatEntity, EventChatEntity>
{
}

public class EventChatRepository : BaseRepository<EventChatEntity, EventChatEntity>, IEventChatRepository
{
    public EventChatRepository(AppDbContext context) : base(context)
    {
    }
}
