// -----------------------------------------------------------------------------
// Design.Stubs - Generic Standalone Stub with Overloaded Methods
// -----------------------------------------------------------------------------
// This stub tests the intersection of:
// - Generic standalone stub pattern (Pattern 1B)
// - Overload API (Returns/Execute disambiguation by parameter signature)
// - User methods (base class overrides) with generic type T
// -----------------------------------------------------------------------------

using Design.Domain.Services;
using KnockOff;

namespace Design.Stubs.StubPatterns;

/// <summary>
/// Generic standalone stub implementing a generic interface with overloaded methods.
/// Tests that the overload API works correctly with class-level type parameters.
/// </summary>
[KnockOff]
public partial class GenericFormatterStub<T> : IGenericFormatter<T> where T : class
{
    // Users can add custom methods/state to standalone stubs
    private readonly List<string> _formatHistory = [];
    public IReadOnlyList<string> FormatHistory => _formatHistory;

    public void TrackFormat(string result)
    {
        _formatHistory.Add(result);
    }
}

/// <summary>
/// Generic standalone stub WITH user method overrides for T-returning overloads.
/// Demonstrates that user methods work with generic type parameter T.
/// </summary>
[KnockOff]
public partial class GenericFormatterWithUserMethodsStub<T> : IGenericFormatter<T> where T : class
{
    // Factory function to create instances of T - set by test
    public Func<int, T>? CreateById { get; set; }
    public Func<string, T>? CreateByName { get; set; }
    public T? DefaultInstance { get; set; }
}

#pragma warning disable CA1062 // Validate arguments of public methods
public partial class GenericFormatterWithUserMethodsStub<T>
{
    // =========================================================================
    // User Method Overrides for T Get() overloads
    // =========================================================================

    protected override T Get_()
    {
        // Return the default instance or throw
        return DefaultInstance ?? throw new InvalidOperationException("DefaultInstance not set");
    }

    protected override T Get_(int id)
    {
        // Use factory to create by ID
        return CreateById?.Invoke(id) ?? throw new InvalidOperationException("CreateById not set");
    }

    protected override T Get_(string name)
    {
        // Use factory to create by name
        return CreateByName?.Invoke(name) ?? throw new InvalidOperationException("CreateByName not set");
    }

    // =========================================================================
    // User Method Overrides for T Transform() overloads
    // =========================================================================

    protected override T Transform_(T item)
    {
        // Default transform - just return the item
        return item;
    }

    protected override T Transform_(T item, string options)
    {
        // Transform with options - still return item (real impl would use options)
        return item;
    }

    // =========================================================================
    // User Method Overrides for string Format() overloads
    // =========================================================================

    protected override string Format_(T item)
    {
        return $"[User: {item}]";
    }

    protected override string Format_(T item, bool uppercase)
    {
        var result = item?.ToString() ?? "null";
        return uppercase ? result.ToUpperInvariant() : result;
    }

    protected override string Format_(T item, bool uppercase, int maxLength)
    {
        var result = Format_(item, uppercase);
        return result.Length > maxLength ? result[..maxLength] : result;
    }
}
#pragma warning restore CA1062
