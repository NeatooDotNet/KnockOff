// -----------------------------------------------------------------------------
// Design.Stubs - Protected Member Access Modifier Verification
// -----------------------------------------------------------------------------
// ACCEPTANCE CRITERIA: This file must compile after the event AccessModifier
// fix is implemented. It verifies that class stubs correctly preserve access
// modifiers for protected virtual events.
//
// CURRENT BUG: The generator emits "public override event" for all events,
// regardless of the base class's access modifier. When the base class declares
// "protected virtual event EventHandler? InternalStateChanged", the generated
// Impl class produces "public override event EventHandler? InternalStateChanged",
// which causes CS0507: cannot change access modifiers when overriding.
//
// AFTER FIX: The generator should emit "protected override event" to match
// the base class access modifier.
//
// Patterns covered:
// - Pattern 3: Standalone Class   [KnockOffBase<ServiceBase>]
// - Pattern 6: Inline Class       [KnockOff<ServiceBase>]
// -----------------------------------------------------------------------------

using Design.Domain.Abstractions;
using KnockOff;

namespace Design.Stubs.ProtectedMembers;

// =============================================================================
// PATTERN 3: Standalone Class with protected members
// =============================================================================
// The existing StandaloneServiceStub in AllPatterns.cs uses [KnockOffBase<ServiceBase>].
// ServiceBase now has protected virtual event InternalStateChanged.
// If the generator emits "public override event" for this protected event,
// the generated code will fail with CS0507.
//
// This stub exercises the fix: it must compile, proving that the generator
// emits "protected override event" for protected events.
// =============================================================================

[KnockOffBase<ServiceBase>]
public partial class ProtectedMemberServiceStub
{
    // =========================================================================
    // ACCEPTANCE CRITERIA: Protected event interceptor accessible on wrapper
    // =========================================================================
    // The event interceptor (InternalStateChanged) should still be accessible
    // as a public property on the wrapper class, even though the Impl class
    // override is protected. The wrapper is not the base class -- it holds
    // interceptors with public access for test configuration.

    public void AcceptanceCriteria_ProtectedEvent_InterceptorAccessible()
    {
        var stub = new ProtectedMemberServiceStub();

        // Must compile: event interceptor on wrapper is public
        _ = stub.InternalStateChanged;
        _ = stub.InternalStateChanged.HasSubscribers;
        stub.InternalStateChanged.Raise(stub.Object, EventArgs.Empty);
    }

    // =========================================================================
    // ACCEPTANCE CRITERIA: Protected members generate correct access modifiers
    // =========================================================================
    // All protected members (property, method, indexer, event) should compile.
    // If any generates "public override" instead of "protected override",
    // CS0507 will fail the build.

    public void AcceptanceCriteria_AllProtectedMembers_Compile()
    {
        var stub = new ProtectedMemberServiceStub();

        // Protected property interceptor accessible on wrapper
        _ = stub.Tag;

        // Protected method interceptor accessible on wrapper
        _ = stub.GetInternalId;
        _ = stub.FormatTag;

        // Protected indexer interceptor accessible on wrapper
        _ = stub.Indexer;

        // The .Object is a ServiceBase -- all protected overrides compiled
        ServiceBase service = stub.Object;
    }
}

// =============================================================================
// PATTERN 6: Inline Class with protected members
// =============================================================================
// Same verification for inline class stubs, which use ClassRenderer.

[KnockOff<ServiceBase>]
public partial class InlineProtectedMemberDemo
{
    public void AcceptanceCriteria_InlineClass_ProtectedEvent_Compiles()
    {
        var stub = new Stubs.ServiceBase();

        // Must compile: inline class stub with protected event
        _ = stub.InternalStateChanged;
        _ = stub.InternalStateChanged.HasSubscribers;
        stub.InternalStateChanged.Raise(stub.Object, EventArgs.Empty);
    }

    public void AcceptanceCriteria_InlineClass_AllProtectedMembers()
    {
        var stub = new Stubs.ServiceBase();

        // Protected property, method, indexer interceptors accessible
        _ = stub.Tag;
        _ = stub.GetInternalId;
        _ = stub.FormatTag;
        _ = stub.Indexer;

        ServiceBase service = stub.Object;
    }
}
