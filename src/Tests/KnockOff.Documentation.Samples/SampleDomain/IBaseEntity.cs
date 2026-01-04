namespace KnockOff.Documentation.Samples.SampleDomain;

/// <summary>
/// Base entity interface for interface inheritance examples.
/// </summary>
public interface IBaseEntity
{
    int Id { get; set; }
    bool IsNew { get; }
}

/// <summary>
/// Auditable entity interface extending base entity.
/// </summary>
public interface IAuditableEntity : IBaseEntity
{
    DateTime CreatedAt { get; set; }
    DateTime? ModifiedAt { get; set; }
    string CreatedBy { get; set; }
}

/// <summary>
/// Soft-deletable entity interface extending auditable entity.
/// </summary>
public interface ISoftDeletable : IAuditableEntity
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAt { get; set; }
}
