// ============================================================================
// InterfaceMethodReturnWithEventsTests: Edge case tests for an interface with
// a method returning int AND an event, verifying both fire together.
// Inspired by Rocks.Analysis.IntegrationTests.InterfaceMethodReturnWithEventsTests
//
// Target interface:
//   public interface IInterfaceMethodReturnWithEvents
//   {
//       int NoParameters();
//       event EventHandler MyEvent;
//   }
//
// Tests exercise the INTERFACE pipeline for:
// 1. Callback raises event AND returns value — assert both
// 2. Multiple calls — assert event count matches call count
// 3. Combined callback invoked flag + event raised flag + return value
// 4. Event add verification alongside method call
//
// Applicable patterns (interface only, non-generic):
// - Pattern 1 (Standalone): [KnockOff] partial class Stub : IInterfaceMethodReturnWithEvents {}
// - Pattern 5 (Inline): [KnockOff<IInterfaceMethodReturnWithEvents>]
// ============================================================================

using KnockOff;

// ============================================================================
// TYPE DEFINITIONS
// ============================================================================

namespace KnockOff.Analysis.Tests.InterfaceMethodReturnWithEventsTestTypes
{
	public interface IInterfaceMethodReturnWithEvents
	{
		int NoParameters();
		event EventHandler MyEvent;
	}

	// Pattern 1: Standalone interface stub
	[KnockOff]
	public partial class InterfaceMethodReturnWithEventsStandaloneKnockOff : IInterfaceMethodReturnWithEvents
	{
	}
}

// ============================================================================
// INLINE PATTERN DECLARATIONS + TESTS
// ============================================================================

namespace KnockOff.Analysis.Tests
{
	using KnockOff.Analysis.Tests.InterfaceMethodReturnWithEventsTestTypes;

	// Pattern 5: Inline interface stub
	[KnockOff<IInterfaceMethodReturnWithEvents>]
	public partial class InterfaceMethodReturnWithEventsInlineTests
	{
	}

	public class InterfaceMethodReturnWithEventsTests
	{
		// ====================================================================
		// Scenario 1: Callback raises event and returns value
		// Configure a callback that raises the event and returns a value.
		// Assert both the return value and that the event was fired.
		// ====================================================================

		#region Standalone: Callback raises event and returns value

		[Fact]
		public void CallbackRaisesEventAndReturns_Standalone_BothFire()
		{
			var stub = new InterfaceMethodReturnWithEventsStandaloneKnockOff();
			IInterfaceMethodReturnWithEvents service = stub;

			stub.NoParameters.Call(() =>
			{
				stub.MyEvent.Raise(stub, EventArgs.Empty);
				return 42;
			});

			var wasEventRaised = false;
			service.MyEvent += (s, e) => wasEventRaised = true;

			int value = service.NoParameters();

			Assert.Equal(42, value);
			Assert.True(wasEventRaised);
		}

		#endregion

		#region Inline: Callback raises event and returns value

		[Fact]
		public void CallbackRaisesEventAndReturns_Inline_BothFire()
		{
			var stub = new InterfaceMethodReturnWithEventsInlineTests.Stubs.IInterfaceMethodReturnWithEvents();
			IInterfaceMethodReturnWithEvents service = stub;

			stub.NoParameters.Call(() =>
			{
				stub.MyEvent.Raise(stub, EventArgs.Empty);
				return 42;
			});

			var wasEventRaised = false;
			service.MyEvent += (s, e) => wasEventRaised = true;

			int value = service.NoParameters();

			Assert.Equal(42, value);
			Assert.True(wasEventRaised);
		}

		#endregion

		// ====================================================================
		// Scenario 2: Multiple calls — event count matches call count
		// Call the method multiple times, verify event fires each time.
		// ====================================================================

		#region Standalone: Multiple calls track event count

		[Fact]
		public void MultipleCalls_Standalone_EventCountMatchesCallCount()
		{
			var stub = new InterfaceMethodReturnWithEventsStandaloneKnockOff();
			IInterfaceMethodReturnWithEvents service = stub;

			stub.NoParameters.Call(() =>
			{
				stub.MyEvent.Raise(stub, EventArgs.Empty);
				return 3;
			});

			var eventRaisedCount = 0;
			service.MyEvent += (s, e) => eventRaisedCount++;

			int v1 = service.NoParameters();
			int v2 = service.NoParameters();

			Assert.Equal(3, v1);
			Assert.Equal(3, v2);
			Assert.Equal(2, eventRaisedCount);
		}

		#endregion

		#region Inline: Multiple calls track event count

		[Fact]
		public void MultipleCalls_Inline_EventCountMatchesCallCount()
		{
			var stub = new InterfaceMethodReturnWithEventsInlineTests.Stubs.IInterfaceMethodReturnWithEvents();
			IInterfaceMethodReturnWithEvents service = stub;

			stub.NoParameters.Call(() =>
			{
				stub.MyEvent.Raise(stub, EventArgs.Empty);
				return 3;
			});

			var eventRaisedCount = 0;
			service.MyEvent += (s, e) => eventRaisedCount++;

			int v1 = service.NoParameters();
			int v2 = service.NoParameters();

			Assert.Equal(3, v1);
			Assert.Equal(3, v2);
			Assert.Equal(2, eventRaisedCount);
		}

		#endregion

		// ====================================================================
		// Scenario 3: Callback invoked flag + event raised flag + return value
		// Full combination: callback is invoked, event fires, value returned.
		// ====================================================================

		#region Standalone: Callback + event + return value verified

		[Fact]
		public void CallbackAndEvent_Standalone_AllThreeVerified()
		{
			var stub = new InterfaceMethodReturnWithEventsStandaloneKnockOff();
			IInterfaceMethodReturnWithEvents service = stub;

			var wasCallbackInvoked = false;
			stub.NoParameters.Call(() =>
			{
				wasCallbackInvoked = true;
				stub.MyEvent.Raise(stub, EventArgs.Empty);
				return 99;
			});

			var wasEventRaised = false;
			service.MyEvent += (s, e) => wasEventRaised = true;

			int value = service.NoParameters();

			Assert.Equal(99, value);
			Assert.True(wasCallbackInvoked);
			Assert.True(wasEventRaised);
		}

		#endregion

		#region Inline: Callback + event + return value verified

		[Fact]
		public void CallbackAndEvent_Inline_AllThreeVerified()
		{
			var stub = new InterfaceMethodReturnWithEventsInlineTests.Stubs.IInterfaceMethodReturnWithEvents();
			IInterfaceMethodReturnWithEvents service = stub;

			var wasCallbackInvoked = false;
			stub.NoParameters.Call(() =>
			{
				wasCallbackInvoked = true;
				stub.MyEvent.Raise(stub, EventArgs.Empty);
				return 99;
			});

			var wasEventRaised = false;
			service.MyEvent += (s, e) => wasEventRaised = true;

			int value = service.NoParameters();

			Assert.Equal(99, value);
			Assert.True(wasCallbackInvoked);
			Assert.True(wasEventRaised);
		}

		#endregion

		// ====================================================================
		// Scenario 4: Event add verification alongside method call
		// Verify that subscribing to the event is tracked alongside method
		// invocation.
		// ====================================================================

		#region Standalone: Event add tracked alongside method call

		[Fact]
		public void EventAddTracked_Standalone_VerifyBothMethodAndEvent()
		{
			var stub = new InterfaceMethodReturnWithEventsStandaloneKnockOff();
			IInterfaceMethodReturnWithEvents service = stub;

			stub.NoParameters.Call(() =>
			{
				stub.MyEvent.Raise(stub, EventArgs.Empty);
				return 7;
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
			var stub = new InterfaceMethodReturnWithEventsInlineTests.Stubs.IInterfaceMethodReturnWithEvents();
			IInterfaceMethodReturnWithEvents service = stub;

			stub.NoParameters.Call(() =>
			{
				stub.MyEvent.Raise(stub, EventArgs.Empty);
				return 7;
			});

			service.MyEvent += (s, e) => { };

			service.NoParameters();

			stub.NoParameters.Verify(Called.Once);
			stub.MyEvent.VerifyAdd(Called.Once);
		}

		#endregion

		// ====================================================================
		// Scenario 5: Unconfigured method returns default, event not raised
		// When method is not configured, it returns default(int).
		// No event is raised because no callback exists.
		// ====================================================================

		#region Standalone: Unconfigured, no event

		[Fact]
		public void Unconfigured_Standalone_ReturnsDefaultNoEvent()
		{
			var stub = new InterfaceMethodReturnWithEventsStandaloneKnockOff();
			IInterfaceMethodReturnWithEvents service = stub;

			var wasEventRaised = false;
			service.MyEvent += (s, e) => wasEventRaised = true;

			int value = service.NoParameters();

			Assert.Equal(0, value);
			Assert.False(wasEventRaised);
		}

		#endregion

		#region Inline: Unconfigured, no event

		[Fact]
		public void Unconfigured_Inline_ReturnsDefaultNoEvent()
		{
			var stub = new InterfaceMethodReturnWithEventsInlineTests.Stubs.IInterfaceMethodReturnWithEvents();
			IInterfaceMethodReturnWithEvents service = stub;

			var wasEventRaised = false;
			service.MyEvent += (s, e) => wasEventRaised = true;

			int value = service.NoParameters();

			Assert.Equal(0, value);
			Assert.False(wasEventRaised);
		}

		#endregion
	}
}
