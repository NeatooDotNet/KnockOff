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

#region interface-inheritance-basic-usage
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

#region interface-inheritance-deep-inheritance
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

#region interface-inheritance-entity-base
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

#region interface-inheritance-validation-pattern
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

#region interface-inheritance-repository-hierarchy
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

// ============================================================================
// Usage Examples
// ============================================================================

/// <summary>
/// Usage examples demonstrating interface inheritance patterns.
/// Each method is compilable; snippets extract key portions.
/// </summary>
public static class InterfaceInheritanceUsageExamples
{
    public static void TrackingExample()
    {
        var knockOff = new IhAuditableEntityKnockOff();
        IIhAuditableEntity entity = knockOff;

        #region interface-inheritance-tracking
        // Access base interface properties
        var id = entity.Id;
        var created = entity.CreatedAt;

        // Access derived interface properties
        entity.ModifiedAt = DateTime.Now;
        entity.ModifiedBy = "TestUser";

        // All members tracked directly on the stub (flat API)
        var idCount = knockOff.Id.GetCount;           // 1
        var createdCount = knockOff.CreatedAt.GetCount; // 1
        var modAtCount = knockOff.ModifiedAt.SetCount;  // 1
        var modByCount = knockOff.ModifiedBy.SetCount;  // 1
        #endregion

        _ = (id, created, idCount, createdCount, modAtCount, modByCount);
    }

    public static void AsMethodsExample()
    {
        var knockOff = new IhAuditableEntityKnockOff();

        #region interface-inheritance-interface-access
        // Access as derived interface via implicit conversion
        IIhAuditableEntity auditable = knockOff;

        // Access as base interface
        IIhBaseEntity baseEntity = knockOff;

        // Same underlying instance
        var areSame = ReferenceEquals(knockOff, auditable);  // true
        #endregion

        _ = (auditable, baseEntity, areSame);
    }

    public static void CallbacksExample()
    {
        var knockOff = new IhAuditableEntityKnockOff();

        #region interface-inheritance-callbacks
        // Base interface member
        knockOff.Id.OnGet = (ko) => 42;

        // Derived interface member
        knockOff.ModifiedBy.OnGet = (ko) => "System";
        knockOff.ModifiedAt.OnSet = (ko, value) =>
        {
            Console.WriteLine($"Modified at {value}");
        };
        #endregion
    }

    public static void ValidationPatternUsage()
    {
        var knockOff = new IhOrderKnockOff();
        var totalAccessCount = 0;

        #region interface-inheritance-validation-usage
        // Configure validation (flat API)
        knockOff.Total.OnGet = (ko) => { totalAccessCount++; return 100m; };
        knockOff.IsValid.OnGet = (ko) => totalAccessCount > 0;
        knockOff.GetErrors.OnCall(ko =>
            knockOff.IsValid.OnGet!(ko) ? [] : ["No total calculated"]);
        #endregion
    }

    public static void RepositoryUsage()
    {
        var knockOff = new IhUserWriteRepositoryKnockOff();
        var users = new List<IhUser>();

        #region interface-inheritance-repository-usage
        // All members accessed via flat API regardless of declaring interface
        knockOff.GetById.OnCall((ko, id) => users.FirstOrDefault(u => u.Id == id));
        knockOff.GetAll.OnCall(ko => users);
        knockOff.Add.OnCall((ko, user) => users.Add(user));
        knockOff.Delete.OnCall((ko, id) => users.RemoveAll(u => u.Id == id));
        #endregion
    }
}
