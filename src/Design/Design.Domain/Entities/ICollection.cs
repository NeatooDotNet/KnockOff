// -----------------------------------------------------------------------------
// Design.Domain - Interface for demonstrating indexer stubbing
// -----------------------------------------------------------------------------

namespace Design.Domain.Entities;

/// <summary>
/// A collection interface with indexer support.
///
/// This interface demonstrates:
/// - Single-key indexers
/// - Backing dictionary pattern
/// - Get/Set callbacks with key access
/// - Indexer sequences
/// - LastGetKey, LastSetEntry tracking
/// </summary>
public interface ICollection<TKey, TValue>
{
    /// <summary>
    /// Indexer for accessing collection items by key.
    /// Used to demonstrate: Get(callback), Set(callback), Backing
    /// </summary>
    TValue this[TKey key] { get; set; }

    /// <summary>
    /// Gets the number of items in the collection.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Checks if the collection contains the specified key.
    /// </summary>
    bool ContainsKey(TKey key);

    /// <summary>
    /// Clears all items from the collection.
    /// </summary>
    void Clear();
}

/// <summary>
/// A read-only collection interface with get-only indexer.
///
/// This interface demonstrates:
/// - Get-only indexers
/// - Simpler Backing usage without set
/// </summary>
public interface IReadOnlyCollection<TKey, TValue>
{
    /// <summary>
    /// Get-only indexer for accessing collection items.
    /// Used to demonstrate: Get without Set
    /// </summary>
    TValue this[TKey key] { get; }

    /// <summary>
    /// Gets the number of items in the collection.
    /// </summary>
    int Count { get; }
}

/// <summary>
/// An interface with multi-key indexer.
///
/// This interface demonstrates:
/// - Multi-key indexers using tuple keys
/// - IndexerContainer&lt;(TKey1, TKey2), TValue&gt; pattern
/// </summary>
public interface IMatrix
{
    /// <summary>
    /// Multi-key indexer for matrix access.
    /// Used to demonstrate: IndexerContainer with tuple keys
    /// </summary>
    double this[int row, int col] { get; set; }

    /// <summary>
    /// Gets the number of rows.
    /// </summary>
    int Rows { get; }

    /// <summary>
    /// Gets the number of columns.
    /// </summary>
    int Columns { get; }
}

/// <summary>
/// An interface with a get/init indexer.
///
/// This interface demonstrates:
/// - Init-only indexer accessors
/// - Generator must emit 'init' instead of 'set'
/// </summary>
public interface IInitIndexerCollection<TKey, TValue>
{
    /// <summary>
    /// Indexer with get and init accessors.
    /// </summary>
    TValue this[TKey key] { get; init; }
}

/// <summary>
/// An interface with multiple indexers distinguished by key type.
///
/// This interface demonstrates:
/// - Multi-indexer disambiguation by C# overload resolution
/// - Different return types per key type
/// - Mixed get/set and get-only indexers
/// </summary>
public interface IMultiIndexerCollection
{
    /// <summary>
    /// String-keyed indexer with get and set.
    /// </summary>
    string this[string key] { get; set; }

    /// <summary>
    /// Int-keyed indexer with get only.
    /// </summary>
    int this[int index] { get; }
}

