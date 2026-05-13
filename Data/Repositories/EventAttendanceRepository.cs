using Data.Contexts;
using Data.Entities;
using Domain.Models;

namespace Data.Repositories;

public interface IEventAttendanceRepository : IBaseRepository<EventAttendanceEntity, EventAttendance>
{
}

public class EventAttendanceRepository(AppDbContext context) : BaseRepository<EventAttendanceEntity, EventAttendance>(context), IEventAttendanceRepository
{
}
