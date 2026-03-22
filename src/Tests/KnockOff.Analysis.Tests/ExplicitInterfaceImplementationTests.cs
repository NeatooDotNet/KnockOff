// ============================================================================
// ExplicitInterfaceImplementationTests: Edge case tests for explicit interface
// implementations inspired by Rocks.Analysis.IntegrationTests.
//
// Tests two categories:
// 1. Diamond with identical members — two sibling interfaces with the same
//    method, property, indexer, and event names combined via a third interface
// 2. Covariant return type hiding — new keyword on derived generic interface
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

		#region Standalone: ICombined compiles

		[Fact]
		public void Combined_Standalone_CompilesAndImplementsInterface()
		{
			var knockOff = new CombinedStandaloneKnockOff();
			ICombined combined = knockOff;

			// Basic compile check — the stub should compile successfully
			Assert.NotNull(knockOff);
		}

		[Fact]
		public void Combined_Standalone_CanCallMethodViaIExplicitOne()
		{
			var knockOff = new CombinedStandaloneKnockOff();
			var one = (IExplicitOne)knockOff;

			// Should be able to call the method via IExplicitOne
			one.A();
		}

		[Fact]
		public void Combined_Standalone_CanCallMethodViaIExplicitTwo()
		{
			var knockOff = new CombinedStandaloneKnockOff();
			var two = (IExplicitTwo)knockOff;

			// Should be able to call the method via IExplicitTwo
			two.A();
		}

		[Fact]
		public void Combined_Standalone_CanAccessPropertyViaIExplicitOne()
		{
			var knockOff = new CombinedStandaloneKnockOff();
			var one = (IExplicitOne)knockOff;

			// Should be able to get/set properties via IExplicitOne
			one.B = 42;
			var val = one.B;
		}

		[Fact]
		public void Combined_Standalone_CanAccessPropertyViaIExplicitTwo()
		{
			var knockOff = new CombinedStandaloneKnockOff();
			var two = (IExplicitTwo)knockOff;

			// Should be able to get/set properties via IExplicitTwo
			two.B = 99;
			var val = two.B;
		}

		[Fact]
		public void Combined_Standalone_CanAccessIndexerViaIExplicitOne()
		{
			var knockOff = new CombinedStandaloneKnockOff();
			var one = (IExplicitOne)knockOff;

			// Should be able to access indexer via IExplicitOne
			one[0] = 42;
			var val = one[0];
		}

		[Fact]
		public void Combined_Standalone_CanAccessIndexerViaIExplicitTwo()
		{
			var knockOff = new CombinedStandaloneKnockOff();
			var two = (IExplicitTwo)knockOff;

			// Should be able to access indexer via IExplicitTwo
			two[0] = 99;
			var val = two[0];
		}

		[Fact]
		public void Combined_Standalone_CanSubscribeEventViaIExplicitOne()
		{
			var knockOff = new CombinedStandaloneKnockOff();
			var one = (IExplicitOne)knockOff;

			EventHandler handler = (sender, args) => { };
			one.C += handler;
			one.C -= handler;
		}

		[Fact]
		public void Combined_Standalone_CanSubscribeEventViaIExplicitTwo()
		{
			var knockOff = new CombinedStandaloneKnockOff();
			var two = (IExplicitTwo)knockOff;

			EventHandler handler = (sender, args) => { };
			two.C += handler;
			two.C -= handler;
		}

		#endregion

		#region Inline: ICombined compiles

		[Fact]
		public void Combined_Inline_CompilesAndImplementsInterface()
		{
			var stub = new ExplicitImplInlineTests.Stubs.ICombined();
			ICombined combined = stub;

			Assert.NotNull(stub);
		}

		[Fact]
		public void Combined_Inline_CanCallMethodViaIExplicitOne()
		{
			var stub = new ExplicitImplInlineTests.Stubs.ICombined();
			var one = (IExplicitOne)stub;

			one.A();
		}

		[Fact]
		public void Combined_Inline_CanCallMethodViaIExplicitTwo()
		{
			var stub = new ExplicitImplInlineTests.Stubs.ICombined();
			var two = (IExplicitTwo)stub;

			two.A();
		}

		[Fact]
		public void Combined_Inline_CanAccessPropertyViaIExplicitOne()
		{
			var stub = new ExplicitImplInlineTests.Stubs.ICombined();
			var one = (IExplicitOne)stub;

			one.B = 42;
			var val = one.B;
		}

		[Fact]
		public void Combined_Inline_CanAccessPropertyViaIExplicitTwo()
		{
			var stub = new ExplicitImplInlineTests.Stubs.ICombined();
			var two = (IExplicitTwo)stub;

			two.B = 99;
			var val = two.B;
		}

		[Fact]
		public void Combined_Inline_CanAccessIndexerViaIExplicitOne()
		{
			var stub = new ExplicitImplInlineTests.Stubs.ICombined();
			var one = (IExplicitOne)stub;

			one[0] = 42;
			var val = one[0];
		}

		[Fact]
		public void Combined_Inline_CanAccessIndexerViaIExplicitTwo()
		{
			var stub = new ExplicitImplInlineTests.Stubs.ICombined();
			var two = (IExplicitTwo)stub;

			two[0] = 99;
			var val = two[0];
		}

		[Fact]
		public void Combined_Inline_CanSubscribeEventViaIExplicitOne()
		{
			var stub = new ExplicitImplInlineTests.Stubs.ICombined();
			var one = (IExplicitOne)stub;

			EventHandler handler = (sender, args) => { };
			one.C += handler;
			one.C -= handler;
		}

		[Fact]
		public void Combined_Inline_CanSubscribeEventViaIExplicitTwo()
		{
			var stub = new ExplicitImplInlineTests.Stubs.ICombined();
			var two = (IExplicitTwo)stub;

			EventHandler handler = (sender, args) => { };
			two.C += handler;
			two.C -= handler;
		}

		#endregion

		// ====================================================================
		// Edge Case 2: Covariant return type hiding
		// ====================================================================

		#region Standalone: Covariant return type

		[Fact]
		public void Covariant_Standalone_CompilesAndImplementsInterface()
		{
			var knockOff = new IterableStandaloneKnockOff<string>();
			IIterable<string> iterable = knockOff;

			Assert.NotNull(knockOff);
		}

		[Fact]
		public void Covariant_Standalone_CanCallDerivedGetIterator()
		{
			var knockOff = new IterableStandaloneKnockOff<string>();
			IIterable<string> iterable = knockOff;

			// Configure the derived (IIterator<T>) overload
			// Use typed delegate to disambiguate between GetIteratorDelegate and GetIteratorDelegate2
			knockOff.GetIterator.Call(
				new IterableStandaloneKnockOff<string>.GetIteratorInterceptor.GetIteratorDelegate(() => null!));

			// IIterable<T>.GetIterator() returns IIterator<T>
			var iterator = iterable.GetIterator();
			knockOff.GetIterator.Verify(Called.Once);
		}

		[Fact]
		public void Covariant_Standalone_CanCallBaseGetIteratorViaCast()
		{
			var knockOff = new IterableStandaloneKnockOff<string>();
			IIterable baseIterable = knockOff;

			// Configure the derived (IIterator<T>) overload -- the base delegates through the derived
			knockOff.GetIterator.Call(
				new IterableStandaloneKnockOff<string>.GetIteratorInterceptor.GetIteratorDelegate(() => null!));

			// IIterable.GetIterator() returns IIterator (non-generic)
			// The base IIterable.GetIterator() delegates to IIterable<T>.GetIterator()
			var iterator = baseIterable.GetIterator();
		}

		#endregion

		#region Inline: Covariant return type

		[Fact]
		public void Covariant_Inline_CompilesAndImplementsInterface()
		{
			var stub = new ExplicitImplInlineTests.Stubs.IIterable();
			IIterable<string> iterable = stub;

			Assert.NotNull(stub);
		}

		[Fact]
		public void Covariant_Inline_CanCallDerivedGetIterator()
		{
			var stub = new ExplicitImplInlineTests.Stubs.IIterable();
			IIterable<string> iterable = stub;

			// Configure derived GetIterator
			stub.GetIterator.Call(() => null!);

			// IIterable<T>.GetIterator() returns IIterator<T>
			var iterator = iterable.GetIterator();
			stub.GetIterator.Verify(Called.Once);
		}

		[Fact]
		public void Covariant_Inline_CanCallBaseGetIteratorViaCast()
		{
			var stub = new ExplicitImplInlineTests.Stubs.IIterable();
			IIterable baseIterable = stub;

			// Configure derived GetIterator (base will delegate through derived)
			stub.GetIterator.Call(() => null!);

			// IIterable.GetIterator() returns IIterator (non-generic)
			var iterator = baseIterable.GetIterator();
		}

		#endregion
	}
}
