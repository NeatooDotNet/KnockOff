// ============================================================================
// ExplicitInterfaceImplementationTests: Behavioral tests for explicit interface
// implementations inspired by Rocks.Analysis.IntegrationTests.
//
// Tests two categories:
// 1. Diamond with identical members — two sibling interfaces with the same
//    method, property, indexer, and event names combined via a third interface.
//    Key discovery: KnockOff generates ONE shared interceptor per member name.
//    Both IExplicitOne.A() and IExplicitTwo.A() route to the same AInterceptor.
//
// 2. Covariant return type hiding — new keyword on derived generic interface.
//    The base IIterable.GetIterator() delegates through to the derived
//    IIterable<T>.GetIterator(), so one configuration serves both.
//
// Each edge case is tested with both Standalone (Pattern 1) and Inline (Pattern 5).
// ============================================================================

using KnockOff;

// ============================================================================
// TYPE DEFINITIONS
// ============================================================================

namespace KnockOff.Analysis.Tests.ExplicitInterfaceImplementationTestTypes
{
	// ========================================================================
	// Edge Case 1: Two interfaces with identical member names
	// ========================================================================

	public interface IExplicitOne
	{
		void A();
		int B { get; set; }
		int this[int x] { get; set; }
		event EventHandler C;
	}

	public interface IExplicitTwo
	{
		void A();
		int B { get; set; }
		int this[int x] { get; set; }
		event EventHandler C;
	}

	public interface ICombined : IExplicitOne, IExplicitTwo
	{
	}

	[KnockOff]
	public partial class CombinedStandaloneKnockOff : ICombined
	{
	}

	// ========================================================================
	// Edge Case 2: Covariant return type hiding (new keyword)
	// ========================================================================

	public interface IIterator
	{
		void Iterate();
	}

	public interface IIterator<out T> : IIterator
	{
		new T Iterate();
	}

	public interface IIterable
	{
		IIterator GetIterator();
	}

	public interface IIterable<out T> : IIterable
	{
		new IIterator<T> GetIterator();
	}

	[KnockOff]
	public partial class IterableStandaloneKnockOff<T> : IIterable<T>
	{
	}
}

// ============================================================================
// INLINE PATTERN DECLARATIONS + TESTS
// ============================================================================

namespace KnockOff.Analysis.Tests
{
	using KnockOff.Analysis.Tests.ExplicitInterfaceImplementationTestTypes;

	// Inline stubs
	[KnockOff<ICombined>]
	[KnockOff<IIterable<string>>]
	public partial class ExplicitImplInlineTests
	{
	}

	public class ExplicitInterfaceImplementationTests
	{
		// ====================================================================
		// Edge Case 1: ICombined — two interfaces with identical members
		// ====================================================================
		// DESIGN DISCOVERY: KnockOff generates ONE shared interceptor per
		// member name. Both IExplicitOne.B and IExplicitTwo.B route to the
		// same B interceptor. Configuring stub.B.Get(42) applies to calls
		// through either interface cast. This is intentional: the stub
		// exposes a single configuration surface. The generated explicit
		// implementations both delegate to the same interceptor.
		// ====================================================================

		#region Standalone: Methods

		[Fact]
		public void Combined_Standalone_Method_CallbackExecutedViaIExplicitOne()
		{
			var stub = new CombinedStandaloneKnockOff();
			var callbackExecuted = false;

			stub.A.Call(() => { callbackExecuted = true; });

			var one = (IExplicitOne)stub;
			one.A();

			Assert.True(callbackExecuted);
		}

		[Fact]
		public void Combined_Standalone_Method_CallbackExecutedViaIExplicitTwo()
		{
			var stub = new CombinedStandaloneKnockOff();
			var callbackExecuted = false;

			stub.A.Call(() => { callbackExecuted = true; });

			var two = (IExplicitTwo)stub;
			two.A();

			Assert.True(callbackExecuted);
		}

		[Fact]
		public void Combined_Standalone_Method_SharedInterceptorCountsBothCalls()
		{
			var stub = new CombinedStandaloneKnockOff();

			((IExplicitOne)stub).A();
			((IExplicitTwo)stub).A();

			// Both explicit implementations route to the same AInterceptor,
			// so Verify sees 2 total calls.
			stub.A.Verify(Called.Exactly(2));
		}

		#endregion

		#region Standalone: Properties

		[Fact]
		public void Combined_Standalone_PropertyGet_ReturnsConfiguredValueViaIExplicitOne()
		{
			var stub = new CombinedStandaloneKnockOff();

			stub.B.Get(42);

			var one = (IExplicitOne)stub;
			Assert.Equal(42, one.B);
		}

		[Fact]
		public void Combined_Standalone_PropertyGet_ReturnsConfiguredValueViaIExplicitTwo()
		{
			var stub = new CombinedStandaloneKnockOff();

			stub.B.Get(99);

			var two = (IExplicitTwo)stub;
			Assert.Equal(99, two.B);
		}

		[Fact]
		public void Combined_Standalone_PropertyGet_SharedInterceptorReturnsSameValueViaBothCasts()
		{
			var stub = new CombinedStandaloneKnockOff();

			stub.B.Get(42);

			// Both interface casts see the same configured value because
			// there is one shared B interceptor.
			Assert.Equal(42, ((IExplicitOne)stub).B);
			Assert.Equal(42, ((IExplicitTwo)stub).B);
		}

		[Fact]
		public void Combined_Standalone_PropertySet_TracksSetValueFromEitherCast()
		{
			var stub = new CombinedStandaloneKnockOff();

			((IExplicitOne)stub).B = 10;
			Assert.Equal(10, stub.B.LastSetValue);

			((IExplicitTwo)stub).B = 20;
			Assert.Equal(20, stub.B.LastSetValue);
		}

		[Fact]
		public void Combined_Standalone_PropertySet_VerifyCountsFromBothCasts()
		{
			var stub = new CombinedStandaloneKnockOff();

			((IExplicitOne)stub).B = 10;
			((IExplicitTwo)stub).B = 20;

			stub.B.VerifySet(Called.Exactly(2));
		}

		[Fact]
		public void Combined_Standalone_PropertyGetSet_BackingStoreWorksAcrossCasts()
		{
			var stub = new CombinedStandaloneKnockOff();

			int backingStore = 0;
			stub.B.Get(() => backingStore);
			stub.B.Set(v => backingStore = v);

			((IExplicitOne)stub).B = 42;
			Assert.Equal(42, ((IExplicitTwo)stub).B);

			((IExplicitTwo)stub).B = 99;
			Assert.Equal(99, ((IExplicitOne)stub).B);
		}

		#endregion

		#region Standalone: Indexers

		[Fact]
		public void Combined_Standalone_IndexerGet_ReturnsConfiguredValueViaIExplicitOne()
		{
			var stub = new CombinedStandaloneKnockOff();

			stub.Indexer.Get((int x) => x * 10);

			var one = (IExplicitOne)stub;
			Assert.Equal(30, one[3]);
		}

		[Fact]
		public void Combined_Standalone_IndexerGet_ReturnsConfiguredValueViaIExplicitTwo()
		{
			var stub = new CombinedStandaloneKnockOff();

			stub.Indexer.Get((int x) => x * 10);

			var two = (IExplicitTwo)stub;
			Assert.Equal(50, two[5]);
		}

		[Fact]
		public void Combined_Standalone_IndexerGet_SharedInterceptorReturnsSameViaBothCasts()
		{
			var stub = new CombinedStandaloneKnockOff();

			stub.Indexer.Get((int x) => x * 10);

			Assert.Equal(30, ((IExplicitOne)stub)[3]);
			Assert.Equal(30, ((IExplicitTwo)stub)[3]);
		}

		[Fact]
		public void Combined_Standalone_IndexerSet_TracksSetFromEitherCast()
		{
			var stub = new CombinedStandaloneKnockOff();
			var captured = new Dictionary<int, int>();

			stub.Indexer.Set((int key, int value) => captured[key] = value);

			((IExplicitOne)stub)[1] = 100;
			((IExplicitTwo)stub)[2] = 200;

			Assert.Equal(100, captured[1]);
			Assert.Equal(200, captured[2]);
		}

		[Fact]
		public void Combined_Standalone_IndexerPerKey_WorksViaBothCasts()
		{
			var stub = new CombinedStandaloneKnockOff();

			stub.Indexer[7].Returns(77);

			Assert.Equal(77, ((IExplicitOne)stub)[7]);
			Assert.Equal(77, ((IExplicitTwo)stub)[7]);
		}

		[Fact]
		public void Combined_Standalone_IndexerVerify_CountsBothCasts()
		{
			var stub = new CombinedStandaloneKnockOff();

			stub.Indexer.Get((int x) => x);

			_ = ((IExplicitOne)stub)[1];
			_ = ((IExplicitTwo)stub)[2];

			stub.Indexer.VerifyGet(Called.Exactly(2));
		}

		#endregion

		#region Standalone: Events

		[Fact]
		public void Combined_Standalone_Event_RaiseInvokesHandlerSubscribedViaIExplicitOne()
		{
			var stub = new CombinedStandaloneKnockOff();
			var eventFired = false;

			((IExplicitOne)stub).C += (sender, args) => { eventFired = true; };

			stub.C.Raise(stub, EventArgs.Empty);

			Assert.True(eventFired);
		}

		[Fact]
		public void Combined_Standalone_Event_RaiseInvokesHandlerSubscribedViaIExplicitTwo()
		{
			var stub = new CombinedStandaloneKnockOff();
			var eventFired = false;

			((IExplicitTwo)stub).C += (sender, args) => { eventFired = true; };

			stub.C.Raise(stub, EventArgs.Empty);

			Assert.True(eventFired);
		}

		[Fact]
		public void Combined_Standalone_Event_SharedInterceptorCombinesSubscriptions()
		{
			var stub = new CombinedStandaloneKnockOff();
			int fireCount = 0;

			((IExplicitOne)stub).C += (sender, args) => { fireCount++; };
			((IExplicitTwo)stub).C += (sender, args) => { fireCount++; };

			stub.C.Raise(stub, EventArgs.Empty);

			// Both handlers invoked because they share one event interceptor
			Assert.Equal(2, fireCount);
		}

		[Fact]
		public void Combined_Standalone_Event_VerifyAddCountsBothCasts()
		{
			var stub = new CombinedStandaloneKnockOff();

			((IExplicitOne)stub).C += (sender, args) => { };
			((IExplicitTwo)stub).C += (sender, args) => { };

			stub.C.VerifyAdd(Called.Exactly(2));
		}

		[Fact]
		public void Combined_Standalone_Event_UnsubscribeViaOneCastDoesNotAffectOther()
		{
			var stub = new CombinedStandaloneKnockOff();
			int oneCount = 0;
			int twoCount = 0;

			EventHandler oneHandler = (sender, args) => { oneCount++; };
			EventHandler twoHandler = (sender, args) => { twoCount++; };

			((IExplicitOne)stub).C += oneHandler;
			((IExplicitTwo)stub).C += twoHandler;
			((IExplicitOne)stub).C -= oneHandler;

			stub.C.Raise(stub, EventArgs.Empty);

			Assert.Equal(0, oneCount);
			Assert.Equal(1, twoCount);
		}

		#endregion

		#region Inline: Methods

		[Fact]
		public void Combined_Inline_Method_CallbackExecutedViaIExplicitOne()
		{
			var stub = new ExplicitImplInlineTests.Stubs.ICombined();
			var callbackExecuted = false;

			stub.A.Call(() => { callbackExecuted = true; });

			var one = (IExplicitOne)stub;
			one.A();

			Assert.True(callbackExecuted);
		}

		[Fact]
		public void Combined_Inline_Method_CallbackExecutedViaIExplicitTwo()
		{
			var stub = new ExplicitImplInlineTests.Stubs.ICombined();
			var callbackExecuted = false;

			stub.A.Call(() => { callbackExecuted = true; });

			var two = (IExplicitTwo)stub;
			two.A();

			Assert.True(callbackExecuted);
		}

		[Fact]
		public void Combined_Inline_Method_SharedInterceptorCountsBothCalls()
		{
			var stub = new ExplicitImplInlineTests.Stubs.ICombined();

			((IExplicitOne)stub).A();
			((IExplicitTwo)stub).A();

			stub.A.Verify(Called.Exactly(2));
		}

		#endregion

		#region Inline: Properties

		[Fact]
		public void Combined_Inline_PropertyGet_ReturnsConfiguredValueViaIExplicitOne()
		{
			var stub = new ExplicitImplInlineTests.Stubs.ICombined();

			stub.B.Get(42);

			var one = (IExplicitOne)stub;
			Assert.Equal(42, one.B);
		}

		[Fact]
		public void Combined_Inline_PropertyGet_ReturnsConfiguredValueViaIExplicitTwo()
		{
			var stub = new ExplicitImplInlineTests.Stubs.ICombined();

			stub.B.Get(99);

			var two = (IExplicitTwo)stub;
			Assert.Equal(99, two.B);
		}

		[Fact]
		public void Combined_Inline_PropertyGet_SharedInterceptorReturnsSameValueViaBothCasts()
		{
			var stub = new ExplicitImplInlineTests.Stubs.ICombined();

			stub.B.Get(42);

			Assert.Equal(42, ((IExplicitOne)stub).B);
			Assert.Equal(42, ((IExplicitTwo)stub).B);
		}

		[Fact]
		public void Combined_Inline_PropertySet_TracksSetValueFromEitherCast()
		{
			var stub = new ExplicitImplInlineTests.Stubs.ICombined();

			((IExplicitOne)stub).B = 10;
			Assert.Equal(10, stub.B.LastSetValue);

			((IExplicitTwo)stub).B = 20;
			Assert.Equal(20, stub.B.LastSetValue);
		}

		[Fact]
		public void Combined_Inline_PropertySet_VerifyCountsFromBothCasts()
		{
			var stub = new ExplicitImplInlineTests.Stubs.ICombined();

			((IExplicitOne)stub).B = 10;
			((IExplicitTwo)stub).B = 20;

			stub.B.VerifySet(Called.Exactly(2));
		}

		[Fact]
		public void Combined_Inline_PropertyGetSet_BackingStoreWorksAcrossCasts()
		{
			var stub = new ExplicitImplInlineTests.Stubs.ICombined();

			int backingStore = 0;
			stub.B.Get(() => backingStore);
			stub.B.Set(v => backingStore = v);

			((IExplicitOne)stub).B = 42;
			Assert.Equal(42, ((IExplicitTwo)stub).B);

			((IExplicitTwo)stub).B = 99;
			Assert.Equal(99, ((IExplicitOne)stub).B);
		}

		#endregion

		#region Inline: Indexers

		[Fact]
		public void Combined_Inline_IndexerGet_ReturnsConfiguredValueViaIExplicitOne()
		{
			var stub = new ExplicitImplInlineTests.Stubs.ICombined();

			stub.Indexer.Get((int x) => x * 10);

			var one = (IExplicitOne)stub;
			Assert.Equal(30, one[3]);
		}

		[Fact]
		public void Combined_Inline_IndexerGet_ReturnsConfiguredValueViaIExplicitTwo()
		{
			var stub = new ExplicitImplInlineTests.Stubs.ICombined();

			stub.Indexer.Get((int x) => x * 10);

			var two = (IExplicitTwo)stub;
			Assert.Equal(50, two[5]);
		}

		[Fact]
		public void Combined_Inline_IndexerGet_SharedInterceptorReturnsSameViaBothCasts()
		{
			var stub = new ExplicitImplInlineTests.Stubs.ICombined();

			stub.Indexer.Get((int x) => x * 10);

			Assert.Equal(30, ((IExplicitOne)stub)[3]);
			Assert.Equal(30, ((IExplicitTwo)stub)[3]);
		}

		[Fact]
		public void Combined_Inline_IndexerSet_TracksSetFromEitherCast()
		{
			var stub = new ExplicitImplInlineTests.Stubs.ICombined();
			var captured = new Dictionary<int, int>();

			stub.Indexer.Set((int key, int value) => captured[key] = value);

			((IExplicitOne)stub)[1] = 100;
			((IExplicitTwo)stub)[2] = 200;

			Assert.Equal(100, captured[1]);
			Assert.Equal(200, captured[2]);
		}

		[Fact]
		public void Combined_Inline_IndexerVerify_CountsBothCasts()
		{
			var stub = new ExplicitImplInlineTests.Stubs.ICombined();

			stub.Indexer.Get((int x) => x);

			_ = ((IExplicitOne)stub)[1];
			_ = ((IExplicitTwo)stub)[2];

			stub.Indexer.VerifyGet(Called.Exactly(2));
		}

		#endregion

		#region Inline: Events

		[Fact]
		public void Combined_Inline_Event_RaiseInvokesHandlerSubscribedViaIExplicitOne()
		{
			var stub = new ExplicitImplInlineTests.Stubs.ICombined();
			var eventFired = false;

			((IExplicitOne)stub).C += (sender, args) => { eventFired = true; };

			stub.C.Raise(stub, EventArgs.Empty);

			Assert.True(eventFired);
		}

		[Fact]
		public void Combined_Inline_Event_RaiseInvokesHandlerSubscribedViaIExplicitTwo()
		{
			var stub = new ExplicitImplInlineTests.Stubs.ICombined();
			var eventFired = false;

			((IExplicitTwo)stub).C += (sender, args) => { eventFired = true; };

			stub.C.Raise(stub, EventArgs.Empty);

			Assert.True(eventFired);
		}

		[Fact]
		public void Combined_Inline_Event_SharedInterceptorCombinesSubscriptions()
		{
			var stub = new ExplicitImplInlineTests.Stubs.ICombined();
			int fireCount = 0;

			((IExplicitOne)stub).C += (sender, args) => { fireCount++; };
			((IExplicitTwo)stub).C += (sender, args) => { fireCount++; };

			stub.C.Raise(stub, EventArgs.Empty);

			Assert.Equal(2, fireCount);
		}

		[Fact]
		public void Combined_Inline_Event_VerifyAddCountsBothCasts()
		{
			var stub = new ExplicitImplInlineTests.Stubs.ICombined();

			((IExplicitOne)stub).C += (sender, args) => { };
			((IExplicitTwo)stub).C += (sender, args) => { };

			stub.C.VerifyAdd(Called.Exactly(2));
		}

		[Fact]
		public void Combined_Inline_Event_UnsubscribeViaOneCastDoesNotAffectOther()
		{
			var stub = new ExplicitImplInlineTests.Stubs.ICombined();
			int oneCount = 0;
			int twoCount = 0;

			EventHandler oneHandler = (sender, args) => { oneCount++; };
			EventHandler twoHandler = (sender, args) => { twoCount++; };

			((IExplicitOne)stub).C += oneHandler;
			((IExplicitTwo)stub).C += twoHandler;
			((IExplicitOne)stub).C -= oneHandler;

			stub.C.Raise(stub, EventArgs.Empty);

			Assert.Equal(0, oneCount);
			Assert.Equal(1, twoCount);
		}

		#endregion

		// ====================================================================
		// Edge Case 2: Covariant return type hiding
		// ====================================================================
		// DESIGN DISCOVERY: The base IIterable.GetIterator() implementation
		// delegates through to the derived IIterable<T>.GetIterator().
		// Configuring the stub's GetIterator interceptor once covers both paths.
		// ====================================================================

		#region Standalone: Covariant return type

		[Fact]
		public void Covariant_Standalone_DerivedGetIterator_ReturnsConfiguredValue()
		{
			var stub = new IterableStandaloneKnockOff<string>();

			stub.GetIterator.Call(
				new IterableStandaloneKnockOff<string>.GetIteratorInterceptor.GetIteratorDelegate(() => null!));

			IIterable<string> iterable = stub;
			var iterator = iterable.GetIterator();

			Assert.Null(iterator);
			stub.GetIterator.Verify(Called.Once);
		}

		[Fact]
		public void Covariant_Standalone_BaseGetIteratorViaCast_DelegatesToDerived()
		{
			var stub = new IterableStandaloneKnockOff<string>();

			stub.GetIterator.Call(
				new IterableStandaloneKnockOff<string>.GetIteratorInterceptor.GetIteratorDelegate(() => null!));

			IIterable baseIterable = stub;

			// Calling via base IIterable delegates through IIterable<T>,
			// so the same interceptor handles it.
			var iterator = baseIterable.GetIterator();

			Assert.Null(iterator);
		}

		[Fact]
		public void Covariant_Standalone_DerivedGetIterator_ReturnsNonNullValue()
		{
			var stub = new IterableStandaloneKnockOff<string>();
			var mockIterator = new MockIterator();

			stub.GetIterator.Call(
				new IterableStandaloneKnockOff<string>.GetIteratorInterceptor.GetIteratorDelegate(() => mockIterator));

			IIterable<string> iterable = stub;
			var result = iterable.GetIterator();

			Assert.Same(mockIterator, result);
		}

		[Fact]
		public void Covariant_Standalone_BaseGetIteratorViaCast_ReturnsDerivedResult()
		{
			var stub = new IterableStandaloneKnockOff<string>();
			var mockIterator = new MockIterator();

			stub.GetIterator.Call(
				new IterableStandaloneKnockOff<string>.GetIteratorInterceptor.GetIteratorDelegate(() => mockIterator));

			IIterable baseIterable = stub;
			var result = baseIterable.GetIterator();

			// The base cast returns IIterator, which is the same MockIterator object
			// (since IIterator<T> : IIterator)
			Assert.Same(mockIterator, result);
		}

		#endregion

		#region Inline: Covariant return type

		[Fact]
		public void Covariant_Inline_DerivedGetIterator_ReturnsConfiguredValue()
		{
			var stub = new ExplicitImplInlineTests.Stubs.IIterable();

			stub.GetIterator.Call(() => null!);

			IIterable<string> iterable = stub;
			var iterator = iterable.GetIterator();

			Assert.Null(iterator);
			stub.GetIterator.Verify(Called.Once);
		}

		[Fact]
		public void Covariant_Inline_BaseGetIteratorViaCast_DelegatesToDerived()
		{
			var stub = new ExplicitImplInlineTests.Stubs.IIterable();

			stub.GetIterator.Call(() => null!);

			IIterable baseIterable = stub;
			var iterator = baseIterable.GetIterator();

			Assert.Null(iterator);
		}

		[Fact]
		public void Covariant_Inline_DerivedGetIterator_ReturnsNonNullValue()
		{
			var stub = new ExplicitImplInlineTests.Stubs.IIterable();
			var mockIterator = new MockIterator();

			stub.GetIterator.Return(mockIterator);

			IIterable<string> iterable = stub;
			var result = iterable.GetIterator();

			Assert.Same(mockIterator, result);
		}

		[Fact]
		public void Covariant_Inline_BaseGetIteratorViaCast_ReturnsDerivedResult()
		{
			var stub = new ExplicitImplInlineTests.Stubs.IIterable();
			var mockIterator = new MockIterator();

			stub.GetIterator.Return(mockIterator);

			IIterable baseIterable = stub;
			var result = baseIterable.GetIterator();

			Assert.Same(mockIterator, result);
		}

		[Fact]
		public void Covariant_Inline_GetIterator_VerifiesCallCountAcrossBothPaths()
		{
			var stub = new ExplicitImplInlineTests.Stubs.IIterable();

			stub.GetIterator.Call(() => null!);

			// Call via derived
			((IIterable<string>)stub).GetIterator();
			// Call via base (which delegates to derived internally)
			((IIterable)stub).GetIterator();

			// Derived is called twice (once directly, once via base delegation)
			stub.GetIterator.Verify(Called.Exactly(2));
		}

		#endregion

		// ====================================================================
		// Shared test helper
		// ====================================================================

		private class MockIterator : IIterator<string>
		{
			public string Iterate() => "mock";
			void IIterator.Iterate() { }
		}
	}
}
