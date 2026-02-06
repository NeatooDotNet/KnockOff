// -----------------------------------------------------------------------------
// Design.Domain - Abstract base class for demonstrating async method patterns
// with class stubs (patterns 3, 6)
// -----------------------------------------------------------------------------

namespace Design.Domain.Abstractions;

/// <summary>
/// Abstract base class with async methods for demonstrating async auto-wrapping
/// across class stub patterns.
/// </summary>
public abstract class AsyncServiceBase
{
    /// <summary>Abstract async method returning Task&lt;string&gt;.</summary>
    public abstract Task<string> FetchAsync(int id);

    /// <summary>Virtual async void method returning Task.</summary>
    public virtual Task SaveAsync(string data) => Task.CompletedTask;
}
