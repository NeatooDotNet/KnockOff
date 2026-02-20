# Migration Guide: v0.53.0 (Delegate-Based Call API)

**Version:** 0.53.0 (Breaking Change)
**Date:** 2026-02-19

---

## Summary

v0.53.0 replaces `Func<tuple>`/`Action<tuple>` with custom named delegates for all `Call` callbacks and `When` predicates. This fixes CS0121 overload ambiguity for interfaces with same-type overloads and provides better IntelliSense with original parameter names in delegate tooltips.

---

## Breaking Changes

### 1. Call Callback Syntax (2+ Parameters)

Tuple accessor syntax replaced with typed delegate parameters:

```csharp
// Before (v0.52.0):
stub.Add.Call(args => args.a + args.b);

// After (v0.53.0):
stub.Add.Call((int a, int b) => a + b);
```

### 2. When Predicate Syntax (2+ Parameters)

```csharp
// Before (v0.52.0):
stub.Add.When(args => args.a > 0).Return(42);

// After (v0.53.0):
stub.Add.When((int a, int b) => a > 0).Return(42);
```

### 3. Overloaded Method Syntax (2+ Parameters)

```csharp
// Before (v0.52.0):
stub.Format.Call(((string input, FormatOptions options) args) => args.input);

// After (v0.53.0):
stub.Format.Call((string input, FormatOptions options) => input);
```

### 4. Generated Type Names

| Before (v0.52.0) | After (v0.53.0) |
|-------------------|-----------------|
| `MethodCallBuilderImpl` | `AddImpl` |
| `MethodSequenceImpl` | `AddSequence` |
| `Func<(int a, int b), int>` | `AddDelegate` |
| `Func<(int a, int b), bool>` (When) | `AddPredicate` |

Overloaded methods use numbered suffixes: `FormatDelegate`, `FormatDelegate2`, `FormatDelegate3`.

---

## What Did NOT Change

- `Return(value)` / `Return(first, params rest)` -- unchanged
- `LastArg` / `LastArgs` -- still tuple-based, unchanged
- `When(exactValue)` / `When(a, b)` exact match -- unchanged
- `Verify()` / `Verifiable()` / `Reset()` -- unchanged
- Property, indexer, and event interceptors -- unchanged
- `Source()` delegation -- unchanged

---

## Migration Steps

1. **Find-replace tuple accessors:** Search for `args => args.` in your test files
2. **Replace with typed params:** Change `args => args.a + args.b` to `(int a, int b) => a + b`
3. **Update overloaded lambdas:** Change `((Type1, Type2) args) => args.field` to `(Type1 field1, Type2 field2) => field1`
4. **Build and fix:** The compiler will flag any remaining issues with clear type mismatch errors

---

## Related

- [Release Notes v0.53.0](../release-notes/v0.53.0.md)
- [Todo: Delegate-Based Call API](../todos/delegate-based-call-api.md)
