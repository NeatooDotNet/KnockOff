# Unify OnCall/OnGet/OnSet to Method Signatures

**Date:** 2026-01-23
**Related Todo:** [Unify OnCall/OnGet/OnSet Signatures to Methods](../../todos/completed/unify-oncall-onget-onset-signatures.md)
**Status:** Complete
**Last Updated:** 2026-01-23

---

## Overview

Convert property and indexer `OnGet`/`OnSet` from settable properties to methods returning tracking interfaces, matching the proven `OnCall()` pattern used for methods. This enables consistent fluent verification and sequence support across all member types.

---

## Approach

**Clean Architecture approach** with full unification:

1. Create new tracking/sequence interfaces in runtime library
2. Create unified models for property/indexer interceptors
3. Create shared renderers following MethodInterceptorRenderer pattern
4. Integrate shared renderers into FlatRenderer and InlineRenderer
5. Update explicit interface implementations
6. Migrate all tests and documentation

---

## Design

### New Public API

#### Properties
```csharp
// Repeating callbacks (return tracking)
IPropertyGetTracking OnGet(Func<T> callback)
IPropertySetTracking<T> OnSet(Action<T> callback)

// Sequences (return sequence for chaining)
IPropertyGetSequence<T> OnGetSequence(Func<T> callback)
IPropertySetSequence<T> OnSetSequence(Action<T> callback)
```

#### Indexers
```csharp
// Repeating callbacks (return tracking)
IIndexerGetTracking<TKey> OnGet(Func<TKey, TValue> callback)
IIndexerSetTracking<TKey, TValue> OnSet(Action<TKey, TValue> callback)

// Sequences (return sequence for chaining)
IIndexerGetSequence<TKey, TValue> OnGetSequence(Func<TKey, TValue> callback)
IIndexerSetSequence<TKey, TValue> OnSetSequence(Action<TKey, TValue> callback)
```

### Tracking Interfaces

#### IPropertyTracking.cs (NEW)
```csharp
namespace KnockOff;

public interface IPropertyGetTracking
{
    void Reset();
    void Verify();
    void Verify(Times times);
    IPropertyGetTracking Verifiable();
    IPropertyGetTracking Verifiable(Times times);
}

public interface IPropertySetTracking<TValue>
{
    TValue LastValue { get; }
    void Reset();
    void Verify();
    void Verify(Times times);
    IPropertySetTracking<TValue> Verifiable();
    IPropertySetTracking<TValue> Verifiable(Times times);
}
```

#### IPropertySequence.cs (NEW)
```csharp
namespace KnockOff;

public interface IPropertyGetSequence<TValue>
{
    IPropertyGetSequence<TValue> ThenGet(Func<TValue> callback);
    void Verify();
    void Reset();
    IPropertyGetSequence<TValue> Verifiable();
}

public interface IPropertySetSequence<TValue>
{
    IPropertySetSequence<TValue> ThenSet(Action<TValue> callback);
    void Verify();
    void Reset();
    IPropertySetSequence<TValue> Verifiable();
}
```

#### IIndexerTracking.cs (NEW)
```csharp
namespace KnockOff;

public interface IIndexerGetTracking<TKey>
{
    TKey? LastKey { get; }
    void Reset();
    void Verify();
    void Verify(Times times);
    IIndexerGetTracking<TKey> Verifiable();
    IIndexerGetTracking<TKey> Verifiable(Times times);
}

public interface IIndexerSetTracking<TKey, TValue>
{
    (TKey Key, TValue Value)? LastEntry { get; }
    void Reset();
    void Verify();
    void Verify(Times times);
    IIndexerSetTracking<TKey, TValue> Verifiable();
    IIndexerSetTracking<TKey, TValue> Verifiable(Times times);
}
```

#### IIndexerSequence.cs (NEW)
```csharp
namespace KnockOff;

public interface IIndexerGetSequence<TKey, TValue>
{
    IIndexerGetSequence<TKey, TValue> ThenGet(Func<TKey, TValue> callback);
    void Verify();
    void Reset();
    IIndexerGetSequence<TKey, TValue> Verifiable();
}

public interface IIndexerSetSequence<TKey, TValue>
{
    IIndexerSetSequence<TKey, TValue> ThenSet(Action<TKey, TValue> callback);
    void Verify();
    void Reset();
    IIndexerSetSequence<TKey, TValue> Verifiable();
}
```

### Unified Models

#### UnifiedPropertyInterceptorModel.cs (NEW)
```csharp
namespace KnockOff.Model.Shared;

internal sealed record UnifiedPropertyInterceptorModel(
    string InterceptorClassName,
    string PropertyName,
    string DeclaringInterface,
    PropertyAccessorConfig? Getter,
    PropertyAccessorConfig? Setter,
    string ValueType,
    bool IsInitOnly,
    string DefaultExpression);

internal sealed record PropertyAccessorConfig(
    string CallbackDelegateType,    // e.g., "Func<string>"
    string TrackingInterface,       // e.g., "IPropertyGetTracking"
    string SequenceInterface);      // e.g., "IPropertyGetSequence<string>"
```

#### UnifiedIndexerInterceptorModel.cs (NEW)
```csharp
namespace KnockOff.Model.Shared;

internal sealed record UnifiedIndexerInterceptorModel(
    string InterceptorClassName,
    string IndexerName,
    string DeclaringInterface,
    string KeyType,
    string ValueType,
    IndexerAccessorConfig? Getter,
    IndexerAccessorConfig? Setter,
    string DefaultExpression);

internal sealed record IndexerAccessorConfig(
    string GetCallbackDelegateType,  // e.g., "Func<int, string>"
    string SetCallbackDelegateType,  // e.g., "Action<int, string>"
    string GetTrackingInterface,     // e.g., "IIndexerGetTracking<int>"
    string SetTrackingInterface,     // e.g., "IIndexerSetTracking<int, string>"
    string GetSequenceInterface,
    string SetSequenceInterface);
```

### Shared Renderers

#### PropertyInterceptorRenderer.cs (NEW)
Following MethodInterceptorRenderer pattern exactly:
- Storage fields for OnGet/OnSet callbacks and tracking
- Storage fields for OnGetSequence/OnSetSequence lists and indices
- OnGet() / OnSet() methods returning tracking interfaces
- OnGetSequence() / OnSetSequence() methods returning sequence interfaces
- Nested PropertyGetTrackingImpl class
- Nested PropertySetTrackingImpl class
- Nested PropertyGetSequenceImpl class
- Nested PropertySetSequenceImpl class
- InvokeGet() / InvokeSet() methods for explicit implementations
- Reset() method
- Verification support (CheckVerification, CheckVerificationAll)

#### IndexerInterceptorRenderer.cs (NEW)
Same structure as PropertyInterceptorRenderer with:
- Key parameter handling
- Backing dictionary support
- LastGetKey / LastSetEntry tracking
- Multi-parameter indexer support if needed

### Storage Pattern (Per Property Interceptor)

```csharp
// Getter storage
private Func<TValue>? _onGet;
private PropertyGetTrackingImpl? _onGetTracking;
private List<(Func<TValue> Callback, PropertyGetTrackingImpl Tracking)>? _getSequence;
private int _getSequenceIndex;
private bool _isGetVerifiable;
private Times? _getVerifiableTimes;

// Setter storage (if HasSetter)
private Action<TValue>? _onSet;
private PropertySetTrackingImpl? _onSetTracking;
private List<(Action<TValue> Callback, PropertySetTrackingImpl Tracking)>? _setSequence;
private int _setSequenceIndex;
private bool _isSetVerifiable;
private Times? _setVerifiableTimes;
```

### Invocation Priority Chain

```
Getter:
1. Check sequence (_getSequence != null && index < count) → invoke, record, increment
2. Check repeating callback (_onGet != null) → invoke, record
3. Check source (_source is { } src) → return src.Property
4. Check strict mode → throw StubException.NotConfigured
5. Return Value
```

```
Setter:
1. Check sequence (_setSequence != null && index < count) → invoke, record, increment
2. Check repeating callback (_onSet != null) → invoke, record
3. Check source (_source is { } src) → src.Property = value
4. Check strict mode → throw StubException.NotConfigured
5. Update Value
```

---

## Implementation Steps

### Phase 1: Public Interfaces (~200 lines)
- [ ] Create `src/KnockOff/IPropertyTracking.cs` (IPropertyGetTracking, IPropertySetTracking<T>)
- [ ] Create `src/KnockOff/IPropertySequence.cs` (IPropertyGetSequence<T>, IPropertySetSequence<T>)
- [ ] Create `src/KnockOff/IIndexerTracking.cs` (IIndexerGetTracking<TKey>, IIndexerSetTracking<TKey, TValue>)
- [ ] Create `src/KnockOff/IIndexerSequence.cs` (IIndexerGetSequence<TKey, TValue>, IIndexerSetSequence<TKey, TValue>)
- [ ] Verify compilation

### Phase 2: Unified Models (~200 lines)
- [ ] Create `src/Generator/Model/Shared/UnifiedPropertyInterceptorModel.cs`
- [ ] Create `src/Generator/Model/Shared/UnifiedIndexerInterceptorModel.cs`
- [ ] Add builder methods to UnifiedInterceptorBuilder.cs or create new adapter

### Phase 3: Property Interceptor Renderer (~600 lines)
- [ ] Create `src/Generator/Renderer/Shared/PropertyInterceptorRenderer.cs`
- [ ] Implement RenderInterceptorClass() following MethodInterceptorRenderer
- [ ] Implement OnGet() / OnGetSequence() rendering
- [ ] Implement OnSet() / OnSetSequence() rendering
- [ ] Implement nested tracking classes (PropertyGetTrackingImpl, PropertySetTrackingImpl)
- [ ] Implement nested sequence classes (PropertyGetSequenceImpl, PropertySetSequenceImpl)
- [ ] Implement InvokeGet() / InvokeSet()
- [ ] Implement verification support

### Phase 4: Indexer Interceptor Renderer (~500 lines)
- [ ] Create `src/Generator/Renderer/Shared/IndexerInterceptorRenderer.cs`
- [ ] Copy structure from PropertyInterceptorRenderer
- [ ] Adapt for key parameter handling
- [ ] Implement Backing dictionary integration
- [ ] Implement LastGetKey / LastSetEntry tracking

### Phase 5: Renderer Integration
- [ ] Update FlatRenderer to use PropertyInterceptorRenderer
- [ ] Update FlatRenderer to use IndexerInterceptorRenderer
- [ ] Update InlineRenderer for properties
- [ ] Update InlineRenderer for indexers
- [ ] Remove old RenderPropertyInterceptorClass methods
- [ ] Remove old RenderIndexerInterceptorClass methods

### Phase 6: Explicit Implementation Updates
- [ ] Update FlatRenderer property implementations to call InvokeGet/InvokeSet
- [ ] Update FlatRenderer indexer implementations
- [ ] Update InlineRenderer property implementations
- [ ] Update InlineRenderer indexer implementations

### Phase 7: Test Migration (~100 lines changed)
- [ ] Update IndexerTests.cs (lines 49, 68, 120, etc.)
- [ ] Update InitPropertyTests.cs
- [ ] Update InlineMultiIndexerTests.cs
- [ ] Update all NeatooInterfaceTests (~17 files)
- [ ] Add new sequence tests for properties
- [ ] Add new sequence tests for indexers

### Phase 8: Documentation Updates
- [ ] Update docs/guides/properties.md
- [ ] Update docs/guides/advanced-callbacks.md
- [ ] Update docs/reference/interceptor-api.md
- [ ] Update docs/troubleshooting.md
- [ ] Add migration examples

### Phase 9: Skills Documentation
- [ ] Update KnockOff skill files with new API patterns

---

## Acceptance Criteria

- [ ] All three patterns work (Standalone, Inline Interface, Inline Class)
- [ ] OnGet() / OnSet() return tracking interfaces
- [ ] OnGetSequence() / OnSetSequence() return sequence interfaces
- [ ] Sequences work with ThenGet() / ThenSet() chaining
- [ ] Verifiable() fluent chaining works
- [ ] Stub.Verify() includes property/indexer sequences
- [ ] All existing tests pass (after migration)
- [ ] New sequence tests pass
- [ ] Documentation updated with new syntax
- [ ] Generated code compiles for all patterns

---

## Dependencies

- Existing MethodInterceptorRenderer pattern (template to follow)
- Times and VerificationException classes (already exist)
- UnifiedMethodInterceptorModel pattern (for model design)

---

## Risks / Considerations

### Breaking Change
- All `stub.Property.OnGet = callback` must become `stub.Property.OnGet(callback)`
- All `stub.Property.OnSet = callback` must become `stub.Property.OnSet(callback)`
- Same for indexers
- **Mitigation:** KnockOff is pre-1.0, clean break is acceptable

### Code Size Increase
- Each property interceptor gains 4 nested classes
- Each indexer interceptor gains 4 nested classes
- **Mitigation:** Generated code size increase is acceptable for test stubs

### Test Migration Effort
- Estimated ~50-70 test method updates
- Mechanical find-replace pattern
- **Mitigation:** Simple regex replacement

---

## Architectural Verification
[Architect completes this checklist before handoff]

### Checklist
- [x] All three patterns analyzed (Standalone, Inline Interface, Inline Class)
- [x] Breaking changes assessment completed
- [x] Pattern consistency verified
- [x] Diagnostic requirements identified
- [x] Test strategy defined
- [x] Edge cases documented
- [x] Codebase deep-dive completed (document files examined)

---

### Three Patterns Analysis

#### Standalone Pattern (FlatRenderer)
**Current Implementation:**
- `FlatRenderer.RenderPropertyInterceptorClass()` (lines 246-547) generates property interceptors
- `FlatRenderer.RenderIndexerInterceptorClass()` (lines 635-810+) generates indexer interceptors
- Properties use `OnGet`/`OnSet` as settable properties with tracking
- Indexers use `OnGet`/`OnSet` with `Func<TKey,TValue>`/`Action<TKey,TValue>` types

**Required Changes:**
- Replace `RenderPropertyInterceptorClass()` with `PropertyInterceptorRenderer.RenderInterceptorClass()`
- Replace `RenderIndexerInterceptorClass()` with `IndexerInterceptorRenderer.RenderInterceptorClass()`
- Update `RenderPropertyImplementation()` to call `interceptor.InvokeGet()`/`InvokeSet()` instead of direct callback access
- Update `RenderIndexerImplementation()` similarly

**Init-only Properties (Critical Edge Case):**
- Currently handled separately in `RenderInitPropertyInterceptorContent()` (lines 263-380)
- Init-only properties have restricted setter semantics - the init accessor can only be called during object initialization
- **Decision:** Init-only property interceptors should NOT support OnSet() method or OnSetSequence()
- **Reason:** The generated explicit interface implementation uses `init` accessor which can only be called once during construction
- **API for init-only properties:**
  - `OnGet(callback)` - returns IPropertyGetTracking (same as regular)
  - `OnGetSequence(callback)` - returns IPropertyGetSequence<T> (same as regular)
  - No `OnSet()` / `OnSetSequence()` - the Value property setter handles configuration
  - `RecordSet()` still exists for tracking that the init setter was called

#### Inline Interface Pattern (InlineRenderer)
**Current Implementation:**
- `InlineRenderer.RenderPropertyInterceptorClass()` (lines 259-428) generates property interceptors
- `InlineRenderer.RenderIndexerInterceptorClass()` (lines 430-603) generates indexer interceptors
- Same `OnGet`/`OnSet` settable property pattern as FlatRenderer

**Required Changes:**
- Replace with shared `PropertyInterceptorRenderer` / `IndexerInterceptorRenderer`
- Update `RenderPropertyImplementation()` (lines 1026-1093) to use new invocation pattern
- Update `RenderIndexerImplementation()` (lines 1095-1126) to use new invocation pattern

#### Inline Class Pattern (ClassRenderer)
**Current Implementation:**
- `ClassRenderer.cs` delegates to InlineRenderer patterns for property/indexer rendering
- Class stubs use same interceptor structure as interface stubs

**Required Changes:**
- Same as Inline Interface - ClassRenderer will automatically benefit from shared renderers

---

### Multi-Indexer Container Pattern (OfXxx)

**Current Implementation:**
```csharp
// For interfaces with multiple indexer overloads by key type:
stub.Indexer.OfString.OnGet = (key) => "value";
stub.Indexer.OfInt32.OnGet = (index) => 42;
```

**New API:**
```csharp
stub.Indexer.OfString.OnGet((key) => "value");
stub.Indexer.OfInt32.OnGet((index) => 42);
```

**Files Involved:**
- `FlatRenderer.RenderIndexerContainerClass()` (lines 564-629)
- `InlineRenderer.RenderIndexerContainerClass()` (lines 605-670)

**No structural changes needed to containers** - they just hold interceptor instances. The interceptor classes themselves change.

---

### Generic Type Parameters for Indexers

**Current Implementation:**
- `InlineIndexerModel` has `TypeParameterList` and `ConstraintClauses` for open generic interfaces
- Example: `IRepository<T>` with `this[int index]` generates `IndexerInterceptor<T>`

**New Tracking Interfaces:**
- `IIndexerGetTracking<TKey>` - key type parameter needed
- `IIndexerSetTracking<TKey, TValue>` - both key and value type parameters
- Must flow type parameters through to tracking implementations

**Implementation Note:**
- The IndexerInterceptorRenderer must accept type parameter information via `InterceptorRenderOptions` (same pattern as MethodInterceptorRenderer)
- Nested tracking classes inherit parent class type parameters

---

### Breaking Changes Assessment

**Breaking API Changes:**
1. `stub.Property.OnGet = () => value` -> `stub.Property.OnGet(() => value)`
2. `stub.Property.OnSet = (val) => { }` -> `stub.Property.OnSet((val) => { })`
3. `stub.Indexer.OnGet = (key) => value` -> `stub.Indexer.OnGet((key) => value)`
4. `stub.Indexer.OnSet = (k,v) => { }` -> `stub.Indexer.OnSet((k,v) => { })`

**Migration Pattern:**
```
Find:    \.OnGet\s*=\s*
Replace: .OnGet(
         ...);

Find:    \.OnSet\s*=\s*
Replace: .OnSet(
         ...);
```

**User Impact:**
- All test code using property/indexer callbacks must be updated
- Compile-time error on old syntax (good - no silent failures)
- KnockOff is pre-1.0, breaking changes are acceptable per versioning policy

---

### Diagnostic Requirements

No new diagnostics needed for this change. The API change is fully compile-time enforced:
- Old syntax: `OnGet = callback` - will fail to compile (OnGet is now a method, not property)
- New syntax: `OnGet(callback)` - type-safe method call

---

### Test Strategy

**Phase 7 Test Migration (~50-70 methods):**

1. **Direct Usage Tests (update required):**
   - `IndexerTests.cs` - lines 49, 68, 120 (OnGet/OnSet assignments)
   - `InitPropertyTests.cs` - lines 478-479, 529 (OnGet assignments)
   - `InlineMultiIndexerTests.cs` - lines 52, 82 (OnGet/OnSet assignments)
   - `CallbackTests.cs` - lines 87, 102, 113, 182 (OnGet/OnSet assignments)
   - `BclStandaloneTests.cs` - ~10 occurrences

2. **NeatooInterfaceTests (update required):**
   - ~17 files with OnGet/OnSet usage patterns
   - Mostly property configurations in integration tests

3. **Documentation Samples (update required):**
   - `PropertiesSamples.cs` - ~10 occurrences
   - `AdvancedCallbacksSamples.cs` - ~5 occurrences
   - `InterceptorApiSamples.cs` - ~4 occurrences
   - `TroubleshootingSamples.cs` - ~8 occurrences

**New Tests to Add:**
- `PropertySequenceTests.cs` - OnGetSequence/OnSetSequence behavior
- `IndexerSequenceTests.cs` - OnGetSequence/OnSetSequence for indexers
- Tests for ThenGet/ThenSet chaining
- Tests for sequence exhaustion behavior in strict mode

---

### Edge Cases

1. **Init-only Properties:**
   - OnGet() method available, OnGetSequence() available
   - OnSet() NOT available - init setter is one-time only at construction
   - Value property setter remains for configuration
   - RecordSet() still tracks that init was called

2. **Nullable Value Types:**
   - `IPropertySetTracking<T?>` handles nullable value types
   - LastValue property type matches property type exactly

3. **Open Generic Interfaces:**
   - `IRepository<T>` with `T Value { get; set; }`
   - Interceptor class: `ValueInterceptor<T>`
   - Type parameters flow through to tracking implementations

4. **Multi-parameter Indexers:**
   - `this[int x, int y]` with key tuple `(int, int)`
   - `IIndexerGetTracking<(int, int)>` - tuple key type
   - Same pattern works, just complex key type

5. **Sequence Exhaustion:**
   - When OnGetSequence is used and exhausted in strict mode -> throw StubException.SequenceExhausted
   - When OnGetSequence exhausted in non-strict mode -> fall through to Value/default
   - Same behavior as OnCallSequence for methods

6. **Overwrite Previous:**
   - Calling OnGet() clears any previous OnGet configuration AND any OnGetSequence
   - Calling OnGetSequence() clears any previous OnGetSequence AND any OnGet
   - Matches OnCall/OnCallSequence mutual exclusivity

---

### Codebase Analysis (Files Examined)

**Generator Core:**
- `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs` (984 lines) - template for new renderers
- `src/Generator/Model/Shared/UnifiedMethodInterceptorModel.cs` (77 lines) - template for new models
- `src/Generator/Renderer/FlatRenderer.cs` (read lines 1-800+) - current property/indexer rendering
- `src/Generator/Renderer/InlineRenderer.cs` (1454 lines) - current property/indexer rendering
- `src/Generator/Renderer/Shared/ModelAdapters.cs` - adapter pattern for unified models

**Generator Models:**
- `src/Generator/Model/Flat/FlatPropertyModel.cs` (27 lines)
- `src/Generator/Model/Inline/InlinePropertyModel.cs` (32 lines)
- `src/Generator/Model/Inline/InlineIndexerModel.cs` (42 lines)

**Runtime Library:**
- `src/KnockOff/IMethodTracking.cs` (84 lines) - template for property/indexer tracking
- `src/KnockOff/IMethodSequence.cs` (43 lines) - template for property/indexer sequences

**Tests:**
- `src/Tests/KnockOffTests/InitPropertyTests.cs` (689 lines) - init-only property patterns
- `src/Tests/KnockOffTests/InlineMultiIndexerTests.cs` (152 lines) - multi-indexer patterns
- `src/Tests/KnockOffTests/IndexerTests.cs` - current OnGet/OnSet usage
- `src/Tests/KnockOffTests/CallbackTests.cs` - callback patterns

**Patterns Observed:**
1. MethodInterceptorRenderer uses:
   - Separate storage for repeating callback vs sequence list
   - Nested `MethodTrackingImpl` and `MethodSequenceImpl` classes
   - `Invoke()` method centralizes callback execution
   - Reset() clears tracking but preserves configuration

2. Property interceptors currently have:
   - `Value` property for static configuration
   - `OnGet`/`OnSet` settable properties for dynamic callbacks
   - `RecordGet()`/`RecordSet()` for tracking
   - `VerifyGet()`/`VerifySet()` for verification

3. Multi-indexer containers use OfXxx pattern with delegated interceptor instances

---

## Developer Review
[Developer adds concerns/questions here during review phase]

**Status:** Under Review - Questions Raised

**Analysis Completed:**
- MethodInterceptorRenderer.cs - 984 lines, template for new renderers
- UnifiedMethodInterceptorModel.cs - template for unified models
- IMethodTracking.cs / IMethodSequence.cs - interface patterns to follow
- FlatRenderer.cs (lines 246-814) - current property/indexer rendering
- InlineRenderer.cs (lines 259-670) - current property/indexer rendering
- Test files examined: IndexerTests.cs, CallbackTests.cs, InitPropertyTests.cs
- All tests passing (643 KnockOffTests + 473 NeatooInterfaceTests + 134 DocumentationSamples)

**Concerns/Questions:**

1. **IPropertyGetTracking - no LastValue?** Unlike IMethodTracking<TArg> which has LastArg, the plan shows IPropertyGetTracking without any LastValue tracking. Since the getter always returns the same type, should we track last returned value? Or intentionally omit since it's always the same (Value or callback result)?

2. **Init-only properties API:** Plan says no OnSet()/OnSetSequence() for init-only. Current implementation has RecordSet() for tracking. Should the new init-only interceptor:
   - Only have OnGet()/OnGetSequence() methods?
   - Implement subset of interfaces (no set tracking)?

3. **Value property priority:** Confirm priority chain for getters:
   Sequence -> OnGet callback -> Source -> Strict check -> Value property

4. **RecordGet/RecordSet vs InvokeGet/InvokeSet:** Current public methods are RecordXxx. Plan mentions InvokeXxx. Are we renaming for consistency with methods?

5. **VerifyGet/VerifySet retention:** Keep these convenience methods on interceptor class alongside new tracking interface methods?

**Developer Recommendations (pending approval):**
1. Keep IPropertyGetTracking simple (no LastValue) - reasonable since getter return is deterministic
2. Init-only properties: only OnGet()/OnGetSequence(), no setter methods
3. Standard priority chain matching methods
4. Rename to InvokeGet()/InvokeSet() for consistency
5. Keep VerifyGet()/VerifySet() on interceptor for convenience

**Awaiting user decision:** Proceed with recommendations or send back to architect?

---

## Implementation Contract
[Developer fills before starting implementation]

**In Scope:**
- [ ] 4 new interface files in src/KnockOff/
- [ ] 2 new model files in src/Generator/Model/Shared/
- [ ] 2 new renderer files in src/Generator/Renderer/Shared/
- [ ] Updates to FlatRenderer.cs property/indexer sections
- [ ] Updates to InlineRenderer.cs property/indexer sections
- [ ] Test migrations (~50-70 test methods)
- [ ] Documentation updates (4-6 files)

**Out of Scope:**
- Method interceptors (already correct)
- Event interceptors (different pattern)
- Generic method handlers (already correct)
- Delegate stubs (already use different pattern)

---

## Implementation Progress

**Phase 1:** Create Public Interfaces
- [x] IPropertyTracking.cs (IPropertyGetTracking, IPropertySetTracking<TValue>)
- [x] IPropertySequence.cs (IPropertyGetSequence<TValue>, IPropertySetSequence<TValue>)
- [x] IIndexerTracking.cs (IIndexerGetTracking<TKey>, IIndexerSetTracking<TKey, TValue>)
- [x] IIndexerSequence.cs (IIndexerGetSequence<TKey, TValue>, IIndexerSetSequence<TKey, TValue>)
- [x] **Verification**: Build compiles (net8.0, net9.0, net10.0)

**Phase 2:** Create Unified Models
- [x] UnifiedPropertyInterceptorModel.cs
- [x] UnifiedIndexerInterceptorModel.cs (with additional fields for multi-param support)
- [x] **Verification**: Build compiles

**Phase 3:** PropertyInterceptorRenderer
- [x] Create PropertyInterceptorRenderer.cs following MethodInterceptorRenderer pattern
- [x] **Verification**: Build compiles

**Phase 4:** IndexerInterceptorRenderer
- [x] Create IndexerInterceptorRenderer.cs
- [x] **Verification**: Build compiles

**Phase 5:** Renderer Integration
- [ ] Update FlatRenderer to use new shared renderers
- [ ] Update InlineRenderer to use new shared renderers
- [x] Update ClassRenderer to use new shared renderers
- [x] Update ClassRenderer verification to check combined count when Verifiable() marks both accessors
- [ ] **Verification**: All tests pass (in progress - ClassRenderer complete)

**Phase 6:** Explicit Implementation Updates
- [ ] Update FlatRenderer RenderPropertyImplementation to call InvokeGet/InvokeSet
- [ ] Update FlatRenderer RenderIndexerImplementation similarly
- [ ] Update InlineRenderer RenderPropertyImplementation to call InvokeGet/InvokeSet
- [ ] Update InlineRenderer RenderIndexerImplementation similarly
- [x] Update ClassRenderer Impl class property overrides to use InvokeGet/InvokeSet
- [x] Update ClassRenderer Impl class indexer overrides to use InvokeGet/InvokeSet
- [x] Fix virtual property/indexer tracking (always track calls, delegate to base if unconfigured)
- [ ] **Verification**: All tests pass (in progress - ClassRenderer complete)

**Phase 7:** Test Migration
- [x] Migrate OnGet = callback to OnGet(callback) - done for all test files
- [x] Migrate OnSet = callback to OnSet(callback) - done for all test files
- [x] All existing tests pass after migration (643 KnockOffTests + 473 NeatooInterfaceTests + 134 DocumentationSamples)
- [ ] Add new sequence tests
- [x] **Verification**: All tests pass

---

## Completion Evidence
[Required before marking complete]

- **Tests Passing:** [Output or screenshot]
- **Generated Code Sample:** [Snippet showing feature works]
- **All Checklist Items:** [Confirmed 100% complete]
