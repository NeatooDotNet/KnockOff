# Remove `ko` Parameter from OnCall Callbacks

**Status:** Completed
**Priority:** Medium
**Created:** 2026-01-19
**Last Updated:** 2026-01-19
**Completed:** 2026-01-19

---

## Plans

- [Remove ko Parameter Implementation Plan](../plans/remove-ko-parameter.md)

---

## Problem

The `ko` parameter in OnCall callbacks is redundant when the stub is already a local variable:

```csharp
var stub = new FooStub();
stub.GetUser.OnCall((ko, id) => new User { Id = id });
//                   ^^ same as stub - unnecessary noise
```

Moq-skeptic feedback: "unnecessary noise when stub is already a local variable."

## Solution

Remove the `ko` parameter from generated OnCall delegate signatures.

**Before:**
```csharp
stub.GetUser.OnCall((ko, id) => new User { Id = id });
stub.IsActive.OnGet = (ko) => true;
stub.Name.OnSet = (ko, value) => { };
```

**After:**
```csharp
stub.GetUser.OnCall((id) => new User { Id = id });
stub.IsActive.OnGet = () => true;
stub.Name.OnSet = (value) => { };
```

---

## Tasks

- [x] Update generator to remove `ko` parameter from OnCall/OnGet/OnSet delegate signatures
- [x] Update all documentation samples
- [x] Update all test samples
- [x] Consider: Does any existing pattern rely on `ko`? (accessing other interceptors within callback)
  - Users can use closure to access stub when needed

---

## Impact

- **Breaking change** - all existing OnCall/OnGet/OnSet callbacks will need updating
- Affects all three patterns: Stand-Alone, Inline Interface, Inline Class

---

## Notes

If users need access to the stub within a callback, they can use closure:
```csharp
var stub = new FooStub();
stub.GetUser.OnCall((id) => {
    // Access stub via closure if needed
    return stub.SomeOtherMethod.CallCount > 0 ? new User { Id = id } : null;
});
```
