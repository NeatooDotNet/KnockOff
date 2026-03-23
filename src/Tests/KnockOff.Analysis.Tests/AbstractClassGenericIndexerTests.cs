// ============================================================================
// AbstractClassGenericIndexerTests: Edge case tests for a generic abstract class
// with indexers that use the class-level type parameter in keys and returns.
// Inspired by Rocks.Analysis.IntegrationTests.AbstractClassGenericIndexerTests
//
// Target classes:
//   public abstract class AbstractClassGenericIndexer<T>
//   {
//       public abstract List<string> this[int a] { get; }
//       public abstract int this[int a, T b] { get; }
//       public abstract T this[string a] { get; }
//   }
//
//   public abstract class AbstractClassGenericIndexerGetAndInit<T>
//   {
//       public abstract List<string> this[int a] { get; init; }
//       public abstract int this[int a, T b] { get; init; }
//       public abstract T this[string a] { get; init; }
//   }
//
// Key difference from ClassGenericIndexerTests: members are ABSTRACT, not virtual.
// Abstract indexers return default(T) when unconfigured.
//
// Applicable patterns (generic class):
// - Pattern 3 (Standalone Class): [KnockOffBase<AbstractClassGenericIndexer<int>>]
// - Pattern 4 (Generic Standalone Class): [KnockOffBase(typeof(AbstractClassGenericIndexer<>))]
// - Pattern 6 (Inline Class): [KnockOff<AbstractClassGenericIndexer<int>>]
// - Pattern 9 (Open Generic Class): [KnockOff(typeof(AbstractClassGenericIndexer<>))]
// ============================================================================

using KnockOff;

// ============================================================================
// TYPE DEFINITIONS
// ============================================================================

namespace KnockOff.Analysis.Tests.AbstractClassGenericIndexerTestTypes
{
	public abstract class AbstractClassGenericIndexer<T>
	{
		public abstract List<string> this[int a] { get; }
		public abstract int this[int a, T b] { get; }
		public abstract T this[string a] { get; }
	}

	public abstract class AbstractClassGenericIndexerGetAndInit<T>
	{
		public abstract List<string> this[int a] { get; init; }
		public abstract int this[int a, T b] { get; init; }
		public abstract T this[string a] { get; init; }
	}

	// Pattern 3: Standalone class stub (closed generic)
	[KnockOffBase<AbstractClassGenericIndexer<int>>]
	public partial class AbstractClassGenericIndexerStandaloneKnockOff
	{
	}

	// Pattern 3: Standalone class stub for GetAndInit variant
	[KnockOffBase<AbstractClassGenericIndexerGetAndInit<int>>]
	public partial class AbstractClassGenericIndexerGetAndInitStandaloneKnockOff
	{
	}

	// Pattern 4: Generic standalone class stub (open generic)
	[KnockOffBase(typeof(AbstractClassGenericIndexer<>))]
	public partial class AbstractClassGenericIndexerGenericStandaloneKnockOff<T>
	{
	}

	// Pattern 4: Generic standalone class stub for GetAndInit variant
	[KnockOffBase(typeof(AbstractClassGenericIndexerGetAndInit<>))]
	public partial class AbstractClassGenericIndexerGetAndInitGenericStandaloneKnockOff<T>
	{
	}
}

// ============================================================================
// INLINE PATTERN DECLARATIONS + TESTS
// ============================================================================

namespace KnockOff.Analysis.Tests
{
	using KnockOff.Analysis.Tests.AbstractClassGenericIndexerTestTypes;

	// Pattern 6: Inline class stub (closed generic)
	[KnockOff<AbstractClassGenericIndexer<int>>]
	[KnockOff<AbstractClassGenericIndexerGetAndInit<int>>]
	public partial class AbstractClassGenericIndexerInlineTests
	{
	}

	// Pattern 9: Open generic class stub
	[KnockOff(typeof(AbstractClassGenericIndexer<>))]
	[KnockOff(typeof(AbstractClassGenericIndexerGetAndInit<>))]
	public partial class AbstractClassGenericIndexerOpenGenericTests
	{
	}

	public class AbstractClassGenericIndexerTests
	{
		// ====================================================================
		// Scenario 1: Concrete return indexer (this[int a] -> List<string>)
		// ====================================================================

		#region Pattern 3 (Standalone Class): Concrete return indexer

		[Fact]
		public void IntKey_StandaloneClass_ConfiguredReturnsValue()
		{
			var stub = new AbstractClassGenericIndexerStandaloneKnockOff();
			AbstractClassGenericIndexer<int> obj = stub.Object;

			var returnValue = new List<string> { "a", "b" };
			stub.Indexer.Get((int key) => returnValue);

			var result = obj[4];

			Assert.Same(returnValue, result);
		}

		[Fact]
		public void IntKey_StandaloneClass_UnconfiguredReturnsDefault()
		{
			var stub = new AbstractClassGenericIndexerStandaloneKnockOff();
			AbstractClassGenericIndexer<int> obj = stub.Object;

			// Abstract indexer returns default(List<string>) => null
			var result = obj[4];

			Assert.Null(result);
		}

		[Fact]
		public void IntKey_StandaloneClass_VerifyTracksAccess()
		{
			var stub = new AbstractClassGenericIndexerStandaloneKnockOff();
			AbstractClassGenericIndexer<int> obj = stub.Object;

			stub.Indexer.Get((int key) => new List<string>());
			_ = obj[1];
			_ = obj[2];

			stub.Indexer.VerifyGet(Called.Exactly(2));
		}

		#endregion

		#region Pattern 4 (Generic Standalone Class): Concrete return indexer

		[Fact]
		public void IntKey_GenericStandaloneClass_ConfiguredReturnsValue()
		{
			var stub = new AbstractClassGenericIndexerGenericStandaloneKnockOff<int>();
			AbstractClassGenericIndexer<int> obj = stub.Object;

			var returnValue = new List<string> { "x", "y" };
			stub.Indexer.Get((int key) => returnValue);

			var result = obj[4];

			Assert.Same(returnValue, result);
		}

		[Fact]
		public void IntKey_GenericStandaloneClass_UnconfiguredReturnsDefault()
		{
			var stub = new AbstractClassGenericIndexerGenericStandaloneKnockOff<int>();
			AbstractClassGenericIndexer<int> obj = stub.Object;

			var result = obj[4];

			Assert.Null(result);
		}

		#endregion

		#region Pattern 6 (Inline Class): Concrete return indexer

		[Fact]
		public void IntKey_InlineClass_ConfiguredReturnsValue()
		{
			var stub = new AbstractClassGenericIndexerInlineTests.Stubs.AbstractClassGenericIndexer();
			AbstractClassGenericIndexer<int> obj = stub.Object;

			var returnValue = new List<string> { "c", "d" };
			stub.Indexer.Get((int key) => returnValue);

			var result = obj[4];

			Assert.Same(returnValue, result);
		}

		[Fact]
		public void IntKey_InlineClass_UnconfiguredReturnsDefault()
		{
			var stub = new AbstractClassGenericIndexerInlineTests.Stubs.AbstractClassGenericIndexer();
			AbstractClassGenericIndexer<int> obj = stub.Object;

			var result = obj[4];

			Assert.Null(result);
		}

		[Fact]
		public void IntKey_InlineClass_VerifyTracksAccess()
		{
			var stub = new AbstractClassGenericIndexerInlineTests.Stubs.AbstractClassGenericIndexer();
			AbstractClassGenericIndexer<int> obj = stub.Object;

			stub.Indexer.Get((int key) => new List<string>());
			_ = obj[1];
			_ = obj[2];

			stub.Indexer.VerifyGet(Called.Exactly(2));
		}

		#endregion

		#region Pattern 9 (Open Generic Class): Concrete return indexer

		[Fact]
		public void IntKey_OpenGenericClass_ConfiguredReturnsValue()
		{
			var stub = new AbstractClassGenericIndexerOpenGenericTests.Stubs.AbstractClassGenericIndexer<int>();
			AbstractClassGenericIndexer<int> obj = stub.Object;

			var returnValue = new List<string> { "e", "f" };
			stub.Indexer.Get((int key) => returnValue);

			var result = obj[4];

			Assert.Same(returnValue, result);
		}

		[Fact]
		public void IntKey_OpenGenericClass_UnconfiguredReturnsDefault()
		{
			var stub = new AbstractClassGenericIndexerOpenGenericTests.Stubs.AbstractClassGenericIndexer<int>();
			AbstractClassGenericIndexer<int> obj = stub.Object;

			var result = obj[4];

			Assert.Null(result);
		}

		#endregion

		// ====================================================================
		// Scenario 2: T in key indexer (this[int a, T b] -> int)
		// ====================================================================

		#region Pattern 3 (Standalone Class): T in key indexer

		[Fact]
		public void IntIntKey_StandaloneClass_ConfiguredReturnsValue()
		{
			var stub = new AbstractClassGenericIndexerStandaloneKnockOff();
			AbstractClassGenericIndexer<int> obj = stub.Object;

			stub.Indexer.Get(((int a, int b) key) => key.a + key.b);

			int result = obj[4, 5];

			Assert.Equal(9, result);
		}

		[Fact]
		public void IntIntKey_StandaloneClass_UnconfiguredReturnsDefault()
		{
			var stub = new AbstractClassGenericIndexerStandaloneKnockOff();
			AbstractClassGenericIndexer<int> obj = stub.Object;

			int result = obj[4, 5];

			Assert.Equal(0, result);
		}

		#endregion

		#region Pattern 4 (Generic Standalone Class): T in key indexer

		[Fact]
		public void IntIntKey_GenericStandaloneClass_ConfiguredReturnsValue()
		{
			var stub = new AbstractClassGenericIndexerGenericStandaloneKnockOff<int>();
			AbstractClassGenericIndexer<int> obj = stub.Object;

			stub.Indexer.Get(((int a, int b) key) => key.a * key.b);

			int result = obj[3, 4];

			Assert.Equal(12, result);
		}

		[Fact]
		public void IntIntKey_GenericStandaloneClass_UnconfiguredReturnsDefault()
		{
			var stub = new AbstractClassGenericIndexerGenericStandaloneKnockOff<int>();
			AbstractClassGenericIndexer<int> obj = stub.Object;

			int result = obj[4, 5];

			Assert.Equal(0, result);
		}

		#endregion

		#region Pattern 6 (Inline Class): T in key indexer

		[Fact]
		public void IntIntKey_InlineClass_ConfiguredReturnsValue()
		{
			var stub = new AbstractClassGenericIndexerInlineTests.Stubs.AbstractClassGenericIndexer();
			AbstractClassGenericIndexer<int> obj = stub.Object;

			stub.Indexer.Get(((int a, int b) key) => key.a + key.b);

			int result = obj[4, 5];

			Assert.Equal(9, result);
		}

		[Fact]
		public void IntIntKey_InlineClass_UnconfiguredReturnsDefault()
		{
			var stub = new AbstractClassGenericIndexerInlineTests.Stubs.AbstractClassGenericIndexer();
			AbstractClassGenericIndexer<int> obj = stub.Object;

			int result = obj[4, 5];

			Assert.Equal(0, result);
		}

		#endregion

		#region Pattern 9 (Open Generic Class): T in key indexer

		[Fact]
		public void IntIntKey_OpenGenericClass_ConfiguredReturnsValue()
		{
			var stub = new AbstractClassGenericIndexerOpenGenericTests.Stubs.AbstractClassGenericIndexer<int>();
			AbstractClassGenericIndexer<int> obj = stub.Object;

			stub.Indexer.Get(((int a, int b) key) => key.a * key.b);

			int result = obj[3, 4];

			Assert.Equal(12, result);
		}

		[Fact]
		public void IntIntKey_OpenGenericClass_UnconfiguredReturnsDefault()
		{
			var stub = new AbstractClassGenericIndexerOpenGenericTests.Stubs.AbstractClassGenericIndexer<int>();
			AbstractClassGenericIndexer<int> obj = stub.Object;

			int result = obj[4, 5];

			Assert.Equal(0, result);
		}

		#endregion

		// ====================================================================
		// Scenario 3: T as return indexer (this[string a] -> T)
		// ====================================================================

		#region Pattern 3 (Standalone Class): T as return indexer

		[Fact]
		public void StringKey_StandaloneClass_ConfiguredReturnsValue()
		{
			var stub = new AbstractClassGenericIndexerStandaloneKnockOff();
			AbstractClassGenericIndexer<int> obj = stub.Object;

			stub.Indexer.Get((string key) => key.Length);

			var result = obj["hello"];

			Assert.Equal(5, result);
		}

		[Fact]
		public void StringKey_StandaloneClass_UnconfiguredReturnsDefault()
		{
			var stub = new AbstractClassGenericIndexerStandaloneKnockOff();
			AbstractClassGenericIndexer<int> obj = stub.Object;

			var result = obj["hello"];

			Assert.Equal(default, result);
		}

		#endregion

		#region Pattern 4 (Generic Standalone Class): T as return indexer

		[Fact]
		public void StringKey_GenericStandaloneClass_ConfiguredReturnsValue()
		{
			var stub = new AbstractClassGenericIndexerGenericStandaloneKnockOff<int>();
			AbstractClassGenericIndexer<int> obj = stub.Object;

			stub.Indexer.Get((string key) => key.Length * 2);

			var result = obj["hi"];

			Assert.Equal(4, result);
		}

		[Fact]
		public void StringKey_GenericStandaloneClass_UnconfiguredReturnsDefault()
		{
			var stub = new AbstractClassGenericIndexerGenericStandaloneKnockOff<int>();
			AbstractClassGenericIndexer<int> obj = stub.Object;

			var result = obj["hello"];

			Assert.Equal(default, result);
		}

		#endregion

		#region Pattern 6 (Inline Class): T as return indexer

		[Fact]
		public void StringKey_InlineClass_ConfiguredReturnsValue()
		{
			var stub = new AbstractClassGenericIndexerInlineTests.Stubs.AbstractClassGenericIndexer();
			AbstractClassGenericIndexer<int> obj = stub.Object;

			stub.Indexer.Get((string key) => key.Length);

			var result = obj["hello"];

			Assert.Equal(5, result);
		}

		[Fact]
		public void StringKey_InlineClass_UnconfiguredReturnsDefault()
		{
			var stub = new AbstractClassGenericIndexerInlineTests.Stubs.AbstractClassGenericIndexer();
			AbstractClassGenericIndexer<int> obj = stub.Object;

			var result = obj["hello"];

			Assert.Equal(default, result);
		}

		#endregion

		#region Pattern 9 (Open Generic Class): T as return indexer

		[Fact]
		public void StringKey_OpenGenericClass_ConfiguredReturnsValue()
		{
			var stub = new AbstractClassGenericIndexerOpenGenericTests.Stubs.AbstractClassGenericIndexer<int>();
			AbstractClassGenericIndexer<int> obj = stub.Object;

			stub.Indexer.Get((string key) => key.Length * 2);

			var result = obj["hi"];

			Assert.Equal(4, result);
		}

		[Fact]
		public void StringKey_OpenGenericClass_UnconfiguredReturnsDefault()
		{
			var stub = new AbstractClassGenericIndexerOpenGenericTests.Stubs.AbstractClassGenericIndexer<int>();
			AbstractClassGenericIndexer<int> obj = stub.Object;

			var result = obj["hello"];

			Assert.Equal(default, result);
		}

		#endregion

		// ====================================================================
		// Scenario 4: Init variant — concrete return indexer
		// ====================================================================

		#region Pattern 3 (Standalone Class): GetAndInit concrete return indexer

		[Fact]
		public void IntKeyGetInit_StandaloneClass_ConfiguredReturnsValue()
		{
			var stub = new AbstractClassGenericIndexerGetAndInitStandaloneKnockOff();
			AbstractClassGenericIndexerGetAndInit<int> obj = stub.Object;

			var returnValue = new List<string> { "init-a" };
			stub.Indexer.Get((int key) => returnValue);

			var result = obj[4];

			Assert.Same(returnValue, result);
		}

		[Fact]
		public void IntKeyGetInit_StandaloneClass_UnconfiguredReturnsDefault()
		{
			var stub = new AbstractClassGenericIndexerGetAndInitStandaloneKnockOff();
			AbstractClassGenericIndexerGetAndInit<int> obj = stub.Object;

			var result = obj[4];

			Assert.Null(result);
		}

		#endregion

		#region Pattern 4 (Generic Standalone Class): GetAndInit concrete return indexer

		[Fact]
		public void IntKeyGetInit_GenericStandaloneClass_ConfiguredReturnsValue()
		{
			var stub = new AbstractClassGenericIndexerGetAndInitGenericStandaloneKnockOff<int>();
			AbstractClassGenericIndexerGetAndInit<int> obj = stub.Object;

			var returnValue = new List<string> { "init-b" };
			stub.Indexer.Get((int key) => returnValue);

			var result = obj[4];

			Assert.Same(returnValue, result);
		}

		[Fact]
		public void IntKeyGetInit_GenericStandaloneClass_UnconfiguredReturnsDefault()
		{
			var stub = new AbstractClassGenericIndexerGetAndInitGenericStandaloneKnockOff<int>();
			AbstractClassGenericIndexerGetAndInit<int> obj = stub.Object;

			var result = obj[4];

			Assert.Null(result);
		}

		#endregion

		#region Pattern 6 (Inline Class): GetAndInit concrete return indexer

		[Fact]
		public void IntKeyGetInit_InlineClass_ConfiguredReturnsValue()
		{
			var stub = new AbstractClassGenericIndexerInlineTests.Stubs.AbstractClassGenericIndexerGetAndInit();
			AbstractClassGenericIndexerGetAndInit<int> obj = stub.Object;

			var returnValue = new List<string> { "init-c" };
			stub.Indexer.Get((int key) => returnValue);

			var result = obj[4];

			Assert.Same(returnValue, result);
		}

		[Fact]
		public void IntKeyGetInit_InlineClass_UnconfiguredReturnsDefault()
		{
			var stub = new AbstractClassGenericIndexerInlineTests.Stubs.AbstractClassGenericIndexerGetAndInit();
			AbstractClassGenericIndexerGetAndInit<int> obj = stub.Object;

			var result = obj[4];

			Assert.Null(result);
		}

		#endregion

		#region Pattern 9 (Open Generic Class): GetAndInit concrete return indexer

		[Fact]
		public void IntKeyGetInit_OpenGenericClass_ConfiguredReturnsValue()
		{
			var stub = new AbstractClassGenericIndexerOpenGenericTests.Stubs.AbstractClassGenericIndexerGetAndInit<int>();
			AbstractClassGenericIndexerGetAndInit<int> obj = stub.Object;

			var returnValue = new List<string> { "init-d" };
			stub.Indexer.Get((int key) => returnValue);

			var result = obj[4];

			Assert.Same(returnValue, result);
		}

		[Fact]
		public void IntKeyGetInit_OpenGenericClass_UnconfiguredReturnsDefault()
		{
			var stub = new AbstractClassGenericIndexerOpenGenericTests.Stubs.AbstractClassGenericIndexerGetAndInit<int>();
			AbstractClassGenericIndexerGetAndInit<int> obj = stub.Object;

			var result = obj[4];

			Assert.Null(result);
		}

		#endregion

		// ====================================================================
		// Scenario 5: Init variant — T in key indexer
		// ====================================================================

		#region Pattern 3 (Standalone Class): GetAndInit T in key indexer

		[Fact]
		public void IntIntKeyGetInit_StandaloneClass_ConfiguredReturnsValue()
		{
			var stub = new AbstractClassGenericIndexerGetAndInitStandaloneKnockOff();
			AbstractClassGenericIndexerGetAndInit<int> obj = stub.Object;

			stub.Indexer.Get(((int a, int b) key) => key.a + key.b);

			int result = obj[4, 5];

			Assert.Equal(9, result);
		}

		#endregion

		#region Pattern 4 (Generic Standalone Class): GetAndInit T in key indexer

		[Fact]
		public void IntIntKeyGetInit_GenericStandaloneClass_ConfiguredReturnsValue()
		{
			var stub = new AbstractClassGenericIndexerGetAndInitGenericStandaloneKnockOff<int>();
			AbstractClassGenericIndexerGetAndInit<int> obj = stub.Object;

			stub.Indexer.Get(((int a, int b) key) => key.a * key.b);

			int result = obj[3, 4];

			Assert.Equal(12, result);
		}

		#endregion

		#region Pattern 6 (Inline Class): GetAndInit T in key indexer

		[Fact]
		public void IntIntKeyGetInit_InlineClass_ConfiguredReturnsValue()
		{
			var stub = new AbstractClassGenericIndexerInlineTests.Stubs.AbstractClassGenericIndexerGetAndInit();
			AbstractClassGenericIndexerGetAndInit<int> obj = stub.Object;

			stub.Indexer.Get(((int a, int b) key) => key.a + key.b);

			int result = obj[4, 5];

			Assert.Equal(9, result);
		}

		#endregion

		#region Pattern 9 (Open Generic Class): GetAndInit T in key indexer

		[Fact]
		public void IntIntKeyGetInit_OpenGenericClass_ConfiguredReturnsValue()
		{
			var stub = new AbstractClassGenericIndexerOpenGenericTests.Stubs.AbstractClassGenericIndexerGetAndInit<int>();
			AbstractClassGenericIndexerGetAndInit<int> obj = stub.Object;

			stub.Indexer.Get(((int a, int b) key) => key.a * key.b);

			int result = obj[3, 4];

			Assert.Equal(12, result);
		}

		#endregion

		// ====================================================================
		// Scenario 6: Init variant — T as return indexer
		// ====================================================================

		#region Pattern 3 (Standalone Class): GetAndInit T as return indexer

		[Fact]
		public void StringKeyGetInit_StandaloneClass_ConfiguredReturnsValue()
		{
			var stub = new AbstractClassGenericIndexerGetAndInitStandaloneKnockOff();
			AbstractClassGenericIndexerGetAndInit<int> obj = stub.Object;

			stub.Indexer.Get((string key) => key.Length);

			var result = obj["hello"];

			Assert.Equal(5, result);
		}

		[Fact]
		public void StringKeyGetInit_StandaloneClass_UnconfiguredReturnsDefault()
		{
			var stub = new AbstractClassGenericIndexerGetAndInitStandaloneKnockOff();
			AbstractClassGenericIndexerGetAndInit<int> obj = stub.Object;

			var result = obj["hello"];

			Assert.Equal(default, result);
		}

		#endregion

		#region Pattern 4 (Generic Standalone Class): GetAndInit T as return indexer

		[Fact]
		public void StringKeyGetInit_GenericStandaloneClass_ConfiguredReturnsValue()
		{
			var stub = new AbstractClassGenericIndexerGetAndInitGenericStandaloneKnockOff<int>();
			AbstractClassGenericIndexerGetAndInit<int> obj = stub.Object;

			stub.Indexer.Get((string key) => key.Length * 2);

			var result = obj["hi"];

			Assert.Equal(4, result);
		}

		#endregion

		#region Pattern 6 (Inline Class): GetAndInit T as return indexer

		[Fact]
		public void StringKeyGetInit_InlineClass_ConfiguredReturnsValue()
		{
			var stub = new AbstractClassGenericIndexerInlineTests.Stubs.AbstractClassGenericIndexerGetAndInit();
			AbstractClassGenericIndexerGetAndInit<int> obj = stub.Object;

			stub.Indexer.Get((string key) => key.Length);

			var result = obj["hello"];

			Assert.Equal(5, result);
		}

		[Fact]
		public void StringKeyGetInit_InlineClass_UnconfiguredReturnsDefault()
		{
			var stub = new AbstractClassGenericIndexerInlineTests.Stubs.AbstractClassGenericIndexerGetAndInit();
			AbstractClassGenericIndexerGetAndInit<int> obj = stub.Object;

			var result = obj["hello"];

			Assert.Equal(default, result);
		}

		#endregion

		#region Pattern 9 (Open Generic Class): GetAndInit T as return indexer

		[Fact]
		public void StringKeyGetInit_OpenGenericClass_ConfiguredReturnsValue()
		{
			var stub = new AbstractClassGenericIndexerOpenGenericTests.Stubs.AbstractClassGenericIndexerGetAndInit<int>();
			AbstractClassGenericIndexerGetAndInit<int> obj = stub.Object;

			stub.Indexer.Get((string key) => key.Length * 2);

			var result = obj["hi"];

			Assert.Equal(4, result);
		}

		[Fact]
		public void StringKeyGetInit_OpenGenericClass_UnconfiguredReturnsDefault()
		{
			var stub = new AbstractClassGenericIndexerOpenGenericTests.Stubs.AbstractClassGenericIndexerGetAndInit<int>();
			AbstractClassGenericIndexerGetAndInit<int> obj = stub.Object;

			var result = obj["hello"];

			Assert.Equal(default, result);
		}

		#endregion
	}
}
