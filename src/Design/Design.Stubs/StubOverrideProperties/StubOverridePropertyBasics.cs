// -----------------------------------------------------------------------------
// Design.Stubs - Stub Override Properties
// -----------------------------------------------------------------------------
// This file explores and documents user-defined property patterns:
// - Base class pattern (protected override properties with _ suffix)
// - Get-only, set-only, and get/set property overrides
// - Tracking API on interceptors (VerifyGet, VerifySet, LastSetValue)
// - Get/Set to supersede user properties
// - Mixed scenarios (some with user properties, some without)
// - Priority: Get/Set > stub override property
// - Strict mode behavior
// - All four applicable standalone patterns
// -----------------------------------------------------------------------------

using Design.Domain.Abstractions;
using Design.Domain.Services;
using KnockOff;

namespace Design.Stubs.StubOverrideProperties;

// =============================================================================
// STUB OVERRIDE PROPERTIES - OVERVIEW
// =============================================================================
//
// WHAT ARE STUB OVERRIDE PROPERTIES?
// User properties are protected override properties you define in a partial stub
// class to provide default behavior for interface properties. KnockOff generates
// a base class with virtual properties that you can override.
//
// WHY USE THEM?
// 1. Reusable defaults - Define once, use across all tests
// 2. Computed values - When you need logic to compute property values
// 3. State management - When you need instance state with backing fields
// 4. IDE support - Full IntelliSense, refactoring, debugging
// 5. Compile-time safety - Signature errors caught by compiler
//
// THE BASE CLASS PATTERN:
// KnockOff generates a base class (e.g., BasicStubOverridePropertyStubBase) with virtual
// protected properties suffixed with underscore (e.g., Count_). You override
// these properties to provide default behavior:
//
//   // Generated base class (BasicStubOverridePropertyStubBase):
//   protected virtual int Count_ => default!;
//
//   // Your override:
//   protected override int Count_ => _items.Count;
//
// INTERCEPTOR NAMING:
// Interceptor properties use clean names (stub.Count, not stub.Count2).
// The underscore suffix is only on the overridable property (Count_).
//
// =============================================================================

// =============================================================================
// PATTERN 1: STANDALONE INTERFACE STUB WITH STUB OVERRIDE PROPERTIES
// =============================================================================

[KnockOff]
public partial class BasicStubOverridePropertyStub : IStubOverridePropertyService { }

public partial class BasicStubOverridePropertyStub
{
    // =========================================================================
    // User Property Definition - Base Class Pattern
    // =========================================================================
    // PATTERN: Override the generated virtual property with underscore suffix:
    // - Add 'override' keyword
    // - Add '_' suffix to property name (e.g., Count_)
    // - Same return type
    //
    // COMPILER ENFORCES SIGNATURE:
    // If you typo the property name, the compiler catches it
    // (no suitable property to override). No more silent failures!
    //
    // GENERATED BASE CLASS (BasicStubOverridePropertyStubBase):
    // protected virtual int Count_ => default!;
    // protected virtual string Name_ { get => default!; set { } }
    // protected virtual string Setting_ { set { } }
    // protected virtual string? Description_ => default!;
    // =========================================================================

    // Backing fields for stateful properties
    private int _count;
    private string _name = "";
    private string _setting = "";

    // -------------------------------------------------------------------------
    // Get-only property override - expression-bodied
    // -------------------------------------------------------------------------
    protected override int Count_ => _count;

    // Public method to manipulate backing field (for testing)
    public void SetCount(int value) => _count = value;

    // -------------------------------------------------------------------------
    // Get/set property override with backing field
    // -------------------------------------------------------------------------
    protected override string Name_
    {
        get => _name;
        set => _name = value;
    }

    // -------------------------------------------------------------------------
    // Set-only property override
    // -------------------------------------------------------------------------
    protected override string Setting_
    {
        set => _setting = value;
    }

    // Public accessor to verify the set value (for testing)
#pragma warning disable CA1024 // Use properties where appropriate - intentional method for demo
    public string GetSettingValue() => _setting;
#pragma warning restore CA1024

    // -------------------------------------------------------------------------
    // Nullable get-only property override
    // -------------------------------------------------------------------------
    protected override string? Description_ => _name.Length > 0 ? $"Description for {_name}" : null;
}

[KnockOff<IStubOverridePropertyService>]
public partial class StubOverridePropertyBasicsDemo
{
    // =========================================================================
    // Using Stubs with User Properties
    // =========================================================================

    public void BasicUsage()
    {
        var stub = new BasicStubOverridePropertyStub();
        IStubOverridePropertyService service = stub;

        // Set up backing field via public method
        stub.SetCount(42);

        // Get-only property uses your protected override Count_
        var count = service.Count;
        // count == 42

        // Get/set property uses your protected override Name_
        service.Name = "Test Entity";
        var name = service.Name;
        // name == "Test Entity"

        // Set-only property uses your protected override Setting_
        service.Setting = "debug=true";
        var settingValue = stub.GetSettingValue();
        // settingValue == "debug=true"

        // Nullable property computes based on Name
        var desc = service.Description;
        // desc == "Description for Test Entity"
    }

    // =========================================================================
    // Tracking API - Clean Interceptor Names
    // =========================================================================
    // DESIGN DECISION: Interceptor properties use CLEAN names (no suffix).
    // With the base class pattern:
    // - Override property: Count_ (underscore suffix)
    // - Interceptor: stub.Count (clean name)
    //
    // The tracking interceptor provides:
    // - VerifyGet(Called) - verify getter call count
    // - VerifySet(Called) - verify setter call count
    // - LastSetValue - last value passed to setter
    // - Reset() - clear tracking state
    // - Get() - supersede stub override property getter per-test
    // - Set() - supersede stub override property setter per-test
    // =========================================================================

    public void TrackingWithVerifyGet()
    {
        var stub = new BasicStubOverridePropertyStub();
        stub.SetCount(100);

        IStubOverridePropertyService service = stub;

        _ = service.Count;
        _ = service.Count;
        _ = service.Count;

        // Verify getter was called three times
        stub.Count.VerifyGet(Called.Exactly(3));
        stub.Count.VerifyGet(Called.AtLeast(2));
    }

    public void TrackingWithVerifySet()
    {
        var stub = new BasicStubOverridePropertyStub();
        IStubOverridePropertyService service = stub;

        service.Name = "First";
        service.Name = "Second";

        // Verify setter was called twice
        stub.Name.VerifySet(Called.Exactly(2));
    }

    public void TrackingWithLastSetValue()
    {
        var stub = new BasicStubOverridePropertyStub();
        IStubOverridePropertyService service = stub;

        service.Name = "First Value";
        service.Name = "Last Value";

        // LastSetValue gives the most recent value
        var lastValue = stub.Name.LastSetValue;
        // lastValue == "Last Value"
    }

    public void TrackingWithReset()
    {
        var stub = new BasicStubOverridePropertyStub();
        stub.SetCount(50);
        IStubOverridePropertyService service = stub;

        _ = service.Count;
        stub.Count.VerifyGet(Called.Once);

        // Reset clears all tracking state
        stub.Count.Reset();

        stub.Count.VerifyGet(Called.Never);
        // But stub override property still works
        var count = service.Count;
        // count == 50 (stub override still active)
    }

    // =========================================================================
    // Get/Set Supersedes User Properties
    // =========================================================================
    // User properties provide shareable defaults. Get/Set override per test.
    //
    // RATIONALE: Stubs should be shareable yet configurable per test.
    // - Stub override property = sensible default for most tests
    // - Get/Set = override when a specific test needs different behavior
    //
    // GENERATED CODE PATTERN:
    //   get
    //   {
    //       Count.RecordGet();
    //       if (Count.HasGet) return Count.InvokeGetCallback();  // Get wins
    //       return Count_;                                          // Stub override
    //   }
    //
    // Reset() behavior: Clears tracking state but PRESERVES Get/Set configuration.
    // This matches regular interceptor semantics.
    // =========================================================================

    public void Get_SupersedesUserProperty()
    {
        var stub = new BasicStubOverridePropertyStub();
        stub.SetCount(42);

        IStubOverridePropertyService service = stub;

        // By default, stub override property is called
        var defaultValue = service.Count;
        // defaultValue == 42 (from Count_ override)

        // Get supersedes the stub override property for per-test override
        stub.Count.Get(999);
        var overrideValue = service.Count;
        // overrideValue == 999 (Get wins)

        stub.Count.VerifyGet(Called.Exactly(2)); // Both calls tracked
    }

    public void Set_SupersedesUserProperty()
    {
        var stub = new BasicStubOverridePropertyStub();
        var capturedValue = "";

        // Set supersedes the stub override property setter
        stub.Name.Set(v => capturedValue = $"Captured: {v}");

        IStubOverridePropertyService service = stub;
        service.Name = "Test";

        // capturedValue == "Captured: Test" (Set was invoked)
        // stub._name is NOT set because Set bypassed the stub override

        stub.Name.VerifySet(Called.Once);
    }

    public void Reset_PreservesGetConfiguration()
    {
        var stub = new BasicStubOverridePropertyStub();
        stub.Count.Get(100); // Override stub override property

        IStubOverridePropertyService service = stub;
        _ = service.Count;
        stub.Count.VerifyGet(Called.Once);

        // Reset clears tracking but preserves Get
        stub.Count.Reset();
        stub.Count.VerifyGet(Called.Never);

        var value = service.Count;
        // value == 100 (Get still active)
    }
}

// =============================================================================
// MIXED SCENARIOS - Some Properties with User Impl, Some Without
// =============================================================================

[KnockOff]
public partial class MixedStubOverridePropertyStub : IMixedStubOverridePropertyService { }

public partial class MixedStubOverridePropertyStub
{
    // Only override SOME interface properties as user properties
    // Others will use the regular interceptor API (no override needed)

    private int _stubOverridePropertyValue = 100;

    protected override int WithStubOverrideProperty_ => _stubOverridePropertyValue;

    protected override string ComputedWithStubOverrideProperty_ => $"Computed: {_stubOverridePropertyValue}";

    public void SetStubOverridePropertyValue(int value) => _stubOverridePropertyValue = value;

    // WithoutStubOverrideProperty_ and ComputedWithoutStubOverrideProperty_ are NOT overridden
    // They use the regular interceptor path (Get or default)
}

[KnockOff<IMixedStubOverridePropertyService>]
public partial class MixedStubOverridePropertyDemo
{
    public void MixedScenario()
    {
        var stub = new MixedStubOverridePropertyStub();

        // Properties WITH stub override use the interceptor for tracking + Get
        stub.WithStubOverrideProperty.VerifyGet(Called.Never);

        // Properties WITHOUT stub override also use interceptor (same API)
        stub.WithoutStubOverrideProperty.Get(42);
        stub.WithoutStubOverrideProperty.VerifyGet(Called.Never);

        stub.ComputedWithoutStubOverrideProperty.Get("Configured value");

        IMixedStubOverridePropertyService service = stub;

        var r1 = service.WithStubOverrideProperty;       // 100 (from override)
        var r2 = service.WithoutStubOverrideProperty;    // 42 (from Get)
        var r3 = service.ComputedWithStubOverrideProperty;  // "Computed: 100" (from override)
        var r4 = service.ComputedWithoutStubOverrideProperty; // "Configured value" (from Get)

        stub.WithStubOverrideProperty.VerifyGet(Called.Once);
        stub.WithoutStubOverrideProperty.VerifyGet(Called.Once);
    }

    // =========================================================================
    // DESIGN OBSERVATION: Consistent Naming
    // =========================================================================
    // With the base class pattern, interceptor names are ALWAYS clean:
    // - Stub override property present: stub.Property (with override behavior)
    // - No stub override property: stub.Property (default/interceptor behavior)
    //
    // The presence or absence of a stub override is detected at compile time.
    // When override exists: Get > Stub override
    // When no override: Get > Strict/Default
    // =========================================================================
}

// =============================================================================
// STRICT MODE AND STUB OVERRIDE PROPERTIES
// =============================================================================

[KnockOff(Strict = true)]
public partial class StrictStubOverridePropertyStub : IStubOverridePropertyService { }

public partial class StrictStubOverridePropertyStub
{
    // Stub overrides bypass strict mode - they ARE the configuration
    private int _count = 10;
    private string _name = "Strict Default";
    private string _setting = "";

    protected override int Count_ => _count;
    protected override string Name_ { get => _name; set => _name = value; }
    protected override string Setting_ { set => _setting = value; }
    protected override string? Description_ => "Strict description";

    public void SetCount(int value) => _count = value;
}

[KnockOff<IStubOverridePropertyService>]
public partial class StrictModeStubOverridePropertyDemo
{
    public void StrictMode_UserPropertiesBypassed()
    {
        var stub = new StrictStubOverridePropertyStub();
        IStubOverridePropertyService service = stub;

        // Stub overrides work in strict mode - they ARE the configuration
        var count = service.Count;  // 10 (from override)
        var name = service.Name;    // "Strict Default"
        var desc = service.Description; // "Strict description"

        // Set-only also works
        service.Setting = "value";  // Calls stub override

        // All tracked correctly
        stub.Count.VerifyGet(Called.Once);
        stub.Name.VerifyGet(Called.Once);
    }

    // =========================================================================
    // DESIGN DECISION: Stub Overrides Bypass Strict Mode
    // =========================================================================
    // Strict mode means "throw if unconfigured". Stub overrides ARE configured
    // by their very existence. This is consistent - the override IS the
    // behavior, just defined in a different way than Get.
    // =========================================================================
}

// =============================================================================
// PATTERN 2: GENERIC STANDALONE INTERFACE STUB WITH STUB OVERRIDE PROPERTIES
// =============================================================================

[KnockOff]
public partial class GenericStubOverridePropertyStub<T> : IGenericStubOverridePropertyService<T> where T : class { }

public partial class GenericStubOverridePropertyStub<T> where T : class
{
    // =========================================================================
    // Generic User Properties
    // =========================================================================
    // User properties work naturally with generic type parameters.
    // The base class generates virtual properties using the type parameter:
    //
    // GENERATED BASE CLASS (GenericStubOverridePropertyStubBase<T>):
    // protected virtual T? CurrentItem_ => default!;
    // protected virtual T? DefaultItem_ { get => default!; set { } }
    // protected virtual int ItemCount_ => default!;
    // =========================================================================

    private T? _currentItem;
    private T? _defaultItem;
    private int _itemCount;

    protected override T? CurrentItem_ => _currentItem;

    protected override T? DefaultItem_
    {
        get => _defaultItem;
        set => _defaultItem = value;
    }

    protected override int ItemCount_ => _itemCount;

    // Public methods for testing
    public void SetCurrentItem(T? item) => _currentItem = item;
    public void SetItemCount(int count) => _itemCount = count;
}

[KnockOff<IGenericStubOverridePropertyService<string>>]
public partial class GenericStubOverridePropertyDemo
{
    public void GenericStandalone_Usage()
    {
        var stub = new GenericStubOverridePropertyStub<string>();
        stub.SetCurrentItem("Current Value");
        stub.SetItemCount(5);

        IGenericStubOverridePropertyService<string> service = stub;

        var current = service.CurrentItem;
        // current == "Current Value"

        service.DefaultItem = "New Default";
        var defaultVal = service.DefaultItem;
        // defaultVal == "New Default"

        var count = service.ItemCount;
        // count == 5

        // Tracking works as expected
        stub.CurrentItem.VerifyGet(Called.Once);
        stub.DefaultItem.VerifyGet(Called.Once);
        stub.DefaultItem.VerifySet(Called.Once);
    }

    public void GenericStandalone_GetSupersedes()
    {
        var stub = new GenericStubOverridePropertyStub<string>();
        stub.SetCurrentItem("Default");

        // Get supersedes the stub override property
        stub.CurrentItem.Get("Override Value");

        IGenericStubOverridePropertyService<string> service = stub;
        var value = service.CurrentItem;
        // value == "Override Value" (Get wins over stub override property)
    }
}

// =============================================================================
// PATTERN 3: STANDALONE CLASS STUB WITH STUB OVERRIDE PROPERTIES
// =============================================================================
// When stubbing a CLASS (not interface), user properties work the same way.
// The key difference: use .Object to access the class instance.
// =============================================================================

[KnockOffBase<ConfigBase>]
public partial class ConfigStubOverridePropertyStub { }

public partial class ConfigStubOverridePropertyStub
{
    // =========================================================================
    // Class Stub User Properties
    // =========================================================================
    // For standalone class stubs, user properties override the base class's
    // abstract/virtual properties. The pattern is identical to interface stubs.
    //
    // GENERATED BASE CLASS (ConfigStubOverridePropertyStubBase):
    // protected virtual string ConfigName_ => default!;
    // protected virtual int MaxRetries_ { get => default!; set { } }
    // protected virtual bool IsDebugMode_ => default!;
    // protected virtual string LogPath_ { set { } }
    // =========================================================================

    private string _configName = "Default Config";
    private int _maxRetries = 5;
    private bool _isDebugMode = true;
    private string _logPath = "";

    protected override string ConfigName_ => _configName;

    protected override int MaxRetries_
    {
        get => _maxRetries;
        set => _maxRetries = value;
    }

    protected override bool IsDebugMode_ => _isDebugMode;

    protected override string LogPath_
    {
        set => _logPath = value;
    }

    // Public methods for testing
    public void SetConfigName(string name) => _configName = name;
    public void SetDebugMode(bool value) => _isDebugMode = value;
#pragma warning disable CA1024 // Use properties where appropriate - intentional method for demo
    public string GetLogPath() => _logPath;
#pragma warning restore CA1024
}

[KnockOff<ConfigBase>]
public partial class StandaloneClassStubOverridePropertyDemo
{
    public void StandaloneClass_Usage()
    {
        var stub = new ConfigStubOverridePropertyStub();
        stub.SetConfigName("Test Config");

        // IMPORTANT: Use .Object to get the actual class instance
        ConfigBase config = stub.Object;

        var name = config.ConfigName;
        // name == "Test Config" (from stub override)

        var retries = config.MaxRetries;
        // retries == 5 (from stub override)

        config.MaxRetries = 10;
        retries = config.MaxRetries;
        // retries == 10 (setter updated backing field)

        var debugMode = config.IsDebugMode;
        // debugMode == true (from stub override)

        config.LogPath = "/var/log/app.log";
        var path = stub.GetLogPath();
        // path == "/var/log/app.log"

        // Tracking works as expected
        stub.ConfigName.VerifyGet(Called.Once);
        stub.MaxRetries.VerifyGet(Called.Exactly(2));
        stub.MaxRetries.VerifySet(Called.Once);
    }

    public void StandaloneClass_GetSupersedes()
    {
        var stub = new ConfigStubOverridePropertyStub();
        stub.SetConfigName("Default");

        // Get supersedes the stub override property
        stub.ConfigName.Get("Override Config");

        ConfigBase config = stub.Object;
        var name = config.ConfigName;
        // name == "Override Config" (Get wins)

        stub.ConfigName.VerifyGet(Called.Once);
    }
}

// =============================================================================
// PATTERN 4: GENERIC STANDALONE CLASS STUB WITH STUB OVERRIDE PROPERTIES
// =============================================================================

[KnockOffBase(typeof(CacheBase<>))]
public partial class CacheStubOverridePropertyStub<T> where T : class { }

public partial class CacheStubOverridePropertyStub<T> where T : class
{
    // =========================================================================
    // Generic Class Stub User Properties
    // =========================================================================
    // For generic standalone class stubs, user properties work with the type
    // parameter from the stub class declaration.
    //
    // GENERATED BASE CLASS (CacheStubOverridePropertyStubBase<T>):
    // protected virtual T? DefaultValue_ => default!;
    // protected virtual T? CurrentValue_ { get => default!; set { } }
    // protected virtual int CacheSize_ => default!;
    // protected virtual string CacheName_ => default!;
    // =========================================================================

    private T? _defaultValue;
    private T? _currentValue;
    private int _cacheSize;
    private string _cacheName = "Default Cache";

    protected override T? DefaultValue_ => _defaultValue;

    protected override T? CurrentValue_
    {
        get => _currentValue;
        set => _currentValue = value;
    }

    protected override int CacheSize_ => _cacheSize;

    protected override string CacheName_ => _cacheName;

    // Public methods for testing
    public void SetDefaultValue(T? value) => _defaultValue = value;
    public void SetCacheSize(int size) => _cacheSize = size;
    public void SetCacheName(string name) => _cacheName = name;
}

[KnockOff(typeof(CacheBase<>))]
public partial class GenericStandaloneClassStubOverridePropertyDemo
{
    public void GenericStandaloneClass_Usage()
    {
        var stub = new CacheStubOverridePropertyStub<string>();
        stub.SetDefaultValue("Default String");
        stub.SetCacheSize(100);
        stub.SetCacheName("String Cache");

        // Use .Object to get the actual class instance
        CacheBase<string> cache = stub.Object;

        var defaultVal = cache.DefaultValue;
        // defaultVal == "Default String"

        cache.CurrentValue = "Current String";
        var currentVal = cache.CurrentValue;
        // currentVal == "Current String"

        var size = cache.CacheSize;
        // size == 100

        var name = cache.CacheName;
        // name == "String Cache"

        // Tracking works as expected
        stub.DefaultValue.VerifyGet(Called.Once);
        stub.CurrentValue.VerifyGet(Called.Once);
        stub.CurrentValue.VerifySet(Called.Once);
    }

    public void GenericStandaloneClass_GetSupersedes()
    {
        var stub = new CacheStubOverridePropertyStub<string>();
        stub.SetDefaultValue("Default");

        // Get supersedes the stub override property
        stub.DefaultValue.Get("Override Default");

        CacheBase<string> cache = stub.Object;
        var value = cache.DefaultValue;
        // value == "Override Default" (Get wins)
    }
}

// =============================================================================
// DESIGN SUMMARY - STUB OVERRIDE PROPERTIES
// =============================================================================
//
// **THE BASE CLASS PATTERN:**
// KnockOff generates a base class with virtual protected properties suffixed with
// underscore (e.g., Count_). Users override these properties to provide default
// behavior. The compiler enforces signature correctness.
//
// **KEY BENEFITS:**
// 1. Clean interceptor names - stub.Count instead of stub.Count2
// 2. Compile-time safety - typos/signature errors caught by compiler
// 3. IntelliSense discovery - type override and see available properties
// 4. Same API for configured and override properties
//
// **PROPERTY ACCESSOR PATTERNS:**
// - Get-only: protected override int Count_ => _count;
// - Set-only: protected override string Setting_ { set => _setting = value; }
// - Get/set: protected override string Name_ { get => _name; set => _name = value; }
//
// **APPLICABLE PATTERNS (Standalone Only):**
// User properties apply to standalone patterns only (same as stub overrides).
// Inline patterns generate the entire stub class - no partial for stub overrides.
//
// | Pattern | User Properties Supported |
// |---------|---------------------------|
// | 1. Standalone | Yes - [KnockOff] partial class : IInterface |
// | 2. Generic Standalone | Yes - [KnockOff] partial class<T> : IInterface<T> |
// | 3. Standalone Class | Yes - [KnockOffBase<Class>] partial class |
// | 4. Generic Standalone Class | Yes - [KnockOffBase(typeof(Class<>))] partial class<T> |
// | 5-9. Inline patterns | No - entire class generated, no partial available |
//
// **PRIORITY ORDER:**
// Get()/Set() > Stub override > Smart default (or StubException in strict mode)
//
// **NOT SUPPORTED (by design):**
// - Inline stubs - only standalone patterns support user properties
// - Indexers - see separate design for user indexer overrides
//
// =============================================================================
