// src/Generator/Renderer/Shared/PreCompiledInterceptorRenderer.cs
#nullable enable
using System.Collections.Generic;
using System.Linq;
using KnockOff;
using KnockOff.Model.Shared;

namespace KnockOff.Renderer.Shared;

/// <summary>
/// Determines whether a method/property/indexer can use a pre-compiled interceptor type
/// from the KnockOff.Interceptors namespace, and emits field declarations when eligible.
/// Falls back to the existing generated-class approach for ref/out, ref returns, >8 params.
/// </summary>
internal static class PreCompiledInterceptorRenderer
{
	// ========================================================================
	// Decision tree
	// ========================================================================

	/// <summary>
	/// Determines whether a single-signature method can use a pre-compiled interceptor.
	/// Returns false for overload groups, ref/out, ref returns, and >8 params.
	/// </summary>
	public static bool CanUsePreCompiled(UnifiedMethodInterceptorModel model)
	{
		// Overload groups use fully generated interceptor classes
		if (model.Overloads.Count > 0) return false;

		// Ref/out parameters - Func<> cannot express ref/out
		if (model.Parameters.Any(p => p.RefKind == Microsoft.CodeAnalysis.RefKind.Ref || p.RefKind == Microsoft.CodeAnalysis.RefKind.Out))
			return false;

		// Ref returns - need backing field in generated class
		if (model.IsRefReturn) return false;

		// >8 parameters - no MethodInterceptor9+
		if (model.Parameters.Count > 8) return false;

		return true;
	}

	/// <summary>
	/// Determines whether a property can use a pre-compiled interceptor.
	/// Init-only and ref return properties use the existing generated-class approach.
	/// </summary>
	public static bool CanUsePreCompiled(UnifiedPropertyInterceptorModel model)
	{
		// Init-only properties have special handling (SetValue pattern)
		if (model.IsInitOnly) return false;

		// Ref return properties need backing field
		if (model.IsRefReturn) return false;

		return true;
	}

	/// <summary>
	/// Determines whether an indexer can use a pre-compiled interceptor.
	/// Multi-param, init-only, and ref return indexers use the existing generated-class approach.
	/// </summary>
	public static bool CanUsePreCompiled(IReadOnlyList<UnifiedIndexerInterceptorModel> models)
	{
		if (models.Count == 0) return false;

		// Multi-key indexers stay inline (different calling convention)
		if (models.Count > 1) return false;

		var first = models[0];

		// Multi-param indexers (e.g., this[int x, int y])
		if (first.KeyExpression.StartsWith("(")) return false;

		// Init-only indexers
		if (first.IsInitOnly) return false;

		// Ref return indexers
		if (first.IsRefReturn) return false;

		return true;
	}

	// ========================================================================
	// Method interceptor field type computation
	// ========================================================================

	/// <summary>
	/// Computes the TArgs type for TTuple interceptors.
	/// 1 param -> raw type, 2+ params -> named ValueTuple.
	/// </summary>
	private static string ComputeTArgsType(IEnumerable<ParameterModel> parameters)
	{
		var paramList = parameters.ToList();
		if (paramList.Count == 1)
			return paramList[0].Type;
		// Named ValueTuple: (T1 name1, T2 name2, ...)
		return "(" + string.Join(", ", paramList.Select(p => $"{p.Type} {p.EscapedName}")) + ")";
	}

	/// <summary>
	/// Computes the delegate type name for a method.
	/// Follows the convention: {MethodName}Delegate.
	/// </summary>
	public static string ComputeDelegateTypeName(string methodName)
	{
		return $"{methodName}Delegate";
	}

	/// <summary>
	/// Computes the sync delegate type name for an async method.
	/// Follows the convention: {MethodName}SyncDelegate.
	/// </summary>
	public static string ComputeSyncDelegateTypeName(string methodName)
	{
		return $"{methodName}SyncDelegate";
	}

	/// <summary>
	/// Builds the sync delegate type declaration for an async method with 1+ params.
	/// Returns null if the method is not async or has 0 params.
	/// The sync delegate has the same parameters as the async delegate but returns
	/// the inner type (TReturn) instead of Task&lt;TReturn&gt;, or void instead of Task.
	/// </summary>
	public static string? BuildSyncDelegateDeclaration(string methodName, IEnumerable<ParameterModel> parameters, string returnType, bool isVoid, string? delegateBaseName = null)
	{
		var paramList = parameters.ToList();
		if (paramList.Count == 0) return null;

		var (innerType, isAsyncTaskT, isAsyncValueTaskT) = GetAsyncTypeInfo(returnType);
		var (isVoidTask, isVoidValueTask) = GetVoidAsyncInfo(returnType);
		var isAsync = isAsyncTaskT || isAsyncValueTaskT || isVoidTask || isVoidValueTask;

		if (!isAsync) return null;

		var syncDelegateName = ComputeSyncDelegateTypeName(delegateBaseName ?? methodName);
		var paramDecls = string.Join(", ", paramList.Select(p => $"{p.RefPrefix}{p.Type} {p.EscapedName}"));

		string syncReturnType;
		if (isVoid || isVoidTask || isVoidValueTask)
			syncReturnType = "void";
		else
			syncReturnType = innerType;

		return $"public delegate {syncReturnType} {syncDelegateName}({paramDecls});";
	}

	/// <summary>
	/// Builds the sync delegate type declaration for an overload signature.
	/// Returns null if the overload is not async or has 0 params.
	/// </summary>
	public static string? BuildOverloadSyncDelegateDeclaration(MethodOverloadSignature overload)
	{
		if (overload.Parameters.Count == 0) return null;

		var (innerType, isAsyncTaskT, isAsyncValueTaskT) = GetAsyncTypeInfo(overload.ReturnType);
		var (isVoidTask, isVoidValueTask) = GetVoidAsyncInfo(overload.ReturnType);
		var isAsync = isAsyncTaskT || isAsyncValueTaskT || isVoidTask || isVoidValueTask;

		if (!isAsync) return null;

		// Derive the sync delegate name from the overload's DelegateName
		// DelegateName is e.g. "TransformAsync_String_CancellationTokenDelegate"
		// We need "TransformAsync_String_CancellationTokenSyncDelegate"
		var syncDelegateName = overload.DelegateName.EndsWith("Delegate")
			? overload.DelegateName.Substring(0, overload.DelegateName.Length - "Delegate".Length) + "SyncDelegate"
			: overload.DelegateName + "Sync";

		var paramDecls = string.Join(", ", overload.Parameters.Select(p => $"{p.RefPrefix}{p.Type} {p.EscapedName}"));

		string syncReturnType;
		if (overload.IsVoid || isVoidTask || isVoidValueTask)
			syncReturnType = "void";
		else
			syncReturnType = innerType;

		return $"public delegate {syncReturnType} {syncDelegateName}({paramDecls});";
	}

	/// <summary>
	/// Computes the sync delegate name for an overload, derived from the overload's DelegateName.
	/// Returns null if the overload is not async or has 0 params.
	/// </summary>
	public static string? ComputeOverloadSyncDelegateName(MethodOverloadSignature overload)
	{
		if (overload.Parameters.Count == 0) return null;

		var (_, isAsyncTaskT, isAsyncValueTaskT) = GetAsyncTypeInfo(overload.ReturnType);
		var (isVoidTask, isVoidValueTask) = GetVoidAsyncInfo(overload.ReturnType);
		var isAsync = isAsyncTaskT || isAsyncValueTaskT || isVoidTask || isVoidValueTask;

		if (!isAsync) return null;

		return overload.DelegateName.EndsWith("Delegate")
			? overload.DelegateName.Substring(0, overload.DelegateName.Length - "Delegate".Length) + "SyncDelegate"
			: overload.DelegateName + "Sync";
	}

	/// <summary>
	/// Builds the delegate type declaration for a method.
	/// Returns the full declaration string (e.g., "public delegate int AddDelegate(int a, int b);").
	/// For async methods, the delegate returns Task/Task&lt;T&gt; (the interceptor's stored delegate type).
	/// For ValueTask/ValueTask&lt;T&gt; methods, the delegate returns Task/Task&lt;T&gt;.
	/// Optional delegateBaseName overrides methodName for the delegate name (useful when interceptor name differs from method name).
	/// </summary>
	public static string BuildDelegateDeclaration(string methodName, IEnumerable<ParameterModel> parameters, string returnType, bool isVoid, string? delegateBaseName = null)
	{
		var delegateName = ComputeDelegateTypeName(delegateBaseName ?? methodName);
		var paramDecls = string.Join(", ", parameters.Select(p => $"{p.RefPrefix}{p.Type} {p.EscapedName}"));

		// Determine the delegate return type
		string returnTypeStr;
		if (isVoid)
		{
			returnTypeStr = "void";
		}
		else
		{
			var (innerType, isAsyncTaskT, isAsyncValueTaskT) = GetAsyncTypeInfo(returnType);
			var (isVoidTask, isVoidValueTask) = GetVoidAsyncInfo(returnType);

			if (isVoidTask || isVoidValueTask)
			{
				// Async void: delegate returns Task
				returnTypeStr = "global::System.Threading.Tasks.Task";
			}
			else if (isAsyncTaskT || isAsyncValueTaskT)
			{
				// Async with return: delegate returns Task<TReturn>
				returnTypeStr = $"global::System.Threading.Tasks.Task<{innerType}>";
			}
			else
			{
				// Sync: use the raw return type
				returnTypeStr = returnType;
			}
		}

		return $"public delegate {returnTypeStr} {delegateName}({paramDecls});";
	}

	/// <summary>
	/// Builds the delegate type declaration for an overload signature.
	/// Uses the overload's DelegateName (which is already suffixed for uniqueness).
	/// Returns the full declaration string.
	/// </summary>
	public static string BuildOverloadDelegateDeclaration(MethodOverloadSignature overload)
	{
		var paramDecls = string.Join(", ", overload.Parameters.Select(p => $"{p.RefPrefix}{p.Type} {p.EscapedName}"));

		// For async interceptors, the delegate must return the async type (Task<T>/Task),
		// not the inner type. Determine the actual delegate return type.
		var (innerType, isAsyncTaskT, isAsyncValueTaskT) = GetAsyncTypeInfo(overload.ReturnType);
		var (isVoidTask, isVoidValueTask) = GetVoidAsyncInfo(overload.ReturnType);

		string returnTypeStr;
		if (overload.IsVoid)
		{
			returnTypeStr = "void";
		}
		else if (isVoidTask || isVoidValueTask)
		{
			// Async void: delegate returns Task (the interceptor's stored delegate type)
			returnTypeStr = "global::System.Threading.Tasks.Task";
		}
		else if (isAsyncTaskT || isAsyncValueTaskT)
		{
			// Async with return: delegate returns Task<TReturn>
			returnTypeStr = $"global::System.Threading.Tasks.Task<{innerType}>";
		}
		else
		{
			returnTypeStr = overload.ReturnType;
		}

		return $"public delegate {returnTypeStr} {overload.DelegateName}({paramDecls});";
	}

	/// <summary>
	/// Gets the fully qualified pre-compiled interceptor type for a property.
	/// </summary>
	public static string GetPropertyInterceptorType(UnifiedPropertyInterceptorModel model)
	{
		if (model.HasGetter && model.HasSetter)
			return $"global::KnockOff.Interceptors.PropertyGetSetInterceptor<{model.ValueType}>";
		if (model.HasGetter)
			return $"global::KnockOff.Interceptors.PropertyGetInterceptor<{model.ValueType}>";
		return $"global::KnockOff.Interceptors.PropertySetInterceptor<{model.ValueType}>";
	}

	/// <summary>
	/// Gets the fully qualified pre-compiled interceptor type for an indexer.
	/// </summary>
	public static string GetIndexerInterceptorType(UnifiedIndexerInterceptorModel model)
	{
		return $"global::KnockOff.Interceptors.IndexerGetSetInterceptor<{model.KeyType}, {model.ValueType}>";
	}

	// ========================================================================
	// Invoke expression generation
	// ========================================================================

	/// <summary>
	/// Generates the interface implementation body for a pre-compiled method interceptor.
	/// Returns the full statement (e.g., "return Add.Invoke(Strict, (a, b));").
	/// For 0 params: no args. For 1 param: raw value. For 2+ params: tuple literal.
	/// </summary>
	public static string GetMethodInvokeExpression(
		string interceptorName,
		string returnType,
		bool isVoid,
		IEnumerable<ParameterModel> parameters,
		string strictExpression)
	{
		var paramList = parameters.ToList();
		var paramArgs = FormatInvokeArgs(paramList);

		var (_, isAsyncTaskT, isAsyncValueTaskT) = GetAsyncTypeInfo(returnType);
		var (isVoidTask, isVoidValueTask) = GetVoidAsyncInfo(returnType);

		if (isVoid)
		{
			return $"{interceptorName}.Invoke({strictExpression}{paramArgs});";
		}

		if (isVoidTask)
		{
			// Task -> direct return of Task from AsyncVoidMethodInterceptor.Invoke
			return $"return {interceptorName}.Invoke({strictExpression}{paramArgs});";
		}

		if (isVoidValueTask)
		{
			// ValueTask -> wrap the Task from AsyncVoidMethodInterceptor.Invoke
			return $"return new global::System.Threading.Tasks.ValueTask({interceptorName}.Invoke({strictExpression}{paramArgs}));";
		}

		if (isAsyncTaskT)
		{
			// Task<T> -> direct return of Task<T> from AsyncMethodInterceptor.Invoke
			return $"return {interceptorName}.Invoke({strictExpression}{paramArgs});";
		}

		if (isAsyncValueTaskT)
		{
			// ValueTask<T> -> wrap the Task<T> from AsyncMethodInterceptor.Invoke
			return $"return new {returnType}({interceptorName}.Invoke({strictExpression}{paramArgs}));";
		}

		// Sync non-void
		return $"return {interceptorName}.Invoke({strictExpression}{paramArgs});";
	}

	/// <summary>
	/// Formats the invoke arguments for TTuple interceptors.
	/// 0 params: empty string. 1 param: ", paramName". 2+ params: ", (a, b, c)".
	/// </summary>
	private static string FormatInvokeArgs(List<ParameterModel> paramList)
	{
		if (paramList.Count == 0)
			return "";
		if (paramList.Count == 1)
			return $", {paramList[0].EscapedName}";
		// 2+ params: wrap in tuple literal
		return ", (" + string.Join(", ", paramList.Select(p => p.EscapedName)) + ")";
	}

	/// <summary>
	/// Wraps a cleaned argument list string for TTuple Invoke calls.
	/// For 0 args (empty string): returns "". For 1 arg (no commas): returns ", arg".
	/// For 2+ args: returns ", (arg1, arg2, ...)".
	/// </summary>
	public static string WrapInvokeArgs(string cleanArgs)
	{
		if (string.IsNullOrEmpty(cleanArgs))
			return "";
		// Count commas to determine param count
		if (!cleanArgs.Contains(','))
			return $", {cleanArgs}";
		// 2+ params: wrap in tuple literal
		return $", ({cleanArgs})";
	}

	// ========================================================================
	// Source delegation expression generation
	// ========================================================================

	/// <summary>
	/// Generates the SetSourceFallback call for a pre-compiled method interceptor.
	/// For TTuple types (1+ params), always uses lambdas since SetSourceFallback takes TDelegate.
	/// For zero-param types, uses method groups where possible.
	/// Optional delegateBaseName overrides methodName for delegate type naming.
	/// </summary>
	public static string GetMethodSourceFallbackExpression(
		string interceptorName,
		string methodName,
		string sourceParamName,
		IEnumerable<ParameterModel> parameters,
		string returnType,
		bool isVoid,
		string? declaringInterface = null,
		string? delegateBaseName = null)
	{
		var paramList = parameters.ToList();
		var paramTypes = paramList.Select(p => p.Type).ToList();

		var (innerType, isAsyncTaskT, isAsyncValueTaskT) = GetAsyncTypeInfo(returnType);
		var (isVoidTask, isVoidValueTask) = GetVoidAsyncInfo(returnType);

		// TTuple types (1+ params): SetSourceFallback takes TDelegate (generated delegate type).
		// Always use lambdas — the lambda's parameter types match the generated delegate.
		if (paramList.Count > 0)
		{
			return GetMethodSourceFallbackLambdaExpression(
				interceptorName, methodName, sourceParamName, paramList, paramTypes,
				returnType, isVoid, innerType, isAsyncValueTaskT, isVoidValueTask, declaringInterface, delegateBaseName);
		}

		// Zero-param methods: delegate type is Action/Func (unchanged from arity-0 types)
		var hasInParams = paramList.Any(p => p.RefKind == Microsoft.CodeAnalysis.RefKind.In);
		var needsLambda = isAsyncValueTaskT || isVoidValueTask || hasInParams || declaringInterface != null;

		if (needsLambda)
		{
			return GetMethodSourceFallbackLambdaExpression(
				interceptorName, methodName, sourceParamName, paramList, paramTypes,
				returnType, isVoid, innerType, isAsyncValueTaskT, isVoidValueTask, declaringInterface);
		}

		// Use explicit delegate construction to avoid "target-typed conditional expression" errors.
		var delegateType = GetDelegateType(paramTypes, returnType, isVoid);

		return $"{interceptorName}.SetSourceFallback({sourceParamName} != null ? new {delegateType}({sourceParamName}.{methodName}) : null);";
	}

	/// <summary>
	/// Generates a wrapping lambda for source delegation when a method group cannot be used directly.
	/// Handles ValueTask-to-Task conversion, `in` parameter forwarding, and diamond inheritance disambiguation.
	/// </summary>
	private static string GetMethodSourceFallbackLambdaExpression(
		string interceptorName,
		string methodName,
		string sourceParamName,
		List<ParameterModel> paramList,
		List<string> paramTypes,
		string returnType,
		bool isVoid,
		string innerType,
		bool isAsyncValueTaskT,
		bool isVoidValueTask,
		string? declaringInterface,
		string? delegateBaseName = null)
	{
		// Build lambda parameter list
		// If any parameter has 'in' ref kind, we need typed parameters in the lambda
		var hasInParams = paramList.Any(p => p.RefKind == Microsoft.CodeAnalysis.RefKind.In);
		var lambdaParams = paramList.Count > 0
			? hasInParams
				? string.Join(", ", paramList.Select(p => p.RefKind == Microsoft.CodeAnalysis.RefKind.In ? $"in {p.Type} {p.EscapedName}" : p.EscapedName))
				: string.Join(", ", paramList.Select(p => p.EscapedName))
			: "";

		// Build the source invocation with optional interface cast
		var sourceExpr = declaringInterface != null
			? $"(({declaringInterface}){sourceParamName})"
			: sourceParamName;
		var argList = paramList.Count > 0
			? string.Join(", ", paramList.Select(p => p.EscapedName))
			: "";
		var invocation = $"{sourceExpr}.{methodName}({argList})";

		// For TTuple types (1+ params), cast the lambda to the delegate type to help target-typed conditional
		var delegateTypeName = paramList.Count > 0 ? ComputeDelegateTypeName(delegateBaseName ?? methodName) : null;

		// Determine the lambda body based on return type
		if (isVoidValueTask)
		{
			// ValueTask -> need async lambda: async (args) => await source.Method(args)
			// SetSourceFallback expects Func<..., Task>, async lambda returns Task
			var asyncLambda = paramList.Count > 0
				? $"async ({lambdaParams}) => await {invocation}"
				: $"async () => await {invocation}";
			if (delegateTypeName != null)
				return $"{interceptorName}.SetSourceFallback({sourceParamName} != null ? ({delegateTypeName})(({asyncLambda})) : null);";
			return $"{interceptorName}.SetSourceFallback({sourceParamName} != null ? {asyncLambda} : null);";
		}
		else if (isAsyncValueTaskT)
		{
			// ValueTask<T> -> need async lambda: async (args) => await source.Method(args)
			// SetSourceFallback expects Func<..., Task<T>>, async lambda returns Task<T>
			var asyncLambda = paramList.Count > 0
				? $"async ({lambdaParams}) => await {invocation}"
				: $"async () => await {invocation}";
			if (delegateTypeName != null)
				return $"{interceptorName}.SetSourceFallback({sourceParamName} != null ? ({delegateTypeName})(({asyncLambda})) : null);";
			return $"{interceptorName}.SetSourceFallback({sourceParamName} != null ? {asyncLambda} : null);";
		}
		else if (isVoid)
		{
			// Void method with `in` params or disambiguation
			var lambdaExpr = paramList.Count > 0
				? $"({lambdaParams}) => {invocation}"
				: $"() => {invocation}";
			if (delegateTypeName != null)
				return $"{interceptorName}.SetSourceFallback({sourceParamName} != null ? ({delegateTypeName})({lambdaExpr}) : null);";
			return $"{interceptorName}.SetSourceFallback({sourceParamName} != null ? {lambdaExpr} : null);";
		}
		else
		{
			// Non-void sync or Task<T>/Task with `in` params or disambiguation
			var lambdaExpr = paramList.Count > 0
				? $"({lambdaParams}) => {invocation}"
				: $"() => {invocation}";
			if (delegateTypeName != null)
				return $"{interceptorName}.SetSourceFallback({sourceParamName} != null ? ({delegateTypeName})({lambdaExpr}) : null);";
			return $"{interceptorName}.SetSourceFallback({sourceParamName} != null ? {lambdaExpr} : null);";
		}
	}

	/// <summary>
	/// Gets the fully-qualified delegate type (Action or Func) for a method signature.
	/// </summary>
	private static string GetDelegateType(List<string> paramTypes, string returnType, bool isVoid)
	{
		if (isVoid)
		{
			if (paramTypes.Count == 0)
				return "global::System.Action";
			return $"global::System.Action<{string.Join(", ", paramTypes)}>";
		}
		else
		{
			if (paramTypes.Count == 0)
				return $"global::System.Func<{returnType}>";
			return $"global::System.Func<{string.Join(", ", paramTypes)}, {returnType}>";
		}
	}

	/// <summary>
	/// Generates the SetSourceFallback call for a pre-compiled property interceptor.
	/// </summary>
	public static string GetPropertySourceFallbackExpression(
		string interceptorName,
		string propertyName,
		string sourceParamName,
		bool hasGetter,
		bool hasSetter)
	{
		if (hasGetter && hasSetter)
		{
			return $"{interceptorName}.SetSourceFallback({sourceParamName} != null ? () => {sourceParamName}.{propertyName} : null, {sourceParamName} != null ? (value) => {sourceParamName}.{propertyName} = value : null);";
		}
		if (hasGetter)
		{
			return $"{interceptorName}.SetSourceFallback({sourceParamName} != null ? () => {sourceParamName}.{propertyName} : null);";
		}
		// Set-only
		return $"{interceptorName}.SetSourceFallback({sourceParamName} != null ? (value) => {sourceParamName}.{propertyName} = value : null);";
	}

	/// <summary>
	/// Generates the SetSourceFallback call for a pre-compiled indexer interceptor.
	/// </summary>
	public static string GetIndexerSourceFallbackExpression(
		string interceptorName,
		string sourceParamName,
		string declaringInterface,
		string keyParamName,
		bool hasGetter,
		bool hasSetter)
	{
		if (hasGetter && hasSetter)
		{
			return $"{interceptorName}.SetSourceFallback({sourceParamName} != null ? ({keyParamName}) => (({declaringInterface}){sourceParamName})[{keyParamName}] : null, {sourceParamName} != null ? ({keyParamName}, value) => (({declaringInterface}){sourceParamName})[{keyParamName}] = value : null);";
		}
		if (hasGetter)
		{
			return $"{interceptorName}.SetGetSourceFallback({sourceParamName} != null ? ({keyParamName}) => (({declaringInterface}){sourceParamName})[{keyParamName}] : null);";
		}
		return $"{interceptorName}.SetSetSourceFallback({sourceParamName} != null ? ({keyParamName}, value) => (({declaringInterface}){sourceParamName})[{keyParamName}] = value : null);";
	}

	/// <summary>
	/// Generates a null-clearing SetSourceFallback call.
	/// For TTuple types (1+ params), SetSourceFallback takes TDelegate? (single overload), so null is unambiguous.
	/// For zero-param async interceptors, cast null to the async delegate type to disambiguate overloads.
	/// </summary>
	public static string GetMethodSourceFallbackClearExpression(
		string interceptorName,
		IEnumerable<ParameterModel> parameters,
		string returnType,
		bool isVoid)
	{
		var paramList = parameters.ToList();

		// TTuple types (1+ params): SetSourceFallback takes TDelegate? (single overload).
		// null is unambiguous.
		if (paramList.Count > 0)
		{
			return $"{interceptorName}.SetSourceFallback(null);";
		}

		// Zero-param types: check for async ambiguity
		var (innerType, isAsyncTaskT, isAsyncValueTaskT) = GetAsyncTypeInfo(returnType);
		var (isVoidTask, isVoidValueTask) = GetVoidAsyncInfo(returnType);
		var isAsync = isAsyncTaskT || isAsyncValueTaskT || isVoidTask || isVoidValueTask;

		if (!isAsync)
		{
			// Sync methods: only one SetSourceFallback overload, null is unambiguous
			return $"{interceptorName}.SetSourceFallback(null);";
		}

		// Zero-param async methods: cast null to the async delegate type to disambiguate
		if (isVoidTask || isVoidValueTask)
		{
			string funcType = "global::System.Func<global::System.Threading.Tasks.Task>";
			return $"{interceptorName}.SetSourceFallback(({funcType}?)null);";
		}
		else
		{
			string funcType = $"global::System.Func<global::System.Threading.Tasks.Task<{innerType}>>";
			return $"{interceptorName}.SetSourceFallback(({funcType}?)null);";
		}
	}

	// ========================================================================
	// Stub override fallback generation
	// ========================================================================

	/// <summary>
	/// Generates a SetFallback call for wiring a stub override method to a pre-compiled interceptor.
	/// Used in constructor generation for stub override patterns.
	/// For TTuple types (1+ params), SetFallback takes TDelegate.
	/// Method groups implicitly convert to the generated delegate type.
	/// For ValueTask returns, a wrapping async lambda is needed.
	/// </summary>
	public static string GetStubOverrideFallbackExpression(
		string interceptorName,
		string stubOverrideName,
		string returnType = "",
		IEnumerable<ParameterModel>? parameters = null)
	{
		// For ValueTask/ValueTask<T> returns, the stub override method returns ValueTask
		// but SetFallback expects a delegate returning Task<T> or Task.
		// We need a wrapping async lambda to convert.
		var (_, _, isAsyncValueTaskT) = GetAsyncTypeInfo(returnType);
		var (_, isVoidValueTask) = GetVoidAsyncInfo(returnType);

		if (isAsyncValueTaskT || isVoidValueTask)
		{
			var paramList = parameters?.ToList() ?? new List<ParameterModel>();
			var lambdaParams = paramList.Count > 0
				? string.Join(", ", paramList.Select(p => p.EscapedName))
				: "";
			var argList = paramList.Count > 0
				? string.Join(", ", paramList.Select(p => p.EscapedName))
				: "";

			var asyncLambda = paramList.Count > 0
				? $"async ({lambdaParams}) => await {stubOverrideName}({argList})"
				: $"async () => await {stubOverrideName}()";
			return $"{interceptorName}.SetFallback({asyncLambda});";
		}

		// Method groups implicitly convert to both Func<>/Action<> and generated delegate types
		return $"{interceptorName}.SetFallback({stubOverrideName});";
	}

	/// <summary>
	/// Generates a SetFallback call for wiring a stub override property.
	/// </summary>
	public static string GetPropertyStubOverrideFallbackExpression(
		string interceptorName,
		string propertyName,
		bool hasGetter,
		bool hasSetter)
	{
		if (hasGetter && hasSetter)
		{
			return $"{interceptorName}.SetFallback(() => {propertyName}_, (value) => {propertyName}_ = value);";
		}
		if (hasGetter)
		{
			return $"{interceptorName}.SetFallback(() => {propertyName}_);";
		}
		return $"{interceptorName}.SetFallback((value) => {propertyName}_ = value);";
	}

	// ========================================================================
	// Helpers
	// ========================================================================

	/// <summary>Public accessor for GetAsyncTypeInfo for use by renderers.</summary>
	public static (string ValueStorageType, bool IsTaskT, bool IsValueTaskT) GetAsyncTypeInfoPublic(string returnType)
		=> GetAsyncTypeInfo(returnType);

	/// <summary>Public accessor for GetVoidAsyncInfo for use by renderers.</summary>
	public static (bool IsTask, bool IsValueTask) GetVoidAsyncInfoPublic(string returnType)
		=> GetVoidAsyncInfo(returnType);

	private static (string ValueStorageType, bool IsTaskT, bool IsValueTaskT) GetAsyncTypeInfo(string returnType)
	{
		const string TaskPrefix = "global::System.Threading.Tasks.Task<";
		const string ValueTaskPrefix = "global::System.Threading.Tasks.ValueTask<";

		if (returnType.StartsWith(TaskPrefix) && returnType.EndsWith(">"))
		{
			var innerType = returnType.Substring(TaskPrefix.Length, returnType.Length - TaskPrefix.Length - 1);
			return (innerType, true, false);
		}

		if (returnType.StartsWith(ValueTaskPrefix) && returnType.EndsWith(">"))
		{
			var innerType = returnType.Substring(ValueTaskPrefix.Length, returnType.Length - ValueTaskPrefix.Length - 1);
			return (innerType, false, true);
		}

		return (returnType, false, false);
	}

	private static (bool IsTask, bool IsValueTask) GetVoidAsyncInfo(string returnType)
	{
		if (returnType == "global::System.Threading.Tasks.Task")
			return (true, false);
		if (returnType == "global::System.Threading.Tasks.ValueTask")
			return (false, true);
		return (false, false);
	}

	// ========================================================================
	// Smart default factory expression generation
	// ========================================================================

	/// <summary>
	/// Gets the smart default factory expression for a method's pre-compiled interceptor constructor.
	/// Returns null if no factory is needed (strategy is Default — interceptor already returns default!).
	/// For ThrowException, returns a lambda that throws InvalidOperationException.
	/// For NewInstance, returns a lambda that creates a new instance.
	/// For async methods (Task&lt;T&gt;/ValueTask&lt;T&gt;), the factory operates on the inner type,
	/// not the wrapper — the async interceptor handles wrapping.
	/// </summary>
	public static string? GetMethodSmartDefaultFactory(
		string returnType,
		bool isVoid,
		DefaultValueStrategy strategy,
		string? concreteType,
		string memberName)
	{
		// Void methods have no return value, no factory needed
		if (isVoid) return null;

		// Check for void-async (Task, ValueTask) - no return value
		var (isVoidTask, isVoidValueTask) = GetVoidAsyncInfo(returnType);
		if (isVoidTask || isVoidValueTask) return null;

		// Default strategy: interceptor returns default! already
		if (strategy == DefaultValueStrategy.Default) return null;

		// For async methods, get the inner type (the factory operates on inner type)
		var (innerType, isAsyncTaskT, isAsyncValueTaskT) = GetAsyncTypeInfo(returnType);
		var effectiveType = (isAsyncTaskT || isAsyncValueTaskT) ? innerType : returnType;
		var effectiveConcreteType = concreteType; // concreteType is already for the inner type

		if (strategy == DefaultValueStrategy.ThrowException)
		{
			return $"() => throw new global::System.InvalidOperationException(\"No implementation provided for {memberName}\")";
		}

		// NewInstance
		var typeToNew = effectiveConcreteType ?? effectiveType;
		return $"() => new {typeToNew}()";
	}

	/// <summary>
	/// Gets the smart default factory expression for a property's pre-compiled interceptor constructor.
	/// Returns null if no factory is needed (strategy is Default or ThrowException).
	/// Properties never throw on unconfigured non-strict access (matching old generated interceptor behavior).
	/// Only NewInstance strategies produce a factory.
	/// </summary>
	public static string? GetPropertySmartDefaultFactory(
		string valueType,
		DefaultValueStrategy strategy,
		string? concreteType,
		string memberName)
	{
		// Properties return default! for both Default and ThrowException in non-strict mode.
		// ThrowException on properties is ignored because the old generated property interceptors
		// always returned default! in non-strict mode (ThrowsOnDefault only applied to methods).
		if (strategy != DefaultValueStrategy.NewInstance) return null;

		// NewInstance
		var typeToNew = concreteType ?? valueType;
		return $"() => new {typeToNew}()";
	}

	/// <summary>
	/// Gets the smart default factory expression for an indexer's pre-compiled interceptor constructor.
	/// Returns null if no factory is needed (strategy is Default or ThrowException).
	/// Indexers never throw on unconfigured non-strict access (matching old generated interceptor behavior).
	/// Only NewInstance strategies produce a factory.
	/// </summary>
	public static string? GetIndexerSmartDefaultFactory(
		string valueType,
		DefaultValueStrategy strategy,
		string? concreteType,
		string memberName)
	{
		// Indexers return default! for both Default and ThrowException in non-strict mode.
		// ThrowException on indexers is ignored because the old generated indexer interceptors
		// always returned default! in non-strict mode (ThrowsOnDefault only applied to methods).
		if (strategy != DefaultValueStrategy.NewInstance) return null;

		// NewInstance
		var typeToNew = concreteType ?? valueType;
		return $"() => new {typeToNew}()";
	}

	/// <summary>
	/// Generates the full constructor arguments for a pre-compiled interceptor field declaration.
	/// Returns the arguments portion including parentheses (e.g., '("Name")' or '("Name", () => new List())').
	/// </summary>
	public static string GetFieldConstructorArgs(string memberName, string? factoryExpression)
	{
		if (factoryExpression == null)
			return $"(\"{memberName}\")";
		return $"(\"{memberName}\", {factoryExpression})";
	}
}
