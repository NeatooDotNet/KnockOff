// -----------------------------------------------------------------------------
// Design.Domain - Interface for demonstrating method overload stubbing
// -----------------------------------------------------------------------------

namespace Design.Domain.Services;

/// <summary>
/// A formatter service with method overloads.
///
/// This interface demonstrates various overload scenarios:
/// - Same name, different parameter counts (easy disambiguation)
/// - Same name, different parameter types (needs explicit types)
/// - Overloaded async methods (with/without CancellationToken)
/// - Overloaded void methods
/// </summary>
public interface IFormatter
{
    // -------------------------------------------------------------------------
    // Format() - Different parameter counts (easy disambiguation)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Formats a string with default options.
    /// Used to demonstrate: single-parameter overload
    /// </summary>
    string Format(string input);

    /// <summary>
    /// Formats a string with options.
    /// Used to demonstrate: two-parameter overload
    /// </summary>
    string Format(string input, FormatOptions options);

    /// <summary>
    /// Formats a string with options and max length.
    /// Used to demonstrate: three-parameter overload
    /// </summary>
    string Format(string input, FormatOptions options, int maxLength);

    // -------------------------------------------------------------------------
    // Transform() - Async overloads (common pattern: with/without CancellationToken)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Transforms data asynchronously.
    /// Used to demonstrate: async overload without cancellation
    /// </summary>
    Task<string> TransformAsync(string input);

    /// <summary>
    /// Transforms data asynchronously with cancellation support.
    /// Used to demonstrate: async overload with CancellationToken
    /// </summary>
    Task<string> TransformAsync(string input, CancellationToken cancellationToken);

    // -------------------------------------------------------------------------
    // Log() - Void method overloads
    // -------------------------------------------------------------------------

    /// <summary>
    /// Logs a message.
    /// Used to demonstrate: void overload, single parameter
    /// </summary>
    void Log(string message);

    /// <summary>
    /// Logs a message with severity level.
    /// Used to demonstrate: void overload, two parameters
    /// </summary>
    void Log(string message, int level);

    /// <summary>
    /// Logs a message with severity and category.
    /// Used to demonstrate: void overload, three parameters
    /// </summary>
    void Log(string message, int level, string category);
}

/// <summary>
/// Options for formatting.
/// </summary>
public record FormatOptions(bool Uppercase = false, bool Trim = false);
