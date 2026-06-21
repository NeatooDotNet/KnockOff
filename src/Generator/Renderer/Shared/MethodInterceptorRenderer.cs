// src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs
#nullable enable
using System.Collections.Generic;
using System.Linq;
using KnockOff.Builder;
using KnockOff.Model.Shared;

namespace KnockOff.Renderer.Shared;

/// <summary>
/// Renders method interceptor classes for both inline and flat stubs.
/// Generates Return()/Call() entry points for non-void/void methods (repeating callback, elevatable to sequence via ThenReturn/ThenCall),
/// nested MethodCallBuilderImpl and MethodSequenceImpl classes, Invoke methods, and verification.
/// </summary>
internal static class MethodInterceptorRenderer
{
	/// <summary>
	/// Renders a complete method interceptor class.
	/// For single-signature methods, generates a simple interceptor.
	/// For overload groups, generates per-signature delegates, sequences, and Return/Call overloads.
	/// </summary>
	public static void RenderInterceptorClass(
		CodeWriter w,
		UnifiedMethodInterceptorModel model,
		InterceptorRenderOptions options)
	{
		var typeParams = options.InterceptorTypeParameters;
		var constraints = options.InterceptorConstraints;

		// All single-signature methods now inherit from MethodInterceptorRuntime (non-generic base).
		// Overload groups remain self-contained (Phase 4 will migrate them).
		string classDecl;
		if (model.Overloads.Count > 0)
		{
			classDecl = $"public sealed class {model.InterceptorClassName}{typeParams}{constraints}";
		}
		else
		{
			classDecl = $"public sealed class {model.InterceptorClassName}{typeParams} : global::KnockOff.Interceptors.MethodInterceptorRuntime{constraints}";
		}

		if (model.Overloads.Count == 0)
		{
			var classSig = FormatMethodSignatureForDoc(model.MethodName, model.Parameters, model.ReturnType, model.IsVoid);
			var classSummary = model.XmlDocSummary != null
				? $"Tracks and configures behavior for {classSig}. {model.XmlDocSummary}"
				: $"Tracks and configures behavior for {classSig}.";
			w.Line($"/// <summary>{classSummary}</summary>");
		}
		else
		{
			w.Line($"/// <summary>Tracks and configures behavior for {model.MethodName} (overloaded).</summary>");
		}
		using (w.Block(classDecl))
		{
			if (model.Overloads.Count > 0)
			{
				RenderOverloadGroupContent(w, model, options);
			}
			else
			{
				RenderBaseClassContent(w, model, options);
			}
		}
		w.Line();
	}

	/// <summary>
	/// Computes the TArgs type parameter for the base class.
	/// 0 params -> Unit, 1 param -> the param type, 2+ params -> named ValueTuple.
	/// </summary>
	private static string ComputeTArgsType(EquatableArray<ParameterModel> parameters)
	{
		if (parameters.Count == 0)
			return "global::KnockOff.Unit";
		if (parameters.Count == 1)
			return parameters.GetArray()![0].Type;
		// Named ValueTuple: (T1 name1, T2 name2, ...)
		return "(" + string.Join(", ", parameters.Select(p => $"{p.Type} {p.EscapedName}")) + ")";
	}


	#region Base Class Mode Interceptor

	/// <summary>
	/// Renders a fully generated interceptor class that inherits from MethodInterceptorRuntime.
	/// Handles all method types: sync, async (Task/ValueTask), ref/out, ref return.
	/// Generated class provides typed Call/Return/When API methods and abstract overrides.
	/// </summary>
	private static void RenderBaseClassContent(
		CodeWriter w,
		UnifiedMethodInterceptorModel model,
		InterceptorRenderOptions options)
	{
		// The delegate type from the model is always the full-signature custom delegate for non-void,
		// or Action for void methods. We keep using this as the storage/API delegate type.
		// The Func<>/Action<> migration to the user-facing API is a later phase.
		var delegateType = model.CallDelegateType.TrimEnd('?');
		var tArgs = ComputeTArgsType(model.Parameters);
		var fullInterceptorClassName = model.InterceptorClassName + options.InterceptorTypeParameters;
		var hasRefOrOut = model.Parameters.Any(p => p.RefKind == Microsoft.CodeAnalysis.RefKind.Ref || p.RefKind == Microsoft.CodeAnalysis.RefKind.Out);

		// Async type info
		var (innerType, isAsyncTaskT, isAsyncValueTaskT) = GetAsyncTypeInfo(model.ReturnType);
		var isAsyncWithInnerType = isAsyncTaskT || isAsyncValueTaskT;
		var (isVoidTask, isVoidValueTask) = GetVoidAsyncInfo(model.ReturnType);
		var isVoidAsync = isVoidTask || isVoidValueTask;

		// Source field for Source(T) feature
		if (!string.IsNullOrEmpty(model.DeclaringInterface))
		{
			w.Line($"/// <summary>Source object to delegate to when no callback is configured.</summary>");
			w.Line($"internal {model.DeclaringInterface}? _source;");
			w.Line();
		}

		// Custom delegate declaration (always generated for all methods)
		if (model.CustomDelegateSignature != null)
		{
			var delegateSig = FormatMethodSignatureForDoc(model.MethodName, model.Parameters, model.ReturnType, model.IsVoid);
			w.Line($"/// <summary>Callback delegate for {delegateSig}.</summary>");
			w.Line(model.CustomDelegateSignature);
			w.Line();
		}

		// Predicate delegate declaration (for 2+ params, used by When chains)
		if (model.PredicateDelegateSignature != null)
		{
			var predicateSig = FormatMethodSignatureForDoc(model.MethodName, model.Parameters, model.ReturnType, model.IsVoid);
			w.Line($"/// <summary>Predicate delegate for {predicateSig}.</summary>");
			w.Line(model.PredicateDelegateSignature);
			w.Line();
		}

		// Constructor (with optional smart default factory)
		w.Line($"public {model.InterceptorClassName}() : base(\"{model.MethodName}\") {{ }}");
		if (!model.IsVoid && !string.IsNullOrEmpty(model.DefaultExpression) && model.DefaultExpression != "default!")
		{
			w.Line($"public {model.InterceptorClassName}(global::System.Func<object> smartDefaultFactory) : base(\"{model.MethodName}\", smartDefaultFactory) {{ }}");
		}
		w.Line();

		// Ref return backing field
		if (model.IsRefReturn)
		{
			w.Line($"internal {model.ReturnType} _refReturnBacking = default!;");
			w.Line();
		}

		// Abstract overrides for MethodInterceptorRuntime
		// Both InvokeDelegate and InvokeVoidDelegate are abstract and MUST be overridden by all subclasses.
		var isEffectivelyVoid = model.IsVoid && !isVoidAsync;

		if (model.HasRefStructParameter)
		{
			// Ref struct params/returns: args can't be boxed/unboxed. Override with throw (never called from ref struct Invoke path).
			w.Line("protected override void InvokeVoidDelegate(global::System.Delegate del, object? args) => throw new global::System.NotSupportedException(\"Ref struct parameters cannot be boxed.\");");
			w.Line("protected override object? InvokeDelegate(global::System.Delegate del, object? args) => throw new global::System.NotSupportedException(\"Ref struct parameters cannot be boxed.\");");
		}
		else
		{
			// InvokeVoidDelegate: typed cast + void invocation
			RenderBaseClassInvokeVoidDelegate(w, delegateType, tArgs, model.Parameters);

			// InvokeDelegate: typed cast + return boxed result
			if (isEffectivelyVoid)
			{
				// Sync void: InvokeDelegate forwards to InvokeVoidDelegate and returns null
				w.Line("protected override object? InvokeDelegate(global::System.Delegate del, object? args) { InvokeVoidDelegate(del, args); return null; }");
			}
			else
			{
				RenderBaseClassInvokeDelegate(w, delegateType, tArgs, model.ReturnType, model.Parameters);
			}

			// CreateValueDelegate (non-void only, including async)
			if (!model.IsVoid)
			{
				RenderBaseClassCreateValueDelegate(w, delegateType, model.ReturnType, model.Parameters);
			}
		}

		// RecordArgs
		var builderClassName = model.BuilderFriendlyName ?? "MethodCallBuilderImpl";
		RenderBaseClassRecordArgs(w, tArgs, model.TrackableParameters, model.Parameters, builderClassName);
		// RecordUnconfiguredArgs
		RenderBaseClassRecordUnconfiguredArgs(w, tArgs, model.LastArgType, model.LastArgsType, model.TrackableParameters, model.Parameters);
		w.Line();

		// Unconfigured last arg/args fields + LastArg/LastArgs property
		RenderBaseClassUnconfiguredArgFields(w, model.LastArgType, model.LastArgsType, model.TrackableParameters, model.IsVoid, builderClassName);

		// Call/Return entry points (API rename: callback entry is always "Call", value is "Return")
		RenderBaseClassEntryPoints(w, model, delegateType, fullInterceptorClassName, hasRefOrOut, isAsyncWithInnerType, isVoidAsync, isAsyncTaskT, isAsyncValueTaskT, isVoidTask, innerType);

		// When entry points (no ref/out, no ref struct)
		var canHaveWhenChain = !model.IsVoid && model.Parameters.Count > 0 && !hasRefOrOut && !model.HasRefStructParameter;
		var canHaveVoidWhenChain = model.IsVoid && model.Parameters.Count > 0 && !hasRefOrOut && !model.HasRefStructParameter;
		if (canHaveWhenChain)
		{
			RenderBaseClassWhenEntryPoints(w, fullInterceptorClassName, model.Parameters, model.ReturnType, tArgs, methodName: model.MethodName, xmlDocSummary: model.XmlDocSummary, predicateFriendlyName: model.PredicateFriendlyName);
		}
		if (canHaveVoidWhenChain)
		{
			RenderBaseClassVoidWhenEntryPoints(w, fullInterceptorClassName, model.Parameters, delegateType, tArgs, methodName: model.MethodName, xmlDocSummary: model.XmlDocSummary, predicateFriendlyName: model.PredicateFriendlyName);
		}

		// Invoke method
		RenderBaseClassInvokeMethod(w, model, options, tArgs);

		// InvokeRef method (for ref return methods)
		if (model.IsRefReturn)
		{
			RenderBaseClassInvokeRefMethod(w, model, options, tArgs);
		}

		// Reset override
		RenderBaseClassResetMethod(w, model.LastArgType, model.LastArgsType,
			hasSourceField: !string.IsNullOrEmpty(model.DeclaringInterface));

		// Inner classes
		RenderBaseClassMethodCallBuilderImpl(w, model, fullInterceptorClassName, delegateType, tArgs);
		RenderBaseClassMethodSequenceImpl(w, model, fullInterceptorClassName, delegateType, tArgs);

		if (canHaveWhenChain)
		{
			RenderBaseClassNonVoidWhenMatcherClasses(w, tArgs, model.Parameters, model.ReturnType);
			RenderBaseClassWhenBuilder(w, fullInterceptorClassName, model.Parameters, model.ReturnType, tArgs);
			RenderBaseClassWhenChain(w, fullInterceptorClassName, model.Parameters, model.ReturnType, delegateType, tArgs, model.PredicateFriendlyName);
		}
		if (canHaveVoidWhenChain)
		{
			RenderBaseClassVoidWhenMatcherClasses(w, tArgs, model.Parameters);
			RenderBaseClassVoidWhenChain(w, fullInterceptorClassName, model.Parameters, delegateType, tArgs, model.PredicateFriendlyName);
		}
	}

	// --- MethodInterceptorRuntime abstract overrides ---

	/// <summary>
	/// Renders the InvokeDelegate override for MethodInterceptorRuntime.
	/// Casts the Delegate and object? args to typed forms, invokes, returns boxed result.
	/// For async methods, the delegate is always the full async type (simplified callbacks are wrapped at registration).
	/// </summary>
	private static void RenderBaseClassInvokeDelegate(CodeWriter w, string delegateType, string tArgs, string returnType, EquatableArray<ParameterModel> parameters)
	{
		var callArgs = BuildBaseClassDelegateCallArgs(parameters, tArgs);
		// Args unpacking: cast object? to the TArgs type
		var argsCast = BuildArgsCast(parameters, tArgs);
		w.Line("protected override object? InvokeDelegate(global::System.Delegate del, object? args)");
		using (w.Braces())
		{
			if (parameters.Count > 0)
				w.Line($"{argsCast}");
			w.Line($"return (({delegateType})del)({callArgs});");
		}
	}

	/// <summary>
	/// Renders the InvokeVoidDelegate override for MethodInterceptorRuntime.
	/// For async void (Task/ValueTask), the delegate returns Task/ValueTask but we treat it as void invocation.
	/// </summary>
	private static void RenderBaseClassInvokeVoidDelegate(CodeWriter w, string delegateType, string tArgs, EquatableArray<ParameterModel> parameters)
	{
		var callArgs = BuildBaseClassDelegateCallArgs(parameters, tArgs);
		var argsCast = BuildArgsCast(parameters, tArgs);
		w.Line("protected override void InvokeVoidDelegate(global::System.Delegate del, object? args)");
		using (w.Braces())
		{
			if (parameters.Count > 0)
				w.Line($"{argsCast}");
			w.Line($"(({delegateType})del)({callArgs});");
		}
	}

	/// <summary>
	/// Renders the CreateValueDelegate override for MethodInterceptorRuntime.
	/// Returns a Delegate (typed as the full delegate type) that ignores args and returns the value.
	/// </summary>
	private static void RenderBaseClassCreateValueDelegate(CodeWriter w, string delegateType, string returnType, EquatableArray<ParameterModel> parameters)
	{
		var hasRefOrOut = parameters.Any(p => p.RefKind == Microsoft.CodeAnalysis.RefKind.Ref || p.RefKind == Microsoft.CodeAnalysis.RefKind.Out);
		if (hasRefOrOut)
		{
			// ref/out methods can't have value overloads, so CreateValueDelegate should never be called.
			// Provide an override that throws to satisfy the abstract requirement.
			w.Line("protected override global::System.Delegate CreateValueDelegate(object? value) => throw new global::System.NotSupportedException(\"Value delegates not supported for ref/out methods.\");");
		}
		else
		{
			var discards = BuildDiscardLambdaPrefix(parameters.Count);
			w.Line($"protected override global::System.Delegate CreateValueDelegate(object? value) => ({delegateType})({discards} => ({returnType})value!);");
		}
	}

	/// <summary>
	/// Renders the RecordArgs override for MethodInterceptorRuntime.
	/// Casts object? args to typed form and records on the typed builder.
	/// </summary>
	private static void RenderBaseClassRecordArgs(CodeWriter w, string tArgs, EquatableArray<ParameterModel> trackableParams, EquatableArray<ParameterModel> parameters, string builderClassName)
	{
		if (trackableParams.Count == 0)
		{
			// No args to record, but we need the override
			w.Line("protected override void RecordArgs(object? args, MethodCallBuilderBase tracking) { }");
		}
		else
		{
			w.Line("protected override void RecordArgs(object? args, MethodCallBuilderBase tracking)");
			using (w.Braces())
			{
				var argsCast = BuildArgsCastForRecord(parameters, tArgs);
				w.Line($"if (tracking is {builderClassName} impl) impl.RecordArg({argsCast});");
			}
		}
	}

	/// <summary>
	/// Renders the RecordUnconfiguredArgs override for MethodInterceptorRuntime.
	/// </summary>
	private static void RenderBaseClassRecordUnconfiguredArgs(CodeWriter w, string tArgs, string? lastArgType, string? lastArgsType, EquatableArray<ParameterModel> trackableParams, EquatableArray<ParameterModel> parameters)
	{
		if (lastArgType != null && trackableParams.Count == 1)
		{
			if (parameters.Count == 1)
			{
				// Single param total: cast directly
				w.Line($"protected override void RecordUnconfiguredArgs(object? args) => _unconfiguredLastArg = ({parameters.GetArray()![0].Type})args!;");
			}
			else
			{
				// Multiple total params but only 1 trackable (others are out/ref): extract from tuple
				var trackable = trackableParams.GetArray()![0];
				w.Line($"protected override void RecordUnconfiguredArgs(object? args) => _unconfiguredLastArg = (({tArgs})args!).{trackable.EscapedName};");
			}
		}
		else if (lastArgsType != null && trackableParams.Count > 1)
		{
			// Multi-param: cast to tuple, then extract fields
			w.Line($"protected override void RecordUnconfiguredArgs(object? args)");
			using (w.Braces())
			{
				w.Line($"var typedArgs = ({tArgs})args!;");
				var tupleParts = string.Join(", ", trackableParams.Select(p => $"typedArgs.{p.EscapedName}"));
				w.Line($"_unconfiguredLastArgs = ({tupleParts});");
			}
		}
		else
		{
			// 0 params
			w.Line("protected override void RecordUnconfiguredArgs(object? args) { }");
		}
	}

	/// <summary>
	/// Builds an args cast statement from object? to the typed TArgs form.
	/// For 0 params: no cast needed (args is Unit/default).
	/// For 1 param: "var typedArgs = (int)args!;"
	/// For 2+ params: "var typedArgs = ((int a, string b))args!;"
	/// </summary>
	private static string BuildArgsCast(EquatableArray<ParameterModel> parameters, string tArgs)
	{
		if (parameters.Count == 0)
			return "";
		return $"var typedArgs = ({tArgs})args!;";
	}

	/// <summary>
	/// Builds the args cast expression for RecordArg. Returns the expression to pass to RecordArg.
	/// </summary>
	private static string BuildArgsCastForRecord(EquatableArray<ParameterModel> parameters, string tArgs)
	{
		if (parameters.Count == 0) return "default";
		if (parameters.Count == 1) return $"({parameters.GetArray()![0].Type})args!";
		return $"({tArgs})args!";
	}

	/// <summary>
	/// Builds the delegate call args for InvokeDelegate/InvokeVoidDelegate.
	/// Uses "typedArgs" variable from BuildArgsCast.
	/// For 0 params: "", for 1 param: "typedArgs",
	/// for 2+ params without ref/out: "typedArgs" (tuple passed as single arg to Func/Action),
	/// for 2+ params with ref/out: "typedArgs.a, typedArgs.b" (unpacked for custom delegate).
	/// </summary>
	/// <summary>
	/// Builds the arguments for invoking the stored delegate from InvokeDelegate/InvokeVoidDelegate.
	/// For tuple delegates (2+ params, no ref/out, non-delegate stubs): passes typedArgs as single tuple.
	/// For individual-param delegates (ref/out, delegate stubs, 0-1 params): unpacks tuple fields.
	/// </summary>
	private static string BuildBaseClassDelegateCallArgs(EquatableArray<ParameterModel> parameters, string tArgs)
	{
		if (parameters.Count == 0)
			return ""; // Action with no params -- delegate takes no args
		// Only add ref/out prefix (not "in " -- delegates don't use "in" modifier)
		static string GetDelegateRefPrefix(ParameterModel p)
		{
			return p.RefKind switch
			{
				Microsoft.CodeAnalysis.RefKind.Ref => "ref ",
				Microsoft.CodeAnalysis.RefKind.Out => "out ",
				_ => "" // "in" and none both pass by value to delegate
			};
		}
		if (parameters.Count == 1)
		{
			var p = parameters.GetArray()![0];
			return $"{GetDelegateRefPrefix(p)}typedArgs";
		}
		// Individual-param delegate -- unpack from tuple storage
		return string.Join(", ", parameters.Select(p => $"{GetDelegateRefPrefix(p)}typedArgs.{p.EscapedName}"));
	}

	// --- Unconfigured arg fields and LastArg/LastArgs properties ---

	private static void RenderBaseClassUnconfiguredArgFields(CodeWriter w, string? lastArgType, string? lastArgsType, EquatableArray<ParameterModel> trackableParams, bool isVoid, string builderClassName)
	{
		// Manual cast approach for LastArg/LastArgs: FindLastArgInTracking doesn't work for value types
		// because TResult? doesn't become Nullable<T> without a struct constraint.
		// Non-void interceptors check _returnValueTracking first.
		// Void interceptors only have _callTracking and _sequence.

		if (lastArgType != null && trackableParams.Count == 1)
		{
			var nullableType = lastArgType.EndsWith("?") ? lastArgType : $"{lastArgType}?";
			w.Line($"private {nullableType} _unconfiguredLastArg;");
			w.Line();

			w.Line($"/// <summary>Last argument from the most recently called registration.</summary>");
			w.Line($"public {nullableType} LastArg");
			using (w.Braces())
			{
				w.Line("get");
				using (w.Braces())
				{
					if (!isVoid)
						w.Line($"if ((_returnValueTracking?._callCount ?? 0) > 0 && _returnValueTracking is {builderClassName} rvb) return rvb.LastArg;");
					w.Line($"if ((_callTracking?._callCount ?? 0) > 0 && _callTracking is {builderClassName} cb) return cb.LastArg;");
					w.Line($"if (_sequence != null) for (int i = _sequence.Count - 1; i >= 0; i--) if (_sequence[i].Tracking._callCount > 0 && _sequence[i].Tracking is {builderClassName} sb) return sb.LastArg;");
					w.Line("return _unconfiguredCallCount > 0 ? _unconfiguredLastArg : default;");
				}
			}
			w.Line();
		}
		else if (lastArgsType != null && trackableParams.Count > 1)
		{
			var nullableType = lastArgsType.EndsWith("?") ? lastArgsType : $"{lastArgsType}?";
			w.Line($"private {nullableType} _unconfiguredLastArgs;");
			w.Line();

			w.Line($"/// <summary>Last arguments from the most recently called registration.</summary>");
			w.Line($"public {nullableType} LastArgs");
			using (w.Braces())
			{
				w.Line("get");
				using (w.Braces())
				{
					if (!isVoid)
						w.Line($"if ((_returnValueTracking?._callCount ?? 0) > 0 && _returnValueTracking is {builderClassName} rvb) return rvb.LastArgs;");
					w.Line($"if ((_callTracking?._callCount ?? 0) > 0 && _callTracking is {builderClassName} cb) return cb.LastArgs;");
					w.Line($"if (_sequence != null) for (int i = _sequence.Count - 1; i >= 0; i--) if (_sequence[i].Tracking._callCount > 0 && _sequence[i].Tracking is {builderClassName} sb) return sb.LastArgs;");
					w.Line("return _unconfiguredCallCount > 0 ? _unconfiguredLastArgs : default;");
				}
			}
			w.Line();
		}
	}

	// --- Call/Return entry points ---

	/// <summary>
	/// Renders Call/Return entry points for single-signature interceptors in base-class mode.
	/// API: Call(callback) for ALL callbacks (void and non-void), Return(value) for values only.
	/// For async methods (Task&lt;T&gt;/ValueTask&lt;T&gt;), also generates simplified Call overloads.
	/// </summary>
	private static void RenderBaseClassEntryPoints(
		CodeWriter w,
		UnifiedMethodInterceptorModel model,
		string delegateType,
		string fullInterceptorClassName,
		bool hasRefOrOut,
		bool isAsyncWithInnerType,
		bool isVoidAsync,
		bool isAsyncTaskT,
		bool isAsyncValueTaskT,
		bool isVoidTask,
		string innerType)
	{
		var hasValueOverload = !model.IsVoid && !hasRefOrOut && !model.HasRefStructParameter;
		var isEffectivelyVoid = model.IsVoid && !isVoidAsync;
		var builderClassName = model.BuilderFriendlyName ?? "MethodCallBuilderImpl";
		var sequenceClassName = model.SequenceFriendlyName ?? "MethodSequenceImpl";

		// Call(callback) - full signature callback, for ALL methods (void and non-void)
		EmitCallXmlDoc(w, model.MethodName, model.Parameters, model.XmlDocSummary, model.ReturnType, model.IsVoid);
		w.Line($"public {builderClassName} Call({delegateType} callback)");
		using (w.Braces())
		{
			w.Line($"var builder = new {builderClassName}(this);");
			if (isEffectivelyVoid)
				w.Line("SetupVoidCallback(callback, builder);");
			else
				w.Line("SetupReturnCallback(callback, builder);");
			w.Line("return builder;");
		}
		w.Line();

		// Call(simplifiedCallback) - for Task<T>/ValueTask<T> methods: accepts Func<..., TInnerType>
		if (isAsyncWithInnerType && !hasRefOrOut)
		{
			var simplifiedDelegateType = BuildSimplifiedDelegateType(model.Parameters, innerType);
			EmitCallXmlDoc(w, model.MethodName, model.Parameters, model.XmlDocSummary, innerType, false, $"Result auto-wrapped in {(isAsyncTaskT ? "Task" : "ValueTask")}.");
			w.Line($"public {builderClassName} Call({simplifiedDelegateType} callback)");
			using (w.Braces())
			{
				w.Line($"var builder = new {builderClassName}(this);");
				var wrapExpr = BuildAsyncWrapExpression(model.Parameters, innerType, isAsyncTaskT);
				w.Line($"SetupReturnCallback(({delegateType})({wrapExpr}), builder);");
				w.Line("return builder;");
			}
			w.Line();
		}

		// Call(simplifiedVoidCallback) - for Task/ValueTask void methods: accepts Action<...>
		if (isVoidAsync && !hasRefOrOut)
		{
			var voidDelegateType = BuildSimplifiedVoidDelegateType(model.Parameters);
			EmitCallXmlDoc(w, model.MethodName, model.Parameters, model.XmlDocSummary, model.ReturnType, true, $"{(isVoidTask ? "Task.CompletedTask" : "default(ValueTask)")} auto-returned.");
			w.Line($"public {builderClassName} Call({voidDelegateType} callback)");
			using (w.Braces())
			{
				w.Line($"var builder = new {builderClassName}(this);");
				var wrapExpr = BuildVoidAsyncWrapExpression(model.Parameters, isVoidTask);
				w.Line($"SetupReturnCallback(({delegateType})({wrapExpr}), builder);");
				w.Line("return builder;");
			}
			w.Line();
		}

		// Return(value) for non-void (value only, never lambda)
		if (hasValueOverload)
		{
			var (valueStorageType, _, _) = GetAsyncTypeInfo(model.ReturnType);
			EmitReturnXmlDoc(w, model.MethodName, model.Parameters, model.XmlDocSummary, model.ReturnType, model.IsVoid);
			w.Line($"public {builderClassName} Return({valueStorageType} value)");
			using (w.Braces())
			{
				w.Line($"var builder = new {builderClassName}(this);");
				if (isAsyncTaskT || isAsyncValueTaskT)
				{
					if (isAsyncTaskT)
						w.Line("SetupReturnValue(global::System.Threading.Tasks.Task.FromResult(value), builder);");
					else
						w.Line($"SetupReturnValue(new global::System.Threading.Tasks.ValueTask<{innerType}>(value), builder);");
				}
				else
					w.Line("SetupReturnValue(value, builder);");
				w.Line("return builder;");
			}
			w.Line();

			// Return(first, params rest) - creates sequence from multiple values
			w.Line($"/// <summary>Configures sequence of return values. Each value returned once, last repeats.</summary>");
			w.Line($"public {sequenceClassName} Return({valueStorageType} first, params {valueStorageType}[] rest)");
			using (w.Braces())
			{
				w.Line("var builder = Return(first);");
				w.Line("if (rest.Length == 0)");
				using (w.Braces())
				{
					w.Line("return builder.ThenReturn(first);");
				}
				w.Line("var seq = builder.ThenReturn(rest[0]);");
				w.Line("for (int i = 1; i < rest.Length; i++)");
				using (w.Braces())
				{
					w.Line("seq.ThenReturn(rest[i]);");
				}
				w.Line("return seq;");
			}
			w.Line();
		}
	}

	/// <summary>
	/// Builds the lambda expression to wrap a simplified async callback into the full async delegate.
	/// For base-class mode: both full and simplified delegates use tuple for 2+ params, so wrapping is simple.
	/// E.g., for 2+ params: "(args) =&gt; Task.FromResult(callback(args))"
	/// E.g., for 1 param: "(int id) =&gt; Task.FromResult(callback(id))"
	/// </summary>
	private static string BuildAsyncWrapExpression(EquatableArray<ParameterModel> parameters, string innerType, bool isTaskT)
	{
		// Full delegate and simplified callback both use individual params.
		var paramDecls = BuildDelegateMatchingParamDecls(parameters);
		var callbackCallArgs = BuildDelegateMatchingCallArgs(parameters);
		var callbackCall = parameters.Count == 0 ? "callback()" : $"callback({callbackCallArgs})";
		var wrapCall = isTaskT
			? $"global::System.Threading.Tasks.Task.FromResult({callbackCall})"
			: $"new global::System.Threading.Tasks.ValueTask<{innerType}>({callbackCall})";
		return $"{paramDecls} => {wrapCall}";
	}

	/// <summary>
	/// Builds the lambda expression to wrap a void async callback (Action) into Func&lt;Task&gt;/Func&lt;ValueTask&gt;.
	/// Full delegate uses individual params; simplified void callback uses tuple for 2+ params.
	/// </summary>
	private static string BuildVoidAsyncWrapExpression(EquatableArray<ParameterModel> parameters, bool isVoidTask)
	{
		// Full delegate and simplified void callback both use individual params.
		var paramDecls = BuildDelegateMatchingParamDecls(parameters);
		var callbackCallArgs = BuildDelegateMatchingCallArgs(parameters);
		var callbackCall = parameters.Count == 0 ? "callback()" : $"callback({callbackCallArgs})";
		var completedExpr = isVoidTask
			? "global::System.Threading.Tasks.Task.CompletedTask"
			: "default(global::System.Threading.Tasks.ValueTask)";
		return $"{paramDecls} => {{ {callbackCall}; return {completedExpr}; }}";
	}

	/// <summary>Builds individual parameter declarations matching the custom delegate signature (for ref/out or overload delegates): "()" for 0, "(int x)" for 1, "(int a, int b)" for 2+.</summary>
	private static string BuildDelegateMatchingParamDecls(EquatableArray<ParameterModel> parameters)
	{
		if (parameters.Count == 0) return "()";
		var decls = string.Join(", ", parameters.Select(p => $"{p.Type} {p.EscapedName}"));
		return $"({decls})";
	}

	/// <summary>Builds individual call args matching custom delegate params: "" for 0, "x" for 1, "a, b" for 2+.</summary>
	private static string BuildDelegateMatchingCallArgs(EquatableArray<ParameterModel> parameters)
	{
		if (parameters.Count == 0) return "";
		return string.Join(", ", parameters.Select(p => p.EscapedName));
	}

	// --- When entry points (base class mode) ---

	private static void RenderBaseClassWhenEntryPoints(
		CodeWriter w,
		string interceptorClassName,
		EquatableArray<ParameterModel> parameters,
		string returnType,
		string tArgs,
		string? methodName = null,
		string? xmlDocSummary = null,
		string? predicateFriendlyName = null)
	{
		if (parameters.Count == 0) return;

		var paramTypeList = BuildParamTypeList(parameters);

		// When() value overload - exact value matching
		EmitWhenXmlDoc(w, methodName, parameters, xmlDocSummary, returnType, false, "Matches exact values using Object.Equals. Returns builder for Return().");
		w.Line($"public WhenBuilder When({paramTypeList})");
		using (w.Braces())
		{
			w.Line("_whenChain ??= new global::System.Collections.Generic.List<WhenMatcherBase>();");
			// Build predicate that bridges individual params to TArgs predicate
			if (parameters.Count == 1)
			{
				var p = parameters.GetArray()![0];
				w.Line($"return new WhenBuilder(this, (_arg0) => global::System.Object.Equals(_arg0, {p.EscapedName}));");
			}
			else
			{
				// Multi-param: (args) => object.Equals(args.name1, name1) && ...
				var predicateBody = string.Join(" && ", parameters.Select(p => $"global::System.Object.Equals(args.{p.EscapedName}, {p.EscapedName})"));
				w.Line($"return new WhenBuilder(this, (args) => {predicateBody});");
			}
		}
		w.Line();

		// When() predicate overload - custom predicate delegate for 2+ params, Func<T, bool> for 0-1
		{
			var predicateType = BuildPredicateType(parameters, predicateFriendlyName);
			EmitWhenXmlDoc(w, methodName, parameters, xmlDocSummary, returnType, false, "Matches using predicate. Returns builder for Return().");
			w.Line($"public WhenBuilder When({predicateType} predicate)");
			using (w.Braces())
			{
				w.Line("_whenChain ??= new global::System.Collections.Generic.List<WhenMatcherBase>();");
				if (parameters.Count >= 2 && predicateFriendlyName != null)
				{
					// Bridge custom predicate delegate (individual params) to tuple-based internal predicate
					var unpackedArgs = string.Join(", ", parameters.Select(p => $"args.{p.EscapedName}"));
					w.Line($"return new WhenBuilder(this, (args) => predicate({unpackedArgs}));");
				}
				else
				{
					// 0-1 params: Func<T, bool> matches internal predicate type directly
					w.Line("return new WhenBuilder(this, predicate);");
				}
			}
			w.Line();
		}
	}

	private static void RenderBaseClassVoidWhenEntryPoints(
		CodeWriter w,
		string interceptorClassName,
		EquatableArray<ParameterModel> parameters,
		string delegateType,
		string tArgs,
		string? methodName = null,
		string? xmlDocSummary = null,
		string? predicateFriendlyName = null)
	{
		if (parameters.Count == 0) return;

		var paramTypeList = BuildParamTypeList(parameters);

		// When() value overload
		EmitWhenXmlDoc(w, methodName, parameters, xmlDocSummary, extraSummary: "Matches exact values using Object.Equals. Returns chain directly.");
		w.Line($"public VoidWhenChain When({paramTypeList})");
		using (w.Braces())
		{
			w.Line("_whenChain ??= new global::System.Collections.Generic.List<WhenMatcherBase>();");
			if (parameters.Count == 1)
			{
				var p = parameters.GetArray()![0];
				w.Line($"var matcher = new VoidWhenMatcherPredicateBase((_arg0) => global::System.Object.Equals(_arg0, {p.EscapedName}));");
			}
			else
			{
				var predicateBody = string.Join(" && ", parameters.Select(p => $"global::System.Object.Equals(args.{p.EscapedName}, {p.EscapedName})"));
				w.Line($"var matcher = new VoidWhenMatcherPredicateBase((args) => {predicateBody});");
			}
			w.Line("_whenChain.Add(matcher);");
			w.Line("return new VoidWhenChain(this, matcher);");
		}
		w.Line();

		// When() predicate overload - custom predicate delegate for 2+ params, Func<T, bool> for 0-1
		{
			var predicateType = BuildPredicateType(parameters, predicateFriendlyName);
			EmitWhenXmlDoc(w, methodName, parameters, xmlDocSummary, extraSummary: "Matches using predicate. Returns chain directly.");
			w.Line($"public VoidWhenChain When({predicateType} predicate)");
			using (w.Braces())
			{
				w.Line("_whenChain ??= new global::System.Collections.Generic.List<WhenMatcherBase>();");
				if (parameters.Count >= 2 && predicateFriendlyName != null)
				{
					// Bridge custom predicate delegate (individual params) to tuple-based internal predicate
					var unpackedArgs = string.Join(", ", parameters.Select(p => $"args.{p.EscapedName}"));
					w.Line($"var matcher = new VoidWhenMatcherPredicateBase((args) => predicate({unpackedArgs}));");
				}
				else
				{
					// 0-1 params: Func<T, bool> matches internal predicate type directly
					w.Line("var matcher = new VoidWhenMatcherPredicateBase(predicate);");
				}
				w.Line("_whenChain.Add(matcher);");
				w.Line("return new VoidWhenChain(this, matcher);");
			}
			w.Line();
		}
	}

	// --- Invoke method (MethodInterceptorRuntime mode) ---

	private static void RenderBaseClassInvokeMethod(
		CodeWriter w,
		UnifiedMethodInterceptorModel model,
		InterceptorRenderOptions options,
		string tArgs)
	{
		var needsStubParam = options.StubOverrideFallback && !string.IsNullOrEmpty(options.StubTypeName) && !string.IsNullOrEmpty(model.StubOverrideName);
		var invokeParams = BuildInvokeParams(model.Parameters, options.IncludeStrictParameter, needsStubParam ? options.StubTypeName : null);
		var returnType = model.IsVoid ? "void" : model.ReturnType;
		var delegateType = model.CallDelegateType.TrimEnd('?');
		var hasRefOrOut = model.Parameters.Any(p => p.RefKind == Microsoft.CodeAnalysis.RefKind.Ref || p.RefKind == Microsoft.CodeAnalysis.RefKind.Out);

		// Check async info
		var (_, isVoidTask, isVoidValueTask) = (false, false, false);
		{
			var vi = GetVoidAsyncInfo(model.ReturnType);
			isVoidTask = vi.IsTask;
			isVoidValueTask = vi.IsValueTask;
		}
		var isVoidAsync = isVoidTask || isVoidValueTask;
		// For void async methods, the priority chain treats them as non-void (delegates return Task/ValueTask)
		var useVoidPriorityChain = model.IsVoid && !isVoidAsync;

		w.Line($"/// <summary>Invokes the configured callback. Called by explicit interface implementation.</summary>");
		w.Line($"internal {returnType} Invoke({invokeParams})");
		using (w.Braces())
		{
			// Assign defaults to out parameters (they can't be read before assignment)
			foreach (var p in model.Parameters.Where(p => p.RefKind == Microsoft.CodeAnalysis.RefKind.Out))
			{
				w.Line($"{p.EscapedName} = default!;");
			}

			if (model.HasRefStructParameter)
			{
				// Ref struct parameters cannot be boxed, stored in tuples, or used as generic type args.
				// Generate a simplified invoke path: sequence > callback > unconfigured.
				// No args tracking (LastArgs/RecordArgs/RecordUnconfiguredArgs).
				var callbackArgs = BuildCallbackArgs(model.Parameters);

				// Sequence
				w.Line("if (_sequence != null && _sequenceIndex < _sequence.Count)");
				using (w.Braces())
				{
					w.Line("var (__callback, __tracking) = _sequence[_sequenceIndex];");
					w.Line("__tracking.RecordCallBase();");
					w.Line("_sequenceIndex++;");
					if (model.IsVoid)
					{
						w.Line($"(({delegateType})__callback)({callbackArgs});");
						w.Line("return;");
					}
					else
					{
						w.Line($"return (({delegateType})__callback)({callbackArgs});");
					}
				}
				w.Line();

				// Callback
				w.Line("if (_call != null && _callTracking != null)");
				using (w.Braces())
				{
					w.Line("_callTracking.RecordCallBase();");
					if (model.IsVoid)
					{
						w.Line($"(({delegateType})_call)({callbackArgs});");
						w.Line("return;");
					}
					else
					{
						w.Line($"return (({delegateType})_call)({callbackArgs});");
					}
				}
				w.Line();

				// Unconfigured tail
				w.Line("_unconfiguredCallCount++;");

				// Sequence exhausted repeat
				w.Line("if (_sequence != null && _sequenceIndex >= _sequence.Count)");
				using (w.Braces())
				{
					w.Line($"if ({options.StrictAccessExpression}) throw global::KnockOff.StubException.SequenceExhausted(\"{model.MethodName}\");");
					w.Line("if (_repeatLastValue && _sequence.Count > 0)");
					using (w.Braces())
					{
						w.Line("var (__callback, __tracking) = _sequence[_sequence.Count - 1];");
						w.Line("__tracking.RecordCallBase();");
						if (model.IsVoid)
						{
							w.Line($"(({delegateType})__callback)({callbackArgs});");
							w.Line("return;");
						}
						else
						{
							w.Line($"return (({delegateType})__callback)({callbackArgs});");
						}
					}
					if (model.IsVoid)
					{
						w.Line("return;");
					}
				}

				// Final fallback
				RenderInvokeFinalFallback(w, model, options);
			}
			else if (hasRefOrOut)
			{
				// Ref/out methods: inline the priority chain to invoke delegates directly with
				// the original ref/out parameters. Boxing into object? would lose ref modifications.
				// Note: ref/out methods never have When chains or Return(value) overloads.
				var callbackArgs = BuildCallbackArgs(model.Parameters);

				// Build boxed args for tracking only (RecordArgs/RecordUnconfiguredArgs)
				string argsExpr;
				if (model.Parameters.Count == 1)
				{
					argsExpr = model.Parameters.GetArray()![0].EscapedName;
					w.Line($"object? __args = {argsExpr};");
					argsExpr = "__args";
				}
				else
				{
					var tupleExpr = "(" + string.Join(", ", model.Parameters.Select(p => p.EscapedName)) + ")";
					w.Line($"object __args = ({tArgs}){tupleExpr};");
					argsExpr = "__args";
				}

				// Sequence (highest priority after When chain, which ref/out doesn't have)
				w.Line("if (_sequence != null && _sequenceIndex < _sequence.Count)");
				using (w.Braces())
				{
					w.Line("var (__callback, __tracking) = _sequence[_sequenceIndex];");
					w.Line("__tracking.RecordCallBase();");
					w.Line($"RecordArgs({argsExpr}, __tracking);");
					w.Line("_sequenceIndex++;");
					if (model.IsVoid)
					{
						w.Line($"(({delegateType})__callback)({callbackArgs});");
						w.Line("return;");
					}
					else
					{
						w.Line($"return (({delegateType})__callback)({callbackArgs});");
					}
				}
				w.Line();

				// Callback
				w.Line("if (_call != null && _callTracking != null)");
				using (w.Braces())
				{
					w.Line("_callTracking.RecordCallBase();");
					w.Line($"RecordArgs({argsExpr}, _callTracking);");
					if (model.IsVoid)
					{
						w.Line($"(({delegateType})_call)({callbackArgs});");
						w.Line("return;");
					}
					else
					{
						w.Line($"return (({delegateType})_call)({callbackArgs});");
					}
				}
				w.Line();

				// Unconfigured tail
				w.Line("_unconfiguredCallCount++;");
				w.Line($"RecordUnconfiguredArgs({argsExpr});");

				// Sequence exhausted repeat
				w.Line("if (_sequence != null && _sequenceIndex >= _sequence.Count)");
				using (w.Braces())
				{
					w.Line($"if ({options.StrictAccessExpression}) throw global::KnockOff.StubException.SequenceExhausted(\"{model.MethodName}\");");
					w.Line("if (_repeatLastValue && _sequence.Count > 0)");
					using (w.Braces())
					{
						w.Line("var (__callback, __tracking) = _sequence[_sequence.Count - 1];");
						w.Line("__tracking.RecordCallBase();");
						w.Line($"RecordArgs({argsExpr}, __tracking);");
						if (model.IsVoid)
						{
							w.Line($"(({delegateType})__callback)({callbackArgs});");
							w.Line("return;");
						}
						else
						{
							w.Line($"return (({delegateType})__callback)({callbackArgs});");
						}
					}
					// Exhausted, no repeat (ThenDefault or empty) -> fall through to default
					if (model.IsVoid)
						w.Line("return;");
					else
					{
						var defaultExpr = string.IsNullOrEmpty(model.DefaultExpression) ? "default!" : model.DefaultExpression;
						w.Line($"return {defaultExpr};");
					}
				}
				w.Line();

				// Final fallback: Stub Override > Source > Strict > Default
				RenderInvokeFinalFallback(w, model, options);
			}
			else
			{
				// Non-ref/out methods: use the base class priority chain (boxed args)
				// Build the args value for the priority chain (boxed as object?)
				// Use __args prefix to avoid collision with parameter names
				string argsExpr;
				if (model.Parameters.Count == 0)
					argsExpr = "null";
				else if (model.Parameters.Count == 1)
				{
					// Box single param
					argsExpr = model.Parameters.GetArray()![0].EscapedName;
				}
				else
				{
					// Create tuple and box
					var tupleExpr = "(" + string.Join(", ", model.Parameters.Select(p => p.EscapedName)) + ")";
					w.Line($"object __args = ({tArgs}){tupleExpr};");
					argsExpr = "__args";
				}

				// For single params, create a local to avoid multiple boxing
				if (model.Parameters.Count == 1)
				{
					w.Line($"object? __args = {argsExpr};");
					argsExpr = "__args";
				}

				// Run priority chain (use __ prefix to avoid collision with out/ref parameter names)
				if (useVoidPriorityChain)
				{
					w.Line($"if (RunVoidPriorityChain({argsExpr})) return;");
				}
				else
				{
					w.Line($"var (__handled, __result) = RunPriorityChain({argsExpr});");
					w.Line($"if (__handled) return ({model.ReturnType})__result!;");
				}

				// Sequence exhausted repeat (checked BEFORE incrementing unconfigured count,
				// because sequence repeat is configured behavior and should not trigger
				// the class stub's "unconfigured -> fall back to base" logic)
				if (useVoidPriorityChain)
				{
					w.Line($"if (HandleVoidSequenceExhaustedRepeat({options.StrictAccessExpression}, {argsExpr})) return;");
				}
				else
				{
					w.Line($"var (__seqHandled, __seqResult) = HandleNonVoidSequenceExhaustedRepeat({options.StrictAccessExpression}, {argsExpr});");
					var defaultExpr = string.IsNullOrEmpty(model.DefaultExpression) ? $"default({model.ReturnType})!" : model.DefaultExpression;
					w.Line($"if (__seqHandled) return __seqResult is null ? {defaultExpr} : ({model.ReturnType})__seqResult;");
				}

				// Unconfigured tail (only reached if no configured behavior handled the call)
				w.Line("_unconfiguredCallCount++;");
				w.Line($"RecordUnconfiguredArgs({argsExpr});");

				// Final fallback: Stub Override > Source > Strict > Default
				RenderInvokeFinalFallback(w, model, options);
			}
		}
		w.Line();
	}

	/// <summary>
	/// Renders the final fallback section of the Invoke method: Stub Override > Source > Strict > Default.
	/// Shared between ref/out and non-ref/out paths.
	/// </summary>
	private static void RenderInvokeFinalFallback(
		CodeWriter w,
		UnifiedMethodInterceptorModel model,
		InterceptorRenderOptions options)
	{
		if (options.StubOverrideFallback && !string.IsNullOrEmpty(model.StubOverrideName))
		{
			var stubOverrideCallArgs = string.Join(", ", model.Parameters.Select(p => $"{p.RefPrefix}{p.EscapedName}"));
			var methodPrefix = !string.IsNullOrEmpty(options.StubTypeName) ? "stub." : "";
			if (model.IsVoid)
			{
				w.Line($"{methodPrefix}{model.StubOverrideName}({stubOverrideCallArgs});");
				w.Line("return;");
			}
			else
			{
				w.Line($"return {methodPrefix}{model.StubOverrideName}({stubOverrideCallArgs});");
			}
		}
		else
		{
			if (!string.IsNullOrEmpty(model.DeclaringInterface))
			{
				// Justification: Source delegation passes through the source's return value and out parameters.
				// The compiler cannot prove nullability matches the interceptor's declared types (e.g., out parameters
				// on TryGetValue-style methods, or return types with nullable mismatches). This is inherent to
				// source delegation where the stub proxies an unknown implementation.
				w.Line("#pragma warning disable CS8601 // Possible null reference assignment");
				var sourceCallArgs = string.Join(", ", model.Parameters.Select(p => $"{p.RefPrefix}{p.EscapedName}"));
				if (model.IsVoid)
				{
					w.Line($"if (_source is {{ }} src) {{ src.{model.MethodName}({sourceCallArgs}); return; }}");
				}
				else
				{
					w.Line($"if (_source is {{ }} src) return src.{model.MethodName}({sourceCallArgs});");
				}
				w.Line("#pragma warning restore CS8601");
			}

			w.Line($"if ({options.StrictAccessExpression}) throw global::KnockOff.StubException.NotConfigured(\"\", \"{model.MethodName}\");");
			if (model.IsVoid)
				w.Line("return;");
			else if (model.ThrowsOnDefault)
				w.Line($"throw new global::System.InvalidOperationException(\"No implementation provided for {model.MethodName}. Configure via Call or Return.\");");
			else
			{
				var defaultExpr = string.IsNullOrEmpty(model.DefaultExpression) ? "default!" : model.DefaultExpression;
				w.Line($"return {defaultExpr};");
			}
		}
	}

	/// <summary>
	/// Renders InvokeRef method for ref return methods. Stores result in _refReturnBacking.
	/// </summary>
	private static void RenderBaseClassInvokeRefMethod(
		CodeWriter w,
		UnifiedMethodInterceptorModel model,
		InterceptorRenderOptions options,
		string tArgs)
	{
		var needsStubParam = options.StubOverrideFallback && !string.IsNullOrEmpty(options.StubTypeName) && !string.IsNullOrEmpty(model.StubOverrideName);
		var invokeParams = BuildInvokeParams(model.Parameters, options.IncludeStrictParameter, needsStubParam ? options.StubTypeName : null);

		w.Line($"/// <summary>Invokes and stores result for ref return. Called by explicit interface implementation.</summary>");
		w.Line($"internal void InvokeRef({invokeParams})");
		using (w.Braces())
		{
			string argsExpr;
			if (model.Parameters.Count == 0)
				argsExpr = "null";
			else if (model.Parameters.Count == 1)
			{
				argsExpr = model.Parameters.GetArray()![0].EscapedName;
				w.Line($"object? __args = {argsExpr};");
				argsExpr = "__args";
			}
			else
			{
				var tupleExpr = "(" + string.Join(", ", model.Parameters.Select(p => p.EscapedName)) + ")";
				w.Line($"object __args = ({tArgs}){tupleExpr};");
				argsExpr = "__args";
			}

			w.Line($"var (__handled, __result) = RunPriorityChain({argsExpr});");
			w.Line($"if (__handled) {{ _refReturnBacking = ({model.ReturnType})__result!; return; }}");

			// Sequence exhausted repeat (checked BEFORE incrementing unconfigured count)
			w.Line($"var (__seqHandled, __seqResult) = HandleNonVoidSequenceExhaustedRepeat({options.StrictAccessExpression}, {argsExpr});");
			{
				var defaultExprRef = string.IsNullOrEmpty(model.DefaultExpression) ? $"default({model.ReturnType})!" : model.DefaultExpression;
				w.Line($"if (__seqHandled) {{ _refReturnBacking = __seqResult is null ? {defaultExprRef} : ({model.ReturnType})__seqResult; return; }}");
			}

			w.Line("_unconfiguredCallCount++;");
			w.Line($"RecordUnconfiguredArgs({argsExpr});");

			if (options.StubOverrideFallback && !string.IsNullOrEmpty(model.StubOverrideName))
			{
				var stubOverrideCallArgs = string.Join(", ", model.Parameters.Select(p => $"{p.RefPrefix}{p.EscapedName}"));
				var methodPrefix = !string.IsNullOrEmpty(options.StubTypeName) ? "stub." : "";
				w.Line($"_refReturnBacking = {methodPrefix}{model.StubOverrideName}({stubOverrideCallArgs});");
			}
			else
			{
				if (!string.IsNullOrEmpty(model.DeclaringInterface))
				{
					// Justification: Source delegation assigns the source's return value to the ref backing field.
					// Nullability mismatch is inherent to source delegation proxying an unknown implementation.
					w.Line("#pragma warning disable CS8601 // Possible null reference assignment");
					var sourceCallArgs = string.Join(", ", model.Parameters.Select(p => $"{p.RefPrefix}{p.EscapedName}"));
					w.Line($"if (_source is {{ }} src) {{ _refReturnBacking = src.{model.MethodName}({sourceCallArgs}); return; }}");
					w.Line("#pragma warning restore CS8601");
				}
				w.Line($"if ({options.StrictAccessExpression}) throw global::KnockOff.StubException.NotConfigured(\"\", \"{model.MethodName}\");");
				var defaultExpr = string.IsNullOrEmpty(model.DefaultExpression) ? "default!" : model.DefaultExpression;
				w.Line($"_refReturnBacking = {defaultExpr};");
			}
		}
		w.Line();
	}

	// --- Reset override (base class mode) ---

	private static void RenderBaseClassResetMethod(CodeWriter w, string? lastArgType, string? lastArgsType, bool hasSourceField)
	{
		w.Line("/// <summary>Resets tracking state but preserves configuration and verifiable marking.</summary>");
		w.Line("public override void Reset()");
		using (w.Braces())
		{
			w.Line("base.Reset();");
			if (lastArgType != null)
				w.Line("_unconfiguredLastArg = default;");
			if (lastArgsType != null)
				w.Line("_unconfiguredLastArgs = default;");
			if (hasSourceField)
				w.Line("_source = null;");
		}
		w.Line();
	}

	// --- Thin inner classes (base class mode) ---

	private static void RenderBaseClassMethodCallBuilderImpl(
		CodeWriter w,
		UnifiedMethodInterceptorModel model,
		string interceptorClassName,
		string delegateType,
		string tArgs)
	{
		var trackableParams = model.TrackableParameters;
		var lastArgType = model.LastArgType;
		var lastArgsType = model.LastArgsType;
		var builderClassName = model.BuilderFriendlyName ?? "MethodCallBuilderImpl";
		var sequenceClassName = model.SequenceFriendlyName ?? "MethodSequenceImpl";

		string baseClass;
		if (model.IsVoid)
		{
			baseClass = "MethodCallBuilderBase";
		}
		else
		{
			baseClass = "ReturnMethodCallBuilderBase";
		}

		w.Line($"/// <summary>Builder for callback registration. Supports tracking and lazy elevation to sequence.</summary>");
		w.Line($"public sealed class {builderClassName} : {baseClass}, {model.BuilderInterface}");
		using (w.Braces())
		{
			w.Line($"private readonly {interceptorClassName} _typedInterceptor;");

			// LastArg/LastArgs storage
			if (trackableParams.Count == 1)
			{
				var param = trackableParams.GetArray()![0];
				w.Line($"private {param.Type} _lastArg = default!;");
			}
			else if (trackableParams.Count > 1)
			{
				// Builder field uses non-nullable tuple (nullable elements, but the tuple itself is not nullable)
				// This matches inline mode where builder LastArgs is non-nullable
				w.Line($"private {lastArgsType} _lastArgs;");
			}
			w.Line();

			// Constructor
			w.Line($"public {builderClassName}({interceptorClassName} interceptor) : base(interceptor)");
			using (w.Braces())
			{
				w.Line("_typedInterceptor = interceptor;");
			}
			w.Line();

			// LastArg/LastArgs property (non-nullable on builder, matching inline mode)
			if (trackableParams.Count == 1)
			{
				var param = trackableParams.GetArray()![0];
				w.Line($"public {param.Type} LastArg => _lastArg;");
			}
			else if (trackableParams.Count > 1)
			{
				w.Line($"public {lastArgsType} LastArgs => _lastArgs;");
			}

			// RecordArg method
			if (trackableParams.Count == 1)
			{
				var trackable = trackableParams.GetArray()![0];
				if (model.Parameters.Count == 1)
				{
					// Single param total: args IS the value
					w.Line($"public void RecordArg({tArgs} args) => _lastArg = args;");
				}
				else
				{
					// Multiple params (some out/ref): extract trackable field from tuple
					w.Line($"public void RecordArg({tArgs} args) => _lastArg = args.{trackable.EscapedName};");
				}
			}
			else if (trackableParams.Count > 1)
			{
				// Store as nullable tuple from TArgs
				var tupleParts = string.Join(", ", trackableParams.Select(p => $"args.{p.EscapedName}"));
				w.Line($"public void RecordArg({tArgs} args) => _lastArgs = ({tupleParts});");
			}
			w.Line();

			// Reset
			if (trackableParams.Count == 0)
				w.Line("public override void Reset() => base.Reset();");
			else if (trackableParams.Count == 1)
				w.Line("public override void Reset() { base.Reset(); _lastArg = default!; }");
			else
				w.Line("public override void Reset() { base.Reset(); _lastArgs = default; }");
			w.Line();

			// ThenReturn / ThenCall
			var thenChainName = model.IsVoid ? "ThenCall" : "ThenReturn";
			w.Line($"/// <summary>Elevates to sequence mode and adds another callback. Returns sequence for further chaining.</summary>");
			if (model.IsVoid)
			{
				w.Line($"public {sequenceClassName} {thenChainName}({delegateType} callback)");
				using (w.Braces())
				{
					w.Line("ThenCallBase(callback);");
					w.Line($"return new {sequenceClassName}(_typedInterceptor);");
				}
			}
			else
			{
				w.Line($"public {sequenceClassName} {thenChainName}({delegateType} callback)");
				using (w.Braces())
				{
					w.Line("ThenReturnCallbackBase(callback);");
					w.Line($"return new {sequenceClassName}(_typedInterceptor);");
				}
			}
			w.Line();

			// ThenReturn(value) for non-void (skip for ref/out and ref struct -- value overloads not supported)
			var hasRefOrOutInBuilder = model.Parameters.Any(p => p.RefKind == Microsoft.CodeAnalysis.RefKind.Ref || p.RefKind == Microsoft.CodeAnalysis.RefKind.Out);
			if (!model.IsVoid && !hasRefOrOutInBuilder && !model.HasRefStructParameter)
			{
				var (valueType, isTaskTBuilder, isValueTaskTBuilder) = GetAsyncTypeInfo(model.ReturnType);
				var isAsyncBuilder = isTaskTBuilder || isValueTaskTBuilder;
				var discardPrefix = BuildDiscardLambdaPrefix(model.Parameters.Count);
				w.Line($"/// <summary>Elevates to sequence mode and adds a value. Returns sequence for further chaining.</summary>");
				if (isTaskTBuilder)
					w.Line($"public {sequenceClassName} ThenReturn({valueType} value) => ThenReturn({discardPrefix} => global::System.Threading.Tasks.Task.FromResult(value));");
				else if (isValueTaskTBuilder)
					w.Line($"public {sequenceClassName} ThenReturn({valueType} value) => ThenReturn({discardPrefix} => new global::System.Threading.Tasks.ValueTask<{valueType}>(value));");
				else
					w.Line($"public {sequenceClassName} ThenReturn({valueType} value) => ThenReturn({discardPrefix} => value);");
				w.Line();

				// ThenReturn(params values)
				w.Line($"/// <summary>Adds multiple values to the sequence. Each value returned once.</summary>");
				w.Line($"public {sequenceClassName} ThenReturn(params {valueType}[] values)");
				using (w.Braces())
				{
					w.Line($"if (values.Length == 0) {{ ElevateToSequenceBase(); return new {sequenceClassName}(_typedInterceptor); }}");
					w.Line("var seq = ThenReturn(values[0]);");
					w.Line("for (int i = 1; i < values.Length; i++) seq.ThenReturn(values[i]);");
					w.Line("return seq;");
				}
				w.Line();
			}

			// Verifiable
			w.Line("/// <summary>Marks for verification by Stub.Verify().</summary>");
			w.Line($"public {builderClassName} Verifiable() {{ VerifiableBase(); return this; }}");
			w.Line("/// <summary>Marks for verification by Stub.Verify() with Called constraint.</summary>");
			w.Line($"public {builderClassName} Verifiable(global::KnockOff.Called times) {{ VerifiableBase(times); return this; }}");
			w.Line();

			// CreateNextBuilder / CreateNextReturnBuilder override
			if (model.IsVoid)
			{
				w.Line($"protected override MethodCallBuilderBase CreateNextBuilder() => new {builderClassName}(_typedInterceptor);");
			}
			else
			{
				w.Line($"protected override ReturnMethodCallBuilderBase CreateNextReturnBuilder() => new {builderClassName}(_typedInterceptor);");
			}
			w.Line();

			// Explicit interface implementations for IMethodTracking hierarchy
			w.Line("global::KnockOff.IMethodTracking global::KnockOff.IMethodTracking.Verifiable() => Verifiable();");
			w.Line("global::KnockOff.IMethodTracking global::KnockOff.IMethodTracking.Verifiable(global::KnockOff.Called times) => Verifiable(times);");
			if (trackableParams.Count == 1)
			{
				var param = trackableParams.GetArray()![0];
				w.Line($"global::KnockOff.IMethodTracking<{param.Type}> global::KnockOff.IMethodTracking<{param.Type}>.Verifiable() => Verifiable();");
				w.Line($"global::KnockOff.IMethodTracking<{param.Type}> global::KnockOff.IMethodTracking<{param.Type}>.Verifiable(global::KnockOff.Called times) => Verifiable(times);");
			}
			else if (trackableParams.Count > 1)
			{
				w.Line($"global::KnockOff.IMethodTrackingArgs<{lastArgsType}> global::KnockOff.IMethodTrackingArgs<{lastArgsType}>.Verifiable() => Verifiable();");
				w.Line($"global::KnockOff.IMethodTrackingArgs<{lastArgsType}> global::KnockOff.IMethodTrackingArgs<{lastArgsType}>.Verifiable(global::KnockOff.Called times) => Verifiable(times);");
			}

			// Explicit interface implementation for Verifiable on the builder interface (return type must match)
			w.Line($"{model.BuilderInterface} {model.BuilderInterface}.Verifiable() => Verifiable();");
			w.Line($"{model.BuilderInterface} {model.BuilderInterface}.Verifiable(global::KnockOff.Called times) => Verifiable(times);");

			// Explicit interface implementation for ThenReturn/ThenCall (interface requires sequence interface return)
			if (model.IsVoid)
			{
				w.Line($"global::KnockOff.IMethodCallSequence<{delegateType}> {model.BuilderInterface}.ThenCall({delegateType} callback) => ThenCall(callback);");
			}
			else
			{
				w.Line($"global::KnockOff.IMethodReturnSequence<{delegateType}> {model.BuilderInterface}.ThenReturn({delegateType} callback) => ThenReturn(callback);");
			}
		}
		w.Line();
	}

	private static void RenderBaseClassMethodSequenceImpl(
		CodeWriter w,
		UnifiedMethodInterceptorModel model,
		string interceptorClassName,
		string delegateType,
		string tArgs)
	{
		var thenChainName = model.IsVoid ? "ThenCall" : "ThenReturn";
		var builderClassName = model.BuilderFriendlyName ?? "MethodCallBuilderImpl";
		var sequenceClassName = model.SequenceFriendlyName ?? "MethodSequenceImpl";
		string baseClass;
		string sequenceInterface;
		if (model.IsVoid)
		{
			baseClass = "MethodSequenceBase";
			sequenceInterface = $"global::KnockOff.IMethodCallSequence<{delegateType}>";
		}
		else
		{
			baseClass = "ReturnMethodSequenceBase";
			sequenceInterface = $"global::KnockOff.IMethodReturnSequence<{delegateType}>";
		}

		w.Line($"/// <summary>Sequence implementation for {thenChainName} chaining.</summary>");
		w.Line($"public sealed class {sequenceClassName} : {baseClass}, {sequenceInterface}");
		using (w.Braces())
		{
			w.Line($"private readonly {interceptorClassName} _typedInterceptor;");
			w.Line();

			w.Line($"public {sequenceClassName}({interceptorClassName} interceptor) : base(interceptor)");
			using (w.Braces())
			{
				w.Line("_typedInterceptor = interceptor;");
			}
			w.Line();

			// ThenReturn / ThenCall
			w.Line($"/// <summary>Adds another callback to the sequence. Each callback runs exactly once.</summary>");
			if (model.IsVoid)
			{
				w.Line($"public {sequenceClassName} ThenCall({delegateType} callback)");
				using (w.Braces())
				{
					w.Line($"var tracking = new {builderClassName}(_typedInterceptor);");
					w.Line("AddToSequence(callback, tracking);");
					w.Line("return this;");
				}
			}
			else
			{
				w.Line($"public {sequenceClassName} ThenReturn({delegateType} callback) {{ ThenReturnCallbackBase(callback); return this; }}");
			}
			w.Line();

			// ThenReturn(value) for non-void (skip for ref/out and ref struct methods)
			var hasRefOrOutInSeq = model.Parameters.Any(p => p.RefKind == Microsoft.CodeAnalysis.RefKind.Ref || p.RefKind == Microsoft.CodeAnalysis.RefKind.Out);
			if (!model.IsVoid && !hasRefOrOutInSeq && !model.HasRefStructParameter)
			{
				var (valueType, isTaskTSeq, isValueTaskTSeq) = GetAsyncTypeInfo(model.ReturnType);
				var discardPrefix = BuildDiscardLambdaPrefix(model.Parameters.Count);
				w.Line($"/// <summary>Adds a value to the sequence. The value is returned exactly once.</summary>");
				if (isTaskTSeq)
					w.Line($"public {sequenceClassName} ThenReturn({valueType} value) => ThenReturn({discardPrefix} => global::System.Threading.Tasks.Task.FromResult(value));");
				else if (isValueTaskTSeq)
					w.Line($"public {sequenceClassName} ThenReturn({valueType} value) => ThenReturn({discardPrefix} => new global::System.Threading.Tasks.ValueTask<{valueType}>(value));");
				else
					w.Line($"public {sequenceClassName} ThenReturn({valueType} value) => ThenReturn({discardPrefix} => value);");
				w.Line();

				// ThenReturn(params values)
				w.Line($"/// <summary>Adds multiple values to the sequence. Each value returned once.</summary>");
				w.Line($"public {sequenceClassName} ThenReturn(params {valueType}[] values)");
				using (w.Braces())
				{
					w.Line("foreach (var value in values) ThenReturn(value);");
					w.Line("return this;");
				}
				w.Line();
			}

			// Verifiable
			w.Line("/// <summary>Marks for verification by Stub.Verify().</summary>");
			w.Line($"public {sequenceClassName} Verifiable() {{ VerifiableBase(); return this; }}");
			w.Line();

			// CreateNextBuilder / CreateNextReturnBuilder override
			if (model.IsVoid)
			{
				w.Line($"protected override MethodCallBuilderBase CreateNextBuilder() => new {builderClassName}(_typedInterceptor);");
			}
			else
			{
				w.Line($"protected override ReturnMethodCallBuilderBase CreateNextReturnBuilder() => new {builderClassName}(_typedInterceptor);");
			}
			w.Line();

			// Explicit interface implementations for sequence interface
			if (model.IsVoid)
			{
				w.Line($"global::KnockOff.IMethodCallSequence<{delegateType}> global::KnockOff.IMethodCallSequence<{delegateType}>.ThenCall({delegateType} callback) => ThenCall(callback);");
				w.Line($"global::KnockOff.IMethodCallSequence<{delegateType}> global::KnockOff.IMethodCallSequence<{delegateType}>.Verifiable() => Verifiable();");
			}
			else
			{
				w.Line($"global::KnockOff.IMethodReturnSequence<{delegateType}> global::KnockOff.IMethodReturnSequence<{delegateType}>.ThenReturn({delegateType} callback) => ThenReturn(callback);");
				w.Line($"global::KnockOff.IMethodReturnSequence<{delegateType}> global::KnockOff.IMethodReturnSequence<{delegateType}>.Verifiable() => Verifiable();");
			}
			w.Line("global::KnockOff.IMethodSequence global::KnockOff.IMethodSequence.Verifiable() => Verifiable();");
		}
		w.Line();
	}

	// --- Thin WhenBuilder/WhenChain/VoidWhenChain (base class mode) ---

	/// <summary>
	/// Renders concrete WhenMatcherBase subclasses for non-void When chains.
	/// WhenMatcherPredicateValueBase: predicate-based matcher with stored return value.
	/// WhenMatcherTerminalCallbackBase: terminal always-matching callback matcher.
	/// WhenMatcherNoneBase: terminal no-op matcher for ThenNone().
	/// </summary>
	private static void RenderBaseClassNonVoidWhenMatcherClasses(
		CodeWriter w,
		string tArgs,
		EquatableArray<ParameterModel> parameters,
		string returnType)
	{
		// WhenMatcherPredicateValueBase - predicate matcher with stored return value
		w.Line("/// <summary>Predicate-based When matcher that returns a stored value.</summary>");
		w.Line($"private sealed class WhenMatcherPredicateValueBase : WhenMatcherBase");
		using (w.Braces())
		{
			w.Line($"private readonly global::System.Func<{tArgs}, bool> _predicate;");
			w.Line("private readonly object? _value;");
			w.Line();
			w.Line($"public WhenMatcherPredicateValueBase(global::System.Func<{tArgs}, bool> predicate, object? value) {{ _predicate = predicate; _value = value; }}");
			w.Line();
			// Matches
			if (parameters.Count == 0)
				w.Line("public override bool Matches(object? args) => _predicate(default);");
			else
				w.Line($"public override bool Matches(object? args) => _predicate(({tArgs})args!);");
			// Execute (void - no-op for non-void matchers)
			w.Line("public override void Execute(object? args) { }");
			// ExecuteReturn
			w.Line("public override object? ExecuteReturn(object? args) => _value;");
			w.Line("public override bool IsTerminal => false;");
		}
		w.Line();

		// WhenMatcherTerminalCallbackBase - terminal always-matching callback matcher
		// Uses Func<object?, object?> to store a boxed-args-to-boxed-result wrapper
		w.Line("/// <summary>Terminal always-matching callback matcher for non-void When chains.</summary>");
		w.Line($"private sealed class WhenMatcherTerminalCallbackBase : WhenMatcherBase");
		using (w.Braces())
		{
			w.Line($"private readonly global::System.Func<object?, object?> _callback;");
			w.Line();
			w.Line($"public WhenMatcherTerminalCallbackBase(global::System.Func<object?, object?> callback) {{ _callback = callback; }}");
			w.Line();
			w.Line("public override bool Matches(object? args) => true;");
			w.Line("public override void Execute(object? args) { }");
			w.Line("public override object? ExecuteReturn(object? args) => _callback(args);");
			w.Line("public override bool IsTerminal => true;");
		}
		w.Line();

		// WhenMatcherNoneBase - terminal no-op matcher for ThenNone()
		w.Line("/// <summary>Terminal no-op matcher for ThenNone() on non-void When chains.</summary>");
		w.Line($"private sealed class WhenMatcherNoneBase : WhenMatcherBase");
		using (w.Braces())
		{
			w.Line("public override bool Matches(object? args) => false;");
			w.Line("public override void Execute(object? args) { }");
			w.Line("public override object? ExecuteReturn(object? args) => null;");
			w.Line("public override bool IsTerminal => true;");
		}
		w.Line();
	}

	private static void RenderBaseClassWhenBuilder(
		CodeWriter w,
		string interceptorClassName,
		EquatableArray<ParameterModel> parameters,
		string returnType,
		string tArgs)
	{
		// Check if this is an async method (Task<T> or ValueTask<T>)
		var (innerType, isTaskT, isValueTaskT) = GetAsyncTypeInfo(returnType);
		var isAsync = isTaskT || isValueTaskT;

		w.Line($"/// <summary>Builder for When matchers. Captures predicate, awaits Return(value).</summary>");
		w.Line($"public sealed class WhenBuilder : WhenBuilderBase");
		using (w.Braces())
		{
			// Store typed predicate as a field (not passed to base)
			w.Line($"private readonly global::System.Func<{tArgs}, bool> _predicate;");
			w.Line();
			w.Line($"public WhenBuilder({interceptorClassName} interceptor, global::System.Func<{tArgs}, bool> predicate) : base(interceptor)");
			using (w.Braces())
			{
				w.Line("_predicate = predicate;");
			}
			w.Line();

			// Return(value) - creates a WhenMatcherPredicateValueBase and calls AddValueMatcher
			if (isAsync)
			{
				// Async: Return accepts unwrapped type and wraps
				w.Line($"/// <summary>Configures the return value. Auto-wrapped in {(isTaskT ? "Task.FromResult" : "new ValueTask")}.</summary>");
				w.Line($"public WhenChain Return({innerType} value)");
				using (w.Braces())
				{
					if (isTaskT)
						w.Line($"AddValueMatcher(new WhenMatcherPredicateValueBase(_predicate, global::System.Threading.Tasks.Task.FromResult(value)));");
					else
						w.Line($"AddValueMatcher(new WhenMatcherPredicateValueBase(_predicate, new global::System.Threading.Tasks.ValueTask<{innerType}>(value)));");
					w.Line($"return new WhenChain(({interceptorClassName})_interceptor);");
				}
			}
			else
			{
				w.Line($"/// <summary>Configures the return value when predicate matches.</summary>");
				w.Line($"public WhenChain Return({returnType} value)");
				using (w.Braces())
				{
					w.Line("AddValueMatcher(new WhenMatcherPredicateValueBase(_predicate, value));");
					w.Line($"return new WhenChain(({interceptorClassName})_interceptor);");
				}
			}
		}
		w.Line();
	}

	private static void RenderBaseClassWhenChain(
		CodeWriter w,
		string interceptorClassName,
		EquatableArray<ParameterModel> parameters,
		string returnType,
		string delegateType,
		string tArgs,
		string? predicateFriendlyName = null)
	{
		var paramTypeList = BuildParamTypeList(parameters);

		w.Line($"/// <summary>When chain implementation with ThenWhen, ThenCall, ThenNone, verification support.</summary>");
		w.Line($"public sealed class WhenChain : WhenChainBase");
		using (w.Braces())
		{
			w.Line($"private readonly {interceptorClassName} _typedInterceptor;");
			w.Line();
			w.Line($"public WhenChain({interceptorClassName} interceptor) : base(interceptor)");
			using (w.Braces())
			{
				w.Line("_typedInterceptor = interceptor;");
			}
			w.Line();

			// ThenWhen with values
			if (parameters.Count > 0)
			{
				w.Line($"/// <summary>Adds another matcher with exact value matching.</summary>");
				w.Line($"public WhenBuilder ThenWhen({paramTypeList})");
				using (w.Braces())
				{
					if (parameters.Count == 1)
					{
						var p = parameters.GetArray()![0];
						w.Line($"return new WhenBuilder(_typedInterceptor, (_arg0) => global::System.Object.Equals(_arg0, {p.EscapedName}));");
					}
					else
					{
						var predicateBody = string.Join(" && ", parameters.Select(p => $"global::System.Object.Equals(args.{p.EscapedName}, {p.EscapedName})"));
						w.Line($"return new WhenBuilder(_typedInterceptor, (args) => {predicateBody});");
					}
				}
				w.Line();

				// ThenWhen with predicate - custom predicate delegate for 2+ params
				{
					var predicateType = BuildPredicateType(parameters, predicateFriendlyName);
					w.Line($"/// <summary>Adds another matcher with predicate matching.</summary>");
					if (parameters.Count >= 2 && predicateFriendlyName != null)
					{
						// Bridge custom predicate delegate to internal tuple-based predicate
						var unpackedArgs = string.Join(", ", parameters.Select(p => $"args.{p.EscapedName}"));
						w.Line($"public WhenBuilder ThenWhen({predicateType} predicate) => new WhenBuilder(_typedInterceptor, (args) => predicate({unpackedArgs}));");
					}
					else
					{
						w.Line($"public WhenBuilder ThenWhen({predicateType} predicate) => new WhenBuilder(_typedInterceptor, predicate);");
					}
					w.Line();
				}
			}

			// ThenCall - terminal with callback (creates WhenMatcherTerminalCallbackBase)
			// The callback (delegateType) returns a typed value; we wrap it in Func<object?, object?> for the matcher
			w.Line($"/// <summary>Adds an unconditional callback as terminal matcher.</summary>");
			w.Line($"public WhenChain ThenCall({delegateType} callback)");
			using (w.Braces())
			{
				if (parameters.Count == 0)
				{
					w.Line("AddTerminalCallbackMatcher(new WhenMatcherTerminalCallbackBase((object? args) => (object?)callback()));");
				}
				else if (parameters.Count == 1)
				{
					var p = parameters.GetArray()![0];
					w.Line($"AddTerminalCallbackMatcher(new WhenMatcherTerminalCallbackBase((object? args) => (object?)callback(({p.Type})args!)));");
				}
				else
				{
					// 2+ params with individual-param delegate: unpack tuple fields
					var unpackedArgs = string.Join(", ", parameters.Select(p => $"typedArgs.{p.EscapedName}"));
					w.Line($"AddTerminalCallbackMatcher(new WhenMatcherTerminalCallbackBase((object? args) => {{ var typedArgs = ({tArgs})args!; return (object?)callback({unpackedArgs}); }}));");
				}
				w.Line("return this;");
			}
			w.Line();

			// ThenNone - terminal no-op matcher
			w.Line($"/// <summary>Closes chain with no matcher.</summary>");
			w.Line("public WhenChain ThenNone() { AddNoneMatcher(new WhenMatcherNoneBase()); return this; }");
			w.Line();

			// Verifiable
			w.Line("/// <summary>Marks this When chain for verification by Stub.Verify().</summary>");
			w.Line("public WhenChain Verifiable() { VerifiableBase(); return this; }");
		}
		w.Line();
	}

	/// <summary>
	/// Renders concrete WhenMatcherBase subclasses for void When chains.
	/// VoidWhenMatcherPredicateBase: predicate-based matcher with optional callback.
	/// VoidWhenMatcherCallBase: terminal always-matching callback matcher.
	/// </summary>
	private static void RenderBaseClassVoidWhenMatcherClasses(CodeWriter w, string tArgs, EquatableArray<ParameterModel> parameters)
	{
		// VoidWhenMatcherPredicateBase - predicate matcher for void methods
		w.Line("/// <summary>Predicate-based When matcher for void methods.</summary>");
		w.Line($"private sealed class VoidWhenMatcherPredicateBase : WhenMatcherBase");
		using (w.Braces())
		{
			w.Line($"private readonly global::System.Func<{tArgs}, bool> _predicate;");
			w.Line($"private global::System.Action<{tArgs}>? _callback;");
			w.Line();
			w.Line($"public VoidWhenMatcherPredicateBase(global::System.Func<{tArgs}, bool> predicate) {{ _predicate = predicate; }}");
			w.Line();
			w.Line($"public void SetCallback(global::System.Action<{tArgs}> callback) {{ _callback = callback; }}");
			w.Line();
			// Matches: cast args to TArgs, invoke predicate
			if (parameters.Count == 0)
				w.Line("public override bool Matches(object? args) => _predicate(default);");
			else if (parameters.Count == 1)
				w.Line($"public override bool Matches(object? args) => _predicate(({tArgs})args!);");
			else
				w.Line($"public override bool Matches(object? args) => _predicate(({tArgs})args!);");
			// Execute: invoke callback if set
			if (parameters.Count == 0)
				w.Line("public override void Execute(object? args) { _callback?.Invoke(default); }");
			else
				w.Line($"public override void Execute(object? args) {{ _callback?.Invoke(({tArgs})args!); }}");
			// ExecuteReturn: void matcher, return null
			w.Line("public override object? ExecuteReturn(object? args) { Execute(args); return null; }");
			w.Line("public override bool IsTerminal => false;");
		}
		w.Line();

		// VoidWhenMatcherCallBase - terminal always-matching callback matcher
		w.Line("/// <summary>Terminal always-matching callback matcher for void methods.</summary>");
		w.Line($"private sealed class VoidWhenMatcherCallBase : WhenMatcherBase");
		using (w.Braces())
		{
			w.Line($"private readonly global::System.Action<{tArgs}> _callback;");
			w.Line();
			w.Line($"public VoidWhenMatcherCallBase(global::System.Action<{tArgs}> callback) {{ _callback = callback; }}");
			w.Line();
			w.Line("public override bool Matches(object? args) => true;");
			if (parameters.Count == 0)
				w.Line("public override void Execute(object? args) { _callback(default); }");
			else
				w.Line($"public override void Execute(object? args) {{ _callback(({tArgs})args!); }}");
			w.Line("public override object? ExecuteReturn(object? args) { Execute(args); return null; }");
			w.Line("public override bool IsTerminal => true;");
		}
		w.Line();

		// VoidWhenMatcherNoneBase - terminal no-op matcher for ThenNone()
		w.Line("/// <summary>Terminal no-op matcher for ThenNone() on void When chains.</summary>");
		w.Line($"private sealed class VoidWhenMatcherNoneBase : WhenMatcherBase");
		using (w.Braces())
		{
			w.Line("public override bool Matches(object? args) => false;");
			w.Line("public override void Execute(object? args) { }");
			w.Line("public override object? ExecuteReturn(object? args) => null;");
			w.Line("public override bool IsTerminal => true;");
		}
		w.Line();
	}

	private static void RenderBaseClassVoidWhenChain(
		CodeWriter w,
		string interceptorClassName,
		EquatableArray<ParameterModel> parameters,
		string delegateType,
		string tArgs,
		string? predicateFriendlyName = null)
	{
		var paramTypeList = BuildParamTypeList(parameters);

		w.Line($"/// <summary>Void When chain implementation with Call, ThenWhen, ThenCall, ThenNone, verification support.</summary>");
		w.Line($"public sealed class VoidWhenChain : VoidWhenChainBase");
		using (w.Braces())
		{
			w.Line($"private readonly {interceptorClassName} _typedInterceptor;");
			w.Line("private readonly VoidWhenMatcherPredicateBase? _typedMatcher;");
			w.Line();

			w.Line($"public VoidWhenChain({interceptorClassName} interceptor, WhenMatcherBase matcher) : base(interceptor, matcher)");
			using (w.Braces())
			{
				w.Line("_typedInterceptor = interceptor;");
				w.Line("_typedMatcher = matcher as VoidWhenMatcherPredicateBase;");
			}
			w.Line();

			// Call - sets callback on current matcher
			// All params: bridge custom delegate to internal Action<TArgs> via lambda
			w.Line($"/// <summary>Sets an optional callback to invoke when this matcher matches.</summary>");
			w.Line($"public VoidWhenChain Call({delegateType} callback)");
			using (w.Braces())
			{
				if (parameters.Count == 0)
				{
					w.Line("_typedMatcher?.SetCallback((_) => callback());");
				}
				else if (parameters.Count == 1)
				{
					// For 1 param: bridge custom delegate to Action<T> via lambda
					var p = parameters.GetArray()![0];
					w.Line($"_typedMatcher?.SetCallback((_arg) => callback(_arg));");
				}
				else
				{
					// For 2+ params: bridge custom delegate to Action<tuple> via lambda
					var unpackedCallParams = string.Join(", ", parameters.Select(p => $"args.{p.EscapedName}"));
					w.Line($"_typedMatcher?.SetCallback((args) => callback({unpackedCallParams}));");
				}
				w.Line("return this;");
			}
			w.Line();

			// ThenWhen with values
			if (parameters.Count > 0)
			{
				w.Line($"/// <summary>Adds another matcher with exact value matching.</summary>");
				w.Line($"public VoidWhenChain ThenWhen({paramTypeList})");
				using (w.Braces())
				{
					w.Line("_typedInterceptor._whenChain ??= new global::System.Collections.Generic.List<WhenMatcherBase>();");
					if (parameters.Count == 1)
					{
						var p = parameters.GetArray()![0];
						w.Line($"var matcher = new VoidWhenMatcherPredicateBase((_arg0) => global::System.Object.Equals(_arg0, {p.EscapedName}));");
					}
					else
					{
						var predicateBody = string.Join(" && ", parameters.Select(p => $"global::System.Object.Equals(args.{p.EscapedName}, {p.EscapedName})"));
						w.Line($"var matcher = new VoidWhenMatcherPredicateBase((args) => {predicateBody});");
					}
					w.Line("_typedInterceptor._whenChain.Add(matcher);");
					w.Line("return new VoidWhenChain(_typedInterceptor, matcher);");
				}
				w.Line();

				// ThenWhen with predicate - custom predicate delegate for 2+ params
				{
					var predicateType = BuildPredicateType(parameters, predicateFriendlyName);
					w.Line($"/// <summary>Adds another matcher with predicate matching.</summary>");
					w.Line($"public VoidWhenChain ThenWhen({predicateType} predicate)");
					using (w.Braces())
					{
						w.Line("_typedInterceptor._whenChain ??= new global::System.Collections.Generic.List<WhenMatcherBase>();");
						if (parameters.Count >= 2 && predicateFriendlyName != null)
						{
							// Bridge custom predicate delegate to tuple-based internal predicate
							var unpackedArgs = string.Join(", ", parameters.Select(p => $"args.{p.EscapedName}"));
							w.Line($"var matcher = new VoidWhenMatcherPredicateBase((args) => predicate({unpackedArgs}));");
						}
						else
						{
							w.Line("var matcher = new VoidWhenMatcherPredicateBase(predicate);");
						}
						w.Line("_typedInterceptor._whenChain.Add(matcher);");
						w.Line("return new VoidWhenChain(_typedInterceptor, matcher);");
					}
					w.Line();
				}
			}

			// ThenCall - terminal with callback
			// All params: bridge custom delegate to internal Action<TArgs> via lambda
			w.Line($"/// <summary>Adds an unconditional callback as terminal matcher.</summary>");
			w.Line($"public VoidWhenChain ThenCall({delegateType} callback)");
			using (w.Braces())
			{
				w.Line("_typedInterceptor._whenChain ??= new global::System.Collections.Generic.List<WhenMatcherBase>();");
				if (parameters.Count == 0)
				{
					w.Line("_typedInterceptor._whenChain.Add(new VoidWhenMatcherCallBase((_) => callback()));");
				}
				else if (parameters.Count == 1)
				{
					// For 1 param: bridge custom delegate to Action<T> via lambda
					w.Line("_typedInterceptor._whenChain.Add(new VoidWhenMatcherCallBase((_arg) => callback(_arg)));");
				}
				else
				{
					// For 2+ params: bridge custom delegate to Action<tuple> via lambda
					var unpackedThenCallParams = string.Join(", ", parameters.Select(p => $"args.{p.EscapedName}"));
					w.Line($"_typedInterceptor._whenChain.Add(new VoidWhenMatcherCallBase((args) => callback({unpackedThenCallParams})));");
				}
				w.Line("return this;");
			}
			w.Line();

			// ThenNone - terminal no-op matcher for void methods
			w.Line("/// <summary>Closes chain with no matcher.</summary>");
			w.Line("public VoidWhenChain ThenNone() { AddTerminalMatcher(new VoidWhenMatcherNoneBase()); return this; }");
			w.Line();

			// Verifiable
			w.Line("/// <summary>Marks this When chain for verification by Stub.Verify().</summary>");
			w.Line("public VoidWhenChain Verifiable() { VerifiableBase(); return this; }");
		}
		w.Line();
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
			w.Line($"/// <summary>Source object to delegate to when no callback is configured.</summary>");
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
			var ovlBuilderName = overload.BuilderFriendlyName ?? $"MethodCallBuilderImpl_{overload.SignatureSuffix}";

			// Delegate declaration (always generated for all overloads)
			if (overload.DelegateSignature != null)
			{
				var ovlDelegateSig = FormatMethodSignatureForDoc(model.MethodName, overload.Parameters, overload.ReturnType, overload.IsVoid);
				w.Line($"/// <summary>Callback delegate for {ovlDelegateSig}.</summary>");
				w.Line(overload.DelegateSignature);
				w.Line();
			}

			// Predicate delegate declaration (for 2+ params, used by When chains)
			if (overload.PredicateDelegateSignature != null)
			{
				var ovlPredicateSig = FormatMethodSignatureForDoc(model.MethodName, overload.Parameters, overload.ReturnType, overload.IsVoid);
				w.Line($"/// <summary>Predicate delegate for {ovlPredicateSig}.</summary>");
				w.Line(overload.PredicateDelegateSignature);
				w.Line();
			}

			// Callback storage
			w.Line($"private {overload.DelegateName}? _call_{overload.SignatureSuffix};");
			w.Line($"private {ovlBuilderName}? _callTracking_{overload.SignatureSuffix};");
			w.Line();

			// Simplified callback storage for Task<T>/ValueTask<T> overloads
			var hasRefOrOut = HasRefOrOutParameters(overload.Parameters);
			var (innerType, isTaskT, isValueTaskT) = GetAsyncTypeInfo(overload.ReturnType);
			var isAsyncWithInnerType = isTaskT || isValueTaskT;
			if (isAsyncWithInnerType && !hasRefOrOut)
			{
				var simplifiedDelegateType = BuildSimplifiedDelegateType(overload.Parameters, innerType);
				w.Line($"private {simplifiedDelegateType}? _callSimplified_{overload.SignatureSuffix};");
				w.Line($"private {ovlBuilderName}? _callSimplifiedTracking_{overload.SignatureSuffix};");
				w.Line();
			}

			// Simplified void callback storage for Task/ValueTask overloads
			var (isVoidTask, isVoidValueTask) = GetVoidAsyncInfo(overload.ReturnType);
			var isVoidAsync = isVoidTask || isVoidValueTask;
			if (isVoidAsync && !hasRefOrOut)
			{
				var voidDelegateType = BuildSimplifiedVoidDelegateType(overload.Parameters);
				w.Line($"private {voidDelegateType}? _callSimplifiedVoid_{overload.SignatureSuffix};");
				w.Line($"private {ovlBuilderName}? _callSimplifiedVoidTracking_{overload.SignatureSuffix};");
				w.Line();
			}

			// Value storage for Return(value) overload (skip for void/ref/out overloads)
			var canHaveValueOverloadForThis = !overload.IsVoid && !hasRefOrOut;
			if (canHaveValueOverloadForThis)
			{
				var (valueStorageType, _, _) = GetAsyncTypeInfo(overload.ReturnType);
				w.Line($"private {valueStorageType} _returnValue_{overload.SignatureSuffix} = default!;");
				w.Line($"private bool _hasReturnValue_{overload.SignatureSuffix};");
				w.Line($"private {ovlBuilderName}? _returnValueTracking_{overload.SignatureSuffix};");
				w.Line();
			}

			// Sequence storage
			w.Line($"private global::System.Collections.Generic.List<({overload.DelegateName} Callback, {ovlBuilderName} Tracking)>? _sequence_{overload.SignatureSuffix};");
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
			w.Line($"private global::KnockOff.Called? _verifiableTimes_{overload.SignatureSuffix};");
			w.Line();
		}

		// Backward compatibility: aggregate tracking properties across all overloads
		RenderOverloadBackwardCompatibleProperties(w, model.Overloads);
		w.Line();

		// Verify() methods for direct interceptor verification
		// Skip Verifiable() for overload groups - they have per-signature verifiable fields
		RenderInterceptorVerifyMethods(w, model.MethodName, isOverloadGroup: true);

		// Call/Return overloads for each unique signature
		foreach (var overload in model.Overloads)
		{
			var ovlBuilderName = overload.BuilderFriendlyName ?? $"MethodCallBuilderImpl_{overload.SignatureSuffix}";
			// Determine async characteristics for this overload
			var hasRefOrOut = HasRefOrOutParameters(overload.Parameters);
			var (innerType, isTaskT, isValueTaskT) = GetAsyncTypeInfo(overload.ReturnType);
			var isAsyncWithInnerType = isTaskT || isValueTaskT;
			var (isVoidTask, isVoidValueTask) = GetVoidAsyncInfo(overload.ReturnType);
			var isVoidAsync = isVoidTask || isVoidValueTask;

			// Call - repeating callback (always "Call" for overloads, regardless of void/non-void)
			var canHaveValueOverloadForThis = !overload.IsVoid && !hasRefOrOut;
			EmitCallXmlDoc(w, model.MethodName, overload.Parameters, overload.XmlDocSummary, overload.ReturnType, overload.IsVoid, "Returns builder for sequence chaining.");
			w.Line($"public {ovlBuilderName} Call({overload.DelegateName} callback)");
			using (w.Braces())
			{
				w.Line($"_sequence_{overload.SignatureSuffix} = null;");
				w.Line($"_sequenceIndex_{overload.SignatureSuffix} = 0;");
				w.Line($"_isVerifiable_{overload.SignatureSuffix} = false;");
				w.Line($"_verifiableTimes_{overload.SignatureSuffix} = null;");
				// Clear value storage (Call replaces Return(value))
				if (canHaveValueOverloadForThis)
				{
					w.Line($"_hasReturnValue_{overload.SignatureSuffix} = false;");
					w.Line($"_returnValue_{overload.SignatureSuffix} = default!;");
					w.Line($"_returnValueTracking_{overload.SignatureSuffix} = null;");
				}
				// Clear simplified callback storage (mutual exclusivity)
				if (isAsyncWithInnerType && !hasRefOrOut)
				{
					w.Line($"_callSimplified_{overload.SignatureSuffix} = null;");
					w.Line($"_callSimplifiedTracking_{overload.SignatureSuffix} = null;");
				}
				if (isVoidAsync && !hasRefOrOut)
				{
					w.Line($"_callSimplifiedVoid_{overload.SignatureSuffix} = null;");
					w.Line($"_callSimplifiedVoidTracking_{overload.SignatureSuffix} = null;");
				}
				w.Line($"_call_{overload.SignatureSuffix} = callback;");
				w.Line($"_callTracking_{overload.SignatureSuffix} = new {ovlBuilderName}(this);");
				w.Line($"return _callTracking_{overload.SignatureSuffix};");
			}
			w.Line();

			// Call(Func<..., TInnerType>) - simplified callback for Task<T>/ValueTask<T> overloads
			if (isAsyncWithInnerType && !hasRefOrOut)
			{
				var simplifiedDelegateType = BuildSimplifiedDelegateType(overload.Parameters, innerType);
				EmitCallXmlDoc(w, model.MethodName, overload.Parameters, overload.XmlDocSummary, innerType, false, $"Result auto-wrapped in {(isTaskT ? "Task.FromResult" : "new ValueTask")}.");
				w.Line($"public {ovlBuilderName} Call({simplifiedDelegateType} callback)");
				using (w.Braces())
				{
					w.Line($"_sequence_{overload.SignatureSuffix} = null;");
					w.Line($"_sequenceIndex_{overload.SignatureSuffix} = 0;");
					w.Line($"_isVerifiable_{overload.SignatureSuffix} = false;");
					w.Line($"_verifiableTimes_{overload.SignatureSuffix} = null;");
					// Clear async callback storage (mutual exclusivity)
					w.Line($"_call_{overload.SignatureSuffix} = null;");
					w.Line($"_callTracking_{overload.SignatureSuffix} = null;");
					// Set simplified callback storage
					w.Line($"_callSimplified_{overload.SignatureSuffix} = callback;");
					w.Line($"_callSimplifiedTracking_{overload.SignatureSuffix} = new {ovlBuilderName}(this);");
					w.Line($"return _callSimplifiedTracking_{overload.SignatureSuffix};");
				}
				w.Line();
			}

			// Call(Action<...>) - simplified void callback for Task/ValueTask overloads
			if (isVoidAsync && !hasRefOrOut)
			{
				var voidDelegateType = BuildSimplifiedVoidDelegateType(overload.Parameters);
				EmitCallXmlDoc(w, model.MethodName, overload.Parameters, overload.XmlDocSummary, overload.ReturnType, true, $"{(isVoidTask ? "Task.CompletedTask" : "default(ValueTask)")} auto-returned.");
				w.Line($"public {ovlBuilderName} Call({voidDelegateType} callback)");
				using (w.Braces())
				{
					w.Line($"_sequence_{overload.SignatureSuffix} = null;");
					w.Line($"_sequenceIndex_{overload.SignatureSuffix} = 0;");
					w.Line($"_isVerifiable_{overload.SignatureSuffix} = false;");
					w.Line($"_verifiableTimes_{overload.SignatureSuffix} = null;");
					// Clear async callback storage (mutual exclusivity)
					w.Line($"_call_{overload.SignatureSuffix} = null;");
					w.Line($"_callTracking_{overload.SignatureSuffix} = null;");
					// Set simplified void callback storage
					w.Line($"_callSimplifiedVoid_{overload.SignatureSuffix} = callback;");
					w.Line($"_callSimplifiedVoidTracking_{overload.SignatureSuffix} = new {ovlBuilderName}(this);");
					w.Line($"return _callSimplifiedVoidTracking_{overload.SignatureSuffix};");
				}
				w.Line();
			}

		}

		// Return(value) methods for non-void overloads.
		// Only generated when the return type is unique among non-void overloads (otherwise ambiguous).
		// Normalize types by stripping trailing '?' for reference type nullability
		// (C# treats T and T? as the same method signature for reference types).
		var returnTypeOccurrences = new Dictionary<string, int>();
		foreach (var overload in model.Overloads)
		{
			if (!overload.IsVoid && !HasRefOrOutParameters(overload.Parameters))
			{
				var (valueType, _, _) = GetAsyncTypeInfo(overload.ReturnType);
				var normalizedType = valueType.TrimEnd('?');
				returnTypeOccurrences.TryGetValue(normalizedType, out var count);
				returnTypeOccurrences[normalizedType] = count + 1;
			}
		}
		foreach (var overload in model.Overloads)
		{
			var ovlBuilderName = overload.BuilderFriendlyName ?? $"MethodCallBuilderImpl_{overload.SignatureSuffix}";
			var hasRefOrOut = HasRefOrOutParameters(overload.Parameters);
			if (overload.IsVoid || hasRefOrOut) continue;

			var (valueStorageType, valIsTaskT, valIsValueTaskT) = GetAsyncTypeInfo(overload.ReturnType);
			// Skip if return type is not unique (ambiguous overload resolution)
			var normalizedStorageType = valueStorageType.TrimEnd('?');
			if (returnTypeOccurrences.TryGetValue(normalizedStorageType, out var occurrences) && occurrences > 1)
				continue;

			EmitReturnXmlDoc(w, model.MethodName, overload.Parameters, overload.XmlDocSummary, overload.ReturnType, overload.IsVoid, "Returns builder for sequence chaining.");
			w.Line($"public {ovlBuilderName} Return({valueStorageType} value)");
			using (w.Braces())
			{
				w.Line($"_sequence_{overload.SignatureSuffix} = null;");
				w.Line($"_sequenceIndex_{overload.SignatureSuffix} = 0;");
				w.Line($"_isVerifiable_{overload.SignatureSuffix} = false;");
				w.Line($"_verifiableTimes_{overload.SignatureSuffix} = null;");
				// Clear callback storage (Return(value) replaces Call(callback))
				w.Line($"_call_{overload.SignatureSuffix} = null;");
				w.Line($"_callTracking_{overload.SignatureSuffix} = null;");
				// Clear simplified callback storage
				var (_, rvIsTaskT, rvIsValueTaskT) = GetAsyncTypeInfo(overload.ReturnType);
				var rvIsAsyncWithInnerType = rvIsTaskT || rvIsValueTaskT;
				if (rvIsAsyncWithInnerType && !hasRefOrOut)
				{
					w.Line($"_callSimplified_{overload.SignatureSuffix} = null;");
					w.Line($"_callSimplifiedTracking_{overload.SignatureSuffix} = null;");
				}
				var (rvIsVoidTask, rvIsVoidValueTask) = GetVoidAsyncInfo(overload.ReturnType);
				if ((rvIsVoidTask || rvIsVoidValueTask) && !hasRefOrOut)
				{
					w.Line($"_callSimplifiedVoid_{overload.SignatureSuffix} = null;");
					w.Line($"_callSimplifiedVoidTracking_{overload.SignatureSuffix} = null;");
				}
				// Set value storage
				w.Line($"_hasReturnValue_{overload.SignatureSuffix} = true;");
				if (valIsTaskT)
					w.Line($"_returnValue_{overload.SignatureSuffix} = global::System.Threading.Tasks.Task.FromResult(value);");
				else if (valIsValueTaskT)
					w.Line($"_returnValue_{overload.SignatureSuffix} = new global::System.Threading.Tasks.ValueTask<{valueStorageType}>(value);");
				else
					w.Line($"_returnValue_{overload.SignatureSuffix} = value;");
				w.Line($"_returnValueTracking_{overload.SignatureSuffix} = new {ovlBuilderName}(this);");
				w.Line($"return _returnValueTracking_{overload.SignatureSuffix};");
			}
			w.Line();
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
				RenderWhenEntryPoints(w, fullInterceptorClassName, overload.Parameters, overload.ReturnType, overload.DelegateName, overload.SignatureSuffix, returnTypeSuffix, methodName: model.MethodName, xmlDocSummary: overload.XmlDocSummary, predicateFriendlyName: overload.PredicateFriendlyName);
			}
			if (canHaveVoidWhenChain)
			{
				RenderVoidWhenEntryPoints(w, fullInterceptorClassName, overload.Parameters, overload.DelegateName, overload.SignatureSuffix, returnTypeSuffix, methodName: model.MethodName, xmlDocSummary: overload.XmlDocSummary, predicateFriendlyName: overload.PredicateFriendlyName);
			}
		}

		// Ref return backing fields for overloads that return by ref
		// Justification: Ref return methods need a backing field to hold the value between InvokeRef
		// and the ref return. The field must match the non-nullable return type but cannot be initialized
		// in the constructor because the value comes from the interceptor at invocation time. CS8618 is inherent.
		foreach (var overload in model.Overloads)
		{
			if (overload.IsRefReturn)
			{
				w.Line("#pragma warning disable CS8618 // Ref return backing field initialized by InvokeRef before use");
				w.Line($"internal {overload.ReturnType} _refReturnBacking_{overload.SignatureSuffix};");
				w.Line("#pragma warning restore CS8618");
				w.Line();
			}
		}

		// Invoke methods for each unique signature
		foreach (var overload in model.Overloads)
		{
			RenderOverloadInvokeMethod(w, model, overload, options);
			if (overload.IsRefReturn)
			{
				RenderOverloadInvokeRefMethod(w, model, overload, options);
			}
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
			RenderMethodCallBuilderImpl(w, overload.TrackableParameters, overload.LastArgType, overload.LastArgsType, overload.BuilderInterface, fullInterceptorClassName, overload.DelegateName, overload.SignatureSuffix, overload.ReturnType, overload.IsVoid, hasRefOrOut, overload.Parameters.Count, overload.Parameters, overload.BuilderFriendlyName, overload.SequenceFriendlyName);
		}

		// Nested sequence classes for each unique signature
		foreach (var overload in model.Overloads)
		{
			var hasRefOrOut = HasRefOrOutParameters(overload.Parameters);
			RenderMethodSequenceImpl(w, fullInterceptorClassName, overload.DelegateName, overload.SignatureSuffix, overload.ReturnType, overload.IsVoid, hasRefOrOut, overload.Parameters.Count, overload.Parameters, overload.BuilderFriendlyName, overload.SequenceFriendlyName);
		}

		// Nested When chain classes for each unique signature (for parameter-specific matching)
		foreach (var overload in model.Overloads)
		{
			var hasRefOrOut = HasRefOrOutParameters(overload.Parameters);
			var canHaveWhenChain = !overload.IsVoid && overload.Parameters.Count > 0 && !hasRefOrOut;
			var canHaveVoidWhenChain = overload.IsVoid && overload.Parameters.Count > 0 && !hasRefOrOut;
			if (canHaveWhenChain)
			{
				RenderWhenMatcherClasses(w, overload.Parameters, overload.ReturnType, overload.DelegateName, overload.SignatureSuffix, overload.PredicateFriendlyName);
				RenderWhenBuilderImpl(w, fullInterceptorClassName, overload.Parameters, overload.ReturnType, overload.DelegateName, overload.SignatureSuffix, overload.PredicateFriendlyName);
				RenderWhenChainImpl(w, fullInterceptorClassName, overload.Parameters, overload.ReturnType, overload.DelegateName, overload.SignatureSuffix, overload.PredicateFriendlyName);
			}
			if (canHaveVoidWhenChain)
			{
				RenderVoidWhenMatcherClasses(w, overload.Parameters, overload.DelegateName, overload.SignatureSuffix, overload.PredicateFriendlyName);
				RenderVoidWhenChainImpl(w, fullInterceptorClassName, overload.Parameters, overload.DelegateName, overload.SignatureSuffix, overload.PredicateFriendlyName);
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
		// Include stub parameter when stub override fallback is needed and we have a stub type
		var needsStubParam = options.StubOverrideFallback && !string.IsNullOrEmpty(options.StubTypeName) && !string.IsNullOrEmpty(model.StubOverrideName);
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
				var callbackArgs = BuildDelegateCallArgs(model.Parameters);
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
				w.Line("if (_hasReturnValue && _returnValueTracking != null)");
				using (w.Braces())
				{
					w.Line($"_returnValueTracking.RecordCall({trackingArgs});");
					// Return value, wrapping in Task/ValueTask if needed
					if (isTaskT)
						w.Line($"return global::System.Threading.Tasks.Task.FromResult(_returnValue);");
					else if (isValueTaskT)
						w.Line($"return new global::System.Threading.Tasks.ValueTask<{valueType}>(_returnValue);");
					else
						w.Line("return _returnValue;");
				}
				w.Line();
			}

			// Check repeating callback
			w.Line("if (_call != null && _callTracking != null)");
			using (w.Braces())
			{
				w.Line($"_callTracking.RecordCall({trackingArgs});");
				var callbackArgs = BuildDelegateCallArgs(model.Parameters);
				if (model.IsVoid)
					w.Line($"_call({callbackArgs});");
				else
					w.Line($"return _call({callbackArgs});");
				if (model.IsVoid)
					w.Line("return;");
			}
			w.Line();

			// Check simplified callback for Task<T>/ValueTask<T> methods
			var (invokeInnerType, invokeIsTaskT, invokeIsValueTaskT) = GetAsyncTypeInfo(model.ReturnType);
			var invokeIsAsyncWithInnerType = invokeIsTaskT || invokeIsValueTaskT;
			if (invokeIsAsyncWithInnerType && !hasRefOrOut)
			{
				w.Line("if (_callSimplified != null && _callSimplifiedTracking != null)");
				using (w.Braces())
				{
					w.Line($"_callSimplifiedTracking.RecordCall({trackingArgs});");
					var callbackArgs = BuildDelegateCallArgs(model.Parameters);
					if (invokeIsTaskT)
						w.Line($"return global::System.Threading.Tasks.Task.FromResult(_callSimplified({callbackArgs}));");
					else
						w.Line($"return new global::System.Threading.Tasks.ValueTask<{invokeInnerType}>(_callSimplified({callbackArgs}));");
				}
				w.Line();
			}

			// Check simplified void callback for Task/ValueTask methods
			var (invokeIsVoidTask, invokeIsVoidValueTask) = GetVoidAsyncInfo(model.ReturnType);
			var invokeIsVoidAsync = invokeIsVoidTask || invokeIsVoidValueTask;
			if (invokeIsVoidAsync && !hasRefOrOut)
			{
				w.Line("if (_callSimplifiedVoid != null && _callSimplifiedVoidTracking != null)");
				using (w.Braces())
				{
					w.Line($"_callSimplifiedVoidTracking.RecordCall({trackingArgs});");
					var callbackArgs = BuildDelegateCallArgs(model.Parameters);
					w.Line($"_callSimplifiedVoid({callbackArgs});");
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
					var repeatCallbackArgs = BuildDelegateCallArgs(model.Parameters);
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

			// Final fallback: Stub Override > Source > Strict > Default
			if (options.StubOverrideFallback && !string.IsNullOrEmpty(model.StubOverrideName))
			{
				// Stub override fallback - stub override IS the configured behavior, bypasses Source/Strict
				var stubOverrideCallArgs = string.Join(", ", model.Parameters.Select(p => $"{p.RefPrefix}{p.EscapedName}"));
				// Call via stub parameter if available (flat stubs), otherwise direct call (inline stubs)
				var methodPrefix = !string.IsNullOrEmpty(options.StubTypeName) ? "stub." : "";
				if (model.IsVoid)
				{
					w.Line($"{methodPrefix}{model.StubOverrideName}({stubOverrideCallArgs});");
					w.Line("return;");
				}
				else
				{
					w.Line($"return {methodPrefix}{model.StubOverrideName}({stubOverrideCallArgs});");
				}
			}
			else
			{
				// Standard fallback: Source > Strict > Default
				if (!string.IsNullOrEmpty(model.DeclaringInterface))
				{
					// Justification: Source delegation passes through the source's return value and out parameters.
					// The compiler cannot prove nullability matches the interceptor's declared types (e.g., out parameters
					// on TryGetValue-style methods, or return types with nullable mismatches). This is inherent to
					// source delegation where the stub proxies an unknown implementation.
					w.Line("#pragma warning disable CS8601 // Possible null reference assignment");
					var sourceCallArgs = string.Join(", ", model.Parameters.Select(p => $"{p.RefPrefix}{p.EscapedName}"));
					if (model.IsVoid)
					{
						w.Line($"if (_source is {{ }} src) {{ src.{model.MethodName}({sourceCallArgs}); return; }}");
					}
					else
					{
						w.Line($"if (_source is {{ }} src) return src.{model.MethodName}({sourceCallArgs});");
					}
					w.Line("#pragma warning restore CS8601");
				}

				w.Line($"if ({options.StrictAccessExpression}) throw global::KnockOff.StubException.NotConfigured(\"\", \"{model.MethodName}\");");
				if (model.IsVoid)
					w.Line("return;");
				else if (model.ThrowsOnDefault)
					w.Line($"throw new global::System.InvalidOperationException(\"No implementation provided for {model.MethodName}. Configure via Return or Call.\");");
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
		// Include stub parameter when stub override fallback is needed and we have a stub type
		var needsStubParam = options.StubOverrideFallback && !string.IsNullOrEmpty(options.StubTypeName) && !string.IsNullOrEmpty(overload.StubOverrideName);
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
				var callbackArgs = BuildDelegateCallArgs(overload.Parameters);
				if (overload.IsVoid)
					w.Line($"callback({callbackArgs});");
				else
					w.Line($"return callback({callbackArgs});");
				if (overload.IsVoid)
					w.Line("return;");
			}
			w.Line();

			// Check repeating return value (before callback - value is simpler, check it first)
			var hasRefOrOut = HasRefOrOutParameters(overload.Parameters);
			var canHaveValueOverload = !overload.IsVoid && !hasRefOrOut;
			if (canHaveValueOverload)
			{
				var (valueType, valIsTaskT, valIsValueTaskT) = GetAsyncTypeInfo(overload.ReturnType);
				w.Line($"if (_hasReturnValue_{overload.SignatureSuffix} && _returnValueTracking_{overload.SignatureSuffix} != null)");
				using (w.Braces())
				{
					w.Line($"_returnValueTracking_{overload.SignatureSuffix}.RecordCall({trackingArgs});");
					if (valIsTaskT)
						w.Line($"return global::System.Threading.Tasks.Task.FromResult(_returnValue_{overload.SignatureSuffix});");
					else if (valIsValueTaskT)
						w.Line($"return new global::System.Threading.Tasks.ValueTask<{valueType}>(_returnValue_{overload.SignatureSuffix});");
					else
						w.Line($"return _returnValue_{overload.SignatureSuffix};");
				}
				w.Line();
			}

			// Check repeating callback
			w.Line($"if (_call_{overload.SignatureSuffix} != null && _callTracking_{overload.SignatureSuffix} != null)");
			using (w.Braces())
			{
				w.Line($"_callTracking_{overload.SignatureSuffix}.RecordCall({trackingArgs});");
				var callbackArgs = BuildDelegateCallArgs(overload.Parameters);
				if (overload.IsVoid)
					w.Line($"_call_{overload.SignatureSuffix}({callbackArgs});");
				else
					w.Line($"return _call_{overload.SignatureSuffix}({callbackArgs});");
				if (overload.IsVoid)
					w.Line("return;");
			}
			w.Line();

			// Check simplified callback for Task<T>/ValueTask<T> overloads
			var (innerType, isTaskT, isValueTaskT) = GetAsyncTypeInfo(overload.ReturnType);
			var isAsyncWithInnerType = isTaskT || isValueTaskT;
			if (isAsyncWithInnerType && !hasRefOrOut)
			{
				w.Line($"if (_callSimplified_{overload.SignatureSuffix} != null && _callSimplifiedTracking_{overload.SignatureSuffix} != null)");
				using (w.Braces())
				{
					w.Line($"_callSimplifiedTracking_{overload.SignatureSuffix}.RecordCall({trackingArgs});");
					var callbackArgs = BuildDelegateCallArgs(overload.Parameters);
					if (isTaskT)
						w.Line($"return global::System.Threading.Tasks.Task.FromResult(_callSimplified_{overload.SignatureSuffix}({callbackArgs}));");
					else
						w.Line($"return new global::System.Threading.Tasks.ValueTask<{innerType}>(_callSimplified_{overload.SignatureSuffix}({callbackArgs}));");
				}
				w.Line();
			}

			// Check simplified void callback for Task/ValueTask overloads
			var (isVoidTask, isVoidValueTask) = GetVoidAsyncInfo(overload.ReturnType);
			var isVoidAsync = isVoidTask || isVoidValueTask;
			if (isVoidAsync && !hasRefOrOut)
			{
				w.Line($"if (_callSimplifiedVoid_{overload.SignatureSuffix} != null && _callSimplifiedVoidTracking_{overload.SignatureSuffix} != null)");
				using (w.Braces())
				{
					w.Line($"_callSimplifiedVoidTracking_{overload.SignatureSuffix}.RecordCall({trackingArgs});");
					var callbackArgs = BuildDelegateCallArgs(overload.Parameters);
					w.Line($"_callSimplifiedVoid_{overload.SignatureSuffix}({callbackArgs});");
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
					var repeatCallbackArgs = BuildDelegateCallArgs(overload.Parameters);
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

			// Final fallback: Stub Override (per-signature) > Source > Strict > Default
			if (options.StubOverrideFallback && !string.IsNullOrEmpty(overload.StubOverrideName))
			{
				// Stub override fallback - stub override IS the configured behavior, bypasses Source/Strict
				var stubOverrideCallArgs = string.Join(", ", overload.Parameters.Select(p => $"{p.RefPrefix}{p.EscapedName}"));
				// Call via stub parameter if available (flat stubs), otherwise direct call (inline stubs)
				var methodPrefix = !string.IsNullOrEmpty(options.StubTypeName) ? "stub." : "";
				if (overload.IsVoid)
				{
					w.Line($"{methodPrefix}{overload.StubOverrideName}({stubOverrideCallArgs});");
					w.Line("return;");
				}
				else
				{
					w.Line($"return {methodPrefix}{overload.StubOverrideName}({stubOverrideCallArgs});");
				}
			}
			else
			{
				// Standard fallback: Source > Strict > Default
				if (!string.IsNullOrEmpty(model.DeclaringInterface))
				{
					// Justification: Source delegation passes through the source's return value and out parameters.
					// The compiler cannot prove nullability matches the interceptor's declared types (e.g., out parameters
					// on TryGetValue-style methods, or return types with nullable mismatches). This is inherent to
					// source delegation where the stub proxies an unknown implementation.
					w.Line("#pragma warning disable CS8601 // Possible null reference assignment");
					var sourceCallArgs = string.Join(", ", overload.Parameters.Select(p => $"{p.RefPrefix}{p.EscapedName}"));
					if (overload.IsVoid)
					{
						w.Line($"if (_source is {{ }} src) {{ src.{model.MethodName}({sourceCallArgs}); return; }}");
					}
					else
					{
						w.Line($"if (_source is {{ }} src) return src.{model.MethodName}({sourceCallArgs});");
					}
					w.Line("#pragma warning restore CS8601");
				}

				w.Line($"if ({options.StrictAccessExpression}) throw global::KnockOff.StubException.NotConfigured(\"\", \"{model.MethodName}\");");
				if (overload.IsVoid)
					w.Line("return;");
				else if (overload.ThrowsOnDefault)
					w.Line($"throw new global::System.InvalidOperationException(\"No implementation provided for {model.MethodName}. Configure via Return or Call.\");");
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

				// Return value directly - Call() returns the full return type (async wrapping done at config time)
				w.Line($"return matcher.Call({callbackArgs});");
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

	#region InvokeRef Methods (Ref Return Support)

	/// <summary>
	/// Renders the InvokeRef method for single-signature ref return methods.
	/// This is a simplified version of Invoke that writes to _refReturnBacking instead of returning,
	/// and skips all async branches (C# prohibits async ref returns).
	/// </summary>
	private static void RenderInvokeRefMethod(
		CodeWriter w,
		UnifiedMethodInterceptorModel model,
		InterceptorRenderOptions options,
		string? signatureSuffix)
	{
		// Include stub parameter when stub override fallback is needed and we have a stub type
		var needsStubParam = options.StubOverrideFallback && !string.IsNullOrEmpty(options.StubTypeName) && !string.IsNullOrEmpty(model.StubOverrideName);
		var invokeParams = BuildInvokeParams(model.Parameters, options.IncludeStrictParameter, needsStubParam ? options.StubTypeName : null);

		// Determine if value overload exists for this method
		var hasRefOrOut = HasRefOrOutParameters(model.Parameters);
		var canHaveValueOverload = !model.IsVoid && !hasRefOrOut;

		w.Line($"/// <summary>Invokes the configured callback, writing result to _refReturnBacking. Called by ref return interface implementations.</summary>");
		w.Line($"internal void InvokeRef({invokeParams})");
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
			if (canHaveWhenChain)
			{
				RenderWhenChainInvokeRefCheck(w, model.Parameters, null);
			}

			// Check sequence (takes priority if When chain didn't match)
			w.Line("if (_sequence != null && _sequenceIndex < _sequence.Count)");
			using (w.Braces())
			{
				w.Line("var (callback, tracking) = _sequence[_sequenceIndex];");
				w.Line($"tracking.RecordCall({trackingArgs});");
				w.Line("_sequenceIndex++;");
				var callbackArgs = BuildDelegateCallArgs(model.Parameters);
				w.Line($"_refReturnBacking = callback({callbackArgs});");
				w.Line("return;");
			}
			w.Line();

			// Check repeating Returns value (before callback - value is simpler, check it first)
			if (canHaveValueOverload)
			{
				w.Line("if (_hasReturnValue && _returnValueTracking != null)");
				using (w.Braces())
				{
					w.Line($"_returnValueTracking.RecordCall({trackingArgs});");
					// Direct assignment - no Task/ValueTask wrapping (ref returns can't be async)
					w.Line("_refReturnBacking = _returnValue;");
					w.Line("return;");
				}
				w.Line();
			}

			// Check repeating callback
			w.Line("if (_call != null && _callTracking != null)");
			using (w.Braces())
			{
				w.Line($"_callTracking.RecordCall({trackingArgs});");
				var callbackArgs = BuildDelegateCallArgs(model.Parameters);
				w.Line($"_refReturnBacking = _call({callbackArgs});");
				w.Line("return;");
			}
			w.Line();

			// Steps 7-8 SKIPPED: simplified callback for Task<T>/ValueTask<T> and void Task/ValueTask
			// C# prohibits async ref returns, so these branches are never applicable.

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
					var repeatCallbackArgs = BuildDelegateCallArgs(model.Parameters);
					w.Line($"_refReturnBacking = callback({repeatCallbackArgs});");
					w.Line("return;");
				}
				// Write default to backing (only reached when _repeatLastValue is false via ThenDefault())
				w.Line("_refReturnBacking = default!;");
				w.Line("return;");
			}
			w.Line();

			// Final fallback: Stub Override > Source > Strict > Default
			if (options.StubOverrideFallback && !string.IsNullOrEmpty(model.StubOverrideName))
			{
				// Stub override method fallback - writes result to _refReturnBacking
				var stubOverrideCallArgs = string.Join(", ", model.Parameters.Select(p => $"{p.RefPrefix}{p.EscapedName}"));
				var methodPrefix = !string.IsNullOrEmpty(options.StubTypeName) ? "stub." : "";
				w.Line($"_refReturnBacking = {methodPrefix}{model.StubOverrideName}({stubOverrideCallArgs});");
				w.Line("return;");
			}
			else
			{
				// Standard fallback: Source > Strict > Default
				if (!string.IsNullOrEmpty(model.DeclaringInterface))
				{
					// Justification: Source delegation assigns the source's return value to the ref backing field.
					// Nullability mismatch is inherent to source delegation proxying an unknown implementation.
					w.Line("#pragma warning disable CS8601 // Possible null reference assignment");
					var sourceCallArgs = string.Join(", ", model.Parameters.Select(p => $"{p.RefPrefix}{p.EscapedName}"));
					// Source delegation: copy source's value to _refReturnBacking (lossy ref redirection, acceptable for stubs)
					w.Line($"if (_source is {{ }} src) {{ _refReturnBacking = src.{model.MethodName}({sourceCallArgs}); return; }}");
					w.Line("#pragma warning restore CS8601");
				}

				w.Line($"if ({options.StrictAccessExpression}) throw global::KnockOff.StubException.NotConfigured(\"\", \"{model.MethodName}\");");
				w.Line("_refReturnBacking = default!;");
				w.Line("return;");
			}
		}
		w.Line();
	}

	/// <summary>
	/// Renders the InvokeRef method for overload-group ref return methods.
	/// </summary>
	private static void RenderOverloadInvokeRefMethod(
		CodeWriter w,
		UnifiedMethodInterceptorModel model,
		MethodOverloadSignature overload,
		InterceptorRenderOptions options)
	{
		// Include stub parameter when stub override fallback is needed and we have a stub type
		var needsStubParam = options.StubOverrideFallback && !string.IsNullOrEmpty(options.StubTypeName) && !string.IsNullOrEmpty(overload.StubOverrideName);
		var invokeParams = BuildInvokeParams(overload.Parameters, options.IncludeStrictParameter, needsStubParam ? options.StubTypeName : null);
		var backingField = $"_refReturnBacking_{overload.SignatureSuffix}";

		w.Line($"/// <summary>Invokes configured callback for {model.MethodName}({GetParamTypeList(overload.Parameters)}), writing result to backing field.</summary>");
		w.Line($"internal void InvokeRef_{overload.SignatureSuffix}({invokeParams})");
		using (w.Braces())
		{
			// Initialize out parameters
			foreach (var p in overload.Parameters.Where(p => p.RefKind == Microsoft.CodeAnalysis.RefKind.Out))
			{
				w.Line($"{p.EscapedName} = default!;");
			}

			var trackingArgs = UnifiedInterceptorBuilder.BuildTrackingArgs(overload.TrackableParameters);

			// When chain - check HEAD matcher first (highest priority)
			var hasRefOrOutForWhen = HasRefOrOutParameters(overload.Parameters);
			var canHaveWhenChain = !overload.IsVoid && overload.Parameters.Count > 0 && !hasRefOrOutForWhen;
			if (canHaveWhenChain)
			{
				RenderWhenChainInvokeRefCheck(w, overload.Parameters, overload.SignatureSuffix, backingField);
			}

			// Check sequence (takes priority if When chain didn't match)
			w.Line($"if (_sequence_{overload.SignatureSuffix} != null && _sequenceIndex_{overload.SignatureSuffix} < _sequence_{overload.SignatureSuffix}.Count)");
			using (w.Braces())
			{
				w.Line($"var (callback, tracking) = _sequence_{overload.SignatureSuffix}[_sequenceIndex_{overload.SignatureSuffix}];");
				w.Line($"tracking.RecordCall({trackingArgs});");
				w.Line($"_sequenceIndex_{overload.SignatureSuffix}++;");
				var callbackArgs = BuildDelegateCallArgs(overload.Parameters);
				w.Line($"{backingField} = callback({callbackArgs});");
				w.Line("return;");
			}
			w.Line();

			// Check repeating callback
			w.Line($"if (_call_{overload.SignatureSuffix} != null && _callTracking_{overload.SignatureSuffix} != null)");
			using (w.Braces())
			{
				w.Line($"_callTracking_{overload.SignatureSuffix}.RecordCall({trackingArgs});");
				var callbackArgs = BuildDelegateCallArgs(overload.Parameters);
				w.Line($"{backingField} = _call_{overload.SignatureSuffix}({callbackArgs});");
				w.Line("return;");
			}
			w.Line();

			// Steps 7-8 SKIPPED: async branches impossible for ref returns

			// No callback configured
			w.Line("_unconfiguredCallCount++;");

			// Sequence exhausted
			w.Line($"if (_sequence_{overload.SignatureSuffix} != null && _sequenceIndex_{overload.SignatureSuffix} >= _sequence_{overload.SignatureSuffix}.Count)");
			using (w.Braces())
			{
				w.Line($"if ({options.StrictAccessExpression}) throw global::KnockOff.StubException.SequenceExhausted(\"{model.MethodName}\");");
				w.Line($"if (_repeatLastValue_{overload.SignatureSuffix} && _sequence_{overload.SignatureSuffix}.Count > 0)");
				using (w.Braces())
				{
					w.Line($"var (callback, tracking) = _sequence_{overload.SignatureSuffix}[_sequence_{overload.SignatureSuffix}.Count - 1];");
					w.Line($"tracking.RecordCall({trackingArgs});");
					var repeatCallbackArgs = BuildDelegateCallArgs(overload.Parameters);
					w.Line($"{backingField} = callback({repeatCallbackArgs});");
					w.Line("return;");
				}
				w.Line($"{backingField} = default!;");
				w.Line("return;");
			}
			w.Line();

			// Final fallback: Stub Override > Source > Strict > Default
			if (options.StubOverrideFallback && !string.IsNullOrEmpty(overload.StubOverrideName))
			{
				var stubOverrideCallArgs = string.Join(", ", overload.Parameters.Select(p => $"{p.RefPrefix}{p.EscapedName}"));
				var methodPrefix = !string.IsNullOrEmpty(options.StubTypeName) ? "stub." : "";
				w.Line($"{backingField} = {methodPrefix}{overload.StubOverrideName}({stubOverrideCallArgs});");
				w.Line("return;");
			}
			else
			{
				if (!string.IsNullOrEmpty(model.DeclaringInterface))
				{
					// Justification: Source delegation assigns the source's return value to the ref backing field.
					// Nullability mismatch is inherent to source delegation proxying an unknown implementation.
					w.Line("#pragma warning disable CS8601 // Possible null reference assignment");
					var sourceCallArgs = string.Join(", ", overload.Parameters.Select(p => $"{p.RefPrefix}{p.EscapedName}"));
					w.Line($"if (_source is {{ }} src) {{ {backingField} = src.{model.MethodName}({sourceCallArgs}); return; }}");
					w.Line("#pragma warning restore CS8601");
				}

				w.Line($"if ({options.StrictAccessExpression}) throw global::KnockOff.StubException.NotConfigured(\"\", \"{model.MethodName}\");");
				w.Line($"{backingField} = default!;");
				w.Line("return;");
			}
		}
		w.Line();
	}

	/// <summary>
	/// Renders the When chain invoke check for InvokeRef (ref return methods).
	/// Writes to _refReturnBacking instead of returning.
	/// </summary>
	private static void RenderWhenChainInvokeRefCheck(
		CodeWriter w,
		EquatableArray<ParameterModel> parameters,
		string? signatureSuffix,
		string backingField = "_refReturnBacking")
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

				// Write result to backing field instead of returning
				w.Line($"{backingField} = matcher.Call({callbackArgs});");
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
				w.Line("_callTracking?.Reset();");
				// Reset value tracking only if value overload exists
				if (hasValueOverload)
					w.Line("_returnValueTracking?.Reset();");
				// Reset simplified callback tracking
				if (hasSimplifiedCallback)
					w.Line("_callSimplifiedTracking?.Reset();");
				if (hasSimplifiedVoidCallback)
					w.Line("_callSimplifiedVoidTracking?.Reset();");
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
					w.Line($"_callTracking_{overload.SignatureSuffix}?.Reset();");
					// Reset value tracking for non-void overloads
					var hasRefOrOut = HasRefOrOutParameters(overload.Parameters);
					if (!overload.IsVoid && !hasRefOrOut)
					{
						w.Line($"_returnValueTracking_{overload.SignatureSuffix}?.Reset();");
					}
					// Reset simplified callback tracking for async overloads
					var (_, isTaskT, isValueTaskT) = GetAsyncTypeInfo(overload.ReturnType);
					var isAsyncWithInnerType = isTaskT || isValueTaskT;
					if (isAsyncWithInnerType && !hasRefOrOut)
					{
						w.Line($"_callSimplifiedTracking_{overload.SignatureSuffix}?.Reset();");
					}
					var (isVoidTask, isVoidValueTask) = GetVoidAsyncInfo(overload.ReturnType);
					var isVoidAsync = isVoidTask || isVoidValueTask;
					if (isVoidAsync && !hasRefOrOut)
					{
						w.Line($"_callSimplifiedVoidTracking_{overload.SignatureSuffix}?.Reset();");
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
				? "_hasReturnValue || _call != null || (_sequence?.Count ?? 0) > 0"
				: "_call != null || (_sequence?.Count ?? 0) > 0";
			if (hasSimplifiedCallback)
				isConfiguredExpr += " || _callSimplified != null";
			if (hasSimplifiedVoidCallback)
				isConfiguredExpr += " || _callSimplifiedVoid != null";
			if (hasWhenChain)
				isConfiguredExpr += " || (_whenChain?.Count ?? 0) > 0";
			w.Line("/// <summary>Whether this interceptor has been configured (Return, Call, Return(value), or When).</summary>");
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
					w.Line("var times = _verifiableTimes ?? global::KnockOff.Called.AtLeastOnce;");
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
						w.Line("if (head < count && !_whenChain[head].IsTerminal && _whenChain[head].CallCount == 0)");
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
				w.Line("if (!global::KnockOff.Called.AtLeastOnce.Validate(TotalCallCount))");
				w.Line($"\treturn new global::KnockOff.VerificationFailure(\"{methodName}\", global::KnockOff.Called.AtLeastOnce, TotalCallCount);");
				// Check When chain if configured
				if (hasWhenChain)
				{
					w.Line("if (_whenChain != null && _whenChain.Count > 0)");
					using (w.Braces())
					{
						w.Line("var head = _whenChainHead;");
						w.Line("var count = _whenChain.Count;");
						w.Line("// Chain must be fully consumed (HEAD at end or at terminal matcher)");
						w.Line("if (head < count && !_whenChain[head].IsTerminal && _whenChain[head].CallCount == 0)");
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

			// Build IsConfigured including value storage, simplified callbacks and When chains for each overload
			w.Line("/// <summary>Whether any overload has been configured.</summary>");
			var isConfiguredParts = new List<string>();
			foreach (var overload in overloads)
			{
				var parts = new List<string>
				{
					$"_call_{overload.SignatureSuffix} != null",
					$"(_sequence_{overload.SignatureSuffix}?.Count ?? 0) > 0"
				};
				// Add value storage check for non-void overloads
				var hasRefOrOut = HasRefOrOutParameters(overload.Parameters);
				if (!overload.IsVoid && !hasRefOrOut)
					parts.Add($"_hasReturnValue_{overload.SignatureSuffix}");
				// Add simplified callback checks for async overloads
				var (_, isTaskT, isValueTaskT) = GetAsyncTypeInfo(overload.ReturnType);
				if ((isTaskT || isValueTaskT) && !hasRefOrOut)
					parts.Add($"_callSimplified_{overload.SignatureSuffix} != null");
				var (isVoidTask, isVoidValueTask) = GetVoidAsyncInfo(overload.ReturnType);
				if ((isVoidTask || isVoidValueTask) && !hasRefOrOut)
					parts.Add($"_callSimplifiedVoid_{overload.SignatureSuffix} != null");
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
						w.Line($"var times = _verifiableTimes_{overload.SignatureSuffix} ?? global::KnockOff.Called.AtLeastOnce;");
						// Build count including value tracking and simplified tracking
						var countParts = new List<string>
						{
							$"(_callTracking_{overload.SignatureSuffix}?._callCount ?? 0)",
							$"(_sequence_{overload.SignatureSuffix}?.Sum(s => s.Tracking._callCount) ?? 0)"
						};
						var hasRefOrOut = HasRefOrOutParameters(overload.Parameters);
						// Add value tracking count
						if (!overload.IsVoid && !hasRefOrOut)
							countParts.Add($"(_returnValueTracking_{overload.SignatureSuffix}?._callCount ?? 0)");
						var (_, isTaskT, isValueTaskT) = GetAsyncTypeInfo(overload.ReturnType);
						if ((isTaskT || isValueTaskT) && !hasRefOrOut)
							countParts.Add($"(_callSimplifiedTracking_{overload.SignatureSuffix}?._callCount ?? 0)");
						var (isVoidTask, isVoidValueTask) = GetVoidAsyncInfo(overload.ReturnType);
						if ((isVoidTask || isVoidValueTask) && !hasRefOrOut)
							countParts.Add($"(_callSimplifiedVoidTracking_{overload.SignatureSuffix}?._callCount ?? 0)");
						// When chain call counts
						var canHaveWhenChainForOverload = !overload.IsVoid && overload.Parameters.Count > 0 && !hasRefOrOut;
						var canHaveVoidWhenChainForOverload = overload.IsVoid && overload.Parameters.Count > 0 && !hasRefOrOut;
						if (canHaveWhenChainForOverload || canHaveVoidWhenChainForOverload)
							countParts.Add($"(_whenChain_{overload.SignatureSuffix}?.Sum(m => m.CallCount) ?? 0)");
						var countExpr = string.Join(" + ", countParts);
						w.Line($"var count = {countExpr};");
						w.Line($"if (!times.Validate(count)) return new global::KnockOff.VerificationFailure(\"{methodName}\", times, count);");
					}
					// Check When chain verification for this overload
					var hasRefOrOutForWhen = HasRefOrOutParameters(overload.Parameters);
					var canHaveWhenChainForOverloadForWhen = !overload.IsVoid && overload.Parameters.Count > 0 && !hasRefOrOutForWhen;
					var canHaveVoidWhenChainForOverloadForWhen = overload.IsVoid && overload.Parameters.Count > 0 && !hasRefOrOutForWhen;
					if (canHaveWhenChainForOverloadForWhen || canHaveVoidWhenChainForOverloadForWhen)
					{
						w.Line($"if (_whenVerifiable_{overload.SignatureSuffix} && _whenChain_{overload.SignatureSuffix} != null && _whenChain_{overload.SignatureSuffix}.Count > 0)");
						using (w.Braces())
						{
							w.Line($"var head = _whenChainHead_{overload.SignatureSuffix};");
							w.Line($"var chainCount = _whenChain_{overload.SignatureSuffix}.Count;");
							w.Line("// Chain must be fully consumed (HEAD at end or at terminal matcher)");
							w.Line($"if (head < chainCount && !_whenChain_{overload.SignatureSuffix}[head].IsTerminal && _whenChain_{overload.SignatureSuffix}[head].CallCount == 0)");
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
					// Build condition including value storage and simplified callbacks
					var condParts = new List<string>
					{
						$"_call_{overload.SignatureSuffix} != null",
						$"(_sequence_{overload.SignatureSuffix}?.Count ?? 0) > 0"
					};
					var hasRefOrOut = HasRefOrOutParameters(overload.Parameters);
					// Add value storage condition
					if (!overload.IsVoid && !hasRefOrOut)
						condParts.Add($"_hasReturnValue_{overload.SignatureSuffix}");
					var (_, isTaskT, isValueTaskT) = GetAsyncTypeInfo(overload.ReturnType);
					if ((isTaskT || isValueTaskT) && !hasRefOrOut)
						condParts.Add($"_callSimplified_{overload.SignatureSuffix} != null");
					var (isVoidTask, isVoidValueTask) = GetVoidAsyncInfo(overload.ReturnType);
					if ((isVoidTask || isVoidValueTask) && !hasRefOrOut)
						condParts.Add($"_callSimplifiedVoid_{overload.SignatureSuffix} != null");
					// When chain configured check (matching IsConfigured property pattern)
					var canHaveWhenChainForOverload = !overload.IsVoid && overload.Parameters.Count > 0 && !hasRefOrOut;
					var canHaveVoidWhenChainForOverload = overload.IsVoid && overload.Parameters.Count > 0 && !hasRefOrOut;
					if (canHaveWhenChainForOverload || canHaveVoidWhenChainForOverload)
						condParts.Add($"(_whenChain_{overload.SignatureSuffix}?.Count ?? 0) > 0");
					var condExpr = string.Join(" || ", condParts);
					w.Line($"if ({condExpr})");
					using (w.Braces())
					{
						// Build count including value tracking and simplified tracking
						var countParts = new List<string>
						{
							$"(_callTracking_{overload.SignatureSuffix}?._callCount ?? 0)",
							$"(_sequence_{overload.SignatureSuffix}?.Sum(s => s.Tracking._callCount) ?? 0)"
						};
						// Add value tracking count
						if (!overload.IsVoid && !hasRefOrOut)
							countParts.Add($"(_returnValueTracking_{overload.SignatureSuffix}?._callCount ?? 0)");
						if ((isTaskT || isValueTaskT) && !hasRefOrOut)
							countParts.Add($"(_callSimplifiedTracking_{overload.SignatureSuffix}?._callCount ?? 0)");
						if ((isVoidTask || isVoidValueTask) && !hasRefOrOut)
							countParts.Add($"(_callSimplifiedVoidTracking_{overload.SignatureSuffix}?._callCount ?? 0)");
						// When chain call counts
						if (canHaveWhenChainForOverload || canHaveVoidWhenChainForOverload)
							countParts.Add($"(_whenChain_{overload.SignatureSuffix}?.Sum(m => m.CallCount) ?? 0)");
						var countExpr = string.Join(" + ", countParts);
						w.Line($"var count = {countExpr};");
						w.Line($"if (!global::KnockOff.Called.AtLeastOnce.Validate(count)) return new global::KnockOff.VerificationFailure(\"{methodName}\", global::KnockOff.Called.AtLeastOnce, count);");
					}
					// Check When chain for this overload if configured
					var hasRefOrOutForWhen = HasRefOrOutParameters(overload.Parameters);
					var canHaveWhenChainForOverloadForWhen = !overload.IsVoid && overload.Parameters.Count > 0 && !hasRefOrOutForWhen;
					var canHaveVoidWhenChainForOverloadForWhen = overload.IsVoid && overload.Parameters.Count > 0 && !hasRefOrOutForWhen;
					if (canHaveWhenChainForOverloadForWhen || canHaveVoidWhenChainForOverloadForWhen)
					{
						w.Line($"if (_whenChain_{overload.SignatureSuffix} != null && _whenChain_{overload.SignatureSuffix}.Count > 0)");
						using (w.Braces())
						{
							w.Line($"var head = _whenChainHead_{overload.SignatureSuffix};");
							w.Line($"var chainCount = _whenChain_{overload.SignatureSuffix}.Count;");
							w.Line("// Chain must be fully consumed (HEAD at end or at terminal matcher)");
							w.Line($"if (head < chainCount && !_whenChain_{overload.SignatureSuffix}[head].IsTerminal && _whenChain_{overload.SignatureSuffix}[head].CallCount == 0)");
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
		int parameterCount,
		EquatableArray<ParameterModel> parameters,
		string? builderFriendlyName = null,
		string? sequenceFriendlyName = null)
	{
		var className = builderFriendlyName ?? (signatureSuffix == null ? "MethodCallBuilderImpl" : $"MethodCallBuilderImpl_{signatureSuffix}");
		var sequenceClassName = sequenceFriendlyName ?? (signatureSuffix == null ? "MethodSequenceImpl" : $"MethodSequenceImpl_{signatureSuffix}");
		var verifiableFieldName = signatureSuffix == null ? "_isVerifiable" : $"_isVerifiable_{signatureSuffix}";
		var verifiableTimesFieldName = signatureSuffix == null ? "_verifiableTimes" : $"_verifiableTimes_{signatureSuffix}";
		var sequenceFieldName = signatureSuffix == null ? "_sequence" : $"_sequence_{signatureSuffix}";
		var sequenceIndexFieldName = signatureSuffix == null ? "_sequenceIndex" : $"_sequenceIndex_{signatureSuffix}";
		var callFieldName = signatureSuffix == null ? "_call" : $"_call_{signatureSuffix}";
		var callTrackingFieldName = signatureSuffix == null ? "_callTracking" : $"_callTracking_{signatureSuffix}";

		// Field name variables for simplified callback storage (needed for sequence elevation fix)
		var callSimplifiedFieldName = signatureSuffix == null ? "_callSimplified" : $"_callSimplified_{signatureSuffix}";
		var callSimplifiedTrackingFieldName = signatureSuffix == null ? "_callSimplifiedTracking" : $"_callSimplifiedTracking_{signatureSuffix}";
		var callSimplifiedVoidFieldName = signatureSuffix == null ? "_callSimplifiedVoid" : $"_callSimplifiedVoid_{signatureSuffix}";
		var callSimplifiedVoidTrackingFieldName = signatureSuffix == null ? "_callSimplifiedVoidTracking" : $"_callSimplifiedVoidTracking_{signatureSuffix}";

		// Derive conditions for which branches to emit in sequence elevation
		var canHaveValueOverload = !isVoid && !hasRefOrOut && signatureSuffix == null;
		var (elevationInnerType, elevationIsTaskT, elevationIsValueTaskT) = GetAsyncTypeInfo(returnType);
		var elevationIsAsyncWithInnerType = elevationIsTaskT || elevationIsValueTaskT;
		var (elevationIsVoidTask, elevationIsVoidValueTask) = GetVoidAsyncInfo(returnType);
		var elevationIsVoidAsync = elevationIsVoidTask || elevationIsVoidValueTask;

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
			w.Line("public void Verify() => Verify(global::KnockOff.Called.AtLeastOnce);");
			w.Line();

			// Verify(Times) - throws on failure
			w.Line("/// <summary>Verifies call count satisfies the Called constraint. Throws VerificationException if not.</summary>");
			w.Line("public void Verify(global::KnockOff.Called times)");
			using (w.Braces())
			{
				w.Line("if (!times.Validate(_callCount))");
				w.Line("\tthrow new global::KnockOff.VerificationException(new global::KnockOff.VerificationFailure(\"method\", times, _callCount));");
			}
			w.Line();

			// ThenReturn()/ThenCall() - lazy elevation from repeating to sequence mode
			var thenChainName = isVoid ? "ThenCall" : "ThenReturn";
			w.Line("/// <summary>Elevates to sequence mode and adds another callback. Return sequence for further chaining.</summary>");
			w.Line($"public {sequenceClassName} {thenChainName}({delegateType} callback)");
			using (w.Braces())
			{
				// Lazy elevation: if not already in sequence mode, move current callback/value into sequence as first element
				EmitSequenceElevation(w, sequenceFieldName, delegateType, className,
					callFieldName, callTrackingFieldName, sequenceIndexFieldName,
					canHaveValueOverload, elevationIsAsyncWithInnerType, elevationIsVoidAsync,
					elevationIsTaskT, elevationIsValueTaskT, elevationIsVoidTask, elevationIsVoidValueTask,
					elevationInnerType, callSimplifiedFieldName, callSimplifiedTrackingFieldName,
					callSimplifiedVoidFieldName, callSimplifiedVoidTrackingFieldName,
					hasRefOrOut, parameterCount, parameters);
				// Add new callback with fresh builder for its tracking
				w.Line($"var nextBuilder = new {className}(_interceptor);");
				w.Line($"_interceptor.{sequenceFieldName}.Add((callback, nextBuilder));");
				w.Line($"return new {sequenceClassName}(_interceptor);");
			}
			w.Line();

			// ThenReturn(value) - value wrapper that elevates to sequence, only for non-void methods without ref/out
			if (!isVoid && !hasRefOrOut)
			{
				var (valueType, isTaskT, isValueTaskT) = GetAsyncTypeInfo(returnType);
				var discardPrefix = BuildDiscardLambdaPrefix(parameterCount);
				w.Line($"/// <summary>Elevates to sequence mode and adds a value. Return sequence for further chaining.</summary>");
				if (isTaskT)
				{
					w.Line($"public {sequenceClassName} ThenReturn({valueType} value) => ThenReturn({discardPrefix} => global::System.Threading.Tasks.Task.FromResult(value));");
				}
				else if (isValueTaskT)
				{
					w.Line($"public {sequenceClassName} ThenReturn({valueType} value) => ThenReturn({discardPrefix} => new global::System.Threading.Tasks.ValueTask<{valueType}>(value));");
				}
				else
				{
					w.Line($"public {sequenceClassName} ThenReturn({valueType} value) => ThenReturn({discardPrefix} => value);");
				}
				w.Line();

				// ThenReturn(params values) - adds multiple values to sequence
				w.Line($"/// <summary>Adds multiple values to the sequence. Each value returned once.</summary>");
				w.Line($"public {sequenceClassName} ThenReturn(params {valueType}[] values)");
				using (w.Braces())
				{
					w.Line("if (values.Length == 0)");
					using (w.Braces())
					{
						// Elevate to sequence mode without adding any new values (same as ThenReturn elevation)
						EmitSequenceElevation(w, sequenceFieldName, delegateType, className,
							callFieldName, callTrackingFieldName, sequenceIndexFieldName,
							canHaveValueOverload, elevationIsAsyncWithInnerType, elevationIsVoidAsync,
							elevationIsTaskT, elevationIsValueTaskT, elevationIsVoidTask, elevationIsVoidValueTask,
							elevationInnerType, callSimplifiedFieldName, callSimplifiedTrackingFieldName,
							callSimplifiedVoidFieldName, callSimplifiedVoidTrackingFieldName,
							hasRefOrOut, parameterCount, parameters);
						w.Line($"return new {sequenceClassName}(_interceptor);");
					}
					w.Line("var seq = ThenReturn(values[0]);");
					w.Line("for (int i = 1; i < values.Length; i++)");
					using (w.Braces())
					{
						w.Line("seq = seq.ThenReturn(values[i]);");
					}
					w.Line("return seq;");
				}
				w.Line();

				// Simplified async ThenReturn(Func<..., T>) - for Task<T>/ValueTask<T> methods
				var (builderAsyncInner, builderIsTaskT, builderIsValueTaskT) = GetAsyncTypeInfo(returnType);
				var builderIsAsync = builderIsTaskT || builderIsValueTaskT;
				if (builderIsAsync && !hasRefOrOut)
				{
					var simplifiedDelegateType = BuildSimplifiedDelegateType(parameters, builderAsyncInner);
					// All delegates (custom and simplified) use individual params.
					var wrapperLambdaParamDecls = BuildDelegateMatchingParamDecls(parameters);
					var callArgs = BuildDelegateMatchingCallArgs(parameters);
					var wrapperCallbackCall = parameters.Count == 0 ? "callback()" : $"callback({callArgs})";
					w.Line($"/// <summary>Elevates to sequence mode with simplified callback. Result auto-wrapped in {(builderIsTaskT ? "Task.FromResult" : "new ValueTask")}.</summary>");
					if (builderIsTaskT)
					{
						w.Line($"public {sequenceClassName} ThenReturn({simplifiedDelegateType} callback) => ThenReturn({wrapperLambdaParamDecls} => global::System.Threading.Tasks.Task.FromResult({wrapperCallbackCall}));");
					}
					else
					{
						w.Line($"public {sequenceClassName} ThenReturn({simplifiedDelegateType} callback) => ThenReturn({wrapperLambdaParamDecls} => new global::System.Threading.Tasks.ValueTask<{builderAsyncInner}>({wrapperCallbackCall}));");
					}
					w.Line();
				}
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

			w.Line("/// <summary>Marks for verification by Stub.Verify() with Called constraint. Returns this for fluent chaining.</summary>");
			w.Line($"public {builderInterface} Verifiable(global::KnockOff.Called times)");
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
			w.Line("global::KnockOff.IMethodTracking global::KnockOff.IMethodTracking.Verifiable(global::KnockOff.Called times) => Verifiable(times);");

			// If implementing IMethodTracking<TArg>, also need explicit implementations for it
			if (trackableParams.Count == 1)
			{
				var param = trackableParams.GetArray()![0];
				w.Line($"global::KnockOff.IMethodTracking<{param.Type}> global::KnockOff.IMethodTracking<{param.Type}>.Verifiable() => Verifiable();");
				w.Line($"global::KnockOff.IMethodTracking<{param.Type}> global::KnockOff.IMethodTracking<{param.Type}>.Verifiable(global::KnockOff.Called times) => Verifiable(times);");
			}
			else if (trackableParams.Count > 1)
			{
				w.Line($"global::KnockOff.IMethodTrackingArgs<{lastArgsType}> global::KnockOff.IMethodTrackingArgs<{lastArgsType}>.Verifiable() => Verifiable();");
				w.Line($"global::KnockOff.IMethodTrackingArgs<{lastArgsType}> global::KnockOff.IMethodTrackingArgs<{lastArgsType}>.Verifiable(global::KnockOff.Called times) => Verifiable(times);");
			}

			// Explicit interface implementation for ThenReturn/ThenCall - interface requires sequence return
			if (isVoid)
			{
				w.Line($"global::KnockOff.IMethodCallSequence<{delegateType}> {builderInterface}.ThenCall({delegateType} callback) => ThenCall(callback);");
			}
			else
			{
				w.Line($"global::KnockOff.IMethodReturnSequence<{delegateType}> {builderInterface}.ThenReturn({delegateType} callback) => ThenReturn(callback);");
			}
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
		int parameterCount,
		EquatableArray<ParameterModel> parameters,
		string? builderFriendlyName = null,
		string? sequenceFriendlyName = null)
	{
		var className = sequenceFriendlyName ?? (signatureSuffix == null ? "MethodSequenceImpl" : $"MethodSequenceImpl_{signatureSuffix}");
		var trackingClassName = builderFriendlyName ?? (signatureSuffix == null ? "MethodCallBuilderImpl" : $"MethodCallBuilderImpl_{signatureSuffix}");
		var sequenceField = signatureSuffix == null ? "_sequence" : $"_sequence_{signatureSuffix}";
		var sequenceIndexField = signatureSuffix == null ? "_sequenceIndex" : $"_sequenceIndex_{signatureSuffix}";
		var repeatLastValueField = signatureSuffix == null ? "_repeatLastValue" : $"_repeatLastValue_{signatureSuffix}";
		var verifiableField = signatureSuffix == null ? "_isVerifiable" : $"_isVerifiable_{signatureSuffix}";
		var verifiableTimesField = signatureSuffix == null ? "_verifiableTimes" : $"_verifiableTimes_{signatureSuffix}";
		var thenChainName = isVoid ? "ThenCall" : "ThenReturn";
		var sequenceInterface = isVoid
			? $"global::KnockOff.IMethodCallSequence<{delegateType}>"
			: $"global::KnockOff.IMethodReturnSequence<{delegateType}>";
		var sequenceBaseInterface = isVoid
			? "global::KnockOff.IMethodCallSequence"
			: "global::KnockOff.IMethodReturnSequence";

		w.Line($"/// <summary>Sequence implementation for {thenChainName} chaining.</summary>");
		w.Line($"public sealed class {className} : {sequenceInterface}");
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

			// ThenReturn/ThenCall - no Times parameter, each callback runs once
			w.Line($"/// <summary>Adds another callback to the sequence. Each callback runs exactly once.</summary>");
			w.Line($"public {className} {thenChainName}({delegateType} callback)");
			using (w.Braces())
			{
				w.Line($"var tracking = new {trackingClassName}(_interceptor);");
				w.Line($"_interceptor.{sequenceField}!.Add((callback, tracking));");
				w.Line("return this;");
			}
			w.Line();

			// ThenReturn(value) - value wrapper for ThenReturn(callback), only for non-void methods without ref/out
			if (!isVoid && !hasRefOrOut)
			{
				var (valueType, isTaskT, isValueTaskT) = GetAsyncTypeInfo(returnType);
				var discardPrefix = BuildDiscardLambdaPrefix(parameterCount);
				w.Line($"/// <summary>Adds a value to the sequence. The value is returned exactly once.</summary>");
				if (isTaskT)
				{
					w.Line($"public {className} ThenReturn({valueType} value) => ThenReturn({discardPrefix} => global::System.Threading.Tasks.Task.FromResult(value));");
				}
				else if (isValueTaskT)
				{
					w.Line($"public {className} ThenReturn({valueType} value) => ThenReturn({discardPrefix} => new global::System.Threading.Tasks.ValueTask<{valueType}>(value));");
				}
				else
				{
					w.Line($"public {className} ThenReturn({valueType} value) => ThenReturn({discardPrefix} => value);");
				}
				w.Line();

				// ThenReturn(params values) - adds multiple values to sequence
				w.Line($"/// <summary>Adds multiple values to the sequence. Each value returned once.</summary>");
				w.Line($"public {className} ThenReturn(params {valueType}[] values)");
				using (w.Braces())
				{
					w.Line("foreach (var value in values)");
					using (w.Braces())
					{
						w.Line("ThenReturn(value);");
					}
					w.Line("return this;");
				}
				w.Line();

				// Simplified async ThenReturn(Func<..., T>) - for Task<T>/ValueTask<T> methods
				if ((isTaskT || isValueTaskT) && !hasRefOrOut)
				{
					var simplifiedDelegateType = BuildSimplifiedDelegateType(parameters, valueType);
					// All delegates (custom and simplified) use individual params.
					var seqWrapperLambdaParamDecls = BuildDelegateMatchingParamDecls(parameters);
					var seqCallArgs = BuildDelegateMatchingCallArgs(parameters);
					var seqWrapperCallbackCall = parameters.Count == 0 ? "callback()" : $"callback({seqCallArgs})";
					w.Line($"/// <summary>Adds simplified callback to the sequence. Result auto-wrapped in {(isTaskT ? "Task.FromResult" : "new ValueTask")}.</summary>");
					if (isTaskT)
					{
						w.Line($"public {className} ThenReturn({simplifiedDelegateType} callback) => ThenReturn({seqWrapperLambdaParamDecls} => global::System.Threading.Tasks.Task.FromResult({seqWrapperCallbackCall}));");
					}
					else
					{
						w.Line($"public {className} ThenReturn({simplifiedDelegateType} callback) => ThenReturn({seqWrapperLambdaParamDecls} => new global::System.Threading.Tasks.ValueTask<{valueType}>({seqWrapperCallbackCall}));");
					}
					w.Line();
				}
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

			// Explicit interface implementations for IMethodReturnSequence<T> / IMethodCallSequence<T>
			if (isVoid)
			{
				w.Line($"global::KnockOff.IMethodCallSequence<{delegateType}> global::KnockOff.IMethodCallSequence<{delegateType}>.ThenCall({delegateType} callback) => ThenCall(callback);");
				w.Line($"global::KnockOff.IMethodCallSequence<{delegateType}> global::KnockOff.IMethodCallSequence<{delegateType}>.Verifiable() => Verifiable();");
			}
			else
			{
				w.Line($"global::KnockOff.IMethodReturnSequence<{delegateType}> global::KnockOff.IMethodReturnSequence<{delegateType}>.ThenReturn({delegateType} callback) => ThenReturn(callback);");
				w.Line($"global::KnockOff.IMethodReturnSequence<{delegateType}> global::KnockOff.IMethodReturnSequence<{delegateType}>.Verifiable() => Verifiable();");
			}

			// IMethodSequence.Verifiable() (base interface - IMethodReturnSequence/IMethodCallSequence are marker interfaces with no members)
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
	/// <param name="parameters">Method parameters for Matches/Call signatures.</param>
	/// <param name="returnType">Return type for Call method.</param>
	/// <param name="delegateType">Delegate type for callbacks.</param>
	/// <param name="signatureSuffix">Suffix for overload groups, null for single-signature.</param>
	private static void RenderWhenMatcherClasses(
		CodeWriter w,
		EquatableArray<ParameterModel> parameters,
		string returnType,
		string delegateType,
		string? signatureSuffix,
		string? predicateFriendlyName = null)
	{
		var suffix = signatureSuffix == null ? "" : $"_{signatureSuffix}";
		var matchParams = BuildMatchParams(parameters);
		var callParams = BuildMatchParams(parameters);
		// All delegates now use individual parameters (custom named delegates).
		var callbackInvokeArgs = BuildDelegateCallArgs(parameters);
		var predicateCallArgs = BuildPredicateCallArgs(parameters);
		var predicateType = BuildPredicateType(parameters, predicateFriendlyName);

		// WhenMatcher abstract base
		w.Line($"/// <summary>Abstract base for When chain matchers.</summary>");
		w.Line($"private abstract class WhenMatcher{suffix}");
		using (w.Braces())
		{
			w.Line($"public abstract bool Matches({matchParams});");
			w.Line($"public abstract {returnType} Call({callParams});");
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
			w.Line($"public override bool Matches({matchParams}) => _predicate({predicateCallArgs});");
			w.Line($"public override {returnType} Call({callParams}) => _value;");
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
			w.Line($"public override {returnType} Call({callParams}) => _callback({callbackInvokeArgs});");
			w.Line("public override bool IsTerminal => true;");
		}
		w.Line();

		// WhenMatcherNone - never matches, terminal
		w.Line($"/// <summary>Matcher that never matches. Used to close chain without fallback. Terminal.</summary>");
		w.Line($"private sealed class WhenMatcherNone{suffix} : WhenMatcher{suffix}");
		using (w.Braces())
		{
			w.Line($"public override bool Matches({matchParams}) => false;");
			w.Line($"public override {returnType} Call({callParams}) => default!;");
			w.Line("public override bool IsTerminal => true;");
		}
		w.Line();
	}

	/// <summary>
	/// Renders the WhenBuilderImpl nested class.
	/// Holds a pending predicate and exposes Return(value) to complete the matcher.
	/// </summary>
	private static void RenderWhenBuilderImpl(
		CodeWriter w,
		string interceptorClassName,
		EquatableArray<ParameterModel> parameters,
		string returnType,
		string delegateType,
		string? signatureSuffix,
		string? predicateFriendlyName = null)
	{
		var suffix = signatureSuffix == null ? "" : $"_{signatureSuffix}";
		var predicateType = BuildPredicateType(parameters, predicateFriendlyName);
		var whenChainField = signatureSuffix == null ? "_whenChain" : $"_whenChain_{signatureSuffix}";

		// Check if this is an async method (Task<T> or ValueTask<T>)
		var (innerType, isTaskT, isValueTaskT) = GetAsyncTypeInfo(returnType);
		var isAsync = isTaskT || isValueTaskT;

		w.Line($"/// <summary>Builder for When matchers. Captures predicate, awaits Return(value).</summary>");
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

			// For async methods (Task<T>/ValueTask<T>), generate Return(TInner) that auto-wraps
			if (isAsync)
			{
				// Return accepts the unwrapped type and wraps internally
				w.Line($"/// <summary>Configures the return value. Auto-wrapped in {(isTaskT ? "Task.FromResult" : "new ValueTask")}.</summary>");
				w.Line($"public WhenChain{suffix} Return({innerType} value)");
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
					w.Line($"global::KnockOff.IWhenChain<{delegateType}, {returnType}> global::KnockOff.IWhenBuilder<{delegateType}, {returnType}>.Return({returnType} value) => Return(value.Result);");
				else
					w.Line($"global::KnockOff.IWhenChain<{delegateType}, {returnType}> global::KnockOff.IWhenBuilder<{delegateType}, {returnType}>.Return({returnType} value) => Return(value.Result);");
			}
			else
			{
				// Non-async: Return accepts the full return type directly
				w.Line($"public WhenChain{suffix} Return({returnType} value)");
				using (w.Braces())
				{
					w.Line($"_interceptor.{whenChainField} ??= new global::System.Collections.Generic.List<WhenMatcher{suffix}>();");
					w.Line($"_interceptor.{whenChainField}.Add(new WhenMatcherValue{suffix}(_predicate, value));");
					w.Line($"return new WhenChain{suffix}(_interceptor);");
				}
				w.Line();
				// Explicit interface implementation for IWhenBuilder.Return
				w.Line($"global::KnockOff.IWhenChain<{delegateType}, {returnType}> global::KnockOff.IWhenBuilder<{delegateType}, {returnType}>.Return({returnType} value) => Return(value);");
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
		string? signatureSuffix,
		string? predicateFriendlyName = null)
	{
		var suffix = signatureSuffix == null ? "" : $"_{signatureSuffix}";
		var whenChainField = signatureSuffix == null ? "_whenChain" : $"_whenChain_{signatureSuffix}";
		var whenChainHeadField = signatureSuffix == null ? "_whenChainHead" : $"_whenChainHead_{signatureSuffix}";
		var whenVerifiableField = signatureSuffix == null ? "_whenVerifiable" : $"_whenVerifiable_{signatureSuffix}";
		var predicateType = BuildPredicateType(parameters, predicateFriendlyName);
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

				// ThenWhen with predicate - custom predicate delegate for 2+ params
				w.Line($"/// <summary>Adds another matcher with predicate matching.</summary>");
				w.Line($"public WhenBuilder{suffix} ThenWhen({predicateType} predicate)");
				using (w.Braces())
				{
					// Predicate type matches WhenBuilder constructor type directly
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
				w.Line("if (head < count && !_interceptor." + whenChainField + "[head].IsTerminal && _interceptor." + whenChainField + "[head].CallCount == 0)");
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
		string? methodNameSuffix = null,
		string? methodName = null,
		string? xmlDocSummary = null,
		string? predicateFriendlyName = null)
	{
		// When() requires parameters - parameterless methods cannot use When()
		if (parameters.Count == 0) return;

		var suffix = signatureSuffix == null ? "" : $"_{signatureSuffix}";
		var whenChainField = signatureSuffix == null ? "_whenChain" : $"_whenChain_{signatureSuffix}";
		var predicateType = UnifiedInterceptorBuilder.BuildWhenPredicateType(parameters, predicateFriendlyName);
		var paramTypeList = BuildParamTypeList(parameters);
		var whenMethodName = methodNameSuffix == null ? "When" : $"When_{methodNameSuffix}";

		// When() value overload - exact value matching via Object.Equals
		// Returns concrete type to enable fluent ThenWhen chaining
		EmitWhenXmlDoc(w, methodName, parameters, xmlDocSummary, returnType, false, "Matches exact values using Object.Equals. Returns builder for Return().");
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

		// When() predicate overload - custom predicate delegate for 2+ params, Func<T, bool> for 0-1
		// Returns concrete type to enable fluent ThenWhen chaining
		EmitWhenXmlDoc(w, methodName, parameters, xmlDocSummary, returnType, false, "Matches using predicate. Returns builder for Return().");
		w.Line($"public WhenBuilder{suffix} {whenMethodName}({predicateType} predicate)");
		using (w.Braces())
		{
			// Initialize When chain if null
			w.Line($"{whenChainField} ??= new global::System.Collections.Generic.List<WhenMatcher{suffix}>();");
			// Predicate type matches WhenBuilder constructor type directly (custom predicate delegate for 2+, Func<T, bool> for 0-1)
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
	/// Builds the predicate type for When matching.
	/// 0 params: Func&lt;bool&gt;, 1 param: Func&lt;T1, bool&gt;, 2+ params: custom predicate delegate name if available, else Func&lt;(T1 a, T2 b), bool&gt;.
	/// </summary>
	private static string BuildPredicateType(EquatableArray<ParameterModel> parameters, string? predicateFriendlyName = null)
	{
		if (parameters.Count == 0)
			return "global::System.Func<bool>";

		if (parameters.Count == 1)
			return $"global::System.Func<{parameters.GetArray()![0].Type}, bool>";

		// 2+ params: use custom predicate delegate name if available
		if (predicateFriendlyName != null)
			return predicateFriendlyName;

		// Fallback: named tuple (for internal matcher classes that still use tuple-based predicate)
		var tupleType = "(" + string.Join(", ", parameters.Select(p => $"{p.Type} {p.EscapedName}")) + ")";
		return $"global::System.Func<{tupleType}, bool>";
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
	/// Builds lambda parameter names for equality comparisons.
	/// For 1 param: "_arg0" (indexed to avoid keyword conflicts).
	/// For 2+ params: "_args" (single tuple parameter, access fields via _args.fieldName).
	/// </summary>
	private static string BuildLambdaParamsForEquality(EquatableArray<ParameterModel> parameters)
	{
		if (parameters.Count == 0) return "";
		if (parameters.Count == 1) return "_arg0";
		// 2+ params: individual parameters matching custom predicate delegate
		return string.Join(", ", Enumerable.Range(0, parameters.Count).Select(i => $"_p{i}"));
	}

	/// <summary>
	/// Builds an equality predicate body comparing lambda params to method params.
	/// For 1 param: "Equals(_arg0, a)".
	/// For 2+ params: "Equals(_args.a, a) &amp;&amp; Equals(_args.b, b)" (accessing tuple fields).
	/// Uses Object.Equals for null-safety.
	/// </summary>
	private static string BuildEqualityPredicateBody(EquatableArray<ParameterModel> parameters)
	{
		if (parameters.Count == 0) return "true";

		if (parameters.Count == 1)
		{
			return $"global::System.Object.Equals(_arg0, {parameters.GetArray()![0].EscapedName})";
		}

		// 2+ params: individual lambda params (_p0, _p1, ...)
		var parts = new List<string>();
		for (int i = 0; i < parameters.Count; i++)
		{
			var p = parameters.GetArray()![i];
			parts.Add($"global::System.Object.Equals(_p{i}, {p.EscapedName})");
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
		string? methodNameSuffix = null,
		string? methodName = null,
		string? xmlDocSummary = null,
		string? predicateFriendlyName = null)
	{
		// When() requires parameters - parameterless methods cannot use When()
		if (parameters.Count == 0) return;

		var suffix = signatureSuffix == null ? "" : $"_{signatureSuffix}";
		var whenChainField = signatureSuffix == null ? "_whenChain" : $"_whenChain_{signatureSuffix}";
		var predicateType = UnifiedInterceptorBuilder.BuildWhenPredicateType(parameters, predicateFriendlyName);
		var paramTypeList = BuildParamTypeList(parameters);
		var whenMethodName = methodNameSuffix == null ? "When" : $"When_{methodNameSuffix}";
		var chainType = $"VoidWhenChain{suffix}";

		// When() value overload - exact value matching via Object.Equals
		// Returns concrete type to enable fluent ThenWhen chaining
		EmitWhenXmlDoc(w, methodName, parameters, xmlDocSummary, extraSummary: "Matches exact values using Object.Equals. Returns chain directly.");
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

		// When() predicate overload - custom predicate delegate for 2+ params, Func<T, bool> for 0-1
		// Returns concrete type to enable fluent ThenWhen chaining
		EmitWhenXmlDoc(w, methodName, parameters, xmlDocSummary, extraSummary: "Matches using predicate. Returns chain directly.");
		w.Line($"public {chainType} {whenMethodName}({predicateType} predicate)");
		using (w.Braces())
		{
			// Initialize When chain if null
			w.Line($"{whenChainField} ??= new global::System.Collections.Generic.List<VoidWhenMatcher{suffix}>();");
			// Predicate type matches VoidWhenMatcherPredicate constructor type directly
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

				// Call (void) and return - no return value
				w.Line($"matcher.Call({callbackArgs});");
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
		string? signatureSuffix,
		string? predicateFriendlyName = null)
	{
		var suffix = signatureSuffix == null ? "" : $"_{signatureSuffix}";
		var matchParams = BuildMatchParams(parameters);
		// All delegates now use individual parameters (custom named delegates).
		var callbackInvokeArgs = BuildDelegateCallArgs(parameters);
		var predicateCallArgs = BuildPredicateCallArgs(parameters);
		var predicateType = BuildPredicateType(parameters, predicateFriendlyName);

		// VoidWhenMatcher abstract base
		w.Line($"/// <summary>Abstract base for void When chain matchers.</summary>");
		w.Line($"internal abstract class VoidWhenMatcher{suffix}");
		using (w.Braces())
		{
			w.Line($"public abstract bool Matches({matchParams});");
			w.Line($"public abstract void Call({matchParams});");
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
			w.Line($"public override bool Matches({matchParams}) => _predicate({predicateCallArgs});");
			w.Line($"public override void Call({matchParams}) {{ Callback?.Invoke({callbackInvokeArgs}); }}");
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
			w.Line($"public override void Call({matchParams}) => _callback({callbackInvokeArgs});");
			w.Line("public override bool IsTerminal => true;");
		}
		w.Line();

		// VoidWhenMatcherNone - never matches, terminal
		w.Line($"/// <summary>Matcher that never matches. Used to close chain without fallback. Terminal.</summary>");
		w.Line($"private sealed class VoidWhenMatcherNone{suffix} : VoidWhenMatcher{suffix}");
		using (w.Braces())
		{
			w.Line($"public override bool Matches({matchParams}) => false;");
			w.Line($"public override void Call({matchParams}) {{ }}");
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
		string? signatureSuffix,
		string? predicateFriendlyName = null)
	{
		var suffix = signatureSuffix == null ? "" : $"_{signatureSuffix}";
		var whenChainField = signatureSuffix == null ? "_whenChain" : $"_whenChain_{signatureSuffix}";
		var whenChainHeadField = signatureSuffix == null ? "_whenChainHead" : $"_whenChainHead_{signatureSuffix}";
		var whenVerifiableField = signatureSuffix == null ? "_whenVerifiable" : $"_whenVerifiable_{signatureSuffix}";
		var predicateType = BuildPredicateType(parameters, predicateFriendlyName);
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

				// ThenWhen with predicate - custom predicate delegate for 2+ params
				w.Line($"/// <summary>Adds another matcher with predicate matching.</summary>");
				w.Line($"public {chainType} ThenWhen({predicateType} predicate)");
				using (w.Braces())
				{
					w.Line($"_interceptor.{whenChainField} ??= new global::System.Collections.Generic.List<VoidWhenMatcher{suffix}>();");
					// Predicate type matches VoidWhenMatcherPredicate constructor type directly
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
				w.Line("if (head < count && !_interceptor." + whenChainField + "[head].IsTerminal && _interceptor." + whenChainField + "[head].CallCount == 0)");
				using (w.Braces())
				{
					w.Line("throw new global::KnockOff.VerificationException(global::KnockOff.VerificationFailure.SequenceIncomplete(\"When chain\", count, head));");
				}
			}
			w.Line();

			// Verify(Times) - parameter-specific verification for current matcher
			w.Line($"/// <summary>Verifies this specific matcher was called the expected number of times.</summary>");
			w.Line("public void Verify(global::KnockOff.Called times)");
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

	/// <summary>
	/// Emits the sequence elevation block used by ThenReturn/ThenCall and ThenReturn(params) when
	/// the sequence has not yet been created. Handles three cases where _call may be null:
	/// Return(value), Return(simplifiedCallback), and Return(simplifiedVoidCallback).
	/// </summary>
	private static void EmitSequenceElevation(
		CodeWriter w,
		string sequenceFieldName,
		string delegateType,
		string className,
		string callFieldName,
		string callTrackingFieldName,
		string sequenceIndexFieldName,
		bool canHaveValueOverload,
		bool isAsyncWithInnerType,
		bool isVoidAsync,
		bool isTaskT,
		bool isValueTaskT,
		bool isVoidTask,
		bool isVoidValueTask,
		string innerType,
		string callSimplifiedFieldName,
		string callSimplifiedTrackingFieldName,
		string callSimplifiedVoidFieldName,
		string callSimplifiedVoidTrackingFieldName,
		bool hasRefOrOut,
		int parameterCount,
		EquatableArray<ParameterModel> parameters)
	{
		w.Line($"if (_interceptor.{sequenceFieldName} == null)");
		using (w.Braces())
		{
			w.Line($"_interceptor.{sequenceFieldName} = new global::System.Collections.Generic.List<({delegateType} Callback, {className} Tracking)>();");

			// Branch 1: _call is non-null (Return(callback) was used -- existing non-buggy path)
			w.Line($"if (_interceptor.{callFieldName} != null)");
			using (w.Braces())
			{
				w.Line($"_interceptor.{sequenceFieldName}.Add((_interceptor.{callFieldName}, this));");
			}

			// Branch 2: _hasReturnValue is true (Return(value) was used)
			// Only emitted for single-signature, non-void, no ref/out interceptors
			if (canHaveValueOverload)
			{
				var discardPrefix = BuildDiscardLambdaPrefix(parameterCount);
				w.Line("else if (_interceptor._hasReturnValue)");
				using (w.Braces())
				{
					w.Line("var capturedValue = _interceptor._returnValue;");
					if (isTaskT)
					{
						w.Line($"{delegateType} valueWrapper = {discardPrefix} => global::System.Threading.Tasks.Task.FromResult(capturedValue);");
					}
					else if (isValueTaskT)
					{
						w.Line($"{delegateType} valueWrapper = {discardPrefix} => new global::System.Threading.Tasks.ValueTask<{innerType}>(capturedValue);");
					}
					else
					{
						w.Line($"{delegateType} valueWrapper = {discardPrefix} => capturedValue;");
					}
					w.Line($"_interceptor.{sequenceFieldName}.Add((valueWrapper, this));");
					w.Line("_interceptor._hasReturnValue = false;");
					w.Line("_interceptor._returnValue = default!;");
					w.Line("_interceptor._returnValueTracking = null;");
				}
			}

			// Branch 3: _callSimplified is non-null (Return(simplifiedCallback) was used for Task<T>/ValueTask<T>)
			// Only emitted when method is Task<T> or ValueTask<T> and has no ref/out params
			if (isAsyncWithInnerType && !hasRefOrOut)
			{
				// Custom delegate with individual params -- bridge to simplified callback
				var paramDecls = BuildDelegateMatchingParamDecls(parameters);
				var simplifiedCallArgs = BuildDelegateMatchingCallArgs(parameters);
				var lambdaCall = parameters.Count == 0 ? "captured()" : $"captured({simplifiedCallArgs})";
				string wrapperLambda;
				if (isTaskT)
					wrapperLambda = $"{paramDecls} => global::System.Threading.Tasks.Task.FromResult({lambdaCall})";
				else
					wrapperLambda = $"{paramDecls} => new global::System.Threading.Tasks.ValueTask<{innerType}>({lambdaCall})";
				w.Line($"else if (_interceptor.{callSimplifiedFieldName} != null)");
				using (w.Braces())
				{
					w.Line($"var captured = _interceptor.{callSimplifiedFieldName};");
					w.Line($"{delegateType} wrapper = {wrapperLambda};");
					w.Line($"_interceptor.{sequenceFieldName}.Add((wrapper, this));");
					w.Line($"_interceptor.{callSimplifiedFieldName} = null;");
					w.Line($"_interceptor.{callSimplifiedTrackingFieldName} = null;");
				}
			}

			// Branch 4: _callSimplifiedVoid is non-null (Return(simplifiedVoidCallback) was used for Task/ValueTask void)
			// Only emitted when method is void Task or void ValueTask and has no ref/out params
			if (isVoidAsync && !hasRefOrOut)
			{
				// Custom delegate with individual params -- bridge to simplified void callback
				var paramDecls = BuildDelegateMatchingParamDecls(parameters);
				var simplifiedCallArgs = BuildDelegateMatchingCallArgs(parameters);
				var lambdaCall = parameters.Count == 0 ? "captured()" : $"captured({simplifiedCallArgs})";
				string wrapperLambda;
				if (isVoidTask)
					wrapperLambda = $"{paramDecls} => {{ {lambdaCall}; return global::System.Threading.Tasks.Task.CompletedTask; }}";
				else
					wrapperLambda = $"{paramDecls} => {{ {lambdaCall}; return default; }}";
				w.Line($"else if (_interceptor.{callSimplifiedVoidFieldName} != null)");
				using (w.Braces())
				{
					w.Line($"var captured = _interceptor.{callSimplifiedVoidFieldName};");
					w.Line($"{delegateType} voidWrapper = {wrapperLambda};");
					w.Line($"_interceptor.{sequenceFieldName}.Add((voidWrapper, this));");
					w.Line($"_interceptor.{callSimplifiedVoidFieldName} = null;");
					w.Line($"_interceptor.{callSimplifiedVoidTrackingFieldName} = null;");
				}
			}

			w.Line($"_interceptor.{callFieldName} = null;");
			w.Line($"_interceptor.{callTrackingFieldName} = null;");
			w.Line($"_interceptor.{sequenceIndexFieldName} = 0;");
		}
	}

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
			var scopedPrefix = p.IsScoped ? "scoped " : "";
			parts.Add($"{scopedPrefix}{p.RefPrefix}{p.Type} {p.EscapedName}");
		}
		return string.Join(", ", parts);
	}

	private static string BuildCallbackArgs(EquatableArray<ParameterModel> parameters)
	{
		var parts = new List<string>();
		foreach (var p in parameters)
		{
			// Only include ref/out at call sites - 'in' and 'ref readonly' are not valid
			// at call sites for Action/Func delegates which don't declare these modifiers.
			var callSitePrefix = p.RefKind switch
			{
				Microsoft.CodeAnalysis.RefKind.Ref => "ref ",
				Microsoft.CodeAnalysis.RefKind.Out => "out ",
				_ => ""
			};
			parts.Add($"{callSitePrefix}{p.EscapedName}");
		}
		return string.Join(", ", parts);
	}

	/// <summary>
	/// Builds invocation args for calling a delegate field.
	/// All delegates now use individual parameters (custom named delegates),
	/// so args are never wrapped in tuple syntax.
	/// </summary>
	private static string BuildDelegateCallArgs(EquatableArray<ParameterModel> parameters)
	{
		return BuildCallbackArgs(parameters);
	}

	/// <summary>
	/// Builds the invocation args for a predicate call.
	/// All predicates now use individual parameters (custom delegates for 2+ params),
	/// so args are never wrapped in tuple syntax.
	/// 0 params: "" (no args), 1 param: "a", 2+ params: "a, b" (individual args).
	/// </summary>
	private static string BuildPredicateCallArgs(EquatableArray<ParameterModel> parameters)
	{
		if (parameters.Count == 0) return "";
		if (parameters.Count == 1) return parameters.GetArray()![0].EscapedName;
		// 2+ params: individual args
		return string.Join(", ", parameters.Select(p => p.EscapedName));
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
	/// 0 params: Func&lt;TInner&gt;, 1 param: Func&lt;T1, TInner&gt;, 2+ params: Func&lt;(T1 a, T2 b), TInner&gt;.
	/// </summary>
	private static string BuildSimplifiedDelegateType(EquatableArray<ParameterModel> parameters, string innerType)
	{
		if (parameters.Count == 0)
			return $"global::System.Func<{innerType}>";

		if (parameters.Count == 1)
			return $"global::System.Func<{parameters.GetArray()![0].Type}, {innerType}>";

		// 2+ params: individual type params (Func<T1, T2, ..., TReturn>)
		var typeParams = string.Join(", ", parameters.Select(p => p.Type));
		return $"global::System.Func<{typeParams}, {innerType}>";
	}

	/// <summary>
	/// Builds the simplified void callback delegate type for Task/ValueTask methods.
	/// 0 params: Action, 1 param: Action&lt;T1&gt;, 2+ params: Action&lt;(T1 a, T2 b)&gt;.
	/// </summary>
	private static string BuildSimplifiedVoidDelegateType(EquatableArray<ParameterModel> parameters)
	{
		if (parameters.Count == 0)
			return "global::System.Action";

		if (parameters.Count == 1)
			return $"global::System.Action<{parameters.GetArray()![0].Type}>";

		// 2+ params: individual type params (Action<T1, T2, ...>)
		var typeParams = string.Join(", ", parameters.Select(p => p.Type));
		return $"global::System.Action<{typeParams}>";
	}

	// ========================================================================
	// New-style Func<>/Action<> delegate type computation for MethodInterceptorRuntime
	// ========================================================================

	/// <summary>
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
	/// 0 params: "()", 1 param: "(_)", 2+ params: "(_, _, ...)".
	/// </summary>
	/// <param name="parameterCount">Number of method parameters.</param>
	private static string BuildDiscardLambdaPrefix(int parameterCount)
	{
		if (parameterCount == 0)
			return "()";
		if (parameterCount == 1)
			return "(_)"; // Single param
		// Multiple individual params
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
		bool hasSimplifiedVoidCallback = false,
		bool hasWhenChain = false)
	{
		// CallCount - total across Return/Call + Return(value) + simplified + sequence + When chain + unconfigured (private - use Verify() API to check call counts)
		// Include value tracking when value overload exists, and simplified callback tracking when present
		var valueTrackingPart = hasValueOverload ? " + (_returnValueTracking?._callCount ?? 0)" : "";
		var simplifiedTrackingPart = hasSimplifiedCallback ? " + (_callSimplifiedTracking?._callCount ?? 0)" : "";
		var simplifiedVoidTrackingPart = hasSimplifiedVoidCallback ? " + (_callSimplifiedVoidTracking?._callCount ?? 0)" : "";
		var whenChainPart = hasWhenChain ? " if (_whenChain != null) foreach (var m in _whenChain) sum += m.CallCount;" : "";
		w.Line($"private int TotalCallCount {{ get {{ var sum = _unconfiguredCallCount + (_callTracking?._callCount ?? 0){valueTrackingPart}{simplifiedTrackingPart}{simplifiedVoidTrackingPart}; if (_sequence != null) foreach (var s in _sequence) sum += s.Tracking._callCount;{whenChainPart} return sum; }} }}");
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
				getterParts.Add("if ((_returnValueTracking?._callCount ?? 0) > 0) return _returnValueTracking!.LastArg;");
			if (hasSimplifiedCallback)
				getterParts.Add("if ((_callSimplifiedTracking?._callCount ?? 0) > 0) return _callSimplifiedTracking!.LastArg;");
			if (hasSimplifiedVoidCallback)
				getterParts.Add("if ((_callSimplifiedVoidTracking?._callCount ?? 0) > 0) return _callSimplifiedVoidTracking!.LastArg;");
			getterParts.Add("if ((_callTracking?._callCount ?? 0) > 0) return _callTracking!.LastArg;");
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
				getterParts.Add("if ((_returnValueTracking?._callCount ?? 0) > 0) return _returnValueTracking!.LastArgs;");
			if (hasSimplifiedCallback)
				getterParts.Add("if ((_callSimplifiedTracking?._callCount ?? 0) > 0) return _callSimplifiedTracking!.LastArgs;");
			if (hasSimplifiedVoidCallback)
				getterParts.Add("if ((_callSimplifiedVoidTracking?._callCount ?? 0) > 0) return _callSimplifiedVoidTracking!.LastArgs;");
			getterParts.Add("if ((_callTracking?._callCount ?? 0) > 0) return _callTracking!.LastArgs;");
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
		// Include value tracking and simplified callback tracking for async overloads
		var sumParts = new List<string>();
		foreach (var overload in overloads)
		{
			sumParts.Add($"(_callTracking_{overload.SignatureSuffix}?._callCount ?? 0)");
			sumParts.Add($"(_sequence_{overload.SignatureSuffix}?.Sum(s => s.Tracking._callCount) ?? 0)");
			// Add value tracking for non-void overloads
			var hasRefOrOut = HasRefOrOutParameters(overload.Parameters);
			if (!overload.IsVoid && !hasRefOrOut)
				sumParts.Add($"(_returnValueTracking_{overload.SignatureSuffix}?._callCount ?? 0)");
			// Add simplified tracking for async overloads
			var (_, isTaskT, isValueTaskT) = GetAsyncTypeInfo(overload.ReturnType);
			if ((isTaskT || isValueTaskT) && !hasRefOrOut)
				sumParts.Add($"(_callSimplifiedTracking_{overload.SignatureSuffix}?._callCount ?? 0)");
			var (isVoidTask, isVoidValueTask) = GetVoidAsyncInfo(overload.ReturnType);
			if ((isVoidTask || isVoidValueTask) && !hasRefOrOut)
				sumParts.Add($"(_callSimplifiedVoidTracking_{overload.SignatureSuffix}?._callCount ?? 0)");
			// Add When chain call counts for eligible overloads
			var canHaveWhenChainForOverload = !overload.IsVoid && overload.Parameters.Count > 0 && !hasRefOrOut;
			var canHaveVoidWhenChainForOverload = overload.IsVoid && overload.Parameters.Count > 0 && !hasRefOrOut;
			if (canHaveWhenChainForOverload || canHaveVoidWhenChainForOverload)
				sumParts.Add($"(_whenChain_{overload.SignatureSuffix}?.Sum(m => m.CallCount) ?? 0)");
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
		w.Line("public void Verify() => Verify(global::KnockOff.Called.AtLeastOnce);");
		w.Line();

		w.Line("/// <summary>Verifies call count satisfies the Called constraint. Throws VerificationException if not.</summary>");
		w.Line("public void Verify(global::KnockOff.Called times)");
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

			w.Line("/// <summary>Marks for verification by Stub.Verify() with Called constraint.</summary>");
			w.Line("public void Verifiable(global::KnockOff.Called times)");
			using (w.Braces())
			{
				w.Line("_isVerifiable = true;");
				w.Line("_verifiableTimes = times;");
			}
			w.Line();
		}
	}

	#endregion

	#region XML Documentation Helpers

	/// <summary>
	/// Builds a method signature string for XML doc summaries.
	/// E.g., "DoWork(int id, string name)" or "GetCount()".
	/// Uses shortened type names for readability.
	/// </summary>
	internal static string FormatMethodSignatureForDoc(string methodName, EquatableArray<ParameterModel> parameters, string? returnType = null, bool isVoid = true)
	{
		string paramPart;
		if (parameters.Count == 0)
			paramPart = $"{methodName}()";
		else
		{
			var paramList = string.Join(", ", parameters.Select(p =>
			{
				var shortType = ShortenTypeForDoc(p.Type);
				return $"{p.RefPrefix}{shortType} {p.Name}";
			}));
			paramPart = $"{methodName}({paramList})";
		}

		// Append return type for non-void methods
		if (!isVoid && returnType != null)
		{
			var shortReturn = ShortenTypeForDoc(returnType);
			return $"{paramPart} -> {shortReturn}";
		}

		return paramPart;
	}

	/// <summary>
	/// Shortens a fully qualified type name for display in XML doc comments.
	/// </summary>
	internal static string ShortenTypeForDoc(string type)
	{
		var result = type;
		if (result.StartsWith("global::"))
			result = result.Substring(8);

		// Map common System types to keywords
		result = result switch
		{
			"System.Int32" => "int",
			"System.Int64" => "long",
			"System.Int16" => "short",
			"System.Byte" => "byte",
			"System.SByte" => "sbyte",
			"System.UInt32" => "uint",
			"System.UInt64" => "ulong",
			"System.UInt16" => "ushort",
			"System.Single" => "float",
			"System.Double" => "double",
			"System.Decimal" => "decimal",
			"System.Boolean" => "bool",
			"System.Char" => "char",
			"System.String" => "string",
			"System.Object" => "object",
			"System.Void" => "void",
			_ => result
		};

		return result;
	}

	/// <summary>
	/// Emits XML doc comments for a Call() method on the interceptor.
	/// Includes method signature in summary and parameter-level docs in the callback param tag.
	/// </summary>
	private static void EmitCallXmlDoc(CodeWriter w, string methodName, EquatableArray<ParameterModel> parameters, string? xmlDocSummary, string returnType, bool isVoid, string? extraSummary = null)
	{
		var sig = FormatMethodSignatureForDoc(methodName, parameters, returnType, isVoid);
		var summaryText = xmlDocSummary != null
			? $"Configures callback for {sig}. {xmlDocSummary}"
			: $"Configures callback for {sig}.";
		if (extraSummary != null)
			summaryText += $" {extraSummary}";
		w.Line($"/// <summary>{summaryText}</summary>");

		// Emit <param> for the callback parameter with per-parameter docs
		EmitCallbackParamDoc(w, "callback", parameters);
	}

	/// <summary>
	/// Emits XML doc comments for a Return() method on the interceptor.
	/// Includes method signature in summary.
	/// </summary>
	private static void EmitReturnXmlDoc(CodeWriter w, string methodName, EquatableArray<ParameterModel> parameters, string? xmlDocSummary, string returnType, bool isVoid, string? extraSummary = null)
	{
		var sig = FormatMethodSignatureForDoc(methodName, parameters, returnType, isVoid);
		var summaryText = xmlDocSummary != null
			? $"Sets return value for {sig}. {xmlDocSummary}"
			: $"Sets return value for {sig}.";
		if (extraSummary != null)
			summaryText += $" {extraSummary}";
		w.Line($"/// <summary>{summaryText}</summary>");
		w.Line("/// <param name=\"value\">The value to return.</param>");
	}

	/// <summary>
	/// Emits a &lt;param&gt; XML doc tag for a callback parameter, including per-parameter
	/// documentation from the original interface/class if available.
	/// </summary>
	private static void EmitCallbackParamDoc(CodeWriter w, string paramName, EquatableArray<ParameterModel> parameters)
	{
		var hasAnyDocs = parameters.Any(p => p.XmlDoc != null);
		if (!hasAnyDocs || parameters.Count == 0)
		{
			// No parameter-level docs available; emit simple param tag
			return;
		}

		if (parameters.Count == 1)
		{
			var p = parameters.GetArray()![0];
			var shortType = ShortenTypeForDoc(p.Type);
			var docText = p.XmlDoc != null
				? $"Callback for {p.Name} ({shortType}): {p.XmlDoc}"
				: $"Callback for {p.Name} ({shortType}).";
			w.Line($"/// <param name=\"{paramName}\">{docText}</param>");
		}
		else
		{
			// Multi-param: list each parameter
			w.Line($"/// <param name=\"{paramName}\">");
			w.Line($"/// Callback receiving ({string.Join(", ", parameters.Select(p => p.Name))}) parameters.");
			foreach (var p in parameters)
			{
				var shortType = ShortenTypeForDoc(p.Type);
				var docSuffix = p.XmlDoc != null ? $": {p.XmlDoc}" : "";
				w.Line($"/// - {p.Name} ({shortType}){docSuffix}");
			}
			w.Line("/// </param>");
		}
	}

	/// <summary>
	/// Emits XML doc comments for a When() method on the interceptor.
	/// Includes method signature in summary and parameter-level docs.
	/// </summary>
	private static void EmitWhenXmlDoc(CodeWriter w, string? methodName, EquatableArray<ParameterModel> parameters, string? xmlDocSummary, string? returnType = null, bool isVoid = true, string? extraSummary = null)
	{
		if (methodName != null)
		{
			var sig = FormatMethodSignatureForDoc(methodName, parameters, returnType, isVoid);
			var summaryText = xmlDocSummary != null
				? $"Configures parameter matching for {sig}. {xmlDocSummary}"
				: $"Configures parameter matching for {sig}.";
			if (extraSummary != null)
				summaryText += $" {extraSummary}";
			w.Line($"/// <summary>{summaryText}</summary>");
		}
		else
		{
			var summaryText = "Configures parameter-specific matching.";
			if (extraSummary != null)
				summaryText += $" {extraSummary}";
			w.Line($"/// <summary>{summaryText}</summary>");
		}
	}

	#endregion
}
