# KnockOffGenerator Refactor: Code Model + Renderer Pattern

## Problem Statement

The current KnockOffGenerator has several pain points:

1. **Hard to read/maintain** - GenerateFlat.cs (~1,600 lines) and GenerateInline.cs (~2,000 lines) mix decision logic with string emission
2. **Hard to add features** - Adding new functionality requires changes scattered across generation methods
3. **Hard to test** - Can't unit test generation logic in isolation
4. **Inconsistencies** - The three output patterns (flat, inline, class) have drifted apart

## Solution: Code Model + Renderer Pattern

Separate "what to generate" (model) from "how to generate it" (renderer).

```
Roslyn Symbols
    → Transform (existing, unchanged)
    → KnockOffTypeInfo / InlineStubClassInfo (existing models)
    → ModelBuilder (NEW - makes all decisions)
    → GenerationUnit (NEW - describes exactly what to emit)
    → Renderer (NEW - pure mechanical emission)
    → string (source code)
```

### Three Separate Hierarchies

Each output pattern gets its own model and renderer - no shared types between patterns:

```
FlatModelBuilder    → FlatGenerationUnit    → FlatRenderer
InlineModelBuilder  → InlineGenerationUnit  → InlineRenderer
ClassModelBuilder   → ClassGenerationUnit   → ClassRenderer
```

### Key Principle

The GenerationUnit contains **resolved, final values** - no decisions left for the Renderer:
- `InterceptorName: "Value2"` (already collision-resolved)
- `IndexerName: "IndexerString"` (already computed)
- `DefaultExpression: "new List<int>()"` (already determined)

---

## File Structure

```
src/Generator/
├── KnockOffGenerator.cs              # Entry point (simplified)
├── KnockOffGenerator.Transform.cs    # Unchanged
├── KnockOffGenerator.Helpers.cs      # Unchanged (used by builders)
│
├── Model/
│   ├── Flat/
│   │   ├── FlatGenerationUnit.cs
│   │   ├── FlatPropertyModel.cs
│   │   ├── FlatIndexerModel.cs
│   │   ├── FlatMethodModel.cs
│   │   ├── FlatGenericMethodHandlerModel.cs
│   │   └── FlatEventModel.cs
│   │
│   ├── Inline/
│   │   ├── InlineGenerationUnit.cs
│   │   ├── InlineInterfaceStubModel.cs
│   │   ├── InlinePropertyModel.cs
│   │   ├── InlineIndexerModel.cs
│   │   ├── InlineMethodModel.cs
│   │   ├── InlineGenericMethodHandlerModel.cs
│   │   ├── InlineEventModel.cs
│   │   ├── InlineDelegateStubModel.cs
│   │   └── InlineClassStubModel.cs
│   │
│   ├── Class/
│   │   ├── ClassGenerationUnit.cs
│   │   ├── ClassConstructorModel.cs
│   │   ├── ClassPropertyModel.cs
│   │   ├── ClassIndexerModel.cs
│   │   ├── ClassMethodModel.cs
│   │   └── ClassEventModel.cs
│   │
│   └── Shared/
│       ├── TypeParameterModel.cs
│       ├── ParameterModel.cs
│       └── ContainingTypeModel.cs
│
├── Builder/
│   ├── FlatModelBuilder.cs
│   ├── InlineModelBuilder.cs
│   └── ClassModelBuilder.cs
│
└── Renderer/
    ├── CodeWriter.cs
    ├── FlatRenderer.cs
    ├── InlineRenderer.cs
    └── ClassRenderer.cs
```

---

## CodeWriter Utility

Shared helper for all renderers - handles indentation and common patterns.

```csharp
// src/Generator/Renderer/CodeWriter.cs
namespace KnockOff.Renderer;

internal sealed class CodeWriter
{
    private readonly StringBuilder _sb = new();
    private int _indent = 0;

    public void Line(string text = "")
    {
        if (string.IsNullOrEmpty(text))
            _sb.AppendLine();
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

    private sealed class BlockScope(CodeWriter writer) : IDisposable
    {
        public void Dispose()
        {
            writer._indent--;
            writer.Line("}");
        }
    }
}
```

Uses tabs for indentation to match current generated output.

---

## Flat API Models

### FlatGenerationUnit

Top-level container for standalone stub generation.

```csharp
// src/Generator/Model/Flat/FlatGenerationUnit.cs
namespace KnockOff.Model.Flat;

internal sealed record FlatGenerationUnit
{
    // File-level
    public required string ClassName { get; init; }
    public required string Namespace { get; init; }
    public required EquatableArray<string> InterfaceList { get; init; }

    // Type parameters (for generic stubs)
    public required EquatableArray<TypeParameterModel> TypeParameters { get; init; }

    // All resolved members
    public required EquatableArray<FlatPropertyModel> Properties { get; init; }
    public required EquatableArray<FlatIndexerModel> Indexers { get; init; }
    public required EquatableArray<FlatMethodModel> Methods { get; init; }
    public required EquatableArray<FlatGenericMethodHandlerModel> GenericMethodHandlers { get; init; }
    public required EquatableArray<FlatEventModel> Events { get; init; }

    // Features
    public bool HasGenericMethods { get; init; }
    public bool ImplementsIKnockOffStub { get; init; }
}
```

### FlatPropertyModel

```csharp
// src/Generator/Model/Flat/FlatPropertyModel.cs
internal sealed record FlatPropertyModel
{
    // Resolved names
    public required string InterceptorName { get; init; }
    public required string InterceptorClassName { get; init; }

    // Interface implementation
    public required string DeclaringInterface { get; init; }
    public required string MemberName { get; init; }
    public required string ReturnType { get; init; }
    public required string NullableReturnType { get; init; }

    // Accessors
    public bool HasGetter { get; init; }
    public bool HasSetter { get; init; }
    public bool IsInitOnly { get; init; }

    // Resolved default
    public required string DefaultExpression { get; init; }

    // Nullability handling
    public string? SetterPragmaDisable { get; init; }
    public string? SetterPragmaRestore { get; init; }

    // Strict mode
    public required string SimpleInterfaceName { get; init; }

    // User method delegation
    public string? UserMethodName { get; init; }

    // Needs 'new' keyword
    public bool NeedsNewKeyword { get; init; }
}
```

### FlatIndexerModel

```csharp
// src/Generator/Model/Flat/FlatIndexerModel.cs
internal sealed record FlatIndexerModel
{
    public required string InterceptorName { get; init; }
    public required string InterceptorClassName { get; init; }

    public required string DeclaringInterface { get; init; }
    public required string ReturnType { get; init; }
    public required string NullableReturnType { get; init; }
    public required string DefaultExpression { get; init; }

    // Key info
    public required string KeyType { get; init; }
    public required string KeyParamName { get; init; }
    public required string NullableKeyType { get; init; }

    public bool HasGetter { get; init; }
    public bool HasSetter { get; init; }

    public required string SimpleInterfaceName { get; init; }
    public bool NeedsNewKeyword { get; init; }
}
```

### FlatMethodModel

```csharp
// src/Generator/Model/Flat/FlatMethodModel.cs
internal sealed record FlatMethodModel
{
    // Resolved names
    public required string InterceptorName { get; init; }
    public required string InterceptorClassName { get; init; }

    // Interface implementation
    public required string DeclaringInterface { get; init; }
    public required string MethodName { get; init; }
    public required string ReturnType { get; init; }
    public bool IsVoid { get; init; }

    // Parameters
    public required EquatableArray<ParameterModel> Parameters { get; init; }
    public required string ParameterDeclarations { get; init; }
    public required string ParameterNames { get; init; }
    public required string RecordCallArgs { get; init; }

    // For LastCallArg(s)
    public required EquatableArray<ParameterModel> TrackableParameters { get; init; }
    public required string? LastCallType { get; init; }

    // Delegate signature
    public required string OnCallDelegateType { get; init; }
    public bool NeedsCustomDelegate { get; init; }
    public string? CustomDelegateName { get; init; }
    public string? CustomDelegateSignature { get; init; }

    // Default behavior
    public required string DefaultExpression { get; init; }
    public bool ThrowsOnDefault { get; init; }

    // User method
    public string? UserMethodCall { get; init; }

    // Strict mode
    public required string SimpleInterfaceName { get; init; }

    // Type parameters
    public required string TypeParameterDecl { get; init; }
    public required string TypeParameterList { get; init; }
    public required string ConstraintClauses { get; init; }

    public bool NeedsNewKeyword { get; init; }
}
```

### FlatGenericMethodHandlerModel

For generic methods using the `Of<T>()` dictionary pattern.

```csharp
// src/Generator/Model/Flat/FlatGenericMethodHandlerModel.cs
internal sealed record FlatGenericMethodHandlerModel
{
    public required string InterceptorName { get; init; }
    public required string InterceptorClassName { get; init; }
    public required string MethodName { get; init; }

    // Type parameters
    public required string TypeParameterNames { get; init; }
    public required string KeyType { get; init; }
    public required string KeyConstruction { get; init; }
    public required string ConstraintClauses { get; init; }

    // Typed handler inner class
    public required string TypedHandlerClassName { get; init; }
    public required string DelegateSignature { get; init; }
    public required EquatableArray<ParameterModel> NonGenericParams { get; init; }
    public required string? LastCallType { get; init; }

    public bool IsVoid { get; init; }
    public required string ReturnType { get; init; }

    public bool NeedsNewKeyword { get; init; }
}
```

### FlatEventModel

```csharp
// src/Generator/Model/Flat/FlatEventModel.cs
internal sealed record FlatEventModel
{
    public required string InterceptorName { get; init; }
    public required string InterceptorClassName { get; init; }

    public required string DeclaringInterface { get; init; }
    public required string EventName { get; init; }
    public required string DelegateType { get; init; }

    // Raise method (pre-computed)
    public required string RaiseParameters { get; init; }
    public required string RaiseArguments { get; init; }
    public required string RaiseReturnType { get; init; }
    public bool RaiseReturnsValue { get; init; }
    public bool UsesDynamicInvoke { get; init; }

    public bool NeedsNewKeyword { get; init; }
}
```

---

## Inline API Models

### InlineGenerationUnit

```csharp
// src/Generator/Model/Inline/InlineGenerationUnit.cs
namespace KnockOff.Model.Inline;

internal sealed record InlineGenerationUnit
{
    public required string ClassName { get; init; }
    public required string Namespace { get; init; }

    // Containing types (for nested classes)
    public required EquatableArray<ContainingTypeModel> ContainingTypes { get; init; }

    // What to generate inside Stubs class
    public required EquatableArray<InlineInterfaceStubModel> InterfaceStubs { get; init; }
    public required EquatableArray<InlineDelegateStubModel> DelegateStubs { get; init; }
    public required EquatableArray<InlineClassStubModel> ClassStubs { get; init; }

    // Partial properties (outside Stubs, inside partial class)
    public required EquatableArray<PartialPropertyModel> PartialProperties { get; init; }

    public bool HasGenericMethods { get; init; }
}
```

### InlineInterfaceStubModel

```csharp
// src/Generator/Model/Inline/InlineInterfaceStubModel.cs
internal sealed record InlineInterfaceStubModel
{
    public required string StubClassName { get; init; }
    public required string InterfaceFullName { get; init; }

    // Type parameters
    public required EquatableArray<TypeParameterModel> TypeParameters { get; init; }
    public required string TypeParameterList { get; init; }
    public required string ConstraintClause { get; init; }

    // Members (with interface prefix: IFoo_Value)
    public required EquatableArray<InlinePropertyModel> Properties { get; init; }
    public required EquatableArray<InlineIndexerModel> Indexers { get; init; }
    public required EquatableArray<InlineMethodModel> Methods { get; init; }
    public required EquatableArray<InlineGenericMethodHandlerModel> GenericMethodHandlers { get; init; }
    public required EquatableArray<InlineEventModel> Events { get; init; }
}
```

The inline member models (`InlinePropertyModel`, etc.) mirror flat models but with interface-prefixed names: `IFoo_ValueInterceptor`.

### Supporting Models

```csharp
// src/Generator/Model/Inline/InlineDelegateStubModel.cs
internal sealed record InlineDelegateStubModel
{
    public required string StubClassName { get; init; }
    public required string DelegateFullName { get; init; }
    public required string ReturnType { get; init; }
    public required EquatableArray<ParameterModel> Parameters { get; init; }
    public required string ParameterDeclarations { get; init; }
    public required string ParameterNames { get; init; }
    public bool IsVoid { get; init; }
}

// src/Generator/Model/Inline/PartialPropertyModel.cs
internal sealed record PartialPropertyModel
{
    public required string PropertyName { get; init; }
    public required string StubTypeName { get; init; }
    public required string AccessModifier { get; init; }
    public required string BackingFieldName { get; init; }
}

// src/Generator/Model/Shared/ContainingTypeModel.cs
internal sealed record ContainingTypeModel
{
    public required string Keyword { get; init; }
    public required string Name { get; init; }
    public required string AccessModifier { get; init; }
}
```

---

## Class Stub Models

### ClassGenerationUnit

Class stubs use composition (wrapper + nested Impl).

```csharp
// src/Generator/Model/Class/ClassGenerationUnit.cs
namespace KnockOff.Model.Class;

internal sealed record ClassGenerationUnit
{
    public required string StubClassName { get; init; }
    public required string TargetClassFullName { get; init; }

    // Type parameters
    public required EquatableArray<TypeParameterModel> TypeParameters { get; init; }
    public required string TypeParameterList { get; init; }
    public required string ConstraintClause { get; init; }

    // Constructors
    public required EquatableArray<ClassConstructorModel> Constructors { get; init; }

    // Members
    public required EquatableArray<ClassPropertyModel> Properties { get; init; }
    public required EquatableArray<ClassIndexerModel> Indexers { get; init; }
    public required EquatableArray<ClassMethodModel> Methods { get; init; }
    public required EquatableArray<ClassEventModel> Events { get; init; }

    // Required member handling
    public bool HasRequiredMembers { get; init; }
    public required EquatableArray<string> RequiredMemberInitializers { get; init; }
}
```

### ClassConstructorModel

```csharp
// src/Generator/Model/Class/ClassConstructorModel.cs
internal sealed record ClassConstructorModel
{
    public required string ParameterDeclarations { get; init; }
    public required string ParameterNames { get; init; }
    public required string ImplParameterDeclarations { get; init; }
    public required string BaseCallArgs { get; init; }
}
```

### ClassPropertyModel

```csharp
// src/Generator/Model/Class/ClassPropertyModel.cs
internal sealed record ClassPropertyModel
{
    public required string InterceptorName { get; init; }
    public required string InterceptorClassName { get; init; }

    public required string PropertyName { get; init; }
    public required string ReturnType { get; init; }
    public required string NullableReturnType { get; init; }
    public required string AccessModifier { get; init; }

    public bool HasGetter { get; init; }
    public bool HasSetter { get; init; }
    public bool IsInitOnly { get; init; }
    public bool IsAbstract { get; init; }
    public bool IsRequired { get; init; }

    public required string DefaultExpression { get; init; }
}
```

### ClassMethodModel

```csharp
// src/Generator/Model/Class/ClassMethodModel.cs
internal sealed record ClassMethodModel
{
    public required string InterceptorName { get; init; }
    public required string InterceptorClassName { get; init; }

    public required string MethodName { get; init; }
    public required string ReturnType { get; init; }
    public required string AccessModifier { get; init; }
    public bool IsVoid { get; init; }
    public bool IsAbstract { get; init; }

    // Async handling
    public bool IsTask { get; init; }
    public bool IsValueTask { get; init; }
    public required string DefaultReturn { get; init; }

    public required EquatableArray<ParameterModel> Parameters { get; init; }
    public required EquatableArray<ParameterModel> TrackableParameters { get; init; }
    public required string ParameterDeclarations { get; init; }
    public required string ArgumentList { get; init; }
    public required string RecordCallArgs { get; init; }
    public required string? LastCallType { get; init; }
    public required string OnCallDelegateType { get; init; }
}
```

---

## Shared Models

```csharp
// src/Generator/Model/Shared/TypeParameterModel.cs
namespace KnockOff.Model.Shared;

internal sealed record TypeParameterModel
{
    public required string Name { get; init; }
    public required string Constraints { get; init; }
}

// src/Generator/Model/Shared/ParameterModel.cs
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

---

## Builders

### FlatModelBuilder

```csharp
// src/Generator/Builder/FlatModelBuilder.cs
namespace KnockOff.Builder;

internal static class FlatModelBuilder
{
    public static FlatGenerationUnit Build(KnockOffTypeInfo typeInfo)
    {
        // Step 1: Build the name map (collision resolution)
        var nameMap = BuildNameMap(typeInfo.FlatMembers, typeInfo.FlatEvents, typeInfo.UserMethods);

        // Step 2: Group methods and split mixed groups
        var methodGroups = GroupAndSplitMethods(typeInfo.FlatMembers);

        // Step 3: Count indexers for naming strategy
        var indexerCount = CountIndexers(typeInfo.FlatMembers);

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
            HasGenericMethods = methodGroups.Any(g => g.HasGenericOverloads),
            ImplementsIKnockOffStub = true
        };
    }

    // Helper methods moved from GenerateFlat.cs
    private static Dictionary<string, string> BuildNameMap(...) { ... }
    private static FlatPropertyModel BuildPropertyModel(...) { ... }
    private static FlatIndexerModel BuildIndexerModel(...) { ... }
    private static FlatMethodModel BuildMethodModel(...) { ... }
    private static FlatEventModel BuildEventModel(...) { ... }
    // etc.
}
```

### InlineModelBuilder

```csharp
// src/Generator/Builder/InlineModelBuilder.cs
namespace KnockOff.Builder;

internal static class InlineModelBuilder
{
    public static InlineGenerationUnit Build(InlineStubClassInfo info)
    {
        var interfaceStubs = new List<InlineInterfaceStubModel>();

        foreach (var iface in info.Interfaces)
        {
            var methodGroups = GroupMethodsByName(iface.Members);
            var indexerCount = CountIndexers(iface.Members);

            // Deduplicate members
            var deduplicatedProperties = DeduplicateProperties(iface.Members, indexerCount);
            var deduplicatedEvents = DeduplicateEvents(iface.Events);

            interfaceStubs.Add(new InlineInterfaceStubModel
            {
                StubClassName = iface.StubClassName,
                InterfaceFullName = iface.FullName,
                TypeParameters = BuildTypeParameters(iface.TypeParameters),
                TypeParameterList = FormatTypeParameterList(iface.TypeParameters),
                ConstraintClause = FormatConstraintClause(iface.TypeParameters),
                Properties = BuildPropertyModels(deduplicatedProperties, iface),
                Indexers = BuildIndexerModels(deduplicatedProperties, iface, indexerCount),
                Methods = BuildMethodModels(methodGroups, iface),
                GenericMethodHandlers = BuildGenericMethodHandlers(methodGroups, iface),
                Events = BuildEventModels(deduplicatedEvents, iface)
            });
        }

        return new InlineGenerationUnit
        {
            ClassName = info.ClassName,
            Namespace = info.Namespace,
            ContainingTypes = BuildContainingTypes(info.ContainingTypes),
            InterfaceStubs = interfaceStubs.ToEquatableArray(),
            DelegateStubs = BuildDelegateStubs(info.Delegates),
            ClassStubs = BuildClassStubs(info.Classes),
            PartialProperties = BuildPartialProperties(info.PartialProperties),
            HasGenericMethods = interfaceStubs.Any(s => s.GenericMethodHandlers.Count > 0)
        };
    }
}
```

### ClassModelBuilder

```csharp
// src/Generator/Builder/ClassModelBuilder.cs
namespace KnockOff.Builder;

internal static class ClassModelBuilder
{
    public static ClassGenerationUnit Build(ClassStubInfo cls)
    {
        var methodGroups = GroupClassMethodsByName(cls.Members);
        var indexerCount = CountClassIndexers(cls.Members);
        var requiredMembers = cls.Members.Where(m => m.IsRequired).ToList();

        return new ClassGenerationUnit
        {
            StubClassName = cls.Name,
            TargetClassFullName = cls.FullName,
            TypeParameters = BuildTypeParameters(cls.TypeParameters),
            TypeParameterList = FormatTypeParameterList(cls.TypeParameters),
            ConstraintClause = FormatConstraintClause(cls.TypeParameters),
            Constructors = BuildConstructorModels(cls.Constructors, cls.Name),
            Properties = BuildPropertyModels(cls.Members, cls.Name, indexerCount),
            Indexers = BuildIndexerModels(cls.Members, cls.Name, indexerCount),
            Methods = BuildMethodModels(methodGroups, cls.Name),
            Events = BuildEventModels(cls.Events, cls.Name),
            HasRequiredMembers = requiredMembers.Count > 0,
            RequiredMemberInitializers = requiredMembers
                .Select(m => $"{m.Name} = default!;")
                .ToEquatableArray()
        };
    }
}
```

---

## Renderers

### FlatRenderer

```csharp
// src/Generator/Renderer/FlatRenderer.cs
namespace KnockOff.Renderer;

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
        var typeParams = FormatTypeParams(unit.TypeParameters);
        using (w.Block($"partial class {unit.ClassName}{typeParams} : {interfaces}, global::KnockOff.IKnockOffStub"))
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

            // Strict property and Object property
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

    // Private render methods - pure emission, no decisions
    private static void RenderPropertyInterceptorClass(CodeWriter w, FlatPropertyModel prop, string className)
    {
        w.Line($"/// <summary>Tracks and configures behavior for {prop.MemberName}.</summary>");
        var newKeyword = prop.NeedsNewKeyword ? "new " : "";
        using (w.Block($"public {newKeyword}sealed class {prop.InterceptorClassName}"))
        {
            if (prop.HasGetter)
            {
                w.Line("/// <summary>Number of times the getter was accessed.</summary>");
                w.Line("public int GetCount { get; private set; }");
                w.Line();
                w.Line($"/// <summary>Callback invoked when the getter is accessed.</summary>");
                w.Line($"public global::System.Func<{className}, {prop.ReturnType}>? OnGet {{ get; set; }}");
                w.Line();
            }

            if (prop.HasSetter)
            {
                w.Line("/// <summary>Number of times the setter was accessed.</summary>");
                w.Line("public int SetCount { get; private set; }");
                w.Line();
                w.Line($"/// <summary>The last value passed to the setter.</summary>");
                w.Line($"public {prop.NullableReturnType} LastSetValue {{ get; private set; }}");
                w.Line();
                w.Line($"/// <summary>Callback invoked when the setter is accessed.</summary>");
                w.Line($"public global::System.Action<{className}, {prop.ReturnType}>? OnSet {{ get; set; }}");
                w.Line();
            }

            w.Line($"/// <summary>Value returned by getter when OnGet is not set.</summary>");
            w.Line($"public {prop.ReturnType} Value {{ get; set; }} = {prop.DefaultExpression};");
            w.Line();

            // RecordGet/RecordSet methods
            if (prop.HasGetter)
            {
                w.Line("/// <summary>Records a getter access.</summary>");
                w.Line("public void RecordGet() => GetCount++;");
                w.Line();
            }

            if (prop.HasSetter)
            {
                w.Line("/// <summary>Records a setter access.</summary>");
                w.Line($"public void RecordSet({prop.NullableReturnType} value) {{ SetCount++; LastSetValue = value; }}");
                w.Line();
            }

            // Reset method
            RenderResetMethod(w, prop);
        }
        w.Line();
    }

    private static void RenderPropertyImplementation(CodeWriter w, FlatPropertyModel prop)
    {
        w.Line($"{prop.ReturnType} {prop.DeclaringInterface}.{prop.MemberName}");
        using (w.Braces())
        {
            if (prop.HasGetter)
            {
                w.Line($"get {{ {prop.InterceptorName}.RecordGet(); if ({prop.InterceptorName}.OnGet is {{ }} onGet) return onGet(this); if (Strict) throw global::KnockOff.StubException.NotConfigured(\"{prop.SimpleInterfaceName}\", \"{prop.MemberName}\"); return {prop.InterceptorName}.Value; }}");
            }

            if (prop.HasSetter)
            {
                var setterKeyword = prop.IsInitOnly ? "init" : "set";
                if (prop.SetterPragmaDisable != null)
                    w.Line(prop.SetterPragmaDisable);
                w.Line($"{setterKeyword} {{ {prop.InterceptorName}.RecordSet(value); if ({prop.InterceptorName}.OnSet is {{ }} onSet) {{ onSet(this, value); return; }} if (Strict) throw global::KnockOff.StubException.NotConfigured(\"{prop.SimpleInterfaceName}\", \"{prop.MemberName}\"); {prop.InterceptorName}.Value = value; }}");
                if (prop.SetterPragmaRestore != null)
                    w.Line(prop.SetterPragmaRestore);
            }
        }
        w.Line();
    }

    // Additional render methods...
}
```

### InlineRenderer

```csharp
// src/Generator/Renderer/InlineRenderer.cs
namespace KnockOff.Renderer;

internal static class InlineRenderer
{
    public static string Render(InlineGenerationUnit unit)
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

        // Open containing types
        foreach (var ct in unit.ContainingTypes)
        {
            var accessMod = string.IsNullOrEmpty(ct.AccessModifier) ? "" : ct.AccessModifier + " ";
            w.Line($"{accessMod}partial {ct.Keyword} {ct.Name}");
            w.Line("{");
        }

        using (w.Block($"partial class {unit.ClassName}"))
        {
            w.Line("/// <summary>Contains stub implementations for inline stub pattern.</summary>");
            using (w.Block("public static class Stubs"))
            {
                if (unit.HasGenericMethods)
                {
                    w.Line("private interface IGenericMethodCallTracker { int CallCount { get; } bool WasCalled { get; } }");
                    w.Line("private interface IResettable { void Reset(); }");
                    w.Line();
                }

                foreach (var stub in unit.InterfaceStubs)
                    RenderInterfaceStub(w, stub);

                foreach (var del in unit.DelegateStubs)
                    RenderDelegateStub(w, del);

                foreach (var cls in unit.ClassStubs)
                    ClassRenderer.RenderAsNestedStub(w, cls);
            }

            // Partial properties
            foreach (var prop in unit.PartialProperties)
            {
                w.Line();
                w.Line($"private readonly Stubs.{prop.StubTypeName} {prop.BackingFieldName} = new();");
                var accessMod = string.IsNullOrEmpty(prop.AccessModifier) ? "" : $"{prop.AccessModifier} ";
                w.Line($"/// <summary>Auto-instantiated stub for {prop.StubTypeName}.</summary>");
                w.Line($"{accessMod}partial Stubs.{prop.StubTypeName} {prop.PropertyName} {{ get => {prop.BackingFieldName}; }}");
            }
        }

        // Close containing types
        for (int i = 0; i < unit.ContainingTypes.Count; i++)
            w.Line("}");

        return w.ToString();
    }

    private static void RenderInterfaceStub(CodeWriter w, InlineInterfaceStubModel stub)
    {
        // Render interceptor classes
        foreach (var prop in stub.Properties)
            RenderPropertyInterceptorClass(w, prop, stub.StubClassName, stub.TypeParameterList);

        // ... similar for indexers, methods, events

        // Render stub class
        var baseType = stub.InterfaceFullName;
        w.Line($"/// <summary>Stub implementation of {baseType}.</summary>");
        using (w.Block($"public class {stub.StubClassName}{stub.TypeParameterList} : {baseType}, global::KnockOff.IKnockOffStub{stub.ConstraintClause}"))
        {
            // Interceptor properties
            foreach (var prop in stub.Properties)
            {
                w.Line($"/// <summary>Interceptor for {prop.MemberName}.</summary>");
                w.Line($"public {stub.StubClassName}_{prop.InterceptorName}Interceptor{stub.TypeParameterList} {prop.InterceptorName} {{ get; }} = new();");
                w.Line();
            }

            // ... similar for other members

            // Standard members
            w.Line("/// <summary>When true, unconfigured calls throw StubException.</summary>");
            w.Line("public bool Strict { get; set; }");
            w.Line();

            // Explicit implementations
            // ...
        }
        w.Line();
    }
}
```

### ClassRenderer

```csharp
// src/Generator/Renderer/ClassRenderer.cs
namespace KnockOff.Renderer;

internal static class ClassRenderer
{
    public static void RenderAsNestedStub(CodeWriter w, ClassGenerationUnit cls)
    {
        // Render interceptor classes
        foreach (var prop in cls.Properties)
            RenderPropertyInterceptorClass(w, prop, cls.StubClassName);

        foreach (var method in cls.Methods)
            RenderMethodInterceptorClass(w, method, cls.StubClassName);

        foreach (var evt in cls.Events)
            RenderEventInterceptorClass(w, evt, cls.StubClassName);

        // Render wrapper class
        w.Line($"/// <summary>Stub for {cls.TargetClassFullName} via composition.</summary>");
        using (w.Block($"public class {cls.StubClassName}{cls.TypeParameterList} : global::KnockOff.IKnockOffStub{cls.ConstraintClause}"))
        {
            w.Line("/// <summary>When true, unconfigured calls throw StubException.</summary>");
            w.Line("public bool Strict { get; set; }");
            w.Line();

            // Interceptor properties
            foreach (var prop in cls.Properties)
            {
                w.Line($"/// <summary>Interceptor for {prop.PropertyName}.</summary>");
                w.Line($"public {cls.StubClassName}_{prop.InterceptorName}Interceptor {prop.InterceptorName} {{ get; }} = new();");
            }

            // ... methods, events

            w.Line();
            w.Line($"/// <summary>The {cls.TargetClassFullName} instance.</summary>");
            w.Line($"public {cls.TargetClassFullName} Object {{ get; }}");
            w.Line();

            // Constructors
            foreach (var ctor in cls.Constructors)
                RenderWrapperConstructor(w, ctor, cls.StubClassName);

            // ResetInterceptors
            RenderResetInterceptors(w, cls);

            // Nested Impl class
            RenderImplClass(w, cls);
        }
        w.Line();
    }

    private static void RenderImplClass(CodeWriter w, ClassGenerationUnit cls)
    {
        if (cls.HasRequiredMembers)
            w.Line("#pragma warning disable CS8618");

        w.Line($"/// <summary>Internal implementation that inherits from {cls.TargetClassFullName}.</summary>");
        using (w.Block($"private sealed class Impl : {cls.TargetClassFullName}{cls.ConstraintClause}"))
        {
            w.Line($"private readonly {cls.StubClassName}{cls.TypeParameterList} _stub;");
            w.Line();

            // Constructors
            foreach (var ctor in cls.Constructors)
                RenderImplConstructor(w, ctor, cls);

            // Overrides
            foreach (var prop in cls.Properties)
                RenderPropertyOverride(w, prop);

            foreach (var method in cls.Methods)
                RenderMethodOverride(w, method);

            foreach (var evt in cls.Events)
                RenderEventOverride(w, evt);
        }

        if (cls.HasRequiredMembers)
            w.Line("#pragma warning restore CS8618");
    }
}
```

---

## Entry Point Changes

The main generator becomes much simpler:

```csharp
// src/Generator/KnockOffGenerator.cs (simplified)
[Generator]
public partial class KnockOffGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Pipeline 1: Standalone stubs (flat API)
        var standaloneStubs = context.SyntaxProvider
            .ForAttributeWithMetadataName(...)
            .Select(TransformClass);

        context.RegisterSourceOutput(standaloneStubs, GenerateFlatStub);

        // Pipeline 2: Inline stubs (generic attribute)
        var inlineStubs = context.SyntaxProvider
            .ForAttributeWithMetadataName(...)
            .Select(TransformInlineStubClass);

        context.RegisterSourceOutput(inlineStubs, GenerateInlineStubs);

        // Pipeline 3: Typeof stubs (open generics)
        // ... similar
    }

    private static void GenerateFlatStub(SourceProductionContext context, KnockOffTypeInfo typeInfo)
    {
        // Report diagnostics
        foreach (var diag in typeInfo.Diagnostics)
            ReportDiagnostic(context, diag);

        if (HasBlockingDiagnostics(typeInfo.Diagnostics))
            return;

        // Build model and render
        var unit = FlatModelBuilder.Build(typeInfo);
        var source = FlatRenderer.Render(unit);

        context.AddSource($"{typeInfo.Name}.g.cs", source);
    }

    private static void GenerateInlineStubs(SourceProductionContext context, InlineStubClassInfo info)
    {
        ReportDiagnostics(context, info.Diagnostics);

        if (HasBlockingDiagnostics(info.Diagnostics))
            return;

        var unit = InlineModelBuilder.Build(info);
        var source = InlineRenderer.Render(unit);

        var hintName = BuildHintName(info);
        context.AddSource($"{hintName}.Stubs.g.cs", source);
    }
}
```

---

## Decisions Moved to Builders

All decision logic moves from Generate*.cs to the builders:

### Name Resolution & Disambiguation
1. Name collision resolution (`BuildNameMap`)
2. Indexer naming (single vs multiple)
3. Type suffix computation
4. `new` keyword detection
5. Identifier escaping

### Method & Overload Handling
6. Method grouping
7. Mixed group splitting (generic/non-generic)
8. Combined parameter computation
9. User method matching
10. Generic parameter filtering

### Type System Decisions
11. Default value strategy
12. Nullability handling
13. Type parameter constraints
14. Generic handler structure (single vs `Of<T>()`)

### Output Structure Decisions
15. Strict mode injection points
16. Abstract vs virtual handling
17. Required member handling
18. Blocking diagnostic checks
19. Member deduplication

### Formatting Decisions
20. Parameter ref/out/in formatting
21. Setter nullability pragmas
22. Task/ValueTask detection

---

## Validation Strategy

**Existing tests pass without modification** (except trivial whitespace differences).

The generated code must be semantically equivalent. If all tests pass, the refactor is successful.

---

## Implementation Phases

### Phase 1: Foundation
- [ ] Create `Renderer/CodeWriter.cs`
- [ ] Create shared models (`Model/Shared/*.cs`)
- [ ] Verify build succeeds

### Phase 2: Flat API
- [ ] Create `Model/Flat/*.cs`
- [ ] Create `Builder/FlatModelBuilder.cs` (extract from GenerateFlat.cs)
- [ ] Create `Renderer/FlatRenderer.cs`
- [ ] Wire up in `KnockOffGenerator.cs`
- [ ] Run tests, fix differences
- [ ] Delete old `GenerateFlat.cs` code

### Phase 3: Inline API
- [ ] Create `Model/Inline/*.cs`
- [ ] Create `Builder/InlineModelBuilder.cs`
- [ ] Create `Renderer/InlineRenderer.cs`
- [ ] Wire up in `KnockOffGenerator.cs`
- [ ] Run tests, fix differences
- [ ] Delete old `GenerateInline.cs` code

### Phase 4: Class Stubs
- [ ] Create `Model/Class/*.cs`
- [ ] Create `Builder/ClassModelBuilder.cs`
- [ ] Create `Renderer/ClassRenderer.cs`
- [ ] Wire up (already part of InlineRenderer)
- [ ] Run tests, fix differences
- [ ] Delete old `GenerateClass.cs` code

### Phase 5: Cleanup
- [ ] Remove unused helper methods
- [ ] Final test run
- [ ] Update any documentation

---

## What Gets Deleted

After refactoring is complete:
- `KnockOffGenerator.GenerateFlat.cs` (entire file)
- `KnockOffGenerator.GenerateInline.cs` (entire file)
- `KnockOffGenerator.GenerateClass.cs` (entire file)
- Redundant helper methods in `KnockOffGenerator.Helpers.cs`

Estimated deletion: ~4,400 lines of mixed model/rendering code, replaced with cleanly separated model + renderer code.
