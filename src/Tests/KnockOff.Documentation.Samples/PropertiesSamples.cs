namespace KnockOff.Documentation.Samples.Properties;

// =============================================================================
// Interfaces for Property Samples
// =============================================================================

public interface IUserConfigProps
{
    User? CurrentUser { get; set; }
    int UserId { get; set; }
    string Email { get; set; }
}

public interface ITimeProviderProps
{
    DateTime Timestamp { get; }
}

public interface IServiceWithInitProps
{
    bool IsReady { get; }
    void Initialize();
}

public interface IConfigProps
{
    string Name { get; set; }
    int Age { get; set; }
}

// =============================================================================
// Stubs for Property Samples
// =============================================================================

[KnockOff]
public partial class UserConfigPropsStub : IUserConfigProps { }

[KnockOff]
public partial class TimeProviderPropsStub : ITimeProviderProps { }

[KnockOff]
public partial class ServiceWithInitPropsStub : IServiceWithInitProps { }

[KnockOff]
public partial class ConfigPropsStub : IConfigProps { }

// =============================================================================
// Static Value Samples
// =============================================================================

public class StaticValueTests
{
    [Fact]
    public void Value_SetsPropertyReturnValue()
    {
        var stub = new UserConfigPropsStub();

        #region properties-value-basic
        // Set a static value for the property via the interceptor
        stub.CurrentUser.Get(new User { Id = 1, Name = "Alice" });
        #endregion

        IUserConfigProps config = stub;
        var user = config.CurrentUser;

        Assert.NotNull(user);
        Assert.Equal("Alice", user.Name);
    }

    [Fact]
    public void Value_ConfigureMultipleProperties()
    {
        var stub = new UserConfigPropsStub();

        #region properties-value-multiple
        // Configure several properties before test execution
        stub.UserId.Get(42);
        stub.Email.Get("test@example.com");
        stub.CurrentUser.Get(new User { Id = 42, Name = "Test User" });
        #endregion

        IUserConfigProps config = stub;

        Assert.Equal(42, config.UserId);
        Assert.Equal("test@example.com", config.Email);
        Assert.NotNull(config.CurrentUser);
    }
}

// =============================================================================
// Dynamic Getter Samples
// =============================================================================

public class DynamicGetterTests
{
    [Fact]
    public void OnGet_ValueVsCallback()
    {
        var stub = new ConfigPropsStub();

        #region properties-onget-value-vs-callback
        // VALUE: Simple syntax for static values
        stub.Name.Get("StaticName");

        // CALLBACK: For computed or dynamic values
        stub.Age.Get(() => DateTime.Now.Year - 2000);
        #endregion

        IConfigProps config = stub;

        Assert.Equal("StaticName", config.Name);
        Assert.True(config.Age >= 0); // Dynamic value
    }

    [Fact]
    public void OnGet_ReturnsComputedValue()
    {
        var stub = new TimeProviderPropsStub();

        #region properties-onget-dynamic
        // Get callback returns dynamic value on each access
        stub.Timestamp.Get(() => DateTime.UtcNow);
        #endregion

        ITimeProviderProps timeProvider = stub;

        var time1 = timeProvider.Timestamp;
        Thread.Sleep(10);
        var time2 = timeProvider.Timestamp;

        // Each access returns current time
        Assert.True(time2 >= time1);
    }

    [Fact]
    public void OnGet_DependsOnOtherInterceptorState()
    {
        var stub = new ServiceWithInitPropsStub();

        // Track initialization state with local variable
        var isInitialized = false;

        #region properties-onget-stateful
        // Get checks the tracked state
        stub.IsReady.Get(() => isInitialized);
        // Initialize method updates the tracked state
        stub.Initialize.Call(() => { isInitialized = true; });
        #endregion

        IServiceWithInitProps service = stub;

        // Initially false (Initialize not called)
        Assert.False(service.IsReady);

        // After Initialize, becomes true
        service.Initialize();
        Assert.True(service.IsReady);
    }
}

// =============================================================================
// Setter Interception Samples
// =============================================================================

public class SetterInterceptionTests
{
    [Fact]
    public void OnSet_TracksAllWrittenValues()
    {
        var stub = new ConfigPropsStub();

        #region properties-onset-tracking
        // Set captures every value written to the property
        var setValues = new List<string>();
        stub.Name.Set((value) => setValues.Add(value));
        #endregion

        IConfigProps config = stub;

        config.Name = "First";
        config.Name = "Second";
        config.Name = "Third";

        Assert.Equal(3, setValues.Count);
        Assert.Equal(new[] { "First", "Second", "Third" }, setValues);
    }

    [Fact]
    public void OnSet_SimulatesValidation()
    {
        var stub = new ConfigPropsStub();

        #region properties-onset-validation
        // Set throws for invalid values
        stub.Age.Set((value) =>
        {
            if (value < 0)
                throw new ArgumentException("Age cannot be negative");
        });
        #endregion

        IConfigProps config = stub;

        // Valid value works
        config.Age = 25;

        // Invalid value throws
        Assert.Throws<ArgumentException>(() => config.Age = -1);
    }
}

// =============================================================================
// Verification Samples
// =============================================================================

public class PropertyVerificationTests
{
    [Fact]
    public void VerifyGet_TracksPropertyReads()
    {
        var stub = new ConfigPropsStub();
        stub.Age.Get(42);

        IConfigProps service = stub;

        _ = service.Age;
        _ = service.Age;

        #region properties-verify-getcount
        // VerifyGet checks how many times property was read
        stub.Age.VerifyGet(Called.Exactly(2));
        #endregion
    }

    [Fact]
    public void LastSetValue_CapturesLastWrittenValue()
    {
        var stub = new ConfigPropsStub();

        IConfigProps service = stub;

        service.Name = "First";
        service.Name = "Second";
        service.Name = "Expected";

        #region properties-verify-lastsetvalue
        // LastSetValue contains the most recent value
        Assert.Equal("Expected", stub.Name.LastSetValue);
        #endregion
    }

    [Fact]
    public void Verifiable_MarksPropertyForVerification()
    {
        var stub = new ConfigPropsStub();

        #region properties-verifiable
        // Mark property as verifiable - requires access before Verify()
        stub.Name.Get("test");
        stub.Name.Verifiable();
        stub.Age.Verifiable();
        #endregion

        IConfigProps service = stub;
        _ = service.Name;
        service.Age = 42;

        // Verify individually (standalone stubs verify at interceptor level)
        stub.Name.Verify();
        stub.Age.Verify();
    }
}

// =============================================================================
// Sequence Behavior Samples
// =============================================================================

public class PropertySequenceTests
{
    [Fact]
    public void OnGet_ValueSyntax_ThenGet()
    {
        var stub = new ConfigPropsStub();

        #region properties-ongetsequence-value
        // Get with value, ThenGet elevates to sequence mode
        stub.Name.Get("First")
            .ThenGet(() => "Second")
            .ThenGet(() => "Third");
        #endregion

        IConfigProps config = stub;

        Assert.Equal("First", config.Name);
        Assert.Equal("Second", config.Name);
        Assert.Equal("Third", config.Name);
    }

    [Fact]
    public void OnGet_ThenGet_ReturnsDifferentValuesOnSuccessiveReads()
    {
        var stub = new ConfigPropsStub();

        #region properties-onget-then-sequence
        // Get().ThenGet() configures different return values for each read
        stub.Name
            .Get(() => "First")
            .ThenGet(() => "Second")
            .ThenGet(() => "Third");
        #endregion

        IConfigProps config = stub;

        // Each read returns the next value in the sequence
        Assert.Equal("First", config.Name);
        Assert.Equal("Second", config.Name);
        Assert.Equal("Third", config.Name);
    }

    [Fact]
    public void Sequence_ExhaustionRepeatsLastValue()
    {
        var stub = new ConfigPropsStub();

        #region properties-sequence-exhaustion
        // Configure a sequence - after exhaustion, repeats last value
        stub.Name.Get("first")
            .ThenGet("second")
            .ThenGet("third");
        #endregion

        IConfigProps config = stub;

        // Each read advances through the sequence
        Assert.Equal("first", config.Name);
        Assert.Equal("second", config.Name);
        Assert.Equal("third", config.Name);

        // After exhaustion, repeats the last value
        Assert.Equal("third", config.Name);
        Assert.Equal("third", config.Name);
    }

    [Fact]
    public void Sequence_ThenDefault_ReturnsDefaultAfterExhaustion()
    {
        var stub = new ConfigPropsStub();

        #region properties-sequence-thendefault
        // ThenDefault() returns default(T) after exhaustion instead of repeating
        stub.Name.Get("first")
            .ThenGet("second")
            .ThenDefault();
        #endregion

        IConfigProps config = stub;

        Assert.Equal("first", config.Name);
        Assert.Equal("second", config.Name);

        // After exhaustion, returns default (null for string)
        Assert.Null(config.Name);
        Assert.Null(config.Name);
    }

    [Fact]
    public void OnSet_ThenSet_ReactsDifferentlyToSuccessiveWrites()
    {
        var stub = new ConfigPropsStub();

        var firstWriteValue = "";
        var secondWriteValue = "";

        #region properties-onset-then-sequence
        // Set().ThenSet() configures different callbacks for each write
        stub.Name
            .Set((value) => { firstWriteValue = $"First: {value}"; })
            .ThenSet((value) => { secondWriteValue = $"Second: {value}"; });
        #endregion

        IConfigProps config = stub;

        // First write triggers first callback
        config.Name = "Alpha";
        Assert.Equal("First: Alpha", firstWriteValue);
        Assert.Equal("", secondWriteValue);

        // Second write triggers second callback
        config.Name = "Beta";
        Assert.Equal("Second: Beta", secondWriteValue);
    }

    [Fact]
    public void Sequence_VerifiesLikeRegularCallbacks()
    {
        var stub = new ConfigPropsStub();

        #region properties-sequence-verification
        // Sequences support verification like regular callbacks
        var getSequence = stub.Name
            .Get(() => "A")
            .ThenGet(() => "B");

        var setSequence = stub.Age
            .Set((v) => { })
            .ThenSet((v) => { });
        #endregion

        IConfigProps config = stub;

        // Access properties
        _ = config.Name;
        _ = config.Name;
        config.Age = 1;
        config.Age = 2;

        // Verify sequence was fully consumed
        getSequence.Verify();
        setSequence.Verify();

        // VerifyGet/VerifySet work the same with sequences
        stub.Name.VerifyGet(Called.Exactly(2));
        stub.Age.VerifySet(Called.Exactly(2));
    }
}

// =============================================================================
// Reset Sample
// =============================================================================

public class PropertyResetTests
{
    [Fact]
    public void Reset_ClearsCountsButPreservesValue()
    {
        var stub = new ConfigPropsStub();

        stub.Name.Get("test");

        IConfigProps config = stub;

        // Access property to increment counts
        _ = config.Name;
        config.Name = "updated";

        stub.Name.VerifyGet(Called.AtLeastOnce);
        stub.Name.VerifySet(Called.AtLeastOnce);

        #region properties-reset
        // Reset clears counts but preserves callbacks
        stub.Name.Reset();

        stub.Name.VerifyGet(Called.Never);
        stub.Name.VerifySet(Called.Never);
        #endregion
    }
}

// =============================================================================
// Priority Sample
// =============================================================================

public class PropertyPriorityTests
{
    [Fact]
    public void OnGet_TakesPrecedenceOverValue()
    {
        var stub = new ConfigPropsStub();

        #region properties-priority
        // Last Get call wins - can upgrade from value to callback
        stub.Name.Get("initial");
        stub.Name.Get(() => "dynamic");
        #endregion

        IConfigProps config = stub;

        // Callback syntax takes precedence (last call wins)
        Assert.Equal("dynamic", config.Name);
    }
}

// =============================================================================
// User Properties Samples
// =============================================================================

#region stub-override-properties-interface-and-stub
public interface ISkillUserSvc
{
    int Count { get; }
    string Name { get; set; }
    string Setting { set; }
}

[KnockOff]
public partial class SkillUserSvcStub : ISkillUserSvc { }

public partial class SkillUserSvcStub
{
    private int _count;
    private string _name = "";
    private string _setting = "";

    // Get-only property override
    protected override int Count_ => _count;

    // Get/set property override
    protected override string Name_
    {
        get => _name;
        set => _name = value;
    }

    // Set-only property override
    protected override string Setting_
    {
        set => _setting = value;
    }

    // Public methods for test setup
    public void SetCount(int value) => _count = value;
}
#endregion

// Strict mode stub for user properties
[KnockOff(Strict = true)]
public partial class StrictSkillUserSvcStub : ISkillUserSvc { }

public partial class StrictSkillUserSvcStub
{
    protected override int Count_ => 10;  // This IS configured
    protected override string Name_ { get => ""; set { } }
    protected override string Setting_ { set { } }
}

public class StubOverridePropertyBasicTests
{
    [Fact]
    public void StubOverrideProperty_ProvidesDefaultBehavior()
    {
        #region stub-override-properties-basic-usage
        var stub = new SkillUserSvcStub();
        stub.SetCount(42);

        ISkillUserSvc service = stub;

        // Get-only property uses your protected override Count_
        var count = service.Count;  // 42

        // Get/set property uses your protected override Name_
        service.Name = "Test";
        var name = service.Name;    // "Test"

        // Set-only property uses your protected override Setting_
        service.Setting = "value";
        #endregion

        Assert.Equal(42, count);
        Assert.Equal("Test", name);
    }
}

public class StubOverridePropertyOnGetOnSetTests
{
    [Fact]
    public void OnGetOnSet_SupersedesStubOverrideProperty()
    {
        #region stub-override-properties-onget-onset-override
        var stub = new SkillUserSvcStub();
        stub.SetCount(42);

        ISkillUserSvc service = stub;

        // Default: stub override property is called
        var defaultValue = service.Count;  // 42

        // Get supersedes the stub override property for this test
        stub.Count.Get(999);
        var overrideValue = service.Count;  // 999

        // Set supersedes the stub override property for this test
        var capturedValue = "";
        stub.Name.Set(v => capturedValue = $"Captured: {v}");
        service.Name = "Test";
        // capturedValue == "Captured: Test"
        // The stub override's backing field was NOT updated
        #endregion

        Assert.Equal(42, defaultValue);
        Assert.Equal(999, overrideValue);
        Assert.Equal("Captured: Test", capturedValue);
    }
}

public class StubOverridePropertyTrackingTests
{
    [Fact]
    public void Tracking_WorksThroughUserProperties()
    {
        #region stub-override-properties-tracking
        var stub = new SkillUserSvcStub();
        stub.SetCount(100);

        ISkillUserSvc service = stub;

        _ = service.Count;
        _ = service.Count;
        _ = service.Count;

        stub.Count.VerifyGet(Called.Exactly(3));
        stub.Name.VerifySet(Called.Never);
        #endregion
    }
}

public class StubOverridePropertyResetTests
{
    [Fact]
    public void Reset_PreservesOnGetOnSetConfiguration()
    {
        #region stub-override-properties-reset
        var stub = new SkillUserSvcStub();
        stub.Count.Get(100);  // Override stub override property

        ISkillUserSvc service = stub;
        _ = service.Count;
        stub.Count.VerifyGet(Called.Once);

        // Reset clears tracking but preserves Get
        stub.Count.Reset();
        stub.Count.VerifyGet(Called.Never);

        var value = service.Count;  // 100 (Get still active)
        #endregion

        Assert.Equal(100, value);
    }
}

public class StubOverridePropertyStrictModeTests
{
    [Fact]
    public void StrictMode_BypassedForUserProperties()
    {
        #region stub-override-properties-strict-mode
        // [KnockOff(Strict = true)]
        // public partial class StrictSkillUserSvcStub : ISkillUserSvc { }
        //
        // public partial class StrictSkillUserSvcStub
        // {
        //     protected override int Count_ => 10;  // This IS configured
        // }

        // Usage:
        var stub = new StrictSkillUserSvcStub();
        ISkillUserSvc service = stub;

        var count = service.Count;  // 10 (no exception - stub override is configured)
        #endregion

        Assert.Equal(10, count);
    }
}

// =============================================================================
// Complete Example
// =============================================================================

public interface IUserConfigComplete
{
    User? CurrentUser { get; }
    bool IsConnected { get; }
    string ConnectionString { get; set; }
    void Connect();
}

[KnockOff]
public partial class UserConfigCompleteStub : IUserConfigComplete { }

public class CompletePropertyExampleTests
{
    [Fact]
    public void CompletePropertyExample_AllConfigurationApproaches()
    {
        var stub = new UserConfigCompleteStub();

        // Track connection state with local variable
        var isConnected = false;

        #region properties-complete-example
        // Get with static value: Fixed test data
        stub.CurrentUser.Get(new User { Id = 1, Name = "Alice" });

        // Get with callback: State-dependent behavior
        stub.IsConnected.Get(() => isConnected);

        // Set: Track all values written
        var connectionStrings = new List<string>();
        stub.ConnectionString.Set((value) => connectionStrings.Add(value));

        // Method callback updates the tracked state
        stub.Connect.Call(() => { isConnected = true; });
        #endregion

        IUserConfigComplete service = stub;

        // Test execution
        var user = service.CurrentUser;            // Read CurrentUser
        Assert.False(service.IsConnected);         // Not connected yet

        service.Connect();                          // Call Connect
        Assert.True(service.IsConnected);          // Now connected

        service.ConnectionString = "Server=test";  // Write ConnectionString

        // Verification
        stub.CurrentUser.VerifyGet(Called.Once);
        Assert.True(service.IsConnected);
        Assert.Single(connectionStrings);
        Assert.Equal("Server=test", stub.ConnectionString.LastSetValue);
    }
}
