using System.ComponentModel.DataAnnotations;
using AwesomeAssertions;
using DtoEvent = App.DTO.v1.Event;

namespace TestProject.DTO;

public class EventDtoValidationTests
{
    private static DtoEvent BuildValid() => new()
    {
        EventName = "Valid",
        MaxParticipants = 10,
        StartTime = DateTime.UtcNow.AddDays(1),
        EndTime = DateTime.UtcNow.AddDays(2),
    };

    [Fact]
    public void Validate_StartTimeInFutureAndEndTimeAfterStart_ReturnsNoErrors()
    {
        var dto = BuildValid();
        
        var results = dto.Validate(new ValidationContext(dto)).ToList();
        
        results.Should().BeEmpty();
    }

    [Fact]
    public void Validate_StartTimeInPast_ReturnsErrorForStartTime()
    {
        var dto = BuildValid();
        dto.StartTime = DateTime.UtcNow.AddDays(-1);
        dto.EndTime = DateTime.UtcNow.AddDays(1);
        
        var results = dto.Validate(new ValidationContext(dto)).ToList();
        
        results.Should().Contain(r => r.MemberNames.Contains(nameof(DtoEvent.StartTime)));
    }

    [Fact]
    public void Validate_EndTimeEqualsStartTime_ReturnsErrorForEndTime()
    {
        var dto = BuildValid();
        var t = DateTime.UtcNow.AddDays(1);
        dto.StartTime = t;
        dto.EndTime = t;
        
        var results = dto.Validate(new ValidationContext(dto)).ToList();
        
        results.Should().Contain(r => r.MemberNames.Contains(nameof(DtoEvent.EndTime)));
    }

    [Fact]
    public void Validate_EndTimeBeforeStartTime_ReturnsErrorForEndTime()
    {
        var dto = BuildValid();
        dto.StartTime = DateTime.UtcNow.AddDays(2);
        dto.EndTime = DateTime.UtcNow.AddDays(1);
        
        var results = dto.Validate(new ValidationContext(dto)).ToList();
        
        results.Should().Contain(r => r.MemberNames.Contains(nameof(DtoEvent.EndTime)));
    }

    [Fact]
    public void DataAnnotations_EventNameEmpty_ReturnsValidationError()
    {
        var dto = BuildValid();
        dto.EventName = "";
        
        var (isValid, results) = TestHelpers.ValidationRunner.Validate(dto);
        
        isValid.Should().BeFalse();
        results.Should().Contain(r => r.MemberNames.Contains(nameof(DtoEvent.EventName)));
    }

    [Fact]
    public void DataAnnotations_EventNameExceedsMaxLength_ReturnsValidationError()
    {
        var dto = BuildValid();
        dto.EventName = new string('x', 129);
        
        var (isValid, results) = TestHelpers.ValidationRunner.Validate(dto);
        
        isValid.Should().BeFalse();
        results.Should().Contain(r => r.MemberNames.Contains(nameof(DtoEvent.EventName)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(100001)]
    public void DataAnnotations_MaxParticipantsOutOfRange_ReturnsValidationError(int max)
    {
        var dto = BuildValid();
        dto.MaxParticipants = max;
        
        var (isValid, results) = TestHelpers.ValidationRunner.Validate(dto);
        
        isValid.Should().BeFalse();
        results.Should().Contain(r => r.MemberNames.Contains(nameof(DtoEvent.MaxParticipants)));
    }
}
