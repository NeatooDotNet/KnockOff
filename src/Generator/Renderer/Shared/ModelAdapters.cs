// src/Generator/Renderer/Shared/ModelAdapters.cs
#nullable enable
using System.Collections.Generic;
using System.Linq;
using KnockOff.Builder;
using KnockOff.Model.Flat;
using KnockOff.Model.Inline;
using KnockOff.Model.Shared;

namespace KnockOff.Renderer.Shared;

/// <summary>
/// Converts existing flat/inline models to unified models for shared rendering.
/// Allows incremental migration to the unified renderer without changing existing builders.
/// </summary>
internal static class ModelAdapters
{
	/// <summary>
	/// Converts a FlatMethodGroup to UnifiedMethodInterceptorModel.
	/// </summary>
	public static UnifiedMethodInterceptorModel ToUnifiedModel(FlatMethodGroup group, string className, string typeParameters)
	{
		var methods = group.Methods.GetArray() ?? System.Array.Empty<FlatMethodModel>();

		if (methods.Length == 0)
			throw new System.ArgumentException("Method group must have at least one method", nameof(group));

		// Convert FlatMethodModels to MethodSignatureInfos
		var signatures = methods.Select(m => new MethodSignatureInfo(
			Parameters: m.Parameters,
			TrackableParameters: m.TrackableParameters,
			ParameterDeclarations: m.ParameterDeclarations,
			ReturnType: m.ReturnType,
			IsVoid: m.IsVoid,
			HasRefOrOutParams: m.Parameters.Any(p => p.RefKind == Microsoft.CodeAnalysis.RefKind.Ref || p.RefKind == Microsoft.CodeAnalysis.RefKind.Out),
			DefaultExpression: m.DefaultExpression,
			ThrowsOnDefault: m.ThrowsOnDefault,
			ReturnsByRef: m.ReturnsByRef,
			ReturnsByRefReadonly: m.ReturnsByRefReadonly,
			HasRefStructParameter: m.Parameters.Any(p => p.IsRefStruct) || m.IsRefStructReturn)).ToList();

		// Get unique signatures
		var uniqueSignatures = GetUniqueSignatures(signatures);

		if (uniqueSignatures.Count == 1)
		{
			// Single-signature case
			var first = methods[0];
			return BuildSingleSignatureModel(group, first, className, typeParameters);
		}
		else
		{
			// Multi-overload case
			var first = methods[0];
			return BuildMultiOverloadModel(group, methods, uniqueSignatures, className, typeParameters);
		}
	}

	private static UnifiedMethodInterceptorModel BuildSingleSignatureModel(
		FlatMethodGroup group,
		FlatMethodModel first,
		string className,
		string typeParameters)
	{
		// Recompute delegate types using UnifiedInterceptorBuilder for consistency.
		var hasRefOrOut = first.Parameters.Any(p => p.RefKind == Microsoft.CodeAnalysis.RefKind.Ref || p.RefKind == Microsoft.CodeAnalysis.RefKind.Out);
		var hasRefStruct = first.Parameters.Any(p => p.IsRefStruct) || first.IsRefStructReturn;
		var sig = new MethodSignatureInfo(
			Parameters: first.Parameters,
			TrackableParameters: first.TrackableParameters,
			ParameterDeclarations: first.ParameterDeclarations,
			ReturnType: first.ReturnType,
			IsVoid: first.IsVoid,
			HasRefOrOutParams: hasRefOrOut,
			DefaultExpression: first.DefaultExpression,
			ThrowsOnDefault: first.ThrowsOnDefault,
			ReturnsByRef: first.ReturnsByRef,
			ReturnsByRefReadonly: first.ReturnsByRefReadonly,
			XmlDocSummary: first.XmlDocSummary,
			HasRefStructParameter: hasRefStruct);

		var callDelegateType = UnifiedInterceptorBuilder.BuildCallDelegateType(first.MethodName, sig, className, typeParameters);
		var customDelegateSignature = UnifiedInterceptorBuilder.BuildCustomDelegateSignature(first.MethodName, sig, className, typeParameters);

		// Get delegate type without nullable marker for builder interface
		var delegateTypeForBuilder = callDelegateType.TrimEnd('?');

		// Friendly names for single-signature (no overload suffix)
		var delegateFriendlyName = $"{first.MethodName}Delegate";
		var builderFriendlyName = $"{first.MethodName}Impl";
		var sequenceFriendlyName = $"{first.MethodName}Sequence";

		// When any parameter is a ref struct, disable args tracking entirely
		var effectiveTrackableParams = hasRefStruct
			? EquatableArray<ParameterModel>.Empty
			: first.TrackableParameters;
		var effectiveLastCallType = hasRefStruct ? null : first.LastCallType;

		// Predicate delegate: only for 2+ trackable params
		string? predicateFriendlyName = null;
		string? predicateDelegateSignature = null;
		if (effectiveTrackableParams.Count >= 2)
		{
			predicateFriendlyName = $"{first.MethodName}Predicate";
			var predicateParamList = BuildDelegateParamList(effectiveTrackableParams);
			predicateDelegateSignature = $"public delegate bool {predicateFriendlyName}({predicateParamList});";
		}

		return new UnifiedMethodInterceptorModel(
			InterceptorClassName: group.InterceptorClassName,
			MethodName: first.MethodName,
			DeclaringInterface: first.DeclaringInterface,
			OwnerClassName: className,
			OwnerTypeParameters: typeParameters,
			Parameters: first.Parameters,
			TrackableParameters: effectiveTrackableParams,
			ParameterDeclarations: first.ParameterDeclarations,
			ReturnType: first.ReturnType,
			IsVoid: first.IsVoid,
			CallDelegateType: callDelegateType,
			NeedsCustomDelegate: true,
			CustomDelegateSignature: customDelegateSignature,
			LastArgType: GetLastArgType(effectiveTrackableParams),
			LastArgsType: GetLastArgsType(effectiveTrackableParams, effectiveLastCallType),
			BuilderInterface: GetBuilderInterface(effectiveTrackableParams, effectiveLastCallType, delegateTypeForBuilder, first.IsVoid),
			DefaultExpression: first.DefaultExpression,
			ThrowsOnDefault: first.ThrowsOnDefault,
			// Stub override name: if HasStubOverride, the stub override name is MethodName + "_"
			StubOverrideName: first.HasStubOverride ? $"{first.MethodName}_" : null,
			Overloads: EquatableArray<MethodOverloadSignature>.Empty,
			ReturnsByRef: first.ReturnsByRef,
			ReturnsByRefReadonly: first.ReturnsByRefReadonly,
			XmlDocSummary: first.XmlDocSummary,
			DelegateFriendlyName: delegateFriendlyName,
			PredicateFriendlyName: predicateFriendlyName,
			PredicateDelegateSignature: predicateDelegateSignature,
			BuilderFriendlyName: builderFriendlyName,
			SequenceFriendlyName: sequenceFriendlyName,
			HasRefStructParameter: hasRefStruct);
	}

	private static UnifiedMethodInterceptorModel BuildMultiOverloadModel(
		FlatMethodGroup group,
		FlatMethodModel[] methods,
		List<MethodSignatureInfo> uniqueSignatures,
		string className,
		string typeParameters)
	{
		// Sort unique methods for stable numbering: ascending by param count, then lex order by param types
		var seenSuffixes = new HashSet<string>();
		var uniqueMethods = new List<FlatMethodModel>();
		foreach (var method in methods)
		{
			var suffix = UnifiedInterceptorBuilder.GetSignatureSuffix(method.Parameters, method.ReturnType);
			if (seenSuffixes.Add(suffix))
				uniqueMethods.Add(method);
		}

		// Sort for numbering
		var sorted = uniqueMethods
			.OrderBy(m => m.Parameters.Count)
			.ThenBy(m => string.Join(",", m.Parameters.Select(p => p.Type)))
			.ToList();

		// Build overload signatures with numbered friendly names
		var overloads = new List<MethodOverloadSignature>();
		for (int i = 0; i < sorted.Count; i++)
		{
			var method = sorted[i];
			var suffix = UnifiedInterceptorBuilder.GetSignatureSuffix(method.Parameters, method.ReturnType);
			var overloadSuffix = UnifiedInterceptorBuilder.GetOverloadSuffix(i);

			// Always use custom delegate with method-name-based naming
			var delegateFriendlyName = $"{method.MethodName}Delegate{overloadSuffix}";
			var delegateParamList = BuildDelegateParamList(method.Parameters);
			var delegateSignature = method.IsVoid
				? $"public delegate void {delegateFriendlyName}({delegateParamList});"
				: $"public delegate {method.ReturnType} {delegateFriendlyName}({delegateParamList});";

			var builderFriendlyName = $"{method.MethodName}Impl{overloadSuffix}";
			var sequenceFriendlyName = $"{method.MethodName}Sequence{overloadSuffix}";

			// Predicate delegate: only for 2+ trackable params
			string? predicateFriendlyName = null;
			string? predicateDelegateSignature = null;
			if (method.TrackableParameters.Count >= 2)
			{
				predicateFriendlyName = $"{method.MethodName}Predicate{overloadSuffix}";
				var predicateParamList = BuildDelegateParamList(method.TrackableParameters);
				predicateDelegateSignature = $"public delegate bool {predicateFriendlyName}({predicateParamList});";
			}

			overloads.Add(new MethodOverloadSignature(
				SignatureSuffix: suffix,
				Parameters: method.Parameters,
				TrackableParameters: method.TrackableParameters,
				ParameterDeclarations: method.ParameterDeclarations,
				ReturnType: method.ReturnType,
				IsVoid: method.IsVoid,
				DelegateName: delegateFriendlyName,
				DelegateSignature: delegateSignature,
				LastArgType: GetLastArgType(method.TrackableParameters),
				LastArgsType: GetLastArgsType(method.TrackableParameters, method.LastCallType),
				BuilderInterface: GetBuilderInterface(method.TrackableParameters, method.LastCallType, delegateFriendlyName, method.IsVoid),
				DefaultExpression: method.DefaultExpression,
				ThrowsOnDefault: method.ThrowsOnDefault,
				// Per-signature stub override name for mixed overload groups
				StubOverrideName: method.HasStubOverride ? $"{method.MethodName}_" : null,
				ReturnsByRef: method.ReturnsByRef,
				ReturnsByRefReadonly: method.ReturnsByRefReadonly,
				XmlDocSummary: method.XmlDocSummary,
				DelegateFriendlyName: delegateFriendlyName,
				PredicateFriendlyName: predicateFriendlyName,
				PredicateDelegateSignature: predicateDelegateSignature,
				BuilderFriendlyName: builderFriendlyName,
				SequenceFriendlyName: sequenceFriendlyName));
		}

		// For overload groups, check if any method has stub override (for model-level tracking)
		var first = sorted[0];
		// Use the original (unsorted) methods list for DeclaringInterface -- the first method's
		// declaring interface is the most specific type that supports all overloads.
		// Sorted order would pick the overload with fewest params, whose declaring interface
		// may not have all overload signatures (e.g., IDictionary.Add vs ICollection.Add).
		var declaringInterface = methods[0].DeclaringInterface;
		var anyHasStubOverride = methods.Any(m => m.HasStubOverride);

		return new UnifiedMethodInterceptorModel(
			InterceptorClassName: group.InterceptorClassName,
			MethodName: first.MethodName,
			DeclaringInterface: declaringInterface,
			OwnerClassName: className,
			OwnerTypeParameters: typeParameters,
			Parameters: first.Parameters,
			TrackableParameters: first.TrackableParameters,
			ParameterDeclarations: first.ParameterDeclarations,
			ReturnType: first.ReturnType,
			IsVoid: first.IsVoid,
			CallDelegateType: "",
			NeedsCustomDelegate: false,
			CustomDelegateSignature: null,
			LastArgType: null,
			LastArgsType: null,
			// For multi-overload, the builder interface is per-signature, not at the model level
			BuilderInterface: "global::KnockOff.IMethodTracking",
			DefaultExpression: first.DefaultExpression,
			ThrowsOnDefault: first.ThrowsOnDefault,
			// For overload groups, stub override is tracked per-signature (see overloads below)
			StubOverrideName: anyHasStubOverride ? $"{first.MethodName}_" : null,
			Overloads: new EquatableArray<MethodOverloadSignature>(overloads.ToArray()),
			ReturnsByRef: first.ReturnsByRef,
			ReturnsByRefReadonly: first.ReturnsByRefReadonly,
			XmlDocSummary: first.XmlDocSummary);
	}

	private static List<MethodSignatureInfo> GetUniqueSignatures(List<MethodSignatureInfo> signatures)
	{
		var seen = new HashSet<string>();
		var unique = new List<MethodSignatureInfo>();

		foreach (var sig in signatures)
		{
			var suffix = UnifiedInterceptorBuilder.GetSignatureSuffix(sig.Parameters, sig.ReturnType);
			if (seen.Add(suffix))
			{
				unique.Add(sig);
			}
		}

		return unique;
	}

	private static string? GetLastArgType(EquatableArray<ParameterModel> trackableParams)
	{
		if (trackableParams.Count != 1)
			return null;
		return trackableParams.GetArray()![0].Type;
	}

	private static string? GetLastArgsType(EquatableArray<ParameterModel> trackableParams, string? lastCallType)
	{
		if (trackableParams.Count < 2)
			return null;
		// Use the LastCallType from the model if available, otherwise compute
		return lastCallType ?? $"({string.Join(", ", trackableParams.Select(p => $"{p.Type} {p.EscapedName}"))})";
	}

	private static string GetBuilderInterface(EquatableArray<ParameterModel> trackableParams, string? lastCallType, string delegateType, bool isVoid)
	{
		if (isVoid)
		{
			if (trackableParams.Count == 0)
				return $"global::KnockOff.IMethodCallBuilder<{delegateType}>";
			if (trackableParams.Count == 1)
			{
				var param = trackableParams.GetArray()![0];
				return $"global::KnockOff.IMethodCallBuilder<{delegateType}, {param.Type}>";
			}
			var tupleType = lastCallType ?? $"({string.Join(", ", trackableParams.Select(p => $"{p.Type} {p.EscapedName}"))})";
			return $"global::KnockOff.IMethodCallBuilderArgs<{delegateType}, {tupleType}>";
		}
		else
		{
			if (trackableParams.Count == 0)
				return $"global::KnockOff.IMethodReturnBuilder<{delegateType}>";
			if (trackableParams.Count == 1)
			{
				var param = trackableParams.GetArray()![0];
				return $"global::KnockOff.IMethodReturnBuilder<{delegateType}, {param.Type}>";
			}
			var tupleType = lastCallType ?? $"({string.Join(", ", trackableParams.Select(p => $"{p.Type} {p.EscapedName}"))})";
			return $"global::KnockOff.IMethodReturnBuilderArgs<{delegateType}, {tupleType}>";
		}
	}

	private static string BuildDelegateParamList(EquatableArray<ParameterModel> parameters)
	{
		var parts = new List<string>();
		foreach (var p in parameters)
		{
			parts.Add($"{p.RefPrefix}{p.Type} {p.EscapedName}");
		}
		return string.Join(", ", parts);
	}

	#region Property Adapters

	/// <summary>
	/// Converts a FlatPropertyModel to UnifiedPropertyInterceptorModel.
	/// </summary>
	public static UnifiedPropertyInterceptorModel ToUnifiedPropertyModel(FlatPropertyModel prop)
	{
		return new UnifiedPropertyInterceptorModel(
			InterceptorClassName: prop.InterceptorClassName,
			PropertyName: prop.MemberName,
			DeclaringInterface: prop.DeclaringInterface,
			ValueType: prop.ReturnType,
			NullableValueType: prop.NullableReturnType,
			DefaultExpression: prop.DefaultExpression,
			HasGetter: prop.HasGetter,
			HasSetter: prop.HasSetter,
			IsInitOnly: prop.IsInitOnly,
			ReturnsByRef: prop.ReturnsByRef,
			ReturnsByRefReadonly: prop.ReturnsByRefReadonly,
			IsRefStructType: prop.IsRefStructType);
	}

	/// <summary>
	/// Converts an InlinePropertyModel to UnifiedPropertyInterceptorModel.
	/// </summary>
	public static UnifiedPropertyInterceptorModel ToUnifiedPropertyModel(Model.Inline.InlinePropertyModel prop)
	{
		return new UnifiedPropertyInterceptorModel(
			InterceptorClassName: prop.InterceptorClassName,
			PropertyName: prop.PropertyName,
			DeclaringInterface: prop.DeclaringInterface,
			ValueType: prop.ReturnType,
			NullableValueType: prop.NullableReturnType,
			DefaultExpression: "default!",
			HasGetter: prop.HasGetter,
			HasSetter: prop.HasSetter,
			IsInitOnly: prop.IsInitOnly,
			ReturnsByRef: prop.ReturnsByRef,
			ReturnsByRefReadonly: prop.ReturnsByRefReadonly,
			IsRefStructType: prop.IsRefStructType);
	}

	#endregion

	#region Indexer Adapters

	/// <summary>
	/// Converts a FlatIndexerModel to UnifiedIndexerInterceptorModel.
	/// </summary>
	public static UnifiedIndexerInterceptorModel ToUnifiedIndexerModel(FlatIndexerModel indexer)
	{
		return new UnifiedIndexerInterceptorModel(
			InterceptorClassName: indexer.InterceptorClassName,
			IndexerName: "Indexer",
			DeclaringInterface: indexer.DeclaringInterface,
			KeyType: indexer.KeyType,
			NullableKeyType: indexer.NullableKeyType,
			KeyParamName: indexer.KeyParamName,
			KeyTypeFriendlyName: indexer.KeyTypeFriendlyName,
			ValueType: indexer.ReturnType,
			NullableValueType: indexer.NullableReturnType,
			DefaultExpression: indexer.DefaultExpression,
			HasGetter: indexer.HasGetter,
			HasSetter: indexer.HasSetter,
			ParameterSignature: indexer.ParameterSignature,
			ParameterTypes: indexer.ParameterTypes,
			KeyExpression: indexer.KeyExpression,
			ArgumentList: indexer.ArgumentList,
			IsInitOnly: indexer.IsInitOnly,
			ReturnsByRef: indexer.ReturnsByRef,
			ReturnsByRefReadonly: indexer.ReturnsByRefReadonly);
	}

	/// <summary>
	/// Converts an InlineIndexerModel to UnifiedIndexerInterceptorModel.
	/// </summary>
	public static UnifiedIndexerInterceptorModel ToUnifiedIndexerModel(Model.Inline.InlineIndexerModel indexer)
	{
		// For source delegation, we need flattened argument list (e.g., "row, col") not tuple key expression "(row, col)"
		var argumentList = indexer.KeyExpression.StartsWith("(") && indexer.KeyExpression.EndsWith(")")
			? indexer.KeyExpression.Substring(1, indexer.KeyExpression.Length - 2)
			: indexer.KeyExpression;

		return new UnifiedIndexerInterceptorModel(
			InterceptorClassName: indexer.InterceptorClassName,
			IndexerName: indexer.IndexerName,
			DeclaringInterface: indexer.DeclaringInterface,
			KeyType: indexer.KeyType,
			NullableKeyType: indexer.NullableKeyType,
			KeyParamName: "key",
			KeyTypeFriendlyName: indexer.KeyTypeFriendlyName,
			ValueType: indexer.ReturnType,
			NullableValueType: indexer.ReturnType + "?", // Approximate nullable
			DefaultExpression: "default!",
			HasGetter: indexer.HasGetter,
			HasSetter: indexer.HasSetter,
			ParameterSignature: indexer.ParameterSignature,
			ParameterTypes: indexer.ParameterTypes,
			KeyExpression: indexer.KeyExpression,
			ArgumentList: argumentList,
			IsInitOnly: indexer.IsInitOnly,
			ReturnsByRef: indexer.ReturnsByRef,
			ReturnsByRefReadonly: indexer.ReturnsByRefReadonly);
	}

	#endregion

	#region Delegate Adapters

	/// <summary>
	/// Converts an InlineDelegateStubModel to UnifiedMethodInterceptorModel and InterceptorRenderOptions.
	/// A delegate maps to a single non-overloaded method with no declaring interface, no stub override, no ref/out.
	/// </summary>
	public static (UnifiedMethodInterceptorModel Model, InterceptorRenderOptions Options) ToUnifiedModel(InlineDelegateStubModel del)
	{
		// Delegates have no out params, so trackable == all params
		var methodName = del.StubClassName;

		// Custom delegate type and signature
		var callDelegateType = $"{methodName}Delegate?";
		var delegateParamList = string.Join(", ", del.Parameters.Select(p => $"{p.RefPrefix}{p.Type} {p.EscapedName}"));
		var customDelegateSignature = del.IsVoid
			? $"public delegate void {methodName}Delegate({delegateParamList});"
			: $"public delegate {del.ReturnType} {methodName}Delegate({delegateParamList});";
		var delegateTypeForBuilder = callDelegateType.TrimEnd('?');

		// Friendly names
		var delegateFriendlyName = $"{methodName}Delegate";
		var builderFriendlyName = $"{methodName}Impl";
		var sequenceFriendlyName = $"{methodName}Sequence";

		// Predicate delegate: only for 2+ params
		string? predicateFriendlyName = null;
		string? predicateDelegateSignature = null;
		if (del.Parameters.Count >= 2)
		{
			predicateFriendlyName = $"{methodName}Predicate";
			predicateDelegateSignature = $"public delegate bool {predicateFriendlyName}({delegateParamList});";
		}

		var builderInterface = GetBuilderInterface(del.Parameters, null, delegateTypeForBuilder, del.IsVoid);

		var model = new UnifiedMethodInterceptorModel(
			InterceptorClassName: del.InterceptorClassName,
			MethodName: methodName,
			DeclaringInterface: "",
			OwnerClassName: del.StubClassName,
			OwnerTypeParameters: del.TypeParameterList,
			Parameters: del.Parameters,
			TrackableParameters: del.Parameters,
			ParameterDeclarations: del.InvokeParameterDeclarations,
			ReturnType: del.ReturnType,
			IsVoid: del.IsVoid,
			CallDelegateType: callDelegateType,
			NeedsCustomDelegate: true,
			CustomDelegateSignature: customDelegateSignature,
			LastArgType: GetLastArgType(del.Parameters),
			LastArgsType: GetLastArgsType(del.Parameters, null),
			BuilderInterface: builderInterface,
			DefaultExpression: del.DefaultExpression,
			ThrowsOnDefault: false,
			StubOverrideName: null,
			Overloads: EquatableArray<MethodOverloadSignature>.Empty,
			DelegateFriendlyName: delegateFriendlyName,
			PredicateFriendlyName: predicateFriendlyName,
			PredicateDelegateSignature: predicateDelegateSignature,
			BuilderFriendlyName: builderFriendlyName,
			SequenceFriendlyName: sequenceFriendlyName);

		var options = new InterceptorRenderOptions(
			BaseIndent: 2,
			IncludeStrictParameter: true,
			StrictAccessExpression: "strict",
			InterceptorTypeParameters: del.TypeParameterList,
			InterceptorConstraints: del.ConstraintClauses,
			StubOverrideFallback: false,
			StubTypeName: null);

		return (model, options);
	}

	#endregion
}
