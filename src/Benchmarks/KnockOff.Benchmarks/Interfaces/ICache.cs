namespace KnockOff.Benchmarks.Interfaces;

/// <summary>
/// Interface with indexers.
/// Used to measure indexer get/set overhead.
/// </summary>
public interface ICache
{
    object this[string key] { get; set; }
    int this[int index] { get; }
}
