using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace KnockOff;

[Generator(LanguageNames.CSharp)]
public partial class KnockOffGenerator : IIncrementalGenerator
{
	#region Diagnostics

	/// <summary>
	/// KO1001: Type argument must be an interface, class, or named delegate type.
	/// </summary>
	private static readonly DiagnosticDescriptor KO1001_TypeMustBeInterfaceClassOrDelegate = new(
		id: "KO1001",
		title: "Type must be interface, class, or delegate",
		messageFormat: "Type '{0}' must be an interface, class, or named delegate type",
		category: "KnockOff",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	/// <summary>
	/// KO1002: Multiple interfaces with same simple name from different namespaces.
	/// </summary>
	private static readonly DiagnosticDescriptor KO1002_NameCollision = new(
		id: "KO1002",
		title: "Name collision",
		messageFormat: "Multiple types named '{0}' found; use explicit [KnockOff] pattern for disambiguation",
		category: "KnockOff",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	/// <summary>
	/// KO1003: Stubs type already exists in scope.
	/// </summary>
	private static readonly DiagnosticDescriptor KO1003_StubsTypeConflict = new(
		id: "KO1003",
		title: "Stubs type conflict",
		messageFormat: "Type 'Stubs' conflicts with generated nested class; rename existing type or use explicit pattern",
		category: "KnockOff",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	#region Standalone Stub Diagnostics (KO0xxx)

	/// <summary>
	/// KO0008: Generic standalone stub type parameter count must match interface.
	/// </summary>
	private static readonly DiagnosticDescriptor KO0008_TypeParameterArityMismatch = new(
		id: "KO0008",
		title: "Type parameter count mismatch",
		messageFormat: "Generic standalone stub '{0}' has {1} type parameter(s) but interface '{2}' has {3}. Type parameter count must match exactly.",
		category: "KnockOff",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	/// <summary>
	/// KO0010: Standalone stubs should implement a single interface.
	/// </summary>
	private static readonly DiagnosticDescriptor KO0010_MultipleInterfaces = new(
		id: "KO0010",
		title: "Multiple interfaces on standalone stub",
		messageFormat: "KnockOff stubs should implement a single interface. Create separate stubs for {0}.",
		category: "KnockOff",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	#endregion

	#region Class Stub Diagnostics (KO2xxx)

	/// <summary>
	/// KO2001: Cannot stub sealed class.
	/// </summary>
	private static readonly DiagnosticDescriptor KO2001_CannotStubSealedClass = new(
		id: "KO2001",
		title: "Cannot stub sealed class",
		messageFormat: "Cannot stub sealed class '{0}'",
		category: "KnockOff",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	/// <summary>
	/// KO2002: Type has no accessible constructors.
	/// </summary>
	private static readonly DiagnosticDescriptor KO2002_NoAccessibleConstructors = new(
		id: "KO2002",
		title: "No accessible constructors",
		messageFormat: "Type '{0}' has no accessible constructors",
		category: "KnockOff",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	/// <summary>
	/// KO2003: Non-virtual member skipped (info diagnostic).
	/// </summary>
	private static readonly DiagnosticDescriptor KO2003_NonVirtualMemberSkipped = new(
		id: "KO2003",
		title: "Non-virtual member skipped",
		messageFormat: "Member '{0}.{1}' is not virtual and cannot be intercepted",
		category: "KnockOff",
		defaultSeverity: DiagnosticSeverity.Info,
		isEnabledByDefault: true);

	/// <summary>
	/// KO2004: Class has no virtual or abstract members.
	/// </summary>
	private static readonly DiagnosticDescriptor KO2004_NoVirtualMembers = new(
		id: "KO2004",
		title: "No virtual members",
		messageFormat: "Class '{0}' has no virtual or abstract members to intercept",
		category: "KnockOff",
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true);

	/// <summary>
	/// KO2005: Cannot stub static class.
	/// </summary>
	private static readonly DiagnosticDescriptor KO2005_CannotStubStaticClass = new(
		id: "KO2005",
		title: "Cannot stub static class",
		messageFormat: "Cannot stub static class '{0}'",
		category: "KnockOff",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	/// <summary>
	/// KO2006: Cannot stub built-in type.
	/// </summary>
	private static readonly DiagnosticDescriptor KO2006_CannotStubBuiltInType = new(
		id: "KO2006",
		title: "Cannot stub built-in type",
		messageFormat: "Cannot stub built-in type '{0}'",
		category: "KnockOff",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	#endregion

	#endregion

	#region CS0108 Hiding Prevention

	/// <summary>
	/// Names of members inherited from object that interceptor properties could hide.
	/// When an interceptor property has one of these names, we need the 'new' keyword
	/// to suppress CS0108 warnings (treated as errors with TreatWarningsAsErrors).
	/// </summary>
	private static readonly HashSet<string> ObjectMemberNames = new(StringComparer.Ordinal)
	{
		"Equals",
		"GetHashCode",
		"ToString",
		"GetType"
	};

	/// <summary>
	/// Returns "new " if the interceptor name would hide an inherited object member, empty string otherwise.
	/// </summary>
	private static string GetNewKeywordIfNeeded(string interceptorName) =>
		ObjectMemberNames.Contains(interceptorName) ? "new " : "";

	/// <summary>
	/// Gets the nullability attribute declaration for a setter, if needed.
	/// Returns the appropriate [param: ...] attribute when the interface setter has
	/// [DisallowNull] or [AllowNull], or empty string otherwise.
	/// </summary>
	private static string GetSetterNullabilityAttribute(InterfaceMemberInfo member)
	{
		// For interfaces with asymmetric nullability ([AllowNull] or [DisallowNull] on setter),
		// we suppress CS8769 warning because C# explicit interface implementation doesn't
		// fully support propagating these attributes via [param:] syntax.
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

	#endregion

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
		// Pipeline 1: Explicit [KnockOff] pattern (class implements interfaces)
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

		// Pipeline 2: Inline [KnockOff<T>] pattern (generates Stubs nested class)
		// ForAttributeWithMetadataName triggers once per node with ALL matching attributes
		var inlineStubsToGenerate = context.SyntaxProvider.ForAttributeWithMetadataName(
			"KnockOff.KnockOffAttribute`1",
			predicate: static (node, _) => IsInlineStubCandidate(node),
			transform: static (ctx, _) => TransformInlineStubClass(ctx));

		context.RegisterSourceOutput(inlineStubsToGenerate, static (spc, info) =>
		{
			if (info is not null)
			{
				GenerateInlineStubs(spc, info);
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

		// Generic classes are now supported - removed Phase 1 limitation

		// Must have base list (potential interfaces)
		if (classDecl.BaseList is null || classDecl.BaseList.Types.Count == 0)
			return false;

		return true;
	}

	/// <summary>
	/// Predicate for inline stubs: partial class (doesn't need to implement interfaces).
	/// </summary>
	private static bool IsInlineStubCandidate(SyntaxNode node)
	{
		if (node is not ClassDeclarationSyntax classDecl)
			return false;

		// Must be partial
		if (!classDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
			return false;

		// Must not be abstract
		if (classDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.AbstractKeyword)))
			return false;

		return true;
	}


	/// <summary>
	/// Generate the partial class with explicit interface implementations
	/// </summary>
	private static void GenerateKnockOff(SourceProductionContext context, KnockOffTypeInfo typeInfo)
	{
		// Report diagnostics first
		ReportDiagnostics(context, typeInfo.Diagnostics);

		// Skip generation if there are no interfaces (e.g., due to KO0010 error)
		if (typeInfo.Interfaces.Count == 0)
			return;

		var sb = new System.Text.StringBuilder();

		sb.AppendLine("// <auto-generated/>");
		sb.AppendLine("#nullable enable");
		sb.AppendLine();

		// Check if we have any generic methods - if so, we need LINQ for aggregate tracking
		var hasGenericMethods = typeInfo.Interfaces.Any(iface =>
			iface.Members.Any(m => m.IsGenericMethod));
		if (hasGenericMethods)
		{
			sb.AppendLine("using System.Linq;");
			sb.AppendLine();
		}

		if (!string.IsNullOrEmpty(typeInfo.Namespace))
		{
			sb.AppendLine($"namespace {typeInfo.Namespace};");
			sb.AppendLine();
		}

		// Open containing type wrappers for nested classes
		foreach (var containingType in typeInfo.ContainingTypes)
		{
			var accessMod = string.IsNullOrEmpty(containingType.AccessibilityModifier)
				? ""
				: containingType.AccessibilityModifier + " ";
			sb.AppendLine($"{accessMod}partial {containingType.Keyword} {containingType.Name}");
			sb.AppendLine("{");
		}

		// Generate class declaration with type parameters and constraints for generic stubs
		var typeParamList = SymbolHelpers.FormatTypeParameterList(typeInfo.TypeParameters);
		var constraints = SymbolHelpers.FormatTypeConstraints(typeInfo.TypeParameters);
		var constraintClause = string.IsNullOrEmpty(constraints) ? "" : $" {constraints}";
		sb.AppendLine($"partial class {typeInfo.ClassName}{typeParamList}{constraintClause}");
		sb.AppendLine("{");

		// Class name with type parameters for use in delegate signatures (e.g., "RepositoryStub<T>")
		var classNameWithTypeParams = $"{typeInfo.ClassName}{typeParamList}";

		// Add marker interfaces and SmartDefault helper for generic method tracking if needed
		if (hasGenericMethods)
		{
			sb.AppendLine("\t/// <summary>Marker interface for generic method call tracking.</summary>");
			sb.AppendLine("\tprivate interface IGenericMethodCallTracker { int CallCount { get; } bool WasCalled { get; } }");
			sb.AppendLine();
			sb.AppendLine("\t/// <summary>Marker interface for resettable handlers.</summary>");
			sb.AppendLine("\tprivate interface IResettable { void Reset(); }");
			sb.AppendLine();

			// Add SmartDefault helper for generic method default returns
			sb.AppendLine("\t/// <summary>Gets a smart default value for a generic type at runtime.</summary>");
			sb.AppendLine("\tprivate static T SmartDefault<T>(string methodName)");
			sb.AppendLine("\t{");
			sb.AppendLine("\t\tvar type = typeof(T);");
			sb.AppendLine();
			sb.AppendLine("\t\t// Value types -> default(T)");
			sb.AppendLine("\t\tif (type.IsValueType)");
			sb.AppendLine("\t\t\treturn default!;");
			sb.AppendLine();
			sb.AppendLine("\t\t// Check for parameterless constructor");
			sb.AppendLine("\t\tvar ctor = type.GetConstructor(");
			sb.AppendLine("\t\t\tSystem.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance,");
			sb.AppendLine("\t\t\tnull, System.Type.EmptyTypes, null);");
			sb.AppendLine();
			sb.AppendLine("\t\tif (ctor != null)");
			sb.AppendLine("\t\t\treturn (T)ctor.Invoke(null);");
			sb.AppendLine();
			sb.AppendLine("\t\tthrow new global::System.InvalidOperationException(");
			sb.AppendLine("\t\t\t$\"No implementation provided for {methodName}<{type.Name}>. \" +");
			sb.AppendLine("\t\t\t$\"Define a protected method '{methodName}' in your partial class, or set the handler's OnCall.\");");
			sb.AppendLine("\t}");
			sb.AppendLine();
		}

		// ===== FLAT API GENERATION (v10.9+) =====
		// Build flat name map with collision handling (also avoids user method names)
		var flatNameMap = BuildFlatNameMap(typeInfo.FlatMembers, typeInfo.FlatEvents, typeInfo.UserMethods);
		var flatMethodGroups = GroupMethodsByName(typeInfo.FlatMembers.Where(m => !m.IsProperty && !m.IsIndexer));

		// 1. Generate flat handler classes for properties/indexers
		foreach (var member in typeInfo.FlatMembers)
		{
			if (member.IsProperty || member.IsIndexer)
			{
				var interceptorName = flatNameMap[GetMemberKey(member)];
				GenerateFlatMemberInterceptorClass(sb, member, classNameWithTypeParams, interceptorName);
			}
		}

		// 2. Generate flat handler classes for method groups
		foreach (var group in flatMethodGroups.Values)
		{
			if (IsMixedMethodGroup(group))
			{
				// Mixed group: generate BOTH non-generic and generic handler classes
				var (nonGenericGroup, genericGroup) = SplitMixedGroup(group);

				if (nonGenericGroup is not null)
				{
					GenerateFlatMethodGroupInterceptorClassWithNames(sb, nonGenericGroup, classNameWithTypeParams, flatNameMap);
				}

				if (genericGroup is not null)
				{
					GenerateFlatGenericMethodHandler(sb, genericGroup, classNameWithTypeParams, flatNameMap);
				}
			}
			else
			{
				var groupHasGenericMethods = group.Overloads.Any(o => o.IsGenericMethod);
				if (groupHasGenericMethods)
				{
					// Generic methods use the Of<T>() pattern - generate base handler class
					GenerateFlatGenericMethodHandler(sb, group, classNameWithTypeParams, flatNameMap);
				}
				else
				{
					GenerateFlatMethodGroupInterceptorClassWithNames(sb, group, classNameWithTypeParams, flatNameMap);
				}
			}
		}

		// 3. Generate flat handler classes for events
		foreach (var evt in typeInfo.FlatEvents)
		{
			var interceptorName = flatNameMap[$"event:{evt.Name}"];
			GenerateFlatEventInterceptorClassWithName(sb, evt, classNameWithTypeParams, interceptorName);
		}

		// 4. Generate flat interceptor properties directly on stub
		foreach (var member in typeInfo.FlatMembers)
		{
			if (member.IsProperty || member.IsIndexer)
			{
				var interceptorName = flatNameMap[GetMemberKey(member)];
				sb.AppendLine($"\t/// <summary>Interceptor for {member.Name}.</summary>");
				sb.AppendLine($"\tpublic {GetNewKeywordIfNeeded(interceptorName)}{interceptorName}Interceptor {interceptorName} {{ get; }} = new();");
				sb.AppendLine();
			}
		}

		// 4b. Generate flat interceptor properties for method groups
		foreach (var group in flatMethodGroups.Values)
		{
			if (IsMixedMethodGroup(group))
			{
				// Mixed group: generate BOTH non-generic and generic properties
				var (nonGenericGroup, genericGroup) = SplitMixedGroup(group);

				if (nonGenericGroup is not null)
				{
					GenerateFlatMethodGroupInterceptorProperties(sb, nonGenericGroup, flatNameMap);
				}

				if (genericGroup is not null)
				{
					var interceptorName = flatNameMap[$"method:{group.Name}()_generic"];
					sb.AppendLine($"\t/// <summary>Interceptor for {group.Name} (generic overloads, use .Of&lt;T&gt;()).</summary>");
					sb.AppendLine($"\tpublic {GetNewKeywordIfNeeded(interceptorName)}{interceptorName}Interceptor {interceptorName} {{ get; }} = new();");
					sb.AppendLine();
				}
			}
			else
			{
				var groupHasGenerics = group.Overloads.Any(o => o.IsGenericMethod);
				if (groupHasGenerics)
				{
					// Generic methods get a single property with Of<T>() access
					var interceptorName = flatNameMap[$"method:{group.Name}()_generic"];
					sb.AppendLine($"\t/// <summary>Interceptor for {group.Name} (use .Of&lt;T&gt;() to access typed handler).</summary>");
					sb.AppendLine($"\tpublic {GetNewKeywordIfNeeded(interceptorName)}{interceptorName}Interceptor {interceptorName} {{ get; }} = new();");
					sb.AppendLine();
				}
				else
				{
					GenerateFlatMethodGroupInterceptorProperties(sb, group, flatNameMap);
				}
			}
		}

		// 4c. Generate flat interceptor properties for events
		foreach (var evt in typeInfo.FlatEvents)
		{
			var interceptorName = flatNameMap[$"event:{evt.Name}"];
			sb.AppendLine($"\t/// <summary>Interceptor for {evt.Name} event.</summary>");
			sb.AppendLine($"\tpublic {GetNewKeywordIfNeeded(interceptorName)}{interceptorName}Interceptor {interceptorName} {{ get; }} = new();");
			sb.AppendLine();
		}

		// 5. Generate flat backing dictionaries for indexers only (properties use interceptor.Value)
		foreach (var member in typeInfo.FlatMembers)
		{
			if (member.IsIndexer)
			{
				var interceptorName = flatNameMap[GetMemberKey(member)];
				GenerateFlatIndexerBackingDictionary(sb, member, interceptorName);
			}
			// Properties no longer need backing - they use interceptor.Value
		}

		// 7. Generate explicit interface implementations for ALL members from ALL interfaces
		// We iterate over all interfaces (not FlatMembers) to ensure inherited interface members
		// get their own explicit implementations (e.g., IEnumerable.GetEnumerator() vs IEnumerable<T>.GetEnumerator())
		// Track generated implementations to avoid duplicates when same member appears in multiple InterfaceInfos
		var generatedImplementations = new HashSet<string>();
		foreach (var iface in typeInfo.Interfaces)
		{
			foreach (var member in iface.Members)
			{
				// Create unique key for this specific interface implementation
				var implKey = $"{member.DeclaringInterfaceFullName}.{member.Name}({string.Join(",", member.Parameters.Select(p => p.Type))})";
				if (!generatedImplementations.Add(implKey))
					continue; // Skip duplicates

				var interceptorName = flatNameMap[GetMemberKey(member)];
				if (member.IsIndexer)
				{
					GenerateFlatIndexerImplementationWithName(sb, member, interceptorName);
				}
				else if (member.IsProperty)
				{
					// Check if this property should delegate to a typed counterpart
					// (e.g., IProperty.Value (object) delegates to IProperty<T>.Value (T))
					var (propTarget, propTargetInterface) = FindPropertyDelegationTargetInInterfaces(member, typeInfo.Interfaces);
					if (propTarget != null && propTargetInterface != null)
					{
						GenerateFlatPropertyDelegationImplementation(sb, member, propTarget, propTargetInterface);
					}
					else
					{
						GenerateFlatPropertyImplementationWithName(sb, member, interceptorName);
					}
				}
				else
				{
					// Check if this method should delegate to a typed counterpart
					// (e.g., IRule.RunRule(IValidateBase) delegates to IRule<T>.RunRule(T))
					var (methodTarget, methodTargetInterface) = FindDelegationTargetInInterfaces(member, typeInfo.Interfaces);
					if (methodTarget != null && methodTargetInterface != null)
					{
						GenerateFlatMethodDelegationImplementation(sb, member, methodTarget, methodTargetInterface);
					}
					else
					{
						// Method - find its group for overload handling
						var group = flatMethodGroups[member.Name];
						GenerateFlatMethodImplementationWithName(sb, member, typeInfo, group, flatNameMap);
					}
				}
			}
		}

		// 7b. Generate explicit interface implementations for events
		foreach (var iface in typeInfo.Interfaces)
		{
			foreach (var evt in iface.Events)
			{
				var evtKey = $"{evt.DeclaringInterfaceFullName}.{evt.Name}";
				if (!generatedImplementations.Add(evtKey))
					continue;

				var interceptorName = flatNameMap[$"event:{evt.Name}"];
				GenerateFlatEventImplementationWithName(sb, evt, interceptorName);
			}
		}

		sb.AppendLine("}");

		// Close containing type wrappers for nested classes
		for (int i = 0; i < typeInfo.ContainingTypes.Count; i++)
		{
			sb.AppendLine("}");
		}

		// Build hint name including containing types to ensure uniqueness
		// For generic classes, add arity suffix (e.g., RepositoryStub`1) to make valid filename
		var className = typeInfo.TypeParameters.Count > 0
			? $"{typeInfo.ClassName}`{typeInfo.TypeParameters.Count}"
			: typeInfo.ClassName;
		var hintName = typeInfo.ContainingTypes.Count > 0
			? string.Join(".", typeInfo.ContainingTypes.Select(ct => ct.Name)) + "." + className
			: className;

		context.AddSource($"{hintName}.g.cs", sb.ToString());
	}

	/// <summary>
	/// Generate a per-member Handler class with strongly-typed tracking and callbacks
	/// </summary>
	private static void GenerateMemberHandlerClass(System.Text.StringBuilder sb, InterfaceMemberInfo member, string className)
	{
		sb.AppendLine($"\t/// <summary>Tracks and configures behavior for {member.Name}.</summary>");
		sb.AppendLine($"\tpublic sealed class {member.Name}Interceptor");
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
			// Check for generic methods first - they need special handling
			if (member.IsGenericMethod)
			{
				GenerateGenericMethodHandler(sb, member, className);
				sb.AppendLine("\t}");
				sb.AppendLine();
				return;
			}

			// Method tracking: CallCount, WasCalled, LastCallArg/LastCallArgs (if params), OnCall
			var paramCount = member.Parameters.Count;
			var isVoid = member.ReturnType == "void";

			if (paramCount == 1)
			{
				// Single parameter - track last call only (no List for performance)
				var param = member.Parameters.GetArray()![0];
				var nullableType = MakeNullable(param.Type);

				sb.AppendLine("\t\t/// <summary>Number of times this method was called.</summary>");
				sb.AppendLine("\t\tpublic int CallCount { get; private set; }");
				sb.AppendLine();
				sb.AppendLine("\t\t/// <summary>True if this method was called at least once.</summary>");
				sb.AppendLine("\t\tpublic bool WasCalled => CallCount > 0;");
				sb.AppendLine();
				sb.AppendLine($"\t\t/// <summary>The '{param.Name}' argument from the most recent call.</summary>");
				sb.AppendLine($"\t\tpublic {nullableType} LastCallArg {{ get; private set; }}");
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
				sb.AppendLine($"\t\tpublic void RecordCall({param.Type} {param.Name}) {{ CallCount++; LastCallArg = {param.Name}; }}");
				sb.AppendLine();

				sb.AppendLine("\t\t/// <summary>Resets all tracking state.</summary>");
				sb.AppendLine("\t\tpublic void Reset() { CallCount = 0; LastCallArg = default; OnCall = null; }");
			}
			else if (paramCount > 1)
			{
				// Multiple parameters - track last call only (no List for performance)
				var tupleType = GetTupleType(member.Parameters);
				var paramList = string.Join(", ", member.Parameters.Select(p => FormatParameter(p)));
				var tupleConstruction = GetTupleConstruction(member.Parameters);

				sb.AppendLine("\t\t/// <summary>Number of times this method was called.</summary>");
				sb.AppendLine("\t\tpublic int CallCount { get; private set; }");
				sb.AppendLine();
				sb.AppendLine("\t\t/// <summary>True if this method was called at least once.</summary>");
				sb.AppendLine("\t\tpublic bool WasCalled => CallCount > 0;");
				sb.AppendLine();
				sb.AppendLine("\t\t/// <summary>The arguments from the most recent call.</summary>");
				sb.AppendLine($"\t\tpublic {tupleType}? LastCallArgs {{ get; private set; }}");
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

				sb.AppendLine("\t\t/// <summary>Records a method call.</summary>");
				sb.AppendLine($"\t\tpublic void RecordCall({paramList}) {{ CallCount++; LastCallArgs = {tupleConstruction}; }}");
				sb.AppendLine();

				sb.AppendLine("\t\t/// <summary>Resets all tracking state.</summary>");
				sb.AppendLine("\t\tpublic void Reset() { CallCount = 0; LastCallArgs = default; OnCall = null; }");
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
	/// Generate handler content for generic methods using the Of&lt;T&gt;() pattern.
	/// This generates a base handler with type-keyed access and a nested typed handler class.
	/// </summary>
	private static void GenerateGenericMethodHandler(System.Text.StringBuilder sb, InterfaceMemberInfo member, string className)
	{
		var typeParams = member.TypeParameters.GetArray()!;
		var typeParamNames = string.Join(", ", typeParams.Select(tp => tp.Name));
		var typeParamCount = typeParams.Length;

		// Build constraint clauses for the type parameters
		var constraintClauses = GetConstraintClauses(typeParams);

		// Get non-generic parameters (parameters that are not type parameters)
		var nonGenericParams = member.Parameters
			.Where(p => !typeParams.Any(tp => p.Type == tp.Name || p.Type == tp.Name + "?"))
			.ToArray();

		var isVoid = member.ReturnType == "void" ||
			member.ReturnType == "global::System.Threading.Tasks.Task" ||
			member.ReturnType == "global::System.Threading.Tasks.ValueTask";

		// Build the dictionary key type based on number of type parameters
		var keyType = typeParamCount == 1
			? "global::System.Type"
			: $"({string.Join(", ", typeParams.Select(_ => "global::System.Type"))})";

		var keyConstruction = typeParamCount == 1
			? $"typeof({typeParams[0].Name})"
			: $"({string.Join(", ", typeParams.Select(tp => $"typeof({tp.Name})"))})";

		// --- Base Handler: Dictionary + Of<T>() + aggregate tracking ---
		sb.AppendLine($"\t\tprivate readonly global::System.Collections.Generic.Dictionary<{keyType}, object> _typedHandlers = new();");
		sb.AppendLine();

		// Of<T>() method
		sb.AppendLine($"\t\t/// <summary>Gets the typed handler for the specified type argument(s).</summary>");
		sb.AppendLine($"\t\tpublic {member.Name}TypedHandler<{typeParamNames}> Of<{typeParamNames}>(){constraintClauses}");
		sb.AppendLine("\t\t{");
		sb.AppendLine($"\t\t\tvar key = {keyConstruction};");
		sb.AppendLine($"\t\t\tif (!_typedHandlers.TryGetValue(key, out var handler))");
		sb.AppendLine("\t\t\t{");
		sb.AppendLine($"\t\t\t\thandler = new {member.Name}TypedHandler<{typeParamNames}>();");
		sb.AppendLine("\t\t\t\t_typedHandlers[key] = handler;");
		sb.AppendLine("\t\t\t}");
		sb.AppendLine($"\t\t\treturn ({member.Name}TypedHandler<{typeParamNames}>)handler;");
		sb.AppendLine("\t\t}");
		sb.AppendLine();

		// Aggregate tracking
		sb.AppendLine("\t\t/// <summary>Total number of calls across all type arguments.</summary>");
		sb.AppendLine("\t\tpublic int TotalCallCount => _typedHandlers.Values.Sum(h => ((IGenericMethodCallTracker)h).CallCount);");
		sb.AppendLine();
		sb.AppendLine("\t\t/// <summary>True if this method was called with any type argument.</summary>");
		sb.AppendLine("\t\tpublic bool WasCalled => _typedHandlers.Values.Any(h => ((IGenericMethodCallTracker)h).WasCalled);");
		sb.AppendLine();
		sb.AppendLine($"\t\t/// <summary>All type argument(s) that were used in calls.</summary>");
		sb.AppendLine($"\t\tpublic global::System.Collections.Generic.IReadOnlyList<{keyType}> CalledTypeArguments => _typedHandlers.Keys.ToList();");
		sb.AppendLine();

		// Reset method
		sb.AppendLine("\t\t/// <summary>Resets all typed handlers.</summary>");
		sb.AppendLine("\t\tpublic void Reset()");
		sb.AppendLine("\t\t{");
		sb.AppendLine("\t\t\tforeach (var handler in _typedHandlers.Values)");
		sb.AppendLine("\t\t\t\t((IResettable)handler).Reset();");
		sb.AppendLine("\t\t\t_typedHandlers.Clear();");
		sb.AppendLine("\t\t}");
		sb.AppendLine();

		// --- Nested Typed Handler Class ---
		sb.AppendLine($"\t\t/// <summary>Typed handler for {member.Name} with specific type arguments.</summary>");
		sb.AppendLine($"\t\tpublic sealed class {member.Name}TypedHandler<{typeParamNames}> : IGenericMethodCallTracker, IResettable{constraintClauses}");
		sb.AppendLine("\t\t{");

		// Delegate type - need to use actual return type which may include type parameters
		var delegateReturnType = isVoid ? "void" : member.ReturnType;
		var delegateParams = new List<string> { $"{className} ko" };
		foreach (var p in member.Parameters)
		{
			delegateParams.Add($"{p.Type} {p.Name}");
		}
		var delegateParamList = string.Join(", ", delegateParams);

		sb.AppendLine($"\t\t\t/// <summary>Delegate for {member.Name}.</summary>");
		if (isVoid)
		{
			sb.AppendLine($"\t\t\tpublic delegate void {member.Name}Delegate({delegateParamList});");
		}
		else
		{
			sb.AppendLine($"\t\t\tpublic delegate {delegateReturnType} {member.Name}Delegate({delegateParamList});");
		}
		sb.AppendLine();

		// Tracking storage - based on non-generic parameter count (no List for performance)
		sb.AppendLine("\t\t\t/// <summary>Number of times this method was called with these type arguments.</summary>");
		sb.AppendLine("\t\t\tpublic int CallCount { get; private set; }");
		sb.AppendLine();

		if (nonGenericParams.Length == 1)
		{
			var param = nonGenericParams[0];
			var nullableType = MakeNullable(param.Type);
			sb.AppendLine($"\t\t\t/// <summary>The '{param.Name}' argument from the most recent call.</summary>");
			sb.AppendLine($"\t\t\tpublic {nullableType} LastCallArg {{ get; private set; }}");
			sb.AppendLine();
		}
		else if (nonGenericParams.Length > 1)
		{
			var tupleType = "(" + string.Join(", ", nonGenericParams.Select(p => $"{p.Type} {p.Name}")) + ")";
			sb.AppendLine("\t\t\t/// <summary>The arguments from the most recent call.</summary>");
			sb.AppendLine($"\t\t\tpublic {tupleType}? LastCallArgs {{ get; private set; }}");
			sb.AppendLine();
		}

		sb.AppendLine("\t\t\t/// <summary>True if this method was called at least once with these type arguments.</summary>");
		sb.AppendLine("\t\t\tpublic bool WasCalled => CallCount > 0;");
		sb.AppendLine();

		sb.AppendLine("\t\t\t/// <summary>Callback invoked when this method is called. If set, its return value is used.</summary>");
		sb.AppendLine($"\t\t\tpublic {member.Name}Delegate? OnCall {{ get; set; }}");
		sb.AppendLine();

		// RecordCall method
		sb.AppendLine("\t\t\t/// <summary>Records a method call.</summary>");
		if (nonGenericParams.Length == 0)
		{
			sb.AppendLine("\t\t\tpublic void RecordCall() => CallCount++;");
		}
		else if (nonGenericParams.Length == 1)
		{
			var param = nonGenericParams[0];
			sb.AppendLine($"\t\t\tpublic void RecordCall({param.Type} {param.Name}) {{ CallCount++; LastCallArg = {param.Name}; }}");
		}
		else
		{
			var paramList = string.Join(", ", nonGenericParams.Select(p => $"{p.Type} {p.Name}"));
			var tupleConstruction = "(" + string.Join(", ", nonGenericParams.Select(p => p.Name)) + ")";
			sb.AppendLine($"\t\t\tpublic void RecordCall({paramList}) {{ CallCount++; LastCallArgs = {tupleConstruction}; }}");
		}
		sb.AppendLine();

		// Reset method
		sb.AppendLine("\t\t\t/// <summary>Resets all tracking state.</summary>");
		if (nonGenericParams.Length == 0)
		{
			sb.AppendLine("\t\t\tpublic void Reset() { CallCount = 0; OnCall = null; }");
		}
		else if (nonGenericParams.Length == 1)
		{
			sb.AppendLine("\t\t\tpublic void Reset() { CallCount = 0; LastCallArg = default; OnCall = null; }");
		}
		else
		{
			sb.AppendLine("\t\t\tpublic void Reset() { CallCount = 0; LastCallArgs = default; OnCall = null; }");
		}

		sb.AppendLine("\t\t}");
	}

	/// <summary>
	/// Build constraint clauses for type parameters (e.g., " where T : class, new()")
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
	/// Build constraint clauses for explicit interface implementations.
	/// Only class and struct constraints are allowed (CS0460).
	/// We only emit 'class' constraint when:
	/// 1. The original interface has 'class' constraint, OR
	/// 2. The return type is T? (nullable generic parameter) AND T has any constraint that implies reference type
	/// </summary>
	private static string GetConstraintsForExplicitImpl(TypeParameterInfo[] typeParams, string returnType = "")
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

			// For nullable return types (T?), check if this type parameter needs a class constraint
			// to make T? mean "nullable reference type" instead of "Nullable<T>" (value type wrapper)
			if (returnType.EndsWith("?"))
			{
				var baseReturnType = returnType.TrimEnd('?');
				if (baseReturnType == tp.Name)
				{
					// Return type is T? where T is this type parameter
					// Check if there's any type constraint (not just keywords)
					// Type constraints on classes imply reference type, but interface constraints don't
					// However, we can't easily distinguish at generator time, so check if any constraint
					// that looks like a class (contains "global::" and not an interface)
					// For simplicity, emit class if there's any non-keyword constraint
					var hasTypeConstraint = constraintArray.Any(c =>
						c != "notnull" && c != "unmanaged" && c != "new()");

					if (hasTypeConstraint)
					{
						clauses.Add($" where {tp.Name} : class");
					}
				}
			}
		}
		return string.Join("", clauses);
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
			var nullableKeyType = MakeNullable(keyType);

			sb.AppendLine("\t\t/// <summary>Number of times the getter was accessed.</summary>");
			sb.AppendLine("\t\tpublic int GetCount { get; private set; }");
			sb.AppendLine();

			sb.AppendLine("\t\t/// <summary>The key from the most recent getter access.</summary>");
			sb.AppendLine($"\t\tpublic {nullableKeyType} LastGetKey {{ get; private set; }}");
			sb.AppendLine();

			// OnGet callback: Func<TKnockOff, TKey, TReturn>?
			sb.AppendLine("\t\t/// <summary>Callback invoked when the getter is accessed. If set, its return value is used.</summary>");
			sb.AppendLine($"\t\tpublic global::System.Func<{className}, {keyType}, {member.ReturnType}>? OnGet {{ get; set; }}");
			sb.AppendLine();

			sb.AppendLine("\t\t/// <summary>Records a getter access.</summary>");
			sb.AppendLine($"\t\tpublic void RecordGet({keyType} {keyParamName}) {{ GetCount++; LastGetKey = {keyParamName}; }}");
			sb.AppendLine();
		}

		if (member.HasSetter)
		{
			var tupleType = $"({keyType} {keyParamName}, {member.ReturnType} value)";

			sb.AppendLine("\t\t/// <summary>Number of times the setter was accessed.</summary>");
			sb.AppendLine("\t\tpublic int SetCount { get; private set; }");
			sb.AppendLine();

			sb.AppendLine("\t\t/// <summary>The key-value pair from the most recent setter access.</summary>");
			sb.AppendLine($"\t\tpublic {tupleType}? LastSetEntry {{ get; private set; }}");
			sb.AppendLine();

			// OnSet callback: Action<TKnockOff, TKey, TValue>?
			sb.AppendLine("\t\t/// <summary>Callback invoked when the setter is accessed.</summary>");
			sb.AppendLine($"\t\tpublic global::System.Action<{className}, {keyType}, {member.ReturnType}>? OnSet {{ get; set; }}");
			sb.AppendLine();

			sb.AppendLine("\t\t/// <summary>Records a setter access.</summary>");
			sb.AppendLine($"\t\tpublic void RecordSet({keyType} {keyParamName}, {member.ReturnType} value) {{ SetCount++; LastSetEntry = ({keyParamName}, value); }}");
			sb.AppendLine();
		}

		// Reset method
		sb.AppendLine("\t\t/// <summary>Resets all tracking state.</summary>");
		sb.Append("\t\tpublic void Reset() { ");
		if (member.HasGetter) sb.Append("GetCount = 0; LastGetKey = default; OnGet = null; ");
		if (member.HasSetter) sb.Append("SetCount = 0; LastSetEntry = default; OnSet = null; ");
		sb.AppendLine("}");
	}

	/// <summary>
	/// Generate handler class for a method group (handles overloads)
	/// </summary>
	private static void GenerateMethodGroupHandlerClass(System.Text.StringBuilder sb, MethodGroupInfo group, string className)
	{
		sb.AppendLine($"\t/// <summary>Tracks and configures behavior for {group.Name}.</summary>");
		sb.AppendLine($"\tpublic sealed class {group.Name}Interceptor");
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

		// 3. Call tracking - no List for performance, just track last call
		var inputParams = GetInputCombinedParameters(combinedParams).ToArray();
		sb.AppendLine("\t\t/// <summary>Number of times this method was called.</summary>");
		sb.AppendLine("\t\tpublic int CallCount { get; private set; }");
		sb.AppendLine();

		sb.AppendLine("\t\t/// <summary>True if this method was called at least once.</summary>");
		sb.AppendLine("\t\tpublic bool WasCalled => CallCount > 0;");
		sb.AppendLine();

		if (inputParams.Length == 1)
		{
			var param = inputParams[0];
			var nullableType = MakeNullable(param.Type);
			sb.AppendLine($"\t\t/// <summary>The '{param.Name}' argument from the most recent call.</summary>");
			sb.AppendLine($"\t\tpublic {nullableType} LastCallArg {{ get; private set; }}");
			sb.AppendLine();
		}
		else if (inputParams.Length > 1)
		{
			var tupleElements = inputParams.Select(p => $"{p.NullableType} {p.Name}");
			var tupleType = $"({string.Join(", ", tupleElements)})";
			sb.AppendLine("\t\t/// <summary>Arguments from the most recent call (nullable for params not in all overloads).</summary>");
			sb.AppendLine($"\t\tpublic {tupleType}? LastCallArgs {{ get; private set; }}");
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
				// Single input parameter - store directly
				var param = inputParams[0];
				var matchingParam = inputOverloadParams.FirstOrDefault(p => p.Name == param.Name);
				var assignValue = matchingParam != null ? matchingParam.Name : "default";
				sb.AppendLine($"\t\tpublic void RecordCall({paramList}) {{ CallCount++; LastCallArg = {assignValue}; }}");
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

				sb.AppendLine($"\t\tpublic void RecordCall({paramList}) {{ CallCount++; LastCallArgs = {tupleConstruction}; }}");
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
		sb.Append("\t\tpublic void Reset() { CallCount = 0; ");
		if (inputParams.Length == 1)
		{
			sb.Append("LastCallArg = default; ");
		}
		else if (inputParams.Length > 1)
		{
			sb.Append("LastCallArgs = default; ");
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
	/// Generate handler class for an event
	/// </summary>
	private static void GenerateEventHandlerClass(System.Text.StringBuilder sb, EventMemberInfo evt, string className)
	{
		sb.AppendLine($"\t/// <summary>Tracks and raises {evt.Name}.</summary>");
		sb.AppendLine($"\tpublic sealed class {evt.Name}Interceptor");
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

	/// <summary>
	/// Computes the KO property name for an interface, handling collisions with member names.
	/// If the interface name collides with a member name, adds underscore suffix.
	/// </summary>
	private static string GetKOPropertyName(InterfaceInfo iface)
	{
		// Extract interface name from FullName (keeps the 'I' prefix unlike SimpleName)
		// e.g., "Namespace.IFoo" -> "IFoo", "Namespace.IRepository<User>" -> "IRepository_User"
		var koPropertyName = iface.FullName;

		// Remove namespace prefix - find last dot that's not inside angle brackets
		var depth = 0;
		var lastNonGenericDot = -1;
		for (int i = 0; i < koPropertyName.Length; i++)
		{
			if (koPropertyName[i] == '<') depth++;
			else if (koPropertyName[i] == '>') depth--;
			else if (koPropertyName[i] == '.' && depth == 0) lastNonGenericDot = i;
		}
		if (lastNonGenericDot >= 0)
			koPropertyName = koPropertyName.Substring(lastNonGenericDot + 1);

		// Sanitize for C# identifiers: replace < > , with valid characters
		koPropertyName = koPropertyName
			.Replace("<", "_")
			.Replace(">", "")
			.Replace(",", "_")
			.Replace(" ", "")
			.Replace(".", "_");

		// Check for collision with any member name
		var memberNames = new HashSet<string>();
		foreach (var member in iface.Members)
			memberNames.Add(member.Name);
		foreach (var evt in iface.Events)
			memberNames.Add(evt.Name);

		if (memberNames.Contains(koPropertyName))
			koPropertyName += "_";

		return koPropertyName;
	}

	/// <summary>
	/// Generate an interface KO class with handlers for all members of that interface
	/// </summary>
	private static void GenerateInterfaceKOClass(
		System.Text.StringBuilder sb,
		string knockOffClassName,
		InterfaceInfo iface,
		string koPropertyName,
		Dictionary<string, MethodGroupInfo> methodGroups)
	{
		sb.AppendLine($"\t/// <summary>Tracks invocations and configures behavior for {iface.FullName}.</summary>");
		sb.AppendLine($"\tpublic sealed class {koPropertyName}Interceptors");
		sb.AppendLine("\t{");

		// Property/indexer handlers
		foreach (var member in iface.Members)
		{
			if (member.IsProperty || member.IsIndexer)
			{
				sb.AppendLine($"\t\t/// <summary>Interceptor for {member.Name}.</summary>");
				sb.AppendLine($"\t\tpublic {GetNewKeywordIfNeeded(member.Name)}{koPropertyName}_{member.Name}Interceptor {member.Name} {{ get; }} = new();");
			}
		}

		// Method handlers - for overloaded methods, generate Method1, Method2, etc. (1-based)
		foreach (var group in methodGroups.Values)
		{
			var hasOverloads = group.Overloads.Count > 1;
			if (hasOverloads)
			{
				// Generate separate property for each overload: Method1, Method2, etc. (1-based)
				for (int i = 0; i < group.Overloads.Count; i++)
				{
					var overloadNumber = i + 1; // 1-based numbering
					sb.AppendLine($"\t\t/// <summary>Interceptor for {group.Name} overload {overloadNumber}.</summary>");
					sb.AppendLine($"\t\tpublic {koPropertyName}_{group.Name}{overloadNumber}Interceptor {group.Name}{overloadNumber} {{ get; }} = new();");
				}
			}
			else
			{
				// Single method (no overloads) - generate single property without suffix
				sb.AppendLine($"\t\t/// <summary>Interceptor for {group.Name}.</summary>");
				sb.AppendLine($"\t\tpublic {GetNewKeywordIfNeeded(group.Name)}{koPropertyName}_{group.Name}Interceptor {group.Name} {{ get; }} = new();");
			}
		}

		// Event handlers
		foreach (var evt in iface.Events)
		{
			sb.AppendLine($"\t\t/// <summary>Interceptor for {evt.Name} event.</summary>");
			sb.AppendLine($"\t\tpublic {GetNewKeywordIfNeeded(evt.Name)}{koPropertyName}_{evt.Name}Interceptor {evt.Name} {{ get; }} = new();");
		}

		sb.AppendLine("\t}");
		sb.AppendLine();
	}

	/// <summary>
	/// Generate a per-member Handler class with strongly-typed tracking and callbacks (interface-scoped version)
	/// </summary>
	private static void GenerateInterfaceMemberHandlerClass(
		System.Text.StringBuilder sb,
		InterfaceMemberInfo member,
		string knockOffClassName,
		string koPropertyName)
	{
		var interceptClassName = $"{koPropertyName}_{member.Name}Interceptor";

		sb.AppendLine($"\t/// <summary>Tracks and configures behavior for {koPropertyName}.{member.Name}.</summary>");
		sb.AppendLine($"\tpublic sealed class {interceptClassName}");
		sb.AppendLine("\t{");

		if (member.IsIndexer)
		{
			GenerateIndexerExecutionDetailsForInterface(sb, member, knockOffClassName);
		}
		else if (member.IsProperty)
		{
			// Property tracking: GetCount, SetCount, LastSetValue (typed)
			if (member.HasGetter)
			{
				sb.AppendLine("\t\t/// <summary>Number of times the getter was accessed.</summary>");
				sb.AppendLine("\t\tpublic int GetCount { get; private set; }");
				sb.AppendLine();

				sb.AppendLine("\t\t/// <summary>Callback invoked when the getter is accessed. If set, its return value is used.</summary>");
				sb.AppendLine($"\t\tpublic global::System.Func<{knockOffClassName}, {member.ReturnType}>? OnGet {{ get; set; }}");
				sb.AppendLine();
			}

			if (member.HasSetter)
			{
				sb.AppendLine("\t\t/// <summary>Number of times the setter was accessed.</summary>");
				sb.AppendLine("\t\tpublic int SetCount { get; private set; }");
				sb.AppendLine();

				var nullableType = MakeNullable(member.ReturnType);
				sb.AppendLine("\t\t/// <summary>The value from the most recent setter call.</summary>");
				sb.AppendLine($"\t\tpublic {nullableType} LastSetValue {{ get; private set; }}");
				sb.AppendLine();

				sb.AppendLine("\t\t/// <summary>Callback invoked when the setter is accessed.</summary>");
				sb.AppendLine($"\t\tpublic global::System.Action<{knockOffClassName}, {member.ReturnType}>? OnSet {{ get; set; }}");
				sb.AppendLine();
			}

			if (member.HasGetter)
			{
				sb.AppendLine("\t\t/// <summary>Records a getter access.</summary>");
				sb.AppendLine("\t\tpublic void RecordGet() => GetCount++;");
				sb.AppendLine();
			}

			if (member.HasSetter)
			{
				var nullableType = MakeNullable(member.ReturnType);
				sb.AppendLine("\t\t/// <summary>Records a setter access.</summary>");
				sb.AppendLine($"\t\tpublic void RecordSet({nullableType} value) {{ SetCount++; LastSetValue = value; }}");
				sb.AppendLine();
			}

			sb.AppendLine("\t\t/// <summary>Resets all tracking state.</summary>");
			sb.Append("\t\tpublic void Reset() { ");
			if (member.HasGetter) sb.Append("GetCount = 0; OnGet = null; ");
			if (member.HasSetter) sb.Append("SetCount = 0; LastSetValue = default; OnSet = null; ");
			sb.AppendLine("}");
		}

		sb.AppendLine("\t}");
		sb.AppendLine();
	}

	/// <summary>
	/// Generate indexer execution details content (interface-scoped version)
	/// </summary>
	private static void GenerateIndexerExecutionDetailsForInterface(
		System.Text.StringBuilder sb,
		InterfaceMemberInfo member,
		string knockOffClassName)
	{
		var keyType = member.IndexerParameters.Count > 0
			? member.IndexerParameters.GetArray()![0].Type
			: "object";
		var keyParamName = member.IndexerParameters.Count > 0
			? member.IndexerParameters.GetArray()![0].Name
			: "key";

		if (member.HasGetter)
		{
			var nullableKeyType = MakeNullable(keyType);

			sb.AppendLine("\t\t/// <summary>Number of times the getter was accessed.</summary>");
			sb.AppendLine("\t\tpublic int GetCount { get; private set; }");
			sb.AppendLine();

			sb.AppendLine("\t\t/// <summary>The key from the most recent getter access.</summary>");
			sb.AppendLine($"\t\tpublic {nullableKeyType} LastGetKey {{ get; private set; }}");
			sb.AppendLine();

			sb.AppendLine("\t\t/// <summary>Callback invoked when the getter is accessed. If set, its return value is used.</summary>");
			sb.AppendLine($"\t\tpublic global::System.Func<{knockOffClassName}, {keyType}, {member.ReturnType}>? OnGet {{ get; set; }}");
			sb.AppendLine();

			sb.AppendLine("\t\t/// <summary>Records a getter access.</summary>");
			sb.AppendLine($"\t\tpublic void RecordGet({keyType} {keyParamName}) {{ GetCount++; LastGetKey = {keyParamName}; }}");
			sb.AppendLine();
		}

		if (member.HasSetter)
		{
			var tupleType = $"({keyType} {keyParamName}, {member.ReturnType} value)";

			sb.AppendLine("\t\t/// <summary>Number of times the setter was accessed.</summary>");
			sb.AppendLine("\t\tpublic int SetCount { get; private set; }");
			sb.AppendLine();

			sb.AppendLine("\t\t/// <summary>The key-value pair from the most recent setter access.</summary>");
			sb.AppendLine($"\t\tpublic {tupleType}? LastSetEntry {{ get; private set; }}");
			sb.AppendLine();

			sb.AppendLine("\t\t/// <summary>Callback invoked when the setter is accessed.</summary>");
			sb.AppendLine($"\t\tpublic global::System.Action<{knockOffClassName}, {keyType}, {member.ReturnType}>? OnSet {{ get; set; }}");
			sb.AppendLine();

			sb.AppendLine("\t\t/// <summary>Records a setter access.</summary>");
			sb.AppendLine($"\t\tpublic void RecordSet({keyType} {keyParamName}, {member.ReturnType} value) {{ SetCount++; LastSetEntry = ({keyParamName}, value); }}");
			sb.AppendLine();
		}

		sb.AppendLine("\t\t/// <summary>Resets all tracking state.</summary>");
		sb.Append("\t\tpublic void Reset() { ");
		if (member.HasGetter) sb.Append("GetCount = 0; LastGetKey = default; OnGet = null; ");
		if (member.HasSetter) sb.Append("SetCount = 0; LastSetEntry = default; OnSet = null; ");
		sb.AppendLine("}");
	}

	/// <summary>
	/// Generate handler classes for a method group - interface-scoped version.
	/// For methods with overloads, generates separate handler classes per overload (Method1Handler, Method2Handler - 1-based).
	/// For methods without overloads, generates a single handler class (MethodHandler).
	/// </summary>
	private static void GenerateInterfaceMethodGroupHandlerClass(
		System.Text.StringBuilder sb,
		MethodGroupInfo group,
		string knockOffClassName,
		string koPropertyName)
	{
		var hasOverloads = group.Overloads.Count > 1;

		if (hasOverloads)
		{
			// Generate separate handler class for each overload (1-based: Method1, Method2, etc.)
			for (int i = 0; i < group.Overloads.Count; i++)
			{
				var overload = group.Overloads.GetArray()![i];
				GenerateSingleOverloadHandlerClass(sb, group.Name, overload, group.ReturnType, group.IsVoid, knockOffClassName, koPropertyName, i + 1);
			}
		}
		else
		{
			// Single method (no overloads) - generate single handler without numeric suffix
			var overload = group.Overloads.GetArray()![0];
			GenerateSingleOverloadHandlerClass(sb, group.Name, overload, group.ReturnType, group.IsVoid, knockOffClassName, koPropertyName, null);
		}
	}

	/// <summary>
	/// Generates a single handler class for one method signature.
	/// When overloadIndex is null, generates "MethodHandler". When non-null, generates "Method1Handler", "Method2Handler", etc. (1-based).
	/// </summary>
	private static void GenerateSingleOverloadHandlerClass(
		System.Text.StringBuilder sb,
		string methodName,
		MethodOverloadInfo overload,
		string returnType,
		bool isVoid,
		string knockOffClassName,
		string koPropertyName,
		int? overloadIndex)
	{
		// Generic methods need special handling with Of<T>() pattern
		if (overload.IsGenericMethod)
		{
			GenerateGenericMethodHandlerForInterface(sb, methodName, overload, returnType, isVoid, knockOffClassName, koPropertyName, overloadIndex);
			return;
		}

		var handlerSuffix = overloadIndex.HasValue ? overloadIndex.Value.ToString() : "";
		var interceptClassName = $"{koPropertyName}_{methodName}{handlerSuffix}Interceptor";
		var delegateName = $"{methodName}Delegate";

		// Get input parameters for this specific overload
		var inputParams = GetInputParameters(overload.Parameters).ToArray();
		var paramList = string.Join(", ", overload.Parameters.Select(p => FormatParameter(p)));
		var fullParamList = string.IsNullOrEmpty(paramList)
			? $"{knockOffClassName} ko"
			: $"{knockOffClassName} ko, {paramList}";

		sb.AppendLine($"\t/// <summary>Tracks and configures behavior for {koPropertyName}.{methodName}{handlerSuffix}.</summary>");
		sb.AppendLine($"\tpublic sealed class {interceptClassName}");
		sb.AppendLine("\t{");

		// 1. Delegate type
		sb.AppendLine($"\t\t/// <summary>Delegate for {methodName}({paramList}).</summary>");
		if (isVoid)
		{
			sb.AppendLine($"\t\tpublic delegate void {delegateName}({fullParamList});");
		}
		else
		{
			sb.AppendLine($"\t\tpublic delegate {returnType} {delegateName}({fullParamList});");
		}
		sb.AppendLine();

		// 2. Tracking - uses exact types from this overload (no nullable wrappers)
		if (inputParams.Length == 1)
		{
			var param = inputParams[0];
			var nullableType = MakeNullable(param.Type);

			sb.AppendLine("\t\t/// <summary>Number of times this method was called.</summary>");
			sb.AppendLine("\t\tpublic int CallCount { get; private set; }");
			sb.AppendLine();

			sb.AppendLine("\t\t/// <summary>True if this method was called at least once.</summary>");
			sb.AppendLine("\t\tpublic bool WasCalled => CallCount > 0;");
			sb.AppendLine();

			sb.AppendLine($"\t\t/// <summary>The '{param.Name}' argument from the most recent call.</summary>");
			sb.AppendLine($"\t\tpublic {nullableType} LastCallArg {{ get; private set; }}");
			sb.AppendLine();
		}
		else if (inputParams.Length > 1)
		{
			// Multiple parameters - use tuple with actual types (no nullable wrappers needed)
			var tupleElements = inputParams.Select(p => $"{p.Type} {p.Name}");
			var tupleType = $"({string.Join(", ", tupleElements)})";

			sb.AppendLine("\t\t/// <summary>Number of times this method was called.</summary>");
			sb.AppendLine("\t\tpublic int CallCount { get; private set; }");
			sb.AppendLine();

			sb.AppendLine("\t\t/// <summary>True if this method was called at least once.</summary>");
			sb.AppendLine("\t\tpublic bool WasCalled => CallCount > 0;");
			sb.AppendLine();

			sb.AppendLine("\t\t/// <summary>Arguments from the most recent call.</summary>");
			sb.AppendLine($"\t\tpublic {tupleType}? LastCallArgs {{ get; private set; }}");
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

		// 4. OnCall property - simple assignment like OnGet/OnSet for properties
		sb.AppendLine($"\t\t/// <summary>Callback invoked when this method is called. If set, its return value is used.</summary>");
		sb.AppendLine($"\t\tpublic {delegateName}? OnCall {{ get; set; }}");
		sb.AppendLine();

		// 6. RecordCall method - matches this overload's exact signature
		var recordCallParamList = string.Join(", ", inputParams.Select(p => FormatRecordCallParameter(p)));
		sb.AppendLine($"\t\t/// <summary>Records a method call.</summary>");
		if (inputParams.Length == 1)
		{
			sb.AppendLine($"\t\tpublic void RecordCall({recordCallParamList}) {{ CallCount++; LastCallArg = {inputParams[0].Name}; }}");
		}
		else if (inputParams.Length > 1)
		{
			var tupleConstruction = $"({string.Join(", ", inputParams.Select(p => p.Name))})";
			sb.AppendLine($"\t\tpublic void RecordCall({recordCallParamList}) {{ CallCount++; LastCallArgs = {tupleConstruction}; }}");
		}
		else
		{
			sb.AppendLine($"\t\tpublic void RecordCall() => CallCount++;");
		}
		sb.AppendLine();

		// 6. Reset method
		sb.AppendLine("\t\t/// <summary>Resets all tracking state.</summary>");
		if (inputParams.Length == 1)
		{
			sb.AppendLine("\t\tpublic void Reset() { CallCount = 0; LastCallArg = default; OnCall = null; }");
		}
		else if (inputParams.Length > 1)
		{
			sb.AppendLine("\t\tpublic void Reset() { CallCount = 0; LastCallArgs = default; OnCall = null; }");
		}
		else
		{
			sb.AppendLine("\t\tpublic void Reset() { CallCount = 0; OnCall = null; }");
		}

		sb.AppendLine("\t}");
		sb.AppendLine();
	}

	/// <summary>
	/// Generates a handler class for a generic method using the Of&lt;T&gt;() pattern.
	/// The handler has a Dictionary&lt;Type, object&gt; for type-keyed access and a nested typed handler class.
	/// </summary>
	private static void GenerateGenericMethodHandlerForInterface(
		System.Text.StringBuilder sb,
		string methodName,
		MethodOverloadInfo overload,
		string returnType,
		bool isVoid,
		string knockOffClassName,
		string koPropertyName,
		int? overloadIndex)
	{
		var handlerSuffix = overloadIndex.HasValue ? overloadIndex.Value.ToString() : "";
		var interceptClassName = $"{koPropertyName}_{methodName}{handlerSuffix}Interceptor";
		var typeParams = overload.TypeParameters.GetArray()!;
		var typeParamNames = string.Join(", ", typeParams.Select(tp => tp.Name));
		var typeParamCount = typeParams.Length;

		// Build constraint clauses for the type parameters
		var constraintClauses = GetConstraintClauses(typeParams);

		// Get non-generic parameters (parameters that don't use type parameters)
		var typeParamSet = new HashSet<string>(typeParams.Select(tp => tp.Name));
		var nonGenericParams = overload.Parameters
			.Where(p => !IsGenericParameterType(p.Type, typeParamSet))
			.ToArray();

		// Build the dictionary key type based on number of type parameters
		var keyType = typeParamCount == 1
			? "global::System.Type"
			: $"({string.Join(", ", typeParams.Select(_ => "global::System.Type"))})";

		var keyConstruction = typeParamCount == 1
			? $"typeof({typeParams[0].Name})"
			: $"({string.Join(", ", typeParams.Select(tp => $"typeof({tp.Name})"))})";

		// Start base handler class
		sb.AppendLine($"\t/// <summary>Tracks and configures behavior for {koPropertyName}.{methodName}{handlerSuffix}.</summary>");
		sb.AppendLine($"\tpublic sealed class {interceptClassName}");
		sb.AppendLine("\t{");

		// Dictionary for typed handlers
		sb.AppendLine($"\t\tprivate readonly global::System.Collections.Generic.Dictionary<{keyType}, object> _typedHandlers = new();");
		sb.AppendLine();

		// Of<T>() method
		sb.AppendLine($"\t\t/// <summary>Gets the typed handler for the specified type argument(s).</summary>");
		sb.AppendLine($"\t\tpublic {methodName}TypedHandler<{typeParamNames}> Of<{typeParamNames}>(){constraintClauses}");
		sb.AppendLine("\t\t{");
		sb.AppendLine($"\t\t\tvar key = {keyConstruction};");
		sb.AppendLine($"\t\t\tif (!_typedHandlers.TryGetValue(key, out var handler))");
		sb.AppendLine("\t\t\t{");
		sb.AppendLine($"\t\t\t\thandler = new {methodName}TypedHandler<{typeParamNames}>();");
		sb.AppendLine("\t\t\t\t_typedHandlers[key] = handler;");
		sb.AppendLine("\t\t\t}");
		sb.AppendLine($"\t\t\treturn ({methodName}TypedHandler<{typeParamNames}>)handler;");
		sb.AppendLine("\t\t}");
		sb.AppendLine();

		// Aggregate tracking properties
		sb.AppendLine("\t\t/// <summary>Total number of calls across all type arguments.</summary>");
		sb.AppendLine("\t\tpublic int TotalCallCount => _typedHandlers.Values.Cast<IGenericMethodCallTracker>().Sum(h => h.CallCount);");
		sb.AppendLine();
		sb.AppendLine("\t\t/// <summary>True if this method was called with any type argument.</summary>");
		sb.AppendLine("\t\tpublic bool WasCalled => _typedHandlers.Values.Cast<IGenericMethodCallTracker>().Any(h => h.WasCalled);");
		sb.AppendLine();
		sb.AppendLine($"\t\t/// <summary>All type argument(s) that were used in calls.</summary>");
		sb.AppendLine($"\t\tpublic global::System.Collections.Generic.IReadOnlyList<{keyType}> CalledTypeArguments => _typedHandlers.Keys.ToList();");
		sb.AppendLine();

		// Reset method
		sb.AppendLine("\t\t/// <summary>Resets all typed handlers.</summary>");
		sb.AppendLine("\t\tpublic void Reset()");
		sb.AppendLine("\t\t{");
		sb.AppendLine("\t\t\tforeach (var handler in _typedHandlers.Values.Cast<IResettable>())");
		sb.AppendLine("\t\t\t\thandler.Reset();");
		sb.AppendLine("\t\t\t_typedHandlers.Clear();");
		sb.AppendLine("\t\t}");
		sb.AppendLine();

		// --- Nested Typed Handler Class ---
		sb.AppendLine($"\t\t/// <summary>Typed handler for {methodName} with specific type arguments.</summary>");
		sb.AppendLine($"\t\tpublic sealed class {methodName}TypedHandler<{typeParamNames}> : IGenericMethodCallTracker, IResettable{constraintClauses}");
		sb.AppendLine("\t\t{");

		// Delegate type - return type may include type parameters
		var delegateName = $"{methodName}Delegate";
		var delegateParams = new List<string> { $"{knockOffClassName} ko" };
		foreach (var p in overload.Parameters)
		{
			delegateParams.Add($"{p.Type} {p.Name}");
		}
		var delegateParamList = string.Join(", ", delegateParams);

		sb.AppendLine($"\t\t\t/// <summary>Delegate for {methodName}.</summary>");
		if (isVoid)
		{
			sb.AppendLine($"\t\t\tpublic delegate void {delegateName}({delegateParamList});");
		}
		else
		{
			sb.AppendLine($"\t\t\tpublic delegate {returnType} {delegateName}({delegateParamList});");
		}
		sb.AppendLine();

		// Tracking storage - based on non-generic parameter count
		var inputParams = GetInputParameters(new EquatableArray<ParameterInfo>(nonGenericParams)).ToArray();
		if (inputParams.Length == 0)
		{
			sb.AppendLine("\t\t\t/// <summary>Number of times this method was called with these type arguments.</summary>");
			sb.AppendLine("\t\t\tpublic int CallCount { get; private set; }");
		}
		else if (inputParams.Length == 1)
		{
			var param = inputParams[0];
			var nullableType = MakeNullable(param.Type);
			sb.AppendLine("\t\t\t/// <summary>Number of times this method was called with these type arguments.</summary>");
			sb.AppendLine("\t\t\tpublic int CallCount { get; private set; }");
			sb.AppendLine();
			sb.AppendLine($"\t\t\t/// <summary>The '{param.Name}' argument from the most recent call.</summary>");
			sb.AppendLine($"\t\t\tpublic {nullableType} LastCallArg {{ get; private set; }}");
		}
		else
		{
			var tupleElements = inputParams.Select(p => $"{p.Type} {p.Name}");
			var tupleType = $"({string.Join(", ", tupleElements)})";
			sb.AppendLine("\t\t\t/// <summary>Number of times this method was called with these type arguments.</summary>");
			sb.AppendLine("\t\t\tpublic int CallCount { get; private set; }");
			sb.AppendLine();
			sb.AppendLine("\t\t\t/// <summary>The arguments from the most recent call.</summary>");
			sb.AppendLine($"\t\t\tpublic {tupleType}? LastCallArgs {{ get; private set; }}");
		}
		sb.AppendLine();

		sb.AppendLine("\t\t\t/// <summary>True if this method was called at least once with these type arguments.</summary>");
		sb.AppendLine("\t\t\tpublic bool WasCalled => CallCount > 0;");
		sb.AppendLine();

		sb.AppendLine("\t\t\t/// <summary>Callback invoked when this method is called. If set, its return value is used.</summary>");
		sb.AppendLine($"\t\t\tpublic {delegateName}? OnCall {{ get; set; }}");
		sb.AppendLine();

		// RecordCall method
		sb.AppendLine("\t\t\t/// <summary>Records a method call.</summary>");
		if (inputParams.Length == 0)
		{
			sb.AppendLine("\t\t\tpublic void RecordCall() => CallCount++;");
		}
		else if (inputParams.Length == 1)
		{
			var param = inputParams[0];
			sb.AppendLine($"\t\t\tpublic void RecordCall({param.Type} {param.Name}) {{ CallCount++; LastCallArg = {param.Name}; }}");
		}
		else
		{
			var recordCallParams = string.Join(", ", inputParams.Select(p => $"{p.Type} {p.Name}"));
			var tupleConstruction = $"({string.Join(", ", inputParams.Select(p => p.Name))})";
			sb.AppendLine($"\t\t\tpublic void RecordCall({recordCallParams}) {{ CallCount++; LastCallArgs = {tupleConstruction}; }}");
		}
		sb.AppendLine();

		// Reset method for typed handler
		sb.AppendLine("\t\t\t/// <summary>Resets all tracking state.</summary>");
		if (inputParams.Length == 0)
		{
			sb.AppendLine("\t\t\tpublic void Reset() { CallCount = 0; OnCall = null; }");
		}
		else if (inputParams.Length == 1)
		{
			sb.AppendLine("\t\t\tpublic void Reset() { CallCount = 0; LastCallArg = default; OnCall = null; }");
		}
		else
		{
			sb.AppendLine("\t\t\tpublic void Reset() { CallCount = 0; LastCallArgs = default; OnCall = null; }");
		}

		sb.AppendLine("\t\t}"); // Close typed handler class
		sb.AppendLine("\t}"); // Close base handler class
		sb.AppendLine();
	}

	/// <summary>
	/// Checks if a type string references any of the generic type parameters.
	/// </summary>
	private static bool IsGenericParameterType(string typeString, HashSet<string> typeParamNames)
	{
		// Check for exact match or if type starts with any type param name followed by common suffixes
		foreach (var tp in typeParamNames)
		{
			if (typeString == tp || typeString == tp + "?" ||
				typeString.StartsWith(tp + "[") ||
				typeString.Contains($"<{tp}>") || typeString.Contains($"<{tp},") || typeString.Contains($", {tp}>"))
			{
				return true;
			}
		}
		return false;
	}

	/// <summary>
	/// Generate handler class for an event - interface-scoped version
	/// </summary>
	private static void GenerateInterfaceEventHandlerClass(
		System.Text.StringBuilder sb,
		EventMemberInfo evt,
		string knockOffClassName,
		string koPropertyName)
	{
		var interceptClassName = $"{koPropertyName}_{evt.Name}Interceptor";

		sb.AppendLine($"\t/// <summary>Tracks and raises {koPropertyName}.{evt.Name}.</summary>");
		sb.AppendLine($"\tpublic sealed class {interceptClassName}");
		sb.AppendLine("\t{");

		var delegateTypeForField = evt.FullDelegateTypeName.TrimEnd('?');
		sb.AppendLine($"\t\tprivate {delegateTypeForField}? _handler;");

		var paramCount = evt.DelegateParameters.Count;
		string raiseTrackingType;
		string raiseTrackingConstruction;

		if (paramCount == 0)
		{
			raiseTrackingType = "";
			raiseTrackingConstruction = "";
		}
		else if (paramCount == 1)
		{
			var param = evt.DelegateParameters.GetArray()![0];
			raiseTrackingType = param.Type;
			raiseTrackingConstruction = param.Name;
		}
		else
		{
			raiseTrackingType = GetTupleType(evt.DelegateParameters);
			raiseTrackingConstruction = GetTupleConstruction(evt.DelegateParameters);
		}

		if (paramCount > 0)
		{
			sb.AppendLine($"\t\tprivate readonly global::System.Collections.Generic.List<{raiseTrackingType}> _raises = new();");
		}
		sb.AppendLine();

		sb.AppendLine("\t\t/// <summary>Number of times handlers were added.</summary>");
		sb.AppendLine("\t\tpublic int SubscribeCount { get; private set; }");
		sb.AppendLine();
		sb.AppendLine("\t\t/// <summary>Number of times handlers were removed.</summary>");
		sb.AppendLine("\t\tpublic int UnsubscribeCount { get; private set; }");
		sb.AppendLine();
		sb.AppendLine("\t\t/// <summary>True if at least one handler is subscribed.</summary>");
		sb.AppendLine("\t\tpublic bool HasSubscribers => _handler != null;");
		sb.AppendLine();

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

		GenerateEventRaiseMethods(sb, evt, raiseTrackingConstruction);

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
	/// Generate backing property for an interface property (interface-scoped version)
	/// </summary>
	private static void GenerateInterfaceBackingProperty(
		System.Text.StringBuilder sb,
		InterfaceMemberInfo prop,
		string koPropertyName)
	{
		var backingName = $"{koPropertyName}_{prop.Name}Backing";

		// Initialize based on default strategy
		// For backing properties, we must provide a value even if the type can't be new()'d
		var typeToNew = prop.ConcreteTypeForNew ?? prop.ReturnType;
		var initializer = prop.DefaultStrategy switch
		{
			DefaultValueStrategy.NewInstance => $" = new {typeToNew}();",
			DefaultValueStrategy.Default => "", // Value types/nullable get default automatically
			DefaultValueStrategy.ThrowException => GetBackingPropertyInitializer(prop.ReturnType),
			_ => ""
		};

		sb.AppendLine($"\t/// <summary>Backing field for {koPropertyName}.{prop.Name}.</summary>");
		sb.AppendLine($"\tprotected {prop.ReturnType} {backingName} {{ get; set; }}{initializer}");
		sb.AppendLine();
	}

	/// <summary>
	/// Generate backing dictionary for an indexer property (interface-scoped version)
	/// </summary>
	private static void GenerateInterfaceIndexerBackingDictionary(
		System.Text.StringBuilder sb,
		InterfaceMemberInfo indexer,
		string koPropertyName)
	{
		var backingName = $"{koPropertyName}_{indexer.Name}Backing";
		var keyType = indexer.IndexerParameters.Count > 0
			? indexer.IndexerParameters.GetArray()![0].Type
			: "object";

		sb.AppendLine($"\t/// <summary>Backing dictionary for {koPropertyName}.{indexer.Name}. Pre-populate with values or use OnGet callback.</summary>");
		sb.AppendLine($"\tpublic global::System.Collections.Generic.Dictionary<{keyType}, {indexer.ReturnType}> {backingName} {{ get; }} = new();");
		sb.AppendLine();
	}

	/// <summary>
	/// Generate explicit interface implementation for a property (interface-scoped version)
	/// </summary>
	private static void GenerateInterfacePropertyImplementation(
		System.Text.StringBuilder sb,
		string interfaceName,
		InterfaceMemberInfo prop,
		string koPropertyName)
	{
		var backingName = $"{koPropertyName}_{prop.Name}Backing";

		sb.AppendLine($"\t{prop.ReturnType} {interfaceName}.{prop.Name}");
		sb.AppendLine("\t{");

		if (prop.HasGetter)
		{
			sb.AppendLine("\t\tget");
			sb.AppendLine("\t\t{");
			sb.AppendLine($"\t\t\t{koPropertyName}.{prop.Name}.RecordGet();");
			sb.AppendLine($"\t\t\tif ({koPropertyName}.{prop.Name}.OnGet is {{ }} onGetCallback)");
			sb.AppendLine($"\t\t\t\treturn onGetCallback(this);");
			sb.AppendLine($"\t\t\treturn {backingName};");
			sb.AppendLine("\t\t}");
		}

		if (prop.HasSetter)
		{
			var pragmaDisable = GetSetterNullabilityAttribute(prop);
			var pragmaRestore = GetSetterNullabilityRestore(prop);
			if (!string.IsNullOrEmpty(pragmaDisable))
				sb.Append(pragmaDisable);
			sb.AppendLine("\t\tset");
			sb.AppendLine("\t\t{");
			sb.AppendLine($"\t\t\t{koPropertyName}.{prop.Name}.RecordSet(value);");
			sb.AppendLine($"\t\t\tif ({koPropertyName}.{prop.Name}.OnSet is {{ }} onSetCallback)");
			sb.AppendLine($"\t\t\t\tonSetCallback(this, value);");
			sb.AppendLine($"\t\t\telse");
			sb.AppendLine($"\t\t\t\t{backingName} = value;");
			sb.AppendLine("\t\t}");
			if (!string.IsNullOrEmpty(pragmaRestore))
				sb.AppendLine(pragmaRestore);
		}

		sb.AppendLine("\t}");
		sb.AppendLine();
	}

	/// <summary>
	/// Generate explicit interface implementation for an indexer (interface-scoped version)
	/// </summary>
	private static void GenerateInterfaceIndexerImplementation(
		System.Text.StringBuilder sb,
		string interfaceName,
		InterfaceMemberInfo indexer,
		string koPropertyName)
	{
		var backingName = $"{koPropertyName}_{indexer.Name}Backing";
		var keyType = indexer.IndexerParameters.Count > 0
			? indexer.IndexerParameters.GetArray()![0].Type
			: "object";
		var keyParamName = indexer.IndexerParameters.Count > 0
			? indexer.IndexerParameters.GetArray()![0].Name
			: "key";

		sb.AppendLine($"\t{indexer.ReturnType} {interfaceName}.this[{keyType} {keyParamName}]");
		sb.AppendLine("\t{");

		if (indexer.HasGetter)
		{
			sb.AppendLine("\t\tget");
			sb.AppendLine("\t\t{");
			sb.AppendLine($"\t\t\t{koPropertyName}.{indexer.Name}.RecordGet({keyParamName});");
			sb.AppendLine($"\t\t\tif ({koPropertyName}.{indexer.Name}.OnGet is {{ }} onGetCallback)");
			sb.AppendLine($"\t\t\t\treturn onGetCallback(this, {keyParamName});");
			sb.AppendLine($"\t\t\tif ({backingName}.TryGetValue({keyParamName}, out var value))");
			sb.AppendLine($"\t\t\t\treturn value;");
			if (indexer.DefaultStrategy == DefaultValueStrategy.ThrowException)
			{
				sb.AppendLine($"\t\t\tthrow new global::System.Collections.Generic.KeyNotFoundException($\"Key '{{{keyParamName}}}' not found. Set {koPropertyName}.{indexer.Name}.OnGet or add to {backingName} dictionary.\");");
			}
			else
			{
				sb.AppendLine($"\t\t\t{GenerateDefaultReturn(indexer.ReturnType, indexer.DefaultStrategy, indexer.ConcreteTypeForNew)}");
			}
			sb.AppendLine("\t\t}");
		}

		if (indexer.HasSetter)
		{
			sb.AppendLine("\t\tset");
			sb.AppendLine("\t\t{");
			sb.AppendLine($"\t\t\t{koPropertyName}.{indexer.Name}.RecordSet({keyParamName}, value);");
			sb.AppendLine($"\t\t\tif ({koPropertyName}.{indexer.Name}.OnSet is {{ }} onSetCallback)");
			sb.AppendLine($"\t\t\t\tonSetCallback(this, {keyParamName}, value);");
			sb.AppendLine($"\t\t\telse");
			sb.AppendLine($"\t\t\t\t{backingName}[{keyParamName}] = value;");
			sb.AppendLine("\t\t}");
		}

		sb.AppendLine("\t}");
		sb.AppendLine();
	}

	/// <summary>
	/// Generate method implementation (interface-scoped version)
	/// </summary>
	private static void GenerateInterfaceMethod(
		System.Text.StringBuilder sb,
		string interfaceName,
		InterfaceMemberInfo method,
		KnockOffTypeInfo typeInfo,
		MethodGroupInfo group,
		string koPropertyName)
	{
		// Generic methods need special handling with Of<T>() pattern
		if (method.IsGenericMethod)
		{
			GenerateGenericInterfaceMethod(sb, interfaceName, method, typeInfo, group, koPropertyName);
			return;
		}

		var paramList = string.Join(", ", method.Parameters.Select(p => FormatParameter(p)));
		var argList = string.Join(", ", method.Parameters.Select(p => FormatArgument(p)));
		var paramCount = method.Parameters.Count;

		var inputParams = GetInputParameters(method.Parameters).ToArray();
		var inputArgList = string.Join(", ", inputParams.Select(p => p.Name));
		var inputParamCount = inputParams.Length;

		var outParams = method.Parameters.Where(p => IsOutputParameter(p.RefKind)).ToArray();

		var hasUserMethod = typeInfo.UserMethods.Any(um =>
			um.Name == method.Name &&
			um.ReturnType == method.ReturnType &&
			um.Parameters.Count == method.Parameters.Count &&
			um.Parameters.Zip(method.Parameters, (a, b) => a.Type == b.Type).All(x => x));

		var isVoid = method.ReturnType == "void";
		var isTask = method.ReturnType == "global::System.Threading.Tasks.Task";
		var isValueTask = method.ReturnType == "global::System.Threading.Tasks.ValueTask";
		var isTaskOfT = method.ReturnType.StartsWith("global::System.Threading.Tasks.Task<");
		var isValueTaskOfT = method.ReturnType.StartsWith("global::System.Threading.Tasks.ValueTask<");

		// Find the overload index
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

		foreach (var outParam in outParams)
		{
			sb.AppendLine($"\t\t{outParam.Name} = default!;");
		}

		// For overloaded methods, use Method1, Method2, etc. (1-based). For single methods, use Method
		var hasOverloads = group.Overloads.Count > 1;
		var handlerName = hasOverloads ? $"{method.Name}{overloadIndex + 1}" : method.Name;

		if (inputParamCount > 0)
		{
			sb.AppendLine($"\t\t{koPropertyName}.{handlerName}.RecordCall({inputArgList});");
		}
		else
		{
			sb.AppendLine($"\t\t{koPropertyName}.{handlerName}.RecordCall();");
		}

		sb.AppendLine($"\t\tif ({koPropertyName}.{handlerName}.OnCall is {{ }} onCallCallback)");
		if (isVoid)
		{
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
			if (paramCount == 0)
			{
				sb.AppendLine($"\t\t\treturn onCallCallback(this);");
			}
			else
			{
				sb.AppendLine($"\t\t\treturn onCallCallback(this, {argList});");
			}
		}

		if (hasUserMethod)
		{
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
			sb.AppendLine($"\t\treturn global::System.Threading.Tasks.Task.CompletedTask;");
		}
		else if (isValueTask)
		{
			sb.AppendLine($"\t\treturn default;");
		}
		else if (isTaskOfT)
		{
			var innerType = ExtractGenericArg(method.ReturnType);
			if (method.DefaultStrategy == DefaultValueStrategy.ThrowException)
			{
				sb.AppendLine($"\t\tthrow new global::System.InvalidOperationException(\"No implementation provided for non-nullable return type. Define a protected method '{method.Name}' in your partial class, or set {koPropertyName}.{handlerName}.OnCall.\");");
			}
			else
			{
				sb.AppendLine($"\t\t{GenerateTaskOfTReturn(innerType, method.DefaultStrategy, method.ConcreteTypeForNew)}");
			}
		}
		else if (isValueTaskOfT)
		{
			var innerType = ExtractGenericArg(method.ReturnType);
			if (method.DefaultStrategy == DefaultValueStrategy.ThrowException)
			{
				sb.AppendLine($"\t\tthrow new global::System.InvalidOperationException(\"No implementation provided for non-nullable return type. Define a protected method '{method.Name}' in your partial class, or set {koPropertyName}.{handlerName}.OnCall.\");");
			}
			else
			{
				sb.AppendLine($"\t\t{GenerateValueTaskOfTReturn(innerType, method.DefaultStrategy, method.ConcreteTypeForNew)}");
			}
		}
		else if (method.DefaultStrategy == DefaultValueStrategy.ThrowException)
		{
			sb.AppendLine($"\t\tthrow new global::System.InvalidOperationException(\"No implementation provided for non-nullable return type. Define a protected method '{method.Name}' in your partial class, or set {koPropertyName}.{handlerName}.OnCall.\");");
		}
		else
		{
			sb.AppendLine($"\t\t{GenerateDefaultReturn(method.ReturnType, method.DefaultStrategy, method.ConcreteTypeForNew)}");
		}

		sb.AppendLine("\t}");
		sb.AppendLine();
	}

	/// <summary>
	/// Generate explicit interface implementation for a generic method.
	/// Uses the Of&lt;T&gt;() pattern to access typed handlers.
	/// </summary>
	private static void GenerateGenericInterfaceMethod(
		System.Text.StringBuilder sb,
		string interfaceName,
		InterfaceMemberInfo method,
		KnockOffTypeInfo typeInfo,
		MethodGroupInfo group,
		string koPropertyName)
	{
		var typeParams = method.TypeParameters.GetArray()!;
		var typeParamNames = string.Join(", ", typeParams.Select(tp => tp.Name));

		// For explicit interface implementations, only class/struct constraints are allowed (CS0460)
		// Pass return type to determine if class constraint is needed for nullable returns
		var constraintClauses = GetConstraintsForExplicitImpl(typeParams, method.ReturnType);

		var paramList = string.Join(", ", method.Parameters.Select(p => FormatParameter(p)));
		var argList = string.Join(", ", method.Parameters.Select(p => FormatArgument(p)));
		var paramCount = method.Parameters.Count;

		// Get non-generic parameters for RecordCall
		var typeParamSet = new HashSet<string>(typeParams.Select(tp => tp.Name));
		var nonGenericParams = method.Parameters
			.Where(p => !IsGenericParameterType(p.Type, typeParamSet))
			.ToArray();
		var nonGenericArgList = string.Join(", ", nonGenericParams.Select(p => p.Name));

		var isVoid = method.ReturnType == "void";
		var isTask = method.ReturnType == "global::System.Threading.Tasks.Task";
		var isValueTask = method.ReturnType == "global::System.Threading.Tasks.ValueTask";

		// Find the overload index
		var overloadIndex = -1;
		for (int i = 0; i < group.Overloads.Count; i++)
		{
			var overload = group.Overloads.GetArray()![i];
			if (overload.Parameters.Count == method.Parameters.Count && overload.IsGenericMethod)
			{
				// For generic methods, match by parameter count and type param count
				if (overload.TypeParameters.Count == method.TypeParameters.Count)
				{
					overloadIndex = i;
					break;
				}
			}
		}

		// For overloaded methods, use Method1, Method2, etc. (1-based). For single methods, use Method
		var hasOverloads = group.Overloads.Count > 1;
		var handlerName = hasOverloads ? $"{method.Name}{overloadIndex + 1}" : method.Name;

		// Generate method signature with type parameters (only class/struct constraints allowed in explicit impl)
		sb.AppendLine($"\t{method.ReturnType} {interfaceName}.{method.Name}<{typeParamNames}>({paramList}){constraintClauses}");
		sb.AppendLine("\t{");

		// Get the typed handler via Of<T>()
		sb.AppendLine($"\t\tvar typedHandler = {koPropertyName}.{handlerName}.Of<{typeParamNames}>();");

		// Record the call
		if (nonGenericParams.Length > 0)
		{
			sb.AppendLine($"\t\ttypedHandler.RecordCall({nonGenericArgList});");
		}
		else
		{
			sb.AppendLine($"\t\ttypedHandler.RecordCall();");
		}

		// Check for OnCall callback
		sb.AppendLine($"\t\tif (typedHandler.OnCall is {{ }} onCallCallback)");
		if (isVoid)
		{
			// Void methods - call callback and return without value
			if (paramCount == 0)
			{
				sb.AppendLine($"\t\t{{ onCallCallback(this); return; }}");
			}
			else
			{
				sb.AppendLine($"\t\t{{ onCallCallback(this, {argList}); return; }}");
			}
		}
		else if (isTask || isValueTask)
		{
			// Task/ValueTask methods - return the callback result
			if (paramCount == 0)
			{
				sb.AppendLine($"\t\t{{ return onCallCallback(this); }}");
			}
			else
			{
				sb.AppendLine($"\t\t{{ return onCallCallback(this, {argList}); }}");
			}
		}
		else
		{
			if (paramCount == 0)
			{
				sb.AppendLine($"\t\t\treturn onCallCallback(this);");
			}
			else
			{
				sb.AppendLine($"\t\t\treturn onCallCallback(this, {argList});");
			}
		}

		// Default behavior
		if (isVoid)
		{
			// void - no return needed
		}
		else if (isTask)
		{
			sb.AppendLine($"\t\treturn global::System.Threading.Tasks.Task.CompletedTask;");
		}
		else if (isValueTask)
		{
			sb.AppendLine($"\t\treturn default;");
		}
		else if (method.IsNullable)
		{
			sb.AppendLine($"\t\treturn default!;");
		}
		else
		{
			// Non-nullable return - use SmartDefault helper for runtime evaluation
			sb.AppendLine($"\t\treturn SmartDefault<{method.ReturnType}>(\"{method.Name}\");");
		}

		sb.AppendLine("\t}");
		sb.AppendLine();
	}

	/// <summary>
	/// Generate explicit interface implementation for an event (interface-scoped version)
	/// </summary>
	private static void GenerateInterfaceEventImplementation(
		System.Text.StringBuilder sb,
		string interfaceName,
		EventMemberInfo evt,
		string koPropertyName)
	{
		sb.AppendLine($"\tevent {evt.FullDelegateTypeName} {interfaceName}.{evt.Name}");
		sb.AppendLine("\t{");
		sb.AppendLine($"\t\tadd => {koPropertyName}.{evt.Name}.Add(value);");
		sb.AppendLine($"\t\tremove => {koPropertyName}.{evt.Name}.Remove(value);");
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

	/// <summary>
	/// Generates a return statement based on the DefaultValueStrategy.
	/// </summary>
	private static string GenerateDefaultReturn(string returnType, DefaultValueStrategy strategy, string? concreteType = null)
	{
		var typeToNew = concreteType ?? returnType;
		return strategy switch
		{
			DefaultValueStrategy.Default => "return default!;",
			DefaultValueStrategy.NewInstance => $"return new {typeToNew}();",
			DefaultValueStrategy.ThrowException => throw new InvalidOperationException("ThrowException strategy should be handled separately"),
			_ => "return default!;"
		};
	}

	/// <summary>
	/// Generates a return statement for Task&lt;T&gt; based on the DefaultValueStrategy.
	/// </summary>
	private static string GenerateTaskOfTReturn(string innerType, DefaultValueStrategy strategy, string? concreteType = null)
	{
		var typeToNew = concreteType ?? innerType;
		return strategy switch
		{
			DefaultValueStrategy.Default => $"return global::System.Threading.Tasks.Task.FromResult<{innerType}>(default!);",
			DefaultValueStrategy.NewInstance => $"return global::System.Threading.Tasks.Task.FromResult<{innerType}>(new {typeToNew}());",
			DefaultValueStrategy.ThrowException => throw new InvalidOperationException("ThrowException strategy should be handled separately"),
			_ => $"return global::System.Threading.Tasks.Task.FromResult<{innerType}>(default!);"
		};
	}

	/// <summary>
	/// Generates a return statement for ValueTask&lt;T&gt; based on the DefaultValueStrategy.
	/// </summary>
	private static string GenerateValueTaskOfTReturn(string innerType, DefaultValueStrategy strategy, string? concreteType = null)
	{
		var typeToNew = concreteType ?? innerType;
		return strategy switch
		{
			DefaultValueStrategy.Default => "return default;",
			DefaultValueStrategy.NewInstance => $"return new global::System.Threading.Tasks.ValueTask<{innerType}>(new {typeToNew}());",
			DefaultValueStrategy.ThrowException => throw new InvalidOperationException("ThrowException strategy should be handled separately"),
			_ => "return default;"
		};
	}

	/// <summary>
	/// Gets an initializer for backing properties when the type can't be new()'d.
	/// Backing properties MUST have a value, so we provide sensible defaults.
	/// </summary>
	private static string GetBackingPropertyInitializer(string typeName)
	{
		// Handle string - use empty string
		if (typeName == "global::System.String" || typeName == "string")
			return " = \"\";";

		// Handle arrays - use Array.Empty<T>() or empty array
		if (typeName.EndsWith("[]"))
		{
			var elementType = typeName.Substring(0, typeName.Length - 2);
			return $" = global::System.Array.Empty<{elementType}>();";
		}

		// Handle collection interfaces - use Array.Empty<T>()
		if (typeName.Contains("IEnumerable<") || typeName.Contains("IReadOnlyCollection<") || typeName.Contains("IReadOnlyList<"))
		{
			var elementType = ExtractGenericArg(typeName);
			return $" = global::System.Array.Empty<{elementType}>();";
		}

		// Fallback: suppress nullable warning (property exists but user must set it)
		return " = default!;";
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

	/// <summary>
	/// Escapes C# reserved keywords by prefixing with @.
	/// </summary>
	private static string EscapeIdentifier(string name)
	{
		// Common C# keywords that might appear as parameter names
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
	/// Gets a default value initializer for a property member.
	/// </summary>
	private static string GetDefaultValueForProperty(InterfaceMemberInfo member)
	{
		// Use the member's DefaultStrategy to determine proper initialization
		var typeToNew = member.ConcreteTypeForNew ?? member.ReturnType;
		return member.DefaultStrategy switch
		{
			DefaultValueStrategy.NewInstance => $" = new {typeToNew}();",
			DefaultValueStrategy.Default => " = default!;", // Value types/nullable get default
			DefaultValueStrategy.ThrowException => GetBackingPropertyInitializer(member.ReturnType),
			_ => " = default!;"
		};
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
				// Use strategy to determine inner value
				// For Default strategy, use `default` which creates a completed ValueTask with default(T)
				// For NewInstance, create the ValueTask with a new instance
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
				// Use strategy to determine inner value
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


}
