// ============================================================================
// InterfaceIndexerTests: Edge case tests for interfaces with indexers
// covering every accessor combination plus events.
// Inspired by Rocks.Analysis.IntegrationTests.InterfaceIndexerTests
//
// Target interfaces:
//   IInterfaceIndexerGetter       - get-only, with event
//   IInterfaceIndexerGetterSetter - get + set
//   IInterfaceIndexerGetterInit   - get + init
//   IInterfaceIndexerSetter       - set-only, with event
//   IInterfaceIndexerInit         - init-only, with event
//
// Each interface has BOTH single-parameter (this[int a]) and multi-parameter
// (this[int a, string b]) indexers.
//
// Tests exercise the INTERFACE pipeline (not class) for:
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
// Applicable patterns (interface only, non-generic):
// - Pattern 1 (Standalone): [KnockOff] partial class Stub : IInterface {}
// - Pattern 5 (Inline): [KnockOff<IInterface>]
// ============================================================================

using KnockOff;

// ============================================================================
// TYPE DEFINITIONS
// ============================================================================

namespace KnockOff.Analysis.Tests.InterfaceIndexerTestTypes
{
	public interface IInterfaceIndexerGetter
	{
		int this[int a] { get; }
		int this[int a, string b] { get; }

		event EventHandler MyEvent;
	}

	public interface IInterfaceIndexerGetterSetter
	{
		int this[int a] { get; set; }
		int this[int a, string b] { get; set; }
	}

	public interface IInterfaceIndexerGetterInit
	{
		int this[int a] { get; init; }
		int this[int a, string b] { get; init; }
	}

	public interface IInterfaceIndexerSetter
	{
#pragma warning disable CA1044 // Properties should not be write only
		int this[int a] { set; }
		int this[int a, string b] { set; }
#pragma warning restore CA1044 // Properties should not be write only

		event EventHandler MyEvent;
	}

	public interface IInterfaceIndexerInit
	{
#pragma warning disable CA1044 // Properties should not be write only
		int this[int a] { init; }
		int this[int a, string b] { init; }
#pragma warning restore CA1044 // Properties should not be write only

		event EventHandler MyEvent;
	}

	// Pattern 1: Standalone interface stubs
	[KnockOff]
	public partial class InterfaceIndexerGetterStandalone : IInterfaceIndexerGetter { }

	[KnockOff]
	public partial class InterfaceIndexerGetterSetterStandalone : IInterfaceIndexerGetterSetter { }

	[KnockOff]
	public partial class InterfaceIndexerGetterInitStandalone : IInterfaceIndexerGetterInit { }

	[KnockOff]
	public partial class InterfaceIndexerSetterStandalone : IInterfaceIndexerSetter { }

	[KnockOff]
	public partial class InterfaceIndexerInitStandalone : IInterfaceIndexerInit { }
}

// ============================================================================
// INLINE PATTERN DECLARATIONS
// ============================================================================

namespace KnockOff.Analysis.Tests
{
	using KnockOff.Analysis.Tests.InterfaceIndexerTestTypes;

	// Pattern 5: Inline interface stubs
	[KnockOff<IInterfaceIndexerGetter>]
	[KnockOff<IInterfaceIndexerGetterSetter>]
	[KnockOff<IInterfaceIndexerGetterInit>]
	[KnockOff<IInterfaceIndexerSetter>]
	[KnockOff<IInterfaceIndexerInit>]
	public partial class InterfaceIndexerInlineTests { }

	// ========================================================================
	// TESTS
	// ========================================================================

	public class InterfaceIndexerTests
	{
		// ====================================================================
		// Scenario 1: Get-only indexer (single param) — configure return, assert
		// ====================================================================

		#region Standalone: Get-only single-param indexer

		[Fact]
		public void GetOnly_SingleParam_Standalone_ConfiguredReturnsValue()
		{
			var stub = new InterfaceIndexerGetterStandalone();
			IInterfaceIndexerGetter service = stub;

			stub.Indexer.Get((int key) => key * 10);

			int result = service[3];

			Assert.Equal(30, result);
		}

		[Fact]
		public void GetOnly_SingleParam_Standalone_UnconfiguredReturnsDefault()
		{
			var stub = new InterfaceIndexerGetterStandalone();
			IInterfaceIndexerGetter service = stub;

			int result = service[5];

			Assert.Equal(0, result); // default(int) = 0
		}

		[Fact]
		public void GetOnly_SingleParam_Standalone_VerifyTracksAccess()
		{
			var stub = new InterfaceIndexerGetterStandalone();
			IInterfaceIndexerGetter service = stub;

			stub.Indexer.Get((int key) => key);
			_ = service[1];
			_ = service[2];

			stub.Indexer.VerifyGet(Called.Exactly(2));
		}

		#endregion

		#region Inline: Get-only single-param indexer

		[Fact]
		public void GetOnly_SingleParam_Inline_ConfiguredReturnsValue()
		{
			var stub = new InterfaceIndexerInlineTests.Stubs.IInterfaceIndexerGetter();
			IInterfaceIndexerGetter service = stub;

			stub.Indexer.Get((int key) => key * 10);

			int result = service[3];

			Assert.Equal(30, result);
		}

		[Fact]
		public void GetOnly_SingleParam_Inline_UnconfiguredReturnsDefault()
		{
			var stub = new InterfaceIndexerInlineTests.Stubs.IInterfaceIndexerGetter();
			IInterfaceIndexerGetter service = stub;

			int result = service[5];

			Assert.Equal(0, result);
		}

		[Fact]
		public void GetOnly_SingleParam_Inline_VerifyTracksAccess()
		{
			var stub = new InterfaceIndexerInlineTests.Stubs.IInterfaceIndexerGetter();
			IInterfaceIndexerGetter service = stub;

			stub.Indexer.Get((int key) => key);
			_ = service[1];
			_ = service[2];

			stub.Indexer.VerifyGet(Called.Exactly(2));
		}

		#endregion

		// ====================================================================
		// Scenario 2: Set-only indexer (single param) — set value, verify tracking
		// ====================================================================

		#region Standalone: Set-only single-param indexer

		[Fact]
		public void SetOnly_SingleParam_Standalone_SetIsTracked()
		{
			var stub = new InterfaceIndexerSetterStandalone();
			IInterfaceIndexerSetter service = stub;

			service[3] = 42;

			stub.Indexer.VerifySet(Called.Once);
		}

		[Fact]
		public void SetOnly_SingleParam_Standalone_LastSetEntryCaptured()
		{
			var stub = new InterfaceIndexerSetterStandalone();
			IInterfaceIndexerSetter service = stub;

			service[7] = 99;

			var lastEntry = stub.Indexer.LastInt32SetEntry;
			Assert.NotNull(lastEntry);
			Assert.Equal(7, lastEntry!.Value.Key);
			Assert.Equal(99, lastEntry!.Value.Value);
		}

		#endregion

		#region Inline: Set-only single-param indexer

		[Fact]
		public void SetOnly_SingleParam_Inline_SetIsTracked()
		{
			var stub = new InterfaceIndexerInlineTests.Stubs.IInterfaceIndexerSetter();
			IInterfaceIndexerSetter service = stub;

			service[3] = 42;

			stub.Indexer.VerifySet(Called.Once);
		}

		[Fact]
		public void SetOnly_SingleParam_Inline_LastSetEntryCaptured()
		{
			var stub = new InterfaceIndexerInlineTests.Stubs.IInterfaceIndexerSetter();
			IInterfaceIndexerSetter service = stub;

			service[7] = 99;

			var lastEntry = stub.Indexer.LastInt32SetEntry;
			Assert.NotNull(lastEntry);
			Assert.Equal(7, lastEntry!.Value.Key);
			Assert.Equal(99, lastEntry!.Value.Value);
		}

		#endregion

		// ====================================================================
		// Scenario 3: Get/Set indexer (single param) — configure get, set, verify
		// ====================================================================

		#region Standalone: Get/Set single-param indexer

		[Fact]
		public void GetSet_SingleParam_Standalone_ConfigureGetAndSet()
		{
			var stub = new InterfaceIndexerGetterSetterStandalone();
			IInterfaceIndexerGetterSetter service = stub;

			stub.Indexer.Get((int key) => key + 100);

			int getResult = service[5];
			service[5] = 42;

			Assert.Equal(105, getResult);
			stub.Indexer.VerifyGet(Called.Once);
			stub.Indexer.VerifySet(Called.Once);
		}

		[Fact]
		public void GetSet_SingleParam_Standalone_SetCapturesValue()
		{
			var stub = new InterfaceIndexerGetterSetterStandalone();
			IInterfaceIndexerGetterSetter service = stub;

			int? capturedKey = null;
			int? capturedValue = null;
			stub.Indexer.Set((int key, int value) =>
			{
				capturedKey = key;
				capturedValue = value;
			});

			service[3] = 77;

			Assert.Equal(3, capturedKey);
			Assert.Equal(77, capturedValue);
		}

		#endregion

		#region Inline: Get/Set single-param indexer

		[Fact]
		public void GetSet_SingleParam_Inline_ConfigureGetAndSet()
		{
			var stub = new InterfaceIndexerInlineTests.Stubs.IInterfaceIndexerGetterSetter();
			IInterfaceIndexerGetterSetter service = stub;

			stub.Indexer.Get((int key) => key + 100);

			int getResult = service[5];
			service[5] = 42;

			Assert.Equal(105, getResult);
			stub.Indexer.VerifyGet(Called.Once);
			stub.Indexer.VerifySet(Called.Once);
		}

		[Fact]
		public void GetSet_SingleParam_Inline_SetCapturesValue()
		{
			var stub = new InterfaceIndexerInlineTests.Stubs.IInterfaceIndexerGetterSetter();
			IInterfaceIndexerGetterSetter service = stub;

			int? capturedKey = null;
			int? capturedValue = null;
			stub.Indexer.Set((int key, int value) =>
			{
				capturedKey = key;
				capturedValue = value;
			});

			service[3] = 77;

			Assert.Equal(3, capturedKey);
			Assert.Equal(77, capturedValue);
		}

		#endregion

		// ====================================================================
		// Scenario 4: Get/Init indexer (single param) — configure get, read, verify
		// ====================================================================

		#region Standalone: Get/Init single-param indexer

		[Fact]
		public void GetInit_SingleParam_Standalone_ConfiguredReturnsValue()
		{
			var stub = new InterfaceIndexerGetterInitStandalone();
			IInterfaceIndexerGetterInit service = stub;

			stub.Indexer.Get((int key) => key * 5);

			int result = service[4];

			Assert.Equal(20, result);
		}

		[Fact]
		public void GetInit_SingleParam_Standalone_VerifyTracksAccess()
		{
			var stub = new InterfaceIndexerGetterInitStandalone();
			IInterfaceIndexerGetterInit service = stub;

			stub.Indexer.Get((int key) => key);
			_ = service[1];

			stub.Indexer.VerifyGet(Called.Once);
		}

		#endregion

		#region Inline: Get/Init single-param indexer

		[Fact]
		public void GetInit_SingleParam_Inline_ConfiguredReturnsValue()
		{
			var stub = new InterfaceIndexerInlineTests.Stubs.IInterfaceIndexerGetterInit();
			IInterfaceIndexerGetterInit service = stub;

			stub.Indexer.Get((int key) => key * 5);

			int result = service[4];

			Assert.Equal(20, result);
		}

		[Fact]
		public void GetInit_SingleParam_Inline_VerifyTracksAccess()
		{
			var stub = new InterfaceIndexerInlineTests.Stubs.IInterfaceIndexerGetterInit();
			IInterfaceIndexerGetterInit service = stub;

			stub.Indexer.Get((int key) => key);
			_ = service[1];

			stub.Indexer.VerifyGet(Called.Once);
		}

		#endregion

		// ====================================================================
		// Scenario 5: Init-only indexer — verify stub can be instantiated
		// Init-only indexers on interfaces cannot be set after construction.
		// ====================================================================

		#region Standalone: Init-only indexer

		[Fact]
		public void InitOnly_Standalone_StubCanBeInstantiated()
		{
			var stub = new InterfaceIndexerInitStandalone();

			Assert.NotNull(stub);
		}

		#endregion

		#region Inline: Init-only indexer

		[Fact]
		public void InitOnly_Inline_StubCanBeInstantiated()
		{
			var stub = new InterfaceIndexerInlineTests.Stubs.IInterfaceIndexerInit();

			Assert.NotNull(stub);
		}

		#endregion

		// ====================================================================
		// Scenario 6: Multi-parameter indexer — callback receives ALL key params
		// Multi-param indexers use tuple keys: (int a, string b)
		// ====================================================================

		#region Standalone: Multi-param get indexer

		[Fact]
		public void MultiParam_Get_Standalone_CallbackReceivesAllParams()
		{
			var stub = new InterfaceIndexerGetterStandalone();
			IInterfaceIndexerGetter service = stub;

			stub.Indexer.Get(((int a, string b) key) => key.a + key.b.Length);

			int result = service[3, "hello"];

			Assert.Equal(8, result); // 3 + 5
		}

		[Fact]
		public void MultiParam_Get_Standalone_UnconfiguredReturnsDefault()
		{
			var stub = new InterfaceIndexerGetterStandalone();
			IInterfaceIndexerGetter service = stub;

			int result = service[3, "hello"];

			Assert.Equal(0, result); // default(int) = 0
		}

		#endregion

		#region Inline: Multi-param get indexer

		[Fact]
		public void MultiParam_Get_Inline_CallbackReceivesAllParams()
		{
			var stub = new InterfaceIndexerInlineTests.Stubs.IInterfaceIndexerGetter();
			IInterfaceIndexerGetter service = stub;

			stub.Indexer.Get(((int a, string b) key) => key.a + key.b.Length);

			int result = service[3, "hello"];

			Assert.Equal(8, result);
		}

		[Fact]
		public void MultiParam_Get_Inline_UnconfiguredReturnsDefault()
		{
			var stub = new InterfaceIndexerInlineTests.Stubs.IInterfaceIndexerGetter();
			IInterfaceIndexerGetter service = stub;

			int result = service[3, "hello"];

			Assert.Equal(0, result);
		}

		#endregion

		#region Standalone: Multi-param set indexer

		[Fact]
		public void MultiParam_Set_Standalone_CallbackCapturesKeyAndValue()
		{
			var stub = new InterfaceIndexerSetterStandalone();
			IInterfaceIndexerSetter service = stub;

			(int, string)? capturedKey = null;
			int? capturedValue = null;
			stub.Indexer.Set(((int a, string b) key, int value) =>
			{
				capturedKey = (key.a, key.b);
				capturedValue = value;
			});

			service[5, "test"] = 42;

			Assert.Equal((5, "test"), capturedKey);
			Assert.Equal(42, capturedValue);
		}

		#endregion

		#region Inline: Multi-param set indexer

		[Fact]
		public void MultiParam_Set_Inline_CallbackCapturesKeyAndValue()
		{
			var stub = new InterfaceIndexerInlineTests.Stubs.IInterfaceIndexerSetter();
			IInterfaceIndexerSetter service = stub;

			(int, string)? capturedKey = null;
			int? capturedValue = null;
			stub.Indexer.Set(((int a, string b) key, int value) =>
			{
				capturedKey = (key.a, key.b);
				capturedValue = value;
			});

			service[5, "test"] = 42;

			Assert.Equal((5, "test"), capturedKey);
			Assert.Equal(42, capturedValue);
		}

		#endregion

		#region Standalone: Multi-param get/set indexer

		[Fact]
		public void MultiParam_GetSet_Standalone_BothWork()
		{
			var stub = new InterfaceIndexerGetterSetterStandalone();
			IInterfaceIndexerGetterSetter service = stub;

			stub.Indexer.Get(((int a, string b) key) => key.a * key.b.Length);

			int getResult = service[2, "abc"];
			service[2, "abc"] = 99;

			Assert.Equal(6, getResult); // 2 * 3
			stub.Indexer.VerifyGet(Called.Once);
			stub.Indexer.VerifySet(Called.Once);
		}

		#endregion

		#region Inline: Multi-param get/set indexer

		[Fact]
		public void MultiParam_GetSet_Inline_BothWork()
		{
			var stub = new InterfaceIndexerInlineTests.Stubs.IInterfaceIndexerGetterSetter();
			IInterfaceIndexerGetterSetter service = stub;

			stub.Indexer.Get(((int a, string b) key) => key.a * key.b.Length);

			int getResult = service[2, "abc"];
			service[2, "abc"] = 99;

			Assert.Equal(6, getResult);
			stub.Indexer.VerifyGet(Called.Once);
			stub.Indexer.VerifySet(Called.Once);
		}

		#endregion

		// ====================================================================
		// Scenario 7: Event raising from indexer get
		// Use a getter callback that explicitly raises the event.
		// ====================================================================

		#region Standalone: Event raised during indexer get

		[Fact]
		public void EventOnGet_Standalone_CallbackRaisesEvent()
		{
			var stub = new InterfaceIndexerGetterStandalone();
			IInterfaceIndexerGetter service = stub;

			stub.Indexer.Get((int key) =>
			{
				stub.MyEvent.Raise(stub, EventArgs.Empty);
				return key * 10;
			});

			var wasEventRaised = false;
			service.MyEvent += (s, e) => wasEventRaised = true;

			int value = service[3];

			Assert.Equal(30, value);
			Assert.True(wasEventRaised);
		}

		#endregion

		#region Inline: Event raised during indexer get

		[Fact]
		public void EventOnGet_Inline_CallbackRaisesEvent()
		{
			var stub = new InterfaceIndexerInlineTests.Stubs.IInterfaceIndexerGetter();
			IInterfaceIndexerGetter service = stub;

			stub.Indexer.Get((int key) =>
			{
				stub.MyEvent.Raise(stub, EventArgs.Empty);
				return key * 10;
			});

			var wasEventRaised = false;
			service.MyEvent += (s, e) => wasEventRaised = true;

			int value = service[3];

			Assert.Equal(30, value);
			Assert.True(wasEventRaised);
		}

		#endregion

		// ====================================================================
		// Scenario 8: Event raising from indexer set
		// Use a setter callback that explicitly raises the event.
		// ====================================================================

		#region Standalone: Event raised during indexer set

		[Fact]
		public void EventOnSet_Standalone_CallbackRaisesEvent()
		{
			var stub = new InterfaceIndexerSetterStandalone();
			IInterfaceIndexerSetter service = stub;

			stub.Indexer.Set((int key, int value) =>
			{
				stub.MyEvent.Raise(stub, EventArgs.Empty);
			});

			var wasEventRaised = false;
			service.MyEvent += (s, e) => wasEventRaised = true;

			service[3] = 42;

			Assert.True(wasEventRaised);
		}

		#endregion

		#region Inline: Event raised during indexer set

		[Fact]
		public void EventOnSet_Inline_CallbackRaisesEvent()
		{
			var stub = new InterfaceIndexerInlineTests.Stubs.IInterfaceIndexerSetter();
			IInterfaceIndexerSetter service = stub;

			stub.Indexer.Set((int key, int value) =>
			{
				stub.MyEvent.Raise(stub, EventArgs.Empty);
			});

			var wasEventRaised = false;
			service.MyEvent += (s, e) => wasEventRaised = true;

			service[3] = 42;

			Assert.True(wasEventRaised);
		}

		#endregion

		// ====================================================================
		// Multi-param GetterInit indexer
		// ====================================================================

		#region Standalone: Multi-param GetterInit

		[Fact]
		public void MultiParamGetInit_Standalone_ConfiguredReturnsValue()
		{
			var stub = new InterfaceIndexerGetterInitStandalone();
			IInterfaceIndexerGetterInit service = stub;

			stub.Indexer.Get(((int a, string b) key) => key.a + key.b.Length);

			int result = service[3, "hello"];

			Assert.Equal(8, result);
		}

		[Fact]
		public void MultiParamGetInit_Standalone_UnconfiguredReturnsDefault()
		{
			var stub = new InterfaceIndexerGetterInitStandalone();
			IInterfaceIndexerGetterInit service = stub;

			int result = service[3, "hello"];

			Assert.Equal(0, result); // default(int) = 0
		}

		#endregion

		#region Inline: Multi-param GetterInit

		[Fact]
		public void MultiParamGetInit_Inline_ConfiguredReturnsValue()
		{
			var stub = new InterfaceIndexerInlineTests.Stubs.IInterfaceIndexerGetterInit();
			IInterfaceIndexerGetterInit service = stub;

			stub.Indexer.Get(((int a, string b) key) => key.a + key.b.Length);

			int result = service[3, "hello"];

			Assert.Equal(8, result);
		}

		[Fact]
		public void MultiParamGetInit_Inline_UnconfiguredReturnsDefault()
		{
			var stub = new InterfaceIndexerInlineTests.Stubs.IInterfaceIndexerGetterInit();
			IInterfaceIndexerGetterInit service = stub;

			int result = service[3, "hello"];

			Assert.Equal(0, result);
		}

		#endregion

		// ====================================================================
		// Multi-param event raising from indexer access
		// ====================================================================

		#region Standalone: Multi-param event on get/set

		[Fact]
		public void MultiParamEventOnGet_Standalone_EventFires()
		{
			var stub = new InterfaceIndexerGetterStandalone();
			IInterfaceIndexerGetter service = stub;

			stub.Indexer.Get(((int a, string b) key) =>
			{
				stub.MyEvent.Raise(stub, EventArgs.Empty);
				return key.a;
			});

			var wasEventRaised = false;
			service.MyEvent += (s, e) => wasEventRaised = true;

			int result = service[3, "test"];

			Assert.Equal(3, result);
			Assert.True(wasEventRaised);
		}

		[Fact]
		public void MultiParamEventOnSet_Standalone_EventFires()
		{
			var stub = new InterfaceIndexerSetterStandalone();
			IInterfaceIndexerSetter service = stub;

			stub.Indexer.Set(((int a, string b) key, int value) =>
			{
				stub.MyEvent.Raise(stub, EventArgs.Empty);
			});

			var wasEventRaised = false;
			service.MyEvent += (s, e) => wasEventRaised = true;

			service[3, "test"] = 42;

			Assert.True(wasEventRaised);
		}

		#endregion

		#region Inline: Multi-param event on get/set

		[Fact]
		public void MultiParamEventOnGet_Inline_EventFires()
		{
			var stub = new InterfaceIndexerInlineTests.Stubs.IInterfaceIndexerGetter();
			IInterfaceIndexerGetter service = stub;

			stub.Indexer.Get(((int a, string b) key) =>
			{
				stub.MyEvent.Raise(stub, EventArgs.Empty);
				return key.a;
			});

			var wasEventRaised = false;
			service.MyEvent += (s, e) => wasEventRaised = true;

			int result = service[3, "test"];

			Assert.Equal(3, result);
			Assert.True(wasEventRaised);
		}

		[Fact]
		public void MultiParamEventOnSet_Inline_EventFires()
		{
			var stub = new InterfaceIndexerInlineTests.Stubs.IInterfaceIndexerSetter();
			IInterfaceIndexerSetter service = stub;

			stub.Indexer.Set(((int a, string b) key, int value) =>
			{
				stub.MyEvent.Raise(stub, EventArgs.Empty);
			});

			var wasEventRaised = false;
			service.MyEvent += (s, e) => wasEventRaised = true;

			service[3, "test"] = 42;

			Assert.True(wasEventRaised);
		}

		#endregion
	}
}
