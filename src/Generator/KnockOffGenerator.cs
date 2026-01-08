using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace KnockOff;

[Generator(LanguageNames.CSharp)]
public class KnockOffGenerator : IIncrementalGenerator
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

		// Must not be generic (Phase 1 limitation)
		if (classDecl.TypeParameterList?.Parameters.Count > 0)
			return false;

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
	/// Transform: extract interface types from ALL generic attribute arguments on this class.
	/// ForAttributeWithMetadataName triggers once per node, with context.Attributes containing
	/// ALL matching attributes on that node.
	/// </summary>
	private static InlineStubClassInfo? TransformInlineStubClass(GeneratorAttributeSyntaxContext context)
	{
		var classDeclaration = (ClassDeclarationSyntax)context.TargetNode;
		var classSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclaration);

		if (classSymbol is null)
			return null;

		// Get namespace
		var ns = classSymbol.ContainingNamespace;
		var namespaceName = ns.IsGlobalNamespace ? "" : ns.ToDisplayString();

		// Get containing types chain (for nested class support)
		var containingTypes = GetContainingTypes(classSymbol);

		// Collect diagnostics
		var diagnostics = new List<DiagnosticInfo>();
		var filePath = classDeclaration.SyntaxTree.FilePath;

		// Check for existing "Stubs" nested type (KO1003)
		var existingStubsType = classSymbol.GetTypeMembers("Stubs").FirstOrDefault();
		if (existingStubsType is not null)
		{
			var location = classDeclaration.Identifier.GetLocation();
			var lineSpan = location.GetLineSpan();
			diagnostics.Add(new DiagnosticInfo(
				"KO1003",
				filePath,
				lineSpan.StartLinePosition.Line,
				lineSpan.StartLinePosition.Character,
				Array.Empty<string>()));
		}

		// Track names for collision detection (KO1002)
		var nameToFullNames = new Dictionary<string, List<string>>();

		// Process ALL [KnockOff<T>] attributes on this class
		var interfaces = new List<InterfaceInfo>();
		var delegates = new List<DelegateInfo>();
		var classes = new List<ClassStubInfo>();
		var stubTypeNames = new HashSet<string>();
		foreach (var attributeData in context.Attributes)
		{
			if (attributeData.AttributeClass is not INamedTypeSymbol attrType || !attrType.IsGenericType)
				continue;

			var typeArg = attrType.TypeArguments.FirstOrDefault();
			if (typeArg is null)
				continue;

			// Get attribute location for diagnostics
			var attrLocation = attributeData.ApplicationSyntaxReference?.GetSyntax()?.GetLocation();
			var attrLineSpan = attrLocation?.GetLineSpan() ?? default;

			// Check if type argument is an interface, class, or delegate (KO1001)
			if (typeArg.TypeKind != TypeKind.Interface && typeArg.TypeKind != TypeKind.Delegate && typeArg.TypeKind != TypeKind.Class)
			{
				diagnostics.Add(new DiagnosticInfo(
					"KO1001",
					filePath,
					attrLineSpan.StartLinePosition.Line,
					attrLineSpan.StartLinePosition.Character,
					new[] { typeArg.ToDisplayString() }));
				continue;
			}

			// Track name for collision detection
			var simpleName = typeArg.Name;
			var fullName = typeArg.ToDisplayString();
			if (!nameToFullNames.TryGetValue(simpleName, out var fullNames))
			{
				fullNames = new List<string>();
				nameToFullNames[simpleName] = fullNames;
			}
			if (!fullNames.Contains(fullName))
				fullNames.Add(fullName);

			// Get interface info
			if (typeArg.TypeKind == TypeKind.Interface && typeArg is INamedTypeSymbol namedInterface)
			{
				var interfaceInfo = ExtractInterfaceInfo(namedInterface, classSymbol.ContainingAssembly);
				interfaces.Add(interfaceInfo);
				stubTypeNames.Add(namedInterface.Name); // e.g., "IUserService"
			}
			// Get delegate info
			else if (typeArg.TypeKind == TypeKind.Delegate && typeArg is INamedTypeSymbol namedDelegate)
			{
				var delegateInfo = ExtractDelegateInfo(namedDelegate);
				if (delegateInfo is not null)
				{
					delegates.Add(delegateInfo);
					stubTypeNames.Add(namedDelegate.Name); // e.g., "IsUniqueRule"
				}
			}
			// Get class info for class stubbing via inheritance
			else if (typeArg.TypeKind == TypeKind.Class && typeArg is INamedTypeSymbol namedClass)
			{
				var classInfo = ExtractClassInfo(namedClass, classSymbol.ContainingAssembly, filePath, attrLineSpan, diagnostics);
				if (classInfo is not null)
				{
					classes.Add(classInfo);
					stubTypeNames.Add(namedClass.Name); // e.g., "UserService"
				}
			}
		}

		// Check for name collisions (KO1002) - same simple name from different namespaces
		foreach (var kvp in nameToFullNames)
		{
			if (kvp.Value.Count > 1)
			{
				var location = classDeclaration.Identifier.GetLocation();
				var lineSpan = location.GetLineSpan();
				diagnostics.Add(new DiagnosticInfo(
					"KO1002",
					filePath,
					lineSpan.StartLinePosition.Line,
					lineSpan.StartLinePosition.Character,
					new[] { kvp.Key }));
			}
		}

		// If there are blocking diagnostics (KO1002, KO1003), still return to report them
		// but code generation will be skipped

		if (interfaces.Count == 0 && delegates.Count == 0 && classes.Count == 0 && diagnostics.Count == 0)
			return null;

		// Detect partial properties that return Stubs.{TypeName}
		var partialProperties = DetectPartialProperties(classDeclaration, stubTypeNames);

		return new InlineStubClassInfo(
			Namespace: namespaceName,
			ClassName: classSymbol.Name,
			ContainingTypes: containingTypes,
			Interfaces: new EquatableArray<InterfaceInfo>(interfaces.ToArray()),
			Delegates: new EquatableArray<DelegateInfo>(delegates.ToArray()),
			Classes: new EquatableArray<ClassStubInfo>(classes.ToArray()),
			PartialProperties: new EquatableArray<PartialPropertyInfo>(partialProperties.ToArray()),
			Diagnostics: new EquatableArray<DiagnosticInfo>(diagnostics.ToArray()));
	}

	/// <summary>
	/// Detects partial properties in the class declaration that return Stubs.{InterfaceName}.
	/// </summary>
	private static List<PartialPropertyInfo> DetectPartialProperties(
		ClassDeclarationSyntax classDeclaration,
		HashSet<string> interfaceNames)
	{
		var partialProperties = new List<PartialPropertyInfo>();

		foreach (var member in classDeclaration.Members)
		{
			if (member is not PropertyDeclarationSyntax property)
				continue;

			// Check if the property has the partial modifier
			if (!property.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
				continue;

			// Check if property type is Stubs.{SomeName}
			var typeText = property.Type.ToString();
			if (!typeText.StartsWith("Stubs."))
				continue;

			var stubTypeName = typeText.Substring(6); // Remove "Stubs." prefix
			if (!interfaceNames.Contains(stubTypeName))
				continue;

			// Property matches - extract info
			var accessModifier = GetAccessModifier(property.Modifiers);
			var hasGetter = property.AccessorList?.Accessors.Any(a => a.IsKind(SyntaxKind.GetAccessorDeclaration)) ?? false;
			var hasSetter = property.AccessorList?.Accessors.Any(a => a.IsKind(SyntaxKind.SetAccessorDeclaration)) ?? false;

			// If no accessor list but has expression body, it's a getter
			if (property.ExpressionBody != null)
			{
				hasGetter = true;
			}

			partialProperties.Add(new PartialPropertyInfo(
				PropertyName: property.Identifier.Text,
				StubTypeName: stubTypeName,
				AccessModifier: accessModifier,
				HasGetter: hasGetter,
				HasSetter: hasSetter));
		}

		return partialProperties;
	}

	/// <summary>
	/// Extracts access modifier from property modifiers.
	/// </summary>
	private static string GetAccessModifier(SyntaxTokenList modifiers)
	{
		var accessParts = new List<string>();
		foreach (var mod in modifiers)
		{
			if (mod.IsKind(SyntaxKind.PublicKeyword))
				accessParts.Add("public");
			else if (mod.IsKind(SyntaxKind.ProtectedKeyword))
				accessParts.Add("protected");
			else if (mod.IsKind(SyntaxKind.PrivateKeyword))
				accessParts.Add("private");
			else if (mod.IsKind(SyntaxKind.InternalKeyword))
				accessParts.Add("internal");
		}
		return string.Join(" ", accessParts);
	}

	/// <summary>
	/// Extracts interface info for inline stubs (reuses same InterfaceInfo as explicit pattern).
	/// </summary>
	private static InterfaceInfo ExtractInterfaceInfo(INamedTypeSymbol iface, IAssemblySymbol knockOffAssembly)
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

		// Also get inherited interface members
		foreach (var baseInterface in iface.AllInterfaces)
		{
			foreach (var member in baseInterface.GetMembers())
			{
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
		}

		var simpleName = GetSimpleInterfaceName(iface.Name);

		return new InterfaceInfo(
			iface.ToDisplayString(),
			iface.Name,
			simpleName,
			new EquatableArray<InterfaceMemberInfo>(members.ToArray()),
			new EquatableArray<EventMemberInfo>(events.ToArray()));
	}

	/// <summary>
	/// Extracts delegate info for inline delegate stubs.
	/// </summary>
	private static DelegateInfo? ExtractDelegateInfo(INamedTypeSymbol delegateType)
	{
		// Get the Invoke method - all delegates have one
		var invokeMethod = delegateType.DelegateInvokeMethod;
		if (invokeMethod is null)
			return null;

		// Extract return type
		var returnType = invokeMethod.ReturnType.ToDisplayString();
		var isVoid = invokeMethod.ReturnsVoid;

		// Extract parameters
		var parameters = new List<ParameterInfo>();
		foreach (var param in invokeMethod.Parameters)
		{
			parameters.Add(new ParameterInfo(
				param.Name,
				param.Type.ToDisplayString(),
				param.RefKind));
		}

		return new DelegateInfo(
			FullName: delegateType.ToDisplayString(),
			Name: delegateType.Name,
			ReturnType: returnType,
			IsVoid: isVoid,
			Parameters: new EquatableArray<ParameterInfo>(parameters.ToArray()));
	}

	/// <summary>
	/// Extracts class info for class stubbing via inheritance.
	/// Returns null if the class cannot be stubbed (sealed, static, built-in, etc.).
	/// </summary>
	private static ClassStubInfo? ExtractClassInfo(
		INamedTypeSymbol classType,
		IAssemblySymbol knockOffAssembly,
		string filePath,
		Microsoft.CodeAnalysis.FileLinePositionSpan attrLineSpan,
		List<DiagnosticInfo> diagnostics)
	{
		var className = classType.Name;
		var classFullName = classType.ToDisplayString();

		// KO2005: Cannot stub static class
		if (classType.IsStatic)
		{
			diagnostics.Add(new DiagnosticInfo(
				"KO2005",
				filePath,
				attrLineSpan.StartLinePosition.Line,
				attrLineSpan.StartLinePosition.Character,
				new[] { classFullName }));
			return null;
		}

		// KO2001: Cannot stub sealed class
		if (classType.IsSealed)
		{
			diagnostics.Add(new DiagnosticInfo(
				"KO2001",
				filePath,
				attrLineSpan.StartLinePosition.Line,
				attrLineSpan.StartLinePosition.Character,
				new[] { classFullName }));
			return null;
		}

		// KO2006: Cannot stub built-in types (string, object, ValueType, Enum, Delegate, Array)
		if (IsBuiltInType(classType))
		{
			diagnostics.Add(new DiagnosticInfo(
				"KO2006",
				filePath,
				attrLineSpan.StartLinePosition.Line,
				attrLineSpan.StartLinePosition.Character,
				new[] { classFullName }));
			return null;
		}

		// KO2002: Must have accessible constructors
		var accessibleConstructors = classType.InstanceConstructors
			.Where(c => c.DeclaredAccessibility == Accessibility.Public ||
						c.DeclaredAccessibility == Accessibility.Protected ||
						c.DeclaredAccessibility == Accessibility.ProtectedOrInternal ||
						(c.DeclaredAccessibility == Accessibility.Internal &&
						 SymbolEqualityComparer.Default.Equals(c.ContainingAssembly, knockOffAssembly)))
			.ToList();

		if (accessibleConstructors.Count == 0)
		{
			diagnostics.Add(new DiagnosticInfo(
				"KO2002",
				filePath,
				attrLineSpan.StartLinePosition.Line,
				attrLineSpan.StartLinePosition.Character,
				new[] { classFullName }));
			return null;
		}

		// Extract constructors
		var constructors = new List<ClassConstructorInfo>();
		foreach (var ctor in accessibleConstructors)
		{
			var ctorParams = new List<ParameterInfo>();
			foreach (var param in ctor.Parameters)
			{
				ctorParams.Add(new ParameterInfo(
					param.Name,
					param.Type.ToDisplayString(FullyQualifiedWithNullability),
					param.RefKind));
			}

			var accessModifier = ctor.DeclaredAccessibility switch
			{
				Accessibility.Public => "public",
				Accessibility.Protected => "protected",
				Accessibility.ProtectedOrInternal => "protected internal",
				Accessibility.Internal => "internal",
				_ => "protected"
			};

			constructors.Add(new ClassConstructorInfo(
				new EquatableArray<ParameterInfo>(ctorParams.ToArray()),
				accessModifier));
		}

		// Extract virtual/abstract members (properties, methods, indexers)
		var members = new List<ClassMemberInfo>();
		var events = new List<EventMemberInfo>();

		// Get all members including inherited ones
		foreach (var member in GetAllVirtualMembers(classType))
		{
			// Skip internal members from external assemblies
			if (!IsMemberAccessible(member, knockOffAssembly))
				continue;

			if (member is IPropertySymbol property)
			{
				// Only include virtual/abstract/override properties that aren't sealed
				if ((property.IsVirtual || property.IsAbstract || property.IsOverride) && !property.IsSealed)
				{
					members.Add(CreateClassPropertyInfo(property));
				}
			}
			else if (member is IMethodSymbol method && method.MethodKind == MethodKind.Ordinary)
			{
				// Only include virtual/abstract/override methods that aren't sealed
				if ((method.IsVirtual || method.IsAbstract || method.IsOverride) && !method.IsSealed)
				{
					members.Add(CreateClassMethodInfo(method));
				}
			}
			else if (member is IEventSymbol eventSymbol)
			{
				// Only include virtual/abstract/override events that aren't sealed
				if ((eventSymbol.IsVirtual || eventSymbol.IsAbstract || eventSymbol.IsOverride) && !eventSymbol.IsSealed)
				{
					events.Add(CreateEventInfo(eventSymbol));
				}
			}
		}

		// KO2004: Warning if no virtual/abstract members to intercept
		if (members.Count == 0 && events.Count == 0)
		{
			diagnostics.Add(new DiagnosticInfo(
				"KO2004",
				filePath,
				attrLineSpan.StartLinePosition.Line,
				attrLineSpan.StartLinePosition.Character,
				new[] { classFullName }));
			// Continue - this is just a warning, we still generate the stub
		}

		return new ClassStubInfo(
			classFullName,
			className,
			new EquatableArray<ClassMemberInfo>(members.ToArray()),
			new EquatableArray<ClassConstructorInfo>(constructors.ToArray()),
			new EquatableArray<EventMemberInfo>(events.ToArray()));
	}

	/// <summary>
	/// Checks if a type is a built-in type that cannot be stubbed.
	/// </summary>
	private static bool IsBuiltInType(INamedTypeSymbol type)
	{
		var ns = type.ContainingNamespace?.ToDisplayString();
		if (ns != "System")
			return false;

		// String, Object, Array, Delegate, MulticastDelegate, ValueType, Enum
		return type.Name is "String" or "Object" or "Array" or "Delegate" or "MulticastDelegate" or "ValueType" or "Enum";
	}

	/// <summary>
	/// Gets all virtual/abstract/override members from a class and its base classes.
	/// </summary>
	private static IEnumerable<ISymbol> GetAllVirtualMembers(INamedTypeSymbol classType)
	{
		var seenSignatures = new HashSet<string>();

		// Walk up the inheritance hierarchy
		var current = classType;
		while (current != null && current.SpecialType != SpecialType.System_Object)
		{
			foreach (var member in current.GetMembers())
			{
				// Create a signature to detect duplicates from overrides
				var signature = GetMemberSignature(member);
				if (signature != null && seenSignatures.Add(signature))
				{
					yield return member;
				}
			}
			current = current.BaseType;
		}
	}

	/// <summary>
	/// Gets a unique signature for a member to detect duplicates.
	/// </summary>
	private static string? GetMemberSignature(ISymbol member)
	{
		return member switch
		{
			IPropertySymbol prop => prop.IsIndexer
				? $"indexer[{string.Join(",", prop.Parameters.Select(p => p.Type.ToDisplayString()))}]"
				: $"property:{prop.Name}",
			IMethodSymbol method when method.MethodKind == MethodKind.Ordinary =>
				$"method:{method.Name}({string.Join(",", method.Parameters.Select(p => p.Type.ToDisplayString()))})",
			IEventSymbol evt => $"event:{evt.Name}",
			_ => null
		};
	}

	/// <summary>
	/// Creates ClassMemberInfo for a property (including indexers).
	/// </summary>
	private static ClassMemberInfo CreateClassPropertyInfo(IPropertySymbol property)
	{
		var returnType = property.Type.ToDisplayString(FullyQualifiedWithNullability);
		var isNullable = property.Type.NullableAnnotation == NullableAnnotation.Annotated
			|| (property.Type.IsReferenceType && property.Type.NullableAnnotation != NullableAnnotation.NotAnnotated);
		var (defaultStrategy, concreteType) = GetDefaultValueStrategyWithConcreteType(property.Type);

		// Handle indexers
		var isIndexer = property.IsIndexer;
		var indexerParameters = EquatableArray<ParameterInfo>.Empty;
		var name = property.Name;

		if (isIndexer)
		{
			var paramTypes = property.Parameters
				.Select(p => GetSimpleTypeName(p.Type))
				.ToArray();
			name = string.Join("", paramTypes) + "Indexer";

			indexerParameters = new EquatableArray<ParameterInfo>(
				property.Parameters
					.Select(p => new ParameterInfo(p.Name, p.Type.ToDisplayString(FullyQualifiedWithNullability), p.RefKind))
					.ToArray());
		}

		var accessModifier = property.DeclaredAccessibility switch
		{
			Accessibility.Public => "public",
			Accessibility.Protected => "protected",
			Accessibility.ProtectedOrInternal => "protected internal",
			Accessibility.Internal => "internal",
			_ => "protected"
		};

		return new ClassMemberInfo(
			Name: name,
			ReturnType: returnType,
			IsProperty: true,
			IsIndexer: isIndexer,
			HasGetter: property.GetMethod is not null,
			HasSetter: property.SetMethod is not null,
			IsNullable: isNullable,
			DefaultStrategy: defaultStrategy,
			ConcreteTypeForNew: concreteType,
			Parameters: EquatableArray<ParameterInfo>.Empty,
			IndexerParameters: indexerParameters,
			IsGenericMethod: false,
			TypeParameters: EquatableArray<TypeParameterInfo>.Empty,
			IsAbstract: property.IsAbstract,
			AccessModifier: accessModifier);
	}

	/// <summary>
	/// Creates ClassMemberInfo for a method.
	/// </summary>
	private static ClassMemberInfo CreateClassMethodInfo(IMethodSymbol method)
	{
		var returnType = method.ReturnType.ToDisplayString(FullyQualifiedWithNullability);
		var isNullable = method.ReturnType.NullableAnnotation == NullableAnnotation.Annotated
			|| (method.ReturnType.IsReferenceType && method.ReturnType.NullableAnnotation != NullableAnnotation.NotAnnotated);
		var defaultStrategy = DefaultValueStrategy.Default;
		string? concreteType = null;

		if (method.ReturnsVoid)
			isNullable = true;

		var typeFullName = method.ReturnType.OriginalDefinition.ToDisplayString();
		if (typeFullName == "System.Threading.Tasks.Task" || typeFullName == "System.Threading.Tasks.ValueTask")
			isNullable = true;

		if (method.ReturnType is INamedTypeSymbol namedType && namedType.IsGenericType)
		{
			var origDef = namedType.OriginalDefinition.ToDisplayString();
			if (origDef == "System.Threading.Tasks.Task<TResult>" || origDef == "System.Threading.Tasks.ValueTask<TResult>")
			{
				var innerType = namedType.TypeArguments[0];
				isNullable = innerType.NullableAnnotation == NullableAnnotation.Annotated
					|| (innerType.IsReferenceType && innerType.NullableAnnotation != NullableAnnotation.NotAnnotated)
					|| innerType.IsValueType;
				(defaultStrategy, concreteType) = GetDefaultValueStrategyWithConcreteType(innerType);
			}
		}

		if (!method.ReturnsVoid && typeFullName != "System.Threading.Tasks.Task" && typeFullName != "System.Threading.Tasks.ValueTask")
		{
			(defaultStrategy, concreteType) = GetDefaultValueStrategyWithConcreteType(method.ReturnType);
		}

		var parameters = new List<ParameterInfo>();
		foreach (var param in method.Parameters)
		{
			parameters.Add(new ParameterInfo(
				param.Name,
				param.Type.ToDisplayString(FullyQualifiedWithNullability),
				param.RefKind));
		}

		var typeParameters = new List<TypeParameterInfo>();
		if (method.IsGenericMethod)
		{
			foreach (var tp in method.TypeParameters)
			{
				var constraints = GetTypeParameterConstraints(tp);
				typeParameters.Add(new TypeParameterInfo(tp.Name, new EquatableArray<string>(constraints.ToArray())));
			}
		}

		var accessModifier = method.DeclaredAccessibility switch
		{
			Accessibility.Public => "public",
			Accessibility.Protected => "protected",
			Accessibility.ProtectedOrInternal => "protected internal",
			Accessibility.Internal => "internal",
			_ => "protected"
		};

		return new ClassMemberInfo(
			Name: method.Name,
			ReturnType: returnType,
			IsProperty: false,
			IsIndexer: false,
			HasGetter: false,
			HasSetter: false,
			IsNullable: isNullable,
			DefaultStrategy: defaultStrategy,
			ConcreteTypeForNew: concreteType,
			Parameters: new EquatableArray<ParameterInfo>(parameters.ToArray()),
			IndexerParameters: EquatableArray<ParameterInfo>.Empty,
			IsGenericMethod: method.IsGenericMethod,
			TypeParameters: new EquatableArray<TypeParameterInfo>(typeParameters.ToArray()),
			IsAbstract: method.IsAbstract,
			AccessModifier: accessModifier);
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

		// Get containing types chain (for nested class support)
		var containingTypes = GetContainingTypes(classSymbol);

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
					iface.Name,
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
			ContainingTypes: containingTypes,
			Interfaces: new EquatableArray<InterfaceInfo>(interfaceInfos.ToArray()),
			UserMethods: userMethods);
	}

	/// <summary>
	/// Gets the chain of containing types for nested class support.
	/// Returns types from outermost to innermost.
	/// </summary>
	private static EquatableArray<ContainingTypeInfo> GetContainingTypes(INamedTypeSymbol classSymbol)
	{
		var containingTypes = new List<ContainingTypeInfo>();
		var current = classSymbol.ContainingType;

		while (current != null)
		{
			var keyword = current.TypeKind switch
			{
				TypeKind.Class => current.IsRecord ? "record" : "class",
				TypeKind.Struct => current.IsRecord ? "record struct" : "struct",
				TypeKind.Interface => "interface",
				_ => "class"
			};

			var accessibility = current.DeclaredAccessibility switch
			{
				Accessibility.Public => "public",
				Accessibility.Internal => "internal",
				Accessibility.Private => "private",
				Accessibility.Protected => "protected",
				Accessibility.ProtectedOrInternal => "protected internal",
				Accessibility.ProtectedAndInternal => "private protected",
				_ => ""
			};

			containingTypes.Insert(0, new ContainingTypeInfo(
				current.Name,
				keyword,
				accessibility));

			current = current.ContainingType;
		}

		return new EquatableArray<ContainingTypeInfo>(containingTypes.ToArray());
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
		var (defaultStrategy, concreteType) = GetDefaultValueStrategyWithConcreteType(property.Type);

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
			DefaultStrategy: defaultStrategy,
			ConcreteTypeForNew: concreteType,
			Parameters: EquatableArray<ParameterInfo>.Empty,
			IndexerParameters: indexerParameters,
			IsGenericMethod: false,
			TypeParameters: EquatableArray<TypeParameterInfo>.Empty);
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

	/// <summary>
	/// Determines the default value strategy for a return type.
	/// </summary>
	private static DefaultValueStrategy GetDefaultValueStrategy(ITypeSymbol type) =>
		GetDefaultValueStrategyWithConcreteType(type).Strategy;

	/// <summary>
	/// Determines the default value strategy and concrete type for a return type.
	/// For collection interfaces, returns the concrete implementation type.
	/// </summary>
	private static (DefaultValueStrategy Strategy, string? ConcreteType) GetDefaultValueStrategyWithConcreteType(ITypeSymbol type)
	{
		// Value types: always use default (0, false, etc.)
		if (type.IsValueType)
			return (DefaultValueStrategy.Default, null);

		// Nullable reference types: use default (null is valid)
		if (type.NullableAnnotation == NullableAnnotation.Annotated)
			return (DefaultValueStrategy.Default, null);

		// Non-nullable reference: check for accessible parameterless constructor
		if (type is INamedTypeSymbol named)
		{
			if (!named.IsAbstract && named.TypeKind == TypeKind.Class)
			{
				var hasParameterlessCtor = named.Constructors.Any(c =>
					c.Parameters.Length == 0 &&
					c.DeclaredAccessibility >= Accessibility.Public);

				if (hasParameterlessCtor)
					return (DefaultValueStrategy.NewInstance, null);
			}

			// Check for well-known collection interfaces
			if (named.TypeKind == TypeKind.Interface)
			{
				var concreteType = GetCollectionInterfaceMapping(named);
				if (concreteType is not null)
					return (DefaultValueStrategy.NewInstance, concreteType);
			}
		}

		// No safe default available (string, abstract class, interface, etc.)
		return (DefaultValueStrategy.ThrowException, null);
	}

	/// <summary>
	/// Maps well-known collection interfaces to concrete implementation types.
	/// Returns null if no mapping exists.
	/// </summary>
	private static string? GetCollectionInterfaceMapping(INamedTypeSymbol interfaceType)
	{
		var ns = interfaceType.ContainingNamespace?.ToDisplayString();
		if (ns != "System.Collections.Generic")
			return null;

		var name = interfaceType.Name;
		var typeArgs = interfaceType.TypeArguments;

		// Map collection interfaces to concrete types
		return name switch
		{
			// List-based interfaces
			"IEnumerable" when typeArgs.Length == 1 =>
				$"global::System.Collections.Generic.List<{typeArgs[0].ToDisplayString(FullyQualifiedWithNullability)}>",
			"ICollection" when typeArgs.Length == 1 =>
				$"global::System.Collections.Generic.List<{typeArgs[0].ToDisplayString(FullyQualifiedWithNullability)}>",
			"IList" when typeArgs.Length == 1 =>
				$"global::System.Collections.Generic.List<{typeArgs[0].ToDisplayString(FullyQualifiedWithNullability)}>",
			"IReadOnlyList" when typeArgs.Length == 1 =>
				$"global::System.Collections.Generic.List<{typeArgs[0].ToDisplayString(FullyQualifiedWithNullability)}>",
			"IReadOnlyCollection" when typeArgs.Length == 1 =>
				$"global::System.Collections.Generic.List<{typeArgs[0].ToDisplayString(FullyQualifiedWithNullability)}>",

			// Dictionary-based interfaces
			"IDictionary" when typeArgs.Length == 2 =>
				$"global::System.Collections.Generic.Dictionary<{typeArgs[0].ToDisplayString(FullyQualifiedWithNullability)}, {typeArgs[1].ToDisplayString(FullyQualifiedWithNullability)}>",
			"IReadOnlyDictionary" when typeArgs.Length == 2 =>
				$"global::System.Collections.Generic.Dictionary<{typeArgs[0].ToDisplayString(FullyQualifiedWithNullability)}, {typeArgs[1].ToDisplayString(FullyQualifiedWithNullability)}>",

			// Set-based interfaces
			"ISet" when typeArgs.Length == 1 =>
				$"global::System.Collections.Generic.HashSet<{typeArgs[0].ToDisplayString(FullyQualifiedWithNullability)}>",

			_ => null
		};
	}

	private static InterfaceMemberInfo CreateMethodInfo(IMethodSymbol method)
	{
		var returnType = method.ReturnType.ToDisplayString(FullyQualifiedWithNullability);
		var isNullable = method.ReturnType.NullableAnnotation == NullableAnnotation.Annotated
			|| (method.ReturnType.IsReferenceType && method.ReturnType.NullableAnnotation != NullableAnnotation.NotAnnotated);
		var defaultStrategy = DefaultValueStrategy.Default; // Default for void, Task, ValueTask
		string? concreteType = null;

		// For void methods, they're not "nullable" in the sense that matters
		if (method.ReturnsVoid)
			isNullable = true; // void can't throw for missing return

		// For Task and ValueTask (non-generic), treat as void-like
		var typeFullName = method.ReturnType.OriginalDefinition.ToDisplayString();
		if (typeFullName == "System.Threading.Tasks.Task" || typeFullName == "System.Threading.Tasks.ValueTask")
			isNullable = true; // async void-like, return completed task

		// For Task<T> and ValueTask<T>, check the inner type for nullability and default strategy
		if (method.ReturnType is INamedTypeSymbol namedType && namedType.IsGenericType)
		{
			var containingNs = namedType.ContainingNamespace?.ToDisplayString() ?? "";
			var typeName = namedType.Name;
			if (containingNs == "System.Threading.Tasks" && (typeName == "Task" || typeName == "ValueTask"))
			{
				var innerType = namedType.TypeArguments[0];
				isNullable = innerType.NullableAnnotation == NullableAnnotation.Annotated
					|| (innerType.IsReferenceType && innerType.NullableAnnotation != NullableAnnotation.NotAnnotated);
				(defaultStrategy, concreteType) = GetDefaultValueStrategyWithConcreteType(innerType);
			}
			else
			{
				// Generic type that's not Task/ValueTask (e.g., List<T>) - check strategy directly
				(defaultStrategy, concreteType) = GetDefaultValueStrategyWithConcreteType(method.ReturnType);
			}
		}
		else if (!method.ReturnsVoid &&
			typeFullName != "System.Threading.Tasks.Task" &&
			typeFullName != "System.Threading.Tasks.ValueTask")
		{
			// Non-void, non-Task return type - check strategy directly
			(defaultStrategy, concreteType) = GetDefaultValueStrategyWithConcreteType(method.ReturnType);
		}

		var parameters = method.Parameters
			.Select(p => new ParameterInfo(p.Name, p.Type.ToDisplayString(FullyQualifiedWithNullability), p.RefKind))
			.ToArray();

		// Extract type parameters for generic methods
		var isGenericMethod = method.IsGenericMethod;
		var typeParameters = EquatableArray<TypeParameterInfo>.Empty;

		if (isGenericMethod)
		{
			typeParameters = new EquatableArray<TypeParameterInfo>(
				method.TypeParameters
					.Select(tp => new TypeParameterInfo(
						tp.Name,
						new EquatableArray<string>(GetTypeParameterConstraints(tp).ToArray())))
					.ToArray());
		}

		return new InterfaceMemberInfo(
			Name: method.Name,
			ReturnType: returnType,
			IsProperty: false,
			IsIndexer: false,
			HasGetter: false,
			HasSetter: false,
			IsNullable: isNullable,
			DefaultStrategy: defaultStrategy,
			ConcreteTypeForNew: concreteType,
			Parameters: new EquatableArray<ParameterInfo>(parameters),
			IndexerParameters: EquatableArray<ParameterInfo>.Empty,
			IsGenericMethod: isGenericMethod,
			TypeParameters: typeParameters);
	}

	/// <summary>
	/// Extracts constraint strings from a type parameter symbol.
	/// </summary>
	private static IEnumerable<string> GetTypeParameterConstraints(ITypeParameterSymbol tp)
	{
		if (tp.HasReferenceTypeConstraint)
			yield return "class";
		if (tp.HasValueTypeConstraint)
			yield return "struct";
		if (tp.HasUnmanagedTypeConstraint)
			yield return "unmanaged";
		if (tp.HasNotNullConstraint)
			yield return "notnull";
		foreach (var constraintType in tp.ConstraintTypes)
			yield return constraintType.ToDisplayString(FullyQualifiedWithNullability);
		if (tp.HasConstructorConstraint)
			yield return "new()";
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
	/// Generate the partial class with explicit interface implementations
	/// </summary>
	private static void GenerateKnockOff(SourceProductionContext context, KnockOffTypeInfo typeInfo)
	{
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

		sb.AppendLine($"partial class {typeInfo.ClassName}");
		sb.AppendLine("{");

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

		// Build KO property names for each interface (with collision detection)
		var interfaceKONames = new Dictionary<InterfaceInfo, string>();
		foreach (var iface in typeInfo.Interfaces)
		{
			interfaceKONames[iface] = GetKOPropertyName(iface);
		}

		// For each interface, generate handlers and KO class
		foreach (var iface in typeInfo.Interfaces)
		{
			var koPropertyName = interfaceKONames[iface];

			// Group methods by name for this interface only
			var interfaceMethods = iface.Members.Where(m => !m.IsProperty && !m.IsIndexer);
			var methodGroups = GroupMethodsByName(interfaceMethods);

			// 1. Generate handler classes for this interface's members
			foreach (var member in iface.Members)
			{
				if (member.IsProperty || member.IsIndexer)
				{
					GenerateInterfaceMemberHandlerClass(sb, member, typeInfo.ClassName, koPropertyName);
				}
			}

			// 1b. Generate method group handlers for this interface
			foreach (var group in methodGroups.Values)
			{
				GenerateInterfaceMethodGroupHandlerClass(sb, group, typeInfo.ClassName, koPropertyName);
			}

			// 1c. Generate event handlers for this interface
			foreach (var evt in iface.Events)
			{
				GenerateInterfaceEventHandlerClass(sb, evt, typeInfo.ClassName, koPropertyName);
			}

			// 2. Generate KO class for this interface
			GenerateInterfaceKOClass(sb, typeInfo.ClassName, iface, koPropertyName, methodGroups);
		}

		// 3. Generate interface KO properties
		foreach (var iface in typeInfo.Interfaces)
		{
			var koPropertyName = interfaceKONames[iface];
			sb.AppendLine($"\t/// <summary>Tracks invocations and configures behavior for {iface.FullName}.</summary>");
			sb.AppendLine($"\tpublic {koPropertyName}Interceptorors {koPropertyName} {{ get; }} = new();");
			sb.AppendLine();
		}

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

		// 5. Generate backing properties/dictionaries PER INTERFACE (separate backing per interface)
		foreach (var iface in typeInfo.Interfaces)
		{
			var koPropertyName = interfaceKONames[iface];
			foreach (var member in iface.Members)
			{
				if (member.IsIndexer)
				{
					GenerateInterfaceIndexerBackingDictionary(sb, member, koPropertyName);
				}
				else if (member.IsProperty)
				{
					GenerateInterfaceBackingProperty(sb, member, koPropertyName);
				}
			}
		}

		// 6. Generate explicit interface implementations for EACH interface member
		foreach (var iface in typeInfo.Interfaces)
		{
			var koPropertyName = interfaceKONames[iface];

			// Build method groups for this interface for callback lookup
			var interfaceMethods = iface.Members.Where(m => !m.IsProperty && !m.IsIndexer);
			var methodGroups = GroupMethodsByName(interfaceMethods);

			foreach (var member in iface.Members)
			{
				if (member.IsIndexer)
				{
					GenerateInterfaceIndexerImplementation(sb, iface.FullName, member, koPropertyName);
				}
				else if (member.IsProperty)
				{
					GenerateInterfacePropertyImplementation(sb, iface.FullName, member, koPropertyName);
				}
				else
				{
					// All methods use delegate-based handler
					var group = methodGroups[member.Name];
					GenerateInterfaceMethod(sb, iface.FullName, member, typeInfo, group, koPropertyName);
				}
			}

			// 6b. Generate explicit interface implementations for events
			foreach (var evt in iface.Events)
			{
				GenerateInterfaceEventImplementation(sb, iface.FullName, evt, koPropertyName);
			}
		}

		sb.AppendLine("}");

		// Close containing type wrappers for nested classes
		for (int i = 0; i < typeInfo.ContainingTypes.Count; i++)
		{
			sb.AppendLine("}");
		}

		// Build hint name including containing types to ensure uniqueness
		var hintName = typeInfo.ContainingTypes.Count > 0
			? string.Join(".", typeInfo.ContainingTypes.Select(ct => ct.Name)) + "." + typeInfo.ClassName
			: typeInfo.ClassName;

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
	/// Only class and struct constraints are allowed (C# 9+).
	/// </summary>
	private static string GetClassOrStructConstraintsOnly(TypeParameterInfo[] typeParams)
	{
		var clauses = new List<string>();
		foreach (var tp in typeParams)
		{
			// Only "class" and "struct" are allowed in explicit interface implementations
			var allowedConstraints = tp.Constraints
				.Where(c => c == "class" || c == "struct")
				.ToArray();

			if (allowedConstraints.Length > 0)
			{
				var constraints = string.Join(", ", allowedConstraints);
				clauses.Add($" where {tp.Name} : {constraints}");
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
		sb.AppendLine($"\tpublic sealed class {koPropertyName}Interceptorors");
		sb.AppendLine("\t{");

		// Property/indexer handlers
		foreach (var member in iface.Members)
		{
			if (member.IsProperty || member.IsIndexer)
			{
				sb.AppendLine($"\t\t/// <summary>Interceptor for {member.Name}.</summary>");
				sb.AppendLine($"\t\tpublic {koPropertyName}_{member.Name}Interceptor {member.Name} {{ get; }} = new();");
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
				sb.AppendLine($"\t\tpublic {koPropertyName}_{group.Name}Interceptor {group.Name} {{ get; }} = new();");
			}
		}

		// Event handlers
		foreach (var evt in iface.Events)
		{
			sb.AppendLine($"\t\t/// <summary>Interceptor for {evt.Name} event.</summary>");
			sb.AppendLine($"\t\tpublic {koPropertyName}_{evt.Name}Interceptor {evt.Name} {{ get; }} = new();");
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
			sb.AppendLine("\t\tset");
			sb.AppendLine("\t\t{");
			sb.AppendLine($"\t\t\t{koPropertyName}.{prop.Name}.RecordSet(value);");
			sb.AppendLine($"\t\t\tif ({koPropertyName}.{prop.Name}.OnSet is {{ }} onSetCallback)");
			sb.AppendLine($"\t\t\t\tonSetCallback(this, value);");
			sb.AppendLine($"\t\t\telse");
			sb.AppendLine($"\t\t\t\t{backingName} = value;");
			sb.AppendLine("\t\t}");
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

		// For nullable reference type returns (T? where T : class), we need to include the class constraint
		// in the explicit implementation. C# 9+ allows class/struct constraints in explicit implementations.
		var constraintClauses = GetClassOrStructConstraintsOnly(typeParams);

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
		if (isVoid || isTask || isValueTask)
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

	#region Inline Stubs Generation

	/// <summary>
	/// Generates the Stubs nested class for inline stub pattern.
	/// </summary>
	private static void GenerateInlineStubs(SourceProductionContext context, InlineStubClassInfo info)
	{
		// Report all collected diagnostics
		foreach (var diag in info.Diagnostics)
		{
			var descriptor = diag.Id switch
			{
				"KO1001" => KO1001_TypeMustBeInterfaceClassOrDelegate,
				"KO1002" => KO1002_NameCollision,
				"KO1003" => KO1003_StubsTypeConflict,
				"KO2001" => KO2001_CannotStubSealedClass,
				"KO2002" => KO2002_NoAccessibleConstructors,
				"KO2003" => KO2003_NonVirtualMemberSkipped,
				"KO2004" => KO2004_NoVirtualMembers,
				"KO2005" => KO2005_CannotStubStaticClass,
				"KO2006" => KO2006_CannotStubBuiltInType,
				_ => null
			};

			if (descriptor is not null)
			{
				var location = Location.Create(
					diag.FilePath,
					new Microsoft.CodeAnalysis.Text.TextSpan(0, 0),
					new Microsoft.CodeAnalysis.Text.LinePositionSpan(
						new Microsoft.CodeAnalysis.Text.LinePosition(diag.Line, diag.Column),
						new Microsoft.CodeAnalysis.Text.LinePosition(diag.Line, diag.Column)));

				context.ReportDiagnostic(Diagnostic.Create(descriptor, location, diag.Args));
			}
		}

		// Check for blocking diagnostics - don't generate code if blocking errors present
		var hasBlockingDiagnostics = info.Diagnostics.Any(d => d.Id is "KO1002" or "KO1003" or "KO2001" or "KO2002" or "KO2005" or "KO2006");

		if (info.Interfaces.Count == 0 && info.Delegates.Count == 0 && info.Classes.Count == 0)
			return;

		if (hasBlockingDiagnostics)
			return;

		var sb = new System.Text.StringBuilder();

		sb.AppendLine("// <auto-generated/>");
		sb.AppendLine("#nullable enable");
		sb.AppendLine();

		if (!string.IsNullOrEmpty(info.Namespace))
		{
			sb.AppendLine($"namespace {info.Namespace};");
			sb.AppendLine();
		}

		// Open containing type wrappers for nested classes
		foreach (var containingType in info.ContainingTypes)
		{
			var accessMod = string.IsNullOrEmpty(containingType.AccessibilityModifier)
				? ""
				: containingType.AccessibilityModifier + " ";
			sb.AppendLine($"{accessMod}partial {containingType.Keyword} {containingType.Name}");
			sb.AppendLine("{");
		}

		sb.AppendLine($"partial class {info.ClassName}");
		sb.AppendLine("{");

		// Generate the nested Stubs class
		sb.AppendLine("\t/// <summary>Contains stub implementations for inline stub pattern.</summary>");
		sb.AppendLine("\tpublic static class Stubs");
		sb.AppendLine("\t{");

		foreach (var iface in info.Interfaces)
		{
			// Generate handler classes for this interface's members
			var methodGroups = GroupMethodsByName(iface.Members.Where(m => !m.IsProperty && !m.IsIndexer));

			// Generate handler classes for properties/indexers
			foreach (var member in iface.Members)
			{
				if (member.IsProperty || member.IsIndexer)
				{
					GenerateInlineStubMemberHandlerClass(sb, member, iface.Name);
				}
			}

			// Generate handler classes for methods
			foreach (var group in methodGroups.Values)
			{
				GenerateInlineStubMethodGroupHandlerClass(sb, group, iface.Name);
			}

			// Generate handler classes for events
			foreach (var evt in iface.Events)
			{
				GenerateInlineStubEventHandlerClass(sb, evt, iface.Name);
			}

			// Generate the stub class implementing the interface
			GenerateInlineStubClass(sb, iface, methodGroups);
		}

		// Generate delegate stub classes
		foreach (var del in info.Delegates)
		{
			GenerateDelegateStubClass(sb, del);
		}

		// Generate class stub classes (inheritance-based)
		foreach (var cls in info.Classes)
		{
			GenerateClassStubClass(sb, cls);
		}

		sb.AppendLine("\t}"); // Close Stubs class

		// Generate partial property implementations (outside Stubs class, inside partial class)
		foreach (var prop in info.PartialProperties)
		{
			var fieldName = $"__{prop.PropertyName}__Backing";
			var accessMod = string.IsNullOrEmpty(prop.AccessModifier) ? "" : $"{prop.AccessModifier} ";

			// Generate backing field
			sb.AppendLine();
			sb.AppendLine($"\tprivate readonly Stubs.{prop.StubTypeName} {fieldName} = new();");

			// Generate partial property implementation
			sb.AppendLine($"\t/// <summary>Auto-instantiated stub for {prop.StubTypeName}.</summary>");
			sb.AppendLine($"\t{accessMod}partial Stubs.{prop.StubTypeName} {prop.PropertyName} {{ get => {fieldName}; }}");
		}

		sb.AppendLine("}"); // Close partial class

		// Close containing type wrappers for nested classes
		for (int i = 0; i < info.ContainingTypes.Count; i++)
		{
			sb.AppendLine("}");
		}

		// Build hint name including containing types to ensure uniqueness
		var hintName = info.ContainingTypes.Count > 0
			? string.Join(".", info.ContainingTypes.Select(ct => ct.Name)) + "." + info.ClassName
			: info.ClassName;

		context.AddSource($"{hintName}.Stubs.g.cs", sb.ToString());
	}

	/// <summary>
	/// Generates a handler class for property/indexer member in inline stubs.
	/// </summary>
	private static void GenerateInlineStubMemberHandlerClass(
		System.Text.StringBuilder sb,
		InterfaceMemberInfo member,
		string interfaceSimpleName)
	{
		var interceptClassName = $"{interfaceSimpleName}_{member.Name}Interceptor";
		var stubClassName = $"Stubs.{interfaceSimpleName}";

		sb.AppendLine($"\t\t/// <summary>Interceptor for {interfaceSimpleName}.{member.Name}.</summary>");
		sb.AppendLine($"\t\tpublic sealed class {interceptClassName}");
		sb.AppendLine("\t\t{");

		if (member.IsIndexer)
		{
			GenerateInlineIndexerHandler(sb, member, stubClassName);
		}
		else if (member.IsProperty)
		{
			// Property: GetCount, SetCount, Value, OnGet, OnSet
			if (member.HasGetter)
			{
				sb.AppendLine("\t\t\t/// <summary>Number of times the getter was accessed.</summary>");
				sb.AppendLine("\t\t\tpublic int GetCount { get; private set; }");
				sb.AppendLine();
				sb.AppendLine($"\t\t\t/// <summary>Callback for getter. If set, returns its value.</summary>");
				sb.AppendLine($"\t\t\tpublic global::System.Func<{stubClassName}, {member.ReturnType}>? OnGet {{ get; set; }}");
				sb.AppendLine();
			}

			if (member.HasSetter)
			{
				sb.AppendLine("\t\t\t/// <summary>Number of times the setter was accessed.</summary>");
				sb.AppendLine("\t\t\tpublic int SetCount { get; private set; }");
				sb.AppendLine();
				var nullableType = MakeNullable(member.ReturnType);
				sb.AppendLine($"\t\t\t/// <summary>The last value passed to the setter.</summary>");
				sb.AppendLine($"\t\t\tpublic {nullableType} LastSetValue {{ get; private set; }}");
				sb.AppendLine();
				sb.AppendLine($"\t\t\t/// <summary>Callback for setter.</summary>");
				sb.AppendLine($"\t\t\tpublic global::System.Action<{stubClassName}, {member.ReturnType}>? OnSet {{ get; set; }}");
				sb.AppendLine();
			}

			// Value property (for stubbing return value)
			sb.AppendLine($"\t\t\t/// <summary>Value returned by getter when OnGet is not set.</summary>");
			sb.AppendLine($"\t\t\tpublic {member.ReturnType} Value {{ get; set; }} = default!;");
			sb.AppendLine();

			// RecordGet/RecordSet
			if (member.HasGetter)
			{
				sb.AppendLine("\t\t\t/// <summary>Records a getter access.</summary>");
				sb.AppendLine("\t\t\tpublic void RecordGet() => GetCount++;");
				sb.AppendLine();
			}
			if (member.HasSetter)
			{
				var nullableType = MakeNullable(member.ReturnType);
				sb.AppendLine("\t\t\t/// <summary>Records a setter access.</summary>");
				sb.AppendLine($"\t\t\tpublic void RecordSet({nullableType} value) {{ SetCount++; LastSetValue = value; }}");
				sb.AppendLine();
			}

			// Reset method
			sb.AppendLine("\t\t\t/// <summary>Resets all tracking state.</summary>");
			sb.Append("\t\t\tpublic void Reset() { ");
			if (member.HasGetter) sb.Append("GetCount = 0; OnGet = null; ");
			if (member.HasSetter) sb.Append("SetCount = 0; LastSetValue = default; OnSet = null; ");
			sb.Append("Value = default!; ");
			sb.AppendLine("}");
		}

		sb.AppendLine("\t\t}");
		sb.AppendLine();
	}

	/// <summary>
	/// Generates handler content for indexer in inline stubs.
	/// </summary>
	private static void GenerateInlineIndexerHandler(
		System.Text.StringBuilder sb,
		InterfaceMemberInfo member,
		string stubClassName)
	{
		var keyType = member.IndexerParameters.Count == 1
			? member.IndexerParameters.GetArray()![0].Type
			: $"({string.Join(", ", member.IndexerParameters.Select(p => $"{p.Type} {p.Name}"))})";

		if (member.HasGetter)
		{
			sb.AppendLine("\t\t\t/// <summary>Number of times the getter was accessed.</summary>");
			sb.AppendLine("\t\t\tpublic int GetCount { get; private set; }");
			sb.AppendLine();

			var nullableKeyType = MakeNullable(keyType);
			sb.AppendLine($"\t\t\t/// <summary>The last key used to access the getter.</summary>");
			sb.AppendLine($"\t\t\tpublic {nullableKeyType} LastGetKey {{ get; private set; }}");
			sb.AppendLine();

			// OnGet callback
			var paramList = string.Join(", ", member.IndexerParameters.Select(p => p.Type));
			sb.AppendLine($"\t\t\t/// <summary>Callback for getter.</summary>");
			sb.AppendLine($"\t\t\tpublic global::System.Func<{stubClassName}, {paramList}, {member.ReturnType}>? OnGet {{ get; set; }}");
			sb.AppendLine();
		}

		if (member.HasSetter)
		{
			sb.AppendLine("\t\t\t/// <summary>Number of times the setter was accessed.</summary>");
			sb.AppendLine("\t\t\tpublic int SetCount { get; private set; }");
			sb.AppendLine();

			var entryType = $"({keyType} Key, {member.ReturnType} Value)";
			sb.AppendLine($"\t\t\t/// <summary>The last key-value pair passed to the setter.</summary>");
			sb.AppendLine($"\t\t\tpublic {entryType}? LastSetEntry {{ get; private set; }}");
			sb.AppendLine();

			// OnSet callback
			var paramList = string.Join(", ", member.IndexerParameters.Select(p => p.Type));
			sb.AppendLine($"\t\t\t/// <summary>Callback for setter.</summary>");
			sb.AppendLine($"\t\t\tpublic global::System.Action<{stubClassName}, {paramList}, {member.ReturnType}>? OnSet {{ get; set; }}");
			sb.AppendLine();
		}

		// RecordGet/RecordSet
		if (member.HasGetter)
		{
			var paramSig = string.Join(", ", member.IndexerParameters.Select(p => $"{p.Type} {p.Name}"));
			var keyExpr = member.IndexerParameters.Count == 1
				? member.IndexerParameters.GetArray()![0].Name
				: $"({string.Join(", ", member.IndexerParameters.Select(p => p.Name))})";
			sb.AppendLine($"\t\t\t/// <summary>Records a getter access.</summary>");
			sb.AppendLine($"\t\t\tpublic void RecordGet({paramSig}) {{ GetCount++; LastGetKey = {keyExpr}; }}");
			sb.AppendLine();
		}
		if (member.HasSetter)
		{
			var paramSig = string.Join(", ", member.IndexerParameters.Select(p => $"{p.Type} {p.Name}"));
			var keyExpr = member.IndexerParameters.Count == 1
				? member.IndexerParameters.GetArray()![0].Name
				: $"({string.Join(", ", member.IndexerParameters.Select(p => p.Name))})";
			sb.AppendLine($"\t\t\t/// <summary>Records a setter access.</summary>");
			sb.AppendLine($"\t\t\tpublic void RecordSet({paramSig}, {member.ReturnType} value) {{ SetCount++; LastSetEntry = ({keyExpr}, value); }}");
			sb.AppendLine();
		}

		// Reset method
		sb.AppendLine("\t\t\t/// <summary>Resets all tracking state.</summary>");
		sb.Append("\t\t\tpublic void Reset() { ");
		if (member.HasGetter) sb.Append("GetCount = 0; LastGetKey = default; OnGet = null; ");
		if (member.HasSetter) sb.Append("SetCount = 0; LastSetEntry = default; OnSet = null; ");
		sb.AppendLine("}");
	}

	/// <summary>
	/// Generates a handler class for method group in inline stubs.
	/// </summary>
	private static void GenerateInlineStubMethodGroupHandlerClass(
		System.Text.StringBuilder sb,
		MethodGroupInfo group,
		string interfaceSimpleName)
	{
		var interceptClassName = $"{interfaceSimpleName}_{group.Name}Interceptor";
		var stubClassName = $"Stubs.{interfaceSimpleName}";
		var inputParams = GetInputCombinedParameters(group.CombinedParameters).ToArray();

		sb.AppendLine($"\t\t/// <summary>Interceptor for {interfaceSimpleName}.{group.Name}.</summary>");
		sb.AppendLine($"\t\tpublic sealed class {interceptClassName}");
		sb.AppendLine("\t\t{");

		// Delegate type for OnCall
		var delegateParamTypes = string.Join(", ", inputParams.Select(p => p.NullableType));
		var delegateParams = string.IsNullOrEmpty(delegateParamTypes)
			? stubClassName
			: $"{stubClassName}, {delegateParamTypes}";
		var delegateType = group.IsVoid
			? $"global::System.Action<{delegateParams}>"
			: $"global::System.Func<{delegateParams}, {group.ReturnType}>";

		sb.AppendLine("\t\t\t/// <summary>Number of times this method was called.</summary>");
		sb.AppendLine("\t\t\tpublic int CallCount { get; private set; }");
		sb.AppendLine();
		sb.AppendLine("\t\t\t/// <summary>Whether this method was called at least once.</summary>");
		sb.AppendLine("\t\t\tpublic bool WasCalled => CallCount > 0;");
		sb.AppendLine();

		// LastCallArg / LastCallArgs - always nullable since default before any call
		if (inputParams.Length == 1)
		{
			var param = inputParams[0];
			var nullableArgType = MakeNullable(param.Type);
			sb.AppendLine($"\t\t\t/// <summary>The argument from the last call.</summary>");
			sb.AppendLine($"\t\t\tpublic {nullableArgType} LastCallArg {{ get; private set; }}");
			sb.AppendLine();
		}
		else if (inputParams.Length > 1)
		{
			var tupleType = $"({string.Join(", ", inputParams.Select(p => $"{MakeNullable(p.Type)} {p.Name}"))})?";
			sb.AppendLine($"\t\t\t/// <summary>The arguments from the last call.</summary>");
			sb.AppendLine($"\t\t\tpublic {tupleType} LastCallArgs {{ get; private set; }}");
			sb.AppendLine();
		}

		// OnCall callback
		sb.AppendLine($"\t\t\t/// <summary>Callback invoked when method is called.</summary>");
		sb.AppendLine($"\t\t\tpublic {delegateType}? OnCall {{ get; set; }}");
		sb.AppendLine();

		// RecordCall method
		var recordParams = string.Join(", ", inputParams.Select(p => $"{p.NullableType} {p.Name}"));
		sb.Append($"\t\t\tpublic void RecordCall({recordParams}) {{ CallCount++; ");
		if (inputParams.Length == 1)
		{
			sb.Append($"LastCallArg = {inputParams[0].Name}; ");
		}
		else if (inputParams.Length > 1)
		{
			sb.Append($"LastCallArgs = ({string.Join(", ", inputParams.Select(p => p.Name))}); ");
		}
		sb.AppendLine("}");
		sb.AppendLine();

		// Reset method
		sb.Append("\t\t\tpublic void Reset() { CallCount = 0; ");
		if (inputParams.Length == 1)
		{
			sb.Append("LastCallArg = default; ");
		}
		else if (inputParams.Length > 1)
		{
			sb.Append("LastCallArgs = default; ");
		}
		sb.AppendLine("OnCall = null; }");

		sb.AppendLine("\t\t}");
		sb.AppendLine();
	}

	/// <summary>
	/// Generates a handler class for event in inline stubs.
	/// </summary>
	private static void GenerateInlineStubEventHandlerClass(
		System.Text.StringBuilder sb,
		EventMemberInfo evt,
		string interfaceSimpleName)
	{
		var interceptClassName = $"{interfaceSimpleName}_{evt.Name}Interceptor";

		sb.AppendLine($"\t\t/// <summary>Interceptor for {interfaceSimpleName}.{evt.Name} event.</summary>");
		sb.AppendLine($"\t\tpublic sealed class {interceptClassName}");
		sb.AppendLine("\t\t{");

		sb.AppendLine("\t\t\t/// <summary>Number of times the event was subscribed to.</summary>");
		sb.AppendLine("\t\t\tpublic int AddCount { get; private set; }");
		sb.AppendLine();
		sb.AppendLine("\t\t\t/// <summary>Number of times the event was unsubscribed from.</summary>");
		sb.AppendLine("\t\t\tpublic int RemoveCount { get; private set; }");
		sb.AppendLine();
		sb.AppendLine($"\t\t\t/// <summary>The backing delegate for raising the event.</summary>");
		sb.AppendLine($"\t\t\tpublic {evt.FullDelegateTypeName}? Handler {{ get; private set; }}");
		sb.AppendLine();
		sb.AppendLine("\t\t\t/// <summary>Records an event subscription.</summary>");
		sb.AppendLine($"\t\t\tpublic void RecordAdd({evt.FullDelegateTypeName}? handler) {{ AddCount++; Handler = ({evt.FullDelegateTypeName}?)global::System.Delegate.Combine(Handler, handler); }}");
		sb.AppendLine();
		sb.AppendLine("\t\t\t/// <summary>Records an event unsubscription.</summary>");
		sb.AppendLine($"\t\t\tpublic void RecordRemove({evt.FullDelegateTypeName}? handler) {{ RemoveCount++; Handler = ({evt.FullDelegateTypeName}?)global::System.Delegate.Remove(Handler, handler); }}");
		sb.AppendLine();
		sb.AppendLine("\t\t\t/// <summary>Resets all tracking state.</summary>");
		sb.AppendLine("\t\t\tpublic void Reset() { AddCount = 0; RemoveCount = 0; Handler = null; }");

		sb.AppendLine("\t\t}");
		sb.AppendLine();
	}

	/// <summary>
	/// Generates the stub class implementing the interface in inline stubs.
	/// </summary>
	private static void GenerateInlineStubClass(
		System.Text.StringBuilder sb,
		InterfaceInfo iface,
		Dictionary<string, MethodGroupInfo> methodGroups)
	{
		var stubClassName = iface.Name;

		sb.AppendLine($"\t\t/// <summary>Stub implementation of {iface.FullName}.</summary>");
		sb.AppendLine($"\t\tpublic class {stubClassName} : {iface.FullName}");
		sb.AppendLine("\t\t{");

		// Generate intercept properties
		foreach (var member in iface.Members)
		{
			if (member.IsProperty || member.IsIndexer)
			{
				sb.AppendLine($"\t\t\t/// <summary>Interceptor for {member.Name}.</summary>");
				sb.AppendLine($"\t\t\tpublic {stubClassName}_{member.Name}Interceptor {member.Name} {{ get; }} = new();");
				sb.AppendLine();
			}
		}
		foreach (var group in methodGroups.Values)
		{
			sb.AppendLine($"\t\t\t/// <summary>Interceptor for {group.Name}.</summary>");
			sb.AppendLine($"\t\t\tpublic {stubClassName}_{group.Name}Interceptor {group.Name} {{ get; }} = new();");
			sb.AppendLine();
		}
		foreach (var evt in iface.Events)
		{
			sb.AppendLine($"\t\t\t/// <summary>Interceptor for {evt.Name} event.</summary>");
			sb.AppendLine($"\t\t\tpublic {stubClassName}_{evt.Name}Interceptor {evt.Name}Interceptor {{ get; }} = new();");
			sb.AppendLine();
		}

		// Generate explicit interface implementations
		foreach (var member in iface.Members)
		{
			if (member.IsIndexer)
			{
				GenerateInlineStubIndexerImplementation(sb, iface.FullName, member, stubClassName);
			}
			else if (member.IsProperty)
			{
				GenerateInlineStubPropertyImplementation(sb, iface.FullName, member, stubClassName);
			}
			else
			{
				var group = methodGroups[member.Name];
				GenerateInlineStubMethodImplementation(sb, iface.FullName, member, group, stubClassName);
			}
		}

		// Generate event implementations
		foreach (var evt in iface.Events)
		{
			GenerateInlineStubEventImplementation(sb, iface.FullName, evt, stubClassName);
		}

		sb.AppendLine("\t\t}");
		sb.AppendLine();
	}

	/// <summary>
	/// Generates a stub class for a delegate type.
	/// </summary>
	private static void GenerateDelegateStubClass(
		System.Text.StringBuilder sb,
		DelegateInfo del)
	{
		var stubClassName = del.Name;
		var interceptClassName = $"{del.Name}Interceptor";

		// Generate handler class first
		sb.AppendLine($"\t\t/// <summary>Interceptor for {del.Name} delegate.</summary>");
		sb.AppendLine($"\t\tpublic sealed class {interceptClassName}");
		sb.AppendLine("\t\t{");

		// CallCount and WasCalled
		sb.AppendLine("\t\t\t/// <summary>Number of times this delegate was invoked.</summary>");
		sb.AppendLine("\t\t\tpublic int CallCount { get; private set; }");
		sb.AppendLine();
		sb.AppendLine("\t\t\t/// <summary>Whether this delegate was invoked at least once.</summary>");
		sb.AppendLine("\t\t\tpublic bool WasCalled => CallCount > 0;");
		sb.AppendLine();

		// LastCallArg/LastCallArgs based on parameter count
		if (del.Parameters.Count == 1)
		{
			var param = del.Parameters.GetArray()![0];
			var nullableType = MakeNullable(param.Type);
			sb.AppendLine($"\t\t\t/// <summary>The argument from the last invocation.</summary>");
			sb.AppendLine($"\t\t\tpublic {nullableType} LastCallArg {{ get; private set; }}");
			sb.AppendLine();
		}
		else if (del.Parameters.Count > 1)
		{
			var tupleTypes = string.Join(", ", del.Parameters.Select(p => $"{MakeNullable(p.Type)} {p.Name}"));
			sb.AppendLine($"\t\t\t/// <summary>The arguments from the last invocation.</summary>");
			sb.AppendLine($"\t\t\tpublic ({tupleTypes})? LastCallArgs {{ get; private set; }}");
			sb.AppendLine();
		}

		// OnCall callback
		var onCallType = GenerateOnCallType(del, $"Stubs.{stubClassName}");
		sb.AppendLine($"\t\t\t/// <summary>Callback invoked when delegate is called.</summary>");
		sb.AppendLine($"\t\t\tpublic {onCallType}? OnCall {{ get; set; }}");
		sb.AppendLine();

		// RecordCall method
		if (del.Parameters.Count == 0)
		{
			sb.AppendLine("\t\t\tpublic void RecordCall() { CallCount++; }");
		}
		else if (del.Parameters.Count == 1)
		{
			var param = del.Parameters.GetArray()![0];
			sb.AppendLine($"\t\t\tpublic void RecordCall({param.Type} {param.Name}) {{ CallCount++; LastCallArg = {param.Name}; }}");
		}
		else
		{
			var paramList = string.Join(", ", del.Parameters.Select(p => $"{p.Type} {p.Name}"));
			var argList = string.Join(", ", del.Parameters.Select(p => p.Name));
			sb.AppendLine($"\t\t\tpublic void RecordCall({paramList}) {{ CallCount++; LastCallArgs = ({argList}); }}");
		}
		sb.AppendLine();

		// Reset method
		sb.Append("\t\t\tpublic void Reset() { CallCount = 0; ");
		if (del.Parameters.Count == 1)
			sb.Append("LastCallArg = default; ");
		else if (del.Parameters.Count > 1)
			sb.Append("LastCallArgs = default; ");
		sb.AppendLine("OnCall = null; }");

		sb.AppendLine("\t\t}");
		sb.AppendLine();

		// Generate stub class
		sb.AppendLine($"\t\t/// <summary>Stub for {del.FullName} delegate.</summary>");
		sb.AppendLine($"\t\tpublic sealed class {stubClassName}");
		sb.AppendLine("\t\t{");

		// Interceptor property
		sb.AppendLine($"\t\t\t/// <summary>Interceptor for tracking and configuring delegate behavior.</summary>");
		sb.AppendLine($"\t\t\tpublic {interceptClassName} Interceptor {{ get; }} = new();");
		sb.AppendLine();

		// Private Invoke method
		var invokeParamList = string.Join(", ", del.Parameters.Select(p => $"{p.Type} {p.Name}"));
		var invokeArgList = string.Join(", ", del.Parameters.Select(p => p.Name));
		var recordCallArgs = del.Parameters.Count > 0 ? invokeArgList : "";

		sb.AppendLine($"\t\t\tprivate {del.ReturnType} Invoke({invokeParamList})");
		sb.AppendLine("\t\t\t{");
		sb.AppendLine($"\t\t\t\tInterceptor.RecordCall({recordCallArgs});");
		if (del.IsVoid)
		{
			var onCallArgs = del.Parameters.Count > 0 ? $"this, {invokeArgList}" : "this";
			sb.AppendLine($"\t\t\t\tif (Interceptor.OnCall is {{ }} onCall) onCall({onCallArgs});");
		}
		else
		{
			var onCallArgs = del.Parameters.Count > 0 ? $"this, {invokeArgList}" : "this";
			sb.AppendLine($"\t\t\t\tif (Interceptor.OnCall is {{ }} onCall) return onCall({onCallArgs});");
			sb.AppendLine("\t\t\t\treturn default!;");
		}
		sb.AppendLine("\t\t\t}");
		sb.AppendLine();

		// Implicit conversion operator to delegate
		sb.AppendLine($"\t\t\t/// <summary>Implicit conversion to {del.FullName}.</summary>");
		sb.AppendLine($"\t\t\tpublic static implicit operator {del.FullName}({stubClassName} stub) => stub.Invoke;");

		sb.AppendLine("\t\t}");
		sb.AppendLine();
	}

	/// <summary>
	/// Generates the OnCall type signature for a delegate stub.
	/// </summary>
	private static string GenerateOnCallType(DelegateInfo del, string stubClassName)
	{
		if (del.IsVoid)
		{
			// Action<Stubs.DelegateName, ...params>
			if (del.Parameters.Count == 0)
				return $"global::System.Action<{stubClassName}>";
			var paramTypes = string.Join(", ", del.Parameters.Select(p => p.Type));
			return $"global::System.Action<{stubClassName}, {paramTypes}>";
		}
		else
		{
			// Func<Stubs.DelegateName, ...params, returnType>
			if (del.Parameters.Count == 0)
				return $"global::System.Func<{stubClassName}, {del.ReturnType}>";
			var paramTypes = string.Join(", ", del.Parameters.Select(p => p.Type));
			return $"global::System.Func<{stubClassName}, {paramTypes}, {del.ReturnType}>";
		}
	}

	#region Class Stub Generation

	/// <summary>
	/// Generates a class stub that inherits from the target class.
	/// </summary>
	private static void GenerateClassStubClass(
		System.Text.StringBuilder sb,
		ClassStubInfo cls)
	{
		var stubClassName = cls.Name;
		var interceptorsClassName = $"{cls.Name}Interceptors";

		// Group methods by name for overload handling
		var methodGroups = GroupClassMethodsByName(cls.Members.Where(m => !m.IsProperty && !m.IsIndexer));

		// Generate interceptor classes for properties/indexers
		foreach (var member in cls.Members)
		{
			if (member.IsProperty || member.IsIndexer)
			{
				GenerateClassMemberInterceptorClass(sb, member, cls.Name);
			}
		}

		// Generate interceptor classes for method groups
		foreach (var group in methodGroups.Values)
		{
			GenerateClassMethodGroupInterceptorClass(sb, group, cls.Name);
		}

		// Generate interceptor classes for events
		foreach (var evt in cls.Events)
		{
			GenerateInlineStubEventHandlerClass(sb, evt, cls.Name);
		}

		// Generate the interceptors container class
		GenerateClassInterceptorsContainer(sb, cls, methodGroups, interceptorsClassName);

		// Generate the stub class
		sb.AppendLine($"\t\t/// <summary>Stub for {cls.FullName} via inheritance.</summary>");
		sb.AppendLine($"\t\tpublic class {stubClassName} : {cls.FullName}");
		sb.AppendLine("\t\t{");

		// Interceptor container property - using "Interceptor" to avoid name collision with class name
		sb.AppendLine($"\t\t\t/// <summary>Interceptors for tracking and configuring member behavior.</summary>");
		sb.AppendLine($"\t\t\tpublic {interceptorsClassName} Interceptor {{ get; }} = new();");
		sb.AppendLine();

		// Generate constructors that chain to base
		foreach (var ctor in cls.Constructors)
		{
			GenerateClassConstructor(sb, ctor, stubClassName, cls.FullName);
		}

		// Generate overrides for properties
		foreach (var member in cls.Members)
		{
			if (member.IsProperty && !member.IsIndexer)
			{
				GenerateClassPropertyOverride(sb, member, cls.Name);
			}
			else if (member.IsIndexer)
			{
				GenerateClassIndexerOverride(sb, member, cls.Name);
			}
		}

		// Generate overrides for methods
		foreach (var group in methodGroups.Values)
		{
			var hasOverloads = group.Members.Count > 1;
			for (int i = 0; i < group.Members.Count; i++)
			{
				var member = group.Members.GetArray()![i];
				var handlerName = hasOverloads ? $"{group.Name}{i + 1}" : group.Name;
				GenerateClassMethodOverride(sb, member, cls.Name, handlerName);
			}
		}

		// Generate overrides for events
		foreach (var evt in cls.Events)
		{
			GenerateClassEventOverride(sb, evt, cls.Name);
		}

		// ResetInterceptors method
		sb.AppendLine("\t\t\t/// <summary>Resets all interceptor state.</summary>");
		sb.AppendLine($"\t\t\tpublic void ResetInterceptors() => Interceptor.Reset();");

		sb.AppendLine("\t\t}");
		sb.AppendLine();
	}

	/// <summary>
	/// Groups class methods by name for overload handling.
	/// </summary>
	private static Dictionary<string, ClassMethodGroupInfo> GroupClassMethodsByName(IEnumerable<ClassMemberInfo> methods)
	{
		// First, group into lists by method name
		var tempGroups = new Dictionary<string, List<ClassMemberInfo>>();

		foreach (var method in methods)
		{
			if (!tempGroups.TryGetValue(method.Name, out var list))
			{
				list = new List<ClassMemberInfo>();
				tempGroups[method.Name] = list;
			}
			list.Add(method);
		}

		// Convert to ClassMethodGroupInfo
		return tempGroups.ToDictionary(
			kvp => kvp.Key,
			kvp =>
			{
				var first = kvp.Value[0];
				return new ClassMethodGroupInfo(
					kvp.Key,
					first.ReturnType,
					first.ReturnType == "void",
					first.IsNullable,
					kvp.Value);
			});
	}

	/// <summary>
	/// Generates an interceptor class for a property or indexer.
	/// </summary>
	private static void GenerateClassMemberInterceptorClass(
		System.Text.StringBuilder sb,
		ClassMemberInfo member,
		string className)
	{
		var interceptClassName = $"{className}_{member.Name}Interceptor";
		var stubClassName = $"Stubs.{className}";

		sb.AppendLine($"\t\t/// <summary>Interceptor for {className}.{member.Name}.</summary>");
		sb.AppendLine($"\t\tpublic sealed class {interceptClassName}");
		sb.AppendLine("\t\t{");

		if (member.IsIndexer)
		{
			GenerateClassIndexerInterceptorMembers(sb, member, stubClassName);
		}
		else
		{
			// Property interceptor
			if (member.HasGetter)
			{
				sb.AppendLine("\t\t\t/// <summary>Number of times the getter was accessed.</summary>");
				sb.AppendLine("\t\t\tpublic int GetCount { get; private set; }");
				sb.AppendLine();
				sb.AppendLine($"\t\t\t/// <summary>Callback for getter. If set, returns its value instead of base.</summary>");
				sb.AppendLine($"\t\t\tpublic global::System.Func<{stubClassName}, {member.ReturnType}>? OnGet {{ get; set; }}");
				sb.AppendLine();
			}

			if (member.HasSetter)
			{
				sb.AppendLine("\t\t\t/// <summary>Number of times the setter was accessed.</summary>");
				sb.AppendLine("\t\t\tpublic int SetCount { get; private set; }");
				sb.AppendLine();
				var nullableType = MakeNullable(member.ReturnType);
				sb.AppendLine($"\t\t\t/// <summary>The last value passed to the setter.</summary>");
				sb.AppendLine($"\t\t\tpublic {nullableType} LastSetValue {{ get; private set; }}");
				sb.AppendLine();
				sb.AppendLine($"\t\t\t/// <summary>Callback for setter. If set, called instead of base.</summary>");
				sb.AppendLine($"\t\t\tpublic global::System.Action<{stubClassName}, {member.ReturnType}>? OnSet {{ get; set; }}");
				sb.AppendLine();
			}

			// RecordGet/RecordSet
			if (member.HasGetter)
			{
				sb.AppendLine("\t\t\t/// <summary>Records a getter access.</summary>");
				sb.AppendLine("\t\t\tpublic void RecordGet() => GetCount++;");
				sb.AppendLine();
			}
			if (member.HasSetter)
			{
				var nullableType = MakeNullable(member.ReturnType);
				sb.AppendLine("\t\t\t/// <summary>Records a setter access.</summary>");
				sb.AppendLine($"\t\t\tpublic void RecordSet({nullableType} value) {{ SetCount++; LastSetValue = value; }}");
				sb.AppendLine();
			}

			// Reset method
			sb.AppendLine("\t\t\t/// <summary>Resets all tracking state.</summary>");
			sb.Append("\t\t\tpublic void Reset() { ");
			if (member.HasGetter) sb.Append("GetCount = 0; OnGet = null; ");
			if (member.HasSetter) sb.Append("SetCount = 0; LastSetValue = default; OnSet = null; ");
			sb.AppendLine("}");
		}

		sb.AppendLine("\t\t}");
		sb.AppendLine();
	}

	/// <summary>
	/// Generates indexer interceptor members.
	/// </summary>
	private static void GenerateClassIndexerInterceptorMembers(
		System.Text.StringBuilder sb,
		ClassMemberInfo member,
		string stubClassName)
	{
		var keyType = member.IndexerParameters.Count == 1
			? member.IndexerParameters.GetArray()![0].Type
			: $"({string.Join(", ", member.IndexerParameters.Select(p => $"{p.Type} {p.Name}"))})";

		if (member.HasGetter)
		{
			sb.AppendLine("\t\t\t/// <summary>Number of times the getter was accessed.</summary>");
			sb.AppendLine("\t\t\tpublic int GetCount { get; private set; }");
			sb.AppendLine();

			var nullableKeyType = MakeNullable(keyType);
			sb.AppendLine($"\t\t\t/// <summary>The last key used to access the getter.</summary>");
			sb.AppendLine($"\t\t\tpublic {nullableKeyType} LastGetKey {{ get; private set; }}");
			sb.AppendLine();

			var paramList = string.Join(", ", member.IndexerParameters.Select(p => p.Type));
			sb.AppendLine($"\t\t\t/// <summary>Callback for getter.</summary>");
			sb.AppendLine($"\t\t\tpublic global::System.Func<{stubClassName}, {paramList}, {member.ReturnType}>? OnGet {{ get; set; }}");
			sb.AppendLine();
		}

		if (member.HasSetter)
		{
			sb.AppendLine("\t\t\t/// <summary>Number of times the setter was accessed.</summary>");
			sb.AppendLine("\t\t\tpublic int SetCount { get; private set; }");
			sb.AppendLine();

			var entryType = $"({keyType} Key, {member.ReturnType} Value)";
			sb.AppendLine($"\t\t\t/// <summary>The last key-value pair passed to the setter.</summary>");
			sb.AppendLine($"\t\t\tpublic {entryType}? LastSetEntry {{ get; private set; }}");
			sb.AppendLine();

			var paramList = string.Join(", ", member.IndexerParameters.Select(p => p.Type));
			sb.AppendLine($"\t\t\t/// <summary>Callback for setter.</summary>");
			sb.AppendLine($"\t\t\tpublic global::System.Action<{stubClassName}, {paramList}, {member.ReturnType}>? OnSet {{ get; set; }}");
			sb.AppendLine();
		}

		// RecordGet/RecordSet
		if (member.HasGetter)
		{
			var paramSig = string.Join(", ", member.IndexerParameters.Select(p => $"{p.Type} {p.Name}"));
			var keyExpr = member.IndexerParameters.Count == 1
				? member.IndexerParameters.GetArray()![0].Name
				: $"({string.Join(", ", member.IndexerParameters.Select(p => p.Name))})";
			sb.AppendLine($"\t\t\t/// <summary>Records a getter access.</summary>");
			sb.AppendLine($"\t\t\tpublic void RecordGet({paramSig}) {{ GetCount++; LastGetKey = {keyExpr}; }}");
			sb.AppendLine();
		}
		if (member.HasSetter)
		{
			var paramSig = string.Join(", ", member.IndexerParameters.Select(p => $"{p.Type} {p.Name}"));
			var keyExpr = member.IndexerParameters.Count == 1
				? member.IndexerParameters.GetArray()![0].Name
				: $"({string.Join(", ", member.IndexerParameters.Select(p => p.Name))})";
			sb.AppendLine($"\t\t\t/// <summary>Records a setter access.</summary>");
			sb.AppendLine($"\t\t\tpublic void RecordSet({paramSig}, {member.ReturnType} value) {{ SetCount++; LastSetEntry = ({keyExpr}, value); }}");
			sb.AppendLine();
		}

		// Reset method
		sb.AppendLine("\t\t\t/// <summary>Resets all tracking state.</summary>");
		sb.Append("\t\t\tpublic void Reset() { ");
		if (member.HasGetter) sb.Append("GetCount = 0; LastGetKey = default; OnGet = null; ");
		if (member.HasSetter) sb.Append("SetCount = 0; LastSetEntry = default; OnSet = null; ");
		sb.AppendLine("}");
	}

	/// <summary>
	/// Generates an interceptor class for a method group.
	/// </summary>
	private static void GenerateClassMethodGroupInterceptorClass(
		System.Text.StringBuilder sb,
		ClassMethodGroupInfo group,
		string className)
	{
		var hasOverloads = group.Members.Count > 1;

		for (int i = 0; i < group.Members.Count; i++)
		{
			var member = group.Members.GetArray()![i];
			var interceptClassName = hasOverloads
				? $"{className}_{group.Name}{i + 1}Interceptor"
				: $"{className}_{group.Name}Interceptor";
			var stubClassName = $"Stubs.{className}";

			var inputParams = GetInputParameters(member.Parameters).ToArray();

			sb.AppendLine($"\t\t/// <summary>Interceptor for {className}.{group.Name}.</summary>");
			sb.AppendLine($"\t\tpublic sealed class {interceptClassName}");
			sb.AppendLine("\t\t{");

			// Delegate type for OnCall
			var delegateParamTypes = string.Join(", ", inputParams.Select(p => p.Type));
			var delegateParams = string.IsNullOrEmpty(delegateParamTypes)
				? stubClassName
				: $"{stubClassName}, {delegateParamTypes}";
			var isVoid = member.ReturnType == "void";
			var delegateType = isVoid
				? $"global::System.Action<{delegateParams}>"
				: $"global::System.Func<{delegateParams}, {member.ReturnType}>";

			sb.AppendLine("\t\t\t/// <summary>Number of times this method was called.</summary>");
			sb.AppendLine("\t\t\tpublic int CallCount { get; private set; }");
			sb.AppendLine();
			sb.AppendLine("\t\t\t/// <summary>Whether this method was called at least once.</summary>");
			sb.AppendLine("\t\t\tpublic bool WasCalled => CallCount > 0;");
			sb.AppendLine();

			// LastCallArg / LastCallArgs
			if (inputParams.Length == 1)
			{
				var param = inputParams[0];
				var nullableArgType = MakeNullable(param.Type);
				sb.AppendLine($"\t\t\t/// <summary>The argument from the last call.</summary>");
				sb.AppendLine($"\t\t\tpublic {nullableArgType} LastCallArg {{ get; private set; }}");
				sb.AppendLine();
			}
			else if (inputParams.Length > 1)
			{
				var tupleType = $"({string.Join(", ", inputParams.Select(p => $"{MakeNullable(p.Type)} {p.Name}"))})?";
				sb.AppendLine($"\t\t\t/// <summary>The arguments from the last call.</summary>");
				sb.AppendLine($"\t\t\tpublic {tupleType} LastCallArgs {{ get; private set; }}");
				sb.AppendLine();
			}

			// OnCall callback
			sb.AppendLine($"\t\t\t/// <summary>Callback invoked when method is called. If set, called instead of base.</summary>");
			sb.AppendLine($"\t\t\tpublic {delegateType}? OnCall {{ get; set; }}");
			sb.AppendLine();

			// RecordCall method
			var recordParams = string.Join(", ", inputParams.Select(p => $"{p.Type} {p.Name}"));
			sb.Append($"\t\t\tpublic void RecordCall({recordParams}) {{ CallCount++; ");
			if (inputParams.Length == 1)
			{
				sb.Append($"LastCallArg = {inputParams[0].Name}; ");
			}
			else if (inputParams.Length > 1)
			{
				sb.Append($"LastCallArgs = ({string.Join(", ", inputParams.Select(p => p.Name))}); ");
			}
			sb.AppendLine("}");
			sb.AppendLine();

			// Reset method
			sb.Append("\t\t\tpublic void Reset() { CallCount = 0; ");
			if (inputParams.Length == 1)
			{
				sb.Append("LastCallArg = default; ");
			}
			else if (inputParams.Length > 1)
			{
				sb.Append("LastCallArgs = default; ");
			}
			sb.AppendLine("OnCall = null; }");

			sb.AppendLine("\t\t}");
			sb.AppendLine();
		}
	}

	/// <summary>
	/// Generates the interceptors container class.
	/// </summary>
	private static void GenerateClassInterceptorsContainer(
		System.Text.StringBuilder sb,
		ClassStubInfo cls,
		Dictionary<string, ClassMethodGroupInfo> methodGroups,
		string containerClassName)
	{
		sb.AppendLine($"\t\t/// <summary>Container for all {cls.Name} interceptors.</summary>");
		sb.AppendLine($"\t\tpublic sealed class {containerClassName}");
		sb.AppendLine("\t\t{");

		// Property interceptor instances
		foreach (var member in cls.Members)
		{
			if (member.IsProperty || member.IsIndexer)
			{
				var interceptorType = $"{cls.Name}_{member.Name}Interceptor";
				sb.AppendLine($"\t\t\t/// <summary>Interceptor for {member.Name}.</summary>");
				sb.AppendLine($"\t\t\tpublic {interceptorType} {member.Name} {{ get; }} = new();");
			}
		}

		// Method interceptor instances
		foreach (var group in methodGroups.Values)
		{
			var hasOverloads = group.Members.Count > 1;
			for (int i = 0; i < group.Members.Count; i++)
			{
				var handlerName = hasOverloads ? $"{group.Name}{i + 1}" : group.Name;
				var interceptorType = hasOverloads
					? $"{cls.Name}_{group.Name}{i + 1}Interceptor"
					: $"{cls.Name}_{group.Name}Interceptor";
				sb.AppendLine($"\t\t\t/// <summary>Interceptor for {group.Name}.</summary>");
				sb.AppendLine($"\t\t\tpublic {interceptorType} {handlerName} {{ get; }} = new();");
			}
		}

		// Event interceptor instances
		foreach (var evt in cls.Events)
		{
			var interceptorType = $"{cls.Name}_{evt.Name}Interceptor";
			sb.AppendLine($"\t\t\t/// <summary>Interceptor for {evt.Name}.</summary>");
			sb.AppendLine($"\t\t\tpublic {interceptorType} {evt.Name} {{ get; }} = new();");
		}

		sb.AppendLine();

		// Reset method
		sb.AppendLine("\t\t\t/// <summary>Resets all interceptors.</summary>");
		sb.AppendLine("\t\t\tpublic void Reset()");
		sb.AppendLine("\t\t\t{");

		foreach (var member in cls.Members)
		{
			if (member.IsProperty || member.IsIndexer)
			{
				sb.AppendLine($"\t\t\t\t{member.Name}.Reset();");
			}
		}

		foreach (var group in methodGroups.Values)
		{
			var hasOverloads = group.Members.Count > 1;
			for (int i = 0; i < group.Members.Count; i++)
			{
				var handlerName = hasOverloads ? $"{group.Name}{i + 1}" : group.Name;
				sb.AppendLine($"\t\t\t\t{handlerName}.Reset();");
			}
		}

		foreach (var evt in cls.Events)
		{
			sb.AppendLine($"\t\t\t\t{evt.Name}.Reset();");
		}

		sb.AppendLine("\t\t\t}");
		sb.AppendLine("\t\t}");
		sb.AppendLine();
	}

	/// <summary>
	/// Generates a constructor that chains to the base class.
	/// </summary>
	private static void GenerateClassConstructor(
		System.Text.StringBuilder sb,
		ClassConstructorInfo ctor,
		string stubClassName,
		string baseClassName)
	{
		var paramList = string.Join(", ", ctor.Parameters.Select(p => $"{p.Type} {p.Name}"));
		var argList = string.Join(", ", ctor.Parameters.Select(p => p.Name));

		sb.AppendLine($"\t\t\tpublic {stubClassName}({paramList}) : base({argList}) {{ }}");
		sb.AppendLine();
	}

	/// <summary>
	/// Generates a property override.
	/// </summary>
	private static void GenerateClassPropertyOverride(
		System.Text.StringBuilder sb,
		ClassMemberInfo member,
		string className)
	{
		sb.AppendLine($"\t\t\t/// <inheritdoc />");
		sb.AppendLine($"\t\t\t{member.AccessModifier} override {member.ReturnType} {member.Name}");
		sb.AppendLine("\t\t\t{");

		if (member.HasGetter)
		{
			sb.AppendLine("\t\t\t\tget");
			sb.AppendLine("\t\t\t\t{");
			sb.AppendLine($"\t\t\t\t\tInterceptor.{member.Name}.RecordGet();");
			sb.AppendLine($"\t\t\t\t\tif (Interceptor.{member.Name}.OnGet is {{ }} onGet) return onGet(this);");
			if (member.IsAbstract)
			{
				sb.AppendLine($"\t\t\t\t\treturn default!;");
			}
			else
			{
				sb.AppendLine($"\t\t\t\t\treturn base.{member.Name};");
			}
			sb.AppendLine("\t\t\t\t}");
		}

		if (member.HasSetter)
		{
			sb.AppendLine("\t\t\t\tset");
			sb.AppendLine("\t\t\t\t{");
			sb.AppendLine($"\t\t\t\t\tInterceptor.{member.Name}.RecordSet(value);");
			sb.AppendLine($"\t\t\t\t\tif (Interceptor.{member.Name}.OnSet is {{ }} onSet) onSet(this, value);");
			if (member.IsAbstract)
			{
				// Abstract - no base to call
			}
			else
			{
				sb.AppendLine($"\t\t\t\t\telse base.{member.Name} = value;");
			}
			sb.AppendLine("\t\t\t\t}");
		}

		sb.AppendLine("\t\t\t}");
		sb.AppendLine();
	}

	/// <summary>
	/// Generates an indexer override.
	/// </summary>
	private static void GenerateClassIndexerOverride(
		System.Text.StringBuilder sb,
		ClassMemberInfo member,
		string className)
	{
		var paramList = string.Join(", ", member.IndexerParameters.Select(p => $"{p.Type} {p.Name}"));
		var argList = string.Join(", ", member.IndexerParameters.Select(p => p.Name));

		sb.AppendLine($"\t\t\t/// <inheritdoc />");
		sb.AppendLine($"\t\t\t{member.AccessModifier} override {member.ReturnType} this[{paramList}]");
		sb.AppendLine("\t\t\t{");

		if (member.HasGetter)
		{
			sb.AppendLine("\t\t\t\tget");
			sb.AppendLine("\t\t\t\t{");
			sb.AppendLine($"\t\t\t\t\tInterceptor.{member.Name}.RecordGet({argList});");
			sb.AppendLine($"\t\t\t\t\tif (Interceptor.{member.Name}.OnGet is {{ }} onGet) return onGet(this, {argList});");
			if (member.IsAbstract)
			{
				sb.AppendLine($"\t\t\t\t\treturn default!;");
			}
			else
			{
				sb.AppendLine($"\t\t\t\t\treturn base[{argList}];");
			}
			sb.AppendLine("\t\t\t\t}");
		}

		if (member.HasSetter)
		{
			sb.AppendLine("\t\t\t\tset");
			sb.AppendLine("\t\t\t\t{");
			sb.AppendLine($"\t\t\t\t\tInterceptor.{member.Name}.RecordSet({argList}, value);");
			sb.AppendLine($"\t\t\t\t\tif (Interceptor.{member.Name}.OnSet is {{ }} onSet) onSet(this, {argList}, value);");
			if (!member.IsAbstract)
			{
				sb.AppendLine($"\t\t\t\t\telse base[{argList}] = value;");
			}
			sb.AppendLine("\t\t\t\t}");
		}

		sb.AppendLine("\t\t\t}");
		sb.AppendLine();
	}

	/// <summary>
	/// Generates a method override.
	/// </summary>
	private static void GenerateClassMethodOverride(
		System.Text.StringBuilder sb,
		ClassMemberInfo member,
		string className,
		string handlerName)
	{
		var paramList = string.Join(", ", member.Parameters.Select(p => FormatParameter(p)));
		var argList = string.Join(", ", member.Parameters.Select(p => FormatArgument(p)));
		var inputParams = GetInputParameters(member.Parameters).ToArray();
		var inputArgList = string.Join(", ", inputParams.Select(p => p.Name));

		var isVoid = member.ReturnType == "void";
		var isTask = member.ReturnType == "global::System.Threading.Tasks.Task";
		var isValueTask = member.ReturnType == "global::System.Threading.Tasks.ValueTask";

		sb.AppendLine($"\t\t\t/// <inheritdoc />");
		sb.AppendLine($"\t\t\t{member.AccessModifier} override {member.ReturnType} {member.Name}({paramList})");
		sb.AppendLine("\t\t\t{");

		// Record the call
		if (inputParams.Length > 0)
		{
			sb.AppendLine($"\t\t\t\tInterceptor.{handlerName}.RecordCall({inputArgList});");
		}
		else
		{
			sb.AppendLine($"\t\t\t\tInterceptor.{handlerName}.RecordCall();");
		}

		// Check for OnCall callback
		var onCallArgs = inputParams.Length > 0 ? $"this, {inputArgList}" : "this";
		if (isVoid || isTask || isValueTask)
		{
			sb.AppendLine($"\t\t\t\tif (Interceptor.{handlerName}.OnCall is {{ }} onCall) {{ onCall({onCallArgs}); return; }}");
		}
		else
		{
			sb.AppendLine($"\t\t\t\tif (Interceptor.{handlerName}.OnCall is {{ }} onCall) return onCall({onCallArgs});");
		}

		// Default behavior - delegate to base or return default for abstract
		if (member.IsAbstract)
		{
			// Abstract - return default
			if (isVoid)
			{
				// void - nothing to return
			}
			else if (isTask)
			{
				sb.AppendLine($"\t\t\t\treturn global::System.Threading.Tasks.Task.CompletedTask;");
			}
			else if (isValueTask)
			{
				sb.AppendLine($"\t\t\t\treturn default;");
			}
			else
			{
				sb.AppendLine($"\t\t\t\treturn default!;");
			}
		}
		else
		{
			// Virtual - delegate to base
			if (isVoid)
			{
				sb.AppendLine($"\t\t\t\tbase.{member.Name}({argList});");
			}
			else
			{
				sb.AppendLine($"\t\t\t\treturn base.{member.Name}({argList});");
			}
		}

		sb.AppendLine("\t\t\t}");
		sb.AppendLine();
	}

	/// <summary>
	/// Generates an event override.
	/// </summary>
	private static void GenerateClassEventOverride(
		System.Text.StringBuilder sb,
		EventMemberInfo evt,
		string className)
	{
		sb.AppendLine($"\t\t\t/// <inheritdoc />");
		sb.AppendLine($"\t\t\tpublic override event {evt.FullDelegateTypeName}? {evt.Name}");
		sb.AppendLine("\t\t\t{");
		sb.AppendLine($"\t\t\t\tadd => Interceptor.{evt.Name}.RecordAdd(value);");
		sb.AppendLine($"\t\t\t\tremove => Interceptor.{evt.Name}.RecordRemove(value);");
		sb.AppendLine("\t\t\t}");
		sb.AppendLine();
	}

	#endregion

	/// <summary>
	/// Generates explicit property implementation for inline stubs.
	/// </summary>
	private static void GenerateInlineStubPropertyImplementation(
		System.Text.StringBuilder sb,
		string interfaceFullName,
		InterfaceMemberInfo member,
		string stubClassName)
	{
		sb.AppendLine($"\t\t\t{member.ReturnType} {interfaceFullName}.{member.Name}");
		sb.AppendLine("\t\t\t{");

		if (member.HasGetter)
		{
			sb.AppendLine("\t\t\t\tget");
			sb.AppendLine("\t\t\t\t{");
			sb.AppendLine($"\t\t\t\t\t{member.Name}.RecordGet();");
			sb.AppendLine($"\t\t\t\t\tif ({member.Name}.OnGet is {{ }} onGet) return onGet(this);");
			sb.AppendLine($"\t\t\t\t\treturn {member.Name}.Value;");
			sb.AppendLine("\t\t\t\t}");
		}

		if (member.HasSetter)
		{
			sb.AppendLine("\t\t\t\tset");
			sb.AppendLine("\t\t\t\t{");
			sb.AppendLine($"\t\t\t\t\t{member.Name}.RecordSet(value);");
			sb.AppendLine($"\t\t\t\t\tif ({member.Name}.OnSet is {{ }} onSet) onSet(this, value);");
			sb.AppendLine($"\t\t\t\t\telse {member.Name}.Value = value;");
			sb.AppendLine("\t\t\t\t}");
		}

		sb.AppendLine("\t\t\t}");
		sb.AppendLine();
	}

	/// <summary>
	/// Generates explicit indexer implementation for inline stubs.
	/// </summary>
	private static void GenerateInlineStubIndexerImplementation(
		System.Text.StringBuilder sb,
		string interfaceFullName,
		InterfaceMemberInfo member,
		string stubClassName)
	{
		var paramList = string.Join(", ", member.IndexerParameters.Select(p => $"{p.Type} {p.Name}"));
		var argList = string.Join(", ", member.IndexerParameters.Select(p => p.Name));

		sb.AppendLine($"\t\t\t{member.ReturnType} {interfaceFullName}.this[{paramList}]");
		sb.AppendLine("\t\t\t{");

		if (member.HasGetter)
		{
			sb.AppendLine("\t\t\t\tget");
			sb.AppendLine("\t\t\t\t{");
			sb.AppendLine($"\t\t\t\t\t{member.Name}.RecordGet({argList});");
			sb.AppendLine($"\t\t\t\t\tif ({member.Name}.OnGet is {{ }} onGet) return onGet(this, {argList});");
			sb.AppendLine($"\t\t\t\t\treturn default!;");
			sb.AppendLine("\t\t\t\t}");
		}

		if (member.HasSetter)
		{
			sb.AppendLine("\t\t\t\tset");
			sb.AppendLine("\t\t\t\t{");
			sb.AppendLine($"\t\t\t\t\t{member.Name}.RecordSet({argList}, value);");
			sb.AppendLine($"\t\t\t\t\tif ({member.Name}.OnSet is {{ }} onSet) onSet(this, {argList}, value);");
			sb.AppendLine("\t\t\t\t}");
		}

		sb.AppendLine("\t\t\t}");
		sb.AppendLine();
	}

	/// <summary>
	/// Generates explicit method implementation for inline stubs.
	/// </summary>
	private static void GenerateInlineStubMethodImplementation(
		System.Text.StringBuilder sb,
		string interfaceFullName,
		InterfaceMemberInfo member,
		MethodGroupInfo group,
		string stubClassName)
	{
		var paramList = string.Join(", ", member.Parameters.Select(p => FormatParameter(p)));
		var inputParams = GetInputParameters(member.Parameters).ToArray();
		var argList = string.Join(", ", inputParams.Select(p => p.Name));
		var isVoid = member.ReturnType == "void";

		// Check for async types
		var isTask = member.ReturnType.StartsWith("global::System.Threading.Tasks.Task");
		var isValueTask = member.ReturnType.StartsWith("global::System.Threading.Tasks.ValueTask");
		var isAsync = isTask || isValueTask;

		sb.AppendLine($"\t\t\t{member.ReturnType} {interfaceFullName}.{member.Name}({paramList})");
		sb.AppendLine("\t\t\t{");

		// Record the call
		var recordArgs = inputParams.Length > 0 ? argList : "";
		sb.AppendLine($"\t\t\t\t{group.Name}.RecordCall({recordArgs});");

		// Call OnCall if set
		var onCallArgs = inputParams.Length > 0 ? $"this, {argList}" : "this";
		if (isVoid)
		{
			sb.AppendLine($"\t\t\t\tif ({group.Name}.OnCall is {{ }} onCall) onCall({onCallArgs});");
		}
		else if (isAsync)
		{
			sb.AppendLine($"\t\t\t\tif ({group.Name}.OnCall is {{ }} onCall) return onCall({onCallArgs});");
			// Return completed task for async methods
			if (isTask && member.ReturnType == "global::System.Threading.Tasks.Task")
			{
				sb.AppendLine("\t\t\t\treturn global::System.Threading.Tasks.Task.CompletedTask;");
			}
			else if (isValueTask && member.ReturnType == "global::System.Threading.Tasks.ValueTask")
			{
				sb.AppendLine("\t\t\t\treturn default;");
			}
			else
			{
				// Task<T> or ValueTask<T>
				sb.AppendLine("\t\t\t\treturn default!;");
			}
		}
		else
		{
			sb.AppendLine($"\t\t\t\tif ({group.Name}.OnCall is {{ }} onCall) return onCall({onCallArgs});");
			sb.AppendLine("\t\t\t\treturn default!;");
		}

		sb.AppendLine("\t\t\t}");
		sb.AppendLine();
	}

	/// <summary>
	/// Generates explicit event implementation for inline stubs.
	/// </summary>
	private static void GenerateInlineStubEventImplementation(
		System.Text.StringBuilder sb,
		string interfaceFullName,
		EventMemberInfo evt,
		string stubClassName)
	{
		sb.AppendLine($"\t\t\tevent {evt.FullDelegateTypeName}? {interfaceFullName}.{evt.Name}");
		sb.AppendLine("\t\t\t{");
		sb.AppendLine($"\t\t\t\tadd => {evt.Name}Interceptor.RecordAdd(value);");
		sb.AppendLine($"\t\t\t\tremove => {evt.Name}Interceptor.RecordRemove(value);");
		sb.AppendLine("\t\t\t}");
		sb.AppendLine();
	}

	#endregion
}

#region Transform Model Types (Equatable for Incremental Generation)

internal sealed record KnockOffTypeInfo(
	string Namespace,
	string ClassName,
	EquatableArray<ContainingTypeInfo> ContainingTypes,
	EquatableArray<InterfaceInfo> Interfaces,
	EquatableArray<UserMethodInfo> UserMethods) : IEquatable<KnockOffTypeInfo>;

/// <summary>
/// Represents a containing type (for nested class support).
/// </summary>
internal sealed record ContainingTypeInfo(
	string Name,
	string Keyword,
	string AccessibilityModifier) : IEquatable<ContainingTypeInfo>;

internal sealed record InterfaceInfo(
	string FullName,
	string Name,
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
	DefaultValueStrategy DefaultStrategy,
	string? ConcreteTypeForNew,
	EquatableArray<ParameterInfo> Parameters,
	EquatableArray<ParameterInfo> IndexerParameters,
	bool IsGenericMethod,
	EquatableArray<TypeParameterInfo> TypeParameters) : IEquatable<InterfaceMemberInfo>;

internal sealed record ParameterInfo(
	string Name,
	string Type,
	RefKind RefKind) : IEquatable<ParameterInfo>;

/// <summary>
/// Represents a type parameter for generic methods (e.g., T in Method&lt;T&gt;).
/// </summary>
internal sealed record TypeParameterInfo(
	string Name,
	EquatableArray<string> Constraints) : IEquatable<TypeParameterInfo>;

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
/// Strategy for generating default return values when no callback/user method is provided.
/// </summary>
internal enum DefaultValueStrategy
{
	/// <summary>Use default/default! - for value types and nullable reference types.</summary>
	Default,
	/// <summary>Use new T() - for non-nullable reference types with parameterless constructor.</summary>
	NewInstance,
	/// <summary>Throw exception - no safe default available.</summary>
	ThrowException
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
	EquatableArray<ParameterInfo> Parameters,
	bool IsGenericMethod,
	EquatableArray<TypeParameterInfo> TypeParameters) : IEquatable<MethodOverloadInfo>;

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

#region Inline Stub Model Types

/// <summary>
/// Info for a diagnostic to report.
/// </summary>
internal sealed record DiagnosticInfo(
	string Id,
	string FilePath,
	int Line,
	int Column,
	string[] Args) : IEquatable<DiagnosticInfo>;

/// <summary>
/// Info for all [KnockOff&lt;T&gt;] attributes on a single class.
/// </summary>
internal sealed record InlineStubClassInfo(
	string Namespace,
	string ClassName,
	EquatableArray<ContainingTypeInfo> ContainingTypes,
	EquatableArray<InterfaceInfo> Interfaces,
	EquatableArray<DelegateInfo> Delegates,
	EquatableArray<ClassStubInfo> Classes,
	EquatableArray<PartialPropertyInfo> PartialProperties,
	EquatableArray<DiagnosticInfo> Diagnostics) : IEquatable<InlineStubClassInfo>;

/// <summary>
/// Info about a partial property that should be auto-implemented to return a stub instance.
/// </summary>
internal sealed record PartialPropertyInfo(
	string PropertyName,
	string StubTypeName,
	string AccessModifier,
	bool HasGetter,
	bool HasSetter) : IEquatable<PartialPropertyInfo>;

/// <summary>
/// Info about a delegate type for generating inline delegate stubs.
/// </summary>
internal sealed record DelegateInfo(
	string FullName,
	string Name,
	string ReturnType,
	bool IsVoid,
	EquatableArray<ParameterInfo> Parameters) : IEquatable<DelegateInfo>;

#endregion

#region Class Stub Model Types

/// <summary>
/// Info about a class type for generating class stubs via inheritance.
/// </summary>
internal sealed record ClassStubInfo(
	string FullName,
	string Name,
	EquatableArray<ClassMemberInfo> Members,
	EquatableArray<ClassConstructorInfo> Constructors,
	EquatableArray<EventMemberInfo> Events) : IEquatable<ClassStubInfo>;

/// <summary>
/// Info about a virtual/abstract member of a class for stubbing.
/// Reuses InterfaceMemberInfo structure but tracks whether member is abstract.
/// </summary>
internal sealed record ClassMemberInfo(
	string Name,
	string ReturnType,
	bool IsProperty,
	bool IsIndexer,
	bool HasGetter,
	bool HasSetter,
	bool IsNullable,
	DefaultValueStrategy DefaultStrategy,
	string? ConcreteTypeForNew,
	EquatableArray<ParameterInfo> Parameters,
	EquatableArray<ParameterInfo> IndexerParameters,
	bool IsGenericMethod,
	EquatableArray<TypeParameterInfo> TypeParameters,
	bool IsAbstract,
	string AccessModifier) : IEquatable<ClassMemberInfo>;

/// <summary>
/// Info about an accessible constructor of a class.
/// </summary>
internal sealed record ClassConstructorInfo(
	EquatableArray<ParameterInfo> Parameters,
	string AccessModifier) : IEquatable<ClassConstructorInfo>;

/// <summary>
/// Groups class methods by name for overload handling.
/// This is a mutable intermediate type used during generation.
/// </summary>
internal sealed class ClassMethodGroupInfo
{
	public string Name { get; }
	public string ReturnType { get; }
	public bool IsVoid { get; }
	public bool IsNullable { get; }
	public EquatableArray<ClassMemberInfo> Members { get; }

	// Constructor for initial creation with List
	public ClassMethodGroupInfo(string name, string returnType, bool isVoid, bool isNullable, IReadOnlyCollection<ClassMemberInfo> members)
	{
		Name = name;
		ReturnType = returnType;
		IsVoid = isVoid;
		IsNullable = isNullable;
		Members = members is List<ClassMemberInfo> list
			? new EquatableArray<ClassMemberInfo>(list.ToArray())
			: new EquatableArray<ClassMemberInfo>(members.ToArray());
	}
}

#endregion
