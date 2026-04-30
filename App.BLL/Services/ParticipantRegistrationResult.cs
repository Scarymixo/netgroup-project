using App.Domain;

namespace App.BLL.Services;

public enum ParticipantRegistrationStatus
{
    Ok,
    EventNotFound,
    EventFull,
    DuplicateRegistration,
    SerializationConflict
}

public sealed record ParticipantRegistrationResult(
    ParticipantRegistrationStatus Status,
    Participant? Participant);
