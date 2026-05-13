using Data.Contexts;
using Data.Entities;

namespace Data.Repositories;

public interface IEventTaskAssignmentRepository : IBaseRepository<EventTaskAssignmentEntity, EventTaskAssignmentEntity>
{
}

public class EventTaskAssignmentRepository : BaseRepository<EventTaskAssignmentEntity, EventTaskAssignmentEntity>, IEventTaskAssignmentRepository
{
    public EventTaskAssignmentRepository(AppDbContext context) : base(context)
    {
    }
}
