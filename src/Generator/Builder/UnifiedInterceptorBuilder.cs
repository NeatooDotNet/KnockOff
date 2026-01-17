// src/Generator/Builder/UnifiedInterceptorBuilder.cs
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using KnockOff.Model.Shared;
using Microsoft.CodeAnalysis;

namespace KnockOff.Builder;

/// <summary>
/// Transforms interface member information into unified interceptor models.
/// Shared by both inline and flat builders to eliminate code duplication.
/// Contains all the algorithmic logic for building interceptor models.
/// </summary>
internal static class UnifiedInterceptorBuilder
{
	#region Method Interceptor Building

	/// <summary>
	/// Builds a unified method interceptor model for a method group (single method or overloads).
	/// </summary>
	public static UnifiedMethodInterceptorModel BuildMethodInterceptor(
		string interceptorClassName,
		string methodName,
		string ownerClassName,
		string ownerTypeParameters,
		IReadOnlyList<MethodSignatureInfo> overloads)
	{
		if (overloads.Count == 0)
			throw new ArgumentException("At least one overload is required", nameof(overloads));

		// Get unique signatures (some interface methods may have identical signatures)
		var uniqueSignatures = GetUniqueSignatures(overloads, methodName, ownerClassName, ownerTypeParameters);

		if (uniqueSignatures.Count == 1)
		{
			// Single-signature case
			var sig = uniqueSignatures[0];
			return new UnifiedMethodInterceptorModel(
				InterceptorClassName: interceptorClassName,
				MethodName: methodName,
				OwnerClassName: ownerClassName,
				OwnerTypeParameters: ownerTypeParameters,
				Parameters: sig.Parameters,
				TrackableParameters: sig.TrackableParameters,
				ParameterDeclarations: sig.ParameterDeclarations,
				ReturnType: sig.ReturnType,
				IsVoid: sig.IsVoid,
				OnCallDelegateType: BuildOnCallDelegateType(methodName, sig, ownerClassName, ownerTypeParameters),
				NeedsCustomDelegate: NeedsCustomDelegate(sig),
				CustomDelegateSignature: BuildCustomDelegateSignature(methodName, sig, ownerClassName, ownerTypeParameters),
				LastArgType: GetLastArgType(sig.TrackableParameters),
				LastArgsType: GetLastArgsType(sig.TrackableParameters),
				TrackingInterface: GetTrackingInterface(sig.TrackableParameters),
				DefaultExpression: sig.DefaultExpression,
				ThrowsOnDefault: sig.ThrowsOnDefault,
				Overloads: EquatableArray<MethodOverloadSignature>.Empty);
		}
		else
		{
			// Multi-overload case
			var first = uniqueSignatures[0];
			return new UnifiedMethodInterceptorModel(
				InterceptorClassName: interceptorClassName,
				MethodName: methodName,
				OwnerClassName: ownerClassName,
				OwnerTypeParameters: ownerTypeParameters,
				// Single-signature fields (not used for multi-overload, but need values)
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
				Overloads: new EquatableArray<MethodOverloadSignature>(
					uniqueSignatures.Select(sig => BuildOverloadSignature(methodName, sig, ownerClassName, ownerTypeParameters)).ToArray()));
		}
	}

	private static List<MethodSignatureInfo> GetUniqueSignatures(
		IReadOnlyList<MethodSignatureInfo> overloads,
		string methodName,
		string ownerClassName,
		string ownerTypeParameters)
	{
		var seen = new HashSet<string>();
		var unique = new List<MethodSignatureInfo>();

		foreach (var sig in overloads)
		{
			var suffix = GetSignatureSuffix(sig.Parameters, sig.ReturnType);
			if (seen.Add(suffix))
			{
				unique.Add(sig);
			}
		}

		return unique;
	}

	private static MethodOverloadSignature BuildOverloadSignature(
		string methodName,
		MethodSignatureInfo sig,
		string ownerClassName,
		string ownerTypeParameters)
	{
		var suffix = GetSignatureSuffix(sig.Parameters, sig.ReturnType);
		var delegateName = $"{methodName}Delegate_{suffix}";
		var ownerWithParams = string.IsNullOrEmpty(ownerTypeParameters)
			? ownerClassName
			: $"{ownerClassName}{ownerTypeParameters}";

		var delegateParamList = BuildDelegateParamList(ownerWithParams, sig.Parameters);
		var delegateSignature = sig.IsVoid
			? $"public delegate void {delegateName}({delegateParamList});"
			: $"public delegate {sig.ReturnType} {delegateName}({delegateParamList});";

		return new MethodOverloadSignature(
			SignatureSuffix: suffix,
			Parameters: sig.Parameters,
			TrackableParameters: sig.TrackableParameters,
			ParameterDeclarations: sig.ParameterDeclarations,
			ReturnType: sig.ReturnType,
			IsVoid: sig.IsVoid,
			DelegateName: delegateName,
			DelegateSignature: delegateSignature,
			LastArgType: GetLastArgType(sig.TrackableParameters),
			LastArgsType: GetLastArgsType(sig.TrackableParameters),
			TrackingInterface: GetTrackingInterface(sig.TrackableParameters),
			DefaultExpression: sig.DefaultExpression,
			ThrowsOnDefault: sig.ThrowsOnDefault);
	}

	#endregion

	#region Signature Suffix Generation

	/// <summary>
	/// Generates a stable signature suffix for overload resolution.
	/// E.g., "String_Int32_Boolean" for (string, int) -> bool
	/// </summary>
	public static string GetSignatureSuffix(EquatableArray<ParameterModel> parameters, string returnType)
	{
		var returnSuffix = GetTypeSuffix(returnType);
		if (parameters.Count == 0)
			return $"NoParams_{returnSuffix}";
		return string.Join("_", parameters.Select(p => GetTypeSuffix(p.Type))) + $"_{returnSuffix}";
	}

	/// <summary>
	/// Extracts a friendly type suffix from a fully qualified type name.
	/// E.g., "global::System.String" -> "String", "int" -> "Int32"
	/// </summary>
	public static string GetTypeSuffix(string type)
	{
		var simple = type.Replace("global::", "").Replace("System.", "");
		simple = simple switch
		{
			"int" => "Int32",
			"string" => "String",
			"bool" => "Boolean",
			"long" => "Int64",
			"double" => "Double",
			"float" => "Single",
			"decimal" => "Decimal",
			"char" => "Char",
			"byte" => "Byte",
			"short" => "Int16",
			"uint" => "UInt32",
			"ulong" => "UInt64",
			"ushort" => "UInt16",
			"sbyte" => "SByte",
			"object" => "Object",
			"void" => "void",
			_ => simple.Replace(".", "_").Replace("<", "_").Replace(">", "").Replace(",", "_").Replace(" ", "")
		};
		return simple.TrimEnd('?');
	}

	#endregion

	#region Tracking Type Determination

	/// <summary>
	/// Determines the IMethodTracking interface type based on trackable parameter count.
	/// </summary>
	public static string GetTrackingInterface(EquatableArray<ParameterModel> trackableParams)
	{
		if (trackableParams.Count == 0)
			return "global::KnockOff.IMethodTracking";
		if (trackableParams.Count == 1)
		{
			var param = trackableParams.GetArray()![0];
			return $"global::KnockOff.IMethodTracking<{param.Type}>";
		}
		// Multiple params use tuple
		var tupleType = GetLastArgsType(trackableParams);
		return $"global::KnockOff.IMethodTrackingArgs<{tupleType}>";
	}

	/// <summary>
	/// Gets the LastArg type for single-parameter tracking.
	/// Returns null if not exactly one trackable parameter.
	/// </summary>
	public static string? GetLastArgType(EquatableArray<ParameterModel> trackableParams)
	{
		if (trackableParams.Count != 1)
			return null;
		return trackableParams.GetArray()![0].Type;
	}

	/// <summary>
	/// Gets the LastArgs tuple type for multi-parameter tracking.
	/// Returns null if less than 2 trackable parameters.
	/// </summary>
	public static string? GetLastArgsType(EquatableArray<ParameterModel> trackableParams)
	{
		if (trackableParams.Count < 2)
			return null;
		return $"({string.Join(", ", trackableParams.Select(p => $"{p.Type} {p.EscapedName}"))})";
	}

	/// <summary>
	/// Builds the RecordCall argument expression from trackable parameters.
	/// </summary>
	public static string BuildTrackingArgs(EquatableArray<ParameterModel> trackableParams)
	{
		if (trackableParams.Count == 0)
			return "";
		if (trackableParams.Count == 1)
			return trackableParams.GetArray()![0].EscapedName;
		return "(" + string.Join(", ", trackableParams.Select(p => p.EscapedName)) + ")";
	}

	#endregion

	#region Delegate Type Construction

	/// <summary>
	/// Determines if a custom delegate is needed (vs Func/Action).
	/// Custom delegate is needed for ref/out parameters or non-void returns.
	/// </summary>
	public static bool NeedsCustomDelegate(MethodSignatureInfo sig)
	{
		return sig.HasRefOrOutParams || !sig.IsVoid;
	}

	/// <summary>
	/// Builds the OnCall delegate type string.
	/// </summary>
	public static string BuildOnCallDelegateType(
		string methodName,
		MethodSignatureInfo sig,
		string ownerClassName,
		string ownerTypeParameters)
	{
		if (NeedsCustomDelegate(sig))
		{
			return $"{methodName}Delegate?";
		}

		var ownerWithParams = string.IsNullOrEmpty(ownerTypeParameters)
			? ownerClassName
			: $"{ownerClassName}{ownerTypeParameters}";

		if (sig.Parameters.Count == 0)
			return $"global::System.Action<{ownerWithParams}>?";

		var paramTypes = string.Join(", ", sig.Parameters.Select(p => p.Type));
		return $"global::System.Action<{ownerWithParams}, {paramTypes}>?";
	}

	/// <summary>
	/// Builds the custom delegate signature if needed.
	/// </summary>
	public static string? BuildCustomDelegateSignature(
		string methodName,
		MethodSignatureInfo sig,
		string ownerClassName,
		string ownerTypeParameters)
	{
		if (!NeedsCustomDelegate(sig))
			return null;

		var ownerWithParams = string.IsNullOrEmpty(ownerTypeParameters)
			? ownerClassName
			: $"{ownerClassName}{ownerTypeParameters}";

		var delegateName = $"{methodName}Delegate";
		var delegateParamList = BuildDelegateParamList(ownerWithParams, sig.Parameters);

		return sig.IsVoid
			? $"public delegate void {delegateName}({delegateParamList});"
			: $"public delegate {sig.ReturnType} {delegateName}({delegateParamList});";
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

	#endregion

	#region Parameter Processing

	/// <summary>
	/// Filters parameters to only those that should be tracked.
	/// Excludes out parameters (they don't have input values to track).
	/// </summary>
	public static EquatableArray<ParameterModel> GetTrackableParameters(EquatableArray<ParameterModel> allParams)
	{
		return allParams.Where(p => p.RefKind != RefKind.Out).ToEquatableArray();
	}

	/// <summary>
	/// Filters parameters for generic methods: excludes out params AND params with generic type arguments.
	/// </summary>
	public static EquatableArray<ParameterModel> GetTrackableParametersForGenericMethod(
		EquatableArray<ParameterModel> allParams,
		IReadOnlyList<string> typeParamNames)
	{
		return allParams
			.Where(p => p.RefKind != RefKind.Out)
			.Where(p => !typeParamNames.Any(tp => p.Type.Contains(tp)))
			.ToEquatableArray();
	}

	/// <summary>
	/// Builds parameter declarations string (e.g., "string name, ref int count").
	/// </summary>
	public static string BuildParameterDeclarations(EquatableArray<ParameterModel> parameters)
	{
		return string.Join(", ", parameters.Select(p => $"{p.RefPrefix}{p.Type} {p.EscapedName}"));
	}

	/// <summary>
	/// Builds parameter names string for method calls (e.g., "name, ref count").
	/// </summary>
	public static string BuildParameterNames(EquatableArray<ParameterModel> parameters)
	{
		return string.Join(", ", parameters.Select(p => $"{p.RefPrefix}{p.EscapedName}"));
	}

	/// <summary>
	/// Makes a type nullable if it isn't already.
	/// </summary>
	public static string MakeNullable(string type)
	{
		if (type.EndsWith("?"))
			return type;
		// Reference types that are already nullable or primitives that need ?
		return type + "?";
	}

	/// <summary>
	/// Escapes a C# identifier if it's a reserved keyword.
	/// </summary>
	public static string EscapeIdentifier(string name)
	{
		return name switch
		{
			"abstract" or "as" or "base" or "bool" or "break" or "byte" or "case" or "catch" or "char" or "checked" or
			"class" or "const" or "continue" or "decimal" or "default" or "delegate" or "do" or "double" or "else" or
			"enum" or "event" or "explicit" or "extern" or "false" or "finally" or "fixed" or "float" or "for" or
			"foreach" or "goto" or "if" or "implicit" or "in" or "int" or "interface" or "internal" or "is" or "lock" or
			"long" or "namespace" or "new" or "null" or "object" or "operator" or "out" or "override" or "params" or
			"private" or "protected" or "public" or "readonly" or "ref" or "return" or "sbyte" or "sealed" or "short" or
			"sizeof" or "stackalloc" or "static" or "string" or "struct" or "switch" or "this" or "throw" or "true" or
			"try" or "typeof" or "uint" or "ulong" or "unchecked" or "unsafe" or "ushort" or "using" or "virtual" or
			"void" or "volatile" or "while" => $"@{name}",
			_ => name
		};
	}

	/// <summary>
	/// Gets the ref kind prefix string for a parameter.
	/// </summary>
	public static string GetRefKindPrefix(RefKind refKind)
	{
		return refKind switch
		{
			RefKind.Ref => "ref ",
			RefKind.Out => "out ",
			RefKind.In => "in ",
			RefKind.RefReadOnlyParameter => "ref readonly ",
			_ => ""
		};
	}

	#endregion
}

/// <summary>
/// Intermediate representation of a method signature for building unified models.
/// Used to pass method information from specific builders to the unified builder.
/// </summary>
internal sealed record MethodSignatureInfo(
	EquatableArray<ParameterModel> Parameters,
	EquatableArray<ParameterModel> TrackableParameters,
	string ParameterDeclarations,
	string ReturnType,
	bool IsVoid,
	bool HasRefOrOutParams,
	string DefaultExpression,
	bool ThrowsOnDefault);
