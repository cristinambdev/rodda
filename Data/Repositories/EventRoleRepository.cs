using Data.Contexts;
using Data.Entities;
using Domain.Models;

namespace Data.Repositories;

public interface IEventRoleRepository : IBaseRepository<EventRoleEntity, EventRole>
{
}

public class EventRoleRepository(AppDbContext context) : BaseRepository<EventRoleEntity, EventRole>(context), IEventRoleRepository
{
}
