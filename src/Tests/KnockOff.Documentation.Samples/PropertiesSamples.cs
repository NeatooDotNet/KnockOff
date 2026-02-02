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
        stub.CurrentUser.OnGet(new User { Id = 1, Name = "Alice" });
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
        stub.UserId.OnGet(42);
        stub.Email.OnGet("test@example.com");
        stub.CurrentUser.OnGet(new User { Id = 42, Name = "Test User" });
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
        stub.Name.OnGet("StaticName");

        // CALLBACK: For computed or dynamic values
        stub.Age.OnGet(() => DateTime.Now.Year - 2000);
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
        // OnGet callback returns dynamic value on each access
        stub.Timestamp.OnGet(() => DateTime.UtcNow);
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
        // OnGet checks the tracked state
        stub.IsReady.OnGet(() => isInitialized);
        // Initialize method updates the tracked state
        stub.Initialize.OnCall(() => { isInitialized = true; });
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
        // OnSet captures every value written to the property
        var setValues = new List<string>();
        stub.Name.OnSet((value) => setValues.Add(value));
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
        // OnSet throws for invalid values
        stub.Age.OnSet((value) =>
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
        stub.Age.OnGet(42);

        IConfigProps service = stub;

        _ = service.Age;
        _ = service.Age;

        #region properties-verify-getcount
        // VerifyGet checks how many times property was read
        stub.Age.VerifyGet(Times.Exactly(2));
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
        stub.Name.OnGet("test");
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
        // OnGet with value, ThenGet elevates to sequence mode
        stub.Name.OnGet("First")
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
        // OnGet().ThenGet() configures different return values for each read
        stub.Name
            .OnGet(() => "First")
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
        stub.Name.OnGet("first")
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
        stub.Name.OnGet("first")
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
        // OnSet().ThenSet() configures different callbacks for each write
        stub.Name
            .OnSet((value) => { firstWriteValue = $"First: {value}"; })
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
            .OnGet(() => "A")
            .ThenGet(() => "B");

        var setSequence = stub.Age
            .OnSet((v) => { })
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
        stub.Name.VerifyGet(Times.Exactly(2));
        stub.Age.VerifySet(Times.Exactly(2));
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

        stub.Name.OnGet("test");

        IConfigProps config = stub;

        // Access property to increment counts
        _ = config.Name;
        config.Name = "updated";

        stub.Name.VerifyGet(Times.AtLeastOnce);
        stub.Name.VerifySet(Times.AtLeastOnce);

        #region properties-reset
        // Reset clears counts but preserves callbacks
        stub.Name.Reset();

        stub.Name.VerifyGet(Times.Never);
        stub.Name.VerifySet(Times.Never);
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
        // Last OnGet call wins - can upgrade from value to callback
        stub.Name.OnGet("initial");
        stub.Name.OnGet(() => "dynamic");
        #endregion

        IConfigProps config = stub;

        // Callback syntax takes precedence (last call wins)
        Assert.Equal("dynamic", config.Name);
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
        // OnGet with static value: Fixed test data
        stub.CurrentUser.OnGet(new User { Id = 1, Name = "Alice" });

        // OnGet with callback: State-dependent behavior
        stub.IsConnected.OnGet(() => isConnected);

        // OnSet: Track all values written
        var connectionStrings = new List<string>();
        stub.ConnectionString.OnSet((value) => connectionStrings.Add(value));

        // Method callback updates the tracked state
        stub.Connect.OnCall(() => { isConnected = true; });
        #endregion

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
}
