# Add Value-Based Overloads to OnCall/OnGet/OnSet

**Status:** Complete
**Priority:** High
**Created:** 2026-01-24
**Last Updated:** 2026-01-24 (All phases complete - 1432 tests passing)

---

## Problem

Current API requires lambdas even for simple value returns, which is verbose:
```csharp
stub.GetValue.OnCall(() => 42);           // Verbose for simple value
stub.GetUserAsync.OnCall((id) => Task.FromResult(user));  // Async ceremony
stub.Name.Value = "test";                 // No tracking returned
```

Users want simpler syntax for common cases while maintaining tracking capabilities.

## Solution

Add value-based overloads to OnCall/OnGet/OnSet methods:

1. **Method value overloads**: `stub.GetValue.OnCall(42)` returns tracking
2. **Async auto-wrapping**: `stub.GetUserAsync.OnCall(user)` auto-wraps to `Task.FromResult(user)`
3. **Property value overloads**: Replace `.Value` with `OnGet(value)` that returns tracking
4. **Sequence support**: `OnCallSequence(1).ThenCall(2).ThenCall(3)`
5. **Mutual exclusivity**: Value clears callback, callback clears value

### Breaking Change
Remove `.Value` property from property interceptors. Users must migrate from `stub.Name.Value = "x"` to `stub.Name.OnGet("x")`.

---

## Plans

- [Value-Based Overloads Architecture](../plans/value-overloads-architecture.md)

---

## Tasks

- [x] Create architecture plan
- [x] Implement method value overloads (Phase 1)
- [x] Implement async auto-wrapping (Phase 1)
- [ ] Add value-based sequences (deferred)
- [x] Remove property .Value (breaking change) (Phase 2)
- [x] Implement property OnGet value overloads (OnSet removed from scope) (Phase 2)
- [x] Support delegates (Phase 4) - generics and indexers work automatically via shared renderers
- [x] Update all tests (31 non-generated files using .Value) (Phase 3)
- [x] Update documentation (Phase 5)
- [x] Add migration guide (Phase 5)

---

## Progress Log

**2026-01-24**: Created todo and initial architecture plan after user request for value-based overloads.

**2026-01-24**: Architect deep-dive complete. Key findings:
- All three main patterns (Standalone, Inline Interface, Inline Class) use shared renderers - automatic support
- Delegate stubs require separate implementation in `InlineRenderer.RenderDelegateStub()`
- Sequence value overloads should be generated methods (not interface methods) due to type constraints
- `OnSet(value)` removed from scope - semantically unclear
- Init-only properties: keep internal `_value` field, remove only public accessor
- 47 test files require migration (247 occurrences of `.Value =`)
- No generator diagnostics needed - compile errors are self-explanatory
- Plan updated with verification checklist, edge cases, and test strategy

**2026-01-24**: Developer review complete. Findings:
- Architect's design is comprehensive and implementable
- Test count corrected: 29 non-generated files need manual migration (generated files auto-regenerate)
- Confirmed: Runtime interfaces (`IMethodSequence<T>`, `IPropertySequence<T>`) remain unchanged
- Confirmed: Async type detection via string prefix matching is appropriate
- Implementation contract created with 5 phases and verification checkpoints
- Plan status updated to "Ready for Implementation"

**2026-01-24**: Phase 1 (Method Value Overloads) implementation complete:
- Added value storage fields (`_onCallValue`, `_hasOnCallValue`, `_onCallValueTracking`) to single-signature methods
- Added `OnCall(TReturn value)` overload that returns `IMethodTracking`
- Implemented async type detection and auto-wrapping for `Task<T>` and `ValueTask<T>`
- Updated Invoke priority chain: sequence -> value -> callback -> unconfigured
- Updated `OnCall(callback)` and `OnCallSequence` to clear value storage (mutual exclusivity)
- Created `MethodValueOverloadTests.cs` with 21 tests covering all scenarios
- All 664 tests passing (net10.0/net9.0), 663 on net8.0
- Key files modified: `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs`
- Key helper methods added: `GetAsyncTypeInfo()`, `MakeNullableForStorage()`, `HasRefOrOutParameters()`
- Overload groups deferred to later phase (only single-signature methods implemented)

**2026-01-24**: Phase 2 (Property Breaking Change) implementation complete:
- Removed public `.Value` property from regular property interceptors
- For init-only properties: kept internal `_value` and `_valueSet` fields, added `SetValue(T value)` method
- For regular properties: removed `_value` field entirely (no longer needed)
- Added `OnGet(TValue value)` wrapper method to both init-only and regular properties
- Added `OnGetSequence(TValue value)` wrapper method
- Added `ThenGet(TValue value)` to `PropertyGetSequenceImpl`
- Updated `InvokeGet` to return `default!` instead of `_value` for regular properties
- Updated `IsConfigured` to remove `_valueSet` condition for regular properties
- Updated init setter code in `FlatRenderer.cs` and `InlineRenderer.cs` to use `SetValue(value)`
- Created `PropertyValueOverloadTests.cs` with 14 tests for the new OnGet(value) API
- **Expected breaking change**: 31 test files fail with CS1061 "does not contain definition for 'Value'"
- These test files need migration in Phase 3

**2026-01-24**: Phase 4 (Delegate Value Overloads) implementation complete:
- Added `OnCall(TReturn value)` overload to delegate stub interceptors in `InlineRenderer.RenderDelegateStub()`
- Uses wrapper pattern: wraps value in lambda that ignores parameters
  - 0 params: `_onCall = () => value;`
  - 1 param: `_onCall = (_) => value;`
  - n params: `_onCall = (_, _, ...) => value;`
- Correctly skips void delegates (Action, Action<T>) - no value overload generated
- Created `DelegateValueOverloadTests.cs` with 18 tests covering:
  - Basic value returns (string, int, bool)
  - Nullable returns
  - Single and multi-parameter delegates
  - Complex objects and collections
  - Mutual exclusivity (value clears callback, callback clears value)
  - Void delegate verification (callback-only)
  - Reset functionality
- All 694 tests passing on net10.0, 694 on net9.0, 693 on net8.0
- Key file modified: `src/Generator/Renderer/InlineRenderer.cs` (lines 1285-1294)

**2026-01-24**: Phase 5 (New Tests & Documentation) complete:
- Created `SequenceValueOverloadTests.cs` (17 tests)
  - Property sequence value overloads: `OnGetSequence(value)`, mixed callbacks
  - Method sequence tests (callback-only, value overloads not implemented for method sequences)
  - Edge cases: exhausted sequences, strict mode, verification, reset
- Created `EdgeCaseValueOverloadTests.cs` (14 tests)
  - Delegate return types (method returns `Func<int>`)
  - Nullable async types (`Task<string?>`, `ValueTask<int?>`)
  - Overloaded methods (overload groups use callback syntax)
  - Edge values (empty string, zero, false)
- Created `ThreePatternValueOverloadTests.cs` (16 tests)
  - Standalone pattern: full value overload support
  - Inline Interface pattern: full value overload support
  - Inline Class pattern: property OnGet(value) works, method OnCall(value) NOT available
- Updated `docs/getting-started.md`:
  - Added "Configuring Return Values" section with value vs callback comparison
  - Added async auto-wrapping documentation
- Updated Documentation.Samples:
  - `MethodsSamples.cs`: Added value overload examples
  - `PropertiesSamples.cs`: Added OnGet value vs callback example, sequence value example
  - `AsyncSamples.cs`: Added async value auto-wrapping example
  - `DelegatesSamples.cs`: Added delegate value overload example
- Created `docs/migration/property-value-removal.md` migration guide
- **All 1432 tests passing** (218 docs + 473 interface + 741 knockoff)

---

## Results / Conclusions

**Phase 5 complete. Feature fully implemented with comprehensive test coverage and documentation.**

Key outcomes:
1. **Method value overloads**: `OnCall(value)` for single-signature methods with async auto-wrapping
2. **Property value overloads**: `OnGet(value)`, `OnGetSequence(value)`, `ThenGet(value)` all working
3. **Delegate value overloads**: `OnCall(value)` for delegates with parameter-ignoring wrapper
4. **Breaking change**: `.Value` property removed, migration guide provided
5. **Three patterns**: Standalone and Inline Interface fully support value overloads; Inline Class has property support only (methods use callback syntax)

Not implemented (out of scope):
- Method sequence value overloads (`OnCallSequence(value)`, `ThenCall(value)`) - interface constraints
- Overload group value overloads - use delegate type disambiguation with callback syntax
- Generic method value overloads - use typed handler callback syntax
