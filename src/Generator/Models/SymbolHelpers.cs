// Helper methods for extracting information from Roslyn symbols
#nullable enable
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace KnockOff;

/// <summary>
/// Shared helper methods for working with Roslyn symbols during model extraction.
/// </summary>
internal static class SymbolHelpers
{
	/// <summary>
	/// Display format that includes nullability annotations and fully qualified names.
	/// </summary>
	public static readonly SymbolDisplayFormat FullyQualifiedWithNullability = new(
		globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
		typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
		genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
		miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers
			| SymbolDisplayMiscellaneousOptions.UseSpecialTypes
			| SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

	/// <summary>
	/// Determines the default value strategy for a return type.
	/// </summary>
	public static DefaultValueStrategy GetDefaultValueStrategy(ITypeSymbol type) =>
		GetDefaultValueStrategyWithConcreteType(type).Strategy;

	/// <summary>
	/// Determines the default value strategy and concrete type for a return type.
	/// For collection interfaces, returns the concrete implementation type.
	/// </summary>
	public static (DefaultValueStrategy Strategy, string? ConcreteType) GetDefaultValueStrategyWithConcreteType(ITypeSymbol type)
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
	public static string? GetCollectionInterfaceMapping(INamedTypeSymbol interfaceType)
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

	/// <summary>
	/// Gets a simple type name for use in indexer naming (e.g., "String" from "global::System.String")
	/// </summary>
	public static string GetSimpleTypeName(ITypeSymbol type)
	{
		// Use the simple name, capitalize first letter
		var name = type.Name;
		if (string.IsNullOrEmpty(name))
			return "Unknown";
		return char.ToUpperInvariant(name[0]) + name.Substring(1);
	}

	/// <summary>
	/// Extracts constraint strings from a type parameter symbol.
	/// </summary>
	public static IEnumerable<string> GetTypeParameterConstraints(ITypeParameterSymbol tp)
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
	/// Classify the delegate type for code generation
	/// </summary>
	public static EventDelegateKind ClassifyDelegateKind(INamedTypeSymbol delegateType)
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
	public static bool IsAsyncDelegate(IMethodSymbol invokeMethod)
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
}
