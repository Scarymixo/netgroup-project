using System.Data;
using App.DAL.EF;
using App.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace App.BLL.Services;

public class ParticipantService
{
    private const int MaxRetries = 3;

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

        for (var attempt = 1; attempt <= MaxRetries; attempt++)
        {
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
                return new ParticipantRegistrationResult(ParticipantRegistrationStatus.Ok, participant);
            }
            catch (DbUpdateException ex)
            {
                await tx.RollbackAsync();
                _context.Entry(participant).State = EntityState.Detached;

                if (ex.GetBaseException() is PostgresException pg)
                {
                    // 23505 = unique_violation -> genuine duplicate registration
                    if (pg.SqlState == PostgresErrorCodes.UniqueViolation)
                    {
                        return new ParticipantRegistrationResult(
                            ParticipantRegistrationStatus.DuplicateRegistration, null);
                    }

                    // 40001 = serialization_failure, 40P01 = deadlock_detected.
                    // Two concurrent registrations on the last slot land here; retry and
                    // the loser will see EventFull on the re-read.
                    if (pg.SqlState is PostgresErrorCodes.SerializationFailure
                                    or PostgresErrorCodes.DeadlockDetected)
                    {
                        if (attempt < MaxRetries) continue;
                        return new ParticipantRegistrationResult(
                            ParticipantRegistrationStatus.SerializationConflict, null);
                    }
                }

                throw;
            }
        }

        return new ParticipantRegistrationResult(ParticipantRegistrationStatus.SerializationConflict, null);
    }
}
