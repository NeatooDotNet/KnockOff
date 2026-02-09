// -----------------------------------------------------------------------------
// Design.Stubs - Generic Type Gaps Verification
// -----------------------------------------------------------------------------
// This file provides compilation verification for all "Needs Verification"
// items in the generic-type-gaps plan. Each stub declaration exercises a
// specific pattern + feature combination.
//
// If these declarations compile, the generator handles that combination.
// If they fail, the compiler error becomes the acceptance criteria for the fix.
//
// See: docs/plans/generic-type-gaps.md
// See: docs/todos/generic-type-gaps.md
// -----------------------------------------------------------------------------

using Design.Domain.Abstractions;
using Design.Domain.Services;
using KnockOff;

namespace Design.Stubs.StubPatterns;

// =============================================================================
// GAP 1: P4 (Generic Standalone Class) with two type params
// =============================================================================
// Scope table item: Feature B, Pattern P4
// Expected: [KnockOffBase(typeof(CacheBase<,>))] generates a stub with
//           TKey and TValue type parameters and proper constraint propagation.
// =============================================================================

[KnockOffBase(typeof(CacheBase<,>))]
public partial class CacheStub<TKey, TValue> where TKey : notnull
{
}

// =============================================================================
// GAP 2: P2 (Generic Standalone) with method-level type params
// =============================================================================
// Scope table items: Feature D/E, Pattern P2
// Expected: Generic standalone stub where the interface has methods with
//           their own type parameters (alongside the interface-level T).
//           Method interceptors use Of<T>() pattern.
// =============================================================================

[KnockOff]
public partial class GenericTransformServiceStub<T> : IGenericTransformService<T> where T : class
{
}

// =============================================================================
// GAP 3: P8 (Open Generic Interface) with method-level type params
// =============================================================================
// Scope table items: Feature D/E, Pattern P8
// Expected: Open generic interface stub where the interface has methods with
//           their own type parameters. The generated nested stub class should
//           have Of<T>() handlers for generic methods.
// =============================================================================

[KnockOff(typeof(IGenericTransformService<>))]
#pragma warning disable CA1052 // Static holder types should be Static or NotInheritable
public partial class OpenGenericTransformServiceTest
#pragma warning restore CA1052
{
}

// =============================================================================
// GAP 4: P9 (Open Generic Class) with method-level type params AND two type params
// =============================================================================
// Scope table items: Feature D/E, Pattern P9 + Feature B
// Expected: Open generic class stub for CacheBase<TKey, TValue> generates
//           correctly with both class-level type params and method-level Transform<TResult>.
// =============================================================================

[KnockOff(typeof(CacheBase<,>))]
#pragma warning disable CA1052 // Static holder types should be Static or NotInheritable
public partial class OpenGenericCacheTest
#pragma warning restore CA1052
{
}
