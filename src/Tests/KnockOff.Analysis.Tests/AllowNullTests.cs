// ============================================================================
// AllowNullTests: Edge case tests for [AllowNull] properties
// Inspired by Rocks.Analysis.IntegrationTests.AllowNullTests
//
// Target types:
//   public interface IAllow
//   {
//       [AllowNull]
//       string NewLine { get; set; }
//   }
//   public class Allow
//   {
//       [AllowNull]
//       public virtual string NewLine { get; set; }
//   }
//
// [AllowNull] allows null assignment to a non-nullable string property.
// The generated stub must preserve this attribute so the compiler doesn't
// emit CS8625 when null is assigned.
//
// Tests exercise:
// 1. Set null to a non-nullable string property — should not warn/error
// 2. Get property after null set — should return null
// 3. Set property to non-null value — works normally
// 4. Verify tracks null assignment
//
// Applicable patterns:
// - Pattern 1 (Standalone Interface): [KnockOff] on partial class : IAllow
// - Pattern 3 (Standalone Class): [KnockOffBase<Allow>]
// - Pattern 5 (Inline Interface): [KnockOff<IAllow>]
// - Pattern 6 (Inline Class): [KnockOff<Allow>]
// ============================================================================

using System.Diagnostics.CodeAnalysis;
using KnockOff;

// ============================================================================
// TYPE DEFINITIONS
// ============================================================================

namespace KnockOff.Analysis.Tests.AllowNullTestTypes
{
	public interface IAllow
	{
		[AllowNull]
		string NewLine { get; set; }
	}

	public class Allow
	{
		[AllowNull]
		public virtual string NewLine { get; set; } = "";
	}

	// Pattern 1: Standalone interface stub
	[KnockOff]
	public partial class AllowNullStandaloneKnockOff : IAllow
	{
	}

	// Pattern 3: Standalone class stub
	[KnockOffBase<Allow>]
	public partial class AllowNullClassStandaloneKnockOff
	{
	}
}

// ============================================================================
// INLINE PATTERN DECLARATIONS + TESTS
// ============================================================================

namespace KnockOff.Analysis.Tests
{
	using KnockOff.Analysis.Tests.AllowNullTestTypes;

	// Pattern 5: Inline interface stub
	// Pattern 6: Inline class stub
	[KnockOff<IAllow>]
	[KnockOff<Allow>]
	public partial class AllowNullInlineTests
	{
	}

	public class AllowNullTests
	{
		// ====================================================================
		// Scenario 1: Set null to [AllowNull] non-nullable string property
		// ====================================================================

		#region Standalone Interface: Set null

		[Fact]
		public void SetNull_Standalone_DoesNotThrow()
		{
			var stub = new AllowNullStandaloneKnockOff();
			IAllow svc = stub;

			// [AllowNull] allows null assignment to non-nullable string
			svc.NewLine = null;

			stub.NewLine.VerifySet(Called.Once);
		}

		[Fact]
		public void SetNull_Standalone_LastSetValueIsNull()
		{
			var stub = new AllowNullStandaloneKnockOff();
			IAllow svc = stub;

			svc.NewLine = null;

			Assert.Null(stub.NewLine.LastSetValue);
		}

		#endregion

		#region Inline Interface: Set null

		[Fact]
		public void SetNull_Inline_DoesNotThrow()
		{
			var stub = new AllowNullInlineTests.Stubs.IAllow();
			IAllow svc = stub;

			svc.NewLine = null;

			stub.NewLine.VerifySet(Called.Once);
		}

		[Fact]
		public void SetNull_Inline_LastSetValueIsNull()
		{
			var stub = new AllowNullInlineTests.Stubs.IAllow();
			IAllow svc = stub;

			svc.NewLine = null;

			Assert.Null(stub.NewLine.LastSetValue);
		}

		#endregion

		#region Standalone Class: Set null

		[Fact]
		public void SetNull_StandaloneClass_DoesNotThrow()
		{
			var stub = new AllowNullClassStandaloneKnockOff();
			Allow obj = stub.Object;

			obj.NewLine = null;

			stub.NewLine.VerifySet(Called.Once);
		}

		[Fact]
		public void SetNull_StandaloneClass_LastSetValueIsNull()
		{
			var stub = new AllowNullClassStandaloneKnockOff();
			Allow obj = stub.Object;

			obj.NewLine = null;

			Assert.Null(stub.NewLine.LastSetValue);
		}

		#endregion

		#region Inline Class: Set null

		[Fact]
		public void SetNull_InlineClass_DoesNotThrow()
		{
			var stub = new AllowNullInlineTests.Stubs.Allow();
			Allow obj = stub.Object;

			obj.NewLine = null;

			stub.NewLine.VerifySet(Called.Once);
		}

		[Fact]
		public void SetNull_InlineClass_LastSetValueIsNull()
		{
			var stub = new AllowNullInlineTests.Stubs.Allow();
			Allow obj = stub.Object;

			obj.NewLine = null;

			Assert.Null(stub.NewLine.LastSetValue);
		}

		#endregion

		// ====================================================================
		// Scenario 2: Get property after setting null
		// ====================================================================

		#region Standalone Interface: Get after null set

		[Fact]
		public void GetAfterNullSet_Standalone_ReturnsConfiguredValue()
		{
			var stub = new AllowNullStandaloneKnockOff();
			IAllow svc = stub;

			// Configure getter to return null (via callback since it's non-nullable return type)
			stub.NewLine.Get(() => null!);

			string result = svc.NewLine;

			Assert.Null(result);
		}

		#endregion

		#region Inline Interface: Get after null set

		[Fact]
		public void GetAfterNullSet_Inline_ReturnsConfiguredValue()
		{
			var stub = new AllowNullInlineTests.Stubs.IAllow();
			IAllow svc = stub;

			stub.NewLine.Get(() => null!);

			string result = svc.NewLine;

			Assert.Null(result);
		}

		#endregion

		#region Standalone Class: Get after null set

		[Fact]
		public void GetAfterNullSet_StandaloneClass_UnconfiguredFallsToBase()
		{
			var stub = new AllowNullClassStandaloneKnockOff();
			Allow obj = stub.Object;

			// Unconfigured class stub falls through to base, which initializes to ""
			string result = obj.NewLine;

			Assert.Equal("", result);
		}

		[Fact]
		public void GetAfterNullSet_StandaloneClass_ConfiguredReturnsValue()
		{
			var stub = new AllowNullClassStandaloneKnockOff();
			Allow obj = stub.Object;

			stub.NewLine.Get("configured");

			string result = obj.NewLine;

			Assert.Equal("configured", result);
		}

		#endregion

		#region Inline Class: Get after null set

		[Fact]
		public void GetAfterNullSet_InlineClass_UnconfiguredFallsToBase()
		{
			var stub = new AllowNullInlineTests.Stubs.Allow();
			Allow obj = stub.Object;

			string result = obj.NewLine;

			Assert.Equal("", result);
		}

		[Fact]
		public void GetAfterNullSet_InlineClass_ConfiguredReturnsValue()
		{
			var stub = new AllowNullInlineTests.Stubs.Allow();
			Allow obj = stub.Object;

			stub.NewLine.Get("configured");

			string result = obj.NewLine;

			Assert.Equal("configured", result);
		}

		#endregion

		// ====================================================================
		// Scenario 3: Set property to non-null value — normal operation
		// ====================================================================

		#region Standalone Interface: Set non-null

		[Fact]
		public void SetNonNull_Standalone_TracksValue()
		{
			var stub = new AllowNullStandaloneKnockOff();
			IAllow svc = stub;

			svc.NewLine = "hello";

			stub.NewLine.VerifySet(Called.Once);
			Assert.Equal("hello", stub.NewLine.LastSetValue);
		}

		#endregion

		#region Inline Interface: Set non-null

		[Fact]
		public void SetNonNull_Inline_TracksValue()
		{
			var stub = new AllowNullInlineTests.Stubs.IAllow();
			IAllow svc = stub;

			svc.NewLine = "hello";

			stub.NewLine.VerifySet(Called.Once);
			Assert.Equal("hello", stub.NewLine.LastSetValue);
		}

		#endregion

		#region Standalone Class: Set non-null

		[Fact]
		public void SetNonNull_StandaloneClass_TracksValue()
		{
			var stub = new AllowNullClassStandaloneKnockOff();
			Allow obj = stub.Object;

			obj.NewLine = "hello";

			stub.NewLine.VerifySet(Called.Once);
			Assert.Equal("hello", stub.NewLine.LastSetValue);
		}

		#endregion

		#region Inline Class: Set non-null

		[Fact]
		public void SetNonNull_InlineClass_TracksValue()
		{
			var stub = new AllowNullInlineTests.Stubs.Allow();
			Allow obj = stub.Object;

			obj.NewLine = "hello";

			stub.NewLine.VerifySet(Called.Once);
			Assert.Equal("hello", stub.NewLine.LastSetValue);
		}

		#endregion

		// ====================================================================
		// Scenario 4: Callback on set — captures null assignment
		// ====================================================================

		#region Standalone Interface: Callback captures null

		[Fact]
		public void CallbackOnSet_Standalone_CapturesNull()
		{
			var stub = new AllowNullStandaloneKnockOff();
			IAllow svc = stub;

			string? capturedValue = "not-null";
			stub.NewLine.Set(v => capturedValue = v);

			svc.NewLine = null;

			Assert.Null(capturedValue);
		}

		#endregion

		#region Inline Interface: Callback captures null

		[Fact]
		public void CallbackOnSet_Inline_CapturesNull()
		{
			var stub = new AllowNullInlineTests.Stubs.IAllow();
			IAllow svc = stub;

			string? capturedValue = "not-null";
			stub.NewLine.Set(v => capturedValue = v);

			svc.NewLine = null;

			Assert.Null(capturedValue);
		}

		#endregion

		#region Standalone Class: Callback captures null

		[Fact]
		public void CallbackOnSet_StandaloneClass_CapturesNull()
		{
			var stub = new AllowNullClassStandaloneKnockOff();
			Allow obj = stub.Object;

			string? capturedValue = "not-null";
			stub.NewLine.Set(v => capturedValue = v);

			obj.NewLine = null;

			Assert.Null(capturedValue);
		}

		#endregion

		#region Inline Class: Callback captures null

		[Fact]
		public void CallbackOnSet_InlineClass_CapturesNull()
		{
			var stub = new AllowNullInlineTests.Stubs.Allow();
			Allow obj = stub.Object;

			string? capturedValue = "not-null";
			stub.NewLine.Set(v => capturedValue = v);

			obj.NewLine = null;

			Assert.Null(capturedValue);
		}

		#endregion
	}
}
