# Generic Interfaces

KnockOff supports generic interfaces with concrete type parameters.

## Basic Usage

For generic interfaces, create a KnockOff class with concrete type arguments:

```csharp
public interface IRepository<T> where T : class
{
    T? GetById(int id);
    void Save(T entity);
    Task<T?> GetByIdAsync(int id);
}

// Concrete KnockOff for User entities
[KnockOff]
public partial class UserRepositoryKnockOff : IRepository<User> { }

// Concrete KnockOff for Order entities
[KnockOff]
public partial class OrderRepositoryKnockOff : IRepository<Order> { }
```

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

```csharp
public interface ICache<TKey, TValue>
{
    TValue? Get(TKey key);
    void Set(TKey key, TValue value);
}

[KnockOff]
public partial class StringCacheKnockOff : ICache<string, User> { }
```

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

```csharp
public interface IEntityRepository<T> where T : class, IEntity
{
    T? FindById(int id);
}

// Employee must implement IEntity
[KnockOff]
public partial class EmployeeRepositoryKnockOff : IEntityRepository<Employee> { }
```

## Common Patterns

### Factory Pattern

```csharp
public interface IFactory<T> where T : new()
{
    T Create();
}

[KnockOff]
public partial class UserFactoryKnockOff : IFactory<User> { }

// Usage
knockOff.IFactory_User.Create.OnCall = (ko) => new User { Name = "Created" };
```

### Collection Repositories

```csharp
public interface IReadOnlyRepository<T>
{
    IEnumerable<T> GetAll();
    T? FindFirst(Func<T, bool> predicate);
}

[KnockOff]
public partial class ProductRepositoryKnockOff : IReadOnlyRepository<Product> { }

// Usage
var products = new List<Product>
{
    new Product { Id = 1, Name = "Widget" },
    new Product { Id = 2, Name = "Gadget" }
};

knockOff.IReadOnlyRepository_Product.GetAll.OnCall = (ko) => products;

knockOff.IReadOnlyRepository_Product.FindFirst.OnCall = (ko, predicate) =>
    products.FirstOrDefault(predicate);
```

### Async Generic Repositories

```csharp
public interface IAsyncRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task SaveAsync(T entity);
}

[KnockOff]
public partial class AsyncUserRepositoryKnockOff : IAsyncRepository<User> { }

// Usage
knockOff.IAsyncRepository_User.GetByIdAsync.OnCall = (ko, id) =>
    Task.FromResult<User?>(new User { Id = id });

knockOff.IAsyncRepository_User.GetAllAsync.OnCall = (ko) =>
    Task.FromResult<IEnumerable<User>>(users);
```

## Limitations

- **Generic methods** within interfaces are not yet supported
- The KnockOff class must specify **concrete type arguments**

```csharp
// NOT supported - generic KnockOff class
[KnockOff]
public partial class GenericRepoKnockOff<T> : IRepository<T> { }  // Won't work

// Supported - concrete type
[KnockOff]
public partial class UserRepoKnockOff : IRepository<User> { }  // Works
```
