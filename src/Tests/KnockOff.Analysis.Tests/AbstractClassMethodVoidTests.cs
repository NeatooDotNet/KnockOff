// ============================================================================
// AbstractClassMethodVoidTests: Edge case tests for an abstract class with
// abstract void methods, with varying parameter counts.
// Inspired by Rocks.Analysis.IntegrationTests.AbstractClassMethodVoidTests
//
// Target class:
//   public abstract class AbstractClassMethodVoid
//   {
//       public abstract void NoParameters();
//       public abstract void OneParameter(int a);
//       public abstract void MultipleParameters(int a, string b);
//   }
//
// Key difference from ClassMethodVoidTests: members are ABSTRACT, not virtual.
// Abstract members have no base implementation to fall through to, so unconfigured
// calls simply do nothing (return immediately), not via base.Method().
//
// Tests exercise the CLASS pipeline (not interface) for:
// 1. Callback invocation — Call(() => { flag = true; }), call, assert flag
// 2. One-param callback captures arg — Call((int a) => { captured = a; })
// 3. Multi-param callback captures args — callback captures both args
// 4. Unconfigured (no throw) — no config, verify it doesn't throw
// 5. LastArgs tracking — call with args, verify LastArgs
// 6. Verify call count — call multiple times, verify exact count
// 7. Sequence — Call(() => x++).ThenCall(() => x += 10), verify x values
//
// Applicable patterns (class only):
// - Pattern 3 (Standalone Class): [KnockOffBase<AbstractClassMethodVoid>]
// - Pattern 6 (Inline Class): [KnockOff<AbstractClassMethodVoid>]
// ============================================================================

using KnockOff;

// ============================================================================
// TYPE DEFINITIONS
// ============================================================================

namespace KnockOff.Analysis.Tests.AbstractClassMethodVoidTestTypes
{
	public abstract class AbstractClassMethodVoid
	{
		public abstract void NoParameters();
		public abstract void OneParameter(int a);
		public abstract void MultipleParameters(int a, string b);
	}

	// Pattern 3: Standalone class stub
	[KnockOffBase<AbstractClassMethodVoid>]
	public partial class AbstractClassMethodVoidStandaloneKnockOff
	{
	}
}

// ============================================================================
// INLINE PATTERN DECLARATIONS + TESTS
// ============================================================================

namespace KnockOff.Analysis.Tests
{
	using KnockOff.Analysis.Tests.AbstractClassMethodVoidTestTypes;

	// Pattern 6: Inline class stub
	[KnockOff<AbstractClassMethodVoid>]
	public partial class AbstractClassMethodVoidInlineTests
	{
	}

	public class AbstractClassMethodVoidTests
	{
		// ====================================================================
		// Scenario 1: Callback invocation — Call(() => { flag = true; })
		// ====================================================================

		#region Standalone Class: Callback invocation (NoParameters)

		[Fact]
		public void Callback_StandaloneClass_InvokesCallback()
		{
			var stub = new AbstractClassMethodVoidStandaloneKnockOff();
			AbstractClassMethodVoid obj = stub.Object;

			var wasCallbackInvoked = false;
			stub.NoParameters.Call(() => wasCallbackInvoked = true);

			obj.NoParameters();

			Assert.True(wasCallbackInvoked);
		}

		#endregion

		#region Inline Class: Callback invocation (NoParameters)

		[Fact]
		public void Callback_InlineClass_InvokesCallback()
		{
			var stub = new AbstractClassMethodVoidInlineTests.Stubs.AbstractClassMethodVoid();
			AbstractClassMethodVoid obj = stub.Object;

			var wasCallbackInvoked = false;
			stub.NoParameters.Call(() => wasCallbackInvoked = true);

			obj.NoParameters();

			Assert.True(wasCallbackInvoked);
		}

		#endregion

		// ====================================================================
		// Scenario 2: One-param callback captures arg
		// ====================================================================

		#region Standalone Class: One-param callback (OneParameter)

		[Fact]
		public void OneParamCallback_StandaloneClass_CapturesArg()
		{
			var stub = new AbstractClassMethodVoidStandaloneKnockOff();
			AbstractClassMethodVoid obj = stub.Object;

			int captured = 0;
			stub.OneParameter.Call((int a) => captured = a);

			obj.OneParameter(3);

			Assert.Equal(3, captured);
		}

		#endregion

		#region Inline Class: One-param callback (OneParameter)

		[Fact]
		public void OneParamCallback_InlineClass_CapturesArg()
		{
			var stub = new AbstractClassMethodVoidInlineTests.Stubs.AbstractClassMethodVoid();
			AbstractClassMethodVoid obj = stub.Object;

			int captured = 0;
			stub.OneParameter.Call((int a) => captured = a);

			obj.OneParameter(3);

			Assert.Equal(3, captured);
		}

		#endregion

		// ====================================================================
		// Scenario 3: Multi-param callback captures args
		// ====================================================================

		#region Standalone Class: Multi-param callback (MultipleParameters)

		[Fact]
		public void MultiParamCallback_StandaloneClass_CapturesArgs()
		{
			var stub = new AbstractClassMethodVoidStandaloneKnockOff();
			AbstractClassMethodVoid obj = stub.Object;

			int capturedA = 0;
			string capturedB = "";
			stub.MultipleParameters.Call((int a, string b) =>
			{
				capturedA = a;
				capturedB = b;
			});

			obj.MultipleParameters(42, "hello");

			Assert.Equal(42, capturedA);
			Assert.Equal("hello", capturedB);
		}

		#endregion

		#region Inline Class: Multi-param callback (MultipleParameters)

		[Fact]
		public void MultiParamCallback_InlineClass_CapturesArgs()
		{
			var stub = new AbstractClassMethodVoidInlineTests.Stubs.AbstractClassMethodVoid();
			AbstractClassMethodVoid obj = stub.Object;

			int capturedA = 0;
			string capturedB = "";
			stub.MultipleParameters.Call((int a, string b) =>
			{
				capturedA = a;
				capturedB = b;
			});

			obj.MultipleParameters(42, "hello");

			Assert.Equal(42, capturedA);
			Assert.Equal("hello", capturedB);
		}

		#endregion

		// ====================================================================
		// Scenario 4: Unconfigured (no throw)
		// Abstract void methods have no base implementation. Unconfigured calls
		// simply return without doing anything (no throw).
		// ====================================================================

		#region Standalone Class: Unconfigured (no throw)

		[Fact]
		public void Unconfigured_StandaloneClass_DoesNotThrow()
		{
			var stub = new AbstractClassMethodVoidStandaloneKnockOff();
			AbstractClassMethodVoid obj = stub.Object;

			// No configuration — abstract void method returns immediately
			var exception = Record.Exception(() => obj.NoParameters());

			Assert.Null(exception);
		}

		[Fact]
		public void Unconfigured_StandaloneClass_OneParamDoesNotThrow()
		{
			var stub = new AbstractClassMethodVoidStandaloneKnockOff();
			AbstractClassMethodVoid obj = stub.Object;

			var exception = Record.Exception(() => obj.OneParameter(99));

			Assert.Null(exception);
		}

		[Fact]
		public void Unconfigured_StandaloneClass_MultipleParamsDoesNotThrow()
		{
			var stub = new AbstractClassMethodVoidStandaloneKnockOff();
			AbstractClassMethodVoid obj = stub.Object;

			var exception = Record.Exception(() => obj.MultipleParameters(1, "test"));

			Assert.Null(exception);
		}

		#endregion

		#region Inline Class: Unconfigured (no throw)

		[Fact]
		public void Unconfigured_InlineClass_DoesNotThrow()
		{
			var stub = new AbstractClassMethodVoidInlineTests.Stubs.AbstractClassMethodVoid();
			AbstractClassMethodVoid obj = stub.Object;

			var exception = Record.Exception(() => obj.NoParameters());

			Assert.Null(exception);
		}

		[Fact]
		public void Unconfigured_InlineClass_OneParamDoesNotThrow()
		{
			var stub = new AbstractClassMethodVoidInlineTests.Stubs.AbstractClassMethodVoid();
			AbstractClassMethodVoid obj = stub.Object;

			var exception = Record.Exception(() => obj.OneParameter(99));

			Assert.Null(exception);
		}

		[Fact]
		public void Unconfigured_InlineClass_MultipleParamsDoesNotThrow()
		{
			var stub = new AbstractClassMethodVoidInlineTests.Stubs.AbstractClassMethodVoid();
			AbstractClassMethodVoid obj = stub.Object;

			var exception = Record.Exception(() => obj.MultipleParameters(1, "test"));

			Assert.Null(exception);
		}

		#endregion

		// ====================================================================
		// Scenario 5: LastArgs tracking
		// Call with args, verify LastArgs captures them
		// ====================================================================

		#region Standalone Class: LastArgs tracking (MultipleParameters)

		[Fact]
		public void LastArgs_StandaloneClass_CapturesLastCallArguments()
		{
			var stub = new AbstractClassMethodVoidStandaloneKnockOff();
			AbstractClassMethodVoid obj = stub.Object;

			stub.MultipleParameters.Call((int a, string b) => { });

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
			var stub = new AbstractClassMethodVoidStandaloneKnockOff();
			AbstractClassMethodVoid obj = stub.Object;

			stub.OneParameter.Call((int a) => { });

			obj.OneParameter(42);
			obj.OneParameter(99);

			Assert.Equal(99, stub.OneParameter.LastArg);
		}

		#endregion

		#region Inline Class: LastArgs tracking (MultipleParameters)

		[Fact]
		public void LastArgs_InlineClass_CapturesLastCallArguments()
		{
			var stub = new AbstractClassMethodVoidInlineTests.Stubs.AbstractClassMethodVoid();
			AbstractClassMethodVoid obj = stub.Object;

			stub.MultipleParameters.Call((int a, string b) => { });

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
			var stub = new AbstractClassMethodVoidInlineTests.Stubs.AbstractClassMethodVoid();
			AbstractClassMethodVoid obj = stub.Object;

			stub.OneParameter.Call((int a) => { });

			obj.OneParameter(42);
			obj.OneParameter(99);

			Assert.Equal(99, stub.OneParameter.LastArg);
		}

		#endregion

		// ====================================================================
		// Scenario 6: Verify call count
		// Call multiple times, verify exact count
		// ====================================================================

		#region Standalone Class: Verify call count

		[Fact]
		public void VerifyCount_StandaloneClass_TracksExactCallCount()
		{
			var stub = new AbstractClassMethodVoidStandaloneKnockOff();
			AbstractClassMethodVoid obj = stub.Object;

			obj.NoParameters();
			obj.NoParameters();
			obj.NoParameters();

			stub.NoParameters.Verify(Called.Exactly(3));
		}

		[Fact]
		public void VerifyCount_StandaloneClass_MultipleParametersTracked()
		{
			var stub = new AbstractClassMethodVoidStandaloneKnockOff();
			AbstractClassMethodVoid obj = stub.Object;

			stub.MultipleParameters.Call((int a, string b) => { });

			obj.MultipleParameters(1, "a");
			obj.MultipleParameters(2, "b");

			stub.MultipleParameters.Verify(Called.Exactly(2));
		}

		#endregion

		#region Inline Class: Verify call count

		[Fact]
		public void VerifyCount_InlineClass_TracksExactCallCount()
		{
			var stub = new AbstractClassMethodVoidInlineTests.Stubs.AbstractClassMethodVoid();
			AbstractClassMethodVoid obj = stub.Object;

			obj.NoParameters();
			obj.NoParameters();
			obj.NoParameters();

			stub.NoParameters.Verify(Called.Exactly(3));
		}

		[Fact]
		public void VerifyCount_InlineClass_MultipleParametersTracked()
		{
			var stub = new AbstractClassMethodVoidInlineTests.Stubs.AbstractClassMethodVoid();
			AbstractClassMethodVoid obj = stub.Object;

			stub.MultipleParameters.Call((int a, string b) => { });

			obj.MultipleParameters(1, "a");
			obj.MultipleParameters(2, "b");

			stub.MultipleParameters.Verify(Called.Exactly(2));
		}

		#endregion

		// ====================================================================
		// Scenario 7: Sequence — Call().ThenCall()
		// Void methods use Call().ThenCall() for sequences (not ThenReturn)
		// ====================================================================

		#region Standalone Class: Sequence

		[Fact]
		public void Sequence_StandaloneClass_ExecutesCallbacksInOrder()
		{
			var stub = new AbstractClassMethodVoidStandaloneKnockOff();
			AbstractClassMethodVoid obj = stub.Object;

			int x = 0;
			stub.NoParameters
				.Call(() => x++)
				.ThenCall(() => x += 10);

			obj.NoParameters(); // x = 1
			obj.NoParameters(); // x = 11

			Assert.Equal(11, x);
		}

		[Fact]
		public void Sequence_StandaloneClass_RepeatsLastAfterExhaustion()
		{
			var stub = new AbstractClassMethodVoidStandaloneKnockOff();
			AbstractClassMethodVoid obj = stub.Object;

			var log = new List<string>();
			stub.NoParameters
				.Call(() => log.Add("first"))
				.ThenCall(() => log.Add("second"));

			obj.NoParameters(); // "first"
			obj.NoParameters(); // "second"
			obj.NoParameters(); // "second" (repeats last)

			Assert.Equal(3, log.Count);
			Assert.Equal("first", log[0]);
			Assert.Equal("second", log[1]);
			Assert.Equal("second", log[2]);
		}

		#endregion

		#region Inline Class: Sequence

		[Fact]
		public void Sequence_InlineClass_ExecutesCallbacksInOrder()
		{
			var stub = new AbstractClassMethodVoidInlineTests.Stubs.AbstractClassMethodVoid();
			AbstractClassMethodVoid obj = stub.Object;

			int x = 0;
			stub.NoParameters
				.Call(() => x++)
				.ThenCall(() => x += 10);

			obj.NoParameters(); // x = 1
			obj.NoParameters(); // x = 11

			Assert.Equal(11, x);
		}

		[Fact]
		public void Sequence_InlineClass_RepeatsLastAfterExhaustion()
		{
			var stub = new AbstractClassMethodVoidInlineTests.Stubs.AbstractClassMethodVoid();
			AbstractClassMethodVoid obj = stub.Object;

			var log = new List<string>();
			stub.NoParameters
				.Call(() => log.Add("first"))
				.ThenCall(() => log.Add("second"));

			obj.NoParameters(); // "first"
			obj.NoParameters(); // "second"
			obj.NoParameters(); // "second" (repeats last)

			Assert.Equal(3, log.Count);
			Assert.Equal("first", log[0]);
			Assert.Equal("second", log[1]);
			Assert.Equal("second", log[2]);
		}

		#endregion
	}
}
