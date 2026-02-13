// -----------------------------------------------------------------------------
// Design.Stubs - Nullable unconstrained generic method support for class stubs
// Bug: CS0453 — generator emits Nullable<TData> for unconstrained type params
// Fix: Strip ? from unconstrained type params, wrap with #nullable disable
// -----------------------------------------------------------------------------
// This file verifies that the generator handles nullable unconstrained type
// parameters correctly in class stubs (patterns 3, 6). In C# 9+, T? on an
// unconstrained type parameter means "default value" and is NOT Nullable<T>.
//
// If this file fails to compile with CS0453, the bug is present.
// -----------------------------------------------------------------------------

using Design.Domain.Abstractions;
using KnockOff;

namespace Design.Stubs.Methods;

// =============================================================================
// PATTERN 6: INLINE CLASS - Abstract base with nullable unconstrained generics
// =============================================================================

[KnockOff<NullableGenericBase>]
#pragma warning disable CA1052 // Static holder types should be Static or NotInheritable
public partial class NullableGenericAbstractInlineTest
#pragma warning restore CA1052
{
}

// =============================================================================
// PATTERN 6: INLINE CLASS - Virtual base with nullable unconstrained generics
// =============================================================================

[KnockOff<NullableGenericVirtual>]
#pragma warning disable CA1052 // Static holder types should be Static or NotInheritable
public partial class NullableGenericVirtualInlineTest
#pragma warning restore CA1052
{
}

// =============================================================================
// PATTERN 3: STANDALONE CLASS - Abstract base with nullable unconstrained generics
// =============================================================================

[KnockOffBase<NullableGenericBase>]
public partial class NullableGenericAbstractStandaloneStub
{
}

// =============================================================================
// PATTERN 3: STANDALONE CLASS - Virtual base with nullable unconstrained generics
// =============================================================================

[KnockOffBase<NullableGenericVirtual>]
public partial class NullableGenericVirtualStandaloneStub
{
}
