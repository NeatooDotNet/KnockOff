// -----------------------------------------------------------------------------
// Design.Domain - Interface for demonstrating property stubbing
// -----------------------------------------------------------------------------

namespace Design.Domain.Entities;

/// <summary>
/// An entity interface with various property types.
///
/// This interface demonstrates:
/// - Get-only properties
/// - Set-only properties (rare but valid)
/// - Get/set properties
/// - OnGet, OnSet, Value backing store
/// - Property verification (VerifyGet, VerifySet)
/// </summary>
public interface IEntity
{
    /// <summary>
    /// Get-only property for entity ID.
    /// Used to demonstrate: OnGet(value), OnGet(callback)
    /// </summary>
    int Id { get; }

    /// <summary>
    /// Get-only property for entity name.
    /// Used to demonstrate: OnGet().ThenGet() sequences
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Get/set property for description.
    /// Used to demonstrate: Value backing store, OnSet(callback)
    /// </summary>
    string Description { get; set; }

    /// <summary>
    /// Get/set property for modification timestamp.
    /// Used to demonstrate: LastValue capture, VerifySet
    /// </summary>
    DateTime? ModifiedAt { get; set; }

    /// <summary>
    /// Get-only property indicating if entity is valid.
    /// Used to demonstrate: dynamic OnGet callback based on other state
    /// </summary>
    bool IsValid { get; }
}
