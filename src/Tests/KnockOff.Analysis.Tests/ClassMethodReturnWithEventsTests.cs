// ============================================================================
// ClassMethodReturnWithEventsTests: Edge case tests for a class with a virtual
// method returning int AND a virtual event, verifying both fire together.
// Inspired by Rocks.Analysis.IntegrationTests.ClassMethodReturnWithEventsTests
//
// Target class:
//   public class ClassMethodReturnWithEvents
//   {
//       public virtual int NoParameters() => default;
//       public virtual event EventHandler? MyEvent;
//   }
//
// Tests exercise the CLASS pipeline for:
// 1. Callback raises event AND returns value — assert both
// 2. Multiple calls — assert event count matches call count
// 3. Return value without callback — event still fires from separate callback
// 4. Combined callback + event verification
//
// Applicable patterns (class only):
// - Pattern 3 (Standalone Class): [KnockOffBase<ClassMethodReturnWithEvents>]
// - Pattern 6 (Inline Class): [KnockOff<ClassMethodReturnWithEvents>]
// ============================================================================

using KnockOff;

// ============================================================================
// TYPE DEFINITIONS
// ============================================================================

namespace KnockOff.Analysis.Tests.ClassMethodReturnWithEventsTestTypes
{
	public class ClassMethodReturnWithEvents
	{
		public virtual int NoParameters() => default;

#pragma warning disable CA1070 // Do not declare event fields as virtual
#pragma warning disable CS0067
		public virtual event EventHandler? MyEvent;
#pragma warning restore CS0067
#pragma warning restore CA1070 // Do not declare event fields as virtual
	}

	// Pattern 3: Standalone class stub
	[KnockOffBase<ClassMethodReturnWithEvents>]
	public partial class ClassMethodReturnWithEventsStandaloneKnockOff
	{
	}
}

// ============================================================================
// INLINE PATTERN DECLARATIONS + TESTS
// ============================================================================

namespace KnockOff.Analysis.Tests
{
	using KnockOff.Analysis.Tests.ClassMethodReturnWithEventsTestTypes;

	// Pattern 6: Inline class stub
	[KnockOff<ClassMethodReturnWithEvents>]
	public partial class ClassMethodReturnWithEventsInlineTests
	{
	}

	public class ClassMethodReturnWithEventsTests
	{
		// ====================================================================
		// Scenario 1: Callback raises event and returns value
		// Configure a callback that raises the event and returns a value.
		// Assert both the return value and that the event was fired.
		// ====================================================================

		#region Standalone Class: Callback raises event and returns value

		[Fact]
		public void CallbackRaisesEventAndReturns_StandaloneClass_BothFire()
		{
			var stub = new ClassMethodReturnWithEventsStandaloneKnockOff();
			ClassMethodReturnWithEvents obj = stub.Object;

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
			var stub = new ClassMethodReturnWithEventsInlineTests.Stubs.ClassMethodReturnWithEvents();
			ClassMethodReturnWithEvents obj = stub.Object;

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
		// Call the method multiple times, verify event fires each time.
		// ====================================================================

		#region Standalone Class: Multiple calls track event count

		[Fact]
		public void MultipleCalls_StandaloneClass_EventCountMatchesCallCount()
		{
			var stub = new ClassMethodReturnWithEventsStandaloneKnockOff();
			ClassMethodReturnWithEvents obj = stub.Object;

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
			var stub = new ClassMethodReturnWithEventsInlineTests.Stubs.ClassMethodReturnWithEvents();
			ClassMethodReturnWithEvents obj = stub.Object;

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
		// Scenario 3: Callback invoked flag + event raised flag + return value
		// Full combination: callback is invoked, event fires, value returned.
		// ====================================================================

		#region Standalone Class: Callback + event + return value verified

		[Fact]
		public void CallbackAndEvent_StandaloneClass_AllThreeVerified()
		{
			var stub = new ClassMethodReturnWithEventsStandaloneKnockOff();
			ClassMethodReturnWithEvents obj = stub.Object;

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
			var stub = new ClassMethodReturnWithEventsInlineTests.Stubs.ClassMethodReturnWithEvents();
			ClassMethodReturnWithEvents obj = stub.Object;

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
		// Scenario 4: Event add verification alongside method call
		// Verify that subscribing to the event is tracked alongside method
		// invocation.
		// ====================================================================

		#region Standalone Class: Event add tracked alongside method call

		[Fact]
		public void EventAddTracked_StandaloneClass_VerifyBothMethodAndEvent()
		{
			var stub = new ClassMethodReturnWithEventsStandaloneKnockOff();
			ClassMethodReturnWithEvents obj = stub.Object;

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
			var stub = new ClassMethodReturnWithEventsInlineTests.Stubs.ClassMethodReturnWithEvents();
			ClassMethodReturnWithEvents obj = stub.Object;

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
		// Scenario 5: Unconfigured method falls to base, event not raised
		// When method is not configured, it falls through to base and returns
		// default. No event is raised because no callback exists.
		// ====================================================================

		#region Standalone Class: Unconfigured fallback, no event

		[Fact]
		public void Unconfigured_StandaloneClass_FallsToBaseNoEvent()
		{
			var stub = new ClassMethodReturnWithEventsStandaloneKnockOff();
			ClassMethodReturnWithEvents obj = stub.Object;

			var wasEventRaised = false;
			obj.MyEvent += (s, e) => wasEventRaised = true;

			int value = obj.NoParameters();

			Assert.Equal(0, value);
			Assert.False(wasEventRaised);
		}

		#endregion

		#region Inline Class: Unconfigured fallback, no event

		[Fact]
		public void Unconfigured_InlineClass_FallsToBaseNoEvent()
		{
			var stub = new ClassMethodReturnWithEventsInlineTests.Stubs.ClassMethodReturnWithEvents();
			ClassMethodReturnWithEvents obj = stub.Object;

			var wasEventRaised = false;
			obj.MyEvent += (s, e) => wasEventRaised = true;

			int value = obj.NoParameters();

			Assert.Equal(0, value);
			Assert.False(wasEventRaised);
		}

		#endregion
	}
}
