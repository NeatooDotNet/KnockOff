// -----------------------------------------------------------------------------
// Design.Domain - Generic interface with overloaded methods
// -----------------------------------------------------------------------------
// This interface tests the intersection of:
// - Generic standalone stub pattern (class-level type parameter T)
// - Overload API (multiple methods with same name, different signatures)
// -----------------------------------------------------------------------------

namespace Design.Domain.Services;

/// <summary>
/// A generic formatter interface with overloaded methods.
/// Tests that overload API works correctly with generic standalone stubs.
/// </summary>
/// <typeparam name="T">The type to format.</typeparam>
public interface IGenericFormatter<T> where T : class
{
    // -------------------------------------------------------------------------
    // Format() - Overloaded methods with different parameter counts
    // -------------------------------------------------------------------------

    /// <summary>Formats an item with default options.</summary>
    string Format(T item);

    /// <summary>Formats an item with uppercase option.</summary>
    string Format(T item, bool uppercase);

    /// <summary>Formats an item with uppercase and max length options.</summary>
    string Format(T item, bool uppercase, int maxLength);

    // -------------------------------------------------------------------------
    // Process() - Void method overloads
    // -------------------------------------------------------------------------

    /// <summary>Processes an item.</summary>
    void Process(T item);

    /// <summary>Processes an item with a tag.</summary>
    void Process(T item, string tag);

    // -------------------------------------------------------------------------
    // Transform() - Overloads returning the generic type
    // -------------------------------------------------------------------------

    /// <summary>Transforms an item.</summary>
    T Transform(T item);

    /// <summary>Transforms an item with options.</summary>
    T Transform(T item, string options);

    // -------------------------------------------------------------------------
    // Get() - Overloads where return type is T, varying parameters
    // -------------------------------------------------------------------------

    /// <summary>Gets default item.</summary>
    T Get();

    /// <summary>Gets item by id.</summary>
    T Get(int id);

    /// <summary>Gets item by name.</summary>
    T Get(string name);

    // -------------------------------------------------------------------------
    // Find() - Overloads with T in various positions
    // -------------------------------------------------------------------------

    /// <summary>Finds matching items.</summary>
    IEnumerable<T> Find(Func<T, bool> predicate);

    /// <summary>Finds matching items with limit.</summary>
    IEnumerable<T> Find(Func<T, bool> predicate, int limit);
}
