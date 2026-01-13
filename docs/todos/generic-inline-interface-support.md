# Generic Inline Interface Support

## Problem

KnockOff supports **closed** generic inline interface stubs but not **open** generic inline stubs.

**Works:**
```csharp
[KnockOff<IRepository<string>>]  // Closed - T is string
public partial class MyTests { }

// Generates: MyTests.Stubs.Repository (implements IRepository<string>)
```

**Not Supported:**
```csharp
[KnockOff(typeof(IRepository<>))]  // Open generic - NOT IMPLEMENTED
public partial class MyTests { }
```

## Use Case

User wants an inline stub for a generic interface without pre-declaring every type argument:

```csharp
[KnockOff(typeof(IRepository<>))]
public partial class MyTests { }

// Desired:
var userRepo = new MyTests.Stubs.Repository<User>();
var orderRepo = new MyTests.Stubs.Repository<Order>();
```

## Current Workaround

**Option 1:** Create separate inline stubs for each closed type needed:

```csharp
[KnockOff<IRepository<User>>]
[KnockOff<IRepository<Order>>]
[KnockOff<IRepository<Product>>]
public partial class MyTests { }

// Use: new MyTests.Stubs.RepositoryUser(), etc.
```

**Option 2:** Use generic standalone stub (v10.14+):

```csharp
[KnockOff]
public partial class RepositoryStub<T> : IRepository<T> where T : class { }

// Use: new RepositoryStub<User>(), etc.
```

## Solution

Use `typeof()` syntax with an open generic interface. The generator creates a generic stub class:

```csharp
// User writes (non-generic test class):
[KnockOff(typeof(IRepository<>))]
public partial class MyTests { }

// Generator produces:
public partial class MyTests
{
    public static partial class Stubs
    {
        public class Repository<T>  // Generator makes the stub generic
        {
            // Interface implementation with T
            public T? GetById.Value;
            public IRepository_T_GetByIdInterceptor<T> GetById { get; }
            // etc.
        }
    }
}
```

### Usage in Tests

```csharp
public class MyTests
{
    [Fact]
    public void Repository_Returns_User()
    {
        var stub = new MyTests.Stubs.Repository<User>();
        stub.GetById.OnCall = (ko, id) => new User { Id = id };

        IRepository<User> repo = stub;
        var user = repo.GetById(42);

        Assert.Equal(42, user.Id);
    }

    [Fact]
    public void Repository_Returns_Order()
    {
        var stub = new MyTests.Stubs.Repository<Order>();
        stub.GetById.OnCall = (ko, id) => new Order { Id = id };

        IRepository<Order> repo = stub;
        var order = repo.GetById(99);

        Assert.Equal(99, order.Id);
    }
}
```

### Why This Is Useful

| Approach | Pros | Cons |
|----------|------|------|
| Closed inline (`[KnockOff<IRepo<User>>]`) | Simple | Must pre-declare every type |
| Generic standalone (`class Stub<T> : IRepo<T>`) | Flexible | Separate class, not inline |
| Open inline (`[KnockOff(typeof(IRepo<>))]`) | Flexible AND inline | More complex generator logic |

Open inline keeps stubs organized inside the test class while supporting any type argument.

## Task List

- [ ] Add `[KnockOff(Type type)]` constructor overload to attribute (if not present)
- [ ] Update predicate to accept non-generic `KnockOffAttribute` with Type argument
- [ ] In transform, detect `typeof(IInterface<>)` with unbound type arguments
- [ ] Extract type parameters from unbound generic type
- [ ] Generate generic stub class with those type parameters
- [ ] Generate generic interceptor classes (delegates need type params too)
- [ ] Handle multiple type parameters: `IRepo<T, U>` → `Stubs.Repo<T, U>`
- [ ] Preserve type constraints from interface definition
- [ ] Add tests for open generic inline interface stubs
- [ ] Update documentation

## Technical Notes

### Generator Detection

```csharp
// Check for [KnockOff(typeof(IFoo<>))] pattern
if (attributeData.ConstructorArguments.Length > 0
    && attributeData.ConstructorArguments[0].Value is INamedTypeSymbol typeSymbol
    && typeSymbol.IsUnboundGenericType)
{
    // Open generic - generate generic stub
    var typeParams = typeSymbol.TypeParameters;
}
```

### Interceptor Classes Must Also Be Generic

The interceptor classes need the type parameter too:

```csharp
public class Repository<T>
{
    public IRepository_T_GetByIdInterceptor<T> GetById { get; }

    public class IRepository_T_GetByIdInterceptor<T>
    {
        public Func<Repository<T>, int, T?> OnCall { get; set; }
        // ...
    }
}
```

## Priority

Medium - workarounds exist (closed inline or generic standalone).

## Related

- Generic standalone stubs for interfaces (implemented in v10.14)
- Generic delegate support (separate todo)
- Closed generic inline stubs (working)
