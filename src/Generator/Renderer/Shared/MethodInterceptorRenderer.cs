// src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs
#nullable enable
using System.Collections.Generic;
using System.Linq;
using KnockOff.Builder;
using KnockOff.Model.Shared;

namespace KnockOff.Renderer.Shared;

/// <summary>
/// Renders method interceptor classes for both inline and flat stubs.
/// Generates OnCall() methods returning IMethodTracking, sequences with Times constraints,
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

		// Custom delegate if needed
		if (model.NeedsCustomDelegate && model.CustomDelegateSignature != null)
		{
			w.Line($"/// <summary>Delegate for {model.MethodName}.</summary>");
			w.Line(model.CustomDelegateSignature);
			w.Line();
		}

		// Sequence storage
		w.Line($"private readonly global::System.Collections.Generic.List<({delegateType} Callback, global::KnockOff.Times Times, MethodTrackingImpl Tracking)> _sequence = new();");
		w.Line("private int _sequenceIndex;");
		w.Line("private int _unconfiguredCallCount;");
		// Track arguments for unconfigured calls
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
		RenderBackwardCompatibleTrackingProperties(w, model.TrackableParameters, model.LastArgType, model.LastArgsType);
		w.Line();

		// OnCall() returning IMethodTracking
		w.Line($"/// <summary>Configures callback that repeats forever. Returns tracking interface.</summary>");
		w.Line($"public {model.TrackingInterface} OnCall({delegateType} callback)");
		using (w.Braces())
		{
			w.Line("var tracking = new MethodTrackingImpl();");
			w.Line("_sequence.Clear();");
			w.Line("_sequence.Add((callback, global::KnockOff.Times.Forever, tracking));");
			w.Line("_sequenceIndex = 0;");
			w.Line("return tracking;");
		}
		w.Line();

		// OnCall with Times returning IMethodSequence
		w.Line($"/// <summary>Configures callback with Times constraint. Returns sequence for ThenCall chaining.</summary>");
		w.Line($"public global::KnockOff.IMethodSequence<{delegateType}> OnCall({delegateType} callback, global::KnockOff.Times times)");
		using (w.Braces())
		{
			w.Line("var tracking = new MethodTrackingImpl();");
			w.Line("_sequence.Clear();");
			w.Line("_sequence.Add((callback, times, tracking));");
			w.Line("_sequenceIndex = 0;");
			w.Line("return new MethodSequenceImpl(this);");
		}
		w.Line();

		// Invoke method
		RenderInvokeMethod(w, model, options, null);

		// Reset method
		RenderResetMethod(w, model.Overloads, model.LastArgType, model.LastArgsType);

		// Verify method
		RenderVerifyMethod(w, model.Overloads);

		// Nested MethodTrackingImpl
		RenderMethodTrackingImpl(w, model.TrackableParameters, model.LastArgType, model.LastArgsType, model.TrackingInterface, null);

		// Nested MethodSequenceImpl
		var fullInterceptorClassName = model.InterceptorClassName + options.InterceptorTypeParameters;
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

		// Track unconfigured calls (shared across all overloads)
		w.Line("private int _unconfiguredCallCount;");
		w.Line();

		// Generate delegates and sequences for each unique overload
		foreach (var overload in model.Overloads)
		{
			// Delegate
			w.Line($"/// <summary>Delegate for {model.MethodName}({GetParamTypeList(overload.Parameters)}).</summary>");
			w.Line(overload.DelegateSignature);
			w.Line();

			// Sequence storage
			w.Line($"private readonly global::System.Collections.Generic.List<({overload.DelegateName} Callback, global::KnockOff.Times Times, MethodTrackingImpl_{overload.SignatureSuffix} Tracking)> _sequence_{overload.SignatureSuffix} = new();");
			w.Line($"private int _sequenceIndex_{overload.SignatureSuffix};");
			w.Line();
		}

		// Backward compatibility: aggregate tracking properties across all overloads
		RenderOverloadBackwardCompatibleProperties(w, model.Overloads);
		w.Line();

		// OnCall overloads for each unique signature
		foreach (var overload in model.Overloads)
		{
			// OnCall without Times
			w.Line($"/// <summary>Configures callback for {model.MethodName}({GetParamTypeList(overload.Parameters)}). Returns tracking interface.</summary>");
			w.Line($"public {overload.TrackingInterface} OnCall({overload.DelegateName} callback)");
			using (w.Braces())
			{
				w.Line($"var tracking = new MethodTrackingImpl_{overload.SignatureSuffix}();");
				w.Line($"_sequence_{overload.SignatureSuffix}.Clear();");
				w.Line($"_sequence_{overload.SignatureSuffix}.Add((callback, global::KnockOff.Times.Forever, tracking));");
				w.Line($"_sequenceIndex_{overload.SignatureSuffix} = 0;");
				w.Line("return tracking;");
			}
			w.Line();

			// OnCall with Times
			w.Line($"/// <summary>Configures callback for {model.MethodName}({GetParamTypeList(overload.Parameters)}) with Times constraint.</summary>");
			w.Line($"public global::KnockOff.IMethodSequence<{overload.DelegateName}> OnCall({overload.DelegateName} callback, global::KnockOff.Times times)");
			using (w.Braces())
			{
				w.Line($"var tracking = new MethodTrackingImpl_{overload.SignatureSuffix}();");
				w.Line($"_sequence_{overload.SignatureSuffix}.Clear();");
				w.Line($"_sequence_{overload.SignatureSuffix}.Add((callback, times, tracking));");
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

		// Reset method (resets all sequences)
		RenderResetMethod(w, model.Overloads);

		// Verify method (verifies all sequences)
		RenderVerifyMethod(w, model.Overloads);

		// Nested tracking classes for each unique signature
		foreach (var overload in model.Overloads)
		{
			RenderMethodTrackingImpl(w, overload.TrackableParameters, overload.LastArgType, overload.LastArgsType, overload.TrackingInterface, overload.SignatureSuffix);
		}

		// Nested sequence classes for each unique signature
		var fullInterceptorClassName = model.InterceptorClassName + options.InterceptorTypeParameters;
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
		var ownerWithParams = GetOwnerWithParams(model);
		var invokeParams = BuildInvokeParams(ownerWithParams, model.Parameters, options.IncludeStrictParameter);
		var returnType = model.IsVoid ? "void" : model.ReturnType;

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

			// No sequence configured - track call and return default
			w.Line("if (_sequence.Count == 0)");
			using (w.Braces())
			{
				w.Line("_unconfiguredCallCount++;");
				// Track last arg/args for unconfigured calls
				if (model.LastArgType != null && model.TrackableParameters.Count > 0)
				{
					var firstParam = model.TrackableParameters.First().EscapedName;
					w.Line($"_unconfiguredLastArg = {firstParam};");
				}
				if (model.LastArgsType != null)
				{
					w.Line($"_unconfiguredLastArgs = ({trackingArgs});");
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

			// Get current callback from sequence
			w.Line("var (callback, times, tracking) = _sequence[_sequenceIndex];");
			w.Line($"tracking.RecordCall({trackingArgs});");
			w.Line();

			// Advance sequence if times exhausted (and not Forever)
			w.Line("if (!times.IsForever && tracking.CallCount >= times.Count)");
			using (w.Braces())
			{
				w.Line("if (_sequenceIndex < _sequence.Count - 1)");
				w.Line("\t_sequenceIndex++;");
				w.Line("else if (tracking.CallCount > times.Count)");
				w.Line($"\tthrow global::KnockOff.StubException.SequenceExhausted(\"{model.MethodName}\");");
			}
			w.Line();

			// Invoke callback
			var callbackArgs = BuildCallbackArgs(model.Parameters);
			if (model.IsVoid)
				w.Line($"callback({callbackArgs});");
			else
				w.Line($"return callback({callbackArgs});");
		}
		w.Line();
	}

	private static void RenderOverloadInvokeMethod(
		CodeWriter w,
		UnifiedMethodInterceptorModel model,
		MethodOverloadSignature overload,
		InterceptorRenderOptions options)
	{
		var ownerWithParams = GetOwnerWithParams(model);
		var invokeParams = BuildInvokeParams(ownerWithParams, overload.Parameters, options.IncludeStrictParameter);
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

			w.Line($"if (_sequence_{overload.SignatureSuffix}.Count == 0)");
			using (w.Braces())
			{
				w.Line("_unconfiguredCallCount++;");
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

			w.Line($"var (callback, times, tracking) = _sequence_{overload.SignatureSuffix}[_sequenceIndex_{overload.SignatureSuffix}];");
			w.Line($"tracking.RecordCall({trackingArgs});");
			w.Line();

			w.Line("if (!times.IsForever && tracking.CallCount >= times.Count)");
			using (w.Braces())
			{
				w.Line($"if (_sequenceIndex_{overload.SignatureSuffix} < _sequence_{overload.SignatureSuffix}.Count - 1)");
				w.Line($"\t_sequenceIndex_{overload.SignatureSuffix}++;");
				w.Line("else if (tracking.CallCount > times.Count)");
				w.Line($"\tthrow global::KnockOff.StubException.SequenceExhausted(\"{model.MethodName}\");");
			}
			w.Line();

			var callbackArgs = BuildCallbackArgs(overload.Parameters);
			if (overload.IsVoid)
				w.Line($"callback({callbackArgs});");
			else
				w.Line($"return callback({callbackArgs});");
		}
		w.Line();
	}

	#endregion

	#region Reset and Verify Methods

	private static void RenderResetMethod(CodeWriter w, EquatableArray<MethodOverloadSignature> overloads, string? lastArgType = null, string? lastArgsType = null)
	{
		w.Line("/// <summary>Resets all tracking state.</summary>");
		using (w.Block("public void Reset()"))
		{
			w.Line("_unconfiguredCallCount = 0;");
			if (lastArgType != null)
				w.Line("_unconfiguredLastArg = default;");
			if (lastArgsType != null)
				w.Line("_unconfiguredLastArgs = default;");
			if (overloads.Count == 0)
			{
				// Single-signature
				w.Line("foreach (var (_, _, tracking) in _sequence)");
				w.Line("\ttracking.Reset();");
				w.Line("_sequenceIndex = 0;");
			}
			else
			{
				// Multi-overload
				foreach (var overload in overloads)
				{
					w.Line($"foreach (var (_, _, tracking) in _sequence_{overload.SignatureSuffix})");
					w.Line("\ttracking.Reset();");
					w.Line($"_sequenceIndex_{overload.SignatureSuffix} = 0;");
				}
			}
		}
		w.Line();
	}

	private static void RenderVerifyMethod(CodeWriter w, EquatableArray<MethodOverloadSignature> overloads)
	{
		w.Line("/// <summary>Verifies all Times constraints were satisfied. For Forever, verifies called at least once.</summary>");
		using (w.Block("public bool Verify()"))
		{
			if (overloads.Count == 0)
			{
				// Single-signature
				w.Line("foreach (var (_, times, tracking) in _sequence)");
				using (w.Braces())
				{
					w.Line("if (times.IsForever)");
					using (w.Braces())
					{
						w.Line("if (!tracking.WasCalled)");
						w.Line("\treturn false;");
					}
					w.Line("else if (!times.Verify(tracking.CallCount))");
					w.Line("\treturn false;");
				}
			}
			else
			{
				// Multi-overload
				foreach (var overload in overloads)
				{
					w.Line($"foreach (var (_, times, tracking) in _sequence_{overload.SignatureSuffix})");
					using (w.Braces())
					{
						w.Line("if (times.IsForever)");
						using (w.Braces())
						{
							w.Line("if (!tracking.WasCalled)");
							w.Line("\treturn false;");
						}
						w.Line("else if (!times.Verify(tracking.CallCount))");
						w.Line("\treturn false;");
					}
				}
			}
			w.Line("return true;");
		}
		w.Line();
	}

	#endregion

	#region Nested Tracking Class

	private static void RenderMethodTrackingImpl(
		CodeWriter w,
		EquatableArray<ParameterModel> trackableParams,
		string? lastArgType,
		string? lastArgsType,
		string trackingInterface,
		string? signatureSuffix)
	{
		var className = signatureSuffix == null ? "MethodTrackingImpl" : $"MethodTrackingImpl_{signatureSuffix}";

		w.Line($"/// <summary>Tracks invocations for this callback registration.</summary>");
		w.Line($"private sealed class {className} : {trackingInterface}");
		using (w.Braces())
		{
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

			// CallCount property
			w.Line("/// <summary>Number of times this callback was invoked.</summary>");
			w.Line("public int CallCount { get; private set; }");
			w.Line();

			// WasCalled property
			w.Line("/// <summary>True if CallCount > 0.</summary>");
			w.Line("public bool WasCalled => CallCount > 0;");
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

		w.Line($"/// <summary>Sequence implementation for ThenCall chaining.</summary>");
		w.Line($"private sealed class {className} : global::KnockOff.IMethodSequence<{delegateType}>");
		using (w.Braces())
		{
			w.Line($"private readonly {interceptorClassName} _interceptor;");
			w.Line();

			w.Line($"public {className}({interceptorClassName} interceptor) => _interceptor = interceptor;");
			w.Line();

			// TotalCallCount
			w.Line("/// <summary>Total calls across all callbacks in sequence.</summary>");
			w.Line("public int TotalCallCount");
			using (w.Braces())
			{
				w.Line("get");
				using (w.Braces())
				{
					w.Line("var total = 0;");
					w.Line($"foreach (var (_, _, tracking) in _interceptor.{sequenceField})");
					w.Line("\ttotal += tracking.CallCount;");
					w.Line("return total;");
				}
			}
			w.Line();

			// ThenCall
			w.Line($"/// <summary>Add another callback to the sequence.</summary>");
			w.Line($"public global::KnockOff.IMethodSequence<{delegateType}> ThenCall({delegateType} callback, global::KnockOff.Times times)");
			using (w.Braces())
			{
				w.Line($"var tracking = new {trackingClassName}();");
				w.Line($"_interceptor.{sequenceField}.Add((callback, times, tracking));");
				w.Line("return this;");
			}
			w.Line();

			// Verify
			w.Line("/// <summary>Verify all Times constraints in the sequence were satisfied.</summary>");
			w.Line("public bool Verify()");
			using (w.Braces())
			{
				w.Line($"foreach (var (_, times, tracking) in _interceptor.{sequenceField})");
				using (w.Braces())
				{
					w.Line("if (!times.Verify(tracking.CallCount))");
					w.Line("\treturn false;");
				}
				w.Line("return true;");
			}
			w.Line();

			// Reset
			w.Line("/// <summary>Reset all tracking in the sequence.</summary>");
			w.Line("public void Reset() => _interceptor.Reset();");
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

	private static string BuildInvokeParams(string ownerClassName, EquatableArray<ParameterModel> parameters, bool includeStrict)
	{
		var parts = new List<string> { $"{ownerClassName} ko" };
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
		var parts = new List<string> { "ko" };
		foreach (var p in parameters)
		{
			parts.Add($"{p.RefPrefix}{p.EscapedName}");
		}
		return string.Join(", ", parts);
	}

	#endregion

	#region Backward Compatibility Properties

	/// <summary>
	/// Renders aggregate tracking properties for backward compatibility (single-signature).
	/// These allow the old pattern: stub.Method.WasCalled, stub.Method.CallCount
	/// </summary>
	private static void RenderBackwardCompatibleTrackingProperties(
		CodeWriter w,
		EquatableArray<ParameterModel> trackableParams,
		string? lastArgType,
		string? lastArgsType)
	{
		// CallCount - total across all registrations plus unconfigured calls
		w.Line("/// <summary>Total number of times this method was called (across all OnCall registrations).</summary>");
		w.Line("public int CallCount { get { int sum = _unconfiguredCallCount; foreach (var s in _sequence) sum += s.Tracking.CallCount; return sum; } }");
		w.Line();

		// WasCalled - true if any registration was called
		w.Line("/// <summary>Whether this method was called at least once.</summary>");
		w.Line("public bool WasCalled => CallCount > 0;");
		w.Line();

		// LastCallArg - for single param methods
		if (lastArgType != null)
		{
			// Make nullable if not already (avoid double ??)
			var nullableType = lastArgType.EndsWith("?") ? lastArgType : $"{lastArgType}?";
			w.Line($"/// <summary>The argument from the last call (from most recently called registration).</summary>");
			w.Line($"public {nullableType} LastCallArg {{ get {{ for (int i = _sequence.Count - 1; i >= 0; i--) if (_sequence[i].Tracking.CallCount > 0) return _sequence[i].Tracking.LastArg; return _unconfiguredCallCount > 0 ? _unconfiguredLastArg : default; }} }}");
			w.Line();
		}

		// LastCallArgs - for multi-param methods
		if (lastArgsType != null)
		{
			// Make nullable if not already (avoid double ??)
			var nullableType = lastArgsType.EndsWith("?") ? lastArgsType : $"{lastArgsType}?";
			w.Line($"/// <summary>The arguments from the last call (from most recently called registration).</summary>");
			w.Line($"public {nullableType} LastCallArgs {{ get {{ for (int i = _sequence.Count - 1; i >= 0; i--) if (_sequence[i].Tracking.CallCount > 0) return _sequence[i].Tracking.LastArgs; return _unconfiguredCallCount > 0 ? _unconfiguredLastArgs : default; }} }}");
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
		// Build a sum expression across all sequences, plus unconfigured calls
		var sumParts = overloads.Select(o => $"_sequence_{o.SignatureSuffix}.Sum(s => s.Tracking.CallCount)");
		var sumExpr = "_unconfiguredCallCount + " + string.Join(" + ", sumParts);

		w.Line("/// <summary>Total number of times this method was called (across all overloads and registrations).</summary>");
		w.Line($"public int CallCount => {sumExpr};");
		w.Line();

		w.Line("/// <summary>Whether this method was called at least once (any overload).</summary>");
		w.Line("public bool WasCalled => CallCount > 0;");
	}

	#endregion
}
