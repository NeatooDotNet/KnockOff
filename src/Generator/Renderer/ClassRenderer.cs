// src/Generator/Renderer/ClassRenderer.cs
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using KnockOff;
using KnockOff.Builder;
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

        // Use shared MethodInterceptorRenderer for method interceptors
        foreach (var method in cls.Methods)
        {
            var options = new InterceptorRenderOptions(
                BaseIndent: 2,
                IncludeStrictParameter: true,
                StrictAccessExpression: "strict",
                InterceptorTypeParameters: cls.TypeParameterList,
                InterceptorConstraints: cls.ConstraintClauses);
            w.SetIndent(2);
            MethodInterceptorRenderer.RenderInterceptorClass(w, method, options);
        }

        // Render generic method handler interceptor classes (Of<T>() pattern)
        foreach (var handler in cls.GenericMethodHandlers)
        {
            RenderClassGenericMethodHandler(w, handler, indent);
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
            var newKeyword = interceptorProp.NeedsNewKeyword ? "new " : "";
            w.Line($"{indent1}/// <summary>{interceptorProp.Description}</summary>");
            w.Line($"{indent1}public {newKeyword}{interceptorProp.InterceptorTypeName} {interceptorProp.PropertyName} {{ get; }} = new();");
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

    #region Event Interceptor Rendering

    private static void RenderEventInterceptorClass(CodeWriter w, InlineClassEventModel evt, string stubClassName, string indent)
    {
        var indent1 = indent + "\t";

        w.Line($"{indent}/// <summary>Interceptor for {stubClassName}.{evt.EventName}.</summary>");
        w.Line($"{indent}public sealed class {evt.InterceptorClassName}{evt.TypeParameterList}{evt.ConstraintClauses}");
        w.Line($"{indent}{{");

        // Backing field
        w.Line($"{indent1}private {evt.DelegateType}? _handler;");
        w.Line();

        // Add/Remove tracking
        w.Line($"{indent1}private int _addCount;");
        w.Line();
        w.Line($"{indent1}private int _removeCount;");
        w.Line();

        w.Line($"{indent1}/// <summary>Whether any handlers are subscribed.</summary>");
        w.Line($"{indent1}public bool HasSubscribers => _handler != null;");
        w.Line();

        w.Line($"{indent1}/// <summary>Records an event subscription.</summary>");
        w.Line($"{indent1}public void RecordAdd({evt.DelegateType}? value) {{ _addCount++; _handler = ({evt.DelegateType}?)global::System.Delegate.Combine(_handler, value); }}");
        w.Line();
        w.Line($"{indent1}/// <summary>Records an event unsubscription.</summary>");
        w.Line($"{indent1}public void RecordRemove({evt.DelegateType}? value) {{ _removeCount++; _handler = ({evt.DelegateType}?)global::System.Delegate.Remove(_handler, value); }}");
        w.Line();

        // Raise method
        RenderEventRaiseMethod(w, evt, indent1);

        w.Line($"{indent1}/// <summary>Resets tracking state (counts, handler) but preserves verifiable marking.</summary>");
        w.Line($"{indent1}public void Reset() {{ _addCount = 0; _removeCount = 0; _handler = null; }}");
        w.Line();

        // Verification API for events
        w.Line($"{indent1}private bool _isVerifiable;");
        w.Line($"{indent1}private global::KnockOff.Called? _verifiableTimes;");
        w.Line();

        w.Line($"{indent1}/// <summary>Verifies the event was subscribed to at least once.</summary>");
        w.Line($"{indent1}public void VerifyAdd() => VerifyAdd(global::KnockOff.Called.AtLeastOnce);");
        w.Line();

        w.Line($"{indent1}/// <summary>Verifies the event subscription count matches the Called constraint.</summary>");
        w.Line($"{indent1}public void VerifyAdd(global::KnockOff.Called times)");
        w.Line($"{indent1}{{");
        w.Line($"{indent1}\tif (!times.Validate(_addCount))");
        w.Line($"{indent1}\t\tthrow new global::KnockOff.VerificationException($\"Event '{evt.EventName}' add verification failed: expected {{times}}, but was called {{_addCount}} time(s).\");");
        w.Line($"{indent1}}}");
        w.Line();

        w.Line($"{indent1}/// <summary>Verifies the event was unsubscribed at least once.</summary>");
        w.Line($"{indent1}public void VerifyRemove() => VerifyRemove(global::KnockOff.Called.AtLeastOnce);");
        w.Line();

        w.Line($"{indent1}/// <summary>Verifies the event unsubscription count matches the Called constraint.</summary>");
        w.Line($"{indent1}public void VerifyRemove(global::KnockOff.Called times)");
        w.Line($"{indent1}{{");
        w.Line($"{indent1}\tif (!times.Validate(_removeCount))");
        w.Line($"{indent1}\t\tthrow new global::KnockOff.VerificationException($\"Event '{evt.EventName}' remove verification failed: expected {{times}}, but was called {{_removeCount}} time(s).\");");
        w.Line($"{indent1}}}");
        w.Line();

        w.Line($"{indent1}/// <summary>Verifies the event was accessed (add or remove) at least once.</summary>");
        w.Line($"{indent1}public void Verify() => Verify(global::KnockOff.Called.AtLeastOnce);");
        w.Line();

        w.Line($"{indent1}/// <summary>Verifies the total event access count matches the Called constraint.</summary>");
        w.Line($"{indent1}public void Verify(global::KnockOff.Called times)");
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
        w.Line($"{indent1}\t_verifiableTimes = global::KnockOff.Called.AtLeastOnce;");
        w.Line($"{indent1}\treturn this;");
        w.Line($"{indent1}}}");
        w.Line();

        w.Line($"{indent1}/// <summary>Marks this event for verification by Stub.Verify() with Called constraint. Returns this for fluent chaining.</summary>");
        w.Line($"{indent1}public {evt.InterceptorClassName}{evt.TypeParameterList} Verifiable(global::KnockOff.Called times)");
        w.Line($"{indent1}{{");
        w.Line($"{indent1}\t_isVerifiable = true;");
        w.Line($"{indent1}\t_verifiableTimes = times;");
        w.Line($"{indent1}\treturn this;");
        w.Line($"{indent1}}}");
        w.Line();

        // Internal verification methods for stub-level Verify()/VerifyAll()
        w.Line($"{indent1}internal bool IsVerifiable => _isVerifiable;");
        w.Line($"{indent1}internal bool IsConfigured => _handler != null;");
        w.Line();

        w.Line($"{indent1}/// <summary>Checks verification for Stub.Verify() - only verifiable items.</summary>");
        w.Line($"{indent1}internal global::KnockOff.VerificationFailure? CheckVerification()");
        w.Line($"{indent1}{{");
        w.Line($"{indent1}\tif (!_isVerifiable) return null;");
        w.Line($"{indent1}\tvar times = _verifiableTimes ?? global::KnockOff.Called.AtLeastOnce;");
        w.Line($"{indent1}\tvar totalCount = _addCount + _removeCount;");
        w.Line($"{indent1}\treturn times.Validate(totalCount) ? null : new global::KnockOff.VerificationFailure(\"{evt.EventName}\", times, totalCount);");
        w.Line($"{indent1}}}");
        w.Line();

        w.Line($"{indent1}/// <summary>Checks verification for Stub.VerifyAll() - all configured items.</summary>");
        w.Line($"{indent1}internal global::KnockOff.VerificationFailure? CheckVerificationAll()");
        w.Line($"{indent1}{{");
        w.Line($"{indent1}\tif (!IsConfigured && !_isVerifiable) return null;");
        w.Line($"{indent1}\tvar times = _verifiableTimes ?? global::KnockOff.Called.AtLeastOnce;");
        w.Line($"{indent1}\tvar totalCount = _addCount + _removeCount;");
        w.Line($"{indent1}\treturn times.Validate(totalCount) ? null : new global::KnockOff.VerificationFailure(\"{evt.EventName}\", times, totalCount);");
        w.Line($"{indent1}}}");

        w.Line($"{indent}}}");
        w.Line();
    }

    private static void RenderEventRaiseMethod(CodeWriter w, InlineClassEventModel evt, string indent)
    {
        if (evt.RaiseReturnsValue)
        {
            // Func-style delegate
            w.Line($"{indent}/// <summary>Raises the event with the specified arguments and returns the result.</summary>");
            if (string.IsNullOrEmpty(evt.RaiseParameters))
            {
                w.Line($"{indent}public {evt.RaiseReturnType} Raise() => _handler != null ? _handler.Invoke() : default!;");
            }
            else
            {
                w.Line($"{indent}public {evt.RaiseReturnType} Raise({evt.RaiseParameters}) => _handler != null ? _handler.Invoke({evt.RaiseArguments}) : default!;");
            }
        }
        else if (evt.UsesDynamicInvoke)
        {
            // Custom delegate - use DynamicInvoke
            if (string.IsNullOrEmpty(evt.RaiseParameters))
            {
                w.Line($"{indent}/// <summary>Raises the event.</summary>");
                w.Line($"{indent}public void Raise() => (_handler as global::System.Action)?.Invoke();");
            }
            else
            {
                w.Line($"{indent}/// <summary>Invokes the handler if subscribed.</summary>");
                w.Line($"{indent}public void Raise({evt.RaiseParameters}) => _handler?.DynamicInvoke({evt.RaiseArguments});");
            }
        }
        else
        {
            // Standard Action/EventHandler
            if (string.IsNullOrEmpty(evt.RaiseParameters))
            {
                w.Line($"{indent}/// <summary>Raises the event.</summary>");
                w.Line($"{indent}public void Raise() => _handler?.Invoke();");
            }
            else
            {
                w.Line($"{indent}/// <summary>Raises the event with the specified arguments.</summary>");
                w.Line($"{indent}public void Raise({evt.RaiseParameters}) => _handler?.Invoke({evt.RaiseArguments});");
            }
        }
        w.Line();
    }

    #endregion

    #region Generic Method Handler Rendering

    /// <summary>
    /// Renders a generic method handler interceptor class for class stubs.
    /// Reuses the same Of&lt;T&gt;() pattern as the interface stub pipeline.
    /// </summary>
    internal static void RenderClassGenericMethodHandler(CodeWriter w, InlineGenericMethodHandlerModel handler, string indent, bool emitHelperInterfaces = false)
    {
        var indent1 = indent + "\t";
        var indent2 = indent + "\t\t";
        var indent3 = indent + "\t\t\t";
        var indent4 = indent + "\t\t\t\t";

        var ifaceTypeParamList = handler.InterfaceTypeParameterList;
        var ifaceConstraintClause = handler.InterfaceConstraintClauses;

        w.Line($"{indent}/// <summary>Interceptor for {handler.MethodName}.</summary>");
        w.Line($"{indent}public sealed class {handler.InterceptorClassName}{ifaceTypeParamList}{ifaceConstraintClause}");
        w.Line($"{indent}{{");

        // For standalone stubs, emit helper interfaces inside each interceptor class
        // to avoid namespace-level collisions between multiple stub files
        if (emitHelperInterfaces)
        {
            w.Line($"{indent1}private interface IGenericMethodCallTracker {{ int CallCount {{ get; }} }}");
            w.Line($"{indent1}private interface IResettable {{ void Reset(); }}");
            w.Line();
        }

        // Dictionary for typed handlers
        w.Line($"{indent1}private readonly global::System.Collections.Generic.Dictionary<{handler.KeyType}, object> _typedHandlers = new();");
        w.Line();

        // Of<T>() method
        w.Line($"{indent1}/// <summary>Gets the typed handler for the specified type argument(s).</summary>");
        w.Line($"{indent1}public {handler.TypedHandlerClassName}<{handler.TypeParameterNames}> Of<{handler.TypeParameterNames}>(){handler.MethodConstraintClauses}");
        w.Line($"{indent1}{{");
        w.Line($"{indent2}var key = {handler.KeyConstruction};");
        w.Line($"{indent2}if (!_typedHandlers.TryGetValue(key, out var handler))");
        w.Line($"{indent2}{{");
        w.Line($"{indent3}handler = new {handler.TypedHandlerClassName}<{handler.TypeParameterNames}>();");
        w.Line($"{indent3}_typedHandlers[key] = handler;");
        w.Line($"{indent2}}}");
        w.Line($"{indent2}return ({handler.TypedHandlerClassName}<{handler.TypeParameterNames}>)handler;");
        w.Line($"{indent1}}}");
        w.Line();

        // Aggregate tracking
        w.Line($"{indent1}private int TotalCallCount => _typedHandlers.Values.Cast<IGenericMethodCallTracker>().Sum(h => h.CallCount);");
        w.Line();
        w.Line($"{indent1}/// <summary>All type argument(s) that were used in calls.</summary>");
        w.Line($"{indent1}public global::System.Collections.Generic.IReadOnlyList<{handler.KeyType}> CalledTypeArguments => _typedHandlers.Where(kvp => ((IGenericMethodCallTracker)kvp.Value).CallCount > 0).Select(kvp => kvp.Key).ToList();");
        w.Line();

        // Reset method
        w.Line($"{indent1}/// <summary>Resets tracking state (call counts) but preserves configuration (Return/Call callbacks).</summary>");
        w.Line($"{indent1}public void Reset()");
        w.Line($"{indent1}{{");
        w.Line($"{indent2}foreach (var handler in _typedHandlers.Values.Cast<IResettable>())");
        w.Line($"{indent3}handler.Reset();");
        w.Line($"{indent1}}}");
        w.Line();

        // Verify methods
        w.Line($"{indent1}/// <summary>Verifies method was called at least once with any type argument. Throws VerificationException if not.</summary>");
        w.Line($"{indent1}public void Verify() => Verify(global::KnockOff.Called.AtLeastOnce);");
        w.Line();

        w.Line($"{indent1}/// <summary>Verifies total call count satisfies the Called constraint. Throws VerificationException if not.</summary>");
        w.Line($"{indent1}public void Verify(global::KnockOff.Called times)");
        w.Line($"{indent1}{{");
        w.Line($"{indent2}if (!times.Validate(TotalCallCount))");
        w.Line($"{indent3}throw new global::KnockOff.VerificationException(new global::KnockOff.VerificationFailure(\"{handler.MethodName}\", times, TotalCallCount));");
        w.Line($"{indent1}}}");
        w.Line();

        // Internal verification support
        w.Line($"{indent1}internal bool IsVerifiable => false;");
        w.Line($"{indent1}internal bool IsConfigured => _typedHandlers.Count > 0;");
        w.Line();

        w.Line($"{indent1}/// <summary>Checks verification for Stub.Verify() - only checks if marked verifiable.</summary>");
        w.Line($"{indent1}internal global::KnockOff.VerificationFailure? CheckVerification() => null;");
        w.Line();

        w.Line($"{indent1}/// <summary>Checks verification for Stub.VerifyAll() - checks if configured.</summary>");
        w.Line($"{indent1}internal global::KnockOff.VerificationFailure? CheckVerificationAll()");
        w.Line($"{indent1}{{");
        w.Line($"{indent2}if (!IsConfigured) return null;");
        w.Line($"{indent2}return TotalCallCount >= 1 ? null : new global::KnockOff.VerificationFailure(\"{handler.MethodName}\", global::KnockOff.Called.AtLeastOnce, TotalCallCount);");
        w.Line($"{indent1}}}");
        w.Line();

        // Nested Typed Handler Class
        RenderClassTypedHandlerClass(w, handler, indent1);

        w.Line($"{indent}}}");
        w.Line();
    }

    internal static void RenderClassTypedHandlerClass(CodeWriter w, InlineGenericMethodHandlerModel handler, string indent)
    {
        var indent1 = indent + "\t";
        var indent2 = indent + "\t\t";

        w.Line($"{indent}/// <summary>Typed handler for {handler.MethodName} with specific type arguments.</summary>");
        w.Line($"{indent}public sealed class {handler.TypedHandlerClassName}<{handler.TypeParameterNames}> : IGenericMethodCallTracker, IResettable, global::KnockOff.IMethodTracking{handler.MethodConstraintClauses}");
        w.Line($"{indent}{{");

        // Delegate
        w.Line($"{indent1}/// <summary>Delegate for {handler.MethodName}.</summary>");
        w.Line($"{indent1}{handler.DelegateSignature}");
        w.Line();

        // Private callback field
        w.Line($"{indent1}private {handler.MethodName}Delegate? _call;");
        w.Line();

        // CallCount
        w.Line($"{indent1}private int _callCount;");
        w.Line($"{indent1}int IGenericMethodCallTracker.CallCount => _callCount;");
        w.Line();

        // LastArg/LastArgs
        if (handler.LastCallArgType != null)
        {
            var param = handler.NonGenericParameters.GetArray()![0];
            w.Line($"{indent1}/// <summary>The '{param.Name}' argument from the most recent call.</summary>");
            w.Line($"{indent1}public {handler.LastCallArgType} LastArg {{ get; private set; }}");
            w.Line();
        }
        else if (handler.LastCallArgsType != null)
        {
            w.Line($"{indent1}/// <summary>The arguments from the most recent call.</summary>");
            w.Line($"{indent1}public {handler.LastCallArgsType} LastArgs {{ get; private set; }}");
            w.Line();
        }

        // Return/Call method
        var typedHandlerEntryPoint = handler.IsVoid ? "Call" : "Return";
        w.Line($"{indent1}/// <summary>Sets the callback invoked when this method is called. Returns this handler for tracking.</summary>");
        w.Line($"{indent1}public global::KnockOff.IMethodTracking {typedHandlerEntryPoint}({handler.MethodName}Delegate callback) {{ _call = callback; return this; }}");
        w.Line();

        // Callback property
        w.Line($"{indent1}/// <summary>Gets the configured callback (internal use).</summary>");
        w.Line($"{indent1}internal {handler.MethodName}Delegate? Callback => _call;");
        w.Line();

        // RecordCall
        w.Line($"{indent1}/// <summary>Records a method call.</summary>");
        if (handler.NonGenericParameters.Count == 0)
        {
            w.Line($"{indent1}public void RecordCall() => _callCount++;");
        }
        else if (handler.NonGenericParameters.Count == 1)
        {
            var param = handler.NonGenericParameters.GetArray()![0];
            w.Line($"{indent1}public void RecordCall({param.Type} {param.Name}) {{ _callCount++; LastArg = {param.Name}; }}");
        }
        else
        {
            var paramList = string.Join(", ", handler.NonGenericParameters.Select(p => $"{p.Type} {p.Name}"));
            var tupleConstruction = string.Join(", ", handler.NonGenericParameters.Select(p => p.Name));
            w.Line($"{indent1}public void RecordCall({paramList}) {{ _callCount++; LastArgs = ({tupleConstruction}); }}");
        }
        w.Line();

        // Reset
        w.Line($"{indent1}/// <summary>Resets tracking state but preserves configuration.</summary>");
        if (handler.NonGenericParameters.Count == 0)
        {
            w.Line($"{indent1}public void Reset() {{ _callCount = 0; }}");
        }
        else if (handler.NonGenericParameters.Count == 1)
        {
            w.Line($"{indent1}public void Reset() {{ _callCount = 0; LastArg = default; }}");
        }
        else
        {
            w.Line($"{indent1}public void Reset() {{ _callCount = 0; LastArgs = default; }}");
        }
        w.Line();

        // Verify methods
        w.Line($"{indent1}/// <summary>Verifies call count is at least once. Throws VerificationException if not.</summary>");
        w.Line($"{indent1}public void Verify() => Verify(global::KnockOff.Called.AtLeastOnce);");
        w.Line();

        w.Line($"{indent1}/// <summary>Verifies call count satisfies the Called constraint. Throws VerificationException if not.</summary>");
        w.Line($"{indent1}public void Verify(global::KnockOff.Called times)");
        w.Line($"{indent1}{{");
        w.Line($"{indent2}if (!times.Validate(_callCount))");
        w.Line($"{indent2}\tthrow new global::KnockOff.VerificationException(new global::KnockOff.VerificationFailure(\"method\", times, _callCount));");
        w.Line($"{indent1}}}");
        w.Line();

        // Verifiable methods
        w.Line($"{indent1}/// <summary>Marks for verification by Stub.Verify(). Returns this for fluent chaining.</summary>");
        w.Line($"{indent1}public global::KnockOff.IMethodTracking Verifiable() => this;");
        w.Line();

        w.Line($"{indent1}/// <summary>Marks for verification by Stub.Verify() with Called constraint. Returns this for fluent chaining.</summary>");
        w.Line($"{indent1}public global::KnockOff.IMethodTracking Verifiable(global::KnockOff.Called times) => this;");

        w.Line($"{indent}}}");
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

        // Default ref backing fields for abstract ref return members (_stub == null path during base constructor)
        foreach (var prop in cls.ImplProperties)
        {
            if (prop.IsRefReturn && prop.IsAbstract)
            {
                w.Line($"{indent1}private {prop.ReturnType} _defaultRefBacking_{prop.PropertyName};");
            }
        }
        foreach (var indexer in cls.ImplIndexers)
        {
            if (indexer.IsRefReturn && indexer.IsAbstract)
            {
                w.Line($"{indent1}private {indexer.ReturnType} _defaultRefBacking_{indexer.IndexerName};");
            }
        }
        foreach (var method in cls.ImplMethods)
        {
            if (method.IsRefReturn && method.IsAbstract)
            {
                w.Line($"{indent1}private {method.ReturnType} _defaultRefBacking_{method.MethodName};");
            }
        }
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
        w.Line($"{indent}{requiredKeyword}{prop.AccessModifier} override {prop.RefReturnPrefix}{prop.ReturnType} {prop.PropertyName}");
        w.Line($"{indent}{{");

        if (prop.IsRefReturn)
        {
            // Ref return properties are always get-only (C# constraint)
            w.Line($"{indent1}get");
            w.Line($"{indent1}{{");
            if (prop.IsAbstract)
            {
                w.Line($"{indent2}if (_stub == null) {{ _defaultRefBacking_{prop.PropertyName} = default!; return ref _defaultRefBacking_{prop.PropertyName}; }}");
                w.Line($"{indent2}_stub.{prop.PropertyName}.InvokeRefGet(_stub.Strict);");
                w.Line($"{indent2}return ref _stub.{prop.PropertyName}._refReturnBacking;");
            }
            else
            {
                // Virtual: IsConfigured-first pattern
                w.Line($"{indent2}if (_stub == null) return ref base.{prop.PropertyName};");
                w.Line($"{indent2}if (_stub.{prop.PropertyName}.IsConfigured)");
                w.Line($"{indent2}{{");
                w.Line($"{indent2}\t_stub.{prop.PropertyName}.InvokeRefGet(_stub.Strict);");
                w.Line($"{indent2}\treturn ref _stub.{prop.PropertyName}._refReturnBacking;");
                w.Line($"{indent2}}}");
                w.Line($"{indent2}_stub.{prop.PropertyName}.InvokeRefGet(_stub.Strict);");
                w.Line($"{indent2}return ref base.{prop.PropertyName};");
            }
            w.Line($"{indent1}}}");
        }
        else
        {
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
        }

        w.Line($"{indent}}}");
        w.Line();
    }

    private static void RenderImplIndexerOverride(CodeWriter w, InlineClassImplIndexerModel indexer, string indent, string indent1)
    {
        var indent2 = indent1 + "\t";

        w.Line($"{indent}/// <inheritdoc />");
        w.Line($"{indent}{indexer.AccessModifier} override {indexer.RefReturnPrefix}{indexer.ReturnType} this[{indexer.ParameterDeclarations}]");
        w.Line($"{indent}{{");

        if (indexer.IsRefReturn)
        {
            // Ref return indexers are always get-only (C# constraint)
            w.Line($"{indent1}get");
            w.Line($"{indent1}{{");
            if (indexer.IsAbstract)
            {
                w.Line($"{indent2}if (_stub == null) {{ _defaultRefBacking_{indexer.IndexerName} = default!; return ref _defaultRefBacking_{indexer.IndexerName}; }}");
                w.Line($"{indent2}_stub.{indexer.IndexerName}.InvokeRefGet(_stub.Strict, {indexer.ArgumentList});");
                w.Line($"{indent2}return ref _stub.{indexer.IndexerName}._refReturnBacking;");
            }
            else
            {
                // Virtual: IsConfigured-first pattern
                w.Line($"{indent2}if (_stub == null) return ref base[{indexer.ArgumentList}];");
                w.Line($"{indent2}if (_stub.{indexer.IndexerName}.IsConfigured)");
                w.Line($"{indent2}{{");
                w.Line($"{indent2}\t_stub.{indexer.IndexerName}.InvokeRefGet(_stub.Strict, {indexer.ArgumentList});");
                w.Line($"{indent2}\treturn ref _stub.{indexer.IndexerName}._refReturnBacking;");
                w.Line($"{indent2}}}");
                w.Line($"{indent2}_stub.{indexer.IndexerName}.InvokeRefGet(_stub.Strict, {indexer.ArgumentList});");
                w.Line($"{indent2}return ref base[{indexer.ArgumentList}];");
            }
            w.Line($"{indent1}}}");
        }
        else
        {
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
        }

        w.Line($"{indent}}}");
        w.Line();
    }

    private static void RenderImplMethodOverride(CodeWriter w, InlineClassImplMethodModel method, string indent, string indent1)
    {
        if (method.IsGenericMethod)
        {
            RenderImplGenericMethodOverride(w, method, indent, indent1);
            return;
        }

        w.Line($"{indent}/// <inheritdoc />");
        w.Line($"{indent}{method.AccessModifier} override {method.RefReturnPrefix}{method.ReturnType} {method.MethodName}({method.ParameterDeclarations})");
        w.Line($"{indent}{{");

        if (method.IsRefReturn)
        {
            // Ref return method override - uses InvokeRef + _refReturnBacking pattern
            var invokeRefMethodName = string.IsNullOrEmpty(method.InvokeSuffix)
                ? "InvokeRef"
                : $"InvokeRef{method.InvokeSuffix}";
            var backingField = string.IsNullOrEmpty(method.InvokeSuffix)
                ? "_refReturnBacking"
                : $"_refReturnBacking{method.InvokeSuffix}";
            var invokeArgs = "_stub.Strict" + (string.IsNullOrEmpty(method.InputArgumentList) ? "" : $", {method.InputArgumentList}");

            if (method.IsAbstract)
            {
                w.Line($"{indent1}if (_stub == null) {{ _defaultRefBacking_{method.MethodName} = default!; return ref _defaultRefBacking_{method.MethodName}; }}");
                w.Line($"{indent1}_stub.{method.HandlerName}.{invokeRefMethodName}({invokeArgs});");
                w.Line($"{indent1}return ref _stub.{method.HandlerName}.{backingField};");
            }
            else
            {
                // Virtual: IsConfigured-first pattern
                w.Line($"{indent1}if (_stub == null) return ref base.{method.MethodName}({method.ArgumentList});");
                w.Line($"{indent1}if (_stub.{method.HandlerName}.IsConfigured)");
                w.Line($"{indent1}{{");
                w.Line($"{indent1}\t_stub.{method.HandlerName}.{invokeRefMethodName}({invokeArgs});");
                w.Line($"{indent1}\treturn ref _stub.{method.HandlerName}.{backingField};");
                w.Line($"{indent1}}}");
                w.Line($"{indent1}_stub.{method.HandlerName}.{invokeRefMethodName}({invokeArgs});");
                w.Line($"{indent1}return ref base.{method.MethodName}({method.ArgumentList});");
            }
        }
        else
        {
            // Null check for calls during base constructor
            w.Line($"{indent1}if (_stub == null)");
            w.Line($"{indent1}{{");
            if (method.IsAbstract)
            {
                // Abstract - return default
                if (method.IsVoid)
                {
                    w.Line($"{indent1}\treturn;");
                }
                else if (method.IsTask)
                {
                    w.Line($"{indent1}\treturn global::System.Threading.Tasks.Task.CompletedTask;");
                }
                else if (method.IsValueTask)
                {
                    w.Line($"{indent1}\treturn default;");
                }
                else
                {
                    w.Line($"{indent1}\treturn default!;");
                }
            }
            else
            {
                // Virtual - delegate to base
                if (method.IsVoid)
                {
                    w.Line($"{indent1}\tbase.{method.MethodName}({method.ArgumentList});");
                    w.Line($"{indent1}\treturn;");
                }
                else
                {
                    w.Line($"{indent1}\treturn base.{method.MethodName}({method.ArgumentList});");
                }
            }
            w.Line($"{indent1}}}");
            w.Line();

            // Both single-signature and multi-overload interceptors use the shared MethodInterceptorRenderer
            // which generates Invoke/Invoke_{suffix} methods that handle everything internally.
            // For class stubs:
            // - Abstract methods: always use Invoke (no base to fall back to)
            // - Virtual methods: call Invoke, but check if it actually handled the call and fall back to base if not

            // Determine the invoke method name
            var invokeMethodName = string.IsNullOrEmpty(method.InvokeSuffix)
                ? "Invoke"
                : $"Invoke{method.InvokeSuffix}";

            // Build invoke arguments: strict, then method parameters
            var invokeArgs = "_stub.Strict" + (string.IsNullOrEmpty(method.InputArgumentList) ? "" : $", {method.InputArgumentList}");

            if (method.IsAbstract)
            {
                // Abstract methods: always use Invoke - it handles everything
                if (method.IsVoid)
                {
                    w.Line($"{indent1}_stub.{method.HandlerName}.{invokeMethodName}({invokeArgs});");
                }
                else
                {
                    w.Line($"{indent1}return _stub.{method.HandlerName}.{invokeMethodName}({invokeArgs});");
                }
            }
            else
            {
                // Virtual methods: track whether a real handler handled the call
                // The shared interceptor tracks unconfigured calls via UnconfiguredCallCount
                // If this counter increments during Invoke, nothing handled the call -> use base
                w.Line($"{indent1}var unconfiguredBefore = _stub.{method.HandlerName}.UnconfiguredCallCount;");
                if (method.IsVoid)
                {
                    w.Line($"{indent1}_stub.{method.HandlerName}.{invokeMethodName}({invokeArgs});");
                    w.Line($"{indent1}if (_stub.{method.HandlerName}.UnconfiguredCallCount > unconfiguredBefore)");
                    w.Line($"{indent1}{{");
                    w.Line($"{indent1}\tbase.{method.MethodName}({method.ArgumentList});");
                    w.Line($"{indent1}}}");
                }
                else
                {
                    w.Line($"{indent1}var result = _stub.{method.HandlerName}.{invokeMethodName}({invokeArgs});");
                    w.Line($"{indent1}if (_stub.{method.HandlerName}.UnconfiguredCallCount > unconfiguredBefore)");
                    w.Line($"{indent1}{{");
                    w.Line($"{indent1}\treturn base.{method.MethodName}({method.ArgumentList});");
                    w.Line($"{indent1}}}");
                    w.Line($"{indent1}return result;");
                }
            }
        }

        w.Line($"{indent}}}");
        w.Line();
    }

    /// <summary>
    /// Renders a generic method override in the Impl class using the Of&lt;T&gt;() handler pattern.
    /// </summary>
    internal static void RenderImplGenericMethodOverride(CodeWriter w, InlineClassImplMethodModel method, string indent, string indent1)
    {
        w.Line($"{indent}/// <inheritdoc />");
        // NOTE: No constraint clauses on override -- C# inherits them from the base method.
        w.Line($"{indent}{method.AccessModifier} override {method.ReturnType} {method.MethodName}{method.TypeParameterDecl}({method.ParameterDeclarations})");
        w.Line($"{indent}{{");

        // Null check for base constructor calls
        w.Line($"{indent1}if (_stub == null)");
        w.Line($"{indent1}{{");
        if (method.IsAbstract)
        {
            if (method.IsVoid)
            {
                w.Line($"{indent1}\treturn;");
            }
            else if (method.IsTask && !string.IsNullOrEmpty(method.TaskTypeArg))
            {
                w.Line($"{indent1}\treturn global::System.Threading.Tasks.Task.FromResult<{method.TaskTypeArg}>(default!);");
            }
            else if (method.IsTask)
            {
                w.Line($"{indent1}\treturn global::System.Threading.Tasks.Task.CompletedTask;");
            }
            else if (method.IsValueTask && !string.IsNullOrEmpty(method.TaskTypeArg))
            {
                w.Line($"{indent1}\treturn new global::System.Threading.Tasks.ValueTask<{method.TaskTypeArg}>(default!);");
            }
            else if (method.IsValueTask)
            {
                w.Line($"{indent1}\treturn default;");
            }
            else
            {
                w.Line($"{indent1}\treturn default!;");
            }
        }
        else
        {
            // Virtual - delegate to base
            if (method.IsVoid)
            {
                w.Line($"{indent1}\tbase.{method.MethodName}{method.TypeParameterDecl}({method.ArgumentList});");
                w.Line($"{indent1}\treturn;");
            }
            else
            {
                w.Line($"{indent1}\treturn base.{method.MethodName}{method.TypeParameterDecl}({method.ArgumentList});");
            }
        }
        w.Line($"{indent1}}}");
        w.Line();

        // Get typed handler via Of<T>()
        w.Line($"{indent1}var typedHandler = _stub.{method.HandlerName}{method.OfTypeAccess};");

        // Record the call -- NonGenericArgList excludes params typed with method-level type params
        if (string.IsNullOrEmpty(method.NonGenericArgList))
            w.Line($"{indent1}typedHandler.RecordCall();");
        else
            w.Line($"{indent1}typedHandler.RecordCall({method.NonGenericArgList});");

        // Check for callback
        w.Line($"{indent1}if (typedHandler.Callback is {{ }} callCallback)");
        if (method.IsVoid)
            w.Line($"{indent1}{{ callCallback({method.ArgumentList}); return; }}");
        else
            w.Line($"{indent1}\treturn callCallback({method.ArgumentList});");

        // Fallback for unconfigured calls
        if (!method.IsAbstract)
        {
            // Virtual: fall back to base
            if (method.IsVoid)
                w.Line($"{indent1}base.{method.MethodName}{method.TypeParameterDecl}({method.ArgumentList});");
            else
                w.Line($"{indent1}return base.{method.MethodName}{method.TypeParameterDecl}({method.ArgumentList});");
        }
        else
        {
            // Abstract: return default
            if (!method.IsVoid)
            {
                if (method.IsTask && !string.IsNullOrEmpty(method.TaskTypeArg))
                    w.Line($"{indent1}return global::System.Threading.Tasks.Task.FromResult<{method.TaskTypeArg}>(default!);");
                else if (method.IsTask)
                    w.Line($"{indent1}return global::System.Threading.Tasks.Task.CompletedTask;");
                else if (method.IsValueTask && !string.IsNullOrEmpty(method.TaskTypeArg))
                    w.Line($"{indent1}return new global::System.Threading.Tasks.ValueTask<{method.TaskTypeArg}>(default!);");
                else if (method.IsValueTask)
                    w.Line($"{indent1}return default;");
                else
                    w.Line($"{indent1}return default!;");
            }
        }

        w.Line($"{indent}}}");
        w.Line();
    }

    private static void RenderImplEventOverride(CodeWriter w, InlineClassImplEventModel evt, string indent, string indent1)
    {
        w.Line($"{indent}/// <inheritdoc />");
        w.Line($"{indent}{evt.AccessModifier} override event {evt.DelegateType}? {evt.EventName}");
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
            IsInitOnly: false,
            ReturnsByRef: prop.ReturnsByRef,
            ReturnsByRefReadonly: prop.ReturnsByRefReadonly);
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
            KeyExpression: indexer.KeyExpression,
            ReturnsByRef: indexer.ReturnsByRef,
            ReturnsByRefReadonly: indexer.ReturnsByRefReadonly);
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
