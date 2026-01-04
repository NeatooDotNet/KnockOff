namespace KnockOff.Documentation.Samples.SampleDomain;

/// <summary>
/// Event source interface used in events examples.
/// </summary>
public interface IEventSource
{
    event EventHandler<string>? DataReceived;
    event EventHandler? OnCompleted;
    event Action<int>? ValueChanged;
}
