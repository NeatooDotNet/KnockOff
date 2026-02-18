[Home](../../README.md) > [Guides](.) > Multiple Interfaces

# Multiple Interfaces

KnockOff handles multiple interfaces through two mechanisms: **interface inheritance** (a single stub covers the full hierarchy with unified interceptors) and **inline stubs** (multiple `[KnockOff<T>]` attributes on one test class for unrelated interfaces).

---

## The Comparison

### The scenario

A service that implements both a repository and a unit of work:

<!-- snippet: multi-unrelated-interfaces -->
```cs
public interface IRepository
{
    User? GetUser(int id);
    void Save(User user);
}

public interface IUnitOfWork
{
    void Commit();
    void Rollback();
}
```
<!-- endSnippet -->

And an interface hierarchy:

<!-- snippet: multi-hierarchy-interfaces -->
```cs
public interface IReadableStore
{
    string? GetById(int id);
    int Count { get; }
}

public interface IStore : IReadableStore
{
    void Save(int id, string value);
    void Delete(int id);
}
```
<!-- endSnippet -->

### KnockOff

**Interface inheritance** — a single stub covers the full hierarchy. All interceptors are unified:

<!-- snippet: multi-hierarchy-unified -->
```cs
var stub = new Stubs.IStore();

// All members from IStore AND IReadableStore — same flat API
stub.GetById.Call((id) => $"item-{id}");  // from IReadableStore
stub.Count.Get(42);                           // from IReadableStore
stub.Save.Call(_ => { });           // from IStore
stub.Delete.Call((id) => { });                // from IStore

IStore store = stub;
Assert.Equal("item-1", store.GetById(1));
Assert.Equal(42, store.Count);
```
<!-- endSnippet -->

Source delegation works per interface level — pass a partial implementation and only matching members get delegated:

<!-- snippet: multi-source-per-level -->
```cs
var stub = new Stubs.IStore();
var readOnlySource = new ReadOnlyStore(
    new Dictionary<int, string> { [1] = "value-1" });

// Only delegates GetById and Count — Save and Delete remain unconfigured
stub.Source(readOnlySource);
```
<!-- endSnippet -->

**Unrelated interfaces** — use inline stubs:

<!-- snippet: multi-unrelated-stubs -->
```cs
var repo = new Stubs.IRepository();
var uow = new Stubs.IUnitOfWork();

repo.GetUser.Call((id) => new User { Id = id });
uow.Commit.Call(() => { }).Verifiable();

// Pass both to the system under test
var service = new MyService(repo, uow);
service.UpdateUser(1);

uow.Verify();
```
<!-- endSnippet -->

### Moq

Moq uses `mock.As<T>()` to add interfaces to a single mock:

```csharp
var mock = new Mock<IRepository>();

// Add second interface
var uow = mock.As<IUnitOfWork>();

// Setup on primary interface
mock.Setup(x => x.GetUser(It.IsAny<int>()))
    .Returns<int>(id => new User { Id = id });

// Setup on secondary interface — different variable
uow.Setup(x => x.Commit());

// Cast to get the secondary interface
var service = new MyService(mock.Object, (IUnitOfWork)mock.Object);

// Verify on secondary — need the As<T> reference
uow.Verify(x => x.Commit(), Times.Once());
```

**Pain points:**
- `.As<T>()` returns a separate `Mock<T>` reference — setup and verify are split across variables
- With 3+ interfaces, you juggle multiple references: `mock`, `asRepo`, `asUow`, `asLogger`...
- Easy to forget which variable corresponds to which interface
- Must cast `mock.Object` to access secondary interfaces

### NSubstitute

NSubstitute supports multiple interfaces via `Substitute.For<T1, T2>()`:

```csharp
var sub = Substitute.For<IRepository, IUnitOfWork>();

// Primary interface — works directly
sub.GetUser(Arg.Any<int>()).Returns(x => new User { Id = x.Arg<int>() });

// Secondary interface — must cast
((IUnitOfWork)sub).Commit();

// Verify on secondary — must cast
((IUnitOfWork)sub).Received().Commit();
```

**Pain points:**
- Must cast to access non-primary interfaces: `((IUnitOfWork)sub).Commit()`
- Casts are noisy with 3+ interfaces
- `Substitute.For<T1, T2, T3>()` supports up to 3 type parameters; beyond that requires the params overload

---

## Summary

| Capability | KnockOff | Moq | NSubstitute |
|------------|----------|-----|-------------|
| Interface hierarchy | Unified flat API: `stub.Member` | Same as single interface | Same as single interface |
| Unrelated interfaces | Separate inline stubs or standalone stubs | `.As<T>()` — split setup/verify | `Substitute.For<T1, T2>()` — cast for secondary |
| Source delegation per level | `stub.Source(partialImpl)` — per interface | Not available | Not available |
| 3+ interfaces | One `[KnockOff<T>]` per interface | Multiple `.As<T>()` variables | Cast for each non-primary interface |
| Verify across interfaces | Each stub has its own `Verify()` | Verify on each `.As<T>()` ref | Cast + `.Received()` per interface |
