namespace App.DTO.v1.Mappers;

public static class EventMapper
{
    public static Event Map(App.Domain.Event entity, int registeredCount = 0)
    {
        return new Event
        {
            Id = entity.Id,
            EventName = entity.EventName,
            MaxParticipants = entity.MaxParticipants,
            StartTime = entity.StartTime,
            EndTime = entity.EndTime,
            SpotsLeft = Math.Max(0, entity.MaxParticipants - registeredCount),
        };
    }

    public static App.Domain.Event Map(Event dto)
    {
        return new App.Domain.Event
        {
            Id = dto.Id,
            EventName = dto.EventName,
            MaxParticipants = dto.MaxParticipants,
            StartTime = dto.StartTime,
            EndTime = dto.EndTime,
        };
    }
}
