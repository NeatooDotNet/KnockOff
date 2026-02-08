// -----------------------------------------------------------------------------
// Design.Domain - Ref Return Abstract Base Class
// -----------------------------------------------------------------------------
// Abstract base class with ref return members for testing class stub patterns.
// Used to verify KnockOff generates correct overrides for ref return members
// in the Impl class, including both abstract and virtual overrides.
// -----------------------------------------------------------------------------

namespace Design.Domain.Abstractions;

/// <summary>
/// Abstract base class with ref and ref readonly return members.
///
/// DESIGN DECISION: Class stubs for ref return members use different override
/// patterns depending on whether the member is abstract or virtual:
///
/// Abstract ref return members:
///   - Always use InvokeRef + return ref _refReturnBacking
///   - No base fallback (abstract has no base implementation)
///
/// Virtual ref return members:
///   - Use IsConfigured-first pattern (same as virtual property overrides)
///   - If configured: InvokeRef + return ref _refReturnBacking
///   - If not configured: track unconfigured call, return ref base.Member()
///
/// This differs from non-ref virtual method overrides which use the
/// UnconfiguredCallCount comparison pattern. The IsConfigured-first pattern
/// is necessary because ref locals cannot be conditionally reassigned.
/// </summary>
public abstract class RefReturnBase
{
	// =========================================================================
	// Abstract ref return members (no base fallback)
	// =========================================================================

	/// <summary>Abstract ref return method.</summary>
	public abstract ref int GetAbstractRef();

	/// <summary>Abstract ref readonly return method.</summary>
	public abstract ref readonly int GetAbstractRefReadonly();

	/// <summary>Abstract ref return property.</summary>
	public abstract ref int AbstractRefProperty { get; }

	/// <summary>Abstract ref return indexer.</summary>
	public abstract ref int this[int index] { get; }

	// =========================================================================
	// Virtual ref return members (base fallback when unconfigured)
	// =========================================================================

	private int _virtualMethodBacking = 42;

	/// <summary>
	/// Virtual ref return method with base implementation.
	/// When unconfigured, the stub should fall back to this base implementation.
	/// </summary>
	public virtual ref int GetVirtualRef() => ref _virtualMethodBacking;

	private int _virtualPropBacking = 77;

	/// <summary>
	/// Virtual ref return property with base implementation.
	/// When unconfigured, the stub should fall back to this base implementation.
	/// </summary>
	public virtual ref int VirtualRefProperty => ref _virtualPropBacking;

	// =========================================================================
	// Non-ref members (verify mixed generation works)
	// =========================================================================

	/// <summary>Non-ref abstract property to verify mixed generation.</summary>
	public abstract string Name { get; }

	/// <summary>Non-ref abstract method to verify mixed generation.</summary>
	public abstract void Execute(string command);
}
