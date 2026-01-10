namespace KnockOff.Benchmarks.Interfaces;

/// <summary>
/// Interface with ref/out parameters.
/// Used to measure ref/out parameter handling overhead.
/// </summary>
public interface IParser
{
    bool TryParse(string input, out int result);
    void Increment(ref int value);
}
