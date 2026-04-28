namespace App.DTO.v1.Mappers;

public static class ParticipantMapper
{
    public static Participant Map(App.Domain.Participant entity)
    {
        return new Participant
        {
            Id = entity.Id,
            EventId = entity.EventId,
            FirstName = entity.FirstName,
            LastName = entity.LastName,
            NationalId = entity.NationalId,
        };
    }

    public static App.Domain.Participant Map(Participant dto)
    {
        return new App.Domain.Participant
        {
            Id = dto.Id,
            EventId = dto.EventId,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            NationalId = dto.NationalId,
        };
    }
}
