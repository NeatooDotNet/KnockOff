// src/Generator/KnockOffGenerator.StandaloneClass.cs
// Transform and generation logic for standalone class stubs [KnockOffBase<T>]
#nullable enable
using KnockOff.Builder;
using KnockOff.Renderer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KnockOff;

public partial class KnockOffGenerator
{
	/// <summary>
	/// Transform: extract class info for standalone class stubs.
	/// Handles both [KnockOffBase&lt;T&gt;] and [KnockOffBase(typeof(T&lt;&gt;))] patterns.
	/// </summary>
	private static StandaloneClassStubInfo? TransformStandaloneClassStub(GeneratorAttributeSyntaxContext context, AssemblyConfiguration assemblyConfig)
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

		// Get attribute location for diagnostics
		var attributeData = context.Attributes.FirstOrDefault();
		var attrLocation = attributeData?.ApplicationSyntaxReference?.GetSyntax()?.GetLocation();
		var attrLineSpan = attrLocation?.GetLineSpan() ?? default;

		// Extract target class type from attribute
		INamedTypeSymbol? targetClassType = null;
		bool isOpenGeneric = false;
		EquatableArray<TypeParameterInfo> openGenericTypeParams = default;

		// Path 1: Generic attribute [KnockOffBase<T>] - extract from TypeArguments
		if (attributeData?.AttributeClass is INamedTypeSymbol attrType && attrType.IsGenericType)
		{
			if (attrType.TypeArguments.FirstOrDefault() is INamedTypeSymbol namedTypeArg)
			{
				targetClassType = namedTypeArg;
			}
		}
		// Path 2: Non-generic attribute [KnockOffBase(typeof(T<>))] - extract from ConstructorArguments
		else if (attributeData?.ConstructorArguments.Length > 0
			&& attributeData.ConstructorArguments[0].Value is INamedTypeSymbol ctorTypeArg)
		{
			targetClassType = ctorTypeArg;

			// Check if this is an open generic (unbound) type
			if (ctorTypeArg.IsUnboundGenericType)
			{
				isOpenGeneric = true;
				openGenericTypeParams = SymbolHelpers.ExtractTypeParameters(ctorTypeArg.TypeParameters);
			}
		}

		if (targetClassType is null)
			return null;

		// Validate target is a class (not interface or delegate)
		if (targetClassType.TypeKind != TypeKind.Class)
		{
			diagnostics.Add(new DiagnosticInfo(
				"KO1001",
				filePath,
				attrLineSpan.StartLinePosition.Line,
				attrLineSpan.StartLinePosition.Character,
				new[] { targetClassType.ToDisplayString() + " must be a class type for [KnockOffBase<T>]" }));
			return null;
		}

		// Extract Strict property from attribute
		var attributeStrict = attributeData is not null ? ExtractStrictFromAttributeData(attributeData) : null;
		var strict = attributeStrict ?? assemblyConfig.DefaultStrict;

		// Extract user class type parameters for generic standalone stubs
		var userClassTypeParameters = SymbolHelpers.ExtractTypeParameters(classSymbol.TypeParameters);

		// Validate type parameter arity matches for open generic (KO2007)
		if (isOpenGeneric && userClassTypeParameters.Count != openGenericTypeParams.Count)
		{
			var location = classDeclaration.Identifier.GetLocation();
			var lineSpan = location.GetLineSpan();
			diagnostics.Add(new DiagnosticInfo(
				"KO2007",
				filePath,
				lineSpan.StartLinePosition.Line,
				lineSpan.StartLinePosition.Character,
				new[] {
					classSymbol.Name,
					userClassTypeParameters.Count.ToString(),
					targetClassType.Name,
					openGenericTypeParams.Count.ToString()
				}));

			return new StandaloneClassStubInfo(
				Namespace: namespaceName,
				ClassName: classSymbol.Name,
				ContainingTypes: containingTypes,
				TypeParameters: userClassTypeParameters,
				TargetClassInfo: null,
				Diagnostics: new EquatableArray<DiagnosticInfo>(diagnostics.ToArray()),
				Strict: strict,
				UserOverrideProperties: default,
				UserOverrideMethods: default);
		}

		// For non-open-generic closed generic targets (e.g., [KnockOffBase<ServiceBase>]),
		// user class should NOT have type parameters
		if (!isOpenGeneric && userClassTypeParameters.Count > 0)
		{
			// This is actually valid - user might want a generic stub that wraps a non-generic class
			// But if the target is a closed generic, we need to check arity
			if (targetClassType.IsGenericType && !targetClassType.IsUnboundGenericType &&
				userClassTypeParameters.Count != targetClassType.TypeParameters.Length)
			{
				var location = classDeclaration.Identifier.GetLocation();
				var lineSpan = location.GetLineSpan();
				diagnostics.Add(new DiagnosticInfo(
					"KO2007",
					filePath,
					lineSpan.StartLinePosition.Line,
					lineSpan.StartLinePosition.Character,
					new[] {
						classSymbol.Name,
						userClassTypeParameters.Count.ToString(),
						targetClassType.Name,
						targetClassType.TypeParameters.Length.ToString()
					}));

				return new StandaloneClassStubInfo(
					Namespace: namespaceName,
					ClassName: classSymbol.Name,
					ContainingTypes: containingTypes,
					TypeParameters: userClassTypeParameters,
					TargetClassInfo: null,
					Diagnostics: new EquatableArray<DiagnosticInfo>(diagnostics.ToArray()),
					Strict: strict,
					UserOverrideProperties: default,
					UserOverrideMethods: default);
			}
		}

		// Extract class info (reuse existing method from inline class stubs)
		var classSource = isOpenGeneric ? targetClassType.OriginalDefinition : targetClassType;
		var knockOffAssembly = classSymbol.ContainingAssembly;
		var targetClassInfo = ExtractClassInfo(
			classSource,
			targetClassType,
			knockOffAssembly,
			filePath,
			attrLineSpan,
			diagnostics,
			isOpenGeneric,
			isOpenGeneric ? openGenericTypeParams : userClassTypeParameters);

		// Detect user-defined property overrides (base class pattern)
		var userOverrideProperties = DetectUserOverrideProperties(classSymbol);
		var userOverridePropertiesArray = new EquatableArray<string>(userOverrideProperties.ToArray());

		// Detect user-defined method overrides (base class pattern)
		var userOverrideMethods = DetectUserOverrideMethods(classSymbol, context.SemanticModel.Compilation);
		var userOverrideMethodsArray = new EquatableArray<string>(userOverrideMethods.ToArray());

		return new StandaloneClassStubInfo(
			Namespace: namespaceName,
			ClassName: classSymbol.Name,
			ContainingTypes: containingTypes,
			TypeParameters: userClassTypeParameters,
			TargetClassInfo: targetClassInfo,
			Diagnostics: new EquatableArray<DiagnosticInfo>(diagnostics.ToArray()),
			Strict: strict,
			UserOverrideProperties: userOverridePropertiesArray,
			UserOverrideMethods: userOverrideMethodsArray);
	}

	/// <summary>
	/// Generate the standalone class stub.
	/// </summary>
	private static void GenerateStandaloneClassStub(SourceProductionContext context, StandaloneClassStubInfo info)
	{
		// Report diagnostics first
		ReportDiagnostics(context, info.Diagnostics);

		// Skip generation if there's no target class info (e.g., due to errors)
		if (info.TargetClassInfo is null)
			return;

		// Build the model
		var model = StandaloneClassModelBuilder.Build(info);

		// Build hint name including containing types to ensure uniqueness
		var className = info.TypeParameters.Count > 0
			? $"{info.ClassName}`{info.TypeParameters.Count}"
			: info.ClassName;
		var hintName = info.ContainingTypes.Count > 0
			? string.Join(".", info.ContainingTypes.Select(ct => ct.Name)) + "." + className
			: className;

		// Generate base class file first (contains virtual protected properties for user overrides)
		var baseClassSource = StandaloneClassRenderer.RenderBaseClass(model);
		context.AddSource($"{hintName}.Base.g.cs", baseClassSource);

		// Generate main partial class file (extends base class, provides interceptors)
		var source = StandaloneClassRenderer.Render(model);
		context.AddSource($"{hintName}.g.cs", source);
	}
}

/// <summary>
/// Info about a standalone class stub to generate.
/// </summary>
internal sealed record StandaloneClassStubInfo(
	/// <summary>The namespace of the user's stub class.</summary>
	string Namespace,
	/// <summary>The user's stub class name.</summary>
	string ClassName,
	/// <summary>Containing types chain for nested class support.</summary>
	EquatableArray<ContainingTypeInfo> ContainingTypes,
	/// <summary>Type parameters of the user's stub class.</summary>
	EquatableArray<TypeParameterInfo> TypeParameters,
	/// <summary>Info about the target class to stub, or null if there were errors.</summary>
	ClassStubInfo? TargetClassInfo,
	/// <summary>Diagnostics collected during transformation.</summary>
	EquatableArray<DiagnosticInfo> Diagnostics,
	/// <summary>Whether strict mode is enabled.</summary>
	bool Strict,
	/// <summary>Property names (with _ suffix) that have user overrides in the partial class.</summary>
	EquatableArray<string> UserOverrideProperties = default,
	/// <summary>Method signature keys (format: "MethodName_(ParamType1,...)") that have user overrides in the partial class.</summary>
	EquatableArray<string> UserOverrideMethods = default) : IEquatable<StandaloneClassStubInfo>;
