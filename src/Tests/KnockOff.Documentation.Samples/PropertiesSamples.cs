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
        stub.CurrentUser.OnGet(new User { Id = 1, Name = "Alice" });

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
        stub.UserId.OnGet(42);
        stub.Email.OnGet("test@example.com");
        stub.CurrentUser.OnGet(new User { Id = 42, Name = "Test User" });

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
    #region properties-onget-value-vs-callback
    [Fact]
    public void OnGet_ValueVsCallback()
    {
        var stub = new ConfigPropsStub();

        // VALUE: Simple syntax for static values
        stub.Name.OnGet("StaticName");

        // CALLBACK: For computed or dynamic values
        stub.Age.OnGet(() => DateTime.Now.Year - 2000);

        IConfigProps config = stub;

        Assert.Equal("StaticName", config.Name);
        Assert.True(config.Age >= 0); // Dynamic value
    }
    #endregion

    #region properties-onget-dynamic
    [Fact]
    public void OnGet_ReturnsComputedValue()
    {
        var stub = new TimeProviderPropsStub();

        // OnGet callback returns dynamic value on each access
        stub.Timestamp.OnGet(() => DateTime.UtcNow);

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

        // Track initialization state with local variable
        var isInitialized = false;

        // OnGet checks the tracked state
        stub.IsReady.OnGet(() => isInitialized);
        var initTracking = stub.Initialize.OnCall(() => { isInitialized = true; });

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
        stub.Name.OnSet((value) => setValues.Add(value));

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
        stub.Age.OnSet((value) =>
        {
            if (value < 0)
                throw new ArgumentException("Age cannot be negative");
        });

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
    public void VerifyGet_TracksPropertyReads()
    {
        var stub = new ConfigPropsStub();
        stub.Age.OnGet(42);

        IConfigProps service = stub;

        _ = service.Age;
        _ = service.Age;

        // VerifyGet checks how many times property was read
        stub.Age.VerifyGet(Times.Exactly(2));
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
        stub.Name.OnGet("test");
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
// Sequence Behavior Samples
// =============================================================================

public class PropertySequenceTests
{
    #region properties-ongetsequence-value
    [Fact]
    public void OnGet_ValueSyntax_ThenGet()
    {
        var stub = new ConfigPropsStub();

        // OnGet with value - simpler syntax for static values
        // ThenGet elevates to sequence mode
        stub.Name.OnGet("First")
            .ThenGet(() => "Second")
            .ThenGet(() => "Third");

        IConfigProps config = stub;

        Assert.Equal("First", config.Name);
        Assert.Equal("Second", config.Name);
        Assert.Equal("Third", config.Name);
    }
    #endregion

    #region properties-onget-then-sequence
    [Fact]
    public void OnGet_ThenGet_ReturnsDifferentValuesOnSuccessiveReads()
    {
        var stub = new ConfigPropsStub();

        // OnGet().ThenGet() configures different return values for each read
        stub.Name
            .OnGet(() => "First")
            .ThenGet(() => "Second")
            .ThenGet(() => "Third");

        IConfigProps config = stub;

        // Each read returns the next value in the sequence
        Assert.Equal("First", config.Name);
        Assert.Equal("Second", config.Name);
        Assert.Equal("Third", config.Name);
    }
    #endregion

    #region properties-sequence-exhaustion
    [Fact]
    public void Sequence_ExhaustionRepeatsLastValue()
    {
        var stub = new ConfigPropsStub();

        // Configure a sequence of three values
        stub.Name.OnGet("first")
            .ThenGet("second")
            .ThenGet("third");

        IConfigProps config = stub;

        // Each read advances through the sequence
        Assert.Equal("first", config.Name);
        Assert.Equal("second", config.Name);
        Assert.Equal("third", config.Name);

        // After exhaustion, repeats the last value
        Assert.Equal("third", config.Name);
        Assert.Equal("third", config.Name);
    }
    #endregion

    #region properties-sequence-thendefault
    [Fact]
    public void Sequence_ThenDefault_ReturnsDefaultAfterExhaustion()
    {
        var stub = new ConfigPropsStub();

        // ThenDefault() changes exhaustion behavior
        stub.Name.OnGet("first")
            .ThenGet("second")
            .ThenDefault();

        IConfigProps config = stub;

        Assert.Equal("first", config.Name);
        Assert.Equal("second", config.Name);

        // After exhaustion, returns default (null for string)
        Assert.Null(config.Name);
        Assert.Null(config.Name);
    }
    #endregion

    #region properties-onset-then-sequence
    [Fact]
    public void OnSet_ThenSet_ReactsDifferentlyToSuccessiveWrites()
    {
        var stub = new ConfigPropsStub();

        var firstWriteValue = "";
        var secondWriteValue = "";

        // OnSet().ThenSet() configures different callbacks for each write
        stub.Name
            .OnSet((value) => { firstWriteValue = $"First: {value}"; })
            .ThenSet((value) => { secondWriteValue = $"Second: {value}"; });

        IConfigProps config = stub;

        // First write triggers first callback
        config.Name = "Alpha";
        Assert.Equal("First: Alpha", firstWriteValue);
        Assert.Equal("", secondWriteValue);

        // Second write triggers second callback
        config.Name = "Beta";
        Assert.Equal("Second: Beta", secondWriteValue);
    }
    #endregion

    #region properties-sequence-verification
    [Fact]
    public void Sequence_VerifiesLikeRegularCallbacks()
    {
        var stub = new ConfigPropsStub();

        // Configure sequences
        var getSequence = stub.Name
            .OnGet(() => "A")
            .ThenGet(() => "B");

        var setSequence = stub.Age
            .OnSet((v) => { })
            .ThenSet((v) => { });

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
        stub.Name.VerifyGet(Times.Exactly(2));
        stub.Age.VerifySet(Times.Exactly(2));
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

        stub.Name.OnGet("test");

        IConfigProps config = stub;

        // Access property to increment counts
        _ = config.Name;
        config.Name = "updated";

        stub.Name.VerifyGet(Times.AtLeastOnce);
        stub.Name.VerifySet(Times.AtLeastOnce);

        // Reset clears counts and callbacks
        stub.Name.Reset();

        stub.Name.VerifyGet(Times.Never);
        stub.Name.VerifySet(Times.Never);
        // Note: Reset clears tracking counters and all configured callbacks
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
        stub.Name.OnGet("initial");

        // Then set OnGet - it takes precedence
        stub.Name.OnGet(() => "dynamic");

        IConfigProps config = stub;

        // Callback syntax takes precedence (last call wins)
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

        // Track connection state with local variable
        var isConnected = false;

        // OnGet with static value: Fixed test data
        stub.CurrentUser.OnGet(new User { Id = 1, Name = "Alice" });

        // OnGet: State-dependent behavior using tracked state
        stub.IsConnected.OnGet(() => isConnected);

        // OnSet: Track all values written
        var connectionStrings = new List<string>();
        stub.ConnectionString.OnSet((value) => connectionStrings.Add(value));

        // Configure the Connect method to update state
        var connectTracking = stub.Connect.OnCall(() => { isConnected = true; });

        IUserConfigComplete service = stub;

        // Test execution
        var user = service.CurrentUser;            // Read CurrentUser
        Assert.False(service.IsConnected);         // Not connected yet

        service.Connect();                          // Call Connect
        Assert.True(service.IsConnected);          // Now connected

        service.ConnectionString = "Server=test";  // Write ConnectionString

        // Verification
        stub.CurrentUser.VerifyGet(Times.Once);
        Assert.True(service.IsConnected);
        Assert.Single(connectionStrings);
        Assert.Equal("Server=test", stub.ConnectionString.LastSetValue);
    }
    #endregion
}
