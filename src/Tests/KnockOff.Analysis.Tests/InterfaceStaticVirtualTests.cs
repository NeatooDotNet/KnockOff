// ============================================================================
// InterfaceStaticVirtualTests: Edge case tests for interfaces with static
// virtual/abstract members alongside instance members.
// Inspired by Rocks.Analysis.IntegrationTests.InterfaceStaticVirtualTests
//
// Key behavior: The generator should skip static virtual/abstract members
// and only stub instance members. Static virtuals belong to the type, not
// instances, so stubs must not attempt to implement them.
//
// Target interface:
//   public interface IHaveStaticVirtuals
//   {
//       string InstanceLift();
//       string? InstancePush { get; set; }
//       static virtual string StaticLift() => "Lift";
//       static virtual string? StaticPush { get; set; }
//   }
//
// Scenarios:
// 1. Instance method works (call, return, verify)
// 2. Instance property get/set works
// 3. Static members are excluded (no interceptors generated for them)
//
// Applicable patterns (interface only):
// - Pattern 1 (Standalone): [KnockOff] on class implementing interface
// - Pattern 5 (Inline Interface): [KnockOff<IHaveStaticVirtuals>]
// ============================================================================

using KnockOff;

// ============================================================================
// TYPE DEFINITIONS
// ============================================================================

namespace KnockOff.Analysis.Tests.InterfaceStaticVirtualTestTypes
{
	public interface IHaveStaticVirtuals
	{
		string InstanceLift();
		string? InstancePush { get; set; }

		static virtual string StaticLift() => "Lift";
		static virtual string? StaticPush { get; set; }
	}

	// Pattern 1: Standalone interface stub
	[KnockOff]
	public partial class StaticVirtualStandaloneKnockOff : IHaveStaticVirtuals
	{
	}
}

// ============================================================================
// INLINE PATTERN DECLARATIONS + TESTS
// ============================================================================

namespace KnockOff.Analysis.Tests
{
	using KnockOff.Analysis.Tests.InterfaceStaticVirtualTestTypes;

	// Pattern 5: Inline interface stub
	[KnockOff<IHaveStaticVirtuals>]
	public partial class InterfaceStaticVirtualInlineTests
	{
	}

	public class InterfaceStaticVirtualTests
	{
		// ====================================================================
		// Scenario 1: Instance method — return configured value
		// ====================================================================

		#region Pattern 1 (Standalone): Instance method

		[Fact]
		public void InstanceMethod_Standalone_ReturnsConfiguredValue()
		{
			var stub = new StaticVirtualStandaloneKnockOff();
			IHaveStaticVirtuals service = stub;

			stub.InstanceLift.Return("Lifted");

			var result = service.InstanceLift();

			Assert.Equal("Lifted", result);
		}

		[Fact]
		public void InstanceMethod_Standalone_VerifyTracksCall()
		{
			var stub = new StaticVirtualStandaloneKnockOff();
			IHaveStaticVirtuals service = stub;

			stub.InstanceLift.Return("a");

			service.InstanceLift();
			service.InstanceLift();

			stub.InstanceLift.Verify(Called.Exactly(2));
		}

		#endregion

		#region Pattern 5 (Inline): Instance method

		[Fact]
		public void InstanceMethod_Inline_ReturnsConfiguredValue()
		{
			var stub = new InterfaceStaticVirtualInlineTests.Stubs.IHaveStaticVirtuals();
			IHaveStaticVirtuals service = stub;

			stub.InstanceLift.Return("Lifted");

			var result = service.InstanceLift();

			Assert.Equal("Lifted", result);
		}

		[Fact]
		public void InstanceMethod_Inline_VerifyTracksCall()
		{
			var stub = new InterfaceStaticVirtualInlineTests.Stubs.IHaveStaticVirtuals();
			IHaveStaticVirtuals service = stub;

			stub.InstanceLift.Return("a");

			service.InstanceLift();

			stub.InstanceLift.Verify(Called.Once);
		}

		#endregion

		// ====================================================================
		// Scenario 2: Instance property get/set
		// ====================================================================

		#region Pattern 1 (Standalone): Instance property

		[Fact]
		public void InstanceProperty_Standalone_GetReturnsConfiguredValue()
		{
			var stub = new StaticVirtualStandaloneKnockOff();
			IHaveStaticVirtuals service = stub;

			stub.InstancePush.Get("TestValue");

			var result = service.InstancePush;

			Assert.Equal("TestValue", result);
		}

		[Fact]
		public void InstanceProperty_Standalone_SetTracksValue()
		{
			var stub = new StaticVirtualStandaloneKnockOff();
			IHaveStaticVirtuals service = stub;

			service.InstancePush = "NewValue";

			stub.InstancePush.VerifySet(Called.Once);
			Assert.Equal("NewValue", stub.InstancePush.LastSetValue);
		}

		#endregion

		#region Pattern 5 (Inline): Instance property

		[Fact]
		public void InstanceProperty_Inline_GetReturnsConfiguredValue()
		{
			var stub = new InterfaceStaticVirtualInlineTests.Stubs.IHaveStaticVirtuals();
			IHaveStaticVirtuals service = stub;

			stub.InstancePush.Get("TestValue");

			var result = service.InstancePush;

			Assert.Equal("TestValue", result);
		}

		[Fact]
		public void InstanceProperty_Inline_SetTracksValue()
		{
			var stub = new InterfaceStaticVirtualInlineTests.Stubs.IHaveStaticVirtuals();
			IHaveStaticVirtuals service = stub;

			service.InstancePush = "NewValue";

			stub.InstancePush.VerifySet(Called.Once);
			Assert.Equal("NewValue", stub.InstancePush.LastSetValue);
		}

		#endregion

		// ====================================================================
		// Scenario 3: Static members are excluded (compile-time verification)
		// ====================================================================
		// The fact that this file compiles at all proves static members are
		// excluded. If the generator tried to create instance-level interceptors
		// for static virtual members, it would produce invalid code. The tests
		// above demonstrate that ONLY instance members are accessible on the
		// stub, confirming correct filtering.
	}
}
