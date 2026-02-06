[Home](../../README.md) > [Guides](.) > Source Delegation

# Source Delegation

`stub.Source(realImplementation)` tells every interceptor on the stub to forward unconfigured calls to a real implementation. Configured methods (OnCall, Returns, When) still take priority — the source is only consulted when nothing else is configured for that member.

**Availability**: Source delegation is available for **interface stubs** only (Standalone and Inline patterns). Class stubs inherit from the base class directly and do not need `Source()`.

---

## What Source Does

When you call `stub.Source(realImplementation)`, KnockOff sets a `_source` field on every interceptor. When a method is called:

1. If OnCall, Returns, or a When chain is configured — use that
2. If a user method override exists (Standalone patterns) — use that
3. If a source is set — **forward the call to the real implementation**
4. Otherwise — return the smart default

This means you get real behavior for free on every method, and you only configure the ones your test cares about.

---

## The Problem Source Solves

Without Source, testing a decorator or integration scenario that needs mostly-real behavior requires manually forwarding every method:

```cs
var realRepo = new SqlUserRepo(connectionString);
var stub = new UserRepoStub();

// Without Source — manually forward EVERY method
stub.GetUser.OnCall((id) => realRepo.GetUser(id));
stub.GetUserAsync.OnCall((id) => realRepo.GetUserAsync(id));
stub.Save.OnCall((user) => realRepo.Save(user));
stub.Delete.OnCall((id) => realRepo.Delete(id));
stub.GetAll.OnCall(() => realRepo.GetAll());
// ... 10 more methods ...

// NOW you can override the one method you're testing
stub.GetUser.OnCall((id) => new User { Id = id, Name = "Test User" });
// But wait — you just overwrote the forwarding you set up above
```

This is tedious, error-prone, and breaks when the interface changes. With Source:

<!-- snippet: source-basic -->
```cs
// Configure stub to delegate to real implementation
stub.Source(realStore);
```
<!-- endSnippet -->

One line. Every method forwards. Now override just the ones you need.

---

## Partial Stubbing

Set the source for baseline behavior, then override specific members:

<!-- snippet: source-partial-override -->
```cs
// Override specific member while source handles the rest
stub.GetById.OnCall((id) => new User { Id = id, Name = "Test User" });
```
<!-- endSnippet -->

In a complete test this looks like:

```cs
var stub = new SourceRepoStub();
var realRepo = new SimpleRepository();
realRepo.Save(new User { Id = 1, Name = "Real User" });

stub.Source(realRepo);
stub.GetById.OnCall((id) => new User { Id = id, Name = "Test User" });

IRepository repository = stub;

// GetById uses your override
var user = repository.GetById(1);
Assert.Equal("Test User", user.Name);

// Save delegates to the real implementation — no configuration needed
repository.Save(new User { Id = 2, Name = "New User" });
Assert.NotNull(realRepo.GetById(2));
```

---

## OnCall Takes Full Control

Once OnCall is configured for a member, the source is **never consulted** for that member — even if your callback returns null:

<!-- snippet: source-complete-example -->
```cs
// OnCall takes full control - source not consulted even for non-matches
stub.Read.OnCall((filename) =>
    filename == "config.txt" ? "Test Config" : null);
```
<!-- endSnippet -->

```cs
var realDataSource = new FileDataSource();
realDataSource.Write("config.txt", "Production Config");
realDataSource.Write("data.txt", "Production Data");

stub.Source(realDataSource);
stub.Read.OnCall((filename) =>
    filename == "config.txt" ? "Test Config" : null);

IDataSource ds = stub;

ds.Read("config.txt");  // "Test Config" — from OnCall
ds.Read("data.txt");     // null — OnCall returned null, source NOT consulted

// Write has no OnCall — delegates to source
ds.Write("output.txt", "New Data");  // Goes to real FileDataSource
```

If you need the source to handle some arguments and your override to handle others, use a When chain instead of OnCall:

```cs
stub.Read.When("config.txt").Returns("Test Config");
// All other filenames fall through to source
```

---

## Clearing Source

Remove source delegation by passing null:

<!-- snippet: source-clear -->
```cs
// Clear source to revert to smart defaults
stub.Source(null);
```
<!-- endSnippet -->

After clearing, unconfigured methods return defaults (or throw in strict mode). This is useful when you need source delegation for test setup but want to verify stub behavior independently later.

Note: `Reset()` on an individual interceptor also clears its source reference. If you reset a member and still want delegation, call `stub.Source(realImplementation)` again.

---

## Priority Order

KnockOff evaluates member calls in this order:

1. **When chains** — `stub.Method.When(...).Returns(...)`
2. **OnCall / Returns** — `stub.Method.OnCall(...)` or `stub.Method.Returns(...)`
3. **User methods** — `protected override` with `_` suffix (Standalone only)
4. **Source delegation** — `stub.Source(realImplementation)`
5. **Smart default** — KnockOff's built-in default value

The first match wins. This makes Source ideal as a baseline: set it once, then selectively override specific members at higher priority levels.

---

## When to Use Source

**Use `Source()` when:**
- Testing decorator or wrapper patterns where you want real behavior by default
- Integration tests that need mostly-real dependencies with a few test overrides
- Large interfaces where manually configuring every member is impractical
- You need to verify interactions while still exercising real logic on other methods

**Don't use `Source()` when:**
- You want full isolation with no real dependencies (use pure stubbing)
- The source has side effects you want to avoid (database, network, file I/O)
- You need complete control over all return values

---

**Next Steps:**
- [Methods Guide](methods.md) - Complete guide to OnCall, Returns, and When chains
- [User Methods Guide](user-methods.md) - Default behavior through override methods
- [Verification Guide](verification.md) - Assert on stub interactions
