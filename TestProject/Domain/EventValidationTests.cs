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
        var ev = BuildValid();
        
        var results = ev.Validate(new ValidationContext(ev)).ToList();
        
        results.Should().BeEmpty();
    }

    [Fact]
    public void Validate_StartTimeInPast_ReturnsErrorForStartTime()
    {
        var ev = BuildValid();
        ev.StartTime = DateTime.UtcNow.AddDays(-1);
        ev.EndTime = DateTime.UtcNow.AddDays(1);
        
        var results = ev.Validate(new ValidationContext(ev)).ToList();
        
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
        
        var results = ev.Validate(new ValidationContext(ev)).ToList();
        
        results.Should().Contain(r => r.MemberNames.Contains(nameof(DomainEvent.StartTime)));
    }

    [Fact]
    public void Validate_EndTimeBeforeStartTime_ReturnsErrorForEndTime()
    {
        var ev = BuildValid();
        ev.StartTime = DateTime.UtcNow.AddDays(2);
        ev.EndTime = DateTime.UtcNow.AddDays(1);

        var results = ev.Validate(new ValidationContext(ev)).ToList();
        
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
        
        var results = ev.Validate(new ValidationContext(ev)).ToList();
        
        results.Should().Contain(r => r.MemberNames.Contains(nameof(DomainEvent.EndTime)));
    }

    [Fact]
    public void Validate_StartTimePastAndEndTimeBeforeStart_ReturnsBothErrors()
    {
        var ev = BuildValid();
        ev.StartTime = DateTime.UtcNow.AddDays(-1);
        ev.EndTime = DateTime.UtcNow.AddDays(-2);
        
        var results = ev.Validate(new ValidationContext(ev)).ToList();
        
        results.Should().HaveCount(2);
        results.Should().Contain(r => r.MemberNames.Contains(nameof(DomainEvent.StartTime)));
        results.Should().Contain(r => r.MemberNames.Contains(nameof(DomainEvent.EndTime)));
    }

    [Fact]
    public void DataAnnotations_EventNameEmpty_ReturnsValidationError()
    {
        var ev = BuildValid();
        ev.EventName = "";
        
        var (isValid, results) = TestHelpers.ValidationRunner.Validate(ev);

        isValid.Should().BeFalse();
        results.Should().Contain(r => r.MemberNames.Contains(nameof(DomainEvent.EventName)));
    }

    [Fact]
    public void DataAnnotations_EventNameExceedsMaxLength_ReturnsValidationError()
    {
        var ev = BuildValid();
        ev.EventName = new string('x', 129);

        var (isValid, results) = TestHelpers.ValidationRunner.Validate(ev);

        isValid.Should().BeFalse();
        results.Should().Contain(r => r.MemberNames.Contains(nameof(DomainEvent.EventName)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(100001)]
    public void DataAnnotations_MaxParticipantsOutOfRange_ReturnsValidationError(int max)
    {
        var ev = BuildValid();
        ev.MaxParticipants = max;

        var (isValid, results) = TestHelpers.ValidationRunner.Validate(ev);

        isValid.Should().BeFalse();
        results.Should().Contain(r => r.MemberNames.Contains(nameof(DomainEvent.MaxParticipants)));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(50000)]
    [InlineData(100000)]
    public void DataAnnotations_MaxParticipantsAtBoundary_ReturnsValid(int max)
    {
        var ev = BuildValid();
        ev.MaxParticipants = max;

        var (isValid, _) = TestHelpers.ValidationRunner.Validate(ev);

        isValid.Should().BeTrue();
    }
}
