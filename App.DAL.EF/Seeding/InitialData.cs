using App.Domain;
using Base.Domain;

namespace App.DAL.EF.Seeding;

public static class InitialData
{
    public static readonly Guid AdminUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid TestEventId = Guid.NewGuid();

    public static readonly Event[] Events =
    [
        new()
        {
            Id = TestEventId,
            AppUserId = AdminUserId,
            EventName = "Test Event, come party!",
            StartTime = new DateTime(2026, 4, 27, 19, 30, 0, DateTimeKind.Utc),
            EndTime = new DateTime(2026, 4, 28, 19, 30, 0, DateTimeKind.Utc),
            MaxParticipants = 15,
            CreatedAt = DateTime.UtcNow,
        },
    ];

    public static readonly Participant[] Participants =
    [
        new()
        {
            Id = Guid.NewGuid(),
            EventId = TestEventId,
            FirstName = "John",
            LastName = "Doe",
            NationalId = "12345678912"
        },
        new()
        {
            Id = Guid.NewGuid(),
            EventId = TestEventId,
            FirstName = "Jane",
            LastName = "Doe",
            NationalId = "98765432101"
        }
    ];

    public static readonly string[] Roles =
    [
        "User",
        "Admin"
    ];

    public static readonly (string email, string password, string[] roles)[] Users =
    [
        ("admin@taltech.ee", "Kala.12345", ["Admin"])
    ];
}
