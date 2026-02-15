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
	/// Returns false for overload groups (those need compositor handling), ref/out,
	/// ref returns, and >8 params.
	/// </summary>
	public static bool CanUsePreCompiled(UnifiedMethodInterceptorModel model)
	{
		// Overload groups need compositor classes
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
	/// Gets the fully qualified pre-compiled interceptor type for a method.
	/// Determines the correct type family (sync/void/async/async-void) and arity.
	/// </summary>
	public static string GetMethodInterceptorType(UnifiedMethodInterceptorModel model)
	{
		var paramCount = model.Parameters.Count;
		var paramTypes = string.Join(", ", model.Parameters.Select(p => p.Type));

		var (innerType, isAsyncTaskT, isAsyncValueTaskT) = GetAsyncTypeInfo(model.ReturnType);
		var isAsyncWithInnerType = isAsyncTaskT || isAsyncValueTaskT;
		var (isVoidTask, isVoidValueTask) = GetVoidAsyncInfo(model.ReturnType);
		var isVoidAsync = isVoidTask || isVoidValueTask;

		if (model.IsVoid)
		{
			// void -> VoidMethodInterceptorN<T1,...,TN>
			if (paramCount == 0)
				return "global::KnockOff.Interceptors.VoidMethodInterceptor0";
			return $"global::KnockOff.Interceptors.VoidMethodInterceptor{paramCount}<{paramTypes}>";
		}

		if (isVoidAsync)
		{
			// Task/ValueTask -> AsyncVoidMethodInterceptorN<T1,...,TN>
			if (paramCount == 0)
				return "global::KnockOff.Interceptors.AsyncVoidMethodInterceptor0";
			return $"global::KnockOff.Interceptors.AsyncVoidMethodInterceptor{paramCount}<{paramTypes}>";
		}

		if (isAsyncWithInnerType)
		{
			// Task<T>/ValueTask<T> -> AsyncMethodInterceptorN<T1,...,TN,TReturn>
			var allTypeArgs = paramCount > 0 ? $"{paramTypes}, {innerType}" : innerType;
			return $"global::KnockOff.Interceptors.AsyncMethodInterceptor{paramCount}<{allTypeArgs}>";
		}

		// Non-void sync -> MethodInterceptorN<T1,...,TN,TReturn>
		{
			var allTypeArgs = paramCount > 0 ? $"{paramTypes}, {model.ReturnType}" : model.ReturnType;
			return $"global::KnockOff.Interceptors.MethodInterceptor{paramCount}<{allTypeArgs}>";
		}
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
	/// Returns the full statement (e.g., "return Add.Invoke(Strict, a, b);").
	/// </summary>
	public static string GetMethodInvokeExpression(
		string interceptorName,
		string returnType,
		bool isVoid,
		IEnumerable<ParameterModel> parameters,
		string strictExpression)
	{
		var paramArgs = parameters.Any()
			? ", " + string.Join(", ", parameters.Select(p => p.EscapedName))
			: "";

		var (_, isAsyncTaskT, isAsyncValueTaskT) = GetAsyncTypeInfo(returnType);
		var (isVoidTask, isVoidValueTask) = GetVoidAsyncInfo(returnType);

		if (isVoid)
		{
			return $"{interceptorName}.Invoke({strictExpression}{paramArgs});";
		}

		if (isVoidTask)
		{
			// Task -> direct return of Task from AsyncVoidMethodInterceptorN.Invoke
			return $"return {interceptorName}.Invoke({strictExpression}{paramArgs});";
		}

		if (isVoidValueTask)
		{
			// ValueTask -> wrap the Task from AsyncVoidMethodInterceptorN.Invoke
			return $"return new global::System.Threading.Tasks.ValueTask({interceptorName}.Invoke({strictExpression}{paramArgs}));";
		}

		if (isAsyncTaskT)
		{
			// Task<T> -> direct return of Task<T> from AsyncMethodInterceptorN.Invoke
			return $"return {interceptorName}.Invoke({strictExpression}{paramArgs});";
		}

		if (isAsyncValueTaskT)
		{
			// ValueTask<T> -> wrap the Task<T> from AsyncMethodInterceptorN.Invoke
			return $"return new {returnType}({interceptorName}.Invoke({strictExpression}{paramArgs}));";
		}

		// Sync non-void
		return $"return {interceptorName}.Invoke({strictExpression}{paramArgs});";
	}

	// ========================================================================
	// Source delegation expression generation
	// ========================================================================

	/// <summary>
	/// Generates the SetSourceFallback call for a pre-compiled method interceptor.
	/// Uses method groups where possible; falls back to lambdas when needed.
	/// </summary>
	public static string GetMethodSourceFallbackExpression(
		string interceptorName,
		string methodName,
		string sourceParamName,
		IEnumerable<ParameterModel> parameters,
		string returnType,
		bool isVoid,
		string? declaringInterface = null)
	{
		var paramList = parameters.ToList();
		var paramTypes = paramList.Select(p => p.Type).ToList();

		// Check if we need a wrapping lambda instead of a method group.
		// Lambdas are needed for:
		// 1. ValueTask/ValueTask<T> returns - SetSourceFallback expects Task/Task<T>-based delegates
		// 2. `in` parameters - method groups with `in` params don't match Action/Func delegates
		// 3. Diamond inheritance disambiguation - need to cast to specific interface
		var (innerType, _, isAsyncValueTaskT) = GetAsyncTypeInfo(returnType);
		var (_, isVoidValueTask) = GetVoidAsyncInfo(returnType);
		var hasInParams = paramList.Any(p => p.RefKind == Microsoft.CodeAnalysis.RefKind.In);
		var needsLambda = isAsyncValueTaskT || isVoidValueTask || hasInParams || declaringInterface != null;

		if (needsLambda)
		{
			return GetMethodSourceFallbackLambdaExpression(
				interceptorName, methodName, sourceParamName, paramList, paramTypes,
				returnType, isVoid, innerType, isAsyncValueTaskT, isVoidValueTask, declaringInterface);
		}

		// Use explicit delegate construction to avoid "target-typed conditional expression" errors.
		// Method groups and lambdas in ternary with null cause CS1503/CS0121.
		// Using `new DelegateType(methodGroup)` provides an explicit type for the ternary.
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
		string? declaringInterface)
	{
		// Build lambda parameter list (simple names, no types)
		var lambdaParams = paramList.Count > 0
			? string.Join(", ", paramList.Select(p => p.EscapedName))
			: "";

		// Build the source invocation with optional interface cast
		var sourceExpr = declaringInterface != null
			? $"(({declaringInterface}){sourceParamName})"
			: sourceParamName;
		var argList = paramList.Count > 0
			? string.Join(", ", paramList.Select(p => p.EscapedName))
			: "";
		var invocation = $"{sourceExpr}.{methodName}({argList})";

		// Determine the lambda body based on return type
		if (isVoidValueTask)
		{
			// ValueTask -> need async lambda: async (args) => await source.Method(args)
			// SetSourceFallback expects Func<..., Task>, async lambda returns Task
			var asyncLambda = paramList.Count > 0
				? $"async ({lambdaParams}) => await {invocation}"
				: $"async () => await {invocation}";
			return $"{interceptorName}.SetSourceFallback({sourceParamName} != null ? {asyncLambda} : null);";
		}
		else if (isAsyncValueTaskT)
		{
			// ValueTask<T> -> need async lambda: async (args) => await source.Method(args)
			// SetSourceFallback expects Func<..., Task<T>>, async lambda returns Task<T>
			var asyncLambda = paramList.Count > 0
				? $"async ({lambdaParams}) => await {invocation}"
				: $"async () => await {invocation}";
			return $"{interceptorName}.SetSourceFallback({sourceParamName} != null ? {asyncLambda} : null);";
		}
		else if (isVoid)
		{
			// Void method with `in` params or disambiguation
			var lambdaExpr = paramList.Count > 0
				? $"({lambdaParams}) => {invocation}"
				: $"() => {invocation}";
			return $"{interceptorName}.SetSourceFallback({sourceParamName} != null ? {lambdaExpr} : null);";
		}
		else
		{
			// Non-void sync or Task<T>/Task with `in` params or disambiguation
			var lambdaExpr = paramList.Count > 0
				? $"({lambdaParams}) => {invocation}"
				: $"() => {invocation}";
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
	/// Generates a null-clearing SetSourceFallback call with proper cast to avoid ambiguity for async interceptors.
	/// For async method interceptors, both SetSourceFallback(Func&lt;..., Task&lt;T&gt;&gt;?) and SetSourceFallback(Func&lt;..., T&gt;?)
	/// accept null, causing CS0121. This generates a cast to the async form.
	/// </summary>
	public static string GetMethodSourceFallbackClearExpression(
		string interceptorName,
		IEnumerable<ParameterModel> parameters,
		string returnType,
		bool isVoid)
	{
		var paramList = parameters.ToList();
		var (innerType, isAsyncTaskT, isAsyncValueTaskT) = GetAsyncTypeInfo(returnType);
		var (isVoidTask, isVoidValueTask) = GetVoidAsyncInfo(returnType);
		var isAsync = isAsyncTaskT || isAsyncValueTaskT || isVoidTask || isVoidValueTask;

		if (!isAsync)
		{
			// Sync methods: only one SetSourceFallback overload, null is unambiguous
			return $"{interceptorName}.SetSourceFallback(null);";
		}

		// Async methods: cast null to the async delegate type to disambiguate
		var paramTypes = paramList.Select(p => p.Type).ToList();
		if (isVoidTask || isVoidValueTask)
		{
			// Async void: Func<T1,...,TN, Task>
			string funcType;
			if (paramList.Count == 0)
				funcType = "global::System.Func<global::System.Threading.Tasks.Task>";
			else
				funcType = $"global::System.Func<{string.Join(", ", paramTypes)}, global::System.Threading.Tasks.Task>";
			return $"{interceptorName}.SetSourceFallback(({funcType}?)null);";
		}
		else
		{
			// Async with return: Func<T1,...,TN, Task<TReturn>>
			string funcType;
			if (paramList.Count == 0)
				funcType = $"global::System.Func<global::System.Threading.Tasks.Task<{innerType}>>";
			else
				funcType = $"global::System.Func<{string.Join(", ", paramTypes)}, global::System.Threading.Tasks.Task<{innerType}>>";
			return $"{interceptorName}.SetSourceFallback(({funcType}?)null);";
		}
	}

	// ========================================================================
	// Stub override fallback generation
	// ========================================================================

	/// <summary>
	/// Generates a SetFallback call for wiring a stub override method to a pre-compiled interceptor.
	/// Used in constructor generation for stub override patterns.
	/// </summary>
	public static string GetStubOverrideFallbackExpression(
		string interceptorName,
		string stubOverrideName,
		string returnType = "",
		IEnumerable<ParameterModel>? parameters = null)
	{
		// For ValueTask/ValueTask<T> returns, the stub override method returns ValueTask
		// but SetFallback expects Func<..., Task<T>> or Func<..., T>.
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
	// Overload group rendering: thin compositor class
	// ========================================================================

	/// <summary>
	/// Determines whether an overload group can use pre-compiled interceptors for ALL its individual overloads.
	/// If any individual overload needs fallback (ref/out, ref return, >8 params), the entire group
	/// falls back to the existing generated-class approach.
	/// </summary>
	public static bool CanOverloadGroupUsePreCompiled(UnifiedMethodInterceptorModel model)
	{
		if (model.Overloads.Count == 0) return false; // Not an overload group

		foreach (var overload in model.Overloads)
		{
			// Check each overload individually
			if (overload.Parameters.Any(p => p.RefKind == Microsoft.CodeAnalysis.RefKind.Ref || p.RefKind == Microsoft.CodeAnalysis.RefKind.Out))
				return false;
			if (overload.IsRefReturn) return false;
			if (overload.Parameters.Count > 8) return false;
		}

		return true;
	}

	/// <summary>
	/// Gets the pre-compiled interceptor type for a single overload signature.
	/// </summary>
	public static string GetOverloadInterceptorType(MethodOverloadSignature overload)
	{
		var paramCount = overload.Parameters.Count;
		var paramTypes = string.Join(", ", overload.Parameters.Select(p => p.Type));

		var (innerType, isAsyncTaskT, isAsyncValueTaskT) = GetAsyncTypeInfo(overload.ReturnType);
		var isAsyncWithInnerType = isAsyncTaskT || isAsyncValueTaskT;
		var (isVoidTask, isVoidValueTask) = GetVoidAsyncInfo(overload.ReturnType);
		var isVoidAsync = isVoidTask || isVoidValueTask;

		if (overload.IsVoid)
		{
			if (paramCount == 0) return "global::KnockOff.Interceptors.VoidMethodInterceptor0";
			return $"global::KnockOff.Interceptors.VoidMethodInterceptor{paramCount}<{paramTypes}>";
		}

		if (isVoidAsync)
		{
			if (paramCount == 0) return "global::KnockOff.Interceptors.AsyncVoidMethodInterceptor0";
			return $"global::KnockOff.Interceptors.AsyncVoidMethodInterceptor{paramCount}<{paramTypes}>";
		}

		if (isAsyncWithInnerType)
		{
			var allTypeArgs = paramCount > 0 ? $"{paramTypes}, {innerType}" : innerType;
			return $"global::KnockOff.Interceptors.AsyncMethodInterceptor{paramCount}<{allTypeArgs}>";
		}

		{
			var allTypeArgs = paramCount > 0 ? $"{paramTypes}, {overload.ReturnType}" : overload.ReturnType;
			return $"global::KnockOff.Interceptors.MethodInterceptor{paramCount}<{allTypeArgs}>";
		}
	}

	/// <summary>
	/// Renders a thin overload compositor class containing pre-compiled interceptor fields.
	/// The compositor delegates Return/Call/When/Verify to the appropriate inner interceptor.
	/// </summary>
	public static void RenderOverloadCompositorClass(
		CodeWriter w,
		UnifiedMethodInterceptorModel model,
		InterceptorRenderOptions options)
	{
		var typeParams = options.InterceptorTypeParameters;
		var constraints = options.InterceptorConstraints;

		w.Line($"/// <summary>Compositor for overloaded {model.MethodName}. Delegates to per-signature interceptors.</summary>");
		using (w.Block($"public sealed class {model.InterceptorClassName}{typeParams}{constraints}"))
		{
			// Inner interceptor fields
			for (int i = 0; i < model.Overloads.Count; i++)
			{
				var overload = model.Overloads.GetArray()![i];
				var fieldType = GetOverloadInterceptorType(overload);
				w.Line($"internal {fieldType} _ov{i + 1} = new(\"{model.MethodName}\");");
			}
			w.Line();

			// Invoke methods for interface implementation routing (suffixed by signature)
			for (int i = 0; i < model.Overloads.Count; i++)
			{
				var overload = model.Overloads.GetArray()![i];
				RenderOverloadInvokeMethod(w, overload, i + 1, options);
			}

			// Expose per-overload Return/Call methods (compiler resolves by lambda arity)
			for (int i = 0; i < model.Overloads.Count; i++)
			{
				var overload = model.Overloads.GetArray()![i];
				RenderOverloadReturnMethod(w, overload, i + 1);
			}

			// When methods (deduplicated by parameter types - overloads with same params but different return types share one When)
			var renderedWhenSignatures = new HashSet<string>();
			for (int i = 0; i < model.Overloads.Count; i++)
			{
				var overload = model.Overloads.GetArray()![i];
				if (overload.Parameters.Count == 0) continue;
				var paramSig = string.Join(",", overload.Parameters.Select(p => p.Type));
				if (renderedWhenSignatures.Add(paramSig))
				{
					// Collect all overload indices with matching param types to call When on all of them
					var matchingIndices = new List<int>();
					for (int j = 0; j < model.Overloads.Count; j++)
					{
						var otherOverload = model.Overloads.GetArray()![j];
						var otherSig = string.Join(",", otherOverload.Parameters.Select(p => p.Type));
						if (otherSig == paramSig)
							matchingIndices.Add(j + 1);
					}
					RenderOverloadWhenMethodDeduplicated(w, overload, matchingIndices);
					RenderOverloadWhenPredicateMethodDeduplicated(w, overload, matchingIndices);
				}
			}

			// Aggregated Verify
			w.Line("/// <summary>Verifies method was called at least once across all overloads.</summary>");
			w.Line("public void Verify() => Verify(global::KnockOff.Called.AtLeastOnce);");
			w.Line();
			w.Line("/// <summary>Verifies call count satisfies the Called constraint across all overloads.</summary>");
			using (w.Block("public void Verify(global::KnockOff.Called times)"))
			{
				w.Line("var total = " + string.Join(" + ", Enumerable.Range(1, model.Overloads.Count).Select(i => $"_ov{i}.TotalCallCount")) + ";");
				w.Line("if (!times.Validate(total))");
				w.Line($"\tthrow new global::KnockOff.VerificationException(new global::KnockOff.VerificationFailure(\"{model.MethodName}\", times, total));");
			}
			w.Line();

			// Aggregated Verifiable
			w.Line("/// <summary>Marks all overloads for verification by Stub.Verify().</summary>");
			using (w.Block("public void Verifiable()"))
			{
				for (int i = 1; i <= model.Overloads.Count; i++)
					w.Line($"_ov{i}.Verifiable();");
			}
			w.Line();

			w.Line("/// <summary>Marks all overloads for verification by Stub.Verify() with Called constraint.</summary>");
			using (w.Block("public void Verifiable(global::KnockOff.Called times)"))
			{
				for (int i = 1; i <= model.Overloads.Count; i++)
					w.Line($"_ov{i}.Verifiable(times);");
			}
			w.Line();

			// IsVerifiable
			w.Line("/// <summary>Whether any overload interceptor was marked verifiable.</summary>");
			w.Line("public bool IsVerifiable => " + string.Join(" || ", Enumerable.Range(1, model.Overloads.Count).Select(i => $"_ov{i}.IsVerifiable")) + ";");
			w.Line();

			// IsConfigured
			w.Line("/// <summary>Whether any overload interceptor was configured.</summary>");
			w.Line("public bool IsConfigured => " + string.Join(" || ", Enumerable.Range(1, model.Overloads.Count).Select(i => $"_ov{i}.IsConfigured")) + ";");
			w.Line();

			// TotalCallCount
			w.Line("/// <summary>Total call count across all overloads.</summary>");
			w.Line("public int TotalCallCount => " + string.Join(" + ", Enumerable.Range(1, model.Overloads.Count).Select(i => $"_ov{i}.TotalCallCount")) + ";");
			w.Line();

			// UnconfiguredCallCount
			w.Line("/// <summary>Unconfigured call count across all overloads.</summary>");
			w.Line("public int UnconfiguredCallCount => " + string.Join(" + ", Enumerable.Range(1, model.Overloads.Count).Select(i => $"_ov{i}.UnconfiguredCallCount")) + ";");
			w.Line();

			// CheckVerification
			w.Line("/// <summary>Checks verification for Stub.Verify().</summary>");
			using (w.Block("public global::KnockOff.VerificationFailure? CheckVerification()"))
			{
				for (int i = 1; i <= model.Overloads.Count; i++)
				{
					w.Line($"if (_ov{i}.CheckVerification() is {{ }} f{i}) return f{i};");
				}
				w.Line("return null;");
			}
			w.Line();

			// CheckVerificationAll - check each configured overload individually
			w.Line("/// <summary>Checks verification for Stub.VerifyAll(). Each configured overload must be called at least once.</summary>");
			using (w.Block("public global::KnockOff.VerificationFailure? CheckVerificationAll()"))
			{
				w.Line("if (!IsConfigured) return null;");
				for (int i = 1; i <= model.Overloads.Count; i++)
				{
					w.Line($"if (_ov{i}.IsConfigured && !global::KnockOff.Called.AtLeastOnce.Validate(_ov{i}.TotalCallCount))");
					w.Line($"\treturn new global::KnockOff.VerificationFailure(\"{model.MethodName}\", global::KnockOff.Called.AtLeastOnce, _ov{i}.TotalCallCount);");
				}
				w.Line("return null;");
			}
			w.Line();

			// Reset
			w.Line("/// <summary>Resets all overload interceptors.</summary>");
			using (w.Block("public void Reset()"))
			{
				for (int i = 1; i <= model.Overloads.Count; i++)
					w.Line($"_ov{i}.Reset();");
			}
		}
		w.Line();
	}

	private static void RenderOverloadReturnMethod(CodeWriter w, MethodOverloadSignature overload, int overloadIndex)
	{
		var (innerType, isAsyncTaskT, isAsyncValueTaskT) = GetAsyncTypeInfo(overload.ReturnType);
		var isAsyncWithInnerType = isAsyncTaskT || isAsyncValueTaskT;
		var (isVoidTask, isVoidValueTask) = GetVoidAsyncInfo(overload.ReturnType);
		var isVoidAsync = isVoidTask || isVoidValueTask;

		var paramTypes = string.Join(", ", overload.Parameters.Select(p => p.Type));
		var builderReturnType = GetCallBuilderType(overload);

		if (overload.IsVoid || isVoidAsync)
		{
			// Void: Call(Action<T1,...,TN>) or Call(Func<T1,...,TN,Task>)
			string callbackType;
			if (isVoidAsync)
			{
				if (overload.Parameters.Count == 0)
					callbackType = "global::System.Func<global::System.Threading.Tasks.Task>";
				else
					callbackType = $"global::System.Func<{paramTypes}, global::System.Threading.Tasks.Task>";
			}
			else
			{
				if (overload.Parameters.Count == 0)
					callbackType = "global::System.Action";
				else
					callbackType = $"global::System.Action<{paramTypes}>";
			}
			w.Line($"/// <summary>Configures callback for {overload.Parameters.Count}-param overload.</summary>");
			w.Line($"public {builderReturnType} Call({callbackType} callback) => _ov{overloadIndex}.Call(callback);");
			w.Line();
		}
		else
		{
			// Non-void: Return(Func<T1,...,TN,TReturn>)
			string returnType;
			if (isAsyncWithInnerType)
			{
				// For async, expose both simplified and full callback
				var simplifiedType = overload.Parameters.Count > 0
					? $"global::System.Func<{paramTypes}, {innerType}>"
					: $"global::System.Func<{innerType}>";
				w.Line($"/// <summary>Configures simplified callback for {overload.Parameters.Count}-param async overload.</summary>");
				w.Line($"public {builderReturnType} Return({simplifiedType} callback) => _ov{overloadIndex}.Return(callback);");
				w.Line();

				var asyncType = overload.Parameters.Count > 0
					? $"global::System.Func<{paramTypes}, global::System.Threading.Tasks.Task<{innerType}>>"
					: $"global::System.Func<global::System.Threading.Tasks.Task<{innerType}>>";
				w.Line($"/// <summary>Configures async callback for {overload.Parameters.Count}-param async overload.</summary>");
				w.Line($"public {builderReturnType} Return({asyncType} asyncCallback) => _ov{overloadIndex}.Return(asyncCallback);");
				w.Line();
			}
			else
			{
				returnType = overload.Parameters.Count > 0
					? $"global::System.Func<{paramTypes}, {overload.ReturnType}>"
					: $"global::System.Func<{overload.ReturnType}>";
				w.Line($"/// <summary>Configures callback for {overload.Parameters.Count}-param overload.</summary>");
				w.Line($"public {builderReturnType} Return({returnType} callback) => _ov{overloadIndex}.Return(callback);");
				w.Line();
			}
		}
	}

	private static void RenderOverloadWhenMethod(CodeWriter w, MethodOverloadSignature overload, int overloadIndex)
	{
		if (overload.Parameters.Count == 0) return; // No When for zero-param overloads

		var paramDecls = string.Join(", ", overload.Parameters.Select(p => $"{p.Type} {p.EscapedName}"));
		var paramArgs = string.Join(", ", overload.Parameters.Select(p => p.EscapedName));

		w.Line($"/// <summary>When matcher for {overload.Parameters.Count}-param overload.</summary>");
		w.Line($"public void When({paramDecls}) => _ov{overloadIndex}.When({paramArgs});");
		w.Line();
	}

	/// <summary>
	/// Renders a When method that calls When on all matching overload indices.
	/// Used when multiple overloads share the same parameter types (e.g., ISet.Add(string)->bool and ICollection.Add(string)->void).
	/// </summary>
	private static void RenderOverloadWhenMethodDeduplicated(CodeWriter w, MethodOverloadSignature overload, List<int> matchingIndices)
	{
		var paramDecls = string.Join(", ", overload.Parameters.Select(p => $"{p.Type} {p.EscapedName}"));
		var paramArgs = string.Join(", ", overload.Parameters.Select(p => p.EscapedName));
		var whenReturnType = GetWhenBuilderType(overload);

		w.Line($"/// <summary>When matcher for {overload.Parameters.Count}-param overload.</summary>");
		if (matchingIndices.Count == 1)
		{
			w.Line($"public {whenReturnType} When({paramDecls}) => _ov{matchingIndices[0]}.When({paramArgs});");
		}
		else
		{
			// When multiple overloads share params, call When on all but return the first one's builder
			using (w.Block($"public {whenReturnType} When({paramDecls})"))
			{
				for (int i = 1; i < matchingIndices.Count; i++)
				{
					w.Line($"_ov{matchingIndices[i]}.When({paramArgs});");
				}
				w.Line($"return _ov{matchingIndices[0]}.When({paramArgs});");
			}
		}
		w.Line();
	}

	/// <summary>
	/// Renders a predicate-based When method that calls When on all matching overload indices.
	/// Generates When(Func&lt;T1,...,TN,bool&gt; predicate) overloads for lambda-based matching.
	/// </summary>
	private static void RenderOverloadWhenPredicateMethodDeduplicated(CodeWriter w, MethodOverloadSignature overload, List<int> matchingIndices)
	{
		var paramTypes = string.Join(", ", overload.Parameters.Select(p => p.Type));
		var predicateType = $"global::System.Func<{paramTypes}, bool>";
		var whenReturnType = GetWhenBuilderType(overload);

		w.Line($"/// <summary>Predicate-based When matcher for {overload.Parameters.Count}-param overload.</summary>");
		if (matchingIndices.Count == 1)
		{
			w.Line($"public {whenReturnType} When({predicateType} predicate) => _ov{matchingIndices[0]}.When(predicate);");
		}
		else
		{
			// When multiple overloads share params, call When on all but return the first one's builder
			using (w.Block($"public {whenReturnType} When({predicateType} predicate)"))
			{
				for (int i = 1; i < matchingIndices.Count; i++)
				{
					w.Line($"_ov{matchingIndices[i]}.When(predicate);");
				}
				w.Line($"return _ov{matchingIndices[0]}.When(predicate);");
			}
		}
		w.Line();
	}

	/// <summary>
	/// Renders an Invoke method for a specific overload within the compositor.
	/// The suffix matches the signature suffix used by the method implementation to route calls.
	/// Handles async wrapping (ValueTask from Task) as needed.
	/// </summary>
	private static void RenderOverloadInvokeMethod(
		CodeWriter w,
		MethodOverloadSignature overload,
		int overloadIndex,
		InterceptorRenderOptions options)
	{
		var suffix = overload.SignatureSuffix;
		var strictParam = options.IncludeStrictParameter ? "bool strict" : "";
		var strictArg = options.IncludeStrictParameter ? options.StrictAccessExpression : "";
		var paramDecls = overload.Parameters.Count > 0
			? (options.IncludeStrictParameter ? ", " : "") + string.Join(", ", overload.Parameters.Select(p => $"{p.Type} {p.EscapedName}"))
			: "";
		var paramArgs = overload.Parameters.Count > 0
			? ", " + string.Join(", ", overload.Parameters.Select(p => p.EscapedName))
			: "";

		var (innerType, isAsyncTaskT, isAsyncValueTaskT) = GetAsyncTypeInfo(overload.ReturnType);
		var (isVoidTask, isVoidValueTask) = GetVoidAsyncInfo(overload.ReturnType);

		if (overload.IsVoid)
		{
			w.Line($"internal void Invoke_{suffix}({strictParam}{paramDecls}) => _ov{overloadIndex}.Invoke({strictArg}{paramArgs});");
		}
		else if (isVoidTask)
		{
			// Task -> direct delegation
			w.Line($"internal {overload.ReturnType} Invoke_{suffix}({strictParam}{paramDecls}) => _ov{overloadIndex}.Invoke({strictArg}{paramArgs});");
		}
		else if (isVoidValueTask)
		{
			// ValueTask -> wrap Task from AsyncVoidMethodInterceptorN
			w.Line($"internal {overload.ReturnType} Invoke_{suffix}({strictParam}{paramDecls}) => new global::System.Threading.Tasks.ValueTask(_ov{overloadIndex}.Invoke({strictArg}{paramArgs}));");
		}
		else if (isAsyncTaskT)
		{
			// Task<T> -> direct delegation
			w.Line($"internal {overload.ReturnType} Invoke_{suffix}({strictParam}{paramDecls}) => _ov{overloadIndex}.Invoke({strictArg}{paramArgs});");
		}
		else if (isAsyncValueTaskT)
		{
			// ValueTask<T> -> wrap Task<T> from AsyncMethodInterceptorN
			w.Line($"internal {overload.ReturnType} Invoke_{suffix}({strictParam}{paramDecls}) => new {overload.ReturnType}(_ov{overloadIndex}.Invoke({strictArg}{paramArgs}));");
		}
		else
		{
			// Sync non-void -> direct delegation
			w.Line($"internal {overload.ReturnType} Invoke_{suffix}({strictParam}{paramDecls}) => _ov{overloadIndex}.Invoke({strictArg}{paramArgs});");
		}
		w.Line();
	}

	// ========================================================================
	// Builder/When return type computation for compositors
	// ========================================================================

	/// <summary>
	/// Gets the fully-qualified MethodCallBuilder type returned by Return()/Call() on the pre-compiled interceptor.
	/// </summary>
	public static string GetCallBuilderType(MethodOverloadSignature overload)
	{
		var interceptorType = GetOverloadInterceptorType(overload);
		var paramCount = overload.Parameters.Count;
		return $"{interceptorType}.MethodCallBuilder{paramCount}";
	}

	/// <summary>
	/// Gets the fully-qualified WhenBuilder type returned by When() on the pre-compiled interceptor.
	/// </summary>
	public static string GetWhenBuilderType(MethodOverloadSignature overload)
	{
		var interceptorType = GetOverloadInterceptorType(overload);
		var paramCount = overload.Parameters.Count;

		var (_, isAsyncTaskT, isAsyncValueTaskT) = GetAsyncTypeInfo(overload.ReturnType);
		var isAsyncWithInnerType = isAsyncTaskT || isAsyncValueTaskT;
		var (isVoidTask, isVoidValueTask) = GetVoidAsyncInfo(overload.ReturnType);
		var isVoidAsync = isVoidTask || isVoidValueTask;

		// Void and async-void interceptors use VoidWhenBuilderN
		if (overload.IsVoid || isVoidAsync)
			return $"{interceptorType}.VoidWhenBuilder{paramCount}";

		// Non-void (sync and async non-void) use WhenBuilderN
		return $"{interceptorType}.WhenBuilder{paramCount}";
	}

	// ========================================================================
	// Helpers (borrowed from MethodInterceptorRenderer)
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
