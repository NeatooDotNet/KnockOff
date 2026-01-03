using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KnockOff;

[Generator(LanguageNames.CSharp)]
public class KnockOffGenerator : IIncrementalGenerator
{
	/// <summary>
	/// Display format that includes nullability annotations and fully qualified names.
	/// </summary>
	private static readonly SymbolDisplayFormat FullyQualifiedWithNullability = new(
		globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
		typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
		genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
		miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers
			| SymbolDisplayMiscellaneousOptions.UseSpecialTypes
			| SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		var classesToGenerate = context.SyntaxProvider.ForAttributeWithMetadataName(
			"KnockOff.KnockOffAttribute",
			predicate: static (node, _) => IsCandidateClass(node),
			transform: static (ctx, _) => TransformClass(ctx));

		context.RegisterSourceOutput(classesToGenerate, static (spc, typeInfo) =>
		{
			if (typeInfo is not null)
			{
				GenerateKnockOff(spc, typeInfo);
			}
		});
	}

	/// <summary>
	/// Predicate: partial class, not abstract, not generic, implements at least one interface
	/// </summary>
	private static bool IsCandidateClass(SyntaxNode node)
	{
		if (node is not ClassDeclarationSyntax classDecl)
			return false;

		// Must be partial
		if (!classDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
			return false;

		// Must not be abstract
		if (classDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.AbstractKeyword)))
			return false;

		// Must not be generic (Phase 1 limitation)
		if (classDecl.TypeParameterList?.Parameters.Count > 0)
			return false;

		// Must have base list (potential interfaces)
		if (classDecl.BaseList is null || classDecl.BaseList.Types.Count == 0)
			return false;

		return true;
	}

	/// <summary>
	/// Transform: extract all interface members and user-defined methods
	/// </summary>
	private static KnockOffTypeInfo? TransformClass(GeneratorAttributeSyntaxContext context)
	{
		var classDeclaration = (ClassDeclarationSyntax)context.TargetNode;
		var classSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclaration);

		if (classSymbol is null)
			return null;

		// Get namespace
		var ns = classSymbol.ContainingNamespace;
		var namespaceName = ns.IsGlobalNamespace ? "" : ns.ToDisplayString();

		// Get all implemented interfaces
		var interfaces = classSymbol.AllInterfaces;
		if (interfaces.Length == 0)
			return null;

		// Get the KnockOff class's assembly for accessibility checks
		var knockOffAssembly = classSymbol.ContainingAssembly;

		var interfaceInfos = new List<InterfaceInfo>();

		foreach (var iface in interfaces)
		{
			var members = new List<InterfaceMemberInfo>();
			var events = new List<EventMemberInfo>();

			foreach (var member in iface.GetMembers())
			{
				// Skip internal members from external assemblies
				if (!IsMemberAccessible(member, knockOffAssembly))
					continue;

				if (member is IPropertySymbol property)
				{
					members.Add(CreatePropertyInfo(property));
				}
				else if (member is IMethodSymbol method && method.MethodKind == MethodKind.Ordinary)
				{
					members.Add(CreateMethodInfo(method));
				}
				else if (member is IEventSymbol eventSymbol)
				{
					events.Add(CreateEventInfo(eventSymbol));
				}
			}

			if (members.Count > 0 || events.Count > 0)
			{
				// Extract simple interface name for AsXYZ() generation
				var simpleName = GetSimpleInterfaceName(iface.Name);

				interfaceInfos.Add(new InterfaceInfo(
					iface.ToDisplayString(),
					simpleName,
					new EquatableArray<InterfaceMemberInfo>(members.ToArray()),
					new EquatableArray<EventMemberInfo>(events.ToArray())));
			}
		}

		// Get user-defined methods that could override interface methods
		var userMethods = GetUserDefinedMethods(classSymbol, interfaceInfos);

		return new KnockOffTypeInfo(
			Namespace: namespaceName,
			ClassName: classSymbol.Name,
			Interfaces: new EquatableArray<InterfaceInfo>(interfaceInfos.ToArray()),
			UserMethods: userMethods);
	}

	/// <summary>
	/// Gets simple name for AsXYZ() method (strips leading 'I' if followed by uppercase)
	/// </summary>
	private static string GetSimpleInterfaceName(string interfaceName)
	{
		// If starts with 'I' followed by uppercase letter, strip the 'I'
		if (interfaceName.Length > 1 &&
			interfaceName[0] == 'I' &&
			char.IsUpper(interfaceName[1]))
		{
			return interfaceName.Substring(1);
		}
		return interfaceName;
	}

	/// <summary>
	/// Checks if a member is accessible from the KnockOff class.
	/// Internal members are only accessible from the same assembly.
	/// </summary>
	private static bool IsMemberAccessible(ISymbol member, IAssemblySymbol knockOffAssembly)
	{
		// Internal or ProtectedAndInternal members are only accessible from the same assembly
		if (member.DeclaredAccessibility == Accessibility.Internal ||
			member.DeclaredAccessibility == Accessibility.ProtectedAndInternal)
		{
			var memberAssembly = member.ContainingAssembly;
			return SymbolEqualityComparer.Default.Equals(memberAssembly, knockOffAssembly);
		}

		// Public and Protected members are accessible
		return true;
	}

	/// <summary>
	/// Gets the keyword prefix for a ref kind (out, ref, in, ref readonly).
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
	/// Formats a parameter for use in a method signature (e.g., "out string value").
	/// </summary>
	private static string FormatParameter(ParameterInfo p) =>
		$"{GetRefKindPrefix(p.RefKind)}{p.Type} {p.Name}";

	/// <summary>
	/// Formats a parameter for use in a method call/argument (e.g., "out value").
	/// </summary>
	private static string FormatArgument(ParameterInfo p) =>
		$"{GetRefKindPrefix(p.RefKind)}{p.Name}";

	/// <summary>
	/// Returns true if the parameter is an output-only parameter (out, not ref).
	/// Out parameters are outputs from the method, not inputs to track.
	/// </summary>
	private static bool IsOutputParameter(RefKind refKind) =>
		refKind == RefKind.Out;

	/// <summary>
	/// Filters parameters to only include input parameters (excludes out params).
	/// </summary>
	private static IEnumerable<ParameterInfo> GetInputParameters(EquatableArray<ParameterInfo> parameters) =>
		parameters.Where(p => !IsOutputParameter(p.RefKind));

	/// <summary>
	/// Filters combined parameters to only include input parameters (excludes out params).
	/// </summary>
	private static IEnumerable<CombinedParameterInfo> GetInputCombinedParameters(EquatableArray<CombinedParameterInfo> parameters) =>
		parameters.Where(p => !IsOutputParameter(p.RefKind));

	/// <summary>
	/// Formats a parameter for RecordCall (stores value, no ref/out keywords).
	/// </summary>
	private static string FormatRecordCallParameter(ParameterInfo p) =>
		$"{p.Type} {p.Name}";

	private static InterfaceMemberInfo CreatePropertyInfo(IPropertySymbol property)
	{
		var returnType = property.Type.ToDisplayString(FullyQualifiedWithNullability);
		var isNullable = property.Type.NullableAnnotation == NullableAnnotation.Annotated
			|| property.Type.IsReferenceType && property.Type.NullableAnnotation != NullableAnnotation.NotAnnotated;

		// Handle indexers
		var isIndexer = property.IsIndexer;
		var indexerParameters = EquatableArray<ParameterInfo>.Empty;
		var name = property.Name;

		if (isIndexer)
		{
			// For indexers, create a name based on parameter types for uniqueness
			// e.g., this[string key] -> "StringIndexer"
			var paramTypes = property.Parameters
				.Select(p => GetSimpleTypeName(p.Type))
				.ToArray();
			name = string.Join("", paramTypes) + "Indexer";

			indexerParameters = new EquatableArray<ParameterInfo>(
				property.Parameters
					.Select(p => new ParameterInfo(p.Name, p.Type.ToDisplayString(FullyQualifiedWithNullability), p.RefKind))
					.ToArray());
		}

		return new InterfaceMemberInfo(
			Name: name,
			ReturnType: returnType,
			IsProperty: true,
			IsIndexer: isIndexer,
			HasGetter: property.GetMethod is not null,
			HasSetter: property.SetMethod is not null,
			IsNullable: isNullable,
			Parameters: EquatableArray<ParameterInfo>.Empty,
			IndexerParameters: indexerParameters);
	}

	/// <summary>
	/// Gets a simple type name for use in indexer naming (e.g., "String" from "global::System.String")
	/// </summary>
	private static string GetSimpleTypeName(ITypeSymbol type)
	{
		// Use the simple name, capitalize first letter
		var name = type.Name;
		if (string.IsNullOrEmpty(name))
			return "Unknown";
		return char.ToUpperInvariant(name[0]) + name.Substring(1);
	}

	private static InterfaceMemberInfo CreateMethodInfo(IMethodSymbol method)
	{
		var returnType = method.ReturnType.ToDisplayString(FullyQualifiedWithNullability);
		var isNullable = method.ReturnType.NullableAnnotation == NullableAnnotation.Annotated
			|| (method.ReturnType.IsReferenceType && method.ReturnType.NullableAnnotation != NullableAnnotation.NotAnnotated);

		// For void methods, they're not "nullable" in the sense that matters
		if (method.ReturnsVoid)
			isNullable = true; // void can't throw for missing return

		// For Task and ValueTask (non-generic), treat as void-like
		var typeFullName = method.ReturnType.OriginalDefinition.ToDisplayString();
		if (typeFullName == "System.Threading.Tasks.Task" || typeFullName == "System.Threading.Tasks.ValueTask")
			isNullable = true; // async void-like, return completed task

		// For Task<T> and ValueTask<T>, check the inner type for nullability
		if (method.ReturnType is INamedTypeSymbol namedType && namedType.IsGenericType)
		{
			var containingNs = namedType.ContainingNamespace?.ToDisplayString() ?? "";
			var typeName = namedType.Name;
			if (containingNs == "System.Threading.Tasks" && (typeName == "Task" || typeName == "ValueTask"))
			{
				var innerType = namedType.TypeArguments[0];
				isNullable = innerType.NullableAnnotation == NullableAnnotation.Annotated
					|| (innerType.IsReferenceType && innerType.NullableAnnotation != NullableAnnotation.NotAnnotated);
			}
		}

		var parameters = method.Parameters
			.Select(p => new ParameterInfo(p.Name, p.Type.ToDisplayString(FullyQualifiedWithNullability), p.RefKind))
			.ToArray();

		return new InterfaceMemberInfo(
			Name: method.Name,
			ReturnType: returnType,
			IsProperty: false,
			IsIndexer: false,
			HasGetter: false,
			HasSetter: false,
			IsNullable: isNullable,
			Parameters: new EquatableArray<ParameterInfo>(parameters),
			IndexerParameters: EquatableArray<ParameterInfo>.Empty);
	}

	/// <summary>
	/// Create event info from an IEventSymbol
	/// </summary>
	private static EventMemberInfo CreateEventInfo(IEventSymbol eventSymbol)
	{
		var delegateType = (INamedTypeSymbol)eventSymbol.Type;
		var invokeMethod = delegateType.DelegateInvokeMethod;

		if (invokeMethod is null)
		{
			// Fallback for malformed delegates
			return new EventMemberInfo(
				Name: eventSymbol.Name,
				FullDelegateTypeName: delegateType.ToDisplayString(FullyQualifiedWithNullability),
				DelegateKind: EventDelegateKind.Custom,
				DelegateParameters: EquatableArray<ParameterInfo>.Empty,
				ReturnTypeName: null,
				IsAsync: false);
		}

		var delegateKind = ClassifyDelegateKind(delegateType);
		var isAsync = IsAsyncDelegate(invokeMethod);

		var parameters = invokeMethod.Parameters
			.Select(p => new ParameterInfo(p.Name, p.Type.ToDisplayString(FullyQualifiedWithNullability), p.RefKind))
			.ToArray();

		var returnType = invokeMethod.ReturnsVoid ? null
			: invokeMethod.ReturnType.ToDisplayString(FullyQualifiedWithNullability);

		return new EventMemberInfo(
			Name: eventSymbol.Name,
			FullDelegateTypeName: delegateType.ToDisplayString(FullyQualifiedWithNullability),
			DelegateKind: delegateKind,
			DelegateParameters: new EquatableArray<ParameterInfo>(parameters),
			ReturnTypeName: returnType,
			IsAsync: isAsync);
	}

	/// <summary>
	/// Classify the delegate type for code generation
	/// </summary>
	private static EventDelegateKind ClassifyDelegateKind(INamedTypeSymbol delegateType)
	{
		var ns = delegateType.ContainingNamespace?.ToDisplayString() ?? "";
		var name = delegateType.Name;

		// Check for System.EventHandler and System.EventHandler<T>
		if (ns == "System")
		{
			if (name == "EventHandler")
			{
				return delegateType.IsGenericType
					? EventDelegateKind.EventHandlerOfT
					: EventDelegateKind.EventHandler;
			}

			if (name == "Action")
				return EventDelegateKind.Action;

			if (name == "Func")
				return EventDelegateKind.Func;
		}

		return EventDelegateKind.Custom;
	}

	/// <summary>
	/// Check if a delegate returns Task or ValueTask (async pattern)
	/// </summary>
	private static bool IsAsyncDelegate(IMethodSymbol invokeMethod)
	{
		if (invokeMethod.ReturnsVoid)
			return false;

		var returnType = invokeMethod.ReturnType;
		var ns = returnType.ContainingNamespace?.ToDisplayString() ?? "";
		var name = returnType.Name;

		if (ns == "System.Threading.Tasks")
		{
			return name == "Task" || name == "ValueTask";
		}

		return false;
	}

	/// <summary>
	/// Find user-defined protected methods that match interface method signatures
	/// </summary>
	private static EquatableArray<UserMethodInfo> GetUserDefinedMethods(
		INamedTypeSymbol classSymbol,
		List<InterfaceInfo> interfaces)
	{
		var userMethods = new List<UserMethodInfo>();

		// Get all methods declared directly on this class (not inherited)
		var classMethods = classSymbol.GetMembers()
			.OfType<IMethodSymbol>()
			.Where(m => m.MethodKind == MethodKind.Ordinary
				&& m.DeclaredAccessibility == Accessibility.Protected
				&& !m.IsStatic)
			.ToList();

		// Build a set of interface method signatures to match against
		var interfaceMethodSignatures = new HashSet<string>();
		foreach (var iface in interfaces)
		{
			foreach (var member in iface.Members)
			{
				if (!member.IsProperty)
				{
					var sig = GetMethodSignature(member.Name, member.ReturnType, member.Parameters);
					interfaceMethodSignatures.Add(sig);
				}
			}
		}

		// Find matching user methods
		foreach (var method in classMethods)
		{
			var returnType = method.ReturnType.ToDisplayString(FullyQualifiedWithNullability);
			var parameters = method.Parameters
				.Select(p => new ParameterInfo(p.Name, p.Type.ToDisplayString(FullyQualifiedWithNullability), p.RefKind))
				.ToArray();

			var sig = GetMethodSignature(method.Name, returnType, new EquatableArray<ParameterInfo>(parameters));

			if (interfaceMethodSignatures.Contains(sig))
			{
				userMethods.Add(new UserMethodInfo(
					Name: method.Name,
					ReturnType: returnType,
					Parameters: new EquatableArray<ParameterInfo>(parameters)));
			}
		}

		return new EquatableArray<UserMethodInfo>(userMethods.ToArray());
	}

	private static string GetMethodSignature(string name, string returnType, EquatableArray<ParameterInfo> parameters)
	{
		var paramTypes = string.Join(",", parameters.Select(p => p.Type));
		return $"{returnType} {name}({paramTypes})";
	}

	/// <summary>
	/// Groups methods by name to handle overloads, creating combined parameter tuples
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
			// Params not in all overloads become nullable
			var allParamNames = new Dictionary<string, (string Type, int Count, RefKind RefKind)>();
			var totalOverloads = overloads.Count;

			foreach (var overload in overloads)
			{
				foreach (var param in overload.Parameters)
				{
					if (allParamNames.TryGetValue(param.Name, out var existing))
					{
						// Same name exists - increment count (param appears in multiple overloads)
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
				var isNullable = count < totalOverloads; // Not in all overloads = nullable
				var nullableType = isNullable ? MakeNullable(paramType) : paramType;

				combinedParams.Add(new CombinedParameterInfo(paramName, paramType, nullableType, isNullable, refKind));
			}

			// Create overload infos
			var overloadInfos = overloads
				.Select(o => new MethodOverloadInfo(o.Parameters))
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
	/// Generate the partial class with explicit interface implementations
	/// </summary>
	private static void GenerateKnockOff(SourceProductionContext context, KnockOffTypeInfo typeInfo)
	{
		var sb = new System.Text.StringBuilder();

		sb.AppendLine("// <auto-generated/>");
		sb.AppendLine("#nullable enable");
		sb.AppendLine();

		if (!string.IsNullOrEmpty(typeInfo.Namespace))
		{
			sb.AppendLine($"namespace {typeInfo.Namespace};");
			sb.AppendLine();
		}

		sb.AppendLine($"partial class {typeInfo.ClassName}");
		sb.AppendLine("{");

		// Collect unique members across all interfaces
		var processedProperties = new Dictionary<string, InterfaceMemberInfo>();
		var processedMethods = new Dictionary<string, InterfaceMemberInfo>(); // key = signature
		var processedEvents = new Dictionary<string, EventMemberInfo>();

		foreach (var iface in typeInfo.Interfaces)
		{
			foreach (var member in iface.Members)
			{
				if (member.IsProperty)
				{
					var propKey = $"prop:{member.Name}";
					if (!processedProperties.ContainsKey(propKey))
						processedProperties[propKey] = member;
				}
				else
				{
					var methodKey = GetMethodSignature(member.Name, member.ReturnType, member.Parameters);
					if (!processedMethods.ContainsKey(methodKey))
						processedMethods[methodKey] = member;
				}
			}

			foreach (var evt in iface.Events)
			{
				var eventKey = $"event:{evt.Name}";
				if (!processedEvents.ContainsKey(eventKey))
					processedEvents[eventKey] = evt;
			}
		}

		// Group methods by name to handle overloads
		var methodGroups = GroupMethodsByName(processedMethods.Values);

		// 1. Generate per-property Handler classes
		foreach (var kvp in processedProperties)
		{
			GenerateMemberHandlerClass(sb, kvp.Value, typeInfo.ClassName);
		}

		// 1b. Generate per-method-group Handler classes (delegate-based for all methods)
		foreach (var group in methodGroups.Values)
		{
			GenerateMethodGroupHandlerClass(sb, group, typeInfo.ClassName);
		}

		// 1c. Generate per-event Handler classes
		foreach (var kvp in processedEvents)
		{
			GenerateEventHandlerClass(sb, kvp.Value, typeInfo.ClassName);
		}

		// 2. Generate Spy class
		GenerateSpyClass(sb, typeInfo.ClassName, processedProperties.Values, methodGroups.Values, processedEvents.Values);

		// 3. Generate Spy property
		sb.AppendLine("\t/// <summary>Tracks invocations and configures behavior for all interface members.</summary>");
		sb.AppendLine($"\tpublic {typeInfo.ClassName}Spy Spy {{ get; }} = new();");
		sb.AppendLine();

		// 4. Generate AsXYZ() methods for each interface
		var generatedAsMethodNames = new HashSet<string>();
		foreach (var iface in typeInfo.Interfaces)
		{
			var methodName = $"As{iface.SimpleName}";
			if (!generatedAsMethodNames.Contains(methodName))
			{
				generatedAsMethodNames.Add(methodName);
				sb.AppendLine($"\t/// <summary>Returns this instance as {iface.FullName}.</summary>");
				sb.AppendLine($"\tpublic {iface.FullName} {methodName}() => this;");
				sb.AppendLine();
			}
		}

		// 5. Generate backing properties/dictionaries ONCE per unique property name
		var generatedBackingProperties = new HashSet<string>();
		foreach (var kvp in processedProperties)
		{
			var member = kvp.Value;
			if (!generatedBackingProperties.Contains(member.Name))
			{
				generatedBackingProperties.Add(member.Name);
				if (member.IsIndexer)
				{
					GenerateIndexerBackingDictionary(sb, member);
				}
				else
				{
					GenerateBackingProperty(sb, member);
				}
			}
		}

		// 6. Generate explicit interface implementations for EACH interface member
		foreach (var iface in typeInfo.Interfaces)
		{
			foreach (var member in iface.Members)
			{
				if (member.IsIndexer)
				{
					GenerateIndexerImplementation(sb, iface.FullName, member);
				}
				else if (member.IsProperty)
				{
					GeneratePropertyImplementation(sb, iface.FullName, member);
				}
				else
				{
					// All methods use delegate-based handler
					var group = methodGroups[member.Name];
					GenerateMethod(sb, iface.FullName, member, typeInfo, group);
				}
			}

			// 6b. Generate explicit interface implementations for events
			foreach (var evt in iface.Events)
			{
				GenerateEventImplementation(sb, iface.FullName, evt);
			}
		}

		sb.AppendLine("}");

		context.AddSource($"{typeInfo.ClassName}.g.cs", sb.ToString());
	}

	/// <summary>
	/// Generate a per-member Handler class with strongly-typed tracking and callbacks
	/// </summary>
	private static void GenerateMemberHandlerClass(System.Text.StringBuilder sb, InterfaceMemberInfo member, string className)
	{
		sb.AppendLine($"\t/// <summary>Tracks and configures behavior for {member.Name}.</summary>");
		sb.AppendLine($"\tpublic sealed class {member.Name}Handler");
		sb.AppendLine("\t{");

		if (member.IsIndexer)
		{
			GenerateIndexerExecutionDetails(sb, member, className);
		}
		else if (member.IsProperty)
		{
			// Property tracking: GetCount, SetCount, LastSetValue (typed)
			if (member.HasGetter)
			{
				sb.AppendLine("\t\t/// <summary>Number of times the getter was accessed.</summary>");
				sb.AppendLine("\t\tpublic int GetCount { get; private set; }");
				sb.AppendLine();

				// OnGet callback: Func<TKnockOff, TReturn>?
				sb.AppendLine("\t\t/// <summary>Callback invoked when the getter is accessed. If set, its return value is used.</summary>");
				sb.AppendLine($"\t\tpublic global::System.Func<{className}, {member.ReturnType}>? OnGet {{ get; set; }}");
				sb.AppendLine();
			}

			if (member.HasSetter)
			{
				sb.AppendLine("\t\t/// <summary>Number of times the setter was accessed.</summary>");
				sb.AppendLine("\t\tpublic int SetCount { get; private set; }");
				sb.AppendLine();

				// Determine nullable type for LastSetValue
				var nullableType = MakeNullable(member.ReturnType);
				sb.AppendLine("\t\t/// <summary>The value from the most recent setter call.</summary>");
				sb.AppendLine($"\t\tpublic {nullableType} LastSetValue {{ get; private set; }}");
				sb.AppendLine();

				// OnSet callback: Action<TKnockOff, TValue>?
				sb.AppendLine("\t\t/// <summary>Callback invoked when the setter is accessed.</summary>");
				sb.AppendLine($"\t\tpublic global::System.Action<{className}, {member.ReturnType}>? OnSet {{ get; set; }}");
				sb.AppendLine();
			}

			// RecordGet method
			if (member.HasGetter)
			{
				sb.AppendLine("\t\t/// <summary>Records a getter access.</summary>");
				sb.AppendLine("\t\tpublic void RecordGet() => GetCount++;");
				sb.AppendLine();
			}

			// RecordSet method (typed)
			if (member.HasSetter)
			{
				var nullableType = MakeNullable(member.ReturnType);
				sb.AppendLine("\t\t/// <summary>Records a setter access.</summary>");
				sb.AppendLine($"\t\tpublic void RecordSet({nullableType} value) {{ SetCount++; LastSetValue = value; }}");
				sb.AppendLine();
			}

			// Reset method
			sb.AppendLine("\t\t/// <summary>Resets all tracking state.</summary>");
			sb.Append("\t\tpublic void Reset() { ");
			if (member.HasGetter) sb.Append("GetCount = 0; OnGet = null; ");
			if (member.HasSetter) sb.Append("SetCount = 0; LastSetValue = default; OnSet = null; ");
			sb.AppendLine("}");
		}
		else
		{
			// Method tracking: CallCount, WasCalled, LastCallArgs, AllCalls, OnCall
			var paramCount = member.Parameters.Count;
			var isVoid = member.ReturnType == "void";

			if (paramCount == 1)
			{
				// Single parameter - store directly (tuples require 2+ elements)
				var param = member.Parameters.GetArray()![0];
				var nullableType = MakeNullable(param.Type);

				sb.AppendLine($"\t\tprivate readonly global::System.Collections.Generic.List<{param.Type}> _calls = new();");
				sb.AppendLine();
				sb.AppendLine("\t\t/// <summary>Number of times this method was called.</summary>");
				sb.AppendLine("\t\tpublic int CallCount => _calls.Count;");
				sb.AppendLine();
				sb.AppendLine("\t\t/// <summary>True if this method was called at least once.</summary>");
				sb.AppendLine("\t\tpublic bool WasCalled => _calls.Count > 0;");
				sb.AppendLine();
				sb.AppendLine($"\t\t/// <summary>The '{param.Name}' argument from the most recent call.</summary>");
				sb.AppendLine($"\t\tpublic {nullableType} LastCallArg => _calls.Count > 0 ? _calls[_calls.Count - 1] : null;");
				sb.AppendLine();
				sb.AppendLine("\t\t/// <summary>All recorded calls with their arguments.</summary>");
				sb.AppendLine($"\t\tpublic global::System.Collections.Generic.IReadOnlyList<{param.Type}> AllCalls => _calls;");
				sb.AppendLine();

				// OnCall callback: Func<TKnockOff, TArg, TReturn>? or Action<TKnockOff, TArg>?
				sb.AppendLine("\t\t/// <summary>Callback invoked when the method is called. If set (and returns non-null for Func), its result is used.</summary>");
				if (isVoid)
				{
					sb.AppendLine($"\t\tpublic global::System.Action<{className}, {param.Type}>? OnCall {{ get; set; }}");
				}
				else
				{
					sb.AppendLine($"\t\tpublic global::System.Func<{className}, {param.Type}, {member.ReturnType}>? OnCall {{ get; set; }}");
				}
				sb.AppendLine();

				sb.AppendLine("\t\t/// <summary>Records a method call.</summary>");
				sb.AppendLine($"\t\tpublic void RecordCall({param.Type} {param.Name}) => _calls.Add({param.Name});");
				sb.AppendLine();

				sb.AppendLine("\t\t/// <summary>Resets all tracking state.</summary>");
				sb.AppendLine("\t\tpublic void Reset() { _calls.Clear(); OnCall = null; }");
			}
			else if (paramCount > 1)
			{
				// Multiple parameters - use tuple
				var tupleType = GetTupleType(member.Parameters);

				sb.AppendLine($"\t\tprivate readonly global::System.Collections.Generic.List<{tupleType}> _calls = new();");
				sb.AppendLine();
				sb.AppendLine("\t\t/// <summary>Number of times this method was called.</summary>");
				sb.AppendLine("\t\tpublic int CallCount => _calls.Count;");
				sb.AppendLine();
				sb.AppendLine("\t\t/// <summary>True if this method was called at least once.</summary>");
				sb.AppendLine("\t\tpublic bool WasCalled => _calls.Count > 0;");
				sb.AppendLine();
				sb.AppendLine("\t\t/// <summary>The arguments from the most recent call.</summary>");
				sb.AppendLine($"\t\tpublic {tupleType}? LastCallArgs => _calls.Count > 0 ? _calls[_calls.Count - 1] : null;");
				sb.AppendLine();
				sb.AppendLine("\t\t/// <summary>All recorded calls with their arguments.</summary>");
				sb.AppendLine($"\t\tpublic global::System.Collections.Generic.IReadOnlyList<{tupleType}> AllCalls => _calls;");
				sb.AppendLine();

				// OnCall callback with tuple parameter: Func<TKnockOff, (T1, T2), TReturn>? or Action<TKnockOff, (T1, T2)>?
				sb.AppendLine("\t\t/// <summary>Callback invoked when the method is called. If set (and returns non-null for Func), its result is used.</summary>");
				if (isVoid)
				{
					sb.AppendLine($"\t\tpublic global::System.Action<{className}, {tupleType}>? OnCall {{ get; set; }}");
				}
				else
				{
					sb.AppendLine($"\t\tpublic global::System.Func<{className}, {tupleType}, {member.ReturnType}>? OnCall {{ get; set; }}");
				}
				sb.AppendLine();

				// RecordCall with typed parameters
				var paramList = string.Join(", ", member.Parameters.Select(p => FormatParameter(p)));
				var tupleConstruction = GetTupleConstruction(member.Parameters);
				sb.AppendLine("\t\t/// <summary>Records a method call.</summary>");
				sb.AppendLine($"\t\tpublic void RecordCall({paramList}) => _calls.Add({tupleConstruction});");
				sb.AppendLine();

				sb.AppendLine("\t\t/// <summary>Resets all tracking state.</summary>");
				sb.AppendLine("\t\tpublic void Reset() { _calls.Clear(); OnCall = null; }");
			}
			else
			{
				// No parameters - just track call count
				sb.AppendLine("\t\t/// <summary>Number of times this method was called.</summary>");
				sb.AppendLine("\t\tpublic int CallCount { get; private set; }");
				sb.AppendLine();
				sb.AppendLine("\t\t/// <summary>True if this method was called at least once.</summary>");
				sb.AppendLine("\t\tpublic bool WasCalled => CallCount > 0;");
				sb.AppendLine();

				// OnCall callback: Func<TKnockOff, TReturn>? or Action<TKnockOff>?
				sb.AppendLine("\t\t/// <summary>Callback invoked when the method is called. If set (and returns non-null for Func), its result is used.</summary>");
				if (isVoid)
				{
					sb.AppendLine($"\t\tpublic global::System.Action<{className}>? OnCall {{ get; set; }}");
				}
				else
				{
					sb.AppendLine($"\t\tpublic global::System.Func<{className}, {member.ReturnType}>? OnCall {{ get; set; }}");
				}
				sb.AppendLine();

				sb.AppendLine("\t\t/// <summary>Records a method call.</summary>");
				sb.AppendLine("\t\tpublic void RecordCall() => CallCount++;");
				sb.AppendLine();
				sb.AppendLine("\t\t/// <summary>Resets all tracking state.</summary>");
				sb.AppendLine("\t\tpublic void Reset() { CallCount = 0; OnCall = null; }");
			}
		}

		sb.AppendLine("\t}");
		sb.AppendLine();
	}

	/// <summary>
	/// Generate ExecutionDetails class content for indexers
	/// </summary>
	private static void GenerateIndexerExecutionDetails(System.Text.StringBuilder sb, InterfaceMemberInfo member, string className)
	{
		// Get the key type from indexer parameters (typically one parameter)
		var keyType = member.IndexerParameters.Count > 0
			? member.IndexerParameters.GetArray()![0].Type
			: "object";
		var keyParamName = member.IndexerParameters.Count > 0
			? member.IndexerParameters.GetArray()![0].Name
			: "key";

		if (member.HasGetter)
		{
			// Track keys accessed
			sb.AppendLine($"\t\tprivate readonly global::System.Collections.Generic.List<{keyType}> _getKeys = new();");
			sb.AppendLine();

			sb.AppendLine("\t\t/// <summary>Number of times the getter was accessed.</summary>");
			sb.AppendLine("\t\tpublic int GetCount => _getKeys.Count;");
			sb.AppendLine();

			var nullableKeyType = MakeNullable(keyType);
			sb.AppendLine("\t\t/// <summary>The key from the most recent getter access.</summary>");
			sb.AppendLine($"\t\tpublic {nullableKeyType} LastGetKey => _getKeys.Count > 0 ? _getKeys[_getKeys.Count - 1] : default;");
			sb.AppendLine();

			sb.AppendLine("\t\t/// <summary>All keys accessed via the getter.</summary>");
			sb.AppendLine($"\t\tpublic global::System.Collections.Generic.IReadOnlyList<{keyType}> AllGetKeys => _getKeys;");
			sb.AppendLine();

			// OnGet callback: Func<TKnockOff, TKey, TReturn>?
			sb.AppendLine("\t\t/// <summary>Callback invoked when the getter is accessed. If set, its return value is used.</summary>");
			sb.AppendLine($"\t\tpublic global::System.Func<{className}, {keyType}, {member.ReturnType}>? OnGet {{ get; set; }}");
			sb.AppendLine();

			sb.AppendLine("\t\t/// <summary>Records a getter access.</summary>");
			sb.AppendLine($"\t\tpublic void RecordGet({keyType} {keyParamName}) => _getKeys.Add({keyParamName});");
			sb.AppendLine();
		}

		if (member.HasSetter)
		{
			// Track key-value pairs set
			var tupleType = $"({keyType} {keyParamName}, {member.ReturnType} value)";

			sb.AppendLine($"\t\tprivate readonly global::System.Collections.Generic.List<{tupleType}> _setEntries = new();");
			sb.AppendLine();

			sb.AppendLine("\t\t/// <summary>Number of times the setter was accessed.</summary>");
			sb.AppendLine("\t\tpublic int SetCount => _setEntries.Count;");
			sb.AppendLine();

			sb.AppendLine("\t\t/// <summary>The key-value pair from the most recent setter access.</summary>");
			sb.AppendLine($"\t\tpublic {tupleType}? LastSetEntry => _setEntries.Count > 0 ? _setEntries[_setEntries.Count - 1] : null;");
			sb.AppendLine();

			sb.AppendLine("\t\t/// <summary>All key-value pairs set via the setter.</summary>");
			sb.AppendLine($"\t\tpublic global::System.Collections.Generic.IReadOnlyList<{tupleType}> AllSetEntries => _setEntries;");
			sb.AppendLine();

			// OnSet callback: Action<TKnockOff, TKey, TValue>?
			sb.AppendLine("\t\t/// <summary>Callback invoked when the setter is accessed.</summary>");
			sb.AppendLine($"\t\tpublic global::System.Action<{className}, {keyType}, {member.ReturnType}>? OnSet {{ get; set; }}");
			sb.AppendLine();

			sb.AppendLine("\t\t/// <summary>Records a setter access.</summary>");
			sb.AppendLine($"\t\tpublic void RecordSet({keyType} {keyParamName}, {member.ReturnType} value) => _setEntries.Add(({keyParamName}, value));");
			sb.AppendLine();
		}

		// Reset method
		sb.AppendLine("\t\t/// <summary>Resets all tracking state.</summary>");
		sb.Append("\t\tpublic void Reset() { ");
		if (member.HasGetter) sb.Append("_getKeys.Clear(); OnGet = null; ");
		if (member.HasSetter) sb.Append("_setEntries.Clear(); OnSet = null; ");
		sb.AppendLine("}");
	}

	/// <summary>
	/// Generate handler class for a method group (handles overloads)
	/// </summary>
	private static void GenerateMethodGroupHandlerClass(System.Text.StringBuilder sb, MethodGroupInfo group, string className)
	{
		sb.AppendLine($"\t/// <summary>Tracks and configures behavior for {group.Name}.</summary>");
		sb.AppendLine($"\tpublic sealed class {group.Name}Handler");
		sb.AppendLine("\t{");

		var hasOverloads = group.Overloads.Count > 1;
		var combinedParams = group.CombinedParameters;
		var hasCombinedParams = combinedParams.Count > 0;

		// 1. Generate delegate types for each overload
		var delegateIndex = 0;
		foreach (var overload in group.Overloads)
		{
			var delegateName = hasOverloads
				? $"{group.Name}Delegate{delegateIndex}"
				: $"{group.Name}Delegate";

			var paramList = string.Join(", ", overload.Parameters.Select(p => FormatParameter(p)));
			var fullParamList = string.IsNullOrEmpty(paramList)
				? $"{className} ko"
				: $"{className} ko, {paramList}";

			sb.AppendLine($"\t\t/// <summary>Delegate for {group.Name}({paramList}).</summary>");
			if (group.IsVoid)
			{
				sb.AppendLine($"\t\tpublic delegate void {delegateName}({fullParamList});");
			}
			else
			{
				sb.AppendLine($"\t\tpublic delegate {group.ReturnType} {delegateName}({fullParamList});");
			}
			sb.AppendLine();
			delegateIndex++;
		}

		// 2. Private callback storage for each overload
		delegateIndex = 0;
		foreach (var overload in group.Overloads)
		{
			var delegateName = hasOverloads
				? $"{group.Name}Delegate{delegateIndex}"
				: $"{group.Name}Delegate";
			var fieldName = hasOverloads ? $"_onCall{delegateIndex}" : "_onCall";

			sb.AppendLine($"\t\tprivate {delegateName}? {fieldName};");
			delegateIndex++;
		}
		sb.AppendLine();

		// 3. Combined tracking list (only input parameters - exclude out params)
		var inputParams = GetInputCombinedParameters(combinedParams).ToArray();
		if (inputParams.Length == 1)
		{
			// Single input parameter - store directly (tuples require 2+ elements)
			var param = inputParams[0];

			sb.AppendLine($"\t\tprivate readonly global::System.Collections.Generic.List<{param.Type}> _calls = new();");
			sb.AppendLine();

			sb.AppendLine("\t\t/// <summary>Number of times this method was called.</summary>");
			sb.AppendLine("\t\tpublic int CallCount => _calls.Count;");
			sb.AppendLine();

			sb.AppendLine("\t\t/// <summary>True if this method was called at least once.</summary>");
			sb.AppendLine("\t\tpublic bool WasCalled => _calls.Count > 0;");
			sb.AppendLine();

			var nullableType = MakeNullable(param.Type);
			sb.AppendLine($"\t\t/// <summary>The '{param.Name}' argument from the most recent call.</summary>");
			sb.AppendLine($"\t\tpublic {nullableType} LastCallArg => _calls.Count > 0 ? _calls[_calls.Count - 1] : null;");
			sb.AppendLine();

			sb.AppendLine("\t\t/// <summary>All recorded calls with their arguments.</summary>");
			sb.AppendLine($"\t\tpublic global::System.Collections.Generic.IReadOnlyList<{param.Type}> AllCalls => _calls;");
			sb.AppendLine();
		}
		else if (inputParams.Length > 1)
		{
			// Multiple input parameters - use tuple
			var tupleElements = inputParams.Select(p => $"{p.NullableType} {p.Name}");
			var tupleType = $"({string.Join(", ", tupleElements)})";

			sb.AppendLine($"\t\tprivate readonly global::System.Collections.Generic.List<{tupleType}> _calls = new();");
			sb.AppendLine();

			sb.AppendLine("\t\t/// <summary>Number of times this method was called.</summary>");
			sb.AppendLine("\t\tpublic int CallCount => _calls.Count;");
			sb.AppendLine();

			sb.AppendLine("\t\t/// <summary>True if this method was called at least once.</summary>");
			sb.AppendLine("\t\tpublic bool WasCalled => _calls.Count > 0;");
			sb.AppendLine();

			sb.AppendLine("\t\t/// <summary>Arguments from the most recent call (nullable for params not in all overloads).</summary>");
			sb.AppendLine($"\t\tpublic {tupleType}? LastCallArgs => _calls.Count > 0 ? _calls[_calls.Count - 1] : null;");
			sb.AppendLine();

			sb.AppendLine("\t\t/// <summary>All recorded calls with their arguments.</summary>");
			sb.AppendLine($"\t\tpublic global::System.Collections.Generic.IReadOnlyList<{tupleType}> AllCalls => _calls;");
			sb.AppendLine();
		}
		else
		{
			// No parameters - just track count
			sb.AppendLine("\t\t/// <summary>Number of times this method was called.</summary>");
			sb.AppendLine("\t\tpublic int CallCount { get; private set; }");
			sb.AppendLine();

			sb.AppendLine("\t\t/// <summary>True if this method was called at least once.</summary>");
			sb.AppendLine("\t\tpublic bool WasCalled => CallCount > 0;");
			sb.AppendLine();
		}

		// 4. OnCall methods for each overload (uses delegates so compiler can resolve by signature)
		delegateIndex = 0;
		foreach (var overload in group.Overloads)
		{
			var delegateName = hasOverloads
				? $"{group.Name}Delegate{delegateIndex}"
				: $"{group.Name}Delegate";
			var fieldName = hasOverloads ? $"_onCall{delegateIndex}" : "_onCall";
			var paramDesc = overload.Parameters.Count == 0
				? "parameterless"
				: string.Join(", ", overload.Parameters.Select(p => p.Name));

			sb.AppendLine($"\t\t/// <summary>Sets callback for {group.Name}({paramDesc}) overload.</summary>");
			sb.AppendLine($"\t\tpublic void OnCall({delegateName} callback) => {fieldName} = callback;");
			sb.AppendLine();
			delegateIndex++;
		}

		// 5. Internal TryGetCallback methods for generated code to use
		delegateIndex = 0;
		foreach (var overload in group.Overloads)
		{
			var delegateName = hasOverloads
				? $"{group.Name}Delegate{delegateIndex}"
				: $"{group.Name}Delegate";
			var fieldName = hasOverloads ? $"_onCall{delegateIndex}" : "_onCall";
			var methodSuffix = hasOverloads ? delegateIndex.ToString() : "";

			sb.AppendLine($"\t\tinternal {delegateName}? GetCallback{methodSuffix}() => {fieldName};");
			delegateIndex++;
		}
		sb.AppendLine();

		// 6. RecordCall methods for each overload (only input params, no out/ref keywords)
		delegateIndex = 0;
		foreach (var overload in group.Overloads)
		{
			// Only include input parameters in RecordCall (exclude out params, strip ref)
			var inputOverloadParams = GetInputParameters(overload.Parameters).ToArray();
			var paramList = string.Join(", ", inputOverloadParams.Select(p => FormatRecordCallParameter(p)));

			sb.AppendLine($"\t\t/// <summary>Records a method call.</summary>");
			if (inputParams.Length == 1)
			{
				// Single input parameter - add directly
				var param = inputParams[0];
				var matchingParam = inputOverloadParams.FirstOrDefault(p => p.Name == param.Name);
				var addValue = matchingParam != null ? matchingParam.Name : "default";
				sb.AppendLine($"\t\tpublic void RecordCall({paramList}) => _calls.Add({addValue});");
			}
			else if (inputParams.Length > 1)
			{
				// Build tuple with nulls for missing params (only input params)
				var tupleValues = new List<string>();
				foreach (var inputParam in inputParams)
				{
					var matchingParam = inputOverloadParams.FirstOrDefault(p => p.Name == inputParam.Name);
					tupleValues.Add(matchingParam != null ? matchingParam.Name : "default");
				}
				var tupleConstruction = $"({string.Join(", ", tupleValues)})";

				sb.AppendLine($"\t\tpublic void RecordCall({paramList}) => _calls.Add({tupleConstruction});");
			}
			else
			{
				// No input parameters - just track count
				sb.AppendLine($"\t\tpublic void RecordCall() => CallCount++;");
			}
			delegateIndex++;
		}
		sb.AppendLine();

		// 7. Reset method
		sb.AppendLine("\t\t/// <summary>Resets all tracking state.</summary>");
		sb.Append("\t\tpublic void Reset() { ");
		if (inputParams.Length > 0)
		{
			sb.Append("_calls.Clear(); ");
		}
		else
		{
			sb.Append("CallCount = 0; ");
		}
		delegateIndex = 0;
		foreach (var _ in group.Overloads)
		{
			var fieldName = hasOverloads ? $"_onCall{delegateIndex}" : "_onCall";
			sb.Append($"{fieldName} = null; ");
			delegateIndex++;
		}
		sb.AppendLine("}");

		sb.AppendLine("\t}");
		sb.AppendLine();
	}

	/// <summary>
	/// Makes a type nullable if it isn't already
	/// </summary>
	private static string MakeNullable(string type)
	{
		if (type.EndsWith("?"))
			return type;

		// Value types need ? suffix, reference types are already nullable with #nullable enable
		// But for clarity and to handle both, we add ? if not present
		return type + "?";
	}

	/// <summary>
	/// Gets the tuple type string for method parameters: (T1 name1, T2 name2, ...)
	/// </summary>
	private static string GetTupleType(EquatableArray<ParameterInfo> parameters)
	{
		var elements = parameters.Select(p => FormatParameter(p));
		return $"({string.Join(", ", elements)})";
	}

	/// <summary>
	/// Gets the tuple construction expression: (name1, name2, ...)
	/// </summary>
	private static string GetTupleConstruction(EquatableArray<ParameterInfo> parameters)
	{
		var elements = parameters.Select(p => p.Name);
		return $"({string.Join(", ", elements)})";
	}

	/// <summary>
	/// Generate handler class for an event
	/// </summary>
	private static void GenerateEventHandlerClass(System.Text.StringBuilder sb, EventMemberInfo evt, string className)
	{
		sb.AppendLine($"\t/// <summary>Tracks and raises {evt.Name}.</summary>");
		sb.AppendLine($"\tpublic sealed class {evt.Name}Handler");
		sb.AppendLine("\t{");

		// Private handler field - strip trailing ? from delegate type since we add our own
		var delegateTypeForField = evt.FullDelegateTypeName.TrimEnd('?');
		sb.AppendLine($"\t\tprivate {delegateTypeForField}? _handler;");

		// Determine raise tracking type based on parameter count
		var paramCount = evt.DelegateParameters.Count;
		string raiseTrackingType;
		string raiseTrackingConstruction;

		if (paramCount == 0)
		{
			raiseTrackingType = ""; // No tracking for parameterless events
			raiseTrackingConstruction = "";
		}
		else if (paramCount == 1)
		{
			// Single parameter - track directly (no tuple)
			var param = evt.DelegateParameters.GetArray()![0];
			raiseTrackingType = param.Type;
			raiseTrackingConstruction = param.Name;
		}
		else
		{
			// Multiple parameters - use tuple
			raiseTrackingType = GetTupleType(evt.DelegateParameters);
			raiseTrackingConstruction = GetTupleConstruction(evt.DelegateParameters);
		}

		// Raise tracking list (if there are parameters to track)
		if (paramCount > 0)
		{
			sb.AppendLine($"\t\tprivate readonly global::System.Collections.Generic.List<{raiseTrackingType}> _raises = new();");
		}
		sb.AppendLine();

		// === Subscription Tracking ===
		sb.AppendLine("\t\t/// <summary>Number of times handlers were added.</summary>");
		sb.AppendLine("\t\tpublic int SubscribeCount { get; private set; }");
		sb.AppendLine();
		sb.AppendLine("\t\t/// <summary>Number of times handlers were removed.</summary>");
		sb.AppendLine("\t\tpublic int UnsubscribeCount { get; private set; }");
		sb.AppendLine();
		sb.AppendLine("\t\t/// <summary>True if at least one handler is subscribed.</summary>");
		sb.AppendLine("\t\tpublic bool HasSubscribers => _handler != null;");
		sb.AppendLine();

		// === Raise Tracking ===
		if (paramCount == 0)
		{
			sb.AppendLine("\t\t/// <summary>Number of times the event was raised.</summary>");
			sb.AppendLine("\t\tpublic int RaiseCount { get; private set; }");
			sb.AppendLine();
		}
		else
		{
			sb.AppendLine("\t\t/// <summary>Number of times the event was raised.</summary>");
			sb.AppendLine("\t\tpublic int RaiseCount => _raises.Count;");
			sb.AppendLine();
		}

		sb.AppendLine("\t\t/// <summary>True if the event was raised at least once.</summary>");
		sb.AppendLine("\t\tpublic bool WasRaised => RaiseCount > 0;");
		sb.AppendLine();

		if (paramCount == 1)
		{
			var param = evt.DelegateParameters.GetArray()![0];
			var nullableType = MakeNullable(param.Type);
			sb.AppendLine($"\t\t/// <summary>Arguments from the most recent raise.</summary>");
			sb.AppendLine($"\t\tpublic {nullableType} LastRaiseArgs => _raises.Count > 0 ? _raises[_raises.Count - 1] : default;");
			sb.AppendLine();
			sb.AppendLine($"\t\t/// <summary>All recorded raise invocations.</summary>");
			sb.AppendLine($"\t\tpublic global::System.Collections.Generic.IReadOnlyList<{param.Type}> AllRaises => _raises;");
			sb.AppendLine();
		}
		else if (paramCount > 1)
		{
			sb.AppendLine($"\t\t/// <summary>Arguments from the most recent raise.</summary>");
			sb.AppendLine($"\t\tpublic {raiseTrackingType}? LastRaiseArgs => _raises.Count > 0 ? _raises[_raises.Count - 1] : null;");
			sb.AppendLine();
			sb.AppendLine($"\t\t/// <summary>All recorded raise invocations.</summary>");
			sb.AppendLine($"\t\tpublic global::System.Collections.Generic.IReadOnlyList<{raiseTrackingType}> AllRaises => _raises;");
			sb.AppendLine();
		}

		// === Add/Remove methods ===
		sb.AppendLine($"\t\tinternal void Add({evt.FullDelegateTypeName} handler)");
		sb.AppendLine("\t\t{");
		sb.AppendLine("\t\t\t_handler += handler;");
		sb.AppendLine("\t\t\tSubscribeCount++;");
		sb.AppendLine("\t\t}");
		sb.AppendLine();

		sb.AppendLine($"\t\tinternal void Remove({evt.FullDelegateTypeName} handler)");
		sb.AppendLine("\t\t{");
		sb.AppendLine("\t\t\t_handler -= handler;");
		sb.AppendLine("\t\t\tUnsubscribeCount++;");
		sb.AppendLine("\t\t}");
		sb.AppendLine();

		// === Raise method(s) ===
		GenerateEventRaiseMethods(sb, evt, raiseTrackingConstruction);

		// === Reset and Clear ===
		sb.AppendLine("\t\t/// <summary>Resets all tracking counters.</summary>");
		sb.Append("\t\tpublic void Reset() { SubscribeCount = 0; UnsubscribeCount = 0; ");
		if (paramCount == 0)
			sb.Append("RaiseCount = 0; ");
		else
			sb.Append("_raises.Clear(); ");
		sb.AppendLine("}");
		sb.AppendLine();

		sb.AppendLine("\t\t/// <summary>Clears all handlers and resets tracking.</summary>");
		sb.AppendLine("\t\tpublic void Clear() { _handler = null; Reset(); }");

		sb.AppendLine("\t}");
		sb.AppendLine();
	}

	/// <summary>
	/// Generate Raise methods for an event handler
	/// </summary>
	private static void GenerateEventRaiseMethods(System.Text.StringBuilder sb, EventMemberInfo evt, string raiseTrackingConstruction)
	{
		var paramList = string.Join(", ", evt.DelegateParameters.Select(p => FormatParameter(p)));
		var argList = string.Join(", ", evt.DelegateParameters.Select(p => p.Name));
		var paramCount = evt.DelegateParameters.Count;

		if (evt.IsAsync)
		{
			// Async: iterate through invocation list and await each
			sb.AppendLine($"\t\t/// <summary>Raises the event and awaits all handlers sequentially.</summary>");
			sb.AppendLine($"\t\tpublic async global::System.Threading.Tasks.Task RaiseAsync({paramList})");
			sb.AppendLine("\t\t{");
			if (paramCount > 0)
				sb.AppendLine($"\t\t\t_raises.Add({raiseTrackingConstruction});");
			else
				sb.AppendLine("\t\t\tRaiseCount++;");
			sb.AppendLine("\t\t\tif (_handler == null) return;");
			sb.AppendLine("\t\t\tforeach (var h in _handler.GetInvocationList())");
			sb.AppendLine($"\t\t\t\tawait (({evt.FullDelegateTypeName})h)({argList});");
			sb.AppendLine("\t\t}");
		}
		else if (evt.ReturnTypeName != null)
		{
			// Func delegate: return result of invocation
			sb.AppendLine($"\t\t/// <summary>Raises the event and returns the result.</summary>");
			sb.AppendLine($"\t\tpublic {evt.ReturnTypeName} Raise({paramList})");
			sb.AppendLine("\t\t{");
			if (paramCount > 0)
				sb.AppendLine($"\t\t\t_raises.Add({raiseTrackingConstruction});");
			else
				sb.AppendLine("\t\t\tRaiseCount++;");
			sb.AppendLine($"\t\t\treturn _handler?.Invoke({argList}) ?? default!;");
			sb.AppendLine("\t\t}");
		}
		else
		{
			// Void/Action: simple invocation
			sb.AppendLine($"\t\t/// <summary>Raises the event.</summary>");
			sb.AppendLine($"\t\tpublic void Raise({paramList})");
			sb.AppendLine("\t\t{");
			if (paramCount > 0)
				sb.AppendLine($"\t\t\t_raises.Add({raiseTrackingConstruction});");
			else
				sb.AppendLine("\t\t\tRaiseCount++;");
			sb.AppendLine($"\t\t\t_handler?.Invoke({argList});");
			sb.AppendLine("\t\t}");
		}
		sb.AppendLine();

		// Convenience overloads for EventHandler patterns
		if (evt.DelegateKind == EventDelegateKind.EventHandler && paramCount == 2)
		{
			// EventHandler: Raise() with null sender and EventArgs.Empty
			sb.AppendLine("\t\t/// <summary>Raises the event with null sender and empty args.</summary>");
			sb.AppendLine("\t\tpublic void Raise() => Raise(null, global::System.EventArgs.Empty);");
			sb.AppendLine();
		}
		else if (evt.DelegateKind == EventDelegateKind.EventHandlerOfT && paramCount == 2)
		{
			// EventHandler<T>: Raise(e) with null sender
			var eventArgsParam = evt.DelegateParameters.GetArray()![1];
			sb.AppendLine($"\t\t/// <summary>Raises the event with null sender.</summary>");
			sb.AppendLine($"\t\tpublic void Raise({eventArgsParam.Type} e) => Raise(null, e);");
			sb.AppendLine();
		}
	}

	private static void GenerateSpyClass(
		System.Text.StringBuilder sb,
		string className,
		IEnumerable<InterfaceMemberInfo> properties,
		IEnumerable<MethodGroupInfo> methodGroups,
		IEnumerable<EventMemberInfo> events)
	{
		sb.AppendLine($"\t/// <summary>Spy for {className} - tracks invocations and configures behavior.</summary>");
		sb.AppendLine($"\tpublic sealed class {className}Spy");
		sb.AppendLine("\t{");

		// Property handlers
		foreach (var prop in properties)
		{
			sb.AppendLine($"\t\t/// <summary>Handler for {prop.Name}.</summary>");
			sb.AppendLine($"\t\tpublic {prop.Name}Handler {prop.Name} {{ get; }} = new();");
		}

		// Method handlers (delegate-based)
		foreach (var group in methodGroups)
		{
			sb.AppendLine($"\t\t/// <summary>Handler for {group.Name}.</summary>");
			sb.AppendLine($"\t\tpublic {group.Name}Handler {group.Name} {{ get; }} = new();");
		}

		// Event handlers
		foreach (var evt in events)
		{
			sb.AppendLine($"\t\t/// <summary>Handler for {evt.Name} event.</summary>");
			sb.AppendLine($"\t\tpublic {evt.Name}Handler {evt.Name} {{ get; }} = new();");
		}

		sb.AppendLine("\t}");
		sb.AppendLine();
	}

	/// <summary>
	/// Generate explicit interface implementation for an event
	/// </summary>
	private static void GenerateEventImplementation(
		System.Text.StringBuilder sb,
		string interfaceName,
		EventMemberInfo evt)
	{
		sb.AppendLine($"\tevent {evt.FullDelegateTypeName} {interfaceName}.{evt.Name}");
		sb.AppendLine("\t{");
		sb.AppendLine($"\t\tadd => Spy.{evt.Name}.Add(value);");
		sb.AppendLine($"\t\tremove => Spy.{evt.Name}.Remove(value);");
		sb.AppendLine("\t}");
		sb.AppendLine();
	}

	/// <summary>
	/// Generate the backing property for an interface property (called once per unique property name)
	/// </summary>
	private static void GenerateBackingProperty(System.Text.StringBuilder sb, InterfaceMemberInfo prop)
	{
		var accessors = "";
		if (prop.HasGetter && prop.HasSetter)
			accessors = "get; set;";
		else if (prop.HasGetter)
			accessors = "get;";
		else if (prop.HasSetter)
			accessors = "set;";

		var defaultValue = prop.IsNullable ? "" : GetDefaultValue(prop.ReturnType);
		var initializer = !string.IsNullOrEmpty(defaultValue) ? $" = {defaultValue};" : "";

		sb.AppendLine($"\t/// <summary>Backing field for {prop.Name}.</summary>");
		sb.AppendLine($"\tprotected {prop.ReturnType} {prop.Name}Backing {{ {accessors} }}{initializer}");
		sb.AppendLine();
	}

	/// <summary>
	/// Generate backing dictionary for an indexer property
	/// </summary>
	private static void GenerateIndexerBackingDictionary(System.Text.StringBuilder sb, InterfaceMemberInfo indexer)
	{
		var keyType = indexer.IndexerParameters.Count > 0
			? indexer.IndexerParameters.GetArray()![0].Type
			: "object";

		sb.AppendLine($"\t/// <summary>Backing dictionary for {indexer.Name}. Pre-populate with values or use OnGet callback.</summary>");
		sb.AppendLine($"\tpublic global::System.Collections.Generic.Dictionary<{keyType}, {indexer.ReturnType}> {indexer.Name}Backing {{ get; }} = new();");
		sb.AppendLine();
	}

	/// <summary>
	/// Generate explicit interface implementation for a property (called for each interface)
	/// </summary>
	private static void GeneratePropertyImplementation(
		System.Text.StringBuilder sb,
		string interfaceName,
		InterfaceMemberInfo prop)
	{
		sb.AppendLine($"\t{prop.ReturnType} {interfaceName}.{prop.Name}");
		sb.AppendLine("\t{");

		if (prop.HasGetter)
		{
			sb.AppendLine("\t\tget");
			sb.AppendLine("\t\t{");
			sb.AppendLine($"\t\t\tSpy.{prop.Name}.RecordGet();");
			// Check OnGet callback first
			sb.AppendLine($"\t\t\tif (Spy.{prop.Name}.OnGet is {{ }} onGetCallback)");
			sb.AppendLine($"\t\t\t\treturn onGetCallback(this);");
			sb.AppendLine($"\t\t\treturn {prop.Name}Backing;");
			sb.AppendLine("\t\t}");
		}

		if (prop.HasSetter)
		{
			sb.AppendLine("\t\tset");
			sb.AppendLine("\t\t{");
			sb.AppendLine($"\t\t\tSpy.{prop.Name}.RecordSet(value);");
			// Check OnSet callback first, else store in backing
			sb.AppendLine($"\t\t\tif (Spy.{prop.Name}.OnSet is {{ }} onSetCallback)");
			sb.AppendLine($"\t\t\t\tonSetCallback(this, value);");
			sb.AppendLine($"\t\t\telse");
			sb.AppendLine($"\t\t\t\t{prop.Name}Backing = value;");
			sb.AppendLine("\t\t}");
		}

		sb.AppendLine("\t}");
		sb.AppendLine();
	}

	/// <summary>
	/// Generate explicit interface implementation for an indexer
	/// </summary>
	private static void GenerateIndexerImplementation(
		System.Text.StringBuilder sb,
		string interfaceName,
		InterfaceMemberInfo indexer)
	{
		var keyType = indexer.IndexerParameters.Count > 0
			? indexer.IndexerParameters.GetArray()![0].Type
			: "object";
		var keyParamName = indexer.IndexerParameters.Count > 0
			? indexer.IndexerParameters.GetArray()![0].Name
			: "key";

		// Explicit indexer implementation syntax: ReturnType IInterface.this[KeyType key]
		sb.AppendLine($"\t{indexer.ReturnType} {interfaceName}.this[{keyType} {keyParamName}]");
		sb.AppendLine("\t{");

		if (indexer.HasGetter)
		{
			sb.AppendLine("\t\tget");
			sb.AppendLine("\t\t{");
			sb.AppendLine($"\t\t\tSpy.{indexer.Name}.RecordGet({keyParamName});");
			// Check OnGet callback first (with key parameter)
			sb.AppendLine($"\t\t\tif (Spy.{indexer.Name}.OnGet is {{ }} onGetCallback)");
			sb.AppendLine($"\t\t\t\treturn onGetCallback(this, {keyParamName});");
			// Fall back to backing dictionary
			sb.AppendLine($"\t\t\tif ({indexer.Name}Backing.TryGetValue({keyParamName}, out var value))");
			sb.AppendLine($"\t\t\t\treturn value;");
			// Return default for nullable, throw for non-nullable
			if (indexer.IsNullable)
			{
				sb.AppendLine($"\t\t\treturn default!;");
			}
			else
			{
				sb.AppendLine($"\t\t\tthrow new global::System.Collections.Generic.KeyNotFoundException($\"Key '{{{keyParamName}}}' not found. Set Spy.{indexer.Name}.OnGet or add to {indexer.Name}Backing dictionary.\");");
			}
			sb.AppendLine("\t\t}");
		}

		if (indexer.HasSetter)
		{
			sb.AppendLine("\t\tset");
			sb.AppendLine("\t\t{");
			sb.AppendLine($"\t\t\tSpy.{indexer.Name}.RecordSet({keyParamName}, value);");
			// Check OnSet callback first
			sb.AppendLine($"\t\t\tif (Spy.{indexer.Name}.OnSet is {{ }} onSetCallback)");
			sb.AppendLine($"\t\t\t\tonSetCallback(this, {keyParamName}, value);");
			sb.AppendLine($"\t\t\telse");
			sb.AppendLine($"\t\t\t\t{indexer.Name}Backing[{keyParamName}] = value;");
			sb.AppendLine("\t\t}");
		}

		sb.AppendLine("\t}");
		sb.AppendLine();
	}

	/// <summary>
	/// Generate method implementation (uses delegate-based handler)
	/// </summary>
	private static void GenerateMethod(
		System.Text.StringBuilder sb,
		string interfaceName,
		InterfaceMemberInfo method,
		KnockOffTypeInfo typeInfo,
		MethodGroupInfo group)
	{
		var paramList = string.Join(", ", method.Parameters.Select(p => FormatParameter(p)));
		var argList = string.Join(", ", method.Parameters.Select(p => FormatArgument(p)));
		var paramCount = method.Parameters.Count;

		// Separate input params for RecordCall (exclude out params)
		var inputParams = GetInputParameters(method.Parameters).ToArray();
		var inputArgList = string.Join(", ", inputParams.Select(p => p.Name));
		var inputParamCount = inputParams.Length;

		// Find out parameters that need initialization
		var outParams = method.Parameters.Where(p => IsOutputParameter(p.RefKind)).ToArray();

		// Check if user defined this method
		var hasUserMethod = typeInfo.UserMethods.Any(um =>
			um.Name == method.Name &&
			um.ReturnType == method.ReturnType &&
			um.Parameters.Count == method.Parameters.Count &&
			um.Parameters.Zip(method.Parameters, (a, b) => a.Type == b.Type).All(x => x));

		var isVoid = method.ReturnType == "void";

		// Detect async patterns
		var isTask = method.ReturnType == "global::System.Threading.Tasks.Task";
		var isValueTask = method.ReturnType == "global::System.Threading.Tasks.ValueTask";
		var isTaskOfT = method.ReturnType.StartsWith("global::System.Threading.Tasks.Task<");
		var isValueTaskOfT = method.ReturnType.StartsWith("global::System.Threading.Tasks.ValueTask<");

		// Find the overload index for this specific method signature
		var overloadIndex = -1;
		for (int i = 0; i < group.Overloads.Count; i++)
		{
			var overload = group.Overloads.GetArray()![i];
			if (overload.Parameters.Count == method.Parameters.Count)
			{
				var matches = true;
				for (int j = 0; j < overload.Parameters.Count && matches; j++)
				{
					if (overload.Parameters.GetArray()![j].Type != method.Parameters.GetArray()![j].Type)
						matches = false;
				}
				if (matches)
				{
					overloadIndex = i;
					break;
				}
			}
		}

		sb.AppendLine($"\t{method.ReturnType} {interfaceName}.{method.Name}({paramList})");
		sb.AppendLine("\t{");

		// Initialize out parameters to default
		foreach (var outParam in outParams)
		{
			sb.AppendLine($"\t\t{outParam.Name} = default!;");
		}

		// Record the call (only input params - out params are outputs, not inputs)
		if (inputParamCount > 0)
		{
			sb.AppendLine($"\t\tSpy.{method.Name}.RecordCall({inputArgList});");
		}
		else
		{
			sb.AppendLine($"\t\tSpy.{method.Name}.RecordCall();");
		}

		// Check for callback using the appropriate GetCallback method
		var hasOverloads = group.Overloads.Count > 1;
		var callbackMethodSuffix = hasOverloads ? overloadIndex.ToString() : "";
		sb.AppendLine($"\t\tif (Spy.{method.Name}.GetCallback{callbackMethodSuffix}() is {{ }} onCallCallback)");
		if (isVoid)
		{
			// Void: just invoke callback
			if (paramCount == 0)
			{
				sb.AppendLine($"\t\t{{ onCallCallback(this); return; }}");
			}
			else
			{
				sb.AppendLine($"\t\t{{ onCallCallback(this, {argList}); return; }}");
			}
		}
		else
		{
			// Return type: invoke callback and return result
			if (paramCount == 0)
			{
				sb.AppendLine($"\t\t\treturn onCallCallback(this);");
			}
			else
			{
				sb.AppendLine($"\t\t\treturn onCallCallback(this, {argList});");
			}
		}

		// Fall back to user method or default behavior
		if (hasUserMethod)
		{
			// Call user-defined method
			if (isVoid)
			{
				sb.AppendLine($"\t\t{method.Name}({argList});");
			}
			else
			{
				sb.AppendLine($"\t\treturn {method.Name}({argList});");
			}
		}
		else if (isVoid)
		{
			// void - no return needed
		}
		else if (isTask)
		{
			// Task (non-generic) - return completed task
			sb.AppendLine($"\t\treturn global::System.Threading.Tasks.Task.CompletedTask;");
		}
		else if (isValueTask)
		{
			// ValueTask (non-generic) - return default (completed)
			sb.AppendLine($"\t\treturn default;");
		}
		else if (isTaskOfT)
		{
			// Task<T> - return Task.FromResult with default value
			if (method.IsNullable)
			{
				sb.AppendLine($"\t\treturn global::System.Threading.Tasks.Task.FromResult<{ExtractGenericArg(method.ReturnType)}>(default!);");
			}
			else
			{
				sb.AppendLine($"\t\tthrow new global::System.InvalidOperationException(\"No implementation provided for non-nullable return type. Define a protected method '{method.Name}' in your partial class, or set Spy.{method.Name}.OnCall.\");");
			}
		}
		else if (isValueTaskOfT)
		{
			// ValueTask<T> - return default (which wraps default value)
			if (method.IsNullable)
			{
				sb.AppendLine($"\t\treturn default;");
			}
			else
			{
				sb.AppendLine($"\t\tthrow new global::System.InvalidOperationException(\"No implementation provided for non-nullable return type. Define a protected method '{method.Name}' in your partial class, or set Spy.{method.Name}.OnCall.\");");
			}
		}
		else if (method.IsNullable)
		{
			// Other nullable return type - return default
			sb.AppendLine($"\t\treturn default!;");
		}
		else
		{
			// Non-nullable return type - throw
			sb.AppendLine($"\t\tthrow new global::System.InvalidOperationException(\"No implementation provided for non-nullable return type. Define a protected method '{method.Name}' in your partial class, or set Spy.{method.Name}.OnCall.\");");
		}

		sb.AppendLine("\t}");
		sb.AppendLine();
	}

	/// <summary>
	/// Extracts the generic type argument from a generic type string like "global::System.Threading.Tasks.Task&lt;int&gt;"
	/// </summary>
	private static string ExtractGenericArg(string genericType)
	{
		var startIndex = genericType.IndexOf('<') + 1;
		var endIndex = genericType.LastIndexOf('>');
		return genericType.Substring(startIndex, endIndex - startIndex);
	}

	private static string GetDefaultValue(string typeName)
	{
		// Handle common value types that need explicit defaults
		if (typeName == "global::System.String" || typeName == "string")
			return "\"\"";

		// Handle collection types - use Array.Empty<T>() for IEnumerable<T> and IReadOnlyCollection<T>
		if (typeName.Contains("IEnumerable<") || typeName.Contains("IReadOnlyCollection<") || typeName.Contains("IReadOnlyList<"))
		{
			var elementType = ExtractGenericArg(typeName);
			return $"global::System.Array.Empty<{elementType}>()";
		}

		return "";
	}
}

#region Transform Model Types (Equatable for Incremental Generation)

internal sealed record KnockOffTypeInfo(
	string Namespace,
	string ClassName,
	EquatableArray<InterfaceInfo> Interfaces,
	EquatableArray<UserMethodInfo> UserMethods) : IEquatable<KnockOffTypeInfo>;

internal sealed record InterfaceInfo(
	string FullName,
	string SimpleName,
	EquatableArray<InterfaceMemberInfo> Members,
	EquatableArray<EventMemberInfo> Events) : IEquatable<InterfaceInfo>;

internal sealed record InterfaceMemberInfo(
	string Name,
	string ReturnType,
	bool IsProperty,
	bool IsIndexer,
	bool HasGetter,
	bool HasSetter,
	bool IsNullable,
	EquatableArray<ParameterInfo> Parameters,
	EquatableArray<ParameterInfo> IndexerParameters) : IEquatable<InterfaceMemberInfo>;

internal sealed record ParameterInfo(
	string Name,
	string Type,
	RefKind RefKind) : IEquatable<ParameterInfo>;

internal sealed record UserMethodInfo(
	string Name,
	string ReturnType,
	EquatableArray<ParameterInfo> Parameters) : IEquatable<UserMethodInfo>;

internal sealed record EventMemberInfo(
	string Name,
	string FullDelegateTypeName,
	EventDelegateKind DelegateKind,
	EquatableArray<ParameterInfo> DelegateParameters,
	string? ReturnTypeName,
	bool IsAsync) : IEquatable<EventMemberInfo>;

internal enum EventDelegateKind
{
	EventHandler,       // System.EventHandler
	EventHandlerOfT,    // System.EventHandler<TEventArgs>
	Action,             // System.Action or Action<T...>
	Func,               // System.Func<..., TResult>
	Custom              // Custom delegate type
}

/// <summary>
/// Represents a group of method overloads with the same name
/// </summary>
internal sealed record MethodGroupInfo(
	string Name,
	string ReturnType,
	bool IsVoid,
	bool IsNullable,
	EquatableArray<MethodOverloadInfo> Overloads,
	EquatableArray<CombinedParameterInfo> CombinedParameters) : IEquatable<MethodGroupInfo>;

/// <summary>
/// Represents a single method overload's parameters
/// </summary>
internal sealed record MethodOverloadInfo(
	EquatableArray<ParameterInfo> Parameters) : IEquatable<MethodOverloadInfo>;

/// <summary>
/// Represents a parameter in the combined tuple (nullable if not in all overloads)
/// </summary>
internal sealed record CombinedParameterInfo(
	string Name,
	string Type,
	string NullableType,
	bool IsNullable,
	RefKind RefKind) : IEquatable<CombinedParameterInfo>;

#endregion
