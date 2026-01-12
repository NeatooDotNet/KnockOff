/// <summary>
/// Code samples for docs/reference/attributes.md
///
/// Snippets in this file:
/// - attributes-knockoff-usage
/// - attributes-valid-examples
/// - attributes-namespace-qualified
///
/// Corresponding tests: AttributesSamplesTests.cs
/// </summary>

namespace KnockOff.Documentation.Samples.Reference;

// ============================================================================
// Domain Types for Attribute Samples
// ============================================================================

public interface IAttrService
{
    void DoWork();
}

public interface IAttrRepository<T>
{
    T? GetById(int id);
}

public interface IAttrAuditableEntity
{
    DateTime CreatedAt { get; }
    string CreatedBy { get; }
}

public class AttrUser
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

// ============================================================================
// Basic Usage
// ============================================================================

#region attributes-knockoff-usage
[KnockOff]
public partial class AttrMyServiceKnockOff : IAttrService
{
}
#endregion

// ============================================================================
// Valid Examples
// ============================================================================

#region attributes-valid-examples
// Basic usage
[KnockOff]
public partial class AttrServiceKnockOff : IAttrService { }

// Generic interface (with concrete type)
[KnockOff]
public partial class AttrUserRepoKnockOff : IAttrRepository<AttrUser> { }

// Interface inheritance
[KnockOff]
public partial class AttrAuditableKnockOff : IAttrAuditableEntity { }

// Internal class
[KnockOff]
internal partial class AttrInternalServiceKnockOff : IAttrService { }

// Nested class
public partial class AttrTestFixture
{
    [KnockOff]
    public partial class NestedKnockOff : IAttrService { }
}
#endregion

// ============================================================================
// Namespace Qualified Usage
// ============================================================================

#region attributes-namespace-qualified
[KnockOff.KnockOff]
public partial class AttrQualifiedServiceKnockOff : IAttrService { }
#endregion
