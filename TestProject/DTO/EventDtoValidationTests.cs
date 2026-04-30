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
        // Arrange
        var dto = BuildValid();

        // Act
        var results = dto.Validate(new ValidationContext(dto)).ToList();

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public void Validate_StartTimeInPast_ReturnsErrorForStartTime()
    {
        // Arrange
        var dto = BuildValid();
        dto.StartTime = DateTime.UtcNow.AddDays(-1);
        dto.EndTime = DateTime.UtcNow.AddDays(1);

        // Act
        var results = dto.Validate(new ValidationContext(dto)).ToList();

        // Assert
        results.Should().Contain(r => r.MemberNames.Contains(nameof(DtoEvent.StartTime)));
    }

    [Fact]
    public void Validate_EndTimeEqualsStartTime_ReturnsErrorForEndTime()
    {
        // Arrange
        var dto = BuildValid();
        var t = DateTime.UtcNow.AddDays(1);
        dto.StartTime = t;
        dto.EndTime = t;

        // Act
        var results = dto.Validate(new ValidationContext(dto)).ToList();

        // Assert
        results.Should().Contain(r => r.MemberNames.Contains(nameof(DtoEvent.EndTime)));
    }

    [Fact]
    public void Validate_EndTimeBeforeStartTime_ReturnsErrorForEndTime()
    {
        // Arrange
        var dto = BuildValid();
        dto.StartTime = DateTime.UtcNow.AddDays(2);
        dto.EndTime = DateTime.UtcNow.AddDays(1);

        // Act
        var results = dto.Validate(new ValidationContext(dto)).ToList();

        // Assert
        results.Should().Contain(r => r.MemberNames.Contains(nameof(DtoEvent.EndTime)));
    }

    [Fact]
    public void DataAnnotations_EventNameEmpty_ReturnsValidationError()
    {
        // Arrange
        var dto = BuildValid();
        dto.EventName = "";

        // Act
        var (isValid, results) = TestHelpers.ValidationRunner.Validate(dto);

        // Assert
        isValid.Should().BeFalse();
        results.Should().Contain(r => r.MemberNames.Contains(nameof(DtoEvent.EventName)));
    }

    [Fact]
    public void DataAnnotations_EventNameExceedsMaxLength_ReturnsValidationError()
    {
        // Arrange
        var dto = BuildValid();
        dto.EventName = new string('x', 129);

        // Act
        var (isValid, results) = TestHelpers.ValidationRunner.Validate(dto);

        // Assert
        isValid.Should().BeFalse();
        results.Should().Contain(r => r.MemberNames.Contains(nameof(DtoEvent.EventName)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(100001)]
    public void DataAnnotations_MaxParticipantsOutOfRange_ReturnsValidationError(int max)
    {
        // Arrange
        var dto = BuildValid();
        dto.MaxParticipants = max;

        // Act
        var (isValid, results) = TestHelpers.ValidationRunner.Validate(dto);

        // Assert
        isValid.Should().BeFalse();
        results.Should().Contain(r => r.MemberNames.Contains(nameof(DtoEvent.MaxParticipants)));
    }
}
