// -----------------------------------------------------------------------------
// Design.Domain - Generic repository interface for demonstrating open generic stubs
// -----------------------------------------------------------------------------

namespace Design.Domain.Services;

/// <summary>
/// A generic repository interface demonstrating open generic stubbing.
///
/// This interface demonstrates:
/// - Open generic stub pattern: [KnockOff(typeof(IRepository<>))]
/// - Generic method parameters
/// - Generic return types
/// </summary>
/// <typeparam name="T">The entity type managed by this repository.</typeparam>
public interface IRepository<T> where T : class
{
    /// <summary>
    /// Gets an entity by ID.
    /// Used to demonstrate: Generic return type in stubs
    /// </summary>
    T? GetById(int id);

    /// <summary>
    /// Gets all entities.
    /// Used to demonstrate: IEnumerable<T> in stubs
    /// </summary>
    IEnumerable<T> GetAll();

    /// <summary>
    /// Adds an entity.
    /// Used to demonstrate: Generic parameter in stubs
    /// </summary>
    void Add(T entity);

    /// <summary>
    /// Updates an entity.
    /// Used to demonstrate: Generic parameter in stubs
    /// </summary>
    void Update(T entity);

    /// <summary>
    /// Deletes an entity by ID.
    /// </summary>
    void Delete(int id);

    /// <summary>
    /// Checks if an entity exists.
    /// </summary>
    bool Exists(int id);
}
