// -----------------------------------------------------------------------------
// Design.Stubs - Standalone Class Stub Overrides
// -----------------------------------------------------------------------------
// This file documents stub override overrides for standalone class stubs
// (patterns 3 and 4). The underscore-suffix pattern works the same as
// interface stubs, but with class-specific behavior:
//
// - Stub override method completely replaces base.Method() call (no dual fallback)
// - Base class generates virtual _ methods for ALL target class methods
// - Each overload is handled independently (partial overload coverage)
//
// DESIGN DECISIONS:
// 1. Stub override method replaces base.Method() -- consistent with interface pipeline
//    where stub overrides replace Source/Strict as fallback, and with standalone
//    class property stub overrides which replace the base value entirely.
// 2. Interceptor-internal fallback: stub passed to Invoke(), interceptor
//    calls stub.Method_() when unconfigured.
// 3. Base class generates virtual _ methods for ALL target class methods,
//    not just user-overridden ones. Provides IntelliSense discoverability.
// 4. HasStubOverride on shared InlineClassImplMethodModel record.
// 5. No custom KnockOff diagnostic; CS0115 is sufficient for mismatches.
// 6. .When() chains supported on stub override interceptors.
//
// PRIORITY CHAIN (same as interface stubs):
//   When chains > Sequences > Returns/Execute > Stub Override
// -----------------------------------------------------------------------------

using Design.Domain.Abstractions;
using KnockOff;

namespace Design.Stubs.StubOverrides;

// =============================================================================
// PATTERN 3: STANDALONE CLASS WITH STUB OVERRIDE METHODS
// =============================================================================
//
// Use [KnockOffBase<T>] with protected override methods suffixed with
// underscore to provide default method behavior for class stubs.
//
// GENERATED BASE CLASS (StandaloneClassStubOverrideStubBase):
//   protected virtual void Execute_(string command) { }
//   protected virtual void Initialize_() { }
//
// The base class generates virtual _ methods for ALL target class methods,
// regardless of whether the stub overrides them. This allows adding overrides
// later without regeneration and provides IntelliSense discoverability.
//
// BEHAVIOR:
// - Abstract methods (Execute): stub override IS the default behavior
// - Virtual methods (Initialize): stub override REPLACES base.Initialize()
//   The stub override is the fallback, not a supplement to the base call.
// - Returns/Execute still supersede stub overrides per-test
// - .When() chains have highest priority
// =============================================================================

[KnockOffBase<ServiceBase>]
public partial class StandaloneClassStubOverrideStub
{
    // Override abstract method -- stub override provides default behavior
    // Without this, unconfigured calls use the interceptor's strict/default path
    protected override void Execute_(string command)
    {
        // User-defined default behavior for abstract method
    }

    // Override virtual method -- completely replaces base.Initialize()
    // When unconfigured, the interceptor calls this instead of base.Initialize()
    protected override void Initialize_()
    {
        // User-defined default behavior for virtual method
        // NOTE: This completely replaces base.Initialize() -- the stub override
        // IS the fallback, not a supplement to the base call.
    }
}

// =============================================================================
// PATTERN 4: GENERIC STANDALONE CLASS WITH STUB OVERRIDE METHODS
// =============================================================================
//
// Generic standalone class stubs use [KnockOffBase(typeof(T<>))] and support
// the same stub override override pattern. The generic type parameter flows
// through the base class, Impl class, and interceptors.
//
// GENERATED BASE CLASS (RepositoryStubOverrideStubBase<T>):
//   protected virtual T? GetById_(int id) => default!;
//   protected virtual void Save_(T entity) { }
//   protected virtual T? GetDefault_() => default!;
//   protected virtual T? GetDefault_(string filter) => default!;
//
// PARTIAL OVERLOAD COVERAGE:
// Each overload is handled independently. You can override GetDefault_()
// without overriding GetDefault_(string). The non-overridden overload uses
// the standard interceptor path (unconfigured-count + base call).
//
// This is different from interface stubs where all overloads share the same
// interceptor. For class stubs, each overload's Impl method independently
// decides whether to pass _stub to Invoke() based on HasStubOverride.
// =============================================================================

[KnockOffBase(typeof(RepositoryBase<>))]
public partial class RepositoryStubOverrideStub<T> where T : class
{
    // Virtual method returning generic type T? with stub override
    protected override T? GetById_(int id)
    {
        // User-defined default: return null for all lookups
        return default;
    }

    // Void virtual method with generic type T parameter
    protected override void Save_(T entity)
    {
        // User-defined default: no-op save
    }

    // Overloaded method -- only override the parameterless version
    // GetDefault() has two overloads: GetDefault() and GetDefault(string)
    // Only overriding the no-parameter version. The string overload uses the
    // standard interceptor path (unconfigured-count + base call).
    protected override T? GetDefault_()
    {
        // User-defined default for parameterless overload
        return default;
    }

    // NOTE: GetDefault_(string filter) is intentionally NOT overridden.
    // This demonstrates partial overload coverage: each overload is
    // independently handled. The non-overridden overload falls back to
    // base.GetDefault(filter) when unconfigured.
}
