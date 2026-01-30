// -----------------------------------------------------------------------------
// Design.Domain - Interface for demonstrating async method stubbing
// -----------------------------------------------------------------------------

namespace Design.Domain.Services;

/// <summary>
/// A data service interface with async methods.
///
/// This interface demonstrates:
/// - Async method stubbing with Task and Task<T> return types
/// - ValueTask handling
/// - Async auto-wrapping in Returns()
/// </summary>
public interface IDataService
{
    /// <summary>
    /// Gets data by ID asynchronously.
    /// Used to demonstrate: Task<T> return, async auto-wrapping in Returns()
    /// </summary>
    Task<string?> GetDataAsync(int id);

    /// <summary>
    /// Saves data asynchronously.
    /// Used to demonstrate: Task return (void async equivalent)
    /// </summary>
    Task SaveDataAsync(string data);

    /// <summary>
    /// Checks if data exists using ValueTask for performance.
    /// Used to demonstrate: ValueTask<T> handling
    /// </summary>
    ValueTask<bool> ExistsAsync(int id);

    /// <summary>
    /// Gets multiple items as an async enumerable.
    /// Used to demonstrate: IAsyncEnumerable handling
    /// </summary>
    IAsyncEnumerable<string> GetAllAsync();

    /// <summary>
    /// Deletes data asynchronously. Can throw exceptions.
    /// Used to demonstrate: async exception handling in OnCall
    /// </summary>
    Task DeleteAsync(int id);
}
