// ============================================================================
// InterfaceMethodVoidTests: Edge case tests for an interface with void methods,
// varying parameter counts.
// Inspired by Rocks.Analysis.IntegrationTests.InterfaceMethodVoidTests
//
// Target interface:
//   public interface IInterfaceMethodVoid
//   {
//       void NoParameters();
//       void OneParameter(int a);
//       void MultipleParameters(int a, string b);
//   }
//
// Tests exercise the INTERFACE pipeline (not class) for:
// 1. Callback invocation — Call(() => { flag = true; }), call, assert flag
// 2. One-param callback captures arg — Call((int a) => { captured = a; })
// 3. Multi-param callback captures args — callback captures both args
// 4. Unconfigured — no config, verify it doesn't throw
// 5. LastArgs tracking — call with args, verify LastArgs
// 6. Verify call count — call multiple times, verify exact count
// 7. Sequence — Call(() => x++).ThenCall(() => x += 10), verify x values
//
// Applicable patterns (interface only, non-generic):
// - Pattern 1 (Standalone): [KnockOff] partial class Stub : IInterface {}
// - Pattern 5 (Inline): [KnockOff<IInterface>]
// ============================================================================

using KnockOff;

// ============================================================================
// TYPE DEFINITIONS
// ============================================================================

namespace KnockOff.Analysis.Tests.InterfaceMethodVoidTestTypes
{
	public interface IInterfaceMethodVoid
	{
		void NoParameters();
		void OneParameter(int a);
		void MultipleParameters(int a, string b);
	}

	// Pattern 1: Standalone interface stub
	[KnockOff]
	public partial class InterfaceMethodVoidStandaloneKnockOff : IInterfaceMethodVoid
	{
	}
}

// ============================================================================
// INLINE PATTERN DECLARATIONS + TESTS
// ============================================================================

namespace KnockOff.Analysis.Tests
{
	using KnockOff.Analysis.Tests.InterfaceMethodVoidTestTypes;

	// Pattern 5: Inline interface stub
	[KnockOff<IInterfaceMethodVoid>]
	public partial class InterfaceMethodVoidInlineTests
	{
	}

	public class InterfaceMethodVoidTests
	{
		// ====================================================================
		// Scenario 1: Callback invocation — Call(() => { flag = true; })
		// ====================================================================

		#region Standalone: Callback invocation (NoParameters)

		[Fact]
		public void Callback_Standalone_InvokesCallback()
		{
			var stub = new InterfaceMethodVoidStandaloneKnockOff();
			IInterfaceMethodVoid service = stub;

			var wasCallbackInvoked = false;
			stub.NoParameters.Call(() => wasCallbackInvoked = true);

			service.NoParameters();

			Assert.True(wasCallbackInvoked);
		}

		#endregion

		#region Inline: Callback invocation (NoParameters)

		[Fact]
		public void Callback_Inline_InvokesCallback()
		{
			var stub = new InterfaceMethodVoidInlineTests.Stubs.IInterfaceMethodVoid();
			IInterfaceMethodVoid service = stub;

			var wasCallbackInvoked = false;
			stub.NoParameters.Call(() => wasCallbackInvoked = true);

			service.NoParameters();

			Assert.True(wasCallbackInvoked);
		}

		#endregion

		// ====================================================================
		// Scenario 2: One-param callback captures arg
		// ====================================================================

		#region Standalone: One-param callback (OneParameter)

		[Fact]
		public void OneParamCallback_Standalone_CapturesArg()
		{
			var stub = new InterfaceMethodVoidStandaloneKnockOff();
			IInterfaceMethodVoid service = stub;

			int captured = 0;
			stub.OneParameter.Call((int a) => captured = a);

			service.OneParameter(3);

			Assert.Equal(3, captured);
		}

		#endregion

		#region Inline: One-param callback (OneParameter)

		[Fact]
		public void OneParamCallback_Inline_CapturesArg()
		{
			var stub = new InterfaceMethodVoidInlineTests.Stubs.IInterfaceMethodVoid();
			IInterfaceMethodVoid service = stub;

			int captured = 0;
			stub.OneParameter.Call((int a) => captured = a);

			service.OneParameter(3);

			Assert.Equal(3, captured);
		}

		#endregion

		// ====================================================================
		// Scenario 3: Multi-param callback captures args
		// ====================================================================

		#region Standalone: Multi-param callback (MultipleParameters)

		[Fact]
		public void MultiParamCallback_Standalone_CapturesArgs()
		{
			var stub = new InterfaceMethodVoidStandaloneKnockOff();
			IInterfaceMethodVoid service = stub;

			int capturedA = 0;
			string capturedB = "";
			stub.MultipleParameters.Call((int a, string b) =>
			{
				capturedA = a;
				capturedB = b;
			});

			service.MultipleParameters(42, "hello");

			Assert.Equal(42, capturedA);
			Assert.Equal("hello", capturedB);
		}

		#endregion

		#region Inline: Multi-param callback (MultipleParameters)

		[Fact]
		public void MultiParamCallback_Inline_CapturesArgs()
		{
			var stub = new InterfaceMethodVoidInlineTests.Stubs.IInterfaceMethodVoid();
			IInterfaceMethodVoid service = stub;

			int capturedA = 0;
			string capturedB = "";
			stub.MultipleParameters.Call((int a, string b) =>
			{
				capturedA = a;
				capturedB = b;
			});

			service.MultipleParameters(42, "hello");

			Assert.Equal(42, capturedA);
			Assert.Equal("hello", capturedB);
		}

		#endregion

		// ====================================================================
		// Scenario 4: Unconfigured — no config, verify it doesn't throw
		// Interface stubs have no base implementation; unconfigured void
		// methods are no-ops that don't throw.
		// ====================================================================

		#region Standalone: Unconfigured void methods

		[Fact]
		public void Unconfigured_Standalone_DoesNotThrow()
		{
			var stub = new InterfaceMethodVoidStandaloneKnockOff();
			IInterfaceMethodVoid service = stub;

			// No configuration — should not throw
			var exception = Record.Exception(() => service.NoParameters());

			Assert.Null(exception);
		}

		[Fact]
		public void Unconfigured_Standalone_OneParamDoesNotThrow()
		{
			var stub = new InterfaceMethodVoidStandaloneKnockOff();
			IInterfaceMethodVoid service = stub;

			var exception = Record.Exception(() => service.OneParameter(99));

			Assert.Null(exception);
		}

		[Fact]
		public void Unconfigured_Standalone_MultipleParamsDoesNotThrow()
		{
			var stub = new InterfaceMethodVoidStandaloneKnockOff();
			IInterfaceMethodVoid service = stub;

			var exception = Record.Exception(() => service.MultipleParameters(1, "test"));

			Assert.Null(exception);
		}

		#endregion

		#region Inline: Unconfigured void methods

		[Fact]
		public void Unconfigured_Inline_DoesNotThrow()
		{
			var stub = new InterfaceMethodVoidInlineTests.Stubs.IInterfaceMethodVoid();
			IInterfaceMethodVoid service = stub;

			var exception = Record.Exception(() => service.NoParameters());

			Assert.Null(exception);
		}

		[Fact]
		public void Unconfigured_Inline_OneParamDoesNotThrow()
		{
			var stub = new InterfaceMethodVoidInlineTests.Stubs.IInterfaceMethodVoid();
			IInterfaceMethodVoid service = stub;

			var exception = Record.Exception(() => service.OneParameter(99));

			Assert.Null(exception);
		}

		[Fact]
		public void Unconfigured_Inline_MultipleParamsDoesNotThrow()
		{
			var stub = new InterfaceMethodVoidInlineTests.Stubs.IInterfaceMethodVoid();
			IInterfaceMethodVoid service = stub;

			var exception = Record.Exception(() => service.MultipleParameters(1, "test"));

			Assert.Null(exception);
		}

		#endregion

		// ====================================================================
		// Scenario 5: LastArgs tracking
		// Call with args, verify LastArgs captures them
		// ====================================================================

		#region Standalone: LastArgs tracking (MultipleParameters)

		[Fact]
		public void LastArgs_Standalone_CapturesLastCallArguments()
		{
			var stub = new InterfaceMethodVoidStandaloneKnockOff();
			IInterfaceMethodVoid service = stub;

			stub.MultipleParameters.Call((int a, string b) => { });

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
			var stub = new InterfaceMethodVoidStandaloneKnockOff();
			IInterfaceMethodVoid service = stub;

			stub.OneParameter.Call((int a) => { });

			service.OneParameter(42);
			service.OneParameter(99);

			Assert.Equal(99, stub.OneParameter.LastArg);
		}

		#endregion

		#region Inline: LastArgs tracking (MultipleParameters)

		[Fact]
		public void LastArgs_Inline_CapturesLastCallArguments()
		{
			var stub = new InterfaceMethodVoidInlineTests.Stubs.IInterfaceMethodVoid();
			IInterfaceMethodVoid service = stub;

			stub.MultipleParameters.Call((int a, string b) => { });

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
			var stub = new InterfaceMethodVoidInlineTests.Stubs.IInterfaceMethodVoid();
			IInterfaceMethodVoid service = stub;

			stub.OneParameter.Call((int a) => { });

			service.OneParameter(42);
			service.OneParameter(99);

			Assert.Equal(99, stub.OneParameter.LastArg);
		}

		#endregion

		// ====================================================================
		// Scenario 6: Verify call count
		// Call multiple times, verify exact count
		// ====================================================================

		#region Standalone: Verify call count

		[Fact]
		public void VerifyCount_Standalone_TracksExactCallCount()
		{
			var stub = new InterfaceMethodVoidStandaloneKnockOff();
			IInterfaceMethodVoid service = stub;

			service.NoParameters();
			service.NoParameters();
			service.NoParameters();

			stub.NoParameters.Verify(Called.Exactly(3));
		}

		[Fact]
		public void VerifyCount_Standalone_MultipleParametersTracked()
		{
			var stub = new InterfaceMethodVoidStandaloneKnockOff();
			IInterfaceMethodVoid service = stub;

			stub.MultipleParameters.Call((int a, string b) => { });

			service.MultipleParameters(1, "a");
			service.MultipleParameters(2, "b");

			stub.MultipleParameters.Verify(Called.Exactly(2));
		}

		#endregion

		#region Inline: Verify call count

		[Fact]
		public void VerifyCount_Inline_TracksExactCallCount()
		{
			var stub = new InterfaceMethodVoidInlineTests.Stubs.IInterfaceMethodVoid();
			IInterfaceMethodVoid service = stub;

			service.NoParameters();
			service.NoParameters();
			service.NoParameters();

			stub.NoParameters.Verify(Called.Exactly(3));
		}

		[Fact]
		public void VerifyCount_Inline_MultipleParametersTracked()
		{
			var stub = new InterfaceMethodVoidInlineTests.Stubs.IInterfaceMethodVoid();
			IInterfaceMethodVoid service = stub;

			stub.MultipleParameters.Call((int a, string b) => { });

			service.MultipleParameters(1, "a");
			service.MultipleParameters(2, "b");

			stub.MultipleParameters.Verify(Called.Exactly(2));
		}

		#endregion

		// ====================================================================
		// Scenario 7: Sequence — Call().ThenCall()
		// Void methods use Call().ThenCall() for sequences
		// ====================================================================

		#region Standalone: Sequence

		[Fact]
		public void Sequence_Standalone_ExecutesCallbacksInOrder()
		{
			var stub = new InterfaceMethodVoidStandaloneKnockOff();
			IInterfaceMethodVoid service = stub;

			int x = 0;
			stub.NoParameters
				.Call(() => x++)
				.ThenCall(() => x += 10);

			service.NoParameters(); // x = 1
			service.NoParameters(); // x = 11

			Assert.Equal(11, x);
		}

		[Fact]
		public void Sequence_Standalone_RepeatsLastAfterExhaustion()
		{
			var stub = new InterfaceMethodVoidStandaloneKnockOff();
			IInterfaceMethodVoid service = stub;

			var log = new List<string>();
			stub.NoParameters
				.Call(() => log.Add("first"))
				.ThenCall(() => log.Add("second"));

			service.NoParameters(); // "first"
			service.NoParameters(); // "second"
			service.NoParameters(); // "second" (repeats last)

			Assert.Equal(3, log.Count);
			Assert.Equal("first", log[0]);
			Assert.Equal("second", log[1]);
			Assert.Equal("second", log[2]);
		}

		#endregion

		#region Inline: Sequence

		[Fact]
		public void Sequence_Inline_ExecutesCallbacksInOrder()
		{
			var stub = new InterfaceMethodVoidInlineTests.Stubs.IInterfaceMethodVoid();
			IInterfaceMethodVoid service = stub;

			int x = 0;
			stub.NoParameters
				.Call(() => x++)
				.ThenCall(() => x += 10);

			service.NoParameters(); // x = 1
			service.NoParameters(); // x = 11

			Assert.Equal(11, x);
		}

		[Fact]
		public void Sequence_Inline_RepeatsLastAfterExhaustion()
		{
			var stub = new InterfaceMethodVoidInlineTests.Stubs.IInterfaceMethodVoid();
			IInterfaceMethodVoid service = stub;

			var log = new List<string>();
			stub.NoParameters
				.Call(() => log.Add("first"))
				.ThenCall(() => log.Add("second"));

			service.NoParameters(); // "first"
			service.NoParameters(); // "second"
			service.NoParameters(); // "second" (repeats last)

			Assert.Equal(3, log.Count);
			Assert.Equal("first", log[0]);
			Assert.Equal("second", log[1]);
			Assert.Equal("second", log[2]);
		}

		#endregion
	}
}
