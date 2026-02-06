# Migration Guide: Property .Value Removal

**Version:** 0.24.0 (Breaking Change)
**Date:** 2026-01-24

## Overview

KnockOff 0.24.0 removes the `.Value` property from property interceptors and replaces it with method syntax for consistency with the rest of the API. This is a breaking change that requires code updates.

## What Changed

### Old API (Removed)

<!-- snippet: property-value-old-api -->
```cs
// OLD API (no longer compiles in 0.24.0+):
//
// stub.Name.Value = "Alice";           // Set property return value
// var name = stub.Name.Value;          // Read configured value
```
<!-- endSnippet -->

### New API

<!-- snippet: property-value-new-api -->
```cs
// NEW API: Configure property return value with OnGet
stub.Name.OnGet("Alice");
```
<!-- endSnippet -->

## Migration Steps

### 1. Replace `.Value = x` with `.OnGet(x)`

**Before:**
<!-- snippet: migration-value-to-onget-before -->
```cs
// BEFORE (0.23.x and earlier):
//
// stub.ConnectionString.Value = "Server=localhost";
// stub.Timeout.Value = 30;
// stub.IsEnabled.Value = true;
```
<!-- endSnippet -->

**After:**
<!-- snippet: migration-value-to-onget-after -->
```cs
// AFTER (0.24.0+):
stub.ConnectionString.OnGet("Server=localhost");
stub.Timeout.OnGet(30);
stub.IsEnabled.OnGet(true);
```
<!-- endSnippet -->

### 2. Replace `.Value` reads with test assertions

If you were reading `.Value` to verify what was configured, that's no longer needed. The new API configures behavior directly without storing accessible state.

**Before:**
<!-- snippet: migration-value-read-before -->
```cs
// BEFORE (0.23.x and earlier):
//
// stub.Name.Value = "Expected";
// Assert.Equal("Expected", stub.Name.Value);  // Reading .Value
```
<!-- endSnippet -->

**After:**
<!-- snippet: migration-value-read-after -->
```cs
// AFTER: Configure with OnGet, verify through interface
stub.Name.OnGet("Expected");
```
<!-- endSnippet -->

### 3. Dynamic values use OnGet with callback

If you were using `.Value` with the expectation it would be read each time, use `OnGet` with a callback for dynamic behavior:

**Before (if expecting dynamic behavior):**
<!-- snippet: migration-dynamic-value-before -->
```cs
// BEFORE (0.23.x and earlier):
// Value was captured once at assignment time
//
// stub.LastUpdated.Value = DateTime.UtcNow;
```
<!-- endSnippet -->

**After (for truly dynamic values):**
<!-- snippet: migration-dynamic-value-after -->
```cs
// AFTER: Use callback for values evaluated on each access
stub.LastUpdated.OnGet(() => DateTime.UtcNow);
```
<!-- endSnippet -->

## Why This Change?

1. **API Consistency**: Methods now use `OnCall(value)`, properties use `OnGet(value)`. The pattern is consistent.

2. **Clearer Intent**: `OnGet("value")` clearly indicates you're configuring what the getter returns. `.Value = x` was ambiguous about whether it was setting a backing store or configuring behavior.

3. **Tracking Support**: `OnGet()` returns a tracking interface, enabling call verification. `.Value` had no tracking.

4. **Sequence Support**: `OnGetSequence(value)` enables chained sequences. The `.Value` property couldn't support this pattern.

## Affected APIs

| Old API | New API | Returns |
|---------|---------|---------|
| `interceptor.Value = x` | `interceptor.OnGet(x)` | `IPropertyGetTracking` |
| `interceptor.Value` (get) | Not available | N/A |

## Tooling Support

Your IDE should show compile errors wherever `.Value` was used, making migration straightforward:

```
error CS1061: 'NameInterceptor' does not contain a definition for 'Value'
```

## Examples

### Simple Property

<!-- snippet: migration-example-simple-property -->
```cs
// BEFORE: stub.Name.Value = "Alice";
// AFTER:
stub.Name.OnGet("Alice");
```
<!-- endSnippet -->

### Nullable Property

<!-- snippet: migration-example-nullable-property -->
```cs
// BEFORE: stub.Email.Value = null;
// AFTER: Cast null to the property type for OnGet
stub.Email.OnGet((string?)null);
```
<!-- endSnippet -->

### Value Type Property

<!-- snippet: migration-example-value-type-property -->
```cs
// BEFORE: stub.Age.Value = 42;
// AFTER:
stub.Age.OnGet(42);
```
<!-- endSnippet -->

### Boolean Property

<!-- snippet: migration-example-boolean-property -->
```cs
// BEFORE: stub.IsActive.Value = true;
// AFTER:
stub.IsActive.OnGet(true);
```
<!-- endSnippet -->

## Verifying Your Migration

After migrating, confirm:

1. **Build succeeds** - All `.Value` references should cause compile errors until replaced
2. **Tests pass** - Behavior remains unchanged, only syntax differs
3. **Search for `.Value`** - Grep your test files to find any missed instances:
   ```bash
   grep -r "\.Value\s*=" tests/
   ```

## Related Documentation

- [Property Configuration Guide](../guides/properties.md) - Full guide to property configuration and verification

## Questions?

If you encounter issues migrating, please open an issue on the KnockOff GitHub repository.

---

**UPDATED:** 2026-01-25
