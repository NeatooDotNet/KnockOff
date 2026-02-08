// -----------------------------------------------------------------------------
// Design.Stubs - Nullable unconstrained generic method verification
// Bug: Spurious `where TData : class` on nullable unconstrained generics (CS8665)
// Related: docs/todos/nullable-generic-spurious-class-constraint.md
// -----------------------------------------------------------------------------
// This file verifies that the generator handles nullable unconstrained type
// parameters correctly. In C# 9+, T? on an unconstrained type parameter means
// "default value" and does NOT require `where T : class`.
//
// If this file fails to compile with CS8665, the bug is present.
// -----------------------------------------------------------------------------

using Design.Domain.Services;
using KnockOff;

namespace Design.Stubs.Methods;

// =============================================================================
// PATTERN 1: STANDALONE - Nullable unconstrained generic methods
// =============================================================================

[KnockOff]
public partial class NullableGenericServiceStub : INullableGenericService
{
}

// =============================================================================
// PATTERN 5: INLINE INTERFACE - Nullable unconstrained generic methods
// =============================================================================

[KnockOff<INullableGenericService>]
public partial class NullableGenericInlineTests
{
}
