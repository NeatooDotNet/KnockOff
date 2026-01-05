# Generic Interfaces

KnockOff supports generic interfaces with concrete type parameters.

## Basic Usage

For generic interfaces, create a KnockOff class with concrete type arguments:

<!-- snippet: docs:generics:basic-interface -->
```csharp
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
<!-- /snippet -->

## Tracking

Tracking uses the concrete types. For generic interfaces, the spy property includes the type arguments in the name:

```csharp
var knockOff = new UserRepositoryKnockOff();
IRepository<User> repo = knockOff;

var user = new User { Id = 1, Name = "Test" };
repo.Save(user);

// Spy property is IRepository_User (interface name + type arg)
// LastCallArg is strongly typed as User
User? savedUser = knockOff.IRepository_User.Save.LastCallArg;
Assert.Same(user, savedUser);
```

## Callbacks

Callbacks also use concrete types:

```csharp
knockOff.IRepository_User.GetById.OnCall = (ko, id) =>
    new User { Id = id, Name = $"User-{id}" };

knockOff.IRepository_User.Save.OnCall = (ko, user) =>
{
    // user is typed as User, not T
    Assert.NotNull(user.Name);
};
```

## Multiple Generic Parameters

<!-- snippet: docs:generics:multiple-params -->
```csharp
public interface IGenCache<TKey, TValue>
{
    TValue? Get(TKey key);
    void Set(TKey key, TValue value);
}

[KnockOff]
public partial class GenStringCacheKnockOff : IGenCache<string, GenUser> { }
```
<!-- /snippet -->

Usage:

```csharp
var knockOff = new StringCacheKnockOff();

// Spy property: ICache_string_User (interface + type args)
knockOff.ICache_string_User.Get.OnCall = (ko, key) => key switch
{
    "admin" => new User { Name = "Admin" },
    _ => null
};

knockOff.ICache_string_User.Set.OnCall = (ko, key, value) =>
{
    // string key, User value
    Console.WriteLine($"Cached {key}: {value.Name}");
};
```

## Constrained Generics

KnockOff works with constrained generic interfaces:

<!-- snippet: docs:generics:constrained -->
```csharp
public interface IGenEntityRepository<T> where T : class, IGenEntity
{
    T? FindById(int id);
}

// GenEmployee must implement IGenEntity
[KnockOff]
public partial class GenEmployeeRepositoryKnockOff : IGenEntityRepository<GenEmployee> { }
```
<!-- /snippet -->

## Common Patterns

### Factory Pattern

<!-- snippet: docs:generics:factory-pattern -->
```csharp
public interface IGenFactory<T> where T : new()
{
    T Create();
}

[KnockOff]
public partial class GenUserFactoryKnockOff : IGenFactory<GenUser> { }
```
<!-- /snippet -->

```csharp
// Usage
knockOff.IGenFactory_GenUser.Create.OnCall = (ko) => new GenUser { Name = "Created" };
```

### Collection Repositories

<!-- snippet: docs:generics:collection-repo -->
```csharp
public interface IGenReadOnlyRepository<T>
{
    IEnumerable<T> GetAll();
    T? FindFirst(Func<T, bool> predicate);
}

[KnockOff]
public partial class GenProductRepositoryKnockOff : IGenReadOnlyRepository<GenProduct> { }
```
<!-- /snippet -->

```csharp
// Usage
var products = new List<GenProduct>
{
    new GenProduct { Id = 1, Name = "Widget" },
    new GenProduct { Id = 2, Name = "Gadget" }
};

knockOff.IGenReadOnlyRepository_GenProduct.GetAll.OnCall = (ko) => products;

knockOff.IGenReadOnlyRepository_GenProduct.FindFirst.OnCall = (ko, predicate) =>
    products.FirstOrDefault(predicate);
```

### Async Generic Repositories

<!-- snippet: docs:generics:async-generic -->
```csharp
public interface IGenAsyncRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task SaveAsync(T entity);
}

[KnockOff]
public partial class GenAsyncUserRepositoryKnockOff : IGenAsyncRepository<GenUser> { }
```
<!-- /snippet -->

```csharp
// Usage
knockOff.IGenAsyncRepository_GenUser.GetByIdAsync.OnCall = (ko, id) =>
    Task.FromResult<GenUser?>(new GenUser { Id = id });

knockOff.IGenAsyncRepository_GenUser.GetAllAsync.OnCall = (ko) =>
    Task.FromResult<IEnumerable<GenUser>>(users);
```

## Generic Methods

KnockOff supports generic methods using the `.Of<T>()` pattern. This allows type-specific behavior configuration and call tracking.

### Basic Usage

```csharp
public interface ISerializer
{
    T Deserialize<T>(string json);
    void Process<T>(T value);
}

[KnockOff]
public partial class SerializerKnockOff : ISerializer { }
```

Configure behavior per type argument:

```csharp
var knockOff = new SerializerKnockOff();

// Configure for specific type using Of<T>()
knockOff.ISerializer.Deserialize.Of<User>().OnCall = (ko, json) =>
    JsonSerializer.Deserialize<User>(json)!;

knockOff.ISerializer.Deserialize.Of<Order>().OnCall = (ko, json) =>
    new Order { Id = 123 };
```

### Per-Type Call Tracking

```csharp
ISerializer service = knockOff;

service.Deserialize<User>("{...}");
service.Deserialize<User>("{...}");
service.Deserialize<Order>("{...}");

// Per-type tracking
Assert.Equal(2, knockOff.ISerializer.Deserialize.Of<User>().CallCount);
Assert.Equal(1, knockOff.ISerializer.Deserialize.Of<Order>().CallCount);

// Aggregate tracking across all type arguments
Assert.Equal(3, knockOff.ISerializer.Deserialize.TotalCallCount);
Assert.True(knockOff.ISerializer.Deserialize.WasCalled);

// See which types were used
var types = knockOff.ISerializer.Deserialize.CalledTypeArguments;
// Returns: [typeof(User), typeof(Order)]
```

### Multiple Type Parameters

```csharp
public interface IConverter
{
    TOut Convert<TIn, TOut>(TIn input);
}

[KnockOff]
public partial class ConverterKnockOff : IConverter { }
```

```csharp
knockOff.IConverter.Convert.Of<string, int>().OnCall = (ko, s) => s.Length;
knockOff.IConverter.Convert.Of<int, string>().OnCall = (ko, i) => i.ToString();
```

### Constrained Generic Methods

Type constraints are preserved on the `.Of<T>()` method:

```csharp
public interface IEntityFactory
{
    T Create<T>() where T : class, IEntity, new();
}
```

```csharp
// Constraints enforced at compile time
knockOff.IEntityFactory.Create.Of<Employee>().OnCall = (ko) => new Employee();
```

### Smart Defaults

Unconfigured generic methods use smart defaults at runtime:
- **Value types**: Return `default(T)`
- **Types with parameterless constructor**: Return `new T()`
- **Nullable return types**: Return `null`
- **Other types**: Throw `InvalidOperationException`

## Limitations

- The KnockOff class must specify **concrete type arguments** for generic interfaces

```csharp
// NOT supported - generic KnockOff class
[KnockOff]
public partial class GenericRepoKnockOff<T> : IRepository<T> { }  // Won't work

// Supported - concrete type
[KnockOff]
public partial class UserRepoKnockOff : IRepository<User> { }  // Works
```
