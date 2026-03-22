// ============================================================================
// ConstraintTests: Edge case tests inspired by Rocks.Analysis.IntegrationTests
//
// Tests abstract generic class with method that has its own constraint:
//   BaseForConstraintCase<T> where T : class
//     abstract BaseForConstraintCase<TTarget> As<TTarget>() where TTarget : class
//
// This exercises:
// - Class-level type parameter with constraint (T : class)
// - Method-level type parameter with constraint (TTarget : class)
// - Abstract method returning a type using the class-level generic parameter
//
// Patterns tested:
// - Pattern 3 (Standalone Class): [KnockOffBase<BaseForConstraintCase<string>>]
// - Pattern 4 (Generic Standalone Class): [KnockOffBase(typeof(BaseForConstraintCase<>))]
// - Pattern 6 (Inline Class): [KnockOff<BaseForConstraintCase<string>>]
// - Pattern 9 (Open Generic Class): [KnockOff(typeof(BaseForConstraintCase<>))]
// ============================================================================

using KnockOff;

// ============================================================================
// TYPE DEFINITIONS
// ============================================================================

namespace KnockOff.Analysis.Tests.ConstraintTestTypes
{
	public class BaseForConstraintCase { }

	public abstract class BaseForConstraintCase<T>
		: BaseForConstraintCase where T : class
	{
		public abstract BaseForConstraintCase<TTarget> As<TTarget>() where TTarget : class;
	}

	// Pattern 3: Standalone class stub
	[KnockOffBase<BaseForConstraintCase<string>>]
	public partial class ConstraintCaseStandaloneKnockOff
	{
	}

	// Pattern 4: Generic standalone class stub
	[KnockOffBase(typeof(BaseForConstraintCase<>))]
	public partial class ConstraintCaseOpenStandaloneKnockOff<T> where T : class
	{
	}
}

// ============================================================================
// INLINE PATTERN DECLARATIONS + TESTS
// ============================================================================

namespace KnockOff.Analysis.Tests
{
	using KnockOff.Analysis.Tests.ConstraintTestTypes;

	// Pattern 6: Inline class stub
	[KnockOff<BaseForConstraintCase<string>>]
	public partial class ConstraintInlineTests
	{
	}

	// Pattern 9: Open Generic Class stub
	[KnockOff(typeof(BaseForConstraintCase<>))]
	public partial class ConstraintOpenInlineTests
	{
	}

	public class ConstraintTests
	{
		// ====================================================================
		// Pattern 3: Standalone class — BaseForConstraintCase<string>
		// ====================================================================

		#region Standalone Class: Constraint propagation

		[Fact]
		public void Constraint_Standalone_CompilesAndHasObject()
		{
			var stub = new ConstraintCaseStandaloneKnockOff();
			BaseForConstraintCase<string> obj = stub.Object;

			Assert.NotNull(obj);
		}

		[Fact]
		public void Constraint_Standalone_As_CanCallWithConstrainedType()
		{
			var stub = new ConstraintCaseStandaloneKnockOff();
			BaseForConstraintCase<string> obj = stub.Object;

			// As<TTarget>() where TTarget : class — call with a reference type
			var result = obj.As<string>();

			// Unconfigured abstract method should return default (null)
			Assert.Null(result);
		}

		[Fact]
		public void Constraint_Standalone_As_ConfigureReturnValue()
		{
			var stub = new ConstraintCaseStandaloneKnockOff();
			BaseForConstraintCase<string> obj = stub.Object;

			// Configure As<string>() to return a specific value
			// We need a concrete implementation to return, so we use a callback
			BaseForConstraintCase<string>? returnValue = null;
			stub.As.Of<string>().Call(() => returnValue!);

			var result = obj.As<string>();

			Assert.Same(returnValue, result);
		}

		[Fact]
		public void Constraint_Standalone_As_VerifyTracking()
		{
			var stub = new ConstraintCaseStandaloneKnockOff();
			BaseForConstraintCase<string> obj = stub.Object;

			obj.As<string>();
			obj.As<object>();

			stub.As.Of<string>().Verify(Called.Once);
			stub.As.Of<object>().Verify(Called.Once);
			stub.As.Verify(Called.Exactly(2));
		}

		#endregion

		// ====================================================================
		// Pattern 4: Generic Standalone Class — BaseForConstraintCase<T>
		// ====================================================================

		#region Generic Standalone Class: Constraint propagation

		[Fact]
		public void Constraint_GenericStandaloneClass_CompilesAndHasObject()
		{
			var stub = new ConstraintCaseOpenStandaloneKnockOff<string>();
			BaseForConstraintCase<string> obj = stub.Object;

			Assert.NotNull(obj);
		}

		[Fact]
		public void Constraint_GenericStandaloneClass_As_CanCallWithConstrainedType()
		{
			var stub = new ConstraintCaseOpenStandaloneKnockOff<string>();
			BaseForConstraintCase<string> obj = stub.Object;

			// As<TTarget>() where TTarget : class — call with a reference type
			var result = obj.As<string>();

			// Unconfigured abstract method should return default (null)
			Assert.Null(result);
		}

		[Fact]
		public void Constraint_GenericStandaloneClass_As_ConfigureReturnValue()
		{
			var stub = new ConstraintCaseOpenStandaloneKnockOff<string>();
			BaseForConstraintCase<string> obj = stub.Object;

			BaseForConstraintCase<string>? returnValue = null;
			stub.As.Of<string>().Call(() => returnValue!);

			var result = obj.As<string>();

			Assert.Same(returnValue, result);
		}

		[Fact]
		public void Constraint_GenericStandaloneClass_As_VerifyTracking()
		{
			var stub = new ConstraintCaseOpenStandaloneKnockOff<string>();
			BaseForConstraintCase<string> obj = stub.Object;

			obj.As<string>();
			obj.As<object>();

			stub.As.Of<string>().Verify(Called.Once);
			stub.As.Of<object>().Verify(Called.Once);
			stub.As.Verify(Called.Exactly(2));
		}

		#endregion

		// ====================================================================
		// Pattern 6: Inline class — BaseForConstraintCase<string>
		// ====================================================================

		#region Inline Class: Constraint propagation

		[Fact]
		public void Constraint_Inline_CompilesAndHasObject()
		{
			var stub = new ConstraintInlineTests.Stubs.BaseForConstraintCase();
			BaseForConstraintCase<string> obj = stub.Object;

			Assert.NotNull(obj);
		}

		[Fact]
		public void Constraint_Inline_As_CanCallWithConstrainedType()
		{
			var stub = new ConstraintInlineTests.Stubs.BaseForConstraintCase();
			BaseForConstraintCase<string> obj = stub.Object;

			var result = obj.As<string>();

			Assert.Null(result);
		}

		[Fact]
		public void Constraint_Inline_As_ConfigureReturnValue()
		{
			var stub = new ConstraintInlineTests.Stubs.BaseForConstraintCase();
			BaseForConstraintCase<string> obj = stub.Object;

			BaseForConstraintCase<string>? returnValue = null;
			stub.As.Of<string>().Call(() => returnValue!);

			var result = obj.As<string>();

			Assert.Same(returnValue, result);
		}

		[Fact]
		public void Constraint_Inline_As_VerifyTracking()
		{
			var stub = new ConstraintInlineTests.Stubs.BaseForConstraintCase();
			BaseForConstraintCase<string> obj = stub.Object;

			obj.As<string>();
			obj.As<object>();

			stub.As.Of<string>().Verify(Called.Once);
			stub.As.Of<object>().Verify(Called.Once);
			stub.As.Verify(Called.Exactly(2));
		}

		#endregion

		// ====================================================================
		// Pattern 9: Open Generic Class — BaseForConstraintCase<>
		// ====================================================================

		#region Open Generic Class: Constraint propagation

		[Fact]
		public void Constraint_OpenGenericClass_CompilesAndHasObject()
		{
			var stub = new ConstraintOpenInlineTests.Stubs.BaseForConstraintCase<string>();
			BaseForConstraintCase<string> obj = stub.Object;

			Assert.NotNull(obj);
		}

		[Fact]
		public void Constraint_OpenGenericClass_As_CanCallWithConstrainedType()
		{
			var stub = new ConstraintOpenInlineTests.Stubs.BaseForConstraintCase<string>();
			BaseForConstraintCase<string> obj = stub.Object;

			// As<TTarget>() where TTarget : class — call with a reference type
			var result = obj.As<string>();

			// Unconfigured abstract method should return default (null)
			Assert.Null(result);
		}

		[Fact]
		public void Constraint_OpenGenericClass_As_ConfigureReturnValue()
		{
			var stub = new ConstraintOpenInlineTests.Stubs.BaseForConstraintCase<string>();
			BaseForConstraintCase<string> obj = stub.Object;

			BaseForConstraintCase<string>? returnValue = null;
			stub.As.Of<string>().Call(() => returnValue!);

			var result = obj.As<string>();

			Assert.Same(returnValue, result);
		}

		[Fact]
		public void Constraint_OpenGenericClass_As_VerifyTracking()
		{
			var stub = new ConstraintOpenInlineTests.Stubs.BaseForConstraintCase<string>();
			BaseForConstraintCase<string> obj = stub.Object;

			obj.As<string>();
			obj.As<object>();

			stub.As.Of<string>().Verify(Called.Once);
			stub.As.Of<object>().Verify(Called.Once);
			stub.As.Verify(Called.Exactly(2));
		}

		#endregion
	}
}
