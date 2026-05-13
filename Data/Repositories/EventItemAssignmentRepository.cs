using Data.Contexts;
using Data.Entities;
using Domain.Models;

namespace Data.Repositories;

public interface IEventItemAssignmentRepository : IBaseRepository<EventItemAssignmentEntity, EventItemAssignment>
{
}

public class EventItemAssignmentRepository(AppDbContext context) : BaseRepository<EventItemAssignmentEntity, EventItemAssignment>(context), IEventItemAssignmentRepository
{
}
