// src/Generator/Renderer/Shared/ModelAdapters.cs
#nullable enable
using System.Collections.Generic;
using System.Linq;
using KnockOff.Builder;
using KnockOff.Model.Flat;
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
			HasRefOrOutParams: m.NeedsCustomDelegate && m.CustomDelegateName != null, // Approximation
			DefaultExpression: m.DefaultExpression,
			ThrowsOnDefault: m.ThrowsOnDefault)).ToList();

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

		return new UnifiedMethodInterceptorModel(
			InterceptorClassName: group.InterceptorClassName,
			MethodName: first.MethodName,
			OwnerClassName: className,
			OwnerTypeParameters: typeParameters,
			Parameters: first.Parameters,
			TrackableParameters: first.TrackableParameters,
			ParameterDeclarations: first.ParameterDeclarations,
			ReturnType: first.ReturnType,
			IsVoid: first.IsVoid,
			OnCallDelegateType: first.OnCallDelegateType,
			NeedsCustomDelegate: first.NeedsCustomDelegate,
			CustomDelegateSignature: first.CustomDelegateSignature,
			LastArgType: GetLastArgType(first.TrackableParameters),
			LastArgsType: GetLastArgsType(first.TrackableParameters, first.LastCallType),
			TrackingInterface: GetTrackingInterface(first.TrackableParameters, first.LastCallType),
			DefaultExpression: first.DefaultExpression,
			ThrowsOnDefault: first.ThrowsOnDefault,
			Overloads: EquatableArray<MethodOverloadSignature>.Empty);
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

			var delegateName = $"{method.MethodName}Delegate_{suffix}";
			var delegateParamList = BuildDelegateParamList(ownerWithParams, method.Parameters);
			var delegateSignature = method.IsVoid
				? $"public delegate void {delegateName}({delegateParamList});"
				: $"public delegate {method.ReturnType} {delegateName}({delegateParamList});";

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
				TrackingInterface: GetTrackingInterface(method.TrackableParameters, method.LastCallType),
				DefaultExpression: method.DefaultExpression,
				ThrowsOnDefault: method.ThrowsOnDefault));
		}

		return new UnifiedMethodInterceptorModel(
			InterceptorClassName: group.InterceptorClassName,
			MethodName: first.MethodName,
			OwnerClassName: className,
			OwnerTypeParameters: typeParameters,
			Parameters: first.Parameters,
			TrackableParameters: first.TrackableParameters,
			ParameterDeclarations: first.ParameterDeclarations,
			ReturnType: first.ReturnType,
			IsVoid: first.IsVoid,
			OnCallDelegateType: "",
			NeedsCustomDelegate: false,
			CustomDelegateSignature: null,
			LastArgType: null,
			LastArgsType: null,
			TrackingInterface: "global::KnockOff.IMethodTracking",
			DefaultExpression: first.DefaultExpression,
			ThrowsOnDefault: first.ThrowsOnDefault,
			Overloads: new EquatableArray<MethodOverloadSignature>(overloads.ToArray()));
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

	private static string GetTrackingInterface(EquatableArray<ParameterModel> trackableParams, string? lastCallType)
	{
		if (trackableParams.Count == 0)
			return "global::KnockOff.IMethodTracking";
		if (trackableParams.Count == 1)
		{
			var param = trackableParams.GetArray()![0];
			return $"global::KnockOff.IMethodTracking<{param.Type}>";
		}
		var tupleType = lastCallType ?? $"({string.Join(", ", trackableParams.Select(p => $"{p.Type} {p.EscapedName}"))})";
		return $"global::KnockOff.IMethodTrackingArgs<{tupleType}>";
	}

	private static string BuildDelegateParamList(string ownerClassName, EquatableArray<ParameterModel> parameters)
	{
		var parts = new List<string> { $"{ownerClassName} ko" };
		foreach (var p in parameters)
		{
			parts.Add($"{p.RefPrefix}{p.Type} {p.EscapedName}");
		}
		return string.Join(", ", parts);
	}
}
