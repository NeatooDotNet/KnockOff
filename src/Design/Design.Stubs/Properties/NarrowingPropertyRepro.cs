// -----------------------------------------------------------------------------
// Design.Stubs - Repros and coverage for shadowed properties using C#'s `new`.
//
// DESIGN DECISION: When multiple `new`-shadowed property declarations share a
// name, the generated stub exposes ONE interceptor (interceptor-as-property
// principle). That interceptor's accessor set is the UNION of accessors across
// all shadowed declarations, so routing via any interface face works.
//
// Explicit interface implementations still use each declaration's own accessor
// set (C# requires this). Source(T) fallbacks still only bind to accessors
// declared on the source face.
//
// DID NOT DO THIS: Emit two interceptors (e.g., Prop_Narrow, Prop_Wide). That
// breaks user intuition and forces test code to know about shadowing.
// -----------------------------------------------------------------------------

using Design.Domain.Entities;
using KnockOff;

namespace Design.Stubs.Properties;

// Pattern 5 — Inline interface, narrow-first hierarchy (Narrow is get-only,
// Wide is get/set). Interceptor must be PropertyGetSetInterceptor<int>.
[KnockOff<IInterfaceNarrow>]
public partial class NarrowingInlineStub
{
}

[KnockOff<IInterfaceWide>]
public partial class WideInlineStub
{
}

// Pattern 1 — Standalone. Same hierarchy via FlatRenderer.
[KnockOff]
public partial class NarrowingStandaloneStub : IInterfaceNarrow
{
}

// Widening direction: base get-only, derived adds setter. Patterns 1 and 5.
[KnockOff<IGetSetFromGetOnly>]
public partial class WideningInlineStub
{
}

[KnockOff]
public partial class WideningStandaloneStub : IGetSetFromGetOnly
{
}

// Pattern 8 — Open generic interface (Inline pipeline via pattern 8).
[KnockOff(typeof(IShadowed<>))]
public partial class ShadowedOpenGenericInlineStub
{
}

// Pattern 2 — Generic standalone (Flat pipeline).
[KnockOff]
public partial class ShadowedGenericStandaloneStub<T> : IShadowed<T>
{
}
