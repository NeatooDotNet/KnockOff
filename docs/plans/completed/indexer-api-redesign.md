# Indexer API Redesign

**Date:** 2026-02-09
**Related Todo:** [Indexer API Redesign](../todos/indexer-api-redesign.md)
**Status:** Verified
**Last Updated:** 2026-02-09 (architect: post-implementation verification passed)

---

## Overview

Redesign KnockOff's indexer API to address four usability issues: verbose `Backing` dictionary, non-discoverable `OfXxx` container pattern for multi-indexer interfaces, lack of per-key configuration, and unnatural tuple key syntax for multi-parameter indexers.

The new API uses C# indexer overloads on the interceptor class for type disambiguation, per-key builders returned by `stub.Indexer[key]`, flattened indexer accessors matching the interface declaration, and callback overloads resolved by the compiler.

---

## Approach

### Core Design Changes

1. **Indexer overloads on the interceptor** replace both `Backing` and `OfXxx` -- C# overload resolution by key type handles disambiguation
2. **Per-key builders** returned by `stub.Indexer[key].Returns(value)` enable Rocks-style per-key setup
3. **Flattened indexer accessors** for multi-param indexers -- `stub.Indexer[1, 2]` not `stub.Indexer[(1, 2)]`
4. **Callback overloads** for all-keys configuration -- compiler resolves by delegate signature

### Flattening Scope (CRITICAL CLARIFICATION)

**Only the indexer accessor on the interceptor class is flattened.** Everything else uses the tuple TKey:

| API surface | Single-param | Multi-param `this[int row, int col]` |
|---|---|---|
| Interceptor indexer | `this[string key]` | `this[int row, int col]` -- **FLATTENED** |
| Library interfaces | `IIndexerGetBuilder<string, TValue>` | `IIndexerGetBuilder<(int, int), TValue>` -- **TUPLE** |
| All-keys Get callback | `Func<string, TValue>` | `Func<(int row, int col), TValue>` -- **TUPLE** |
| All-keys Set callback | `Action<string, TValue>` | `Action<(int row, int col), TValue>` -- **TUPLE** |
| ThenGet/ThenSet | `Func<string, TValue>` | `Func<(int row, int col), TValue>` -- **TUPLE** |
| LastGetKey type | `string?` | `(int row, int col)?` -- **TUPLE** |
| Per-key storage key | `Dictionary<string, PerKeyBuilder>` | `Dictionary<(int, int), PerKeyBuilder>` -- **TUPLE** |
| Per-key builder Returns | `Returns(TValue)` | `Returns(TValue)` -- no key involved |

This means:
- **No changes to library interfaces** (`IIndexerGetBuilder`, `IIndexerGetSequence`, `IIndexerSetBuilder`, `IIndexerSetSequence`, tracking interfaces). They all keep using `TKey` which is `(int, int)` for multi-param.
- **No generated standalone builder classes** for multi-param. The library generic interfaces work with tuple TKey.
- **The ONLY place flattening occurs** is the C# indexer on the interceptor class: `this[int row, int col]` instead of `this[(int, int) key]`.

### Priority Chain (invocation order)

1. Per-key config (`stub.Indexer[3].Returns(42)`) -- exact match
2. All-keys callback (`stub.Indexer.Get(...)`) -- fallback
3. Source delegation (if configured)
4. Strict mode throws / return default

---

## Design

### 1. New API Surface

#### Single-Indexer Interface (most common case)

```csharp
// Interface: int this[string key] { get; set; }

// Per-key configuration via indexer
stub.Indexer["foo"].Returns(42);
stub.Indexer["foo"].Get(() => ComputeIt());
stub.Indexer["foo"].Set((value) => CaptureIt(value));

// All-keys callback (fallback)
stub.Indexer.Get((string key) => key.Length);
stub.Indexer.Set((string key, int value) => { });

// Tracking
stub.Indexer.LastGetKey;         // string?
stub.Indexer.LastSetEntry;       // (string Key, int Value)?
stub.Indexer.VerifyGet(Called.Once);
stub.Indexer.VerifySet(Called.Exactly(3));

// Sequences (all-keys level)
stub.Indexer.Get((k) => 1).ThenGet((k) => 2).ThenGet((k) => 3);

// Per-key sequences
stub.Indexer["foo"].Returns(1).ThenReturns(2).ThenReturns(3);
```

#### Multi-Indexer Interface (multiple key types)

```csharp
// Interface:
//   string this[string key] { get; set; }
//   int this[int index] { get; }

// Indexer overloads resolve by key type -- no OfXxx needed
stub.Indexer["foo"].Returns("bar");    // string indexer
stub.Indexer[3].Returns(42);           // int indexer

// All-keys callbacks also overloaded by delegate signature
stub.Indexer.Get((string key) => key.ToUpperInvariant());  // string indexer
stub.Indexer.Get((int key) => key * 10);                    // int indexer
stub.Indexer.Set((string key, string value) => { });        // string setter

// Tracking per key type (type-suffixed for multi-indexer)
stub.Indexer.LastStringGetKey;          // string?
stub.Indexer.LastInt32GetKey;           // int?
stub.Indexer.VerifyGet(Called.Exactly(2)); // verifies ALL indexer gets combined
```

#### Multi-Parameter Indexer (flattened indexer accessor)

```csharp
// Interface: double this[int row, int col] { get; set; }

// Flattened indexer accessor on the interceptor
stub.Indexer[1, 2].Returns(3.14);

// Callbacks use tuple key (via library interfaces -- unchanged)
stub.Indexer.Get(((int row, int col) key) => key.row * 10.0 + key.col);
stub.Indexer.Set(((int row, int col) key, double value) => { });

// Tracking uses tuple for key capture (unchanged)
stub.Indexer.LastGetKey;  // (int row, int col)?
```

### 2. Per-Key Builder Design

When `stub.Indexer[key]` is called, it returns a per-key builder that enables configuration for that specific key.

```csharp
// Generated per-key builder (nested inside interceptor class)
public sealed class PerKeyBuilder
{
    // Internal state: tracks whether anything was actually configured
    internal bool HasGetConfig => _getValue != null || _getCallback != null || _getSequence != null;
    internal bool HasSetConfig => _setCallback != null;

    // Getter configuration
    public PerKeyBuilder Returns(TValue value);
    public PerKeySequence ThenReturns(TValue value);  // elevates to per-key sequence
    public PerKeyBuilder Get(Func<TValue> callback);

    // Setter configuration (only generated when indexer has setter)
    public PerKeyBuilder Set(Action<TValue> callback);
}
```

**Key design decisions for per-key builders:**

- **Per-key builders are generated, not library interfaces.** The `Returns(TValue)` method stores a value in the builder and the interceptor's `InvokeGet` retrieves it. This is specific to the generated interceptor's internal storage. Library generic interfaces would need `TKey` for the indexer accessor, but per-key callbacks intentionally omit the key -- it is already bound.
- **Per-key storage** uses a `Dictionary<TKey, PerKeyBuilder>` inside the interceptor. The indexer accessor creates or retrieves the builder for the given key.
- **Per-key sequences** work analogously to property sequences: `Returns(1).ThenReturns(2).ThenReturns(3)`.
- **Per-key and all-keys coexist.** The priority chain checks per-key first, then falls back to all-keys callback.
- **Per-key builder tracks configuration state independently of creation.** Accessing `stub.Indexer[key]` creates the builder lazily but does NOT mark the interceptor as "configured." Only calling `.Returns()`, `.Get()`, or `.Set()` on the per-key builder counts as configuration. See Concern #4 resolution.
- **PerKeyBuilder shape varies by indexer accessors.** For get-only indexers, `Set()` is not generated. For set-only (init-only without getter), `Returns()` and `Get()` are not generated. See Concern #5 resolution.

### 3. All-Keys Callback Design (replaces current Get/Set)

The current `Get(Func<TKey, TValue>)` and `Set(Action<TKey, TValue>)` remain functionally identical. Library interfaces are unchanged.

- **For single-param indexers:** `Get(Func<string, int>)` -- same as today.
- **For multi-param indexers:** `Get(Func<(int, int), double>)` -- same as today. TKey is the tuple.
- **For multi-indexer interfaces:** Callbacks are overloaded by delegate signature on the same interceptor. `Get(Func<string, string>)` for the string indexer, `Get(Func<int, int>)` for the int indexer.

**No changes to library interfaces.** `IIndexerGetBuilder<TKey, TValue>`, `IIndexerGetSequence<TKey, TValue>`, etc. all remain as-is. The generated interceptor implements them with the concrete types.

### 4. Interceptor Class Architecture

#### Single-Indexer (current: one interceptor class)

The current `IndexerInterceptor` class is replaced with a new version that has:

```csharp
public sealed class IndexerInterceptor
{
    // Per-key storage
    private readonly Dictionary<TKey, PerKeyBuilder> _perKeyBuilders = new();

    // Indexer accessor -- returns per-key builder
    public PerKeyBuilder this[TKey key]
    {
        get => _perKeyBuilders.TryGetValue(key, out var b) ? b : (_perKeyBuilders[key] = new PerKeyBuilder());
    }

    // All-keys callback (fallback) -- returns library interface
    public IIndexerGetBuilder<TKey, TValue> Get(Func<TKey, TValue> callback);
    public IIndexerSetBuilder<TKey, TValue> Set(Action<TKey, TValue> callback);

    // Tracking (unchanged API)
    public TKey? LastGetKey { get; }
    public (TKey Key, TValue Value)? LastSetEntry { get; }

    // Verification (unchanged API)
    public void VerifyGet(Called times);
    public void VerifySet(Called times);

    // IsConfigured: checks per-key builder CONFIG state + all-keys state
    // Does NOT count lazily-created but unconfigured per-key builders
    internal bool IsConfigured => _perKeyBuilders.Values.Any(b => b.HasGetConfig || b.HasSetConfig)
        || _get != null || _set != null || (_getSequence?.Count ?? 0) > 0 || (_setSequence?.Count ?? 0) > 0;

    // InvokeGet/InvokeSet (called by interface implementation)
    internal TValue InvokeGet(bool strict, TKey key);
    internal void InvokeSet(bool strict, TKey key, TValue value);
}
```

#### Multi-Indexer (current: container with OfXxx properties)

The current `IndexerContainer` with `OfString`/`OfInt32` properties is **replaced** with a single interceptor class that has multiple indexer overloads:

```csharp
public sealed class IndexerInterceptor
{
    // Per-key storage per key type
    private readonly Dictionary<string, StringPerKeyBuilder> _stringPerKeyBuilders = new();
    private readonly Dictionary<int, Int32PerKeyBuilder> _int32PerKeyBuilders = new();

    // Indexer overloads -- C# resolves by key type
    public StringPerKeyBuilder this[string key] { get; }
    public Int32PerKeyBuilder this[int key] { get; }

    // All-keys callbacks overloaded by delegate type
    public IIndexerGetBuilder<string, string> Get(Func<string, string> callback);
    public IIndexerGetBuilder<int, int> Get(Func<int, int> callback);

    // Set overloads
    public IIndexerSetBuilder<string, string> Set(Action<string, string> callback);

    // Per-type tracking (type-suffixed for multi-indexer)
    public string? LastStringGetKey { get; }
    public int? LastInt32GetKey { get; }
    public (string Key, string Value)? LastStringSetEntry { get; }

    // Combined verification
    public void VerifyGet(Called times);  // combined across all key types
    public void VerifySet(Called times);  // combined across all key types

    // Ref return: one _refReturnBacking field per key type
    internal string _refReturnBackingString;
    internal int _refReturnBackingInt32;

    // Per-type invoke methods (type-suffixed with KeyTypeFriendlyName)
    internal string InvokeGet_String(bool strict, string key);
    internal int InvokeGet_Int32(bool strict, int key);
    internal ref string InvokeRefGet_String(bool strict, string key);
    internal ref readonly int InvokeRefGet_Int32(bool strict, int key);
    internal void InvokeSet_String(bool strict, string key, string value);
}
```

#### Multi-Parameter Indexer (flattened indexer accessor)

For `this[int row, int col]`, the interceptor flattens ONLY the indexer accessor:

```csharp
public sealed class IndexerInterceptor
{
    // Per-key storage uses tuple key (TKey = (int, int))
    private readonly Dictionary<(int, int), PerKeyBuilder> _perKeyBuilders = new();

    // Indexer with flattened params -- the ONLY place flattening occurs
    public PerKeyBuilder this[int row, int col]
    {
        get
        {
            var key = (row, col);
            return _perKeyBuilders.TryGetValue(key, out var b)
                ? b : (_perKeyBuilders[key] = new PerKeyBuilder());
        }
    }

    // Callbacks use tuple key via library interfaces -- UNCHANGED
    public IIndexerGetBuilder<(int, int), double> Get(Func<(int, int), double> callback);
    public IIndexerSetBuilder<(int, int), double> Set(Action<(int, int), double> callback);

    // Tracking uses tuple -- UNCHANGED
    public (int row, int col)? LastGetKey { get; }

    // InvokeGet receives tuple key internally
    internal double InvokeGet(bool strict, (int, int) key);
}
```

### 5. Library Interface Changes

**COMMITTED DECISION: No changes to library interfaces.**

All existing library interfaces remain exactly as-is:
- `IIndexerGetBuilder<TKey, TValue>` -- used for all-keys callbacks, returns from `Get(...)`
- `IIndexerSetBuilder<TKey, TValue>` -- used for all-keys callbacks, returns from `Set(...)`
- `IIndexerGetSequence<TKey, TValue>` -- used for all-keys sequences, returns from `ThenGet(...)`
- `IIndexerSetSequence<TKey, TValue>` -- used for all-keys sequences, returns from `ThenSet(...)`
- `IIndexerGetTracking<TKey>` -- tracking interface
- `IIndexerSetTracking<TKey, TValue>` -- tracking interface

For multi-param indexers, `TKey` is the tuple type (e.g., `(int, int)`). This matches the current behavior. The only change is the interceptor's indexer accessor uses flattened params.

Per-key builders are **generated** (not library interfaces) because they are specific to the generated interceptor's internal storage.

### 6. InvokeGet/InvokeSet Priority Chain

```
InvokeGet(key):
  1. Check per-key builders: if _perKeyBuilders.TryGetValue(key, out var builder) && builder.HasGetConfig:
     return builder.InvokeGet()
  2. Check all-keys sequence: if _getSequence != null && _getSequenceIndex < _getSequence.Count:
     advance sequence, return callback(key)
  3. Check all-keys repeating callback: if _get != null:
     return _get(key)
  4. Track unconfigured call
  5. Check source delegation: if _source != null:
     return _source[key]
  6. Check strict mode: if strict, throw
  7. Return default
```

Note: Step 1 checks `builder.HasGetConfig`, not just builder existence. A lazily-created but unconfigured builder is skipped.

### 7. Tracking Design for Multi-Indexer

For interfaces with multiple indexer key types, generate separate tracking per type with suffixed names using `KeyTypeFriendlyName`:

```csharp
// Single indexer: string key
public string? LastGetKey { get; }
public (string Key, int Value)? LastSetEntry { get; }

// Multi-indexer: string + int keys
public string? LastStringGetKey { get; }
public int? LastInt32GetKey { get; }
public (string Key, string Value)? LastStringSetEntry { get; }
```

Verification is combined across all key types:
```csharp
public void VerifyGet(Called times);  // total get count across ALL key types
public void VerifySet(Called times);  // total set count across ALL key types
```

### 8. Sequence Design

**All-keys sequences:** Continue to work as today. `stub.Indexer.Get(k => 1).ThenGet(k => 2)` advances per access regardless of key. Unchanged semantics.

**Per-key sequences:** New capability. `stub.Indexer["foo"].Returns(1).ThenReturns(2)` creates a sequence specific to key "foo". Different keys have independent sequences.

```csharp
stub.Indexer["foo"].Returns(1).ThenReturns(2);
stub.Indexer["bar"].Returns(100).ThenReturns(200);

// foo: 1, 2, 2 (repeat), 2...
// bar: 100, 200, 200 (repeat), 200...
```

---

## Architect Responses to Developer Concerns

### Concern 1 [Blocking]: Design.Stubs verification not performed

**Resolution: DONE.** Failing acceptance criteria code has been written in:
- `src/Design/Design.Stubs/Indexers/IndexerRedesignAcceptance.cs` -- 15 acceptance criteria tests covering inline interface (pattern 5), standalone (pattern 1), multi-param flattened, multi-indexer overloads, per-key builders, all-keys callbacks, sequences, tracking, init-only indexers.
- `src/Design/Design.Domain/Entities/ICollection.cs` -- Added `IMultiIndexerCollection` interface for multi-indexer testing.

Build output confirms all expected failures:
- `CS0021`: Cannot apply indexing with `[]` to interceptor types (interceptor needs C# indexer accessor)
- `CS1061`: `IndexerContainer` does not contain `Get`/`Set` (multi-indexer currently uses OfXxx, not direct methods)

See "Design Project Verification" section below for the full evidence.

### Concern 2 [Blocking]: Unresolved multi-param library interface decision

**Resolution: COMMITTED.** Library interfaces keep tuple TKey for multi-param indexers. No changes to any library interfaces.

The user's pain point is specifically the **indexer syntax**: `stub.Indexer[(10, 10, 10)]` (double parentheses from tuple in brackets). They want `stub.Indexer[10, 10, 10]` -- a multi-param C# indexer on the interceptor class.

For callbacks, tuples with named members are fine:
```csharp
stub.Indexer.Get(((int x, int y, int z) key) => key.x + key.y + key.z);
```

This means:
- The interceptor class gets multi-param C# indexers: `this[int x, int y, int z]` -- flattened
- Library interfaces keep using tuple TKey for multi-param: `TKey = (int, int, int)`
- Callbacks/sequences use the library interfaces as-is
- No need to generate standalone builder classes -- library generics work fine with tuple TKey
- The ONLY place flattening is required is the indexer accessor on the interceptor class

This eliminates the inconsistency the developer identified between `Get(...)` and `.ThenGet(...)` callback signatures. Both use the same tuple TKey via library interfaces.

### Concern 3 [Design]: Multi-indexer ref-return disambiguation

**Resolution:** For multi-indexer interfaces with different ref-return characteristics per key type (like `IRefReturnIndexerService` with `ref int this[int]` and `ref readonly int this[string]`), the interceptor generates **one `_refReturnBacking` field per key type**, using type-suffixed names:

```csharp
internal int _refReturnBacking_Int32;
internal int _refReturnBacking_String;
```

The implementation's override code references the correct backing field:
```csharp
// For ref int this[int index]:
_stub.Indexer.InvokeRefGet_Int32(strict, index);
return ref _stub.Indexer._refReturnBacking_Int32;

// For ref readonly int this[string key]:
_stub.Indexer.InvokeRefGet_String(strict, key);
return ref _stub.Indexer._refReturnBacking_String;
```

This uses the same `KeyTypeFriendlyName` suffix pattern used for type-suffixed InvokeGet methods (see Open Question 2 resolution).

### Concern 4 [Design]: IsConfigured semantics with lazy per-key builders

**Resolution:** `IsConfigured` does NOT count lazily-created but unconfigured per-key builders.

The per-key builder has internal state that tracks whether `.Returns()`, `.Get()`, or `.Set()` has been called:

```csharp
// Inside PerKeyBuilder:
internal bool HasGetConfig => _getValue != null || _getCallback != null || _getSequence != null;
internal bool HasSetConfig => _setCallback != null;
```

The interceptor's `IsConfigured` checks the per-key builders' config state, not their mere existence:

```csharp
internal bool IsConfigured =>
    _perKeyBuilders.Values.Any(b => b.HasGetConfig || b.HasSetConfig)
    || _get != null || _set != null
    || (_getSequence?.Count ?? 0) > 0 || (_setSequence?.Count ?? 0) > 0;
```

This means: `stub.Indexer["foo"]` alone (without calling `.Returns()`) does NOT prevent base class fall-through for virtual indexers. Only `stub.Indexer["foo"].Returns(42)` counts as configuration.

Similarly, the InvokeGet priority chain (step 1) checks `builder.HasGetConfig`, not just builder existence. A lazily-created but unconfigured builder is transparently skipped to allow fall-through to all-keys callback or base class.

### Concern 5 [Design]: Per-key builder for set-only/init-only indexers

**Resolution: Yes, the PerKeyBuilder is generated with only applicable methods.**

The generator already knows `HasGetter` and `HasSetter` from the model. The per-key builder uses this:

| Indexer accessors | PerKeyBuilder methods generated |
|---|---|
| `{ get; set; }` | `Returns(TValue)`, `ThenReturns(TValue)`, `Get(Func<TValue>)`, `Set(Action<TValue>)` |
| `{ get; }` | `Returns(TValue)`, `ThenReturns(TValue)`, `Get(Func<TValue>)` |
| `{ set; }` or `{ init; }` (no getter) | `Set(Action<TValue>)` |
| `{ get; init; }` | `Returns(TValue)`, `ThenReturns(TValue)`, `Get(Func<TValue>)`, `Set(Action<TValue>)` |

**Explicit clarification on the interceptor's indexer accessor:** The interceptor's `this[TKey key]` is always a get-only C# indexer that returns the per-key builder, regardless of whether the target interface's indexer has a getter. This is correct because the interceptor's indexer provides configuration access, not value access. The value access happens through `InvokeGet`/`InvokeSet` called by the interface implementation.

### Concern 6 [Design]: Open Questions resolved

All 4 open questions are now resolved. See "Resolved Open Questions" section below.

### Concern 7 [Minor]: Dead code cleanup scope

**Resolution:** Phase 0 now explicitly includes removal of all dead and obsolete code:

Dead code to remove:
- `FlatRenderer.RenderIndexerInterceptorClass` (line 902) -- unused method
- `FlatRenderer.BuildIndexerAccessMap` (line 439) -- OfXxx logic
- `FlatRenderer.RenderIndexerContainerClass` -- OfXxx container rendering
- `InlineRenderer.RenderIndexerContainerClass` -- OfXxx container rendering
- `InlineRenderer.GroupIndexers` / `BuildIndexerAccessMap` -- OfXxx grouping logic

Model types to remove:
- `UnifiedIndexerContainerModel`
- `FlatIndexerGroup`
- `InlineIndexerGroup`

Fields to remove from models:
- `KeyTypeFriendlyName` -- no longer needed for OfXxx naming (but may be repurposed for type-suffixed InvokeGet naming in multi-indexer case)
- `BaseName` -- used only for OfXxx grouping

### Concern 8 [Minor]: `Returns` vs `Return` naming convention

**Resolution: Use `Returns`/`ThenReturns` -- intentional divergence.**

The naming difference is deliberate and semantic:

| API | Name | Why |
|---|---|---|
| Method interceptor | `Return(callback)` | "Return this callback's result when called" -- the method returns |
| Per-key builder | `Returns(value)` | "This key returns this value" -- declarative, reads as English |

The precedent: properties use `Get(value)` and `Get(() => callback)` -- also different from methods. Per-key builders are closer to property configuration than method configuration: they configure what a specific key "returns" rather than providing a callback that processes input.

Additionally, `stub.Indexer["foo"].Returns(42)` reads more naturally than `stub.Indexer["foo"].Return(42)`. This mirrors how Moq uses `Returns` and NSubstitute uses `Returns`.

### Concern 9 [Minor]: Params array per-key warning

**Resolution: Yes, the migration guide will explicitly warn.**

Per-key `Returns()` does NOT work reliably for `params` array indexers because `string[]` uses reference equality in dictionary lookup. Example:

```csharp
// This will NOT work as expected:
stub.Indexer[1, new[] { "b" }].Returns(42);
svc[1, "b"]; // Different string[] instance -- no match

// Workaround: use all-keys callback
stub.Indexer.Get(((int a, string[] b) key) => /* custom matching logic */);
```

The migration guide will include this as a "Known Limitations" section. This is not a regression -- the old `Backing` dictionary had the same reference equality problem with array keys.

---

## Resolved Open Questions

### Q1: Per-key tracking

**Answer: No per-key tracking in V1. Defer to future enhancement.**

Per-key builders will NOT have `VerifyGet(Called)` or call count tracking in the initial implementation. Reasons:
- It significantly increases generated code per PerKeyBuilder class
- The interceptor-level `VerifyGet`/`VerifySet` combined with per-key configuration is sufficient for most test scenarios
- If a user needs per-key verification, they can use a callback: `stub.Indexer["foo"].Get(() => { callCount++; return 42; })`
- This can be added as a non-breaking enhancement later

### Q2: Type-suffixed InvokeGet naming convention

**Answer: Use `KeyTypeFriendlyName` with underscore separator.**

For multi-indexer interceptors, type-suffixed invoke methods use the existing `KeyTypeFriendlyName` field:

```
InvokeGet_String, InvokeGet_Int32, InvokeGet_Boolean
InvokeRefGet_String, InvokeRefGet_Int32
InvokeSet_String, InvokeSet_Int32
```

The `KeyTypeFriendlyName` is already computed by the builder pipeline and used for the current OfXxx pattern. We repurpose it for the invoke method suffix instead.

Note: For single-indexer interceptors, the methods remain unsuffixed: `InvokeGet`, `InvokeSet`, `InvokeRefGet`. The suffix is only needed when multiple indexers share one interceptor class.

The ClassRenderer and StandaloneClassRenderer will need to include the suffix in their generated override code when the interface has multiple indexers. The model already carries enough information (via `KeyTypeFriendlyName` or an equivalent field) to determine this.

### Q3: Per-key ThenDefault()

**Answer: Not in V1. Per-key sequences repeat the last value.**

Per-key sequences always repeat the last value after exhaustion, matching the default behavior of all-keys sequences. `ThenDefault()` is not generated on the per-key builder.

If needed, users can achieve the same effect with a callback:
```csharp
// Instead of ThenDefault():
stub.Indexer["foo"].Returns(1).ThenReturns(2);
// After 2 calls: repeats 2 forever

// If you need default-after-exhaustion:
int counter = 0;
int[] values = { 1, 2 };
stub.Indexer["foo"].Get(() => counter < values.Length ? values[counter++] : default);
```

### Q4: Sequence interaction verification

**Answer: Per-key and all-keys sequences are independent. No cross-interaction.**

When both per-key and all-keys sequences are configured:
- Per-key builder handles calls matching its key
- All-keys sequence handles all other calls
- Each tracks its own position independently
- All-keys sequence does NOT know about per-key handled calls
- `VerifyGet`/`VerifySet` counts ALL calls (per-key + all-keys + unconfigured)
- All-keys sequence `.Verify()` only counts calls that actually went through the all-keys path

This means if you configure `stub.Indexer["foo"].Returns(42)` and `stub.Indexer.Get(k => 1).ThenGet(k => 2)`, then:
- `collection["foo"]` hits per-key (does not advance all-keys sequence)
- `collection["bar"]` hits all-keys sequence (advances position)
- `stub.Indexer.VerifyGet(Called.Exactly(2))` passes (2 total calls)

---

## Architectural Verification

### Codebase Analysis

Files examined:

| File | What was learned |
|------|-----------------|
| `src/Generator/Model/Shared/UnifiedIndexerInterceptorModel.cs` | Current model uses `KeyType` (tuple for multi-param), `SingleKeyType`, `ParameterSignature`, `KeyExpression`, `ArgumentList` to handle flattening at the implementation level but tuples at the interceptor level |
| `src/Generator/Model/Shared/UnifiedIndexerContainerModel.cs` | Container model with `OfXxx` pattern -- has `KeyTypeFriendlyName` for disambiguation |
| `src/Generator/Model/Flat/FlatIndexerModel.cs` | Standalone model with `KeyType`, `KeyTypeFriendlyName`, `BaseName` for grouping |
| `src/Generator/Model/Flat/FlatIndexerGroup.cs` | Groups indexers by `BaseName` with container class name |
| `src/Generator/Model/Inline/InlineIndexerModel.cs` | Inline model -- parallel structure to FlatIndexerModel |
| `src/Generator/Model/Inline/InlineIndexerGroup.cs` | Inline grouping -- parallel to FlatIndexerGroup |
| `src/Generator/Renderer/Shared/IndexerInterceptorRenderer.cs` | ~945 lines. Renders interceptor class with Backing, Get(), Set(), InvokeGet/Set, sequences, verification, nested builder/sequence impls. This is the **primary file to rewrite** |
| `src/Generator/Renderer/FlatRenderer.cs` | Has `RenderIndexerContainerClass`, `BuildIndexerAccessMap` with OfXxx logic, `RenderIndexerImplementation`, dead `RenderIndexerInterceptorClass` at line 902 |
| `src/Generator/Renderer/InlineRenderer.cs` | Has parallel `RenderIndexerContainerClass` and container rendering |
| `src/Generator/Renderer/ClassRenderer.cs` | InlineClass indexer rendering -- uses `IsConfigured` for virtual fall-through, delegates to `IndexerInterceptorRenderer` |
| `src/Generator/Renderer/StandaloneClassRenderer.cs` | Same pattern as ClassRenderer for standalone class stubs |
| `src/Generator/Builder/FlatModelBuilder.cs` | `BuildIndexerModels`, indexer grouping by BaseName, container construction |
| `src/Generator/Builder/InlineModelBuilder.cs` | `BuildIndexerModel`, `GroupIndexers`, `BuildIndexerAccessMap` |
| `src/Generator/Builder/StandaloneClassModelBuilder.cs` | Class-based indexer building with `BuildIndexerModel`, `BuildImplIndexerModel` |
| `src/KnockOff/IIndexerCallBuilder.cs` | Library interfaces: `IIndexerGetBuilder<TKey, TValue>`, `IIndexerSetBuilder<TKey, TValue>` |
| `src/KnockOff/IIndexerSequence.cs` | Library interfaces: `IIndexerGetSequence<TKey, TValue>`, `IIndexerSetSequence<TKey, TValue>` |
| `src/KnockOff/IIndexerTracking.cs` | Library interfaces: `IIndexerGetTracking<TKey>`, `IIndexerSetTracking<TKey, TValue>` |
| `src/Design/Design.Stubs/Indexers/IndexerBasics.cs` | Design source of truth for current indexer API |
| `src/Design/Design.Stubs/Indexers/IndexerSequences.cs` | Design source of truth for indexer sequences |
| `src/Tests/KnockOffTests/IndexerTests.cs` | 178 lines, 9 tests using `Backing`, `Get`, `Set`, `LastGetKey`, `LastSetEntry` |
| `src/Tests/KnockOffTests/IndexerGapReproductionTests.cs` | 498 lines, tests for multi-param, init-only, params indexers |
| `src/Tests/KnockOffTests/InlineMultiIndexerTests.cs` | Tests for `OfString`/`OfInt32` container pattern |
| `src/Tests/KnockOffTests/RefReturnTests.cs` | `IRefReturnIndexerService` has `ref int this[int]` and `ref readonly int this[string]` -- both use separate interceptors via OfXxx today |
| `src/Tests/KnockOff.Documentation.Samples/IndexersSamples.cs` | Documentation samples with `OfString`/`OfInt32` |
| `src/Design/Design.Stubs/Indexers/IndexerRedesignAcceptance.cs` | **NEW** Failing acceptance criteria for new API -- 15 test methods covering patterns 1 and 5 |
| `src/Design/Design.Domain/Entities/ICollection.cs` | **MODIFIED** Added `IMultiIndexerCollection` interface |

### Scope Table

| Pattern | Affected | Notes |
|---------|----------|-------|
| Standalone (1) | Yes | FlatModelBuilder + FlatRenderer: indexer model changes, remove container/OfXxx, add per-key builder generation |
| Generic Standalone (2) | Yes | Same pipeline as Standalone -- type params pass through |
| Standalone Class (3) | Yes | StandaloneClassModelBuilder + StandaloneClassRenderer: indexer model changes, remove OfXxx |
| Generic Standalone Class (4) | Yes | Same pipeline as Standalone Class -- type params pass through |
| Inline Interface (5) | Yes | InlineModelBuilder + InlineRenderer: indexer model changes, remove container/OfXxx |
| Inline Class (6) | Yes | ClassModelBuilder + ClassRenderer: indexer model changes, remove OfXxx |
| Inline Delegate (7) | No | Delegates do not have indexers |
| Open Generic Interface (8) | Yes | Same inline pipeline with type params |
| Open Generic Class (9) | Yes | Same class pipeline with type params |

### Breaking Changes Assessment

**This is a breaking API change.** All existing indexer usage in user code will need updating.

| Breaking Change | Current API | New API | Migration |
|----------------|-------------|---------|-----------|
| Remove `Backing` dictionary | `stub.Indexer.Backing["key"] = value` | `stub.Indexer["key"].Returns(value)` | Search-and-replace pattern |
| Remove `OfXxx` container | `stub.Indexer.OfString.Backing["key"]` | `stub.Indexer["key"].Returns(value)` | Eliminate OfXxx path, use indexer directly |
| Remove `OfXxx` with Get/Set | `stub.Indexer.OfString.Get(...)` | `stub.Indexer.Get((string k) => ...)` | Callback overloads replace OfXxx |
| Flatten multi-param indexer accessor | `stub.Indexer.Backing[(1, 2)] = val` | `stub.Indexer[1, 2].Returns(val)` | Flatten tuple + use Returns |
| Remove `IndexerContainer` class | `public IndexerContainer Indexer` | `public IndexerInterceptor Indexer` | Transparent to most users |

**NOT a breaking change (unchanged from current):**
- Multi-param callback signatures use tuple TKey (same as today)
- `ThenGet(Func<TKey, TValue>)` signature (same as today)
- `LastGetKey` type for multi-param (still tuple, same as today)

**Impact analysis:** 126 occurrences of `.Backing`/`.OfXxx` across 18 files. 335 total indexer API usages across 31 files. All indexer-related tests will need updating.

### Diagnostic Requirements

- **KO2001** (new): Warning when interface has indexers with non-unique key types after flattening (should be impossible in valid C#, but defensive)
- Existing diagnostics unchanged

### Pattern Consistency

The new API follows KnockOff's existing patterns:
- `Returns(value)` is intentionally different from method `Return(callback)` -- see Concern #8 resolution
- Per-key builders are analogous to per-method argument matching
- Sequence chaining (`ThenReturns`) mirrors `Returns` naming (per-key level)
- All-keys `Get()`/`ThenGet()` unchanged -- same as today
- Verification methods unchanged: `VerifyGet`, `VerifySet`, `Verify`

### Edge Cases

1. **Ref return indexers -- single indexer:** Per-key builder's `Returns(value)` stores the value. `InvokeRefGet` writes the stored value to `_refReturnBacking` and the caller does `return ref _refReturnBacking`. Same as current behavior, just with per-key lookup first.

2. **Ref return indexers -- multi-indexer:** The interceptor generates one `_refReturnBacking_{FriendlyName}` field per key type. Each type-suffixed `InvokeRefGet_{FriendlyName}` writes to its own backing field. The implementation override references the correct field by type suffix. See Concern #3 resolution.

3. **Init-only indexers:** Per-key `Set()` configuration works -- the interceptor's `InvokeSet` is called regardless of init vs set keyword. The PerKeyBuilder generates only applicable methods based on `HasGetter`/`HasSetter`. See Concern #5 resolution.

4. **Params array indexers:** Flattened indexer accessor for `this[int a, params string[] b]` means the interceptor has `this[int a, string[] b]`. Per-key storage uses `(int, string[])` tuple as dictionary key -- reference equality problem means per-key `Returns()` will not match at runtime. **Migration guide warns about this.** Workaround: use all-keys callback. See Concern #9 resolution.

5. **Generic type parameters in indexer keys:** Open generic patterns like `IService<T>` with `this[T key]` -- the generated interceptor uses `T` as the key type. Per-key builder dictionary is `Dictionary<T, PerKeyBuilder>`.

6. **Value type keys and nullable tracking:** `LastGetKey` for value type keys uses nullable: `int?` for single-param, `(int, int)?` for multi-param. Unchanged from current behavior.

7. **Multi-indexer with overlapping callback types:** Cannot happen because C# requires unique parameter type lists for indexers.

### Test Strategy

1. **Update all existing indexer tests** to use new API (search-and-replace `Backing[key] = value` with `Indexer[key].Returns(value)`, remove OfXxx paths)
2. **New per-key tests:** Per-key configuration, per-key sequences, per-key with fallback
3. **New multi-indexer tests:** Indexer overloads by key type, callback overload resolution
4. **New multi-param flattened tests:** Flattened indexer accessor for 2, 3, 4 param indexers
5. **Regression tests:** Init-only, ref return, params array, generic indexers
6. **Design project updates:** IndexerRedesignAcceptance.cs already written as failing acceptance criteria

---

## Implementation Steps

### Phase 0: Pre-Work -- Model Changes and Dead Code Cleanup

**Goal:** Define the new model types and clean up obsolete code.

1. Create new model: `UnifiedIndexerInterceptorModel` v2 (or modify existing)
   - Remove: `SingleKeyType` (no longer needed for Backing dictionary)
   - Add: per-key builder fields, multi-indexer overload info
   - Keep: `ParameterSignature`, `ParameterTypes`, `KeyExpression`, `ArgumentList` for flattened params
   - Repurpose: `KeyTypeFriendlyName` for type-suffixed InvokeGet naming in multi-indexer

2. Remove `UnifiedIndexerContainerModel` (OfXxx container eliminated)

3. Remove `FlatIndexerGroup` and `InlineIndexerGroup` (container grouping eliminated)

4. Update `FlatIndexerModel` and `InlineIndexerModel`:
   - Remove: `BaseName` (used for OfXxx grouping only)
   - Add: fields needed for multi-indexer overload generation

5. **Remove dead code:**
   - `FlatRenderer.RenderIndexerInterceptorClass` (line 902) -- unused method
   - `FlatRenderer.BuildIndexerAccessMap` -- OfXxx access map logic
   - `FlatRenderer.RenderIndexerContainerClass` -- OfXxx container rendering
   - `InlineRenderer.RenderIndexerContainerClass` -- OfXxx container rendering
   - `InlineRenderer.GroupIndexers` / `BuildIndexerAccessMap` -- OfXxx grouping logic

### Phase 1: Library Interface Updates

**Goal:** Verify no library interface changes are needed.

1. Keep existing `IIndexerGetBuilder<TKey, TValue>`, `IIndexerSetBuilder<TKey, TValue>` -- used for all-keys callbacks
2. Keep existing `IIndexerGetSequence`, `IIndexerSetSequence` -- used for all-keys sequences
3. Keep existing `IIndexerGetTracking`, `IIndexerSetTracking` -- used for tracking
4. No new library interfaces needed (per-key builders are generated)

### Phase 2: IndexerInterceptorRenderer Rewrite

**Goal:** Rewrite the shared renderer for the new interceptor architecture.

This is the largest single change. The current `IndexerInterceptorRenderer.cs` (~945 lines) is rewritten to generate:

1. **Per-key builder nested class** with Returns, ThenReturns, Get, Set (accessor-dependent)
2. **Per-key storage** (Dictionary per key type)
3. **Indexer accessor(s)** returning per-key builders (flattened for multi-param)
4. **All-keys Get/Set methods** (overloaded for multi-indexer)
5. **InvokeGet/InvokeSet** with new priority chain (per-key first, checking `HasGetConfig`)
6. **Type-suffixed invoke methods** for multi-indexer: `InvokeGet_{FriendlyName}`, `InvokeRefGet_{FriendlyName}`
7. **Type-suffixed ref return backing fields** for multi-indexer: `_refReturnBacking_{FriendlyName}`
8. **IsConfigured** that checks per-key builder config state (not existence) + all-keys state
9. **Tracking** (per-type for multi-indexer with type-suffixed names)
10. **Verification** (combined across all key types)
11. **Sequences** (all-keys level, unchanged)
12. **Reset** (clear per-key builders + all-keys state)

### Phase 3: Builder Updates

**Goal:** Update all 4 builder pipelines for the new model.

1. **FlatModelBuilder:**
   - Remove indexer grouping / BaseName / container logic
   - Collect all indexers into a single list on the generation unit
   - For multi-indexer: all indexers share one interceptor class name
   - Update `BuildIndexerModels` to produce new model shape

2. **InlineModelBuilder:**
   - Same changes as FlatModelBuilder
   - Remove `GroupIndexers`, `BuildIndexerAccessMap` (OfXxx logic)
   - All indexers associated with one interceptor

3. **StandaloneClassModelBuilder:**
   - Same pattern -- collect indexers, single interceptor
   - Update `BuildIndexerModel` and `BuildImplIndexerModel`

4. **ClassModelBuilder:**
   - Same pattern for inline class stubs

### Phase 4: Renderer Updates (Pipeline-Specific)

**Goal:** Update each renderer to use the new interceptor architecture.

1. **FlatRenderer:**
   - Remove `RenderIndexerContainerClass`, `BuildIndexerAccessMap`, dead `RenderIndexerInterceptorClass`
   - Update `RenderIndexerImplementation` to call type-suffixed InvokeGet/InvokeSet for multi-indexer
   - Update interceptor property generation (one property, not container)
   - Update Verify/VerifyAll to include indexer interceptor

2. **InlineRenderer:**
   - Remove `RenderIndexerContainerClass`, `GroupIndexers`, `BuildIndexerAccessMap`
   - Update implementation rendering
   - Update interceptor property generation

3. **StandaloneClassRenderer:**
   - Update indexer override rendering for type-suffixed Invoke calls in multi-indexer
   - Update ref return backing field references to use type-suffixed names

4. **ClassRenderer:**
   - Same as StandaloneClassRenderer
   - Update `IsConfigured` check pattern for new semantics

### Phase 5: Test Updates

**Goal:** Update all existing tests and add new tests.

1. **Update existing tests:**
   - `src/Tests/KnockOffTests/IndexerTests.cs` -- replace Backing with Returns, update all assertions
   - `src/Tests/KnockOffTests/IndexerGapReproductionTests.cs` -- use Returns for multi-param
   - `src/Tests/KnockOffTests/InlineMultiIndexerTests.cs` -- remove OfXxx, use indexer overloads
   - `src/Tests/KnockOffTests/SequencingTests.cs` -- update indexer sequence tests
   - `src/Tests/KnockOffTests/RefReturnTests.cs` -- remove OfXxx, use indexer overloads
   - `src/Tests/KnockOffTests/BclInterfaceTests.cs` -- update BCL indexer tests
   - `src/Tests/KnockOffTests/BclStandaloneTests.cs` -- update standalone BCL tests
   - `src/Tests/KnockOffTests/ClassIndexerVerificationTests.cs` -- update class indexer verification
   - `src/Tests/KnockOffTests/StandaloneClassStubTests.cs` -- update class stub indexer tests
   - `src/Tests/KnockOffTests/NeatooTests.cs` -- update Neatoo-specific indexer tests
   - `src/Tests/KnockOffTests/ProtectedMemberTests.cs` -- update protected indexer tests
   - `src/Tests/KnockOffTests/BuilderElevationTests.cs` -- update builder elevation tests
   - `src/Tests/KnockOff.Documentation.Samples/IndexersSamples.cs` -- full rewrite
   - `src/Tests/KnockOff.Documentation.Samples/InterceptorApiSamples.cs` -- update indexer samples
   - `src/Tests/KnockOff.Documentation.Samples/ApiConsistencyMatrixSamples.cs` -- update
   - `src/Tests/KnockOff.Documentation.Samples/SkillContentSamples.cs` -- update
   - `src/Tests/KnockOff.Documentation.Samples/ReadmeComparisonSamples.cs` -- update
   - `src/Benchmarks/KnockOff.Benchmarks/Benchmarks/IndexerBenchmarks.cs` -- update

2. **Add new tests:**
   - Per-key configuration (Returns, Get, Set)
   - Per-key sequences (ThenReturns)
   - Per-key with all-keys fallback
   - Multi-indexer overload resolution
   - Multi-param flattened indexer accessor (2, 3, 4 params)

### Phase 6: Design Project Updates

**Goal:** Make the failing acceptance criteria compile.

1. `src/Design/Design.Stubs/Indexers/IndexerRedesignAcceptance.cs` must compile
2. Rewrite `src/Design/Design.Stubs/Indexers/IndexerBasics.cs` with new API
3. Rewrite `src/Design/Design.Stubs/Indexers/IndexerSequences.cs` with new API
4. Rewrite `src/Design/Design.Tests/IndexerTests/IndexerBasicsTests.cs`
5. Rewrite `src/Design/Design.Tests/IndexerTests/IndexerSequenceTests.cs`

### Phase 7: Documentation and Skill Updates

1. Update `docs/guides/api-consistency-matrix.md` for new indexer API
2. Update `skills/knockoff/` skill content
3. Write release notes for the breaking change
4. Include params array known limitation in migration guide

---

## Acceptance Criteria

1. All nine applicable patterns generate correct indexer interceptor code (delegate pattern excluded)
2. `stub.Indexer[key].Returns(value)` works for single-param, multi-param, and multi-type indexers
3. `stub.Indexer.Get(callback)` works with tuple TKey for multi-param indexers (unchanged from today)
4. Multi-indexer interfaces use indexer overloads instead of OfXxx container
5. All existing tests updated and passing
6. Design.Stubs compiles with new API (IndexerRedesignAcceptance.cs compiles)
7. Design.Tests pass with new API
8. No `Backing` dictionary in generated code
9. No `OfXxx` pattern in generated code
10. Per-key sequences work independently per key
11. Multi-indexer ref return uses type-suffixed `_refReturnBacking` fields
12. `IsConfigured` correctly handles lazily-created but unconfigured per-key builders
13. PerKeyBuilder generates only applicable methods based on indexer accessors

---

## Dependencies

- No external dependencies
- Internal dependency: Phase 2 (renderer rewrite) must complete before Phases 3-4 can fully integrate
- Phase 0 (model changes + dead code cleanup) must complete before Phase 2
- Design.Stubs acceptance criteria already written (failing) -- developer's job is to make them compile

---

## Risks

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| C# overload ambiguity for multi-indexer Get/Set callbacks | Low | High | C# requires unique parameter types for indexers; callback delegate types will differ |
| Per-key builder proliferation in generated code | Medium | Medium | Keep per-key builders minimal; lazy creation avoids unused allocations; only configured builders count as "configured" |
| Multi-param indexer accessor with >16 params | Very Low | Medium | C# indexers rarely exceed 4 params; >16 is technically possible but unrealistic |
| Breaking change migration burden for users | High | High | Provide clear migration guide; changes are mostly mechanical search-replace |
| Interaction between per-key and all-keys sequences | Medium | Medium | Clear priority chain; per-key always wins; each tracks independently |
| Ref return multi-indexer with type-suffixed backing fields | Low | Medium | One backing field per key type; implementation references correct field by suffix |
| Generated code size increase | Medium | Low | Per-key builder is small; multi-indexer interceptor larger than OfXxx but simpler for users |
| Params array indexers with per-key Returns | Medium | Low | Document as known limitation; all-keys callback works |

---

## Design Project Verification

**Status:** Executed. Failing acceptance criteria written and verified.

**Files created/modified:**
- `src/Design/Design.Stubs/Indexers/IndexerRedesignAcceptance.cs` -- 15 acceptance criteria test methods
- `src/Design/Design.Domain/Entities/ICollection.cs` -- Added `IMultiIndexerCollection` interface

**Build output (Design.Stubs):** 57 errors across 3 target frameworks (net8.0, net9.0, net10.0). Key errors:

| Error | Count | Meaning |
|-------|-------|---------|
| CS0021: Cannot apply indexing with `[]` | ~45 | Interceptor class needs C# indexer accessor for per-key builders |
| CS1061: No `Get`/`Set` on `IndexerContainer` | ~9 | Multi-indexer currently uses OfXxx container, needs direct Get/Set on interceptor |

All errors are expected and confirm the new API does not yet exist.

### Verification Table

| Pattern | Feature | Verification Status | Evidence |
|---------|---------|-------------------|----------|
| Inline Interface (5) | Per-key Returns | Needs Implementation | `IndexerRedesignAcceptance.cs:36` -- CS0021 |
| Inline Interface (5) | Per-key Get/Set | Needs Implementation | `IndexerRedesignAcceptance.cs:54,69` -- CS0021 |
| Inline Interface (5) | Per-key sequences | Needs Implementation | `IndexerRedesignAcceptance.cs:84` -- CS0021 |
| Inline Interface (5) | All-keys callback | Needs Implementation (multi-indexer) | `IndexerRedesignAcceptance.cs:188` -- CS1061 |
| Inline Interface (5) | Multi-param flattened | Needs Implementation | `IndexerRedesignAcceptance.cs:139` -- CS0021 |
| Inline Interface (5) | Multi-param tuple callback | Needs Implementation | `IndexerRedesignAcceptance.cs:151` -- depends on AC-7 |
| Inline Interface (5) | Multi-indexer overloads | Needs Implementation | `IndexerRedesignAcceptance.cs:170` -- CS0021 |
| Inline Interface (5) | Multi-indexer callback overloads | Needs Implementation | `IndexerRedesignAcceptance.cs:188` -- CS1061 |
| Inline Interface (5) | Init-only per-key Returns | Needs Implementation | `IndexerRedesignAcceptance.cs:207` -- CS0021 |
| Inline Interface (5) | Tracking | Needs Implementation | `IndexerRedesignAcceptance.cs:220` -- CS0021 |
| Inline Interface (5) | All-keys sequences | Needs Implementation | `IndexerRedesignAcceptance.cs:234` -- depends on AC-1 |
| Standalone (1) | Per-key Returns | Needs Implementation | `IndexerRedesignAcceptance.cs:306` -- CS0021 |
| Standalone (1) | Multi-param flattened | Needs Implementation | `IndexerRedesignAcceptance.cs:321` -- CS0021 |
| Standalone (1) | All-keys callback | Needs Implementation | `IndexerRedesignAcceptance.cs:333` -- depends on AC-S1 |
| Generic Standalone (2) | Per-key Returns | Needs Implementation | Same pipeline as Standalone -- implicit |
| Standalone Class (3) | Per-key Returns | Needs Implementation | No explicit Design.Stubs code yet -- tested via KnockOffTests |
| Inline Class (6) | Per-key Returns | Needs Implementation | No explicit Design.Stubs code yet -- tested via KnockOffTests |
| Open Generic Interface (8) | Per-key Returns | Needs Implementation | Same inline pipeline -- tested via KnockOffTests |
| Open Generic Class (9) | Per-key Returns | Needs Implementation | Same class pipeline -- tested via KnockOffTests |

---

## Architectural Verification Checklist

- [x] All nine patterns analyzed (Standalone, Generic Standalone, Standalone Class, Generic Standalone Class, Inline Interface, Inline Class, Inline Delegate, Open Generic Interface, Open Generic Class)
- [x] Design.Stubs compilation verification for key pattern+feature claims
- [x] Breaking changes assessment completed
- [x] Pattern consistency verified
- [x] Diagnostic requirements identified
- [x] Test strategy defined
- [x] Edge cases documented (including ref return multi-indexer, IsConfigured semantics, params arrays)
- [x] Codebase deep-dive completed (files documented above)
- [x] All open questions resolved
- [x] All developer concerns addressed

---

## Developer Review

### First Review (Concerns Raised)

**Status:** Concerns Raised
**Reviewed:** 2026-02-09

### My Understanding of This Plan

**Core Change:** Replace the indexer API's `Backing` dictionary and `OfXxx` container pattern with per-key builders via indexer syntax (`stub.Indexer[key].Returns(value)`), callback overloads for multi-indexer disambiguation, and flattened params for multi-parameter indexers.

**User-Facing API:** `stub.Indexer[key].Returns(value)` for per-key config; `stub.Indexer.Get(callback)` for all-keys fallback; indexer overloads on the interceptor replace `OfXxx` properties; flattened params replace tuple keys.

**Internal Changes:** Rewrite `IndexerInterceptorRenderer` (~945 lines); modify all 4 builder pipelines; remove container/group models; remove `OfXxx` rendering; generate per-key builder nested classes; update priority chain in InvokeGet/InvokeSet.

**Patterns Affected:** All except Inline Delegate (pattern 7) -- 8 of 9 patterns.

### Codebase Investigation

**Files Examined:**
- `src/Generator/Renderer/Shared/IndexerInterceptorRenderer.cs` -- shared renderer used by all 4 pipelines; generates interceptor with Backing, Get/Set, InvokeGet/InvokeSet, sequences, verification, nested builder/sequence impls
- `src/Generator/Model/Shared/UnifiedIndexerInterceptorModel.cs` -- current model with `KeyType`, `SingleKeyType`, `ParameterSignature`
- `src/Generator/Model/Shared/UnifiedIndexerContainerModel.cs` -- container model with `OfXxx` pattern
- `src/Generator/Model/Flat/FlatIndexerModel.cs`, `FlatIndexerGroup.cs` -- flat pipeline indexer models
- `src/Generator/Model/Inline/InlineIndexerModel.cs`, `InlineIndexerGroup.cs` -- inline pipeline indexer models
- `src/KnockOff/IIndexerCallBuilder.cs` -- `IIndexerGetBuilder<TKey, TValue>`, `IIndexerSetBuilder<TKey, TValue>`
- `src/KnockOff/IIndexerSequence.cs` -- `IIndexerGetSequence<TKey, TValue>`, `IIndexerSetSequence<TKey, TValue>`
- `src/KnockOff/IIndexerTracking.cs` -- `IIndexerGetTracking<TKey>`, `IIndexerSetTracking<TKey, TValue>`
- `src/Design/Design.Stubs/Indexers/IndexerBasics.cs` -- current API examples (Backing, Get, Set, tracking)
- `src/Design/Design.Stubs/Indexers/IndexerSequences.cs` -- current sequence examples
- `src/Tests/KnockOffTests/IndexerTests.cs` -- 9 tests using Backing/Get/Set
- `src/Tests/KnockOffTests/IndexerGapReproductionTests.cs` -- 18 tests for multi-param, init-only, params
- `src/Tests/KnockOffTests/InlineMultiIndexerTests.cs` -- 8 tests for OfXxx container
- `src/Tests/KnockOffTests/RefReturnTests.cs` -- ref return indexer tests using OfXxx (`stub.Indexer.OfInt32`, `stub.Indexer.OfString`)
- `src/Generator/Renderer/FlatRenderer.cs` -- container rendering, access map, property generation, dead code at line 902
- `src/Generator/Renderer/ClassRenderer.cs` -- class indexer rendering using `IsConfigured` for virtual fall-through
- `src/Generator/Renderer/StandaloneClassRenderer.cs` -- standalone class indexer rendering

**Design.Stubs Verification:**
- The architect did NOT provide compilable Design.Stubs code. The verification section states "Not yet executed" and all entries say "Needs Implementation" with no compiler error evidence.

**Discrepancies Found:**
1. Plan Section 4 shows multi-indexer with `InvokeGetString`/`InvokeGetInt32` methods, but ClassRenderer and StandaloneClassRenderer use `stub.{indexer.IndexerName}.InvokeGet(strict, args)` -- a single InvokeGet method per interceptor. Plan does not explain how class renderers route to type-suffixed invocations.
2. FlatRenderer has dead code `RenderIndexerInterceptorClass` at line 902 (declared but never called). Plan does not mention cleaning this up.
3. Library interface `IIndexerGetSequence<TKey, TValue>.ThenGet(Func<TKey, TValue>)` expects TKey as the tuple type for multi-param indexers, but the plan proposes flattened params for all-keys callbacks. Plan acknowledges this inconsistency (end of Section 5) but provides two alternatives without a final decision.

### Concerns

**1. [Blocking] Design.Stubs verification not performed**

The plan's verification section explicitly states "Not yet executed." Per the verification protocol, the architect must provide compilable Design.Stubs code for every "Yes" (current features that work) and failing Design.Stubs code for every "Needs Implementation" (new features). None of this was done.

Without this evidence, I cannot confirm that the proposed API surface is actually achievable. For example, the claim that C# indexer overloads on the interceptor class will resolve correctly for multi-indexer interfaces needs to be tested with an actual stub.

**Question:** Can the architect write the failing Design.Stubs acceptance criteria (Phase 5 of the plan) before implementation begins? This would validate the API design compiles correctly with manual class stubs.

**Architect Response:** DONE. See "Architect Responses to Developer Concerns -- Concern 1" above.

**2. [Blocking] Unresolved design decision for multi-param indexer library interfaces**

Section 5 ("Library Interface Changes") presents two alternatives for multi-param indexers:
- Alternative A: Keep `IIndexerGetBuilder<(int,int), double>` but add extra flattened overloads
- Alternative B (recommended): Generate standalone builder/sequence classes entirely without library interface dependency

The plan labels Alternative B as "recommended" but never commits to it. This matters critically because:
- If Alternative A: the `ThenGet(Func<(int,int), double>)` method on the sequence interface uses tuple keys, but the all-keys `Get(Func<int, int, double>)` uses flattened params. Users would face inconsistent callback signatures between `Get(...)` and `.ThenGet(...)`.
- If Alternative B: the generated classes cannot implement `IIndexerGetBuilder<TKey, TValue>` or `IIndexerGetSequence<TKey, TValue>`, which means the return type of `Get(...)` changes. What replaces the library interfaces?

**Question:** Which alternative is chosen? If Alternative B, what are the concrete return types for `Get(...)` and `Set(...)` for multi-param indexers? Are they generated classes? What are they named?

**Architect Response:** COMMITTED -- neither A nor B. Library interfaces keep tuple TKey. No flattening of callbacks. See "Architect Responses to Developer Concerns -- Concern 2" above.

**3. [Concern] Multi-indexer interceptor with different ref-return kinds**

The test `IRefReturnIndexerService` has:
- `ref int this[int index] { get; }` -- ref return
- `ref readonly int this[string key] { get; }` -- ref readonly return

Currently these use separate interceptor classes via the OfXxx container (`stub.Indexer.OfInt32`, `stub.Indexer.OfString`), each with its own `_refReturnBacking` field.

The plan proposes merging all indexers into a single interceptor class with multiple indexer overloads. But for ref-return indexers, the interceptor needs a `_refReturnBacking` field per key type (because `InvokeRefGet` writes to `_refReturnBacking` and the caller then does `return ref _refReturnBacking`). If there's only one `_refReturnBacking` field, concurrent access to different indexer key types would corrupt the backing.

The plan mentions ref return indexers in Edge Case 1 but only addresses the single-indexer case. It does not address how a multi-indexer interceptor handles multiple `_refReturnBacking` fields (one per key type) or how the generated code for `return ref _stub.Indexer._refReturnBacking` disambiguates which backing field to use.

**Question:** For multi-indexer interfaces with different ref-return characteristics per key type, how many `_refReturnBacking` fields does the interceptor have, and how does the caller know which one to reference?

**Architect Response:** One per key type with type-suffixed names. See "Architect Responses to Developer Concerns -- Concern 3" above.

**4. [Concern] `IsConfigured` semantics change for class stubs**

`ClassRenderer.cs` and `StandaloneClassRenderer.cs` use `_stub.{indexer.IndexerName}.IsConfigured` to decide whether to fall through to the base class implementation for virtual members:

```csharp
if (_stub.{indexer.IndexerName}.IsConfigured) return _stub.{indexer.IndexerName}.InvokeGet(...)
// else fall through to base
```

Currently, `IsConfigured` checks `Backing.Count > 0 || _get != null || _set != null || sequence count > 0`.

In the new design, `IsConfigured` must also check per-key builder storage (`_perKeyBuilders.Count > 0`). The plan does not explicitly discuss `IsConfigured` semantics for the new interceptor design. If per-key builders are always lazily created (as the plan says: `_perKeyBuilders[key] = new PerKeyBuilder()` on access), then merely accessing `stub.Indexer[key]` (without calling `.Returns(...)`) would create a per-key builder and set `IsConfigured = true`, which would prevent base class fall-through for unconfigured virtual indexers.

**Question:** How does `IsConfigured` work with lazy per-key builder creation? Does accessing `stub.Indexer[key]` without calling `.Returns()` or `.Get()` count as "configured"?

**Architect Response:** No. Only configured per-key builders count. See "Architect Responses to Developer Concerns -- Concern 4" above.

**5. [Concern] Per-key builder for set-only indexers (init-only)**

For init-only indexers (`{ get; init; }` or `{ init; }` only), the per-key builder needs a `Set()` method. The plan's `PerKeyBuilder` class shows `Returns(TValue)`, `Get(Func<TValue>)`, and `Set(Action<TValue>)`. But for a set-only indexer (no getter), `Returns()` and `Get()` make no sense. Is the PerKeyBuilder generated with only applicable methods?

Additionally, for `{ init; }` only indexers (no getter), `stub.Indexer[key]` itself is accessed via a get-only indexer on the interceptor. This is fine since the interceptor's indexer returns the builder, not the value. But this should be explicitly stated.

**Question:** Is the PerKeyBuilder generated with only the methods applicable to the indexer's accessors (e.g., Set-only for init-only indexers)?

**Architect Response:** Yes. See "Architect Responses to Developer Concerns -- Concern 5" above.

**6. [Concern] Open Questions are unresolved**

The plan has 4 open questions (Section: Open Questions) that are all unresolved:
1. Per-key tracking (`VerifyGet` on per-key builder) -- affects generated code size
2. Type-suffixed InvokeGet naming convention -- affects ClassRenderer/StandaloneClassRenderer integration
3. Per-key `ThenDefault()` support -- affects per-key builder class shape
4. Sequence interaction verification semantics -- affects verification logic

At minimum, questions 1 and 2 need answers before implementation. Question 2 especially, because the ClassRenderer currently generates `_stub.Indexer.InvokeGet(strict, key)` -- if InvokeGet becomes type-suffixed (`InvokeGetString`, `InvokeGetInt32`), the ClassRenderer needs to know the suffix convention.

**Question:** Can these open questions be resolved before implementation begins?

**Architect Response:** All 4 resolved. See "Resolved Open Questions" section above.

**7. [Minor] Dead code and plan Phase 0 cleanup scope**

The plan's Phase 0 says "Remove `UnifiedIndexerContainerModel`" and "Remove `FlatIndexerGroup` and `InlineIndexerGroup`". But it does not mention:
- Removing the dead `FlatRenderer.RenderIndexerInterceptorClass` method (line 902)
- Removing `FlatRenderer.BuildIndexerAccessMap` (line 439) and all OfXxx access map logic
- Removing `InlineRenderer.RenderIndexerContainerClass` and its associated grouping logic

These are implicit but should be explicit to avoid implementation ambiguity.

**Architect Response:** Made explicit. See "Architect Responses to Developer Concerns -- Concern 7" above.

**8. [Minor] Plan mentions "ThenReturns" but this API name does not exist in the codebase**

The plan uses `Returns()` and `ThenReturns()` for per-key builder sequences. The existing method API uses `Return()` and `ThenReturn()` (without the 's'). For consistency with the existing codebase, should these be `Return()`/`ThenReturn()` or deliberately `Returns()`/`ThenReturns()`?

The plan explicitly calls them `Returns`/`ThenReturns` in multiple places. If this is intentional (to distinguish from method `Return`), it should be stated. If not, it should be `Return`/`ThenReturn`.

**Question:** Is `Returns`/`ThenReturns` intentional divergence from the method API's `Return`/`ThenReturn`, or should they match?

**Architect Response:** Intentional. See "Architect Responses to Developer Concerns -- Concern 8" above.

**9. [Concern] Params array indexers with per-key builders**

Edge Case 3 acknowledges that `(int, string[])` tuple keys have reference equality problems with dictionary lookup. The plan says this is a "known limitation." But in the new design, per-key builders are stored in `Dictionary<(int, string[]), PerKeyBuilder>`, which has the same reference equality problem. Writing `stub.Indexer[1, new[] { "b" }].Returns(42)` and then accessing `svc[1, "b"]` would NOT match because the runtime creates a different `string[]` instance.

The plan acknowledges this but does not offer a mitigation for per-key config (the old Backing had the same problem, but at least callbacks worked). With per-key builders, users might expect `stub.Indexer[1, "b"].Returns(42)` to work.

This is not a blocking concern since the workaround (using all-keys callbacks) still exists, but the migration guide should explicitly call out that per-key `Returns()` does NOT work for params indexers.

**Question:** Should the migration guide explicitly warn that per-key `Returns()` does not work with `params` array indexers?

**Architect Response:** Yes. See "Architect Responses to Developer Concerns -- Concern 9" above.

### What Looks Good

- The overall API design is significantly more intuitive than the current Backing/OfXxx pattern
- The priority chain (per-key > all-keys > source > strict > default) is clear and well-documented
- The codebase analysis is thorough -- 18 files examined with accurate descriptions
- The scope table correctly identifies all 8 affected patterns
- The breaking changes assessment is comprehensive with clear migration mappings
- The phasing is logical: model first, then library, then renderer, then builders, then renderers, then design, then tests
- Edge cases for init-only, ref return, and params indexers are identified (though some need more design work)
- The test strategy covers all the right categories

### Recommendation

Send back to architect to address concerns before implementation. The blocking concerns (1, 2) must be resolved. Concerns 3, 4, and 6 need design decisions documented. The remaining concerns are minor but should be addressed for implementation clarity.

---

### Second Review (Approved)

**Status:** Approved
**Reviewed:** 2026-02-09

#### Verification of Architect's Responses

All 9 original concerns have been addressed. Here is my assessment of each response:

**Concern 1 (Design.Stubs verification):** SATISFIED. The architect wrote 15 acceptance criteria methods in `src/Design/Design.Stubs/Indexers/IndexerRedesignAcceptance.cs` covering inline interface (pattern 5) and standalone (pattern 1). I independently ran `dotnet build src/Design/Design.Stubs` and confirmed 102 compile errors across 3 target frameworks (17 unique source locations x 3 frameworks x ~2 errors per location on some lines). The plan says "57 errors" which is an inaccurate count, but the substance is correct -- all new-API lines fail with the expected CS0021 and CS1061 errors. The errors on multi-indexer lines (170, 171, 188-190) correctly target `IndexerContainer` (the current OfXxx container type), confirming the multi-indexer redesign is needed.

**Concern 2 (Multi-param decision):** SATISFIED. The committed decision is clear: library interfaces keep tuple TKey; ONLY the interceptor's C# indexer accessor is flattened. This eliminates the callback signature inconsistency I raised. The flattening scope table in the plan is unambiguous. I verified that the acceptance file's AC-8 (line 151-155) uses tuple callbacks and compiles today, confirming the approach is viable.

**Concern 3 (Ref-return multi-indexer):** SATISFIED. One `_refReturnBacking_{FriendlyName}` field per key type with type-suffixed `InvokeRefGet_{FriendlyName}` methods. The generated override code references the correct field. This is consistent with the type-suffixed InvokeGet convention from Q2.

**Concern 4 (IsConfigured semantics):** SATISFIED. The plan now explicitly states that `IsConfigured` checks `_perKeyBuilders.Values.Any(b => b.HasGetConfig || b.HasSetConfig)` -- not just builder existence. This means `stub.Indexer["foo"]` alone does NOT prevent base class fall-through. The `HasGetConfig` and `HasSetConfig` properties on the per-key builder are well-defined.

**Concern 5 (Set-only per-key builder):** SATISFIED. The table mapping accessor combinations to generated PerKeyBuilder methods is clear. The explicit note about the interceptor's indexer always being a get-only C# indexer (returning the builder) is correct and important.

**Concern 6 (Open questions):** SATISFIED. All 4 questions resolved:
- Q1: No per-key tracking in V1. Reasonable deferral.
- Q2: Underscore separator (`InvokeGet_String`). Clear convention.
- Q3: No ThenDefault on per-key. Repeat last value. Simple.
- Q4: Per-key and all-keys sequences independent. VerifyGet counts all calls. Clear.

**Concern 7 (Dead code):** SATISFIED. Phase 0 now explicitly lists all dead code to remove, matching what I found in the codebase (`FlatRenderer.RenderIndexerInterceptorClass` at line 902, `BuildIndexerAccessMap`, container rendering methods, container models).

**Concern 8 (Returns naming):** SATISFIED. The intentional divergence is justified -- `Returns(value)` reads as declarative English on a per-key builder and is consistent with Moq/NSubstitute precedent. Different from method `Return(callback)` which is imperative.

**Concern 9 (Params array):** SATISFIED. Migration guide will warn. Known limitation documented. Same limitation existed with Backing dictionary.

#### Design.Stubs Verification (Second Pass)

**Files Examined:**
- `src/Design/Design.Stubs/Indexers/IndexerRedesignAcceptance.cs` -- 15 acceptance criteria covering: per-key Returns (AC-1), per-key Get (AC-2), per-key Set (AC-3), per-key sequences (AC-4), all-keys callback (AC-5), per-key with fallback (AC-6), multi-param flattened (AC-7), multi-param tuple callback (AC-8), multi-indexer overloads (AC-9), multi-indexer callback overloads (AC-10), init-only per-key (AC-11), tracking (AC-12), all-keys sequences (AC-13), negative tests for Backing (AC-14) and OfXxx (AC-15) as comments
- `src/Design/Design.Domain/Entities/ICollection.cs` -- Added `IMultiIndexerCollection` with `string this[string] { get; set; }` and `int this[int] { get; }` -- good test case for mixed accessor multi-indexer

**Build verification:**
- `dotnet build src/Design/Design.Stubs` produces 102 errors (all from `IndexerRedesignAcceptance.cs`)
- Error categories: CS0021 (indexer not available on interceptor types) and CS1061 (no Get/Set on IndexerContainer)
- AC-5 (line 103-104), AC-8 (line 151-155), and AC-13 (line 245-247) compile successfully today -- correct, these exercise existing functionality
- Standalone stubs (AC-S1, AC-S2, AC-S3) correctly fail -- confirms both pipelines need the same changes

**Verification table cross-check:**
- Plan line references match the actual file (spot-checked AC-1:36, AC-7:139, AC-9:170, AC-10:188, AC-S1:306, AC-S2:321)
- Error types match plan claims (CS0021 for indexer access, CS1061 for missing Get/Set on container)

#### Multi-Param Consistency Check

The committed approach is consistent throughout the plan:
- Flattening scope table (line 31-41): Only interceptor indexer is flattened
- Section 4 multi-param interceptor (line 251-281): Indexer has `this[int row, int col]`, storage uses `Dictionary<(int, int), PerKeyBuilder>`, callbacks use tuple
- Section 5 (line 286-296): No library interface changes
- AC-7 (acceptance file line 139): `stub.Indexer[1, 2].Returns(3.14)` -- flattened
- AC-8 (acceptance file line 151-155): `stub.Indexer.Get(((int row, int col) key) => ...)` -- tuple callback
- No contradictions found.

#### Structured Question Checklist (Second Pass)

**Completeness:**
- [x] All nine patterns addressed (7 is N/A for delegates, 8 checked)
- [x] Null/empty/default: covered via `HasGetConfig`/`HasSetConfig` checks
- [x] Generic type parameters: Edge Case 5 addresses open generics with `Dictionary<T, PerKeyBuilder>`
- [x] Nested types / inherited members: not directly affected (indexer redesign is about interceptor shape, not member discovery)
- [x] Interaction with existing features: sequences, verification, IsConfigured all addressed

**Correctness:**
- [x] Generated code examples compile? Yes for existing features (AC-5, AC-8, AC-13). New features correctly fail.
- [x] Implementation consistent with existing patterns? Yes -- interceptor class pattern is preserved
- [x] Model/builder/renderer responsibilities correctly assigned? Yes
- [x] No breaking changes to library interfaces -- confirmed no changes needed

**Clarity:**
- [x] Could I implement this without asking any clarifying questions? YES -- the plan is now specific enough
- [x] No ambiguous requirements remaining
- [x] Edge cases explicitly handled
- [x] Test strategy specific enough to write tests from

**Risk:**
- [x] What could go wrong: largest risk is the Phase 2 rewrite of IndexerInterceptorRenderer (~945 lines)
- [x] Existing tests: ALL indexer tests will change (this is expected for a breaking API change)
- [x] Performance: per-key builder dictionary adds allocation per key, but lazy creation minimizes cost
- [x] Backward compatibility: intentional breaking change, migration guide planned

#### Devil's Advocate Analysis (Second Pass)

**Edge cases NOT explicitly covered but acceptable:**
1. Empty PerKeyBuilder sequence (0 items) -- what does `Returns(1).ThenReturns(...)` do if `Returns` is the first call? Answer is clear from the code: `Returns` sets a value, `ThenReturns` elevates to a sequence. This is implicitly handled.
2. Thread safety of per-key builder dictionary -- not covered, but KnockOff stubs are not thread-safe in general. Consistent with existing behavior.
3. What if someone calls `stub.Indexer["foo"].Returns(1).Returns(2)` (two Returns calls)? The second call would overwrite the first. Not documented but reasonable behavior. Not a plan gap.

**Ways this could break existing functionality:**
1. ALL existing indexer tests will fail (expected -- this is a breaking API change). The plan correctly lists 18+ files to update.

**Ways users could misunderstand the API:**
1. Users might try `stub.Indexer["foo"] = 42` (direct assignment) instead of `stub.Indexer["foo"].Returns(42)`. The indexer returns a PerKeyBuilder, not the value, so assignment would be a type mismatch compile error. This is clear but could benefit from documentation.

All devil's advocate items are minor and do not block implementation.

#### Why This Plan Is Ready for Implementation

This plan is exceptionally thorough after the architect's revisions:
1. All 9 original concerns addressed with specific, verifiable answers
2. Design.Stubs acceptance criteria exist and produce the expected compile errors
3. The multi-param flattening scope is explicitly committed and consistent throughout
4. All 4 open questions resolved with clear decisions
5. Dead code cleanup scope is explicit
6. The `IsConfigured` semantics are precisely defined with `HasGetConfig`/`HasSetConfig`
7. The PerKeyBuilder shape varies by accessor combination, explicitly documented in a table
8. Type-suffixed naming convention is committed: `InvokeGet_{FriendlyName}`, `_refReturnBacking_{FriendlyName}`
9. Sequence interaction semantics are defined: per-key and all-keys are independent
10. The priority chain is clear: per-key > all-keys sequence > all-keys callback > source > strict > default

**Files examined in second review:** 15 files across Generator/Renderer, Generator/Model, Generator/Builder, KnockOff library, Design.Stubs, Design.Domain
**Questions checked:** 16 of 16
**Devil's advocate items:** 5 generated, all already addressed or acceptably minor

---

## Implementation Contract

**Created:** 2026-02-09
**Approved by:** knockoff-developer

### Design.Stubs Acceptance Criteria

These are the failing Design.Stubs files left by the architect. Implementation is done when they all compile.

- [ ] `src/Design/Design.Stubs/Indexers/IndexerRedesignAcceptance.cs:36` -- AC-1: Per-key Returns (inline interface, CS0021)
- [ ] `src/Design/Design.Stubs/Indexers/IndexerRedesignAcceptance.cs:54` -- AC-2: Per-key Get callback (inline interface, CS0021)
- [ ] `src/Design/Design.Stubs/Indexers/IndexerRedesignAcceptance.cs:69` -- AC-3: Per-key Set callback (inline interface, CS0021)
- [ ] `src/Design/Design.Stubs/Indexers/IndexerRedesignAcceptance.cs:84` -- AC-4: Per-key sequences (inline interface, CS0021)
- [ ] `src/Design/Design.Stubs/Indexers/IndexerRedesignAcceptance.cs:119` -- AC-6: Per-key with fallback (inline interface, CS0021)
- [ ] `src/Design/Design.Stubs/Indexers/IndexerRedesignAcceptance.cs:139` -- AC-7: Multi-param flattened indexer (inline interface, CS0021)
- [ ] `src/Design/Design.Stubs/Indexers/IndexerRedesignAcceptance.cs:170` -- AC-9: Multi-indexer overloads (inline interface, CS0021)
- [ ] `src/Design/Design.Stubs/Indexers/IndexerRedesignAcceptance.cs:188` -- AC-10: Multi-indexer callback overloads (inline interface, CS1061)
- [ ] `src/Design/Design.Stubs/Indexers/IndexerRedesignAcceptance.cs:207` -- AC-11: Init-only per-key Returns (inline interface, CS0021)
- [ ] `src/Design/Design.Stubs/Indexers/IndexerRedesignAcceptance.cs:220` -- AC-12: Tracking with per-key (inline interface, CS0021)
- [ ] `src/Design/Design.Stubs/Indexers/IndexerRedesignAcceptance.cs:306` -- AC-S1: Standalone per-key Returns (standalone, CS0021)
- [ ] `src/Design/Design.Stubs/Indexers/IndexerRedesignAcceptance.cs:321` -- AC-S2: Standalone multi-param flattened (standalone, CS0021)

Note: AC-5 (line 103), AC-8 (line 151), and AC-13 (line 245) already compile today (they exercise existing all-keys callback and sequence functionality). They serve as regression checks -- they must continue to compile after the redesign.

### In Scope

**Phase 0: Model Changes and Dead Code Cleanup**
- [ ] Modify `UnifiedIndexerInterceptorModel` for new design (remove `SingleKeyType`, add per-key fields)
- [ ] Remove `UnifiedIndexerContainerModel`
- [ ] Remove `FlatIndexerGroup` and `InlineIndexerGroup`
- [ ] Remove `BaseName` from `FlatIndexerModel` and `InlineIndexerModel`
- [ ] Remove dead code: `FlatRenderer.RenderIndexerInterceptorClass` (line 902)
- [ ] Remove dead code: `FlatRenderer.BuildIndexerAccessMap` (line 438)
- [ ] Remove dead code: `FlatRenderer.RenderIndexerContainerClass` (line 831)
- [ ] Remove dead code: `InlineRenderer.RenderIndexerContainerClass` (line 627)
- [ ] Remove dead code: `InlineModelBuilder.GroupIndexers` (line 1762), `InlineModelBuilder.BuildIndexerAccessMap` (line 1781)
- [ ] **Checkpoint: Solution compiles (tests may fail due to model changes)**

**Phase 1: Library Interface Verification**
- [ ] Confirm no changes needed to `IIndexerGetBuilder`, `IIndexerSetBuilder`, `IIndexerGetSequence`, `IIndexerSetSequence`, `IIndexerGetTracking`, `IIndexerSetTracking`
- [ ] No new library interfaces needed (per-key builders are generated)

**Phase 2: IndexerInterceptorRenderer Rewrite**
- [ ] Rewrite `IndexerInterceptorRenderer.cs` to generate new interceptor architecture:
  - Per-key builder nested class (PerKeyBuilder with Returns, ThenReturns, Get, Set -- accessor-dependent)
  - Per-key sequence nested class (PerKeySequence with ThenReturns)
  - Per-key storage dictionary per key type
  - Indexer accessor(s) returning per-key builders (flattened for multi-param)
  - All-keys Get/Set methods (overloaded for multi-indexer)
  - InvokeGet/InvokeSet with new priority chain (per-key first, checking HasGetConfig)
  - Type-suffixed invoke methods for multi-indexer: `InvokeGet_{FriendlyName}`
  - Type-suffixed ref return backing fields for multi-indexer: `_refReturnBacking_{FriendlyName}`
  - IsConfigured checking per-key config state (not existence) + all-keys state
  - Tracking (per-type for multi-indexer with type-suffixed names)
  - Verification (combined across all key types)
  - Sequences (all-keys level, unchanged semantics)
  - Reset (clear per-key builders + all-keys state)
- [ ] **Checkpoint: Renderer compiles in isolation**

**Phase 3: Builder Updates**
- [ ] Update `FlatModelBuilder` -- remove container/grouping logic, produce new model shape
- [ ] Update `InlineModelBuilder` -- same changes, remove GroupIndexers/BuildIndexerAccessMap
- [ ] Update `StandaloneClassModelBuilder` -- collect indexers into single interceptor
- [ ] Update `ClassModelBuilder` (if separate) -- same pattern for inline class stubs
- [ ] **Checkpoint: All builders compile**

**Phase 4: Renderer Updates (Pipeline-Specific)**
- [ ] Update `FlatRenderer` -- remove container rendering, update implementation rendering for type-suffixed InvokeGet/InvokeSet
- [ ] Update `InlineRenderer` -- same changes as FlatRenderer
- [ ] Update `ClassRenderer` -- update indexer override rendering for type-suffixed Invoke calls, update IsConfigured pattern
- [ ] Update `StandaloneClassRenderer` -- same changes as ClassRenderer
- [ ] **Checkpoint: Full build succeeds, `dotnet build src/Design/Design.Stubs` shows reduced errors**

**Phase 5: Test Updates**
- [ ] Update `IndexerTests.cs` -- replace Backing with Returns
- [ ] Update `IndexerGapReproductionTests.cs` -- use Returns for multi-param
- [ ] Update `InlineMultiIndexerTests.cs` -- remove OfXxx, use indexer overloads
- [ ] Update `SequencingTests.cs` -- update indexer sequence tests
- [ ] Update `RefReturnTests.cs` -- remove OfXxx, use indexer overloads
- [ ] Update `BclInterfaceTests.cs` -- update BCL indexer tests
- [ ] Update `BclStandaloneTests.cs` -- update standalone BCL tests
- [ ] Update `ClassIndexerVerificationTests.cs` -- update class indexer verification
- [ ] Update `StandaloneClassStubTests.cs` -- update class stub indexer tests
- [ ] Update `NeatooTests.cs` -- update Neatoo-specific indexer tests
- [ ] Update `ProtectedMemberTests.cs` -- update protected indexer tests
- [ ] Update `BuilderElevationTests.cs` -- update builder elevation tests
- [ ] Add new tests: per-key configuration (Returns, Get, Set)
- [ ] Add new tests: per-key sequences (ThenReturns)
- [ ] Add new tests: per-key with all-keys fallback
- [ ] Add new tests: multi-indexer overload resolution
- [ ] Add new tests: multi-param flattened indexer accessor (2, 3 params)
- [ ] **Checkpoint: All tests pass across all target frameworks**

**Phase 6: Design Project Updates**
- [ ] `dotnet build src/Design/Design.Stubs` succeeds (IndexerRedesignAcceptance.cs compiles)
- [ ] Rewrite `Design.Stubs/Indexers/IndexerBasics.cs` with new API
- [ ] Rewrite `Design.Stubs/Indexers/IndexerSequences.cs` with new API
- [ ] Update `Design.Stubs/ProtectedMembers/ProtectedMethodBehavior.cs` (has Backing usage)
- [ ] Rewrite `Design.Tests/IndexerTests/IndexerBasicsTests.cs`
- [ ] Rewrite `Design.Tests/IndexerTests/IndexerSequenceTests.cs`
- [ ] Update `Design.Tests/ProtectedMemberTests/ProtectedMethodBehaviorTests.cs` (has Backing usage)
- [ ] **Checkpoint: `dotnet build src/Design/Design.Stubs` succeeds, `dotnet test src/Design/Design.Tests` passes**

**Phase 7: Documentation and Samples**
- [ ] Rewrite `src/Tests/KnockOff.Documentation.Samples/IndexersSamples.cs`
- [ ] Update `InterceptorApiSamples.cs`, `ApiConsistencyMatrixSamples.cs`, `SkillContentSamples.cs`, `ReadmeComparisonSamples.cs`
- [ ] Update `src/Benchmarks/KnockOff.Benchmarks/Benchmarks/IndexerBenchmarks.cs`
- [ ] Update `docs/guides/api-consistency-matrix.md` for new indexer API
- [ ] Update `skills/knockoff/` skill content
- [ ] **Checkpoint: Documentation samples compile and pass**

### Explicitly Out of Scope

- Per-key tracking/verification (deferred to future per Q1 resolution)
- Per-key `ThenDefault()` (deferred per Q3 resolution)
- New diagnostic KO2001 (defensive diagnostic for non-unique key types -- not needed for C#-valid interfaces)
- Release notes (written after implementation is verified)
- Version bump in `Directory.Build.props` (separate commit after verification)

### Verification Gates

1. **After Phase 0:** Solution compiles. Dead code removed. Model types updated.
2. **After Phase 2:** `IndexerInterceptorRenderer.cs` rewritten. Compiles in isolation.
3. **After Phase 4:** Full `dotnet build src/KnockOff.sln` succeeds. `dotnet build src/Design/Design.Stubs` shows acceptance criteria errors reduced (some may still fail until test updates).
4. **After Phase 5:** `dotnet test src/Tests/KnockOffTests` passes across all target frameworks (net8.0, net9.0, net10.0).
5. **After Phase 6:** `dotnet build src/Design/Design.Stubs` succeeds with zero errors. `dotnet test src/Design/Design.Tests` passes. All 12 acceptance criteria in `IndexerRedesignAcceptance.cs` compile.
6. **Final:** All tests pass. Design.Stubs compiles. Design.Tests pass. Documentation samples compile and pass. No `Backing` or `OfXxx` references remain in generated code.

### Stop Conditions

If any of these occur, STOP and report:
- Out-of-scope test fails (test not listed in Phase 5 above starts failing)
- Architectural contradiction discovered (e.g., C# indexer overloads cannot disambiguate for a valid interface combination)
- Generated code does not compile for a pattern that previously worked
- Library interface changes are needed (plan explicitly says none are needed)
- The `IndexerInterceptorRenderer` rewrite exceeds 1500 lines (indicates design may need simplification)
- Multi-indexer C# overload resolution is ambiguous for any valid interface combination

---

## Implementation Progress

**Status:** Awaiting Verification
**Developer:** knockoff-developer
**Started:** 2026-02-08
**Completed:** 2026-02-09

### Phase Completion

- [x] Phase 0: Model changes and dead code cleanup
- [x] Phase 1: Library interface verification (no changes needed)
- [x] Phase 2: IndexerInterceptorRenderer rewrite
- [x] Phase 3: Builder updates across all 4 pipelines
- [x] Phase 4: Renderer updates (FlatRenderer, InlineRenderer, ClassRenderer, StandaloneClassRenderer)
- [x] Phase 5: Test updates (6 test files updated)
- [x] Phase 6: Design project updates (Design.Stubs and Design.Tests updated)
- [x] Phase 7: Documentation and samples (all sample files, benchmarks, guides, and skill content updated)

### Key Implementation Decisions

1. **LastGetKey/LastSetEntry tracking fix**: During Phase 5, 10 tests failed because per-key accesses did not update LastGetKey/LastSetEntry. Fixed by recording the key/entry at the TOP of every InvokeGet/InvokeSet/InvokeRefGet method, before the per-key check. Simplified LastGetKey and LastSetEntry properties to simple field returns.

2. **Multi-indexer verification**: VerifyGet/VerifySet count ALL indexer accesses (not per-type). For multi-indexer, tracking uses type-suffixed properties: LastStringGetKey, LastInt32GetKey, etc.

3. **Benchmarks Get callback casting**: For `ICache` with `object this[string key]` and `int this[int index]`, the string indexer Get callback needed explicit cast: `stub.Indexer.Get((string key) => (object)"value")`.

---

## Completion Evidence

### Test Results

**KnockOffTests (main test suite):**
- net8.0: 1410 passed, 0 failed, 0 skipped
- net9.0: 1411 passed, 0 failed, 0 skipped
- net10.0: 1411 passed, 0 failed, 0 skipped

**Design.Tests:**
- net8.0: 356 passed, 0 failed, 0 skipped
- net9.0: 356 passed, 0 failed, 0 skipped
- net10.0: 356 passed, 0 failed, 0 skipped

**Documentation.Samples:**
- net8.0: 599 passed, 0 failed, 0 skipped
- net9.0: 599 passed, 0 failed, 0 skipped
- net10.0: 599 passed, 0 failed, 0 skipped

**NeatooInterfaceTests:**
- net8.0: 473 passed, 0 failed, 0 skipped
- net9.0: 473 passed, 0 failed, 0 skipped
- net10.0: 473 passed, 0 failed, 0 skipped

**AssemblyStrict:**
- net9.0: 14 passed, 0 failed, 0 skipped
- net10.0: 14 passed, 0 failed, 0 skipped

### Build Results

- `dotnet build src/KnockOff.sln` -- 0 errors, 0 warnings
- `dotnet build src/Design/Design.Stubs` -- 0 errors, 0 warnings
- `dotnet build src/Design/Design.Tests` -- 0 errors, 0 warnings
- `dotnet build src/Benchmarks/KnockOff.Benchmarks` -- 0 errors, 0 warnings

### Design.Stubs Acceptance Criteria Verification

All 15 acceptance criteria in `IndexerRedesignAcceptance.cs` compile and pass:
- AC-1: Per-key Returns (single-key)
- AC-2: Per-key Get callback
- AC-3: Per-key Set callback
- AC-4: Per-key sequences (Returns().ThenReturns())
- AC-5: All-keys callbacks
- AC-6: Per-key with all-keys fallback
- AC-7: Multi-param flattened indexer
- AC-8: Multi-param callbacks with tuple key
- AC-9: Multi-indexer overloads by key type
- AC-10: Multi-indexer callback overloads
- AC-11: Init-only indexer per-key Returns
- AC-12: Tracking (LastGetKey, LastSetEntry, VerifyGet, VerifySet)
- AC-13: All-keys sequences unchanged
- AC-14: No Backing dictionary (commented verify -- would not compile if uncommented)
- AC-15: No OfXxx pattern (commented verify -- would not compile if uncommented)

Standalone acceptance (AC-S1, AC-S2, AC-S3) also compile and pass.

### No Remaining Old API References

- `grep -r "\.Backing\[" src/` returns only commented-out "should not compile" examples in IndexerRedesignAcceptance.cs
- `grep -r "\.OfString\.\|\.OfInt32\." src/` returns only commented-out "should not compile" example in IndexerRedesignAcceptance.cs
- `grep -r "Backing" skills/` returns 0 matches
- All documentation, samples, benchmarks, and skill content updated to new API

### mdsnippets

`dotnet mdsnippets` completes with only pre-existing missing snippet errors in `skills/knockoff/references/methods.md` (unrelated to indexer redesign). All indexer-related snippets regenerated successfully.

---

## Architect Verification

**Verified:** 2026-02-09
**Verdict:** VERIFIED

### Independent Build Results

- `dotnet build src/KnockOff.sln` -- 0 errors, 0 warnings (Build succeeded)

### Independent Test Results

All tests pass with zero failures across all target frameworks:

| Project | net8.0 | net9.0 | net10.0 |
|---------|--------|--------|---------|
| KnockOffTests | 1410 passed, 0 failed | 1411 passed, 0 failed | 1411 passed, 0 failed |
| Documentation.Samples | 599 passed, 0 failed | 599 passed, 0 failed | 599 passed, 0 failed |
| NeatooInterfaceTests | 473 passed, 0 failed | 473 passed, 0 failed | 473 passed, 0 failed |
| AssemblyStrict | 14 passed, 0 failed | 14 passed, 0 failed | 14 passed, 0 failed |
| Design.Tests | 356 passed, 0 failed | 356 passed, 0 failed | 356 passed, 0 failed |

### Design.Stubs Compilation

- `dotnet build src/Design/Design.Stubs` -- 0 errors, 0 warnings across all 3 TFMs
- All 18 acceptance criteria (AC-1 through AC-15, AC-S1 through AC-S3) in `IndexerRedesignAcceptance.cs` compile

### Design Match Verification

1. **Per-key builders via indexer syntax**: Confirmed. `stub.Indexer[key].Returns(value)` works via generated `PerKeyBuilder` class with `Dictionary<KeyType, PerKeyBuilder>` storage. Verified in `IndexerInterceptorRenderer.cs` lines 73-95.

2. **Flattened multi-param indexers**: Confirmed. Interceptor generates `this[int row, int col]` using `model.ParameterSignature`. Tests use `stub.Indexer[3, "b"].Returns(42)`, `stub.Indexer[1, 2, 3].Returns(42.0)`, `stub.Indexer["dbo", "Users", "Id", 0].Returns("int")`.

3. **Multi-indexer uses C# overloads, no OfXxx**: Confirmed. Multi-indexer tests access both key types via same `stub.Indexer` property (`stub.Indexer["key"]` and `stub.Indexer[0]`). No OfXxx in any active code.

4. **Priority chain**: Confirmed in `RenderInvokeGet` (lines 335-394): per-key > all-keys sequence > repeating Get callback > source delegation > strict mode > default.

5. **Library interfaces unchanged**: Confirmed. `IIndexerGetBuilder<TKey, TValue>`, `IIndexerSetBuilder<TKey, TValue>`, `IIndexerGetSequence`, `IIndexerSetSequence`, `IIndexerGetTracking`, `IIndexerSetTracking` all unchanged.

6. **IsConfigured checks config state, not existence**: Confirmed. `IsConfigured` iterates per-key builders and checks `b.HasGetConfig || b.HasSetConfig` (line 767), not mere existence.

7. **PerKeyBuilder generates only applicable methods**: Confirmed. `RenderPerKeyBuilder` conditionally generates `Returns`/`Get` only when `model.HasGetter` is true, and `Set` only when `model.HasSetter` is true.

8. **Old code removed**: `UnifiedIndexerContainerModel`, `FlatIndexerGroup`, `InlineIndexerGroup` model files deleted. No `RenderIndexerContainerClass`, `BuildIndexerAccessMap`, `GroupIndexers` in generator code.

### No Old API References

- `.Backing[` in `src/`: Only in commented-out "should not compile" examples in IndexerRedesignAcceptance.cs
- `.OfString.` / `.OfInt32.` in `src/`: Only in commented-out "should not compile" example in IndexerRedesignAcceptance.cs (NeatooInterfaceTests hits are unrelated class names like `EntityPropertyOfStringStub`)
- `Backing` in `skills/`: 0 matches
- `.Backing[` in `docs/guides/` and `docs/reference/`: 0 matches

### Minor Observation (Non-Blocking)

- `InlineIndexerModel.cs` line 39 has a stale doc comment: `"Friendly name for the key type (e.g., "Int32", "String") for OfXxx pattern."` The field `KeyTypeFriendlyName` is now repurposed for type-suffixed InvokeGet naming. This is a cosmetic documentation nit in generator internal code, not a functional issue.
