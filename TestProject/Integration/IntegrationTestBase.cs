using System.Net.Http.Headers;
using App.DAL.EF;
using App.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace TestProject.Integration;

public abstract class IntegrationTestBase : IClassFixture<IntegrationTestFactory>, IAsyncLifetime
{
    protected readonly IntegrationTestFactory Factory;
    protected readonly HttpClient Client;

    protected IntegrationTestBase(IntegrationTestFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        // Wipe app tables between tests; keep seeded Identity user.
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Participants.RemoveRange(db.Participants);
        db.Events.RemoveRange(db.Events);
        await db.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    protected void Authenticate(HttpClient? client = null)
    {
        var token = JwtTestTokenIssuer.Issue(Factory.SeededUserId, IntegrationTestFactory.SeededUserEmail);
        (client ?? Client).DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    protected async Task<Event> SeedEventAsync(
        string name = "Test Event",
        int maxParticipants = 10,
        DateTime? start = null,
        DateTime? end = null,
        int participantCount = 0)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var startTime = start ?? DateTime.UtcNow.AddDays(7);
        var endTime = end ?? startTime.AddHours(2);

        var ev = new Event
        {
            Id = Guid.NewGuid(),
            AppUserId = Factory.SeededUserId,
            EventName = name,
            MaxParticipants = maxParticipants,
            StartTime = startTime,
            EndTime = endTime,
            CreatedAt = DateTime.UtcNow,
        };
        db.Events.Add(ev);

        for (var i = 0; i < participantCount; i++)
        {
            db.Participants.Add(new Participant
            {
                Id = Guid.NewGuid(),
                EventId = ev.Id,
                FirstName = $"First{i}",
                LastName = $"Last{i}",
                NationalId = $"NID-{Guid.NewGuid():N}".Substring(0, 20),
            });
        }

        await db.SaveChangesAsync();
        return ev;
    }

    protected async Task<int> CountParticipantsAsync(Guid eventId)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Participants.CountAsync(p => p.EventId == eventId);
    }

    protected async Task<bool> EventExistsAsync(Guid eventId)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Events.AnyAsync(e => e.Id == eventId);
    }
}
