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

        #region events-raise-eventhandler
        // Subscribe to the event through the interface
        IEventPub publisher = stub;
        publisher.DataReceived += (sender, args) =>
        {
            receivedArgs = args;
        };

        // Raise the event using the interceptor
        var eventArgs = new DataEventArgs { Data = "Test Data" };
        stub.DataReceived.Raise(stub, eventArgs);
        #endregion

        Assert.NotNull(receivedArgs);
        Assert.Equal("Test Data", receivedArgs.Data);
    }

    [Fact]
    public void Raise_Action_NotifiesSubscribers()
    {
        var stub = new EventPubStub();
        string? receivedStatus = null;

        #region events-raise-action
        IEventPub publisher = stub;
        publisher.StatusChanged += status => receivedStatus = status;

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

        #region events-verify-subscribe
        // Initially no subscribers
        Assert.False(stub.OnCompleted.HasSubscribers);

        // Subscribe a handler
        subscriber.OnCompleted += (sender, args) => { };

        // Now has subscribers
        Assert.True(stub.OnCompleted.HasSubscribers);
        #endregion
    }

    [Fact]
    public void AddCount_TracksSubscriptionOperations()
    {
        var stub = new EventSubStub();
        IEventSub subscriber = stub;

        #region events-verify-addcount
        subscriber.OnCompleted += (sender, args) => { };
        subscriber.OnCompleted += (sender, args) => { };

        // VerifyAdd tracks subscribe operations
        stub.OnCompleted.VerifyAdd(Times.Exactly(2));
        #endregion
    }

    [Fact]
    public void RemoveCount_TracksUnsubscribeOperations()
    {
        var stub = new EventSubStub();
        IEventSub subscriber = stub;
        EventHandler handler = (sender, args) => { };

        #region events-verify-unsubscribe
        subscriber.OnCompleted += handler;
        subscriber.OnCompleted -= handler;

        // VerifyRemove tracks unsubscribe operations
        stub.OnCompleted.VerifyRemove(Times.Once);
        #endregion
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
        // Reset clears counts and subscribers
        stub.OnCompleted.Reset();

        // Counts are cleared - verify add was never called after reset
        stub.OnCompleted.VerifyAdd(Times.Never);

        // Subscribers are also cleared
        Assert.False(stub.OnCompleted.HasSubscribers);
        #endregion
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
        #region events-complete-example
        var stub = new EventPubStub();

        DataEventArgs? receivedArgs = null;
        int raiseCount = 0;

        EventHandler<DataEventArgs> handler = (sender, args) =>
        {
            receivedArgs = args;
            raiseCount++;
        };

        IEventPub publisher = stub;

        // Subscribe and verify
        publisher.DataReceived += handler;
        stub.DataReceived.VerifyAdd(Times.Once);
        Assert.True(stub.DataReceived.HasSubscribers);

        // Raise the event
        var eventArgs = new DataEventArgs { Data = "Test" };
        stub.DataReceived.Raise(stub, eventArgs);
        Assert.Equal(1, raiseCount);
        Assert.Equal("Test", receivedArgs?.Data);

        // Unsubscribe and verify
        publisher.DataReceived -= handler;
        stub.DataReceived.VerifyRemove(Times.Once);
        Assert.False(stub.DataReceived.HasSubscribers);
        #endregion
    }
}
