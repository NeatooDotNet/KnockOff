// -----------------------------------------------------------------------------
// Design.Stubs - Standalone Indexer Stubs for Multi-Param and Init-Only
// -----------------------------------------------------------------------------
// These standalone stubs verify Fix #2 (FlatModelBuilder multi-param) and
// Fix #1 (init-only indexer accessor) for standalone patterns.
// Related plan: docs/plans/fix-indexer-gaps.md
// -----------------------------------------------------------------------------

using Design.Domain.Abstractions;
using Design.Domain.Entities;
using KnockOff;

namespace Design.Stubs.Indexers;

// =============================================================================
// Standalone stubs for multi-param indexer (Fix #2)
// =============================================================================

/// <summary>
/// Standalone stub for IMatrix (multi-param indexer: this[int row, int col]).
/// Verifies FlatModelBuilder correctly extracts all indexer parameters.
/// </summary>
[KnockOff]
public partial class MatrixStandaloneStub : IMatrix
{
}

// =============================================================================
// Standalone stubs for init-only indexer (Fix #1)
// =============================================================================

/// <summary>
/// Standalone stub for IInitIndexerCollection (indexer with get; init;).
/// Verifies FlatModelBuilder propagates IsInitOnly and FlatRenderer emits 'init'.
/// </summary>
[KnockOff]
public partial class InitIndexerStandaloneStub : IInitIndexerCollection<string, int>
{
}

// =============================================================================
// Class stubs for init-only indexer (Bug #35)
// =============================================================================
// Without the fix, these produce CS8853: "must match by init-only of overridden member"
// because the generator emitted 'set' instead of 'init' in the Impl class override.

/// <summary>
/// Inline class stub for InitIndexerBase (abstract class with get/init indexer).
/// Verifies ClassRenderer emits 'init' instead of 'set' for init-only indexer overrides.
///
/// GENERATOR BEHAVIOR: The Impl class override must emit:
///   public override int this[string key]
///   {
///       get { ... }
///       init { ... }   // NOT 'set' -- CS8853 if 'set'
///   }
/// </summary>
[KnockOff<InitIndexerBase>]
[KnockOff<VirtualInitIndexerBase>]
public partial class InitIndexerClassStubDemo
{
}

/// <summary>
/// Standalone class stub for InitIndexerBase (abstract class with get/init indexer).
/// Verifies StandaloneClassRenderer emits 'init' instead of 'set'.
/// </summary>
[KnockOffBase<InitIndexerBase>]
public partial class InitIndexerStandaloneClassStub
{
}

/// <summary>
/// Standalone class stub for VirtualInitIndexerBase (virtual class with get/init indexer).
/// Verifies StandaloneClassRenderer emits 'init' instead of 'set'.
/// </summary>
[KnockOffBase<VirtualInitIndexerBase>]
public partial class VirtualInitIndexerStandaloneClassStub
{
}
