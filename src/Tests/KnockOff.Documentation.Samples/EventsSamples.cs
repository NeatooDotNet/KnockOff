namespace KnockOff.Documentation.Samples.Events;

// =============================================================================
// Interfaces for Event Samples
// =============================================================================

public interface IEventPub
{
    event EventHandler<DataEventArgs>? DataReceived;
    event Action<string>? StatusChanged;
    void Publish(string data);
}

public interface IEventSub
{
    event EventHandler? OnCompleted;
    void Subscribe();
}

public interface IEventSource
{
    event EventHandler? Started;
    event EventHandler<DataEventArgs>? DataReceived;
    event Action? Completed;
    event Action<string, int>? Progress;
}

[KnockOff]
public partial class EventSourceStub : IEventSource { }

// =============================================================================
// Stubs for Event Samples
// =============================================================================

[KnockOff]
public partial class EventPubStub : IEventPub { }

[KnockOff]
public partial class EventSubStub : IEventSub { }

// =============================================================================
// Raising Events Samples
// =============================================================================

public class RaisingEventsTests
{
    [Fact]
    public void Raise_EventHandler_NotifiesSubscribers()
    {
        var stub = new EventPubStub();
        DataEventArgs? receivedArgs = null;

        // Subscribe to the event through the interface
        IEventPub publisher = stub;
        publisher.DataReceived += (sender, args) =>
        {
            receivedArgs = args;
        };

        #region events-raise-eventhandler
        // Raise EventHandler<T> event with sender and args
        stub.DataReceived.Raise(stub, new DataEventArgs { Data = "Test Data" });
        #endregion

        Assert.NotNull(receivedArgs);
        Assert.Equal("Test Data", receivedArgs.Data);
    }

    [Fact]
    public void Raise_Action_NotifiesSubscribers()
    {
        var stub = new EventPubStub();
        string? receivedStatus = null;

        IEventPub publisher = stub;
        publisher.StatusChanged += status => receivedStatus = status;

        #region events-raise-action
        // Raise Action<T> event with single argument
        stub.StatusChanged.Raise("Connected");
        #endregion

        Assert.Equal("Connected", receivedStatus);
    }
}

// =============================================================================
// Verifying Subscriptions
// =============================================================================

public class SubscriptionVerificationTests
{
    [Fact]
    public void HasSubscribers_VerifiesActiveSubscriptions()
    {
        var stub = new EventSubStub();
        IEventSub subscriber = stub;

        // Initially no subscribers
        Assert.False(stub.OnCompleted.HasSubscribers);

        // Subscribe a handler
        subscriber.OnCompleted += (sender, args) => { };

        #region events-verify-subscribe
        // Check if any handlers are subscribed
        var hasHandlers = stub.OnCompleted.HasSubscribers;
        #endregion

        Assert.True(hasHandlers);
    }

    [Fact]
    public void AddCount_TracksSubscriptionOperations()
    {
        var stub = new EventSubStub();
        IEventSub subscriber = stub;

        subscriber.OnCompleted += (sender, args) => { };
        subscriber.OnCompleted += (sender, args) => { };

        #region events-verify-addcount
        // Verify how many times handlers were subscribed
        stub.OnCompleted.VerifyAdd(Called.Exactly(2));
        #endregion
    }

    [Fact]
    public void RemoveCount_TracksUnsubscribeOperations()
    {
        var stub = new EventSubStub();
        IEventSub subscriber = stub;
        EventHandler handler = (sender, args) => { };

        subscriber.OnCompleted += handler;
        subscriber.OnCompleted -= handler;

        #region events-verify-unsubscribe
        // Verify how many times handlers were unsubscribed
        stub.OnCompleted.VerifyRemove(Called.Once);
        #endregion
    }
}

// =============================================================================
// Batch Verification with Verifiable
// =============================================================================

public class EventVerifiableTests
{
    [Fact]
    public void Verifiable_IncludesEventInBatchVerification()
    {
        var stub = new EventSubStub();
        IEventSub subscriber = stub;

        #region events-verifiable
        // Mark event for batch verification (expects at least one add/remove)
        stub.OnCompleted.Verifiable();
        #endregion

        // Subscribe to the event (satisfies Verifiable)
        subscriber.OnCompleted += (sender, args) => { };

        // Verify() checks all members marked with .Verifiable()
        stub.Verify();
    }
}

// =============================================================================
// Reset Events
// =============================================================================

public class EventResetTests
{
    [Fact]
    public void Reset_ClearsCountsAndSubscribers()
    {
        var stub = new EventSubStub();
        IEventSub subscriber = stub;
        EventHandler handler = (sender, args) => { };
        subscriber.OnCompleted += handler;

        stub.OnCompleted.VerifyAdd(Called.Once);
        Assert.True(stub.OnCompleted.HasSubscribers);

        #region events-reset
        // Clear all tracking counts and remove all subscribers
        stub.OnCompleted.Reset();
        #endregion

        // Counts are cleared - verify add was never called after reset
        stub.OnCompleted.VerifyAdd(Called.Never);

        // Subscribers are also cleared
        Assert.False(stub.OnCompleted.HasSubscribers);
    }
}

// =============================================================================
// Complete Example
// =============================================================================

public class CompleteEventExampleTests
{
    [Fact]
    public void Event_FullWorkflow_SubscribeRaiseUnsubscribe()
    {
        var stub = new EventPubStub();

        DataEventArgs? receivedArgs = null;
        int raiseCount = 0;

        EventHandler<DataEventArgs> handler = (sender, args) =>
        {
            receivedArgs = args;
            raiseCount++;
        };

        IEventPub publisher = stub;

        #region events-complete-example
        // Subscribe through the interface
        publisher.DataReceived += handler;
        stub.DataReceived.VerifyAdd(Called.Once);

        // Raise the event from the stub
        stub.DataReceived.Raise(stub, new DataEventArgs { Data = "Test" });

        // Unsubscribe and verify
        publisher.DataReceived -= handler;
        stub.DataReceived.VerifyRemove(Called.Once);
        #endregion

        Assert.Equal(1, raiseCount);
        Assert.Equal("Test", receivedArgs?.Data);
        Assert.False(stub.DataReceived.HasSubscribers);
    }
}

// =============================================================================
// Event Reference Samples (for events.md)
// =============================================================================

public class EventRefRaiseTests
{
    [Fact]
    public void Events_RaiseEventHandler()
    {
        var stub = new EventSourceStub();
        IEventSource source = stub;
        var raised = false;
        source.Started += (s, e) => raised = true;

        #region events-ref-raise-eventhandler
        // event EventHandler? Started;
        stub.Started.Raise(source, EventArgs.Empty);
        #endregion

        Assert.True(raised);
    }

    [Fact]
    public void Events_RaiseEventHandlerT()
    {
        var stub = new EventSourceStub();
        IEventSource source = stub;
        DataEventArgs? received = null;
        source.DataReceived += (s, e) => received = e;

        #region events-ref-raise-eventhandler-t
        // event EventHandler<DataEventArgs>? DataReceived;
        stub.DataReceived.Raise(source, new DataEventArgs { Data = "test data" });
        #endregion

        Assert.Equal("test data", received?.Data);
    }

    [Fact]
    public void Events_RaiseAction()
    {
        var stub = new EventSourceStub();
        IEventSource source = stub;
        var raised = false;
        source.Completed += () => raised = true;

        #region events-ref-raise-action
        // event Action? Completed;
        stub.Completed.Raise();
        #endregion

        Assert.True(raised);
    }

    [Fact]
    public void Events_RaiseActionTyped()
    {
        var stub = new EventSourceStub();
        IEventSource source = stub;
        string? msg = null;
        int? pct = null;
        source.Progress += (m, p) => { msg = m; pct = p; };

        #region events-ref-raise-action-typed
        // event Action<string, int>? Progress;
        stub.Progress.Raise("Loading", 50);
        #endregion

        Assert.Equal("Loading", msg);
        Assert.Equal(50, pct);
    }

    [Fact]
    public void Events_RaiseSafety()
    {
        #region events-ref-raise-safety
        var stub = new EventSourceStub();
        // No subscribers -- no exception
        stub.Started.Raise(null, EventArgs.Empty);
        #endregion
    }
}

public class EventRefHasSubscribersTests
{
    [Fact]
    public void Events_HasSubscribers()
    {
        #region events-ref-has-subscribers
        var stub = new EventSourceStub();
        IEventSource source = stub;

        Assert.False(stub.Started.HasSubscribers);

        source.Started += (s, e) => { };
        Assert.True(stub.Started.HasSubscribers);
        #endregion
    }
}

public class EventRefVerifyTests
{
    [Fact]
    public void Events_VerifyAddRemove()
    {
        var stub = new EventSourceStub();
        IEventSource source = stub;

        void Handler1(object? s, EventArgs e) { }
        void Handler2(object? s, EventArgs e) { }

        source.Started += Handler1;
        source.Started += Handler2;
        source.Started -= Handler1;

        #region events-ref-verify-add-remove
        stub.Started.VerifyAdd(Called.Exactly(2));  // 2 subscriptions
        stub.Started.VerifyRemove(Called.Once);     // 1 unsubscription
        #endregion
    }

    [Fact]
    public void Events_BatchVerifiable()
    {
        var stub = new EventSourceStub();
        IEventSource source = stub;

        #region events-ref-batch-verifiable
        stub.Started.Verifiable();           // Requires at least one subscription
        stub.DataReceived.Verifiable(Called.Never); // Must NOT be subscribed
        #endregion

        source.Started += (s, e) => { };

        stub.Verify();
    }
}

public class EventRefResetTests
{
    [Fact]
    public void Events_ResetBehavior()
    {
        var stub = new EventSourceStub();
        IEventSource source = stub;

        #region events-ref-reset
        source.Started += (s, e) => { };
        source.Started += (s, e) => { };

        stub.Started.Reset();

        Assert.False(stub.Started.HasSubscribers);
        // VerifyAdd(Called.AtLeastOnce) would FAIL now
        #endregion
    }
}

public class EventRefCompleteExampleTests
{
    [Fact]
    public void Events_CompleteExample()
    {
        #region events-ref-complete
        var stub = new EventSourceStub();
        IEventSource source = stub;

        // Track events
        var events = new List<string>();

        source.Started += (s, e) => events.Add("started");
        source.DataReceived += (s, e) => events.Add($"data: {e.Data}");
        source.Completed += () => events.Add("completed");
        source.Progress += (msg, pct) => events.Add($"{msg}: {pct}%");

        // Fire events
        stub.Started.Raise(source, EventArgs.Empty);
        stub.DataReceived.Raise(source, new DataEventArgs { Data = "test" });
        stub.Progress.Raise("Loading", 75);
        stub.Completed.Raise();

        // Verify subscriptions
        stub.Started.VerifyAdd(Called.Once);
        stub.DataReceived.VerifyAdd(Called.Once);
        stub.Completed.VerifyAdd(Called.Once);
        stub.Progress.VerifyAdd(Called.Once);
        #endregion

        Assert.Equal(4, events.Count);
    }
}
