using System.Collections;

namespace KnockOff.Benchmarks.Interfaces;

/// <summary>
/// Interface implementing BCL interfaces.
/// Used to measure BCL interface implementation overhead.
/// </summary>
public interface IDataProvider : IEnumerable<string>, IDisposable
{
    int Count { get; }
}
