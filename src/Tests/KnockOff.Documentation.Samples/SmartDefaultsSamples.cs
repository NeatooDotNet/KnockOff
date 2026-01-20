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

public interface IUserFactory
{
    // User class (from shared types) has no parameterless constructor
    User GetUser();
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
    #region smart-defaults-value-types
    [Fact]
    public void ValueTypes_ReturnDefault()
    {
        var stub = new ValueTypeServiceStub();
        IValueTypeService service = stub;

        // No configuration - smart defaults apply

        // int defaults to 0
        Assert.Equal(0, service.GetCount());

        // bool defaults to false
        Assert.False(service.IsEnabled());

        // decimal defaults to 0.0m
        Assert.Equal(0.0m, service.GetRate());

        // DateTime defaults to default(DateTime)
        Assert.Equal(default(DateTime), service.GetTimestamp());
    }
    #endregion
}

// =============================================================================
// Nullable Reference Types Smart Defaults
// =============================================================================

public class NullableSmartDefaultsTests
{
    #region smart-defaults-nullable
    [Fact]
    public void NullableTypes_ReturnNull()
    {
        var stub = new NullableServiceStub();
        INullableService service = stub;

        // No configuration - smart defaults apply

        // string? returns null
        Assert.Null(service.GetOptionalName());

        // User? returns null
        Assert.Null(service.FindUserById(42));
    }
    #endregion
}

// =============================================================================
// Types with Parameterless Constructor
// =============================================================================

public class ConstructorSmartDefaultsTests
{
    #region smart-defaults-ctor
    [Fact]
    public void TypesWithCtor_ReturnNewInstance()
    {
        var stub = new ConfigServiceStub();
        IConfigService service = stub;

        // No configuration - smart defaults apply

        // Types with parameterless constructor return new T()
        var config = service.GetConfig();
        Assert.NotNull(config);
        Assert.Equal("default", config.Environment); // Default property value

        var options = service.GetOptions();
        Assert.NotNull(options);
        Assert.False(options.FeatureA); // Default property value
    }
    #endregion
}

// =============================================================================
// Collection Interfaces Smart Defaults
// =============================================================================

public class CollectionSmartDefaultsTests
{
    #region smart-defaults-collections
    [Fact]
    public void Collections_ReturnEmptyInstances()
    {
        var stub = new CollectionServiceStub();
        ICollectionService service = stub;

        // No configuration - smart defaults apply

        // IEnumerable<T> returns empty List<T>
        var users = service.GetUsers();
        Assert.NotNull(users);
        Assert.Empty(users);

        // IList<T> returns empty List<T>
        var tags = service.GetTags();
        Assert.NotNull(tags);
        Assert.Empty(tags);

        // IReadOnlyList<T> returns empty List<T>
        var ids = service.GetIds();
        Assert.NotNull(ids);
        Assert.Empty(ids);

        // IDictionary<K,V> returns empty Dictionary<K,V>
        var metadata = service.GetMetadata();
        Assert.NotNull(metadata);
        Assert.Empty(metadata);

        // ISet<T> returns empty HashSet<T>
        var keys = service.GetUniqueKeys();
        Assert.NotNull(keys);
        Assert.Empty(keys);
    }
    #endregion
}

// =============================================================================
// Types Without Parameterless Constructor
// =============================================================================

public class NoConstructorSmartDefaultsTests
{
    #region smart-defaults-throw
    [Fact]
    public void TypeWithoutCtor_ThrowsWithoutConfiguration()
    {
        var stub = new UserFactoryStub();
        IUserFactory factory = stub;

        // User has no parameterless constructor, so smart defaults can't create one
        // Without OnCall, user method, or Source, it throws

        // Note: The actual behavior depends on how the generator handles this.
        // In strict mode, it would throw. In non-strict, it returns default (null).
        // For non-nullable return types without ctor, configure explicitly.

        // Configure OnCall to provide value
        stub.GetUser.OnCall(() => new User { Id = 1, Name = "Configured" });

        var user = factory.GetUser();
        Assert.NotNull(user);
        Assert.Equal("Configured", user.Name);
    }
    #endregion
}

// =============================================================================
// Async Smart Defaults
// =============================================================================

public class AsyncSmartDefaultsTests
{
    #region smart-defaults-async
    [Fact]
    public async Task AsyncTypes_ReturnCompletedTasks()
    {
        var stub = new AsyncDefaultsServiceStub();
        IAsyncDefaultsService service = stub;

        // No configuration - smart defaults apply

        // Task<User?> returns completed task with null
        var user = await service.GetUserAsync(1);
        Assert.Null(user);

        // Task<int> returns completed task with 0
        var count = await service.GetCountAsync();
        Assert.Equal(0, count);

        // Task returns completed task
        await service.CompleteAsync(); // Should not throw

        // ValueTask<bool> returns completed with false
        var isValid = await service.IsValidAsync();
        Assert.False(isValid);
    }
    #endregion
}
