// ============================================================================
// InterfaceMethodVoidWithEventsTests: Edge case tests for an interface with a
// void method AND an event, verifying both fire together.
// Inspired by Rocks.Analysis.IntegrationTests.InterfaceMethodVoidWithEventsTests
//
// Target interface:
//   public interface IInterfaceMethodVoidWithEvents
//   {
//       void NoParameters();
//       event EventHandler MyEvent;
//   }
//
// Tests exercise the INTERFACE pipeline for:
// 1. Callback raises event — assert event fired
// 2. Multiple calls — assert event count matches call count
// 3. Combined callback invoked flag + event raised flag
// 4. Event add verification alongside method call
//
// Applicable patterns (interface only, non-generic):
// - Pattern 1 (Standalone): [KnockOff] partial class Stub : IInterfaceMethodVoidWithEvents {}
// - Pattern 5 (Inline): [KnockOff<IInterfaceMethodVoidWithEvents>]
// ============================================================================

using KnockOff;

// ============================================================================
// TYPE DEFINITIONS
// ============================================================================

namespace KnockOff.Analysis.Tests.InterfaceMethodVoidWithEventsTestTypes
{
	public interface IInterfaceMethodVoidWithEvents
	{
		void NoParameters();
		event EventHandler MyEvent;
	}

	// Pattern 1: Standalone interface stub
	[KnockOff]
	public partial class InterfaceMethodVoidWithEventsStandaloneKnockOff : IInterfaceMethodVoidWithEvents
	{
	}
}

// ============================================================================
// INLINE PATTERN DECLARATIONS + TESTS
// ============================================================================

namespace KnockOff.Analysis.Tests
{
	using KnockOff.Analysis.Tests.InterfaceMethodVoidWithEventsTestTypes;

	// Pattern 5: Inline interface stub
	[KnockOff<IInterfaceMethodVoidWithEvents>]
	public partial class InterfaceMethodVoidWithEventsInlineTests
	{
	}

	public class InterfaceMethodVoidWithEventsTests
	{
		// ====================================================================
		// Scenario 1: Callback raises event — assert event fired
		// Configure a void callback that raises the event. Verify the event
		// handler was invoked.
		// ====================================================================

		#region Standalone: Callback raises event

		[Fact]
		public void CallbackRaisesEvent_Standalone_EventFired()
		{
			var stub = new InterfaceMethodVoidWithEventsStandaloneKnockOff();
			IInterfaceMethodVoidWithEvents service = stub;

			stub.NoParameters.Call(() =>
			{
				stub.MyEvent.Raise(stub, EventArgs.Empty);
			});

			var wasEventRaised = false;
			service.MyEvent += (s, e) => wasEventRaised = true;

			service.NoParameters();

			Assert.True(wasEventRaised);
		}

		#endregion

		#region Inline: Callback raises event

		[Fact]
		public void CallbackRaisesEvent_Inline_EventFired()
		{
			var stub = new InterfaceMethodVoidWithEventsInlineTests.Stubs.IInterfaceMethodVoidWithEvents();
			IInterfaceMethodVoidWithEvents service = stub;

			stub.NoParameters.Call(() =>
			{
				stub.MyEvent.Raise(stub, EventArgs.Empty);
			});

			var wasEventRaised = false;
			service.MyEvent += (s, e) => wasEventRaised = true;

			service.NoParameters();

			Assert.True(wasEventRaised);
		}

		#endregion

		// ====================================================================
		// Scenario 2: Multiple calls — event count matches call count
		// Call the void method multiple times, verify event fires each time.
		// ====================================================================

		#region Standalone: Multiple calls track event count

		[Fact]
		public void MultipleCalls_Standalone_EventCountMatchesCallCount()
		{
			var stub = new InterfaceMethodVoidWithEventsStandaloneKnockOff();
			IInterfaceMethodVoidWithEvents service = stub;

			stub.NoParameters.Call(() =>
			{
				stub.MyEvent.Raise(stub, EventArgs.Empty);
			});

			var eventRaisedCount = 0;
			service.MyEvent += (s, e) => eventRaisedCount++;

			service.NoParameters();
			service.NoParameters();

			Assert.Equal(2, eventRaisedCount);
		}

		#endregion

		#region Inline: Multiple calls track event count

		[Fact]
		public void MultipleCalls_Inline_EventCountMatchesCallCount()
		{
			var stub = new InterfaceMethodVoidWithEventsInlineTests.Stubs.IInterfaceMethodVoidWithEvents();
			IInterfaceMethodVoidWithEvents service = stub;

			stub.NoParameters.Call(() =>
			{
				stub.MyEvent.Raise(stub, EventArgs.Empty);
			});

			var eventRaisedCount = 0;
			service.MyEvent += (s, e) => eventRaisedCount++;

			service.NoParameters();
			service.NoParameters();

			Assert.Equal(2, eventRaisedCount);
		}

		#endregion

		// ====================================================================
		// Scenario 3: Combined callback invoked flag + event raised flag
		// Verify both the callback and the event independently.
		// ====================================================================

		#region Standalone: Callback + event both verified

		[Fact]
		public void CallbackAndEvent_Standalone_BothVerified()
		{
			var stub = new InterfaceMethodVoidWithEventsStandaloneKnockOff();
			IInterfaceMethodVoidWithEvents service = stub;

			var wasCallbackInvoked = false;
			stub.NoParameters.Call(() =>
			{
				wasCallbackInvoked = true;
				stub.MyEvent.Raise(stub, EventArgs.Empty);
			});

			var wasEventRaised = false;
			service.MyEvent += (s, e) => wasEventRaised = true;

			service.NoParameters();

			Assert.True(wasCallbackInvoked);
			Assert.True(wasEventRaised);
		}

		#endregion

		#region Inline: Callback + event both verified

		[Fact]
		public void CallbackAndEvent_Inline_BothVerified()
		{
			var stub = new InterfaceMethodVoidWithEventsInlineTests.Stubs.IInterfaceMethodVoidWithEvents();
			IInterfaceMethodVoidWithEvents service = stub;

			var wasCallbackInvoked = false;
			stub.NoParameters.Call(() =>
			{
				wasCallbackInvoked = true;
				stub.MyEvent.Raise(stub, EventArgs.Empty);
			});

			var wasEventRaised = false;
			service.MyEvent += (s, e) => wasEventRaised = true;

			service.NoParameters();

			Assert.True(wasCallbackInvoked);
			Assert.True(wasEventRaised);
		}

		#endregion

		// ====================================================================
		// Scenario 4: Event add verification alongside method call
		// Verify that subscribing to the event is tracked alongside method
		// invocation tracking.
		// ====================================================================

		#region Standalone: Event add tracked alongside method call

		[Fact]
		public void EventAddTracked_Standalone_VerifyBothMethodAndEvent()
		{
			var stub = new InterfaceMethodVoidWithEventsStandaloneKnockOff();
			IInterfaceMethodVoidWithEvents service = stub;

			stub.NoParameters.Call(() =>
			{
				stub.MyEvent.Raise(stub, EventArgs.Empty);
			});

			service.MyEvent += (s, e) => { };

			service.NoParameters();

			stub.NoParameters.Verify(Called.Once);
			stub.MyEvent.VerifyAdd(Called.Once);
		}

		#endregion

		#region Inline: Event add tracked alongside method call

		[Fact]
		public void EventAddTracked_Inline_VerifyBothMethodAndEvent()
		{
			var stub = new InterfaceMethodVoidWithEventsInlineTests.Stubs.IInterfaceMethodVoidWithEvents();
			IInterfaceMethodVoidWithEvents service = stub;

			stub.NoParameters.Call(() =>
			{
				stub.MyEvent.Raise(stub, EventArgs.Empty);
			});

			service.MyEvent += (s, e) => { };

			service.NoParameters();

			stub.NoParameters.Verify(Called.Once);
			stub.MyEvent.VerifyAdd(Called.Once);
		}

		#endregion

		// ====================================================================
		// Scenario 5: Unconfigured void method does not throw, event not raised
		// When method is not configured, it's a no-op.
		// No event is raised because no callback exists.
		// ====================================================================

		#region Standalone: Unconfigured, no event

		[Fact]
		public void Unconfigured_Standalone_DoesNotThrowNoEvent()
		{
			var stub = new InterfaceMethodVoidWithEventsStandaloneKnockOff();
			IInterfaceMethodVoidWithEvents service = stub;

			var wasEventRaised = false;
			service.MyEvent += (s, e) => wasEventRaised = true;

			var exception = Record.Exception(() => service.NoParameters());

			Assert.Null(exception);
			Assert.False(wasEventRaised);
		}

		#endregion

		#region Inline: Unconfigured, no event

		[Fact]
		public void Unconfigured_Inline_DoesNotThrowNoEvent()
		{
			var stub = new InterfaceMethodVoidWithEventsInlineTests.Stubs.IInterfaceMethodVoidWithEvents();
			IInterfaceMethodVoidWithEvents service = stub;

			var wasEventRaised = false;
			service.MyEvent += (s, e) => wasEventRaised = true;

			var exception = Record.Exception(() => service.NoParameters());

			Assert.Null(exception);
			Assert.False(wasEventRaised);
		}

		#endregion
	}
}
