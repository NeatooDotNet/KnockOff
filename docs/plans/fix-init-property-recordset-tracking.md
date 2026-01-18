# Fix Init-Only Property RecordSet Tracking

**Date:** 2026-01-18
**Related Todo:** [Init-only Property RecordSet Tracking Inconsistent](../todos/init-only-property-recordset-inconsistency.md)
**Status:** Draft
**Last Updated:** 2026-01-18

---

## Overview

Fix the FlatRenderer to include set tracking (`SetCount`, `LastSetValue`, `RecordSet()`) for init-only properties, matching InlineRenderer's behavior. Currently, FlatRenderer only generates get-tracking for init-only properties, causing inconsistency across stub patterns.

---

## Approach

The fix is straightforward: update two methods in FlatRenderer to match InlineRenderer's behavior for init-only properties.

**Key insight:** In FlatPropertyModel, `HasSetter = true` when the property has an init accessor, so the existing `prop.HasSetter` check can be used to generate set-tracking members.

---

## Design

### Current FlatRenderer Behavior (Bug)

**`RenderInitPropertyInterceptorContent` (lines 264-280):**
```csharp
private static void RenderInitPropertyInterceptorContent(CodeWriter w, FlatPropertyModel prop)
{
    w.Line($"public {prop.ReturnType} Value {{ get; set; }} = default!;");
    w.Line("public int GetCount { get; private set; }");
    w.Line("public void RecordGet() => GetCount++;");
    w.Line("public void Reset() { GetCount = 0; Value = default!; }");
}
```

**`RenderPropertyImplementation` init accessor (lines 1708-1712):**
```csharp
if (prop.IsInitOnly)
{
    w.Line($"get {{ {prop.InterceptorName}.RecordGet(); return {prop.InterceptorName}.Value; }}");
    w.Line($"init {{ {prop.InterceptorName}.Value = value; }}");  // BUG: No RecordSet call
}
```

### Correct InlineRenderer Behavior (Reference)

**`RenderPropertyInterceptorClass` (lines 219-256):**
- Generates `SetCount`, `LastSetValue`, `OnSet` when `prop.HasSetter` is true (includes init)
- Generates `RecordSet(value)` method
- Reset method clears `SetCount = 0; LastSetValue = default; OnSet = null;`

**`RenderPropertyImplementation` init accessor (lines 617-620):**
```csharp
if (impl.IsInitOnly)
{
    w.Line($"{impl.InterceptorName}.RecordSet(value);");  // Tracks the set
    w.Line($"{impl.InterceptorName}.Value = value;");
}
```

### Fix Strategy

1. **`RenderInitPropertyInterceptorContent`** - Add set tracking members:
   - `SetCount` property
   - `LastSetValue` property
   - `RecordSet(value)` method
   - Update `Reset()` to clear set tracking state

2. **`RenderPropertyImplementation`** - Update init accessor:
   - Call `RecordSet(value)` before assigning `Value = value`

### Generated Code Comparison

**Before (Buggy):**
```csharp
public sealed class IEntityWithInitProperty_IdInterceptor
{
    public string Value { get; set; } = default!;
    public int GetCount { get; private set; }
    public void RecordGet() => GetCount++;
    public void Reset() { GetCount = 0; Value = default!; }
}

string IEntityWithInitProperty.Id
{
    get { IEntityWithInitProperty_IdInterceptor.RecordGet(); return IEntityWithInitProperty_IdInterceptor.Value; }
    init { IEntityWithInitProperty_IdInterceptor.Value = value; }
}
```

**After (Fixed):**
```csharp
public sealed class IEntityWithInitProperty_IdInterceptor
{
    public string Value { get; set; } = default!;
    public int GetCount { get; private set; }
    public int SetCount { get; private set; }
    public string? LastSetValue { get; private set; }
    public void RecordGet() => GetCount++;
    public void RecordSet(string? value) { SetCount++; LastSetValue = value; }
    public void Reset() { GetCount = 0; SetCount = 0; LastSetValue = default; Value = default!; }
}

string IEntityWithInitProperty.Id
{
    get { IEntityWithInitProperty_IdInterceptor.RecordGet(); return IEntityWithInitProperty_IdInterceptor.Value; }
    init { IEntityWithInitProperty_IdInterceptor.RecordSet(value); IEntityWithInitProperty_IdInterceptor.Value = value; }
}
```

---

## Implementation Steps

### Step 1: Update `RenderInitPropertyInterceptorContent`

File: `src/Generator/Renderer/FlatRenderer.cs`
Lines: 264-280

Add after `RecordGet()` line:
```csharp
w.Line("/// <summary>Number of times the setter was accessed.</summary>");
w.Line("public int SetCount { get; private set; }");
w.Line();

w.Line("/// <summary>The value from the most recent setter call.</summary>");
w.Line($"public {prop.NullableReturnType} LastSetValue {{ get; private set; }}");
w.Line();

w.Line("/// <summary>Records a setter access.</summary>");
w.Line($"public void RecordSet({prop.NullableReturnType} value) {{ SetCount++; LastSetValue = value; }}");
w.Line();
```

Update Reset method:
```csharp
w.Line("public void Reset() { GetCount = 0; SetCount = 0; LastSetValue = default; Value = default!; }");
```

### Step 2: Update `RenderPropertyImplementation` init accessor

File: `src/Generator/Renderer/FlatRenderer.cs`
Lines: 1708-1712

Change from:
```csharp
w.Line($"init {{ {prop.InterceptorName}.Value = value; }}");
```

To:
```csharp
w.Line($"init {{ {prop.InterceptorName}.RecordSet(value); {prop.InterceptorName}.Value = value; }}");
```

### Step 3: Verify the failing test passes

Run the specific test:
```bash
dotnet test --filter "FullyQualifiedName~StandaloneStub_InitProperty_InterceptorHasSetTracking"
```

### Step 4: Run full test suite for all three patterns

```bash
dotnet test src/Tests/KnockOffTests
```

Verify tests pass for:
- **Stand-Alone/Flat**: `InitPropertyStandaloneTests`
- **Inline Interface**: `InitPropertyInlineStubTests`
- **Inline Class**: `ClassInitPropertyStubTests`

### Step 5: Review generated code

Check the generated files to confirm consistency:
- `src/Tests/KnockOffTests/Generated/KnockOff.Generator/KnockOff.Generator.FlatApiGenerator/EntityWithInitPropertyKnockOff.g.cs`
- Compare with inline equivalent to verify matching structure

---

## Acceptance Criteria

- [ ] `StandaloneStub_InitProperty_InterceptorHasSetTracking` test passes
- [ ] All existing `InitPropertyTests.cs` tests pass
- [ ] Generated interceptor for init-only properties has:
  - `SetCount` property
  - `LastSetValue` property
  - `RecordSet(value)` method
- [ ] Init accessor calls `RecordSet(value)` before assigning
- [ ] `Reset()` clears set tracking state
- [ ] No regression in other property types (regular setter, get-only)
- [ ] All three stub patterns (Standalone, Inline Interface, Inline Class) behave consistently

---

## Dependencies

None. This is a contained fix within FlatRenderer.

---

## Risks / Considerations

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Breaking existing flat stubs that rely on current interceptor structure | Low | Medium | API is additive (new members), existing code continues to work |
| Generated code size increase | Low | Low | Only adds 4 lines per init property interceptor |

### Notes

1. **No OnSet callback for init properties** - This is intentional. Init properties are set during object construction, not during test execution. OnSet callbacks would have limited utility. InlineRenderer also omits OnSet for init properties.

2. **No _source delegation for init properties** - Init properties cannot be delegated to a source object because init-only setters can only be called during object initialization. Both renderers correctly skip _source for init properties.

3. **NullableReturnType usage** - Using `NullableReturnType` for `LastSetValue` and `RecordSet` parameter allows storing/recording null values for nullable properties.
