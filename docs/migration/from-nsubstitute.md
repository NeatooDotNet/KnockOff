# Migrating from NSubstitute to KnockOff

Switching from NSubstitute to KnockOff means moving from runtime dynamic proxies to compile-time generated stubs. You gain compile-time verification and reusable stub classes, while trading NSubstitute's runtime flexibility and elegant fluent API for explicit, but more verbose, interceptor patterns.

This guide walks you through the migration step-by-step, with side-by-side comparisons and honest trade-off analysis.

---

## Before You Migrate: Is KnockOff Right for You?

NSubstitute is a mature, battle-tested framework with an exceptionally clean API. Before migrating, consider whether KnockOff's benefits justify the trade-offs:

**Consider KnockOff if:**
- You want to share stub implementations across multiple test files
- You value compile-time verification of stub configurations
- You prefer explicit, inspectable generated code over runtime magic
- Performance overhead from dynamic proxies is a concern

**Stick with NSubstitute if:**
- You prefer NSubstitute's elegant fluent API (`.Returns()`, `.Received()`)
- Your tests rarely share stub implementations
- You rely heavily on recursive mocks or auto-substitution
- Your team is already proficient with NSubstitute patterns

---

## What Changes

**NSubstitute's approach:**
- Runtime dynamic proxy generation
- Fluent `.Returns()` API for configuring behavior
- Intuitive `.Received()` / `.DidNotReceive()` for verification
- `Arg.Any<T>()` and `Arg.Is<T>()` for argument matching
- Seamless async support (no explicit `Task.FromResult`)
- Recursive mocking and auto-substitution

**KnockOff's approach:**
- Compile-time source generation with partial classes
- `Returns()` or `OnCall()` delegates for behavior configuration
- `Verifiable()` + `Verify()` for batch verification
- `When()` API for declarative argument matching (similar to `Arg.Is<T>()`)
- Auto-wrapped `Task.FromResult()` for async methods with `Returns()`
- No recursive mocking support

**What stays the same:**
- You still create test doubles for interfaces and classes
- You still configure behavior and verify calls
- Your test goals and patterns remain unchanged

---

## Honest Trade-Off Analysis

### Where NSubstitute is Better

| Feature | NSubstitute | KnockOff | Verdict |
|---------|-------------|----------|---------|
| **API elegance** | `.Returns(42)` | `.Returns(42)` or `OnCall(() => 42)` | Comparable |
| **Verification readability** | `sub.Received().Method()` | `tracking.Verify(Times.Once)` | NSub is more intuitive |
| **Async setup** | `.Returns(user)` auto-wraps | `.Returns(user)` auto-wraps | Comparable |
| **Learning curve** | Familiar to most C# devs | New patterns to learn | NSub wins |
| **Recursive mocks** | Built-in support | Not supported | NSub only |

### Where KnockOff is Better

| Feature | KnockOff | NSubstitute | Verdict |
|---------|----------|-------------|---------|
| **Compile-time safety** | Setup errors caught at build | Runtime errors | KnockOff wins |
| **Stub reusability** | Define once, share everywhere | Duplicate setup | KnockOff wins |
| **Generated code** | Visible, debuggable | Hidden proxy magic | KnockOff wins |
| **Performance** | Zero reflection overhead | Dynamic proxy cost | KnockOff wins |
| **Default behavior** | Configure in stub class | Repeat in each test | KnockOff wins |
| **Argument access** | `(a, b) => a + b` typed params | `callInfo.Arg<int>()` or `ArgAt<>()` | KnockOff wins |
| **Parameter matching** | `When((a, b) => a > 0)` | `Arg.Is<T>(x => ...)` per param | Comparable |
| **Source delegation** | `stub.Source(realImpl)` | Not supported | KnockOff only |

---

## Quick Reference

| NSubstitute Pattern | KnockOff Equivalent |
|---------------------|---------------------|
| `Substitute.For<IFoo>()` | `new FooStub()` with `[KnockOff] partial class FooStub : IFoo` |
| `.Returns(value)` | `stub.Method.Returns(value)` or `stub.Method.OnCall(() => value)` |
| `.Returns(callInfo => ...)` | `stub.Method.OnCall((args) => ...)` |
| `.ReturnsForAnyArgs(value)` | `stub.Method.Returns(value)` (inherently matches any) |
| `.When(x => x.Method()).Do(...)` | Logic in `OnCall` delegate |
| `.Returns(...).AndDoes(...)` | Combine in single `OnCall` delegate |
| `.Received()` | `stub.Method.Verify(Times.AtLeastOnce)` or `.Verifiable()` |
| `.Received(n)` | `stub.Method.Verify(Times.Exactly(n))` |
| `.DidNotReceive()` | `tracking.Verify(Times.Never)` |
| `Arg.Any<T>()` | Callback receives all arguments (default behavior) |
| `Arg.Is<T>(predicate)` | `stub.Method.When((args) => predicate).Returns(value)` |
| `.Returns(v1, v2, v3)` | `stub.Method.OnCall(() => v1).ThenCall(() => v2).ThenCall(() => v3)` |
| `.ClearReceivedCalls()` | `stub.Method.Reset()` |
| `sub.Property.Returns(value)` | `stub.Property.OnGet(value)` |

---

## Step 1: Install KnockOff

Replace the NSubstitute package with KnockOff.

```bash
# Remove NSubstitute:
dotnet remove package NSubstitute

# Add KnockOff:
dotnet add package KnockOff
```

---

## Step 2: Create Stubs

Replace `Substitute.For<T>()` calls with KnockOff stub classes.

**NSubstitute:**

<!-- snippet: nsub-migration-create-stub-nsub -->
```cs
[Fact]
public void CreateStub_NSubstituteApproach()
{
    var substitute = Substitute.For<INSubUserRepo>();
    INSubUserRepo repository = substitute;

    Assert.NotNull(repository);
}
```
<!-- endSnippet -->

**KnockOff:**

<!-- snippet: nsub-migration-create-stub-knockoff -->
```cs
[Fact]
public void CreateStub_KnockOffApproach()
{
    var stub = new NSubUserRepoStub();
    INSubUserRepo repository = stub;

    Assert.NotNull(repository);
}
```
<!-- endSnippet -->

**Key differences:**
- NSubstitute creates proxies at runtime with a single line
- KnockOff requires a partial class declaration (one-time setup)
- The stub is used directly (no wrapper object)

**Trade-off:** NSubstitute's `Substitute.For<T>()` is undeniably simpler. KnockOff's upfront declaration pays off when you reuse the stub across multiple test files.

---

## Step 3: Configure Returns

Replace `.Returns()` with `OnCall` property assignments.

**NSubstitute:**

<!-- snippet: nsub-migration-returns-nsub -->
```cs
[Fact]
public void Returns_NSubstituteApproach()
{
    var substitute = Substitute.For<INSubUserRepo>();
    var testUser = new User { Id = 42, Name = "Alice" };

    // NSubstitute's elegant fluent API
    substitute.GetUser(Arg.Any<int>()).Returns(testUser);

    INSubUserRepo repository = substitute;
    var user = repository.GetUser(42);

    Assert.NotNull(user);
    Assert.Equal("Alice", user.Name);
}
```
<!-- endSnippet -->

**KnockOff:**

<!-- snippet: nsub-migration-returns-knockoff -->
```cs
[Fact]
public void Returns_KnockOffApproach()
{
    var stub = new NSubUserRepoStub();
    var testUser = new User { Id = 42, Name = "Alice" };

    // KnockOff uses OnCall with typed delegate
    stub.GetUser.OnCall((id) => testUser);

    INSubUserRepo repository = stub;
    var user = repository.GetUser(42);

    Assert.NotNull(user);
    Assert.Equal("Alice", user.Name);
}
```
<!-- endSnippet -->

**Trade-off:** NSubstitute's `.Returns()` reads like English. KnockOff's `OnCall` is more explicit but requires a lambda. However, KnockOff gives you typed access to arguments directly in the delegate.

---

## Step 4: Configure ReturnsForAnyArgs

Replace `.ReturnsForAnyArgs()` with standard `OnCall`.

**NSubstitute:**

<!-- snippet: nsub-migration-returns-anyargs-nsub -->
```cs
[Fact]
public void ReturnsForAnyArgs_NSubstituteApproach()
{
    var substitute = Substitute.For<INSubUserRepo>();
    var testUser = new User { Id = 1, Name = "Default" };

    // ReturnsForAnyArgs: matches any argument combination
    substitute.GetUser(default).ReturnsForAnyArgs(testUser);

    INSubUserRepo repository = substitute;
    var user1 = repository.GetUser(1);
    var user2 = repository.GetUser(999);

    Assert.Equal("Default", user1?.Name);
    Assert.Equal("Default", user2?.Name);
}
```
<!-- endSnippet -->

**KnockOff:**

<!-- snippet: nsub-migration-returns-anyargs-knockoff -->
```cs
[Fact]
public void ReturnsForAnyArgs_KnockOffApproach()
{
    var stub = new NSubUserRepoStub();
    var testUser = new User { Id = 1, Name = "Default" };

    // KnockOff: OnCall inherently matches any arguments
    // (no separate "ForAnyArgs" needed)
    stub.GetUser.OnCall((id) => testUser);

    INSubUserRepo repository = stub;
    var user1 = repository.GetUser(1);
    var user2 = repository.GetUser(999);

    Assert.Equal("Default", user1?.Name);
    Assert.Equal("Default", user2?.Name);
}
```
<!-- endSnippet -->

**Trade-off:** NSubstitute has an explicit `.ReturnsForAnyArgs()` method that makes intent clear. KnockOff's `OnCall` inherently matches any arguments, so no special method is needed. This is simpler but less declarative.

---

## Step 5: Configure Returns with Argument Access

Replace `callInfo.Arg<T>()` with direct argument access.

**NSubstitute:**

<!-- snippet: nsub-migration-returns-args-nsub -->
```cs
[Fact]
public void ReturnsWithArgs_NSubstituteApproach()
{
    var substitute = Substitute.For<INSubUserRepo>();

    // NSubstitute: Access args through callback in Returns
    substitute.GetUser(Arg.Any<int>())
        .Returns(callInfo => new User
        {
            Id = callInfo.Arg<int>(),
            Name = $"User{callInfo.Arg<int>()}"
        });

    INSubUserRepo repository = substitute;
    var user = repository.GetUser(42);

    Assert.NotNull(user);
    Assert.Equal(42, user.Id);
    Assert.Equal("User42", user.Name);
}
```
<!-- endSnippet -->

**KnockOff:**

<!-- snippet: nsub-migration-returns-args-knockoff -->
```cs
[Fact]
public void ReturnsWithArgs_KnockOffApproach()
{
    var stub = new NSubUserRepoStub();

    // KnockOff: Arguments are directly available in the delegate
    stub.GetUser.OnCall((id) => new User
    {
        Id = id,
        Name = $"User{id}"
    });

    INSubUserRepo repository = stub;
    var user = repository.GetUser(42);

    Assert.NotNull(user);
    Assert.Equal(42, user.Id);
    Assert.Equal("User42", user.Name);
}
```
<!-- endSnippet -->

**Trade-off:** KnockOff wins here. Direct typed argument access (`id`) is cleaner than NSubstitute's `callInfo.Arg<int>()` or `callInfo.ArgAt<int>(0)`.

---

## Step 6: Configure Properties

Replace property `.Returns()` with `.Value` assignments.

**NSubstitute:**

<!-- snippet: nsub-migration-property-nsub -->
```cs
[Fact]
public void PropertySetup_NSubstituteApproach()
{
    var substitute = Substitute.For<INSubUserRepo>();

    // NSubstitute: elegant property Returns
    substitute.ConnectionString.Returns("server=localhost");
    substitute.IsConnected.Returns(true);

    INSubUserRepo repository = substitute;

    Assert.Equal("server=localhost", repository.ConnectionString);
    Assert.True(repository.IsConnected);
}
```
<!-- endSnippet -->

**KnockOff:**

<!-- snippet: nsub-migration-property-knockoff -->
```cs
[Fact]
public void PropertySetup_KnockOffApproach()
{
    var stub = new NSubUserRepoStub();

    // KnockOff: Use OnGet(value) for all properties
    stub.ConnectionString.OnGet("server=localhost");
    // For read-only properties, also use OnGet(value)
    stub.IsConnected.OnGet(true);

    INSubUserRepo repository = stub;

    Assert.Equal("server=localhost", repository.ConnectionString);
    Assert.True(repository.IsConnected);
}
```
<!-- endSnippet -->

**Trade-off:** NSubstitute's `.Returns()` is consistent with methods. KnockOff's `.OnGet()` is more explicit about configuring the getter behavior.

---

## Step 7: Verify Calls (Received)

Replace `.Received()` with `Verifiable()` + `Verify()` or direct `Verify(Times)`.

**NSubstitute:**

<!-- snippet: nsub-migration-received-nsub -->
```cs
[Fact]
public void Received_NSubstituteApproach()
{
    var substitute = Substitute.For<INSubUserRepo>();

    INSubUserRepo repository = substitute;
    repository.SaveUser(new User { Id = 1, Name = "Bob" });

    // NSubstitute's intuitive Received() syntax
    substitute.Received().SaveUser(Arg.Any<User>());
    substitute.Received(1).SaveUser(Arg.Any<User>());
}
```
<!-- endSnippet -->

**KnockOff:**

<!-- snippet: nsub-migration-received-knockoff -->
```cs
[Fact]
public void Received_KnockOffApproach()
{
    var stub = new NSubUserRepoStub();

    // Mark method as verifiable during setup
    stub.SaveUser.OnCall((user) => { }).Verifiable();

    INSubUserRepo repository = stub;
    repository.SaveUser(new User { Id = 1, Name = "Bob" });

    // Verify() checks all members marked with .Verifiable()
    stub.Verify();

    // Or verify with explicit Times via tracking
    stub.SaveUser.Verify(Times.Once);
}
```
<!-- endSnippet -->

**Trade-off:** NSubstitute's `.Received()` is genuinely more intuitive. `substitute.Received().Method()` reads naturally. KnockOff's approach requires setup-time configuration (`.Verifiable()`) and a separate `Verify()` call. This is one area where NSubstitute's design is simply better.

---

## Step 8: Verify No Calls (DidNotReceive)

Replace `.DidNotReceive()` with `Times.Never`.

**NSubstitute:**

<!-- snippet: nsub-migration-didnotreceive-nsub -->
```cs
[Fact]
public void DidNotReceive_NSubstituteApproach()
{
    var substitute = Substitute.For<INSubUserRepo>();

    INSubUserRepo repository = substitute;
    // Don't call DeleteUser

    // NSubstitute's DidNotReceive - beautifully readable
    substitute.DidNotReceive().DeleteUser(Arg.Any<int>());
}
```
<!-- endSnippet -->

**KnockOff:**

<!-- snippet: nsub-migration-didnotreceive-knockoff -->
```cs
[Fact]
public void DidNotReceive_KnockOffApproach()
{
    var stub = new NSubUserRepoStub();
    var tracking = stub.DeleteUser.OnCall((id) => { });

    INSubUserRepo repository = stub;
    // Don't call DeleteUser

    // KnockOff: Use Times.Never for "did not receive"
    tracking.Verify(Times.Never);
}
```
<!-- endSnippet -->

**Trade-off:** NSubstitute's `.DidNotReceive()` is self-documenting. KnockOff's `Times.Never` achieves the same result but requires knowing the `Times` API.

---

## Step 9: Verify with Specific Arguments

Replace `.Received()` with specific arguments with argument capture or `LastCallArg`.

**NSubstitute:**

<!-- snippet: nsub-migration-received-args-nsub -->
```cs
[Fact]
public void ReceivedWithArgs_NSubstituteApproach()
{
    var substitute = Substitute.For<INSubUserRepo>();

    INSubUserRepo repository = substitute;
    repository.GetUser(42);
    repository.GetUser(99);

    // NSubstitute: Verify specific argument was used
    substitute.Received().GetUser(42);
    substitute.Received().GetUser(99);
    substitute.DidNotReceive().GetUser(1);
}
```
<!-- endSnippet -->

**KnockOff:**

<!-- snippet: nsub-migration-received-args-knockoff -->
```cs
[Fact]
public void ReceivedWithArgs_KnockOffApproach()
{
    var stub = new NSubUserRepoStub();
    var calledIds = new List<int>();

    // Capture arguments for later verification
    stub.GetUser.OnCall((id) =>
    {
        calledIds.Add(id);
        return null;
    });

    INSubUserRepo repository = stub;
    repository.GetUser(42);
    repository.GetUser(99);

    // KnockOff: Inspect captured arguments
    Assert.Contains(42, calledIds);
    Assert.Contains(99, calledIds);
    Assert.DoesNotContain(1, calledIds);

    // Or use LastCallArg for the most recent call
    Assert.Equal(99, stub.GetUser.LastCallArg);
}
```
<!-- endSnippet -->

**Trade-off:** NSubstitute's `.Received().GetUser(42)` is declarative and readable. KnockOff requires capturing arguments in the callback or using `LastCallArg` to inspect the most recent call. This is more manual but gives full access to call history.

---

## Step 10: Multiple Arguments

Replace multiple `Arg.*` matchers with named callback parameters.

**NSubstitute:**

<!-- snippet: nsub-migration-multiargs-nsub -->
```cs
[Fact]
public void MultipleArgs_NSubstituteApproach()
{
    var substitute = Substitute.For<INSubUserRepo>();

    // NSubstitute: Multiple Arg matchers
    substitute.FindUsers(Arg.Any<string>(), Arg.Is<int>(x => x > 0))
        .Returns(callInfo => new[]
        {
            new User { Name = callInfo.ArgAt<string>(0) }
        });

    INSubUserRepo repository = substitute;
    var users = repository.FindUsers("Alice", 10);

    Assert.Single(users);
    Assert.Equal("Alice", users.First().Name);
}
```
<!-- endSnippet -->

**KnockOff:**

<!-- snippet: nsub-migration-multiargs-knockoff -->
```cs
[Fact]
public void MultipleArgs_KnockOffApproach()
{
    var stub = new NSubUserRepoStub();

    // KnockOff: Named parameters directly in delegate
    stub.FindUsers.OnCall((name, limit) =>
    {
        if (limit <= 0) return Enumerable.Empty<User>();
        return new[] { new User { Name = name } };
    });

    INSubUserRepo repository = stub;
    var users = repository.FindUsers("Alice", 10);

    Assert.Single(users);
    Assert.Equal("Alice", users.First().Name);
}
```
<!-- endSnippet -->

**Trade-off:** NSubstitute's `Arg.Any<T>()` and `Arg.Is<T>()` per parameter are declarative but verbose. KnockOff provides typed, named parameters directly in the callback signature, which is cleaner and gives immediate access to all arguments.

---

## Step 11: Side Effects (When...Do)

Replace `.When().Do()` with logic in `OnCall`.

**NSubstitute:**

<!-- snippet: nsub-migration-whendo-nsub -->
```cs
[Fact]
public void WhenDo_NSubstituteApproach()
{
    var substitute = Substitute.For<INSubUserRepo>();
    var savedUsers = new List<User>();

    // NSubstitute: When...Do for void methods with side effects
    substitute.When(x => x.SaveUser(Arg.Any<User>()))
        .Do(callInfo => savedUsers.Add(callInfo.Arg<User>()));

    INSubUserRepo repository = substitute;
    repository.SaveUser(new User { Id = 1, Name = "Alice" });
    repository.SaveUser(new User { Id = 2, Name = "Bob" });

    Assert.Equal(2, savedUsers.Count);
    Assert.Equal("Alice", savedUsers[0].Name);
    Assert.Equal("Bob", savedUsers[1].Name);
}
```
<!-- endSnippet -->

**KnockOff:**

<!-- snippet: nsub-migration-whendo-knockoff -->
```cs
[Fact]
public void WhenDo_KnockOffApproach()
{
    var stub = new NSubUserRepoStub();
    var savedUsers = new List<User>();

    // KnockOff: OnCall handles side effects directly
    stub.SaveUser.OnCall((user) =>
    {
        savedUsers.Add(user);
    });

    INSubUserRepo repository = stub;
    repository.SaveUser(new User { Id = 1, Name = "Alice" });
    repository.SaveUser(new User { Id = 2, Name = "Bob" });

    Assert.Equal(2, savedUsers.Count);
    Assert.Equal("Alice", savedUsers[0].Name);
    Assert.Equal("Bob", savedUsers[1].Name);
}
```
<!-- endSnippet -->

**Trade-off:** Both approaches work. NSubstitute's `.When().Do()` is more explicit about intent (side effect only). KnockOff's unified `OnCall` is simpler but doesn't distinguish between "return value" and "side effect" configurations.

---

## Step 12: Returns with Side Effects

Replace `.Returns().AndDoes()` with combined logic in `OnCall`.

**NSubstitute:**

<!-- snippet: nsub-migration-returnsanddoes-nsub -->
```cs
[Fact]
public void ReturnsAndDoes_NSubstituteApproach()
{
    var substitute = Substitute.For<INSubUserRepo>();
    var accessLog = new List<int>();

    // NSubstitute: AndDoes for side effects with return value
    substitute.GetUser(Arg.Any<int>())
        .Returns(callInfo => new User { Id = callInfo.Arg<int>(), Name = "Test" })
        .AndDoes(callInfo => accessLog.Add(callInfo.Arg<int>()));

    INSubUserRepo repository = substitute;
    var user1 = repository.GetUser(1);
    var user2 = repository.GetUser(2);

    Assert.Equal(new[] { 1, 2 }, accessLog);
    Assert.Equal("Test", user1?.Name);
}
```
<!-- endSnippet -->

**KnockOff:**

<!-- snippet: nsub-migration-returnsanddoes-knockoff -->
```cs
[Fact]
public void ReturnsAndDoes_KnockOffApproach()
{
    var stub = new NSubUserRepoStub();
    var accessLog = new List<int>();

    // KnockOff: Side effects and return in same delegate
    stub.GetUser.OnCall((id) =>
    {
        accessLog.Add(id);
        return new User { Id = id, Name = "Test" };
    });

    INSubUserRepo repository = stub;
    var user1 = repository.GetUser(1);
    var user2 = repository.GetUser(2);

    Assert.Equal(new[] { 1, 2 }, accessLog);
    Assert.Equal("Test", user1?.Name);
}
```
<!-- endSnippet -->

**Trade-off:** KnockOff is actually simpler here. NSubstitute's chained `.Returns().AndDoes()` is more verbose than KnockOff's single delegate.

---

## Step 13: Argument Matchers (Callback Approach)

Replace `Arg.Is<T>()` with conditional logic in callbacks.

**NSubstitute:**

<!-- snippet: nsub-migration-argmatchers-nsub -->
```cs
[Fact]
public void ArgMatchers_NSubstituteApproach()
{
    var substitute = Substitute.For<INSubUserRepo>();

    // Arg.Is<T>() for conditional matching
    substitute.GetUser(Arg.Is<int>(id => id > 0))
        .Returns(callInfo => new User
        {
            Id = callInfo.Arg<int>(),
            Name = "Valid User"
        });

    INSubUserRepo repository = substitute;

    var validUser = repository.GetUser(1);
    var invalidUser = repository.GetUser(-1);

    Assert.NotNull(validUser);
    Assert.Null(invalidUser); // No setup matched
}
```
<!-- endSnippet -->

**KnockOff:**

<!-- snippet: nsub-migration-argmatchers-knockoff -->
```cs
[Fact]
public void ArgMatchers_KnockOffApproach()
{
    var stub = new NSubUserRepoStub();

    // KnockOff: Conditional logic in the callback
    stub.GetUser.OnCall((id) =>
        id > 0 ? new User { Id = id, Name = "Valid User" } : null);

    INSubUserRepo repository = stub;

    var validUser = repository.GetUser(1);
    var invalidUser = repository.GetUser(-1);

    Assert.NotNull(validUser);
    Assert.Null(invalidUser);
}
```
<!-- endSnippet -->

**Trade-off:** NSubstitute's `Arg.Is<T>()` is declarative and can be combined with multiple matchers per method. KnockOff's callback approach puts the logic inside the callback, which is less elegant but more flexible.

---

## Step 13b: Argument Matchers (Predicate Matching)

For permanent predicate matching that applies to multiple calls, both frameworks take different approaches.

**NSubstitute:**

<!-- snippet: nsub-migration-when-predicate-nsub -->
```cs
[Fact]
public void WhenPredicate_NSubstituteApproach()
{
    var substitute = Substitute.For<INSubUserRepo>();

    // Arg.Is<T>() with predicate per parameter
    substitute.GetUser(Arg.Is<int>(id => id > 0))
        .Returns(callInfo => new User
        {
            Id = callInfo.Arg<int>(),
            Name = "Valid User"
        });

    substitute.GetUser(Arg.Is<int>(id => id <= 0))
        .Returns((User?)null);

    INSubUserRepo repository = substitute;

    Assert.NotNull(repository.GetUser(1));
    Assert.NotNull(repository.GetUser(100));
    Assert.Null(repository.GetUser(0));
    Assert.Null(repository.GetUser(-5));
}
```
<!-- endSnippet -->

**KnockOff:**

<!-- snippet: nsub-migration-when-predicate-knockoff -->
```cs
[Fact]
public void WhenPredicate_KnockOffApproach()
{
    var stub = new NSubUserRepoStub();

    // For permanent predicate matching, use OnCall with conditionals
    // (When() is for sequential/consumable matching)
    stub.GetUser.OnCall((id) =>
        id > 0 ? new User { Id = id, Name = "Valid User" } : null);

    INSubUserRepo repository = stub;

    Assert.NotNull(repository.GetUser(1));
    Assert.NotNull(repository.GetUser(100));
    Assert.Null(repository.GetUser(0));
    Assert.Null(repository.GetUser(-5));
}
```
<!-- endSnippet -->

**Trade-off:** NSubstitute's `Arg.Is<T>()` matchers are permanent—they apply to all matching calls. KnockOff's `OnCall()` with conditionals achieves the same result. KnockOff's `When()` API is designed for sequential/consumable matching (first call matches X, second matches Y), which is a different use case.

---

## Step 13c: Exact Value Matching

Both frameworks support exact value matching.

**NSubstitute:**

<!-- snippet: nsub-migration-when-values-nsub -->
```cs
[Fact]
public void WhenValues_NSubstituteApproach()
{
    var substitute = Substitute.For<INSubUserRepo>();

    // Exact value matching
    substitute.GetUser(42).Returns(new User { Id = 42, Name = "Alice" });
    substitute.GetUser(99).Returns(new User { Id = 99, Name = "Bob" });

    INSubUserRepo repository = substitute;

    Assert.Equal("Alice", repository.GetUser(42)?.Name);
    Assert.Equal("Bob", repository.GetUser(99)?.Name);
    Assert.Null(repository.GetUser(1)); // No match
}
```
<!-- endSnippet -->

**KnockOff (When API):**

<!-- snippet: nsub-migration-when-values-knockoff -->
```cs
[Fact]
public void WhenValues_KnockOffApproach()
{
    var stub = new NSubUserRepoStub();

    // When() with exact values
    stub.GetUser.When(42).Returns(new User { Id = 42, Name = "Alice" });
    stub.GetUser.When(99).Returns(new User { Id = 99, Name = "Bob" });

    INSubUserRepo repository = stub;

    Assert.Equal("Alice", repository.GetUser(42)?.Name);
    Assert.Equal("Bob", repository.GetUser(99)?.Name);
    Assert.Null(repository.GetUser(1)); // No match
}
```
<!-- endSnippet -->

**Trade-off:** Both approaches are clean and declarative for exact value matching. KnockOff's `When(value).Returns(result)` reads similarly to NSubstitute's `Method(value).Returns(result)`.

---

## Step 14: Async Methods

Replace seamless async `.Returns()` with explicit `Task.FromResult()`.

**NSubstitute:**

<!-- snippet: nsub-migration-async-nsub -->
```cs
[Fact]
public async Task AsyncMethod_NSubstituteApproach()
{
    var substitute = Substitute.For<INSubUserRepo>();
    var testUser = new User { Id = 42, Name = "Alice" };

    // NSubstitute: Returns works seamlessly with Task
    substitute.GetUserAsync(Arg.Any<int>()).Returns(testUser);

    INSubUserRepo repository = substitute;
    var user = await repository.GetUserAsync(42);

    Assert.NotNull(user);
    Assert.Equal("Alice", user.Name);
}
```
<!-- endSnippet -->

**KnockOff:**

<!-- snippet: nsub-migration-async-knockoff -->
```cs
[Fact]
public async Task AsyncMethod_KnockOffApproach()
{
    var stub = new NSubUserRepoStub();
    var testUser = new User { Id = 42, Name = "Alice" };

    // KnockOff: Must wrap in Task.FromResult explicitly
    stub.GetUserAsync.OnCall((id) => Task.FromResult<User?>(testUser));

    INSubUserRepo repository = stub;
    var user = await repository.GetUserAsync(42);

    Assert.NotNull(user);
    Assert.Equal("Alice", user.Name);
}
```
<!-- endSnippet -->

**Trade-off:** NSubstitute wins here. Its automatic `Task` wrapping is convenient and reduces ceremony. KnockOff's explicit `Task.FromResult()` is verbose but makes the async nature explicit.

---

## Step 15: Clearing Call History

Replace `.ClearReceivedCalls()` with `.Reset()`.

**NSubstitute:**

<!-- snippet: nsub-migration-clear-nsub -->
```cs
[Fact]
public void ClearReceived_NSubstituteApproach()
{
    var substitute = Substitute.For<INSubUserRepo>();

    INSubUserRepo repository = substitute;
    repository.GetUser(1);
    repository.GetUser(2);

    // NSubstitute: Clear call history
    substitute.ClearReceivedCalls();

    substitute.DidNotReceive().GetUser(Arg.Any<int>());
}
```
<!-- endSnippet -->

**KnockOff:**

<!-- snippet: nsub-migration-clear-knockoff -->
```cs
[Fact]
public void ClearReceived_KnockOffApproach()
{
    var stub = new NSubUserRepoStub();
    var tracking = stub.GetUser.OnCall((id) => null);

    INSubUserRepo repository = stub;
    repository.GetUser(1);
    repository.GetUser(2);

    // Verify calls were made
    tracking.Verify(Times.Exactly(2));

    // KnockOff: Reset clears call tracking
    stub.GetUser.Reset();

    // Now verify no calls
    tracking.Verify(Times.Never);
}
```
<!-- endSnippet -->

**Trade-off:** Both work. NSubstitute clears all calls on the substitute at once. KnockOff resets per-interceptor, giving more granular control.

---

## Step 16: Throwing Exceptions

Replace exception `.Returns()` with throw in callback.

**NSubstitute:**

<!-- snippet: nsub-migration-throws-nsub -->
```cs
[Fact]
public void Throws_NSubstituteApproach()
{
    var substitute = Substitute.For<INSubUserRepo>();

    // NSubstitute: Throws extension
    substitute.GetUser(Arg.Any<int>())
        .Returns<User?>(_ => throw new InvalidOperationException("Database offline"));

    INSubUserRepo repository = substitute;

    Assert.Throws<InvalidOperationException>(() => repository.GetUser(1));
}
```
<!-- endSnippet -->

**KnockOff:**

<!-- snippet: nsub-migration-throws-knockoff -->
```cs
[Fact]
public void Throws_KnockOffApproach()
{
    var stub = new NSubUserRepoStub();

    // KnockOff: Throw directly in callback
    stub.GetUser.OnCall((id) =>
        throw new InvalidOperationException("Database offline"));

    INSubUserRepo repository = stub;

    Assert.Throws<InvalidOperationException>(() => repository.GetUser(1));
}
```
<!-- endSnippet -->

**Trade-off:** Equivalent. Both require a lambda that throws. NSubstitute has a `.Throws()` extension for convenience, but the lambda approach works in both frameworks.

---

## Complete Before/After Example

This example shows a full test class migrated from NSubstitute to KnockOff.

### Before: NSubstitute

<!-- snippet: nsub-migration-complete-nsub -->
```cs
private readonly INSubUserRepo _substitute;
private readonly UserServiceNSub _service;

public CompleteNSubstituteTests()
{
    _substitute = Substitute.For<INSubUserRepo>();
    _service = new UserServiceNSub(_substitute);
}

[Fact]
public async Task GetUser_ReturnsUser()
{
    var user = new User { Id = 1, Name = "Alice" };
    _substitute.GetUserAsync(1).Returns(user);

    var result = await _service.GetUserAsync(1);

    Assert.Equal("Alice", result?.Name);
    await _substitute.Received(1).GetUserAsync(1);
}

[Fact]
public void SaveUser_CallsRepository()
{
    var user = new User { Id = 1, Name = "Bob" };

    _service.SaveUser(user);

    _substitute.Received().SaveUser(Arg.Is<User>(u => u.Name == "Bob"));
}

[Fact]
public void TryDeleteUser_WhenUserExists_DeletesAndReturnsTrue()
{
    var user = new User { Id = 1, Name = "Charlie" };
    _substitute.GetUser(1).Returns(user);

    var result = _service.TryDeleteUser(1);

    Assert.True(result);
    _substitute.Received().DeleteUser(1);
}

[Fact]
public void TryDeleteUser_WhenUserNotFound_ReturnsFalse()
{
    _substitute.GetUser(1).Returns((User?)null);

    var result = _service.TryDeleteUser(1);

    Assert.False(result);
    _substitute.DidNotReceive().DeleteUser(Arg.Any<int>());
}
```
<!-- endSnippet -->

### After: KnockOff

<!-- snippet: nsub-migration-complete-knockoff -->
```cs
private readonly NSubUserRepoStub _stub;
private readonly UserServiceNSub _service;

public CompleteKnockOffNSubTests()
{
    _stub = new NSubUserRepoStub();
    _service = new UserServiceNSub(_stub);
}

[Fact]
public async Task GetUser_ReturnsUser()
{
    var user = new User { Id = 1, Name = "Alice" };
    _stub.GetUserAsync.OnCall((id) => Task.FromResult<User?>(user)).Verifiable();

    var result = await _service.GetUserAsync(1);

    Assert.Equal("Alice", result?.Name);
    _stub.Verify();
}

[Fact]
public void SaveUser_CallsRepository()
{
    User? savedUser = null;
    _stub.SaveUser.OnCall((user) =>
    {
        savedUser = user;
    }).Verifiable();

    _service.SaveUser(new User { Id = 1, Name = "Bob" });

    _stub.Verify();
    Assert.Equal("Bob", savedUser?.Name);
}

[Fact]
public void TryDeleteUser_WhenUserExists_DeletesAndReturnsTrue()
{
    var user = new User { Id = 1, Name = "Charlie" };
    _stub.GetUser.OnCall((id) => user);
    _stub.DeleteUser.OnCall((id) => { }).Verifiable();

    var result = _service.TryDeleteUser(1);

    Assert.True(result);
    _stub.Verify();
}

[Fact]
public void TryDeleteUser_WhenUserNotFound_ReturnsFalse()
{
    _stub.GetUser.OnCall((id) => null);
    var deleteTracking = _stub.DeleteUser.OnCall((id) => { });

    var result = _service.TryDeleteUser(1);

    Assert.False(result);
    deleteTracking.Verify(Times.Never);
}
```
<!-- endSnippet -->

**What changed:**
- Added stub class declaration with `[KnockOff]` attribute
- Replaced `Substitute.For<T>()` with stub instance
- Replaced `.Returns()` with `OnCall` delegates
- Replaced `.Received()` with `.Verifiable()` + `.Verify()`
- Replaced `.DidNotReceive()` with `tracking.Verify(Times.Never)`
- Added explicit `Task.FromResult()` for async methods

**What stayed the same:**
- Test logic and assertions
- Test structure and organization
- Coverage and test goals

---

## Common Gotchas

### Missing `.Verifiable()` for Verification

**Problem:** You expect `Verify()` to check a method, but forgot to mark it.

```csharp
// Wrong: Method not marked, Verify() won't check it
stub.SaveUser.OnCall((user) => { });
stub.Verify(); // Passes even if SaveUser wasn't called!

// Correct: Mark with Verifiable()
stub.SaveUser.OnCall((user) => { }).Verifiable();
stub.Verify(); // Now fails if SaveUser wasn't called
```

### Forgetting `Task.FromResult` for Async Methods

**Problem:** NSubstitute auto-wraps; KnockOff doesn't.

```csharp
// Wrong: Compiler error - return type mismatch
stub.GetUserAsync.OnCall((id) => user);

// Correct: Explicit Task wrapping
stub.GetUserAsync.OnCall((id) => Task.FromResult<User?>(user));
```

### Expecting `.Received()` Syntax

**Problem:** Trying to use NSubstitute verification patterns.

```csharp
// Wrong: No Received() method in KnockOff
stub.Received().SaveUser(Arg.Any<User>());

// Correct: Use Verify with Times
stub.SaveUser.Verify(Times.AtLeastOnce);

// Or use batch verification
stub.SaveUser.OnCall((user) => { }).Verifiable();
// ... call method ...
stub.Verify();
```

### Missing the `partial` Keyword

**Problem:** Stub class isn't marked `partial`, causing duplicate member errors.

```csharp
// Wrong
[KnockOff]
class UserRepoStub : IUserRepo { }

// Correct
[KnockOff]
partial class UserRepoStub : IUserRepo { }
```

### Wrong `OnCall` Signature

**Problem:** Callback signature doesn't match the method parameters.

```csharp
// Wrong: GetUser(int id) expects (int) callback
stub.GetUser.OnCall(() => user);

// Correct
stub.GetUser.OnCall((id) => user);
```

---

## Features Not Supported in KnockOff

### Recursive Mocks

NSubstitute automatically creates substitutes for return types:

```csharp
// NSubstitute: Auto-creates nested substitute
var sub = Substitute.For<IOrderService>();
sub.GetOrder(1).Customer.Name.Returns("Alice");
```

KnockOff does not support this. You must create separate stubs:

```csharp
// KnockOff: Create each stub explicitly
var customerStub = new CustomerStub();
customerStub.Name.Value = "Alice";

var orderStub = new OrderStub();
orderStub.Customer.Value = customerStub;
```

### `Arg.Do<T>()` for Argument Capture

NSubstitute's `Arg.Do<T>()` captures arguments during setup:

```csharp
// NSubstitute
User? captured = null;
sub.SaveUser(Arg.Do<User>(u => captured = u));
```

KnockOff captures in the callback:

```csharp
// KnockOff
User? captured = null;
stub.SaveUser.OnCall((user) => { captured = user; });
```

### Multiple Return Values (Sequences)

NSubstitute can return different values for sequential calls:

```csharp
// NSubstitute
sub.GetUser(1).Returns(user1, user2, user3);
```

KnockOff uses `ThenCall()` chaining:

```csharp
// KnockOff - ThenCall sequence
stub.GetUser
    .OnCall((id) => user1)
    .ThenCall((id) => user2)
    .ThenCall((id) => user3);
```

**Behavior match:** KnockOff now matches NSubstitute's sequence exhaustion behavior - both repeat the last value after exhaustion.

**KnockOff extension:** Use `ThenDefault()` to explicitly return `default(T)` after exhaustion instead of repeating (NSubstitute has no equivalent). In strict mode, sequences throw `StubException.SequenceExhausted` on exhaustion.

---

## When to Use NSubstitute Instead

Honestly, NSubstitute remains the better choice when:

1. **You need recursive mocks** - NSubstitute's auto-substitution is powerful
2. **You value API elegance** - `.Returns()` and `.Received()` are genuinely better
3. **Tests are isolated** - If you don't share stubs, KnockOff's main benefit disappears
4. **Your team knows NSubstitute** - Migration has a learning curve

KnockOff earns its place when:

1. **You share stubs across many test files** - Define once, configure per-test
2. **Compile-time safety matters** - Catch configuration errors at build time
3. **You want inspectable generated code** - See exactly what your stubs do
4. **Performance is critical** - No runtime proxy generation overhead

---

## Next Steps

- **[Getting Started Guide](../getting-started.md)** - Learn KnockOff patterns from scratch
- **[Stub Patterns](../guides/stub-patterns.md)** - Stand-alone, inline interface, and inline class patterns
- **[Interceptor API Reference](../reference/interceptor-api.md)** - Complete reference for `OnCall`, `OnGet`, `OnSet`
- **[Verification Guide](../guides/verification.md)** - Advanced call tracking and verification patterns

---

**Need help?** Open an issue on [GitHub](https://github.com/neatoodotnet/KnockOff/issues) or check existing discussions.

---

**UPDATED:** 2026-02-01
