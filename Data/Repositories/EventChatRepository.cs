using Data.Contexts;
using Data.Entities;
using Domain.Models;

namespace Data.Repositories;

public interface IEventChatRepository : IBaseRepository<EventChatEntity, EventChat>
{
}

public class EventChatRepository(AppDbContext context) : BaseRepository<EventChatEntity, EventChat>(context), IEventChatRepository
{
}
