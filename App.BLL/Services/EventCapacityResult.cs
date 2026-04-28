namespace App.BLL.Services;

public enum EventCapacityChangeStatus
{
    Ok,
    EventNotFound,
    BelowCurrentRegistered
}

public sealed record EventCapacityChangeResult(
    EventCapacityChangeStatus Status,
    int CurrentRegistered);
