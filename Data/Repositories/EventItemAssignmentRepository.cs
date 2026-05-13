using Data.Contexts;
using Data.Entities;

namespace Data.Repositories;

public interface IEventItemAssignmentRepository : IBaseRepository<EventItemAssignmentEntity, EventItemAssignmentEntity>
{
}

public class EventItemAssignmentRepository : BaseRepository<EventItemAssignmentEntity, EventItemAssignmentEntity>, IEventItemAssignmentRepository
{
    public EventItemAssignmentRepository(AppDbContext context) : base(context)
    {
    }
}
