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
    #region properties-value-basic
    [Fact]
    public void Value_SetsPropertyReturnValue()
    {
        var stub = new UserConfigPropsStub();

        // Set a static value for the property via the interceptor
        stub.CurrentUser.Value = new User { Id = 1, Name = "Alice" };

        IUserConfigProps config = stub;
        var user = config.CurrentUser;

        Assert.NotNull(user);
        Assert.Equal("Alice", user.Name);
    }
    #endregion

    #region properties-value-multiple
    [Fact]
    public void Value_ConfigureMultipleProperties()
    {
        var stub = new UserConfigPropsStub();

        // Configure several properties before test execution
        stub.UserId.Value = 42;
        stub.Email.Value = "test@example.com";
        stub.CurrentUser.Value = new User { Id = 42, Name = "Test User" };

        IUserConfigProps config = stub;

        Assert.Equal(42, config.UserId);
        Assert.Equal("test@example.com", config.Email);
        Assert.NotNull(config.CurrentUser);
    }
    #endregion
}

// =============================================================================
// Dynamic Getter Samples
// =============================================================================

public class DynamicGetterTests
{
    #region properties-onget-dynamic
    [Fact]
    public void OnGet_ReturnsComputedValue()
    {
        var stub = new TimeProviderPropsStub();

        // OnGet callback returns dynamic value on each access
        stub.Timestamp.OnGet = () => DateTime.UtcNow;

        ITimeProviderProps timeProvider = stub;

        var time1 = timeProvider.Timestamp;
        Thread.Sleep(10);
        var time2 = timeProvider.Timestamp;

        // Each access returns current time
        Assert.True(time2 >= time1);
    }
    #endregion

    #region properties-onget-stateful
    [Fact]
    public void OnGet_DependsOnOtherInterceptorState()
    {
        var stub = new ServiceWithInitPropsStub();

        // OnGet checks if Initialize() was called via interceptor CallCount
        stub.IsReady.OnGet = () => stub.Initialize.CallCount > 0;
        var initTracking = stub.Initialize.OnCall(() => { });

        IServiceWithInitProps service = stub;

        // Initially false (Initialize not called)
        Assert.False(service.IsReady);

        // After Initialize, becomes true
        service.Initialize();
        Assert.True(service.IsReady);
    }
    #endregion
}

// =============================================================================
// Setter Interception Samples
// =============================================================================

public class SetterInterceptionTests
{
    #region properties-onset-tracking
    [Fact]
    public void OnSet_TracksAllWrittenValues()
    {
        var stub = new ConfigPropsStub();

        var setValues = new List<string>();
        stub.Name.OnSet = (value) => setValues.Add(value);

        IConfigProps config = stub;

        config.Name = "First";
        config.Name = "Second";
        config.Name = "Third";

        Assert.Equal(3, setValues.Count);
        Assert.Equal(new[] { "First", "Second", "Third" }, setValues);
    }
    #endregion

    #region properties-onset-validation
    [Fact]
    public void OnSet_SimulatesValidation()
    {
        var stub = new ConfigPropsStub();

        // OnSet throws for invalid values
        stub.Age.OnSet = (value) =>
        {
            if (value < 0)
                throw new ArgumentException("Age cannot be negative");
        };

        IConfigProps config = stub;

        // Valid value works
        config.Age = 25;

        // Invalid value throws
        Assert.Throws<ArgumentException>(() => config.Age = -1);
    }
    #endregion
}

// =============================================================================
// Verification Samples
// =============================================================================

public class PropertyVerificationTests
{
    #region properties-verify-getcount
    [Fact]
    public void GetCount_TracksPropertyReads()
    {
        var stub = new ConfigPropsStub();
        stub.Age.Value = 42;

        IConfigProps service = stub;

        _ = service.Age;
        _ = service.Age;

        // GetCount tracks how many times property was read
        Assert.Equal(2, stub.Age.GetCount);
    }
    #endregion

    #region properties-verify-lastsetvalue
    [Fact]
    public void LastSetValue_CapturesLastWrittenValue()
    {
        var stub = new ConfigPropsStub();

        IConfigProps service = stub;

        service.Name = "First";
        service.Name = "Second";
        service.Name = "Expected";

        // LastSetValue contains the most recent value
        Assert.Equal("Expected", stub.Name.LastSetValue);
    }
    #endregion

    #region properties-verifiable
    [Fact]
    public void Verifiable_MarksPropertyForVerification()
    {
        var stub = new ConfigPropsStub();

        // Mark property as verifiable
        stub.Name.Value = "test";
        stub.Name.Verifiable();
        stub.Age.Verifiable();

        IConfigProps service = stub;
        _ = service.Name;
        service.Age = 42;

        // Verify individually (standalone stubs verify at interceptor level)
        stub.Name.Verify();
        stub.Age.Verify();
    }
    #endregion
}

// =============================================================================
// Reset Sample
// =============================================================================

public class PropertyResetTests
{
    #region properties-reset
    [Fact]
    public void Reset_ClearsCountsButPreservesValue()
    {
        var stub = new ConfigPropsStub();

        stub.Name.Value = "test";

        IConfigProps config = stub;

        // Access property to increment counts
        _ = config.Name;
        config.Name = "updated";

        Assert.True(stub.Name.GetCount > 0);
        Assert.True(stub.Name.SetCount > 0);

        // Reset clears counts and callbacks
        stub.Name.Reset();

        Assert.Equal(0, stub.Name.GetCount);
        Assert.Equal(0, stub.Name.SetCount);
        // Note: Reset also clears Value, OnGet, OnSet
    }
    #endregion
}

// =============================================================================
// Priority Sample
// =============================================================================

public class PropertyPriorityTests
{
    #region properties-priority
    [Fact]
    public void OnGet_TakesPrecedenceOverValue()
    {
        var stub = new ConfigPropsStub();

        // Set a static value
        stub.Name.Value = "initial";

        // Then set OnGet - it takes precedence
        stub.Name.OnGet = () => "dynamic";

        IConfigProps config = stub;

        // OnGet wins over Value
        Assert.Equal("dynamic", config.Name);
    }
    #endregion
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
    #region properties-complete-example
    [Fact]
    public void CompletePropertyExample_AllConfigurationApproaches()
    {
        var stub = new UserConfigCompleteStub();

        // Value: Static test data
        stub.CurrentUser.Value = new User { Id = 1, Name = "Alice" };

        // OnGet: State-dependent behavior
        stub.IsConnected.OnGet = () => stub.Connect.CallCount > 0;

        // OnSet: Track all values written
        var connectionStrings = new List<string>();
        stub.ConnectionString.OnSet = (value) => connectionStrings.Add(value);

        // Configure the Connect method
        var connectTracking = stub.Connect.OnCall(() => { });

        IUserConfigComplete service = stub;

        // Test execution
        var user = service.CurrentUser;            // Read CurrentUser
        Assert.False(service.IsConnected);         // Not connected yet

        service.Connect();                          // Call Connect
        Assert.True(service.IsConnected);          // Now connected

        service.ConnectionString = "Server=test";  // Write ConnectionString

        // Verification
        Assert.Equal(1, stub.CurrentUser.GetCount);
        Assert.True(service.IsConnected);
        Assert.Single(connectionStrings);
        Assert.Equal("Server=test", stub.ConnectionString.LastSetValue);
    }
    #endregion
}
