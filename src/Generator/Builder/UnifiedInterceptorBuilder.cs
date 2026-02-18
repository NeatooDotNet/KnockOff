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
	/// <param name="interceptorClassName">The name of the interceptor class (e.g., "ProcessInterceptor").</param>
	/// <param name="methodName">The method name (e.g., "Process").</param>
	/// <param name="declaringInterface">The declaring interface type for Source(T) feature.</param>
	/// <param name="ownerClassName">The class name that owns this interceptor.</param>
	/// <param name="ownerTypeParameters">Type parameters on the owner class.</param>
	/// <param name="overloads">The method signatures (one or more for overload groups).</param>
	/// <param name="stubOverrideName">Optional stub override name for fallback (e.g., "Process_"). Null if no stub override.</param>
	public static UnifiedMethodInterceptorModel BuildMethodInterceptor(
		string interceptorClassName,
		string methodName,
		string declaringInterface,
		string ownerClassName,
		string ownerTypeParameters,
		IReadOnlyList<MethodSignatureInfo> overloads,
		string? stubOverrideName = null)
	{
		if (overloads.Count == 0)
			throw new ArgumentException("At least one overload is required", nameof(overloads));

		// Get unique signatures (some interface methods may have identical signatures)
		var uniqueSignatures = GetUniqueSignatures(overloads, methodName, ownerClassName, ownerTypeParameters);

		if (uniqueSignatures.Count == 1)
		{
			// Single-signature case
			var sig = uniqueSignatures[0];
			var callDelegateType = BuildCallDelegateType(methodName, sig, ownerClassName, ownerTypeParameters);
			var needsCustom = NeedsCustomDelegate(sig);
			// UsesTupleCallDelegate: true when we use a named tuple in the Func/Action type (2+ params, no ref/out)
			var usesTuple = !needsCustom && sig.Parameters.Count >= 2;
			// Get the delegate type without nullable marker for builder interface
			var delegateTypeForBuilder = callDelegateType.TrimEnd('?');
			return new UnifiedMethodInterceptorModel(
				InterceptorClassName: interceptorClassName,
				MethodName: methodName,
				DeclaringInterface: declaringInterface,
				OwnerClassName: ownerClassName,
				OwnerTypeParameters: ownerTypeParameters,
				Parameters: sig.Parameters,
				TrackableParameters: sig.TrackableParameters,
				ParameterDeclarations: sig.ParameterDeclarations,
				ReturnType: sig.ReturnType,
				IsVoid: sig.IsVoid,
				CallDelegateType: callDelegateType,
				NeedsCustomDelegate: needsCustom,
				CustomDelegateSignature: BuildCustomDelegateSignature(methodName, sig, ownerClassName, ownerTypeParameters),
				LastArgType: GetLastArgType(sig.TrackableParameters),
				LastArgsType: GetLastArgsType(sig.TrackableParameters),
				BuilderInterface: GetBuilderInterface(sig.TrackableParameters, delegateTypeForBuilder, sig.IsVoid),
				DefaultExpression: sig.DefaultExpression,
				ThrowsOnDefault: sig.ThrowsOnDefault,
				StubOverrideName: stubOverrideName,
				Overloads: EquatableArray<MethodOverloadSignature>.Empty,
				ReturnsByRef: sig.ReturnsByRef,
				ReturnsByRefReadonly: sig.ReturnsByRefReadonly,
				XmlDocSummary: sig.XmlDocSummary,
				UsesTupleCallDelegate: usesTuple);
		}
		else
		{
			// Multi-overload case
			var first = uniqueSignatures[0];
			return new UnifiedMethodInterceptorModel(
				InterceptorClassName: interceptorClassName,
				MethodName: methodName,
				DeclaringInterface: declaringInterface,
				OwnerClassName: ownerClassName,
				OwnerTypeParameters: ownerTypeParameters,
				// Single-signature fields (not used for multi-overload, but need values)
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
				// For multi-overload, stub override is tracked per-signature (see MethodOverloadSignature.StubOverrideName)
				StubOverrideName: stubOverrideName,
				Overloads: new EquatableArray<MethodOverloadSignature>(
					uniqueSignatures.Select(sig => BuildOverloadSignature(methodName, sig, ownerClassName, ownerTypeParameters, stubOverrideName)).ToArray()),
				ReturnsByRef: first.ReturnsByRef,
				ReturnsByRefReadonly: first.ReturnsByRefReadonly,
				XmlDocSummary: first.XmlDocSummary);
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
			// Deduplicate by PARAMETERS ONLY (ignore return type for deduplication).
			// Same-params-different-return (like ISet<T>.Add void vs bool) should be ONE signature.
			// Different-params (like Transform(int)->string vs Transform(string)->int) remain distinct.
			var paramKey = GetParameterOnlyKey(sig.Parameters);
			if (seen.Add(paramKey))
			{
				unique.Add(sig);
			}
		}

		return unique;
	}

	/// <summary>
	/// Gets a key based only on parameter types, ignoring return type.
	/// Used for deduplication when same-params-different-return overloads should merge.
	/// </summary>
	private static string GetParameterOnlyKey(EquatableArray<ParameterModel> parameters)
	{
		if (parameters.Count == 0)
			return "NoParams";
		return string.Join("_", parameters.Select(p => GetTypeSuffix(p.Type)));
	}

	private static MethodOverloadSignature BuildOverloadSignature(
		string methodName,
		MethodSignatureInfo sig,
		string ownerClassName,
		string ownerTypeParameters,
		string? stubOverrideName = null)
	{
		var suffix = GetSignatureSuffix(sig.Parameters, sig.ReturnType);

		// Use Func/Action for non-ref/out overloads (same logic as single-signature),
		// only fall back to custom delegates for ref/out.
		string delegateName;
		string? delegateSignature;
		bool usesTuple;

		if (NeedsCustomDelegate(sig))
		{
			// ref/out: keep custom delegate
			delegateName = $"{methodName}Delegate_{suffix}";
			var delegateParamList = BuildDelegateParamList(sig.Parameters);
			delegateSignature = sig.IsVoid
				? $"public delegate void {delegateName}({delegateParamList});"
				: $"public delegate {sig.ReturnType} {delegateName}({delegateParamList});";
			usesTuple = false;
		}
		else
		{
			// Use Func/Action (same logic as BuildCallDelegateType but without trailing ?)
			var fullType = BuildCallDelegateType(methodName, sig, ownerClassName, ownerTypeParameters);
			delegateName = fullType.TrimEnd('?');
			delegateSignature = null;
			usesTuple = sig.Parameters.Count >= 2;
		}

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
			BuilderInterface: GetBuilderInterface(sig.TrackableParameters, delegateName, sig.IsVoid),
			DefaultExpression: sig.DefaultExpression,
			ThrowsOnDefault: sig.ThrowsOnDefault,
			StubOverrideName: sig.StubOverrideName,
			ReturnsByRef: sig.ReturnsByRef,
			ReturnsByRefReadonly: sig.ReturnsByRefReadonly,
			XmlDocSummary: sig.XmlDocSummary,
			UsesTupleCallDelegate: usesTuple);
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
	/// Computes a friendly type suffix for indexer key types from individual parameter types.
	/// For single-param: returns GetTypeSuffix of that param's type (e.g., "Int32").
	/// For multi-param: joins each param's type suffix with "_" (e.g., "Int32_String").
	/// This avoids the bug where passing a combined type string like "(int a, string b)"
	/// to GetTypeSuffix would produce "inta_stringb" instead of "Int32_String".
	/// </summary>
	public static string GetIndexerKeyTypeFriendlyName(EquatableArray<ParameterInfo> indexerParameters)
	{
		return indexerParameters.Count == 1
			? GetTypeSuffix(indexerParameters.GetArray()![0].Type)
			: string.Join("_", indexerParameters.Select(p => GetTypeSuffix(p.Type)));
	}

	/// <summary>
	/// Extracts a friendly type suffix from a fully qualified type name.
	/// E.g., "global::System.String" -> "String", "int" -> "Int32"
	/// </summary>
	public static string GetTypeSuffix(string type)
	{
		// Strip trailing nullable marker for array bracket detection
		var workingType = type.TrimEnd('?');

		// Parse array suffixes (handles [], [,], [,,], etc.)
		var arraySuffixes = new List<int>(); // rank per array dimension
		while (true)
		{
			// Check for array brackets: [], [,], [,,], etc.
			if (workingType.Length >= 2)
			{
				var lastBracket = workingType.LastIndexOf('[');
				if (lastBracket >= 0 && workingType[workingType.Length - 1] == ']')
				{
					var bracketContent = workingType.Substring(lastBracket + 1, workingType.Length - lastBracket - 2);
					if (bracketContent.Length == 0 || bracketContent.All(c => c == ','))
					{
						var rank = bracketContent.Length + 1; // "" = rank 1, "," = rank 2
						arraySuffixes.Add(rank);
						workingType = workingType.Substring(0, lastBracket);
						continue;
					}
				}
			}
			break;
		}

		// Strip nullable after array brackets (handles string?[] -> string? -> string)
		workingType = workingType.TrimEnd('?');

		var simple = workingType.Replace("global::", "").Replace("System.", "");
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
			"void" => "Void",
			"nint" => "IntPtr",
			"nuint" => "UIntPtr",
			_ => simple.Replace(".", "_").Replace("<", "_").Replace(">", "")
				.Replace(",", "_").Replace(" ", "")
				.Replace("[", "").Replace("]", "")
				.Replace("(", "").Replace(")", "")
				.Replace("?", "")
		};

		// Append array suffixes in reverse order (outermost first)
		for (int i = arraySuffixes.Count - 1; i >= 0; i--)
		{
			simple += arraySuffixes[i] == 1 ? "Array" : $"Array{arraySuffixes[i]}D";
		}

		return simple;
	}

	#endregion

	#region Tracking Type Determination

	/// <summary>
	/// Determines the builder interface type based on trackable parameter count and void/non-void.
	/// Non-void methods use IMethodReturnBuilder, void methods use IMethodCallBuilder.
	/// </summary>
	public static string GetBuilderInterface(EquatableArray<ParameterModel> trackableParams, string delegateType, bool isVoid)
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
			var tupleType = GetLastArgsType(trackableParams);
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
			var tupleType = GetLastArgsType(trackableParams);
			return $"global::KnockOff.IMethodReturnBuilderArgs<{delegateType}, {tupleType}>";
		}
	}

	/// <summary>
	/// Determines the IMethodTracking interface type based on trackable parameter count.
	/// Used internally by the builder implementation class.
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
	/// Custom delegate is only needed for ref/out parameters.
	/// Non-void methods without ref/out use Func&lt;&gt;, void methods use Action&lt;&gt;.
	/// For 2+ params, the Func/Action uses a named tuple as a single parameter.
	/// </summary>
	public static bool NeedsCustomDelegate(MethodSignatureInfo sig)
	{
		return sig.HasRefOrOutParams;
	}

	/// <summary>
	/// Builds the Call delegate type string.
	/// For ref/out: custom delegate. For non-void without ref/out: Func&lt;...&gt;.
	/// For void without ref/out: Action&lt;...&gt;.
	/// For 2+ params (without ref/out): uses named tuple as single parameter.
	/// </summary>
	public static string BuildCallDelegateType(
		string methodName,
		MethodSignatureInfo sig,
		string ownerClassName,
		string ownerTypeParameters)
	{
		if (NeedsCustomDelegate(sig))
		{
			return $"{methodName}Delegate?";
		}

		if (sig.IsVoid)
		{
			// Void methods: Action, Action<T1>, or Action<(T1 a, T2 b)>
			if (sig.Parameters.Count == 0)
				return "global::System.Action?";
			if (sig.Parameters.Count == 1)
				return $"global::System.Action<{sig.Parameters.GetArray()![0].Type}>?";
			// 2+ params: named tuple
			var tupleType = "(" + string.Join(", ", sig.Parameters.Select(p => $"{p.Type} {p.EscapedName}")) + ")";
			return $"global::System.Action<{tupleType}>?";
		}
		else
		{
			// Non-void methods: Func<TReturn>, Func<T1, TReturn>, or Func<(T1 a, T2 b), TReturn>
			if (sig.Parameters.Count == 0)
				return $"global::System.Func<{sig.ReturnType}>?";
			if (sig.Parameters.Count == 1)
				return $"global::System.Func<{sig.Parameters.GetArray()![0].Type}, {sig.ReturnType}>?";
			// 2+ params: named tuple
			var tupleType = "(" + string.Join(", ", sig.Parameters.Select(p => $"{p.Type} {p.EscapedName}")) + ")";
			return $"global::System.Func<{tupleType}, {sig.ReturnType}>?";
		}
	}

	/// <summary>
	/// Builds the custom delegate signature if needed.
	/// Only generates for ref/out parameters.
	/// </summary>
	public static string? BuildCustomDelegateSignature(
		string methodName,
		MethodSignatureInfo sig,
		string ownerClassName,
		string ownerTypeParameters)
	{
		if (!NeedsCustomDelegate(sig))
			return null;

		var delegateName = $"{methodName}Delegate";
		var delegateParamList = BuildDelegateParamList(sig.Parameters);

		return sig.IsVoid
			? $"public delegate void {delegateName}({delegateParamList});"
			: $"public delegate {sig.ReturnType} {delegateName}({delegateParamList});";
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

	#region When Chain Support

	/// <summary>
	/// Builds the predicate Func type for When matching.
	/// 0 params: Func&lt;bool&gt;, 1 param: Func&lt;T1, bool&gt;, 2+ params: Func&lt;(T1 a, T2 b), bool&gt;.
	/// </summary>
	public static string BuildWhenPredicateType(EquatableArray<ParameterModel> parameters)
	{
		if (parameters.Count == 0)
			return "global::System.Func<bool>";

		if (parameters.Count == 1)
			return $"global::System.Func<{parameters.GetArray()![0].Type}, bool>";

		// 2+ params: named tuple
		var tupleType = "(" + string.Join(", ", parameters.Select(p => $"{p.Type} {p.EscapedName}")) + ")";
		return $"global::System.Func<{tupleType}, bool>";
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
	bool ThrowsOnDefault,
	/// <summary>Per-signature stub override name for partial overload coverage. Null if no stub override for this signature.</summary>
	string? StubOverrideName = null,
	/// <summary>True if the method returns by ref (ref T).</summary>
	bool ReturnsByRef = false,
	/// <summary>True if the method returns by ref readonly (ref readonly T).</summary>
	bool ReturnsByRefReadonly = false,
	/// <summary>XML documentation summary text for this method, extracted from the original interface/class. Null if none.</summary>
	string? XmlDocSummary = null);
