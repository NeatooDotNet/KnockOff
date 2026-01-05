namespace KnockOff.Benchmarks.Interfaces;

/// <summary>
/// Simple interface with a single void method.
/// Used to measure baseline creation and invocation overhead.
/// </summary>
public interface ISimpleService
{
    void DoWork();
}
