# Return(value).ThenReturn() Sequence NRE Bug

**Status:** In Progress
**Priority:** Medium
**Created:** 2026-02-07
**Last Updated:** 2026-02-07

---

## Problem

When a method interceptor is configured with the value-based `Return(value)` followed by `.ThenReturn(value)`, the first call in the sequence throws a `NullReferenceException`.

**Reproduction:**

```csharp
stub.GetInternalId
    .Return("first-id")        // value-based Return
    .ThenReturn("second-id");  // triggers sequence creation

service.GetDescription();      // NRE on first call
```

**Root cause:** `Return(string value)` sets `_hasReturnValue = true` and `_returnValue = value`, but sets `_call = null`. When `ThenReturn` elevates to sequence mode, it adds `_interceptor._call!` as the first sequence entry — which is null. On the first invocation, `Invoke()` calls `callback()` on that null delegate.

**Affected code path** (generated interceptor):

```csharp
// Return(string value) — sets _returnValue, nulls _call
public MethodCallBuilderImpl Return(string value)
{
    _call = null;           // ← null
    _callTracking = null;
    _hasReturnValue = true;
    _returnValue = value;
    ...
}

// ThenReturn — reads _call which is null
public MethodSequenceImpl ThenReturn(GetInternalIdDelegate callback)
{
    if (_interceptor._sequence == null)
    {
        _interceptor._sequence = new List<...>();
        _interceptor._sequence.Add((_interceptor._call!, this)); // ← null added!
        ...
    }
    ...
}
```

**Workaround:** Use callback form instead of value form:

```csharp
// Works correctly
stub.GetInternalId
    .Return(() => "first-id")
    .ThenReturn(() => "second-id");
```

## Solution

Fix `ThenReturn` in `MethodCallBuilderImpl` to handle the case where the initial configuration was `Return(value)` instead of `Return(callback)`. When `_call` is null but `_hasReturnValue` is true, the first sequence entry should wrap `_returnValue` in a lambda: `() => _interceptor._returnValue`.

---

## Plans

---

## Tasks

- [ ] Identify the generator code that emits `ThenReturn` for method interceptors
- [ ] Fix the sequence elevation logic to handle value-based `Return`
- [ ] Verify fix compiles across all patterns that generate method interceptors
- [ ] Add Design.Tests regression test using `Return(value).ThenReturn(value)`
- [ ] Verify all existing tests still pass

---

## Progress Log

### 2026-02-07
- Discovered bug while writing protected method behavior tests
- Root cause identified: `Return(value)` nulls `_call`, `ThenReturn` reads `_call!`
- Documented workaround: use callback form `Return(() => "value")`
- Filed this todo

---

## Completion Verification

Before marking this todo as Complete, verify:

- [ ] Design project builds successfully
- [ ] Design project tests pass

**Verification results:**
- Design build: [Pending]
- Design tests: [Pending]

---

## Results / Conclusions

