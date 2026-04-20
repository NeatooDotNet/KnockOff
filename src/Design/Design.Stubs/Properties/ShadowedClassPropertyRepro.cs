// -----------------------------------------------------------------------------
// Design.Stubs - Repros for shadowed virtual properties in CLASS hierarchies
// (companion to NarrowingPropertyRepro.cs, which covers interfaces).
//
// Exercises class-based stub pipelines (patterns 3, 4, 6, 9) with `new virtual`
// to shadow a base virtual property with a different accessor set.
// -----------------------------------------------------------------------------

using Design.Domain.Abstractions;
using KnockOff;

namespace Design.Stubs.Properties;

// Pattern 3 — Standalone class stub over narrow-first hierarchy.
[KnockOffBase<NarrowClassDerived>]
public partial class NarrowClassStandaloneStub
{
}

// Pattern 3 — Widening direction (base get-only, derived adds setter).
[KnockOffBase<GetSetFromGetOnlyClass>]
public partial class WideningClassStandaloneStub
{
}

// Pattern 4 — Generic standalone class stub.
[KnockOffBase(typeof(ShadowedClassDerived<>))]
public partial class ShadowedGenericClassStandaloneStub<T>
    where T : class
{
}
