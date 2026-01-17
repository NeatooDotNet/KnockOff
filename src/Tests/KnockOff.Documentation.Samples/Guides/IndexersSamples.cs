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
/// - indexers-single-interceptor-access
/// - indexers-multiple-interceptor-access
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

#region indexers-basic-interface
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

#region indexers-entity-property
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

#region indexers-dictionary-like
[KnockOff]
public partial class IdxConfigStoreKnockOff : IIdxConfigStore { }
#endregion

// ============================================================================
// Integer Indexers
// ============================================================================

#region indexers-integer-indexer
public interface IIdxList
{
    object? this[int index] { get; set; }
}

[KnockOff]
public partial class IdxListKnockOff : IIdxList { }
#endregion

// ============================================================================
// Multiple Indexers (different key types)
// ============================================================================

public interface IIdxMultiStore
{
    object? this[string key] { get; set; }
    object? this[int index] { get; set; }
}

[KnockOff]
public partial class IdxMultiStoreKnockOff : IIdxMultiStore { }

// ============================================================================
// Usage Examples
// ============================================================================

/// <summary>
/// Usage examples demonstrating indexer patterns.
/// Each method is compilable; snippets extract key portions.
/// </summary>
public static class IndexersUsageExamples
{
    public static void GetTracking()
    {
        var knockOff = new IdxReadWriteStoreKnockOff();
        IIdxReadWriteStore store = knockOff;

        #region indexers-get-tracking
        _ = store["Name"];
        _ = store["Age"];

        var getCount = knockOff.Indexer.GetCount;       // 2
        var lastKey = knockOff.Indexer.LastGetKey;      // "Age"
        #endregion

        _ = (getCount, lastKey);
    }

    public static void SetTracking()
    {
        var knockOff = new IdxReadWriteStoreKnockOff();
        IIdxReadWriteStore store = knockOff;
        var value1 = new IdxPropertyInfo { Name = "Test", Value = 123 };

        #region indexers-set-tracking
        store["Key"] = value1;

        var setCount = knockOff.Indexer.SetCount;         // 1
        var lastEntry = knockOff.Indexer.LastSetEntry;
        var lastSetKey = lastEntry?.Key;                        // "Key"
        var lastSetValue = lastEntry?.Value;                    // value1
        #endregion

        _ = (setCount, lastSetKey, lastSetValue);
    }

    public static void BackingDictionary()
    {
        var knockOff = new IdxReadWriteStoreKnockOff();
        IIdxReadWriteStore store = knockOff;

        #region indexers-backing-dictionary
        // Pre-populate backing dictionary
        knockOff.Indexer.Backing["Config"] = new IdxPropertyInfo { Value = "Value1" };
        knockOff.Indexer.Backing["Setting"] = new IdxPropertyInfo { Value = "Value2" };

        // Access returns backing values
        var config = store["Config"];   // Returns the pre-populated value
        var setting = store["Setting"]; // Returns the pre-populated value
        #endregion

        _ = (config, setting);
    }

    public static void OnGetCallback()
    {
        var knockOff = new IdxReadWriteStoreKnockOff();
        IIdxReadWriteStore store = knockOff;

        #region indexers-onget-callback
        knockOff.Indexer.OnGet = (ko, key) =>
        {
            // Compute or fetch value dynamically
            return new IdxPropertyInfo { Name = key, Value = key.Length };
        };

        var result = store["Hello"];  // Returns IdxPropertyInfo with Value = 5
        #endregion

        _ = result;
    }

    public static void OnSetCallback()
    {
        var knockOff = new IdxReadWriteStoreKnockOff();
        IIdxReadWriteStore store = knockOff;
        var changes = new List<(string key, IdxPropertyInfo? value)>();

        #region indexers-onset-callback
        knockOff.Indexer.OnSet = (ko, key, value) =>
        {
            changes.Add((key, value));
        };

        store["Key1"] = new IdxPropertyInfo { Value = "A" };
        store["Key2"] = new IdxPropertyInfo { Value = "B" };

        // changes contains [("Key1", ...), ("Key2", ...)]
        #endregion

        _ = changes;
    }

    public static void FallbackToBacking()
    {
        var knockOff = new IdxReadWriteStoreKnockOff();
        IIdxReadWriteStore store = knockOff;

        #region indexers-fallback-to-backing
        // No OnGet callback - falls back to backing dictionary
        knockOff.Indexer.Backing["Existing"] = new IdxPropertyInfo { Value = "Found" };

        var existing = store["Existing"];  // Returns backing value
        var missing = store["Missing"];    // Returns null (not in backing)
        #endregion

        _ = (existing, missing);
    }

    public static void ResetBehavior()
    {
        var knockOff = new IdxReadWriteStoreKnockOff();
        IIdxReadWriteStore store = knockOff;

        _ = store["Test"];
        knockOff.Indexer.OnGet = (ko, key) => new IdxPropertyInfo();

        #region indexers-reset
        knockOff.Indexer.Reset();

        var getCount = knockOff.Indexer.GetCount;    // 0
        var onGet = knockOff.Indexer.OnGet;          // null
        // Note: Backing dictionary is NOT cleared
        #endregion

        _ = (getCount, onGet);
    }

    public static void IntegerIndexer()
    {
        var knockOff = new IdxListKnockOff();
        IIdxList list = knockOff;

        #region indexers-integer-indexer-usage
        knockOff.Indexer.Backing[0] = "First";
        knockOff.Indexer.Backing[1] = "Second";

        var first = list[0];   // "First"
        var second = list[1];  // "Second"

        var lastGetIndex = knockOff.Indexer.LastGetKey;  // 1
        #endregion

        _ = (first, second, lastGetIndex);
    }

    public static void SingleIndexerAccess()
    {
        var knockOff = new IdxPropertyStoreKnockOff();

        #region indexers-single-interceptor-access
        _ = knockOff.Indexer;          // for this[string key]
        _ = knockOff.Indexer.Backing;  // Dictionary backing storage
        #endregion
    }

    public static void MultipleIndexerAccess()
    {
        var knockOff = new IdxMultiStoreKnockOff();

        #region indexers-multiple-interceptor-access
        _ = knockOff.Indexer.OfString;          // for this[string key]
        _ = knockOff.Indexer.OfInt32;           // for this[int index]
        _ = knockOff.Indexer.OfString.Backing;
        _ = knockOff.Indexer.OfInt32.Backing;
        #endregion
    }
}
