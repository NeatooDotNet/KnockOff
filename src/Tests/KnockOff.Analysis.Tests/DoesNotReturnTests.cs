// ============================================================================
// DoesNotReturnTests: Edge case tests for class members marked with
// [DoesNotReturn] attribute.
// Inspired by Rocks.Analysis.IntegrationTests.DoesNotReturnTests
//
// Key behavior: When a method has [DoesNotReturn], the generated override
// must also have the attribute. The method's base implementation throws
// NotSupportedException. Configured callbacks can throw different exceptions.
// Unconfigured calls fall through to base, which throws NotSupportedException.
//
// Target class:
//   public class UsesDoesNotReturn
//   {
//       [DoesNotReturn]
//       public virtual void VoidMethod() => throw new NotSupportedException();
//       [DoesNotReturn]
//       public virtual int IntMethod() => throw new NotSupportedException();
//   }
//
// Scenarios:
// 1. Configured callback that throws — exception propagates
// 2. Unconfigured falls through to base — throws NotSupportedException
// 3. Verify tracks calls even when exception is thrown
//
// Applicable patterns (class only — virtual members):
// - Pattern 3 (Standalone Class): [KnockOffBase<UsesDoesNotReturn>]
// - Pattern 6 (Inline Class): [KnockOff<UsesDoesNotReturn>]
// ============================================================================

using System.Diagnostics.CodeAnalysis;
using KnockOff;

// ============================================================================
// TYPE DEFINITIONS
// ============================================================================

namespace KnockOff.Analysis.Tests.DoesNotReturnTestTypes
{
	public class UsesDoesNotReturn
	{
		[DoesNotReturn]
		public virtual void VoidMethod() => throw new NotSupportedException();

		[DoesNotReturn]
		public virtual int IntMethod() => throw new NotSupportedException();
	}

	// Pattern 3: Standalone class stub
	[KnockOffBase<UsesDoesNotReturn>]
	public partial class DoesNotReturnStandaloneKnockOff
	{
	}
}

// ============================================================================
// INLINE PATTERN DECLARATIONS + TESTS
// ============================================================================

namespace KnockOff.Analysis.Tests
{
	using KnockOff.Analysis.Tests.DoesNotReturnTestTypes;

	// Pattern 6: Inline class stub
	[KnockOff<UsesDoesNotReturn>]
	public partial class DoesNotReturnInlineTests
	{
	}

	public class DoesNotReturnTests
	{
		// ====================================================================
		// Scenario 1: Void method — configured callback throws
		// ====================================================================

		#region Pattern 3 (Standalone Class): Void method with callback

		[Fact]
		public void VoidMethod_StandaloneClass_ConfiguredCallbackThrows()
		{
			var stub = new DoesNotReturnStandaloneKnockOff();
			UsesDoesNotReturn obj = stub.Object;

			stub.VoidMethod.Call(() => throw new NotImplementedException());

			Assert.Throws<NotImplementedException>(() => obj.VoidMethod());
		}

		#endregion

		#region Pattern 6 (Inline Class): Void method with callback

		[Fact]
		public void VoidMethod_InlineClass_ConfiguredCallbackThrows()
		{
			var stub = new DoesNotReturnInlineTests.Stubs.UsesDoesNotReturn();
			UsesDoesNotReturn obj = stub.Object;

			stub.VoidMethod.Call(() => throw new NotImplementedException());

			Assert.Throws<NotImplementedException>(() => obj.VoidMethod());
		}

		#endregion

		// ====================================================================
		// Scenario 2: Void method — unconfigured falls to base (throws)
		// ====================================================================

		#region Pattern 3 (Standalone Class): Void method unconfigured

		[Fact]
		public void VoidMethod_StandaloneClass_UnconfiguredThrowsNotSupported()
		{
			var stub = new DoesNotReturnStandaloneKnockOff();
			UsesDoesNotReturn obj = stub.Object;

			// Unconfigured virtual falls through to base which throws
			Assert.Throws<NotSupportedException>(() => obj.VoidMethod());
		}

		#endregion

		#region Pattern 6 (Inline Class): Void method unconfigured

		[Fact]
		public void VoidMethod_InlineClass_UnconfiguredThrowsNotSupported()
		{
			var stub = new DoesNotReturnInlineTests.Stubs.UsesDoesNotReturn();
			UsesDoesNotReturn obj = stub.Object;

			Assert.Throws<NotSupportedException>(() => obj.VoidMethod());
		}

		#endregion

		// ====================================================================
		// Scenario 3: Int method — configured callback throws
		// ====================================================================

		#region Pattern 3 (Standalone Class): Int method with callback

		[Fact]
		public void IntMethod_StandaloneClass_ConfiguredCallbackThrows()
		{
			var stub = new DoesNotReturnStandaloneKnockOff();
			UsesDoesNotReturn obj = stub.Object;

			stub.IntMethod.Call(() =>
			{
				throw new NotImplementedException();
#pragma warning disable CS0162 // Unreachable code detected
				return 0;
#pragma warning restore CS0162
			});

			Assert.Throws<NotImplementedException>(() => obj.IntMethod());
		}

		#endregion

		#region Pattern 6 (Inline Class): Int method with callback

		[Fact]
		public void IntMethod_InlineClass_ConfiguredCallbackThrows()
		{
			var stub = new DoesNotReturnInlineTests.Stubs.UsesDoesNotReturn();
			UsesDoesNotReturn obj = stub.Object;

			stub.IntMethod.Call(() =>
			{
				throw new NotImplementedException();
#pragma warning disable CS0162 // Unreachable code detected
				return 0;
#pragma warning restore CS0162
			});

			Assert.Throws<NotImplementedException>(() => obj.IntMethod());
		}

		#endregion

		// ====================================================================
		// Scenario 4: Int method — unconfigured falls to base (throws)
		// ====================================================================

		#region Pattern 3 (Standalone Class): Int method unconfigured

		[Fact]
		public void IntMethod_StandaloneClass_UnconfiguredThrowsNotSupported()
		{
			var stub = new DoesNotReturnStandaloneKnockOff();
			UsesDoesNotReturn obj = stub.Object;

			Assert.Throws<NotSupportedException>(() => obj.IntMethod());
		}

		#endregion

		#region Pattern 6 (Inline Class): Int method unconfigured

		[Fact]
		public void IntMethod_InlineClass_UnconfiguredThrowsNotSupported()
		{
			var stub = new DoesNotReturnInlineTests.Stubs.UsesDoesNotReturn();
			UsesDoesNotReturn obj = stub.Object;

			Assert.Throws<NotSupportedException>(() => obj.IntMethod());
		}

		#endregion

		// ====================================================================
		// Scenario 5: Verify tracks call even when exception thrown
		// ====================================================================

		#region Pattern 3 (Standalone Class): Verify after exception

		[Fact]
		public void VoidMethod_StandaloneClass_VerifyTracksCallOnException()
		{
			var stub = new DoesNotReturnStandaloneKnockOff();
			UsesDoesNotReturn obj = stub.Object;

			stub.VoidMethod.Call(() => throw new NotImplementedException());

			try { obj.VoidMethod(); }
			catch (NotImplementedException) { }

			stub.VoidMethod.Verify(Called.Once);
		}

		#endregion

		#region Pattern 6 (Inline Class): Verify after exception

		[Fact]
		public void VoidMethod_InlineClass_VerifyTracksCallOnException()
		{
			var stub = new DoesNotReturnInlineTests.Stubs.UsesDoesNotReturn();
			UsesDoesNotReturn obj = stub.Object;

			stub.VoidMethod.Call(() => throw new NotImplementedException());

			try { obj.VoidMethod(); }
			catch (NotImplementedException) { }

			stub.VoidMethod.Verify(Called.Once);
		}

		#endregion
	}
}
