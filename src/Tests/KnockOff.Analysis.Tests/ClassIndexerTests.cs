// ============================================================================
// ClassIndexerTests: Edge case tests for classes with virtual indexers
// covering every accessor combination plus virtual events.
// Inspired by Rocks.Analysis.IntegrationTests.ClassIndexerTests
//
// Target classes:
//   ClassIndexerGetter       - get-only, with event
//   ClassIndexerGetterSetter - get + set
//   ClassIndexerGetterInit   - get + init
//   ClassIndexerSetter       - set-only, with event
//   ClassIndexerInit         - init-only, with event
//
// Each class has BOTH single-parameter (this[int a]) and multi-parameter
// (this[int a, string b]) indexers.
//
// Tests exercise the CLASS pipeline for:
// 1. Get-only indexer — configure return via callback, assert returned value
// 2. Set-only indexer — set value, verify tracking, assert LastSetEntry
// 3. Get/Set indexer — configure get, set value, verify both independently
// 4. Get/Init indexer — configure get, read it, verify
// 5. Init-only indexer — verify stub can be instantiated
// 6. Multi-parameter indexer — callback receives ALL key params, assert dispatch
// 7. Event raising from indexer get — callback raises event during indexer access
// 8. Event raising from indexer set — callback raises event during indexer set
// 9. Callback on indexer get — callback computes return from key
// 10. Callback on indexer set — callback captures key + value
//
// Applicable patterns (class only):
// - Pattern 3 (Standalone Class): [KnockOffBase<T>]
// - Pattern 6 (Inline Class): [KnockOff<T>]
// ============================================================================

using KnockOff;

// ============================================================================
// TYPE DEFINITIONS
// ============================================================================

namespace KnockOff.Analysis.Tests.ClassIndexerTestTypes
{
	public class ClassIndexerGetter
	{
		public virtual int this[int a] => default;
		public virtual int this[int a, string b] => default;

#pragma warning disable CA1070 // Do not declare event fields as virtual
#pragma warning disable CS0067
		public virtual event EventHandler? MyEvent;
#pragma warning restore CS0067
#pragma warning restore CA1070 // Do not declare event fields as virtual
	}

	public class ClassIndexerGetterSetter
	{
		public virtual int this[int a] { get => default; set { } }
		public virtual int this[int a, string b] { get => default; set { } }
	}

	public class ClassIndexerGetterInit
	{
		public virtual int this[int a] { get => default; init { } }
		public virtual int this[int a, string b] { get => default; init { } }
	}

	public class ClassIndexerSetter
	{
#pragma warning disable CA1044 // Properties should not be write only
		public virtual int this[int a] { set { } }
		public virtual int this[int a, string b] { set { } }
#pragma warning restore CA1044 // Properties should not be write only

#pragma warning disable CA1070 // Do not declare event fields as virtual
#pragma warning disable CS0067
		public virtual event EventHandler? MyEvent;
#pragma warning restore CS0067
#pragma warning restore CA1070 // Do not declare event fields as virtual
	}

	public class ClassIndexerInit
	{
#pragma warning disable CA1044 // Properties should not be write only
		public virtual int this[int a] { init { } }
		public virtual int this[int a, string b] { init { } }
#pragma warning restore CA1044 // Properties should not be write only

#pragma warning disable CA1070 // Do not declare event fields as virtual
#pragma warning disable CS0067
		public virtual event EventHandler? MyEvent;
#pragma warning restore CS0067
#pragma warning restore CA1070 // Do not declare event fields as virtual
	}

	// Pattern 3: Standalone class stubs
	[KnockOffBase<ClassIndexerGetter>]
	public partial class ClassIndexerGetterStandalone { }

	[KnockOffBase<ClassIndexerGetterSetter>]
	public partial class ClassIndexerGetterSetterStandalone { }

	[KnockOffBase<ClassIndexerGetterInit>]
	public partial class ClassIndexerGetterInitStandalone { }

	[KnockOffBase<ClassIndexerSetter>]
	public partial class ClassIndexerSetterStandalone { }

	[KnockOffBase<ClassIndexerInit>]
	public partial class ClassIndexerInitStandalone { }
}

// ============================================================================
// INLINE PATTERN DECLARATIONS
// ============================================================================

namespace KnockOff.Analysis.Tests
{
	using KnockOff.Analysis.Tests.ClassIndexerTestTypes;

	// Pattern 6: Inline class stubs
	[KnockOff<ClassIndexerGetter>]
	[KnockOff<ClassIndexerGetterSetter>]
	[KnockOff<ClassIndexerGetterInit>]
	[KnockOff<ClassIndexerSetter>]
	[KnockOff<ClassIndexerInit>]
	public partial class ClassIndexerInlineTests { }

	// ========================================================================
	// TESTS
	// ========================================================================

	public class ClassIndexerTests
	{
		// ====================================================================
		// Scenario 1: Get-only indexer (single param) — configure return, assert
		// ====================================================================

		#region Standalone Class: Get-only single-param indexer

		[Fact]
		public void GetOnly_SingleParam_StandaloneClass_ConfiguredReturnsValue()
		{
			var stub = new ClassIndexerGetterStandalone();
			ClassIndexerGetter obj = stub.Object;

			stub.Indexer.Get((int key) => key * 10);

			int result = obj[3];

			Assert.Equal(30, result);
		}

		[Fact]
		public void GetOnly_SingleParam_StandaloneClass_UnconfiguredFallsToBase()
		{
			var stub = new ClassIndexerGetterStandalone();
			ClassIndexerGetter obj = stub.Object;

			int result = obj[5];

			Assert.Equal(0, result); // base returns default
		}

		[Fact]
		public void GetOnly_SingleParam_StandaloneClass_VerifyTracksAccess()
		{
			var stub = new ClassIndexerGetterStandalone();
			ClassIndexerGetter obj = stub.Object;

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
			var stub = new ClassIndexerInlineTests.Stubs.ClassIndexerGetter();
			ClassIndexerGetter obj = stub.Object;

			stub.Indexer.Get((int key) => key * 10);

			int result = obj[3];

			Assert.Equal(30, result);
		}

		[Fact]
		public void GetOnly_SingleParam_InlineClass_UnconfiguredFallsToBase()
		{
			var stub = new ClassIndexerInlineTests.Stubs.ClassIndexerGetter();
			ClassIndexerGetter obj = stub.Object;

			int result = obj[5];

			Assert.Equal(0, result);
		}

		[Fact]
		public void GetOnly_SingleParam_InlineClass_VerifyTracksAccess()
		{
			var stub = new ClassIndexerInlineTests.Stubs.ClassIndexerGetter();
			ClassIndexerGetter obj = stub.Object;

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
			var stub = new ClassIndexerSetterStandalone();
			ClassIndexerSetter obj = stub.Object;

			obj[3] = 42;

			stub.Indexer.VerifySet(Called.Once);
		}

		[Fact]
		public void SetOnly_SingleParam_StandaloneClass_LastSetEntryCaptured()
		{
			var stub = new ClassIndexerSetterStandalone();
			ClassIndexerSetter obj = stub.Object;

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
			var stub = new ClassIndexerInlineTests.Stubs.ClassIndexerSetter();
			ClassIndexerSetter obj = stub.Object;

			obj[3] = 42;

			stub.Indexer.VerifySet(Called.Once);
		}

		[Fact]
		public void SetOnly_SingleParam_InlineClass_LastSetEntryCaptured()
		{
			var stub = new ClassIndexerInlineTests.Stubs.ClassIndexerSetter();
			ClassIndexerSetter obj = stub.Object;

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
			var stub = new ClassIndexerGetterSetterStandalone();
			ClassIndexerGetterSetter obj = stub.Object;

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
			var stub = new ClassIndexerGetterSetterStandalone();
			ClassIndexerGetterSetter obj = stub.Object;

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
			var stub = new ClassIndexerInlineTests.Stubs.ClassIndexerGetterSetter();
			ClassIndexerGetterSetter obj = stub.Object;

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
			var stub = new ClassIndexerInlineTests.Stubs.ClassIndexerGetterSetter();
			ClassIndexerGetterSetter obj = stub.Object;

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
			var stub = new ClassIndexerGetterInitStandalone();
			ClassIndexerGetterInit obj = stub.Object;

			stub.Indexer.Get((int key) => key * 5);

			int result = obj[4];

			Assert.Equal(20, result);
		}

		[Fact]
		public void GetInit_SingleParam_StandaloneClass_VerifyTracksAccess()
		{
			var stub = new ClassIndexerGetterInitStandalone();
			ClassIndexerGetterInit obj = stub.Object;

			stub.Indexer.Get((int key) => key);
			_ = obj[1];

			stub.Indexer.VerifyGet(Called.Once);
		}

		#endregion

		#region Inline Class: Get/Init single-param indexer

		[Fact]
		public void GetInit_SingleParam_InlineClass_ConfiguredReturnsValue()
		{
			var stub = new ClassIndexerInlineTests.Stubs.ClassIndexerGetterInit();
			ClassIndexerGetterInit obj = stub.Object;

			stub.Indexer.Get((int key) => key * 5);

			int result = obj[4];

			Assert.Equal(20, result);
		}

		[Fact]
		public void GetInit_SingleParam_InlineClass_VerifyTracksAccess()
		{
			var stub = new ClassIndexerInlineTests.Stubs.ClassIndexerGetterInit();
			ClassIndexerGetterInit obj = stub.Object;

			stub.Indexer.Get((int key) => key);
			_ = obj[1];

			stub.Indexer.VerifyGet(Called.Once);
		}

		#endregion

		// ====================================================================
		// Scenario 5: Init-only indexer — verify stub can be instantiated
		// Init-only virtual indexers cannot be set after construction.
		// ====================================================================

		#region Standalone Class: Init-only indexer

		[Fact]
		public void InitOnly_StandaloneClass_StubCanBeInstantiated()
		{
			var stub = new ClassIndexerInitStandalone();
			ClassIndexerInit obj = stub.Object;

			Assert.NotNull(obj);
		}

		#endregion

		#region Inline Class: Init-only indexer

		[Fact]
		public void InitOnly_InlineClass_StubCanBeInstantiated()
		{
			var stub = new ClassIndexerInlineTests.Stubs.ClassIndexerInit();
			ClassIndexerInit obj = stub.Object;

			Assert.NotNull(obj);
		}

		#endregion

		// ====================================================================
		// Scenario 6: Multi-parameter indexer — callback receives ALL key params
		// Multi-param indexers use tuple keys: (int a, string b)
		// ====================================================================

		#region Standalone Class: Multi-param get indexer

		[Fact]
		public void MultiParam_Get_StandaloneClass_CallbackReceivesAllParams()
		{
			var stub = new ClassIndexerGetterStandalone();
			ClassIndexerGetter obj = stub.Object;

			stub.Indexer.Get(((int a, string b) key) => key.a + key.b.Length);

			int result = obj[3, "hello"];

			Assert.Equal(8, result); // 3 + 5
		}

		[Fact]
		public void MultiParam_Get_StandaloneClass_UnconfiguredFallsToBase()
		{
			var stub = new ClassIndexerGetterStandalone();
			ClassIndexerGetter obj = stub.Object;

			int result = obj[3, "hello"];

			Assert.Equal(0, result); // base returns default
		}

		#endregion

		#region Inline Class: Multi-param get indexer

		[Fact]
		public void MultiParam_Get_InlineClass_CallbackReceivesAllParams()
		{
			var stub = new ClassIndexerInlineTests.Stubs.ClassIndexerGetter();
			ClassIndexerGetter obj = stub.Object;

			stub.Indexer.Get(((int a, string b) key) => key.a + key.b.Length);

			int result = obj[3, "hello"];

			Assert.Equal(8, result);
		}

		[Fact]
		public void MultiParam_Get_InlineClass_UnconfiguredFallsToBase()
		{
			var stub = new ClassIndexerInlineTests.Stubs.ClassIndexerGetter();
			ClassIndexerGetter obj = stub.Object;

			int result = obj[3, "hello"];

			Assert.Equal(0, result);
		}

		#endregion

		#region Standalone Class: Multi-param set indexer

		[Fact]
		public void MultiParam_Set_StandaloneClass_CallbackCapturesKeyAndValue()
		{
			var stub = new ClassIndexerSetterStandalone();
			ClassIndexerSetter obj = stub.Object;

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
			var stub = new ClassIndexerInlineTests.Stubs.ClassIndexerSetter();
			ClassIndexerSetter obj = stub.Object;

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
			var stub = new ClassIndexerGetterSetterStandalone();
			ClassIndexerGetterSetter obj = stub.Object;

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
			var stub = new ClassIndexerInlineTests.Stubs.ClassIndexerGetterSetter();
			ClassIndexerGetterSetter obj = stub.Object;

			stub.Indexer.Get(((int a, string b) key) => key.a * key.b.Length);

			int getResult = obj[2, "abc"];
			obj[2, "abc"] = 99;

			Assert.Equal(6, getResult);
			stub.Indexer.VerifyGet(Called.Once);
			stub.Indexer.VerifySet(Called.Once);
		}

		#endregion

		// ====================================================================
		// Scenario 7: Event raising from indexer get
		// Use a getter callback that explicitly raises the event.
		// ====================================================================

		#region Standalone Class: Event raised during indexer get

		[Fact]
		public void EventOnGet_StandaloneClass_CallbackRaisesEvent()
		{
			var stub = new ClassIndexerGetterStandalone();
			ClassIndexerGetter obj = stub.Object;

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
			var stub = new ClassIndexerInlineTests.Stubs.ClassIndexerGetter();
			ClassIndexerGetter obj = stub.Object;

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
		// Scenario 8: Event raising from indexer set
		// Use a setter callback that explicitly raises the event.
		// ====================================================================

		#region Standalone Class: Event raised during indexer set

		[Fact]
		public void EventOnSet_StandaloneClass_CallbackRaisesEvent()
		{
			var stub = new ClassIndexerSetterStandalone();
			ClassIndexerSetter obj = stub.Object;

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
			var stub = new ClassIndexerInlineTests.Stubs.ClassIndexerSetter();
			ClassIndexerSetter obj = stub.Object;

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
			var stub = new ClassIndexerGetterInitStandalone();
			ClassIndexerGetterInit obj = stub.Object;

			stub.Indexer.Get(((int a, string b) key) => key.a + key.b.Length);

			int result = obj[3, "hello"];

			Assert.Equal(8, result);
		}

		[Fact]
		public void MultiParamGetInit_StandaloneClass_UnconfiguredFallsToBase()
		{
			var stub = new ClassIndexerGetterInitStandalone();
			ClassIndexerGetterInit obj = stub.Object;

			int result = obj[3, "hello"];

			Assert.Equal(0, result); // base returns default
		}

		#endregion

		#region Inline Class: Multi-param GetterInit

		[Fact]
		public void MultiParamGetInit_InlineClass_ConfiguredReturnsValue()
		{
			var stub = new ClassIndexerInlineTests.Stubs.ClassIndexerGetterInit();
			ClassIndexerGetterInit obj = stub.Object;

			stub.Indexer.Get(((int a, string b) key) => key.a + key.b.Length);

			int result = obj[3, "hello"];

			Assert.Equal(8, result);
		}

		[Fact]
		public void MultiParamGetInit_InlineClass_UnconfiguredFallsToBase()
		{
			var stub = new ClassIndexerInlineTests.Stubs.ClassIndexerGetterInit();
			ClassIndexerGetterInit obj = stub.Object;

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
			var stub = new ClassIndexerGetterStandalone();
			ClassIndexerGetter obj = stub.Object;

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
			var stub = new ClassIndexerSetterStandalone();
			ClassIndexerSetter obj = stub.Object;

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
			var stub = new ClassIndexerInlineTests.Stubs.ClassIndexerGetter();
			ClassIndexerGetter obj = stub.Object;

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
			var stub = new ClassIndexerInlineTests.Stubs.ClassIndexerSetter();
			ClassIndexerSetter obj = stub.Object;

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
