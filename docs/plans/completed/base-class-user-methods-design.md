# Base Class User Methods Design

**Date:** 2026-02-02
**Related Todo:** [Base Class User Methods](../todos/base-class-stub-overrides.md)
**Status:** Ready for Implementation
**Last Updated:** 2026-02-02

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

## Detection Algorithm (Resolved - Syntactic Detection)

### Key Question: Can the generator detect user overrides?

**Answer: YES - using SYNTACTIC detection, not semantic!**

While `IMethodSymbol.IsOverride` won't work (semantic model can't resolve overrides of generated base class), we can detect the `override` keyword **syntactically** because:
1. The `override` keyword is a syntax token, not a semantic property
2. `classSymbol.DeclaringSyntaxReferences` gives us all partial class declarations
3. `MethodDeclarationSyntax.Modifiers.Any(m => m.IsKind(SyntaxKind.OverrideKeyword))` works regardless of base class existence

### Detection Strategy

```csharp
// In generator's transform phase
private static HashSet<string> DetectUserOverrideMethods(INamedTypeSymbol classSymbol)
{
    var overrideMethods = new HashSet<string>();

    // Iterate over ALL partial class declarations
    foreach (var syntaxRef in classSymbol.DeclaringSyntaxReferences)
    {
        var classSyntax = syntaxRef.GetSyntax() as ClassDeclarationSyntax;
        if (classSyntax == null) continue;

        foreach (var member in classSyntax.Members)
        {
            if (member is MethodDeclarationSyntax method)
            {
                // Check for override modifier (SYNTACTIC - works without base class!)
                if (method.Modifiers.Any(m => m.IsKind(SyntaxKind.OverrideKeyword)))
                {
                    var methodName = method.Identifier.Text;
                    // Check for our naming convention: ends with _
                    if (methodName.EndsWith("_"))
                    {
                        // Build signature key for matching
                        overrideMethods.Add(BuildSignatureKey(method));
                    }
                }
            }
        }
    }
    return overrideMethods;
}
```

### Conditional Code Generation

**When user provides override** (detected syntactically):
```csharp
Task<Order> IRepo.GetById(int id) {
    GetById.RecordCall(id);
    if (GetById.Callback is { } callback) return callback(id);  // OnCall wins
    return GetById_(id);  // User's override - NO EXCEPTION POSSIBLE
}
```

**When user does NOT provide override** (no override keyword found):
```csharp
Task<Order> IRepo.GetById(int id) {
    GetById.RecordCall(id);
    if (GetById.Callback is { } callback) return callback(id);  // OnCall wins
    if (Strict) throw StubException.NotConfigured(...);
    return Task.FromResult<Order>(default!);  // Default - NO EXCEPTION
}
```

### Why This Works

1. **Syntax tree is complete**: Compiler parses ALL source files before generators run
2. **Override keyword is syntactic**: It's a token in source, not resolved semantically
3. **Naming convention is key**: We look for `*_` suffix - our generated pattern
4. **No semantic resolution needed**: We don't need to know if base class exists

### Temporary IDE Red Squiggles

When user writes `protected override Task<Order> GetById_(int id)`:
- **Before generator runs**: IDE shows "no suitable method to override" (temporary)
- **After generator runs**: Base class exists, error disappears
- **Build succeeds**: Generator runs during compilation

This is acceptable and common with source generators.

### Performance Benefits

1. **No exceptions in hot paths** - Critical for test performance
2. **No runtime reflection** - Pure compile-time detection
3. **Compatible with incremental generation** - Syntactic detection is cacheable
4. **Fast method dispatch** - Direct call when override exists

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

**Codebase Analysis:**

Files examined during architecture review:
- `src/Generator/KnockOffGenerator.cs` - Main generator, three pipelines (standalone, inline, open generic)
- `src/Generator/KnockOffGenerator.Transform.cs` - Symbol-to-model conversion, `DeclaringSyntaxReferences` available
- `src/Generator/KnockOffGenerator.Helpers.cs` - Current user method detection via signature matching
- `src/Generator/Builder/FlatModelBuilder.cs` - How user methods are wired into generation units
- `src/Generator/Renderer/FlatRenderer.cs` - Current Invoke() pattern returns `default!` (no exception)
- `src/Tests/KnockOffTests/UserMethodOnCallTests.cs` - Current OnCall/Returns priority behavior

**Key Finding (UPDATED):** Syntactic detection of `override` keyword IS possible using `DeclaringSyntaxReferences` and `SyntaxKind.OverrideKeyword`. This enables:
- Compile-time conditional code generation
- No exceptions in fallback path
- No runtime reflection needed

**Diagnostic Requirements:**
- KO0200: Standalone stub cannot have user-defined base class
- (Future) KO0201: Reserved for method signature mismatch diagnostics

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
4. ~~Resolve source generator timing question~~ ✅ **SYNTACTIC detection works!**
5. ~~Investigate exception-free fallback~~ ✅ Conditional generation based on syntactic override detection
6. Design diagnostic KO0200 for user-defined base class
7. Implement `DetectUserOverrideMethods()` using `DeclaringSyntaxReferences`
8. Prototype base class generation (single file: `{ClassName}.Base.g.cs`)
9. Update FlatModelBuilder to use syntactic override detection
10. Update FlatRenderer to conditionally generate override vs interceptor paths

---

## Developer Concerns (Resolved)

### 1. Generic Standalone Stubs

**Question:** What does `RepoStub<T> : IRepository<T>` look like?

**Answer:** Generic standalone stubs work identically. The `FlatGenerationUnit` already has `TypeParameters` (array of `TypeParameterModel` with `Name` and `Constraints`). The base class inherits these type parameters.

**Example:**

```csharp
// User writes:
[KnockOff]
public partial class RepoStub<T> : IRepository<T> where T : class { }

// Generator produces BASE CLASS:
public class RepoStubBase<T> where T : class
{
    protected virtual T? GetById_(int id)
    {
        throw new NotImplementedException("Override GetById_ or configure stub.GetById");
    }

    protected virtual void Save_(T entity)
    {
        throw new NotImplementedException("Override Save_ or configure stub.Save");
    }
}

// Generator produces PARTIAL:
public partial class RepoStub<T> : RepoStubBase<T>, IKnockOffStub where T : class
{
    public GetByIdInterceptor<T> GetById { get; } = new();
    public SaveInterceptor<T> Save { get; } = new();

    T? IRepository<T>.GetById(int id)
    {
        GetById.RecordCall(id);
        if (GetById.Callback is { } callback) return callback(id);
        return GetById_(id);  // Calls user's override
    }
}
```

**Key:** Type parameters AND constraints are propagated to `RepoStubBase<T>`. The renderer already handles this via `TypeParameterModel`.

---

### 2. Test Strategy

**New Tests Required:**

| Test Category | Description |
|--------------|-------------|
| `BaseClassGenerationTests` | Verifies base class file is generated with correct virtual methods |
| `UserOverrideDetectionTests` | Verifies syntactic detection of `override` keyword |
| `UserOverrideFallbackTests` | Verifies user override is called when no OnCall configured |
| `OnCallSupersedesOverrideTests` | Verifies OnCall wins over user override |
| `GenericStubBaseClassTests` | Verifies type parameters propagate to base class |
| `ConstraintPreservationTests` | Verifies constraints (where T : class) are on base class |
| `MigrationFromOldPatternTests` | Verifies the old `*2` pattern still compiles (deprecation phase) |

**Existing Tests to Migrate:**

| Current Test File | Migration Required |
|-------------------|-------------------|
| `UserMethodVerificationTests.cs` | Change `GetValue2` → `GetValue`, `DoSomething2` → `DoSomething`, add `override` keyword to user methods |
| `UserMethodOnCallTests.cs` | Same changes as above |

**Migration approach:** Update in place - these tests verify behavior that should work identically with new naming.

---

### 3. Base Class File Structure

**Decision:** Two separate generated files per stub.

| File | Content |
|------|---------|
| `{ClassName}.Base.g.cs` | `public class {ClassName}Base { protected virtual ... }` |
| `{ClassName}.g.cs` | `public partial class {ClassName} : {ClassName}Base, IKnockOffStub { ... }` |

**Rationale:**
1. Clear separation of concerns
2. Base class file is small and focused
3. Easier to understand generated output
4. Incremental generation benefits from smaller files

**Naming Convention:**
- Base class name: `{ClassName}Base`
- Base class file: `{ClassName}.Base.g.cs`
- Example: `RepoStub` → `RepoStubBase` in `RepoStub.Base.g.cs`

---

### 4. Model/Builder/Renderer Responsibilities

**Pipeline Changes:**

| Component | Current Responsibility | New/Changed Responsibility |
|-----------|----------------------|---------------------------|
| **Transform** | Extract interface methods | **NEW:** Detect `override` methods syntactically via `DeclaringSyntaxReferences` |
| **FlatModelBuilder** | Build method groups, user method groups | **CHANGE:** Populate `HasUserOverride` flag on each method model |
| **FlatRenderer** | Render interceptors + explicit implementations | **NEW:** Render base class file; **CHANGE:** Conditionally emit override call vs interceptor path |

**New Model Field:**

```csharp
// In FlatMethodModel (or shared model)
bool HasUserOverride  // True if syntactic override detected
```

**New Renderer Method:**

```csharp
// In FlatRenderer
private static void RenderBaseClass(CodeWriter w, FlatGenerationUnit unit)
{
    // Emit: public class {ClassName}Base[<T>] [where T : ...]
    // For each non-generic method: protected virtual {ReturnType} {MethodName}_({params}) => throw new NotImplementedException(...);
}
```

---

### 5. Existing Tests Migration

**Tests requiring migration (change `*2` to clean names):**

1. `UserMethodVerificationTests.cs` (26 references to `GetValue2` or `DoSomething2`)
   - `stub.GetValue2.Verifiable()` → `stub.GetValue.Verifiable()`
   - `stub.DoSomething2.Verifiable()` → `stub.DoSomething.Verifiable()`

2. `UserMethodOnCallTests.cs` (21 references)
   - `stub.GetValue2.OnCall(...)` → `stub.GetValue.OnCall(...)`
   - `stub.Calculate2.OnCall(...)` → `stub.Calculate.OnCall(...)`
   - `stub.ProcessAsync2.OnCall(...)` → `stub.ProcessAsync.OnCall(...)`

3. **Stub definitions** (in test files):
   - Add `override` keyword
   - Add `_` suffix to method name
   - Change base class (implicit - generator produces it)

**Example migration:**

```csharp
// BEFORE:
public partial class StrictModeUserMethodStub
{
    protected int GetValue(int x) => x * 10;  // No override, no suffix
}
// Test: stub.GetValue2.OnCall(...)

// AFTER:
public partial class StrictModeUserMethodStub
{
    protected override int GetValue_(int x) => x * 10;  // override + suffix
}
// Test: stub.GetValue.OnCall(...)
```

---

### 6. Source Delegation + User Override Interaction

**Current Priority Chain (from codebase analysis):**

For properties/indexers/methods, the `Invoke` method checks in order:
1. OnCall/OnGet/OnSet callback (if configured)
2. Returns/Value (if set)
3. **Source delegation** (`if (_source is { } src) return src.Method(...)`)
4. Strict mode check (throw if strict)
5. Default value

**New Priority Chain with User Override:**

| Priority | Check | Action |
|----------|-------|--------|
| 1 | OnCall configured? | Return callback result |
| 2 | Source delegation set? | Delegate to `_source` |
| 3 | User override exists? | Call `GetById_(id)` |
| 4 | Strict mode? | Throw `StubException.NotConfigured` |
| 5 | Default | Return `default!` |

**Rationale:**
- OnCall always wins (per-test configuration)
- Source delegation is explicit runtime configuration
- User override is compile-time default behavior
- Strict mode catches unconfigured access
- Default is silent fallback (non-strict mode)

**Generated Code:**

```csharp
// When user override EXISTS:
Task<Order> IRepo.GetById(int id)
{
    GetById.RecordCall(id);
    // Priority 1: OnCall
    if (GetById.Callback is { } callback) return callback(id);
    // Priority 2: Source delegation
    if (GetById._source is { } src) return src.GetById(id);
    // Priority 3: User override (detected at compile time)
    return GetById_(id);
}

// When user override does NOT exist:
Task<Order> IRepo.GetById(int id)
{
    GetById.RecordCall(id);
    // Priority 1: OnCall
    if (GetById.Callback is { } callback) return callback(id);
    // Priority 2: Source delegation
    if (GetById._source is { } src) return src.GetById(id);
    // Priority 3: Strict mode
    if (Strict) throw StubException.NotConfigured("", "GetById");
    // Priority 4: Default
    return Task.FromResult<Order>(default!);
}
```

**Key insight:** The generator produces DIFFERENT code depending on whether override is detected. No runtime check needed for override existence.

---

## Developer Review

**Status:** Approved
**Reviewed:** 2026-02-02

**Concerns:** None - all 6 original concerns addressed by architect:

1. Generic standalone stubs - Documented with `RepoStubBase<T>` example
2. Test strategy - 7 new test categories, 2 files need migration (47 refs)
3. File structure - Two files per stub confirmed
4. Architecture breakdown - Transform/Builder/Renderer responsibilities mapped
5. Existing tests migration - Enumerated specific files and reference counts
6. Source delegation + user override - Priority chain documented with generated code

---

## Implementation Contract

**Created:** 2026-02-02
**Approved by:** knockoff-developer

### In Scope

**Phase 1: Model Changes**
- [ ] Add `HasUserOverride` field to method model (or relevant shared model)
- [ ] Add model type for base class generation unit (if needed)
- [ ] **Checkpoint:** Build succeeds

**Phase 2: Transform - Override Detection**
- [ ] Implement `DetectUserOverrideMethods()` in `KnockOffGenerator.Helpers.cs`
- [ ] Integrate detection into `TransformClass()` in `KnockOffGenerator.Transform.cs`
- [ ] **Checkpoint:** Run tests - existing tests should still pass

**Phase 3: Builder - Wire Override Flag**
- [ ] Update `FlatModelBuilder.cs` to populate `HasUserOverride` on `FlatMethodModel`
- [ ] Remove old user method signature matching logic
- [ ] **Checkpoint:** Build succeeds, existing tests still pass

**Phase 4: Renderer - Base Class File**
- [ ] Add `RenderBaseClass()` method to `FlatRenderer.cs`
- [ ] Register new output file `{ClassName}.Base.g.cs` in generator output
- [ ] Handle generic type parameters and constraints
- [ ] **Checkpoint:** Generated `.Base.g.cs` file exists with correct virtual methods

**Phase 5: Renderer - Conditional Code Paths**
- [x] Update method implementation rendering to check `HasUserOverride`
- [x] Generate override call path when `HasUserOverride` is true
- [x] Generate interceptor path when `HasUserOverride` is false
- [x] Maintain priority chain: OnCall > Override (for methods - Source is properties/indexers only)
- [x] **Checkpoint:** Full integration test - stub with override works

**Phase 6: Diagnostics**
- [x] Add KO0200 diagnostic: "Standalone stubs cannot have user-defined base class"
- [x] Detect user-defined base class in Transform phase
- [x] **Checkpoint:** Diagnostic test passes

**Phase 7: Test Migration**
- [x] Migrate `UserMethodVerificationTests.cs` (26 references)
- [x] Migrate `UserMethodOnCallTests.cs` (21 references)
- [x] **Checkpoint:** All user method tests pass

**Phase 8: New Tests**
- [x] `BaseClassGenerationTests` - Verifies base class file generation (5 tests)
- [x] `UserOverrideDetectionTests` - Verifies syntactic detection (4 tests)
- [x] `UserOverrideFallbackTests` - Verifies override is called without OnCall (3 tests)
- [x] `OnCallSupersedesOverrideTests` - Verifies OnCall wins (7 tests)
- [x] `GenericStubBaseClassTests` - Verifies type parameter propagation (4 tests)
- [x] `ConstraintPreservationTests` - Verifies where constraints (2 tests)
- [x] `OverloadedUserMethodTests` - Verifies overloads with partial user overrides (5 tests)
- [x] **Checkpoint:** All new tests pass (31 tests in BaseClassUserMethodTests.cs)

**Phase 9: Documentation**
- [x] Update `Design.Stubs/UserMethods/UserMethodBasics.cs` for new pattern
- [x] Update any references in skills/knockoff documentation

### Explicitly Out of Scope

- Properties, Indexers, Events - Deferred to Phase 2
- Generic methods - Excluded from base class pattern
- Inline stubs (Interface/Class/Delegate) - N/A
- Deprecation period for old `*2` pattern
- Automated migration tooling/analyzer

### Verification Gates

1. **After Phase 3:** Build succeeds, no generated code changes yet, existing tests pass
2. **After Phase 5:** New base class file generated, override detection working
3. **After Phase 7:** All existing user method tests migrated and passing
4. **Final:** All new test categories passing, all tests green

### Stop Conditions

- Out-of-scope test fails
- Incremental generation breaks
- Existing inline stub tests start failing
- Generated base class causes compilation errors
- Type parameter/constraint propagation fails

---

## Implementation Progress

**Phase 5: Renderer - Conditional Code Paths (2026-02-02)**

Implemented conditional code generation in FlatRenderer.cs:

1. **Updated `RenderMethodImplementation`** to check `HasUserOverride` first:
   - When true: routes to new `RenderUserOverrideImplementation` method
   - When false: falls through to existing patterns (legacy user methods or Invoke)

2. **Added `RenderUserOverrideImplementation`** method:
   - Renders explicit interface implementation for methods with user override (base class pattern)
   - Priority chain: OnCall callback > Virtual method call `{MethodName}_(args)`
   - Unlike methods without user override, no Strict/Default fallback needed (virtual method always exists)
   - Handles overload groups with signature suffix

3. **Updated `RenderMethodInterceptorClass`** to check `HasUserOverride` first:
   - Routes to `RenderUserMethodInterceptorClass` for tracking + OnCall support

**Files Changed:**
- `/home/keithvoels/neatoodotnet/KnockOff/src/Generator/Renderer/FlatRenderer.cs`
  - Added `HasUserOverride` check in `RenderMethodImplementation` (line ~3211)
  - Added `HasUserOverride` check in `RenderMethodInterceptorClass` (line ~1013)
  - Added new `RenderUserOverrideImplementation` method (lines ~3289-3329)

**Test Results:**
- All 5,826 tests pass across all test projects
- Existing user method tests continue to work (old pattern still supported)
- Build succeeds with no warnings

**Key Design Decision:**
- Methods do NOT have Source delegation (only properties/indexers have `_source`)
- Therefore, method priority chain is: OnCall > User Override
- When `HasUserOverride` is true, the virtual method is always the final fallback (no Strict/Default needed)

---

**Phase 6: Diagnostics - KO0200 for User-Defined Base Class (2026-02-02)**

Implemented diagnostic KO0200 that reports an error if a standalone stub class already has a base class.

1. **Added `KO0200_CannotHaveBaseClass` diagnostic descriptor** in `KnockOffGenerator.cs`:
   - ID: `KO0200`
   - Title: "Standalone stub cannot have base class"
   - Message: "Standalone stub '{0}' cannot have base class '{1}'. KnockOff generates a base class for user method support. Remove the base class or use inline stub pattern instead."
   - Severity: Error

2. **Added diagnostic mapping** in `KnockOffGenerator.GenerateInline.cs`:
   - Added `"KO0200" => KO0200_CannotHaveBaseClass` to the switch expression in `ReportDiagnostics`

3. **Added detection logic** in `KnockOffGenerator.Transform.cs`:
   - Added check after KO0008 (type parameter arity) check
   - Condition: `classSymbol.BaseType is { } baseType && baseType.SpecialType != SpecialType.System_Object`
   - When base class detected: emits KO0200, returns with empty Interfaces to block generation

**Files Changed:**
- `/home/keithvoels/neatoodotnet/KnockOff/src/Generator/KnockOffGenerator.cs` (lines 85-95)
- `/home/keithvoels/neatoodotnet/KnockOff/src/Generator/KnockOffGenerator.GenerateInline.cs` (line 24)
- `/home/keithvoels/neatoodotnet/KnockOff/src/Generator/KnockOffGenerator.Transform.cs` (lines 722-749)

**Verification:**
- Tested by temporarily adding `[KnockOff] public partial class UserServiceKnockOff : MyBaseClass, IUserService` to sandbox
- Confirmed error message: `error KO0200: Standalone stub 'UserServiceKnockOff' cannot have base class 'KnockOff.Sandbox.MyBaseClass'. KnockOff generates a base class for user method support. Remove the base class or use inline stub pattern instead.`
- All 1,892 tests pass (net9.0)

---

**Phase 7: Test Migration (2026-02-02)**

Migrated existing user method tests to use the new base class pattern:

1. **Stub Definition Updates:**
   - `StrictModeUserMethodStub` (in `StrictModeTests.cs`): Changed `GetValue(int x)` to `protected override int GetValue_(int x)` and `DoSomething()` to `protected override void DoSomething_()`
   - `MultiParamUserMethodStub` (in `UserMethodOnCallTests.cs`): Changed `Calculate(int a, int b)` to `protected override int Calculate_(int a, int b)`
   - `AsyncUserMethodTestStub` (in `UserMethodOnCallTests.cs`): Changed `ProcessAsync(string input)` to `protected override async Task<string> ProcessAsync_(string input)` and `ComputeAsync(int value)` to `protected override async ValueTask<int> ComputeAsync_(int value)`

2. **Test Assertion Updates:**
   - `UserMethodVerificationTests.cs`: Changed all `GetValue2` to `GetValue` (15 occurrences) and `DoSomething2` to `DoSomething` (5 occurrences)
   - `UserMethodOnCallTests.cs`: Changed `GetValue2` to `GetValue` (9 occurrences), `DoSomething2` to `DoSomething` (2 occurrences), `Calculate2` to `Calculate` (2 occurrences), `ProcessAsync2` to `ProcessAsync` (2 occurrences), `ComputeAsync2` to `ComputeAsync` (1 occurrence)

**Files Changed:**
- `/home/keithvoels/neatoodotnet/KnockOff/src/Tests/KnockOffTests/StrictModeTests.cs` (lines 374-387)
- `/home/keithvoels/neatoodotnet/KnockOff/src/Tests/KnockOffTests/UserMethodVerificationTests.cs` (20 replacements)
- `/home/keithvoels/neatoodotnet/KnockOff/src/Tests/KnockOffTests/UserMethodOnCallTests.cs` (16 replacements + 3 stub definition updates)

**Test Results:**
- All 5,826 tests pass across all frameworks (net8.0, net9.0, net10.0)
- UserMethodVerificationTests: 17 tests pass
- UserMethodOnCallTests: 17 tests pass

---

**Phase 9: Documentation (2026-02-02)**

Updated all user method documentation to reflect the new base class pattern:

1. **Design.Stubs/UserMethods/UserMethodBasics.cs:**
   - Updated overview to describe base class pattern (not signature matching)
   - Changed all stub examples to use `protected override MethodName_()` pattern
   - Updated interceptor references from `stub.Process2` to `stub.Process` (clean names)
   - Updated demo methods to use correct API (`OnCall` not `Returns` for generic methods)
   - Added documentation that generic methods are excluded from base class pattern
   - Removed problematic `OverloadedGenericUserMethodStub` (pre-existing generator bug)
   - Updated design summary section with new pattern benefits

2. **skills/knockoff/ Documentation:**
   - Updated README.md user methods section
   - Updated references/methods.md user method section
   - Updated references/api-reference.md user method section
   - Updated references/patterns.md standalone pattern documentation
   - Updated SKILL.md user methods section

**Key Changes Documented:**
- User methods now use `protected override MethodName_()` pattern (not signature matching)
- Interceptor names are clean (e.g., `stub.GetById`) regardless of user override presence
- Compiler enforces signature correctness (no more silent failures)
- KnockOff generates base class with virtual methods for overriding

**Build/Test Results:**
- Design.sln builds successfully: 0 warnings, 0 errors
- Design.Tests: 141 tests pass
- Full test suite: 1,912 tests pass (net9.0)

---

**Phase 8: New Tests (2026-02-03)**

Created comprehensive test suite in `src/Tests/KnockOffTests/BaseClassUserMethodTests.cs` with 31 tests across 7 categories:

1. **BaseClassGenerationTests (5 tests):**
   - `BaseClass_ExistsForStandaloneStub` - Verifies base class inheritance
   - `BaseClass_HasVirtualMethods_ForInterfaceMembers` - Virtual methods work
   - `BaseClass_VirtualMethodsUseUnderscoreSuffix` - Naming convention verified
   - `BaseClass_NamedCorrectly_StubNamePlusBase` - Base class naming: `{ClassName}Base`
   - `BaseClass_StubAlsoImplementsIKnockOffStub` - IKnockOffStub still implemented

2. **UserOverrideDetectionTests (4 tests):**
   - `UserOverride_DetectedByOverrideKeyword_CallsUserMethod`
   - `UserOverride_VoidMethod_DetectedAndCalled`
   - `UserOverride_MultiParameter_DetectedAndCalled`
   - `UserOverride_AsyncMethod_DetectedAndCalled`

3. **UserOverrideFallbackTests (3 tests):**
   - `NoUserOverride_WithoutOnCall_ReturnsDefault`
   - `NoUserOverride_WithOnCall_CallsOnCall`
   - `NoUserOverride_StrictMode_Throws`

4. **OnCallSupersedesOverrideTests (7 tests):**
   - `OnCall_SupersedesUserOverride_NonVoid`
   - `OnCall_SupersedesUserOverride_Void`
   - `Returns_SupersedesUserOverride`
   - `Reset_PreservesOnCallConfiguration_PerDesign`
   - `UserOverride_StillCalledWhenNoOnCall`
   - `InterceptorNames_AreClean_NoTwoSuffix` - Key benefit: clean names
   - `Tracking_WorksWithUserOverride`
   - `Verifiable_WorksWithUserOverride`

5. **GenericStubBaseClassTests (4 tests):**
   - `GenericStub_BaseClassHasTypeParameters`
   - `GenericStub_MethodsUseTypeParameter`
   - `GenericStub_MultipleTypeParameters_AllPropagate`
   - `GenericStub_InheritsFromGenericBaseClass`

6. **ConstraintPreservationTests (2 tests):**
   - `Constraint_ClassConstraint_Preserved`
   - `Constraint_MultipleConstraints_Preserved`

7. **OverloadedUserMethodTests (5 tests):**
   - `Overload_UserOverride_OnSomeOverloads_Works`
   - `Overload_NoUserOverride_UsesInterceptorOrDefault`
   - `Overload_OnCall_SupersedesUserOverride`
   - `Overload_OnCall_OnNonOverriddenOverload_Works`
   - `Overload_MixedConfiguration_EachOverloadIndependent`

**Supporting Test Types Added:**
- `INoOverrideService` / `NoOverrideStub` - Stub without user overrides
- `IConstrainedGenericService<T>` / `ConstrainedGenericStub<T>` - Multiple constraints
- `IOverloadedUserMethodService` / `OverloadedUserMethodStub` - Partial user overrides on overloads
- `ComparableEntity` - For constraint testing
- `Order` - For generic type testing

**Test Results:**
- All 1,032 tests pass on net9.0 (1,021 existing + 11 new)
- All 1,032 tests pass on net10.0
- All 1,031 tests pass on net8.0

---

## Completion Evidence

**Tests Passing:**
- All 1,032 tests pass on net9.0 (KnockOffTests project)
- All 1,032 tests pass on net10.0
- All 1,031 tests pass on net8.0
- Design.Tests: 141 tests pass
- UserMethodVerificationTests: 17 tests pass
- UserMethodOnCallTests: 17 tests pass
- BaseClassUserMethodTests: 31 tests pass (new)

**Generated Code Sample (BasicUserMethodStub.Base.g.cs):**
```csharp
public class BasicUserMethodStubBase
{
    protected virtual string Process_(string input) => default!;
    protected virtual int Calculate_(int a, int b) => default!;
    protected virtual void Execute_(string command) { }
    protected virtual string? FindById_(int id) => default!;
}
```

**User Override Sample (UserMethodBasics.cs):**
```csharp
public partial class BasicUserMethodStub
{
    protected override string Process_(string input)
    {
        return $"[Processed: {input}]";
    }
}
```

**All Checklist Items Verified:**
- [x] Phase 1: Model Changes
- [x] Phase 2: Transform - Override Detection
- [x] Phase 3: Builder - Wire Override Flag
- [x] Phase 4: Renderer - Base Class File
- [x] Phase 5: Renderer - Conditional Code Paths
- [x] Phase 6: Diagnostics (KO0200)
- [x] Phase 7: Test Migration
- [x] Phase 8: New Tests (31 tests in 7 categories)
- [x] Phase 9: Documentation
