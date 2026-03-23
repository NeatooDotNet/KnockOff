#define NEVER

// ============================================================================
// AttributeTests: Edge case tests for various code analysis attributes
// Inspired by Rocks.Analysis.IntegrationTests.AttributeTests
//
// Tests four categories of attribute edge cases:
// 1. [MemberNotNullWhen] on an abstract class virtual property — the stub
//    should compile and the property should be configurable/verifiable.
// 2. [UnscopedRef] on an interface property returning ReadOnlySpan<nint> —
//    ReadOnlySpan is a ref struct, requiring inline interceptor rendering.
// 3. [NotNullIfNotNull] on a class virtual method return — the stub should
//    compile and the method should accept null/non-null inputs.
// 4. [Conditional("NEVER")] on a class virtual void method — the method
//    must be callable and trackable (requires #define NEVER at file top).
//
// Applicable patterns:
// - [MemberNotNullWhen] (abstract class): Patterns 3, 6
// - [UnscopedRef] (interface): Patterns 1, 5
// - [NotNullIfNotNull] (class): Patterns 3, 6
// - [Conditional] (class): Patterns 3, 6
// ============================================================================

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using KnockOff;

// ============================================================================
// TYPE DEFINITIONS
// ============================================================================

namespace KnockOff.Analysis.Tests.AttributeTestTypes
{
	// Edge Case 1: [MemberNotNullWhen] on virtual property of abstract class
	public abstract class UsingMemberNotNullWhen
	{
		protected string? Connection { get; set; }

		[MemberNotNullWhen(true, nameof(Connection))]
		public virtual bool IsConnectionProvided => this.Connection is not null;
	}

	// Edge Case 2: [UnscopedRef] on interface property returning ReadOnlySpan
	public interface ITense
	{
		[UnscopedRef]
		ReadOnlySpan<nint> Lengths { get; }
	}

	// Edge Case 3: [NotNullIfNotNull] on class virtual method
	public class NotNullIfNotCases
	{
		[return: NotNullIfNotNull(nameof(node))]
		public virtual object? VisitMethod(object? node) => default;
	}

	// Edge Case 4: [Conditional("NEVER")] on class virtual void method
	// Note: requires #define NEVER at the top of any file that calls this method,
	// otherwise the compiler elides the call entirely.
	public class ConventionDispatcher
	{
		[Conditional("NEVER")]
		public virtual void AssertNoScope() { }
	}

	// Pattern 3: Standalone class stubs
	[KnockOffBase<UsingMemberNotNullWhen>]
	public partial class MemberNotNullWhenStandaloneKnockOff
	{
	}

	[KnockOffBase<NotNullIfNotCases>]
	public partial class NotNullIfNotStandaloneKnockOff
	{
	}

	[KnockOffBase<ConventionDispatcher>]
	public partial class ConditionalStandaloneKnockOff
	{
	}

	// Pattern 1: Standalone interface stub
	[KnockOff]
	public partial class UnscopedRefStandaloneKnockOff : ITense
	{
	}
}

// ============================================================================
// INLINE PATTERN DECLARATIONS + TESTS
// ============================================================================

namespace KnockOff.Analysis.Tests
{
	using KnockOff.Analysis.Tests.AttributeTestTypes;

	// Pattern 5: Inline interface stub
	// Pattern 6: Inline class stubs
	[KnockOff<ITense>]
	[KnockOff<UsingMemberNotNullWhen>]
	[KnockOff<NotNullIfNotCases>]
	[KnockOff<ConventionDispatcher>]
	public partial class AttributeInlineTests
	{
	}

	public class AttributeTests
	{
		// ====================================================================
		// Edge Case 1: [MemberNotNullWhen] on abstract class virtual property
		// The stub should compile and the property should be configurable.
		// ====================================================================

		#region Standalone Class: MemberNotNullWhen

		[Fact]
		public void MemberNotNullWhen_StandaloneClass_UnconfiguredFallsToBase()
		{
			var stub = new MemberNotNullWhenStandaloneKnockOff();
			UsingMemberNotNullWhen obj = stub.Object;

			// base.IsConnectionProvided checks this.Connection is not null.
			// Connection defaults to null, so base returns false.
			bool result = obj.IsConnectionProvided;

			Assert.False(result);
		}

		[Fact]
		public void MemberNotNullWhen_StandaloneClass_ConfiguredReturnsTrue()
		{
			var stub = new MemberNotNullWhenStandaloneKnockOff();
			UsingMemberNotNullWhen obj = stub.Object;

			stub.IsConnectionProvided.Get(true);

			bool result = obj.IsConnectionProvided;

			Assert.True(result);
		}

		[Fact]
		public void MemberNotNullWhen_StandaloneClass_VerifyTracksAccess()
		{
			var stub = new MemberNotNullWhenStandaloneKnockOff();
			UsingMemberNotNullWhen obj = stub.Object;

			stub.IsConnectionProvided.Get(true);
			_ = obj.IsConnectionProvided;
			_ = obj.IsConnectionProvided;

			stub.IsConnectionProvided.VerifyGet(Called.Exactly(2));
		}

		#endregion

		#region Inline Class: MemberNotNullWhen

		[Fact]
		public void MemberNotNullWhen_InlineClass_UnconfiguredFallsToBase()
		{
			var stub = new AttributeInlineTests.Stubs.UsingMemberNotNullWhen();
			UsingMemberNotNullWhen obj = stub.Object;

			bool result = obj.IsConnectionProvided;

			Assert.False(result);
		}

		[Fact]
		public void MemberNotNullWhen_InlineClass_ConfiguredReturnsTrue()
		{
			var stub = new AttributeInlineTests.Stubs.UsingMemberNotNullWhen();
			UsingMemberNotNullWhen obj = stub.Object;

			stub.IsConnectionProvided.Get(true);

			bool result = obj.IsConnectionProvided;

			Assert.True(result);
		}

		[Fact]
		public void MemberNotNullWhen_InlineClass_VerifyTracksAccess()
		{
			var stub = new AttributeInlineTests.Stubs.UsingMemberNotNullWhen();
			UsingMemberNotNullWhen obj = stub.Object;

			stub.IsConnectionProvided.Get(false);
			_ = obj.IsConnectionProvided;

			stub.IsConnectionProvided.VerifyGet(Called.Once);
		}

		#endregion

		// ====================================================================
		// Edge Case 2: [UnscopedRef] on interface ReadOnlySpan<nint> property
		// ReadOnlySpan is a ref struct — requires inline interceptor rendering.
		// ====================================================================

		#region Standalone Interface: UnscopedRef ReadOnlySpan property

		[Fact]
		public void UnscopedRef_Standalone_UnconfiguredReturnsEmpty()
		{
			var stub = new UnscopedRefStandaloneKnockOff();
			ITense svc = stub;

			ReadOnlySpan<nint> result = svc.Lengths;

			Assert.True(result.IsEmpty);
		}

		[Fact]
		public void UnscopedRef_Standalone_ConfiguredCallbackReturnsSpan()
		{
			var stub = new UnscopedRefStandaloneKnockOff();
			ITense svc = stub;

			var backing = new nint[] { 1, 2, 3 };
			stub.Lengths.Get(() => backing.AsSpan());

			ReadOnlySpan<nint> result = svc.Lengths;

			Assert.Equal(3, result.Length);
			Assert.Equal((nint)1, result[0]);
			Assert.Equal((nint)2, result[1]);
			Assert.Equal((nint)3, result[2]);
		}

		[Fact]
		public void UnscopedRef_Standalone_VerifyTracksAccess()
		{
			var stub = new UnscopedRefStandaloneKnockOff();
			ITense svc = stub;

			_ = svc.Lengths;
			_ = svc.Lengths;

			stub.Lengths.VerifyGet(Called.Exactly(2));
		}

		#endregion

		#region Inline Interface: UnscopedRef ReadOnlySpan property

		[Fact]
		public void UnscopedRef_Inline_UnconfiguredReturnsEmpty()
		{
			var stub = new AttributeInlineTests.Stubs.ITense();
			ITense svc = stub;

			ReadOnlySpan<nint> result = svc.Lengths;

			Assert.True(result.IsEmpty);
		}

		[Fact]
		public void UnscopedRef_Inline_ConfiguredCallbackReturnsSpan()
		{
			var stub = new AttributeInlineTests.Stubs.ITense();
			ITense svc = stub;

			var backing = new nint[] { 10, 20 };
			stub.Lengths.Get(() => backing.AsSpan());

			ReadOnlySpan<nint> result = svc.Lengths;

			Assert.Equal(2, result.Length);
			Assert.Equal((nint)10, result[0]);
		}

		[Fact]
		public void UnscopedRef_Inline_VerifyTracksAccess()
		{
			var stub = new AttributeInlineTests.Stubs.ITense();
			ITense svc = stub;

			_ = svc.Lengths;

			stub.Lengths.VerifyGet(Called.Once);
		}

		#endregion

		// ====================================================================
		// Edge Case 3: [NotNullIfNotNull] on class virtual method
		// The stub should compile and the method should handle null/non-null.
		// ====================================================================

		#region Standalone Class: NotNullIfNotNull

		[Fact]
		public void NotNullIfNotNull_StandaloneClass_UnconfiguredFallsToBase()
		{
			var stub = new NotNullIfNotStandaloneKnockOff();
			NotNullIfNotCases obj = stub.Object;

			// base.VisitMethod(node) => default (null)
			var result = obj.VisitMethod(new object());

			Assert.Null(result);
		}

		[Fact]
		public void NotNullIfNotNull_StandaloneClass_ConfiguredReturnValue()
		{
			var stub = new NotNullIfNotStandaloneKnockOff();
			NotNullIfNotCases obj = stub.Object;

			var expected = new object();
			stub.VisitMethod.Return(expected);

			var result = obj.VisitMethod(new object());

			Assert.Same(expected, result);
		}

		[Fact]
		public void NotNullIfNotNull_StandaloneClass_CallbackTransforms()
		{
			var stub = new NotNullIfNotStandaloneKnockOff();
			NotNullIfNotCases obj = stub.Object;

			// Callback mirrors the [NotNullIfNotNull] contract:
			// non-null input -> non-null output
			stub.VisitMethod.Call((object? node) => node ?? new object());

			var input = new object();
			var result = obj.VisitMethod(input);

			Assert.Same(input, result);
		}

		[Fact]
		public void NotNullIfNotNull_StandaloneClass_NullInputReturnsNull()
		{
			var stub = new NotNullIfNotStandaloneKnockOff();
			NotNullIfNotCases obj = stub.Object;

			// Callback returns null when input is null
			stub.VisitMethod.Call((object? node) => node);

			var result = obj.VisitMethod(null);

			Assert.Null(result);
		}

		[Fact]
		public void NotNullIfNotNull_StandaloneClass_VerifyTracksCalls()
		{
			var stub = new NotNullIfNotStandaloneKnockOff();
			NotNullIfNotCases obj = stub.Object;

			obj.VisitMethod(null);
			obj.VisitMethod(new object());

			stub.VisitMethod.Verify(Called.Exactly(2));
		}

		#endregion

		#region Inline Class: NotNullIfNotNull

		[Fact]
		public void NotNullIfNotNull_InlineClass_UnconfiguredFallsToBase()
		{
			var stub = new AttributeInlineTests.Stubs.NotNullIfNotCases();
			NotNullIfNotCases obj = stub.Object;

			var result = obj.VisitMethod(new object());

			Assert.Null(result);
		}

		[Fact]
		public void NotNullIfNotNull_InlineClass_ConfiguredReturnValue()
		{
			var stub = new AttributeInlineTests.Stubs.NotNullIfNotCases();
			NotNullIfNotCases obj = stub.Object;

			var expected = new object();
			stub.VisitMethod.Return(expected);

			var result = obj.VisitMethod(new object());

			Assert.Same(expected, result);
		}

		[Fact]
		public void NotNullIfNotNull_InlineClass_CallbackTransforms()
		{
			var stub = new AttributeInlineTests.Stubs.NotNullIfNotCases();
			NotNullIfNotCases obj = stub.Object;

			stub.VisitMethod.Call((object? node) => node ?? new object());

			var input = new object();
			var result = obj.VisitMethod(input);

			Assert.Same(input, result);
		}

		[Fact]
		public void NotNullIfNotNull_InlineClass_VerifyTracksCalls()
		{
			var stub = new AttributeInlineTests.Stubs.NotNullIfNotCases();
			NotNullIfNotCases obj = stub.Object;

			obj.VisitMethod(null);
			obj.VisitMethod(new object());

			stub.VisitMethod.Verify(Called.Exactly(2));
		}

		#endregion

		// ====================================================================
		// Edge Case 4: [Conditional("NEVER")] on class virtual void method
		// #define NEVER at file top ensures calls are not elided by the compiler.
		// The method must be callable and call tracking must work.
		// ====================================================================

		#region Standalone Class: Conditional

		[Fact]
		public void Conditional_StandaloneClass_CanCallMethod()
		{
			var stub = new ConditionalStandaloneKnockOff();
			ConventionDispatcher obj = stub.Object;

			// With #define NEVER, the call is not elided
			obj.AssertNoScope();

			stub.AssertNoScope.Verify(Called.Once);
		}

		[Fact]
		public void Conditional_StandaloneClass_CallbackIsInvoked()
		{
			var stub = new ConditionalStandaloneKnockOff();
			ConventionDispatcher obj = stub.Object;

			var callbackInvoked = false;
			stub.AssertNoScope.Call(() => callbackInvoked = true);

			obj.AssertNoScope();

			Assert.True(callbackInvoked);
		}

		[Fact]
		public void Conditional_StandaloneClass_VerifyMultipleCalls()
		{
			var stub = new ConditionalStandaloneKnockOff();
			ConventionDispatcher obj = stub.Object;

			obj.AssertNoScope();
			obj.AssertNoScope();
			obj.AssertNoScope();

			stub.AssertNoScope.Verify(Called.Exactly(3));
		}

		#endregion

		#region Inline Class: Conditional

		[Fact]
		public void Conditional_InlineClass_CanCallMethod()
		{
			var stub = new AttributeInlineTests.Stubs.ConventionDispatcher();
			ConventionDispatcher obj = stub.Object;

			obj.AssertNoScope();

			stub.AssertNoScope.Verify(Called.Once);
		}

		[Fact]
		public void Conditional_InlineClass_CallbackIsInvoked()
		{
			var stub = new AttributeInlineTests.Stubs.ConventionDispatcher();
			ConventionDispatcher obj = stub.Object;

			var callbackInvoked = false;
			stub.AssertNoScope.Call(() => callbackInvoked = true);

			obj.AssertNoScope();

			Assert.True(callbackInvoked);
		}

		[Fact]
		public void Conditional_InlineClass_VerifyMultipleCalls()
		{
			var stub = new AttributeInlineTests.Stubs.ConventionDispatcher();
			ConventionDispatcher obj = stub.Object;

			obj.AssertNoScope();
			obj.AssertNoScope();

			stub.AssertNoScope.Verify(Called.Exactly(2));
		}

		#endregion
	}
}
