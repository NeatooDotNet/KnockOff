# Migration Guide: Property .Value Removal

**Version:** 10.24.0 (Breaking Change)
**Date:** 2026-01-24

## Overview

KnockOff 10.24.0 removes the `.Value` property from property interceptors and replaces it with method syntax for consistency with the rest of the API. This is a breaking change that requires code updates.

## What Changed

### Old API (Removed)

```csharp
// Set a static value for a property
stub.Name.Value = "Alice";

// Access the configured value
var value = stub.Name.Value;
```

### New API

```csharp
// Use OnGet to configure the getter return value
stub.Name.OnGet("Alice");

// Value is no longer directly accessible - configure via OnGet
```

## Migration Steps

### 1. Replace `.Value = x` with `.OnGet(x)`

**Before:**
```csharp
var stub = new UserStub();
stub.Name.Value = "Alice";
stub.Age.Value = 30;
stub.IsActive.Value = true;
```

**After:**
```csharp
var stub = new UserStub();
stub.Name.OnGet("Alice");
stub.Age.OnGet(30);
stub.IsActive.OnGet(true);
```

### 2. Replace `.Value` reads with test assertions

If you were reading `.Value` to verify what was configured, that's no longer needed. The new API configures behavior directly without storing accessible state.

**Before:**
```csharp
stub.Name.Value = "Alice";
Assert.Equal("Alice", stub.Name.Value); // Verify configuration
```

**After:**
```csharp
stub.Name.OnGet("Alice");
// Configuration is implicit - just use the stub
IUserService service = stub;
Assert.Equal("Alice", service.Name); // Test through the interface
```

### 3. Dynamic values use OnGet with callback

If you were using `.Value` with the expectation it would be read each time, use `OnGet` with a callback for dynamic behavior:

**Before (if expecting dynamic behavior):**
```csharp
stub.Timestamp.Value = DateTime.UtcNow; // Only captures time at assignment
```

**After (for truly dynamic values):**
```csharp
stub.Timestamp.OnGet(() => DateTime.UtcNow); // Evaluates each time
```

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

```csharp
// Before
stub.Status.Value = "Active";

// After
stub.Status.OnGet("Active");
```

### Nullable Property

```csharp
// Before
stub.CurrentUser.Value = null;

// After
stub.CurrentUser.OnGet((User?)null);
```

### Value Type Property

```csharp
// Before
stub.Count.Value = 42;

// After
stub.Count.OnGet(42);
```

### Boolean Property

```csharp
// Before
stub.IsEnabled.Value = true;

// After
stub.IsEnabled.OnGet(true);
```

## Questions?

If you encounter issues migrating, please open an issue on the KnockOff GitHub repository.
