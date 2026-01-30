// -----------------------------------------------------------------------------
// Design.Stubs - Event Stubbing
// -----------------------------------------------------------------------------
// This file demonstrates the event stubbing APIs:
// - Handler property to raise events
// - VerifyAdd() and VerifyRemove() for subscription verification
// - Handler != null to check for active handlers
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
    // Handler.Invoke(sender, args) - Fire EventHandler Events
    // =========================================================================
    // DESIGN DECISION: Event interceptors expose a Handler property that holds
    // the backing delegate. To raise an event, invoke the Handler directly.
    //
    // GENERATOR BEHAVIOR: For EventHandler event:
    //
    //   public event EventHandler? Started;
    //
    // The generator produces:
    //
    //   public class StartedInterceptor
    //   {
    //       public EventHandler? Handler { get; private set; }
    //       public void RecordAdd(EventHandler? handler) { ... }
    //       public void RecordRemove(EventHandler? handler) { ... }
    //   }
    //
    // DID NOT DO THIS: Add Raise() method to interceptors
    //
    // REJECTED PATTERN:
    //   stub.StartedInterceptor.Raise(source, EventArgs.Empty);
    //
    // WHY NOT: Keeping interceptors simple - they expose the Handler directly.
    // Use Handler?.Invoke() to fire events. This is consistent with standard
    // C# event invocation patterns.
    //
    // ACTUAL PATTERN:
    //   stub.StartedInterceptor.Handler?.Invoke(source, EventArgs.Empty);
    // =========================================================================

    public void Handler_FiresEventHandler()
    {
        var stub = new Stubs.IEventSource();
        var eventFired = false;

        IEventSource source = stub;

        // Subscribe to the event
        source.Started += (sender, args) =>
        {
            eventFired = true;
        };

        // Fire the event from test code via Handler
        stub.StartedInterceptor.Handler?.Invoke(source, EventArgs.Empty);

        // eventFired is now true
    }

    // =========================================================================
    // Handler.Invoke(sender, args) - Fire EventHandler<T> Events
    // =========================================================================
    // DESIGN DECISION: EventHandler<T> events use typed EventArgs.
    // The Handler property has the correct delegate type.
    //
    // GENERATOR BEHAVIOR: For EventHandler<DataEventArgs>:
    //
    //   public EventHandler<DataEventArgs>? Handler { get; private set; }
    // =========================================================================

    public void Handler_FiresEventHandlerWithTypedArgs()
    {
        var stub = new Stubs.IEventSource();
        DataEventArgs? receivedArgs = null;

        IEventSource source = stub;

        source.DataReceived += (sender, args) =>
        {
            receivedArgs = args;
        };

        // Fire with typed args
        stub.DataReceivedInterceptor.Handler?.Invoke(source, new DataEventArgs("test data"));

        // receivedArgs.Data == "test data"
    }

    // =========================================================================
    // Handler.Invoke() - Fire Action Events (No Parameters)
    // =========================================================================
    // DESIGN DECISION: Action events (no sender/args) use Handler.Invoke()
    // with no parameters.
    //
    // GENERATOR BEHAVIOR: For Action event:
    //
    //   public event Action? Completed;
    //
    // Generates:
    //   public Action? Handler { get; private set; }
    // =========================================================================

    public void Handler_FiresActionEvent()
    {
        var stub = new Stubs.IEventSource();
        var completed = false;

        IEventSource source = stub;

        source.Completed += () =>
        {
            completed = true;
        };

        // Fire with no parameters
        stub.CompletedInterceptor.Handler?.Invoke();

        // completed is now true
    }

    // =========================================================================
    // Handler.Invoke(arg1, arg2, ...) - Fire Action<T...> Events
    // =========================================================================
    // DESIGN DECISION: Action<T1, T2, ...> events take the typed parameters
    // directly when invoking Handler.
    //
    // GENERATOR BEHAVIOR: For Action<string, int>:
    //
    //   public event Action<string, int>? Progress;
    //
    // Generates:
    //   public Action<string, int>? Handler { get; private set; }
    // =========================================================================

    public void Handler_FiresActionWithParameters()
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
        stub.ProgressInterceptor.Handler?.Invoke("Loading", 50);

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
    //   public void VerifyAdd() { VerifyAdd(Times.AtLeastOnce); }
    //   public void VerifyAdd(Times times) { ... check _addCount ... }
    //   public void VerifyRemove() { VerifyRemove(Times.AtLeastOnce); }
    //   public void VerifyRemove(Times times) { ... check _removeCount ... }
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
        stub.StartedInterceptor.VerifyAdd(Times.Exactly(2));
        stub.StartedInterceptor.VerifyRemove(Times.Once);
    }

    // =========================================================================
    // Handler != null - Check for Active Handlers
    // =========================================================================
    // DESIGN DECISION: Check Handler != null to determine if at least one
    // handler is currently subscribed. This is useful for conditional event
    // firing.
    //
    // DID NOT DO THIS: Add HasSubscribers property to interceptors
    //
    // REJECTED PATTERN:
    //   var hasSubscribers = stub.StartedInterceptor.HasSubscribers;
    //
    // WHY NOT: The Handler property already provides this information.
    // Checking Handler != null is the standard .NET pattern for determining
    // if an event has subscribers.
    //
    // ACTUAL PATTERN:
    //   var hasSubscribers = stub.StartedInterceptor.Handler != null;
    // =========================================================================

    public void Handler_ChecksForActiveHandlers()
    {
        var stub = new Stubs.IEventSource();
        IEventSource source = stub;

        // Initially no subscribers
        var has1 = stub.StartedInterceptor.Handler != null; // false

        void Handler(object? s, EventArgs e) { }

        // Subscribe
        source.Started += Handler;
        var has2 = stub.StartedInterceptor.Handler != null; // true

        // Unsubscribe
        source.Started -= Handler;
        var has3 = stub.StartedInterceptor.Handler != null; // false
    }

    // =========================================================================
    // Reset() - Clear Handlers and Tracking
    // =========================================================================
    // DESIGN DECISION: Reset() on events:
    // - Clears all subscribed handlers (Handler = null)
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
        stub.StartedInterceptor.Reset();

        var hasHandlers = stub.StartedInterceptor.Handler != null; // false
        // VerifyAdd(Times.AtLeastOnce) would fail now
    }

    // =========================================================================
    // COMMON MISTAKE: Raising Events Without Subscribers
    // =========================================================================
    //
    // COMMON MISTAKE: Forgetting null-conditional when raising events
    //
    // Always use Handler?.Invoke() with the null-conditional operator.
    // If no handlers are subscribed, Handler is null.
    //
    // WRONG: stub.StartedInterceptor.Handler.Invoke(source, EventArgs.Empty);
    // RIGHT: stub.StartedInterceptor.Handler?.Invoke(source, EventArgs.Empty);
    // =========================================================================

    public void Raise_SafeWithNoSubscribers()
    {
        var stub = new Stubs.IEventSource();

        // No subscribers - safe with null-conditional
        stub.StartedInterceptor.Handler?.Invoke(null, EventArgs.Empty);

        // No exception thrown
    }

    // =========================================================================
    // DESIGN DECISION: Event Types Supported
    // =========================================================================
    // KnockOff supports these event delegate types:
    //
    // 1. EventHandler - Handler?.Invoke(object? sender, EventArgs e)
    // 2. EventHandler<T> - Handler?.Invoke(object? sender, T e) where T : EventArgs
    // 3. Action - Handler?.Invoke()
    // 4. Action<T1> - Handler?.Invoke(T1 arg1)
    // 5. Action<T1, T2> - Handler?.Invoke(T1 arg1, T2 arg2)
    // 6. Action<T1, T2, T3, ...> - Handler?.Invoke(T1 arg1, T2 arg2, T3 arg3, ...)
    //
    // DID NOT DO THIS: Support custom delegate types for events
    //
    // REJECTED PATTERN:
    //   public delegate void MyCustomEventHandler(int code, string message);
    //   event MyCustomEventHandler? CustomEvent;
    //
    // Custom delegate events may work but are not explicitly tested.
    // Prefer standard EventHandler<T> or Action<T...> patterns.
    // =========================================================================
}
