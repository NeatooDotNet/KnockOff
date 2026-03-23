// ============================================================================
// RecordTests: Edge case tests for record types
// Inspired by Rocks.Analysis.IntegrationTests.RecordTests
//
// Records are classes with value semantics. The compiler synthesizes members
// (Equals, GetHashCode, ToString, PrintMembers, EqualityContract, <Clone>$).
// The generator must:
// 1. Emit 'sealed record' (not 'sealed class') for the Impl type
// 2. Filter out record-synthesized members so they are inherited as-is
// 3. Only intercept user-declared virtual members (e.g., Foo())
//
// These tests verify that record-synthesized behavior (equality, ToString,
// cloning via 'with') survives stubbing. Generic interceptor behavior
// (Verify, Call, unconfigured fallthrough) is tested elsewhere.
//
// Applicable patterns (class only -- records are classes):
// - Pattern 3: [KnockOffBase<MyRecord>] (Standalone Class)
// - Pattern 6: [KnockOff<MyRecord>] (Inline Class)
// ============================================================================

using KnockOff;

// ============================================================================
// TYPE DEFINITIONS
// ============================================================================

namespace KnockOff.Analysis.Tests.RecordTestTypes
{
	public record MyRecord
	{
		public virtual void Foo() { }
	}

	// Pattern 3: Standalone class stub
	[KnockOffBase<MyRecord>]
	public partial class MyRecordStandaloneKnockOff
	{
	}
}

// ============================================================================
// INLINE PATTERN DECLARATIONS + TESTS
// ============================================================================

namespace KnockOff.Analysis.Tests
{
	using KnockOff.Analysis.Tests.RecordTestTypes;

	// Pattern 6: Inline class stub
	[KnockOff<MyRecord>]
	public partial class RecordInlineTests
	{
	}

	public class RecordTests
	{
		// ====================================================================
		// Pattern 3 (Standalone Class): Record with virtual method
		// ====================================================================

		#region Standalone Class: Record virtual method

		[Fact]
		public void Record_StandaloneClass_ConfiguredMethodReturnsNothing()
		{
			// Foo() is void -- configure a callback to confirm it runs
			var stub = new MyRecordStandaloneKnockOff();
			MyRecord obj = stub.Object;

			bool called = false;
			stub.Foo.Call(() => called = true);

			obj.Foo();

			Assert.True(called);
		}

		#endregion

		// ====================================================================
		// Pattern 3 (Standalone Class): Record-synthesized behavior
		// ====================================================================

		#region Standalone Class: Record-synthesized behavior

		[Fact]
		public void Record_StandaloneClass_SelfEqualityWorks()
		{
			// Records override Equals for value semantics.
			// The generated Impl is a 'sealed record' so Equals should work.
			var stub = new MyRecordStandaloneKnockOff();
			MyRecord obj = stub.Object;

			Assert.Equal(obj, obj);
		}

		[Fact]
		public void Record_StandaloneClass_GetHashCodeIsConsistent()
		{
			// Record GetHashCode should be consistent across calls
			var stub = new MyRecordStandaloneKnockOff();
			MyRecord obj = stub.Object;

			Assert.Equal(obj.GetHashCode(), obj.GetHashCode());
		}

		[Fact]
		public void Record_StandaloneClass_ToStringProducesRecordRepresentation()
		{
			// Records synthesize ToString() via PrintMembers.
			// The Impl type should produce the record-style string representation.
			var stub = new MyRecordStandaloneKnockOff();
			MyRecord obj = stub.Object;

			string str = obj.ToString();

			Assert.NotNull(str);
			// Record ToString() produces "TypeName { Prop = Value, ... }" format
			// At minimum it should not throw and should contain the type name
			Assert.Contains("{", str);
		}

		[Fact]
		public void Record_StandaloneClass_WithExpressionClonesObject()
		{
			// Records support 'with' expressions for non-destructive mutation.
			// The generated Impl is a 'sealed record' so 'with' should work.
			var stub = new MyRecordStandaloneKnockOff();
			MyRecord obj = stub.Object;

			var clone = obj with { };

			Assert.NotNull(clone);
			Assert.Equal(obj, clone);
			Assert.NotSame(obj, clone);
		}

		#endregion

		// ====================================================================
		// Pattern 6 (Inline Class): Record with virtual method
		// ====================================================================

		#region Inline Class: Record virtual method

		[Fact]
		public void Record_InlineClass_ConfiguredMethodReturnsNothing()
		{
			var stub = new RecordInlineTests.Stubs.MyRecord();
			MyRecord obj = stub.Object;

			bool called = false;
			stub.Foo.Call(() => called = true);

			obj.Foo();

			Assert.True(called);
		}

		#endregion

		// ====================================================================
		// Pattern 6 (Inline Class): Record-synthesized behavior
		// ====================================================================

		#region Inline Class: Record-synthesized behavior

		[Fact]
		public void Record_InlineClass_SelfEqualityWorks()
		{
			var stub = new RecordInlineTests.Stubs.MyRecord();
			MyRecord obj = stub.Object;

			Assert.Equal(obj, obj);
		}

		[Fact]
		public void Record_InlineClass_GetHashCodeIsConsistent()
		{
			var stub = new RecordInlineTests.Stubs.MyRecord();
			MyRecord obj = stub.Object;

			Assert.Equal(obj.GetHashCode(), obj.GetHashCode());
		}

		[Fact]
		public void Record_InlineClass_ToStringProducesRecordRepresentation()
		{
			var stub = new RecordInlineTests.Stubs.MyRecord();
			MyRecord obj = stub.Object;

			string str = obj.ToString();

			Assert.NotNull(str);
			Assert.Contains("{", str);
		}

		[Fact]
		public void Record_InlineClass_WithExpressionClonesObject()
		{
			var stub = new RecordInlineTests.Stubs.MyRecord();
			MyRecord obj = stub.Object;

			var clone = obj with { };

			Assert.NotNull(clone);
			Assert.Equal(obj, clone);
			Assert.NotSame(obj, clone);
		}

		#endregion
	}
}
