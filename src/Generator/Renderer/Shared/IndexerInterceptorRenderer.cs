// src/Generator/Renderer/Shared/IndexerInterceptorRenderer.cs
#nullable enable
using System.Collections.Generic;
using System.Linq;
using KnockOff.Model.Shared;

namespace KnockOff.Renderer.Shared;

/// <summary>
/// Renders indexer interceptor classes for both inline and flat stubs.
/// Supports single-indexer and multi-indexer interfaces.
/// Generates per-key builders, all-keys Get/Set callbacks, sequences,
/// InvokeGet/InvokeSet with priority chain, tracking, and verification.
/// </summary>
internal static class IndexerInterceptorRenderer
{
	/// <summary>
	/// Renders a complete indexer interceptor class for one or more indexers.
	/// When models.Count > 1, generates a multi-indexer interceptor with type-suffixed members.
	/// </summary>
	public static void RenderInterceptorClass(
		CodeWriter w,
		IReadOnlyList<UnifiedIndexerInterceptorModel> models,
		IndexerInterceptorRenderOptions options)
	{
		if (models.Count == 0) return;

		// Deduplicate by key type -- diamond inheritance can produce duplicate models
		// (e.g., IEntityBase.this[string] and IValidateBase.this[string] both have KeyType=string).
		// Keep the first model per KeyTypeFriendlyName (they share the same generated infrastructure).
		var seen = new HashSet<string>();
		var deduped = new List<UnifiedIndexerInterceptorModel>();
		foreach (var m in models)
		{
			if (seen.Add(m.KeyTypeFriendlyName))
				deduped.Add(m);
		}
		models = deduped;

		// Source interfaces from deduped models (one per key type, avoids duplicate _source fields)
		var allSourceInterfaces = models
			.Where(m => !string.IsNullOrEmpty(m.DeclaringInterface))
			.Select(m => m.DeclaringInterface)
			.Distinct()
			.ToList();

		var isMulti = models.Count > 1;
		var first = models[0];
		var interceptorClassName = first.InterceptorClassName;
		var typeParams = options.InterceptorTypeParameters;
		var constraints = options.InterceptorConstraints;
		var fullInterceptorClassName = interceptorClassName + typeParams;

		// Determine emission mode: base class mode for single-key, single-param indexers without init-only or ref return.
		// Multi-param indexers (KeyExpression starts with "(") stay inline because the calling convention
		// (InvokeGet(strict, a, b)) is incompatible with the base class (InvokeGet(strict, TKey key)).
		var isMultiParam = first.KeyExpression.StartsWith("(");
		var useBaseClass = !isMulti && !isMultiParam && !models.Any(m => m.IsInitOnly) && !models.Any(m => m.IsRefReturn);

		string classDecl;
		if (useBaseClass)
		{
			var keyType = first.KeyType;
			var valueType = first.ValueType;
			classDecl = $"public sealed class {interceptorClassName}{typeParams} : global::KnockOff.Interceptors.IndexerGetSetInterceptorBase<{keyType}, {valueType}>{constraints}";
		}
		else
		{
			classDecl = $"public sealed class {interceptorClassName}{typeParams}{constraints}";
		}

		w.Line($"/// <summary>Tracks and configures behavior for indexer.</summary>");
		using (w.Block(classDecl))
		{
			if (useBaseClass)
			{
				RenderBaseClassContent(w, first, options, fullInterceptorClassName, allSourceInterfaces);
			}
			else
			{
			// Source fields for Source(T) feature - one per declaring interface
			// Uses allSourceInterfaces (computed before dedup) to handle diamond inheritance.
			foreach (var iface in allSourceInterfaces)
			{
				w.Line($"/// <summary>Source object to delegate to when no Get/Set is configured.</summary>");
				w.Line($"internal {iface}? _source;");
				w.Line();
			}

			// Per-key storage and builders for each key type
			foreach (var model in models)
			{
				var suffix = isMulti ? $"_{model.KeyTypeFriendlyName}" : "";
				var builderName = isMulti ? $"{model.KeyTypeFriendlyName}PerKeyBuilder" : "PerKeyBuilder";

				w.Line($"private readonly global::System.Collections.Generic.Dictionary<{model.KeyType}, {builderName}> _perKeyBuilders{suffix} = new();");
			}
			w.Line();

			// Indexer accessor(s) returning per-key builders
			foreach (var model in models)
			{
				var builderName = isMulti ? $"{model.KeyTypeFriendlyName}PerKeyBuilder" : "PerKeyBuilder";
				var suffix = isMulti ? $"_{model.KeyTypeFriendlyName}" : "";

				w.Line($"/// <summary>Gets or creates a per-key builder for the specified key.</summary>");
				w.Line($"public {builderName} this[{model.ParameterSignature}]");
				using (w.Braces())
				{
					w.Line("get");
					using (w.Braces())
					{
						var keyExpr = model.KeyExpression;
						w.Line($"return _perKeyBuilders{suffix}.TryGetValue({keyExpr}, out var __existing) ? __existing : (_perKeyBuilders{suffix}[{keyExpr}] = new {builderName}());");
					}
				}
				w.Line();
			}

			// All-keys callback storage and tracking per key type
			foreach (var model in models)
			{
				var suffix = isMulti ? $"_{model.KeyTypeFriendlyName}" : "";
				var friendlyName = isMulti ? model.KeyTypeFriendlyName : "";

				if (model.HasGetter)
				{
					w.Line($"private global::System.Func<{model.KeyType}, {model.ValueType}>? _get{suffix};");
					w.Line($"private IndexerGetBuilderImpl{friendlyName}? _getTracking{suffix};");
					w.Line($"private global::System.Collections.Generic.List<(global::System.Func<{model.KeyType}, {model.ValueType}> Callback, IndexerGetBuilderImpl{friendlyName} Tracking)>? _getSequence{suffix};");
					w.Line($"private int _getSequenceIndex{suffix};");
					w.Line($"private bool _getRepeatLastValue{suffix} = true;");
					w.Line($"private bool _isGetVerifiable{suffix};");
					w.Line($"private global::KnockOff.Called? _getVerifiableTimes{suffix};");
					w.Line($"private int _unconfiguredGetCount{suffix};");
					w.Line($"private {model.NullableKeyType} _unconfiguredLastGetKey{suffix};");
					w.Line();
				}

				if (model.HasSetter)
				{
					w.Line($"private global::System.Action<{model.KeyType}, {model.ValueType}>? _set{suffix};");
					w.Line($"private IndexerSetBuilderImpl{friendlyName}? _setTracking{suffix};");
					w.Line($"private global::System.Collections.Generic.List<(global::System.Action<{model.KeyType}, {model.ValueType}> Callback, IndexerSetBuilderImpl{friendlyName} Tracking)>? _setSequence{suffix};");
					w.Line($"private int _setSequenceIndex{suffix};");
					w.Line($"private bool _setRepeatLastValue{suffix} = true;");
					w.Line($"private bool _isSetVerifiable{suffix};");
					w.Line($"private global::KnockOff.Called? _setVerifiableTimes{suffix};");
					w.Line($"private int _unconfiguredSetCount{suffix};");
					w.Line($"private ({model.KeyType} Key, {model.ValueType} Value)? _unconfiguredLastSetEntry{suffix};");
					w.Line();
				}
			}

			// When chain fields per key type (separate getter and setter chains)
			foreach (var model in models)
			{
				var suffix = isMulti ? $"_{model.KeyTypeFriendlyName}" : "";
				var friendlyName = isMulti ? model.KeyTypeFriendlyName : "";

				if (model.HasGetter)
				{
					w.Line($"private global::System.Collections.Generic.List<IndexerGetWhenMatcher{friendlyName}>? _whenGetChain{suffix};");
					w.Line($"private int _whenGetChainHead{suffix};");
					w.Line($"private bool _whenGetVerifiable{suffix};");
					w.Line();
				}
				if (model.HasSetter)
				{
					w.Line($"private global::System.Collections.Generic.List<IndexerSetWhenMatcher{friendlyName}>? _whenSetChain{suffix};");
					w.Line($"private int _whenSetChainHead{suffix};");
					w.Line($"private bool _whenSetVerifiable{suffix};");
					w.Line();
				}
			}

			// Aggregate total get/set count properties
			RenderTotalCountProperties(w, models, isMulti);

			// Tracking properties: LastGetKey, LastSetEntry (type-suffixed for multi)
			foreach (var model in models)
			{
				var suffix = isMulti ? $"_{model.KeyTypeFriendlyName}" : "";
				var propSuffix = isMulti ? model.KeyTypeFriendlyName : "";

				if (model.HasGetter)
				{
					w.Line($"/// <summary>The key from the last getter access{(isMulti ? $" ({model.KeyTypeFriendlyName} indexer)" : "")}.</summary>");
					w.Line($"public {model.NullableKeyType} Last{propSuffix}GetKey => _unconfiguredLastGetKey{suffix};");
					w.Line();
				}

				if (model.HasSetter)
				{
					w.Line($"/// <summary>The key-value pair from the last setter access{(isMulti ? $" ({model.KeyTypeFriendlyName} indexer)" : "")}.</summary>");
					w.Line($"public ({model.KeyType} Key, {model.ValueType} Value)? Last{propSuffix}SetEntry => _unconfiguredLastSetEntry{suffix};");
					w.Line();
				}
			}

			// All-keys Get/Set methods (overloaded for multi-indexer)
			foreach (var model in models)
			{
				var suffix = isMulti ? $"_{model.KeyTypeFriendlyName}" : "";
				var friendlyName = isMulti ? model.KeyTypeFriendlyName : "";

				if (model.HasGetter)
				{
					w.Line($"/// <summary>Configures getter callback that repeats indefinitely. Returns builder for tracking and sequence chaining.</summary>");
					w.Line($"public global::KnockOff.IIndexerGetBuilder<{model.KeyType}, {model.ValueType}> Get(global::System.Func<{model.KeyType}, {model.ValueType}> callback)");
					using (w.Braces())
					{
						w.Line($"_getSequence{suffix} = null;");
						w.Line($"_getSequenceIndex{suffix} = 0;");
						w.Line($"_isGetVerifiable{suffix} = false;");
						w.Line($"_getVerifiableTimes{suffix} = null;");
						w.Line($"_get{suffix} = callback;");
						w.Line($"_getTracking{suffix} = new IndexerGetBuilderImpl{friendlyName}(this);");
						w.Line($"return _getTracking{suffix};");
					}
					w.Line();
				}

				if (model.HasSetter)
				{
					w.Line($"/// <summary>Configures setter callback that repeats indefinitely. Returns builder for tracking and sequence chaining.</summary>");
					w.Line($"public global::KnockOff.IIndexerSetBuilder<{model.KeyType}, {model.ValueType}> Set(global::System.Action<{model.KeyType}, {model.ValueType}> callback)");
					using (w.Braces())
					{
						w.Line($"_setSequence{suffix} = null;");
						w.Line($"_setSequenceIndex{suffix} = 0;");
						w.Line($"_isSetVerifiable{suffix} = false;");
						w.Line($"_setVerifiableTimes{suffix} = null;");
						w.Line($"_set{suffix} = callback;");
						w.Line($"_setTracking{suffix} = new IndexerSetBuilderImpl{friendlyName}(this);");
						w.Line($"return _setTracking{suffix};");
					}
					w.Line();
				}
			}

			// When() entry point methods per key type
			foreach (var model in models)
			{
				var suffix = isMulti ? $"_{model.KeyTypeFriendlyName}" : "";
				var friendlyName = isMulti ? model.KeyTypeFriendlyName : "";

				w.Line($"/// <summary>Configures predicate-based key matching. Returns builder for Returns()/Get()/Set().</summary>");
				w.Line($"public IndexerWhenBuilder{friendlyName} When(global::System.Func<{model.KeyType}, bool> predicate)");
				using (w.Braces())
				{
					w.Line($"return new IndexerWhenBuilder{friendlyName}(this, predicate);");
				}
				w.Line();
			}

			// Ref return backing fields (per key type for multi-indexer)
			foreach (var model in models)
			{
				if (model.IsRefReturn)
				{
					var suffix = isMulti ? $"_{model.KeyTypeFriendlyName}" : "";
					w.Line("#pragma warning disable CS8618 // Ref return backing field initialized by InvokeRefGet before use");
					w.Line($"internal {model.ValueType} _refReturnBacking{suffix};");
					w.Line("#pragma warning restore CS8618");
					w.Line();
				}
			}

			// InvokeGet/InvokeSet/InvokeRefGet methods per key type
			foreach (var model in models)
			{
				var invokeSuffix = isMulti ? $"_{model.KeyTypeFriendlyName}" : "";
				var fieldSuffix = isMulti ? $"_{model.KeyTypeFriendlyName}" : "";
				var builderName = isMulti ? $"{model.KeyTypeFriendlyName}PerKeyBuilder" : "PerKeyBuilder";

				if (model.HasGetter)
				{
					RenderInvokeGet(w, model, options, invokeSuffix, fieldSuffix, builderName);
					if (model.IsRefReturn)
					{
						RenderInvokeRefGet(w, model, options, invokeSuffix, fieldSuffix, builderName);
					}
				}
				if (model.HasSetter)
				{
					RenderInvokeSet(w, model, options, invokeSuffix, fieldSuffix, builderName);
				}
			}

			// Reset method
			RenderResetMethod(w, models, isMulti, allSourceInterfaces.Count > 0);

			// Verification methods (combined across all key types)
			RenderVerificationMethods(w, models, isMulti, fullInterceptorClassName);

			// Internal verification support
			RenderInternalVerification(w, models, isMulti, fullInterceptorClassName);

			// Nested PerKeyBuilder and PerKeySequence classes per key type
			foreach (var model in models)
			{
				var builderName = isMulti ? $"{model.KeyTypeFriendlyName}PerKeyBuilder" : "PerKeyBuilder";
				var sequenceName = isMulti ? $"{model.KeyTypeFriendlyName}PerKeySequence" : "PerKeySequence";
				RenderPerKeyBuilder(w, model, builderName, sequenceName);
				RenderPerKeySequence(w, model, builderName, sequenceName);
			}

			// Nested all-keys builder and sequence implementation classes per key type
			foreach (var model in models)
			{
				var suffix = isMulti ? $"_{model.KeyTypeFriendlyName}" : "";
				var friendlyName = isMulti ? model.KeyTypeFriendlyName : "";

				if (model.HasGetter)
				{
					RenderIndexerGetBuilderImpl(w, model, fullInterceptorClassName, friendlyName, suffix);
					RenderIndexerGetSequenceImpl(w, model, fullInterceptorClassName, friendlyName, suffix);
				}
				if (model.HasSetter)
				{
					RenderIndexerSetBuilderImpl(w, model, fullInterceptorClassName, friendlyName, suffix);
					RenderIndexerSetSequenceImpl(w, model, fullInterceptorClassName, friendlyName, suffix);
				}
			}

			// Nested When matcher, builder, and chain classes per key type
			foreach (var model in models)
			{
				var suffix = isMulti ? $"_{model.KeyTypeFriendlyName}" : "";
				var friendlyName = isMulti ? model.KeyTypeFriendlyName : "";

				RenderIndexerGetWhenMatcherClasses(w, model, friendlyName);
				RenderIndexerWhenBuilder(w, model, fullInterceptorClassName, friendlyName, suffix);
				RenderIndexerGetWhenChain(w, model, fullInterceptorClassName, friendlyName, suffix);

				if (model.HasSetter)
				{
					RenderIndexerSetWhenMatcherClasses(w, model, friendlyName);
					RenderIndexerSetWhenChain(w, model, fullInterceptorClassName, friendlyName, suffix);
				}
			}
			} // end else (inline mode)
		}
		w.Line();
	}

	#region Base Class Mode: Single-Key Indexers

	/// <summary>
	/// Renders a thin interceptor that inherits from IndexerGetSetInterceptorBase&lt;TKey, TValue&gt;.
	/// Only emitted for single-key indexers without init-only or ref return.
	/// </summary>
	private static void RenderBaseClassContent(
		CodeWriter w,
		UnifiedIndexerInterceptorModel model,
		IndexerInterceptorRenderOptions options,
		string fullInterceptorClassName,
		List<string> allSourceInterfaces)
	{
		var keyType = model.KeyType;
		var valueType = model.ValueType;
		var indexerName = model.IndexerName;

		// Source fields for Source(T) feature - one per declaring interface
		foreach (var iface in allSourceInterfaces)
		{
			w.Line($"/// <summary>Source object to delegate to when no Get/Set is configured.</summary>");
			w.Line($"internal {iface}? _source;");
			w.Line();
		}

		// No constructor needed -- base class has default parameterless constructor


		// Abstract override: InvokeGetUnconfigured (always required by base class)
		// For single-param indexers (the only ones using base class mode), the key IS the param.
		w.Line($"protected override {valueType} InvokeGetUnconfigured(bool strict, {keyType} key)");
		using (w.Braces())
		{
			if (model.HasGetter && !string.IsNullOrEmpty(model.DeclaringInterface))
			{
				w.Line($"if (_source is {{ }} src) return src[key];");
			}
			w.Line($"if (strict) throw global::KnockOff.StubException.NotConfigured(\"\", \"{indexerName}\");");
			w.Line($"return {model.DefaultExpression};");
		}
		w.Line();

		// Abstract override: InvokeSetUnconfigured (always required by base class)
		w.Line($"protected override void InvokeSetUnconfigured(bool strict, {keyType} key, {valueType} value)");
		using (w.Braces())
		{
			if (model.HasSetter && !string.IsNullOrEmpty(model.DeclaringInterface))
			{
				w.Line($"if (_source is {{ }} src) {{ src[key] = value; return; }}");
			}
			w.Line($"if (strict) throw global::KnockOff.StubException.NotConfigured(\"\", \"{indexerName}\");");
		}
		w.Line();

		// Reset override (base.Reset() + clear source)
		w.Line("public override void Reset()");
		using (w.Braces())
		{
			w.Line("base.Reset();");
			if (allSourceInterfaces.Count > 0)
			{
				w.Line("_source = null;");
			}
		}
		w.Line();

		// Typed indexer accessor: this[params] -> GetOrCreatePerKeyBuilder(keyExpr)
		// No "new" keyword needed -- base class has GetOrCreatePerKeyBuilder(TKey) method, not an indexer.
		w.Line($"/// <summary>Gets or creates a per-key builder for the specified key.</summary>");
		w.Line($"public PerKeyBuilder this[{model.ParameterSignature}] => GetOrCreatePerKeyBuilder({model.KeyExpression});");
		w.Line();

		// Typed Get() method returning IIndexerGetBuilder<TKey, TValue>
		// Base class inner classes now implement the interfaces directly, so we use IndexerGetBuilderBase
		if (model.HasGetter)
		{
			w.Line($"/// <summary>Configures getter callback that repeats indefinitely. Returns builder for tracking and sequence chaining.</summary>");
			w.Line($"public global::KnockOff.IIndexerGetBuilder<{keyType}, {valueType}> Get(global::System.Func<{keyType}, {valueType}> callback)");
			using (w.Braces())
			{
				w.Line("_getSequence = null; _getSequenceIndex = 0;");
				w.Line("_isGetVerifiable = false; _getVerifiableTimes = null;");
				w.Line("_get = callback;");
				w.Line("var builder = new IndexerGetBuilderBase(this);");
				w.Line("_getTracking = builder;");
				w.Line("return builder;");
			}
			w.Line();
		}

		// Typed Set() method returning IIndexerSetBuilder<TKey, TValue>
		// Base class inner classes now implement the interfaces directly, so we use IndexerSetBuilderBase
		if (model.HasSetter)
		{
			w.Line($"/// <summary>Configures setter callback that repeats indefinitely. Returns builder for tracking and sequence chaining.</summary>");
			w.Line($"public global::KnockOff.IIndexerSetBuilder<{keyType}, {valueType}> Set(global::System.Action<{keyType}, {valueType}> callback)");
			using (w.Braces())
			{
				w.Line("_setSequence = null; _setSequenceIndex = 0;");
				w.Line("_isSetVerifiable = false; _setVerifiableTimes = null;");
				w.Line("_set = callback;");
				w.Line("var builder = new IndexerSetBuilderBase(this);");
				w.Line("_setTracking = builder;");
				w.Line("return builder;");
			}
			w.Line();
		}

		// When() entry point -- uses base class IndexerWhenBuilderBase directly (no thin subclass needed)
		w.Line($"/// <summary>Configures predicate-based key matching. Returns builder for Returns()/Get()/Set().</summary>");
		w.Line($"public IndexerWhenBuilderBase When(global::System.Func<{keyType}, bool> predicate)");
		using (w.Braces())
		{
			w.Line("return new IndexerWhenBuilderBase(this, predicate);");
		}
		w.Line();

		// Verifiable fluent methods (marks both get and set as verifiable)
		// Uses "new" to intentionally hide base class Verifiable() which returns the base type
		w.Line($"/// <summary>Marks this indexer for verification by Stub.Verify(). Returns this for fluent chaining.</summary>");
		{
			var parts = new List<string>();
			if (model.HasGetter) parts.Add("_isGetVerifiable = true; _getVerifiableTimes = null;");
			if (model.HasSetter) parts.Add("_isSetVerifiable = true; _setVerifiableTimes = null;");
			w.Line($"public new {fullInterceptorClassName} Verifiable() {{ {string.Join(" ", parts)} return this; }}");
		}
		w.Line();
		w.Line($"/// <summary>Marks this indexer for verification by Stub.Verify() with Called constraint. Returns this for fluent chaining.</summary>");
		{
			var parts = new List<string>();
			if (model.HasGetter) parts.Add("_isGetVerifiable = true; _getVerifiableTimes = times;");
			if (model.HasSetter) parts.Add("_isSetVerifiable = true; _setVerifiableTimes = times;");
			w.Line($"public new {fullInterceptorClassName} Verifiable(global::KnockOff.Called times) {{ {string.Join(" ", parts)} return this; }}");
		}
		w.Line();

		// No thin inner classes needed -- base class inner classes implement interfaces directly
		// and When chain base classes have final typed methods
	}

	#endregion

	#region TotalCount Properties

	private static void RenderTotalCountProperties(
		CodeWriter w,
		IReadOnlyList<UnifiedIndexerInterceptorModel> models,
		bool isMulti)
	{
		// Build combined total get count across all key types using explicit loops (avoid LINQ in generated code)
		var hasAnyGetter = models.Any(m => m.HasGetter);
		var hasAnySetter = models.Any(m => m.HasSetter);

		if (hasAnyGetter)
		{
			w.Line("private int TotalGetCount { get { var sum = 0;");
			foreach (var model in models)
			{
				if (!model.HasGetter) continue;
				var suffix = isMulti ? $"_{model.KeyTypeFriendlyName}" : "";
				w.Line($"sum += _unconfiguredGetCount{suffix} + (_getTracking{suffix}?._callCount ?? 0);");
				w.Line($"if (_getSequence{suffix} != null) foreach (var s in _getSequence{suffix}) sum += s.Tracking._callCount;");
				w.Line($"foreach (var b in _perKeyBuilders{suffix}.Values) sum += b._getCallCount;");
			}
			w.Line("return sum; } }");
		}
		if (hasAnySetter)
		{
			w.Line("private int TotalSetCount { get { var sum = 0;");
			foreach (var model in models)
			{
				if (!model.HasSetter) continue;
				var suffix = isMulti ? $"_{model.KeyTypeFriendlyName}" : "";
				w.Line($"sum += _unconfiguredSetCount{suffix} + (_setTracking{suffix}?._callCount ?? 0);");
				w.Line($"if (_setSequence{suffix} != null) foreach (var s in _setSequence{suffix}) sum += s.Tracking._callCount;");
				w.Line($"foreach (var b in _perKeyBuilders{suffix}.Values) sum += b._setCallCount;");
			}
			w.Line("return sum; } }");
		}
		if (hasAnyGetter || hasAnySetter)
		{
			w.Line();
		}
	}

	#endregion

	#region InvokeGet / InvokeSet / InvokeRefGet Methods

	private static void RenderInvokeGet(
		CodeWriter w,
		UnifiedIndexerInterceptorModel model,
		IndexerInterceptorRenderOptions options,
		string invokeSuffix,
		string fieldSuffix,
		string builderName)
	{
		var strictParam = options.IncludeStrictParameter ? "bool strict, " : "";

		w.Line($"/// <summary>Invokes the configured getter callback. Called by explicit interface implementation.</summary>");
		w.Line($"internal {model.ValueType} InvokeGet{invokeSuffix}({strictParam}{model.ParameterSignature})");
		using (w.Braces())
		{
			// Always record the last key accessed (for LastGetKey tracking)
			w.Line($"_unconfiguredLastGetKey{fieldSuffix} = {model.KeyExpression};");
			w.Line();

			// Priority 1: Per-key builder (if configured)
			w.Line($"if (_perKeyBuilders{fieldSuffix}.TryGetValue({model.KeyExpression}, out var perKeyBuilder) && perKeyBuilder.HasGetConfig)");
			using (w.Braces())
			{
				w.Line($"perKeyBuilder._getCallCount++;");
				w.Line("return perKeyBuilder.InvokeGet();");
			}
			w.Line();

			// Priority 2: When predicate chain (getter)
			{
				var friendlyName = fieldSuffix == "" ? "" : fieldSuffix.Substring(1); // strip leading _
				w.Line($"if (_whenGetChain{fieldSuffix} != null && _whenGetChainHead{fieldSuffix} < _whenGetChain{fieldSuffix}.Count)");
				using (w.Braces())
				{
					w.Line($"var matcher = _whenGetChain{fieldSuffix}[_whenGetChainHead{fieldSuffix}];");
					w.Line($"if (matcher.Matches({model.KeyExpression}))");
					using (w.Braces())
					{
						w.Line("matcher.CallCount++;");
						w.Line();
						w.Line("// Advance HEAD unless at last matcher (which repeats)");
						w.Line($"if (_whenGetChainHead{fieldSuffix} < _whenGetChain{fieldSuffix}.Count - 1)");
						using (w.Braces())
						{
							w.Line($"_whenGetChainHead{fieldSuffix}++;");
						}
						w.Line("// At last matcher: never advance (repeat behavior for both ThenWhen and ThenGet)");
						w.Line();
						w.Line($"return matcher.Call({model.KeyExpression});");
					}
					w.Line("else if (matcher.IsTerminal)");
					using (w.Braces())
					{
						w.Line("// ThenNone: didn't match (always false), exhaust by advancing past it");
						w.Line($"_whenGetChainHead{fieldSuffix}++;");
					}
					w.Line("// Non-terminal didn't match: fall through to rest of priority chain");
				}
				w.Line();
			}

			// Priority 3: All-keys sequence (if present and not exhausted)
			w.Line($"if (_getSequence{fieldSuffix} != null && _getSequenceIndex{fieldSuffix} < _getSequence{fieldSuffix}.Count)");
			using (w.Braces())
			{
				w.Line($"var (callback, tracking) = _getSequence{fieldSuffix}[_getSequenceIndex{fieldSuffix}];");
				w.Line($"tracking.RecordCall({model.KeyExpression});");
				w.Line($"_getSequenceIndex{fieldSuffix}++;");
				w.Line($"return callback({model.KeyExpression});");
			}
			w.Line();

			// Sequence exhausted - check strict mode first (always throws), then repeat-last-value, then default
			w.Line($"if (_getSequence{fieldSuffix} != null && _getSequenceIndex{fieldSuffix} >= _getSequence{fieldSuffix}.Count)");
			using (w.Braces())
			{
				w.Line($"if ({options.StrictAccessExpression}) throw global::KnockOff.StubException.SequenceExhausted(\"{model.IndexerName} (get)\");");
				w.Line($"if (_getRepeatLastValue{fieldSuffix} && _getSequence{fieldSuffix}.Count > 0)");
				using (w.Braces())
				{
					w.Line($"var (callback, tracking) = _getSequence{fieldSuffix}[_getSequence{fieldSuffix}.Count - 1];");
					w.Line($"tracking.RecordCall({model.KeyExpression});");
					w.Line($"return callback({model.KeyExpression});");
				}
			}
			w.Line();

			// Priority 4: Repeating Get callback
			w.Line($"if (_get{fieldSuffix} != null && _getTracking{fieldSuffix} != null)");
			using (w.Braces())
			{
				w.Line($"_getTracking{fieldSuffix}.RecordCall({model.KeyExpression});");
				w.Line($"return _get{fieldSuffix}({model.KeyExpression});");
			}
			w.Line();

			// No callback configured - track unconfigured call count
			w.Line($"_unconfiguredGetCount{fieldSuffix}++;");
			w.Line();

			// Priority 5: Source (if available)
			if (!string.IsNullOrEmpty(model.DeclaringInterface))
			{
				w.Line($"if (_source is {{ }} src) return src[{model.ArgumentList}];");
				w.Line();
			}

			// Priority 6: Strict mode check
			w.Line($"if ({options.StrictAccessExpression}) throw global::KnockOff.StubException.NotConfigured(\"\", \"{model.IndexerName}\");");

			// Priority 7: Default
			w.Line("return default!;");
		}
		w.Line();
	}

	private static void RenderInvokeSet(
		CodeWriter w,
		UnifiedIndexerInterceptorModel model,
		IndexerInterceptorRenderOptions options,
		string invokeSuffix,
		string fieldSuffix,
		string builderName)
	{
		var strictParam = options.IncludeStrictParameter ? "bool strict, " : "";

		w.Line($"/// <summary>Invokes the configured setter callback. Called by explicit interface implementation.</summary>");
		w.Line($"internal void InvokeSet{invokeSuffix}({strictParam}{model.ParameterSignature}, {model.ValueType} value)");
		using (w.Braces())
		{
			// Always record the last set entry (for LastSetEntry tracking)
			w.Line($"_unconfiguredLastSetEntry{fieldSuffix} = ({model.KeyExpression}, value);");
			w.Line();

			// Priority 1: Per-key builder (if configured)
			w.Line($"if (_perKeyBuilders{fieldSuffix}.TryGetValue({model.KeyExpression}, out var perKeyBuilder) && perKeyBuilder.HasSetConfig)");
			using (w.Braces())
			{
				w.Line($"perKeyBuilder._setCallCount++;");
				w.Line("perKeyBuilder.InvokeSet(value);");
				w.Line("return;");
			}
			w.Line();

			// Priority 2: When predicate chain (setter)
			{
				w.Line($"if (_whenSetChain{fieldSuffix} != null && _whenSetChainHead{fieldSuffix} < _whenSetChain{fieldSuffix}.Count)");
				using (w.Braces())
				{
					w.Line($"var matcher = _whenSetChain{fieldSuffix}[_whenSetChainHead{fieldSuffix}];");
					w.Line($"if (matcher.Matches({model.KeyExpression}))");
					using (w.Braces())
					{
						w.Line("matcher.CallCount++;");
						w.Line();
						w.Line("// Advance HEAD unless at last matcher (which repeats)");
						w.Line($"if (_whenSetChainHead{fieldSuffix} < _whenSetChain{fieldSuffix}.Count - 1)");
						using (w.Braces())
						{
							w.Line($"_whenSetChainHead{fieldSuffix}++;");
						}
						w.Line();
						w.Line($"matcher.Call({model.KeyExpression}, value);");
						w.Line("return;");
					}
					w.Line("else if (matcher.IsTerminal)");
					using (w.Braces())
					{
						w.Line("// ThenNone: didn't match (always false), exhaust by advancing past it");
						w.Line($"_whenSetChainHead{fieldSuffix}++;");
					}
					w.Line("// Non-terminal didn't match: fall through to rest of priority chain");
				}
				w.Line();
			}

			// Priority 3: All-keys sequence (if present and not exhausted)
			w.Line($"if (_setSequence{fieldSuffix} != null && _setSequenceIndex{fieldSuffix} < _setSequence{fieldSuffix}.Count)");
			using (w.Braces())
			{
				w.Line($"var (callback, tracking) = _setSequence{fieldSuffix}[_setSequenceIndex{fieldSuffix}];");
				w.Line($"tracking.RecordCall({model.KeyExpression}, value);");
				w.Line($"_setSequenceIndex{fieldSuffix}++;");
				w.Line($"callback({model.KeyExpression}, value);");
				w.Line("return;");
			}
			w.Line();

			// Sequence exhausted
			w.Line($"if (_setSequence{fieldSuffix} != null && _setSequenceIndex{fieldSuffix} >= _setSequence{fieldSuffix}.Count)");
			using (w.Braces())
			{
				w.Line($"if ({options.StrictAccessExpression}) throw global::KnockOff.StubException.SequenceExhausted(\"{model.IndexerName} (set)\");");
				w.Line($"if (_setRepeatLastValue{fieldSuffix} && _setSequence{fieldSuffix}.Count > 0)");
				using (w.Braces())
				{
					w.Line($"var (callback, tracking) = _setSequence{fieldSuffix}[_setSequence{fieldSuffix}.Count - 1];");
					w.Line($"tracking.RecordCall({model.KeyExpression}, value);");
					w.Line($"callback({model.KeyExpression}, value);");
					w.Line("return;");
				}
				w.Line("return;");
			}
			w.Line();

			// Priority 4: Repeating Set callback
			w.Line($"if (_set{fieldSuffix} != null && _setTracking{fieldSuffix} != null)");
			using (w.Braces())
			{
				w.Line($"_setTracking{fieldSuffix}.RecordCall({model.KeyExpression}, value);");
				w.Line($"_set{fieldSuffix}({model.KeyExpression}, value);");
				w.Line("return;");
			}
			w.Line();

			// No callback configured - track unconfigured call count
			w.Line($"_unconfiguredSetCount{fieldSuffix}++;");
			w.Line();

			// Priority 5: Source (if available) - skip for init-only indexers
			if (!string.IsNullOrEmpty(model.DeclaringInterface) && !model.IsInitOnly)
			{
				w.Line($"if (_source is {{ }} src) {{ src[{model.ArgumentList}] = value; return; }}");
				w.Line();
			}

			// Priority 6: Strict mode check
			w.Line($"if ({options.StrictAccessExpression}) throw global::KnockOff.StubException.NotConfigured(\"\", \"{model.IndexerName}\");");
		}
		w.Line();
	}

	private static void RenderInvokeRefGet(
		CodeWriter w,
		UnifiedIndexerInterceptorModel model,
		IndexerInterceptorRenderOptions options,
		string invokeSuffix,
		string fieldSuffix,
		string builderName)
	{
		var strictParam = options.IncludeStrictParameter ? "bool strict, " : "";

		w.Line($"/// <summary>Invokes the configured getter callback, writing result to _refReturnBacking. Called by ref return interface implementations.</summary>");
		w.Line($"internal void InvokeRefGet{invokeSuffix}({strictParam}{model.ParameterSignature})");
		using (w.Braces())
		{
			// Always record the last key accessed (for LastGetKey tracking)
			w.Line($"_unconfiguredLastGetKey{fieldSuffix} = {model.KeyExpression};");
			w.Line();

			// Priority 1: Per-key builder (if configured)
			w.Line($"if (_perKeyBuilders{fieldSuffix}.TryGetValue({model.KeyExpression}, out var perKeyBuilder) && perKeyBuilder.HasGetConfig)");
			using (w.Braces())
			{
				w.Line($"perKeyBuilder._getCallCount++;");
				w.Line($"_refReturnBacking{fieldSuffix} = perKeyBuilder.InvokeGet();");
				w.Line("return;");
			}
			w.Line();

			// Priority 2: When predicate chain (getter)
			{
				w.Line($"if (_whenGetChain{fieldSuffix} != null && _whenGetChainHead{fieldSuffix} < _whenGetChain{fieldSuffix}.Count)");
				using (w.Braces())
				{
					w.Line($"var matcher = _whenGetChain{fieldSuffix}[_whenGetChainHead{fieldSuffix}];");
					w.Line($"if (matcher.Matches({model.KeyExpression}))");
					using (w.Braces())
					{
						w.Line("matcher.CallCount++;");
						w.Line($"if (_whenGetChainHead{fieldSuffix} < _whenGetChain{fieldSuffix}.Count - 1)");
						using (w.Braces())
						{
							w.Line($"_whenGetChainHead{fieldSuffix}++;");
						}
						w.Line($"_refReturnBacking{fieldSuffix} = matcher.Call({model.KeyExpression});");
						w.Line("return;");
					}
					w.Line("else if (matcher.IsTerminal)");
					using (w.Braces())
					{
						w.Line($"_whenGetChainHead{fieldSuffix}++;");
					}
				}
				w.Line();
			}

			// Priority 3: All-keys sequence
			w.Line($"if (_getSequence{fieldSuffix} != null && _getSequenceIndex{fieldSuffix} < _getSequence{fieldSuffix}.Count)");
			using (w.Braces())
			{
				w.Line($"var (callback, tracking) = _getSequence{fieldSuffix}[_getSequenceIndex{fieldSuffix}];");
				w.Line($"tracking.RecordCall({model.KeyExpression});");
				w.Line($"_getSequenceIndex{fieldSuffix}++;");
				w.Line($"_refReturnBacking{fieldSuffix} = callback({model.KeyExpression});");
				w.Line("return;");
			}
			w.Line();

			// Sequence exhausted
			w.Line($"if (_getSequence{fieldSuffix} != null && _getSequenceIndex{fieldSuffix} >= _getSequence{fieldSuffix}.Count)");
			using (w.Braces())
			{
				w.Line($"if ({options.StrictAccessExpression}) throw global::KnockOff.StubException.SequenceExhausted(\"{model.IndexerName} (get)\");");
				w.Line($"if (_getRepeatLastValue{fieldSuffix} && _getSequence{fieldSuffix}.Count > 0)");
				using (w.Braces())
				{
					w.Line($"var (callback, tracking) = _getSequence{fieldSuffix}[_getSequence{fieldSuffix}.Count - 1];");
					w.Line($"tracking.RecordCall({model.KeyExpression});");
					w.Line($"_refReturnBacking{fieldSuffix} = callback({model.KeyExpression});");
					w.Line("return;");
				}
			}
			w.Line();

			// Priority 4: Repeating Get callback
			w.Line($"if (_get{fieldSuffix} != null && _getTracking{fieldSuffix} != null)");
			using (w.Braces())
			{
				w.Line($"_getTracking{fieldSuffix}.RecordCall({model.KeyExpression});");
				w.Line($"_refReturnBacking{fieldSuffix} = _get{fieldSuffix}({model.KeyExpression});");
				w.Line("return;");
			}
			w.Line();

			// No callback configured - track unconfigured call count
			w.Line($"_unconfiguredGetCount{fieldSuffix}++;");
			w.Line();

			// Priority 5: Source (if available)
			if (!string.IsNullOrEmpty(model.DeclaringInterface))
			{
				w.Line($"if (_source is {{ }} src) {{ _refReturnBacking{fieldSuffix} = src[{model.ArgumentList}]; return; }}");
				w.Line();
			}

			// Priority 6: Strict mode check
			w.Line($"if ({options.StrictAccessExpression}) throw global::KnockOff.StubException.NotConfigured(\"\", \"{model.IndexerName}\");");

			// Priority 7: Default
			w.Line($"_refReturnBacking{fieldSuffix} = default!;");
		}
		w.Line();
	}

	#endregion

	#region Reset Method

	private static void RenderResetMethod(
		CodeWriter w,
		IReadOnlyList<UnifiedIndexerInterceptorModel> models,
		bool isMulti,
		bool hasSourceField)
	{
		w.Line("/// <summary>Resets tracking state but preserves configuration (Get, Set, per-key builders) and verifiable marking.</summary>");
		w.Line("public void Reset()");
		using (w.Braces())
		{
			foreach (var model in models)
			{
				var suffix = isMulti ? $"_{model.KeyTypeFriendlyName}" : "";

				if (model.HasGetter)
				{
					w.Line($"_unconfiguredGetCount{suffix} = 0;");
					w.Line($"_unconfiguredLastGetKey{suffix} = default;");
					w.Line($"_getTracking{suffix}?.Reset();");
					w.Line($"if (_getSequence{suffix} != null)");
					using (w.Braces())
					{
						w.Line($"foreach (var (_, tracking) in _getSequence{suffix})");
						w.Line("\ttracking.Reset();");
					}
					w.Line($"_getSequenceIndex{suffix} = 0;");
				}
				if (model.HasSetter)
				{
					w.Line($"_unconfiguredSetCount{suffix} = 0;");
					w.Line($"_unconfiguredLastSetEntry{suffix} = default;");
					w.Line($"_setTracking{suffix}?.Reset();");
					w.Line($"if (_setSequence{suffix} != null)");
					using (w.Braces())
					{
						w.Line($"foreach (var (_, tracking) in _setSequence{suffix})");
						w.Line("\ttracking.Reset();");
					}
					w.Line($"_setSequenceIndex{suffix} = 0;");
				}

				// Reset When chains
				if (model.HasGetter)
				{
					w.Line($"_whenGetChainHead{suffix} = 0;");
					w.Line($"if (_whenGetChain{suffix} != null) foreach (var m in _whenGetChain{suffix}) m.CallCount = 0;");
				}
				if (model.HasSetter)
				{
					w.Line($"_whenSetChainHead{suffix} = 0;");
					w.Line($"if (_whenSetChain{suffix} != null) foreach (var m in _whenSetChain{suffix}) m.CallCount = 0;");
				}

				// Reset per-key builders
				w.Line($"foreach (var b in _perKeyBuilders{suffix}.Values) b.Reset();");
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

	private static void RenderVerificationMethods(
		CodeWriter w,
		IReadOnlyList<UnifiedIndexerInterceptorModel> models,
		bool isMulti,
		string fullInterceptorClassName)
	{
		var hasAnyGetter = models.Any(m => m.HasGetter);
		var hasAnySetter = models.Any(m => m.HasSetter);
		var indexerName = models[0].IndexerName;

		var totalCountExpr = hasAnyGetter && hasAnySetter
			? "TotalGetCount + TotalSetCount"
			: (hasAnyGetter ? "TotalGetCount" : "TotalSetCount");

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
			w.Line($"\tthrow new global::KnockOff.VerificationException(new global::KnockOff.VerificationFailure(\"{indexerName}\", times, totalCount));");
		}
		w.Line();

		// VerifyGet (combined across all key types)
		if (hasAnyGetter)
		{
			w.Line("/// <summary>Verifies the getter was accessed at least once. Throws VerificationException if not.</summary>");
			w.Line("public void VerifyGet() => VerifyGet(global::KnockOff.Called.AtLeastOnce);");
			w.Line();

			w.Line("/// <summary>Verifies getter access count satisfies the Called constraint. Throws VerificationException if not.</summary>");
			w.Line("public void VerifyGet(global::KnockOff.Called times)");
			using (w.Braces())
			{
				w.Line("if (!times.Validate(TotalGetCount))");
				w.Line($"\tthrow new global::KnockOff.VerificationException(new global::KnockOff.VerificationFailure(\"{indexerName} (get)\", times, TotalGetCount));");
			}
			w.Line();
		}

		// VerifySet (combined across all key types)
		if (hasAnySetter)
		{
			w.Line("/// <summary>Verifies the setter was accessed at least once. Throws VerificationException if not.</summary>");
			w.Line("public void VerifySet() => VerifySet(global::KnockOff.Called.AtLeastOnce);");
			w.Line();

			w.Line("/// <summary>Verifies setter access count satisfies the Called constraint. Throws VerificationException if not.</summary>");
			w.Line("public void VerifySet(global::KnockOff.Called times)");
			using (w.Braces())
			{
				w.Line("if (!times.Validate(TotalSetCount))");
				w.Line($"\tthrow new global::KnockOff.VerificationException(new global::KnockOff.VerificationFailure(\"{indexerName} (set)\", times, TotalSetCount));");
			}
			w.Line();
		}

		// Verifiable fluent methods
		RenderVerifiableMethods(w, models, isMulti, fullInterceptorClassName);
	}

	private static void RenderVerifiableMethods(
		CodeWriter w,
		IReadOnlyList<UnifiedIndexerInterceptorModel> models,
		bool isMulti,
		string fullInterceptorClassName)
	{
		var hasAnyGetter = models.Any(m => m.HasGetter);
		var hasAnySetter = models.Any(m => m.HasSetter);

		// Build verifiable body that marks ALL key types
		var verifiableParts = new List<string>();
		var verifiableTimesParts = new List<string>();
		foreach (var model in models)
		{
			var suffix = isMulti ? $"_{model.KeyTypeFriendlyName}" : "";
			if (model.HasGetter)
			{
				verifiableParts.Add($"_isGetVerifiable{suffix} = true; _getVerifiableTimes{suffix} = null;");
				verifiableTimesParts.Add($"_isGetVerifiable{suffix} = true; _getVerifiableTimes{suffix} = times;");
			}
			if (model.HasSetter)
			{
				verifiableParts.Add($"_isSetVerifiable{suffix} = true; _setVerifiableTimes{suffix} = null;");
				verifiableTimesParts.Add($"_isSetVerifiable{suffix} = true; _setVerifiableTimes{suffix} = times;");
			}
		}

		w.Line($"/// <summary>Marks this indexer for verification by Stub.Verify(). Returns this for fluent chaining.</summary>");
		w.Line($"public {fullInterceptorClassName} Verifiable() {{ {string.Join(" ", verifiableParts)} return this; }}");
		w.Line();

		w.Line($"/// <summary>Marks this indexer for verification by Stub.Verify() with Called constraint. Returns this for fluent chaining.</summary>");
		w.Line($"public {fullInterceptorClassName} Verifiable(global::KnockOff.Called times) {{ {string.Join(" ", verifiableTimesParts)} return this; }}");
		w.Line();
	}

	#endregion

	#region Internal Verification Support

	private static void RenderInternalVerification(
		CodeWriter w,
		IReadOnlyList<UnifiedIndexerInterceptorModel> models,
		bool isMulti,
		string fullInterceptorClassName)
	{
		var indexerName = models[0].IndexerName;
		var hasAnyGetter = models.Any(m => m.HasGetter);
		var hasAnySetter = models.Any(m => m.HasSetter);

		// IsVerifiable: check if ANY key type's get or set is verifiable (includes When chain verifiable)
		var isVerifiableParts = new List<string>();
		foreach (var model in models)
		{
			var suffix = isMulti ? $"_{model.KeyTypeFriendlyName}" : "";
			if (model.HasGetter)
			{
				isVerifiableParts.Add($"_isGetVerifiable{suffix}");
				isVerifiableParts.Add($"_whenGetVerifiable{suffix}");
			}
			if (model.HasSetter)
			{
				isVerifiableParts.Add($"_isSetVerifiable{suffix}");
				isVerifiableParts.Add($"_whenSetVerifiable{suffix}");
			}
		}
		w.Line("/// <summary>Whether this indexer was marked with Verifiable().</summary>");
		w.Line($"internal bool IsVerifiable => {string.Join(" || ", isVerifiableParts)};");
		w.Line();

		// IsConfigured: check per-key builder CONFIG state + all-keys state (using explicit loops, no LINQ)
		w.Line("/// <summary>Whether this indexer has been configured.</summary>");
		w.Line("internal bool IsConfigured { get {");
		foreach (var model in models)
		{
			var suffix = isMulti ? $"_{model.KeyTypeFriendlyName}" : "";
			var configCheck = model.HasGetter && model.HasSetter
				? "b.HasGetConfig || b.HasSetConfig"
				: model.HasGetter ? "b.HasGetConfig" : "b.HasSetConfig";
			w.Line($"foreach (var b in _perKeyBuilders{suffix}.Values) if ({configCheck}) return true;");
			if (model.HasGetter) w.Line($"if (_get{suffix} != null || (_getSequence{suffix}?.Count ?? 0) > 0) return true;");
			if (model.HasSetter) w.Line($"if (_set{suffix} != null || (_setSequence{suffix}?.Count ?? 0) > 0) return true;");
		}
		w.Line("return false; } }");
		w.Line();

		// CheckVerification
		var totalCountExpr = hasAnyGetter && hasAnySetter
			? "TotalGetCount + TotalSetCount"
			: (hasAnyGetter ? "TotalGetCount" : "TotalSetCount");

		w.Line("/// <summary>Checks verification for Stub.Verify() - only checks if marked verifiable.</summary>");
		w.Line($"internal global::KnockOff.VerificationFailure? CheckVerification()");
		using (w.Braces())
		{
			w.Line("if (!IsVerifiable) return null;");

			// Check regular (all-keys) verifiable
			// Build check that combines _isGetVerifiable and _isSetVerifiable
			var regularVerifiableParts = new List<string>();
			foreach (var model in models)
			{
				var rvSuffix = isMulti ? $"_{model.KeyTypeFriendlyName}" : "";
				if (model.HasGetter) regularVerifiableParts.Add($"_isGetVerifiable{rvSuffix}");
				if (model.HasSetter) regularVerifiableParts.Add($"_isSetVerifiable{rvSuffix}");
			}
			w.Line($"if ({string.Join(" || ", regularVerifiableParts)})");
			using (w.Braces())
			{
				w.Line($"var times = global::KnockOff.Called.AtLeastOnce;");

				// Find any verifiable times constraint
				foreach (var model in models)
				{
					var rvSuffix = isMulti ? $"_{model.KeyTypeFriendlyName}" : "";
					if (model.HasGetter)
					{
						w.Line($"if (_getVerifiableTimes{rvSuffix} != null) times = _getVerifiableTimes{rvSuffix}.Value;");
					}
					if (model.HasSetter)
					{
						w.Line($"if (_setVerifiableTimes{rvSuffix} != null) times = _setVerifiableTimes{rvSuffix}.Value;");
					}
				}

				w.Line($"var totalCount = {totalCountExpr};");
				w.Line($"if (!times.Validate(totalCount)) return new global::KnockOff.VerificationFailure(\"{indexerName}\", times, totalCount);");
			}

			// Check When chain verifiable (getter and setter chains)
			foreach (var model in models)
			{
				var wcSuffix = isMulti ? $"_{model.KeyTypeFriendlyName}" : "";
				var friendlyName = isMulti ? model.KeyTypeFriendlyName : "";

				if (model.HasGetter)
				{
					w.Line($"if (_whenGetVerifiable{wcSuffix} && _whenGetChain{wcSuffix} != null && _whenGetChain{wcSuffix}.Count > 0)");
					using (w.Braces())
					{
						w.Line($"var head = _whenGetChainHead{wcSuffix};");
						w.Line($"var count = _whenGetChain{wcSuffix}.Count;");
						w.Line($"if (head < count && !_whenGetChain{wcSuffix}[head].IsTerminal && _whenGetChain{wcSuffix}[head].CallCount == 0)");
						using (w.Braces())
						{
							w.Line($"return global::KnockOff.VerificationFailure.SequenceIncomplete(\"indexer getter When chain\", count, head);");
						}
					}
				}
				if (model.HasSetter)
				{
					w.Line($"if (_whenSetVerifiable{wcSuffix} && _whenSetChain{wcSuffix} != null && _whenSetChain{wcSuffix}.Count > 0)");
					using (w.Braces())
					{
						w.Line($"var head = _whenSetChainHead{wcSuffix};");
						w.Line($"var count = _whenSetChain{wcSuffix}.Count;");
						w.Line($"if (head < count && !_whenSetChain{wcSuffix}[head].IsTerminal && _whenSetChain{wcSuffix}[head].CallCount == 0)");
						using (w.Braces())
						{
							w.Line($"return global::KnockOff.VerificationFailure.SequenceIncomplete(\"indexer setter When chain\", count, head);");
						}
					}
				}
			}

			w.Line("return null;");
		}
		w.Line();

		// CheckVerificationAll
		w.Line("/// <summary>Checks verification for Stub.VerifyAll() - checks if configured.</summary>");
		w.Line($"internal global::KnockOff.VerificationFailure? CheckVerificationAll()");
		using (w.Braces())
		{
			w.Line("if (!IsConfigured) return null;");
			w.Line($"var totalCount = {totalCountExpr};");
			w.Line($"return totalCount >= 1 ? null : new global::KnockOff.VerificationFailure(\"{indexerName}\", global::KnockOff.Called.AtLeastOnce, totalCount);");
		}
		w.Line();
	}

	#endregion

	#region Nested PerKeyBuilder

	private static void RenderPerKeyBuilder(
		CodeWriter w,
		UnifiedIndexerInterceptorModel model,
		string builderName,
		string sequenceName)
	{
		w.Line($"/// <summary>Per-key builder for configuring behavior for a specific key.</summary>");
		w.Line($"public sealed class {builderName}");
		using (w.Braces())
		{
			// Internal state for getter configuration
			if (model.HasGetter)
			{
				// Use ValueType directly with default!. _hasValue tracks whether a value was configured.
				// Cannot use ValueType? because int? breaks List<int>.Add(_getValue!).
				// Cannot use NullableValueType because nullable-ref types like User? would not need _hasValue.
				w.Line($"private {model.ValueType} _getValue = default!;");
				w.Line("private bool _hasValue;");
				w.Line($"private global::System.Func<{model.ValueType}>? _getCallback;");
				// internal so PerKeySequence (sibling class) can access for ThenReturns
				w.Line($"internal global::System.Collections.Generic.List<{model.ValueType}>? _getSequence;");
				w.Line("private int _getSequenceIndex;");
				w.Line();
			}

			// Internal state for setter configuration
			if (model.HasSetter)
			{
				w.Line($"private global::System.Action<{model.ValueType}>? _setCallback;");
				w.Line();
			}

			// Call count tracking (for TotalGetCount/TotalSetCount)
			if (model.HasGetter)
			{
				w.Line("internal int _getCallCount;");
			}
			if (model.HasSetter)
			{
				w.Line("internal int _setCallCount;");
			}
			w.Line();

			// HasGetConfig / HasSetConfig
			if (model.HasGetter)
			{
				w.Line("/// <summary>Whether getter configuration has been set (Returns, Get, or sequence).</summary>");
				w.Line("internal bool HasGetConfig => _hasValue || _getCallback != null || _getSequence != null;");
			}
			if (model.HasSetter)
			{
				w.Line("/// <summary>Whether setter configuration has been set (Set).</summary>");
				w.Line("internal bool HasSetConfig => _setCallback != null;");
			}
			w.Line();

			// Returns(TValue) - only if has getter
			if (model.HasGetter)
			{
				w.Line($"/// <summary>Configures this key to return the specified value.</summary>");
				w.Line($"public {builderName} Returns({model.ValueType} value)");
				using (w.Braces())
				{
					w.Line("_getValue = value;");
					w.Line("_hasValue = true;");
					w.Line("_getCallback = null;");
					w.Line("_getSequence = null;");
					w.Line("_getSequenceIndex = 0;");
					w.Line("return this;");
				}
				w.Line();

				// ThenReturns(TValue) - elevates to per-key sequence
				w.Line($"/// <summary>Adds another value to the per-key return sequence.</summary>");
				w.Line($"public {sequenceName} ThenReturns({model.ValueType} value)");
				using (w.Braces())
				{
					w.Line("if (_getSequence == null)");
					using (w.Braces())
					{
						w.Line($"_getSequence = new global::System.Collections.Generic.List<{model.ValueType}>();");
						w.Line("if (_hasValue) _getSequence.Add(_getValue!);");
					}
					w.Line("_getSequence.Add(value);");
					w.Line("_hasValue = false;");
					w.Line("_getCallback = null;");
					w.Line($"return new {sequenceName}(this);");
				}
				w.Line();

				// Get(Func<TValue>) - per-key callback (no key param since key is already bound)
				w.Line($"/// <summary>Configures this key to use the specified callback for getter.</summary>");
				w.Line($"public {builderName} Get(global::System.Func<{model.ValueType}> callback)");
				using (w.Braces())
				{
					w.Line("_getCallback = callback;");
					w.Line("_hasValue = false;");
					w.Line("_getValue = default!;");
					w.Line("_getSequence = null;");
					w.Line("_getSequenceIndex = 0;");
					w.Line("return this;");
				}
				w.Line();
			}

			// Set(Action<TValue>) - only if has setter
			if (model.HasSetter)
			{
				w.Line($"/// <summary>Configures this key to use the specified callback for setter.</summary>");
				w.Line($"public {builderName} Set(global::System.Action<{model.ValueType}> callback)");
				using (w.Braces())
				{
					w.Line("_setCallback = callback;");
					w.Line("return this;");
				}
				w.Line();
			}

			// InvokeGet() - called by interceptor's InvokeGet
			if (model.HasGetter)
			{
				w.Line($"/// <summary>Invokes the configured getter for this key.</summary>");
				w.Line($"internal {model.ValueType} InvokeGet()");
				using (w.Braces())
				{
					// Sequence first
					w.Line("if (_getSequence != null)");
					using (w.Braces())
					{
						w.Line("if (_getSequenceIndex < _getSequence.Count)");
						using (w.Braces())
						{
							w.Line("return _getSequence[_getSequenceIndex++];");
						}
						w.Line("// Repeat last value");
						w.Line("return _getSequence[_getSequence.Count - 1];");
					}
					// Callback
					w.Line("if (_getCallback != null) return _getCallback();");
					// Value
					w.Line("return _getValue!;");
				}
				w.Line();
			}

			// InvokeSet(TValue) - called by interceptor's InvokeSet
			if (model.HasSetter)
			{
				w.Line($"/// <summary>Invokes the configured setter for this key.</summary>");
				w.Line($"internal void InvokeSet({model.ValueType} value)");
				using (w.Braces())
				{
					w.Line("_setCallback?.Invoke(value);");
				}
				w.Line();
			}

			// Per-key verification methods
			if (model.HasGetter)
			{
				w.Line("/// <summary>Verifies this key's getter was called at least once. Throws VerificationException if not.</summary>");
				w.Line("public void VerifyGet() => VerifyGet(global::KnockOff.Called.AtLeastOnce);");
				w.Line();
				w.Line("/// <summary>Verifies this key's getter call count satisfies the Called constraint. Throws VerificationException if not.</summary>");
				w.Line("public void VerifyGet(global::KnockOff.Called times)");
				using (w.Braces())
				{
					w.Line("if (!times.Validate(_getCallCount))");
					w.Line("\tthrow new global::KnockOff.VerificationException(new global::KnockOff.VerificationFailure(\"indexer getter (per-key)\", times, _getCallCount));");
				}
				w.Line();
			}

			if (model.HasSetter)
			{
				w.Line("/// <summary>Verifies this key's setter was called at least once. Throws VerificationException if not.</summary>");
				w.Line("public void VerifySet() => VerifySet(global::KnockOff.Called.AtLeastOnce);");
				w.Line();
				w.Line("/// <summary>Verifies this key's setter call count satisfies the Called constraint. Throws VerificationException if not.</summary>");
				w.Line("public void VerifySet(global::KnockOff.Called times)");
				using (w.Braces())
				{
					w.Line("if (!times.Validate(_setCallCount))");
					w.Line("\tthrow new global::KnockOff.VerificationException(new global::KnockOff.VerificationFailure(\"indexer setter (per-key)\", times, _setCallCount));");
				}
				w.Line();
			}

			// Reset
			w.Line("/// <summary>Resets call counts for this per-key builder.</summary>");
			w.Line("internal void Reset()");
			using (w.Braces())
			{
				if (model.HasGetter)
				{
					w.Line("_getCallCount = 0;");
					w.Line("_getSequenceIndex = 0;");
				}
				if (model.HasSetter)
				{
					w.Line("_setCallCount = 0;");
				}
			}
		}
		w.Line();
	}

	#endregion

	#region Nested PerKeySequence

	private static void RenderPerKeySequence(
		CodeWriter w,
		UnifiedIndexerInterceptorModel model,
		string builderName,
		string sequenceName)
	{
		if (!model.HasGetter) return;

		w.Line($"/// <summary>Per-key sequence for chaining multiple return values.</summary>");
		w.Line($"public sealed class {sequenceName}");
		using (w.Braces())
		{
			w.Line($"private readonly {builderName} _builder;");
			w.Line();

			w.Line($"internal {sequenceName}({builderName} builder) => _builder = builder;");
			w.Line();

			w.Line($"/// <summary>Adds another value to the per-key return sequence.</summary>");
			w.Line($"public {sequenceName} ThenReturns({model.ValueType} value)");
			using (w.Braces())
			{
				w.Line("_builder._getSequence!.Add(value);");
				w.Line("return this;");
			}
		}
		w.Line();
	}

	#endregion

	#region Nested IndexerGetBuilderImpl

	private static void RenderIndexerGetBuilderImpl(
		CodeWriter w,
		UnifiedIndexerInterceptorModel model,
		string interceptorClassName,
		string friendlyName,
		string fieldSuffix)
	{
		var className = $"IndexerGetBuilderImpl{friendlyName}";
		var keyType = model.KeyType;
		var valueType = model.ValueType;
		var parameterTypes = model.KeyType;

		w.Line($"/// <summary>Builder for getter callback registration. Supports tracking and lazy elevation to sequence.</summary>");
		w.Line($"private sealed class {className} : global::KnockOff.IIndexerGetBuilder<{keyType}, {valueType}>");
		using (w.Braces())
		{
			w.Line($"private readonly {interceptorClassName} _interceptor;");
			w.Line();

			w.Line($"public {className}({interceptorClassName} interceptor) => _interceptor = interceptor;");
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
				w.Line($"if (_interceptor._getSequence{fieldSuffix} == null)");
				using (w.Braces())
				{
					w.Line($"_interceptor._getSequence{fieldSuffix} = new global::System.Collections.Generic.List<(global::System.Func<{parameterTypes}, {valueType}> Callback, {className} Tracking)>();");
					w.Line($"_interceptor._getSequence{fieldSuffix}.Add((_interceptor._get{fieldSuffix}!, this));");
					w.Line($"_interceptor._get{fieldSuffix} = null;");
					w.Line($"_interceptor._getTracking{fieldSuffix} = null;");
					w.Line($"_interceptor._getSequenceIndex{fieldSuffix} = 0;");
				}
				w.Line($"var nextBuilder = new {className}(_interceptor);");
				w.Line($"_interceptor._getSequence{fieldSuffix}.Add((callback, nextBuilder));");
				w.Line($"return new IndexerGetSequenceImpl{friendlyName}(_interceptor);");
			}
			w.Line();

			w.Line("/// <summary>Marks for verification by Stub.Verify(). Returns this for fluent chaining.</summary>");
			w.Line($"public global::KnockOff.IIndexerGetBuilder<{keyType}, {valueType}> Verifiable()");
			using (w.Braces())
			{
				w.Line($"_interceptor._isGetVerifiable{fieldSuffix} = true;");
				w.Line($"_interceptor._getVerifiableTimes{fieldSuffix} = null;");
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
		UnifiedIndexerInterceptorModel model,
		string interceptorClassName,
		string friendlyName,
		string fieldSuffix)
	{
		var className = $"IndexerSetBuilderImpl{friendlyName}";
		var keyType = model.KeyType;
		var valueType = model.ValueType;
		var parameterTypes = model.KeyType;

		w.Line($"/// <summary>Builder for setter callback registration. Supports tracking and lazy elevation to sequence.</summary>");
		w.Line($"private sealed class {className} : global::KnockOff.IIndexerSetBuilder<{keyType}, {valueType}>");
		using (w.Braces())
		{
			w.Line($"private readonly {interceptorClassName} _interceptor;");
			w.Line();

			w.Line($"public {className}({interceptorClassName} interceptor) => _interceptor = interceptor;");
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
				w.Line($"if (_interceptor._setSequence{fieldSuffix} == null)");
				using (w.Braces())
				{
					w.Line($"_interceptor._setSequence{fieldSuffix} = new global::System.Collections.Generic.List<(global::System.Action<{parameterTypes}, {valueType}> Callback, {className} Tracking)>();");
					w.Line($"_interceptor._setSequence{fieldSuffix}.Add((_interceptor._set{fieldSuffix}!, this));");
					w.Line($"_interceptor._set{fieldSuffix} = null;");
					w.Line($"_interceptor._setTracking{fieldSuffix} = null;");
					w.Line($"_interceptor._setSequenceIndex{fieldSuffix} = 0;");
				}
				w.Line($"var nextBuilder = new {className}(_interceptor);");
				w.Line($"_interceptor._setSequence{fieldSuffix}.Add((callback, nextBuilder));");
				w.Line($"return new IndexerSetSequenceImpl{friendlyName}(_interceptor);");
			}
			w.Line();

			w.Line("/// <summary>Marks for verification by Stub.Verify(). Returns this for fluent chaining.</summary>");
			w.Line($"public global::KnockOff.IIndexerSetBuilder<{keyType}, {valueType}> Verifiable()");
			using (w.Braces())
			{
				w.Line($"_interceptor._isSetVerifiable{fieldSuffix} = true;");
				w.Line($"_interceptor._setVerifiableTimes{fieldSuffix} = null;");
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
		UnifiedIndexerInterceptorModel model,
		string interceptorClassName,
		string friendlyName,
		string fieldSuffix)
	{
		var className = $"IndexerGetSequenceImpl{friendlyName}";
		var builderClassName = $"IndexerGetBuilderImpl{friendlyName}";
		var keyType = model.KeyType;
		var valueType = model.ValueType;
		var parameterTypes = model.KeyType;

		w.Line($"/// <summary>Sequence implementation for ThenGet chaining.</summary>");
		w.Line($"private sealed class {className} : global::KnockOff.IIndexerGetSequence<{keyType}, {valueType}>");
		using (w.Braces())
		{
			w.Line($"private readonly {interceptorClassName} _interceptor;");
			w.Line();

			w.Line($"public {className}({interceptorClassName} interceptor) => _interceptor = interceptor;");
			w.Line();

			w.Line($"/// <summary>Adds another getter callback to the sequence. Each callback runs exactly once.</summary>");
			w.Line($"public global::KnockOff.IIndexerGetSequence<{keyType}, {valueType}> ThenGet(global::System.Func<{parameterTypes}, {valueType}> callback)");
			using (w.Braces())
			{
				w.Line($"var tracking = new {builderClassName}(_interceptor);");
				w.Line($"_interceptor._getSequence{fieldSuffix}!.Add((callback, tracking));");
				w.Line("return this;");
			}
			w.Line();

			w.Line("/// <summary>Verifies the entire sequence was executed (all callbacks invoked). Throws VerificationException if incomplete.</summary>");
			w.Line("public void Verify()");
			using (w.Braces())
			{
				w.Line($"if (_interceptor._getSequence{fieldSuffix} == null) return;");
				w.Line($"var sequenceLength = _interceptor._getSequence{fieldSuffix}.Count;");
				w.Line($"var completedCount = _interceptor._getSequenceIndex{fieldSuffix};");
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
				w.Line($"_interceptor._isGetVerifiable{fieldSuffix} = true;");
				w.Line($"_interceptor._getVerifiableTimes{fieldSuffix} = null;");
				w.Line("return this;");
			}
			w.Line();

			// ThenDefault() - terminates sequence with default(T) after exhaustion
			w.Line("/// <summary>Terminates sequence with default(T) after exhaustion instead of repeating last value.</summary>");
			w.Line("public void ThenDefault()");
			using (w.Braces())
			{
				w.Line($"_interceptor._getRepeatLastValue{fieldSuffix} = false;");
			}
		}
		w.Line();
	}

	#endregion

	#region Nested IndexerSetSequenceImpl

	private static void RenderIndexerSetSequenceImpl(
		CodeWriter w,
		UnifiedIndexerInterceptorModel model,
		string interceptorClassName,
		string friendlyName,
		string fieldSuffix)
	{
		var className = $"IndexerSetSequenceImpl{friendlyName}";
		var builderClassName = $"IndexerSetBuilderImpl{friendlyName}";
		var keyType = model.KeyType;
		var valueType = model.ValueType;
		var parameterTypes = model.KeyType;

		w.Line($"/// <summary>Sequence implementation for ThenSet chaining.</summary>");
		w.Line($"private sealed class {className} : global::KnockOff.IIndexerSetSequence<{keyType}, {valueType}>");
		using (w.Braces())
		{
			w.Line($"private readonly {interceptorClassName} _interceptor;");
			w.Line();

			w.Line($"public {className}({interceptorClassName} interceptor) => _interceptor = interceptor;");
			w.Line();

			w.Line($"/// <summary>Adds another setter callback to the sequence. Each callback runs exactly once.</summary>");
			w.Line($"public global::KnockOff.IIndexerSetSequence<{keyType}, {valueType}> ThenSet(global::System.Action<{parameterTypes}, {valueType}> callback)");
			using (w.Braces())
			{
				w.Line($"var tracking = new {builderClassName}(_interceptor);");
				w.Line($"_interceptor._setSequence{fieldSuffix}!.Add((callback, tracking));");
				w.Line("return this;");
			}
			w.Line();

			w.Line("/// <summary>Verifies the entire sequence was executed (all callbacks invoked). Throws VerificationException if incomplete.</summary>");
			w.Line("public void Verify()");
			using (w.Braces())
			{
				w.Line($"if (_interceptor._setSequence{fieldSuffix} == null) return;");
				w.Line($"var sequenceLength = _interceptor._setSequence{fieldSuffix}.Count;");
				w.Line($"var completedCount = _interceptor._setSequenceIndex{fieldSuffix};");
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
				w.Line($"_interceptor._isSetVerifiable{fieldSuffix} = true;");
				w.Line($"_interceptor._setVerifiableTimes{fieldSuffix} = null;");
				w.Line("return this;");
			}
			w.Line();

			// ThenDefault()
			w.Line("/// <summary>Terminates sequence after exhaustion instead of repeating last callback.</summary>");
			w.Line("public void ThenDefault()");
			using (w.Braces())
			{
				w.Line($"_interceptor._setRepeatLastValue{fieldSuffix} = false;");
			}
		}
		w.Line();
	}

	#endregion

	#region Nested IndexerGetWhenMatcher Classes

	private static void RenderIndexerGetWhenMatcherClasses(
		CodeWriter w,
		UnifiedIndexerInterceptorModel model,
		string friendlyName)
	{
		var keyType = model.KeyType;
		var valueType = model.ValueType;

		// Abstract base
		w.Line($"/// <summary>Abstract base for indexer getter When chain matchers.</summary>");
		w.Line($"private abstract class IndexerGetWhenMatcher{friendlyName}");
		using (w.Braces())
		{
			w.Line($"public abstract bool Matches({keyType} key);");
			w.Line($"public abstract {valueType} Call({keyType} key);");
			w.Line("public abstract bool IsTerminal { get; }");
			w.Line("public int CallCount { get; set; }");
		}
		w.Line();

		// Value matcher - predicate + stored value
		w.Line($"/// <summary>Getter matcher that uses a predicate and returns a stored value.</summary>");
		w.Line($"private sealed class IndexerGetWhenMatcherValue{friendlyName} : IndexerGetWhenMatcher{friendlyName}");
		using (w.Braces())
		{
			w.Line($"private readonly global::System.Func<{keyType}, bool> _predicate;");
			w.Line($"private readonly {valueType} _value;");
			w.Line();
			w.Line($"public IndexerGetWhenMatcherValue{friendlyName}(global::System.Func<{keyType}, bool> predicate, {valueType} value)");
			using (w.Braces())
			{
				w.Line("_predicate = predicate;");
				w.Line("_value = value;");
			}
			w.Line();
			w.Line($"public override bool Matches({keyType} key) => _predicate(key);");
			w.Line($"public override {valueType} Call({keyType} key) => _value;");
			w.Line("public override bool IsTerminal => false;");
		}
		w.Line();

		// Callback matcher - predicate + callback
		w.Line($"/// <summary>Getter matcher that uses a predicate and invokes a callback.</summary>");
		w.Line($"private sealed class IndexerGetWhenMatcherCallback{friendlyName} : IndexerGetWhenMatcher{friendlyName}");
		using (w.Braces())
		{
			w.Line($"private readonly global::System.Func<{keyType}, bool> _predicate;");
			w.Line($"private readonly global::System.Func<{keyType}, {valueType}> _callback;");
			w.Line();
			w.Line($"public IndexerGetWhenMatcherCallback{friendlyName}(global::System.Func<{keyType}, bool> predicate, global::System.Func<{keyType}, {valueType}> callback)");
			using (w.Braces())
			{
				w.Line("_predicate = predicate;");
				w.Line("_callback = callback;");
			}
			w.Line();
			w.Line($"public override bool Matches({keyType} key) => _predicate(key);");
			w.Line($"public override {valueType} Call({keyType} key) => _callback(key);");
			w.Line("public override bool IsTerminal => false;");
		}
		w.Line();

		// None matcher - never matches, terminal
		w.Line($"/// <summary>Getter matcher that never matches. Used to close chain without fallback. Terminal.</summary>");
		w.Line($"private sealed class IndexerGetWhenMatcherNone{friendlyName} : IndexerGetWhenMatcher{friendlyName}");
		using (w.Braces())
		{
			w.Line($"public override bool Matches({keyType} key) => false;");
			w.Line($"public override {valueType} Call({keyType} key) => default!;");
			w.Line("public override bool IsTerminal => true;");
		}
		w.Line();
	}

	#endregion

	#region Nested IndexerSetWhenMatcher Classes

	private static void RenderIndexerSetWhenMatcherClasses(
		CodeWriter w,
		UnifiedIndexerInterceptorModel model,
		string friendlyName)
	{
		var keyType = model.KeyType;
		var valueType = model.ValueType;

		// Abstract base
		w.Line($"/// <summary>Abstract base for indexer setter When chain matchers.</summary>");
		w.Line($"private abstract class IndexerSetWhenMatcher{friendlyName}");
		using (w.Braces())
		{
			w.Line($"public abstract bool Matches({keyType} key);");
			w.Line($"public abstract void Call({keyType} key, {valueType} value);");
			w.Line("public abstract bool IsTerminal { get; }");
			w.Line("public int CallCount { get; set; }");
		}
		w.Line();

		// Callback matcher - predicate + callback
		w.Line($"/// <summary>Setter matcher that uses a predicate and invokes a callback.</summary>");
		w.Line($"private sealed class IndexerSetWhenMatcherCallback{friendlyName} : IndexerSetWhenMatcher{friendlyName}");
		using (w.Braces())
		{
			w.Line($"private readonly global::System.Func<{keyType}, bool> _predicate;");
			w.Line($"private readonly global::System.Action<{keyType}, {valueType}> _callback;");
			w.Line();
			w.Line($"public IndexerSetWhenMatcherCallback{friendlyName}(global::System.Func<{keyType}, bool> predicate, global::System.Action<{keyType}, {valueType}> callback)");
			using (w.Braces())
			{
				w.Line("_predicate = predicate;");
				w.Line("_callback = callback;");
			}
			w.Line();
			w.Line($"public override bool Matches({keyType} key) => _predicate(key);");
			w.Line($"public override void Call({keyType} key, {valueType} value) => _callback(key, value);");
			w.Line("public override bool IsTerminal => false;");
		}
		w.Line();

		// None matcher - never matches, terminal
		w.Line($"/// <summary>Setter matcher that never matches. Used to close chain without fallback. Terminal.</summary>");
		w.Line($"private sealed class IndexerSetWhenMatcherNone{friendlyName} : IndexerSetWhenMatcher{friendlyName}");
		using (w.Braces())
		{
			w.Line($"public override bool Matches({keyType} key) => false;");
			w.Line($"public override void Call({keyType} key, {valueType} value) {{ }}");
			w.Line("public override bool IsTerminal => true;");
		}
		w.Line();
	}

	#endregion

	#region Nested IndexerWhenBuilder

	private static void RenderIndexerWhenBuilder(
		CodeWriter w,
		UnifiedIndexerInterceptorModel model,
		string interceptorClassName,
		string friendlyName,
		string fieldSuffix)
	{
		var keyType = model.KeyType;
		var valueType = model.ValueType;
		var builderName = $"IndexerWhenBuilder{friendlyName}";

		w.Line($"/// <summary>Builder for indexer When matchers. Captures predicate, routes Returns()/Get() to getter chain and Set() to setter chain.</summary>");
		w.Line($"public sealed class {builderName}");
		using (w.Braces())
		{
			w.Line($"private readonly {interceptorClassName} _interceptor;");
			w.Line($"private readonly global::System.Func<{keyType}, bool> _predicate;");
			w.Line();

			w.Line($"internal {builderName}({interceptorClassName} interceptor, global::System.Func<{keyType}, bool> predicate)");
			using (w.Braces())
			{
				w.Line("_interceptor = interceptor;");
				w.Line("_predicate = predicate;");
			}
			w.Line();

			// Returns(value) - routes to getter chain
			if (model.HasGetter)
			{
				w.Line($"/// <summary>Configures the return value when predicate matches (getter chain). Returns chain for ThenWhen/ThenNone.</summary>");
				w.Line($"public IndexerGetWhenChain{friendlyName} Returns({valueType} value)");
				using (w.Braces())
				{
					w.Line($"_interceptor._whenGetChain{fieldSuffix} ??= new global::System.Collections.Generic.List<IndexerGetWhenMatcher{friendlyName}>();");
					w.Line($"_interceptor._whenGetChain{fieldSuffix}.Add(new IndexerGetWhenMatcherValue{friendlyName}(_predicate, value));");
					w.Line($"return new IndexerGetWhenChain{friendlyName}(_interceptor);");
				}
				w.Line();

				// Get(callback) - routes to getter chain with callback
				w.Line($"/// <summary>Configures a callback when predicate matches (getter chain). Returns chain for ThenWhen/ThenNone.</summary>");
				w.Line($"public IndexerGetWhenChain{friendlyName} Get(global::System.Func<{keyType}, {valueType}> callback)");
				using (w.Braces())
				{
					w.Line($"_interceptor._whenGetChain{fieldSuffix} ??= new global::System.Collections.Generic.List<IndexerGetWhenMatcher{friendlyName}>();");
					w.Line($"_interceptor._whenGetChain{fieldSuffix}.Add(new IndexerGetWhenMatcherCallback{friendlyName}(_predicate, callback));");
					w.Line($"return new IndexerGetWhenChain{friendlyName}(_interceptor);");
				}
				w.Line();
			}

			// Set(callback) - routes to setter chain
			if (model.HasSetter)
			{
				w.Line($"/// <summary>Configures a callback when predicate matches (setter chain). Returns chain for ThenWhen/ThenNone.</summary>");
				w.Line($"public IndexerSetWhenChain{friendlyName} Set(global::System.Action<{keyType}, {valueType}> callback)");
				using (w.Braces())
				{
					w.Line($"_interceptor._whenSetChain{fieldSuffix} ??= new global::System.Collections.Generic.List<IndexerSetWhenMatcher{friendlyName}>();");
					w.Line($"_interceptor._whenSetChain{fieldSuffix}.Add(new IndexerSetWhenMatcherCallback{friendlyName}(_predicate, callback));");
					w.Line($"return new IndexerSetWhenChain{friendlyName}(_interceptor);");
				}
				w.Line();
			}
		}
		w.Line();
	}

	#endregion

	#region Nested IndexerGetWhenChain

	private static void RenderIndexerGetWhenChain(
		CodeWriter w,
		UnifiedIndexerInterceptorModel model,
		string interceptorClassName,
		string friendlyName,
		string fieldSuffix)
	{
		if (!model.HasGetter) return;

		var keyType = model.KeyType;
		var valueType = model.ValueType;
		var chainName = $"IndexerGetWhenChain{friendlyName}";
		var builderName = $"IndexerWhenBuilder{friendlyName}";

		w.Line($"/// <summary>Getter When chain implementation with ThenWhen, ThenNone, Verify, Reset.</summary>");
		w.Line($"public sealed class {chainName}");
		using (w.Braces())
		{
			w.Line($"private readonly {interceptorClassName} _interceptor;");
			w.Line();

			w.Line($"internal {chainName}({interceptorClassName} interceptor) => _interceptor = interceptor;");
			w.Line();

			// ThenWhen(predicate) - returns builder for next matcher
			w.Line($"/// <summary>Adds another predicate matcher to the getter When chain.</summary>");
			w.Line($"public {builderName} ThenWhen(global::System.Func<{keyType}, bool> predicate)");
			using (w.Braces())
			{
				w.Line($"return new {builderName}(_interceptor, predicate);");
			}
			w.Line();

			// ThenNone() - terminal, never matches
			w.Line($"/// <summary>Closes getter chain with no matcher. Falls through when exhausted.</summary>");
			w.Line("public void ThenNone()");
			using (w.Braces())
			{
				w.Line($"_interceptor._whenGetChain{fieldSuffix} ??= new global::System.Collections.Generic.List<IndexerGetWhenMatcher{friendlyName}>();");
				w.Line($"_interceptor._whenGetChain{fieldSuffix}.Add(new IndexerGetWhenMatcherNone{friendlyName}());");
			}
			w.Line();

			// Verify() - checks if chain was consumed
			w.Line($"/// <summary>Verifies the getter When chain was fully consumed.</summary>");
			w.Line("public void Verify()");
			using (w.Braces())
			{
				w.Line($"if (_interceptor._whenGetChain{fieldSuffix} == null || _interceptor._whenGetChain{fieldSuffix}.Count == 0) return;");
				w.Line($"var head = _interceptor._whenGetChainHead{fieldSuffix};");
				w.Line($"var count = _interceptor._whenGetChain{fieldSuffix}.Count;");
				w.Line($"if (head < count && !_interceptor._whenGetChain{fieldSuffix}[head].IsTerminal && _interceptor._whenGetChain{fieldSuffix}[head].CallCount == 0)");
				using (w.Braces())
				{
					w.Line("throw new global::KnockOff.VerificationException(global::KnockOff.VerificationFailure.SequenceIncomplete(\"indexer getter When chain\", count, head));");
				}
			}
			w.Line();

			// Reset() - resets HEAD and all matcher CallCounts
			w.Line($"/// <summary>Resets getter When chain HEAD and all matcher call counts.</summary>");
			w.Line("public void Reset()");
			using (w.Braces())
			{
				w.Line($"_interceptor._whenGetChainHead{fieldSuffix} = 0;");
				w.Line($"if (_interceptor._whenGetChain{fieldSuffix} != null)");
				using (w.Braces())
				{
					w.Line($"foreach (var matcher in _interceptor._whenGetChain{fieldSuffix})");
					w.Line("\tmatcher.CallCount = 0;");
				}
			}
			w.Line();

			// Verifiable()
			w.Line($"/// <summary>Marks this getter When chain for verification by Stub.Verify().</summary>");
			w.Line($"public {chainName} Verifiable()");
			using (w.Braces())
			{
				w.Line($"_interceptor._whenGetVerifiable{fieldSuffix} = true;");
				w.Line("return this;");
			}
		}
		w.Line();
	}

	#endregion

	#region Nested IndexerSetWhenChain

	private static void RenderIndexerSetWhenChain(
		CodeWriter w,
		UnifiedIndexerInterceptorModel model,
		string interceptorClassName,
		string friendlyName,
		string fieldSuffix)
	{
		if (!model.HasSetter) return;

		var keyType = model.KeyType;
		var valueType = model.ValueType;
		var chainName = $"IndexerSetWhenChain{friendlyName}";
		var builderName = $"IndexerWhenBuilder{friendlyName}";

		w.Line($"/// <summary>Setter When chain implementation with ThenWhen, ThenNone, Verify, Reset.</summary>");
		w.Line($"public sealed class {chainName}");
		using (w.Braces())
		{
			w.Line($"private readonly {interceptorClassName} _interceptor;");
			w.Line();

			w.Line($"internal {chainName}({interceptorClassName} interceptor) => _interceptor = interceptor;");
			w.Line();

			// ThenWhen(predicate) - returns builder for next matcher
			w.Line($"/// <summary>Adds another predicate matcher to the setter When chain.</summary>");
			w.Line($"public {builderName} ThenWhen(global::System.Func<{keyType}, bool> predicate)");
			using (w.Braces())
			{
				w.Line($"return new {builderName}(_interceptor, predicate);");
			}
			w.Line();

			// ThenNone() - terminal, never matches
			w.Line($"/// <summary>Closes setter chain with no matcher. Falls through when exhausted.</summary>");
			w.Line("public void ThenNone()");
			using (w.Braces())
			{
				w.Line($"_interceptor._whenSetChain{fieldSuffix} ??= new global::System.Collections.Generic.List<IndexerSetWhenMatcher{friendlyName}>();");
				w.Line($"_interceptor._whenSetChain{fieldSuffix}.Add(new IndexerSetWhenMatcherNone{friendlyName}());");
			}
			w.Line();

			// Verify() - checks if chain was consumed
			w.Line($"/// <summary>Verifies the setter When chain was fully consumed.</summary>");
			w.Line("public void Verify()");
			using (w.Braces())
			{
				w.Line($"if (_interceptor._whenSetChain{fieldSuffix} == null || _interceptor._whenSetChain{fieldSuffix}.Count == 0) return;");
				w.Line($"var head = _interceptor._whenSetChainHead{fieldSuffix};");
				w.Line($"var count = _interceptor._whenSetChain{fieldSuffix}.Count;");
				w.Line($"if (head < count && !_interceptor._whenSetChain{fieldSuffix}[head].IsTerminal && _interceptor._whenSetChain{fieldSuffix}[head].CallCount == 0)");
				using (w.Braces())
				{
					w.Line("throw new global::KnockOff.VerificationException(global::KnockOff.VerificationFailure.SequenceIncomplete(\"indexer setter When chain\", count, head));");
				}
			}
			w.Line();

			// Reset() - resets HEAD and all matcher CallCounts
			w.Line($"/// <summary>Resets setter When chain HEAD and all matcher call counts.</summary>");
			w.Line("public void Reset()");
			using (w.Braces())
			{
				w.Line($"_interceptor._whenSetChainHead{fieldSuffix} = 0;");
				w.Line($"if (_interceptor._whenSetChain{fieldSuffix} != null)");
				using (w.Braces())
				{
					w.Line($"foreach (var matcher in _interceptor._whenSetChain{fieldSuffix})");
					w.Line("\tmatcher.CallCount = 0;");
				}
			}
			w.Line();

			// Verifiable()
			w.Line($"/// <summary>Marks this setter When chain for verification by Stub.Verify().</summary>");
			w.Line($"public {chainName} Verifiable()");
			using (w.Braces())
			{
				w.Line($"_interceptor._whenSetVerifiable{fieldSuffix} = true;");
				w.Line("return this;");
			}
		}
		w.Line();
	}

	#endregion
}
