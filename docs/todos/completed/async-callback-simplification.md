# Async Callback Simplification for OnCall

**Status:** Complete
**Priority:** Medium
**Created:** 2026-01-26
**Last Updated:** 2026-01-26

---

## Problem

For async methods with parameters, configuring callbacks requires verbose `Task.FromResult()` wrapping:

```csharp
// Current - verbose
PatientRepository.InsertContactAsync.OnCall((entity) => Task.FromResult(GenerateId()));
stub.GetUserAsync.OnCall((id) => Task.FromResult(user));
```

The value overload `OnCall(value)` already auto-wraps for async methods, but when you need the callback to receive the parameter (even if you don't use it dynamically), you're forced into the verbose syntax.

## Solution

Add callback overloads that accept `Func<TParams..., TInnerType>` for async methods and auto-wrap the result:

```csharp
// Proposed - clean
PatientRepository.InsertContactAsync.OnCall((entity) => GenerateId());  // Auto-wraps
stub.GetUserAsync.OnCall((id) => user);  // Auto-wraps

// Still available for actual async needs
stub.GetUserAsync.OnCall((id) => FetchFromCacheAsync(id));  // Returns Task<T> directly
```

**Scope:**
- Generate `OnCall(Func<TParams..., TInnerType>)` overload for `Task<TInnerType>` methods
- Generate `OnCall(Func<TParams..., TInnerType>)` overload for `ValueTask<TInnerType>` methods
- Auto-wrap results in `Task.FromResult()` / `new ValueTask<T>()`
- Generate `OnCall(Action<TParams...>)` overload for void async methods (`Task` / `ValueTask`)
- Auto-wrap void callbacks with `Task.CompletedTask` / `default(ValueTask)`

**Not in scope:**
- Sequence methods (`ThenCall`) - [separate todo](sequence-callback-simplification.md)
- Method overload groups - [separate todo](overload-group-value-callbacks.md)

---

## Plans

- [Async Callback Simplification Architecture](../plans/async-callback-simplification.md)

---

## Tasks

- [x] Design overload generation strategy in MethodInterceptorRenderer
- [x] Handle potential overload ambiguity (delegate type resolution)
- [x] Update renderer to generate unwrapped callback overloads for async methods
- [x] Add tests for Task<T> callback simplification
- [x] Add tests for ValueTask<T> callback simplification
- [x] Add tests for void async (Task) callback simplification
- [x] Add tests for void async (ValueTask) callback simplification
- [x] Add tests for methods with multiple parameters
- [x] Verify all three patterns (Standalone, Inline Interface, Inline Class)
- [x] Update documentation samples

---

## Progress Log

- 2026-01-26: Created todo based on feature exploration
- 2026-01-26: Architecture plan created by knockoff-architect
- 2026-01-26: Developer raised 7 concerns during review
- 2026-01-26: Architect addressed all 7 concerns; plan updated with missing phases for Reset, IsConfigured, and aggregate tracking
- 2026-01-26: Developer re-reviewed, verified all concerns addressed, created implementation contract, plan status set to "Ready for Implementation"
- 2026-01-26: Expanded scope to include void async methods (Task/ValueTask without <T>); created separate todos for sequence methods and overload groups
- 2026-01-26: Updated architecture plan with void async technical analysis, design patterns, implementation steps, and implementation contract checklist items
- 2026-01-26: Implementation complete - all 6 phases completed successfully

---

## Results / Conclusions

### Implementation Complete

The Async Callback Simplification feature has been successfully implemented:

**Features Added:**
1. `OnCall(Func<TParams..., TInnerType>)` for `Task<T>`/`ValueTask<T>` methods - auto-wraps in `Task.FromResult()` or `new ValueTask<T>()`
2. `OnCall(Action<TParams...>)` for `Task`/`ValueTask` methods - auto-returns `Task.CompletedTask` or `default(ValueTask)`

**Files Modified:**
- `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs` - Added helper methods, storage fields, OnCall overloads, and invoke handling
- `src/Tests/KnockOff.Documentation.Samples/AsyncSamples.cs` - Added documentation examples

**Files Created:**
- `src/Tests/KnockOffTests/AsyncCallbackSimplificationTests.cs` - 33 comprehensive tests

**Test Results:**
- All 33 new tests pass
- All existing tests pass (no regressions)
- Total tests: 2032 across all test projects

**Edge Case Discovered:**
Throw-only lambdas create overload ambiguity (lambda with only `throw` has no return type). Resolution: Use explicit delegate type for throw-only callbacks. This is documented in the plan.

**Before/After Syntax:**
```csharp
// Before (verbose)
stub.GetUserAsync.OnCall((id) => Task.FromResult(user));
stub.SaveUserAsync.OnCall((user) => { ValidateUser(user); return Task.CompletedTask; });

// After (simplified)
stub.GetUserAsync.OnCall((id) => user);  // Auto-wraps
stub.SaveUserAsync.OnCall((user) => ValidateUser(user));  // Auto-returns Task.CompletedTask
```

