/// <summary>
/// Code samples for docs/guides/indexers.md
///
/// Snippets in this file:
/// - docs:indexers:basic-interface
/// - docs:indexers:get-tracking
/// - docs:indexers:set-tracking
/// - docs:indexers:backing-dictionary
/// - docs:indexers:onget-callback
/// - docs:indexers:onset-callback
/// - docs:indexers:fallback-to-backing
/// - docs:indexers:reset
/// - docs:indexers:entity-property
/// - docs:indexers:integer-indexer
///
/// Corresponding tests: IndexersSamplesTests.cs
/// </summary>

namespace KnockOff.Documentation.Samples.Guides;

// ============================================================================
// Domain Types
// ============================================================================

public class IdxPropertyInfo
{
    public string? Name { get; set; }
    public object? Value { get; set; }
}

public class IdxConfigValue
{
    public string? Value { get; set; }
}

public interface IIdxEntityProperty
{
    bool IsModified { get; set; }
}

public class IdxEntityProperty : IIdxEntityProperty
{
    public bool IsModified { get; set; }
}

// ============================================================================
// Basic Usage
// ============================================================================

#region docs:indexers:basic-interface
public interface IIdxPropertyStore
{
    IdxPropertyInfo? this[string key] { get; }
}

public interface IIdxReadWriteStore
{
    IdxPropertyInfo? this[string key] { get; set; }
}

[KnockOff]
public partial class IdxPropertyStoreKnockOff : IIdxPropertyStore { }

[KnockOff]
public partial class IdxReadWriteStoreKnockOff : IIdxReadWriteStore { }
#endregion

// ============================================================================
// Entity Property Access (IEntityBase Pattern)
// ============================================================================

#region docs:indexers:entity-property
public interface IIdxEntityBase
{
    IIdxEntityProperty? this[string propertyName] { get; }
    bool IsNew { get; }
}

[KnockOff]
public partial class IdxEntityBaseKnockOff : IIdxEntityBase { }
#endregion

// ============================================================================
// Config Store (Dictionary-Like)
// ============================================================================

public interface IIdxConfigStore
{
    IdxConfigValue? this[string key] { get; }
}

#region docs:indexers:dictionary-like
[KnockOff]
public partial class IdxConfigStoreKnockOff : IIdxConfigStore { }
#endregion

// ============================================================================
// Integer Indexers
// ============================================================================

#region docs:indexers:integer-indexer
public interface IIdxList
{
    object? this[int index] { get; set; }
}

[KnockOff]
public partial class IdxListKnockOff : IIdxList { }
#endregion
