// -----------------------------------------------------------------------------
// Design.Domain - Generic interface for demonstrating async method patterns
// with generic interface stubs (patterns 2, 8)
// -----------------------------------------------------------------------------

namespace Design.Domain.Services;

/// <summary>
/// Generic repository interface with async methods for demonstrating async
/// auto-wrapping across generic interface stub patterns.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
public interface IAsyncRepository<T> where T : class
{
    /// <summary>Async method returning Task&lt;T?&gt;.</summary>
    Task<T?> GetByIdAsync(int id);

    /// <summary>Async void method returning Task.</summary>
    Task SaveAsync(T entity);
}
