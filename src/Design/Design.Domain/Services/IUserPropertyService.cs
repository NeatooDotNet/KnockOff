// -----------------------------------------------------------------------------
// Design.Domain - Interfaces for demonstrating user-defined property patterns
// -----------------------------------------------------------------------------
// These interfaces are designed to explore and document user property behavior:
// - Basic user properties (protected override properties with underscore suffix)
// - Get-only properties (expression-bodied or block syntax)
// - Set-only properties (write-only configuration)
// - Get/set properties (full property with backing field)
// - Mixed scenarios (some properties with user impl, some without)
// -----------------------------------------------------------------------------

namespace Design.Domain.Services;

/// <summary>
/// Basic interface for demonstrating user-defined properties.
/// User properties are protected override properties with underscore suffix (e.g., Count_).
/// </summary>
public interface IUserPropertyService
{
    /// <summary>
    /// Get-only property.
    /// Demonstrates: basic user property pattern with expression-bodied override
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Get/set property.
    /// Demonstrates: user property with backing field
    /// </summary>
    string Name { get; set; }

    /// <summary>
    /// Set-only property.
    /// Demonstrates: write-only user property override
    /// </summary>
#pragma warning disable CA1044 // Properties should not be write only - intentional for demo
    string Setting { set; }
#pragma warning restore CA1044

    /// <summary>
    /// Get-only property returning nullable.
    /// Demonstrates: nullable return types in user properties
    /// </summary>
    string? Description { get; }
}

/// <summary>
/// Interface for testing mixed scenarios - some properties have user implementations,
/// others don't.
/// </summary>
public interface IMixedUserPropertyService
{
    /// <summary>Will have user property implementation.</summary>
    int WithUserProperty { get; }

    /// <summary>Will NOT have user property - uses OnGet instead.</summary>
    int WithoutUserProperty { get; }

    /// <summary>Will have user property implementation.</summary>
    string ComputedWithUserProperty { get; }

    /// <summary>Will NOT have user property - uses OnGet instead.</summary>
    string ComputedWithoutUserProperty { get; }
}

/// <summary>
/// Generic interface for testing user properties with generic type parameters.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
public interface IGenericUserPropertyService<T> where T : class
{
    /// <summary>Get-only property returning generic type.</summary>
    T? CurrentItem { get; }

    /// <summary>Get/set property with generic type.</summary>
    T? DefaultItem { get; set; }

    /// <summary>Get-only property returning count.</summary>
    int ItemCount { get; }
}
