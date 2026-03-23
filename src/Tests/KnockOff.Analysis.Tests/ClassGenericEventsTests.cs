// ============================================================================
// ClassGenericEventsTests: Edge case tests for a generic class with a virtual
// event using a generic EventArgs type parameter.
// Inspired by Rocks.Analysis.IntegrationTests.ClassGenericEventsTests
//
// Target class:
//   public sealed class ClassEventArgs : EventArgs { }
//   public class ClassGenericEvents<T> where T : EventArgs
//   {
//       public virtual void Foo() { }
//       public virtual event EventHandler<T>? MyEvent;
//   }
//
// Tests exercise the CLASS pipeline for:
// 1. Call Foo, raise generic MyEvent from callback, verify handler invoked
// 2. Verify event args type is correct (ClassEventArgs)
// 3. Verify event add/remove tracking
//
// Applicable patterns (generic class):
// - Pattern 3 (Standalone Class): [KnockOffBase<ClassGenericEvents<ClassEventArgs>>]
// - Pattern 4 (Generic Standalone Class): [KnockOffBase(typeof(ClassGenericEvents<>))]
// - Pattern 6 (Inline Class): [KnockOff<ClassGenericEvents<ClassEventArgs>>]
// - Pattern 9 (Open Generic Class): [KnockOff(typeof(ClassGenericEvents<>))]
// ============================================================================

using KnockOff;

// ============================================================================
// TYPE DEFINITIONS
// ============================================================================

namespace KnockOff.Analysis.Tests.ClassGenericEventsTestTypes
{
	public sealed class ClassEventArgs : EventArgs { }

	public class ClassGenericEvents<T> where T : EventArgs
	{
		public virtual void Foo() { }

#pragma warning disable CA1070 // Do not declare event fields as virtual
#pragma warning disable CS0067
		public virtual event EventHandler<T>? MyEvent;
#pragma warning restore CS0067
#pragma warning restore CA1070 // Do not declare event fields as virtual
	}

	// Pattern 3: Standalone class stub (closed generic)
	[KnockOffBase<ClassGenericEvents<ClassEventArgs>>]
	public partial class ClassGenericEventsStandaloneKnockOff
	{
	}

	// Pattern 4: Generic standalone class stub (open generic)
	[KnockOffBase(typeof(ClassGenericEvents<>))]
	public partial class ClassGenericEventsGenericStandaloneKnockOff<T> where T : EventArgs
	{
	}
}

// ============================================================================
// INLINE PATTERN DECLARATIONS + TESTS
// ============================================================================

namespace KnockOff.Analysis.Tests
{
	using KnockOff.Analysis.Tests.ClassGenericEventsTestTypes;

	// Pattern 6: Inline class stub (closed generic)
	[KnockOff<ClassGenericEvents<ClassEventArgs>>]
	public partial class ClassGenericEventsInlineTests
	{
	}

	// Pattern 9: Open generic class stub
	[KnockOff(typeof(ClassGenericEvents<>))]
	public partial class ClassGenericEventsOpenGenericTests
	{
	}

	public class ClassGenericEventsTests
	{
		// ====================================================================
		// Scenario 1: Call Foo, raise generic MyEvent from callback, verify
		// handler invoked with correct EventArgs type
		// ====================================================================

		#region Pattern 3 (Standalone Class): Raise generic event from callback

		[Fact]
		public void RaiseGenericEvent_StandaloneClass_HandlerInvoked()
		{
			var stub = new ClassGenericEventsStandaloneKnockOff();
			ClassGenericEvents<ClassEventArgs> obj = stub.Object;

			var receivedArgs = default(ClassEventArgs);
			var eventArgs = new ClassEventArgs();

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
			var stub = new ClassGenericEventsStandaloneKnockOff();
			ClassGenericEvents<ClassEventArgs> obj = stub.Object;

			object? receivedSender = null;

			stub.Foo.Call(() =>
			{
				stub.MyEvent.Raise(obj, new ClassEventArgs());
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
			var stub = new ClassGenericEventsGenericStandaloneKnockOff<ClassEventArgs>();
			ClassGenericEvents<ClassEventArgs> obj = stub.Object;

			var receivedArgs = default(ClassEventArgs);
			var eventArgs = new ClassEventArgs();

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
			var stub = new ClassGenericEventsInlineTests.Stubs.ClassGenericEvents();
			ClassGenericEvents<ClassEventArgs> obj = stub.Object;

			var receivedArgs = default(ClassEventArgs);
			var eventArgs = new ClassEventArgs();

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
			var stub = new ClassGenericEventsInlineTests.Stubs.ClassGenericEvents();
			ClassGenericEvents<ClassEventArgs> obj = stub.Object;

			object? receivedSender = null;

			stub.Foo.Call(() =>
			{
				stub.MyEvent.Raise(obj, new ClassEventArgs());
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
			var stub = new ClassGenericEventsOpenGenericTests.Stubs.ClassGenericEvents<ClassEventArgs>();
			ClassGenericEvents<ClassEventArgs> obj = stub.Object;

			var receivedArgs = default(ClassEventArgs);
			var eventArgs = new ClassEventArgs();

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
		// Scenario 2: Verify event add/remove tracking on generic events
		// ====================================================================

		#region Pattern 3 (Standalone Class): Event add/remove tracking

		[Fact]
		public void VerifyAdd_StandaloneClass_TracksSubscription()
		{
			var stub = new ClassGenericEventsStandaloneKnockOff();
			ClassGenericEvents<ClassEventArgs> obj = stub.Object;

			void Handler(object? s, ClassEventArgs e) { }

			obj.MyEvent += Handler;

			stub.MyEvent.VerifyAdd(Called.Once);
		}

		[Fact]
		public void VerifyAddRemove_StandaloneClass_TracksUnsubscription()
		{
			var stub = new ClassGenericEventsStandaloneKnockOff();
			ClassGenericEvents<ClassEventArgs> obj = stub.Object;

			void Handler(object? s, ClassEventArgs e) { }

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
			var stub = new ClassGenericEventsGenericStandaloneKnockOff<ClassEventArgs>();
			ClassGenericEvents<ClassEventArgs> obj = stub.Object;

			void Handler(object? s, ClassEventArgs e) { }

			obj.MyEvent += Handler;

			stub.MyEvent.VerifyAdd(Called.Once);
		}

		#endregion

		#region Pattern 6 (Inline Class): Event add/remove tracking

		[Fact]
		public void VerifyAdd_InlineClass_TracksSubscription()
		{
			var stub = new ClassGenericEventsInlineTests.Stubs.ClassGenericEvents();
			ClassGenericEvents<ClassEventArgs> obj = stub.Object;

			void Handler(object? s, ClassEventArgs e) { }

			obj.MyEvent += Handler;

			stub.MyEvent.VerifyAdd(Called.Once);
		}

		[Fact]
		public void VerifyAddRemove_InlineClass_TracksUnsubscription()
		{
			var stub = new ClassGenericEventsInlineTests.Stubs.ClassGenericEvents();
			ClassGenericEvents<ClassEventArgs> obj = stub.Object;

			void Handler(object? s, ClassEventArgs e) { }

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
			var stub = new ClassGenericEventsOpenGenericTests.Stubs.ClassGenericEvents<ClassEventArgs>();
			ClassGenericEvents<ClassEventArgs> obj = stub.Object;

			void Handler(object? s, ClassEventArgs e) { }

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
			var stub = new ClassGenericEventsStandaloneKnockOff();
			ClassGenericEvents<ClassEventArgs> obj = stub.Object;

			Assert.False(stub.MyEvent.HasSubscribers);

			void Handler(object? s, ClassEventArgs e) { }
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
			var stub = new ClassGenericEventsInlineTests.Stubs.ClassGenericEvents();
			ClassGenericEvents<ClassEventArgs> obj = stub.Object;

			Assert.False(stub.MyEvent.HasSubscribers);

			void Handler(object? s, ClassEventArgs e) { }
			obj.MyEvent += Handler;

			Assert.True(stub.MyEvent.HasSubscribers);

			obj.MyEvent -= Handler;

			Assert.False(stub.MyEvent.HasSubscribers);
		}

		#endregion
	}
}
