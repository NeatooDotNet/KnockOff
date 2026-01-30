// -----------------------------------------------------------------------------
// Design.Domain - Interface for demonstrating event stubbing
// -----------------------------------------------------------------------------

namespace Design.Domain.Services;

/// <summary>
/// An event source interface demonstrating various event patterns.
///
/// This interface demonstrates:
/// - EventHandler event stubbing
/// - EventHandler<T> event stubbing
/// - Action-based event stubbing
/// - Event verification (VerifyAdd, VerifyRemove, HasSubscribers)
/// </summary>
public interface IEventSource
{
    /// <summary>
    /// Standard EventHandler event with sender and EventArgs.
    /// Used to demonstrate: Raise(sender, args), VerifyAdd, VerifyRemove
    /// </summary>
    event EventHandler? Started;

    /// <summary>
    /// Generic EventHandler event with custom event args.
    /// Used to demonstrate: Raise(sender, args) with typed EventArgs
    /// </summary>
    event EventHandler<DataEventArgs>? DataReceived;

    /// <summary>
    /// Action-based event with no parameters.
    /// Used to demonstrate: Raise() with no arguments
    /// </summary>
    event Action? Completed;

    /// <summary>
    /// Action-based event with parameters.
    /// Used to demonstrate: Raise(arg1, arg2, ...) pattern
    /// </summary>
    event Action<string, int>? Progress;

    /// <summary>
    /// Starts the event source.
    /// Used to trigger Started event in tests.
    /// </summary>
    void Start();

    /// <summary>
    /// Stops the event source.
    /// Used to trigger Completed event in tests.
    /// </summary>
    void Stop();
}

/// <summary>
/// Custom EventArgs for DataReceived event.
/// </summary>
public class DataEventArgs : EventArgs
{
    public string Data { get; }
    public DateTime Timestamp { get; }

    public DataEventArgs(string data)
    {
        Data = data;
        Timestamp = DateTime.UtcNow;
    }
}
