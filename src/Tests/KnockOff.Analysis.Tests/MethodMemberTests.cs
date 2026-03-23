// ============================================================================
// MethodMemberTests: Edge case tests inspired by Rocks.Analysis.IntegrationTests
//
// Tests four categories of edge cases:
// 1. 20+ parameters — pushes beyond existing 10-param tests
// 2. 'in' parameters — readonly ref parameter modifier
// 3. Generic methods with ref/out parameters — Of<T1,T2>() with ref/out
// 4. Non-generic ref/out parameters — simple ref int, out int
//
// Edge cases 1-3 test Standalone (Pattern 1) and Inline (Pattern 5).
// Edge case 4 tests all four applicable patterns:
//   Pattern 1 (Standalone), Pattern 3 (Standalone Class),
//   Pattern 5 (Inline), Pattern 6 (Inline Class).
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

	// ========================================================================
	// Edge Case 4: Non-generic ref/out parameters
	// Simple ref int and out int — the non-generic version of Edge Case 3.
	// Tests all four applicable patterns (1, 3, 5, 6).
	// ========================================================================

	public interface IHaveRefAndOut
	{
		void RefArgument(ref int a);
		void OutArgument(out int a);
	}

	public abstract class HaveRefAndOutBase
	{
		public abstract void RefArgument(ref int a);
		public abstract void OutArgument(out int a);
	}

	// Pattern 1: Standalone interface stub
	[KnockOff]
	public partial class RefOutStandaloneKnockOff : IHaveRefAndOut
	{
	}

	// Pattern 3: Standalone class stub
	[KnockOffBase<HaveRefAndOutBase>]
	public partial class RefOutClassStandaloneKnockOff
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
	[KnockOff<IHaveRefAndOut>]        // Pattern 5: Inline interface stub
	[KnockOff<HaveRefAndOutBase>]     // Pattern 6: Inline class stub
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

		// ====================================================================
		// Edge Case 4: Non-generic ref/out parameters
		// Simple ref int and out int — the non-generic version of Edge Case 3.
		// Uses custom delegates generated by KnockOff (not Func<>/Action<>).
		// ====================================================================

		#region Pattern 1 (Standalone): ref parameter

		[Fact]
		public void Ref_Standalone_CallbackModifiesValue()
		{
			var knockOff = new RefOutStandaloneKnockOff();
			IHaveRefAndOut service = knockOff;

			knockOff.RefArgument.Call((ref int a) => a = 42);

			int value = 0;
			service.RefArgument(ref value);

			Assert.Equal(42, value);
		}

		[Fact]
		public void Ref_Standalone_Unconfigured_ValueUnchanged()
		{
			var knockOff = new RefOutStandaloneKnockOff();
			IHaveRefAndOut service = knockOff;

			// No configuration — ref param should be unchanged
			int value = 99;
			service.RefArgument(ref value);

			Assert.Equal(99, value);
		}

		[Fact]
		public void Ref_Standalone_VerifyCallTracking()
		{
			var knockOff = new RefOutStandaloneKnockOff();
			IHaveRefAndOut service = knockOff;

			knockOff.RefArgument.Call((ref int a) => { });

			int v1 = 1;
			service.RefArgument(ref v1);
			int v2 = 2;
			service.RefArgument(ref v2);

			knockOff.RefArgument.Verify(Called.Exactly(2));
		}

		#endregion

		#region Pattern 1 (Standalone): out parameter

		[Fact]
		public void Out_Standalone_CallbackSetsValue()
		{
			var knockOff = new RefOutStandaloneKnockOff();
			IHaveRefAndOut service = knockOff;

			knockOff.OutArgument.Call((out int a) => a = 42);

			service.OutArgument(out int result);

			Assert.Equal(42, result);
		}

		[Fact]
		public void Out_Standalone_Unconfigured_GetsDefault()
		{
			var knockOff = new RefOutStandaloneKnockOff();
			IHaveRefAndOut service = knockOff;

			// No configuration — out param gets default(int) = 0
			service.OutArgument(out int result);

			Assert.Equal(0, result);
		}

		[Fact]
		public void Out_Standalone_VerifyCallTracking()
		{
			var knockOff = new RefOutStandaloneKnockOff();
			IHaveRefAndOut service = knockOff;

			knockOff.OutArgument.Call((out int a) => a = 0);

			service.OutArgument(out _);
			service.OutArgument(out _);
			service.OutArgument(out _);

			knockOff.OutArgument.Verify(Called.Exactly(3));
		}

		#endregion

		#region Pattern 3 (Standalone Class): ref parameter

		[Fact]
		public void Ref_StandaloneClass_CallbackModifiesValue()
		{
			var stub = new RefOutClassStandaloneKnockOff();
			HaveRefAndOutBase obj = stub.Object;

			stub.RefArgument.Call((ref int a) => a = 42);

			int value = 0;
			obj.RefArgument(ref value);

			Assert.Equal(42, value);
		}

		[Fact]
		public void Ref_StandaloneClass_Unconfigured_ValueUnchanged()
		{
			var stub = new RefOutClassStandaloneKnockOff();
			HaveRefAndOutBase obj = stub.Object;

			// No configuration — ref param should be unchanged
			int value = 99;
			obj.RefArgument(ref value);

			Assert.Equal(99, value);
		}

		[Fact]
		public void Ref_StandaloneClass_VerifyCallTracking()
		{
			var stub = new RefOutClassStandaloneKnockOff();
			HaveRefAndOutBase obj = stub.Object;

			stub.RefArgument.Call((ref int a) => { });

			int v1 = 1;
			obj.RefArgument(ref v1);
			int v2 = 2;
			obj.RefArgument(ref v2);

			stub.RefArgument.Verify(Called.Exactly(2));
		}

		#endregion

		#region Pattern 3 (Standalone Class): out parameter

		[Fact]
		public void Out_StandaloneClass_CallbackSetsValue()
		{
			var stub = new RefOutClassStandaloneKnockOff();
			HaveRefAndOutBase obj = stub.Object;

			stub.OutArgument.Call((out int a) => a = 42);

			obj.OutArgument(out int result);

			Assert.Equal(42, result);
		}

		[Fact]
		public void Out_StandaloneClass_Unconfigured_GetsDefault()
		{
			var stub = new RefOutClassStandaloneKnockOff();
			HaveRefAndOutBase obj = stub.Object;

			// No configuration — out param gets default(int) = 0
			obj.OutArgument(out int result);

			Assert.Equal(0, result);
		}

		[Fact]
		public void Out_StandaloneClass_VerifyCallTracking()
		{
			var stub = new RefOutClassStandaloneKnockOff();
			HaveRefAndOutBase obj = stub.Object;

			stub.OutArgument.Call((out int a) => a = 0);

			obj.OutArgument(out _);
			obj.OutArgument(out _);
			obj.OutArgument(out _);

			stub.OutArgument.Verify(Called.Exactly(3));
		}

		#endregion

		#region Pattern 5 (Inline): ref parameter

		[Fact]
		public void Ref_Inline_CallbackModifiesValue()
		{
			var stub = new MethodMemberInlineTests.Stubs.IHaveRefAndOut();
			IHaveRefAndOut service = stub;

			stub.RefArgument.Call((ref int a) => a = 42);

			int value = 0;
			service.RefArgument(ref value);

			Assert.Equal(42, value);
		}

		[Fact]
		public void Ref_Inline_Unconfigured_ValueUnchanged()
		{
			var stub = new MethodMemberInlineTests.Stubs.IHaveRefAndOut();
			IHaveRefAndOut service = stub;

			// No configuration — ref param should be unchanged
			int value = 99;
			service.RefArgument(ref value);

			Assert.Equal(99, value);
		}

		[Fact]
		public void Ref_Inline_VerifyCallTracking()
		{
			var stub = new MethodMemberInlineTests.Stubs.IHaveRefAndOut();
			IHaveRefAndOut service = stub;

			stub.RefArgument.Call((ref int a) => { });

			int v1 = 1;
			service.RefArgument(ref v1);
			int v2 = 2;
			service.RefArgument(ref v2);

			stub.RefArgument.Verify(Called.Exactly(2));
		}

		#endregion

		#region Pattern 5 (Inline): out parameter

		[Fact]
		public void Out_Inline_CallbackSetsValue()
		{
			var stub = new MethodMemberInlineTests.Stubs.IHaveRefAndOut();
			IHaveRefAndOut service = stub;

			stub.OutArgument.Call((out int a) => a = 42);

			service.OutArgument(out int result);

			Assert.Equal(42, result);
		}

		[Fact]
		public void Out_Inline_Unconfigured_GetsDefault()
		{
			var stub = new MethodMemberInlineTests.Stubs.IHaveRefAndOut();
			IHaveRefAndOut service = stub;

			// No configuration — out param gets default(int) = 0
			service.OutArgument(out int result);

			Assert.Equal(0, result);
		}

		[Fact]
		public void Out_Inline_VerifyCallTracking()
		{
			var stub = new MethodMemberInlineTests.Stubs.IHaveRefAndOut();
			IHaveRefAndOut service = stub;

			stub.OutArgument.Call((out int a) => a = 0);

			service.OutArgument(out _);
			service.OutArgument(out _);
			service.OutArgument(out _);

			stub.OutArgument.Verify(Called.Exactly(3));
		}

		#endregion

		#region Pattern 6 (Inline Class): ref parameter

		[Fact]
		public void Ref_InlineClass_CallbackModifiesValue()
		{
			var stub = new MethodMemberInlineTests.Stubs.HaveRefAndOutBase();
			HaveRefAndOutBase obj = stub.Object;

			stub.RefArgument.Call((ref int a) => a = 42);

			int value = 0;
			obj.RefArgument(ref value);

			Assert.Equal(42, value);
		}

		[Fact]
		public void Ref_InlineClass_Unconfigured_ValueUnchanged()
		{
			var stub = new MethodMemberInlineTests.Stubs.HaveRefAndOutBase();
			HaveRefAndOutBase obj = stub.Object;

			// No configuration — ref param should be unchanged
			int value = 99;
			obj.RefArgument(ref value);

			Assert.Equal(99, value);
		}

		[Fact]
		public void Ref_InlineClass_VerifyCallTracking()
		{
			var stub = new MethodMemberInlineTests.Stubs.HaveRefAndOutBase();
			HaveRefAndOutBase obj = stub.Object;

			stub.RefArgument.Call((ref int a) => { });

			int v1 = 1;
			obj.RefArgument(ref v1);
			int v2 = 2;
			obj.RefArgument(ref v2);

			stub.RefArgument.Verify(Called.Exactly(2));
		}

		#endregion

		#region Pattern 6 (Inline Class): out parameter

		[Fact]
		public void Out_InlineClass_CallbackSetsValue()
		{
			var stub = new MethodMemberInlineTests.Stubs.HaveRefAndOutBase();
			HaveRefAndOutBase obj = stub.Object;

			stub.OutArgument.Call((out int a) => a = 42);

			obj.OutArgument(out int result);

			Assert.Equal(42, result);
		}

		[Fact]
		public void Out_InlineClass_Unconfigured_GetsDefault()
		{
			var stub = new MethodMemberInlineTests.Stubs.HaveRefAndOutBase();
			HaveRefAndOutBase obj = stub.Object;

			// No configuration — out param gets default(int) = 0
			obj.OutArgument(out int result);

			Assert.Equal(0, result);
		}

		[Fact]
		public void Out_InlineClass_VerifyCallTracking()
		{
			var stub = new MethodMemberInlineTests.Stubs.HaveRefAndOutBase();
			HaveRefAndOutBase obj = stub.Object;

			stub.OutArgument.Call((out int a) => a = 0);

			obj.OutArgument(out _);
			obj.OutArgument(out _);
			obj.OutArgument(out _);

			stub.OutArgument.Verify(Called.Exactly(3));
		}

		#endregion
	}
}
