// -----------------------------------------------------------------------------
// Design.Domain - Generic interface with events for demonstrating event
// stubbing on open generic patterns
// -----------------------------------------------------------------------------

namespace Design.Domain.Services;

/// <summary>
/// Generic interface with event members for testing open generic interface
/// event stubbing (Pattern 8: [KnockOff(typeof(IGenericEventSource&lt;&gt;))]).
/// </summary>
/// <typeparam name="T">The data type for events.</typeparam>
public interface IGenericEventSource<T>
{
    /// <summary>
    /// Standard EventHandler event.
    /// Used to demonstrate: Raise(sender, args) on open generic stubs
    /// </summary>
    event EventHandler? StatusChanged;

    /// <summary>
    /// Generic EventHandler event with typed EventArgs.
    /// Used to demonstrate: Raise(sender, args) with typed args on open generic stubs
    /// </summary>
    event EventHandler<GenericDataEventArgs<T>>? DataReceived;

    /// <summary>Gets an item by ID.</summary>
    T? GetById(int id);
}

/// <summary>
/// Generic EventArgs for typed data events.
/// </summary>
public class GenericDataEventArgs<T> : EventArgs
{
    public T Data { get; }

    public GenericDataEventArgs(T data)
    {
        Data = data;
    }
}
