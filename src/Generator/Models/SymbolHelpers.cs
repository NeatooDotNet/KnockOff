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
	/// Computes the final indexer name based on whether there are multiple indexers.
	/// - Single indexer: "Indexer"
	/// - Multiple indexers: "Indexer{TypeSuffix}" (e.g., "IndexerString", "IndexerInt32")
	/// </summary>
	/// <param name="indexerCount">Total number of indexers in the containing type.</param>
	/// <param name="typeSuffix">The type suffix for this specific indexer (e.g., "String").</param>
	/// <returns>The computed indexer name.</returns>
	public static string GetIndexerName(int indexerCount, string? typeSuffix)
	{
		if (indexerCount == 1)
			return "Indexer";

		// Multiple indexers - use type suffix
		return "Indexer" + (typeSuffix ?? "Unknown");
	}

	/// <summary>
	/// Counts the number of indexers in a collection of interface members.
	/// </summary>
	public static int CountIndexers(EquatableArray<InterfaceMemberInfo> members)
	{
		var arr = members.GetArray();
		if (arr is null) return 0;
		return arr.Count(m => m.IsIndexer);
	}

	/// <summary>
	/// Counts the number of indexers in a collection of class members.
	/// </summary>
	public static int CountClassIndexers(EquatableArray<ClassMemberInfo> members)
	{
		var arr = members.GetArray();
		if (arr is null) return 0;
		return arr.Count(m => m.IsIndexer);
	}

	/// <summary>
	/// Gets a type suffix for generic type arguments to disambiguate collision scenarios.
	/// Examples:
	/// - IList&lt;string&gt; → "String"
	/// - IList&lt;int&gt; → "Int32"
	/// - IDictionary&lt;string, int&gt; → "StringInt32"
	/// - IList&lt;string[]&gt; → "StringArray"
	/// - IList&lt;int?&gt; → "NullableInt32"
	/// </summary>
	public static string GetTypeSuffix(ITypeSymbol type)
	{
		return type switch
		{
			IArrayTypeSymbol array =>
				GetTypeSuffix(array.ElementType) + "Array",

			// Check for Nullable<T> using OriginalDefinition comparison
			INamedTypeSymbol named when IsNullableValueType(named) =>
				"Nullable" + GetTypeSuffix(named.TypeArguments[0]),

			INamedTypeSymbol { IsTupleType: true } tuple =>
				"ValueTuple" + string.Concat(tuple.TupleElements.Select(e => GetTypeSuffix(e.Type))),

			INamedTypeSymbol named when named.IsGenericType =>
				GetSimpleTypeName(named) + string.Concat(named.TypeArguments.Select(GetTypeSuffix)),

			_ => GetSimpleTypeName(type)
		};
	}

	/// <summary>
	/// Checks if a type is a nullable value type (e.g., int?, DateTime?).
	/// </summary>
	private static bool IsNullableValueType(INamedTypeSymbol type)
	{
		return type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;
	}

	/// <summary>
	/// Gets a combined type suffix for all type arguments of a generic type.
	/// Returns empty string for non-generic types.
	/// </summary>
	public static string GetTypeArgumentsSuffix(INamedTypeSymbol type)
	{
		if (!type.IsGenericType || type.TypeArguments.Length == 0)
			return "";

		return string.Concat(type.TypeArguments.Select(GetTypeSuffix));
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

	/// <summary>
	/// Formats a type parameter list string (e.g., "&lt;T, U&gt;") from type parameter info.
	/// Returns empty string if no type parameters.
	/// </summary>
	public static string FormatTypeParameterList(EquatableArray<TypeParameterInfo> typeParameters)
	{
		if (typeParameters.Count == 0)
			return "";

		return $"<{string.Join(", ", typeParameters.Select(tp => tp.Name))}>";
	}

	/// <summary>
	/// Formats type constraint clauses (e.g., "where T : class where U : new()") from type parameter info.
	/// Returns empty string if no constraints.
	/// </summary>
	public static string FormatTypeConstraints(EquatableArray<TypeParameterInfo> typeParameters)
	{
		if (typeParameters.Count == 0)
			return "";

		var clauses = new List<string>();
		foreach (var tp in typeParameters)
		{
			if (tp.Constraints.Count > 0)
			{
				clauses.Add($"where {tp.Name} : {string.Join(", ", tp.Constraints)}");
			}
		}

		return clauses.Count > 0 ? string.Join(" ", clauses) : "";
	}

	/// <summary>
	/// Extracts TypeParameterInfo from a type symbol's type parameters.
	/// </summary>
	public static EquatableArray<TypeParameterInfo> ExtractTypeParameters(IEnumerable<ITypeParameterSymbol> typeParams)
	{
		var result = new List<TypeParameterInfo>();
		foreach (var tp in typeParams)
		{
			var constraints = GetTypeParameterConstraints(tp).ToArray();
			result.Add(new TypeParameterInfo(tp.Name, new EquatableArray<string>(constraints)));
		}
		return new EquatableArray<TypeParameterInfo>(result.ToArray());
	}
}
