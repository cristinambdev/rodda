using Data.Contexts;
using Data.Entities;
using Domain.Models;

namespace Data.Repositories;

public interface IEventTaskRepository : IBaseRepository<EventTaskEntity, EventTask>
{
}

public class EventTaskRepository(AppDbContext context) : BaseRepository<EventTaskEntity, EventTask>(context), IEventTaskRepository
{
}
