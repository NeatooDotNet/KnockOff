/// <summary>
/// Code samples for docs/guides/interface-inheritance.md
///
/// Snippets in this file:
/// - docs:interface-inheritance:basic-usage
/// - docs:interface-inheritance:tracking
/// - docs:interface-inheritance:as-methods
/// - docs:interface-inheritance:callbacks
/// - docs:interface-inheritance:deep-inheritance
/// - docs:interface-inheritance:entity-base
/// - docs:interface-inheritance:validation-pattern
/// - docs:interface-inheritance:repository-hierarchy
///
/// Corresponding tests: InterfaceInheritanceSamplesTests.cs
/// </summary>

namespace KnockOff.Documentation.Samples.Guides;

// ============================================================================
// Domain Types
// ============================================================================

public class IhUser
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

// ============================================================================
// Basic Usage
// ============================================================================

#region docs:interface-inheritance:basic-usage
public interface IIhBaseEntity
{
    int Id { get; }
    DateTime CreatedAt { get; }
}

public interface IIhAuditableEntity : IIhBaseEntity
{
    DateTime? ModifiedAt { get; set; }
    string ModifiedBy { get; set; }
}

[KnockOff]
public partial class IhAuditableEntityKnockOff : IIhAuditableEntity { }
#endregion

// ============================================================================
// Deep Inheritance
// ============================================================================

#region docs:interface-inheritance:deep-inheritance
public interface IIhEntity
{
    int Id { get; }
}

public interface IIhTimestampedEntity : IIhEntity
{
    DateTime CreatedAt { get; }
}

public interface IIhFullAuditableEntity : IIhTimestampedEntity
{
    string CreatedBy { get; }
    string? ModifiedBy { get; set; }
}

[KnockOff]
public partial class IhFullEntityKnockOff : IIhFullAuditableEntity { }
#endregion

// ============================================================================
// Entity Base Pattern
// ============================================================================

#region docs:interface-inheritance:entity-base
public interface IIhEntityBase
{
    int Id { get; }
}

public interface IIhEmployee : IIhEntityBase
{
    string Name { get; set; }
    string Department { get; set; }
}

[KnockOff]
public partial class IhEmployeeKnockOff : IIhEmployee { }
#endregion

// ============================================================================
// Validation Pattern
// ============================================================================

#region docs:interface-inheritance:validation-pattern
public interface IIhValidatable
{
    bool IsValid { get; }
    IEnumerable<string> GetErrors();
}

public interface IIhOrder : IIhValidatable
{
    decimal Total { get; }
    void Submit();
}

[KnockOff]
public partial class IhOrderKnockOff : IIhOrder { }
#endregion

// ============================================================================
// Repository Hierarchy
// ============================================================================

#region docs:interface-inheritance:repository-hierarchy
public interface IIhReadRepository<T>
{
    T? GetById(int id);
    IEnumerable<T> GetAll();
}

public interface IIhWriteRepository<T> : IIhReadRepository<T>
{
    void Add(T entity);
    void Delete(int id);
}

[KnockOff]
public partial class IhUserWriteRepositoryKnockOff : IIhWriteRepository<IhUser> { }
#endregion
