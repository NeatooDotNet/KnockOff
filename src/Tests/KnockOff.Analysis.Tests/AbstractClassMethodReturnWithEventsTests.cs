// ============================================================================
// AbstractClassMethodReturnWithEventsTests: Edge case tests for an abstract class
// with an abstract method returning int AND an abstract event, verifying both
// fire together.
// Inspired by Rocks.Analysis.IntegrationTests.AbstractClassMethodReturnWithEventsTests
//
// Target class:
//   public abstract class AbstractClassMethodReturnWithEvents
//   {
//       public abstract int NoParameters();
//       public abstract event EventHandler? MyEvent;
//   }
//
// Key difference from ClassMethodReturnWithEventsTests: members are ABSTRACT,
// not virtual. Abstract members have no base implementation, so unconfigured calls
// return default(int) = 0 directly.
//
// Applicable patterns (class only):
// - Pattern 3 (Standalone Class): [KnockOffBase<AbstractClassMethodReturnWithEvents>]
// - Pattern 6 (Inline Class): [KnockOff<AbstractClassMethodReturnWithEvents>]
// ============================================================================

using KnockOff;

// ============================================================================
// TYPE DEFINITIONS
// ============================================================================

namespace KnockOff.Analysis.Tests.AbstractClassMethodReturnWithEventsTestTypes
{
	public abstract class AbstractClassMethodReturnWithEvents
	{
		public abstract int NoParameters();
		public abstract event EventHandler? MyEvent;
	}

	// Pattern 3: Standalone class stub
	[KnockOffBase<AbstractClassMethodReturnWithEvents>]
	public partial class AbstractClassMethodReturnWithEventsStandaloneKnockOff
	{
	}
}

// ============================================================================
// INLINE PATTERN DECLARATIONS + TESTS
// ============================================================================

namespace KnockOff.Analysis.Tests
{
	using KnockOff.Analysis.Tests.AbstractClassMethodReturnWithEventsTestTypes;

	// Pattern 6: Inline class stub
	[KnockOff<AbstractClassMethodReturnWithEvents>]
	public partial class AbstractClassMethodReturnWithEventsInlineTests
	{
	}

	public class AbstractClassMethodReturnWithEventsTests
	{
		// ====================================================================
		// Scenario 1: Callback raises event and returns value
		// ====================================================================

		#region Standalone Class: Callback raises event and returns value

		[Fact]
		public void CallbackRaisesEventAndReturns_StandaloneClass_BothFire()
		{
			var stub = new AbstractClassMethodReturnWithEventsStandaloneKnockOff();
			AbstractClassMethodReturnWithEvents obj = stub.Object;

			stub.NoParameters.Call(() =>
			{
				stub.MyEvent.Raise(obj, EventArgs.Empty);
				return 42;
			});

			var wasEventRaised = false;
			obj.MyEvent += (s, e) => wasEventRaised = true;

			int value = obj.NoParameters();

			Assert.Equal(42, value);
			Assert.True(wasEventRaised);
		}

		#endregion

		#region Inline Class: Callback raises event and returns value

		[Fact]
		public void CallbackRaisesEventAndReturns_InlineClass_BothFire()
		{
			var stub = new AbstractClassMethodReturnWithEventsInlineTests.Stubs.AbstractClassMethodReturnWithEvents();
			AbstractClassMethodReturnWithEvents obj = stub.Object;

			stub.NoParameters.Call(() =>
			{
				stub.MyEvent.Raise(obj, EventArgs.Empty);
				return 42;
			});

			var wasEventRaised = false;
			obj.MyEvent += (s, e) => wasEventRaised = true;

			int value = obj.NoParameters();

			Assert.Equal(42, value);
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
			var stub = new AbstractClassMethodReturnWithEventsStandaloneKnockOff();
			AbstractClassMethodReturnWithEvents obj = stub.Object;

			stub.NoParameters.Call(() =>
			{
				stub.MyEvent.Raise(obj, EventArgs.Empty);
				return 3;
			});

			var eventRaisedCount = 0;
			obj.MyEvent += (s, e) => eventRaisedCount++;

			int v1 = obj.NoParameters();
			int v2 = obj.NoParameters();

			Assert.Equal(3, v1);
			Assert.Equal(3, v2);
			Assert.Equal(2, eventRaisedCount);
		}

		#endregion

		#region Inline Class: Multiple calls track event count

		[Fact]
		public void MultipleCalls_InlineClass_EventCountMatchesCallCount()
		{
			var stub = new AbstractClassMethodReturnWithEventsInlineTests.Stubs.AbstractClassMethodReturnWithEvents();
			AbstractClassMethodReturnWithEvents obj = stub.Object;

			stub.NoParameters.Call(() =>
			{
				stub.MyEvent.Raise(obj, EventArgs.Empty);
				return 3;
			});

			var eventRaisedCount = 0;
			obj.MyEvent += (s, e) => eventRaisedCount++;

			int v1 = obj.NoParameters();
			int v2 = obj.NoParameters();

			Assert.Equal(3, v1);
			Assert.Equal(3, v2);
			Assert.Equal(2, eventRaisedCount);
		}

		#endregion

		// ====================================================================
		// Scenario 3: Callback + event + return value all verified
		// ====================================================================

		#region Standalone Class: Callback + event + return value verified

		[Fact]
		public void CallbackAndEvent_StandaloneClass_AllThreeVerified()
		{
			var stub = new AbstractClassMethodReturnWithEventsStandaloneKnockOff();
			AbstractClassMethodReturnWithEvents obj = stub.Object;

			var wasCallbackInvoked = false;
			stub.NoParameters.Call(() =>
			{
				wasCallbackInvoked = true;
				stub.MyEvent.Raise(obj, EventArgs.Empty);
				return 99;
			});

			var wasEventRaised = false;
			obj.MyEvent += (s, e) => wasEventRaised = true;

			int value = obj.NoParameters();

			Assert.Equal(99, value);
			Assert.True(wasCallbackInvoked);
			Assert.True(wasEventRaised);
		}

		#endregion

		#region Inline Class: Callback + event + return value verified

		[Fact]
		public void CallbackAndEvent_InlineClass_AllThreeVerified()
		{
			var stub = new AbstractClassMethodReturnWithEventsInlineTests.Stubs.AbstractClassMethodReturnWithEvents();
			AbstractClassMethodReturnWithEvents obj = stub.Object;

			var wasCallbackInvoked = false;
			stub.NoParameters.Call(() =>
			{
				wasCallbackInvoked = true;
				stub.MyEvent.Raise(obj, EventArgs.Empty);
				return 99;
			});

			var wasEventRaised = false;
			obj.MyEvent += (s, e) => wasEventRaised = true;

			int value = obj.NoParameters();

			Assert.Equal(99, value);
			Assert.True(wasCallbackInvoked);
			Assert.True(wasEventRaised);
		}

		#endregion

		// ====================================================================
		// Scenario 4: Event add tracked alongside method call
		// ====================================================================

		#region Standalone Class: Event add tracked alongside method call

		[Fact]
		public void EventAddTracked_StandaloneClass_VerifyBothMethodAndEvent()
		{
			var stub = new AbstractClassMethodReturnWithEventsStandaloneKnockOff();
			AbstractClassMethodReturnWithEvents obj = stub.Object;

			stub.NoParameters.Call(() =>
			{
				stub.MyEvent.Raise(obj, EventArgs.Empty);
				return 7;
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
			var stub = new AbstractClassMethodReturnWithEventsInlineTests.Stubs.AbstractClassMethodReturnWithEvents();
			AbstractClassMethodReturnWithEvents obj = stub.Object;

			stub.NoParameters.Call(() =>
			{
				stub.MyEvent.Raise(obj, EventArgs.Empty);
				return 7;
			});

			obj.MyEvent += (s, e) => { };

			obj.NoParameters();

			stub.NoParameters.Verify(Called.Once);
			stub.MyEvent.VerifyAdd(Called.Once);
		}

		#endregion

		// ====================================================================
		// Scenario 5: Unconfigured returns default, no event raised
		// ====================================================================

		#region Standalone Class: Unconfigured returns default, no event

		[Fact]
		public void Unconfigured_StandaloneClass_ReturnsDefaultNoEvent()
		{
			var stub = new AbstractClassMethodReturnWithEventsStandaloneKnockOff();
			AbstractClassMethodReturnWithEvents obj = stub.Object;

			var wasEventRaised = false;
			obj.MyEvent += (s, e) => wasEventRaised = true;

			int value = obj.NoParameters();

			Assert.Equal(0, value);
			Assert.False(wasEventRaised);
		}

		#endregion

		#region Inline Class: Unconfigured returns default, no event

		[Fact]
		public void Unconfigured_InlineClass_ReturnsDefaultNoEvent()
		{
			var stub = new AbstractClassMethodReturnWithEventsInlineTests.Stubs.AbstractClassMethodReturnWithEvents();
			AbstractClassMethodReturnWithEvents obj = stub.Object;

			var wasEventRaised = false;
			obj.MyEvent += (s, e) => wasEventRaised = true;

			int value = obj.NoParameters();

			Assert.Equal(0, value);
			Assert.False(wasEventRaised);
		}

		#endregion
	}
}
