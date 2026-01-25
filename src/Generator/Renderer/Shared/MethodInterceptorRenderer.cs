// src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs
#nullable enable
using System.Collections.Generic;
using System.Linq;
using KnockOff.Builder;
using KnockOff.Model.Shared;

namespace KnockOff.Renderer.Shared;

/// <summary>
/// Renders method interceptor classes for both inline and flat stubs.
/// Generates OnCall() returning IMethodTracking (repeating callback),
/// OnCallSequence() returning IMethodSequence (for ThenCall chaining),
/// nested MethodTrackingImpl and MethodSequenceImpl classes, Invoke methods, and verification.
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
		w.Line("private MethodTrackingImpl? _onCallTracking;");
		w.Line();

		// Value storage for OnCall(value) overload (skip for void/ref/out methods)
		var hasRefOrOut = model.Parameters.Any(p => p.RefKind == Microsoft.CodeAnalysis.RefKind.Ref || p.RefKind == Microsoft.CodeAnalysis.RefKind.Out);
		var canHaveValueOverload = !model.IsVoid && !hasRefOrOut;
		if (canHaveValueOverload)
		{
			var (valueStorageType, isTaskT, isValueTaskT) = GetAsyncTypeInfo(model.ReturnType);
			// Use non-nullable storage with default! - _hasOnCallValue distinguishes "not set" from "set to null/default"
			w.Line($"private {valueStorageType} _onCallValue = default!;");
			w.Line("private bool _hasOnCallValue;");
			w.Line("private MethodTrackingImpl? _onCallValueTracking;");
			w.Line();
		}

		// Sequence storage - list of callbacks that each run once
		w.Line($"private global::System.Collections.Generic.List<({delegateType} Callback, MethodTrackingImpl Tracking)>? _sequence;");
		w.Line("private int _sequenceIndex;");
		w.Line();

		// Verifiable state
		w.Line("private bool _isVerifiable;");
		w.Line("private global::KnockOff.Times? _verifiableTimes;");
		w.Line();

		// Track unconfigured calls
		w.Line("private int _unconfiguredCallCount;");
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
		RenderBackwardCompatibleTrackingProperties(w, model.TrackableParameters, model.LastArgType, model.LastArgsType, canHaveValueOverload);
		w.Line();

		// Verify() methods for direct interceptor verification
		RenderInterceptorVerifyMethods(w, model.MethodName);

		// OnCall() - repeating callback, returns IMethodTracking
		w.Line($"/// <summary>Configures callback that repeats indefinitely. Returns tracking interface for LastArg access.</summary>");
		w.Line($"public {model.TrackingInterface} OnCall({delegateType} callback)");
		using (w.Braces())
		{
			w.Line("_sequence = null;");
			w.Line("_sequenceIndex = 0;");
			w.Line("_isVerifiable = false;");
			w.Line("_verifiableTimes = null;");
			// Clear value storage (mutual exclusivity with OnCall(value))
			if (canHaveValueOverload)
			{
				w.Line("_hasOnCallValue = false;");
				w.Line("_onCallValue = default!;");
				w.Line("_onCallValueTracking = null;");
			}
			w.Line("_onCall = callback;");
			w.Line("_onCallTracking = new MethodTrackingImpl(this);");
			w.Line("return _onCallTracking;");
		}
		w.Line();

		// OnCall(value) - repeating value return, returns IMethodTracking
		if (canHaveValueOverload)
		{
			var (valueStorageType, isTaskT, isValueTaskT) = GetAsyncTypeInfo(model.ReturnType);
			w.Line($"/// <summary>Configures return value that repeats indefinitely. Returns tracking interface.</summary>");
			w.Line($"public {model.TrackingInterface} OnCall({valueStorageType} value)");
			using (w.Braces())
			{
				w.Line("_sequence = null;");
				w.Line("_sequenceIndex = 0;");
				w.Line("_isVerifiable = false;");
				w.Line("_verifiableTimes = null;");
				// Clear callback storage (mutual exclusivity)
				w.Line("_onCall = null;");
				w.Line("_onCallTracking = null;");
				// Set value storage
				w.Line("_hasOnCallValue = true;");
				w.Line("_onCallValue = value;");
				w.Line("_onCallValueTracking = new MethodTrackingImpl(this);");
				w.Line("return _onCallValueTracking;");
			}
			w.Line();
		}

		// OnCallSequence() - starts a sequence, returns IMethodSequence
		w.Line($"/// <summary>Starts a callback sequence. Returns sequence for ThenCall chaining. Each callback runs exactly once.</summary>");
		w.Line($"public global::KnockOff.IMethodSequence<{delegateType}> OnCallSequence({delegateType} callback)");
		using (w.Braces())
		{
			w.Line("_onCall = null;");
			w.Line("_onCallTracking = null;");
			// Clear value storage (mutual exclusivity)
			if (canHaveValueOverload)
			{
				w.Line("_hasOnCallValue = false;");
				w.Line("_onCallValue = default!;");
				w.Line("_onCallValueTracking = null;");
			}
			w.Line("_isVerifiable = false;");
			w.Line("_verifiableTimes = null;");
			w.Line("_sequence = new global::System.Collections.Generic.List<(" + delegateType + " Callback, MethodTrackingImpl Tracking)>();");
			w.Line("var tracking = new MethodTrackingImpl(this);");
			w.Line("_sequence.Add((callback, tracking));");
			w.Line("_sequenceIndex = 0;");
			w.Line("return new MethodSequenceImpl(this);");
		}
		w.Line();

		// Invoke method
		RenderInvokeMethod(w, model, options, null);

		// Reset method - clears counts but preserves configuration and verifiable marking
		RenderResetMethod(w, model.Overloads, model.LastArgType, model.LastArgsType, hasSourceField: !string.IsNullOrEmpty(model.DeclaringInterface));

		// Internal verification support
		RenderInternalVerificationMembers(w, model.MethodName, model.Overloads, canHaveValueOverload);

		// Nested MethodTrackingImpl
		var fullInterceptorClassName = model.InterceptorClassName + options.InterceptorTypeParameters;
		RenderMethodTrackingImpl(w, model.TrackableParameters, model.LastArgType, model.LastArgsType, model.TrackingInterface, fullInterceptorClassName, null);

		// Nested MethodSequenceImpl
		RenderMethodSequenceImpl(w, fullInterceptorClassName, delegateType, null);
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

		// Generate delegates and storage for each unique overload
		foreach (var overload in model.Overloads)
		{
			// Delegate
			w.Line($"/// <summary>Delegate for {model.MethodName}({GetParamTypeList(overload.Parameters)}).</summary>");
			w.Line(overload.DelegateSignature);
			w.Line();

			// OnCall storage
			w.Line($"private {overload.DelegateName}? _onCall_{overload.SignatureSuffix};");
			w.Line($"private MethodTrackingImpl_{overload.SignatureSuffix}? _onCallTracking_{overload.SignatureSuffix};");
			w.Line();

			// Sequence storage
			w.Line($"private global::System.Collections.Generic.List<({overload.DelegateName} Callback, MethodTrackingImpl_{overload.SignatureSuffix} Tracking)>? _sequence_{overload.SignatureSuffix};");
			w.Line($"private int _sequenceIndex_{overload.SignatureSuffix};");
			w.Line();

			// Verifiable state per overload
			w.Line($"private bool _isVerifiable_{overload.SignatureSuffix};");
			w.Line($"private global::KnockOff.Times? _verifiableTimes_{overload.SignatureSuffix};");
			w.Line();
		}

		// Backward compatibility: aggregate tracking properties across all overloads
		RenderOverloadBackwardCompatibleProperties(w, model.Overloads);
		w.Line();

		// Verify() methods for direct interceptor verification
		RenderInterceptorVerifyMethods(w, model.MethodName);

		// OnCall overloads for each unique signature
		foreach (var overload in model.Overloads)
		{
			// OnCall - repeating callback
			w.Line($"/// <summary>Configures callback for {model.MethodName}({GetParamTypeList(overload.Parameters)}). Returns tracking interface.</summary>");
			w.Line($"public {overload.TrackingInterface} OnCall({overload.DelegateName} callback)");
			using (w.Braces())
			{
				w.Line($"_sequence_{overload.SignatureSuffix} = null;");
				w.Line($"_sequenceIndex_{overload.SignatureSuffix} = 0;");
				w.Line($"_isVerifiable_{overload.SignatureSuffix} = false;");
				w.Line($"_verifiableTimes_{overload.SignatureSuffix} = null;");
				w.Line($"_onCall_{overload.SignatureSuffix} = callback;");
				w.Line($"_onCallTracking_{overload.SignatureSuffix} = new MethodTrackingImpl_{overload.SignatureSuffix}(this);");
				w.Line($"return _onCallTracking_{overload.SignatureSuffix};");
			}
			w.Line();

			// OnCallSequence - starts a sequence
			w.Line($"/// <summary>Starts a callback sequence for {model.MethodName}({GetParamTypeList(overload.Parameters)}). Returns sequence for ThenCall chaining.</summary>");
			w.Line($"public global::KnockOff.IMethodSequence<{overload.DelegateName}> OnCallSequence({overload.DelegateName} callback)");
			using (w.Braces())
			{
				w.Line($"_onCall_{overload.SignatureSuffix} = null;");
				w.Line($"_onCallTracking_{overload.SignatureSuffix} = null;");
				w.Line($"_isVerifiable_{overload.SignatureSuffix} = false;");
				w.Line($"_verifiableTimes_{overload.SignatureSuffix} = null;");
				w.Line($"_sequence_{overload.SignatureSuffix} = new global::System.Collections.Generic.List<({overload.DelegateName} Callback, MethodTrackingImpl_{overload.SignatureSuffix} Tracking)>();");
				w.Line($"var tracking = new MethodTrackingImpl_{overload.SignatureSuffix}(this);");
				w.Line($"_sequence_{overload.SignatureSuffix}.Add((callback, tracking));");
				w.Line($"_sequenceIndex_{overload.SignatureSuffix} = 0;");
				w.Line($"return new MethodSequenceImpl_{overload.SignatureSuffix}(this);");
			}
			w.Line();
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

		// Nested tracking classes for each unique signature
		var fullInterceptorClassName = model.InterceptorClassName + options.InterceptorTypeParameters;
		foreach (var overload in model.Overloads)
		{
			RenderMethodTrackingImpl(w, overload.TrackableParameters, overload.LastArgType, overload.LastArgsType, overload.TrackingInterface, fullInterceptorClassName, overload.SignatureSuffix);
		}

		// Nested sequence classes for each unique signature
		foreach (var overload in model.Overloads)
		{
			RenderMethodSequenceImpl(w, fullInterceptorClassName, overload.DelegateName, overload.SignatureSuffix);
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
		var invokeParams = BuildInvokeParams(model.Parameters, options.IncludeStrictParameter);
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

			// Check sequence first (takes priority if present and not exhausted)
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

			// Check repeating OnCall value (before callback - value is simpler, check it first)
			if (canHaveValueOverload)
			{
				var (valueType, isTaskT, isValueTaskT) = GetAsyncTypeInfo(model.ReturnType);
				w.Line("if (_hasOnCallValue && _onCallValueTracking != null)");
				using (w.Braces())
				{
					w.Line($"_onCallValueTracking.RecordCall({trackingArgs});");
					// Return value, wrapping in Task/ValueTask if needed
					if (isTaskT)
						w.Line($"return global::System.Threading.Tasks.Task.FromResult(_onCallValue);");
					else if (isValueTaskT)
						w.Line($"return new global::System.Threading.Tasks.ValueTask<{valueType}>(_onCallValue);");
					else
						w.Line("return _onCallValue;");
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

			// Sequence exhausted in strict mode
			w.Line("if (_sequence != null && _sequenceIndex >= _sequence.Count)");
			using (w.Braces())
			{
				w.Line($"if ({options.StrictAccessExpression}) throw global::KnockOff.StubException.SequenceExhausted(\"{model.MethodName}\");");
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

			// Priority chain: Source > Strict > Default
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
		w.Line();
	}

	private static void RenderOverloadInvokeMethod(
		CodeWriter w,
		UnifiedMethodInterceptorModel model,
		MethodOverloadSignature overload,
		InterceptorRenderOptions options)
	{
		var invokeParams = BuildInvokeParams(overload.Parameters, options.IncludeStrictParameter);
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

			// Check sequence first
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

			// No callback configured
			w.Line("_unconfiguredCallCount++;");

			// Sequence exhausted in strict mode
			w.Line($"if (_sequence_{overload.SignatureSuffix} != null && _sequenceIndex_{overload.SignatureSuffix} >= _sequence_{overload.SignatureSuffix}.Count)");
			using (w.Braces())
			{
				w.Line($"if ({options.StrictAccessExpression}) throw global::KnockOff.StubException.SequenceExhausted(\"{model.MethodName}\");");
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

			// Priority chain: Source > Strict > Default
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
		w.Line();
	}

	#endregion

	#region Reset and Internal Verification Methods

	private static void RenderResetMethod(CodeWriter w, EquatableArray<MethodOverloadSignature> overloads, string? lastArgType = null, string? lastArgsType = null, bool hasSourceField = false)
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
				w.Line("if (_sequence != null)");
				using (w.Braces())
				{
					w.Line("foreach (var (_, tracking) in _sequence)");
					w.Line("\ttracking.Reset();");
				}
				w.Line("_sequenceIndex = 0;");
			}
			else
			{
				// Multi-overload
				foreach (var overload in overloads)
				{
					w.Line($"_onCallTracking_{overload.SignatureSuffix}?.Reset();");
					w.Line($"if (_sequence_{overload.SignatureSuffix} != null)");
					using (w.Braces())
					{
						w.Line($"foreach (var (_, tracking) in _sequence_{overload.SignatureSuffix})");
						w.Line("\ttracking.Reset();");
					}
					w.Line($"_sequenceIndex_{overload.SignatureSuffix} = 0;");
				}
			}
		}
		w.Line();
	}

	private static void RenderInternalVerificationMembers(CodeWriter w, string methodName, EquatableArray<MethodOverloadSignature> overloads, bool hasValueOverload)
	{
		if (overloads.Count == 0)
		{
			// Single-signature
			w.Line("/// <summary>Whether this interceptor was marked with Verifiable().</summary>");
			w.Line("internal bool IsVerifiable => _isVerifiable;");
			w.Line();

			// IsConfigured includes value storage if value overload is supported
			var isConfiguredExpr = hasValueOverload
				? "_hasOnCallValue || _onCall != null || (_sequence?.Count ?? 0) > 0"
				: "_onCall != null || (_sequence?.Count ?? 0) > 0";
			w.Line("/// <summary>Whether this interceptor has been configured (OnCall, OnCall(value), or OnCallSequence).</summary>");
			w.Line($"internal bool IsConfigured => {isConfiguredExpr};");
			w.Line();

			w.Line("/// <summary>Checks verification for Stub.Verify() - only checks if marked verifiable.</summary>");
			w.Line($"internal global::KnockOff.VerificationFailure? CheckVerification()");
			using (w.Braces())
			{
				w.Line("if (!_isVerifiable) return null;");
				w.Line("var times = _verifiableTimes ?? global::KnockOff.Times.AtLeastOnce;");
				w.Line($"return times.Validate(TotalCallCount) ? null : new global::KnockOff.VerificationFailure(\"{methodName}\", times, TotalCallCount);");
			}
			w.Line();

			w.Line("/// <summary>Checks verification for Stub.VerifyAll() - checks if configured.</summary>");
			w.Line($"internal global::KnockOff.VerificationFailure? CheckVerificationAll()");
			using (w.Braces())
			{
				w.Line("if (!IsConfigured) return null;");
				w.Line($"return global::KnockOff.Times.AtLeastOnce.Validate(TotalCallCount) ? null : new global::KnockOff.VerificationFailure(\"{methodName}\", global::KnockOff.Times.AtLeastOnce, TotalCallCount);");
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

			w.Line("/// <summary>Whether any overload has been configured.</summary>");
			var isConfiguredParts = overloads.Select(o => $"_onCall_{o.SignatureSuffix} != null || (_sequence_{o.SignatureSuffix}?.Count ?? 0) > 0");
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
						w.Line($"var count = (_onCallTracking_{overload.SignatureSuffix}?.CallCount ?? 0) + (_sequence_{overload.SignatureSuffix}?.Sum(s => s.Tracking.CallCount) ?? 0);");
						w.Line($"if (!times.Validate(count)) return new global::KnockOff.VerificationFailure(\"{methodName}\", times, count);");
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
					w.Line($"if (_onCall_{overload.SignatureSuffix} != null || (_sequence_{overload.SignatureSuffix}?.Count ?? 0) > 0)");
					using (w.Braces())
					{
						w.Line($"var count = (_onCallTracking_{overload.SignatureSuffix}?.CallCount ?? 0) + (_sequence_{overload.SignatureSuffix}?.Sum(s => s.Tracking.CallCount) ?? 0);");
						w.Line($"if (!global::KnockOff.Times.AtLeastOnce.Validate(count)) return new global::KnockOff.VerificationFailure(\"{methodName}\", global::KnockOff.Times.AtLeastOnce, count);");
					}
				}
				w.Line("return null;");
			}
			w.Line();
		}
	}

	#endregion

	#region Nested Tracking Class

	private static void RenderMethodTrackingImpl(
		CodeWriter w,
		EquatableArray<ParameterModel> trackableParams,
		string? lastArgType,
		string? lastArgsType,
		string trackingInterface,
		string interceptorClassName,
		string? signatureSuffix)
	{
		var className = signatureSuffix == null ? "MethodTrackingImpl" : $"MethodTrackingImpl_{signatureSuffix}";
		var verifiableFieldName = signatureSuffix == null ? "_isVerifiable" : $"_isVerifiable_{signatureSuffix}";
		var verifiableTimesFieldName = signatureSuffix == null ? "_verifiableTimes" : $"_verifiableTimes_{signatureSuffix}";

		w.Line($"/// <summary>Tracks invocations for this callback registration.</summary>");
		w.Line($"private sealed class {className} : {trackingInterface}");
		using (w.Braces())
		{
			// Reference to parent interceptor for setting verifiable
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

			// CallCount property (internal to nested class - parent accesses for aggregate; users can't access private nested class)
			w.Line("internal int CallCount { get; private set; }");
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
				w.Line("public void RecordCall() => CallCount++;");
			}
			else if (trackableParams.Count == 1)
			{
				var param = trackableParams.GetArray()![0];
				w.Line($"public void RecordCall({param.Type} {param.EscapedName}) {{ CallCount++; _lastArg = {param.EscapedName}; }}");
			}
			else
			{
				w.Line($"public void RecordCall({lastArgsType} args) {{ CallCount++; _lastArgs = args; }}");
			}
			w.Line();

			// Reset method
			w.Line("/// <summary>Resets tracking state.</summary>");
			if (trackableParams.Count == 0)
				w.Line("public void Reset() => CallCount = 0;");
			else if (trackableParams.Count == 1)
				w.Line("public void Reset() { CallCount = 0; _lastArg = default!; }");
			else
				w.Line("public void Reset() { CallCount = 0; _lastArgs = default; }");
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
				w.Line("if (!times.Validate(CallCount))");
				w.Line("\tthrow new global::KnockOff.VerificationException(new global::KnockOff.VerificationFailure(\"method\", times, CallCount));");
			}
			w.Line();

			// Verifiable() - marks for Stub.Verify()
			// When implementing derived interfaces (IMethodTracking<TArg> or IMethodTrackingArgs<TArgs>),
			// we need explicit interface implementations for the base IMethodTracking methods
			var isBaseInterface = trackingInterface == "global::KnockOff.IMethodTracking";

			if (isBaseInterface)
			{
				w.Line("/// <summary>Marks for verification by Stub.Verify(). Returns this for fluent chaining.</summary>");
				w.Line("public global::KnockOff.IMethodTracking Verifiable()");
				using (w.Braces())
				{
					w.Line($"_interceptor.{verifiableFieldName} = true;");
					w.Line($"_interceptor.{verifiableTimesFieldName} = null;");
					w.Line("return this;");
				}
				w.Line();

				w.Line("/// <summary>Marks for verification by Stub.Verify() with Times constraint. Returns this for fluent chaining.</summary>");
				w.Line("public global::KnockOff.IMethodTracking Verifiable(global::KnockOff.Times times)");
				using (w.Braces())
				{
					w.Line($"_interceptor.{verifiableFieldName} = true;");
					w.Line($"_interceptor.{verifiableTimesFieldName} = times;");
					w.Line("return this;");
				}
			}
			else
			{
				// Typed interface - need to implement both the derived and base versions
				w.Line("/// <summary>Marks for verification by Stub.Verify(). Returns this for fluent chaining.</summary>");
				w.Line($"public {trackingInterface} Verifiable()");
				using (w.Braces())
				{
					w.Line($"_interceptor.{verifiableFieldName} = true;");
					w.Line($"_interceptor.{verifiableTimesFieldName} = null;");
					w.Line("return this;");
				}
				w.Line();

				w.Line("/// <summary>Marks for verification by Stub.Verify() with Times constraint. Returns this for fluent chaining.</summary>");
				w.Line($"public {trackingInterface} Verifiable(global::KnockOff.Times times)");
				using (w.Braces())
				{
					w.Line($"_interceptor.{verifiableFieldName} = true;");
					w.Line($"_interceptor.{verifiableTimesFieldName} = times;");
					w.Line("return this;");
				}
				w.Line();

				// Explicit interface implementations for base IMethodTracking
				w.Line("global::KnockOff.IMethodTracking global::KnockOff.IMethodTracking.Verifiable() => Verifiable();");
				w.Line("global::KnockOff.IMethodTracking global::KnockOff.IMethodTracking.Verifiable(global::KnockOff.Times times) => Verifiable(times);");
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
		string? signatureSuffix)
	{
		var className = signatureSuffix == null ? "MethodSequenceImpl" : $"MethodSequenceImpl_{signatureSuffix}";
		var trackingClassName = signatureSuffix == null ? "MethodTrackingImpl" : $"MethodTrackingImpl_{signatureSuffix}";
		var sequenceField = signatureSuffix == null ? "_sequence" : $"_sequence_{signatureSuffix}";
		var sequenceIndexField = signatureSuffix == null ? "_sequenceIndex" : $"_sequenceIndex_{signatureSuffix}";
		var verifiableField = signatureSuffix == null ? "_isVerifiable" : $"_isVerifiable_{signatureSuffix}";
		var verifiableTimesField = signatureSuffix == null ? "_verifiableTimes" : $"_verifiableTimes_{signatureSuffix}";

		w.Line($"/// <summary>Sequence implementation for ThenCall chaining.</summary>");
		w.Line($"private sealed class {className} : global::KnockOff.IMethodSequence<{delegateType}>");
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
					w.Line("\ttotal += tracking.CallCount;");
					w.Line("return total;");
				}
			}
			w.Line();

			// ThenCall - no Times parameter, each callback runs once
			w.Line($"/// <summary>Adds another callback to the sequence. Each callback runs exactly once.</summary>");
			w.Line($"public global::KnockOff.IMethodSequence<{delegateType}> ThenCall({delegateType} callback)");
			using (w.Braces())
			{
				w.Line($"var tracking = new {trackingClassName}(_interceptor);");
				w.Line($"_interceptor.{sequenceField}!.Add((callback, tracking));");
				w.Line("return this;");
			}
			w.Line();

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
			w.Line($"public global::KnockOff.IMethodSequence<{delegateType}> Verifiable()");
			using (w.Braces())
			{
				w.Line($"_interceptor.{verifiableField} = true;");
				w.Line($"_interceptor.{verifiableTimesField} = null;");
				w.Line("return this;");
			}
			w.Line();

			// Non-generic IMethodSequence.Verifiable()
			w.Line("/// <summary>Marks this sequence for verification by Stub.Verify(). Returns this for fluent chaining.</summary>");
			w.Line("global::KnockOff.IMethodSequence global::KnockOff.IMethodSequence.Verifiable() => Verifiable();");
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

	private static string BuildInvokeParams(EquatableArray<ParameterModel> parameters, bool includeStrict)
	{
		var parts = new List<string>();
		if (includeStrict)
			parts.Add("bool strict");
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

	#endregion

	#region Backward Compatibility Properties

	/// <summary>
	/// Renders aggregate tracking properties for backward compatibility (single-signature).
	/// These provide LastCallArg/LastCallArgs for argument tracking.
	/// </summary>
	private static void RenderBackwardCompatibleTrackingProperties(
		CodeWriter w,
		EquatableArray<ParameterModel> trackableParams,
		string? lastArgType,
		string? lastArgsType,
		bool hasValueOverload)
	{
		// CallCount - total across OnCall + OnCall(value) + sequence + unconfigured (private - use Verify() API to check call counts)
		// Include value tracking when value overload exists
		var valueTrackingPart = hasValueOverload ? " + (_onCallValueTracking?.CallCount ?? 0)" : "";
		w.Line($"private int TotalCallCount {{ get {{ var sum = _unconfiguredCallCount + (_onCallTracking?.CallCount ?? 0){valueTrackingPart}; if (_sequence != null) foreach (var s in _sequence) sum += s.Tracking.CallCount; return sum; }} }}");
		w.Line();

		// LastCallArg - for single param methods
		// Note: value tracking also has LastArg when there are trackable parameters
		if (lastArgType != null)
		{
			var nullableType = lastArgType.EndsWith("?") ? lastArgType : $"{lastArgType}?";
			w.Line($"/// <summary>The argument from the last call (from most recently called registration).</summary>");
			if (hasValueOverload)
			{
				// Include value tracking in the priority chain
				w.Line($"public {nullableType} LastCallArg {{ get {{ if ((_onCallValueTracking?.CallCount ?? 0) > 0) return _onCallValueTracking!.LastArg; if ((_onCallTracking?.CallCount ?? 0) > 0) return _onCallTracking!.LastArg; if (_sequence != null) for (int i = _sequence.Count - 1; i >= 0; i--) if (_sequence[i].Tracking.CallCount > 0) return _sequence[i].Tracking.LastArg; return _unconfiguredCallCount > 0 ? _unconfiguredLastArg : default; }} }}");
			}
			else
			{
				w.Line($"public {nullableType} LastCallArg {{ get {{ if ((_onCallTracking?.CallCount ?? 0) > 0) return _onCallTracking!.LastArg; if (_sequence != null) for (int i = _sequence.Count - 1; i >= 0; i--) if (_sequence[i].Tracking.CallCount > 0) return _sequence[i].Tracking.LastArg; return _unconfiguredCallCount > 0 ? _unconfiguredLastArg : default; }} }}");
			}
			w.Line();
		}

		// LastCallArgs - for multi-param methods
		if (lastArgsType != null)
		{
			var nullableType = lastArgsType.EndsWith("?") ? lastArgsType : $"{lastArgsType}?";
			w.Line($"/// <summary>The arguments from the last call (from most recently called registration).</summary>");
			if (hasValueOverload)
			{
				// Include value tracking in the priority chain
				w.Line($"public {nullableType} LastCallArgs {{ get {{ if ((_onCallValueTracking?.CallCount ?? 0) > 0) return _onCallValueTracking!.LastArgs; if ((_onCallTracking?.CallCount ?? 0) > 0) return _onCallTracking!.LastArgs; if (_sequence != null) for (int i = _sequence.Count - 1; i >= 0; i--) if (_sequence[i].Tracking.CallCount > 0) return _sequence[i].Tracking.LastArgs; return _unconfiguredCallCount > 0 ? _unconfiguredLastArgs : default; }} }}");
			}
			else
			{
				w.Line($"public {nullableType} LastCallArgs {{ get {{ if ((_onCallTracking?.CallCount ?? 0) > 0) return _onCallTracking!.LastArgs; if (_sequence != null) for (int i = _sequence.Count - 1; i >= 0; i--) if (_sequence[i].Tracking.CallCount > 0) return _sequence[i].Tracking.LastArgs; return _unconfiguredCallCount > 0 ? _unconfiguredLastArgs : default; }} }}");
			}
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
		var sumParts = overloads.Select(o =>
			$"(_onCallTracking_{o.SignatureSuffix}?.CallCount ?? 0) + (_sequence_{o.SignatureSuffix}?.Sum(s => s.Tracking.CallCount) ?? 0)");
		var sumExpr = "_unconfiguredCallCount + " + string.Join(" + ", sumParts);

		// Private - use Verify() API to check call counts
		w.Line($"private int TotalCallCount => {sumExpr};");
	}

	/// <summary>
	/// Renders Verify() and Verify(Times) methods on an interceptor class.
	/// </summary>
	private static void RenderInterceptorVerifyMethods(CodeWriter w, string methodName)
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
	}

	#endregion
}
