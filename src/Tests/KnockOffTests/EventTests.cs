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

		Assert.False(knockOff.MessageReceived.HasSubscribers);
		Assert.Equal(0, knockOff.MessageReceived.AddCount);

		source.MessageReceived += (sender, e) => { };

		Assert.True(knockOff.MessageReceived.HasSubscribers);
		Assert.Equal(1, knockOff.MessageReceived.AddCount);
	}

	[Fact]
	public void Event_Unsubscribe_TracksUnsubscription()
	{
		var knockOff = new EventSourceKnockOff();
		IEventSource source = knockOff;

		EventHandler<string> handler = (sender, e) => { };
		source.MessageReceived += handler;
		source.MessageReceived -= handler;

		Assert.Equal(1, knockOff.MessageReceived.AddCount);
		Assert.Equal(1, knockOff.MessageReceived.RemoveCount);
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

		knockOff.MessageReceived.Raise(knockOff, "Hello World");

		Assert.Same(knockOff, receivedSender);
		Assert.Equal("Hello World", receivedMessage);
	}

	[Fact]
	public void Event_Raise_MultipleInvocations()
	{
		var knockOff = new EventSourceKnockOff();
		IEventSource source = knockOff;

		var invocationCount = 0;
		source.MessageReceived += (sender, e) => invocationCount++;

		knockOff.MessageReceived.Raise(null, "First");
		knockOff.MessageReceived.Raise(null, "Second");

		Assert.Equal(2, invocationCount);
	}

	[Fact]
	public void Event_EventHandler_Raise_WithSenderAndArgs()
	{
		var knockOff = new EventSourceKnockOff();
		IEventSource source = knockOff;

		var invoked = false;
		source.OnCompleted += (sender, e) => { invoked = true; };

		knockOff.OnCompleted.Raise(null, EventArgs.Empty);

		Assert.True(invoked);
	}

	[Fact]
	public void Event_Action_SingleParam_Raise()
	{
		var knockOff = new EventSourceKnockOff();
		IEventSource source = knockOff;

		int? capturedProgress = null;
		source.OnProgress += (value) => { capturedProgress = value; };

		knockOff.OnProgress.Raise(75);

		Assert.Equal(75, capturedProgress);
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

		knockOff.OnData.Raise("DataPoint", 42);

		Assert.Equal("DataPoint", capturedName);
		Assert.Equal(42, capturedValue);
	}

	[Fact]
	public void Event_Reset_ClearsTrackingAndHandlers()
	{
		var knockOff = new EventSourceKnockOff();
		IEventSource source = knockOff;

		source.MessageReceived += (sender, e) => { };

		Assert.True(knockOff.MessageReceived.HasSubscribers);
		Assert.Equal(1, knockOff.MessageReceived.AddCount);

		knockOff.MessageReceived.Reset();

		Assert.Equal(0, knockOff.MessageReceived.AddCount);
		Assert.Equal(0, knockOff.MessageReceived.RemoveCount);
		Assert.False(knockOff.MessageReceived.HasSubscribers);
	}

	[Fact]
	public void Event_NoSubscribers_RaiseDoesNotThrow()
	{
		var knockOff = new EventSourceKnockOff();

		var exception = Record.Exception(() =>
		{
			knockOff.MessageReceived.Raise(null, "No subscribers");
		});

		Assert.Null(exception);
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

		Assert.Equal(3, knockOff.MessageReceived.AddCount);

		knockOff.MessageReceived.Raise(null, "Test");

		Assert.Equal(3, invocations.Count);
		Assert.Contains(1, invocations);
		Assert.Contains(2, invocations);
		Assert.Contains(3, invocations);
	}
}
