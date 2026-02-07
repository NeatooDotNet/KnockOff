# API Consistency Matrix

This document maps KnockOff's API across the 8 interface/class stub patterns (2×2×2 matrix) to demonstrate consistency and document intentional variations.

## The 2×2×2 Matrix

|  | **Interface** | **Class** |
|---|---|---|
| **Standalone** | Pattern 1: `[KnockOff]` | Pattern 3: `[KnockOffBase<T>]` |
| **Standalone Generic** | Pattern 2: `[KnockOff]` on `<T>` | Pattern 4: `[KnockOffBase(typeof(T<>))]` |
| **Inline** | Pattern 5: `[KnockOff<IFoo>]` | Pattern 6: `[KnockOff<Foo>]` |
| **Inline Generic** | Pattern 8: `[KnockOff(typeof(IFoo<>))]` | Pattern 9: `[KnockOff(typeof(Foo<>))]` |

*Pattern 7 (Inline Delegate) is a separate category.*

---

## Feature 1: Instantiation & Target Access

| | **Interface** | **Class** |
|---|---|---|
| **Standalone** | `var stub = new FooStub();`<br>`IFoo foo = stub;` | `var stub = new FooStub();`<br>`Foo foo = stub.Object;` |
| **Standalone Generic** | `var stub = new FooStub<T>();`<br>`IFoo<T> foo = stub;` | `var stub = new FooStub<T>();`<br>`Foo<T> foo = stub.Object;` |
| **Inline** | `var stub = new Stubs.IFoo();`<br>`IFoo foo = stub;` | `var stub = new Stubs.Foo();`<br>`Foo foo = stub.Object;` |
| **Inline Generic** | `var stub = new Stubs.IFoo<T>();`<br>`IFoo<T> foo = stub;` | `var stub = new Stubs.Foo<T>();`<br>`Foo<T> foo = stub.Object;` |

**Rule:** Interface stubs allow direct assignment. Class stubs require `.Object`.

**Why:** Class stubs use composition (wrapper + nested Impl) to avoid name collisions between interceptor properties and overridden members.

---

## Feature 2: Method Interception

All 8 patterns use identical API:

<!-- snippet: matrix-method-interception -->
```cs
// Configure behavior
stub.GetData.Return("test-value");
stub.GetData.Return((id) => $"Data-{id}");

// Verify calls
stub.GetData.Verify(Times.Never);
```
<!-- endSnippet -->

| Feature | All 8 Patterns |
|---------|:--------------:|
| `Return(value)` | ✓ |
| `Return((args) => result)` / `Call((args) => { })` | ✓ |
| `Verify(Times.X)` | ✓ |
| `LastArg` / `LastArgs` | ✓ |

---

## Feature 3: Property Interception

All 8 patterns use identical API:

<!-- snippet: matrix-property-interception -->
```cs
// Configure getter
stub.Name.Get("test-name");

// Configure setter
stub.Name.Set((value) => { /* capture or validate */ });

// Verify
stub.Name.VerifyGet(Times.Never);
stub.Name.VerifySet(Times.Never);

// Access history
// var lastSet = stub.Name.LastSetValue;
```
<!-- endSnippet -->

| Feature | All 8 Patterns |
|---------|:--------------:|
| `Get(value)` | ✓ |
| `Set((v) => { })` | ✓ |
| `VerifyGet(Times.X)` | ✓ |
| `VerifySet(Times.X)` | ✓ |
| `LastSetValue` | ✓ |

---

## Feature 4: Indexer Interception

All 8 patterns use identical API:

<!-- snippet: matrix-indexer-interception -->
```cs
// Configure getter
stub.Indexer.Get((key) => $"value-{key}");

// Configure setter
stub.Indexer.Set((key, value) => { });

// Use backing dictionary
stub.Indexer.Backing["preloaded"] = "data";

// Verify
stub.Indexer.VerifyGet(Times.Never);
stub.Indexer.VerifySet(Times.Never);

// Access history
// var lastKey = stub.Indexer.LastGetKey;
// var lastEntry = stub.Indexer.LastSetEntry;
```
<!-- endSnippet -->

| Feature | All 8 Patterns |
|---------|:--------------:|
| `Get((key) => value)` | ✓ |
| `Set((key, value) => { })` | ✓ |
| `Backing` dictionary | ✓ |
| `VerifyGet()` / `VerifySet()` | ✓ |
| `LastGetKey` / `LastSetEntry` | ✓ |

---

## Feature 5: Event Interception

All 8 patterns use identical API:

<!-- snippet: matrix-event-interception -->
```cs
// Raise event
stub.DataReceived.Raise(stub, new DataEventArgs { Data = "test" });

// Check subscription
bool hasSubscribers = stub.DataReceived.HasSubscribers;

// Verify add/remove
stub.DataReceived.VerifyAdd(Times.Never);
stub.DataReceived.VerifyRemove(Times.Never);
```
<!-- endSnippet -->

| Feature | All 8 Patterns |
|---------|:--------------:|
| `Raise(sender, args)` | ✓ |
| `HasSubscribers` | ✓ |
| `VerifyAdd(Times.X)` | ✓ |
| `VerifyRemove(Times.X)` | ✓ |

**Note:** All patterns use clean event names (e.g., `stub.DataReceived`). This is consistent across standalone, inline, and class stubs.

---

## Feature 6: Sequences

All 8 patterns use identical API:

<!-- snippet: matrix-sequences -->
```cs
// Return different values on successive calls
stub.GetStatus
    .Return(() => "Pending")
    .ThenReturn(() => "Processing")
    .ThenReturn(() => "Complete");
// Call 1: "Pending", Call 2: "Processing", Call 3+: "Complete" (repeats last)

// Properties support sequences too
configStub.Name
    .Get("first")
    .ThenGet("second");
```
<!-- endSnippet -->

| Feature | All 8 Patterns |
|---------|:--------------:|
| `Return(callback).ThenReturn(callback)` / `Call(callback).ThenCall(callback)` | ✓ |
| Repeats last value | ✓ |
| Property sequences (`Get().ThenGet()`) | ✓ |

---

## Feature 7: Conditional Matching (When)

All 8 patterns use identical API:

<!-- snippet: matrix-when-chains -->
```cs
// Chain multiple conditions (sequential - each consumed once)
stub.Add
    .When(1, 2).Return(100)
    .ThenWhen(3, 4).Return(200)
    .ThenWhen((a, b) => a < 0).Return(0);

// Fallback for non-matching calls or after chain is consumed
stub.Add.Return(42);
```
<!-- endSnippet -->

| Feature | All 8 Patterns |
|---------|:--------------:|
| `When(values).Return()` | ✓ |
| `When(predicate).Return()` | ✓ |
| `ThenWhen()` chaining | ✓ |
| Fallback behavior | ✓ |

**Priority:** When chains > Sequences > Return/Call > User Methods > Source > Smart default

---

## Feature 8: Verification

All 8 patterns use identical API:

<!-- snippet: matrix-verification -->
```cs
// Mark for verification
stub.GetData.Return((id) => "data").Verifiable();

// Verify only marked items
// stub.Verify();  // Throws if any Verifiable() not called

// Verify all configured items
// stub.VerifyAll();  // Throws if any configured member not called

// Individual member verification
// stub.GetData.Verify(Times.Once);
```
<!-- endSnippet -->

| Feature | All 8 Patterns |
|---------|:--------------:|
| `.Verifiable()` | ✓ |
| `stub.Verify()` | ✓ |
| `stub.VerifyAll()` | ✓ |
| `Times.Once/Never/Exactly/AtLeast/AtMost` | ✓ |

---

## Feature 9: Strict Mode

All 8 patterns use identical API:

<!-- snippet: matrix-strict-mode -->
```cs
// Enable strict mode via property
var stub = new MatrixServiceStub();
stub.Strict = true;
// Or fluently
var fluentStub = new MatrixServiceStub().Strict();
```
<!-- endSnippet -->

| Behavior | Interface Stubs | Class Stubs |
|----------|-----------------|-------------|
| Unconfigured method | Throws `StubException` | Throws `StubException` |
| Non-strict unconfigured | Returns smart default | Calls base class |

| Feature | All 8 Patterns |
|---------|:--------------:|
| `stub.Strict = true` | ✓ |
| `.Strict()` extension | ✓ |
| Throws `StubException` | ✓ |

---

## Feature 10: Reset

All 8 patterns use identical API:

<!-- snippet: matrix-reset -->
```cs
// Reset individual member
stub.GetData.Reset();
stub.Save.Reset();
```
<!-- endSnippet -->

Reset clears:
- Return/Call configuration
- When matchers
- Call history (LastArg, LastArgs)
- Sequence position
- Verifiable marking

| Feature | All 8 Patterns |
|---------|:--------------:|
| `member.Reset()` | ✓ |

---

## Feature 11: User Methods

This is the one feature with intentional variation:

| | **Interface** | **Class** |
|---|---|---|
| **Standalone** | ✓ Override with `_` suffix | ✓ Override with `_` suffix |
| **Standalone Generic** | ✓ Override with `_` suffix | ✓ Override with `_` suffix |
| **Inline** | ✗ Fully generated | ✗ Fully generated |
| **Inline Generic** | ✗ Fully generated | ✗ Fully generated |

**All four standalone patterns** allow user-defined methods in the partial class.

### Defining a user method (interface stubs, patterns 1, 2)

<!-- snippet: matrix-user-methods-interface -->
```cs
public partial class MatrixUserMethodStub
{
    protected override int Add_(int a, int b) => a + b;
}
```
<!-- endSnippet -->

### Usage and Returns override

<!-- snippet: matrix-user-methods-interface-usage -->
```cs
var stub = new MatrixUserMethodStub();
IMatrixCalculator calc = stub;

// User method provides default behavior
var result = calc.Add(3, 4);
Assert.Equal(7, result);

// Return supersedes user method
stub.Add.Return((a, b) => 999);
var overridden = calc.Add(3, 4);
Assert.Equal(999, overridden);
```
<!-- endSnippet -->

### Class stubs (patterns 3, 4)

Class stubs use the same `protected override MethodName_(...)` convention. Key differences:
- **Abstract methods**: User method IS the default behavior (no base implementation exists)
- **Virtual methods**: User method completely replaces the `base.Method()` call -- the user override IS the fallback, not a supplement to the base call
- **Without user override**: Virtual methods retain the standard interceptor path with `base.Method()` fallback

### Priority chain

User methods sit between Return/Call and Source in the priority chain:

`When chains > Sequences > Return/Call > User Method > Source > Smart default`

**Inline patterns** are fully generated and cannot be extended. See the [User Methods Guide](user-methods.md) for detailed examples.

---

## Feature 12: Async Method Auto-Wrapping

For async methods (`Task<T>`, `ValueTask<T>`), KnockOff provides three configuration tiers that auto-wrap return values. All 8 interface/class patterns use identical APIs:

<!-- snippet: matrix-async-autowrap -->
```cs
// Given: Task<string> GetDataAsync(int id)

// Tier 1: Returns(unwrappedValue) - auto-wraps in Task.FromResult
stub.GetDataAsync.Return("hello");

// Tier 2: Return(simplified callback) - returns T, auto-wrapped
stub.GetDataAsync.Return((id) => $"Data-{id}");

// Tier 3: Return(full delegate) - returns Task<T> directly
stub.GetDataAsync.Return((int id) => Task.FromResult($"Full-{id}"));
```
<!-- endSnippet -->

| Feature | All 8 Patterns |
|---------|:--------------:|
| `Return(unwrappedValue)` auto-wrap | ✓ |
| `Return(Func<..., T>)` simplified callback | ✓ |
| `Return(Func<..., Task<T>>)` full delegate | ✓ |
| Void async `Call(Action<...>)` | ✓ |
| `ValueTask<T>` auto-wrap | ✓ |

**See also:** [Async Patterns Guide](async-patterns.md) for detailed examples including delays and failure simulation.

---

## Summary: Consistency Status

| Feature Category | Status |
|------------------|--------|
| Method Interception | ✓ **100% consistent** |
| Property Interception | ✓ **100% consistent** |
| Indexer Interception | ✓ **100% consistent** |
| Event Interception | ✓ **100% consistent** |
| Sequences | ✓ **100% consistent** |
| Conditional Matching | ✓ **100% consistent** |
| Verification | ✓ **100% consistent** |
| Strict Mode | ✓ **100% consistent** |
| Reset | ✓ **100% consistent** |
| Target Access | ✓ **Logical split** (Interface=direct, Class=`.Object`) |
| User Methods | ✓ **Logical split** (Standalone=yes, Inline=no) |
| Async Auto-Wrapping | ✓ **100% consistent** (all 9 patterns) |

---

## Quick Reference: Standalone Interface Pattern

Stub declaration:

<!-- snippet: matrix-instantiation -->
```cs
// Pattern 1: Standalone Interface
[KnockOff]
public partial class MatrixCalcStub : IMatrixCalculator { }
```
<!-- endSnippet -->

Configure, use, and verify:

<!-- snippet: matrix-all-patterns -->
```cs
// Pattern 1: Standalone Interface
var calcStub = new MatrixCalcStub();
IMatrixCalculator calc = calcStub;

// Configure and use - same API across all patterns
calcStub.Add.Return((a, b) => a + b);
var result = calc.Add(3, 4);
Assert.Equal(7, result);

// Verification - same API across all patterns
calcStub.Add.Verify(Times.Once);
Assert.Equal((3, 4), calcStub.Add.LastArgs);
```
<!-- endSnippet -->

The API is identical across all 8 patterns. The only variation is instantiation (see Feature 1 table above) and `.Object` for class stubs.

---

## Intentional Variations Explained

### Why Class Stubs Require `.Object`

Class stubs use a composition pattern (wrapper + nested Impl) to avoid C# compilation errors. If the stub class inherited from the target, there would be name collisions between:
- Interceptor properties (`Name`, `Execute`)
- Overridden members (`override string Name`, `override void Execute()`)

The `.Object` property returns the nested Impl instance that actually inherits from the target class.

### Why Only Standalone Patterns Support User Methods

Inline stubs are fully generated inside the test class's `Stubs` namespace. There's no partial class for users to extend. Standalone stubs are partial classes that users define, allowing them to add custom methods, constructors, and (for class stubs) override base class methods.
