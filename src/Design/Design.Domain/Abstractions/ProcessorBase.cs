// -----------------------------------------------------------------------------
// Design.Domain - Non-generic abstract base class for testing inline class stubs
// -----------------------------------------------------------------------------

namespace Design.Domain.Abstractions;

/// <summary>
/// Non-generic abstract base class for testing inline class stubbing with overloads.
/// Tests [KnockOff&lt;ProcessorBase&gt;] pattern with compatible and incompatible overloads.
/// </summary>
public abstract class ProcessorBase
{
    /// <summary>Abstract property - must be stubbed.</summary>
    public abstract string Name { get; }

    // =========================================================================
    // COMPATIBLE OVERLOADS - Single interceptor with multiple Call
    // =========================================================================

    /// <summary>Process with no parameters.</summary>
    public virtual string Process() => "base-no-params";

    /// <summary>Process with message parameter.</summary>
    public virtual string Process(string message) => $"base-message: {message}";

    /// <summary>Process with message and priority.</summary>
    public virtual string Process(string message, int priority) => $"base-message-priority: {message}, {priority}";

    // =========================================================================
    // INCOMPATIBLE OVERLOADS - Different return types require numbered interceptors
    // =========================================================================

    /// <summary>Transform returning string.</summary>
    public virtual string Transform(int value) => $"string-{value}";

    /// <summary>Transform returning int - incompatible with string overload.</summary>
    public virtual int Transform(string text) => text?.Length ?? 0;

    // =========================================================================
    // ADDITIONAL COMPATIBLE OVERLOADS - For edge case testing
    // =========================================================================

    /// <summary>Calculate with single int.</summary>
    public virtual int Calculate(int x) => x;

    /// <summary>Calculate with two ints - same param name is OK when types match.</summary>
    public virtual int Calculate(int x, int y) => x + y;
}
