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

		Assert.False(knockOff.Spy.MessageReceived.HasSubscribers);
		Assert.Equal(0, knockOff.Spy.MessageReceived.SubscribeCount);

		source.MessageReceived += (sender, e) => { };

		Assert.True(knockOff.Spy.MessageReceived.HasSubscribers);
		Assert.Equal(1, knockOff.Spy.MessageReceived.SubscribeCount);
	}

	[Fact]
	public void Event_Unsubscribe_TracksUnsubscription()
	{
		var knockOff = new EventSourceKnockOff();
		IEventSource source = knockOff;

		EventHandler<string> handler = (sender, e) => { };
		source.MessageReceived += handler;
		source.MessageReceived -= handler;

		Assert.Equal(1, knockOff.Spy.MessageReceived.SubscribeCount);
		Assert.Equal(1, knockOff.Spy.MessageReceived.UnsubscribeCount);
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

		knockOff.Spy.MessageReceived.Raise(knockOff, "Hello World");

		Assert.Same(knockOff, receivedSender);
		Assert.Equal("Hello World", receivedMessage);
	}

	[Fact]
	public void Event_Raise_TracksRaiseCount()
	{
		var knockOff = new EventSourceKnockOff();
		IEventSource source = knockOff;

		source.MessageReceived += (sender, e) => { };

		Assert.False(knockOff.Spy.MessageReceived.WasRaised);
		Assert.Equal(0, knockOff.Spy.MessageReceived.RaiseCount);

		knockOff.Spy.MessageReceived.Raise("First");
		knockOff.Spy.MessageReceived.Raise("Second");

		Assert.True(knockOff.Spy.MessageReceived.WasRaised);
		Assert.Equal(2, knockOff.Spy.MessageReceived.RaiseCount);
	}

	[Fact]
	public void Event_Raise_TracksLastRaiseArgs()
	{
		var knockOff = new EventSourceKnockOff();
		IEventSource source = knockOff;

		source.MessageReceived += (sender, e) => { };

		knockOff.Spy.MessageReceived.Raise(knockOff, "First");
		knockOff.Spy.MessageReceived.Raise(knockOff, "Second");
		knockOff.Spy.MessageReceived.Raise(knockOff, "Last");

		var lastArgs = knockOff.Spy.MessageReceived.LastRaiseArgs;
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

		knockOff.Spy.MessageReceived.Raise("First");
		knockOff.Spy.MessageReceived.Raise("Second");
		knockOff.Spy.MessageReceived.Raise("Third");

		var allRaises = knockOff.Spy.MessageReceived.AllRaises;
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

		knockOff.Spy.OnCompleted.Raise();

		Assert.True(invoked);
		Assert.True(knockOff.Spy.OnCompleted.WasRaised);
	}

	[Fact]
	public void Event_Action_SingleParam_Raise()
	{
		var knockOff = new EventSourceKnockOff();
		IEventSource source = knockOff;

		int? capturedProgress = null;
		source.OnProgress += (value) => { capturedProgress = value; };

		knockOff.Spy.OnProgress.Raise(75);

		Assert.Equal(75, capturedProgress);
		Assert.Equal(1, knockOff.Spy.OnProgress.RaiseCount);
		Assert.Equal(75, knockOff.Spy.OnProgress.LastRaiseArgs);
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

		knockOff.Spy.OnData.Raise("DataPoint", 42);

		Assert.Equal("DataPoint", capturedName);
		Assert.Equal(42, capturedValue);

		var lastArgs = knockOff.Spy.OnData.LastRaiseArgs;
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

		knockOff.Spy.OnData.Raise("First", 1);
		knockOff.Spy.OnData.Raise("Second", 2);
		knockOff.Spy.OnData.Raise("Third", 3);

		var allRaises = knockOff.Spy.OnData.AllRaises;
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

		knockOff.Spy.MessageReceived.Raise("Before reset");
		Assert.True(knockOff.Spy.MessageReceived.WasRaised);
		Assert.Equal(1, knockOff.Spy.MessageReceived.RaiseCount);

		knockOff.Spy.MessageReceived.Reset();

		Assert.Equal(0, knockOff.Spy.MessageReceived.SubscribeCount);
		Assert.Equal(0, knockOff.Spy.MessageReceived.UnsubscribeCount);
		Assert.Equal(0, knockOff.Spy.MessageReceived.RaiseCount);
		Assert.Empty(knockOff.Spy.MessageReceived.AllRaises);

		// But handlers should still work
		invoked = false;
		knockOff.Spy.MessageReceived.Raise("After reset");
		Assert.True(invoked);
	}

	[Fact]
	public void Event_Clear_ClearsHandlersAndTracking()
	{
		var knockOff = new EventSourceKnockOff();
		IEventSource source = knockOff;

		var invoked = false;
		source.MessageReceived += (sender, e) => { invoked = true; };

		knockOff.Spy.MessageReceived.Raise("Before clear");
		Assert.True(invoked);

		knockOff.Spy.MessageReceived.Clear();

		Assert.Equal(0, knockOff.Spy.MessageReceived.RaiseCount);
		Assert.False(knockOff.Spy.MessageReceived.HasSubscribers);

		// Handlers should also be cleared
		invoked = false;
		knockOff.Spy.MessageReceived.Raise("After clear");
		Assert.False(invoked);
	}

	[Fact]
	public void Event_NoSubscribers_RaiseDoesNotThrow()
	{
		var knockOff = new EventSourceKnockOff();

		var exception = Record.Exception(() =>
		{
			knockOff.Spy.MessageReceived.Raise("No subscribers");
		});

		Assert.Null(exception);
		Assert.True(knockOff.Spy.MessageReceived.WasRaised);
		Assert.Equal(1, knockOff.Spy.MessageReceived.RaiseCount);
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

		Assert.Equal(3, knockOff.Spy.MessageReceived.SubscribeCount);

		knockOff.Spy.MessageReceived.Raise("Test");

		Assert.Equal(3, invocations.Count);
		Assert.Contains(1, invocations);
		Assert.Contains(2, invocations);
		Assert.Contains(3, invocations);
	}
}
