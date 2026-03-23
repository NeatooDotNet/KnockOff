// ============================================================================
// InterfaceGenericIndexerTests: Edge case tests for a generic interface with
// indexers using the class-level type parameter in keys and returns.
// Inspired by Rocks.Analysis.IntegrationTests.InterfaceGenericIndexerTests
//
// Target interfaces:
//   public interface IInterfaceGenericIndexer<T>
//   {
//       List<string> this[int a] { get; }       // concrete return, int key
//       int this[int a, T b] { get; }           // T in key (multi-param)
//       T this[string a] { get; }               // T as return, string key
//   }
//
//   public interface IInterfaceGenericIndexerGetAndInit<T>
//   {
//       List<string> this[int a] { get; init; }
//       int this[int a, T b] { get; init; }
//       T this[string a] { get; init; }
//   }
//
// Scenarios:
// 1. Concrete return indexer (this[int a] -> List<string>) — callback return
// 2. T in key indexer (this[int a, T b] -> int) — multi-param callback
// 3. T as return indexer (this[string a] -> T) — type param return
// 4. Unconfigured defaults — verify default returns
// 5. Init variant — configured
// 6. Verify tracking — VerifyGet counts access
//
// Applicable patterns (generic interface):
// - Pattern 1 (Standalone): [KnockOff] partial class Stub : IInterfaceGenericIndexer<int> {}
// - Pattern 2 (Generic Standalone): [KnockOff] partial class Stub<T> : IInterfaceGenericIndexer<T> {}
// - Pattern 5 (Inline Interface): [KnockOff<IInterfaceGenericIndexer<int>>]
// - Pattern 8 (Open Generic Interface): [KnockOff(typeof(IInterfaceGenericIndexer<>))]
// ============================================================================

using KnockOff;

// ============================================================================
// TYPE DEFINITIONS
// ============================================================================

namespace KnockOff.Analysis.Tests.InterfaceGenericIndexerTestTypes
{
	public interface IInterfaceGenericIndexer<T>
	{
		List<string> this[int a] { get; }
		int this[int a, T b] { get; }
		T this[string a] { get; }
	}

	public interface IInterfaceGenericIndexerGetAndInit<T>
	{
		List<string> this[int a] { get; init; }
		int this[int a, T b] { get; init; }
		T this[string a] { get; init; }
	}

	// Pattern 1: Standalone interface stub (closed generic)
	[KnockOff]
	public partial class InterfaceGenericIndexerStandaloneKnockOff : IInterfaceGenericIndexer<int>
	{
	}

	// Pattern 1: Standalone interface stub for GetAndInit variant
	[KnockOff]
	public partial class InterfaceGenericIndexerGetAndInitStandaloneKnockOff : IInterfaceGenericIndexerGetAndInit<int>
	{
	}

	// Pattern 2: Generic standalone interface stub (open generic)
	[KnockOff]
	public partial class InterfaceGenericIndexerGenericStandaloneKnockOff<T> : IInterfaceGenericIndexer<T>
	{
	}

	// Pattern 2: Generic standalone interface stub for GetAndInit variant
	[KnockOff]
	public partial class InterfaceGenericIndexerGetAndInitGenericStandaloneKnockOff<T> : IInterfaceGenericIndexerGetAndInit<T>
	{
	}
}

// ============================================================================
// INLINE PATTERN DECLARATIONS + TESTS
// ============================================================================

namespace KnockOff.Analysis.Tests
{
	using KnockOff.Analysis.Tests.InterfaceGenericIndexerTestTypes;

	// Pattern 5: Inline interface stub (closed generic)
	[KnockOff<IInterfaceGenericIndexer<int>>]
	[KnockOff<IInterfaceGenericIndexerGetAndInit<int>>]
	public partial class InterfaceGenericIndexerInlineTests
	{
	}

	// Pattern 8: Open generic interface stub
	[KnockOff(typeof(IInterfaceGenericIndexer<>))]
	[KnockOff(typeof(IInterfaceGenericIndexerGetAndInit<>))]
	public partial class InterfaceGenericIndexerOpenGenericTests
	{
	}

	public class InterfaceGenericIndexerTests
	{
		// ====================================================================
		// Scenario 1: Concrete return indexer (this[int a] -> List<string>)
		// Configure get callback, assert returned value
		// ====================================================================

		#region Pattern 1 (Standalone): Concrete return indexer

		[Fact]
		public void ConcreteReturn_Standalone_ConfiguredReturnsValue()
		{
			var stub = new InterfaceGenericIndexerStandaloneKnockOff();
			IInterfaceGenericIndexer<int> service = stub;

			var returnValue = new List<string> { "a", "b" };
			stub.Indexer.Get((int key) => returnValue);

			var result = service[4];

			Assert.Same(returnValue, result);
		}

		[Fact]
		public void ConcreteReturn_Standalone_VerifyTracksAccess()
		{
			var stub = new InterfaceGenericIndexerStandaloneKnockOff();
			IInterfaceGenericIndexer<int> service = stub;

			stub.Indexer.Get((int key) => new List<string>());
			_ = service[1];
			_ = service[2];

			stub.Indexer.VerifyGet(Called.Exactly(2));
		}

		#endregion

		#region Pattern 2 (Generic Standalone): Concrete return indexer

		[Fact]
		public void ConcreteReturn_GenericStandalone_ConfiguredReturnsValue()
		{
			var stub = new InterfaceGenericIndexerGenericStandaloneKnockOff<int>();
			IInterfaceGenericIndexer<int> service = stub;

			var returnValue = new List<string> { "x", "y" };
			stub.Indexer.Get((int key) => returnValue);

			var result = service[4];

			Assert.Same(returnValue, result);
		}

		#endregion

		#region Pattern 5 (Inline Interface): Concrete return indexer

		[Fact]
		public void ConcreteReturn_Inline_ConfiguredReturnsValue()
		{
			var stub = new InterfaceGenericIndexerInlineTests.Stubs.IInterfaceGenericIndexer();
			IInterfaceGenericIndexer<int> service = stub;

			var returnValue = new List<string> { "c", "d" };
			stub.Indexer.Get((int key) => returnValue);

			var result = service[4];

			Assert.Same(returnValue, result);
		}

		[Fact]
		public void ConcreteReturn_Inline_VerifyTracksAccess()
		{
			var stub = new InterfaceGenericIndexerInlineTests.Stubs.IInterfaceGenericIndexer();
			IInterfaceGenericIndexer<int> service = stub;

			stub.Indexer.Get((int key) => new List<string>());
			_ = service[1];
			_ = service[2];

			stub.Indexer.VerifyGet(Called.Exactly(2));
		}

		#endregion

		#region Pattern 8 (Open Generic Interface): Concrete return indexer

		[Fact]
		public void ConcreteReturn_OpenGeneric_ConfiguredReturnsValue()
		{
			var stub = new InterfaceGenericIndexerOpenGenericTests.Stubs.IInterfaceGenericIndexer<int>();
			IInterfaceGenericIndexer<int> service = stub;

			var returnValue = new List<string> { "e", "f" };
			stub.Indexer.Get((int key) => returnValue);

			var result = service[4];

			Assert.Same(returnValue, result);
		}

		#endregion

		// ====================================================================
		// Scenario 2: T in key indexer (this[int a, T b] -> int)
		// Multi-param indexer with T used as a key parameter.
		// ====================================================================

		#region Pattern 1 (Standalone): T in key indexer

		[Fact]
		public void TypeParamKey_Standalone_CallbackReceivesArgs()
		{
			var stub = new InterfaceGenericIndexerStandaloneKnockOff();
			IInterfaceGenericIndexer<int> service = stub;

			stub.Indexer.Get(((int a, int b) key) => key.a + key.b);

			int result = service[4, 5];

			Assert.Equal(9, result);
		}

		[Fact]
		public void TypeParamKey_Standalone_VerifyTracksAccess()
		{
			var stub = new InterfaceGenericIndexerStandaloneKnockOff();
			IInterfaceGenericIndexer<int> service = stub;

			stub.Indexer.Get(((int a, int b) key) => key.a);
			_ = service[1, 2];
			_ = service[3, 4];

			stub.Indexer.VerifyGet(Called.Exactly(2));
		}

		#endregion

		#region Pattern 2 (Generic Standalone): T in key indexer

		[Fact]
		public void TypeParamKey_GenericStandalone_CallbackReceivesArgs()
		{
			var stub = new InterfaceGenericIndexerGenericStandaloneKnockOff<int>();
			IInterfaceGenericIndexer<int> service = stub;

			stub.Indexer.Get(((int a, int b) key) => key.a + key.b);

			int result = service[4, 5];

			Assert.Equal(9, result);
		}

		#endregion

		#region Pattern 5 (Inline Interface): T in key indexer

		[Fact]
		public void TypeParamKey_Inline_CallbackReceivesArgs()
		{
			var stub = new InterfaceGenericIndexerInlineTests.Stubs.IInterfaceGenericIndexer();
			IInterfaceGenericIndexer<int> service = stub;

			stub.Indexer.Get(((int a, int b) key) => key.a + key.b);

			int result = service[4, 5];

			Assert.Equal(9, result);
		}

		[Fact]
		public void TypeParamKey_Inline_VerifyTracksAccess()
		{
			var stub = new InterfaceGenericIndexerInlineTests.Stubs.IInterfaceGenericIndexer();
			IInterfaceGenericIndexer<int> service = stub;

			stub.Indexer.Get(((int a, int b) key) => key.a);
			_ = service[1, 2];
			_ = service[3, 4];

			stub.Indexer.VerifyGet(Called.Exactly(2));
		}

		#endregion

		#region Pattern 8 (Open Generic Interface): T in key indexer

		[Fact]
		public void TypeParamKey_OpenGeneric_CallbackReceivesArgs()
		{
			var stub = new InterfaceGenericIndexerOpenGenericTests.Stubs.IInterfaceGenericIndexer<int>();
			IInterfaceGenericIndexer<int> service = stub;

			stub.Indexer.Get(((int a, int b) key) => key.a + key.b);

			int result = service[4, 5];

			Assert.Equal(9, result);
		}

		#endregion

		// ====================================================================
		// Scenario 3: T as return indexer (this[string a] -> T)
		// Type parameter used as return type with string key.
		// ====================================================================

		#region Pattern 1 (Standalone): T as return indexer

		[Fact]
		public void TypeParamReturn_Standalone_ConfiguredReturnsValue()
		{
			var stub = new InterfaceGenericIndexerStandaloneKnockOff();
			IInterfaceGenericIndexer<int> service = stub;

			stub.Indexer.Get((string key) => key.Length);

			int result = service["hello"];

			Assert.Equal(5, result);
		}

		[Fact]
		public void TypeParamReturn_Standalone_UnconfiguredReturnsDefault()
		{
			var stub = new InterfaceGenericIndexerStandaloneKnockOff();
			IInterfaceGenericIndexer<int> service = stub;

			// int is a value type — unconfigured returns default(int) = 0
			int result = service["hello"];

			Assert.Equal(0, result);
		}

		#endregion

		#region Pattern 2 (Generic Standalone): T as return indexer

		[Fact]
		public void TypeParamReturn_GenericStandalone_ConfiguredReturnsValue()
		{
			var stub = new InterfaceGenericIndexerGenericStandaloneKnockOff<int>();
			IInterfaceGenericIndexer<int> service = stub;

			stub.Indexer.Get((string key) => key.Length * 2);

			int result = service["hi"];

			Assert.Equal(4, result);
		}

		#endregion

		#region Pattern 5 (Inline Interface): T as return indexer

		[Fact]
		public void TypeParamReturn_Inline_ConfiguredReturnsValue()
		{
			var stub = new InterfaceGenericIndexerInlineTests.Stubs.IInterfaceGenericIndexer();
			IInterfaceGenericIndexer<int> service = stub;

			stub.Indexer.Get((string key) => key.Length);

			int result = service["hello"];

			Assert.Equal(5, result);
		}

		[Fact]
		public void TypeParamReturn_Inline_UnconfiguredReturnsDefault()
		{
			var stub = new InterfaceGenericIndexerInlineTests.Stubs.IInterfaceGenericIndexer();
			IInterfaceGenericIndexer<int> service = stub;

			int result = service["hello"];

			Assert.Equal(0, result);
		}

		#endregion

		#region Pattern 8 (Open Generic Interface): T as return indexer

		[Fact]
		public void TypeParamReturn_OpenGeneric_ConfiguredReturnsValue()
		{
			var stub = new InterfaceGenericIndexerOpenGenericTests.Stubs.IInterfaceGenericIndexer<int>();
			IInterfaceGenericIndexer<int> service = stub;

			stub.Indexer.Get((string key) => key.Length * 2);

			int result = service["hi"];

			Assert.Equal(4, result);
		}

		#endregion

		// ====================================================================
		// Scenario 4: Get/Init variant — configured
		// IInterfaceGenericIndexerGetAndInit<T>
		// ====================================================================

		#region Pattern 1 (Standalone): Get/Init concrete return

		[Fact]
		public void ConcreteReturnGetInit_Standalone_ConfiguredReturnsValue()
		{
			var stub = new InterfaceGenericIndexerGetAndInitStandaloneKnockOff();
			IInterfaceGenericIndexerGetAndInit<int> service = stub;

			var returnValue = new List<string> { "init-a" };
			stub.Indexer.Get((int key) => returnValue);

			var result = service[4];

			Assert.Same(returnValue, result);
		}

		#endregion

		#region Pattern 2 (Generic Standalone): Get/Init concrete return

		[Fact]
		public void ConcreteReturnGetInit_GenericStandalone_ConfiguredReturnsValue()
		{
			var stub = new InterfaceGenericIndexerGetAndInitGenericStandaloneKnockOff<int>();
			IInterfaceGenericIndexerGetAndInit<int> service = stub;

			var returnValue = new List<string> { "init-b" };
			stub.Indexer.Get((int key) => returnValue);

			var result = service[4];

			Assert.Same(returnValue, result);
		}

		#endregion

		#region Pattern 5 (Inline Interface): Get/Init concrete return

		[Fact]
		public void ConcreteReturnGetInit_Inline_ConfiguredReturnsValue()
		{
			var stub = new InterfaceGenericIndexerInlineTests.Stubs.IInterfaceGenericIndexerGetAndInit();
			IInterfaceGenericIndexerGetAndInit<int> service = stub;

			var returnValue = new List<string> { "init-c" };
			stub.Indexer.Get((int key) => returnValue);

			var result = service[4];

			Assert.Same(returnValue, result);
		}

		#endregion

		#region Pattern 8 (Open Generic Interface): Get/Init concrete return

		[Fact]
		public void ConcreteReturnGetInit_OpenGeneric_ConfiguredReturnsValue()
		{
			var stub = new InterfaceGenericIndexerOpenGenericTests.Stubs.IInterfaceGenericIndexerGetAndInit<int>();
			IInterfaceGenericIndexerGetAndInit<int> service = stub;

			var returnValue = new List<string> { "init-d" };
			stub.Indexer.Get((int key) => returnValue);

			var result = service[4];

			Assert.Same(returnValue, result);
		}

		#endregion

		// ====================================================================
		// Scenario 5: Get/Init T as return
		// ====================================================================

		#region Pattern 1 (Standalone): Get/Init T as return

		[Fact]
		public void TypeParamReturnGetInit_Standalone_ConfiguredReturnsValue()
		{
			var stub = new InterfaceGenericIndexerGetAndInitStandaloneKnockOff();
			IInterfaceGenericIndexerGetAndInit<int> service = stub;

			stub.Indexer.Get((string key) => key.Length);

			int result = service["hello"];

			Assert.Equal(5, result);
		}

		#endregion

		#region Pattern 5 (Inline Interface): Get/Init T as return

		[Fact]
		public void TypeParamReturnGetInit_Inline_ConfiguredReturnsValue()
		{
			var stub = new InterfaceGenericIndexerInlineTests.Stubs.IInterfaceGenericIndexerGetAndInit();
			IInterfaceGenericIndexerGetAndInit<int> service = stub;

			stub.Indexer.Get((string key) => key.Length);

			int result = service["hello"];

			Assert.Equal(5, result);
		}

		#endregion

		// ====================================================================
		// Scenario 6: Get/Init T in key
		// ====================================================================

		#region Pattern 1 (Standalone): Get/Init T in key

		[Fact]
		public void TypeParamKeyGetInit_Standalone_CallbackReceivesArgs()
		{
			var stub = new InterfaceGenericIndexerGetAndInitStandaloneKnockOff();
			IInterfaceGenericIndexerGetAndInit<int> service = stub;

			stub.Indexer.Get(((int a, int b) key) => key.a + key.b);

			int result = service[4, 5];

			Assert.Equal(9, result);
		}

		#endregion

		#region Pattern 5 (Inline Interface): Get/Init T in key

		[Fact]
		public void TypeParamKeyGetInit_Inline_CallbackReceivesArgs()
		{
			var stub = new InterfaceGenericIndexerInlineTests.Stubs.IInterfaceGenericIndexerGetAndInit();
			IInterfaceGenericIndexerGetAndInit<int> service = stub;

			stub.Indexer.Get(((int a, int b) key) => key.a + key.b);

			int result = service[4, 5];

			Assert.Equal(9, result);
		}

		#endregion
	}
}
