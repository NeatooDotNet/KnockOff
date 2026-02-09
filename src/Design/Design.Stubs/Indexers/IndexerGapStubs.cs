// -----------------------------------------------------------------------------
// Design.Stubs - Standalone Indexer Stubs for Multi-Param and Init-Only
// -----------------------------------------------------------------------------
// These standalone stubs verify Fix #2 (FlatModelBuilder multi-param) and
// Fix #1 (init-only indexer accessor) for standalone patterns.
// Related plan: docs/plans/fix-indexer-gaps.md
// -----------------------------------------------------------------------------

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
