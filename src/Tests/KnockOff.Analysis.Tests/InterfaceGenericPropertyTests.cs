// ============================================================================
// InterfaceGenericPropertyTests: Edge case tests for a generic interface with
// properties using the class-level type parameter.
// Inspired by Rocks.Analysis.IntegrationTests.InterfaceGenericPropertyTests
//
// Target interfaces:
//   public interface IInterfaceGenericProperty<T>
//   {
//       List<string> Values { get; }   // concrete generic type
//       T Data { get; }                // class type parameter
//   }
//
//   public interface IInterfaceGenericPropertyGetAndInit<T>
//   {
//       List<string> Values { get; init; }
//       T Data { get; init; }
//   }
//
// Scenarios:
// 1. Concrete generic property (Values: List<string>) — configure return, assert
// 2. Class type param property (Data: T->int) — configure return, assert
// 3. Unconfigured defaults — verify default returns
// 4. Init variant — configured and unconfigured
// 5. Verify tracking — VerifyGet counts access
// 6. Callback on property get — computed return value
//
// Applicable patterns (generic interface):
// - Pattern 1 (Standalone): [KnockOff] partial class Stub : IInterfaceGenericProperty<int> {}
// - Pattern 2 (Generic Standalone): [KnockOff] partial class Stub<T> : IInterfaceGenericProperty<T> {}
// - Pattern 5 (Inline Interface): [KnockOff<IInterfaceGenericProperty<int>>]
// - Pattern 8 (Open Generic Interface): [KnockOff(typeof(IInterfaceGenericProperty<>))]
// ============================================================================

using KnockOff;

// ============================================================================
// TYPE DEFINITIONS
// ============================================================================

namespace KnockOff.Analysis.Tests.InterfaceGenericPropertyTestTypes
{
	public interface IInterfaceGenericProperty<T>
	{
		List<string> Values { get; }
		T Data { get; }
	}

	public interface IInterfaceGenericPropertyGetAndInit<T>
	{
		List<string> Values { get; init; }
		T Data { get; init; }
	}

	// Pattern 1: Standalone interface stub (closed generic)
	[KnockOff]
	public partial class InterfaceGenericPropertyStandaloneKnockOff : IInterfaceGenericProperty<int>
	{
	}

	// Pattern 1: Standalone interface stub for GetAndInit variant
	[KnockOff]
	public partial class InterfaceGenericPropertyGetAndInitStandaloneKnockOff : IInterfaceGenericPropertyGetAndInit<int>
	{
	}

	// Pattern 2: Generic standalone interface stub (open generic)
	[KnockOff]
	public partial class InterfaceGenericPropertyGenericStandaloneKnockOff<T> : IInterfaceGenericProperty<T>
	{
	}

	// Pattern 2: Generic standalone interface stub for GetAndInit variant
	[KnockOff]
	public partial class InterfaceGenericPropertyGetAndInitGenericStandaloneKnockOff<T> : IInterfaceGenericPropertyGetAndInit<T>
	{
	}
}

// ============================================================================
// INLINE PATTERN DECLARATIONS + TESTS
// ============================================================================

namespace KnockOff.Analysis.Tests
{
	using KnockOff.Analysis.Tests.InterfaceGenericPropertyTestTypes;

	// Pattern 5: Inline interface stub (closed generic)
	[KnockOff<IInterfaceGenericProperty<int>>]
	[KnockOff<IInterfaceGenericPropertyGetAndInit<int>>]
	public partial class InterfaceGenericPropertyInlineTests
	{
	}

	// Pattern 8: Open generic interface stub
	[KnockOff(typeof(IInterfaceGenericProperty<>))]
	[KnockOff(typeof(IInterfaceGenericPropertyGetAndInit<>))]
	public partial class InterfaceGenericPropertyOpenGenericTests
	{
	}

	public class InterfaceGenericPropertyTests
	{
		// ====================================================================
		// Scenario 1: Concrete generic property (Values: List<string>)
		// Configure return, read, assert
		// ====================================================================

		#region Pattern 1 (Standalone): Concrete generic property (Values)

		[Fact]
		public void Values_Standalone_ConfiguredReturnsValue()
		{
			var stub = new InterfaceGenericPropertyStandaloneKnockOff();
			IInterfaceGenericProperty<int> service = stub;

			var returnValue = new List<string> { "a", "b" };
			stub.Values.Get(returnValue);

			var result = service.Values;

			Assert.Same(returnValue, result);
		}

		[Fact]
		public void Values_Standalone_UnconfiguredReturnsDefault()
		{
			var stub = new InterfaceGenericPropertyStandaloneKnockOff();
			IInterfaceGenericProperty<int> service = stub;

			// List<string> has a parameterless ctor — unconfigured returns new()
			var result = service.Values;

			Assert.NotNull(result);
			Assert.Empty(result);
		}

		[Fact]
		public void Values_Standalone_VerifyTracksAccess()
		{
			var stub = new InterfaceGenericPropertyStandaloneKnockOff();
			IInterfaceGenericProperty<int> service = stub;

			stub.Values.Get(new List<string>());
			_ = service.Values;
			_ = service.Values;

			stub.Values.VerifyGet(Called.Exactly(2));
		}

		#endregion

		#region Pattern 2 (Generic Standalone): Concrete generic property (Values)

		[Fact]
		public void Values_GenericStandalone_ConfiguredReturnsValue()
		{
			var stub = new InterfaceGenericPropertyGenericStandaloneKnockOff<int>();
			IInterfaceGenericProperty<int> service = stub;

			var returnValue = new List<string> { "x", "y" };
			stub.Values.Get(returnValue);

			var result = service.Values;

			Assert.Same(returnValue, result);
		}

		#endregion

		#region Pattern 5 (Inline Interface): Concrete generic property (Values)

		[Fact]
		public void Values_Inline_ConfiguredReturnsValue()
		{
			var stub = new InterfaceGenericPropertyInlineTests.Stubs.IInterfaceGenericProperty();
			IInterfaceGenericProperty<int> service = stub;

			var returnValue = new List<string> { "c", "d" };
			stub.Values.Get(returnValue);

			var result = service.Values;

			Assert.Same(returnValue, result);
		}

		[Fact]
		public void Values_Inline_VerifyTracksAccess()
		{
			var stub = new InterfaceGenericPropertyInlineTests.Stubs.IInterfaceGenericProperty();
			IInterfaceGenericProperty<int> service = stub;

			stub.Values.Get(new List<string>());
			_ = service.Values;
			_ = service.Values;

			stub.Values.VerifyGet(Called.Exactly(2));
		}

		#endregion

		#region Pattern 8 (Open Generic Interface): Concrete generic property (Values)

		[Fact]
		public void Values_OpenGeneric_ConfiguredReturnsValue()
		{
			var stub = new InterfaceGenericPropertyOpenGenericTests.Stubs.IInterfaceGenericProperty<int>();
			IInterfaceGenericProperty<int> service = stub;

			var returnValue = new List<string> { "e", "f" };
			stub.Values.Get(returnValue);

			var result = service.Values;

			Assert.Same(returnValue, result);
		}

		#endregion

		// ====================================================================
		// Scenario 2: Class type param property (Data: T->int)
		// Configure return with the closed type, read, assert
		// ====================================================================

		#region Pattern 1 (Standalone): Class type param property (Data)

		[Fact]
		public void Data_Standalone_ConfiguredReturnsValue()
		{
			var stub = new InterfaceGenericPropertyStandaloneKnockOff();
			IInterfaceGenericProperty<int> service = stub;

			stub.Data.Get(42);

			int result = service.Data;

			Assert.Equal(42, result);
		}

		[Fact]
		public void Data_Standalone_UnconfiguredReturnsDefault()
		{
			var stub = new InterfaceGenericPropertyStandaloneKnockOff();
			IInterfaceGenericProperty<int> service = stub;

			// int is a value type — unconfigured returns default(int) = 0
			int result = service.Data;

			Assert.Equal(0, result);
		}

		[Fact]
		public void Data_Standalone_VerifyTracksAccess()
		{
			var stub = new InterfaceGenericPropertyStandaloneKnockOff();
			IInterfaceGenericProperty<int> service = stub;

			stub.Data.Get(1);
			_ = service.Data;
			_ = service.Data;

			stub.Data.VerifyGet(Called.Exactly(2));
		}

		#endregion

		#region Pattern 2 (Generic Standalone): Class type param property (Data)

		[Fact]
		public void Data_GenericStandalone_ConfiguredReturnsValue()
		{
			var stub = new InterfaceGenericPropertyGenericStandaloneKnockOff<int>();
			IInterfaceGenericProperty<int> service = stub;

			stub.Data.Get(99);

			int result = service.Data;

			Assert.Equal(99, result);
		}

		#endregion

		#region Pattern 5 (Inline Interface): Class type param property (Data)

		[Fact]
		public void Data_Inline_ConfiguredReturnsValue()
		{
			var stub = new InterfaceGenericPropertyInlineTests.Stubs.IInterfaceGenericProperty();
			IInterfaceGenericProperty<int> service = stub;

			stub.Data.Get(42);

			int result = service.Data;

			Assert.Equal(42, result);
		}

		[Fact]
		public void Data_Inline_UnconfiguredReturnsDefault()
		{
			var stub = new InterfaceGenericPropertyInlineTests.Stubs.IInterfaceGenericProperty();
			IInterfaceGenericProperty<int> service = stub;

			int result = service.Data;

			Assert.Equal(0, result);
		}

		[Fact]
		public void Data_Inline_VerifyTracksAccess()
		{
			var stub = new InterfaceGenericPropertyInlineTests.Stubs.IInterfaceGenericProperty();
			IInterfaceGenericProperty<int> service = stub;

			stub.Data.Get(1);
			_ = service.Data;
			_ = service.Data;

			stub.Data.VerifyGet(Called.Exactly(2));
		}

		#endregion

		#region Pattern 8 (Open Generic Interface): Class type param property (Data)

		[Fact]
		public void Data_OpenGeneric_ConfiguredReturnsValue()
		{
			var stub = new InterfaceGenericPropertyOpenGenericTests.Stubs.IInterfaceGenericProperty<int>();
			IInterfaceGenericProperty<int> service = stub;

			stub.Data.Get(99);

			int result = service.Data;

			Assert.Equal(99, result);
		}

		#endregion

		// ====================================================================
		// Scenario 3: Get/Init variant — concrete generic property
		// IInterfaceGenericPropertyGetAndInit<T>.Values { get; init; }
		// ====================================================================

		#region Pattern 1 (Standalone): Get/Init Values

		[Fact]
		public void ValuesGetInit_Standalone_ConfiguredReturnsValue()
		{
			var stub = new InterfaceGenericPropertyGetAndInitStandaloneKnockOff();
			IInterfaceGenericPropertyGetAndInit<int> service = stub;

			var returnValue = new List<string> { "init-a" };
			stub.Values.Get(returnValue);

			var result = service.Values;

			Assert.Same(returnValue, result);
		}

		#endregion

		#region Pattern 2 (Generic Standalone): Get/Init Values

		[Fact]
		public void ValuesGetInit_GenericStandalone_ConfiguredReturnsValue()
		{
			var stub = new InterfaceGenericPropertyGetAndInitGenericStandaloneKnockOff<int>();
			IInterfaceGenericPropertyGetAndInit<int> service = stub;

			var returnValue = new List<string> { "init-b" };
			stub.Values.Get(returnValue);

			var result = service.Values;

			Assert.Same(returnValue, result);
		}

		#endregion

		#region Pattern 5 (Inline Interface): Get/Init Values

		[Fact]
		public void ValuesGetInit_Inline_ConfiguredReturnsValue()
		{
			var stub = new InterfaceGenericPropertyInlineTests.Stubs.IInterfaceGenericPropertyGetAndInit();
			IInterfaceGenericPropertyGetAndInit<int> service = stub;

			var returnValue = new List<string> { "init-c" };
			stub.Values.Get(returnValue);

			var result = service.Values;

			Assert.Same(returnValue, result);
		}

		#endregion

		#region Pattern 8 (Open Generic Interface): Get/Init Values

		[Fact]
		public void ValuesGetInit_OpenGeneric_ConfiguredReturnsValue()
		{
			var stub = new InterfaceGenericPropertyOpenGenericTests.Stubs.IInterfaceGenericPropertyGetAndInit<int>();
			IInterfaceGenericPropertyGetAndInit<int> service = stub;

			var returnValue = new List<string> { "init-d" };
			stub.Values.Get(returnValue);

			var result = service.Values;

			Assert.Same(returnValue, result);
		}

		#endregion

		// ====================================================================
		// Scenario 4: Get/Init variant — class type param property
		// IInterfaceGenericPropertyGetAndInit<T>.Data { get; init; }
		// ====================================================================

		#region Pattern 1 (Standalone): Get/Init Data

		[Fact]
		public void DataGetInit_Standalone_ConfiguredReturnsValue()
		{
			var stub = new InterfaceGenericPropertyGetAndInitStandaloneKnockOff();
			IInterfaceGenericPropertyGetAndInit<int> service = stub;

			stub.Data.Get(77);

			int result = service.Data;

			Assert.Equal(77, result);
		}

		[Fact]
		public void DataGetInit_Standalone_UnconfiguredReturnsDefault()
		{
			var stub = new InterfaceGenericPropertyGetAndInitStandaloneKnockOff();
			IInterfaceGenericPropertyGetAndInit<int> service = stub;

			int result = service.Data;

			Assert.Equal(0, result);
		}

		#endregion

		#region Pattern 2 (Generic Standalone): Get/Init Data

		[Fact]
		public void DataGetInit_GenericStandalone_ConfiguredReturnsValue()
		{
			var stub = new InterfaceGenericPropertyGetAndInitGenericStandaloneKnockOff<int>();
			IInterfaceGenericPropertyGetAndInit<int> service = stub;

			stub.Data.Get(88);

			int result = service.Data;

			Assert.Equal(88, result);
		}

		#endregion

		#region Pattern 5 (Inline Interface): Get/Init Data

		[Fact]
		public void DataGetInit_Inline_ConfiguredReturnsValue()
		{
			var stub = new InterfaceGenericPropertyInlineTests.Stubs.IInterfaceGenericPropertyGetAndInit();
			IInterfaceGenericPropertyGetAndInit<int> service = stub;

			stub.Data.Get(77);

			int result = service.Data;

			Assert.Equal(77, result);
		}

		[Fact]
		public void DataGetInit_Inline_UnconfiguredReturnsDefault()
		{
			var stub = new InterfaceGenericPropertyInlineTests.Stubs.IInterfaceGenericPropertyGetAndInit();
			IInterfaceGenericPropertyGetAndInit<int> service = stub;

			int result = service.Data;

			Assert.Equal(0, result);
		}

		#endregion

		#region Pattern 8 (Open Generic Interface): Get/Init Data

		[Fact]
		public void DataGetInit_OpenGeneric_ConfiguredReturnsValue()
		{
			var stub = new InterfaceGenericPropertyOpenGenericTests.Stubs.IInterfaceGenericPropertyGetAndInit<int>();
			IInterfaceGenericPropertyGetAndInit<int> service = stub;

			stub.Data.Get(88);

			int result = service.Data;

			Assert.Equal(88, result);
		}

		#endregion

		// ====================================================================
		// Scenario 5: Callback on property get — computed return value
		// Verify dynamic callback works with generic type param return.
		// ====================================================================

		#region Pattern 1 (Standalone): Callback on Data get

		[Fact]
		public void Data_Standalone_CallbackReturnsComputedValue()
		{
			var stub = new InterfaceGenericPropertyStandaloneKnockOff();
			IInterfaceGenericProperty<int> service = stub;

			var accessCount = 0;
			stub.Data.Get(() =>
			{
				accessCount++;
				return accessCount * 10;
			});

			int first = service.Data;
			int second = service.Data;

			Assert.Equal(10, first);
			Assert.Equal(20, second);
		}

		#endregion

		#region Pattern 2 (Generic Standalone): Callback on Data get

		[Fact]
		public void Data_GenericStandalone_CallbackReturnsComputedValue()
		{
			var stub = new InterfaceGenericPropertyGenericStandaloneKnockOff<int>();
			IInterfaceGenericProperty<int> service = stub;

			var accessCount = 0;
			stub.Data.Get(() =>
			{
				accessCount++;
				return accessCount * 5;
			});

			int first = service.Data;
			int second = service.Data;

			Assert.Equal(5, first);
			Assert.Equal(10, second);
		}

		#endregion

		#region Pattern 5 (Inline Interface): Callback on Data get

		[Fact]
		public void Data_Inline_CallbackReturnsComputedValue()
		{
			var stub = new InterfaceGenericPropertyInlineTests.Stubs.IInterfaceGenericProperty();
			IInterfaceGenericProperty<int> service = stub;

			var accessCount = 0;
			stub.Data.Get(() =>
			{
				accessCount++;
				return accessCount * 10;
			});

			int first = service.Data;
			int second = service.Data;

			Assert.Equal(10, first);
			Assert.Equal(20, second);
		}

		#endregion

		#region Pattern 8 (Open Generic Interface): Callback on Data get

		[Fact]
		public void Data_OpenGeneric_CallbackReturnsComputedValue()
		{
			var stub = new InterfaceGenericPropertyOpenGenericTests.Stubs.IInterfaceGenericProperty<int>();
			IInterfaceGenericProperty<int> service = stub;

			var accessCount = 0;
			stub.Data.Get(() =>
			{
				accessCount++;
				return accessCount * 5;
			});

			int first = service.Data;
			int second = service.Data;

			Assert.Equal(5, first);
			Assert.Equal(10, second);
		}

		#endregion
	}
}
