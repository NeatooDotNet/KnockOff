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
        var indent2 = indent + "\t\t";

        var inputParams = method.InputParameters.GetArray() ?? Array.Empty<Model.Shared.ParameterModel>();
        var hasParams = inputParams.Length > 0;
        var canHaveWhenChain = !method.IsVoid && hasParams;
        var canHaveVoidWhenChain = method.IsVoid && hasParams;
        var canHaveReturns = !method.IsVoid;

        // Build parameter-related strings
        var recordParams = string.Join(", ", inputParams.Select(p => $"{p.Type} {p.Name}"));
        var recordArgs = string.Join(", ", inputParams.Select(p => p.Name));
        var predicateType = UnifiedInterceptorBuilder.BuildWhenPredicateType(method.InputParameters);
        var paramTypeList = string.Join(", ", inputParams.Select(p => $"{p.Type} {p.Name}"));

        w.Line($"{indent}/// <summary>Interceptor for {stubClassName}.{method.MethodName}.</summary>");
        w.Line($"{indent}public sealed class {method.InterceptorClassName}{method.TypeParameterList} : global::KnockOff.IMethodTracking{method.ConstraintClauses}");
        w.Line($"{indent}{{");

        // Private callback field
        w.Line($"{indent1}private {method.DelegateType}? _onCall;");
        w.Line();

        // CallCount (private - use Verify(Times) for public API)
        w.Line($"{indent1}private int _callCount;");
        w.Line();

        // Returns value storage (for non-void methods)
        if (canHaveReturns)
        {
            w.Line($"{indent1}private {method.ReturnType} _returnsValue = default!;");
            w.Line($"{indent1}private bool _hasReturnsValue;");
            w.Line();
        }

        // When chain storage (for methods with parameters)
        if (canHaveWhenChain)
        {
            w.Line($"{indent1}private global::System.Collections.Generic.List<WhenMatcher>? _whenChain;");
            w.Line($"{indent1}private int _whenChainHead;");
            w.Line($"{indent1}private bool _whenVerifiable;");
            w.Line();
        }
        if (canHaveVoidWhenChain)
        {
            w.Line($"{indent1}private global::System.Collections.Generic.List<VoidWhenMatcher>? _whenChain;");
            w.Line($"{indent1}private int _whenChainHead;");
            w.Line($"{indent1}private bool _whenVerifiable;");
            w.Line();
        }

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

        // Returns method (for non-void methods)
        if (canHaveReturns)
        {
            w.Line($"{indent1}/// <summary>Configures return value that repeats indefinitely. Returns this for tracking.</summary>");
            w.Line($"{indent1}public global::KnockOff.IMethodTracking Returns({method.ReturnType} value) {{ _hasReturnsValue = true; _returnsValue = value; return this; }}");
            w.Line();
        }

        // When entry points (for methods with parameters)
        if (canHaveWhenChain)
        {
            // When(values) overload
            w.Line($"{indent1}/// <summary>Configures parameter-specific matching with exact values. Returns builder for Returns().</summary>");
            w.Line($"{indent1}public WhenBuilder When({paramTypeList})");
            w.Line($"{indent1}{{");
            w.Line($"{indent2}_whenChain ??= new global::System.Collections.Generic.List<WhenMatcher>();");
            var lambdaParams = string.Join(", ", Enumerable.Range(0, inputParams.Length).Select(i => $"_arg{i}"));
            var predicateBody = string.Join(" && ", Enumerable.Range(0, inputParams.Length).Select(i => $"global::System.Object.Equals(_arg{i}, {inputParams[i].Name})"));
            w.Line($"{indent2}return new WhenBuilder(this, ({lambdaParams}) => {predicateBody});");
            w.Line($"{indent1}}}");
            w.Line();

            // When(predicate) overload
            w.Line($"{indent1}/// <summary>Configures parameter-specific matching with predicate. Returns builder for Returns().</summary>");
            w.Line($"{indent1}public WhenBuilder When({predicateType} predicate)");
            w.Line($"{indent1}{{");
            w.Line($"{indent2}_whenChain ??= new global::System.Collections.Generic.List<WhenMatcher>();");
            w.Line($"{indent2}return new WhenBuilder(this, predicate);");
            w.Line($"{indent1}}}");
            w.Line();
        }
        if (canHaveVoidWhenChain)
        {
            // When(values) overload for void methods
            w.Line($"{indent1}/// <summary>Configures parameter-specific matching with exact values for void method. Returns chain directly.</summary>");
            w.Line($"{indent1}public VoidWhenChain When({paramTypeList})");
            w.Line($"{indent1}{{");
            w.Line($"{indent2}_whenChain ??= new global::System.Collections.Generic.List<VoidWhenMatcher>();");
            var lambdaParams = string.Join(", ", Enumerable.Range(0, inputParams.Length).Select(i => $"_arg{i}"));
            var predicateBody = string.Join(" && ", Enumerable.Range(0, inputParams.Length).Select(i => $"global::System.Object.Equals(_arg{i}, {inputParams[i].Name})"));
            w.Line($"{indent2}var matcher = new VoidWhenMatcherPredicate(({lambdaParams}) => {predicateBody});");
            w.Line($"{indent2}_whenChain.Add(matcher);");
            w.Line($"{indent2}return new VoidWhenChain(this, matcher);");
            w.Line($"{indent1}}}");
            w.Line();

            // When(predicate) overload for void methods
            w.Line($"{indent1}/// <summary>Configures parameter-specific matching with predicate for void method. Returns chain directly.</summary>");
            w.Line($"{indent1}public VoidWhenChain When({predicateType} predicate)");
            w.Line($"{indent1}{{");
            w.Line($"{indent2}_whenChain ??= new global::System.Collections.Generic.List<VoidWhenMatcher>();");
            w.Line($"{indent2}var matcher = new VoidWhenMatcherPredicate(predicate);");
            w.Line($"{indent2}_whenChain.Add(matcher);");
            w.Line($"{indent2}return new VoidWhenChain(this, matcher);");
            w.Line($"{indent1}}}");
            w.Line();
        }

        // Invoke method with full priority chain
        RenderMethodInterceptorInvoke(w, method, inputParams, indent1, indent2);

        // RecordCall method
        w.Append($"{indent1}internal void RecordCall({recordParams}) {{ _callCount++; ");
        if (method.LastCallArgType != null && inputParams.Length > 0)
        {
            w.Append($"LastCallArg = {inputParams[0].Name}; ");
        }
        else if (method.LastCallArgsType != null && inputParams.Length > 1)
        {
            w.Append($"LastCallArgs = ({recordArgs}); ");
        }
        w.Line("}");
        w.Line();

        // Reset method - clears tracking state but preserves configuration
        w.Line($"{indent1}/// <summary>Resets tracking state but preserves configuration.</summary>");
        w.Append($"{indent1}public void Reset() {{ _callCount = 0; ");
        if (method.LastCallArgType != null)
        {
            w.Append("LastCallArg = default; ");
        }
        else if (method.LastCallArgsType != null)
        {
            w.Append("LastCallArgs = default; ");
        }
        if (canHaveWhenChain || canHaveVoidWhenChain)
        {
            w.Append("_whenChainHead = 0; if (_whenChain != null) foreach (var m in _whenChain) m.CallCount = 0; ");
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
        var isConfiguredExpr = "_onCall != null";
        if (canHaveReturns)
            isConfiguredExpr += " || _hasReturnsValue";
        if (canHaveWhenChain || canHaveVoidWhenChain)
            isConfiguredExpr += " || (_whenChain?.Count ?? 0) > 0";
        w.Line($"{indent1}internal bool IsConfigured => {isConfiguredExpr};");
        w.Line();

        w.Line($"{indent1}/// <summary>Checks verification for Stub.Verify() - only checks if marked verifiable.</summary>");
        w.Line($"{indent1}internal global::KnockOff.VerificationFailure? CheckVerification()");
        w.Line($"{indent1}{{");
        if (canHaveWhenChain || canHaveVoidWhenChain)
        {
            w.Line($"{indent2}if (!_isVerifiable && !_whenVerifiable) return null;");
            w.Line($"{indent2}if (_isVerifiable)");
            w.Line($"{indent2}{{");
            w.Line($"{indent2}\tvar times = _verifiableTimes ?? global::KnockOff.Times.AtLeastOnce;");
            w.Line($"{indent2}\tif (!times.Validate(_callCount)) return new global::KnockOff.VerificationFailure(\"{method.MethodName}\", times, _callCount);");
            w.Line($"{indent2}}}");
            w.Line($"{indent2}if (_whenVerifiable && _whenChain != null && _whenChain.Count > 0)");
            w.Line($"{indent2}{{");
            w.Line($"{indent2}\tvar head = _whenChainHead;");
            w.Line($"{indent2}\tvar count = _whenChain.Count;");
            w.Line($"{indent2}\tif (head < count && !_whenChain[head].IsTerminal)");
            w.Line($"{indent2}\t\treturn global::KnockOff.VerificationFailure.SequenceIncomplete(\"{method.MethodName} When chain\", count, head);");
            w.Line($"{indent2}}}");
            w.Line($"{indent2}return null;");
        }
        else
        {
            w.Line($"{indent2}if (!_isVerifiable) return null;");
            w.Line($"{indent2}var times = _verifiableTimes ?? global::KnockOff.Times.AtLeastOnce;");
            w.Line($"{indent2}return times.Validate(_callCount) ? null : new global::KnockOff.VerificationFailure(\"{method.MethodName}\", times, _callCount);");
        }
        w.Line($"{indent1}}}");
        w.Line();

        w.Line($"{indent1}/// <summary>Checks verification for Stub.VerifyAll() - checks if configured.</summary>");
        w.Line($"{indent1}internal global::KnockOff.VerificationFailure? CheckVerificationAll()");
        w.Line($"{indent1}{{");
        w.Line($"{indent2}if (!IsConfigured) return null;");
        if (canHaveWhenChain || canHaveVoidWhenChain)
        {
            w.Line($"{indent2}if (!global::KnockOff.Times.AtLeastOnce.Validate(_callCount))");
            w.Line($"{indent2}\treturn new global::KnockOff.VerificationFailure(\"{method.MethodName}\", global::KnockOff.Times.AtLeastOnce, _callCount);");
            w.Line($"{indent2}if (_whenChain != null && _whenChain.Count > 0)");
            w.Line($"{indent2}{{");
            w.Line($"{indent2}\tvar head = _whenChainHead;");
            w.Line($"{indent2}\tvar count = _whenChain.Count;");
            w.Line($"{indent2}\tif (head < count && !_whenChain[head].IsTerminal)");
            w.Line($"{indent2}\t\treturn global::KnockOff.VerificationFailure.SequenceIncomplete(\"{method.MethodName} When chain\", count, head);");
            w.Line($"{indent2}}}");
            w.Line($"{indent2}return null;");
        }
        else
        {
            w.Line($"{indent2}return _callCount >= 1 ? null : new global::KnockOff.VerificationFailure(\"{method.MethodName}\", global::KnockOff.Times.AtLeastOnce, _callCount);");
        }
        w.Line($"{indent1}}}");

        // Render When chain nested classes (for methods with parameters)
        if (canHaveWhenChain)
        {
            RenderWhenMatcherClasses(w, method, inputParams, indent1, indent2);
            RenderWhenBuilderClass(w, method, inputParams, indent1, indent2);
            RenderWhenChainClass(w, method, inputParams, indent1, indent2);
        }
        if (canHaveVoidWhenChain)
        {
            RenderVoidWhenMatcherClasses(w, method, inputParams, indent1, indent2);
            RenderVoidWhenChainClass(w, method, inputParams, indent1, indent2);
        }

        w.Line($"{indent}}}");
        w.Line();
    }

    private static void RenderMethodInterceptorInvoke(CodeWriter w, InlineClassMethodModel method, ParameterModel[] inputParams, string indent, string indent1)
    {
        var hasParams = inputParams.Length > 0;
        var canHaveWhenChain = !method.IsVoid && hasParams;
        var canHaveVoidWhenChain = method.IsVoid && hasParams;
        var canHaveReturns = !method.IsVoid;

        var invokeParams = "bool strict, out bool handled" + (hasParams ? ", " + string.Join(", ", inputParams.Select(p => $"{p.Type} {p.Name}")) : "");
        var callbackArgs = string.Join(", ", inputParams.Select(p => p.Name));
        var recordArgs = callbackArgs;
        var returnType = method.IsVoid ? "void" : method.ReturnType;

        w.Line($"{indent}/// <summary>Invokes the configured behavior. Called by Impl class. Sets handled=true if invocation was handled.</summary>");
        w.Line($"{indent}internal {returnType} Invoke({invokeParams})");
        w.Line($"{indent}{{");

        // When chain - highest priority (for methods with parameters)
        if (canHaveWhenChain)
        {
            w.Line($"{indent1}// When chain - check HEAD matcher first (highest priority)");
            w.Line($"{indent1}if (_whenChain != null && _whenChainHead < _whenChain.Count)");
            w.Line($"{indent1}{{");
            w.Line($"{indent1}\tvar matcher = _whenChain[_whenChainHead];");
            w.Line($"{indent1}\tif (matcher.Matches({callbackArgs}))");
            w.Line($"{indent1}\t{{");
            w.Line($"{indent1}\t\tmatcher.CallCount++;");
            w.Line($"{indent1}\t\tRecordCall({recordArgs});");
            w.Line($"{indent1}\t\tif (_whenChainHead < _whenChain.Count - 1) _whenChainHead++;");
            w.Line($"{indent1}\t\thandled = true;");
            w.Line($"{indent1}\t\treturn matcher.Execute({callbackArgs});");
            w.Line($"{indent1}\t}}");
            w.Line($"{indent1}\telse if (matcher.IsTerminal)");
            w.Line($"{indent1}\t{{");
            w.Line($"{indent1}\t\t_whenChainHead++;");
            w.Line($"{indent1}\t}}");
            w.Line($"{indent1}}}");
            w.Line();
        }
        if (canHaveVoidWhenChain)
        {
            w.Line($"{indent1}// When chain - check HEAD matcher first (highest priority)");
            w.Line($"{indent1}if (_whenChain != null && _whenChainHead < _whenChain.Count)");
            w.Line($"{indent1}{{");
            w.Line($"{indent1}\tvar matcher = _whenChain[_whenChainHead];");
            w.Line($"{indent1}\tif (matcher.Matches({callbackArgs}))");
            w.Line($"{indent1}\t{{");
            w.Line($"{indent1}\t\tmatcher.CallCount++;");
            w.Line($"{indent1}\t\tRecordCall({recordArgs});");
            w.Line($"{indent1}\t\tif (_whenChainHead < _whenChain.Count - 1) _whenChainHead++;");
            w.Line($"{indent1}\t\tmatcher.Execute({callbackArgs});");
            w.Line($"{indent1}\t\thandled = true;");
            w.Line($"{indent1}\t\treturn;");
            w.Line($"{indent1}\t}}");
            w.Line($"{indent1}\telse if (matcher.IsTerminal)");
            w.Line($"{indent1}\t{{");
            w.Line($"{indent1}\t\t_whenChainHead++;");
            w.Line($"{indent1}\t}}");
            w.Line($"{indent1}}}");
            w.Line();
        }

        // Returns value - next priority (for non-void methods)
        if (canHaveReturns)
        {
            w.Line($"{indent1}if (_hasReturnsValue)");
            w.Line($"{indent1}{{");
            w.Line($"{indent1}\tRecordCall({recordArgs});");
            w.Line($"{indent1}\thandled = true;");
            w.Line($"{indent1}\treturn _returnsValue;");
            w.Line($"{indent1}}}");
            w.Line();
        }

        // OnCall callback - next priority
        w.Line($"{indent1}if (_onCall != null)");
        w.Line($"{indent1}{{");
        w.Line($"{indent1}\tRecordCall({recordArgs});");
        w.Line($"{indent1}\thandled = true;");
        if (method.IsVoid)
        {
            w.Line($"{indent1}\t_onCall({callbackArgs});");
            w.Line($"{indent1}\treturn;");
        }
        else
        {
            w.Line($"{indent1}\treturn _onCall({callbackArgs});");
        }
        w.Line($"{indent1}}}");
        w.Line();

        // Not configured/not matched - signal to caller that nothing was handled
        w.Line($"{indent1}// Not configured or When chain didn't match - signal to caller");
        w.Line($"{indent1}handled = false;");
        if (!method.IsVoid)
        {
            w.Line($"{indent1}return default!;");
        }
        w.Line($"{indent}}}");
        w.Line();
    }

    private static void RenderWhenMatcherClasses(CodeWriter w, InlineClassMethodModel method, ParameterModel[] inputParams, string indent, string indent1)
    {
        var matchParams = string.Join(", ", inputParams.Select(p => $"{p.Type} {p.Name}"));
        var callbackArgs = string.Join(", ", inputParams.Select(p => p.Name));
        var predicateType = UnifiedInterceptorBuilder.BuildWhenPredicateType(method.InputParameters);

        w.Line();
        w.Line($"{indent}/// <summary>Abstract base for When chain matchers.</summary>");
        w.Line($"{indent}private abstract class WhenMatcher");
        w.Line($"{indent}{{");
        w.Line($"{indent1}public abstract bool Matches({matchParams});");
        w.Line($"{indent1}public abstract {method.ReturnType} Execute({matchParams});");
        w.Line($"{indent1}public abstract bool IsTerminal {{ get; }}");
        w.Line($"{indent1}public int CallCount {{ get; set; }}");
        w.Line($"{indent}}}");
        w.Line();

        w.Line($"{indent}/// <summary>Matcher that uses a predicate and returns a stored value.</summary>");
        w.Line($"{indent}private sealed class WhenMatcherValue : WhenMatcher");
        w.Line($"{indent}{{");
        w.Line($"{indent1}private readonly {predicateType} _predicate;");
        w.Line($"{indent1}private readonly {method.ReturnType} _value;");
        w.Line();
        w.Line($"{indent1}public WhenMatcherValue({predicateType} predicate, {method.ReturnType} value)");
        w.Line($"{indent1}{{");
        w.Line($"{indent1}\t_predicate = predicate;");
        w.Line($"{indent1}\t_value = value;");
        w.Line($"{indent1}}}");
        w.Line();
        w.Line($"{indent1}public override bool Matches({matchParams}) => _predicate({callbackArgs});");
        w.Line($"{indent1}public override {method.ReturnType} Execute({matchParams}) => _value;");
        w.Line($"{indent1}public override bool IsTerminal => false;");
        w.Line($"{indent}}}");
        w.Line();

        w.Line($"{indent}/// <summary>Matcher that always matches and invokes a callback. Terminal.</summary>");
        w.Line($"{indent}private sealed class WhenMatcherCall : WhenMatcher");
        w.Line($"{indent}{{");
        w.Line($"{indent1}private readonly {method.DelegateType} _callback;");
        w.Line();
        w.Line($"{indent1}public WhenMatcherCall({method.DelegateType} callback) => _callback = callback;");
        w.Line();
        w.Line($"{indent1}public override bool Matches({matchParams}) => true;");
        w.Line($"{indent1}public override {method.ReturnType} Execute({matchParams}) => _callback({callbackArgs});");
        w.Line($"{indent1}public override bool IsTerminal => true;");
        w.Line($"{indent}}}");
        w.Line();

        w.Line($"{indent}/// <summary>Matcher that never matches. Used to close chain without fallback. Terminal.</summary>");
        w.Line($"{indent}private sealed class WhenMatcherNone : WhenMatcher");
        w.Line($"{indent}{{");
        w.Line($"{indent1}public override bool Matches({matchParams}) => false;");
        w.Line($"{indent1}public override {method.ReturnType} Execute({matchParams}) => default!;");
        w.Line($"{indent1}public override bool IsTerminal => true;");
        w.Line($"{indent}}}");
    }

    private static void RenderWhenBuilderClass(CodeWriter w, InlineClassMethodModel method, ParameterModel[] inputParams, string indent, string indent1)
    {
        var predicateType = UnifiedInterceptorBuilder.BuildWhenPredicateType(method.InputParameters);

        // Check if this is an async method (Task<T> or ValueTask<T>)
        var (innerType, isTaskT, isValueTaskT) = GetAsyncTypeInfoForMethod(method.ReturnType);
        var isAsync = isTaskT || isValueTaskT;

        w.Line();
        w.Line($"{indent}/// <summary>Builder for When matchers. Captures predicate, awaits Returns(value).</summary>");
        w.Line($"{indent}public sealed class WhenBuilder : global::KnockOff.IWhenBuilder<{method.DelegateType}, {method.ReturnType}>");
        w.Line($"{indent}{{");
        w.Line($"{indent1}private readonly {method.InterceptorClassName}{method.TypeParameterList} _interceptor;");
        w.Line($"{indent1}private readonly {predicateType} _predicate;");
        w.Line();
        w.Line($"{indent1}public WhenBuilder({method.InterceptorClassName}{method.TypeParameterList} interceptor, {predicateType} predicate)");
        w.Line($"{indent1}{{");
        w.Line($"{indent1}\t_interceptor = interceptor;");
        w.Line($"{indent1}\t_predicate = predicate;");
        w.Line($"{indent1}}}");
        w.Line();

        // For async methods (Task<T>/ValueTask<T>), generate Returns(TInner) that auto-wraps
        if (isAsync)
        {
            w.Line($"{indent1}/// <summary>Configures the return value. Auto-wrapped in {(isTaskT ? "Task.FromResult" : "new ValueTask")}.</summary>");
            w.Line($"{indent1}public WhenChain Returns({innerType} value)");
            w.Line($"{indent1}{{");
            w.Line($"{indent1}\t_interceptor._whenChain ??= new global::System.Collections.Generic.List<WhenMatcher>();");
            if (isTaskT)
                w.Line($"{indent1}\t_interceptor._whenChain.Add(new WhenMatcherValue(_predicate, global::System.Threading.Tasks.Task.FromResult(value)));");
            else
                w.Line($"{indent1}\t_interceptor._whenChain.Add(new WhenMatcherValue(_predicate, new global::System.Threading.Tasks.ValueTask<{innerType}>(value)));");
            w.Line($"{indent1}\treturn new WhenChain(_interceptor);");
            w.Line($"{indent1}}}");
            w.Line();
            // Explicit interface implementation wraps too
            if (isTaskT)
                w.Line($"{indent1}global::KnockOff.IWhenChain<{method.DelegateType}, {method.ReturnType}> global::KnockOff.IWhenBuilder<{method.DelegateType}, {method.ReturnType}>.Returns({method.ReturnType} value) => Returns(value.Result);");
            else
                w.Line($"{indent1}global::KnockOff.IWhenChain<{method.DelegateType}, {method.ReturnType}> global::KnockOff.IWhenBuilder<{method.DelegateType}, {method.ReturnType}>.Returns({method.ReturnType} value) => Returns(value.Result);");
        }
        else
        {
            w.Line($"{indent1}public WhenChain Returns({method.ReturnType} value)");
            w.Line($"{indent1}{{");
            w.Line($"{indent1}\t_interceptor._whenChain ??= new global::System.Collections.Generic.List<WhenMatcher>();");
            w.Line($"{indent1}\t_interceptor._whenChain.Add(new WhenMatcherValue(_predicate, value));");
            w.Line($"{indent1}\treturn new WhenChain(_interceptor);");
            w.Line($"{indent1}}}");
            w.Line();
            w.Line($"{indent1}global::KnockOff.IWhenChain<{method.DelegateType}, {method.ReturnType}> global::KnockOff.IWhenBuilder<{method.DelegateType}, {method.ReturnType}>.Returns({method.ReturnType} value) => Returns(value);");
        }
        w.Line($"{indent}}}");
    }

    private static void RenderWhenChainClass(CodeWriter w, InlineClassMethodModel method, ParameterModel[] inputParams, string indent, string indent1)
    {
        var predicateType = UnifiedInterceptorBuilder.BuildWhenPredicateType(method.InputParameters);
        var paramTypeList = string.Join(", ", inputParams.Select(p => $"{p.Type} {p.Name}"));

        w.Line();
        w.Line($"{indent}/// <summary>When chain implementation with ThenWhen, ThenCall, ThenNone, verification support.</summary>");
        w.Line($"{indent}public sealed class WhenChain : global::KnockOff.IWhenChain<{method.DelegateType}, {method.ReturnType}>");
        w.Line($"{indent}{{");
        w.Line($"{indent1}private readonly {method.InterceptorClassName}{method.TypeParameterList} _interceptor;");
        w.Line();
        w.Line($"{indent1}public WhenChain({method.InterceptorClassName}{method.TypeParameterList} interceptor) => _interceptor = interceptor;");
        w.Line();

        // ThenWhen with values
        var lambdaParams = string.Join(", ", Enumerable.Range(0, inputParams.Length).Select(i => $"_arg{i}"));
        var predicateBody = string.Join(" && ", Enumerable.Range(0, inputParams.Length).Select(i => $"global::System.Object.Equals(_arg{i}, {inputParams[i].Name})"));
        w.Line($"{indent1}/// <summary>Adds another matcher with exact value matching.</summary>");
        w.Line($"{indent1}public WhenBuilder ThenWhen({paramTypeList})");
        w.Line($"{indent1}{{");
        w.Line($"{indent1}\treturn new WhenBuilder(_interceptor, ({lambdaParams}) => {predicateBody});");
        w.Line($"{indent1}}}");
        w.Line();

        // ThenWhen with predicate
        w.Line($"{indent1}/// <summary>Adds another matcher with predicate matching.</summary>");
        w.Line($"{indent1}public WhenBuilder ThenWhen({predicateType} predicate)");
        w.Line($"{indent1}{{");
        w.Line($"{indent1}\treturn new WhenBuilder(_interceptor, predicate);");
        w.Line($"{indent1}}}");
        w.Line();

        // ThenCall
        w.Line($"{indent1}/// <summary>Adds an unconditional callback as terminal matcher.</summary>");
        w.Line($"{indent1}public global::KnockOff.IWhenTracking ThenCall({method.DelegateType} callback)");
        w.Line($"{indent1}{{");
        w.Line($"{indent1}\t_interceptor._whenChain ??= new global::System.Collections.Generic.List<WhenMatcher>();");
        w.Line($"{indent1}\t_interceptor._whenChain.Add(new WhenMatcherCall(callback));");
        w.Line($"{indent1}\treturn this;");
        w.Line($"{indent1}}}");
        w.Line();

        // ThenNone
        w.Line($"{indent1}/// <summary>Closes chain with no matcher. Falls through when exhausted.</summary>");
        w.Line($"{indent1}public global::KnockOff.IWhenTracking ThenNone()");
        w.Line($"{indent1}{{");
        w.Line($"{indent1}\t_interceptor._whenChain ??= new global::System.Collections.Generic.List<WhenMatcher>();");
        w.Line($"{indent1}\t_interceptor._whenChain.Add(new WhenMatcherNone());");
        w.Line($"{indent1}\treturn this;");
        w.Line($"{indent1}}}");
        w.Line();

        // Verify
        w.Line($"{indent1}/// <summary>Verifies the When chain was fully consumed (reached terminal state).</summary>");
        w.Line($"{indent1}public void Verify()");
        w.Line($"{indent1}{{");
        w.Line($"{indent1}\tif (_interceptor._whenChain == null || _interceptor._whenChain.Count == 0) return;");
        w.Line($"{indent1}\tvar head = _interceptor._whenChainHead;");
        w.Line($"{indent1}\tvar count = _interceptor._whenChain.Count;");
        w.Line($"{indent1}\tif (head < count && !_interceptor._whenChain[head].IsTerminal)");
        w.Line($"{indent1}\t\tthrow new global::KnockOff.VerificationException(global::KnockOff.VerificationFailure.SequenceIncomplete(\"When chain\", count, head));");
        w.Line($"{indent1}}}");
        w.Line();

        // Reset
        w.Line($"{indent1}/// <summary>Resets When chain HEAD and all matcher call counts.</summary>");
        w.Line($"{indent1}public void Reset()");
        w.Line($"{indent1}{{");
        w.Line($"{indent1}\t_interceptor._whenChainHead = 0;");
        w.Line($"{indent1}\tif (_interceptor._whenChain != null)");
        w.Line($"{indent1}\t\tforeach (var matcher in _interceptor._whenChain)");
        w.Line($"{indent1}\t\t\tmatcher.CallCount = 0;");
        w.Line($"{indent1}}}");
        w.Line();

        // Verifiable
        w.Line($"{indent1}/// <summary>Marks this When chain for verification by Stub.Verify().</summary>");
        w.Line($"{indent1}public WhenChain Verifiable()");
        w.Line($"{indent1}{{");
        w.Line($"{indent1}\t_interceptor._whenVerifiable = true;");
        w.Line($"{indent1}\treturn this;");
        w.Line($"{indent1}}}");
        w.Line();

        // Explicit interface implementations
        w.Line($"{indent1}global::KnockOff.IWhenChain<{method.DelegateType}, {method.ReturnType}> global::KnockOff.IWhenChain<{method.DelegateType}, {method.ReturnType}>.Verifiable() => Verifiable();");
        w.Line($"{indent1}global::KnockOff.IWhenTracking global::KnockOff.IWhenTracking.Verifiable() => Verifiable();");
        w.Line($"{indent}}}");
    }

    private static void RenderVoidWhenMatcherClasses(CodeWriter w, InlineClassMethodModel method, ParameterModel[] inputParams, string indent, string indent1)
    {
        var matchParams = string.Join(", ", inputParams.Select(p => $"{p.Type} {p.Name}"));
        var callbackArgs = string.Join(", ", inputParams.Select(p => p.Name));
        var predicateType = UnifiedInterceptorBuilder.BuildWhenPredicateType(method.InputParameters);

        w.Line();
        w.Line($"{indent}/// <summary>Abstract base for void When chain matchers.</summary>");
        w.Line($"{indent}internal abstract class VoidWhenMatcher");
        w.Line($"{indent}{{");
        w.Line($"{indent1}public abstract bool Matches({matchParams});");
        w.Line($"{indent1}public abstract void Execute({matchParams});");
        w.Line($"{indent1}public abstract bool IsTerminal {{ get; }}");
        w.Line($"{indent1}public int CallCount {{ get; set; }}");
        w.Line($"{indent1}public {method.DelegateType}? Callback {{ get; set; }}");
        w.Line($"{indent}}}");
        w.Line();

        w.Line($"{indent}/// <summary>Matcher that uses a predicate and optionally invokes a callback.</summary>");
        w.Line($"{indent}private sealed class VoidWhenMatcherPredicate : VoidWhenMatcher");
        w.Line($"{indent}{{");
        w.Line($"{indent1}private readonly {predicateType} _predicate;");
        w.Line();
        w.Line($"{indent1}public VoidWhenMatcherPredicate({predicateType} predicate) => _predicate = predicate;");
        w.Line();
        w.Line($"{indent1}public override bool Matches({matchParams}) => _predicate({callbackArgs});");
        w.Line($"{indent1}public override void Execute({matchParams}) {{ Callback?.Invoke({callbackArgs}); }}");
        w.Line($"{indent1}public override bool IsTerminal => false;");
        w.Line($"{indent}}}");
        w.Line();

        w.Line($"{indent}/// <summary>Matcher that always matches and invokes a callback. Terminal.</summary>");
        w.Line($"{indent}private sealed class VoidWhenMatcherCall : VoidWhenMatcher");
        w.Line($"{indent}{{");
        w.Line($"{indent1}private readonly {method.DelegateType} _callback;");
        w.Line();
        w.Line($"{indent1}public VoidWhenMatcherCall({method.DelegateType} callback) => _callback = callback;");
        w.Line();
        w.Line($"{indent1}public override bool Matches({matchParams}) => true;");
        w.Line($"{indent1}public override void Execute({matchParams}) => _callback({callbackArgs});");
        w.Line($"{indent1}public override bool IsTerminal => true;");
        w.Line($"{indent}}}");
        w.Line();

        w.Line($"{indent}/// <summary>Matcher that never matches. Used to close chain without fallback. Terminal.</summary>");
        w.Line($"{indent}private sealed class VoidWhenMatcherNone : VoidWhenMatcher");
        w.Line($"{indent}{{");
        w.Line($"{indent1}public override bool Matches({matchParams}) => false;");
        w.Line($"{indent1}public override void Execute({matchParams}) {{ }}");
        w.Line($"{indent1}public override bool IsTerminal => true;");
        w.Line($"{indent}}}");
    }

    private static void RenderVoidWhenChainClass(CodeWriter w, InlineClassMethodModel method, ParameterModel[] inputParams, string indent, string indent1)
    {
        var predicateType = UnifiedInterceptorBuilder.BuildWhenPredicateType(method.InputParameters);
        var paramTypeList = string.Join(", ", inputParams.Select(p => $"{p.Type} {p.Name}"));

        w.Line();
        w.Line($"{indent}/// <summary>Void When chain implementation with Call, ThenWhen, ThenCall, ThenNone, verification support.</summary>");
        w.Line($"{indent}public sealed class VoidWhenChain : global::KnockOff.IVoidWhenChain<{method.DelegateType}>");
        w.Line($"{indent}{{");
        w.Line($"{indent1}private readonly {method.InterceptorClassName}{method.TypeParameterList} _interceptor;");
        w.Line($"{indent1}private readonly VoidWhenMatcher _currentMatcher;");
        w.Line();
        w.Line($"{indent1}internal VoidWhenChain({method.InterceptorClassName}{method.TypeParameterList} interceptor, VoidWhenMatcher currentMatcher)");
        w.Line($"{indent1}{{");
        w.Line($"{indent1}\t_interceptor = interceptor;");
        w.Line($"{indent1}\t_currentMatcher = currentMatcher;");
        w.Line($"{indent1}}}");
        w.Line();

        // Call
        w.Line($"{indent1}/// <summary>Sets an optional callback to invoke when this matcher matches.</summary>");
        w.Line($"{indent1}public VoidWhenChain Call({method.DelegateType} callback)");
        w.Line($"{indent1}{{");
        w.Line($"{indent1}\t_currentMatcher.Callback = callback;");
        w.Line($"{indent1}\treturn this;");
        w.Line($"{indent1}}}");
        w.Line();
        w.Line($"{indent1}global::KnockOff.IVoidWhenChain<{method.DelegateType}> global::KnockOff.IVoidWhenChain<{method.DelegateType}>.Call({method.DelegateType} callback) => Call(callback);");
        w.Line();

        // ThenWhen with values
        var lambdaParams = string.Join(", ", Enumerable.Range(0, inputParams.Length).Select(i => $"_arg{i}"));
        var predicateBody = string.Join(" && ", Enumerable.Range(0, inputParams.Length).Select(i => $"global::System.Object.Equals(_arg{i}, {inputParams[i].Name})"));
        w.Line($"{indent1}/// <summary>Adds another matcher with exact value matching.</summary>");
        w.Line($"{indent1}public VoidWhenChain ThenWhen({paramTypeList})");
        w.Line($"{indent1}{{");
        w.Line($"{indent1}\t_interceptor._whenChain ??= new global::System.Collections.Generic.List<VoidWhenMatcher>();");
        w.Line($"{indent1}\tvar matcher = new VoidWhenMatcherPredicate(({lambdaParams}) => {predicateBody});");
        w.Line($"{indent1}\t_interceptor._whenChain.Add(matcher);");
        w.Line($"{indent1}\treturn new VoidWhenChain(_interceptor, matcher);");
        w.Line($"{indent1}}}");
        w.Line();

        // ThenWhen with predicate
        w.Line($"{indent1}/// <summary>Adds another matcher with predicate matching.</summary>");
        w.Line($"{indent1}public VoidWhenChain ThenWhen({predicateType} predicate)");
        w.Line($"{indent1}{{");
        w.Line($"{indent1}\t_interceptor._whenChain ??= new global::System.Collections.Generic.List<VoidWhenMatcher>();");
        w.Line($"{indent1}\tvar matcher = new VoidWhenMatcherPredicate(predicate);");
        w.Line($"{indent1}\t_interceptor._whenChain.Add(matcher);");
        w.Line($"{indent1}\treturn new VoidWhenChain(_interceptor, matcher);");
        w.Line($"{indent1}}}");
        w.Line();

        // ThenCall
        w.Line($"{indent1}/// <summary>Adds an unconditional callback as terminal matcher.</summary>");
        w.Line($"{indent1}public global::KnockOff.IWhenTracking ThenCall({method.DelegateType} callback)");
        w.Line($"{indent1}{{");
        w.Line($"{indent1}\t_interceptor._whenChain ??= new global::System.Collections.Generic.List<VoidWhenMatcher>();");
        w.Line($"{indent1}\t_interceptor._whenChain.Add(new VoidWhenMatcherCall(callback));");
        w.Line($"{indent1}\treturn this;");
        w.Line($"{indent1}}}");
        w.Line();

        // ThenNone
        w.Line($"{indent1}/// <summary>Closes chain with no matcher. Falls through when exhausted.</summary>");
        w.Line($"{indent1}public global::KnockOff.IWhenTracking ThenNone()");
        w.Line($"{indent1}{{");
        w.Line($"{indent1}\t_interceptor._whenChain ??= new global::System.Collections.Generic.List<VoidWhenMatcher>();");
        w.Line($"{indent1}\t_interceptor._whenChain.Add(new VoidWhenMatcherNone());");
        w.Line($"{indent1}\treturn this;");
        w.Line($"{indent1}}}");
        w.Line();

        // Verify
        w.Line($"{indent1}/// <summary>Verifies the When chain was fully consumed (reached terminal state).</summary>");
        w.Line($"{indent1}public void Verify()");
        w.Line($"{indent1}{{");
        w.Line($"{indent1}\tif (_interceptor._whenChain == null || _interceptor._whenChain.Count == 0) return;");
        w.Line($"{indent1}\tvar head = _interceptor._whenChainHead;");
        w.Line($"{indent1}\tvar count = _interceptor._whenChain.Count;");
        w.Line($"{indent1}\tif (head < count && !_interceptor._whenChain[head].IsTerminal)");
        w.Line($"{indent1}\t\tthrow new global::KnockOff.VerificationException(global::KnockOff.VerificationFailure.SequenceIncomplete(\"When chain\", count, head));");
        w.Line($"{indent1}}}");
        w.Line();

        // Verify(Times)
        w.Line($"{indent1}/// <summary>Verifies this specific matcher was called the expected number of times.</summary>");
        w.Line($"{indent1}public void Verify(global::KnockOff.Times times)");
        w.Line($"{indent1}{{");
        w.Line($"{indent1}\tif (!times.Validate(_currentMatcher.CallCount))");
        w.Line($"{indent1}\t\tthrow new global::KnockOff.VerificationException(new global::KnockOff.VerificationFailure(\"When matcher\", times, _currentMatcher.CallCount));");
        w.Line($"{indent1}}}");
        w.Line();

        // Reset
        w.Line($"{indent1}/// <summary>Resets When chain HEAD and all matcher call counts.</summary>");
        w.Line($"{indent1}public void Reset()");
        w.Line($"{indent1}{{");
        w.Line($"{indent1}\t_interceptor._whenChainHead = 0;");
        w.Line($"{indent1}\tif (_interceptor._whenChain != null)");
        w.Line($"{indent1}\t\tforeach (var matcher in _interceptor._whenChain)");
        w.Line($"{indent1}\t\t\tmatcher.CallCount = 0;");
        w.Line($"{indent1}}}");
        w.Line();

        // Verifiable
        w.Line($"{indent1}/// <summary>Marks this When chain for verification by Stub.Verify().</summary>");
        w.Line($"{indent1}public VoidWhenChain Verifiable()");
        w.Line($"{indent1}{{");
        w.Line($"{indent1}\t_interceptor._whenVerifiable = true;");
        w.Line($"{indent1}\treturn this;");
        w.Line($"{indent1}}}");
        w.Line();

        // Explicit interface implementations
        w.Line($"{indent1}global::KnockOff.IVoidWhenChain<{method.DelegateType}> global::KnockOff.IVoidWhenChain<{method.DelegateType}>.Verifiable() => Verifiable();");
        w.Line($"{indent1}global::KnockOff.IWhenTracking global::KnockOff.IWhenTracking.Verifiable() => Verifiable();");
        w.Line($"{indent}}}");
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

        // Build the invoke arguments: strict, out handled, then all method parameters
        var invokeArgs = "_stub.Strict, out var handled" + (string.IsNullOrEmpty(method.InputArgumentList) ? "" : $", {method.InputArgumentList}");

        // Check if interceptor is configured - if so, use Invoke for full priority chain
        // Invoke returns handled=true if something matched; false means fall back to base
        w.Line($"{indent1}if (_stub.{method.HandlerName}.IsConfigured)");
        w.Line($"{indent1}{{");
        if (method.IsVoid)
        {
            w.Line($"{indent1}\t_stub.{method.HandlerName}.Invoke({invokeArgs});");
            w.Line($"{indent1}\tif (handled) return;");
        }
        else
        {
            w.Line($"{indent1}\tvar result = _stub.{method.HandlerName}.Invoke({invokeArgs});");
            w.Line($"{indent1}\tif (handled) return result;");
        }
        // Fall through to base behavior if not handled
        w.Line($"{indent1}}}");
        w.Line();

        // Not configured or not handled - record the call and delegate to base or return default
        if (string.IsNullOrEmpty(method.InputArgumentList))
        {
            w.Line($"{indent1}_stub.{method.HandlerName}.RecordCall();");
        }
        else
        {
            w.Line($"{indent1}_stub.{method.HandlerName}.RecordCall({method.InputArgumentList});");
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

    /// <summary>
    /// Analyzes a return type for async patterns and extracts the inner type.
    /// Used for When().Returns() auto-wrapping.
    /// </summary>
    private static (string InnerType, bool IsTaskT, bool IsValueTaskT) GetAsyncTypeInfoForMethod(string returnType)
    {
        const string TaskPrefix = "global::System.Threading.Tasks.Task<";
        const string ValueTaskPrefix = "global::System.Threading.Tasks.ValueTask<";

        if (returnType.StartsWith(TaskPrefix) && returnType.EndsWith(">"))
        {
            var innerType = returnType.Substring(TaskPrefix.Length, returnType.Length - TaskPrefix.Length - 1);
            return (innerType, true, false);
        }

        if (returnType.StartsWith(ValueTaskPrefix) && returnType.EndsWith(">"))
        {
            var innerType = returnType.Substring(ValueTaskPrefix.Length, returnType.Length - ValueTaskPrefix.Length - 1);
            return (innerType, false, true);
        }

        return (returnType, false, false);
    }

    #endregion
}
