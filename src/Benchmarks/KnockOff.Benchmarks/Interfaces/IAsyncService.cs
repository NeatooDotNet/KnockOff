namespace KnockOff.Benchmarks.Interfaces;

/// <summary>
/// Interface with async methods.
/// Used to measure async invocation overhead.
/// </summary>
public interface IAsyncService
{
    Task DoWorkAsync();
    Task<int> GetValueAsync();
    ValueTask<string> GetStringValueAsync();
}
