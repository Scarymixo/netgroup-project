using Base.Contracts.Domain;

namespace Base.Domain;

public class BaseEntity : IBaseEntity
{
    public Guid Id { get; set; } =  Guid.NewGuid();
}