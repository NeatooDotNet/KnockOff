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

        // Track which interceptors use pre-compiled mode vs generated classes
        var preCompiledInterceptors = new Dictionary<string, string>(); // interceptorName -> pre-compiled type
        var preCompiledDelegateDecls = new Dictionary<string, string>(); // interceptorName -> delegate declaration
        var preCompiledSyncDelegateDecls = new Dictionary<string, string>(); // interceptorName -> sync delegate declaration
        var compositorGroups = new Dictionary<string, UnifiedMethodInterceptorModel>(); // interceptorName -> model
        var renderedInterceptorClasses = new HashSet<string>();

        // Render interceptor classes
        // For class stubs, use IncludeStrictParameter=true so the Impl class can pass _stub.Strict
        foreach (var prop in cls.Properties)
        {
            if (renderedInterceptorClasses.Add(prop.InterceptorClassName))
            {
                var unifiedModel = ToUnifiedPropertyModel(prop);
                if (PreCompiledInterceptorRenderer.CanUsePreCompiled(unifiedModel))
                {
                    preCompiledInterceptors[prop.PropertyName] = PreCompiledInterceptorRenderer.GetPropertyInterceptorType(unifiedModel);
                }
                else
                {
                    var options = new PropertyInterceptorRenderOptions(
                        BaseIndent: 2,
                        IncludeStrictParameter: true,
                        StrictAccessExpression: "strict",
                        InterceptorTypeParameters: prop.TypeParameterList,
                        InterceptorConstraints: prop.ConstraintClauses);
                    w.SetIndent(2);
                    PropertyInterceptorRenderer.RenderInterceptorClass(w, unifiedModel, options);
                }
            }
        }

        // Group indexers by interceptor class name and render as a single multi-indexer interceptor
        var indexersByClass = cls.Indexers.GroupBy(i => i.InterceptorClassName);
        foreach (var group in indexersByClass)
        {
            if (renderedInterceptorClasses.Add(group.Key))
            {
                var firstIndexer = group.First();
                var unifiedModels = group.Select(i => ToUnifiedIndexerModel(i)).ToList();
                if (PreCompiledInterceptorRenderer.CanUsePreCompiled(unifiedModels))
                {
                    preCompiledInterceptors[firstIndexer.IndexerName] = PreCompiledInterceptorRenderer.GetIndexerInterceptorType(unifiedModels[0]);
                }
                else
                {
                    var options = new IndexerInterceptorRenderOptions(
                        BaseIndent: 2,
                        IncludeStrictParameter: true,
                        StrictAccessExpression: "strict",
                        InterceptorTypeParameters: firstIndexer.TypeParameterList,
                        InterceptorConstraints: firstIndexer.ConstraintClauses);
                    w.SetIndent(2);
                    IndexerInterceptorRenderer.RenderInterceptorClass(w, unifiedModels, options);
                }
            }
        }

        // Use shared MethodInterceptorRenderer for method interceptors
        foreach (var method in cls.Methods)
        {
            if (renderedInterceptorClasses.Add(method.InterceptorClassName))
            {
                if (PreCompiledInterceptorRenderer.CanUsePreCompiled(method))
                {
                    preCompiledInterceptors[method.MethodName] = PreCompiledInterceptorRenderer.GetMethodInterceptorType(method);
                    // Track delegate declaration for TTuple types (1+ params)
                    if (method.Parameters.Count > 0)
                    {
                        preCompiledDelegateDecls[method.MethodName] = PreCompiledInterceptorRenderer.BuildDelegateDeclaration(
                            method.MethodName, method.Parameters.AsEnumerable(), method.ReturnType, method.IsVoid);
                        var syncDecl = PreCompiledInterceptorRenderer.BuildSyncDelegateDeclaration(
                            method.MethodName, method.Parameters.AsEnumerable(), method.ReturnType, method.IsVoid);
                        if (syncDecl != null)
                            preCompiledSyncDelegateDecls[method.MethodName] = syncDecl;
                    }
                }
                else if (PreCompiledInterceptorRenderer.CanOverloadGroupUsePreCompiled(method))
                {
                    var options = new InterceptorRenderOptions(
                        BaseIndent: 2,
                        IncludeStrictParameter: true,
                        StrictAccessExpression: "strict",
                        InterceptorTypeParameters: cls.TypeParameterList,
                        InterceptorConstraints: cls.ConstraintClauses);
                    w.SetIndent(2);
                    PreCompiledInterceptorRenderer.RenderOverloadCompositorClass(w, method, options);
                    compositorGroups[method.MethodName] = method;
                }
                else
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
            }
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
            // Emit delegate declaration for TTuple types (1+ params)
            if (preCompiledDelegateDecls.TryGetValue(interceptorProp.PropertyName, out var delegateDecl))
            {
                w.Line($"{indent1}{delegateDecl}");
            }
            // Emit sync delegate declaration for async TTuple types
            if (preCompiledSyncDelegateDecls.TryGetValue(interceptorProp.PropertyName, out var syncDelegateDecl))
            {
                w.Line($"{indent1}{syncDelegateDecl}");
            }
            w.Line($"{indent1}/// <summary>{interceptorProp.Description}</summary>");
            if (preCompiledInterceptors.TryGetValue(interceptorProp.PropertyName, out var preCompiledType))
            {
                w.Line($"{indent1}public {newKeyword}{preCompiledType} {interceptorProp.PropertyName} {{ get; }} = new(\"{interceptorProp.PropertyName}\");");
            }
            else if (compositorGroups.ContainsKey(interceptorProp.PropertyName))
            {
                w.Line($"{indent1}public {newKeyword}{interceptorProp.InterceptorTypeName} {interceptorProp.PropertyName} {{ get; }} = new();");
            }
            else
            {
                w.Line($"{indent1}public {newKeyword}{interceptorProp.InterceptorTypeName} {interceptorProp.PropertyName} {{ get; }} = new();");
            }
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
        RenderImplClass(w, cls, indent1, indent2, indent3, indent4, preCompiledInterceptors, compositorGroups);

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

        var ifaceTypeParamList = handler.InterfaceTypeParameterList;
        var ifaceConstraintClause = handler.InterfaceConstraintClauses;

        var isMultiArity = handler.ArityGroups.Count > 1;
        string GetDictSuffix(int typeParamCount) => isMultiArity ? $"_{typeParamCount}" : "";

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

        // Each type arity gets its own dictionary
        foreach (var arity in handler.ArityGroups)
        {
            var dictSuffix = GetDictSuffix(arity.TypeParameterCount);
            w.Line($"{indent1}private readonly global::System.Collections.Generic.Dictionary<{arity.KeyType}, object> _typedHandlers{dictSuffix} = new();");
        }
        w.Line();

        // Of<T>() method for each type arity
        foreach (var arity in handler.ArityGroups)
        {
            var dictSuffix = GetDictSuffix(arity.TypeParameterCount);
            w.Line($"{indent1}/// <summary>Gets the typed handler for the specified type argument(s).</summary>");
            w.Line($"{indent1}public {arity.TypedHandlerClassName}<{arity.TypeParameterNames}> Of<{arity.TypeParameterNames}>(){arity.ConstraintClauses}");
            w.Line($"{indent1}{{");
            w.Line($"{indent2}var key = {arity.KeyConstruction};");
            w.Line($"{indent2}if (!_typedHandlers{dictSuffix}.TryGetValue(key, out var handler))");
            w.Line($"{indent2}{{");
            w.Line($"{indent3}handler = new {arity.TypedHandlerClassName}<{arity.TypeParameterNames}>();");
            w.Line($"{indent3}_typedHandlers{dictSuffix}[key] = handler;");
            w.Line($"{indent2}}}");
            w.Line($"{indent2}return ({arity.TypedHandlerClassName}<{arity.TypeParameterNames}>)handler;");
            w.Line($"{indent1}}}");
            w.Line();
        }

        // Aggregate tracking
        if (handler.ArityGroups.Count == 1)
        {
            w.Line($"{indent1}private int TotalCallCount => _typedHandlers.Values.Cast<IGenericMethodCallTracker>().Sum(h => h.CallCount);");
        }
        else
        {
            var dictNames = string.Join(".Concat(", handler.ArityGroups.Select((a, i) =>
                $"_typedHandlers{GetDictSuffix(a.TypeParameterCount)}.Values"));
            dictNames += string.Join("", Enumerable.Range(0, handler.ArityGroups.Count - 1).Select(_ => ")"));
            w.Line($"{indent1}private int TotalCallCount => {dictNames}.Cast<IGenericMethodCallTracker>().Sum(h => h.CallCount);");
        }
        w.Line();

        // CalledTypeArguments - only for single-arity, too complex for mixed
        if (handler.ArityGroups.Count == 1)
        {
            var arity = handler.ArityGroups.First();
            w.Line($"{indent1}/// <summary>All type argument(s) that were used in calls.</summary>");
            w.Line($"{indent1}public global::System.Collections.Generic.IReadOnlyList<{arity.KeyType}> CalledTypeArguments => _typedHandlers.Where(kvp => ((IGenericMethodCallTracker)kvp.Value).CallCount > 0).Select(kvp => kvp.Key).ToList();");
            w.Line();
        }

        // Reset method
        w.Line($"{indent1}/// <summary>Resets tracking state (call counts) but preserves configuration (Return/Call callbacks).</summary>");
        w.Line($"{indent1}public void Reset()");
        w.Line($"{indent1}{{");
        foreach (var arity in handler.ArityGroups)
        {
            var dictSuffix = GetDictSuffix(arity.TypeParameterCount);
            w.Line($"{indent2}foreach (var handler in _typedHandlers{dictSuffix}.Values.Cast<IResettable>())");
            w.Line($"{indent3}handler.Reset();");
        }
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
        if (handler.ArityGroups.Count == 1)
        {
            w.Line($"{indent1}internal bool IsConfigured => _typedHandlers.Count > 0;");
        }
        else
        {
            var configChecks = string.Join(" || ", handler.ArityGroups.Select(a =>
                $"_typedHandlers{GetDictSuffix(a.TypeParameterCount)}.Count > 0"));
            w.Line($"{indent1}internal bool IsConfigured => {configChecks};");
        }
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

        // Nested Typed Handler Classes (one per type arity)
        foreach (var arity in handler.ArityGroups)
        {
            RenderClassTypedHandlerClass(w, handler.MethodName, arity, indent1);
        }

        w.Line($"{indent}}}");
        w.Line();
    }

    internal static void RenderClassTypedHandlerClass(CodeWriter w, string methodName, InlineGenericTypeArityGroup arity, string indent)
    {
        var indent1 = indent + "\t";
        var indent2 = indent + "\t\t";

        w.Line($"{indent}/// <summary>Typed handler for {methodName} with specific type arguments.</summary>");
        w.Line($"{indent}public sealed class {arity.TypedHandlerClassName}<{arity.TypeParameterNames}> : IGenericMethodCallTracker, IResettable, global::KnockOff.IMethodTracking{arity.ConstraintClauses}");
        w.Line($"{indent}{{");

        // Delegate
        w.Line($"{indent1}/// <summary>Delegate for {methodName}.</summary>");
        w.Line($"{indent1}{arity.DelegateSignature}");
        w.Line();

        // Private callback field
        w.Line($"{indent1}private {methodName}Delegate? _call;");
        w.Line();

        // CallCount
        w.Line($"{indent1}private int _callCount;");
        w.Line($"{indent1}int IGenericMethodCallTracker.CallCount => _callCount;");
        w.Line();

        // LastArg/LastArgs
        if (arity.LastCallArgType != null)
        {
            var param = arity.NonGenericParameters.GetArray()![0];
            w.Line($"{indent1}/// <summary>The '{param.Name}' argument from the most recent call.</summary>");
            w.Line($"{indent1}public {arity.LastCallArgType} LastArg {{ get; private set; }}");
            w.Line();
        }
        else if (arity.LastCallArgsType != null)
        {
            w.Line($"{indent1}/// <summary>The arguments from the most recent call.</summary>");
            w.Line($"{indent1}public {arity.LastCallArgsType} LastArgs {{ get; private set; }}");
            w.Line();
        }

        // Return/Call method
        var typedHandlerEntryPoint = arity.IsVoid ? "Call" : "Return";
        w.Line($"{indent1}/// <summary>Sets the callback invoked when this method is called. Returns this handler for tracking.</summary>");
        w.Line($"{indent1}public global::KnockOff.IMethodTracking {typedHandlerEntryPoint}({methodName}Delegate callback) {{ _call = callback; return this; }}");
        w.Line();

        // Callback property
        w.Line($"{indent1}/// <summary>Gets the configured callback (internal use).</summary>");
        w.Line($"{indent1}internal {methodName}Delegate? Callback => _call;");
        w.Line();

        // RecordCall
        w.Line($"{indent1}/// <summary>Records a method call.</summary>");
        if (arity.NonGenericParameters.Count == 0)
        {
            w.Line($"{indent1}public void RecordCall() => _callCount++;");
        }
        else if (arity.NonGenericParameters.Count == 1)
        {
            var param = arity.NonGenericParameters.GetArray()![0];
            w.Line($"{indent1}public void RecordCall({param.Type} {param.Name}) {{ _callCount++; LastArg = {param.Name}; }}");
        }
        else
        {
            var paramList = string.Join(", ", arity.NonGenericParameters.Select(p => $"{p.Type} {p.Name}"));
            var tupleConstruction = string.Join(", ", arity.NonGenericParameters.Select(p => p.Name));
            w.Line($"{indent1}public void RecordCall({paramList}) {{ _callCount++; LastArgs = ({tupleConstruction}); }}");
        }
        w.Line();

        // Reset
        w.Line($"{indent1}/// <summary>Resets tracking state but preserves configuration.</summary>");
        if (arity.NonGenericParameters.Count == 0)
        {
            w.Line($"{indent1}public void Reset() {{ _callCount = 0; }}");
        }
        else if (arity.NonGenericParameters.Count == 1)
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

    private static void RenderImplClass(
        CodeWriter w,
        InlineClassStubModel cls,
        string indent,
        string indent1,
        string indent2,
        string indent3,
        Dictionary<string, string> preCompiledInterceptors,
        Dictionary<string, UnifiedMethodInterceptorModel> compositorGroups)
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
        var implKeyword = cls.IsRecord ? "record" : "class";
        w.Line($"{indent}private sealed {implKeyword} Impl : {cls.BaseType}");
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
                w.Line($"{indent1}private {indexer.ReturnType} _defaultRefBacking_{indexer.IndexerName}{indexer.InvokeSuffix};");
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
            RenderImplMethodOverride(w, method, indent1, indent2, preCompiledInterceptors, compositorGroups);
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

        if (prop.SetterHasAllowNull)
            w.Line("#pragma warning disable CS8765 // Nullability of parameter doesn't match overridden member");
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
        if (prop.SetterHasAllowNull)
            w.Line("#pragma warning restore CS8765");
        w.Line();
    }

    private static void RenderImplIndexerOverride(CodeWriter w, InlineClassImplIndexerModel indexer, string indent, string indent1)
    {
        var indent2 = indent1 + "\t";
        var invokeSuffix = indexer.InvokeSuffix;

        if (indexer.SetterHasAllowNull)
            w.Line("#pragma warning disable CS8765 // Nullability of parameter doesn't match overridden member");
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
                w.Line($"{indent2}if (_stub == null) {{ _defaultRefBacking_{indexer.IndexerName}{invokeSuffix} = default!; return ref _defaultRefBacking_{indexer.IndexerName}{invokeSuffix}; }}");
                w.Line($"{indent2}_stub.{indexer.IndexerName}.InvokeRefGet{invokeSuffix}(_stub.Strict, {indexer.ArgumentList});");
                w.Line($"{indent2}return ref _stub.{indexer.IndexerName}._refReturnBacking{invokeSuffix};");
            }
            else
            {
                // Virtual: IsConfigured-first pattern
                w.Line($"{indent2}if (_stub == null) return ref base[{indexer.ArgumentList}];");
                w.Line($"{indent2}if (_stub.{indexer.IndexerName}.IsConfigured)");
                w.Line($"{indent2}{{");
                w.Line($"{indent2}\t_stub.{indexer.IndexerName}.InvokeRefGet{invokeSuffix}(_stub.Strict, {indexer.ArgumentList});");
                w.Line($"{indent2}\treturn ref _stub.{indexer.IndexerName}._refReturnBacking{invokeSuffix};");
                w.Line($"{indent2}}}");
                w.Line($"{indent2}_stub.{indexer.IndexerName}.InvokeRefGet{invokeSuffix}(_stub.Strict, {indexer.ArgumentList});");
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
                    w.Line($"{indent2}return _stub.{indexer.IndexerName}.InvokeGet{invokeSuffix}(_stub.Strict, {indexer.ArgumentList});");
                }
                else
                {
                    // Virtual: always track via InvokeGet, but also fall back to base if not configured
                    w.Line($"{indent2}if (_stub == null) return base[{indexer.ArgumentList}];");
                    w.Line($"{indent2}if (_stub.{indexer.IndexerName}.IsConfigured) return _stub.{indexer.IndexerName}.InvokeGet{invokeSuffix}(_stub.Strict, {indexer.ArgumentList});");
                    // Not configured: track the unconfigured call, then return base value
                    w.Line($"{indent2}_stub.{indexer.IndexerName}.InvokeGet{invokeSuffix}(_stub.Strict, {indexer.ArgumentList});");
                    w.Line($"{indent2}return base[{indexer.ArgumentList}];");
                }
                w.Line($"{indent1}}}");
            }

            if (indexer.HasSetter)
            {
                var setterKeyword = indexer.IsInitOnly ? "init" : "set";
                w.Line($"{indent1}{setterKeyword}");
                w.Line($"{indent1}{{");
                // Handle calls from base constructor when _stub is null
                if (indexer.IsAbstract)
                {
                    // Abstract: always use InvokeSet (no base to fall back to)
                    w.Line($"{indent2}if (_stub == null) return;");
                    w.Line($"{indent2}_stub.{indexer.IndexerName}.InvokeSet{invokeSuffix}(_stub.Strict, {indexer.ArgumentList}, value);");
                }
                else
                {
                    // Virtual: always track via InvokeSet, but also delegate to base if not configured
                    w.Line($"{indent2}if (_stub == null) {{ base[{indexer.ArgumentList}] = value; return; }}");
                    w.Line($"{indent2}_stub.{indexer.IndexerName}.InvokeSet{invokeSuffix}(_stub.Strict, {indexer.ArgumentList}, value);");
                    w.Line($"{indent2}if (!_stub.{indexer.IndexerName}.IsConfigured) base[{indexer.ArgumentList}] = value;");
                }
                w.Line($"{indent1}}}");
            }
        }

        w.Line($"{indent}}}");
        if (indexer.SetterHasAllowNull)
            w.Line("#pragma warning restore CS8765");
        w.Line();
    }

    private static void RenderImplMethodOverride(
        CodeWriter w,
        InlineClassImplMethodModel method,
        string indent,
        string indent1,
        Dictionary<string, string> preCompiledInterceptors,
        Dictionary<string, UnifiedMethodInterceptorModel> compositorGroups)
    {
        if (method.IsGenericMethod)
        {
            RenderImplGenericMethodOverride(w, method, indent, indent1);
            return;
        }

        var isPreCompiled = preCompiledInterceptors.ContainsKey(method.HandlerName);
        var isCompositor = compositorGroups.ContainsKey(method.HandlerName);

        if (method.DoesNotReturn)
        {
            w.Line("#pragma warning disable CS8763 // A method marked [DoesNotReturn] should not return");
            w.Line($"{indent}[global::System.Diagnostics.CodeAnalysis.DoesNotReturn]");
        }
        w.Line($"{indent}/// <inheritdoc />");
        w.Line($"{indent}{method.AccessModifier} override {method.RefReturnPrefix}{method.ReturnType} {method.MethodName}({method.ParameterDeclarations})");
        w.Line($"{indent}{{");

        if (method.IsRefReturn)
        {
            // Ref return method override - uses InvokeRef + _refReturnBacking pattern
            // Note: ref return methods always fall back to generated interceptor classes (never pre-compiled)
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

            // Determine the invoke method name
            var invokeMethodName = string.IsNullOrEmpty(method.InvokeSuffix)
                ? "Invoke"
                : $"Invoke{method.InvokeSuffix}";

            // Build invoke arguments: strict, then method parameters
            // For pre-compiled interceptors, strip ref/in prefixes and wrap for TTuple
            var rawInputArgs = method.InputArgumentList;
            var cleanInputArgs = (isPreCompiled || isCompositor) ? StripRefPrefixes(rawInputArgs) : rawInputArgs;
            string invokeArgs;
            if (isPreCompiled || isCompositor)
            {
                // TTuple types: wrap 2+ params in tuple literal for Invoke(bool strict, TArgs args)
                var wrappedArgs = PreCompiledInterceptorRenderer.WrapInvokeArgs(cleanInputArgs);
                invokeArgs = "_stub.Strict" + wrappedArgs;
            }
            else
            {
                invokeArgs = "_stub.Strict" + (string.IsNullOrEmpty(cleanInputArgs) ? "" : $", {cleanInputArgs}");
            }

            // Determine if ValueTask wrapping is needed (only for pre-compiled/compositor)
            var needsValueTaskWrap = false;
            var needsVoidValueTaskWrap = false;
            if (isPreCompiled)
            {
                var (_, _, isAsyncValueTaskT) = PreCompiledInterceptorRenderer.GetAsyncTypeInfoPublic(method.ReturnType);
                var (_, isVoidValueTask) = PreCompiledInterceptorRenderer.GetVoidAsyncInfoPublic(method.ReturnType);
                needsValueTaskWrap = isAsyncValueTaskT;
                needsVoidValueTaskWrap = isVoidValueTask;
            }
            // Compositors already handle ValueTask wrapping in their Invoke methods

            if (method.IsAbstract)
            {
                // Abstract methods: always use Invoke - it handles everything
                if (method.IsVoid)
                {
                    w.Line($"{indent1}_stub.{method.HandlerName}.{invokeMethodName}({invokeArgs});");
                }
                else if (needsVoidValueTaskWrap)
                {
                    w.Line($"{indent1}return new global::System.Threading.Tasks.ValueTask(_stub.{method.HandlerName}.{invokeMethodName}({invokeArgs}));");
                }
                else if (needsValueTaskWrap)
                {
                    w.Line($"{indent1}return new {method.ReturnType}(_stub.{method.HandlerName}.{invokeMethodName}({invokeArgs}));");
                }
                else
                {
                    w.Line($"{indent1}return _stub.{method.HandlerName}.{invokeMethodName}({invokeArgs});");
                }
            }
            else
            {
                // Virtual methods: track whether a real handler handled the call
                w.Line($"{indent1}var unconfiguredBefore = _stub.{method.HandlerName}.UnconfiguredCallCount;");
                if (method.IsVoid)
                {
                    w.Line($"{indent1}_stub.{method.HandlerName}.{invokeMethodName}({invokeArgs});");
                    w.Line($"{indent1}if (_stub.{method.HandlerName}.UnconfiguredCallCount > unconfiguredBefore)");
                    w.Line($"{indent1}{{");
                    w.Line($"{indent1}\tbase.{method.MethodName}({method.ArgumentList});");
                    w.Line($"{indent1}}}");
                }
                else if (needsVoidValueTaskWrap)
                {
                    w.Line($"{indent1}var result = new global::System.Threading.Tasks.ValueTask(_stub.{method.HandlerName}.{invokeMethodName}({invokeArgs}));");
                    w.Line($"{indent1}if (_stub.{method.HandlerName}.UnconfiguredCallCount > unconfiguredBefore)");
                    w.Line($"{indent1}{{");
                    w.Line($"{indent1}\treturn base.{method.MethodName}({method.ArgumentList});");
                    w.Line($"{indent1}}}");
                    w.Line($"{indent1}return result;");
                }
                else if (needsValueTaskWrap)
                {
                    w.Line($"{indent1}var result = new {method.ReturnType}(_stub.{method.HandlerName}.{invokeMethodName}({invokeArgs}));");
                    w.Line($"{indent1}if (_stub.{method.HandlerName}.UnconfiguredCallCount > unconfiguredBefore)");
                    w.Line($"{indent1}{{");
                    w.Line($"{indent1}\treturn base.{method.MethodName}({method.ArgumentList});");
                    w.Line($"{indent1}}}");
                    w.Line($"{indent1}return result;");
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
        if (method.DoesNotReturn)
            w.Line("#pragma warning restore CS8763");
        w.Line();
    }

    /// <summary>
    /// Strips ref/in/out prefixes from an argument list string.
    /// Pre-compiled interceptors use plain parameters, not ref/in/out.
    /// </summary>
    private static string StripRefPrefixes(string argumentList)
    {
        if (string.IsNullOrEmpty(argumentList))
            return argumentList;

        var args = argumentList.Split(',');
        var cleanArgs = new List<string>();
        foreach (var arg in args)
        {
            var trimmed = arg.Trim();
            if (trimmed.StartsWith("in "))
                trimmed = trimmed.Substring(3);
            else if (trimmed.StartsWith("ref "))
                trimmed = trimmed.Substring(4);
            else if (trimmed.StartsWith("out "))
                trimmed = trimmed.Substring(4);
            cleanArgs.Add(trimmed);
        }
        return string.Join(", ", cleanArgs);
    }

    /// <summary>
    /// Renders a generic method override in the Impl class using the Of&lt;T&gt;() handler pattern.
    /// </summary>
    internal static void RenderImplGenericMethodOverride(CodeWriter w, InlineClassImplMethodModel method, string indent, string indent1)
    {
        if (method.DoesNotReturn)
        {
            w.Line("#pragma warning disable CS8763 // A method marked [DoesNotReturn] should not return");
            w.Line($"{indent}[global::System.Diagnostics.CodeAnalysis.DoesNotReturn]");
        }
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
        if (method.DoesNotReturn)
            w.Line("#pragma warning restore CS8763");
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
        var paramTypes = indexer.ParameterDeclarations.Split(',').Select(p => { var parts = p.Trim().Split(' '); return parts[parts.Length - 2]; }).ToArray();
        var paramList = string.Join(", ", paramTypes);

        // For source delegation, compute flattened argument list from key expression
        var argumentList = indexer.KeyExpression.StartsWith("(") && indexer.KeyExpression.EndsWith(")")
            ? indexer.KeyExpression.Substring(1, indexer.KeyExpression.Length - 2)
            : indexer.KeyExpression;

        return new UnifiedIndexerInterceptorModel(
            InterceptorClassName: indexer.InterceptorClassName,
            IndexerName: indexer.IndexerName,
            DeclaringInterface: "", // Class stubs don't have a declaring interface for Source(T) feature
            KeyType: indexer.KeyType,
            NullableKeyType: MakeNullable(indexer.KeyType),
            KeyParamName: "key", // Extracted from parameter declarations
            KeyTypeFriendlyName: indexer.KeyTypeFriendlyName,
            ValueType: indexer.ReturnType,
            NullableValueType: MakeNullable(indexer.ReturnType),
            DefaultExpression: "default!",
            HasGetter: indexer.HasGetter,
            HasSetter: indexer.HasSetter,
            ParameterSignature: indexer.ParameterDeclarations,
            ParameterTypes: paramList,
            KeyExpression: indexer.KeyExpression,
            ArgumentList: argumentList,
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
