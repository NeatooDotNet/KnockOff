# Generic Interfaces

KnockOff supports generic interfaces with concrete type parameters.

## Basic Usage

For generic interfaces, create a KnockOff class with concrete type arguments:

<!-- snippet: generics-basic-interface -->
```cs
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
```
<!-- endSnippet -->

## Tracking

Tracking uses the concrete types:

<!-- snippet: generics-tracking -->
```cs
var user = new GenUser { Id = 1, Name = "Test" };
repo.Save(user);

// LastCallArg is strongly typed as GenUser
GenUser? savedUser = knockOff.Save.LastCallArg;  // same as user
```
<!-- endSnippet -->

## Callbacks

Callbacks also use concrete types:

<!-- snippet: generics-callbacks -->
```cs
knockOff.GetById.OnCall = (ko, id) =>
    new GenUser { Id = id, Name = $"User-{id}" };

knockOff.Save.OnCall = (ko, user) =>
{
    // user is typed as GenUser, not T
    Console.WriteLine($"Saving: {user.Name}");
};
```
<!-- endSnippet -->

## Multiple Generic Parameters

<!-- snippet: generics-multiple-params -->
```cs
public interface IGenCache<TKey, TValue>
{
    TValue? Get(TKey key);
    void Set(TKey key, TValue value);
}

[KnockOff]
public partial class GenStringCacheKnockOff : IGenCache<string, GenUser> { }
```
<!-- endSnippet -->

Usage:

<!-- snippet: generics-multiple-params-usage -->
```cs
knockOff.Get.OnCall = (ko, key) => key switch
{
    "admin" => new GenUser { Name = "Admin" },
    _ => null
};

knockOff.Set.OnCall = (ko, key, value) =>
{
    // string key, GenUser value
    Console.WriteLine($"Cached {key}: {value.Name}");
};
```
<!-- endSnippet -->

## Constrained Generics

KnockOff works with constrained generic interfaces:

<!-- snippet: generics-constrained -->
```cs
public interface IGenEntityRepository<T> where T : class, IGenEntity
{
    T? FindById(int id);
}

// GenEmployee must implement IGenEntity
[KnockOff]
public partial class GenEmployeeRepositoryKnockOff : IGenEntityRepository<GenEmployee> { }
```
<!-- endSnippet -->

## Common Patterns

### Factory Pattern

<!-- snippet: generics-factory-pattern -->
```cs
public interface IGenFactory<T> where T : new()
{
    T Create();
}

[KnockOff]
public partial class GenUserFactoryKnockOff : IGenFactory<GenUser> { }
```
<!-- endSnippet -->

<!-- snippet: generics-factory-usage -->
```cs
knockOff.Create.OnCall = (ko) => new GenUser { Name = "Created" };
```
<!-- endSnippet -->

### Collection Repositories

<!-- snippet: generics-collection-repo -->
```cs
public interface IGenReadOnlyRepository<T>
{
    IEnumerable<T> GetAll();
    T? FindFirst(Func<T, bool> predicate);
}

[KnockOff]
public partial class GenProductRepositoryKnockOff : IGenReadOnlyRepository<GenProduct> { }
```
<!-- endSnippet -->

<!-- snippet: generics-collection-usage -->
```cs
var products = new List<GenProduct>
{
    new GenProduct { Id = 1, Name = "Widget" },
    new GenProduct { Id = 2, Name = "Gadget" }
};

knockOff.GetAll.OnCall = (ko) => products;

knockOff.FindFirst.OnCall = (ko, predicate) =>
    products.FirstOrDefault(predicate);
```
<!-- endSnippet -->

### Async Generic Repositories

<!-- snippet: generics-async-generic -->
```cs
public interface IGenAsyncRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task SaveAsync(T entity);
}

[KnockOff]
public partial class GenAsyncUserRepositoryKnockOff : IGenAsyncRepository<GenUser> { }
```
<!-- endSnippet -->

<!-- snippet: generics-async-usage -->
```cs
knockOff.GetByIdAsync.OnCall = (ko, id) =>
    Task.FromResult<GenUser?>(new GenUser { Id = id });

knockOff.GetAllAsync.OnCall = (ko) =>
    Task.FromResult<IEnumerable<GenUser>>(users);
```
<!-- endSnippet -->

## Generic Methods

KnockOff supports generic methods using the `.Of<T>()` pattern. This allows type-specific behavior configuration and call tracking.

### Basic Usage

<!-- pseudo:generic-method-interface -->
```csharp
public interface ISerializer
{
    T Deserialize<T>(string json);
    void Process<T>(T value);
}

[KnockOff]
public partial class SerializerKnockOff : ISerializer { }
```
<!-- /snippet -->

Configure behavior per type argument:

<!-- pseudo:generic-method-config -->
```csharp
var knockOff = new SerializerKnockOff();

// Configure for specific type using Of<T>()
knockOff.Deserialize.Of<User>().OnCall = (ko, json) =>
    JsonSerializer.Deserialize<User>(json)!;

knockOff.Deserialize.Of<Order>().OnCall = (ko, json) =>
    new Order { Id = 123 };
```
<!-- /snippet -->

### Per-Type Call Tracking

<!-- pseudo:generic-method-tracking -->
```csharp
ISerializer service = knockOff;

service.Deserialize<User>("{...}");
service.Deserialize<User>("{...}");
service.Deserialize<Order>("{...}");

// Per-type tracking
Assert.Equal(2, knockOff.Deserialize.Of<User>().CallCount);
Assert.Equal(1, knockOff.Deserialize.Of<Order>().CallCount);

// Aggregate tracking across all type arguments
Assert.Equal(3, knockOff.Deserialize.TotalCallCount);
Assert.True(knockOff.Deserialize.WasCalled);

// See which types were used
var types = knockOff.Deserialize.CalledTypeArguments;
// Returns: [typeof(User), typeof(Order)]
```
<!-- /snippet -->

### Multiple Type Parameters

<!-- pseudo:generic-method-multi-param -->
```csharp
public interface IConverter
{
    TOut Convert<TIn, TOut>(TIn input);
}

[KnockOff]
public partial class ConverterKnockOff : IConverter { }
```
<!-- /snippet -->

<!-- pseudo:generic-method-multi-usage -->
```csharp
knockOff.Convert.Of<string, int>().OnCall = (ko, s) => s.Length;
knockOff.Convert.Of<int, string>().OnCall = (ko, i) => i.ToString();
```
<!-- /snippet -->

### Constrained Generic Methods

Type constraints are preserved on the `.Of<T>()` method:

<!-- pseudo:generic-method-constrained -->
```csharp
public interface IEntityFactory
{
    T Create<T>() where T : class, IEntity, new();
}
```
<!-- /snippet -->

<!-- pseudo:generic-method-constrained-usage -->
```csharp
// Constraints enforced at compile time
knockOff.Create.Of<Employee>().OnCall = (ko) => new Employee();
```
<!-- /snippet -->

### Smart Defaults

Unconfigured generic methods use smart defaults at runtime:
- **Value types**: Return `default(T)`
- **Types with parameterless constructor**: Return `new T()`
- **Nullable return types**: Return `null`
- **Other types**: Throw `InvalidOperationException`

## Generic Standalone Stubs

Starting in v10.14, KnockOff supports **generic standalone stubs** - stub classes with their own type parameters that implement generic interfaces.

### Basic Usage

<!-- snippet: docs:generics:standalone-basic -->
```cs
public interface IGenericRepo<T> where T : class
{
    T? GetById(int id);
    void Save(T entity);
    IEnumerable<T> GetAll();
}

// Generic standalone stub - reusable with any type
[KnockOff]
public partial class GenericRepoStub<T> : IGenericRepo<T> where T : class { }
```
<!-- endSnippet -->

Use the same stub class with different type arguments:

<!-- snippet: docs:generics:standalone-usage -->
```cs
// Same stub class, different type arguments
var userRepo = new GenericRepoStub<GenUser>();
var orderRepo = new GenericRepoStub<GenOrder>();

// Configure user repository
userRepo.GetById.OnCall = (ko, id) => new GenUser { Id = id, Name = $"User-{id}" };
userRepo.GetAll.OnCall = (ko) => new List<GenUser>();

// Configure order repository
orderRepo.GetById.OnCall = (ko, id) => new GenOrder { Id = id };
```
<!-- endSnippet -->

### Tracking

<!-- snippet: docs:generics:standalone-tracking -->
```cs
var stub = new GenericRepoStub<GenUser>();
IGenericRepo<GenUser> repo = stub;

var user = new GenUser { Id = 1, Name = "Test" };
repo.Save(user);

// Tracking works with the type parameter
var callCount = stub.Save.CallCount;      // 1
var lastArg = stub.Save.LastCallArg;      // same as user
```
<!-- endSnippet -->

### Multiple Type Parameters

<!-- snippet: docs:generics:standalone-multiple-params -->
```cs
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
```
<!-- endSnippet -->

<!-- snippet: docs:generics:standalone-multiple-usage -->
```cs
var cache = new GenericKeyValueStub<string, GenUser>();
IGenericKeyValue<string, GenUser> service = cache;

cache.Get.OnCall = (ko, key) => new GenUser { Name = key };
cache.Set.OnCall = (ko, key, value) => { /* stored */ };

var result = service.Get("admin");  // returns GenUser with Name="admin"
```
<!-- endSnippet -->

### Constraints

Type parameter constraints must match between the stub class and interface:

<!-- snippet: docs:generics:standalone-constrained -->
```cs
public interface IGenericEntityRepo<T> where T : class, IGenEntity
{
    T? FindById(int id);
    void Save(T entity);
}

[KnockOff]
public partial class GenericEntityRepoStub<T> : IGenericEntityRepo<T>
    where T : class, IGenEntity { }
```
<!-- endSnippet -->

### Type Parameter Arity

The stub class must have the **same number of type parameters** as the interface. Mismatched arity produces diagnostic `KO0008`:

```csharp
// Error KO0008: Type parameter count mismatch
[KnockOff]
public partial class BadStub<T, TExtra> : IRepository<T> { }  // 2 vs 1

// Correct: matching arity
[KnockOff]
public partial class GoodStub<T> : IRepository<T> { }  // 1 vs 1
```

## Choosing Between Patterns

| Pattern | When to Use |
|---------|-------------|
| **Concrete stubs** (`UserRepoStub : IRepository<User>`) | One-off stubs, specific test scenarios |
| **Generic stubs** (`RepoStub<T> : IRepository<T>`) | Reusable across multiple entity types |

Both patterns are fully supported and generate identical interceptor APIs.
