// ============================================================================
// ClassGenericIndexerTests: Edge case tests for a generic class with
// indexers that use the class-level type parameter in keys and returns.
// Inspired by Rocks.Analysis.IntegrationTests.ClassGenericIndexerTests
//
// Target classes:
//   public class ClassGenericIndexer<T>
//   {
//       public virtual List<string>? this[int a] => default;     // concrete return, int key
//       public virtual int this[int a, T b] => default;          // T in key (multi-param)
//       public virtual T? this[string a] => default;             // T as return, string key
//   }
//
//   public class ClassGenericIndexerGetAndInit<T>
//   {
//       public virtual List<string>? this[int a] { get => default; init { } }
//       public virtual int this[int a, T b] { get => default; init { } }
//       public virtual T? this[string a] { get => default; init { } }
//   }
//
// Scenarios:
// 1. Concrete return indexer (this[int a] → List<string>?) — callback return
// 2. T in key indexer (this[int a, T b] → int) — multi-param callback
// 3. T as return indexer (this[string a] → T?) — type param return
// 4. Unconfigured fallback to base — verify default returns
// 5. Init variant — configured and unconfigured
// 6. Verify tracking — VerifyGet counts access
//
// Applicable patterns (generic class):
// - Pattern 3 (Standalone Class): [KnockOffBase<ClassGenericIndexer<int>>]
// - Pattern 4 (Generic Standalone Class): [KnockOffBase(typeof(ClassGenericIndexer<>))]
// - Pattern 6 (Inline Class): [KnockOff<ClassGenericIndexer<int>>]
// - Pattern 9 (Open Generic Class): [KnockOff(typeof(ClassGenericIndexer<>))]
// ============================================================================

using KnockOff;

// ============================================================================
// TYPE DEFINITIONS
// ============================================================================

namespace KnockOff.Analysis.Tests.ClassGenericIndexerTestTypes
{
	public class ClassGenericIndexer<T>
	{
		public virtual List<string>? this[int a] => default;
		public virtual int this[int a, T b] => default;
		public virtual T? this[string a] => default;
	}

	public class ClassGenericIndexerGetAndInit<T>
	{
		public virtual List<string>? this[int a] { get => default; init { } }
		public virtual int this[int a, T b] { get => default; init { } }
		public virtual T? this[string a] { get => default; init { } }
	}

	// Pattern 3: Standalone class stub (closed generic)
	[KnockOffBase<ClassGenericIndexer<int>>]
	public partial class ClassGenericIndexerStandaloneKnockOff
	{
	}

	// Pattern 3: Standalone class stub for GetAndInit variant
	[KnockOffBase<ClassGenericIndexerGetAndInit<int>>]
	public partial class ClassGenericIndexerGetAndInitStandaloneKnockOff
	{
	}

	// Pattern 4: Generic standalone class stub (open generic)
	[KnockOffBase(typeof(ClassGenericIndexer<>))]
	public partial class ClassGenericIndexerGenericStandaloneKnockOff<T>
	{
	}

	// Pattern 4: Generic standalone class stub for GetAndInit variant
	[KnockOffBase(typeof(ClassGenericIndexerGetAndInit<>))]
	public partial class ClassGenericIndexerGetAndInitGenericStandaloneKnockOff<T>
	{
	}
}

// ============================================================================
// INLINE PATTERN DECLARATIONS + TESTS
// ============================================================================

namespace KnockOff.Analysis.Tests
{
	using KnockOff.Analysis.Tests.ClassGenericIndexerTestTypes;

	// Pattern 6: Inline class stub (closed generic)
	[KnockOff<ClassGenericIndexer<int>>]
	[KnockOff<ClassGenericIndexerGetAndInit<int>>]
	public partial class ClassGenericIndexerInlineTests
	{
	}

	// Pattern 9: Open generic class stub
	[KnockOff(typeof(ClassGenericIndexer<>))]
	[KnockOff(typeof(ClassGenericIndexerGetAndInit<>))]
	public partial class ClassGenericIndexerOpenGenericTests
	{
	}

	public class ClassGenericIndexerTests
	{
		// ====================================================================
		// Scenario 1: Concrete return indexer (this[int a] → List<string>?)
		// Callback-based return with concrete generic type.
		// ====================================================================

		#region Pattern 3 (Standalone Class): Concrete return indexer

		[Fact]
		public void IntKey_StandaloneClass_ConfiguredReturnsValue()
		{
			var stub = new ClassGenericIndexerStandaloneKnockOff();
			ClassGenericIndexer<int> obj = stub.Object;

			var returnValue = new List<string> { "a", "b" };
			stub.Indexer.Get((int key) => returnValue);

			var result = obj[4];

			Assert.Same(returnValue, result);
		}

		[Fact]
		public void IntKey_StandaloneClass_UnconfiguredFallsToBase()
		{
			var stub = new ClassGenericIndexerStandaloneKnockOff();
			ClassGenericIndexer<int> obj = stub.Object;

			// base indexer returns default => null
			var result = obj[4];

			Assert.Null(result);
		}

		[Fact]
		public void IntKey_StandaloneClass_VerifyTracksAccess()
		{
			var stub = new ClassGenericIndexerStandaloneKnockOff();
			ClassGenericIndexer<int> obj = stub.Object;

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
			var stub = new ClassGenericIndexerGenericStandaloneKnockOff<int>();
			ClassGenericIndexer<int> obj = stub.Object;

			var returnValue = new List<string> { "x", "y" };
			stub.Indexer.Get((int key) => returnValue);

			var result = obj[4];

			Assert.Same(returnValue, result);
		}

		[Fact]
		public void IntKey_GenericStandaloneClass_UnconfiguredFallsToBase()
		{
			var stub = new ClassGenericIndexerGenericStandaloneKnockOff<int>();
			ClassGenericIndexer<int> obj = stub.Object;

			var result = obj[4];

			Assert.Null(result);
		}

		#endregion

		#region Pattern 6 (Inline Class): Concrete return indexer

		[Fact]
		public void IntKey_InlineClass_ConfiguredReturnsValue()
		{
			var stub = new ClassGenericIndexerInlineTests.Stubs.ClassGenericIndexer();
			ClassGenericIndexer<int> obj = stub.Object;

			var returnValue = new List<string> { "c", "d" };
			stub.Indexer.Get((int key) => returnValue);

			var result = obj[4];

			Assert.Same(returnValue, result);
		}

		[Fact]
		public void IntKey_InlineClass_UnconfiguredFallsToBase()
		{
			var stub = new ClassGenericIndexerInlineTests.Stubs.ClassGenericIndexer();
			ClassGenericIndexer<int> obj = stub.Object;

			var result = obj[4];

			Assert.Null(result);
		}

		[Fact]
		public void IntKey_InlineClass_VerifyTracksAccess()
		{
			var stub = new ClassGenericIndexerInlineTests.Stubs.ClassGenericIndexer();
			ClassGenericIndexer<int> obj = stub.Object;

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
			var stub = new ClassGenericIndexerOpenGenericTests.Stubs.ClassGenericIndexer<int>();
			ClassGenericIndexer<int> obj = stub.Object;

			var returnValue = new List<string> { "e", "f" };
			stub.Indexer.Get((int key) => returnValue);

			var result = obj[4];

			Assert.Same(returnValue, result);
		}

		[Fact]
		public void IntKey_OpenGenericClass_UnconfiguredFallsToBase()
		{
			var stub = new ClassGenericIndexerOpenGenericTests.Stubs.ClassGenericIndexer<int>();
			ClassGenericIndexer<int> obj = stub.Object;

			var result = obj[4];

			Assert.Null(result);
		}

		#endregion

		// ====================================================================
		// Scenario 2: T in key indexer (this[int a, T b] → int)
		// Multi-param indexer where T=int appears in the key.
		// When T=int, this becomes this[int a, int b] → int.
		// ====================================================================

		#region Pattern 3 (Standalone Class): T in key indexer

		[Fact]
		public void IntIntKey_StandaloneClass_ConfiguredReturnsValue()
		{
			var stub = new ClassGenericIndexerStandaloneKnockOff();
			ClassGenericIndexer<int> obj = stub.Object;

			stub.Indexer.Get(((int a, int b) key) => key.a + key.b);

			int result = obj[4, 5];

			Assert.Equal(9, result);
		}

		[Fact]
		public void IntIntKey_StandaloneClass_UnconfiguredFallsToBase()
		{
			var stub = new ClassGenericIndexerStandaloneKnockOff();
			ClassGenericIndexer<int> obj = stub.Object;

			// base indexer returns default => 0
			int result = obj[4, 5];

			Assert.Equal(0, result);
		}

		#endregion

		#region Pattern 4 (Generic Standalone Class): T in key indexer

		[Fact]
		public void IntIntKey_GenericStandaloneClass_ConfiguredReturnsValue()
		{
			var stub = new ClassGenericIndexerGenericStandaloneKnockOff<int>();
			ClassGenericIndexer<int> obj = stub.Object;

			stub.Indexer.Get(((int a, int b) key) => key.a * key.b);

			int result = obj[3, 4];

			Assert.Equal(12, result);
		}

		[Fact]
		public void IntIntKey_GenericStandaloneClass_UnconfiguredFallsToBase()
		{
			var stub = new ClassGenericIndexerGenericStandaloneKnockOff<int>();
			ClassGenericIndexer<int> obj = stub.Object;

			int result = obj[4, 5];

			Assert.Equal(0, result);
		}

		#endregion

		#region Pattern 6 (Inline Class): T in key indexer

		[Fact]
		public void IntIntKey_InlineClass_ConfiguredReturnsValue()
		{
			var stub = new ClassGenericIndexerInlineTests.Stubs.ClassGenericIndexer();
			ClassGenericIndexer<int> obj = stub.Object;

			stub.Indexer.Get(((int a, int b) key) => key.a + key.b);

			int result = obj[4, 5];

			Assert.Equal(9, result);
		}

		[Fact]
		public void IntIntKey_InlineClass_UnconfiguredFallsToBase()
		{
			var stub = new ClassGenericIndexerInlineTests.Stubs.ClassGenericIndexer();
			ClassGenericIndexer<int> obj = stub.Object;

			int result = obj[4, 5];

			Assert.Equal(0, result);
		}

		#endregion

		#region Pattern 9 (Open Generic Class): T in key indexer

		[Fact]
		public void IntIntKey_OpenGenericClass_ConfiguredReturnsValue()
		{
			var stub = new ClassGenericIndexerOpenGenericTests.Stubs.ClassGenericIndexer<int>();
			ClassGenericIndexer<int> obj = stub.Object;

			stub.Indexer.Get(((int a, int b) key) => key.a * key.b);

			int result = obj[3, 4];

			Assert.Equal(12, result);
		}

		[Fact]
		public void IntIntKey_OpenGenericClass_UnconfiguredFallsToBase()
		{
			var stub = new ClassGenericIndexerOpenGenericTests.Stubs.ClassGenericIndexer<int>();
			ClassGenericIndexer<int> obj = stub.Object;

			int result = obj[4, 5];

			Assert.Equal(0, result);
		}

		#endregion

		// ====================================================================
		// Scenario 3: T as return indexer (this[string a] → T?)
		// String key, generic return type. When T=int, returns int?.
		// ====================================================================

		#region Pattern 3 (Standalone Class): T as return indexer

		[Fact]
		public void StringKey_StandaloneClass_ConfiguredReturnsValue()
		{
			var stub = new ClassGenericIndexerStandaloneKnockOff();
			ClassGenericIndexer<int> obj = stub.Object;

			stub.Indexer.Get((string key) => key.Length);

			var result = obj["hello"];

			Assert.Equal(5, result);
		}

		[Fact]
		public void StringKey_StandaloneClass_UnconfiguredFallsToBase()
		{
			var stub = new ClassGenericIndexerStandaloneKnockOff();
			ClassGenericIndexer<int> obj = stub.Object;

			// base indexer returns default => null for T? (0 for int? cast scenario)
			var result = obj["hello"];

			Assert.Equal(default, result);
		}

		#endregion

		#region Pattern 4 (Generic Standalone Class): T as return indexer

		[Fact]
		public void StringKey_GenericStandaloneClass_ConfiguredReturnsValue()
		{
			var stub = new ClassGenericIndexerGenericStandaloneKnockOff<int>();
			ClassGenericIndexer<int> obj = stub.Object;

			stub.Indexer.Get((string key) => key.Length * 2);

			var result = obj["hi"];

			Assert.Equal(4, result);
		}

		[Fact]
		public void StringKey_GenericStandaloneClass_UnconfiguredFallsToBase()
		{
			var stub = new ClassGenericIndexerGenericStandaloneKnockOff<int>();
			ClassGenericIndexer<int> obj = stub.Object;

			var result = obj["hello"];

			Assert.Equal(default, result);
		}

		#endregion

		#region Pattern 6 (Inline Class): T as return indexer

		[Fact]
		public void StringKey_InlineClass_ConfiguredReturnsValue()
		{
			var stub = new ClassGenericIndexerInlineTests.Stubs.ClassGenericIndexer();
			ClassGenericIndexer<int> obj = stub.Object;

			stub.Indexer.Get((string key) => key.Length);

			var result = obj["hello"];

			Assert.Equal(5, result);
		}

		[Fact]
		public void StringKey_InlineClass_UnconfiguredFallsToBase()
		{
			var stub = new ClassGenericIndexerInlineTests.Stubs.ClassGenericIndexer();
			ClassGenericIndexer<int> obj = stub.Object;

			var result = obj["hello"];

			Assert.Equal(default, result);
		}

		#endregion

		#region Pattern 9 (Open Generic Class): T as return indexer

		[Fact]
		public void StringKey_OpenGenericClass_ConfiguredReturnsValue()
		{
			var stub = new ClassGenericIndexerOpenGenericTests.Stubs.ClassGenericIndexer<int>();
			ClassGenericIndexer<int> obj = stub.Object;

			stub.Indexer.Get((string key) => key.Length * 2);

			var result = obj["hi"];

			Assert.Equal(4, result);
		}

		[Fact]
		public void StringKey_OpenGenericClass_UnconfiguredFallsToBase()
		{
			var stub = new ClassGenericIndexerOpenGenericTests.Stubs.ClassGenericIndexer<int>();
			ClassGenericIndexer<int> obj = stub.Object;

			var result = obj["hello"];

			Assert.Equal(default, result);
		}

		#endregion

		// ====================================================================
		// Scenario 4: Init variant — concrete return indexer
		// ClassGenericIndexerGetAndInit<T>.this[int a] { get; init; }
		// ====================================================================

		#region Pattern 3 (Standalone Class): GetAndInit concrete return indexer

		[Fact]
		public void IntKeyGetInit_StandaloneClass_ConfiguredReturnsValue()
		{
			var stub = new ClassGenericIndexerGetAndInitStandaloneKnockOff();
			ClassGenericIndexerGetAndInit<int> obj = stub.Object;

			var returnValue = new List<string> { "init-a" };
			stub.Indexer.Get((int key) => returnValue);

			var result = obj[4];

			Assert.Same(returnValue, result);
		}

		[Fact]
		public void IntKeyGetInit_StandaloneClass_UnconfiguredFallsToBase()
		{
			var stub = new ClassGenericIndexerGetAndInitStandaloneKnockOff();
			ClassGenericIndexerGetAndInit<int> obj = stub.Object;

			var result = obj[4];

			Assert.Null(result);
		}

		#endregion

		#region Pattern 4 (Generic Standalone Class): GetAndInit concrete return indexer

		[Fact]
		public void IntKeyGetInit_GenericStandaloneClass_ConfiguredReturnsValue()
		{
			var stub = new ClassGenericIndexerGetAndInitGenericStandaloneKnockOff<int>();
			ClassGenericIndexerGetAndInit<int> obj = stub.Object;

			var returnValue = new List<string> { "init-b" };
			stub.Indexer.Get((int key) => returnValue);

			var result = obj[4];

			Assert.Same(returnValue, result);
		}

		[Fact]
		public void IntKeyGetInit_GenericStandaloneClass_UnconfiguredFallsToBase()
		{
			var stub = new ClassGenericIndexerGetAndInitGenericStandaloneKnockOff<int>();
			ClassGenericIndexerGetAndInit<int> obj = stub.Object;

			var result = obj[4];

			Assert.Null(result);
		}

		#endregion

		#region Pattern 6 (Inline Class): GetAndInit concrete return indexer

		[Fact]
		public void IntKeyGetInit_InlineClass_ConfiguredReturnsValue()
		{
			var stub = new ClassGenericIndexerInlineTests.Stubs.ClassGenericIndexerGetAndInit();
			ClassGenericIndexerGetAndInit<int> obj = stub.Object;

			var returnValue = new List<string> { "init-c" };
			stub.Indexer.Get((int key) => returnValue);

			var result = obj[4];

			Assert.Same(returnValue, result);
		}

		[Fact]
		public void IntKeyGetInit_InlineClass_UnconfiguredFallsToBase()
		{
			var stub = new ClassGenericIndexerInlineTests.Stubs.ClassGenericIndexerGetAndInit();
			ClassGenericIndexerGetAndInit<int> obj = stub.Object;

			var result = obj[4];

			Assert.Null(result);
		}

		#endregion

		#region Pattern 9 (Open Generic Class): GetAndInit concrete return indexer

		[Fact]
		public void IntKeyGetInit_OpenGenericClass_ConfiguredReturnsValue()
		{
			var stub = new ClassGenericIndexerOpenGenericTests.Stubs.ClassGenericIndexerGetAndInit<int>();
			ClassGenericIndexerGetAndInit<int> obj = stub.Object;

			var returnValue = new List<string> { "init-d" };
			stub.Indexer.Get((int key) => returnValue);

			var result = obj[4];

			Assert.Same(returnValue, result);
		}

		[Fact]
		public void IntKeyGetInit_OpenGenericClass_UnconfiguredFallsToBase()
		{
			var stub = new ClassGenericIndexerOpenGenericTests.Stubs.ClassGenericIndexerGetAndInit<int>();
			ClassGenericIndexerGetAndInit<int> obj = stub.Object;

			var result = obj[4];

			Assert.Null(result);
		}

		#endregion

		// ====================================================================
		// Scenario 5: Init variant — T in key indexer
		// ClassGenericIndexerGetAndInit<T>.this[int a, T b] { get; init; }
		// ====================================================================

		#region Pattern 3 (Standalone Class): GetAndInit T in key indexer

		[Fact]
		public void IntIntKeyGetInit_StandaloneClass_ConfiguredReturnsValue()
		{
			var stub = new ClassGenericIndexerGetAndInitStandaloneKnockOff();
			ClassGenericIndexerGetAndInit<int> obj = stub.Object;

			stub.Indexer.Get(((int a, int b) key) => key.a + key.b);

			int result = obj[4, 5];

			Assert.Equal(9, result);
		}

		#endregion

		#region Pattern 4 (Generic Standalone Class): GetAndInit T in key indexer

		[Fact]
		public void IntIntKeyGetInit_GenericStandaloneClass_ConfiguredReturnsValue()
		{
			var stub = new ClassGenericIndexerGetAndInitGenericStandaloneKnockOff<int>();
			ClassGenericIndexerGetAndInit<int> obj = stub.Object;

			stub.Indexer.Get(((int a, int b) key) => key.a * key.b);

			int result = obj[3, 4];

			Assert.Equal(12, result);
		}

		#endregion

		#region Pattern 6 (Inline Class): GetAndInit T in key indexer

		[Fact]
		public void IntIntKeyGetInit_InlineClass_ConfiguredReturnsValue()
		{
			var stub = new ClassGenericIndexerInlineTests.Stubs.ClassGenericIndexerGetAndInit();
			ClassGenericIndexerGetAndInit<int> obj = stub.Object;

			stub.Indexer.Get(((int a, int b) key) => key.a + key.b);

			int result = obj[4, 5];

			Assert.Equal(9, result);
		}

		#endregion

		#region Pattern 9 (Open Generic Class): GetAndInit T in key indexer

		[Fact]
		public void IntIntKeyGetInit_OpenGenericClass_ConfiguredReturnsValue()
		{
			var stub = new ClassGenericIndexerOpenGenericTests.Stubs.ClassGenericIndexerGetAndInit<int>();
			ClassGenericIndexerGetAndInit<int> obj = stub.Object;

			stub.Indexer.Get(((int a, int b) key) => key.a * key.b);

			int result = obj[3, 4];

			Assert.Equal(12, result);
		}

		#endregion

		// ====================================================================
		// Scenario 6: Init variant — T as return indexer
		// ClassGenericIndexerGetAndInit<T>.this[string a] { get; init; }
		// ====================================================================

		#region Pattern 3 (Standalone Class): GetAndInit T as return indexer

		[Fact]
		public void StringKeyGetInit_StandaloneClass_ConfiguredReturnsValue()
		{
			var stub = new ClassGenericIndexerGetAndInitStandaloneKnockOff();
			ClassGenericIndexerGetAndInit<int> obj = stub.Object;

			stub.Indexer.Get((string key) => key.Length);

			var result = obj["hello"];

			Assert.Equal(5, result);
		}

		[Fact]
		public void StringKeyGetInit_StandaloneClass_UnconfiguredFallsToBase()
		{
			var stub = new ClassGenericIndexerGetAndInitStandaloneKnockOff();
			ClassGenericIndexerGetAndInit<int> obj = stub.Object;

			var result = obj["hello"];

			Assert.Equal(default, result);
		}

		#endregion

		#region Pattern 4 (Generic Standalone Class): GetAndInit T as return indexer

		[Fact]
		public void StringKeyGetInit_GenericStandaloneClass_ConfiguredReturnsValue()
		{
			var stub = new ClassGenericIndexerGetAndInitGenericStandaloneKnockOff<int>();
			ClassGenericIndexerGetAndInit<int> obj = stub.Object;

			stub.Indexer.Get((string key) => key.Length * 2);

			var result = obj["hi"];

			Assert.Equal(4, result);
		}

		#endregion

		#region Pattern 6 (Inline Class): GetAndInit T as return indexer

		[Fact]
		public void StringKeyGetInit_InlineClass_ConfiguredReturnsValue()
		{
			var stub = new ClassGenericIndexerInlineTests.Stubs.ClassGenericIndexerGetAndInit();
			ClassGenericIndexerGetAndInit<int> obj = stub.Object;

			stub.Indexer.Get((string key) => key.Length);

			var result = obj["hello"];

			Assert.Equal(5, result);
		}

		[Fact]
		public void StringKeyGetInit_InlineClass_UnconfiguredFallsToBase()
		{
			var stub = new ClassGenericIndexerInlineTests.Stubs.ClassGenericIndexerGetAndInit();
			ClassGenericIndexerGetAndInit<int> obj = stub.Object;

			var result = obj["hello"];

			Assert.Equal(default, result);
		}

		#endregion

		#region Pattern 9 (Open Generic Class): GetAndInit T as return indexer

		[Fact]
		public void StringKeyGetInit_OpenGenericClass_ConfiguredReturnsValue()
		{
			var stub = new ClassGenericIndexerOpenGenericTests.Stubs.ClassGenericIndexerGetAndInit<int>();
			ClassGenericIndexerGetAndInit<int> obj = stub.Object;

			stub.Indexer.Get((string key) => key.Length * 2);

			var result = obj["hi"];

			Assert.Equal(4, result);
		}

		[Fact]
		public void StringKeyGetInit_OpenGenericClass_UnconfiguredFallsToBase()
		{
			var stub = new ClassGenericIndexerOpenGenericTests.Stubs.ClassGenericIndexerGetAndInit<int>();
			ClassGenericIndexerGetAndInit<int> obj = stub.Object;

			var result = obj["hello"];

			Assert.Equal(default, result);
		}

		#endregion
	}
}
