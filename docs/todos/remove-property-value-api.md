# Remove Property .Value API

**Status:** In Progress
**Priority:** High
**Created:** 2026-01-24
**Last Updated:** 2026-01-24

---

## Problem

The property interceptor API has an inconsistency: `.Value` provides no tracking, while `OnGet(() => value)` returns `IPropertyGetTracking`. This creates two different patterns for the same goal (returning a fixed value), with different capabilities:

```csharp
// Current pattern - no tracking
stub.Name.Value = "test";

// Current pattern - has tracking
stub.Name.OnGet(() => "test");
```

Users who want both a simple value AND tracking must use the lambda syntax, which feels ceremonial for a constant value.

## Solution

Remove the `.Value` property and add an `OnGet(T value)` overload that:
1. Accepts a direct value (not a lambda)
2. Returns `IPropertyGetTracking` for verification
3. Behaves identically to `OnGet(() => value)` for all purposes

New pattern:
```csharp
stub.Name.OnGet("test");  // Returns tracking, simple syntax
stub.Name.OnGet(() => "test");  // Also works, for dynamic cases
```

This is a **breaking change** - all existing `.Value = x` usages must migrate to `OnGet(x)`.

---

## Plans

- [Remove Property Value API Design](../plans/remove-property-value-api.md)

---

## Tasks

- [ ] Architect: Complete design and verification checklist
- [ ] Developer: Review design and create implementation contract
- [ ] Add `OnGet(T value)` overload to PropertyInterceptorRenderer
- [ ] Remove `Value` property from PropertyInterceptorRenderer
- [ ] Update all tests using `.Value =` to use `.OnGet(value)`
- [ ] Update documentation (properties.md, interceptor-api.md)
- [ ] Update samples and migration guides
- [ ] Add release notes for breaking change
- [ ] Consider diagnostic for migration assistance

---

## Progress Log

**2026-01-24**: Created todo and plan. Initial architectural analysis in progress.

---

## Results / Conclusions
