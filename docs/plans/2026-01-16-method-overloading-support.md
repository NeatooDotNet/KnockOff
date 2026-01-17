# Method Overloading Support Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Fix method overloading support so that overloads within a single interface work via compiler overload resolution on OnCall(), while blocking same-signature methods from different interfaces.

**Architecture:** Each interceptor class supports multiple method signatures. The generator groups methods by InterceptorName, then generates multiple delegates, OnCall overloads, and Invoke methods within a single interceptor class. KO0100 only fires when identical signatures appear across different interfaces.

**Tech Stack:** C#, Roslyn Source Generator, xUnit

---

## Context

The current implementation incorrectly:
1. Fires KO0100 for ANY methods with the same name (including legitimate overloads within one interface)
2. Generates one delegate/OnCall/Invoke per interceptor (can't handle multiple signatures)

**What should work:**
- `IFoo.Add(int)` and `IFoo.Add(string)` - same interface, different signatures ✅
- `IFoo.Add(int)` and `IBar.Add(string)` - different interfaces, different signatures ✅

**What should be blocked (KO0100):**
- `IFoo.Add(int)` and `IBar.Add(int)` - different interfaces, SAME signature ❌

---

## Task 1: Fix KO0100 Diagnostic Logic

**Files:**
- Modify: `src/Generator/KnockOffGenerator.Transform.cs:786-819`
- Modify: `src/Generator/KnockOffGenerator.cs:56-62` (update message format)

**Step 1: Update the diagnostic message in KnockOffGenerator.cs**

Change line 59 from:
```csharp
messageFormat: "Method '{0}' has overloads with different signatures. KnockOff does not support method overloading. Define separate interfaces or use distinct method names.",
```

To:
```csharp
messageFormat: "Method '{0}' has identical signature in multiple interfaces: {1}. KnockOff cannot distinguish which interface's method was called.",
```

**Step 2: Fix detection logic in KnockOffGenerator.Transform.cs**

Replace lines 786-819 with:

```csharp
// Detect cross-interface signature conflicts (KO0100)
// Same method name + same parameter types from DIFFERENT interfaces = conflict
var methods = flatMembers.Where(m => !m.IsProperty && !m.IsIndexer).ToList();
var conflictGroups = methods
    .GroupBy(m => GetMethodSignature(m))  // Group by name + param types (not return type)
    .Where(g => g.Count() > 1)  // Multiple methods with same signature
    .Where(g => g.Select(m => m.DeclaringInterfaceFullName).Distinct().Count() > 1)  // From DIFFERENT interfaces
    .ToList();

foreach (var conflict in conflictGroups)
{
    var interfaces = string.Join(", ", conflict.Select(m => ExtractSimpleTypeName(m.DeclaringInterfaceFullName)).Distinct());
    var location = classDeclaration.Identifier.GetLocation();
    var lineSpan = location.GetLineSpan();
    diagnostics.Add(new DiagnosticInfo(
        "KO0100",
        filePath,
        lineSpan.StartLinePosition.Line,
        lineSpan.StartLinePosition.Character,
        new[] { conflict.Key, interfaces }));
}

// If there are blocking diagnostics (KO0100), return early - code generation will be skipped
if (diagnostics.Any(d => d.Id == "KO0100"))
{
    return new KnockOffTypeInfo(
        Namespace: namespaceName,
        ClassName: classSymbol.Name,
        ContainingTypes: containingTypes,
        TypeParameters: classTypeParameters,
        Interfaces: new EquatableArray<InterfaceInfo>(Array.Empty<InterfaceInfo>()),
        UserMethods: new EquatableArray<UserMethodInfo>(Array.Empty<UserMethodInfo>()),
        Diagnostics: new EquatableArray<DiagnosticInfo>(diagnostics.ToArray()),
        FlatMembers: new EquatableArray<InterfaceMemberInfo>(Array.Empty<InterfaceMemberInfo>()),
        FlatEvents: new EquatableArray<EventMemberInfo>(Array.Empty<EventMemberInfo>()),
        Strict: strict);
}
```

**Step 3: Add helper method GetMethodSignature**

Add this method near the existing `GetMemberKey` method (around line 867):

```csharp
/// <summary>
/// Gets method signature for conflict detection: name + parameter types (not return type).
/// </summary>
private static string GetMethodSignature(InterfaceMemberInfo member)
{
    var paramTypes = string.Join(",", member.Parameters.Select(p => p.Type));
    return $"{member.Name}({paramTypes})";
}
```

**Step 4: Build and verify**

Run: `dotnet build src/KnockOff.sln --no-incremental 2>&1 | head -50`
Expected: Generator compiles successfully

**Step 5: Commit**

```bash
git add src/Generator/KnockOffGenerator.cs src/Generator/KnockOffGenerator.Transform.cs
git commit -m "fix(KO0100): only fire for identical signatures across different interfaces"
```

---

## Task 2: Restore IOverloadedService in Sandbox

**Files:**
- Modify: `src/Tests/KnockOffSandbox/NeatooStubs.cs`

**Step 1: Restore the interface and stub**

Replace lines 24-36 with:

```csharp
// Test interface with method overloads (different parameter counts)
public interface IOverloadedService
{
	string Format(string input);
	string Format(string input, bool uppercase);
	string Format(string input, int maxLength);
}

[KnockOff]
public partial class OutParameterServiceKnockOff : IOutParameterService
{
}

[KnockOff]
public partial class RefParameterServiceKnockOff : IRefParameterService
{
}

[KnockOff]
public partial class OverloadedServiceKnockOff : IOverloadedService
{
}
```

**Step 2: Build to verify diagnostic no longer fires**

Run: `dotnet build src/Tests/KnockOffSandbox/KnockOffSandbox.csproj --no-incremental 2>&1 | head -30`
Expected: Build should progress (may still have other errors from old API usage, but no KO0100)

**Step 3: Commit**

```bash
git add src/Tests/KnockOffSandbox/NeatooStubs.cs
git commit -m "test: restore IOverloadedService - method overloading is supported"
```

---

## Task 3: Create FlatMethodGroup Model

**Files:**
- Create: `src/Generator/Model/Flat/FlatMethodGroup.cs`

**Step 1: Create the model**

```csharp
// src/Generator/Model/Flat/FlatMethodGroup.cs
#nullable enable
using KnockOff;

namespace KnockOff.Model.Flat;

/// <summary>
/// Groups multiple method overloads that share the same interceptor.
/// Used for generating interceptor classes with multiple OnCall overloads.
/// </summary>
internal sealed record FlatMethodGroup(
    string InterceptorName,
    string InterceptorClassName,
    bool NeedsNewKeyword,
    EquatableArray<FlatMethodModel> Methods);
```

**Step 2: Commit**

```bash
git add src/Generator/Model/Flat/FlatMethodGroup.cs
git commit -m "feat: add FlatMethodGroup for grouping method overloads"
```

---

## Task 4: Update FlatModelBuilder to Group Methods

**Files:**
- Modify: `src/Generator/Builder/FlatModelBuilder.cs`
- Modify: `src/Generator/Model/Flat/FlatGenerationUnit.cs`

**Step 1: Add MethodGroups to FlatGenerationUnit**

In `FlatGenerationUnit.cs`, add a new property after `Methods`:

```csharp
/// <summary>Method groups for interceptor generation (groups overloads by name).</summary>
EquatableArray<FlatMethodGroup> MethodGroups,
```

**Step 2: Update FlatModelBuilder.Build to create method groups**

After the existing method creation loop (after line ~100 where methods are added), add:

```csharp
// Group non-generic methods by interceptor name for multi-overload support
var methodGroups = methods
    .Where(m => !m.IsGenericMethod && m.UserMethodCall == null)
    .GroupBy(m => m.InterceptorName)
    .Select(g => new FlatMethodGroup(
        InterceptorName: g.Key,
        InterceptorClassName: g.First().InterceptorClassName,
        NeedsNewKeyword: g.Any(m => m.NeedsNewKeyword),
        Methods: new EquatableArray<FlatMethodModel>(g.ToArray())))
    .ToList();
```

**Step 3: Pass methodGroups to FlatGenerationUnit constructor**

Update the return statement to include:
```csharp
MethodGroups: new EquatableArray<FlatMethodGroup>(methodGroups.ToArray()),
```

**Step 4: Build to verify**

Run: `dotnet build src/Generator/Generator.csproj --no-incremental`
Expected: PASS

**Step 5: Commit**

```bash
git add src/Generator/Builder/FlatModelBuilder.cs src/Generator/Model/Flat/FlatGenerationUnit.cs src/Generator/Model/Flat/FlatMethodGroup.cs
git commit -m "feat: group method overloads by interceptor name in FlatModelBuilder"
```

---

## Task 5: Update FlatRenderer to Generate Multi-Overload Interceptors

**Files:**
- Modify: `src/Generator/Renderer/FlatRenderer.cs`

This is the largest task. The renderer must generate interceptor classes with multiple:
- Delegates (one per unique signature)
- Sequences (one per signature)
- OnCall overloads (compiler resolves based on delegate)
- Invoke methods (called by explicit interface implementations)

**Step 1: Update the render loop to use MethodGroups**

Replace lines 86-92:
```csharp
// Only render interceptor classes for non-generic methods
// Generic methods use the handler classes with Of<T>() pattern
foreach (var method in unit.Methods.Where(m => !m.IsGenericMethod))
{
    if (renderedInterceptorClasses.Add(method.InterceptorClassName))
        RenderMethodInterceptorClass(w, method, classNameWithTypeParams);
}
```

With:
```csharp
// Render method interceptor classes using groups (handles overloads)
foreach (var group in unit.MethodGroups)
{
    if (renderedInterceptorClasses.Add(group.InterceptorClassName))
        RenderMethodGroupInterceptorClass(w, group, classNameWithTypeParams);
}
```

**Step 2: Create RenderMethodGroupInterceptorClass method**

Add new method after RenderMethodInterceptorClass:

```csharp
private static void RenderMethodGroupInterceptorClass(CodeWriter w, FlatMethodGroup group, string className)
{
    // If only one method in group, use simpler single-method pattern
    if (group.Methods.Count == 1)
    {
        RenderMethodInterceptorClass(w, group.Methods.GetArray()![0], className);
        return;
    }

    var firstMethod = group.Methods.GetArray()![0];
    w.Line($"/// <summary>Tracks and configures behavior for {firstMethod.MethodName} (overloaded).</summary>");
    using (w.Block($"public sealed class {group.InterceptorClassName}"))
    {
        // Generate delegates and sequences for each overload
        var index = 0;
        foreach (var method in group.Methods)
        {
            var suffix = GetSignatureSuffix(method);

            // Delegate
            w.Line($"/// <summary>Delegate for {method.MethodName}({GetParamTypeList(method)}).</summary>");
            w.Line($"public delegate {method.ReturnType} {method.MethodName}Delegate_{suffix}({className} ko{(method.Parameters.Count > 0 ? ", " : "")}{method.ParameterDeclarations});");
            w.Line();

            // Sequence list
            var trackingInterface = GetTrackingInterface(method);
            w.Line($"private readonly global::System.Collections.Generic.List<({method.MethodName}Delegate_{suffix} Callback, global::KnockOff.Times Times, MethodTrackingImpl_{suffix} Tracking)> _sequence_{suffix} = new();");
            w.Line($"private int _sequenceIndex_{suffix};");
            w.Line();

            index++;
        }

        // OnCall overloads for each signature
        foreach (var method in group.Methods)
        {
            var suffix = GetSignatureSuffix(method);
            var trackingInterface = GetTrackingInterface(method);

            // OnCall without Times
            w.Line($"/// <summary>Configures callback for {method.MethodName}({GetParamTypeList(method)}). Returns tracking interface.</summary>");
            w.Line($"public {trackingInterface} OnCall({method.MethodName}Delegate_{suffix} callback)");
            using (w.Braces())
            {
                w.Line($"var tracking = new MethodTrackingImpl_{suffix}();");
                w.Line($"_sequence_{suffix}.Clear();");
                w.Line($"_sequence_{suffix}.Add((callback, global::KnockOff.Times.Forever, tracking));");
                w.Line($"_sequenceIndex_{suffix} = 0;");
                w.Line("return tracking;");
            }
            w.Line();

            // OnCall with Times
            w.Line($"/// <summary>Configures callback for {method.MethodName}({GetParamTypeList(method)}) with Times constraint.</summary>");
            w.Line($"public global::KnockOff.IMethodSequence<{method.MethodName}Delegate_{suffix}> OnCall({method.MethodName}Delegate_{suffix} callback, global::KnockOff.Times times)");
            using (w.Braces())
            {
                w.Line($"var tracking = new MethodTrackingImpl_{suffix}();");
                w.Line($"_sequence_{suffix}.Clear();");
                w.Line($"_sequence_{suffix}.Add((callback, times, tracking));");
                w.Line($"_sequenceIndex_{suffix} = 0;");
                w.Line($"return new MethodSequenceImpl_{suffix}(this);");
            }
            w.Line();
        }

        // Invoke methods for each signature
        foreach (var method in group.Methods)
        {
            RenderGroupInvokeMethod(w, method, className);
        }

        // Reset method (resets all sequences)
        w.Line("/// <summary>Resets all tracking state for all overloads.</summary>");
        using (w.Block("public void Reset()"))
        {
            foreach (var method in group.Methods)
            {
                var suffix = GetSignatureSuffix(method);
                w.Line($"foreach (var (_, _, tracking) in _sequence_{suffix})");
                w.Line("\ttracking.Reset();");
                w.Line($"_sequenceIndex_{suffix} = 0;");
            }
        }
        w.Line();

        // Verify method
        w.Line("/// <summary>Verifies all Times constraints for all overloads were satisfied.</summary>");
        using (w.Block("public bool Verify()"))
        {
            foreach (var method in group.Methods)
            {
                var suffix = GetSignatureSuffix(method);
                w.Line($"foreach (var (_, times, tracking) in _sequence_{suffix})");
                using (w.Braces())
                {
                    w.Line("if (!times.Verify(tracking.CallCount))");
                    w.Line("\treturn false;");
                }
            }
            w.Line("return true;");
        }
        w.Line();

        // Nested tracking classes for each signature
        foreach (var method in group.Methods)
        {
            RenderGroupMethodTrackingImpl(w, method);
        }

        // Nested sequence classes for each signature
        foreach (var method in group.Methods)
        {
            RenderGroupMethodSequenceImpl(w, method, group.InterceptorClassName);
        }
    }
    w.Line();
}

private static string GetSignatureSuffix(FlatMethodModel method)
{
    if (method.Parameters.Count == 0)
        return "NoParams";
    return string.Join("_", method.Parameters.Select(p => GetTypeSuffix(p.Type)));
}

private static string GetTypeSuffix(string type)
{
    // Extract simple type name: "global::System.String" -> "String", "int" -> "Int32"
    var simple = type.Replace("global::", "").Replace("System.", "");
    simple = simple switch
    {
        "int" => "Int32",
        "string" => "String",
        "bool" => "Boolean",
        "long" => "Int64",
        "double" => "Double",
        "float" => "Single",
        "decimal" => "Decimal",
        "char" => "Char",
        "byte" => "Byte",
        _ => simple.Replace(".", "_").Replace("<", "_").Replace(">", "").Replace(",", "_").Replace(" ", "")
    };
    // Remove ? for nullable types
    return simple.TrimEnd('?');
}

private static string GetParamTypeList(FlatMethodModel method)
{
    return string.Join(", ", method.Parameters.Select(p => p.Type));
}
```

**Step 3: Add RenderGroupInvokeMethod**

```csharp
private static void RenderGroupInvokeMethod(CodeWriter w, FlatMethodModel method, string className)
{
    var suffix = GetSignatureSuffix(method);
    var invokeParams = method.Parameters.Count > 0
        ? $"{className} ko, bool strict, " + string.Join(", ", method.Parameters.Select(p => $"{p.RefPrefix}{p.Type} {p.EscapedName}"))
        : $"{className} ko, bool strict";
    var returnType = method.IsVoid ? "void" : method.ReturnType;

    w.Line($"/// <summary>Invokes configured callback for {method.MethodName}({GetParamTypeList(method)}).</summary>");
    w.Line($"internal {returnType} Invoke_{suffix}({invokeParams})");
    using (w.Braces())
    {
        // Initialize out parameters
        foreach (var p in method.Parameters.Where(p => p.RefKind == Microsoft.CodeAnalysis.RefKind.Out))
        {
            w.Line($"{p.EscapedName} = default!;");
        }

        var trackingArgs = BuildTrackingArgs(method);

        w.Line($"if (_sequence_{suffix}.Count == 0)");
        using (w.Braces())
        {
            w.Line($"if (strict) throw global::KnockOff.StubException.NotConfigured(\"\", \"{method.MethodName}\");");
            if (method.IsVoid)
                w.Line("return;");
            else
            {
                var defaultExpr = string.IsNullOrEmpty(method.DefaultExpression) ? "default!" : method.DefaultExpression;
                w.Line($"return {defaultExpr};");
            }
        }
        w.Line();

        w.Line($"var (callback, times, tracking) = _sequence_{suffix}[_sequenceIndex_{suffix}];");
        w.Line($"tracking.RecordCall({trackingArgs});");
        w.Line();

        w.Line("if (!times.IsForever && tracking.CallCount >= times.Count)");
        using (w.Braces())
        {
            w.Line($"if (_sequenceIndex_{suffix} < _sequence_{suffix}.Count - 1)");
            w.Line($"\t_sequenceIndex_{suffix}++;");
            w.Line("else if (tracking.CallCount > times.Count)");
            w.Line($"\tthrow global::KnockOff.StubException.SequenceExhausted(\"{method.MethodName}\");");
        }
        w.Line();

        var callbackArgs = method.Parameters.Count > 0
            ? "ko, " + string.Join(", ", method.Parameters.Select(p => $"{p.RefPrefix}{p.EscapedName}"))
            : "ko";

        if (method.IsVoid)
            w.Line($"callback({callbackArgs});");
        else
            w.Line($"return callback({callbackArgs});");
    }
    w.Line();
}
```

**Step 4: Add RenderGroupMethodTrackingImpl and RenderGroupMethodSequenceImpl**

```csharp
private static void RenderGroupMethodTrackingImpl(CodeWriter w, FlatMethodModel method)
{
    var suffix = GetSignatureSuffix(method);
    var trackingInterface = GetTrackingInterface(method);

    w.Line($"private sealed class MethodTrackingImpl_{suffix} : {trackingInterface}");
    using (w.Braces())
    {
        if (method.TrackableParameters.Count == 1)
        {
            var param = method.TrackableParameters.GetArray()![0];
            w.Line($"private {param.Type} _lastArg = default!;");
        }
        else if (method.TrackableParameters.Count > 1)
        {
            w.Line($"private {method.LastCallType} _lastArgs;");
        }
        w.Line();

        w.Line("public int CallCount { get; private set; }");
        w.Line();
        w.Line("public bool WasCalled => CallCount > 0;");
        w.Line();

        if (method.TrackableParameters.Count == 1)
        {
            var param = method.TrackableParameters.GetArray()![0];
            w.Line($"public {param.Type} LastArg => _lastArg;");
            w.Line();
        }
        else if (method.TrackableParameters.Count > 1)
        {
            w.Line($"public {method.LastCallType} LastArgs => _lastArgs;");
            w.Line();
        }

        if (method.TrackableParameters.Count == 0)
        {
            w.Line("public void RecordCall() => CallCount++;");
        }
        else if (method.TrackableParameters.Count == 1)
        {
            var param = method.TrackableParameters.GetArray()![0];
            w.Line($"public void RecordCall({param.Type} {param.EscapedName}) {{ CallCount++; _lastArg = {param.EscapedName}; }}");
        }
        else
        {
            w.Line($"public void RecordCall({method.LastCallType} args) {{ CallCount++; _lastArgs = args; }}");
        }
        w.Line();

        if (method.TrackableParameters.Count == 0)
            w.Line("public void Reset() => CallCount = 0;");
        else if (method.TrackableParameters.Count == 1)
            w.Line("public void Reset() { CallCount = 0; _lastArg = default!; }");
        else
            w.Line("public void Reset() { CallCount = 0; _lastArgs = default; }");
    }
    w.Line();
}

private static void RenderGroupMethodSequenceImpl(CodeWriter w, FlatMethodModel method, string interceptorClassName)
{
    var suffix = GetSignatureSuffix(method);
    var delegateType = $"{method.MethodName}Delegate_{suffix}";

    w.Line($"private sealed class MethodSequenceImpl_{suffix} : global::KnockOff.IMethodSequence<{delegateType}>");
    using (w.Braces())
    {
        w.Line($"private readonly {interceptorClassName} _interceptor;");
        w.Line();
        w.Line($"public MethodSequenceImpl_{suffix}({interceptorClassName} interceptor) => _interceptor = interceptor;");
        w.Line();

        w.Line("public int TotalCallCount");
        using (w.Braces())
        {
            w.Line("get");
            using (w.Braces())
            {
                w.Line("var total = 0;");
                w.Line($"foreach (var (_, _, tracking) in _interceptor._sequence_{suffix})");
                w.Line("\ttotal += tracking.CallCount;");
                w.Line("return total;");
            }
        }
        w.Line();

        w.Line($"public global::KnockOff.IMethodSequence<{delegateType}> ThenCall({delegateType} callback, global::KnockOff.Times times)");
        using (w.Braces())
        {
            w.Line($"var tracking = new MethodTrackingImpl_{suffix}();");
            w.Line($"_interceptor._sequence_{suffix}.Add((callback, times, tracking));");
            w.Line("return this;");
        }
        w.Line();

        w.Line("public bool Verify()");
        using (w.Braces())
        {
            w.Line($"foreach (var (_, times, tracking) in _interceptor._sequence_{suffix})");
            using (w.Braces())
            {
                w.Line("if (!times.Verify(tracking.CallCount))");
                w.Line("\treturn false;");
            }
            w.Line("return true;");
        }
        w.Line();

        w.Line("public void Reset() => _interceptor.Reset();");
    }
    w.Line();
}
```

**Step 5: Update RenderMethodImplementation to use suffixed Invoke**

In `RenderMethodImplementation`, update the non-generic method path to call the correct Invoke method:

For grouped methods, change:
```csharp
w.Line($"{method.InterceptorName}.Invoke({invokeArgs});");
```

To detect if this method is part of a group with multiple overloads and use the suffixed version:
```csharp
var suffix = method.IsPartOfOverloadGroup ? $"_{GetSignatureSuffix(method)}" : "";
w.Line($"{method.InterceptorName}.Invoke{suffix}({invokeArgs});");
```

This requires adding `IsPartOfOverloadGroup` to FlatMethodModel or detecting it from context.

**Step 6: Build and test**

Run: `dotnet build src/KnockOff.sln --no-incremental 2>&1 | head -100`
Expected: Build succeeds

**Step 7: Commit**

```bash
git add src/Generator/Renderer/FlatRenderer.cs
git commit -m "feat: generate multi-overload interceptor classes"
```

---

## Task 6: Add FlatMethodModel.SignatureSuffix Property

**Files:**
- Modify: `src/Generator/Model/Flat/FlatMethodModel.cs`
- Modify: `src/Generator/Builder/FlatModelBuilder.cs`

**Step 1: Add SignatureSuffix to FlatMethodModel**

Add to the record:
```csharp
/// <summary>Suffix for this signature when part of overload group (e.g., "String_Int32").</summary>
string SignatureSuffix,
/// <summary>True if this method is part of an overload group with multiple signatures.</summary>
bool IsPartOfOverloadGroup
```

**Step 2: Populate in FlatModelBuilder**

When creating FlatMethodModel, compute the suffix based on parameter types.

**Step 3: Build and verify**

Run: `dotnet build src/Generator/Generator.csproj --no-incremental`
Expected: PASS

**Step 4: Commit**

```bash
git add src/Generator/Model/Flat/FlatMethodModel.cs src/Generator/Builder/FlatModelBuilder.cs
git commit -m "feat: add SignatureSuffix to FlatMethodModel for overload support"
```

---

## Task 7: Write Tests for Method Overloading

**Files:**
- Modify: `src/Tests/KnockOffTests/SequencingTests.cs` (add overload tests)

**Step 1: Add test interface and stub**

```csharp
public interface IOverloadTestService
{
    string Format(string input);
    string Format(string input, bool uppercase);
    string Format(string input, int maxLength);
}

[KnockOff]
public partial class OverloadTestKnockOff : IOverloadTestService
{
}
```

**Step 2: Add tests**

```csharp
public class MethodOverloadTests
{
    [Fact]
    public void OnCall_DifferentOverloads_CompilerResolvesCorrectly()
    {
        var stub = new OverloadTestKnockOff();

        // Compiler resolves based on lambda parameter types
        var tracking1 = stub.Format.OnCall((ko, input) => input.ToUpper());
        var tracking2 = stub.Format.OnCall((ko, input, uppercase) => uppercase ? input.ToUpper() : input);
        var tracking3 = stub.Format.OnCall((ko, input, maxLength) => input.Substring(0, Math.Min(input.Length, maxLength)));

        IOverloadTestService svc = stub;

        Assert.Equal("HELLO", svc.Format("hello"));
        Assert.Equal("world", svc.Format("world", false));
        Assert.Equal("hel", svc.Format("hello", 3));

        Assert.Equal(1, tracking1.CallCount);
        Assert.Equal(1, tracking2.CallCount);
        Assert.Equal(1, tracking3.CallCount);
    }

    [Fact]
    public void OnCall_EachOverload_TracksIndependently()
    {
        var stub = new OverloadTestKnockOff();

        var tracking1 = stub.Format.OnCall((ko, input) => "1");
        var tracking2 = stub.Format.OnCall((ko, input, uppercase) => "2");

        IOverloadTestService svc = stub;

        svc.Format("a");
        svc.Format("b");
        svc.Format("c", true);

        Assert.Equal(2, tracking1.CallCount);
        Assert.Equal("b", tracking1.LastArg);

        Assert.Equal(1, tracking2.CallCount);
        Assert.Equal(("c", true), tracking2.LastArgs);
    }
}
```

**Step 3: Run tests**

Run: `dotnet test src/Tests/KnockOffTests --filter "FullyQualifiedName~MethodOverloadTests" -v n`
Expected: PASS

**Step 4: Commit**

```bash
git add src/Tests/KnockOffTests/SequencingTests.cs
git commit -m "test: add method overload tests"
```

---

## Task 8: Update Sandbox Program.cs for New API

**Files:**
- Modify: `src/Tests/KnockOffSandbox/Program.cs`

**Step 1: Update to use OnCall() method API**

The sandbox uses old property-based API. Update to use new method-based API:

```csharp
// Old: knockOff.DoWork.WasCalled
// New: Set up tracking via OnCall first
var doWorkTracking = knockOff.DoWork.OnCall(ko => { });
service.DoWork();
Console.WriteLine($"  DoWork.WasCalled: {doWorkTracking.WasCalled}");
```

**Step 2: Build and run**

Run: `dotnet run --project src/Tests/KnockOffSandbox`
Expected: Runs without errors

**Step 3: Commit**

```bash
git add src/Tests/KnockOffSandbox/Program.cs
git commit -m "chore: update sandbox to new OnCall API"
```

---

## Task 9: Re-enable Excluded Test Files

**Files:**
- Modify: `src/Tests/KnockOffTests/KnockOffTests.csproj`
- Modify: Various excluded test files to use new API

**Step 1: Remove exclusions one at a time**

Start with simpler files first. For each file:
1. Remove from exclusion list
2. Update to new API
3. Build and test
4. Commit

**Step 2: Update API patterns**

| Old Pattern | New Pattern |
|-------------|-------------|
| `stub.Method.OnCall = callback` | `var tracking = stub.Method.OnCall(callback)` |
| `stub.Method.CallCount` | `tracking.CallCount` |
| `stub.Method1.OnCall = ...` | `stub.Method.OnCall((with, params) => ...)` |

**Step 3: Commit each file as you go**

```bash
git commit -m "refactor: migrate [filename] to new OnCall API"
```

---

## Verification

After all tasks complete:

1. Run full test suite:
   ```bash
   dotnet test src/Tests/KnockOffTests -v n
   ```
   Expected: All tests pass

2. Run benchmarks to verify no regressions:
   ```bash
   dotnet run -c Release --project src/Benchmarks/KnockOff.Benchmarks
   ```

3. Check generated code for overloaded methods:
   - Look at `src/Tests/KnockOffTests/Generated/.../OverloadedServiceKnockOff.g.cs`
   - Verify multiple OnCall overloads exist in the interceptor class

---

## Summary

| Task | Description |
|------|-------------|
| 1 | Fix KO0100 to only fire for identical signatures across different interfaces |
| 2 | Restore IOverloadedService in sandbox |
| 3 | Create FlatMethodGroup model |
| 4 | Update FlatModelBuilder to group methods |
| 5 | Update FlatRenderer for multi-overload interceptors |
| 6 | Add SignatureSuffix to FlatMethodModel |
| 7 | Write method overload tests |
| 8 | Update sandbox for new API |
| 9 | Re-enable excluded test files |
