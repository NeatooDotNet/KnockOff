// -----------------------------------------------------------------------------
// Design.Tests - Event Basics Tests
// -----------------------------------------------------------------------------

using Design.Domain.Services;
using Design.Stubs.Events;
using KnockOff;

namespace Design.Tests.EventTests;

/// <summary>
/// Tests for event stubbing: Raise invocation, subscription verification.
/// </summary>
public class EventBasicsTests
{
    [Fact]
    public void EventHandler_CanBeRaisedFromStub()
    {
        var stub = new EventPatternsDemo.Stubs.IEventSource();
        var eventFired = false;

        IEventSource source = stub;
        source.Started += (sender, args) => eventFired = true;

        stub.Started.Raise(source, EventArgs.Empty);

        Assert.True(eventFired);
    }

    [Fact]
    public void EventHandlerOfT_CanBeRaisedWithTypedArgs()
    {
        var stub = new EventPatternsDemo.Stubs.IEventSource();
        DataEventArgs? receivedArgs = null;

        IEventSource source = stub;
        source.DataReceived += (sender, args) => receivedArgs = args;

        stub.DataReceived.Raise(source, new DataEventArgs("test data"));

        Assert.NotNull(receivedArgs);
        Assert.Equal("test data", receivedArgs.Data);
    }

    [Fact]
    public void ActionEvent_CanBeRaisedFromStub()
    {
        var stub = new EventPatternsDemo.Stubs.IEventSource();
        var completed = false;

        IEventSource source = stub;
        source.Completed += () => completed = true;

        stub.Completed.Raise();

        Assert.True(completed);
    }

    [Fact]
    public void ActionWithParams_CanBeRaisedFromStub()
    {
        var stub = new EventPatternsDemo.Stubs.IEventSource();
        string? message = null;
        int? percent = null;

        IEventSource source = stub;
        source.Progress += (msg, pct) =>
        {
            message = msg;
            percent = pct;
        };

        stub.Progress.Raise("Loading", 50);

        Assert.Equal("Loading", message);
        Assert.Equal(50, percent);
    }

    [Fact]
    public void HasSubscribers_FalseWithNoSubscribers()
    {
        var stub = new EventPatternsDemo.Stubs.IEventSource();

        Assert.False(stub.Started.HasSubscribers);
    }

    [Fact]
    public void HasSubscribers_TrueAfterSubscribe()
    {
        var stub = new EventPatternsDemo.Stubs.IEventSource();

        IEventSource source = stub;
        source.Started += (s, e) => { };

        Assert.True(stub.Started.HasSubscribers);
    }

    [Fact]
    public void Event_SafeToCallWithNoSubscribers()
    {
        var stub = new EventPatternsDemo.Stubs.IEventSource();

        // Should not throw
        stub.Started.Raise(null, EventArgs.Empty);
    }

    [Fact]
    public void VerifyAdd_ChecksSubscriptionCount()
    {
        var stub = new EventPatternsDemo.Stubs.IEventSource();
        IEventSource source = stub;

        source.Started += (s, e) => { };
        source.Started += (s, e) => { };

        stub.Started.VerifyAdd(Times.Exactly(2));
    }

    [Fact]
    public void VerifyRemove_ChecksUnsubscriptionCount()
    {
        var stub = new EventPatternsDemo.Stubs.IEventSource();
        IEventSource source = stub;

        void Handler(object? s, EventArgs e) { }

        source.Started += Handler;
        source.Started -= Handler;

        stub.Started.VerifyRemove(Times.Once);
    }

    [Fact]
    public void Reset_ClearsHandlersAndTracking()
    {
        var stub = new EventPatternsDemo.Stubs.IEventSource();
        IEventSource source = stub;

        source.Started += (s, e) => { };

        stub.Started.Reset();

        Assert.False(stub.Started.HasSubscribers);
        Assert.Throws<VerificationException>(() =>
            stub.Started.VerifyAdd(Times.AtLeastOnce));
    }

    [Fact]
    public void HasSubscribers_FalseAfterUnsubscribeAll()
    {
        var stub = new EventPatternsDemo.Stubs.IEventSource();
        IEventSource source = stub;

        void Handler(object? s, EventArgs e) { }

        source.Started += Handler;
        Assert.True(stub.Started.HasSubscribers);

        source.Started -= Handler;
        Assert.False(stub.Started.HasSubscribers);
    }
}
