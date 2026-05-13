using Data.Contexts;
using Data.Entities;

namespace Data.Repositories;

public interface IEventRoleRepository : IBaseRepository<EventRoleEntity, EventRoleEntity>
{
}

public class EventRoleRepository : BaseRepository<EventRoleEntity, EventRoleEntity>, IEventRoleRepository
{
    public EventRoleRepository(AppDbContext context) : base(context)
    {
    }
}
