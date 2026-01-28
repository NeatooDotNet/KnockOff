# Returns API Rename

**Status:** Complete
**Priority:** High
**Created:** 2026-01-27
**Last Updated:** 2026-01-27

---

## Plans

- [Returns API Rename Design Plan](../plans/returns-api-rename.md)

---

## Problem

`stub.Add.OnCall(10)` is confusing for users familiar with NSubstitute. It reads as "when argument is 10" rather than "return 10". This is especially problematic when comparing KnockOff to NSubstitute in documentation.

NSubstitute: `calc.Add(1, 2).Returns(3)` - parameter-specific, returns 3
KnockOff: `stub.Add.OnCall(10)` - looks like parameter matching, but means "return 10 for any args"

## Solution

Rename `OnCall(value)` to `Returns(value)` for clarity:

```csharp
// Before (confusing)
stub.GetUser.OnCall(user);

// After (clear)
stub.GetUser.Returns(user);
```

**Keep `OnCall(callback)` unchanged** - the name still makes sense for callbacks:
```csharp
stub.GetUser.OnCall(id => FindUser(id));  // "on call, run this callback"
```

### Constraints (unchanged from current)

- `Returns(value)` only available for **single-signature methods** (non-overloaded)
- Overload groups continue to use `OnCall(callback)` per signature
- This matches current `OnCall(value)` constraint

### Why this constraint?

For overloaded methods, tracking becomes ambiguous:
- Which overload was called?
- What are `LastCallArgs`?
- How do we verify call counts?

---

## Prerequisite For

- [Parameter-Specific Matching](./parameter-specific-matching.md) - establishes the `Returns` naming convention

---

## Requirements

- [x] Rename `OnCall(TValue value)` to `Returns(TValue value)`
- [x] Keep same constraint: single-signature methods only
- [x] Keep same tracking behavior (`IMethodTracking` return)
- [x] Keep same async support (Task<T>/ValueTask<T> unwrapping)
- [x] Remove `OnCall(value)` entirely (pre-1.0, breaking change acceptable)
- [x] Update documentation samples
- [x] Update README comparison

---

## Tasks

- [x] Update `MethodInterceptorRenderer` to generate `Returns()` instead of `OnCall(value)`
- [x] Remove `OnCall(value)` generation entirely
- [x] Update `ReadmeComparisonSamples.cs`
- [x] Update README.md comparison section
- [x] Add/update tests

---

## Progress Log

**2026-01-27:** Created todo. Identified that `OnCall(10)` is confusing - reads as parameter matching rather than return value. Decided to rename to `Returns(value)` while keeping `OnCall(callback)` for callbacks. Same single-signature constraint applies. Decision: remove `OnCall(value)` entirely (pre-1.0, breaking changes acceptable).

**2026-01-27:** Created design plan with architectural verification. Identified primary change location in `MethodInterceptorRenderer.cs` (line ~184) and secondary in `InlineRenderer.cs` (line ~1294). Three-pattern analysis complete: Standalone and Inline Interface patterns affected; Inline Class pattern not affected (no value overloads for methods). Updated plan to include internal field renames (`_onCallValue` → `_returnsValue`, etc.) for code maintainability and consistency. Re-reviewed plan: verified three-pattern analysis accuracy by examining `ClassRenderer.cs` (confirms Inline Class only has callback syntax). Added missing documentation files to update list (`docs/guides/methods.md`, `docs/guides/user-methods.md`, `docs/guides/source-delegation.md`). Plan ready for developer review.

---

## Decisions

1. **Backward compatibility:** Remove `OnCall(value)` entirely. Pre-1.0, breaking changes acceptable.

---

## Results / Conclusions

**Completed:** 2026-01-27

The `OnCall(value)` method has been successfully renamed to `Returns(value)` across the KnockOff codebase:

1. **Generator Changes:**
   - `MethodInterceptorRenderer.cs`: Renamed method and 25 internal field references
   - `InlineRenderer.cs`: Renamed delegate value overload

2. **Test Updates:**
   - All value overload tests updated to use `.Returns(value)` syntax
   - All tests passing (4513 tests across net8.0, net9.0, net10.0)

3. **Documentation Updates:**
   - README.md comparison tables updated
   - All documentation guides updated
   - MarkdownSnippets regenerated

**API Change:**
- `stub.GetUser.OnCall(user)` is now `stub.GetUser.Returns(user)`
- `stub.Interceptor.OnCall("result")` is now `stub.Interceptor.Returns("result")`
- `OnCall(callback)` remains unchanged

