// ============================================================================
// InterfaceGenericEventsTests: Edge case tests for a generic interface with a
// generic event using EventHandler<T> where T : EventArgs.
// Inspired by Rocks.Analysis.IntegrationTests.InterfaceGenericEventsTests
//
// Target interface:
//   public sealed class InterfaceEventArgs : EventArgs { }
//   public interface IInterfaceGenericEvents<T> where T : EventArgs
//   {
//       void Foo();
//       event EventHandler<T> MyEvent;
//   }
//
// Tests exercise the INTERFACE pipeline for:
// 1. Call Foo, raise generic MyEvent from callback, verify handler invoked
// 2. Verify event args type is correct (InterfaceEventArgs)
// 3. Verify event add/remove tracking
//
// Applicable patterns (generic interface):
// - Pattern 1 (Standalone): [KnockOff] partial class Stub : IInterfaceGenericEvents<InterfaceEventArgs> {}
// - Pattern 2 (Generic Standalone): [KnockOff] partial class Stub<T> : IInterfaceGenericEvents<T> where T : EventArgs {}
// - Pattern 5 (Inline Interface): [KnockOff<IInterfaceGenericEvents<InterfaceEventArgs>>]
// - Pattern 8 (Open Generic Interface): [KnockOff(typeof(IInterfaceGenericEvents<>))]
// ============================================================================

using KnockOff;

// ============================================================================
// TYPE DEFINITIONS
// ============================================================================

namespace KnockOff.Analysis.Tests.InterfaceGenericEventsTestTypes
{
	public sealed class InterfaceEventArgs : EventArgs { }

	public interface IInterfaceGenericEvents<T> where T : EventArgs
	{
		void Foo();
		event EventHandler<T> MyEvent;
	}

	// Pattern 1: Standalone interface stub (closed generic)
	[KnockOff]
	public partial class InterfaceGenericEventsStandaloneKnockOff : IInterfaceGenericEvents<InterfaceEventArgs>
	{
	}

	// Pattern 2: Generic standalone interface stub (open generic)
	[KnockOff]
	public partial class InterfaceGenericEventsGenericStandaloneKnockOff<T> : IInterfaceGenericEvents<T> where T : EventArgs
	{
	}
}

// ============================================================================
// INLINE PATTERN DECLARATIONS + TESTS
// ============================================================================

namespace KnockOff.Analysis.Tests
{
	using KnockOff.Analysis.Tests.InterfaceGenericEventsTestTypes;

	// Pattern 5: Inline interface stub (closed generic)
	[KnockOff<IInterfaceGenericEvents<InterfaceEventArgs>>]
	public partial class InterfaceGenericEventsInlineTests
	{
	}

	// Pattern 8: Open generic interface stub
	[KnockOff(typeof(IInterfaceGenericEvents<>))]
	public partial class InterfaceGenericEventsOpenGenericTests
	{
	}

	public class InterfaceGenericEventsTests
	{
		// ====================================================================
		// Scenario 1: Call Foo, raise generic MyEvent from callback, verify
		// handler invoked with correct EventArgs type
		// ====================================================================

		#region Pattern 1 (Standalone): Raise generic event from callback

		[Fact]
		public void RaiseGenericEvent_Standalone_HandlerInvoked()
		{
			var stub = new InterfaceGenericEventsStandaloneKnockOff();
			IInterfaceGenericEvents<InterfaceEventArgs> service = stub;

			var receivedArgs = default(InterfaceEventArgs);
			var eventArgs = new InterfaceEventArgs();

			stub.Foo.Call(() =>
			{
				stub.MyEvent.Raise(stub, eventArgs);
			});

			service.MyEvent += (s, e) => receivedArgs = e;
			service.Foo();

			Assert.Same(eventArgs, receivedArgs);
		}

		[Fact]
		public void RaiseGenericEvent_Standalone_HandlerReceivesCorrectSender()
		{
			var stub = new InterfaceGenericEventsStandaloneKnockOff();
			IInterfaceGenericEvents<InterfaceEventArgs> service = stub;

			object? receivedSender = null;

			stub.Foo.Call(() =>
			{
				stub.MyEvent.Raise(stub, new InterfaceEventArgs());
			});

			service.MyEvent += (s, e) => receivedSender = s;
			service.Foo();

			Assert.Same(stub, receivedSender);
		}

		#endregion

		#region Pattern 2 (Generic Standalone): Raise generic event from callback

		[Fact]
		public void RaiseGenericEvent_GenericStandalone_HandlerInvoked()
		{
			var stub = new InterfaceGenericEventsGenericStandaloneKnockOff<InterfaceEventArgs>();
			IInterfaceGenericEvents<InterfaceEventArgs> service = stub;

			var receivedArgs = default(InterfaceEventArgs);
			var eventArgs = new InterfaceEventArgs();

			stub.Foo.Call(() =>
			{
				stub.MyEvent.Raise(stub, eventArgs);
			});

			service.MyEvent += (s, e) => receivedArgs = e;
			service.Foo();

			Assert.Same(eventArgs, receivedArgs);
		}

		#endregion

		#region Pattern 5 (Inline Interface): Raise generic event from callback

		[Fact]
		public void RaiseGenericEvent_Inline_HandlerInvoked()
		{
			var stub = new InterfaceGenericEventsInlineTests.Stubs.IInterfaceGenericEvents();
			IInterfaceGenericEvents<InterfaceEventArgs> service = stub;

			var receivedArgs = default(InterfaceEventArgs);
			var eventArgs = new InterfaceEventArgs();

			stub.Foo.Call(() =>
			{
				stub.MyEvent.Raise(stub, eventArgs);
			});

			service.MyEvent += (s, e) => receivedArgs = e;
			service.Foo();

			Assert.Same(eventArgs, receivedArgs);
		}

		[Fact]
		public void RaiseGenericEvent_Inline_HandlerReceivesCorrectSender()
		{
			var stub = new InterfaceGenericEventsInlineTests.Stubs.IInterfaceGenericEvents();
			IInterfaceGenericEvents<InterfaceEventArgs> service = stub;

			object? receivedSender = null;

			stub.Foo.Call(() =>
			{
				stub.MyEvent.Raise(stub, new InterfaceEventArgs());
			});

			service.MyEvent += (s, e) => receivedSender = s;
			service.Foo();

			Assert.Same(stub, receivedSender);
		}

		#endregion

		#region Pattern 8 (Open Generic Interface): Raise generic event from callback

		[Fact]
		public void RaiseGenericEvent_OpenGeneric_HandlerInvoked()
		{
			var stub = new InterfaceGenericEventsOpenGenericTests.Stubs.IInterfaceGenericEvents<InterfaceEventArgs>();
			IInterfaceGenericEvents<InterfaceEventArgs> service = stub;

			var receivedArgs = default(InterfaceEventArgs);
			var eventArgs = new InterfaceEventArgs();

			stub.Foo.Call(() =>
			{
				stub.MyEvent.Raise(stub, eventArgs);
			});

			service.MyEvent += (s, e) => receivedArgs = e;
			service.Foo();

			Assert.Same(eventArgs, receivedArgs);
		}

		#endregion

		// ====================================================================
		// Scenario 2: Verify event add/remove tracking on generic events
		// ====================================================================

		#region Pattern 1 (Standalone): Event add/remove tracking

		[Fact]
		public void VerifyAdd_Standalone_TracksSubscription()
		{
			var stub = new InterfaceGenericEventsStandaloneKnockOff();
			IInterfaceGenericEvents<InterfaceEventArgs> service = stub;

			void Handler(object? s, InterfaceEventArgs e) { }

			service.MyEvent += Handler;

			stub.MyEvent.VerifyAdd(Called.Once);
		}

		[Fact]
		public void VerifyAddRemove_Standalone_TracksUnsubscription()
		{
			var stub = new InterfaceGenericEventsStandaloneKnockOff();
			IInterfaceGenericEvents<InterfaceEventArgs> service = stub;

			void Handler(object? s, InterfaceEventArgs e) { }

			service.MyEvent += Handler;
			service.MyEvent -= Handler;

			stub.MyEvent.VerifyAdd(Called.Once);
			stub.MyEvent.VerifyRemove(Called.Once);
		}

		#endregion

		#region Pattern 2 (Generic Standalone): Event add/remove tracking

		[Fact]
		public void VerifyAdd_GenericStandalone_TracksSubscription()
		{
			var stub = new InterfaceGenericEventsGenericStandaloneKnockOff<InterfaceEventArgs>();
			IInterfaceGenericEvents<InterfaceEventArgs> service = stub;

			void Handler(object? s, InterfaceEventArgs e) { }

			service.MyEvent += Handler;

			stub.MyEvent.VerifyAdd(Called.Once);
		}

		#endregion

		#region Pattern 5 (Inline Interface): Event add/remove tracking

		[Fact]
		public void VerifyAdd_Inline_TracksSubscription()
		{
			var stub = new InterfaceGenericEventsInlineTests.Stubs.IInterfaceGenericEvents();
			IInterfaceGenericEvents<InterfaceEventArgs> service = stub;

			void Handler(object? s, InterfaceEventArgs e) { }

			service.MyEvent += Handler;

			stub.MyEvent.VerifyAdd(Called.Once);
		}

		[Fact]
		public void VerifyAddRemove_Inline_TracksUnsubscription()
		{
			var stub = new InterfaceGenericEventsInlineTests.Stubs.IInterfaceGenericEvents();
			IInterfaceGenericEvents<InterfaceEventArgs> service = stub;

			void Handler(object? s, InterfaceEventArgs e) { }

			service.MyEvent += Handler;
			service.MyEvent -= Handler;

			stub.MyEvent.VerifyAdd(Called.Once);
			stub.MyEvent.VerifyRemove(Called.Once);
		}

		#endregion

		#region Pattern 8 (Open Generic Interface): Event add/remove tracking

		[Fact]
		public void VerifyAdd_OpenGeneric_TracksSubscription()
		{
			var stub = new InterfaceGenericEventsOpenGenericTests.Stubs.IInterfaceGenericEvents<InterfaceEventArgs>();
			IInterfaceGenericEvents<InterfaceEventArgs> service = stub;

			void Handler(object? s, InterfaceEventArgs e) { }

			service.MyEvent += Handler;

			stub.MyEvent.VerifyAdd(Called.Once);
		}

		#endregion

		// ====================================================================
		// Scenario 3: HasSubscribers on generic events
		// ====================================================================

		#region Pattern 1 (Standalone): HasSubscribers

		[Fact]
		public void HasSubscribers_Standalone_ReflectsSubscriptionState()
		{
			var stub = new InterfaceGenericEventsStandaloneKnockOff();
			IInterfaceGenericEvents<InterfaceEventArgs> service = stub;

			Assert.False(stub.MyEvent.HasSubscribers);

			void Handler(object? s, InterfaceEventArgs e) { }
			service.MyEvent += Handler;

			Assert.True(stub.MyEvent.HasSubscribers);

			service.MyEvent -= Handler;

			Assert.False(stub.MyEvent.HasSubscribers);
		}

		#endregion

		#region Pattern 5 (Inline Interface): HasSubscribers

		[Fact]
		public void HasSubscribers_Inline_ReflectsSubscriptionState()
		{
			var stub = new InterfaceGenericEventsInlineTests.Stubs.IInterfaceGenericEvents();
			IInterfaceGenericEvents<InterfaceEventArgs> service = stub;

			Assert.False(stub.MyEvent.HasSubscribers);

			void Handler(object? s, InterfaceEventArgs e) { }
			service.MyEvent += Handler;

			Assert.True(stub.MyEvent.HasSubscribers);

			service.MyEvent -= Handler;

			Assert.False(stub.MyEvent.HasSubscribers);
		}

		#endregion
	}
}
