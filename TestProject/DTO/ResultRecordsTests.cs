using App.BLL.Services;
using AwesomeAssertions;

namespace TestProject.DTO;

public class ResultRecordsTests
{
    [Fact]
    public void EventCapacityChangeResult_Construction_PreservesStatusAndCount()
    {
        // Arrange & Act
        var result = new EventCapacityChangeResult(EventCapacityChangeStatus.BelowCurrentRegistered, 7);

        // Assert
        result.Status.Should().Be(EventCapacityChangeStatus.BelowCurrentRegistered);
        result.CurrentRegistered.Should().Be(7);
    }

    [Fact]
    public void EventCapacityChangeResult_RecordEquality_HoldsForSameValues()
    {
        // Arrange
        var a = new EventCapacityChangeResult(EventCapacityChangeStatus.Ok, 3);
        var b = new EventCapacityChangeResult(EventCapacityChangeStatus.Ok, 3);

        // Act & Assert
        a.Should().Be(b);
    }

    [Fact]
    public void ParticipantRegistrationResult_Construction_PreservesStatusAndParticipant()
    {
        // Arrange
        var participant = new App.Domain.Participant
        {
            EventId = Guid.NewGuid(),
            FirstName = "X",
            LastName = "Y",
            NationalId = "Z",
        };

        // Act
        var result = new ParticipantRegistrationResult(ParticipantRegistrationStatus.Ok, participant);

        // Assert
        result.Status.Should().Be(ParticipantRegistrationStatus.Ok);
        result.Participant.Should().BeSameAs(participant);
    }

    [Fact]
    public void ParticipantRegistrationResult_FailureCase_AllowsNullParticipant()
    {
        // Arrange & Act
        var result = new ParticipantRegistrationResult(ParticipantRegistrationStatus.EventFull, null);

        // Assert
        result.Status.Should().Be(ParticipantRegistrationStatus.EventFull);
        result.Participant.Should().BeNull();
    }
}
