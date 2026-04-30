using AwesomeAssertions;
using DtoParticipant = App.DTO.v1.Participant;

namespace TestProject.DTO;

public class ParticipantDtoValidationTests
{
    private static DtoParticipant BuildValid() => new()
    {
        Id = Guid.NewGuid(),
        EventId = Guid.NewGuid(),
        FirstName = "Jane",
        LastName = "Doe",
        NationalId = "12345",
    };

    [Fact]
    public void Validate_AllRequiredFieldsPopulated_ReturnsValid()
    {
        var dto = BuildValid();
        
        var (isValid, _) = TestHelpers.ValidationRunner.Validate(dto);
        
        isValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_MissingFirstName_ReturnsValidationError()
    {
        var dto = BuildValid();
        dto.FirstName = "";
        
        var (isValid, results) = TestHelpers.ValidationRunner.Validate(dto);
        
        isValid.Should().BeFalse();
        results.Should().Contain(r => r.MemberNames.Contains(nameof(DtoParticipant.FirstName)));
    }

    [Fact]
    public void Validate_MissingLastName_ReturnsValidationError()
    {
        var dto = BuildValid();
        dto.LastName = "";
        
        var (isValid, results) = TestHelpers.ValidationRunner.Validate(dto);
        
        isValid.Should().BeFalse();
        results.Should().Contain(r => r.MemberNames.Contains(nameof(DtoParticipant.LastName)));
    }

    [Fact]
    public void Validate_MissingNationalId_ReturnsValidationError()
    {
        var dto = BuildValid();
        dto.NationalId = "";
        
        var (isValid, results) = TestHelpers.ValidationRunner.Validate(dto);
        
        isValid.Should().BeFalse();
        results.Should().Contain(r => r.MemberNames.Contains(nameof(DtoParticipant.NationalId)));
    }

    [Fact]
    public void Validate_FirstNameExceedsMaxLength_ReturnsValidationError()
    {
        var dto = BuildValid();
        dto.FirstName = new string('a', 51);
        
        var (isValid, results) = TestHelpers.ValidationRunner.Validate(dto);
        
        isValid.Should().BeFalse();
        results.Should().Contain(r => r.MemberNames.Contains(nameof(DtoParticipant.FirstName)));
    }

    [Fact]
    public void Validate_NationalIdExceedsMaxLength_ReturnsValidationError()
    {
        var dto = BuildValid();
        dto.NationalId = new string('1', 26);
        
        var (isValid, results) = TestHelpers.ValidationRunner.Validate(dto);
        
        isValid.Should().BeFalse();
        results.Should().Contain(r => r.MemberNames.Contains(nameof(DtoParticipant.NationalId)));
    }
}
