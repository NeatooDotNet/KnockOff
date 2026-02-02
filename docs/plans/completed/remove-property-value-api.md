# Remove Property Value API Design

**Date:** 2026-01-24
**Related Todo:** [Remove Property .Value API](../todos/remove-property-value-api.md)
**Status:** Draft (Architect)
**Last Updated:** 2026-01-24

---

## Overview

This plan documents the breaking change to remove the `.Value` property from property interceptors and replace it with an `OnGet(T value)` overload that provides tracking. This unifies the property configuration API around the `OnGet` pattern.

---

## Current State Analysis

### Current Property Interceptor API

From `/home/keithvoels/neatoodotnet/KnockOff/src/Generator/Renderer/Shared/PropertyInterceptorRenderer.cs`:

**Regular Properties (lines 181-190):**
```csharp
w.Line("private bool _valueSet;");
w.Line($"private {model.ValueType} _value{GetDefaultValueSuffix(model.DefaultExpression)}");
w.Line($"/// <summary>Value returned by getter when OnGet is not set. Setting this marks the property as configured.</summary>");
w.Line($"public {model.ValueType} Value");
using (w.Braces())
{
    w.Line("get => _value;");
    w.Line("set { _value = value; _valueSet = true; }");
}
```

**Init-Only Properties (lines 54-63):**
```csharp
w.Line("private bool _valueSet;");
w.Line($"private {model.ValueType} _value = default!;");
w.Line($"/// <summary>The configured value for {model.PropertyName}. Setting this marks the property as configured.</summary>");
w.Line($"public {model.ValueType} Value");
using (w.Braces())
{
    w.Line("get => _value;");
    w.Line("set { _value = value; _valueSet = true; }");
}
```

**OnGet Method (lines 242-256):**
```csharp
w.Line($"/// <summary>Configures getter callback that repeats indefinitely. Returns tracking interface.</summary>");
w.Line($"public global::KnockOff.IPropertyGetTracking OnGet(global::System.Func<{model.ValueType}> callback)");
using (w.Braces())
{
    w.Line("_getSequence = null;");
    w.Line("_getSequenceIndex = 0;");
    w.Line("_isGetVerifiable = false;");
    w.Line("_getVerifiableTimes = null;");
    w.Line("_onGet = callback;");
    w.Line("_onGetTracking = new PropertyGetTrackingImpl(this);");
    w.Line("return _onGetTracking;");
}
```

### InvokeGet Priority (lines 346-404)

The current priority in `InvokeGet`:
1. Sequence (if present and not exhausted)
2. Repeating OnGet callback
3. Source (if available, for class stubs)
4. Strict mode check
5. Return `_value`

Key insight: `_value` is the **fallback** when nothing else is configured.

### Usage Patterns Found

**Tests using `.Value =`**: 78 files match (from grep results)
- `stub.Name.Value = "test";`
- `stub.Age.Value = 42;`
- `stub.CurrentUser.Value = new User { ... };`

**Tests using `OnGet()`**: 241 files match
- `stub.Id.OnGet(() => "test-id");`
- `stub.Timestamp.OnGet(() => DateTime.UtcNow);`

---

## Approach

### Design Philosophy

1. **One way to configure, one behavior**: Eliminate the dichotomy between `.Value` (no tracking) and `OnGet` (tracking)
2. **Tracking by default**: All configurations should provide access to tracking
3. **Simple cases remain simple**: `OnGet("value")` is barely longer than `.Value = "value"`
4. **No silent migrations**: Breaking change requires explicit code updates

### New API

**Add overload:**
```csharp
/// <summary>Configures getter to return a fixed value. Returns tracking interface.</summary>
public IPropertyGetTracking OnGet(T value) => OnGet(() => value);
```

**Remove:**
```csharp
public T Value { get; set; }  // REMOVE
```

### Implementation Decision: Capture Semantics

**Question**: Should `OnGet(T value)` capture the value at configuration time, or should it allow mutation?

**Option A: Immediate Capture (Recommended)**
```csharp
public IPropertyGetTracking OnGet(T value)
{
    _getSequence = null;
    _getSequenceIndex = 0;
    _isGetVerifiable = false;
    _getVerifiableTimes = null;
    _onGet = () => value;  // Closes over value
    _onGetTracking = new PropertyGetTrackingImpl(this);
    return _onGetTracking;
}
```

**Option B: Delegate to Func Overload**
```csharp
public IPropertyGetTracking OnGet(T value) => OnGet(() => value);
```

**Recommendation**: Option B is simpler and ensures identical behavior. The lambda captures `value` at call time, which is the expected behavior for "configure with this value."

---

## Design Details

### Overload Resolution Concerns

**Potential Issue**: For `Action` and `Func` types, `OnGet(value)` might be ambiguous with `OnGet(Func<T>)`.

**Example problematic case:**
```csharp
interface IHasActionProperty
{
    Action Callback { get; }
}

// Which overload?
stub.Callback.OnGet(someAction);  // OnGet(Action value) or OnGet(Func<Action> callback)?
```

**Analysis**: This is not a real problem because:
1. `Func<T>` and `T` are different types when `T = Action`
2. The compiler resolves `OnGet(someAction)` to `OnGet(Action value)` because `someAction` is an `Action`, not a `Func<Action>`
3. To use the Func overload, you'd write `OnGet(() => someAction)`

**Conclusion**: No ambiguity issues.

### Generated Code Patterns

**Before (generated code):**
```csharp
public sealed class IService_NameInterceptor
{
    private bool _valueSet;
    private string _value = default!;

    public string Value
    {
        get => _value;
        set { _value = value; _valueSet = true; }
    }

    private Func<string>? _onGet;

    public IPropertyGetTracking OnGet(Func<string> callback) { ... }

    internal string InvokeGet(bool strict)
    {
        // Priority: sequence > onGet > source > strict check > _value
        if (_onGet != null) { ... return _onGet(); }
        return _value;
    }
}
```

**After (generated code):**
```csharp
public sealed class IService_NameInterceptor
{
    private Func<string>? _onGet;

    public IPropertyGetTracking OnGet(string value) => OnGet(() => value);

    public IPropertyGetTracking OnGet(Func<string> callback) { ... }

    internal string InvokeGet(bool strict)
    {
        // Priority: sequence > onGet > source > strict check > default
        if (_onGet != null) { ... return _onGet(); }
        return default!;  // No more _value fallback
    }
}
```

### Fallback Behavior Change

**Current**: Unconfigured properties return `_value` (which is `default!` unless set).

**After**: Unconfigured properties:
1. In strict mode: throw `StubException.NotConfigured`
2. In non-strict mode: return `default!`

**Impact**: No behavioral change for users who always configure their properties. Users who rely on `_value` as "smart default" will get `default!` instead - same result unless they were setting `.Value` before tests.

### _valueSet Flag

**Current use**: `_valueSet` tracks whether `.Value` was assigned, used for `IsConfigured` check.

**After**: Remove `_valueSet`. `IsConfigured` now checks `_onGet != null || (_getSequence?.Count ?? 0) > 0`.

This is a semantic change: previously, setting `.Value` without using `OnGet` would mark as "configured". Now, only `OnGet` configurations count.

---

## Three Patterns Analysis

### 1. Standalone (Flat) Pattern

**File**: `/home/keithvoels/neatoodotnet/KnockOff/src/Generator/Renderer/FlatRenderer.cs`

**Generated structure:**
```csharp
[KnockOff]
public partial class MyStub : IService
{
    public IService_NameInterceptor Name { get; } = new();
}
```

**Impact**:
- Interceptor class generated in `PropertyInterceptorRenderer.RenderRegularPropertyContent` and `RenderInitOnlyPropertyContent`
- Remove `Value` property, add `OnGet(T value)` overload
- Update `IsConfigured` expression

### 2. Inline Interface Pattern

**File**: `/home/keithvoels/neatoodotnet/KnockOff/src/Generator/Renderer/InlineRenderer.cs`

**Generated structure:**
```csharp
[KnockOff<IService>]
public partial class Tests
{
    public static partial class Stubs
    {
        public sealed partial class IService : global::IService
        {
            public IService_NameInterceptor Name { get; } = new();
        }
    }
}
```

**Impact**: Same as Standalone - uses shared `PropertyInterceptorRenderer`.

### 3. Inline Class Pattern

**Generated structure:**
```csharp
[KnockOff<MyClass>]
public partial class Tests
{
    public static partial class Stubs
    {
        public sealed partial class MyClass
        {
            public MyClass Object => this;
            public MyClass_NameInterceptor Name { get; } = new();
        }
    }
}
```

**Impact**: Same as Standalone - uses shared `PropertyInterceptorRenderer`.

**Conclusion**: All three patterns use `PropertyInterceptorRenderer`, so the change is centralized.

---

## Breaking Changes Assessment

### API Breaking Changes

1. **Removal of `.Value` property**: All usages must change to `.OnGet(value)`
2. **Behavioral change for unconfigured reads**: Previously returned `_value` (mutable), now returns `default!`
3. **`IsConfigured` semantics**: Now only true when `OnGet` or `OnGetSequence` is called

### Migration Examples

```csharp
// Before
stub.Name.Value = "test";

// After
stub.Name.OnGet("test");
```

```csharp
// Before (init property)
stub.Id.Value = 42;

// After
stub.Id.OnGet(42);
```

```csharp
// Before (multiple properties)
stub.User.Value = new User { Id = 1 };
stub.Count.Value = 10;

// After
stub.User.OnGet(new User { Id = 1 });
stub.Count.OnGet(10);
```

### No Migration Needed

```csharp
// Already correct - no change needed
stub.Timestamp.OnGet(() => DateTime.UtcNow);
stub.IsReady.OnGet(() => isInitialized);
```

---

## Test Strategy

### Unit Tests to Add

1. **Value Overload Behavior**
   - `OnGet(value)` returns tracking
   - `OnGet(value)` and `OnGet(() => value)` behave identically
   - Tracking from value overload supports `Verifiable()`, `Verify()`, `CallCount`

2. **Overload Resolution**
   - For `Func<T>` properties, both overloads resolve correctly
   - For `Action` properties, both overloads resolve correctly
   - Null value handling

3. **All Three Patterns**
   - Standalone stub with value overload
   - Inline interface stub with value overload
   - Inline class stub with value overload

4. **Init-Only Properties**
   - Value overload works for init-only properties
   - RecordSet tracking still works

### Existing Tests to Update

**Files with `.Value =` usage** (78 files identified):
- Update to use `.OnGet(value)`
- Verify tests still pass with new API
- Preserve original test intent

---

## Diagnostic Requirements

### Optional: Migration Diagnostic

**Consideration**: Should we provide a Roslyn analyzer diagnostic to help users migrate?

**Pros**:
- Automated detection of `.Value =` usage
- Suggests fix: change to `.OnGet(value)`
- Smooth migration experience

**Cons**:
- `.Value` won't exist in generated code after change
- Diagnostic would need to detect usage of old versions
- Complex to implement for marginal benefit

**Recommendation**: Skip diagnostic. The compile error when `.Value` is removed is clear enough: "Property 'Value' does not exist". Users searching for migration will find the release notes.

---

## Edge Cases

### 1. Null Values

```csharp
// Works with reference types
stub.Name.OnGet((string?)null);

// Works with nullable value types
stub.Age.OnGet((int?)null);
```

### 2. Default Values

```csharp
// Before: reading unconfigured returns default!
var x = stub.Object.Name;  // Returns null (string default)

// After: same behavior - returns default!
var x = stub.Object.Name;  // Returns null (string default)
```

### 3. Chaining with Verifiable

```csharp
// Before (not possible with .Value)
stub.Name.Value = "test";
stub.Name.Verifiable();  // Works but Value doesn't track

// After
stub.Name.OnGet("test").Verifiable();  // Clean, tracking enabled
```

### 4. Reset Behavior

**Before**: `Reset()` clears `OnGet` but preserves `Value`.

**After**: `Reset()` clears `OnGet`. No `Value` to preserve. After reset, property is unconfigured and returns `default!`.

**User impact**: Users who relied on `Value` persisting after `Reset()` will need to re-call `OnGet(value)`.

---

## Implementation Steps

### Phase 1: Add Overload (Non-Breaking)

1. Add `OnGet(T value)` overload to `PropertyInterceptorRenderer`
2. Add tests for new overload
3. Document both APIs

### Phase 2: Deprecate Value (Warning)

**Skip this phase** - per KnockOff versioning policy (pre-1.0, breaking changes bump minor version), we go directly to removal.

### Phase 3: Remove Value (Breaking)

1. Remove `Value` property from `PropertyInterceptorRenderer`
2. Remove `_value` and `_valueSet` fields
3. Update `IsConfigured` check
4. Update `InvokeGet` fallback to `default!`
5. Update all tests
6. Update all documentation
7. Update samples and guides
8. Write release notes

---

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Users miss migration | Medium | Medium | Clear release notes, compile errors are self-explanatory |
| Reset behavior surprise | Low | Low | Document new Reset behavior in release notes |
| Overload confusion for delegate types | Low | Low | Analysis shows no ambiguity |
| IsConfigured false negatives | Medium | Low | Test coverage for verification scenarios |

---

## Architectural Verification

**Three Patterns Analysis:**
- Standalone: Uses shared PropertyInterceptorRenderer - applies
- Inline Interface: Uses shared PropertyInterceptorRenderer - applies
- Inline Class: Uses shared PropertyInterceptorRenderer - applies

**Breaking Changes:** Yes - removal of `.Value` property

**Pattern Consistency:** Aligns with method `OnCall` pattern; all configurations now return tracking

**Codebase Analysis:**

Files examined:
- `/home/keithvoels/neatoodotnet/KnockOff/src/Generator/Renderer/Shared/PropertyInterceptorRenderer.cs` - Main implementation
- `/home/keithvoels/neatoodotnet/KnockOff/docs/guides/properties.md` - Documentation
- `/home/keithvoels/neatoodotnet/KnockOff/docs/reference/interceptor-api.md` - API reference
- `/home/keithvoels/neatoodotnet/KnockOff/src/Tests/KnockOff.Documentation.Samples/PropertiesSamples.cs` - Samples
- `/home/keithvoels/neatoodotnet/KnockOff/src/Tests/KnockOffTests/InitPropertyTests.cs` - Init property tests

---

## Developer Review

**Status:** Not Started

**Concerns:** [To be filled by developer]

---

## Implementation Contract

[To be filled by developer before implementation]

**In Scope:**
- [ ] TBD

**Out of Scope:**
- TBD

---

## Implementation Progress

[To be filled during implementation]

---

## Completion Evidence

[Required before marking complete]

- **Tests Passing:** [Output or screenshot]
- **Generated Code Sample:** [Snippet showing feature works]
- **All Checklist Items:** [Confirmed 100% complete]
