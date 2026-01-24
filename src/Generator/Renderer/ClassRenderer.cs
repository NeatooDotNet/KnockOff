// src/Generator/Renderer/ClassRenderer.cs
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using KnockOff;
using KnockOff.Model.Inline;
using KnockOff.Model.Shared;
using KnockOff.Renderer.Shared;

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
        // For class stubs, use IncludeStrictParameter=true so the Impl class can pass _stub.Strict
        foreach (var prop in cls.Properties)
        {
            var unifiedModel = ToUnifiedPropertyModel(prop);
            var options = new PropertyInterceptorRenderOptions(
                BaseIndent: 2,
                IncludeStrictParameter: true,
                StrictAccessExpression: "strict",
                InterceptorTypeParameters: prop.TypeParameterList,
                InterceptorConstraints: prop.ConstraintClauses);
            w.SetIndent(2);
            PropertyInterceptorRenderer.RenderInterceptorClass(w, unifiedModel, options);
        }

        foreach (var indexer in cls.Indexers)
        {
            var unifiedModel = ToUnifiedIndexerModel(indexer);
            var options = new IndexerInterceptorRenderOptions(
                BaseIndent: 2,
                IncludeStrictParameter: true,
                StrictAccessExpression: "strict",
                InterceptorTypeParameters: indexer.TypeParameterList,
                InterceptorConstraints: indexer.ConstraintClauses);
            w.SetIndent(2);
            IndexerInterceptorRenderer.RenderInterceptorClass(w, unifiedModel, options);
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

        // Verify and VerifyAll methods
        RenderClassVerifyMethods(w, cls, indent1, indent2);

        // Nested Impl class
        RenderImplClass(w, cls, indent1, indent2, indent3, indent4);

        w.Line($"{indent}}}");
        w.Line();
    }

    #region Interceptor Class Rendering

    private static void RenderMethodInterceptorClass(CodeWriter w, InlineClassMethodModel method, string stubClassName, string indent)
    {
        var indent1 = indent + "\t";

        w.Line($"{indent}/// <summary>Interceptor for {stubClassName}.{method.MethodName}.</summary>");
        w.Line($"{indent}public sealed class {method.InterceptorClassName}{method.TypeParameterList} : global::KnockOff.IMethodTracking{method.ConstraintClauses}");
        w.Line($"{indent}{{");

        // Private callback field
        w.Line($"{indent1}private {method.DelegateType}? _onCall;");
        w.Line();

        // CallCount (private - use Verify(Times) for public API)
        w.Line($"{indent1}private int _callCount;");
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

        // OnCall method (returns IMethodTracking for consistency)
        w.Line($"{indent1}/// <summary>Sets the callback invoked when method is called. Returns this interceptor for tracking.</summary>");
        w.Line($"{indent1}public global::KnockOff.IMethodTracking OnCall({method.DelegateType} callback) {{ _onCall = callback; return this; }}");
        w.Line();

        // Callback property for internal use by invocation logic
        w.Line($"{indent1}/// <summary>Gets the configured callback (internal use).</summary>");
        w.Line($"{indent1}internal {method.DelegateType}? Callback => _onCall;");
        w.Line();

        // RecordCall method
        var inputParams = method.InputParameters.GetArray() ?? Array.Empty<Model.Shared.ParameterModel>();
        var recordParams = string.Join(", ", inputParams.Select(p => $"{p.Type} {p.Name}"));
        w.Append($"{indent1}public void RecordCall({recordParams}) {{ _callCount++; ");
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

        // Reset method - clears tracking state but preserves configuration (OnCall)
        w.Line($"{indent1}/// <summary>Resets tracking state (CallCount, LastCallArg/LastCallArgs) but preserves configuration (OnCall).</summary>");
        w.Append($"{indent1}public void Reset() {{ _callCount = 0; ");
        if (method.LastCallArgType != null)
        {
            w.Append("LastCallArg = default; ");
        }
        else if (method.LastCallArgsType != null)
        {
            w.Append("LastCallArgs = default; ");
        }
        w.Line("}");
        w.Line();

        // Verify methods - new API throws VerificationException
        w.Line($"{indent1}/// <summary>Verifies call count is at least once. Throws VerificationException if not.</summary>");
        w.Line($"{indent1}public void Verify() => Verify(global::KnockOff.Times.AtLeastOnce);");
        w.Line();

        w.Line($"{indent1}/// <summary>Verifies call count satisfies the Times constraint. Throws VerificationException if not.</summary>");
        w.Line($"{indent1}public void Verify(global::KnockOff.Times times)");
        w.Line($"{indent1}{{");
        w.Line($"{indent1}\tif (!times.Validate(_callCount))");
        w.Line($"{indent1}\t\tthrow new global::KnockOff.VerificationException(new global::KnockOff.VerificationFailure(\"method\", times, _callCount));");
        w.Line($"{indent1}}}");
        w.Line();

        // Verifiable state fields
        w.Line($"{indent1}private bool _isVerifiable;");
        w.Line($"{indent1}private global::KnockOff.Times? _verifiableTimes;");
        w.Line();

        // Verifiable methods
        w.Line($"{indent1}/// <summary>Marks for verification by Stub.Verify(). Returns this for fluent chaining.</summary>");
        w.Line($"{indent1}public global::KnockOff.IMethodTracking Verifiable() {{ _isVerifiable = true; _verifiableTimes = null; return this; }}");
        w.Line();

        w.Line($"{indent1}/// <summary>Marks for verification by Stub.Verify() with Times constraint. Returns this for fluent chaining.</summary>");
        w.Line($"{indent1}public global::KnockOff.IMethodTracking Verifiable(global::KnockOff.Times times) {{ _isVerifiable = true; _verifiableTimes = times; return this; }}");
        w.Line();

        // Internal verification support for stub-level Verify/VerifyAll
        w.Line($"{indent1}internal bool IsVerifiable => _isVerifiable;");
        w.Line($"{indent1}internal bool IsConfigured => _onCall != null;");
        w.Line();

        w.Line($"{indent1}/// <summary>Checks verification for Stub.Verify() - only checks if marked verifiable.</summary>");
        w.Line($"{indent1}internal global::KnockOff.VerificationFailure? CheckVerification()");
        w.Line($"{indent1}{{");
        w.Line($"{indent1}\tif (!_isVerifiable) return null;");
        w.Line($"{indent1}\tvar times = _verifiableTimes ?? global::KnockOff.Times.AtLeastOnce;");
        w.Line($"{indent1}\treturn times.Validate(_callCount) ? null : new global::KnockOff.VerificationFailure(\"{method.MethodName}\", times, _callCount);");
        w.Line($"{indent1}}}");
        w.Line();

        w.Line($"{indent1}/// <summary>Checks verification for Stub.VerifyAll() - checks if configured.</summary>");
        w.Line($"{indent1}internal global::KnockOff.VerificationFailure? CheckVerificationAll()");
        w.Line($"{indent1}{{");
        w.Line($"{indent1}\tif (!IsConfigured) return null;");
        w.Line($"{indent1}\treturn _callCount >= 1 ? null : new global::KnockOff.VerificationFailure(\"{method.MethodName}\", global::KnockOff.Times.AtLeastOnce, _callCount);");
        w.Line($"{indent1}}}");

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
        w.Line($"{indent1}private int _addCount;");
        w.Line();
        w.Line($"{indent1}/// <summary>Number of times the event was unsubscribed from.</summary>");
        w.Line($"{indent1}private int _removeCount;");
        w.Line();
        w.Line($"{indent1}/// <summary>The backing delegate for raising the event.</summary>");
        w.Line($"{indent1}public {evt.DelegateType}? Handler {{ get; private set; }}");
        w.Line();
        w.Line($"{indent1}/// <summary>Records an event subscription.</summary>");
        w.Line($"{indent1}public void RecordAdd({evt.DelegateType}? handler) {{ _addCount++; Handler = ({evt.DelegateType}?)global::System.Delegate.Combine(Handler, handler); }}");
        w.Line();
        w.Line($"{indent1}/// <summary>Records an event unsubscription.</summary>");
        w.Line($"{indent1}public void RecordRemove({evt.DelegateType}? handler) {{ _removeCount++; Handler = ({evt.DelegateType}?)global::System.Delegate.Remove(Handler, handler); }}");
        w.Line();
        w.Line($"{indent1}/// <summary>Resets tracking state (counts, Handler) but preserves verifiable marking.</summary>");
        w.Line($"{indent1}public void Reset() {{ _addCount = 0; _removeCount = 0; Handler = null; }}");
        w.Line();

        // Verification API for events
        w.Line($"{indent1}private bool _isVerifiable;");
        w.Line($"{indent1}private global::KnockOff.Times? _verifiableTimes;");
        w.Line();

        w.Line($"{indent1}/// <summary>Verifies the event was subscribed to at least once.</summary>");
        w.Line($"{indent1}public void VerifyAdd() => VerifyAdd(global::KnockOff.Times.AtLeastOnce);");
        w.Line();

        w.Line($"{indent1}/// <summary>Verifies the event subscription count matches the Times constraint.</summary>");
        w.Line($"{indent1}public void VerifyAdd(global::KnockOff.Times times)");
        w.Line($"{indent1}{{");
        w.Line($"{indent1}\tif (!times.Validate(_addCount))");
        w.Line($"{indent1}\t\tthrow new global::KnockOff.VerificationException($\"Event '{evt.EventName}' add verification failed: expected {{times}}, but was called {{_addCount}} time(s).\");");
        w.Line($"{indent1}}}");
        w.Line();

        w.Line($"{indent1}/// <summary>Verifies the event was unsubscribed at least once.</summary>");
        w.Line($"{indent1}public void VerifyRemove() => VerifyRemove(global::KnockOff.Times.AtLeastOnce);");
        w.Line();

        w.Line($"{indent1}/// <summary>Verifies the event unsubscription count matches the Times constraint.</summary>");
        w.Line($"{indent1}public void VerifyRemove(global::KnockOff.Times times)");
        w.Line($"{indent1}{{");
        w.Line($"{indent1}\tif (!times.Validate(_removeCount))");
        w.Line($"{indent1}\t\tthrow new global::KnockOff.VerificationException($\"Event '{evt.EventName}' remove verification failed: expected {{times}}, but was called {{_removeCount}} time(s).\");");
        w.Line($"{indent1}}}");
        w.Line();

        w.Line($"{indent1}/// <summary>Verifies the event was accessed (add or remove) at least once.</summary>");
        w.Line($"{indent1}public void Verify() => Verify(global::KnockOff.Times.AtLeastOnce);");
        w.Line();

        w.Line($"{indent1}/// <summary>Verifies the total event access count matches the Times constraint.</summary>");
        w.Line($"{indent1}public void Verify(global::KnockOff.Times times)");
        w.Line($"{indent1}{{");
        w.Line($"{indent1}\tvar totalCount = _addCount + _removeCount;");
        w.Line($"{indent1}\tif (!times.Validate(totalCount))");
        w.Line($"{indent1}\t\tthrow new global::KnockOff.VerificationException($\"Event '{evt.EventName}' verification failed: expected {{times}}, but was called {{totalCount}} time(s).\");");
        w.Line($"{indent1}}}");
        w.Line();

        w.Line($"{indent1}/// <summary>Marks this event for verification by Stub.Verify(). Returns this for fluent chaining.</summary>");
        w.Line($"{indent1}public {evt.InterceptorClassName}{evt.TypeParameterList} Verifiable()");
        w.Line($"{indent1}{{");
        w.Line($"{indent1}\t_isVerifiable = true;");
        w.Line($"{indent1}\t_verifiableTimes = global::KnockOff.Times.AtLeastOnce;");
        w.Line($"{indent1}\treturn this;");
        w.Line($"{indent1}}}");
        w.Line();

        w.Line($"{indent1}/// <summary>Marks this event for verification by Stub.Verify() with Times constraint. Returns this for fluent chaining.</summary>");
        w.Line($"{indent1}public {evt.InterceptorClassName}{evt.TypeParameterList} Verifiable(global::KnockOff.Times times)");
        w.Line($"{indent1}{{");
        w.Line($"{indent1}\t_isVerifiable = true;");
        w.Line($"{indent1}\t_verifiableTimes = times;");
        w.Line($"{indent1}\treturn this;");
        w.Line($"{indent1}}}");
        w.Line();

        // Internal verification methods for stub-level Verify()/VerifyAll()
        w.Line($"{indent1}internal bool IsVerifiable => _isVerifiable;");
        w.Line($"{indent1}internal bool IsConfigured => Handler != null;");
        w.Line();

        w.Line($"{indent1}/// <summary>Checks verification for Stub.Verify() - only verifiable items.</summary>");
        w.Line($"{indent1}internal global::KnockOff.VerificationFailure? CheckVerification()");
        w.Line($"{indent1}{{");
        w.Line($"{indent1}\tif (!_isVerifiable) return null;");
        w.Line($"{indent1}\tvar times = _verifiableTimes ?? global::KnockOff.Times.AtLeastOnce;");
        w.Line($"{indent1}\tvar totalCount = _addCount + _removeCount;");
        w.Line($"{indent1}\treturn times.Validate(totalCount) ? null : new global::KnockOff.VerificationFailure(\"{evt.EventName}\", times, totalCount);");
        w.Line($"{indent1}}}");
        w.Line();

        w.Line($"{indent1}/// <summary>Checks verification for Stub.VerifyAll() - all configured items.</summary>");
        w.Line($"{indent1}internal global::KnockOff.VerificationFailure? CheckVerificationAll()");
        w.Line($"{indent1}{{");
        w.Line($"{indent1}\tif (!IsConfigured && !_isVerifiable) return null;");
        w.Line($"{indent1}\tvar times = _verifiableTimes ?? global::KnockOff.Times.AtLeastOnce;");
        w.Line($"{indent1}\tvar totalCount = _addCount + _removeCount;");
        w.Line($"{indent1}\treturn times.Validate(totalCount) ? null : new global::KnockOff.VerificationFailure(\"{evt.EventName}\", times, totalCount);");
        w.Line($"{indent1}}}");

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

    private static void RenderClassVerifyMethods(CodeWriter w, InlineClassStubModel cls, string indent, string indent1)
    {
        // Get unique interceptor property names (the actual property names on the stub class)
        var interceptorPropertyNames = cls.InterceptorProperties
            .Select(p => p.PropertyName)
            .Distinct()
            .ToList();

        // Verify method - checks only .Verifiable() items, throws if any fail
        w.Line($"{indent}/// <summary>Verifies all members marked with .Verifiable() were invoked as expected. Throws VerificationException with all failures if any fail.</summary>");
        w.Line($"{indent}public void Verify()");
        w.Line($"{indent}{{");
        w.Line($"{indent1}var failures = new global::System.Collections.Generic.List<global::KnockOff.VerificationFailure>();");
        w.Line();

        // Check all interceptors (methods and properties share the same pattern)
        foreach (var name in interceptorPropertyNames)
        {
            w.Line($"{indent1}if ({name}.CheckVerification() is {{ }} {name.ToLowerInvariant()}Failure) failures.Add({name.ToLowerInvariant()}Failure);");
        }

        w.Line();
        w.Line($"{indent1}if (failures.Count > 0)");
        w.Line($"{indent1}\tthrow new global::KnockOff.VerificationException(failures);");
        w.Line($"{indent}}}");
        w.Line();

        // VerifyAll method - checks ALL configured members, throws if any fail
        w.Line($"{indent}/// <summary>Verifies ALL configured members were invoked at least once. Throws VerificationException with all failures if any fail.</summary>");
        w.Line($"{indent}public void VerifyAll()");
        w.Line($"{indent}{{");
        w.Line($"{indent1}var failures = new global::System.Collections.Generic.List<global::KnockOff.VerificationFailure>();");
        w.Line();

        // Check all configured interceptors
        foreach (var name in interceptorPropertyNames)
        {
            w.Line($"{indent1}if ({name}.CheckVerificationAll() is {{ }} {name.ToLowerInvariant()}Failure) failures.Add({name.ToLowerInvariant()}Failure);");
        }

        w.Line();
        w.Line($"{indent1}if (failures.Count > 0)");
        w.Line($"{indent1}\tthrow new global::KnockOff.VerificationException(failures);");
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
            if (prop.IsAbstract)
            {
                // Abstract: always use InvokeGet (no base to fall back to)
                w.Line($"{indent2}if (_stub == null) return default!;");
                w.Line($"{indent2}return _stub.{prop.PropertyName}.InvokeGet(_stub.Strict);");
            }
            else
            {
                // Virtual: always track via InvokeGet, but also fall back to base if not configured
                w.Line($"{indent2}if (_stub == null) return base.{prop.PropertyName};");
                w.Line($"{indent2}if (_stub.{prop.PropertyName}.IsConfigured) return _stub.{prop.PropertyName}.InvokeGet(_stub.Strict);");
                // Not configured: track the unconfigured call, then return base value
                w.Line($"{indent2}_stub.{prop.PropertyName}.InvokeGet(_stub.Strict);");
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
            if (prop.IsAbstract)
            {
                // Abstract: always use InvokeSet (no base to fall back to)
                w.Line($"{indent2}if (_stub == null) return;");
                w.Line($"{indent2}_stub.{prop.PropertyName}.InvokeSet(_stub.Strict, value);");
            }
            else
            {
                // Virtual: always track via InvokeSet, but also delegate to base if not configured
                w.Line($"{indent2}if (_stub == null) {{ base.{prop.PropertyName} = value; return; }}");
                w.Line($"{indent2}_stub.{prop.PropertyName}.InvokeSet(_stub.Strict, value);");
                w.Line($"{indent2}if (!_stub.{prop.PropertyName}.IsConfigured) base.{prop.PropertyName} = value;");
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
            if (indexer.IsAbstract)
            {
                // Abstract: always use InvokeGet (no base to fall back to)
                var defaultExpr = indexer.IsNullable ? "default" : GetDefaultForType(indexer.ReturnType, indexer.DefaultStrategy, indexer.ConcreteTypeForNew);
                w.Line($"{indent2}if (_stub == null) return {defaultExpr};");
                w.Line($"{indent2}return _stub.{indexer.IndexerName}.InvokeGet(_stub.Strict, {indexer.ArgumentList});");
            }
            else
            {
                // Virtual: always track via InvokeGet, but also fall back to base if not configured
                w.Line($"{indent2}if (_stub == null) return base[{indexer.ArgumentList}];");
                w.Line($"{indent2}if (_stub.{indexer.IndexerName}.IsConfigured) return _stub.{indexer.IndexerName}.InvokeGet(_stub.Strict, {indexer.ArgumentList});");
                // Not configured: track the unconfigured call, then return base value
                w.Line($"{indent2}_stub.{indexer.IndexerName}.InvokeGet(_stub.Strict, {indexer.ArgumentList});");
                w.Line($"{indent2}return base[{indexer.ArgumentList}];");
            }
            w.Line($"{indent1}}}");
        }

        if (indexer.HasSetter)
        {
            w.Line($"{indent1}set");
            w.Line($"{indent1}{{");
            // Handle calls from base constructor when _stub is null
            if (indexer.IsAbstract)
            {
                // Abstract: always use InvokeSet (no base to fall back to)
                w.Line($"{indent2}if (_stub == null) return;");
                w.Line($"{indent2}_stub.{indexer.IndexerName}.InvokeSet(_stub.Strict, {indexer.ArgumentList}, value);");
            }
            else
            {
                // Virtual: always track via InvokeSet, but also delegate to base if not configured
                w.Line($"{indent2}if (_stub == null) {{ base[{indexer.ArgumentList}] = value; return; }}");
                w.Line($"{indent2}_stub.{indexer.IndexerName}.InvokeSet(_stub.Strict, {indexer.ArgumentList}, value);");
                w.Line($"{indent2}if (!_stub.{indexer.IndexerName}.IsConfigured) base[{indexer.ArgumentList}] = value;");
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

        // Check for Callback (null check for calls during base constructor)
        if (method.IsVoid || method.IsTask || method.IsValueTask)
        {
            w.Line($"{indent1}if (_stub?.{method.HandlerName}.Callback is {{ }} onCall) {{ onCall({method.OnCallArgumentList}); return; }}");
        }
        else
        {
            w.Line($"{indent1}if (_stub?.{method.HandlerName}.Callback is {{ }} onCall) return onCall({method.OnCallArgumentList});");
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

    #region Model Adapters

    /// <summary>
    /// Converts an InlineClassPropertyModel to a UnifiedPropertyInterceptorModel for shared rendering.
    /// </summary>
    private static UnifiedPropertyInterceptorModel ToUnifiedPropertyModel(InlineClassPropertyModel prop)
    {
        return new UnifiedPropertyInterceptorModel(
            InterceptorClassName: prop.InterceptorClassName,
            PropertyName: prop.PropertyName,
            DeclaringInterface: "", // Class stubs don't have a declaring interface for Source(T) feature
            ValueType: prop.ReturnType,
            NullableValueType: prop.NullableReturnType,
            DefaultExpression: "default!",
            HasGetter: prop.HasGetter,
            HasSetter: prop.HasSetter,
            IsInitOnly: false);
    }

    /// <summary>
    /// Converts an InlineClassIndexerModel to a UnifiedIndexerInterceptorModel for shared rendering.
    /// </summary>
    private static UnifiedIndexerInterceptorModel ToUnifiedIndexerModel(InlineClassIndexerModel indexer)
    {
        var paramTypes = indexer.ParameterDeclarations.Split(',').Select(p => p.Trim().Split(' ')[0]).ToArray();
        var paramList = string.Join(", ", paramTypes);

        return new UnifiedIndexerInterceptorModel(
            InterceptorClassName: indexer.InterceptorClassName,
            IndexerName: indexer.IndexerName,
            DeclaringInterface: "", // Class stubs don't have a declaring interface for Source(T) feature
            KeyType: indexer.KeyType,
            NullableKeyType: MakeNullable(indexer.KeyType),
            KeyParamName: "key", // Extracted from parameter declarations
            SingleKeyType: indexer.KeyType.StartsWith("(") ? indexer.KeyType : indexer.KeyType, // Handle tuple keys
            ValueType: indexer.ReturnType,
            NullableValueType: MakeNullable(indexer.ReturnType),
            DefaultExpression: "default!",
            HasGetter: indexer.HasGetter,
            HasSetter: indexer.HasSetter,
            ParameterSignature: indexer.ParameterDeclarations,
            ParameterTypes: paramList,
            KeyExpression: indexer.KeyExpression);
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
