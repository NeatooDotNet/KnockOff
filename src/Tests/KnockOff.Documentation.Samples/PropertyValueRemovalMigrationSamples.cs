namespace KnockOff.Documentation.Samples.PropertyValueRemovalMigration;

// =============================================================================
// Interfaces for Property Value Removal Migration Samples
// =============================================================================

public interface IMigrationUserService
{
    string Name { get; }
    string? Email { get; }
    int Age { get; }
    bool IsActive { get; }
}

public interface IMigrationConfigService
{
    string ConnectionString { get; }
    int Timeout { get; }
    bool IsEnabled { get; }
}

public interface IMigrationTimestampService
{
    DateTime LastUpdated { get; }
}

// =============================================================================
// Stubs for Property Value Removal Migration Samples
// =============================================================================

[KnockOff]
public partial class MigrationUserServiceStub : IMigrationUserService { }

[KnockOff]
public partial class MigrationConfigServiceStub : IMigrationConfigService { }

[KnockOff]
public partial class MigrationTimestampServiceStub : IMigrationTimestampService { }

// =============================================================================
// Old API vs New API - What Changed
// =============================================================================

public class OldApiTests
{
    #region property-value-old-api
    // OLD API (no longer compiles in 0.24.0+):
    //
    // stub.Name.Value = "Alice";           // Set property return value
    // var name = stub.Name.Value;          // Read configured value
    #endregion
}

public class NewApiTests
{
    [Fact]
    public void OnGet_ReplacesValueProperty()
    {
        var stub = new MigrationUserServiceStub();

        #region property-value-new-api
        // NEW API: Configure property return value with Get
        stub.Name.Get("Alice");
        #endregion

        IMigrationUserService service = stub;
        Assert.Equal("Alice", service.Name);
    }
}

// =============================================================================
// Migration Step 1: Replace .Value = x with .Get(x)
// =============================================================================

public class MigrationValueToOnGetTests
{
    #region migration-value-to-onget-before
    // BEFORE (0.23.x and earlier):
    //
    // stub.ConnectionString.Value = "Server=localhost";
    // stub.Timeout.Value = 30;
    // stub.IsEnabled.Value = true;
    #endregion

    [Fact]
    public void OnGet_ConfiguresPropertyValues()
    {
        var stub = new MigrationConfigServiceStub();

        #region migration-value-to-onget-after
        // AFTER (0.24.0+):
        stub.ConnectionString.Get("Server=localhost");
        stub.Timeout.Get(30);
        stub.IsEnabled.Get(true);
        #endregion

        IMigrationConfigService config = stub;
        Assert.Equal("Server=localhost", config.ConnectionString);
        Assert.Equal(30, config.Timeout);
        Assert.True(config.IsEnabled);
    }
}

// =============================================================================
// Migration Step 2: Replace .Value reads with interface assertions
// =============================================================================

public class MigrationValueReadTests
{
    #region migration-value-read-before
    // BEFORE (0.23.x and earlier):
    //
    // stub.Name.Value = "Expected";
    // Assert.Equal("Expected", stub.Name.Value);  // Reading .Value
    #endregion

    [Fact]
    public void VerifyThroughInterface_NotInterceptor()
    {
        var stub = new MigrationUserServiceStub();

        #region migration-value-read-after
        // AFTER: Configure with Get, verify through interface
        stub.Name.Get("Expected");
        #endregion

        IMigrationUserService service = stub;
        Assert.Equal("Expected", service.Name);
    }
}

// =============================================================================
// Migration Step 3: Dynamic values use Get with callback
// =============================================================================

public class MigrationDynamicValueTests
{
    #region migration-dynamic-value-before
    // BEFORE (0.23.x and earlier):
    // Value was captured once at assignment time
    //
    // stub.LastUpdated.Value = DateTime.UtcNow;
    #endregion

    [Fact]
    public void OnGetCallback_ForDynamicValues()
    {
        var stub = new MigrationTimestampServiceStub();
        var callCount = 0;

        #region migration-dynamic-value-after
        // AFTER: Use callback for values evaluated on each access
        stub.LastUpdated.Get(() => DateTime.UtcNow);
        #endregion

        stub.LastUpdated.Get(() =>
        {
            callCount++;
            return DateTime.UtcNow;
        });

        IMigrationTimestampService service = stub;

        _ = service.LastUpdated;
        _ = service.LastUpdated;

        Assert.Equal(2, callCount);
    }
}

// =============================================================================
// Migration Examples by Type
// =============================================================================

public class MigrationExampleSimplePropertyTests
{
    [Fact]
    public void SimpleProperty_MigrationExample()
    {
        var stub = new MigrationUserServiceStub();

        #region migration-example-simple-property
        // BEFORE: stub.Name.Value = "Alice";
        // AFTER:
        stub.Name.Get("Alice");
        #endregion

        IMigrationUserService service = stub;
        Assert.Equal("Alice", service.Name);
    }
}

public class MigrationExampleNullablePropertyTests
{
    [Fact]
    public void NullableProperty_MigrationExample()
    {
        var stub = new MigrationUserServiceStub();

        #region migration-example-nullable-property
        // BEFORE: stub.Email.Value = null;
        // AFTER: Cast null to the property type for Get
        stub.Email.Get((string?)null);
        #endregion

        IMigrationUserService service = stub;
        Assert.Null(service.Email);
    }
}

public class MigrationExampleValueTypePropertyTests
{
    [Fact]
    public void ValueTypeProperty_MigrationExample()
    {
        var stub = new MigrationUserServiceStub();

        #region migration-example-value-type-property
        // BEFORE: stub.Age.Value = 42;
        // AFTER:
        stub.Age.Get(42);
        #endregion

        IMigrationUserService service = stub;
        Assert.Equal(42, service.Age);
    }
}

public class MigrationExampleBooleanPropertyTests
{
    [Fact]
    public void BooleanProperty_MigrationExample()
    {
        var stub = new MigrationUserServiceStub();

        #region migration-example-boolean-property
        // BEFORE: stub.IsActive.Value = true;
        // AFTER:
        stub.IsActive.Get(true);
        #endregion

        IMigrationUserService service = stub;
        Assert.True(service.IsActive);
    }
}
