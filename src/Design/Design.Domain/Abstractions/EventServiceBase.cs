// -----------------------------------------------------------------------------
// Design.Domain - Abstract base class with events for demonstrating event
// stubbing on standalone class patterns
// -----------------------------------------------------------------------------

namespace Design.Domain.Abstractions;

/// <summary>
/// Abstract base class with event members for testing standalone class
/// event stubbing (Pattern 3: [KnockOffBase&lt;EventServiceBase&gt;]).
/// </summary>
public abstract class EventServiceBase
{
    /// <summary>
    /// Standard EventHandler event.
    /// Used to demonstrate: Raise(sender, args) on standalone class stubs
    /// </summary>
    public abstract event EventHandler? StatusChanged;

    /// <summary>
    /// Action-based event with no parameters.
    /// Used to demonstrate: Raise() with no arguments on standalone class stubs
    /// </summary>
    public abstract event Action? Completed;

    /// <summary>Abstract method for basic stubbing.</summary>
    public abstract void Execute(string command);
}
