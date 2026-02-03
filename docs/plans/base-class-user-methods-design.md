# Base Class User Methods Design

**Date:** 2026-02-02
**Related Todo:** [Base Class User Methods](../todos/base-class-user-methods.md)
**Status:** Draft (Architect)
**Last Updated:** 2026-02-03

---

## Overview

Replace the current user method detection (matching protected methods to interface signatures) with a base class pattern where:
1. Generator creates `{ClassName}Base` with virtual protected methods suffixed with `_`
2. Users write `protected override` methods
3. Signature mismatches cause compile errors
4. Tracker properties use clean names (no '2' suffix)

---

## Current State

### How User Methods Work Today

```csharp
// User writes:
[KnockOff]
public partial class RepoStub : IRepo { }

public partial class RepoStub {
    protected User? GetUserById(int id) {
        return new User { Id = id, Name = "Default" };
    }
}

// Generator produces:
public partial class RepoStub : IKnockOffStub {
    public GetUserById2Interceptor GetUserById2 { get; }  // '2' suffix!

    User? IRepo.GetUserById(int id) {
        GetUserById2.RecordCall(id);
        // OnCall supersedes user method (added 2026-02-02)
        if (GetUserById2.Callback is { } callback) return callback(id);
        return GetUserById(id);  // User method is fallback
    }
}
```

### Current Capabilities (as of 2026-02-02)

User method interceptors (`*2`) now have **full API support**:
- `OnCall(callback)` - Override user method behavior per-test
- `Returns(value)` - Shorthand for constant return
- `Verify(Times)` - Verify call count
- `LastArg`/`LastArgs` - Access last arguments
- `Reset()` - Clears tracking, **preserves OnCall configuration**

**Priority:** OnCall supersedes user method → user method is fallback

### Remaining Problems

1. **'2' suffix**: `stub.GetUserById2.Verify()` is confusing
2. **Silent breakage**: Change `GetUserById(int id)` to `GetUserById(string id)` → no error, method ignored
3. **Discovery**: User must know exact signature; no IntelliSense help

---

## Proposed Design

### Generated Structure

```csharp
// ═══════════════════════════════════════════════════════════
// GENERATED BASE CLASS (RepoStub.Base.g.cs)
// ═══════════════════════════════════════════════════════════
public class RepoStubBase {
    protected virtual Task<Order> GetById_(int id) {
        throw new NotImplementedException("Override GetById_ or configure stub.GetById");
    }

    protected virtual Task<IList<Order>> GetAll_() {
        throw new NotImplementedException("Override GetAll_ or configure stub.GetAll");
    }
}

// ═══════════════════════════════════════════════════════════
// USER CODE (unchanged location, but now uses override)
// ═══════════════════════════════════════════════════════════
[KnockOff]
public partial class RepoStub : IRepo {
    private List<Order> _orders;

    public RepoStub(List<Order> orders) => _orders = orders;

    // Override with underscore suffix - compiler enforces signature!
    protected override Task<Order> GetById_(int id) {
        return Task.FromResult(_orders.Single(o => o.Id == id));
    }
    // GetAll_ NOT overridden - will use interceptor
}

// ═══════════════════════════════════════════════════════════
// GENERATED PARTIAL (RepoStub.g.cs)
// ═══════════════════════════════════════════════════════════
public partial class RepoStub : RepoStubBase, IKnockOffStub {
    public bool Strict { get; set; }

    // Clean tracker names!
    public GetByIdInterceptor GetById { get; } = new();
    public GetAllInterceptor GetAll { get; } = new();

    // GetById - user provided override (OnCall can still supersede)
    Task<Order> IRepo.GetById(int id) {
        GetById.RecordCall(id);
        if (GetById.Callback is { } callback) return callback(id);  // OnCall wins
        return GetById_(id);  // User's override is fallback
    }

    // GetAll - no override detected, use interceptor
    Task<IList<Order>> IRepo.GetAll() {
        return GetAll.Invoke(Strict);
    }
}
```

### Naming Convention

| Member Type | Base Class Method | Tracker Property |
|-------------|-------------------|------------------|
| Method | `GetById_(int id)` | `GetById` |
| Parameterless | `GetAll_()` | `GetAll` |
| Overload 1 | `Process_(int x)` | `Process` |
| Overload 2 | `Process_(string x)` | `Process` |
| Generic | `Find_<T>(T item)` | `Find` (with `.Of<T>()`) |

The `_` suffix is:
- Minimal visual noise
- Clearly distinguishes from interface method
- IntelliSense shows `GetById_` when user types `GetById`
- Consistent across all member types

---

## Analysis: Overloads, Generics, and Properties

### 1. Overloads - WORKS NATURALLY

**Interface:**
```csharp
interface IFormatter {
    string Format(string input);
    string Format(string input, FormatOptions options);
    string Format(string input, FormatOptions options, int maxLength);
}
```

**Generated base class:**
```csharp
public class FormatterStubBase {
    protected virtual string Format_(string input) { throw new NotImplementedException(); }
    protected virtual string Format_(string input, FormatOptions options) { throw new NotImplementedException(); }
    protected virtual string Format_(string input, FormatOptions options, int maxLength) { throw new NotImplementedException(); }
}
```

**User can override ANY SUBSET:**
```csharp
public partial class FormatterStub {
    // Override only the first overload
    protected override string Format_(string input) => input.ToUpper();

    // Leave others to use interceptor
}
```

**Generator behavior:**
- For each overload, check if user provided override
- If override exists → call `Format_(...)`
- If no override → call interceptor path

**Detection:** The generator can detect overrides by looking for `IsOverride` on protected methods with `_` suffix and matching parameter types. C# method overload resolution works the same in base/derived as in single class.

**Current KnockOff overload handling:**
- Single interceptor property for all overloads: `stub.Format`
- `OnCall` resolved by lambda signature: `stub.Format.OnCall((input) => ...)` vs `stub.Format.OnCall((input, options) => ...)`
- Each overload has independent tracking via returned builder

**With base class approach:**
- Same interceptor property: `stub.Format`
- User overrides specific overloads via `Format_(int x)` vs `Format_(string x)`
- Non-overridden overloads still use interceptor
- OnCall on interceptor supersedes user override (same as current behavior)
- **This is ORTHOGONAL to current overload handling** - just a different way to provide default behavior

**Conclusion:** Overloads work naturally. Each overload becomes a separate virtual method in the base class. Users override the ones they want. OnCall/Returns still work on the clean-named interceptor (`stub.Format`) to override per-test.

---

### 2. Generic Methods - COMPLEX, NEEDS ANALYSIS

**Current KnockOff pattern for generic methods:**
```csharp
// Interface:
T? GetById<T>(int id) where T : class, new();

// Generated:
public GenericMethodHandler<T?> GetById { get; }  // Uses .Of<T>() pattern

// Usage:
stub.GetById.Of<User>().OnCall((id) => new User { Id = id });
stub.GetById.Of<Order>().OnCall((id) => new Order { Id = id });
```

**Base class approach challenge:**

```csharp
// Base class would have:
protected virtual T? GetById_<T>(int id) where T : class, new() {
    throw new NotImplementedException();
}

// User override:
protected override T? GetById_<T>(int id) {
    // How do they return different types for different T?
    // This is a SINGLE method handling ALL type arguments!
}
```

**The Problem:**
- User's override is ONE method for ALL type arguments
- Current `.Of<T>()` pattern allows DIFFERENT callbacks per type argument
- These are fundamentally different approaches

**Options:**

**Option A: User override replaces ALL type-specific behavior**
```csharp
protected override T? GetById_<T>(int id) {
    if (typeof(T) == typeof(User))
        return (T?)(object?)new User { Id = id };
    if (typeof(T) == typeof(Order))
        return (T?)(object?)new Order { Id = id };
    return default;
}
```
- Ugly type checks and casts
- Loses type safety benefits
- Not ergonomic

**Option B: User override as fallback, .Of<T>() takes priority**
```csharp
// Generator behavior:
T? IRepo.GetById<T>(int id) {
    // Try .Of<T>() first
    if (GetById.Of<T>().IsConfigured)
        return GetById.Of<T>().Invoke(id);

    // Fall back to user override
    return GetById_<T>(id);
}
```
- User override becomes the "default for unconfigured types"
- .Of<T>() still works for type-specific configuration
- Reasonable but adds complexity

**Option C: Don't support user overrides for generic methods**
- Generic methods use current pattern only (.Of<T>())
- Base class doesn't generate virtual methods for generic members
- Simplest, but reduces feature coverage

**Decision:** Exclude generic methods from base class pattern.

Reasons:
1. User override requires ugly type switching with casts
2. Verification still needs `.Of<T>()` per-type - awkward split between behavior (override) and verification (`.Of<T>()`)
3. Current `.Of<T>()` pattern handles both behavior AND verification consistently

---

### 3. Properties - NOT CURRENTLY SUPPORTED

**Current state:** `GetUserDefinedMethods()` explicitly filters with `!member.IsProperty`:
```csharp
// From KnockOffGenerator.Helpers.cs:
foreach (var member in iface.Members)
{
    if (!member.IsProperty)  // ← Properties excluded!
    {
        var sig = GetMethodSignature(...);
        interfaceMethodSignatures.Add(sig);
    }
}
```

**Properties are NOT supported for user definition today.** This is intentional - properties use `OnGet`/`OnSet` pattern which is quite different from method callbacks.

**With base class approach, properties COULD work:**

**Interface:**
```csharp
interface IConfig {
    string ConnectionString { get; }      // Get-only
    int Timeout { get; set; }             // Get/set
}
```

**Base class:**
```csharp
public class ConfigStubBase {
    protected virtual string ConnectionString_ => throw new NotImplementedException();
    protected virtual int Timeout_ {
        get => throw new NotImplementedException();
        set => throw new NotImplementedException();
    }
}
```

**User override:**
```csharp
public partial class ConfigStub {
    protected override string ConnectionString_ => "Server=localhost;...";

    private int _timeout = 30;
    protected override int Timeout_ {
        get => _timeout;
        set => _timeout = value;
    }
}
```

**Challenges:**
1. **Get/set requires backing field** - Can't use auto-property in override
2. **Tracker naming conflict** - Same as methods: `ConnectionString` (tracker) vs `ConnectionString_` (override)
3. **Verification** - How does `VerifyGet()`/`VerifySet()` work with user overrides?

**Tracker for user-overridden property:**
```csharp
public partial class ConfigStub : ConfigStubBase {
    public ConnectionStringInterceptor ConnectionString { get; }  // Tracker

    string IConfig.ConnectionString {
        get {
            ConnectionString.RecordGet();
            return ConnectionString_;  // Calls user's override
        }
    }
}
```

**This is similar to method handling** - tracker records, then delegates to user override.

**Recommendation:** Defer to Phase 2. The pattern is viable but:
- Methods are higher priority (more common use case)
- Get/set backing field is clunkier than method override
- Current property API (`OnGet`/`OnSet`) is already quite ergonomic

---

## Open Questions (Updated)

### 4. How is user-defined base class handled?

**Problem:** User might already have a base class:
```csharp
public partial class RepoStub : ExistingBaseClass, IRepo { }
```

**C# constraint:** A class can only extend one base class.

**Options:**
1. **Block with diagnostic:** Emit error KO0xxx "Standalone stubs cannot have a base class"
2. **Require base to extend generated:** User's `ExistingBaseClass` must extend `RepoStubBase`
3. **Fall back to current behavior:** If base class detected, use current '2' suffix approach

**Recommendation:** Option 1 - block with diagnostic. This is a rare edge case, and the benefit of clean naming outweighs supporting this scenario.

---

### 5. What goes in base class virtual method body?

**Options:**
1. `throw new NotImplementedException("Override or configure via interceptor")`
2. `throw new InvalidOperationException("Method not overridden")`
3. `return default!;` (silent failure)

**Recommendation:** Option 1 with helpful message. If user forgets to override AND doesn't configure interceptor, they get a clear error.

**Note:** The generated explicit interface implementation should NEVER call the base method if no override exists. It should use the interceptor path. The base method body is only hit if:
- User overrides but calls `base.GetById_(id)` (unlikely)
- Generator bug

---

## Detection Algorithm

### How does the generator know which methods are overridden?

**Approach 1: Look for `override` keyword in user's partial**
```csharp
// In transform phase, scan user's class for override methods
var userOverrides = classSymbol.GetMembers()
    .OfType<IMethodSymbol>()
    .Where(m => m.IsOverride && m.Name.EndsWith("_"))
    .ToList();
```

**Approach 2: Rely on naming convention**
- If user declares `protected override GetById_(int id)`, it matches base class
- Generator doesn't need to detect - just generates both paths
- If override exists, explicit impl calls it; if not, uses interceptor

**Issue:** Can the generator see the override during the same compilation where it generates the base class?

**Source Generator Timing:**
1. Generator runs on syntax pass
2. Produces base class with virtual methods
3. Compiler adds generated code to compilation
4. Semantic analysis sees user's override against generated base

This should work because the generator adds the base class to the compilation, and subsequent semantic analysis sees both.

---

## Breaking Changes

This is a **breaking change** for users of user methods:

| Before | After |
|--------|-------|
| `protected GetById(int id)` | `protected override GetById_(int id)` |
| `stub.GetById2.Verify()` | `stub.GetById.Verify()` |

**Migration:**
1. Add `override` keyword
2. Add `_` suffix to method name
3. Remove `2` from tracker access in tests

**Mitigation:**
- Pre-1.0, breaking changes are acceptable
- Could add analyzer/fixer to help migration

---

## Architectural Verification

**Three Patterns Analysis:**
- Standalone: Primary target - this feature only applies here
- Inline Interface: N/A - no user code in generated stubs
- Inline Class: N/A - different pattern (extends concrete class, not interface)

**Breaking Changes:** Yes - see above

**Pattern Consistency:** This introduces a new pattern (generated base class) not used elsewhere in KnockOff. Need to ensure it integrates cleanly with:
- `IKnockOffStub` interface
- Source delegation
- Strict mode
- Verification
- **OnCall/Returns** - Maintains same priority: OnCall supersedes user override (just like current `*2` interceptors)

**Codebase Analysis:** See todo progress log for exploration details.

---

## Summary of Analysis

| Feature | Status | Notes |
|---------|--------|-------|
| **Methods** | ✅ Full support | Core feature, works naturally |
| **Overloads** | ✅ Full support | Each overload = separate virtual method |
| **Generic methods** | ❌ Excluded | Behavior/verification split makes it awkward; `.Of<T>()` handles both |
| **Properties** | ⏳ Defer to Phase 2 | Viable but lower priority |
| **Indexers** | ❓ TBD | Similar to properties, likely defer |
| **Events** | ❓ TBD | Need to analyze event handler pattern |

---

## Next Steps

1. ~~Investigate overload detection mechanism~~ ✅ Works naturally
2. ~~Verify generic method compatibility~~ ✅ Recommend: exclude from base class
3. ~~Decide on property support~~ ✅ Defer to Phase 2
4. Design diagnostic for user-defined base class
5. Prototype base class generation
6. Handle source generator timing for override detection

---

## Developer Review

**Status:** Not Started

**Concerns:** [To be filled by developer review]

---

## Implementation Contract

[To be filled before implementation]

---

## Implementation Progress

[To be filled during implementation]

---

## Completion Evidence

[Required before marking complete]
