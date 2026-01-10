namespace KnockOff.Benchmarks.Interfaces;

/// <summary>
/// Generic repository interface.
/// Used to measure generic type resolution overhead.
/// </summary>
public interface IRepository<T> where T : class
{
    T? GetById(int id);
    void Save(T entity);
    IEnumerable<T> GetAll();
}
