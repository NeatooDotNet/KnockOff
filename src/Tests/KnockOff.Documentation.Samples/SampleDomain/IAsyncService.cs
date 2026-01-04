namespace KnockOff.Documentation.Samples.SampleDomain;

/// <summary>
/// Async service interface for async method examples.
/// </summary>
public interface IAsyncService
{
    Task InitializeAsync();
    Task<string> GetDataAsync(int id);
    Task<string?> GetOptionalDataAsync(int id);
    ValueTask<int> GetValueAsync();
    ValueTask ProcessAsync(string data);
    Task SaveAsync(User user, CancellationToken cancellationToken = default);
}
