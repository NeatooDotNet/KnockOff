// ============================================================================
// InterfaceMethodReturnTests: Edge case tests for an interface with methods
// returning int, with varying parameter counts.
// Inspired by Rocks.Analysis.IntegrationTests.InterfaceMethodReturnTests
//
// Target interface:
//   public interface IInterfaceMethodReturn
//   {
//       int NoParameters();
//       int OneParameter(int a);
//       int MultipleParameters(int a, string b);
//   }
//
// Tests exercise the INTERFACE pipeline (not class) for:
// 1. Configured return value — Return(42), call, assert 42
// 2. Callback return — Call(() => 42), call, assert 42
// 3. Unconfigured defaults — no configuration, assert default(int) = 0
// 4. One-parameter callback — Call((int a) => a * 2), call with 3, assert 6
// 5. Multi-parameter callback — Call((int a, string b) => a + b.Length), assert
// 6. LastArgs tracking — call with specific args, verify LastArgs
// 7. Verify call count — call multiple times, verify exact count
// 8. Sequence — Return(1, 2, 3), call three times, assert different values
//
// Applicable patterns (interface only, non-generic):
// - Pattern 1 (Standalone): [KnockOff] partial class Stub : IInterface {}
// - Pattern 5 (Inline): [KnockOff<IInterface>]
// ============================================================================

using KnockOff;

// ============================================================================
// TYPE DEFINITIONS
// ============================================================================

namespace KnockOff.Analysis.Tests.InterfaceMethodReturnTestTypes
{
	public interface IInterfaceMethodReturn
	{
		int NoParameters();
		int OneParameter(int a);
		int MultipleParameters(int a, string b);
	}

	// Pattern 1: Standalone interface stub
	[KnockOff]
	public partial class InterfaceMethodReturnStandaloneKnockOff : IInterfaceMethodReturn
	{
	}
}

// ============================================================================
// INLINE PATTERN DECLARATIONS + TESTS
// ============================================================================

namespace KnockOff.Analysis.Tests
{
	using KnockOff.Analysis.Tests.InterfaceMethodReturnTestTypes;

	// Pattern 5: Inline interface stub
	[KnockOff<IInterfaceMethodReturn>]
	public partial class InterfaceMethodReturnInlineTests
	{
	}

	public class InterfaceMethodReturnTests
	{
		// ====================================================================
		// Scenario 1: Configured return value — Return(42), call, assert 42
		// ====================================================================

		#region Standalone: Configured return value (NoParameters)

		[Fact]
		public void ConfiguredReturn_Standalone_ReturnsConfiguredValue()
		{
			var stub = new InterfaceMethodReturnStandaloneKnockOff();
			IInterfaceMethodReturn service = stub;

			stub.NoParameters.Return(42);

			int result = service.NoParameters();

			Assert.Equal(42, result);
		}

		#endregion

		#region Inline: Configured return value (NoParameters)

		[Fact]
		public void ConfiguredReturn_Inline_ReturnsConfiguredValue()
		{
			var stub = new InterfaceMethodReturnInlineTests.Stubs.IInterfaceMethodReturn();
			IInterfaceMethodReturn service = stub;

			stub.NoParameters.Return(42);

			int result = service.NoParameters();

			Assert.Equal(42, result);
		}

		#endregion

		// ====================================================================
		// Scenario 2: Callback return — Call(() => 42), call, assert 42
		// ====================================================================

		#region Standalone: Callback return (NoParameters)

		[Fact]
		public void CallbackReturn_Standalone_ReturnsCallbackResult()
		{
			var stub = new InterfaceMethodReturnStandaloneKnockOff();
			IInterfaceMethodReturn service = stub;

			stub.NoParameters.Call(() => 42);

			int result = service.NoParameters();

			Assert.Equal(42, result);
		}

		#endregion

		#region Inline: Callback return (NoParameters)

		[Fact]
		public void CallbackReturn_Inline_ReturnsCallbackResult()
		{
			var stub = new InterfaceMethodReturnInlineTests.Stubs.IInterfaceMethodReturn();
			IInterfaceMethodReturn service = stub;

			stub.NoParameters.Call(() => 42);

			int result = service.NoParameters();

			Assert.Equal(42, result);
		}

		#endregion

		// ====================================================================
		// Scenario 3: Unconfigured defaults — returns default(int) = 0
		// Interface stubs have no base implementation; unconfigured methods
		// return default for the return type.
		// ====================================================================

		#region Standalone: Unconfigured defaults

		[Fact]
		public void Unconfigured_Standalone_ReturnsDefault()
		{
			var stub = new InterfaceMethodReturnStandaloneKnockOff();
			IInterfaceMethodReturn service = stub;

			// No configuration — returns default(int) = 0
			int result = service.NoParameters();

			Assert.Equal(0, result);
		}

		#endregion

		#region Inline: Unconfigured defaults

		[Fact]
		public void Unconfigured_Inline_ReturnsDefault()
		{
			var stub = new InterfaceMethodReturnInlineTests.Stubs.IInterfaceMethodReturn();
			IInterfaceMethodReturn service = stub;

			int result = service.NoParameters();

			Assert.Equal(0, result);
		}

		#endregion

		// ====================================================================
		// Scenario 4: One-parameter callback
		// Configure with callback (int a) => a * 2, call with 3, assert 6
		// ====================================================================

		#region Standalone: One-parameter callback (OneParameter)

		[Fact]
		public void OneParamCallback_Standalone_ReturnsComputedValue()
		{
			var stub = new InterfaceMethodReturnStandaloneKnockOff();
			IInterfaceMethodReturn service = stub;

			stub.OneParameter.Call((int a) => a * 2);

			int result = service.OneParameter(3);

			Assert.Equal(6, result);
		}

		#endregion

		#region Inline: One-parameter callback (OneParameter)

		[Fact]
		public void OneParamCallback_Inline_ReturnsComputedValue()
		{
			var stub = new InterfaceMethodReturnInlineTests.Stubs.IInterfaceMethodReturn();
			IInterfaceMethodReturn service = stub;

			stub.OneParameter.Call((int a) => a * 2);

			int result = service.OneParameter(3);

			Assert.Equal(6, result);
		}

		#endregion

		// ====================================================================
		// Scenario 5: Multi-parameter callback
		// Configure with callback (int a, string b) => a + b.Length
		// ====================================================================

		#region Standalone: Multi-parameter callback (MultipleParameters)

		[Fact]
		public void MultiParamCallback_Standalone_ReturnsComputedValue()
		{
			var stub = new InterfaceMethodReturnStandaloneKnockOff();
			IInterfaceMethodReturn service = stub;

			stub.MultipleParameters.Call((int a, string b) => a + b.Length);

			int result = service.MultipleParameters(10, "hello");

			Assert.Equal(15, result); // 10 + 5 = 15
		}

		#endregion

		#region Inline: Multi-parameter callback (MultipleParameters)

		[Fact]
		public void MultiParamCallback_Inline_ReturnsComputedValue()
		{
			var stub = new InterfaceMethodReturnInlineTests.Stubs.IInterfaceMethodReturn();
			IInterfaceMethodReturn service = stub;

			stub.MultipleParameters.Call((int a, string b) => a + b.Length);

			int result = service.MultipleParameters(10, "hello");

			Assert.Equal(15, result);
		}

		#endregion

		// ====================================================================
		// Scenario 6: LastArgs tracking
		// Call with specific args, verify LastArgs captures them
		// ====================================================================

		#region Standalone: LastArgs tracking (MultipleParameters)

		[Fact]
		public void LastArgs_Standalone_CapturesLastCallArguments()
		{
			var stub = new InterfaceMethodReturnStandaloneKnockOff();
			IInterfaceMethodReturn service = stub;

			stub.MultipleParameters.Return(0);

			service.MultipleParameters(3, "abc");
			service.MultipleParameters(7, "xyz");

			var args = stub.MultipleParameters.LastArgs;

			Assert.NotNull(args);
			Assert.Equal(7, args.Value.a);
			Assert.Equal("xyz", args.Value.b);
		}

		[Fact]
		public void LastArg_Standalone_CapturesSingleParamLastArg()
		{
			var stub = new InterfaceMethodReturnStandaloneKnockOff();
			IInterfaceMethodReturn service = stub;

			stub.OneParameter.Return(0);

			service.OneParameter(42);
			service.OneParameter(99);

			Assert.Equal(99, stub.OneParameter.LastArg);
		}

		#endregion

		#region Inline: LastArgs tracking (MultipleParameters)

		[Fact]
		public void LastArgs_Inline_CapturesLastCallArguments()
		{
			var stub = new InterfaceMethodReturnInlineTests.Stubs.IInterfaceMethodReturn();
			IInterfaceMethodReturn service = stub;

			stub.MultipleParameters.Return(0);

			service.MultipleParameters(3, "abc");
			service.MultipleParameters(7, "xyz");

			var args = stub.MultipleParameters.LastArgs;

			Assert.NotNull(args);
			Assert.Equal(7, args.Value.a);
			Assert.Equal("xyz", args.Value.b);
		}

		[Fact]
		public void LastArg_Inline_CapturesSingleParamLastArg()
		{
			var stub = new InterfaceMethodReturnInlineTests.Stubs.IInterfaceMethodReturn();
			IInterfaceMethodReturn service = stub;

			stub.OneParameter.Return(0);

			service.OneParameter(42);
			service.OneParameter(99);

			Assert.Equal(99, stub.OneParameter.LastArg);
		}

		#endregion

		// ====================================================================
		// Scenario 7: Verify call count
		// Call multiple times, verify exact count
		// ====================================================================

		#region Standalone: Verify call count

		[Fact]
		public void VerifyCount_Standalone_TracksExactCallCount()
		{
			var stub = new InterfaceMethodReturnStandaloneKnockOff();
			IInterfaceMethodReturn service = stub;

			service.NoParameters();
			service.NoParameters();
			service.NoParameters();

			stub.NoParameters.Verify(Called.Exactly(3));
		}

		[Fact]
		public void VerifyCount_Standalone_MultipleParametersTracked()
		{
			var stub = new InterfaceMethodReturnStandaloneKnockOff();
			IInterfaceMethodReturn service = stub;

			stub.MultipleParameters.Return(0);

			service.MultipleParameters(1, "a");
			service.MultipleParameters(2, "b");

			stub.MultipleParameters.Verify(Called.Exactly(2));
		}

		#endregion

		#region Inline: Verify call count

		[Fact]
		public void VerifyCount_Inline_TracksExactCallCount()
		{
			var stub = new InterfaceMethodReturnInlineTests.Stubs.IInterfaceMethodReturn();
			IInterfaceMethodReturn service = stub;

			service.NoParameters();
			service.NoParameters();
			service.NoParameters();

			stub.NoParameters.Verify(Called.Exactly(3));
		}

		[Fact]
		public void VerifyCount_Inline_MultipleParametersTracked()
		{
			var stub = new InterfaceMethodReturnInlineTests.Stubs.IInterfaceMethodReturn();
			IInterfaceMethodReturn service = stub;

			stub.MultipleParameters.Return(0);

			service.MultipleParameters(1, "a");
			service.MultipleParameters(2, "b");

			stub.MultipleParameters.Verify(Called.Exactly(2));
		}

		#endregion

		// ====================================================================
		// Scenario 8: Sequence — Return(1, 2, 3), call three times
		// Uses the params overload to create a sequence of return values
		// ====================================================================

		#region Standalone: Sequence

		[Fact]
		public void Sequence_Standalone_ReturnsValuesInOrder()
		{
			var stub = new InterfaceMethodReturnStandaloneKnockOff();
			IInterfaceMethodReturn service = stub;

			stub.NoParameters.Return(1, 2, 3);

			int r1 = service.NoParameters();
			int r2 = service.NoParameters();
			int r3 = service.NoParameters();
			int r4 = service.NoParameters(); // repeats last

			Assert.Equal(1, r1);
			Assert.Equal(2, r2);
			Assert.Equal(3, r3);
			Assert.Equal(3, r4);
		}

		#endregion

		#region Inline: Sequence

		[Fact]
		public void Sequence_Inline_ReturnsValuesInOrder()
		{
			var stub = new InterfaceMethodReturnInlineTests.Stubs.IInterfaceMethodReturn();
			IInterfaceMethodReturn service = stub;

			stub.NoParameters.Return(1, 2, 3);

			int r1 = service.NoParameters();
			int r2 = service.NoParameters();
			int r3 = service.NoParameters();
			int r4 = service.NoParameters(); // repeats last

			Assert.Equal(1, r1);
			Assert.Equal(2, r2);
			Assert.Equal(3, r3);
			Assert.Equal(3, r4);
		}

		#endregion
	}
}
