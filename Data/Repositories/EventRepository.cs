using Data.Contexts;
using Data.Entities;
using Domain.Models;

namespace Data.Repositories;

public interface IEventRepository : IBaseRepository<EventEntity, Event>
{
}

public class EventRepository(AppDbContext context) : BaseRepository<EventEntity, Event>(context), IEventRepository
{
}
