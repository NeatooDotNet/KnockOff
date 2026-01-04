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
