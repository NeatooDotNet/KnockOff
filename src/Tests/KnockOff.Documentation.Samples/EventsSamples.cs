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
    #region events-raise-eventhandler
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

        // Raise the event using the interceptor
        var eventArgs = new DataEventArgs { Data = "Test Data" };
        stub.DataReceived.Raise(stub, eventArgs);

        Assert.NotNull(receivedArgs);
        Assert.Equal("Test Data", receivedArgs.Data);
    }
    #endregion

    #region events-raise-action
    [Fact]
    public void Raise_Action_NotifiesSubscribers()
    {
        var stub = new EventPubStub();

        string? receivedStatus = null;

        IEventPub publisher = stub;
        publisher.StatusChanged += status => receivedStatus = status;

        // Raise Action<T> event with single argument
        stub.StatusChanged.Raise("Connected");

        Assert.Equal("Connected", receivedStatus);
    }
    #endregion
}

// =============================================================================
// Verifying Subscriptions
// =============================================================================

public class SubscriptionVerificationTests
{
    #region events-verify-subscribe
    [Fact]
    public void HasSubscribers_VerifiesActiveSubscriptions()
    {
        var stub = new EventSubStub();

        IEventSub subscriber = stub;

        // Initially no subscribers
        Assert.False(stub.OnCompleted.HasSubscribers);

        // Subscribe a handler
        subscriber.OnCompleted += (sender, args) => { };

        // Now has subscribers
        Assert.True(stub.OnCompleted.HasSubscribers);
    }
    #endregion

    #region events-verify-addcount
    [Fact]
    public void AddCount_TracksSubscriptionOperations()
    {
        var stub = new EventSubStub();

        IEventSub subscriber = stub;

        subscriber.OnCompleted += (sender, args) => { };
        subscriber.OnCompleted += (sender, args) => { };

        // AddCount tracks subscribe operations
        Assert.Equal(2, stub.OnCompleted.AddCount);
    }
    #endregion

    #region events-verify-unsubscribe
    [Fact]
    public void RemoveCount_TracksUnsubscribeOperations()
    {
        var stub = new EventSubStub();

        IEventSub subscriber = stub;

        EventHandler handler = (sender, args) => { };

        subscriber.OnCompleted += handler;
        subscriber.OnCompleted -= handler;

        // RemoveCount tracks unsubscribe operations
        Assert.Equal(1, stub.OnCompleted.RemoveCount);
    }
    #endregion
}

// =============================================================================
// Reset Events
// =============================================================================

public class EventResetTests
{
    #region events-reset
    [Fact]
    public void Reset_ClearsCountsAndSubscribers()
    {
        var stub = new EventSubStub();

        IEventSub subscriber = stub;

        EventHandler handler = (sender, args) => { };
        subscriber.OnCompleted += handler;

        Assert.Equal(1, stub.OnCompleted.AddCount);
        Assert.True(stub.OnCompleted.HasSubscribers);

        // Reset clears counts and subscribers
        stub.OnCompleted.Reset();

        // Counts are cleared
        Assert.Equal(0, stub.OnCompleted.AddCount);

        // Subscribers are also cleared
        Assert.False(stub.OnCompleted.HasSubscribers);
    }
    #endregion
}

// =============================================================================
// Complete Example
// =============================================================================

public class CompleteEventExampleTests
{
    #region events-complete-example
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

        // Subscribe and verify
        publisher.DataReceived += handler;
        Assert.Equal(1, stub.DataReceived.AddCount);
        Assert.True(stub.DataReceived.HasSubscribers);

        // Raise the event
        var eventArgs = new DataEventArgs { Data = "Test" };
        stub.DataReceived.Raise(stub, eventArgs);
        Assert.Equal(1, raiseCount);
        Assert.Equal("Test", receivedArgs?.Data);

        // Unsubscribe and verify
        publisher.DataReceived -= handler;
        Assert.Equal(1, stub.DataReceived.RemoveCount);
        Assert.False(stub.DataReceived.HasSubscribers);
    }
    #endregion
}
