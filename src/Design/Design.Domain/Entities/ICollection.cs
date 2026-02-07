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
/// KNOWN LIMITATION: Multi-key indexers are not currently supported by the
/// KnockOff generator due to a bug with tuple key types in ThenGet/ThenSet
/// sequence methods. This interface exists to document the expected API when
/// support is added. See IndexerBasics.cs for details.
///
/// This interface demonstrates:
/// - Multi-key indexers using tuple keys
/// - IndexerContainer<(TKey1, TKey2), TValue> pattern
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
