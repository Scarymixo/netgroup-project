using App.Domain;

namespace App.BLL.Services;

public enum ParticipantRegistrationStatus
{
    Ok,
    EventNotFound,
    EventFull,
    DuplicateRegistration
}

public sealed record ParticipantRegistrationResult(
    ParticipantRegistrationStatus Status,
    Participant? Participant);
