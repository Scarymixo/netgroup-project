using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using ParticipantDto = App.DTO.v1.Participant;

namespace TestProject.Integration;

public class ParticipantsEndpointsTests : IntegrationTestBase
{
    public ParticipantsEndpointsTests(IntegrationTestFactory factory) : base(factory) { }

    [Fact]
    public async Task PostParticipant_Returns201_OnHappyPath()
    {
        var ev = await SeedEventAsync(maxParticipants: 5);

        var dto = new ParticipantDto
        {
            EventId = ev.Id,
            FirstName = "Alice",
            LastName = "Smith",
            NationalId = "NID-001",
        };

        var response = await Client.PostAsJsonAsync("/api/v1/participants", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<ParticipantDto>();
        body.Should().NotBeNull();
        body!.Id.Should().NotBe(Guid.Empty);
        body.EventId.Should().Be(ev.Id);

        (await CountParticipantsAsync(ev.Id)).Should().Be(1);
    }

    [Fact]
    public async Task PostParticipant_Returns404_WhenEventMissing()
    {
        var dto = new ParticipantDto
        {
            EventId = Guid.NewGuid(),
            FirstName = "Bob",
            LastName = "Jones",
            NationalId = "NID-404",
        };

        var response = await Client.PostAsJsonAsync("/api/v1/participants", dto);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PostParticipant_Returns409_WhenEventFull()
    {
        var ev = await SeedEventAsync(maxParticipants: 1, participantCount: 1);

        var dto = new ParticipantDto
        {
            EventId = ev.Id,
            FirstName = "Carol",
            LastName = "Doe",
            NationalId = "NID-Unique",
        };

        var response = await Client.PostAsJsonAsync("/api/v1/participants", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Event is full");
    }

    [Fact]
    public async Task PostParticipant_Returns409_OnDuplicateNationalId()
    {
        var ev = await SeedEventAsync(maxParticipants: 5);

        var dto = new ParticipantDto
        {
            EventId = ev.Id,
            FirstName = "Dan",
            LastName = "Lee",
            NationalId = "NID-DUP",
        };

        var first = await Client.PostAsJsonAsync("/api/v1/participants", dto);
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var second = await Client.PostAsJsonAsync("/api/v1/participants", dto);
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await second.Content.ReadAsStringAsync();
        body.Should().Contain("Already registered for this event");
    }

    [Fact]
    public async Task GetParticipant_Returns401_WithoutAuth()
    {
        var response = await Client.GetAsync($"/api/v1/participants/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetParticipant_Returns200_WithAuth()
    {
        var ev = await SeedEventAsync(maxParticipants: 5);
        var dto = new ParticipantDto
        {
            EventId = ev.Id,
            FirstName = "Eve",
            LastName = "King",
            NationalId = "NID-200",
        };
        var post = await Client.PostAsJsonAsync("/api/v1/participants", dto);
        post.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await post.Content.ReadFromJsonAsync<ParticipantDto>();

        Authenticate();
        var response = await Client.GetAsync($"/api/v1/participants/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ParticipantDto>();
        body.Should().NotBeNull();
        body!.Id.Should().Be(created.Id);
        body.NationalId.Should().Be("NID-200");
    }
}
