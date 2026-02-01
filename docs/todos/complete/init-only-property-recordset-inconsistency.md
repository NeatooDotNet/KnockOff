# Init-only Property RecordSet Tracking Inconsistent

**Status:** In Progress
**Priority:** Medium
**Created:** 2026-01-18
**Last Updated:** 2026-01-18

---

## Problem

InlineRenderer correctly calls `RecordSet(value)` for init-only properties, but FlatRenderer only assigns `Value = value` without tracking. This inconsistency means that init-only property sets are not recorded when using the flat/stand-alone pattern, preventing verification of property initialization.

**Investigation revealed the bug is more extensive than initially thought.** The entire set tracking infrastructure is missing from FlatRenderer's init-only property interceptor.

---

## Solution

Update FlatRenderer's `RenderInitPropertyInterceptorContent` method to include set tracking, matching InlineRenderer's behavior:

1. Add `SetCount`, `LastSetValue`, and `RecordSet()` to the interceptor class
2. Update the `init` accessor to call `RecordSet(value)`
3. Update `Reset()` to clear set tracking state

---

## Plans

- [Fix Init-Only Property RecordSet Tracking](../plans/fix-init-property-recordset-tracking.md)

---

## Tasks

- [x] Investigate InlineRenderer's init-only property handling to understand correct pattern
- [x] Locate FlatRenderer's init-only property generation code
- [x] Add failing test to verify init-only property tracking in flat pattern
- [ ] Update `RenderInitPropertyInterceptorContent` to add `SetCount`, `LastSetValue`, `RecordSet`
- [ ] Update `RenderPropertyImplementation` init accessor to call `RecordSet(value)`
- [ ] Verify all three patterns behave consistently

---

## Progress Log

**2026-01-18:** Investigation complete. Found two issues in FlatRenderer:

1. **`RenderInitPropertyInterceptorContent` (lines 264-280):** Missing `SetCount`, `LastSetValue`, and `RecordSet()` members. Only generates `Value`, `GetCount`, `RecordGet()`, and `Reset()`.

2. **`RenderPropertyImplementation` (lines 1708-1712):** The `init` accessor only assigns `Value = value` without calling `RecordSet(value)`.

**Comparison:**

| Member | InlineRenderer | FlatRenderer (Bug) |
|--------|---------------|-------------------|
| `SetCount` | ✓ Generated | ✗ Missing |
| `LastSetValue` | ✓ Generated | ✗ Missing |
| `RecordSet()` | ✓ Generated | ✗ Missing |
| init calls RecordSet | ✓ Yes | ✗ No |

**Test Added:** `StandaloneStub_InitProperty_InterceptorHasSetTracking` in `InitPropertyTests.cs` - fails to compile due to missing members.

---

## Results / Conclusions
