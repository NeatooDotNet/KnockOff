// -----------------------------------------------------------------------------
// Design.Domain - Generic abstract base class for demonstrating async method
// patterns with generic class stubs (patterns 4, 9)
// -----------------------------------------------------------------------------

namespace Design.Domain.Abstractions;

/// <summary>
/// Generic abstract base class with async methods for demonstrating async
/// auto-wrapping across generic class stub patterns.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
public abstract class AsyncRepositoryBase<T> where T : class
{
    /// <summary>Abstract async method returning Task&lt;T?&gt;.</summary>
    public abstract Task<T?> GetByIdAsync(int id);

    /// <summary>Virtual async void method returning Task.</summary>
    public virtual Task SaveAsync(T entity) => Task.CompletedTask;
}
