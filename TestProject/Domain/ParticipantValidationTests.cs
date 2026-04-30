using AwesomeAssertions;
using DomainParticipant = App.Domain.Participant;

namespace TestProject.Domain;

public class ParticipantValidationTests
{
    private static DomainParticipant BuildValid() => new()
    {
        EventId = Guid.NewGuid(),
        FirstName = "Jane",
        LastName = "Doe",
        NationalId = "1234567890",
    };

    [Fact]
    public void Validate_AllRequiredFieldsPopulated_ReturnsValid()
    {
        // Arrange
        var p = BuildValid();

        // Act
        var (isValid, results) = TestHelpers.ValidationRunner.Validate(p);

        // Assert
        isValid.Should().BeTrue();
        results.Should().BeEmpty();
    }

    [Fact]
    public void Validate_MissingFirstName_ReturnsValidationError()
    {
        // Arrange
        var p = BuildValid();
        p.FirstName = "";

        // Act
        var (isValid, results) = TestHelpers.ValidationRunner.Validate(p);

        // Assert
        isValid.Should().BeFalse();
        results.Should().Contain(r => r.MemberNames.Contains(nameof(DomainParticipant.FirstName)));
    }

    [Fact]
    public void Validate_MissingLastName_ReturnsValidationError()
    {
        // Arrange
        var p = BuildValid();
        p.LastName = "";

        // Act
        var (isValid, results) = TestHelpers.ValidationRunner.Validate(p);

        // Assert
        isValid.Should().BeFalse();
        results.Should().Contain(r => r.MemberNames.Contains(nameof(DomainParticipant.LastName)));
    }

    [Fact]
    public void Validate_MissingNationalId_ReturnsValidationError()
    {
        // Arrange
        var p = BuildValid();
        p.NationalId = "";

        // Act
        var (isValid, results) = TestHelpers.ValidationRunner.Validate(p);

        // Assert
        isValid.Should().BeFalse();
        results.Should().Contain(r => r.MemberNames.Contains(nameof(DomainParticipant.NationalId)));
    }

    [Fact]
    public void Validate_FirstNameExceedsMaxLength_ReturnsValidationError()
    {
        // Arrange
        var p = BuildValid();
        p.FirstName = new string('a', 51);

        // Act
        var (isValid, results) = TestHelpers.ValidationRunner.Validate(p);

        // Assert
        isValid.Should().BeFalse();
        results.Should().Contain(r => r.MemberNames.Contains(nameof(DomainParticipant.FirstName)));
    }

    [Fact]
    public void Validate_LastNameExceedsMaxLength_ReturnsValidationError()
    {
        // Arrange
        var p = BuildValid();
        p.LastName = new string('b', 51);

        // Act
        var (isValid, results) = TestHelpers.ValidationRunner.Validate(p);

        // Assert
        isValid.Should().BeFalse();
        results.Should().Contain(r => r.MemberNames.Contains(nameof(DomainParticipant.LastName)));
    }

    [Fact]
    public void Validate_NationalIdExceedsMaxLength_ReturnsValidationError()
    {
        // Arrange
        var p = BuildValid();
        p.NationalId = new string('1', 26);

        // Act
        var (isValid, results) = TestHelpers.ValidationRunner.Validate(p);

        // Assert
        isValid.Should().BeFalse();
        results.Should().Contain(r => r.MemberNames.Contains(nameof(DomainParticipant.NationalId)));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(50)]
    public void Validate_FirstNameAtBoundary_ReturnsValid(int length)
    {
        // Arrange
        var p = BuildValid();
        p.FirstName = new string('a', length);

        // Act
        var (isValid, _) = TestHelpers.ValidationRunner.Validate(p);

        // Assert
        isValid.Should().BeTrue();
    }
}
