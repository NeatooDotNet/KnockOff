// ============================================================================
// AbstractClassIndexerTests: Edge case tests for abstract classes with
// abstract indexers covering every accessor combination plus abstract events.
// Inspired by Rocks.Analysis.IntegrationTests.AbstractClassIndexerTests
//
// Target classes:
//   AbstractClassIndexerGetter       - get-only, with event
//   AbstractClassIndexerGetterSetter - get + set
//   AbstractClassIndexerGetterInit   - get + init
//   AbstractClassIndexerSetter       - set-only, with event
//
// Each class has BOTH single-parameter (this[int a]) and multi-parameter
// (this[int a, string b]) indexers.
//
// Key difference from ClassIndexerTests: members are ABSTRACT, not virtual.
// Abstract indexers have no base implementation to fall through to, so
// unconfigured calls return default(int) = 0 directly.
//
// Applicable patterns (class only):
// - Pattern 3 (Standalone Class): [KnockOffBase<T>]
// - Pattern 6 (Inline Class): [KnockOff<T>]
// ============================================================================

using KnockOff;

// ============================================================================
// TYPE DEFINITIONS
// ============================================================================

namespace KnockOff.Analysis.Tests.AbstractClassIndexerTestTypes
{
	public abstract class AbstractClassIndexerGetter
	{
		public abstract int this[int a] { get; }
		public abstract int this[int a, string b] { get; }

		public abstract event EventHandler? MyEvent;
	}

	public abstract class AbstractClassIndexerGetterSetter
	{
		public abstract int this[int a] { get; set; }
		public abstract int this[int a, string b] { get; set; }
	}

	public abstract class AbstractClassIndexerGetterInit
	{
		public abstract int this[int a] { get; init; }
		public abstract int this[int a, string b] { get; init; }
	}

	public abstract class AbstractClassIndexerSetter
	{
#pragma warning disable CA1044 // Properties should not be write only
		public abstract int this[int a] { set; }
		public abstract int this[int a, string b] { set; }
#pragma warning restore CA1044 // Properties should not be write only

		public abstract event EventHandler? MyEvent;
	}

	// Pattern 3: Standalone class stubs
	[KnockOffBase<AbstractClassIndexerGetter>]
	public partial class AbstractClassIndexerGetterStandalone { }

	[KnockOffBase<AbstractClassIndexerGetterSetter>]
	public partial class AbstractClassIndexerGetterSetterStandalone { }

	[KnockOffBase<AbstractClassIndexerGetterInit>]
	public partial class AbstractClassIndexerGetterInitStandalone { }

	[KnockOffBase<AbstractClassIndexerSetter>]
	public partial class AbstractClassIndexerSetterStandalone { }
}

// ============================================================================
// INLINE PATTERN DECLARATIONS
// ============================================================================

namespace KnockOff.Analysis.Tests
{
	using KnockOff.Analysis.Tests.AbstractClassIndexerTestTypes;

	// Pattern 6: Inline class stubs
	[KnockOff<AbstractClassIndexerGetter>]
	[KnockOff<AbstractClassIndexerGetterSetter>]
	[KnockOff<AbstractClassIndexerGetterInit>]
	[KnockOff<AbstractClassIndexerSetter>]
	public partial class AbstractClassIndexerInlineTests { }

	// ========================================================================
	// TESTS
	// ========================================================================

	public class AbstractClassIndexerTests
	{
		// ====================================================================
		// Scenario 1: Get-only indexer (single param) — configure return, assert
		// ====================================================================

		#region Standalone Class: Get-only single-param indexer

		[Fact]
		public void GetOnly_SingleParam_StandaloneClass_ConfiguredReturnsValue()
		{
			var stub = new AbstractClassIndexerGetterStandalone();
			AbstractClassIndexerGetter obj = stub.Object;

			stub.Indexer.Get((int key) => key * 10);

			int result = obj[3];

			Assert.Equal(30, result);
		}

		[Fact]
		public void GetOnly_SingleParam_StandaloneClass_UnconfiguredReturnsDefault()
		{
			var stub = new AbstractClassIndexerGetterStandalone();
			AbstractClassIndexerGetter obj = stub.Object;

			// Abstract indexer returns default (0) when unconfigured
			int result = obj[5];

			Assert.Equal(0, result);
		}

		[Fact]
		public void GetOnly_SingleParam_StandaloneClass_VerifyTracksAccess()
		{
			var stub = new AbstractClassIndexerGetterStandalone();
			AbstractClassIndexerGetter obj = stub.Object;

			stub.Indexer.Get((int key) => key);
			_ = obj[1];
			_ = obj[2];

			stub.Indexer.VerifyGet(Called.Exactly(2));
		}

		#endregion

		#region Inline Class: Get-only single-param indexer

		[Fact]
		public void GetOnly_SingleParam_InlineClass_ConfiguredReturnsValue()
		{
			var stub = new AbstractClassIndexerInlineTests.Stubs.AbstractClassIndexerGetter();
			AbstractClassIndexerGetter obj = stub.Object;

			stub.Indexer.Get((int key) => key * 10);

			int result = obj[3];

			Assert.Equal(30, result);
		}

		[Fact]
		public void GetOnly_SingleParam_InlineClass_UnconfiguredReturnsDefault()
		{
			var stub = new AbstractClassIndexerInlineTests.Stubs.AbstractClassIndexerGetter();
			AbstractClassIndexerGetter obj = stub.Object;

			int result = obj[5];

			Assert.Equal(0, result);
		}

		[Fact]
		public void GetOnly_SingleParam_InlineClass_VerifyTracksAccess()
		{
			var stub = new AbstractClassIndexerInlineTests.Stubs.AbstractClassIndexerGetter();
			AbstractClassIndexerGetter obj = stub.Object;

			stub.Indexer.Get((int key) => key);
			_ = obj[1];
			_ = obj[2];

			stub.Indexer.VerifyGet(Called.Exactly(2));
		}

		#endregion

		// ====================================================================
		// Scenario 2: Set-only indexer (single param) — set value, verify tracking
		// ====================================================================

		#region Standalone Class: Set-only single-param indexer

		[Fact]
		public void SetOnly_SingleParam_StandaloneClass_SetIsTracked()
		{
			var stub = new AbstractClassIndexerSetterStandalone();
			AbstractClassIndexerSetter obj = stub.Object;

			obj[3] = 42;

			stub.Indexer.VerifySet(Called.Once);
		}

		[Fact]
		public void SetOnly_SingleParam_StandaloneClass_LastSetEntryCaptured()
		{
			var stub = new AbstractClassIndexerSetterStandalone();
			AbstractClassIndexerSetter obj = stub.Object;

			obj[7] = 99;

			var lastEntry = stub.Indexer.LastInt32SetEntry;
			Assert.NotNull(lastEntry);
			Assert.Equal(7, lastEntry!.Value.Key);
			Assert.Equal(99, lastEntry!.Value.Value);
		}

		#endregion

		#region Inline Class: Set-only single-param indexer

		[Fact]
		public void SetOnly_SingleParam_InlineClass_SetIsTracked()
		{
			var stub = new AbstractClassIndexerInlineTests.Stubs.AbstractClassIndexerSetter();
			AbstractClassIndexerSetter obj = stub.Object;

			obj[3] = 42;

			stub.Indexer.VerifySet(Called.Once);
		}

		[Fact]
		public void SetOnly_SingleParam_InlineClass_LastSetEntryCaptured()
		{
			var stub = new AbstractClassIndexerInlineTests.Stubs.AbstractClassIndexerSetter();
			AbstractClassIndexerSetter obj = stub.Object;

			obj[7] = 99;

			var lastEntry = stub.Indexer.LastInt32SetEntry;
			Assert.NotNull(lastEntry);
			Assert.Equal(7, lastEntry!.Value.Key);
			Assert.Equal(99, lastEntry!.Value.Value);
		}

		#endregion

		// ====================================================================
		// Scenario 3: Get/Set indexer (single param) — configure get, set, verify
		// ====================================================================

		#region Standalone Class: Get/Set single-param indexer

		[Fact]
		public void GetSet_SingleParam_StandaloneClass_ConfigureGetAndSet()
		{
			var stub = new AbstractClassIndexerGetterSetterStandalone();
			AbstractClassIndexerGetterSetter obj = stub.Object;

			stub.Indexer.Get((int key) => key + 100);

			int getResult = obj[5];
			obj[5] = 42;

			Assert.Equal(105, getResult);
			stub.Indexer.VerifyGet(Called.Once);
			stub.Indexer.VerifySet(Called.Once);
		}

		[Fact]
		public void GetSet_SingleParam_StandaloneClass_SetCapturesValue()
		{
			var stub = new AbstractClassIndexerGetterSetterStandalone();
			AbstractClassIndexerGetterSetter obj = stub.Object;

			int? capturedKey = null;
			int? capturedValue = null;
			stub.Indexer.Set((int key, int value) =>
			{
				capturedKey = key;
				capturedValue = value;
			});

			obj[3] = 77;

			Assert.Equal(3, capturedKey);
			Assert.Equal(77, capturedValue);
		}

		#endregion

		#region Inline Class: Get/Set single-param indexer

		[Fact]
		public void GetSet_SingleParam_InlineClass_ConfigureGetAndSet()
		{
			var stub = new AbstractClassIndexerInlineTests.Stubs.AbstractClassIndexerGetterSetter();
			AbstractClassIndexerGetterSetter obj = stub.Object;

			stub.Indexer.Get((int key) => key + 100);

			int getResult = obj[5];
			obj[5] = 42;

			Assert.Equal(105, getResult);
			stub.Indexer.VerifyGet(Called.Once);
			stub.Indexer.VerifySet(Called.Once);
		}

		[Fact]
		public void GetSet_SingleParam_InlineClass_SetCapturesValue()
		{
			var stub = new AbstractClassIndexerInlineTests.Stubs.AbstractClassIndexerGetterSetter();
			AbstractClassIndexerGetterSetter obj = stub.Object;

			int? capturedKey = null;
			int? capturedValue = null;
			stub.Indexer.Set((int key, int value) =>
			{
				capturedKey = key;
				capturedValue = value;
			});

			obj[3] = 77;

			Assert.Equal(3, capturedKey);
			Assert.Equal(77, capturedValue);
		}

		#endregion

		// ====================================================================
		// Scenario 4: Get/Init indexer (single param) — configure get, read, verify
		// ====================================================================

		#region Standalone Class: Get/Init single-param indexer

		[Fact]
		public void GetInit_SingleParam_StandaloneClass_ConfiguredReturnsValue()
		{
			var stub = new AbstractClassIndexerGetterInitStandalone();
			AbstractClassIndexerGetterInit obj = stub.Object;

			stub.Indexer.Get((int key) => key * 5);

			int result = obj[4];

			Assert.Equal(20, result);
		}

		[Fact]
		public void GetInit_SingleParam_StandaloneClass_VerifyTracksAccess()
		{
			var stub = new AbstractClassIndexerGetterInitStandalone();
			AbstractClassIndexerGetterInit obj = stub.Object;

			stub.Indexer.Get((int key) => key);
			_ = obj[1];

			stub.Indexer.VerifyGet(Called.Once);
		}

		#endregion

		#region Inline Class: Get/Init single-param indexer

		[Fact]
		public void GetInit_SingleParam_InlineClass_ConfiguredReturnsValue()
		{
			var stub = new AbstractClassIndexerInlineTests.Stubs.AbstractClassIndexerGetterInit();
			AbstractClassIndexerGetterInit obj = stub.Object;

			stub.Indexer.Get((int key) => key * 5);

			int result = obj[4];

			Assert.Equal(20, result);
		}

		[Fact]
		public void GetInit_SingleParam_InlineClass_VerifyTracksAccess()
		{
			var stub = new AbstractClassIndexerInlineTests.Stubs.AbstractClassIndexerGetterInit();
			AbstractClassIndexerGetterInit obj = stub.Object;

			stub.Indexer.Get((int key) => key);
			_ = obj[1];

			stub.Indexer.VerifyGet(Called.Once);
		}

		#endregion

		// ====================================================================
		// Scenario 5: Multi-parameter indexer — callback receives ALL key params
		// ====================================================================

		#region Standalone Class: Multi-param get indexer

		[Fact]
		public void MultiParam_Get_StandaloneClass_CallbackReceivesAllParams()
		{
			var stub = new AbstractClassIndexerGetterStandalone();
			AbstractClassIndexerGetter obj = stub.Object;

			stub.Indexer.Get(((int a, string b) key) => key.a + key.b.Length);

			int result = obj[3, "hello"];

			Assert.Equal(8, result); // 3 + 5
		}

		[Fact]
		public void MultiParam_Get_StandaloneClass_UnconfiguredReturnsDefault()
		{
			var stub = new AbstractClassIndexerGetterStandalone();
			AbstractClassIndexerGetter obj = stub.Object;

			int result = obj[3, "hello"];

			Assert.Equal(0, result); // abstract returns default
		}

		#endregion

		#region Inline Class: Multi-param get indexer

		[Fact]
		public void MultiParam_Get_InlineClass_CallbackReceivesAllParams()
		{
			var stub = new AbstractClassIndexerInlineTests.Stubs.AbstractClassIndexerGetter();
			AbstractClassIndexerGetter obj = stub.Object;

			stub.Indexer.Get(((int a, string b) key) => key.a + key.b.Length);

			int result = obj[3, "hello"];

			Assert.Equal(8, result);
		}

		[Fact]
		public void MultiParam_Get_InlineClass_UnconfiguredReturnsDefault()
		{
			var stub = new AbstractClassIndexerInlineTests.Stubs.AbstractClassIndexerGetter();
			AbstractClassIndexerGetter obj = stub.Object;

			int result = obj[3, "hello"];

			Assert.Equal(0, result);
		}

		#endregion

		#region Standalone Class: Multi-param set indexer

		[Fact]
		public void MultiParam_Set_StandaloneClass_CallbackCapturesKeyAndValue()
		{
			var stub = new AbstractClassIndexerSetterStandalone();
			AbstractClassIndexerSetter obj = stub.Object;

			(int, string)? capturedKey = null;
			int? capturedValue = null;
			stub.Indexer.Set(((int a, string b) key, int value) =>
			{
				capturedKey = (key.a, key.b);
				capturedValue = value;
			});

			obj[5, "test"] = 42;

			Assert.Equal((5, "test"), capturedKey);
			Assert.Equal(42, capturedValue);
		}

		#endregion

		#region Inline Class: Multi-param set indexer

		[Fact]
		public void MultiParam_Set_InlineClass_CallbackCapturesKeyAndValue()
		{
			var stub = new AbstractClassIndexerInlineTests.Stubs.AbstractClassIndexerSetter();
			AbstractClassIndexerSetter obj = stub.Object;

			(int, string)? capturedKey = null;
			int? capturedValue = null;
			stub.Indexer.Set(((int a, string b) key, int value) =>
			{
				capturedKey = (key.a, key.b);
				capturedValue = value;
			});

			obj[5, "test"] = 42;

			Assert.Equal((5, "test"), capturedKey);
			Assert.Equal(42, capturedValue);
		}

		#endregion

		#region Standalone Class: Multi-param get/set indexer

		[Fact]
		public void MultiParam_GetSet_StandaloneClass_BothWork()
		{
			var stub = new AbstractClassIndexerGetterSetterStandalone();
			AbstractClassIndexerGetterSetter obj = stub.Object;

			stub.Indexer.Get(((int a, string b) key) => key.a * key.b.Length);

			int getResult = obj[2, "abc"];
			obj[2, "abc"] = 99;

			Assert.Equal(6, getResult); // 2 * 3
			stub.Indexer.VerifyGet(Called.Once);
			stub.Indexer.VerifySet(Called.Once);
		}

		#endregion

		#region Inline Class: Multi-param get/set indexer

		[Fact]
		public void MultiParam_GetSet_InlineClass_BothWork()
		{
			var stub = new AbstractClassIndexerInlineTests.Stubs.AbstractClassIndexerGetterSetter();
			AbstractClassIndexerGetterSetter obj = stub.Object;

			stub.Indexer.Get(((int a, string b) key) => key.a * key.b.Length);

			int getResult = obj[2, "abc"];
			obj[2, "abc"] = 99;

			Assert.Equal(6, getResult);
			stub.Indexer.VerifyGet(Called.Once);
			stub.Indexer.VerifySet(Called.Once);
		}

		#endregion

		// ====================================================================
		// Scenario 6: Event raising from indexer get
		// ====================================================================

		#region Standalone Class: Event raised during indexer get

		[Fact]
		public void EventOnGet_StandaloneClass_CallbackRaisesEvent()
		{
			var stub = new AbstractClassIndexerGetterStandalone();
			AbstractClassIndexerGetter obj = stub.Object;

			stub.Indexer.Get((int key) =>
			{
				stub.MyEvent.Raise(obj, EventArgs.Empty);
				return key * 10;
			});

			var wasEventRaised = false;
			obj.MyEvent += (s, e) => wasEventRaised = true;

			int value = obj[3];

			Assert.Equal(30, value);
			Assert.True(wasEventRaised);
		}

		#endregion

		#region Inline Class: Event raised during indexer get

		[Fact]
		public void EventOnGet_InlineClass_CallbackRaisesEvent()
		{
			var stub = new AbstractClassIndexerInlineTests.Stubs.AbstractClassIndexerGetter();
			AbstractClassIndexerGetter obj = stub.Object;

			stub.Indexer.Get((int key) =>
			{
				stub.MyEvent.Raise(obj, EventArgs.Empty);
				return key * 10;
			});

			var wasEventRaised = false;
			obj.MyEvent += (s, e) => wasEventRaised = true;

			int value = obj[3];

			Assert.Equal(30, value);
			Assert.True(wasEventRaised);
		}

		#endregion

		// ====================================================================
		// Scenario 7: Event raising from indexer set
		// ====================================================================

		#region Standalone Class: Event raised during indexer set

		[Fact]
		public void EventOnSet_StandaloneClass_CallbackRaisesEvent()
		{
			var stub = new AbstractClassIndexerSetterStandalone();
			AbstractClassIndexerSetter obj = stub.Object;

			stub.Indexer.Set((int key, int value) =>
			{
				stub.MyEvent.Raise(obj, EventArgs.Empty);
			});

			var wasEventRaised = false;
			obj.MyEvent += (s, e) => wasEventRaised = true;

			obj[3] = 42;

			Assert.True(wasEventRaised);
		}

		#endregion

		#region Inline Class: Event raised during indexer set

		[Fact]
		public void EventOnSet_InlineClass_CallbackRaisesEvent()
		{
			var stub = new AbstractClassIndexerInlineTests.Stubs.AbstractClassIndexerSetter();
			AbstractClassIndexerSetter obj = stub.Object;

			stub.Indexer.Set((int key, int value) =>
			{
				stub.MyEvent.Raise(obj, EventArgs.Empty);
			});

			var wasEventRaised = false;
			obj.MyEvent += (s, e) => wasEventRaised = true;

			obj[3] = 42;

			Assert.True(wasEventRaised);
		}

		#endregion

		// ====================================================================
		// Multi-param GetterInit indexer
		// ====================================================================

		#region Standalone Class: Multi-param GetterInit

		[Fact]
		public void MultiParamGetInit_StandaloneClass_ConfiguredReturnsValue()
		{
			var stub = new AbstractClassIndexerGetterInitStandalone();
			AbstractClassIndexerGetterInit obj = stub.Object;

			stub.Indexer.Get(((int a, string b) key) => key.a + key.b.Length);

			int result = obj[3, "hello"];

			Assert.Equal(8, result);
		}

		[Fact]
		public void MultiParamGetInit_StandaloneClass_UnconfiguredReturnsDefault()
		{
			var stub = new AbstractClassIndexerGetterInitStandalone();
			AbstractClassIndexerGetterInit obj = stub.Object;

			int result = obj[3, "hello"];

			Assert.Equal(0, result); // abstract returns default
		}

		#endregion

		#region Inline Class: Multi-param GetterInit

		[Fact]
		public void MultiParamGetInit_InlineClass_ConfiguredReturnsValue()
		{
			var stub = new AbstractClassIndexerInlineTests.Stubs.AbstractClassIndexerGetterInit();
			AbstractClassIndexerGetterInit obj = stub.Object;

			stub.Indexer.Get(((int a, string b) key) => key.a + key.b.Length);

			int result = obj[3, "hello"];

			Assert.Equal(8, result);
		}

		[Fact]
		public void MultiParamGetInit_InlineClass_UnconfiguredReturnsDefault()
		{
			var stub = new AbstractClassIndexerInlineTests.Stubs.AbstractClassIndexerGetterInit();
			AbstractClassIndexerGetterInit obj = stub.Object;

			int result = obj[3, "hello"];

			Assert.Equal(0, result);
		}

		#endregion

		// ====================================================================
		// Multi-param event raising from indexer access
		// ====================================================================

		#region Standalone Class: Multi-param event on get/set

		[Fact]
		public void MultiParamEventOnGet_StandaloneClass_EventFires()
		{
			var stub = new AbstractClassIndexerGetterStandalone();
			AbstractClassIndexerGetter obj = stub.Object;

			stub.Indexer.Get(((int a, string b) key) =>
			{
				stub.MyEvent.Raise(obj, EventArgs.Empty);
				return key.a;
			});

			var wasEventRaised = false;
			obj.MyEvent += (s, e) => wasEventRaised = true;

			int result = obj[3, "test"];

			Assert.Equal(3, result);
			Assert.True(wasEventRaised);
		}

		[Fact]
		public void MultiParamEventOnSet_StandaloneClass_EventFires()
		{
			var stub = new AbstractClassIndexerSetterStandalone();
			AbstractClassIndexerSetter obj = stub.Object;

			stub.Indexer.Set(((int a, string b) key, int value) =>
			{
				stub.MyEvent.Raise(obj, EventArgs.Empty);
			});

			var wasEventRaised = false;
			obj.MyEvent += (s, e) => wasEventRaised = true;

			obj[3, "test"] = 42;

			Assert.True(wasEventRaised);
		}

		#endregion

		#region Inline Class: Multi-param event on get/set

		[Fact]
		public void MultiParamEventOnGet_InlineClass_EventFires()
		{
			var stub = new AbstractClassIndexerInlineTests.Stubs.AbstractClassIndexerGetter();
			AbstractClassIndexerGetter obj = stub.Object;

			stub.Indexer.Get(((int a, string b) key) =>
			{
				stub.MyEvent.Raise(obj, EventArgs.Empty);
				return key.a;
			});

			var wasEventRaised = false;
			obj.MyEvent += (s, e) => wasEventRaised = true;

			int result = obj[3, "test"];

			Assert.Equal(3, result);
			Assert.True(wasEventRaised);
		}

		[Fact]
		public void MultiParamEventOnSet_InlineClass_EventFires()
		{
			var stub = new AbstractClassIndexerInlineTests.Stubs.AbstractClassIndexerSetter();
			AbstractClassIndexerSetter obj = stub.Object;

			stub.Indexer.Set(((int a, string b) key, int value) =>
			{
				stub.MyEvent.Raise(obj, EventArgs.Empty);
			});

			var wasEventRaised = false;
			obj.MyEvent += (s, e) => wasEventRaised = true;

			obj[3, "test"] = 42;

			Assert.True(wasEventRaised);
		}

		#endregion
	}
}
