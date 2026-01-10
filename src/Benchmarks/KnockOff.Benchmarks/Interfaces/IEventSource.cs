using System.Diagnostics.CodeAnalysis;

namespace KnockOff.Benchmarks.Interfaces;

/// <summary>
/// Interface with events.
/// Used to measure event subscription/raise overhead.
/// </summary>
[SuppressMessage("Design", "CA1003:Use generic event handler instances", Justification = "Testing various event delegate types")]
public interface IEventSource
{
    event EventHandler<string> MessageReceived;
    event Action<int> ValueChanged;
}
