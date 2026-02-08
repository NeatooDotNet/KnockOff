// -----------------------------------------------------------------------------
// Design.Tests - User Property Basics Tests
// -----------------------------------------------------------------------------
// Tests for user-defined properties (protected override properties with _ suffix)
// in standalone stubs. These tests cover:
// - Get-only, set-only, and get/set property overrides
// - Get/Set superseding user overrides
// - Tracking via VerifyGet/VerifySet
// - LastSetValue capture through user override
// - Strict mode bypass for overridden properties
// - Reset behavior preserving Get/Set configuration
// - Mixed scenarios (some properties overridden, some not)
// - All four standalone patterns
// -----------------------------------------------------------------------------

using Design.Domain.Abstractions;
using Design.Domain.Services;
using Design.Stubs.UserProperties;
using KnockOff;

namespace Design.Tests.UserPropertyTests;

/// <summary>
/// Tests for user property overrides in standalone stubs.
/// User properties are protected override properties with underscore suffix
/// that provide default behavior, superseded by Get/Set per-test.
/// </summary>
public class UserPropertyBasicsTests
{
    // =========================================================================
    // Test 1: Get-only property override is called when no Get configured
    // =========================================================================

    [Fact]
    public void GetOnlyProperty_Override_IsCalledWhenNoGetConfigured()
    {
        var stub = new BasicUserPropertyStub();
        stub.SetCount(42);

        IUserPropertyService service = stub;

        Assert.Equal(42, service.Count);
    }

    // =========================================================================
    // Test 2: Get/set property override is called (both accessors)
    // =========================================================================

    [Fact]
    public void GetSetProperty_Override_GetterIsCalled()
    {
        var stub = new BasicUserPropertyStub();

        IUserPropertyService service = stub;
        service.Name = "Test Value";

        Assert.Equal("Test Value", service.Name);
    }

    [Fact]
    public void GetSetProperty_Override_SetterIsCalled()
    {
        var stub = new BasicUserPropertyStub();

        IUserPropertyService service = stub;
        service.Name = "Set Value";

        // The user override stores the value in a backing field
        Assert.Equal("Set Value", service.Name);
    }

    // =========================================================================
    // Test 3: Set-only property override is called when no Set configured
    // =========================================================================

    [Fact]
    public void SetOnlyProperty_Override_IsCalledWhenNoSetConfigured()
    {
        var stub = new BasicUserPropertyStub();

        IUserPropertyService service = stub;
        service.Setting = "test-setting";

        // Verify via the public accessor method
        Assert.Equal("test-setting", stub.GetSettingValue());
    }

    // =========================================================================
    // Test 4: Get supersedes property override per-test
    // =========================================================================

    [Fact]
    public void Get_SupersedesPropertyOverride()
    {
        var stub = new BasicUserPropertyStub();
        stub.SetCount(42);

        // Get supersedes the user property
        stub.Count.Get(999);

        IUserPropertyService service = stub;

        Assert.Equal(999, service.Count);
    }

    [Fact]
    public void Get_WithCallback_SupersedesPropertyOverride()
    {
        var stub = new BasicUserPropertyStub();
        stub.SetCount(42);

        var counter = 0;
        stub.Count.Get(() => ++counter);

        IUserPropertyService service = stub;

        Assert.Equal(1, service.Count);
        Assert.Equal(2, service.Count);
        Assert.Equal(3, service.Count);
    }

    // =========================================================================
    // Test 5: Set supersedes property override per-test
    // =========================================================================

    [Fact]
    public void Set_SupersedesPropertyOverride()
    {
        var stub = new BasicUserPropertyStub();
        var capturedValue = "";

        stub.Name.Set(v => capturedValue = $"Captured: {v}");

        IUserPropertyService service = stub;
        service.Name = "Test";

        // Set was invoked, NOT the user override
        Assert.Equal("Captured: Test", capturedValue);
        // The user override's backing field was NOT updated
        // because Set bypassed it
    }

    // =========================================================================
    // Test 6: VerifyGet/VerifySet track calls through user override
    // =========================================================================

    [Fact]
    public void VerifyGet_TracksCallsThroughUserOverride()
    {
        var stub = new BasicUserPropertyStub();
        stub.SetCount(100);

        IUserPropertyService service = stub;

        _ = service.Count;
        _ = service.Count;
        _ = service.Count;

        stub.Count.VerifyGet(Called.Exactly(3));
        stub.Count.VerifyGet(Called.AtLeast(2));
    }

    [Fact]
    public void VerifySet_TracksCallsThroughUserOverride()
    {
        var stub = new BasicUserPropertyStub();

        IUserPropertyService service = stub;

        service.Name = "First";
        service.Name = "Second";

        stub.Name.VerifySet(Called.Exactly(2));
    }

    // =========================================================================
    // Test 7: LastSetValue captured through user override
    // =========================================================================

    [Fact]
    public void LastSetValue_CapturedThroughUserOverride()
    {
        var stub = new BasicUserPropertyStub();

        IUserPropertyService service = stub;

        service.Name = "First Value";
        service.Name = "Last Value";

        Assert.Equal("Last Value", stub.Name.LastSetValue);
    }

    // =========================================================================
    // Test 8: Strict mode bypassed for overridden properties
    // =========================================================================

    [Fact]
    public void StrictMode_UserPropertyOverrideBypassed()
    {
        var stub = new StrictUserPropertyStub();

        IUserPropertyService service = stub;

        // User overrides work in strict mode - they ARE the configuration
        var count = service.Count;  // 10 (from override)
        var name = service.Name;    // "Strict Default"
        var desc = service.Description; // "Strict description"

        Assert.Equal(10, count);
        Assert.Equal("Strict Default", name);
        Assert.Equal("Strict description", desc);
    }

    [Fact]
    public void StrictMode_SetOnlyUserPropertyOverrideBypassed()
    {
        var stub = new StrictUserPropertyStub();

        IUserPropertyService service = stub;

        // Set-only user override also works in strict mode
        service.Setting = "value";

        stub.Setting.VerifySet(Called.Once);
    }

    // =========================================================================
    // Test 9: Reset preserves Get/Set configuration
    // =========================================================================

    [Fact]
    public void Reset_PreservesGetConfiguration()
    {
        var stub = new BasicUserPropertyStub();
        stub.Count.Get(100);

        IUserPropertyService service = stub;
        _ = service.Count;
        stub.Count.VerifyGet(Called.Once);

        // Reset clears tracking but preserves Get
        stub.Count.Reset();
        stub.Count.VerifyGet(Called.Never);

        var value = service.Count;
        Assert.Equal(100, value); // Get still active
    }

    [Fact]
    public void Reset_PreservesSetConfiguration()
    {
        var stub = new BasicUserPropertyStub();
        var callCount = 0;
        stub.Name.Set(v => callCount++);

        IUserPropertyService service = stub;
        service.Name = "First";
        Assert.Equal(1, callCount);

        // Reset clears tracking but preserves Set
        stub.Name.Reset();
        stub.Name.VerifySet(Called.Never);

        service.Name = "Second";
        Assert.Equal(2, callCount); // Set still active
    }

    [Fact]
    public void Reset_UserOverrideStillActiveAfterReset()
    {
        var stub = new BasicUserPropertyStub();
        stub.SetCount(50);

        IUserPropertyService service = stub;

        _ = service.Count;
        stub.Count.VerifyGet(Called.Once);

        // Reset clears tracking
        stub.Count.Reset();
        stub.Count.VerifyGet(Called.Never);

        // User override still works
        var count = service.Count;
        Assert.Equal(50, count);
    }

    // =========================================================================
    // Test 10: Mixed scenario (some properties overridden, some not)
    // =========================================================================

    [Fact]
    public void MixedScenario_OverriddenAndNonOverriddenProperties()
    {
        var stub = new MixedUserPropertyStub();

        // Properties WITH user override
        stub.WithUserProperty.VerifyGet(Called.Never);

        // Properties WITHOUT user override - configure via Get
        stub.WithoutUserProperty.Get(42);
        stub.ComputedWithoutUserProperty.Get("Configured value");

        IMixedUserPropertyService service = stub;

        var r1 = service.WithUserProperty;       // 100 (from override)
        var r2 = service.WithoutUserProperty;    // 42 (from Get)
        var r3 = service.ComputedWithUserProperty;  // "Computed: 100" (from override)
        var r4 = service.ComputedWithoutUserProperty; // "Configured value" (from Get)

        Assert.Equal(100, r1);
        Assert.Equal(42, r2);
        Assert.Equal("Computed: 100", r3);
        Assert.Equal("Configured value", r4);

        stub.WithUserProperty.VerifyGet(Called.Once);
        stub.WithoutUserProperty.VerifyGet(Called.Once);
    }

    // =========================================================================
    // Test 11: All four standalone patterns work correctly
    // =========================================================================

    [Fact]
    public void Pattern1_Standalone_UserPropertiesWork()
    {
        var stub = new BasicUserPropertyStub();
        stub.SetCount(42);

        IUserPropertyService service = stub;

        Assert.Equal(42, service.Count);
        stub.Count.VerifyGet(Called.Once);
    }

    [Fact]
    public void Pattern2_GenericStandalone_UserPropertiesWork()
    {
        var stub = new GenericUserPropertyStub<string>();
        stub.SetCurrentItem("Test Item");
        stub.SetItemCount(5);

        IGenericUserPropertyService<string> service = stub;

        Assert.Equal("Test Item", service.CurrentItem);
        Assert.Equal(5, service.ItemCount);

        service.DefaultItem = "New Default";
        Assert.Equal("New Default", service.DefaultItem);

        stub.CurrentItem.VerifyGet(Called.Once);
        stub.ItemCount.VerifyGet(Called.Once);
        stub.DefaultItem.VerifyGet(Called.Once);
        stub.DefaultItem.VerifySet(Called.Once);
    }

    [Fact]
    public void Pattern3_StandaloneClass_UserPropertiesWork()
    {
        var stub = new ConfigUserPropertyStub();
        stub.SetConfigName("Test Config");
        stub.SetDebugMode(true);

        // Use .Object to access the class instance
        ConfigBase config = stub.Object;

        Assert.Equal("Test Config", config.ConfigName);
        Assert.Equal(5, config.MaxRetries);  // Default from user override
        Assert.True(config.IsDebugMode);

        config.MaxRetries = 10;
        Assert.Equal(10, config.MaxRetries);

        config.LogPath = "/var/log/test.log";
        Assert.Equal("/var/log/test.log", stub.GetLogPath());

        stub.ConfigName.VerifyGet(Called.Once);
        stub.MaxRetries.VerifyGet(Called.Exactly(2));
        stub.MaxRetries.VerifySet(Called.Once);
        stub.IsDebugMode.VerifyGet(Called.Once);
        stub.LogPath.VerifySet(Called.Once);
    }

    [Fact]
    public void Pattern4_GenericStandaloneClass_UserPropertiesWork()
    {
        var stub = new CacheUserPropertyStub<string>();
        stub.SetDefaultValue("Default String");
        stub.SetCacheSize(100);
        stub.SetCacheName("String Cache");

        // Use .Object to access the class instance
        CacheBase<string> cache = stub.Object;

        Assert.Equal("Default String", cache.DefaultValue);
        Assert.Equal(100, cache.CacheSize);
        Assert.Equal("String Cache", cache.CacheName);

        cache.CurrentValue = "Current String";
        Assert.Equal("Current String", cache.CurrentValue);

        stub.DefaultValue.VerifyGet(Called.Once);
        stub.CacheSize.VerifyGet(Called.Once);
        stub.CacheName.VerifyGet(Called.Once);
        stub.CurrentValue.VerifyGet(Called.Once);
        stub.CurrentValue.VerifySet(Called.Once);
    }

    // =========================================================================
    // Additional edge case tests
    // =========================================================================

    [Fact]
    public void NullableProperty_Override_ReturnsNullWhenConditionNotMet()
    {
        var stub = new BasicUserPropertyStub();
        // Name is empty, so Description_ returns null

        IUserPropertyService service = stub;

        Assert.Null(service.Description);
    }

    [Fact]
    public void NullableProperty_Override_ReturnsValueWhenConditionMet()
    {
        var stub = new BasicUserPropertyStub();

        IUserPropertyService service = stub;
        service.Name = "TestName";

        Assert.Equal("Description for TestName", service.Description);
    }

    [Fact]
    public void Get_SupersedesOnStandaloneClass()
    {
        var stub = new ConfigUserPropertyStub();
        stub.SetConfigName("Default");

        // Get supersedes the user property
        stub.ConfigName.Get("Override Config");

        ConfigBase config = stub.Object;

        Assert.Equal("Override Config", config.ConfigName);
        stub.ConfigName.VerifyGet(Called.Once);
    }

    [Fact]
    public void Get_SupersedesOnGenericStandalone()
    {
        var stub = new GenericUserPropertyStub<string>();
        stub.SetCurrentItem("Default");

        // Get supersedes the user property
        stub.CurrentItem.Get("Override Value");

        IGenericUserPropertyService<string> service = stub;

        Assert.Equal("Override Value", service.CurrentItem);
    }

    [Fact]
    public void Get_SupersedesOnGenericStandaloneClass()
    {
        var stub = new CacheUserPropertyStub<string>();
        stub.SetDefaultValue("Default");

        // Get supersedes the user property
        stub.DefaultValue.Get("Override Default");

        CacheBase<string> cache = stub.Object;

        Assert.Equal("Override Default", cache.DefaultValue);
    }
}
