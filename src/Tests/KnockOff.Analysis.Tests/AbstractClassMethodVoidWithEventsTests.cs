// ============================================================================
// AbstractClassMethodVoidWithEventsTests: Edge case tests for an abstract class
// with an abstract void method AND an abstract event, verifying both fire together.
// Inspired by Rocks.Analysis.IntegrationTests.AbstractClassMethodVoidWithEventsTests
//
// Target class:
//   public abstract class AbstractClassMethodVoidWithEvents
//   {
//       public abstract void NoParameters();
//       public abstract event EventHandler? MyEvent;
//   }
//
// Key difference from ClassMethodVoidWithEventsTests: members are ABSTRACT,
// not virtual. Abstract members have no base implementation, so unconfigured
// calls simply return immediately (no-op).
//
// Applicable patterns (class only):
// - Pattern 3 (Standalone Class): [KnockOffBase<AbstractClassMethodVoidWithEvents>]
// - Pattern 6 (Inline Class): [KnockOff<AbstractClassMethodVoidWithEvents>]
// ============================================================================

using KnockOff;

// ============================================================================
// TYPE DEFINITIONS
// ============================================================================

namespace KnockOff.Analysis.Tests.AbstractClassMethodVoidWithEventsTestTypes
{
	public abstract class AbstractClassMethodVoidWithEvents
	{
		public abstract void NoParameters();
		public abstract event EventHandler? MyEvent;
	}

	// Pattern 3: Standalone class stub
	[KnockOffBase<AbstractClassMethodVoidWithEvents>]
	public partial class AbstractClassMethodVoidWithEventsStandaloneKnockOff
	{
	}
}

// ============================================================================
// INLINE PATTERN DECLARATIONS + TESTS
// ============================================================================

namespace KnockOff.Analysis.Tests
{
	using KnockOff.Analysis.Tests.AbstractClassMethodVoidWithEventsTestTypes;

	// Pattern 6: Inline class stub
	[KnockOff<AbstractClassMethodVoidWithEvents>]
	public partial class AbstractClassMethodVoidWithEventsInlineTests
	{
	}

	public class AbstractClassMethodVoidWithEventsTests
	{
		// ====================================================================
		// Scenario 1: Callback raises event — assert event fired
		// ====================================================================

		#region Standalone Class: Callback raises event

		[Fact]
		public void CallbackRaisesEvent_StandaloneClass_EventFired()
		{
			var stub = new AbstractClassMethodVoidWithEventsStandaloneKnockOff();
			AbstractClassMethodVoidWithEvents obj = stub.Object;

			stub.NoParameters.Call(() =>
			{
				stub.MyEvent.Raise(obj, EventArgs.Empty);
			});

			var wasEventRaised = false;
			obj.MyEvent += (s, e) => wasEventRaised = true;

			obj.NoParameters();

			Assert.True(wasEventRaised);
		}

		#endregion

		#region Inline Class: Callback raises event

		[Fact]
		public void CallbackRaisesEvent_InlineClass_EventFired()
		{
			var stub = new AbstractClassMethodVoidWithEventsInlineTests.Stubs.AbstractClassMethodVoidWithEvents();
			AbstractClassMethodVoidWithEvents obj = stub.Object;

			stub.NoParameters.Call(() =>
			{
				stub.MyEvent.Raise(obj, EventArgs.Empty);
			});

			var wasEventRaised = false;
			obj.MyEvent += (s, e) => wasEventRaised = true;

			obj.NoParameters();

			Assert.True(wasEventRaised);
		}

		#endregion

		// ====================================================================
		// Scenario 2: Multiple calls — event count matches call count
		// ====================================================================

		#region Standalone Class: Multiple calls track event count

		[Fact]
		public void MultipleCalls_StandaloneClass_EventCountMatchesCallCount()
		{
			var stub = new AbstractClassMethodVoidWithEventsStandaloneKnockOff();
			AbstractClassMethodVoidWithEvents obj = stub.Object;

			stub.NoParameters.Call(() =>
			{
				stub.MyEvent.Raise(obj, EventArgs.Empty);
			});

			var eventRaisedCount = 0;
			obj.MyEvent += (s, e) => eventRaisedCount++;

			obj.NoParameters();
			obj.NoParameters();

			Assert.Equal(2, eventRaisedCount);
		}

		#endregion

		#region Inline Class: Multiple calls track event count

		[Fact]
		public void MultipleCalls_InlineClass_EventCountMatchesCallCount()
		{
			var stub = new AbstractClassMethodVoidWithEventsInlineTests.Stubs.AbstractClassMethodVoidWithEvents();
			AbstractClassMethodVoidWithEvents obj = stub.Object;

			stub.NoParameters.Call(() =>
			{
				stub.MyEvent.Raise(obj, EventArgs.Empty);
			});

			var eventRaisedCount = 0;
			obj.MyEvent += (s, e) => eventRaisedCount++;

			obj.NoParameters();
			obj.NoParameters();

			Assert.Equal(2, eventRaisedCount);
		}

		#endregion

		// ====================================================================
		// Scenario 3: Combined callback invoked flag + event raised flag
		// ====================================================================

		#region Standalone Class: Callback + event both verified

		[Fact]
		public void CallbackAndEvent_StandaloneClass_BothVerified()
		{
			var stub = new AbstractClassMethodVoidWithEventsStandaloneKnockOff();
			AbstractClassMethodVoidWithEvents obj = stub.Object;

			var wasCallbackInvoked = false;
			stub.NoParameters.Call(() =>
			{
				wasCallbackInvoked = true;
				stub.MyEvent.Raise(obj, EventArgs.Empty);
			});

			var wasEventRaised = false;
			obj.MyEvent += (s, e) => wasEventRaised = true;

			obj.NoParameters();

			Assert.True(wasCallbackInvoked);
			Assert.True(wasEventRaised);
		}

		#endregion

		#region Inline Class: Callback + event both verified

		[Fact]
		public void CallbackAndEvent_InlineClass_BothVerified()
		{
			var stub = new AbstractClassMethodVoidWithEventsInlineTests.Stubs.AbstractClassMethodVoidWithEvents();
			AbstractClassMethodVoidWithEvents obj = stub.Object;

			var wasCallbackInvoked = false;
			stub.NoParameters.Call(() =>
			{
				wasCallbackInvoked = true;
				stub.MyEvent.Raise(obj, EventArgs.Empty);
			});

			var wasEventRaised = false;
			obj.MyEvent += (s, e) => wasEventRaised = true;

			obj.NoParameters();

			Assert.True(wasCallbackInvoked);
			Assert.True(wasEventRaised);
		}

		#endregion

		// ====================================================================
		// Scenario 4: Event add verification alongside method call
		// ====================================================================

		#region Standalone Class: Event add tracked alongside method call

		[Fact]
		public void EventAddTracked_StandaloneClass_VerifyBothMethodAndEvent()
		{
			var stub = new AbstractClassMethodVoidWithEventsStandaloneKnockOff();
			AbstractClassMethodVoidWithEvents obj = stub.Object;

			stub.NoParameters.Call(() =>
			{
				stub.MyEvent.Raise(obj, EventArgs.Empty);
			});

			obj.MyEvent += (s, e) => { };

			obj.NoParameters();

			stub.NoParameters.Verify(Called.Once);
			stub.MyEvent.VerifyAdd(Called.Once);
		}

		#endregion

		#region Inline Class: Event add tracked alongside method call

		[Fact]
		public void EventAddTracked_InlineClass_VerifyBothMethodAndEvent()
		{
			var stub = new AbstractClassMethodVoidWithEventsInlineTests.Stubs.AbstractClassMethodVoidWithEvents();
			AbstractClassMethodVoidWithEvents obj = stub.Object;

			stub.NoParameters.Call(() =>
			{
				stub.MyEvent.Raise(obj, EventArgs.Empty);
			});

			obj.MyEvent += (s, e) => { };

			obj.NoParameters();

			stub.NoParameters.Verify(Called.Once);
			stub.MyEvent.VerifyAdd(Called.Once);
		}

		#endregion

		// ====================================================================
		// Scenario 5: Unconfigured — no callback, no event
		// ====================================================================

		#region Standalone Class: Unconfigured, no event

		[Fact]
		public void Unconfigured_StandaloneClass_NoOpNoEvent()
		{
			var stub = new AbstractClassMethodVoidWithEventsStandaloneKnockOff();
			AbstractClassMethodVoidWithEvents obj = stub.Object;

			var wasEventRaised = false;
			obj.MyEvent += (s, e) => wasEventRaised = true;

			obj.NoParameters();

			Assert.False(wasEventRaised);
		}

		#endregion

		#region Inline Class: Unconfigured, no event

		[Fact]
		public void Unconfigured_InlineClass_NoOpNoEvent()
		{
			var stub = new AbstractClassMethodVoidWithEventsInlineTests.Stubs.AbstractClassMethodVoidWithEvents();
			AbstractClassMethodVoidWithEvents obj = stub.Object;

			var wasEventRaised = false;
			obj.MyEvent += (s, e) => wasEventRaised = true;

			obj.NoParameters();

			Assert.False(wasEventRaised);
		}

		#endregion
	}
}
