namespace KnockOff.Tests;

/// <summary>
/// Tests for event support (EventHandler, EventHandler&lt;T&gt;, Action&lt;T&gt;, etc.).
/// </summary>
public class EventTests
{
	[Fact]
	public void Event_Subscribe_TracksSubscription()
	{
		var knockOff = new EventSourceKnockOff();
		IEventSource source = knockOff;

		Assert.False(knockOff.IEventSource.MessageReceived.HasSubscribers);
		Assert.Equal(0, knockOff.IEventSource.MessageReceived.SubscribeCount);

		source.MessageReceived += (sender, e) => { };

		Assert.True(knockOff.IEventSource.MessageReceived.HasSubscribers);
		Assert.Equal(1, knockOff.IEventSource.MessageReceived.SubscribeCount);
	}

	[Fact]
	public void Event_Unsubscribe_TracksUnsubscription()
	{
		var knockOff = new EventSourceKnockOff();
		IEventSource source = knockOff;

		EventHandler<string> handler = (sender, e) => { };
		source.MessageReceived += handler;
		source.MessageReceived -= handler;

		Assert.Equal(1, knockOff.IEventSource.MessageReceived.SubscribeCount);
		Assert.Equal(1, knockOff.IEventSource.MessageReceived.UnsubscribeCount);
	}

	[Fact]
	public void Event_Raise_InvokesHandler_EventHandlerOfT()
	{
		var knockOff = new EventSourceKnockOff();
		IEventSource source = knockOff;

		string? receivedMessage = null;
		object? receivedSender = null;
		source.MessageReceived += (sender, e) =>
		{
			receivedSender = sender;
			receivedMessage = e;
		};

		knockOff.IEventSource.MessageReceived.Raise(knockOff, "Hello World");

		Assert.Same(knockOff, receivedSender);
		Assert.Equal("Hello World", receivedMessage);
	}

	[Fact]
	public void Event_Raise_TracksRaiseCount()
	{
		var knockOff = new EventSourceKnockOff();
		IEventSource source = knockOff;

		source.MessageReceived += (sender, e) => { };

		Assert.False(knockOff.IEventSource.MessageReceived.WasRaised);
		Assert.Equal(0, knockOff.IEventSource.MessageReceived.RaiseCount);

		knockOff.IEventSource.MessageReceived.Raise("First");
		knockOff.IEventSource.MessageReceived.Raise("Second");

		Assert.True(knockOff.IEventSource.MessageReceived.WasRaised);
		Assert.Equal(2, knockOff.IEventSource.MessageReceived.RaiseCount);
	}

	[Fact]
	public void Event_Raise_TracksLastRaiseArgs()
	{
		var knockOff = new EventSourceKnockOff();
		IEventSource source = knockOff;

		source.MessageReceived += (sender, e) => { };

		knockOff.IEventSource.MessageReceived.Raise(knockOff, "First");
		knockOff.IEventSource.MessageReceived.Raise(knockOff, "Second");
		knockOff.IEventSource.MessageReceived.Raise(knockOff, "Last");

		var lastArgs = knockOff.IEventSource.MessageReceived.LastRaiseArgs;
		Assert.NotNull(lastArgs);
		Assert.Same(knockOff, lastArgs.Value.sender);
		Assert.Equal("Last", lastArgs.Value.e);
	}

	[Fact]
	public void Event_Raise_TracksAllRaises()
	{
		var knockOff = new EventSourceKnockOff();
		IEventSource source = knockOff;

		source.MessageReceived += (sender, e) => { };

		knockOff.IEventSource.MessageReceived.Raise("First");
		knockOff.IEventSource.MessageReceived.Raise("Second");
		knockOff.IEventSource.MessageReceived.Raise("Third");

		var allRaises = knockOff.IEventSource.MessageReceived.AllRaises;
		Assert.Equal(3, allRaises.Count);
		Assert.Equal("First", allRaises[0].e);
		Assert.Equal("Second", allRaises[1].e);
		Assert.Equal("Third", allRaises[2].e);
	}

	[Fact]
	public void Event_EventHandler_Raise_WithNoArgs()
	{
		var knockOff = new EventSourceKnockOff();
		IEventSource source = knockOff;

		var invoked = false;
		source.OnCompleted += (sender, e) => { invoked = true; };

		knockOff.IEventSource.OnCompleted.Raise();

		Assert.True(invoked);
		Assert.True(knockOff.IEventSource.OnCompleted.WasRaised);
	}

	[Fact]
	public void Event_Action_SingleParam_Raise()
	{
		var knockOff = new EventSourceKnockOff();
		IEventSource source = knockOff;

		int? capturedProgress = null;
		source.OnProgress += (value) => { capturedProgress = value; };

		knockOff.IEventSource.OnProgress.Raise(75);

		Assert.Equal(75, capturedProgress);
		Assert.Equal(1, knockOff.IEventSource.OnProgress.RaiseCount);
		Assert.Equal(75, knockOff.IEventSource.OnProgress.LastRaiseArgs);
	}

	[Fact]
	public void Event_Action_MultipleParams_Raise()
	{
		var knockOff = new EventSourceKnockOff();
		IEventSource source = knockOff;

		string? capturedName = null;
		int? capturedValue = null;
		source.OnData += (name, value) =>
		{
			capturedName = name;
			capturedValue = value;
		};

		knockOff.IEventSource.OnData.Raise("DataPoint", 42);

		Assert.Equal("DataPoint", capturedName);
		Assert.Equal(42, capturedValue);

		var lastArgs = knockOff.IEventSource.OnData.LastRaiseArgs;
		Assert.NotNull(lastArgs);
		Assert.Equal("DataPoint", lastArgs.Value.arg1);
		Assert.Equal(42, lastArgs.Value.arg2);
	}

	[Fact]
	public void Event_AllRaises_TracksMultiParamTuples()
	{
		var knockOff = new EventSourceKnockOff();
		IEventSource source = knockOff;

		source.OnData += (name, value) => { };

		knockOff.IEventSource.OnData.Raise("First", 1);
		knockOff.IEventSource.OnData.Raise("Second", 2);
		knockOff.IEventSource.OnData.Raise("Third", 3);

		var allRaises = knockOff.IEventSource.OnData.AllRaises;
		Assert.Equal(3, allRaises.Count);
		Assert.Equal(("First", 1), allRaises[0]);
		Assert.Equal(("Second", 2), allRaises[1]);
		Assert.Equal(("Third", 3), allRaises[2]);
	}

	[Fact]
	public void Event_Reset_ClearsTrackingButNotHandlers()
	{
		var knockOff = new EventSourceKnockOff();
		IEventSource source = knockOff;

		var invoked = false;
		source.MessageReceived += (sender, e) => { invoked = true; };

		knockOff.IEventSource.MessageReceived.Raise("Before reset");
		Assert.True(knockOff.IEventSource.MessageReceived.WasRaised);
		Assert.Equal(1, knockOff.IEventSource.MessageReceived.RaiseCount);

		knockOff.IEventSource.MessageReceived.Reset();

		Assert.Equal(0, knockOff.IEventSource.MessageReceived.SubscribeCount);
		Assert.Equal(0, knockOff.IEventSource.MessageReceived.UnsubscribeCount);
		Assert.Equal(0, knockOff.IEventSource.MessageReceived.RaiseCount);
		Assert.Empty(knockOff.IEventSource.MessageReceived.AllRaises);

		// But handlers should still work
		invoked = false;
		knockOff.IEventSource.MessageReceived.Raise("After reset");
		Assert.True(invoked);
	}

	[Fact]
	public void Event_Clear_ClearsHandlersAndTracking()
	{
		var knockOff = new EventSourceKnockOff();
		IEventSource source = knockOff;

		var invoked = false;
		source.MessageReceived += (sender, e) => { invoked = true; };

		knockOff.IEventSource.MessageReceived.Raise("Before clear");
		Assert.True(invoked);

		knockOff.IEventSource.MessageReceived.Clear();

		Assert.Equal(0, knockOff.IEventSource.MessageReceived.RaiseCount);
		Assert.False(knockOff.IEventSource.MessageReceived.HasSubscribers);

		// Handlers should also be cleared
		invoked = false;
		knockOff.IEventSource.MessageReceived.Raise("After clear");
		Assert.False(invoked);
	}

	[Fact]
	public void Event_NoSubscribers_RaiseDoesNotThrow()
	{
		var knockOff = new EventSourceKnockOff();

		var exception = Record.Exception(() =>
		{
			knockOff.IEventSource.MessageReceived.Raise("No subscribers");
		});

		Assert.Null(exception);
		Assert.True(knockOff.IEventSource.MessageReceived.WasRaised);
		Assert.Equal(1, knockOff.IEventSource.MessageReceived.RaiseCount);
	}

	[Fact]
	public void Event_MultipleSubscribers_AllInvoked()
	{
		var knockOff = new EventSourceKnockOff();
		IEventSource source = knockOff;

		var invocations = new List<int>();
		source.MessageReceived += (sender, e) => invocations.Add(1);
		source.MessageReceived += (sender, e) => invocations.Add(2);
		source.MessageReceived += (sender, e) => invocations.Add(3);

		Assert.Equal(3, knockOff.IEventSource.MessageReceived.SubscribeCount);

		knockOff.IEventSource.MessageReceived.Raise("Test");

		Assert.Equal(3, invocations.Count);
		Assert.Contains(1, invocations);
		Assert.Contains(2, invocations);
		Assert.Contains(3, invocations);
	}
}
