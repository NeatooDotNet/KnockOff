// ============================================================================
// OpenGenericsTests: Edge case tests for open generic interfaces with
// multiple type parameters and different type argument orderings.
//
// Inspired by Rocks.Analysis.IntegrationTests.OpenGenericsTests
//
// Tests that the generator correctly handles type argument mapping when
// type parameter ORDER matters:
//   IService<int, string>  -- Service(int) returns string
//   IService<string, int>  -- Service(string) returns int
//
// Smart default behavior:
//   - Closed generics with value type return (int): unconfigured returns 0
//   - Closed generics with non-nullable ref type return (string): unconfigured
//     throws InvalidOperationException (no safe default)
//   - Open generics (TReturn type parameter): unconfigured always throws
//     because the generator cannot determine default strategy for type params
//
// Patterns tested:
// - Pattern 1 (Standalone): Closed generic [KnockOff] on class : IService<int, string>
// - Pattern 2 (Generic Standalone): [KnockOff] on class<T, TReturn> : IService<T, TReturn>
// - Pattern 5 (Inline Interface): [KnockOff<IService<int, string>>]
// - Pattern 8 (Open Generic Interface): [KnockOff(typeof(IService<,>))]
// ============================================================================

using KnockOff;

// ============================================================================
// TYPE DEFINITIONS
// ============================================================================

namespace KnockOff.Analysis.Tests.OpenGenericsTestTypes
{
	public interface IService<T, TReturn>
	{
		TReturn Service(T data);
	}

	// Pattern 1: Standalone stubs — one per closed generic variant
	[KnockOff]
	public partial class ServiceIntStringStandaloneKnockOff : IService<int, string>
	{
	}

	[KnockOff]
	public partial class ServiceStringIntStandaloneKnockOff : IService<string, int>
	{
	}

	// Pattern 2: Generic standalone stub — open generic
	[KnockOff]
	public partial class ServiceGenericStub<T, TReturn> : IService<T, TReturn>
	{
	}
}

// ============================================================================
// INLINE PATTERN DECLARATIONS + TESTS
// ============================================================================

namespace KnockOff.Analysis.Tests
{
	using KnockOff.Analysis.Tests.OpenGenericsTestTypes;

	// Pattern 5: Inline stubs — closed generic variants
	// Each closed generic needs its own host class because the generator uses
	// the simple name (IService) for the nested stub class. Two closed generics
	// of the same interface on the same class would collide.
	[KnockOff<IService<int, string>>]
	public partial class OpenGenericsInlineIntStringTests
	{
	}

	[KnockOff<IService<string, int>>]
	public partial class OpenGenericsInlineStringIntTests
	{
	}

	// Pattern 8: Open Generic Interface
	[KnockOff(typeof(IService<,>))]
	public partial class OpenGenericsOpenInlineTests
	{
	}

	public class OpenGenericsTests
	{
		// ====================================================================
		// Pattern 1: Standalone — IService<int, string>
		// ====================================================================

		#region Standalone: IService<int, string>

		[Fact]
		public void IntString_Standalone_ReturnConfigured()
		{
			var knockOff = new ServiceIntStringStandaloneKnockOff();
			IService<int, string> service = knockOff;

			knockOff.Service.Return("three");

			var result = service.Service(3);

			Assert.Equal("three", result);
		}

		[Fact]
		public void IntString_Standalone_UnconfiguredThrows()
		{
			var knockOff = new ServiceIntStringStandaloneKnockOff();
			IService<int, string> service = knockOff;

			// string is non-nullable ref type without parameterless ctor —
			// smart default throws InvalidOperationException
			var ex = Assert.Throws<InvalidOperationException>(() => service.Service(3));
			Assert.Contains("No implementation provided", ex.Message);
		}

		[Fact]
		public void IntString_Standalone_VerifyTracking()
		{
			var knockOff = new ServiceIntStringStandaloneKnockOff();
			IService<int, string> service = knockOff;

			knockOff.Service.Return("any");

			service.Service(1);
			service.Service(2);

			knockOff.Service.Verify(Called.Exactly(2));
		}

		#endregion

		#region Standalone: IService<string, int>

		[Fact]
		public void StringInt_Standalone_ReturnConfigured()
		{
			var knockOff = new ServiceStringIntStandaloneKnockOff();
			IService<string, int> service = knockOff;

			knockOff.Service.Return(4);

			var result = service.Service("four");

			Assert.Equal(4, result);
		}

		[Fact]
		public void StringInt_Standalone_UnconfiguredReturnsDefault()
		{
			var knockOff = new ServiceStringIntStandaloneKnockOff();
			IService<string, int> service = knockOff;

			// int is a value type — unconfigured returns default(int) = 0
			var result = service.Service("four");

			Assert.Equal(0, result);
		}

		[Fact]
		public void StringInt_Standalone_VerifyTracking()
		{
			var knockOff = new ServiceStringIntStandaloneKnockOff();
			IService<string, int> service = knockOff;

			service.Service("a");
			service.Service("b");
			service.Service("c");

			knockOff.Service.Verify(Called.Exactly(3));
		}

		#endregion

		// ====================================================================
		// Pattern 2: Generic Standalone — IService<T, TReturn>
		// Both type orderings from the SAME stub class definition.
		//
		// Open generic stubs always throw on unconfigured calls because the
		// generator cannot determine the default value strategy for unbounded
		// type parameters at generation time.
		// ====================================================================

		#region Generic Standalone: IService<int, string>

		[Fact]
		public void IntString_GenericStandalone_ReturnConfigured()
		{
			var knockOff = new ServiceGenericStub<int, string>();
			IService<int, string> service = knockOff;

			knockOff.Service.Return("three");

			var result = service.Service(3);

			Assert.Equal("three", result);
		}

		[Fact]
		public void IntString_GenericStandalone_UnconfiguredThrows()
		{
			var knockOff = new ServiceGenericStub<int, string>();
			IService<int, string> service = knockOff;

			// Open generic stub with TReturn — always throws on unconfigured
			var ex = Assert.Throws<InvalidOperationException>(() => service.Service(3));
			Assert.Contains("No implementation provided", ex.Message);
		}

		[Fact]
		public void IntString_GenericStandalone_VerifyTracking()
		{
			var knockOff = new ServiceGenericStub<int, string>();
			IService<int, string> service = knockOff;

			knockOff.Service.Return("any");

			service.Service(1);
			service.Service(2);

			knockOff.Service.Verify(Called.Exactly(2));
		}

		#endregion

		#region Generic Standalone: IService<string, int>

		[Fact]
		public void StringInt_GenericStandalone_ReturnConfigured()
		{
			var knockOff = new ServiceGenericStub<string, int>();
			IService<string, int> service = knockOff;

			knockOff.Service.Return(4);

			var result = service.Service("four");

			Assert.Equal(4, result);
		}

		[Fact]
		public void StringInt_GenericStandalone_UnconfiguredThrows()
		{
			var knockOff = new ServiceGenericStub<string, int>();
			IService<string, int> service = knockOff;

			// Even though int has a safe default, the generator sees TReturn
			// (unbounded type parameter) and emits ThrowException
			var ex = Assert.Throws<InvalidOperationException>(() => service.Service("four"));
			Assert.Contains("No implementation provided", ex.Message);
		}

		[Fact]
		public void StringInt_GenericStandalone_VerifyTracking()
		{
			var knockOff = new ServiceGenericStub<string, int>();
			IService<string, int> service = knockOff;

			knockOff.Service.Return(0);

			service.Service("a");
			service.Service("b");
			service.Service("c");

			knockOff.Service.Verify(Called.Exactly(3));
		}

		#endregion

		// ====================================================================
		// Pattern 5: Inline Interface — IService<int, string> and
		//                                IService<string, int>
		//
		// Closed generic inline stubs use simple names (no type args in the
		// stub class name). Two closed generics of the same interface on
		// one host class would collide on "IService", so each ordering
		// gets its own host class.
		// ====================================================================

		#region Inline: IService<int, string>

		[Fact]
		public void IntString_Inline_ReturnConfigured()
		{
			var stub = new OpenGenericsInlineIntStringTests.Stubs.IService();
			IService<int, string> service = stub;

			stub.Service.Return("three");

			var result = service.Service(3);

			Assert.Equal("three", result);
		}

		[Fact]
		public void IntString_Inline_UnconfiguredThrows()
		{
			var stub = new OpenGenericsInlineIntStringTests.Stubs.IService();
			IService<int, string> service = stub;

			// string return — throws on unconfigured
			var ex = Assert.Throws<InvalidOperationException>(() => service.Service(3));
			Assert.Contains("No implementation provided", ex.Message);
		}

		[Fact]
		public void IntString_Inline_VerifyTracking()
		{
			var stub = new OpenGenericsInlineIntStringTests.Stubs.IService();
			IService<int, string> service = stub;

			stub.Service.Return("any");

			service.Service(1);
			service.Service(2);

			stub.Service.Verify(Called.Exactly(2));
		}

		#endregion

		#region Inline: IService<string, int>

		[Fact]
		public void StringInt_Inline_ReturnConfigured()
		{
			var stub = new OpenGenericsInlineStringIntTests.Stubs.IService();
			IService<string, int> service = stub;

			stub.Service.Return(4);

			var result = service.Service("four");

			Assert.Equal(4, result);
		}

		[Fact]
		public void StringInt_Inline_UnconfiguredReturnsDefault()
		{
			var stub = new OpenGenericsInlineStringIntTests.Stubs.IService();
			IService<string, int> service = stub;

			// int return — unconfigured returns default(int) = 0
			var result = service.Service("four");

			Assert.Equal(0, result);
		}

		[Fact]
		public void StringInt_Inline_VerifyTracking()
		{
			var stub = new OpenGenericsInlineStringIntTests.Stubs.IService();
			IService<string, int> service = stub;

			service.Service("a");
			service.Service("b");
			service.Service("c");

			stub.Service.Verify(Called.Exactly(3));
		}

		#endregion

		// ====================================================================
		// Pattern 8: Open Generic Interface — IService<,>
		// Test with BOTH type argument orderings from the same stub definition.
		//
		// Open generic stubs always throw on unconfigured calls because the
		// generator cannot determine the default value strategy for unbounded
		// type parameters at generation time.
		// ====================================================================

		#region Open Generic: IService<int, string>

		[Fact]
		public void IntString_OpenGeneric_ReturnConfigured()
		{
			var stub = new OpenGenericsOpenInlineTests.Stubs.IService<int, string>();
			IService<int, string> service = stub;

			stub.Service.Return("three");

			var result = service.Service(3);

			Assert.Equal("three", result);
		}

		[Fact]
		public void IntString_OpenGeneric_UnconfiguredThrows()
		{
			var stub = new OpenGenericsOpenInlineTests.Stubs.IService<int, string>();
			IService<int, string> service = stub;

			// Open generic with TReturn — always throws on unconfigured
			var ex = Assert.Throws<InvalidOperationException>(() => service.Service(3));
			Assert.Contains("No implementation provided", ex.Message);
		}

		[Fact]
		public void IntString_OpenGeneric_VerifyTracking()
		{
			var stub = new OpenGenericsOpenInlineTests.Stubs.IService<int, string>();
			IService<int, string> service = stub;

			stub.Service.Return("any");

			service.Service(1);
			service.Service(2);

			stub.Service.Verify(Called.Exactly(2));
		}

		#endregion

		#region Open Generic: IService<string, int>

		[Fact]
		public void StringInt_OpenGeneric_ReturnConfigured()
		{
			var stub = new OpenGenericsOpenInlineTests.Stubs.IService<string, int>();
			IService<string, int> service = stub;

			stub.Service.Return(4);

			var result = service.Service("four");

			Assert.Equal(4, result);
		}

		[Fact]
		public void StringInt_OpenGeneric_UnconfiguredThrows()
		{
			var stub = new OpenGenericsOpenInlineTests.Stubs.IService<string, int>();
			IService<string, int> service = stub;

			// Open generic with TReturn — always throws on unconfigured
			var ex = Assert.Throws<InvalidOperationException>(() => service.Service("four"));
			Assert.Contains("No implementation provided", ex.Message);
		}

		[Fact]
		public void StringInt_OpenGeneric_VerifyTracking()
		{
			var stub = new OpenGenericsOpenInlineTests.Stubs.IService<string, int>();
			IService<string, int> service = stub;

			stub.Service.Return(0);

			service.Service("a");
			service.Service("b");
			service.Service("c");

			stub.Service.Verify(Called.Exactly(3));
		}

		#endregion
	}
}
