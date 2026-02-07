# Standalone Class Stubs Design

**Date:** 2026-02-04
**Related Todo:** [Stand-Alone Class Stubs](../todos/standalone-class-stubs.md)
**Status:** Ready for Implementation
**Last Updated:** 2026-02-04

---

## Overview

This plan introduces two new stub patterns for standalone class stubs:

- **Pattern 8: Standalone Class** - `[KnockOffBase<Foo>]`
- **Pattern 9: Generic Standalone Class** - `[KnockOffBase(typeof(Foo<>))]`

These patterns allow users to create reusable, file-based stubs for classes with virtual/abstract members, enabling custom constructors, user-defined methods, and shared stub configurations across tests.

---

## Name Collision Analysis

### The Problem

The previous design attempted to use inheritance where the user's partial class inherits from a generated base class (Impl) that inherits from the target:

```
TargetClass (has virtual string Name)
     ^
     |
ServiceStubImpl (generated, has override string Name)
     ^
     |
ServiceStub (user's partial, wants NameInterceptor Name)  <-- COLLISION!
```

This causes a C# compiler error because `ServiceStub` inherits `override string Name` from `ServiceStubImpl`, then tries to declare `ServiceStub_NameInterceptor Name` - two properties with the same name but different types.

### Why Inline Class Stubs Don't Have This Problem

In the inline class stub pattern (ClassRenderer.cs), the architecture uses **composition with nested classes**:

```csharp
// Wrapper - holds interceptors with clean names
public class Stubs_ServiceBase : IKnockOffStub
{
    public NameInterceptor Name { get; }  // Clean interceptor name
    public ServiceBase Object { get; }     // Returns the Impl instance

    // Impl is nested and private - users never see it
    private sealed class Impl : ServiceBase
    {
        private readonly Stubs_ServiceBase _stub;
        public override string Name => _stub.Name.InvokeGet(_stub.Strict);
    }
}
```

The nested `Impl` class's `override string Name` is **hidden from users** because:
1. `Impl` is `private` - not accessible outside the wrapper
2. Users interact with the wrapper, which has clean interceptor names
3. `.Object` exposes `Impl` as `ServiceBase`, not as `Impl`

### Solution Options Analyzed

| Option | Description | Pros | Cons |
|--------|-------------|------|------|
| **1. `new` keyword** | `public new NameInterceptor Name` | Compiles | Hides useful inherited property; confusing semantics |
| **2. Suffix interceptors** | `NameInterceptor` instead of `Name` | Unambiguous | Breaks API consistency with inline stubs |
| **3. Composition pattern** | Nested Impl like inline stubs | Consistent with inline; clean API | Requires `.Object`; user's class doesn't directly inherit from target |
| **4. Three-level hierarchy** | Extra layer of indirection | Could work | Very complex |
| **5. Full composition** | User's stub uses composition, not inheritance | Clean separation | User's stub is not a `ServiceBase` |

### Selected Solution: Composition Pattern (Option 3)

**The standalone class stub will follow the SAME architecture as inline class stubs:**

1. **User's partial class** = wrapper (holds interceptors with clean names)
2. **Nested Impl class** = generated, inherits from target, delegates to wrapper
3. **`.Object` property** = returns the Impl instance as `TargetClass`

This is the **only** solution that:
- Avoids name collisions (interceptors on wrapper, overrides on nested Impl)
- Provides clean interceptor API (`stub.Name.Get(...)`)
- Is consistent with the existing inline class stub pattern
- Compiles correctly

**Trade-off:** The user's stub class does NOT directly inherit from the target. Users must use `.Object` to get the actual `ServiceBase` instance, just like inline class stubs.

---

## Approach

**ARCHITECTURE**: Following the inline class stub pattern exactly (wrapper + nested Impl).

**Key insight**: For class stubs, composition is the only way to avoid name collisions between interceptor properties and override properties.

```
User's partial class (ServiceStub) = wrapper
    |
    +-- Interceptor properties (Name, Execute, etc.)
    |
    +-- .Object property (returns the Impl instance)
    |
    +-- nested sealed class Impl : ServiceBase
            |
            +-- _stub reference to wrapper
            |
            +-- override string Name => _stub.Name.InvokeGet(...)
```

Key architectural decisions:

1. **New Attributes**: `KnockOffBaseAttribute<T>` and `KnockOffBaseAttribute` (with typeof parameter)
2. **Composition Pattern**: User's partial class is a WRAPPER, not derived from target
3. **Nested Impl Class**: Generated nested class that inherits from target and delegates to wrapper
4. **`.Object` Property**: Returns the nested Impl instance as `TargetClass`
5. **Consistent with Inline**: Follows the exact same pattern as inline class stubs

---

## Design

### 1. Attribute Definition

```csharp
// src/KnockOff/KnockOffBaseAttribute.cs

namespace KnockOff;

/// <summary>
/// Marks a partial class as a standalone stub for a concrete class.
/// The stub can override virtual/abstract members with interceptor-based behavior.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class KnockOffBaseAttribute<T> : Attribute where T : class
{
    /// <summary>
    /// When true, unconfigured method calls throw StubException instead of returning default.
    /// </summary>
    public bool Strict { get; set; }
}

/// <summary>
/// Marks a partial class as a standalone stub for an open generic class.
/// Use with typeof() syntax: [KnockOffBase(typeof(ServiceBase&lt;&gt;))]
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class KnockOffBaseAttribute : Attribute
{
    public KnockOffBaseAttribute(Type type)
    {
        Type = type;
    }

    public Type Type { get; }

    /// <summary>
    /// When true, unconfigured method calls throw StubException instead of returning default.
    /// </summary>
    public bool Strict { get; set; }
}
```

### 2. Composition Architecture (Corrected)

The user's partial class is a **wrapper** that holds interceptors. A **nested Impl class** inherits from the target and delegates to the wrapper. This follows the same architecture as inline class stubs.

```
User's partial class: ServiceStub (wrapper)
    |
    +-- Implements IKnockOffStub
    +-- Interceptor properties: Name, Execute, Initialize
    +-- .Object property (returns nested Impl as ServiceBase)
    +-- Custom user methods/fields
    |
    +-- nested private sealed class Impl : ServiceBase
            |
            +-- _stub reference to ServiceStub wrapper
            +-- override string Name => _stub.Name.InvokeGet(...)
            +-- override void Execute(...) => _stub.Execute.Invoke(...)
```

**CRITICAL INSIGHT**: This architecture avoids name collisions because:
- Interceptors (`Name`, `Execute`) live on `ServiceStub` (the wrapper)
- Overrides (`override string Name`) live on `Impl` (nested, private, hidden)
- Users never see the `Impl` class - they access it via `.Object` as `ServiceBase`

**Key Difference from Previous Design:**
- **Previous (broken)**: `ServiceStub` inherits from `ServiceStubImpl` which inherits from `ServiceBase`
- **Corrected**: `ServiceStub` is a wrapper with a NESTED `Impl` class that inherits from `ServiceBase`

**Pattern 8 Example - Non-Generic:**

```csharp
// User writes:
public abstract class ServiceBase
{
    public virtual string Name { get; set; } = "";
    public abstract void Execute(string command);
    public virtual void Initialize() { }
}

[KnockOffBase<ServiceBase>]
public partial class ServiceStub : ICustomInterface
{
    // User can add custom methods, fields, etc.
    public bool WasUsed { get; set; }

    public void MarkUsed() => WasUsed = true;
}
```

```csharp
// Generated (ServiceStub.g.cs):

// Interceptor classes (same as inline class stubs)
public sealed class ServiceStub_NameInterceptor { ... }
public sealed class ServiceStub_ExecuteInterceptor { ... }
public sealed class ServiceStub_InitializeInterceptor { ... }

// User's partial class completion - wrapper that holds interceptors
public partial class ServiceStub : global::KnockOff.IKnockOffStub
{
    // Interceptor properties - clean names, no collision!
    public ServiceStub_NameInterceptor Name { get; } = new();
    public ServiceStub_ExecuteInterceptor Execute { get; } = new();
    public ServiceStub_InitializeInterceptor Initialize { get; } = new();

    /// <summary>When true, unconfigured method calls throw StubException.</summary>
    public bool Strict { get; set; }

    /// <summary>The ServiceBase instance. Pass this to code expecting ServiceBase.</summary>
    public ServiceBase Object { get; }

    // Default constructor - creates the Impl instance
    public ServiceStub()
    {
        Object = new Impl(this);
    }

    // Constructor with parameters
    public ServiceStub(string name)
    {
        Object = new Impl(this, name);
    }

    public void ResetInterceptors()
    {
        Name.Reset();
        Execute.Reset();
        Initialize.Reset();
    }

    public void Verify() { /* ... */ }
    public void VerifyAll() { /* ... */ }

    // Nested Impl class - inherits from target, delegates to wrapper
    private sealed class Impl : global::ServiceBase
    {
        private readonly ServiceStub _stub;

        public Impl(ServiceStub stub) : base()
        {
            _stub = stub;
        }

        public Impl(ServiceStub stub, string name) : base(name)
        {
            _stub = stub;
        }

        // Property override - delegates to _stub's interceptor
        public override string Name
        {
            get
            {
                if (_stub.Name.IsConfigured) return _stub.Name.InvokeGet(_stub.Strict);
                _stub.Name.InvokeGet(_stub.Strict); // Track unconfigured call
                return base.Name;
            }
            set
            {
                _stub.Name.InvokeSet(_stub.Strict, value);
                if (!_stub.Name.IsConfigured) base.Name = value;
            }
        }

        // Abstract method - delegates to _stub's interceptor
        public override void Execute(string command)
        {
            _stub.Execute.Invoke(_stub.Strict, command);
        }

        // Virtual method - delegates to _stub's interceptor, falls back to base
        public override void Initialize()
        {
            var unconfiguredBefore = _stub.Initialize.UnconfiguredCallCount;
            _stub.Initialize.Invoke(_stub.Strict);
            if (_stub.Initialize.UnconfiguredCallCount > unconfiguredBefore)
            {
                base.Initialize();
            }
        }
    }
}
```

**Usage:**

```csharp
// Instantiate the stub
var stub = new ServiceStub();

// Configure interceptors with clean names
stub.Name.Get("TestService");
stub.Execute.OnCall(cmd => Console.WriteLine($"Executed: {cmd}"));

// Get the actual ServiceBase instance via .Object
ServiceBase service = stub.Object;

// Use it
Console.WriteLine(service.Name);  // "TestService"
service.Execute("test");          // "Executed: test"
service.Initialize();             // Calls base.Initialize() (not configured)
```

**Pattern 9 Example - Open Generic:**

```csharp
// User writes:
public abstract class RepositoryBase<T> where T : class
{
    public abstract T? GetById(int id);
    public abstract void Save(T entity);
}

[KnockOffBase(typeof(RepositoryBase<>))]
public partial class RepositoryStub<T> where T : class
{
    // User can add custom tracking
    private readonly List<T> _saved = [];
    public IReadOnlyList<T> SavedEntities => _saved;
}
```

```csharp
// Generated (RepositoryStub.g.cs):

// Interceptor classes with generic parameter
public sealed class RepositoryStub_GetByIdInterceptor<T> where T : class { ... }
public sealed class RepositoryStub_SaveInterceptor<T> where T : class { ... }

// User's partial class completion - wrapper
public partial class RepositoryStub<T> : global::KnockOff.IKnockOffStub
    where T : class
{
    public RepositoryStub_GetByIdInterceptor<T> GetById { get; } = new();
    public RepositoryStub_SaveInterceptor<T> Save { get; } = new();

    public bool Strict { get; set; }

    public RepositoryBase<T> Object { get; }

    public RepositoryStub()
    {
        Object = new Impl(this);
    }

    public void ResetInterceptors() { /* ... */ }
    public void Verify() { /* ... */ }
    public void VerifyAll() { /* ... */ }

    // Nested Impl class
    private sealed class Impl : global::RepositoryBase<T>
    {
        private readonly RepositoryStub<T> _stub;

        public Impl(RepositoryStub<T> stub)
        {
            _stub = stub;
        }

        public override T? GetById(int id)
        {
            return _stub.GetById.Invoke(_stub.Strict, id);
        }

        public override void Save(T entity)
        {
            _stub.Save.Invoke(_stub.Strict, entity);
        }
    }
}
```

### 3. Generator Pipeline

Add a new pipeline to `KnockOffGenerator.cs`:

```csharp
// Pipeline 4: Standalone class stubs [KnockOffBase<T>]
var standaloneClassStubs = context.SyntaxProvider.ForAttributeWithMetadataName(
    "KnockOff.KnockOffBaseAttribute`1",
    predicate: static (node, _) => IsStandaloneClassCandidate(node),
    transform: static (ctx, _) => ctx)
    .Combine(assemblyConfigProvider)
    .Select(static (data, _) =>
    {
        var (ctx, assemblyConfig) = data;
        return TransformStandaloneClassStub(ctx, assemblyConfig);
    })
    .Where(static info => info is not null);

context.RegisterSourceOutput(standaloneClassStubs, static (spc, info) =>
{
    if (info is not null)
    {
        GenerateStandaloneClassStub(spc, info);
    }
});

// Pipeline 5: Open generic standalone class stubs [KnockOffBase(typeof(T<>))]
var openGenericStandaloneClassStubs = context.SyntaxProvider.ForAttributeWithMetadataName(
    "KnockOff.KnockOffBaseAttribute",
    predicate: static (node, _) => IsStandaloneClassCandidate(node) && HasTypeofArgument(node),
    transform: static (ctx, _) => ctx)
    .Combine(assemblyConfigProvider)
    .Select(static (data, _) =>
    {
        var (ctx, assemblyConfig) = data;
        return TransformOpenGenericStandaloneClassStub(ctx, assemblyConfig);
    })
    .Where(static info => info is not null);

context.RegisterSourceOutput(openGenericStandaloneClassStubs, static (spc, info) =>
{
    if (info is not null)
    {
        GenerateStandaloneClassStub(spc, info);
    }
});
```

### 4. Predicate Function

```csharp
/// <summary>
/// Predicate: partial class, not abstract, not sealed
/// </summary>
private static bool IsStandaloneClassCandidate(SyntaxNode node)
{
    if (node is not ClassDeclarationSyntax classDecl)
        return false;

    // Must be partial
    if (!classDecl.Modifiers.Any(SyntaxKind.PartialKeyword))
        return false;

    // Cannot be abstract (user's stub class must be concrete)
    if (classDecl.Modifiers.Any(SyntaxKind.AbstractKeyword))
        return false;

    // Cannot be sealed (but this is fine since user's class is the leaf)
    // Actually, sealed IS allowed for standalone class stubs - user may want to prevent further derivation

    return true;
}
```

### 5. Model Structure

Create new model types in `src/Generator/Model/StandaloneClass/`:

```csharp
// StandaloneClassGenerationUnit.cs
internal sealed record StandaloneClassGenerationUnit(
    string ClassName,           // User's stub class name (e.g., "ServiceStub")
    string ImplClassName,       // Generated base name (e.g., "ServiceStubImpl")
    string TargetClassName,     // The class being stubbed (e.g., "ServiceBase")
    string Namespace,
    EquatableArray<TypeParameterModel> TypeParameters,
    EquatableArray<ContainingTypeModel> ContainingTypes,
    EquatableArray<string> UserInterfaces,  // Additional interfaces user implements
    EquatableArray<StandaloneClassPropertyModel> Properties,
    EquatableArray<StandaloneClassIndexerModel> Indexers,
    EquatableArray<UnifiedMethodInterceptorModel> Methods,
    EquatableArray<StandaloneClassEventModel> Events,
    EquatableArray<StandaloneClassConstructorModel> Constructors,
    EquatableArray<InlineInterceptorPropertyModel> InterceptorProperties,
    EquatableArray<string> ResetStatements,
    bool HasRequiredMembers,
    EquatableArray<string> RequiredMemberNames,
    bool IsOpenGeneric,
    bool Strict);
```

### 6. Diagnostics

Reusing existing KO20XX series for class stub diagnostics:

| Code | Severity | Message | Status |
|------|----------|---------|--------|
| KO2001 | Error | Cannot stub sealed class | Existing |
| KO2002 | Error | Type has no accessible constructors | Existing |
| KO2003 | Info | Non-virtual member skipped | Existing |
| KO2004 | Warning | Class has no virtual or abstract members | Existing |
| KO2005 | Error | Cannot stub static class | Existing |
| KO2006 | Error | Cannot stub built-in type | Existing |
| KO2007 | Error | Type parameter mismatch (for open generic pattern) | **NEW** |
| KO2008 | Error | User class must be partial | **NEW** |

### 7. Member Selection Rules

Following inline class stub behavior:

| Member Type | Selection Rule | Impl Class Behavior |
|-------------|----------------|---------------------|
| Abstract method | Always intercepted | Delegate to `_stub.Method.Invoke(...)` |
| Virtual method | Always intercepted | Delegate, fall back to `base.Method()` if unconfigured |
| Abstract property | Always intercepted | Delegate to `_stub.Prop.InvokeGet/Set(...)` |
| Virtual property | Always intercepted | Delegate, fall back to `base.Prop` if unconfigured |
| Abstract indexer | Always intercepted | Delegate to `_stub.Indexer.InvokeGet/Set(...)` |
| Virtual indexer | Always intercepted | Delegate, fall back to `base[key]` if unconfigured |
| Virtual event | Always intercepted | Delegate to `_stub.Event.RecordAdd/Remove(...)` |
| Non-virtual | Skipped | Inherited directly through chain |
| Sealed override | Skipped | Cannot override |
| `new` hiding | Skipped | Not a virtual member |

**Inherited Virtual Members**: Virtual members from the target's base classes are also eligible for interception. The generator examines the full inheritance chain (stopping at System.Object) to find all virtual/abstract members.

### 8. Constructor Forwarding

The generated Impl class forwards all accessible constructors from the target class. The wrapper's constructors create the nested Impl instance and pass `this` to it:

```csharp
// Target class
public class ServiceBase
{
    public ServiceBase() { }
    public ServiceBase(string name) { this.Name = name; }
    protected ServiceBase(int id) { }
}

// Generated wrapper partial (user's stub) with nested Impl
public partial class ServiceStub : global::KnockOff.IKnockOffStub
{
    public ServiceBase Object { get; }

    // Wrapper constructors - create nested Impl and assign to .Object
    public ServiceStub()
    {
        Object = new Impl(this);
    }

    public ServiceStub(string name)
    {
        Object = new Impl(this, name);
    }

    // Protected constructors from target are exposed as public on wrapper
    // (since Impl handles the actual protected constructor call)
    public ServiceStub(int id)
    {
        Object = new Impl(this, id);
    }

    // Nested Impl class - inherits from target
    private sealed class Impl : ServiceBase
    {
        private readonly ServiceStub _stub;

        public Impl(ServiceStub stub) : base()
        {
            _stub = stub;
        }

        public Impl(ServiceStub stub, string name) : base(name)
        {
            _stub = stub;
        }

        // Protected constructor is accessible from nested class
        public Impl(ServiceStub stub, int id) : base(id)
        {
            _stub = stub;
        }

        // ... overrides delegate to _stub
    }
}
```

Private constructors are not forwarded.

---

## Architectural Verification

**Nine Patterns Analysis:**

| Pattern | Impact | Notes |
|---------|--------|-------|
| 1. Standalone | None | Different attribute |
| 2. Generic Standalone | None | Different attribute |
| 3. Inline Interface | None | Different attribute |
| 4. Inline Class | **Reference** | Shares wrapper+nested Impl pattern exactly |
| 5. Inline Delegate | None | Different attribute |
| 6. Open Generic Interface | None | Different attribute |
| 7. Open Generic Class | **Reference** | Same wrapper+nested Impl pattern |
| 8. Standalone Class | **NEW** | `[KnockOffBase<T>]` - wrapper with nested Impl |
| 9. Generic Standalone Class | **NEW** | `[KnockOffBase(typeof(T<>))]` - generic wrapper with nested Impl |

**Breaking Changes:** None - new attributes, new patterns, no changes to existing patterns.

**Pattern Consistency:**
- Uses same interceptor classes and render logic as inline class stubs
- Follows wrapper+nested Impl separation pattern from inline class stubs EXACTLY
- `.Object` returns the nested Impl instance (same as inline class stubs)
- Clean interceptor names (no collision because interceptors on wrapper, overrides on nested Impl)
- User's class does NOT inherit from target (same as inline class stubs)

**Key Design Decision - Composition over Inheritance:**

The corrected architecture uses **composition** (wrapper + nested Impl) rather than **inheritance** (wrapper inherits from Impl). This is the only way to:
1. Avoid name collisions between interceptor properties and override properties
2. Provide clean interceptor API (`stub.Name.Get(...)`)
3. Be consistent with inline class stubs

**Trade-off:** The user's stub class is NOT a `ServiceBase`. Users must use `.Object` to get the actual `ServiceBase` instance. This matches inline class stub behavior exactly.

**Codebase Analysis:**

Files examined:
- `/src/Generator/Renderer/ClassRenderer.cs` - Inline class rendering, wrapper+nested Impl pattern
  - Line 79: Wrapper class declaration (no inheritance from target)
  - Line 97: `.Object` property returning the Impl instance
  - Lines 316-373: Nested Impl class generation (`private sealed class Impl : {cls.BaseType}`)
  - Line 333: `_stub` reference in Impl
- `/src/Generator/Renderer/FlatRenderer.cs` - Standalone interface pattern, base class generation
- `/src/Generator/Builder/ClassModelBuilder.cs` - How class stub models are built
- `/src/Generator/Model/Inline/InlineClassStubModel.cs` - Class stub model structure with Impl* models
- `/src/Generator/Model/Flat/FlatGenerationUnit.cs` - Standalone model structure
- `/src/KnockOff/KnockOffAttribute.cs` - Existing attribute definitions
- `/src/Generator/KnockOffGenerator.cs` - Pipeline setup, diagnostic definitions (KO20XX series)
- `/src/Design/Design.Stubs/StubPatterns/AllPatterns.cs` - Seven pattern documentation

**Key Insight from ClassRenderer.cs (lines 77-124):**
```csharp
// Wrapper class - holds interceptors with clean names
w.Line($"public class {cls.StubClassName} : IKnockOffStub");  // NOT inheriting from target!
// ...
w.Line($"public {interceptorType} {propertyName} {{ get; }} = new();");  // Clean names
w.Line($"public {cls.BaseType} Object {{ get; }}");  // Returns nested Impl

// Nested Impl - inherits from target, delegates to wrapper
w.Line($"private sealed class Impl : {cls.BaseType}");
w.Line($"private readonly {stubClassName} _stub;");
```

The standalone class stub generator must follow this same pattern.

---

## Developer Review

**Status:** Approved
**Reviewed:** 2026-02-04

### Review Summary

**Files Examined:**
- `src/Generator/Renderer/ClassRenderer.cs` - Confirmed wrapper + nested Impl pattern (lines 79, 97, 316-373)
- `src/Generator/Builder/ClassModelBuilder.cs` - Model building for inline class stubs
- `src/KnockOff/KnockOffAttribute.cs` - Existing attribute patterns
- `src/Design/Design.Stubs/StubPatterns/AllPatterns.cs` - Seven-pattern documentation
- `src/Tests/KnockOffTests/Generated/.../ClassStubTestClass.Stubs.g.cs` - Verified generated code structure

**Verification Results:**
- Generated code examples in plan match actual ClassRenderer.cs output
- Composition pattern (wrapper + nested Impl) is proven in production
- Name collision is definitively resolved by separating interceptors (on wrapper) from overrides (on nested Impl)

### All Concerns Resolved

1. **Name Collision** - RESOLVED via composition pattern. Verified against ClassStubTestClass.Stubs.g.cs:
   - Wrapper: `public class SimpleService : global::KnockOff.IKnockOffStub`
   - Nested Impl: `private sealed class Impl : global::KnockOff.Tests.SimpleService`
   - No inheritance between wrapper and Impl = no collision

2. **Private Field Access** - RESOLVED: `_stub` is a private field in nested Impl (line 333 of ClassRenderer.cs)

3. **Diagnostic Code Numbering** - RESOLVED: KO2007-KO2008 continue existing series

4. **Inherited Virtual Members** - RESOLVED: Section 7 documents inclusion

### What Looks Good

- Composition pattern matches inline class stubs exactly (ClassRenderer.cs)
- Generated code examples compile correctly (verified against actual .g.cs files)
- Clear implementation phases with checkpoints
- Reuses existing components (UnifiedInterceptorBuilder, shared renderers)
- Testable acceptance criteria
- No breaking changes to existing patterns

### Trade-off Acknowledged

Users must use `.Object` to get the actual `ServiceBase` instance. The user's stub class does NOT inherit from `ServiceBase`. This is the same behavior as inline class stubs (Pattern 3/7).

---

## Implementation Contract

**Created:** 2026-02-04
**Approved by:** knockoff-developer

### In Scope

**Phase 1: Foundation**
- [ ] Create `src/KnockOff/KnockOffBaseAttribute.cs` with:
  - `KnockOffBaseAttribute<T>` (generic, for closed types)
  - `KnockOffBaseAttribute` (non-generic, for open generic via typeof)
- [ ] Add predicate `IsStandaloneClassCandidate` to `KnockOffGenerator.cs`
- [ ] Add diagnostic descriptors KO2007 (type parameter mismatch) and KO2008 (non-partial class)
- [ ] **Checkpoint:** Attributes compile, can be applied to partial classes

**Phase 2: Pipeline & Transform**
- [ ] Create `StandaloneClassStubInfo` record in `src/Generator/Transform/`
- [ ] Add Pipeline 4 to `KnockOffGenerator.cs` for `[KnockOffBase<T>]`
- [ ] Add Pipeline 5 to `KnockOffGenerator.cs` for `[KnockOffBase(typeof(T<>))]`
- [ ] Implement `TransformStandaloneClassStub` using existing class analysis helpers
- [ ] **Checkpoint:** Transform produces valid StandaloneClassStubInfo (debug/log)

**Phase 3: Model**
- [ ] Create `src/Generator/Model/StandaloneClass/StandaloneClassGenerationUnit.cs`
- [ ] Create `src/Generator/Builder/StandaloneClassModelBuilder.cs` (adapt from ClassModelBuilder)
- [ ] Key differences from inline: no Stubs container, user's class is the wrapper
- [ ] **Checkpoint:** Model builder produces valid models for test cases

**Phase 4: Renderer**
- [ ] Create `src/Generator/Renderer/StandaloneClassRenderer.cs`
- [ ] Render interceptor classes (reuse existing shared renderers)
- [ ] Render wrapper partial class completion (interceptor properties, `.Object`, constructors)
- [ ] Render nested `private sealed class Impl : TargetClass` (reuse ClassRenderer logic)
- [ ] **Checkpoint:** Generated code compiles for basic test case

**Phase 5: Testing**
- [ ] Add `StandaloneClassStubTests.cs` with:
  - Basic virtual method test
  - Abstract method test
  - Property (get/set, get-only, init-only) tests
  - Indexer tests
  - Event tests
  - Constructor forwarding tests (parameterless, with params, protected)
- [ ] Add `GenericStandaloneClassStubTests.cs` for Pattern 9
- [ ] Add diagnostic tests (KO2007, KO2008, existing KO2001 for sealed)
- [ ] Verify `.Object` returns correct type
- [ ] **Checkpoint:** All tests pass, no regressions

**Phase 6: Documentation**
- [ ] Update `src/Design/Design.Stubs/StubPatterns/AllPatterns.cs` with Patterns 8 and 9
- [ ] **Checkpoint:** Design samples compile

### Explicitly Out of Scope

- **Source() delegation for class stubs** - Not supported for inline class stubs, not added here
- **Changes to existing patterns 1-7** - No modifications
- **User guide updates** - Separate documentation task
- **Performance optimizations** - Use existing patterns, optimize later if needed

### Verification Gates

1. **After Phase 1:** Attributes exist, diagnostics registered, project compiles
2. **After Phase 2:** Pipeline produces StandaloneClassStubInfo for `[KnockOffBase<T>]` usage
3. **After Phase 3:** Model builder correctly builds StandaloneClassGenerationUnit
4. **After Phase 4:** Generated code for basic case compiles without errors
5. **After Phase 5:** All tests pass including edge cases
6. **Final:** All existing tests pass (no regressions), generated code compiles

### Stop Conditions

If any of these occur, STOP and report:
- Out-of-scope test fails (existing inline class tests, standalone interface tests)
- Architectural contradiction discovered (e.g., reuse of ClassRenderer not possible)
- Generated code does not compile for a documented scenario
- Name collision reappears in any form

**Key Implementation Notes:**
- Reuse ClassRenderer logic for nested Impl class generation
- The wrapper partial completes the user's partial class
- Nested Impl class is `private sealed class Impl : TargetClass`
- Constructors create `new Impl(this, args)` and assign to `.Object`

**Out of Scope:**
- Source() delegation (matches inline class behavior)
- Changes to existing inline class or standalone interface patterns

---

## Implementation Steps

### Phase 1: Foundation

1. Add `KnockOffBaseAttribute<T>` and `KnockOffBaseAttribute` to `src/KnockOff/`
2. Add predicates to `KnockOffGenerator.cs`
3. Add diagnostic descriptors for KO2007-KO2008
4. Create `StandaloneClassStubInfo` record in Transform.cs

### Phase 2: Model Layer

5. Create `StandaloneClassGenerationUnit` model (similar to InlineClassStubModel)
6. Create `StandaloneClassModelBuilder` (based on ClassModelBuilder)
7. Adapt model for wrapper + nested Impl pattern (matching inline class stubs)

### Phase 3: Renderer

8. Create `StandaloneClassRenderer`:
   - Generate interceptor classes (reuse shared renderers)
   - Generate wrapper partial class (interceptor properties, `.Object`, constructors)
   - Generate nested Impl class (inherits target, overrides delegate to wrapper)
   - **Key**: Reuse ClassRenderer's `RenderImplClass` logic for the nested Impl

### Phase 4: Testing

9. Add basic tests (virtual methods, abstract methods)
10. Add property and indexer tests
11. Add event tests
12. Add constructor forwarding tests
13. Add generic standalone class tests (Pattern 9)
14. Add diagnostic tests (sealed class, no constructors, etc.)
15. Verify `.Object` returns correct type (not `this`)

### Phase 5: Documentation

15. Update `Design.Stubs/StubPatterns/AllPatterns.cs` with Pattern 8 and 9
16. Update API reference documentation
17. Add examples to getting-started guide

---

## Acceptance Criteria

- [ ] `[KnockOffBase<ServiceBase>]` generates a working standalone class stub
- [ ] `[KnockOffBase(typeof(RepositoryBase<>))]` generates a working generic standalone stub
- [ ] No C# compilation errors from name collisions (interceptor vs override)
- [ ] Abstract members use strict/default behavior
- [ ] Virtual members call base when not configured
- [ ] All accessible constructors are forwarded (creating nested Impl instance)
- [ ] `.Object` property returns the nested Impl instance as `TargetClass`
- [ ] `Strict` property controls strict mode
- [ ] `ResetInterceptors()` clears all interceptor state
- [ ] `Verify()` and `VerifyAll()` work correctly
- [ ] Users can add interfaces to their stub class
- [ ] Users can add custom methods and fields
- [ ] Diagnostic KO2001 emitted for sealed target class
- [ ] Diagnostic KO2007 emitted for type parameter mismatch
- [ ] Diagnostic KO2008 emitted for non-partial user class
- [ ] All existing tests pass (no regressions)
- [ ] Pattern 8 and 9 documented in AllPatterns.cs

---

## Dependencies

- Existing `ClassRenderer.cs` patterns for override generation (Impl class logic)
- Existing `UnifiedInterceptorBuilder` for method interceptors
- Existing shared renderers (PropertyInterceptorRenderer, IndexerInterceptorRenderer, etc.)

---

## Risks / Considerations

1. **Base Constructor Timing**: The nested Impl constructor runs before interceptors are available. However, since `_stub` is passed in the constructor (not via `SetStub()`), this is simpler than the original design. The `_stub` reference is valid immediately after construction.

2. **Required Members**: C# 11 `required` members need special handling - use `[SetsRequiredMembers]` on Impl constructors and initialize required members.

3. **Protected Constructors**: Protected constructors from target are forwarded as public constructors on the user's stub class (since the nested Impl handles the actual construction).

4. **Accessibility Matching**: Nested Impl class is always `private sealed` - this is correct since it's an implementation detail.

5. **Multiple Inheritance Levels**: If target has deep inheritance (A <- B <- C), all virtual members up the chain must be found and intercepted in the nested Impl.

6. **Sealed Override in Target's Base**: If target's base class has a sealed override, we cannot intercept it (handled by existing KO2003 diagnostic).

7. **User's Stub is NOT a ServiceBase**: Unlike the original design, the user's stub class does NOT inherit from the target. Users must use `.Object` to get the actual `ServiceBase` instance. This is the same trade-off as inline class stubs.

---

## Implementation Progress

**Status:** Phases 1-5 Complete

**Phase 1:** Foundation
- [x] `KnockOffBaseAttribute.cs` created
- [x] `IsStandaloneClassCandidate` predicate added
- [x] Diagnostics KO2007-KO2008 registered
- [x] **Verification**: Attributes compile, can be applied

**Phase 2:** Pipeline & Transform
- [x] `StandaloneClassStubInfo` record created
- [x] Pipeline 4 added for `[KnockOffBase<T>]`
- [x] Pipeline 5 added for `[KnockOffBase(typeof(T<>))]`
- [x] `TransformStandaloneClassStub` implemented
- [x] **Verification**: Transform produces valid info

**Phase 3:** Model Layer
- [x] `StandaloneClassGenerationUnit` model created
- [x] `StandaloneClassModelBuilder` created
- [x] **Verification**: Model builder produces valid models

**Phase 4:** Renderer
- [x] `StandaloneClassRenderer` created
- [x] Interceptor classes rendered
- [x] Wrapper partial class completion rendered
- [x] Nested Impl class rendered
- [x] **Verification**: Generated code compiles

**Phase 5:** Testing
- [x] Basic method tests pass
- [x] Property tests pass
- [x] Indexer tests pass
- [x] Event tests pass
- [x] Constructor forwarding tests pass
- [x] Generic standalone tests pass (Pattern 9)
- [x] No regressions in existing tests
- [x] **Verification**: 100% test pass rate

**Phase 6:** Documentation
- [ ] AllPatterns.cs updated with Patterns 8 and 9
- [ ] **Verification**: Design samples compile

---

## Completion Evidence (Phases 1-5)

**Completed:** 2026-02-04

### Test Results

All tests pass across all target frameworks:
- net8.0: 1084 passed
- net9.0: 1085 passed
- net10.0: 1085 passed

New tests added:
- `StandaloneClassStubTests.cs`: 27 tests covering basic existence, constructor forwarding, virtual/abstract properties and methods, strict mode, reset, and custom user methods
- `GenericStandaloneClassStubTests.cs`: 17 tests covering Pattern 9 (open generic standalone class stubs)
- Additional tests for events, indexers, and abstract classes

### Files Created

1. **Attribute:** `/src/KnockOff/KnockOffBaseAttribute.cs`
   - `KnockOffBaseAttribute<T>` for closed types
   - `KnockOffBaseAttribute` for open generics via typeof()

2. **Generator:**
   - `/src/Generator/KnockOffGenerator.StandaloneClass.cs` - Transform and generation logic
   - Added Pipelines 4 and 5 in `/src/Generator/KnockOffGenerator.cs`
   - Added diagnostics KO2007 (type parameter mismatch) and KO2008 (non-partial class)

3. **Model:**
   - `/src/Generator/Model/StandaloneClass/StandaloneClassGenerationUnit.cs`

4. **Builder:**
   - `/src/Generator/Builder/StandaloneClassModelBuilder.cs`

5. **Renderer:**
   - `/src/Generator/Renderer/StandaloneClassRenderer.cs`

6. **Tests:**
   - `/src/Tests/KnockOffTests/StandaloneClassStubTests.cs`
   - `/src/Tests/KnockOffTests/GenericStandaloneClassStubTests.cs`

### Generated Code Sample

```csharp
// From ServiceBaseStub.g.cs (simplified)

partial class ServiceBaseStub : global::KnockOff.IKnockOffStub
{
    public bool Strict { get; set; }
    public ServiceBaseStub_NameInterceptor Name { get; } = new();
    public ServiceBaseStub_ExecuteInterceptor Execute { get; } = new();
    public ServiceBaseStub_InitializeInterceptor Initialize { get; } = new();

    public global::KnockOff.Tests.ServiceBase Object { get; }

    public ServiceBaseStub()
    {
        Object = new Impl(this);
    }

    private sealed class Impl : global::KnockOff.Tests.ServiceBase
    {
        private readonly ServiceBaseStub _stub;

        public Impl(ServiceBaseStub stub) : base()
        {
            _stub = stub;
        }

        public override string Name
        {
            get => _stub.Name.IsConfigured
                ? _stub.Name.InvokeGet(_stub.Strict)
                : base.Name;
            set { ... }
        }

        public override int Execute(string command)
        {
            return _stub.Execute.Invoke(_stub.Strict, command);
        }
    }
}
```

### Remaining Work

Phase 6 (Documentation) is tracked in a separate todo and will update `AllPatterns.cs` with Patterns 8 and 9.
