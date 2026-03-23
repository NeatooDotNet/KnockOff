// ============================================================================
// ClassMethodVoidWithEventsTests: Edge case tests for a class with a virtual
// void method AND a virtual event, verifying both fire together.
// Inspired by Rocks.Analysis.IntegrationTests.ClassMethodVoidWithEventsTests
//
// Target class:
//   public class ClassMethodVoidWithEvents
//   {
//       public virtual void NoParameters() { }
//       public virtual event EventHandler? MyEvent;
//   }
//
// Tests exercise the CLASS pipeline for:
// 1. Callback raises event — assert event fired
// 2. Multiple calls — assert event count matches call count
// 3. Combined callback invoked flag + event raised flag
// 4. Event add verification alongside method call
//
// Applicable patterns (class only):
// - Pattern 3 (Standalone Class): [KnockOffBase<ClassMethodVoidWithEvents>]
// - Pattern 6 (Inline Class): [KnockOff<ClassMethodVoidWithEvents>]
// ============================================================================

using KnockOff;

// ============================================================================
// TYPE DEFINITIONS
// ============================================================================

namespace KnockOff.Analysis.Tests.ClassMethodVoidWithEventsTestTypes
{
	public class ClassMethodVoidWithEvents
	{
		public virtual void NoParameters() { }

#pragma warning disable CA1070 // Do not declare event fields as virtual
#pragma warning disable CS0067
		public virtual event EventHandler? MyEvent;
#pragma warning restore CS0067
#pragma warning restore CA1070 // Do not declare event fields as virtual
	}

	// Pattern 3: Standalone class stub
	[KnockOffBase<ClassMethodVoidWithEvents>]
	public partial class ClassMethodVoidWithEventsStandaloneKnockOff
	{
	}
}

// ============================================================================
// INLINE PATTERN DECLARATIONS + TESTS
// ============================================================================

namespace KnockOff.Analysis.Tests
{
	using KnockOff.Analysis.Tests.ClassMethodVoidWithEventsTestTypes;

	// Pattern 6: Inline class stub
	[KnockOff<ClassMethodVoidWithEvents>]
	public partial class ClassMethodVoidWithEventsInlineTests
	{
	}

	public class ClassMethodVoidWithEventsTests
	{
		// ====================================================================
		// Scenario 1: Callback raises event — assert event fired
		// Configure a void callback that raises the event. Verify the event
		// handler was invoked.
		// ====================================================================

		#region Standalone Class: Callback raises event

		[Fact]
		public void CallbackRaisesEvent_StandaloneClass_EventFired()
		{
			var stub = new ClassMethodVoidWithEventsStandaloneKnockOff();
			ClassMethodVoidWithEvents obj = stub.Object;

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
			var stub = new ClassMethodVoidWithEventsInlineTests.Stubs.ClassMethodVoidWithEvents();
			ClassMethodVoidWithEvents obj = stub.Object;

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
		// Call the void method multiple times, verify event fires each time.
		// ====================================================================

		#region Standalone Class: Multiple calls track event count

		[Fact]
		public void MultipleCalls_StandaloneClass_EventCountMatchesCallCount()
		{
			var stub = new ClassMethodVoidWithEventsStandaloneKnockOff();
			ClassMethodVoidWithEvents obj = stub.Object;

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
			var stub = new ClassMethodVoidWithEventsInlineTests.Stubs.ClassMethodVoidWithEvents();
			ClassMethodVoidWithEvents obj = stub.Object;

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
		// Verify both the callback and the event independently.
		// ====================================================================

		#region Standalone Class: Callback + event both verified

		[Fact]
		public void CallbackAndEvent_StandaloneClass_BothVerified()
		{
			var stub = new ClassMethodVoidWithEventsStandaloneKnockOff();
			ClassMethodVoidWithEvents obj = stub.Object;

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
			var stub = new ClassMethodVoidWithEventsInlineTests.Stubs.ClassMethodVoidWithEvents();
			ClassMethodVoidWithEvents obj = stub.Object;

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
		// Verify that subscribing to the event is tracked alongside method
		// invocation tracking.
		// ====================================================================

		#region Standalone Class: Event add tracked alongside method call

		[Fact]
		public void EventAddTracked_StandaloneClass_VerifyBothMethodAndEvent()
		{
			var stub = new ClassMethodVoidWithEventsStandaloneKnockOff();
			ClassMethodVoidWithEvents obj = stub.Object;

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
			var stub = new ClassMethodVoidWithEventsInlineTests.Stubs.ClassMethodVoidWithEvents();
			ClassMethodVoidWithEvents obj = stub.Object;

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
		// Scenario 5: Unconfigured method falls to base, event not raised
		// When method is not configured, it falls through to base.
		// No event is raised because no callback exists.
		// ====================================================================

		#region Standalone Class: Unconfigured fallback, no event

		[Fact]
		public void Unconfigured_StandaloneClass_FallsToBaseNoEvent()
		{
			var stub = new ClassMethodVoidWithEventsStandaloneKnockOff();
			ClassMethodVoidWithEvents obj = stub.Object;

			var wasEventRaised = false;
			obj.MyEvent += (s, e) => wasEventRaised = true;

			obj.NoParameters();

			Assert.False(wasEventRaised);
		}

		#endregion

		#region Inline Class: Unconfigured fallback, no event

		[Fact]
		public void Unconfigured_InlineClass_FallsToBaseNoEvent()
		{
			var stub = new ClassMethodVoidWithEventsInlineTests.Stubs.ClassMethodVoidWithEvents();
			ClassMethodVoidWithEvents obj = stub.Object;

			var wasEventRaised = false;
			obj.MyEvent += (s, e) => wasEventRaised = true;

			obj.NoParameters();

			Assert.False(wasEventRaised);
		}

		#endregion
	}
}
