// ============================================================================
// AbstractClassPropertyTests: Edge case tests for an abstract class with
// abstract properties covering every accessor combination plus an abstract event.
// Inspired by Rocks.Analysis.IntegrationTests.AbstractClassPropertyTests
//
// Target class:
//   public abstract class AbstractClassProperty
//   {
//       public abstract int GetData { get; }
//       public abstract int GetAndInitData { get; init; }
//       public abstract int GetAndSetData { get; set; }
//       public abstract int InitData { init; }
//       public abstract int SetData { set; }
//       public abstract event EventHandler? MyEvent;
//   }
//
// Key difference from ClassPropertyTests: members are ABSTRACT, not virtual.
// Abstract members have no base implementation to fall through to, so unconfigured
// calls return default(int) = 0 directly (not via base.Property).
//
// Tests exercise the CLASS pipeline for:
// 1. Get-only property — configure return value, read it, assert
// 2. Set-only property — set a value, verify the set was tracked
// 3. Get/Set property — configure get, set a value, verify both
// 4. Get/Init property — configure get, read it, assert
// 5. Init-only property — verify stub can be instantiated
// 6. Event raising from property get — use callback to raise event on get
// 7. Event raising from property set — use callback to raise event on set
// 8. Callback on property get — configure callback that returns computed value
// 9. Callback on property set — configure callback that captures the set value
// 10. Combined callback + event — callback and event raising together
//
// Applicable patterns (class only):
// - Pattern 3 (Standalone Class): [KnockOffBase<AbstractClassProperty>]
// - Pattern 6 (Inline Class): [KnockOff<AbstractClassProperty>]
// ============================================================================

using KnockOff;

// ============================================================================
// TYPE DEFINITIONS
// ============================================================================

namespace KnockOff.Analysis.Tests.AbstractClassPropertyTestTypes
{
	public abstract class AbstractClassProperty
	{
		public abstract int GetData { get; }
		public abstract int GetAndInitData { get; init; }
		public abstract int GetAndSetData { get; set; }
#pragma warning disable CA1044 // Properties should not be write only
		public abstract int InitData { init; }
		public abstract int SetData { set; }
#pragma warning restore CA1044 // Properties should not be write only

		public abstract event EventHandler? MyEvent;
	}

	// Pattern 3: Standalone class stub
	[KnockOffBase<AbstractClassProperty>]
	public partial class AbstractClassPropertyStandaloneKnockOff
	{
	}
}

// ============================================================================
// INLINE PATTERN DECLARATIONS + TESTS
// ============================================================================

namespace KnockOff.Analysis.Tests
{
	using KnockOff.Analysis.Tests.AbstractClassPropertyTestTypes;

	// Pattern 6: Inline class stub
	[KnockOff<AbstractClassProperty>]
	public partial class AbstractClassPropertyInlineTests
	{
	}

	public class AbstractClassPropertyTests
	{
		// ====================================================================
		// Scenario 1: Get-only property — configure return value, read, assert
		// ====================================================================

		#region Standalone Class: Get-only property (GetData)

		[Fact]
		public void GetOnly_StandaloneClass_ConfiguredReturnsValue()
		{
			var stub = new AbstractClassPropertyStandaloneKnockOff();
			AbstractClassProperty obj = stub.Object;

			stub.GetData.Get(42);

			int result = obj.GetData;

			Assert.Equal(42, result);
		}

		[Fact]
		public void GetOnly_StandaloneClass_UnconfiguredReturnsDefault()
		{
			// Abstract members return default when unconfigured (no base to fall through to)
			var stub = new AbstractClassPropertyStandaloneKnockOff();
			AbstractClassProperty obj = stub.Object;

			int result = obj.GetData;

			Assert.Equal(0, result);
		}

		[Fact]
		public void GetOnly_StandaloneClass_VerifyTracksAccess()
		{
			var stub = new AbstractClassPropertyStandaloneKnockOff();
			AbstractClassProperty obj = stub.Object;

			stub.GetData.Get(10);
			_ = obj.GetData;
			_ = obj.GetData;

			stub.GetData.VerifyGet(Called.Exactly(2));
		}

		#endregion

		#region Inline Class: Get-only property (GetData)

		[Fact]
		public void GetOnly_InlineClass_ConfiguredReturnsValue()
		{
			var stub = new AbstractClassPropertyInlineTests.Stubs.AbstractClassProperty();
			AbstractClassProperty obj = stub.Object;

			stub.GetData.Get(42);

			int result = obj.GetData;

			Assert.Equal(42, result);
		}

		[Fact]
		public void GetOnly_InlineClass_UnconfiguredReturnsDefault()
		{
			var stub = new AbstractClassPropertyInlineTests.Stubs.AbstractClassProperty();
			AbstractClassProperty obj = stub.Object;

			int result = obj.GetData;

			Assert.Equal(0, result);
		}

		[Fact]
		public void GetOnly_InlineClass_VerifyTracksAccess()
		{
			var stub = new AbstractClassPropertyInlineTests.Stubs.AbstractClassProperty();
			AbstractClassProperty obj = stub.Object;

			stub.GetData.Get(10);
			_ = obj.GetData;
			_ = obj.GetData;

			stub.GetData.VerifyGet(Called.Exactly(2));
		}

		#endregion

		// ====================================================================
		// Scenario 2: Set-only property — set a value, verify set tracked
		// ====================================================================

		#region Standalone Class: Set-only property (SetData)

		[Fact]
		public void SetOnly_StandaloneClass_SetIsTracked()
		{
			var stub = new AbstractClassPropertyStandaloneKnockOff();
			AbstractClassProperty obj = stub.Object;

			obj.SetData = 99;

			stub.SetData.VerifySet(Called.Once);
		}

		[Fact]
		public void SetOnly_StandaloneClass_LastSetValueCaptured()
		{
			var stub = new AbstractClassPropertyStandaloneKnockOff();
			AbstractClassProperty obj = stub.Object;

			obj.SetData = 77;

			Assert.Equal(77, stub.SetData.LastSetValue);
		}

		#endregion

		#region Inline Class: Set-only property (SetData)

		[Fact]
		public void SetOnly_InlineClass_SetIsTracked()
		{
			var stub = new AbstractClassPropertyInlineTests.Stubs.AbstractClassProperty();
			AbstractClassProperty obj = stub.Object;

			obj.SetData = 99;

			stub.SetData.VerifySet(Called.Once);
		}

		[Fact]
		public void SetOnly_InlineClass_LastSetValueCaptured()
		{
			var stub = new AbstractClassPropertyInlineTests.Stubs.AbstractClassProperty();
			AbstractClassProperty obj = stub.Object;

			obj.SetData = 77;

			Assert.Equal(77, stub.SetData.LastSetValue);
		}

		#endregion

		// ====================================================================
		// Scenario 3: Get/Set property — configure get, set value, verify both
		// ====================================================================

		#region Standalone Class: Get/Set property (GetAndSetData)

		[Fact]
		public void GetSet_StandaloneClass_ConfigureGetAndSet()
		{
			var stub = new AbstractClassPropertyStandaloneKnockOff();
			AbstractClassProperty obj = stub.Object;

			stub.GetAndSetData.Get(10);

			int getResult = obj.GetAndSetData;
			obj.GetAndSetData = 20;

			Assert.Equal(10, getResult);
			stub.GetAndSetData.VerifyGet(Called.Once);
			stub.GetAndSetData.VerifySet(Called.Once);
			Assert.Equal(20, stub.GetAndSetData.LastSetValue);
		}

		[Fact]
		public void GetSet_StandaloneClass_UnconfiguredReturnsDefault()
		{
			var stub = new AbstractClassPropertyStandaloneKnockOff();
			AbstractClassProperty obj = stub.Object;

			// Abstract property returns default (0) when unconfigured
			int result = obj.GetAndSetData;

			Assert.Equal(0, result);
		}

		#endregion

		#region Inline Class: Get/Set property (GetAndSetData)

		[Fact]
		public void GetSet_InlineClass_ConfigureGetAndSet()
		{
			var stub = new AbstractClassPropertyInlineTests.Stubs.AbstractClassProperty();
			AbstractClassProperty obj = stub.Object;

			stub.GetAndSetData.Get(10);

			int getResult = obj.GetAndSetData;
			obj.GetAndSetData = 20;

			Assert.Equal(10, getResult);
			stub.GetAndSetData.VerifyGet(Called.Once);
			stub.GetAndSetData.VerifySet(Called.Once);
			Assert.Equal(20, stub.GetAndSetData.LastSetValue);
		}

		[Fact]
		public void GetSet_InlineClass_UnconfiguredReturnsDefault()
		{
			var stub = new AbstractClassPropertyInlineTests.Stubs.AbstractClassProperty();
			AbstractClassProperty obj = stub.Object;

			int result = obj.GetAndSetData;

			Assert.Equal(0, result);
		}

		#endregion

		// ====================================================================
		// Scenario 4: Get/Init property — configure get, read it, assert
		// ====================================================================

		#region Standalone Class: Get/Init property (GetAndInitData)

		[Fact]
		public void GetInit_StandaloneClass_ConfiguredReturnsValue()
		{
			var stub = new AbstractClassPropertyStandaloneKnockOff();
			AbstractClassProperty obj = stub.Object;

			stub.GetAndInitData.Get(55);

			int result = obj.GetAndInitData;

			Assert.Equal(55, result);
		}

		[Fact]
		public void GetInit_StandaloneClass_UnconfiguredReturnsDefault()
		{
			var stub = new AbstractClassPropertyStandaloneKnockOff();
			AbstractClassProperty obj = stub.Object;

			int result = obj.GetAndInitData;

			Assert.Equal(0, result);
		}

		[Fact]
		public void GetInit_StandaloneClass_VerifyTracksAccess()
		{
			var stub = new AbstractClassPropertyStandaloneKnockOff();
			AbstractClassProperty obj = stub.Object;

			stub.GetAndInitData.Get(1);
			_ = obj.GetAndInitData;

			stub.GetAndInitData.VerifyGet(Called.Once);
		}

		#endregion

		#region Inline Class: Get/Init property (GetAndInitData)

		[Fact]
		public void GetInit_InlineClass_ConfiguredReturnsValue()
		{
			var stub = new AbstractClassPropertyInlineTests.Stubs.AbstractClassProperty();
			AbstractClassProperty obj = stub.Object;

			stub.GetAndInitData.Get(55);

			int result = obj.GetAndInitData;

			Assert.Equal(55, result);
		}

		[Fact]
		public void GetInit_InlineClass_UnconfiguredReturnsDefault()
		{
			var stub = new AbstractClassPropertyInlineTests.Stubs.AbstractClassProperty();
			AbstractClassProperty obj = stub.Object;

			int result = obj.GetAndInitData;

			Assert.Equal(0, result);
		}

		[Fact]
		public void GetInit_InlineClass_VerifyTracksAccess()
		{
			var stub = new AbstractClassPropertyInlineTests.Stubs.AbstractClassProperty();
			AbstractClassProperty obj = stub.Object;

			stub.GetAndInitData.Get(1);
			_ = obj.GetAndInitData;

			stub.GetAndInitData.VerifyGet(Called.Once);
		}

		#endregion

		// ====================================================================
		// Scenario 5: Init-only property — verify stub can be instantiated
		// ====================================================================

		#region Standalone Class: Init-only property (InitData)

		[Fact]
		public void InitOnly_StandaloneClass_StubCanBeInstantiated()
		{
			var stub = new AbstractClassPropertyStandaloneKnockOff();
			AbstractClassProperty obj = stub.Object;

			Assert.NotNull(obj);
		}

		#endregion

		#region Inline Class: Init-only property (InitData)

		[Fact]
		public void InitOnly_InlineClass_StubCanBeInstantiated()
		{
			var stub = new AbstractClassPropertyInlineTests.Stubs.AbstractClassProperty();
			AbstractClassProperty obj = stub.Object;

			Assert.NotNull(obj);
		}

		#endregion

		// ====================================================================
		// Scenario 6: Event raising from property get
		// Use a getter callback that explicitly raises the event.
		// ====================================================================

		#region Standalone Class: Event raised during property get

		[Fact]
		public void EventOnGet_StandaloneClass_CallbackRaisesEvent()
		{
			var stub = new AbstractClassPropertyStandaloneKnockOff();
			AbstractClassProperty obj = stub.Object;

			stub.GetData.Get(() =>
			{
				stub.MyEvent.Raise(obj, EventArgs.Empty);
				return 42;
			});

			var wasEventRaised = false;
			obj.MyEvent += (s, e) => wasEventRaised = true;

			int value = obj.GetData;

			Assert.Equal(42, value);
			Assert.True(wasEventRaised);
		}

		#endregion

		#region Inline Class: Event raised during property get

		[Fact]
		public void EventOnGet_InlineClass_CallbackRaisesEvent()
		{
			var stub = new AbstractClassPropertyInlineTests.Stubs.AbstractClassProperty();
			AbstractClassProperty obj = stub.Object;

			stub.GetData.Get(() =>
			{
				stub.MyEvent.Raise(obj, EventArgs.Empty);
				return 42;
			});

			var wasEventRaised = false;
			obj.MyEvent += (s, e) => wasEventRaised = true;

			int value = obj.GetData;

			Assert.Equal(42, value);
			Assert.True(wasEventRaised);
		}

		#endregion

		// ====================================================================
		// Scenario 7: Event raising from property set
		// Use a setter callback that explicitly raises the event.
		// ====================================================================

		#region Standalone Class: Event raised during property set

		[Fact]
		public void EventOnSet_StandaloneClass_CallbackRaisesEvent()
		{
			var stub = new AbstractClassPropertyStandaloneKnockOff();
			AbstractClassProperty obj = stub.Object;

			stub.SetData.Set(value =>
			{
				stub.MyEvent.Raise(obj, EventArgs.Empty);
			});

			var wasEventRaised = false;
			obj.MyEvent += (s, e) => wasEventRaised = true;

			obj.SetData = 1;

			Assert.True(wasEventRaised);
		}

		#endregion

		#region Inline Class: Event raised during property set

		[Fact]
		public void EventOnSet_InlineClass_CallbackRaisesEvent()
		{
			var stub = new AbstractClassPropertyInlineTests.Stubs.AbstractClassProperty();
			AbstractClassProperty obj = stub.Object;

			stub.SetData.Set(value =>
			{
				stub.MyEvent.Raise(obj, EventArgs.Empty);
			});

			var wasEventRaised = false;
			obj.MyEvent += (s, e) => wasEventRaised = true;

			obj.SetData = 1;

			Assert.True(wasEventRaised);
		}

		#endregion

		// ====================================================================
		// Scenario 8: Callback on property get — computed value
		// ====================================================================

		#region Standalone Class: Callback on get

		[Fact]
		public void CallbackOnGet_StandaloneClass_ReturnsComputedValue()
		{
			var stub = new AbstractClassPropertyStandaloneKnockOff();
			AbstractClassProperty obj = stub.Object;

			var accessCount = 0;
			stub.GetData.Get(() =>
			{
				accessCount++;
				return accessCount * 10;
			});

			int first = obj.GetData;
			int second = obj.GetData;

			Assert.Equal(10, first);
			Assert.Equal(20, second);
		}

		#endregion

		#region Inline Class: Callback on get

		[Fact]
		public void CallbackOnGet_InlineClass_ReturnsComputedValue()
		{
			var stub = new AbstractClassPropertyInlineTests.Stubs.AbstractClassProperty();
			AbstractClassProperty obj = stub.Object;

			var accessCount = 0;
			stub.GetData.Get(() =>
			{
				accessCount++;
				return accessCount * 10;
			});

			int first = obj.GetData;
			int second = obj.GetData;

			Assert.Equal(10, first);
			Assert.Equal(20, second);
		}

		#endregion

		// ====================================================================
		// Scenario 9: Callback on property set — captures set value
		// ====================================================================

		#region Standalone Class: Callback on set

		[Fact]
		public void CallbackOnSet_StandaloneClass_CapturesSetValue()
		{
			var stub = new AbstractClassPropertyStandaloneKnockOff();
			AbstractClassProperty obj = stub.Object;

			int? capturedValue = null;
			stub.SetData.Set(v => capturedValue = v);

			obj.SetData = 123;

			Assert.Equal(123, capturedValue);
		}

		#endregion

		#region Inline Class: Callback on set

		[Fact]
		public void CallbackOnSet_InlineClass_CapturesSetValue()
		{
			var stub = new AbstractClassPropertyInlineTests.Stubs.AbstractClassProperty();
			AbstractClassProperty obj = stub.Object;

			int? capturedValue = null;
			stub.SetData.Set(v => capturedValue = v);

			obj.SetData = 123;

			Assert.Equal(123, capturedValue);
		}

		#endregion

		// ====================================================================
		// Scenario 10: Combined callback + event raising
		// ====================================================================

		#region Standalone Class: Combined callback + event

		[Fact]
		public void CombinedCallbackAndEvent_StandaloneClass_BothFire()
		{
			var stub = new AbstractClassPropertyStandaloneKnockOff();
			AbstractClassProperty obj = stub.Object;

			var callbackInvoked = false;
			stub.GetData.Get(() =>
			{
				callbackInvoked = true;
				stub.MyEvent.Raise(obj, EventArgs.Empty);
				return 99;
			});

			var wasEventRaised = false;
			obj.MyEvent += (s, e) => wasEventRaised = true;

			int value = obj.GetData;

			Assert.Equal(99, value);
			Assert.True(callbackInvoked);
			Assert.True(wasEventRaised);
		}

		[Fact]
		public void CombinedCallbackAndEventOnSet_StandaloneClass_BothFire()
		{
			var stub = new AbstractClassPropertyStandaloneKnockOff();
			AbstractClassProperty obj = stub.Object;

			int capturedValue = 0;
			stub.SetData.Set(v =>
			{
				capturedValue = v;
				stub.MyEvent.Raise(obj, EventArgs.Empty);
			});

			var wasEventRaised = false;
			obj.MyEvent += (s, e) => wasEventRaised = true;

			obj.SetData = 77;

			Assert.Equal(77, capturedValue);
			Assert.True(wasEventRaised);
		}

		#endregion

		#region Inline Class: Combined callback + event

		[Fact]
		public void CombinedCallbackAndEvent_InlineClass_BothFire()
		{
			var stub = new AbstractClassPropertyInlineTests.Stubs.AbstractClassProperty();
			AbstractClassProperty obj = stub.Object;

			var callbackInvoked = false;
			stub.GetData.Get(() =>
			{
				callbackInvoked = true;
				stub.MyEvent.Raise(obj, EventArgs.Empty);
				return 99;
			});

			var wasEventRaised = false;
			obj.MyEvent += (s, e) => wasEventRaised = true;

			int value = obj.GetData;

			Assert.Equal(99, value);
			Assert.True(callbackInvoked);
			Assert.True(wasEventRaised);
		}

		[Fact]
		public void CombinedCallbackAndEventOnSet_InlineClass_BothFire()
		{
			var stub = new AbstractClassPropertyInlineTests.Stubs.AbstractClassProperty();
			AbstractClassProperty obj = stub.Object;

			int capturedValue = 0;
			stub.SetData.Set(v =>
			{
				capturedValue = v;
				stub.MyEvent.Raise(obj, EventArgs.Empty);
			});

			var wasEventRaised = false;
			obj.MyEvent += (s, e) => wasEventRaised = true;

			obj.SetData = 77;

			Assert.Equal(77, capturedValue);
			Assert.True(wasEventRaised);
		}

		#endregion
	}
}
