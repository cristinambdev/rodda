using Data.Contexts;
using Data.Entities;
using Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Data.Repositories;

public interface IEventShareLinkRepository : IBaseRepository<EventShareLinkEntity, EventShareLinkEntity>
{
    Task<RepositoryResult<EventShareLinkEntity>> GetActiveByEventIdAsync(string eventId);
    Task<RepositoryResult<EventShareLinkEntity>> GetByEventAndTokenAsync(string eventId, string token);
}

public class EventShareLinkRepository(AppDbContext context) : BaseRepository<EventShareLinkEntity, EventShareLinkEntity>(context), IEventShareLinkRepository
{
    // **************************************************************************************************************************
    public async Task<RepositoryResult<EventShareLinkEntity>> GetActiveByEventIdAsync(string eventId)
    {
        var link = await _table
            .Where(l => l.EventId == eventId && !l.IsRevoked)
            .OrderByDescending(l => l.CreatedAt)
            .FirstOrDefaultAsync();

        if (link == null)
            return new RepositoryResult<EventShareLinkEntity> { Succeeded = false, StatusCode = 404, ErrorMessage = "Share link not found." };

        return new RepositoryResult<EventShareLinkEntity> { Succeeded = true, StatusCode = 200, Result = link };
    }

    // **************************************************************************************************************************
    public async Task<RepositoryResult<EventShareLinkEntity>> GetByEventAndTokenAsync(string eventId, string token)
    {
        var link = await _table.FirstOrDefaultAsync(l => l.EventId == eventId && l.Token == token);

        if (link == null)
            return new RepositoryResult<EventShareLinkEntity> { Succeeded = false, StatusCode = 404, ErrorMessage = "Share link not found." };

        return new RepositoryResult<EventShareLinkEntity> { Succeeded = true, StatusCode = 200, Result = link };
    }
}
