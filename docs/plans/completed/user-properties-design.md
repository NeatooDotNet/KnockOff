# User Properties Design

**Date:** 2026-02-04
**Related Todo:** [Add User Properties](../todos/add-stub-override-properties.md)
**Status:** Ready for Implementation
**Last Updated:** 2026-02-04 (Phase 4 complete - Design.Stubs examples created)

---

## Overview

Extend the existing User Methods base class pattern to support user-defined property implementations in standalone stubs. Users will override generated virtual properties to provide default behavior, with interceptors still available for per-test configuration and verification.

---

## Applicable Patterns

User properties apply to **standalone patterns only** (same as user methods). Inline patterns generate the entire stub class, leaving no partial for user overrides.

| Pattern | Applies | Rationale |
|---------|---------|-----------|
| 1. Standalone | ✅ Yes | User writes partial class, can add overrides |
| 2. Generic Standalone | ✅ Yes | Same as Standalone with type parameters |
| 3. Standalone Class | ✅ Yes | User writes partial class, can add overrides |
| 4. Generic Standalone Class | ✅ Yes | Same as Standalone Class with type parameters |
| 5. Inline Interface | ❌ No | Entire stub generated, no user partial |
| 6. Inline Class | ❌ No | Entire stub generated, no user partial |
| 7. Inline Delegate | ❌ No | No properties on delegates |
| 8. Open Generic Interface | ❌ No | Entire stub generated, no user partial |
| 9. Open Generic Class | ❌ No | Entire stub generated, no user partial |

---

## Property Types

Three property accessor patterns must be handled:

### Get-Only Properties
```csharp
public interface IRepo { int Count { get; } }
```

**Generated base class:**
```csharp
protected virtual int Count_ => default;
```

**User override:**
```csharp
protected override int Count_ => _items.Count;
```

### Set-Only Properties
```csharp
public interface IConfig { string Setting { set; } }
```

**Generated base class:**
```csharp
protected virtual string Setting_ { set { } }
```

**User override:**
```csharp
protected override string Setting_ { set => _setting = value; }
```

### Get/Set Properties
```csharp
public interface IEntity { string Name { get; set; } }
```

**Generated base class:**
```csharp
protected virtual string Name_ { get => default!; set { } }
```

**User override:**
```csharp
private string _name = "";
protected override string Name_ { get => _name; set => _name = value; }
```

---

## Approach

### Priority Order (Same as User Methods)

When a property is accessed:

1. **OnGet/OnSet** - If configured via interceptor, use it (supersedes user override)
2. **User override** - If user provided `protected override`, call it
3. **Smart default** - Return `default(T)` (or throw in strict mode)

This matches the user methods pattern: `OnCall > User override > Default`

### Generated Code Structure

For interface:
```csharp
public interface IEntity
{
    int Id { get; }
    string Name { get; set; }
}
```

Generator produces:

```csharp
// Base class with virtual properties
public abstract class EntityStubBase
{
    protected virtual int Id_ => default;
    protected virtual string Name_ { get => default!; set { } }
}

// Stub class extends base
public partial class EntityStub : EntityStubBase, IEntity, IKnockOffStub
{
    public IdInterceptor Id { get; }
    public NameInterceptor Name { get; }

    // Explicit interface implementation calls interceptor
    int IEntity.Id => Id.Get();
    string IEntity.Name
    {
        get => Name.Get();
        set => Name.Set(value);
    }
}
```

Interceptor's `Get()` method:
```csharp
public T Get()
{
    RecordGet();
    if (_getCallback is { } callback) return callback();  // OnGet wins
    return Id_;  // Calls virtual (user override or base default)
}
```

### Naming Convention

- **User override property:** `PropertyName_` (underscore suffix, matches methods)
- **Interceptor property:** `PropertyName` (clean name, no suffix)

Example:
- Override: `protected override int Count_ => ...`
- Interceptor: `stub.Count.OnGet(42)`, `stub.Count.VerifyGet()`

---

## Design Decisions

### D1: Underscore Suffix (Same as Methods)

**Decision:** Use `PropertyName_` for the overridable virtual property.

**Rationale:** Consistency with user methods (`Process_`). The underscore:
- Distinguishes override target from interceptor
- Compiler catches typos (no matching virtual to override)
- Clear visual separation

### D2: Full Property Syntax for Get/Set

**Decision:** Generate full `{ get; set; }` syntax, not expression-bodied.

**Rationale:** Expression-bodied properties can't have setters. For consistency, all properties use block syntax:
```csharp
protected virtual string Name_ { get => default!; set { } }
```

### D3: Interceptor Tracking Still Works

**Decision:** User overrides don't bypass tracking - interceptor still records calls.

**Rationale:** Users need `VerifyGet()`, `VerifySet()`, and `LastSetValue` even with overrides. The interceptor wraps the call:
1. Record the access
2. Check for OnGet/OnSet configuration
3. If not configured, call user override (or base default)

### D4: OnGet/OnSet Supersedes User Override

**Decision:** Per-test configuration via `OnGet()`/`OnSet()` takes priority over user override.

**Rationale:** Same pattern as methods - user override provides reusable default, interceptor allows per-test customization.

```csharp
var stub = new EntityStub();

// By default, uses user override
IEntity e = stub;
var id = e.Id;  // Calls Count_ override

// OnGet supersedes for this test
stub.Id.OnGet(999);
id = e.Id;  // Returns 999, override not called
```

### D5: Strict Mode Bypass

**Decision:** User overrides bypass strict mode (they ARE the configuration).

**Rationale:** Same as user methods - if you wrote an override, the property is configured.

---

## Implementation Steps

### Phase 1: Model and Detection Changes

**1.1. Add `HasUserOverride` to FlatPropertyModel**

File: `src/Generator/Model/Flat/FlatPropertyModel.cs`

```csharp
internal sealed record FlatPropertyModel(
    // ... existing fields ...
    bool HasUserOverride,  // NEW
    // ... remaining fields ...
);
```

**1.2. Add property override detection function**

File: `src/Generator/KnockOffGenerator.Helpers.cs`

Add parallel function `DetectUserOverrideProperties()`:
- Same pattern as `DetectUserOverrideMethods()`
- Scan for `PropertyDeclarationSyntax` with `override` modifier and `_` suffix
- Return `HashSet<string>` of property names (no signature params needed - properties don't overload)

**1.3. Pass user override properties through transform**

File: `src/Generator/KnockOffGenerator.Transform.cs`

- Call `DetectUserOverrideProperties()` alongside existing `DetectUserOverrideMethods()`
- Add to `KnockOffTypeInfo` record: `UserOverrideProperties: EquatableArray<string>`

**1.4. Set HasUserOverride in FlatModelBuilder**

File: `src/Generator/Builder/FlatModelBuilder.cs`

- When building `FlatPropertyModel`, check if property name (with `_` suffix) is in user override set
- Set `HasUserOverride = userOverrideProperties.Contains(propName + "_")`

### Phase 2: Base Class Property Generation

**2.1. Property Deduplication Logic for Base Class**

File: `src/Generator/Renderer/FlatRenderer.cs`

Properties can appear in multiple interfaces with different signatures (get-only in one, get/set in another). The base class can only have ONE virtual property per name. Apply deduplication parallel to method deduplication:

```csharp
// Generate virtual protected properties for each interface property
// When multiple interfaces have the same property name but different accessors
// (e.g., IReadOnly.Name has getter only, IMutable.Name has get/set), we must pick one.
// We prefer the version with MORE accessors (get/set > get-only or set-only).
var propertiesToRender = new Dictionary<string, FlatPropertyModel>();

foreach (var property in unit.Properties)
{
    // Skip delegation targets (these are synthetic properties)
    if (property.DelegationTarget != null)
        continue;

    // Key by property name ONLY
    // C# does not allow overloading properties by return type or accessors
    var key = property.MemberName;

    if (propertiesToRender.TryGetValue(key, out var existing))
    {
        // Conflict: same property name from different interfaces
        // Prefer the version with MORE accessors
        var existingAccessorCount = (existing.HasGetter ? 1 : 0) + (existing.HasSetter ? 1 : 0);
        var newAccessorCount = (property.HasGetter ? 1 : 0) + (property.HasSetter ? 1 : 0);

        if (newAccessorCount > existingAccessorCount)
        {
            propertiesToRender[key] = property;
        }
        // If same accessor count, keep first (stable ordering)
    }
    else
    {
        propertiesToRender[key] = property;
    }
}

// Render the selected properties
foreach (var property in propertiesToRender.Values)
{
    RenderBaseClassProperty(w, property);
}
```

**NOTE:** This handles the case where `IReadOnlyEntity.Name { get; }` and `IMutableEntity.Name { get; set; }` both exist. The base class will have `protected virtual string Name_ { get => default!; set { } }` (the get/set version).

**2.2. Generate virtual properties in base class**

File: `src/Generator/Renderer/FlatRenderer.cs`

New method `RenderBaseClassProperty()`:

```csharp
/// <summary>
/// Renders a single virtual protected property in the base class.
/// Property name is suffixed with '_' to distinguish from interface property.
/// Returns default! to allow compilation without override.
/// </summary>
private static void RenderBaseClassProperty(CodeWriter w, FlatPropertyModel property)
{
    var propertyName = $"{property.MemberName}_";
    var returnType = property.ReturnType;

    w.Line($"/// <summary>Override to provide default implementation for {property.DeclaringInterface}.{property.MemberName}.</summary>");

    if (property.HasGetter && property.HasSetter)
    {
        // Get/set property - full property syntax
        w.Line($"protected virtual {returnType} {propertyName} {{ get => default!; set {{ }} }}");
    }
    else if (property.HasGetter)
    {
        // Get-only property - expression-bodied
        w.Line($"protected virtual {returnType} {propertyName} => default!;");
    }
    else if (property.HasSetter)
    {
        // Set-only property - block syntax
        w.Line($"protected virtual {returnType} {propertyName} {{ set {{ }} }}");
    }
    w.Line();
}
```

### Phase 3: Interface Implementation for User Override Properties (REVISED)

**IMPORTANT: This phase follows DC1 Option B - handle user override in interface implementation, NOT in the interceptor.**

This matches exactly how user methods work in `RenderUserOverrideImplementation()`.

**3.1. Add `RenderPropertyUserOverrideImplementation()` method**

File: `src/Generator/Renderer/FlatRenderer.cs`

Create a new method parallel to `RenderUserOverrideImplementation()` for methods:

```csharp
/// <summary>
/// Renders the explicit interface implementation for a property with user override (base class pattern).
/// Priority chain: OnGet/OnSet > User Override (virtual property with _ suffix).
/// Unlike properties without user override, this does NOT fall through to Strict/Default.
/// </summary>
private static void RenderPropertyUserOverrideImplementation(CodeWriter w, FlatPropertyModel prop)
{
    w.Line($"{prop.ReturnType} {prop.DeclaringInterface}.{prop.MemberName}");
    using (w.Braces())
    {
        if (prop.HasGetter)
        {
            w.Line("get");
            using (w.Braces())
            {
                // Record the access (for verification)
                w.Line($"{prop.InterceptorName}.RecordGet();");
                // OnGet supersedes user override
                w.Line($"if ({prop.InterceptorName}.HasOnGet) return {prop.InterceptorName}.InvokeGetCallback();");
                // User override (virtual property with _ suffix)
                w.Line($"return {prop.MemberName}_;");
            }
        }

        if (prop.HasSetter)
        {
            if (prop.SetterPragmaDisable != null)
                w.Append(prop.SetterPragmaDisable);
            w.Line("set");
            using (w.Braces())
            {
                // Record the access (for verification)
                w.Line($"{prop.InterceptorName}.RecordSet(value);");
                // OnSet supersedes user override
                w.Line($"if ({prop.InterceptorName}.HasOnSet) {{ {prop.InterceptorName}.InvokeSetCallback(value); return; }}");
                // User override (virtual property with _ suffix)
                w.Line($"{prop.MemberName}_ = value;");
            }
            if (prop.SetterPragmaRestore != null)
                w.Line(prop.SetterPragmaRestore);
        }
    }
    w.Line();
}
```

**3.2. Modify `RenderPropertyImplementation()` to route to new method**

File: `src/Generator/Renderer/FlatRenderer.cs`

Update the existing `RenderPropertyImplementation()` method to check for `HasUserOverride`:

```csharp
private static void RenderPropertyImplementation(CodeWriter w, FlatPropertyModel prop)
{
    // Handle property delegation first
    if (prop.DelegationTarget != null && prop.DelegationTargetInterface != null)
    {
        RenderPropertyDelegation(w, prop);
        return;
    }

    // User-defined properties with base class pattern: record access, check OnGet/OnSet, then delegate to virtual property
    if (prop.HasUserOverride)
    {
        RenderPropertyUserOverrideImplementation(w, prop);
        return;
    }

    // Existing implementation for non-user-override properties (unchanged)
    // ...
}
```

**3.3. Add new interceptor methods for user override support**

File: `src/Generator/Renderer/Shared/PropertyInterceptorRenderer.cs`

The interceptor needs these NEW methods to support the user override pattern:

```csharp
// For getter:
internal bool HasOnGet => _onGet != null || (_getSequence?.Count ?? 0) > 0;
internal T InvokeGetCallback()
{
    if (_getSequence != null && _getSequenceIndex < _getSequence.Count)
    {
        var (callback, tracking) = _getSequence[_getSequenceIndex];
        tracking.RecordCall();
        _getSequenceIndex++;
        return callback();
    }
    if (_onGet != null && _onGetTracking != null)
    {
        _onGetTracking.RecordCall();
        return _onGet();
    }
    throw new InvalidOperationException("InvokeGetCallback called without callback configured");
}

// For getter tracking (separate from callback invocation):
internal void RecordGet() => _unconfiguredGetCount++;

// For setter:
internal bool HasOnSet => _onSet != null || (_setSequence?.Count ?? 0) > 0;
internal void InvokeSetCallback(T value)
{
    if (_setSequence != null && _setSequenceIndex < _setSequence.Count)
    {
        var (callback, tracking) = _setSequence[_setSequenceIndex];
        tracking.RecordCall(value);
        _setSequenceIndex++;
        callback(value);
        return;
    }
    if (_onSet != null && _onSetTracking != null)
    {
        _onSetTracking.RecordCall(value);
        _onSet(value);
        return;
    }
    throw new InvalidOperationException("InvokeSetCallback called without callback configured");
}

// For setter tracking (separate from callback invocation):
internal void RecordSet(T value) { _unconfiguredSetCount++; _unconfiguredLastSetValue = value; }
```

**NOTE:** `RecordSet` already exists in init-only property interceptors (line 107-108 of PropertyInterceptorRenderer.cs). For regular properties, we need to add `RecordGet()` and `RecordSet()` methods that ONLY track (do not invoke callbacks), plus `HasOnGet`/`HasOnSet` boolean properties and `InvokeGetCallback()`/`InvokeSetCallback()` methods.

**3.4. Conditional generation of new interceptor methods**

The new methods (`HasOnGet`, `InvokeGetCallback`, `RecordGet`, `HasOnSet`, `InvokeSetCallback`, `RecordSet`) should ALWAYS be generated for flat stub property interceptors. They add minimal overhead and simplify the generation logic. Properties without user override will simply not use these methods - they continue using `InvokeGet(strict)`/`InvokeSet(strict, value)`.

### Phase 4: Placeholder (Merged into Phase 3)

Phase 4 from the original plan (interface implementation) has been merged into Phase 3 above, as they are the same concern.

### Phase 5: Design.Stubs Examples

**5.1. Create `StubOverrideProperties/StubOverridePropertyBasics.cs`**

File: `src/Design/Design.Stubs/StubOverrideProperties/StubOverridePropertyBasics.cs`

Parallel to `UserMethodBasics.cs`:
- Basic get-only property override
- Get/set property override with backing field
- Set-only property override
- Mixed scenario (some properties overridden, some not)
- OnGet/OnSet superseding user override
- Strict mode with user property overrides

**5.2. Add all four applicable patterns**

Demonstrate in each pattern:
- Standalone
- Generic Standalone
- Standalone Class
- Generic Standalone Class

### Phase 6: Tests

**6.1. Create test file**

File: `src/Design/Design.Tests/StubOverridePropertyTests/StubOverridePropertyBasicsTests.cs`

Tests per Test Strategy section above.

### Phase 7: Documentation

**7.1. Update skill documentation**

Update knockoff skill with user property examples.

---

## Acceptance Criteria

- [ ] Get-only properties can be overridden with `protected override T Prop_ => ...`
- [ ] Get/set properties can be overridden with `protected override T Prop_ { get; set; }`
- [ ] Set-only properties can be overridden with `protected override T Prop_ { set => ... }`
- [ ] OnGet/OnSet supersedes user override per-test
- [ ] VerifyGet/VerifySet track calls through user overrides
- [ ] LastSetValue captured through user overrides
- [ ] Strict mode bypassed for overridden properties
- [ ] All four standalone patterns supported
- [ ] Design.Stubs examples demonstrate all scenarios
- [ ] Tests pass for all scenarios

---

## Dependencies

- Existing user methods infrastructure (base class generation, override detection)
- Property interceptor classes

---

## Risks / Considerations

### R1: Partial Override of Get/Set

**Risk:** User overrides only getter but not setter (or vice versa) for get/set property.

**Mitigation:** C# requires overriding both accessors if you override either. The compiler enforces this - not a KnockOff concern.

### R2: Indexers

**Question:** Should indexers also support user overrides?

**Decision:** Defer to separate todo. Indexers are more complex (key parameters) and less commonly needed. Focus on properties first.

### R3: Expression-Bodied Override Syntax

**Consideration:** Users may want `protected override int Count_ => value;` for get-only.

**Status:** This works naturally in C# - no special handling needed.

---

## Existing Test Impact Assessment

**Added to address C4 concern.**

### Tests That May Be Affected

The implementation changes `RenderPropertyImplementation()` in FlatRenderer.cs by adding a conditional branch for `HasUserOverride`. This could affect tests that exercise property interface implementations.

**Files Examined:**
- `src/Design/Design.Tests/PropertyTests/PropertyBasicsTests.cs`
- `src/Design/Design.Tests/PropertyTests/PropertySequenceTests.cs`
- `src/Tests/KnockOffTests/PropertyValueOverloadTests.cs`

### Analysis

**1. PropertyBasicsTests.cs** - 11 test methods covering:
- `OnGet_SetsConstantGetterValue` - Tests OnGet configuration
- `OnGet_WithCallback` - Tests OnGet with callback
- `OnSet_InterceptsSetter` - Tests OnSet configuration
- `LastSetValue_CapturesSetterArgument` - Tests LastSetValue tracking
- `BackingStore_WithClosurePattern` - Tests OnGet + OnSet together
- `VerifyGet_ChecksGetterAccess` - Tests VerifyGet
- `VerifySet_ChecksSetterAccess` - Tests VerifySet
- `UnconfiguredProperty_ReturnsDefault` - Tests default behavior
- `Reset_ClearsTrackingPreservesConfig` - Tests Reset behavior

**Impact:** NONE. These tests use INLINE stubs (`PropertyBasicsDemo.Stubs.IEntity`), not standalone stubs. User override only applies to standalone patterns. The `HasUserOverride` flag will be `false` for all inline stubs, so code path is unchanged.

**2. PropertySequenceTests.cs** - Tests property sequence functionality (ThenGet, ThenSet)

**Impact:** NONE. Same as above - uses inline stubs.

**3. PropertyValueOverloadTests.cs** - Tests property value overloads

**Impact:** NONE. Uses standalone stub (`PropertyTestKnockOff`), but the stub does NOT define any property overrides (no `override` keyword on properties with `_` suffix). The `HasUserOverride` flag will be `false`, so existing code path is used.

### Conclusion

**No existing tests should be affected.** The changes are additive:
1. New code path (`RenderPropertyUserOverrideImplementation`) only executes when `HasUserOverride == true`
2. No existing stubs define property user overrides
3. Inline stubs are excluded from user override feature by design
4. The new interceptor methods (`RecordGet`, `RecordSet`, `HasOnGet`, `HasOnSet`, `InvokeGetCallback`, `InvokeSetCallback`) are additions that don't change existing method signatures

**Tests to add (new, not modifications):**
- `src/Design/Design.Tests/StubOverridePropertyTests/StubOverridePropertyBasicsTests.cs` - New test file per Phase 6

---

## Design Clarification Needed

### DC1: Interceptor Architecture for User Override Call - RESOLVED

**STATUS: RESOLVED - Option B selected. Phase 3 updated to match.**

**Question:** Where should the user override call live?

**Option A: In interceptor's InvokeGet/InvokeSet** (NOT SELECTED)
- Interceptor needs access to the user override virtual property
- Requires passing a delegate/func to the interceptor
- More complex but keeps logic centralized

**Option B: In interface implementation (like user methods)** (SELECTED)
- Interface impl checks interceptor state, then calls virtual property
- Simpler - mirrors user method pattern exactly
- Pattern: `if (interceptor.HasOnGet) return interceptor.InvokeGetCallback(); else return PropertyName_;`

**Decision:** Option B - matches user methods pattern exactly.

**Implementation:** Phase 3 has been updated to implement Option B with:
1. New `RenderPropertyUserOverrideImplementation()` method in FlatRenderer.cs
2. Routing from `RenderPropertyImplementation()` when `HasUserOverride == true`
3. New interceptor methods: `RecordGet()`, `RecordSet()`, `HasOnGet`, `HasOnSet`, `InvokeGetCallback()`, `InvokeSetCallback()`

For user methods (current implementation):
```csharp
// Interface impl (from RenderUserOverrideImplementation)
int IService.Calculate(int a, int b)
{
    Calculate.RecordCall(a, b);
    if (Calculate.Callback is { } callback) return callback(a, b);
    return Calculate_(a, b);  // User override or base default
}
```

For user properties (implementation in Phase 3):
```csharp
// Interface impl (from RenderPropertyUserOverrideImplementation)
int IEntity.Count
{
    get
    {
        Count.RecordGet();
        if (Count.HasOnGet) return Count.InvokeGetCallback();  // OnGet wins
        return Count_;  // User override or base default
    }
}
```

This keeps the interceptor simpler (just callback storage + tracking) while the interface impl handles the priority logic.

---

## Architectural Verification

**Architect:** knockoff-architect
**Date:** 2026-02-04

### Four Applicable Patterns Analysis

| Pattern | Applies | Analysis |
|---------|---------|----------|
| 1. Standalone | Yes | Primary target. Base class (`{StubName}Base`) receives virtual properties with `_` suffix. User's partial class overrides them. |
| 2. Generic Standalone | Yes | Same as Standalone. Type parameters from stub class flow to base class properties. No special handling needed. |
| 3. Standalone Class | Yes | Already has base class generation (for methods). Properties added following same pattern. Uses `.Object` accessor. |
| 4. Generic Standalone Class | Yes | Same as Standalone Class with type parameters. |
| 5-9. Inline patterns | No | Inline patterns generate entire stub class. No partial for user overrides. This is by design. |

### Breaking Changes Assessment

**Breaking Changes:** No

- Additive feature only
- Existing stubs unchanged - they already have base classes with methods
- New virtual properties are optional to override
- Default behavior preserved (properties return default or use interceptor path)

### Pattern Consistency Check

**Follows existing user methods pattern exactly:**

| Aspect | User Methods | User Properties (Proposed) |
|--------|-------------|----------------------------|
| Override detection | Syntactic via `override` keyword + `_` suffix | Same - detect `override` on property with `_` suffix |
| Base class member | `protected virtual ReturnType MethodName_(...) => default!;` | `protected virtual ReturnType PropertyName_ => default!;` (get-only) |
| Naming convention | `MethodName_` | `PropertyName_` |
| Priority | OnCall > User override > Default | OnGet/OnSet > User override > Default |
| Tracking | `RecordCall()` before any execution path | `RecordGet()`/`RecordSet()` before any execution path |
| Strict mode | User override bypasses strict (it IS configured) | Same - user override bypasses strict |

### Codebase Deep-Dive

**Files Examined:**

1. **`src/Generator/KnockOffGenerator.Helpers.cs` (lines 21-53)**
   - `DetectUserOverrideMethods()` provides template for property detection
   - Uses syntactic detection via `DeclaringSyntaxReferences`
   - Checks for `override` modifier + `_` suffix
   - Returns `HashSet<string>` of signature keys

2. **`src/Generator/Renderer/FlatRenderer.cs` (lines 200-350)**
   - `RenderBaseClass()` currently only renders methods
   - **Gap:** No property rendering in base class
   - `RenderUserOverrideImplementation()` (lines 3244-3284) shows interface impl pattern

3. **`src/Generator/Model/Flat/FlatMethodModel.cs`**
   - Contains `HasUserOverride` boolean
   - **Gap:** `FlatPropertyModel.cs` lacks this field

4. **`src/Generator/Model/Flat/FlatPropertyModel.cs`**
   - Current fields: InterceptorName, DeclaringInterface, MemberName, ReturnType, HasGetter, HasSetter, etc.
   - **Need to add:** `HasUserOverride` field

5. **`src/Generator/Renderer/Shared/PropertyInterceptorRenderer.cs` (lines 301-399)**
   - `RenderInvokeGet()` has priority chain but no user override path
   - **Need to modify:** Add user override call after OnGet/sequence check

6. **`src/Generator/Builder/FlatModelBuilder.cs`**
   - `BuildOverrideSignatureKeyFromMember()` builds signature keys for methods
   - **Need to add:** Similar detection for properties

7. **`src/Design/Design.Stubs/UserMethods/UserMethodBasics.cs`**
   - Comprehensive examples of user method pattern
   - Shows: basic overrides, tracking, OnCall superseding, strict mode bypass, async, overloads
   - **Use as template** for `StubOverrideProperties/StubOverridePropertyBasics.cs`

8. **Generated code sample:** `BasicUserMethodStub.Base.g.cs`
   - Shows clean base class with virtual methods only
   - **Pattern to follow** for property generation

### Diagnostic Requirements

No new diagnostics required. Existing patterns:
- `KO0200` - User already warned if they add explicit base class (conflicts with generated base)
- Override typos caught by compiler ("no suitable method to override")

### Edge Cases Identified

1. **Get/Set partial override** - C# forbids overriding only one accessor. Compiler enforces. No KnockOff concern.

2. **Init-only properties** - Currently use special `SetValue()` pattern. User override should still work for getter. Need to verify init setter handling.

3. **Property hiding in interfaces** - If interface A derives from B and both have `Name`, dedupe logic already handles this for methods. Verify it works for properties.

4. **Multiple interfaces with same property name** - Already handled by `FlatModelBuilder` deduplication.

5. **Reference type null handling** - `protected virtual string Name_ => default!;` uses `default!` to suppress nullability warning. Same pattern as methods.

### Test Strategy

**New test file:** `Design.Tests/StubOverridePropertyTests/StubOverridePropertyBasicsTests.cs`

Tests required:
1. Get-only property override is called when no OnGet configured
2. Get/set property override is called (both accessors)
3. Set-only property override is called when no OnSet configured
4. OnGet supersedes property override per-test
5. OnSet supersedes property override per-test
6. VerifyGet/VerifySet track calls through user override
7. LastSetValue captured through user override
8. Strict mode bypassed for overridden properties
9. Reset preserves OnGet/OnSet configuration (user override still active after reset)
10. Mixed scenario: some properties overridden, some not
11. All four standalone patterns work correctly

---

## Developer Review

**Status:** Approved
**Reviewed:** 2026-02-04
**Reviewer:** knockoff-developer

---

## Architect Response to Concerns (2026-02-04)

### C1 Resolution: Phase 3 vs DC1 Inconsistency - RESOLVED

**Resolution:** Phase 3 has been completely rewritten to implement DC1 Option B (interface implementation approach).

**Changes Made:**
- Removed Phase 3.1-3.3 which described modifying `PropertyInterceptorRenderer.RenderInvokeGet/Set()`
- Added Phase 3.1: New `RenderPropertyUserOverrideImplementation()` method
- Added Phase 3.2: Routing from `RenderPropertyImplementation()` when `HasUserOverride == true`
- Added Phase 3.3: New interceptor methods required for the pattern
- Merged original Phase 4 into Phase 3 (they were the same concern)
- Marked DC1 as RESOLVED with Option B selected

The implementation now mirrors `RenderUserOverrideImplementation()` for methods exactly.

### C2 Resolution: Missing Interceptor Methods - RESOLVED

**Resolution:** Phase 3.3 now explicitly specifies the NEW interceptor methods required:

For getters:
- `RecordGet()` - Tracking only (does not invoke callback)
- `HasOnGet` - Boolean property to check if callback configured
- `InvokeGetCallback()` - Invokes callback without tracking (tracking done separately)

For setters:
- `RecordSet(T value)` - Tracking only (does not invoke callback)
- `HasOnSet` - Boolean property to check if callback configured
- `InvokeSetCallback(T value)` - Invokes callback without tracking (tracking done separately)

**Note:** `RecordSet` already exists for init-only properties. The new methods follow the same pattern but are added to regular property interceptors as well.

### C3 Resolution: Property Deduplication Logic - RESOLVED

**Resolution:** Phase 2.1 now includes explicit deduplication logic for properties:

- Properties keyed by name only (properties cannot be overloaded)
- When multiple interfaces have same property name, prefer version with MORE accessors (get/set > get-only > set-only)
- Delegation targets skipped (same as methods)
- Code example provided showing the deduplication dictionary pattern

This handles cases like `IReadOnlyEntity.Name { get; }` + `IMutableEntity.Name { get; set; }` by selecting the get/set version for the base class.

### C4 Resolution: Test Impact Assessment - RESOLVED

**Resolution:** Added new section "Existing Test Impact Assessment" after R3.

**Analysis Results:**
- PropertyBasicsTests.cs - NO IMPACT (uses inline stubs)
- PropertySequenceTests.cs - NO IMPACT (uses inline stubs)
- PropertyValueOverloadTests.cs - NO IMPACT (standalone stub but no property overrides)

**Conclusion:** No existing tests should be affected. Changes are additive - new code path only executes when `HasUserOverride == true`, and no existing stubs define property user overrides.

---

### My Understanding of This Plan

**Core Change:** Extend the user methods base class pattern to support user-defined property implementations in standalone stubs. Users will override generated virtual properties (with `_` suffix) to provide default behavior.

**User-Facing API:**
- Users write `protected override int Count_ => _items.Count;` in their partial stub class
- Priority: OnGet/OnSet > User override > Default
- Works with all three property accessor patterns (get-only, set-only, get/set)

**Internal Changes:**
1. Add `HasUserOverride` field to `FlatPropertyModel`
2. Add `DetectUserOverrideProperties()` function in `KnockOffGenerator.Helpers.cs`
3. Add `UserOverrideProperties` to `KnockOffTypeInfo`
4. Modify base class generation to include virtual properties
5. Modify property interface implementation to check user override

**Patterns Affected:** Standalone patterns only (1-4). Inline patterns (5-9) are explicitly excluded.

---

### Codebase Investigation

**Files Examined:**

- `src/Generator/KnockOffGenerator.Helpers.cs` (lines 21-53) - Contains `DetectUserOverrideMethods()`. This is the template for the proposed `DetectUserOverrideProperties()`. Uses syntactic detection via `DeclaringSyntaxReferences`, checks for `override` modifier and `_` suffix.

- `src/Generator/Model/Flat/FlatPropertyModel.cs` - Currently has 14 fields. **Confirmed gap:** No `HasUserOverride` field exists.

- `src/Generator/Model/Flat/FlatMethodModel.cs` (line 31) - Has `HasUserOverride`. This is the pattern to follow.

- `src/Generator/Renderer/FlatRenderer.cs` (lines 200-352) - `RenderBaseClass()` currently only renders methods. **Confirmed gap:** No property rendering in base class.

- `src/Generator/Renderer/FlatRenderer.cs` (lines 3095-3132) - `RenderPropertyImplementation()` uses `InvokeGet(Strict)` and `InvokeSet(Strict, value)`. For user override support, this will need modification.

- `src/Generator/Renderer/FlatRenderer.cs` (lines 3244-3284) - `RenderUserOverrideImplementation()` for methods shows the pattern: check callback, then call virtual method.

- `src/Generator/Renderer/Shared/PropertyInterceptorRenderer.cs` (lines 301-477) - `RenderInvokeGet()` and `RenderInvokeSet()` implement current priority chain.

- `src/Generator/Models/CommonModels.cs` (line 32) - `KnockOffTypeInfo` has `UserOverrideMethods`. A parallel `UserOverrideProperties` field will be needed.

- `src/Design/Design.Stubs/UserMethods/UserMethodBasics.cs` - Good template for the proposed `StubOverridePropertyBasics.cs`.

**Searches Performed:**
- Searched for "HasUserOverride" - found 21 usages in FlatModelBuilder.cs, FlatRenderer.cs, FlatMethodModel.cs
- Searched for "RenderBaseClass" - found 3 usages, all in FlatRenderer.cs

**Discrepancies Found:**
- Plan Phase 3.1 mentions modifying `InvokeGet` in PropertyInterceptorRenderer, but DC1 recommends Option B (handle in interface implementation). These are contradictory approaches.

---

### Concerns

#### C1: Internal Inconsistency Between Phase 3 and DC1 - RESOLVED

**Category:** Plan Clarity
**Status:** RESOLVED - See "Architect Response to Concerns" section above.

~~**Details:** Phase 3.1-3.3 describe modifying `PropertyInterceptorRenderer.RenderInvokeGet()` and `RenderInvokeSet()` to include user override calls. However, DC1 recommends Option B, which explicitly states the user override call should happen in the **interface implementation**, NOT in the interceptor.~~

**Resolution:** Phase 3 completely rewritten to implement DC1 Option B. Now includes `RenderPropertyUserOverrideImplementation()` in FlatRenderer.cs.

---

#### C2: Missing Property-Specific User Override Renderer - RESOLVED

**Category:** Implementation Completeness
**Status:** RESOLVED - See "Architect Response to Concerns" section above.

~~**Details:** For methods with `HasUserOverride`, the code uses `RenderUserOverrideImplementation()`. For properties, we need a parallel function. However, property interceptors don't have `RecordGet()` method - tracking is embedded in `InvokeGet`.~~

**Resolution:** Phase 3.3 now specifies new interceptor methods: `RecordGet()`, `RecordSet()`, `HasOnGet`, `HasOnSet`, `InvokeGetCallback()`, `InvokeSetCallback()`.

---

#### C3: Base Class Property Deduplication Logic Not Specified - RESOLVED

**Category:** Design Gap
**Status:** RESOLVED - See "Architect Response to Concerns" section above.

~~**Details:** For methods, `RenderBaseClass()` handles signature conflicts. The plan didn't specify equivalent logic for properties.~~

**Resolution:** Phase 2.1 now includes explicit deduplication logic - prefer version with MORE accessors (get/set > get-only > set-only).

---

#### C4: Test Impact Not Assessed - RESOLVED

**Category:** Risk Assessment
**Status:** RESOLVED - See "Architect Response to Concerns" section above.

~~**Details:** The plan lists new tests to add but doesn't assess whether existing tests might fail.~~

**Resolution:** Added "Existing Test Impact Assessment" section. Conclusion: No existing tests affected (additive changes only).

---

### What Looks Good

- Clear explanation of the three property accessor patterns (get-only, set-only, get/set)
- Comprehensive pattern coverage analysis (all 9 patterns considered)
- Sound design decisions (D1-D5) that follow established user methods precedent
- Good edge case identification (R1-R3 in Risks section)
- Thorough architectural verification with specific file references
- Test strategy with 11 specific test cases

---

### Recommendation

**APPROVED for implementation.**

All four concerns have been satisfactorily addressed by the architect:

1. **C1 - Phase 3 Consistency:** Phase 3 now correctly implements DC1 Option B. The `RenderPropertyUserOverrideImplementation()` method mirrors the existing `RenderUserOverrideImplementation()` for methods.

2. **C2 - Interceptor Methods:** Phase 3.3 specifies the six new interceptor methods needed. The pattern matches init-only properties which already have `RecordSet()`.

3. **C3 - Property Deduplication:** Phase 2.1 includes explicit deduplication logic (key by name, prefer more accessors), mirroring the method deduplication pattern.

4. **C4 - Test Impact:** Assessment confirms no existing tests affected (all use inline stubs or standalone stubs without property overrides).

### Why This Plan Is Approved

- **Internally consistent:** Phase 3 now aligns with DC1 Option B decision
- **Pattern precedent:** Follows established user methods implementation exactly
- **Complete specification:** All new methods and their signatures are documented
- **Edge cases covered:** Property deduplication, accessor variants, init-only properties
- **Test impact assessed:** Additive changes only, no breaking changes expected
- **Clear implementation phases:** Logical progression from model to renderer to tests

---

## Implementation Contract

**Created:** 2026-02-04
**Approved by:** knockoff-developer

### In Scope

**Phase 1: Model and Detection Changes**
- [ ] Add `HasUserOverride` field to `src/Generator/Model/Flat/FlatPropertyModel.cs`
- [ ] Add `DetectUserOverrideProperties()` function to `src/Generator/KnockOffGenerator.Helpers.cs`
- [ ] Add `UserOverrideProperties: EquatableArray<string>` to `KnockOffTypeInfo` in `src/Generator/Models/CommonModels.cs`
- [ ] Update `src/Generator/KnockOffGenerator.Transform.cs` to call `DetectUserOverrideProperties()` and pass to type info
- [ ] Update `src/Generator/Builder/FlatModelBuilder.cs` to set `HasUserOverride` on property models
- [ ] **Checkpoint:** Build solution, verify no compilation errors

**Phase 2: Base Class Property Generation**
- [ ] Add property deduplication logic to `RenderBaseClass()` in `src/Generator/Renderer/FlatRenderer.cs`
- [ ] Add `RenderBaseClassProperty()` method in `src/Generator/Renderer/FlatRenderer.cs`
- [ ] Call `RenderBaseClassProperty()` for each deduplicated property in `RenderBaseClass()`
- [ ] **Checkpoint:** Build solution, verify base class renders virtual properties

**Phase 3: Interface Implementation and Interceptor Changes**
- [ ] Add `RenderPropertyUserOverrideImplementation()` method in `src/Generator/Renderer/FlatRenderer.cs`
- [ ] Modify `RenderPropertyImplementation()` to route to new method when `HasUserOverride == true`
- [ ] Add `RecordGet()`, `HasOnGet`, `InvokeGetCallback()` methods to `PropertyInterceptorRenderer.cs` (for getters)
- [ ] Add `RecordSet(T)`, `HasOnSet`, `InvokeSetCallback(T)` methods to `PropertyInterceptorRenderer.cs` (for setters)
- [ ] **Checkpoint:** Build solution, run existing property tests, verify they pass

**Phase 4: Design.Stubs Examples**
- [x] Create `src/Design/Design.Stubs/StubOverrideProperties/StubOverridePropertyBasics.cs` with comprehensive examples
- [x] Include all three accessor patterns (get-only, set-only, get/set)
- [x] Include all four applicable standalone patterns
- [x] Include OnGet/OnSet superseding user override examples
- [x] Include strict mode with user property override examples
- [x] **Checkpoint:** Build Design.Stubs project, verify generated code compiles

**Phase 5: Tests**
- [ ] Create `src/Design/Design.Tests/StubOverridePropertyTests/StubOverridePropertyBasicsTests.cs`
- [ ] Test: Get-only property override is called when no OnGet configured
- [ ] Test: Get/set property override is called (both accessors)
- [ ] Test: Set-only property override is called when no OnSet configured
- [ ] Test: OnGet supersedes property override per-test
- [ ] Test: OnSet supersedes property override per-test
- [ ] Test: VerifyGet/VerifySet track calls through user override
- [ ] Test: LastSetValue captured through user override
- [ ] Test: Strict mode bypassed for overridden properties
- [ ] Test: Reset preserves OnGet/OnSet configuration
- [ ] Test: Mixed scenario (some properties overridden, some not)
- [ ] Test: All four standalone patterns work correctly
- [ ] **Checkpoint:** All tests pass

**Phase 6: Documentation**
- [ ] Update knockoff skill with user property examples

### Explicitly Out of Scope

- Indexer user overrides (separate feature - indexers have key parameters, more complex)
- Inline patterns (by design - no user partial available)
- Event user overrides (separate feature if needed)
- Init-only property user overrides for the setter (only getter can be overridden)

### Verification Gates

1. **After Phase 1:** Solution builds, `FlatPropertyModel` has `HasUserOverride` field
2. **After Phase 2:** Base class renders virtual properties with `_` suffix
3. **After Phase 3:** Existing property tests still pass (no regression)
4. **After Phase 4:** Design.Stubs compiles with user property examples
5. **Final:** All new tests pass, all existing tests pass

### Stop Conditions

If any of these occur, STOP and report:
- Out-of-scope test fails (especially existing PropertyBasicsTests, PropertySequenceTests)
- Architectural contradiction discovered (generated code pattern differs from specification)
- Generated code does not compile
- Init-only property user override causes unexpected behavior

---

## Implementation Progress

### Phase 1: Model and Detection Changes - COMPLETE

**Completed:** 2026-02-04

- [x] Added `HasUserOverride` field to `FlatPropertyModel.cs`
- [x] Added `DetectUserOverrideProperties()` function to `KnockOffGenerator.Helpers.cs`
- [x] Added `UserOverrideProperties: EquatableArray<string>` to `KnockOffTypeInfo` in `CommonModels.cs`
- [x] Updated `KnockOffGenerator.Transform.cs` to call `DetectUserOverrideProperties()` and pass to type info
- [x] Updated `FlatModelBuilder.cs` to set `HasUserOverride` on property models
- [x] **Checkpoint:** Build succeeded, no compilation errors

### Phase 2: Base Class Property Generation - COMPLETE

**Completed:** 2026-02-04

- [x] Added property deduplication logic to `RenderBaseClass()` in `FlatRenderer.cs`
- [x] Added `RenderBaseClassProperty()` method in `FlatRenderer.cs`
- [x] Base class now renders virtual properties with `_` suffix for all interface properties
- [x] **Checkpoint:** Build succeeded, base class renders virtual properties (verified in `EntityBaseStub.Base.g.cs`)

### Phase 3: Interface Implementation and Interceptor Changes - COMPLETE

**Completed:** 2026-02-04

- [x] Added `RenderUserOverrideSupportMethods()` to `PropertyInterceptorRenderer.cs`
  - Added `RecordGet()` - tracking only method
  - Added `HasOnGet` - boolean property to check if callback configured
  - Added `InvokeGetCallback()` - invokes callback without tracking
  - Added `RecordSet(T)` - tracking only method
  - Added `HasOnSet` - boolean property to check if callback configured
  - Added `InvokeSetCallback(T)` - invokes callback without tracking
- [x] Added `RenderPropertyUserOverrideImplementation()` method in `FlatRenderer.cs`
- [x] Modified `RenderPropertyImplementation()` to route to new method when `HasUserOverride == true`
- [x] **Checkpoint:** Build succeeded, all existing property tests pass (1091 tests on net10.0)

---

## Completion Evidence (Phases 1-3)

**Generator Agent Scope Complete:** 2026-02-04

### Test Results

All tests pass across all target frameworks:
- KnockOffTests.dll (net10.0): 1091 passed
- KnockOffTests.dll (net9.0): 1091 passed
- KnockOffTests.dll (net8.0): 1090 passed
- KnockOff.NeatooInterfaceTests.dll: 473 passed (all frameworks)
- KnockOff.Documentation.Samples.dll: 409 passed (all frameworks)
- KnockOffTests.AssemblyStrict.dll: 14 passed (all frameworks)

### Files Modified

**Phase 1:**
- `src/Generator/Model/Flat/FlatPropertyModel.cs` - Added `HasUserOverride` field
- `src/Generator/KnockOffGenerator.Helpers.cs` - Added `DetectUserOverrideProperties()` function
- `src/Generator/Models/CommonModels.cs` - Added `UserOverrideProperties` to `KnockOffTypeInfo`
- `src/Generator/KnockOffGenerator.Transform.cs` - Call detection and pass to type info
- `src/Generator/Builder/FlatModelBuilder.cs` - Set `HasUserOverride` on property models

**Phase 2:**
- `src/Generator/Renderer/FlatRenderer.cs` - Added property deduplication and `RenderBaseClassProperty()`

**Phase 3:**
- `src/Generator/Renderer/Shared/PropertyInterceptorRenderer.cs` - Added user override support methods
- `src/Generator/Renderer/FlatRenderer.cs` - Added `RenderPropertyUserOverrideImplementation()`

### Generated Code Sample (Base Class)

```csharp
// From EntityBaseStub.Base.g.cs
public class EntityBaseStubBase
{
    /// <summary>Override to provide default implementation for global::Neatoo.IEntityBase.Root.</summary>
    protected virtual global::Neatoo.IValidateBase? Root_ => default!;

    /// <summary>Override to provide default implementation for global::Neatoo.IEntityBase.ModifiedProperties.</summary>
    protected virtual global::System.Collections.Generic.IEnumerable<string> ModifiedProperties_ => default!;

    // ... more properties with _ suffix ...
}
```

### Phase 4: Design.Stubs Examples - COMPLETE

**Completed:** 2026-02-04
**Agent:** knockoff-developer (Examples Agent)

- [x] Created `src/Design/Design.Stubs/StubOverrideProperties/StubOverridePropertyBasics.cs` with comprehensive examples
- [x] All three accessor patterns demonstrated (get-only, set-only, get/set)
- [x] All four applicable standalone patterns demonstrated:
  - Pattern 1: Standalone (`BasicStubOverridePropertyStub : IStubOverridePropertyService`)
  - Pattern 2: Generic Standalone (`GenericStubOverridePropertyStub<T> : IGenericStubOverridePropertyService<T>`)
  - Pattern 3: Standalone Class (`ConfigStubOverridePropertyStub` with `[KnockOffBase<ConfigBase>]`)
  - Pattern 4: Generic Standalone Class (`CacheStubOverridePropertyStub<T>` with `[KnockOffBase(typeof(CacheBase<>))]`)
- [x] OnGet/OnSet superseding user override examples included
- [x] Strict mode with user property override examples included
- [x] Mixed scenario (some properties overridden, some not) demonstrated
- [x] **Checkpoint:** Build succeeded, Design.Stubs compiles, all tests pass

**Files Created:**
- `src/Design/Design.Stubs/StubOverrideProperties/StubOverridePropertyBasics.cs` - Comprehensive examples file
- `src/Design/Design.Domain/Services/IStubOverridePropertyService.cs` - Interfaces for user property demos
- `src/Design/Design.Domain/Abstractions/ConfigBase.cs` - Abstract classes for class stub demos

**Test Results After Phase 4:**
All tests pass across all target frameworks:
- KnockOffTests.dll (net10.0): 1091 passed
- KnockOffTests.dll (net9.0): 1091 passed
- KnockOffTests.dll (net8.0): 1090 passed
- KnockOff.NeatooInterfaceTests.dll: 473 passed (all frameworks)
- KnockOff.Documentation.Samples.dll: 409 passed (all frameworks)
- KnockOffTests.AssemblyStrict.dll: 14 passed (all frameworks)

### Remaining Phases

- **Phase 5:** Tests - Create `Design.Tests/StubOverridePropertyTests/StubOverridePropertyBasicsTests.cs`
- **Phase 6:** Documentation - Update knockoff skill with user property examples
