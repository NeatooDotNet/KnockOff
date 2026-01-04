namespace KnockOff.Documentation.Samples.SampleDomain;

/// <summary>
/// Generic repository interface used in generics examples.
/// </summary>
public interface IRepository<T> where T : class
{
    T? GetById(int id);
    void Save(T entity);
    Task<T?> GetByIdAsync(int id);
    int Count();
    IEnumerable<T> GetAll();
}
