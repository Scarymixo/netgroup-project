using App.BLL.Services;
using AwesomeAssertions;

namespace TestProject.DTO;

public class ResultRecordsTests
{
    [Fact]
    public void EventCapacityChangeResult_Construction_PreservesStatusAndCount()
    {
        var result = new EventCapacityChangeResult(EventCapacityChangeStatus.BelowCurrentRegistered, 7);
        
        result.Status.Should().Be(EventCapacityChangeStatus.BelowCurrentRegistered);
        result.CurrentRegistered.Should().Be(7);
    }

    [Fact]
    public void EventCapacityChangeResult_RecordEquality_HoldsForSameValues()
    {
        var a = new EventCapacityChangeResult(EventCapacityChangeStatus.Ok, 3);
        var b = new EventCapacityChangeResult(EventCapacityChangeStatus.Ok, 3);
        
        a.Should().Be(b);
    }

    [Fact]
    public void ParticipantRegistrationResult_Construction_PreservesStatusAndParticipant()
    {
        var participant = new App.Domain.Participant
        {
            EventId = Guid.NewGuid(),
            FirstName = "X",
            LastName = "Y",
            NationalId = "Z",
        };
        
        var result = new ParticipantRegistrationResult(ParticipantRegistrationStatus.Ok, participant);
        
        result.Status.Should().Be(ParticipantRegistrationStatus.Ok);
        result.Participant.Should().BeSameAs(participant);
    }

    [Fact]
    public void ParticipantRegistrationResult_FailureCase_AllowsNullParticipant()
    {
        var result = new ParticipantRegistrationResult(ParticipantRegistrationStatus.EventFull, null);
        
        result.Status.Should().Be(ParticipantRegistrationStatus.EventFull);
        result.Participant.Should().BeNull();
    }
}
