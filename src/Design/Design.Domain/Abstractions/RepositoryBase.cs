// -----------------------------------------------------------------------------
// Design.Domain - Generic abstract base class for testing open generic class stubs
// -----------------------------------------------------------------------------

namespace Design.Domain.Abstractions;

/// <summary>
/// Generic abstract base class for testing open generic class stubbing.
/// Tests [KnockOff(typeof(RepositoryBase&lt;&gt;))] pattern.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
public abstract class RepositoryBase<T> where T : class
{
    /// <summary>Abstract property - must be stubbed.</summary>
    public abstract string Name { get; }

    /// <summary>Virtual method returning T.</summary>
    public virtual T? GetById(int id) => default;

    /// <summary>Virtual method with T parameter.</summary>
    public virtual void Save(T entity) { }

    /// <summary>Overloaded method - no params.</summary>
    public virtual T? GetDefault() => default;

    /// <summary>Overloaded method - with filter.</summary>
    public virtual T? GetDefault(string filter) => default;
}
