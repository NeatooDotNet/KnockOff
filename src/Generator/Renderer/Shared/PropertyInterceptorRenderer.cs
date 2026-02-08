// src/Generator/Renderer/Shared/PropertyInterceptorRenderer.cs
#nullable enable
using KnockOff.Model.Shared;

namespace KnockOff.Renderer.Shared;

/// <summary>
/// Renders property interceptor classes for both inline and flat stubs.
/// Generates Get() returning IPropertyGetBuilder (repeating callback, elevatable to sequence via ThenGet),
/// Set() returning IPropertySetBuilder similarly for setters,
/// nested builder and sequence implementation classes, InvokeGet/InvokeSet methods, and verification.
/// </summary>
internal static class PropertyInterceptorRenderer
{
	/// <summary>
	/// Renders a complete property interceptor class.
	/// For init-only properties, generates getter-only API (no Set methods).
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

		// Internal value storage (init-only - used by init setter to store value)
		w.Line("private bool _valueSet;");
		w.Line($"private {model.ValueType} _value = default!;");
		w.Line("internal void SetValue(" + model.ValueType + " value) { _value = value; _valueSet = true; }");
		w.Line();

		// Getter tracking and sequence storage
		w.Line($"private global::System.Func<{model.ValueType}>? _get;");
		w.Line("private PropertyGetBuilderImpl? _getTracking;");
		w.Line($"private global::System.Collections.Generic.List<(global::System.Func<{model.ValueType}> Callback, PropertyGetBuilderImpl Tracking)>? _getSequence;");
		w.Line("private int _getSequenceIndex;");
		w.Line("private bool _getRepeatLastValue = true;");
		w.Line();

		// Verifiable state (for getter)
		w.Line("private bool _isGetVerifiable;");
		w.Line("private global::KnockOff.Called? _getVerifiableTimes;");
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
		w.Line("private int TotalGetCount { get { var sum = _unconfiguredGetCount + (_getTracking?._callCount ?? 0); if (_getSequence != null) foreach (var s in _getSequence) sum += s.Tracking._callCount; return sum; } }");
		w.Line();

		// Get() - repeating callback, returns IPropertyGetBuilder
		w.Line($"/// <summary>Configures getter callback that repeats indefinitely. Returns builder for tracking and sequence chaining.</summary>");
		w.Line($"public global::KnockOff.IPropertyGetBuilder<{model.ValueType}> Get(global::System.Func<{model.ValueType}> callback)");
		using (w.Braces())
		{
			w.Line("_getSequence = null;");
			w.Line("_getSequenceIndex = 0;");
			w.Line("_isGetVerifiable = false;");
			w.Line("_getVerifiableTimes = null;");
			w.Line("_get = callback;");
			w.Line("_getTracking = new PropertyGetBuilderImpl(this);");
			w.Line("return _getTracking;");
		}
		w.Line();

		// Get(value) - wrapper method for value-based configuration
		w.Line($"/// <summary>Configures getter to return the specified value. Returns builder for tracking and sequence chaining.</summary>");
		w.Line($"public global::KnockOff.IPropertyGetBuilder<{model.ValueType}> Get({model.ValueType} value) => Get(() => value);");
		w.Line();

		// RecordSet - tracks init setter invocation (for verification)
		w.Line("/// <summary>Records an init setter access.</summary>");
		w.Line($"public void RecordSet({model.NullableValueType} value) {{ _setCount++; LastSetValue = value; }}");
		w.Line();

		// InvokeGet method
		RenderInvokeGet(w, model, options);

		// Reset method
		w.Line("/// <summary>Resets tracking state but preserves configuration (Get) and verifiable marking.</summary>");
		w.Line("public void Reset()");
		using (w.Braces())
		{
			w.Line("_unconfiguredGetCount = 0;");
			w.Line("_setCount = 0;");
			w.Line("LastSetValue = default;");
			w.Line("_getTracking?.Reset();");
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
		RenderPropertyGetBuilderImpl(w, model.ValueType, fullInterceptorClassName, isInitOnly: true);
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
			w.Line($"/// <summary>Source object to delegate to when no Get/Set is configured.</summary>");
			w.Line($"internal {model.DeclaringInterface}? _source;");
			w.Line();
		}

		// Backing field for property round-trip storage (set via interface, then get returns it)
		// This enables basic property behavior where setting a value allows getting it back
		if (model.HasSetter && model.HasGetter)
		{
			w.Line("private bool _valueSet;");
			w.Line($"private {model.ValueType} _value = default!;");
			w.Line();
		}

		// Getter storage and tracking (if has getter)
		if (model.HasGetter)
		{
			w.Line($"private global::System.Func<{model.ValueType}>? _get;");
			w.Line("private PropertyGetBuilderImpl? _getTracking;");
			w.Line($"private global::System.Collections.Generic.List<(global::System.Func<{model.ValueType}> Callback, PropertyGetBuilderImpl Tracking)>? _getSequence;");
			w.Line("private int _getSequenceIndex;");
			w.Line("private bool _getRepeatLastValue = true;");
			w.Line("private bool _isGetVerifiable;");
			w.Line("private global::KnockOff.Called? _getVerifiableTimes;");
			w.Line("private int _unconfiguredGetCount;");
			w.Line();
		}

		// Setter storage and tracking (if has setter)
		if (model.HasSetter)
		{
			w.Line($"private global::System.Action<{model.ValueType}>? _set;");
			w.Line("private PropertySetBuilderImpl? _setTracking;");
			w.Line($"private global::System.Collections.Generic.List<(global::System.Action<{model.ValueType}> Callback, PropertySetBuilderImpl Tracking)>? _setSequence;");
			w.Line("private int _setSequenceIndex;");
			w.Line("private bool _setRepeatLastValue = true;");
			w.Line("private bool _isSetVerifiable;");
			w.Line("private global::KnockOff.Called? _setVerifiableTimes;");
			w.Line("private int _unconfiguredSetCount;");
			w.Line($"private {model.NullableValueType} _unconfiguredLastSetValue;");
			w.Line();
		}

		// Aggregate counts (private - use VerifyGet/VerifySet to check)
		if (model.HasGetter)
		{
			w.Line("private int TotalGetCount { get { var sum = _unconfiguredGetCount + (_getTracking?._callCount ?? 0); if (_getSequence != null) foreach (var s in _getSequence) sum += s.Tracking._callCount; return sum; } }");
		}
		if (model.HasSetter)
		{
			w.Line("private int TotalSetCount { get { var sum = _unconfiguredSetCount + (_setTracking?._callCount ?? 0); if (_setSequence != null) foreach (var s in _setSequence) sum += s.Tracking._callCount; return sum; } }");
		}
		if (model.HasGetter || model.HasSetter)
		{
			w.Line();
		}

		// LastSetValue for backward compatibility (if has setter)
		if (model.HasSetter)
		{
			w.Line($"/// <summary>The value from the last setter call (from most recently called registration).</summary>");
			w.Line($"public {model.NullableValueType} LastSetValue {{ get {{ if ((_setTracking?._callCount ?? 0) > 0) return _setTracking!.LastValue; if (_setSequence != null) for (int i = _setSequence.Count - 1; i >= 0; i--) if (_setSequence[i].Tracking._callCount > 0) return _setSequence[i].Tracking.LastValue; return _unconfiguredSetCount > 0 ? _unconfiguredLastSetValue : default; }} }}");
			w.Line();
		}

		// Get() method (if has getter)
		if (model.HasGetter)
		{
			w.Line($"/// <summary>Configures getter callback that repeats indefinitely. Returns builder for tracking and sequence chaining.</summary>");
			w.Line($"public global::KnockOff.IPropertyGetBuilder<{model.ValueType}> Get(global::System.Func<{model.ValueType}> callback)");
			using (w.Braces())
			{
				w.Line("_getSequence = null;");
				w.Line("_getSequenceIndex = 0;");
				w.Line("_isGetVerifiable = false;");
				w.Line("_getVerifiableTimes = null;");
				w.Line("_get = callback;");
				w.Line("_getTracking = new PropertyGetBuilderImpl(this);");
				w.Line("return _getTracking;");
			}
			w.Line();

			// Get(value) - wrapper method for value-based configuration
			w.Line($"/// <summary>Configures getter to return the specified value. Returns builder for tracking and sequence chaining.</summary>");
			w.Line($"public global::KnockOff.IPropertyGetBuilder<{model.ValueType}> Get({model.ValueType} value) => Get(() => value);");
			w.Line();
		}

		// Set() method (if has setter)
		if (model.HasSetter)
		{
			w.Line($"/// <summary>Configures setter callback that repeats indefinitely. Returns builder for tracking and sequence chaining.</summary>");
			w.Line($"public global::KnockOff.IPropertySetBuilder<{model.ValueType}> Set(global::System.Action<{model.ValueType}> callback)");
			using (w.Braces())
			{
				w.Line("_setSequence = null;");
				w.Line("_setSequenceIndex = 0;");
				w.Line("_isSetVerifiable = false;");
				w.Line("_setVerifiableTimes = null;");
				w.Line("_set = callback;");
				w.Line("_setTracking = new PropertySetBuilderImpl(this);");
				w.Line("return _setTracking;");
			}
			w.Line();

		}

		// User override support methods - these are used by RenderPropertyUserOverrideImplementation
		// They separate tracking from callback invocation for the base class pattern
		RenderUserOverrideSupportMethods(w, model);

		// Ref return backing field (for ref return properties)
		if (model.IsRefReturn)
		{
			w.Line("#pragma warning disable CS8618 // Ref return backing field initialized by InvokeRefGet before use");
			w.Line($"internal {model.ValueType} _refReturnBacking;");
			w.Line("#pragma warning restore CS8618");
			w.Line();
		}

		// InvokeGet/InvokeSet methods
		if (model.HasGetter)
		{
			RenderInvokeGet(w, model, options);
			// InvokeRefGet for ref return properties - writes to _refReturnBacking instead of returning
			if (model.IsRefReturn)
			{
				RenderInvokeRefGet(w, model, options);
			}
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
			RenderPropertyGetBuilderImpl(w, model.ValueType, fullInterceptorClassName, isInitOnly: false);
			RenderPropertyGetSequenceImpl(w, model.ValueType, fullInterceptorClassName, isInitOnly: false);
		}
		if (model.HasSetter)
		{
			RenderPropertySetBuilderImpl(w, model.ValueType, fullInterceptorClassName);
			RenderPropertySetSequenceImpl(w, model.ValueType, fullInterceptorClassName);
		}
	}

	#endregion

	#region User Override Support Methods

	/// <summary>
	/// Renders methods for user override property support (base class pattern).
	/// These methods separate tracking from callback invocation:
	/// - RecordGet() / RecordSet(value) - tracking only
	/// - HasGet / HasSet - check if callback is configured
	/// - InvokeGetCallback() / InvokeSetCallback(value) - invoke callback without tracking
	/// </summary>
	private static void RenderUserOverrideSupportMethods(CodeWriter w, UnifiedPropertyInterceptorModel model)
	{
		// Getter support
		if (model.HasGetter)
		{
			w.Line("/// <summary>Records a getter access (tracking only, does not invoke callback). Used by user override pattern.</summary>");
			w.Line("internal void RecordGet() => _unconfiguredGetCount++;");
			w.Line();

			w.Line("/// <summary>Returns true if Get is configured (callback or sequence). Used by user override pattern.</summary>");
			w.Line("internal bool HasGet => _get != null || (_getSequence?.Count ?? 0) > 0;");
			w.Line();

			w.Line("/// <summary>Invokes the configured getter callback without tracking. Used by user override pattern.</summary>");
			w.Line($"internal {model.ValueType} InvokeGetCallback()");
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
				// Priority 2: Repeating Get callback
				w.Line("if (_get != null && _getTracking != null)");
				using (w.Braces())
				{
					w.Line("_getTracking.RecordCall();");
					w.Line("return _get();");
				}
				w.Line("throw new global::System.InvalidOperationException(\"InvokeGetCallback called without callback configured\");");
			}
			w.Line();
		}

		// Setter support
		if (model.HasSetter)
		{
			w.Line("/// <summary>Records a setter access (tracking only, does not invoke callback). Used by user override pattern.</summary>");
			w.Line($"internal void RecordSet({model.ValueType} value) {{ _unconfiguredSetCount++; _unconfiguredLastSetValue = value; }}");
			w.Line();

			w.Line("/// <summary>Returns true if Set is configured (callback or sequence). Used by user override pattern.</summary>");
			w.Line("internal bool HasSet => _set != null || (_setSequence?.Count ?? 0) > 0;");
			w.Line();

			w.Line("/// <summary>Invokes the configured setter callback without tracking. Used by user override pattern.</summary>");
			w.Line($"internal void InvokeSetCallback({model.ValueType} value)");
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
				// Priority 2: Repeating Set callback
				w.Line("if (_set != null && _setTracking != null)");
				using (w.Braces())
				{
					w.Line("_setTracking.RecordCall(value);");
					w.Line("_set(value);");
					w.Line("return;");
				}
				w.Line("throw new global::System.InvalidOperationException(\"InvokeSetCallback called without callback configured\");");
			}
			w.Line();
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

			// Priority 2: Repeating Get callback
			w.Line("if (_get != null && _getTracking != null)");
			using (w.Braces())
			{
				w.Line("_getTracking.RecordCall();");
				w.Line("return _get();");
			}
			w.Line();

			// No callback configured - track unconfigured call
			w.Line("_unconfiguredGetCount++;");
			w.Line();

			// Sequence exhausted - check strict mode first (always throws), then repeat-last-value, then default
			w.Line("if (_getSequence != null && _getSequenceIndex >= _getSequence.Count)");
			using (w.Braces())
			{
				// Strict mode ALWAYS throws on exhaustion (regardless of _repeatLastValue)
				w.Line($"if ({options.StrictAccessExpression}) throw global::KnockOff.StubException.SequenceExhausted(\"{model.PropertyName} (get)\");");
				// Repeat last value if enabled (default behavior in non-strict mode)
				w.Line("if (_getRepeatLastValue && _getSequence.Count > 0)");
				using (w.Braces())
				{
					w.Line("var (callback, tracking) = _getSequence[_getSequence.Count - 1];");
					w.Line("tracking.RecordCall();");
					w.Line("return callback();");
				}
				// Return fallback value (only reached when _repeatLastValue is false via ThenDefault())
				// Init-only: return _value (set by init setter)
				// Regular with setter: return backing value if set, otherwise default
				// Getter-only: use smart default (from DefaultExpression)
				if (model.IsInitOnly)
				{
					w.Line("return _value;");
				}
				else if (model.HasSetter)
				{
					w.Line("return _valueSet ? _value : default!;");
				}
				else
				{
					// Getter-only property: use smart default
					w.Line($"return {model.DefaultExpression};");
				}
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

			// Priority 5: Return fallback value
			// Init-only: return _value (set by init setter)
			// Regular with setter: return backing value if set, otherwise default
			// Getter-only: use smart default (from DefaultExpression)
			if (model.IsInitOnly)
			{
				w.Line("return _value;");
			}
			else if (model.HasSetter)
			{
				w.Line("return _valueSet ? _value : default!;");
			}
			else
			{
				// Getter-only property: use smart default
				w.Line($"return {model.DefaultExpression};");
			}
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

			// Priority 2: Repeating Set callback
			w.Line("if (_set != null && _setTracking != null)");
			using (w.Braces())
			{
				w.Line("_setTracking.RecordCall(value);");
				w.Line("_set(value);");
				w.Line("return;");
			}
			w.Line();

			// No callback configured - track unconfigured call
			w.Line("_unconfiguredSetCount++;");
			w.Line("_unconfiguredLastSetValue = value;");
			w.Line();

			// Sequence exhausted - check strict mode first (always throws), then repeat-last-value, then do nothing
			w.Line("if (_setSequence != null && _setSequenceIndex >= _setSequence.Count)");
			using (w.Braces())
			{
				// Strict mode ALWAYS throws on exhaustion (regardless of _repeatLastValue)
				w.Line($"if ({options.StrictAccessExpression}) throw global::KnockOff.StubException.SequenceExhausted(\"{model.PropertyName} (set)\");");
				// Repeat last callback if enabled (default behavior in non-strict mode)
				w.Line("if (_setRepeatLastValue && _setSequence.Count > 0)");
				using (w.Braces())
				{
					w.Line("var (callback, tracking) = _setSequence[_setSequence.Count - 1];");
					w.Line("tracking.RecordCall(value);");
					w.Line("callback(value);");
					w.Line("return;");
				}
				// Do nothing (only reached when _repeatLastValue is false via ThenDefault())
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

			// Store in backing field for round-trip storage (when property has both getter and setter)
			if (model.HasGetter)
			{
				w.Line("_value = value;");
				w.Line("_valueSet = true;");
			}
		}
		w.Line();
	}

	/// <summary>
	/// Renders InvokeRefGet for ref return properties.
	/// Same priority chain as InvokeGet but writes to _refReturnBacking instead of returning.
	/// </summary>
	private static void RenderInvokeRefGet(
		CodeWriter w,
		UnifiedPropertyInterceptorModel model,
		PropertyInterceptorRenderOptions options)
	{
		var strictParam = options.IncludeStrictParameter ? "bool strict" : "";

		w.Line($"/// <summary>Invokes the configured getter callback, writing result to _refReturnBacking. Called by ref return interface implementations.</summary>");
		w.Line($"internal void InvokeRefGet({strictParam})");
		using (w.Braces())
		{
			// Priority 1: Sequence (if present and not exhausted)
			w.Line("if (_getSequence != null && _getSequenceIndex < _getSequence.Count)");
			using (w.Braces())
			{
				w.Line("var (callback, tracking) = _getSequence[_getSequenceIndex];");
				w.Line("tracking.RecordCall();");
				w.Line("_getSequenceIndex++;");
				w.Line("_refReturnBacking = callback();");
				w.Line("return;");
			}
			w.Line();

			// Priority 2: Repeating Get callback
			w.Line("if (_get != null && _getTracking != null)");
			using (w.Braces())
			{
				w.Line("_getTracking.RecordCall();");
				w.Line("_refReturnBacking = _get();");
				w.Line("return;");
			}
			w.Line();

			// No callback configured - track unconfigured call
			w.Line("_unconfiguredGetCount++;");
			w.Line();

			// Sequence exhausted - check strict mode first (always throws), then repeat-last-value, then default
			w.Line("if (_getSequence != null && _getSequenceIndex >= _getSequence.Count)");
			using (w.Braces())
			{
				// Strict mode ALWAYS throws on exhaustion (regardless of _repeatLastValue)
				w.Line($"if ({options.StrictAccessExpression}) throw global::KnockOff.StubException.SequenceExhausted(\"{model.PropertyName} (get)\");");
				// Repeat last value if enabled (default behavior in non-strict mode)
				w.Line("if (_getRepeatLastValue && _getSequence.Count > 0)");
				using (w.Braces())
				{
					w.Line("var (callback, tracking) = _getSequence[_getSequence.Count - 1];");
					w.Line("tracking.RecordCall();");
					w.Line("_refReturnBacking = callback();");
					w.Line("return;");
				}
				// Write default to backing (only reached when _repeatLastValue is false via ThenDefault())
				w.Line("_refReturnBacking = default!;");
				w.Line("return;");
			}
			w.Line();

			// Priority 3: Source (if available)
			if (!string.IsNullOrEmpty(model.DeclaringInterface) && !model.IsInitOnly)
			{
				// Source delegation: copy source's value to _refReturnBacking (lossy ref redirection)
				w.Line($"if (_source is {{ }} src) {{ _refReturnBacking = src.{model.PropertyName}; return; }}");
				w.Line();
			}

			// Priority 4: Strict mode check
			w.Line($"if ({options.StrictAccessExpression}) throw global::KnockOff.StubException.NotConfigured(\"\", \"{model.PropertyName}\");");

			// Priority 5: Write default to backing
			w.Line("_refReturnBacking = default!;");
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
		w.Line("/// <summary>Resets tracking state but preserves configuration (Get, Set) and verifiable marking.</summary>");
		w.Line("public void Reset()");
		using (w.Braces())
		{
			if (model.HasGetter)
			{
				w.Line("_unconfiguredGetCount = 0;");
				w.Line("_getTracking?.Reset();");
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
				w.Line("_setTracking?.Reset();");
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
		w.Line("public void Verify() => Verify(global::KnockOff.Called.AtLeastOnce);");
		w.Line();

		w.Line("/// <summary>Verifies total access count satisfies the Called constraint. Throws VerificationException if not.</summary>");
		w.Line("public void Verify(global::KnockOff.Called times)");
		using (w.Braces())
		{
			w.Line("var totalCount = TotalGetCount + _setCount;");
			w.Line("if (!times.Validate(totalCount))");
			w.Line($"\tthrow new global::KnockOff.VerificationException(new global::KnockOff.VerificationFailure(\"{model.PropertyName}\", times, totalCount));");
		}
		w.Line();

		// VerifyGet
		w.Line("/// <summary>Verifies the getter was accessed at least once. Throws VerificationException if not.</summary>");
		w.Line("public void VerifyGet() => VerifyGet(global::KnockOff.Called.AtLeastOnce);");
		w.Line();

		w.Line("/// <summary>Verifies getter access count satisfies the Called constraint. Throws VerificationException if not.</summary>");
		w.Line("public void VerifyGet(global::KnockOff.Called times)");
		using (w.Braces())
		{
			w.Line("if (!times.Validate(TotalGetCount))");
			w.Line($"\tthrow new global::KnockOff.VerificationException(new global::KnockOff.VerificationFailure(\"{model.PropertyName} (get)\", times, TotalGetCount));");
		}
		w.Line();

		// VerifySet
		w.Line("/// <summary>Verifies the init setter was accessed at least once. Throws VerificationException if not.</summary>");
		w.Line("public void VerifySet() => VerifySet(global::KnockOff.Called.AtLeastOnce);");
		w.Line();

		w.Line("/// <summary>Verifies init setter access count satisfies the Called constraint. Throws VerificationException if not.</summary>");
		w.Line("public void VerifySet(global::KnockOff.Called times)");
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
		w.Line($"/// <summary>Marks this property for verification by Stub.Verify() with Called constraint. Returns this for fluent chaining.</summary>");
		w.Line($"public {fullInterceptorClassName} Verifiable(global::KnockOff.Called times) {{ _isGetVerifiable = true; _getVerifiableTimes = times; return this; }}");
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
		w.Line("public void Verify() => Verify(global::KnockOff.Called.AtLeastOnce);");
		w.Line();

		w.Line("/// <summary>Verifies total access count satisfies the Called constraint. Throws VerificationException if not.</summary>");
		w.Line("public void Verify(global::KnockOff.Called times)");
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
			w.Line("public void VerifyGet() => VerifyGet(global::KnockOff.Called.AtLeastOnce);");
			w.Line();

			w.Line("/// <summary>Verifies getter access count satisfies the Called constraint. Throws VerificationException if not.</summary>");
			w.Line("public void VerifyGet(global::KnockOff.Called times)");
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
			w.Line("public void VerifySet() => VerifySet(global::KnockOff.Called.AtLeastOnce);");
			w.Line();

			w.Line("/// <summary>Verifies setter access count satisfies the Called constraint. Throws VerificationException if not.</summary>");
			w.Line("public void VerifySet(global::KnockOff.Called times)");
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
		w.Line($"/// <summary>Marks this property for verification by Stub.Verify() with Called constraint. Returns this for fluent chaining.</summary>");
		w.Line($"public {fullInterceptorClassName} Verifiable(global::KnockOff.Called times) {{ {verifiableTimesBody} return this; }}");
		w.Line();
	}

	#endregion

	#region Internal Verification Support

	private static void RenderInitOnlyInternalVerification(CodeWriter w, UnifiedPropertyInterceptorModel model)
	{
		w.Line("/// <summary>Whether this property was marked with Verifiable().</summary>");
		w.Line("internal bool IsVerifiable => _isGetVerifiable;");
		w.Line();

		w.Line("/// <summary>Whether this property has been configured (Value set or Get configured).</summary>");
		w.Line("internal bool IsConfigured => _valueSet || _get != null || (_getSequence?.Count ?? 0) > 0;");
		w.Line();

		w.Line("/// <summary>Checks verification for Stub.Verify() - only checks if marked verifiable.</summary>");
		w.Line($"internal global::KnockOff.VerificationFailure? CheckVerification()");
		using (w.Braces())
		{
			w.Line("if (!_isGetVerifiable) return null;");
			w.Line("var times = _getVerifiableTimes ?? global::KnockOff.Called.AtLeastOnce;");
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
			w.Line($"return totalCount >= 1 ? null : new global::KnockOff.VerificationFailure(\"{model.PropertyName}\", global::KnockOff.Called.AtLeastOnce, totalCount);");
		}
		w.Line();
	}

	private static void RenderRegularInternalVerification(CodeWriter w, UnifiedPropertyInterceptorModel model)
	{
		var isVerifiableExpr = model.HasGetter && model.HasSetter
			? "_isGetVerifiable || _isSetVerifiable"
			: (model.HasGetter ? "_isGetVerifiable" : "_isSetVerifiable");

		// IsConfigured checks Get/Set - no longer includes _valueSet since .Value API is removed
		var isConfiguredParts = new System.Collections.Generic.List<string>();
		if (model.HasGetter) isConfiguredParts.Add("_get != null || (_getSequence?.Count ?? 0) > 0");
		if (model.HasSetter) isConfiguredParts.Add("_set != null || (_setSequence?.Count ?? 0) > 0");
		var isConfiguredExpr = isConfiguredParts.Count > 0 ? string.Join(" || ", isConfiguredParts) : "false";

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
			// When only one is verifiable (e.g., Get().Verifiable()), check individually.
			if (model.HasGetter && model.HasSetter)
			{
				w.Line("if (_isGetVerifiable && _isSetVerifiable)");
				using (w.Braces())
				{
					w.Line("// Both marked verifiable - check combined count (either accessor satisfies)");
					w.Line("var times = _getVerifiableTimes ?? _setVerifiableTimes ?? global::KnockOff.Called.AtLeastOnce;");
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
					w.Line("var times = _getVerifiableTimes ?? global::KnockOff.Called.AtLeastOnce;");
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
					w.Line("var times = _setVerifiableTimes ?? global::KnockOff.Called.AtLeastOnce;");
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
			w.Line($"return totalCount >= 1 ? null : new global::KnockOff.VerificationFailure(\"{model.PropertyName}\", global::KnockOff.Called.AtLeastOnce, totalCount);");
		}
		w.Line();
	}

	#endregion

	#region Nested PropertyGetBuilderImpl

	private static void RenderPropertyGetBuilderImpl(
		CodeWriter w,
		string valueType,
		string interceptorClassName,
		bool isInitOnly)
	{
		w.Line($"/// <summary>Builder for getter callback registration. Supports tracking and lazy elevation to sequence.</summary>");
		w.Line($"private sealed class PropertyGetBuilderImpl : global::KnockOff.IPropertyGetBuilder<{valueType}>");
		using (w.Braces())
		{
			w.Line($"private readonly {interceptorClassName} _interceptor;");
			w.Line();

			w.Line($"public PropertyGetBuilderImpl({interceptorClassName} interceptor) => _interceptor = interceptor;");
			w.Line();

			w.Line("internal int _callCount;");
			w.Line();

			w.Line("/// <summary>Records a call to this callback.</summary>");
			w.Line("public void RecordCall() => _callCount++;");
			w.Line();

			w.Line("/// <summary>Resets tracking state.</summary>");
			w.Line("public void Reset() => _callCount = 0;");
			w.Line();

			w.Line("/// <summary>Verifies callback was invoked at least once. Throws VerificationException if not.</summary>");
			w.Line("public void Verify() => Verify(global::KnockOff.Called.AtLeastOnce);");
			w.Line();

			w.Line("/// <summary>Verifies call count satisfies the Called constraint. Throws VerificationException if not.</summary>");
			w.Line("public void Verify(global::KnockOff.Called times)");
			using (w.Braces())
			{
				w.Line("if (!times.Validate(_callCount))");
				w.Line("\tthrow new global::KnockOff.VerificationException(new global::KnockOff.VerificationFailure(\"property getter\", times, _callCount));");
			}
			w.Line();

			// ThenGet(callback) - lazy elevation from repeating to sequence mode
			w.Line($"/// <summary>Elevates to sequence mode and adds another getter callback. Returns sequence for further chaining.</summary>");
			w.Line($"public global::KnockOff.IPropertyGetSequence<{valueType}> ThenGet(global::System.Func<{valueType}> callback)");
			using (w.Braces())
			{
				w.Line("if (_interceptor._getSequence == null)");
				using (w.Braces())
				{
					w.Line($"_interceptor._getSequence = new global::System.Collections.Generic.List<(global::System.Func<{valueType}> Callback, PropertyGetBuilderImpl Tracking)>();");
					w.Line("_interceptor._getSequence.Add((_interceptor._get!, this));");
					w.Line("_interceptor._get = null;");
					w.Line("_interceptor._getTracking = null;");  // Clear to prevent double-counting in TotalGetCount
					w.Line("_interceptor._getSequenceIndex = 0;");
				}
				w.Line("var nextBuilder = new PropertyGetBuilderImpl(_interceptor);");
				w.Line("_interceptor._getSequence.Add((callback, nextBuilder));");
				w.Line("return new PropertyGetSequenceImpl(_interceptor);");
			}
			w.Line();

			// ThenGet(value) - wrapper for value-based sequence chaining
			w.Line($"/// <summary>Elevates to sequence mode and adds a value to return. Returns sequence for further chaining.</summary>");
			w.Line($"public global::KnockOff.IPropertyGetSequence<{valueType}> ThenGet({valueType} value) => ThenGet(() => value);");
			w.Line();

			// ThenGet(params values) - adds multiple values to sequence
			w.Line($"/// <summary>Adds multiple values to the sequence. Each value returned once.</summary>");
			w.Line($"public global::KnockOff.IPropertyGetSequence<{valueType}> ThenGet(params {valueType}[] values)");
			using (w.Braces())
			{
				w.Line("if (values.Length == 0)");
				using (w.Braces())
				{
					// Elevate to sequence mode without adding any new values (same as ThenGet elevation)
					w.Line("if (_interceptor._getSequence == null)");
					using (w.Braces())
					{
						w.Line($"_interceptor._getSequence = new global::System.Collections.Generic.List<(global::System.Func<{valueType}> Callback, PropertyGetBuilderImpl Tracking)>();");
						w.Line("_interceptor._getSequence.Add((_interceptor._get!, this));");
						w.Line("_interceptor._get = null;");
						w.Line("_interceptor._getTracking = null;");
						w.Line("_interceptor._getSequenceIndex = 0;");
					}
					w.Line("return new PropertyGetSequenceImpl(_interceptor);");
				}
				w.Line("var seq = ThenGet(values[0]);");
				w.Line("for (int i = 1; i < values.Length; i++)");
				using (w.Braces())
				{
					w.Line("seq = seq.ThenGet(values[i]);");
				}
				w.Line("return seq;");
			}
			w.Line();

			w.Line("/// <summary>Marks for verification by Stub.Verify(). Returns this for fluent chaining.</summary>");
			w.Line($"public global::KnockOff.IPropertyGetBuilder<{valueType}> Verifiable()");
			using (w.Braces())
			{
				w.Line("_interceptor._isGetVerifiable = true;");
				w.Line("_interceptor._getVerifiableTimes = null;");
				w.Line("return this;");
			}
			w.Line();

			// Explicit interface implementation for base IPropertyGetTracking.Verifiable()
			w.Line("global::KnockOff.IPropertyGetTracking global::KnockOff.IPropertyGetTracking.Verifiable() => Verifiable();");
			w.Line("global::KnockOff.IPropertyGetTracking global::KnockOff.IPropertyGetTracking.Verifiable(global::KnockOff.Called times) => Verifiable();");
		}
		w.Line();
	}

	#endregion

	#region Nested PropertySetBuilderImpl

	private static void RenderPropertySetBuilderImpl(
		CodeWriter w,
		string valueType,
		string interceptorClassName)
	{
		w.Line($"/// <summary>Builder for setter callback registration. Supports tracking and lazy elevation to sequence.</summary>");
		w.Line($"private sealed class PropertySetBuilderImpl : global::KnockOff.IPropertySetBuilder<{valueType}>");
		using (w.Braces())
		{
			w.Line($"private readonly {interceptorClassName} _interceptor;");
			w.Line();

			w.Line($"public PropertySetBuilderImpl({interceptorClassName} interceptor) => _interceptor = interceptor;");
			w.Line();

			w.Line($"private {valueType} _lastValue = default!;");
			w.Line();

			w.Line("internal int _callCount;");
			w.Line();

			w.Line($"/// <summary>Last value passed to this setter callback. Default if never called.</summary>");
			w.Line($"public {valueType} LastValue => _lastValue;");
			w.Line();

			w.Line("/// <summary>Records a call to this callback.</summary>");
			w.Line($"public void RecordCall({valueType} value) {{ _callCount++; _lastValue = value; }}");
			w.Line();

			w.Line("/// <summary>Resets tracking state.</summary>");
			w.Line("public void Reset() { _callCount = 0; _lastValue = default!; }");
			w.Line();

			w.Line("/// <summary>Verifies callback was invoked at least once. Throws VerificationException if not.</summary>");
			w.Line("public void Verify() => Verify(global::KnockOff.Called.AtLeastOnce);");
			w.Line();

			w.Line("/// <summary>Verifies call count satisfies the Called constraint. Throws VerificationException if not.</summary>");
			w.Line("public void Verify(global::KnockOff.Called times)");
			using (w.Braces())
			{
				w.Line("if (!times.Validate(_callCount))");
				w.Line("\tthrow new global::KnockOff.VerificationException(new global::KnockOff.VerificationFailure(\"property setter\", times, _callCount));");
			}
			w.Line();

			// ThenSet(callback) - lazy elevation from repeating to sequence mode
			w.Line($"/// <summary>Elevates to sequence mode and adds another setter callback. Returns sequence for further chaining.</summary>");
			w.Line($"public global::KnockOff.IPropertySetSequence<{valueType}> ThenSet(global::System.Action<{valueType}> callback)");
			using (w.Braces())
			{
				w.Line("if (_interceptor._setSequence == null)");
				using (w.Braces())
				{
					w.Line($"_interceptor._setSequence = new global::System.Collections.Generic.List<(global::System.Action<{valueType}> Callback, PropertySetBuilderImpl Tracking)>();");
					w.Line("_interceptor._setSequence.Add((_interceptor._set!, this));");
					w.Line("_interceptor._set = null;");
					w.Line("_interceptor._setTracking = null;");  // Clear to prevent double-counting in TotalSetCount
					w.Line("_interceptor._setSequenceIndex = 0;");
				}
				w.Line("var nextBuilder = new PropertySetBuilderImpl(_interceptor);");
				w.Line("_interceptor._setSequence.Add((callback, nextBuilder));");
				w.Line("return new PropertySetSequenceImpl(_interceptor);");
			}
			w.Line();

			w.Line("/// <summary>Marks for verification by Stub.Verify(). Returns this for fluent chaining.</summary>");
			w.Line($"public global::KnockOff.IPropertySetBuilder<{valueType}> Verifiable()");
			using (w.Braces())
			{
				w.Line("_interceptor._isSetVerifiable = true;");
				w.Line("_interceptor._setVerifiableTimes = null;");
				w.Line("return this;");
			}
			w.Line();

			// Explicit interface implementation for base IPropertySetTracking<T>.Verifiable()
			w.Line($"global::KnockOff.IPropertySetTracking<{valueType}> global::KnockOff.IPropertySetTracking<{valueType}>.Verifiable() => Verifiable();");
			w.Line($"global::KnockOff.IPropertySetTracking<{valueType}> global::KnockOff.IPropertySetTracking<{valueType}>.Verifiable(global::KnockOff.Called times) => Verifiable();");
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
				w.Line("var tracking = new PropertyGetBuilderImpl(_interceptor);");
				w.Line("_interceptor._getSequence!.Add((callback, tracking));");
				w.Line("return this;");
			}
			w.Line();

			// ThenGet(value) - wrapper method for value-based sequence chaining
			w.Line($"/// <summary>Adds a value to the sequence. The value is returned exactly once.</summary>");
			w.Line($"public global::KnockOff.IPropertyGetSequence<{valueType}> ThenGet({valueType} value) => ThenGet(() => value);");
			w.Line();

			// ThenGet(params values) - adds multiple values to sequence
			w.Line($"/// <summary>Adds multiple values to the sequence. Each value returned once.</summary>");
			w.Line($"public global::KnockOff.IPropertyGetSequence<{valueType}> ThenGet(params {valueType}[] values)");
			using (w.Braces())
			{
				w.Line("foreach (var value in values)");
				using (w.Braces())
				{
					w.Line("ThenGet(value);");
				}
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
			w.Line();

			// ThenDefault() - terminates sequence with default(T) after exhaustion
			w.Line("/// <summary>Terminates sequence with default(T) after exhaustion instead of repeating last value.</summary>");
			w.Line("public void ThenDefault()");
			using (w.Braces())
			{
				w.Line("_interceptor._getRepeatLastValue = false;");
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
				w.Line("var tracking = new PropertySetBuilderImpl(_interceptor);");
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
			w.Line();

			// ThenDefault() - terminates sequence (do nothing) after exhaustion
			w.Line("/// <summary>Terminates sequence after exhaustion instead of repeating last callback.</summary>");
			w.Line("public void ThenDefault()");
			using (w.Braces())
			{
				w.Line("_interceptor._setRepeatLastValue = false;");
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
