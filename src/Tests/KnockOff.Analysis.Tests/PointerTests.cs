// ============================================================================
// PointerTests: Edge case tests for interfaces with pointer types.
// Inspired by Rocks.Analysis.IntegrationTests.PointerTests
//
// Key behavior: Interfaces with unsafe pointer types (int*, delegate*,
// generic T* where T : unmanaged) are a special case. The generator may
// or may not support pointer types. These tests document the current
// support level.
//
// Target interfaces:
//   public interface ISurface
//   {
//       unsafe void Create<T>(T* allocator) where T : unmanaged;
//   }
//
//   public unsafe interface IHavePointers
//   {
//       void DelegatePointerParameter(delegate*<int, void> value);
//       delegate*<int, void> DelegatePointerReturn();
//       void PointerParameter(int* value);
//       int* PointerReturn();
//   }
//
// Applicable patterns (interface only):
// - Pattern 1 (Standalone): [KnockOff] on class implementing interface
// - Pattern 5 (Inline Interface): [KnockOff<ISurface>] / [KnockOff<IHavePointers>]
// ============================================================================

using KnockOff;

// ============================================================================
// TYPE DEFINITIONS
// ============================================================================

namespace KnockOff.Analysis.Tests.PointerTestTypes
{
	public interface ISurface
	{
		unsafe void Create<T>(T* allocator) where T : unmanaged;
	}

	public unsafe interface IHavePointers
	{
		void DelegatePointerParameter(delegate*<int, void> value);
		delegate*<int, void> DelegatePointerReturn();
		void PointerParameter(int* value);
		int* PointerReturn();
	}
}

// ============================================================================
// TESTS
// ============================================================================

namespace KnockOff.Analysis.Tests
{
	using KnockOff.Analysis.Tests.PointerTestTypes;

	public class PointerTests
	{
		// KnockOff does not currently support unsafe pointer types.
		// Pointer parameters (int*, T*, delegate*<int, void>) cannot be
		// represented as interceptor generic type arguments or stored in
		// the standard call tracking infrastructure.
		//
		// These tests document the limitation. When pointer support is
		// added, replace Skip with real assertions.

		[Fact(Skip = "KnockOff does not support unsafe/pointer types - ISurface with T* parameter")]
		public void PointerTypeParameter_Standalone_Create()
		{
			// Would test: [KnockOff] on class implementing ISurface
			// ISurface.Create<T>(T* allocator) uses pointer in generic context
		}

		[Fact(Skip = "KnockOff does not support unsafe/pointer types - ISurface with T* parameter")]
		public void PointerTypeParameter_Inline_Create()
		{
			// Would test: [KnockOff<ISurface>] inline stub
		}

		[Fact(Skip = "KnockOff does not support unsafe/pointer types - IHavePointers.PointerParameter(int*)")]
		public void PointerParameter_Standalone_PassesPointer()
		{
			// Would test: stub.PointerParameter called with int* value
		}

		[Fact(Skip = "KnockOff does not support unsafe/pointer types - IHavePointers.PointerParameter(int*)")]
		public void PointerParameter_Inline_PassesPointer()
		{
			// Would test: inline stub PointerParameter with int* value
		}

		[Fact(Skip = "KnockOff does not support unsafe/pointer types - IHavePointers.PointerReturn()")]
		public void PointerReturn_Standalone_ReturnsPointer()
		{
			// Would test: stub.PointerReturn configured to return int*
		}

		[Fact(Skip = "KnockOff does not support unsafe/pointer types - IHavePointers.PointerReturn()")]
		public void PointerReturn_Inline_ReturnsPointer()
		{
			// Would test: inline stub PointerReturn returns int*
		}

		[Fact(Skip = "KnockOff does not support unsafe/pointer types - delegate*<int, void> parameter")]
		public void DelegatePointerParameter_Standalone()
		{
			// Would test: stub.DelegatePointerParameter with delegate*<int, void>
		}

		[Fact(Skip = "KnockOff does not support unsafe/pointer types - delegate*<int, void> parameter")]
		public void DelegatePointerParameter_Inline()
		{
			// Would test: inline stub DelegatePointerParameter
		}

		[Fact(Skip = "KnockOff does not support unsafe/pointer types - delegate*<int, void> return")]
		public void DelegatePointerReturn_Standalone()
		{
			// Would test: stub.DelegatePointerReturn returns delegate*<int, void>
		}

		[Fact(Skip = "KnockOff does not support unsafe/pointer types - delegate*<int, void> return")]
		public void DelegatePointerReturn_Inline()
		{
			// Would test: inline stub DelegatePointerReturn
		}

		[Fact(Skip = "KnockOff does not support unsafe/pointer types - pointer parameter callback")]
		public void PointerParameter_Standalone_WithCallback()
		{
			// Would test: callback on PointerParameter that modifies pointed-to value
		}

		[Fact(Skip = "KnockOff does not support unsafe/pointer types - pointer return callback")]
		public void PointerReturn_Standalone_WithCallback()
		{
			// Would test: callback on PointerReturn that returns a pointer
		}
	}
}
