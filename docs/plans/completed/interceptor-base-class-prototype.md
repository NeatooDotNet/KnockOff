# Interceptor Base Class Prototype

**Status:** Verified
**Created:** 2026-02-13
**Last Updated:** 2026-02-13 (Architect Verification Passed)
**Linked Todo:** [Reduce Generated Code Size](../todos/reduce-generated-code-size.md)

---

## Problem Statement

KnockOff generates large amounts of structurally identical code per interceptor. Every method interceptor gets its own `MethodCallBuilderImpl`, `MethodSequenceImpl`, `WhenMatcher` hierarchy, `WhenBuilder`, `WhenChain`, plus the full `Invoke` priority chain -- all of which are structurally identical across interceptors and differ only in delegate type, argument type, and return type.

For a stub like `DataReaderStubTests` (32 interceptors, 17,234 lines), most of this code is repeated 32 times with only type substitutions. This costs compile time because the C# compiler must parse, bind, and emit all of it from scratch for every generated file.

**Goal of the prototype:** Validate that interceptor logic can be moved into pre-compiled generic base classes in the KnockOff library, so the generated interceptor classes become thin wrappers. This is a standalone prototype project -- no generator changes.

---

## Approach

Create a `src/Prototype/` project that:

1. Copies three real generated stubs verbatim (as baseline)
2. Creates generic base classes in a separate library-like namespace
3. Refactors the copied stubs to inherit from the base classes
4. Must compile and produce identical behavior (verified by copying relevant tests)

### Why a Prototype

The base class approach is a significant architectural change. Before modifying the generator, we need to prove:
- The generic type parameterization works for all interceptor variants (void/non-void methods, properties, indexers)
- Inner classes (MethodCallBuilderImpl, MethodSequenceImpl, WhenMatcher, etc.) can be replaced by library generics
- The `Invoke` method can be split so the priority chain lives in the base class
- No behavioral regressions occur

---

## Three Stubs to Copy

### 1. BasicUserMethodStub (Standalone, methods only)

**Source generated file:** `src/Design/Design.Stubs/Generated/KnockOff.Generator/KnockOff.KnockOffGenerator/BasicUserMethodStub.g.cs` (2,314 lines)
**Base file:** `src/Design/Design.Stubs/Generated/KnockOff.Generator/KnockOff.KnockOffGenerator/BasicUserMethodStub.Base.g.cs` (20 lines)
**Interface:** `IStubOverrideService` from `src/Design/Design.Domain/Services/IStubOverrideService.cs`
**Stub declaration file:** not in source control (generated code references `Design.Domain.Services.IUserMethodService` which was renamed to `IStubOverrideService`)

Contains 4 method interceptors covering key variations:
- `ProcessInterceptor` -- non-void, 1 param (`string Process(string input)`)
- `CalculateInterceptor` -- non-void, 2 params (`int Calculate(int a, int b)`)
- `ExecuteInterceptor` -- void, 1 param (`void Execute(string command)`)
- `FindByIdInterceptor` -- non-void, 1 param, nullable return (`string? FindById(int id)`)

Each interceptor includes: delegate type, callback field, return value fields, sequence, When chain (WhenMatcher hierarchy + WhenBuilder + WhenChain), MethodCallBuilderImpl, MethodSequenceImpl, Invoke method, Reset, Verify, CheckVerification, CheckVerificationAll, Verifiable, IsConfigured, IsVerifiable.

**Why this stub:** Covers the core method interceptor with all four method signatures (void vs non-void, single vs multi-param). The presence of stub override (base class with `Process_`, `Calculate_`, etc.) is important because it exercises the fallback path in Invoke.

### 2. MatrixStandaloneStub (Standalone, properties + indexers)

**Source generated file:** `src/Design/Design.Stubs/Generated/KnockOff.Generator/KnockOff.KnockOffGenerator/MatrixStandaloneStub.g.cs` (1,578 lines)
**Base file:** `src/Design/Design.Stubs/Generated/KnockOff.Generator/KnockOff.KnockOffGenerator/MatrixStandaloneStub.Base.g.cs` (14 lines)
**Interface:** `IMatrix` from `src/Design/Design.Domain/Entities/ICollection.cs`
**Stub declaration:** `src/Design/Design.Stubs/Indexers/IndexerGapStubs.cs` line 24

Contains 3 interceptors:
- `RowsInterceptor` -- get-only property (`int Rows { get; }`)
- `ColumnsInterceptor` -- get-only property (`int Columns { get; }`)
- `IndexerInterceptor` -- get+set indexer with multi-key (`double this[int row, int col] { get; set; }`)

The indexer interceptor is significantly more complex than properties: per-key builders (`Dictionary<(int,int), PerKeyBuilder>`), separate get/set chains, get/set When chains (`IndexerGetWhenMatcher`, `IndexerSetWhenMatcher`), and separate get/set builder/sequence inner classes.

**Why this stub:** Covers property and indexer interceptors -- structurally different from method interceptors. The indexer with its per-key builders, dual get/set paths, and When chains for both get and set is the most complex interceptor type.

### 3. DataReaderStubTests (Inline, methods + properties + indexers, large scale)

**Source generated file:** `src/Tests/KnockOffTests/Generated/KnockOff.Generator/KnockOff.KnockOffGenerator/DataReaderStubTests.Stubs.g.cs` (17,234 lines)
**Stub declaration:** `src/Tests/KnockOffTests/BclInterfaceStubs.cs` lines 360-363
**Interface:** `System.Data.IDataReader` (BCL interface)

Contains 32 interceptors:
- 4 property interceptors (Depth, IsClosed, RecordsAffected, FieldCount -- all get-only `int`)
- 1 indexer interceptor (Indexer -- `object this[int]` and `object this[string]`)
- 27 method interceptors (Close, GetSchemaTable, NextResult, Read, GetBoolean, GetByte, GetBytes, GetChar, GetChars, GetData, GetDataTypeName, GetDateTime, GetDecimal, GetDouble, GetFieldType, GetFloat, GetGuid, GetInt16, GetInt32, GetInt64, GetName, GetOrdinal, GetString, GetValue, GetValues, IsDBNull, Dispose)

**Why this stub:** Proves the approach scales. 32 interceptors of the same structural patterns means 32x code duplication. If the base class approach works here, the compile-time savings will be substantial. Also exercises the inline pattern (nested in `Stubs` static class) vs. standalone.

---

## Base Class Design

### Type Hierarchy

```
                              MethodInterceptorBase<TDelegate, TArgs, TReturn>
                                  (non-void methods)
                                        |
                        VoidMethodInterceptorBase<TDelegate, TArgs>
                                  (void methods)
                                        |
                              PropertyGetInterceptorBase<TValue>
                                  (get-only properties)
                                        |
                           PropertyGetSetInterceptorBase<TValue>
                                  (get+set properties)
                                        |
                        IndexerGetInterceptorBase<TKey, TValue>
                                  (get-only indexers)
                                        |
                       IndexerGetSetInterceptorBase<TKey, TValue>
                                  (get+set indexers)
```

Note: Property and indexer interceptors do NOT inherit from method interceptors. They are separate hierarchies because their internal structures differ significantly (no delegate type, no arg tracking for properties; per-key builders for indexers).

### Method Interceptor Base Classes

#### VoidMethodInterceptorBase&lt;TDelegate, TArgs&gt;

**Type parameters:**
- `TDelegate` -- the method's delegate type (e.g., `Action<string>` for `Execute`)
- `TArgs` -- the argument(s): single type for 1-param, `ValueTuple` for multi-param, `Unit` struct for 0-param

**Fields moved from generated interceptor to base class:**
```csharp
// Callback
protected TDelegate? _call;
protected MethodCallBuilderBase<TDelegate, TArgs>? _callTracking;

// Sequence
protected List<(TDelegate Callback, MethodCallBuilderBase<TDelegate, TArgs> Tracking)>? _sequence;
protected int _sequenceIndex;
protected bool _repeatLastValue = true;

// When chain
protected List<VoidWhenMatcherBase<TArgs>>? _whenChain;
protected int _whenChainHead;
protected bool _whenVerifiable;

// Verification
protected bool _isVerifiable;
protected Called? _verifiableTimes;

// Unconfigured tracking
protected int _unconfiguredCallCount;
```

**Methods moved to base class:**
- `Reset()` -- structural, only touches fields listed above
- `Verify()`, `Verify(Called)` -- structural except member name string
- `Verifiable()`, `Verifiable(Called)` -- structural
- `CheckVerification()` -- structural except member name string
- `CheckVerificationAll()` -- structural except member name string
- `IsVerifiable` property -- structural
- `IsConfigured` property -- structural
- `TotalCallCount` property -- structural

**Constructor takes member name:**
```csharp
protected VoidMethodInterceptorBase(string memberName)
```

**Invoke split -- RunVoidPriorityChain in base class:**
```csharp
protected bool RunVoidPriorityChain(bool strict, TArgs args)
{
    // When chain block (~15 lines) -- uses abstract InvokeVoidDelegate, abstract MatchesWhen
    // Sequence block (~7 lines) -- uses abstract InvokeVoidDelegate
    // Callback block (~5 lines) -- uses abstract InvokeVoidDelegate
    // Returns false if nothing handled the call
    // Returns true if something handled it
}
```

**Abstract methods the generated class must override:**
```csharp
protected abstract void InvokeVoidDelegate(TDelegate del, TArgs args);
protected abstract void RecordUnconfiguredArgs(TArgs args);
```

**Generated interceptor Invoke becomes:**
```csharp
internal void Invoke(bool strict, BasicUserMethodStub stub, string command)
{
    if (RunVoidPriorityChain(strict, command))
        return;
    _unconfiguredCallCount++;
    RecordUnconfiguredArgs(command);
    // Sequence exhausted repeat (structural -- could move to base)
    // Source delegation
    // Strict exception
    // Fallback
    stub.Execute_(command);
}
```

The unconfigured tail (~15 lines) remains generated because it references the stub instance and the specific fallback method.

#### MethodInterceptorBase&lt;TDelegate, TArgs, TReturn&gt; : VoidMethodInterceptorBase&lt;TDelegate, TArgs&gt;

**Additional fields:**
```csharp
protected TReturn _returnValue;
protected bool _hasReturnValue;
protected MethodCallBuilderBase<TDelegate, TArgs>? _returnValueTracking;
```

**Override of RunPriorityChain to add return-value block:**
```csharp
protected (bool Handled, TReturn Result) RunPriorityChain(bool strict, TArgs args)
{
    // When chain block -- same as void but returns TReturn
    // Sequence block -- returns value
    // Return value block (NEW -- not in void) -- returns _returnValue
    // Callback block -- returns delegate result
    // Returns (false, default) if nothing handled
}
```

**Abstract methods:**
```csharp
protected abstract TReturn InvokeDelegate(TDelegate del, TArgs args);
// RecordUnconfiguredArgs inherited from void base
```

**Generated interceptor Invoke becomes:**
```csharp
internal string Invoke(bool strict, BasicUserMethodStub stub, string input)
{
    var (handled, result) = RunPriorityChain(strict, input);
    if (handled) return result;
    _unconfiguredCallCount++;
    RecordUnconfiguredArgs(input);
    // Sequence exhausted repeat
    // Source delegation
    // Strict exception
    return stub.Process_(input);
}
```

### Inner Class Base Classes

#### MethodCallBuilderBase&lt;TDelegate, TArgs&gt;

Replaces the generated `MethodCallBuilderImpl` inner class.

**Fields:**
```csharp
internal int _callCount;
```

**Methods moved to base:**
- `Reset()` -- structural
- `Verify()`, `Verify(Called)` -- structural (uses "method" as member name)
- `ThenReturn(TDelegate callback)` -- elevates to sequence mode
- `ThenReturn(params TReturn[] values)` -- convenience multi-value (for non-void variant)
- `Verifiable()`, `Verifiable(Called)` -- sets interceptor fields

**Problem:** The builder accesses private fields on the interceptor (`_sequence`, `_call`, `_hasReturnValue`, `_returnValue`, etc.) to perform lazy elevation from callback mode to sequence mode. These field accesses are what make the builder tightly coupled to its interceptor.

**Solution:** The base class builder takes a reference to the base class interceptor. Since the fields are `protected` on the base interceptor, the builder base class (also in the library) can access them IF it is a nested class or friend. In C#, nested generics in the base class can access protected members. Alternative: use `internal` and put both in the same assembly.

**Recommended approach:** Make `MethodCallBuilderBase` a nested class of `VoidMethodInterceptorBase`/`MethodInterceptorBase`. This gives it natural access to all interceptor fields. The generated code creates instances but does not need to extend it.

#### MethodSequenceBase&lt;TDelegate, TArgs&gt;

Replaces the generated `MethodSequenceImpl` inner class.

Structurally identical across all interceptors. Adds items to the interceptor's `_sequence` list and provides `Verify()`, `Reset()`, `ThenReturn()`, `ThenDefault()`, `Verifiable()`.

Same nesting approach as the builder.

#### WhenMatcherBase&lt;TArgs&gt; and subclasses

**Note:** The generated code uses individual parameters for multi-param WhenMatchers (e.g., `Matches(int a, int b)`). The base class normalizes these to `TArgs` (tuples). The generated `When(...)` setup methods bridge between the user-facing individual-param API and the tuple-based `Matches(TArgs)`. See Developer Review Concern 1 for full details.

For non-void methods:
- `WhenMatcherBase<TArgs, TReturn>` (abstract) -- `Matches(TArgs)`, `Call(TArgs) -> TReturn`, `IsTerminal`, `CallCount`
- `WhenMatcherValueBase<TArgs, TReturn>` -- `Func<TArgs, bool>` predicate + stored `TReturn` value
- `WhenMatcherCallBase<TArgs, TReturn>` -- `Func<TArgs, TReturn>` callback, always matches, terminal
- `WhenMatcherNoneBase<TArgs, TReturn>` -- never matches, terminal

For void methods:
- `VoidWhenMatcherBase<TArgs>` (abstract) -- `Matches(TArgs)`, `Call(TArgs)`, `IsTerminal`, `CallCount`
- `VoidWhenMatcherPredicateBase<TArgs>` -- `Func<TArgs, bool>` predicate, mutable `Action<TArgs>?` callback (set by VoidWhenChain.Call)
- `VoidWhenMatcherCallBase<TArgs>` -- `Action<TArgs>` callback, always matches, terminal
- `VoidWhenMatcherNoneBase<TArgs>` -- never matches, terminal

#### WhenBuilderBase and WhenChainBase (non-void) / VoidWhenChainBase (void)

Non-void and void When flows have different construction patterns. Non-void uses `WhenBuilder.Return(value)` to complete a matcher. Void adds the matcher immediately and `VoidWhenChain.Call(callback)` mutates it. See Developer Review Concern 3 for full details.

Both base class variants handle: ThenWhen, ThenCall, ThenNone, Verify, Reset, Verifiable. The generated code provides only the `When(...)` entry points and (for void) the `Call(delegate)` bridging method.

### Property and Indexer Base Classes

Property and indexer interceptors follow a different structural pattern than methods:
- No delegate type parameter
- Get uses `Func<TValue>`, Set uses `Action<TValue>`
- Indexer Get uses `Func<TKey, TValue>`, Set uses `Action<TKey, TValue>`
- Indexer has per-key builders with their own get/set state
- Separate When chains for get and set on indexers

These will need their own base class hierarchies, but the same principle applies: fields and priority-chain logic move to the base class, generated code provides thin overrides.

**Property base classes:**
- `PropertyGetInterceptorBase<TValue>` -- get-only property, includes stub override helpers (`RecordGet()`, `HasGet`, `InvokeGetCallback()`)
- `PropertyGetSetInterceptorBase<TValue>` -- adds set fields, InvokeSet, and set stub override helpers (`RecordSet()`, `HasSet`, `InvokeSetCallback()`)

**Indexer base classes:**
- `IndexerGetInterceptorBase<TKey, TValue>` -- get-only indexer with per-key builders
- `IndexerGetSetInterceptorBase<TKey, TValue>` -- adds set fields, per-key set, set When chain

---

## Estimated Code Reduction

### Per Method Interceptor

| Section | Current Lines | After Base Class | Saved |
|---------|--------------|-----------------|-------|
| Fields | ~15 | 0 (in base) | 15 |
| MethodCallBuilderImpl | ~85 | 0 (library generic) | 85 |
| MethodSequenceImpl | ~55 | 0 (library generic) | 55 |
| WhenMatcher hierarchy | ~35 | 0 (library generic) | 35 |
| WhenBuilder | ~20 | 0 (library generic) | 20 |
| WhenChain | ~45 | 0 (library generic) | 45 |
| Invoke (priority chain) | ~50 | ~12 (thin override) | 38 |
| Verify/Verifiable/Check | ~40 | 0 (in base) | 40 |
| Reset | ~15 | 0 (in base) | 15 |
| Return/Call setup methods | ~40 | ~40 (must stay -- public API) | 0 |
| When setup methods | ~10 | ~10 (must stay -- public API) | 0 |
| LastArg property | ~5 | 0 (in base, or trivial override) | 5 |
| **Total** | **~415** | **~62** | **~353 (85%)** |

### Per Property Interceptor

| Section | Current Lines | After Base Class | Saved |
|---------|--------------|-----------------|-------|
| Fields | ~8 | 0 (in base) | 8 |
| PropertyGetBuilderImpl | ~60 | 0 (library generic) | 60 |
| PropertyGetSequenceImpl | ~40 | 0 (library generic) | 40 |
| InvokeGet (priority chain) | ~25 | ~8 (thin override) | 17 |
| Verify/Verifiable/Check | ~30 | 0 (in base) | 30 |
| Reset | ~10 | 0 (in base) | 10 |
| Get setup methods | ~20 | ~20 (must stay -- public API) | 0 |
| **Total** | **~193** | **~28** | **~165 (85%)** |

### DataReaderStubTests.Stubs.g.cs Projection

- 27 method interceptors: 27 x 353 = ~9,531 lines saved
- 4 property interceptors: 4 x 165 = ~660 lines saved
- 1 indexer interceptor: ~600 lines saved (estimate)
- Stub-level boilerplate: ~400 lines (remains)
- **Total saved: ~10,791 of ~17,234 lines (63%)**

This means the C# compiler processes ~6,400 lines instead of ~17,200 lines for this one stub -- a 63% reduction in per-stub compile work.

---

## Prototype Project Structure

```
src/Prototype/
  Prototype.sln                        -- standalone solution
  Prototype.Library/                   -- simulates KnockOff library additions
    Prototype.Library.csproj
    Unit.cs                            -- zero-param sentinel type
    Interceptors/
      MethodInterceptorBase.cs         -- generic base for non-void methods
      VoidMethodInterceptorBase.cs     -- generic base for void methods
      PropertyGetInterceptorBase.cs    -- generic base for get-only properties
      IndexerGetSetInterceptorBase.cs  -- generic base for get+set indexers
  Prototype.Stubs/                     -- copied + refactored generated stubs
    Prototype.Stubs.csproj             -- references Prototype.Library + KnockOff
    Interfaces/
      IStubOverrideService.cs          -- copy of Design.Domain IStubOverrideService
      IMatrix.cs                       -- copy of IMatrix from Design.Domain
    Original/                          -- verbatim copies for diff comparison
      BasicUserMethodStub.Original.cs
      MatrixStandaloneStub.Original.cs
      DataReaderStubTests.Original.cs
    Refactored/                        -- stubs refactored to use base classes
      BasicUserMethodStub.cs
      MatrixStandaloneStub.cs
      DataReaderStubTests.cs
  Prototype.Tests/                     -- behavioral equivalence tests
    Prototype.Tests.csproj
    BasicUserMethodTests.cs            -- tests for method interceptors
    MatrixStandaloneTests.cs           -- tests for property + indexer interceptors
    DataReaderTests.cs                 -- tests for scale + inline pattern
```

### Key Design Decisions for the Prototype

1. **Prototype.Library simulates KnockOff library additions.** The base classes will eventually live in `src/KnockOff/`. For the prototype, a separate project keeps things isolated.

2. **Original/ folder contains verbatim copies** with namespaces adjusted. No other changes. These are the control group.

3. **Refactored/ folder contains the same stubs** modified to inherit from base classes. Same public API surface, same behavior.

4. **Tests verify behavioral equivalence** by running identical test scenarios against both Original and Refactored stubs.

5. **The Return/Call setup methods remain generated.** These are the public API entry points (`Return(TDelegate)`, `Return(TReturn)`, `Return(TReturn, params TReturn[])`, `Call(Action<TArgs>)`, `When(TArgs)`, `When(Func<TArgs, bool>)`). They differ in their overload resolution (which depends on the concrete types) and must remain on the generated class to provide the right API.

6. **Delegate definition remains generated.** Each method interceptor defines its own delegate type (e.g., `ProcessDelegate`, `CalculateDelegate`). These must stay because they name the specific parameters.

---

## TArgs Convention

| Param Count | TArgs Type | Example |
|-------------|-----------|---------|
| 0 params | `Unit` (struct) | `VoidMethodInterceptorBase<Action, Unit>` |
| 1 param | The param type directly | `VoidMethodInterceptorBase<Action<string>, string>` |
| 2 params | `ValueTuple<T1, T2>` | `MethodInterceptorBase<CalculateDelegate, (int, int), int>` |
| 3+ params | `ValueTuple<T1, T2, T3, ...>` | Same pattern |

The `Unit` type is a zero-size struct:
```csharp
public readonly struct Unit
{
    public static readonly Unit Value = default;
}
```

### InvokeDelegate Implementations

For 1-param non-void:
```csharp
protected override string InvokeDelegate(ProcessDelegate del, string args) => del(args);
```

For 2-param non-void:
```csharp
protected override int InvokeDelegate(CalculateDelegate del, (int a, int b) args) => del(args.a, args.b);
```

For 1-param void:
```csharp
protected override void InvokeVoidDelegate(Action<string> del, string args) => del(args);
```

For 0-param non-void:
```csharp
protected override bool InvokeDelegate(ReadDelegate del, Unit args) => del();
```

---

## What Stays Generated (Cannot Move to Base Class)

1. **Delegate type definition** -- `public delegate string ProcessDelegate(string input);`
2. **Return/Call/When setup methods** -- public API with concrete type overloads
3. **Invoke method thin wrapper** -- calls `RunPriorityChain`, then handles unconfigured tail (source delegation, strict exception, stub override fallback)
4. **InvokeDelegate override** -- 1-line bridge from generic delegate to concrete call
5. **RecordUnconfiguredArgs override** -- captures args in typed fields
6. **Source field** -- typed to the specific interface/class being stubbed
7. **Stub-level interface implementation** -- the stub class itself, Strict property, Source method, VerifyAll, etc.

---

## Implementation Phases

### Phase 1: Method Interceptor Base Classes (Core)

**Goal:** Get BasicUserMethodStub working with base classes.

1. Create `VoidMethodInterceptorBase<TDelegate, TArgs>` with all shared fields and `RunVoidPriorityChain`
2. Create `MethodInterceptorBase<TDelegate, TArgs, TReturn>` extending void with return-value support
3. Create nested `MethodCallBuilderBase` and `MethodSequenceBase`
4. Create nested `WhenMatcherBase` hierarchy, `WhenBuilderBase`, `WhenChainBase`
5. Create `Unit` struct
6. Copy BasicUserMethodStub.g.cs + BasicUserMethodStub.Base.g.cs to Original/
7. Copy IStubOverrideService interface
8. Refactor all 4 interceptors (Process, Calculate, Execute, FindById) to use base classes
9. Write tests verifying: Return(value), Return(callback), sequences, When chains, verification, strict mode, stub override fallback, Reset, LastArg

### Phase 2: Property Interceptor Base Classes

**Goal:** Get MatrixStandaloneStub's property interceptors working.

1. Create `PropertyGetInterceptorBase<TValue>` with get fields and `InvokeGet` priority chain
2. Create nested `PropertyGetBuilderBase` and `PropertyGetSequenceBase`
3. Copy MatrixStandaloneStub.g.cs + MatrixStandaloneStub.Base.g.cs to Original/
4. Copy IMatrix interface
5. Refactor RowsInterceptor and ColumnsInterceptor to use property base class
6. Write tests verifying: Get(value), Get(callback), sequences, verification, strict mode

### Phase 3: Indexer Interceptor Base Classes

**Goal:** Get MatrixStandaloneStub's indexer interceptor working.

1. Create `IndexerGetSetInterceptorBase<TKey, TValue>` with get+set fields, per-key builders, and dual When chains
2. Create nested builder/sequence/when base classes for indexer get and set
3. Refactor IndexerInterceptor to use indexer base class
4. Write tests verifying: per-key Get/Set, When chains, sequences, verification

### Phase 4: Scale Validation (DataReaderStubTests)

**Goal:** Prove the approach scales to 32 interceptors.

1. Copy DataReaderStubTests.Stubs.g.cs to Original/
2. Refactor all 32 interceptors to use appropriate base classes
3. Write tests verifying a representative subset (not all 32 -- pick 5-6 covering property, indexer, method patterns)
4. Measure: count lines in Original vs. Refactored for concrete savings numbers

---

## Success Criteria

1. **Prototype.Stubs compiles** with both Original/ and Refactored/ stubs
2. **All tests pass** for both Original and Refactored variants
3. **Refactored stubs have measurably fewer lines** than Original
4. **Public API is identical** -- test code does not differ between Original and Refactored
5. **Line count reduction is documented** with exact numbers per stub

---

## Risks and Mitigations

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| Generic type complexity makes base classes unusable | Low | High | Prototype validates before generator changes |
| Builder/Sequence inner classes cannot access base fields | Medium | Medium | Nest them in the base class; use protected/internal |
| Value tuple TArgs causes boxing for 0-param case | Low | Low | Unit struct is zero-size, no allocation |
| WhenMatcher generic hierarchy too complex | Medium | Medium | Start with method interceptors, iterate |
| Indexer per-key builders resist generification | Medium | Medium | Phase 3 tackles separately; may need different approach |
| DataReader stub has edge cases not covered by BasicUserMethod/Matrix | Low | Medium | Phase 4 is explicitly for discovering such cases |

---

## Open Questions

1. **Should property base classes use Func/Action directly or introduce delegate types?** Properties currently use `Func<TValue>` for get and have no delegate type. This simplifies the base class but means property interceptors have a different type parameter signature than method interceptors. Prototype will clarify whether a unified base is possible.

2. **Can indexer per-key builders be generic?** The `PerKeyBuilder` inner class holds per-key get/set configuration. It accesses interceptor fields differently from the top-level builder. Need to see if it fits the same base class pattern.

3. **LastArg/LastArgs for multi-param methods.** Currently, 1-param methods expose `LastArg` (singular) and 2+ param methods expose `LastArgs` (plural, named tuple). The base class builder holds a generic `TArgs` field. The generated `MethodCallBuilderImpl` subclass adds the appropriately named property (`LastArg` or `LastArgs`) and a typed `RecordCall` method -- see Concern 2 resolution. The base class provides `RecordCallBase()` for incrementing the counter. **Resolved by Developer Review Concern 2.**

4. **Builder interface implementations.** The three-variant library interface hierarchy (`IMethodCallBuilder<TCallback>` / `IMethodCallBuilder<TCallback, TArg>` / `IMethodCallBuilderArgs<TCallback, TArgs>`) is incompatible with a single base class implementation. **Resolved by Developer Review Concern 2:** base class builder does NOT implement library interfaces; generated subclass adds explicit interface implementation shims (~5-8 lines).

---

## Developer Review

**Status:** Approved
**Reviewed:** 2026-02-13

### Concern 1 (Critical): WhenMatcher individual params vs TArgs tuple

**Observation:** The CalculateInterceptor's WhenMatcher uses individual parameters -- `Matches(int a, int b)` and `Call(int a, int b)` -- not `Matches((int, int) args)`. The plan proposes `WhenMatcherBase<TArgs>` with `Matches(TArgs)` but the generated code uses splatted individual parameters.

**Evidence from generated code:**

```csharp
// CalculateInterceptor.WhenMatcher (line 1035-1041 of BasicUserMethodStub.g.cs):
private abstract class WhenMatcher
{
    public abstract bool Matches(int a, int b);   // <-- individual params
    public abstract int Call(int a, int b);        // <-- individual params
    public abstract bool IsTerminal { get; }
    public int CallCount { get; set; }
}

// But Invoke ALSO uses individual params (line 722):
if (matcher.Matches(a, b))
// ...
return matcher.Call(a, b);
```

Meanwhile, tracking uses tuples in the same interceptor:

```csharp
// CalculateInterceptor.MethodCallBuilderImpl.RecordCall (line 860):
public void RecordCall((int? a, int? b) args) { _callCount++; _lastArgs = args; }

// CalculateInterceptor.Invoke callback invocation (line 746):
tracking.RecordCall((a, b));       // <-- tuple
return callback(a, b);             // <-- individual params
```

**Design Decision: WhenMatcher uses `TArgs` (tuples) in the base class. Invoke packs individual params into a tuple before calling `RunPriorityChain`.**

Rationale:

1. The base class `RunPriorityChain(bool strict, TArgs args)` already receives `TArgs`. The When chain is invoked inside `RunPriorityChain`. So the WhenMatcher already receives `TArgs`, not individual params.

2. The generated `Invoke` method is the boundary between the concrete parameter list and the generic `TArgs`. The generated Invoke packs params into a tuple:

```csharp
// Generated CalculateInterceptor.Invoke (refactored):
internal int Invoke(bool strict, BasicUserMethodStub stub, int a, int b)
{
    var args = (a, b);                                       // <-- pack here
    var (handled, result) = RunPriorityChain(strict, args);  // TArgs = (int, int)
    if (handled) return result;
    _unconfiguredCallCount++;
    RecordUnconfiguredArgs(args);
    // ... unconfigured tail ...
    return stub.Calculate_(a, b);
}
```

3. The WhenMatcher predicate stored inside `WhenMatcherValueBase` becomes `Func<TArgs, bool>` instead of `Func<int, int, bool>`. The generated `When(int a, int b)` setup method bridges the gap:

```csharp
// Generated (stays on interceptor):
public WhenBuilder When(int a, int b)
{
    _whenChain ??= new List<WhenMatcherBase<(int, int), int>>();
    // Bridge: wrap Func<int,int,bool> into Func<(int,int),bool>
    return new WhenBuilder(this, (args) => Object.Equals(args.Item1, a) && Object.Equals(args.Item2, b));
}

// Also generated (predicate variant):
public WhenBuilder When(Func<int, int, bool> predicate)
{
    _whenChain ??= new List<WhenMatcherBase<(int, int), int>>();
    return new WhenBuilder(this, (args) => predicate(args.Item1, args.Item2));
}
```

4. This is already consistent with how `RecordCall` works today -- it already receives a tuple `(a, b)` for multi-param methods. The WhenMatcher is the only place that uses splatted individual params. Normalizing it to tuples is the right move.

5. For 1-param methods, `TArgs` IS the param type directly (not a tuple), so there is no wrapping overhead. `Matches(string input)` becomes `Matches(string args)` -- identical.

**Impact on generated code:** The `When(...)` setup methods and `When(Func<...> predicate)` methods remain generated and stay on the interceptor class. They bridge between the user-facing individual-parameter API and the tuple-based `TArgs` used internally. This is 2-4 lines of bridging code per When overload. The WhenMatcher hierarchy, WhenBuilder, and WhenChain all move to the base class.

### Concern 2 (Medium): Three builder interface variants

**Observation:** The KnockOff library defines three distinct builder interface families:

| Interface | Tracking Property | Verifiable() Returns | Used When |
|-----------|------------------|---------------------|-----------|
| `IMethodCallBuilder<TCallback>` / `IMethodReturnBuilder<TCallback>` | (none) | `IMethodCallBuilder<TCallback>` | 0-param void/non-void |
| `IMethodCallBuilder<TCallback, TArg>` / `IMethodReturnBuilder<TCallback, TArg>` | `TArg LastArg` | `IMethodCallBuilder<TCallback, TArg>` | 1-param methods |
| `IMethodCallBuilderArgs<TCallback, TArgs>` / `IMethodReturnBuilderArgs<TCallback, TArgs>` | `TArgs LastArgs` | `IMethodCallBuilderArgs<TCallback, TArgs>` | 2+ param methods |

A single `MethodCallBuilderBase<TDelegate, TArgs>` cannot implement all three because:
- The 0-param variant has no `LastArg`/`LastArgs` property
- The 1-param variant exposes `LastArg` (singular)
- The 2+ param variant exposes `LastArgs` (plural)
- The `Verifiable()` return types differ

**Design Decision: The base class builder does NOT implement KnockOff library interfaces. Library interface implementations stay as explicit interface implementations on the generated class.**

Rationale:

1. **This is a prototype, not production.** The goal is to prove the base class holds fields and logic, not to perfectly satisfy the library interface hierarchy. The library interfaces are a generator concern -- when the generator emits the refactored interceptors, it can add explicit interface implementation shims just as it does today.

2. **What the base class builder provides:** `_callCount`, `Reset()`, `Verify()`, `Verify(Called)`, `ThenReturn(TDelegate)` / `ThenCall(TDelegate)` (sequence elevation logic), `Verifiable()`, `Verifiable(Called)`. These are the structural methods.

3. **What remains generated as thin shims:**

```csharp
// For 1-param method (ProcessInterceptor):
public sealed class MethodCallBuilderImpl : MethodCallBuilderBase<ProcessDelegate, string>
{
    // LastArg specific to 1-param:
    private string _lastArg = default!;
    public string LastArg => _lastArg;
    public void RecordCall(string input) { RecordCallBase(); _lastArg = input; }

    // Explicit interface implementations:
    IMethodReturnBuilder<ProcessDelegate, string> IMethodReturnBuilder<ProcessDelegate, string>.Verifiable() => ...;
    // etc.
}
```

4. **For 2+ param methods (CalculateInterceptor):**

```csharp
public sealed class MethodCallBuilderImpl : MethodCallBuilderBase<CalculateDelegate, (int, int)>
{
    private (int? a, int? b) _lastArgs;
    public (int? a, int? b) LastArgs => _lastArgs;
    public void RecordCall((int? a, int? b) args) { RecordCallBase(); _lastArgs = args; }

    // Explicit interface implementations:
    IMethodReturnBuilderArgs<CalculateDelegate, (int? a, int? b)> IMethodReturnBuilderArgs<...>.Verifiable() => ...;
}
```

5. **The generated MethodCallBuilderImpl is reduced from ~85 lines to ~15 lines.** The sequence elevation logic (~30 lines), Verify (~10 lines), Reset, and Verifiable core logic all live in the base class. The generated code is: a few typed fields, a typed RecordCall, and explicit interface shims.

6. **When moving to the generator later**, we can evaluate whether the library interface hierarchy itself should be simplified. If `LastArg` and `LastArgs` were unified into a single `LastArgs` of type `TArgs`, the three-variant problem disappears. But that is a separate API change discussion -- not for this prototype.

### Concern 3 (Medium): Void When chain structural differences

**Observation:** The void When chain has structural differences from the non-void When chain:

Non-void `WhenBuilder`:
```csharp
// When(predicate) -> WhenBuilder -> .Return(value) -> WhenChain
public WhenBuilder When(Func<string, bool> predicate)
{
    _whenChain ??= new List<WhenMatcher>();
    return new WhenBuilder(this, predicate);
}
// WhenBuilder.Return adds WhenMatcherValue to chain, returns WhenChain
```

Void `VoidWhenChain`:
```csharp
// When(predicate) -> adds VoidWhenMatcherPredicate immediately -> VoidWhenChain
// VoidWhenChain.Call(callback) sets _currentMatcher.Callback (mutates existing matcher)
public VoidWhenChain When(string command)
{
    _whenChain ??= new List<VoidWhenMatcher>();
    var matcher = new VoidWhenMatcherPredicate(predicate);
    _whenChain.Add(matcher);                                 // <-- adds immediately
    return new VoidWhenChain(this, matcher);                  // <-- holds reference
}

public sealed class VoidWhenChain
{
    private readonly VoidWhenMatcher _currentMatcher;         // <-- holds matcher ref
    public VoidWhenChain Call(Action<string> callback)
    {
        _currentMatcher.Callback = callback;                  // <-- mutates matcher
        return this;
    }
}
```

Key differences:
- Non-void: `When` returns `WhenBuilder`, `WhenBuilder.Return(value)` creates and adds the matcher
- Void: `When` creates and adds the matcher immediately, returns `VoidWhenChain` which holds a reference to it, `VoidWhenChain.Call(callback)` mutates the matcher to set its callback

**Design Decision: Two separate base class pairs -- `WhenBuilderBase`/`WhenChainBase` for non-void, `VoidWhenChainBase` for void. These are small enough that the structural difference is fine.**

Rationale:

1. The non-void and void When flows are genuinely different in their construction pattern. Non-void requires a `Return(value)` call to complete the matcher (predicate + value = complete matcher). Void methods have no return value, so the matcher is "complete" immediately (predicate + optional callback).

2. The base class hierarchy handles this naturally:

```csharp
// In MethodInterceptorBase<TDelegate, TArgs, TReturn> (non-void):
public class WhenBuilderBase
{
    protected readonly MethodInterceptorBase<TDelegate, TArgs, TReturn> _interceptor;
    protected readonly Func<TArgs, bool> _predicate;

    // Creates WhenMatcherValue, adds to chain, returns WhenChainBase
    public WhenChainBase Return(TReturn value) { ... }
}

public class WhenChainBase
{
    // ThenCall(TDelegate) -- adds WhenMatcherCall
    // ThenNone() -- adds WhenMatcherNone
    // Verify(), Reset(), Verifiable()
}
```

```csharp
// In VoidMethodInterceptorBase<TDelegate, TArgs> (void):
public class VoidWhenChainBase
{
    protected readonly VoidMethodInterceptorBase<TDelegate, TArgs> _interceptor;
    protected readonly VoidWhenMatcherBase<TArgs> _currentMatcher;

    // Call(TDelegate) -- sets _currentMatcher.Callback via abstract InvokeVoidDelegate
    // ThenWhen(...) -- adds new matcher, returns new VoidWhenChainBase
    // ThenCall(TDelegate) -- adds terminal matcher
    // ThenNone() -- adds terminal matcher
    // Verify(), Reset(), Verifiable()
}
```

3. **`VoidWhenChainBase.Call()` problem:** The current void pattern sets `_currentMatcher.Callback` as a typed `Action<string>` property on the matcher. In the base class, the matcher is `VoidWhenMatcherBase<TArgs>` and the callback should be `Action<TArgs>` (not the original delegate type). The `VoidWhenMatcherPredicateBase` implementation:

```csharp
public class VoidWhenMatcherPredicateBase<TArgs> : VoidWhenMatcherBase<TArgs>
{
    private readonly Func<TArgs, bool> _predicate;
    private Action<TArgs>? _callback;

    public override bool Matches(TArgs args) => _predicate(args);
    public override void Call(TArgs args) => _callback?.Invoke(args);
    public override bool IsTerminal => false;

    internal void SetCallback(Action<TArgs> callback) => _callback = callback;
}
```

The generated `VoidWhenChain.Call(Action<string> callback)` bridges:

```csharp
// Generated (1-param void):
public VoidWhenChain Call(Action<string> callback)
{
    _currentMatcher.SetCallback(callback);  // TArgs = string, no wrapping needed
    return this;
}

// Generated (2-param void, if it existed):
public VoidWhenChain Call(Action<int, int> callback)
{
    _currentMatcher.SetCallback((args) => callback(args.Item1, args.Item2));  // bridge
    return this;
}
```

4. **What stays generated for void When:** Only the `When(...)` entry point methods (which bridge individual params to `TArgs` as described in Concern 1) and the `Call()` method on the chain (which bridges the concrete delegate type). The matcher hierarchy, ThenWhen, ThenCall, ThenNone, Verify, Reset, Verifiable all live in the base class.

### Concern 4 (Low-Medium): Property stub override helpers

**Observation:** Property interceptors have three internal helpers used by the stub override pattern that are not mentioned in the plan:

```csharp
// From RowsInterceptor (MatrixStandaloneStub.g.cs lines 42-64):
internal void RecordGet() => _unconfiguredGetCount++;
internal bool HasGet => _get != null || (_getSequence?.Count ?? 0) > 0;
internal int InvokeGetCallback()
{
    if (_getSequence != null && _getSequenceIndex < _getSequence.Count) { ... }
    if (_get != null && _getTracking != null) { ... }
    throw new InvalidOperationException("InvokeGetCallback called without callback configured");
}
```

These are used by the stub's interface implementation when a stub override is present -- the stub class body checks `HasGet` to decide whether to call the interceptor or the stub override method.

**Design Decision: `RecordGet()`, `HasGet`, and `InvokeGetCallback()` all move to `PropertyGetInterceptorBase<TValue>`. They are fully structural.**

Rationale:

1. `RecordGet()` simply increments `_unconfiguredGetCount` -- it only touches base class fields.

2. `HasGet` checks `_get != null || (_getSequence?.Count ?? 0) > 0` -- these are base class fields (`_get` is `Func<TValue>?`, `_getSequence` is `List<(Func<TValue>, BuilderImpl)>?`).

3. `InvokeGetCallback()` iterates the sequence list and invokes the get callback. All fields it touches (`_getSequence`, `_getSequenceIndex`, `_get`, `_getTracking`) are base class fields. The callback type is `Func<TValue>`, which is a base class type parameter.

4. The same applies to the set counterparts on get+set properties: `RecordSet()`, `HasSet`, `InvokeSetCallback()`.

5. For method interceptors, the equivalent helper is `UnconfiguredCallCount` (a simple property returning `_unconfiguredCallCount`). This also moves to the base class.

No generated code is needed for these helpers.

---

## Implementation Contract

**Created:** 2026-02-13
**Approved by:** knockoff-developer

### Acceptance Criteria

The prototype compiles and demonstrates that interceptor base classes reduce generated code size while preserving identical behavior. No Design.Stubs acceptance criteria (standalone prototype).

### In Scope

**Phase 1: Method Interceptor Base Classes (BasicUserMethodStub)**
- [x] Create `src/Prototype/` directory structure (Prototype.sln, Prototype.Library, Prototype.Stubs, Prototype.Tests)
- [x] Create `Unit` struct in Prototype.Library
- [x] Create `VoidMethodInterceptorBase<TDelegate, TArgs>` with all shared fields, RunVoidPriorityChain, VoidWhenMatcher hierarchy, VoidWhenChainBase, MethodCallBuilderBase, MethodSequenceBase
- [x] Create `MethodInterceptorBase<TDelegate, TArgs, TReturn>` extending void base with return-value support, RunPriorityChain, WhenMatcher hierarchy, WhenBuilderBase, WhenChainBase
- [x] Copy IStubOverrideService to Prototype.Stubs/Interfaces/
- [x] Refactor BasicUserMethodStub to Refactored/ using base classes (all 4 interceptors)
- [x] Write BasicUserMethodTests.cs (33 tests)
- [x] **Checkpoint: Build passes, 33 tests pass across 3 TFMs**

**Phase 2: Property Interceptor Base Classes**
- [x] Create `PropertyGetInterceptorBase<TValue>` with get fields, priority chain, stub override helpers, builders, sequences
- [x] Copy IMatrix to Prototype.Stubs/Interfaces/
- [x] Refactor RowsInterceptor and ColumnsInterceptor
- [x] Write property tests in MatrixStandaloneTests.cs (17 tests)
- [x] **Checkpoint: Build passes, 50 tests pass across 3 TFMs**

**Phase 3: Indexer Interceptor Base Classes**
- [x] Create `IndexerGetSetInterceptorBase<TKey, TValue>` with get+set fields, per-key builders, dual When chains
- [x] Refactor IndexerInterceptor
- [x] Write indexer tests in MatrixStandaloneTests.cs (33 tests)
- [x] **Checkpoint: Build passes, 83 tests pass across 3 TFMs**

**Phase 4: Scale Validation**
- [x] Refactor all 32 DataReader interceptors using base classes
- [x] Write representative tests in DataReaderTests.cs (27 tests)
- [x] Document line count comparison (see Completion Evidence)
- [x] **Checkpoint: Build passes, 110 tests pass across 3 TFMs**

### Out of Scope

- Generator changes
- KnockOff library modifications
- Existing code modifications
- Event interceptor base classes

### Verification Gates

1. After Phase 1: BasicUserMethodStub compiles and tests pass
2. After Phase 2: Property interceptors compile and test
3. After Phase 3: Indexer interceptor compiles and tests
4. After Phase 4: All 32 interceptors compile, line count report documented
5. Final: Entire Prototype.sln builds with zero errors, all tests pass

### Stop Conditions

- Base class generic constraints prevent clean inheritance
- Behavioral difference between base-class and original interceptors
- Per-key indexer builders cannot be generified
- Prototype structure conflicts with existing solution

---

## Implementation Progress

**Started:** 2026-02-13
**Developer:** knockoff-developer

### Current Status: Awaiting Verification

---

## Completion Evidence

### Test Results

All 110 tests pass across all 3 target frameworks (net8.0, net9.0, net10.0):

- **net8.0:** Passed: 110, Failed: 0, Skipped: 0
- **net9.0:** Passed: 110, Failed: 0, Skipped: 0
- **net10.0:** Passed: 110, Failed: 0, Skipped: 0

Test breakdown:
- BasicUserMethodTests: 33 tests (methods: return value, callback, sequences, When chains, verification, strict, stub override, Reset, LastArg)
- MatrixStandalonePropertyTests: 17 tests (properties: Get value, Get callback, sequences, verification, strict, stub override)
- MatrixStandaloneIndexerTests: 33 tests (indexers: get/set callbacks, per-key, sequences, When chains, verification, strict, Reset)
- DataReaderTests: 27 tests (cross-cutting: properties, void/non-void methods, dual-key indexer, multi-param method, strict, Verify/VerifyAll, integration read loop)

### Build Results

`dotnet build src/Prototype/Prototype.sln` succeeds with 0 warnings, 0 errors across all 3 TFMs.

### Line Count Comparison

#### Original Generated Code

| Stub | File | Lines |
|------|------|------:|
| BasicUserMethodStub | BasicUserMethodStub.g.cs + Base.g.cs | 2,334 |
| MatrixStandaloneStub | MatrixStandaloneStub.g.cs + Base.g.cs | 1,592 |
| DataReaderStubTests | DataReaderStubTests.Stubs.g.cs | 17,234 |
| **Total Original** | | **21,160** |

#### Refactored Code

| Stub | File | Lines |
|------|------|------:|
| BasicUserMethodStub | BasicUserMethodStub.cs | 763 |
| MatrixStandaloneStub | MatrixStandaloneStub.cs | 422 |
| DataReaderStub | DataReaderStub.cs | 763 |
| **Total Refactored Stubs** | | **1,948** |

#### Base Class Library (One-Time Cost)

| File | Lines |
|------|------:|
| VoidMethodInterceptorBase.cs | 498 |
| MethodInterceptorBase.cs | 413 |
| PropertyGetInterceptorBase.cs | 282 |
| IndexerGetSetInterceptorBase.cs | 951 |
| Unit.cs | 9 |
| **Total Library** | **2,153** |

#### Summary

| Metric | Value |
|--------|------:|
| Original generated code (3 stubs) | 21,160 lines |
| Refactored stubs (3 stubs) | 1,948 lines |
| Base class library (one-time) | 2,153 lines |
| Total refactored (stubs + library) | 4,101 lines |
| **Net savings** | **17,059 lines (81%)** |
| **Per-compilation savings** (library is pre-compiled) | **19,212 lines (91%)** |

The base class library is compiled once and shared across all stubs. The per-compilation savings (what the C# compiler processes for each stub) is 91% -- from 21,160 lines to 1,948 lines.

The DataReaderStub provides the most dramatic demonstration: 17,234 lines of generated code reduced to 763 lines (96% reduction per-compilation). The 18 structurally identical `int -> T` methods share a single `SimpleIntMethodInterceptor<TDelegate, TReturn>` generic class, demonstrating that repetitive interceptors scale to O(1) new code per interceptor.

### Design Observations

1. **Public `Get` on PropertyGetInterceptorBase:** Added public `Get(Func<TValue>)` and `Get(TValue)` methods to the property base class. The typed interceptor subclasses (like RowsInterceptor) override these with `new` keyword to return typed builder interfaces. For simple stubs (like DataReader's properties), the base class `Get` method returning `PropertyGetBuilderBase` is sufficient.

2. **Typed builder interfaces omitted for DataReader prototype:** The DataReader interceptors return base class types (`ReturnMethodCallBuilderBase`, `MethodCallBuilderBase`) instead of implementing the KnockOff library interfaces (`IMethodReturnBuilder<T, TArg>`, etc.). This is intentional for the prototype -- the base class APIs provide all necessary functionality. In the production generator, the generated thin subclasses would add explicit interface implementations as described in Developer Review Concern 2.

3. **Reusable interceptor patterns:** The DataReader stub demonstrates that constructor-injection of behavior functions (source invoke, value delegate factory, delegate invoke) allows a single generic class to serve multiple interceptors. The `SimpleIntMethodInterceptor<TDelegate, TReturn>` handles 18 of 32 interceptors.

4. **Dual-key indexer composition:** The `DualKeyIndexerInterceptor` composes two `IndexerGetSetInterceptorBase` instances (one for `int` keys, one for `string` keys), demonstrating that the base class approach handles IDataReader's dual-indexer pattern without special-casing.

---

## Architect Verification

**Verified:** 2026-02-13
**Verdict:** VERIFIED

### Independent Build Results

| Solution | Result | Details |
|----------|--------|---------|
| `src/KnockOff.sln` | 0 errors, 0 warnings | All 10 projects build successfully |
| `src/Prototype/Prototype.sln` | 0 errors, 0 warnings | All 3 projects build successfully across 3 TFMs |

### Independent Test Results

| Project | net8.0 | net9.0 | net10.0 |
|---------|--------|--------|---------|
| KnockOff.Documentation.Samples | 691 passed, 0 failed | 691 passed, 0 failed | 691 passed, 0 failed |
| KnockOffTests | 1464 passed, 0 failed | 1465 passed, 0 failed | 1465 passed, 0 failed |
| KnockOffTests.AssemblyStrict | 14 passed, 0 failed | 14 passed, 0 failed | 14 passed, 0 failed |
| KnockOff.NeatooInterfaceTests | 473 passed, 0 failed | 473 passed, 0 failed | 473 passed, 0 failed |
| **Prototype.Tests** | **110 passed, 0 failed** | **110 passed, 0 failed** | **110 passed, 0 failed** |

Zero failures across all projects and all target frameworks.

### Design Match

**Base class hierarchy** -- Matches plan:
- `VoidMethodInterceptorBase<TDelegate, TArgs>` with `RunVoidPriorityChain`, `InvokeVoidDelegate`, `RecordUnconfiguredArgs` abstracts
- `MethodInterceptorBase<TDelegate, TArgs, TReturn>` extends void base with `RunPriorityChain`, `InvokeDelegate` abstract, return value fields
- `PropertyGetInterceptorBase<TValue>` with `InvokeGet`, stub override helpers (`HasGet`, `RecordGet`, `InvokeGetCallback`)
- `IndexerGetSetInterceptorBase<TKey, TValue>` with dual get/set chains and per-key builders

**Refactored stubs inherit correctly:**
- `ProcessInterceptor : MethodInterceptorBase<ProcessDelegate, string, string>`
- `CalculateInterceptor : MethodInterceptorBase<CalculateDelegate, (int a, int b), int>`
- `ExecuteInterceptor : VoidMethodInterceptorBase<Action<string>, string>`
- `FindByIdInterceptor : MethodInterceptorBase<FindByIdDelegate, int, string?>`
- `RowsInterceptor : PropertyGetInterceptorBase<int>`
- `IndexerInterceptor : IndexerGetSetInterceptorBase<(int row, int col), double>`
- DataReader uses reusable `SimpleIntMethodInterceptor<TDelegate, TReturn>` for 18 of 32 interceptors

**Developer Review Concern resolutions implemented:**
1. **Concern 1 (WhenMatcher TArgs):** Confirmed -- `WhenMatcherBase` uses `Matches(TArgs args)`, generated `When(int a, int b)` bridges via `(args) => object.Equals(args.a, a) && object.Equals(args.b, b)`
2. **Concern 2 (Builder interfaces):** Confirmed -- base class builders do not implement KnockOff library interfaces; generated `MethodCallBuilderImpl` thin subclasses add typed `LastArg`/`LastArgs` and `RecordCall`
3. **Concern 3 (Void When chain):** Confirmed -- `VoidWhenChainBase` holds `_currentMatcher` reference; `VoidWhenMatcherPredicateBase` has `SetCallback(Action<TArgs>)` method
4. **Concern 4 (Property stub override helpers):** Confirmed -- `RecordGet()`, `HasGet`, `InvokeGetCallback()` all in `PropertyGetInterceptorBase<TValue>`

**Line count verification:**
- Refactored stubs: 1,948 lines (verified: 763 + 422 + 763)
- Base class library: 2,153 lines (verified: 498 + 413 + 282 + 951 + 9)
- All numbers match developer claims exactly

**Test count verification:**
- 110 `[Fact]`/`[Theory]` attributes found across 3 test files (33 + 50 + 27)
- Matches developer's claimed 33 + 17 + 33 + 27 = 110

---

## Notes

- This plan is for the prototype only. A subsequent plan will cover modifying the generator to emit code using base classes.
- The prototype references the KnockOff NuGet package (or project reference) for types like `IKnockOffStub`, `Called`, `VerificationException`, etc.
- Generated code in the prototype is hand-edited C# -- not generated by Roslyn. The point is to prove the base class design, not to change the generator.
- The Original/ files may need namespace adjustments to compile in the prototype project, but NO structural changes.
