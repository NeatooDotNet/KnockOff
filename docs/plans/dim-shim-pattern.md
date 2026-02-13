# DIM Shim Pattern: Default Interface Method Support

**Date:** 2026-02-10
**Related Todo:** [Fix Gap #12: Default Interface Methods (DIMs) Not Executed](../todos/default-interface-methods.md)
**Status:** Complete
**Last Updated:** 2026-02-10

---

## Overview

KnockOff returns `default(T)` for unconfigured interface members even when the interface provides a Default Interface Method (DIM) implementation. The generator collects ALL interface members (both abstract and DIM) and generates explicit implementations + interceptors for all of them. The explicit implementations override the DIMs, so the C# runtime never invokes them.

**Goal:** Make DIMs configurable via interceptors with the DIM as fallback when unconfigured -- matching how class stubs treat virtual methods with `base.Method()`.

---

## Approach: Shim Pattern

### Why a Shim?

The core problem: the stub class provides explicit interface implementations for ALL members, including DIMs. This overrides the DIM. We need a way to invoke the DIM when a DIM interceptor is unconfigured.

We cannot simply use `_source = this` on DIM interceptors because when the interceptor calls `_source.GetPerimeter()`, it calls the stub's explicit interface implementation, which routes through the interceptor again -- infinite recursion.

A **shim class** breaks the recursion. The shim:
1. Implements the interface
2. Provides explicit implementations for **abstract members only** -- these delegate back to the stub's interceptors
3. Does **NOT** implement DIM members -- the C# runtime invokes the DIM naturally
4. When the DIM body calls `this.SideLength`, `this` is the shim (cast to the interface), and the call goes through the shim's explicit implementation, which delegates to the stub's interceptor for `SideLength`

### How It Works

For `IDefaultMethodPolygon`:
```csharp
public interface IDefaultMethodPolygon
{
    int NumberOfSides { get; }           // abstract
    int SideLength { get; }             // abstract
    double GetPerimeter() => this.SideLength * this.NumberOfSides;  // DIM
}
```

The generator produces a private shim class:
```csharp
private sealed class __DimShim : IDefaultMethodPolygon
{
    private readonly IDefaultMethodPolygon _stub;
    internal __DimShim(IDefaultMethodPolygon stub) { _stub = stub; }

    // Abstract members: delegate to stub (which routes through interceptors)
    int IDefaultMethodPolygon.NumberOfSides => _stub.NumberOfSides;
    int IDefaultMethodPolygon.SideLength => _stub.SideLength;

    // DIM member (GetPerimeter): NOT implemented here.
    // Runtime invokes the DIM: this.SideLength * this.NumberOfSides
    // "this" is the shim, so this.SideLength calls the explicit impl above,
    // which calls _stub.SideLength, which goes through the interceptor.
}
```

In the constructor:
```csharp
public IDefaultMethodPolygonStub(bool strict = false)
{
    Strict = strict;
    // Auto-wire DIM interceptors to use shim as source
    var __shim = new __DimShim(this);
    GetPerimeter._source = __shim;
}
```

When `GetPerimeter` is unconfigured, the interceptor's `_source` check fires:
```csharp
if (_source is { } src) return src.GetPerimeter();
```
This calls `__shim.GetPerimeter()`, and since the shim doesn't implement `GetPerimeter`, the C# runtime calls the DIM, which computes `this.SideLength * this.NumberOfSides` using the shim's explicit implementations that delegate back to the stub's interceptors.

When `GetPerimeter` IS configured (e.g., `stub.GetPerimeter.Return(42)`), the interceptor returns the configured value without reaching `_source`.

### DIM-to-DIM Calls Work Naturally

If a DIM calls another DIM, the called DIM also executes naturally on the shim because the shim doesn't implement either DIM. Both DIMs run through the C# runtime. If the user configures one DIM's interceptor, DIM-to-DIM calls from other DIMs will NOT see the configured value -- they bypass the interceptor entirely because `this` in the DIM body is the shim, not the stub.

This is the correct behavior: a DIM body is fixed code. The user can override what the interceptor returns when called through the stub's interface, but the DIM's own body always runs as written.

### Interfaces Without DIMs

If an interface has no DIM members (all members are abstract), no shim is generated. This is the common case and introduces zero overhead for existing stubs.

---

## Affected Patterns and Pipelines

| Patterns | Pipeline | Transform | Builder | Renderer |
|---|---|---|---|---|
| Standalone (1) | Flat | `ExtractInterfaceInfo` | `FlatModelBuilder` | `FlatRenderer` |
| Generic Standalone (2) | Flat | `ExtractInterfaceInfo` | `FlatModelBuilder` | `FlatRenderer` |
| Inline interface (5) | Inline | `ExtractInterfaceInfo` | `InlineModelBuilder` | `InlineRenderer` |
| Open generic interface (8) | Inline | `ExtractInterfaceInfo` | `InlineModelBuilder` | `InlineRenderer` |

**Not affected:** Class stubs (3,4,6,9) already use `base.Method()` for virtual methods. Inline delegate stubs (7) also not affected.

---

## Design

### Phase 1: Model Changes

#### 1a. Add `IsAbstract` to `InterfaceMemberInfo`

**File:** `src/Generator/Models/InterfaceModels.cs`

Add `IsAbstract` property to the `InterfaceMemberInfo` record, after `ReturnsByRefReadonly`:
```csharp
/// <summary>
/// True if the member is abstract (has no default implementation).
/// False for Default Interface Method (DIM) members.
/// </summary>
bool IsAbstract = true
```

Default is `true` for backward compatibility -- all existing code assumes members are abstract.

Update `FromProperty` factory method (line ~169-190) to pass:
```csharp
IsAbstract: property.IsAbstract
```

Update `FromMethod` factory method (line ~256-272) to pass:
```csharp
IsAbstract: method.IsAbstract
```

Roslyn's `IPropertySymbol.IsAbstract` and `IMethodSymbol.IsAbstract` are exactly what we need:
- `true` for abstract members (no body)
- `false` for DIM members (have a body)

#### 1b. Add `IsAbstract` to `EventMemberInfo`

**File:** `src/Generator/Models/EventModels.cs`

Add `IsAbstract` to the record, after `AccessModifier`:
```csharp
/// <summary>
/// True if the event is abstract. False for events with default add/remove handlers (DIM events).
/// </summary>
bool IsAbstract = true
```

Update `FromEvent` factory method to pass:
```csharp
IsAbstract: eventSymbol.IsAbstract
```

Note: DIM events are rare but the model should be complete.

#### 1c. Add `HasDimMembers` computed property to `InterfaceInfo`

**File:** `src/Generator/Models/InterfaceModels.cs`

Add a computed property to the `InterfaceInfo` record body:
```csharp
/// <summary>True if this interface has any non-abstract (DIM) members.</summary>
public bool HasDimMembers => Members.Any(m => !m.IsAbstract) || Events.Any(e => !e.IsAbstract);
```

#### 1d. Add `IsAbstract` to `InlineInterfaceImplementation`

**File:** `src/Generator/Model/Inline/InlineInterfaceImplementation.cs`

Add a field after `NeedsNullableDisable`:
```csharp
/// <summary>True if the member is abstract. False for DIM members.</summary>
bool IsAbstract = true
```

#### 1e. Add shim-related fields to `InlineInterfaceStubModel`

**File:** `src/Generator/Model/Inline/InlineInterfaceStubModel.cs`

Add after `SourceProviders`:
```csharp
/// <summary>True if a DIM shim class should be generated.</summary>
bool HasDimShim = false,
/// <summary>Explicit implementations for the DIM shim class (abstract members only). Empty if no shim needed.</summary>
EquatableArray<InlineInterfaceImplementation> ShimImplementations = default
```

#### 1f. Add shim-related fields to `FlatGenerationUnit`

**File:** `src/Generator/Model/Flat/FlatGenerationUnit.cs`

Add after `Strict`:
```csharp
/// <summary>True if any interface has DIM members requiring a shim class.</summary>
bool HasDimShim = false,
/// <summary>DIM member interceptor names that need _source wired to the shim in the constructor.</summary>
EquatableArray<string> DimInterceptorNames = default,
/// <summary>Shim data for each interface that has DIM members. Contains abstract-only member info for shim generation.</summary>
EquatableArray<FlatDimShimInfo> DimShimInfos = default
```

#### 1g. New model: `FlatDimShimInfo`

**File:** `src/Generator/Model/Flat/FlatDimShimInfo.cs` (new file)

The flat pipeline doesn't use `InlineInterfaceImplementation`. It needs its own shim data model:
```csharp
internal sealed record FlatDimShimInfo(
    /// <summary>The interface this shim implements.</summary>
    string InterfaceFullName,
    /// <summary>Abstract property members to delegate to _stub.</summary>
    EquatableArray<FlatDimShimPropertyMember> Properties,
    /// <summary>Abstract indexer members to delegate to _stub.</summary>
    EquatableArray<FlatDimShimIndexerMember> Indexers,
    /// <summary>Abstract method members to delegate to _stub.</summary>
    EquatableArray<FlatDimShimMethodMember> Methods,
    /// <summary>Abstract event members to delegate to _stub.</summary>
    EquatableArray<FlatDimShimEventMember> Events);

internal sealed record FlatDimShimPropertyMember(
    string InterfaceFullName,
    string Name,
    string ReturnType,
    bool HasGetter,
    bool HasSetter,
    bool IsInitOnly,
    bool ReturnsByRef,
    bool ReturnsByRefReadonly);

internal sealed record FlatDimShimIndexerMember(
    string InterfaceFullName,
    string ReturnType,
    string ParameterDeclarations,
    string ArgumentList,
    bool HasGetter,
    bool HasSetter,
    bool IsInitOnly,
    bool ReturnsByRef,
    bool ReturnsByRefReadonly);

internal sealed record FlatDimShimMethodMember(
    string InterfaceFullName,
    string Name,
    string ReturnType,
    bool IsVoid,
    string ParameterDeclarations,
    string ArgumentList,
    bool IsGenericMethod,
    string TypeParameterDecl,
    string ConstraintClauses);

internal sealed record FlatDimShimEventMember(
    string InterfaceFullName,
    string Name,
    string DelegateType);
```

These models carry just enough information for the FlatRenderer to emit shim delegation code. They parallel the inline pipeline's `InlineInterfaceImplementation` but are simpler since the shim only needs delegation signatures.

### Phase 2: Transform Changes

#### 2a. Propagate `IsAbstract` from Roslyn symbols

**File:** `src/Generator/Models/InterfaceModels.cs`

The `InterfaceMemberInfo.FromProperty` and `InterfaceMemberInfo.FromMethod` factory methods receive the Roslyn symbols directly. Adding the `IsAbstract` parameter to the constructor call in each factory method is sufficient. No changes needed in `KnockOffGenerator.Transform.cs` because the transform calls these factory methods, and the new field flows automatically.

The `EventMemberInfo.FromEvent` factory method similarly needs to pass `eventSymbol.IsAbstract`.

**No other Transform changes needed.** The `ExtractInterfaceInfo` method (line 306-408 in `KnockOffGenerator.Transform.cs`) already collects all members from both the primary interface and its base interfaces. The `IsAbstract` flag flows through the existing member collection.

### Phase 3: Builder Changes

#### 3a. InlineModelBuilder changes

**File:** `src/Generator/Builder/InlineModelBuilder.cs`

In `BuildInterfaceStub` method (starts at line 85):

1. After building implementations (line 209), compute whether this interface has DIM members:
```csharp
var hasDimMembers = iface.HasDimMembers;
```

2. Build shim implementations (abstract-only members for the shim class):
```csharp
var shimImplementations = hasDimMembers
    ? implementations.Where(i => i.IsAbstract && i.Kind != InlineMemberKind.Event)
        .Concat(implementations.Where(i => i.IsAbstract && i.Kind == InlineMemberKind.Event))
        .ToEquatableArray()
    : EquatableArray<InlineInterfaceImplementation>.Empty;
```

The shim implementations are a SUBSET of the main implementations -- only the abstract ones. The existing `BuildImplementations` method produces `InlineInterfaceImplementation` objects. We filter to `IsAbstract == true` to get the shim list.

**The shim does NOT need its own Build methods.** The same `InlineInterfaceImplementation` records work for both the stub's explicit implementations and the shim's delegation implementations. The renderer just emits different code for the shim (simple `_stub.Member` delegation instead of interceptor routing).

3. Pass to model:
```csharp
return new InlineInterfaceStubModel(
    // ... existing ...
    SourceProviders: sourceProviders,
    HasDimShim: hasDimMembers,
    ShimImplementations: shimImplementations);
```

4. **Propagate `IsAbstract` through `InlineInterfaceImplementation`**: Each `Build*Implementation` method in InlineModelBuilder creates an `InlineInterfaceImplementation`. Add `IsAbstract` parameter sourcing from the `InterfaceMemberInfo`:
   - `BuildPropertyImplementation` (line 743): Add `IsAbstract: member.IsAbstract` (the `member` parameter is `InterfaceMemberInfo`; add `IsAbstract` to method signature)
   - `BuildIndexerImplementation` (line 796): Add `IsAbstract: member.IsAbstract`
   - `BuildNonGenericMethodImplementation` (line 877): Add `IsAbstract: member.IsAbstract`
   - `BuildGenericMethodImplementation` (line 967): Add `IsAbstract: member.IsAbstract`
   - `BuildMethodDelegationImplementation` (line 1046): Always `IsAbstract: true` (delegation targets are always for abstract members)
   - `BuildEventImplementation` (line 1111): Add `IsAbstract: evt.IsAbstract` (needs signature change to accept `EventMemberInfo` instead of just the extracted fields, OR pass `isAbstract` explicitly)

Note: The `BuildPropertyImplementation` method currently takes `InterfaceMemberInfo member` as a parameter (line 743), so `member.IsAbstract` is directly available. Same for the others.

#### 3b. FlatModelBuilder changes

**File:** `src/Generator/Builder/FlatModelBuilder.cs`

In the `Build` method (starts at line 23):

1. After building all models (line ~80), compute DIM information:
```csharp
var hasDimShim = typeInfo.Interfaces.Any(i => i.HasDimMembers);
```

2. Build DIM shim info from the InterfaceInfo data (not from the flat models, which have lost the abstract/DIM distinction):
```csharp
var dimShimInfos = new List<FlatDimShimInfo>();
var dimInterceptorNames = new List<string>();
if (hasDimShim)
{
    foreach (var iface in typeInfo.Interfaces.Where(i => i.HasDimMembers))
    {
        var shimProps = new List<FlatDimShimPropertyMember>();
        var shimIndexers = new List<FlatDimShimIndexerMember>();
        var shimMethods = new List<FlatDimShimMethodMember>();
        var shimEvents = new List<FlatDimShimEventMember>();

        foreach (var member in iface.Members.Where(m => m.IsAbstract))
        {
            // Build shim delegation info for abstract members
            if (member.IsProperty && !member.IsIndexer)
                shimProps.Add(/* build from member */);
            else if (member.IsIndexer)
                shimIndexers.Add(/* build from member */);
            else
                shimMethods.Add(/* build from member */);
        }
        foreach (var evt in iface.Events.Where(e => e.IsAbstract))
        {
            shimEvents.Add(/* build from evt */);
        }

        dimShimInfos.Add(new FlatDimShimInfo(
            iface.FullName, shimProps, shimIndexers, shimMethods, shimEvents));

        // Collect DIM interceptor names
        foreach (var member in iface.Members.Where(m => !m.IsAbstract))
        {
            var interceptorName = nameMap[GetMemberKey(member)];
            dimInterceptorNames.Add(interceptorName);
        }
    }
}
```

3. Pass to `FlatGenerationUnit`:
```csharp
HasDimShim: hasDimShim,
DimInterceptorNames: dimInterceptorNames.Distinct().ToEquatableArray(),
DimShimInfos: dimShimInfos.ToEquatableArray()
```

**Important:** The `FlatModelBuilder.GetMemberKey` is currently a private method (line ~not visible) that builds keys for the name map. The DIM interceptor name lookup must use whatever key format the name map uses. Examine the existing `BuildNameMap` implementation to confirm the key format.

### Phase 4: Renderer Changes

#### 4a. InlineRenderer: Generate shim class

**File:** `src/Generator/Renderer/InlineRenderer.cs`

In `RenderInterfaceStub` (line 102), after the existing class body and before the closing brace `w.Line("\t\t}");` (line 223):

```csharp
// DIM Shim class (if interface has DIM members)
if (iface.HasDimShim)
{
    w.Line();
    RenderDimShimClass(w, iface);
}
```

New method `RenderDimShimClass`:
```csharp
private static void RenderDimShimClass(CodeWriter w, InlineInterfaceStubModel iface)
{
    var typeParams = FormatTypeParameterList(iface.TypeParameters);
    var constraints = FormatConstraints(iface.TypeParameters);
    w.Line($"\t\t\tprivate sealed class __DimShim{typeParams} : {iface.BaseType}{constraints}");
    w.Line("\t\t\t{");
    w.Line($"\t\t\t\tprivate readonly {iface.BaseType} _stub;");
    w.Line($"\t\t\t\tinternal __DimShim({iface.BaseType} stub) {{ _stub = stub; }}");

    // Render explicit implementations for ABSTRACT members only
    foreach (var impl in iface.ShimImplementations)
    {
        RenderShimImplementation(w, impl);
    }

    w.Line("\t\t\t}");
}
```

New method `RenderShimImplementation` generates simple delegation to `_stub`:
```csharp
private static void RenderShimImplementation(CodeWriter w, InlineInterfaceImplementation impl)
{
    switch (impl.Kind)
    {
        case InlineMemberKind.Property:
            // Property: delegate getter/setter to _stub
            w.Line($"\t\t\t\t{impl.RefReturnPrefix}{impl.ReturnType} {impl.InterfaceFullName}.{impl.MemberName}");
            w.Line("\t\t\t\t{");
            if (impl.HasGetter)
                w.Line($"\t\t\t\t\tget => _stub.{impl.MemberName};");
            if (impl.HasSetter)
            {
                var setterKeyword = impl.IsInitOnly ? "init" : "set";
                w.Line($"\t\t\t\t\t{setterKeyword} => _stub.{impl.MemberName} = value;");
            }
            w.Line("\t\t\t\t}");
            break;

        case InlineMemberKind.Indexer:
            // Indexer: delegate to _stub[args]
            w.Line($"\t\t\t\t{impl.RefReturnPrefix}{impl.ReturnType} {impl.InterfaceFullName}.this[{impl.ParameterDeclarations}]");
            w.Line("\t\t\t\t{");
            if (impl.HasGetter)
                w.Line($"\t\t\t\t\tget => _stub[{impl.ArgumentList}];");
            if (impl.HasSetter)
            {
                var setterKeyword = impl.IsInitOnly ? "init" : "set";
                w.Line($"\t\t\t\t\t{setterKeyword} {{ _stub[{impl.ArgumentList}] = value; }}");
            }
            w.Line("\t\t\t\t}");
            break;

        case InlineMemberKind.Method:
            // Method: delegate to _stub.Method(args)
            if (impl.IsGenericMethod)
            {
                // Generic methods: delegate with type parameters
                var returnKw = impl.IsVoid ? "" : "return ";
                w.Line($"\t\t\t\t{impl.ReturnType} {impl.InterfaceFullName}.{impl.MemberName}{impl.TypeParameterDecl}({impl.ParameterDeclarations}){impl.ConstraintClauses}");
                w.Line($"\t\t\t\t\t=> {returnKw}_stub.{impl.MemberName}{impl.TypeParameterDecl}({impl.ArgumentList});");
            }
            else if (impl.IsVoid)
            {
                w.Line($"\t\t\t\tvoid {impl.InterfaceFullName}.{impl.MemberName}({impl.ParameterDeclarations})");
                w.Line($"\t\t\t\t\t=> _stub.{impl.MemberName}({impl.ArgumentList});");
            }
            else
            {
                w.Line($"\t\t\t\t{impl.RefReturnPrefix}{impl.ReturnType} {impl.InterfaceFullName}.{impl.MemberName}({impl.ParameterDeclarations})");
                w.Line($"\t\t\t\t\t=> _stub.{impl.MemberName}({impl.ArgumentList});");
            }
            break;

        case InlineMemberKind.Event:
            // Event: delegate add/remove to _stub
            w.Line($"\t\t\t\tevent {impl.ReturnType}? {impl.InterfaceFullName}.{impl.MemberName}");
            w.Line("\t\t\t\t{");
            w.Line($"\t\t\t\t\tadd => (({impl.InterfaceFullName})_stub).{impl.MemberName} += value;");
            w.Line($"\t\t\t\t\tremove => (({impl.InterfaceFullName})_stub).{impl.MemberName} -= value;");
            w.Line("\t\t\t\t}");
            break;
    }
    w.Line();
}
```

**Key detail:** `_stub` is typed as the interface (`IDefaultMethodPolygon`), so `_stub.SideLength` calls the stub's explicit interface implementation, which goes through the interceptor. No recursion because the shim itself does NOT implement DIM members.

#### 4b. InlineRenderer: Wire shim in constructor

In `RenderInterfaceStub`, the constructor is rendered at lines 205-208. After `w.Line("\t\t\t\tStrict = strict;");` (line 207), add:

```csharp
if (iface.HasDimShim)
{
    var typeParams = FormatTypeParameterList(iface.TypeParameters);
    w.Line($"\t\t\t\tvar __shim = new __DimShim{typeParams}(this);");
    var emitted = new HashSet<string>();
    foreach (var impl in iface.Implementations)
    {
        if (!impl.IsAbstract && emitted.Add(impl.InterceptorName) && !string.IsNullOrEmpty(impl.InterceptorName))
        {
            w.Line($"\t\t\t\t{impl.InterceptorName}._source = __shim;");
        }
    }
}
```

The HashSet deduplicates -- if multiple DIM members share an interceptor (e.g., overloaded methods), `_source` is only set once.

#### 4c. FlatRenderer: Add constructor and generate shim class

**File:** `src/Generator/Renderer/FlatRenderer.cs`

**Current state:** Flat stubs have NO constructor. They use property initializers (`= new()`, `= false`). We need to add a constructor for DIM shim wiring.

1. **Conditionally render a constructor** in `RenderStandardMembers` (line 1505), after the Strict property:

```csharp
// Constructor (only when DIM shim needs wiring)
if (unit.HasDimShim)
{
    var typeParams = FormatTypeParameters(unit.TypeParameters);
    w.Line($"public {unit.ClassName}()");
    w.Line("{");
    w.Line($"\tvar __shim = new __DimShim{typeParams}(this);");
    foreach (var name in unit.DimInterceptorNames)
    {
        w.Line($"\t{name}._source = __shim;");
    }
    w.Line("}");
    w.Line();
}
```

Note: This constructor has no `strict` parameter because the flat pattern uses a `Strict` property setter. The constructor only wires up the shim.

2. **Render the shim class** inside the partial class body. Add before the closing brace (line 207):

```csharp
if (unit.HasDimShim)
{
    RenderFlatDimShimClass(w, unit);
}
```

New method `RenderFlatDimShimClass` generates a shim that implements all interfaces with DIM members:

```csharp
private static void RenderFlatDimShimClass(CodeWriter w, FlatGenerationUnit unit)
{
    var typeParams = FormatTypeParameters(unit.TypeParameters);
    var constraints = FormatConstraints(unit.TypeParameters);
    var interfaces = string.Join(", ", unit.DimShimInfos.Select(s => s.InterfaceFullName));
    w.Line($"private sealed class __DimShim{typeParams} : {interfaces}{constraints}");
    using (w.Braces())
    {
        var primaryInterface = unit.InterfaceList.GetArray()![0];
        w.Line($"private readonly {primaryInterface} _stub;");
        w.Line($"internal __DimShim({primaryInterface} stub) => _stub = stub;");
        w.Line();

        // Render delegation implementations for abstract members only
        foreach (var shimInfo in unit.DimShimInfos)
        {
            foreach (var prop in shimInfo.Properties)
                RenderShimPropertyDelegation(w, prop);
            foreach (var indexer in shimInfo.Indexers)
                RenderShimIndexerDelegation(w, indexer);
            foreach (var method in shimInfo.Methods)
                RenderShimMethodDelegation(w, method);
            foreach (var evt in shimInfo.Events)
                RenderShimEventDelegation(w, evt);
        }
    }
    w.Line();
}
```

The delegation methods generate simple forwarding to `_stub`:
- Properties: `int IFoo.Name => _stub.Name;`
- Indexers: `double IFoo.this[int i] => _stub[i];`
- Methods: `void IFoo.DoSomething(string arg) => _stub.DoSomething(arg);`
- Events: `event Handler IFoo.Changed { add => ((IFoo)_stub).Changed += value; remove => ((IFoo)_stub).Changed -= value; }`

### Phase 5: Edge Cases

#### 5a. DIM members from base interfaces

If `IChild : IParent` where `IParent` has a DIM, the shim must correctly handle the inherited DIM. The `ExtractInterfaceInfo` already collects members from all base interfaces. The `IsAbstract` flag correctly propagates.

#### 5b. Open generic interfaces

For `[KnockOff(typeof(IService<>))]`, the shim class must also be generic:
```csharp
private sealed class __DimShim<T> : IService<T>
{
    private readonly IService<T> _stub;
    // ...
}
```

The type parameters and constraints must be forwarded. Both InlineRenderer and FlatRenderer already have helper methods for type parameter formatting (`FormatTypeParameterList`, `FormatConstraints`).

#### 5c. Interfaces with only DIM members (no abstract members)

If an interface has ONLY DIM members and no abstract members, the shim is still needed but has no explicit implementations (it only implements the interface declaration). All DIMs execute naturally on the shim.

#### 5d. Overloaded methods where some are abstract and some are DIM

Each overload is treated independently. Abstract overloads get shim implementations, DIM overloads do not.

#### 5e. Source(T) interaction

When a user calls `stub.Source(someImplementation)`, this sets `_source` on ALL interceptors (including DIM interceptors). This should take priority over the shim. The existing `_source` infrastructure already handles this -- `Source(T)` overwrites whatever `_source` was set to (including the shim). If `Source(null)` is called, `_source` is cleared, and the DIM interceptor falls through to strict/default instead of the DIM.

**Decision:** The shim is the initial default only. `Source(null)` clears `_source` entirely. If the user explicitly clears the source, they're saying "I don't want any delegation." This is consistent with how `Source()` works for class stubs. If needed in the future, we could add a `Reset()` improvement that restores the shim.

#### 5f. Events with DIMs

Event DIMs (default add/remove handlers) are rare but possible. The `IsAbstract` on `EventMemberInfo` handles this. The shim class generates explicit event implementations for abstract events only.

#### 5g. Strict mode interaction

The interceptor priority chain is:
1. Configured value (Return/Call/etc.)
2. Source delegation (`_source`)
3. Strict check (throw)
4. Default value

Since the shim is wired as `_source`, unconfigured DIM calls will hit priority 2 (source) and invoke the DIM. This is correct -- the DIM IS a default implementation, analogous to `base.Method()` in class stubs.

If the user wants strict behavior for a DIM (throw instead of running the DIM), they must set the interceptor's `_source = null` explicitly. This is an edge case we can document but not optimize for.

#### 5h. Flat pipeline constructor considerations

The flat pipeline currently has NO constructor. Adding one is safe because:
- Users write `[KnockOff] public partial class Stub : IFoo { }` -- they don't write constructors on the stub class
- The generated constructor is parameterless and only wires `_source` on DIM interceptors
- If the interface has no DIMs, no constructor is generated (zero regression risk)

#### 5i. Ref return properties/methods in shim

The shim delegates to `_stub` using simple forwarding. For ref return members, the shim's explicit implementation cannot use `ref` because it's calling through an interface reference. However, this is fine because:
- The `_source` check in the interceptor already handles this: for ref returns, it copies the value to `_refReturnBacking` (lossy ref redirection, already accepted as a pattern for Source(T))
- The shim delegation path is: shim returns non-ref value -> interceptor stores in backing field -> returns ref to backing field

#### 5j. Multiple interfaces on standalone stubs

A standalone stub can implement multiple interfaces: `class Stub : IFoo, IBar`. If both have DIMs:
- The shim implements BOTH interfaces
- Abstract members from each interface get separate explicit implementations
- DIM members from each interface are left unimplemented
- Each DIM interceptor's `_source` is wired to the same shim instance

### Phase 6: Testing

#### 6a. Existing failing tests (must pass after implementation)

- `Design.Tests/DimTests/DefaultInterfaceMethodTests.cs` -- 3 inline tests
- `Design.Tests/DimTests/DefaultInterfaceMethodStandaloneTests.cs` -- 3 standalone tests + 1 configured test

#### 6b. Additional tests to add

- DIM with configured interceptor (verify configuration takes priority over DIM) -- inline version
- Interface with mix of abstract and DIM members -- already covered by all 3 interfaces
- DIM that calls abstract members -- already covered (all 3 test interfaces have DIMs that reference abstract members)
- Source(T) overriding DIM shim -- inline and standalone versions
- Interface with no DIM members (verify no shim generated -- run existing tests, check for regressions)

#### 6c. Tests NOT in initial scope (can be added later)

- DIM on open generic interface -- requires new test interface + stubs
- DIM events -- requires new test interface with DIM event handlers (very rare scenario)

---

## Architectural Verification

### Scope Table

| Pattern | Affected? | Status | Notes |
|---|---|---|---|
| Standalone (1) | Yes | Needs Implementation | FlatModelBuilder + FlatRenderer: shim class + constructor, verified with Design.Stubs |
| Generic Standalone (2) | Yes | Needs Implementation | Same pipeline as (1), type parameters forwarded to shim |
| Standalone Class (3) | No | N/A | Uses `base.Method()` |
| Generic Standalone Class (4) | No | N/A | Uses `base.Method()` |
| Inline Interface (5) | Yes | Needs Implementation | InlineModelBuilder + InlineRenderer: shim class + constructor wiring, verified with Design.Stubs |
| Inline Class (6) | No | N/A | Uses `base.Method()` |
| Inline Delegate (7) | No | N/A | Delegates have no DIMs |
| Open Generic Interface (8) | Yes | Needs Implementation | Same pipeline as (5), type parameters forwarded to shim |
| Open Generic Class (9) | No | N/A | Uses `base.Method()` |

### Design Project Verification

**Inline stubs (existing):**
- `src/Design/Design.Stubs/DefaultMethods/DefaultMethodStubs.cs` -- 3 inline stubs for IDefaultMethodPolygon, IDefaultPropertyPolygon, IDefaultIndexerPolygon
- `src/Design/Design.Tests/DimTests/DefaultInterfaceMethodTests.cs` -- 3 failing tests (return 0 instead of 15)
- Status: **Compiles, tests fail as expected** (6 of 6 DIM tests fail)

**Standalone stubs (new, added during verification):**
- `src/Design/Design.Stubs/DefaultMethods/DefaultMethodStandaloneStubs.cs` -- 3 standalone stubs
- `src/Design/Design.Tests/DimTests/DefaultInterfaceMethodStandaloneTests.cs` -- 3 unconfigured tests + 1 configured test
- Status: **Compiles, unconfigured tests fail as expected, configured test passes** (proves interceptor priority works when configured)

**Total acceptance criteria:** 6 DIM tests must pass after implementation (3 inline + 3 standalone unconfigured). The 1 configured test already passes.

### Breaking Changes

**None.** This change only adds behavior (DIM fallback) where `default(T)` was returned before. No existing API surface changes.

The only structural change visible to users is a new constructor on flat stubs that have DIM interfaces. Since users don't call the constructor directly (they write `new DefaultMethodPolygonStub()` which matches the parameterless generated constructor), this is backward compatible.

### Codebase Analysis

Files examined during architectural review:

**Models (raw transform layer):**
- `src/Generator/Models/InterfaceModels.cs` -- `InterfaceMemberInfo` record (line 59-273), `InterfaceInfo` record (line 8-57). Needs `IsAbstract` field and `HasDimMembers` computed property.
- `src/Generator/Models/EventModels.cs` -- `EventMemberInfo` record (line 8-68). Needs `IsAbstract` field.
- `src/Generator/Models/CommonModels.cs` -- `KnockOffTypeInfo` record. No changes needed (DIM info flows through `InterfaceInfo`).

**Models (structured generation layer):**
- `src/Generator/Model/Inline/InlineInterfaceStubModel.cs` -- Needs `HasDimShim` and `ShimImplementations`.
- `src/Generator/Model/Inline/InlineInterfaceImplementation.cs` -- Needs `IsAbstract` field. Currently has 81 lines with `NeedsNullableDisable` as the last optional field.
- `src/Generator/Model/Flat/FlatGenerationUnit.cs` -- Needs `HasDimShim`, `DimInterceptorNames`, `DimShimInfos`.
- `src/Generator/Model/Flat/FlatPropertyModel.cs` -- No changes (flat models don't need IsAbstract since we build shim info from InterfaceInfo directly).
- `src/Generator/Model/Flat/FlatMethodModel.cs` -- No changes.
- `src/Generator/Model/Flat/FlatIndexerModel.cs` -- No changes.
- `src/Generator/Model/Flat/FlatEventModel.cs` -- No changes.

**Transform:**
- `src/Generator/KnockOffGenerator.Transform.cs` -- `ExtractInterfaceInfo` (line 299-408). No changes needed (factory methods handle propagation).

**Builders:**
- `src/Generator/Builder/InlineModelBuilder.cs` -- `BuildInterfaceStub` (line 85-232), `BuildImplementations` (line 671-741), individual `Build*Implementation` methods. Needs IsAbstract propagation and shim computation.
- `src/Generator/Builder/FlatModelBuilder.cs` -- `Build` (line 23-100). Needs DIM detection, shim info construction, interceptor name collection.

**Renderers:**
- `src/Generator/Renderer/InlineRenderer.cs` -- `RenderInterfaceStub` (line 102-225), constructor (line 205-208). Needs shim class rendering and constructor wiring.
- `src/Generator/Renderer/FlatRenderer.cs` -- `RenderStandardMembers` (line 1505-1536), explicit implementations (line 195-206). Needs constructor generation, shim class rendering.
- `src/Generator/Renderer/Shared/PropertyInterceptorRenderer.cs` -- `_source` check at line 479. No changes (already handles source delegation correctly).
- `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs` -- `_source` check at line 867. No changes.
- `src/Generator/Renderer/Shared/IndexerInterceptorRenderer.cs` -- `_source` check at line 473. No changes.

**Design projects:**
- `src/Design/Design.Domain/Services/IDefaultMethodPolygon.cs` -- 3 interfaces with DIMs (method, property, indexer)
- `src/Design/Design.Stubs/DefaultMethods/DefaultMethodStubs.cs` -- Inline stubs
- `src/Design/Design.Stubs/DefaultMethods/DefaultMethodStandaloneStubs.cs` -- Standalone stubs (NEW)
- `src/Design/Design.Tests/DimTests/DefaultInterfaceMethodTests.cs` -- 3 inline tests
- `src/Design/Design.Tests/DimTests/DefaultInterfaceMethodStandaloneTests.cs` -- 4 standalone tests (NEW)

### Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| Shim creates infinite recursion | Low | High | Impossible by design: shim does NOT implement DIM members, so calling them goes to C# runtime DIM. Abstract member delegation goes to `_stub` which is interface-typed, hitting the stub's explicit impl, which routes through interceptor. No cycle. |
| Open generic shim type constraints | Medium | Medium | Forward all type parameters and constraints. Pattern already exists in InlineRenderer for open generic stub classes. |
| Source(T) conflict with shim | Low | Low | Source(T) overwrites `_source`. Last writer wins. Well-defined behavior. |
| Performance regression for non-DIM interfaces | None | None | Shim only generated when `HasDimMembers` is true. No impact on existing stubs. |
| Flat constructor breaks user partial classes | Low | Low | Generated constructors on partial classes are standard C# pattern. Only generated when interface has DIMs. Users don't typically define constructors on stub partial classes. |

---

## Developer Review

**Status:** Approved
**Reviewed:** 2026-02-10

### My Understanding of This Plan

**Core Change:** When an interface has Default Interface Method (DIM) members, generate a private `__DimShim` class inside the stub. The shim implements the interface, provides explicit implementations for abstract members only (delegating back to the stub's interceptors), and deliberately does NOT implement DIM members so the C# runtime invokes the DIMs naturally. DIM interceptors have their `_source` wired to the shim at construction time.

**User-Facing API:** No API changes. Unconfigured DIM members will now run the default implementation instead of returning `default(T)`. Users can still override DIMs via the existing interceptor API (`stub.GetPerimeter.Return(42)`). `Source(T)` will overwrite the shim's `_source`.

**Internal Changes:** Add `IsAbstract` to `InterfaceMemberInfo`, `EventMemberInfo`, and `InlineInterfaceImplementation`. Add shim-related fields to `InlineInterfaceStubModel` and `FlatGenerationUnit`. Create `FlatDimShimInfo` model. Builder changes in `InlineModelBuilder` and `FlatModelBuilder` to compute shim data. Renderer changes in `InlineRenderer` and `FlatRenderer` to emit the shim class and constructor wiring.

**Patterns Affected:** Standalone (1), Generic Standalone (2), Inline Interface (5), Open Generic Interface (8). Class stubs (3,4,6,9) and inline delegate (7) not affected -- correct.

### Codebase Investigation

**Files Examined:**
- `src/Generator/Model/Inline/InlineInterfaceImplementation.cs` -- 81 lines. Has all fields needed for shim rendering: `InterfaceFullName`, `MemberName`, `ParameterDeclarations`, `ArgumentList`, `IsVoid`, `IsGenericMethod`, `TypeParameterDecl`, `ConstraintClauses`, `RefReturnPrefix`, `IsInitOnly`, `HasGetter`, `HasSetter`, `InterceptorName`, `Kind`. `NeedsNullableDisable` is the last optional field. Confirmed.
- `src/Generator/Model/Inline/InlineInterfaceStubModel.cs` -- 41 lines. Ends with `SourceProviders`. New fields go after this. Confirmed.
- `src/Generator/Models/InterfaceModels.cs` -- `InterfaceMemberInfo` (line 59-273), `InterfaceInfo` (line 8-57). `FromProperty` returns at line 169-190. `FromMethod` returns at line 256-272. Both use named constructor arguments. Adding `IsAbstract` as an optional parameter (default `true`) is straightforward. Confirmed.
- `src/Generator/Models/EventModels.cs` -- `EventMemberInfo` (line 8-68). `FromEvent` returns at line 59-67. Adding `IsAbstract` similarly. Confirmed.
- `src/Generator/Model/Flat/FlatGenerationUnit.cs` -- 32 lines. Ends with `Strict`. New fields go after. Confirmed.
- `src/Generator/Renderer/InlineRenderer.cs` -- `RenderInterfaceStub` at line 102-225. Constructor rendered at lines 205-208 (`Strict = strict;`). Closing brace at line 223 (`w.Line("\t\t}");`). Plan correctly identifies where to add shim class (before line 223) and constructor wiring (after line 207). Confirmed.
- `src/Generator/Renderer/FlatRenderer.cs` -- Uses `using (w.Block(...))` pattern at line 59, which auto-closes the brace. The explicit implementations are rendered at lines 195-206, inside the block. `RenderStandardMembers` at line 1505 renders `Strict` property and `Object`. The shim class goes inside the block. Constructor goes in `RenderStandardMembers`. Confirmed.
- `src/Generator/Builder/InlineModelBuilder.cs` -- `BuildImplementations` at line 671-741 iterates members, calling `BuildPropertyImplementation`, `BuildIndexerImplementation`, `BuildMethodImplementation`, `BuildEventImplementation`. None currently pass `IsAbstract`. `BuildPropertyImplementation` takes `InterfaceMemberInfo member` (line 743) so `member.IsAbstract` is available. `BuildEventImplementation` takes `EventMemberInfo evt` (line 1111) so `evt.IsAbstract` is available. Confirmed.
- `src/Generator/Builder/FlatModelBuilder.cs` -- `Build` at line 23-101. `GetMemberKey` at line 236. `BuildNameMap` at line 110-216. DIM interceptor name lookup needs `GetMemberKey` + `nameMap` which are both available in `Build`. Confirmed.
- `src/Generator/Renderer/Shared/PropertyInterceptorRenderer.cs` -- `_source` field at line 159, source check at line 479. Confirmed existing infrastructure handles source delegation.
- `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs` -- `_source` field at line 60, source check at line 867/871. Confirmed.
- `src/Generator/Renderer/Shared/IndexerInterceptorRenderer.cs` -- `_source` field at line 63, source check at line 473. Confirmed.

**Searches Performed:**
- `FormatTypeParameters` in FlatRenderer -- exists at line 443. Used in plan for shim class.
- `FormatConstraints` in FlatRenderer -- exists at line 452. Used in plan for shim class.
- `FormatTypeParameterList` in InlineRenderer -- exists at line 1264. Used in plan for shim class.
- `w.Braces()` -- available on CodeWriter at line 48. Used in plan for flat shim rendering.
- `_source` usage in all three shared interceptor renderers -- all follow the same pattern: field declaration, null check in Invoke, cleared in Reset. Confirmed plan's claim about priority chain.
- `RenderSourceMethods` in both renderers -- sets `_source` directly on interceptors (`mapping.InterceptorName._source = source`). Plan's claim that `Source(T)` will overwrite the shim wiring is correct.

**Design.Stubs Verification:**
- Inline stubs: `src/Design/Design.Stubs/DefaultMethods/DefaultMethodStubs.cs` -- 3 inline stubs for `IDefaultMethodPolygon`, `IDefaultPropertyPolygon`, `IDefaultIndexerPolygon`. Compiles. Confirmed.
- Inline tests: `src/Design/Design.Tests/DimTests/DefaultInterfaceMethodTests.cs` -- 3 failing tests (return 0 instead of 15). Ran tests, confirmed 3 failures. Confirmed.
- Standalone stubs: `src/Design/Design.Stubs/DefaultMethods/DefaultMethodStandaloneStubs.cs` -- 3 standalone stubs. Compiles. Confirmed.
- Standalone tests: `src/Design/Design.Tests/DimTests/DefaultInterfaceMethodStandaloneTests.cs` -- 3 unconfigured tests fail, 1 configured test passes. Ran tests, confirmed 6 total failures (3 inline + 3 standalone), 1 pass. Confirmed.

**Discrepancies Found:**
- None significant. The plan accurately describes the codebase structure, line numbers, and field positions.

### Structured Question Checklist

**Completeness Questions:**
- [x] Are all nine patterns addressed? Yes. Four interface patterns (1,2,5,8) get implementation. Five non-applicable patterns (3,4,6,7,9) are correctly excluded with justification.
- [x] What happens with null/empty/default? `IsAbstract` defaults to `true` for backward compatibility. If an interface has zero DIM members, no shim is generated. Covered.
- [x] What happens with generic type parameters? Section 5b covers open generic interfaces. Type parameters and constraints forwarded to the shim class. Both renderers have existing helpers.
- [x] What happens with nested types or inherited members? Section 5a covers DIM from base interfaces. `ExtractInterfaceInfo` already collects all base interface members, and `IsAbstract` propagates through.
- [x] How does this interact with existing features? Source(T) interaction covered in 5e. Strict mode in 5g. Configured interceptors take priority (tested by the already-passing configured test).

**Correctness Questions:**
- [x] Do the generated code examples compile? The shim pattern uses standard C# (explicit interface implementation, delegation). The examples are structurally sound.
- [x] Is the proposed implementation consistent with existing patterns? Yes. Uses the same `_source` mechanism already used by `Source(T)`. Uses existing `InlineInterfaceImplementation` for the inline pipeline, new `FlatDimShimInfo` for the flat pipeline (which parallels the existing separation).
- [x] Are the model/builder/renderer responsibilities correctly assigned? Yes. Models hold data, builders compute it, renderers emit code. No logic in renderers, no emission in builders.
- [x] Breaking changes? None. Only adds behavior where `default(T)` was returned before. The parameterless constructor for flat stubs is backward compatible.

**Clarity Questions:**
- [x] Could I implement this without asking any clarifying questions? Yes. The plan provides specific file paths, line numbers, code snippets, and model definitions for each change. Verified against actual code.
- [x] Are there ambiguous requirements? No. The shim pattern is well-defined and the DIM/abstract distinction is binary.
- [x] Are edge cases explicitly handled? Yes. Nine edge cases (5a-5j) are explicitly addressed.
- [x] Is the test strategy specific enough? Yes. 6 existing failing tests serve as acceptance criteria. Additional test suggestions are concrete.

**Risk Questions:**
- [x] What could go wrong? The plan addresses this: infinite recursion (impossible by design), open generic constraints, Source(T) interaction, flat constructor. All mitigated.
- [x] Which existing tests might fail? Only the 6 DIM tests should change from failing to passing. No existing passing tests should break.
- [x] Performance implications? None for non-DIM interfaces (shim only generated when `HasDimMembers` is true).
- [x] Backward compatibility? Preserved. No API changes.

### Devil's Advocate Analysis

**Edge cases NOT explicitly covered:**
1. **DIM property with setter**: The test interfaces only have get-only DIM properties. What if a DIM property has a setter? The shim pattern should still work (the shim simply does not implement the setter, so the DIM setter runs on the shim). The plan's renderer code handles `HasGetter`/`HasSetter` correctly for abstract members in the shim, so this is implicitly covered. Not a blocker.
2. **DIM on an interface that also has events**: The shim needs event delegation for abstract events. The plan includes `InlineMemberKind.Event` in the `RenderShimImplementation` method and `FlatDimShimEventMember` in the flat shim. Covered.
3. **Multiple interfaces on an inline stub where both have DIMs**: The inline pattern generates one shim per interface (each `InlineInterfaceStubModel` has its own `ShimImplementations`). The constructor wires each DIM interceptor to its own interface's shim. This should work because each inline stub class handles one interface. Not a concern.

**Ways this could break existing functionality:**
1. **Flat constructor side effect**: The flat pipeline currently has NO constructor. Adding a parameterless constructor only when DIMs exist is safe. However, I note that existing flat stubs already work without constructors because `_source` defaults to `null` (field initializer). The constructor only adds shim wiring. No existing behavior changes.

**Ways users could misunderstand the API:**
1. **Source(null) clears the DIM**: If a user calls `stub.Source(null)`, the DIM interceptor's `_source` is cleared, and the DIM no longer executes. This is documented in section 5e. The plan acknowledges this as intentional behavior consistent with class stubs. Acceptable.

### Why This Plan Is Exceptionally Clear

This plan is one of the most thorough I have reviewed. Specifically:

1. **Every file path and line number was verified against the actual codebase and found accurate.** The plan references specific method locations, model field positions, and renderer insertion points that all match reality.
2. **The Design.Stubs evidence is comprehensive.** Both inline and standalone stubs exist, compile, and their tests fail as expected (verified by running them). The architect provided 7 tests (6 failing, 1 passing) as acceptance criteria.
3. **The model definitions are complete.** `FlatDimShimInfo` and its sub-records carry exactly the fields needed for shim rendering. `InlineInterfaceImplementation` already has all fields needed for the inline shim renderer.
4. **Nine edge cases are explicitly addressed** with clear rationale. The infinite recursion concern (the most dangerous risk) is impossible by design.
5. **Pipeline awareness is strong.** The plan correctly identifies that inline and flat pipelines need separate handling, provides separate model types for each, and does not assume "same code path."
6. **The one area left as pseudocode** (FlatModelBuilder section 3b, the `/* build from member */` placeholders) is intentional shorthand for straightforward field extraction from `InterfaceMemberInfo`, which is a mechanical translation. This is acceptable.

### Review Summary

- Files examined: 14 source files across models, builders, renderers, and design projects
- Questions checked: 16 of 16
- Devil's advocate items: 5 generated, all addressed either in the plan or implicitly by the architecture

---

## Implementation Contract

**Created:** 2026-02-10
**Approved by:** knockoff-developer

### Design.Stubs Acceptance Criteria

These are the failing Design.Stubs tests. Implementation is done when they all pass.

- [x] `src/Design/Design.Tests/DimTests/DefaultInterfaceMethodTests.cs:34` - Inline: Method DIM returns 15.0 instead of 0.0
- [x] `src/Design/Design.Tests/DimTests/DefaultInterfaceMethodTests.cs:53` - Inline: Property DIM returns 15.0 instead of 0.0
- [x] `src/Design/Design.Tests/DimTests/DefaultInterfaceMethodTests.cs:71` - Inline: Indexer DIM returns 15.0 instead of 0.0
- [x] `src/Design/Design.Tests/DimTests/DefaultInterfaceMethodStandaloneTests.cs:33` - Standalone: Method DIM returns 15.0 instead of 0.0
- [x] `src/Design/Design.Tests/DimTests/DefaultInterfaceMethodStandaloneTests.cs:65` - Standalone: Property DIM returns 15.0 instead of 0.0
- [x] `src/Design/Design.Tests/DimTests/DefaultInterfaceMethodStandaloneTests.cs:82` - Standalone: Indexer DIM returns 15.0 instead of 0.0

### In Scope

**Phase 1: Model Changes**
- [x] Add `IsAbstract` field to `InterfaceMemberInfo` (default `true`) in `src/Generator/Models/InterfaceModels.cs`
- [x] Pass `IsAbstract` from `FromProperty` and `FromMethod` factory methods
- [x] Add `IsAbstract` field to `EventMemberInfo` in `src/Generator/Models/EventModels.cs`
- [x] Pass `IsAbstract` from `FromEvent` factory method
- [x] Add `HasDimMembers` computed property to `InterfaceInfo`
- [x] Add `IsAbstract` field to `InlineInterfaceImplementation` in `src/Generator/Model/Inline/InlineInterfaceImplementation.cs`
- [x] Add `HasDimShim` and `ShimImplementations` to `InlineInterfaceStubModel` in `src/Generator/Model/Inline/InlineInterfaceStubModel.cs`
- [x] Add `HasDimShim`, `DimInterfaceNames`, `DimInterceptorNames`, `DimShimInfos` to `FlatGenerationUnit` in `src/Generator/Model/Flat/FlatGenerationUnit.cs`
- [x] Create `FlatDimShimInfo` and related records in `src/Generator/Model/Flat/FlatDimShimInfo.cs`
- [x] **Checkpoint: Solution compiles**

**Phase 2: Builder Changes**
- [x] Propagate `IsAbstract` through `InlineModelBuilder.Build*Implementation` methods
- [x] Compute shim data in `InlineModelBuilder.BuildInterfaceStub` (filter to abstract-only implementations, set `HasDimShim` and `ShimImplementations`)
- [x] Compute DIM info in `FlatModelBuilder.Build` (detect DIM interfaces, build `FlatDimShimInfo`, collect DIM interceptor names, deduplicate diamond inheritance members)
- [x] **Checkpoint: Solution compiles**

**Phase 3: Renderer Changes (Inline)**
- [x] Add `RenderDimShimClass` method to `InlineRenderer`
- [x] Add `RenderShimImplementation` method to `InlineRenderer` (property, indexer, method, event delegation)
- [x] Add shim wiring in `RenderInterfaceStub` constructor
- [x] **Checkpoint: Inline DIM tests pass (3 tests)**

**Phase 4: Renderer Changes (Flat)**
- [x] Add conditional constructor to `FlatRenderer.RenderStandardMembers`
- [x] Add `RenderFlatDimShimClass` method and delegation helpers to `FlatRenderer`
- [x] **Checkpoint: Standalone DIM tests pass (4 tests, including configured)**

**Phase 5: Verification**
- [x] All 7 DIM tests pass (6 unconfigured + 1 configured)
- [x] Full test suite passes (no regressions)
- [x] `dotnet build src/Design/Design.Stubs` succeeds

### Explicitly Out of Scope

- Open generic DIM tests (pattern 8) -- requires new test interfaces; can be added later
- DIM event tests -- rare scenario; model supports it but no test interfaces yet
- Source(T) interaction tests with DIM -- can be added later
- Strict mode + DIM interaction tests -- can be added later

### Verification Gates

1. After Phase 1: Solution compiles with new model fields
2. After Phase 2: Solution compiles with builder changes
3. After Phase 3: 3 inline DIM tests pass
4. After Phase 4: 3 standalone DIM tests pass
5. Final: All 7 DIM tests pass, full test suite passes, `dotnet build src/Design/Design.Stubs` succeeds

### Stop Conditions

If any of these occur, STOP and report:
- Out-of-scope test fails after changes
- Infinite recursion in generated stub code (indicates shim design flaw)
- Generated code does not compile
- Architectural contradiction between inline and flat shim patterns

---

## Implementation Progress

**Started:** 2026-02-10
**Developer:** knockoff-developer
**Current Status:** Awaiting Verification

### Phase 1: Model Changes - COMPLETE

All model changes applied successfully. `IsAbstract` added to `InterfaceMemberInfo`, `EventMemberInfo`, and `InlineInterfaceImplementation`. `HasDimMembers` computed property added to `InterfaceInfo`. `FlatDimShimInfo` and related records created. Shim-related fields added to `InlineInterfaceStubModel` and `FlatGenerationUnit`.

### Phase 2: Builder Changes - COMPLETE

`IsAbstract` propagated through all 6 `Build*Implementation` methods in `InlineModelBuilder`. Shim data computed in `InlineModelBuilder.BuildInterfaceStub`. DIM detection, shim info construction with diamond inheritance deduplication, and interceptor name collection added to `FlatModelBuilder.Build`.

### Phase 3: Renderer Changes (Inline) - COMPLETE

`RenderDimShimClass` and `RenderShimImplementation` methods added to `InlineRenderer`. Shim wiring added to constructor. Key design decision: shim delegation uses explicit interface casts `((InterfaceFullName)_stub).MemberName` to avoid type mismatch when diamond inheritance causes the same member name to resolve to different interface types.

3 inline DIM tests passing across net8.0, net9.0, net10.0.

### Phase 4: Renderer Changes (Flat) - COMPLETE

Conditional constructor added to `FlatRenderer.RenderStandardMembers`. `RenderFlatDimShimClass` and 4 delegation helper methods added. Key issue discovered and resolved: diamond inheritance in Neatoo interface tests caused 1032 duplicate member errors. Fixed by deduplicating shim members using HashSets in `FlatModelBuilder.Build` and consolidating all abstract members into a single `FlatDimShimInfo` entry. Added `DimInterfaceNames` field to `FlatGenerationUnit` for the shim class declaration.

4 standalone DIM tests passing across net8.0, net9.0, net10.0.

### Phase 5: Verification - COMPLETE

All 7 DIM tests pass. Full test suite passes with zero regressions. Design.Stubs builds successfully.

---

## Completion Evidence

### Test Results

**DIM Tests (7 tests x 3 frameworks = 21 executions):**
```
Passed!  - Failed: 0, Passed: 7, Skipped: 0, Total: 7 - Design.Tests.dll (net8.0)
Passed!  - Failed: 0, Passed: 7, Skipped: 0, Total: 7 - Design.Tests.dll (net9.0)
Passed!  - Failed: 0, Passed: 7, Skipped: 0, Total: 7 - Design.Tests.dll (net10.0)
```

**Full Test Suite (no regressions):**
```
Passed! - 1444 passed - KnockOffTests.dll (net8.0)
Passed! - 1445 passed - KnockOffTests.dll (net9.0)
Passed! - 1445 passed - KnockOffTests.dll (net10.0)
Passed! - 691 passed - KnockOff.Documentation.Samples.dll (net8.0)
Passed! - 691 passed - KnockOff.Documentation.Samples.dll (net9.0)
Passed! - 691 passed - KnockOff.Documentation.Samples.dll (net10.0)
Passed! - 473 passed - KnockOff.NeatooInterfaceTests.dll (net8.0)
Passed! - 473 passed - KnockOff.NeatooInterfaceTests.dll (net9.0)
Passed! - 473 passed - KnockOff.NeatooInterfaceTests.dll (net10.0)
Passed! - 14 passed - KnockOffTests.AssemblyStrict.dll (net8.0)
Passed! - 14 passed - KnockOffTests.AssemblyStrict.dll (net9.0)
Passed! - 14 passed - KnockOffTests.AssemblyStrict.dll (net10.0)
```

**Design.Stubs Build:**
```
Build succeeded. 0 Warning(s) 0 Error(s)
Design.Stubs -> net8.0, net9.0, net10.0
```

### Generated Code Samples

**Inline shim (DefaultMethodPolygonStub):**
```csharp
// Constructor wiring
var __shim = new __DimShim(this);
GetPerimeter._source = __shim;

// Shim class (only abstract members delegated)
private sealed class __DimShim : IDefaultMethodPolygon
{
    private readonly IDefaultMethodPolygon _stub;
    internal __DimShim(IDefaultMethodPolygon stub) { _stub = stub; }

    int IDefaultMethodPolygon.NumberOfSides
    {
        get => ((IDefaultMethodPolygon)_stub).NumberOfSides;
    }
    int IDefaultMethodPolygon.SideLength
    {
        get => ((IDefaultMethodPolygon)_stub).SideLength;
    }
    // GetPerimeter NOT implemented -> DIM executes naturally
}
```

**Standalone shim (same pattern, different indent level):**
```csharp
public DefaultMethodPolygonStub()
{
    var __shim = new __DimShim(this);
    GetPerimeter._source = __shim;
}

private sealed class __DimShim : IDefaultMethodPolygon
{
    private readonly IDefaultMethodPolygon _stub;
    internal __DimShim(IDefaultMethodPolygon stub) => _stub = stub;

    int IDefaultMethodPolygon.NumberOfSides
    {
        get => ((IDefaultMethodPolygon)_stub).NumberOfSides;
    }
    int IDefaultMethodPolygon.SideLength
    {
        get => ((IDefaultMethodPolygon)_stub).SideLength;
    }
}
```

### Files Modified

**New Files:**
- `src/Generator/Model/Flat/FlatDimShimInfo.cs` - Shim model records for flat pipeline

**Modified Files:**
- `src/Generator/Models/InterfaceModels.cs` - Added `IsAbstract` to `InterfaceMemberInfo`, `HasDimMembers` to `InterfaceInfo`
- `src/Generator/Models/EventModels.cs` - Added `IsAbstract` to `EventMemberInfo`
- `src/Generator/Model/Inline/InlineInterfaceImplementation.cs` - Added `IsAbstract` field
- `src/Generator/Model/Inline/InlineInterfaceStubModel.cs` - Added `HasDimShim`, `ShimImplementations`
- `src/Generator/Model/Flat/FlatGenerationUnit.cs` - Added `HasDimShim`, `DimInterfaceNames`, `DimInterceptorNames`, `DimShimInfos`
- `src/Generator/Builder/InlineModelBuilder.cs` - `IsAbstract` propagation in 6 methods, shim computation in `BuildInterfaceStub`
- `src/Generator/Builder/FlatModelBuilder.cs` - DIM detection, deduplication, shim info construction in `Build`
- `src/Generator/Renderer/InlineRenderer.cs` - Shim class rendering, constructor wiring
- `src/Generator/Renderer/FlatRenderer.cs` - Constructor generation, shim class rendering

### Implementation Deviations from Plan

1. **Explicit interface casts in shim delegation**: The plan suggested `_stub.MemberName` for shim delegation. Implementation uses `((InterfaceFullName)_stub).MemberName` instead. This was necessary because diamond inheritance (e.g., Neatoo interfaces) causes `_stub.MemberName` to resolve to the wrong interface member type, producing CS0266 type mismatch errors.

2. **Diamond inheritance deduplication in FlatModelBuilder**: The plan assumed one `FlatDimShimInfo` per DIM interface. Implementation consolidates all abstract members across all DIM interfaces into a single deduplicated entry using HashSets, because diamond inheritance causes the same member to appear in multiple interfaces. A separate `DimInterfaceNames` field was added to `FlatGenerationUnit` for the shim class declaration.

3. **`DimInterfaceNames` field added to `FlatGenerationUnit`**: Not in original plan. Needed because the consolidated `FlatDimShimInfo` entry lost the per-interface names, but the renderer needs them for the shim class declaration (`class __DimShim : IFoo, IBar`).

### Contract Item Verification

All contract items checked. All acceptance criteria met. All verification gates passed.

---

## Architect Verification

**Verified:** 2026-02-10
**Verdict:** VERIFIED

### Independent Test Results

All builds and tests run independently by the architect. Zero failures.

**Solution Build:**
```
Build succeeded. 0 Warning(s) 0 Error(s) - src/KnockOff.sln
```

**Design.Stubs Build:**
```
Build succeeded. 0 Warning(s) 0 Error(s) - net8.0, net9.0, net10.0
```

**Full Test Suite (no regressions):**
```
Passed! - 1444 passed, 0 failed - KnockOffTests.dll (net8.0)
Passed! - 1445 passed, 0 failed - KnockOffTests.dll (net9.0)
Passed! - 1445 passed, 0 failed - KnockOffTests.dll (net10.0)
Passed! -  691 passed, 0 failed - KnockOff.Documentation.Samples.dll (net8.0)
Passed! -  691 passed, 0 failed - KnockOff.Documentation.Samples.dll (net9.0)
Passed! -  691 passed, 0 failed - KnockOff.Documentation.Samples.dll (net10.0)
Passed! -  473 passed, 0 failed - KnockOff.NeatooInterfaceTests.dll (net8.0)
Passed! -  473 passed, 0 failed - KnockOff.NeatooInterfaceTests.dll (net9.0)
Passed! -  473 passed, 0 failed - KnockOff.NeatooInterfaceTests.dll (net10.0)
Passed! -   14 passed, 0 failed - KnockOffTests.AssemblyStrict.dll (net8.0)
Passed! -   14 passed, 0 failed - KnockOffTests.AssemblyStrict.dll (net9.0)
Passed! -   14 passed, 0 failed - KnockOffTests.AssemblyStrict.dll (net10.0)
```

**Design.Tests (including 7 DIM tests):**
```
Passed! - 377 passed, 0 failed - Design.Tests.dll (net8.0)
Passed! - 377 passed, 0 failed - Design.Tests.dll (net9.0)
Passed! - 377 passed, 0 failed - Design.Tests.dll (net10.0)
```

### Design Match Verification

**Generated code matches the plan's shim pattern:**

1. **Inline shim (DefaultMethodDemo.Stubs.g.cs):** Constructor wires `__shim` to DIM interceptors. Shim class implements interface, delegates abstract members via explicit interface cast `((IDefaultMethodPolygon)_stub).MemberName`. DIM members are NOT implemented on the shim. Matches plan.

2. **Standalone shim (DefaultMethodPolygonStub.g.cs):** Parameterless constructor created. Shim class inside partial class body. Same delegation pattern as inline. Matches plan.

3. **Three DIM member types covered:** Method DIM (`GetPerimeter`), property DIM (`Perimeter`), indexer DIM (`this[int]`). All three produce correct shim code.

4. **No regression for non-DIM stubs:** Grep confirmed `__DimShim` only appears in the 4 expected generated files (3 standalone + 1 inline). Zero matches in src/Tests/, confirming HasDimMembers guard prevents unnecessary shim generation.

### Deviation Analysis

All three deviations from the original plan are well-justified:

1. **Explicit interface casts** (`((IFoo)_stub).Member` instead of `_stub.Member`): Necessary for diamond inheritance correctness. Prevents CS0266 when the same member name resolves to different interface types.

2. **Consolidated shim info with deduplication**: Prevents 1032 duplicate member errors from diamond inheritance in Neatoo interfaces. Uses HashSets for deterministic deduplication.

3. **`DimInterfaceNames` field**: Clean separation between the interface list for the shim class declaration and the member data for rendering.

### Production Code Review

All 9 modified/new production files reviewed:

- `InterfaceModels.cs`: `IsAbstract` field with correct `true` default. `HasDimMembers` computed property. Factory methods pass `property.IsAbstract` and `method.IsAbstract`.
- `EventModels.cs`: `IsAbstract` field. Factory method passes `eventSymbol.IsAbstract`.
- `InlineInterfaceImplementation.cs`: `IsAbstract` field as last optional parameter.
- `InlineInterfaceStubModel.cs`: `HasDimShim` and `ShimImplementations` fields.
- `FlatGenerationUnit.cs`: `HasDimShim`, `DimInterfaceNames`, `DimInterceptorNames`, `DimShimInfos` fields.
- `FlatDimShimInfo.cs`: New file with 4 member records. Clean, minimal models for shim rendering.
- `InlineModelBuilder.cs`: `IsAbstract` propagated through all 6 Build*Implementation methods. Shim data computed by filtering implementations to abstract-only.
- `FlatModelBuilder.cs`: DIM detection with diamond inheritance deduplication. Interceptor name collection via nameMap lookup.
- `InlineRenderer.cs`: `RenderDimShimClass` and `RenderShimImplementation` methods. Constructor wiring with HashSet deduplication.
- `FlatRenderer.cs`: Conditional constructor, `RenderFlatDimShimClass` and 4 delegation helper methods.

### Notes

The branch also contains uncommitted changes to `ClassModelBuilder.cs`, `StandaloneClassModelBuilder.cs`, `ClassRenderer.cs`, `InlineClassStubModel.cs`, and `InlineGenericTypeArityGroup.cs`. These appear to be nullable type parameter handling improvements for class stubs, unrelated to the DIM shim feature. All tests pass, so these changes do not introduce regressions.
