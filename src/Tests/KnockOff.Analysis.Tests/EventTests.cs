// ============================================================================
// EventTests: Edge case tests for EventHandler<T> where T does NOT extend
// EventArgs. Most event patterns assume T : EventArgs, but .NET Core removed
// that constraint from EventHandler<T>. This verifies KnockOff handles
// arbitrary T correctly.
// Inspired by Rocks.Analysis.IntegrationTests.EventTests
//
// Target interface:
//   public class NotEventArgs { }  // NOT : EventArgs
//   public interface IUseNotEventArgs
//   {
//       void A();
//       event EventHandler<NotEventArgs> NotEvent;
//   }
//
// Tests exercise the INTERFACE pipeline for:
// 1. Raise event with custom args — verify handler receives correct instance
// 2. Event add/remove tracking — VerifyAdd, VerifyRemove
// 3. Multiple subscribers — two handlers, raise, both fire
// 4. Unsubscribe — subscribe, unsubscribe, raise, handler NOT fired
//
// Applicable patterns (interface only):
// - Pattern 1 (Standalone): [KnockOff] partial class Stub : IUseNotEventArgs {}
// - Pattern 5 (Inline): [KnockOff<IUseNotEventArgs>]
// ============================================================================

using KnockOff;

// ============================================================================
// TYPE DEFINITIONS
// ============================================================================

namespace KnockOff.Analysis.Tests.EventTestTypes
{
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
	public class NotEventArgs { }
#pragma warning restore CA1711 // Identifiers should not have incorrect suffix

	public interface IUseNotEventArgs
	{
		void A();
		event EventHandler<NotEventArgs> NotEvent;
	}

	// Pattern 1: Standalone interface stub
	[KnockOff]
	public partial class NotEventArgsStandaloneKnockOff : IUseNotEventArgs
	{
	}
}

// ============================================================================
// INLINE PATTERN DECLARATIONS + TESTS
// ============================================================================

namespace KnockOff.Analysis.Tests
{
	using KnockOff.Analysis.Tests.EventTestTypes;

	// Pattern 5: Inline interface stub
	[KnockOff<IUseNotEventArgs>]
	public partial class EventInlineTests
	{
	}

	public class EventTests
	{
		// ====================================================================
		// Scenario 1: Raise event with custom args
		// Subscribe to the event, raise from a method callback, verify the
		// handler received the correct NotEventArgs instance.
		// ====================================================================

		#region Standalone: Raise event with custom args

		[Fact]
		public void RaiseWithCustomArgs_Standalone_HandlerReceivesCorrectInstance()
		{
			var stub = new NotEventArgsStandaloneKnockOff();
			var eventArgs = new NotEventArgs();
			NotEventArgs? receivedArgs = null;

			stub.A.Call(() =>
			{
				stub.NotEvent.Raise(stub, eventArgs);
			});

			((IUseNotEventArgs)stub).NotEvent += (sender, args) => receivedArgs = args;

			((IUseNotEventArgs)stub).A();

			Assert.Same(eventArgs, receivedArgs);
		}

		#endregion

		#region Inline: Raise event with custom args

		[Fact]
		public void RaiseWithCustomArgs_Inline_HandlerReceivesCorrectInstance()
		{
			var stub = new EventInlineTests.Stubs.IUseNotEventArgs();
			var eventArgs = new NotEventArgs();
			NotEventArgs? receivedArgs = null;

			stub.A.Call(() =>
			{
				stub.NotEvent.Raise(stub, eventArgs);
			});

			((IUseNotEventArgs)stub).NotEvent += (sender, args) => receivedArgs = args;

			((IUseNotEventArgs)stub).A();

			Assert.Same(eventArgs, receivedArgs);
		}

		#endregion

		// ====================================================================
		// Scenario 2: Event add/remove tracking
		// Subscribe to the event, verify add is tracked. Unsubscribe, verify
		// remove is tracked.
		// ====================================================================

		#region Standalone: Event add/remove tracking

		[Fact]
		public void AddRemoveTracking_Standalone_VerifyAddAndRemove()
		{
			var stub = new NotEventArgsStandaloneKnockOff();

			EventHandler<NotEventArgs> handler = (sender, args) => { };

			((IUseNotEventArgs)stub).NotEvent += handler;
			((IUseNotEventArgs)stub).NotEvent -= handler;

			stub.NotEvent.VerifyAdd(Called.Once);
			stub.NotEvent.VerifyRemove(Called.Once);
		}

		#endregion

		#region Inline: Event add/remove tracking

		[Fact]
		public void AddRemoveTracking_Inline_VerifyAddAndRemove()
		{
			var stub = new EventInlineTests.Stubs.IUseNotEventArgs();

			EventHandler<NotEventArgs> handler = (sender, args) => { };

			((IUseNotEventArgs)stub).NotEvent += handler;
			((IUseNotEventArgs)stub).NotEvent -= handler;

			stub.NotEvent.VerifyAdd(Called.Once);
			stub.NotEvent.VerifyRemove(Called.Once);
		}

		#endregion

		// ====================================================================
		// Scenario 3: Multiple subscribers
		// Subscribe two handlers, raise, verify both fire.
		// ====================================================================

		#region Standalone: Multiple subscribers both fire

		[Fact]
		public void MultipleSubscribers_Standalone_BothHandlersFire()
		{
			var stub = new NotEventArgsStandaloneKnockOff();
			var eventArgs = new NotEventArgs();
			var handler1Fired = false;
			var handler2Fired = false;

			((IUseNotEventArgs)stub).NotEvent += (sender, args) => handler1Fired = true;
			((IUseNotEventArgs)stub).NotEvent += (sender, args) => handler2Fired = true;

			stub.NotEvent.Raise(stub, eventArgs);

			Assert.True(handler1Fired);
			Assert.True(handler2Fired);
			stub.NotEvent.VerifyAdd(Called.Exactly(2));
		}

		#endregion

		#region Inline: Multiple subscribers both fire

		[Fact]
		public void MultipleSubscribers_Inline_BothHandlersFire()
		{
			var stub = new EventInlineTests.Stubs.IUseNotEventArgs();
			var eventArgs = new NotEventArgs();
			var handler1Fired = false;
			var handler2Fired = false;

			((IUseNotEventArgs)stub).NotEvent += (sender, args) => handler1Fired = true;
			((IUseNotEventArgs)stub).NotEvent += (sender, args) => handler2Fired = true;

			stub.NotEvent.Raise(stub, eventArgs);

			Assert.True(handler1Fired);
			Assert.True(handler2Fired);
			stub.NotEvent.VerifyAdd(Called.Exactly(2));
		}

		#endregion

		// ====================================================================
		// Scenario 4: Unsubscribe
		// Subscribe, unsubscribe, raise. Handler should NOT fire.
		// ====================================================================

		#region Standalone: Unsubscribe prevents handler from firing

		[Fact]
		public void Unsubscribe_Standalone_HandlerNotFired()
		{
			var stub = new NotEventArgsStandaloneKnockOff();
			var eventArgs = new NotEventArgs();
			var handlerFired = false;

			EventHandler<NotEventArgs> handler = (sender, args) => handlerFired = true;

			((IUseNotEventArgs)stub).NotEvent += handler;
			((IUseNotEventArgs)stub).NotEvent -= handler;

			stub.NotEvent.Raise(stub, eventArgs);

			Assert.False(handlerFired);
			stub.NotEvent.VerifyAdd(Called.Once);
			stub.NotEvent.VerifyRemove(Called.Once);
		}

		#endregion

		#region Inline: Unsubscribe prevents handler from firing

		[Fact]
		public void Unsubscribe_Inline_HandlerNotFired()
		{
			var stub = new EventInlineTests.Stubs.IUseNotEventArgs();
			var eventArgs = new NotEventArgs();
			var handlerFired = false;

			EventHandler<NotEventArgs> handler = (sender, args) => handlerFired = true;

			((IUseNotEventArgs)stub).NotEvent += handler;
			((IUseNotEventArgs)stub).NotEvent -= handler;

			stub.NotEvent.Raise(stub, eventArgs);

			Assert.False(handlerFired);
			stub.NotEvent.VerifyAdd(Called.Once);
			stub.NotEvent.VerifyRemove(Called.Once);
		}

		#endregion
	}
}
