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

#region generics-basic-interface
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

#region generics-multiple-params
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

#region generics-constrained
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

#region generics-factory-pattern
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

#region generics-collection-repo
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

#region generics-async-generic
public interface IGenAsyncRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task SaveAsync(T entity);
}

[KnockOff]
public partial class GenAsyncUserRepositoryKnockOff : IGenAsyncRepository<GenUser> { }
#endregion

// ============================================================================
// Generic Standalone Stubs (v10.14+)
// ============================================================================

#region docs:generics:standalone-basic
public interface IGenericRepo<T> where T : class
{
    T? GetById(int id);
    void Save(T entity);
    IEnumerable<T> GetAll();
}

// Generic standalone stub - reusable with any type
[KnockOff]
public partial class GenericRepoStub<T> : IGenericRepo<T> where T : class { }
#endregion

#region docs:generics:standalone-multiple-params
public interface IGenericKeyValue<TKey, TValue>
    where TKey : notnull
    where TValue : class
{
    TValue? Get(TKey key);
    void Set(TKey key, TValue value);
}

[KnockOff]
public partial class GenericKeyValueStub<TKey, TValue> : IGenericKeyValue<TKey, TValue>
    where TKey : notnull
    where TValue : class { }
#endregion

#region docs:generics:standalone-constrained
public interface IGenericEntityRepo<T> where T : class, IGenEntity
{
    T? FindById(int id);
    void Save(T entity);
}

[KnockOff]
public partial class GenericEntityRepoStub<T> : IGenericEntityRepo<T>
    where T : class, IGenEntity { }
#endregion

// ============================================================================
// Generic Methods (Of<T>() pattern)
// ============================================================================

#region generics-generic-method-interface
public interface IGenSerializer
{
    T Deserialize<T>(string json);
    void Process<T>(T value);
}

[KnockOff]
public partial class GenSerializerKnockOff : IGenSerializer { }
#endregion

#region generics-generic-method-multi-param
public interface IGenConverter
{
    TOut Convert<TIn, TOut>(TIn input);
}

[KnockOff]
public partial class GenConverterKnockOff : IGenConverter { }
#endregion

#region generics-generic-method-constrained
public interface IGenEntityFactory
{
    T Create<T>() where T : class, IGenEntity, new();
}

[KnockOff]
public partial class GenEntityFactoryKnockOff : IGenEntityFactory { }
#endregion

// ============================================================================
// Usage Examples
// ============================================================================

/// <summary>
/// Usage examples demonstrating generic interface patterns.
/// Each method is compilable; snippets extract key portions.
/// </summary>
public static class GenericsUsageExamples
{
    public static void TrackingExample()
    {
        var knockOff = new GenUserRepositoryKnockOff();
        var saveTracking = knockOff.Save.OnCall((ko, user) => { });
        IGenRepository<GenUser> repo = knockOff;

        #region generics-tracking
        var user = new GenUser { Id = 1, Name = "Test" };
        repo.Save(user);

        // LastArg is strongly typed as GenUser
        GenUser? savedUser = saveTracking.LastArg;  // same as user
        #endregion

        _ = savedUser;
    }

    public static void CallbacksExample()
    {
        var knockOff = new GenUserRepositoryKnockOff();

        #region generics-callbacks
        knockOff.GetById.OnCall((ko, id) =>
            new GenUser { Id = id, Name = $"User-{id}" });

        knockOff.Save.OnCall((ko, user) =>
        {
            // user is typed as GenUser, not T
            Console.WriteLine($"Saving: {user.Name}");
        });
        #endregion
    }

    public static void MultipleParamsUsage()
    {
        var knockOff = new GenStringCacheKnockOff();

        #region generics-multiple-params-usage
        knockOff.Get.OnCall((ko, key) => key switch
        {
            "admin" => new GenUser { Name = "Admin" },
            _ => null
        });

        knockOff.Set.OnCall((ko, key, value) =>
        {
            // string key, GenUser value
            Console.WriteLine($"Cached {key}: {value.Name}");
        });
        #endregion
    }

    public static void FactoryUsage()
    {
        var knockOff = new GenUserFactoryKnockOff();

        #region generics-factory-usage
        knockOff.Create.OnCall(ko => new GenUser { Name = "Created" });
        #endregion
    }

    public static void CollectionRepoUsage()
    {
        var knockOff = new GenProductRepositoryKnockOff();

        #region generics-collection-usage
        var products = new List<GenProduct>
        {
            new GenProduct { Id = 1, Name = "Widget" },
            new GenProduct { Id = 2, Name = "Gadget" }
        };

        knockOff.GetAll.OnCall(ko => products);

        knockOff.FindFirst.OnCall((ko, predicate) =>
            products.FirstOrDefault(predicate));
        #endregion
    }

    public static void AsyncGenericUsage()
    {
        var knockOff = new GenAsyncUserRepositoryKnockOff();
        var users = new List<GenUser> { new GenUser { Id = 1, Name = "Test" } };

        #region generics-async-usage
        knockOff.GetByIdAsync.OnCall((ko, id) =>
            Task.FromResult<GenUser?>(new GenUser { Id = id }));

        knockOff.GetAllAsync.OnCall(ko =>
            Task.FromResult<IEnumerable<GenUser>>(users));
        #endregion

        _ = users;
    }

    public static void GenericStandaloneUsage()
    {
        #region docs:generics:standalone-usage
        // Same stub class, different type arguments
        var userRepo = new GenericRepoStub<GenUser>();
        var orderRepo = new GenericRepoStub<GenOrder>();

        // Configure user repository
        userRepo.GetById.OnCall((ko, id) => new GenUser { Id = id, Name = $"User-{id}" });
        userRepo.GetAll.OnCall(ko => new List<GenUser>());

        // Configure order repository
        orderRepo.GetById.OnCall((ko, id) => new GenOrder { Id = id });
        #endregion
    }

    public static void GenericStandaloneTracking()
    {
        #region docs:generics:standalone-tracking
        var stub = new GenericRepoStub<GenUser>();
        var saveTracking = stub.Save.OnCall((ko, entity) => { });
        IGenericRepo<GenUser> repo = stub;

        var user = new GenUser { Id = 1, Name = "Test" };
        repo.Save(user);

        // Tracking works via the returned tracking interface
        var callCount = saveTracking.CallCount;   // 1
        var lastArg = saveTracking.LastArg;       // same as user
        #endregion

        _ = callCount;
        _ = lastArg;
    }

    public static void GenericStandaloneMultipleParams()
    {
        #region docs:generics:standalone-multiple-usage
        var cache = new GenericKeyValueStub<string, GenUser>();
        IGenericKeyValue<string, GenUser> service = cache;

        cache.Get.OnCall((ko, key) => new GenUser { Name = key });
        cache.Set.OnCall((ko, key, value) => { /* stored */ });

        var result = service.Get("admin");  // returns GenUser with Name="admin"
        #endregion

        _ = result;
    }

    // ========================================================================
    // Generic Method Examples (Of<T>() pattern)
    // ========================================================================

    public static void GenericMethodConfig()
    {
        #region generics-generic-method-config
        var knockOff = new GenSerializerKnockOff();

        // Configure for specific type using Of<T>()
        knockOff.Deserialize.Of<GenUser>().OnCall((ko, json) =>
            new GenUser { Id = 1, Name = "FromJson" });

        knockOff.Deserialize.Of<GenOrder>().OnCall((ko, json) =>
            new GenOrder { Id = 123 });
        #endregion
    }

    public static void GenericMethodTracking()
    {
        var knockOff = new GenSerializerKnockOff();
        knockOff.Deserialize.Of<GenUser>().OnCall((ko, json) => new GenUser());
        knockOff.Deserialize.Of<GenOrder>().OnCall((ko, json) => new GenOrder());

        #region generics-generic-method-tracking
        IGenSerializer service = knockOff;

        service.Deserialize<GenUser>("{...}");
        service.Deserialize<GenUser>("{...}");
        service.Deserialize<GenOrder>("{...}");

        // Per-type tracking via typed handler
        Assert.Equal(2, knockOff.Deserialize.Of<GenUser>().CallCount);
        Assert.Equal(1, knockOff.Deserialize.Of<GenOrder>().CallCount);

        // Aggregate tracking across all type arguments
        Assert.Equal(3, knockOff.Deserialize.TotalCallCount);
        Assert.True(knockOff.Deserialize.WasCalled);

        // See which types were used
        var types = knockOff.Deserialize.CalledTypeArguments;
        // Returns: [typeof(GenUser), typeof(GenOrder)]
        #endregion

        _ = types;
    }

    public static void GenericMethodMultiUsage()
    {
        var knockOff = new GenConverterKnockOff();

        #region generics-generic-method-multi-usage
        knockOff.Convert.Of<string, int>().OnCall((ko, s) => s.Length);
        knockOff.Convert.Of<int, string>().OnCall((ko, i) => i.ToString());
        #endregion
    }

    public static void GenericMethodConstrainedUsage()
    {
        var knockOff = new GenEntityFactoryKnockOff();

        #region generics-generic-method-constrained-usage
        // Constraints enforced at compile time
        knockOff.Create.Of<GenEmployee>().OnCall((ko) => new GenEmployee());
        #endregion
    }
}

// Minimal Assert class for compilation (tests use xUnit)
file static class Assert
{
    public static void Equal<T>(T expected, T actual) { }
    public static void True(bool condition) { }
}
