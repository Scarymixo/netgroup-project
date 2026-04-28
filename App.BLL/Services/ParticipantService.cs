using System.Data;
using App.DAL.EF;
using App.Domain;
using Microsoft.EntityFrameworkCore;

namespace App.BLL.Services;

public class ParticipantService
{
    private readonly AppDbContext _context;

    public ParticipantService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ParticipantRegistrationResult> RegisterAsync(Participant participant)
    {
        var @event = await _context.Events.FindAsync(participant.EventId);
        if (@event == null)
        {
            return new ParticipantRegistrationResult(ParticipantRegistrationStatus.EventNotFound, null);
        }

        if (participant.Id == Guid.Empty)
        {
            participant.Id = Guid.NewGuid();
        }

        await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        var currentCount = await _context.Participants.CountAsync(p => p.EventId == participant.EventId);
        if (currentCount >= @event.MaxParticipants)
        {
            return new ParticipantRegistrationResult(ParticipantRegistrationStatus.EventFull, null);
        }

        _context.Participants.Add(participant);

        try
        {
            await _context.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch (DbUpdateException)
        {
            return new ParticipantRegistrationResult(ParticipantRegistrationStatus.DuplicateRegistration, null);
        }

        return new ParticipantRegistrationResult(ParticipantRegistrationStatus.Ok, participant);
    }
}
