// ============================================================================
// NonPublicMemberTests: Edge case tests for abstract classes with protected
// abstract members.
// Inspired by Rocks.Analysis.IntegrationTests.NonPublicMemberTests
//
// Key behavior: Class stubs must override protected abstract members and
// wire them through interceptors. Calling a public method that internally
// invokes the protected abstract method should trigger the interceptor.
//
// Target class:
//   public abstract class HasProtectedMember
//   {
//       protected HasProtectedMember() { }
//       public void CallsDoSomething() => this.DoSomething(3);
//       protected abstract void DoSomething(int data);
//   }
//
// Scenarios:
// 1. Calling CallsDoSomething() invokes the overridden DoSomething(3)
// 2. Callback captures the argument (3) passed by CallsDoSomething()
// 3. Verify tracks the call
// 4. LastArgs captures the argument
//
// Applicable patterns (class only — protected members):
// - Pattern 3 (Standalone Class): [KnockOffBase<HasProtectedMember>]
// - Pattern 6 (Inline Class): [KnockOff<HasProtectedMember>]
// ============================================================================

using KnockOff;

// ============================================================================
// TYPE DEFINITIONS
// ============================================================================

namespace KnockOff.Analysis.Tests.NonPublicMemberTestTypes
{
	public abstract class HasProtectedMember
	{
		protected HasProtectedMember() { }

		public void CallsDoSomething() => this.DoSomething(3);

		protected abstract void DoSomething(int data);
	}

	// Pattern 3: Standalone class stub
	[KnockOffBase<HasProtectedMember>]
	public partial class HasProtectedMemberStandaloneKnockOff
	{
	}
}

// ============================================================================
// INLINE PATTERN DECLARATIONS + TESTS
// ============================================================================

namespace KnockOff.Analysis.Tests
{
	using KnockOff.Analysis.Tests.NonPublicMemberTestTypes;

	// Pattern 6: Inline class stub
	[KnockOff<HasProtectedMember>]
	public partial class NonPublicMemberInlineTests
	{
	}

	public class NonPublicMemberTests
	{
		// ====================================================================
		// Scenario 1: Callback captures protected method argument
		// ====================================================================

		#region Pattern 3 (Standalone Class): Callback on protected member

		[Fact]
		public void ProtectedCallback_StandaloneClass_CapturesArgument()
		{
			var stub = new HasProtectedMemberStandaloneKnockOff();
			HasProtectedMember obj = stub.Object;

			int capturedData = 0;
			stub.DoSomething.Call((int data) => capturedData = data);

			obj.CallsDoSomething();

			Assert.Equal(3, capturedData);
		}

		#endregion

		#region Pattern 6 (Inline Class): Callback on protected member

		[Fact]
		public void ProtectedCallback_InlineClass_CapturesArgument()
		{
			var stub = new NonPublicMemberInlineTests.Stubs.HasProtectedMember();
			HasProtectedMember obj = stub.Object;

			int capturedData = 0;
			stub.DoSomething.Call((int data) => capturedData = data);

			obj.CallsDoSomething();

			Assert.Equal(3, capturedData);
		}

		#endregion

		// ====================================================================
		// Scenario 2: Verify call tracking on protected member
		// ====================================================================

		#region Pattern 3 (Standalone Class): Verify protected member

		[Fact]
		public void ProtectedVerify_StandaloneClass_TracksCall()
		{
			var stub = new HasProtectedMemberStandaloneKnockOff();
			HasProtectedMember obj = stub.Object;

			obj.CallsDoSomething();

			stub.DoSomething.Verify(Called.Once);
		}

		#endregion

		#region Pattern 6 (Inline Class): Verify protected member

		[Fact]
		public void ProtectedVerify_InlineClass_TracksCall()
		{
			var stub = new NonPublicMemberInlineTests.Stubs.HasProtectedMember();
			HasProtectedMember obj = stub.Object;

			obj.CallsDoSomething();

			stub.DoSomething.Verify(Called.Once);
		}

		#endregion

		// ====================================================================
		// Scenario 3: LastArg captures argument from protected member
		// (single parameter uses LastArg, not LastArgs)
		// ====================================================================

		#region Pattern 3 (Standalone Class): LastArg on protected member

		[Fact]
		public void ProtectedLastArg_StandaloneClass_CapturesValue()
		{
			var stub = new HasProtectedMemberStandaloneKnockOff();
			HasProtectedMember obj = stub.Object;

			obj.CallsDoSomething();

			Assert.Equal(3, stub.DoSomething.LastArg);
		}

		#endregion

		#region Pattern 6 (Inline Class): LastArg on protected member

		[Fact]
		public void ProtectedLastArg_InlineClass_CapturesValue()
		{
			var stub = new NonPublicMemberInlineTests.Stubs.HasProtectedMember();
			HasProtectedMember obj = stub.Object;

			obj.CallsDoSomething();

			Assert.Equal(3, stub.DoSomething.LastArg);
		}

		#endregion

		// ====================================================================
		// Scenario 4: Unconfigured protected abstract does not throw
		// ====================================================================

		#region Pattern 3 (Standalone Class): Unconfigured does not throw

		[Fact]
		public void ProtectedUnconfigured_StandaloneClass_DoesNotThrow()
		{
			var stub = new HasProtectedMemberStandaloneKnockOff();
			HasProtectedMember obj = stub.Object;

			// Abstract void method with no configuration should not throw
			obj.CallsDoSomething();

			stub.DoSomething.Verify(Called.Once);
		}

		#endregion

		#region Pattern 6 (Inline Class): Unconfigured does not throw

		[Fact]
		public void ProtectedUnconfigured_InlineClass_DoesNotThrow()
		{
			var stub = new NonPublicMemberInlineTests.Stubs.HasProtectedMember();
			HasProtectedMember obj = stub.Object;

			obj.CallsDoSomething();

			stub.DoSomething.Verify(Called.Once);
		}

		#endregion
	}
}
