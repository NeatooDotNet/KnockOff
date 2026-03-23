// ============================================================================
// ClassMethodVoidTests: Edge case tests for a class with virtual void methods,
// varying parameter counts.
// Inspired by Rocks.Analysis.IntegrationTests.ClassMethodVoidTests
//
// Target class:
//   public class ClassMethodVoid
//   {
//       public virtual void NoParameters() { }
//       public virtual void OneParameter(int a) { }
//       public virtual void MultipleParameters(int a, string b) { }
//   }
//
// Tests exercise the CLASS pipeline (not interface) for:
// 1. Callback invocation — Call(() => { flag = true; }), call, assert flag
// 2. One-param callback captures arg — Call((int a) => { captured = a; })
// 3. Multi-param callback captures args — callback captures both args
// 4. Unconfigured fallback to base — no config, verify it doesn't throw
// 5. LastArgs tracking — call with args, verify LastArgs
// 6. Verify call count — call multiple times, verify exact count
// 7. Sequence — Call(() => x++).ThenCall(() => x += 10), verify x values
//
// Applicable patterns (class only):
// - Pattern 3 (Standalone Class): [KnockOffBase<ClassMethodVoid>]
// - Pattern 6 (Inline Class): [KnockOff<ClassMethodVoid>]
// ============================================================================

using KnockOff;

// ============================================================================
// TYPE DEFINITIONS
// ============================================================================

namespace KnockOff.Analysis.Tests.ClassMethodVoidTestTypes
{
	public class ClassMethodVoid
	{
		public virtual void NoParameters() { }
		public virtual void OneParameter(int a) { }
		public virtual void MultipleParameters(int a, string b) { }
	}

	// Pattern 3: Standalone class stub
	[KnockOffBase<ClassMethodVoid>]
	public partial class ClassMethodVoidStandaloneKnockOff
	{
	}
}

// ============================================================================
// INLINE PATTERN DECLARATIONS + TESTS
// ============================================================================

namespace KnockOff.Analysis.Tests
{
	using KnockOff.Analysis.Tests.ClassMethodVoidTestTypes;

	// Pattern 6: Inline class stub
	[KnockOff<ClassMethodVoid>]
	public partial class ClassMethodVoidInlineTests
	{
	}

	public class ClassMethodVoidTests
	{
		// ====================================================================
		// Scenario 1: Callback invocation — Call(() => { flag = true; })
		// ====================================================================

		#region Standalone Class: Callback invocation (NoParameters)

		[Fact]
		public void Callback_StandaloneClass_InvokesCallback()
		{
			var stub = new ClassMethodVoidStandaloneKnockOff();
			ClassMethodVoid obj = stub.Object;

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
			var stub = new ClassMethodVoidInlineTests.Stubs.ClassMethodVoid();
			ClassMethodVoid obj = stub.Object;

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
			var stub = new ClassMethodVoidStandaloneKnockOff();
			ClassMethodVoid obj = stub.Object;

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
			var stub = new ClassMethodVoidInlineTests.Stubs.ClassMethodVoid();
			ClassMethodVoid obj = stub.Object;

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
			var stub = new ClassMethodVoidStandaloneKnockOff();
			ClassMethodVoid obj = stub.Object;

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
			var stub = new ClassMethodVoidInlineTests.Stubs.ClassMethodVoid();
			ClassMethodVoid obj = stub.Object;

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
		// Scenario 4: Unconfigured fallback to base
		// Virtual void methods have empty bodies, so calling without config
		// should simply not throw.
		// ====================================================================

		#region Standalone Class: Unconfigured fallback to base

		[Fact]
		public void Unconfigured_StandaloneClass_FallsToBaseWithoutThrowing()
		{
			var stub = new ClassMethodVoidStandaloneKnockOff();
			ClassMethodVoid obj = stub.Object;

			// No configuration — falls through to base.NoParameters() which is empty
			var exception = Record.Exception(() => obj.NoParameters());

			Assert.Null(exception);
		}

		[Fact]
		public void Unconfigured_StandaloneClass_OneParamFallsToBase()
		{
			var stub = new ClassMethodVoidStandaloneKnockOff();
			ClassMethodVoid obj = stub.Object;

			var exception = Record.Exception(() => obj.OneParameter(99));

			Assert.Null(exception);
		}

		[Fact]
		public void Unconfigured_StandaloneClass_MultipleParamsFallsToBase()
		{
			var stub = new ClassMethodVoidStandaloneKnockOff();
			ClassMethodVoid obj = stub.Object;

			var exception = Record.Exception(() => obj.MultipleParameters(1, "test"));

			Assert.Null(exception);
		}

		#endregion

		#region Inline Class: Unconfigured fallback to base

		[Fact]
		public void Unconfigured_InlineClass_FallsToBaseWithoutThrowing()
		{
			var stub = new ClassMethodVoidInlineTests.Stubs.ClassMethodVoid();
			ClassMethodVoid obj = stub.Object;

			var exception = Record.Exception(() => obj.NoParameters());

			Assert.Null(exception);
		}

		[Fact]
		public void Unconfigured_InlineClass_OneParamFallsToBase()
		{
			var stub = new ClassMethodVoidInlineTests.Stubs.ClassMethodVoid();
			ClassMethodVoid obj = stub.Object;

			var exception = Record.Exception(() => obj.OneParameter(99));

			Assert.Null(exception);
		}

		[Fact]
		public void Unconfigured_InlineClass_MultipleParamsFallsToBase()
		{
			var stub = new ClassMethodVoidInlineTests.Stubs.ClassMethodVoid();
			ClassMethodVoid obj = stub.Object;

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
			var stub = new ClassMethodVoidStandaloneKnockOff();
			ClassMethodVoid obj = stub.Object;

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
			var stub = new ClassMethodVoidStandaloneKnockOff();
			ClassMethodVoid obj = stub.Object;

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
			var stub = new ClassMethodVoidInlineTests.Stubs.ClassMethodVoid();
			ClassMethodVoid obj = stub.Object;

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
			var stub = new ClassMethodVoidInlineTests.Stubs.ClassMethodVoid();
			ClassMethodVoid obj = stub.Object;

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
			var stub = new ClassMethodVoidStandaloneKnockOff();
			ClassMethodVoid obj = stub.Object;

			obj.NoParameters();
			obj.NoParameters();
			obj.NoParameters();

			stub.NoParameters.Verify(Called.Exactly(3));
		}

		[Fact]
		public void VerifyCount_StandaloneClass_MultipleParametersTracked()
		{
			var stub = new ClassMethodVoidStandaloneKnockOff();
			ClassMethodVoid obj = stub.Object;

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
			var stub = new ClassMethodVoidInlineTests.Stubs.ClassMethodVoid();
			ClassMethodVoid obj = stub.Object;

			obj.NoParameters();
			obj.NoParameters();
			obj.NoParameters();

			stub.NoParameters.Verify(Called.Exactly(3));
		}

		[Fact]
		public void VerifyCount_InlineClass_MultipleParametersTracked()
		{
			var stub = new ClassMethodVoidInlineTests.Stubs.ClassMethodVoid();
			ClassMethodVoid obj = stub.Object;

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
			var stub = new ClassMethodVoidStandaloneKnockOff();
			ClassMethodVoid obj = stub.Object;

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
			var stub = new ClassMethodVoidStandaloneKnockOff();
			ClassMethodVoid obj = stub.Object;

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
			var stub = new ClassMethodVoidInlineTests.Stubs.ClassMethodVoid();
			ClassMethodVoid obj = stub.Object;

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
			var stub = new ClassMethodVoidInlineTests.Stubs.ClassMethodVoid();
			ClassMethodVoid obj = stub.Object;

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
