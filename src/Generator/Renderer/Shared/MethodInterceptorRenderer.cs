// src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs
#nullable enable
using System.Collections.Generic;
using System.Linq;
using KnockOff.Builder;
using KnockOff.Model.Shared;

namespace KnockOff.Renderer.Shared;

/// <summary>
/// Renders method interceptor classes for both inline and flat stubs.
/// Generates OnCall() returning IMethodCallBuilder (repeating callback, elevatable to sequence via ThenCall),
/// nested MethodCallBuilderImpl and MethodSequenceImpl classes, Invoke methods, and verification.
/// </summary>
internal static class MethodInterceptorRenderer
{
	/// <summary>
	/// Renders a complete method interceptor class.
	/// For single-signature methods, generates a simple interceptor.
	/// For overload groups, generates per-signature delegates, sequences, and OnCall overloads.
	/// </summary>
	public static void RenderInterceptorClass(
		CodeWriter w,
		UnifiedMethodInterceptorModel model,
		InterceptorRenderOptions options)
	{
		var typeParams = options.InterceptorTypeParameters;
		var constraints = options.InterceptorConstraints;
		var classDecl = $"public sealed class {model.InterceptorClassName}{typeParams}{constraints}";

		w.Line($"/// <summary>Tracks and configures behavior for {model.MethodName}.</summary>");
		using (w.Block(classDecl))
		{
			if (model.Overloads.Count == 0)
			{
				RenderSingleSignatureContent(w, model, options);
			}
			else
			{
				RenderOverloadGroupContent(w, model, options);
			}
		}
		w.Line();
	}

	#region Single-Signature Interceptor

	private static void RenderSingleSignatureContent(
		CodeWriter w,
		UnifiedMethodInterceptorModel model,
		InterceptorRenderOptions options)
	{
		var ownerWithParams = GetOwnerWithParams(model);
		var delegateType = model.OnCallDelegateType.TrimEnd('?');

		// Source field for Source(T) feature - uses declaring interface type
		if (!string.IsNullOrEmpty(model.DeclaringInterface))
		{
			w.Line($"/// <summary>Source object to delegate to when no OnCall is configured.</summary>");
			w.Line($"internal {model.DeclaringInterface}? _source;");
			w.Line();
		}

		// Custom delegate if needed
		if (model.NeedsCustomDelegate && model.CustomDelegateSignature != null)
		{
			w.Line($"/// <summary>Delegate for {model.MethodName}.</summary>");
			w.Line(model.CustomDelegateSignature);
			w.Line();
		}

		// OnCall storage - single repeating callback (separate from sequence)
		w.Line($"private {delegateType}? _onCall;");
		w.Line("private MethodCallBuilderImpl? _onCallTracking;");
		w.Line();

		// Value storage for Returns(value) overload (skip for void/ref/out methods)
		var hasRefOrOut = model.Parameters.Any(p => p.RefKind == Microsoft.CodeAnalysis.RefKind.Ref || p.RefKind == Microsoft.CodeAnalysis.RefKind.Out);
		var canHaveValueOverload = !model.IsVoid && !hasRefOrOut;
		if (canHaveValueOverload)
		{
			var (valueStorageType, isTaskT, isValueTaskT) = GetAsyncTypeInfo(model.ReturnType);
			// Use non-nullable storage with default! - _hasReturnsValue distinguishes "not set" from "set to null/default"
			w.Line($"private {valueStorageType} _returnsValue = default!;");
			w.Line("private bool _hasReturnsValue;");
			w.Line("private MethodCallBuilderImpl? _returnsValueTracking;");
			w.Line();
		}

		// Simplified callback storage for Task<T>/ValueTask<T> methods (accepts Func<..., TInnerType>)
		var (innerType, isAsyncTaskT, isAsyncValueTaskT) = GetAsyncTypeInfo(model.ReturnType);
		var isAsyncWithInnerType = isAsyncTaskT || isAsyncValueTaskT;
		if (isAsyncWithInnerType && !hasRefOrOut)
		{
			var simplifiedDelegateType = BuildSimplifiedDelegateType(model.Parameters, innerType);
			w.Line($"private {simplifiedDelegateType}? _onCallSimplified;");
			w.Line("private MethodCallBuilderImpl? _onCallSimplifiedTracking;");
			w.Line();
		}

		// Simplified void callback storage for Task/ValueTask methods (accepts Action<...>)
		var (isVoidTask, isVoidValueTask) = GetVoidAsyncInfo(model.ReturnType);
		var isVoidAsync = isVoidTask || isVoidValueTask;
		if (isVoidAsync && !hasRefOrOut)
		{
			var voidDelegateType = BuildSimplifiedVoidDelegateType(model.Parameters);
			w.Line($"private {voidDelegateType}? _onCallSimplifiedVoid;");
			w.Line("private MethodCallBuilderImpl? _onCallSimplifiedVoidTracking;");
			w.Line();
		}

		// Sequence storage - list of callbacks that each run once
		w.Line($"private global::System.Collections.Generic.List<({delegateType} Callback, MethodCallBuilderImpl Tracking)>? _sequence;");
		w.Line("private int _sequenceIndex;");
		w.Line("private bool _repeatLastValue = true;");
		w.Line();

		// When chain storage - parameter-specific matching (for methods with parameters and no ref/out)
		// Non-void methods use WhenMatcher, void methods use VoidWhenMatcher
		var canHaveWhenChain = !model.IsVoid && model.Parameters.Count > 0 && !hasRefOrOut;
		var canHaveVoidWhenChain = model.IsVoid && model.Parameters.Count > 0 && !hasRefOrOut;
		if (canHaveWhenChain)
		{
			w.Line("private global::System.Collections.Generic.List<WhenMatcher>? _whenChain;");
			w.Line("private int _whenChainHead;");
			w.Line("private bool _whenVerifiable;");
			w.Line();
		}
		if (canHaveVoidWhenChain)
		{
			w.Line("private global::System.Collections.Generic.List<VoidWhenMatcher>? _whenChain;");
			w.Line("private int _whenChainHead;");
			w.Line("private bool _whenVerifiable;");
			w.Line();
		}

		// Verifiable state
		w.Line("private bool _isVerifiable;");
		w.Line("private global::KnockOff.Times? _verifiableTimes;");
		w.Line();

		// Track unconfigured calls
		w.Line("private int _unconfiguredCallCount;");
		w.Line("/// <summary>Count of calls that were not handled by any configured behavior (used for class stub base fallback).</summary>");
		w.Line("internal int UnconfiguredCallCount => _unconfiguredCallCount;");
		w.Line();
		if (model.LastArgType != null)
		{
			var nullableType = model.LastArgType.EndsWith("?") ? model.LastArgType : $"{model.LastArgType}?";
			w.Line($"private {nullableType} _unconfiguredLastArg;");
		}
		if (model.LastArgsType != null)
		{
			var nullableType = model.LastArgsType.EndsWith("?") ? model.LastArgsType : $"{model.LastArgsType}?";
			w.Line($"private {nullableType} _unconfiguredLastArgs;");
		}
		w.Line();

		// Backward compatibility: aggregate tracking properties
		RenderBackwardCompatibleTrackingProperties(w, model.TrackableParameters, model.LastArgType, model.LastArgsType, canHaveValueOverload,
			hasSimplifiedCallback: isAsyncWithInnerType && !hasRefOrOut,
			hasSimplifiedVoidCallback: isVoidAsync && !hasRefOrOut);
		w.Line();

		// Verify() methods for direct interceptor verification
		RenderInterceptorVerifyMethods(w, model.MethodName);

		// OnCall() - repeating callback, returns concrete builder for ThenReturns access
		w.Line($"/// <summary>Configures callback that repeats indefinitely. Returns builder for sequence chaining.</summary>");
		w.Line($"public MethodCallBuilderImpl OnCall({delegateType} callback)");
		using (w.Braces())
		{
			w.Line("_sequence = null;");
			w.Line("_sequenceIndex = 0;");
			w.Line("_isVerifiable = false;");
			w.Line("_verifiableTimes = null;");
			// Clear value storage (mutual exclusivity with Returns(value))
			if (canHaveValueOverload)
			{
				w.Line("_hasReturnsValue = false;");
				w.Line("_returnsValue = default!;");
				w.Line("_returnsValueTracking = null;");
			}
			// Clear simplified callback storage (mutual exclusivity)
			if (isAsyncWithInnerType && !hasRefOrOut)
			{
				w.Line("_onCallSimplified = null;");
				w.Line("_onCallSimplifiedTracking = null;");
			}
			if (isVoidAsync && !hasRefOrOut)
			{
				w.Line("_onCallSimplifiedVoid = null;");
				w.Line("_onCallSimplifiedVoidTracking = null;");
			}
			w.Line("_onCall = callback;");
			w.Line("_onCallTracking = new MethodCallBuilderImpl(this);");
			w.Line("return _onCallTracking;");
		}
		w.Line();

		// Returns(value) - repeating value return, returns IMethodTracking
		if (canHaveValueOverload)
		{
			var (valueStorageType, isTaskT, isValueTaskT) = GetAsyncTypeInfo(model.ReturnType);
			w.Line($"/// <summary>Configures return value that repeats indefinitely. Returns builder for sequence chaining.</summary>");
			w.Line($"public MethodCallBuilderImpl Returns({valueStorageType} value)");
			using (w.Braces())
			{
				w.Line("_sequence = null;");
				w.Line("_sequenceIndex = 0;");
				w.Line("_isVerifiable = false;");
				w.Line("_verifiableTimes = null;");
				// Clear callback storage (mutual exclusivity)
				w.Line("_onCall = null;");
				w.Line("_onCallTracking = null;");
				// Clear simplified callback storage (mutual exclusivity)
				if (isAsyncWithInnerType && !hasRefOrOut)
				{
					w.Line("_onCallSimplified = null;");
					w.Line("_onCallSimplifiedTracking = null;");
				}
				// Set value storage
				w.Line("_hasReturnsValue = true;");
				w.Line("_returnsValue = value;");
				w.Line("_returnsValueTracking = new MethodCallBuilderImpl(this);");
				w.Line("return _returnsValueTracking;");
			}
			w.Line();

			// Returns(first, params rest) - creates sequence from multiple values
			var discardPrefix = BuildDiscardLambdaPrefix(model.Parameters.Count);
			w.Line($"/// <summary>Configures sequence of return values. Each value returned once, last repeats.</summary>");
			w.Line($"public MethodSequenceImpl Returns({valueStorageType} first, params {valueStorageType}[] rest)");
			using (w.Braces())
			{
				// Start with OnCall for first value, then ThenReturns to get MethodSequenceImpl
				// If rest is empty, we still return a sequence (with just first value repeating)
				if (isTaskT)
				{
					w.Line($"var builder = OnCall({discardPrefix} => global::System.Threading.Tasks.Task.FromResult(first));");
				}
				else if (isValueTaskT)
				{
					w.Line($"var builder = OnCall({discardPrefix} => new global::System.Threading.Tasks.ValueTask<{valueStorageType}>(first));");
				}
				else
				{
					w.Line($"var builder = OnCall({discardPrefix} => first);");
				}
				w.Line("if (rest.Length == 0)");
				using (w.Braces())
				{
					// Return a sequence with just the first value (elevate to sequence mode)
					w.Line("return builder.ThenReturns(first);");
				}
				// First rest value elevates to sequence, then loop for remaining
				w.Line("var seq = builder.ThenReturns(rest[0]);");
				w.Line("for (int i = 1; i < rest.Length; i++)");
				using (w.Braces())
				{
					w.Line("seq = seq.ThenReturns(rest[i]);");
				}
				w.Line("return seq;");
			}
			w.Line();
		}

		// OnCall(Func<..., TInnerType>) - simplified callback for Task<T>/ValueTask<T> methods
		if (isAsyncWithInnerType && !hasRefOrOut)
		{
			var simplifiedDelegateType = BuildSimplifiedDelegateType(model.Parameters, innerType);
			w.Line($"/// <summary>Configures callback returning unwrapped value. Result auto-wrapped in {(isAsyncTaskT ? "Task.FromResult" : "new ValueTask")}.</summary>");
			w.Line($"public MethodCallBuilderImpl OnCall({simplifiedDelegateType} callback)");
			using (w.Braces())
			{
				w.Line("_sequence = null;");
				w.Line("_sequenceIndex = 0;");
				w.Line("_isVerifiable = false;");
				w.Line("_verifiableTimes = null;");
				// Clear value storage (mutual exclusivity)
				if (canHaveValueOverload)
				{
					w.Line("_hasReturnsValue = false;");
					w.Line("_returnsValue = default!;");
					w.Line("_returnsValueTracking = null;");
				}
				// Clear async callback storage (mutual exclusivity)
				w.Line("_onCall = null;");
				w.Line("_onCallTracking = null;");
				// Set simplified callback storage
				w.Line("_onCallSimplified = callback;");
				w.Line("_onCallSimplifiedTracking = new MethodCallBuilderImpl(this);");
				w.Line("return _onCallSimplifiedTracking;");
			}
			w.Line();
		}

		// OnCall(Action<...>) - simplified void callback for Task/ValueTask methods
		if (isVoidAsync && !hasRefOrOut)
		{
			var voidDelegateType = BuildSimplifiedVoidDelegateType(model.Parameters);
			w.Line($"/// <summary>Configures callback action. {(isVoidTask ? "Task.CompletedTask" : "default(ValueTask)")} auto-returned.</summary>");
			w.Line($"public MethodCallBuilderImpl OnCall({voidDelegateType} callback)");
			using (w.Braces())
			{
				w.Line("_sequence = null;");
				w.Line("_sequenceIndex = 0;");
				w.Line("_isVerifiable = false;");
				w.Line("_verifiableTimes = null;");
				// Clear async callback storage (mutual exclusivity)
				w.Line("_onCall = null;");
				w.Line("_onCallTracking = null;");
				// Set simplified void callback storage
				w.Line("_onCallSimplifiedVoid = callback;");
				w.Line("_onCallSimplifiedVoidTracking = new MethodCallBuilderImpl(this);");
				w.Line("return _onCallSimplifiedVoidTracking;");
			}
			w.Line();
		}

		// Full interceptor class name for nested class constructors
		var fullInterceptorClassName = model.InterceptorClassName + options.InterceptorTypeParameters;

		// When() entry points for parameter-specific matching
		if (canHaveWhenChain)
		{
			RenderWhenEntryPoints(w, fullInterceptorClassName, model.Parameters, model.ReturnType, delegateType, null);
		}
		if (canHaveVoidWhenChain)
		{
			RenderVoidWhenEntryPoints(w, fullInterceptorClassName, model.Parameters, delegateType, null);
		}

		// Invoke method
		RenderInvokeMethod(w, model, options, null);

		// Reset method - clears counts but preserves configuration and verifiable marking
		RenderResetMethod(w, model.Overloads, model.LastArgType, model.LastArgsType,
			hasSourceField: !string.IsNullOrEmpty(model.DeclaringInterface),
			hasValueOverload: canHaveValueOverload,
			hasSimplifiedCallback: isAsyncWithInnerType && !hasRefOrOut,
			hasSimplifiedVoidCallback: isVoidAsync && !hasRefOrOut,
			hasWhenChain: canHaveWhenChain || canHaveVoidWhenChain);

		// Internal verification support
		RenderInternalVerificationMembers(w, model.MethodName, model.Overloads, canHaveValueOverload,
			hasSimplifiedCallback: isAsyncWithInnerType && !hasRefOrOut,
			hasSimplifiedVoidCallback: isVoidAsync && !hasRefOrOut,
			hasWhenChain: canHaveWhenChain || canHaveVoidWhenChain);

		// Nested MethodCallBuilderImpl (renamed from MethodCallBuilderImpl)
		RenderMethodCallBuilderImpl(w, model.TrackableParameters, model.LastArgType, model.LastArgsType, model.BuilderInterface, fullInterceptorClassName, delegateType, null, model.ReturnType, model.IsVoid, hasRefOrOut, model.Parameters.Count);

		// Nested MethodSequenceImpl
		RenderMethodSequenceImpl(w, fullInterceptorClassName, delegateType, null, model.ReturnType, model.IsVoid, hasRefOrOut, model.Parameters.Count);

		// Nested When chain classes (for parameter-specific matching)
		if (canHaveWhenChain)
		{
			RenderWhenMatcherClasses(w, model.Parameters, model.ReturnType, delegateType, null);
			RenderWhenBuilderImpl(w, fullInterceptorClassName, model.Parameters, model.ReturnType, delegateType, null);
			RenderWhenChainImpl(w, fullInterceptorClassName, model.Parameters, model.ReturnType, delegateType, null);
		}
		if (canHaveVoidWhenChain)
		{
			RenderVoidWhenMatcherClasses(w, model.Parameters, delegateType, null);
			RenderVoidWhenChainImpl(w, fullInterceptorClassName, model.Parameters, delegateType, null);
		}
	}

	#endregion

	#region Overload Group Interceptor

	private static void RenderOverloadGroupContent(
		CodeWriter w,
		UnifiedMethodInterceptorModel model,
		InterceptorRenderOptions options)
	{
		var ownerWithParams = GetOwnerWithParams(model);

		// Source field for Source(T) feature - uses declaring interface type
		if (!string.IsNullOrEmpty(model.DeclaringInterface))
		{
			w.Line($"/// <summary>Source object to delegate to when no OnCall is configured.</summary>");
			w.Line($"internal {model.DeclaringInterface}? _source;");
			w.Line();
		}

		// Track unconfigured calls (shared across all overloads)
		w.Line("private int _unconfiguredCallCount;");
		w.Line();

		w.Line("/// <summary>Count of calls that were not handled by any configured behavior (used for class stub base fallback).</summary>");
		w.Line("internal int UnconfiguredCallCount => _unconfiguredCallCount;");
		w.Line();

		// Generate delegates and storage for each unique overload
		foreach (var overload in model.Overloads)
		{
			// Delegate
			w.Line($"/// <summary>Delegate for {model.MethodName}({GetParamTypeList(overload.Parameters)}).</summary>");
			w.Line(overload.DelegateSignature);
			w.Line();

			// OnCall storage
			w.Line($"private {overload.DelegateName}? _onCall_{overload.SignatureSuffix};");
			w.Line($"private MethodCallBuilderImpl_{overload.SignatureSuffix}? _onCallTracking_{overload.SignatureSuffix};");
			w.Line();

			// Simplified callback storage for Task<T>/ValueTask<T> overloads
			var hasRefOrOut = HasRefOrOutParameters(overload.Parameters);
			var (innerType, isTaskT, isValueTaskT) = GetAsyncTypeInfo(overload.ReturnType);
			var isAsyncWithInnerType = isTaskT || isValueTaskT;
			if (isAsyncWithInnerType && !hasRefOrOut)
			{
				var simplifiedDelegateType = BuildSimplifiedDelegateType(overload.Parameters, innerType);
				w.Line($"private {simplifiedDelegateType}? _onCallSimplified_{overload.SignatureSuffix};");
				w.Line($"private MethodCallBuilderImpl_{overload.SignatureSuffix}? _onCallSimplifiedTracking_{overload.SignatureSuffix};");
				w.Line();
			}

			// Simplified void callback storage for Task/ValueTask overloads
			var (isVoidTask, isVoidValueTask) = GetVoidAsyncInfo(overload.ReturnType);
			var isVoidAsync = isVoidTask || isVoidValueTask;
			if (isVoidAsync && !hasRefOrOut)
			{
				var voidDelegateType = BuildSimplifiedVoidDelegateType(overload.Parameters);
				w.Line($"private {voidDelegateType}? _onCallSimplifiedVoid_{overload.SignatureSuffix};");
				w.Line($"private MethodCallBuilderImpl_{overload.SignatureSuffix}? _onCallSimplifiedVoidTracking_{overload.SignatureSuffix};");
				w.Line();
			}

			// Sequence storage
			w.Line($"private global::System.Collections.Generic.List<({overload.DelegateName} Callback, MethodCallBuilderImpl_{overload.SignatureSuffix} Tracking)>? _sequence_{overload.SignatureSuffix};");
			w.Line($"private int _sequenceIndex_{overload.SignatureSuffix};");
			w.Line($"private bool _repeatLastValue_{overload.SignatureSuffix} = true;");
			w.Line();

			// When chain storage - parameter-specific matching (for overloads with parameters and no ref/out)
			var canHaveWhenChain = !overload.IsVoid && overload.Parameters.Count > 0 && !hasRefOrOut;
			var canHaveVoidWhenChain = overload.IsVoid && overload.Parameters.Count > 0 && !hasRefOrOut;
			if (canHaveWhenChain)
			{
				w.Line($"private global::System.Collections.Generic.List<WhenMatcher_{overload.SignatureSuffix}>? _whenChain_{overload.SignatureSuffix};");
				w.Line($"private int _whenChainHead_{overload.SignatureSuffix};");
				w.Line($"private bool _whenVerifiable_{overload.SignatureSuffix};");
				w.Line();
			}
			if (canHaveVoidWhenChain)
			{
				w.Line($"private global::System.Collections.Generic.List<VoidWhenMatcher_{overload.SignatureSuffix}>? _whenChain_{overload.SignatureSuffix};");
				w.Line($"private int _whenChainHead_{overload.SignatureSuffix};");
				w.Line($"private bool _whenVerifiable_{overload.SignatureSuffix};");
				w.Line();
			}

			// Verifiable state per overload
			w.Line($"private bool _isVerifiable_{overload.SignatureSuffix};");
			w.Line($"private global::KnockOff.Times? _verifiableTimes_{overload.SignatureSuffix};");
			w.Line();
		}

		// Backward compatibility: aggregate tracking properties across all overloads
		RenderOverloadBackwardCompatibleProperties(w, model.Overloads);
		w.Line();

		// Verify() methods for direct interceptor verification
		// Skip Verifiable() for overload groups - they have per-signature verifiable fields
		RenderInterceptorVerifyMethods(w, model.MethodName, isOverloadGroup: true);

		// OnCall overloads for each unique signature
		foreach (var overload in model.Overloads)
		{
			// Determine async characteristics for this overload
			var hasRefOrOut = HasRefOrOutParameters(overload.Parameters);
			var (innerType, isTaskT, isValueTaskT) = GetAsyncTypeInfo(overload.ReturnType);
			var isAsyncWithInnerType = isTaskT || isValueTaskT;
			var (isVoidTask, isVoidValueTask) = GetVoidAsyncInfo(overload.ReturnType);
			var isVoidAsync = isVoidTask || isVoidValueTask;

			// OnCall - repeating callback
			w.Line($"/// <summary>Configures callback for {model.MethodName}({GetParamTypeList(overload.Parameters)}). Returns builder for sequence chaining.</summary>");
			w.Line($"public MethodCallBuilderImpl_{overload.SignatureSuffix} OnCall({overload.DelegateName} callback)");
			using (w.Braces())
			{
				w.Line($"_sequence_{overload.SignatureSuffix} = null;");
				w.Line($"_sequenceIndex_{overload.SignatureSuffix} = 0;");
				w.Line($"_isVerifiable_{overload.SignatureSuffix} = false;");
				w.Line($"_verifiableTimes_{overload.SignatureSuffix} = null;");
				// Clear simplified callback storage (mutual exclusivity)
				if (isAsyncWithInnerType && !hasRefOrOut)
				{
					w.Line($"_onCallSimplified_{overload.SignatureSuffix} = null;");
					w.Line($"_onCallSimplifiedTracking_{overload.SignatureSuffix} = null;");
				}
				if (isVoidAsync && !hasRefOrOut)
				{
					w.Line($"_onCallSimplifiedVoid_{overload.SignatureSuffix} = null;");
					w.Line($"_onCallSimplifiedVoidTracking_{overload.SignatureSuffix} = null;");
				}
				w.Line($"_onCall_{overload.SignatureSuffix} = callback;");
				w.Line($"_onCallTracking_{overload.SignatureSuffix} = new MethodCallBuilderImpl_{overload.SignatureSuffix}(this);");
				w.Line($"return _onCallTracking_{overload.SignatureSuffix};");
			}
			w.Line();

			// OnCall(Func<..., TInnerType>) - simplified callback for Task<T>/ValueTask<T> overloads
			if (isAsyncWithInnerType && !hasRefOrOut)
			{
				var simplifiedDelegateType = BuildSimplifiedDelegateType(overload.Parameters, innerType);
				w.Line($"/// <summary>Configures callback returning unwrapped value for {model.MethodName}({GetParamTypeList(overload.Parameters)}). Result auto-wrapped in {(isTaskT ? "Task.FromResult" : "new ValueTask")}.</summary>");
				w.Line($"public MethodCallBuilderImpl_{overload.SignatureSuffix} OnCall({simplifiedDelegateType} callback)");
				using (w.Braces())
				{
					w.Line($"_sequence_{overload.SignatureSuffix} = null;");
					w.Line($"_sequenceIndex_{overload.SignatureSuffix} = 0;");
					w.Line($"_isVerifiable_{overload.SignatureSuffix} = false;");
					w.Line($"_verifiableTimes_{overload.SignatureSuffix} = null;");
					// Clear async callback storage (mutual exclusivity)
					w.Line($"_onCall_{overload.SignatureSuffix} = null;");
					w.Line($"_onCallTracking_{overload.SignatureSuffix} = null;");
					// Set simplified callback storage
					w.Line($"_onCallSimplified_{overload.SignatureSuffix} = callback;");
					w.Line($"_onCallSimplifiedTracking_{overload.SignatureSuffix} = new MethodCallBuilderImpl_{overload.SignatureSuffix}(this);");
					w.Line($"return _onCallSimplifiedTracking_{overload.SignatureSuffix};");
				}
				w.Line();
			}

			// OnCall(Action<...>) - simplified void callback for Task/ValueTask overloads
			if (isVoidAsync && !hasRefOrOut)
			{
				var voidDelegateType = BuildSimplifiedVoidDelegateType(overload.Parameters);
				w.Line($"/// <summary>Configures callback action for {model.MethodName}({GetParamTypeList(overload.Parameters)}). {(isVoidTask ? "Task.CompletedTask" : "default(ValueTask)")} auto-returned.</summary>");
				w.Line($"public MethodCallBuilderImpl_{overload.SignatureSuffix} OnCall({voidDelegateType} callback)");
				using (w.Braces())
				{
					w.Line($"_sequence_{overload.SignatureSuffix} = null;");
					w.Line($"_sequenceIndex_{overload.SignatureSuffix} = 0;");
					w.Line($"_isVerifiable_{overload.SignatureSuffix} = false;");
					w.Line($"_verifiableTimes_{overload.SignatureSuffix} = null;");
					// Clear async callback storage (mutual exclusivity)
					w.Line($"_onCall_{overload.SignatureSuffix} = null;");
					w.Line($"_onCallTracking_{overload.SignatureSuffix} = null;");
					// Set simplified void callback storage
					w.Line($"_onCallSimplifiedVoid_{overload.SignatureSuffix} = callback;");
					w.Line($"_onCallSimplifiedVoidTracking_{overload.SignatureSuffix} = new MethodCallBuilderImpl_{overload.SignatureSuffix}(this);");
					w.Line($"return _onCallSimplifiedVoidTracking_{overload.SignatureSuffix};");
				}
				w.Line();
			}

		}

		// Full interceptor class name for nested class constructors
		var fullInterceptorClassName = model.InterceptorClassName + options.InterceptorTypeParameters;

		// When() entry points for each unique signature (for parameter-specific matching)
		// Check if we need return-type disambiguation (multiple overloads with same params but different returns)
		var needsReturnTypeDisambiguation = HasReturnTypeOnlyOverloads(model.Overloads);
		foreach (var overload in model.Overloads)
		{
			var hasRefOrOut = HasRefOrOutParameters(overload.Parameters);
			var canHaveWhenChain = !overload.IsVoid && overload.Parameters.Count > 0 && !hasRefOrOut;
			var canHaveVoidWhenChain = overload.IsVoid && overload.Parameters.Count > 0 && !hasRefOrOut;
			// Use return type suffix to disambiguate When methods when needed
			var returnTypeSuffix = needsReturnTypeDisambiguation ? UnifiedInterceptorBuilder.GetTypeSuffix(overload.ReturnType) : null;
			if (canHaveWhenChain)
			{
				RenderWhenEntryPoints(w, fullInterceptorClassName, overload.Parameters, overload.ReturnType, overload.DelegateName, overload.SignatureSuffix, returnTypeSuffix);
			}
			if (canHaveVoidWhenChain)
			{
				RenderVoidWhenEntryPoints(w, fullInterceptorClassName, overload.Parameters, overload.DelegateName, overload.SignatureSuffix, returnTypeSuffix);
			}
		}

		// Invoke methods for each unique signature
		foreach (var overload in model.Overloads)
		{
			RenderOverloadInvokeMethod(w, model, overload, options);
		}

		// Reset method (resets all)
		RenderResetMethod(w, model.Overloads, hasSourceField: !string.IsNullOrEmpty(model.DeclaringInterface));

		// Internal verification support
		// Note: Value overloads for individual overload signatures are handled per-signature
		// For now, pass false for overload groups (value support to be added per-signature)
		RenderInternalVerificationMembers(w, model.MethodName, model.Overloads, hasValueOverload: false);

		// Nested builder classes for each unique signature (renamed from tracking)
		foreach (var overload in model.Overloads)
		{
			var hasRefOrOut = HasRefOrOutParameters(overload.Parameters);
			RenderMethodCallBuilderImpl(w, overload.TrackableParameters, overload.LastArgType, overload.LastArgsType, overload.BuilderInterface, fullInterceptorClassName, overload.DelegateName, overload.SignatureSuffix, overload.ReturnType, overload.IsVoid, hasRefOrOut, overload.Parameters.Count);
		}

		// Nested sequence classes for each unique signature
		foreach (var overload in model.Overloads)
		{
			var hasRefOrOut = HasRefOrOutParameters(overload.Parameters);
			RenderMethodSequenceImpl(w, fullInterceptorClassName, overload.DelegateName, overload.SignatureSuffix, overload.ReturnType, overload.IsVoid, hasRefOrOut, overload.Parameters.Count);
		}

		// Nested When chain classes for each unique signature (for parameter-specific matching)
		foreach (var overload in model.Overloads)
		{
			var hasRefOrOut = HasRefOrOutParameters(overload.Parameters);
			var canHaveWhenChain = !overload.IsVoid && overload.Parameters.Count > 0 && !hasRefOrOut;
			var canHaveVoidWhenChain = overload.IsVoid && overload.Parameters.Count > 0 && !hasRefOrOut;
			if (canHaveWhenChain)
			{
				RenderWhenMatcherClasses(w, overload.Parameters, overload.ReturnType, overload.DelegateName, overload.SignatureSuffix);
				RenderWhenBuilderImpl(w, fullInterceptorClassName, overload.Parameters, overload.ReturnType, overload.DelegateName, overload.SignatureSuffix);
				RenderWhenChainImpl(w, fullInterceptorClassName, overload.Parameters, overload.ReturnType, overload.DelegateName, overload.SignatureSuffix);
			}
			if (canHaveVoidWhenChain)
			{
				RenderVoidWhenMatcherClasses(w, overload.Parameters, overload.DelegateName, overload.SignatureSuffix);
				RenderVoidWhenChainImpl(w, fullInterceptorClassName, overload.Parameters, overload.DelegateName, overload.SignatureSuffix);
			}
		}
	}

	#endregion

	#region Invoke Methods

	private static void RenderInvokeMethod(
		CodeWriter w,
		UnifiedMethodInterceptorModel model,
		InterceptorRenderOptions options,
		string? signatureSuffix)
	{
		// Include stub parameter when user method fallback is needed and we have a stub type
		var needsStubParam = options.UserMethodFallback && !string.IsNullOrEmpty(options.StubTypeName) && !string.IsNullOrEmpty(model.UserMethodName);
		var invokeParams = BuildInvokeParams(model.Parameters, options.IncludeStrictParameter, needsStubParam ? options.StubTypeName : null);
		var returnType = model.IsVoid ? "void" : model.ReturnType;

		// Determine if value overload exists for this method
		var hasRefOrOut = HasRefOrOutParameters(model.Parameters);
		var canHaveValueOverload = !model.IsVoid && !hasRefOrOut;

		w.Line($"/// <summary>Invokes the configured callback. Called by explicit interface implementation.</summary>");
		w.Line($"internal {returnType} Invoke({invokeParams})");
		using (w.Braces())
		{
			// Initialize out parameters
			foreach (var p in model.Parameters.Where(p => p.RefKind == Microsoft.CodeAnalysis.RefKind.Out))
			{
				w.Line($"{p.EscapedName} = default!;");
			}

			var trackingArgs = UnifiedInterceptorBuilder.BuildTrackingArgs(model.TrackableParameters);

			// When chain - check HEAD matcher first (highest priority)
			// For non-void methods with parameters and no ref/out
			var canHaveWhenChain = !model.IsVoid && model.Parameters.Count > 0 && !hasRefOrOut;
			var canHaveVoidWhenChain = model.IsVoid && model.Parameters.Count > 0 && !hasRefOrOut;
			if (canHaveWhenChain)
			{
				RenderWhenChainInvokeCheck(w, model.Parameters, model.ReturnType, null);
			}
			if (canHaveVoidWhenChain)
			{
				RenderVoidWhenChainInvokeCheck(w, model.Parameters, null);
			}

			// Check sequence (takes priority if When chain didn't match)
			w.Line("if (_sequence != null && _sequenceIndex < _sequence.Count)");
			using (w.Braces())
			{
				w.Line("var (callback, tracking) = _sequence[_sequenceIndex];");
				w.Line($"tracking.RecordCall({trackingArgs});");
				w.Line("_sequenceIndex++;");
				var callbackArgs = BuildCallbackArgs(model.Parameters);
				if (model.IsVoid)
					w.Line($"callback({callbackArgs});");
				else
					w.Line($"return callback({callbackArgs});");
				if (model.IsVoid)
					w.Line("return;");
			}
			w.Line();

			// Check repeating Returns value (before callback - value is simpler, check it first)
			if (canHaveValueOverload)
			{
				var (valueType, isTaskT, isValueTaskT) = GetAsyncTypeInfo(model.ReturnType);
				w.Line("if (_hasReturnsValue && _returnsValueTracking != null)");
				using (w.Braces())
				{
					w.Line($"_returnsValueTracking.RecordCall({trackingArgs});");
					// Return value, wrapping in Task/ValueTask if needed
					if (isTaskT)
						w.Line($"return global::System.Threading.Tasks.Task.FromResult(_returnsValue);");
					else if (isValueTaskT)
						w.Line($"return new global::System.Threading.Tasks.ValueTask<{valueType}>(_returnsValue);");
					else
						w.Line("return _returnsValue;");
				}
				w.Line();
			}

			// Check repeating OnCall callback
			w.Line("if (_onCall != null && _onCallTracking != null)");
			using (w.Braces())
			{
				w.Line($"_onCallTracking.RecordCall({trackingArgs});");
				var callbackArgs = BuildCallbackArgs(model.Parameters);
				if (model.IsVoid)
					w.Line($"_onCall({callbackArgs});");
				else
					w.Line($"return _onCall({callbackArgs});");
				if (model.IsVoid)
					w.Line("return;");
			}
			w.Line();

			// Check simplified callback for Task<T>/ValueTask<T> methods
			var (invokeInnerType, invokeIsTaskT, invokeIsValueTaskT) = GetAsyncTypeInfo(model.ReturnType);
			var invokeIsAsyncWithInnerType = invokeIsTaskT || invokeIsValueTaskT;
			if (invokeIsAsyncWithInnerType && !hasRefOrOut)
			{
				w.Line("if (_onCallSimplified != null && _onCallSimplifiedTracking != null)");
				using (w.Braces())
				{
					w.Line($"_onCallSimplifiedTracking.RecordCall({trackingArgs});");
					var callbackArgs = BuildCallbackArgs(model.Parameters);
					if (invokeIsTaskT)
						w.Line($"return global::System.Threading.Tasks.Task.FromResult(_onCallSimplified({callbackArgs}));");
					else
						w.Line($"return new global::System.Threading.Tasks.ValueTask<{invokeInnerType}>(_onCallSimplified({callbackArgs}));");
				}
				w.Line();
			}

			// Check simplified void callback for Task/ValueTask methods
			var (invokeIsVoidTask, invokeIsVoidValueTask) = GetVoidAsyncInfo(model.ReturnType);
			var invokeIsVoidAsync = invokeIsVoidTask || invokeIsVoidValueTask;
			if (invokeIsVoidAsync && !hasRefOrOut)
			{
				w.Line("if (_onCallSimplifiedVoid != null && _onCallSimplifiedVoidTracking != null)");
				using (w.Braces())
				{
					w.Line($"_onCallSimplifiedVoidTracking.RecordCall({trackingArgs});");
					var callbackArgs = BuildCallbackArgs(model.Parameters);
					w.Line($"_onCallSimplifiedVoid({callbackArgs});");
					if (invokeIsVoidTask)
						w.Line("return global::System.Threading.Tasks.Task.CompletedTask;");
					else
						w.Line("return default;"); // default(ValueTask)
				}
				w.Line();
			}

			// No callback configured - track, check source, then strict/default
			w.Line("_unconfiguredCallCount++;");
			if (model.LastArgType != null && model.TrackableParameters.Count > 0)
			{
				var firstParam = model.TrackableParameters.First().EscapedName;
				w.Line($"_unconfiguredLastArg = {firstParam};");
			}
			if (model.LastArgsType != null)
			{
				w.Line($"_unconfiguredLastArgs = ({trackingArgs});");
			}

			// Sequence exhausted - check strict mode first (always throws), then repeat-last-value, then default
			w.Line("if (_sequence != null && _sequenceIndex >= _sequence.Count)");
			using (w.Braces())
			{
				// Strict mode ALWAYS throws on exhaustion (regardless of _repeatLastValue)
				w.Line($"if ({options.StrictAccessExpression}) throw global::KnockOff.StubException.SequenceExhausted(\"{model.MethodName}\");");
				// Repeat last value if enabled (default behavior in non-strict mode)
				w.Line("if (_repeatLastValue && _sequence.Count > 0)");
				using (w.Braces())
				{
					w.Line("var (callback, tracking) = _sequence[_sequence.Count - 1];");
					w.Line($"tracking.RecordCall({trackingArgs});");
					var repeatCallbackArgs = BuildCallbackArgs(model.Parameters);
					if (model.IsVoid)
					{
						w.Line($"callback({repeatCallbackArgs});");
						w.Line("return;");
					}
					else
					{
						w.Line($"return callback({repeatCallbackArgs});");
					}
				}
				// Return default (only reached when _repeatLastValue is false via ThenDefault())
				if (!model.IsVoid)
				{
					var defaultExpr = string.IsNullOrEmpty(model.DefaultExpression) ? "default!" : model.DefaultExpression;
					w.Line($"return {defaultExpr};");
				}
				else
				{
					w.Line("return;");
				}
			}
			w.Line();

			// Final fallback: User Method > Source > Strict > Default
			if (options.UserMethodFallback && !string.IsNullOrEmpty(model.UserMethodName))
			{
				// User method fallback - user method IS the configured behavior, bypasses Source/Strict
				var userMethodCallArgs = string.Join(", ", model.Parameters.Select(p => $"{p.RefPrefix}{p.EscapedName}"));
				// Call via stub parameter if available (flat stubs), otherwise direct call (inline stubs)
				var methodPrefix = !string.IsNullOrEmpty(options.StubTypeName) ? "stub." : "";
				if (model.IsVoid)
				{
					w.Line($"{methodPrefix}{model.UserMethodName}({userMethodCallArgs});");
					w.Line("return;");
				}
				else
				{
					w.Line($"return {methodPrefix}{model.UserMethodName}({userMethodCallArgs});");
				}
			}
			else
			{
				// Standard fallback: Source > Strict > Default
				if (!string.IsNullOrEmpty(model.DeclaringInterface))
				{
					w.Line("#pragma warning disable CS8601, SYSLIB0050");
					var sourceCallArgs = string.Join(", ", model.Parameters.Select(p => $"{p.RefPrefix}{p.EscapedName}"));
					if (model.IsVoid)
					{
						w.Line($"if (_source is {{ }} src) {{ src.{model.MethodName}({sourceCallArgs}); return; }}");
					}
					else
					{
						w.Line($"if (_source is {{ }} src) return src.{model.MethodName}({sourceCallArgs});");
					}
					w.Line("#pragma warning restore CS8601, SYSLIB0050");
				}

				w.Line($"if ({options.StrictAccessExpression}) throw global::KnockOff.StubException.NotConfigured(\"\", \"{model.MethodName}\");");
				if (model.IsVoid)
					w.Line("return;");
				else if (model.ThrowsOnDefault)
					w.Line($"throw new global::System.InvalidOperationException(\"No implementation provided for {model.MethodName}. Configure via OnCall.\");");
				else
				{
					var defaultExpr = string.IsNullOrEmpty(model.DefaultExpression) ? "default!" : model.DefaultExpression;
					w.Line($"return {defaultExpr};");
				}
			}
		}
		w.Line();
	}

	private static void RenderOverloadInvokeMethod(
		CodeWriter w,
		UnifiedMethodInterceptorModel model,
		MethodOverloadSignature overload,
		InterceptorRenderOptions options)
	{
		// Include stub parameter when user method fallback is needed and we have a stub type
		var needsStubParam = options.UserMethodFallback && !string.IsNullOrEmpty(options.StubTypeName) && !string.IsNullOrEmpty(overload.UserMethodName);
		var invokeParams = BuildInvokeParams(overload.Parameters, options.IncludeStrictParameter, needsStubParam ? options.StubTypeName : null);
		var returnType = overload.IsVoid ? "void" : overload.ReturnType;

		w.Line($"/// <summary>Invokes configured callback for {model.MethodName}({GetParamTypeList(overload.Parameters)}).</summary>");
		w.Line($"internal {returnType} Invoke_{overload.SignatureSuffix}({invokeParams})");
		using (w.Braces())
		{
			// Initialize out parameters
			foreach (var p in overload.Parameters.Where(p => p.RefKind == Microsoft.CodeAnalysis.RefKind.Out))
			{
				w.Line($"{p.EscapedName} = default!;");
			}

			var trackingArgs = UnifiedInterceptorBuilder.BuildTrackingArgs(overload.TrackableParameters);

			// When chain - check HEAD matcher first (highest priority)
			// For overloads with parameters and no ref/out
			var hasRefOrOutForWhen = HasRefOrOutParameters(overload.Parameters);
			var canHaveWhenChain = !overload.IsVoid && overload.Parameters.Count > 0 && !hasRefOrOutForWhen;
			var canHaveVoidWhenChain = overload.IsVoid && overload.Parameters.Count > 0 && !hasRefOrOutForWhen;
			if (canHaveWhenChain)
			{
				RenderWhenChainInvokeCheck(w, overload.Parameters, overload.ReturnType, overload.SignatureSuffix);
			}
			if (canHaveVoidWhenChain)
			{
				RenderVoidWhenChainInvokeCheck(w, overload.Parameters, overload.SignatureSuffix);
			}

			// Check sequence (takes priority if When chain didn't match)
			w.Line($"if (_sequence_{overload.SignatureSuffix} != null && _sequenceIndex_{overload.SignatureSuffix} < _sequence_{overload.SignatureSuffix}.Count)");
			using (w.Braces())
			{
				w.Line($"var (callback, tracking) = _sequence_{overload.SignatureSuffix}[_sequenceIndex_{overload.SignatureSuffix}];");
				w.Line($"tracking.RecordCall({trackingArgs});");
				w.Line($"_sequenceIndex_{overload.SignatureSuffix}++;");
				var callbackArgs = BuildCallbackArgs(overload.Parameters);
				if (overload.IsVoid)
					w.Line($"callback({callbackArgs});");
				else
					w.Line($"return callback({callbackArgs});");
				if (overload.IsVoid)
					w.Line("return;");
			}
			w.Line();

			// Check repeating OnCall callback
			w.Line($"if (_onCall_{overload.SignatureSuffix} != null && _onCallTracking_{overload.SignatureSuffix} != null)");
			using (w.Braces())
			{
				w.Line($"_onCallTracking_{overload.SignatureSuffix}.RecordCall({trackingArgs});");
				var callbackArgs = BuildCallbackArgs(overload.Parameters);
				if (overload.IsVoid)
					w.Line($"_onCall_{overload.SignatureSuffix}({callbackArgs});");
				else
					w.Line($"return _onCall_{overload.SignatureSuffix}({callbackArgs});");
				if (overload.IsVoid)
					w.Line("return;");
			}
			w.Line();

			// Check simplified callback for Task<T>/ValueTask<T> overloads
			var hasRefOrOut = HasRefOrOutParameters(overload.Parameters);
			var (innerType, isTaskT, isValueTaskT) = GetAsyncTypeInfo(overload.ReturnType);
			var isAsyncWithInnerType = isTaskT || isValueTaskT;
			if (isAsyncWithInnerType && !hasRefOrOut)
			{
				w.Line($"if (_onCallSimplified_{overload.SignatureSuffix} != null && _onCallSimplifiedTracking_{overload.SignatureSuffix} != null)");
				using (w.Braces())
				{
					w.Line($"_onCallSimplifiedTracking_{overload.SignatureSuffix}.RecordCall({trackingArgs});");
					var callbackArgs = BuildCallbackArgs(overload.Parameters);
					if (isTaskT)
						w.Line($"return global::System.Threading.Tasks.Task.FromResult(_onCallSimplified_{overload.SignatureSuffix}({callbackArgs}));");
					else
						w.Line($"return new global::System.Threading.Tasks.ValueTask<{innerType}>(_onCallSimplified_{overload.SignatureSuffix}({callbackArgs}));");
				}
				w.Line();
			}

			// Check simplified void callback for Task/ValueTask overloads
			var (isVoidTask, isVoidValueTask) = GetVoidAsyncInfo(overload.ReturnType);
			var isVoidAsync = isVoidTask || isVoidValueTask;
			if (isVoidAsync && !hasRefOrOut)
			{
				w.Line($"if (_onCallSimplifiedVoid_{overload.SignatureSuffix} != null && _onCallSimplifiedVoidTracking_{overload.SignatureSuffix} != null)");
				using (w.Braces())
				{
					w.Line($"_onCallSimplifiedVoidTracking_{overload.SignatureSuffix}.RecordCall({trackingArgs});");
					var callbackArgs = BuildCallbackArgs(overload.Parameters);
					w.Line($"_onCallSimplifiedVoid_{overload.SignatureSuffix}({callbackArgs});");
					if (isVoidTask)
						w.Line("return global::System.Threading.Tasks.Task.CompletedTask;");
					else
						w.Line("return default;"); // default(ValueTask)
				}
				w.Line();
			}

			// No callback configured
			w.Line("_unconfiguredCallCount++;");

			// Sequence exhausted - check strict mode first (always throws), then repeat-last-value, then default
			w.Line($"if (_sequence_{overload.SignatureSuffix} != null && _sequenceIndex_{overload.SignatureSuffix} >= _sequence_{overload.SignatureSuffix}.Count)");
			using (w.Braces())
			{
				// Strict mode ALWAYS throws on exhaustion (regardless of _repeatLastValue)
				w.Line($"if ({options.StrictAccessExpression}) throw global::KnockOff.StubException.SequenceExhausted(\"{model.MethodName}\");");
				// Repeat last value if enabled (default behavior in non-strict mode)
				w.Line($"if (_repeatLastValue_{overload.SignatureSuffix} && _sequence_{overload.SignatureSuffix}.Count > 0)");
				using (w.Braces())
				{
					w.Line($"var (callback, tracking) = _sequence_{overload.SignatureSuffix}[_sequence_{overload.SignatureSuffix}.Count - 1];");
					w.Line($"tracking.RecordCall({trackingArgs});");
					var repeatCallbackArgs = BuildCallbackArgs(overload.Parameters);
					if (overload.IsVoid)
					{
						w.Line($"callback({repeatCallbackArgs});");
						w.Line("return;");
					}
					else
					{
						w.Line($"return callback({repeatCallbackArgs});");
					}
				}
				// Return default (only reached when _repeatLastValue is false via ThenDefault())
				if (!overload.IsVoid)
				{
					var defaultExpr = string.IsNullOrEmpty(overload.DefaultExpression) ? "default!" : overload.DefaultExpression;
					w.Line($"return {defaultExpr};");
				}
				else
				{
					w.Line("return;");
				}
			}
			w.Line();

			// Final fallback: User Method (per-signature) > Source > Strict > Default
			if (options.UserMethodFallback && !string.IsNullOrEmpty(overload.UserMethodName))
			{
				// User method fallback - user method IS the configured behavior, bypasses Source/Strict
				var userMethodCallArgs = string.Join(", ", overload.Parameters.Select(p => $"{p.RefPrefix}{p.EscapedName}"));
				// Call via stub parameter if available (flat stubs), otherwise direct call (inline stubs)
				var methodPrefix = !string.IsNullOrEmpty(options.StubTypeName) ? "stub." : "";
				if (overload.IsVoid)
				{
					w.Line($"{methodPrefix}{overload.UserMethodName}({userMethodCallArgs});");
					w.Line("return;");
				}
				else
				{
					w.Line($"return {methodPrefix}{overload.UserMethodName}({userMethodCallArgs});");
				}
			}
			else
			{
				// Standard fallback: Source > Strict > Default
				if (!string.IsNullOrEmpty(model.DeclaringInterface))
				{
					w.Line("#pragma warning disable CS8601, SYSLIB0050");
					var sourceCallArgs = string.Join(", ", overload.Parameters.Select(p => $"{p.RefPrefix}{p.EscapedName}"));
					if (overload.IsVoid)
					{
						w.Line($"if (_source is {{ }} src) {{ src.{model.MethodName}({sourceCallArgs}); return; }}");
					}
					else
					{
						w.Line($"if (_source is {{ }} src) return src.{model.MethodName}({sourceCallArgs});");
					}
					w.Line("#pragma warning restore CS8601, SYSLIB0050");
				}

				w.Line($"if ({options.StrictAccessExpression}) throw global::KnockOff.StubException.NotConfigured(\"\", \"{model.MethodName}\");");
				if (overload.IsVoid)
					w.Line("return;");
				else if (overload.ThrowsOnDefault)
					w.Line($"throw new global::System.InvalidOperationException(\"No implementation provided for {model.MethodName}. Configure via OnCall.\");");
				else
				{
					var defaultExpr = string.IsNullOrEmpty(overload.DefaultExpression) ? "default!" : overload.DefaultExpression;
					w.Line($"return {defaultExpr};");
				}
			}
		}
		w.Line();
	}

	/// <summary>
	/// Renders the When chain invoke check logic.
	/// This should be called at the TOP of the Invoke method, before sequence check.
	/// </summary>
	/// <param name="w">Code writer.</param>
	/// <param name="parameters">Method parameters.</param>
	/// <param name="returnType">Method return type.</param>
	/// <param name="signatureSuffix">Suffix for overload groups, null for single-signature.</param>
	private static void RenderWhenChainInvokeCheck(
		CodeWriter w,
		EquatableArray<ParameterModel> parameters,
		string returnType,
		string? signatureSuffix)
	{
		var suffix = signatureSuffix == null ? "" : $"_{signatureSuffix}";
		var whenChainField = signatureSuffix == null ? "_whenChain" : $"_whenChain_{signatureSuffix}";
		var whenChainHeadField = signatureSuffix == null ? "_whenChainHead" : $"_whenChainHead_{signatureSuffix}";
		var callbackArgs = BuildCallbackArgs(parameters);

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

	#endregion

	#region Reset and Internal Verification Methods

	private static void RenderResetMethod(CodeWriter w, EquatableArray<MethodOverloadSignature> overloads, string? lastArgType = null, string? lastArgsType = null, bool hasSourceField = false, bool hasValueOverload = false, bool hasSimplifiedCallback = false, bool hasSimplifiedVoidCallback = false, bool hasWhenChain = false)
	{
		w.Line("/// <summary>Resets tracking state but preserves configuration and verifiable marking.</summary>");
		using (w.Block("public void Reset()"))
		{
			w.Line("_unconfiguredCallCount = 0;");
			if (lastArgType != null)
				w.Line("_unconfiguredLastArg = default;");
			if (lastArgsType != null)
				w.Line("_unconfiguredLastArgs = default;");
			if (hasSourceField)
				w.Line("_source = null;");
			if (overloads.Count == 0)
			{
				// Single-signature
				w.Line("_onCallTracking?.Reset();");
				// Reset value tracking only if value overload exists
				if (hasValueOverload)
					w.Line("_returnsValueTracking?.Reset();");
				// Reset simplified callback tracking
				if (hasSimplifiedCallback)
					w.Line("_onCallSimplifiedTracking?.Reset();");
				if (hasSimplifiedVoidCallback)
					w.Line("_onCallSimplifiedVoidTracking?.Reset();");
				w.Line("if (_sequence != null)");
				using (w.Braces())
				{
					w.Line("foreach (var (_, tracking) in _sequence)");
					w.Line("\ttracking.Reset();");
				}
				w.Line("_sequenceIndex = 0;");
				// Reset When chain (only if When chain is supported)
				if (hasWhenChain)
				{
					w.Line("_whenChainHead = 0;");
					w.Line("if (_whenChain != null)");
					using (w.Braces())
					{
						w.Line("foreach (var matcher in _whenChain)");
						w.Line("\tmatcher.CallCount = 0;");
					}
				}
			}
			else
			{
				// Multi-overload
				foreach (var overload in overloads)
				{
					w.Line($"_onCallTracking_{overload.SignatureSuffix}?.Reset();");
					// Reset simplified callback tracking for async overloads
					var hasRefOrOut = HasRefOrOutParameters(overload.Parameters);
					var (_, isTaskT, isValueTaskT) = GetAsyncTypeInfo(overload.ReturnType);
					var isAsyncWithInnerType = isTaskT || isValueTaskT;
					if (isAsyncWithInnerType && !hasRefOrOut)
					{
						w.Line($"_onCallSimplifiedTracking_{overload.SignatureSuffix}?.Reset();");
					}
					var (isVoidTask, isVoidValueTask) = GetVoidAsyncInfo(overload.ReturnType);
					var isVoidAsync = isVoidTask || isVoidValueTask;
					if (isVoidAsync && !hasRefOrOut)
					{
						w.Line($"_onCallSimplifiedVoidTracking_{overload.SignatureSuffix}?.Reset();");
					}
					w.Line($"if (_sequence_{overload.SignatureSuffix} != null)");
					using (w.Braces())
					{
						w.Line($"foreach (var (_, tracking) in _sequence_{overload.SignatureSuffix})");
						w.Line("\ttracking.Reset();");
					}
					w.Line($"_sequenceIndex_{overload.SignatureSuffix} = 0;");
					// Reset When chain for this overload (only if When chain is supported)
					var canHaveWhenChain = !overload.IsVoid && overload.Parameters.Count > 0 && !hasRefOrOut;
					var canHaveVoidWhenChain = overload.IsVoid && overload.Parameters.Count > 0 && !hasRefOrOut;
					if (canHaveWhenChain || canHaveVoidWhenChain)
					{
						w.Line($"_whenChainHead_{overload.SignatureSuffix} = 0;");
						w.Line($"if (_whenChain_{overload.SignatureSuffix} != null)");
						using (w.Braces())
						{
							w.Line($"foreach (var matcher in _whenChain_{overload.SignatureSuffix})");
							w.Line("\tmatcher.CallCount = 0;");
						}
					}
				}
			}
		}
		w.Line();
	}

	private static void RenderInternalVerificationMembers(CodeWriter w, string methodName, EquatableArray<MethodOverloadSignature> overloads, bool hasValueOverload, bool hasSimplifiedCallback = false, bool hasSimplifiedVoidCallback = false, bool hasWhenChain = false)
	{
		if (overloads.Count == 0)
		{
			// Single-signature
			w.Line("/// <summary>Whether this interceptor was marked with Verifiable().</summary>");
			w.Line("internal bool IsVerifiable => _isVerifiable;");
			w.Line();

			// IsConfigured includes value storage if value overload is supported, plus simplified callbacks and When chain
			var isConfiguredExpr = hasValueOverload
				? "_hasReturnsValue || _onCall != null || (_sequence?.Count ?? 0) > 0"
				: "_onCall != null || (_sequence?.Count ?? 0) > 0";
			if (hasSimplifiedCallback)
				isConfiguredExpr += " || _onCallSimplified != null";
			if (hasSimplifiedVoidCallback)
				isConfiguredExpr += " || _onCallSimplifiedVoid != null";
			if (hasWhenChain)
				isConfiguredExpr += " || (_whenChain?.Count ?? 0) > 0";
			w.Line("/// <summary>Whether this interceptor has been configured (OnCall, Returns(value), or When).</summary>");
			w.Line($"internal bool IsConfigured => {isConfiguredExpr};");
			w.Line();

			w.Line("/// <summary>Checks verification for Stub.Verify() - only checks if marked verifiable.</summary>");
			w.Line($"internal global::KnockOff.VerificationFailure? CheckVerification()");
			using (w.Braces())
			{
				// Early return if nothing is verifiable (include When chain check only if When chain is supported)
				var verifiableCheck = hasWhenChain ? "if (!_isVerifiable && !_whenVerifiable) return null;" : "if (!_isVerifiable) return null;";
				w.Line(verifiableCheck);
				// Check regular verifiable first
				w.Line("if (_isVerifiable)");
				using (w.Braces())
				{
					w.Line("var times = _verifiableTimes ?? global::KnockOff.Times.AtLeastOnce;");
					w.Line($"if (!times.Validate(TotalCallCount)) return new global::KnockOff.VerificationFailure(\"{methodName}\", times, TotalCallCount);");
				}
				// Check When chain verifiable (only if When chain is supported)
				if (hasWhenChain)
				{
					w.Line("if (_whenVerifiable && _whenChain != null && _whenChain.Count > 0)");
					using (w.Braces())
					{
						w.Line("var head = _whenChainHead;");
						w.Line("var count = _whenChain.Count;");
						w.Line("// Chain must be fully consumed (HEAD at end or at terminal matcher)");
						w.Line("if (head < count && !_whenChain[head].IsTerminal)");
						w.Line($"\treturn global::KnockOff.VerificationFailure.SequenceIncomplete(\"{methodName} When chain\", count, head);");
					}
				}
				w.Line("return null;");
			}
			w.Line();

			w.Line("/// <summary>Checks verification for Stub.VerifyAll() - checks if configured.</summary>");
			w.Line($"internal global::KnockOff.VerificationFailure? CheckVerificationAll()");
			using (w.Braces())
			{
				w.Line("if (!IsConfigured) return null;");
				// Check regular configuration
				w.Line("if (!global::KnockOff.Times.AtLeastOnce.Validate(TotalCallCount))");
				w.Line($"\treturn new global::KnockOff.VerificationFailure(\"{methodName}\", global::KnockOff.Times.AtLeastOnce, TotalCallCount);");
				// Check When chain if configured
				if (hasWhenChain)
				{
					w.Line("if (_whenChain != null && _whenChain.Count > 0)");
					using (w.Braces())
					{
						w.Line("var head = _whenChainHead;");
						w.Line("var count = _whenChain.Count;");
						w.Line("// Chain must be fully consumed (HEAD at end or at terminal matcher)");
						w.Line("if (head < count && !_whenChain[head].IsTerminal)");
						w.Line($"\treturn global::KnockOff.VerificationFailure.SequenceIncomplete(\"{methodName} When chain\", count, head);");
					}
				}
				w.Line("return null;");
			}
			w.Line();
		}
		else
		{
			// Multi-overload - combine across all overloads
			w.Line("/// <summary>Whether any overload was marked with Verifiable().</summary>");
			var isVerifiableExpr = string.Join(" || ", overloads.Select(o => $"_isVerifiable_{o.SignatureSuffix}"));
			w.Line($"internal bool IsVerifiable => {isVerifiableExpr};");
			w.Line();

			// Build IsConfigured including simplified callbacks and When chains for each overload
			w.Line("/// <summary>Whether any overload has been configured.</summary>");
			var isConfiguredParts = new List<string>();
			foreach (var overload in overloads)
			{
				var parts = new List<string>
				{
					$"_onCall_{overload.SignatureSuffix} != null",
					$"(_sequence_{overload.SignatureSuffix}?.Count ?? 0) > 0"
				};
				// Add simplified callback checks for async overloads
				var hasRefOrOut = HasRefOrOutParameters(overload.Parameters);
				var (_, isTaskT, isValueTaskT) = GetAsyncTypeInfo(overload.ReturnType);
				if ((isTaskT || isValueTaskT) && !hasRefOrOut)
					parts.Add($"_onCallSimplified_{overload.SignatureSuffix} != null");
				var (isVoidTask, isVoidValueTask) = GetVoidAsyncInfo(overload.ReturnType);
				if ((isVoidTask || isVoidValueTask) && !hasRefOrOut)
					parts.Add($"_onCallSimplifiedVoid_{overload.SignatureSuffix} != null");
				// Add When chain check for overloads with parameters and no ref/out
				var canHaveWhenChainForOverload = !overload.IsVoid && overload.Parameters.Count > 0 && !hasRefOrOut;
				var canHaveVoidWhenChainForOverload = overload.IsVoid && overload.Parameters.Count > 0 && !hasRefOrOut;
				if (canHaveWhenChainForOverload || canHaveVoidWhenChainForOverload)
					parts.Add($"(_whenChain_{overload.SignatureSuffix}?.Count ?? 0) > 0");
				isConfiguredParts.Add(string.Join(" || ", parts));
			}
			var isConfiguredExpr = string.Join(" || ", isConfiguredParts);
			w.Line($"internal bool IsConfigured => {isConfiguredExpr};");
			w.Line();

			w.Line("/// <summary>Checks verification for Stub.Verify() - checks all verifiable overloads.</summary>");
			w.Line($"internal global::KnockOff.VerificationFailure? CheckVerification()");
			using (w.Braces())
			{
				foreach (var overload in overloads)
				{
					w.Line($"if (_isVerifiable_{overload.SignatureSuffix})");
					using (w.Braces())
					{
						w.Line($"var times = _verifiableTimes_{overload.SignatureSuffix} ?? global::KnockOff.Times.AtLeastOnce;");
						// Build count including simplified tracking
						var countParts = new List<string>
						{
							$"(_onCallTracking_{overload.SignatureSuffix}?._callCount ?? 0)",
							$"(_sequence_{overload.SignatureSuffix}?.Sum(s => s.Tracking._callCount) ?? 0)"
						};
						var hasRefOrOut = HasRefOrOutParameters(overload.Parameters);
						var (_, isTaskT, isValueTaskT) = GetAsyncTypeInfo(overload.ReturnType);
						if ((isTaskT || isValueTaskT) && !hasRefOrOut)
							countParts.Add($"(_onCallSimplifiedTracking_{overload.SignatureSuffix}?._callCount ?? 0)");
						var (isVoidTask, isVoidValueTask) = GetVoidAsyncInfo(overload.ReturnType);
						if ((isVoidTask || isVoidValueTask) && !hasRefOrOut)
							countParts.Add($"(_onCallSimplifiedVoidTracking_{overload.SignatureSuffix}?._callCount ?? 0)");
						var countExpr = string.Join(" + ", countParts);
						w.Line($"var count = {countExpr};");
						w.Line($"if (!times.Validate(count)) return new global::KnockOff.VerificationFailure(\"{methodName}\", times, count);");
					}
					// Check When chain verification for this overload
					var hasRefOrOutForWhen = HasRefOrOutParameters(overload.Parameters);
					var canHaveWhenChainForOverload = !overload.IsVoid && overload.Parameters.Count > 0 && !hasRefOrOutForWhen;
					var canHaveVoidWhenChainForOverload = overload.IsVoid && overload.Parameters.Count > 0 && !hasRefOrOutForWhen;
					if (canHaveWhenChainForOverload || canHaveVoidWhenChainForOverload)
					{
						w.Line($"if (_whenVerifiable_{overload.SignatureSuffix} && _whenChain_{overload.SignatureSuffix} != null && _whenChain_{overload.SignatureSuffix}.Count > 0)");
						using (w.Braces())
						{
							w.Line($"var head = _whenChainHead_{overload.SignatureSuffix};");
							w.Line($"var chainCount = _whenChain_{overload.SignatureSuffix}.Count;");
							w.Line("// Chain must be fully consumed (HEAD at end or at terminal matcher)");
							w.Line($"if (head < chainCount && !_whenChain_{overload.SignatureSuffix}[head].IsTerminal)");
							w.Line($"\treturn global::KnockOff.VerificationFailure.SequenceIncomplete(\"{methodName} When chain\", chainCount, head);");
						}
					}
				}
				w.Line("return null;");
			}
			w.Line();

			w.Line("/// <summary>Checks verification for Stub.VerifyAll() - checks all configured overloads.</summary>");
			w.Line($"internal global::KnockOff.VerificationFailure? CheckVerificationAll()");
			using (w.Braces())
			{
				foreach (var overload in overloads)
				{
					// Build condition including simplified callbacks
					var condParts = new List<string>
					{
						$"_onCall_{overload.SignatureSuffix} != null",
						$"(_sequence_{overload.SignatureSuffix}?.Count ?? 0) > 0"
					};
					var hasRefOrOut = HasRefOrOutParameters(overload.Parameters);
					var (_, isTaskT, isValueTaskT) = GetAsyncTypeInfo(overload.ReturnType);
					if ((isTaskT || isValueTaskT) && !hasRefOrOut)
						condParts.Add($"_onCallSimplified_{overload.SignatureSuffix} != null");
					var (isVoidTask, isVoidValueTask) = GetVoidAsyncInfo(overload.ReturnType);
					if ((isVoidTask || isVoidValueTask) && !hasRefOrOut)
						condParts.Add($"_onCallSimplifiedVoid_{overload.SignatureSuffix} != null");
					var condExpr = string.Join(" || ", condParts);
					w.Line($"if ({condExpr})");
					using (w.Braces())
					{
						// Build count including simplified tracking
						var countParts = new List<string>
						{
							$"(_onCallTracking_{overload.SignatureSuffix}?._callCount ?? 0)",
							$"(_sequence_{overload.SignatureSuffix}?.Sum(s => s.Tracking._callCount) ?? 0)"
						};
						if ((isTaskT || isValueTaskT) && !hasRefOrOut)
							countParts.Add($"(_onCallSimplifiedTracking_{overload.SignatureSuffix}?._callCount ?? 0)");
						if ((isVoidTask || isVoidValueTask) && !hasRefOrOut)
							countParts.Add($"(_onCallSimplifiedVoidTracking_{overload.SignatureSuffix}?._callCount ?? 0)");
						var countExpr = string.Join(" + ", countParts);
						w.Line($"var count = {countExpr};");
						w.Line($"if (!global::KnockOff.Times.AtLeastOnce.Validate(count)) return new global::KnockOff.VerificationFailure(\"{methodName}\", global::KnockOff.Times.AtLeastOnce, count);");
					}
					// Check When chain for this overload if configured
					var hasRefOrOutForWhen = HasRefOrOutParameters(overload.Parameters);
					var canHaveWhenChainForOverload = !overload.IsVoid && overload.Parameters.Count > 0 && !hasRefOrOutForWhen;
					var canHaveVoidWhenChainForOverload = overload.IsVoid && overload.Parameters.Count > 0 && !hasRefOrOutForWhen;
					if (canHaveWhenChainForOverload || canHaveVoidWhenChainForOverload)
					{
						w.Line($"if (_whenChain_{overload.SignatureSuffix} != null && _whenChain_{overload.SignatureSuffix}.Count > 0)");
						using (w.Braces())
						{
							w.Line($"var head = _whenChainHead_{overload.SignatureSuffix};");
							w.Line($"var chainCount = _whenChain_{overload.SignatureSuffix}.Count;");
							w.Line("// Chain must be fully consumed (HEAD at end or at terminal matcher)");
							w.Line($"if (head < chainCount && !_whenChain_{overload.SignatureSuffix}[head].IsTerminal)");
							w.Line($"\treturn global::KnockOff.VerificationFailure.SequenceIncomplete(\"{methodName} When chain\", chainCount, head);");
						}
					}
				}
				w.Line("return null;");
			}
			w.Line();
		}
	}

	#endregion

	#region Nested Builder Class

	private static void RenderMethodCallBuilderImpl(
		CodeWriter w,
		EquatableArray<ParameterModel> trackableParams,
		string? lastArgType,
		string? lastArgsType,
		string builderInterface,
		string interceptorClassName,
		string delegateType,
		string? signatureSuffix,
		string returnType,
		bool isVoid,
		bool hasRefOrOut,
		int parameterCount)
	{
		var className = signatureSuffix == null ? "MethodCallBuilderImpl" : $"MethodCallBuilderImpl_{signatureSuffix}";
		var sequenceClassName = signatureSuffix == null ? "MethodSequenceImpl" : $"MethodSequenceImpl_{signatureSuffix}";
		var verifiableFieldName = signatureSuffix == null ? "_isVerifiable" : $"_isVerifiable_{signatureSuffix}";
		var verifiableTimesFieldName = signatureSuffix == null ? "_verifiableTimes" : $"_verifiableTimes_{signatureSuffix}";
		var sequenceFieldName = signatureSuffix == null ? "_sequence" : $"_sequence_{signatureSuffix}";
		var sequenceIndexFieldName = signatureSuffix == null ? "_sequenceIndex" : $"_sequenceIndex_{signatureSuffix}";
		var onCallFieldName = signatureSuffix == null ? "_onCall" : $"_onCall_{signatureSuffix}";
		var onCallTrackingFieldName = signatureSuffix == null ? "_onCallTracking" : $"_onCallTracking_{signatureSuffix}";

		w.Line($"/// <summary>Builder for callback registration. Supports tracking and lazy elevation to sequence.</summary>");
		w.Line($"public sealed class {className} : {builderInterface}");
		using (w.Braces())
		{
			// Reference to parent interceptor for setting verifiable and accessing sequence storage
			w.Line($"private readonly {interceptorClassName} _interceptor;");
			w.Line();

			// Constructor
			w.Line($"public {className}({interceptorClassName} interceptor) => _interceptor = interceptor;");
			w.Line();

			// LastArg/LastArgs storage
			if (trackableParams.Count == 1)
			{
				var param = trackableParams.GetArray()![0];
				w.Line($"private {param.Type} _lastArg = default!;");
			}
			else if (trackableParams.Count > 1)
			{
				w.Line($"private {lastArgsType} _lastArgs;");
			}
			w.Line();

			// CallCount field (private - parent interceptor can access since this is a nested class)
			w.Line("internal int _callCount;");
			w.Line();

			// LastArg/LastArgs property
			if (trackableParams.Count == 1)
			{
				var param = trackableParams.GetArray()![0];
				w.Line($"/// <summary>Last argument passed to this callback. Default if never called.</summary>");
				w.Line($"public {param.Type} LastArg => _lastArg;");
				w.Line();
			}
			else if (trackableParams.Count > 1)
			{
				w.Line($"/// <summary>Last arguments passed to this callback. Default if never called.</summary>");
				w.Line($"public {lastArgsType} LastArgs => _lastArgs;");
				w.Line();
			}

			// RecordCall method
			w.Line("/// <summary>Records a call to this callback.</summary>");
			if (trackableParams.Count == 0)
			{
				w.Line("public void RecordCall() => _callCount++;");
			}
			else if (trackableParams.Count == 1)
			{
				var param = trackableParams.GetArray()![0];
				w.Line($"public void RecordCall({param.Type} {param.EscapedName}) {{ _callCount++; _lastArg = {param.EscapedName}; }}");
			}
			else
			{
				w.Line($"public void RecordCall({lastArgsType} args) {{ _callCount++; _lastArgs = args; }}");
			}
			w.Line();

			// Reset method
			w.Line("/// <summary>Resets tracking state.</summary>");
			if (trackableParams.Count == 0)
				w.Line("public void Reset() => _callCount = 0;");
			else if (trackableParams.Count == 1)
				w.Line("public void Reset() { _callCount = 0; _lastArg = default!; }");
			else
				w.Line("public void Reset() { _callCount = 0; _lastArgs = default; }");
			w.Line();

			// Verify() - no params, defaults to AtLeastOnce
			w.Line("/// <summary>Verifies callback was invoked at least once. Throws VerificationException if not.</summary>");
			w.Line("public void Verify() => Verify(global::KnockOff.Times.AtLeastOnce);");
			w.Line();

			// Verify(Times) - throws on failure
			w.Line("/// <summary>Verifies call count satisfies the Times constraint. Throws VerificationException if not.</summary>");
			w.Line("public void Verify(global::KnockOff.Times times)");
			using (w.Braces())
			{
				w.Line("if (!times.Validate(_callCount))");
				w.Line("\tthrow new global::KnockOff.VerificationException(new global::KnockOff.VerificationFailure(\"method\", times, _callCount));");
			}
			w.Line();

			// ThenCall() - lazy elevation from repeating to sequence mode
			w.Line("/// <summary>Elevates to sequence mode and adds another callback. Returns sequence for further chaining.</summary>");
			w.Line($"public {sequenceClassName} ThenCall({delegateType} callback)");
			using (w.Braces())
			{
				// Lazy elevation: if not already in sequence mode, move this callback into sequence as first element
				w.Line($"if (_interceptor.{sequenceFieldName} == null)");
				using (w.Braces())
				{
					w.Line($"_interceptor.{sequenceFieldName} = new global::System.Collections.Generic.List<({delegateType} Callback, {className} Tracking)>();");
					// Move current OnCall into sequence as first element (this builder tracks it)
					w.Line($"_interceptor.{sequenceFieldName}.Add((_interceptor.{onCallFieldName}!, this));");
					w.Line($"_interceptor.{onCallFieldName} = null;");
					w.Line($"_interceptor.{onCallTrackingFieldName} = null;");
					w.Line($"_interceptor.{sequenceIndexFieldName} = 0;");
				}
				// Add new callback with fresh builder for its tracking
				w.Line($"var nextBuilder = new {className}(_interceptor);");
				w.Line($"_interceptor.{sequenceFieldName}.Add((callback, nextBuilder));");
				w.Line($"return new {sequenceClassName}(_interceptor);");
			}
			w.Line();

			// ThenReturns(value) - value wrapper that elevates to sequence, only for non-void methods without ref/out
			if (!isVoid && !hasRefOrOut)
			{
				var (valueType, isTaskT, isValueTaskT) = GetAsyncTypeInfo(returnType);
				var discardPrefix = BuildDiscardLambdaPrefix(parameterCount);
				w.Line($"/// <summary>Elevates to sequence mode and adds a value. Returns sequence for further chaining.</summary>");
				if (isTaskT)
				{
					w.Line($"public {sequenceClassName} ThenReturns({valueType} value) => ThenCall({discardPrefix} => global::System.Threading.Tasks.Task.FromResult(value));");
				}
				else if (isValueTaskT)
				{
					w.Line($"public {sequenceClassName} ThenReturns({valueType} value) => ThenCall({discardPrefix} => new global::System.Threading.Tasks.ValueTask<{valueType}>(value));");
				}
				else
				{
					w.Line($"public {sequenceClassName} ThenReturns({valueType} value) => ThenCall({discardPrefix} => value);");
				}
				w.Line();

				// ThenReturns(params values) - adds multiple values to sequence
				w.Line($"/// <summary>Adds multiple values to the sequence. Each value returned once.</summary>");
				w.Line($"public {sequenceClassName} ThenReturns(params {valueType}[] values)");
				using (w.Braces())
				{
					w.Line("if (values.Length == 0)");
					using (w.Braces())
					{
						// Elevate to sequence mode without adding any new values (same as ThenCall elevation)
						w.Line($"if (_interceptor.{sequenceFieldName} == null)");
						using (w.Braces())
						{
							w.Line($"_interceptor.{sequenceFieldName} = new global::System.Collections.Generic.List<({delegateType} Callback, {className} Tracking)>();");
							w.Line($"_interceptor.{sequenceFieldName}.Add((_interceptor.{onCallFieldName}!, this));");
							w.Line($"_interceptor.{onCallFieldName} = null;");
							w.Line($"_interceptor.{onCallTrackingFieldName} = null;");
							w.Line($"_interceptor.{sequenceIndexFieldName} = 0;");
						}
						w.Line($"return new {sequenceClassName}(_interceptor);");
					}
					w.Line("var seq = ThenReturns(values[0]);");
					w.Line("for (int i = 1; i < values.Length; i++)");
					using (w.Braces())
					{
						w.Line("seq = seq.ThenReturns(values[i]);");
					}
					w.Line("return seq;");
				}
				w.Line();
			}

			// Verifiable() - returns builder interface for fluent chaining
			w.Line("/// <summary>Marks for verification by Stub.Verify(). Returns this for fluent chaining.</summary>");
			w.Line($"public {builderInterface} Verifiable()");
			using (w.Braces())
			{
				w.Line($"_interceptor.{verifiableFieldName} = true;");
				w.Line($"_interceptor.{verifiableTimesFieldName} = null;");
				w.Line("return this;");
			}
			w.Line();

			w.Line("/// <summary>Marks for verification by Stub.Verify() with Times constraint. Returns this for fluent chaining.</summary>");
			w.Line($"public {builderInterface} Verifiable(global::KnockOff.Times times)");
			using (w.Braces())
			{
				w.Line($"_interceptor.{verifiableFieldName} = true;");
				w.Line($"_interceptor.{verifiableTimesFieldName} = times;");
				w.Line("return this;");
			}
			w.Line();

			// Explicit interface implementations for base tracking interfaces
			// IMethodTracking.Verifiable() -> builder
			w.Line("global::KnockOff.IMethodTracking global::KnockOff.IMethodTracking.Verifiable() => Verifiable();");
			w.Line("global::KnockOff.IMethodTracking global::KnockOff.IMethodTracking.Verifiable(global::KnockOff.Times times) => Verifiable(times);");

			// If implementing IMethodTracking<TArg>, also need explicit implementations for it
			if (trackableParams.Count == 1)
			{
				var param = trackableParams.GetArray()![0];
				w.Line($"global::KnockOff.IMethodTracking<{param.Type}> global::KnockOff.IMethodTracking<{param.Type}>.Verifiable() => Verifiable();");
				w.Line($"global::KnockOff.IMethodTracking<{param.Type}> global::KnockOff.IMethodTracking<{param.Type}>.Verifiable(global::KnockOff.Times times) => Verifiable(times);");
			}
			else if (trackableParams.Count > 1)
			{
				w.Line($"global::KnockOff.IMethodTrackingArgs<{lastArgsType}> global::KnockOff.IMethodTrackingArgs<{lastArgsType}>.Verifiable() => Verifiable();");
				w.Line($"global::KnockOff.IMethodTrackingArgs<{lastArgsType}> global::KnockOff.IMethodTrackingArgs<{lastArgsType}>.Verifiable(global::KnockOff.Times times) => Verifiable(times);");
			}

			// Explicit interface implementation for ThenCall - interface requires IMethodSequence<T> return
			w.Line($"global::KnockOff.IMethodSequence<{delegateType}> {builderInterface}.ThenCall({delegateType} callback) => ThenCall(callback);");
		}
		w.Line();
	}

	#endregion

	#region Nested Sequence Class

	private static void RenderMethodSequenceImpl(
		CodeWriter w,
		string interceptorClassName,
		string delegateType,
		string? signatureSuffix,
		string returnType,
		bool isVoid,
		bool hasRefOrOut,
		int parameterCount)
	{
		var className = signatureSuffix == null ? "MethodSequenceImpl" : $"MethodSequenceImpl_{signatureSuffix}";
		var trackingClassName = signatureSuffix == null ? "MethodCallBuilderImpl" : $"MethodCallBuilderImpl_{signatureSuffix}";
		var sequenceField = signatureSuffix == null ? "_sequence" : $"_sequence_{signatureSuffix}";
		var sequenceIndexField = signatureSuffix == null ? "_sequenceIndex" : $"_sequenceIndex_{signatureSuffix}";
		var repeatLastValueField = signatureSuffix == null ? "_repeatLastValue" : $"_repeatLastValue_{signatureSuffix}";
		var verifiableField = signatureSuffix == null ? "_isVerifiable" : $"_isVerifiable_{signatureSuffix}";
		var verifiableTimesField = signatureSuffix == null ? "_verifiableTimes" : $"_verifiableTimes_{signatureSuffix}";

		w.Line($"/// <summary>Sequence implementation for ThenCall chaining.</summary>");
		w.Line($"public sealed class {className} : global::KnockOff.IMethodSequence<{delegateType}>");
		using (w.Braces())
		{
			w.Line($"private readonly {interceptorClassName} _interceptor;");
			w.Line();

			w.Line($"public {className}({interceptorClassName} interceptor) => _interceptor = interceptor;");
			w.Line();

			// TotalCallCount (private - use Verify() to check sequence completion)
			w.Line("private int TotalCallCount");
			using (w.Braces())
			{
				w.Line("get");
				using (w.Braces())
				{
					w.Line($"if (_interceptor.{sequenceField} == null) return 0;");
					w.Line("var total = 0;");
					w.Line($"foreach (var (_, tracking) in _interceptor.{sequenceField})");
					w.Line("\ttotal += tracking._callCount;");
					w.Line("return total;");
				}
			}
			w.Line();

			// ThenCall - no Times parameter, each callback runs once
			w.Line($"/// <summary>Adds another callback to the sequence. Each callback runs exactly once.</summary>");
			w.Line($"public {className} ThenCall({delegateType} callback)");
			using (w.Braces())
			{
				w.Line($"var tracking = new {trackingClassName}(_interceptor);");
				w.Line($"_interceptor.{sequenceField}!.Add((callback, tracking));");
				w.Line("return this;");
			}
			w.Line();

			// ThenReturns(value) - value wrapper for ThenCall, only for non-void methods without ref/out
			if (!isVoid && !hasRefOrOut)
			{
				var (valueType, isTaskT, isValueTaskT) = GetAsyncTypeInfo(returnType);
				var discardPrefix = BuildDiscardLambdaPrefix(parameterCount);
				w.Line($"/// <summary>Adds a value to the sequence. The value is returned exactly once.</summary>");
				if (isTaskT)
				{
					w.Line($"public {className} ThenReturns({valueType} value) => ThenCall({discardPrefix} => global::System.Threading.Tasks.Task.FromResult(value));");
				}
				else if (isValueTaskT)
				{
					w.Line($"public {className} ThenReturns({valueType} value) => ThenCall({discardPrefix} => new global::System.Threading.Tasks.ValueTask<{valueType}>(value));");
				}
				else
				{
					w.Line($"public {className} ThenReturns({valueType} value) => ThenCall({discardPrefix} => value);");
				}
				w.Line();

				// ThenReturns(params values) - adds multiple values to sequence
				w.Line($"/// <summary>Adds multiple values to the sequence. Each value returned once.</summary>");
				w.Line($"public {className} ThenReturns(params {valueType}[] values)");
				using (w.Braces())
				{
					w.Line("foreach (var value in values)");
					using (w.Braces())
					{
						w.Line("ThenReturns(value);");
					}
					w.Line("return this;");
				}
				w.Line();
			}

			// Verify() - throws if sequence incomplete
			w.Line("/// <summary>Verifies the entire sequence was executed (all callbacks invoked). Throws VerificationException if incomplete.</summary>");
			w.Line("public void Verify()");
			using (w.Braces())
			{
				w.Line($"if (_interceptor.{sequenceField} == null) return;");
				w.Line($"var sequenceLength = _interceptor.{sequenceField}.Count;");
				w.Line($"var completedCount = _interceptor.{sequenceIndexField};");
				w.Line("if (completedCount < sequenceLength)");
				w.Line("\tthrow new global::KnockOff.VerificationException(global::KnockOff.VerificationFailure.SequenceIncomplete(\"method\", sequenceLength, completedCount));");
			}
			w.Line();

			// Reset
			w.Line("/// <summary>Resets all tracking in the sequence.</summary>");
			w.Line("public void Reset() => _interceptor.Reset();");
			w.Line();

			// Verifiable() - marks for Stub.Verify()
			w.Line("/// <summary>Marks this sequence for verification by Stub.Verify(). Returns this for fluent chaining.</summary>");
			w.Line($"public {className} Verifiable()");
			using (w.Braces())
			{
				w.Line($"_interceptor.{verifiableField} = true;");
				w.Line($"_interceptor.{verifiableTimesField} = null;");
				w.Line("return this;");
			}
			w.Line();

			// ThenDefault() - terminates sequence with default(T) after exhaustion
			w.Line("/// <summary>Terminates sequence with default(T) after exhaustion instead of repeating last value.</summary>");
			w.Line("public void ThenDefault()");
			using (w.Braces())
			{
				w.Line($"_interceptor.{repeatLastValueField} = false;");
			}
			w.Line();

			// Explicit interface implementations for IMethodSequence<T>
			w.Line($"global::KnockOff.IMethodSequence<{delegateType}> global::KnockOff.IMethodSequence<{delegateType}>.ThenCall({delegateType} callback) => ThenCall(callback);");
			w.Line($"global::KnockOff.IMethodSequence<{delegateType}> global::KnockOff.IMethodSequence<{delegateType}>.Verifiable() => Verifiable();");

			// Non-generic IMethodSequence.Verifiable()
			w.Line("/// <summary>Marks this sequence for verification by Stub.Verify(). Returns this for fluent chaining.</summary>");
			w.Line("global::KnockOff.IMethodSequence global::KnockOff.IMethodSequence.Verifiable() => Verifiable();");
		}
		w.Line();
	}

	#endregion

	#region When Chain Classes

	/// <summary>
	/// Renders the WhenMatcher abstract base class and its implementations.
	/// These classes are parameterized by the method's parameters and return type.
	/// </summary>
	/// <param name="w">The code writer.</param>
	/// <param name="parameters">Method parameters for Matches/Execute signatures.</param>
	/// <param name="returnType">Return type for Execute method.</param>
	/// <param name="delegateType">Delegate type for callbacks.</param>
	/// <param name="signatureSuffix">Suffix for overload groups, null for single-signature.</param>
	private static void RenderWhenMatcherClasses(
		CodeWriter w,
		EquatableArray<ParameterModel> parameters,
		string returnType,
		string delegateType,
		string? signatureSuffix)
	{
		var suffix = signatureSuffix == null ? "" : $"_{signatureSuffix}";
		var matchParams = BuildMatchParams(parameters);
		var executeParams = BuildMatchParams(parameters);
		var callbackArgs = BuildCallbackArgs(parameters);
		var predicateType = BuildPredicateType(parameters);

		// WhenMatcher abstract base
		w.Line($"/// <summary>Abstract base for When chain matchers.</summary>");
		w.Line($"private abstract class WhenMatcher{suffix}");
		using (w.Braces())
		{
			w.Line($"public abstract bool Matches({matchParams});");
			w.Line($"public abstract {returnType} Execute({executeParams});");
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
			w.Line($"private readonly {returnType} _value;");
			w.Line();
			w.Line($"public WhenMatcherValue{suffix}({predicateType} predicate, {returnType} value)");
			using (w.Braces())
			{
				w.Line("_predicate = predicate;");
				w.Line("_value = value;");
			}
			w.Line();
			w.Line($"public override bool Matches({matchParams}) => _predicate({callbackArgs});");
			w.Line($"public override {returnType} Execute({executeParams}) => _value;");
			w.Line("public override bool IsTerminal => false;");
		}
		w.Line();

		// WhenMatcherCall - callback, always matches, terminal
		w.Line($"/// <summary>Matcher that always matches and invokes a callback. Terminal.</summary>");
		w.Line($"private sealed class WhenMatcherCall{suffix} : WhenMatcher{suffix}");
		using (w.Braces())
		{
			w.Line($"private readonly {delegateType} _callback;");
			w.Line();
			w.Line($"public WhenMatcherCall{suffix}({delegateType} callback) => _callback = callback;");
			w.Line();
			w.Line($"public override bool Matches({matchParams}) => true;");
			w.Line($"public override {returnType} Execute({executeParams}) => _callback({callbackArgs});");
			w.Line("public override bool IsTerminal => true;");
		}
		w.Line();

		// WhenMatcherNone - never matches, terminal
		w.Line($"/// <summary>Matcher that never matches. Used to close chain without fallback. Terminal.</summary>");
		w.Line($"private sealed class WhenMatcherNone{suffix} : WhenMatcher{suffix}");
		using (w.Braces())
		{
			w.Line($"public override bool Matches({matchParams}) => false;");
			w.Line($"public override {returnType} Execute({executeParams}) => default!;");
			w.Line("public override bool IsTerminal => true;");
		}
		w.Line();
	}

	/// <summary>
	/// Renders the WhenBuilderImpl nested class.
	/// Holds a pending predicate and exposes Returns(value) to complete the matcher.
	/// </summary>
	private static void RenderWhenBuilderImpl(
		CodeWriter w,
		string interceptorClassName,
		EquatableArray<ParameterModel> parameters,
		string returnType,
		string delegateType,
		string? signatureSuffix)
	{
		var suffix = signatureSuffix == null ? "" : $"_{signatureSuffix}";
		var predicateType = BuildPredicateType(parameters);
		var whenChainField = signatureSuffix == null ? "_whenChain" : $"_whenChain_{signatureSuffix}";

		// Check if this is an async method (Task<T> or ValueTask<T>)
		var (innerType, isTaskT, isValueTaskT) = GetAsyncTypeInfo(returnType);
		var isAsync = isTaskT || isValueTaskT;

		w.Line($"/// <summary>Builder for When matchers. Captures predicate, awaits Returns(value).</summary>");
		w.Line($"public sealed class WhenBuilder{suffix} : global::KnockOff.IWhenBuilder<{delegateType}, {returnType}>");
		using (w.Braces())
		{
			w.Line($"private readonly {interceptorClassName} _interceptor;");
			w.Line($"private readonly {predicateType} _predicate;");
			w.Line();

			w.Line($"public WhenBuilder{suffix}({interceptorClassName} interceptor, {predicateType} predicate)");
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
				w.Line($"public WhenChain{suffix} Returns({innerType} value)");
				using (w.Braces())
				{
					w.Line($"_interceptor.{whenChainField} ??= new global::System.Collections.Generic.List<WhenMatcher{suffix}>();");
					// Wrap with Task.FromResult or new ValueTask<T>
					if (isTaskT)
						w.Line($"_interceptor.{whenChainField}.Add(new WhenMatcherValue{suffix}(_predicate, global::System.Threading.Tasks.Task.FromResult(value)));");
					else
						w.Line($"_interceptor.{whenChainField}.Add(new WhenMatcherValue{suffix}(_predicate, new global::System.Threading.Tasks.ValueTask<{innerType}>(value)));");
					w.Line($"return new WhenChain{suffix}(_interceptor);");
				}
				w.Line();
				// Explicit interface implementation wraps too
				if (isTaskT)
					w.Line($"global::KnockOff.IWhenChain<{delegateType}, {returnType}> global::KnockOff.IWhenBuilder<{delegateType}, {returnType}>.Returns({returnType} value) => Returns(value.Result);");
				else
					w.Line($"global::KnockOff.IWhenChain<{delegateType}, {returnType}> global::KnockOff.IWhenBuilder<{delegateType}, {returnType}>.Returns({returnType} value) => Returns(value.Result);");
			}
			else
			{
				// Non-async: Returns accepts the full return type directly
				w.Line($"public WhenChain{suffix} Returns({returnType} value)");
				using (w.Braces())
				{
					w.Line($"_interceptor.{whenChainField} ??= new global::System.Collections.Generic.List<WhenMatcher{suffix}>();");
					w.Line($"_interceptor.{whenChainField}.Add(new WhenMatcherValue{suffix}(_predicate, value));");
					w.Line($"return new WhenChain{suffix}(_interceptor);");
				}
				w.Line();
				// Explicit interface implementation for IWhenBuilder.Returns
				w.Line($"global::KnockOff.IWhenChain<{delegateType}, {returnType}> global::KnockOff.IWhenBuilder<{delegateType}, {returnType}>.Returns({returnType} value) => Returns(value);");
			}
		}
		w.Line();
	}

	/// <summary>
	/// Renders the WhenChainImpl nested class.
	/// Implements IWhenChain with ThenCall, ThenNone, Verify, Reset, Verifiable.
	/// ThenWhen overloads are generated separately (they require parameter types).
	/// </summary>
	private static void RenderWhenChainImpl(
		CodeWriter w,
		string interceptorClassName,
		EquatableArray<ParameterModel> parameters,
		string returnType,
		string delegateType,
		string? signatureSuffix)
	{
		var suffix = signatureSuffix == null ? "" : $"_{signatureSuffix}";
		var whenChainField = signatureSuffix == null ? "_whenChain" : $"_whenChain_{signatureSuffix}";
		var whenChainHeadField = signatureSuffix == null ? "_whenChainHead" : $"_whenChainHead_{signatureSuffix}";
		var whenVerifiableField = signatureSuffix == null ? "_whenVerifiable" : $"_whenVerifiable_{signatureSuffix}";
		var predicateType = BuildPredicateType(parameters);
		var paramTypeList = BuildParamTypeList(parameters);

		w.Line($"/// <summary>When chain implementation with ThenCall, ThenNone, verification support.</summary>");
		w.Line($"public sealed class WhenChain{suffix} : global::KnockOff.IWhenChain<{delegateType}, {returnType}>");
		using (w.Braces())
		{
			w.Line($"private readonly {interceptorClassName} _interceptor;");
			w.Line();

			w.Line($"public WhenChain{suffix}({interceptorClassName} interceptor) => _interceptor = interceptor;");
			w.Line();

			// ThenWhen with values and predicate
			if (parameters.Count > 0)
			{
				w.Line($"/// <summary>Adds another matcher with exact value matching.</summary>");
				w.Line($"public WhenBuilder{suffix} ThenWhen({paramTypeList})");
				using (w.Braces())
				{
					// Build equality predicate - lambda params are prefixed with _ to avoid shadowing method params
					var lambdaParams = BuildLambdaParamsForEquality(parameters);
					var predicateBody = BuildEqualityPredicateBody(parameters);
					w.Line($"return new WhenBuilder{suffix}(_interceptor, ({lambdaParams}) => {predicateBody});");
				}
				w.Line();

				// ThenWhen with predicate
				w.Line($"/// <summary>Adds another matcher with predicate matching.</summary>");
				w.Line($"public WhenBuilder{suffix} ThenWhen({predicateType} predicate)");
				using (w.Braces())
				{
					w.Line($"return new WhenBuilder{suffix}(_interceptor, predicate);");
				}
				w.Line();
			}

			// ThenCall - terminal with callback
			w.Line($"/// <summary>Adds an unconditional callback as terminal matcher.</summary>");
			w.Line($"public global::KnockOff.IWhenTracking ThenCall({delegateType} callback)");
			using (w.Braces())
			{
				w.Line($"_interceptor.{whenChainField} ??= new global::System.Collections.Generic.List<WhenMatcher{suffix}>();");
				w.Line($"_interceptor.{whenChainField}.Add(new WhenMatcherCall{suffix}(callback));");
				w.Line("return this;");
			}
			w.Line();

			// ThenNone - terminal that never matches
			w.Line($"/// <summary>Closes chain with no matcher. Falls through when exhausted.</summary>");
			w.Line($"public global::KnockOff.IWhenTracking ThenNone()");
			using (w.Braces())
			{
				w.Line($"_interceptor.{whenChainField} ??= new global::System.Collections.Generic.List<WhenMatcher{suffix}>();");
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
			w.Line($"public WhenChain{suffix} Verifiable()");
			using (w.Braces())
			{
				w.Line($"_interceptor.{whenVerifiableField} = true;");
				w.Line("return this;");
			}
			w.Line();

			// Explicit interface implementations for IWhenChain.Verifiable and IWhenTracking.Verifiable
			w.Line($"global::KnockOff.IWhenChain<{delegateType}, {returnType}> global::KnockOff.IWhenChain<{delegateType}, {returnType}>.Verifiable() => Verifiable();");
			w.Line("global::KnockOff.IWhenTracking global::KnockOff.IWhenTracking.Verifiable() => Verifiable();");
		}
		w.Line();
	}

	/// <summary>
	/// Renders the When() entry point methods for parameter-specific matching.
	/// Generates both value overload (exact matching) and predicate overload (Func&lt;T1, T2, bool&gt;).
	/// </summary>
	/// <param name="w">The code writer.</param>
	/// <param name="interceptorClassName">The full interceptor class name including type parameters.</param>
	/// <param name="parameters">Method parameters for When() signature.</param>
	/// <param name="returnType">Return type for IWhenBuilder.</param>
	/// <param name="delegateType">Delegate type for IWhenBuilder.</param>
	/// <param name="signatureSuffix">Suffix for overload groups, null for single-signature.</param>
	/// <param name="methodNameSuffix">Suffix for method name to disambiguate return-type-only overloads.</param>
	private static void RenderWhenEntryPoints(
		CodeWriter w,
		string interceptorClassName,
		EquatableArray<ParameterModel> parameters,
		string returnType,
		string delegateType,
		string? signatureSuffix,
		string? methodNameSuffix = null)
	{
		// When() requires parameters - parameterless methods cannot use When()
		if (parameters.Count == 0) return;

		var suffix = signatureSuffix == null ? "" : $"_{signatureSuffix}";
		var whenChainField = signatureSuffix == null ? "_whenChain" : $"_whenChain_{signatureSuffix}";
		var predicateType = UnifiedInterceptorBuilder.BuildWhenPredicateType(parameters);
		var paramTypeList = BuildParamTypeList(parameters);
		var whenMethodName = methodNameSuffix == null ? "When" : $"When_{methodNameSuffix}";

		// When() value overload - exact value matching via Object.Equals
		// Returns concrete type to enable fluent ThenWhen chaining
		w.Line($"/// <summary>Configures parameter-specific matching with exact values. Returns builder for Returns().</summary>");
		w.Line($"public WhenBuilder{suffix} {whenMethodName}({paramTypeList})");
		using (w.Braces())
		{
			// Initialize When chain if null
			w.Line($"{whenChainField} ??= new global::System.Collections.Generic.List<WhenMatcher{suffix}>();");

			// Build equality predicate - use indexed lambda params to avoid keyword conflicts
			var lambdaParams = BuildLambdaParamsForEquality(parameters);
			var predicateBody = BuildEqualityPredicateBody(parameters);
			w.Line($"return new WhenBuilder{suffix}(this, ({lambdaParams}) => {predicateBody});");
		}
		w.Line();

		// When() predicate overload - Func<T1, T2, bool>
		// Returns concrete type to enable fluent ThenWhen chaining
		w.Line($"/// <summary>Configures parameter-specific matching with predicate. Returns builder for Returns().</summary>");
		w.Line($"public WhenBuilder{suffix} {whenMethodName}({predicateType} predicate)");
		using (w.Braces())
		{
			// Initialize When chain if null
			w.Line($"{whenChainField} ??= new global::System.Collections.Generic.List<WhenMatcher{suffix}>();");
			w.Line($"return new WhenBuilder{suffix}(this, predicate);");
		}
		w.Line();
	}

	/// <summary>
	/// Builds a parameter list for Matches/Execute methods (e.g., "int a, string b").
	/// </summary>
	private static string BuildMatchParams(EquatableArray<ParameterModel> parameters)
	{
		if (parameters.Count == 0) return "";
		return string.Join(", ", parameters.Select(p => $"{p.Type} {p.EscapedName}"));
	}

	/// <summary>
	/// Builds the predicate Func type for When matching (e.g., "Func&lt;int, string, bool&gt;").
	/// </summary>
	private static string BuildPredicateType(EquatableArray<ParameterModel> parameters)
	{
		if (parameters.Count == 0)
			return "global::System.Func<bool>";

		var paramTypes = string.Join(", ", parameters.Select(p => p.Type));
		return $"global::System.Func<{paramTypes}, bool>";
	}

	/// <summary>
	/// Builds a comma-separated list of parameter types (e.g., "int a, string b").
	/// </summary>
	private static string BuildParamTypeList(EquatableArray<ParameterModel> parameters)
	{
		if (parameters.Count == 0) return "";
		return string.Join(", ", parameters.Select(p => $"{p.Type} {p.EscapedName}"));
	}

	/// <summary>
	/// Builds lambda parameter names (e.g., "a, b" for use in "(a, b) => ...").
	/// </summary>
	private static string BuildLambdaParams(EquatableArray<ParameterModel> parameters)
	{
		if (parameters.Count == 0) return "";
		return string.Join(", ", parameters.Select(p => p.EscapedName));
	}

	/// <summary>
	/// Builds lambda parameter names for equality comparisons using indexed names to avoid keyword conflicts.
	/// E.g., "_arg0, _arg1" for use in "(_arg0, _arg1) => Equals(_arg0, a) &amp;&amp; Equals(_arg1, b)".
	/// </summary>
	private static string BuildLambdaParamsForEquality(EquatableArray<ParameterModel> parameters)
	{
		if (parameters.Count == 0) return "";
		// Use indexed names to avoid conflicts with C# keywords and the method parameters
		return string.Join(", ", Enumerable.Range(0, parameters.Count).Select(i => $"_arg{i}"));
	}

	/// <summary>
	/// Builds an equality predicate body comparing lambda params (indexed) to method params.
	/// E.g., "Equals(_arg0, a) &amp;&amp; Equals(_arg1, b)".
	/// Uses Object.Equals for null-safety.
	/// </summary>
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

	#region Void When Chain Classes

	/// <summary>
	/// Renders the When() entry point methods for void methods.
	/// For void methods, When() returns IVoidWhenChain directly (no builder needed).
	/// </summary>
	/// <param name="methodNameSuffix">Suffix for method name to disambiguate return-type-only overloads.</param>
	private static void RenderVoidWhenEntryPoints(
		CodeWriter w,
		string interceptorClassName,
		EquatableArray<ParameterModel> parameters,
		string delegateType,
		string? signatureSuffix,
		string? methodNameSuffix = null)
	{
		// When() requires parameters - parameterless methods cannot use When()
		if (parameters.Count == 0) return;

		var suffix = signatureSuffix == null ? "" : $"_{signatureSuffix}";
		var whenChainField = signatureSuffix == null ? "_whenChain" : $"_whenChain_{signatureSuffix}";
		var predicateType = UnifiedInterceptorBuilder.BuildWhenPredicateType(parameters);
		var paramTypeList = BuildParamTypeList(parameters);
		var whenMethodName = methodNameSuffix == null ? "When" : $"When_{methodNameSuffix}";
		var chainType = $"VoidWhenChain{suffix}";

		// When() value overload - exact value matching via Object.Equals
		// Returns concrete type to enable fluent ThenWhen chaining
		w.Line($"/// <summary>Configures parameter-specific matching with exact values for void method. Returns chain directly.</summary>");
		w.Line($"public {chainType} {whenMethodName}({paramTypeList})");
		using (w.Braces())
		{
			// Initialize When chain if null
			w.Line($"{whenChainField} ??= new global::System.Collections.Generic.List<VoidWhenMatcher{suffix}>();");

			// Build equality predicate - use indexed lambda params to avoid keyword conflicts
			var lambdaParams = BuildLambdaParamsForEquality(parameters);
			var predicateBody = BuildEqualityPredicateBody(parameters);

			// Add matcher immediately (no builder needed for void)
			w.Line($"var matcher = new VoidWhenMatcherPredicate{suffix}(({lambdaParams}) => {predicateBody});");
			w.Line($"{whenChainField}.Add(matcher);");
			w.Line($"return new {chainType}(this, matcher);");
		}
		w.Line();

		// When() predicate overload - Func<T1, T2, bool>
		// Returns concrete type to enable fluent ThenWhen chaining
		w.Line($"/// <summary>Configures parameter-specific matching with predicate for void method. Returns chain directly.</summary>");
		w.Line($"public {chainType} {whenMethodName}({predicateType} predicate)");
		using (w.Braces())
		{
			// Initialize When chain if null
			w.Line($"{whenChainField} ??= new global::System.Collections.Generic.List<VoidWhenMatcher{suffix}>();");
			w.Line($"var matcher = new VoidWhenMatcherPredicate{suffix}(predicate);");
			w.Line($"{whenChainField}.Add(matcher);");
			w.Line($"return new {chainType}(this, matcher);");
		}
		w.Line();
	}

	/// <summary>
	/// Renders the void When chain invoke check logic.
	/// For void methods - executes callback if configured, no return value.
	/// </summary>
	private static void RenderVoidWhenChainInvokeCheck(
		CodeWriter w,
		EquatableArray<ParameterModel> parameters,
		string? signatureSuffix)
	{
		var suffix = signatureSuffix == null ? "" : $"_{signatureSuffix}";
		var whenChainField = signatureSuffix == null ? "_whenChain" : $"_whenChain_{signatureSuffix}";
		var whenChainHeadField = signatureSuffix == null ? "_whenChainHead" : $"_whenChainHead_{signatureSuffix}";
		var callbackArgs = BuildCallbackArgs(parameters);

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
	private static void RenderVoidWhenMatcherClasses(
		CodeWriter w,
		EquatableArray<ParameterModel> parameters,
		string delegateType,
		string? signatureSuffix)
	{
		var suffix = signatureSuffix == null ? "" : $"_{signatureSuffix}";
		var matchParams = BuildMatchParams(parameters);
		var callbackArgs = BuildCallbackArgs(parameters);
		var predicateType = BuildPredicateType(parameters);

		// VoidWhenMatcher abstract base
		w.Line($"/// <summary>Abstract base for void When chain matchers.</summary>");
		w.Line($"internal abstract class VoidWhenMatcher{suffix}");
		using (w.Braces())
		{
			w.Line($"public abstract bool Matches({matchParams});");
			w.Line($"public abstract void Execute({matchParams});");
			w.Line("public abstract bool IsTerminal { get; }");
			w.Line("public int CallCount { get; set; }");
			w.Line($"public {delegateType}? Callback {{ get; set; }}");
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
			w.Line($"private readonly {delegateType} _callback;");
			w.Line();
			w.Line($"public VoidWhenMatcherCall{suffix}({delegateType} callback) => _callback = callback;");
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
	private static void RenderVoidWhenChainImpl(
		CodeWriter w,
		string interceptorClassName,
		EquatableArray<ParameterModel> parameters,
		string delegateType,
		string? signatureSuffix)
	{
		var suffix = signatureSuffix == null ? "" : $"_{signatureSuffix}";
		var whenChainField = signatureSuffix == null ? "_whenChain" : $"_whenChain_{signatureSuffix}";
		var whenChainHeadField = signatureSuffix == null ? "_whenChainHead" : $"_whenChainHead_{signatureSuffix}";
		var whenVerifiableField = signatureSuffix == null ? "_whenVerifiable" : $"_whenVerifiable_{signatureSuffix}";
		var predicateType = BuildPredicateType(parameters);
		var paramTypeList = BuildParamTypeList(parameters);

		w.Line($"/// <summary>Void When chain implementation with Call, ThenWhen, ThenCall, ThenNone, verification support.</summary>");
		w.Line($"public sealed class VoidWhenChain{suffix} : global::KnockOff.IVoidWhenChain<{delegateType}>");
		using (w.Braces())
		{
			w.Line($"private readonly {interceptorClassName} _interceptor;");
			w.Line($"private readonly VoidWhenMatcher{suffix} _currentMatcher;");
			w.Line();

			w.Line($"internal VoidWhenChain{suffix}({interceptorClassName} interceptor, VoidWhenMatcher{suffix} currentMatcher)");
			using (w.Braces())
			{
				w.Line("_interceptor = interceptor;");
				w.Line("_currentMatcher = currentMatcher;");
			}
			w.Line();

			var chainType = $"VoidWhenChain{suffix}";

			// Call - sets optional callback on current matcher
			// Returns concrete type to enable fluent ThenWhen chaining
			w.Line($"/// <summary>Sets an optional callback to invoke when this matcher matches.</summary>");
			w.Line($"public {chainType} Call({delegateType} callback)");
			using (w.Braces())
			{
				w.Line("_currentMatcher.Callback = callback;");
				w.Line("return this;");
			}
			w.Line();
			// Explicit interface implementation for IVoidWhenChain.Call
			w.Line($"global::KnockOff.IVoidWhenChain<{delegateType}> global::KnockOff.IVoidWhenChain<{delegateType}>.Call({delegateType} callback) => Call(callback);");
			w.Line();

			// ThenWhen with values and predicate
			if (parameters.Count > 0)
			{
				w.Line($"/// <summary>Adds another matcher with exact value matching.</summary>");
				w.Line($"public {chainType} ThenWhen({paramTypeList})");
				using (w.Braces())
				{
					// Build equality predicate - lambda params are prefixed with _ to avoid shadowing method params
					var lambdaParams = BuildLambdaParamsForEquality(parameters);
					var predicateBody = BuildEqualityPredicateBody(parameters);
					w.Line($"_interceptor.{whenChainField} ??= new global::System.Collections.Generic.List<VoidWhenMatcher{suffix}>();");
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
					w.Line($"_interceptor.{whenChainField} ??= new global::System.Collections.Generic.List<VoidWhenMatcher{suffix}>();");
					w.Line($"var matcher = new VoidWhenMatcherPredicate{suffix}(predicate);");
					w.Line($"_interceptor.{whenChainField}.Add(matcher);");
					w.Line($"return new {chainType}(_interceptor, matcher);");
				}
				w.Line();
			}

			// ThenCall - terminal with callback
			w.Line($"/// <summary>Adds an unconditional callback as terminal matcher.</summary>");
			w.Line($"public global::KnockOff.IWhenTracking ThenCall({delegateType} callback)");
			using (w.Braces())
			{
				w.Line($"_interceptor.{whenChainField} ??= new global::System.Collections.Generic.List<VoidWhenMatcher{suffix}>();");
				w.Line($"_interceptor.{whenChainField}.Add(new VoidWhenMatcherCall{suffix}(callback));");
				w.Line("return this;");
			}
			w.Line();

			// ThenNone - terminal that never matches
			w.Line($"/// <summary>Closes chain with no matcher. Falls through when exhausted.</summary>");
			w.Line($"public global::KnockOff.IWhenTracking ThenNone()");
			using (w.Braces())
			{
				w.Line($"_interceptor.{whenChainField} ??= new global::System.Collections.Generic.List<VoidWhenMatcher{suffix}>();");
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
			w.Line($"global::KnockOff.IVoidWhenChain<{delegateType}> global::KnockOff.IVoidWhenChain<{delegateType}>.Verifiable() => Verifiable();");
			w.Line("global::KnockOff.IWhenTracking global::KnockOff.IWhenTracking.Verifiable() => Verifiable();");
		}
		w.Line();
	}

	#endregion

	#region Helpers

	private static string GetOwnerWithParams(UnifiedMethodInterceptorModel model)
	{
		return string.IsNullOrEmpty(model.OwnerTypeParameters)
			? model.OwnerClassName
			: $"{model.OwnerClassName}{model.OwnerTypeParameters}";
	}

	private static string GetParamTypeList(EquatableArray<ParameterModel> parameters)
	{
		return string.Join(", ", parameters.Select(p => p.Type));
	}

	private static string BuildInvokeParams(EquatableArray<ParameterModel> parameters, bool includeStrict, string? stubTypeName = null)
	{
		var parts = new List<string>();
		if (includeStrict)
			parts.Add("bool strict");
		if (!string.IsNullOrEmpty(stubTypeName))
			parts.Add($"{stubTypeName} stub");
		foreach (var p in parameters)
		{
			parts.Add($"{p.RefPrefix}{p.Type} {p.EscapedName}");
		}
		return string.Join(", ", parts);
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

	/// <summary>
	/// Analyzes a return type for async patterns and extracts the inner type.
	/// </summary>
	/// <param name="returnType">The fully-qualified return type (e.g., "global::System.Threading.Tasks.Task&lt;string&gt;").</param>
	/// <returns>Tuple of (valueStorageType, isTaskT, isValueTaskT) where valueStorageType is the unwrapped type for storage.</returns>
	private static (string ValueStorageType, bool IsTaskT, bool IsValueTaskT) GetAsyncTypeInfo(string returnType)
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

	/// <summary>
	/// Checks if the return type is a void async type (Task or ValueTask without generic argument).
	/// </summary>
	/// <param name="returnType">The fully-qualified return type.</param>
	/// <returns>Tuple of (isTask, isValueTask) indicating which void async type, if any.</returns>
	private static (bool IsTask, bool IsValueTask) GetVoidAsyncInfo(string returnType)
	{
		if (returnType == "global::System.Threading.Tasks.Task")
			return (true, false);
		if (returnType == "global::System.Threading.Tasks.ValueTask")
			return (false, true);
		return (false, false);
	}

	/// <summary>
	/// Builds the simplified callback delegate type for Task&lt;T&gt;/ValueTask&lt;T&gt; methods.
	/// E.g., Func&lt;int, User?&gt; for a method with int param returning Task&lt;User?&gt;
	/// </summary>
	private static string BuildSimplifiedDelegateType(EquatableArray<ParameterModel> parameters, string innerType)
	{
		if (parameters.Count == 0)
			return $"global::System.Func<{innerType}>";

		var paramTypes = string.Join(", ", parameters.Select(p => p.Type));
		return $"global::System.Func<{paramTypes}, {innerType}>";
	}

	/// <summary>
	/// Builds the simplified void callback delegate type for Task/ValueTask methods.
	/// E.g., Action&lt;User&gt; for a method with User param returning Task
	/// </summary>
	private static string BuildSimplifiedVoidDelegateType(EquatableArray<ParameterModel> parameters)
	{
		if (parameters.Count == 0)
			return "global::System.Action";

		var paramTypes = string.Join(", ", parameters.Select(p => p.Type));
		return $"global::System.Action<{paramTypes}>";
	}

	/// <summary>
	/// Makes a type nullable for storage, avoiding double-nullable types.
	/// </summary>
	/// <param name="type">The type to make nullable.</param>
	/// <returns>The nullable storage type.</returns>
	private static string MakeNullableForStorage(string type)
	{
		// Already nullable reference type or nullable value type
		if (type.EndsWith("?"))
			return type;

		return type + "?";
	}

	/// <summary>
	/// Checks if a method signature has ref or out parameters.
	/// </summary>
	private static bool HasRefOrOutParameters(EquatableArray<ParameterModel> parameters)
	{
		return parameters.Any(p => p.RefKind == Microsoft.CodeAnalysis.RefKind.Ref || p.RefKind == Microsoft.CodeAnalysis.RefKind.Out);
	}

	/// <summary>
	/// Builds the lambda prefix for a value-returning lambda that ignores method parameters.
	/// E.g., for 0 params: "()", for 1 param: "(_)", for 2 params: "(_, _)".
	/// </summary>
	private static string BuildDiscardLambdaPrefix(int parameterCount)
	{
		if (parameterCount == 0)
			return "()";
		var discards = string.Join(", ", Enumerable.Range(0, parameterCount).Select(_ => "_"));
		return $"({discards})";
	}

	/// <summary>
	/// Checks if overloads contain return-type-only differences (same params, different returns).
	/// When true, When() methods need return type suffix to avoid conflicts.
	/// </summary>
	private static bool HasReturnTypeOnlyOverloads(EquatableArray<MethodOverloadSignature> overloads)
	{
		// Group by parameter types only (excluding return type from the signature suffix)
		// If any group has more than one entry, we have return-type-only differences
		var paramGroups = new Dictionary<string, int>();
		foreach (var overload in overloads)
		{
			// Build a key from parameter types only
			var paramKey = overload.Parameters.Count == 0
				? "NoParams"
				: string.Join("_", overload.Parameters.Select(p => UnifiedInterceptorBuilder.GetTypeSuffix(p.Type)));

			if (paramGroups.TryGetValue(paramKey, out var count))
			{
				paramGroups[paramKey] = count + 1;
			}
			else
			{
				paramGroups[paramKey] = 1;
			}
		}

		return paramGroups.Values.Any(count => count > 1);
	}

	#endregion

	#region Backward Compatibility Properties

	/// <summary>
	/// Renders aggregate tracking properties for backward compatibility (single-signature).
	/// These provide LastArg/LastArgs for argument tracking.
	/// </summary>
	private static void RenderBackwardCompatibleTrackingProperties(
		CodeWriter w,
		EquatableArray<ParameterModel> trackableParams,
		string? lastArgType,
		string? lastArgsType,
		bool hasValueOverload,
		bool hasSimplifiedCallback = false,
		bool hasSimplifiedVoidCallback = false)
	{
		// CallCount - total across OnCall + Returns(value) + simplified + sequence + unconfigured (private - use Verify() API to check call counts)
		// Include value tracking when value overload exists, and simplified callback tracking when present
		var valueTrackingPart = hasValueOverload ? " + (_returnsValueTracking?._callCount ?? 0)" : "";
		var simplifiedTrackingPart = hasSimplifiedCallback ? " + (_onCallSimplifiedTracking?._callCount ?? 0)" : "";
		var simplifiedVoidTrackingPart = hasSimplifiedVoidCallback ? " + (_onCallSimplifiedVoidTracking?._callCount ?? 0)" : "";
		w.Line($"private int TotalCallCount {{ get {{ var sum = _unconfiguredCallCount + (_onCallTracking?._callCount ?? 0){valueTrackingPart}{simplifiedTrackingPart}{simplifiedVoidTrackingPart}; if (_sequence != null) foreach (var s in _sequence) sum += s.Tracking._callCount; return sum; }} }}");
		w.Line();

		// LastArg - for single param methods (aggregate across all call sources)
		// Note: value tracking also has LastArg when there are trackable parameters
		if (lastArgType != null)
		{
			var nullableType = lastArgType.EndsWith("?") ? lastArgType : $"{lastArgType}?";
			w.Line($"/// <summary>The argument from the last call (from most recently called registration).</summary>");

			// Build the priority chain: value > simplified > simplifiedVoid > onCall > sequence > unconfigured
			var getterParts = new List<string>();
			if (hasValueOverload)
				getterParts.Add("if ((_returnsValueTracking?._callCount ?? 0) > 0) return _returnsValueTracking!.LastArg;");
			if (hasSimplifiedCallback)
				getterParts.Add("if ((_onCallSimplifiedTracking?._callCount ?? 0) > 0) return _onCallSimplifiedTracking!.LastArg;");
			if (hasSimplifiedVoidCallback)
				getterParts.Add("if ((_onCallSimplifiedVoidTracking?._callCount ?? 0) > 0) return _onCallSimplifiedVoidTracking!.LastArg;");
			getterParts.Add("if ((_onCallTracking?._callCount ?? 0) > 0) return _onCallTracking!.LastArg;");
			getterParts.Add("if (_sequence != null) for (int i = _sequence.Count - 1; i >= 0; i--) if (_sequence[i].Tracking._callCount > 0) return _sequence[i].Tracking.LastArg;");
			getterParts.Add("return _unconfiguredCallCount > 0 ? _unconfiguredLastArg : default;");

			w.Line($"public {nullableType} LastArg {{ get {{ {string.Join(" ", getterParts)} }} }}");
			w.Line();
		}

		// LastArgs - for multi-param methods (aggregate across all call sources)
		if (lastArgsType != null)
		{
			var nullableType = lastArgsType.EndsWith("?") ? lastArgsType : $"{lastArgsType}?";
			w.Line($"/// <summary>The arguments from the last call (from most recently called registration).</summary>");

			// Build the priority chain: value > simplified > simplifiedVoid > onCall > sequence > unconfigured
			var getterParts = new List<string>();
			if (hasValueOverload)
				getterParts.Add("if ((_returnsValueTracking?._callCount ?? 0) > 0) return _returnsValueTracking!.LastArgs;");
			if (hasSimplifiedCallback)
				getterParts.Add("if ((_onCallSimplifiedTracking?._callCount ?? 0) > 0) return _onCallSimplifiedTracking!.LastArgs;");
			if (hasSimplifiedVoidCallback)
				getterParts.Add("if ((_onCallSimplifiedVoidTracking?._callCount ?? 0) > 0) return _onCallSimplifiedVoidTracking!.LastArgs;");
			getterParts.Add("if ((_onCallTracking?._callCount ?? 0) > 0) return _onCallTracking!.LastArgs;");
			getterParts.Add("if (_sequence != null) for (int i = _sequence.Count - 1; i >= 0; i--) if (_sequence[i].Tracking._callCount > 0) return _sequence[i].Tracking.LastArgs;");
			getterParts.Add("return _unconfiguredCallCount > 0 ? _unconfiguredLastArgs : default;");

			w.Line($"public {nullableType} LastArgs {{ get {{ {string.Join(" ", getterParts)} }} }}");
			w.Line();
		}
	}

	/// <summary>
	/// Renders aggregate tracking properties for overload groups.
	/// Aggregates across all overload sequences.
	/// </summary>
	private static void RenderOverloadBackwardCompatibleProperties(
		CodeWriter w,
		EquatableArray<MethodOverloadSignature> overloads)
	{
		// Build a sum expression across all storage types for each overload, plus unconfigured calls
		// Include simplified callback tracking for async overloads
		var sumParts = new List<string>();
		foreach (var overload in overloads)
		{
			sumParts.Add($"(_onCallTracking_{overload.SignatureSuffix}?._callCount ?? 0)");
			sumParts.Add($"(_sequence_{overload.SignatureSuffix}?.Sum(s => s.Tracking._callCount) ?? 0)");
			// Add simplified tracking for async overloads
			var hasRefOrOut = HasRefOrOutParameters(overload.Parameters);
			var (_, isTaskT, isValueTaskT) = GetAsyncTypeInfo(overload.ReturnType);
			if ((isTaskT || isValueTaskT) && !hasRefOrOut)
				sumParts.Add($"(_onCallSimplifiedTracking_{overload.SignatureSuffix}?._callCount ?? 0)");
			var (isVoidTask, isVoidValueTask) = GetVoidAsyncInfo(overload.ReturnType);
			if ((isVoidTask || isVoidValueTask) && !hasRefOrOut)
				sumParts.Add($"(_onCallSimplifiedVoidTracking_{overload.SignatureSuffix}?._callCount ?? 0)");
		}
		var sumExpr = "_unconfiguredCallCount + " + string.Join(" + ", sumParts);

		// Private - use Verify() API to check call counts
		w.Line($"private int TotalCallCount => {sumExpr};");
	}

	/// <summary>
	/// Renders Verify() and Verify(Times) methods on an interceptor class.
	/// Also renders Verifiable() methods for marking interceptors for Stub.Verify() (single-signature only).
	/// </summary>
	/// <param name="isOverloadGroup">True for overload groups which have per-signature verifiable fields.</param>
	private static void RenderInterceptorVerifyMethods(CodeWriter w, string methodName, bool isOverloadGroup = false)
	{
		w.Line("/// <summary>Verifies method was called at least once. Throws VerificationException if not.</summary>");
		w.Line("public void Verify() => Verify(global::KnockOff.Times.AtLeastOnce);");
		w.Line();

		w.Line("/// <summary>Verifies call count satisfies the Times constraint. Throws VerificationException if not.</summary>");
		w.Line("public void Verify(global::KnockOff.Times times)");
		using (w.Braces())
		{
			w.Line("if (!times.Validate(TotalCallCount))");
			w.Line($"\tthrow new global::KnockOff.VerificationException(new global::KnockOff.VerificationFailure(\"{methodName}\", times, TotalCallCount));");
		}
		w.Line();

		// Verifiable() methods - only for single-signature interceptors
		// Overload groups have per-signature verifiable marking on their builders
		if (!isOverloadGroup)
		{
			w.Line("/// <summary>Marks for verification by Stub.Verify().</summary>");
			w.Line("public void Verifiable()");
			using (w.Braces())
			{
				w.Line("_isVerifiable = true;");
				w.Line("_verifiableTimes = null;");
			}
			w.Line();

			w.Line("/// <summary>Marks for verification by Stub.Verify() with Times constraint.</summary>");
			w.Line("public void Verifiable(global::KnockOff.Times times)");
			using (w.Braces())
			{
				w.Line("_isVerifiable = true;");
				w.Line("_verifiableTimes = times;");
			}
			w.Line();
		}
	}

	#endregion
}
