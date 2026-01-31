// src/Generator/Renderer/Shared/WhenChainRenderer.cs
#nullable enable
using System.Collections.Generic;
using System.Linq;
using KnockOff.Builder;
using KnockOff.Model.Shared;

namespace KnockOff.Renderer.Shared;

/// <summary>
/// Shared renderer for When chain support.
/// Generates matcher classes, builder/chain implementations, storage fields,
/// entry points, and invoke check logic.
/// Used by both MethodInterceptorRenderer (for method interceptors) and
/// InlineRenderer (for delegate stubs).
/// </summary>
internal static class WhenChainRenderer
{
	/// <summary>
	/// Configuration for When chain rendering.
	/// </summary>
	internal readonly struct WhenChainConfig
	{
		/// <summary>The interceptor class name (for constructor references).</summary>
		public string InterceptorClassName { get; init; }

		/// <summary>Parameters for matching.</summary>
		public EquatableArray<ParameterModel> Parameters { get; init; }

		/// <summary>Return type of the method/delegate.</summary>
		public string ReturnType { get; init; }

		/// <summary>Delegate type for callbacks (OnCall type).</summary>
		public string DelegateType { get; init; }

		/// <summary>Optional signature suffix for overload groups.</summary>
		public string? SignatureSuffix { get; init; }

		/// <summary>Whether the method/delegate returns void.</summary>
		public bool IsVoid { get; init; }
	}

	/// <summary>
	/// Renders When chain storage fields.
	/// </summary>
	public static void RenderWhenStorageFields(CodeWriter w, WhenChainConfig config)
	{
		var suffix = GetSuffix(config.SignatureSuffix);
		var matcherType = $"WhenMatcher{suffix}";

		w.Line($"private global::System.Collections.Generic.List<{matcherType}>? _whenChain{suffix};");
		w.Line($"private int _whenChainHead{suffix};");
		w.Line($"private bool _whenVerifiable{suffix};");
	}

	/// <summary>
	/// Renders the When() entry point methods (value and predicate overloads).
	/// </summary>
	public static void RenderWhenEntryPoints(CodeWriter w, WhenChainConfig config)
	{
		// When() requires parameters - parameterless methods cannot use When()
		if (config.Parameters.Count == 0) return;

		var suffix = GetSuffix(config.SignatureSuffix);
		var whenChainField = $"_whenChain{suffix}";
		var predicateType = UnifiedInterceptorBuilder.BuildWhenPredicateType(config.Parameters);
		var paramTypeList = BuildParamTypeList(config.Parameters);
		var matcherType = $"WhenMatcher{suffix}";
		var builderType = $"WhenBuilder{suffix}";

		// When() value overload - exact value matching via Object.Equals
		// Returns concrete type to enable fluent ThenWhen chaining
		w.Line($"/// <summary>Configures parameter-specific matching with exact values. Returns builder for Returns().</summary>");
		w.Line($"public {builderType} When({paramTypeList})");
		using (w.Braces())
		{
			// Initialize When chain if null
			w.Line($"{whenChainField} ??= new global::System.Collections.Generic.List<{matcherType}>();");

			// Build equality predicate - use indexed lambda params to avoid keyword conflicts
			var lambdaParams = BuildLambdaParamsForEquality(config.Parameters);
			var predicateBody = BuildEqualityPredicateBody(config.Parameters);
			w.Line($"return new {builderType}(this, ({lambdaParams}) => {predicateBody});");
		}
		w.Line();

		// When() predicate overload - Func<T1, T2, bool>
		// Returns concrete type to enable fluent ThenWhen chaining
		w.Line($"/// <summary>Configures parameter-specific matching with predicate. Returns builder for Returns().</summary>");
		w.Line($"public {builderType} When({predicateType} predicate)");
		using (w.Braces())
		{
			// Initialize When chain if null
			w.Line($"{whenChainField} ??= new global::System.Collections.Generic.List<{matcherType}>();");
			w.Line($"return new {builderType}(this, predicate);");
		}
		w.Line();
	}

	/// <summary>
	/// Renders the When chain invoke check logic for insertion at the top of Invoke().
	/// </summary>
	public static void RenderWhenInvokeCheck(CodeWriter w, WhenChainConfig config)
	{
		var suffix = GetSuffix(config.SignatureSuffix);
		var whenChainField = $"_whenChain{suffix}";
		var whenChainHeadField = $"_whenChainHead{suffix}";
		var callbackArgs = BuildCallbackArgs(config.Parameters);

		w.Line($"// When chain - check HEAD matcher first (highest priority)");
		w.Line($"if ({whenChainField} != null && {whenChainHeadField} < {whenChainField}.Count)");
		using (w.Braces())
		{
			w.Line($"var matcher = {whenChainField}[{whenChainHeadField}];");
			w.Line($"if (matcher.Matches({callbackArgs}))");
			using (w.Braces())
			{
				w.Line("matcher.CallCount++;");
				w.Line();
				w.Line("// Advance HEAD unless at last matcher (which repeats)");
				w.Line($"if ({whenChainHeadField} < {whenChainField}.Count - 1)");
				using (w.Braces())
				{
					w.Line($"{whenChainHeadField}++;");
				}
				w.Line("// At last matcher: never advance (repeat behavior for both ThenWhen and ThenCall)");
				w.Line();

				// Return value directly - Execute() returns the full return type (async wrapping done at config time)
				w.Line($"return matcher.Execute({callbackArgs});");
			}
			w.Line("else if (matcher.IsTerminal)");
			using (w.Braces())
			{
				w.Line("// ThenNone: didn't match (always false), exhaust by advancing past it");
				w.Line($"{whenChainHeadField}++;");
			}
			w.Line("// Non-terminal didn't match: fall through to rest of priority chain");
		}
		w.Line();
	}

	/// <summary>
	/// Renders the WhenMatcher abstract base class and its implementations.
	/// </summary>
	public static void RenderWhenMatcherClasses(CodeWriter w, WhenChainConfig config)
	{
		var suffix = GetSuffix(config.SignatureSuffix);
		var matchParams = BuildMatchParams(config.Parameters);
		var executeParams = BuildMatchParams(config.Parameters);
		var callbackArgs = BuildCallbackArgs(config.Parameters);
		var predicateType = BuildPredicateType(config.Parameters);

		// WhenMatcher abstract base
		w.Line($"/// <summary>Abstract base for When chain matchers.</summary>");
		w.Line($"private abstract class WhenMatcher{suffix}");
		using (w.Braces())
		{
			w.Line($"public abstract bool Matches({matchParams});");
			w.Line($"public abstract {config.ReturnType} Execute({executeParams});");
			w.Line("public abstract bool IsTerminal { get; }");
			w.Line("public int CallCount { get; set; }");
		}
		w.Line();

		// WhenMatcherValue - predicate + value
		w.Line($"/// <summary>Matcher that uses a predicate and returns a stored value.</summary>");
		w.Line($"private sealed class WhenMatcherValue{suffix} : WhenMatcher{suffix}");
		using (w.Braces())
		{
			w.Line($"private readonly {predicateType} _predicate;");
			w.Line($"private readonly {config.ReturnType} _value;");
			w.Line();
			w.Line($"public WhenMatcherValue{suffix}({predicateType} predicate, {config.ReturnType} value)");
			using (w.Braces())
			{
				w.Line("_predicate = predicate;");
				w.Line("_value = value;");
			}
			w.Line();
			w.Line($"public override bool Matches({matchParams}) => _predicate({callbackArgs});");
			w.Line($"public override {config.ReturnType} Execute({executeParams}) => _value;");
			w.Line("public override bool IsTerminal => false;");
		}
		w.Line();

		// WhenMatcherCall - callback, always matches, terminal
		w.Line($"/// <summary>Matcher that always matches and invokes a callback. Terminal.</summary>");
		w.Line($"private sealed class WhenMatcherCall{suffix} : WhenMatcher{suffix}");
		using (w.Braces())
		{
			w.Line($"private readonly {config.DelegateType} _callback;");
			w.Line();
			w.Line($"public WhenMatcherCall{suffix}({config.DelegateType} callback) => _callback = callback;");
			w.Line();
			w.Line($"public override bool Matches({matchParams}) => true;");
			w.Line($"public override {config.ReturnType} Execute({executeParams}) => _callback({callbackArgs});");
			w.Line("public override bool IsTerminal => true;");
		}
		w.Line();

		// WhenMatcherNone - never matches, terminal
		w.Line($"/// <summary>Matcher that never matches. Used to close chain without fallback. Terminal.</summary>");
		w.Line($"private sealed class WhenMatcherNone{suffix} : WhenMatcher{suffix}");
		using (w.Braces())
		{
			w.Line($"public override bool Matches({matchParams}) => false;");
			w.Line($"public override {config.ReturnType} Execute({executeParams}) => default!;");
			w.Line("public override bool IsTerminal => true;");
		}
		w.Line();
	}

	/// <summary>
	/// Renders the WhenBuilderImpl nested class.
	/// For async methods (Task&lt;T&gt;/ValueTask&lt;T&gt;), Returns accepts the unwrapped type and auto-wraps.
	/// </summary>
	public static void RenderWhenBuilderImpl(CodeWriter w, WhenChainConfig config)
	{
		var suffix = GetSuffix(config.SignatureSuffix);
		var predicateType = BuildPredicateType(config.Parameters);
		var whenChainField = $"_whenChain{suffix}";
		var matcherType = $"WhenMatcher{suffix}";
		var builderType = $"WhenBuilder{suffix}";
		var chainType = $"WhenChain{suffix}";

		// Check if this is an async method (Task<T> or ValueTask<T>)
		var (innerType, isTaskT, isValueTaskT) = GetAsyncTypeInfo(config.ReturnType);
		var isAsync = isTaskT || isValueTaskT;

		w.Line($"/// <summary>Builder for When matchers. Captures predicate, awaits Returns(value).</summary>");
		w.Line($"public sealed class {builderType} : global::KnockOff.IWhenBuilder<{config.DelegateType}, {config.ReturnType}>");
		using (w.Braces())
		{
			w.Line($"private readonly {config.InterceptorClassName} _interceptor;");
			w.Line($"private readonly {predicateType} _predicate;");
			w.Line();

			w.Line($"public {builderType}({config.InterceptorClassName} interceptor, {predicateType} predicate)");
			using (w.Braces())
			{
				w.Line("_interceptor = interceptor;");
				w.Line("_predicate = predicate;");
			}
			w.Line();

			// For async methods (Task<T>/ValueTask<T>), generate Returns(TInner) that auto-wraps
			if (isAsync)
			{
				// Returns accepts the unwrapped type and wraps internally
				w.Line($"/// <summary>Configures the return value. Auto-wrapped in {(isTaskT ? "Task.FromResult" : "new ValueTask")}.</summary>");
				w.Line($"public {chainType} Returns({innerType} value)");
				using (w.Braces())
				{
					w.Line($"_interceptor.{whenChainField} ??= new global::System.Collections.Generic.List<{matcherType}>();");
					// Wrap with Task.FromResult or new ValueTask<T>
					if (isTaskT)
						w.Line($"_interceptor.{whenChainField}.Add(new WhenMatcherValue{suffix}(_predicate, global::System.Threading.Tasks.Task.FromResult(value)));");
					else
						w.Line($"_interceptor.{whenChainField}.Add(new WhenMatcherValue{suffix}(_predicate, new global::System.Threading.Tasks.ValueTask<{innerType}>(value)));");
					w.Line($"return new {chainType}(_interceptor);");
				}
				w.Line();
				// Explicit interface implementation wraps too
				if (isTaskT)
					w.Line($"global::KnockOff.IWhenChain<{config.DelegateType}, {config.ReturnType}> global::KnockOff.IWhenBuilder<{config.DelegateType}, {config.ReturnType}>.Returns({config.ReturnType} value) => Returns(value.Result);");
				else
					w.Line($"global::KnockOff.IWhenChain<{config.DelegateType}, {config.ReturnType}> global::KnockOff.IWhenBuilder<{config.DelegateType}, {config.ReturnType}>.Returns({config.ReturnType} value) => Returns(value.Result);");
			}
			else
			{
				// Non-async: Returns accepts the full return type directly
				w.Line($"public {chainType} Returns({config.ReturnType} value)");
				using (w.Braces())
				{
					w.Line($"_interceptor.{whenChainField} ??= new global::System.Collections.Generic.List<{matcherType}>();");
					w.Line($"_interceptor.{whenChainField}.Add(new WhenMatcherValue{suffix}(_predicate, value));");
					w.Line($"return new {chainType}(_interceptor);");
				}
				w.Line();
				// Explicit interface implementation for IWhenBuilder.Returns
				w.Line($"global::KnockOff.IWhenChain<{config.DelegateType}, {config.ReturnType}> global::KnockOff.IWhenBuilder<{config.DelegateType}, {config.ReturnType}>.Returns({config.ReturnType} value) => Returns(value);");
			}
		}
		w.Line();
	}

	/// <summary>
	/// Renders the WhenChainImpl nested class.
	/// </summary>
	public static void RenderWhenChainImpl(CodeWriter w, WhenChainConfig config)
	{
		var suffix = GetSuffix(config.SignatureSuffix);
		var whenChainField = $"_whenChain{suffix}";
		var whenChainHeadField = $"_whenChainHead{suffix}";
		var whenVerifiableField = $"_whenVerifiable{suffix}";
		var predicateType = BuildPredicateType(config.Parameters);
		var paramTypeList = BuildParamTypeList(config.Parameters);
		var matcherType = $"WhenMatcher{suffix}";
		var builderType = $"WhenBuilder{suffix}";
		var chainType = $"WhenChain{suffix}";

		w.Line($"/// <summary>When chain implementation with ThenCall, ThenNone, verification support.</summary>");
		w.Line($"public sealed class {chainType} : global::KnockOff.IWhenChain<{config.DelegateType}, {config.ReturnType}>");
		using (w.Braces())
		{
			w.Line($"private readonly {config.InterceptorClassName} _interceptor;");
			w.Line();

			w.Line($"public {chainType}({config.InterceptorClassName} interceptor) => _interceptor = interceptor;");
			w.Line();

			// ThenWhen with values and predicate
			if (config.Parameters.Count > 0)
			{
				w.Line($"/// <summary>Adds another matcher with exact value matching.</summary>");
				w.Line($"public {builderType} ThenWhen({paramTypeList})");
				using (w.Braces())
				{
					// Build equality predicate - lambda params are prefixed with _ to avoid shadowing method params
					var lambdaParams = BuildLambdaParamsForEquality(config.Parameters);
					var predicateBody = BuildEqualityPredicateBody(config.Parameters);
					w.Line($"return new {builderType}(_interceptor, ({lambdaParams}) => {predicateBody});");
				}
				w.Line();

				// ThenWhen with predicate
				w.Line($"/// <summary>Adds another matcher with predicate matching.</summary>");
				w.Line($"public {builderType} ThenWhen({predicateType} predicate)");
				using (w.Braces())
				{
					w.Line($"return new {builderType}(_interceptor, predicate);");
				}
				w.Line();
			}

			// ThenCall - terminal with callback
			w.Line($"/// <summary>Adds an unconditional callback as terminal matcher.</summary>");
			w.Line($"public global::KnockOff.IWhenTracking ThenCall({config.DelegateType} callback)");
			using (w.Braces())
			{
				w.Line($"_interceptor.{whenChainField} ??= new global::System.Collections.Generic.List<{matcherType}>();");
				w.Line($"_interceptor.{whenChainField}.Add(new WhenMatcherCall{suffix}(callback));");
				w.Line("return this;");
			}
			w.Line();

			// ThenNone - terminal that never matches
			w.Line($"/// <summary>Closes chain with no matcher. Falls through when exhausted.</summary>");
			w.Line($"public global::KnockOff.IWhenTracking ThenNone()");
			using (w.Braces())
			{
				w.Line($"_interceptor.{whenChainField} ??= new global::System.Collections.Generic.List<{matcherType}>();");
				w.Line($"_interceptor.{whenChainField}.Add(new WhenMatcherNone{suffix}());");
				w.Line("return this;");
			}
			w.Line();

			// Verify - checks if chain reached terminal state
			w.Line($"/// <summary>Verifies the When chain was fully consumed (reached terminal state).</summary>");
			w.Line("public void Verify()");
			using (w.Braces())
			{
				w.Line($"if (_interceptor.{whenChainField} == null || _interceptor.{whenChainField}.Count == 0) return;");
				w.Line($"var head = _interceptor.{whenChainHeadField};");
				w.Line($"var count = _interceptor.{whenChainField}.Count;");
				w.Line("// Chain is complete if HEAD reached a terminal matcher or exhausted");
				w.Line("if (head < count && !_interceptor." + whenChainField + "[head].IsTerminal)");
				using (w.Braces())
				{
					w.Line("throw new global::KnockOff.VerificationException(global::KnockOff.VerificationFailure.SequenceIncomplete(\"When chain\", count, head));");
				}
			}
			w.Line();

			// Reset - resets HEAD and all matcher CallCounts
			w.Line($"/// <summary>Resets When chain HEAD and all matcher call counts.</summary>");
			w.Line("public void Reset()");
			using (w.Braces())
			{
				w.Line($"_interceptor.{whenChainHeadField} = 0;");
				w.Line($"if (_interceptor.{whenChainField} != null)");
				using (w.Braces())
				{
					w.Line($"foreach (var matcher in _interceptor.{whenChainField})");
					w.Line("\tmatcher.CallCount = 0;");
				}
			}
			w.Line();

			// Verifiable - marks for Stub.Verify()
			w.Line($"/// <summary>Marks this When chain for verification by Stub.Verify().</summary>");
			w.Line($"public {chainType} Verifiable()");
			using (w.Braces())
			{
				w.Line($"_interceptor.{whenVerifiableField} = true;");
				w.Line("return this;");
			}
			w.Line();

			// Explicit interface implementations for IWhenChain.Verifiable and IWhenTracking.Verifiable
			w.Line($"global::KnockOff.IWhenChain<{config.DelegateType}, {config.ReturnType}> global::KnockOff.IWhenChain<{config.DelegateType}, {config.ReturnType}>.Verifiable() => Verifiable();");
			w.Line("global::KnockOff.IWhenTracking global::KnockOff.IWhenTracking.Verifiable() => Verifiable();");
		}
		w.Line();
	}

	/// <summary>
	/// Renders When chain reset logic (for inclusion in a Reset() method).
	/// </summary>
	public static void RenderWhenResetLogic(CodeWriter w, WhenChainConfig config)
	{
		var suffix = GetSuffix(config.SignatureSuffix);
		var whenChainField = $"_whenChain{suffix}";
		var whenChainHeadField = $"_whenChainHead{suffix}";

		w.Line($"{whenChainHeadField} = 0;");
		w.Line($"if ({whenChainField} != null)");
		using (w.Braces())
		{
			w.Line($"foreach (var matcher in {whenChainField})");
			w.Line("\tmatcher.CallCount = 0;");
		}
	}

	#region Void Method Support

	/// <summary>
	/// Renders the When() entry point methods for void methods (value and predicate overloads).
	/// For void methods, When() returns IVoidWhenChain directly (no builder needed).
	/// </summary>
	public static void RenderVoidWhenEntryPoints(CodeWriter w, WhenChainConfig config)
	{
		// When() requires parameters - parameterless methods cannot use When()
		if (config.Parameters.Count == 0) return;

		var suffix = GetSuffix(config.SignatureSuffix);
		var whenChainField = $"_whenChain{suffix}";
		var predicateType = UnifiedInterceptorBuilder.BuildWhenPredicateType(config.Parameters);
		var paramTypeList = BuildParamTypeList(config.Parameters);
		var matcherType = $"VoidWhenMatcher{suffix}";
		var chainType = $"VoidWhenChain{suffix}";

		// When() value overload - exact value matching via Object.Equals
		// Returns concrete type to enable fluent ThenWhen chaining
		w.Line($"/// <summary>Configures parameter-specific matching with exact values for void method. Returns chain directly.</summary>");
		w.Line($"public {chainType} When({paramTypeList})");
		using (w.Braces())
		{
			// Initialize When chain if null
			w.Line($"{whenChainField} ??= new global::System.Collections.Generic.List<{matcherType}>();");

			// Build equality predicate - use indexed lambda params to avoid keyword conflicts
			var lambdaParams = BuildLambdaParamsForEquality(config.Parameters);
			var predicateBody = BuildEqualityPredicateBody(config.Parameters);

			// Add matcher immediately (no builder needed for void)
			w.Line($"var matcher = new VoidWhenMatcherPredicate{suffix}(({lambdaParams}) => {predicateBody});");
			w.Line($"{whenChainField}.Add(matcher);");
			w.Line($"return new {chainType}(this, matcher);");
		}
		w.Line();

		// When() predicate overload - Func<T1, T2, bool>
		// Returns concrete type to enable fluent ThenWhen chaining
		w.Line($"/// <summary>Configures parameter-specific matching with predicate for void method. Returns chain directly.</summary>");
		w.Line($"public {chainType} When({predicateType} predicate)");
		using (w.Braces())
		{
			// Initialize When chain if null
			w.Line($"{whenChainField} ??= new global::System.Collections.Generic.List<{matcherType}>();");
			w.Line($"var matcher = new VoidWhenMatcherPredicate{suffix}(predicate);");
			w.Line($"{whenChainField}.Add(matcher);");
			w.Line($"return new {chainType}(this, matcher);");
		}
		w.Line();
	}

	/// <summary>
	/// Renders the void When chain invoke check logic for insertion at the top of Invoke().
	/// For void methods - executes callback if configured, no return statement.
	/// </summary>
	public static void RenderVoidWhenInvokeCheck(CodeWriter w, WhenChainConfig config)
	{
		var suffix = GetSuffix(config.SignatureSuffix);
		var whenChainField = $"_whenChain{suffix}";
		var whenChainHeadField = $"_whenChainHead{suffix}";
		var callbackArgs = BuildCallbackArgs(config.Parameters);

		w.Line($"// When chain - check HEAD matcher first (highest priority)");
		w.Line($"if ({whenChainField} != null && {whenChainHeadField} < {whenChainField}.Count)");
		using (w.Braces())
		{
			w.Line($"var matcher = {whenChainField}[{whenChainHeadField}];");
			w.Line($"if (matcher.Matches({callbackArgs}))");
			using (w.Braces())
			{
				w.Line("matcher.CallCount++;");
				w.Line();
				w.Line("// Advance HEAD unless at last matcher (which repeats)");
				w.Line($"if ({whenChainHeadField} < {whenChainField}.Count - 1)");
				using (w.Braces())
				{
					w.Line($"{whenChainHeadField}++;");
				}
				w.Line("// At last matcher: never advance (repeat behavior for both ThenWhen and ThenCall)");
				w.Line();

				// Execute (void) and return - no return value
				w.Line($"matcher.Execute({callbackArgs});");
				w.Line("return;");
			}
			w.Line("else if (matcher.IsTerminal)");
			using (w.Braces())
			{
				w.Line("// ThenNone: didn't match (always false), exhaust by advancing past it");
				w.Line($"{whenChainHeadField}++;");
			}
			w.Line("// Non-terminal didn't match: fall through to rest of priority chain");
		}
		w.Line();
	}

	/// <summary>
	/// Renders the VoidWhenMatcher abstract base class and its implementations.
	/// </summary>
	public static void RenderVoidWhenMatcherClasses(CodeWriter w, WhenChainConfig config)
	{
		var suffix = GetSuffix(config.SignatureSuffix);
		var matchParams = BuildMatchParams(config.Parameters);
		var callbackArgs = BuildCallbackArgs(config.Parameters);
		var predicateType = BuildPredicateType(config.Parameters);

		// VoidWhenMatcher abstract base
		w.Line($"/// <summary>Abstract base for void When chain matchers.</summary>");
		w.Line($"internal abstract class VoidWhenMatcher{suffix}");
		using (w.Braces())
		{
			w.Line($"public abstract bool Matches({matchParams});");
			w.Line($"public abstract void Execute({matchParams});");
			w.Line("public abstract bool IsTerminal { get; }");
			w.Line("public int CallCount { get; set; }");
			w.Line($"public {config.DelegateType}? Callback {{ get; set; }}");
		}
		w.Line();

		// VoidWhenMatcherPredicate - predicate with optional callback
		w.Line($"/// <summary>Matcher that uses a predicate and optionally invokes a callback.</summary>");
		w.Line($"private sealed class VoidWhenMatcherPredicate{suffix} : VoidWhenMatcher{suffix}");
		using (w.Braces())
		{
			w.Line($"private readonly {predicateType} _predicate;");
			w.Line();
			w.Line($"public VoidWhenMatcherPredicate{suffix}({predicateType} predicate) => _predicate = predicate;");
			w.Line();
			w.Line($"public override bool Matches({matchParams}) => _predicate({callbackArgs});");
			w.Line($"public override void Execute({matchParams}) {{ Callback?.Invoke({callbackArgs}); }}");
			w.Line("public override bool IsTerminal => false;");
		}
		w.Line();

		// VoidWhenMatcherCall - callback, always matches, terminal
		w.Line($"/// <summary>Matcher that always matches and invokes a callback. Terminal.</summary>");
		w.Line($"private sealed class VoidWhenMatcherCall{suffix} : VoidWhenMatcher{suffix}");
		using (w.Braces())
		{
			w.Line($"private readonly {config.DelegateType} _callback;");
			w.Line();
			w.Line($"public VoidWhenMatcherCall{suffix}({config.DelegateType} callback) => _callback = callback;");
			w.Line();
			w.Line($"public override bool Matches({matchParams}) => true;");
			w.Line($"public override void Execute({matchParams}) => _callback({callbackArgs});");
			w.Line("public override bool IsTerminal => true;");
		}
		w.Line();

		// VoidWhenMatcherNone - never matches, terminal
		w.Line($"/// <summary>Matcher that never matches. Used to close chain without fallback. Terminal.</summary>");
		w.Line($"private sealed class VoidWhenMatcherNone{suffix} : VoidWhenMatcher{suffix}");
		using (w.Braces())
		{
			w.Line($"public override bool Matches({matchParams}) => false;");
			w.Line($"public override void Execute({matchParams}) {{ }}");
			w.Line("public override bool IsTerminal => true;");
		}
		w.Line();
	}

	/// <summary>
	/// Renders the VoidWhenChainImpl nested class.
	/// </summary>
	public static void RenderVoidWhenChainImpl(CodeWriter w, WhenChainConfig config)
	{
		var suffix = GetSuffix(config.SignatureSuffix);
		var whenChainField = $"_whenChain{suffix}";
		var whenChainHeadField = $"_whenChainHead{suffix}";
		var whenVerifiableField = $"_whenVerifiable{suffix}";
		var predicateType = BuildPredicateType(config.Parameters);
		var paramTypeList = BuildParamTypeList(config.Parameters);
		var matcherType = $"VoidWhenMatcher{suffix}";
		var chainType = $"VoidWhenChain{suffix}";

		w.Line($"/// <summary>Void When chain implementation with Call, ThenWhen, ThenCall, ThenNone, verification support.</summary>");
		w.Line($"public sealed class {chainType} : global::KnockOff.IVoidWhenChain<{config.DelegateType}>");
		using (w.Braces())
		{
			w.Line($"private readonly {config.InterceptorClassName} _interceptor;");
			w.Line($"private readonly {matcherType} _currentMatcher;");
			w.Line();

			w.Line($"internal {chainType}({config.InterceptorClassName} interceptor, {matcherType} currentMatcher)");
			using (w.Braces())
			{
				w.Line("_interceptor = interceptor;");
				w.Line("_currentMatcher = currentMatcher;");
			}
			w.Line();

			// Call - sets optional callback on current matcher
			// Returns concrete type to enable fluent ThenWhen chaining
			w.Line($"/// <summary>Sets an optional callback to invoke when this matcher matches.</summary>");
			w.Line($"public {chainType} Call({config.DelegateType} callback)");
			using (w.Braces())
			{
				w.Line("_currentMatcher.Callback = callback;");
				w.Line("return this;");
			}
			w.Line();
			// Explicit interface implementation for IVoidWhenChain.Call
			w.Line($"global::KnockOff.IVoidWhenChain<{config.DelegateType}> global::KnockOff.IVoidWhenChain<{config.DelegateType}>.Call({config.DelegateType} callback) => Call(callback);");
			w.Line();

			// ThenWhen with values and predicate
			if (config.Parameters.Count > 0)
			{
				w.Line($"/// <summary>Adds another matcher with exact value matching.</summary>");
				w.Line($"public {chainType} ThenWhen({paramTypeList})");
				using (w.Braces())
				{
					// Build equality predicate - lambda params are prefixed with _ to avoid shadowing method params
					var lambdaParams = BuildLambdaParamsForEquality(config.Parameters);
					var predicateBody = BuildEqualityPredicateBody(config.Parameters);
					w.Line($"_interceptor.{whenChainField} ??= new global::System.Collections.Generic.List<{matcherType}>();");
					w.Line($"var matcher = new VoidWhenMatcherPredicate{suffix}(({lambdaParams}) => {predicateBody});");
					w.Line($"_interceptor.{whenChainField}.Add(matcher);");
					w.Line($"return new {chainType}(_interceptor, matcher);");
				}
				w.Line();

				// ThenWhen with predicate
				w.Line($"/// <summary>Adds another matcher with predicate matching.</summary>");
				w.Line($"public {chainType} ThenWhen({predicateType} predicate)");
				using (w.Braces())
				{
					w.Line($"_interceptor.{whenChainField} ??= new global::System.Collections.Generic.List<{matcherType}>();");
					w.Line($"var matcher = new VoidWhenMatcherPredicate{suffix}(predicate);");
					w.Line($"_interceptor.{whenChainField}.Add(matcher);");
					w.Line($"return new {chainType}(_interceptor, matcher);");
				}
				w.Line();
			}

			// ThenCall - terminal with callback
			w.Line($"/// <summary>Adds an unconditional callback as terminal matcher.</summary>");
			w.Line($"public global::KnockOff.IWhenTracking ThenCall({config.DelegateType} callback)");
			using (w.Braces())
			{
				w.Line($"_interceptor.{whenChainField} ??= new global::System.Collections.Generic.List<{matcherType}>();");
				w.Line($"_interceptor.{whenChainField}.Add(new VoidWhenMatcherCall{suffix}(callback));");
				w.Line("return this;");
			}
			w.Line();

			// ThenNone - terminal that never matches
			w.Line($"/// <summary>Closes chain with no matcher. Falls through when exhausted.</summary>");
			w.Line($"public global::KnockOff.IWhenTracking ThenNone()");
			using (w.Braces())
			{
				w.Line($"_interceptor.{whenChainField} ??= new global::System.Collections.Generic.List<{matcherType}>();");
				w.Line($"_interceptor.{whenChainField}.Add(new VoidWhenMatcherNone{suffix}());");
				w.Line("return this;");
			}
			w.Line();

			// Verify() - checks if chain reached terminal state (from ITracking)
			w.Line($"/// <summary>Verifies the When chain was fully consumed (reached terminal state).</summary>");
			w.Line("public void Verify()");
			using (w.Braces())
			{
				w.Line($"if (_interceptor.{whenChainField} == null || _interceptor.{whenChainField}.Count == 0) return;");
				w.Line($"var head = _interceptor.{whenChainHeadField};");
				w.Line($"var count = _interceptor.{whenChainField}.Count;");
				w.Line("// Chain is complete if HEAD reached a terminal matcher or exhausted");
				w.Line("if (head < count && !_interceptor." + whenChainField + "[head].IsTerminal)");
				using (w.Braces())
				{
					w.Line("throw new global::KnockOff.VerificationException(global::KnockOff.VerificationFailure.SequenceIncomplete(\"When chain\", count, head));");
				}
			}
			w.Line();

			// Verify(Times) - parameter-specific verification for current matcher
			w.Line($"/// <summary>Verifies this specific matcher was called the expected number of times.</summary>");
			w.Line("public void Verify(global::KnockOff.Times times)");
			using (w.Braces())
			{
				w.Line("if (!times.Validate(_currentMatcher.CallCount))");
				using (w.Braces())
				{
					w.Line("throw new global::KnockOff.VerificationException(new global::KnockOff.VerificationFailure(\"When matcher\", times, _currentMatcher.CallCount));");
				}
			}
			w.Line();

			// Reset - resets HEAD and all matcher CallCounts
			w.Line($"/// <summary>Resets When chain HEAD and all matcher call counts.</summary>");
			w.Line("public void Reset()");
			using (w.Braces())
			{
				w.Line($"_interceptor.{whenChainHeadField} = 0;");
				w.Line($"if (_interceptor.{whenChainField} != null)");
				using (w.Braces())
				{
					w.Line($"foreach (var matcher in _interceptor.{whenChainField})");
					w.Line("\tmatcher.CallCount = 0;");
				}
			}
			w.Line();

			// Verifiable - marks for Stub.Verify() - returns concrete type for chaining
			w.Line($"/// <summary>Marks this When chain for verification by Stub.Verify().</summary>");
			w.Line($"public {chainType} Verifiable()");
			using (w.Braces())
			{
				w.Line($"_interceptor.{whenVerifiableField} = true;");
				w.Line("return this;");
			}
			w.Line();

			// Explicit interface implementations
			w.Line($"global::KnockOff.IVoidWhenChain<{config.DelegateType}> global::KnockOff.IVoidWhenChain<{config.DelegateType}>.Verifiable() => Verifiable();");
			w.Line("global::KnockOff.IWhenTracking global::KnockOff.IWhenTracking.Verifiable() => Verifiable();");
		}
		w.Line();
	}

	#endregion

	#region Helper Methods

	private static string GetSuffix(string? signatureSuffix)
	{
		return signatureSuffix == null ? "" : $"_{signatureSuffix}";
	}

	/// <summary>
	/// Analyzes a return type for async patterns and extracts the inner type.
	/// </summary>
	/// <param name="returnType">The fully-qualified return type (e.g., "global::System.Threading.Tasks.Task&lt;string&gt;").</param>
	/// <returns>Tuple of (innerType, isTaskT, isValueTaskT).</returns>
	private static (string InnerType, bool IsTaskT, bool IsValueTaskT) GetAsyncTypeInfo(string returnType)
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

	private static string BuildMatchParams(EquatableArray<ParameterModel> parameters)
	{
		if (parameters.Count == 0) return "";
		return string.Join(", ", parameters.Select(p => $"{p.Type} {p.EscapedName}"));
	}

	private static string BuildPredicateType(EquatableArray<ParameterModel> parameters)
	{
		if (parameters.Count == 0)
			return "global::System.Func<bool>";

		var paramTypes = string.Join(", ", parameters.Select(p => p.Type));
		return $"global::System.Func<{paramTypes}, bool>";
	}

	private static string BuildParamTypeList(EquatableArray<ParameterModel> parameters)
	{
		if (parameters.Count == 0) return "";
		return string.Join(", ", parameters.Select(p => $"{p.Type} {p.EscapedName}"));
	}

	private static string BuildCallbackArgs(EquatableArray<ParameterModel> parameters)
	{
		var parts = new List<string>();
		foreach (var p in parameters)
		{
			parts.Add($"{p.RefPrefix}{p.EscapedName}");
		}
		return string.Join(", ", parts);
	}

	private static string BuildLambdaParamsForEquality(EquatableArray<ParameterModel> parameters)
	{
		if (parameters.Count == 0) return "";
		// Use indexed names to avoid conflicts with C# keywords and the method parameters
		return string.Join(", ", Enumerable.Range(0, parameters.Count).Select(i => $"_arg{i}"));
	}

	private static string BuildEqualityPredicateBody(EquatableArray<ParameterModel> parameters)
	{
		if (parameters.Count == 0) return "true";

		var parts = new List<string>();
		for (int i = 0; i < parameters.Count; i++)
		{
			// Lambda param is indexed (_arg0, _arg1, etc.), method param is the expected value
			parts.Add($"global::System.Object.Equals(_arg{i}, {parameters.GetArray()![i].EscapedName})");
		}
		return string.Join(" && ", parts);
	}

	#endregion
}
