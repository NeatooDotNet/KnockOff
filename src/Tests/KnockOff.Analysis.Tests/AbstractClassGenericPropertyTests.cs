// ============================================================================
// AbstractClassGenericPropertyTests: Edge case tests for a generic abstract class
// with properties that use the class-level type parameter.
// Inspired by Rocks.Analysis.IntegrationTests.AbstractClassGenericPropertyTests
//
// Target classes:
//   public abstract class AbstractClassGenericProperty<T>
//   {
//       public abstract List<string> Values { get; }
//       public abstract T Data { get; }
//   }
//
//   public abstract class AbstractClassGenericPropertyGetAndInit<T>
//   {
//       public abstract List<string> Values { get; init; }
//       public abstract T Data { get; init; }
//   }
//
// Key difference from ClassGenericPropertyTests: members are ABSTRACT, not virtual.
// Abstract members have no base implementation, so unconfigured calls return default(T).
//
// Applicable patterns (generic class):
// - Pattern 3 (Standalone Class): [KnockOffBase<AbstractClassGenericProperty<int>>]
// - Pattern 4 (Generic Standalone Class): [KnockOffBase(typeof(AbstractClassGenericProperty<>))]
// - Pattern 6 (Inline Class): [KnockOff<AbstractClassGenericProperty<int>>]
// - Pattern 9 (Open Generic Class): [KnockOff(typeof(AbstractClassGenericProperty<>))]
// ============================================================================

using KnockOff;

// ============================================================================
// TYPE DEFINITIONS
// ============================================================================

namespace KnockOff.Analysis.Tests.AbstractClassGenericPropertyTestTypes
{
	public abstract class AbstractClassGenericProperty<T>
	{
		public abstract List<string> Values { get; }
		public abstract T Data { get; }
	}

	public abstract class AbstractClassGenericPropertyGetAndInit<T>
	{
		public abstract List<string> Values { get; init; }
		public abstract T Data { get; init; }
	}

	// Pattern 3: Standalone class stub (closed generic)
	[KnockOffBase<AbstractClassGenericProperty<int>>]
	public partial class AbstractClassGenericPropertyStandaloneKnockOff
	{
	}

	// Pattern 3: Standalone class stub for GetAndInit variant
	[KnockOffBase<AbstractClassGenericPropertyGetAndInit<int>>]
	public partial class AbstractClassGenericPropertyGetAndInitStandaloneKnockOff
	{
	}

	// Pattern 4: Generic standalone class stub (open generic)
	[KnockOffBase(typeof(AbstractClassGenericProperty<>))]
	public partial class AbstractClassGenericPropertyGenericStandaloneKnockOff<T>
	{
	}

	// Pattern 4: Generic standalone class stub for GetAndInit variant
	[KnockOffBase(typeof(AbstractClassGenericPropertyGetAndInit<>))]
	public partial class AbstractClassGenericPropertyGetAndInitGenericStandaloneKnockOff<T>
	{
	}
}

// ============================================================================
// INLINE PATTERN DECLARATIONS + TESTS
// ============================================================================

namespace KnockOff.Analysis.Tests
{
	using KnockOff.Analysis.Tests.AbstractClassGenericPropertyTestTypes;

	// Pattern 6: Inline class stub (closed generic)
	[KnockOff<AbstractClassGenericProperty<int>>]
	[KnockOff<AbstractClassGenericPropertyGetAndInit<int>>]
	public partial class AbstractClassGenericPropertyInlineTests
	{
	}

	// Pattern 9: Open generic class stub
	[KnockOff(typeof(AbstractClassGenericProperty<>))]
	[KnockOff(typeof(AbstractClassGenericPropertyGetAndInit<>))]
	public partial class AbstractClassGenericPropertyOpenGenericTests
	{
	}

	public class AbstractClassGenericPropertyTests
	{
		// ====================================================================
		// Scenario 1: Concrete generic property (Values: List<string>)
		// ====================================================================

		#region Pattern 3 (Standalone Class): Concrete generic property (Values)

		[Fact]
		public void Values_StandaloneClass_ConfiguredReturnsValue()
		{
			var stub = new AbstractClassGenericPropertyStandaloneKnockOff();
			AbstractClassGenericProperty<int> obj = stub.Object;

			var returnValue = new List<string> { "a", "b" };
			stub.Values.Get(returnValue);

			var result = obj.Values;

			Assert.Same(returnValue, result);
		}

		[Fact]
		public void Values_StandaloneClass_UnconfiguredReturnsDefault()
		{
			var stub = new AbstractClassGenericPropertyStandaloneKnockOff();
			AbstractClassGenericProperty<int> obj = stub.Object;

			// Abstract property returns default(List<string>) => null
			var result = obj.Values;

			Assert.Null(result);
		}

		[Fact]
		public void Values_StandaloneClass_VerifyTracksAccess()
		{
			var stub = new AbstractClassGenericPropertyStandaloneKnockOff();
			AbstractClassGenericProperty<int> obj = stub.Object;

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
			var stub = new AbstractClassGenericPropertyGenericStandaloneKnockOff<int>();
			AbstractClassGenericProperty<int> obj = stub.Object;

			var returnValue = new List<string> { "x", "y" };
			stub.Values.Get(returnValue);

			var result = obj.Values;

			Assert.Same(returnValue, result);
		}

		[Fact]
		public void Values_GenericStandaloneClass_UnconfiguredReturnsDefault()
		{
			var stub = new AbstractClassGenericPropertyGenericStandaloneKnockOff<int>();
			AbstractClassGenericProperty<int> obj = stub.Object;

			var result = obj.Values;

			Assert.Null(result);
		}

		#endregion

		#region Pattern 6 (Inline Class): Concrete generic property (Values)

		[Fact]
		public void Values_InlineClass_ConfiguredReturnsValue()
		{
			var stub = new AbstractClassGenericPropertyInlineTests.Stubs.AbstractClassGenericProperty();
			AbstractClassGenericProperty<int> obj = stub.Object;

			var returnValue = new List<string> { "c", "d" };
			stub.Values.Get(returnValue);

			var result = obj.Values;

			Assert.Same(returnValue, result);
		}

		[Fact]
		public void Values_InlineClass_UnconfiguredReturnsDefault()
		{
			var stub = new AbstractClassGenericPropertyInlineTests.Stubs.AbstractClassGenericProperty();
			AbstractClassGenericProperty<int> obj = stub.Object;

			var result = obj.Values;

			Assert.Null(result);
		}

		[Fact]
		public void Values_InlineClass_VerifyTracksAccess()
		{
			var stub = new AbstractClassGenericPropertyInlineTests.Stubs.AbstractClassGenericProperty();
			AbstractClassGenericProperty<int> obj = stub.Object;

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
			var stub = new AbstractClassGenericPropertyOpenGenericTests.Stubs.AbstractClassGenericProperty<int>();
			AbstractClassGenericProperty<int> obj = stub.Object;

			var returnValue = new List<string> { "e", "f" };
			stub.Values.Get(returnValue);

			var result = obj.Values;

			Assert.Same(returnValue, result);
		}

		[Fact]
		public void Values_OpenGenericClass_UnconfiguredReturnsDefault()
		{
			var stub = new AbstractClassGenericPropertyOpenGenericTests.Stubs.AbstractClassGenericProperty<int>();
			AbstractClassGenericProperty<int> obj = stub.Object;

			var result = obj.Values;

			Assert.Null(result);
		}

		#endregion

		// ====================================================================
		// Scenario 2: Class type param property (Data: T->int)
		// ====================================================================

		#region Pattern 3 (Standalone Class): Class type param property (Data)

		[Fact]
		public void Data_StandaloneClass_ConfiguredReturnsValue()
		{
			var stub = new AbstractClassGenericPropertyStandaloneKnockOff();
			AbstractClassGenericProperty<int> obj = stub.Object;

			stub.Data.Get(42);

			int result = obj.Data;

			Assert.Equal(42, result);
		}

		[Fact]
		public void Data_StandaloneClass_UnconfiguredReturnsDefault()
		{
			var stub = new AbstractClassGenericPropertyStandaloneKnockOff();
			AbstractClassGenericProperty<int> obj = stub.Object;

			// Abstract property returns default(int) => 0
			int result = obj.Data;

			Assert.Equal(0, result);
		}

		[Fact]
		public void Data_StandaloneClass_VerifyTracksAccess()
		{
			var stub = new AbstractClassGenericPropertyStandaloneKnockOff();
			AbstractClassGenericProperty<int> obj = stub.Object;

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
			var stub = new AbstractClassGenericPropertyGenericStandaloneKnockOff<int>();
			AbstractClassGenericProperty<int> obj = stub.Object;

			stub.Data.Get(99);

			int result = obj.Data;

			Assert.Equal(99, result);
		}

		[Fact]
		public void Data_GenericStandaloneClass_UnconfiguredReturnsDefault()
		{
			var stub = new AbstractClassGenericPropertyGenericStandaloneKnockOff<int>();
			AbstractClassGenericProperty<int> obj = stub.Object;

			int result = obj.Data;

			Assert.Equal(0, result);
		}

		#endregion

		#region Pattern 6 (Inline Class): Class type param property (Data)

		[Fact]
		public void Data_InlineClass_ConfiguredReturnsValue()
		{
			var stub = new AbstractClassGenericPropertyInlineTests.Stubs.AbstractClassGenericProperty();
			AbstractClassGenericProperty<int> obj = stub.Object;

			stub.Data.Get(42);

			int result = obj.Data;

			Assert.Equal(42, result);
		}

		[Fact]
		public void Data_InlineClass_UnconfiguredReturnsDefault()
		{
			var stub = new AbstractClassGenericPropertyInlineTests.Stubs.AbstractClassGenericProperty();
			AbstractClassGenericProperty<int> obj = stub.Object;

			int result = obj.Data;

			Assert.Equal(0, result);
		}

		[Fact]
		public void Data_InlineClass_VerifyTracksAccess()
		{
			var stub = new AbstractClassGenericPropertyInlineTests.Stubs.AbstractClassGenericProperty();
			AbstractClassGenericProperty<int> obj = stub.Object;

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
			var stub = new AbstractClassGenericPropertyOpenGenericTests.Stubs.AbstractClassGenericProperty<int>();
			AbstractClassGenericProperty<int> obj = stub.Object;

			stub.Data.Get(99);

			int result = obj.Data;

			Assert.Equal(99, result);
		}

		[Fact]
		public void Data_OpenGenericClass_UnconfiguredReturnsDefault()
		{
			var stub = new AbstractClassGenericPropertyOpenGenericTests.Stubs.AbstractClassGenericProperty<int>();
			AbstractClassGenericProperty<int> obj = stub.Object;

			int result = obj.Data;

			Assert.Equal(0, result);
		}

		#endregion

		// ====================================================================
		// Scenario 3: Get/Init variant — concrete generic property
		// ====================================================================

		#region Pattern 3 (Standalone Class): Get/Init Values

		[Fact]
		public void ValuesGetInit_StandaloneClass_ConfiguredReturnsValue()
		{
			var stub = new AbstractClassGenericPropertyGetAndInitStandaloneKnockOff();
			AbstractClassGenericPropertyGetAndInit<int> obj = stub.Object;

			var returnValue = new List<string> { "init-a" };
			stub.Values.Get(returnValue);

			var result = obj.Values;

			Assert.Same(returnValue, result);
		}

		[Fact]
		public void ValuesGetInit_StandaloneClass_UnconfiguredReturnsDefault()
		{
			var stub = new AbstractClassGenericPropertyGetAndInitStandaloneKnockOff();
			AbstractClassGenericPropertyGetAndInit<int> obj = stub.Object;

			var result = obj.Values;

			Assert.Null(result);
		}

		#endregion

		#region Pattern 4 (Generic Standalone Class): Get/Init Values

		[Fact]
		public void ValuesGetInit_GenericStandaloneClass_ConfiguredReturnsValue()
		{
			var stub = new AbstractClassGenericPropertyGetAndInitGenericStandaloneKnockOff<int>();
			AbstractClassGenericPropertyGetAndInit<int> obj = stub.Object;

			var returnValue = new List<string> { "init-b" };
			stub.Values.Get(returnValue);

			var result = obj.Values;

			Assert.Same(returnValue, result);
		}

		[Fact]
		public void ValuesGetInit_GenericStandaloneClass_UnconfiguredReturnsDefault()
		{
			var stub = new AbstractClassGenericPropertyGetAndInitGenericStandaloneKnockOff<int>();
			AbstractClassGenericPropertyGetAndInit<int> obj = stub.Object;

			var result = obj.Values;

			Assert.Null(result);
		}

		#endregion

		#region Pattern 6 (Inline Class): Get/Init Values

		[Fact]
		public void ValuesGetInit_InlineClass_ConfiguredReturnsValue()
		{
			var stub = new AbstractClassGenericPropertyInlineTests.Stubs.AbstractClassGenericPropertyGetAndInit();
			AbstractClassGenericPropertyGetAndInit<int> obj = stub.Object;

			var returnValue = new List<string> { "init-c" };
			stub.Values.Get(returnValue);

			var result = obj.Values;

			Assert.Same(returnValue, result);
		}

		[Fact]
		public void ValuesGetInit_InlineClass_UnconfiguredReturnsDefault()
		{
			var stub = new AbstractClassGenericPropertyInlineTests.Stubs.AbstractClassGenericPropertyGetAndInit();
			AbstractClassGenericPropertyGetAndInit<int> obj = stub.Object;

			var result = obj.Values;

			Assert.Null(result);
		}

		#endregion

		#region Pattern 9 (Open Generic Class): Get/Init Values

		[Fact]
		public void ValuesGetInit_OpenGenericClass_ConfiguredReturnsValue()
		{
			var stub = new AbstractClassGenericPropertyOpenGenericTests.Stubs.AbstractClassGenericPropertyGetAndInit<int>();
			AbstractClassGenericPropertyGetAndInit<int> obj = stub.Object;

			var returnValue = new List<string> { "init-d" };
			stub.Values.Get(returnValue);

			var result = obj.Values;

			Assert.Same(returnValue, result);
		}

		[Fact]
		public void ValuesGetInit_OpenGenericClass_UnconfiguredReturnsDefault()
		{
			var stub = new AbstractClassGenericPropertyOpenGenericTests.Stubs.AbstractClassGenericPropertyGetAndInit<int>();
			AbstractClassGenericPropertyGetAndInit<int> obj = stub.Object;

			var result = obj.Values;

			Assert.Null(result);
		}

		#endregion

		// ====================================================================
		// Scenario 4: Get/Init variant — class type param property (Data)
		// ====================================================================

		#region Pattern 3 (Standalone Class): Get/Init Data

		[Fact]
		public void DataGetInit_StandaloneClass_ConfiguredReturnsValue()
		{
			var stub = new AbstractClassGenericPropertyGetAndInitStandaloneKnockOff();
			AbstractClassGenericPropertyGetAndInit<int> obj = stub.Object;

			stub.Data.Get(77);

			int result = obj.Data;

			Assert.Equal(77, result);
		}

		[Fact]
		public void DataGetInit_StandaloneClass_UnconfiguredReturnsDefault()
		{
			var stub = new AbstractClassGenericPropertyGetAndInitStandaloneKnockOff();
			AbstractClassGenericPropertyGetAndInit<int> obj = stub.Object;

			int result = obj.Data;

			Assert.Equal(0, result);
		}

		#endregion

		#region Pattern 4 (Generic Standalone Class): Get/Init Data

		[Fact]
		public void DataGetInit_GenericStandaloneClass_ConfiguredReturnsValue()
		{
			var stub = new AbstractClassGenericPropertyGetAndInitGenericStandaloneKnockOff<int>();
			AbstractClassGenericPropertyGetAndInit<int> obj = stub.Object;

			stub.Data.Get(88);

			int result = obj.Data;

			Assert.Equal(88, result);
		}

		[Fact]
		public void DataGetInit_GenericStandaloneClass_UnconfiguredReturnsDefault()
		{
			var stub = new AbstractClassGenericPropertyGetAndInitGenericStandaloneKnockOff<int>();
			AbstractClassGenericPropertyGetAndInit<int> obj = stub.Object;

			int result = obj.Data;

			Assert.Equal(0, result);
		}

		#endregion

		#region Pattern 6 (Inline Class): Get/Init Data

		[Fact]
		public void DataGetInit_InlineClass_ConfiguredReturnsValue()
		{
			var stub = new AbstractClassGenericPropertyInlineTests.Stubs.AbstractClassGenericPropertyGetAndInit();
			AbstractClassGenericPropertyGetAndInit<int> obj = stub.Object;

			stub.Data.Get(77);

			int result = obj.Data;

			Assert.Equal(77, result);
		}

		[Fact]
		public void DataGetInit_InlineClass_UnconfiguredReturnsDefault()
		{
			var stub = new AbstractClassGenericPropertyInlineTests.Stubs.AbstractClassGenericPropertyGetAndInit();
			AbstractClassGenericPropertyGetAndInit<int> obj = stub.Object;

			int result = obj.Data;

			Assert.Equal(0, result);
		}

		#endregion

		#region Pattern 9 (Open Generic Class): Get/Init Data

		[Fact]
		public void DataGetInit_OpenGenericClass_ConfiguredReturnsValue()
		{
			var stub = new AbstractClassGenericPropertyOpenGenericTests.Stubs.AbstractClassGenericPropertyGetAndInit<int>();
			AbstractClassGenericPropertyGetAndInit<int> obj = stub.Object;

			stub.Data.Get(88);

			int result = obj.Data;

			Assert.Equal(88, result);
		}

		[Fact]
		public void DataGetInit_OpenGenericClass_UnconfiguredReturnsDefault()
		{
			var stub = new AbstractClassGenericPropertyOpenGenericTests.Stubs.AbstractClassGenericPropertyGetAndInit<int>();
			AbstractClassGenericPropertyGetAndInit<int> obj = stub.Object;

			int result = obj.Data;

			Assert.Equal(0, result);
		}

		#endregion

		// ====================================================================
		// Scenario 5: Callback on property get — computed return value
		// ====================================================================

		#region Pattern 3 (Standalone Class): Callback on Data get

		[Fact]
		public void Data_StandaloneClass_CallbackReturnsComputedValue()
		{
			var stub = new AbstractClassGenericPropertyStandaloneKnockOff();
			AbstractClassGenericProperty<int> obj = stub.Object;

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
			var stub = new AbstractClassGenericPropertyGenericStandaloneKnockOff<int>();
			AbstractClassGenericProperty<int> obj = stub.Object;

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
			var stub = new AbstractClassGenericPropertyInlineTests.Stubs.AbstractClassGenericProperty();
			AbstractClassGenericProperty<int> obj = stub.Object;

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
			var stub = new AbstractClassGenericPropertyOpenGenericTests.Stubs.AbstractClassGenericProperty<int>();
			AbstractClassGenericProperty<int> obj = stub.Object;

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
