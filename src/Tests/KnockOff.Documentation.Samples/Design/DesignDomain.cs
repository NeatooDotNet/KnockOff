namespace KnockOff.Documentation.Samples.Design;

/// <summary>
/// Domain types used in API design examples.
/// </summary>

public class DsUser
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

/// <summary>
/// Interface with method, property, and indexer for design examples.
/// </summary>
public interface IDsUserService
{
    // Method
    DsUser? GetUser(int id);

    // Property
    string Name { get; set; }

    // Indexer
    string? this[string key] { get; set; }
}

/// <summary>
/// Class with virtual method, property, and indexer for design examples.
/// </summary>
public class DsEmailService
{
    // Method
    public virtual void Send(string to, string body) { }

    // Property
    public virtual string ServerName { get; set; } = "";

    // Indexer
    public virtual string? this[int index]
    {
        get => null;
        set { }
    }
}

// =============================================================================
// INDEXER EXAMPLES
// =============================================================================

/// <summary>
/// Interface with multiple indexer types for indexer documentation.
/// </summary>
public interface IDsCache
{
    // String-keyed indexer
    string? this[string key] { get; set; }
}

/// <summary>
/// Interface with multiple indexers (different key types).
/// </summary>
public interface IDsMultiIndexer
{
    string? this[string key] { get; set; }
    string? this[int index] { get; set; }
}

// =============================================================================
// INIT PROPERTY EXAMPLES
// =============================================================================

/// <summary>
/// Interface with init-only properties for immutable entity patterns.
/// </summary>
public interface IDsEntity
{
    string Id { get; init; }
    string Name { get; init; }
}

/// <summary>
/// Interface mixing init and regular properties.
/// </summary>
public interface IDsDocument
{
    string Id { get; init; }       // Immutable after creation
    string Title { get; set; }     // Mutable
    int Version { get; }           // Read-only
}

/// <summary>
/// Abstract class with virtual init property.
/// </summary>
public abstract class DsEntityBase
{
    public virtual string Id { get; init; } = "";
    public virtual string CreatedBy { get; init; } = "";
}

// =============================================================================
// REQUIRED PROPERTY EXAMPLES
// =============================================================================

/// <summary>
/// Abstract class with required properties (C# 11+).
/// Required properties must be set during object initialization.
/// </summary>
public abstract class DsAuditableEntity
{
    public required virtual string Id { get; set; }
    public required virtual string CreatedBy { get; set; }
    public virtual DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Abstract class combining required and init properties.
/// </summary>
public abstract class DsImmutableEntity
{
    public required virtual string Id { get; init; }
    public required virtual string Name { get; init; }
    public virtual int Version { get; set; } = 1;
}
