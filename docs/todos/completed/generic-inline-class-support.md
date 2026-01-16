# Generic Inline Class Support

**Status:** Complete
**Completed:** v10.19

## Problem

KnockOff supports **closed** generic inline class stubs but not **open** generic inline stubs.

**Works:**
```csharp
public abstract class Repository<T>
{
    public abstract T? GetById(int id);
    public abstract void Save(T entity);
}

[KnockOff<Repository<User>>]  // Closed - T is User
public partial class MyTests { }

// Generates: MyTests.Stubs.Repository (extends Repository<User>)
```

**Not Supported:**
```csharp
[KnockOff(typeof(Repository<>))]  // Open generic - NOT IMPLEMENTED
public partial class MyTests { }
```

## Use Case

User wants an inline stub for a generic abstract class without pre-declaring every type argument:

```csharp
[KnockOff(typeof(Repository<>))]
public partial class MyTests { }

// Desired:
var userRepo = new MyTests.Stubs.Repository<User>();
var orderRepo = new MyTests.Stubs.Repository<Order>();
```

## Current Workaround

Create separate inline stubs for each closed type needed:

```csharp
[KnockOff<Repository<User>>]
[KnockOff<Repository<Order>>]
[KnockOff<Repository<Product>>]
public partial class MyTests { }

// Use: new MyTests.Stubs.RepositoryUser(), etc.
```

**Note:** Generic standalone stubs (`[KnockOff] class Stub<T> : BaseClass<T>`) do NOT work for class inheritance. The standalone pattern only supports interface implementation. This is why open generic inline class support is needed.

## Solution

Use `typeof()` syntax with an open generic class. The generator creates a generic stub class:

```csharp
// User writes (non-generic test class):
[KnockOff(typeof(Repository<>))]
public partial class MyTests { }

// Generator produces:
public partial class MyTests
{
    public static partial class Stubs
    {
        public class Repository<T> : global::Repository<T>  // Generic stub
        {
            public override T? GetById(int id)
            {
                GetById.CallCount++;
                return GetById.OnCall?.Invoke(this, id) ?? default;
            }

            public Repository_GetByIdInterceptor<T> GetById { get; } = new();

            // Interceptor class also generic
            public class Repository_GetByIdInterceptor<T>
            {
                public Func<Repository<T>, int, T?>? OnCall { get; set; }
                public int CallCount { get; private set; }
            }
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

        Repository<User> repo = stub;
        var user = repo.GetById(42);

        Assert.Equal(42, user.Id);
    }

    [Fact]
    public void Repository_Returns_Order()
    {
        var stub = new MyTests.Stubs.Repository<Order>();
        stub.GetById.OnCall = (ko, id) => new Order { Id = id };

        Repository<Order> repo = stub;
        var order = repo.GetById(99);

        Assert.Equal(99, order.Id);
    }
}
```

## Task List

- [x] Verify standalone generic class stubs work → **Result: Not supported** (standalone requires interface implementation)
- [ ] Add `[KnockOff(Type type)]` constructor overload to attribute (if not present)
- [ ] Update predicate to accept non-generic `KnockOffAttribute` with Type argument
- [ ] In transform, detect `typeof(SomeClass<>)` with unbound type arguments
- [ ] Extract type parameters from unbound generic class
- [ ] Generate generic stub class that extends the base class
- [ ] Generate generic interceptor classes
- [ ] Handle multiple type parameters: `BaseClass<T, U>` → `Stubs.BaseClass<T, U>`
- [ ] Preserve type constraints from class definition
- [ ] Add tests for open generic inline class stubs
- [ ] Update documentation

## Technical Notes

### Standalone Generic Class Stubs

The standalone pattern may already work (untested):

```csharp
[KnockOff]
public partial class RepositoryStub<T> : Repository<T> { }
```

This uses the same code path as generic interface stubs. First task should verify this works before implementing inline support.

### Generator Detection

```csharp
// Check for [KnockOff(typeof(SomeClass<>))] pattern
if (attributeData.ConstructorArguments.Length > 0
    && attributeData.ConstructorArguments[0].Value is INamedTypeSymbol typeSymbol
    && typeSymbol.IsUnboundGenericType
    && typeSymbol.TypeKind == TypeKind.Class)
{
    // Open generic class - generate generic stub
    var typeParams = typeSymbol.TypeParameters;
}
```

### Class vs Interface Differences

| Aspect | Interface Stub | Class Stub |
|--------|---------------|------------|
| Relationship | Implements | Extends |
| Methods | Explicit implementation | Override virtual/abstract |
| Constructor | None | Must call base constructor |
| Single inheritance | N/A | Stub can only extend one class |

## Priority

Low - workarounds exist (closed inline or standalone).

## Related

- Generic standalone stubs for interfaces (implemented in v10.14)
- Generic inline interface support (separate todo)
- Generic delegate support (separate todo)
- Closed generic inline class stubs (working)
