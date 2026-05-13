using Data.Contexts;
using Data.Entities;
using Domain.Models;

namespace Data.Repositories;

public interface IEventTaskAssignmentRepository : IBaseRepository<EventTaskAssignmentEntity, EventTaskAssignment>
{
}

public class EventTaskAssignmentRepository(AppDbContext context) : BaseRepository<EventTaskAssignmentEntity, EventTaskAssignment>(context), IEventTaskAssignmentRepository
{
}
