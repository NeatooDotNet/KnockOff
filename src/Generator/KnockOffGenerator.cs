using KnockOff.Builder;
using KnockOff.Renderer;
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
		// Pipeline 1: Explicit [KnockOff] pattern (class implements interfaces) - standalone stubs
		// Only triggers when [KnockOff] has no constructor arguments (standalone pattern)
		var classesToGenerate = context.SyntaxProvider.ForAttributeWithMetadataName(
			"KnockOff.KnockOffAttribute",
			predicate: static (node, _) => IsCandidateClass(node) && !HasTypeofArgument(node),
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

		// Pipeline 3: Inline [KnockOff(typeof(T))] pattern for open generic stubs
		// Triggers when [KnockOff] has a typeof constructor argument
		var openGenericStubsToGenerate = context.SyntaxProvider.ForAttributeWithMetadataName(
			"KnockOff.KnockOffAttribute",
			predicate: static (node, _) => IsInlineStubCandidate(node) && HasTypeofArgument(node),
			transform: static (ctx, _) => TransformInlineStubClass(ctx));

		context.RegisterSourceOutput(openGenericStubsToGenerate, static (spc, info) =>
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
	/// Checks if any [KnockOff] attribute on the class has a typeof() argument.
	/// Used to distinguish [KnockOff] standalone stubs from [KnockOff(typeof(T))] inline stubs.
	/// </summary>
	private static bool HasTypeofArgument(SyntaxNode node)
	{
		if (node is not ClassDeclarationSyntax classDecl)
			return false;

		// Check all attribute lists on the class
		foreach (var attrList in classDecl.AttributeLists)
		{
			foreach (var attr in attrList.Attributes)
			{
				// Check if this is a KnockOff attribute (without generic args)
				var name = attr.Name.ToString();
				if (name != "KnockOff" && name != "KnockOffAttribute")
					continue;

				// Check if it has an argument list with at least one argument
				if (attr.ArgumentList is { Arguments.Count: > 0 })
				{
					// Check if the first argument is a typeof expression
					var firstArg = attr.ArgumentList.Arguments[0];
					if (firstArg.Expression is TypeOfExpressionSyntax)
						return true;
				}
			}
		}

		return false;
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

		// Use the new FlatModelBuilder and FlatRenderer
		var unit = FlatModelBuilder.Build(typeInfo);
		var source = FlatRenderer.Render(unit);

		// Build hint name including containing types to ensure uniqueness
		// For generic classes, add arity suffix (e.g., RepositoryStub`1) to make valid filename
		var className = typeInfo.TypeParameters.Count > 0
			? $"{typeInfo.ClassName}`{typeInfo.TypeParameters.Count}"
			: typeInfo.ClassName;
		var hintName = typeInfo.ContainingTypes.Count > 0
			? string.Join(".", typeInfo.ContainingTypes.Select(ct => ct.Name)) + "." + className
			: className;

		context.AddSource($"{hintName}.g.cs", source);
	}
}
