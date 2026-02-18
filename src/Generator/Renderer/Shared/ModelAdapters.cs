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
			ReturnsByRefReadonly: m.ReturnsByRefReadonly)).ToList();

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
		var ownerWithParams = string.IsNullOrEmpty(typeParameters)
			? className
			: $"{className}{typeParameters}";

		// Recompute delegate types using UnifiedInterceptorBuilder for consistency.
		// The FlatModelBuilder may compute these differently (e.g., old NeedsCustomDelegate logic).
		var hasRefOrOut = first.Parameters.Any(p => p.RefKind == Microsoft.CodeAnalysis.RefKind.Ref || p.RefKind == Microsoft.CodeAnalysis.RefKind.Out);
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
			XmlDocSummary: first.XmlDocSummary);

		var needsCustomDelegate = UnifiedInterceptorBuilder.NeedsCustomDelegate(sig);
		var callDelegateType = UnifiedInterceptorBuilder.BuildCallDelegateType(first.MethodName, sig, className, typeParameters);
		var customDelegateSignature = UnifiedInterceptorBuilder.BuildCustomDelegateSignature(first.MethodName, sig, className, typeParameters);
		var usesTuple = !needsCustomDelegate && first.Parameters.Count >= 2;

		// Get delegate type without nullable marker for builder interface
		var delegateTypeForBuilder = callDelegateType.TrimEnd('?');

		return new UnifiedMethodInterceptorModel(
			InterceptorClassName: group.InterceptorClassName,
			MethodName: first.MethodName,
			DeclaringInterface: first.DeclaringInterface,
			OwnerClassName: className,
			OwnerTypeParameters: typeParameters,
			Parameters: first.Parameters,
			TrackableParameters: first.TrackableParameters,
			ParameterDeclarations: first.ParameterDeclarations,
			ReturnType: first.ReturnType,
			IsVoid: first.IsVoid,
			CallDelegateType: callDelegateType,
			NeedsCustomDelegate: needsCustomDelegate,
			CustomDelegateSignature: customDelegateSignature,
			LastArgType: GetLastArgType(first.TrackableParameters),
			LastArgsType: GetLastArgsType(first.TrackableParameters, first.LastCallType),
			BuilderInterface: GetBuilderInterface(first.TrackableParameters, first.LastCallType, delegateTypeForBuilder, first.IsVoid),
			DefaultExpression: first.DefaultExpression,
			ThrowsOnDefault: first.ThrowsOnDefault,
			// Stub override name: if HasStubOverride, the stub override name is MethodName + "_"
			StubOverrideName: first.HasStubOverride ? $"{first.MethodName}_" : null,
			Overloads: EquatableArray<MethodOverloadSignature>.Empty,
			ReturnsByRef: first.ReturnsByRef,
			ReturnsByRefReadonly: first.ReturnsByRefReadonly,
			XmlDocSummary: first.XmlDocSummary,
			UsesTupleCallDelegate: usesTuple);
	}

	private static UnifiedMethodInterceptorModel BuildMultiOverloadModel(
		FlatMethodGroup group,
		FlatMethodModel[] methods,
		List<MethodSignatureInfo> uniqueSignatures,
		string className,
		string typeParameters)
	{
		var first = methods[0];
		var ownerWithParams = string.IsNullOrEmpty(typeParameters)
			? className
			: $"{className}{typeParameters}";

		// Build overload signatures
		var overloads = new List<MethodOverloadSignature>();
		var seenSuffixes = new HashSet<string>();

		foreach (var method in methods)
		{
			var suffix = UnifiedInterceptorBuilder.GetSignatureSuffix(method.Parameters, method.ReturnType);
			if (!seenSuffixes.Add(suffix))
				continue;

			// Use Func/Action for non-ref/out overloads (same as single-signature),
			// only fall back to custom delegates for ref/out.
			var hasRefOrOut = method.Parameters.Any(p => p.RefKind == Microsoft.CodeAnalysis.RefKind.Ref || p.RefKind == Microsoft.CodeAnalysis.RefKind.Out);
			string delegateName;
			string? delegateSignature;
			bool usesTuple;

			if (hasRefOrOut)
			{
				delegateName = $"{method.MethodName}Delegate_{suffix}";
				var delegateParamList = BuildDelegateParamList(method.Parameters);
				delegateSignature = method.IsVoid
					? $"public delegate void {delegateName}({delegateParamList});"
					: $"public delegate {method.ReturnType} {delegateName}({delegateParamList});";
				usesTuple = false;
			}
			else
			{
				delegateName = BuildFuncActionDelegateType(method.Parameters, method.ReturnType, method.IsVoid);
				delegateSignature = null;
				usesTuple = method.Parameters.Count >= 2;
			}

			overloads.Add(new MethodOverloadSignature(
				SignatureSuffix: suffix,
				Parameters: method.Parameters,
				TrackableParameters: method.TrackableParameters,
				ParameterDeclarations: method.ParameterDeclarations,
				ReturnType: method.ReturnType,
				IsVoid: method.IsVoid,
				DelegateName: delegateName,
				DelegateSignature: delegateSignature,
				LastArgType: GetLastArgType(method.TrackableParameters),
				LastArgsType: GetLastArgsType(method.TrackableParameters, method.LastCallType),
				BuilderInterface: GetBuilderInterface(method.TrackableParameters, method.LastCallType, delegateName, method.IsVoid),
				DefaultExpression: method.DefaultExpression,
				ThrowsOnDefault: method.ThrowsOnDefault,
				// Per-signature stub override name for mixed overload groups
				StubOverrideName: method.HasStubOverride ? $"{method.MethodName}_" : null,
				ReturnsByRef: method.ReturnsByRef,
				ReturnsByRefReadonly: method.ReturnsByRefReadonly,
				XmlDocSummary: method.XmlDocSummary,
				UsesTupleCallDelegate: usesTuple));
		}

		// For overload groups, check if any method has stub override (for model-level tracking)
		// Per-signature stub overrides are tracked in MethodOverloadSignature.StubOverrideName
		var anyHasStubOverride = methods.Any(m => m.HasStubOverride);

		return new UnifiedMethodInterceptorModel(
			InterceptorClassName: group.InterceptorClassName,
			MethodName: first.MethodName,
			DeclaringInterface: first.DeclaringInterface,
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

	/// <summary>
	/// Builds the Func/Action type string for a non-ref/out method signature.
	/// For 2+ params, uses a named tuple as a single parameter.
	/// Returns the type without trailing ?.
	/// </summary>
	private static string BuildFuncActionDelegateType(EquatableArray<ParameterModel> parameters, string returnType, bool isVoid)
	{
		if (isVoid)
		{
			if (parameters.Count == 0)
				return "global::System.Action";
			if (parameters.Count == 1)
				return $"global::System.Action<{parameters.GetArray()![0].Type}>";
			var tupleType = "(" + string.Join(", ", parameters.Select(p => $"{p.Type} {p.EscapedName}")) + ")";
			return $"global::System.Action<{tupleType}>";
		}
		else
		{
			if (parameters.Count == 0)
				return $"global::System.Func<{returnType}>";
			if (parameters.Count == 1)
				return $"global::System.Func<{parameters.GetArray()![0].Type}, {returnType}>";
			var tupleType = "(" + string.Join(", ", parameters.Select(p => $"{p.Type} {p.EscapedName}")) + ")";
			return $"global::System.Func<{tupleType}, {returnType}>";
		}
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
			ReturnsByRefReadonly: prop.ReturnsByRefReadonly);
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
			ReturnsByRefReadonly: prop.ReturnsByRefReadonly);
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
		var callType = del.CallType;
		var builderInterface = GetBuilderInterface(del.Parameters, null, callType, del.IsVoid);

		var model = new UnifiedMethodInterceptorModel(
			InterceptorClassName: del.InterceptorClassName,
			MethodName: del.StubClassName,
			DeclaringInterface: "",
			OwnerClassName: del.StubClassName,
			OwnerTypeParameters: del.TypeParameterList,
			Parameters: del.Parameters,
			TrackableParameters: del.Parameters,
			ParameterDeclarations: del.InvokeParameterDeclarations,
			ReturnType: del.ReturnType,
			IsVoid: del.IsVoid,
			CallDelegateType: del.CallType,
			NeedsCustomDelegate: false,
			CustomDelegateSignature: null,
			LastArgType: GetLastArgType(del.Parameters),
			LastArgsType: GetLastArgsType(del.Parameters, null),
			BuilderInterface: builderInterface,
			DefaultExpression: del.DefaultExpression,
			ThrowsOnDefault: false,
			StubOverrideName: null,
			Overloads: EquatableArray<MethodOverloadSignature>.Empty);

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
