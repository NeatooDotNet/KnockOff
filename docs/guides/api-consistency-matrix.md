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
stub.GetData.Call((id) => $"Data-{id}");

// Verify calls
stub.GetData.Verify(Called.Never);
```
<!-- endSnippet -->

| Feature | All 8 Patterns |
|---------|:--------------:|
| `Return(value)` | ✓ |
| `Return((args) => result)` / `Call((args) => { })` | ✓ |
| `Verify(Called.X)` | ✓ |
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
stub.Name.VerifyGet(Called.Never);
stub.Name.VerifySet(Called.Never);

// Access history
// var lastSet = stub.Name.LastSetValue;
```
<!-- endSnippet -->

| Feature | All 8 Patterns |
|---------|:--------------:|
| `Get(value)` | ✓ |
| `Set((v) => { })` | ✓ |
| `VerifyGet(Called.X)` | ✓ |
| `VerifySet(Called.X)` | ✓ |
| `LastSetValue` | ✓ |

### Shadowed Properties (C# `new` modifier)

When an interface hierarchy uses `new` to shadow a property with a different accessor set (e.g., a derived interface narrows `int Prop { get; set; }` to `new int Prop { get; }`), the generated stub exposes **one interceptor** whose accessor set is the **union** of all shadowed declarations. Routing via any interface face works, and `stub.Prop.Get(...)` / `stub.Prop.Set(...)` are available whenever any declaration requires the corresponding accessor.

| Pattern | Shadowed Properties |
|---------|:--------------------:|
| 1 — Standalone `[KnockOff]` | ✓ |
| 2 — Generic Standalone `[KnockOff]` on `<T>` | ✓ |
| 5 — Inline `[KnockOff<IFoo>]` | ✓ |
| 8 — Inline Generic `[KnockOff(typeof(IFoo<>))]` | ✓ |
| 3, 4, 6, 9 — Class patterns | ✗ (tracked — see [property-new-narrowing-class-patterns](../todos/property-new-narrowing-class-patterns.md)) |

**Rule:** The single shared interceptor carries the union of accessors across all shadowed declarations that share a name; explicit interface implementations still route through that interceptor using each declaration's own accessor set.

**Why:** Interceptor-as-property requires one interceptor per property name. C# interface shadowing permits a narrower or wider accessor set on each face; the interceptor must support every accessor that any face requires. Per-face source fallbacks (e.g., `stub.Source(IInterfaceNarrow)`) bind only to accessors declared on the source face and emit `null` for the missing side. Added by [property-new-narrowing-bug](../plans/property-new-narrowing-bug.md). Design reference: `src/Design/Design.Stubs/Properties/NarrowingPropertyRepro.cs` and `src/Design/Design.Tests/PropertyTests/NarrowingPropertyTests.cs`.

---

## Feature 4: Indexer Interception

All 8 patterns use identical API:

<!-- snippet: matrix-indexer-interception -->
```cs
// Per-key Returns
stub.Indexer["preloaded"].Returns("data");

// Configure getter callback (fallback for unconfigured keys)
stub.Indexer.Get((key) => $"value-{key}");

// Configure setter
stub.Indexer.Set((key, value) => { });

// Verify
stub.Indexer.VerifyGet(Called.Never);
stub.Indexer.VerifySet(Called.Never);

// Access history
// var lastKey = stub.Indexer.LastGetKey;
// var lastEntry = stub.Indexer.LastSetEntry;
```
<!-- endSnippet -->

| Feature | All 8 Patterns |
|---------|:--------------:|
| `Indexer[key].Returns(value)` (per-key) | ✓ |
| `Get((key) => value)` (all-keys callback) | ✓ |
| `Set((key, value) => { })` | ✓ |
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
stub.DataReceived.VerifyAdd(Called.Never);
stub.DataReceived.VerifyRemove(Called.Never);
```
<!-- endSnippet -->

| Feature | All 8 Patterns |
|---------|:--------------:|
| `Raise(sender, args)` | ✓ |
| `HasSubscribers` | ✓ |
| `VerifyAdd(Called.X)` | ✓ |
| `VerifyRemove(Called.X)` | ✓ |

**Note:** All patterns use clean event names (e.g., `stub.DataReceived`). This is consistent across standalone, inline, and class stubs.

---

## Feature 6: Sequences

All 8 patterns use identical API:

<!-- snippet: matrix-sequences -->
```cs
// Return different values on successive calls
stub.GetStatus
    .Call(() => "Pending")
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
    .ThenWhen((int a, int b) => a < 0).Return(0);

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

**Priority:** When chains > Sequences > Return/Call > Stub Overrides > Source > Smart default

---

## Feature 8: Verification

All 8 patterns use identical API:

<!-- snippet: matrix-verification -->
```cs
// Mark for verification
stub.GetData.Call((id) => "data").Verifiable();

// Verify only marked items
// stub.Verify();  // Throws if any Verifiable() not called

// Verify all configured items
// stub.VerifyAll();  // Throws if any configured member not called

// Individual member verification
// stub.GetData.Verify(Called.Once);
```
<!-- endSnippet -->

| Feature | All 8 Patterns |
|---------|:--------------:|
| `.Verifiable()` | ✓ |
| `stub.Verify()` | ✓ |
| `stub.VerifyAll()` | ✓ |
| `Called.Once/Never/Exactly/AtLeast/AtMost` | ✓ |

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

## Feature 11: Stub Overrides

This is the one feature with intentional variation:

| | **Interface** | **Class** |
|---|---|---|
| **Standalone** | ✓ Override with `_` suffix | ✓ Override with `_` suffix |
| **Standalone Generic** | ✓ Override with `_` suffix | ✓ Override with `_` suffix |
| **Inline** | ✗ Fully generated | ✗ Fully generated |
| **Inline Generic** | ✗ Fully generated | ✗ Fully generated |

**All four standalone patterns** allow user-defined methods in the partial class.

### Defining a stub override (interface stubs, patterns 1, 2)

<!-- snippet: matrix-stub-overrides-interface -->
```cs
[KnockOff]
public partial class MatrixStubOverrideStub : IMatrixCalculator
{
    protected override int Add_(int a, int b) => a + b;
}
```
<!-- endSnippet -->

### Usage and Return override

<!-- snippet: matrix-stub-overrides-interface-usage -->
```cs
var stub = new MatrixStubOverrideStub();
IMatrixCalculator calc = stub;

// Stub override provides default behavior
var result = calc.Add(3, 4);
Assert.Equal(7, result);

// Return supersedes stub override
stub.Add.Call((int a, int b) => 999);
var overridden = calc.Add(3, 4);
Assert.Equal(999, overridden);
```
<!-- endSnippet -->

### Class stubs (patterns 3, 4)

Class stubs use the same `protected override MethodName_(...)` convention. Key differences:
- **Abstract methods**: Stub override IS the default behavior (no base implementation exists)
- **Virtual methods**: Stub override completely replaces the `base.Method()` call -- the stub override IS the fallback, not a supplement to the base call
- **Without stub override**: Virtual methods retain the standard interceptor path with `base.Method()` fallback

### Priority chain

Stub overrides sit between Return/Call and Source in the priority chain:

`When chains > Sequences > Return/Call > Stub Override > Source > Smart default`

**Inline patterns** are fully generated and cannot be extended. See the [Stub Overrides Guide](stub-overrides.md) for detailed examples.

---

## Feature 12: Async Method Auto-Wrapping

For async methods (`Task<T>`, `ValueTask<T>`), KnockOff provides three configuration tiers that auto-wrap return values. All 8 interface/class patterns use identical APIs:

<!-- snippet: matrix-async-autowrap -->
```cs
// Given: Task<string> GetDataAsync(int id)

// Tier 1: Returns(unwrappedValue) - auto-wraps in Task.FromResult
stub.GetDataAsync.Return("hello");

// Tier 2: Return(simplified callback) - returns T, auto-wrapped
stub.GetDataAsync.Call((id) => $"Data-{id}");

// Tier 3: Return(full delegate) - returns Task<T> directly
stub.GetDataAsync.Call((int id) => Task.FromResult($"Full-{id}"));
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

## Feature 13: Type Accessibility

Generated stub classes match the accessibility of the target type. When the target interface, class, or delegate is `internal`, the generated stub class is also `internal`. All 9 patterns (including Pattern 7: Inline Delegate) are supported.

| | **Interface** | **Class** |
|---|---|---|
| **Standalone** | User's stub class controls accessibility.<br>Generated `Base` class matches.<br>`internal partial class Stub : IFoo` -> `internal class StubBase` | User's stub class controls accessibility.<br>Generated `Base` class matches.<br>`internal partial class Stub` -> `internal class StubBase` |
| **Standalone Generic** | Same as Standalone | Same as Standalone |
| **Inline** | Generated stub matches target type.<br>`internal interface IFoo` -> `internal class IFoo` | Generated stub matches target type.<br>`internal class Foo` -> `internal class Foo` |
| **Inline Generic** | Same as Inline | Same as Inline |

| Feature | All 9 Patterns |
|---------|:--------------:|
| `public` target -> `public` stub | ✓ |
| `internal` target -> `internal` stub | ✓ |
| Inline Delegate: `internal` delegate -> `internal sealed class` | ✓ |

**Rule:** Standalone patterns derive accessibility from the user's stub class declaration. Inline patterns derive accessibility from the target type's declaration.

**Why:** A `public` class cannot implement an `internal` interface (CS0060) or inherit from an `internal` class (CS0060). For standalone patterns, the user controls the stub class accessibility (which allows a `public` stub for an `internal` interface when `InternalsVisibleTo` is configured). For inline patterns, the generator controls the stub class and must match the target type.

**Design.Stubs reference:** `src/Design/Design.Stubs/Advanced/InternalAccessibility.cs` (Patterns 1, 3, 5, 6, 7, 8). Added via [internal-interface-stub-accessibility](../plans/internal-interface-stub-accessibility.md).

---

## Summary: Consistency Status

| Feature Category | Status |
|------------------|--------|
| Method Interception | ✓ **100% consistent** |
| Property Interception | ✓ **100% consistent** (shadowed properties: interface patterns 1, 2, 5, 8 only — class patterns 3, 4, 6, 9 tracked) |
| Indexer Interception | ✓ **100% consistent** |
| Event Interception | ✓ **100% consistent** |
| Sequences | ✓ **100% consistent** |
| Conditional Matching | ✓ **100% consistent** |
| Verification | ✓ **100% consistent** |
| Strict Mode | ✓ **100% consistent** |
| Reset | ✓ **100% consistent** |
| Target Access | ✓ **Logical split** (Interface=direct, Class=`.Object`) |
| Stub Overrides | ✓ **Logical split** (Standalone=yes, Inline=no) |
| Async Auto-Wrapping | ✓ **100% consistent** (all 9 patterns) |
| Type Accessibility | ✓ **Logical split** (Standalone=user's class, Inline=target type) |

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
calcStub.Add.Call((int a, int b) => a + b);
var result = calc.Add(3, 4);
Assert.Equal(7, result);

// Verification - same API across all patterns
calcStub.Add.Verify(Called.Once);
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

### Why Only Standalone Patterns Support Stub Overrides

Inline stubs are fully generated inside the test class's `Stubs` namespace. There's no partial class for users to extend. Standalone stubs are partial classes that users define, allowing them to add custom methods, constructors, and (for class stubs) override base class methods.
