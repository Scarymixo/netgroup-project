using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using EventDto = App.DTO.v1.Event;

namespace TestProject.Integration;

public class EventsEndpointsTests : IntegrationTestBase
{
    public EventsEndpointsTests(IntegrationTestFactory factory) : base(factory) { }

    [Fact]
    public async Task GetEvents_ReturnsOk_WithSeededEvents()
    {
        await SeedEventAsync("Event A", maxParticipants: 5, participantCount: 2);
        await SeedEventAsync("Event B", maxParticipants: 10);

        var response = await Client.GetAsync("/api/v1/events");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var events = await response.Content.ReadFromJsonAsync<List<EventDto>>();
        events.Should().NotBeNull();
        events!.Should().HaveCount(2);
        var a = events.Single(e => e.EventName == "Event A");
        a.SpotsLeft.Should().Be(3);
    }

    [Fact]
    public async Task GetEvent_ReturnsNotFound_WhenMissing()
    {
        var response = await Client.GetAsync($"/api/v1/events/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetEvent_ReturnsOk_WithSpotsLeft()
    {
        var ev = await SeedEventAsync(maxParticipants: 5, participantCount: 3);

        var response = await Client.GetAsync($"/api/v1/events/{ev.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<EventDto>();
        dto.Should().NotBeNull();
        dto!.Id.Should().Be(ev.Id);
        dto.SpotsLeft.Should().Be(2);
    }

    [Fact]
    public async Task PostEvent_Returns401_WithoutAuth()
    {
        var dto = NewEventDto();
        var response = await Client.PostAsJsonAsync("/api/v1/events", dto);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostEvent_Returns201_WithAuth()
    {
        Authenticate();
        var dto = NewEventDto("Created via API");

        var response = await Client.PostAsJsonAsync("/api/v1/events", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        var body = await response.Content.ReadFromJsonAsync<EventDto>();
        body.Should().NotBeNull();
        body!.EventName.Should().Be("Created via API");
        body.Id.Should().NotBe(Guid.Empty);

        (await EventExistsAsync(body.Id)).Should().BeTrue();
    }

    [Fact]
    public async Task PutEvent_Returns400_WhenLoweringMaxBelowRegistered()
    {
        Authenticate();
        var ev = await SeedEventAsync(maxParticipants: 10, participantCount: 5);

        var dto = new EventDto
        {
            Id = ev.Id,
            EventName = ev.EventName,
            MaxParticipants = 2,
            StartTime = ev.StartTime,
            EndTime = ev.EndTime,
        };

        var response = await Client.PutAsJsonAsync($"/api/v1/events/{ev.Id}", dto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("MaxParticipants below current registered count");
    }

    [Fact]
    public async Task PutEvent_Returns204_OnValidUpdate()
    {
        Authenticate();
        var ev = await SeedEventAsync(maxParticipants: 10, participantCount: 2);

        var dto = new EventDto
        {
            Id = ev.Id,
            EventName = "Renamed",
            MaxParticipants = 20,
            StartTime = ev.StartTime,
            EndTime = ev.EndTime,
        };

        var response = await Client.PutAsJsonAsync($"/api/v1/events/{ev.Id}", dto);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteEvent_Returns204_ThenGetReturns404()
    {
        Authenticate();
        var ev = await SeedEventAsync();

        var del = await Client.DeleteAsync($"/api/v1/events/{ev.Id}");
        del.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var get = await Client.GetAsync($"/api/v1/events/{ev.Id}");
        get.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetEventParticipants_Returns401_WithoutAuth()
    {
        var ev = await SeedEventAsync();
        var response = await Client.GetAsync($"/api/v1/events/{ev.Id}/participants");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetEventParticipants_Returns200_WithAuth()
    {
        Authenticate();
        var ev = await SeedEventAsync(participantCount: 3);

        var response = await Client.GetAsync($"/api/v1/events/{ev.Id}/participants");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<App.DTO.v1.Participant>>();
        body.Should().NotBeNull();
        body!.Should().HaveCount(3);
    }

    private static EventDto NewEventDto(string name = "New Event", int max = 10)
    {
        var start = DateTime.UtcNow.AddDays(7);
        return new EventDto
        {
            EventName = name,
            MaxParticipants = max,
            StartTime = start,
            EndTime = start.AddHours(2),
        };
    }
}
