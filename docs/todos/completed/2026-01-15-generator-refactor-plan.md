# KnockOffGenerator Refactor Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Refactor KnockOffGenerator from mixed model/rendering to clean Code Model + Renderer pattern.

**Architecture:** Transform phase produces `KnockOffTypeInfo` (unchanged). New builders convert TypeInfo to domain-specific models. New renderers walk models and emit code using `CodeWriter` helper. Three separate hierarchies: Flat, Inline, Class.

**Tech Stack:** C# records for models, pattern matching in builders, `CodeWriter` for indentation management, netstandard2.0 target.

**Validation:** All existing tests must pass without modification.

---

## Phase 1: Foundation

### Task 1.1: Create CodeWriter Utility

**Files:**
- Create: `src/Generator/Renderer/CodeWriter.cs`

**Step 1: Create the CodeWriter class**

```csharp
// src/Generator/Renderer/CodeWriter.cs
#nullable enable
using System;
using System.Text;

namespace KnockOff.Renderer;

/// <summary>
/// Helper for generating formatted C# code with automatic indentation management.
/// </summary>
internal sealed class CodeWriter
{
    private readonly StringBuilder _sb = new();
    private int _indent = 0;

    public void Line(string text = "")
    {
        if (string.IsNullOrEmpty(text))
        {
            _sb.AppendLine();
        }
        else
        {
            _sb.Append('\t', _indent);
            _sb.AppendLine(text);
        }
    }

    public void Append(string text) => _sb.Append(text);

    public IDisposable Block(string header)
    {
        Line(header);
        Line("{");
        _indent++;
        return new BlockScope(this);
    }

    public IDisposable Braces()
    {
        Line("{");
        _indent++;
        return new BlockScope(this);
    }

    public override string ToString() => _sb.ToString();

    private sealed class BlockScope : IDisposable
    {
        private readonly CodeWriter _writer;
        public BlockScope(CodeWriter writer) => _writer = writer;
        public void Dispose()
        {
            _writer._indent--;
            _writer.Line("}");
        }
    }
}
```

**Step 2: Verify it compiles**

Run: `dotnet build src/Generator/Generator.csproj`
Expected: Build succeeds

**Step 3: Commit**

```bash
git add src/Generator/Renderer/CodeWriter.cs
git commit -m "refactor: add CodeWriter utility for clean code generation"
```

---

### Task 1.2: Create Shared ParameterModel

**Files:**
- Create: `src/Generator/Model/Shared/ParameterModel.cs`

**Step 1: Create the ParameterModel record**

```csharp
// src/Generator/Model/Shared/ParameterModel.cs
#nullable enable
using Microsoft.CodeAnalysis;

namespace KnockOff.Model.Shared;

/// <summary>
/// Represents a resolved method parameter for code generation.
/// </summary>
internal sealed record ParameterModel
{
    public required string Name { get; init; }
    public required string EscapedName { get; init; }
    public required string Type { get; init; }
    public required string NullableType { get; init; }
    public RefKind RefKind { get; init; }
    public required string RefPrefix { get; init; }
}
```

**Step 2: Verify it compiles**

Run: `dotnet build src/Generator/Generator.csproj`
Expected: Build succeeds

**Step 3: Commit**

```bash
git add src/Generator/Model/Shared/ParameterModel.cs
git commit -m "refactor: add ParameterModel for resolved method parameters"
```

---

### Task 1.3: Create Shared TypeParameterModel

**Files:**
- Create: `src/Generator/Model/Shared/TypeParameterModel.cs`

**Step 1: Create the TypeParameterModel record**

```csharp
// src/Generator/Model/Shared/TypeParameterModel.cs
#nullable enable

namespace KnockOff.Model.Shared;

/// <summary>
/// Represents a resolved type parameter for code generation.
/// </summary>
internal sealed record TypeParameterModel
{
    public required string Name { get; init; }
    public required string Constraints { get; init; }
}
```

**Step 2: Verify it compiles**

Run: `dotnet build src/Generator/Generator.csproj`
Expected: Build succeeds

**Step 3: Commit**

```bash
git add src/Generator/Model/Shared/TypeParameterModel.cs
git commit -m "refactor: add TypeParameterModel for resolved type parameters"
```

---

### Task 1.4: Create Shared ContainingTypeModel

**Files:**
- Create: `src/Generator/Model/Shared/ContainingTypeModel.cs`

**Step 1: Create the ContainingTypeModel record**

```csharp
// src/Generator/Model/Shared/ContainingTypeModel.cs
#nullable enable

namespace KnockOff.Model.Shared;

/// <summary>
/// Represents a containing type for nested class declarations.
/// </summary>
internal sealed record ContainingTypeModel
{
    public required string Keyword { get; init; }
    public required string Name { get; init; }
    public required string AccessModifier { get; init; }
}
```

**Step 2: Verify it compiles**

Run: `dotnet build src/Generator/Generator.csproj`
Expected: Build succeeds

**Step 3: Commit**

```bash
git add src/Generator/Model/Shared/ContainingTypeModel.cs
git commit -m "refactor: add ContainingTypeModel for nested class support"
```

---

## Phase 2: Flat API Models

### Task 2.1: Create FlatGenerationUnit

**Files:**
- Create: `src/Generator/Model/Flat/FlatGenerationUnit.cs`

**Step 1: Create the FlatGenerationUnit record**

```csharp
// src/Generator/Model/Flat/FlatGenerationUnit.cs
#nullable enable
using KnockOff.Model.Shared;

namespace KnockOff.Model.Flat;

/// <summary>
/// Top-level container for standalone stub generation.
/// Contains all resolved information needed to emit the file.
/// </summary>
internal sealed record FlatGenerationUnit
{
    public required string ClassName { get; init; }
    public required string Namespace { get; init; }
    public required EquatableArray<string> InterfaceList { get; init; }
    public required EquatableArray<TypeParameterModel> TypeParameters { get; init; }
    public required EquatableArray<FlatPropertyModel> Properties { get; init; }
    public required EquatableArray<FlatIndexerModel> Indexers { get; init; }
    public required EquatableArray<FlatMethodModel> Methods { get; init; }
    public required EquatableArray<FlatGenericMethodHandlerModel> GenericMethodHandlers { get; init; }
    public required EquatableArray<FlatEventModel> Events { get; init; }
    public bool HasGenericMethods { get; init; }
    public bool ImplementsIKnockOffStub { get; init; }
}
```

**Step 2: Verify it compiles**

Run: `dotnet build src/Generator/Generator.csproj`
Expected: Build fails (missing FlatPropertyModel, etc.) - expected at this point

**Step 3: Continue to next task** (don't commit until models compile)

---

### Task 2.2: Create FlatPropertyModel

**Files:**
- Create: `src/Generator/Model/Flat/FlatPropertyModel.cs`

**Step 1: Create the FlatPropertyModel record**

```csharp
// src/Generator/Model/Flat/FlatPropertyModel.cs
#nullable enable

namespace KnockOff.Model.Flat;

/// <summary>
/// Resolved model for a property in flat API generation.
/// All names and expressions are pre-computed.
/// </summary>
internal sealed record FlatPropertyModel
{
    public required string InterceptorName { get; init; }
    public required string InterceptorClassName { get; init; }
    public required string DeclaringInterface { get; init; }
    public required string MemberName { get; init; }
    public required string ReturnType { get; init; }
    public required string NullableReturnType { get; init; }
    public bool HasGetter { get; init; }
    public bool HasSetter { get; init; }
    public bool IsInitOnly { get; init; }
    public required string DefaultExpression { get; init; }
    public string? SetterPragmaDisable { get; init; }
    public string? SetterPragmaRestore { get; init; }
    public required string SimpleInterfaceName { get; init; }
    public string? UserMethodName { get; init; }
    public bool NeedsNewKeyword { get; init; }
}
```

**Step 2: Continue to next task**

---

### Task 2.3: Create FlatIndexerModel

**Files:**
- Create: `src/Generator/Model/Flat/FlatIndexerModel.cs`

**Step 1: Create the FlatIndexerModel record**

```csharp
// src/Generator/Model/Flat/FlatIndexerModel.cs
#nullable enable

namespace KnockOff.Model.Flat;

/// <summary>
/// Resolved model for an indexer in flat API generation.
/// </summary>
internal sealed record FlatIndexerModel
{
    public required string InterceptorName { get; init; }
    public required string InterceptorClassName { get; init; }
    public required string DeclaringInterface { get; init; }
    public required string ReturnType { get; init; }
    public required string NullableReturnType { get; init; }
    public required string DefaultExpression { get; init; }
    public required string KeyType { get; init; }
    public required string KeyParamName { get; init; }
    public required string NullableKeyType { get; init; }
    public bool HasGetter { get; init; }
    public bool HasSetter { get; init; }
    public required string SimpleInterfaceName { get; init; }
    public bool NeedsNewKeyword { get; init; }
}
```

**Step 2: Continue to next task**

---

### Task 2.4: Create FlatMethodModel

**Files:**
- Create: `src/Generator/Model/Flat/FlatMethodModel.cs`

**Step 1: Create the FlatMethodModel record**

```csharp
// src/Generator/Model/Flat/FlatMethodModel.cs
#nullable enable
using KnockOff.Model.Shared;

namespace KnockOff.Model.Flat;

/// <summary>
/// Resolved model for a method in flat API generation.
/// </summary>
internal sealed record FlatMethodModel
{
    public required string InterceptorName { get; init; }
    public required string InterceptorClassName { get; init; }
    public required string DeclaringInterface { get; init; }
    public required string MethodName { get; init; }
    public required string ReturnType { get; init; }
    public bool IsVoid { get; init; }
    public required EquatableArray<ParameterModel> Parameters { get; init; }
    public required string ParameterDeclarations { get; init; }
    public required string ParameterNames { get; init; }
    public required string RecordCallArgs { get; init; }
    public required EquatableArray<ParameterModel> TrackableParameters { get; init; }
    public string? LastCallType { get; init; }
    public required string OnCallDelegateType { get; init; }
    public bool NeedsCustomDelegate { get; init; }
    public string? CustomDelegateName { get; init; }
    public string? CustomDelegateSignature { get; init; }
    public required string DefaultExpression { get; init; }
    public bool ThrowsOnDefault { get; init; }
    public string? UserMethodCall { get; init; }
    public required string SimpleInterfaceName { get; init; }
    public required string TypeParameterDecl { get; init; }
    public required string TypeParameterList { get; init; }
    public required string ConstraintClauses { get; init; }
    public bool NeedsNewKeyword { get; init; }
}
```

**Step 2: Continue to next task**

---

### Task 2.5: Create FlatGenericMethodHandlerModel

**Files:**
- Create: `src/Generator/Model/Flat/FlatGenericMethodHandlerModel.cs`

**Step 1: Create the FlatGenericMethodHandlerModel record**

```csharp
// src/Generator/Model/Flat/FlatGenericMethodHandlerModel.cs
#nullable enable
using KnockOff.Model.Shared;

namespace KnockOff.Model.Flat;

/// <summary>
/// Resolved model for generic methods using the Of&lt;T&gt;() pattern.
/// </summary>
internal sealed record FlatGenericMethodHandlerModel
{
    public required string InterceptorName { get; init; }
    public required string InterceptorClassName { get; init; }
    public required string MethodName { get; init; }
    public required string TypeParameterNames { get; init; }
    public required string KeyType { get; init; }
    public required string KeyConstruction { get; init; }
    public required string ConstraintClauses { get; init; }
    public required string TypedHandlerClassName { get; init; }
    public required string DelegateSignature { get; init; }
    public required EquatableArray<ParameterModel> NonGenericParams { get; init; }
    public string? LastCallType { get; init; }
    public bool IsVoid { get; init; }
    public required string ReturnType { get; init; }
    public bool NeedsNewKeyword { get; init; }
}
```

**Step 2: Continue to next task**

---

### Task 2.6: Create FlatEventModel

**Files:**
- Create: `src/Generator/Model/Flat/FlatEventModel.cs`

**Step 1: Create the FlatEventModel record**

```csharp
// src/Generator/Model/Flat/FlatEventModel.cs
#nullable enable

namespace KnockOff.Model.Flat;

/// <summary>
/// Resolved model for an event in flat API generation.
/// </summary>
internal sealed record FlatEventModel
{
    public required string InterceptorName { get; init; }
    public required string InterceptorClassName { get; init; }
    public required string DeclaringInterface { get; init; }
    public required string EventName { get; init; }
    public required string DelegateType { get; init; }
    public required string RaiseParameters { get; init; }
    public required string RaiseArguments { get; init; }
    public required string RaiseReturnType { get; init; }
    public bool RaiseReturnsValue { get; init; }
    public bool UsesDynamicInvoke { get; init; }
    public bool NeedsNewKeyword { get; init; }
}
```

**Step 2: Verify all Flat models compile**

Run: `dotnet build src/Generator/Generator.csproj`
Expected: Build succeeds

**Step 3: Commit all Flat models**

```bash
git add src/Generator/Model/Flat/
git commit -m "refactor: add Flat API generation models"
```

---

## Phase 3: Flat ModelBuilder

### Task 3.1: Create FlatModelBuilder Skeleton

**Files:**
- Create: `src/Generator/Builder/FlatModelBuilder.cs`

**Step 1: Create the builder skeleton with Build method**

```csharp
// src/Generator/Builder/FlatModelBuilder.cs
#nullable enable
using System.Collections.Generic;
using System.Linq;
using KnockOff.Model.Flat;
using KnockOff.Model.Shared;

namespace KnockOff.Builder;

/// <summary>
/// Transforms KnockOffTypeInfo into FlatGenerationUnit.
/// All decision logic for "what to generate" lives here.
/// </summary>
internal static class FlatModelBuilder
{
    public static FlatGenerationUnit Build(KnockOffTypeInfo typeInfo)
    {
        // Step 1: Build the name map (collision resolution)
        var nameMap = BuildNameMap(typeInfo.FlatMembers, typeInfo.FlatEvents, typeInfo.UserMethods);

        // Step 2: Group methods
        var methodGroups = KnockOffGenerator.GroupMethodsByName(
            typeInfo.FlatMembers.Where(m => !m.IsProperty && !m.IsIndexer));

        // Step 3: Count indexers for naming strategy
        var indexerCount = SymbolHelpers.CountIndexers(typeInfo.FlatMembers);

        // Step 4: Build resolved models
        var properties = BuildPropertyModels(typeInfo, nameMap);
        var indexers = BuildIndexerModels(typeInfo, nameMap, indexerCount);
        var (methods, genericHandlers) = BuildMethodModels(typeInfo, nameMap, methodGroups);
        var events = BuildEventModels(typeInfo, nameMap);

        return new FlatGenerationUnit
        {
            ClassName = typeInfo.Name,
            Namespace = typeInfo.Namespace,
            InterfaceList = typeInfo.Interfaces.Select(i => i.FullName).ToEquatableArray(),
            TypeParameters = BuildTypeParameters(typeInfo.TypeParameters),
            Properties = properties,
            Indexers = indexers,
            Methods = methods,
            GenericMethodHandlers = genericHandlers,
            Events = events,
            HasGenericMethods = genericHandlers.Count > 0,
            ImplementsIKnockOffStub = true
        };
    }

    // Placeholder methods - will be implemented in subsequent tasks
    private static Dictionary<string, string> BuildNameMap(
        EquatableArray<InterfaceMemberInfo> flatMembers,
        EquatableArray<EventMemberInfo> flatEvents,
        EquatableArray<UserMethodInfo> userMethods)
    {
        // TODO: Extract from KnockOffGenerator.BuildFlatNameMap
        throw new System.NotImplementedException();
    }

    private static EquatableArray<TypeParameterModel> BuildTypeParameters(
        EquatableArray<TypeParameterInfo> typeParameters)
    {
        // TODO: Implement
        throw new System.NotImplementedException();
    }

    private static EquatableArray<FlatPropertyModel> BuildPropertyModels(
        KnockOffTypeInfo typeInfo,
        Dictionary<string, string> nameMap)
    {
        // TODO: Implement
        throw new System.NotImplementedException();
    }

    private static EquatableArray<FlatIndexerModel> BuildIndexerModels(
        KnockOffTypeInfo typeInfo,
        Dictionary<string, string> nameMap,
        int indexerCount)
    {
        // TODO: Implement
        throw new System.NotImplementedException();
    }

    private static (EquatableArray<FlatMethodModel>, EquatableArray<FlatGenericMethodHandlerModel>) BuildMethodModels(
        KnockOffTypeInfo typeInfo,
        Dictionary<string, string> nameMap,
        Dictionary<string, MethodGroupInfo> methodGroups)
    {
        // TODO: Implement
        throw new System.NotImplementedException();
    }

    private static EquatableArray<FlatEventModel> BuildEventModels(
        KnockOffTypeInfo typeInfo,
        Dictionary<string, string> nameMap)
    {
        // TODO: Implement
        throw new System.NotImplementedException();
    }
}
```

**Step 2: Verify it compiles**

Run: `dotnet build src/Generator/Generator.csproj`
Expected: Build succeeds (NotImplementedException is fine for now)

**Step 3: Commit**

```bash
git add src/Generator/Builder/FlatModelBuilder.cs
git commit -m "refactor: add FlatModelBuilder skeleton"
```

---

### Task 3.2: Implement BuildNameMap in FlatModelBuilder

**Files:**
- Modify: `src/Generator/Builder/FlatModelBuilder.cs`

**Step 1: Copy and adapt BuildFlatNameMap from KnockOffGenerator.GenerateFlat.cs**

Replace the `BuildNameMap` placeholder with the actual implementation. This is a direct copy from `KnockOffGenerator.BuildFlatNameMap` with adjusted method signature.

**Step 2: Verify it compiles**

Run: `dotnet build src/Generator/Generator.csproj`
Expected: Build succeeds

**Step 3: Commit**

```bash
git add src/Generator/Builder/FlatModelBuilder.cs
git commit -m "refactor: implement BuildNameMap in FlatModelBuilder"
```

---

### Task 3.3: Implement BuildPropertyModels in FlatModelBuilder

**Files:**
- Modify: `src/Generator/Builder/FlatModelBuilder.cs`

**Step 1: Implement property model building**

Extract logic from GenerateFlat.cs that determines:
- Interceptor name from nameMap
- Default expression
- Nullability pragmas
- User method matching

**Step 2: Verify it compiles**

Run: `dotnet build src/Generator/Generator.csproj`
Expected: Build succeeds

**Step 3: Commit**

```bash
git add src/Generator/Builder/FlatModelBuilder.cs
git commit -m "refactor: implement BuildPropertyModels in FlatModelBuilder"
```

---

### Task 3.4: Implement Remaining FlatModelBuilder Methods

**Files:**
- Modify: `src/Generator/Builder/FlatModelBuilder.cs`

**Step 1: Implement remaining builder methods**

- `BuildIndexerModels` - indexer name resolution, key type extraction
- `BuildMethodModels` - overload handling, generic method detection
- `BuildEventModels` - delegate kind to Raise signature mapping
- `BuildTypeParameters` - type parameter formatting

**Step 2: Verify it compiles**

Run: `dotnet build src/Generator/Generator.csproj`
Expected: Build succeeds

**Step 3: Commit**

```bash
git add src/Generator/Builder/FlatModelBuilder.cs
git commit -m "refactor: complete FlatModelBuilder implementation"
```

---

## Phase 4: Flat Renderer

### Task 4.1: Create FlatRenderer Skeleton

**Files:**
- Create: `src/Generator/Renderer/FlatRenderer.cs`

**Step 1: Create the renderer with main Render method**

```csharp
// src/Generator/Renderer/FlatRenderer.cs
#nullable enable
using System.Linq;
using KnockOff.Model.Flat;
using KnockOff.Model.Shared;

namespace KnockOff.Renderer;

/// <summary>
/// Renders FlatGenerationUnit to source code string.
/// Pure emission - no decisions, just output what the model says.
/// </summary>
internal static class FlatRenderer
{
    public static string Render(FlatGenerationUnit unit)
    {
        var w = new CodeWriter();

        w.Line("// <auto-generated/>");
        w.Line("#nullable enable");
        w.Line();

        if (!string.IsNullOrEmpty(unit.Namespace))
        {
            w.Line($"namespace {unit.Namespace};");
            w.Line();
        }

        var interfaces = string.Join(", ", unit.InterfaceList);
        var typeParams = FormatTypeParameters(unit.TypeParameters);
        var constraints = FormatConstraints(unit.TypeParameters);

        using (w.Block($"partial class {unit.ClassName}{typeParams} : {interfaces}, global::KnockOff.IKnockOffStub{constraints}"))
        {
            if (unit.HasGenericMethods)
            {
                RenderGenericMethodInterfaces(w);
            }

            // Interceptor classes
            foreach (var prop in unit.Properties)
                RenderPropertyInterceptorClass(w, prop, unit.ClassName);

            foreach (var indexer in unit.Indexers)
                RenderIndexerInterceptorClass(w, indexer, unit.ClassName);

            foreach (var method in unit.Methods)
                RenderMethodInterceptorClass(w, method, unit.ClassName);

            foreach (var handler in unit.GenericMethodHandlers)
                RenderGenericMethodHandler(w, handler, unit.ClassName);

            foreach (var evt in unit.Events)
                RenderEventInterceptorClass(w, evt, unit.ClassName);

            // Interceptor properties
            RenderInterceptorProperties(w, unit);

            // Standard members (Strict, Object)
            RenderStandardMembers(w, unit);

            // Explicit interface implementations
            foreach (var prop in unit.Properties)
                RenderPropertyImplementation(w, prop);

            foreach (var indexer in unit.Indexers)
                RenderIndexerImplementation(w, indexer);

            foreach (var method in unit.Methods)
                RenderMethodImplementation(w, method);

            foreach (var evt in unit.Events)
                RenderEventImplementation(w, evt);
        }

        return w.ToString();
    }

    // Placeholder render methods
    private static string FormatTypeParameters(EquatableArray<TypeParameterModel> typeParams)
    {
        if (typeParams.Count == 0) return "";
        return "<" + string.Join(", ", typeParams.Select(tp => tp.Name)) + ">";
    }

    private static string FormatConstraints(EquatableArray<TypeParameterModel> typeParams)
    {
        var constraints = typeParams
            .Where(tp => !string.IsNullOrEmpty(tp.Constraints))
            .Select(tp => tp.Constraints);
        return string.Join(" ", constraints);
    }

    private static void RenderGenericMethodInterfaces(CodeWriter w)
    {
        w.Line("private interface IGenericMethodCallTracker { int CallCount { get; } bool WasCalled { get; } }");
        w.Line("private interface IResettable { void Reset(); }");
        w.Line();
    }

    // TODO: Implement all render methods
    private static void RenderPropertyInterceptorClass(CodeWriter w, FlatPropertyModel prop, string className)
        => throw new System.NotImplementedException();

    private static void RenderIndexerInterceptorClass(CodeWriter w, FlatIndexerModel indexer, string className)
        => throw new System.NotImplementedException();

    private static void RenderMethodInterceptorClass(CodeWriter w, FlatMethodModel method, string className)
        => throw new System.NotImplementedException();

    private static void RenderGenericMethodHandler(CodeWriter w, FlatGenericMethodHandlerModel handler, string className)
        => throw new System.NotImplementedException();

    private static void RenderEventInterceptorClass(CodeWriter w, FlatEventModel evt, string className)
        => throw new System.NotImplementedException();

    private static void RenderInterceptorProperties(CodeWriter w, FlatGenerationUnit unit)
        => throw new System.NotImplementedException();

    private static void RenderStandardMembers(CodeWriter w, FlatGenerationUnit unit)
        => throw new System.NotImplementedException();

    private static void RenderPropertyImplementation(CodeWriter w, FlatPropertyModel prop)
        => throw new System.NotImplementedException();

    private static void RenderIndexerImplementation(CodeWriter w, FlatIndexerModel indexer)
        => throw new System.NotImplementedException();

    private static void RenderMethodImplementation(CodeWriter w, FlatMethodModel method)
        => throw new System.NotImplementedException();

    private static void RenderEventImplementation(CodeWriter w, FlatEventModel evt)
        => throw new System.NotImplementedException();
}
```

**Step 2: Verify it compiles**

Run: `dotnet build src/Generator/Generator.csproj`
Expected: Build succeeds

**Step 3: Commit**

```bash
git add src/Generator/Renderer/FlatRenderer.cs
git commit -m "refactor: add FlatRenderer skeleton"
```

---

### Task 4.2: Implement Property Rendering in FlatRenderer

**Files:**
- Modify: `src/Generator/Renderer/FlatRenderer.cs`

**Step 1: Implement RenderPropertyInterceptorClass and RenderPropertyImplementation**

Translate the string building from GenerateFlat.cs to CodeWriter calls, using values from FlatPropertyModel.

**Step 2: Verify it compiles**

Run: `dotnet build src/Generator/Generator.csproj`
Expected: Build succeeds

**Step 3: Commit**

```bash
git add src/Generator/Renderer/FlatRenderer.cs
git commit -m "refactor: implement property rendering in FlatRenderer"
```

---

### Task 4.3: Implement Remaining FlatRenderer Methods

**Files:**
- Modify: `src/Generator/Renderer/FlatRenderer.cs`

**Step 1: Implement all remaining render methods**

- Indexer interceptor class and implementation
- Method interceptor class and implementation
- Generic method handler
- Event interceptor class and implementation
- Interceptor properties
- Standard members (Strict, Object)

**Step 2: Verify it compiles**

Run: `dotnet build src/Generator/Generator.csproj`
Expected: Build succeeds

**Step 3: Commit**

```bash
git add src/Generator/Renderer/FlatRenderer.cs
git commit -m "refactor: complete FlatRenderer implementation"
```

---

## Phase 5: Wire Up Flat API

### Task 5.1: Update KnockOffGenerator to Use Flat Builder/Renderer

**Files:**
- Modify: `src/Generator/KnockOffGenerator.cs`

**Step 1: Add using statements and update GenerateKnockOff method**

```csharp
// Add at top of file
using KnockOff.Builder;
using KnockOff.Renderer;

// Replace the body of GenerateKnockOff method:
private static void GenerateKnockOff(SourceProductionContext context, KnockOffTypeInfo typeInfo)
{
    // Report diagnostics
    foreach (var diag in typeInfo.Diagnostics)
        ReportDiagnostic(context, diag);

    // Check for blocking diagnostics
    if (typeInfo.Diagnostics.Any(d => IsBlockingDiagnostic(d.Id)))
        return;

    // Build model and render
    var unit = FlatModelBuilder.Build(typeInfo);
    var source = FlatRenderer.Render(unit);

    context.AddSource($"{typeInfo.Name}.g.cs", source);
}
```

**Step 2: Run tests to verify output matches**

Run: `dotnet test src/KnockOff.sln`
Expected: All tests pass

**Step 3: If tests fail, compare output and fix differences**

Use diff tools to compare generated output before and after. Adjust FlatModelBuilder or FlatRenderer as needed.

**Step 4: Commit**

```bash
git add src/Generator/KnockOffGenerator.cs
git commit -m "refactor: wire up FlatModelBuilder and FlatRenderer"
```

---

### Task 5.2: Delete GenerateFlat.cs Code

**Files:**
- Delete: Methods in `src/Generator/KnockOffGenerator.GenerateFlat.cs` that are now redundant

**Step 1: Remove redundant generation methods**

Keep helper methods that are still used by the builder. Delete:
- `GenerateFlatMemberInterceptorClass`
- `GenerateFlatPropertyImplementationWithName`
- etc.

**Step 2: Run tests**

Run: `dotnet test src/KnockOff.sln`
Expected: All tests pass

**Step 3: Commit**

```bash
git add src/Generator/KnockOffGenerator.GenerateFlat.cs
git commit -m "refactor: remove redundant GenerateFlat methods"
```

---

## Phase 6: Inline API (Same Pattern)

### Task 6.1: Create Inline Models

**Files:**
- Create: `src/Generator/Model/Inline/InlineGenerationUnit.cs`
- Create: `src/Generator/Model/Inline/InlineInterfaceStubModel.cs`
- Create: `src/Generator/Model/Inline/InlinePropertyModel.cs`
- Create: `src/Generator/Model/Inline/InlineIndexerModel.cs`
- Create: `src/Generator/Model/Inline/InlineMethodModel.cs`
- Create: `src/Generator/Model/Inline/InlineGenericMethodHandlerModel.cs`
- Create: `src/Generator/Model/Inline/InlineEventModel.cs`
- Create: `src/Generator/Model/Inline/InlineDelegateStubModel.cs`
- Create: `src/Generator/Model/Inline/PartialPropertyModel.cs`

Follow same pattern as Flat models, with interface-prefixed names.

---

### Task 6.2: Create InlineModelBuilder

**Files:**
- Create: `src/Generator/Builder/InlineModelBuilder.cs`

Extract logic from GenerateInline.cs into builder methods.

---

### Task 6.3: Create InlineRenderer

**Files:**
- Create: `src/Generator/Renderer/InlineRenderer.cs`

Translate GenerateInline.cs string building to CodeWriter calls.

---

### Task 6.4: Wire Up and Test Inline API

**Files:**
- Modify: `src/Generator/KnockOffGenerator.cs`

Update `GenerateInlineStubs` to use InlineModelBuilder and InlineRenderer.

---

## Phase 7: Class Stubs (Same Pattern)

### Task 7.1: Create Class Models

**Files:**
- Create: `src/Generator/Model/Class/ClassGenerationUnit.cs`
- Create: `src/Generator/Model/Class/ClassConstructorModel.cs`
- Create: `src/Generator/Model/Class/ClassPropertyModel.cs`
- Create: `src/Generator/Model/Class/ClassIndexerModel.cs`
- Create: `src/Generator/Model/Class/ClassMethodModel.cs`
- Create: `src/Generator/Model/Class/ClassEventModel.cs`

---

### Task 7.2: Create ClassModelBuilder

**Files:**
- Create: `src/Generator/Builder/ClassModelBuilder.cs`

---

### Task 7.3: Create ClassRenderer

**Files:**
- Create: `src/Generator/Renderer/ClassRenderer.cs`

---

### Task 7.4: Wire Up and Test Class Stubs

Class stubs are rendered as part of InlineRenderer. Update to use ClassModelBuilder and ClassRenderer.

---

## Phase 8: Cleanup

### Task 8.1: Delete Old Generation Files

**Files:**
- Delete or empty: `src/Generator/KnockOffGenerator.GenerateFlat.cs`
- Delete or empty: `src/Generator/KnockOffGenerator.GenerateInline.cs`
- Delete or empty: `src/Generator/KnockOffGenerator.GenerateClass.cs`

**Step 1: Remove files or empty them**

Run: `dotnet test src/KnockOff.sln`
Expected: All tests pass

**Step 2: Commit**

```bash
git add -A
git commit -m "refactor: remove old generation files"
```

---

### Task 8.2: Clean Up Helpers

**Files:**
- Modify: `src/Generator/KnockOffGenerator.Helpers.cs`

Remove helper methods that are no longer used (now in builders).

---

### Task 8.3: Final Test Run

Run: `dotnet test src/KnockOff.sln`
Expected: All tests pass

---

### Task 8.4: Final Commit

```bash
git add -A
git commit -m "refactor: complete KnockOffGenerator Code Model + Renderer refactor"
```

---

## Notes

- **Phases 2-5** (Flat API) should be done first and verified completely before moving to Inline
- **Run tests frequently** after each significant change
- **Compare generated output** when tests fail to identify differences
- The key validation is that generated code is semantically equivalent - whitespace differences are acceptable
- Helper methods in `KnockOffGenerator.Helpers.cs` and `SymbolHelpers.cs` can be reused by builders
