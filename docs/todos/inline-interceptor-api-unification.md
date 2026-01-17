# Inline Interceptor API Unification

**Status:** In Progress (Methods Complete, Indexers Pending)
**Priority:** High
**Created:** 2026-01-16
**Updated:** 2026-01-17

---

## Problem

Standalone stubs and inline stubs have different APIs for methods, despite the design intent being unified:

| Stub Type  | OnCall Syntax | Tracking Access |
|------------|---------------|-----------------|
| **Standalone** | `var tracking = stub.Method.OnCall((ko) => ...)` | Via returned tracking object |
| **Inline** | `stub.Method.OnCall = (ko) => ...` | Direct: `stub.Method.CallCount` |

The interceptor API redesign (completed for standalone in PR #4) was **partially implemented**:

| Member Type | Standalone | Inline | Status |
|-------------|------------|--------|--------|
| Regular methods | OnCall() method | OnCall = assignment | **Inline needs update** |
| Generic methods | OnCall = assignment | OnCall = assignment | **Both need update** |
| Indexers | Direct access | Direct access | **Both need OfXxx pattern** |
| Properties | Assignment-based | Assignment-based | Correct (by design) |
| Events | Current design | Current design | Correct (by design) |

---

## Features to Implement

From the design doc (`docs/plans/2026-01-16-interceptor-api-redesign.md`):

### Methods (Regular & Generic)

1. **OnCall() as method** - Returns `IMethodTracking` instead of being a settable property
2. **OnCall(callback, Times)** - Overload that returns `IMethodSequence` for sequencing
3. **ThenCall() chaining** - For multi-step sequences
4. **Times support** - `Times.Once`, `Times.Twice`, `Times.Exactly(n)`, `Times.Forever`
5. **Verify()** - On interceptor and stub level
6. **Sequence exhaustion** - Throws when sequence runs out
7. **Method overload resolution** - Compiler resolves by callback signature

### Generic Methods

Per the design (line 180-190):
```csharp
var tracking = stub.Deserialize.Of<User>().OnCall((json) => new User());
tracking.CallCount;
tracking.LastArg;
```

The `.Of<T>()` access pattern exists, but the returned typed handler should use OnCall() method, not assignment.

### Indexers

Per the design (lines 145-162), use **OfXxx pattern** for stable naming:
```csharp
// Current (breaks when adding overloads)
stub.Indexer.GetCount

// Target (stable)
stub.Indexer.OfInt32.GetCount
stub.Indexer.OfString.GetCount
```

---

## Scope

### In Scope

**Inline Stubs:**
- [ ] Regular method interceptors - OnCall() returning IMethodTracking
- [ ] Generic method interceptors - OnCall() returning IMethodTracking
- [ ] Indexer interceptors - OfXxx pattern
- [ ] Method overload handling (remove numeric suffixes)
- [ ] Stub-level Verify() method

**Standalone Stubs (to complete the redesign):**
- [ ] Generic method interceptors - OnCall() returning IMethodTracking
- [ ] Indexer interceptors - OfXxx pattern

### Out of Scope (Correct by Design)

- Properties - Assignment-based (`OnGet =`, `OnSet =`, `Value =`)
- Events - Current design (`Raise()`, `AddCount`, `HasSubscribers`)
- Delegate stubs - No overload concerns, simple callbacks

---

## Tasks

### Phase 1: Inline Regular Methods ✅

- [x] Update `InlineMethodModel` to match `FlatMethodModel` structure
- [x] Add overload grouping logic to `InlineModelBuilder`
- [x] Remove numeric suffix generation for method overloads
- [x] Generate `OnCall(callback)` method returning `IMethodTracking`
- [x] Generate `OnCall(callback, Times)` method returning `IMethodSequence<TCallback>`
- [x] Generate `Invoke()` method for explicit interface implementation
- [x] Generate nested `MethodTrackingImpl` class
- [x] Generate nested `MethodSequenceImpl` class with `ThenCall()`
- [x] Generate `Reset()` method
- [x] Generate `Verify()` method

### Phase 2: Inline Generic Methods ✅

- [x] Update generic method handler to use OnCall() method pattern
- [x] Ensure `.Of<T>()` returns handler with OnCall() method
- [x] Support Times and ThenCall() for generic methods

### Phase 3: Inline Indexers (OfXxx Pattern)

- [ ] Generate `IndexerContainer` class with OfXxx properties
- [ ] Generate `OfInt32`, `OfString`, etc. based on key types
- [ ] Each OfXxx is property-like: `Backing`, `OnGet`, `OnSet`, tracking
- [ ] Update explicit interface implementation to route to correct OfXxx

### Phase 4: Standalone Generic Methods ✅

- [x] Update `FlatRenderer` to generate OnCall() method for generic typed handlers
- [x] Ensure consistency with regular method pattern

### Phase 5: Standalone Indexers (OfXxx Pattern)

- [ ] Update `FlatRenderer` to generate IndexerContainer with OfXxx
- [ ] Match inline indexer implementation

### Phase 6: Inline Class Stubs ✅

- [x] Apply method changes to inline class stubs
- [x] Verify virtual method handling matches standalone

### Phase 7: Stub-Level Features ✅

- [x] Generate `Verify()` method on stub class (checks all interceptors)
- [x] Ensure `Strict` mode works with new pattern

### Phase 8: Testing ✅

- [x] Add tests for OnCall() returning IMethodTracking (inline)
- [x] Add tests for Times sequencing (inline)
- [x] Add tests for ThenCall() chaining (inline)
- [x] Add tests for Verify() (inline)
- [x] Add tests for method overload resolution (inline)
- [ ] Add tests for indexer OfXxx pattern (inline & standalone) - *deferred*
- [x] Add tests for generic method OnCall() pattern (inline & standalone)
- [x] Verify existing tests still pass (550+ tests across all projects)

### Phase 9: Documentation

- [ ] Update `docs/guides/inline-stubs.md`
- [ ] Update `docs/guides/methods.md` (if exists)
- [ ] Update inline stub samples in `Documentation.Samples`
- [ ] Update KnockOff skill with unified API

---

## Reference

### Target API (Unified)

**Regular Methods:**
```csharp
// Callback returns tracking
var tracking = stub.Method.OnCall((ko, x) => x * 2);
Assert.Equal(1, tracking.CallCount);

// Sequencing with Times
stub.Method
    .OnCall((ko, x) => 100, Times.Once)
    .ThenCall((ko, x) => 200, Times.Forever);

// Verification
stub.Method.Verify();
stub.Verify();  // Whole stub
```

**Generic Methods:**
```csharp
var tracking = stub.Create.Of<User>().OnCall((ko) => new User());
tracking.CallCount;
tracking.LastArg;
```

**Indexers:**
```csharp
stub.Indexer.OfInt32.Backing[0] = "preset";
stub.Indexer.OfInt32.OnGet = (ko, i) => items[i];
stub.Indexer.OfInt32.GetCount;
stub.Indexer.OfString.OnGet = (ko, k) => dict[k];
```

**Properties (unchanged):**
```csharp
stub.Name.Value = "test";
stub.Name.OnGet = (ko) => "computed";
stub.Name.GetCount;
```

**Events (unchanged):**
```csharp
stub.DataReceived.Raise(sender, EventArgs.Empty);
stub.DataReceived.AddCount;
```

---

## Breaking Changes

This is a **major breaking change**. Migration:

| Old API | New API |
|---------|---------|
| `stub.Method.OnCall = (ko) => ...` | `var t = stub.Method.OnCall((ko) => ...)` |
| `stub.Method.CallCount` | `tracking.CallCount` |
| `stub.Method0.CallCount` (overloads) | `tracking.CallCount` (compiler resolves) |
| `stub.Create.Of<T>().OnCall = ...` | `var t = stub.Create.Of<T>().OnCall(...)` |
| `stub.Indexer.GetCount` | `stub.Indexer.OfInt32.GetCount` |

---

## Files to Modify

```
src/Generator/
├── Builder/
│   ├── InlineModelBuilder.cs      # Add overload grouping, indexer container
│   └── FlatModelBuilder.cs        # Update generic methods, indexers
├── Model/
│   ├── Inline/
│   │   ├── InlineMethodModel.cs   # Match FlatMethodModel structure
│   │   └── InlineIndexerModel.cs  # Add OfXxx support
│   └── Flat/
│       └── FlatIndexerModel.cs    # Add OfXxx support
├── Renderer/
│   ├── InlineRenderer.cs          # Generate new patterns
│   └── FlatRenderer.cs            # Update generic methods, indexers
└── KnockOffGenerator.GenerateInline.cs

src/Tests/
├── KnockOffTests/
│   ├── InlineInterceptorApiTests.cs  # New tests
│   └── IndexerOfXxxTests.cs          # New tests
└── KnockOff.Documentation.Samples/

docs/
├── guides/
│   └── inline-stubs.md
└── release-notes/
    └── vX.X.X.md                  # Breaking change release
```

---

## Progress Log

### 2026-01-16
- Identified API discrepancy between standalone and inline stubs
- Discovered generic methods and indexers also need updates (even in standalone)
- Created comprehensive todo covering all member types

### 2026-01-17 (Session 1)
- Created shared `UnifiedMethodInterceptorModel` and `MethodOverloadSignature` records
- Created `UnifiedInterceptorBuilder` for building method interceptor models
- Created `MethodInterceptorRenderer` for shared code generation
- Refactored `FlatRenderer` to use shared components
- Refactored `InlineRenderer` to use shared components
- Updated flat generic method handlers to method-based API
- Updated inline generic method handlers to method-based API
- Fixed ClassRenderer invocation logic (`.OnCall` → `.Callback`)

### 2026-01-17 (Session 2)
- Fixed unconfigured call tracking (added `_unconfiguredCallCount` field)
- Fixed `LastCallArg`/`LastCallArgs` tracking for unconfigured calls
- Fixed Task/ValueTask default values:
  - `Task` → `Task.CompletedTask`
  - `ValueTask` → `default`
  - `Task<T>` → `Task.FromResult<T>(default!)`
  - `ValueTask<T>` → `new ValueTask<T>((T)default!)`
- Root cause: `GetDefaultExpressionForReturn` was checking `isNullable` before Task types
- Updated BCL interface tests for new behavior (string returns null, not throws)
- Updated Neatoo interface tests (IEnumerable methods require callbacks)
- All 1,345+ tests passing across all projects

---

## Results / Conclusions

### Completed
- **Method API unification is complete** - Both standalone and inline stubs now use `OnCall()` method pattern
- **Generic method API unification is complete** - `.Of<T>().OnCall()` pattern works for both stub types
- **Shared infrastructure** - `UnifiedInterceptorBuilder` and `MethodInterceptorRenderer` reduce code duplication

### Remaining Work
- **Indexers (OfXxx pattern)** - Phases 3 & 5 deferred to separate PR
- **Documentation** - Phase 9 pending

### Breaking Changes Applied
| Old API | New API |
|---------|---------|
| `stub.Method.OnCall = (ko) => ...` | `var t = stub.Method.OnCall((ko) => ...)` |
| `stub.Method.CallCount` | `stub.Method.CallCount` or `tracking.CallCount` |
| `stub.Create.Of<T>().OnCall = ...` | `var t = stub.Create.Of<T>().OnCall(...)` |
