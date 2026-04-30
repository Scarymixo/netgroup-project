using System.ComponentModel.DataAnnotations;
using AwesomeAssertions;
using DomainEvent = App.Domain.Event;

namespace TestProject.Domain;

public class EventValidationTests
{
    private static DomainEvent BuildValid() => new()
    {
        EventName = "Valid",
        MaxParticipants = 10,
        StartTime = DateTime.UtcNow.AddDays(1),
        EndTime = DateTime.UtcNow.AddDays(2),
    };

    [Fact]
    public void Validate_StartTimeInFutureAndEndTimeAfterStart_ReturnsNoErrors()
    {
        // Arrange
        var ev = BuildValid();

        // Act
        var results = ev.Validate(new ValidationContext(ev)).ToList();

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public void Validate_StartTimeInPast_ReturnsErrorForStartTime()
    {
        // Arrange
        var ev = BuildValid();
        ev.StartTime = DateTime.UtcNow.AddDays(-1);
        ev.EndTime = DateTime.UtcNow.AddDays(1);

        // Act
        var results = ev.Validate(new ValidationContext(ev)).ToList();

        // Assert
        results.Should().ContainSingle(r => r.MemberNames.Contains(nameof(DomainEvent.StartTime)));
    }

    [Fact]
    public void Validate_StartTimeEqualsNow_ReturnsErrorForStartTime()
    {
        // Arrange — boundary: <= now is rejected
        var now = DateTime.UtcNow;
        var ev = BuildValid();
        ev.StartTime = now;
        ev.EndTime = now.AddHours(1);

        // Act
        var results = ev.Validate(new ValidationContext(ev)).ToList();

        // Assert
        results.Should().Contain(r => r.MemberNames.Contains(nameof(DomainEvent.StartTime)));
    }

    [Fact]
    public void Validate_EndTimeBeforeStartTime_ReturnsErrorForEndTime()
    {
        // Arrange
        var ev = BuildValid();
        ev.StartTime = DateTime.UtcNow.AddDays(2);
        ev.EndTime = DateTime.UtcNow.AddDays(1);

        // Act
        var results = ev.Validate(new ValidationContext(ev)).ToList();

        // Assert
        results.Should().Contain(r => r.MemberNames.Contains(nameof(DomainEvent.EndTime)));
    }

    [Fact]
    public void Validate_EndTimeEqualsStartTime_ReturnsErrorForEndTime()
    {
        // Arrange — boundary: EndTime must be strictly greater than StartTime
        var ev = BuildValid();
        var t = DateTime.UtcNow.AddDays(1);
        ev.StartTime = t;
        ev.EndTime = t;

        // Act
        var results = ev.Validate(new ValidationContext(ev)).ToList();

        // Assert
        results.Should().Contain(r => r.MemberNames.Contains(nameof(DomainEvent.EndTime)));
    }

    [Fact]
    public void Validate_StartTimePastAndEndTimeBeforeStart_ReturnsBothErrors()
    {
        // Arrange
        var ev = BuildValid();
        ev.StartTime = DateTime.UtcNow.AddDays(-1);
        ev.EndTime = DateTime.UtcNow.AddDays(-2);

        // Act
        var results = ev.Validate(new ValidationContext(ev)).ToList();

        // Assert
        results.Should().HaveCount(2);
        results.Should().Contain(r => r.MemberNames.Contains(nameof(DomainEvent.StartTime)));
        results.Should().Contain(r => r.MemberNames.Contains(nameof(DomainEvent.EndTime)));
    }

    [Fact]
    public void DataAnnotations_EventNameEmpty_ReturnsValidationError()
    {
        // Arrange
        var ev = BuildValid();
        ev.EventName = "";

        // Act
        var (isValid, results) = TestHelpers.ValidationRunner.Validate(ev);

        // Assert
        isValid.Should().BeFalse();
        results.Should().Contain(r => r.MemberNames.Contains(nameof(DomainEvent.EventName)));
    }

    [Fact]
    public void DataAnnotations_EventNameExceedsMaxLength_ReturnsValidationError()
    {
        // Arrange
        var ev = BuildValid();
        ev.EventName = new string('x', 129);

        // Act
        var (isValid, results) = TestHelpers.ValidationRunner.Validate(ev);

        // Assert
        isValid.Should().BeFalse();
        results.Should().Contain(r => r.MemberNames.Contains(nameof(DomainEvent.EventName)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(100001)]
    public void DataAnnotations_MaxParticipantsOutOfRange_ReturnsValidationError(int max)
    {
        // Arrange
        var ev = BuildValid();
        ev.MaxParticipants = max;

        // Act
        var (isValid, results) = TestHelpers.ValidationRunner.Validate(ev);

        // Assert
        isValid.Should().BeFalse();
        results.Should().Contain(r => r.MemberNames.Contains(nameof(DomainEvent.MaxParticipants)));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(50000)]
    [InlineData(100000)]
    public void DataAnnotations_MaxParticipantsAtBoundary_ReturnsValid(int max)
    {
        // Arrange
        var ev = BuildValid();
        ev.MaxParticipants = max;

        // Act
        var (isValid, _) = TestHelpers.ValidationRunner.Validate(ev);

        // Assert
        isValid.Should().BeTrue();
    }
}
