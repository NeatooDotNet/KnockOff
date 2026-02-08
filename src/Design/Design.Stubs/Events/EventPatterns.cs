// -----------------------------------------------------------------------------
// Design.Stubs - Event Stubbing
// -----------------------------------------------------------------------------
// This file demonstrates the event stubbing APIs:
// - Raise() method to fire events with correct parameters
// - HasSubscribers property to check for active handlers
// - VerifyAdd() and VerifyRemove() for subscription verification
// - Different event types: EventHandler, EventHandler<T>, Action<T...>
// -----------------------------------------------------------------------------

using Design.Domain.Services;
using KnockOff;

namespace Design.Stubs.Events;

// =============================================================================
// EVENT STUBBING
// =============================================================================

[KnockOff<IEventSource>]
public partial class EventPatternsDemo
{
    // =========================================================================
    // Raise(sender, args) - Fire EventHandler Events
    // =========================================================================
    // DESIGN DECISION: Event interceptors expose a Raise() method with
    // parameters matching the delegate signature. This provides a type-safe
    // way to fire events from test code.
    //
    // GENERATOR BEHAVIOR: For EventHandler event:
    //
    //   public event EventHandler? Started;
    //
    // The generator produces an interceptor class with bare name:
    //
    //   public class StartedInterceptor
    //   {
    //       private EventHandler? _handler;
    //       public bool HasSubscribers => _handler != null;
    //       public void Raise(object? sender, EventArgs e) => _handler?.Invoke(sender, e);
    //       public void RecordAdd(EventHandler? handler) { ... }
    //       public void RecordRemove(EventHandler? handler) { ... }
    //   }
    //
    // DESIGN DECISION: Event interceptors use bare names matching the event
    // (stub.Started, not stub.StartedInterceptor). The interceptor property
    // and the event live in separate scopes: the event is accessed through
    // the interface cast, while the interceptor is accessed on the stub.
    // =========================================================================

    public void Raise_FiresEventHandler()
    {
        var stub = new Stubs.IEventSource();
        var eventFired = false;

        IEventSource source = stub;

        // Subscribe to the event
        source.Started += (sender, args) =>
        {
            eventFired = true;
        };

        // Fire the event from test code via Raise
        stub.Started.Raise(source, EventArgs.Empty);

        // eventFired is now true
    }

    // =========================================================================
    // Raise(sender, args) - Fire EventHandler<T> Events
    // =========================================================================
    // DESIGN DECISION: EventHandler<T> events use typed EventArgs.
    // The Raise() method has the correct parameter types.
    //
    // GENERATOR BEHAVIOR: For EventHandler<DataEventArgs>:
    //
    //   public void Raise(object? sender, DataEventArgs e) => _handler?.Invoke(sender, e);
    // =========================================================================

    public void Raise_FiresEventHandlerWithTypedArgs()
    {
        var stub = new Stubs.IEventSource();
        DataEventArgs? receivedArgs = null;

        IEventSource source = stub;

        source.DataReceived += (sender, args) =>
        {
            receivedArgs = args;
        };

        // Fire with typed args
        stub.DataReceived.Raise(source, new DataEventArgs("test data"));

        // receivedArgs.Data == "test data"
    }

    // =========================================================================
    // Raise() - Fire Action Events (No Parameters)
    // =========================================================================
    // DESIGN DECISION: Action events (no sender/args) use Raise()
    // with no parameters.
    //
    // GENERATOR BEHAVIOR: For Action event:
    //
    //   public event Action? Completed;
    //
    // Generates:
    //   public void Raise() => _handler?.Invoke();
    // =========================================================================

    public void Raise_FiresActionEvent()
    {
        var stub = new Stubs.IEventSource();
        var completed = false;

        IEventSource source = stub;

        source.Completed += () =>
        {
            completed = true;
        };

        // Fire with no parameters
        stub.Completed.Raise();

        // completed is now true
    }

    // =========================================================================
    // Raise(arg1, arg2, ...) - Fire Action<T...> Events
    // =========================================================================
    // DESIGN DECISION: Action<T1, T2, ...> events take the typed parameters
    // directly when calling Raise().
    //
    // GENERATOR BEHAVIOR: For Action<string, int>:
    //
    //   public event Action<string, int>? Progress;
    //
    // Generates:
    //   public void Raise(string arg0, int arg1) => _handler?.Invoke(arg0, arg1);
    // =========================================================================

    public void Raise_FiresActionWithParameters()
    {
        var stub = new Stubs.IEventSource();
        string? message = null;
        int? percent = null;

        IEventSource source = stub;

        source.Progress += (msg, pct) =>
        {
            message = msg;
            percent = pct;
        };

        // Fire with typed parameters
        stub.Progress.Raise("Loading", 50);

        // message == "Loading", percent == 50
    }

    // =========================================================================
    // VerifyAdd() and VerifyRemove() - Subscription Verification
    // =========================================================================
    // DESIGN DECISION: Events track subscriptions (add) and unsubscriptions
    // (remove) separately. Use VerifyAdd/VerifyRemove to check handler counts.
    //
    // GENERATOR BEHAVIOR:
    //
    //   public void VerifyAdd() { VerifyAdd(Called.AtLeastOnce); }
    //   public void VerifyAdd(Called called) { ... check _addCount ... }
    //   public void VerifyRemove() { VerifyRemove(Called.AtLeastOnce); }
    //   public void VerifyRemove(Called called) { ... check _removeCount ... }
    // =========================================================================

    public void Verify_SubscriptionAndUnsubscription()
    {
        var stub = new Stubs.IEventSource();
        IEventSource source = stub;

        void Handler1(object? s, EventArgs e) { }
        void Handler2(object? s, EventArgs e) { }

        // Subscribe two handlers
        source.Started += Handler1;
        source.Started += Handler2;

        // Unsubscribe one
        source.Started -= Handler1;

        // Verify add/remove counts
        stub.Started.VerifyAdd(Called.Exactly(2));
        stub.Started.VerifyRemove(Called.Once);
    }

    // =========================================================================
    // HasSubscribers - Check for Active Handlers
    // =========================================================================
    // DESIGN DECISION: Use HasSubscribers to determine if at least one handler
    // is currently subscribed. This is useful for conditional event firing.
    // =========================================================================

    public void HasSubscribers_ChecksForActiveHandlers()
    {
        var stub = new Stubs.IEventSource();
        IEventSource source = stub;

        // Initially no subscribers
        var has1 = stub.Started.HasSubscribers; // false

        void Handler(object? s, EventArgs e) { }

        // Subscribe
        source.Started += Handler;
        var has2 = stub.Started.HasSubscribers; // true

        // Unsubscribe
        source.Started -= Handler;
        var has3 = stub.Started.HasSubscribers; // false
    }

    // =========================================================================
    // Reset() - Clear Handlers and Tracking
    // =========================================================================
    // DESIGN DECISION: Reset() on events:
    // - Clears all subscribed handlers (HasSubscribers becomes false)
    // - Resets add/remove counts to 0
    //
    // This is useful for test isolation.
    // =========================================================================

    public void Reset_ClearsHandlersAndTracking()
    {
        var stub = new Stubs.IEventSource();
        IEventSource source = stub;

        source.Started += (s, e) => { };
        source.Started += (s, e) => { };

        // Reset clears everything
        stub.Started.Reset();

        var hasHandlers = stub.Started.HasSubscribers; // false
        // VerifyAdd(Called.AtLeastOnce) would fail now
    }

    // =========================================================================
    // Raise() - Safe With No Subscribers
    // =========================================================================
    // DESIGN DECISION: Raise() is safe to call even when no subscribers exist.
    // Internally it uses null-conditional invocation on the backing delegate.
    // =========================================================================

    public void Raise_SafeWithNoSubscribers()
    {
        var stub = new Stubs.IEventSource();

        // No subscribers - Raise is a no-op
        stub.Started.Raise(null, EventArgs.Empty);

        // No exception thrown
    }

    // =========================================================================
    // DESIGN DECISION: Event Types Supported
    // =========================================================================
    // KnockOff supports these event delegate types:
    //
    // 1. EventHandler       - Raise(object? sender, EventArgs e)
    // 2. EventHandler<T>    - Raise(object? sender, T e) where T : EventArgs
    // 3. Action             - Raise()
    // 4. Action<T1>         - Raise(T1 arg1)
    // 5. Action<T1, T2>     - Raise(T1 arg1, T2 arg2)
    // 6. Action<T1, ...>    - Raise(T1 arg1, T2 arg2, T3 arg3, ...)
    // 7. Custom delegates   - Raise() uses DynamicInvoke
    //
    // Custom delegate events use DynamicInvoke internally. Prefer standard
    // EventHandler<T> or Action<T...> patterns for best type safety.
    // =========================================================================
}
