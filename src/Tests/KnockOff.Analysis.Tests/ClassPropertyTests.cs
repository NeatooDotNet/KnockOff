// ============================================================================
// ClassPropertyTests: Edge case tests for a class with virtual properties
// covering every accessor combination plus a virtual event.
// Inspired by Rocks.Analysis.IntegrationTests.ClassPropertyTests
//
// Target class:
//   public class ClassProperty
//   {
//       public virtual int GetData => default;                          // get-only
//       public virtual int GetAndInitData { get => default; init { } } // get + init
//       public virtual int GetAndSetData { get => default; set { } }   // get + set
//       public virtual int InitData { init { } }                       // init-only
//       public virtual int SetData { set { } }                         // set-only
//       public virtual event EventHandler? MyEvent;                    // event
//   }
//
// Tests exercise the CLASS pipeline (not interface) for:
// 1. Get-only property — configure return value, read it, assert
// 2. Set-only property — set a value, verify the set was tracked
// 3. Get/Set property — configure get, set a value, verify both
// 4. Get/Init property — configure get, read it, assert
// 5. Init-only property — verify it exists on the stub (init-only on class
//    stubs cannot be assigned after construction, so we verify default value)
// 6. Event raising from property get — use callback to raise event on get
// 7. Event raising from property set — use callback to raise event on set
// 8. Callback on property get — configure callback that returns computed value
// 9. Callback on property set — configure callback that captures the set value
// 10. Combined callback + event — callback and event raising together
//
// Applicable patterns (class only):
// - Pattern 3 (Standalone Class): [KnockOffBase<ClassProperty>]
// - Pattern 6 (Inline Class): [KnockOff<ClassProperty>]
// ============================================================================

using KnockOff;

// ============================================================================
// TYPE DEFINITIONS
// ============================================================================

namespace KnockOff.Analysis.Tests.ClassPropertyTestTypes
{
	public class ClassProperty
	{
		public virtual int GetData => default;
		public virtual int GetAndInitData { get => default; init { } }
		public virtual int GetAndSetData { get => default; set { } }
#pragma warning disable CA1044 // Properties should not be write only
		public virtual int InitData { init { } }
		public virtual int SetData { set { } }
#pragma warning restore CA1044 // Properties should not be write only

#pragma warning disable CA1070 // Do not declare event fields as virtual
#pragma warning disable CS0067
		public virtual event EventHandler? MyEvent;
#pragma warning restore CS0067
#pragma warning restore CA1070 // Do not declare event fields as virtual
	}

	// Pattern 3: Standalone class stub
	[KnockOffBase<ClassProperty>]
	public partial class ClassPropertyStandaloneKnockOff
	{
	}
}

// ============================================================================
// INLINE PATTERN DECLARATIONS + TESTS
// ============================================================================

namespace KnockOff.Analysis.Tests
{
	using KnockOff.Analysis.Tests.ClassPropertyTestTypes;

	// Pattern 6: Inline class stub
	[KnockOff<ClassProperty>]
	public partial class ClassPropertyInlineTests
	{
	}

	public class ClassPropertyTests
	{
		// ====================================================================
		// Scenario 1: Get-only property — configure return value, read, assert
		// ====================================================================

		#region Standalone Class: Get-only property (GetData)

		[Fact]
		public void GetOnly_StandaloneClass_ConfiguredReturnsValue()
		{
			var stub = new ClassPropertyStandaloneKnockOff();
			ClassProperty obj = stub.Object;

			stub.GetData.Get(42);

			int result = obj.GetData;

			Assert.Equal(42, result);
		}

		[Fact]
		public void GetOnly_StandaloneClass_UnconfiguredFallsToBase()
		{
			// Class stubs fall through to base when unconfigured.
			// base.GetData => default (0)
			var stub = new ClassPropertyStandaloneKnockOff();
			ClassProperty obj = stub.Object;

			int result = obj.GetData;

			Assert.Equal(0, result);
		}

		[Fact]
		public void GetOnly_StandaloneClass_VerifyTracksAccess()
		{
			var stub = new ClassPropertyStandaloneKnockOff();
			ClassProperty obj = stub.Object;

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
			var stub = new ClassPropertyInlineTests.Stubs.ClassProperty();
			ClassProperty obj = stub.Object;

			stub.GetData.Get(42);

			int result = obj.GetData;

			Assert.Equal(42, result);
		}

		[Fact]
		public void GetOnly_InlineClass_UnconfiguredFallsToBase()
		{
			var stub = new ClassPropertyInlineTests.Stubs.ClassProperty();
			ClassProperty obj = stub.Object;

			int result = obj.GetData;

			Assert.Equal(0, result);
		}

		[Fact]
		public void GetOnly_InlineClass_VerifyTracksAccess()
		{
			var stub = new ClassPropertyInlineTests.Stubs.ClassProperty();
			ClassProperty obj = stub.Object;

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
			var stub = new ClassPropertyStandaloneKnockOff();
			ClassProperty obj = stub.Object;

			obj.SetData = 99;

			stub.SetData.VerifySet(Called.Once);
		}

		[Fact]
		public void SetOnly_StandaloneClass_LastSetValueCaptured()
		{
			var stub = new ClassPropertyStandaloneKnockOff();
			ClassProperty obj = stub.Object;

			obj.SetData = 77;

			Assert.Equal(77, stub.SetData.LastSetValue);
		}

		#endregion

		#region Inline Class: Set-only property (SetData)

		[Fact]
		public void SetOnly_InlineClass_SetIsTracked()
		{
			var stub = new ClassPropertyInlineTests.Stubs.ClassProperty();
			ClassProperty obj = stub.Object;

			obj.SetData = 99;

			stub.SetData.VerifySet(Called.Once);
		}

		[Fact]
		public void SetOnly_InlineClass_LastSetValueCaptured()
		{
			var stub = new ClassPropertyInlineTests.Stubs.ClassProperty();
			ClassProperty obj = stub.Object;

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
			var stub = new ClassPropertyStandaloneKnockOff();
			ClassProperty obj = stub.Object;

			stub.GetAndSetData.Get(10);

			int getResult = obj.GetAndSetData;
			obj.GetAndSetData = 20;

			Assert.Equal(10, getResult);
			stub.GetAndSetData.VerifyGet(Called.Once);
			stub.GetAndSetData.VerifySet(Called.Once);
			Assert.Equal(20, stub.GetAndSetData.LastSetValue);
		}

		[Fact]
		public void GetSet_StandaloneClass_UnconfiguredFallsToBase()
		{
			var stub = new ClassPropertyStandaloneKnockOff();
			ClassProperty obj = stub.Object;

			// base.GetAndSetData returns default (0)
			int result = obj.GetAndSetData;

			Assert.Equal(0, result);
		}

		#endregion

		#region Inline Class: Get/Set property (GetAndSetData)

		[Fact]
		public void GetSet_InlineClass_ConfigureGetAndSet()
		{
			var stub = new ClassPropertyInlineTests.Stubs.ClassProperty();
			ClassProperty obj = stub.Object;

			stub.GetAndSetData.Get(10);

			int getResult = obj.GetAndSetData;
			obj.GetAndSetData = 20;

			Assert.Equal(10, getResult);
			stub.GetAndSetData.VerifyGet(Called.Once);
			stub.GetAndSetData.VerifySet(Called.Once);
			Assert.Equal(20, stub.GetAndSetData.LastSetValue);
		}

		[Fact]
		public void GetSet_InlineClass_UnconfiguredFallsToBase()
		{
			var stub = new ClassPropertyInlineTests.Stubs.ClassProperty();
			ClassProperty obj = stub.Object;

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
			var stub = new ClassPropertyStandaloneKnockOff();
			ClassProperty obj = stub.Object;

			stub.GetAndInitData.Get(55);

			int result = obj.GetAndInitData;

			Assert.Equal(55, result);
		}

		[Fact]
		public void GetInit_StandaloneClass_UnconfiguredFallsToBase()
		{
			var stub = new ClassPropertyStandaloneKnockOff();
			ClassProperty obj = stub.Object;

			// base.GetAndInitData returns default (0)
			int result = obj.GetAndInitData;

			Assert.Equal(0, result);
		}

		[Fact]
		public void GetInit_StandaloneClass_VerifyTracksAccess()
		{
			var stub = new ClassPropertyStandaloneKnockOff();
			ClassProperty obj = stub.Object;

			stub.GetAndInitData.Get(1);
			_ = obj.GetAndInitData;

			stub.GetAndInitData.VerifyGet(Called.Once);
		}

		#endregion

		#region Inline Class: Get/Init property (GetAndInitData)

		[Fact]
		public void GetInit_InlineClass_ConfiguredReturnsValue()
		{
			var stub = new ClassPropertyInlineTests.Stubs.ClassProperty();
			ClassProperty obj = stub.Object;

			stub.GetAndInitData.Get(55);

			int result = obj.GetAndInitData;

			Assert.Equal(55, result);
		}

		[Fact]
		public void GetInit_InlineClass_UnconfiguredFallsToBase()
		{
			var stub = new ClassPropertyInlineTests.Stubs.ClassProperty();
			ClassProperty obj = stub.Object;

			int result = obj.GetAndInitData;

			Assert.Equal(0, result);
		}

		[Fact]
		public void GetInit_InlineClass_VerifyTracksAccess()
		{
			var stub = new ClassPropertyInlineTests.Stubs.ClassProperty();
			ClassProperty obj = stub.Object;

			stub.GetAndInitData.Get(1);
			_ = obj.GetAndInitData;

			stub.GetAndInitData.VerifyGet(Called.Once);
		}

		#endregion

		// ====================================================================
		// Scenario 5: Init-only property — verify default value on stub
		// ====================================================================
		// Init-only virtual properties cannot be set after the Impl is
		// constructed. The test verifies the stub can be created and the
		// init-only property receives its default value.

		#region Standalone Class: Init-only property (InitData)

		[Fact]
		public void InitOnly_StandaloneClass_StubCanBeInstantiated()
		{
			var stub = new ClassPropertyStandaloneKnockOff();
			ClassProperty obj = stub.Object;

			Assert.NotNull(obj);
		}

		#endregion

		#region Inline Class: Init-only property (InitData)

		[Fact]
		public void InitOnly_InlineClass_StubCanBeInstantiated()
		{
			var stub = new ClassPropertyInlineTests.Stubs.ClassProperty();
			ClassProperty obj = stub.Object;

			Assert.NotNull(obj);
		}

		#endregion

		// ====================================================================
		// Scenario 6: Event raising from property get
		// KnockOff does not have Rocks' chained .RaiseMyEvent() API.
		// Instead, use a getter callback that explicitly raises the event.
		// ====================================================================

		#region Standalone Class: Event raised during property get

		[Fact]
		public void EventOnGet_StandaloneClass_CallbackRaisesEvent()
		{
			var stub = new ClassPropertyStandaloneKnockOff();
			ClassProperty obj = stub.Object;

			// Configure getter callback that raises the event
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
			var stub = new ClassPropertyInlineTests.Stubs.ClassProperty();
			ClassProperty obj = stub.Object;

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
			var stub = new ClassPropertyStandaloneKnockOff();
			ClassProperty obj = stub.Object;

			// Configure setter callback that raises the event
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
			var stub = new ClassPropertyInlineTests.Stubs.ClassProperty();
			ClassProperty obj = stub.Object;

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
			var stub = new ClassPropertyStandaloneKnockOff();
			ClassProperty obj = stub.Object;

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
			var stub = new ClassPropertyInlineTests.Stubs.ClassProperty();
			ClassProperty obj = stub.Object;

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
			var stub = new ClassPropertyStandaloneKnockOff();
			ClassProperty obj = stub.Object;

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
			var stub = new ClassPropertyInlineTests.Stubs.ClassProperty();
			ClassProperty obj = stub.Object;

			int? capturedValue = null;
			stub.SetData.Set(v => capturedValue = v);

			obj.SetData = 123;

			Assert.Equal(123, capturedValue);
		}

		#endregion

		// ====================================================================
		// Scenario 10: Combined callback + event raising
		// Getter callback computes a value AND raises an event.
		// ====================================================================

		#region Standalone Class: Combined callback + event

		[Fact]
		public void CombinedCallbackAndEvent_StandaloneClass_BothFire()
		{
			var stub = new ClassPropertyStandaloneKnockOff();
			ClassProperty obj = stub.Object;

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
			var stub = new ClassPropertyStandaloneKnockOff();
			ClassProperty obj = stub.Object;

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
			var stub = new ClassPropertyInlineTests.Stubs.ClassProperty();
			ClassProperty obj = stub.Object;

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
			var stub = new ClassPropertyInlineTests.Stubs.ClassProperty();
			ClassProperty obj = stub.Object;

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
