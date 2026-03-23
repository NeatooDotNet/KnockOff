// ============================================================================
// ClassGenericPropertyTests: Edge case tests for a generic class with
// properties that use the class-level type parameter.
// Inspired by Rocks.Analysis.IntegrationTests.ClassGenericPropertyTests
//
// Target classes:
//   public class ClassGenericProperty<T>
//   {
//       public virtual List<string> Values => default!;  // concrete generic type
//       public virtual T Data => default!;                // class type parameter
//   }
//
//   public class ClassGenericPropertyGetAndInit<T>
//   {
//       public virtual List<string> Values { get => default!; init { } }
//       public virtual T Data { get => default!; init { } }
//   }
//
// Scenarios:
// 1. Concrete generic property (Values: List<string>) — configure return, assert
// 2. Class type param property (Data: T→int) — configure return, assert
// 3. Unconfigured fallback to base — verify default returns
// 4. Init variant — configured and unconfigured
// 5. Verify tracking — VerifyGet counts access
// 6. Callback on property get — computed return value
//
// Applicable patterns (generic class):
// - Pattern 3 (Standalone Class): [KnockOffBase<ClassGenericProperty<int>>]
// - Pattern 4 (Generic Standalone Class): [KnockOffBase(typeof(ClassGenericProperty<>))]
// - Pattern 6 (Inline Class): [KnockOff<ClassGenericProperty<int>>]
// - Pattern 9 (Open Generic Class): [KnockOff(typeof(ClassGenericProperty<>))]
// ============================================================================

using KnockOff;

// ============================================================================
// TYPE DEFINITIONS
// ============================================================================

namespace KnockOff.Analysis.Tests.ClassGenericPropertyTestTypes
{
	public class ClassGenericProperty<T>
	{
		public virtual List<string> Values => default!;
		public virtual T Data => default!;
	}

	public class ClassGenericPropertyGetAndInit<T>
	{
		public virtual List<string> Values { get => default!; init { } }
		public virtual T Data { get => default!; init { } }
	}

	// Pattern 3: Standalone class stub (closed generic)
	[KnockOffBase<ClassGenericProperty<int>>]
	public partial class ClassGenericPropertyStandaloneKnockOff
	{
	}

	// Pattern 3: Standalone class stub for GetAndInit variant
	[KnockOffBase<ClassGenericPropertyGetAndInit<int>>]
	public partial class ClassGenericPropertyGetAndInitStandaloneKnockOff
	{
	}

	// Pattern 4: Generic standalone class stub (open generic)
	[KnockOffBase(typeof(ClassGenericProperty<>))]
	public partial class ClassGenericPropertyGenericStandaloneKnockOff<T>
	{
	}

	// Pattern 4: Generic standalone class stub for GetAndInit variant
	[KnockOffBase(typeof(ClassGenericPropertyGetAndInit<>))]
	public partial class ClassGenericPropertyGetAndInitGenericStandaloneKnockOff<T>
	{
	}
}

// ============================================================================
// INLINE PATTERN DECLARATIONS + TESTS
// ============================================================================

namespace KnockOff.Analysis.Tests
{
	using KnockOff.Analysis.Tests.ClassGenericPropertyTestTypes;

	// Pattern 6: Inline class stub (closed generic)
	[KnockOff<ClassGenericProperty<int>>]
	[KnockOff<ClassGenericPropertyGetAndInit<int>>]
	public partial class ClassGenericPropertyInlineTests
	{
	}

	// Pattern 9: Open generic class stub
	[KnockOff(typeof(ClassGenericProperty<>))]
	[KnockOff(typeof(ClassGenericPropertyGetAndInit<>))]
	public partial class ClassGenericPropertyOpenGenericTests
	{
	}

	public class ClassGenericPropertyTests
	{
		// ====================================================================
		// Scenario 1: Concrete generic property (Values: List<string>)
		// Configure return, read, assert
		// ====================================================================

		#region Pattern 3 (Standalone Class): Concrete generic property (Values)

		[Fact]
		public void Values_StandaloneClass_ConfiguredReturnsValue()
		{
			var stub = new ClassGenericPropertyStandaloneKnockOff();
			ClassGenericProperty<int> obj = stub.Object;

			var returnValue = new List<string> { "a", "b" };
			stub.Values.Get(returnValue);

			var result = obj.Values;

			Assert.Same(returnValue, result);
		}

		[Fact]
		public void Values_StandaloneClass_UnconfiguredFallsToBase()
		{
			var stub = new ClassGenericPropertyStandaloneKnockOff();
			ClassGenericProperty<int> obj = stub.Object;

			// base.Values => default! => null
			var result = obj.Values;

			Assert.Null(result);
		}

		[Fact]
		public void Values_StandaloneClass_VerifyTracksAccess()
		{
			var stub = new ClassGenericPropertyStandaloneKnockOff();
			ClassGenericProperty<int> obj = stub.Object;

			var returnValue = new List<string>();
			stub.Values.Get(returnValue);
			_ = obj.Values;
			_ = obj.Values;

			stub.Values.VerifyGet(Called.Exactly(2));
		}

		#endregion

		#region Pattern 4 (Generic Standalone Class): Concrete generic property (Values)

		[Fact]
		public void Values_GenericStandaloneClass_ConfiguredReturnsValue()
		{
			var stub = new ClassGenericPropertyGenericStandaloneKnockOff<int>();
			ClassGenericProperty<int> obj = stub.Object;

			var returnValue = new List<string> { "x", "y" };
			stub.Values.Get(returnValue);

			var result = obj.Values;

			Assert.Same(returnValue, result);
		}

		[Fact]
		public void Values_GenericStandaloneClass_UnconfiguredFallsToBase()
		{
			var stub = new ClassGenericPropertyGenericStandaloneKnockOff<int>();
			ClassGenericProperty<int> obj = stub.Object;

			var result = obj.Values;

			Assert.Null(result);
		}

		#endregion

		#region Pattern 6 (Inline Class): Concrete generic property (Values)

		[Fact]
		public void Values_InlineClass_ConfiguredReturnsValue()
		{
			var stub = new ClassGenericPropertyInlineTests.Stubs.ClassGenericProperty();
			ClassGenericProperty<int> obj = stub.Object;

			var returnValue = new List<string> { "c", "d" };
			stub.Values.Get(returnValue);

			var result = obj.Values;

			Assert.Same(returnValue, result);
		}

		[Fact]
		public void Values_InlineClass_UnconfiguredFallsToBase()
		{
			var stub = new ClassGenericPropertyInlineTests.Stubs.ClassGenericProperty();
			ClassGenericProperty<int> obj = stub.Object;

			var result = obj.Values;

			Assert.Null(result);
		}

		[Fact]
		public void Values_InlineClass_VerifyTracksAccess()
		{
			var stub = new ClassGenericPropertyInlineTests.Stubs.ClassGenericProperty();
			ClassGenericProperty<int> obj = stub.Object;

			var returnValue = new List<string>();
			stub.Values.Get(returnValue);
			_ = obj.Values;
			_ = obj.Values;

			stub.Values.VerifyGet(Called.Exactly(2));
		}

		#endregion

		#region Pattern 9 (Open Generic Class): Concrete generic property (Values)

		[Fact]
		public void Values_OpenGenericClass_ConfiguredReturnsValue()
		{
			var stub = new ClassGenericPropertyOpenGenericTests.Stubs.ClassGenericProperty<int>();
			ClassGenericProperty<int> obj = stub.Object;

			var returnValue = new List<string> { "e", "f" };
			stub.Values.Get(returnValue);

			var result = obj.Values;

			Assert.Same(returnValue, result);
		}

		[Fact]
		public void Values_OpenGenericClass_UnconfiguredFallsToBase()
		{
			var stub = new ClassGenericPropertyOpenGenericTests.Stubs.ClassGenericProperty<int>();
			ClassGenericProperty<int> obj = stub.Object;

			var result = obj.Values;

			Assert.Null(result);
		}

		#endregion

		// ====================================================================
		// Scenario 2: Class type param property (Data: T→int)
		// Configure return with the closed type, read, assert
		// ====================================================================

		#region Pattern 3 (Standalone Class): Class type param property (Data)

		[Fact]
		public void Data_StandaloneClass_ConfiguredReturnsValue()
		{
			var stub = new ClassGenericPropertyStandaloneKnockOff();
			ClassGenericProperty<int> obj = stub.Object;

			stub.Data.Get(42);

			int result = obj.Data;

			Assert.Equal(42, result);
		}

		[Fact]
		public void Data_StandaloneClass_UnconfiguredFallsToBase()
		{
			var stub = new ClassGenericPropertyStandaloneKnockOff();
			ClassGenericProperty<int> obj = stub.Object;

			// base.Data => default! => 0 for int
			int result = obj.Data;

			Assert.Equal(0, result);
		}

		[Fact]
		public void Data_StandaloneClass_VerifyTracksAccess()
		{
			var stub = new ClassGenericPropertyStandaloneKnockOff();
			ClassGenericProperty<int> obj = stub.Object;

			stub.Data.Get(1);
			_ = obj.Data;
			_ = obj.Data;

			stub.Data.VerifyGet(Called.Exactly(2));
		}

		#endregion

		#region Pattern 4 (Generic Standalone Class): Class type param property (Data)

		[Fact]
		public void Data_GenericStandaloneClass_ConfiguredReturnsValue()
		{
			var stub = new ClassGenericPropertyGenericStandaloneKnockOff<int>();
			ClassGenericProperty<int> obj = stub.Object;

			stub.Data.Get(99);

			int result = obj.Data;

			Assert.Equal(99, result);
		}

		[Fact]
		public void Data_GenericStandaloneClass_UnconfiguredFallsToBase()
		{
			var stub = new ClassGenericPropertyGenericStandaloneKnockOff<int>();
			ClassGenericProperty<int> obj = stub.Object;

			int result = obj.Data;

			Assert.Equal(0, result);
		}

		#endregion

		#region Pattern 6 (Inline Class): Class type param property (Data)

		[Fact]
		public void Data_InlineClass_ConfiguredReturnsValue()
		{
			var stub = new ClassGenericPropertyInlineTests.Stubs.ClassGenericProperty();
			ClassGenericProperty<int> obj = stub.Object;

			stub.Data.Get(42);

			int result = obj.Data;

			Assert.Equal(42, result);
		}

		[Fact]
		public void Data_InlineClass_UnconfiguredFallsToBase()
		{
			var stub = new ClassGenericPropertyInlineTests.Stubs.ClassGenericProperty();
			ClassGenericProperty<int> obj = stub.Object;

			int result = obj.Data;

			Assert.Equal(0, result);
		}

		[Fact]
		public void Data_InlineClass_VerifyTracksAccess()
		{
			var stub = new ClassGenericPropertyInlineTests.Stubs.ClassGenericProperty();
			ClassGenericProperty<int> obj = stub.Object;

			stub.Data.Get(1);
			_ = obj.Data;
			_ = obj.Data;

			stub.Data.VerifyGet(Called.Exactly(2));
		}

		#endregion

		#region Pattern 9 (Open Generic Class): Class type param property (Data)

		[Fact]
		public void Data_OpenGenericClass_ConfiguredReturnsValue()
		{
			var stub = new ClassGenericPropertyOpenGenericTests.Stubs.ClassGenericProperty<int>();
			ClassGenericProperty<int> obj = stub.Object;

			stub.Data.Get(99);

			int result = obj.Data;

			Assert.Equal(99, result);
		}

		[Fact]
		public void Data_OpenGenericClass_UnconfiguredFallsToBase()
		{
			var stub = new ClassGenericPropertyOpenGenericTests.Stubs.ClassGenericProperty<int>();
			ClassGenericProperty<int> obj = stub.Object;

			int result = obj.Data;

			Assert.Equal(0, result);
		}

		#endregion

		// ====================================================================
		// Scenario 3: Get/Init variant — concrete generic property
		// ClassGenericPropertyGetAndInit<T>.Values { get; init; }
		// ====================================================================

		#region Pattern 3 (Standalone Class): Get/Init Values

		[Fact]
		public void ValuesGetInit_StandaloneClass_ConfiguredReturnsValue()
		{
			var stub = new ClassGenericPropertyGetAndInitStandaloneKnockOff();
			ClassGenericPropertyGetAndInit<int> obj = stub.Object;

			var returnValue = new List<string> { "init-a" };
			stub.Values.Get(returnValue);

			var result = obj.Values;

			Assert.Same(returnValue, result);
		}

		[Fact]
		public void ValuesGetInit_StandaloneClass_UnconfiguredFallsToBase()
		{
			var stub = new ClassGenericPropertyGetAndInitStandaloneKnockOff();
			ClassGenericPropertyGetAndInit<int> obj = stub.Object;

			var result = obj.Values;

			Assert.Null(result);
		}

		#endregion

		#region Pattern 4 (Generic Standalone Class): Get/Init Values

		[Fact]
		public void ValuesGetInit_GenericStandaloneClass_ConfiguredReturnsValue()
		{
			var stub = new ClassGenericPropertyGetAndInitGenericStandaloneKnockOff<int>();
			ClassGenericPropertyGetAndInit<int> obj = stub.Object;

			var returnValue = new List<string> { "init-b" };
			stub.Values.Get(returnValue);

			var result = obj.Values;

			Assert.Same(returnValue, result);
		}

		[Fact]
		public void ValuesGetInit_GenericStandaloneClass_UnconfiguredFallsToBase()
		{
			var stub = new ClassGenericPropertyGetAndInitGenericStandaloneKnockOff<int>();
			ClassGenericPropertyGetAndInit<int> obj = stub.Object;

			var result = obj.Values;

			Assert.Null(result);
		}

		#endregion

		#region Pattern 6 (Inline Class): Get/Init Values

		[Fact]
		public void ValuesGetInit_InlineClass_ConfiguredReturnsValue()
		{
			var stub = new ClassGenericPropertyInlineTests.Stubs.ClassGenericPropertyGetAndInit();
			ClassGenericPropertyGetAndInit<int> obj = stub.Object;

			var returnValue = new List<string> { "init-c" };
			stub.Values.Get(returnValue);

			var result = obj.Values;

			Assert.Same(returnValue, result);
		}

		[Fact]
		public void ValuesGetInit_InlineClass_UnconfiguredFallsToBase()
		{
			var stub = new ClassGenericPropertyInlineTests.Stubs.ClassGenericPropertyGetAndInit();
			ClassGenericPropertyGetAndInit<int> obj = stub.Object;

			var result = obj.Values;

			Assert.Null(result);
		}

		#endregion

		#region Pattern 9 (Open Generic Class): Get/Init Values

		[Fact]
		public void ValuesGetInit_OpenGenericClass_ConfiguredReturnsValue()
		{
			var stub = new ClassGenericPropertyOpenGenericTests.Stubs.ClassGenericPropertyGetAndInit<int>();
			ClassGenericPropertyGetAndInit<int> obj = stub.Object;

			var returnValue = new List<string> { "init-d" };
			stub.Values.Get(returnValue);

			var result = obj.Values;

			Assert.Same(returnValue, result);
		}

		[Fact]
		public void ValuesGetInit_OpenGenericClass_UnconfiguredFallsToBase()
		{
			var stub = new ClassGenericPropertyOpenGenericTests.Stubs.ClassGenericPropertyGetAndInit<int>();
			ClassGenericPropertyGetAndInit<int> obj = stub.Object;

			var result = obj.Values;

			Assert.Null(result);
		}

		#endregion

		// ====================================================================
		// Scenario 4: Get/Init variant — class type param property
		// ClassGenericPropertyGetAndInit<T>.Data { get; init; }
		// ====================================================================

		#region Pattern 3 (Standalone Class): Get/Init Data

		[Fact]
		public void DataGetInit_StandaloneClass_ConfiguredReturnsValue()
		{
			var stub = new ClassGenericPropertyGetAndInitStandaloneKnockOff();
			ClassGenericPropertyGetAndInit<int> obj = stub.Object;

			stub.Data.Get(77);

			int result = obj.Data;

			Assert.Equal(77, result);
		}

		[Fact]
		public void DataGetInit_StandaloneClass_UnconfiguredFallsToBase()
		{
			var stub = new ClassGenericPropertyGetAndInitStandaloneKnockOff();
			ClassGenericPropertyGetAndInit<int> obj = stub.Object;

			int result = obj.Data;

			Assert.Equal(0, result);
		}

		#endregion

		#region Pattern 4 (Generic Standalone Class): Get/Init Data

		[Fact]
		public void DataGetInit_GenericStandaloneClass_ConfiguredReturnsValue()
		{
			var stub = new ClassGenericPropertyGetAndInitGenericStandaloneKnockOff<int>();
			ClassGenericPropertyGetAndInit<int> obj = stub.Object;

			stub.Data.Get(88);

			int result = obj.Data;

			Assert.Equal(88, result);
		}

		[Fact]
		public void DataGetInit_GenericStandaloneClass_UnconfiguredFallsToBase()
		{
			var stub = new ClassGenericPropertyGetAndInitGenericStandaloneKnockOff<int>();
			ClassGenericPropertyGetAndInit<int> obj = stub.Object;

			int result = obj.Data;

			Assert.Equal(0, result);
		}

		#endregion

		#region Pattern 6 (Inline Class): Get/Init Data

		[Fact]
		public void DataGetInit_InlineClass_ConfiguredReturnsValue()
		{
			var stub = new ClassGenericPropertyInlineTests.Stubs.ClassGenericPropertyGetAndInit();
			ClassGenericPropertyGetAndInit<int> obj = stub.Object;

			stub.Data.Get(77);

			int result = obj.Data;

			Assert.Equal(77, result);
		}

		[Fact]
		public void DataGetInit_InlineClass_UnconfiguredFallsToBase()
		{
			var stub = new ClassGenericPropertyInlineTests.Stubs.ClassGenericPropertyGetAndInit();
			ClassGenericPropertyGetAndInit<int> obj = stub.Object;

			int result = obj.Data;

			Assert.Equal(0, result);
		}

		#endregion

		#region Pattern 9 (Open Generic Class): Get/Init Data

		[Fact]
		public void DataGetInit_OpenGenericClass_ConfiguredReturnsValue()
		{
			var stub = new ClassGenericPropertyOpenGenericTests.Stubs.ClassGenericPropertyGetAndInit<int>();
			ClassGenericPropertyGetAndInit<int> obj = stub.Object;

			stub.Data.Get(88);

			int result = obj.Data;

			Assert.Equal(88, result);
		}

		[Fact]
		public void DataGetInit_OpenGenericClass_UnconfiguredFallsToBase()
		{
			var stub = new ClassGenericPropertyOpenGenericTests.Stubs.ClassGenericPropertyGetAndInit<int>();
			ClassGenericPropertyGetAndInit<int> obj = stub.Object;

			int result = obj.Data;

			Assert.Equal(0, result);
		}

		#endregion

		// ====================================================================
		// Scenario 5: Callback on property get — computed return value
		// Verify dynamic callback works with generic type param return.
		// ====================================================================

		#region Pattern 3 (Standalone Class): Callback on Data get

		[Fact]
		public void Data_StandaloneClass_CallbackReturnsComputedValue()
		{
			var stub = new ClassGenericPropertyStandaloneKnockOff();
			ClassGenericProperty<int> obj = stub.Object;

			var accessCount = 0;
			stub.Data.Get(() =>
			{
				accessCount++;
				return accessCount * 10;
			});

			int first = obj.Data;
			int second = obj.Data;

			Assert.Equal(10, first);
			Assert.Equal(20, second);
		}

		#endregion

		#region Pattern 4 (Generic Standalone Class): Callback on Data get

		[Fact]
		public void Data_GenericStandaloneClass_CallbackReturnsComputedValue()
		{
			var stub = new ClassGenericPropertyGenericStandaloneKnockOff<int>();
			ClassGenericProperty<int> obj = stub.Object;

			var accessCount = 0;
			stub.Data.Get(() =>
			{
				accessCount++;
				return accessCount * 5;
			});

			int first = obj.Data;
			int second = obj.Data;

			Assert.Equal(5, first);
			Assert.Equal(10, second);
		}

		#endregion

		#region Pattern 6 (Inline Class): Callback on Data get

		[Fact]
		public void Data_InlineClass_CallbackReturnsComputedValue()
		{
			var stub = new ClassGenericPropertyInlineTests.Stubs.ClassGenericProperty();
			ClassGenericProperty<int> obj = stub.Object;

			var accessCount = 0;
			stub.Data.Get(() =>
			{
				accessCount++;
				return accessCount * 10;
			});

			int first = obj.Data;
			int second = obj.Data;

			Assert.Equal(10, first);
			Assert.Equal(20, second);
		}

		#endregion

		#region Pattern 9 (Open Generic Class): Callback on Data get

		[Fact]
		public void Data_OpenGenericClass_CallbackReturnsComputedValue()
		{
			var stub = new ClassGenericPropertyOpenGenericTests.Stubs.ClassGenericProperty<int>();
			ClassGenericProperty<int> obj = stub.Object;

			var accessCount = 0;
			stub.Data.Get(() =>
			{
				accessCount++;
				return accessCount * 5;
			});

			int first = obj.Data;
			int second = obj.Data;

			Assert.Equal(5, first);
			Assert.Equal(10, second);
		}

		#endregion
	}
}
