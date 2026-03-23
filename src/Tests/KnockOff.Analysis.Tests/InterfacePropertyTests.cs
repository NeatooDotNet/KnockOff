// ============================================================================
// InterfacePropertyTests: Edge case tests for an interface with properties
// covering every accessor combination plus an event.
// Inspired by Rocks.Analysis.IntegrationTests.InterfacePropertyTests
//
// Target interface:
//   public interface IInterfaceProperty
//   {
//       int GetData { get; }
//       int GetAndInitData { get; set; }
//       int GetAndSetData { get; set; }
//       int InitData { set; }
//       int SetData { set; }
//       event EventHandler MyEvent;
//   }
//
// Tests exercise the INTERFACE pipeline (not class) for:
// 1. Get-only property — configure return value, read it, assert
// 2. Set-only property — set a value, verify the set was tracked
// 3. Get/Set property — configure get, set a value, verify both
// 4. Get/Set property (GetAndInitData) — same as scenario 3
// 5. Unconfigured get-only — returns default(int) = 0
// 6. Event raising from property get — use callback to raise event on get
// 7. Event raising from property set — use callback to raise event on set
// 8. Callback on property get — configure callback that returns computed value
// 9. Callback on property set — configure callback that captures the set value
// 10. Combined callback + event — callback and event raising together
//
// Applicable patterns (interface only, non-generic):
// - Pattern 1 (Standalone): [KnockOff] partial class Stub : IInterface {}
// - Pattern 5 (Inline): [KnockOff<IInterface>]
// ============================================================================

using KnockOff;

// ============================================================================
// TYPE DEFINITIONS
// ============================================================================

namespace KnockOff.Analysis.Tests.InterfacePropertyTestTypes
{
	public interface IInterfaceProperty
	{
		int GetData { get; }
		int GetAndInitData { get; set; }
		int GetAndSetData { get; set; }
#pragma warning disable CA1044 // Properties should not be write only
		int InitData { set; }
		int SetData { set; }
#pragma warning restore CA1044 // Properties should not be write only

		event EventHandler MyEvent;
	}

	// Pattern 1: Standalone interface stub
	[KnockOff]
	public partial class InterfacePropertyStandaloneKnockOff : IInterfaceProperty
	{
	}
}

// ============================================================================
// INLINE PATTERN DECLARATIONS + TESTS
// ============================================================================

namespace KnockOff.Analysis.Tests
{
	using KnockOff.Analysis.Tests.InterfacePropertyTestTypes;

	// Pattern 5: Inline interface stub
	[KnockOff<IInterfaceProperty>]
	public partial class InterfacePropertyInlineTests
	{
	}

	public class InterfacePropertyTests
	{
		// ====================================================================
		// Scenario 1: Get-only property — configure return value, read, assert
		// ====================================================================

		#region Standalone: Get-only property (GetData)

		[Fact]
		public void GetOnly_Standalone_ConfiguredReturnsValue()
		{
			var stub = new InterfacePropertyStandaloneKnockOff();
			IInterfaceProperty service = stub;

			stub.GetData.Get(42);

			int result = service.GetData;

			Assert.Equal(42, result);
		}

		[Fact]
		public void GetOnly_Standalone_UnconfiguredReturnsDefault()
		{
			// Interface stubs have no base; unconfigured returns default(int) = 0
			var stub = new InterfacePropertyStandaloneKnockOff();
			IInterfaceProperty service = stub;

			int result = service.GetData;

			Assert.Equal(0, result);
		}

		[Fact]
		public void GetOnly_Standalone_VerifyTracksAccess()
		{
			var stub = new InterfacePropertyStandaloneKnockOff();
			IInterfaceProperty service = stub;

			stub.GetData.Get(10);
			_ = service.GetData;
			_ = service.GetData;

			stub.GetData.VerifyGet(Called.Exactly(2));
		}

		#endregion

		#region Inline: Get-only property (GetData)

		[Fact]
		public void GetOnly_Inline_ConfiguredReturnsValue()
		{
			var stub = new InterfacePropertyInlineTests.Stubs.IInterfaceProperty();
			IInterfaceProperty service = stub;

			stub.GetData.Get(42);

			int result = service.GetData;

			Assert.Equal(42, result);
		}

		[Fact]
		public void GetOnly_Inline_UnconfiguredReturnsDefault()
		{
			var stub = new InterfacePropertyInlineTests.Stubs.IInterfaceProperty();
			IInterfaceProperty service = stub;

			int result = service.GetData;

			Assert.Equal(0, result);
		}

		[Fact]
		public void GetOnly_Inline_VerifyTracksAccess()
		{
			var stub = new InterfacePropertyInlineTests.Stubs.IInterfaceProperty();
			IInterfaceProperty service = stub;

			stub.GetData.Get(10);
			_ = service.GetData;
			_ = service.GetData;

			stub.GetData.VerifyGet(Called.Exactly(2));
		}

		#endregion

		// ====================================================================
		// Scenario 2: Set-only property — set a value, verify set tracked
		// ====================================================================

		#region Standalone: Set-only property (SetData)

		[Fact]
		public void SetOnly_Standalone_SetIsTracked()
		{
			var stub = new InterfacePropertyStandaloneKnockOff();
			IInterfaceProperty service = stub;

			service.SetData = 99;

			stub.SetData.VerifySet(Called.Once);
		}

		[Fact]
		public void SetOnly_Standalone_LastSetValueCaptured()
		{
			var stub = new InterfacePropertyStandaloneKnockOff();
			IInterfaceProperty service = stub;

			service.SetData = 77;

			Assert.Equal(77, stub.SetData.LastSetValue);
		}

		#endregion

		#region Inline: Set-only property (SetData)

		[Fact]
		public void SetOnly_Inline_SetIsTracked()
		{
			var stub = new InterfacePropertyInlineTests.Stubs.IInterfaceProperty();
			IInterfaceProperty service = stub;

			service.SetData = 99;

			stub.SetData.VerifySet(Called.Once);
		}

		[Fact]
		public void SetOnly_Inline_LastSetValueCaptured()
		{
			var stub = new InterfacePropertyInlineTests.Stubs.IInterfaceProperty();
			IInterfaceProperty service = stub;

			service.SetData = 77;

			Assert.Equal(77, stub.SetData.LastSetValue);
		}

		#endregion

		// ====================================================================
		// Scenario 3: Get/Set property — configure get, set value, verify both
		// ====================================================================

		#region Standalone: Get/Set property (GetAndSetData)

		[Fact]
		public void GetSet_Standalone_ConfigureGetAndSet()
		{
			var stub = new InterfacePropertyStandaloneKnockOff();
			IInterfaceProperty service = stub;

			stub.GetAndSetData.Get(10);

			int getResult = service.GetAndSetData;
			service.GetAndSetData = 20;

			Assert.Equal(10, getResult);
			stub.GetAndSetData.VerifyGet(Called.Once);
			stub.GetAndSetData.VerifySet(Called.Once);
			Assert.Equal(20, stub.GetAndSetData.LastSetValue);
		}

		[Fact]
		public void GetSet_Standalone_UnconfiguredReturnsDefault()
		{
			var stub = new InterfacePropertyStandaloneKnockOff();
			IInterfaceProperty service = stub;

			// Interface stub unconfigured returns default(int) = 0
			int result = service.GetAndSetData;

			Assert.Equal(0, result);
		}

		#endregion

		#region Inline: Get/Set property (GetAndSetData)

		[Fact]
		public void GetSet_Inline_ConfigureGetAndSet()
		{
			var stub = new InterfacePropertyInlineTests.Stubs.IInterfaceProperty();
			IInterfaceProperty service = stub;

			stub.GetAndSetData.Get(10);

			int getResult = service.GetAndSetData;
			service.GetAndSetData = 20;

			Assert.Equal(10, getResult);
			stub.GetAndSetData.VerifyGet(Called.Once);
			stub.GetAndSetData.VerifySet(Called.Once);
			Assert.Equal(20, stub.GetAndSetData.LastSetValue);
		}

		[Fact]
		public void GetSet_Inline_UnconfiguredReturnsDefault()
		{
			var stub = new InterfacePropertyInlineTests.Stubs.IInterfaceProperty();
			IInterfaceProperty service = stub;

			int result = service.GetAndSetData;

			Assert.Equal(0, result);
		}

		#endregion

		// ====================================================================
		// Scenario 4: Get/Set property (GetAndInitData) — same behavior as
		// GetAndSetData since this is { get; set; } on an interface
		// ====================================================================

		#region Standalone: Get/Set property (GetAndInitData)

		[Fact]
		public void GetAndInitData_Standalone_ConfiguredReturnsValue()
		{
			var stub = new InterfacePropertyStandaloneKnockOff();
			IInterfaceProperty service = stub;

			stub.GetAndInitData.Get(55);

			int result = service.GetAndInitData;

			Assert.Equal(55, result);
		}

		[Fact]
		public void GetAndInitData_Standalone_UnconfiguredReturnsDefault()
		{
			var stub = new InterfacePropertyStandaloneKnockOff();
			IInterfaceProperty service = stub;

			int result = service.GetAndInitData;

			Assert.Equal(0, result);
		}

		[Fact]
		public void GetAndInitData_Standalone_VerifyTracksAccess()
		{
			var stub = new InterfacePropertyStandaloneKnockOff();
			IInterfaceProperty service = stub;

			stub.GetAndInitData.Get(1);
			_ = service.GetAndInitData;

			stub.GetAndInitData.VerifyGet(Called.Once);
		}

		#endregion

		#region Inline: Get/Set property (GetAndInitData)

		[Fact]
		public void GetAndInitData_Inline_ConfiguredReturnsValue()
		{
			var stub = new InterfacePropertyInlineTests.Stubs.IInterfaceProperty();
			IInterfaceProperty service = stub;

			stub.GetAndInitData.Get(55);

			int result = service.GetAndInitData;

			Assert.Equal(55, result);
		}

		[Fact]
		public void GetAndInitData_Inline_UnconfiguredReturnsDefault()
		{
			var stub = new InterfacePropertyInlineTests.Stubs.IInterfaceProperty();
			IInterfaceProperty service = stub;

			int result = service.GetAndInitData;

			Assert.Equal(0, result);
		}

		[Fact]
		public void GetAndInitData_Inline_VerifyTracksAccess()
		{
			var stub = new InterfacePropertyInlineTests.Stubs.IInterfaceProperty();
			IInterfaceProperty service = stub;

			stub.GetAndInitData.Get(1);
			_ = service.GetAndInitData;

			stub.GetAndInitData.VerifyGet(Called.Once);
		}

		#endregion

		// ====================================================================
		// Scenario 5: Set-only property (InitData) — set a value, verify
		// Same behavior as SetData since both are { set; } on an interface
		// ====================================================================

		#region Standalone: Set-only property (InitData)

		[Fact]
		public void InitData_Standalone_SetIsTracked()
		{
			var stub = new InterfacePropertyStandaloneKnockOff();
			IInterfaceProperty service = stub;

			service.InitData = 33;

			stub.InitData.VerifySet(Called.Once);
		}

		[Fact]
		public void InitData_Standalone_LastSetValueCaptured()
		{
			var stub = new InterfacePropertyStandaloneKnockOff();
			IInterfaceProperty service = stub;

			service.InitData = 44;

			Assert.Equal(44, stub.InitData.LastSetValue);
		}

		#endregion

		#region Inline: Set-only property (InitData)

		[Fact]
		public void InitData_Inline_SetIsTracked()
		{
			var stub = new InterfacePropertyInlineTests.Stubs.IInterfaceProperty();
			IInterfaceProperty service = stub;

			service.InitData = 33;

			stub.InitData.VerifySet(Called.Once);
		}

		[Fact]
		public void InitData_Inline_LastSetValueCaptured()
		{
			var stub = new InterfacePropertyInlineTests.Stubs.IInterfaceProperty();
			IInterfaceProperty service = stub;

			service.InitData = 44;

			Assert.Equal(44, stub.InitData.LastSetValue);
		}

		#endregion

		// ====================================================================
		// Scenario 6: Event raising from property get
		// KnockOff does not have Rocks' chained .RaiseMyEvent() API.
		// Instead, use a getter callback that explicitly raises the event.
		// ====================================================================

		#region Standalone: Event raised during property get

		[Fact]
		public void EventOnGet_Standalone_CallbackRaisesEvent()
		{
			var stub = new InterfacePropertyStandaloneKnockOff();
			IInterfaceProperty service = stub;

			// Configure getter callback that raises the event
			stub.GetData.Get(() =>
			{
				stub.MyEvent.Raise(stub, EventArgs.Empty);
				return 42;
			});

			var wasEventRaised = false;
			service.MyEvent += (s, e) => wasEventRaised = true;

			int value = service.GetData;

			Assert.Equal(42, value);
			Assert.True(wasEventRaised);
		}

		#endregion

		#region Inline: Event raised during property get

		[Fact]
		public void EventOnGet_Inline_CallbackRaisesEvent()
		{
			var stub = new InterfacePropertyInlineTests.Stubs.IInterfaceProperty();
			IInterfaceProperty service = stub;

			stub.GetData.Get(() =>
			{
				stub.MyEvent.Raise(stub, EventArgs.Empty);
				return 42;
			});

			var wasEventRaised = false;
			service.MyEvent += (s, e) => wasEventRaised = true;

			int value = service.GetData;

			Assert.Equal(42, value);
			Assert.True(wasEventRaised);
		}

		#endregion

		// ====================================================================
		// Scenario 7: Event raising from property set
		// Use a setter callback that explicitly raises the event.
		// ====================================================================

		#region Standalone: Event raised during property set

		[Fact]
		public void EventOnSet_Standalone_CallbackRaisesEvent()
		{
			var stub = new InterfacePropertyStandaloneKnockOff();
			IInterfaceProperty service = stub;

			// Configure setter callback that raises the event
			stub.SetData.Set(value =>
			{
				stub.MyEvent.Raise(stub, EventArgs.Empty);
			});

			var wasEventRaised = false;
			service.MyEvent += (s, e) => wasEventRaised = true;

			service.SetData = 1;

			Assert.True(wasEventRaised);
		}

		#endregion

		#region Inline: Event raised during property set

		[Fact]
		public void EventOnSet_Inline_CallbackRaisesEvent()
		{
			var stub = new InterfacePropertyInlineTests.Stubs.IInterfaceProperty();
			IInterfaceProperty service = stub;

			stub.SetData.Set(value =>
			{
				stub.MyEvent.Raise(stub, EventArgs.Empty);
			});

			var wasEventRaised = false;
			service.MyEvent += (s, e) => wasEventRaised = true;

			service.SetData = 1;

			Assert.True(wasEventRaised);
		}

		#endregion

		// ====================================================================
		// Scenario 8: Callback on property get — computed value
		// ====================================================================

		#region Standalone: Callback on get

		[Fact]
		public void CallbackOnGet_Standalone_ReturnsComputedValue()
		{
			var stub = new InterfacePropertyStandaloneKnockOff();
			IInterfaceProperty service = stub;

			var accessCount = 0;
			stub.GetData.Get(() =>
			{
				accessCount++;
				return accessCount * 10;
			});

			int first = service.GetData;
			int second = service.GetData;

			Assert.Equal(10, first);
			Assert.Equal(20, second);
		}

		#endregion

		#region Inline: Callback on get

		[Fact]
		public void CallbackOnGet_Inline_ReturnsComputedValue()
		{
			var stub = new InterfacePropertyInlineTests.Stubs.IInterfaceProperty();
			IInterfaceProperty service = stub;

			var accessCount = 0;
			stub.GetData.Get(() =>
			{
				accessCount++;
				return accessCount * 10;
			});

			int first = service.GetData;
			int second = service.GetData;

			Assert.Equal(10, first);
			Assert.Equal(20, second);
		}

		#endregion

		// ====================================================================
		// Scenario 9: Callback on property set — captures set value
		// ====================================================================

		#region Standalone: Callback on set

		[Fact]
		public void CallbackOnSet_Standalone_CapturesSetValue()
		{
			var stub = new InterfacePropertyStandaloneKnockOff();
			IInterfaceProperty service = stub;

			int? capturedValue = null;
			stub.SetData.Set(v => capturedValue = v);

			service.SetData = 123;

			Assert.Equal(123, capturedValue);
		}

		#endregion

		#region Inline: Callback on set

		[Fact]
		public void CallbackOnSet_Inline_CapturesSetValue()
		{
			var stub = new InterfacePropertyInlineTests.Stubs.IInterfaceProperty();
			IInterfaceProperty service = stub;

			int? capturedValue = null;
			stub.SetData.Set(v => capturedValue = v);

			service.SetData = 123;

			Assert.Equal(123, capturedValue);
		}

		#endregion

		// ====================================================================
		// Scenario 10: Combined callback + event raising
		// Getter callback computes a value AND raises an event.
		// ====================================================================

		#region Standalone: Combined callback + event

		[Fact]
		public void CombinedCallbackAndEvent_Standalone_BothFire()
		{
			var stub = new InterfacePropertyStandaloneKnockOff();
			IInterfaceProperty service = stub;

			var callbackInvoked = false;
			stub.GetData.Get(() =>
			{
				callbackInvoked = true;
				stub.MyEvent.Raise(stub, EventArgs.Empty);
				return 99;
			});

			var wasEventRaised = false;
			service.MyEvent += (s, e) => wasEventRaised = true;

			int value = service.GetData;

			Assert.Equal(99, value);
			Assert.True(callbackInvoked);
			Assert.True(wasEventRaised);
		}

		[Fact]
		public void CombinedCallbackAndEventOnSet_Standalone_BothFire()
		{
			var stub = new InterfacePropertyStandaloneKnockOff();
			IInterfaceProperty service = stub;

			int capturedValue = 0;
			stub.SetData.Set(v =>
			{
				capturedValue = v;
				stub.MyEvent.Raise(stub, EventArgs.Empty);
			});

			var wasEventRaised = false;
			service.MyEvent += (s, e) => wasEventRaised = true;

			service.SetData = 77;

			Assert.Equal(77, capturedValue);
			Assert.True(wasEventRaised);
		}

		#endregion

		#region Inline: Combined callback + event

		[Fact]
		public void CombinedCallbackAndEvent_Inline_BothFire()
		{
			var stub = new InterfacePropertyInlineTests.Stubs.IInterfaceProperty();
			IInterfaceProperty service = stub;

			var callbackInvoked = false;
			stub.GetData.Get(() =>
			{
				callbackInvoked = true;
				stub.MyEvent.Raise(stub, EventArgs.Empty);
				return 99;
			});

			var wasEventRaised = false;
			service.MyEvent += (s, e) => wasEventRaised = true;

			int value = service.GetData;

			Assert.Equal(99, value);
			Assert.True(callbackInvoked);
			Assert.True(wasEventRaised);
		}

		[Fact]
		public void CombinedCallbackAndEventOnSet_Inline_BothFire()
		{
			var stub = new InterfacePropertyInlineTests.Stubs.IInterfaceProperty();
			IInterfaceProperty service = stub;

			int capturedValue = 0;
			stub.SetData.Set(v =>
			{
				capturedValue = v;
				stub.MyEvent.Raise(stub, EventArgs.Empty);
			});

			var wasEventRaised = false;
			service.MyEvent += (s, e) => wasEventRaised = true;

			service.SetData = 77;

			Assert.Equal(77, capturedValue);
			Assert.True(wasEventRaised);
		}

		#endregion
	}
}
