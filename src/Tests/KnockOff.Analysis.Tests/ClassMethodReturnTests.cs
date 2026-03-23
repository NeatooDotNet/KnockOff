// ============================================================================
// ClassMethodReturnTests: Edge case tests for a class with virtual methods
// returning int, with varying parameter counts.
// Inspired by Rocks.Analysis.IntegrationTests.ClassMethodReturnTests
//
// Target class:
//   public class ClassMethodReturn
//   {
//       public virtual int NoParameters() => default;
//       public virtual int OneParameter(int a) => default;
//       public virtual int MultipleParameters(int a, string b) => default;
//   }
//
// Tests exercise the CLASS pipeline (not interface) for:
// 1. Configured return value — Return(42), call, assert 42
// 2. Callback return — Call(() => 42), call, assert 42
// 3. Unconfigured fallback to base — no configuration, assert default(int) = 0
// 4. One-parameter callback — Call((int a) => a * 2), call with 3, assert 6
// 5. Multi-parameter callback — Call((int a, string b) => a + b.Length), assert
// 6. LastArgs tracking — call with specific args, verify LastArgs
// 7. Verify call count — call multiple times, verify exact count
// 8. Sequence — Return(1, 2, 3), call three times, assert different values
//
// Applicable patterns (class only):
// - Pattern 3 (Standalone Class): [KnockOffBase<ClassMethodReturn>]
// - Pattern 6 (Inline Class): [KnockOff<ClassMethodReturn>]
// ============================================================================

using KnockOff;

// ============================================================================
// TYPE DEFINITIONS
// ============================================================================

namespace KnockOff.Analysis.Tests.ClassMethodReturnTestTypes
{
	public class ClassMethodReturn
	{
		public virtual int NoParameters() => default;
		public virtual int OneParameter(int a) => default;
		public virtual int MultipleParameters(int a, string b) => default;
	}

	// Pattern 3: Standalone class stub
	[KnockOffBase<ClassMethodReturn>]
	public partial class ClassMethodReturnStandaloneKnockOff
	{
	}
}

// ============================================================================
// INLINE PATTERN DECLARATIONS + TESTS
// ============================================================================

namespace KnockOff.Analysis.Tests
{
	using KnockOff.Analysis.Tests.ClassMethodReturnTestTypes;

	// Pattern 6: Inline class stub
	[KnockOff<ClassMethodReturn>]
	public partial class ClassMethodReturnInlineTests
	{
	}

	public class ClassMethodReturnTests
	{
		// ====================================================================
		// Scenario 1: Configured return value — Return(42), call, assert 42
		// ====================================================================

		#region Standalone Class: Configured return value (NoParameters)

		[Fact]
		public void ConfiguredReturn_StandaloneClass_ReturnsConfiguredValue()
		{
			var stub = new ClassMethodReturnStandaloneKnockOff();
			ClassMethodReturn obj = stub.Object;

			stub.NoParameters.Return(42);

			int result = obj.NoParameters();

			Assert.Equal(42, result);
		}

		#endregion

		#region Inline Class: Configured return value (NoParameters)

		[Fact]
		public void ConfiguredReturn_InlineClass_ReturnsConfiguredValue()
		{
			var stub = new ClassMethodReturnInlineTests.Stubs.ClassMethodReturn();
			ClassMethodReturn obj = stub.Object;

			stub.NoParameters.Return(42);

			int result = obj.NoParameters();

			Assert.Equal(42, result);
		}

		#endregion

		// ====================================================================
		// Scenario 2: Callback return — Call(() => 42), call, assert 42
		// ====================================================================

		#region Standalone Class: Callback return (NoParameters)

		[Fact]
		public void CallbackReturn_StandaloneClass_ReturnsCallbackResult()
		{
			var stub = new ClassMethodReturnStandaloneKnockOff();
			ClassMethodReturn obj = stub.Object;

			stub.NoParameters.Call(() => 42);

			int result = obj.NoParameters();

			Assert.Equal(42, result);
		}

		#endregion

		#region Inline Class: Callback return (NoParameters)

		[Fact]
		public void CallbackReturn_InlineClass_ReturnsCallbackResult()
		{
			var stub = new ClassMethodReturnInlineTests.Stubs.ClassMethodReturn();
			ClassMethodReturn obj = stub.Object;

			stub.NoParameters.Call(() => 42);

			int result = obj.NoParameters();

			Assert.Equal(42, result);
		}

		#endregion

		// ====================================================================
		// Scenario 3: Unconfigured fallback to base — returns default(int)
		// Because base implementations all return default, unconfigured calls
		// fall through to base and return 0.
		// ====================================================================

		#region Standalone Class: Unconfigured fallback to base

		[Fact]
		public void Unconfigured_StandaloneClass_FallsToBaseReturningDefault()
		{
			var stub = new ClassMethodReturnStandaloneKnockOff();
			ClassMethodReturn obj = stub.Object;

			// No configuration — falls through to base.NoParameters() which returns default
			int result = obj.NoParameters();

			Assert.Equal(0, result);
		}

		#endregion

		#region Inline Class: Unconfigured fallback to base

		[Fact]
		public void Unconfigured_InlineClass_FallsToBaseReturningDefault()
		{
			var stub = new ClassMethodReturnInlineTests.Stubs.ClassMethodReturn();
			ClassMethodReturn obj = stub.Object;

			int result = obj.NoParameters();

			Assert.Equal(0, result);
		}

		#endregion

		// ====================================================================
		// Scenario 4: One-parameter callback
		// Configure with callback (int a) => a * 2, call with 3, assert 6
		// ====================================================================

		#region Standalone Class: One-parameter callback (OneParameter)

		[Fact]
		public void OneParamCallback_StandaloneClass_ReturnsComputedValue()
		{
			var stub = new ClassMethodReturnStandaloneKnockOff();
			ClassMethodReturn obj = stub.Object;

			stub.OneParameter.Call((int a) => a * 2);

			int result = obj.OneParameter(3);

			Assert.Equal(6, result);
		}

		#endregion

		#region Inline Class: One-parameter callback (OneParameter)

		[Fact]
		public void OneParamCallback_InlineClass_ReturnsComputedValue()
		{
			var stub = new ClassMethodReturnInlineTests.Stubs.ClassMethodReturn();
			ClassMethodReturn obj = stub.Object;

			stub.OneParameter.Call((int a) => a * 2);

			int result = obj.OneParameter(3);

			Assert.Equal(6, result);
		}

		#endregion

		// ====================================================================
		// Scenario 5: Multi-parameter callback
		// Configure with callback (int a, string b) => a + b.Length
		// ====================================================================

		#region Standalone Class: Multi-parameter callback (MultipleParameters)

		[Fact]
		public void MultiParamCallback_StandaloneClass_ReturnsComputedValue()
		{
			var stub = new ClassMethodReturnStandaloneKnockOff();
			ClassMethodReturn obj = stub.Object;

			stub.MultipleParameters.Call((int a, string b) => a + b.Length);

			int result = obj.MultipleParameters(10, "hello");

			Assert.Equal(15, result); // 10 + 5 = 15
		}

		#endregion

		#region Inline Class: Multi-parameter callback (MultipleParameters)

		[Fact]
		public void MultiParamCallback_InlineClass_ReturnsComputedValue()
		{
			var stub = new ClassMethodReturnInlineTests.Stubs.ClassMethodReturn();
			ClassMethodReturn obj = stub.Object;

			stub.MultipleParameters.Call((int a, string b) => a + b.Length);

			int result = obj.MultipleParameters(10, "hello");

			Assert.Equal(15, result);
		}

		#endregion

		// ====================================================================
		// Scenario 6: LastArgs tracking
		// Call with specific args, verify LastArgs captures them
		// ====================================================================

		#region Standalone Class: LastArgs tracking (MultipleParameters)

		[Fact]
		public void LastArgs_StandaloneClass_CapturesLastCallArguments()
		{
			var stub = new ClassMethodReturnStandaloneKnockOff();
			ClassMethodReturn obj = stub.Object;

			stub.MultipleParameters.Return(0);

			obj.MultipleParameters(3, "abc");
			obj.MultipleParameters(7, "xyz");

			var args = stub.MultipleParameters.LastArgs;

			Assert.NotNull(args);
			Assert.Equal(7, args.Value.a);
			Assert.Equal("xyz", args.Value.b);
		}

		[Fact]
		public void LastArg_StandaloneClass_CapturesSingleParamLastArg()
		{
			var stub = new ClassMethodReturnStandaloneKnockOff();
			ClassMethodReturn obj = stub.Object;

			stub.OneParameter.Return(0);

			obj.OneParameter(42);
			obj.OneParameter(99);

			Assert.Equal(99, stub.OneParameter.LastArg);
		}

		#endregion

		#region Inline Class: LastArgs tracking (MultipleParameters)

		[Fact]
		public void LastArgs_InlineClass_CapturesLastCallArguments()
		{
			var stub = new ClassMethodReturnInlineTests.Stubs.ClassMethodReturn();
			ClassMethodReturn obj = stub.Object;

			stub.MultipleParameters.Return(0);

			obj.MultipleParameters(3, "abc");
			obj.MultipleParameters(7, "xyz");

			var args = stub.MultipleParameters.LastArgs;

			Assert.NotNull(args);
			Assert.Equal(7, args.Value.a);
			Assert.Equal("xyz", args.Value.b);
		}

		[Fact]
		public void LastArg_InlineClass_CapturesSingleParamLastArg()
		{
			var stub = new ClassMethodReturnInlineTests.Stubs.ClassMethodReturn();
			ClassMethodReturn obj = stub.Object;

			stub.OneParameter.Return(0);

			obj.OneParameter(42);
			obj.OneParameter(99);

			Assert.Equal(99, stub.OneParameter.LastArg);
		}

		#endregion

		// ====================================================================
		// Scenario 7: Verify call count
		// Call multiple times, verify exact count
		// ====================================================================

		#region Standalone Class: Verify call count

		[Fact]
		public void VerifyCount_StandaloneClass_TracksExactCallCount()
		{
			var stub = new ClassMethodReturnStandaloneKnockOff();
			ClassMethodReturn obj = stub.Object;

			obj.NoParameters();
			obj.NoParameters();
			obj.NoParameters();

			stub.NoParameters.Verify(Called.Exactly(3));
		}

		[Fact]
		public void VerifyCount_StandaloneClass_MultipleParametersTracked()
		{
			var stub = new ClassMethodReturnStandaloneKnockOff();
			ClassMethodReturn obj = stub.Object;

			stub.MultipleParameters.Return(0);

			obj.MultipleParameters(1, "a");
			obj.MultipleParameters(2, "b");

			stub.MultipleParameters.Verify(Called.Exactly(2));
		}

		#endregion

		#region Inline Class: Verify call count

		[Fact]
		public void VerifyCount_InlineClass_TracksExactCallCount()
		{
			var stub = new ClassMethodReturnInlineTests.Stubs.ClassMethodReturn();
			ClassMethodReturn obj = stub.Object;

			obj.NoParameters();
			obj.NoParameters();
			obj.NoParameters();

			stub.NoParameters.Verify(Called.Exactly(3));
		}

		[Fact]
		public void VerifyCount_InlineClass_MultipleParametersTracked()
		{
			var stub = new ClassMethodReturnInlineTests.Stubs.ClassMethodReturn();
			ClassMethodReturn obj = stub.Object;

			stub.MultipleParameters.Return(0);

			obj.MultipleParameters(1, "a");
			obj.MultipleParameters(2, "b");

			stub.MultipleParameters.Verify(Called.Exactly(2));
		}

		#endregion

		// ====================================================================
		// Scenario 8: Sequence — Return(1, 2, 3), call three times
		// Uses the params overload to create a sequence of return values
		// ====================================================================

		#region Standalone Class: Sequence

		[Fact]
		public void Sequence_StandaloneClass_ReturnsValuesInOrder()
		{
			var stub = new ClassMethodReturnStandaloneKnockOff();
			ClassMethodReturn obj = stub.Object;

			stub.NoParameters.Return(1, 2, 3);

			int r1 = obj.NoParameters();
			int r2 = obj.NoParameters();
			int r3 = obj.NoParameters();
			int r4 = obj.NoParameters(); // repeats last

			Assert.Equal(1, r1);
			Assert.Equal(2, r2);
			Assert.Equal(3, r3);
			Assert.Equal(3, r4);
		}

		#endregion

		#region Inline Class: Sequence

		[Fact]
		public void Sequence_InlineClass_ReturnsValuesInOrder()
		{
			var stub = new ClassMethodReturnInlineTests.Stubs.ClassMethodReturn();
			ClassMethodReturn obj = stub.Object;

			stub.NoParameters.Return(1, 2, 3);

			int r1 = obj.NoParameters();
			int r2 = obj.NoParameters();
			int r3 = obj.NoParameters();
			int r4 = obj.NoParameters(); // repeats last

			Assert.Equal(1, r1);
			Assert.Equal(2, r2);
			Assert.Equal(3, r3);
			Assert.Equal(3, r4);
		}

		#endregion
	}
}
