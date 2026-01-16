// src/Generator/Builder/FlatModelBuilder.cs
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using KnockOff.Model.Flat;
using KnockOff.Model.Shared;
using Microsoft.CodeAnalysis;

namespace KnockOff.Builder;

/// <summary>
/// Transforms KnockOffTypeInfo into FlatGenerationUnit.
/// All decision logic for "what to generate" lives here.
/// </summary>
internal static class FlatModelBuilder
{
	/// <summary>
	/// Suffix added to generic interceptor property names when a method group has both generic and non-generic overloads.
	/// </summary>
	private const string GenericSuffix = "Generic";

	public static FlatGenerationUnit Build(KnockOffTypeInfo typeInfo)
	{
		// Build name map for collision resolution
		var nameMap = BuildNameMap(typeInfo.FlatMembers, typeInfo.FlatEvents, typeInfo.UserMethods);

		// Group methods for overload handling
		var methodGroups = GroupMethodsByName(typeInfo.FlatMembers.Where(m => !m.IsProperty && !m.IsIndexer));

		// Count indexers for naming
		var indexerCount = SymbolHelpers.CountIndexers(typeInfo.FlatMembers);

		// Build models
		var className = typeInfo.ClassName + SymbolHelpers.FormatTypeParameterList(typeInfo.TypeParameters);
		var properties = BuildPropertyModels(typeInfo, nameMap, className);
		var indexers = BuildIndexerModels(typeInfo, nameMap, indexerCount, className);
		var (methods, genericHandlers) = BuildMethodModels(typeInfo, nameMap, methodGroups, className);
		var events = BuildEventModels(typeInfo, nameMap);

		// Group non-generic methods by interceptor name for multi-overload support
		var flatMethodGroups = methods
			.Where(m => !m.IsGenericMethod && m.UserMethodCall == null)
			.GroupBy(m => m.InterceptorName)
			.Select(g => new FlatMethodGroup(
				InterceptorName: g.Key,
				InterceptorClassName: g.First().InterceptorClassName,
				NeedsNewKeyword: g.Any(m => m.NeedsNewKeyword),
				Methods: new EquatableArray<FlatMethodModel>(g.ToArray())))
			.ToList();

		// Build containing types models
		var containingTypes = typeInfo.ContainingTypes.Select(ct => new ContainingTypeModel(
			Keyword: ct.Keyword,
			Name: ct.Name,
			AccessModifier: ct.AccessibilityModifier)).ToEquatableArray();

		return new FlatGenerationUnit(
			ClassName: typeInfo.ClassName,
			Namespace: typeInfo.Namespace,
			InterfaceList: typeInfo.Interfaces.Select(i => i.FullName).ToEquatableArray(),
			TypeParameters: BuildTypeParameters(typeInfo.TypeParameters),
			ContainingTypes: containingTypes,
			Properties: properties,
			Indexers: indexers,
			Methods: methods,
			MethodGroups: new EquatableArray<FlatMethodGroup>(flatMethodGroups.ToArray()),
			GenericMethodHandlers: genericHandlers,
			Events: events,
			HasGenericMethods: genericHandlers.Count > 0 || methods.Any(m => m.IsGenericMethod),
			ImplementsIKnockOffStub: true,
			Strict: typeInfo.Strict);
	}

	#region Name Map Building

	/// <summary>
	/// Builds a map from member keys to collision-safe interceptor names.
	/// For example, if two interfaces both have "Value" property, they map to "Value" and "Value2".
	/// Also avoids conflicts with user-defined methods.
	/// </summary>
	private static Dictionary<string, string> BuildNameMap(
		EquatableArray<InterfaceMemberInfo> flatMembers,
		EquatableArray<EventMemberInfo> flatEvents,
		EquatableArray<UserMethodInfo> userMethods)
	{
		var nameMap = new Dictionary<string, string>();
		var usedNames = new HashSet<string>();

		// Reserve names used by user methods to avoid conflicts
		foreach (var userMethod in userMethods)
		{
			usedNames.Add(userMethod.Name);
		}

		// Count indexers to determine naming strategy
		var indexerCount = SymbolHelpers.CountIndexers(flatMembers);

		// Process properties and indexers
		foreach (var member in flatMembers)
		{
			if (member.IsProperty || member.IsIndexer)
			{
				var key = GetMemberKey(member);
				// For indexers, compute name based on count (single: Indexer, multiple: IndexerString, IndexerInt)
				var baseName = member.IsIndexer
					? SymbolHelpers.GetIndexerName(indexerCount, member.IndexerTypeSuffix)
					: member.Name;
				var finalName = GetUniqueInterceptorName(baseName, usedNames);
				nameMap[key] = finalName;
				usedNames.Add(finalName);
			}
		}

		// Process methods - group by name first
		var methodGroups = flatMembers
			.Where(m => !m.IsProperty && !m.IsIndexer)
			.GroupBy(m => m.Name)
			.ToDictionary(g => g.Key, g => g.ToList());

		foreach (var group in methodGroups)
		{
			var methodName = group.Key;
			var overloads = group.Value;

			// Check for mixed groups (both generic and non-generic overloads)
			var genericOverloads = overloads.Where(m => m.IsGenericMethod).ToList();
			var nonGenericOverloads = overloads.Where(m => !m.IsGenericMethod).ToList();
			var isMixed = genericOverloads.Count > 0 && nonGenericOverloads.Count > 0;

			if (isMixed)
			{
				// Mixed group: handle non-generic and generic overloads separately

				// Non-generic overloads get numbered suffixes if multiple, otherwise base name
				if (nonGenericOverloads.Count == 1)
				{
					var key = GetMemberKey(nonGenericOverloads[0]);
					var finalName = GetUniqueInterceptorName(methodName, usedNames);
					nameMap[key] = finalName;
					usedNames.Add(finalName);
				}
				else
				{
					for (int i = 0; i < nonGenericOverloads.Count; i++)
					{
						var key = GetMemberKey(nonGenericOverloads[i]);
						var suffix = (i + 1).ToString();
						var baseName = $"{methodName}{suffix}";
						var finalName = GetUniqueInterceptorName(baseName, usedNames);
						nameMap[key] = finalName;
						usedNames.Add(finalName);
					}
				}

				// Generic overloads use a handler with Generic suffix
				var genericName = methodName + GenericSuffix;
				var genericKey = $"method:{methodName}()_generic";
				var genericFinalName = GetUniqueInterceptorName(genericName, usedNames);
				nameMap[genericKey] = genericFinalName;
				usedNames.Add(genericFinalName);

				// Also map each generic overload to the generic handler
				foreach (var overload in genericOverloads)
				{
					var key = GetMemberKey(overload);
					nameMap[key] = genericFinalName;
				}
			}
			else if (genericOverloads.Count > 0)
			{
				// All generic - use a single base handler with Of<T>() pattern
				var genericKey = $"method:{methodName}()_generic";
				var finalName = GetUniqueInterceptorName(methodName, usedNames);
				nameMap[genericKey] = finalName;
				usedNames.Add(finalName);

				// Also add entries for each overload (for implementation lookup)
				foreach (var overload in overloads)
				{
					var key = GetMemberKey(overload);
					nameMap[key] = finalName; // All overloads use same base interceptor name
				}
			}
			else if (overloads.Count == 1)
			{
				// Single method - no overload suffix
				var key = GetMemberKey(overloads[0]);
				var finalName = GetUniqueInterceptorName(methodName, usedNames);
				nameMap[key] = finalName;
				usedNames.Add(finalName);
			}
			else
			{
				// Multiple overloads - use numbered suffixes
				for (int i = 0; i < overloads.Count; i++)
				{
					var key = GetMemberKey(overloads[i]);
					var suffix = (i + 1).ToString();
					var baseName = $"{methodName}{suffix}";
					var finalName = GetUniqueInterceptorName(baseName, usedNames);
					nameMap[key] = finalName;
					usedNames.Add(finalName);
				}
			}
		}

		// Process events
		foreach (var evt in flatEvents)
		{
			var key = $"event:{evt.Name}";
			var finalName = GetUniqueInterceptorName(evt.Name, usedNames);
			nameMap[key] = finalName;
			usedNames.Add(finalName);
		}

		return nameMap;
	}

	/// <summary>
	/// Gets a unique name that doesn't conflict with already used names.
	/// </summary>
	private static string GetUniqueInterceptorName(string baseName, HashSet<string> usedNames)
	{
		if (!usedNames.Contains(baseName))
			return baseName;

		int suffix = 2;
		while (usedNames.Contains($"{baseName}{suffix}"))
			suffix++;

		return $"{baseName}{suffix}";
	}

	/// <summary>
	/// Creates a unique key for a member to identify it in the name map.
	/// </summary>
	private static string GetMemberKey(InterfaceMemberInfo member)
	{
		if (member.IsProperty || member.IsIndexer)
		{
			// For properties/indexers, key is name + indexer params
			var indexerParams = member.IndexerParameters.Count > 0
				? $"[{string.Join(",", member.IndexerParameters.Select(p => p.Type))}]"
				: "";
			return $"prop:{member.Name}{indexerParams}";
		}
		else
		{
			// For methods, key is name + parameter types
			var paramTypes = string.Join(",", member.Parameters.Select(p => p.Type));
			return $"method:{member.Name}({paramTypes})";
		}
	}

	#endregion

	#region Property Model Building

	private static EquatableArray<FlatPropertyModel> BuildPropertyModels(
		KnockOffTypeInfo typeInfo,
		Dictionary<string, string> nameMap,
		string className)
	{
		var properties = new List<FlatPropertyModel>();
		var generatedImplementations = new HashSet<string>();

		// Iterate over ALL interfaces for implementations (not just FlatMembers)
		foreach (var iface in typeInfo.Interfaces)
		{
			foreach (var member in iface.Members)
			{
				if (!member.IsProperty || member.IsIndexer)
					continue;

				// Create unique key for this specific interface implementation
				var implKey = $"{member.DeclaringInterfaceFullName}.{member.Name}";
				if (!generatedImplementations.Add(implKey))
					continue; // Skip duplicates

				var key = GetMemberKey(member);
				var interceptorName = nameMap[key];
				var interceptorClassName = $"{interceptorName}Interceptor";

				var defaultExpr = member.IsInitOnly
					? "default!"
					: GetDefaultValueForProperty(member.ReturnType, member.DefaultStrategy, member.ConcreteTypeForNew);

				var setterPragmaDisable = GetSetterNullabilityAttribute(member);
				var setterPragmaRestore = GetSetterNullabilityRestore(member);

				var userMethod = FindUserMethod(typeInfo.UserMethods, member);
				var simpleIfaceName = ExtractSimpleTypeName(member.DeclaringInterfaceFullName);

				// Check for property delegation (IProperty.Value (object) -> IProperty<T>.Value (T))
				var (delegationTarget, delegationInterface) = FindPropertyDelegationTarget(member, typeInfo.Interfaces);

				properties.Add(new FlatPropertyModel(
					InterceptorName: interceptorName,
					InterceptorClassName: interceptorClassName,
					DeclaringInterface: member.DeclaringInterfaceFullName,
					MemberName: member.Name,
					ReturnType: member.ReturnType,
					NullableReturnType: MakeNullable(member.ReturnType),
					HasGetter: member.HasGetter,
					HasSetter: member.HasSetter,
					IsInitOnly: member.IsInitOnly,
					DefaultExpression: defaultExpr,
					SetterPragmaDisable: string.IsNullOrEmpty(setterPragmaDisable) ? null : setterPragmaDisable,
					SetterPragmaRestore: string.IsNullOrEmpty(setterPragmaRestore) ? null : setterPragmaRestore,
					SimpleInterfaceName: simpleIfaceName,
					UserMethodName: userMethod?.Name,
					NeedsNewKeyword: NeedsNewKeyword(interceptorName),
					DelegationTarget: delegationTarget,
					DelegationTargetInterface: delegationInterface));
			}
		}

		return properties.ToEquatableArray();
	}

	/// <summary>
	/// Finds a property delegation target for properties that should delegate to a typed counterpart.
	/// For example, IProperty.Value (object) delegates to IProperty&lt;T&gt;.Value (T).
	/// </summary>
	private static (InterfaceMemberInfo? Target, string? TargetInterface) FindPropertyDelegationTarget(
		InterfaceMemberInfo member,
		EquatableArray<InterfaceInfo> interfaces)
	{
		// Only delegate from object type to specific type
		var memberType = member.ReturnType.TrimEnd('?');
		if (memberType != "object" && memberType != "System.Object" && memberType != "global::System.Object")
			return (null, null);

		// Look for a property with the same name but different (more specific) type
		foreach (var iface in interfaces)
		{
			foreach (var candidate in iface.Members)
			{
				// Skip if this is the same member
				if (candidate.DeclaringInterfaceFullName == member.DeclaringInterfaceFullName &&
				    candidate.Name == member.Name &&
				    candidate.ReturnType == member.ReturnType)
					continue;

				// Must be a property with the same name
				if (!candidate.IsProperty || candidate.Name != member.Name)
					continue;

				// Must have different return type
				if (candidate.ReturnType == member.ReturnType)
					continue;

				// Found a typed property to delegate to
				return (candidate, iface.FullName);
			}
		}

		return (null, null);
	}

	/// <summary>
	/// Finds a method delegation target for methods that should delegate to a typed counterpart.
	/// For example, IRule.RunRule(IValidateBase) delegates to IRule&lt;T&gt;.RunRule(T).
	/// Also handles return type covariance: IEnumerable.GetEnumerator() delegates to IEnumerable&lt;T&gt;.GetEnumerator().
	/// </summary>
	private static (InterfaceMemberInfo? Target, string? TargetInterface) FindMethodDelegationTarget(
		InterfaceMemberInfo member,
		EquatableArray<InterfaceInfo> interfaces)
	{
		// Only applies to methods
		if (member.IsProperty || member.IsIndexer)
			return (null, null);

		// Look for a method with the same name but different (more specific) parameter or return types
		foreach (var iface in interfaces)
		{
			foreach (var candidate in iface.Members)
			{
				// Skip if this is the same member
				if (candidate.DeclaringInterfaceFullName == member.DeclaringInterfaceFullName &&
				    candidate.Name == member.Name &&
				    AreSameParameters(candidate.Parameters, member.Parameters))
					continue;

				// Must be a method with the same name
				if (candidate.IsProperty || candidate.IsIndexer || candidate.Name != member.Name)
					continue;

				// Must have same number of parameters
				if (candidate.Parameters.Count != member.Parameters.Count)
					continue;

				var sameParams = AreSameParameters(candidate.Parameters, member.Parameters);

				if (sameParams)
				{
					// Parameters are identical - check for return type covariance
					// This handles cases like IEnumerable.GetEnumerator() -> IEnumerable<T>.GetEnumerator()
					if (IsReturnTypeCovariantDelegation(member, candidate))
					{
						return (candidate, iface.FullName);
					}
				}
				else
				{
					// Parameters differ - check if the base method uses base types (IValidateBase)
					// and candidate uses specific types (T)
					if (IsBaseToTypedDelegation(member, candidate))
					{
						return (candidate, iface.FullName);
					}
				}
			}
		}

		return (null, null);
	}

	/// <summary>
	/// Checks if two parameter lists are the same.
	/// </summary>
	private static bool AreSameParameters(EquatableArray<ParameterInfo> a, EquatableArray<ParameterInfo> b)
	{
		if (a.Count != b.Count)
			return false;

		var aArray = a.GetArray() ?? Array.Empty<ParameterInfo>();
		var bArray = b.GetArray() ?? Array.Empty<ParameterInfo>();

		for (int i = 0; i < aArray.Length; i++)
		{
			if (aArray[i].Type != bArray[i].Type)
				return false;
		}

		return true;
	}

	/// <summary>
	/// Checks if a method should delegate from base to typed version.
	/// Returns true if member's parameter types are "base" types (like IValidateBase, object)
	/// and candidate's parameters are more specific.
	/// </summary>
	private static bool IsBaseToTypedDelegation(InterfaceMemberInfo member, InterfaceMemberInfo candidate)
	{
		var memberParams = member.Parameters.GetArray() ?? Array.Empty<ParameterInfo>();
		var candidateParams = candidate.Parameters.GetArray() ?? Array.Empty<ParameterInfo>();

		// Check if at least one parameter changes from a base type
		for (int i = 0; i < memberParams.Length; i++)
		{
			var memberType = memberParams[i].Type;
			var candidateType = candidateParams[i].Type;

			// If the types are different and the member type looks like a base interface
			// (ends with "Base" or is "object"), this might be a delegation case
			if (memberType != candidateType)
			{
				var normalizedMemberType = memberType.Replace("global::", "").TrimEnd('?');
				if (normalizedMemberType.EndsWith("Base") ||
				    normalizedMemberType == "object" ||
				    normalizedMemberType == "System.Object")
				{
					return true;
				}
			}
		}

		return false;
	}

	/// <summary>
	/// Checks if a method should delegate based on return type covariance.
	/// Returns true when the member's return type is a non-generic base of the candidate's generic return type.
	/// For example: IEnumerator vs IEnumerator&lt;T&gt;, IEnumerable vs IEnumerable&lt;T&gt;.
	/// </summary>
	private static bool IsReturnTypeCovariantDelegation(InterfaceMemberInfo member, InterfaceMemberInfo candidate)
	{
		var memberReturn = member.ReturnType.Replace("global::", "").TrimEnd('?');
		var candidateReturn = candidate.ReturnType.Replace("global::", "").TrimEnd('?');

		// Return types must be different
		if (memberReturn == candidateReturn)
			return false;

		// Look for pattern: member returns non-generic, candidate returns generic version
		// e.g., IEnumerator vs IEnumerator<string>, IEnumerable vs IEnumerable<int>
		// The generic version will contain '<' while the non-generic won't

		// Member should not be generic, candidate should be generic
		if (memberReturn.Contains('<') || !candidateReturn.Contains('<'))
			return false;

		// Extract the base type name from the candidate (before the '<')
		var candidateBaseName = candidateReturn.Substring(0, candidateReturn.IndexOf('<'));

		// Check if member's type matches the base of candidate's type
		// Handle both full namespace and simple name matching
		// E.g., "System.Collections.IEnumerator" matches "System.Collections.Generic.IEnumerator<T>"
		// Also handle case where member might be simpler: "IEnumerator" matches candidate base "IEnumerator"

		// Get simple name from member type
		var memberSimpleName = memberReturn.Contains('.')
			? memberReturn.Substring(memberReturn.LastIndexOf('.') + 1)
			: memberReturn;

		// Get simple name from candidate base
		var candidateSimpleName = candidateBaseName.Contains('.')
			? candidateBaseName.Substring(candidateBaseName.LastIndexOf('.') + 1)
			: candidateBaseName;

		// The simple names should match for covariant delegation
		// E.g., "IEnumerator" == "IEnumerator" (from IEnumerator<T>)
		return memberSimpleName == candidateSimpleName;
	}

	#endregion

	#region Indexer Model Building

	private static EquatableArray<FlatIndexerModel> BuildIndexerModels(
		KnockOffTypeInfo typeInfo,
		Dictionary<string, string> nameMap,
		int indexerCount,
		string className)
	{
		var indexers = new List<FlatIndexerModel>();
		var generatedImplementations = new HashSet<string>();

		// Iterate over ALL interfaces for implementations
		foreach (var iface in typeInfo.Interfaces)
		{
			foreach (var member in iface.Members)
			{
				if (!member.IsIndexer)
					continue;

				// Create unique key for this specific interface implementation
				var paramTypes = member.IndexerParameters.Select(p => p.Type);
				var implKey = $"{member.DeclaringInterfaceFullName}.this[{string.Join(",", paramTypes)}]";
				if (!generatedImplementations.Add(implKey))
					continue; // Skip duplicates

				var key = GetMemberKey(member);
				var interceptorName = nameMap[key];
				var interceptorClassName = $"{interceptorName}Interceptor";

				var keyType = member.IndexerParameters.Count > 0
					? member.IndexerParameters.GetArray()![0].Type
					: "object";
				var keyParamName = member.IndexerParameters.Count > 0
					? member.IndexerParameters.GetArray()![0].Name
					: "key";

				var defaultExpr = member.IsNullable
					? "default"
					: GetDefaultForType(member.ReturnType, member.DefaultStrategy, member.ConcreteTypeForNew);

				var simpleIfaceName = ExtractSimpleTypeName(member.DeclaringInterfaceFullName);

				indexers.Add(new FlatIndexerModel(
					InterceptorName: interceptorName,
					InterceptorClassName: interceptorClassName,
					DeclaringInterface: member.DeclaringInterfaceFullName,
					ReturnType: member.ReturnType,
					NullableReturnType: MakeNullable(member.ReturnType),
					DefaultExpression: defaultExpr,
					KeyType: keyType,
					KeyParamName: keyParamName,
					NullableKeyType: MakeNullable(keyType),
					HasGetter: member.HasGetter,
					HasSetter: member.HasSetter,
					SimpleInterfaceName: simpleIfaceName,
					NeedsNewKeyword: NeedsNewKeyword(interceptorName)));
			}
		}

		return indexers.ToEquatableArray();
	}

	#endregion

	#region Method Model Building

	private static (EquatableArray<FlatMethodModel> Methods, EquatableArray<FlatGenericMethodHandlerModel> GenericHandlers) BuildMethodModels(
		KnockOffTypeInfo typeInfo,
		Dictionary<string, string> nameMap,
		Dictionary<string, MethodGroupInfo> methodGroups,
		string className)
	{
		var methods = new List<FlatMethodModel>();
		var genericHandlers = new List<FlatGenericMethodHandlerModel>();
		var processedGenericGroups = new HashSet<string>();
		var generatedImplementations = new HashSet<string>();

		// First pass: Build generic method handlers from FlatMembers (deduplicated)
		foreach (var member in typeInfo.FlatMembers)
		{
			if (member.IsProperty || member.IsIndexer)
				continue;

			if (member.IsGenericMethod)
			{
				var groupName = member.Name;
				if (!processedGenericGroups.Contains(groupName))
				{
					processedGenericGroups.Add(groupName);

					if (methodGroups.TryGetValue(groupName, out var group))
					{
						var isMixed = IsMixedMethodGroup(group);
						if (isMixed)
						{
							var (nonGenericGroup, genericGroup) = SplitMixedGroup(group);
							if (genericGroup is not null)
							{
								var handler = BuildGenericMethodHandler(genericGroup, nameMap, className);
								genericHandlers.Add(handler);
							}
						}
						else
						{
							var handler = BuildGenericMethodHandler(group, nameMap, className);
							genericHandlers.Add(handler);
						}
					}
				}
			}
		}

		// Second pass: Build method implementation models from ALL interfaces (not deduplicated)
		// This ensures we generate explicit implementations for every interface member
		foreach (var iface in typeInfo.Interfaces)
		{
			foreach (var member in iface.Members)
			{
				if (member.IsProperty || member.IsIndexer)
					continue;

				// Create unique key for this specific interface implementation
				var paramList = member.Parameters.Select(p => p.Type);
				var implKey = $"{member.DeclaringInterfaceFullName}.{member.Name}({string.Join(",", paramList)})";
				if (!generatedImplementations.Add(implKey))
					continue; // Skip duplicates

				var key = GetMemberKey(member);
				var interceptorName = nameMap[key];

				// Check for method delegation (e.g., IRule.RunRule(IValidateBase) -> IRule<T>.RunRule(T))
				var (delegationTarget, delegationInterface) = FindMethodDelegationTarget(member, typeInfo.Interfaces);

				if (member.IsGenericMethod)
				{
					var model = BuildGenericMethodModel(member, interceptorName, typeInfo, className, delegationTarget, delegationInterface);
					methods.Add(model);
				}
				else
				{
					var model = BuildMethodModel(member, interceptorName, typeInfo, className, delegationTarget, delegationInterface);
					methods.Add(model);
				}
			}
		}

		return (methods.ToEquatableArray(), genericHandlers.ToEquatableArray());
	}

	private static FlatMethodModel BuildMethodModel(
		InterfaceMemberInfo member,
		string interceptorName,
		KnockOffTypeInfo typeInfo,
		string className,
		InterfaceMemberInfo? delegationTarget,
		string? delegationInterface)
	{
		var interceptorClassName = $"{interceptorName}Interceptor";
		var paramArray = member.Parameters.GetArray() ?? Array.Empty<ParameterInfo>();
		var isVoid = member.ReturnType == "void";

		// Build parameter models
		var parameters = paramArray.Select(p => new ParameterModel(
			Name: p.Name,
			EscapedName: EscapeIdentifier(p.Name),
			Type: p.Type,
			NullableType: MakeNullable(p.Type),
			RefKind: p.RefKind,
			RefPrefix: GetRefKindPrefix(p.RefKind))).ToEquatableArray();

		// Trackable parameters (exclude out params)
		var trackableParams = paramArray.Where(p => p.RefKind != RefKind.Out)
			.Select(p => new ParameterModel(
				Name: p.Name,
				EscapedName: EscapeIdentifier(p.Name),
				Type: p.Type,
				NullableType: MakeNullable(p.Type),
				RefKind: p.RefKind,
				RefPrefix: GetRefKindPrefix(p.RefKind))).ToEquatableArray();

		// Parameter declarations and names
		var paramDecls = string.Join(", ", paramArray.Select(p => FormatParameterWithRefKind(p)));
		var paramNames = string.Join(", ", paramArray.Select(p => FormatParameterNameWithRefKind(p)));
		var recordCallArgs = string.Join(", ", trackableParams.Select(p => p.EscapedName));

		// LastCall type
		string? lastCallType = null;
		if (trackableParams.Count == 1)
		{
			lastCallType = trackableParams.GetArray()![0].NullableType;
		}
		else if (trackableParams.Count > 1)
		{
			lastCallType = $"({string.Join(", ", trackableParams.Select(p => $"{p.NullableType} {p.EscapedName}"))})";
		}

		// OnCall delegate
		var hasRefOrOut = paramArray.Any(p => p.RefKind == RefKind.Ref || p.RefKind == RefKind.Out);
		var needsCustomDelegate = hasRefOrOut || !isVoid;
		string? customDelegateName = null;
		string? customDelegateSignature = null;
		string onCallDelegateType;

		if (needsCustomDelegate)
		{
			customDelegateName = $"{member.Name}Delegate";
			var delegateParams = new List<string> { $"{className} ko" };
			foreach (var p in paramArray)
			{
				delegateParams.Add(FormatParameterWithRefKind(p));
			}
			var delegateParamList = string.Join(", ", delegateParams);
			customDelegateSignature = isVoid
				? $"public delegate void {customDelegateName}({delegateParamList});"
				: $"public delegate {member.ReturnType} {customDelegateName}({delegateParamList});";
			onCallDelegateType = $"{customDelegateName}?";
		}
		else if (paramArray.Length == 0)
		{
			onCallDelegateType = $"global::System.Action<{className}>?";
		}
		else
		{
			var paramTypes = string.Join(", ", paramArray.Select(p => p.Type));
			onCallDelegateType = $"global::System.Action<{className}, {paramTypes}>?";
		}

		// Default expression
		var throwsOnDefault = member.DefaultStrategy == DefaultValueStrategy.ThrowException;
		var defaultExpr = throwsOnDefault
			? ""
			: GetDefaultForType(member.ReturnType, member.DefaultStrategy, member.ConcreteTypeForNew);

		// User method
		var userMethod = FindUserMethod(typeInfo.UserMethods, member);
		string? userMethodCall = null;
		if (userMethod != null)
		{
			userMethodCall = $"{member.Name}({paramNames})";
		}

		var simpleIfaceName = ExtractSimpleTypeName(member.DeclaringInterfaceFullName);

		return new FlatMethodModel(
			InterceptorName: interceptorName,
			InterceptorClassName: interceptorClassName,
			DeclaringInterface: member.DeclaringInterfaceFullName,
			MethodName: member.Name,
			ReturnType: member.ReturnType,
			IsVoid: isVoid,
			Parameters: parameters,
			ParameterDeclarations: paramDecls,
			ParameterNames: paramNames,
			RecordCallArgs: recordCallArgs,
			TrackableParameters: trackableParams,
			LastCallType: lastCallType,
			OnCallDelegateType: onCallDelegateType,
			NeedsCustomDelegate: needsCustomDelegate,
			CustomDelegateName: customDelegateName,
			CustomDelegateSignature: customDelegateSignature,
			DefaultExpression: defaultExpr,
			ThrowsOnDefault: throwsOnDefault,
			UserMethodCall: userMethodCall,
			SimpleInterfaceName: simpleIfaceName,
			TypeParameterDecl: "",
			TypeParameterList: "",
			ConstraintClauses: "",
			NeedsNewKeyword: NeedsNewKeyword(interceptorName),
			IsGenericMethod: false,
			IsNullableReturn: member.IsNullable,
			OfTypeAccess: "",
			DelegationTarget: delegationTarget,
			DelegationTargetInterface: delegationInterface);
	}

	/// <summary>
	/// Builds a FlatMethodModel for a generic method (for interface implementation).
	/// Generic methods need special handling: .Of&lt;T&gt;() access, constraint clauses for explicit impl, etc.
	/// </summary>
	private static FlatMethodModel BuildGenericMethodModel(
		InterfaceMemberInfo member,
		string interceptorName,
		KnockOffTypeInfo typeInfo,
		string className,
		InterfaceMemberInfo? delegationTarget,
		string? delegationInterface)
	{
		var interceptorClassName = $"{interceptorName}Interceptor";
		var paramArray = member.Parameters.GetArray() ?? Array.Empty<ParameterInfo>();
		var isVoid = member.ReturnType == "void";

		// Build type parameter info
		var tpArray = member.TypeParameters.GetArray() ?? Array.Empty<TypeParameterInfo>();
		var typeParamNames = string.Join(", ", tpArray.Select(tp => tp.Name));
		var typeParamDecl = tpArray.Length > 0 ? $"<{typeParamNames}>" : "";
		var typeParamList = typeParamDecl; // Same format for method call
		var ofTypeAccess = tpArray.Length > 0 ? $".Of<{typeParamNames}>()" : "";

		// Build constraint clauses for explicit interface implementation (only class/struct constraints)
		var constraintClauses = GetConstraintsForExplicitImpl(tpArray, member.ReturnType);

		// Build parameter models
		var parameters = paramArray.Select(p => new ParameterModel(
			Name: p.Name,
			EscapedName: EscapeIdentifier(p.Name),
			Type: p.Type,
			NullableType: MakeNullable(p.Type),
			RefKind: p.RefKind,
			RefPrefix: GetRefKindPrefix(p.RefKind))).ToEquatableArray();

		// Trackable parameters for generic methods: exclude out params AND generic-typed params
		var trackableParams = paramArray
			.Where(p => p.RefKind != RefKind.Out)
			.Where(p => !tpArray.Any(tp => p.Type.Contains(tp.Name)))
			.Select(p => new ParameterModel(
				Name: p.Name,
				EscapedName: EscapeIdentifier(p.Name),
				Type: p.Type,
				NullableType: MakeNullable(p.Type),
				RefKind: p.RefKind,
				RefPrefix: GetRefKindPrefix(p.RefKind))).ToEquatableArray();

		// Parameter declarations and names
		var paramDecls = string.Join(", ", paramArray.Select(p => FormatParameterWithRefKind(p)));
		var paramNames = string.Join(", ", paramArray.Select(p => FormatParameterNameWithRefKind(p)));
		var recordCallArgs = string.Join(", ", trackableParams.Select(p => p.EscapedName));

		// LastCall type
		string? lastCallType = null;
		if (trackableParams.Count == 1)
		{
			lastCallType = trackableParams.GetArray()![0].NullableType;
		}
		else if (trackableParams.Count > 1)
		{
			lastCallType = $"({string.Join(", ", trackableParams.Select(p => $"{p.NullableType} {p.EscapedName}"))})";
		}

		// For generic methods, we use a delegate (not Action/Func) because of type parameters
		var delegateName = $"{member.Name}Delegate";
		var delegateParams = new List<string> { $"{className} ko" };
		foreach (var p in paramArray)
		{
			delegateParams.Add(FormatParameterWithRefKind(p));
		}
		var delegateParamList = string.Join(", ", delegateParams);
		var delegateSignature = isVoid
			? $"public delegate void {delegateName}({delegateParamList});"
			: $"public delegate {member.ReturnType} {delegateName}({delegateParamList});";
		var onCallDelegateType = $"{delegateName}?";

		// Default expression - for generic methods, use SmartDefault or default based on nullability
		var throwsOnDefault = false; // Generic methods use SmartDefault instead of throwing
		var defaultExpr = member.IsNullable ? "default!" : $"SmartDefault<{member.ReturnType.TrimEnd('?')}>(\"{member.Name}\")";

		// User method
		var userMethod = FindUserMethod(typeInfo.UserMethods, member);
		string? userMethodCall = null;
		if (userMethod != null)
		{
			userMethodCall = $"{member.Name}{typeParamDecl}({paramNames})";
		}

		var simpleIfaceName = ExtractSimpleTypeName(member.DeclaringInterfaceFullName);

		return new FlatMethodModel(
			InterceptorName: interceptorName,
			InterceptorClassName: interceptorClassName,
			DeclaringInterface: member.DeclaringInterfaceFullName,
			MethodName: member.Name,
			ReturnType: member.ReturnType,
			IsVoid: isVoid,
			Parameters: parameters,
			ParameterDeclarations: paramDecls,
			ParameterNames: paramNames,
			RecordCallArgs: recordCallArgs,
			TrackableParameters: trackableParams,
			LastCallType: lastCallType,
			OnCallDelegateType: onCallDelegateType,
			NeedsCustomDelegate: true,
			CustomDelegateName: delegateName,
			CustomDelegateSignature: delegateSignature,
			DefaultExpression: defaultExpr,
			ThrowsOnDefault: throwsOnDefault,
			UserMethodCall: userMethodCall,
			SimpleInterfaceName: simpleIfaceName,
			TypeParameterDecl: typeParamDecl,
			TypeParameterList: typeParamList,
			ConstraintClauses: constraintClauses,
			NeedsNewKeyword: NeedsNewKeyword(interceptorName),
			IsGenericMethod: true,
			IsNullableReturn: member.IsNullable,
			OfTypeAccess: ofTypeAccess,
			DelegationTarget: delegationTarget,
			DelegationTargetInterface: delegationInterface);
	}

	/// <summary>
	/// Gets constraint clauses for explicit interface implementation.
	/// For explicit impl, only class/struct constraints are allowed (CS0460).
	/// </summary>
	private static string GetConstraintsForExplicitImpl(TypeParameterInfo[] typeParams, string returnType)
	{
		var clauses = new List<string>();
		foreach (var tp in typeParams)
		{
			var constraintArray = tp.Constraints.GetArray() ?? Array.Empty<string>();

			// Check for struct first (mutually exclusive with class)
			if (constraintArray.Contains("struct"))
			{
				clauses.Add($" where {tp.Name} : struct");
				continue;
			}

			// Check for explicit class constraint
			if (constraintArray.Contains("class"))
			{
				clauses.Add($" where {tp.Name} : class");
				continue;
			}

			// Check if return type uses this type parameter with nullability (T?)
			// If the return type is nullable reference type T?, we need class constraint
			if (returnType.Contains($"{tp.Name}?") || returnType.EndsWith($"{tp.Name}?"))
			{
				clauses.Add($" where {tp.Name} : class");
			}
		}
		return string.Join("", clauses);
	}

	private static FlatGenericMethodHandlerModel BuildGenericMethodHandler(
		MethodGroupInfo group,
		Dictionary<string, string> nameMap,
		string className)
	{
		// Get interceptor name for this generic method group
		// Note: group.Name may have "Generic" suffix from SplitMixedGroup, but the key in nameMap uses the original name
		var originalName = group.Name.EndsWith(GenericSuffix, StringComparison.Ordinal)
			? group.Name.Substring(0, group.Name.Length - GenericSuffix.Length)
			: group.Name;
		var genericKey = $"method:{originalName}()_generic";
		var interceptorName = nameMap[genericKey];
		var interceptorClassName = $"{interceptorName}Interceptor";

		// Get type parameters from the first generic overload
		var genericOverload = group.Overloads.First(o => o.IsGenericMethod);
		var typeParams = genericOverload.TypeParameters.GetArray() ?? Array.Empty<TypeParameterInfo>();
		var typeParamNames = string.Join(", ", typeParams.Select(tp => tp.Name));
		var constraintClauses = GetConstraintClauses(typeParams);

		// Determine key type based on type parameter count
		var keyType = typeParams.Length == 1
			? "global::System.Type"
			: $"({string.Join(", ", typeParams.Select(_ => "global::System.Type"))})";

		var keyConstruction = typeParams.Length == 1
			? "typeof(T)"
			: $"({string.Join(", ", typeParams.Select(tp => $"typeof({tp.Name})"))})";

		var methodName = group.Name.EndsWith(GenericSuffix, StringComparison.Ordinal)
			? group.Name.Substring(0, group.Name.Length - GenericSuffix.Length)
			: group.Name;

		// Non-generic parameters (parameters that don't depend on type parameters)
		var paramArray = genericOverload.Parameters.GetArray() ?? Array.Empty<ParameterInfo>();
		var nonGenericParams = paramArray
			.Where(p => p.RefKind != RefKind.Out)
			.Where(p => !typeParams.Any(tp => p.Type.Contains(tp.Name)))
			.Select(p => new ParameterModel(
				Name: p.Name,
				EscapedName: EscapeIdentifier(p.Name),
				Type: p.Type,
				NullableType: MakeNullable(p.Type),
				RefKind: p.RefKind,
				RefPrefix: GetRefKindPrefix(p.RefKind)))
			.ToEquatableArray();

		// LastCall type
		string? lastCallType = null;
		if (nonGenericParams.Count == 1)
		{
			lastCallType = nonGenericParams.GetArray()![0].NullableType;
		}
		else if (nonGenericParams.Count > 1)
		{
			lastCallType = $"({string.Join(", ", nonGenericParams.Select(p => $"{p.NullableType} {p.EscapedName}"))})";
		}

		// Delegate signature
		var isVoid = group.IsVoid;
		var delegateReturnType = isVoid ? "void" : group.ReturnType;
		var delegateParams = new List<string> { $"{className} ko" };
		foreach (var p in paramArray)
		{
			delegateParams.Add(FormatParameterWithRefKind(p));
		}
		var delegateParamList = string.Join(", ", delegateParams);
		var delegateSignature = isVoid
			? $"public delegate void {methodName}Delegate({delegateParamList});"
			: $"public delegate {delegateReturnType} {methodName}Delegate({delegateParamList});";

		var typedHandlerClassName = $"{methodName}TypedHandler";

		return new FlatGenericMethodHandlerModel(
			InterceptorName: interceptorName,
			InterceptorClassName: interceptorClassName,
			MethodName: methodName,
			TypeParameterNames: typeParamNames,
			KeyType: keyType,
			KeyConstruction: keyConstruction,
			ConstraintClauses: constraintClauses,
			TypedHandlerClassName: typedHandlerClassName,
			DelegateSignature: delegateSignature,
			NonGenericParams: nonGenericParams,
			LastCallType: lastCallType,
			IsVoid: isVoid,
			ReturnType: group.ReturnType,
			NeedsNewKeyword: NeedsNewKeyword(interceptorName));
	}

	#endregion

	#region Event Model Building

	private static EquatableArray<FlatEventModel> BuildEventModels(
		KnockOffTypeInfo typeInfo,
		Dictionary<string, string> nameMap)
	{
		var events = new List<FlatEventModel>();
		var generatedImplementations = new HashSet<string>();

		// Iterate over ALL interfaces for event implementations
		foreach (var iface in typeInfo.Interfaces)
		{
			foreach (var evt in iface.Events)
			{
				// Create unique key for this specific interface implementation
				var implKey = $"{evt.DeclaringInterfaceFullName}.{evt.Name}";
				if (!generatedImplementations.Add(implKey))
					continue; // Skip duplicates

				var key = $"event:{evt.Name}";
				var interceptorName = nameMap[key];
				var interceptorClassName = $"{interceptorName}Interceptor";

				// Strip trailing ? from delegate type since we add our own nullable marker
				var delegateType = evt.FullDelegateTypeName.TrimEnd('?');

				// Build raise method signature based on delegate kind
				var (raiseParams, raiseArgs, raiseReturnType, raiseReturnsValue, usesDynamicInvoke) =
					GetRaiseMethodInfo(evt);

				events.Add(new FlatEventModel(
					InterceptorName: interceptorName,
					InterceptorClassName: interceptorClassName,
					DeclaringInterface: evt.DeclaringInterfaceFullName,
					EventName: evt.Name,
					DelegateType: delegateType,
					RaiseParameters: raiseParams,
					RaiseArguments: raiseArgs,
					RaiseReturnType: raiseReturnType,
					RaiseReturnsValue: raiseReturnsValue,
					UsesDynamicInvoke: usesDynamicInvoke,
					NeedsNewKeyword: NeedsNewKeyword(interceptorName)));
			}
		}

		return events.ToEquatableArray();
	}

	private static (string RaiseParams, string RaiseArgs, string RaiseReturnType, bool RaiseReturnsValue, bool UsesDynamicInvoke) GetRaiseMethodInfo(EventMemberInfo evt)
	{
		var paramArray = evt.DelegateParameters.GetArray() ?? Array.Empty<ParameterInfo>();

		switch (evt.DelegateKind)
		{
			case EventDelegateKind.EventHandler:
				return ("object? sender, global::System.EventArgs e", "sender, e", "void", false, false);

			case EventDelegateKind.EventHandlerOfT:
				var eventArgsType = paramArray.Length > 1 ? paramArray[1].Type : "global::System.EventArgs";
				return ($"object? sender, {eventArgsType} e", "sender, e", "void", false, false);

			case EventDelegateKind.Action:
				if (paramArray.Length == 0)
				{
					return ("", "", "void", false, false);
				}
				else
				{
					var paramDecls = string.Join(", ", paramArray.Select(p => $"{p.Type} {EscapeIdentifier(p.Name)}"));
					var paramNames = string.Join(", ", paramArray.Select(p => EscapeIdentifier(p.Name)));
					return (paramDecls, paramNames, "void", false, false);
				}

			case EventDelegateKind.Func:
				var funcParamDecls = string.Join(", ", paramArray.Select(p => $"{p.Type} {EscapeIdentifier(p.Name)}"));
				var funcParamNames = string.Join(", ", paramArray.Select(p => EscapeIdentifier(p.Name)));
				var returnType = evt.ReturnTypeName ?? "object";
				return (funcParamDecls, funcParamNames, returnType, true, false);

			case EventDelegateKind.Custom:
			default:
				// For custom delegates, generate a generic Raise
				if (paramArray.Length == 0)
				{
					return ("", "", "void", false, true);
				}
				else
				{
					var customParamDecls = string.Join(", ", paramArray.Select(p => $"{p.Type} {EscapeIdentifier(p.Name)}"));
					var customParamNames = string.Join(", ", paramArray.Select(p => EscapeIdentifier(p.Name)));
					return (customParamDecls, customParamNames, "void", false, true);
				}
		}
	}

	#endregion

	#region Type Parameter Building

	private static EquatableArray<TypeParameterModel> BuildTypeParameters(EquatableArray<TypeParameterInfo> typeParameters)
	{
		if (typeParameters.Count == 0)
			return EquatableArray<TypeParameterModel>.Empty;

		return typeParameters.Select(tp => new TypeParameterModel(
			Name: tp.Name,
			Constraints: tp.Constraints.Count > 0 ? string.Join(", ", tp.Constraints) : ""))
			.ToEquatableArray();
	}

	#endregion

	#region Helper Methods

	/// <summary>
	/// Groups methods by name to handle overloads.
	/// </summary>
	private static Dictionary<string, MethodGroupInfo> GroupMethodsByName(IEnumerable<InterfaceMemberInfo> methods)
	{
		var groups = new Dictionary<string, List<InterfaceMemberInfo>>();

		foreach (var method in methods.Where(m => !m.IsProperty))
		{
			if (!groups.TryGetValue(method.Name, out var list))
			{
				list = new List<InterfaceMemberInfo>();
				groups[method.Name] = list;
			}
			list.Add(method);
		}

		var result = new Dictionary<string, MethodGroupInfo>();

		foreach (var kvp in groups)
		{
			var methodName = kvp.Key;
			var overloads = kvp.Value;
			var first = overloads[0];

			// Build combined parameters: union of all params across overloads
			var allParamNames = new Dictionary<string, (string Type, int Count, RefKind RefKind)>();
			var totalOverloads = overloads.Count;

			foreach (var overload in overloads)
			{
				foreach (var param in overload.Parameters)
				{
					if (allParamNames.TryGetValue(param.Name, out var existing))
					{
						allParamNames[param.Name] = (existing.Type, existing.Count + 1, existing.RefKind);
					}
					else
					{
						allParamNames[param.Name] = (param.Type, 1, param.RefKind);
					}
				}
			}

			// Create combined parameters
			var combinedParams = new List<CombinedParameterInfo>();
			foreach (var kvp2 in allParamNames)
			{
				var paramName = kvp2.Key;
				var paramType = kvp2.Value.Type;
				var count = kvp2.Value.Count;
				var refKind = kvp2.Value.RefKind;
				var isNullable = count < totalOverloads;
				var nullableType = isNullable ? MakeNullable(paramType) : paramType;

				combinedParams.Add(new CombinedParameterInfo(paramName, paramType, nullableType, isNullable, refKind));
			}

			// Create overload infos
			var overloadInfos = overloads
				.Select(o => new MethodOverloadInfo(o.Parameters, o.IsGenericMethod, o.TypeParameters))
				.ToArray();

			result[methodName] = new MethodGroupInfo(
				Name: methodName,
				ReturnType: first.ReturnType,
				IsVoid: first.ReturnType == "void",
				IsNullable: first.IsNullable,
				Overloads: new EquatableArray<MethodOverloadInfo>(overloadInfos),
				CombinedParameters: new EquatableArray<CombinedParameterInfo>(combinedParams.ToArray()));
		}

		return result;
	}

	/// <summary>
	/// Determines if a method group has both generic and non-generic overloads.
	/// </summary>
	private static bool IsMixedMethodGroup(MethodGroupInfo group)
	{
		var overloads = group.Overloads.GetArray() ?? Array.Empty<MethodOverloadInfo>();
		var hasGeneric = overloads.Any(o => o.IsGenericMethod);
		var hasNonGeneric = overloads.Any(o => !o.IsGenericMethod);
		return hasGeneric && hasNonGeneric;
	}

	/// <summary>
	/// Splits a mixed method group into separate non-generic and generic sub-groups.
	/// </summary>
	private static (MethodGroupInfo? NonGeneric, MethodGroupInfo? Generic) SplitMixedGroup(MethodGroupInfo group)
	{
		var overloads = group.Overloads.GetArray() ?? Array.Empty<MethodOverloadInfo>();
		var nonGenericOverloads = overloads.Where(o => !o.IsGenericMethod).ToArray();
		var genericOverloads = overloads.Where(o => o.IsGenericMethod).ToArray();

		if (nonGenericOverloads.Length == 0 || genericOverloads.Length == 0)
			return (null, null);

		// Build combined parameters for non-generic overloads only
		var nonGenericCombinedParams = BuildCombinedParametersForOverloads(nonGenericOverloads);

		var nonGenericGroup = new MethodGroupInfo(
			Name: group.Name,
			ReturnType: group.ReturnType,
			IsVoid: group.IsVoid,
			IsNullable: group.IsNullable,
			Overloads: new EquatableArray<MethodOverloadInfo>(nonGenericOverloads),
			CombinedParameters: new EquatableArray<CombinedParameterInfo>(nonGenericCombinedParams));

		// Build combined parameters for generic overloads only
		var genericCombinedParams = BuildCombinedParametersForOverloads(genericOverloads);

		var genericGroup = new MethodGroupInfo(
			Name: group.Name + GenericSuffix,
			ReturnType: group.ReturnType,
			IsVoid: group.IsVoid,
			IsNullable: group.IsNullable,
			Overloads: new EquatableArray<MethodOverloadInfo>(genericOverloads),
			CombinedParameters: new EquatableArray<CombinedParameterInfo>(genericCombinedParams));

		return (nonGenericGroup, genericGroup);
	}

	/// <summary>
	/// Builds combined parameter info for a set of method overloads.
	/// </summary>
	private static CombinedParameterInfo[] BuildCombinedParametersForOverloads(MethodOverloadInfo[] overloads)
	{
		var allParamNames = new Dictionary<string, (string Type, int Count, RefKind RefKind)>();
		var totalOverloads = overloads.Length;

		foreach (var overload in overloads)
		{
			var parameters = overload.Parameters.GetArray() ?? Array.Empty<ParameterInfo>();
			foreach (var param in parameters)
			{
				if (allParamNames.TryGetValue(param.Name, out var existing))
				{
					allParamNames[param.Name] = (existing.Type, existing.Count + 1, existing.RefKind);
				}
				else
				{
					allParamNames[param.Name] = (param.Type, 1, param.RefKind);
				}
			}
		}

		var combinedParams = new List<CombinedParameterInfo>();
		foreach (var kvp in allParamNames)
		{
			var paramName = kvp.Key;
			var paramType = kvp.Value.Type;
			var count = kvp.Value.Count;
			var refKind = kvp.Value.RefKind;
			var isNullable = count < totalOverloads;
			var nullableType = isNullable ? MakeNullable(paramType) : paramType;

			combinedParams.Add(new CombinedParameterInfo(paramName, paramType, nullableType, isNullable, refKind));
		}

		return combinedParams.ToArray();
	}

	/// <summary>
	/// Build constraint clauses for type parameters.
	/// </summary>
	private static string GetConstraintClauses(TypeParameterInfo[] typeParams)
	{
		var clauses = new List<string>();
		foreach (var tp in typeParams)
		{
			if (tp.Constraints.Count > 0)
			{
				var constraints = string.Join(", ", tp.Constraints);
				clauses.Add($" where {tp.Name} : {constraints}");
			}
		}
		return string.Join("", clauses);
	}

	/// <summary>
	/// Makes a type nullable if it isn't already.
	/// </summary>
	private static string MakeNullable(string type)
	{
		if (type.EndsWith("?"))
			return type;

		return type + "?";
	}

	/// <summary>
	/// Escapes C# reserved keywords by prefixing with @.
	/// </summary>
	private static string EscapeIdentifier(string name)
	{
		var keywords = new HashSet<string>
		{
			"abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
			"class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else",
			"enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for",
			"foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
			"long", "namespace", "new", "null", "object", "operator", "out", "override", "params",
			"private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short",
			"sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true",
			"try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual",
			"void", "volatile", "while", "value"
		};

		return keywords.Contains(name) ? $"@{name}" : name;
	}

	/// <summary>
	/// Gets the ref kind prefix for a parameter.
	/// </summary>
	private static string GetRefKindPrefix(RefKind kind) => kind switch
	{
		RefKind.Out => "out ",
		RefKind.Ref => "ref ",
		RefKind.In => "in ",
		RefKind.RefReadOnlyParameter => "ref readonly ",
		_ => ""
	};

	/// <summary>
	/// Formats a parameter with ref/out/in keyword.
	/// </summary>
	private static string FormatParameterWithRefKind(ParameterInfo p)
	{
		var refKindPrefix = GetRefKindPrefix(p.RefKind);
		return $"{refKindPrefix}{p.Type} {EscapeIdentifier(p.Name)}";
	}

	/// <summary>
	/// Formats a parameter name with ref/out/in keyword for passing to another method.
	/// </summary>
	private static string FormatParameterNameWithRefKind(ParameterInfo p)
	{
		var refKindPrefix = GetRefKindPrefix(p.RefKind);
		return $"{refKindPrefix}{EscapeIdentifier(p.Name)}";
	}

	/// <summary>
	/// Gets a default value expression for property backing storage.
	/// Handles ThrowException strategy specially for string, arrays, etc.
	/// </summary>
	private static string GetDefaultValueForProperty(string typeName, DefaultValueStrategy strategy, string? concreteType)
	{
		var typeToNew = concreteType ?? typeName;
		return strategy switch
		{
			DefaultValueStrategy.NewInstance => $"new {typeToNew}()",
			DefaultValueStrategy.Default => "default!",
			DefaultValueStrategy.ThrowException => GetBackingPropertyInitializer(typeName),
			_ => "default!"
		};
	}

	/// <summary>
	/// Gets a backing property initializer for types with ThrowException strategy.
	/// Provides sensible defaults for string, arrays, collection interfaces.
	/// </summary>
	private static string GetBackingPropertyInitializer(string typeName)
	{
		// Handle string - use empty string
		if (typeName == "global::System.String" || typeName == "string")
			return "\"\"";

		// Handle arrays - use Array.Empty<T>() or empty array
		if (typeName.EndsWith("[]"))
		{
			var elementType = typeName.Substring(0, typeName.Length - 2);
			return $"global::System.Array.Empty<{elementType}>()";
		}

		// Handle collection interfaces - use Array.Empty<T>()
		if (typeName.Contains("IEnumerable<") || typeName.Contains("IReadOnlyCollection<") || typeName.Contains("IReadOnlyList<"))
		{
			var elementType = ExtractGenericArg(typeName);
			return $"global::System.Array.Empty<{elementType}>()";
		}

		// Fallback: suppress nullable warning (property exists but user must set it)
		return "default!";
	}

	/// <summary>
	/// Gets a default expression for a type based on the strategy.
	/// </summary>
	private static string GetDefaultForType(string typeName, DefaultValueStrategy strategy, string? concreteType)
	{
		// Handle non-generic ValueTask FIRST (struct, default is completed)
		if (typeName == "global::System.Threading.Tasks.ValueTask" || typeName == "ValueTask")
		{
			return "default";
		}

		// Handle non-generic Task - must return Task.CompletedTask, not null
		if (typeName == "global::System.Threading.Tasks.Task" || typeName == "Task")
		{
			return "global::System.Threading.Tasks.Task.CompletedTask";
		}

		// Handle ValueTask<T> - must check before Task<T> because "ValueTask<" contains "Task<"
		if (typeName.Contains("ValueTask<") || typeName.Contains("global::System.Threading.Tasks.ValueTask<"))
		{
			var innerType = ExtractGenericArg(typeName);
			if (!string.IsNullOrEmpty(innerType))
			{
				if (strategy == DefaultValueStrategy.NewInstance)
				{
					var innerTypeToNew = concreteType ?? innerType;
					return $"new global::System.Threading.Tasks.ValueTask<{innerType}>(new {innerTypeToNew}())";
				}
				return "default";
			}
			return "default";
		}

		// Handle Task<T> - cannot use new Task<T>(), must use Task.FromResult
		if (typeName.Contains("Task<") || typeName.Contains("global::System.Threading.Tasks.Task<"))
		{
			var innerType = ExtractGenericArg(typeName);
			if (!string.IsNullOrEmpty(innerType))
			{
				var innerTypeToNew = concreteType ?? innerType;
				var innerValue = strategy switch
				{
					DefaultValueStrategy.NewInstance => $"new {innerTypeToNew}()",
					DefaultValueStrategy.Default => "default!",
					_ => "default!"
				};
				return $"global::System.Threading.Tasks.Task.FromResult<{innerType}>({innerValue})";
			}
			return "global::System.Threading.Tasks.Task.CompletedTask";
		}

		var typeToNew = concreteType ?? typeName;
		return strategy switch
		{
			DefaultValueStrategy.NewInstance => $"new {typeToNew}()",
			DefaultValueStrategy.Default => "default!",
			_ => "default!"
		};
	}

	/// <summary>
	/// Extracts the generic argument from a type like Task&lt;T&gt;.
	/// </summary>
	private static string ExtractGenericArg(string typeName)
	{
		var start = typeName.IndexOf('<');
		var end = typeName.LastIndexOf('>');
		if (start >= 0 && end > start)
		{
			return typeName.Substring(start + 1, end - start - 1);
		}
		return "";
	}

	/// <summary>
	/// Finds a user-defined method that matches the interface member.
	/// </summary>
	private static UserMethodInfo? FindUserMethod(EquatableArray<UserMethodInfo> userMethods, InterfaceMemberInfo member)
	{
		var methods = userMethods.GetArray();
		if (methods == null || methods.Length == 0) return null;

		var memberParams = member.Parameters.GetArray() ?? Array.Empty<ParameterInfo>();
		var memberTypeParams = member.TypeParameters.GetArray() ?? Array.Empty<TypeParameterInfo>();

		foreach (var userMethod in methods)
		{
			if (userMethod.Name != member.Name) continue;
			if (userMethod.ReturnType != member.ReturnType) continue;
			if (userMethod.IsGenericMethod != member.IsGenericMethod) continue;

			// Check type parameters match for generic methods
			var userTypeParams = userMethod.TypeParameters.GetArray() ?? Array.Empty<TypeParameterInfo>();
			if (userTypeParams.Length != memberTypeParams.Length) continue;

			var typeParamsMatch = true;
			for (int i = 0; i < userTypeParams.Length; i++)
			{
				if (userTypeParams[i].Name != memberTypeParams[i].Name)
				{
					typeParamsMatch = false;
					break;
				}
			}
			if (!typeParamsMatch) continue;

			var userParams = userMethod.Parameters.GetArray() ?? Array.Empty<ParameterInfo>();
			if (userParams.Length != memberParams.Length) continue;

			var match = true;
			for (int i = 0; i < userParams.Length; i++)
			{
				if (userParams[i].Type != memberParams[i].Type)
				{
					match = false;
					break;
				}
			}

			if (match) return userMethod;
		}

		return null;
	}

	/// <summary>
	/// Extracts the simple type name from a fully qualified name.
	/// </summary>
	private static string ExtractSimpleTypeName(string fullName)
	{
		// Remove global:: prefix if present
		var name = fullName;
		if (name.StartsWith("global::"))
			name = name.Substring(8);

		// Find the last dot to get the simple name
		var lastDot = name.LastIndexOf('.');
		if (lastDot >= 0)
			return name.Substring(lastDot + 1);

		return name;
	}

	/// <summary>
	/// Gets the nullability attribute declaration for a setter, if needed.
	/// </summary>
	private static string GetSetterNullabilityAttribute(InterfaceMemberInfo member)
	{
		if (member.SetterHasDisallowNull || member.SetterHasAllowNull)
			return "#pragma warning disable CS8769\n";
		return "";
	}

	/// <summary>
	/// Gets the pragma restore after a setter with nullability attributes.
	/// </summary>
	private static string GetSetterNullabilityRestore(InterfaceMemberInfo member)
	{
		if (member.SetterHasDisallowNull || member.SetterHasAllowNull)
			return "\n#pragma warning restore CS8769";
		return "";
	}

	/// <summary>
	/// Names of members inherited from object that interceptor properties could hide.
	/// </summary>
	private static readonly HashSet<string> ObjectMemberNames = new(StringComparer.Ordinal)
	{
		"Equals",
		"GetHashCode",
		"ToString",
		"GetType"
	};

	/// <summary>
	/// Returns true if the interceptor name would hide an inherited object member.
	/// </summary>
	private static bool NeedsNewKeyword(string interceptorName) =>
		ObjectMemberNames.Contains(interceptorName);

	#endregion
}
