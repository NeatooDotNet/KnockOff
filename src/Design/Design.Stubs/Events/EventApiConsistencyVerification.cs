// -----------------------------------------------------------------------------
// Design.Stubs - Event API Consistency Verification
// -----------------------------------------------------------------------------
// ACCEPTANCE CRITERIA: This file must compile after the event API consistency
// fix is implemented. It verifies that ALL affected patterns produce event
// interceptors with:
// - Bare property names (no "Interceptor" suffix)
// - Raise() method
// - HasSubscribers property
// - No public Handler property
//
// Patterns covered:
// - Pattern 5: Inline Interface   [KnockOff<IEventSource>]
// - Pattern 3: Standalone Class   [KnockOffBase<EventServiceBase>]
// - Pattern 8: Open Generic Interface  [KnockOff(typeof(IGenericEventSource<>))]
// -----------------------------------------------------------------------------

using Design.Domain.Abstractions;
using Design.Domain.Services;
using KnockOff;

namespace Design.Stubs.Events;

// =============================================================================
// PATTERN 5: Inline Interface - [KnockOff<IEventSource>]
// =============================================================================
// This class already exists in EventPatterns.cs as [KnockOff<IEventSource>].
// We reuse the same EventPatternsDemo.Stubs.IEventSource stub.

public partial class EventPatternsDemo
{
    // =========================================================================
    // ACCEPTANCE CRITERIA: Bare event names (no "Interceptor" suffix)
    // =========================================================================
    // After fix: stub.Started (not stub.StartedInterceptor)

    public void AcceptanceCriteria_BareEventNames()
    {
        var stub = new Stubs.IEventSource();

        // These must compile with bare names (no Interceptor suffix):
        _ = stub.Started;
        _ = stub.DataReceived;
        _ = stub.Completed;
        _ = stub.Progress;
    }

    // =========================================================================
    // ACCEPTANCE CRITERIA: Raise() method on event interceptors
    // =========================================================================
    // After fix: stub.Started.Raise(sender, args)

    public void AcceptanceCriteria_RaiseMethod_EventHandler()
    {
        var stub = new Stubs.IEventSource();
        IEventSource source = stub;
        source.Started += (s, e) => { };

        // Must compile: Raise(object? sender, EventArgs e)
        stub.Started.Raise(stub, EventArgs.Empty);
    }

    public void AcceptanceCriteria_RaiseMethod_EventHandlerOfT()
    {
        var stub = new Stubs.IEventSource();
        IEventSource source = stub;
        source.DataReceived += (s, e) => { };

        // Must compile: Raise(object? sender, DataEventArgs e)
        stub.DataReceived.Raise(stub, new DataEventArgs("test"));
    }

    public void AcceptanceCriteria_RaiseMethod_Action()
    {
        var stub = new Stubs.IEventSource();
        IEventSource source = stub;
        source.Completed += () => { };

        // Must compile: Raise() with no parameters
        stub.Completed.Raise();
    }

    public void AcceptanceCriteria_RaiseMethod_ActionWithParams()
    {
        var stub = new Stubs.IEventSource();
        IEventSource source = stub;
        source.Progress += (msg, pct) => { };

        // Must compile: Raise(string, int)
        stub.Progress.Raise("Loading", 50);
    }

    // =========================================================================
    // ACCEPTANCE CRITERIA: HasSubscribers property
    // =========================================================================

    public void AcceptanceCriteria_HasSubscribers()
    {
        var stub = new Stubs.IEventSource();
        IEventSource source = stub;

        // Must compile: HasSubscribers returns bool
        bool before = stub.Started.HasSubscribers;

        source.Started += (s, e) => { };
        bool after = stub.Started.HasSubscribers;
    }

    // =========================================================================
    // ACCEPTANCE CRITERIA: VerifyAdd/VerifyRemove with bare names
    // =========================================================================

    public void AcceptanceCriteria_VerifyWithBareNames()
    {
        var stub = new Stubs.IEventSource();
        IEventSource source = stub;
        source.Started += (s, e) => { };

        // Must compile with bare names:
        stub.Started.VerifyAdd(Called.Once);
        stub.Started.Verify();
        stub.Started.Verifiable();
        stub.Started.Reset();
    }
}

// =============================================================================
// PATTERN 3: Standalone Class - [KnockOffBase<EventServiceBase>]
// =============================================================================

[KnockOffBase<EventServiceBase>]
public partial class EventServiceBaseStub
{
    // =========================================================================
    // ACCEPTANCE CRITERIA: Standalone class events have Raise() and HasSubscribers
    // =========================================================================

    public void AcceptanceCriteria_StandaloneClass_Raise_EventHandler()
    {
        var stub = new EventServiceBaseStub();

        // Subscribe through .Object
        stub.Object.StatusChanged += (s, e) => { };

        // Must compile: Raise(object? sender, EventArgs e)
        stub.StatusChanged.Raise(stub.Object, EventArgs.Empty);
    }

    public void AcceptanceCriteria_StandaloneClass_Raise_Action()
    {
        var stub = new EventServiceBaseStub();

        stub.Object.Completed += () => { };

        // Must compile: Raise() with no parameters
        stub.Completed.Raise();
    }

    public void AcceptanceCriteria_StandaloneClass_HasSubscribers()
    {
        var stub = new EventServiceBaseStub();

        // Must compile: HasSubscribers returns bool
        bool before = stub.StatusChanged.HasSubscribers;

        stub.Object.StatusChanged += (s, e) => { };
        bool after = stub.StatusChanged.HasSubscribers;
    }

    public void AcceptanceCriteria_StandaloneClass_Verify()
    {
        var stub = new EventServiceBaseStub();

        stub.Object.StatusChanged += (s, e) => { };

        // Must compile: verification methods
        stub.StatusChanged.VerifyAdd(Called.Once);
        stub.StatusChanged.Verify();
        stub.StatusChanged.Verifiable();
        stub.StatusChanged.Reset();
    }
}

// =============================================================================
// PATTERN 8: Open Generic Interface - [KnockOff(typeof(IGenericEventSource<>))]
// =============================================================================

[KnockOff(typeof(IGenericEventSource<>))]
public partial class OpenGenericEventDemo
{
    // =========================================================================
    // ACCEPTANCE CRITERIA: Open generic events have Raise() and HasSubscribers
    // =========================================================================

    public void AcceptanceCriteria_OpenGeneric_Raise_EventHandler()
    {
        var stub = new Stubs.IGenericEventSource<string>();
        IGenericEventSource<string> source = stub;

        source.StatusChanged += (s, e) => { };

        // Must compile: Raise(object? sender, EventArgs e)
        stub.StatusChanged.Raise(stub, EventArgs.Empty);
    }

    public void AcceptanceCriteria_OpenGeneric_Raise_EventHandlerOfT()
    {
        var stub = new Stubs.IGenericEventSource<string>();
        IGenericEventSource<string> source = stub;

        source.DataReceived += (s, e) => { };

        // Must compile: Raise(object? sender, GenericDataEventArgs<string> e)
        stub.DataReceived.Raise(stub, new GenericDataEventArgs<string>("test"));
    }

    public void AcceptanceCriteria_OpenGeneric_HasSubscribers()
    {
        var stub = new Stubs.IGenericEventSource<string>();
        IGenericEventSource<string> source = stub;

        // Must compile: HasSubscribers returns bool
        bool before = stub.StatusChanged.HasSubscribers;

        source.StatusChanged += (s, e) => { };
        bool after = stub.StatusChanged.HasSubscribers;
    }

    public void AcceptanceCriteria_OpenGeneric_Verify()
    {
        var stub = new Stubs.IGenericEventSource<string>();
        IGenericEventSource<string> source = stub;

        source.StatusChanged += (s, e) => { };

        // Must compile: verification methods with bare names
        stub.StatusChanged.VerifyAdd(Called.Once);
        stub.StatusChanged.Verify();
        stub.StatusChanged.Verifiable();
        stub.StatusChanged.Reset();
    }
}
