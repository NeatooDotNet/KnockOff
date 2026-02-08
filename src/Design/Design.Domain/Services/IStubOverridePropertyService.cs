// -----------------------------------------------------------------------------
// Design.Domain - Interfaces for demonstrating stub override property patterns
// -----------------------------------------------------------------------------
// These interfaces are designed to explore and document stub override property behavior:
// - Basic stub override properties (protected override properties with underscore suffix)
// - Get-only properties (expression-bodied or block syntax)
// - Set-only properties (write-only configuration)
// - Get/set properties (full property with backing field)
// - Mixed scenarios (some properties with stub override impl, some without)
// -----------------------------------------------------------------------------

namespace Design.Domain.Services;

/// <summary>
/// Basic interface for demonstrating stub override properties.
/// Stub override properties are protected override properties with underscore suffix (e.g., Count_).
/// </summary>
public interface IStubOverridePropertyService
{
    /// <summary>
    /// Get-only property.
    /// Demonstrates: basic stub override property pattern with expression-bodied override
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Get/set property.
    /// Demonstrates: stub override property with backing field
    /// </summary>
    string Name { get; set; }

    /// <summary>
    /// Set-only property.
    /// Demonstrates: write-only stub override property override
    /// </summary>
#pragma warning disable CA1044 // Properties should not be write only - intentional for demo
    string Setting { set; }
#pragma warning restore CA1044

    /// <summary>
    /// Get-only property returning nullable.
    /// Demonstrates: nullable return types in stub override properties
    /// </summary>
    string? Description { get; }
}

/// <summary>
/// Interface for testing mixed scenarios - some properties have stub override implementations,
/// others don't.
/// </summary>
public interface IMixedStubOverridePropertyService
{
    /// <summary>Will have stub override property implementation.</summary>
    int WithStubOverrideProperty { get; }

    /// <summary>Will NOT have stub override property - uses Get instead.</summary>
    int WithoutStubOverrideProperty { get; }

    /// <summary>Will have stub override property implementation.</summary>
    string ComputedWithStubOverrideProperty { get; }

    /// <summary>Will NOT have stub override property - uses Get instead.</summary>
    string ComputedWithoutStubOverrideProperty { get; }
}

/// <summary>
/// Generic interface for testing stub override properties with generic type parameters.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
public interface IGenericStubOverridePropertyService<T> where T : class
{
    /// <summary>Get-only property returning generic type.</summary>
    T? CurrentItem { get; }

    /// <summary>Get/set property with generic type.</summary>
    T? DefaultItem { get; set; }

    /// <summary>Get-only property returning count.</summary>
    int ItemCount { get; }
}
