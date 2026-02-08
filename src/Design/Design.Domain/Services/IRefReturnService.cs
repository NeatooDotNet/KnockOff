// -----------------------------------------------------------------------------
// Design.Domain - Ref Return Interfaces
// -----------------------------------------------------------------------------
// Interfaces with ref and ref readonly return types for methods, properties,
// and indexers. Used to verify KnockOff generates correct stubs for ref returns.
// -----------------------------------------------------------------------------

namespace Design.Domain.Services;

/// <summary>
/// Interface with ref return methods.
/// GENERATOR BEHAVIOR: The generator must emit ref/ref readonly prefixes on
/// the return type in explicit interface implementations, and use InvokeRef()
/// with a backing field instead of the normal Invoke() pattern.
/// </summary>
public interface IRefReturnService
{
	/// <summary>Ref return method with no parameters.</summary>
	ref int GetValueRef();

	/// <summary>Ref readonly return method with no parameters.</summary>
	ref readonly int GetValueRefReadonly();

	/// <summary>Ref return method with a parameter.</summary>
	ref string GetItemRef(int index);

	/// <summary>Ref return property (always get-only in C#).</summary>
	ref int RefValue { get; }

	/// <summary>Ref readonly return property.</summary>
	ref readonly int ReadonlyValue { get; }

	/// <summary>Ref return indexer (always get-only in C#).</summary>
	ref int this[int index] { get; }

	/// <summary>Non-ref method to verify mixed generation works.</summary>
	int GetNormal(int input);

	/// <summary>Non-ref property to verify mixed generation works.</summary>
	int NormalValue { get; set; }
}
