# Multiple Interfaces

Starting in v10.9, standalone KnockOff stubs implement **one interface** (plus its inheritance chain). For multiple unrelated interfaces, use inline stubs or separate standalone stubs.

## Single Interface Constraint

Standalone stubs must implement exactly one interface:

<!-- invalid:multiple-interface-constraint -->
```csharp
// Valid - single interface
[KnockOff]
public partial class UserServiceKnockOff : IUserService { }

// Valid - interface with inheritance chain
[KnockOff]
public partial class EntityKnockOff : IEntityBase { }  // IEntityBase : IValidatable

// INVALID - multiple unrelated interfaces (emits KO0010)
[KnockOff]
public partial class DataContextKnockOff : IRepository, IUnitOfWork { }  // Error!
```
<!-- /snippet -->

**Diagnostic KO0010:** "KnockOff stubs should implement a single interface. Create separate stubs for IRepository and IUnitOfWork."

## Option 1: Separate Standalone Stubs

Create separate stub classes for each interface:

<!-- pseudo:separate-standalone-stubs -->
```csharp
[KnockOff]
public partial class RepositoryKnockOff : IRepository { }

[KnockOff]
public partial class UnitOfWorkKnockOff : IUnitOfWork { }

// Usage
var repo = new RepositoryKnockOff();
var uow = new UnitOfWorkKnockOff();

// Configure each independently
repo.GetById.OnCall = (ko, id) => new User { Id = id };
uow.SaveChangesAsync.OnCall = (ko, ct) => Task.FromResult(1);
```
<!-- /snippet -->

## Option 2: Inline Stubs

For tests that need multiple interfaces, use inline stubs with `[KnockOff<T>]` attributes on the test class:

<!-- pseudo:inline-stubs-multiple -->
```csharp
[KnockOff<IRepository>]
[KnockOff<IUnitOfWork>]
public partial class DataContextTests
{
    [Fact]
    public async Task SaveChanges_ReturnsAddCount()
    {
        var repo = new Stubs.IRepository();
        var uow = new Stubs.IUnitOfWork();

        // Configure via flat API
        uow.SaveChangesAsync.OnCall = (ko, ct) => Task.FromResult(repo.Add.CallCount);

        repo.Add.OnCall = (ko, user) => { };

        // Use in test
        IRepository repoService = repo;
        IUnitOfWork uowService = uow;

        repoService.Add(new User { Name = "New" });
        repoService.Add(new User { Name = "Another" });
        var saved = await uowService.SaveChangesAsync();

        Assert.Equal(2, saved);
    }
}
```
<!-- /snippet -->

See [Inline Stubs Guide](inline-stubs.md) for more details.

## Why Single Interface?

The single interface constraint exists because:

1. **Stubs satisfy dependencies.** Dependencies are typed as a single interface. Multiple unrelated interfaces on one stub is not a realistic use case.

2. **Flat API simplicity.** With a single interface, member names don't conflict and don't need disambiguation prefixes.

3. **Interface inheritance is different.** Implementing `IEntityBase` (which extends `IValidatable`) is still one interface - the inheritance chain is flattened automatically.

## Migration from v10

If you have existing stubs implementing multiple interfaces:

<!-- pseudo:migration-from-multiple -->
```csharp
// v10 - worked but now deprecated
[KnockOff]
public partial class DataContextKnockOff : IRepository, IUnitOfWork { }

// v10.9 - Option A: Separate stubs
[KnockOff]
public partial class RepositoryKnockOff : IRepository { }

[KnockOff]
public partial class UnitOfWorkKnockOff : IUnitOfWork { }

// v10.9 - Option B: Inline stubs on test class
[KnockOff<IRepository>]
[KnockOff<IUnitOfWork>]
public partial class DataContextTests { }
```
<!-- /snippet -->

## Related Guides

- [Inline Stubs](inline-stubs.md) - Creating stubs inside test classes
- [Interface Inheritance](interface-inheritance.md) - Single interface with inheritance chain
