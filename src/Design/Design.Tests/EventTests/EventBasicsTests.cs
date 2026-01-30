// -----------------------------------------------------------------------------
// Design.Tests - Event Basics Tests
// -----------------------------------------------------------------------------

using Design.Domain.Services;
using Design.Stubs.Events;
using KnockOff;

namespace Design.Tests.EventTests;

/// <summary>
/// Tests for event stubbing: Handler invocation, subscription verification.
/// </summary>
public class EventBasicsTests
{
    [Fact]
    public void Handler_FiresEventHandler()
    {
        var stub = new EventPatternsDemo.Stubs.IEventSource();
        var eventFired = false;

        IEventSource source = stub;
        source.Started += (sender, args) => eventFired = true;

        stub.StartedInterceptor.Handler?.Invoke(source, EventArgs.Empty);

        Assert.True(eventFired);
    }

    [Fact]
    public void Handler_FiresEventHandlerWithTypedArgs()
    {
        var stub = new EventPatternsDemo.Stubs.IEventSource();
        DataEventArgs? receivedArgs = null;

        IEventSource source = stub;
        source.DataReceived += (sender, args) => receivedArgs = args;

        stub.DataReceivedInterceptor.Handler?.Invoke(source, new DataEventArgs("test data"));

        Assert.NotNull(receivedArgs);
        Assert.Equal("test data", receivedArgs.Data);
    }

    [Fact]
    public void Handler_FiresActionEvent()
    {
        var stub = new EventPatternsDemo.Stubs.IEventSource();
        var completed = false;

        IEventSource source = stub;
        source.Completed += () => completed = true;

        stub.CompletedInterceptor.Handler?.Invoke();

        Assert.True(completed);
    }

    [Fact]
    public void Handler_FiresActionWithParameters()
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

        stub.ProgressInterceptor.Handler?.Invoke("Loading", 50);

        Assert.Equal("Loading", message);
        Assert.Equal(50, percent);
    }

    [Fact]
    public void Handler_IsNullWithNoSubscribers()
    {
        var stub = new EventPatternsDemo.Stubs.IEventSource();

        Assert.Null(stub.StartedInterceptor.Handler);
    }

    [Fact]
    public void Handler_IsNotNullAfterSubscribe()
    {
        var stub = new EventPatternsDemo.Stubs.IEventSource();

        IEventSource source = stub;
        source.Started += (s, e) => { };

        Assert.NotNull(stub.StartedInterceptor.Handler);
    }

    [Fact]
    public void Handler_SafeToInvokeWithNoSubscribers()
    {
        var stub = new EventPatternsDemo.Stubs.IEventSource();

        // Should not throw
        stub.StartedInterceptor.Handler?.Invoke(null, EventArgs.Empty);
    }

    [Fact]
    public void VerifyAdd_ChecksSubscriptionCount()
    {
        var stub = new EventPatternsDemo.Stubs.IEventSource();
        IEventSource source = stub;

        source.Started += (s, e) => { };
        source.Started += (s, e) => { };

        stub.StartedInterceptor.VerifyAdd(Times.Exactly(2));
    }

    [Fact]
    public void VerifyRemove_ChecksUnsubscriptionCount()
    {
        var stub = new EventPatternsDemo.Stubs.IEventSource();
        IEventSource source = stub;

        void Handler(object? s, EventArgs e) { }

        source.Started += Handler;
        source.Started -= Handler;

        stub.StartedInterceptor.VerifyRemove(Times.Once);
    }

    [Fact]
    public void Reset_ClearsHandlersAndTracking()
    {
        var stub = new EventPatternsDemo.Stubs.IEventSource();
        IEventSource source = stub;

        source.Started += (s, e) => { };

        stub.StartedInterceptor.Reset();

        Assert.Null(stub.StartedInterceptor.Handler);
        Assert.Throws<VerificationException>(() =>
            stub.StartedInterceptor.VerifyAdd(Times.AtLeastOnce));
    }

    [Fact]
    public void Handler_IsNullAfterUnsubscribeAll()
    {
        var stub = new EventPatternsDemo.Stubs.IEventSource();
        IEventSource source = stub;

        void Handler(object? s, EventArgs e) { }

        source.Started += Handler;
        Assert.NotNull(stub.StartedInterceptor.Handler);

        source.Started -= Handler;
        Assert.Null(stub.StartedInterceptor.Handler);
    }
}
