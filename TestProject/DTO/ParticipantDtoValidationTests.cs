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
        // Arrange
        var dto = BuildValid();

        // Act
        var (isValid, _) = TestHelpers.ValidationRunner.Validate(dto);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_MissingFirstName_ReturnsValidationError()
    {
        // Arrange
        var dto = BuildValid();
        dto.FirstName = "";

        // Act
        var (isValid, results) = TestHelpers.ValidationRunner.Validate(dto);

        // Assert
        isValid.Should().BeFalse();
        results.Should().Contain(r => r.MemberNames.Contains(nameof(DtoParticipant.FirstName)));
    }

    [Fact]
    public void Validate_MissingLastName_ReturnsValidationError()
    {
        // Arrange
        var dto = BuildValid();
        dto.LastName = "";

        // Act
        var (isValid, results) = TestHelpers.ValidationRunner.Validate(dto);

        // Assert
        isValid.Should().BeFalse();
        results.Should().Contain(r => r.MemberNames.Contains(nameof(DtoParticipant.LastName)));
    }

    [Fact]
    public void Validate_MissingNationalId_ReturnsValidationError()
    {
        // Arrange
        var dto = BuildValid();
        dto.NationalId = "";

        // Act
        var (isValid, results) = TestHelpers.ValidationRunner.Validate(dto);

        // Assert
        isValid.Should().BeFalse();
        results.Should().Contain(r => r.MemberNames.Contains(nameof(DtoParticipant.NationalId)));
    }

    [Fact]
    public void Validate_FirstNameExceedsMaxLength_ReturnsValidationError()
    {
        // Arrange
        var dto = BuildValid();
        dto.FirstName = new string('a', 51);

        // Act
        var (isValid, results) = TestHelpers.ValidationRunner.Validate(dto);

        // Assert
        isValid.Should().BeFalse();
        results.Should().Contain(r => r.MemberNames.Contains(nameof(DtoParticipant.FirstName)));
    }

    [Fact]
    public void Validate_NationalIdExceedsMaxLength_ReturnsValidationError()
    {
        // Arrange
        var dto = BuildValid();
        dto.NationalId = new string('1', 26);

        // Act
        var (isValid, results) = TestHelpers.ValidationRunner.Validate(dto);

        // Assert
        isValid.Should().BeFalse();
        results.Should().Contain(r => r.MemberNames.Contains(nameof(DtoParticipant.NationalId)));
    }
}
