// src/Generator/Renderer/ClassRenderer.cs
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using KnockOff;
using KnockOff.Model.Inline;
using KnockOff.Model.Shared;

namespace KnockOff.Renderer;

/// <summary>
/// Renders InlineClassStubModel to source code.
/// Pure emission - no decisions, just output what the model says.
/// </summary>
internal static class ClassRenderer
{
    /// <summary>
    /// Renders a class stub to the CodeWriter at the given base indent level.
    /// </summary>
    public static void Render(CodeWriter w, InlineClassStubModel cls, int baseIndent = 2)
    {
        var indent = new string('\t', baseIndent);
        var indent1 = indent + "\t";
        var indent2 = indent + "\t\t";
        var indent3 = indent + "\t\t\t";
        var indent4 = indent + "\t\t\t\t";

        // Render interceptor classes
        foreach (var prop in cls.Properties)
        {
            RenderPropertyInterceptorClass(w, prop, cls.StubClassName, indent);
        }

        foreach (var indexer in cls.Indexers)
        {
            RenderIndexerInterceptorClass(w, indexer, cls.StubClassName, indent);
        }

        foreach (var method in cls.Methods)
        {
            RenderMethodInterceptorClass(w, method, cls.StubClassName, indent);
        }

        foreach (var evt in cls.Events)
        {
            RenderEventInterceptorClass(w, evt, cls.StubClassName, indent);
        }

        // Render the wrapper stub class
        w.Line($"{indent}/// <summary>Stub for {cls.BaseType} via composition.</summary>");
        w.Line($"{indent}public class {cls.StubClassName}{cls.TypeParameterList} : global::KnockOff.IKnockOffStub{cls.ConstraintClauses}");
        w.Line($"{indent}{{");

        // Strict property
        w.Line($"{indent1}/// <summary>When true, unconfigured method calls throw StubException instead of returning default. Not yet implemented for class stubs.</summary>");
        w.Line($"{indent1}public bool Strict {{ get; set; }}");
        w.Line();

        // Interceptor properties
        foreach (var interceptorProp in cls.InterceptorProperties)
        {
            w.Line($"{indent1}/// <summary>{interceptorProp.Description}</summary>");
            w.Line($"{indent1}public {interceptorProp.InterceptorTypeName} {interceptorProp.PropertyName} {{ get; }} = new();");
        }
        w.Line();

        // .Object property
        w.Line($"{indent1}/// <summary>The {cls.BaseType} instance. Pass this to code expecting the target class.</summary>");
        w.Line($"{indent1}public {cls.BaseType} Object {{ get; }}");
        w.Line();

        // Constructors
        foreach (var ctor in cls.Constructors)
        {
            RenderWrapperConstructor(w, ctor, cls.StubClassName, cls.TypeParameterList, indent1);
        }

        // ResetInterceptors method
        w.Line($"{indent1}/// <summary>Resets all interceptor state.</summary>");
        w.Line($"{indent1}public void ResetInterceptors()");
        w.Line($"{indent1}{{");
        foreach (var resetStmt in cls.ResetStatements)
        {
            w.Line($"{indent2}{resetStmt}");
        }
        w.Line($"{indent1}}}");
        w.Line();

        // Nested Impl class
        RenderImplClass(w, cls, indent1, indent2, indent3, indent4);

        w.Line($"{indent}}}");
        w.Line();
    }

    #region Interceptor Class Rendering

    private static void RenderPropertyInterceptorClass(CodeWriter w, InlineClassPropertyModel prop, string stubClassName, string indent)
    {
        var indent1 = indent + "\t";

        w.Line($"{indent}/// <summary>Interceptor for {stubClassName}.{prop.PropertyName}.</summary>");
        w.Line($"{indent}public sealed class {prop.InterceptorClassName}{prop.TypeParameterList}{prop.ConstraintClauses}");
        w.Line($"{indent}{{");

        if (prop.HasGetter)
        {
            w.Line($"{indent1}/// <summary>Number of times the getter was accessed.</summary>");
            w.Line($"{indent1}public int GetCount {{ get; private set; }}");
            w.Line();
            w.Line($"{indent1}/// <summary>Callback for getter. If set, returns its value instead of base.</summary>");
            w.Line($"{indent1}public global::System.Func<{prop.StubClassName}, {prop.ReturnType}>? OnGet {{ get; set; }}");
            w.Line();
        }

        if (prop.HasSetter)
        {
            w.Line($"{indent1}/// <summary>Number of times the setter was accessed.</summary>");
            w.Line($"{indent1}public int SetCount {{ get; private set; }}");
            w.Line();
            w.Line($"{indent1}/// <summary>The last value passed to the setter.</summary>");
            w.Line($"{indent1}public {prop.NullableReturnType} LastSetValue {{ get; private set; }}");
            w.Line();
            w.Line($"{indent1}/// <summary>Callback for setter. If set, called instead of base.</summary>");
            w.Line($"{indent1}public global::System.Action<{prop.StubClassName}, {prop.ReturnType}>? OnSet {{ get; set; }}");
            w.Line();
        }

        // RecordGet/RecordSet
        if (prop.HasGetter)
        {
            w.Line($"{indent1}/// <summary>Records a getter access.</summary>");
            w.Line($"{indent1}public void RecordGet() => GetCount++;");
            w.Line();
        }
        if (prop.HasSetter)
        {
            w.Line($"{indent1}/// <summary>Records a setter access.</summary>");
            w.Line($"{indent1}public void RecordSet({prop.NullableReturnType} value) {{ SetCount++; LastSetValue = value; }}");
            w.Line();
        }

        // Reset method
        w.Line($"{indent1}/// <summary>Resets all tracking state.</summary>");
        var resetParts = new List<string>();
        if (prop.HasGetter) resetParts.Add("GetCount = 0; OnGet = null;");
        if (prop.HasSetter) resetParts.Add("SetCount = 0; LastSetValue = default; OnSet = null;");
        w.Line($"{indent1}public void Reset() {{ {string.Join(" ", resetParts)} }}");

        w.Line($"{indent}}}");
        w.Line();
    }

    private static void RenderIndexerInterceptorClass(CodeWriter w, InlineClassIndexerModel indexer, string stubClassName, string indent)
    {
        var indent1 = indent + "\t";

        w.Line($"{indent}/// <summary>Interceptor for {stubClassName}.{indexer.IndexerName}.</summary>");
        w.Line($"{indent}public sealed class {indexer.InterceptorClassName}{indexer.TypeParameterList}{indexer.ConstraintClauses}");
        w.Line($"{indent}{{");

        if (indexer.HasGetter)
        {
            w.Line($"{indent1}/// <summary>Number of times the getter was accessed.</summary>");
            w.Line($"{indent1}public int GetCount {{ get; private set; }}");
            w.Line();

            var nullableKeyType = MakeNullable(indexer.KeyType);
            w.Line($"{indent1}/// <summary>The last key used to access the getter.</summary>");
            w.Line($"{indent1}public {nullableKeyType} LastGetKey {{ get; private set; }}");
            w.Line();

            var paramTypes = indexer.ParameterDeclarations.Split(',').Select(p => p.Trim().Split(' ')[0]).ToArray();
            var paramList = string.Join(", ", paramTypes);
            w.Line($"{indent1}/// <summary>Callback for getter.</summary>");
            w.Line($"{indent1}public global::System.Func<{indexer.StubClassName}, {paramList}, {indexer.ReturnType}>? OnGet {{ get; set; }}");
            w.Line();
        }

        if (indexer.HasSetter)
        {
            w.Line($"{indent1}/// <summary>Number of times the setter was accessed.</summary>");
            w.Line($"{indent1}public int SetCount {{ get; private set; }}");
            w.Line();

            var entryType = $"({indexer.KeyType} Key, {indexer.ReturnType} Value)";
            w.Line($"{indent1}/// <summary>The last key-value pair passed to the setter.</summary>");
            w.Line($"{indent1}public {entryType}? LastSetEntry {{ get; private set; }}");
            w.Line();

            var paramTypes = indexer.ParameterDeclarations.Split(',').Select(p => p.Trim().Split(' ')[0]).ToArray();
            var paramList = string.Join(", ", paramTypes);
            w.Line($"{indent1}/// <summary>Callback for setter.</summary>");
            w.Line($"{indent1}public global::System.Action<{indexer.StubClassName}, {paramList}, {indexer.ReturnType}>? OnSet {{ get; set; }}");
            w.Line();
        }

        // RecordGet/RecordSet
        if (indexer.HasGetter)
        {
            w.Line($"{indent1}/// <summary>Records a getter access.</summary>");
            w.Line($"{indent1}public void RecordGet({indexer.ParameterDeclarations}) {{ GetCount++; LastGetKey = {indexer.KeyExpression}; }}");
            w.Line();
        }
        if (indexer.HasSetter)
        {
            w.Line($"{indent1}/// <summary>Records a setter access.</summary>");
            w.Line($"{indent1}public void RecordSet({indexer.ParameterDeclarations}, {indexer.ReturnType} value) {{ SetCount++; LastSetEntry = ({indexer.KeyExpression}, value); }}");
            w.Line();
        }

        // Backing dictionary
        var singleKeyType = indexer.KeyType;
        // If it's a tuple, we need to handle it differently - but for single param indexers, use the type directly
        if (!indexer.KeyType.StartsWith("("))
        {
            singleKeyType = indexer.KeyType;
        }
        w.Line($"{indent1}/// <summary>Backing storage for this indexer.</summary>");
        w.Line($"{indent1}public global::System.Collections.Generic.Dictionary<{singleKeyType}, {indexer.ReturnType}> Backing {{ get; }} = new();");
        w.Line();

        // Reset method
        w.Line($"{indent1}/// <summary>Resets all tracking state.</summary>");
        var resetParts = new List<string>();
        if (indexer.HasGetter) resetParts.Add("GetCount = 0; LastGetKey = default; OnGet = null;");
        if (indexer.HasSetter) resetParts.Add("SetCount = 0; LastSetEntry = default; OnSet = null;");
        // Note: Backing dictionary is intentionally NOT cleared
        w.Line($"{indent1}public void Reset() {{ {string.Join(" ", resetParts)} }}");

        w.Line($"{indent}}}");
        w.Line();
    }

    private static void RenderMethodInterceptorClass(CodeWriter w, InlineClassMethodModel method, string stubClassName, string indent)
    {
        var indent1 = indent + "\t";

        w.Line($"{indent}/// <summary>Interceptor for {stubClassName}.{method.MethodName}.</summary>");
        w.Line($"{indent}public sealed class {method.InterceptorClassName}{method.TypeParameterList}{method.ConstraintClauses}");
        w.Line($"{indent}{{");

        // CallCount and WasCalled
        w.Line($"{indent1}/// <summary>Number of times this method was called.</summary>");
        w.Line($"{indent1}public int CallCount {{ get; private set; }}");
        w.Line();
        w.Line($"{indent1}/// <summary>Whether this method was called at least once.</summary>");
        w.Line($"{indent1}public bool WasCalled => CallCount > 0;");
        w.Line();

        // LastCallArg/LastCallArgs
        if (method.LastCallArgType != null)
        {
            w.Line($"{indent1}/// <summary>The argument from the last call.</summary>");
            w.Line($"{indent1}public {method.LastCallArgType} LastCallArg {{ get; private set; }}");
            w.Line();
        }
        else if (method.LastCallArgsType != null)
        {
            w.Line($"{indent1}/// <summary>The arguments from the last call.</summary>");
            w.Line($"{indent1}public {method.LastCallArgsType} LastCallArgs {{ get; private set; }}");
            w.Line();
        }

        // OnCall callback
        w.Line($"{indent1}/// <summary>Callback invoked when method is called. If set, called instead of base.</summary>");
        w.Line($"{indent1}public {method.DelegateType}? OnCall {{ get; set; }}");
        w.Line();

        // RecordCall method
        var inputParams = method.InputParameters.GetArray() ?? Array.Empty<Model.Shared.ParameterModel>();
        var recordParams = string.Join(", ", inputParams.Select(p => $"{p.Type} {p.Name}"));
        w.Append($"{indent1}public void RecordCall({recordParams}) {{ CallCount++; ");
        if (method.LastCallArgType != null && inputParams.Length > 0)
        {
            w.Append($"LastCallArg = {inputParams[0].Name}; ");
        }
        else if (method.LastCallArgsType != null && inputParams.Length > 1)
        {
            w.Append($"LastCallArgs = ({string.Join(", ", inputParams.Select(p => p.Name))}); ");
        }
        w.Line("}");
        w.Line();

        // Reset method
        w.Append($"{indent1}public void Reset() {{ CallCount = 0; ");
        if (method.LastCallArgType != null)
        {
            w.Append("LastCallArg = default; ");
        }
        else if (method.LastCallArgsType != null)
        {
            w.Append("LastCallArgs = default; ");
        }
        w.Line("OnCall = null; }");

        w.Line($"{indent}}}");
        w.Line();
    }

    private static void RenderEventInterceptorClass(CodeWriter w, InlineClassEventModel evt, string stubClassName, string indent)
    {
        var indent1 = indent + "\t";

        w.Line($"{indent}/// <summary>Interceptor for {stubClassName}.{evt.EventName}.</summary>");
        w.Line($"{indent}public sealed class {evt.InterceptorClassName}{evt.TypeParameterList}{evt.ConstraintClauses}");
        w.Line($"{indent}{{");

        w.Line($"{indent1}/// <summary>Number of times the event was subscribed to.</summary>");
        w.Line($"{indent1}public int AddCount {{ get; private set; }}");
        w.Line();
        w.Line($"{indent1}/// <summary>Number of times the event was unsubscribed from.</summary>");
        w.Line($"{indent1}public int RemoveCount {{ get; private set; }}");
        w.Line();
        w.Line($"{indent1}/// <summary>The backing delegate for raising the event.</summary>");
        w.Line($"{indent1}public {evt.DelegateType}? Handler {{ get; private set; }}");
        w.Line();
        w.Line($"{indent1}/// <summary>Records an event subscription.</summary>");
        w.Line($"{indent1}public void RecordAdd({evt.DelegateType}? handler) {{ AddCount++; Handler = ({evt.DelegateType}?)global::System.Delegate.Combine(Handler, handler); }}");
        w.Line();
        w.Line($"{indent1}/// <summary>Records an event unsubscription.</summary>");
        w.Line($"{indent1}public void RecordRemove({evt.DelegateType}? handler) {{ RemoveCount++; Handler = ({evt.DelegateType}?)global::System.Delegate.Remove(Handler, handler); }}");
        w.Line();
        w.Line($"{indent1}/// <summary>Resets all tracking state.</summary>");
        w.Line($"{indent1}public void Reset() {{ AddCount = 0; RemoveCount = 0; Handler = null; }}");

        w.Line($"{indent}}}");
        w.Line();
    }

    #endregion

    #region Wrapper Constructor Rendering

    private static void RenderWrapperConstructor(CodeWriter w, InlineConstructorModel ctor, string stubClassName, string typeParamList, string indent)
    {
        // Constructors don't include type parameters - those come from the class definition
        w.Line($"{indent}public {stubClassName}({ctor.ParameterDeclarations})");
        w.Line($"{indent}{{");
        if (string.IsNullOrEmpty(ctor.BaseCallArguments))
        {
            w.Line($"{indent}\tObject = new Impl(this);");
        }
        else
        {
            w.Line($"{indent}\tObject = new Impl(this, {ctor.BaseCallArguments});");
        }
        w.Line($"{indent}}}");
        w.Line();
    }

    #endregion

    #region Impl Class Rendering

    private static void RenderImplClass(CodeWriter w, InlineClassStubModel cls, string indent, string indent1, string indent2, string indent3)
    {
        var stubClassName = cls.StubClassName + cls.TypeParameterList;

        // Suppress CS8618 for classes with required members
        if (cls.HasRequiredMembers)
        {
            w.Line("#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor");
        }

        w.Line($"{indent}/// <summary>Internal implementation that inherits from {cls.BaseType}.</summary>");
        // Note: Impl is a nested class - it doesn't need generic type params or constraints
        // because it inherits them from the parent generic class
        w.Line($"{indent}private sealed class Impl : {cls.BaseType}");
        w.Line($"{indent}{{");

        // Reference to the wrapper
        w.Line($"{indent1}private readonly {stubClassName} _stub;");
        w.Line();

        // Constructors
        foreach (var ctor in cls.Constructors)
        {
            RenderImplConstructor(w, ctor, stubClassName, cls.HasRequiredMembers, cls.RequiredMemberNames, indent1);
        }

        // Property overrides
        foreach (var prop in cls.ImplProperties)
        {
            RenderImplPropertyOverride(w, prop, indent1, indent2);
        }

        // Indexer overrides
        foreach (var indexer in cls.ImplIndexers)
        {
            RenderImplIndexerOverride(w, indexer, indent1, indent2);
        }

        // Method overrides
        foreach (var method in cls.ImplMethods)
        {
            RenderImplMethodOverride(w, method, indent1, indent2);
        }

        // Event overrides
        foreach (var evt in cls.ImplEvents)
        {
            RenderImplEventOverride(w, evt, indent1, indent2);
        }

        w.Line($"{indent}}}");

        // Restore the warning if we disabled it
        if (cls.HasRequiredMembers)
        {
            w.Line("#pragma warning restore CS8618");
        }
    }

    private static void RenderImplConstructor(CodeWriter w, InlineConstructorModel ctor, string stubClassName, bool hasRequiredMembers, EquatableArray<string> requiredMemberNames, string indent)
    {
        var indent1 = indent + "\t";

        var paramList = string.IsNullOrEmpty(ctor.ParameterDeclarations)
            ? $"{stubClassName} stub"
            : $"{stubClassName} stub, {ctor.ParameterDeclarations}";

        // Add [SetsRequiredMembers] if the base class has required properties
        if (hasRequiredMembers)
        {
            w.Line($"{indent}[global::System.Diagnostics.CodeAnalysis.SetsRequiredMembers]");
        }
        w.Line($"{indent}public Impl({paramList}) : base({ctor.BaseCallArguments})");
        w.Line($"{indent}{{");

        // Initialize required members FIRST while _stub is still null
        foreach (var memberName in requiredMemberNames)
        {
            w.Line($"{indent1}{memberName} = default!;");
        }

        // Set _stub AFTER required member initialization
        w.Line($"{indent1}_stub = stub;");
        w.Line($"{indent}}}");
        w.Line();
    }

    private static void RenderImplPropertyOverride(CodeWriter w, InlineClassImplPropertyModel prop, string indent, string indent1)
    {
        var indent2 = indent1 + "\t";
        var requiredKeyword = prop.IsRequired ? "required " : "";

        w.Line($"{indent}/// <inheritdoc />");
        w.Line($"{indent}{requiredKeyword}{prop.AccessModifier} override {prop.ReturnType} {prop.PropertyName}");
        w.Line($"{indent}{{");

        if (prop.HasGetter)
        {
            w.Line($"{indent1}get");
            w.Line($"{indent1}{{");
            // Handle calls from base constructor when _stub is null
            w.Line($"{indent2}_stub?.{prop.PropertyName}.RecordGet();");
            w.Line($"{indent2}if (_stub?.{prop.PropertyName}.OnGet is {{ }} onGet) return onGet(_stub);");
            if (prop.IsAbstract)
            {
                w.Line($"{indent2}return default!;");
            }
            else
            {
                w.Line($"{indent2}return base.{prop.PropertyName};");
            }
            w.Line($"{indent1}}}");
        }

        if (prop.HasSetter)
        {
            var setterKeyword = prop.IsInitOnly ? "init" : "set";
            w.Line($"{indent1}{setterKeyword}");
            w.Line($"{indent1}{{");
            // Handle calls from base constructor when _stub is null
            w.Line($"{indent2}_stub?.{prop.PropertyName}.RecordSet(value);");
            w.Line($"{indent2}if (_stub?.{prop.PropertyName}.OnSet is {{ }} onSet) onSet(_stub, value);");
            if (!prop.IsAbstract)
            {
                w.Line($"{indent2}else base.{prop.PropertyName} = value;");
            }
            w.Line($"{indent1}}}");
        }

        w.Line($"{indent}}}");
        w.Line();
    }

    private static void RenderImplIndexerOverride(CodeWriter w, InlineClassImplIndexerModel indexer, string indent, string indent1)
    {
        var indent2 = indent1 + "\t";

        w.Line($"{indent}/// <inheritdoc />");
        w.Line($"{indent}{indexer.AccessModifier} override {indexer.ReturnType} this[{indexer.ParameterDeclarations}]");
        w.Line($"{indent}{{");

        if (indexer.HasGetter)
        {
            w.Line($"{indent1}get");
            w.Line($"{indent1}{{");
            // Handle calls from base constructor when _stub is null
            w.Line($"{indent2}_stub?.{indexer.IndexerName}.RecordGet({indexer.ArgumentList});");
            w.Line($"{indent2}if (_stub?.{indexer.IndexerName}.OnGet is {{ }} onGet) return onGet(_stub, {indexer.ArgumentList});");
            if (indexer.IsAbstract)
            {
                var defaultExpr = indexer.IsNullable ? "default" : GetDefaultForType(indexer.ReturnType, indexer.DefaultStrategy, indexer.ConcreteTypeForNew);
                w.Line($"{indent2}if (_stub?.{indexer.IndexerName}.Backing.TryGetValue({indexer.KeyExpression}, out var v) == true) return v;");
                w.Line($"{indent2}return {defaultExpr};");
            }
            else
            {
                w.Line($"{indent2}return base[{indexer.ArgumentList}];");
            }
            w.Line($"{indent1}}}");
        }

        if (indexer.HasSetter)
        {
            w.Line($"{indent1}set");
            w.Line($"{indent1}{{");
            // Handle calls from base constructor when _stub is null
            w.Line($"{indent2}_stub?.{indexer.IndexerName}.RecordSet({indexer.ArgumentList}, value);");
            w.Line($"{indent2}if (_stub?.{indexer.IndexerName}.OnSet is {{ }} onSet) onSet(_stub, {indexer.ArgumentList}, value);");
            if (indexer.IsAbstract)
            {
                w.Line($"{indent2}else if (_stub is not null) _stub.{indexer.IndexerName}.Backing[{indexer.KeyExpression}] = value;");
            }
            else
            {
                w.Line($"{indent2}else base[{indexer.ArgumentList}] = value;");
            }
            w.Line($"{indent1}}}");
        }

        w.Line($"{indent}}}");
        w.Line();
    }

    private static void RenderImplMethodOverride(CodeWriter w, InlineClassImplMethodModel method, string indent, string indent1)
    {
        w.Line($"{indent}/// <inheritdoc />");
        w.Line($"{indent}{method.AccessModifier} override {method.ReturnType} {method.MethodName}({method.ParameterDeclarations})");
        w.Line($"{indent}{{");

        // Record the call (null check for calls during base constructor)
        if (string.IsNullOrEmpty(method.InputArgumentList))
        {
            w.Line($"{indent1}_stub?.{method.HandlerName}.RecordCall();");
        }
        else
        {
            w.Line($"{indent1}_stub?.{method.HandlerName}.RecordCall({method.InputArgumentList});");
        }

        // Check for OnCall callback (null check for calls during base constructor)
        if (method.IsVoid || method.IsTask || method.IsValueTask)
        {
            w.Line($"{indent1}if (_stub?.{method.HandlerName}.OnCall is {{ }} onCall) {{ onCall({method.OnCallArgumentList}); return; }}");
        }
        else
        {
            w.Line($"{indent1}if (_stub?.{method.HandlerName}.OnCall is {{ }} onCall) return onCall({method.OnCallArgumentList});");
        }

        // Default behavior - delegate to base or return default for abstract
        if (method.IsAbstract)
        {
            // Abstract - return default
            if (method.IsVoid)
            {
                // void - nothing to return
            }
            else if (method.IsTask)
            {
                w.Line($"{indent1}return global::System.Threading.Tasks.Task.CompletedTask;");
            }
            else if (method.IsValueTask)
            {
                w.Line($"{indent1}return default;");
            }
            else
            {
                w.Line($"{indent1}return default!;");
            }
        }
        else
        {
            // Virtual - delegate to base
            if (method.IsVoid)
            {
                w.Line($"{indent1}base.{method.MethodName}({method.ArgumentList});");
            }
            else
            {
                w.Line($"{indent1}return base.{method.MethodName}({method.ArgumentList});");
            }
        }

        w.Line($"{indent}}}");
        w.Line();
    }

    private static void RenderImplEventOverride(CodeWriter w, InlineClassImplEventModel evt, string indent, string indent1)
    {
        w.Line($"{indent}/// <inheritdoc />");
        w.Line($"{indent}public override event {evt.DelegateType}? {evt.EventName}");
        w.Line($"{indent}{{");
        // Null check for calls during base constructor
        w.Line($"{indent1}add => _stub?.{evt.EventName}.RecordAdd(value);");
        w.Line($"{indent1}remove => _stub?.{evt.EventName}.RecordRemove(value);");
        w.Line($"{indent}}}");
        w.Line();
    }

    #endregion

    #region Helper Methods

    private static string MakeNullable(string type)
    {
        if (type.EndsWith("?"))
            return type;
        return type + "?";
    }

    private static string GetDefaultForType(string typeName, DefaultValueStrategy strategy, string? concreteType)
    {
        if (typeName == "global::System.Threading.Tasks.ValueTask" || typeName == "ValueTask")
            return "default";

        if (typeName == "global::System.Threading.Tasks.Task" || typeName == "Task")
            return "global::System.Threading.Tasks.Task.CompletedTask";

        if (typeName.Contains("ValueTask<") || typeName.Contains("global::System.Threading.Tasks.ValueTask<"))
        {
            var innerType = ExtractGenericArg(typeName);
            if (!string.IsNullOrEmpty(innerType))
            {
                if (strategy == DefaultValueStrategy.NewInstance)
                {
                    var innerTypeToNew = concreteType ?? innerType;
                    return $"new global::System.Threading.Tasks.ValueTask<{innerType}>(new {innerTypeToNew}())";
                }
                return "default";
            }
            return "default";
        }

        if (typeName.Contains("Task<") || typeName.Contains("global::System.Threading.Tasks.Task<"))
        {
            var innerType = ExtractGenericArg(typeName);
            if (!string.IsNullOrEmpty(innerType))
            {
                var innerTypeToNew = concreteType ?? innerType;
                var innerValue = strategy switch
                {
                    DefaultValueStrategy.NewInstance => $"new {innerTypeToNew}()",
                    DefaultValueStrategy.Default => "default!",
                    _ => "default!"
                };
                return $"global::System.Threading.Tasks.Task.FromResult<{innerType}>({innerValue})";
            }
            return "global::System.Threading.Tasks.Task.CompletedTask";
        }

        var typeToNew = concreteType ?? typeName;
        return strategy switch
        {
            DefaultValueStrategy.NewInstance => $"new {typeToNew}()",
            DefaultValueStrategy.Default => "default!",
            _ => "default!"
        };
    }

    private static string ExtractGenericArg(string typeName)
    {
        var start = typeName.IndexOf('<');
        var end = typeName.LastIndexOf('>');
        if (start >= 0 && end > start)
        {
            return typeName.Substring(start + 1, end - start - 1);
        }
        return "";
    }

    #endregion
}
