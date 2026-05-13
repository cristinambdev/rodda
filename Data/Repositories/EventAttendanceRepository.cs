using Data.Contexts;
using Data.Entities;

namespace Data.Repositories;

public interface IEventAttendanceRepository : IBaseRepository<EventAttendanceEntity, EventAttendanceEntity>
{
}

public class EventAttendanceRepository : BaseRepository<EventAttendanceEntity, EventAttendanceEntity>, IEventAttendanceRepository
{
    public EventAttendanceRepository(AppDbContext context) : base(context)
    {
    }
}
