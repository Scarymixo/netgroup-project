using App.DTO.v1.Mappers;
using AwesomeAssertions;
using DomainParticipant = App.Domain.Participant;
using DtoParticipant = App.DTO.v1.Participant;

namespace TestProject.Mappers;

public class ParticipantMapperTests
{
    [Fact]
    public void Map_Entity_CopiesAllFields()
    {
        var entity = new DomainParticipant
        {
            Id = Guid.NewGuid(),
            EventId = Guid.NewGuid(),
            FirstName = "Jane",
            LastName = "Doe",
            NationalId = "1234567890",
        };

        var dto = ParticipantMapper.Map(entity);
        
        dto.Id.Should().Be(entity.Id);
        dto.EventId.Should().Be(entity.EventId);
        dto.FirstName.Should().Be(entity.FirstName);
        dto.LastName.Should().Be(entity.LastName);
        dto.NationalId.Should().Be(entity.NationalId);
    }

    [Fact]
    public void Map_Dto_ReturnsDomainEntityWithMatchingFields()
    {
        var dto = new DtoParticipant
        {
            Id = Guid.NewGuid(),
            EventId = Guid.NewGuid(),
            FirstName = "John",
            LastName = "Smith",
            NationalId = "987654",
        };

        var entity = ParticipantMapper.Map(dto);
        
        entity.Id.Should().Be(dto.Id);
        entity.EventId.Should().Be(dto.EventId);
        entity.FirstName.Should().Be(dto.FirstName);
        entity.LastName.Should().Be(dto.LastName);
        entity.NationalId.Should().Be(dto.NationalId);
    }

    [Fact]
    public void Map_DtoWithEmptyGuidId_PreservesEmptyGuid()
    {
        // Arrange — mapper does not generate Ids; that is the service's responsibility
        var dto = new DtoParticipant
        {
            Id = Guid.Empty,
            EventId = Guid.NewGuid(),
            FirstName = "A",
            LastName = "B",
            NationalId = "C",
        };

        var entity = ParticipantMapper.Map(dto);
        
        entity.Id.Should().Be(Guid.Empty);
    }
}
