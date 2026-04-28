using App.DAL.EF;
using Microsoft.EntityFrameworkCore;

namespace App.BLL.Services;

public class EventService
{
    private readonly AppDbContext _context;

    public EventService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<EventCapacityChangeResult> CheckMaxParticipantsChangeAsync(Guid eventId, int newMax)
    {
        var exists = await _context.Events.AnyAsync(e => e.Id == eventId);
        if (!exists)
        {
            return new EventCapacityChangeResult(EventCapacityChangeStatus.EventNotFound, 0);
        }

        var currentCount = await _context.Participants.CountAsync(p => p.EventId == eventId);
        if (newMax < currentCount)
        {
            return new EventCapacityChangeResult(EventCapacityChangeStatus.BelowCurrentRegistered, currentCount);
        }

        return new EventCapacityChangeResult(EventCapacityChangeStatus.Ok, currentCount);
    }
}
