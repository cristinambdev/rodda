using Data.Contexts;
using Data.Entities;
using Domain.Models;

namespace Data.Repositories;

public interface IEventItemRepository : IBaseRepository<EventItemEntity, EventItem>
{
}

public class EventItemRepository(AppDbContext context) : BaseRepository<EventItemEntity, EventItem>(context), IEventItemRepository
{
}
