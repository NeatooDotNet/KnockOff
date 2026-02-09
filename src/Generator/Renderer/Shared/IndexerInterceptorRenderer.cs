// src/Generator/Renderer/Shared/IndexerInterceptorRenderer.cs
#nullable enable
using KnockOff.Model.Shared;

namespace KnockOff.Renderer.Shared;

/// <summary>
/// Renders indexer interceptor classes for both inline and flat stubs.
/// Generates Get() returning IIndexerGetBuilder (repeating callback, elevatable to sequence via ThenGet),
/// Set() returning IIndexerSetBuilder similarly for setters,
/// nested builder and sequence implementation classes, InvokeGet/InvokeSet methods,
/// Backing dictionary, and verification.
/// </summary>
internal static class IndexerInterceptorRenderer
{
	/// <summary>
	/// Renders a complete indexer interceptor class.
	/// </summary>
	public static void RenderInterceptorClass(
		CodeWriter w,
		UnifiedIndexerInterceptorModel model,
		IndexerInterceptorRenderOptions options)
	{
		var typeParams = options.InterceptorTypeParameters;
		var constraints = options.InterceptorConstraints;
		var classDecl = $"public sealed class {model.InterceptorClassName}{typeParams}{constraints}";
		var fullInterceptorClassName = model.InterceptorClassName + typeParams;

		w.Line($"/// <summary>Tracks and configures behavior for indexer.</summary>");
		using (w.Block(classDecl))
		{
			// Source field for Source(T) feature
			if (!string.IsNullOrEmpty(model.DeclaringInterface))
			{
				w.Line($"/// <summary>Source object to delegate to when no Get/Set is configured.</summary>");
				w.Line($"internal {model.DeclaringInterface}? _source;");
				w.Line();
			}

			// Backing dictionary
			w.Line($"/// <summary>Backing storage for this indexer.</summary>");
			w.Line($"public global::System.Collections.Generic.Dictionary<{model.SingleKeyType}, {model.ValueType}> Backing {{ get; }} = new();");
			w.Line();

			// Getter storage and tracking (if has getter)
			if (model.HasGetter)
			{
				w.Line($"private global::System.Func<{model.KeyType}, {model.ValueType}>? _get;");
				w.Line("private IndexerGetBuilderImpl? _getTracking;");
				w.Line($"private global::System.Collections.Generic.List<(global::System.Func<{model.KeyType}, {model.ValueType}> Callback, IndexerGetBuilderImpl Tracking)>? _getSequence;");
				w.Line("private int _getSequenceIndex;");
				w.Line("private bool _getRepeatLastValue = true;");
				w.Line("private bool _isGetVerifiable;");
				w.Line("private global::KnockOff.Called? _getVerifiableTimes;");
				w.Line("private int _unconfiguredGetCount;");
				w.Line($"private {model.NullableKeyType} _unconfiguredLastGetKey;");
				w.Line();
			}

			// Setter storage and tracking (if has setter)
			if (model.HasSetter)
			{
				w.Line($"private global::System.Action<{model.KeyType}, {model.ValueType}>? _set;");
				w.Line("private IndexerSetBuilderImpl? _setTracking;");
				w.Line($"private global::System.Collections.Generic.List<(global::System.Action<{model.KeyType}, {model.ValueType}> Callback, IndexerSetBuilderImpl Tracking)>? _setSequence;");
				w.Line("private int _setSequenceIndex;");
				w.Line("private bool _setRepeatLastValue = true;");
				w.Line("private bool _isSetVerifiable;");
				w.Line("private global::KnockOff.Called? _setVerifiableTimes;");
				w.Line("private int _unconfiguredSetCount;");
				w.Line($"private ({model.KeyType} Key, {model.ValueType} Value)? _unconfiguredLastSetEntry;");
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

			// LastGetKey for backward compatibility (if has getter)
			if (model.HasGetter)
			{
				w.Line($"/// <summary>The key from the last getter access (from most recently called registration).</summary>");
				w.Line($"public {model.NullableKeyType} LastGetKey {{ get {{ if ((_getTracking?._callCount ?? 0) > 0) return _getTracking!.LastKey; if (_getSequence != null) for (int i = _getSequence.Count - 1; i >= 0; i--) if (_getSequence[i].Tracking._callCount > 0) return _getSequence[i].Tracking.LastKey; return _unconfiguredGetCount > 0 ? _unconfiguredLastGetKey : default; }} }}");
				w.Line();
			}

			// LastSetEntry for backward compatibility (if has setter)
			if (model.HasSetter)
			{
				w.Line($"/// <summary>The key-value pair from the last setter access (from most recently called registration).</summary>");
				w.Line($"public ({model.KeyType} Key, {model.ValueType} Value)? LastSetEntry {{ get {{ if ((_setTracking?._callCount ?? 0) > 0) return _setTracking!.LastEntry; if (_setSequence != null) for (int i = _setSequence.Count - 1; i >= 0; i--) if (_setSequence[i].Tracking._callCount > 0) return _setSequence[i].Tracking.LastEntry; return _unconfiguredSetCount > 0 ? _unconfiguredLastSetEntry : default; }} }}");
				w.Line();
			}

			// Get() method (if has getter)
			if (model.HasGetter)
			{
				w.Line($"/// <summary>Configures getter callback that repeats indefinitely. Returns builder for tracking and sequence chaining.</summary>");
				w.Line($"public global::KnockOff.IIndexerGetBuilder<{model.KeyType}, {model.ValueType}> Get(global::System.Func<{model.KeyType}, {model.ValueType}> callback)");
				using (w.Braces())
				{
					w.Line("_getSequence = null;");
					w.Line("_getSequenceIndex = 0;");
					w.Line("_isGetVerifiable = false;");
					w.Line("_getVerifiableTimes = null;");
					w.Line("_get = callback;");
					w.Line("_getTracking = new IndexerGetBuilderImpl(this);");
					w.Line("return _getTracking;");
				}
				w.Line();

			}

			// Set() method (if has setter)
			if (model.HasSetter)
			{
				w.Line($"/// <summary>Configures setter callback that repeats indefinitely. Returns builder for tracking and sequence chaining.</summary>");
				w.Line($"public global::KnockOff.IIndexerSetBuilder<{model.KeyType}, {model.ValueType}> Set(global::System.Action<{model.KeyType}, {model.ValueType}> callback)");
				using (w.Braces())
				{
					w.Line("_setSequence = null;");
					w.Line("_setSequenceIndex = 0;");
					w.Line("_isSetVerifiable = false;");
					w.Line("_setVerifiableTimes = null;");
					w.Line("_set = callback;");
					w.Line("_setTracking = new IndexerSetBuilderImpl(this);");
					w.Line("return _setTracking;");
				}
				w.Line();

			}

			// Ref return backing field (for ref return indexers)
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
				// InvokeRefGet for ref return indexers - writes to _refReturnBacking instead of returning
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
			RenderVerificationMethods(w, model, fullInterceptorClassName);

			// Internal verification support
			RenderInternalVerification(w, model);

			// Nested classes
			if (model.HasGetter)
			{
				RenderIndexerGetBuilderImpl(w, model.KeyType, model.ValueType, model.KeyType, fullInterceptorClassName);
				RenderIndexerGetSequenceImpl(w, model.KeyType, model.ValueType, model.KeyType, fullInterceptorClassName);
			}
			if (model.HasSetter)
			{
				RenderIndexerSetBuilderImpl(w, model.KeyType, model.ValueType, model.KeyType, fullInterceptorClassName);
				RenderIndexerSetSequenceImpl(w, model.KeyType, model.ValueType, model.KeyType, fullInterceptorClassName);
			}
		}
		w.Line();
	}

	#region InvokeGet / InvokeSet Methods

	private static void RenderInvokeGet(
		CodeWriter w,
		UnifiedIndexerInterceptorModel model,
		IndexerInterceptorRenderOptions options)
	{
		var strictParam = options.IncludeStrictParameter ? "bool strict, " : "";

		w.Line($"/// <summary>Invokes the configured getter callback. Called by explicit interface implementation.</summary>");
		w.Line($"internal {model.ValueType} InvokeGet({strictParam}{model.ParameterSignature})");
		using (w.Braces())
		{
			// Priority 1: Sequence (if present and not exhausted)
			w.Line("if (_getSequence != null && _getSequenceIndex < _getSequence.Count)");
			using (w.Braces())
			{
				w.Line("var (callback, tracking) = _getSequence[_getSequenceIndex];");
				w.Line($"tracking.RecordCall({model.KeyExpression});");
				w.Line("_getSequenceIndex++;");
				w.Line($"return callback({model.KeyExpression});");
			}
			w.Line();

			// Sequence exhausted - check strict mode first (always throws), then repeat-last-value, then default
			w.Line("if (_getSequence != null && _getSequenceIndex >= _getSequence.Count)");
			using (w.Braces())
			{
				// Strict mode ALWAYS throws on exhaustion (regardless of _repeatLastValue)
				w.Line($"if ({options.StrictAccessExpression}) throw global::KnockOff.StubException.SequenceExhausted(\"{model.IndexerName} (get)\");");
				// Repeat last value if enabled (default behavior in non-strict mode)
				w.Line("if (_getRepeatLastValue && _getSequence.Count > 0)");
				using (w.Braces())
				{
					w.Line("var (callback, tracking) = _getSequence[_getSequence.Count - 1];");
					w.Line($"tracking.RecordCall({model.KeyExpression});");
					w.Line($"return callback({model.KeyExpression});");
				}
			}
			w.Line();

			// Priority 2: Repeating Get callback
			w.Line("if (_get != null && _getTracking != null)");
			using (w.Braces())
			{
				w.Line($"_getTracking.RecordCall({model.KeyExpression});");
				w.Line($"return _get({model.KeyExpression});");
			}
			w.Line();

			// No callback configured - track unconfigured call
			w.Line("_unconfiguredGetCount++;");
			w.Line($"_unconfiguredLastGetKey = {model.KeyExpression};");
			w.Line();

			// Priority 3: Backing dictionary
			w.Line($"if (Backing.TryGetValue({model.KeyExpression}, out var backingValue)) return backingValue;");
			w.Line();

			// Priority 4: Source (if available)
			if (!string.IsNullOrEmpty(model.DeclaringInterface))
			{
				w.Line($"if (_source is {{ }} src) return src[{model.ArgumentList}];");
				w.Line();
			}

			// Priority 5: Strict mode check
			w.Line($"if ({options.StrictAccessExpression}) throw global::KnockOff.StubException.NotConfigured(\"\", \"{model.IndexerName}\");");

			// Priority 6: Default
			w.Line("return default!;");
		}
		w.Line();
	}

	private static void RenderInvokeSet(
		CodeWriter w,
		UnifiedIndexerInterceptorModel model,
		IndexerInterceptorRenderOptions options)
	{
		var strictParam = options.IncludeStrictParameter ? "bool strict, " : "";

		w.Line($"/// <summary>Invokes the configured setter callback. Called by explicit interface implementation.</summary>");
		w.Line($"internal void InvokeSet({strictParam}{model.ParameterSignature}, {model.ValueType} value)");
		using (w.Braces())
		{
			// Priority 1: Sequence (if present and not exhausted)
			w.Line("if (_setSequence != null && _setSequenceIndex < _setSequence.Count)");
			using (w.Braces())
			{
				w.Line("var (callback, tracking) = _setSequence[_setSequenceIndex];");
				w.Line($"tracking.RecordCall({model.KeyExpression}, value);");
				w.Line("_setSequenceIndex++;");
				w.Line($"callback({model.KeyExpression}, value);");
				w.Line("return;");
			}
			w.Line();

			// Sequence exhausted - check strict mode first (always throws), then repeat-last-value, then do nothing
			w.Line("if (_setSequence != null && _setSequenceIndex >= _setSequence.Count)");
			using (w.Braces())
			{
				// Strict mode ALWAYS throws on exhaustion (regardless of _repeatLastValue)
				w.Line($"if ({options.StrictAccessExpression}) throw global::KnockOff.StubException.SequenceExhausted(\"{model.IndexerName} (set)\");");
				// Repeat last value if enabled (default behavior in non-strict mode)
				w.Line("if (_setRepeatLastValue && _setSequence.Count > 0)");
				using (w.Braces())
				{
					w.Line("var (callback, tracking) = _setSequence[_setSequence.Count - 1];");
					w.Line($"tracking.RecordCall({model.KeyExpression}, value);");
					w.Line($"callback({model.KeyExpression}, value);");
					w.Line("return;");
				}
				// Store in Backing (only reached when _repeatLastValue is false via ThenDefault())
				w.Line($"Backing[{model.KeyExpression}] = value;");
				w.Line("return;");
			}
			w.Line();

			// Priority 2: Repeating Set callback
			w.Line("if (_set != null && _setTracking != null)");
			using (w.Braces())
			{
				w.Line($"_setTracking.RecordCall({model.KeyExpression}, value);");
				w.Line($"_set({model.KeyExpression}, value);");
				w.Line("return;");
			}
			w.Line();

			// No callback configured - track unconfigured call
			w.Line("_unconfiguredSetCount++;");
			w.Line($"_unconfiguredLastSetEntry = ({model.KeyExpression}, value);");
			w.Line();

			// Priority 3: Source (if available) - skip for init-only indexers (can't assign outside init context)
			if (!string.IsNullOrEmpty(model.DeclaringInterface) && !model.IsInitOnly)
			{
				w.Line($"if (_source is {{ }} src) {{ src[{model.ArgumentList}] = value; return; }}");
				w.Line();
			}

			// Priority 4: Strict mode check
			w.Line($"if ({options.StrictAccessExpression}) throw global::KnockOff.StubException.NotConfigured(\"\", \"{model.IndexerName}\");");

			// Priority 5: Update Backing
			w.Line($"Backing[{model.KeyExpression}] = value;");
		}
		w.Line();
	}

	/// <summary>
	/// Renders InvokeRefGet for ref return indexers.
	/// Same priority chain as InvokeGet but writes to _refReturnBacking instead of returning.
	/// </summary>
	private static void RenderInvokeRefGet(
		CodeWriter w,
		UnifiedIndexerInterceptorModel model,
		IndexerInterceptorRenderOptions options)
	{
		var strictParam = options.IncludeStrictParameter ? "bool strict, " : "";

		w.Line($"/// <summary>Invokes the configured getter callback, writing result to _refReturnBacking. Called by ref return interface implementations.</summary>");
		w.Line($"internal void InvokeRefGet({strictParam}{model.ParameterSignature})");
		using (w.Braces())
		{
			// Priority 1: Sequence (if present and not exhausted)
			w.Line("if (_getSequence != null && _getSequenceIndex < _getSequence.Count)");
			using (w.Braces())
			{
				w.Line("var (callback, tracking) = _getSequence[_getSequenceIndex];");
				w.Line($"tracking.RecordCall({model.KeyExpression});");
				w.Line("_getSequenceIndex++;");
				w.Line($"_refReturnBacking = callback({model.KeyExpression});");
				w.Line("return;");
			}
			w.Line();

			// Sequence exhausted - check strict mode first (always throws), then repeat-last-value, then default
			w.Line("if (_getSequence != null && _getSequenceIndex >= _getSequence.Count)");
			using (w.Braces())
			{
				// Strict mode ALWAYS throws on exhaustion (regardless of _repeatLastValue)
				w.Line($"if ({options.StrictAccessExpression}) throw global::KnockOff.StubException.SequenceExhausted(\"{model.IndexerName} (get)\");");
				// Repeat last value if enabled (default behavior in non-strict mode)
				w.Line("if (_getRepeatLastValue && _getSequence.Count > 0)");
				using (w.Braces())
				{
					w.Line("var (callback, tracking) = _getSequence[_getSequence.Count - 1];");
					w.Line($"tracking.RecordCall({model.KeyExpression});");
					w.Line($"_refReturnBacking = callback({model.KeyExpression});");
					w.Line("return;");
				}
			}
			w.Line();

			// Priority 2: Repeating Get callback
			w.Line("if (_get != null && _getTracking != null)");
			using (w.Braces())
			{
				w.Line($"_getTracking.RecordCall({model.KeyExpression});");
				w.Line($"_refReturnBacking = _get({model.KeyExpression});");
				w.Line("return;");
			}
			w.Line();

			// No callback configured - track unconfigured call
			w.Line("_unconfiguredGetCount++;");
			w.Line($"_unconfiguredLastGetKey = {model.KeyExpression};");
			w.Line();

			// Priority 3: Backing dictionary
			w.Line($"if (Backing.TryGetValue({model.KeyExpression}, out var backingValue)) {{ _refReturnBacking = backingValue; return; }}");
			w.Line();

			// Priority 4: Source (if available)
			if (!string.IsNullOrEmpty(model.DeclaringInterface))
			{
				// Source delegation: copy source's value to _refReturnBacking (lossy ref redirection)
				w.Line($"if (_source is {{ }} src) {{ _refReturnBacking = src[{model.ArgumentList}]; return; }}");
				w.Line();
			}

			// Priority 5: Strict mode check
			w.Line($"if ({options.StrictAccessExpression}) throw global::KnockOff.StubException.NotConfigured(\"\", \"{model.IndexerName}\");");

			// Priority 6: Default
			w.Line("_refReturnBacking = default!;");
		}
		w.Line();
	}

	#endregion

	#region Reset Method

	private static void RenderResetMethod(
		CodeWriter w,
		UnifiedIndexerInterceptorModel model,
		bool hasSourceField)
	{
		w.Line("/// <summary>Resets tracking state but preserves configuration (Get, Set, Backing) and verifiable marking.</summary>");
		w.Line("public void Reset()");
		using (w.Braces())
		{
			if (model.HasGetter)
			{
				w.Line("_unconfiguredGetCount = 0;");
				w.Line("_unconfiguredLastGetKey = default;");
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
				w.Line("_unconfiguredLastSetEntry = default;");
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
			// Note: Backing dictionary is intentionally NOT cleared - pre-populated data is preserved
		}
		w.Line();
	}

	#endregion

	#region Verification Methods

	private static void RenderVerificationMethods(CodeWriter w, UnifiedIndexerInterceptorModel model, string fullInterceptorClassName)
	{
		// Build total count expression based on available accessors
		var totalCountExpr = model.HasGetter && model.HasSetter
			? "TotalGetCount + TotalSetCount"
			: (model.HasGetter ? "TotalGetCount" : "TotalSetCount");

		// Verify() - combined
		w.Line("/// <summary>Verifies the indexer was accessed at least once. Throws VerificationException if not.</summary>");
		w.Line("public void Verify() => Verify(global::KnockOff.Called.AtLeastOnce);");
		w.Line();

		w.Line("/// <summary>Verifies total access count satisfies the Called constraint. Throws VerificationException if not.</summary>");
		w.Line("public void Verify(global::KnockOff.Called times)");
		using (w.Braces())
		{
			w.Line($"var totalCount = {totalCountExpr};");
			w.Line("if (!times.Validate(totalCount))");
			w.Line($"\tthrow new global::KnockOff.VerificationException(new global::KnockOff.VerificationFailure(\"{model.IndexerName}\", times, totalCount));");
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
				w.Line($"\tthrow new global::KnockOff.VerificationException(new global::KnockOff.VerificationFailure(\"{model.IndexerName} (get)\", times, TotalGetCount));");
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
				w.Line($"\tthrow new global::KnockOff.VerificationException(new global::KnockOff.VerificationFailure(\"{model.IndexerName} (set)\", times, TotalSetCount));");
			}
			w.Line();
		}

		// Verifiable fluent methods
		w.Line($"/// <summary>Marks this indexer for verification by Stub.Verify(). Returns this for fluent chaining.</summary>");
		var verifiableBody = model.HasGetter && model.HasSetter
			? "_isGetVerifiable = true; _getVerifiableTimes = null; _isSetVerifiable = true; _setVerifiableTimes = null;"
			: (model.HasGetter ? "_isGetVerifiable = true; _getVerifiableTimes = null;" : "_isSetVerifiable = true; _setVerifiableTimes = null;");
		w.Line($"public {fullInterceptorClassName} Verifiable() {{ {verifiableBody} return this; }}");
		w.Line();
		var verifiableTimesBody = model.HasGetter && model.HasSetter
			? "_isGetVerifiable = true; _getVerifiableTimes = times; _isSetVerifiable = true; _setVerifiableTimes = times;"
			: (model.HasGetter ? "_isGetVerifiable = true; _getVerifiableTimes = times;" : "_isSetVerifiable = true; _setVerifiableTimes = times;");
		w.Line($"/// <summary>Marks this indexer for verification by Stub.Verify() with Called constraint. Returns this for fluent chaining.</summary>");
		w.Line($"public {fullInterceptorClassName} Verifiable(global::KnockOff.Called times) {{ {verifiableTimesBody} return this; }}");
		w.Line();
	}

	#endregion

	#region Internal Verification Support

	private static void RenderInternalVerification(CodeWriter w, UnifiedIndexerInterceptorModel model)
	{
		var isVerifiableExpr = model.HasGetter && model.HasSetter
			? "_isGetVerifiable || _isSetVerifiable"
			: (model.HasGetter ? "_isGetVerifiable" : "_isSetVerifiable");

		var isConfiguredParts = new System.Collections.Generic.List<string> { "Backing.Count > 0" };
		if (model.HasGetter) isConfiguredParts.Add("_get != null || (_getSequence?.Count ?? 0) > 0");
		if (model.HasSetter) isConfiguredParts.Add("_set != null || (_setSequence?.Count ?? 0) > 0");
		var isConfiguredExpr = string.Join(" || ", isConfiguredParts);

		var totalCountExpr = model.HasGetter && model.HasSetter
			? "TotalGetCount + TotalSetCount"
			: (model.HasGetter ? "TotalGetCount" : "TotalSetCount");

		w.Line("/// <summary>Whether this indexer was marked with Verifiable().</summary>");
		w.Line($"internal bool IsVerifiable => {isVerifiableExpr};");
		w.Line();

		w.Line("/// <summary>Whether this indexer has been configured.</summary>");
		w.Line($"internal bool IsConfigured => {isConfiguredExpr};");
		w.Line();

		w.Line("/// <summary>Checks verification for Stub.Verify() - only checks if marked verifiable.</summary>");
		w.Line($"internal global::KnockOff.VerificationFailure? CheckVerification()");
		using (w.Braces())
		{
			w.Line($"if (!({isVerifiableExpr})) return null;");

			// When BOTH get and set are verifiable (e.g., Verifiable() called on interceptor),
			// check combined count - "indexer was used" means either get or set.
			// When only one is verifiable (e.g., Get().Verifiable()), check individually.
			if (model.HasGetter && model.HasSetter)
			{
				w.Line("if (_isGetVerifiable && _isSetVerifiable)");
				using (w.Braces())
				{
					w.Line("// Both marked verifiable - check combined count (either accessor satisfies)");
					w.Line("var times = _getVerifiableTimes ?? _setVerifiableTimes ?? global::KnockOff.Called.AtLeastOnce;");
					w.Line($"var totalCount = {totalCountExpr};");
					w.Line($"return times.Validate(totalCount) ? null : new global::KnockOff.VerificationFailure(\"{model.IndexerName}\", times, totalCount);");
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
					w.Line($"if (!times.Validate(TotalGetCount)) return new global::KnockOff.VerificationFailure(\"{model.IndexerName} (get)\", times, TotalGetCount);");
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
					w.Line($"if (!times.Validate(TotalSetCount)) return new global::KnockOff.VerificationFailure(\"{model.IndexerName} (set)\", times, TotalSetCount);");
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
			w.Line($"return totalCount >= 1 ? null : new global::KnockOff.VerificationFailure(\"{model.IndexerName}\", global::KnockOff.Called.AtLeastOnce, totalCount);");
		}
		w.Line();
	}

	#endregion

	#region Nested IndexerGetBuilderImpl

	private static void RenderIndexerGetBuilderImpl(
		CodeWriter w,
		string keyType,
		string valueType,
		string parameterTypes,
		string interceptorClassName)
	{
		w.Line($"/// <summary>Builder for getter callback registration. Supports tracking and lazy elevation to sequence.</summary>");
		w.Line($"private sealed class IndexerGetBuilderImpl : global::KnockOff.IIndexerGetBuilder<{keyType}, {valueType}>");
		using (w.Braces())
		{
			w.Line($"private readonly {interceptorClassName} _interceptor;");
			w.Line();

			w.Line($"public IndexerGetBuilderImpl({interceptorClassName} interceptor) => _interceptor = interceptor;");
			w.Line();

			w.Line($"private {keyType} _lastKey = default!;");
			w.Line();

			w.Line("internal int _callCount;");
			w.Line();

			w.Line($"/// <summary>Last key passed to this getter callback. Default if never called.</summary>");
			w.Line($"public {keyType} LastKey => _lastKey;");
			w.Line();

			w.Line("/// <summary>Records a call to this callback.</summary>");
			w.Line($"public void RecordCall({keyType} key) {{ _callCount++; _lastKey = key; }}");
			w.Line();

			w.Line("/// <summary>Resets tracking state.</summary>");
			w.Line("public void Reset() { _callCount = 0; _lastKey = default!; }");
			w.Line();

			w.Line("/// <summary>Verifies callback was invoked at least once. Throws VerificationException if not.</summary>");
			w.Line("public void Verify() => Verify(global::KnockOff.Called.AtLeastOnce);");
			w.Line();

			w.Line("/// <summary>Verifies call count satisfies the Called constraint. Throws VerificationException if not.</summary>");
			w.Line("public void Verify(global::KnockOff.Called times)");
			using (w.Braces())
			{
				w.Line("if (!times.Validate(_callCount))");
				w.Line("\tthrow new global::KnockOff.VerificationException(new global::KnockOff.VerificationFailure(\"indexer getter\", times, _callCount));");
			}
			w.Line();

			// ThenGet(callback) - lazy elevation from repeating to sequence mode
			w.Line($"/// <summary>Elevates to sequence mode and adds another getter callback. Returns sequence for further chaining.</summary>");
			w.Line($"public global::KnockOff.IIndexerGetSequence<{keyType}, {valueType}> ThenGet(global::System.Func<{parameterTypes}, {valueType}> callback)");
			using (w.Braces())
			{
				w.Line("if (_interceptor._getSequence == null)");
				using (w.Braces())
				{
					w.Line($"_interceptor._getSequence = new global::System.Collections.Generic.List<(global::System.Func<{parameterTypes}, {valueType}> Callback, IndexerGetBuilderImpl Tracking)>();");
					w.Line("_interceptor._getSequence.Add((_interceptor._get!, this));");
					w.Line("_interceptor._get = null;");
					w.Line("_interceptor._getTracking = null;");  // Clear to prevent double-counting in TotalGetCount
					w.Line("_interceptor._getSequenceIndex = 0;");
				}
				w.Line("var nextBuilder = new IndexerGetBuilderImpl(_interceptor);");
				w.Line("_interceptor._getSequence.Add((callback, nextBuilder));");
				w.Line("return new IndexerGetSequenceImpl(_interceptor);");
			}
			w.Line();

			w.Line("/// <summary>Marks for verification by Stub.Verify(). Returns this for fluent chaining.</summary>");
			w.Line($"public global::KnockOff.IIndexerGetBuilder<{keyType}, {valueType}> Verifiable()");
			using (w.Braces())
			{
				w.Line("_interceptor._isGetVerifiable = true;");
				w.Line("_interceptor._getVerifiableTimes = null;");
				w.Line("return this;");
			}
			w.Line();

			// Explicit interface implementation for base IIndexerGetTracking<TKey>.Verifiable()
			w.Line($"global::KnockOff.IIndexerGetTracking<{keyType}> global::KnockOff.IIndexerGetTracking<{keyType}>.Verifiable() => Verifiable();");
			w.Line($"global::KnockOff.IIndexerGetTracking<{keyType}> global::KnockOff.IIndexerGetTracking<{keyType}>.Verifiable(global::KnockOff.Called times) => Verifiable();");
		}
		w.Line();
	}

	#endregion

	#region Nested IndexerSetBuilderImpl

	private static void RenderIndexerSetBuilderImpl(
		CodeWriter w,
		string keyType,
		string valueType,
		string parameterTypes,
		string interceptorClassName)
	{
		w.Line($"/// <summary>Builder for setter callback registration. Supports tracking and lazy elevation to sequence.</summary>");
		w.Line($"private sealed class IndexerSetBuilderImpl : global::KnockOff.IIndexerSetBuilder<{keyType}, {valueType}>");
		using (w.Braces())
		{
			w.Line($"private readonly {interceptorClassName} _interceptor;");
			w.Line();

			w.Line($"public IndexerSetBuilderImpl({interceptorClassName} interceptor) => _interceptor = interceptor;");
			w.Line();

			w.Line($"private ({keyType} Key, {valueType} Value)? _lastEntry;");
			w.Line();

			w.Line("internal int _callCount;");
			w.Line();

			w.Line($"/// <summary>Last key and value passed to this setter callback. Null if never called.</summary>");
			w.Line($"public ({keyType} Key, {valueType} Value)? LastEntry => _lastEntry;");
			w.Line();

			w.Line("/// <summary>Records a call to this callback.</summary>");
			w.Line($"public void RecordCall({keyType} key, {valueType} value) {{ _callCount++; _lastEntry = (key, value); }}");
			w.Line();

			w.Line("/// <summary>Resets tracking state.</summary>");
			w.Line("public void Reset() { _callCount = 0; _lastEntry = null; }");
			w.Line();

			w.Line("/// <summary>Verifies callback was invoked at least once. Throws VerificationException if not.</summary>");
			w.Line("public void Verify() => Verify(global::KnockOff.Called.AtLeastOnce);");
			w.Line();

			w.Line("/// <summary>Verifies call count satisfies the Called constraint. Throws VerificationException if not.</summary>");
			w.Line("public void Verify(global::KnockOff.Called times)");
			using (w.Braces())
			{
				w.Line("if (!times.Validate(_callCount))");
				w.Line("\tthrow new global::KnockOff.VerificationException(new global::KnockOff.VerificationFailure(\"indexer setter\", times, _callCount));");
			}
			w.Line();

			// ThenSet(callback) - lazy elevation from repeating to sequence mode
			w.Line($"/// <summary>Elevates to sequence mode and adds another setter callback. Returns sequence for further chaining.</summary>");
			w.Line($"public global::KnockOff.IIndexerSetSequence<{keyType}, {valueType}> ThenSet(global::System.Action<{parameterTypes}, {valueType}> callback)");
			using (w.Braces())
			{
				w.Line("if (_interceptor._setSequence == null)");
				using (w.Braces())
				{
					w.Line($"_interceptor._setSequence = new global::System.Collections.Generic.List<(global::System.Action<{parameterTypes}, {valueType}> Callback, IndexerSetBuilderImpl Tracking)>();");
					w.Line("_interceptor._setSequence.Add((_interceptor._set!, this));");
					w.Line("_interceptor._set = null;");
					w.Line("_interceptor._setTracking = null;");  // Clear to prevent double-counting in TotalSetCount
					w.Line("_interceptor._setSequenceIndex = 0;");
				}
				w.Line("var nextBuilder = new IndexerSetBuilderImpl(_interceptor);");
				w.Line("_interceptor._setSequence.Add((callback, nextBuilder));");
				w.Line("return new IndexerSetSequenceImpl(_interceptor);");
			}
			w.Line();

			w.Line("/// <summary>Marks for verification by Stub.Verify(). Returns this for fluent chaining.</summary>");
			w.Line($"public global::KnockOff.IIndexerSetBuilder<{keyType}, {valueType}> Verifiable()");
			using (w.Braces())
			{
				w.Line("_interceptor._isSetVerifiable = true;");
				w.Line("_interceptor._setVerifiableTimes = null;");
				w.Line("return this;");
			}
			w.Line();

			// Explicit interface implementation for base IIndexerSetTracking<TKey, TValue>.Verifiable()
			w.Line($"global::KnockOff.IIndexerSetTracking<{keyType}, {valueType}> global::KnockOff.IIndexerSetTracking<{keyType}, {valueType}>.Verifiable() => Verifiable();");
			w.Line($"global::KnockOff.IIndexerSetTracking<{keyType}, {valueType}> global::KnockOff.IIndexerSetTracking<{keyType}, {valueType}>.Verifiable(global::KnockOff.Called times) => Verifiable();");
		}
		w.Line();
	}

	#endregion

	#region Nested IndexerGetSequenceImpl

	private static void RenderIndexerGetSequenceImpl(
		CodeWriter w,
		string keyType,
		string valueType,
		string parameterTypes,
		string interceptorClassName)
	{
		w.Line($"/// <summary>Sequence implementation for ThenGet chaining.</summary>");
		w.Line($"private sealed class IndexerGetSequenceImpl : global::KnockOff.IIndexerGetSequence<{keyType}, {valueType}>");
		using (w.Braces())
		{
			w.Line($"private readonly {interceptorClassName} _interceptor;");
			w.Line();

			w.Line($"public IndexerGetSequenceImpl({interceptorClassName} interceptor) => _interceptor = interceptor;");
			w.Line();

			w.Line($"/// <summary>Adds another getter callback to the sequence. Each callback runs exactly once.</summary>");
			w.Line($"public global::KnockOff.IIndexerGetSequence<{keyType}, {valueType}> ThenGet(global::System.Func<{parameterTypes}, {valueType}> callback)");
			using (w.Braces())
			{
				w.Line("var tracking = new IndexerGetBuilderImpl(_interceptor);");
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
				w.Line("\tthrow new global::KnockOff.VerificationException(global::KnockOff.VerificationFailure.SequenceIncomplete(\"indexer getter\", sequenceLength, completedCount));");
			}
			w.Line();

			w.Line("/// <summary>Resets all tracking in the sequence.</summary>");
			w.Line("public void Reset() => _interceptor.Reset();");
			w.Line();

			w.Line("/// <summary>Marks this sequence for verification by Stub.Verify(). Returns this for fluent chaining.</summary>");
			w.Line($"public global::KnockOff.IIndexerGetSequence<{keyType}, {valueType}> Verifiable()");
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

	#region Nested IndexerSetSequenceImpl

	private static void RenderIndexerSetSequenceImpl(
		CodeWriter w,
		string keyType,
		string valueType,
		string parameterTypes,
		string interceptorClassName)
	{
		w.Line($"/// <summary>Sequence implementation for ThenSet chaining.</summary>");
		w.Line($"private sealed class IndexerSetSequenceImpl : global::KnockOff.IIndexerSetSequence<{keyType}, {valueType}>");
		using (w.Braces())
		{
			w.Line($"private readonly {interceptorClassName} _interceptor;");
			w.Line();

			w.Line($"public IndexerSetSequenceImpl({interceptorClassName} interceptor) => _interceptor = interceptor;");
			w.Line();

			w.Line($"/// <summary>Adds another setter callback to the sequence. Each callback runs exactly once.</summary>");
			w.Line($"public global::KnockOff.IIndexerSetSequence<{keyType}, {valueType}> ThenSet(global::System.Action<{parameterTypes}, {valueType}> callback)");
			using (w.Braces())
			{
				w.Line("var tracking = new IndexerSetBuilderImpl(_interceptor);");
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
				w.Line("\tthrow new global::KnockOff.VerificationException(global::KnockOff.VerificationFailure.SequenceIncomplete(\"indexer setter\", sequenceLength, completedCount));");
			}
			w.Line();

			w.Line("/// <summary>Resets all tracking in the sequence.</summary>");
			w.Line("public void Reset() => _interceptor.Reset();");
			w.Line();

			w.Line("/// <summary>Marks this sequence for verification by Stub.Verify(). Returns this for fluent chaining.</summary>");
			w.Line($"public global::KnockOff.IIndexerSetSequence<{keyType}, {valueType}> Verifiable()");
			using (w.Braces())
			{
				w.Line("_interceptor._isSetVerifiable = true;");
				w.Line("_interceptor._setVerifiableTimes = null;");
				w.Line("return this;");
			}
			w.Line();

			// ThenDefault() - terminates sequence with default after exhaustion (do nothing for setters)
			w.Line("/// <summary>Terminates sequence after exhaustion instead of repeating last callback (do nothing after exhaustion).</summary>");
			w.Line("public void ThenDefault()");
			using (w.Braces())
			{
				w.Line("_interceptor._setRepeatLastValue = false;");
			}
		}
		w.Line();
	}

	#endregion
}
