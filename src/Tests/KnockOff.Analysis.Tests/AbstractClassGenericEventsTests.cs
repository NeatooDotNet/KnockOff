// ============================================================================
// AbstractClassGenericEventsTests: Edge case tests for a generic abstract class
// with an abstract event using a generic EventArgs type parameter.
// Inspired by Rocks.Analysis.IntegrationTests.AbstractClassGenericEventsTests
//
// Target class:
//   public sealed class AbstractClassEventArgs : EventArgs { }
//   public abstract class AbstractClassGenericEvents<T> where T : EventArgs
//   {
//       public abstract void Foo();
//       public abstract event EventHandler<T>? MyEvent;
//   }
//
// Key difference from ClassGenericEventsTests: members are ABSTRACT, not virtual.
// Abstract members have no base implementation — events must be fully implemented
// by the stub.
//
// Applicable patterns (generic class):
// - Pattern 3 (Standalone Class): [KnockOffBase<AbstractClassGenericEvents<AbstractClassEventArgs>>]
// - Pattern 4 (Generic Standalone Class): [KnockOffBase(typeof(AbstractClassGenericEvents<>))]
// - Pattern 6 (Inline Class): [KnockOff<AbstractClassGenericEvents<AbstractClassEventArgs>>]
// - Pattern 9 (Open Generic Class): [KnockOff(typeof(AbstractClassGenericEvents<>))]
// ============================================================================

using KnockOff;

// ============================================================================
// TYPE DEFINITIONS
// ============================================================================

namespace KnockOff.Analysis.Tests.AbstractClassGenericEventsTestTypes
{
	public sealed class AbstractClassEventArgs : EventArgs { }

	public abstract class AbstractClassGenericEvents<T> where T : EventArgs
	{
		public abstract void Foo();

		public abstract event EventHandler<T>? MyEvent;
	}

	// Pattern 3: Standalone class stub (closed generic)
	[KnockOffBase<AbstractClassGenericEvents<AbstractClassEventArgs>>]
	public partial class AbstractClassGenericEventsStandaloneKnockOff
	{
	}

	// Pattern 4: Generic standalone class stub (open generic)
	[KnockOffBase(typeof(AbstractClassGenericEvents<>))]
	public partial class AbstractClassGenericEventsGenericStandaloneKnockOff<T> where T : EventArgs
	{
	}
}

// ============================================================================
// INLINE PATTERN DECLARATIONS + TESTS
// ============================================================================

namespace KnockOff.Analysis.Tests
{
	using KnockOff.Analysis.Tests.AbstractClassGenericEventsTestTypes;

	// Pattern 6: Inline class stub (closed generic)
	[KnockOff<AbstractClassGenericEvents<AbstractClassEventArgs>>]
	public partial class AbstractClassGenericEventsInlineTests
	{
	}

	// Pattern 9: Open generic class stub
	[KnockOff(typeof(AbstractClassGenericEvents<>))]
	public partial class AbstractClassGenericEventsOpenGenericTests
	{
	}

	public class AbstractClassGenericEventsTests
	{
		// ====================================================================
		// Scenario 1: Call Foo, raise generic MyEvent from callback
		// ====================================================================

		#region Pattern 3 (Standalone Class): Raise generic event from callback

		[Fact]
		public void RaiseGenericEvent_StandaloneClass_HandlerInvoked()
		{
			var stub = new AbstractClassGenericEventsStandaloneKnockOff();
			AbstractClassGenericEvents<AbstractClassEventArgs> obj = stub.Object;

			var receivedArgs = default(AbstractClassEventArgs);
			var eventArgs = new AbstractClassEventArgs();

			stub.Foo.Call(() =>
			{
				stub.MyEvent.Raise(obj, eventArgs);
			});

			obj.MyEvent += (s, e) => receivedArgs = e;
			obj.Foo();

			Assert.Same(eventArgs, receivedArgs);
		}

		[Fact]
		public void RaiseGenericEvent_StandaloneClass_HandlerReceivesCorrectSender()
		{
			var stub = new AbstractClassGenericEventsStandaloneKnockOff();
			AbstractClassGenericEvents<AbstractClassEventArgs> obj = stub.Object;

			object? receivedSender = null;

			stub.Foo.Call(() =>
			{
				stub.MyEvent.Raise(obj, new AbstractClassEventArgs());
			});

			obj.MyEvent += (s, e) => receivedSender = s;
			obj.Foo();

			Assert.Same(obj, receivedSender);
		}

		#endregion

		#region Pattern 4 (Generic Standalone Class): Raise generic event from callback

		[Fact]
		public void RaiseGenericEvent_GenericStandaloneClass_HandlerInvoked()
		{
			var stub = new AbstractClassGenericEventsGenericStandaloneKnockOff<AbstractClassEventArgs>();
			AbstractClassGenericEvents<AbstractClassEventArgs> obj = stub.Object;

			var receivedArgs = default(AbstractClassEventArgs);
			var eventArgs = new AbstractClassEventArgs();

			stub.Foo.Call(() =>
			{
				stub.MyEvent.Raise(obj, eventArgs);
			});

			obj.MyEvent += (s, e) => receivedArgs = e;
			obj.Foo();

			Assert.Same(eventArgs, receivedArgs);
		}

		#endregion

		#region Pattern 6 (Inline Class): Raise generic event from callback

		[Fact]
		public void RaiseGenericEvent_InlineClass_HandlerInvoked()
		{
			var stub = new AbstractClassGenericEventsInlineTests.Stubs.AbstractClassGenericEvents();
			AbstractClassGenericEvents<AbstractClassEventArgs> obj = stub.Object;

			var receivedArgs = default(AbstractClassEventArgs);
			var eventArgs = new AbstractClassEventArgs();

			stub.Foo.Call(() =>
			{
				stub.MyEvent.Raise(obj, eventArgs);
			});

			obj.MyEvent += (s, e) => receivedArgs = e;
			obj.Foo();

			Assert.Same(eventArgs, receivedArgs);
		}

		[Fact]
		public void RaiseGenericEvent_InlineClass_HandlerReceivesCorrectSender()
		{
			var stub = new AbstractClassGenericEventsInlineTests.Stubs.AbstractClassGenericEvents();
			AbstractClassGenericEvents<AbstractClassEventArgs> obj = stub.Object;

			object? receivedSender = null;

			stub.Foo.Call(() =>
			{
				stub.MyEvent.Raise(obj, new AbstractClassEventArgs());
			});

			obj.MyEvent += (s, e) => receivedSender = s;
			obj.Foo();

			Assert.Same(obj, receivedSender);
		}

		#endregion

		#region Pattern 9 (Open Generic Class): Raise generic event from callback

		[Fact]
		public void RaiseGenericEvent_OpenGenericClass_HandlerInvoked()
		{
			var stub = new AbstractClassGenericEventsOpenGenericTests.Stubs.AbstractClassGenericEvents<AbstractClassEventArgs>();
			AbstractClassGenericEvents<AbstractClassEventArgs> obj = stub.Object;

			var receivedArgs = default(AbstractClassEventArgs);
			var eventArgs = new AbstractClassEventArgs();

			stub.Foo.Call(() =>
			{
				stub.MyEvent.Raise(obj, eventArgs);
			});

			obj.MyEvent += (s, e) => receivedArgs = e;
			obj.Foo();

			Assert.Same(eventArgs, receivedArgs);
		}

		#endregion

		// ====================================================================
		// Scenario 2: Event add/remove tracking on generic events
		// ====================================================================

		#region Pattern 3 (Standalone Class): Event add/remove tracking

		[Fact]
		public void VerifyAdd_StandaloneClass_TracksSubscription()
		{
			var stub = new AbstractClassGenericEventsStandaloneKnockOff();
			AbstractClassGenericEvents<AbstractClassEventArgs> obj = stub.Object;

			void Handler(object? s, AbstractClassEventArgs e) { }

			obj.MyEvent += Handler;

			stub.MyEvent.VerifyAdd(Called.Once);
		}

		[Fact]
		public void VerifyAddRemove_StandaloneClass_TracksUnsubscription()
		{
			var stub = new AbstractClassGenericEventsStandaloneKnockOff();
			AbstractClassGenericEvents<AbstractClassEventArgs> obj = stub.Object;

			void Handler(object? s, AbstractClassEventArgs e) { }

			obj.MyEvent += Handler;
			obj.MyEvent -= Handler;

			stub.MyEvent.VerifyAdd(Called.Once);
			stub.MyEvent.VerifyRemove(Called.Once);
		}

		#endregion

		#region Pattern 4 (Generic Standalone Class): Event add/remove tracking

		[Fact]
		public void VerifyAdd_GenericStandaloneClass_TracksSubscription()
		{
			var stub = new AbstractClassGenericEventsGenericStandaloneKnockOff<AbstractClassEventArgs>();
			AbstractClassGenericEvents<AbstractClassEventArgs> obj = stub.Object;

			void Handler(object? s, AbstractClassEventArgs e) { }

			obj.MyEvent += Handler;

			stub.MyEvent.VerifyAdd(Called.Once);
		}

		#endregion

		#region Pattern 6 (Inline Class): Event add/remove tracking

		[Fact]
		public void VerifyAdd_InlineClass_TracksSubscription()
		{
			var stub = new AbstractClassGenericEventsInlineTests.Stubs.AbstractClassGenericEvents();
			AbstractClassGenericEvents<AbstractClassEventArgs> obj = stub.Object;

			void Handler(object? s, AbstractClassEventArgs e) { }

			obj.MyEvent += Handler;

			stub.MyEvent.VerifyAdd(Called.Once);
		}

		[Fact]
		public void VerifyAddRemove_InlineClass_TracksUnsubscription()
		{
			var stub = new AbstractClassGenericEventsInlineTests.Stubs.AbstractClassGenericEvents();
			AbstractClassGenericEvents<AbstractClassEventArgs> obj = stub.Object;

			void Handler(object? s, AbstractClassEventArgs e) { }

			obj.MyEvent += Handler;
			obj.MyEvent -= Handler;

			stub.MyEvent.VerifyAdd(Called.Once);
			stub.MyEvent.VerifyRemove(Called.Once);
		}

		#endregion

		#region Pattern 9 (Open Generic Class): Event add/remove tracking

		[Fact]
		public void VerifyAdd_OpenGenericClass_TracksSubscription()
		{
			var stub = new AbstractClassGenericEventsOpenGenericTests.Stubs.AbstractClassGenericEvents<AbstractClassEventArgs>();
			AbstractClassGenericEvents<AbstractClassEventArgs> obj = stub.Object;

			void Handler(object? s, AbstractClassEventArgs e) { }

			obj.MyEvent += Handler;

			stub.MyEvent.VerifyAdd(Called.Once);
		}

		#endregion

		// ====================================================================
		// Scenario 3: HasSubscribers on generic events
		// ====================================================================

		#region Pattern 3 (Standalone Class): HasSubscribers

		[Fact]
		public void HasSubscribers_StandaloneClass_ReflectsSubscriptionState()
		{
			var stub = new AbstractClassGenericEventsStandaloneKnockOff();
			AbstractClassGenericEvents<AbstractClassEventArgs> obj = stub.Object;

			Assert.False(stub.MyEvent.HasSubscribers);

			void Handler(object? s, AbstractClassEventArgs e) { }
			obj.MyEvent += Handler;

			Assert.True(stub.MyEvent.HasSubscribers);

			obj.MyEvent -= Handler;

			Assert.False(stub.MyEvent.HasSubscribers);
		}

		#endregion

		#region Pattern 6 (Inline Class): HasSubscribers

		[Fact]
		public void HasSubscribers_InlineClass_ReflectsSubscriptionState()
		{
			var stub = new AbstractClassGenericEventsInlineTests.Stubs.AbstractClassGenericEvents();
			AbstractClassGenericEvents<AbstractClassEventArgs> obj = stub.Object;

			Assert.False(stub.MyEvent.HasSubscribers);

			void Handler(object? s, AbstractClassEventArgs e) { }
			obj.MyEvent += Handler;

			Assert.True(stub.MyEvent.HasSubscribers);

			obj.MyEvent -= Handler;

			Assert.False(stub.MyEvent.HasSubscribers);
		}

		#endregion
	}
}
