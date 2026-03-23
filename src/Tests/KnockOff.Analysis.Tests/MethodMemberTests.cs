// ============================================================================
// MethodMemberTests: Edge case tests inspired by Rocks.Analysis.IntegrationTests
//
// Tests three categories of edge cases:
// 1. 20+ parameters — pushes beyond existing 10-param tests
// 2. 'in' parameters — readonly ref parameter modifier
// 3. Generic methods with ref/out parameters — Of<T1,T2>() with ref/out
//
// Each edge case is tested with both Standalone (Pattern 1) and Inline (Pattern 5).
// ============================================================================

using KnockOff;

// ============================================================================
// TYPE DEFINITIONS
// ============================================================================

namespace KnockOff.Analysis.Tests.MethodMemberTestTypes
{
	// ========================================================================
	// Edge Case 1: 20+ parameters
	// ========================================================================

	public interface IHaveLotsOfParameters
	{
		void CallThis(
			int i0, int i1, int i2, int i3, int i4,
			int i5, int i6, int i7, int i8, int i9,
			int i10, int i11, int i12, int i13, int i14,
			int i15, int i16, int i17, int i18, int i19);
	}

	[KnockOff]
	public partial class LotsOfParamsStandaloneKnockOff : IHaveLotsOfParameters
	{
	}

	// ========================================================================
	// Edge Case 2: in parameters
	// 'in' is a readonly ref — the stub should handle it like a normal param.
	// Tests both method and indexer with 'in' parameters.
	// ========================================================================

	public interface IHaveIn
	{
		void InArgument(in int a);
		int this[in int a] { get; }
	}

	[KnockOff]
	public partial class InStandaloneKnockOff : IHaveIn
	{
	}

	// ========================================================================
	// Edge Case 3: ref/out with generic type parameters
	// KNOWN BUG (Gap 27/28): Generic methods with ref/out params fail in
	// inline pattern (CS1615). Standalone pattern works via Of<T1, T2>().
	// ========================================================================

	public interface IHaveRefAndOutGenerics
	{
		void RefArgumentsWithGenerics<T1, T2>(T1 a, ref T2 b);
		void OutArgumentsWithGenerics<T1, T2>(T1 a, out T2 b);
	}

	[KnockOff]
	public partial class RefOutGenericsStandaloneKnockOff : IHaveRefAndOutGenerics
	{
	}
}

// ============================================================================
// INLINE PATTERN DECLARATIONS + TESTS
// ============================================================================

namespace KnockOff.Analysis.Tests
{
	using KnockOff.Analysis.Tests.MethodMemberTestTypes;

	// Inline stubs for all edge cases
	[KnockOff<IHaveLotsOfParameters>]
	[KnockOff<IHaveIn>]
	[KnockOff<IHaveRefAndOutGenerics>]
	public partial class MethodMemberInlineTests
	{
	}

	public class MethodMemberTests
	{
		// ====================================================================
		// Edge Case 1: 20+ parameters
		// ====================================================================

		#region Standalone: 20 parameters

		[Fact]
		public void LotsOfParams_Standalone_CanCallMethod()
		{
			var knockOff = new LotsOfParamsStandaloneKnockOff();
			IHaveLotsOfParameters service = knockOff;

			// Call with no configuration — loose mode, should succeed
			service.CallThis(
				0, 1, 2, 3, 4, 5, 6, 7, 8, 9,
				10, 11, 12, 13, 14, 15, 16, 17, 18, 19);

			knockOff.CallThis.Verify(Called.Once);
		}

		[Fact]
		public void LotsOfParams_Standalone_CallbackWorks()
		{
			var knockOff = new LotsOfParamsStandaloneKnockOff();
			IHaveLotsOfParameters service = knockOff;

			int capturedSum = 0;
			knockOff.CallThis.Call((
				int i0, int i1, int i2, int i3, int i4,
				int i5, int i6, int i7, int i8, int i9,
				int i10, int i11, int i12, int i13, int i14,
				int i15, int i16, int i17, int i18, int i19) =>
			{
				capturedSum = i0 + i1 + i2 + i3 + i4 + i5 + i6 + i7 + i8 + i9
					+ i10 + i11 + i12 + i13 + i14 + i15 + i16 + i17 + i18 + i19;
			});

			service.CallThis(
				0, 1, 2, 3, 4, 5, 6, 7, 8, 9,
				10, 11, 12, 13, 14, 15, 16, 17, 18, 19);

			// Sum of 0..19 = 190
			Assert.Equal(190, capturedSum);
		}

		[Fact]
		public void LotsOfParams_Standalone_VerifyMultipleCalls()
		{
			var knockOff = new LotsOfParamsStandaloneKnockOff();
			IHaveLotsOfParameters service = knockOff;

			var tracking = knockOff.CallThis.Call((
				int i0, int i1, int i2, int i3, int i4,
				int i5, int i6, int i7, int i8, int i9,
				int i10, int i11, int i12, int i13, int i14,
				int i15, int i16, int i17, int i18, int i19) => { });

			service.CallThis(
				0, 1, 2, 3, 4, 5, 6, 7, 8, 9,
				10, 11, 12, 13, 14, 15, 16, 17, 18, 19);
			service.CallThis(
				0, 1, 2, 3, 4, 5, 6, 7, 8, 9,
				10, 11, 12, 13, 14, 15, 16, 17, 18, 19);

			tracking.Verify(Called.Exactly(2));
		}

		#endregion

		#region Inline: 20 parameters

		[Fact]
		public void LotsOfParams_Inline_CompilesAndCanCall()
		{
			var stub = new MethodMemberInlineTests.Stubs.IHaveLotsOfParameters();
			IHaveLotsOfParameters service = stub;

			service.CallThis(
				0, 1, 2, 3, 4, 5, 6, 7, 8, 9,
				10, 11, 12, 13, 14, 15, 16, 17, 18, 19);

			stub.CallThis.Verify(Called.Once);
		}

		[Fact]
		public void LotsOfParams_Inline_CallbackWorks()
		{
			var stub = new MethodMemberInlineTests.Stubs.IHaveLotsOfParameters();
			IHaveLotsOfParameters service = stub;

			int capturedSum = 0;
			stub.CallThis.Call((
				int i0, int i1, int i2, int i3, int i4,
				int i5, int i6, int i7, int i8, int i9,
				int i10, int i11, int i12, int i13, int i14,
				int i15, int i16, int i17, int i18, int i19) =>
			{
				capturedSum = i0 + i1 + i2 + i3 + i4 + i5 + i6 + i7 + i8 + i9
					+ i10 + i11 + i12 + i13 + i14 + i15 + i16 + i17 + i18 + i19;
			});

			service.CallThis(
				0, 1, 2, 3, 4, 5, 6, 7, 8, 9,
				10, 11, 12, 13, 14, 15, 16, 17, 18, 19);

			Assert.Equal(190, capturedSum);
		}

		#endregion

		// ====================================================================
		// Edge Case 2: in parameters
		// ====================================================================

		#region Standalone: in parameters — method

		[Fact]
		public void InParam_Standalone_CanCallMethod()
		{
			var knockOff = new InStandaloneKnockOff();
			IHaveIn service = knockOff;

			// 'in' is readonly ref — should behave like a normal value parameter
			service.InArgument(42);

			knockOff.InArgument.Verify(Called.Once);
		}

		[Fact]
		public void InParam_Standalone_CallbackWorks()
		{
			var knockOff = new InStandaloneKnockOff();
			IHaveIn service = knockOff;

			int capturedValue = 0;
			knockOff.InArgument.Call((in int a) =>
			{
				capturedValue = a;
			});

			service.InArgument(42);

			Assert.Equal(42, capturedValue);
		}

		[Fact]
		public void InParam_Standalone_LastArgTracksValue()
		{
			var knockOff = new InStandaloneKnockOff();
			IHaveIn service = knockOff;

			var tracking = knockOff.InArgument.Call((in int a) => { });

			service.InArgument(99);

			Assert.Equal(99, tracking.LastArg);
		}

		#endregion

		#region Standalone: in parameters — indexer

		[Fact]
		public void InIndexer_Standalone_CanCallIndexer()
		{
			var knockOff = new InStandaloneKnockOff();
			IHaveIn service = knockOff;

			// 'in' indexer should work — the parameter should be treated like a normal value
			var result = service[42];

			knockOff.Indexer.VerifyGet(Called.Once);
		}

		[Fact]
		public void InIndexer_Standalone_GetCallbackWorks()
		{
			var knockOff = new InStandaloneKnockOff();
			IHaveIn service = knockOff;

			knockOff.Indexer.Get((int key) => key * 2);

			var result = service[10];

			Assert.Equal(20, result);
		}

		#endregion

		#region Inline: in parameters — method

		[Fact]
		public void InParam_Inline_CompilesAndCanCall()
		{
			var stub = new MethodMemberInlineTests.Stubs.IHaveIn();
			IHaveIn service = stub;

			service.InArgument(42);

			stub.InArgument.Verify(Called.Once);
		}

		[Fact]
		public void InParam_Inline_CallbackWorks()
		{
			var stub = new MethodMemberInlineTests.Stubs.IHaveIn();
			IHaveIn service = stub;

			int capturedValue = 0;
			stub.InArgument.Call((in int a) =>
			{
				capturedValue = a;
			});

			service.InArgument(55);

			Assert.Equal(55, capturedValue);
		}

		#endregion

		#region Inline: in parameters — indexer

		[Fact]
		public void InIndexer_Inline_CompilesAndCanCall()
		{
			var stub = new MethodMemberInlineTests.Stubs.IHaveIn();
			IHaveIn service = stub;

			var result = service[42];

			stub.Indexer.VerifyGet(Called.Once);
		}

		[Fact]
		public void InIndexer_Inline_GetCallbackWorks()
		{
			var stub = new MethodMemberInlineTests.Stubs.IHaveIn();
			IHaveIn service = stub;

			stub.Indexer.Get((int key) => key + 100);

			var result = service[10];

			Assert.Equal(110, result);
		}

		#endregion

		// ====================================================================
		// Edge Case 3: ref/out with generic type parameters
		// ====================================================================

		#region Standalone: generic ref/out

		[Fact]
		public void GenericOut_Standalone_CallbackSetsOutParam()
		{
			var knockOff = new RefOutGenericsStandaloneKnockOff();
			IHaveRefAndOutGenerics service = knockOff;

			// Generic methods with out params use Of<T1, T2>() + Call(delegate)
			knockOff.OutArgumentsWithGenerics.Of<string, int>().Call((string a, out int b) =>
			{
				b = a.Length;
			});

			service.OutArgumentsWithGenerics("hello", out int result);

			Assert.Equal(5, result);
		}

		[Fact]
		public void GenericRef_Standalone_CallbackModifiesRefParam()
		{
			var knockOff = new RefOutGenericsStandaloneKnockOff();
			IHaveRefAndOutGenerics service = knockOff;

			// Generic methods with ref params use Of<T1, T2>() + Call(delegate)
			knockOff.RefArgumentsWithGenerics.Of<string, int>().Call((string a, ref int b) =>
			{
				b = b + a.Length;
			});

			int value = 10;
			service.RefArgumentsWithGenerics("hello", ref value);

			Assert.Equal(15, value);
		}

		[Fact]
		public void GenericOut_Standalone_VerifyCalls()
		{
			var knockOff = new RefOutGenericsStandaloneKnockOff();
			IHaveRefAndOutGenerics service = knockOff;

			knockOff.OutArgumentsWithGenerics.Of<int, string>().Call((int a, out string b) =>
			{
				b = a.ToString();
			});

			service.OutArgumentsWithGenerics(42, out string _);
			service.OutArgumentsWithGenerics(99, out string _);

			knockOff.OutArgumentsWithGenerics.Of<int, string>().Verify(Called.Exactly(2));
		}

		[Fact]
		public void GenericOut_Standalone_Unconfigured_OutParamGetsDefault()
		{
			var knockOff = new RefOutGenericsStandaloneKnockOff();
			IHaveRefAndOutGenerics service = knockOff;

			// No configuration — out param should get default
			service.OutArgumentsWithGenerics("hello", out int result);

			Assert.Equal(0, result); // default(int)
		}

		[Fact]
		public void GenericRef_Standalone_Unconfigured_RefParamUnchanged()
		{
			var knockOff = new RefOutGenericsStandaloneKnockOff();
			IHaveRefAndOutGenerics service = knockOff;

			// No configuration — ref param should be unchanged
			int value = 42;
			service.RefArgumentsWithGenerics("hello", ref value);

			Assert.Equal(42, value);
		}

		#endregion

		#region Inline: generic ref/out

		[Fact]
		public void GenericOut_Inline_CallbackSetsOutParam()
		{
			var stub = new MethodMemberInlineTests.Stubs.IHaveRefAndOutGenerics();
			IHaveRefAndOutGenerics service = stub;

			stub.OutArgumentsWithGenerics.Of<string, int>().Call((string a, out int b) =>
			{
				b = a.Length;
			});

			service.OutArgumentsWithGenerics("test", out int result);

			Assert.Equal(4, result);
		}

		[Fact]
		public void GenericRef_Inline_CallbackModifiesRefParam()
		{
			var stub = new MethodMemberInlineTests.Stubs.IHaveRefAndOutGenerics();
			IHaveRefAndOutGenerics service = stub;

			stub.RefArgumentsWithGenerics.Of<string, int>().Call((string a, ref int b) =>
			{
				b = b * 2;
			});

			int value = 5;
			service.RefArgumentsWithGenerics("unused", ref value);

			Assert.Equal(10, value);
		}

		#endregion
	}
}
