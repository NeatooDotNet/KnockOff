// -----------------------------------------------------------------------------
// Design.Domain - Generic service interface for demonstrating generic standalone stubs
// -----------------------------------------------------------------------------

namespace Design.Domain.Services;

/// <summary>
/// A generic service interface demonstrating generic standalone stubbing.
///
/// This interface demonstrates:
/// - Generic standalone stub pattern: [KnockOff] partial class Stub&lt;T&gt; : IGenericService&lt;T&gt;
/// - Generic method parameters and return types
/// - Collection return types with generic type parameter
/// </summary>
/// <typeparam name="T">The entity type managed by this service.</typeparam>
public interface IGenericService<T> where T : class
{
    /// <summary>
    /// Gets an entity by ID.
    /// Used to demonstrate: Generic return type in stubs
    /// </summary>
    T? GetById(int id);

    /// <summary>
    /// Gets all entities.
    /// Used to demonstrate: IEnumerable&lt;T&gt; in stubs
    /// </summary>
    IEnumerable<T> GetAll();

    /// <summary>
    /// Saves an entity.
    /// Used to demonstrate: Generic parameter in stubs
    /// </summary>
    void Save(T entity);

    /// <summary>
    /// Gets the count of entities.
    /// Used to demonstrate: Property in generic stub
    /// </summary>
    int Count { get; }
}
