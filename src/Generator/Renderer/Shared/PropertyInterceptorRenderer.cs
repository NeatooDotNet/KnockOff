// src/Generator/Renderer/Shared/PropertyInterceptorRenderer.cs
#nullable enable
using KnockOff.Model.Shared;

namespace KnockOff.Renderer.Shared;

/// <summary>
/// Renders property interceptor classes for both inline and flat stubs.
/// Generates OnGet() returning IPropertyGetTracking (repeating callback),
/// OnGetSequence() returning IPropertyGetSequence (for ThenGet chaining),
/// OnSet()/OnSetSequence() similarly for setters,
/// nested tracking and sequence implementation classes, InvokeGet/InvokeSet methods, and verification.
/// </summary>
internal static class PropertyInterceptorRenderer
{
	/// <summary>
	/// Renders a complete property interceptor class.
	/// For init-only properties, generates getter-only API (no OnSet methods).
	/// For regular properties, generates full getter/setter API based on accessor availability.
	/// </summary>
	public static void RenderInterceptorClass(
		CodeWriter w,
		UnifiedPropertyInterceptorModel model,
		PropertyInterceptorRenderOptions options)
	{
		var typeParams = options.InterceptorTypeParameters;
		var constraints = options.InterceptorConstraints;
		var classDecl = $"public sealed class {model.InterceptorClassName}{typeParams}{constraints}";

		w.Line($"/// <summary>Tracks and configures behavior for {model.PropertyName}.</summary>");
		using (w.Block(classDecl))
		{
			if (model.IsInitOnly)
			{
				RenderInitOnlyPropertyContent(w, model, options);
			}
			else
			{
				RenderRegularPropertyContent(w, model, options);
			}
		}
		w.Line();
	}

	#region Init-Only Property (getter-focused, tracking-only for setter)

	private static void RenderInitOnlyPropertyContent(
		CodeWriter w,
		UnifiedPropertyInterceptorModel model,
		PropertyInterceptorRenderOptions options)
	{
		var fullInterceptorClassName = model.InterceptorClassName + options.InterceptorTypeParameters;

		// Value storage (init-only - setting Value marks the property as configured)
		w.Line("private bool _valueSet;");
		w.Line($"private {model.ValueType} _value = default!;");
		w.Line($"/// <summary>The configured value for {model.PropertyName}. Setting this marks the property as configured.</summary>");
		w.Line($"public {model.ValueType} Value");
		using (w.Braces())
		{
			w.Line("get => _value;");
			w.Line("set { _value = value; _valueSet = true; }");
		}
		w.Line();

		// Getter tracking and sequence storage
		w.Line($"private global::System.Func<{model.ValueType}>? _onGet;");
		w.Line("private PropertyGetTrackingImpl? _onGetTracking;");
		w.Line($"private global::System.Collections.Generic.List<(global::System.Func<{model.ValueType}> Callback, PropertyGetTrackingImpl Tracking)>? _getSequence;");
		w.Line("private int _getSequenceIndex;");
		w.Line();

		// Verifiable state (for getter)
		w.Line("private bool _isGetVerifiable;");
		w.Line("private global::KnockOff.Times? _getVerifiableTimes;");
		w.Line();

		// Track unconfigured getter calls
		w.Line("private int _unconfiguredGetCount;");
		w.Line();

		// Setter tracking (init-only - just tracks that init was called)
		w.Line("private int _setCount;");
		w.Line($"/// <summary>The value from the init setter call.</summary>");
		w.Line($"public {model.NullableValueType} LastSetValue {{ get; private set; }}");
		w.Line();

		// Aggregate get count (private - use VerifyGet() to check)
		w.Line("private int TotalGetCount { get { var sum = _unconfiguredGetCount + (_onGetTracking?.CallCount ?? 0); if (_getSequence != null) foreach (var s in _getSequence) sum += s.Tracking.CallCount; return sum; } }");
		w.Line();

		// OnGet() - repeating callback, returns IPropertyGetTracking
		w.Line($"/// <summary>Configures getter callback that repeats indefinitely. Returns tracking interface.</summary>");
		w.Line($"public global::KnockOff.IPropertyGetTracking OnGet(global::System.Func<{model.ValueType}> callback)");
		using (w.Braces())
		{
			w.Line("_getSequence = null;");
			w.Line("_getSequenceIndex = 0;");
			w.Line("_isGetVerifiable = false;");
			w.Line("_getVerifiableTimes = null;");
			w.Line("_onGet = callback;");
			w.Line("_onGetTracking = new PropertyGetTrackingImpl(this);");
			w.Line("return _onGetTracking;");
		}
		w.Line();

		// OnGetSequence() - starts a sequence, returns IPropertyGetSequence
		w.Line($"/// <summary>Starts a getter callback sequence. Returns sequence for ThenGet chaining. Each callback runs exactly once.</summary>");
		w.Line($"public global::KnockOff.IPropertyGetSequence<{model.ValueType}> OnGetSequence(global::System.Func<{model.ValueType}> callback)");
		using (w.Braces())
		{
			w.Line("_onGet = null;");
			w.Line("_onGetTracking = null;");
			w.Line("_isGetVerifiable = false;");
			w.Line("_getVerifiableTimes = null;");
			w.Line($"_getSequence = new global::System.Collections.Generic.List<(global::System.Func<{model.ValueType}> Callback, PropertyGetTrackingImpl Tracking)>();");
			w.Line("var tracking = new PropertyGetTrackingImpl(this);");
			w.Line("_getSequence.Add((callback, tracking));");
			w.Line("_getSequenceIndex = 0;");
			w.Line("return new PropertyGetSequenceImpl(this);");
		}
		w.Line();

		// RecordSet - tracks init setter invocation (for verification)
		w.Line("/// <summary>Records an init setter access.</summary>");
		w.Line($"public void RecordSet({model.NullableValueType} value) {{ _setCount++; LastSetValue = value; }}");
		w.Line();

		// InvokeGet method
		RenderInvokeGet(w, model, options);

		// Reset method
		w.Line("/// <summary>Resets tracking state but preserves configuration (Value, OnGet) and verifiable marking.</summary>");
		w.Line("public void Reset()");
		using (w.Braces())
		{
			w.Line("_unconfiguredGetCount = 0;");
			w.Line("_setCount = 0;");
			w.Line("LastSetValue = default;");
			w.Line("_onGetTracking?.Reset();");
			w.Line("if (_getSequence != null)");
			using (w.Braces())
			{
				w.Line("foreach (var (_, tracking) in _getSequence)");
				w.Line("\ttracking.Reset();");
			}
			w.Line("_getSequenceIndex = 0;");
		}
		w.Line();

		// Verification methods
		RenderInitOnlyVerificationMethods(w, model, fullInterceptorClassName);

		// Internal verification support
		RenderInitOnlyInternalVerification(w, model);

		// Nested classes
		RenderPropertyGetTrackingImpl(w, fullInterceptorClassName, isInitOnly: true);
		RenderPropertyGetSequenceImpl(w, model.ValueType, fullInterceptorClassName, isInitOnly: true);
	}

	#endregion

	#region Regular Property (full getter/setter API)

	private static void RenderRegularPropertyContent(
		CodeWriter w,
		UnifiedPropertyInterceptorModel model,
		PropertyInterceptorRenderOptions options)
	{
		var fullInterceptorClassName = model.InterceptorClassName + options.InterceptorTypeParameters;

		// Source field for Source(T) feature
		if (!string.IsNullOrEmpty(model.DeclaringInterface))
		{
			w.Line($"/// <summary>Source object to delegate to when no OnGet/OnSet is configured.</summary>");
			w.Line($"internal {model.DeclaringInterface}? _source;");
			w.Line();
		}

		// Value storage
		w.Line("private bool _valueSet;");
		w.Line($"private {model.ValueType} _value{GetDefaultValueSuffix(model.DefaultExpression)}");
		w.Line($"/// <summary>Value returned by getter when OnGet is not set. Setting this marks the property as configured.</summary>");
		w.Line($"public {model.ValueType} Value");
		using (w.Braces())
		{
			w.Line("get => _value;");
			w.Line("set { _value = value; _valueSet = true; }");
		}
		w.Line();

		// Getter storage and tracking (if has getter)
		if (model.HasGetter)
		{
			w.Line($"private global::System.Func<{model.ValueType}>? _onGet;");
			w.Line("private PropertyGetTrackingImpl? _onGetTracking;");
			w.Line($"private global::System.Collections.Generic.List<(global::System.Func<{model.ValueType}> Callback, PropertyGetTrackingImpl Tracking)>? _getSequence;");
			w.Line("private int _getSequenceIndex;");
			w.Line("private bool _isGetVerifiable;");
			w.Line("private global::KnockOff.Times? _getVerifiableTimes;");
			w.Line("private int _unconfiguredGetCount;");
			w.Line();
		}

		// Setter storage and tracking (if has setter)
		if (model.HasSetter)
		{
			w.Line($"private global::System.Action<{model.ValueType}>? _onSet;");
			w.Line("private PropertySetTrackingImpl? _onSetTracking;");
			w.Line($"private global::System.Collections.Generic.List<(global::System.Action<{model.ValueType}> Callback, PropertySetTrackingImpl Tracking)>? _setSequence;");
			w.Line("private int _setSequenceIndex;");
			w.Line("private bool _isSetVerifiable;");
			w.Line("private global::KnockOff.Times? _setVerifiableTimes;");
			w.Line("private int _unconfiguredSetCount;");
			w.Line($"private {model.NullableValueType} _unconfiguredLastSetValue;");
			w.Line();
		}

		// Aggregate counts (private - use VerifyGet/VerifySet to check)
		if (model.HasGetter)
		{
			w.Line("private int TotalGetCount { get { var sum = _unconfiguredGetCount + (_onGetTracking?.CallCount ?? 0); if (_getSequence != null) foreach (var s in _getSequence) sum += s.Tracking.CallCount; return sum; } }");
		}
		if (model.HasSetter)
		{
			w.Line("private int TotalSetCount { get { var sum = _unconfiguredSetCount + (_onSetTracking?.CallCount ?? 0); if (_setSequence != null) foreach (var s in _setSequence) sum += s.Tracking.CallCount; return sum; } }");
		}
		if (model.HasGetter || model.HasSetter)
		{
			w.Line();
		}

		// LastSetValue for backward compatibility (if has setter)
		if (model.HasSetter)
		{
			w.Line($"/// <summary>The value from the last setter call (from most recently called registration).</summary>");
			w.Line($"public {model.NullableValueType} LastSetValue {{ get {{ if ((_onSetTracking?.CallCount ?? 0) > 0) return _onSetTracking!.LastValue; if (_setSequence != null) for (int i = _setSequence.Count - 1; i >= 0; i--) if (_setSequence[i].Tracking.CallCount > 0) return _setSequence[i].Tracking.LastValue; return _unconfiguredSetCount > 0 ? _unconfiguredLastSetValue : default; }} }}");
			w.Line();
		}

		// OnGet() method (if has getter)
		if (model.HasGetter)
		{
			w.Line($"/// <summary>Configures getter callback that repeats indefinitely. Returns tracking interface.</summary>");
			w.Line($"public global::KnockOff.IPropertyGetTracking OnGet(global::System.Func<{model.ValueType}> callback)");
			using (w.Braces())
			{
				w.Line("_getSequence = null;");
				w.Line("_getSequenceIndex = 0;");
				w.Line("_isGetVerifiable = false;");
				w.Line("_getVerifiableTimes = null;");
				w.Line("_onGet = callback;");
				w.Line("_onGetTracking = new PropertyGetTrackingImpl(this);");
				w.Line("return _onGetTracking;");
			}
			w.Line();

			w.Line($"/// <summary>Starts a getter callback sequence. Returns sequence for ThenGet chaining. Each callback runs exactly once.</summary>");
			w.Line($"public global::KnockOff.IPropertyGetSequence<{model.ValueType}> OnGetSequence(global::System.Func<{model.ValueType}> callback)");
			using (w.Braces())
			{
				w.Line("_onGet = null;");
				w.Line("_onGetTracking = null;");
				w.Line("_isGetVerifiable = false;");
				w.Line("_getVerifiableTimes = null;");
				w.Line($"_getSequence = new global::System.Collections.Generic.List<(global::System.Func<{model.ValueType}> Callback, PropertyGetTrackingImpl Tracking)>();");
				w.Line("var tracking = new PropertyGetTrackingImpl(this);");
				w.Line("_getSequence.Add((callback, tracking));");
				w.Line("_getSequenceIndex = 0;");
				w.Line("return new PropertyGetSequenceImpl(this);");
			}
			w.Line();
		}

		// OnSet() method (if has setter)
		if (model.HasSetter)
		{
			w.Line($"/// <summary>Configures setter callback that repeats indefinitely. Returns tracking interface.</summary>");
			w.Line($"public global::KnockOff.IPropertySetTracking<{model.ValueType}> OnSet(global::System.Action<{model.ValueType}> callback)");
			using (w.Braces())
			{
				w.Line("_setSequence = null;");
				w.Line("_setSequenceIndex = 0;");
				w.Line("_isSetVerifiable = false;");
				w.Line("_setVerifiableTimes = null;");
				w.Line("_onSet = callback;");
				w.Line("_onSetTracking = new PropertySetTrackingImpl(this);");
				w.Line("return _onSetTracking;");
			}
			w.Line();

			w.Line($"/// <summary>Starts a setter callback sequence. Returns sequence for ThenSet chaining. Each callback runs exactly once.</summary>");
			w.Line($"public global::KnockOff.IPropertySetSequence<{model.ValueType}> OnSetSequence(global::System.Action<{model.ValueType}> callback)");
			using (w.Braces())
			{
				w.Line("_onSet = null;");
				w.Line("_onSetTracking = null;");
				w.Line("_isSetVerifiable = false;");
				w.Line("_setVerifiableTimes = null;");
				w.Line($"_setSequence = new global::System.Collections.Generic.List<(global::System.Action<{model.ValueType}> Callback, PropertySetTrackingImpl Tracking)>();");
				w.Line("var tracking = new PropertySetTrackingImpl(this);");
				w.Line("_setSequence.Add((callback, tracking));");
				w.Line("_setSequenceIndex = 0;");
				w.Line("return new PropertySetSequenceImpl(this);");
			}
			w.Line();
		}

		// InvokeGet/InvokeSet methods
		if (model.HasGetter)
		{
			RenderInvokeGet(w, model, options);
		}
		if (model.HasSetter)
		{
			RenderInvokeSet(w, model, options);
		}

		// Reset method
		RenderResetMethod(w, model, hasSourceField: !string.IsNullOrEmpty(model.DeclaringInterface));

		// Verification methods
		RenderRegularVerificationMethods(w, model, fullInterceptorClassName);

		// Internal verification support
		RenderRegularInternalVerification(w, model);

		// Nested classes
		if (model.HasGetter)
		{
			RenderPropertyGetTrackingImpl(w, fullInterceptorClassName, isInitOnly: false);
			RenderPropertyGetSequenceImpl(w, model.ValueType, fullInterceptorClassName, isInitOnly: false);
		}
		if (model.HasSetter)
		{
			RenderPropertySetTrackingImpl(w, model.ValueType, fullInterceptorClassName);
			RenderPropertySetSequenceImpl(w, model.ValueType, fullInterceptorClassName);
		}
	}

	#endregion

	#region InvokeGet / InvokeSet Methods

	private static void RenderInvokeGet(
		CodeWriter w,
		UnifiedPropertyInterceptorModel model,
		PropertyInterceptorRenderOptions options)
	{
		var strictParam = options.IncludeStrictParameter ? "bool strict" : "";

		w.Line($"/// <summary>Invokes the configured getter callback. Called by explicit interface implementation.</summary>");
		w.Line($"internal {model.ValueType} InvokeGet({strictParam})");
		using (w.Braces())
		{
			// Priority 1: Sequence (if present and not exhausted)
			w.Line("if (_getSequence != null && _getSequenceIndex < _getSequence.Count)");
			using (w.Braces())
			{
				w.Line("var (callback, tracking) = _getSequence[_getSequenceIndex];");
				w.Line("tracking.RecordCall();");
				w.Line("_getSequenceIndex++;");
				w.Line("return callback();");
			}
			w.Line();

			// Priority 2: Repeating OnGet callback
			w.Line("if (_onGet != null && _onGetTracking != null)");
			using (w.Braces())
			{
				w.Line("_onGetTracking.RecordCall();");
				w.Line("return _onGet();");
			}
			w.Line();

			// No callback configured - track unconfigured call
			w.Line("_unconfiguredGetCount++;");
			w.Line();

			// Sequence exhausted in strict mode
			w.Line("if (_getSequence != null && _getSequenceIndex >= _getSequence.Count)");
			using (w.Braces())
			{
				w.Line($"if ({options.StrictAccessExpression}) throw global::KnockOff.StubException.SequenceExhausted(\"{model.PropertyName} (get)\");");
				w.Line("return _value;");
			}
			w.Line();

			// Priority 3: Source (if available)
			if (!string.IsNullOrEmpty(model.DeclaringInterface) && !model.IsInitOnly)
			{
				w.Line($"if (_source is {{ }} src) return src.{model.PropertyName};");
				w.Line();
			}

			// Priority 4: Strict mode check
			w.Line($"if ({options.StrictAccessExpression}) throw global::KnockOff.StubException.NotConfigured(\"\", \"{model.PropertyName}\");");

			// Priority 5: Return Value
			w.Line("return _value;");
		}
		w.Line();
	}

	private static void RenderInvokeSet(
		CodeWriter w,
		UnifiedPropertyInterceptorModel model,
		PropertyInterceptorRenderOptions options)
	{
		var strictParam = options.IncludeStrictParameter ? "bool strict, " : "";

		w.Line($"/// <summary>Invokes the configured setter callback. Called by explicit interface implementation.</summary>");
		w.Line($"internal void InvokeSet({strictParam}{model.ValueType} value)");
		using (w.Braces())
		{
			// Priority 1: Sequence (if present and not exhausted)
			w.Line("if (_setSequence != null && _setSequenceIndex < _setSequence.Count)");
			using (w.Braces())
			{
				w.Line("var (callback, tracking) = _setSequence[_setSequenceIndex];");
				w.Line("tracking.RecordCall(value);");
				w.Line("_setSequenceIndex++;");
				w.Line("callback(value);");
				w.Line("return;");
			}
			w.Line();

			// Priority 2: Repeating OnSet callback
			w.Line("if (_onSet != null && _onSetTracking != null)");
			using (w.Braces())
			{
				w.Line("_onSetTracking.RecordCall(value);");
				w.Line("_onSet(value);");
				w.Line("return;");
			}
			w.Line();

			// No callback configured - track unconfigured call
			w.Line("_unconfiguredSetCount++;");
			w.Line("_unconfiguredLastSetValue = value;");
			w.Line();

			// Sequence exhausted in strict mode
			w.Line("if (_setSequence != null && _setSequenceIndex >= _setSequence.Count)");
			using (w.Braces())
			{
				w.Line($"if ({options.StrictAccessExpression}) throw global::KnockOff.StubException.SequenceExhausted(\"{model.PropertyName} (set)\");");
				w.Line("_value = value;");
				w.Line("return;");
			}
			w.Line();

			// Priority 3: Source (if available)
			if (!string.IsNullOrEmpty(model.DeclaringInterface))
			{
				w.Line($"if (_source is {{ }} src) {{ src.{model.PropertyName} = value; return; }}");
				w.Line();
			}

			// Priority 4: Strict mode check
			w.Line($"if ({options.StrictAccessExpression}) throw global::KnockOff.StubException.NotConfigured(\"\", \"{model.PropertyName}\");");

			// Priority 5: Update Value
			w.Line("_value = value;");
		}
		w.Line();
	}

	#endregion

	#region Reset Method

	private static void RenderResetMethod(
		CodeWriter w,
		UnifiedPropertyInterceptorModel model,
		bool hasSourceField)
	{
		w.Line("/// <summary>Resets tracking state but preserves configuration (Value, OnGet, OnSet) and verifiable marking.</summary>");
		w.Line("public void Reset()");
		using (w.Braces())
		{
			if (model.HasGetter)
			{
				w.Line("_unconfiguredGetCount = 0;");
				w.Line("_onGetTracking?.Reset();");
				w.Line("if (_getSequence != null)");
				using (w.Braces())
				{
					w.Line("foreach (var (_, tracking) in _getSequence)");
					w.Line("\ttracking.Reset();");
				}
				w.Line("_getSequenceIndex = 0;");
			}
			if (model.HasSetter)
			{
				w.Line("_unconfiguredSetCount = 0;");
				w.Line("_unconfiguredLastSetValue = default;");
				w.Line("_onSetTracking?.Reset();");
				w.Line("if (_setSequence != null)");
				using (w.Braces())
				{
					w.Line("foreach (var (_, tracking) in _setSequence)");
					w.Line("\ttracking.Reset();");
				}
				w.Line("_setSequenceIndex = 0;");
			}
			if (hasSourceField)
			{
				w.Line("_source = null;");
			}
		}
		w.Line();
	}

	#endregion

	#region Verification Methods

	private static void RenderInitOnlyVerificationMethods(CodeWriter w, UnifiedPropertyInterceptorModel model, string fullInterceptorClassName)
	{
		// Verify() - combined
		w.Line("/// <summary>Verifies the property was accessed at least once. Throws VerificationException if not.</summary>");
		w.Line("public void Verify() => Verify(global::KnockOff.Times.AtLeastOnce);");
		w.Line();

		w.Line("/// <summary>Verifies total access count satisfies the Times constraint. Throws VerificationException if not.</summary>");
		w.Line("public void Verify(global::KnockOff.Times times)");
		using (w.Braces())
		{
			w.Line("var totalCount = TotalGetCount + _setCount;");
			w.Line("if (!times.Validate(totalCount))");
			w.Line($"\tthrow new global::KnockOff.VerificationException(new global::KnockOff.VerificationFailure(\"{model.PropertyName}\", times, totalCount));");
		}
		w.Line();

		// VerifyGet
		w.Line("/// <summary>Verifies the getter was accessed at least once. Throws VerificationException if not.</summary>");
		w.Line("public void VerifyGet() => VerifyGet(global::KnockOff.Times.AtLeastOnce);");
		w.Line();

		w.Line("/// <summary>Verifies getter access count satisfies the Times constraint. Throws VerificationException if not.</summary>");
		w.Line("public void VerifyGet(global::KnockOff.Times times)");
		using (w.Braces())
		{
			w.Line("if (!times.Validate(TotalGetCount))");
			w.Line($"\tthrow new global::KnockOff.VerificationException(new global::KnockOff.VerificationFailure(\"{model.PropertyName} (get)\", times, TotalGetCount));");
		}
		w.Line();

		// VerifySet
		w.Line("/// <summary>Verifies the init setter was accessed at least once. Throws VerificationException if not.</summary>");
		w.Line("public void VerifySet() => VerifySet(global::KnockOff.Times.AtLeastOnce);");
		w.Line();

		w.Line("/// <summary>Verifies init setter access count satisfies the Times constraint. Throws VerificationException if not.</summary>");
		w.Line("public void VerifySet(global::KnockOff.Times times)");
		using (w.Braces())
		{
			w.Line("if (!times.Validate(_setCount))");
			w.Line($"\tthrow new global::KnockOff.VerificationException(new global::KnockOff.VerificationFailure(\"{model.PropertyName} (set)\", times, _setCount));");
		}
		w.Line();

		// Verifiable fluent methods
		w.Line($"/// <summary>Marks this property for verification by Stub.Verify(). Returns this for fluent chaining.</summary>");
		w.Line($"public {fullInterceptorClassName} Verifiable() {{ _isGetVerifiable = true; _getVerifiableTimes = null; return this; }}");
		w.Line();
		w.Line($"/// <summary>Marks this property for verification by Stub.Verify() with Times constraint. Returns this for fluent chaining.</summary>");
		w.Line($"public {fullInterceptorClassName} Verifiable(global::KnockOff.Times times) {{ _isGetVerifiable = true; _getVerifiableTimes = times; return this; }}");
		w.Line();
	}

	private static void RenderRegularVerificationMethods(CodeWriter w, UnifiedPropertyInterceptorModel model, string fullInterceptorClassName)
	{
		// Build total count expression based on available accessors
		var totalCountExpr = model.HasGetter && model.HasSetter
			? "TotalGetCount + TotalSetCount"
			: (model.HasGetter ? "TotalGetCount" : "TotalSetCount");

		// Verify() - combined
		w.Line("/// <summary>Verifies the property was accessed at least once. Throws VerificationException if not.</summary>");
		w.Line("public void Verify() => Verify(global::KnockOff.Times.AtLeastOnce);");
		w.Line();

		w.Line("/// <summary>Verifies total access count satisfies the Times constraint. Throws VerificationException if not.</summary>");
		w.Line("public void Verify(global::KnockOff.Times times)");
		using (w.Braces())
		{
			w.Line($"var totalCount = {totalCountExpr};");
			w.Line("if (!times.Validate(totalCount))");
			w.Line($"\tthrow new global::KnockOff.VerificationException(new global::KnockOff.VerificationFailure(\"{model.PropertyName}\", times, totalCount));");
		}
		w.Line();

		// VerifyGet (if has getter)
		if (model.HasGetter)
		{
			w.Line("/// <summary>Verifies the getter was accessed at least once. Throws VerificationException if not.</summary>");
			w.Line("public void VerifyGet() => VerifyGet(global::KnockOff.Times.AtLeastOnce);");
			w.Line();

			w.Line("/// <summary>Verifies getter access count satisfies the Times constraint. Throws VerificationException if not.</summary>");
			w.Line("public void VerifyGet(global::KnockOff.Times times)");
			using (w.Braces())
			{
				w.Line("if (!times.Validate(TotalGetCount))");
				w.Line($"\tthrow new global::KnockOff.VerificationException(new global::KnockOff.VerificationFailure(\"{model.PropertyName} (get)\", times, TotalGetCount));");
			}
			w.Line();
		}

		// VerifySet (if has setter)
		if (model.HasSetter)
		{
			w.Line("/// <summary>Verifies the setter was accessed at least once. Throws VerificationException if not.</summary>");
			w.Line("public void VerifySet() => VerifySet(global::KnockOff.Times.AtLeastOnce);");
			w.Line();

			w.Line("/// <summary>Verifies setter access count satisfies the Times constraint. Throws VerificationException if not.</summary>");
			w.Line("public void VerifySet(global::KnockOff.Times times)");
			using (w.Braces())
			{
				w.Line("if (!times.Validate(TotalSetCount))");
				w.Line($"\tthrow new global::KnockOff.VerificationException(new global::KnockOff.VerificationFailure(\"{model.PropertyName} (set)\", times, TotalSetCount));");
			}
			w.Line();
		}

		// Verifiable fluent methods
		w.Line($"/// <summary>Marks this property for verification by Stub.Verify(). Returns this for fluent chaining.</summary>");
		var verifiableBody = model.HasGetter && model.HasSetter
			? "_isGetVerifiable = true; _getVerifiableTimes = null; _isSetVerifiable = true; _setVerifiableTimes = null;"
			: (model.HasGetter ? "_isGetVerifiable = true; _getVerifiableTimes = null;" : "_isSetVerifiable = true; _setVerifiableTimes = null;");
		w.Line($"public {fullInterceptorClassName} Verifiable() {{ {verifiableBody} return this; }}");
		w.Line();
		var verifiableTimesBody = model.HasGetter && model.HasSetter
			? "_isGetVerifiable = true; _getVerifiableTimes = times; _isSetVerifiable = true; _setVerifiableTimes = times;"
			: (model.HasGetter ? "_isGetVerifiable = true; _getVerifiableTimes = times;" : "_isSetVerifiable = true; _setVerifiableTimes = times;");
		w.Line($"/// <summary>Marks this property for verification by Stub.Verify() with Times constraint. Returns this for fluent chaining.</summary>");
		w.Line($"public {fullInterceptorClassName} Verifiable(global::KnockOff.Times times) {{ {verifiableTimesBody} return this; }}");
		w.Line();
	}

	#endregion

	#region Internal Verification Support

	private static void RenderInitOnlyInternalVerification(CodeWriter w, UnifiedPropertyInterceptorModel model)
	{
		w.Line("/// <summary>Whether this property was marked with Verifiable().</summary>");
		w.Line("internal bool IsVerifiable => _isGetVerifiable;");
		w.Line();

		w.Line("/// <summary>Whether this property has been configured (Value set or OnGet configured).</summary>");
		w.Line("internal bool IsConfigured => _valueSet || _onGet != null || (_getSequence?.Count ?? 0) > 0;");
		w.Line();

		w.Line("/// <summary>Checks verification for Stub.Verify() - only checks if marked verifiable.</summary>");
		w.Line($"internal global::KnockOff.VerificationFailure? CheckVerification()");
		using (w.Braces())
		{
			w.Line("if (!_isGetVerifiable) return null;");
			w.Line("var times = _getVerifiableTimes ?? global::KnockOff.Times.AtLeastOnce;");
			w.Line("var totalCount = TotalGetCount + _setCount;");
			w.Line($"return times.Validate(totalCount) ? null : new global::KnockOff.VerificationFailure(\"{model.PropertyName}\", times, totalCount);");
		}
		w.Line();

		w.Line("/// <summary>Checks verification for Stub.VerifyAll() - checks if configured.</summary>");
		w.Line($"internal global::KnockOff.VerificationFailure? CheckVerificationAll()");
		using (w.Braces())
		{
			w.Line("if (!IsConfigured) return null;");
			w.Line("var totalCount = TotalGetCount + _setCount;");
			w.Line($"return totalCount >= 1 ? null : new global::KnockOff.VerificationFailure(\"{model.PropertyName}\", global::KnockOff.Times.AtLeastOnce, totalCount);");
		}
		w.Line();
	}

	private static void RenderRegularInternalVerification(CodeWriter w, UnifiedPropertyInterceptorModel model)
	{
		var isVerifiableExpr = model.HasGetter && model.HasSetter
			? "_isGetVerifiable || _isSetVerifiable"
			: (model.HasGetter ? "_isGetVerifiable" : "_isSetVerifiable");

		var isConfiguredParts = new System.Collections.Generic.List<string> { "_valueSet" };
		if (model.HasGetter) isConfiguredParts.Add("_onGet != null || (_getSequence?.Count ?? 0) > 0");
		if (model.HasSetter) isConfiguredParts.Add("_onSet != null || (_setSequence?.Count ?? 0) > 0");
		var isConfiguredExpr = string.Join(" || ", isConfiguredParts);

		var totalCountExpr = model.HasGetter && model.HasSetter
			? "TotalGetCount + TotalSetCount"
			: (model.HasGetter ? "TotalGetCount" : "TotalSetCount");

		w.Line("/// <summary>Whether this property was marked with Verifiable().</summary>");
		w.Line($"internal bool IsVerifiable => {isVerifiableExpr};");
		w.Line();

		w.Line("/// <summary>Whether this property has been configured.</summary>");
		w.Line($"internal bool IsConfigured => {isConfiguredExpr};");
		w.Line();

		w.Line("/// <summary>Checks verification for Stub.Verify() - only checks if marked verifiable.</summary>");
		w.Line($"internal global::KnockOff.VerificationFailure? CheckVerification()");
		using (w.Braces())
		{
			w.Line($"if (!({isVerifiableExpr})) return null;");

			// When BOTH get and set are verifiable (e.g., Verifiable() called on interceptor),
			// check combined count - "property was used" means either get or set.
			// When only one is verifiable (e.g., OnGet().Verifiable()), check individually.
			if (model.HasGetter && model.HasSetter)
			{
				w.Line("if (_isGetVerifiable && _isSetVerifiable)");
				using (w.Braces())
				{
					w.Line("// Both marked verifiable - check combined count (either accessor satisfies)");
					w.Line("var times = _getVerifiableTimes ?? _setVerifiableTimes ?? global::KnockOff.Times.AtLeastOnce;");
					w.Line($"var totalCount = {totalCountExpr};");
					w.Line($"return times.Validate(totalCount) ? null : new global::KnockOff.VerificationFailure(\"{model.PropertyName}\", times, totalCount);");
				}
			}

			// Check getter verifiable (when only getter is marked)
			if (model.HasGetter)
			{
				var condition = model.HasSetter ? "_isGetVerifiable && !_isSetVerifiable" : "_isGetVerifiable";
				w.Line($"if ({condition})");
				using (w.Braces())
				{
					w.Line("var times = _getVerifiableTimes ?? global::KnockOff.Times.AtLeastOnce;");
					w.Line($"if (!times.Validate(TotalGetCount)) return new global::KnockOff.VerificationFailure(\"{model.PropertyName} (get)\", times, TotalGetCount);");
				}
			}
			// Check setter verifiable (when only setter is marked)
			if (model.HasSetter)
			{
				var condition = model.HasGetter ? "_isSetVerifiable && !_isGetVerifiable" : "_isSetVerifiable";
				w.Line($"if ({condition})");
				using (w.Braces())
				{
					w.Line("var times = _setVerifiableTimes ?? global::KnockOff.Times.AtLeastOnce;");
					w.Line($"if (!times.Validate(TotalSetCount)) return new global::KnockOff.VerificationFailure(\"{model.PropertyName} (set)\", times, TotalSetCount);");
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
			w.Line($"var totalCount = {totalCountExpr};");
			w.Line($"return totalCount >= 1 ? null : new global::KnockOff.VerificationFailure(\"{model.PropertyName}\", global::KnockOff.Times.AtLeastOnce, totalCount);");
		}
		w.Line();
	}

	#endregion

	#region Nested PropertyGetTrackingImpl

	private static void RenderPropertyGetTrackingImpl(
		CodeWriter w,
		string interceptorClassName,
		bool isInitOnly)
	{
		w.Line($"/// <summary>Tracks invocations for getter callback registration.</summary>");
		w.Line($"private sealed class PropertyGetTrackingImpl : global::KnockOff.IPropertyGetTracking");
		using (w.Braces())
		{
			w.Line($"private readonly {interceptorClassName} _interceptor;");
			w.Line();

			w.Line($"public PropertyGetTrackingImpl({interceptorClassName} interceptor) => _interceptor = interceptor;");
			w.Line();

			w.Line("internal int CallCount { get; private set; }");
			w.Line();

			w.Line("/// <summary>Records a call to this callback.</summary>");
			w.Line("public void RecordCall() => CallCount++;");
			w.Line();

			w.Line("/// <summary>Resets tracking state.</summary>");
			w.Line("public void Reset() => CallCount = 0;");
			w.Line();

			w.Line("/// <summary>Verifies callback was invoked at least once. Throws VerificationException if not.</summary>");
			w.Line("public void Verify() => Verify(global::KnockOff.Times.AtLeastOnce);");
			w.Line();

			w.Line("/// <summary>Verifies call count satisfies the Times constraint. Throws VerificationException if not.</summary>");
			w.Line("public void Verify(global::KnockOff.Times times)");
			using (w.Braces())
			{
				w.Line("if (!times.Validate(CallCount))");
				w.Line("\tthrow new global::KnockOff.VerificationException(new global::KnockOff.VerificationFailure(\"property getter\", times, CallCount));");
			}
			w.Line();

			w.Line("/// <summary>Marks for verification by Stub.Verify(). Returns this for fluent chaining.</summary>");
			w.Line("public global::KnockOff.IPropertyGetTracking Verifiable()");
			using (w.Braces())
			{
				w.Line("_interceptor._isGetVerifiable = true;");
				w.Line("_interceptor._getVerifiableTimes = null;");
				w.Line("return this;");
			}
			w.Line();

			w.Line("/// <summary>Marks for verification by Stub.Verify() with Times constraint. Returns this for fluent chaining.</summary>");
			w.Line("public global::KnockOff.IPropertyGetTracking Verifiable(global::KnockOff.Times times)");
			using (w.Braces())
			{
				w.Line("_interceptor._isGetVerifiable = true;");
				w.Line("_interceptor._getVerifiableTimes = times;");
				w.Line("return this;");
			}
		}
		w.Line();
	}

	#endregion

	#region Nested PropertySetTrackingImpl

	private static void RenderPropertySetTrackingImpl(
		CodeWriter w,
		string valueType,
		string interceptorClassName)
	{
		w.Line($"/// <summary>Tracks invocations for setter callback registration.</summary>");
		w.Line($"private sealed class PropertySetTrackingImpl : global::KnockOff.IPropertySetTracking<{valueType}>");
		using (w.Braces())
		{
			w.Line($"private readonly {interceptorClassName} _interceptor;");
			w.Line();

			w.Line($"public PropertySetTrackingImpl({interceptorClassName} interceptor) => _interceptor = interceptor;");
			w.Line();

			w.Line($"private {valueType} _lastValue = default!;");
			w.Line();

			w.Line("internal int CallCount { get; private set; }");
			w.Line();

			w.Line($"/// <summary>Last value passed to this setter callback. Default if never called.</summary>");
			w.Line($"public {valueType} LastValue => _lastValue;");
			w.Line();

			w.Line("/// <summary>Records a call to this callback.</summary>");
			w.Line($"public void RecordCall({valueType} value) {{ CallCount++; _lastValue = value; }}");
			w.Line();

			w.Line("/// <summary>Resets tracking state.</summary>");
			w.Line("public void Reset() { CallCount = 0; _lastValue = default!; }");
			w.Line();

			w.Line("/// <summary>Verifies callback was invoked at least once. Throws VerificationException if not.</summary>");
			w.Line("public void Verify() => Verify(global::KnockOff.Times.AtLeastOnce);");
			w.Line();

			w.Line("/// <summary>Verifies call count satisfies the Times constraint. Throws VerificationException if not.</summary>");
			w.Line("public void Verify(global::KnockOff.Times times)");
			using (w.Braces())
			{
				w.Line("if (!times.Validate(CallCount))");
				w.Line("\tthrow new global::KnockOff.VerificationException(new global::KnockOff.VerificationFailure(\"property setter\", times, CallCount));");
			}
			w.Line();

			w.Line("/// <summary>Marks for verification by Stub.Verify(). Returns this for fluent chaining.</summary>");
			w.Line($"public global::KnockOff.IPropertySetTracking<{valueType}> Verifiable()");
			using (w.Braces())
			{
				w.Line("_interceptor._isSetVerifiable = true;");
				w.Line("_interceptor._setVerifiableTimes = null;");
				w.Line("return this;");
			}
			w.Line();

			w.Line("/// <summary>Marks for verification by Stub.Verify() with Times constraint. Returns this for fluent chaining.</summary>");
			w.Line($"public global::KnockOff.IPropertySetTracking<{valueType}> Verifiable(global::KnockOff.Times times)");
			using (w.Braces())
			{
				w.Line("_interceptor._isSetVerifiable = true;");
				w.Line("_interceptor._setVerifiableTimes = times;");
				w.Line("return this;");
			}
		}
		w.Line();
	}

	#endregion

	#region Nested PropertyGetSequenceImpl

	private static void RenderPropertyGetSequenceImpl(
		CodeWriter w,
		string valueType,
		string interceptorClassName,
		bool isInitOnly)
	{
		w.Line($"/// <summary>Sequence implementation for ThenGet chaining.</summary>");
		w.Line($"private sealed class PropertyGetSequenceImpl : global::KnockOff.IPropertyGetSequence<{valueType}>");
		using (w.Braces())
		{
			w.Line($"private readonly {interceptorClassName} _interceptor;");
			w.Line();

			w.Line($"public PropertyGetSequenceImpl({interceptorClassName} interceptor) => _interceptor = interceptor;");
			w.Line();

			w.Line($"/// <summary>Adds another getter callback to the sequence. Each callback runs exactly once.</summary>");
			w.Line($"public global::KnockOff.IPropertyGetSequence<{valueType}> ThenGet(global::System.Func<{valueType}> callback)");
			using (w.Braces())
			{
				w.Line("var tracking = new PropertyGetTrackingImpl(_interceptor);");
				w.Line("_interceptor._getSequence!.Add((callback, tracking));");
				w.Line("return this;");
			}
			w.Line();

			w.Line("/// <summary>Verifies the entire sequence was executed (all callbacks invoked). Throws VerificationException if incomplete.</summary>");
			w.Line("public void Verify()");
			using (w.Braces())
			{
				w.Line("if (_interceptor._getSequence == null) return;");
				w.Line("var sequenceLength = _interceptor._getSequence.Count;");
				w.Line("var completedCount = _interceptor._getSequenceIndex;");
				w.Line("if (completedCount < sequenceLength)");
				w.Line("\tthrow new global::KnockOff.VerificationException(global::KnockOff.VerificationFailure.SequenceIncomplete(\"property getter\", sequenceLength, completedCount));");
			}
			w.Line();

			w.Line("/// <summary>Resets all tracking in the sequence.</summary>");
			w.Line("public void Reset() => _interceptor.Reset();");
			w.Line();

			w.Line("/// <summary>Marks this sequence for verification by Stub.Verify(). Returns this for fluent chaining.</summary>");
			w.Line($"public global::KnockOff.IPropertyGetSequence<{valueType}> Verifiable()");
			using (w.Braces())
			{
				w.Line("_interceptor._isGetVerifiable = true;");
				w.Line("_interceptor._getVerifiableTimes = null;");
				w.Line("return this;");
			}
		}
		w.Line();
	}

	#endregion

	#region Nested PropertySetSequenceImpl

	private static void RenderPropertySetSequenceImpl(
		CodeWriter w,
		string valueType,
		string interceptorClassName)
	{
		w.Line($"/// <summary>Sequence implementation for ThenSet chaining.</summary>");
		w.Line($"private sealed class PropertySetSequenceImpl : global::KnockOff.IPropertySetSequence<{valueType}>");
		using (w.Braces())
		{
			w.Line($"private readonly {interceptorClassName} _interceptor;");
			w.Line();

			w.Line($"public PropertySetSequenceImpl({interceptorClassName} interceptor) => _interceptor = interceptor;");
			w.Line();

			w.Line($"/// <summary>Adds another setter callback to the sequence. Each callback runs exactly once.</summary>");
			w.Line($"public global::KnockOff.IPropertySetSequence<{valueType}> ThenSet(global::System.Action<{valueType}> callback)");
			using (w.Braces())
			{
				w.Line("var tracking = new PropertySetTrackingImpl(_interceptor);");
				w.Line("_interceptor._setSequence!.Add((callback, tracking));");
				w.Line("return this;");
			}
			w.Line();

			w.Line("/// <summary>Verifies the entire sequence was executed (all callbacks invoked). Throws VerificationException if incomplete.</summary>");
			w.Line("public void Verify()");
			using (w.Braces())
			{
				w.Line("if (_interceptor._setSequence == null) return;");
				w.Line("var sequenceLength = _interceptor._setSequence.Count;");
				w.Line("var completedCount = _interceptor._setSequenceIndex;");
				w.Line("if (completedCount < sequenceLength)");
				w.Line("\tthrow new global::KnockOff.VerificationException(global::KnockOff.VerificationFailure.SequenceIncomplete(\"property setter\", sequenceLength, completedCount));");
			}
			w.Line();

			w.Line("/// <summary>Resets all tracking in the sequence.</summary>");
			w.Line("public void Reset() => _interceptor.Reset();");
			w.Line();

			w.Line("/// <summary>Marks this sequence for verification by Stub.Verify(). Returns this for fluent chaining.</summary>");
			w.Line($"public global::KnockOff.IPropertySetSequence<{valueType}> Verifiable()");
			using (w.Braces())
			{
				w.Line("_interceptor._isSetVerifiable = true;");
				w.Line("_interceptor._setVerifiableTimes = null;");
				w.Line("return this;");
			}
		}
		w.Line();
	}

	#endregion

	#region Helpers

	private static string GetDefaultValueSuffix(string defaultExpression)
	{
		if (string.IsNullOrEmpty(defaultExpression) || defaultExpression == "default!")
			return " = default!;";

		return $" = {defaultExpression};";
	}

	#endregion
}
