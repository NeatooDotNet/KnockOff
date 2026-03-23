// ============================================================================
// RefStructTests: Edge case tests for Span<T> (ref struct) in various positions
// Inspired by Rocks.Analysis.IntegrationTests.RefStructTests
//
// Tests three categories of ref struct edge cases:
// 1. Span<T> as method parameters (void methods, non-generic and generic)
// 2. Span<T> as method return types (no-param and with Span param)
// 3. Span<T> as both method parameter and return type
//
// Span<T> is a ref struct — it cannot be boxed, stored in tuples, or used as
// generic type arguments. KnockOff generates degraded interceptors for methods
// with ref struct parameters or returns (no args tracking, no When chains,
// no Return(value), no params T[] sequences).
// Call(callback) and Verify() still work.
//
// NOT YET SUPPORTED (separate work items):
// - Span<T> as property types (requires custom delegate generation in PropertyInterceptorRenderer)
// - scoped modifier on Span parameters (scoped ref structs can't be passed to delegates)
//
// Each edge case is tested with:
// - Pattern 1 (Standalone Interface) and Pattern 5 (Inline Interface)
// ============================================================================

using KnockOff;

// ============================================================================
// TYPE DEFINITIONS
// ============================================================================

namespace KnockOff.Analysis.Tests.RefStructTestTypes
{
	// Edge Case 1: Span<int> as void method params (non-generic and generic)
	public interface IHaveSpan
	{
		void Foo(Span<int> values);
		void Bar<T>(Span<T> values);
	}

	// Edge Case 2: Span<T> as method return type (no Span params)
	public interface IReturnSpan
	{
		Span<int> GetRandomData();
	}

	// Edge Case 3: Span<T> as both parameter and return
	public interface IHaveInAndOutSpan
	{
		Span<int> Foo(Span<int> values);
	}

	// Pattern 1: Standalone stubs
	[KnockOff]
	public partial class SpanParamStandaloneKnockOff : IHaveSpan
	{
	}

	[KnockOff]
	public partial class SpanReturnStandaloneKnockOff : IReturnSpan
	{
	}

	[KnockOff]
	public partial class SpanInAndOutStandaloneKnockOff : IHaveInAndOutSpan
	{
	}
}

// ============================================================================
// INLINE PATTERN DECLARATIONS + TESTS
// ============================================================================

namespace KnockOff.Analysis.Tests
{
	using KnockOff.Analysis.Tests.RefStructTestTypes;

	// Pattern 5: Inline stubs
	[KnockOff<IHaveSpan>]
	[KnockOff<IReturnSpan>]
	[KnockOff<IHaveInAndOutSpan>]
	public partial class RefStructInlineTests
	{
	}

	public class RefStructTests
	{
		// ====================================================================
		// Edge Case 1: Span<int> as void method parameters
		// ====================================================================

		#region Standalone: Span<int> param void method

		[Fact]
		public void SpanParam_Standalone_CanCallMethod()
		{
			var knockOff = new SpanParamStandaloneKnockOff();
			IHaveSpan service = knockOff;

			Span<int> buffer = stackalloc int[] { 1, 2, 3 };
			service.Foo(buffer);

			knockOff.Foo.Verify(Called.Once);
		}

		[Fact]
		public void SpanParam_Standalone_VerifyMultipleCalls()
		{
			var knockOff = new SpanParamStandaloneKnockOff();
			IHaveSpan service = knockOff;

			service.Foo(stackalloc int[] { 1 });
			service.Foo(stackalloc int[] { 2, 3 });
			service.Foo(default);

			knockOff.Foo.Verify(Called.Exactly(3));
		}

		[Fact]
		public void SpanParam_Standalone_CallbackReceivesSpan()
		{
			var knockOff = new SpanParamStandaloneKnockOff();
			IHaveSpan service = knockOff;

			int capturedLength = 0;
			int capturedFirst = 0;
			knockOff.Foo.Call((Span<int> values) =>
			{
				capturedLength = values.Length;
				if (values.Length > 0)
					capturedFirst = values[0];
			});

			service.Foo(stackalloc int[] { 42, 99 });

			Assert.Equal(2, capturedLength);
			Assert.Equal(42, capturedFirst);
		}

		[Fact]
		public void SpanParam_Standalone_EmptySpan()
		{
			var knockOff = new SpanParamStandaloneKnockOff();
			IHaveSpan service = knockOff;

			int capturedLength = -1;
			knockOff.Foo.Call((Span<int> values) =>
			{
				capturedLength = values.Length;
			});

			service.Foo(default);

			Assert.Equal(0, capturedLength);
		}

		#endregion

		#region Standalone: generic Span<T> param method

		[Fact]
		public void GenericSpanParam_Standalone_CanCallMethod()
		{
			var knockOff = new SpanParamStandaloneKnockOff();
			IHaveSpan service = knockOff;

			Span<int> buffer = stackalloc int[] { 1, 2, 3 };
			service.Bar(buffer);

			knockOff.Bar.Of<int>().Verify(Called.Once);
		}

		[Fact]
		public void GenericSpanParam_Standalone_VerifyMultipleCalls()
		{
			var knockOff = new SpanParamStandaloneKnockOff();
			IHaveSpan service = knockOff;

			service.Bar<int>(stackalloc int[] { 1, 2 });
			service.Bar<int>(stackalloc int[] { 3 });

			knockOff.Bar.Of<int>().Verify(Called.Exactly(2));
		}

		#endregion

		#region Inline: Span<int> param void method

		[Fact]
		public void SpanParam_Inline_CanCallMethod()
		{
			var stub = new RefStructInlineTests.Stubs.IHaveSpan();
			IHaveSpan service = stub;

			Span<int> buffer = stackalloc int[] { 10, 20 };
			service.Foo(buffer);

			stub.Foo.Verify(Called.Once);
		}

		[Fact]
		public void SpanParam_Inline_CallbackReceivesSpan()
		{
			var stub = new RefStructInlineTests.Stubs.IHaveSpan();
			IHaveSpan service = stub;

			int capturedLength = 0;
			stub.Foo.Call((Span<int> values) =>
			{
				capturedLength = values.Length;
			});

			service.Foo(stackalloc int[] { 5, 6, 7 });

			Assert.Equal(3, capturedLength);
		}

		#endregion

		#region Inline: generic Span<T> param method

		[Fact]
		public void GenericSpanParam_Inline_CanCallMethod()
		{
			var stub = new RefStructInlineTests.Stubs.IHaveSpan();
			IHaveSpan service = stub;

			service.Bar<int>(stackalloc int[] { 100 });

			stub.Bar.Of<int>().Verify(Called.Once);
		}

		#endregion

		// ====================================================================
		// Edge Case 2: Span<T> as method return type (no Span params)
		// ====================================================================

		#region Standalone: Span<int> return method

		[Fact]
		public void SpanReturn_Standalone_UnconfiguredReturnsEmptySpan()
		{
			var knockOff = new SpanReturnStandaloneKnockOff();
			IReturnSpan service = knockOff;

			// No configuration -- should return default Span (empty)
			var result = service.GetRandomData();

			Assert.True(result.IsEmpty);
		}

		[Fact]
		public void SpanReturn_Standalone_CallbackReturnsSpan()
		{
			var knockOff = new SpanReturnStandaloneKnockOff();
			IReturnSpan service = knockOff;

			var backing = new int[] { 1, 2, 3 };
			knockOff.GetRandomData.Call(() => backing.AsSpan());

			var result = service.GetRandomData();

			Assert.Equal(3, result.Length);
			Assert.Equal(1, result[0]);
			Assert.Equal(2, result[1]);
			Assert.Equal(3, result[2]);
		}

		[Fact]
		public void SpanReturn_Standalone_VerifyCallTracking()
		{
			var knockOff = new SpanReturnStandaloneKnockOff();
			IReturnSpan service = knockOff;

			_ = service.GetRandomData();
			_ = service.GetRandomData();

			knockOff.GetRandomData.Verify(Called.Exactly(2));
		}

		#endregion

		#region Inline: Span<int> return method

		[Fact]
		public void SpanReturn_Inline_UnconfiguredReturnsEmptySpan()
		{
			var stub = new RefStructInlineTests.Stubs.IReturnSpan();
			IReturnSpan service = stub;

			var result = service.GetRandomData();

			Assert.True(result.IsEmpty);
		}

		[Fact]
		public void SpanReturn_Inline_CallbackReturnsSpan()
		{
			var stub = new RefStructInlineTests.Stubs.IReturnSpan();
			IReturnSpan service = stub;

			var backing = new int[] { 10, 20 };
			stub.GetRandomData.Call(() => backing.AsSpan());

			var result = service.GetRandomData();

			Assert.Equal(2, result.Length);
			Assert.Equal(10, result[0]);
		}

		[Fact]
		public void SpanReturn_Inline_VerifyCallTracking()
		{
			var stub = new RefStructInlineTests.Stubs.IReturnSpan();
			IReturnSpan service = stub;

			_ = service.GetRandomData();
			_ = service.GetRandomData();

			stub.GetRandomData.Verify(Called.Exactly(2));
		}

		#endregion

		// ====================================================================
		// Edge Case 3: Span<T> as both parameter and return
		// ====================================================================

		#region Standalone: Span<int> param + return

		[Fact]
		public void SpanInAndOut_Standalone_CallbackTransformsSpan()
		{
			var knockOff = new SpanInAndOutStandaloneKnockOff();
			IHaveInAndOutSpan service = knockOff;

			var output = new int[] { 10, 20 };
			knockOff.Foo.Call((Span<int> values) => output.AsSpan());

			var result = service.Foo(stackalloc int[] { 1, 2, 3 });

			Assert.Equal(2, result.Length);
			Assert.Equal(10, result[0]);
			Assert.Equal(20, result[1]);
		}

		[Fact]
		public void SpanInAndOut_Standalone_UnconfiguredReturnsEmpty()
		{
			var knockOff = new SpanInAndOutStandaloneKnockOff();
			IHaveInAndOutSpan service = knockOff;

			var result = service.Foo(default);

			Assert.True(result.IsEmpty);
		}

		[Fact]
		public void SpanInAndOut_Standalone_VerifyTracking()
		{
			var knockOff = new SpanInAndOutStandaloneKnockOff();
			IHaveInAndOutSpan service = knockOff;

			_ = service.Foo(default);
			_ = service.Foo(stackalloc int[] { 1 });

			knockOff.Foo.Verify(Called.Exactly(2));
		}

		#endregion

		#region Inline: Span<int> param + return

		[Fact]
		public void SpanInAndOut_Inline_CallbackTransformsSpan()
		{
			var stub = new RefStructInlineTests.Stubs.IHaveInAndOutSpan();
			IHaveInAndOutSpan service = stub;

			var output = new int[] { 5, 6 };
			stub.Foo.Call((Span<int> values) => output.AsSpan());

			var result = service.Foo(default);

			Assert.Equal(2, result.Length);
			Assert.Equal(5, result[0]);
		}

		[Fact]
		public void SpanInAndOut_Inline_UnconfiguredReturnsEmpty()
		{
			var stub = new RefStructInlineTests.Stubs.IHaveInAndOutSpan();
			IHaveInAndOutSpan service = stub;

			var result = service.Foo(default);

			Assert.True(result.IsEmpty);
		}

		#endregion
	}
}
