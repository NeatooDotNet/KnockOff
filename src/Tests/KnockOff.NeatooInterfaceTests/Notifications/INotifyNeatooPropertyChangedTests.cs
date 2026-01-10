using Neatoo;

namespace KnockOff.NeatooInterfaceTests.Notifications;

/// <summary>
/// Tests for INotifyNeatooPropertyChanged - Neatoo's property changed notification interface.
/// This interface has an async event delegate.
/// </summary>
[KnockOff<INotifyNeatooPropertyChanged>]
public partial class INotifyNeatooPropertyChangedTests
{
    [Fact]
    public void InlineStub_CanBeInstantiated()
    {
        var stub = new Stubs.INotifyNeatooPropertyChanged();
        Assert.NotNull(stub);
    }

    [Fact]
    public void InlineStub_ImplementsInterface()
    {
        var stub = new Stubs.INotifyNeatooPropertyChanged();
        INotifyNeatooPropertyChanged notify = stub;
        Assert.NotNull(notify);
    }

    [Fact]
    public void NeatooPropertyChanged_EventCanBeSubscribed()
    {
        var stub = new Stubs.INotifyNeatooPropertyChanged();
        INotifyNeatooPropertyChanged notify = stub;

        notify.NeatooPropertyChanged += (args) => Task.CompletedTask;

        Assert.Equal(1, stub.NeatooPropertyChangedInterceptor.AddCount);
    }

    [Fact]
    public void NeatooPropertyChanged_EventCanBeUnsubscribed()
    {
        var stub = new Stubs.INotifyNeatooPropertyChanged();
        INotifyNeatooPropertyChanged notify = stub;

        NeatooPropertyChanged handler = (args) => Task.CompletedTask;
        notify.NeatooPropertyChanged += handler;
        notify.NeatooPropertyChanged -= handler;

        Assert.Equal(1, stub.NeatooPropertyChangedInterceptor.AddCount);
        Assert.Equal(1, stub.NeatooPropertyChangedInterceptor.RemoveCount);
    }

    [Fact]
    public void NeatooPropertyChanged_TracksMultipleSubscriptions()
    {
        var stub = new Stubs.INotifyNeatooPropertyChanged();
        INotifyNeatooPropertyChanged notify = stub;

        notify.NeatooPropertyChanged += (args) => Task.CompletedTask;
        notify.NeatooPropertyChanged += (args) => Task.CompletedTask;
        notify.NeatooPropertyChanged += (args) => Task.CompletedTask;

        Assert.Equal(3, stub.NeatooPropertyChangedInterceptor.AddCount);
    }
}

/// <summary>
/// Standalone stub for INotifyNeatooPropertyChanged.
/// </summary>
[KnockOff]
public partial class NotifyNeatooPropertyChangedStub : INotifyNeatooPropertyChanged
{
}

public class INotifyNeatooPropertyChangedStandaloneTests
{
    [Fact]
    public void StandaloneStub_CanBeInstantiated()
    {
        var stub = new NotifyNeatooPropertyChangedStub();
        Assert.NotNull(stub);
    }

    [Fact]
    public void StandaloneStub_ImplementsInterface()
    {
        var stub = new NotifyNeatooPropertyChangedStub();
        INotifyNeatooPropertyChanged notify = stub;
        Assert.NotNull(notify);
    }

    [Fact]
    public void NeatooPropertyChanged_EventCanBeSubscribed()
    {
        var stub = new NotifyNeatooPropertyChangedStub();
        INotifyNeatooPropertyChanged notify = stub;

        notify.NeatooPropertyChanged += (args) => Task.CompletedTask;

        Assert.Equal(1, stub.NeatooPropertyChanged.AddCount);
    }
}

/// <summary>
/// Tests for NeatooPropertyChanged delegate stubbing.
/// This is an async delegate returning Task.
/// </summary>
[KnockOff<NeatooPropertyChanged>]
public partial class NeatooPropertyChangedDelegateTests
{
    [Fact]
    public void DelegateStub_CanBeInstantiated()
    {
        var stub = new Stubs.NeatooPropertyChanged();
        Assert.NotNull(stub);
    }

    [Fact]
    public void DelegateStub_CanBeConvertedToDelegate()
    {
        var stub = new Stubs.NeatooPropertyChanged();
        NeatooPropertyChanged del = stub;
        Assert.NotNull(del);
    }

    [Fact]
    public async Task DelegateStub_TracksInvocation()
    {
        var stub = new Stubs.NeatooPropertyChanged();
        NeatooPropertyChanged del = stub;

        // Set OnCall to return Task.CompletedTask since it's an async delegate
        stub.Interceptor.OnCall = (s, args) => Task.CompletedTask;

        await del(new NeatooPropertyChangedEventArgs("TestProperty", this));

        Assert.True(stub.Interceptor.WasCalled);
        Assert.Equal(1, stub.Interceptor.CallCount);
    }

    [Fact]
    public async Task DelegateStub_TracksMultipleInvocations()
    {
        var stub = new Stubs.NeatooPropertyChanged();
        NeatooPropertyChanged del = stub;

        stub.Interceptor.OnCall = (s, args) => Task.CompletedTask;

        await del(new NeatooPropertyChangedEventArgs("Prop1", this));
        await del(new NeatooPropertyChangedEventArgs("Prop2", this));
        await del(new NeatooPropertyChangedEventArgs("Prop3", this));

        Assert.Equal(3, stub.Interceptor.CallCount);
    }

    [Fact]
    public async Task DelegateStub_CanExecuteCallback()
    {
        var stub = new Stubs.NeatooPropertyChanged();
        NeatooPropertyChanged del = stub;
        var callbackExecuted = false;
        string? capturedPropertyName = null;

        stub.Interceptor.OnCall = (s, args) =>
        {
            callbackExecuted = true;
            capturedPropertyName = args.PropertyName;
            return Task.CompletedTask;
        };

        await del(new NeatooPropertyChangedEventArgs("CapturedProp", this));

        Assert.True(callbackExecuted);
        Assert.Equal("CapturedProp", capturedPropertyName);
    }

    [Fact]
    public async Task DelegateStub_Reset_ClearsTracking()
    {
        var stub = new Stubs.NeatooPropertyChanged();
        NeatooPropertyChanged del = stub;

        stub.Interceptor.OnCall = (s, args) => Task.CompletedTask;

        await del(new NeatooPropertyChangedEventArgs("Prop", this));

        stub.Interceptor.Reset();

        Assert.False(stub.Interceptor.WasCalled);
        Assert.Equal(0, stub.Interceptor.CallCount);
    }
}
