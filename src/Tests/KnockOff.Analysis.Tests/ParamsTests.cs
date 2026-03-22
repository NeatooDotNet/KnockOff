// ============================================================================
// ParamsTests: Edge case tests for params parameters
// Inspired by Rocks.Analysis.IntegrationTests.ParamsTests
//
// Tests three categories of params edge cases:
// 1. params string[] on methods — params array as last parameter
// 2. params string[] on indexers — params array as indexer key component
// 3. params ReadOnlySpan<string> on methods — C# 13 params with ref struct
//
// Each edge case is tested with both Standalone (Pattern 1) and Inline (Pattern 5).
// Separate interfaces per edge case so each compiles independently.
// ============================================================================

using KnockOff;

// ============================================================================
// TYPE DEFINITIONS
// ============================================================================

namespace KnockOff.Analysis.Tests.ParamsTestTypes
{
	// Edge Case 1 & 2: params string[] on methods and indexers
	public interface IHaveParamsArray
	{
		void Foo(int a, params string[] b);
		int this[int a, params string[] b] { get; }
	}

	// Edge Case 3: params ReadOnlySpan<string> on methods (ref struct)
	public interface IHaveParamsReadOnlySpan
	{
		void ParamsFoo(int a, params ReadOnlySpan<string> b);
	}

	// Pattern 1: Standalone stubs
	[KnockOff]
	public partial class ParamsArrayStandaloneKnockOff : IHaveParamsArray
	{
	}

	[KnockOff]
	public partial class ParamsReadOnlySpanStandaloneKnockOff : IHaveParamsReadOnlySpan
	{
	}
}

// ============================================================================
// INLINE PATTERN DECLARATIONS + TESTS
// ============================================================================

namespace KnockOff.Analysis.Tests
{
	using KnockOff.Analysis.Tests.ParamsTestTypes;

	// Pattern 5: Inline stubs
	[KnockOff<IHaveParamsArray>]
	[KnockOff<IHaveParamsReadOnlySpan>]
	public partial class ParamsInlineTests
	{
	}

	public class ParamsTests
	{
		// ====================================================================
		// Edge Case 1: params string[] on methods
		// ====================================================================

		#region Standalone: params array method

		[Fact]
		public void ParamsMethod_Standalone_CompilesAndImplementsInterface()
		{
			var knockOff = new ParamsArrayStandaloneKnockOff();
			IHaveParamsArray service = knockOff;

			Assert.NotNull(knockOff.Foo);
		}

		[Fact]
		public void ParamsMethod_Standalone_CanCallWithArgs()
		{
			var knockOff = new ParamsArrayStandaloneKnockOff();
			IHaveParamsArray service = knockOff;

			// Call with params specified
			service.Foo(1, "hello");

			knockOff.Foo.Verify(Called.Once);
		}

		[Fact]
		public void ParamsMethod_Standalone_CanCallWithNoParamsArgs()
		{
			var knockOff = new ParamsArrayStandaloneKnockOff();
			IHaveParamsArray service = knockOff;

			// Call with no params arguments — compiler passes empty array
			service.Foo(1);

			knockOff.Foo.Verify(Called.Once);
		}

		[Fact]
		public void ParamsMethod_Standalone_CanCallWithMultipleParamsArgs()
		{
			var knockOff = new ParamsArrayStandaloneKnockOff();
			IHaveParamsArray service = knockOff;

			// Call with multiple params arguments
			service.Foo(1, "a", "b", "c");

			knockOff.Foo.Verify(Called.Once);
		}

		[Fact]
		public void ParamsMethod_Standalone_CallbackReceivesArray()
		{
			var knockOff = new ParamsArrayStandaloneKnockOff();
			IHaveParamsArray service = knockOff;

			int capturedA = 0;
			string[]? capturedB = null;
			knockOff.Foo.Call((int a, string[] b) =>
			{
				capturedA = a;
				capturedB = b;
			});

			service.Foo(42, "x", "y");

			Assert.Equal(42, capturedA);
			Assert.NotNull(capturedB);
			Assert.Equal(2, capturedB!.Length);
			Assert.Equal("x", capturedB[0]);
			Assert.Equal("y", capturedB[1]);
		}

		[Fact]
		public void ParamsMethod_Standalone_CallbackReceivesEmptyArray()
		{
			var knockOff = new ParamsArrayStandaloneKnockOff();
			IHaveParamsArray service = knockOff;

			string[]? capturedB = null;
			knockOff.Foo.Call((int a, string[] b) =>
			{
				capturedB = b;
			});

			service.Foo(1);

			Assert.NotNull(capturedB);
			Assert.Empty(capturedB!);
		}

		[Fact]
		public void ParamsMethod_Standalone_VerifyMultipleCalls()
		{
			var knockOff = new ParamsArrayStandaloneKnockOff();
			IHaveParamsArray service = knockOff;

			service.Foo(1, "a");
			service.Foo(2);
			service.Foo(3, "b", "c", "d");

			knockOff.Foo.Verify(Called.Exactly(3));
		}

		#endregion

		#region Inline: params array method

		[Fact]
		public void ParamsMethod_Inline_CompilesAndCanCall()
		{
			var stub = new ParamsInlineTests.Stubs.IHaveParamsArray();
			IHaveParamsArray service = stub;

			service.Foo(1, "hello");

			stub.Foo.Verify(Called.Once);
		}

		[Fact]
		public void ParamsMethod_Inline_CanCallWithNoParamsArgs()
		{
			var stub = new ParamsInlineTests.Stubs.IHaveParamsArray();
			IHaveParamsArray service = stub;

			service.Foo(1);

			stub.Foo.Verify(Called.Once);
		}

		[Fact]
		public void ParamsMethod_Inline_CallbackReceivesArray()
		{
			var stub = new ParamsInlineTests.Stubs.IHaveParamsArray();
			IHaveParamsArray service = stub;

			int capturedA = 0;
			string[]? capturedB = null;
			stub.Foo.Call((int a, string[] b) =>
			{
				capturedA = a;
				capturedB = b;
			});

			service.Foo(10, "p", "q", "r");

			Assert.Equal(10, capturedA);
			Assert.NotNull(capturedB);
			Assert.Equal(3, capturedB!.Length);
			Assert.Equal("p", capturedB[0]);
		}

		#endregion

		// ====================================================================
		// Edge Case 2: params string[] on indexers
		// ====================================================================

		#region Standalone: params array indexer

		[Fact]
		public void ParamsIndexer_Standalone_CompilesAndHasInterceptor()
		{
			var knockOff = new ParamsArrayStandaloneKnockOff();
			IHaveParamsArray service = knockOff;

			Assert.NotNull(knockOff.Indexer);
		}

		[Fact]
		public void ParamsIndexer_Standalone_GetCallback()
		{
			var knockOff = new ParamsArrayStandaloneKnockOff();
			IHaveParamsArray service = knockOff;

			// Get callback receives the key tuple
			knockOff.Indexer.Get(key => key.a * 100);

			var result = service[1, "b"];

			Assert.Equal(100, result);
		}

		[Fact]
		public void ParamsIndexer_Standalone_GetCallbackWithMultipleParams()
		{
			var knockOff = new ParamsArrayStandaloneKnockOff();
			IHaveParamsArray service = knockOff;

			knockOff.Indexer.Get(key => key.a + key.b.Length);

			var result = service[5, "a", "b", "c"];

			Assert.Equal(8, result);
		}

		[Fact]
		public void ParamsIndexer_Standalone_NoParamsArgs()
		{
			var knockOff = new ParamsArrayStandaloneKnockOff();
			IHaveParamsArray service = knockOff;

			knockOff.Indexer.Get(key => key.a * 10);

			// No params args -- empty array
			var result = service[7];

			Assert.Equal(70, result);
		}

		[Fact]
		public void ParamsIndexer_Standalone_VerifyTracking()
		{
			var knockOff = new ParamsArrayStandaloneKnockOff();
			IHaveParamsArray service = knockOff;

			knockOff.Indexer.Get(key => 0);

			_ = service[1, "x"];
			_ = service[2];

			knockOff.Indexer.VerifyGet(Called.Exactly(2));
		}

		#endregion

		#region Inline: params array indexer

		[Fact]
		public void ParamsIndexer_Inline_GetCallback()
		{
			var stub = new ParamsInlineTests.Stubs.IHaveParamsArray();
			IHaveParamsArray service = stub;

			stub.Indexer.Get(key => key.a * 100);

			var result = service[2, "x"];

			Assert.Equal(200, result);
		}

		[Fact]
		public void ParamsIndexer_Inline_GetCallbackWithMultipleParams()
		{
			var stub = new ParamsInlineTests.Stubs.IHaveParamsArray();
			IHaveParamsArray service = stub;

			stub.Indexer.Get(key => key.a + key.b.Length);

			var result = service[10, "a", "b"];

			Assert.Equal(12, result);
		}

		[Fact]
		public void ParamsIndexer_Inline_NoParamsArgs()
		{
			var stub = new ParamsInlineTests.Stubs.IHaveParamsArray();
			IHaveParamsArray service = stub;

			stub.Indexer.Get(key => key.a * 10);

			var result = service[3];

			Assert.Equal(30, result);
		}

		[Fact]
		public void ParamsIndexer_Inline_VerifyTracking()
		{
			var stub = new ParamsInlineTests.Stubs.IHaveParamsArray();
			IHaveParamsArray service = stub;

			stub.Indexer.Get(key => 0);

			_ = service[1, "x"];
			_ = service[2, "y", "z"];
			_ = service[3];

			stub.Indexer.VerifyGet(Called.Exactly(3));
		}

		#endregion

		// ====================================================================
		// Edge Case 3: params ReadOnlySpan<string> on methods
		// ReadOnlySpan is a ref struct — cannot be stored, boxed, or used as
		// a generic type argument. This requires special generator handling.
		// ====================================================================

		#region Standalone: params ReadOnlySpan method

		[Fact]
		public void ParamsReadOnlySpan_Standalone_CanCallMethod()
		{
			var knockOff = new ParamsReadOnlySpanStandaloneKnockOff();
			IHaveParamsReadOnlySpan service = knockOff;

			// Call ParamsFoo which takes params ReadOnlySpan<string>
			service.ParamsFoo(1, "b", "c");

			knockOff.ParamsFoo.Verify(Called.Once);
		}

		[Fact]
		public void ParamsReadOnlySpan_Standalone_CanCallWithNoArgs()
		{
			var knockOff = new ParamsReadOnlySpanStandaloneKnockOff();
			IHaveParamsReadOnlySpan service = knockOff;

			service.ParamsFoo(1);

			knockOff.ParamsFoo.Verify(Called.Once);
		}

		[Fact]
		public void ParamsReadOnlySpan_Standalone_VerifyMultipleCalls()
		{
			var knockOff = new ParamsReadOnlySpanStandaloneKnockOff();
			IHaveParamsReadOnlySpan service = knockOff;

			service.ParamsFoo(1, "a");
			service.ParamsFoo(2, "b", "c");
			service.ParamsFoo(3);

			knockOff.ParamsFoo.Verify(Called.Exactly(3));
		}

		#endregion

		#region Inline: params ReadOnlySpan method

		[Fact]
		public void ParamsReadOnlySpan_Inline_CanCallMethod()
		{
			var stub = new ParamsInlineTests.Stubs.IHaveParamsReadOnlySpan();
			IHaveParamsReadOnlySpan service = stub;

			service.ParamsFoo(1, "b", "c");

			stub.ParamsFoo.Verify(Called.Once);
		}

		[Fact]
		public void ParamsReadOnlySpan_Inline_CanCallWithNoArgs()
		{
			var stub = new ParamsInlineTests.Stubs.IHaveParamsReadOnlySpan();
			IHaveParamsReadOnlySpan service = stub;

			service.ParamsFoo(1);

			stub.ParamsFoo.Verify(Called.Once);
		}

		#endregion
	}
}
