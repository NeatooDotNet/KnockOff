namespace KnockOff.Documentation.Samples.SmartDefaults;

// =============================================================================
// Interfaces for Smart Defaults Samples
// =============================================================================

public interface IValueTypeService
{
    int GetCount();
    bool IsEnabled();
    decimal GetRate();
    DateTime GetTimestamp();
}

public interface INullableService
{
    string? GetOptionalName();
    User? FindUserById(int id);
    int? GetOptionalCount();
    bool? GetOptionalFlag();
}

public interface IConfigService
{
    AppConfig GetConfig();
    FeatureOptions GetOptions();
}

// Classes with parameterless constructors
public class AppConfig
{
    public string Environment { get; set; } = "default";
    public int MaxRetries { get; set; } = 3;
}

public class FeatureOptions
{
    public bool FeatureA { get; set; }
    public bool FeatureB { get; set; }
}

public interface ICollectionService
{
    IEnumerable<User> GetUsers();
    IList<string> GetTags();
    IReadOnlyList<int> GetIds();
    IDictionary<string, string> GetMetadata();
    ISet<string> GetUniqueKeys();
}

/// <summary>
/// A class WITHOUT a parameterless constructor - used to demonstrate
/// that smart defaults throw for types that cannot be instantiated.
/// </summary>
public class UserWithRequiredCtor
{
    public int Id { get; }
    public string Name { get; }

    public UserWithRequiredCtor(int id, string name)
    {
        Id = id;
        Name = name;
    }
}

public interface IUserFactory
{
    // UserWithRequiredCtor has no parameterless constructor
    UserWithRequiredCtor GetUser();
}

public interface IAsyncDefaultsService
{
    Task<User?> GetUserAsync(int id);
    Task<int> GetCountAsync();
    Task CompleteAsync();
    ValueTask<bool> IsValidAsync();
}

// =============================================================================
// Stubs for Smart Defaults Samples
// =============================================================================

[KnockOff]
public partial class ValueTypeServiceStub : IValueTypeService { }

[KnockOff]
public partial class NullableServiceStub : INullableService { }

[KnockOff]
public partial class ConfigServiceStub : IConfigService { }

[KnockOff]
public partial class CollectionServiceStub : ICollectionService { }

[KnockOff]
public partial class UserFactoryStub : IUserFactory { }

[KnockOff]
public partial class AsyncDefaultsServiceStub : IAsyncDefaultsService { }

// =============================================================================
// Value Types Smart Defaults
// =============================================================================

public class ValueTypeSmartDefaultsTests
{
    [Fact]
    public void ValueTypes_ReturnDefault()
    {
        var stub = new ValueTypeServiceStub();
        IValueTypeService service = stub;

        #region smart-defaults-value-types
        // No configuration needed - value types return default(T)
        int count = service.GetCount();      // returns 0
        bool enabled = service.IsEnabled();  // returns false
        #endregion

        Assert.Equal(0, count);
        Assert.False(enabled);
        Assert.Equal(0.0m, service.GetRate());
        Assert.Equal(default(DateTime), service.GetTimestamp());
    }
}

// =============================================================================
// Nullable Reference Types Smart Defaults
// =============================================================================

public class NullableSmartDefaultsTests
{
    [Fact]
    public void NullableTypes_ReturnNull()
    {
        var stub = new NullableServiceStub();
        INullableService service = stub;

        #region smart-defaults-nullable
        // Nullable types return null (both reference and value types)
        string? name = service.GetOptionalName();  // returns null
        int? count = service.GetOptionalCount();   // returns null
        #endregion

        Assert.Null(name);
        Assert.Null(count);
        Assert.Null(service.FindUserById(42));
        Assert.Null(service.GetOptionalFlag());
    }
}

// =============================================================================
// Types with Parameterless Constructor
// =============================================================================

public class ConstructorSmartDefaultsTests
{
    [Fact]
    public void TypesWithCtor_ReturnNewInstance()
    {
        var stub = new ConfigServiceStub();
        IConfigService service = stub;

        #region smart-defaults-ctor
        // Types with parameterless constructor return new T()
        AppConfig config = service.GetConfig();  // returns new AppConfig()
        #endregion

        Assert.NotNull(config);
        Assert.Equal("default", config.Environment);
        Assert.NotNull(service.GetOptions());
    }
}

// =============================================================================
// Collection Interfaces Smart Defaults
// =============================================================================

public class CollectionSmartDefaultsTests
{
    [Fact]
    public void Collections_ReturnEmptyInstances()
    {
        var stub = new CollectionServiceStub();
        ICollectionService service = stub;

        #region smart-defaults-collections
        // Collection interfaces return empty, non-null collections
        IEnumerable<User> users = service.GetUsers();       // returns new List<User>()
        IDictionary<string, string> meta = service.GetMetadata();  // returns new Dictionary<>()
        #endregion

        Assert.NotNull(users);
        Assert.Empty(users);
        Assert.NotNull(meta);
        Assert.Empty(meta);
        Assert.Empty(service.GetTags());
        Assert.Empty(service.GetIds());
        Assert.Empty(service.GetUniqueKeys());
    }
}

// =============================================================================
// Types Without Parameterless Constructor
// =============================================================================

public class NoConstructorSmartDefaultsTests
{
    [Fact]
    public void TypeWithoutCtor_ThrowsWithoutConfiguration()
    {
        var stub = new UserFactoryStub();
        IUserFactory factory = stub;

        #region smart-defaults-throw
        // Types without parameterless constructor throw if not configured
        // factory.GetUser(); // throws InvalidOperationException

        // Fix: configure Return to provide the value
        stub.GetUser.Return(() => new UserWithRequiredCtor(1, "Configured"));
        #endregion

        var user = factory.GetUser();
        Assert.NotNull(user);
        Assert.Equal("Configured", user.Name);
    }

    [Fact]
    public void TypeWithoutCtor_ActuallyThrows()
    {
        var stub = new UserFactoryStub();
        IUserFactory factory = stub;

        var exception = Assert.Throws<InvalidOperationException>(() => factory.GetUser());
        Assert.Contains("No implementation provided", exception.Message);
    }
}

// =============================================================================
// Async Smart Defaults
// =============================================================================

public class AsyncSmartDefaultsTests
{
    [Fact]
    public async Task AsyncTypes_ReturnCompletedTasks()
    {
        var stub = new AsyncDefaultsServiceStub();
        IAsyncDefaultsService service = stub;

        #region smart-defaults-async
        // Async methods return completed tasks with smart defaults for inner type
        int count = await service.GetCountAsync();  // returns Task.FromResult(0)
        await service.CompleteAsync();              // returns Task.CompletedTask
        #endregion

        Assert.Equal(0, count);
        Assert.Null(await service.GetUserAsync(1));
        Assert.False(await service.IsValidAsync());
    }
}

// =============================================================================
// Override Smart Defaults
// =============================================================================

public interface IOverridableService
{
    User? GetUser();
}

[KnockOff]
public partial class OverridableServiceStub : IOverridableService { }

[KnockOff]
public partial class UserMethodOverrideStub : IOverridableService
{
    #region smart-defaults-override-user-method
    protected override User? GetUser_() => new User { Name = "Test" };
    #endregion
}

public class RealOverridableService : IOverridableService
{
    public User? GetUser() => new User { Name = "Real" };
}

public class OverrideSmartDefaultsTests
{
    [Fact]
    public void Override_WithReturn()
    {
        var stub = new OverridableServiceStub();

        #region smart-defaults-override-oncall
        stub.GetUser.Return(() => new User { Name = "Test" });
        #endregion

        IOverridableService service = stub;
        Assert.Equal("Test", service.GetUser()!.Name);
    }

    [Fact]
    public void Override_WithUserMethod()
    {
        var stub = new UserMethodOverrideStub();
        IOverridableService service = stub;
        Assert.Equal("Test", service.GetUser()!.Name);
    }

    [Fact]
    public void Override_WithSource()
    {
        var stub = new OverridableServiceStub();

        #region smart-defaults-override-source
        stub.Source(new RealOverridableService());
        #endregion

        IOverridableService service = stub;
        Assert.Equal("Real", service.GetUser()!.Name);
    }
}
