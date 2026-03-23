// ============================================================================
// AbstractClassConstructorTests: Edge case tests for abstract class constructors
// Inspired by Rocks.Analysis.IntegrationTests.AbstractClassConstructorTests
//
// Tests two categories of abstract class constructor edge cases:
// 1. Abstract class with multiple constructors (public + protected)
//    The stub's Impl class must chain to the base constructor.
// 2. Abstract class with an abstract method to verify the stub is functional
//    after construction.
//
// Key difference from ClassConstructorTests: the base class is ABSTRACT.
// The Impl class cannot instantiate the base class directly — it must derive
// from it and chain constructor calls. Abstract members must be implemented.
//
// Applicable patterns (class only):
// - Pattern 3 (Standalone Class): [KnockOffBase<T>]
// - Pattern 6 (Inline Class): [KnockOff<T>]
// ============================================================================

using KnockOff;

// ============================================================================
// TYPE DEFINITIONS
// ============================================================================

namespace KnockOff.Analysis.Tests.AbstractClassConstructorTestTypes
{
	// ========================================================================
	// Edge Case 1: Abstract class with multiple constructors (public + protected)
	// ========================================================================

	public abstract class AbstractClassConstructor
	{
		protected AbstractClassConstructor(string stringData) =>
			this.StringData = stringData;
		public AbstractClassConstructor(int intData) =>
			this.IntData = intData;

		public abstract int NoParameters();

		public int IntData { get; }
		public string? StringData { get; }
	}

	// ========================================================================
	// Edge Case 2: Abstract class with default (parameterless) constructor
	// ========================================================================

	public abstract class AbstractClassDefaultConstructor
	{
		public abstract void DoWork();
	}

	// Pattern 3: Standalone class stubs
	[KnockOffBase<AbstractClassConstructor>]
	public partial class AbstractClassConstructorStandaloneKnockOff
	{
	}

	[KnockOffBase<AbstractClassDefaultConstructor>]
	public partial class AbstractClassDefaultConstructorStandaloneKnockOff
	{
	}
}

// ============================================================================
// INLINE PATTERN DECLARATIONS + TESTS
// ============================================================================

namespace KnockOff.Analysis.Tests
{
	using KnockOff.Analysis.Tests.AbstractClassConstructorTestTypes;

	// Pattern 6: Inline class stubs
	[KnockOff<AbstractClassConstructor>]
	[KnockOff<AbstractClassDefaultConstructor>]
	public partial class AbstractClassConstructorInlineTests
	{
	}

	public class AbstractClassConstructorTests
	{
		// ====================================================================
		// Edge Case 1: Multiple constructors (public + protected)
		// The Impl class must chain to the base abstract class constructor.
		// ====================================================================

		#region Standalone Class: public constructor

		[Fact]
		public void PublicConstructor_StandaloneClass_IntDataSetCorrectly()
		{
			var stub = new AbstractClassConstructorStandaloneKnockOff(3);
			AbstractClassConstructor obj = stub.Object;

			Assert.Equal(3, obj.IntData);
			Assert.Null(obj.StringData);
		}

		[Fact]
		public void PublicConstructor_StandaloneClass_AbstractMethodCanBeCalled()
		{
			var stub = new AbstractClassConstructorStandaloneKnockOff(3);
			AbstractClassConstructor obj = stub.Object;

			// Unconfigured abstract method returns default(int) = 0
			var result = obj.NoParameters();

			Assert.Equal(0, result);
			stub.NoParameters.Verify(Called.Once);
		}

		[Fact]
		public void PublicConstructor_StandaloneClass_ConfiguredReturnValue()
		{
			var stub = new AbstractClassConstructorStandaloneKnockOff(3);
			AbstractClassConstructor obj = stub.Object;

			stub.NoParameters.Return(42);

			var result = obj.NoParameters();

			Assert.Equal(42, result);
		}

		#endregion

		#region Standalone Class: protected constructor

		[Fact]
		public void ProtectedConstructor_StandaloneClass_StringDataSetCorrectly()
		{
			var stub = new AbstractClassConstructorStandaloneKnockOff("b");
			AbstractClassConstructor obj = stub.Object;

			Assert.Equal(0, obj.IntData);
			Assert.Equal("b", obj.StringData);
		}

		[Fact]
		public void ProtectedConstructor_StandaloneClass_AbstractMethodCanBeCalled()
		{
			var stub = new AbstractClassConstructorStandaloneKnockOff("b");
			AbstractClassConstructor obj = stub.Object;

			var result = obj.NoParameters();

			Assert.Equal(0, result);
			stub.NoParameters.Verify(Called.Once);
		}

		#endregion

		#region Inline Class: public constructor

		[Fact]
		public void PublicConstructor_InlineClass_IntDataSetCorrectly()
		{
			var stub = new AbstractClassConstructorInlineTests.Stubs.AbstractClassConstructor(3);
			AbstractClassConstructor obj = stub.Object;

			Assert.Equal(3, obj.IntData);
			Assert.Null(obj.StringData);
		}

		[Fact]
		public void PublicConstructor_InlineClass_AbstractMethodCanBeCalled()
		{
			var stub = new AbstractClassConstructorInlineTests.Stubs.AbstractClassConstructor(3);
			AbstractClassConstructor obj = stub.Object;

			var result = obj.NoParameters();

			Assert.Equal(0, result);
			stub.NoParameters.Verify(Called.Once);
		}

		[Fact]
		public void PublicConstructor_InlineClass_ConfiguredReturnValue()
		{
			var stub = new AbstractClassConstructorInlineTests.Stubs.AbstractClassConstructor(3);
			AbstractClassConstructor obj = stub.Object;

			stub.NoParameters.Return(42);

			var result = obj.NoParameters();

			Assert.Equal(42, result);
		}

		#endregion

		#region Inline Class: protected constructor

		[Fact]
		public void ProtectedConstructor_InlineClass_StringDataSetCorrectly()
		{
			var stub = new AbstractClassConstructorInlineTests.Stubs.AbstractClassConstructor("b");
			AbstractClassConstructor obj = stub.Object;

			Assert.Equal(0, obj.IntData);
			Assert.Equal("b", obj.StringData);
		}

		[Fact]
		public void ProtectedConstructor_InlineClass_AbstractMethodCanBeCalled()
		{
			var stub = new AbstractClassConstructorInlineTests.Stubs.AbstractClassConstructor("b");
			AbstractClassConstructor obj = stub.Object;

			var result = obj.NoParameters();

			Assert.Equal(0, result);
			stub.NoParameters.Verify(Called.Once);
		}

		#endregion

		// ====================================================================
		// Edge Case 2: Default (parameterless) constructor
		// Abstract class with implicit default constructor.
		// ====================================================================

		#region Standalone Class: default constructor

		[Fact]
		public void DefaultConstructor_StandaloneClass_CanInstantiate()
		{
			var stub = new AbstractClassDefaultConstructorStandaloneKnockOff();
			AbstractClassDefaultConstructor obj = stub.Object;

			Assert.NotNull(obj);
		}

		[Fact]
		public void DefaultConstructor_StandaloneClass_AbstractMethodCanBeCalled()
		{
			var stub = new AbstractClassDefaultConstructorStandaloneKnockOff();
			AbstractClassDefaultConstructor obj = stub.Object;

			obj.DoWork();

			stub.DoWork.Verify(Called.Once);
		}

		[Fact]
		public void DefaultConstructor_StandaloneClass_CallbackOnAbstractMethod()
		{
			var stub = new AbstractClassDefaultConstructorStandaloneKnockOff();
			AbstractClassDefaultConstructor obj = stub.Object;

			var callbackInvoked = false;
			stub.DoWork.Call(() => { callbackInvoked = true; });

			obj.DoWork();

			Assert.True(callbackInvoked);
		}

		#endregion

		#region Inline Class: default constructor

		[Fact]
		public void DefaultConstructor_InlineClass_CanInstantiate()
		{
			var stub = new AbstractClassConstructorInlineTests.Stubs.AbstractClassDefaultConstructor();
			AbstractClassDefaultConstructor obj = stub.Object;

			Assert.NotNull(obj);
		}

		[Fact]
		public void DefaultConstructor_InlineClass_AbstractMethodCanBeCalled()
		{
			var stub = new AbstractClassConstructorInlineTests.Stubs.AbstractClassDefaultConstructor();
			AbstractClassDefaultConstructor obj = stub.Object;

			obj.DoWork();

			stub.DoWork.Verify(Called.Once);
		}

		[Fact]
		public void DefaultConstructor_InlineClass_CallbackOnAbstractMethod()
		{
			var stub = new AbstractClassConstructorInlineTests.Stubs.AbstractClassDefaultConstructor();
			AbstractClassDefaultConstructor obj = stub.Object;

			var callbackInvoked = false;
			stub.DoWork.Call(() => { callbackInvoked = true; });

			obj.DoWork();

			Assert.True(callbackInvoked);
		}

		#endregion
	}
}
