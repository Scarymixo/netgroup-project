using App.DTO.v1.Mappers;
using AwesomeAssertions;
using DomainEvent = App.Domain.Event;
using DtoEvent = App.DTO.v1.Event;

namespace TestProject.Mappers;

public class EventMapperTests
{
    private static DomainEvent BuildEntity(int maxParticipants = 10) => new()
    {
        Id = Guid.NewGuid(),
        EventName = "Conference",
        MaxParticipants = maxParticipants,
        StartTime = new DateTime(2030, 1, 1, 10, 0, 0, DateTimeKind.Utc),
        EndTime = new DateTime(2030, 1, 1, 12, 0, 0, DateTimeKind.Utc),
    };

    [Fact]
    public void Map_EntityWithRegisteredCountLessThanMax_ReturnsPositiveSpotsLeft()
    {
        // Arrange
        var entity = BuildEntity(maxParticipants: 10);

        // Act
        var dto = EventMapper.Map(entity, registeredCount: 3);

        // Assert
        dto.SpotsLeft.Should().Be(7);
    }

    [Fact]
    public void Map_EntityWithRegisteredCountEqualToMax_ReturnsZeroSpotsLeft()
    {
        // Arrange
        var entity = BuildEntity(maxParticipants: 5);

        // Act
        var dto = EventMapper.Map(entity, registeredCount: 5);

        // Assert
        dto.SpotsLeft.Should().Be(0);
    }

    [Fact]
    public void Map_EntityWithRegisteredCountGreaterThanMax_ReturnsZeroSpotsLeft()
    {
        // Arrange
        var entity = BuildEntity(maxParticipants: 5);

        // Act
        var dto = EventMapper.Map(entity, registeredCount: 99);

        // Assert
        dto.SpotsLeft.Should().Be(0);
    }

    [Fact]
    public void Map_EntityWithDefaultRegisteredCount_ReturnsSpotsLeftEqualToMax()
    {
        // Arrange
        var entity = BuildEntity(maxParticipants: 42);

        // Act
        var dto = EventMapper.Map(entity);

        // Assert
        dto.SpotsLeft.Should().Be(42);
    }

    [Theory]
    [InlineData(10, 0, 10)]
    [InlineData(10, 1, 9)]
    [InlineData(10, 9, 1)]
    [InlineData(10, 10, 0)]
    [InlineData(10, 11, 0)]
    [InlineData(1, 0, 1)]
    [InlineData(1, 1, 0)]
    [InlineData(100000, 50000, 50000)]
    public void Map_EntityWithVariousRegisteredCounts_ReturnsExpectedSpotsLeft(int max, int registered, int expected)
    {
        // Arrange
        var entity = BuildEntity(maxParticipants: max);

        // Act
        var dto = EventMapper.Map(entity, registered);

        // Assert
        dto.SpotsLeft.Should().Be(expected);
    }

    [Fact]
    public void Map_Entity_CopiesAllScalarFields()
    {
        // Arrange
        var entity = BuildEntity(maxParticipants: 25);

        // Act
        var dto = EventMapper.Map(entity, registeredCount: 5);

        // Assert
        dto.Id.Should().Be(entity.Id);
        dto.EventName.Should().Be(entity.EventName);
        dto.MaxParticipants.Should().Be(entity.MaxParticipants);
        dto.StartTime.Should().Be(entity.StartTime);
        dto.EndTime.Should().Be(entity.EndTime);
    }

    [Fact]
    public void Map_Dto_ReturnsDomainEntityWithMatchingFields()
    {
        // Arrange
        var dto = new DtoEvent
        {
            Id = Guid.NewGuid(),
            EventName = "Workshop",
            MaxParticipants = 30,
            StartTime = new DateTime(2030, 5, 1, 9, 0, 0, DateTimeKind.Utc),
            EndTime = new DateTime(2030, 5, 1, 17, 0, 0, DateTimeKind.Utc),
            SpotsLeft = 10,
        };

        // Act
        var entity = EventMapper.Map(dto);

        // Assert
        entity.Id.Should().Be(dto.Id);
        entity.EventName.Should().Be(dto.EventName);
        entity.MaxParticipants.Should().Be(dto.MaxParticipants);
        entity.StartTime.Should().Be(dto.StartTime);
        entity.EndTime.Should().Be(dto.EndTime);
    }

    [Fact]
    public void Map_Dto_DoesNotPropagateSpotsLeftToDomain()
    {
        // Arrange — domain Event has no SpotsLeft property; verify mapping is total without it
        var dto = new DtoEvent
        {
            Id = Guid.NewGuid(),
            EventName = "X",
            MaxParticipants = 10,
            StartTime = DateTime.UtcNow.AddDays(1),
            EndTime = DateTime.UtcNow.AddDays(2),
            SpotsLeft = 999,
        };

        // Act
        var entity = EventMapper.Map(dto);

        // Assert — round-trip max stays put; SpotsLeft is intentionally not part of the domain
        entity.MaxParticipants.Should().Be(10);
    }
}
