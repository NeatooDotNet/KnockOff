---
name: knockoff
description: This skill should be used when the user asks about "KnockOff stubs", "create a stub", "mock with KnockOff", "[KnockOff] attribute", "[KnockOff<T>] attribute", "Return", "Call", "Get", "Set", "setup stub behavior", "Verify calls", "Verifiable", "VerifyAll", "track method calls", "stub patterns", "Stand-Alone pattern", "Inline Interface", "Inline Class", "Inline Delegate", "stub a delegate", "migrate from Moq", "KnockOff async", "interceptor API", "Strict mode", "Strict()", "assembly-wide strict", "[assembly: KnockOffStrict]", "ThenReturn", "ThenCall", "ThenGet", "ThenSet", ".Of<T>()", "generic method interceptor", "Source() delegation", "When()", "argument matching", or needs guidance on creating, configuring, or verifying KnockOff test stubs. IMPORTANT: When writing tests that need stubs, this skill MUST be consulted to check for existing stubs before creating new inline stubs - prefer standalone stubs when the same type is stubbed in multiple test classes.
version: 2.2.0
---

# KnockOff Usage Guide

KnockOff is a Roslyn Source Generator that creates test stubs at compile time. Stubs are reusable, have zero reflection overhead, and provide compile-time safety.

## CRITICAL BEHAVIORAL GOTCHAS

**Read this section first to avoid common mistakes.**

### 1. Sequences REPEAT Last Value After Exhaustion

Sequences repeat the last value after exhaustion (matching NSubstitute):

<!-- snippet: skill-gotcha-sequence-exhaustion -->
```cs
stub.Add.Return(1, 999);
calc.Add(0, 0); // Returns 1
calc.Add(0, 0); // Returns 999
calc.Add(0, 0); // Returns 999 (repeats last value!)

// Use ThenDefault() to return default(T) instead of repeating
stub.Add.Return(1, 999).ThenDefault();
calc.Add(0, 0); // Returns 1
calc.Add(0, 0); // Returns 999
calc.Add(0, 0); // Returns 0 (default - ThenDefault() terminates with default)
```
<!-- endSnippet -->

### 2. Events Use Raise() Method

Events are raised via the `.Raise()` method on the event interceptor:

<!-- snippet: skill-gotcha-event-raise -->
```cs
// Events use .Raise() method:
stub.Started.Raise(stub, EventArgs.Empty);
```
<!-- endSnippet -->

### 3. Event Interceptors Use Direct Event Name

<!-- snippet: skill-gotcha-event-naming -->
```cs
// Event interceptors use the event name directly:
stub.Started.VerifyAdd(Called.Never);
stub.DataReceived.VerifyAdd(Called.Never);
```
<!-- endSnippet -->

### 4. Class Stubs Call Base by Default (Virtual Methods)

Class stubs (Patterns 3, 4, 6, 9) automatically call the base class implementation for unconfigured virtual methods. This is equivalent to Moq's `.CallBase = true`, but it is the default behavior -- no opt-in required. Abstract methods return `default(T)` when unconfigured (there is no base to call).

- **Virtual method, unconfigured**: calls base class implementation
- **Virtual method, configured** (Return/Call/When): interceptor handles it, base is NOT called
- **Abstract method, unconfigured**: returns `default(T)` (or throws in strict mode)
- **Abstract method, configured**: interceptor handles it

### 5. Class Stubs Use .Object Property

Inline class stubs don't inherit from the base class:

<!-- snippet: skill-gotcha-class-object -->
```cs
// WRONG: ServiceBase service = stub;
// RIGHT:
var stub = new Stubs.ServiceBase();
ServiceBase service = stub.Object;
service.Initialize();
```
<!-- endSnippet -->

### 6. Closed Generic Stubs Use Simple Names

<!-- snippet: skill-gotcha-closed-generic -->
```cs
// For [KnockOff<IRepository<User>>]:
var stub = new Stubs.IRepository();  // NOT Stubs.IRepository<User>
```
<!-- endSnippet -->

### 7. Called.Between() Does NOT Exist

<!-- snippet: skill-gotcha-times-between -->
```cs
// WRONG: Called.Between(1, 5)
// RIGHT: Use separate constraints
stub.Save.Verify(Called.AtLeast(1));
stub.Save.Verify(Called.AtMost(5));
```
<!-- endSnippet -->

### 8. Configuration Methods — Last One Wins

All configuration methods use direct replacement. Calling any configuration method replaces the previous configuration of the same kind:

- `Return(value)` and `Return(callback)` replace each other
- Multiple `Call(callback)` calls — last wins
- Multiple `Get(value)` or `Get(callback)` calls — last wins
- Multiple `Set(callback)` calls — last wins
- Multiple `When()` calls — last wins (replaces previous When chain)

Within a When chain, `.ThenWhen()` accumulates matchers. But calling `.When()` again as a new entry point replaces the entire chain.

**Known bug:** `.When()` currently accumulates like `.ThenWhen()` instead of replacing. See `docs/todos/when-entry-point-should-clear-chain.md`.

<!-- snippet: skill-gotcha-returns-vs-oncall -->
```cs
stub.GetValue.Return("fixed");           // Sets constant value
stub.GetValue.Return((id) => $"val-{id}"); // REPLACES constant, now dynamic
```
<!-- endSnippet -->

### 9. Set Does NOT Auto-Update Getter

<!-- snippet: skill-gotcha-onset-no-auto-update -->
```cs
stub.Name.Set((v) => { /* tracks value */ });
service.Name = "test";
// Getter still returns default! Set doesn't update Get
// To link them: stub.Name.Set((v) => stub.Name.Get(v));
```
<!-- endSnippet -->

### 10. Reset() Clears Tracking BUT Preserves Some State

| Interceptor | Reset Clears | Reset Preserves |
|-------------|--------------|-----------------|
| Method | Counts, LastArg/LastArgs, sequence index, When chain position, source delegation | **Return/Call callbacks**, sequence structure, verifiable flag |
| User Method | Counts, LastArg | **Return/Call configuration**, verifiable flag |
| Property | Get/set counts, LastSetValue, sequence index, source delegation | **Get/Set callbacks**, verifiable flag |
| User Property | Get/set counts, LastSetValue | **Get/Set configuration**, verifiable flag |
| Indexer | Get/set counts, LastGetKey, LastSetEntry | **Backing dictionary**, Get/Set callbacks |
| Delegate | Counts, LastArg/LastArgs, sequence index, When chain position | **Return/Call callbacks**, sequence structure, verifiable flag |
| Event | Tracking counts | **Active subscribers**, verifiable flag |

**Note:** User method and user property interceptors (e.g., `GetById` when you have a `GetById_` override, or `Count` when you have a `Count_` override) preserve Return/Call/Get/Set configuration across Reset(). This matches regular interceptor semantics where the configuration represents "what the stub does" rather than tracking state.

---

## PROACTIVE: Detect Duplicate Inline Stubs

**When writing tests that need stubs, ALWAYS check for existing stubs first.**

Before creating an inline stub with `[KnockOff<T>]`:

1. **Search for existing standalone stubs** of the target type (e.g., `class *Stub : ITargetType`)
2. **Search for existing inline stubs** using `[KnockOff<TargetType>]` in other test classes

**If the same interface/class is already stubbed inline in 1+ other test classes:**

Use AskUserQuestion to recommend converting to standalone:

> "I noticed `IUserRepository` is already stubbed inline in `UserServiceTests.cs`. Creating another inline stub would duplicate the definition.
>
> **Recommendation:** Create a standalone `UserRepositoryStub` that both test classes can share."

**Options to present:**
1. **Create Stand-Alone stub (Recommended)** - Eliminates duplication, enables reuse
2. **Add inline stub anyway** - Creates duplication (not recommended)
3. **Reuse existing** - Move tests to the class that already has the inline stub

**Why this matters:** Inline stubs are convenient for one-off usage, but when the same type is stubbed in multiple test classes, a standalone stub is cleaner and more maintainable.

---

## Pattern Selection

| Need | Pattern | Instantiation |
|------|---------|---------------|
| Reusable stub across files | Standalone | `new MyStub()` |
| Custom methods on stub | Standalone | `new MyStub()` |
| Reusable generic stub with type parameters | Generic Standalone | `new MyStub<T>()` |
| Quick test-local stub | Inline Interface | `new Stubs.IService()` |
| Stub a class (virtual/abstract) | Inline Class | `new Stubs.MyClass()` then `.Object` |
| Stub a delegate | Inline Delegate | `new Stubs.MyDelegate()` |
| Test-local stub for generic interface | Open Generic | `new Stubs.IFoo<T>()` |

### Standalone Pattern

<!-- snippet: skill-standalone-pattern -->
```cs
[KnockOff]
public partial class SkillUserRepoStub : ISkillUserRepo { }
```
<!-- endSnippet -->

Usage:

<!-- snippet: skill-standalone-usage -->
```cs
[Fact]
public void StandaloneStub_ConfigureAndVerify()
{
    var stub = new SkillUserRepoStub();
    stub.GetById.Return((id) => new User { Id = id }).Verifiable();
    stub.Save.Call((user) => { }).Verifiable();
    ISkillUserRepo repo = stub;

    var user = repo.GetById(42);
    repo.Save(user!);

    stub.Verify();
}
```
<!-- endSnippet -->

**User Methods & Properties:** Stand-Alone stubs can define protected methods and properties that provide default behavior. See the User Methods and User Properties sections below.

### Inline Interface Pattern

<!-- snippet: skill-inline-interface-pattern -->
```cs
[KnockOff<ISkillEmailService>]
public partial class SkillEmailTests
{
    [Fact]
    public void Test()
    {
        var stub = new Stubs.ISkillEmailService();
        stub.Send.Return((to, subj) => true).Verifiable();
        ISkillEmailService email = stub;
    }
}
```
<!-- endSnippet -->

### Inline Class Pattern

Class stubs call the base class implementation by default for unconfigured virtual methods. Only configure what you need to override. Abstract methods return `default(T)` when unconfigured.

<!-- snippet: skill-inline-class-pattern -->
```cs
[KnockOff<SkillDataServiceBase>]
public partial class SkillDataTests
{
    [Fact]
    public void Test()
    {
        var stub = new Stubs.SkillDataServiceBase();
        stub.GetData.Return((id) => "test").Verifiable();
        SkillDataServiceBase service = stub.Object;  // Use .Object!
    }
}
```
<!-- endSnippet -->

### Inline Delegate Pattern

<!-- snippet: skill-inline-delegate-pattern -->
```cs
[KnockOff<SkillValidationRule>]  // delegate bool SkillValidationRule(string value);
public partial class SkillValidationTests
{
    [Fact]
    public void Test()
    {
        var stub = new Stubs.SkillValidationRule();
        stub.Interceptor.Return((val) => val != "invalid");
        SkillValidationRule rule = stub;  // Implicit conversion
    }
}
```
<!-- endSnippet -->

---

## Method Configuration

### Return() - Fixed Values

<!-- snippet: skill-method-returns -->
```cs
stub.GetUser.Return(new User { Id = 1, Name = "Alice" });
```
<!-- endSnippet -->

### Return(callback) / Call(callback) - Dynamic Callbacks

<!-- snippet: skill-method-oncall -->
```cs
// With arguments
stub.GetUser.Return((id) => new User { Id = id, Name = $"User{id}" });

// Void methods
stub.Save.Call((user) => { /* side effects */ });

// Async methods - auto-wrapped, no Task.FromResult needed
stub.GetUserAsync.Return((id) => new User { Id = id });  // Returns Task<User>
stub.SaveAsync.Return((user) => { });  // Returns Task.CompletedTask
```
<!-- endSnippet -->

### Sequences (NSubstitute-style)

<!-- snippet: skill-method-sequences -->
```cs
// Concise value sequences (preferred)
stub.GetNext.Return(1, 2, 3);
// After third call, repeats 3 (NSubstitute-like behavior)

// Mix callbacks with value sequences
stub.Add.Return((a, b) => a + b).ThenReturn(100, 200);
// First: computed, then 100, 200, 200...

// Use ThenDefault() to return default(T) instead of repeating:
stub.GetNext.Return(1, 2).ThenDefault();
```
<!-- endSnippet -->

### When() - Argument Matching

<!-- snippet: skill-method-when -->
```cs
// Value matching
stub.GetUser.When(42).Return(adminUser);
stub.GetUser.When(1).Return(regularUser);

// Predicate matching
stub.GetUser.When(id => id < 0).Return(null);

// Chaining
stub.GetUser
    .When(42).Return(adminUser)
    .ThenWhen(id => id > 100).Return(premiumUser)
    .ThenWhen(id => id > 0).Return(regularUser);

// Void methods use Call instead of Return
stub.Log.When("error").Call((msg) => { /* handle */ });
```
<!-- endSnippet -->

---

## Property Configuration

<!-- snippet: skill-property-config -->
```cs
// Static value
stub.Name.Get("TestName");

// Dynamic callback
stub.Timestamp.Get(() => DateTime.UtcNow);

// Setter interception
stub.Name.Set((value) => capturedValues.Add(value));

// Sequences
stub.Counter.Get(() => 1).ThenGet(() => 2).ThenGet(() => 3);
```
<!-- endSnippet -->

---

## Indexer Configuration

<!-- snippet: skill-indexer-config -->
```cs
// Use Backing dictionary for simple cases
stub.Indexer.Backing["key1"] = "value1";
stub.Indexer.Backing["key2"] = "value2";

// Or use callbacks
stub.Indexer.Get((key) => $"computed-{key}");
stub.Indexer.Set((key, value) => { /* handle */ });

// Note: Get/Set override Backing - they don't work together
```
<!-- endSnippet -->

---

## Event Configuration

<!-- snippet: skill-event-config -->
```cs
// Events use Raise() method
stub.DataReceived.Raise(stub, new DataEventArgs("test-data"));

// Verify subscriptions
stub.DataReceived.VerifyAdd(Called.Once);
stub.DataReceived.VerifyRemove(Called.Never);
```
<!-- endSnippet -->

---

## Generic Methods

<!-- snippet: skill-generic-methods -->
```cs
// Use .Of<T>() for type-specific configuration
stub.GetById.Of<User>().Return((id) => new User { Id = id });
stub.GetById.Of<Product>().Return((id) => new Product { Id = id });

// Verify by type
stub.GetById.Of<User>().Verify(Called.Never);
stub.GetById.Of<Product>().Verify(Called.Never);
```
<!-- endSnippet -->

---

## Delegate Configuration

Delegates use `stub.Interceptor` instead of named member properties. All method interceptor features are available.

<!-- snippet: skill-delegate-config -->
```cs
var stub = new Stubs.SkillArithmeticOp();

// Returns (value or callback)
stub.Interceptor.Return(42);
stub.Interceptor.Return((a, b) => a + b);

// Sequences
stub.Interceptor.Return(10, 20, 30);

// When chains
stub.Interceptor.When(1, 2).Return(100)
    .ThenWhen(3, 4).Return(200);

// Async auto-wrapping (for delegates returning Task<T>)
// stub.Interceptor.Return(42);              // auto-wraps in Task.FromResult
// stub.Interceptor.Return((int x) => x * 2); // simplified, auto-wrapped

// Verification (fresh stub for clean tracking)
var verifyStub = new Stubs.SkillArithmeticOp();
verifyStub.Interceptor.Return((a, b) => a + b);
SkillArithmeticOp op = verifyStub;
op(1, 2);
verifyStub.Interceptor.Verify(Called.Once);
Assert.Equal((1, 2), verifyStub.Interceptor.LastArgs);

// Strict mode
stub.Strict = true;

// Implicit conversion to delegate type
SkillArithmeticOp opRef = stub;
```
<!-- endSnippet -->

---

## Verification

### Individual Verification

<!-- snippet: skill-verify-individual -->
```cs
var tracking = stub.Save.Call((user) => { });
// ... exercise stub ...
```
<!-- endSnippet -->

### Batch Verification with Verifiable()

<!-- snippet: skill-verify-batch -->
```cs
stub.GetUser.Return((id) => new User { Id = id }).Verifiable();
stub.Save.Call((u) => { }).Verifiable(Called.Once);
// ... exercise stub ...
```
<!-- endSnippet -->

### Verify() vs VerifyAll()

- `stub.Verify()` - Only members marked with `.Verifiable()`
- `stub.VerifyAll()` - ALL configured members (Return, Call, Get, etc.)

### Called Constraints

| Constraint | Description |
|------------|-------------|
| `Called.Never` | Must not be called |
| `Called.Once` | Exactly 1 call |
| `Called.AtLeastOnce` | 1 or more calls |
| `Called.Exactly(n)` | Exactly n calls |
| `Called.AtLeast(n)` | n or more calls |
| `Called.AtMost(n)` | n or fewer calls |

---

## Argument Capture

<!-- snippet: skill-arg-capture -->
```cs
// Single parameter - LastArg
var getTracking = stub.GetUser.Return((id) => new User { Id = id });
service.GetUser(42);
Assert.Equal(42, getTracking.LastArg);

// Multiple parameters - LastArgs tuple
var updateTracking = stub.Update.Call((id, name) => { });
service.Update(1, "Alice");
var (id, name) = updateTracking.LastArgs;
```
<!-- endSnippet -->

---

## Strict Mode

Throws `StubException` for unconfigured member access:

<!-- snippet: skill-strict-mode -->
```cs
// Per-stub
// [KnockOff(Strict = true)]
// public partial class StrictStub : IService { }

// Or at runtime
var stub = new SvcStub();
stub.Strict();

// Assembly-wide default
// [assembly: KnockOffStrict]
```
<!-- endSnippet -->

---

## User Methods (Stand-Alone Only)

User methods let you define default stub behavior at compile time. The user method is the fallback when no `Return`/`Call` callback is configured.

### Defining User Methods

Override virtual methods with underscore suffix - the compiler enforces signature correctness:

<!-- snippet: skill-user-method-define -->
```cs
[KnockOff]
public partial class SkUserMethodRepoStub : IUserRepo { }

public partial class SkUserMethodRepoStub
{
    // Override virtual method with underscore suffix - compiler enforces signature!
    protected override User? GetById_(int id) => new User { Id = id, Name = "Default" };
}
```
<!-- endSnippet -->

The interceptor uses a clean name (e.g., `GetById`, not `GetById2`) regardless of whether you override the method.

### Return(callback) Supersedes User Method

Use `Return(callback)` to override the user method for specific tests:

<!-- snippet: skill-user-method-oncall -->
```cs
var stub = new SkUserMethodRepoStub();
IUserRepo repo = stub;

// Without Returns: user method is called
var user1 = repo.GetById(1);  // Returns User { Id = 1, Name = "Default" }

// With Returns: callback supersedes user method (clean interceptor name)
stub.GetById.Return(id => new User { Id = id, Name = "Override" });
var user2 = repo.GetById(2);  // Returns User { Id = 2, Name = "Override" }
```
<!-- endSnippet -->

### Return for Constant Values

Use `Return()` for constant return values:

<!-- snippet: skill-user-method-returns -->
```cs
stub.GetById.Return(new User { Id = 99, Name = "Fixed" });
```
<!-- endSnippet -->

For async methods (`Task<T>`, `ValueTask<T>`), `Return()` auto-wraps the value:

<!-- snippet: skill-user-method-async-returns -->
```cs
// Returns auto-wraps in Task.FromResult
stub.GetUserAsync.Return(new User { Id = 1 });
```
<!-- endSnippet -->

### Tracking Works with Return

User method interceptors provide full tracking even when using `Return`:

<!-- snippet: skill-user-method-tracking -->
```cs
stub.GetById.Return(id => new User { Id = id });
repo.GetById(42);

stub.GetById.Verify(Called.Once);
Assert.Equal(42, stub.GetById.LastArg);
```
<!-- endSnippet -->

### Reset Preserves Return Configuration

`Reset()` clears tracking state but preserves the Return configuration:

<!-- snippet: skill-user-method-reset -->
```cs
stub.GetById.Return(id => new User { Id = id });
repo.GetById(1);
stub.GetById.Verify(Called.Once);

stub.GetById.Reset();
stub.GetById.Verify(Called.Never);  // Tracking cleared

repo.GetById(2);  // Still uses Returns callback
```
<!-- endSnippet -->

---

## User Properties (Stand-Alone Only)

User properties let you define default property behavior at compile time. The user property is the fallback when no `Get`/`Set` is configured.

### Defining User Properties

Override virtual properties with underscore suffix - the compiler enforces signature correctness:

<!-- snippet: skill-user-property-define -->
```cs
[KnockOff]
public partial class SkUserPropServiceStub : IUserService { }

public partial class SkUserPropServiceStub
{
    private int _count;

    // Override virtual property with underscore suffix - compiler enforces signature!
    protected override int Count_ => _count;

    public void SetCount(int value) => _count = value;
}
```
<!-- endSnippet -->

The interceptor uses a clean name (e.g., `Count`, not `Count2`) regardless of whether you override the property.

### Get/Set Supersede User Property

Use `Get()` or `Set()` to override the user property for specific tests:

<!-- snippet: skill-user-property-onget -->
```cs
var stub = new SkUserPropServiceStub();
stub.SetCount(42);
IUserService service = stub;

// Without Get: user property is called
var count1 = service.Count;  // Returns 42 (from Count_ override)

// With Get: Get supersedes user property (clean interceptor name)
stub.Count.Get(999);
var count2 = service.Count;  // Returns 999 (Get wins)
```
<!-- endSnippet -->

### Tracking Works with User Properties

User property interceptors provide full tracking even when using the user override:

<!-- snippet: skill-user-property-tracking -->
```cs
_ = service.Count;
_ = service.Count;

stub.Count.VerifyGet(Called.Exactly(2));
```
<!-- endSnippet -->

### Reset Preserves Get/Set Configuration

`Reset()` clears tracking state but preserves the Get/Set configuration:

<!-- snippet: skill-user-property-reset -->
```cs
stub.Count.Get(100);
_ = service.Count;
stub.Count.VerifyGet(Called.Once);

stub.Count.Reset();
stub.Count.VerifyGet(Called.Never);  // Tracking cleared

_ = service.Count;  // Still uses Get (returns 100)
```
<!-- endSnippet -->

---

## Source Delegation

`stub.Source(realImplementation)` delegates unconfigured calls to a real implementation. Configured members (Return, Call, When) still take priority -- Source is only consulted when nothing else handles the call.

<!-- snippet: skill-source-delegation -->
```cs
var stub = new SkSourceDelegationStub();
stub.Source(realImplementation);

// Configured members override source
stub.GetById.Return((id) => testUser);  // This wins over source

// Reset clears tracking (counts, args, sequence position) and source delegation
// but preserves callbacks (Return, Returns, Get, Set)
// stub.GetById.Reset();
```
<!-- endSnippet -->

**Availability:** Source() is available for **interface stubs only** (Standalone and Inline Interface patterns). Class stubs do not need Source() because they already call the base class implementation by default for unconfigured virtual methods (see Gotcha #4).

### Priority Order

KnockOff evaluates member calls in this order:

1. **When chains** -- `stub.Method.When(...).Return(...)`
2. **Return / Call** -- `stub.Method.Return(...)` or `stub.Method.Call(...)`
3. **User methods** -- `protected override` with `_` suffix (Standalone only)
4. **Source delegation** -- `stub.Source(realImplementation)`
5. **Smart default** -- KnockOff's built-in default value

The first match wins. This makes Source ideal as a baseline: set it once, then selectively override specific members at higher priority levels.

### Interface Hierarchy

When your stub implements an interface that extends other interfaces, KnockOff generates one `Source()` overload per level. Each overload sets `_source` on matching interceptors and clears it on non-matching ones. This means C# overload resolution does the right thing automatically -- pass whatever you have (even a `List<T>` for an `ICustomList<T> : IList<T>` stub) and only the matching members get delegated.

### Clearing Source

Remove source delegation by passing null: `stub.Source(null)`. After clearing, unconfigured methods return smart defaults (or throw in strict mode).

### Reset and Source

`Reset()` on an individual interceptor clears its `_source` reference along with tracking state. If you reset a member and still want delegation, call `stub.Source(realImplementation)` again after the reset.

---

## Moq Migration Quick Reference

| Moq | KnockOff |
|-----|----------|
| `new Mock<IFoo>()` | `new FooStub()` or `new Stubs.IFoo()` |
| `mock.Object` | `stub` (interface) or `stub.Object` (class) |
| `.Setup(x => x.Method()).Returns(val)` | `stub.Method.Return(val)` |
| `.Setup(x => x.Method(arg)).Returns(val)` | `stub.Method.When(arg).Return(val)` |
| `.Setup(x => x.Prop).Returns(val)` | `stub.Prop.Get(val)` |
| `.ReturnsAsync(val)` | `stub.Method.Return(val)` (auto-wraps) |
| `.Callback(action)` | Logic inside `Return`/`Call` callback |
| `mock.CallBase = true` | Default for class stubs (just don't configure the member) |
| `.Verify(x => x.Method(), Times.Once)` | `tracking.Verify(Called.Once)` |
| `.Verifiable()` + `mock.Verify()` | `.Verifiable()` + `stub.Verify()` |
| `It.IsAny<T>()` | Callback always receives all args |
| `It.Is<T>(pred)` | `stub.Method.When(pred).Return(val)` |

---

## Common Mistakes

### Missing `partial` Keyword

<!-- snippet: skill-mistake-partial -->
```cs
// WRONG: Compilation errors
// [KnockOff]
// public class FooStub : IFoo { }

// RIGHT:
[KnockOff]
public partial class SkillPartialDemoStub : ISvc { }
```
<!-- endSnippet -->

### Wrong Callback Signature

<!-- snippet: skill-mistake-wrong-signature -->
```cs
// WRONG: Type mismatch
// stub.Process.Return((string id) => { });  // Method takes int

// RIGHT: Match signature exactly
stub.Process.Call((int id) => { });
```
<!-- endSnippet -->

### Forgetting .Object for Class Stubs

<!-- snippet: skill-mistake-forgetting-object -->
```cs
// WRONG:
// MyClass service = stub;  // Won't compile

// RIGHT:
var stub = new Stubs.ServiceBase();
ServiceBase service = stub.Object;
```
<!-- endSnippet -->

### Using Func<>/Action<> Instead of Named Delegates

<!-- snippet: skill-mistake-func-action -->
```cs
// WRONG: KnockOff doesn't support generic delegates
// [KnockOff<Func<int, string>>]  // Won't work

// RIGHT: Define a named delegate
public delegate string SkillNamedOperation(int value);
[KnockOff<SkillNamedOperation>]
public partial class SkillNamedDelegateHost { }
```
<!-- endSnippet -->

### Expecting Sequences to Return Default After Exhaustion

<!-- snippet: skill-mistake-sequence-exhaustion -->
```cs
// Sequences repeat last value by default (NSubstitute-like behavior)
stub.GetNext.Return(1, 2);
// After 2 calls, returns 2 (repeats last value)

// Use ThenDefault() to return default(T) instead of repeating
stub.GetNext.Return(1, 2).ThenDefault();
// After 2 calls, returns 0 (default)

// Use Strict mode to throw when sequence exhausted
stub.Strict = true;
stub.GetNext.Return(1, 2);
// Third call throws StubException
```
<!-- endSnippet -->

---

## Reference Documentation

For detailed documentation, see the reference files in `references/`:

- **`references/patterns.md`** - Complete pattern guide with examples
- **`references/methods.md`** - Method configuration and verification
- **`references/properties.md`** - Property interceptors and user properties
- **`references/api-reference.md`** - Complete API reference
- **`references/strict-mode.md`** - Strict mode configuration
- **`references/moq-migration.md`** - Migration guide

---

**UPDATED:** 2026-02-08
