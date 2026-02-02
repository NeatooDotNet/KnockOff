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
        stub.OnCompleted.VerifyAdd(Times.Exactly(2));
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
        stub.OnCompleted.VerifyRemove(Times.Once);
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

        stub.OnCompleted.VerifyAdd(Times.Once);
        Assert.True(stub.OnCompleted.HasSubscribers);

        #region events-reset
        // Clear all tracking counts and remove all subscribers
        stub.OnCompleted.Reset();
        #endregion

        // Counts are cleared - verify add was never called after reset
        stub.OnCompleted.VerifyAdd(Times.Never);

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
        stub.DataReceived.VerifyAdd(Times.Once);

        // Raise the event from the stub
        stub.DataReceived.Raise(stub, new DataEventArgs { Data = "Test" });

        // Unsubscribe and verify
        publisher.DataReceived -= handler;
        stub.DataReceived.VerifyRemove(Times.Once);
        #endregion

        Assert.Equal(1, raiseCount);
        Assert.Equal("Test", receivedArgs?.Data);
        Assert.False(stub.DataReceived.HasSubscribers);
    }
}
