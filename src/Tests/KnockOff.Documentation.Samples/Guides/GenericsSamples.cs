/// <summary>
/// Code samples for docs/guides/generics.md
///
/// Snippets in this file:
/// - docs:generics:basic-interface
/// - docs:generics:tracking
/// - docs:generics:callbacks
/// - docs:generics:multiple-params
/// - docs:generics:constrained
/// - docs:generics:factory-pattern
/// - docs:generics:collection-repo
/// - docs:generics:async-generic
///
/// Corresponding tests: GenericsSamplesTests.cs
/// </summary>

namespace KnockOff.Documentation.Samples.Guides;

// ============================================================================
// Domain Types
// ============================================================================

public class GenUser
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class GenOrder
{
    public int Id { get; set; }
}

public class GenProduct
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public interface IGenEntity
{
    int Id { get; set; }
}

public class GenEmployee : IGenEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

// ============================================================================
// Basic Usage
// ============================================================================

#region docs:generics:basic-interface
public interface IGenRepository<T> where T : class
{
    T? GetById(int id);
    void Save(T entity);
    Task<T?> GetByIdAsync(int id);
}

// Concrete KnockOff for GenUser entities
[KnockOff]
public partial class GenUserRepositoryKnockOff : IGenRepository<GenUser> { }

// Concrete KnockOff for GenOrder entities
[KnockOff]
public partial class GenOrderRepositoryKnockOff : IGenRepository<GenOrder> { }
#endregion

// ============================================================================
// Multiple Generic Parameters
// ============================================================================

#region docs:generics:multiple-params
public interface IGenCache<TKey, TValue>
{
    TValue? Get(TKey key);
    void Set(TKey key, TValue value);
}

[KnockOff]
public partial class GenStringCacheKnockOff : IGenCache<string, GenUser> { }
#endregion

// ============================================================================
// Constrained Generics
// ============================================================================

#region docs:generics:constrained
public interface IGenEntityRepository<T> where T : class, IGenEntity
{
    T? FindById(int id);
}

// GenEmployee must implement IGenEntity
[KnockOff]
public partial class GenEmployeeRepositoryKnockOff : IGenEntityRepository<GenEmployee> { }
#endregion

// ============================================================================
// Factory Pattern
// ============================================================================

#region docs:generics:factory-pattern
public interface IGenFactory<T> where T : new()
{
    T Create();
}

[KnockOff]
public partial class GenUserFactoryKnockOff : IGenFactory<GenUser> { }
#endregion

// ============================================================================
// Collection Repositories
// ============================================================================

#region docs:generics:collection-repo
public interface IGenReadOnlyRepository<T>
{
    IEnumerable<T> GetAll();
    T? FindFirst(Func<T, bool> predicate);
}

[KnockOff]
public partial class GenProductRepositoryKnockOff : IGenReadOnlyRepository<GenProduct> { }
#endregion

// ============================================================================
// Async Generic Repositories
// ============================================================================

#region docs:generics:async-generic
public interface IGenAsyncRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task SaveAsync(T entity);
}

[KnockOff]
public partial class GenAsyncUserRepositoryKnockOff : IGenAsyncRepository<GenUser> { }
#endregion
