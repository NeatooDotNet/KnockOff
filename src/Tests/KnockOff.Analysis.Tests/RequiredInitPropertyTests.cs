// ============================================================================
// RequiredInitPropertyTests: Edge case tests for required and init-only properties
// Inspired by Rocks.Analysis.IntegrationTests.RequiredInitPropertyTests
//
// Tests two distinct property scenarios on classes with virtual methods:
//
// 1. Required properties (C# 11): The 'required' modifier forces callers to
//    set the property at construction time. Since the generated Impl class uses
//    [SetsRequiredMembers] and initializes required members to default!, the
//    stub can be instantiated without satisfying the required constraint.
//
// 2. Init-only properties: Properties with 'init' instead of 'set' can only
//    be assigned during object initialization. Since the Impl class is created
//    internally, init-only properties receive default! values and cannot be
//    changed by the test author.
//
// Key behavioral differences from Rocks:
// - Rocks uses an initializer object to set init/required properties
// - KnockOff uses composition (stub.Object) with no initializer -- non-virtual
//   properties are inherited and receive default! values in the Impl constructor
// - For properties with 'set' (not init), users CAN set values via stub.Object
//
// Init-only limitation: Once the Impl is constructed, init-only properties
// cannot be reassigned. This is a C# language constraint, not a KnockOff bug.
// The test author has no way to set custom values for init-only properties
// on stub.Object. If custom init values are needed, users must use a different
// approach (e.g., subclassing with object initializer syntax).
//
// Applicable patterns (class only):
// - Pattern 3: [KnockOffBase<T>] (Standalone Class)
// - Pattern 6: [KnockOff<T>] (Inline Class)
// ============================================================================

using KnockOff;

// ============================================================================
// TYPE DEFINITIONS
// ============================================================================

namespace KnockOff.Analysis.Tests.RequiredInitPropertyTestTypes
{
	public class Requireds
	{
		public virtual void Foo() { }

		public required int NonNullableValueType { get; set; }
		public required int? NullableValueType { get; init; }
		public required string NonNullableReferenceType { get; init; }
		public required string? NullableReferenceType { get; init; }
	}

	public class Inits
	{
		public virtual void Foo() { }

		public int NonNullableValueType { get; init; }
		public int? NullableValueType { get; init; }
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
		public string NonNullableReferenceType { get; init; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
		public string? NullableReferenceType { get; init; }
	}

	// Pattern 3: Standalone class stubs
	[KnockOffBase<Requireds>]
	public partial class RequiredsStandaloneKnockOff
	{
	}

	[KnockOffBase<Inits>]
	public partial class InitsStandaloneKnockOff
	{
	}
}

// ============================================================================
// INLINE PATTERN DECLARATIONS + TESTS
// ============================================================================

namespace KnockOff.Analysis.Tests
{
	using KnockOff.Analysis.Tests.RequiredInitPropertyTestTypes;

	// Pattern 6: Inline class stubs
	[KnockOff<Requireds>]
	[KnockOff<Inits>]
	public partial class RequiredInitInlineTests
	{
	}

	public class RequiredInitPropertyTests
	{
		// ====================================================================
		// Required properties: Standalone Class (Pattern 3)
		// ====================================================================

		#region Standalone Class: Required properties

		[Fact]
		public void Required_StandaloneClass_StubCanBeInstantiated()
		{
			// The [SetsRequiredMembers] attribute on the Impl constructor
			// allows instantiation without explicitly setting required properties
			var stub = new RequiredsStandaloneKnockOff();
			Requireds obj = stub.Object;

			Assert.NotNull(obj);
		}

		[Fact]
		public void Required_StandaloneClass_SetPropertyCanBeModifiedAfterCreation()
		{
			// NonNullableValueType has { get; set; } -- it can be modified
			// even though it's 'required', because it has a regular setter
			var stub = new RequiredsStandaloneKnockOff();
			Requireds obj = stub.Object;

			obj.NonNullableValueType = 42;

			Assert.Equal(42, obj.NonNullableValueType);
		}

		[Fact]
		public void Required_StandaloneClass_InitPropertiesHaveDefaultValues()
		{
			// Init-only required properties get default! in the Impl constructor
			var stub = new RequiredsStandaloneKnockOff();
			Requireds obj = stub.Object;

			// default! for value types = 0, for nullable = null, for string = null
			Assert.Equal(0, obj.NonNullableValueType);
			Assert.Null(obj.NullableValueType);
			Assert.Null(obj.NonNullableReferenceType);
			Assert.Null(obj.NullableReferenceType);
		}

		[Fact]
		public void Required_StandaloneClass_SetPropertyRoundTripsMultipleValues()
		{
			// The one 'set' property (NonNullableValueType) should support full
			// round-trip: set a value, read it back, set another, read it back.
			// Init-only properties cannot be retested this way -- they are frozen.
			var stub = new RequiredsStandaloneKnockOff();
			Requireds obj = stub.Object;

			obj.NonNullableValueType = 100;
			Assert.Equal(100, obj.NonNullableValueType);

			obj.NonNullableValueType = -1;
			Assert.Equal(-1, obj.NonNullableValueType);
		}

		#endregion

		// ====================================================================
		// Required properties: Inline Class (Pattern 6)
		// ====================================================================

		#region Inline Class: Required properties

		[Fact]
		public void Required_InlineClass_StubCanBeInstantiated()
		{
			var stub = new RequiredInitInlineTests.Stubs.Requireds();
			Requireds obj = stub.Object;

			Assert.NotNull(obj);
		}

		[Fact]
		public void Required_InlineClass_SetPropertyCanBeModifiedAfterCreation()
		{
			var stub = new RequiredInitInlineTests.Stubs.Requireds();
			Requireds obj = stub.Object;

			obj.NonNullableValueType = 99;

			Assert.Equal(99, obj.NonNullableValueType);
		}

		[Fact]
		public void Required_InlineClass_InitPropertiesHaveDefaultValues()
		{
			var stub = new RequiredInitInlineTests.Stubs.Requireds();
			Requireds obj = stub.Object;

			Assert.Equal(0, obj.NonNullableValueType);
			Assert.Null(obj.NullableValueType);
			Assert.Null(obj.NonNullableReferenceType);
			Assert.Null(obj.NullableReferenceType);
		}

		[Fact]
		public void Required_InlineClass_SetPropertyRoundTripsMultipleValues()
		{
			var stub = new RequiredInitInlineTests.Stubs.Requireds();
			Requireds obj = stub.Object;

			obj.NonNullableValueType = 100;
			Assert.Equal(100, obj.NonNullableValueType);

			obj.NonNullableValueType = -1;
			Assert.Equal(-1, obj.NonNullableValueType);
		}

		#endregion

		// ====================================================================
		// Init-only properties (non-required): Standalone Class (Pattern 3)
		// ====================================================================

		#region Standalone Class: Init-only properties

		[Fact]
		public void Init_StandaloneClass_StubCanBeInstantiated()
		{
			var stub = new InitsStandaloneKnockOff();
			Inits obj = stub.Object;

			Assert.NotNull(obj);
		}

		[Fact]
		public void Init_StandaloneClass_InitPropertiesHaveDefaultValues()
		{
			// Init-only properties receive default values from construction
			var stub = new InitsStandaloneKnockOff();
			Inits obj = stub.Object;

			Assert.Equal(0, obj.NonNullableValueType);
			Assert.Null(obj.NullableValueType);
			Assert.Null(obj.NonNullableReferenceType);
			Assert.Null(obj.NullableReferenceType);
		}

		#endregion

		// ====================================================================
		// Init-only properties (non-required): Inline Class (Pattern 6)
		// ====================================================================

		#region Inline Class: Init-only properties

		[Fact]
		public void Init_InlineClass_StubCanBeInstantiated()
		{
			var stub = new RequiredInitInlineTests.Stubs.Inits();
			Inits obj = stub.Object;

			Assert.NotNull(obj);
		}

		[Fact]
		public void Init_InlineClass_InitPropertiesHaveDefaultValues()
		{
			var stub = new RequiredInitInlineTests.Stubs.Inits();
			Inits obj = stub.Object;

			Assert.Equal(0, obj.NonNullableValueType);
			Assert.Null(obj.NullableValueType);
			Assert.Null(obj.NonNullableReferenceType);
			Assert.Null(obj.NullableReferenceType);
		}

		#endregion
	}
}
